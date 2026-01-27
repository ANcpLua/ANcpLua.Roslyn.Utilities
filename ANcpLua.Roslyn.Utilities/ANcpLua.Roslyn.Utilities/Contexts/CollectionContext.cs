// ReSharper disable InconsistentNaming - Interface-prefixed properties (IEnumerable, IList, etc.)
// are intentional: each property returns the corresponding interface type symbol.
// This naming provides API clarity for Roslyn analyzer authors.

using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Contexts;

/// <summary>
///     Provides cached type symbols and utility methods for working with collection types in Roslyn analysis.
///     <para>
///         This context caches commonly used collection type symbols from the compilation to avoid repeated lookups,
///         and provides methods to classify and analyze collection types.
///     </para>
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Type symbols are resolved once during construction and cached for the lifetime of the context.</description>
///         </item>
///         <item>
///             <description>Properties may be <c>null</c> if the corresponding type is not available in the compilation.</description>
///         </item>
///         <item>
///             <description>All methods handle <c>null</c> input gracefully by returning <c>false</c> or <c>null</c>.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="AwaitableContext" />
/// <seealso cref="DisposableContext" />
/// <seealso cref="AspNetContext" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class CollectionContext
{
    /// <summary>Gets the <see cref="System.Collections.IEnumerable" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? IEnumerable { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Generic.IEnumerable{T}" /> type symbol, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? IEnumerableOfT { get; }

    /// <summary>Gets the <see cref="System.Collections.ICollection" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? ICollection { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Generic.ICollection{T}" /> type symbol, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? ICollectionOfT { get; }

    /// <summary>Gets the <see cref="System.Collections.IList" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? IList { get; }

    /// <summary>Gets the <see cref="System.Collections.Generic.IList{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? IListOfT { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Generic.IReadOnlyCollection{T}" /> type symbol, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? IReadOnlyCollection { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Generic.IReadOnlyList{T}" /> type symbol, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? IReadOnlyList { get; }

    /// <summary>Gets the <see cref="System.Collections.IDictionary" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? IDictionary { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" /> type symbol, or <c>null</c> if
    ///     not available.
    /// </summary>
    public INamedTypeSymbol? IDictionaryOfTKeyTValue { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}" /> type symbol, or
    ///     <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? IReadOnlyDictionary { get; }

    /// <summary>Gets the <see cref="System.Collections.Generic.ISet{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? ISet { get; }

    /// <summary>Gets the <c>IReadOnlySet&lt;T&gt;</c> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? IReadOnlySet { get; }

    /// <summary>Gets the <see cref="System.Collections.Generic.List{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? List { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> type symbol, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? Dictionary { get; }

    /// <summary>Gets the <see cref="System.Collections.Generic.HashSet{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? HashSet { get; }

    /// <summary>Gets the <see cref="System.Collections.Generic.Queue{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? Queue { get; }

    /// <summary>Gets the <see cref="System.Collections.Generic.Stack{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? Stack { get; }

    /// <summary>Gets the <see cref="System.Collections.Generic.LinkedList{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? LinkedList { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Immutable.ImmutableArray{T}" /> type symbol, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? ImmutableArray { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Immutable.ImmutableList{T}" /> type symbol, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? ImmutableList { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Immutable.ImmutableDictionary{TKey, TValue}" /> type symbol, or
    ///     <c>null</c> if not available.
    /// </summary>
    public INamedTypeSymbol? ImmutableDictionary { get; }

    /// <summary>
    ///     Gets the <see cref="System.Collections.Immutable.ImmutableHashSet{T}" /> type symbol, or <c>null</c> if not
    ///     available.
    /// </summary>
    public INamedTypeSymbol? ImmutableHashSet { get; }

    /// <summary>Gets the <c>System.Collections.Frozen.FrozenSet&lt;T&gt;</c> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? FrozenSet { get; }

    /// <summary>
    ///     Gets the <c>System.Collections.Frozen.FrozenDictionary&lt;TKey, TValue&gt;</c> type symbol, or <c>null</c> if
    ///     not available.
    /// </summary>
    public INamedTypeSymbol? FrozenDictionary { get; }

    /// <summary>Gets the <see cref="System.ArraySegment{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? ArraySegment { get; }

    /// <summary>Gets the <see cref="System.Span{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? Span { get; }

    /// <summary>Gets the <see cref="System.ReadOnlySpan{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? ReadOnlySpan { get; }

    /// <summary>Gets the <see cref="System.Memory{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? Memory { get; }

    /// <summary>Gets the <see cref="System.ReadOnlyMemory{T}" /> type symbol, or <c>null</c> if not available.</summary>
    public INamedTypeSymbol? ReadOnlyMemory { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CollectionContext" /> class by resolving
    ///     all collection type symbols from the specified compilation.
    /// </summary>
    /// <param name="compilation">The compilation from which to resolve collection type symbols.</param>
    public CollectionContext(Compilation compilation)
    {
        IEnumerable = compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
        IEnumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        ICollection = compilation.GetTypeByMetadataName("System.Collections.ICollection");
        ICollectionOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
        IList = compilation.GetTypeByMetadataName("System.Collections.IList");
        IListOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1");
        IReadOnlyCollection = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1");
        IReadOnlyList = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1");
        IDictionary = compilation.GetTypeByMetadataName("System.Collections.IDictionary");
        IDictionaryOfTKeyTValue = compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2");
        IReadOnlyDictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2");
        ISet = compilation.GetTypeByMetadataName("System.Collections.Generic.ISet`1");
        IReadOnlySet = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlySet`1");
        List = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
        Dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
        HashSet = compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1");
        Queue = compilation.GetTypeByMetadataName("System.Collections.Generic.Queue`1");
        Stack = compilation.GetTypeByMetadataName("System.Collections.Generic.Stack`1");
        LinkedList = compilation.GetTypeByMetadataName("System.Collections.Generic.LinkedList`1");
        ImmutableArray = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1");
        ImmutableList = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableList`1");
        ImmutableDictionary = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableDictionary`2");
        ImmutableHashSet = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableHashSet`1");
        FrozenSet = compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenSet`1");
        FrozenDictionary = compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenDictionary`2");
        ArraySegment = compilation.GetTypeByMetadataName("System.ArraySegment`1");
        Span = compilation.GetTypeByMetadataName("System.Span`1");
        ReadOnlySpan = compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");
        Memory = compilation.GetTypeByMetadataName("System.Memory`1");
        ReadOnlyMemory = compilation.GetTypeByMetadataName("System.ReadOnlyMemory`1");
    }

    /// <summary>
    ///     Determines whether the specified type is enumerable.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is an array, implements <see cref="System.Collections.IEnumerable" />,
    ///     or implements <see cref="System.Collections.Generic.IEnumerable{T}" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsCollection" />
    /// <seealso cref="IsList" />
    public bool IsEnumerable(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (type is IArrayTypeSymbol)
            return true;

        if (IEnumerable is not null && type.Implements(IEnumerable))
            return true;

        if (IEnumerableOfT is not null && type is INamedTypeSymbol named)
            foreach (var iface in named.AllInterfaces)
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, IEnumerableOfT))
                    return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type is a collection.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is an array, implements <see cref="System.Collections.ICollection" />,
    ///     or implements <see cref="System.Collections.Generic.ICollection{T}" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsEnumerable" />
    /// <seealso cref="IsList" />
    public bool IsCollection(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (type is IArrayTypeSymbol)
            return true;

        if (ICollection is not null && type.Implements(ICollection))
            return true;

        if (ICollectionOfT is not null && type is INamedTypeSymbol named)
            foreach (var iface in named.AllInterfaces)
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, ICollectionOfT))
                    return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type is a list.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is an array, implements <see cref="System.Collections.IList" />,
    ///     or implements <see cref="System.Collections.Generic.IList{T}" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsEnumerable" />
    /// <seealso cref="IsCollection" />
    public bool IsList(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (type is IArrayTypeSymbol)
            return true;

        if (IList is not null && type.Implements(IList))
            return true;

        if (IListOfT is not null && type is INamedTypeSymbol named)
            foreach (var iface in named.AllInterfaces)
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, IListOfT))
                    return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type is a dictionary.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> implements <see cref="System.Collections.IDictionary" />
    ///     or <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsSet" />
    public bool IsDictionary(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (IDictionary is not null && type.Implements(IDictionary))
            return true;

        if (IDictionaryOfTKeyTValue is not null && type is INamedTypeSymbol named)
            foreach (var iface in named.AllInterfaces)
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, IDictionaryOfTKeyTValue))
                    return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type is a set.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> implements <see cref="System.Collections.Generic.ISet{T}" />; otherwise,
    ///     <c>false</c>.
    /// </returns>
    /// <seealso cref="IsDictionary" />
    public bool IsSet(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (ISet is not null && type is INamedTypeSymbol named)
            foreach (var iface in named.AllInterfaces)
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, ISet))
                    return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type is an immutable collection type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is one of the immutable collection types
    ///     (<c>ImmutableArray&lt;T&gt;</c>, <c>ImmutableList&lt;T&gt;</c>, <c>ImmutableDictionary&lt;TKey, TValue&gt;</c>,
    ///     <c>ImmutableHashSet&lt;T&gt;</c>, <c>FrozenSet&lt;T&gt;</c>, or <c>FrozenDictionary&lt;TKey, TValue&gt;</c>);
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsFrozen" />
    /// <seealso cref="IsReadOnly" />
    public bool IsImmutable(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        var original = named.OriginalDefinition;

        return (ImmutableArray is not null && SymbolEqualityComparer.Default.Equals(original, ImmutableArray)) ||
               (ImmutableList is not null && SymbolEqualityComparer.Default.Equals(original, ImmutableList)) ||
               (ImmutableDictionary is not null &&
                SymbolEqualityComparer.Default.Equals(original, ImmutableDictionary)) ||
               (ImmutableHashSet is not null && SymbolEqualityComparer.Default.Equals(original, ImmutableHashSet)) ||
               (FrozenSet is not null && SymbolEqualityComparer.Default.Equals(original, FrozenSet)) ||
               (FrozenDictionary is not null && SymbolEqualityComparer.Default.Equals(original, FrozenDictionary));
    }

    /// <summary>
    ///     Determines whether the specified type is a frozen collection type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is <c>FrozenSet&lt;T&gt;</c> or <c>FrozenDictionary&lt;TKey, TValue&gt;</c>
    ///     ;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsImmutable" />
    public bool IsFrozen(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        var original = named.OriginalDefinition;

        return (FrozenSet is not null && SymbolEqualityComparer.Default.Equals(original, FrozenSet)) ||
               (FrozenDictionary is not null && SymbolEqualityComparer.Default.Equals(original, FrozenDictionary));
    }

    /// <summary>
    ///     Determines whether the specified type is a span-like type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is <see cref="System.Span{T}" /> or <see cref="System.ReadOnlySpan{T}" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsMemoryLike" />
    public bool IsSpanLike(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        var original = named.OriginalDefinition;

        return (Span is not null && SymbolEqualityComparer.Default.Equals(original, Span)) ||
               (ReadOnlySpan is not null && SymbolEqualityComparer.Default.Equals(original, ReadOnlySpan));
    }

    /// <summary>
    ///     Determines whether the specified type is a memory-like type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is <see cref="System.Memory{T}" /> or
    ///     <see cref="System.ReadOnlyMemory{T}" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsSpanLike" />
    public bool IsMemoryLike(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        var original = named.OriginalDefinition;

        return (Memory is not null && SymbolEqualityComparer.Default.Equals(original, Memory)) ||
               (ReadOnlyMemory is not null && SymbolEqualityComparer.Default.Equals(original, ReadOnlyMemory));
    }

    /// <summary>
    ///     Determines whether the specified type is a read-only collection type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> is immutable, frozen, or implements a read-only collection interface
    ///     (<c>IReadOnlyCollection&lt;T&gt;</c>, <c>IReadOnlyList&lt;T&gt;</c>, <c>IReadOnlyDictionary&lt;TKey, TValue&gt;</c>
    ///     ,
    ///     <c>IReadOnlySet&lt;T&gt;</c>), or is <c>ReadOnlySpan&lt;T&gt;</c> or <c>ReadOnlyMemory&lt;T&gt;</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsImmutable" />
    /// <seealso cref="IsFrozen" />
    public bool IsReadOnly(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (IsImmutable(type) || IsFrozen(type))
            return true;

        if (type is not INamedTypeSymbol named)
            return false;

        var original = named.OriginalDefinition;

        return (IReadOnlyCollection is not null && type.Implements(IReadOnlyCollection.OriginalDefinition)) ||
               (IReadOnlyList is not null && type.Implements(IReadOnlyList.OriginalDefinition)) ||
               (IReadOnlyDictionary is not null && type.Implements(IReadOnlyDictionary.OriginalDefinition)) ||
               (IReadOnlySet is not null && type.Implements(IReadOnlySet.OriginalDefinition)) ||
               (ReadOnlySpan is not null && SymbolEqualityComparer.Default.Equals(original, ReadOnlySpan)) ||
               (ReadOnlyMemory is not null && SymbolEqualityComparer.Default.Equals(original, ReadOnlyMemory));
    }

    /// <summary>
    ///     Gets the element type of a collection type.
    /// </summary>
    /// <param name="type">The collection type symbol to analyze.</param>
    /// <returns>
    ///     The element type of the collection if <paramref name="type" /> is an array, a recognized generic collection type,
    ///     or implements <see cref="System.Collections.Generic.IEnumerable{T}" />; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         For arrays, returns the <see cref="IArrayTypeSymbol.ElementType" />.
    ///         For generic collection types with a single type argument, returns that type argument.
    ///         For types implementing <c>IEnumerable&lt;T&gt;</c>, returns the <c>T</c> type argument from the interface.
    ///     </para>
    /// </remarks>
    public ITypeSymbol? GetElementType(ITypeSymbol? type)
    {
        switch (type)
        {
            case null:
                return null;
            case IArrayTypeSymbol array:
                return array.ElementType;
        }

        if (type is not INamedTypeSymbol named)
            return null;

        if (named.TypeArguments.Length == 1 && IsSingleElementCollectionType(named.OriginalDefinition))
            return named.TypeArguments[0];

        // For types implementing IEnumerable<T>, find the element type from the interface
        if (IEnumerableOfT is not null)
            foreach (var iface in named.AllInterfaces)
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, IEnumerableOfT) &&
                    iface.TypeArguments.Length == 1)
                    return iface.TypeArguments[0];

        return null;
    }

    /// <summary>
    ///     Determines whether the specified type definition is a known single-element collection type.
    /// </summary>
    private bool IsSingleElementCollectionType(INamedTypeSymbol original) =>
        MatchesAny(original,
            IEnumerableOfT, ICollectionOfT, IListOfT, IReadOnlyCollection, IReadOnlyList,
            ISet, IReadOnlySet, List, HashSet, Queue, Stack, LinkedList,
            ImmutableArray, ImmutableList, ImmutableHashSet, FrozenSet,
            Span, ReadOnlySpan, Memory, ReadOnlyMemory, ArraySegment);

    /// <summary>
    ///     Checks if the specified type matches any of the provided candidate types.
    /// </summary>
    private static bool MatchesAny(INamedTypeSymbol type, params INamedTypeSymbol?[] candidates)
    {
        foreach (var candidate in candidates)
            if (candidate is not null && SymbolEqualityComparer.Default.Equals(type, candidate))
                return true;

        return false;
    }
}