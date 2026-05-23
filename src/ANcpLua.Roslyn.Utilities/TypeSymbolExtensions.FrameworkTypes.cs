using System;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class TypeSymbolExtensions
{
    /// <summary>
    ///     Determines whether the type symbol matches a namespace + name pair.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="metadataNamespace">The expected namespace name.</param>
    /// <param name="metadataTypeName">The expected type name.</param>
    /// <returns><c>true</c> if the symbol matches.</returns>
    private static bool HasMetadataIdentity(this INamedTypeSymbol symbol, string metadataNamespace, string metadataTypeName)
    {
        if (symbol.Name != metadataTypeName)
            return false;

        return metadataNamespace == symbol.ContainingNamespace.GetMetadataName();
    }

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="Span{T}" /> or <see cref="ReadOnlySpan{T}" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is a span type; otherwise, <c>false</c>.</returns>
    /// <seealso cref="IsMemoryType" />
    /// <seealso cref="GetElementType" />
    public static bool IsSpanType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        return symbol is INamedTypeSymbol named &&
               (named.HasMetadataIdentity("System", "Span") || named.HasMetadataIdentity("System", "ReadOnlySpan"));
    }

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="Memory{T}" /> or <see cref="ReadOnlyMemory{T}" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is a memory type; otherwise, <c>false</c>.</returns>
    /// <seealso cref="IsSpanType" />
    /// <seealso cref="GetElementType" />
    public static bool IsMemoryType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        return symbol is INamedTypeSymbol named &&
               (named.HasMetadataIdentity("System", "Memory") ||
                named.HasMetadataIdentity("System", "ReadOnlyMemory"));
    }

    /// <summary>
    ///     Determines whether the type symbol represents a task type.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is a task type; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>Task types include <c>Task</c>, <c>Task&lt;T&gt;</c>, <c>ValueTask</c>, and <c>ValueTask&lt;T&gt;</c>.</para>
    /// </remarks>
    public static bool IsTaskType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        return symbol is INamedTypeSymbol named &&
               (named.HasMetadataIdentity("System.Threading.Tasks", "Task") ||
                named.HasMetadataIdentity("System.Threading.Tasks", "ValueTask"));
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
    ///         that uses symbol matching instead of cached type strings. Useful in source generators
    ///         that do not create an <c>AwaitableContext</c>.
    ///     </para>
    ///     <para>
    ///         Returns <c>null</c> for non-generic <c>Task</c>, non-generic <c>ValueTask</c>,
    ///         and all non-task types.
    ///     </para>
    /// </remarks>
    public static ITypeSymbol? GetTaskResultType(this ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } named || !named.IsTaskType())
            return null;

        return named.TypeArguments[0];
    }

    /// <summary>
    ///     Determines whether the type symbol represents an enumerable type.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
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

        // Closed generics like IEnumerable<int> carry SpecialType.None; the SpecialType marker lives
        // on the open generic only, so we must consult OriginalDefinition.SpecialType here.
        return symbol is INamedTypeSymbol namedType &&
               namedType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T;
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

        if (namedType.IsSpanType() ||
            namedType.IsMemoryType() ||
            namedType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T)
            return namedType.TypeArguments[0];

        return null;
    }
}
