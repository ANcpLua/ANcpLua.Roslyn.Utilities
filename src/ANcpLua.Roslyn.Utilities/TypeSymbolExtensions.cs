using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="ITypeSymbol" />.
/// </summary>
/// <remarks>
///     <para>
///         This class provides extension methods for working with type symbols in Roslyn analyzers
///         and source generators. The implementation is split across partial files by responsibility:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>This file: type-hierarchy traversal and inheritance/implementation checks</description>
///         </item>
///         <item>
///             <description><c>TypeSymbolExtensions.SpecialTypes.cs</c> — primitive/numeric/enum predicates</description>
///         </item>
///         <item>
///             <description><c>TypeSymbolExtensions.Nullable.cs</c> — <see cref="Nullable{T}" /> unwrapping</description>
///         </item>
///         <item>
///             <description><c>TypeSymbolExtensions.TestClass.cs</c> — unit-test class / static candidacy</description>
///         </item>
///         <item>
///             <description><c>TypeSymbolExtensions.FrameworkTypes.cs</c> — Span/Memory/Task/Enumerable</description>
///         </item>
///         <item>
///             <description><c>TypeSymbolExtensions.Members.cs</c> — Has*Method/Property helpers</description>
///         </item>
///         <item>
///             <description><c>TypeSymbolExtensions.CodeGen.cs</c> — code-generation formatting</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="SymbolExtensions" />
/// <seealso cref="MethodSymbolExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class TypeSymbolExtensions
{
    /// <summary>
    ///     Gets all interfaces implemented by the type, including the type itself if it is an interface.
    /// </summary>
    /// <param name="type">The type symbol to get interfaces from.</param>
    /// <returns>
    ///     An enumerable of all interfaces. If <paramref name="type" /> is an interface,
    ///     it is included in the result along with all its inherited interfaces.
    /// </returns>
    /// <remarks>
    ///     Unlike <see cref="ITypeSymbol.AllInterfaces" />, which only returns inherited interfaces,
    ///     this method includes the type itself when querying an interface type.
    /// </remarks>
    /// <seealso cref="Implements" />
    /// <seealso cref="IsOrImplements" />
    public static IEnumerable<INamedTypeSymbol> GetAllInterfacesIncludingThis(this ITypeSymbol type)
    {
        var allInterfaces = type.AllInterfaces;
        if (type is INamedTypeSymbol { TypeKind: TypeKind.Interface } namedType && !allInterfaces.Contains(namedType))
        {
            var result = new List<INamedTypeSymbol>(allInterfaces.Length + 1);
            result.AddRange(allInterfaces);
            result.Add(namedType);
            return result;
        }

        return allInterfaces;
    }

    /// <summary>
    ///     Determines whether a type inherits from a specified base type.
    /// </summary>
    /// <param name="classSymbol">The type symbol to check.</param>
    /// <param name="baseClassType">The potential base type to check against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="classSymbol" /> inherits from <paramref name="baseClassType" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Walks the inheritance chain using <see cref="SymbolEqualityComparer.Default" /> for comparison.
    ///         Does not consider the type itself as inheriting from itself; use <see cref="IsOrInheritsFrom" />
    ///         for an inclusive check.
    ///     </para>
    /// </remarks>
    /// <seealso cref="IsOrInheritsFrom" />
    public static bool InheritsFrom(this ITypeSymbol classSymbol, ITypeSymbol? baseClassType)
    {
        if (baseClassType is null)
            return false;

        var baseType = classSymbol.BaseType;
        while (baseType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseClassType, baseType))
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether a type implements a specified interface.
    /// </summary>
    /// <param name="classSymbol">The type symbol to check.</param>
    /// <param name="interfaceType">The interface type to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="classSymbol" /> implements <paramref name="interfaceType" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This method uses <see cref="SymbolEqualityComparer.Default" /> for comparison
    ///     and checks against <see cref="ITypeSymbol.AllInterfaces" />.
    /// </remarks>
    /// <seealso cref="IsOrImplements" />
    /// <seealso cref="GetAllInterfacesIncludingThis" />
    public static bool Implements(this ITypeSymbol classSymbol, ITypeSymbol? interfaceType)
    {
        if (interfaceType is null)
            return false;

        foreach (var iface in classSymbol.AllInterfaces)
            if (SymbolEqualityComparer.Default.Equals(interfaceType, iface))
                return true;

        return false;
    }

    internal static bool TypeNameMatches(this ITypeSymbol? symbol, string name)
    {
        if (symbol is null)
            return false;

        var normalizedName = name.StripGlobalPrefix();
        return symbol.Name == normalizedName ||
               symbol.ToDisplayString() == normalizedName ||
               symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StripGlobalPrefix() == normalizedName;
    }

    internal static bool InheritsFromName(this ITypeSymbol type, string name)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.TypeNameMatches(name))
                return true;
            current = current.BaseType;
        }

        return false;
    }

    internal static bool ImplementsInterfaceName(this ITypeSymbol type, string name)
    {
        foreach (var iface in type.AllInterfaces)
            if (iface.TypeNameMatches(name))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether a type is or implements a specified interface.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="interfaceType">The interface type to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is or implements <paramref name="interfaceType" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Unlike <see cref="Implements" />, this method returns <c>true</c> if the symbol itself
    ///     is the interface being checked.
    /// </remarks>
    /// <seealso cref="Implements" />
    /// <seealso cref="GetAllInterfacesIncludingThis" />
    public static bool IsOrImplements(this ITypeSymbol symbol, ITypeSymbol? interfaceType)
    {
        if (interfaceType is null)
            return false;

        foreach (var iface in symbol.GetAllInterfacesIncludingThis())
            if (SymbolEqualityComparer.Default.Equals(interfaceType, iface))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether a type is or inherits from a specified type.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="expectedType">The expected type or base type to check against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> equals or inherits from <paramref name="expectedType" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This method first checks for equality, then checks inheritance if the expected type is not sealed.
    /// </remarks>
    /// <seealso cref="InheritsFrom" />
    public static bool IsOrInheritsFrom(this ITypeSymbol symbol, ITypeSymbol? expectedType)
    {
        if (expectedType is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(symbol, expectedType) ||
               (!expectedType.IsSealed && symbol.InheritsFrom(expectedType));
    }
}
