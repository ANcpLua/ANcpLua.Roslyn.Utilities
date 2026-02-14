using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Matching;

/// <summary>
///     Entry point for fluent symbol matching DSL.
/// </summary>
/// <remarks>
///     <para>
///         Provides factory methods for creating strongly-typed symbol matchers
///         that can be composed using a fluent API.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Use <see cref="Method()" /> or <see cref="Method(string)" /> for matching
///                 <see cref="IMethodSymbol" /> instances.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Use <see cref="Type()" /> or <see cref="Type(string)" /> for matching
///                 <see cref="INamedTypeSymbol" /> instances.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Use <see cref="Property()" /> or <see cref="Property(string)" /> for matching
///                 <see cref="IPropertySymbol" /> instances.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Use <see cref="Field()" /> or <see cref="Field(string)" /> for matching
///                 <see cref="IFieldSymbol" /> instances.
///             </description>
///         </item>
///         <item>
///             <description>Use <see cref="Parameter()" /> for matching <see cref="IParameterSymbol" /> instances.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="MethodMatcher" />
/// <seealso cref="TypeMatcher" />
/// <seealso cref="PropertyMatcher" />
/// <seealso cref="FieldMatcher" />
/// <seealso cref="ParameterMatcher" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class Match
{
    /// <summary>
    ///     Creates a new method matcher with no initial constraints.
    /// </summary>
    /// <returns>A new <see cref="MethodMatcher" /> instance.</returns>
    /// <seealso cref="Method(string)" />
    public static MethodMatcher Method() => new();

    /// <summary>
    ///     Creates a new method matcher that matches methods with the specified name.
    /// </summary>
    /// <param name="name">The exact name to match against the method's <see cref="ISymbol.Name" />.</param>
    /// <returns>A new <see cref="MethodMatcher" /> instance configured to match methods with the specified name.</returns>
    /// <seealso cref="Method()" />
    public static MethodMatcher Method(string name) => new MethodMatcher().Named(name);

    /// <summary>
    ///     Creates a new type matcher with no initial constraints.
    /// </summary>
    /// <returns>A new <see cref="TypeMatcher" /> instance.</returns>
    /// <seealso cref="Type(string)" />
    public static TypeMatcher Type() => new();

    /// <summary>
    ///     Creates a new type matcher that matches types with the specified name.
    /// </summary>
    /// <param name="name">The exact name to match against the type's <see cref="ISymbol.Name" />.</param>
    /// <returns>A new <see cref="TypeMatcher" /> instance configured to match types with the specified name.</returns>
    /// <seealso cref="Type()" />
    public static TypeMatcher Type(string name) => new TypeMatcher().Named(name);

    /// <summary>
    ///     Creates a new property matcher with no initial constraints.
    /// </summary>
    /// <returns>A new <see cref="PropertyMatcher" /> instance.</returns>
    /// <seealso cref="Property(string)" />
    public static PropertyMatcher Property() => new();

    /// <summary>
    ///     Creates a new property matcher that matches properties with the specified name.
    /// </summary>
    /// <param name="name">The exact name to match against the property's <see cref="ISymbol.Name" />.</param>
    /// <returns>A new <see cref="PropertyMatcher" /> instance configured to match properties with the specified name.</returns>
    /// <seealso cref="Property()" />
    public static PropertyMatcher Property(string name) => new PropertyMatcher().Named(name);

    /// <summary>
    ///     Creates a new field matcher with no initial constraints.
    /// </summary>
    /// <returns>A new <see cref="FieldMatcher" /> instance.</returns>
    /// <seealso cref="Field(string)" />
    public static FieldMatcher Field() => new();

    /// <summary>
    ///     Creates a new field matcher that matches fields with the specified name.
    /// </summary>
    /// <param name="name">The exact name to match against the field's <see cref="ISymbol.Name" />.</param>
    /// <returns>A new <see cref="FieldMatcher" /> instance configured to match fields with the specified name.</returns>
    /// <seealso cref="Field()" />
    public static FieldMatcher Field(string name) => new FieldMatcher().Named(name);

    /// <summary>
    ///     Creates a new parameter matcher with no initial constraints.
    /// </summary>
    /// <returns>A new <see cref="ParameterMatcher" /> instance.</returns>
    public static ParameterMatcher Parameter() => new();
}

/// <summary>
///     Base class for all symbol matchers providing common functionality.
/// </summary>
/// <remarks>
///     <para>
///         This abstract class provides the foundational predicate-based matching
///         infrastructure used by all concrete matcher types.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Predicates are added via the fluent API and evaluated in order.</description>
///         </item>
///         <item>
///             <description>All predicates must pass for a symbol to be considered a match.</description>
///         </item>
///         <item>
///             <description>The matcher uses the Curiously Recurring Template Pattern (CRTP) for fluent method chaining.</description>
///         </item>
///     </list>
/// </remarks>
/// <typeparam name="TSelf">The concrete matcher type, used for fluent method chaining.</typeparam>
/// <typeparam name="TSymbol">The type of symbol this matcher operates on, must implement <see cref="ISymbol" />.</typeparam>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    abstract class SymbolMatcherBase<TSelf, TSymbol>
    where TSelf : SymbolMatcherBase<TSelf, TSymbol>
    where TSymbol : class, ISymbol
{
    private readonly List<Func<TSymbol, bool>> _predicates = [];

    /// <summary>
    ///     Adds a custom predicate to the matcher's collection.
    /// </summary>
    /// <param name="predicate">A function that returns <c>true</c> if the symbol matches the condition.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    protected TSelf AddPredicate(Func<TSymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return (TSelf)this;
    }

    /// <summary>
    ///     Tests if the specified symbol matches all configured predicates.
    /// </summary>
    /// <param name="symbol">The symbol to test, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is of type <typeparamref name="TSymbol" />
    ///     and matches all predicates; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Matches(TSymbol?)" />
    public bool Matches(ISymbol? symbol) => symbol is TSymbol typed && MatchesAll(typed);

    /// <summary>
    ///     Tests if the specified strongly-typed symbol matches all configured predicates.
    /// </summary>
    /// <param name="symbol">The symbol to test, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is not <c>null</c>
    ///     and matches all predicates; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Matches(ISymbol?)" />
    public bool Matches(TSymbol? symbol) => symbol is not null && MatchesAll(symbol);

    private bool MatchesAll(TSymbol symbol)
    {
        foreach (var predicate in _predicates)
            if (!predicate(symbol))
                return false;

        return true;
    }

    /// <summary>
    ///     Matches symbols with the exact specified name.
    /// </summary>
    /// <param name="name">The exact name to match against <see cref="ISymbol.Name" />.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="NameMatches" />
    /// <seealso cref="NameStartsWith" />
    /// <seealso cref="NameEndsWith" />
    /// <seealso cref="NameContains" />
    public TSelf Named(string name)
    {
        return AddPredicate(s => s.Name == name);
    }

    /// <summary>
    ///     Matches symbols whose name matches a regular expression pattern.
    /// </summary>
    /// <param name="pattern">The regex pattern to match against <see cref="ISymbol.Name" />.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <remarks>
    ///     The regex evaluation has a timeout of 1 second to prevent ReDoS attacks.
    /// </remarks>
    /// <seealso cref="Named" />
    public TSelf NameMatches(string pattern)
    {
        return AddPredicate(s => Regex.IsMatch(s.Name, pattern, RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    ///     Matches symbols whose name starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match at the start of <see cref="ISymbol.Name" />.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Named" />
    /// <seealso cref="NameEndsWith" />
    public TSelf NameStartsWith(string prefix)
    {
        return AddPredicate(s => s.Name.StartsWithOrdinal(prefix));
    }

    /// <summary>
    ///     Matches symbols whose name ends with the specified suffix.
    /// </summary>
    /// <param name="suffix">The suffix to match at the end of <see cref="ISymbol.Name" />.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Named" />
    /// <seealso cref="NameStartsWith" />
    public TSelf NameEndsWith(string suffix)
    {
        return AddPredicate(s => s.Name.EndsWithOrdinal(suffix));
    }

    /// <summary>
    ///     Matches symbols whose name contains the specified substring.
    /// </summary>
    /// <param name="substring">The substring to search for within <see cref="ISymbol.Name" />.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Named" />
    public TSelf NameContains(string substring)
    {
        return AddPredicate(s => s.Name.Contains(substring, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Matches symbols with <see cref="Accessibility.Public" /> accessibility.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Private" />
    /// <seealso cref="Internal" />
    /// <seealso cref="Protected" />
    /// <seealso cref="VisibleOutsideAssembly" />
    public TSelf Public()
    {
        return AddPredicate(static s => s.DeclaredAccessibility == Accessibility.Public);
    }

    /// <summary>
    ///     Matches symbols with <see cref="Accessibility.Private" /> accessibility.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Public" />
    /// <seealso cref="Internal" />
    /// <seealso cref="Protected" />
    public TSelf Private()
    {
        return AddPredicate(static s => s.DeclaredAccessibility == Accessibility.Private);
    }

    /// <summary>
    ///     Matches symbols with <see cref="Accessibility.Internal" /> accessibility.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Public" />
    /// <seealso cref="Private" />
    /// <seealso cref="Protected" />
    public TSelf Internal()
    {
        return AddPredicate(static s => s.DeclaredAccessibility == Accessibility.Internal);
    }

    /// <summary>
    ///     Matches symbols with <see cref="Accessibility.Protected" /> accessibility.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Public" />
    /// <seealso cref="Private" />
    /// <seealso cref="Internal" />
    public TSelf Protected()
    {
        return AddPredicate(static s => s.DeclaredAccessibility == Accessibility.Protected);
    }

    /// <summary>
    ///     Matches symbols that are visible outside their containing assembly.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <remarks>
    ///     This includes public symbols and protected symbols in non-sealed types.
    /// </remarks>
    /// <seealso cref="Public" />
    public TSelf VisibleOutsideAssembly()
    {
        return AddPredicate(static s => s.IsVisibleOutsideOfAssembly());
    }

    /// <summary>
    ///     Matches static symbols.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="NotStatic" />
    public TSelf Static()
    {
        return AddPredicate(static s => s.IsStatic);
    }

    /// <summary>
    ///     Matches non-static (instance) symbols.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Static" />
    public TSelf NotStatic()
    {
        return AddPredicate(static s => !s.IsStatic);
    }

    /// <summary>
    ///     Matches abstract symbols.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Virtual" />
    /// <seealso cref="Override" />
    public TSelf Abstract()
    {
        return AddPredicate(static s => s.IsAbstract);
    }

    /// <summary>
    ///     Matches sealed symbols.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Virtual" />
    public TSelf Sealed()
    {
        return AddPredicate(static s => s.IsSealed);
    }

    /// <summary>
    ///     Matches virtual symbols.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Abstract" />
    /// <seealso cref="Override" />
    public TSelf Virtual()
    {
        return AddPredicate(static s => s.IsVirtual);
    }

    /// <summary>
    ///     Matches override symbols.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Virtual" />
    /// <seealso cref="Abstract" />
    public TSelf Override()
    {
        return AddPredicate(static s => s.IsOverride);
    }

    /// <summary>
    ///     Matches symbols that have the specified attribute.
    /// </summary>
    /// <param name="fullyQualifiedName">
    ///     The fully qualified name of the attribute type (e.g., <c>"System.ObsoleteAttribute"</c>).
    /// </param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithAttribute(string, string[])" />
    /// <seealso cref="WithoutAttribute" />
    public TSelf WithAttribute(string fullyQualifiedName)
    {
        return AddPredicate(s => s.HasAttribute(fullyQualifiedName));
    }

    /// <summary>
    ///     Matches symbols that have any of the specified attributes.
    /// </summary>
    /// <param name="fullyQualifiedName">
    ///     The first fully qualified attribute name to match.
    /// </param>
    /// <param name="additionalNames">
    ///     Additional fully qualified attribute names to match.
    /// </param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithAttribute(string)" />
    /// <seealso cref="WithoutAttribute" />
    public TSelf WithAttribute(string fullyQualifiedName, params string[] additionalNames)
    {
        return AddPredicate(s =>
        {
            foreach (var attribute in s.GetAttributes())
            {
                var attrName = attribute.AttributeClass?.ToDisplayString();
                if (attrName is null)
                    continue;

                if (attrName == fullyQualifiedName)
                    return true;

                foreach (var name in additionalNames)
                    if (attrName == name)
                        return true;
            }

            return false;
        });
    }

    /// <summary>
    ///     Matches symbols that do not have the specified attribute.
    /// </summary>
    /// <param name="fullyQualifiedName">
    ///     The fully qualified name of the attribute type (e.g., <c>"System.ObsoleteAttribute"</c>).
    /// </param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithAttribute(string)" />
    public TSelf WithoutAttribute(string fullyQualifiedName)
    {
        return AddPredicate(s => !s.HasAttribute(fullyQualifiedName));
    }

    /// <summary>
    ///     Matches symbols declared in a type with the specified name.
    /// </summary>
    /// <param name="typeName">The name of the containing type.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="InNamespace" />
    public TSelf DeclaredIn(string typeName)
    {
        return AddPredicate(s => s.ContainingType?.Name == typeName);
    }

    /// <summary>
    ///     Matches symbols declared in the specified namespace.
    /// </summary>
    /// <param name="namespaceName">
    ///     The fully qualified namespace name (e.g., <c>"System.Collections.Generic"</c>).
    /// </param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="DeclaredIn" />
    public TSelf InNamespace(string namespaceName)
    {
        return AddPredicate(s => s.ContainingNamespace?.ToDisplayString() == namespaceName);
    }

    /// <summary>
    ///     Adds a custom matching condition using a predicate function.
    /// </summary>
    /// <param name="predicate">
    ///     A function that returns <c>true</c> if the symbol should be considered a match.
    /// </param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public TSelf Where(Func<TSymbol, bool> predicate) => AddPredicate(predicate);
}

/// <summary>
///     Fluent matcher for <see cref="IMethodSymbol" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Provides method-specific matching predicates in addition to the common
///         symbol matching functionality from <see cref="SymbolMatcherBase{TSelf, TSymbol}" />.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Match by method kind (constructor, async, extension, generic).</description>
///         </item>
///         <item>
///             <description>Match by parameter count and types.</description>
///         </item>
///         <item>
///             <description>Match by return type.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Match.Method()" />
/// <seealso cref="Match.Method(string)" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class MethodMatcher : SymbolMatcherBase<MethodMatcher, IMethodSymbol>
{
    /// <summary>
    ///     Matches constructor methods.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Finalizer" />
    public MethodMatcher Constructor()
    {
        return AddPredicate(static m => m.MethodKind == MethodKind.Constructor);
    }

    /// <summary>
    ///     Matches finalizer (destructor) methods.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Constructor" />
    public MethodMatcher Finalizer()
    {
        return AddPredicate(static m => m.MethodKind == MethodKind.Destructor);
    }

    /// <summary>
    ///     Matches async methods (methods declared with the <c>async</c> keyword).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="NotAsync" />
    public MethodMatcher Async()
    {
        return AddPredicate(static m => m.IsAsync);
    }

    /// <summary>
    ///     Matches non-async methods.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Async" />
    public MethodMatcher NotAsync()
    {
        return AddPredicate(static m => !m.IsAsync);
    }

    /// <summary>
    ///     Matches extension methods.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="NotExtension" />
    public MethodMatcher Extension()
    {
        return AddPredicate(static m => m.IsExtensionMethod);
    }

    /// <summary>
    ///     Matches non-extension methods.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Extension" />
    public MethodMatcher NotExtension()
    {
        return AddPredicate(static m => !m.IsExtensionMethod);
    }

    /// <summary>
    ///     Matches generic methods.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="NotGeneric" />
    /// <seealso cref="WithTypeParameters" />
    public MethodMatcher Generic()
    {
        return AddPredicate(static m => m.IsGenericMethod);
    }

    /// <summary>
    ///     Matches non-generic methods.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Generic" />
    public MethodMatcher NotGeneric()
    {
        return AddPredicate(static m => !m.IsGenericMethod);
    }

    /// <summary>
    ///     Matches methods with the specified number of type parameters.
    /// </summary>
    /// <param name="count">The exact number of type parameters required.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Generic" />
    public MethodMatcher WithTypeParameters(int count)
    {
        return AddPredicate(m => m.TypeParameters.Length == count);
    }

    /// <summary>
    ///     Matches methods with no parameters.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithParameters" />
    /// <seealso cref="WithMinParameters" />
    public MethodMatcher WithNoParameters()
    {
        return AddPredicate(static m => m.Parameters.Length is 0);
    }

    /// <summary>
    ///     Matches methods with the specified number of parameters.
    /// </summary>
    /// <param name="count">The exact number of parameters required.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithNoParameters" />
    /// <seealso cref="WithMinParameters" />
    public MethodMatcher WithParameters(int count)
    {
        return AddPredicate(m => m.Parameters.Length == count);
    }

    /// <summary>
    ///     Matches methods with at least the specified number of parameters.
    /// </summary>
    /// <param name="count">The minimum number of parameters required.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithParameters" />
    /// <seealso cref="WithNoParameters" />
    public MethodMatcher WithMinParameters(int count)
    {
        return AddPredicate(m => m.Parameters.Length >= count);
    }

    /// <summary>
    ///     Matches methods that have a <see cref="System.Threading.CancellationToken" /> parameter.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public MethodMatcher WithCancellationToken()
    {
        return AddPredicate(static m => HasParameterOfType(m.Parameters, "CancellationToken"));
    }

    /// <summary>
    ///     Matches methods that return <c>void</c>.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Returning" />
    /// <seealso cref="ReturningTask" />
    public MethodMatcher ReturningVoid()
    {
        return AddPredicate(static m => m.ReturnsVoid);
    }

    /// <summary>
    ///     Matches methods returning a type with the specified name.
    /// </summary>
    /// <param name="typeName">The name of the return type to match.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="ReturningVoid" />
    /// <seealso cref="ReturningTask" />
    /// <seealso cref="ReturningBool" />
    /// <seealso cref="ReturningString" />
    public MethodMatcher Returning(string typeName)
    {
        return AddPredicate(m => m.ReturnType.Name == typeName);
    }

    /// <summary>
    ///     Matches methods returning <see cref="System.Threading.Tasks.Task" /> or
    ///     <see cref="System.Threading.Tasks.ValueTask" />.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <remarks>
    ///     Matches both generic and non-generic Task/ValueTask types.
    /// </remarks>
    /// <seealso cref="Async" />
    /// <seealso cref="ReturningVoid" />
    public MethodMatcher ReturningTask()
    {
        return AddPredicate(static m => m.ReturnType.Name is "Task" or "ValueTask" ||
                                 m.ReturnType.OriginalDefinition.Name is "Task" or "ValueTask");
    }

    /// <summary>
    ///     Matches methods returning <see cref="bool" />.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Returning" />
    public MethodMatcher ReturningBool()
    {
        return AddPredicate(static m => m.ReturnType.SpecialType == SpecialType.System_Boolean);
    }

    /// <summary>
    ///     Matches methods returning <see cref="string" />.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Returning" />
    public MethodMatcher ReturningString()
    {
        return AddPredicate(static m => m.ReturnType.SpecialType == SpecialType.System_String);
    }

    /// <summary>
    ///     Matches explicit interface implementation methods.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public MethodMatcher ExplicitImplementation()
    {
        return AddPredicate(static m => m.ExplicitInterfaceImplementations.Length > 0);
    }

    private static bool HasParameterOfType(ImmutableArray<IParameterSymbol> parameters, string typeName)
    {
        foreach (var param in parameters)
            if (param.Type.Name == typeName)
                return true;

        return false;
    }
}

/// <summary>
///     Fluent matcher for <see cref="INamedTypeSymbol" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Provides type-specific matching predicates in addition to the common
///         symbol matching functionality from <see cref="SymbolMatcherBase{TSelf, TSymbol}" />.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Match by type kind (class, struct, interface, enum, record).</description>
///         </item>
///         <item>
///             <description>Match by inheritance hierarchy.</description>
///         </item>
///         <item>
///             <description>Match by interface implementations.</description>
///         </item>
///         <item>
///             <description>Match by nesting level and member presence.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Match.Type()" />
/// <seealso cref="Match.Type(string)" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class TypeMatcher : SymbolMatcherBase<TypeMatcher, INamedTypeSymbol>
{
    /// <summary>
    ///     Matches class types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Struct" />
    /// <seealso cref="Interface" />
    /// <seealso cref="Enum" />
    /// <seealso cref="Record" />
    public TypeMatcher Class()
    {
        return AddPredicate(static t => t.TypeKind == TypeKind.Class);
    }

    /// <summary>
    ///     Matches struct types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Class" />
    /// <seealso cref="Record" />
    public TypeMatcher Struct()
    {
        return AddPredicate(static t => t.TypeKind == TypeKind.Struct);
    }

    /// <summary>
    ///     Matches interface types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Class" />
    public TypeMatcher Interface()
    {
        return AddPredicate(static t => t.TypeKind == TypeKind.Interface);
    }

    /// <summary>
    ///     Matches enum types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Class" />
    /// <seealso cref="Struct" />
    public TypeMatcher Enum()
    {
        return AddPredicate(static t => t.TypeKind == TypeKind.Enum);
    }

    /// <summary>
    ///     Matches record types (both class and struct records).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Class" />
    /// <seealso cref="Struct" />
    public TypeMatcher Record()
    {
        return AddPredicate(static t => t.IsRecord);
    }

    /// <summary>
    ///     Matches generic types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="NotGeneric" />
    public TypeMatcher Generic()
    {
        return AddPredicate(static t => t.IsGenericType);
    }

    /// <summary>
    ///     Matches non-generic types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Generic" />
    public TypeMatcher NotGeneric()
    {
        return AddPredicate(static t => !t.IsGenericType);
    }

    /// <summary>
    ///     Matches types that inherit from a base type with the specified name.
    /// </summary>
    /// <param name="baseTypeName">
    ///     The name or fully qualified name of the base type to check for in the inheritance hierarchy.
    /// </param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <remarks>
    ///     Traverses the entire inheritance hierarchy, not just the immediate base type.
    /// </remarks>
    /// <seealso cref="Implements" />
    public TypeMatcher InheritsFrom(string baseTypeName)
    {
        return AddPredicate(t => InheritsFromName(t, baseTypeName));
    }

    /// <summary>
    ///     Matches types that implement an interface with the specified name.
    /// </summary>
    /// <param name="interfaceName">
    ///     The name or fully qualified name of the interface to check for.
    /// </param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <remarks>
    ///     Checks all interfaces including inherited ones via <see cref="INamedTypeSymbol" />.<c>AllInterfaces</c>.
    /// </remarks>
    /// <seealso cref="InheritsFrom" />
    /// <seealso cref="Disposable" />
    public TypeMatcher Implements(string interfaceName)
    {
        return AddPredicate(t => ImplementsInterface(t, interfaceName));
    }

    /// <summary>
    ///     Matches types that implement <see cref="System.IDisposable" />.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Implements" />
    public TypeMatcher Disposable() => Implements("System.IDisposable");

    /// <summary>
    ///     Matches nested types (types declared within another type).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="TopLevel" />
    public TypeMatcher Nested()
    {
        return AddPredicate(static t => t.ContainingType is not null);
    }

    /// <summary>
    ///     Matches top-level types (types not declared within another type).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Nested" />
    public TypeMatcher TopLevel()
    {
        return AddPredicate(static t => t.ContainingType is null);
    }

    /// <summary>
    ///     Matches static classes.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Class" />
    public TypeMatcher StaticClass()
    {
        return AddPredicate(static t => t.IsStatic && t.TypeKind == TypeKind.Class);
    }

    /// <summary>
    ///     Matches types that have a member with the specified name.
    /// </summary>
    /// <param name="name">The name of the member to look for.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="HasParameterlessConstructor" />
    public TypeMatcher HasMember(string name)
    {
        return AddPredicate(t => t.GetMembers(name).Length > 0);
    }

    /// <summary>
    ///     Matches types that have a parameterless constructor.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="HasMember" />
    public TypeMatcher HasParameterlessConstructor()
    {
        return AddPredicate(HasParameterlessCtor);
    }

    private static bool InheritsFromName(ITypeSymbol type, string name)
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

    private static bool ImplementsInterface(ITypeSymbol type, string name)
    {
        if (type is not INamedTypeSymbol namedType)
            return false;

        foreach (var iface in namedType.AllInterfaces)
            if (iface.Name == name || iface.ToDisplayString() == name)
                return true;

        return false;
    }

    private static bool HasParameterlessCtor(INamedTypeSymbol type)
    {
        foreach (var ctor in type.InstanceConstructors)
            if (ctor.Parameters.Length is 0)
                return true;

        return false;
    }
}

/// <summary>
///     Fluent matcher for <see cref="IPropertySymbol" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Provides property-specific matching predicates in addition to the common
///         symbol matching functionality from <see cref="SymbolMatcherBase{TSelf, TSymbol}" />.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Match by accessor presence (getter, setter, init).</description>
///         </item>
///         <item>
///             <description>Match by property characteristics (indexer, required, read-only).</description>
///         </item>
///         <item>
///             <description>Match by property type.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Match.Property()" />
/// <seealso cref="Match.Property(string)" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class PropertyMatcher : SymbolMatcherBase<PropertyMatcher, IPropertySymbol>
{
    /// <summary>
    ///     Matches properties that have a getter accessor.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithSetter" />
    /// <seealso cref="ReadOnly" />
    public PropertyMatcher WithGetter()
    {
        return AddPredicate(static p => p.GetMethod is not null);
    }

    /// <summary>
    ///     Matches properties that have a setter accessor.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithGetter" />
    /// <seealso cref="WithInitSetter" />
    public PropertyMatcher WithSetter()
    {
        return AddPredicate(static p => p.SetMethod is not null);
    }

    /// <summary>
    ///     Matches properties that have an init-only setter.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithSetter" />
    public PropertyMatcher WithInitSetter()
    {
        return AddPredicate(static p => p.SetMethod?.IsInitOnly == true);
    }

    /// <summary>
    ///     Matches read-only properties (properties with a getter but no setter).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithGetter" />
    /// <seealso cref="WithSetter" />
    public PropertyMatcher ReadOnly()
    {
        return AddPredicate(static p => p.GetMethod is not null && p.SetMethod is null);
    }

    /// <summary>
    ///     Matches indexer properties.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public PropertyMatcher Indexer()
    {
        return AddPredicate(static p => p.IsIndexer);
    }

    /// <summary>
    ///     Matches required properties (C# 11+).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public PropertyMatcher Required()
    {
        return AddPredicate(static p => p.IsRequired);
    }

    /// <summary>
    ///     Matches properties with the specified type name.
    /// </summary>
    /// <param name="typeName">The name of the property type to match.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public PropertyMatcher OfType(string typeName)
    {
        return AddPredicate(p => p.Type.Name == typeName);
    }
}

/// <summary>
///     Fluent matcher for <see cref="IFieldSymbol" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Provides field-specific matching predicates in addition to the common
///         symbol matching functionality from <see cref="SymbolMatcherBase{TSelf, TSymbol}" />.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Match by field modifiers (const, readonly, volatile).</description>
///         </item>
///         <item>
///             <description>Match by field type.</description>
///         </item>
///         <item>
///             <description>Match by backing field status.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Match.Field()" />
/// <seealso cref="Match.Field(string)" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class FieldMatcher : SymbolMatcherBase<FieldMatcher, IFieldSymbol>
{
    /// <summary>
    ///     Matches const fields.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="ReadOnly" />
    public FieldMatcher Const()
    {
        return AddPredicate(static f => f.IsConst);
    }

    /// <summary>
    ///     Matches readonly fields.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Const" />
    public FieldMatcher ReadOnly()
    {
        return AddPredicate(static f => f.IsReadOnly);
    }

    /// <summary>
    ///     Matches volatile fields.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public FieldMatcher Volatile()
    {
        return AddPredicate(static f => f.IsVolatile);
    }

    /// <summary>
    ///     Matches fields with the specified type name.
    /// </summary>
    /// <param name="typeName">The name of the field type to match.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public FieldMatcher OfType(string typeName)
    {
        return AddPredicate(f => f.Type.Name == typeName);
    }

    /// <summary>
    ///     Matches compiler-generated backing fields for auto-properties.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="NotBackingField" />
    public FieldMatcher BackingField()
    {
        return AddPredicate(static f => f.AssociatedSymbol is not null);
    }

    /// <summary>
    ///     Matches fields that are not compiler-generated backing fields.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="BackingField" />
    public FieldMatcher NotBackingField()
    {
        return AddPredicate(static f => f.AssociatedSymbol is null);
    }
}

/// <summary>
///     Fluent matcher for <see cref="IParameterSymbol" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Provides parameter-specific matching predicates in addition to the common
///         symbol matching functionality from <see cref="SymbolMatcherBase{TSelf, TSymbol}" />.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Match by parameter passing mode (ref, out, in).</description>
///         </item>
///         <item>
///             <description>Match by parameter modifiers (params, optional).</description>
///         </item>
///         <item>
///             <description>Match by parameter type.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Match.Parameter()" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class ParameterMatcher : SymbolMatcherBase<ParameterMatcher, IParameterSymbol>
{
    /// <summary>
    ///     Matches ref parameters.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Out" />
    /// <seealso cref="In" />
    public ParameterMatcher Ref()
    {
        return AddPredicate(static p => p.RefKind == RefKind.Ref);
    }

    /// <summary>
    ///     Matches out parameters.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Ref" />
    /// <seealso cref="In" />
    public ParameterMatcher Out()
    {
        return AddPredicate(static p => p.RefKind == RefKind.Out);
    }

    /// <summary>
    ///     Matches in (readonly ref) parameters.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Ref" />
    /// <seealso cref="Out" />
    public ParameterMatcher In()
    {
        return AddPredicate(static p => p.RefKind == RefKind.In);
    }

    /// <summary>
    ///     Matches params array parameters.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Optional" />
    public ParameterMatcher Params()
    {
        return AddPredicate(static p => p.IsParams);
    }

    /// <summary>
    ///     Matches optional parameters (parameters with default values).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Params" />
    public ParameterMatcher Optional()
    {
        return AddPredicate(static p => p.IsOptional);
    }

    /// <summary>
    ///     Matches parameters with the specified type name.
    /// </summary>
    /// <param name="typeName">The name of the parameter type to match.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="CancellationToken" />
    public ParameterMatcher OfType(string typeName)
    {
        return AddPredicate(p => p.Type.Name == typeName);
    }

    /// <summary>
    ///     Matches <see cref="System.Threading.CancellationToken" /> parameters.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="OfType" />
    public ParameterMatcher CancellationToken() => OfType("CancellationToken");
}