using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
sealed class OverloadFinder(Compilation compilation)
{
    private readonly INamedTypeSymbol? _obsoleteAttribute =
        compilation.GetTypeByMetadataName("System.ObsoleteAttribute");

    public bool HasOverloadWithParameter(IMethodSymbol method, ITypeSymbol additionalParamType,
        bool includeObsolete = false) =>
        FindOverloadWithParameter(method, additionalParamType, includeObsolete) is not null;

    public bool HasOverloadWithParameter(IInvocationOperation operation, ITypeSymbol additionalParamType,
        bool includeObsolete = false) =>
        FindOverloadWithParameter(operation.TargetMethod, additionalParamType, includeObsolete) is not null;

    public bool HasOverloadWithParameters(IMethodSymbol method, ReadOnlySpan<ITypeSymbol> additionalParamTypes,
        bool includeObsolete = false) =>
        FindOverloadWithParameters(method, additionalParamTypes, includeObsolete) is not null;

    public bool HasOverloadWithParameters(IInvocationOperation operation,
        ReadOnlySpan<ITypeSymbol> additionalParamTypes, bool includeObsolete = false) =>
        FindOverloadWithParameters(operation.TargetMethod, additionalParamTypes, includeObsolete) is not null;

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
            {
                return candidate;
            }
        }

        return null;
    }

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
            {
                return candidate;
            }
        }

        return null;
    }

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

    public bool HasOverloadWithReturnType(IMethodSymbol method, ITypeSymbol returnType, bool includeObsolete = false)
    {
        foreach (var overload in FindAllOverloads(method, includeObsolete))
        {
            if (overload.ReturnType.IsEqualTo(returnType))
                return true;
        }

        return false;
    }

    public bool HasOverloadWithFewerParameters(IMethodSymbol method, bool includeObsolete = false)
    {
        foreach (var overload in FindAllOverloads(method, includeObsolete))
        {
            if (overload.Parameters.Length < method.Parameters.Length)
                return true;
        }

        return false;
    }

    public bool HasOverloadWithMoreParameters(IMethodSymbol method, bool includeObsolete = false)
    {
        foreach (var overload in FindAllOverloads(method, includeObsolete))
        {
            if (overload.Parameters.Length > method.Parameters.Length)
                return true;
        }

        return false;
    }

    private static bool HasParameterOfType(IMethodSymbol method, ITypeSymbol type)
    {
        foreach (var param in method.Parameters)
        {
            if (param.Type.IsEqualTo(type))
                return true;
        }

        return false;
    }

    private static bool HasAllParameterTypes(IMethodSymbol method, ReadOnlySpan<ITypeSymbol> types)
    {
        foreach (var type in types)
        {
            var found = false;
            foreach (var param in method.Parameters)
            {
                if (param.Type.IsEqualTo(type))
                {
                    found = true;
                    break;
                }
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
            {
                if (candidateParam.Type.IsEqualTo(additionalTypesRemaining[i]))
                {
                    additionalTypesRemaining.RemoveAt(i);
                    matchedAdditional = true;
                    break;
                }
            }

            if (matchedAdditional)
                continue;

            var matchedOriginal = false;
            for (var i = 0; i < originalParams.Count; i++)
            {
                if (candidateParam.Type.IsEqualTo(originalParams[i].Type))
                {
                    originalParams.RemoveAt(i);
                    matchedOriginal = true;
                    break;
                }
            }

            if (!matchedOriginal)
                return false;
        }

        return additionalTypesRemaining.Count == 0 && originalParams.Count == 0;
    }

    private bool IsObsolete(IMethodSymbol method) =>
        _obsoleteAttribute is not null && method.HasAttribute(_obsoleteAttribute);
}