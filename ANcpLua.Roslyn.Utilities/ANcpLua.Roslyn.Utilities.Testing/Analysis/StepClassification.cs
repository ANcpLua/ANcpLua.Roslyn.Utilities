using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities.Testing.Formatting;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing.Analysis;

/// <summary>
///     Represents a forbidden type violation in the generator pipeline.
/// </summary>
/// <param name="StepName">The step where the violation occurred.</param>
/// <param name="ForbiddenType">The forbidden type that was cached.</param>
/// <param name="Path">The path to the forbidden type within the output structure.</param>
/// <remarks>
///     <para>
///         Forbidden types include Roslyn runtime types such as <see cref="ISymbol" />,
///         <see cref="Compilation" />, <see cref="SyntaxNode" />, etc. Caching these types
///         causes memory leaks and IDE performance degradation.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Violations indicate the generator output contains types that should not be cached.</description>
///         </item>
///         <item>
///             <description>The <paramref name="Path" /> helps locate the violation within nested data structures.</description>
///         </item>
///         <item>
///             <description>Fix by extracting only primitive data from Roslyn types before caching.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="GeneratorStepAnalysis" />
/// <seealso cref="GeneratorCachingReport" />
public sealed record ForbiddenTypeViolation(string StepName, Type ForbiddenType, string Path);

/// <summary>
///     Analysis of a single generator pipeline step, containing counts of output states
///     and caching effectiveness metrics.
/// </summary>
/// <remarks>
///     <para>
///         This struct captures the state of outputs from an incremental generator step
///         during the second run, which reveals caching behavior.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="Cached" /> and <see cref="Unchanged" /> outputs indicate successful caching.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="Modified" />, <see cref="New" />, and <see cref="Removed" /> indicate cache misses.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Use <see cref="IsCachedSuccessfully" /> to quickly check if the step achieved full caching.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="ForbiddenTypeViolation" />
/// <seealso cref="StepClassification" />
/// <seealso cref="IncrementalStepRunReason" />
public readonly struct GeneratorStepAnalysis
{
    /// <summary>
    ///     Gets the name of the analyzed step.
    /// </summary>
    /// <returns>The step name as defined in the generator pipeline.</returns>
    public string StepName { get; }

    /// <summary>
    ///     Gets the count of cached outputs.
    /// </summary>
    /// <returns>
    ///     The number of outputs with <see cref="IncrementalStepRunReason.Cached" /> state.
    /// </returns>
    /// <remarks>
    ///     Cached outputs were reused from the previous run without recomputation,
    ///     indicating optimal performance.
    /// </remarks>
    public int Cached { get; }

    /// <summary>
    ///     Gets the count of unchanged outputs.
    /// </summary>
    /// <returns>
    ///     The number of outputs with <see cref="IncrementalStepRunReason.Unchanged" /> state.
    /// </returns>
    /// <remarks>
    ///     Unchanged outputs were recomputed but produced the same value,
    ///     indicating the equality comparison succeeded.
    /// </remarks>
    public int Unchanged { get; }

    /// <summary>
    ///     Gets the count of modified outputs.
    /// </summary>
    /// <returns>
    ///     The number of outputs with <see cref="IncrementalStepRunReason.Modified" /> state.
    /// </returns>
    /// <remarks>
    ///     Modified outputs indicate values that changed between runs,
    ///     which may indicate a caching issue if the source was unchanged.
    /// </remarks>
    public int Modified { get; }

    /// <summary>
    ///     Gets the count of new outputs.
    /// </summary>
    /// <returns>
    ///     The number of outputs with <see cref="IncrementalStepRunReason.New" /> state.
    /// </returns>
    /// <remarks>
    ///     New outputs appear when additional items are produced compared to the previous run.
    /// </remarks>
    public int New { get; }

    /// <summary>
    ///     Gets the count of removed outputs.
    /// </summary>
    /// <returns>
    ///     The number of outputs with <see cref="IncrementalStepRunReason.Removed" /> state.
    /// </returns>
    /// <remarks>
    ///     Removed outputs indicate items that were present in the previous run but are no longer produced.
    /// </remarks>
    public int Removed { get; }

    /// <summary>
    ///     Gets a value indicating whether this step has forbidden type violations.
    /// </summary>
    /// <returns>
    ///     <c>true</c> if the step output contains types that should not be cached; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="ForbiddenTypeViolation" />
    public bool HasForbiddenTypes { get; }

    /// <summary>
    ///     Gets a value indicating whether this step was cached successfully.
    /// </summary>
    /// <returns>
    ///     <c>true</c> if there are no <see cref="Modified" />, <see cref="New" />, or <see cref="Removed" /> outputs;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     A successfully cached step produces only <see cref="Cached" /> or <see cref="Unchanged" /> outputs,
    ///     indicating the generator properly implements incremental caching.
    /// </remarks>
    public bool IsCachedSuccessfully => Modified is 0 && New is 0 && Removed is 0;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratorStepAnalysis" /> struct.
    /// </summary>
    /// <param name="stepName">The name of the step being analyzed.</param>
    /// <param name="secondRun">The steps from the second generator run to analyze.</param>
    /// <param name="hasForbiddenTypes">
    ///     <c>true</c> if this step has forbidden type violations; otherwise, <c>false</c>.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         The constructor iterates through all outputs in <paramref name="secondRun" />
    ///         and categorizes them by their <see cref="IncrementalStepRunReason" />.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Unknown run reasons are counted as <see cref="Modified" />.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The <paramref name="hasForbiddenTypes" /> flag is typically set by
    ///                 <see cref="ForbiddenTypeAnalyzer" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="IncrementalGeneratorRunStep" />
    public GeneratorStepAnalysis(string stepName, ImmutableArray<IncrementalGeneratorRunStep> secondRun,
        bool hasForbiddenTypes)
    {
        StepName = stepName;
        HasForbiddenTypes = hasForbiddenTypes;

        int cached = 0, unchanged = 0, modified = 0, @new = 0, removed = 0;
        foreach (var output in secondRun.SelectMany(static step => step.Outputs))
            switch (output.Reason)
            {
                case IncrementalStepRunReason.Cached: cached++; break;
                case IncrementalStepRunReason.Unchanged: unchanged++; break;
                case IncrementalStepRunReason.Modified: modified++; break;
                case IncrementalStepRunReason.New: @new++; break;
                case IncrementalStepRunReason.Removed: removed++; break;
                default: modified++; break;
            }

        Cached = cached;
        Unchanged = unchanged;
        Modified = modified;
        New = @new;
        Removed = removed;
    }

    /// <summary>
    ///     Formats a breakdown of the step analysis results.
    /// </summary>
    /// <returns>
    ///     A formatted string showing the counts of each output reason,
    ///     suitable for display in test output or reports.
    /// </returns>
    /// <seealso cref="StepFormatter.FormatBreakdown" />
    public string FormatBreakdown() => StepFormatter.FormatBreakdown(this);
}

/// <summary>
///     Classification logic for generator pipeline steps, providing utilities to identify
///     sink steps and infrastructure files.
/// </summary>
/// <remarks>
///     <para>
///         This class provides pattern-based classification of generator steps and files
///         to support caching analysis and report generation.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Sink steps (output registrations) are treated differently in caching analysis
///                 because they represent the final output phase.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Infrastructure files (embedded attributes, polyfills) are typically excluded
///                 from user-facing reports.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="GeneratorStepAnalysis" />
/// <seealso cref="GeneratorCachingReport" />
internal static class StepClassification
{
    /// <summary>
    ///     Patterns that identify sink steps (output registration methods).
    /// </summary>
    private static readonly string[] SinkStepPatterns =
    [
        "RegisterSourceOutput", "RegisterImplementationSourceOutput", "RegisterPostInitializationOutput", "SourceOutput"
    ];

    /// <summary>
    ///     Patterns that identify infrastructure files.
    /// </summary>
    private static readonly string[] InfrastructureFilePatterns =
    [
        "Attribute.g.cs", "Attributes.g.cs", "EmbeddedAttribute", "Polyfill"
    ];

    /// <summary>
    ///     Determines if a step name represents a sink step (output registration).
    /// </summary>
    /// <param name="stepName">The name of the step to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="stepName" /> matches any sink step pattern; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Sink steps are the terminal nodes in a generator pipeline where output is registered.
    ///         These include methods like <c>RegisterSourceOutput</c> and <c>RegisterImplementationSourceOutput</c>.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Pattern matching is case-insensitive.</description>
    ///         </item>
    ///         <item>
    ///             <description>Partial matches are accepted (contains check).</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static bool IsSinkStep(string stepName)
    {
        return SinkStepPatterns.Any(p =>
            stepName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Determines if a step is an infrastructure step.
    /// </summary>
    /// <param name="stepName">The name of the step to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="stepName" /> represents an infrastructure step; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Currently, infrastructure steps are equivalent to sink steps.
    ///     This method exists to allow future differentiation if needed.
    /// </remarks>
    /// <seealso cref="IsSinkStep" />
    public static bool IsInfrastructureStep(string stepName) => IsSinkStep(stepName);

    /// <summary>
    ///     Determines if a file is an infrastructure file (e.g., embedded attributes, polyfills).
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="fileName" /> matches any infrastructure file pattern; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Infrastructure files are auxiliary outputs that support the main generated code,
    ///         such as attribute definitions or polyfills for missing framework features.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Pattern matching is case-insensitive.</description>
    ///         </item>
    ///         <item>
    ///             <description>Partial matches are accepted (contains check).</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Common patterns include <c>Attribute.g.cs</c>, <c>EmbeddedAttribute</c>, and <c>Polyfill</c>.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static bool IsInfrastructureFile(string fileName)
    {
        return InfrastructureFilePatterns.Any(p =>
            fileName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase));
    }
}
