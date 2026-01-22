using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Patterns;

/// <summary>
///     Provides a fluent builder for constructing <see cref="SymbolPattern{T}" /> instances
///     that match <see cref="IParameterSymbol" /> based on configurable predicates.
/// </summary>
/// <remarks>
///     <para>
///         This builder enables composable, declarative parameter matching by accumulating
///         predicates that are evaluated together when the pattern is built.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>All predicates are combined with logical AND semantics.</description>
///         </item>
///         <item>
///             <description>The builder supports implicit conversion to <see cref="SymbolPattern{T}" />.</description>
///         </item>
///         <item>
///             <description>Predicates are evaluated in the order they were added.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="SymbolPattern{T}" />
/// <seealso cref="PredicatePattern{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class ParameterPatternBuilder
{
    private readonly List<Func<IParameterSymbol, bool>> _predicates = [];

    /// <summary>
    ///     Adds a predicate requiring the parameter to have the specified name.
    /// </summary>
    /// <param name="name">The exact name the parameter must have.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ParameterPatternBuilder Named(string name)
    {
        _predicates.Add(p => p.Name == name);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter's type to be equal to the specified type symbol.
    /// </summary>
    /// <param name="type">The <see cref="ITypeSymbol" /> the parameter's type must match.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="OfType(Func{ITypeSymbol, bool})" />
    /// <seealso cref="OfTypeNamed(string)" />
    public ParameterPatternBuilder OfType(ITypeSymbol type)
    {
        _predicates.Add(p => p.Type.IsEqualTo(type));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter's type to satisfy the specified condition.
    /// </summary>
    /// <param name="predicate">A function that evaluates the parameter's type and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="OfType(ITypeSymbol)" />
    /// <seealso cref="OfTypeNamed(string)" />
    public ParameterPatternBuilder OfType(Func<ITypeSymbol, bool> predicate)
    {
        _predicates.Add(p => predicate(p.Type));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter's type to have the specified fully qualified name.
    /// </summary>
    /// <param name="fullTypeName">The fully qualified type name (e.g., <c>"System.String"</c>).</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="OfType(ITypeSymbol)" />
    /// <seealso cref="OfType(Func{ITypeSymbol, bool})" />
    public ParameterPatternBuilder OfTypeNamed(string fullTypeName)
    {
        _predicates.Add(p => p.Type.ToDisplayString() == fullTypeName);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter's type to be <see cref="string" />.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsInt" />
    /// <seealso cref="IsBool" />
    public ParameterPatternBuilder IsString()
    {
        _predicates.Add(p => p.Type.SpecialType == SpecialType.System_String);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter's type to be <see cref="int" />.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsString" />
    /// <seealso cref="IsBool" />
    public ParameterPatternBuilder IsInt()
    {
        _predicates.Add(p => p.Type.SpecialType == SpecialType.System_Int32);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter's type to be <see cref="bool" />.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsString" />
    /// <seealso cref="IsInt" />
    public ParameterPatternBuilder IsBool()
    {
        _predicates.Add(p => p.Type.SpecialType == SpecialType.System_Boolean);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter's type to be <see cref="System.Threading.CancellationToken" />.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public ParameterPatternBuilder IsCancellationToken()
    {
        _predicates.Add(p => p.Type.ToDisplayString() == "System.Threading.CancellationToken");
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to have a nullable annotation.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsNotNullable" />
    public ParameterPatternBuilder IsNullable()
    {
        _predicates.Add(p => p.NullableAnnotation == NullableAnnotation.Annotated);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to not have a nullable annotation.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsNullable" />
    public ParameterPatternBuilder IsNotNullable()
    {
        _predicates.Add(p => p.NullableAnnotation != NullableAnnotation.Annotated);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to be optional (have a default value).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsRequired" />
    /// <seealso cref="HasDefaultValue()" />
    public ParameterPatternBuilder IsOptional()
    {
        _predicates.Add(p => p.IsOptional);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to be required (not optional).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsOptional" />
    public ParameterPatternBuilder IsRequired()
    {
        _predicates.Add(p => !p.IsOptional);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to be a params array parameter.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public ParameterPatternBuilder IsParams()
    {
        _predicates.Add(p => p.IsParams);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to be passed by <c>ref</c>.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsOut" />
    /// <seealso cref="IsIn" />
    /// <seealso cref="IsByRef" />
    public ParameterPatternBuilder IsRef()
    {
        _predicates.Add(p => p.RefKind == RefKind.Ref);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to be passed by <c>out</c>.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsRef" />
    /// <seealso cref="IsIn" />
    /// <seealso cref="IsByRef" />
    public ParameterPatternBuilder IsOut()
    {
        _predicates.Add(p => p.RefKind == RefKind.Out);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to be passed by <c>in</c> (readonly ref).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsRef" />
    /// <seealso cref="IsOut" />
    /// <seealso cref="IsByRef" />
    public ParameterPatternBuilder IsIn()
    {
        _predicates.Add(p => p.RefKind == RefKind.In);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to be passed by reference
    ///     (either <c>ref</c>, <c>out</c>, or <c>in</c>).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsByValue" />
    /// <seealso cref="IsRef" />
    /// <seealso cref="IsOut" />
    /// <seealso cref="IsIn" />
    public ParameterPatternBuilder IsByRef()
    {
        _predicates.Add(p => p.RefKind != RefKind.None);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to be passed by value (not by reference).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsByRef" />
    public ParameterPatternBuilder IsByValue()
    {
        _predicates.Add(p => p.RefKind == RefKind.None);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to be at the specified ordinal position.
    /// </summary>
    /// <param name="ordinal">The zero-based position of the parameter in the method signature.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ParameterPatternBuilder AtOrdinal(int ordinal)
    {
        _predicates.Add(p => p.Ordinal == ordinal);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to have an attribute of the specified type.
    /// </summary>
    /// <param name="attributeType">The <see cref="ITypeSymbol" /> of the attribute to check for.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasAttribute(string)" />
    public ParameterPatternBuilder HasAttribute(ITypeSymbol attributeType)
    {
        _predicates.Add(p => p.HasAttribute(attributeType));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to have an attribute with the specified fully qualified name.
    /// </summary>
    /// <param name="fullyQualifiedName">
    ///     The fully qualified name of the attribute (e.g.,
    ///     <c>"System.ComponentModel.DescriptionAttribute"</c>).
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasAttribute(ITypeSymbol)" />
    public ParameterPatternBuilder HasAttribute(string fullyQualifiedName)
    {
        _predicates.Add(p => p.HasAttribute(fullyQualifiedName));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to have an explicit default value.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasDefaultValue{T}(T)" />
    /// <seealso cref="IsOptional" />
    public ParameterPatternBuilder HasDefaultValue()
    {
        _predicates.Add(p => p.HasExplicitDefaultValue);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter to have an explicit default value equal to the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the default value to compare.</typeparam>
    /// <param name="value">The expected default value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasDefaultValue()" />
    public ParameterPatternBuilder HasDefaultValue<T>(T value)
    {
        _predicates.Add(p => p.HasExplicitDefaultValue && Equals(p.ExplicitDefaultValue, value));
        return this;
    }

    /// <summary>
    ///     Adds a custom predicate to the builder.
    /// </summary>
    /// <param name="predicate">A function that evaluates the parameter and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ParameterPatternBuilder Where(Func<IParameterSymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    /// <summary>
    ///     Builds the accumulated predicates into a <see cref="SymbolPattern{T}" /> for <see cref="IParameterSymbol" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="SymbolPattern{T}" /> that matches parameters satisfying all configured predicates.
    /// </returns>
    /// <remarks>
    ///     All predicates are combined with logical AND semantics. A parameter must satisfy
    ///     every predicate added to this builder to match the resulting pattern.
    /// </remarks>
    public SymbolPattern<IParameterSymbol> Build()
    {
        var predicates = _predicates.ToArray();
        return new PredicatePattern<IParameterSymbol>(p =>
        {
            foreach (var predicate in predicates)
                if (!predicate(p))
                    return false;
            return true;
        });
    }

    /// <summary>
    ///     Implicitly converts a <see cref="ParameterPatternBuilder" /> to a <see cref="SymbolPattern{T}" />
    ///     by calling <see cref="Build" />.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The built <see cref="SymbolPattern{T}" /> for <see cref="IParameterSymbol" />.</returns>
    public static implicit operator SymbolPattern<IParameterSymbol>(ParameterPatternBuilder builder) => builder.Build();
}
