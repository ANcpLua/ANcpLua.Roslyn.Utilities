using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Contexts;

public sealed class CollectionContext
{
    public INamedTypeSymbol? IEnumerable { get; }
    public INamedTypeSymbol? IEnumerableOfT { get; }
    public INamedTypeSymbol? ICollection { get; }
    public INamedTypeSymbol? ICollectionOfT { get; }
    public INamedTypeSymbol? IList { get; }
    public INamedTypeSymbol? IListOfT { get; }
    public INamedTypeSymbol? IReadOnlyCollection { get; }
    public INamedTypeSymbol? IReadOnlyList { get; }
    public INamedTypeSymbol? IDictionary { get; }
    public INamedTypeSymbol? IDictionaryOfTKeyTValue { get; }
    public INamedTypeSymbol? IReadOnlyDictionary { get; }
    public INamedTypeSymbol? ISet { get; }
    public INamedTypeSymbol? IReadOnlySet { get; }
    public INamedTypeSymbol? List { get; }
    public INamedTypeSymbol? Dictionary { get; }
    public INamedTypeSymbol? HashSet { get; }
    public INamedTypeSymbol? Queue { get; }
    public INamedTypeSymbol? Stack { get; }
    public INamedTypeSymbol? LinkedList { get; }
    public INamedTypeSymbol? ImmutableArray { get; }
    public INamedTypeSymbol? ImmutableList { get; }
    public INamedTypeSymbol? ImmutableDictionary { get; }
    public INamedTypeSymbol? ImmutableHashSet { get; }
    public INamedTypeSymbol? FrozenSet { get; }
    public INamedTypeSymbol? FrozenDictionary { get; }
    public INamedTypeSymbol? ArraySegment { get; }
    public INamedTypeSymbol? Span { get; }
    public INamedTypeSymbol? ReadOnlySpan { get; }
    public INamedTypeSymbol? Memory { get; }
    public INamedTypeSymbol? ReadOnlyMemory { get; }

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

    public bool IsEnumerable(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (type is IArrayTypeSymbol)
            return true;

        if (IEnumerable is not null && type.Implements(IEnumerable))
            return true;

        if (IEnumerableOfT is not null && type is INamedTypeSymbol named)
        {
            foreach (var iface in named.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, IEnumerableOfT))
                    return true;
            }
        }

        return false;
    }

    public bool IsCollection(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (type is IArrayTypeSymbol)
            return true;

        if (ICollection is not null && type.Implements(ICollection))
            return true;

        if (ICollectionOfT is not null && type is INamedTypeSymbol named)
        {
            foreach (var iface in named.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, ICollectionOfT))
                    return true;
            }
        }

        return false;
    }

    public bool IsList(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (type is IArrayTypeSymbol)
            return true;

        if (IList is not null && type.Implements(IList))
            return true;

        if (IListOfT is not null && type is INamedTypeSymbol named)
        {
            foreach (var iface in named.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, IListOfT))
                    return true;
            }
        }

        return false;
    }

    public bool IsDictionary(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (IDictionary is not null && type.Implements(IDictionary))
            return true;

        if (IDictionaryOfTKeyTValue is not null && type is INamedTypeSymbol named)
        {
            foreach (var iface in named.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, IDictionaryOfTKeyTValue))
                    return true;
            }
        }

        return false;
    }

    public bool IsSet(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (ISet is not null && type is INamedTypeSymbol named)
        {
            foreach (var iface in named.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, ISet))
                    return true;
            }
        }

        return false;
    }

    public bool IsImmutable(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (type is not INamedTypeSymbol named)
            return false;

        var original = named.OriginalDefinition;

        return (ImmutableArray is not null && SymbolEqualityComparer.Default.Equals(original, ImmutableArray)) ||
               (ImmutableList is not null && SymbolEqualityComparer.Default.Equals(original, ImmutableList)) ||
               (ImmutableDictionary is not null && SymbolEqualityComparer.Default.Equals(original, ImmutableDictionary)) ||
               (ImmutableHashSet is not null && SymbolEqualityComparer.Default.Equals(original, ImmutableHashSet)) ||
               (FrozenSet is not null && SymbolEqualityComparer.Default.Equals(original, FrozenSet)) ||
               (FrozenDictionary is not null && SymbolEqualityComparer.Default.Equals(original, FrozenDictionary));
    }

    public bool IsFrozen(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (type is not INamedTypeSymbol named)
            return false;

        var original = named.OriginalDefinition;

        return (FrozenSet is not null && SymbolEqualityComparer.Default.Equals(original, FrozenSet)) ||
               (FrozenDictionary is not null && SymbolEqualityComparer.Default.Equals(original, FrozenDictionary));
    }

    public bool IsSpanLike(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (type is not INamedTypeSymbol named)
            return false;

        var original = named.OriginalDefinition;

        return (Span is not null && SymbolEqualityComparer.Default.Equals(original, Span)) ||
               (ReadOnlySpan is not null && SymbolEqualityComparer.Default.Equals(original, ReadOnlySpan));
    }

    public bool IsMemoryLike(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (type is not INamedTypeSymbol named)
            return false;

        var original = named.OriginalDefinition;

        return (Memory is not null && SymbolEqualityComparer.Default.Equals(original, Memory)) ||
               (ReadOnlyMemory is not null && SymbolEqualityComparer.Default.Equals(original, ReadOnlyMemory));
    }

    public static bool HasCountProperty(ITypeSymbol type)
    {
        foreach (var member in type.GetAllMembers("Count"))
        {
            if (member is IPropertySymbol { GetMethod: not null })
                return true;
        }

        foreach (var member in type.GetAllMembers("Length"))
        {
            if (member is IPropertySymbol { GetMethod: not null })
                return true;
        }

        return false;
    }

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

    public ITypeSymbol? GetElementType(ITypeSymbol? type)
    {
        if (type is null)
            return null;

        if (type is IArrayTypeSymbol array)
            return array.ElementType;

        if (type is not INamedTypeSymbol named)
            return null;

        if (named.TypeArguments.Length == 1)
        {
            var original = named.OriginalDefinition;
            if ((IEnumerableOfT is not null && SymbolEqualityComparer.Default.Equals(original, IEnumerableOfT)) ||
                (ICollectionOfT is not null && SymbolEqualityComparer.Default.Equals(original, ICollectionOfT)) ||
                (IListOfT is not null && SymbolEqualityComparer.Default.Equals(original, IListOfT)) ||
                (IReadOnlyCollection is not null && SymbolEqualityComparer.Default.Equals(original, IReadOnlyCollection)) ||
                (IReadOnlyList is not null && SymbolEqualityComparer.Default.Equals(original, IReadOnlyList)) ||
                (ISet is not null && SymbolEqualityComparer.Default.Equals(original, ISet)) ||
                (IReadOnlySet is not null && SymbolEqualityComparer.Default.Equals(original, IReadOnlySet)) ||
                (List is not null && SymbolEqualityComparer.Default.Equals(original, List)) ||
                (HashSet is not null && SymbolEqualityComparer.Default.Equals(original, HashSet)) ||
                (Queue is not null && SymbolEqualityComparer.Default.Equals(original, Queue)) ||
                (Stack is not null && SymbolEqualityComparer.Default.Equals(original, Stack)) ||
                (LinkedList is not null && SymbolEqualityComparer.Default.Equals(original, LinkedList)) ||
                (ImmutableArray is not null && SymbolEqualityComparer.Default.Equals(original, ImmutableArray)) ||
                (ImmutableList is not null && SymbolEqualityComparer.Default.Equals(original, ImmutableList)) ||
                (ImmutableHashSet is not null && SymbolEqualityComparer.Default.Equals(original, ImmutableHashSet)) ||
                (FrozenSet is not null && SymbolEqualityComparer.Default.Equals(original, FrozenSet)) ||
                (Span is not null && SymbolEqualityComparer.Default.Equals(original, Span)) ||
                (ReadOnlySpan is not null && SymbolEqualityComparer.Default.Equals(original, ReadOnlySpan)) ||
                (Memory is not null && SymbolEqualityComparer.Default.Equals(original, Memory)) ||
                (ReadOnlyMemory is not null && SymbolEqualityComparer.Default.Equals(original, ReadOnlyMemory)) ||
                (ArraySegment is not null && SymbolEqualityComparer.Default.Equals(original, ArraySegment)))
            {
                return named.TypeArguments[0];
            }
        }

        // For types implementing IEnumerable<T>, find the element type from the interface
        if (IEnumerableOfT is not null)
        {
            foreach (var iface in named.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, IEnumerableOfT) &&
                    iface.TypeArguments.Length == 1)
                {
                    return iface.TypeArguments[0];
                }
            }
        }

        return null;
    }
}
