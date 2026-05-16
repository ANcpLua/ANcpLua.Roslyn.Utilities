using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class TypeSymbolExtensions
{
    // Cached numeric-special-types lookup. The data-driven set replaces a long
    // switch expression: each predicate stays a one-line delegation (CC=1)
    // while the membership check stays branch-free (CC=2 incl. null guard).
    private static readonly HashSet<SpecialType> s_numericSpecialTypes =
    [
        SpecialType.System_SByte,
        SpecialType.System_Byte,
        SpecialType.System_Int16,
        SpecialType.System_UInt16,
        SpecialType.System_Int32,
        SpecialType.System_UInt32,
        SpecialType.System_Int64,
        SpecialType.System_UInt64,
        SpecialType.System_Single,
        SpecialType.System_Double,
        SpecialType.System_Decimal
    ];

    /// <summary>
    ///     Determines whether the type symbol's <see cref="ITypeSymbol.SpecialType" /> equals the specified value.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <param name="specialType">The <see cref="SpecialType" /> to compare against.</param>
    /// <returns><c>true</c> if non-null and the special type matches; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     Internal dispatch that powers the per-primitive predicates (<see cref="IsString" />, etc.).
    ///     Centralising the null guard keeps each predicate at cyclomatic complexity 1.
    /// </remarks>
    private static bool IsSpecialType(this ITypeSymbol? symbol, SpecialType specialType)
    {
        return symbol is not null && symbol.SpecialType == specialType;
    }

    /// <summary>Determines whether the type symbol represents <see cref="object" />.</summary>
    public static bool IsObject(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_Object);

    /// <summary>Determines whether the type symbol represents <see cref="string" />.</summary>
    public static bool IsString(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_String);

    /// <summary>Determines whether the type symbol represents <see cref="char" />.</summary>
    public static bool IsChar(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_Char);

    /// <summary>Determines whether the type symbol represents <see cref="int" />.</summary>
    public static bool IsInt32(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_Int32);

    /// <summary>Determines whether the type symbol represents <see cref="long" />.</summary>
    public static bool IsInt64(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_Int64);

    /// <summary>Determines whether the type symbol represents <see cref="bool" />.</summary>
    public static bool IsBoolean(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_Boolean);

    /// <summary>Determines whether the type symbol represents <see cref="DateTime" />.</summary>
    public static bool IsDateTime(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_DateTime);

    /// <summary>Determines whether the type symbol represents <see cref="byte" />.</summary>
    public static bool IsByte(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_Byte);

    /// <summary>Determines whether the type symbol represents <see cref="sbyte" />.</summary>
    public static bool IsSByte(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_SByte);

    /// <summary>Determines whether the type symbol represents <see cref="short" />.</summary>
    public static bool IsInt16(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_Int16);

    /// <summary>Determines whether the type symbol represents <see cref="ushort" />.</summary>
    public static bool IsUInt16(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_UInt16);

    /// <summary>Determines whether the type symbol represents <see cref="uint" />.</summary>
    public static bool IsUInt32(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_UInt32);

    /// <summary>Determines whether the type symbol represents <see cref="ulong" />.</summary>
    public static bool IsUInt64(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_UInt64);

    /// <summary>Determines whether the type symbol represents <see cref="float" />.</summary>
    public static bool IsSingle(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_Single);

    /// <summary>Determines whether the type symbol represents <see cref="double" />.</summary>
    public static bool IsDouble(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_Double);

    /// <summary>Determines whether the type symbol represents <see cref="decimal" />.</summary>
    public static bool IsDecimal(this ITypeSymbol? symbol) => symbol.IsSpecialType(SpecialType.System_Decimal);

    /// <summary>
    ///     Determines whether the type symbol represents an enumeration type.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is an enumeration type; otherwise, <c>false</c>.</returns>
    /// <seealso cref="GetEnumerationType" />
    public static bool IsEnumeration([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        return symbol?.GetEnumerationType() is not null;
    }

    /// <summary>
    ///     Gets the underlying type of an enumeration.
    /// </summary>
    /// <param name="symbol">The type symbol to get the underlying type from, or <c>null</c>.</param>
    /// <returns>
    ///     The underlying type of the enumeration (e.g., <see cref="int" />), or <c>null</c>
    ///     if <paramref name="symbol" /> is not an enumeration.
    /// </returns>
    /// <seealso cref="IsEnumeration" />
    public static INamedTypeSymbol? GetEnumerationType(this ITypeSymbol? symbol)
    {
        return (symbol as INamedTypeSymbol)?.EnumUnderlyingType;
    }

    /// <summary>
    ///     Determines whether the type symbol represents a numeric type.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is a numeric type; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>Numeric types include all signed/unsigned integers plus <see cref="float" />, <see cref="double" />, and
    ///     <see cref="decimal" />. Implemented as a single hash-set lookup so cyclomatic complexity is independent of how
    ///     many primitives are considered "numeric".</para>
    /// </remarks>
    public static bool IsNumberType(this ITypeSymbol? symbol)
    {
        return symbol is not null && s_numericSpecialTypes.Contains(symbol.SpecialType);
    }
}
