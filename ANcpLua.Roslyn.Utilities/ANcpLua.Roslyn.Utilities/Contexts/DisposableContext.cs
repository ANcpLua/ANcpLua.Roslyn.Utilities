using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Contexts;

/// <summary>
///     Provides context and utilities for analyzing disposable types and disposal patterns in Roslyn compilations.
///     <para>
///         This context caches well-known disposable-related type symbols from the compilation and provides
///         methods to check whether types implement disposal interfaces or inherit from common disposable base classes.
///     </para>
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 Caches symbols for <see cref="System.IDisposable" />, <c>IAsyncDisposable</c>, and common
///                 disposable types.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Provides type classification methods for streams, database types, synchronization primitives,
///                 and more.
///             </description>
///         </item>
///         <item>
///             <description>Properties may be <c>null</c> if the corresponding type is not available in the compilation.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="AwaitableContext" />
/// <seealso cref="CollectionContext" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class DisposableContext
{
    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.IDisposable" />, or <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? IDisposable { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <c>IAsyncDisposable</c>, or <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? IAsyncDisposable { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Runtime.InteropServices.SafeHandle" />, or
    ///     <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? SafeHandle { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.IO.Stream" />, or <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? Stream { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.IO.TextReader" />, or <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? TextReader { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.IO.TextWriter" />, or <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? TextWriter { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Data.Common.DbConnection" />, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? DbConnection { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Data.Common.DbCommand" />, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? DbCommand { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Data.Common.DbDataReader" />, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? DbDataReader { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Net.Http.HttpClient" />, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? HttpClient { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Net.Http.HttpMessageHandler" />, or <c>null</c> if
    ///     not available.
    /// </summary>
    public INamedTypeSymbol? HttpMessageHandler { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Threading.CancellationTokenSource" />, or
    ///     <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? CancellationTokenSource { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Threading.Timer" />, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? Timer { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Threading.Semaphore" />, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? Semaphore { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Threading.SemaphoreSlim" />, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? SemaphoreSlim { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Threading.Mutex" />, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? Mutex { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Threading.ReaderWriterLock" />, or <c>null</c> if
    ///     not available.
    /// </summary>
    public INamedTypeSymbol? ReaderWriterLock { get; }

    /// <summary>
    ///     Gets the <see cref="INamedTypeSymbol" /> for <see cref="System.Threading.ReaderWriterLockSlim" />, or <c>null</c>
    ///     if not available.
    /// </summary>
    public INamedTypeSymbol? ReaderWriterLockSlim { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DisposableContext" /> class by resolving
    ///     well-known disposable type symbols from the specified compilation.
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation" /> from which to resolve type symbols.</param>
    public DisposableContext(Compilation compilation)
    {
        IDisposable = compilation.GetTypeByMetadataName("System.IDisposable");
        IAsyncDisposable = compilation.GetTypeByMetadataName("System.IAsyncDisposable");
        SafeHandle = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.SafeHandle");
        Stream = compilation.GetTypeByMetadataName("System.IO.Stream");
        TextReader = compilation.GetTypeByMetadataName("System.IO.TextReader");
        TextWriter = compilation.GetTypeByMetadataName("System.IO.TextWriter");
        DbConnection = compilation.GetTypeByMetadataName("System.Data.Common.DbConnection");
        DbCommand = compilation.GetTypeByMetadataName("System.Data.Common.DbCommand");
        DbDataReader = compilation.GetTypeByMetadataName("System.Data.Common.DbDataReader");
        HttpClient = compilation.GetTypeByMetadataName("System.Net.Http.HttpClient");
        HttpMessageHandler = compilation.GetTypeByMetadataName("System.Net.Http.HttpMessageHandler");
        CancellationTokenSource = compilation.GetTypeByMetadataName("System.Threading.CancellationTokenSource");
        Timer = compilation.GetTypeByMetadataName("System.Threading.Timer");
        Semaphore = compilation.GetTypeByMetadataName("System.Threading.Semaphore");
        SemaphoreSlim = compilation.GetTypeByMetadataName("System.Threading.SemaphoreSlim");
        Mutex = compilation.GetTypeByMetadataName("System.Threading.Mutex");
        ReaderWriterLock = compilation.GetTypeByMetadataName("System.Threading.ReaderWriterLock");
        ReaderWriterLockSlim = compilation.GetTypeByMetadataName("System.Threading.ReaderWriterLockSlim");
    }

    /// <summary>
    ///     Determines whether the specified type implements <see cref="System.IDisposable" /> or <c>IAsyncDisposable</c>.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> implements either <see cref="System.IDisposable" /> or
    ///     <c>IAsyncDisposable</c>; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsSyncDisposable" />
    /// <seealso cref="IsAsyncDisposable" />
    public bool IsDisposable(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (IDisposable is not null && type.Implements(IDisposable))
            return true;

        if (IAsyncDisposable is not null && type.Implements(IAsyncDisposable))
            return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type implements <see cref="System.IDisposable" />.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> implements <see cref="System.IDisposable" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsDisposable" />
    /// <seealso cref="IsAsyncDisposable" />
    public bool IsSyncDisposable(ITypeSymbol? type) => type is not null && IDisposable is not null && type.Implements(IDisposable);

    /// <summary>
    ///     Determines whether the specified type implements <c>IAsyncDisposable</c>.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> implements <c>IAsyncDisposable</c>; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsDisposable" />
    /// <seealso cref="IsSyncDisposable" />
    public bool IsAsyncDisposable(ITypeSymbol? type) => type is not null && IAsyncDisposable is not null && type.Implements(IAsyncDisposable);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <see cref="System.IO.Stream" />.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is or inherits from <see cref="System.IO.Stream" />; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public bool IsStream(ITypeSymbol? type) => type is not null && Stream is not null && type.IsOrInheritsFrom(Stream);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <see cref="System.IO.TextReader" /> or
    ///     <see cref="System.IO.TextWriter" />.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is or inherits from <see cref="System.IO.TextReader" /> or
    ///     <see cref="System.IO.TextWriter" />; otherwise, <c>false</c>.
    /// </returns>
    public bool IsTextReaderOrWriter(ITypeSymbol? type) =>
        type is not null &&
        (TextReader is not null && type.IsOrInheritsFrom(TextReader) ||
         TextWriter is not null && type.IsOrInheritsFrom(TextWriter));

    /// <summary>
    ///     Determines whether the specified type is or inherits from <see cref="System.Data.Common.DbConnection" />.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is or inherits from <see cref="System.Data.Common.DbConnection" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsDbCommand" />
    /// <seealso cref="IsDbDataReader" />
    public bool IsDbConnection(ITypeSymbol? type) => type is not null && DbConnection is not null && type.IsOrInheritsFrom(DbConnection);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <see cref="System.Data.Common.DbCommand" />.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is or inherits from <see cref="System.Data.Common.DbCommand" />; otherwise,
    ///     <c>false</c>.
    /// </returns>
    /// <seealso cref="IsDbConnection" />
    /// <seealso cref="IsDbDataReader" />
    public bool IsDbCommand(ITypeSymbol? type) => type is not null && DbCommand is not null && type.IsOrInheritsFrom(DbCommand);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <see cref="System.Data.Common.DbDataReader" />.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is or inherits from <see cref="System.Data.Common.DbDataReader" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsDbConnection" />
    /// <seealso cref="IsDbCommand" />
    public bool IsDbDataReader(ITypeSymbol? type) => type is not null && DbDataReader is not null && type.IsOrInheritsFrom(DbDataReader);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <see cref="System.Net.Http.HttpClient" />.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is or inherits from <see cref="System.Net.Http.HttpClient" />; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public bool IsHttpClient(ITypeSymbol? type) => type is not null && HttpClient is not null && type.IsOrInheritsFrom(HttpClient);

    /// <summary>
    ///     Determines whether the specified type is a synchronization primitive
    ///     (Semaphore, SemaphoreSlim, Mutex, ReaderWriterLock, or ReaderWriterLockSlim).
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is or inherits from a synchronization primitive type; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public bool IsSynchronizationPrimitive(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        return Semaphore is not null && type.IsOrInheritsFrom(Semaphore) ||
               SemaphoreSlim is not null && type.IsOrInheritsFrom(SemaphoreSlim) ||
               Mutex is not null && type.IsOrInheritsFrom(Mutex) ||
               ReaderWriterLock is not null && type.IsOrInheritsFrom(ReaderWriterLock) ||
               ReaderWriterLockSlim is not null && type.IsOrInheritsFrom(ReaderWriterLockSlim);
    }

    /// <summary>
    ///     Determines whether the specified type is exactly <see cref="System.Threading.CancellationTokenSource" />.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> equals <see cref="System.Threading.CancellationTokenSource" />; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public bool IsCancellationTokenSource(ITypeSymbol? type) => CancellationTokenSource is not null && type.IsEqualTo(CancellationTokenSource);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <see cref="System.Runtime.InteropServices.SafeHandle" />.
    /// </summary>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is or inherits from
    ///     <see cref="System.Runtime.InteropServices.SafeHandle" />; otherwise, <c>false</c>.
    /// </returns>
    public bool IsSafeHandle(ITypeSymbol? type) => type is not null && SafeHandle is not null && type.IsOrInheritsFrom(SafeHandle);

    /// <summary>
    ///     Determines whether the specified type is disposable and should typically be disposed by the caller.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <strong>This method provides opinionated guidance based on common patterns.</strong>
    ///         It returns <c>false</c> for types typically managed by dependency injection or pooling:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <see cref="System.Net.Http.HttpClient" /> - Usually injected via <c>IHttpClientFactory</c>
    ///                 and managed by the DI container's lifetime.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Override this logic in your analyzer if your codebase has different conventions.
    ///     </para>
    /// </remarks>
    /// <param name="type">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is disposable and should typically be disposed;
    ///     <c>false</c> if it is not disposable or is typically managed externally.
    /// </returns>
    /// <seealso cref="IsDisposable" />
    /// <seealso cref="IsHttpClient" />
    public bool ShouldBeDisposed(ITypeSymbol? type)
    {
        if (!IsDisposable(type))
            return false;

        // These are typically managed by DI or pooling
        if (IsHttpClient(type))
            return false; // Usually injected via IHttpClientFactory

        return true;
    }
}
