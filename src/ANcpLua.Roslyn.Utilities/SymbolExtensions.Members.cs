using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class SymbolExtensions
{
    /// <summary>
    ///     Gets all members of a type including members inherited from base types.
    /// </summary>
    /// <param name="symbol">The type symbol to get members from.</param>
    /// <returns>
    ///     An enumerable of all members from the type and its entire base type hierarchy.
    /// </returns>
    /// <seealso cref="GetAllMembers(ITypeSymbol?, string)" />
    /// <seealso cref="GetAllMembers(INamespaceOrTypeSymbol?, string)" />
    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol? symbol)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers())
                yield return member;

            symbol = symbol.BaseType;
        }
    }

    /// <summary>
    ///     Gets all members with the specified name from a type including inherited members.
    /// </summary>
    /// <param name="symbol">The type symbol to get members from.</param>
    /// <param name="name">The name of the members to find.</param>
    /// <returns>
    ///     An enumerable of all members with the specified name from the type and its base type hierarchy.
    /// </returns>
    /// <seealso cref="GetAllMembers(ITypeSymbol?)" />
    /// <seealso cref="GetAllMembers(INamespaceOrTypeSymbol?, string)" />
    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol? symbol, string name)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers(name))
                yield return member;

            symbol = symbol.BaseType;
        }
    }

    /// <summary>
    ///     Gets all members with the specified name from a namespace or type symbol,
    ///     including inherited members and interface members for interface types.
    /// </summary>
    /// <param name="symbol">The namespace or type symbol to get members from.</param>
    /// <param name="name">The name of the members to find.</param>
    /// <returns>
    ///     An enumerable of all members with the specified name. For interface types,
    ///     this includes members from all inherited interfaces.
    /// </returns>
    /// <seealso cref="GetAllMembers(ITypeSymbol?)" />
    /// <seealso cref="GetAllMembers(ITypeSymbol?, string)" />
    public static IEnumerable<ISymbol> GetAllMembers(this INamespaceOrTypeSymbol? symbol, string name)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers(name))
                yield return member;

            if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Interface } interfaceSymbol)
                foreach (var iface in interfaceSymbol.AllInterfaces)
                foreach (var member in iface.GetMembers(name))
                    yield return member;

            if (symbol is ITypeSymbol typeSymbol)
                symbol = typeSymbol.BaseType;
            else
                yield break;
        }
    }

    /// <summary>
    ///     Gets a single method by name from a named type symbol.
    /// </summary>
    /// <param name="type">The type to search for the method.</param>
    /// <param name="name">The name of the method to find.</param>
    /// <returns>
    ///     The <see cref="IMethodSymbol" /> if exactly one method with the specified name exists;
    ///     <c>null</c> if no method is found or if multiple methods with the same name exist (overloads).
    /// </returns>
    /// <seealso cref="GetProperty" />
    public static IMethodSymbol? GetMethod(this INamedTypeSymbol type, string name)
    {
        IMethodSymbol? result = null;
        foreach (var member in type.GetMembers(name))
            if (member is IMethodSymbol method)
            {
                if (result is not null)
                    return null; // Multiple methods with same name
                result = method;
            }

        return result;
    }

    /// <summary>
    ///     Gets a single property by name from a named type symbol.
    /// </summary>
    /// <param name="type">The type to search for the property.</param>
    /// <param name="name">The name of the property to find.</param>
    /// <returns>
    ///     The <see cref="IPropertySymbol" /> if a property with the specified name exists;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <seealso cref="GetMethod" />
    public static IPropertySymbol? GetProperty(this INamedTypeSymbol type, string name)
    {
        foreach (var member in type.GetMembers(name))
            if (member is IPropertySymbol property)
                return property;

        return null;
    }
}
