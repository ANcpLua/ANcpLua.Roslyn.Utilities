using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class TypeSymbolExtensions
{
    // Original-definition display strings for well-known generic framework types.
    // Centralising these sets makes the predicates one hashset lookup each.
    private static readonly HashSet<string> s_spanOriginalDefinitions =
    [
        "System.Span<T>",
        "System.ReadOnlySpan<T>"
    ];

    private static readonly HashSet<string> s_memoryOriginalDefinitions =
    [
        "System.Memory<T>",
        "System.ReadOnlyMemory<T>"
    ];

    private static readonly HashSet<string> s_taskOriginalDefinitions =
    [
        "System.Threading.Tasks.Task",
        "System.Threading.Tasks.Task<TResult>",
        "System.Threading.Tasks.ValueTask",
        "System.Threading.Tasks.ValueTask<TResult>"
    ];

    private static readonly HashSet<string> s_genericTaskOriginalDefinitions =
    [
        "System.Threading.Tasks.Task<TResult>",
        "System.Threading.Tasks.ValueTask<TResult>"
    ];

    private static readonly HashSet<string> s_elementTypeOriginalDefinitions =
    [
        "System.Span<T>",
        "System.ReadOnlySpan<T>",
        "System.Memory<T>",
        "System.ReadOnlyMemory<T>",
        "System.Collections.Generic.IEnumerable<T>"
    ];

    /// <summary>
    ///     Determines whether the type symbol matches any of the supplied <c>OriginalDefinition</c> display strings.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="originalDefinitions">Display strings of <c>OriginalDefinition</c> (e.g. <c>"System.Span&lt;T&gt;"</c>).</param>
    /// <returns><c>true</c> if the symbol is an <see cref="INamedTypeSymbol" /> whose original definition matches a string in the set.</returns>
    /// <remarks>
    ///     Single chokepoint used by <see cref="IsSpanType" />, <see cref="IsMemoryType" />, and <see cref="IsTaskType" />;
    ///     keeps each predicate at cyclomatic complexity 1.
    /// </remarks>
    private static bool IsKnownNamedType(this ITypeSymbol? symbol, HashSet<string> originalDefinitions)
    {
        return symbol is INamedTypeSymbol named
               && originalDefinitions.Contains(named.OriginalDefinition.ToDisplayString());
    }

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="Span{T}" /> or <see cref="ReadOnlySpan{T}" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is a span type; otherwise, <c>false</c>.</returns>
    /// <seealso cref="IsMemoryType" />
    /// <seealso cref="GetElementType" />
    public static bool IsSpanType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        return symbol.IsKnownNamedType(s_spanOriginalDefinitions);
    }

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="Memory{T}" /> or <see cref="ReadOnlyMemory{T}" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is a memory type; otherwise, <c>false</c>.</returns>
    /// <seealso cref="IsSpanType" />
    /// <seealso cref="GetElementType" />
    public static bool IsMemoryType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        return symbol.IsKnownNamedType(s_memoryOriginalDefinitions);
    }

    /// <summary>
    ///     Determines whether the type symbol represents a task type.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is a task type; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>Task types include <c>Task</c>, <c>Task&lt;T&gt;</c>, <c>ValueTask</c>, and <c>ValueTask&lt;T&gt;</c>.</para>
    /// </remarks>
    public static bool IsTaskType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        // Generic task types funnel through the standard named-type check; for the
        // non-generic Task / ValueTask, fall back to the symbol's own display string.
        return symbol.IsKnownNamedType(s_taskOriginalDefinitions)
               || (symbol is not null
                   && symbol is not INamedTypeSymbol
                   && s_taskOriginalDefinitions.Contains(symbol.ToDisplayString()));
    }

    /// <summary>
    ///     Gets the result type from a generic <c>Task&lt;T&gt;</c> or <c>ValueTask&lt;T&gt;</c> type symbol.
    /// </summary>
    /// <param name="type">The type symbol to extract the result type from.</param>
    /// <returns>
    ///     The <c>TResult</c> type argument if <paramref name="type" /> is <c>Task&lt;T&gt;</c>
    ///     or <c>ValueTask&lt;T&gt;</c>; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This is a context-free alternative to <c>AwaitableContext.GetTaskResultType()</c>
    ///         that uses string matching instead of cached type symbols. Useful in source generators
    ///         that don't create an <c>AwaitableContext</c>.
    ///     </para>
    ///     <para>
    ///         Returns <c>null</c> for non-generic <c>Task</c>, non-generic <c>ValueTask</c>,
    ///         and all non-task types.
    ///     </para>
    /// </remarks>
    public static ITypeSymbol? GetTaskResultType(this ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } named)
            return null;

        return s_genericTaskOriginalDefinitions.Contains(named.OriginalDefinition.ToDisplayString())
            ? named.TypeArguments[0]
            : null;
    }

    /// <summary>
    ///     Determines whether the type symbol represents an enumerable type.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is <see cref="System.Collections.IEnumerable" />
    ///     or <see cref="System.Collections.Generic.IEnumerable{T}" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="GetElementType" />
    public static bool IsEnumerableType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        if (symbol.SpecialType is SpecialType.System_Collections_IEnumerable)
            return true;

        return symbol is INamedTypeSymbol namedType
               && namedType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T;
    }

    /// <summary>
    ///     Gets the element type of a collection or span-like type.
    /// </summary>
    /// <param name="symbol">The type symbol to get the element type from, or <c>null</c>.</param>
    /// <returns>
    ///     The element type if <paramref name="symbol" /> is an array, span, memory, or generic enumerable;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>This method extracts element types from arrays, <c>Span&lt;T&gt;</c>, <c>ReadOnlySpan&lt;T&gt;</c>,
    ///     <c>Memory&lt;T&gt;</c>, <c>ReadOnlyMemory&lt;T&gt;</c>, and <see cref="System.Collections.Generic.IEnumerable{T}" />.</para>
    /// </remarks>
    /// <seealso cref="IsSpanType" />
    /// <seealso cref="IsMemoryType" />
    /// <seealso cref="IsEnumerableType" />
    public static ITypeSymbol? GetElementType(this ITypeSymbol? symbol)
    {
        if (symbol is IArrayTypeSymbol arrayType)
            return arrayType.ElementType;

        if (symbol is not INamedTypeSymbol { TypeArguments.Length: 1 } namedType)
            return null;

        return s_elementTypeOriginalDefinitions.Contains(namedType.OriginalDefinition.ToDisplayString())
            ? namedType.TypeArguments[0]
            : null;
    }
}
