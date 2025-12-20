using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Configuration settings for generator tests.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    ///     Performance tolerance as a percentage (default: 5%).
    /// </summary>
    public const double PerformanceTolerancePercent = 0.05;

    /// <summary>
    ///     Whether to enable JSON reporting in test output.
    /// </summary>
    public const bool EnableJsonReporting = true;

    /// <summary>
    ///     Absolute performance tolerance.
    /// </summary>
    public static readonly TimeSpan PerformanceToleranceAbsolute = TimeSpan.FromMilliseconds(2);

    /// <summary>
    ///     The C# language version to use for tests.
    /// </summary>
    public static LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Preview;

    /// <summary>
    ///     The reference assemblies to use for tests.
    /// </summary>
    public static ReferenceAssemblies ReferenceAssemblies { get; set; } = ReferenceAssemblies.Net.Net90;

    /// <summary>
    ///     Additional references to include in compilations.
    /// </summary>
    public static ImmutableArray<PortableExecutableReference> AdditionalReferences { get; set; } =
        ImmutableArray<PortableExecutableReference>.Empty;

    /// <summary>
    ///     Combines the provided references with additional references.
    /// </summary>
    /// <param name="references">The base references.</param>
    /// <returns>Combined references.</returns>
    public static IReadOnlyList<MetadataReference> CombineReferences(
        ImmutableArray<MetadataReference> references)
    {
        List<MetadataReference> combined = new(references.Length + AdditionalReferences.Length);
        combined.AddRange(references);
        combined.AddRange(AdditionalReferences);
        return combined;
    }
}