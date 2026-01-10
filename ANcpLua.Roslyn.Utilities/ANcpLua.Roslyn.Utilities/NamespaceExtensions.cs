using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="INamespaceSymbol" /> and <see cref="IAssemblySymbol" />.
/// </summary>
/// <remarks>
///     <para>
///         This class provides utilities for navigating and querying namespace hierarchies,
///         including recursive type enumeration and namespace matching.
///     </para>
///     <list type="bullet">
///         <item><description>Zero-allocation namespace matching with <see cref="IsNamespace" /></description></item>
///         <item><description>Recursive type enumeration with <see cref="GetAllTypes" /></description></item>
///         <item><description>Assembly-level type queries with <see cref="GetTypesRecursive" /> and <see cref="GetPublicTypes(IAssemblySymbol)" /></description></item>
///     </list>
/// </remarks>
/// <seealso cref="SymbolExtensions" />
/// <seealso cref="TypeSymbolExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class NamespaceExtensions
{
    /// <summary>
    ///     Checks if a namespace matches the given parts using zero-allocation comparison.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method walks the namespace hierarchy from innermost to outermost,
    ///         comparing each part against the provided array. The comparison uses
    ///         ordinal string comparison for exact matching.
    ///     </para>
    ///     <para>
    ///         The method is designed for zero-allocation when the namespace parts array
    ///         is pre-allocated and reused.
    ///     </para>
    /// </remarks>
    /// <param name="namespaceSymbol">The namespace symbol to check. May be <c>null</c>.</param>
    /// <param name="namespaceParts">
    ///     The expected namespace parts in order from outermost to innermost.
    ///     For example, <c>["System", "Collections", "Generic"]</c> matches <c>System.Collections.Generic</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if <paramref name="namespaceSymbol" /> exactly matches the namespace
    ///     defined by <paramref name="namespaceParts" />; otherwise, <c>false</c>.
    ///     Returns <c>false</c> if <paramref name="namespaceSymbol" /> is <c>null</c>
    ///     and <paramref name="namespaceParts" /> is not empty.
    /// </returns>
    /// <example>
    ///     <code>
    ///     var parts = new[] { "System", "Collections", "Generic" };
    ///     bool isGenericCollections = namespaceSymbol.IsNamespace(parts);
    ///     </code>
    /// </example>
    /// <seealso cref="GetAllNamespaces" />
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
    /// <remarks>
    ///     <para>
    ///         This method performs a depth-first traversal of the namespace hierarchy,
    ///         yielding all <see cref="INamedTypeSymbol" /> instances found. For each type,
    ///         it also recursively enumerates all nested types.
    ///     </para>
    ///     <para>
    ///         The enumeration is lazy and uses <c>yield return</c> for efficient iteration.
    ///     </para>
    /// </remarks>
    /// <param name="ns">The namespace symbol to enumerate types from.</param>
    /// <returns>
    ///     An enumerable sequence of all <see cref="INamedTypeSymbol" /> instances
    ///     in <paramref name="ns" /> and all nested namespaces, including nested types.
    /// </returns>
    /// <example>
    ///     <code>
    ///     foreach (var type in namespaceSymbol.GetAllTypes())
    ///     {
    ///         Console.WriteLine(type.Name);
    ///     }
    ///     </code>
    /// </example>
    /// <seealso cref="GetPublicTypes(INamespaceSymbol)" />
    /// <seealso cref="GetAllNamespaces" />
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

    /// <summary>
    ///     Gets all namespaces within the specified namespace recursively, including the namespace itself.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method performs a depth-first traversal of the namespace hierarchy,
    ///         yielding the starting namespace followed by all descendant namespaces.
    ///     </para>
    ///     <para>
    ///         The enumeration is lazy and uses <c>yield return</c> for efficient iteration.
    ///     </para>
    /// </remarks>
    /// <param name="ns">The namespace symbol to enumerate namespaces from.</param>
    /// <returns>
    ///     An enumerable sequence of <see cref="INamespaceSymbol" /> instances,
    ///     starting with <paramref name="ns" /> followed by all nested namespaces.
    /// </returns>
    /// <example>
    ///     <code>
    ///     foreach (var childNs in rootNamespace.GetAllNamespaces())
    ///     {
    ///         Console.WriteLine(childNs.ToDisplayString());
    ///     }
    ///     </code>
    /// </example>
    /// <seealso cref="GetAllTypes" />
    /// <seealso cref="IsNamespace" />
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

    /// <summary>
    ///     Gets all types in the assembly recursively, including nested types.
    /// </summary>
    /// <remarks>
    ///     This is a convenience method that calls <see cref="GetAllTypes" /> on the
    ///     assembly's <see cref="IAssemblySymbol.GlobalNamespace" />.
    /// </remarks>
    /// <param name="assembly">The assembly symbol to enumerate types from.</param>
    /// <returns>
    ///     An enumerable sequence of all <see cref="INamedTypeSymbol" /> instances
    ///     in the assembly, including nested types.
    /// </returns>
    /// <example>
    ///     <code>
    ///     foreach (var type in compilation.Assembly.GetTypesRecursive())
    ///     {
    ///         Console.WriteLine(type.ToDisplayString());
    ///     }
    ///     </code>
    /// </example>
    /// <seealso cref="GetPublicTypes(IAssemblySymbol)" />
    /// <seealso cref="GetAllTypes" />
    public static IEnumerable<INamedTypeSymbol> GetTypesRecursive(this IAssemblySymbol assembly) =>
        assembly.GlobalNamespace.GetAllTypes();

    /// <summary>
    ///     Gets all public types in the assembly that are visible outside the assembly.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method filters types using <see cref="SymbolExtensions.IsVisibleOutsideOfAssembly" />,
    ///         which checks that the type and all its containing types have public accessibility.
    ///     </para>
    ///     <para>
    ///         Use this method when you need to enumerate types that form the public API surface
    ///         of an assembly.
    ///     </para>
    /// </remarks>
    /// <param name="assembly">The assembly symbol to enumerate public types from.</param>
    /// <returns>
    ///     An enumerable sequence of <see cref="INamedTypeSymbol" /> instances that are
    ///     visible outside the assembly.
    /// </returns>
    /// <example>
    ///     <code>
    ///     var publicApi = compilation.Assembly.GetPublicTypes().ToList();
    ///     Console.WriteLine($"Public API surface: {publicApi.Count} types");
    ///     </code>
    /// </example>
    /// <seealso cref="GetTypesRecursive" />
    /// <seealso cref="GetPublicTypes(INamespaceSymbol)" />
    /// <seealso cref="SymbolExtensions.IsVisibleOutsideOfAssembly" />
    public static IEnumerable<INamedTypeSymbol> GetPublicTypes(this IAssemblySymbol assembly) =>
        assembly.GlobalNamespace.GetAllTypes().Where(t => t.IsVisibleOutsideOfAssembly());

    /// <summary>
    ///     Gets all public types in the namespace that are visible outside the assembly.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method enumerates all types in the namespace (including nested namespaces)
    ///         and filters them using <see cref="SymbolExtensions.IsVisibleOutsideOfAssembly" />.
    ///     </para>
    ///     <para>
    ///         A type is visible outside the assembly if it has public accessibility and all
    ///         its containing types (if any) also have public accessibility.
    ///     </para>
    /// </remarks>
    /// <param name="ns">The namespace symbol to enumerate public types from.</param>
    /// <returns>
    ///     An enumerable sequence of <see cref="INamedTypeSymbol" /> instances that are
    ///     visible outside the assembly.
    /// </returns>
    /// <example>
    ///     <code>
    ///     var publicTypes = myNamespace.GetPublicTypes().ToList();
    ///     foreach (var type in publicTypes)
    ///     {
    ///         Console.WriteLine($"Public: {type.Name}");
    ///     }
    ///     </code>
    /// </example>
    /// <seealso cref="GetAllTypes" />
    /// <seealso cref="GetPublicTypes(IAssemblySymbol)" />
    /// <seealso cref="SymbolExtensions.IsVisibleOutsideOfAssembly" />
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
