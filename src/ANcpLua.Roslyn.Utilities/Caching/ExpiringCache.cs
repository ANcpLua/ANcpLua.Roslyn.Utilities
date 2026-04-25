using System.Collections.Concurrent;

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
    private readonly ConcurrentDictionary<TKey, CacheEntry> _cache;
    private readonly TimeSpan _idleTimeout;
    private readonly ConcurrentQueue<TKey> _insertionOrder = new();
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
        _cache = new ConcurrentDictionary<TKey, CacheEntry>(keyComparer ?? EqualityComparer<TKey>.Default);
    }

    /// <summary>
    ///     Gets the current number of entries in the cache.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    ///     Gets the value associated with the specified key, or creates and caches a new value
    ///     using the provided factory if the key is not present or has expired.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">A factory function invoked when the key is not found in the cache.</param>
    /// <returns>The cached or newly created value.</returns>
    public TValue? GetOrAdd(TKey key, Func<TValue?> factory)
    {
        var now = TimeProviderShim.System.GetUtcNow();

        if (_cache.TryGetValue(key, out var existing))
        {
            existing.Touch(now);
            return existing.Value;
        }

        var created = factory();
        var entry = new CacheEntry(created, now);

        if (_cache.TryAdd(key, entry))
        {
            _insertionOrder.Enqueue(key);
            EvictIfNeeded(now);
            return created;
        }

        if (_cache.TryGetValue(key, out existing))
        {
            existing.Touch(now);
            return existing.Value;
        }

        return created;
    }

    private void EvictIfNeeded(DateTimeOffset now)
    {
        while (_cache.Count > _maxEntries && _insertionOrder.TryDequeue(out var oldest))
        {
            _cache.TryRemove(oldest, out _);
        }

        foreach (var kvp in _cache)
        {
            if (now - kvp.Value.LastAccessUtc <= _idleTimeout)
                continue;

            _cache.TryRemove(kvp.Key, out _);
        }
    }

    private sealed class CacheEntry
    {
        public CacheEntry(TValue? value, DateTimeOffset lastAccessUtc)
        {
            Value = value;
            LastAccessUtc = lastAccessUtc;
        }

        public TValue? Value { get; }
        public DateTimeOffset LastAccessUtc { get; private set; }

        public void Touch(DateTimeOffset now) => LastAccessUtc = now;
    }
}
