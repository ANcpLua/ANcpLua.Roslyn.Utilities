using ANcpLua.Roslyn.Utilities.Testing.Analysis;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing.Formatting;

/// <summary>
///     Provides reusable helper methods for formatting assertion messages in generator test results.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>All formatting methods produce consistent, human-readable output for test assertions.</description>
///         </item>
///         <item>
///             <description>List formatting uses bracket notation with comma separation, or "none" for empty collections.</description>
///         </item>
///         <item>
///             <description>Diagnostic formatting uses visual icons to distinguish errors from warnings.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="StepFormatter" />
/// <seealso cref="GeneratorStepAnalysis" />
internal static class AssertionHelpers
{
    /// <summary>
    ///     Formats a collection of items as a bracketed, comma-separated list using a custom selector.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="items">The collection of items to format.</param>
    /// <param name="selector">A function to extract the string representation from each item.</param>
    /// <returns>
    ///     A string in the format "[item1, item2, ...]" or "none" if the collection is empty.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Projects each item using the selector before formatting.</description>
    ///         </item>
    ///         <item>
    ///             <description>Delegates to <see cref="FormatList(IEnumerable{string})" /> after projection.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="FormatList(IEnumerable{string})" />
    public static string FormatList<T>(IEnumerable<T> items, Func<T, string> selector) => FormatList(items.Select(selector));

    /// <summary>
    ///     Formats a collection of strings as a bracketed, comma-separated list.
    /// </summary>
    /// <param name="items">The collection of strings to format.</param>
    /// <returns>
    ///     A string in the format "[item1, item2, ...]" or "none" if the collection is empty.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Returns "none" for empty collections to clearly indicate absence of items.</description>
    ///         </item>
    ///         <item>
    ///             <description>Uses bracket notation for visual clarity in assertion messages.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static string FormatList(IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.Count > 0 ? $"[{string.Join(", ", list)}]" : "none";
    }

    /// <summary>
    ///     Formats a collection of generated files as a list of their hint names.
    /// </summary>
    /// <param name="files">The collection of generated files to format.</param>
    /// <returns>
    ///     A bracketed, comma-separated list of hint names, or "none" if no files exist.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Extracts the <see cref="GeneratedFile.HintName" /> from each file.</description>
    ///         </item>
    ///         <item>
    ///             <description>Useful for displaying which files were generated in assertion messages.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GeneratedFile" />
    public static string FormatFileList(IEnumerable<GeneratedFile> files)
    {
        return FormatList(files, static f => f.HintName);
    }

    /// <summary>
    ///     Formats a collection of diagnostics as a list of distinct diagnostic IDs.
    /// </summary>
    /// <param name="diagnostics">The collection of diagnostics to format.</param>
    /// <returns>
    ///     A bracketed, comma-separated list of unique diagnostic IDs, or "none" if no diagnostics exist.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Removes duplicate IDs for cleaner output.</description>
    ///         </item>
    ///         <item>
    ///             <description>Useful for summarizing which diagnostic rules were triggered.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="Diagnostic" />
    public static string FormatDiagnosticIds(IEnumerable<Diagnostic> diagnostics)
    {
        return FormatList(diagnostics.Select(static d => d.Id).Distinct());
    }

    /// <summary>
    ///     Formats a single diagnostic as a detailed line with icon, ID, location, and message.
    /// </summary>
    /// <param name="d">The diagnostic to format.</param>
    /// <returns>
    ///     A formatted string in the format "  [icon] ID @line:col: message" for source diagnostics,
    ///     or "  [icon] ID: message" for non-source diagnostics.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Uses a cross mark icon for errors and a warning icon for other severities.</description>
    ///         </item>
    ///         <item>
    ///             <description>Includes 1-based line and column numbers when the diagnostic has a source location.</description>
    ///         </item>
    ///         <item>
    ///             <description>Indents with two spaces for alignment in multi-line output.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="FormatDiagnosticList" />
    /// <seealso cref="FormatErrorList" />
    public static string FormatDiagnosticLine(Diagnostic d)
    {
        var icon = d.Severity == DiagnosticSeverity.Error ? "✗" : "⚠";
        var loc = d.Location.IsInSource
            ? $" @{d.Location.GetMappedLineSpan().StartLinePosition.Line + 1}:{d.Location.GetMappedLineSpan().StartLinePosition.Character + 1}"
            : "";
        return $"  {icon} {d.Id}{loc}: {d.GetMessage()}";
    }

    /// <summary>
    ///     Formats multiple diagnostics as a newline-separated list of detailed lines.
    /// </summary>
    /// <param name="diagnostics">The collection of diagnostics to format.</param>
    /// <returns>
    ///     A multi-line string with each diagnostic on its own line, formatted by <see cref="FormatDiagnosticLine" />.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Includes both errors and warnings with their respective icons.</description>
    ///         </item>
    ///         <item>
    ///             <description>Preserves the order of diagnostics from the input collection.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="FormatDiagnosticLine" />
    /// <seealso cref="FormatErrorList" />
    public static string FormatDiagnosticList(IEnumerable<Diagnostic> diagnostics) => string.Join("\n", diagnostics.Select(FormatDiagnosticLine));

    /// <summary>
    ///     Formats only error-severity diagnostics as a newline-separated list.
    /// </summary>
    /// <param name="diagnostics">The collection of diagnostics to filter and format.</param>
    /// <returns>
    ///     A multi-line string containing only errors, each prefixed with a cross mark icon.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Filters to include only <see cref="DiagnosticSeverity.Error" /> diagnostics.</description>
    ///         </item>
    ///         <item>
    ///             <description>Uses simplified formatting without location information.</description>
    ///         </item>
    ///         <item>
    ///             <description>Useful when only compilation errors are relevant.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="FormatDiagnosticList" />
    /// <seealso cref="DiagnosticSeverity" />
    public static string FormatErrorList(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join("\n", diagnostics
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .Select(static d => $"  ✗ {d.Id}: {d.GetMessage()}"));
    }

    /// <summary>
    ///     Formats generator steps that failed caching validation as a detailed list.
    /// </summary>
    /// <param name="steps">The collection of failed generator step analyses to format.</param>
    /// <returns>
    ///     A multi-line string with each failed step showing its name and caching breakdown.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Each step is prefixed with a cross mark icon to indicate failure.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Includes the step's caching breakdown via
    ///                 <see cref="GeneratorStepAnalysisExtensions.FormatBreakdown" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Useful for diagnosing which pipeline steps are not caching correctly.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GeneratorStepAnalysis" />
    /// <seealso cref="StepFormatter" />
    public static string FormatFailedSteps(IEnumerable<GeneratorStepAnalysis> steps)
    {
        return string.Join("\n", steps.Select(static s =>
        {
            var breakdown = s.FormatBreakdown();
            var hint = s.Modified > 0
                ? " (output equality broken — model lacks IEquatable<T>)"
                : s.New > 0 ? " (new outputs appeared)" : " (outputs removed)";
            return $"  ✗ {s.StepName}: {breakdown}{hint}";
        }));
    }
}

/// <summary>
///     Provides formatting utilities for <see cref="GeneratorStepAnalysis" /> data in test reports.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Produces compact breakdown strings showing cached vs. modified counts.</description>
///         </item>
///         <item>
///             <description>Formats step lines with status icons and optional annotations.</description>
///         </item>
///         <item>
///             <description>Generates detailed issue reports for failed caching steps.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="GeneratorStepAnalysis" />
/// <seealso cref="AssertionHelpers" />
internal static class StepFormatter
{
    /// <summary>
    ///     Formats a step's caching statistics as a compact breakdown string.
    /// </summary>
    /// <param name="step">The generator step analysis containing caching statistics.</param>
    /// <returns>
    ///     A string in the format "C:[cached] U:[unchanged] | M:[modified] N:[new] R:[removed]".
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>C (Cached) and U (Unchanged) indicate successful caching.</description>
    ///         </item>
    ///         <item>
    ///             <description>M (Modified), N (New), and R (Removed) indicate cache invalidation.</description>
    ///         </item>
    ///         <item>
    ///             <description>The pipe separator visually distinguishes good outcomes from problematic ones.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GeneratorStepAnalysis" />
    public static string FormatBreakdown(GeneratorStepAnalysis step) => $"C:{step.Cached} U:{step.Unchanged} | M:{step.Modified} N:{step.New} R:{step.Removed}";

    /// <summary>
    ///     Formats a single step as a detailed status line with annotations.
    /// </summary>
    /// <param name="step">The generator step analysis to format.</param>
    /// <param name="requiredSteps">
    ///     Optional array of step names that are being explicitly tracked. Steps matching these names
    ///     will be annotated with "[Tracked]".
    /// </param>
    /// <returns>
    ///     A formatted string with status icon, step name, annotations, and caching breakdown.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Prefixes with "[OK]" for successfully cached steps or "[FAIL]" for failures.</description>
    ///         </item>
    ///         <item>
    ///             <description>Adds "[Tracked]" annotation for steps in the requiredSteps list.</description>
    ///         </item>
    ///         <item>
    ///             <description>Adds "[!]" annotation for steps with forbidden type violations.</description>
    ///         </item>
    ///         <item>
    ///             <description>Includes the full caching breakdown at the end of the line.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="FormatBreakdown" />
    /// <seealso cref="GeneratorStepAnalysis.HasForbiddenTypes" />
    /// <seealso cref="GeneratorStepAnalysis.IsCachedSuccessfully" />
    public static string FormatStepLine(GeneratorStepAnalysis step, string[]? requiredSteps = null)
    {
        var tracked = requiredSteps?.Contains(step.StepName) == true ? "[Tracked]" : "";
        var forbidden = step.HasForbiddenTypes ? "[!]" : "";
        var icon = step.IsCachedSuccessfully ? (step.IsTrulyCached ? "[OK]" : "[OK*]") : "[FAIL]";
        return $"  {icon} {step.StepName} {tracked}{forbidden} | {FormatBreakdown(step)}";
    }

    /// <summary>
    ///     Formats a detailed issue report for a step that failed caching validation.
    /// </summary>
    /// <param name="issueNumber">The sequential issue number for ordering in multi-issue reports.</param>
    /// <param name="step">The generator step analysis that failed caching.</param>
    /// <returns>
    ///     A multi-line string containing the issue header, breakdown, cause, and suggested fix.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Includes a numbered header for easy reference in reports with multiple issues.</description>
    ///         </item>
    ///         <item>
    ///             <description>Shows the full caching breakdown for diagnostic purposes.</description>
    ///         </item>
    ///         <item>
    ///             <description>Provides cause analysis distinguishing forbidden types from equality issues.</description>
    ///         </item>
    ///         <item>
    ///             <description>Suggests appropriate fixes based on the detected cause.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="FormatBreakdown" />
    /// <seealso cref="GeneratorStepAnalysis.HasForbiddenTypes" />
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
