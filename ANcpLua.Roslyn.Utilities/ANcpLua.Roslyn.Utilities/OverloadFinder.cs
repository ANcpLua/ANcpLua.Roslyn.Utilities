using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides utilities for finding method overloads in a compilation.
/// </summary>
/// <remarks>
///     <para>
///         This class enables analyzers and source generators to discover method overloads
///         based on various criteria such as parameter types, return types, and parameter counts.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Searches for overloads within the same containing type.</description>
///         </item>
///         <item>
///             <description>Supports filtering out obsolete methods via the <c>includeObsolete</c> parameter.</description>
///         </item>
///         <item>
///             <description>Handles single and multiple additional parameter type searches.</description>
///         </item>
///         <item>
///             <description>Provides both existence checks and overload retrieval methods.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="compilation">The <see cref="Compilation" /> used to resolve the <c>System.ObsoleteAttribute</c> type.</param>
/// <seealso cref="IMethodSymbol" />
/// <seealso cref="IInvocationOperation" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class OverloadFinder(Compilation compilation)
{
    private readonly INamedTypeSymbol? _obsoleteAttribute =
        compilation.GetTypeByMetadataName("System.ObsoleteAttribute");

    /// <summary>
    ///     Determines whether an overload of the specified method exists that includes an additional parameter of the given
    ///     type.
    /// </summary>
    /// <param name="method">The method to find overloads for.</param>
    /// <param name="additionalParamType">The type of the additional parameter that the overload must have.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if an overload exists with the additional parameter type; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="FindOverloadWithParameter" />
    public bool HasOverloadWithParameter(IMethodSymbol method, ITypeSymbol additionalParamType,
        bool includeObsolete = false) =>
        FindOverloadWithParameter(method, additionalParamType, includeObsolete) is not null;

    /// <summary>
    ///     Determines whether an overload of the target method of the specified invocation exists that includes an additional
    ///     parameter of the given type.
    /// </summary>
    /// <param name="operation">The invocation operation whose target method is used to find overloads.</param>
    /// <param name="additionalParamType">The type of the additional parameter that the overload must have.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if an overload exists with the additional parameter type; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="FindOverloadWithParameter" />
    public bool HasOverloadWithParameter(IInvocationOperation operation, ITypeSymbol additionalParamType,
        bool includeObsolete = false) =>
        FindOverloadWithParameter(operation.TargetMethod, additionalParamType, includeObsolete) is not null;

    /// <summary>
    ///     Determines whether an overload of the specified method exists that includes all of the specified additional
    ///     parameter types.
    /// </summary>
    /// <param name="method">The method to find overloads for.</param>
    /// <param name="additionalParamTypes">The types of the additional parameters that the overload must have.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if an overload exists with all the additional parameter types; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="FindOverloadWithParameters" />
    public bool HasOverloadWithParameters(IMethodSymbol method, ReadOnlySpan<ITypeSymbol> additionalParamTypes,
        bool includeObsolete = false) =>
        FindOverloadWithParameters(method, additionalParamTypes, includeObsolete) is not null;

    /// <summary>
    ///     Determines whether an overload of the target method of the specified invocation exists that includes all of the
    ///     specified additional parameter types.
    /// </summary>
    /// <param name="operation">The invocation operation whose target method is used to find overloads.</param>
    /// <param name="additionalParamTypes">The types of the additional parameters that the overload must have.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if an overload exists with all the additional parameter types; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="FindOverloadWithParameters" />
    public bool HasOverloadWithParameters(IInvocationOperation operation,
        ReadOnlySpan<ITypeSymbol> additionalParamTypes, bool includeObsolete = false) =>
        FindOverloadWithParameters(operation.TargetMethod, additionalParamTypes, includeObsolete) is not null;

    /// <summary>
    ///     Finds an overload of the specified method that includes an additional parameter of the given type.
    /// </summary>
    /// <remarks>
    ///     The overload must have all parameters of the original method plus exactly one additional parameter
    ///     of the specified type, in any position.
    /// </remarks>
    /// <param name="method">The method to find overloads for.</param>
    /// <param name="additionalParamType">The type of the additional parameter that the overload must have.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     The matching overload <see cref="IMethodSymbol" /> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <seealso cref="HasOverloadWithParameter(IMethodSymbol, ITypeSymbol, bool)" />
    public IMethodSymbol? FindOverloadWithParameter(IMethodSymbol method, ITypeSymbol additionalParamType,
        bool includeObsolete = false)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
            return null;

        foreach (var member in containingType.GetMembers(method.Name))
        {
            if (member is not IMethodSymbol candidate || candidate.Equals(method, SymbolEqualityComparer.Default))
                continue;

            if (!includeObsolete && IsObsolete(candidate))
                continue;

            if (HasParameterOfType(candidate, additionalParamType) &&
                HasSimilarParameters(method, candidate, additionalParamType))
                return candidate;
        }

        return null;
    }

    /// <summary>
    ///     Finds an overload of the specified method that includes all of the specified additional parameter types.
    /// </summary>
    /// <remarks>
    ///     The overload must have all parameters of the original method plus all of the specified additional
    ///     parameter types, in any position.
    /// </remarks>
    /// <param name="method">The method to find overloads for.</param>
    /// <param name="additionalParamTypes">The types of the additional parameters that the overload must have.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     The matching overload <see cref="IMethodSymbol" /> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <seealso cref="HasOverloadWithParameters(IMethodSymbol, ReadOnlySpan{ITypeSymbol}, bool)" />
    public IMethodSymbol? FindOverloadWithParameters(IMethodSymbol method,
        ReadOnlySpan<ITypeSymbol> additionalParamTypes, bool includeObsolete = false)
    {
        if (additionalParamTypes.IsEmpty)
            return null;

        var containingType = method.ContainingType;
        if (containingType is null)
            return null;

        foreach (var member in containingType.GetMembers(method.Name))
        {
            if (member is not IMethodSymbol candidate || candidate.Equals(method, SymbolEqualityComparer.Default))
                continue;

            if (!includeObsolete && IsObsolete(candidate))
                continue;

            if (HasAllParameterTypes(candidate, additionalParamTypes) &&
                HasSimilarParametersMultiple(method, candidate, additionalParamTypes))
                return candidate;
        }

        return null;
    }

    /// <summary>
    ///     Finds an overload of the specified method that matches the given predicate.
    /// </summary>
    /// <param name="method">The method to find overloads for.</param>
    /// <param name="predicate">A function to test each candidate overload for a match.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     The first matching overload <see cref="IMethodSymbol" /> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <seealso cref="FindAllOverloads" />
    public IMethodSymbol? FindOverload(IMethodSymbol method, Func<IMethodSymbol, bool> predicate,
        bool includeObsolete = false)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
            return null;

        foreach (var member in containingType.GetMembers(method.Name))
        {
            if (member is not IMethodSymbol candidate || candidate.Equals(method, SymbolEqualityComparer.Default))
                continue;

            if (!includeObsolete && IsObsolete(candidate))
                continue;

            if (predicate(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>
    ///     Finds all overloads of the specified method.
    /// </summary>
    /// <param name="method">The method to find overloads for.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     An enumerable of all overload <see cref="IMethodSymbol" /> instances, excluding the original method.
    /// </returns>
    /// <seealso cref="FindOverload" />
    public IEnumerable<IMethodSymbol> FindAllOverloads(IMethodSymbol method, bool includeObsolete = false)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
            yield break;

        foreach (var member in containingType.GetMembers(method.Name))
        {
            if (member is not IMethodSymbol candidate || candidate.Equals(method, SymbolEqualityComparer.Default))
                continue;

            if (!includeObsolete && IsObsolete(candidate))
                continue;

            yield return candidate;
        }
    }

    /// <summary>
    ///     Determines whether an overload of the specified method exists that has the given return type.
    /// </summary>
    /// <param name="method">The method to find overloads for.</param>
    /// <param name="returnType">The return type that the overload must have.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if an overload exists with the specified return type; otherwise, <c>false</c>.
    /// </returns>
    public bool HasOverloadWithReturnType(IMethodSymbol method, ITypeSymbol returnType, bool includeObsolete = false)
    {
        foreach (var overload in FindAllOverloads(method, includeObsolete))
            if (overload.ReturnType.IsEqualTo(returnType))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether an overload of the specified method exists that has fewer parameters.
    /// </summary>
    /// <param name="method">The method to find overloads for.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if an overload exists with fewer parameters; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="HasOverloadWithMoreParameters" />
    public bool HasOverloadWithFewerParameters(IMethodSymbol method, bool includeObsolete = false)
    {
        foreach (var overload in FindAllOverloads(method, includeObsolete))
            if (overload.Parameters.Length < method.Parameters.Length)
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether an overload of the specified method exists that has more parameters.
    /// </summary>
    /// <param name="method">The method to find overloads for.</param>
    /// <param name="includeObsolete">
    ///     If <c>true</c>, includes methods marked with <see cref="System.ObsoleteAttribute" />;
    ///     otherwise, obsolete methods are excluded. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if an overload exists with more parameters; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="HasOverloadWithFewerParameters" />
    public bool HasOverloadWithMoreParameters(IMethodSymbol method, bool includeObsolete = false)
    {
        foreach (var overload in FindAllOverloads(method, includeObsolete))
            if (overload.Parameters.Length > method.Parameters.Length)
                return true;

        return false;
    }

    private static bool HasParameterOfType(IMethodSymbol method, ITypeSymbol type)
    {
        foreach (var param in method.Parameters)
            if (param.Type.IsEqualTo(type))
                return true;

        return false;
    }

    private static bool HasAllParameterTypes(IMethodSymbol method, ReadOnlySpan<ITypeSymbol> types)
    {
        foreach (var type in types)
        {
            var found = false;
            foreach (var param in method.Parameters)
                if (param.Type.IsEqualTo(type))
                {
                    found = true;
                    break;
                }

            if (!found)
                return false;
        }

        return true;
    }

    private static bool HasSimilarParameters(IMethodSymbol original, IMethodSymbol candidate,
        ITypeSymbol additionalType)
    {
        // Candidate should have all parameters of original plus the additional one
        if (candidate.Parameters.Length != original.Parameters.Length + 1)
            return false;

        var originalParams = original.Parameters;
        var candidateParams = candidate.Parameters;

        var additionalFound = false;
        var originalIndex = 0;

        for (var i = 0; i < candidateParams.Length; i++)
        {
            var candidateParam = candidateParams[i];

            if (!additionalFound && candidateParam.Type.IsEqualTo(additionalType))
            {
                additionalFound = true;
                continue;
            }

            if (originalIndex >= originalParams.Length)
                return false;

            if (!candidateParam.Type.IsEqualTo(originalParams[originalIndex].Type))
                return false;

            originalIndex++;
        }

        return additionalFound && originalIndex == originalParams.Length;
    }

    private static bool HasSimilarParametersMultiple(IMethodSymbol original, IMethodSymbol candidate,
        ReadOnlySpan<ITypeSymbol> additionalTypes)
    {
        if (candidate.Parameters.Length != original.Parameters.Length + additionalTypes.Length)
            return false;

        var additionalTypesRemaining = additionalTypes.ToArray().ToList();
        var originalParams = original.Parameters.ToList();

        foreach (var candidateParam in candidate.Parameters)
        {
            var matchedAdditional = false;
            for (var i = 0; i < additionalTypesRemaining.Count; i++)
                if (candidateParam.Type.IsEqualTo(additionalTypesRemaining[i]))
                {
                    additionalTypesRemaining.RemoveAt(i);
                    matchedAdditional = true;
                    break;
                }

            if (matchedAdditional)
                continue;

            var matchedOriginal = false;
            for (var i = 0; i < originalParams.Count; i++)
                if (candidateParam.Type.IsEqualTo(originalParams[i].Type))
                {
                    originalParams.RemoveAt(i);
                    matchedOriginal = true;
                    break;
                }

            if (!matchedOriginal)
                return false;
        }

        return additionalTypesRemaining.Count is 0 && originalParams.Count is 0;
    }

    private bool IsObsolete(IMethodSymbol method) => _obsoleteAttribute is not null && method.HasAttribute(_obsoleteAttribute);
}
