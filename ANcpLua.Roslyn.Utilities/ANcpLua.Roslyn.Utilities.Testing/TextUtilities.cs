using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Utilities for text processing in test assertions.
/// </summary>
public static partial class TextUtilities
{
    /// <summary>
    ///     Normalizes a file path by converting backslashes to forward slashes.
    /// </summary>
    public static string NormalizePath(string? path)
    {
        return (path ?? string.Empty).Replace('\\', '/').Trim();
    }

    /// <summary>
    ///     Normalizes line endings to Unix style (\n).
    /// </summary>
    public static string NormalizeNewlines(string source)
    {
        return source.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>
    ///     Normalizes whitespace by collapsing multiple spaces into one.
    /// </summary>
    public static string NormalizeWhitespace(string? input)
    {
        return string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : MyRegex().Replace(input, " ").Trim();
    }

    /// <summary>
    ///     Finds the first index where two strings differ.
    /// </summary>
    public static int FirstDiffIndex(string a, string b)
    {
        var length = Math.Min(a.Length, b.Length);
        for (var i = 0; i < length; i++)
            if (a[i] != b[i])
                return i;
        return a.Length != b.Length ? length : -1;
    }

    /// <summary>
    ///     Builds a caret block showing where two strings differ.
    /// </summary>
    public static string BuildCaretBlock(string expectedLine, string actualLine, string indent = "")
    {
        string quotedExpected = $"\"{expectedLine}\"", quotedActual = $"\"{actualLine}\"";
        var index = FirstDiffIndex(quotedExpected, quotedActual);
        if (index < 0) index = Math.Min(quotedExpected.Length, quotedActual.Length);

        const string expectedLabel = "Expected: ";
        const string actualLabel = "Actual:   ";
        StringBuilder sb = new();
        sb.AppendLine($"{indent}{expectedLabel}{quotedExpected}");
        sb.AppendLine($"{indent}{actualLabel}{quotedActual}");
        sb.Append(indent).Append(new string(' ', actualLabel.Length + index)).AppendLine("^");
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    ///     Builds a contextual diff showing the difference location with surrounding context.
    /// </summary>
    public static string BuildContextualDiff(string expected, string actual, int diffIndex, int contextLines = 3)
    {
        StringBuilder sb = new();

        var expectedLines = expected.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var actualLines = actual.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        int line = 0, count = 0;
        for (var i = 0; i < expectedLines.Length; i++)
        {
            var next = count + expectedLines[i].Length + (i < expectedLines.Length - 1 ? 1 : 0);
            if (next > diffIndex || i == expectedLines.Length - 1)
            {
                line = i;
                break;
            }

            count = next;
        }

        var expectedLine = line < expectedLines.Length ? expectedLines[line] : "(end of expected)";
        var actualLine = line < actualLines.Length ? actualLines[line] : "(end of actual)";

        var col = Math.Max(0, diffIndex - count) + 1;
        sb.AppendLine($"Difference at line {line + 1}, character {col}:");
        sb.AppendLine(BuildCaretBlock(expectedLine, actualLine, "  "));
        sb.AppendLine("\nContext in generated file:");
        var start = Math.Max(0, line - contextLines);
        var end = Math.Min(actualLines.Length - 1, line + contextLines);

        sb.AppendLine("------------------------------");
        for (var i = start; i <= end; i++)
        {
            var marker = i == line ? "-> " : "   ";
            if (i < actualLines.Length) sb.AppendLine($"{marker}{actualLines[i]}");
        }

        sb.AppendLine("------------------------------");
        return sb.ToString();
    }

    /// <summary>
    ///     Gets the line at a specific character index in both expected and actual strings.
    /// </summary>
    public static (string ExpectedLine, string ActualLine) GetLineAtIndex(string expected, string actual, int diffIndex)
    {
        var expectedLines = expected.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var actualLines = actual.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        int line = 0, count = 0;
        for (var i = 0; i < expectedLines.Length; i++)
        {
            var next = count + expectedLines[i].Length + (i < expectedLines.Length - 1 ? 1 : 0);
            if (next > diffIndex || i == expectedLines.Length - 1)
            {
                line = i;
                break;
            }

            count = next;
        }

        var expectedLine = line < expectedLines.Length ? expectedLines[line] : "";
        var actualLine = line < actualLines.Length ? actualLines[line] : "";
        return (expectedLine, actualLine);
    }

    /// <summary>
    ///     Builds a one-line caret comparison.
    /// </summary>
    public static string BuildOneLineCaret(string expectedLine, string actualLine)
    {
        var index = FirstDiffIndex(expectedLine, actualLine);
        if (index < 0) index = Math.Min(expectedLine.Length, actualLine.Length);

        StringBuilder sb = new();
        sb.AppendLine($"Expected: {expectedLine}");
        sb.AppendLine($"Actual:   {actualLine}");
        sb.AppendLine(new string('-', "Actual:   ".Length + index) + "^");
        return sb.ToString().TrimEnd();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex();
}

/// <summary>
///     Configuration settings for generator tests.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    ///     Performance tolerance as a percentage (default: 5%).
    /// </summary>
    public const double PerformanceTolerancePercent = 0.05;

    /// <summary>
    ///     Whether to enable JSON reporting in test output.
    /// </summary>
    public const bool EnableJsonReporting = true;

    /// <summary>
    ///     Absolute performance tolerance.
    /// </summary>
    public static readonly TimeSpan PerformanceToleranceAbsolute = TimeSpan.FromMilliseconds(2);

    /// <summary>
    ///     The C# language version to use for tests.
    /// </summary>
    public static LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Preview;

    /// <summary>
    ///     The reference assemblies to use for tests.
    /// </summary>
    public static ReferenceAssemblies ReferenceAssemblies { get; set; } = ReferenceAssemblies.Net.Net90;

    /// <summary>
    ///     Additional references to include in compilations.
    /// </summary>
    public static ImmutableArray<PortableExecutableReference> AdditionalReferences { get; set; } =
        ImmutableArray<PortableExecutableReference>.Empty;

    /// <summary>
    ///     Combines the provided references with additional references.
    /// </summary>
    /// <param name="references">The base references.</param>
    /// <returns>Combined references.</returns>
    public static IReadOnlyList<MetadataReference> CombineReferences(
        ImmutableArray<MetadataReference> references)
    {
        List<MetadataReference> combined = new(references.Length + AdditionalReferences.Length);
        combined.AddRange(references);
        combined.AddRange(AdditionalReferences);
        return combined;
    }
}