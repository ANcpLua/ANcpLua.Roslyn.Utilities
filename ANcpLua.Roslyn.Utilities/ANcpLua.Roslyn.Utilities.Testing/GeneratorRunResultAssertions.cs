using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Contains assertions for <see cref="GeneratorDriverRunResult" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This is the primary assertion class for validating generator output. It provides methods to:
///         <list type="bullet">
///             <item>
///                 <description>Verify that specific files were generated</description>
///             </item>
///             <item>
///                 <description>Assert that no diagnostics were reported</description>
///             </item>
///             <item>
///                 <description>Check that no forbidden Roslyn types are cached in outputs</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         On failure, comprehensive context is automatically included showing input source,
///         diagnostics, generated files, and pipeline step states to help diagnose issues quickly.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// var result = driver.GetRunResult();
/// result.Should(inputSource).HaveGeneratedSource("Person.Builder.g.cs")
///       .Which.Should().HaveContent("public class PersonBuilder");
/// result.Should().HaveNoDiagnostics();
/// </code>
/// </example>
public sealed class
    GeneratorRunResultAssertions : ReferenceTypeAssertions<GeneratorDriverRunResult, GeneratorRunResultAssertions>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratorRunResultAssertions" /> class.
    /// </summary>
    /// <param name="subject">The generator run result to assert on.</param>
    /// <param name="chain">The assertion chain for error reporting.</param>
    /// <param name="originalSource">Optional original source code to include in failure reports.</param>
    public GeneratorRunResultAssertions(GeneratorDriverRunResult subject, AssertionChain chain,
        string? originalSource = null) : base(subject, chain)
    {
        // Automatically attach the rich context report to the chain.
        // This report is lazy-evaluated and ONLY generated if an assertion fails.
        if (subject is not null)
            chain.AddReportable("Generator Context", () => GeneratorDiagnosticFormatter.Format(subject, originalSource));
    }

    /// <inheritdoc />
    protected override string Identifier => "generator result";

    /// <summary>
    ///     Asserts that a source file with the specified hint name was generated.
    /// </summary>
    /// <param name="hintName">The expected hint name of the generated file (e.g., "Person.g.cs").</param>
    /// <param name="because">A formatted phrase explaining why the assertion should be satisfied.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndWhichConstraint{TParent,TSubject}" /> for chaining assertions on the generated source.</returns>
    /// <remarks>
    ///     The hint name is case-sensitive and must match exactly. If the file is not found,
    ///     the error message includes context about why generation may have failed.
    /// </remarks>
    /// <example>
    ///     <code>
    /// result.Should(source).HaveGeneratedSource("MyType.g.cs")
    ///       .Which.Should().HaveContent("public class MyType");
    /// </code>
    /// </example>
    [CustomAssertion]
    public AndWhichConstraint<GeneratorRunResultAssertions, GeneratedSourceResult> HaveGeneratedSource(string hintName,
        string because = "", params object[] becauseArgs)
    {
        var allSources = Subject.Results.SelectMany(r => r.GeneratedSources).ToList();
        var exists = allSources.Any(s => s.HintName == hintName);

        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .WithExpectation("Expected generated source {0}, ", hintName, chain =>
            {
                if (!exists)
                {
                    if (Subject.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                        chain.FailWith(
                            "but it was not found. (Likely suppressed by compilation errors - see Generator Context below).");
                    else
                        chain.FailWith("but it was not found. Available files: [{0}]",
                            allSources.Count > 0 ? string.Join(", ", allSources.Select(s => s.HintName)) : "none");
                }
            });

        var result = exists ? allSources.First(s => s.HintName == hintName) : default;
        return new AndWhichConstraint<GeneratorRunResultAssertions, GeneratedSourceResult>(this, result);
    }

    /// <summary>
    ///     Asserts that the generator produced no diagnostics.
    /// </summary>
    /// <param name="because">A formatted phrase explaining why the assertion should be satisfied.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
    /// <remarks>
    ///     This assertion fails if any diagnostic was reported by the generator, regardless of severity.
    /// </remarks>
    /// <example>
    ///     <code>
    /// result.Should().HaveNoDiagnostics()
    ///       .And.HaveGeneratedSource("Output.g.cs");
    /// </code>
    /// </example>
    [CustomAssertion]
    public AndConstraint<GeneratorRunResultAssertions> HaveNoDiagnostics(string because = "",
        params object[] becauseArgs)
    {
        var diagnostics = Subject.Results.SelectMany(r => r.Diagnostics).ToList();
        CurrentAssertionChain.BecauseOf(because, becauseArgs)
            .ForCondition(diagnostics.Count is 0)
            .FailWith("Expected no diagnostics, but found {0}.", diagnostics.Count);

        return new AndConstraint<GeneratorRunResultAssertions>(this);
    }

    /// <summary>
    ///     Asserts that no forbidden Roslyn runtime types are cached in generator pipeline outputs.
    /// </summary>
    /// <param name="because">A formatted phrase explaining why the assertion should be satisfied.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
    /// <remarks>
    ///     <para>
    ///         Forbidden types include: <see cref="ISymbol" />, <see cref="Compilation" />,
    ///         <see cref="SemanticModel" />, <see cref="SyntaxNode" />, <see cref="SyntaxTree" />, and
    ///         <see cref="IOperation" />.
    ///     </para>
    ///     <para>
    ///         Note: This assertion requires step tracking to be enabled when creating the generator driver.
    ///     </para>
    /// </remarks>
    [CustomAssertion]
    public AndConstraint<GeneratorRunResultAssertions> NotHaveForbiddenTypes(string because = "",
        params object[] becauseArgs)
    {
        var trackingEnabled = Subject!.Results.Any(r => r.TrackedSteps is { Count: > 0 });
        CurrentAssertionChain.BecauseOf(because, becauseArgs).WithExpectation(
            "Expected {0} to not cache forbidden Roslyn types, ",
            ch => ch.ForCondition(trackingEnabled)
                .FailWith("but step tracking was disabled.")
                .Then.Given(() => ForbiddenTypeAnalyzer.AnalyzeGeneratorRun(Subject!))
                .ForCondition(violations => violations.Count is 0).FailWith("but found {0} violations:\n{1}",
                    violations => violations.Count, BuildViolationReport));

        return new AndConstraint<GeneratorRunResultAssertions>(this);

        static string BuildViolationReport(IEnumerable<ForbiddenTypeViolation> violations)
        {
            StringBuilder sb = new();
            foreach (var group in violations.GroupBy(v => v.StepName))
            {
                sb.AppendLine($"  Step '{group.Key}':");
                foreach (var violation in group)
                    sb.AppendLine($"    ✗ {violation.ForbiddenType.Name} at {violation.Path}");
            }

            return sb.ToString();
        }
    }
}
