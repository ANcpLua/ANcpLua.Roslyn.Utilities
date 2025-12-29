using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
    ///     .NET 10 reference assemblies (not yet in Microsoft.CodeAnalysis.Testing stable release).
    /// </summary>
    private static readonly ReferenceAssemblies Net100Assemblies = new(
        "net10.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"),
        Path.Combine("ref", "net10.0"));

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
    ///     The reference assemblies to use for tests (.NET 10).
    /// </summary>
    public static ReferenceAssemblies ReferenceAssemblies { get; set; } = Net100Assemblies;

    /// <summary>
    ///     Additional references to include in compilations.
    /// </summary>
    public static ImmutableArray<PortableExecutableReference> AdditionalReferences { get; set; } =
        ImmutableArray<PortableExecutableReference>.Empty;
}
