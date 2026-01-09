using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Contexts;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
sealed class AwaitableContext
{
    private readonly INamedTypeSymbol?[] _taskLikeSymbols;

    public INamedTypeSymbol? Task { get; }
    public INamedTypeSymbol? TaskOfT { get; }
    public INamedTypeSymbol? ValueTask { get; }
    public INamedTypeSymbol? ValueTaskOfT { get; }
    public INamedTypeSymbol? IAsyncEnumerable { get; }
    public INamedTypeSymbol? IAsyncEnumerator { get; }
    public INamedTypeSymbol? INotifyCompletion { get; }
    public INamedTypeSymbol? ICriticalNotifyCompletion { get; }
    public INamedTypeSymbol? AsyncMethodBuilderAttribute { get; }
    public INamedTypeSymbol? ConfiguredTaskAwaitable { get; }
    public INamedTypeSymbol? ConfiguredValueTaskAwaitable { get; }

    public AwaitableContext(Compilation compilation)
    {
        Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        ValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        ValueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        IAsyncEnumerable = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        IAsyncEnumerator = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1");
        INotifyCompletion = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.INotifyCompletion");
        ICriticalNotifyCompletion = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ICriticalNotifyCompletion");
        AsyncMethodBuilderAttribute = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");
        ConfiguredTaskAwaitable = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
        ConfiguredValueTaskAwaitable = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable");

        _taskLikeSymbols = [Task, TaskOfT, ValueTask, ValueTaskOfT];
    }

    public bool IsTaskLike(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        var original = type is INamedTypeSymbol named ? named.OriginalDefinition : type;
        foreach (var taskLike in _taskLikeSymbols)
        {
            if (taskLike is not null && SymbolEqualityComparer.Default.Equals(original, taskLike))
                return true;
        }

        return false;
    }

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
        {
            if (member is IMethodSymbol { Parameters.IsEmpty: true, ReturnsVoid: false } method)
            {
                if (ConformsToAwaiterPattern(method.ReturnType))
                    return true;
            }
        }

        return false;
    }

    public bool IsAwaitable(ITypeSymbol? type, SemanticModel semanticModel, int position)
    {
        if (type is null || INotifyCompletion is null)
            return false;

        if (type.SpecialType is SpecialType.System_Void || type.TypeKind is TypeKind.Dynamic)
            return false;

        if (IsTaskLike(type))
            return true;

        // Check for GetAwaiter method including extension methods
        foreach (var symbol in semanticModel.LookupSymbols(position, type, "GetAwaiter", includeReducedExtensionMethods: true))
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
            if (member is IMethodSymbol { Name: "GetResult", Parameters.IsEmpty: true, TypeParameters.IsEmpty: true, IsStatic: false })
                hasGetResult = true;
            else if (member is IPropertySymbol { Name: "IsCompleted", IsStatic: false, Type.SpecialType: SpecialType.System_Boolean, GetMethod: not null })
                hasIsCompleted = true;

            if (hasGetResult && hasIsCompleted)
                return true;
        }

        return false;
    }

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
            if (IAsyncEnumerable is not null && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, IAsyncEnumerable))
                return true;
            if (IAsyncEnumerator is not null && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, IAsyncEnumerator))
                return true;
        }

        if (AsyncMethodBuilderAttribute is not null && method.ReturnType.HasAttribute(AsyncMethodBuilderAttribute))
            return true;

        return false;
    }

    public bool IsAsyncEnumerable(ITypeSymbol? type)
    {
        if (type is null || IAsyncEnumerable is null)
            return false;

        if (type is INamedTypeSymbol named)
            return SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, IAsyncEnumerable);

        return false;
    }

    public bool IsAsyncEnumerator(ITypeSymbol? type)
    {
        if (type is null || IAsyncEnumerator is null)
            return false;

        if (type is INamedTypeSymbol named)
            return SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, IAsyncEnumerator);

        return false;
    }

    public bool IsConfiguredAwaitable(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        var original = type is INamedTypeSymbol named ? named.OriginalDefinition : type;
        return (ConfiguredTaskAwaitable is not null && SymbolEqualityComparer.Default.Equals(original, ConfiguredTaskAwaitable)) ||
               (ConfiguredValueTaskAwaitable is not null && SymbolEqualityComparer.Default.Equals(original, ConfiguredValueTaskAwaitable));
    }

    public ITypeSymbol? GetTaskResultType(ITypeSymbol? taskType)
    {
        if (taskType is not INamedTypeSymbol named)
            return null;

        if (named.TypeArguments.Length != 1)
            return null;

        var original = named.OriginalDefinition;
        if ((TaskOfT is not null && SymbolEqualityComparer.Default.Equals(original, TaskOfT)) ||
            (ValueTaskOfT is not null && SymbolEqualityComparer.Default.Equals(original, ValueTaskOfT)))
        {
            return named.TypeArguments[0];
        }

        return null;
    }
}
