using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing.Formatting;

/// <summary>
///     Reusable helpers for assertion messages.
/// </summary>
internal static class AssertionHelpers
{
    #region List Formatting

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
    ///     Gets hint names from generated sources as a formatted list.
    /// </summary>
    public static string FormatSourceList(IEnumerable<GeneratedSourceResult> sources) =>
        FormatList(sources, static s => s.HintName);

    /// <summary>
    ///     Gets diagnostic IDs as a formatted list.
    /// </summary>
    public static string FormatDiagnosticIds(IEnumerable<Diagnostic> diagnostics) =>
        FormatList(diagnostics.Select(static d => d.Id).Distinct());

    #endregion

    #region Diagnostic Formatting

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

    #endregion

    #region Assertion Messages

    /// <summary>
    ///     Builds "Expected {what} but not found. Available: [list]" message.
    /// </summary>
    public static string NotFound(string what, string available) =>
        $"Expected {what} but it was not found. Available: {available}";

    /// <summary>
    ///     Builds "Expected {what} but found {count}" message.
    /// </summary>
    public static string UnexpectedCount(string what, int count, string details) =>
        $"Expected {what}, but found {count}:\n{details}";

    /// <summary>
    ///     Builds "Expected no {what} but found one" message.
    /// </summary>
    public static string ShouldNotExist(string what) =>
        $"Expected no {what} but one was found.";

    #endregion

    #region Step Formatting

    /// <summary>
    ///     Formats failed caching steps as a list.
    /// </summary>
    public static string FormatFailedSteps(IEnumerable<GeneratorStepAnalysis> steps) =>
        string.Join("\n", steps.Select(static s => $"  ✗ {s.StepName}: {s.FormatBreakdown()}"));

    #endregion
}
