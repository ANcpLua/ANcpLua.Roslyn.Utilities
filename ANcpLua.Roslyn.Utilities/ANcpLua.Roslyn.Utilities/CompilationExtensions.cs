using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for the <see cref="Compilation" /> type.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class CompilationExtensions
{
    /// <summary>
    ///     Checks whether a given compilation (assumed to be for C#) is using at least a given language version.
    /// </summary>
    public static bool
        HasLanguageVersionAtLeastEqualTo(this Compilation compilation, LanguageVersion languageVersion) =>
        ((CSharpCompilation)compilation).LanguageVersion >= languageVersion;

    /// <summary>
    ///     Checks whether or not a type with a specified metadata name is accessible from a given <see cref="Compilation" />.
    /// </summary>
    public static bool HasAccessibleTypeWithMetadataName(this Compilation compilation,
        string fullyQualifiedMetadataName)
    {
        if (compilation.GetTypeByMetadataName(fullyQualifiedMetadataName) is { } typeSymbol)
            return compilation.IsSymbolAccessibleWithin(typeSymbol, compilation.Assembly);

        foreach (var currentTypeSymbol in compilation.GetTypesByMetadataName(fullyQualifiedMetadataName))
        {
            if (compilation.IsSymbolAccessibleWithin(currentTypeSymbol, compilation.Assembly))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if the compilation targets .NET 9 or greater.
    /// </summary>
    public static bool IsNet9OrGreater(this Compilation compilation)
    {
        var type = compilation.GetSpecialType(SpecialType.System_Object);
        var version = type.ContainingAssembly.Identity.Version;
        return version.Major >= 9;
    }

    /// <summary>
    ///     Gets a type by its metadata name, preferring the best match for code analysis.
    ///     Returns the symbol matching these rules in order:
    ///     1. If only one type exists, return it regardless of accessibility
    ///     2. If the current compilation defines it, return that
    ///     3. If exactly one referenced assembly defines it visibly, return that
    ///     4. Otherwise, return null
    /// </summary>
    public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation,
        string fullyQualifiedMetadataName)
    {
        INamedTypeSymbol? type = null;

        foreach (var currentType in compilation.GetTypesByMetadataName(fullyQualifiedMetadataName))
        {
            if (ReferenceEquals(currentType.ContainingAssembly, compilation.Assembly))
                return currentType;

            switch (currentType.GetResultantVisibility())
            {
                case SymbolVisibility.Public:
                case SymbolVisibility.Internal when currentType.ContainingAssembly.GivesAccessTo(compilation.Assembly):
                    break;

                default:
                    continue;
            }

            if (type is not null)
                return null;

            type = currentType;
        }

        return type;
    }

    private static SymbolVisibility GetResultantVisibility(this ISymbol symbol)
    {
        var visibility = SymbolVisibility.Public;

        while (symbol.Kind is not SymbolKind.Namespace)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    return SymbolVisibility.Private;
                case SymbolKind.Parameter:
                    symbol = symbol.ContainingSymbol;
                    continue;
                case SymbolKind.TypeParameter:
                    return SymbolVisibility.Private;
            }

            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    return SymbolVisibility.Private;
                case Accessibility.Internal:
                case Accessibility.ProtectedAndInternal:
                    visibility = SymbolVisibility.Internal;
                    break;
            }

            symbol = symbol.ContainingSymbol;
        }

        return visibility;
    }

    private enum SymbolVisibility
    {
        Public,
        Internal,
        Private
    }
}