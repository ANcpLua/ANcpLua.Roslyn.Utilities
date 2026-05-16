using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Matching;

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
    public ParameterMatcher CancellationToken()
    {
        return OfType("CancellationToken");
    }
}
