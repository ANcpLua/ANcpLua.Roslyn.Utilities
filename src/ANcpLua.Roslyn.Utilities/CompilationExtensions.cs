using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for the <see cref="Compilation" /> type.
/// </summary>
/// <remarks>
///     <para>
///         This class contains utility methods for working with Roslyn compilations,
///         including language version checks, type accessibility queries, and type resolution.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Language version validation for C# compilations</description>
///         </item>
///         <item>
///             <description>Type accessibility checks across assembly boundaries</description>
///         </item>
///         <item>
///             <description>Best-match type resolution for metadata names</description>
///         </item>
///         <item>
///             <description>Target framework detection</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Compilation" />
/// <seealso cref="CSharpCompilation" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class CompilationExtensions
{
    /// <summary>
    ///     Checks whether the specified compilation is using at least the given C# language version.
    /// </summary>
    /// <param name="compilation">
    ///     The compilation to check.
    /// </param>
    /// <param name="languageVersion">
    ///     The minimum language version to check for.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the <paramref name="compilation" /> uses a language version
    ///     greater than or equal to <paramref name="languageVersion" />; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Non-C# compilations return <c>false</c>.
    /// </remarks>
    /// <seealso cref="LanguageVersion" />
    /// <seealso cref="CSharpCompilation.LanguageVersion" />
    public static bool
        HasLanguageVersionAtLeastEqualTo(this Compilation compilation, LanguageVersion languageVersion)
    {
        return compilation is CSharpCompilation cSharpCompilation &&
               cSharpCompilation.LanguageVersion >= languageVersion;
    }

    /// <summary>
    ///     Checks whether a type with the specified metadata name is accessible from the given compilation.
    /// </summary>
    /// <param name="compilation">
    ///     The compilation to check for type accessibility.
    /// </param>
    /// <param name="fullyQualifiedMetadataName">
    ///     The fully qualified metadata name of the type to look for (e.g., "System.Collections.Generic.List`1").
    /// </param>
    /// <returns>
    ///     <c>true</c> if a type with the specified <paramref name="fullyQualifiedMetadataName" />
    ///     exists and is accessible from the <paramref name="compilation" />'s assembly; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method first attempts to find the type using <see cref="Compilation.GetTypeByMetadataName" />,
    ///         and if that fails, falls back to <see cref="Compilation.GetTypesByMetadataName" /> to handle
    ///         cases where multiple types with the same metadata name exist across referenced assemblies.
    ///     </para>
    ///     <para>
    ///         Accessibility is determined using <see cref="Compilation.IsSymbolAccessibleWithin" />,
    ///         which accounts for internal visibility and friend assemblies.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Compilation.GetTypeByMetadataName" />
    /// <seealso cref="Compilation.GetTypesByMetadataName" />
    /// <seealso cref="Compilation.IsSymbolAccessibleWithin" />
    public static bool HasAccessibleTypeWithMetadataName(this Compilation compilation,
        string fullyQualifiedMetadataName)
    {
        if (compilation.GetTypeByMetadataName(fullyQualifiedMetadataName) is { } typeSymbol)
            return compilation.IsSymbolAccessibleWithin(typeSymbol, compilation.Assembly);

        foreach (var currentTypeSymbol in compilation.GetTypesByMetadataName(fullyQualifiedMetadataName))
            if (compilation.IsSymbolAccessibleWithin(currentTypeSymbol, compilation.Assembly))
                return true;

        return false;
    }

    /// <summary>
    ///     Projects an <see cref="IncrementalValueProvider{Compilation}" /> to a value-equatable
    ///     <see cref="bool" /> indicating whether a type with the given metadata name is accessible
    ///     in the compilation.
    /// </summary>
    /// <param name="compilation">
    ///     The compilation provider, typically <c>context.CompilationProvider</c>, that should be
    ///     reduced to a boolean gate before participating in downstream pipeline stages.
    /// </param>
    /// <param name="fullyQualifiedMetadataName">
    ///     The fully qualified metadata name of the type to look for
    ///     (e.g. <c>"Microsoft.AspNetCore.Builder.WebApplicationBuilder"</c>).
    /// </param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{Boolean}" /> that yields <c>true</c> when the type
    ///     is accessible from the compilation's assembly; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         <see cref="Compilation" /> is not value-equatable — combining it directly into a
    ///         pipeline stage invalidates the cache key on every keystroke. The textbook fix is
    ///         to project the compilation to a value-equatable type (here a <see cref="bool" />)
    ///         before any downstream <c>Combine</c>. This helper does that in one line so callers
    ///         can write:
    ///     </para>
    ///     <code>
    /// var runtimeAvailable = context.CompilationProvider.IsTypeAccessible("My.Runtime.Marker");
    /// values.RegisterCollectedEmitter(context, gate: runtimeAvailable, emitter: ...);
    /// </code>
    /// </remarks>
    /// <seealso cref="HasAccessibleTypeWithMetadataName" />
    public static IncrementalValueProvider<bool> IsTypeAccessible(
        this IncrementalValueProvider<Compilation> compilation,
        string fullyQualifiedMetadataName)
    {
        return compilation.Select((c, _) => c.HasAccessibleTypeWithMetadataName(fullyQualifiedMetadataName));
    }

    /// <summary>
    ///     Checks if the compilation targets .NET 10 or a later version.
    /// </summary>
    /// <param name="compilation">
    ///     The compilation to check.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the <paramref name="compilation" /> targets .NET 10 or greater;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This method determines the target framework version by examining the version
    ///     of the assembly containing <see cref="object" /> (typically mscorlib or System.Runtime).
    /// </remarks>
    public static bool IsNet10OrGreater(this Compilation compilation)
    {
        var type = compilation.GetSpecialType(SpecialType.System_Object);
        var version = type.ContainingAssembly.Identity.Version;
        return version.Major >= 10;
    }

    /// <summary>
    ///     Gets a type by its metadata name, selecting the best match for code analysis purposes.
    /// </summary>
    /// <param name="compilation">
    ///     The compilation to search for the type.
    /// </param>
    /// <param name="fullyQualifiedMetadataName">
    ///     The fully qualified metadata name of the type to find (e.g., "System.String" or
    ///     "System.Collections.Generic.List`1").
    /// </param>
    /// <returns>
    ///     The best matching <see cref="INamedTypeSymbol" /> if one can be determined unambiguously;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method resolves type ambiguity by applying the following priority rules in order:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 If only one type with the metadata name exists, return it regardless of accessibility.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If the current compilation defines the type, prefer that definition.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If exactly one referenced assembly defines the type with public or internal
    ///                 visibility (with friend access), return that.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If multiple visible types exist, return <c>null</c> to indicate ambiguity.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         This approach ensures predictable behavior when the same type is defined in multiple
    ///         referenced assemblies (a common scenario with polyfill packages).
    ///     </para>
    /// </remarks>
    /// <seealso cref="Compilation.GetTypesByMetadataName" />
    /// <seealso cref="HasAccessibleTypeWithMetadataName" />
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

                case SymbolVisibility.Private:
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
