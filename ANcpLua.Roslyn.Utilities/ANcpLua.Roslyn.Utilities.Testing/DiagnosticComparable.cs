using System;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Normalizes <see cref="Diagnostic" /> and <see cref="DiagnosticResult" /> for comparison in test assertions.
/// </summary>
/// <remarks>
///     <para>
///         Roslyn's <see cref="Diagnostic" /> type doesn't implement value equality, making direct comparison
///         in tests unreliable. This record normalizes diagnostics to comparable values: ID, severity,
///         location (1-based line/column), and message.
///     </para>
///     <para>
///         Use <see cref="FromDiagnostic" /> to convert actual diagnostics from generator output, and
///         <see cref="FromResult" /> to convert expected <see cref="DiagnosticResult" /> specifications.
///         The <see cref="FindFirstPropertyDifference" /> method provides detailed mismatch information for assertions.
///     </para>
/// </remarks>
/// <param name="Id">The diagnostic ID (e.g., "CS0001", "GEN001").</param>
/// <param name="Severity">The diagnostic severity: <see cref="JSType.Error" />, Warning, Info, or Hidden.</param>
/// <param name="Path">The normalized file path, or empty string if no source location.</param>
/// <param name="Line">The 1-based line number, or 0 if no source location.</param>
/// <param name="Column">The 1-based column number, or 0 if no source location.</param>
/// <param name="Message">The diagnostic message text.</param>
/// <example>
///     <code>
/// // Convert actual diagnostic
/// var actual = DiagnosticComparable.FromDiagnostic(diagnostic);
/// 
/// // Convert expected result
/// var expected = DiagnosticComparable.FromResult(
///     new DiagnosticResult("GEN001", DiagnosticSeverity.Error));
/// 
/// // Find differences
/// var diff = DiagnosticComparable.FindFirstPropertyDifference(expected, actual);
/// </code>
/// </example>
public sealed record DiagnosticComparable(
    string Id,
    DiagnosticSeverity Severity,
    string Path,
    int Line,
    int Column,
    string Message)
{
    /// <summary>
    ///     Creates a <see cref="DiagnosticComparable" /> from a Roslyn <see cref="Diagnostic" />.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to convert.</param>
    /// <returns>A normalized comparable representation.</returns>
    /// <remarks>
    ///     Extracts the mapped line span for accurate source location. Line and column are converted
    ///     to 1-based indexing to match editor conventions. If the diagnostic has no source location,
    ///     <see cref="Line" /> and <see cref="Column" /> are set to 0.
    /// </remarks>
    public static DiagnosticComparable FromDiagnostic(Diagnostic diagnostic)
    {
        var span = diagnostic.Location.GetMappedLineSpan();
        var hasLocation = diagnostic.Location.IsInSource && span.IsValid;
        return new DiagnosticComparable(diagnostic.Id, diagnostic.Severity,
            hasLocation ? TextUtilities.NormalizePath(span.Path) : string.Empty,
            hasLocation ? span.StartLinePosition.Line + 1 : 0, hasLocation ? span.StartLinePosition.Character + 1 : 0,
            diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticComparable" /> from a <see cref="DiagnosticResult" />.
    /// </summary>
    /// <param name="result">The expected diagnostic result to convert.</param>
    /// <returns>A normalized comparable representation.</returns>
    /// <remarks>
    ///     Used to convert test expectations into a format that can be compared with actual diagnostics.
    ///     Handles the case where <see cref="DiagnosticResult.HasLocation" /> is false.
    /// </remarks>
    public static DiagnosticComparable FromResult(DiagnosticResult result)
    {
        var hasLocation = result is { HasLocation: true, Spans.Length: > 0 };
        var span = hasLocation ? result.Spans[0].Span : default;
        var path = hasLocation && span.IsValid ? span.Path : string.Empty;
        var line = hasLocation && span.IsValid ? span.StartLinePosition.Line + 1 : 0;
        var column = hasLocation && span.IsValid ? span.StartLinePosition.Character + 1 : 0;

        return new DiagnosticComparable(result.Id, result.Severity, TextUtilities.NormalizePath(path), line, column,
            result.Message ?? string.Empty);
    }

    /// <summary>
    ///     Formats the diagnostic for human-readable display in test output.
    /// </summary>
    /// <returns>A formatted string like <c>MyFile.cs@10:5 GEN001 (Error): Message text</c>.</returns>
    /// <remarks>
    ///     If no source location exists, omits the file path and position:
    ///     <c>GEN001 (Error): Message text</c>.
    /// </remarks>
    public string Format()
    {
        var path = string.IsNullOrEmpty(Path) ? "<no-file>" : Path;
        var message = TextUtilities.NormalizeWhitespace(Message);
        return Line > 0 ? $"{path}@{Line}:{Column} {Id} ({Severity}): {message}" : $"{Id} ({Severity}): {message}";
    }

    /// <summary>
    ///     Finds the first property that differs between two diagnostics, for detailed assertion messages.
    /// </summary>
    /// <param name="expected">The expected diagnostic.</param>
    /// <param name="actual">The actual diagnostic.</param>
    /// <returns>
    ///     A tuple of (PropertyName, ExpectedValue, ActualValue) if a difference is found;
    ///     <c>null</c> if the diagnostics match.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Properties are compared in order: Severity, Id, Path, Line, Column, Message.
    ///         The first mismatch is returned immediately.
    ///     </para>
    ///     <para>
    ///         Location properties (Path, Line, Column) are only compared if the expected diagnostic
    ///         has a location (<see cref="Line" /> &gt; 0). Message is only compared if the expected
    ///         message is non-empty, allowing tests to match just ID and severity.
    ///     </para>
    /// </remarks>
    public static (string Property, string Expected, string Actual)? FindFirstPropertyDifference(
        DiagnosticComparable expected, DiagnosticComparable actual)
    {
        if (expected.Severity != actual.Severity)
            return ("Severity", expected.Severity.ToString(), actual.Severity.ToString());
        if (!string.Equals(expected.Id, actual.Id, StringComparison.Ordinal)) return ("Id", expected.Id, actual.Id);

        if (expected.Line > 0)
        {
            if (!string.Equals(expected.Path, actual.Path, StringComparison.OrdinalIgnoreCase))
                return ("FilePath", expected.Path, actual.Path);
            if (expected.Line != actual.Line) return ("Line", expected.Line.ToString(), actual.Line.ToString());
            if (expected.Column != actual.Column)
                return ("Column", expected.Column.ToString(), actual.Column.ToString());
        }

        // Skip message comparison if expected message is empty (allows testing just ID and severity)
        var expectedMessage = TextUtilities.NormalizeWhitespace(expected.Message);
        switch (string.IsNullOrEmpty(expectedMessage))
        {
            case false:
            {
                var actualMessage = TextUtilities.NormalizeWhitespace(actual.Message);
                if (!string.Equals(expectedMessage, actualMessage, StringComparison.Ordinal))
                    return ("Message", expectedMessage, actualMessage);
                break;
            }
        }

        return null;
    }
}