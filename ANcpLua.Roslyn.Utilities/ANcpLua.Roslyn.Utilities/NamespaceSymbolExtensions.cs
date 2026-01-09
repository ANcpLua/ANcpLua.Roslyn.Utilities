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
static class NamespaceSymbolExtensions
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
}
