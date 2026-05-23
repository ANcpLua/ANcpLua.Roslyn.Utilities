using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Matching;

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
        return AddPredicate(static m => HasCancellationTokenParameter(m.Parameters));
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
        return AddPredicate(static m => m.ReturnType.IsTaskType());
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

    private static bool HasCancellationTokenParameter(ImmutableArray<IParameterSymbol> parameters)
    {
        foreach (var param in parameters)
            if (param.Type is INamedTypeSymbol namedType &&
                namedType.Name is "CancellationToken" &&
                namedType.ContainingNamespace.GetMetadataName() == "System.Threading")
                return true;

        return false;
    }
}
