using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="INamespaceSymbol" />.
/// </summary>
public static class NamespaceSymbolExtensions
{
    /// <summary>
    ///     Checks if a namespace matches the given parts (zero-allocation).
    /// </summary>
    public static bool IsNamespace(this INamespaceSymbol? namespaceSymbol, string[] namespaceParts)
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
    ///     Checks if a namespace matches the given namespace string.
    /// </summary>
    public static bool IsNamespace(this INamespaceSymbol? namespaceSymbol, string namespaceName)
    {
        return IsNamespace(namespaceSymbol, namespaceName.Split('.'));
    }
}
