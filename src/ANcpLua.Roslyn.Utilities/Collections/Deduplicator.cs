namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Sliding-window deduplicator. Emits the first occurrence of each item immediately,
///     suppresses repeated identical items inside the window, then emits a summary
///     once the burst quiets or the suppression limit is reached.
/// </summary>
/// <typeparam name="T">The type of items to deduplicate.</typeparam>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class Deduplicator<T>
{
    private readonly Dictionary<string, DedupBucket> _buckets = new(StringComparer.Ordinal);
    private readonly Func<T, string> _keyFactory;
    private readonly int _maxSuppressed;
    private readonly TimeSpan _window;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Deduplicator{T}" /> class.
    /// </summary>
    /// <param name="window">The sliding window duration. Buckets expire after this period of inactivity.</param>
    /// <param name="keyFactory">
    ///     A function that extracts a deduplication key from an item.
    ///     Items producing the same key are considered duplicates.
    /// </param>
    /// <param name="maxSuppressed">
    ///     Maximum number of duplicates to suppress before forcing a summary emission.
    ///     Must be at least 1.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="window" /> is zero or negative, or <paramref name="maxSuppressed" /> is less than 1.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keyFactory" /> is null.</exception>
    public Deduplicator(TimeSpan window, Func<T, string> keyFactory, int maxSuppressed = 100)
    {
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window), window, "Window must be positive.");
        if (maxSuppressed < 1)
            throw new ArgumentOutOfRangeException(nameof(maxSuppressed), maxSuppressed, "Max suppressed must be at least 1.");

        _window = window;
        _keyFactory = Guard.NotNull(keyFactory);
        _maxSuppressed = maxSuppressed;
    }

    /// <summary>
    ///     Processes items in ascending timestamp order.
    ///     First occurrences are emitted immediately; duplicate bursts are summarized later.
    /// </summary>
    /// <param name="items">The items to process, ordered by ascending timestamp.</param>
    /// <param name="timestampSelector">A function that extracts the UTC timestamp from each item.</param>
    /// <returns>A list of deduplicated results.</returns>
    public IReadOnlyList<DeduplicatedItem<T>> ProcessBatch(
        IEnumerable<T> items,
        Func<T, DateTime> timestampSelector)
    {
        var output = new List<DeduplicatedItem<T>>();

        foreach (var item in items)
        {
            var timestamp = timestampSelector(item);
            FlushExpired(timestamp, output);

            var key = _keyFactory(item);
            if (_buckets.TryGetValue(key, out var bucket))
            {
                bucket.Count++;
                bucket.LastSeenUtc = timestamp;
                bucket.LastItem = item;

                // Prevent unbounded suppression during sustained spam bursts.
                if (bucket.Count - 1 >= _maxSuppressed)
                {
                    EmitSummary(bucket, output);
                    _buckets.Remove(key);
                }

                continue;
            }

            _buckets[key] = new DedupBucket(item, timestamp);
            output.Add(new DeduplicatedItem<T>(item));
        }

        return output;
    }

    /// <summary>
    ///     Flushes only buckets whose dedupe windows have expired.
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>Summary items for expired buckets that had suppressed duplicates.</returns>
    public IReadOnlyList<DeduplicatedItem<T>> FlushExpired(DateTime utcNow)
    {
        var output = new List<DeduplicatedItem<T>>();
        FlushExpired(utcNow, output);
        return output;
    }

    /// <summary>
    ///     Flushes all pending buckets, emitting summaries for any that had suppressed duplicates.
    /// </summary>
    /// <returns>Summary items for all remaining buckets that had suppressed duplicates.</returns>
    public IReadOnlyList<DeduplicatedItem<T>> FlushAll()
    {
        var output = new List<DeduplicatedItem<T>>(_buckets.Count);
        var keys = _buckets.Keys.ToArray();

        foreach (var key in keys)
        {
            var bucket = _buckets[key];
            EmitSummary(bucket, output);
            _buckets.Remove(key);
        }

        return output;
    }

    private static void EmitSummary(DedupBucket bucket, ICollection<DeduplicatedItem<T>> output)
    {
        var suppressed = bucket.Count - 1;
        if (suppressed <= 0)
            return;

        output.Add(new DeduplicatedItem<T>(bucket.LastItem, suppressed, true));
    }

    private void FlushExpired(DateTime utcNow, ICollection<DeduplicatedItem<T>> output)
    {
        if (_buckets.Count is 0)
            return;

        var expiredKeys = new List<string>();
        foreach (var pair in _buckets)
        {
            var elapsed = utcNow - pair.Value.LastSeenUtc;
            if (elapsed >= _window)
                expiredKeys.Add(pair.Key);
        }

        foreach (var key in expiredKeys)
        {
            var bucket = _buckets[key];
            EmitSummary(bucket, output);
            _buckets.Remove(key);
        }
    }

    private sealed class DedupBucket(T firstItem, DateTime firstSeenUtc)
    {
        public int Count { get; set; } = 1;
        public DateTime LastSeenUtc { get; set; } = firstSeenUtc;
        public T LastItem { get; set; } = firstItem;
    }
}

/// <summary>
///     An item emitted by deduplication.
/// </summary>
/// <typeparam name="T">The type of the original item.</typeparam>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed record DeduplicatedItem<T>(T Item, int RepeatCount = 1, bool IsDuplicateSummary = false);
