using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for Roslyn symbol types.
/// </summary>
public static class SymbolExtensions
{
    /// <summary>
    ///     Gets the fully qualified name of a type symbol (global::Namespace.Type format).
    /// </summary>
    public static string GetFullyQualifiedName(this ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    ///     Gets the metadata name of a type symbol (Namespace.Type format).
    /// </summary>
    public static string GetMetadataName(this ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    }

    /// <summary>
    ///     Checks if a type symbol has a specific attribute.
    /// </summary>
    public static bool HasAttribute(this ISymbol symbol, string fullyQualifiedAttributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName)
                return true;

        return false;
    }

    /// <summary>
    ///     Gets the first attribute matching the specified name, or null.
    /// </summary>
    public static AttributeData? GetAttribute(this ISymbol symbol, string fullyQualifiedAttributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName)
                return attribute;

        return null;
    }

    /// <summary>
    ///     Checks if a type is or inherits from a specific base type.
    /// </summary>
    public static bool IsOrInheritsFrom(this ITypeSymbol? symbol, string fullyQualifiedTypeName)
    {
        while (symbol is not null)
        {
            if (symbol.ToDisplayString() == fullyQualifiedTypeName) return true;

            symbol = symbol.BaseType;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a type implements a specific interface.
    /// </summary>
    public static bool ImplementsInterface(this ITypeSymbol symbol, string fullyQualifiedInterfaceName)
    {
        foreach (var iface in symbol.AllInterfaces)
            if (iface.ToDisplayString() == fullyQualifiedInterfaceName)
                return true;

        return false;
    }

    /// <summary>
    ///     Gets the containing namespace as a string, or empty if global.
    /// </summary>
    public static string GetNamespaceName(this ISymbol symbol)
    {
        var ns = symbol.ContainingNamespace;
        return ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();
    }

    /// <summary>
    ///     Gets a single method by name from a type, or null if not found or multiple exist.
    /// </summary>
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
    ///     Gets a single property by name from a type, or null if not found.
    /// </summary>
    public static IPropertySymbol? GetProperty(this INamedTypeSymbol type, string name)
    {
        foreach (var member in type.GetMembers(name))
            if (member is IPropertySymbol property)
                return property;

        return null;
    }

    /// <summary>
    ///     Checks if a method explicitly implements a specific interface method.
    /// </summary>
    public static bool ExplicitlyImplements(this IMethodSymbol method, IMethodSymbol interfaceMethod)
    {
        foreach (var impl in method.ExplicitInterfaceImplementations)
            if (SymbolEqualityComparer.Default.Equals(impl, interfaceMethod))
                return true;

        return false;
    }

    /// <summary>
    ///     Checks if the type's original definition equals the specified type.
    ///     Useful for comparing generic types like Span&lt;T&gt;.
    /// </summary>
    public static bool IsDefinition(this INamedTypeSymbol type, INamedTypeSymbol definition)
    {
        var original = type.OriginalDefinition ?? type;
        return SymbolEqualityComparer.Default.Equals(original, definition);
    }
}