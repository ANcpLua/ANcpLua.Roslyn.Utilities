using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Formats generator execution results into a detailed diagnostic report for test failures.
///     Produces a "State of the World" view including Input Source, Diagnostics, Generated Files, and Pipeline Steps.
/// </summary>
internal static class GeneratorDiagnosticFormatter
{
    public static string Format(GeneratorDriverRunResult run, string? inputSource)
    {
        var sb = new StringBuilder();

        // 1. INPUT SOURCE
        if (!string.IsNullOrEmpty(inputSource))
        {
            sb.AppendLine("─── 1. INPUT SOURCE ───");
            sb.AppendLine(FormatSourceWithLineNumbers(inputSource!));
            sb.AppendLine();
        }

        // 2. DIAGNOSTICS
        var diagnostics = run.Results.SelectMany(r => r.Diagnostics).ToList();
        sb.AppendLine($"─── 2. DIAGNOSTICS ({diagnostics.Count}) ───");
        if (diagnostics.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var diag in diagnostics)
            {
                var icon = diag.Severity == DiagnosticSeverity.Error ? "✗" : "⚠";
                var loc = diag.Location.IsInSource
                    ? $" @{diag.Location.GetMappedLineSpan().StartLinePosition.Line + 1}:{diag.Location.GetMappedLineSpan().StartLinePosition.Character + 1}"
                    : "";
                sb.AppendLine($"  {icon} {diag.Id}{loc}: {diag.GetMessage()}");
            }
        }

        sb.AppendLine();

        // 3. GENERATED OUTPUTS
        var outputs = run.Results.SelectMany(r => r.GeneratedSources).ToList();
        sb.AppendLine($"─── 3. GENERATED OUTPUTS ({outputs.Count}) ───");
        if (outputs.Count == 0)
        {
            sb.AppendLine("  ⚠ No files generated");
        }
        else
        {
            foreach (var output in outputs)
            {
                sb.AppendLine($"  📄 {output.HintName} ({output.SourceText.Length} chars, {output.SourceText.Lines.Count} lines)");
            }
        }

        sb.AppendLine();

        // 4. PIPELINE TRACE
        var steps = run.Results.SelectMany(r => r.TrackedSteps).OrderBy(k => k.Key).ToList();
        if (steps.Count > 0)
        {
            sb.AppendLine("─── 4. PIPELINE TRACE ───");
            foreach (var (name, runs) in steps)
            {
                var outputs2 = runs.SelectMany(r => r.Outputs).ToList();
                var details = outputs2
                    .GroupBy(o => o.Reason)
                    .Select(g => $"{g.Key}={g.Count()}")
                    .OrderBy(x => x);

                var summary = string.Join(", ", details);
                if (string.IsNullOrEmpty(summary)) summary = "No Output";

                var isInfra = GeneratorStepAnalyzer.IsInfrastructureStep(name);
                var icon = isInfra ? "⚙" : outputs2.Count > 0 ? "✓" : "✗";
                sb.AppendLine($"  {icon} {name}: [{summary}]");
            }
        }
        else
        {
            sb.AppendLine("─── 4. PIPELINE TRACE ───");
            sb.AppendLine("  (step tracking disabled - enable with trackSteps: true)");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatSourceWithLineNumbers(string source)
    {
        var lines = source.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        // Truncate if too large to avoid flooding the console
        if (lines.Length > 30)
        {
            var head = lines.Take(10).Select((l, i) => $"{i + 1,4} │ {l}");
            var tail = lines.TakeLast(10).Select((l, i) => $"{lines.Length - 10 + i + 1,4} │ {l}");
            return string.Join("\n", head) + $"\n       ... ({lines.Length - 20} lines omitted) ...\n" + string.Join("\n", tail);
        }

        return string.Join("\n", lines.Select((l, i) => $"{i + 1,4} │ {l}"));
    }
}
