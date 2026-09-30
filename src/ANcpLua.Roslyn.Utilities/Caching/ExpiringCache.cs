using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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
    // Lazy<T> declares PublicParameterlessConstructor on its T; the in-flight entries are Lazy<TValue?>, so the trim
    // analyzer needs the same annotation here (IL2091 otherwise, in every trimmed or AOT consumer).
    sealed class ExpiringCache<TKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue>
    where TKey : notnull
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
        if (TryGetFresh(key, out var cached))
            return cached;

        // One Lazy per in-flight key. Its value re-checks the cache first, so a caller that registers after
        // another owner has published and left takes that value instead of running the factory again.
        // ExecutionAndPublication replays the value, or the factory exception, to every concurrent waiter.
        var entry = new Lazy<TValue?>(
            () => TryGetFresh(key, out var published) ? published : factory(),
            LazyThreadSafetyMode.ExecutionAndPublication);
        var owner = _inFlight.GetOrAdd(key, entry);

        try
        {
            var created = owner.Value;

            // Every caller returns the one published instance; the first to get here publishes it.
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;
                if (TryGetValueLocked(key, now, out cached))
                    return cached;

                _cache[key] = new CacheEntry(created, now, _lru.AddLast(key));
                EvictIfNeeded(now);
                return created;
            }
        }
        finally
        {
            // Only after the value is published, or the factory has failed and the next call may retry.
            if (ReferenceEquals(entry, owner))
                _inFlight.TryRemove(key, out _);
        }
    }

    private bool TryGetFresh(TKey key, out TValue? value)
    {
        lock (_lock)
            return TryGetValueLocked(key, DateTimeOffset.UtcNow, out value);
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
