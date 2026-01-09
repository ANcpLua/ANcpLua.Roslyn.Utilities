using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Patterns;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
sealed class ParameterPatternBuilder
{
    private readonly List<Func<IParameterSymbol, bool>> _predicates = [];

    public ParameterPatternBuilder Named(string name)
    {
        _predicates.Add(p => p.Name == name);
        return this;
    }

    public ParameterPatternBuilder OfType(ITypeSymbol type)
    {
        _predicates.Add(p => p.Type.IsEqualTo(type));
        return this;
    }

    public ParameterPatternBuilder OfType(Func<ITypeSymbol, bool> predicate)
    {
        _predicates.Add(p => predicate(p.Type));
        return this;
    }

    public ParameterPatternBuilder OfTypeNamed(string fullTypeName)
    {
        _predicates.Add(p => p.Type.ToDisplayString() == fullTypeName);
        return this;
    }

    public ParameterPatternBuilder IsString()
    {
        _predicates.Add(p => p.Type.SpecialType == SpecialType.System_String);
        return this;
    }

    public ParameterPatternBuilder IsInt()
    {
        _predicates.Add(p => p.Type.SpecialType == SpecialType.System_Int32);
        return this;
    }

    public ParameterPatternBuilder IsBool()
    {
        _predicates.Add(p => p.Type.SpecialType == SpecialType.System_Boolean);
        return this;
    }

    public ParameterPatternBuilder IsCancellationToken()
    {
        _predicates.Add(p => p.Type.ToDisplayString() == "System.Threading.CancellationToken");
        return this;
    }

    public ParameterPatternBuilder IsNullable()
    {
        _predicates.Add(p => p.NullableAnnotation == NullableAnnotation.Annotated);
        return this;
    }

    public ParameterPatternBuilder IsNotNullable()
    {
        _predicates.Add(p => p.NullableAnnotation != NullableAnnotation.Annotated);
        return this;
    }

    public ParameterPatternBuilder IsOptional()
    {
        _predicates.Add(p => p.IsOptional);
        return this;
    }

    public ParameterPatternBuilder IsRequired()
    {
        _predicates.Add(p => !p.IsOptional);
        return this;
    }

    public ParameterPatternBuilder IsParams()
    {
        _predicates.Add(p => p.IsParams);
        return this;
    }

    public ParameterPatternBuilder IsRef()
    {
        _predicates.Add(p => p.RefKind == RefKind.Ref);
        return this;
    }

    public ParameterPatternBuilder IsOut()
    {
        _predicates.Add(p => p.RefKind == RefKind.Out);
        return this;
    }

    public ParameterPatternBuilder IsIn()
    {
        _predicates.Add(p => p.RefKind == RefKind.In);
        return this;
    }

    public ParameterPatternBuilder IsByRef()
    {
        _predicates.Add(p => p.RefKind != RefKind.None);
        return this;
    }

    public ParameterPatternBuilder IsByValue()
    {
        _predicates.Add(p => p.RefKind == RefKind.None);
        return this;
    }

    public ParameterPatternBuilder AtOrdinal(int ordinal)
    {
        _predicates.Add(p => p.Ordinal == ordinal);
        return this;
    }

    public ParameterPatternBuilder HasAttribute(ITypeSymbol attributeType)
    {
        _predicates.Add(p => p.HasAttribute(attributeType));
        return this;
    }

    public ParameterPatternBuilder HasAttribute(string fullyQualifiedName)
    {
        _predicates.Add(p => p.HasAttribute(fullyQualifiedName));
        return this;
    }

    public ParameterPatternBuilder HasDefaultValue()
    {
        _predicates.Add(p => p.HasExplicitDefaultValue);
        return this;
    }

    public ParameterPatternBuilder HasDefaultValue<T>(T value)
    {
        _predicates.Add(p => p.HasExplicitDefaultValue && Equals(p.ExplicitDefaultValue, value));
        return this;
    }

    public ParameterPatternBuilder Where(Func<IParameterSymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    public SymbolPattern<IParameterSymbol> Build()
    {
        var predicates = _predicates.ToArray();
        return new PredicatePattern<IParameterSymbol>(p =>
        {
            foreach (var predicate in predicates)
            {
                if (!predicate(p))
                    return false;
            }
            return true;
        });
    }

    public static implicit operator SymbolPattern<IParameterSymbol>(ParameterPatternBuilder builder) => builder.Build();
}
