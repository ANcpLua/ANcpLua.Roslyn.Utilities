using AnalysisStep = ANcpLua.Roslyn.Utilities.Testing.Analysis.GeneratorStepAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing.Formatting;

/// <summary>
///     Formats generator step information for reports.
/// </summary>
internal static class StepFormatter
{
    /// <summary>
    ///     Formats a step breakdown.
    /// </summary>
    public static string FormatBreakdown(GeneratorStepAnalysis step) =>
        $"C:{step.Cached} M:{step.Modified} N:{step.New} R:{step.Removed}";

    /// <summary>
    ///     Formats a step breakdown (Analysis namespace overload).
    /// </summary>
    public static string FormatBreakdown(AnalysisStep step) =>
        $"C:{step.Cached} M:{step.Modified} N:{step.New} R:{step.Removed}";

    /// <summary>
    ///     Formats a step issue with a number.
    /// </summary>
    public static string FormatStepIssue(int issueNumber, GeneratorStepAnalysis step)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Issue #{issueNumber}: Caching failed for step '{step.StepName}'");
        sb.AppendLine($"  Breakdown: {step.FormatBreakdown()}");
        return sb.ToString();
    }

    /// <summary>
    ///     Formats a step line for the pipeline overview.
    /// </summary>
    public static string FormatStepLine(GeneratorStepAnalysis step, string[]? requiredSteps)
    {
        var required = requiredSteps?.Contains(step.StepName) == true ? "[required]" : "";
        var status = step.IsCachedSuccessfully ? "✓" : "✗";
        return $"  {status} {step.StepName} {required} - {step.FormatBreakdown()}".TrimEnd();
    }
}
