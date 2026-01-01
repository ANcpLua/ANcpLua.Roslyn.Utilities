using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Matching;

/// <summary>
///     Entry point for fluent symbol matching DSL.
/// </summary>
public static class Match
{
    /// <summary>Creates a method matcher.</summary>
    public static MethodMatcher Method() => new();

    /// <summary>Creates a method matcher for a specific name.</summary>
    public static MethodMatcher Method(string name) => new MethodMatcher().Named(name);

    /// <summary>Creates a type matcher.</summary>
    public static TypeMatcher Type() => new();

    /// <summary>Creates a type matcher for a specific name.</summary>
    public static TypeMatcher Type(string name) => new TypeMatcher().Named(name);

    /// <summary>Creates a property matcher.</summary>
    public static PropertyMatcher Property() => new();

    /// <summary>Creates a property matcher for a specific name.</summary>
    public static PropertyMatcher Property(string name) => new PropertyMatcher().Named(name);

    /// <summary>Creates a field matcher.</summary>
    public static FieldMatcher Field() => new();

    /// <summary>Creates a field matcher for a specific name.</summary>
    public static FieldMatcher Field(string name) => new FieldMatcher().Named(name);

    /// <summary>Creates a parameter matcher.</summary>
    public static ParameterMatcher Parameter() => new();
}

/// <summary>
///     Base class for all symbol matchers providing common functionality.
/// </summary>
public abstract class SymbolMatcherBase<TSelf, TSymbol>
    where TSelf : SymbolMatcherBase<TSelf, TSymbol>
    where TSymbol : class, ISymbol
{
    private readonly List<Func<TSymbol, bool>> _predicates = [];

    /// <summary>Adds a custom predicate to the matcher.</summary>
    protected TSelf AddPredicate(Func<TSymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return (TSelf)this;
    }

    /// <summary>Tests if a symbol matches all predicates.</summary>
    public bool Matches(ISymbol? symbol) => symbol is TSymbol typed && MatchesAll(typed);

    /// <summary>Tests if a symbol matches all predicates.</summary>
    public bool Matches(TSymbol? symbol) => symbol is not null && MatchesAll(symbol);

    private bool MatchesAll(TSymbol symbol)
    {
        foreach (var predicate in _predicates)
        {
            if (!predicate(symbol))
                return false;
        }

        return true;
    }

    // Name matching
    /// <summary>Matches symbols with the exact name.</summary>
    public TSelf Named(string name) => AddPredicate(s => s.Name == name);

    /// <summary>Matches symbols whose name matches a regex pattern.</summary>
    public TSelf NameMatches(string pattern) =>
        AddPredicate(s => Regex.IsMatch(s.Name, pattern, RegexOptions.None, TimeSpan.FromSeconds(1)));

    /// <summary>Matches symbols whose name starts with prefix.</summary>
    public TSelf NameStartsWith(string prefix) =>
        AddPredicate(s => s.Name.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>Matches symbols whose name ends with suffix.</summary>
    public TSelf NameEndsWith(string suffix) =>
        AddPredicate(s => s.Name.EndsWith(suffix, StringComparison.Ordinal));

    /// <summary>Matches symbols whose name contains substring.</summary>
    public TSelf NameContains(string substring) =>
        AddPredicate(s => s.Name.Contains(substring, StringComparison.Ordinal));

    // Accessibility
    /// <summary>Matches public symbols.</summary>
    public TSelf Public() => AddPredicate(s => s.DeclaredAccessibility == Accessibility.Public);

    /// <summary>Matches private symbols.</summary>
    public TSelf Private() => AddPredicate(s => s.DeclaredAccessibility == Accessibility.Private);

    /// <summary>Matches internal symbols.</summary>
    public TSelf Internal() => AddPredicate(s => s.DeclaredAccessibility == Accessibility.Internal);

    /// <summary>Matches protected symbols.</summary>
    public TSelf Protected() => AddPredicate(s => s.DeclaredAccessibility == Accessibility.Protected);

    /// <summary>Matches symbols visible outside assembly.</summary>
    public TSelf VisibleOutsideAssembly() => AddPredicate(s => s.IsVisibleOutsideOfAssembly());

    // Modifiers
    /// <summary>Matches static symbols.</summary>
    public TSelf Static() => AddPredicate(s => s.IsStatic);

    /// <summary>Matches non-static symbols.</summary>
    public TSelf NotStatic() => AddPredicate(s => !s.IsStatic);

    /// <summary>Matches abstract symbols.</summary>
    public TSelf Abstract() => AddPredicate(s => s.IsAbstract);

    /// <summary>Matches sealed symbols.</summary>
    public TSelf Sealed() => AddPredicate(s => s.IsSealed);

    /// <summary>Matches virtual symbols.</summary>
    public TSelf Virtual() => AddPredicate(s => s.IsVirtual);

    /// <summary>Matches override symbols.</summary>
    public TSelf Override() => AddPredicate(s => s.IsOverride);

    // Attributes
    /// <summary>Matches symbols with the specified attribute.</summary>
    public TSelf WithAttribute(string fullyQualifiedName) =>
        AddPredicate(s => s.HasAttribute(fullyQualifiedName));

    /// <summary>Matches symbols without the specified attribute.</summary>
    public TSelf WithoutAttribute(string fullyQualifiedName) =>
        AddPredicate(s => !s.HasAttribute(fullyQualifiedName));

    // Location
    /// <summary>Matches symbols declared in a type with the specified name.</summary>
    public TSelf DeclaredIn(string typeName) => AddPredicate(s => s.ContainingType?.Name == typeName);

    /// <summary>Matches symbols declared in the specified namespace.</summary>
    public TSelf InNamespace(string namespaceName) =>
        AddPredicate(s => s.ContainingNamespace?.ToDisplayString() == namespaceName);

    /// <summary>Adds a custom matching condition.</summary>
    public TSelf Where(Func<TSymbol, bool> predicate) => AddPredicate(predicate);
}

/// <summary>
///     Fluent matcher for IMethodSymbol.
/// </summary>
public sealed class MethodMatcher : SymbolMatcherBase<MethodMatcher, IMethodSymbol>
{
    /// <summary>Matches constructors.</summary>
    public MethodMatcher Constructor() => AddPredicate(m => m.MethodKind == MethodKind.Constructor);

    /// <summary>Matches async methods.</summary>
    public MethodMatcher Async() => AddPredicate(m => m.IsAsync);

    /// <summary>Matches non-async methods.</summary>
    public MethodMatcher NotAsync() => AddPredicate(m => !m.IsAsync);

    /// <summary>Matches extension methods.</summary>
    public MethodMatcher Extension() => AddPredicate(m => m.IsExtensionMethod);

    /// <summary>Matches non-extension methods.</summary>
    public MethodMatcher NotExtension() => AddPredicate(m => !m.IsExtensionMethod);

    /// <summary>Matches generic methods.</summary>
    public MethodMatcher Generic() => AddPredicate(m => m.IsGenericMethod);

    /// <summary>Matches non-generic methods.</summary>
    public MethodMatcher NotGeneric() => AddPredicate(m => !m.IsGenericMethod);

    /// <summary>Matches methods with specified number of type parameters.</summary>
    public MethodMatcher WithTypeParameters(int count) => AddPredicate(m => m.TypeParameters.Length == count);

    /// <summary>Matches methods with no parameters.</summary>
    public MethodMatcher WithNoParameters() => AddPredicate(m => m.Parameters.Length == 0);

    /// <summary>Matches methods with specified number of parameters.</summary>
    public MethodMatcher WithParameters(int count) => AddPredicate(m => m.Parameters.Length == count);

    /// <summary>Matches methods with at least specified number of parameters.</summary>
    public MethodMatcher WithMinParameters(int count) => AddPredicate(m => m.Parameters.Length >= count);

    /// <summary>Matches methods with a CancellationToken parameter.</summary>
    public MethodMatcher WithCancellationToken() =>
        AddPredicate(m => HasParameterOfType(m.Parameters, "CancellationToken"));

    /// <summary>Matches void methods.</summary>
    public MethodMatcher ReturningVoid() => AddPredicate(m => m.ReturnsVoid);

    /// <summary>Matches methods returning type with specified name.</summary>
    public MethodMatcher Returning(string typeName) => AddPredicate(m => m.ReturnType.Name == typeName);

    /// <summary>Matches methods returning Task or ValueTask.</summary>
    public MethodMatcher ReturningTask() =>
        AddPredicate(m => m.ReturnType.Name is "Task" or "ValueTask" ||
                         m.ReturnType.OriginalDefinition.Name is "Task" or "ValueTask");

    /// <summary>Matches methods returning bool.</summary>
    public MethodMatcher ReturningBool() =>
        AddPredicate(m => m.ReturnType.SpecialType == SpecialType.System_Boolean);

    /// <summary>Matches methods returning string.</summary>
    public MethodMatcher ReturningString() =>
        AddPredicate(m => m.ReturnType.SpecialType == SpecialType.System_String);

    /// <summary>Matches explicit interface implementations.</summary>
    public MethodMatcher ExplicitImplementation() =>
        AddPredicate(m => m.ExplicitInterfaceImplementations.Length > 0);

    private static bool HasParameterOfType(ImmutableArray<IParameterSymbol> parameters, string typeName)
    {
        foreach (var param in parameters)
        {
            if (param.Type.Name == typeName)
                return true;
        }

        return false;
    }
}

/// <summary>
///     Fluent matcher for INamedTypeSymbol.
/// </summary>
public sealed class TypeMatcher : SymbolMatcherBase<TypeMatcher, INamedTypeSymbol>
{
    /// <summary>Matches classes.</summary>
    public TypeMatcher Class() => AddPredicate(t => t.TypeKind == TypeKind.Class);

    /// <summary>Matches structs.</summary>
    public TypeMatcher Struct() => AddPredicate(t => t.TypeKind == TypeKind.Struct);

    /// <summary>Matches interfaces.</summary>
    public TypeMatcher Interface() => AddPredicate(t => t.TypeKind == TypeKind.Interface);

    /// <summary>Matches enums.</summary>
    public TypeMatcher Enum() => AddPredicate(t => t.TypeKind == TypeKind.Enum);

    /// <summary>Matches records.</summary>
    public TypeMatcher Record() => AddPredicate(t => t.IsRecord);

    /// <summary>Matches generic types.</summary>
    public TypeMatcher Generic() => AddPredicate(t => t.IsGenericType);

    /// <summary>Matches non-generic types.</summary>
    public TypeMatcher NotGeneric() => AddPredicate(t => !t.IsGenericType);

    /// <summary>Matches types that inherit from base type.</summary>
    public TypeMatcher InheritsFrom(string baseTypeName) =>
        AddPredicate(t => InheritsFromName(t, baseTypeName));

    /// <summary>Matches types that implement interface.</summary>
    public TypeMatcher Implements(string interfaceName) =>
        AddPredicate(t => ImplementsInterface(t, interfaceName));

    /// <summary>Matches types implementing IDisposable.</summary>
    public TypeMatcher Disposable() => Implements("System.IDisposable");

    /// <summary>Matches nested types.</summary>
    public TypeMatcher Nested() => AddPredicate(t => t.ContainingType is not null);

    /// <summary>Matches top-level types.</summary>
    public TypeMatcher TopLevel() => AddPredicate(t => t.ContainingType is null);

    /// <summary>Matches static classes.</summary>
    public TypeMatcher StaticClass() => AddPredicate(t => t.IsStatic && t.TypeKind == TypeKind.Class);

    /// <summary>Matches types with a member of specified name.</summary>
    public TypeMatcher HasMember(string name) => AddPredicate(t => t.GetMembers(name).Length > 0);

    /// <summary>Matches types with parameterless constructor.</summary>
    public TypeMatcher HasParameterlessConstructor() =>
        AddPredicate(t => HasParameterlessCtor(t));

    private static bool InheritsFromName(INamedTypeSymbol type, string name)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.Name == name || current.ToDisplayString() == name)
                return true;
            current = current.BaseType;
        }

        return false;
    }

    private static bool ImplementsInterface(INamedTypeSymbol type, string name)
    {
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.Name == name || iface.ToDisplayString() == name)
                return true;
        }

        return false;
    }

    private static bool HasParameterlessCtor(INamedTypeSymbol type)
    {
        foreach (var ctor in type.InstanceConstructors)
        {
            if (ctor.Parameters.Length == 0)
                return true;
        }

        return false;
    }
}

/// <summary>
///     Fluent matcher for IPropertySymbol.
/// </summary>
public sealed class PropertyMatcher : SymbolMatcherBase<PropertyMatcher, IPropertySymbol>
{
    /// <summary>Matches properties with a getter.</summary>
    public PropertyMatcher WithGetter() => AddPredicate(p => p.GetMethod is not null);

    /// <summary>Matches properties with a setter.</summary>
    public PropertyMatcher WithSetter() => AddPredicate(p => p.SetMethod is not null);

    /// <summary>Matches properties with init-only setter.</summary>
    public PropertyMatcher WithInitSetter() => AddPredicate(p => p.SetMethod?.IsInitOnly == true);

    /// <summary>Matches read-only properties.</summary>
    public PropertyMatcher ReadOnly() => AddPredicate(p => p.GetMethod is not null && p.SetMethod is null);

    /// <summary>Matches indexers.</summary>
    public PropertyMatcher Indexer() => AddPredicate(p => p.IsIndexer);

    /// <summary>Matches required properties.</summary>
    public PropertyMatcher Required() => AddPredicate(p => p.IsRequired);

    /// <summary>Matches properties with specified type name.</summary>
    public PropertyMatcher OfType(string typeName) => AddPredicate(p => p.Type.Name == typeName);
}

/// <summary>
///     Fluent matcher for IFieldSymbol.
/// </summary>
public sealed class FieldMatcher : SymbolMatcherBase<FieldMatcher, IFieldSymbol>
{
    /// <summary>Matches const fields.</summary>
    public FieldMatcher Const() => AddPredicate(f => f.IsConst);

    /// <summary>Matches readonly fields.</summary>
    public FieldMatcher ReadOnly() => AddPredicate(f => f.IsReadOnly);

    /// <summary>Matches volatile fields.</summary>
    public FieldMatcher Volatile() => AddPredicate(f => f.IsVolatile);

    /// <summary>Matches fields with specified type name.</summary>
    public FieldMatcher OfType(string typeName) => AddPredicate(f => f.Type.Name == typeName);

    /// <summary>Matches backing fields.</summary>
    public FieldMatcher BackingField() => AddPredicate(f => f.AssociatedSymbol is not null);

    /// <summary>Matches non-backing fields.</summary>
    public FieldMatcher NotBackingField() => AddPredicate(f => f.AssociatedSymbol is null);
}

/// <summary>
///     Fluent matcher for IParameterSymbol.
/// </summary>
public sealed class ParameterMatcher : SymbolMatcherBase<ParameterMatcher, IParameterSymbol>
{
    /// <summary>Matches ref parameters.</summary>
    public ParameterMatcher Ref() => AddPredicate(p => p.RefKind == RefKind.Ref);

    /// <summary>Matches out parameters.</summary>
    public ParameterMatcher Out() => AddPredicate(p => p.RefKind == RefKind.Out);

    /// <summary>Matches in parameters.</summary>
    public ParameterMatcher In() => AddPredicate(p => p.RefKind == RefKind.In);

    /// <summary>Matches params array parameters.</summary>
    public ParameterMatcher Params() => AddPredicate(p => p.IsParams);

    /// <summary>Matches optional parameters.</summary>
    public ParameterMatcher Optional() => AddPredicate(p => p.IsOptional);

    /// <summary>Matches parameters with specified type name.</summary>
    public ParameterMatcher OfType(string typeName) => AddPredicate(p => p.Type.Name == typeName);

    /// <summary>Matches CancellationToken parameters.</summary>
    public ParameterMatcher CancellationToken() => OfType("CancellationToken");
}
