using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities.Testing.Formatting;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing.Analysis;

/// <summary>
///     Represents a forbidden type violation in the generator pipeline.
/// </summary>
/// <param name="StepName">The step where the violation occurred.</param>
/// <param name="ForbiddenType">The forbidden type that was cached.</param>
/// <param name="Path">The path to the forbidden type.</param>
/// <remarks>
///     <para>
///         Forbidden types include Roslyn runtime types such as <see cref="ISymbol" />,
///         <see cref="Compilation" />, <see cref="SyntaxNode" />, etc. Caching these types
///         causes memory leaks and IDE performance degradation.
///     </para>
/// </remarks>
public sealed record ForbiddenTypeViolation(string StepName, Type ForbiddenType, string Path);

/// <summary>
///     Analysis of a single generator pipeline step.
/// </summary>
public readonly struct GeneratorStepAnalysis
{
    public string StepName { get; }
    public int Cached { get; }
    public int Unchanged { get; }
    public int Modified { get; }
    public int New { get; }
    public int Removed { get; }
    public bool HasForbiddenTypes { get; }

    public bool IsCachedSuccessfully => Modified is 0 && New is 0 && Removed is 0;

    public GeneratorStepAnalysis(string stepName, ImmutableArray<IncrementalGeneratorRunStep> secondRun,
        bool hasForbiddenTypes)
    {
        StepName = stepName;
        HasForbiddenTypes = hasForbiddenTypes;

        int cached = 0, unchanged = 0, modified = 0, @new = 0, removed = 0;
        foreach (var output in secondRun.SelectMany(static step => step.Outputs))
        {
            switch (output.Reason)
            {
                case IncrementalStepRunReason.Cached: cached++; break;
                case IncrementalStepRunReason.Unchanged: unchanged++; break;
                case IncrementalStepRunReason.Modified: modified++; break;
                case IncrementalStepRunReason.New: @new++; break;
                case IncrementalStepRunReason.Removed: removed++; break;
                default: modified++; break;
            }
        }

        Cached = cached;
        Unchanged = unchanged;
        Modified = modified;
        New = @new;
        Removed = removed;
    }

    public string FormatBreakdown() => StepFormatter.FormatBreakdown(this);
}

/// <summary>
///     Classification logic for generator pipeline steps.
/// </summary>
internal static class StepClassification
{
    private static readonly string[] SinkStepPatterns =
    [
        "RegisterSourceOutput", "RegisterImplementationSourceOutput", "RegisterPostInitializationOutput", "SourceOutput"
    ];

    private static readonly string[] InfrastructureFilePatterns =
    [
        "Attribute.g.cs", "Attributes.g.cs", "EmbeddedAttribute", "Polyfill"
    ];

    /// <summary>
    ///     Determines if a step name represents a sink step (output registration).
    /// </summary>
    /// <param name="stepName">The name of the step to check.</param>
    /// <returns><c>true</c> if this is a sink step; otherwise, <c>false</c>.</returns>
    public static bool IsSinkStep(string stepName) => SinkStepPatterns.Any(p =>
        stepName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Determines if a step is an infrastructure step (sink).
    /// </summary>
    /// <param name="stepName">The name of the step to check.</param>
    /// <returns><c>true</c> if this is an infrastructure step; otherwise, <c>false</c>.</returns>
    public static bool IsInfrastructureStep(string stepName) => IsSinkStep(stepName);

    /// <summary>
    ///     Determines if a file is an infrastructure file (e.g., embedded attributes).
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <returns><c>true</c> if this is an infrastructure file; otherwise, <c>false</c>.</returns>
    public static bool IsInfrastructureFile(string fileName) => InfrastructureFilePatterns.Any(p =>
        fileName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase));
}
