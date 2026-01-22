using ANcpLua.Roslyn.Utilities.Testing.Analysis;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Report on generator caching behavior across two runs.
/// </summary>
/// <remarks>
///     <para>
///         This report aggregates information from two generator runs to analyze caching effectiveness.
///         It tracks observable user steps, sink steps, and any forbidden type violations.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Analyzes the first run for forbidden type violations (e.g., <c>ISymbol</c> or <c>Compilation</c> cached
///                 in pipeline state).
///             </description>
///         </item>
///         <item>
///             <description>
///                 Extracts step data from the second run to determine caching effectiveness.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Filters out infrastructure steps to focus on user-defined pipeline steps.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="GeneratorStepAnalysis" />
/// <seealso cref="ForbiddenTypeViolation" />
/// <seealso cref="GeneratorStepAnalyzer" />
/// <seealso cref="ForbiddenTypeAnalyzer" />
public sealed class GeneratorCachingReport
{
    private GeneratorCachingReport(string generatorName, IReadOnlyList<GeneratorStepAnalysis> observableSteps,
        IReadOnlyList<ForbiddenTypeViolation> violations, bool producedOutput)
    {
        GeneratorName = generatorName;
        ObservableSteps = observableSteps;
        ForbiddenTypeViolations = violations;
        ProducedOutput = producedOutput;
    }

    /// <summary>
    ///     Gets the name of the generator being analyzed.
    /// </summary>
    /// <value>
    ///     The simple type name of the generator (e.g., <c>"MySourceGenerator"</c>).
    /// </value>
    public string GeneratorName { get; }

    /// <summary>
    ///     Gets the observable (user-defined) pipeline steps from the generator run.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Infrastructure steps (internal Roslyn steps) are excluded from this collection.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Steps are ordered alphabetically by name for consistent reporting.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Each step includes caching statistics and forbidden type detection results.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GeneratorStepAnalysis" />
    public IReadOnlyList<GeneratorStepAnalysis> ObservableSteps { get; }

    /// <summary>
    ///     Gets the forbidden type violations detected during the generator run.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Forbidden types include <c>ISymbol</c>, <c>Compilation</c>, and other types that should not be
    ///         cached in incremental generator pipeline state, as they prevent proper caching behavior.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ForbiddenTypeViolation" />
    /// <seealso cref="ForbiddenTypeAnalyzer" />
    public IReadOnlyList<ForbiddenTypeViolation> ForbiddenTypeViolations { get; }

    /// <summary>
    ///     Gets a value indicating whether the generator produced any output files.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Infrastructure files (such as internal Roslyn-generated files) are excluded from this check.
    ///         Only user-visible generated source files are considered.
    ///     </para>
    /// </remarks>
    /// <value>
    ///     <see langword="true" /> if at least one non-infrastructure source file was generated;
    ///     otherwise, <see langword="false" />.
    /// </value>
    public bool ProducedOutput { get; }

    /// <summary>
    ///     Creates a caching report by analyzing two consecutive generator runs.
    /// </summary>
    /// <param name="firstRun">
    ///     The first generator run result, used to detect forbidden type violations.
    /// </param>
    /// <param name="secondRun">
    ///     The second generator run result, used to analyze caching effectiveness.
    /// </param>
    /// <param name="generatorType">
    ///     The <see cref="Type" /> of the generator being tested.
    /// </param>
    /// <returns>
    ///     A <see cref="GeneratorCachingReport" /> containing comprehensive caching analysis.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The two-run approach is essential for caching analysis:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The first run populates the cache and is analyzed for forbidden type violations.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The second run reveals caching behavior by showing which steps were cached vs. re-executed.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Infrastructure steps are automatically filtered out to focus on user-defined pipeline behavior.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="ForbiddenTypeAnalyzer.AnalyzeGeneratorRun" />
    /// <seealso cref="GeneratorStepAnalyzer.ExtractSteps" />
    public static GeneratorCachingReport Create(GeneratorDriverRunResult firstRun, GeneratorDriverRunResult secondRun,
        Type generatorType)
    {
        var violations = ForbiddenTypeAnalyzer.AnalyzeGeneratorRun(firstRun);

        var secondSteps = GeneratorStepAnalyzer.ExtractSteps(secondRun);

        List<GeneratorStepAnalysis> observableSteps = [];

        foreach (var (stepName, stepData) in secondSteps.OrderBy(static kv => kv.Key, StringComparer.Ordinal))
        {
            if (GeneratorStepAnalyzer.IsInfrastructureStep(stepName)) continue;

            var hasForbidden = violations.Any(v => v.StepName == stepName);
            observableSteps.Add(new GeneratorStepAnalysis(stepName, stepData, hasForbidden));
        }

        var producedOutput = secondRun.Results.SelectMany(static r => r.GeneratedSources)
            .Any(static gs => !GeneratorStepAnalyzer.IsInfrastructureFile(gs.HintName));

        return new GeneratorCachingReport(generatorType.Name, observableSteps, violations, producedOutput);
    }
}
