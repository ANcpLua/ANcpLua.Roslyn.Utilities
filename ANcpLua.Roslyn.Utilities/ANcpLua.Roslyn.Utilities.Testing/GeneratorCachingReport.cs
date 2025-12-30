using System.Collections.Immutable;
using System.Text.Json;
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
/// </remarks>
public sealed class GeneratorCachingReport
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private GeneratorCachingReport(string generatorName, IReadOnlyList<GeneratorStepAnalysis> observableSteps,
        IReadOnlyList<GeneratorStepAnalysis> sinkSteps, IReadOnlyList<ForbiddenTypeViolation> violations,
        bool producedOutput)
    {
        GeneratorName = generatorName;
        ObservableSteps = observableSteps;
        SinkSteps = sinkSteps;
        ForbiddenTypeViolations = violations;
        ProducedOutput = producedOutput;
    }

    /// <summary>
    ///     The generator name.
    /// </summary>
    public string GeneratorName { get; }

    /// <summary>
    ///     Observable (user) pipeline steps.
    /// </summary>
    public IReadOnlyList<GeneratorStepAnalysis> ObservableSteps { get; }

    /// <summary>
    ///     Sink (infrastructure) steps.
    /// </summary>
    public IReadOnlyList<GeneratorStepAnalysis> SinkSteps { get; }

    /// <summary>
    ///     Forbidden type violations found.
    /// </summary>
    public IReadOnlyList<ForbiddenTypeViolation> ForbiddenTypeViolations { get; }

    /// <summary>
    ///     Whether the generator produced output.
    /// </summary>
    public bool ProducedOutput { get; }

    /// <summary>
    ///     Whether the report indicates correct behavior (no forbidden types).
    /// </summary>
    public bool IsCorrect => ForbiddenTypeViolations.Count is 0;

    /// <summary>
    ///     Creates a caching report from two run results.
    /// </summary>
    /// <param name="firstRun">The first generator run result.</param>
    /// <param name="secondRun">The second generator run result.</param>
    /// <param name="generatorType">The generator type being tested.</param>
    /// <returns>A comprehensive caching report.</returns>
    public static GeneratorCachingReport Create(GeneratorDriverRunResult firstRun, GeneratorDriverRunResult secondRun,
        Type generatorType)
    {
        var violations = ForbiddenTypeAnalyzer.AnalyzeGeneratorRun(firstRun);

        var firstSteps = GeneratorStepAnalyzer.ExtractSteps(firstRun);
        var secondSteps = GeneratorStepAnalyzer.ExtractSteps(secondRun);

        List<GeneratorStepAnalysis> observableSteps = [];
        List<GeneratorStepAnalysis> sinkSteps = [];

        foreach (var stepName in firstSteps.Keys.Union(secondSteps.Keys).OrderBy(n => n, StringComparer.Ordinal))
        {
            var firstStepData =
                firstSteps.GetValueOrDefault(stepName, ImmutableArray<IncrementalGeneratorRunStep>.Empty);
            var secondStepData =
                secondSteps.GetValueOrDefault(stepName, ImmutableArray<IncrementalGeneratorRunStep>.Empty);
            var hasForbidden = violations.Any(v => v.StepName == stepName);
            GeneratorStepAnalysis analysis = new(stepName, firstStepData, secondStepData, hasForbidden);

            if (GeneratorStepAnalyzer.IsInfrastructureStep(stepName)) sinkSteps.Add(analysis);
            else observableSteps.Add(analysis);
        }

        var producedOutput = secondRun.Results.SelectMany(r => r.GeneratedSources)
            .Any(gs => !GeneratorStepAnalyzer.IsInfrastructureFile(gs.HintName));

        return new GeneratorCachingReport(generatorType.Name, observableSteps, sinkSteps, violations, producedOutput);
    }

    /// <summary>
    ///     Builds a comprehensive failure report.
    /// </summary>
    /// <param name="failedCaching">Steps that failed caching validation.</param>
    /// <param name="requiredSteps">Steps that were explicitly required.</param>
    /// <returns>A formatted failure report string.</returns>
    public string BuildComprehensiveFailureReport(List<GeneratorStepAnalysis> failedCaching, string[]? requiredSteps)
    {
        StringBuilder sb = new();
        var issueNumber = 0;

        if (ForbiddenTypeViolations.Count > 0)
            foreach (var group in ForbiddenTypeViolations.GroupBy(v => v.StepName))
            {
                issueNumber++;
                sb.AppendLine($"--- ISSUE {issueNumber} (CRITICAL): Forbidden Type Cached in '{group.Key}' ---");
                sb.AppendLine("  Detail: Caching ISymbol/Compilation/SyntaxNode causes IDE performance degradation.");
                sb.AppendLine("  Recommendation: Store only simple, equatable data (prefer 'record').");
                foreach (var violation in group)
                    sb.AppendLine($"    - {violation.ForbiddenType.FullName} at {violation.Path}");
                sb.AppendLine();
            }

        foreach (var step in failedCaching)
        {
            issueNumber++;
            sb.AppendLine($"--- ISSUE {issueNumber}: Step Not Cached '{step.StepName}' ---");
            sb.AppendLine($"  Breakdown: {step.FormatBreakdown()}");
            sb.AppendLine(step.HasForbiddenTypes
                ? "  Root Cause: Likely forbidden Roslyn runtime types cached."
                : "  Recommendation: Ensure output model has value equality.");
            sb.AppendLine();
        }

        if (!ProducedOutput && issueNumber is 0)
        {
            issueNumber++;
            sb.AppendLine($"--- ISSUE {issueNumber}: No Meaningful Output Produced ---");
            sb.AppendLine("  Detail: Generator produced no non-infrastructure hint files.");
        }

        sb.AppendLine("=== Full Pipeline Overview ===");
        foreach (var step in ObservableSteps.OrderBy(x => x.StepName))
        {
            var tracked = requiredSteps?.Contains(step.StepName) == true ? "[Tracked]" : "";
            var forbidden = step.HasForbiddenTypes ? "[!]" : "";
            var icon = step.IsCachedSuccessfully ? "[OK]" : "[FAIL]";
            sb.AppendLine(
                $"  {icon} {step.StepName} {tracked} {forbidden} | {step.FormatBreakdown()} | {step.FormatPerformance()}");
        }

        if (TestConfiguration.EnableJsonReporting)
        {
            sb.AppendLine("\n--- MACHINE REPORT (JSON) ---");
            List<object> machineIssues = [];
            if (ForbiddenTypeViolations.Count > 0)
                foreach (var group in ForbiddenTypeViolations.GroupBy(v => v.StepName))
                    machineIssues.Add(new
                    {
                        type = "ForbiddenType", severity = "CRITICAL", step = group.Key, count = group.Count()
                    });

            foreach (var step in failedCaching)
                machineIssues.Add(new
                {
                    type = "CacheFailure",
                    severity = "ERROR",
                    step = step.StepName,
                    breakdown = new
                    {
                        step.Cached,
                        step.Unchanged,
                        step.Modified,
                        step.New,
                        step.Removed
                    }
                });

            if (!ProducedOutput) machineIssues.Add(new { type = "NoOutput", severity = "WARN" });

            var payload = new
            {
                generator = GeneratorName,
                producedOutput = ProducedOutput,
                forbidden = ForbiddenTypeViolations.Select(v => new
                {
                    step = v.StepName, type = v.ForbiddenType.FullName, v.Path
                }),
                failedSteps = failedCaching.Select(s => new
                {
                    s.StepName,
                    s.Cached,
                    s.Unchanged,
                    s.Modified,
                    s.New,
                    s.Removed
                }),
                machineIssues
            };
            sb.AppendLine(JsonSerializer.Serialize(payload, JsonOptions));
        }

        return sb.ToString();
    }
}

/// <summary>
///     Analysis of a single generator pipeline step.
/// </summary>
/// <remarks>
///     <para>
///         This struct captures caching metrics for a single step, including counts of
///         cached, unchanged, modified, new, and removed outputs, as well as timing data.
///     </para>
/// </remarks>
public readonly struct GeneratorStepAnalysis
{
    /// <summary>
    ///     The step name.
    /// </summary>
    public string StepName { get; }

    /// <summary>
    ///     Number of cached outputs.
    /// </summary>
    public int Cached { get; }

    /// <summary>
    ///     Number of unchanged outputs.
    /// </summary>
    public int Unchanged { get; }

    /// <summary>
    ///     Number of modified outputs.
    /// </summary>
    public int Modified { get; }

    /// <summary>
    ///     Number of new outputs.
    /// </summary>
    public int New { get; }

    /// <summary>
    ///     Number of removed outputs.
    /// </summary>
    public int Removed { get; }

    /// <summary>
    ///     Whether this step has forbidden types.
    /// </summary>
    public bool HasForbiddenTypes { get; }

    /// <summary>
    ///     Elapsed time for first run.
    /// </summary>
    public TimeSpan ElapsedTimeFirstRun { get; }

    /// <summary>
    ///     Elapsed time for second run.
    /// </summary>
    public TimeSpan ElapsedTimeSecondRun { get; }

    /// <summary>
    ///     Total number of outputs.
    /// </summary>
    public int TotalOutputs => Cached + Unchanged + Modified + New + Removed;

    /// <summary>
    ///     Whether caching was successful (no modified, new, or removed outputs).
    /// </summary>
    public bool IsCachedSuccessfully => Modified is 0 && New is 0 && Removed is 0;

    /// <summary>
    ///     Creates a step analysis from run data.
    /// </summary>
    /// <param name="stepName">The name of the step.</param>
    /// <param name="firstRun">Data from the first run.</param>
    /// <param name="secondRun">Data from the second run.</param>
    /// <param name="hasForbiddenTypes">Whether this step contains forbidden types.</param>
    public GeneratorStepAnalysis(string stepName, ImmutableArray<IncrementalGeneratorRunStep> firstRun,
        ImmutableArray<IncrementalGeneratorRunStep> secondRun, bool hasForbiddenTypes)
    {
        StepName = stepName;
        HasForbiddenTypes = hasForbiddenTypes;

        int cached = 0, unchanged = 0, modified = 0, @new = 0, removed = 0;
        foreach (var output in secondRun.SelectMany(step => step.Outputs))
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
        ElapsedTimeFirstRun = firstRun.IsDefaultOrEmpty
            ? TimeSpan.Zero
            : firstRun.Aggregate(TimeSpan.Zero, (t, s) => t + s.ElapsedTime);
        ElapsedTimeSecondRun = secondRun.IsDefaultOrEmpty
            ? TimeSpan.Zero
            : secondRun.Aggregate(TimeSpan.Zero, (t, s) => t + s.ElapsedTime);
    }

    /// <summary>
    ///     Formats the breakdown for display.
    /// </summary>
    /// <returns>A formatted string showing caching metrics.</returns>
    public string FormatBreakdown()
    {
        return $"C:{Cached} U:{Unchanged} | M:{Modified} N:{New} R:{Removed} (Total:{TotalOutputs})";
    }

    /// <summary>
    ///     Formats the performance for display.
    /// </summary>
    /// <returns>A formatted string showing timing comparison.</returns>
    public string FormatPerformance()
    {
        return $"{ElapsedTimeFirstRun.TotalMilliseconds:F2}ms -> {ElapsedTimeSecondRun.TotalMilliseconds:F2}ms";
    }
}