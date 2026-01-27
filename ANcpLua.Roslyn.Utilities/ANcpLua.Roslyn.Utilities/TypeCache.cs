using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     A high-performance cache for well-known type symbols, indexed by an enum.
/// </summary>
/// <typeparam name="TEnum">An enum type whose values represent well-known types.</typeparam>
/// <remarks>
///     <para>
///         This cache provides efficient O(1) lookups for frequently accessed type symbols
///         in Roslyn analyzers. Types are lazily loaded on first access using the provided
///         resolver function, and cached for subsequent lookups.
///     </para>
///     <para>
///         The cache is designed for use within a single compilation context.
///         Create a new instance for each <see cref="Compilation" />.
///     </para>
///     <list type="bullet">
///         <item>
///             <description><see cref="Get" />: Retrieves a cached type symbol by enum value</description>
///         </item>
///         <item>
///             <description><see cref="IsType" />: Checks if a symbol matches a cached type</description>
///         </item>
///         <item>
///             <description><see cref="HasAttribute" />: Checks if a symbol has an attribute of a cached type</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// // Define your well-known types enum
/// public enum WellKnownType
/// {
///     SystemString,
///     SystemInt32,
///     ObsoleteAttribute,
///     // ... more types
/// }
///
/// // Create the resolver function
/// Func&lt;WellKnownType, INamedTypeSymbol?&gt; resolver = type => type switch
/// {
///     WellKnownType.SystemString => compilation.GetSpecialType(SpecialType.System_String),
///     WellKnownType.SystemInt32 => compilation.GetSpecialType(SpecialType.System_Int32),
///     WellKnownType.ObsoleteAttribute => compilation.GetTypeByMetadataName("System.ObsoleteAttribute"),
///     _ => null
/// };
///
/// // Create and use the cache
/// var cache = new TypeCache&lt;WellKnownType&gt;(resolver);
/// if (cache.HasAttribute(symbol, WellKnownType.ObsoleteAttribute))
/// {
///     // Symbol is marked obsolete
/// }
/// </code>
/// </example>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class TypeCache<TEnum> where TEnum : struct, Enum
{
    private readonly INamedTypeSymbol?[] _cache;
    private readonly bool[] _resolved;
    private readonly Func<TEnum, INamedTypeSymbol?> _resolver;

    /// <summary>
    ///     Creates a new type cache with the specified resolver function.
    /// </summary>
    /// <param name="resolver">
    ///     A function that resolves an enum value to its corresponding type symbol.
    ///     This function is called lazily on first access for each type.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resolver" /> is null.</exception>
    public TypeCache(Func<TEnum, INamedTypeSymbol?> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        var enumValues = Enum.GetValues(typeof(TEnum));
        var maxValue = 0;
        foreach (TEnum value in enumValues)
        {
            var intValue = Convert.ToInt32(value);
            if (intValue > maxValue)
                maxValue = intValue;
        }

        _cache = new INamedTypeSymbol?[maxValue + 1];
        _resolved = new bool[maxValue + 1];
    }

    /// <summary>
    ///     Gets the type symbol for the specified enum value.
    /// </summary>
    /// <param name="type">The enum value identifying the type to retrieve.</param>
    /// <returns>
    ///     The <see cref="INamedTypeSymbol" /> for the specified type,
    ///     or <c>null</c> if the type could not be resolved.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The type is resolved lazily on first access and cached for subsequent lookups.
    ///         Thread-safe for concurrent reads after initial resolution.
    ///     </para>
    /// </remarks>
    public INamedTypeSymbol? Get(TEnum type)
    {
        var index = Convert.ToInt32(type);
        if (index < 0 || index >= _cache.Length)
            return null;

        if (!_resolved[index])
        {
            _cache[index] = _resolver(type);
            _resolved[index] = true;
        }

        return _cache[index];
    }

    /// <summary>
    ///     Checks if a type symbol matches the specified well-known type.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="type">The well-known type to compare against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is equal to the cached type;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Uses <see cref="SymbolEqualityComparer.Default" /> for comparison.
    ///     </para>
    /// </remarks>
    public bool IsType(ITypeSymbol? symbol, TEnum type)
    {
        if (symbol is null)
            return false;

        var cachedType = Get(type);
        return cachedType is not null && SymbolEqualityComparer.Default.Equals(symbol, cachedType);
    }

    /// <summary>
    ///     Checks if a type symbol's original definition matches the specified well-known type.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="type">The well-known type to compare against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" />'s <see cref="ISymbol.OriginalDefinition" />
    ///     is equal to the cached type; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This is useful for checking generic types regardless of their type arguments.
    ///         For example, checking if a <c>List&lt;int&gt;</c> is a <c>List&lt;T&gt;</c>.
    ///     </para>
    /// </remarks>
    public bool IsTypeDefinition(ITypeSymbol? symbol, TEnum type)
    {
        if (symbol is null)
            return false;

        var cachedType = Get(type);
        return cachedType is not null && SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, cachedType.OriginalDefinition);
    }

    /// <summary>
    ///     Checks if a symbol has an attribute of the specified well-known type.
    /// </summary>
    /// <param name="symbol">The symbol to check for the attribute.</param>
    /// <param name="attributeType">The well-known attribute type to look for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> has an attribute of the specified type;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Uses <see cref="SymbolEqualityComparer.Default" /> to compare the attribute class's
    ///         original definition with the cached type's original definition, supporting generic attributes.
    ///     </para>
    /// </remarks>
    public bool HasAttribute(ISymbol symbol, TEnum attributeType)
    {
        var attrSymbol = Get(attributeType);
        if (attrSymbol is null)
            return false;

        foreach (var attr in symbol.GetAttributes())
            if (attr.AttributeClass is not null &&
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass.OriginalDefinition, attrSymbol.OriginalDefinition))
                return true;

        return false;
    }

    /// <summary>
    ///     Gets the first attribute of the specified well-known type from a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get the attribute from.</param>
    /// <param name="attributeType">The well-known attribute type to look for.</param>
    /// <returns>
    ///     The <see cref="AttributeData" /> for the first matching attribute,
    ///     or <c>null</c> if no attribute of the specified type is found.
    /// </returns>
    public AttributeData? GetAttribute(ISymbol symbol, TEnum attributeType)
    {
        var attrSymbol = Get(attributeType);
        if (attrSymbol is null)
            return null;

        foreach (var attr in symbol.GetAttributes())
            if (attr.AttributeClass is not null &&
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass.OriginalDefinition, attrSymbol.OriginalDefinition))
                return attr;

        return null;
    }

    /// <summary>
    ///     Gets all attributes of the specified well-known type from a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get the attributes from.</param>
    /// <param name="attributeType">The well-known attribute type to look for.</param>
    /// <returns>
    ///     An enumerable of <see cref="AttributeData" /> for all matching attributes.
    /// </returns>
    public IEnumerable<AttributeData> GetAttributes(ISymbol symbol, TEnum attributeType)
    {
        var attrSymbol = Get(attributeType);
        if (attrSymbol is null)
            yield break;

        foreach (var attr in symbol.GetAttributes())
            if (attr.AttributeClass is not null &&
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass.OriginalDefinition, attrSymbol.OriginalDefinition))
                yield return attr;
    }

    /// <summary>
    ///     Checks if a type symbol implements or inherits from the specified well-known type.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="baseOrInterfaceType">The well-known base type or interface to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> implements or inherits from the specified type;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public bool ImplementsOrInheritsFrom(ITypeSymbol? symbol, TEnum baseOrInterfaceType)
    {
        if (symbol is null)
            return false;

        var targetType = Get(baseOrInterfaceType);
        if (targetType is null)
            return false;

        // Check interfaces
        foreach (var iface in symbol.AllInterfaces)
            if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, targetType.OriginalDefinition))
                return true;

        // Check base types
        var current = symbol.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, targetType.OriginalDefinition))
                return true;
            current = current.BaseType;
        }

        return false;
    }
}
