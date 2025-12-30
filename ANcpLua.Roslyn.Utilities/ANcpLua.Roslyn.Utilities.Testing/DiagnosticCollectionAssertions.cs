using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Extension methods for asserting on diagnostic collections.
/// </summary>
/// <remarks>
///     These extensions allow comparing actual Roslyn <see cref="Diagnostic" /> instances
///     against expected <see cref="DiagnosticResult" /> specifications.
/// </remarks>
public static class DiagnosticAssertionExtensions
{
    /// <summary>
    ///     Asserts that a collection of diagnostics is equivalent to the expected diagnostic results.
    /// </summary>
    /// <param name="assertions">The diagnostic collection to assert on.</param>
    /// <param name="expected">The expected diagnostic results to match against.</param>
    /// <param name="because">A formatted phrase explaining why the assertion should be satisfied.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
    /// <remarks>
    ///     <para>
    ///         Diagnostics are compared by:
    ///         <list type="number">
    ///             <item>
    ///                 <description>ID (e.g., "CS0001")</description>
    ///             </item>
    ///             <item>
    ///                 <description>Severity</description>
    ///             </item>
    ///             <item>
    ///                 <description>Location (file path, line, column) if specified</description>
    ///             </item>
    ///             <item>
    ///                 <description>Message content if specified</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Order matters: diagnostics are sorted by location then ID before comparison.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var diagnostics = result.Results.SelectMany(r => r.Diagnostics);
    /// diagnostics.BeEquivalentToDiagnostics([
    ///     new DiagnosticResult("GEN001", DiagnosticSeverity.Error)
    ///         .WithSpan(10, 1, 10, 20)
    ///         .WithMessage("Missing required attribute")
    /// ]);
    /// </code>
    /// </example>
    public static AndConstraint<DiagnosticCollectionAssertions> BeEquivalentToDiagnostics(
        this IEnumerable<Diagnostic> assertions, IEnumerable<DiagnosticResult>? expected, string because = "",
        params object[] becauseArgs)
    {
        var diagnosticList = assertions.ToList();
        var chain = AssertionChain.GetOrCreate();
        DiagnosticCollectionAssertions assertion = new(diagnosticList, chain);
        return assertion.BeEquivalentToDiagnostics(expected, because, becauseArgs);
    }
}

/// <summary>
///     Contains assertions for collections of <see cref="Diagnostic" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This class provides detailed comparison of diagnostic collections, with clear error messages
///         that show exactly which diagnostics differ and how.
///     </para>
/// </remarks>
public sealed class
    DiagnosticCollectionAssertions : ReferenceTypeAssertions<IEnumerable<Diagnostic>, DiagnosticCollectionAssertions>
{
    private readonly AssertionChain _chain;
    private readonly IReadOnlyList<Diagnostic> _subject;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosticCollectionAssertions" /> class.
    /// </summary>
    /// <param name="subject">The diagnostic collection to assert on.</param>
    /// <param name="chain">The assertion chain for error reporting.</param>
    public DiagnosticCollectionAssertions(IReadOnlyList<Diagnostic> subject, AssertionChain chain) : base(subject,
        chain)
    {
        _subject = subject;
        _chain = chain;
    }

    /// <inheritdoc />
    protected override string Identifier => "Diagnostics";

    /// <summary>
    ///     Asserts that the diagnostic collection is equivalent to the expected results.
    /// </summary>
    /// <param name="expected">The expected diagnostic results.</param>
    /// <param name="because">A formatted phrase explaining why the assertion should be satisfied.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
    public AndConstraint<DiagnosticCollectionAssertions> BeEquivalentToDiagnostics(
        IEnumerable<DiagnosticResult>? expected, string because = "", params object[] becauseArgs)
    {
        var actualDiagnostics = _subject.Select(DiagnosticSnapshot.FromDiagnostic).ToList();
        var expectedDiagnostics = (expected ?? [])
            .Select(DiagnosticSnapshot.FromResult).ToList();

        actualDiagnostics = OrderDiagnostics(actualDiagnostics);
        expectedDiagnostics = OrderDiagnostics(expectedDiagnostics);

        _chain.BecauseOf(because, becauseArgs).WithExpectation(
            "Expected diagnostic collection to have the same count, ",
            ch => ch.ForCondition(actualDiagnostics.Count == expectedDiagnostics.Count).FailWith(
                "but found {0} actual vs {1} expected.", actualDiagnostics.Count, expectedDiagnostics.Count));

        if (actualDiagnostics.Count != expectedDiagnostics.Count)
        {
            var summary = BuildCountMismatchReport(actualDiagnostics, expectedDiagnostics);
            _chain.FailWith("{0}", summary);
            return new AndConstraint<DiagnosticCollectionAssertions>(this);
        }

        List<(int Index, string Property, string Expected, string Actual)> differences = [];
        for (var i = 0; i < expectedDiagnostics.Count; i++)
        {
            var difference =
                DiagnosticSnapshot.FindFirstPropertyDifference(expectedDiagnostics[i], actualDiagnostics[i]);
            if (difference is not null)
                differences.Add((i, difference.Value.Property, difference.Value.Expected, difference.Value.Actual));
        }

        if (differences.Count > 0)
        {
            var comprehensiveReport = BuildAllDifferencesReport(differences, expectedDiagnostics, actualDiagnostics);
            _chain.ForCondition(false).BecauseOf(because, becauseArgs).FailWith("{0}", comprehensiveReport);
        }

        return new AndConstraint<DiagnosticCollectionAssertions>(this);

        static List<DiagnosticSnapshot> OrderDiagnostics(IEnumerable<DiagnosticSnapshot> diagnostics)
        {
            return diagnostics.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Line)
                .ThenBy(x => x.Column)
                .ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
        }

        static string BuildAllDifferencesReport(
            List<(int Index, string Property, string Expected, string Actual)> differences,
            List<DiagnosticSnapshot> expectedDiagnostics, List<DiagnosticSnapshot> actualDiagnostics)
        {
            StringBuilder sb = new();
            sb.AppendLine($"Found {differences.Count} diagnostic differences:");
            sb.AppendLine();

            foreach (var (index, property, expectedVal, actualVal) in differences)
            {
                sb.AppendLine(
                    $"--- DIFFERENCE {differences.IndexOf((index, property, expectedVal, actualVal)) + 1} at Index {index} ---");
                sb.AppendLine($"Property: {property}");
                sb.AppendLine($"Expected: '{expectedVal}'");
                sb.AppendLine($"Actual:   '{actualVal}'");
                sb.AppendLine();

                var caretBlock = TextUtilities.BuildCaretBlock(expectedDiagnostics[index].Format(),
                    actualDiagnostics[index].Format());
                sb.AppendLine("Contextual comparison:");
                foreach (var line in caretBlock.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                    sb.Append("  ").AppendLine(line);
                sb.AppendLine();
            }

            sb.AppendLine("=== FULL DIAGNOSTIC COMPARISON ===");
            sb.AppendLine("Expected Diagnostics:");
            if (expectedDiagnostics.Count is 0) sb.AppendLine("  (None)");
            for (var i = 0; i < expectedDiagnostics.Count; i++)
            {
                var marker = differences.Any(d => d.Index == i) ? "[X]" : "[OK]";
                sb.AppendLine($"  {marker} [{i}] {expectedDiagnostics[i].Format()}");
            }

            sb.AppendLine("\nActual Diagnostics:");
            if (actualDiagnostics.Count is 0) sb.AppendLine("  (None)");
            for (var i = 0; i < actualDiagnostics.Count; i++)
            {
                var marker = differences.Any(d => d.Index == i) ? "[X]" : "[OK]";
                sb.AppendLine($"  {marker} [{i}] {actualDiagnostics[i].Format()}");
            }

            return sb.ToString();
        }

        static string BuildCountMismatchReport(List<DiagnosticSnapshot> actual,
            List<DiagnosticSnapshot> expectedList)
        {
            StringBuilder sb = new();
            sb.AppendLine("Diagnostic count mismatch.");
            sb.AppendLine("\nActual Diagnostics:");
            if (actual.Count is 0) sb.AppendLine("  (None)");
            foreach (var diagnostic in actual) sb.AppendLine($"  - {diagnostic.Format()}");
            sb.AppendLine("\nExpected Diagnostics:");
            if (expectedList.Count is 0) sb.AppendLine("  (None)");
            foreach (var diagnostic in expectedList) sb.AppendLine($"  - {diagnostic.Format()}");
            return sb.ToString();
        }
    }
}