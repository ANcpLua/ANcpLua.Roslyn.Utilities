using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for Roslyn symbol types.
/// </summary>
/// <remarks>
///     <para>
///         Utility methods for working with <see cref="ISymbol" /> and its derived types. The
///         implementation is split across partial files by responsibility:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>This file: equality, naming, accessibility predicates, visibility,
///             <see cref="IsOperator" />, <see cref="IsConst" /></description>
///         </item>
///         <item>
///             <description><c>SymbolExtensions.Attributes.cs</c> — attribute lookup by FQN / type /
///             short name and <see cref="GetAttributeTypeArguments" /></description>
///         </item>
///         <item>
///             <description><c>SymbolExtensions.Members.cs</c> — <see cref="GetAllMembers(ITypeSymbol?)" />,
///             <see cref="GetMethod" />, <see cref="GetProperty" /></description>
///         </item>
///         <item>
///             <description><c>SymbolExtensions.Interfaces.cs</c> — explicit/implicit interface
///             implementations</description>
///         </item>
///         <item>
///             <description><c>SymbolExtensions.TypeParameters.cs</c> — type parameter / type argument
///             retrieval (incl. containing-type walks)</description>
///         </item>
///         <item>
///             <description><c>SymbolExtensions.Misc.cs</c> — top-level statement detection, symbol-kind
///             dispatchers (<see cref="GetSymbolType" />, <see cref="GetOverriddenMember" />),
///             <see cref="GetNamespaceName" /></description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="ISymbol" />
/// <seealso cref="ITypeSymbol" />
/// <seealso cref="IMethodSymbol" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class SymbolExtensions
{
    /// <summary>
    ///     Compares two symbols for equality using <see cref="SymbolEqualityComparer.Default" />.
    /// </summary>
    /// <param name="symbol">The symbol to compare.</param>
    /// <param name="expectedType">The symbol to compare against.</param>
    /// <returns>
    ///     <c>true</c> if both symbols are non-null and equal according to <see cref="SymbolEqualityComparer.Default" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="SymbolEqualityComparer.Default" />
    public static bool IsEqualTo(this ISymbol? symbol, [NotNullWhen(true)] ISymbol? expectedType)
    {
        return symbol is not null
               && expectedType is not null
               && SymbolEqualityComparer.Default.Equals(expectedType, symbol);
    }

    /// <summary>
    ///     Gets the fully qualified name of a type symbol in the <c>global::Namespace.Type</c> format.
    /// </summary>
    /// <param name="symbol">The type symbol to get the name for.</param>
    /// <returns>The fully qualified name including the <c>global::</c> prefix.</returns>
    /// <seealso cref="GetMetadataName" />
    /// <seealso cref="SymbolDisplayFormat.FullyQualifiedFormat" />
    public static string GetFullyQualifiedName(this ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    ///     Gets the metadata name of a type symbol in the <c>Namespace.Type</c> format.
    /// </summary>
    /// <param name="symbol">The type symbol to get the name for.</param>
    /// <returns>The metadata name without the <c>global::</c> prefix.</returns>
    /// <seealso cref="GetFullyQualifiedName" />
    /// <seealso cref="SymbolDisplayFormat.CSharpErrorMessageFormat" />
    public static string GetMetadataName(this ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    }

    /// <summary>
    ///     Gets the reflection-style full name of a symbol, suitable for <c>Type.GetType()</c>.
    /// </summary>
    /// <param name="symbol">The symbol to get the reflection name for.</param>
    /// <returns>
    ///     A string in the format <c>Namespace.Outer+Inner</c> using <see cref="ISymbol.MetadataName" />
    ///     and <c>+</c> as the nested type separator.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This produces the same format as <see cref="Type.FullName" /> at runtime, using
    ///         <c>MetadataName</c> (which includes generic arity suffixes like <c>`1</c>) and <c>+</c> for
    ///         nested types. Useful when generated code needs to reference types via reflection or
    ///         <c>Type.GetType()</c>.
    ///     </para>
    /// </remarks>
    public static string GetReflectionFullName(this ISymbol symbol)
    {
        var typeNames = new Stack<string>();
        for (var current = symbol; current is ITypeSymbol; current = current.ContainingType)
            typeNames.Push(current.MetadataName);

        var typeName = string.Join("+", typeNames);
        var ns = symbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : symbol.ContainingNamespace?.ToDisplayString();

        return string.IsNullOrEmpty(ns) ? typeName : $"{ns}.{typeName}";
    }

    /// <summary>Checks if the symbol has public accessibility.</summary>
    public static bool IsPublic(this ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Public;

    /// <summary>Checks if the symbol has internal accessibility.</summary>
    public static bool IsInternal(this ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Internal;

    /// <summary>Checks if the symbol has private accessibility.</summary>
    public static bool IsPrivate(this ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Private;

    /// <summary>Checks if the symbol has protected accessibility.</summary>
    public static bool IsProtected(this ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Protected;

    // Accessibilities considered visible to consumers outside the declaring assembly.
    // ProtectedOrInternal ("protected internal") is visible; Internal-only / ProtectedAndInternal are not.
    private static readonly HashSet<Accessibility> s_externallyVisibleAccessibilities =
    [
        Accessibility.Public,
        Accessibility.Protected,
        Accessibility.ProtectedOrInternal
    ];

    /// <summary>
    ///     Checks if a symbol is visible outside its containing assembly.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if the symbol and all its containing types have public, protected,
    ///     or protected-internal accessibility; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>Recursively walks the containing-type hierarchy. A nested type is only externally
    ///     visible when every enclosing type is also externally visible.</para>
    /// </remarks>
    public static bool IsVisibleOutsideOfAssembly([NotNullWhen(true)] this ISymbol? symbol)
    {
        if (symbol is null)
            return false;

        if (!s_externallyVisibleAccessibilities.Contains(symbol.DeclaredAccessibility))
            return false;

        return symbol.ContainingType is null || symbol.ContainingType.IsVisibleOutsideOfAssembly();
    }

    /// <summary>
    ///     Checks if a symbol is a user-defined operator or conversion method.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if the symbol is an <see cref="IMethodSymbol" /> with
    ///     <see cref="MethodKind.UserDefinedOperator" /> or <see cref="MethodKind.Conversion" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool IsOperator(this ISymbol? symbol)
    {
        return symbol is IMethodSymbol
        {
            MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion
        };
    }

    /// <summary>
    ///     Checks if a symbol is a const field.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if the symbol is an <see cref="IFieldSymbol" /> with <see cref="IFieldSymbol.IsConst" />
    ///     set to <c>true</c>; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsConst(this ISymbol? symbol)
    {
        return symbol is IFieldSymbol { IsConst: true };
    }
}
