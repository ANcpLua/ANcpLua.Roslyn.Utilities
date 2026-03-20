namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Thread-safe circular buffer with O(1) push, pre-allocated storage, and generation
///     tracking for cache invalidation.
/// </summary>
/// <typeparam name="T">The element type stored in the buffer. Must be a reference type.</typeparam>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class CircularBuffer<T> where T : class
{
    private readonly T?[] _buffer;
    private readonly Lock _lock = new();
    private int _count;
    private ulong _generation;
    private int _head;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CircularBuffer{T}" /> class.
    /// </summary>
    /// <param name="capacity">The fixed capacity of the buffer. Must be at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity" /> is less than 1.</exception>
    public CircularBuffer(int capacity = 10_000)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be at least 1.");

        Capacity = capacity;
        _buffer = new T?[capacity];
    }

    /// <summary>
    ///     Gets the fixed capacity of the buffer.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    ///     Gets the current number of elements in the buffer.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock) return _count;
        }
    }

    /// <summary>
    ///     Gets the current generation counter. Incremented on every mutation,
    ///     enabling consumers to detect changes without re-reading the data.
    /// </summary>
    public ulong Generation => Volatile.Read(ref _generation);

    /// <summary>
    ///     Pushes a single item into the buffer, overwriting the oldest entry when full.
    /// </summary>
    /// <param name="item">The item to push. Must not be null.</param>
    public void Push(T item)
    {
        Guard.NotNull(item);
        lock (_lock)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;
            Volatile.Write(ref _generation, _generation + 1);
        }
    }

    /// <summary>
    ///     Pushes multiple items into the buffer in a single lock acquisition.
    /// </summary>
    /// <param name="items">The items to push.</param>
    public void PushRange(IEnumerable<T> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));

        var materialized = items as IReadOnlyList<T> ?? [.. items];
        lock (_lock)
        {
            foreach (var item in materialized)
            {
                _buffer[_head] = item;
                _head = (_head + 1) % Capacity;
                if (_count < Capacity) _count++;
            }

            Volatile.Write(ref _generation, _generation + 1);
        }
    }

    /// <summary>
    ///     Returns the most recently pushed items in newest-first order.
    /// </summary>
    /// <param name="count">The maximum number of items to return.</param>
    /// <param name="generation">Receives the generation at the time of the snapshot.</param>
    /// <returns>An array of the most recent items, up to <paramref name="count" />.</returns>
    public T[] GetLatest(int count, out ulong generation)
    {
        lock (_lock)
        {
            generation = _generation;
            var take = System.Math.Min(count, _count);
            if (take is 0) return [];
            var result = new T[take];
            var idx = (_head - 1 + Capacity) % Capacity;
            for (var i = 0; i < take; i++)
            {
                if (_buffer[idx] is { } item)
                    result[i] = item;
                idx = (idx - 1 + Capacity) % Capacity;
            }

            return result;
        }
    }

    /// <summary>
    ///     Queries the buffer from newest to oldest, returning items matching a predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="maxCount">The maximum number of matching items to return.</param>
    /// <param name="generation">Receives the generation at the time of the snapshot.</param>
    /// <returns>An array of matching items, up to <paramref name="maxCount" />.</returns>
    public T[] Query(Func<T, bool> predicate, int maxCount, out ulong generation)
    {
        lock (_lock)
        {
            generation = _generation;
            if (_count is 0) return [];
            var results = new List<T>(System.Math.Min(maxCount, _count));
            var idx = (_head - 1 + Capacity) % Capacity;
            var scanned = 0;
            while (scanned < _count && results.Count < maxCount)
            {
                var item = _buffer[idx];
                if (item is not null && predicate(item)) results.Add(item);
                idx = (idx - 1 + Capacity) % Capacity;
                scanned++;
            }

            return [.. results];
        }
    }

    /// <summary>
    ///     Returns all items in oldest-first order.
    /// </summary>
    /// <param name="generation">Receives the generation at the time of the snapshot.</param>
    /// <returns>An array of all items from oldest to newest.</returns>
    public T[] GetAllOldestFirst(out ulong generation)
    {
        lock (_lock)
        {
            generation = _generation;
            if (_count is 0) return [];
            var result = new T[_count];
            var startIdx = _count < Capacity ? 0 : _head;
            for (var i = 0; i < _count; i++)
            {
                var idx = (startIdx + i) % Capacity;
                if (_buffer[idx] is { } item)
                    result[i] = item;
            }

            return result;
        }
    }

    /// <summary>
    ///     Removes all items from the buffer and increments the generation.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _count = 0;
            Volatile.Write(ref _generation, _generation + 1);
        }
    }
}
