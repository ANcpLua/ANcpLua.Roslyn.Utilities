using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Patterns;

/// <summary>
/// Provides a fluent builder for constructing <see cref="SymbolPattern{T}"/> instances
/// that match <see cref="INamedTypeSymbol"/> based on composable criteria.
/// <para>
/// This builder enables declarative, readable pattern definitions for type matching
/// in Roslyn analyzers and source generators.
/// </para>
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>All filter methods return <c>this</c> to enable method chaining.</description></item>
/// <item><description>Multiple criteria are combined with logical AND semantics.</description></item>
/// <item><description>Use <see cref="Build"/> to create the final pattern, or rely on implicit conversion.</description></item>
/// <item><description>Patterns are immutable once built and safe for caching.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var pattern = new TypePatternBuilder()
///     .IsClass()
///     .Public()
///     .Implements("System.IDisposable")
///     .Build();
///
/// if (pattern.Matches(typeSymbol))
/// {
///     // Type is a public class implementing IDisposable
/// }
/// </code>
/// </example>
/// <seealso cref="SymbolPattern{T}"/>
/// <seealso cref="Matching.Match"/>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
sealed class TypePatternBuilder
{
    private readonly List<Func<INamedTypeSymbol, bool>> _predicates = [];

    /// <summary>
    /// Adds a predicate that matches types with the specified simple name.
    /// </summary>
    /// <param name="name">The exact simple name to match (e.g., "MyClass").</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="FullName"/>
    /// <seealso cref="NameStartsWith"/>
    /// <seealso cref="NameEndsWith"/>
    public TypePatternBuilder Named(string name)
    {
        _predicates.Add(t => t.Name == name);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types with the specified fully qualified name.
    /// </summary>
    /// <param name="fullName">The fully qualified name including namespace (e.g., "System.Collections.Generic.List").</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Named"/>
    /// <seealso cref="InNamespace"/>
    public TypePatternBuilder FullName(string fullName)
    {
        _predicates.Add(t => t.ToDisplayString() == fullName);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types whose simple name starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match at the beginning of the type name.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Named"/>
    /// <seealso cref="NameEndsWith"/>
    public TypePatternBuilder NameStartsWith(string prefix)
    {
        _predicates.Add(t => t.Name.StartsWith(prefix, StringComparison.Ordinal));
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types whose simple name ends with the specified suffix.
    /// </summary>
    /// <param name="suffix">The suffix to match at the end of the type name.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Named"/>
    /// <seealso cref="NameStartsWith"/>
    public TypePatternBuilder NameEndsWith(string suffix)
    {
        _predicates.Add(t => t.Name.EndsWith(suffix, StringComparison.Ordinal));
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types with <see cref="Accessibility.Public"/> accessibility.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Internal"/>
    public TypePatternBuilder Public()
    {
        _predicates.Add(t => t.DeclaredAccessibility == Accessibility.Public);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types with <see cref="Accessibility.Internal"/> accessibility.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Public"/>
    public TypePatternBuilder Internal()
    {
        _predicates.Add(t => t.DeclaredAccessibility == Accessibility.Internal);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches static types.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="NotStatic"/>
    public TypePatternBuilder Static()
    {
        _predicates.Add(t => t.IsStatic);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches non-static types.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Static"/>
    public TypePatternBuilder NotStatic()
    {
        _predicates.Add(t => !t.IsStatic);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches abstract types.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Sealed"/>
    public TypePatternBuilder Abstract()
    {
        _predicates.Add(t => t.IsAbstract);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches sealed types.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Abstract"/>
    public TypePatternBuilder Sealed()
    {
        _predicates.Add(t => t.IsSealed);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types with multiple declaring syntax references (partial types).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a heuristic approach - types with a single partial declaration won't be detected.
    /// For full partial detection including single-declaration partials, use syntax-level checks.
    /// </para>
    /// </remarks>
    /// <returns>This builder instance for method chaining.</returns>
    public TypePatternBuilder HasMultipleDeclarations()
    {
        _predicates.Add(t => t.DeclaringSyntaxReferences.Length > 1);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types of kind <see cref="TypeKind.Class"/>.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsStruct"/>
    /// <seealso cref="IsInterface"/>
    /// <seealso cref="IsRecord"/>
    public TypePatternBuilder IsClass()
    {
        _predicates.Add(t => t.TypeKind == TypeKind.Class);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types of kind <see cref="TypeKind.Struct"/>.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsClass"/>
    /// <seealso cref="IsRecord"/>
    public TypePatternBuilder IsStruct()
    {
        _predicates.Add(t => t.TypeKind == TypeKind.Struct);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types of kind <see cref="TypeKind.Interface"/>.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsClass"/>
    /// <seealso cref="IsDelegate"/>
    public TypePatternBuilder IsInterface()
    {
        _predicates.Add(t => t.TypeKind == TypeKind.Interface);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types of kind <see cref="TypeKind.Enum"/>.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsClass"/>
    /// <seealso cref="IsStruct"/>
    public TypePatternBuilder IsEnum()
    {
        _predicates.Add(t => t.TypeKind == TypeKind.Enum);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types of kind <see cref="TypeKind.Delegate"/>.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsClass"/>
    /// <seealso cref="IsInterface"/>
    public TypePatternBuilder IsDelegate()
    {
        _predicates.Add(t => t.TypeKind == TypeKind.Delegate);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches record types (record class or record struct).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsClass"/>
    /// <seealso cref="IsStruct"/>
    public TypePatternBuilder IsRecord()
    {
        _predicates.Add(t => t.IsRecord);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches generic types (types with type parameters).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsNotGeneric"/>
    /// <seealso cref="TypeParameterCount"/>
    public TypePatternBuilder IsGeneric()
    {
        _predicates.Add(t => t.IsGenericType);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches non-generic types.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsGeneric"/>
    public TypePatternBuilder IsNotGeneric()
    {
        _predicates.Add(t => !t.IsGenericType);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types with the specified number of type parameters.
    /// </summary>
    /// <param name="count">The exact number of type parameters required.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsGeneric"/>
    /// <seealso cref="IsNotGeneric"/>
    public TypePatternBuilder TypeParameterCount(int count)
    {
        _predicates.Add(t => t.TypeParameters.Length == count);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types implementing the specified interface.
    /// </summary>
    /// <param name="interfaceType">The interface type symbol to check for implementation.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Implements(Func{INamedTypeSymbol, bool})"/>
    /// <seealso cref="InheritsFrom(ITypeSymbol)"/>
    public TypePatternBuilder Implements(ITypeSymbol interfaceType)
    {
        _predicates.Add(t => t.Implements(interfaceType));
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types implementing any interface that satisfies the given predicate.
    /// </summary>
    /// <param name="predicate">A function to test each implemented interface.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Implements(ITypeSymbol)"/>
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

    /// <summary>
    /// Adds a predicate that matches types inheriting from the specified base type.
    /// </summary>
    /// <param name="baseType">The base type symbol to check in the inheritance hierarchy.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="InheritsFrom(Func{INamedTypeSymbol?, bool})"/>
    /// <seealso cref="Implements(ITypeSymbol)"/>
    public TypePatternBuilder InheritsFrom(ITypeSymbol baseType)
    {
        _predicates.Add(t => t.InheritsFrom(baseType));
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types with any base type in the inheritance hierarchy satisfying the given predicate.
    /// </summary>
    /// <param name="predicate">A function to test each base type in the inheritance chain.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="InheritsFrom(ITypeSymbol)"/>
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

    /// <summary>
    /// Adds a predicate that matches types decorated with the specified attribute.
    /// </summary>
    /// <param name="attributeType">The attribute type symbol to check for.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasAttribute(string)"/>
    public TypePatternBuilder HasAttribute(ITypeSymbol attributeType)
    {
        _predicates.Add(t => t.HasAttribute(attributeType));
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types decorated with an attribute of the specified fully qualified name.
    /// </summary>
    /// <param name="fullyQualifiedName">The fully qualified name of the attribute (e.g., "System.ObsoleteAttribute").</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasAttribute(ITypeSymbol)"/>
    public TypePatternBuilder HasAttribute(string fullyQualifiedName)
    {
        _predicates.Add(t => t.HasAttribute(fullyQualifiedName));
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types declared in the specified namespace.
    /// </summary>
    /// <param name="ns">The fully qualified namespace name (e.g., "System.Collections.Generic").</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="InNamespaceStartsWith"/>
    /// <seealso cref="FullName"/>
    public TypePatternBuilder InNamespace(string ns)
    {
        _predicates.Add(t => t.ContainingNamespace?.ToDisplayString() == ns);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types declared in namespaces starting with the specified prefix.
    /// </summary>
    /// <param name="prefix">The namespace prefix to match (e.g., "System.Collections").</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="InNamespace"/>
    public TypePatternBuilder InNamespaceStartsWith(string prefix)
    {
        _predicates.Add(t => t.ContainingNamespace?.ToDisplayString().StartsWith(prefix, StringComparison.Ordinal) == true);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types containing a member with the specified name.
    /// </summary>
    /// <param name="name">The name of the member to search for.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasMethod"/>
    /// <seealso cref="HasProperty"/>
    public TypePatternBuilder HasMember(string name)
    {
        _predicates.Add(t => t.GetMembers(name).Length > 0);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types containing a method that matches the specified pattern.
    /// </summary>
    /// <param name="pattern">The method pattern to match against type members.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasProperty"/>
    /// <seealso cref="HasMember"/>
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

    /// <summary>
    /// Adds a predicate that matches types containing a property that matches the specified pattern.
    /// </summary>
    /// <param name="pattern">The property pattern to match against type members.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasMethod"/>
    /// <seealso cref="HasMember"/>
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

    /// <summary>
    /// Adds a predicate that matches nested types (types declared inside another type).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsTopLevel"/>
    public TypePatternBuilder IsNested()
    {
        _predicates.Add(t => t.ContainingType is not null);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches top-level types (types not declared inside another type).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsNested"/>
    public TypePatternBuilder IsTopLevel()
    {
        _predicates.Add(t => t.ContainingType is null);
        return this;
    }

    /// <summary>
    /// Adds a predicate that matches types implementing <see cref="System.IDisposable"/> or <c>IAsyncDisposable</c>.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Implements(ITypeSymbol)"/>
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

    /// <summary>
    /// Adds a custom predicate for matching types.
    /// </summary>
    /// <param name="predicate">A function that returns <c>true</c> if the type should match.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// Use this method for custom matching logic not covered by the built-in filter methods.
    /// </remarks>
    public TypePatternBuilder Where(Func<INamedTypeSymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    /// <summary>
    /// Builds a <see cref="SymbolPattern{T}"/> from the accumulated predicates.
    /// </summary>
    /// <returns>
    /// A <see cref="SymbolPattern{T}"/> that matches types satisfying all configured predicates.
    /// </returns>
    /// <remarks>
    /// The returned pattern evaluates all predicates with short-circuit AND semantics.
    /// If no predicates are configured, the pattern matches all types.
    /// </remarks>
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

    /// <summary>
    /// Implicitly converts a <see cref="TypePatternBuilder"/> to a <see cref="SymbolPattern{T}"/>
    /// by calling <see cref="Build"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The built pattern.</returns>
    public static implicit operator SymbolPattern<INamedTypeSymbol>(TypePatternBuilder builder) => builder.Build();
}
