namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Manages a series of <see cref="CancellationToken" /> values where creating a new token
///     automatically cancels the previous one ("latest wins" pattern).
/// </summary>
/// <remarks>
///     <para>
///         This is useful in single-threaded pipeline scenarios where a new request should
///         cancel any in-progress work from the previous request. For example, when a user types
///         in a search box, each keystroke creates a new token that cancels the previous search.
///     </para>
///     <para>
///         An optional "super token" can be provided at construction time. When the super token
///         is cancelled, all derived tokens are also cancelled regardless of whether
///         <see cref="CreateNext" /> has been called.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 This type is NOT thread-safe. It is designed for single-threaded pipeline usage.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Call <see cref="Dispose" /> to cancel the current token and release resources.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// using var series = new CancellationSeries();
/// 
/// // First request
/// var ct1 = series.CreateNext();
/// await ProcessAsync(ct1);  // may be cancelled when...
/// 
/// // Second request arrives — ct1 is now cancelled
/// var ct2 = series.CreateNext();
/// await ProcessAsync(ct2);
/// 
/// // With a super token (e.g., application shutdown)
/// using var series2 = new CancellationSeries(appShutdownToken);
/// var ct = series2.CreateNext();
/// // ct is cancelled when either CreateNext() is called again OR appShutdownToken fires
/// </code>
/// </example>
/// <seealso cref="CancellationToken" />
/// <seealso cref="CancellationTokenSource" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class CancellationSeries : IDisposable
{
    private readonly CancellationToken _superToken;
    private CancellationTokenSource? _currentCts;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CancellationSeries" /> class.
    /// </summary>
    /// <param name="superToken">
    ///     An optional token that, when cancelled, also cancels any token created by <see cref="CreateNext" />.
    ///     Defaults to <see cref="CancellationToken.None" />.
    /// </param>
    public CancellationSeries(CancellationToken superToken = default)
    {
        _superToken = superToken;
    }

    /// <summary>
    ///     Gets a value indicating whether there is an active (non-cancelled) token from a previous
    ///     <see cref="CreateNext" /> call.
    /// </summary>
    public bool HasActiveToken => _currentCts is { IsCancellationRequested: false };

    /// <summary>
    ///     Creates a new <see cref="CancellationToken" />, cancelling and disposing the previously created one.
    /// </summary>
    /// <param name="token">
    ///     An optional additional token to link with the new cancellation source.
    ///     The returned token will be cancelled if this token, the super token, or a subsequent
    ///     <see cref="CreateNext" /> call triggers cancellation.
    /// </param>
    /// <returns>
    ///     A new <see cref="CancellationToken" /> that will be cancelled on the next call to <see cref="CreateNext" /> or
    ///     <see cref="Dispose" />.
    /// </returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public CancellationToken CreateNext(CancellationToken token = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(CancellationSeries));

        // Cancel and dispose the previous CTS
        var previousCts = _currentCts;
        if (previousCts != null)
        {
            previousCts.Cancel();
            previousCts.Dispose();
        }

        // Create a new linked CTS
        var newCts = token.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(_superToken, token)
            : _superToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(_superToken)
                : new CancellationTokenSource();

        _currentCts = newCts;
        return newCts.Token;
    }

    /// <summary>
    ///     Cancels the current token (if any) and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        var cts = _currentCts;
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            _currentCts = null;
        }
    }
}