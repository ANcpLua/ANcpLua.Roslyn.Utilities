using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>Thread-safe LRU + idle-timeout cache with atomic get-or-add via value factory.</summary>
/// <typeparam name="TKey">The type of cache keys.</typeparam>
/// <typeparam name="TValue">The type of cached values.</typeparam>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class ExpiringCache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, CacheEntry> _cache;
    private readonly ConcurrentDictionary<TKey, Lazy<TValue?>> _inFlight;
    private readonly LinkedList<TKey> _lru = new();
    private readonly object _lock = new();
    private readonly TimeSpan _idleTimeout;
    private readonly int _maxEntries;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExpiringCache{TKey, TValue}" /> class.
    /// </summary>
    /// <param name="maxEntries">Maximum number of entries before LRU eviction begins.</param>
    /// <param name="idleTimeout">Duration after which an untouched entry is eligible for expiry.</param>
    /// <param name="keyComparer">Optional equality comparer for keys.</param>
    public ExpiringCache(
        int maxEntries = 10_000,
        TimeSpan? idleTimeout = null,
        IEqualityComparer<TKey>? keyComparer = null)
    {
        if (maxEntries < 1)
            throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "Max entries must be at least 1.");

        _maxEntries = maxEntries;
        _idleTimeout = idleTimeout ?? TimeSpan.FromMinutes(60);
        _cache = new Dictionary<TKey, CacheEntry>(keyComparer ?? EqualityComparer<TKey>.Default);
        _inFlight = new ConcurrentDictionary<TKey, Lazy<TValue?>>(keyComparer ?? EqualityComparer<TKey>.Default);
    }

    /// <summary>
    ///     Gets the current number of entries in the cache.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
                return _cache.Count;
        }
    }

    /// <summary>
    ///     Gets the value associated with the specified key, or creates and caches a new value
    ///     using the provided factory if the key is not present or has expired.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">A factory function invoked when the key is not found in the cache.</param>
    /// <returns>The cached or newly created value.</returns>
    public TValue? GetOrAdd(TKey key, Func<TValue?> factory)
    {
        var now = DateTimeOffset.UtcNow;
        TValue? cached;

        lock (_lock)
        {
            if (TryGetValueLocked(key, now, out cached))
                return cached;
        }

        var created = CreateOrWaitForInflight(key, factory);

        lock (_lock)
        {
            now = DateTimeOffset.UtcNow;

            if (TryGetValueLocked(key, now, out cached))
                return cached;

            var node = _lru.AddLast(key);
            _cache[key] = new CacheEntry(created, now, node);
            EvictIfNeeded(now);
            return created;
        }
    }

    private TValue? CreateOrWaitForInflight(TKey key, Func<TValue?> factory)
    {
        // ExecutionAndPublication makes Lazy<T> replay any factory exception to all concurrent waiters.
        // The owner's finally clears the in-flight entry, so the replay window is bounded to a single
        // factory invocation; a subsequent GetOrAdd for the same key will create a fresh Lazy and retry.
        var entry = new Lazy<TValue?>(factory, LazyThreadSafetyMode.ExecutionAndPublication);
        var owner = _inFlight.GetOrAdd(key, entry);
        var isOwner = ReferenceEquals(entry, owner);

        try
        {
            return owner.Value;
        }
        finally
        {
            if (isOwner)
                _inFlight.TryRemove(key, out _);
        }
    }

    private bool TryGetValueLocked(TKey key, DateTimeOffset now, out TValue? value)
    {
        if (!_cache.TryGetValue(key, out var entry))
        {
            value = default;
            return false;
        }

        if (!entry.IsFresh(now, _idleTimeout))
        {
            RemoveLocked(key);
            value = default;
            return false;
        }

        entry.Touch(now, _lru);
        value = entry.Value;
        return true;
    }

    private void EvictIfNeeded(DateTimeOffset now)
    {
        while (_lru.First is not null && _cache.TryGetValue(_lru.First.Value, out var entry) && !entry.IsFresh(now, _idleTimeout))
            RemoveLocked(_lru.First.Value);

        while (_cache.Count > _maxEntries && _lru.First is not null)
            RemoveLocked(_lru.First.Value);
    }

    private void RemoveLocked(TKey key)
    {
        if (_cache.TryGetValue(key, out var removed))
        {
            _cache.Remove(key);
            _lru.Remove(removed.Node);
        }
    }

    private sealed class CacheEntry
    {
        public CacheEntry(TValue? value, DateTimeOffset lastAccessUtc, LinkedListNode<TKey> node)
        {
            Value = value;
            LastAccessUtc = lastAccessUtc;
            Node = node;
        }

        public TValue? Value { get; }
        public DateTimeOffset LastAccessUtc { get; private set; }
        public LinkedListNode<TKey> Node { get; }

        public bool IsFresh(DateTimeOffset now, TimeSpan timeout)
            => now - LastAccessUtc <= timeout;

        public void Touch(DateTimeOffset now, LinkedList<TKey> lru)
        {
            LastAccessUtc = now;

            lru.Remove(Node);
            lru.AddLast(Node);
        }
    }
}
