using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Patterns;

/// <summary>
///     Provides a fluent builder for constructing <see cref="SymbolPattern{T}" /> instances
///     that match <see cref="IMethodSymbol" /> based on various criteria.
/// </summary>
/// <remarks>
///     <para>
///         The builder accumulates predicates that are combined with logical AND semantics.
///         All specified conditions must be satisfied for a method to match the resulting pattern.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Name matching: <see cref="Named" />, <see cref="NameStartsWith" />,
///                 <see cref="NameEndsWith" />, <see cref="NameMatches" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Modifiers: <see cref="Static" />, <see cref="Instance" />, <see cref="Async" />,
///                 <see cref="Virtual" />, <see cref="Abstract" />, <see cref="Sealed" />, <see cref="Override" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Accessibility: <see cref="Public" />, <see cref="Private" />, <see cref="Protected" />,
///                 <see cref="Internal" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Return types: <see cref="ReturnsVoid" />, <see cref="Returns(ITypeSymbol)" />,
///                 <see cref="ReturnsString" />, <see cref="ReturnsBool" />, <see cref="ReturnsInt" />,
///                 <see cref="ReturnsTask" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Parameters: <see cref="NoParameters" />, <see cref="ParameterCount" />,
///                 <see cref="WithParameter(Func{IParameterSymbol, bool})" />, <see cref="WithCancellationToken" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Type parameters: <see cref="NoTypeParameters" />, <see cref="TypeParameterCount" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Method kind: <see cref="IsExtensionMethod" />, <see cref="IsConstructor" />,
///                 <see cref="IsOrdinaryMethod" />, <see cref="IsOperator" />, <see cref="IsPropertyAccessor" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Context: <see cref="InType(Func{INamedTypeSymbol, bool})" />, <see cref="InNamespace" />,
///                 <see cref="HasAttribute(string)" />, <see cref="ImplementsInterface" />
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// var asyncPublicMethod = new MethodPatternBuilder()
///     .Public()
///     .Async()
///     .ReturnsTask()
///     .WithCancellationToken()
///     .Build();
/// 
/// if (asyncPublicMethod.Matches(methodSymbol))
/// {
///     // Method is public, async, returns Task, and has a CancellationToken parameter
/// }
/// </code>
/// </example>
/// <seealso cref="SymbolPattern{T}" />
/// <seealso cref="PredicatePattern{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class MethodPatternBuilder
{
    private readonly List<Func<IMethodSymbol, bool>> _predicates = [];

    /// <summary>
    ///     Adds a predicate requiring the method name to exactly match the specified <paramref name="name" />.
    /// </summary>
    /// <param name="name">The exact method name to match.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="NameStartsWith" />
    /// <seealso cref="NameEndsWith" />
    /// <seealso cref="NameMatches" />
    public MethodPatternBuilder Named(string name)
    {
        _predicates.Add(m => m.Name == name);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method name to start with the specified <paramref name="prefix" />.
    /// </summary>
    /// <param name="prefix">The prefix the method name must start with.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    ///     Uses ordinal string comparison for matching.
    /// </remarks>
    /// <seealso cref="Named" />
    /// <seealso cref="NameEndsWith" />
    public MethodPatternBuilder NameStartsWith(string prefix)
    {
        _predicates.Add(m => m.Name.StartsWith(prefix, StringComparison.Ordinal));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method name to end with the specified <paramref name="suffix" />.
    /// </summary>
    /// <param name="suffix">The suffix the method name must end with.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    ///     Uses ordinal string comparison for matching.
    /// </remarks>
    /// <seealso cref="Named" />
    /// <seealso cref="NameStartsWith" />
    public MethodPatternBuilder NameEndsWith(string suffix)
    {
        _predicates.Add(m => m.Name.EndsWith(suffix, StringComparison.Ordinal));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method name to satisfy the specified custom <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A function that evaluates the method name and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Named" />
    public MethodPatternBuilder NameMatches(Func<string, bool> predicate)
    {
        _predicates.Add(m => predicate(m.Name));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be static.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Instance" />
    public MethodPatternBuilder Static()
    {
        _predicates.Add(m => m.IsStatic);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be an instance method (not static).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Static" />
    public MethodPatternBuilder Instance()
    {
        _predicates.Add(m => !m.IsStatic);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be declared with the <c>async</c> modifier.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="NotAsync" />
    /// <seealso cref="ReturnsTask" />
    public MethodPatternBuilder Async()
    {
        _predicates.Add(m => m.IsAsync);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to not be declared with the <c>async</c> modifier.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Async" />
    public MethodPatternBuilder NotAsync()
    {
        _predicates.Add(m => !m.IsAsync);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be virtual.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Override" />
    /// <seealso cref="Abstract" />
    /// <seealso cref="Sealed" />
    public MethodPatternBuilder Virtual()
    {
        _predicates.Add(m => m.IsVirtual);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be an override of a base method.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Virtual" />
    /// <seealso cref="Abstract" />
    public MethodPatternBuilder Override()
    {
        _predicates.Add(m => m.IsOverride);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be abstract.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Virtual" />
    /// <seealso cref="Override" />
    public MethodPatternBuilder Abstract()
    {
        _predicates.Add(m => m.IsAbstract);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be sealed.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Virtual" />
    /// <seealso cref="Override" />
    public MethodPatternBuilder Sealed()
    {
        _predicates.Add(m => m.IsSealed);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have public accessibility.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Private" />
    /// <seealso cref="Protected" />
    /// <seealso cref="Internal" />
    public MethodPatternBuilder Public()
    {
        _predicates.Add(m => m.DeclaredAccessibility == Accessibility.Public);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have private accessibility.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Public" />
    /// <seealso cref="Protected" />
    /// <seealso cref="Internal" />
    public MethodPatternBuilder Private()
    {
        _predicates.Add(m => m.DeclaredAccessibility == Accessibility.Private);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have protected accessibility.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Public" />
    /// <seealso cref="Private" />
    /// <seealso cref="Internal" />
    public MethodPatternBuilder Protected()
    {
        _predicates.Add(m => m.DeclaredAccessibility == Accessibility.Protected);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have internal accessibility.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Public" />
    /// <seealso cref="Private" />
    /// <seealso cref="Protected" />
    public MethodPatternBuilder Internal()
    {
        _predicates.Add(m => m.DeclaredAccessibility == Accessibility.Internal);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to return <c>void</c>.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Returns(ITypeSymbol)" />
    /// <seealso cref="Returns(Func{ITypeSymbol, bool})" />
    public MethodPatternBuilder ReturnsVoid()
    {
        _predicates.Add(m => m.ReturnsVoid);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method return type to equal the specified <paramref name="type" />.
    /// </summary>
    /// <param name="type">The expected return type.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="ReturnsVoid" />
    /// <seealso cref="Returns(Func{ITypeSymbol, bool})" />
    public MethodPatternBuilder Returns(ITypeSymbol type)
    {
        _predicates.Add(m => m.ReturnType.IsEqualTo(type));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method return type to satisfy the specified custom <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A function that evaluates the return type and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Returns(ITypeSymbol)" />
    public MethodPatternBuilder Returns(Func<ITypeSymbol, bool> predicate)
    {
        _predicates.Add(m => predicate(m.ReturnType));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to return <see cref="string" />.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="ReturnsBool" />
    /// <seealso cref="ReturnsInt" />
    public MethodPatternBuilder ReturnsString()
    {
        _predicates.Add(m => m.ReturnType.SpecialType == SpecialType.System_String);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to return <see cref="bool" />.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="ReturnsString" />
    /// <seealso cref="ReturnsInt" />
    public MethodPatternBuilder ReturnsBool()
    {
        _predicates.Add(m => m.ReturnType.SpecialType == SpecialType.System_Boolean);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to return <see cref="int" />.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="ReturnsString" />
    /// <seealso cref="ReturnsBool" />
    public MethodPatternBuilder ReturnsInt()
    {
        _predicates.Add(m => m.ReturnType.SpecialType == SpecialType.System_Int32);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to return a task-like type.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    ///     Matches <see cref="System.Threading.Tasks.Task" />, <see cref="System.Threading.Tasks.Task{TResult}" />,
    ///     <see cref="System.Threading.Tasks.ValueTask" />, <see cref="System.Threading.Tasks.ValueTask{TResult}" />,
    ///     and other task-like types.
    /// </remarks>
    /// <seealso cref="Async" />
    public MethodPatternBuilder ReturnsTask()
    {
        _predicates.Add(m => m.ReturnType.IsTaskType());
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have no parameters.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="ParameterCount" />
    /// <seealso cref="ParameterCountAtLeast" />
    public MethodPatternBuilder NoParameters()
    {
        _predicates.Add(m => m.Parameters.IsEmpty);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have exactly the specified number of parameters.
    /// </summary>
    /// <param name="count">The exact number of parameters required.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="NoParameters" />
    /// <seealso cref="ParameterCountAtLeast" />
    public MethodPatternBuilder ParameterCount(int count)
    {
        _predicates.Add(m => m.Parameters.Length == count);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have at least the specified number of parameters.
    /// </summary>
    /// <param name="count">The minimum number of parameters required.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="NoParameters" />
    /// <seealso cref="ParameterCount" />
    public MethodPatternBuilder ParameterCountAtLeast(int count)
    {
        _predicates.Add(m => m.Parameters.Length >= count);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have at least one parameter matching the specified
    ///     <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A function that evaluates a parameter and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="WithParameter(int, Func{IParameterSymbol, bool})" />
    /// <seealso cref="WithParameterOfType" />
    public MethodPatternBuilder WithParameter(Func<IParameterSymbol, bool> predicate)
    {
        _predicates.Add(m => m.Parameters.Any(predicate));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the parameter at the specified <paramref name="index" /> to match the specified
    ///     <paramref name="predicate" />.
    /// </summary>
    /// <param name="index">The zero-based index of the parameter to check.</param>
    /// <param name="predicate">A function that evaluates the parameter and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    ///     If the method has fewer parameters than <paramref name="index" /> + 1, the predicate will not match.
    /// </remarks>
    /// <seealso cref="WithParameter(Func{IParameterSymbol, bool})" />
    public MethodPatternBuilder WithParameter(int index, Func<IParameterSymbol, bool> predicate)
    {
        _predicates.Add(m => m.Parameters.Length > index && predicate(m.Parameters[index]));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have at least one parameter of the specified <paramref name="type" />.
    /// </summary>
    /// <param name="type">The type that at least one parameter must have.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="WithParameter(Func{IParameterSymbol, bool})" />
    /// <seealso cref="WithCancellationToken" />
    public MethodPatternBuilder WithParameterOfType(ITypeSymbol type)
    {
        _predicates.Add(m => m.Parameters.Any(p => p.Type.IsEqualTo(type)));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have at least one <see cref="System.Threading.CancellationToken" />
    ///     parameter.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="Async" />
    /// <seealso cref="ReturnsTask" />
    /// <seealso cref="WithParameterOfType" />
    public MethodPatternBuilder WithCancellationToken()
    {
        _predicates.Add(m => m.Parameters.Any(p =>
            p.Type.ToDisplayString() == "System.Threading.CancellationToken"));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have no type parameters (non-generic method).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="TypeParameterCount" />
    public MethodPatternBuilder NoTypeParameters()
    {
        _predicates.Add(m => m.TypeParameters.IsEmpty);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have exactly the specified number of type parameters.
    /// </summary>
    /// <param name="count">The exact number of type parameters required.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="NoTypeParameters" />
    public MethodPatternBuilder TypeParameterCount(int count)
    {
        _predicates.Add(m => m.TypeParameters.Length == count);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be an extension method.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsNotExtensionMethod" />
    /// <seealso cref="Static" />
    public MethodPatternBuilder IsExtensionMethod()
    {
        _predicates.Add(m => m.IsExtensionMethod);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to not be an extension method.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsExtensionMethod" />
    public MethodPatternBuilder IsNotExtensionMethod()
    {
        _predicates.Add(m => !m.IsExtensionMethod);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be a constructor.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsOrdinaryMethod" />
    /// <seealso cref="IsOperator" />
    /// <seealso cref="IsPropertyAccessor" />
    public MethodPatternBuilder IsConstructor()
    {
        _predicates.Add(m => m.MethodKind == MethodKind.Constructor);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be an ordinary method (not constructor, operator, or accessor).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsConstructor" />
    /// <seealso cref="IsOperator" />
    /// <seealso cref="IsPropertyAccessor" />
    public MethodPatternBuilder IsOrdinaryMethod()
    {
        _predicates.Add(m => m.MethodKind == MethodKind.Ordinary);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be a user-defined operator or conversion.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsConstructor" />
    /// <seealso cref="IsOrdinaryMethod" />
    /// <seealso cref="IsPropertyAccessor" />
    public MethodPatternBuilder IsOperator()
    {
        _predicates.Add(m => m.MethodKind is MethodKind.UserDefinedOperator or MethodKind.Conversion);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be a property getter or setter.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="IsConstructor" />
    /// <seealso cref="IsOrdinaryMethod" />
    /// <seealso cref="IsOperator" />
    public MethodPatternBuilder IsPropertyAccessor()
    {
        _predicates.Add(m => m.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have an attribute of the specified type.
    /// </summary>
    /// <param name="attributeType">The attribute type symbol to check for.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasAttribute(string)" />
    public MethodPatternBuilder HasAttribute(ITypeSymbol attributeType)
    {
        _predicates.Add(m => m.HasAttribute(attributeType));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to have an attribute with the specified fully qualified name.
    /// </summary>
    /// <param name="fullyQualifiedName">The fully qualified name of the attribute type (e.g., "System.ObsoleteAttribute").</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="HasAttribute(ITypeSymbol)" />
    public MethodPatternBuilder HasAttribute(string fullyQualifiedName)
    {
        _predicates.Add(m => m.HasAttribute(fullyQualifiedName));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method's containing type to satisfy the specified <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A function that evaluates the containing type and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="InType(SymbolPattern{INamedTypeSymbol})" />
    /// <seealso cref="InNamespace" />
    public MethodPatternBuilder InType(Func<INamedTypeSymbol, bool> predicate)
    {
        _predicates.Add(m => m.ContainingType is not null && predicate(m.ContainingType));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method's containing type to match the specified type <paramref name="pattern" />.
    /// </summary>
    /// <param name="pattern">The pattern that the containing type must match.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="InType(Func{INamedTypeSymbol, bool})" />
    /// <seealso cref="InNamespace" />
    public MethodPatternBuilder InType(SymbolPattern<INamedTypeSymbol> pattern)
    {
        _predicates.Add(m => m.ContainingType is not null && pattern.Matches(m.ContainingType));
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to be declared within the specified namespace.
    /// </summary>
    /// <param name="ns">The fully qualified namespace name (e.g., "System.Collections.Generic").</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <seealso cref="InType(Func{INamedTypeSymbol, bool})" />
    public MethodPatternBuilder InNamespace(string ns)
    {
        _predicates.Add(m => m.ContainingNamespace?.ToDisplayString() == ns);
        return this;
    }

    /// <summary>
    ///     Adds a predicate requiring the method to implement an interface member.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    ///     Matches methods that either explicitly implement an interface member or are the implementation
    ///     target of an interface member on the containing type.
    /// </remarks>
    public MethodPatternBuilder ImplementsInterface()
    {
        _predicates.Add(m => m.ExplicitInterfaceImplementations.Length > 0 ||
                             m.ContainingType?.FindImplementationForInterfaceMember(m) is not null);
        return this;
    }

    /// <summary>
    ///     Adds a custom predicate to the pattern.
    /// </summary>
    /// <param name="predicate">A function that evaluates the method and returns <c>true</c> if it matches.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    ///     Use this method when the built-in builder methods do not cover your specific matching criteria.
    /// </remarks>
    public MethodPatternBuilder Where(Func<IMethodSymbol, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    /// <summary>
    ///     Builds a <see cref="SymbolPattern{T}" /> from the accumulated predicates.
    /// </summary>
    /// <returns>
    ///     A <see cref="SymbolPattern{T}" /> that matches methods satisfying all specified predicates.
    /// </returns>
    /// <remarks>
    ///     All predicates are combined with logical AND semantics. A method must satisfy every
    ///     predicate to be considered a match.
    /// </remarks>
    public SymbolPattern<IMethodSymbol> Build()
    {
        var predicates = _predicates.ToArray();
        return new PredicatePattern<IMethodSymbol>(m =>
        {
            foreach (var predicate in predicates)
                if (!predicate(m))
                    return false;
            return true;
        });
    }

    /// <summary>
    ///     Implicitly converts a <see cref="MethodPatternBuilder" /> to a <see cref="SymbolPattern{T}" />.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>
    ///     A <see cref="SymbolPattern{T}" /> equivalent to calling <see cref="Build" /> on the <paramref name="builder" />.
    /// </returns>
    public static implicit operator SymbolPattern<IMethodSymbol>(MethodPatternBuilder builder)
    {
        return builder.Build();
    }
}