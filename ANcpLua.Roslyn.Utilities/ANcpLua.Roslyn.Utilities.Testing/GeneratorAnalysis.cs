using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Analyzes generator pipeline steps.
/// </summary>
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
    public static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> ExtractSteps(
        GeneratorDriverRunResult result)
    {
        return result.Results.SelectMany(x => x.TrackedSteps).GroupBy(kv => kv.Key)
            .ToDictionary(g => g.Key, g => g.SelectMany(kv => kv.Value).ToImmutableArray());
    }

    /// <summary>
    ///     Determines if a step name represents a sink step.
    /// </summary>
    public static bool IsSink(string stepName)
    {
        return SinkStepPatterns.Any(p => stepName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Determines if a step is an infrastructure step (sink).
    /// </summary>
    public static bool IsInfrastructureStep(string stepName)
    {
        return !string.IsNullOrEmpty(stepName) && IsSink(stepName);
    }

    /// <summary>
    ///     Determines if a file is an infrastructure file.
    /// </summary>
    public static bool IsInfrastructureFile(string fileName)
    {
        return InfrastructureFiles.Any(p => fileName.AsSpan().Contains(p.AsSpan(), StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
///     Represents a forbidden type violation in the generator pipeline.
/// </summary>
/// <param name="StepName">The step where the violation occurred.</param>
/// <param name="ForbiddenType">The forbidden type that was cached.</param>
/// <param name="Path">The path to the forbidden type.</param>
public sealed record ForbiddenTypeViolation(string StepName, Type ForbiddenType, string Path);

/// <summary>
///     Analyzes generator outputs for forbidden Roslyn types.
/// </summary>
internal static class ForbiddenTypeAnalyzer
{
    private static readonly HashSet<Type> ForbiddenTypes =
    [
        typeof(ISymbol),
        typeof(Compilation),
        typeof(SemanticModel),
        typeof(SyntaxNode),
        typeof(SyntaxTree),
        Type.GetType("Microsoft.CodeAnalysis.IOperation, Microsoft.CodeAnalysis") ??
        typeof(object) // Fallback if type not found
    ];

    private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new();

    /// <summary>
    ///     Analyzes a generator run result for forbidden type violations.
    /// </summary>
    public static IReadOnlyList<ForbiddenTypeViolation> AnalyzeGeneratorRun(GeneratorDriverRunResult run)
    {
        List<ForbiddenTypeViolation> violations = [];
        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);

        foreach (var (stepName, steps) in run.Results.SelectMany(r =>
                     r.TrackedSteps))
        foreach (var step in steps)
        foreach (var (output, _) in step.Outputs)
        {
            Visit(output, stepName, "Output");
            if (violations.Count >= 256) return violations;
        }

        return violations;

        void Visit(object? node, string step, string path)
        {
            if (node is null) return;

            var type = node.GetType();

            if (!type.IsValueType && !visited.Add(node)) return;

            if (IsForbiddenType(type))
            {
                violations.Add(new ForbiddenTypeViolation(step, type, path));
                return;
            }

            if (IsAllowedType(type)) return;

            if (node is IEnumerable collection and not string)
            {
                var index = 0;
                foreach (var element in collection)
                {
                    Visit(element, step, $"{path}[{index++}]");
                    if (violations.Count >= 256) return;
                }

                return;
            }

            foreach (var field in GetRelevantFields(type))
            {
                Visit(field.GetValue(node), step, $"{path}.{field.Name}");
                if (violations.Count >= 256) return;
            }
        }
    }

    /// <summary>
    ///     Gets fields that should be inspected for forbidden types.
    /// </summary>
    public static FieldInfo[] GetRelevantFields(Type type)
    {
        return FieldCache.GetOrAdd(type,
            t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                             BindingFlags.DeclaredOnly).Where(f => !IsAllowedType(f.FieldType)).ToArray());
    }

    private static bool IsForbiddenType(Type type)
    {
        return ForbiddenTypes.Contains(type) || ForbiddenTypes.Any(forbidden => forbidden.IsAssignableFrom(type));
    }

    private static bool IsAllowedType(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
               type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan) ||
               (Nullable.GetUnderlyingType(type) is { } underlying && IsAllowedType(underlying));
    }
}

/// <summary>
///     Report on generator caching behavior.
/// </summary>
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
    ///     Whether the report indicates correct behavior.
    /// </summary>
    public bool IsCorrect => ForbiddenTypeViolations.Count is 0;

    /// <summary>
    ///     Creates a caching report from two run results.
    /// </summary>
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
    ///     Whether caching was successful.
    /// </summary>
    public bool IsCachedSuccessfully => Modified is 0 && New is 0 && Removed is 0;

    /// <summary>
    ///     Creates a step analysis from run data.
    /// </summary>
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
    public string FormatBreakdown()
    {
        return $"C:{Cached} U:{Unchanged} | M:{Modified} N:{New} R:{Removed} (Total:{TotalOutputs})";
    }

    /// <summary>
    ///     Formats the performance for display.
    /// </summary>
    public string FormatPerformance()
    {
        return $"{ElapsedTimeFirstRun.TotalMilliseconds:F2}ms -> {ElapsedTimeSecondRun.TotalMilliseconds:F2}ms";
    }
}