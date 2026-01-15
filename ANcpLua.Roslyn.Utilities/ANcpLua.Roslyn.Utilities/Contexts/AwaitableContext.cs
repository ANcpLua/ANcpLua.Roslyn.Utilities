using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Contexts;

/// <summary>
///     Provides context for analyzing awaitable types and async patterns in a <see cref="Compilation" />.
///     <para>
///         This class caches common task-like type symbols and provides methods to determine whether
///         types are awaitable, conform to the awaiter pattern, or can be used with the <c>async</c> keyword.
///     </para>
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             Supports <see cref="System.Threading.Tasks.Task" />, <see cref="System.Threading.Tasks.Task{TResult}" />,
///             <see cref="System.Threading.Tasks.ValueTask" />, and
///             <see cref="System.Threading.Tasks.ValueTask{TResult}" />.
///         </item>
///         <item>Detects custom awaitable types by checking for the awaiter pattern.</item>
///         <item>Supports extension method <c>GetAwaiter</c> when provided with a <see cref="SemanticModel" />.</item>
///         <item>Properties may be <c>null</c> if the corresponding types are not available in the compilation.</item>
///     </list>
/// </remarks>
/// <seealso cref="DisposableContext" />
/// <seealso cref="CollectionContext" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class AwaitableContext
{
    private readonly INamedTypeSymbol?[] _taskLikeSymbols;

    /// <summary>
    ///     Gets the <see cref="System.Threading.Tasks.Task" /> type symbol, or <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? Task { get; }

    /// <summary>
    ///     Gets the <see cref="System.Threading.Tasks.Task{TResult}" /> generic type definition, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? TaskOfT { get; }

    /// <summary>
    ///     Gets the <see cref="System.Threading.Tasks.ValueTask" /> type symbol, or <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? ValueTask { get; }

    /// <summary>
    ///     Gets the <see cref="System.Threading.Tasks.ValueTask{TResult}" /> generic type definition, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? ValueTaskOfT { get; }

    /// <summary>
    ///     Gets the <c>IAsyncEnumerable&lt;T&gt;</c> generic type definition, or <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? IAsyncEnumerable { get; }

    /// <summary>
    ///     Gets the <c>IAsyncEnumerator&lt;T&gt;</c> generic type definition, or <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? IAsyncEnumerator { get; }

    /// <summary>
    ///     Gets the <see cref="System.Runtime.CompilerServices.INotifyCompletion" /> interface type symbol, or <c>null</c> if
    ///     not available.
    /// </summary>
    public INamedTypeSymbol? INotifyCompletion { get; }

    /// <summary>
    ///     Gets the <see cref="System.Runtime.CompilerServices.ICriticalNotifyCompletion" /> interface type symbol, or
    ///     <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? ICriticalNotifyCompletion { get; }

    /// <summary>
    ///     Gets the <see cref="System.Runtime.CompilerServices.AsyncMethodBuilderAttribute" /> type symbol, or <c>null</c> if
    ///     not available.
    /// </summary>
    public INamedTypeSymbol? AsyncMethodBuilderAttribute { get; }

    /// <summary>
    ///     Gets the <see cref="System.Runtime.CompilerServices.ConfiguredTaskAwaitable" /> type symbol, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? ConfiguredTaskAwaitable { get; }

    /// <summary>
    ///     Gets the <see cref="System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable" /> type symbol, or <c>null</c> if
    ///     not available.
    /// </summary>
    public INamedTypeSymbol? ConfiguredValueTaskAwaitable { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AwaitableContext" /> class.
    /// </summary>
    /// <param name="compilation">The compilation from which to resolve awaitable type symbols.</param>
    public AwaitableContext(Compilation compilation)
    {
        Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        ValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        ValueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        IAsyncEnumerable = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        IAsyncEnumerator = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1");
        INotifyCompletion = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.INotifyCompletion");
        ICriticalNotifyCompletion =
            compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ICriticalNotifyCompletion");
        AsyncMethodBuilderAttribute =
            compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");
        ConfiguredTaskAwaitable =
            compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
        ConfiguredValueTaskAwaitable =
            compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable");

        _taskLikeSymbols = [Task, TaskOfT, ValueTask, ValueTaskOfT];
    }

    /// <summary>
    ///     Determines whether the specified type is a task-like type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is <see cref="Task" />, <see cref="TaskOfT" />,
    ///     <see cref="ValueTask" />, or <see cref="ValueTaskOfT" />; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     For generic types, this method compares against the original definition (e.g., <c>Task&lt;T&gt;</c>
    ///     rather than <c>Task&lt;int&gt;</c>).
    /// </remarks>
    public bool IsTaskLike(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        var original = type is INamedTypeSymbol named ? named.OriginalDefinition : type;
        foreach (var taskLike in _taskLikeSymbols)
            if (taskLike is not null && SymbolEqualityComparer.Default.Equals(original, taskLike))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type is awaitable by checking for a valid <c>GetAwaiter</c> method.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is awaitable; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method only checks for instance methods named <c>GetAwaiter</c>. To include extension methods,
    ///         use the overload that accepts a <see cref="SemanticModel" />.
    ///     </para>
    ///     <para>
    ///         A type is awaitable if it is task-like or has a parameterless <c>GetAwaiter</c> method returning
    ///         a type that conforms to the awaiter pattern.
    ///     </para>
    /// </remarks>
    /// <seealso cref="IsAwaitable(ITypeSymbol?, SemanticModel, int)" />
    /// <seealso cref="ConformsToAwaiterPattern" />
    public bool IsAwaitable(ITypeSymbol? type)
    {
        if (type is null || INotifyCompletion is null)
            return false;

        if (type.SpecialType is SpecialType.System_Void || type.TypeKind is TypeKind.Dynamic)
            return false;

        if (IsTaskLike(type))
            return true;

        // Check for GetAwaiter method
        foreach (var member in type.GetMembers("GetAwaiter"))
            if (member is IMethodSymbol { Parameters.IsEmpty: true, ReturnsVoid: false } method)
                if (ConformsToAwaiterPattern(method.ReturnType))
                    return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type is awaitable, including extension methods visible at the given position.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <param name="semanticModel">The semantic model used to look up extension methods.</param>
    /// <param name="position">The position in the source at which to evaluate accessible members.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is awaitable; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Unlike the simpler overload, this method considers extension methods for <c>GetAwaiter</c>
    ///         that are accessible at the specified <paramref name="position" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="IsAwaitable(ITypeSymbol?)" />
    /// <seealso cref="ConformsToAwaiterPattern" />
    public bool IsAwaitable(ITypeSymbol? type, SemanticModel semanticModel, int position)
    {
        if (type is null || INotifyCompletion is null)
            return false;

        if (type.SpecialType is SpecialType.System_Void || type.TypeKind is TypeKind.Dynamic)
            return false;

        if (IsTaskLike(type))
            return true;

        // Check for GetAwaiter method including extension methods
        foreach (var symbol in semanticModel.LookupSymbols(position, type, "GetAwaiter", true))
        {
            if (symbol is not IMethodSymbol method)
                continue;

            if (!method.Parameters.IsEmpty)
                continue;

            if (!semanticModel.IsAccessible(position, method))
                continue;

            if (ConformsToAwaiterPattern(method.ReturnType))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type conforms to the awaiter pattern.
    /// </summary>
    /// <param name="awaiterType">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="awaiterType" /> implements <see cref="INotifyCompletion" />
    ///     and has both an <c>IsCompleted</c> boolean property and a <c>GetResult</c> method; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The awaiter pattern requires:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>The type implements <see cref="System.Runtime.CompilerServices.INotifyCompletion" />.</item>
    ///         <item>An instance property <c>IsCompleted</c> of type <see cref="bool" /> with a getter.</item>
    ///         <item>An instance method <c>GetResult()</c> with no parameters or type parameters.</item>
    ///     </list>
    /// </remarks>
    public bool ConformsToAwaiterPattern(ITypeSymbol? awaiterType)
    {
        if (awaiterType is null || INotifyCompletion is null)
            return false;

        if (!awaiterType.Implements(INotifyCompletion))
            return false;

        var hasGetResult = false;
        var hasIsCompleted = false;

        foreach (var member in awaiterType.GetMembers())
        {
            if (member is IMethodSymbol
                {
                    Name: "GetResult", Parameters.IsEmpty: true, TypeParameters.IsEmpty: true, IsStatic: false
                })
                hasGetResult = true;
            else if (member is IPropertySymbol
                     {
                         Name: "IsCompleted", IsStatic: false, Type.SpecialType: SpecialType.System_Boolean,
                         GetMethod: not null
                     })
                hasIsCompleted = true;

            if (hasGetResult && hasIsCompleted)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether the specified method can use the <c>async</c> modifier.
    /// </summary>
    /// <param name="method">The method symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="method" /> is already async, returns a task-like type,
    ///     returns <see cref="IAsyncEnumerable" />, returns <see cref="IAsyncEnumerator" />,
    ///     or returns a type decorated with <see cref="AsyncMethodBuilderAttribute" />; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method determines whether a method declaration could validly include the <c>async</c> keyword
    ///         based on its return type. Methods returning <c>void</c> cannot use <c>async</c> (except for event handlers,
    ///         which are not distinguished here).
    ///     </para>
    /// </remarks>
    public bool CanUseAsyncKeyword(IMethodSymbol method)
    {
        if (method.IsAsync)
            return true;

        if (method.ReturnsVoid)
            return false;

        if (IsTaskLike(method.ReturnType))
            return true;

        if (method.ReturnType is INamedTypeSymbol namedType)
        {
            if (IAsyncEnumerable is not null &&
                SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, IAsyncEnumerable))
                return true;
            if (IAsyncEnumerator is not null &&
                SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, IAsyncEnumerator))
                return true;
        }

        if (AsyncMethodBuilderAttribute is not null && method.ReturnType.HasAttribute(AsyncMethodBuilderAttribute))
            return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type is <c>IAsyncEnumerable&lt;T&gt;</c>.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is the <c>IAsyncEnumerable&lt;T&gt;</c>
    ///     type; otherwise, <c>false</c>.
    /// </returns>
    public bool IsAsyncEnumerable(ITypeSymbol? type)
    {
        if (type is null || IAsyncEnumerable is null)
            return false;

        if (type is INamedTypeSymbol named)
            return SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, IAsyncEnumerable);

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type is <c>IAsyncEnumerator&lt;T&gt;</c>.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is the <c>IAsyncEnumerator&lt;T&gt;</c>
    ///     type; otherwise, <c>false</c>.
    /// </returns>
    public bool IsAsyncEnumerator(ITypeSymbol? type)
    {
        if (type is null || IAsyncEnumerator is null)
            return false;

        if (type is INamedTypeSymbol named)
            return SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, IAsyncEnumerator);

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type is a configured awaitable type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is <see cref="ConfiguredTaskAwaitable" />
    ///     or <see cref="ConfiguredValueTaskAwaitable" />; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Configured awaitables are returned by <c>ConfigureAwait(bool)</c> method calls on task-like types.
    /// </remarks>
    public bool IsConfiguredAwaitable(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        var original = type is INamedTypeSymbol named ? named.OriginalDefinition : type;
        return (ConfiguredTaskAwaitable is not null &&
                SymbolEqualityComparer.Default.Equals(original, ConfiguredTaskAwaitable)) ||
               (ConfiguredValueTaskAwaitable is not null &&
                SymbolEqualityComparer.Default.Equals(original, ConfiguredValueTaskAwaitable));
    }

    /// <summary>
    ///     Gets the result type argument from a generic task type.
    /// </summary>
    /// <param name="taskType">The task type symbol to inspect.</param>
    /// <returns>
    ///     The type argument <c>T</c> if <paramref name="taskType" /> is <c>Task&lt;T&gt;</c> or <c>ValueTask&lt;T&gt;</c>;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     Returns <c>null</c> for non-generic <see cref="Task" /> or <see cref="ValueTask" />,
    ///     as well as for types that are not task-like.
    /// </remarks>
    public ITypeSymbol? GetTaskResultType(ITypeSymbol? taskType)
    {
        if (taskType is not INamedTypeSymbol named)
            return null;

        if (named.TypeArguments.Length != 1)
            return null;

        var original = named.OriginalDefinition;
        if ((TaskOfT is not null && SymbolEqualityComparer.Default.Equals(original, TaskOfT)) ||
            (ValueTaskOfT is not null && SymbolEqualityComparer.Default.Equals(original, ValueTaskOfT)))
            return named.TypeArguments[0];

        return null;
    }
}