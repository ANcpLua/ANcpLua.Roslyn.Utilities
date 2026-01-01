using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Patterns;

public sealed class PropertyPatternBuilder
{
    private readonly List<Func<IPropertySymbol, bool>> _predicates = [];

    public PropertyPatternBuilder Named(string name)
    {
        _predicates.Add(p => p.Name == name);
        return this;
    }

    public PropertyPatternBuilder OfType(ITypeSymbol type)
    {
        _predicates.Add(p => p.Type.IsEqualTo(type));
        return this;
    }

    public PropertyPatternBuilder OfType(Func<ITypeSymbol, bool> predicate)
    {
        _predicates.Add(p => predicate(p.Type));
        return this;
    }

    public PropertyPatternBuilder Public()
    {
        _predicates.Add(p => p.DeclaredAccessibility == Accessibility.Public);
        return this;
    }

    public PropertyPatternBuilder Private()
    {
        _predicates.Add(p => p.DeclaredAccessibility == Accessibility.Private);
        return this;
    }

    public PropertyPatternBuilder Static()
    {
        _predicates.Add(p => p.IsStatic);
        return this;
    }

    public PropertyPatternBuilder Instance()
    {
        _predicates.Add(p => !p.IsStatic);
        return this;
    }

    public PropertyPatternBuilder HasGetter()
    {
        _predicates.Add(p => p.GetMethod is not null);
        return this;
    }

    public PropertyPatternBuilder HasSetter()
    {
        _predicates.Add(p => p.SetMethod is not null);
        return this;
    }

    public PropertyPatternBuilder IsReadOnly()
    {
        _predicates.Add(p => p.IsReadOnly);
        return this;
    }

    public PropertyPatternBuilder IsWriteOnly()
    {
        _predicates.Add(p => p.IsWriteOnly);
        return this;
    }

    public PropertyPatternBuilder IsAutoProperty()
    {
        _predicates.Add(p =>
            p.GetMethod?.IsImplicitlyDeclared == true ||
            p.SetMethod?.IsImplicitlyDeclared == true);
        return this;
    }

    public PropertyPatternBuilder IsRequired()
    {
        _predicates.Add(p => p.IsRequired);
        return this;
    }

    public PropertyPatternBuilder IsIndexer()
    {
        _predicates.Add(p => p.IsIndexer);
        return this;
    }

    public PropertyPatternBuilder IsVirtual()
    {
        _predicates.Add(p => p.IsVirtual);
        return this;
    }

    public PropertyPatternBuilder IsOverride()
    {
        _predicates.Add(p => p.IsOverride);
        return this;
    }

    public PropertyPatternBuilder HasAttribute(ITypeSymbol attributeType)
    {
        _predicates.Add(p => p.HasAttribute(attributeType));
        return this;
    }

    public PropertyPatternBuilder HasAttribute(string fullyQualifiedName)
    {
        _predicates.Add(p => p.HasAttribute(fullyQualifiedName));
        return this;
    }

    public PropertyPatternBuilder InType(SymbolPattern<INamedTypeSymbol> pattern)
    {
        _predicates.Add(p => p.ContainingType is not null && pattern.Matches(p.ContainingType));
        return this;
    }

    public PropertyPatternBuilder Where(Func<IPropertySymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    public SymbolPattern<IPropertySymbol> Build()
    {
        var predicates = _predicates.ToArray();
        return new PredicatePattern<IPropertySymbol>(p =>
        {
            foreach (var predicate in predicates)
            {
                if (!predicate(p))
                    return false;
            }
            return true;
        });
    }

    public static implicit operator SymbolPattern<IPropertySymbol>(PropertyPatternBuilder builder) => builder.Build();
}

public sealed class FieldPatternBuilder
{
    private readonly List<Func<IFieldSymbol, bool>> _predicates = [];

    public FieldPatternBuilder Named(string name)
    {
        _predicates.Add(f => f.Name == name);
        return this;
    }

    public FieldPatternBuilder OfType(ITypeSymbol type)
    {
        _predicates.Add(f => f.Type.IsEqualTo(type));
        return this;
    }

    public FieldPatternBuilder Public()
    {
        _predicates.Add(f => f.DeclaredAccessibility == Accessibility.Public);
        return this;
    }

    public FieldPatternBuilder Private()
    {
        _predicates.Add(f => f.DeclaredAccessibility == Accessibility.Private);
        return this;
    }

    public FieldPatternBuilder Static()
    {
        _predicates.Add(f => f.IsStatic);
        return this;
    }

    public FieldPatternBuilder Instance()
    {
        _predicates.Add(f => !f.IsStatic);
        return this;
    }

    public FieldPatternBuilder IsConst()
    {
        _predicates.Add(f => f.IsConst);
        return this;
    }

    public FieldPatternBuilder IsReadOnly()
    {
        _predicates.Add(f => f.IsReadOnly);
        return this;
    }

    public FieldPatternBuilder IsVolatile()
    {
        _predicates.Add(f => f.IsVolatile);
        return this;
    }

    public FieldPatternBuilder HasAttribute(ITypeSymbol attributeType)
    {
        _predicates.Add(f => f.HasAttribute(attributeType));
        return this;
    }

    public FieldPatternBuilder HasAttribute(string fullyQualifiedName)
    {
        _predicates.Add(f => f.HasAttribute(fullyQualifiedName));
        return this;
    }

    public FieldPatternBuilder Where(Func<IFieldSymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    public SymbolPattern<IFieldSymbol> Build()
    {
        var predicates = _predicates.ToArray();
        return new PredicatePattern<IFieldSymbol>(f =>
        {
            foreach (var predicate in predicates)
            {
                if (!predicate(f))
                    return false;
            }
            return true;
        });
    }

    public static implicit operator SymbolPattern<IFieldSymbol>(FieldPatternBuilder builder) => builder.Build();
}
