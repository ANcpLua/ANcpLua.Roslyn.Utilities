using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class TypeSymbolExtensions
{
    /// <summary>
    ///     Gets the underlying type of a <see cref="Nullable{T}" /> or returns the type itself.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to unwrap, or <c>null</c>.</param>
    /// <returns>
    ///     The underlying type if <paramref name="typeSymbol" /> is <see cref="Nullable{T}" />;
    ///     otherwise, <paramref name="typeSymbol" /> itself.
    /// </returns>
    /// <remarks>
    ///     This method is useful for analyzing nullable value types without special-casing nullability.
    /// </remarks>
    [return: NotNullIfNotNull(nameof(typeSymbol))]
    public static ITypeSymbol? GetUnderlyingNullableTypeOrSelf(this ITypeSymbol? typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol
        {
            ConstructedFrom.SpecialType: SpecialType.System_Nullable_T,
            TypeArguments.Length: 1
        } named
            ? named.TypeArguments[0]
            : typeSymbol;
    }

    /// <summary>
    ///     Gets the underlying type if the symbol represents a nullable type (either <see cref="Nullable{T}" /> or a nullable
    ///     reference type).
    /// </summary>
    /// <param name="typeSymbol">The type symbol to unwrap.</param>
    /// <returns>The underlying type symbol.</returns>
    public static ITypeSymbol UnwrapNullable(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedType &&
            namedType.TypeArguments.Length > 0)
            return namedType.TypeArguments[0];

        return typeSymbol.NullableAnnotation == NullableAnnotation.Annotated
            ? typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
            : typeSymbol;
    }
}
