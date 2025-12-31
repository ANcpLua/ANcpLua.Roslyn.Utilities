using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="IMethodSymbol" />.
/// </summary>
public static class MethodSymbolExtensions
{
    /// <summary>
    ///     Checks if a method is an interface implementation.
    /// </summary>
    public static bool IsInterfaceImplementation(this IMethodSymbol symbol)
    {
        if (symbol.ExplicitInterfaceImplementations.Length > 0)
            return true;

        return symbol.GetImplementingInterfaceSymbol() is not null;
    }

    /// <summary>
    ///     Checks if a property is an interface implementation.
    /// </summary>
    public static bool IsInterfaceImplementation(this IPropertySymbol symbol)
    {
        if (symbol.ExplicitInterfaceImplementations.Length > 0)
            return true;

        return ((ISymbol)symbol).GetImplementingInterfaceMember() is not null;
    }

    /// <summary>
    ///     Checks if an event is an interface implementation.
    /// </summary>
    public static bool IsInterfaceImplementation(this IEventSymbol symbol)
    {
        if (symbol.ExplicitInterfaceImplementations.Length > 0)
            return true;

        return ((ISymbol)symbol).GetImplementingInterfaceMember() is not null;
    }

    /// <summary>
    ///     Gets the interface method that this method implements.
    /// </summary>
    public static IMethodSymbol? GetImplementingInterfaceSymbol(this IMethodSymbol symbol)
    {
        if (symbol.ExplicitInterfaceImplementations.Any())
            return symbol.ExplicitInterfaceImplementations.First();

        return (IMethodSymbol?)((ISymbol)symbol).GetImplementingInterfaceMember();
    }

    private static ISymbol? GetImplementingInterfaceMember(this ISymbol symbol)
    {
        if (symbol.ContainingType is null)
            return null;

        foreach (var iface in symbol.ContainingType.AllInterfaces)
        {
            foreach (var interfaceMember in iface.GetMembers())
            {
                var impl = symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
                if (SymbolEqualityComparer.Default.Equals(symbol, impl))
                    return interfaceMember;
            }
        }

        return null;
    }

    /// <summary>
    ///     Checks if a method is equal to or overrides a base method.
    /// </summary>
    public static bool IsOrOverrideMethod(this IMethodSymbol? symbol, IMethodSymbol? baseMethod)
    {
        if (symbol is null || baseMethod is null)
            return false;

        if (SymbolEqualityComparer.Default.Equals(symbol, baseMethod))
            return true;

        while (symbol is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(symbol, baseMethod))
                return true;

            symbol = symbol.OverriddenMethod;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a method overrides a specific base method.
    /// </summary>
    public static bool OverridesMethod(this IMethodSymbol? symbol, IMethodSymbol? baseMethod)
    {
        if (symbol is null || baseMethod is null)
            return false;

        var current = symbol.OverriddenMethod;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseMethod))
                return true;

            current = current.OverriddenMethod;
        }

        return false;
    }
}