using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for working with <see cref="IMethodSymbol" />,
///     <see cref="IPropertySymbol" />, and <see cref="IEventSymbol" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This class provides utilities for analyzing method, property, and event symbols
///         in the context of interface implementations and method overrides.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Detect explicit and implicit interface implementations</description>
///         </item>
///         <item>
///             <description>Find the interface member that a symbol implements</description>
///         </item>
///         <item>
///             <description>Analyze method override chains</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="IMethodSymbol" />
/// <seealso cref="IPropertySymbol" />
/// <seealso cref="IEventSymbol" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class MethodSymbolExtensions
{
    /// <summary>
    ///     Determines whether the specified method is an interface implementation.
    /// </summary>
    /// <param name="symbol">The method symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if the method explicitly or implicitly implements an interface method;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method checks both explicit interface implementations (where the method
    ///         is declared with the interface name prefix) and implicit implementations
    ///         (where the method matches an interface member by signature).
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <b>Explicit implementation:</b> The method is declared as <c>void IInterface.Method()</c>.
    ///                 Detected via <see cref="IMethodSymbol.ExplicitInterfaceImplementations" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>Implicit implementation:</b> The method has matching signature and is public.
    ///                 Detected via <c>INamedTypeSymbol.FindImplementationForInterfaceMember()</c>.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // In an analyzer, check if a method implements an interface
    /// public void AnalyzeMethod(SymbolAnalysisContext context)
    /// {
    ///     var method = (IMethodSymbol)context.Symbol;
    /// 
    ///     if (method.IsInterfaceImplementation())
    ///     {
    ///         // Method implements an interface - may need special handling
    ///         var interfaceMethod = method.GetImplementingInterfaceSymbol();
    ///         // Check interface method for attributes, constraints, etc.
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetImplementingInterfaceSymbol" />
    public static bool IsInterfaceImplementation(this IMethodSymbol symbol)
    {
        if (symbol.ExplicitInterfaceImplementations.Length > 0)
            return true;

        return symbol.GetImplementingInterfaceSymbol() is not null;
    }

    /// <summary>
    ///     Determines whether the specified property is an interface implementation.
    /// </summary>
    /// <param name="symbol">The property symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if the property explicitly or implicitly implements an interface property;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method checks both explicit interface implementations (where the property
    ///         is declared with the interface name prefix) and implicit implementations
    ///         (where the property matches an interface member by signature).
    ///     </para>
    /// </remarks>
    public static bool IsInterfaceImplementation(this IPropertySymbol symbol)
    {
        if (symbol.ExplicitInterfaceImplementations.Length > 0)
            return true;

        return symbol.GetImplementingInterfaceMember() is not null;
    }

    /// <summary>
    ///     Determines whether the specified event is an interface implementation.
    /// </summary>
    /// <param name="symbol">The event symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if the event explicitly or implicitly implements an interface event;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method checks both explicit interface implementations (where the event
    ///         is declared with the interface name prefix) and implicit implementations
    ///         (where the event matches an interface member by signature).
    ///     </para>
    /// </remarks>
    public static bool IsInterfaceImplementation(this IEventSymbol symbol)
    {
        if (symbol.ExplicitInterfaceImplementations.Length > 0)
            return true;

        return symbol.GetImplementingInterfaceMember() is not null;
    }

    /// <summary>
    ///     Gets the interface method that the specified method implements.
    /// </summary>
    /// <param name="symbol">The method symbol to analyze.</param>
    /// <returns>
    ///     The <see cref="IMethodSymbol" /> representing the interface method that
    ///     <paramref name="symbol" /> implements, or <c>null</c> if the method does not
    ///     implement any interface method.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         For explicit interface implementations, returns the first explicitly
    ///         implemented interface method. For implicit implementations, searches
    ///         all interfaces of the containing type to find a matching member.
    ///     </para>
    /// </remarks>
    /// <seealso cref="IsInterfaceImplementation(IMethodSymbol)" />
    public static IMethodSymbol? GetImplementingInterfaceSymbol(this IMethodSymbol symbol)
    {
        if (symbol.ExplicitInterfaceImplementations.Any())
            return symbol.ExplicitInterfaceImplementations.First();

        return (IMethodSymbol?)symbol.GetImplementingInterfaceMember();
    }

    private static ISymbol? GetImplementingInterfaceMember(this ISymbol symbol)
    {
        if (symbol.ContainingType is null)
            return null;

        foreach (var iface in symbol.ContainingType.AllInterfaces)
        foreach (var interfaceMember in iface.GetMembers())
        {
            var impl = symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
            if (SymbolEqualityComparer.Default.Equals(symbol, impl))
                return interfaceMember;
        }

        return null;
    }

    /// <summary>
    ///     Determines whether the specified method is equal to or overrides the given base method.
    /// </summary>
    /// <param name="symbol">The method symbol to check. May be <c>null</c>.</param>
    /// <param name="baseMethod">The base method to compare against. May be <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is equal to <paramref name="baseMethod" />
    ///     or if <paramref name="symbol" /> overrides <paramref name="baseMethod" /> at any level
    ///     in the inheritance chain; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method walks up the override chain from <paramref name="symbol" /> to check
    ///         if any method in the chain is equal to <paramref name="baseMethod" />. It uses
    ///         <see cref="SymbolEqualityComparer.Default" /> for symbol comparison.
    ///     </para>
    ///     <para>
    ///         Unlike <see cref="OverridesMethod" />, this method returns <c>true</c> if
    ///         <paramref name="symbol" /> is the same as <paramref name="baseMethod" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="OverridesMethod" />
    public static bool IsOrOverrideMethod(this IMethodSymbol? symbol, IMethodSymbol? baseMethod)
    {
        if (symbol is null || baseMethod is null)
            return false;

        while (symbol is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(symbol, baseMethod))
                return true;

            symbol = symbol.OverriddenMethod;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether the specified method overrides the given base method.
    /// </summary>
    /// <param name="symbol">The method symbol to check. May be <c>null</c>.</param>
    /// <param name="baseMethod">The base method to compare against. May be <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> directly or indirectly overrides
    ///     <paramref name="baseMethod" />; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method walks up the override chain from <paramref name="symbol" /> to check
    ///         if any overridden method is equal to <paramref name="baseMethod" />. It uses
    ///         <see cref="SymbolEqualityComparer.Default" /> for symbol comparison.
    ///     </para>
    ///     <para>
    ///         Unlike <see cref="IsOrOverrideMethod" />, this method returns <c>false</c> if
    ///         <paramref name="symbol" /> is the same as <paramref name="baseMethod" /> (because
    ///         a method does not override itself).
    ///     </para>
    /// </remarks>
    /// <seealso cref="IsOrOverrideMethod" />
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
