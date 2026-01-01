using ANcpLua.Roslyn.Utilities.Testing.Analysis;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing.Formatting;

/// <summary>
///     Reusable helpers for assertion messages.
/// </summary>
internal static class AssertionHelpers
{
    /// <summary>
    ///     Formats a list as "[item1, item2]" or "none" if empty.
    /// </summary>
    public static string FormatList<T>(IEnumerable<T> items, Func<T, string> selector) =>
        FormatList(items.Select(selector));

    /// <summary>
    ///     Formats a list as "[item1, item2]" or "none" if empty.
    /// </summary>
    public static string FormatList(IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.Count > 0 ? $"[{string.Join(", ", list)}]" : "none";
    }

    /// <summary>
    ///     Gets hint names from generated files as a formatted list.
    /// </summary>
    public static string FormatFileList(IEnumerable<GeneratedFile> files) =>
        FormatList(files, static f => f.HintName);

    /// <summary>
    ///     Gets diagnostic IDs as a formatted list.
    /// </summary>
    public static string FormatDiagnosticIds(IEnumerable<Diagnostic> diagnostics) =>
        FormatList(diagnostics.Select(static d => d.Id).Distinct());

    /// <summary>
    ///     Formats a diagnostic as "  ✗ ID @line:col: message" or "  ⚠ ID: message".
    /// </summary>
    public static string FormatDiagnosticLine(Diagnostic d)
    {
        var icon = d.Severity == DiagnosticSeverity.Error ? "✗" : "⚠";
        var loc = d.Location.IsInSource
            ? $" @{d.Location.GetMappedLineSpan().StartLinePosition.Line + 1}:{d.Location.GetMappedLineSpan().StartLinePosition.Character + 1}"
            : "";
        return $"  {icon} {d.Id}{loc}: {d.GetMessage()}";
    }

    /// <summary>
    ///     Formats multiple diagnostics as a newline-separated list.
    /// </summary>
    public static string FormatDiagnosticList(IEnumerable<Diagnostic> diagnostics) =>
        string.Join("\n", diagnostics.Select(FormatDiagnosticLine));

    /// <summary>
    ///     Formats errors only (with ✗ prefix).
    /// </summary>
    public static string FormatErrorList(IEnumerable<Diagnostic> diagnostics) =>
        string.Join("\n", diagnostics
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .Select(static d => $"  ✗ {d.Id}: {d.GetMessage()}"));

    /// <summary>
    ///     Formats failed caching steps as a list.
    /// </summary>
    public static string FormatFailedSteps(IEnumerable<GeneratorStepAnalysis> steps) =>
        string.Join("\n", steps.Select(static s => $"  ✗ {s.StepName}: {s.FormatBreakdown()}"));
}

/// <summary>
///     Formats generator step analysis data.
/// </summary>
internal static class StepFormatter
{
    public static string FormatBreakdown(GeneratorStepAnalysis step) =>
        $"C:{step.Cached} U:{step.Unchanged} | M:{step.Modified} N:{step.New} R:{step.Removed}";

    public static string FormatStepLine(GeneratorStepAnalysis step, string[]? requiredSteps = null)
    {
        var tracked = requiredSteps?.Contains(step.StepName) == true ? "[Tracked]" : "";
        var forbidden = step.HasForbiddenTypes ? "[!]" : "";
        var icon = step.IsCachedSuccessfully ? "[OK]" : "[FAIL]";
        return $"  {icon} {step.StepName} {tracked}{forbidden} | {FormatBreakdown(step)}";
    }

    public static string FormatStepIssue(int issueNumber, GeneratorStepAnalysis step)
    {
        StringBuilder sb = new();
        sb.AppendLine($"--- ISSUE {issueNumber}: Step Not Cached '{step.StepName}' ---");
        sb.AppendLine($"  Breakdown: {FormatBreakdown(step)}");
        sb.AppendLine(step.HasForbiddenTypes
            ? "  Cause: Forbidden Roslyn types cached."
            : "  Fix: Ensure output model has value equality.");
        sb.AppendLine();
        return sb.ToString();
    }
}
