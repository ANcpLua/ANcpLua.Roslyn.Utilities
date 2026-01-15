using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Patterns;

/// <summary>
///     Provides a fluent builder for constructing property symbol patterns.
/// </summary>
/// <remarks>
///     <para>
///         This builder enables declarative matching of <see cref="IPropertySymbol" /> instances
///         by composing multiple predicates that must all be satisfied for a match.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Supports filtering by name, type, accessibility, and modifiers.</description>
///         </item>
///         <item>
///             <description>Can match properties based on getter/setter presence and characteristics.</description>
///         </item>
///         <item>
///             <description>Supports attribute-based filtering and containing type matching.</description>
///         </item>
///         <item>
///             <description>Implicitly converts to <see cref="SymbolPattern{T}" /> for seamless integration.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="SymbolPattern{T}" />
/// <seealso cref="FieldPatternBuilder" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class PropertyPatternBuilder
{
    private readonly List<Func<IPropertySymbol, bool>> _predicates = [];

    /// <summary>
    ///     Adds a predicate requiring the property to have the specified name.
    /// </summary>
    /// <param name="name">The exact name the property must have.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PropertyPatternBuilder Named(string name)
    {
        _predicates.Add(p => p.Name == name);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property type to match the specified type symbol.
    /// </summary>
    /// <param name="type">The type symbol that the property type must equal.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="SymbolExtensions.IsEqualTo" />
    public PropertyPatternBuilder OfType(ITypeSymbol type)
    {
        _predicates.Add(p => p.Type.IsEqualTo(type));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property type to satisfy the specified condition.
    /// </summary>
    /// <param name="predicate">A function that evaluates the property type and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PropertyPatternBuilder OfType(Func<ITypeSymbol, bool> predicate)
    {
        _predicates.Add(p => predicate(p.Type));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to have public accessibility.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Private" />
    public PropertyPatternBuilder Public()
    {
        _predicates.Add(p => p.DeclaredAccessibility == Accessibility.Public);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to have private accessibility.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Public" />
    public PropertyPatternBuilder Private()
    {
        _predicates.Add(p => p.DeclaredAccessibility == Accessibility.Private);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to be static.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Instance" />
    public PropertyPatternBuilder Static()
    {
        _predicates.Add(p => p.IsStatic);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to be an instance member (not static).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Static" />
    public PropertyPatternBuilder Instance()
    {
        _predicates.Add(p => !p.IsStatic);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to have a getter.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasSetter" />
    /// <seealso cref="IsReadOnly" />
    public PropertyPatternBuilder HasGetter()
    {
        _predicates.Add(p => p.GetMethod is not null);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to have a setter.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasGetter" />
    /// <seealso cref="IsWriteOnly" />
    public PropertyPatternBuilder HasSetter()
    {
        _predicates.Add(p => p.SetMethod is not null);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to be read-only (has getter but no setter).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsWriteOnly" />
    /// <seealso cref="HasGetter" />
    public PropertyPatternBuilder IsReadOnly()
    {
        _predicates.Add(p => p.IsReadOnly);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to be write-only (has setter but no getter).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsReadOnly" />
    /// <seealso cref="HasSetter" />
    public PropertyPatternBuilder IsWriteOnly()
    {
        _predicates.Add(p => p.IsWriteOnly);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to be an auto-implemented property.
    /// </summary>
    /// <remarks>
    ///     An auto-property is detected by checking if either the getter or setter is implicitly declared.
    /// </remarks>
    /// <returns>This builder instance for method chaining.</returns>
    public PropertyPatternBuilder IsAutoProperty()
    {
        _predicates.Add(p =>
            p.GetMethod?.IsImplicitlyDeclared == true ||
            p.SetMethod?.IsImplicitlyDeclared == true);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to be marked with the <c>required</c> modifier.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public PropertyPatternBuilder IsRequired()
    {
        _predicates.Add(p => p.IsRequired);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to be an indexer.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public PropertyPatternBuilder IsIndexer()
    {
        _predicates.Add(p => p.IsIndexer);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to be virtual.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsOverride" />
    public PropertyPatternBuilder IsVirtual()
    {
        _predicates.Add(p => p.IsVirtual);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to be an override.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsVirtual" />
    public PropertyPatternBuilder IsOverride()
    {
        _predicates.Add(p => p.IsOverride);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to have the specified attribute.
    /// </summary>
    /// <param name="attributeType">The type symbol of the attribute that must be present.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasAttribute(string)" />
    public PropertyPatternBuilder HasAttribute(ITypeSymbol attributeType)
    {
        _predicates.Add(p => p.HasAttribute(attributeType));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to have an attribute with the specified fully qualified name.
    /// </summary>
    /// <param name="fullyQualifiedName">The fully qualified name of the attribute that must be present.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasAttribute(ITypeSymbol)" />
    public PropertyPatternBuilder HasAttribute(string fullyQualifiedName)
    {
        _predicates.Add(p => p.HasAttribute(fullyQualifiedName));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the property to be declared in a type matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The pattern that the containing type must match.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PropertyPatternBuilder InType(SymbolPattern<INamedTypeSymbol> pattern)
    {
        _predicates.Add(p => p.ContainingType is not null && pattern.Matches(p.ContainingType));
        return this;
    }

    /// <summary>
    ///     Adds a custom predicate for matching properties.
    /// </summary>
    /// <param name="predicate">A function that evaluates the property and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PropertyPatternBuilder Where(Func<IPropertySymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    /// <summary>
    ///     Builds the composed pattern from all added predicates.
    /// </summary>
    /// <returns>A <see cref="SymbolPattern{T}" /> that matches properties satisfying all predicates.</returns>
    public SymbolPattern<IPropertySymbol> Build()
    {
        var predicates = _predicates.ToArray();
        return new PredicatePattern<IPropertySymbol>(p =>
        {
            foreach (var predicate in predicates)
                if (!predicate(p))
                    return false;
            return true;
        });
    }

    /// <summary>
    ///     Implicitly converts a <see cref="PropertyPatternBuilder" /> to a <see cref="SymbolPattern{T}" />.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>A symbol pattern built from the builder's predicates.</returns>
    public static implicit operator SymbolPattern<IPropertySymbol>(PropertyPatternBuilder builder)
    {
        return builder.Build();
    }
}

/// <summary>
///     Provides a fluent builder for constructing field symbol patterns.
/// </summary>
/// <remarks>
///     <para>
///         This builder enables declarative matching of <see cref="IFieldSymbol" /> instances
///         by composing multiple predicates that must all be satisfied for a match.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Supports filtering by name, type, accessibility, and modifiers.</description>
///         </item>
///         <item>
///             <description>Can match fields based on const, readonly, and volatile characteristics.</description>
///         </item>
///         <item>
///             <description>Supports attribute-based filtering.</description>
///         </item>
///         <item>
///             <description>Implicitly converts to <see cref="SymbolPattern{T}" /> for seamless integration.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="SymbolPattern{T}" />
/// <seealso cref="PropertyPatternBuilder" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class FieldPatternBuilder
{
    private readonly List<Func<IFieldSymbol, bool>> _predicates = [];

    /// <summary>
    ///     Adds a predicate requiring the field to have the specified name.
    /// </summary>
    /// <param name="name">The exact name the field must have.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public FieldPatternBuilder Named(string name)
    {
        _predicates.Add(f => f.Name == name);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the field type to match the specified type symbol.
    /// </summary>
    /// <param name="type">The type symbol that the field type must equal.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="SymbolExtensions.IsEqualTo" />
    public FieldPatternBuilder OfType(ITypeSymbol type)
    {
        _predicates.Add(f => f.Type.IsEqualTo(type));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the field to have public accessibility.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Private" />
    public FieldPatternBuilder Public()
    {
        _predicates.Add(f => f.DeclaredAccessibility == Accessibility.Public);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the field to have private accessibility.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Public" />
    public FieldPatternBuilder Private()
    {
        _predicates.Add(f => f.DeclaredAccessibility == Accessibility.Private);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the field to be static.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Instance" />
    public FieldPatternBuilder Static()
    {
        _predicates.Add(f => f.IsStatic);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the field to be an instance member (not static).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Static" />
    public FieldPatternBuilder Instance()
    {
        _predicates.Add(f => !f.IsStatic);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the field to be a compile-time constant.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsReadOnly" />
    public FieldPatternBuilder IsConst()
    {
        _predicates.Add(f => f.IsConst);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the field to be read-only.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsConst" />
    public FieldPatternBuilder IsReadOnly()
    {
        _predicates.Add(f => f.IsReadOnly);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the field to be volatile.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public FieldPatternBuilder IsVolatile()
    {
        _predicates.Add(f => f.IsVolatile);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the field to have the specified attribute.
    /// </summary>
    /// <param name="attributeType">The type symbol of the attribute that must be present.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasAttribute(string)" />
    public FieldPatternBuilder HasAttribute(ITypeSymbol attributeType)
    {
        _predicates.Add(f => f.HasAttribute(attributeType));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the field to have an attribute with the specified fully qualified name.
    /// </summary>
    /// <param name="fullyQualifiedName">The fully qualified name of the attribute that must be present.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasAttribute(ITypeSymbol)" />
    public FieldPatternBuilder HasAttribute(string fullyQualifiedName)
    {
        _predicates.Add(f => f.HasAttribute(fullyQualifiedName));
        return this;
    }

    /// <summary>
    ///     Adds a custom predicate for matching fields.
    /// </summary>
    /// <param name="predicate">A function that evaluates the field and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public FieldPatternBuilder Where(Func<IFieldSymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    /// <summary>
    ///     Builds the composed pattern from all added predicates.
    /// </summary>
    /// <returns>A <see cref="SymbolPattern{T}" /> that matches fields satisfying all predicates.</returns>
    public SymbolPattern<IFieldSymbol> Build()
    {
        var predicates = _predicates.ToArray();
        return new PredicatePattern<IFieldSymbol>(f =>
        {
            foreach (var predicate in predicates)
                if (!predicate(f))
                    return false;
            return true;
        });
    }

    /// <summary>
    ///     Implicitly converts a <see cref="FieldPatternBuilder" /> to a <see cref="SymbolPattern{T}" />.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>A symbol pattern built from the builder's predicates.</returns>
    public static implicit operator SymbolPattern<IFieldSymbol>(FieldPatternBuilder builder)
    {
        return builder.Build();
    }
}