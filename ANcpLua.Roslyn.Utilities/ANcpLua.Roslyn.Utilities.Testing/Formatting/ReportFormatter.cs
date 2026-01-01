using System.Text.Json;
using ANcpLua.Roslyn.Utilities.Testing.Analysis;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing.Formatting;

/// <summary>
///     Central formatting for test failure reports.
/// </summary>
internal static class ReportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    ///     Formats a generator execution context for test failures.
    /// </summary>
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
    ///     Formats a content mismatch failure.
    /// </summary>
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
    ///     Formats a caching failure report.
    /// </summary>
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

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";
}

/// <summary>
///     Formats forbidden type violations consistently across all reports.
/// </summary>
internal static class ViolationFormatter
{
    /// <summary>
    ///     Formats violations grouped by step name.
    /// </summary>
    /// <param name="violations">The violations to format.</param>
    /// <returns>Multi-line grouped format.</returns>
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
    ///     Formats a violation group as an issue block for failure reports.
    /// </summary>
    /// <param name="issueNumber">The issue number.</param>
    /// <param name="group">The group of violations for a step.</param>
    /// <returns>Formatted issue block.</returns>
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
