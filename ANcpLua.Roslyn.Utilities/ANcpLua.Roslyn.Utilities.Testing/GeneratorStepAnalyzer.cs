using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Analyzes generator pipeline steps for caching validation.
/// </summary>
/// <remarks>
///     <para>
///         This class provides utilities for extracting and categorizing tracked steps
///         from generator runs. It distinguishes between observable user steps (which should
///         be validated for caching) and infrastructure sink steps (which are internal).
///     </para>
/// </remarks>
internal static class GeneratorStepAnalyzer
{
    private static readonly string[] SinkStepPatterns =
    [
        "RegisterSourceOutput", "RegisterImplementationSourceOutput", "RegisterPostInitializationOutput", "SourceOutput"
    ];

    private static readonly string[] InfrastructureFiles =
    [
        "Attribute.g.cs", "Attributes.g.cs", "EmbeddedAttribute", "Polyfill"
    ];

    /// <summary>
    ///     Extracts tracked steps from a generator run result.
    /// </summary>
    /// <param name="result">The generator run result to analyze.</param>
    /// <returns>A dictionary mapping step names to their execution data.</returns>
    public static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> ExtractSteps(
        GeneratorDriverRunResult result)
    {
        return result.Results.SelectMany(x => x.TrackedSteps).GroupBy(kv => kv.Key)
            .ToDictionary(g => g.Key, g => g.SelectMany(kv => kv.Value).ToImmutableArray());
    }

    /// <summary>
    ///     Determines if a step name represents a sink step (output registration).
    /// </summary>
    /// <param name="stepName">The name of the step to check.</param>
    /// <returns><c>true</c> if this is a sink step; otherwise, <c>false</c>.</returns>
    public static bool IsSink(string stepName)
    {
        return SinkStepPatterns.Any(p => stepName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Determines if a step is an infrastructure step (sink).
    /// </summary>
    /// <param name="stepName">The name of the step to check.</param>
    /// <returns><c>true</c> if this is an infrastructure step; otherwise, <c>false</c>.</returns>
    public static bool IsInfrastructureStep(string stepName)
    {
        return !string.IsNullOrEmpty(stepName) && IsSink(stepName);
    }

    /// <summary>
    ///     Determines if a file is an infrastructure file (e.g., embedded attributes).
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <returns><c>true</c> if this is an infrastructure file; otherwise, <c>false</c>.</returns>
    public static bool IsInfrastructureFile(string fileName)
    {
        return InfrastructureFiles.Any(p => fileName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase));
    }
}