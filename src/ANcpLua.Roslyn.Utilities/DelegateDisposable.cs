using System.Threading;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     An <see cref="IDisposable" /> that runs a dispose callback at most once.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class DelegateDisposable : IDisposable
{
    private Action? _disposeAction;

    /// <summary>
    ///     Initializes a new instance using the given <paramref name="dispose" /> callback.
    /// </summary>
    /// <param name="dispose">The callback executed when disposed.</param>
    public DelegateDisposable(Action dispose)
    {
        Guard.NotNull(dispose);
        _disposeAction = dispose;
    }

    /// <summary>
    ///     Executes and clears the stored dispose callback.
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
    }

    /// <summary>
    ///     Executes <paramref name="setup" /> and returns a disposable that runs <paramref name="dispose" /> once.
    /// </summary>
    /// <param name="setup">The setup action to run before returning the disposable.</param>
    /// <param name="dispose">The dispose callback to run when the returned instance is disposed.</param>
    /// <returns>A disposable invoking <paramref name="dispose" /> exactly once.</returns>
    public static DelegateDisposable Create(Action setup, Action dispose)
    {
        Guard.NotNull(setup);
        Guard.NotNull(dispose);
        setup();
        return new DelegateDisposable(dispose);
    }

    /// <summary>
    ///     Executes <paramref name="setup" /> and returns a disposable that captures the state and runs
    ///     <paramref name="dispose" /> once.
    /// </summary>
    /// <typeparam name="TState">Type of state produced by <paramref name="setup" />.</typeparam>
    /// <param name="setup">The setup function producing a state value.</param>
    /// <param name="dispose">The dispose callback to run when the returned instance is disposed.</param>
    /// <returns>A disposable invoking <paramref name="dispose" /> with the state exactly once.</returns>
    public static DelegateDisposable Create<TState>(Func<TState> setup, Action<TState> dispose)
    {
        Guard.NotNull(setup);
        Guard.NotNull(dispose);
        var state = setup();
        return new DelegateDisposable(() => dispose(state));
    }
}
