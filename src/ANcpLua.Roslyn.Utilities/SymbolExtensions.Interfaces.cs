using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class SymbolExtensions
{
    // Symbol kinds that can have interface implementations. Centralising the
    // set keeps ExplicitOrImplicitInterfaceImplementations at CC=2 instead of a
    // chain of && conditions.
    private static readonly HashSet<SymbolKind> s_implementableSymbolKinds =
    [
        SymbolKind.Method,
        SymbolKind.Property,
        SymbolKind.Event
    ];

    /// <summary>
    ///     Checks if a method explicitly implements a specific interface method.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="interfaceMethod">The interface method to check against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="method" /> explicitly implements
    ///     <paramref name="interfaceMethod" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="ExplicitOrImplicitInterfaceImplementations" />
    /// <seealso cref="ExplicitInterfaceImplementations" />
    public static bool ExplicitlyImplements(this IMethodSymbol method, IMethodSymbol interfaceMethod)
    {
        foreach (var impl in method.ExplicitInterfaceImplementations)
            if (SymbolEqualityComparer.Default.Equals(impl, interfaceMethod))
                return true;

        return false;
    }

    /// <summary>
    ///     Gets all explicit and implicit interface implementations for a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get implementations for.</param>
    /// <returns>
    ///     An immutable array of interface members that <paramref name="symbol" /> implements,
    ///     both explicitly and implicitly. Returns an empty array if the symbol is not a
    ///     method, property, or event.
    /// </returns>
    /// <seealso cref="ExplicitInterfaceImplementations" />
    /// <seealso cref="ExplicitlyImplements" />
    public static ImmutableArray<ISymbol> ExplicitOrImplicitInterfaceImplementations(this ISymbol symbol)
    {
        if (!s_implementableSymbolKinds.Contains(symbol.Kind))
            return [];

        var containingType = symbol.ContainingType;
        if (containingType is null)
            return [];

        var builder = ImmutableArray.CreateBuilder<ISymbol>();
        foreach (var iface in containingType.AllInterfaces)
        foreach (var interfaceMember in iface.GetMembers())
        {
            var impl = containingType.FindImplementationForInterfaceMember(interfaceMember);
            if (SymbolEqualityComparer.Default.Equals(symbol, impl))
                builder.Add(interfaceMember);
        }

        return builder.ToImmutable();
    }

    /// <summary>
    ///     Gets all explicit interface implementations for a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get explicit implementations for.</param>
    /// <returns>
    ///     An immutable array of interface members that <paramref name="symbol" /> explicitly implements.
    ///     Returns an empty array if the symbol is not an event, method, or property.
    /// </returns>
    /// <seealso cref="ExplicitOrImplicitInterfaceImplementations" />
    /// <seealso cref="ExplicitlyImplements" />
    public static ImmutableArray<ISymbol> ExplicitInterfaceImplementations(this ISymbol symbol)
    {
        return symbol switch
        {
            IEventSymbol @event => ImmutableArray<ISymbol>.CastUp(@event.ExplicitInterfaceImplementations),
            IMethodSymbol method => ImmutableArray<ISymbol>.CastUp(method.ExplicitInterfaceImplementations),
            IPropertySymbol property => ImmutableArray<ISymbol>.CastUp(property.ExplicitInterfaceImplementations),
            _ => []
        };
    }
}
