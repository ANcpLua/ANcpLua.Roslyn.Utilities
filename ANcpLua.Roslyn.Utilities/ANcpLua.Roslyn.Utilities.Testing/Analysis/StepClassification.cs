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
    ///     A successfully cached step produces only <see cref="Cached" /> or <see cref="Unchanged" /> outputs.
    ///     Note that <see cref="Unchanged" /> means the step re-ran but produced the same value —
    ///     use <see cref="IsTrulyCached" /> to detect steps that were skipped entirely.
    /// </remarks>
    public bool IsCachedSuccessfully => Modified is 0 && New is 0 && Removed is 0;

    /// <summary>
    ///     Gets a value indicating whether all outputs were truly cached without any re-computation.
    /// </summary>
    /// <returns>
    ///     <c>true</c> if all outputs have <see cref="IncrementalStepRunReason.Cached" /> state
    ///     (no <see cref="Unchanged" />, <see cref="Modified" />, <see cref="New" />, or <see cref="Removed" />);
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Unlike <see cref="IsCachedSuccessfully" /> which treats <see cref="Unchanged" /> as success,
    ///     this property requires all outputs to be fully cached. An <see cref="Unchanged" /> output means
    ///     the step re-ran but produced the same value — wasted work caused by broken input equality.
    /// </remarks>
    public bool IsTrulyCached => Unchanged is 0 && Modified is 0 && New is 0 && Removed is 0;

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
    ///     Patterns that identify Roslyn internal pipeline steps which are not user-controllable.
    ///     These steps are auto-generated by Roslyn APIs like <c>ForAttributeWithMetadataName</c>
    ///     and will always show <c>Modified</c> when the Compilation changes (which is every run in tests).
    /// </summary>
    private static readonly string[] RoslynInternalStepPatterns =
    [
        "Compilation",
        "ForAttributeWithMetadataName",
        "compilationAndGroupedNodes",
        "compilationUnit"
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
        foreach (var p in SinkStepPatterns)
            if (stepName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>
    ///     Determines if a step is a Roslyn internal step that is not user-controllable.
    /// </summary>
    /// <param name="stepName">The name of the step to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="stepName" /> matches a known Roslyn internal pattern; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Roslyn internal steps are auto-generated by the incremental pipeline and cannot be
    ///         controlled by generator authors. They always show <c>Modified</c> when the Compilation
    ///         changes, which is expected in test scenarios that create a new Compilation.
    ///     </para>
    /// </remarks>
    public static bool IsRoslynInternalStep(string stepName)
    {
        foreach (var p in RoslynInternalStepPatterns)
            if (stepName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>
    ///     Determines if a step is an infrastructure step (sink or Roslyn internal).
    /// </summary>
    /// <param name="stepName">The name of the step to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="stepName" /> represents an infrastructure step; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Infrastructure steps include both output registration sinks and Roslyn internal steps
    ///     that are not user-controllable and should be excluded from caching validation.
    /// </remarks>
    /// <seealso cref="IsSinkStep" />
    /// <seealso cref="IsRoslynInternalStep" />
    public static bool IsInfrastructureStep(string stepName) => IsSinkStep(stepName) || IsRoslynInternalStep(stepName);

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
        foreach (var p in InfrastructureFilePatterns)
            if (fileName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
