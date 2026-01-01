using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Patterns;

public sealed class TypePatternBuilder
{
    private readonly List<Func<INamedTypeSymbol, bool>> _predicates = [];

    public TypePatternBuilder Named(string name)
    {
        _predicates.Add(t => t.Name == name);
        return this;
    }

    public TypePatternBuilder FullName(string fullName)
    {
        _predicates.Add(t => t.ToDisplayString() == fullName);
        return this;
    }

    public TypePatternBuilder NameStartsWith(string prefix)
    {
        _predicates.Add(t => t.Name.StartsWith(prefix, StringComparison.Ordinal));
        return this;
    }

    public TypePatternBuilder NameEndsWith(string suffix)
    {
        _predicates.Add(t => t.Name.EndsWith(suffix, StringComparison.Ordinal));
        return this;
    }

    public TypePatternBuilder Public()
    {
        _predicates.Add(t => t.DeclaredAccessibility == Accessibility.Public);
        return this;
    }

    public TypePatternBuilder Internal()
    {
        _predicates.Add(t => t.DeclaredAccessibility == Accessibility.Internal);
        return this;
    }

    public TypePatternBuilder Static()
    {
        _predicates.Add(t => t.IsStatic);
        return this;
    }

    public TypePatternBuilder NotStatic()
    {
        _predicates.Add(t => !t.IsStatic);
        return this;
    }

    public TypePatternBuilder Abstract()
    {
        _predicates.Add(t => t.IsAbstract);
        return this;
    }

    public TypePatternBuilder Sealed()
    {
        _predicates.Add(t => t.IsSealed);
        return this;
    }

    /// <summary>
    /// Matches types with multiple declaring syntax references (partial types).
    /// Note: This is a heuristic - types with a single partial declaration won't be detected.
    /// For full partial detection including single-declaration partials, use syntax-level checks.
    /// </summary>
    public TypePatternBuilder HasMultipleDeclarations()
    {
        _predicates.Add(t => t.DeclaringSyntaxReferences.Length > 1);
        return this;
    }

    public TypePatternBuilder IsClass()
    {
        _predicates.Add(t => t.TypeKind == TypeKind.Class);
        return this;
    }

    public TypePatternBuilder IsStruct()
    {
        _predicates.Add(t => t.TypeKind == TypeKind.Struct);
        return this;
    }

    public TypePatternBuilder IsInterface()
    {
        _predicates.Add(t => t.TypeKind == TypeKind.Interface);
        return this;
    }

    public TypePatternBuilder IsEnum()
    {
        _predicates.Add(t => t.TypeKind == TypeKind.Enum);
        return this;
    }

    public TypePatternBuilder IsDelegate()
    {
        _predicates.Add(t => t.TypeKind == TypeKind.Delegate);
        return this;
    }

    public TypePatternBuilder IsRecord()
    {
        _predicates.Add(t => t.IsRecord);
        return this;
    }

    public TypePatternBuilder IsGeneric()
    {
        _predicates.Add(t => t.IsGenericType);
        return this;
    }

    public TypePatternBuilder IsNotGeneric()
    {
        _predicates.Add(t => !t.IsGenericType);
        return this;
    }

    public TypePatternBuilder TypeParameterCount(int count)
    {
        _predicates.Add(t => t.TypeParameters.Length == count);
        return this;
    }

    public TypePatternBuilder Implements(ITypeSymbol interfaceType)
    {
        _predicates.Add(t => t.Implements(interfaceType));
        return this;
    }

    public TypePatternBuilder Implements(Func<INamedTypeSymbol, bool> predicate)
    {
        _predicates.Add(t => MatchesAnyInterface(t, predicate));
        return this;
    }

    private static bool MatchesAnyInterface(INamedTypeSymbol type, Func<INamedTypeSymbol, bool> predicate)
    {
        foreach (var iface in type.AllInterfaces)
        {
            if (predicate(iface))
                return true;
        }
        return false;
    }

    public TypePatternBuilder InheritsFrom(ITypeSymbol baseType)
    {
        _predicates.Add(t => t.InheritsFrom(baseType));
        return this;
    }

    public TypePatternBuilder InheritsFrom(Func<INamedTypeSymbol?, bool> predicate)
    {
        _predicates.Add(t =>
        {
            var baseType = t.BaseType;
            while (baseType is not null)
            {
                if (predicate(baseType))
                    return true;
                baseType = baseType.BaseType;
            }
            return false;
        });
        return this;
    }

    public TypePatternBuilder HasAttribute(ITypeSymbol attributeType)
    {
        _predicates.Add(t => t.HasAttribute(attributeType));
        return this;
    }

    public TypePatternBuilder HasAttribute(string fullyQualifiedName)
    {
        _predicates.Add(t => t.HasAttribute(fullyQualifiedName));
        return this;
    }

    public TypePatternBuilder InNamespace(string ns)
    {
        _predicates.Add(t => t.ContainingNamespace?.ToDisplayString() == ns);
        return this;
    }

    public TypePatternBuilder InNamespaceStartsWith(string prefix)
    {
        _predicates.Add(t => t.ContainingNamespace?.ToDisplayString().StartsWith(prefix, StringComparison.Ordinal) == true);
        return this;
    }

    public TypePatternBuilder HasMember(string name)
    {
        _predicates.Add(t => t.GetMembers(name).Length > 0);
        return this;
    }

    public TypePatternBuilder HasMethod(SymbolPattern<IMethodSymbol> pattern)
    {
        _predicates.Add(t => HasMatchingMethod(t, pattern));
        return this;
    }

    private static bool HasMatchingMethod(INamedTypeSymbol type, SymbolPattern<IMethodSymbol> pattern)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is IMethodSymbol method && pattern.Matches(method))
                return true;
        }
        return false;
    }

    public TypePatternBuilder HasProperty(SymbolPattern<IPropertySymbol> pattern)
    {
        _predicates.Add(t => HasMatchingProperty(t, pattern));
        return this;
    }

    private static bool HasMatchingProperty(INamedTypeSymbol type, SymbolPattern<IPropertySymbol> pattern)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is IPropertySymbol property && pattern.Matches(property))
                return true;
        }
        return false;
    }

    public TypePatternBuilder IsNested()
    {
        _predicates.Add(t => t.ContainingType is not null);
        return this;
    }

    public TypePatternBuilder IsTopLevel()
    {
        _predicates.Add(t => t.ContainingType is null);
        return this;
    }

    public TypePatternBuilder IsDisposable()
    {
        _predicates.Add(IsDisposableType);
        return this;
    }

    private static bool IsDisposableType(INamedTypeSymbol type)
    {
        foreach (var iface in type.AllInterfaces)
        {
            var name = iface.ToDisplayString();
            if (name is "System.IDisposable" or "System.IAsyncDisposable")
                return true;
        }
        return false;
    }

    public TypePatternBuilder Where(Func<INamedTypeSymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    public SymbolPattern<INamedTypeSymbol> Build()
    {
        var predicates = _predicates.ToArray();
        return new PredicatePattern<INamedTypeSymbol>(t =>
        {
            foreach (var predicate in predicates)
            {
                if (!predicate(t))
                    return false;
            }
            return true;
        });
    }

    public static implicit operator SymbolPattern<INamedTypeSymbol>(TypePatternBuilder builder) => builder.Build();
}
