using System.Threading;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods that provide lock-scoped <see cref="IDisposable" /> helpers for <see cref="ReaderWriterLockSlim" />.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ReaderWriterLockSlimExtensions
{
    /// <summary>
    ///     Enters a read lock and returns a disposable that exits it on dispose.
    /// </summary>
    /// <param name="lockObject">The lock instance.</param>
    /// <returns>A disposable that exits the read lock when disposed.</returns>
    public static ReaderLockDisposable WithReaderLock(this ReaderWriterLockSlim lockObject)
    {
        Guard.NotNull(lockObject);
        lockObject.EnterReadLock();
        return new ReaderLockDisposable(lockObject);
    }

    /// <summary>
    ///     Enters an upgradeable read lock and returns a disposable that exits it on dispose.
    /// </summary>
    /// <param name="lockObject">The lock instance.</param>
    /// <returns>A disposable that exits the upgradeable read lock when disposed.</returns>
    public static UpgradeableReaderLockDisposable WithUpgradeableReaderLock(this ReaderWriterLockSlim lockObject)
    {
        Guard.NotNull(lockObject);
        lockObject.EnterUpgradeableReadLock();
        return new UpgradeableReaderLockDisposable(lockObject);
    }

    /// <summary>
    ///     Enters a write lock and returns a disposable that exits it on dispose.
    /// </summary>
    /// <param name="lockObject">The lock instance.</param>
    /// <returns>A disposable that exits the write lock when disposed.</returns>
    public static WriterLockDisposable WithWriterLock(this ReaderWriterLockSlim lockObject)
    {
        Guard.NotNull(lockObject);
        lockObject.EnterWriteLock();
        return new WriterLockDisposable(lockObject);
    }
}

/// <summary>
///     Disposable scope guard that exits <see cref="ReaderWriterLockSlim" /> read lock once.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class ReaderLockDisposable : IDisposable
{
    private ReaderWriterLockSlim? _lockObject;

    internal ReaderLockDisposable(ReaderWriterLockSlim lockObject)
    {
        _lockObject = lockObject;
    }

    /// <summary>
    ///     Exits the read lock if it has not already been exited.
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _lockObject, null)?.ExitReadLock();
    }
}

/// <summary>
///     Disposable scope guard that exits <see cref="ReaderWriterLockSlim" /> upgradeable read lock once.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class UpgradeableReaderLockDisposable : IDisposable
{
    private ReaderWriterLockSlim? _lockObject;

    internal UpgradeableReaderLockDisposable(ReaderWriterLockSlim lockObject)
    {
        _lockObject = lockObject;
    }

    /// <summary>
    ///     Exits the upgradeable read lock if it has not already been exited.
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _lockObject, null)?.ExitUpgradeableReadLock();
    }
}

/// <summary>
///     Disposable scope guard that exits <see cref="ReaderWriterLockSlim" /> write lock once.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class WriterLockDisposable : IDisposable
{
    private ReaderWriterLockSlim? _lockObject;

    internal WriterLockDisposable(ReaderWriterLockSlim lockObject)
    {
        _lockObject = lockObject;
    }

    /// <summary>
    ///     Exits the write lock if it has not already been exited.
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _lockObject, null)?.ExitWriteLock();
    }
}
