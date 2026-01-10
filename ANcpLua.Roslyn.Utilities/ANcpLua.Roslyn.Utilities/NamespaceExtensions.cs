using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="INamespaceSymbol" />.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class NamespaceExtensions
{
    /// <summary>
    ///     Checks if a namespace matches the given parts (zero-allocation).
    /// </summary>
#pragma warning disable MA0109 // Span overload not practical for netstandard2.0
    public static bool IsNamespace(this INamespaceSymbol? namespaceSymbol, string[] namespaceParts)
#pragma warning restore MA0109
    {
        for (var i = namespaceParts.Length - 1; i >= 0; i--)
        {
            if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
                return false;

            if (!string.Equals(namespaceParts[i], namespaceSymbol.Name, StringComparison.Ordinal))
                return false;

            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        return namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace;
    }

    /// <summary>
    ///     Gets all types in a namespace recursively, including nested types.
    /// </summary>
    public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamespaceSymbol ns)
    {
        foreach (var member in ns.GetMembers())
        {
            switch (member)
            {
                case INamedTypeSymbol type:
                {
                    yield return type;
                    foreach (var nested in GetNestedTypes(type))
                        yield return nested;
                    break;
                }
                case INamespaceSymbol nestedNs:
                {
                    foreach (var nestedType in nestedNs.GetAllTypes())
                        yield return nestedType;
                    break;
                }
            }
        }
    }

    public static IEnumerable<INamespaceSymbol> GetAllNamespaces(this INamespaceSymbol ns)
    {
        yield return ns;
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol nestedNs)
            {
                foreach (var descendant in nestedNs.GetAllNamespaces())
                    yield return descendant;
            }
        }
    }

    public static IEnumerable<INamedTypeSymbol> GetTypesRecursive(this IAssemblySymbol assembly) =>
        assembly.GlobalNamespace.GetAllTypes();

    public static IEnumerable<INamedTypeSymbol> GetPublicTypes(this IAssemblySymbol assembly) =>
        assembly.GlobalNamespace.GetAllTypes().Where(t => t.IsVisibleOutsideOfAssembly());

    public static IEnumerable<INamedTypeSymbol> GetPublicTypes(this INamespaceSymbol ns) =>
        ns.GetAllTypes().Where(t => t.IsVisibleOutsideOfAssembly());

    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var deepNested in GetNestedTypes(nested))
                yield return deepNested;
        }
    }
}