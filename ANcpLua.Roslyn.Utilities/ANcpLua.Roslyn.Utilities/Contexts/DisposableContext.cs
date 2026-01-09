using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities.Contexts;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
sealed class DisposableContext
{
    public INamedTypeSymbol? IDisposable { get; }
    public INamedTypeSymbol? IAsyncDisposable { get; }
    public INamedTypeSymbol? SafeHandle { get; }
    public INamedTypeSymbol? Stream { get; }
    public INamedTypeSymbol? TextReader { get; }
    public INamedTypeSymbol? TextWriter { get; }
    public INamedTypeSymbol? DbConnection { get; }
    public INamedTypeSymbol? DbCommand { get; }
    public INamedTypeSymbol? DbDataReader { get; }
    public INamedTypeSymbol? HttpClient { get; }
    public INamedTypeSymbol? HttpMessageHandler { get; }
    public INamedTypeSymbol? CancellationTokenSource { get; }
    public INamedTypeSymbol? Timer { get; }
    public INamedTypeSymbol? Semaphore { get; }
    public INamedTypeSymbol? SemaphoreSlim { get; }
    public INamedTypeSymbol? Mutex { get; }
    public INamedTypeSymbol? ReaderWriterLock { get; }
    public INamedTypeSymbol? ReaderWriterLockSlim { get; }

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

    public bool IsSyncDisposable(ITypeSymbol? type) =>
        type is not null && IDisposable is not null && type.Implements(IDisposable);

    public bool IsAsyncDisposable(ITypeSymbol? type) =>
        type is not null && IAsyncDisposable is not null && type.Implements(IAsyncDisposable);

    public static bool HasDisposeMethod(ITypeSymbol type)
    {
        foreach (var member in type.GetAllMembers("Dispose"))
        {
            if (member is IMethodSymbol { Parameters.IsEmpty: true, ReturnsVoid: true })
                return true;
        }

        return false;
    }

    public static bool HasDisposeAsyncMethod(ITypeSymbol type)
    {
        foreach (var member in type.GetAllMembers("DisposeAsync"))
        {
            if (member is IMethodSymbol { Parameters.IsEmpty: true })
                return true;
        }

        return false;
    }

    public static bool IsUsingStatement(IOperation operation) =>
        operation is IUsingOperation or IUsingDeclarationOperation;

    public static bool IsInsideUsing(IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is IUsingOperation or IUsingDeclarationOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    public bool IsStream(ITypeSymbol? type) =>
        type is not null && Stream is not null && type.IsOrInheritsFrom(Stream);

    public bool IsTextReaderOrWriter(ITypeSymbol? type) =>
        type is not null &&
        ((TextReader is not null && type.IsOrInheritsFrom(TextReader)) ||
         (TextWriter is not null && type.IsOrInheritsFrom(TextWriter)));

    public bool IsDbConnection(ITypeSymbol? type) =>
        type is not null && DbConnection is not null && type.IsOrInheritsFrom(DbConnection);

    public bool IsDbCommand(ITypeSymbol? type) =>
        type is not null && DbCommand is not null && type.IsOrInheritsFrom(DbCommand);

    public bool IsDbDataReader(ITypeSymbol? type) =>
        type is not null && DbDataReader is not null && type.IsOrInheritsFrom(DbDataReader);

    public bool IsHttpClient(ITypeSymbol? type) =>
        type is not null && HttpClient is not null && type.IsOrInheritsFrom(HttpClient);

    public bool IsSynchronizationPrimitive(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        return (Semaphore is not null && type.IsOrInheritsFrom(Semaphore)) ||
               (SemaphoreSlim is not null && type.IsOrInheritsFrom(SemaphoreSlim)) ||
               (Mutex is not null && type.IsOrInheritsFrom(Mutex)) ||
               (ReaderWriterLock is not null && type.IsOrInheritsFrom(ReaderWriterLock)) ||
               (ReaderWriterLockSlim is not null && type.IsOrInheritsFrom(ReaderWriterLockSlim));
    }

    public bool IsCancellationTokenSource(ITypeSymbol? type) =>
        CancellationTokenSource is not null && type.IsEqualTo(CancellationTokenSource);

    public bool IsSafeHandle(ITypeSymbol? type) =>
        type is not null && SafeHandle is not null && type.IsOrInheritsFrom(SafeHandle);

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
