using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class SymbolExtensions
{
    private const string AttributeSuffix = "Attribute";

    // ========== Fully-Qualified-Name attribute matching ==========

    /// <summary>
    ///     Yields every attribute on <paramref name="symbol" /> whose attribute class display string equals
    ///     <paramref name="fullyQualifiedAttributeName" />.
    /// </summary>
    /// <remarks>
    ///     Single iteration chokepoint behind <see cref="HasAttribute(ISymbol, string)" /> and
    ///     <see cref="GetAttribute(ISymbol, string)" /> so each public method becomes a one-liner (CC=1).
    /// </remarks>
    private static IEnumerable<AttributeData> EnumerateAttributesByFullName(
        ISymbol symbol,
        string fullyQualifiedAttributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName)
                yield return attribute;
    }

    /// <summary>
    ///     Checks if a symbol has a specific attribute identified by its fully qualified name.
    /// </summary>
    /// <param name="symbol">The symbol to check for the attribute.</param>
    /// <param name="fullyQualifiedAttributeName">
    ///     The fully qualified name of the attribute type (e.g., <c>"System.ObsoleteAttribute"</c>).
    /// </param>
    /// <returns><c>true</c> if the symbol has the specified attribute; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         The fully qualified name must match the format returned by
    ///         <c>ISymbol.ToDisplayString()</c> using the default format, which is
    ///         <c>Namespace.TypeName</c> without the <c>global::</c> prefix.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (methodSymbol.HasAttribute("System.ObsoleteAttribute"))
    /// {
    ///     // Method is marked obsolete
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetAttribute(ISymbol, string)" />
    /// <seealso cref="HasAttribute(ISymbol, ITypeSymbol?, bool)" />
    public static bool HasAttribute(this ISymbol symbol, string fullyQualifiedAttributeName)
    {
        return EnumerateAttributesByFullName(symbol, fullyQualifiedAttributeName).Any();
    }

    /// <summary>
    ///     Gets the first attribute matching the specified fully qualified name.
    /// </summary>
    /// <param name="symbol">The symbol to search for the attribute.</param>
    /// <param name="fullyQualifiedAttributeName">
    ///     The fully qualified name of the attribute type.
    /// </param>
    /// <returns>
    ///     The <see cref="AttributeData" /> for the first matching attribute, or <c>null</c> if not found.
    /// </returns>
    /// <seealso cref="HasAttribute(ISymbol, string)" />
    /// <seealso cref="GetAttribute(ISymbol, ITypeSymbol?, bool)" />
    public static AttributeData? GetAttribute(this ISymbol symbol, string fullyQualifiedAttributeName)
    {
        return EnumerateAttributesByFullName(symbol, fullyQualifiedAttributeName).FirstOrDefault();
    }

    // ========== Attribute-type matching (with optional inheritance) ==========

    /// <summary>
    ///     Gets all attributes of a specific type, optionally including inherited attribute types.
    /// </summary>
    /// <param name="symbol">The symbol to search for attributes.</param>
    /// <param name="attributeType">The type symbol representing the attribute type to find.</param>
    /// <param name="inherits">
    ///     <c>true</c> to include attributes that inherit from <paramref name="attributeType" />;
    ///     <c>false</c> to match only exact types. Defaults to <c>true</c>.
    ///     This parameter is ignored if <paramref name="attributeType" /> is sealed.
    /// </param>
    /// <returns>
    ///     An enumerable of <see cref="AttributeData" /> for all matching attributes.
    ///     Returns an empty enumerable if <paramref name="attributeType" /> is <c>null</c>.
    /// </returns>
    /// <seealso cref="GetAttribute(ISymbol, ITypeSymbol?, bool)" />
    /// <seealso cref="HasAttribute(ISymbol, ITypeSymbol?, bool)" />
    public static IEnumerable<AttributeData> GetAttributes(
        this ISymbol symbol,
        ITypeSymbol? attributeType,
        bool inherits = true)
    {
        if (attributeType is null)
            return [];

        // Sealed types can't be inherited from — collapse to exact-match path either way.
        var effectiveInherits = inherits && !attributeType.IsSealed;
        return FilterAttributesByType(symbol, attributeType, effectiveInherits);
    }

    private static IEnumerable<AttributeData> FilterAttributesByType(
        ISymbol symbol,
        ITypeSymbol attributeType,
        bool inherits)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var attrClass = attribute.AttributeClass;
            if (attrClass is null)
                continue;

            if (inherits
                    ? attrClass.IsOrInheritsFrom(attributeType)
                    : SymbolEqualityComparer.Default.Equals(attributeType, attrClass))
                yield return attribute;
        }
    }

    /// <summary>
    ///     Gets the first attribute of a specific type, optionally including inherited attribute types.
    /// </summary>
    /// <param name="symbol">The symbol to search for the attribute.</param>
    /// <param name="attributeType">The type symbol representing the attribute type to find.</param>
    /// <param name="inherits">
    ///     <c>true</c> to include attributes that inherit from <paramref name="attributeType" />.
    /// </param>
    /// <returns>
    ///     The <see cref="AttributeData" /> for the first matching attribute, or <c>null</c> if not found.
    /// </returns>
    /// <seealso cref="GetAttributes(ISymbol, ITypeSymbol?, bool)" />
    /// <seealso cref="HasAttribute(ISymbol, ITypeSymbol?, bool)" />
    public static AttributeData? GetAttribute(this ISymbol symbol, ITypeSymbol? attributeType, bool inherits = true)
    {
        return symbol.GetAttributes(attributeType, inherits).FirstOrDefault();
    }

    /// <summary>
    ///     Checks if a symbol has an attribute of a specific type.
    /// </summary>
    /// <param name="symbol">The symbol to check for the attribute.</param>
    /// <param name="attributeType">The type symbol representing the attribute type to find.</param>
    /// <param name="inherits">
    ///     <c>true</c> to include attributes that inherit from <paramref name="attributeType" />.
    /// </param>
    /// <returns><c>true</c> if the symbol has the specified attribute; otherwise, <c>false</c>.</returns>
    /// <seealso cref="HasAttribute(ISymbol, string)" />
    /// <seealso cref="GetAttribute(ISymbol, ITypeSymbol?, bool)" />
    public static bool HasAttribute(this ISymbol symbol, ITypeSymbol? attributeType, bool inherits = true)
    {
        return symbol.GetAttributes(attributeType, inherits).Any();
    }

    // ========== Short-name attribute matching ==========

    /// <summary>
    ///     Yields every attribute on <paramref name="symbol" /> whose class short name matches
    ///     <paramref name="attributeShortName" />, with or without the <c>Attribute</c> suffix.
    /// </summary>
    /// <remarks>
    ///     Single chokepoint behind the three public ByShortName methods — keeps each at CC=1.
    /// </remarks>
    private static IEnumerable<AttributeData> EnumerateAttributesByShortName(
        ISymbol symbol,
        string attributeShortName)
    {
        var (withoutSuffix, withSuffix) = NormalizeAttributeShortName(attributeShortName);

        foreach (var attribute in symbol.GetAttributes())
        {
            var className = attribute.AttributeClass?.Name;
            if (className is null)
                continue;

            if (string.Equals(className, withoutSuffix, StringComparison.Ordinal) ||
                string.Equals(className, withSuffix, StringComparison.Ordinal))
                yield return attribute;
        }
    }

    private static (string WithoutSuffix, string WithSuffix) NormalizeAttributeShortName(string name)
    {
        var withoutSuffix = name.EndsWith(AttributeSuffix, StringComparison.Ordinal)
            ? name[..^AttributeSuffix.Length]
            : name;
        return (withoutSuffix, withoutSuffix + AttributeSuffix);
    }

    /// <summary>
    ///     Checks if a symbol has an attribute by its short name (with or without "Attribute" suffix).
    /// </summary>
    /// <param name="symbol">The symbol to check for the attribute.</param>
    /// <param name="attributeShortName">
    ///     The short name of the attribute (e.g., "Obsolete" or "ObsoleteAttribute").
    /// </param>
    /// <returns>
    ///     <c>true</c> if the symbol has an attribute matching the short name; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>Matches by class short name (no namespace) and automatically handles the
    ///     <c>Attribute</c> suffix convention: <c>"Obsolete"</c> and <c>"ObsoleteAttribute"</c> match the
    ///     same attribute.</para>
    /// </remarks>
    /// <seealso cref="HasAttribute(ISymbol, string)" />
    /// <seealso cref="GetAttributeByShortName" />
    public static bool HasAttributeByShortName(this ISymbol symbol, string attributeShortName)
    {
        return EnumerateAttributesByShortName(symbol, attributeShortName).Any();
    }

    /// <summary>
    ///     Gets the first attribute matching the short name (with or without "Attribute" suffix).
    /// </summary>
    /// <param name="symbol">The symbol to get the attribute from.</param>
    /// <param name="attributeShortName">
    ///     The short name of the attribute (e.g., "Obsolete" or "ObsoleteAttribute").
    /// </param>
    /// <returns>
    ///     The <see cref="AttributeData" /> for the first matching attribute, or <c>null</c> if no
    ///     attribute matches.
    /// </returns>
    /// <seealso cref="HasAttributeByShortName" />
    /// <seealso cref="GetAttribute(ISymbol, string)" />
    public static AttributeData? GetAttributeByShortName(this ISymbol symbol, string attributeShortName)
    {
        return EnumerateAttributesByShortName(symbol, attributeShortName).FirstOrDefault();
    }

    /// <summary>
    ///     Gets all attributes matching the short name (with or without "Attribute" suffix).
    /// </summary>
    /// <param name="symbol">The symbol to get the attributes from.</param>
    /// <param name="attributeShortName">
    ///     The short name of the attribute (e.g., "Obsolete" or "ObsoleteAttribute").
    /// </param>
    /// <returns>
    ///     An enumerable of <see cref="AttributeData" /> for all matching attributes.
    /// </returns>
    /// <seealso cref="HasAttributeByShortName" />
    /// <seealso cref="GetAttributeByShortName" />
    public static IEnumerable<AttributeData> GetAttributesByShortName(this ISymbol symbol, string attributeShortName)
    {
        return EnumerateAttributesByShortName(symbol, attributeShortName);
    }

    // ========== Attribute type-argument extraction ==========

    /// <summary>
    ///     Extracts fully-qualified type names from <c>typeof()</c> constructor arguments of all
    ///     attributes matching the specified name.
    /// </summary>
    /// <param name="symbol">The symbol to extract attribute type arguments from.</param>
    /// <param name="fullyQualifiedAttributeName">
    ///     The fully qualified name of the attribute type.
    /// </param>
    /// <returns>
    ///     An <see cref="EquatableArray{T}" /> of fully-qualified type name strings, sorted by ordinal
    ///     comparison for deterministic incremental-generator caching.
    /// </returns>
    /// <seealso cref="GetAttribute(ISymbol, string)" />
    /// <seealso cref="HasAttribute(ISymbol, string)" />
    public static EquatableArray<string> GetAttributeTypeArguments(
        this ISymbol symbol,
        string fullyQualifiedAttributeName)
    {
        var builder = ImmutableArray.CreateBuilder<string>();

        foreach (var attr in EnumerateAttributesByFullName(symbol, fullyQualifiedAttributeName))
            if (TryGetFirstTypeofArgument(attr, out var typeSymbol))
                builder.Add(typeSymbol.GetFullyQualifiedName());

        if (builder.Count == 0)
            return default;

        builder.Sort(StringComparer.Ordinal);
        return new EquatableArray<string>(builder.ToImmutable());
    }

    private static bool TryGetFirstTypeofArgument(AttributeData attr, [NotNullWhen(true)] out INamedTypeSymbol? typeSymbol)
    {
        if (attr.ConstructorArguments.Length > 0
            && attr.ConstructorArguments[0].Value is INamedTypeSymbol named)
        {
            typeSymbol = named;
            return true;
        }

        typeSymbol = null;
        return false;
    }
}
