// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Source: https://github.com/Sergio0694/PolySharp

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for the <see cref="Compilation" /> type.
/// </summary>
public static class CompilationExtensions
{
    /// <summary>
    ///     Checks whether a given compilation (assumed to be for C#) is using at least a given language version.
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation" /> to consider for analysis.</param>
    /// <param name="languageVersion">The minimum language version to check.</param>
    /// <returns>Whether <paramref name="compilation" /> is using at least the specified language version.</returns>
    public static bool HasLanguageVersionAtLeastEqualTo(this Compilation compilation, LanguageVersion languageVersion)
    {
        return ((CSharpCompilation)compilation).LanguageVersion >= languageVersion;
    }

    /// <summary>
    ///     Checks whether or not a type with a specified metadata name is accessible from a given <see cref="Compilation" />
    ///     instance.
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation" /> to consider for analysis.</param>
    /// <param name="fullyQualifiedMetadataName">The fully-qualified metadata type name to find.</param>
    /// <returns>Whether a type with the specified metadata name can be accessed from the given compilation.</returns>
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
}