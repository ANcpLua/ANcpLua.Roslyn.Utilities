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
    ///     Gets all type parameters from a symbol and its containing types.
    /// </summary>
    /// <param name="symbol">The symbol to get type parameters from.</param>
    /// <returns>
    ///     An immutable array containing all type parameters from the symbol
    ///     and its entire containing type hierarchy.
    /// </returns>
    /// <seealso cref="GetTypeParameters" />
    /// <seealso cref="GetAllTypeArguments" />
    public static ImmutableArray<ITypeParameterSymbol> GetAllTypeParameters(this ISymbol? symbol)
    {
        var results = ImmutableArray.CreateBuilder<ITypeParameterSymbol>();
        while (symbol is not null)
        {
            results.AddRange(symbol.GetTypeParameters());
            symbol = symbol.ContainingType;
        }

        return results.ToImmutable();
    }

    /// <summary>
    ///     Gets the type parameters of a method or named type symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get type parameters from.</param>
    /// <returns>
    ///     The type parameters of the symbol if it is an <see cref="IMethodSymbol" /> or
    ///     <see cref="INamedTypeSymbol" />; otherwise, an empty immutable array.
    /// </returns>
    /// <seealso cref="GetAllTypeParameters" />
    /// <seealso cref="GetTypeArguments" />
    public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol? symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => m.TypeParameters,
            INamedTypeSymbol nt => nt.TypeParameters,
            _ => []
        };
    }

    /// <summary>
    ///     Gets the type arguments of a method or named type symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get type arguments from.</param>
    /// <returns>
    ///     The type arguments of the symbol if it is an <see cref="IMethodSymbol" /> or
    ///     <see cref="INamedTypeSymbol" />; otherwise, an empty immutable array.
    /// </returns>
    /// <seealso cref="GetAllTypeArguments" />
    /// <seealso cref="GetTypeParameters" />
    private static ImmutableArray<ITypeSymbol> GetTypeArguments(this ISymbol? symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => m.TypeArguments,
            INamedTypeSymbol nt => nt.TypeArguments,
            _ => []
        };
    }

    /// <summary>
    ///     Gets all type arguments from a symbol and its containing types.
    /// </summary>
    /// <param name="symbol">The symbol to get type arguments from.</param>
    /// <returns>
    ///     An immutable array containing all type arguments from the symbol
    ///     and its entire containing type hierarchy.
    /// </returns>
    /// <seealso cref="GetTypeArguments" />
    /// <seealso cref="GetAllTypeParameters" />
    public static ImmutableArray<ITypeSymbol> GetAllTypeArguments(this ISymbol symbol)
    {
        var results = ImmutableArray.CreateBuilder<ITypeSymbol>();
        results.AddRange(symbol.GetTypeArguments());

        var containingType = symbol.ContainingType;
        while (containingType is not null)
        {
            results.AddRange(containingType.GetTypeArguments());
            containingType = containingType.ContainingType;
        }

        return results.ToImmutable();
    }
}
