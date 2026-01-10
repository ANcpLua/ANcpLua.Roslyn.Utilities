using System.Text.Json;
using ANcpLua.Roslyn.Utilities.Testing.Analysis;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing.Formatting;

/// <summary>
///     Central formatting for test failure reports in the generator testing framework.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Formats generator execution contexts with source, diagnostics, and outputs.</description></item>
///         <item><description>Provides content mismatch diff formatting with contextual highlighting.</description></item>
///         <item><description>Generates comprehensive caching failure reports with JSON payloads.</description></item>
///         <item><description>Delegates step and violation formatting to specialized helper classes.</description></item>
///     </list>
/// </remarks>
/// <seealso cref="ViolationFormatter"/>
/// <seealso cref="StepFormatter"/>
/// <seealso cref="AssertionHelpers"/>
internal static class ReportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    ///     Formats a generator execution context for test failures.
    /// </summary>
    /// <param name="run">The generator driver run result containing diagnostics and generated sources.</param>
    /// <param name="inputSource">The optional input source code that was compiled.</param>
    /// <returns>
    ///     A formatted string containing the input source (with line numbers), diagnostics summary,
    ///     and list of generated outputs with their sizes.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Long source files (over 30 lines) are truncated showing head and tail.</description></item>
    ///         <item><description>Diagnostics are formatted using <see cref="AssertionHelpers.FormatDiagnosticLine"/>.</description></item>
    ///         <item><description>Generated outputs show hint names and character counts.</description></item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GeneratorDriverRunResult"/>
    public static string FormatGeneratorContext(GeneratorDriverRunResult run, string? inputSource)
    {
        var sb = new StringBuilder();

        if (inputSource is { Length: > 0 })
        {
            sb.AppendLine("─── INPUT SOURCE ───");
            sb.AppendLine(FormatSourceWithLineNumbers(inputSource));
            sb.AppendLine();
        }

        var diagnostics = run.Results.SelectMany(static r => r.Diagnostics).ToList();
        sb.AppendLine($"─── DIAGNOSTICS ({diagnostics.Count}) ───");
        if (diagnostics.Count is 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var diag in diagnostics)
                sb.AppendLine(AssertionHelpers.FormatDiagnosticLine(diag));
        }

        sb.AppendLine();

        var outputs = run.Results.SelectMany(static r => r.GeneratedSources).ToList();
        sb.AppendLine($"─── GENERATED OUTPUTS ({outputs.Count}) ───");
        if (outputs.Count is 0)
        {
            sb.AppendLine("  ⚠ No files generated");
        }
        else
        {
            foreach (var output in outputs)
            {
                sb.AppendLine($"  📄 {output.HintName} ({output.SourceText.Length} chars)");
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    ///     Formats a content mismatch failure between expected and actual generated content.
    /// </summary>
    /// <param name="hintName">The hint name of the generated file with mismatched content.</param>
    /// <param name="actual">The actual content that was generated.</param>
    /// <param name="expected">The expected content for comparison.</param>
    /// <param name="exactMatch">
    ///     When <c>true</c>, formats as an exact match failure with character-level diff;
    ///     when <c>false</c>, formats as a contains-match failure.
    /// </param>
    /// <returns>
    ///     A formatted string showing the mismatch details with contextual diff for exact matches
    ///     or a summary for contains matches.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Exact matches use <see cref="TextUtilities"/> for precise diff location.</description></item>
    ///         <item><description>Contains matches show a truncated expected string (100 chars max).</description></item>
    ///         <item><description>Includes a one-line caret indicator for exact match failures.</description></item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="TextUtilities.FirstDiffIndex"/>
    /// <seealso cref="TextUtilities.BuildContextualDiff"/>
    public static string FormatContentFailure(string hintName, string actual, string expected, bool exactMatch)
    {
        StringBuilder sb = new();
        sb.AppendLine();
        sb.AppendLine($"Content mismatch in '{hintName}' ({(exactMatch ? "exact" : "contains")} match)");
        sb.AppendLine();

        if (exactMatch)
        {
            var diffIndex = TextUtilities.FirstDiffIndex(expected, actual);
            if (diffIndex < 0) diffIndex = Math.Min(expected.Length, actual.Length);

            sb.AppendLine(TextUtilities.BuildContextualDiff(expected, actual, diffIndex));
            var (expectedLine, actualLine) = TextUtilities.GetLineAtIndex(expected, actual, diffIndex);
            sb.AppendLine(TextUtilities.BuildOneLineCaret(expectedLine, actualLine));
        }
        else
        {
            sb.AppendLine($"Expected to find: \"{Truncate(expected, 100)}\"");
            sb.AppendLine($"In content of {actual.Length} characters");
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Formats a comprehensive caching failure report including violations, step issues, and a JSON payload.
    /// </summary>
    /// <param name="report">The generator caching report containing analysis results.</param>
    /// <param name="failedCaching">The list of steps that failed caching validation.</param>
    /// <param name="requiredSteps">
    ///     Optional array of step names that were required to be cached.
    ///     Used for highlighting in the pipeline overview.
    /// </param>
    /// <returns>
    ///     A formatted multi-section report containing issues, pipeline overview, and JSON data.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Forbidden type violations are grouped by step and formatted first.</description></item>
    ///         <item><description>Each failed caching step is formatted as a numbered issue.</description></item>
    ///         <item><description>A pipeline overview shows all observable steps with their status.</description></item>
    ///         <item><description>A JSON section provides machine-readable failure data.</description></item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GeneratorCachingReport"/>
    /// <seealso cref="GeneratorStepAnalysis"/>
    /// <seealso cref="ViolationFormatter.FormatIssueBlock"/>
    /// <seealso cref="StepFormatter.FormatStepIssue"/>
    public static string FormatFailureReport(GeneratorCachingReport report,
        IReadOnlyList<GeneratorStepAnalysis> failedCaching, string[]? requiredSteps)
    {
        StringBuilder sb = new();
        var issueNumber = 0;

        if (report.ForbiddenTypeViolations.Count > 0)
        {
            foreach (var group in report.ForbiddenTypeViolations.GroupBy(static v => v.StepName))
            {
                issueNumber++;
                sb.Append(ViolationFormatter.FormatIssueBlock(issueNumber, group));
            }
        }

        foreach (var step in failedCaching)
        {
            issueNumber++;
            sb.Append(StepFormatter.FormatStepIssue(issueNumber, step));
        }

        if (!report.ProducedOutput && issueNumber is 0)
        {
            sb.AppendLine("No meaningful output produced.");
        }

        sb.AppendLine("─── Pipeline Overview ───");
        foreach (var step in report.ObservableSteps.OrderBy(static x => x.StepName))
        {
            sb.AppendLine(StepFormatter.FormatStepLine(step, requiredSteps));
        }

        sb.AppendLine("\n--- JSON ---");
        sb.AppendLine(FormatJson(report, failedCaching));

        return sb.ToString();
    }

    /// <summary>
    ///     Serializes the caching report and failed steps to a JSON format.
    /// </summary>
    /// <param name="report">The caching report to serialize.</param>
    /// <param name="failedCaching">The failed caching steps to include.</param>
    /// <returns>An indented JSON string containing the failure summary.</returns>
    private static string FormatJson(GeneratorCachingReport report, IEnumerable<GeneratorStepAnalysis> failedCaching)
    {
        var payload = new
        {
            generator = report.GeneratorName,
            producedOutput = report.ProducedOutput,
            forbidden =
                report.ForbiddenTypeViolations.Select(static v =>
                    new { v.StepName, Type = v.ForbiddenType.Name, v.Path }),
            failed = failedCaching.Select(static s => new
            {
                s.StepName,
                s.Cached,
                s.Modified,
                s.New,
                s.Removed
            })
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    /// <summary>
    ///     Formats source code with line numbers, truncating long files.
    /// </summary>
    /// <param name="source">The source code to format.</param>
    /// <returns>
    ///     The source with line numbers. Files over 30 lines show the first 10 and last 10 lines
    ///     with an omission indicator in between.
    /// </returns>
    private static string FormatSourceWithLineNumbers(string source)
    {
        var lines = source.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        if (lines.Length > 30)
        {
            var head = lines.Take(10).Select(static (l, i) => $"{i + 1,4} │ {l}");
            var tail = lines.TakeLast(10).Select((l, i) => $"{lines.Length - 10 + i + 1,4} │ {l}");
            return string.Join("\n", head) + $"\n       ... ({lines.Length - 20} lines omitted) ...\n" +
                   string.Join("\n", tail);
        }

        return string.Join("\n", lines.Select(static (l, i) => $"{i + 1,4} │ {l}"));
    }

    /// <summary>
    ///     Truncates a string to a maximum length, adding an ellipsis if truncated.
    /// </summary>
    /// <param name="s">The string to truncate.</param>
    /// <param name="max">The maximum length before truncation.</param>
    /// <returns>The original string if within limit, otherwise truncated with "..." appended.</returns>
    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";
}

/// <summary>
///     Formats forbidden type violations consistently across all test failure reports.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Groups violations by pipeline step name for organized output.</description></item>
///         <item><description>Provides detailed issue blocks with remediation recommendations.</description></item>
///         <item><description>Used by <see cref="ReportFormatter"/> for caching failure reports.</description></item>
///     </list>
/// </remarks>
/// <seealso cref="ForbiddenTypeViolation"/>
/// <seealso cref="ReportFormatter"/>
internal static class ViolationFormatter
{
    /// <summary>
    ///     Formats violations grouped by step name for summary output.
    /// </summary>
    /// <param name="violations">The violations to format.</param>
    /// <returns>
    ///     A multi-line string with violations organized by step, showing the forbidden type name
    ///     and property path for each violation.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Each step group is indented with the step name as a header.</description></item>
    ///         <item><description>Individual violations show the type name and path with a failure marker.</description></item>
    ///     </list>
    /// </remarks>
    public static string FormatGrouped(IEnumerable<ForbiddenTypeViolation> violations)
    {
        StringBuilder sb = new();
        foreach (var group in violations.GroupBy(static v => v.StepName))
        {
            sb.AppendLine($"  Step '{group.Key}':");
            foreach (var v in group)
                sb.AppendLine($"    ✗ {v.ForbiddenType.Name} at {v.Path}");
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Formats a violation group as a numbered issue block for failure reports.
    /// </summary>
    /// <param name="issueNumber">The sequential issue number in the report.</param>
    /// <param name="group">
    ///     The group of violations for a single step, keyed by <see cref="ForbiddenTypeViolation.StepName"/>.
    /// </param>
    /// <returns>
    ///     A formatted issue block with a critical severity header, explanation,
    ///     recommendation, and detailed violation list.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Marked as CRITICAL to indicate severe caching issues.</description></item>
    ///         <item><description>Explains that caching <c>ISymbol</c>, <c>Compilation</c>, or <c>SyntaxNode</c> degrades IDE performance.</description></item>
    ///         <item><description>Recommends using simple, equatable data types (preferably records).</description></item>
    ///         <item><description>Lists each violation with its full type name and property path.</description></item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="ForbiddenTypeViolation"/>
    public static string FormatIssueBlock(int issueNumber, IGrouping<string, ForbiddenTypeViolation> group)
    {
        StringBuilder sb = new();
        sb.AppendLine($"--- ISSUE {issueNumber} (CRITICAL): Forbidden Type Cached in '{group.Key}' ---");
        sb.AppendLine("  Detail: Caching ISymbol/Compilation/SyntaxNode causes IDE performance degradation.");
        sb.AppendLine("  Recommendation: Store only simple, equatable data (prefer 'record').");
        foreach (var violation in group)
            sb.AppendLine($"    - {violation.ForbiddenType.FullName} at {violation.Path}");
        sb.AppendLine();
        return sb.ToString();
    }
}
