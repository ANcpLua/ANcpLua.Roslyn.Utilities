using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Patterns;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
sealed class MethodPatternBuilder
{
    private readonly List<Func<IMethodSymbol, bool>> _predicates = [];

    public MethodPatternBuilder Named(string name)
    {
        _predicates.Add(m => m.Name == name);
        return this;
    }

    public MethodPatternBuilder NameStartsWith(string prefix)
    {
        _predicates.Add(m => m.Name.StartsWith(prefix, StringComparison.Ordinal));
        return this;
    }

    public MethodPatternBuilder NameEndsWith(string suffix)
    {
        _predicates.Add(m => m.Name.EndsWith(suffix, StringComparison.Ordinal));
        return this;
    }

    public MethodPatternBuilder NameMatches(Func<string, bool> predicate)
    {
        _predicates.Add(m => predicate(m.Name));
        return this;
    }

    public MethodPatternBuilder Static()
    {
        _predicates.Add(m => m.IsStatic);
        return this;
    }

    public MethodPatternBuilder Instance()
    {
        _predicates.Add(m => !m.IsStatic);
        return this;
    }

    public MethodPatternBuilder Async()
    {
        _predicates.Add(m => m.IsAsync);
        return this;
    }

    public MethodPatternBuilder NotAsync()
    {
        _predicates.Add(m => !m.IsAsync);
        return this;
    }

    public MethodPatternBuilder Virtual()
    {
        _predicates.Add(m => m.IsVirtual);
        return this;
    }

    public MethodPatternBuilder Override()
    {
        _predicates.Add(m => m.IsOverride);
        return this;
    }

    public MethodPatternBuilder Abstract()
    {
        _predicates.Add(m => m.IsAbstract);
        return this;
    }

    public MethodPatternBuilder Sealed()
    {
        _predicates.Add(m => m.IsSealed);
        return this;
    }

    public MethodPatternBuilder Public()
    {
        _predicates.Add(m => m.DeclaredAccessibility == Accessibility.Public);
        return this;
    }

    public MethodPatternBuilder Private()
    {
        _predicates.Add(m => m.DeclaredAccessibility == Accessibility.Private);
        return this;
    }

    public MethodPatternBuilder Protected()
    {
        _predicates.Add(m => m.DeclaredAccessibility == Accessibility.Protected);
        return this;
    }

    public MethodPatternBuilder Internal()
    {
        _predicates.Add(m => m.DeclaredAccessibility == Accessibility.Internal);
        return this;
    }

    public MethodPatternBuilder ReturnsVoid()
    {
        _predicates.Add(m => m.ReturnsVoid);
        return this;
    }

    public MethodPatternBuilder Returns(ITypeSymbol type)
    {
        _predicates.Add(m => m.ReturnType.IsEqualTo(type));
        return this;
    }

    public MethodPatternBuilder Returns(Func<ITypeSymbol, bool> predicate)
    {
        _predicates.Add(m => predicate(m.ReturnType));
        return this;
    }

    public MethodPatternBuilder ReturnsString()
    {
        _predicates.Add(m => m.ReturnType.SpecialType == SpecialType.System_String);
        return this;
    }

    public MethodPatternBuilder ReturnsBool()
    {
        _predicates.Add(m => m.ReturnType.SpecialType == SpecialType.System_Boolean);
        return this;
    }

    public MethodPatternBuilder ReturnsInt()
    {
        _predicates.Add(m => m.ReturnType.SpecialType == SpecialType.System_Int32);
        return this;
    }

    public MethodPatternBuilder ReturnsTask()
    {
        _predicates.Add(m => m.ReturnType.IsTaskType());
        return this;
    }

    public MethodPatternBuilder NoParameters()
    {
        _predicates.Add(m => m.Parameters.IsEmpty);
        return this;
    }

    public MethodPatternBuilder ParameterCount(int count)
    {
        _predicates.Add(m => m.Parameters.Length == count);
        return this;
    }

    public MethodPatternBuilder ParameterCountAtLeast(int count)
    {
        _predicates.Add(m => m.Parameters.Length >= count);
        return this;
    }

    public MethodPatternBuilder WithParameter(Func<IParameterSymbol, bool> predicate)
    {
        _predicates.Add(m => m.Parameters.Any(predicate));
        return this;
    }

    public MethodPatternBuilder WithParameter(int index, Func<IParameterSymbol, bool> predicate)
    {
        _predicates.Add(m => m.Parameters.Length > index && predicate(m.Parameters[index]));
        return this;
    }

    public MethodPatternBuilder WithParameterOfType(ITypeSymbol type)
    {
        _predicates.Add(m => m.Parameters.Any(p => p.Type.IsEqualTo(type)));
        return this;
    }

    public MethodPatternBuilder WithCancellationToken()
    {
        _predicates.Add(m => m.Parameters.Any(p =>
            p.Type.ToDisplayString() == "System.Threading.CancellationToken"));
        return this;
    }

    public MethodPatternBuilder NoTypeParameters()
    {
        _predicates.Add(m => m.TypeParameters.IsEmpty);
        return this;
    }

    public MethodPatternBuilder TypeParameterCount(int count)
    {
        _predicates.Add(m => m.TypeParameters.Length == count);
        return this;
    }

    public MethodPatternBuilder IsExtensionMethod()
    {
        _predicates.Add(m => m.IsExtensionMethod);
        return this;
    }

    public MethodPatternBuilder IsNotExtensionMethod()
    {
        _predicates.Add(m => !m.IsExtensionMethod);
        return this;
    }

    public MethodPatternBuilder IsConstructor()
    {
        _predicates.Add(m => m.MethodKind == MethodKind.Constructor);
        return this;
    }

    public MethodPatternBuilder IsOrdinaryMethod()
    {
        _predicates.Add(m => m.MethodKind == MethodKind.Ordinary);
        return this;
    }

    public MethodPatternBuilder IsOperator()
    {
        _predicates.Add(m => m.MethodKind is MethodKind.UserDefinedOperator or MethodKind.Conversion);
        return this;
    }

    public MethodPatternBuilder IsPropertyAccessor()
    {
        _predicates.Add(m => m.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet);
        return this;
    }

    public MethodPatternBuilder HasAttribute(ITypeSymbol attributeType)
    {
        _predicates.Add(m => m.HasAttribute(attributeType));
        return this;
    }

    public MethodPatternBuilder HasAttribute(string fullyQualifiedName)
    {
        _predicates.Add(m => m.HasAttribute(fullyQualifiedName));
        return this;
    }

    public MethodPatternBuilder InType(Func<INamedTypeSymbol, bool> predicate)
    {
        _predicates.Add(m => m.ContainingType is not null && predicate(m.ContainingType));
        return this;
    }

    public MethodPatternBuilder InType(SymbolPattern<INamedTypeSymbol> pattern)
    {
        _predicates.Add(m => m.ContainingType is not null && pattern.Matches(m.ContainingType));
        return this;
    }

    public MethodPatternBuilder InNamespace(string ns)
    {
        _predicates.Add(m => m.ContainingNamespace?.ToDisplayString() == ns);
        return this;
    }

    public MethodPatternBuilder ImplementsInterface()
    {
        _predicates.Add(m => m.ExplicitInterfaceImplementations.Length > 0 ||
                             m.ContainingType?.FindImplementationForInterfaceMember(m) is not null);
        return this;
    }

    public MethodPatternBuilder Where(Func<IMethodSymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    public SymbolPattern<IMethodSymbol> Build()
    {
        var predicates = _predicates.ToArray();
        return new PredicatePattern<IMethodSymbol>(m =>
        {
            foreach (var predicate in predicates)
            {
                if (!predicate(m))
                    return false;
            }
            return true;
        });
    }

    public static implicit operator SymbolPattern<IMethodSymbol>(MethodPatternBuilder builder) => builder.Build();
}
