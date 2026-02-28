using System.Threading.Tasks;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Debounces multiple <see cref="AddWork(T)" /> calls into batched asynchronous processing
///     with a configurable delay.
/// </summary>
/// <typeparam name="T">The type of work items to batch.</typeparam>
/// <remarks>
///     <para>
///         Each call to <see cref="AddWork(T)" /> or <see cref="AddWork(IEnumerable{T})" /> restarts
///         an internal delay timer. When the timer expires (no new items arrive within the delay window),
///         all accumulated items are passed to the <c>processBatchAsync</c> callback as an
///         <see cref="ImmutableArray{T}" />.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Thread-safe: multiple threads can call <see cref="AddWork(T)" /> concurrently.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Optional deduplication via the <c>equalityComparer</c> constructor parameter.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Use <see cref="WaitUntilCurrentBatchCompletesAsync" /> in tests to await completion
///                 of the current processing cycle.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// // Process file changes in batches, debounced by 500ms
/// var queue = new AsyncBatchingWorkQueue&lt;string&gt;(
///     delay: TimeSpan.FromMilliseconds(500),
///     processBatchAsync: async (files, ct) =&gt;
///     {
///         foreach (var file in files)
///             await ReindexFileAsync(file, ct);
///     },
///     equalityComparer: StringComparer.OrdinalIgnoreCase,
///     cancellationToken: appShutdown);
///
/// // Each change restarts the 500ms timer
/// queue.AddWork("src/Program.cs");
/// queue.AddWork("src/Helpers.cs");
/// // After 500ms of quiet, both files are processed in one batch
/// </code>
/// </example>
/// <seealso cref="AsyncBatchingWorkQueue" />
/// <seealso cref="CancellationSeries" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    class AsyncBatchingWorkQueue<T> : IDisposable
{
    private readonly TimeSpan _delay;
    private readonly Func<ImmutableArray<T>, CancellationToken, ValueTask> _processBatchAsync;
    private readonly IEqualityComparer<T>? _equalityComparer;
    private readonly CancellationToken _cancellationToken;
    private readonly object _gate = new();
    private readonly Timer _timer;

    private List<T> _pendingItems = new();
    private TaskCompletionSource<bool>? _currentBatchTcs;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncBatchingWorkQueue{T}" /> class.
    /// </summary>
    /// <param name="delay">
    ///     The debounce delay. After the last <see cref="AddWork(T)" /> call, processing begins
    ///     after this duration elapses with no new items.
    /// </param>
    /// <param name="processBatchAsync">
    ///     The callback invoked to process a batch of items. Receives an <see cref="ImmutableArray{T}" />
    ///     of accumulated items and a <see cref="CancellationToken" />.
    /// </param>
    /// <param name="equalityComparer">
    ///     An optional equality comparer used to deduplicate items before processing.
    ///     When <c>null</c>, no deduplication is performed.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token that, when cancelled, stops the timer and prevents further batch processing.
    /// </param>
    public AsyncBatchingWorkQueue(
        TimeSpan delay,
        Func<ImmutableArray<T>, CancellationToken, ValueTask> processBatchAsync,
        IEqualityComparer<T>? equalityComparer,
        CancellationToken cancellationToken)
    {
        _delay = delay;
        _processBatchAsync = Guard.NotNull(processBatchAsync);
        _equalityComparer = equalityComparer;
        _cancellationToken = cancellationToken;
        _timer = new Timer(OnTimerFired, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    ///     Enqueues a single work item and restarts the debounce timer.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    public void AddWork(T item)
    {
        lock (_gate)
        {
            _pendingItems.Add(item);
            RestartTimer();
        }
    }

    /// <summary>
    ///     Enqueues multiple work items and restarts the debounce timer.
    /// </summary>
    /// <param name="items">The items to enqueue.</param>
    public void AddWork(IEnumerable<T> items)
    {
        Guard.NotNull(items);

        lock (_gate)
        {
            _pendingItems.AddRange(items);
            RestartTimer();
        }
    }

    /// <summary>
    ///     Returns a <see cref="Task" /> that completes when the current batch processing cycle finishes.
    /// </summary>
    /// <returns>
    ///     A task that completes when the current batch has been processed, or a completed task
    ///     if no batch is currently being processed.
    /// </returns>
    /// <remarks>
    ///     Primarily intended for use in tests to wait for a batch to finish before asserting results.
    /// </remarks>
    public Task WaitUntilCurrentBatchCompletesAsync()
    {
        lock (_gate)
        {
            return _currentBatchTcs?.Task ?? Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Stops the debounce timer and releases the underlying <see cref="Timer" /> resource.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Dispose();
    }

    private void RestartTimer()
    {
        // Called under lock
        if (_disposed)
        {
            return;
        }

        try
        {
            _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException)
        {
            // Timer was disposed concurrently — ignore
        }
    }

    private void OnTimerFired(object? state)
    {
        _ = ProcessBatchAsync();
    }

    private async Task ProcessBatchAsync()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            return;
        }

        List<T> itemsToProcess;
        TaskCompletionSource<bool> tcs;

        lock (_gate)
        {
            if (_pendingItems.Count == 0)
            {
                return;
            }

            itemsToProcess = _pendingItems;
            _pendingItems = new List<T>();

            tcs = new TaskCompletionSource<bool>();
            _currentBatchTcs = tcs;
        }

        try
        {
            ImmutableArray<T> batch;
            if (_equalityComparer != null)
            {
                batch = itemsToProcess.Distinct(_equalityComparer).ToImmutableArray();
            }
            else
            {
                batch = itemsToProcess.ToImmutableArray();
            }

            if (batch.Length > 0)
            {
                await _processBatchAsync(batch, _cancellationToken).ConfigureAwait(false);
            }

            tcs.TrySetResult(true);
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
            tcs.TrySetCanceled(_cancellationToken);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }
}

/// <summary>
///     A non-generic convenience wrapper around <see cref="AsyncBatchingWorkQueue{T}" />
///     for scenarios where only the signal to process matters, not the data.
/// </summary>
/// <remarks>
///     <para>
///         Use this when you need debounced batch notification but the individual work items
///         carry no data. Each <see cref="AddWork" /> call signals that work is available,
///         and the batch callback receives the count of signals as items.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// var queue = new AsyncBatchingWorkQueue(
///     delay: TimeSpan.FromMilliseconds(200),
///     processBatchAsync: async (_, ct) =&gt;
///     {
///         await RefreshAllAsync(ct);
///     },
///     cancellationToken: appShutdown);
///
/// // Signal that work needs to happen
/// queue.AddWork();
/// </code>
/// </example>
/// <seealso cref="AsyncBatchingWorkQueue{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class AsyncBatchingWorkQueue : AsyncBatchingWorkQueue<AsyncBatchingWorkQueue.VoidResult>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncBatchingWorkQueue" /> class.
    /// </summary>
    /// <param name="delay">The debounce delay.</param>
    /// <param name="processBatchAsync">
    ///     The callback invoked when the debounce timer expires.
    ///     The <see cref="ImmutableArray{T}" /> parameter contains <see cref="VoidResult" /> instances
    ///     (one per <see cref="AddWork" /> call) — typically ignored.
    /// </param>
    /// <param name="cancellationToken">A token to cancel batch processing.</param>
    public AsyncBatchingWorkQueue(
        TimeSpan delay,
        Func<ImmutableArray<VoidResult>, CancellationToken, ValueTask> processBatchAsync,
        CancellationToken cancellationToken)
        : base(delay, processBatchAsync, equalityComparer: null, cancellationToken)
    {
    }

    /// <summary>
    ///     Signals that work is available, restarting the debounce timer.
    /// </summary>
    public void AddWork()
    {
        AddWork(default(VoidResult));
    }

    /// <summary>
    ///     An empty struct used as the type parameter for the non-generic <see cref="AsyncBatchingWorkQueue" />.
    /// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
    public
#else
    internal
#endif
        readonly struct VoidResult
    {
    }
}
