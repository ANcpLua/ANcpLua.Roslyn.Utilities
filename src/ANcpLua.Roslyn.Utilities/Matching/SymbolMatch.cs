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
    public static MethodMatcher Method()
    {
        return new MethodMatcher();
    }

    /// <summary>
    ///     Creates a new method matcher that matches methods with the specified name.
    /// </summary>
    /// <param name="name">The exact name to match against the method's <see cref="ISymbol.Name" />.</param>
    /// <returns>A new <see cref="MethodMatcher" /> instance configured to match methods with the specified name.</returns>
    /// <seealso cref="Method()" />
    public static MethodMatcher Method(string name)
    {
        return new MethodMatcher().Named(name);
    }

    /// <summary>
    ///     Creates a new type matcher with no initial constraints.
    /// </summary>
    /// <returns>A new <see cref="TypeMatcher" /> instance.</returns>
    /// <seealso cref="Type(string)" />
    public static TypeMatcher Type()
    {
        return new TypeMatcher();
    }

    /// <summary>
    ///     Creates a new type matcher that matches types with the specified name.
    /// </summary>
    /// <param name="name">The exact name to match against the type's <see cref="ISymbol.Name" />.</param>
    /// <returns>A new <see cref="TypeMatcher" /> instance configured to match types with the specified name.</returns>
    /// <seealso cref="Type()" />
    public static TypeMatcher Type(string name)
    {
        return new TypeMatcher().Named(name);
    }

    /// <summary>
    ///     Creates a new property matcher with no initial constraints.
    /// </summary>
    /// <returns>A new <see cref="PropertyMatcher" /> instance.</returns>
    /// <seealso cref="Property(string)" />
    public static PropertyMatcher Property()
    {
        return new PropertyMatcher();
    }

    /// <summary>
    ///     Creates a new property matcher that matches properties with the specified name.
    /// </summary>
    /// <param name="name">The exact name to match against the property's <see cref="ISymbol.Name" />.</param>
    /// <returns>A new <see cref="PropertyMatcher" /> instance configured to match properties with the specified name.</returns>
    /// <seealso cref="Property()" />
    public static PropertyMatcher Property(string name)
    {
        return new PropertyMatcher().Named(name);
    }

    /// <summary>
    ///     Creates a new field matcher with no initial constraints.
    /// </summary>
    /// <returns>A new <see cref="FieldMatcher" /> instance.</returns>
    /// <seealso cref="Field(string)" />
    public static FieldMatcher Field()
    {
        return new FieldMatcher();
    }

    /// <summary>
    ///     Creates a new field matcher that matches fields with the specified name.
    /// </summary>
    /// <param name="name">The exact name to match against the field's <see cref="ISymbol.Name" />.</param>
    /// <returns>A new <see cref="FieldMatcher" /> instance configured to match fields with the specified name.</returns>
    /// <seealso cref="Field()" />
    public static FieldMatcher Field(string name)
    {
        return new FieldMatcher().Named(name);
    }

    /// <summary>
    ///     Creates a new parameter matcher with no initial constraints.
    /// </summary>
    /// <returns>A new <see cref="ParameterMatcher" /> instance.</returns>
    public static ParameterMatcher Parameter()
    {
        return new ParameterMatcher();
    }
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
    public bool Matches(ISymbol? symbol)
    {
        return symbol is TSymbol typed && MatchesAll(typed);
    }

    /// <summary>
    ///     Tests if the specified strongly-typed symbol matches all configured predicates.
    /// </summary>
    /// <param name="symbol">The symbol to test, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is not <c>null</c>
    ///     and matches all predicates; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Matches(ISymbol?)" />
    public bool Matches(TSymbol? symbol)
    {
        return symbol is not null && MatchesAll(symbol);
    }

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
    public TSelf Where(Func<TSymbol, bool> predicate)
    {
        return AddPredicate(predicate);
    }
}
