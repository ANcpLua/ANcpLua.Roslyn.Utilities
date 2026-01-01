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
/// </remarks>
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
    ///     The generator name.
    /// </summary>
    public string GeneratorName { get; }

    /// <summary>
    ///     Observable (user) pipeline steps.
    /// </summary>
    public IReadOnlyList<GeneratorStepAnalysis> ObservableSteps { get; }

    /// <summary>
    ///     Forbidden type violations found.
    /// </summary>
    public IReadOnlyList<ForbiddenTypeViolation> ForbiddenTypeViolations { get; }

    /// <summary>
    ///     Whether the generator produced output.
    /// </summary>
    public bool ProducedOutput { get; }

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
