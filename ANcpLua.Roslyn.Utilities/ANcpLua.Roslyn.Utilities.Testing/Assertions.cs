using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Diagnostic = Microsoft.CodeAnalysis.Diagnostic;
using FileLinePositionSpan = Microsoft.CodeAnalysis.FileLinePositionSpan;
using GeneratedSourceResult = Microsoft.CodeAnalysis.GeneratedSourceResult;
using GeneratorDriverRunResult = Microsoft.CodeAnalysis.GeneratorDriverRunResult;
using Location = Microsoft.CodeAnalysis.Location;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides fluent assertion extension methods for generator testing.
/// </summary>
/// <remarks>
///     <para>
///         This class extends AwesomeAssertions to provide specialized assertion capabilities for
///         Roslyn <see cref="Microsoft.CodeAnalysis.IIncrementalGenerator" /> implementations. These assertions are designed
///         to produce clear, actionable failure messages that help diagnose generator issues quickly.
///     </para>
///     <para>
///         The assertions cover three main areas:
///         <list type="bullet">
///             <item>
///                 <description>
///                     Caching validation via <see cref="CachingReportAssertions" /> - ensuring generators
///                     properly cache intermediate results
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Output validation via <see cref="GeneratedSourceAssertions" /> - verifying generated
///                     source content matches expectations
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Diagnostic validation via <see cref="DiagnosticCollectionAssertions" /> - asserting that
///                     generators report correct <see cref="Microsoft.CodeAnalysis.Diagnostic" /> instances
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
/// <example>
///     Basic usage with caching report:
///     <code>
/// var report = GeneratorCachingReport.Create(firstRun, secondRun, typeof(MyGenerator));
/// report.Should().BeValidAndCached(["TransformStep", "CollectStep"]);
/// </code>
///     Verifying generated source:
///     <code>
/// var result = driver.GetRunResult();
/// result.Should().HaveGeneratedSource("MyType.g.cs")
///       .Which.Should().HaveContent("public class MyType { }");
/// </code>
/// </example>
/// <seealso cref="GeneratorRunResultAssertions" />
/// <seealso cref="GeneratedSourceAssertions" />
/// <seealso cref="CachingReportAssertions" />
public static class GeneratorTestAssertions
{
    /// <summary>
    ///     Returns a <see cref="CachingReportAssertions" /> object that can be used to assert
    ///     the current <see cref="GeneratorCachingReport" />.
    /// </summary>
    /// <param name="subject">The caching report to assert on.</param>
    /// <returns>An assertion object for fluent chaining.</returns>
    /// <example>
    ///     <code>
    /// var report = GeneratorCachingReport.Create(firstRun, secondRun, typeof(MyGenerator));
    /// report.Should().BeValidAndCached();
    /// </code>
    /// </example>
    public static CachingReportAssertions Should(this GeneratorCachingReport subject)
    {
        return new CachingReportAssertions(subject, AssertionChain.GetOrCreate());
    }

    /// <summary>
    ///     Returns a <see cref="GeneratorRunResultAssertions" /> object that can be used to assert
    ///     the current <see cref="Microsoft.CodeAnalysis.GeneratorDriverRunResult" />.
    /// </summary>
    /// <param name="subject">The generator run result to assert on.</param>
    /// <returns>An assertion object for fluent chaining.</returns>
    /// <remarks>
    ///     This is the primary entry point for asserting on generator output. Use it to verify
    ///     that expected files were generated and contain the correct content.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var result = driver.GetRunResult();
    /// result.Should()
    ///       .HaveGeneratedSource("Person.g.cs")
    ///       .And.HaveNoDiagnostics();
    /// </code>
    /// </example>
    public static GeneratorRunResultAssertions Should(this GeneratorDriverRunResult subject)
    {
        return new GeneratorRunResultAssertions(subject, AssertionChain.GetOrCreate());
    }

    /// <summary>
    ///     Returns a <see cref="GeneratedSourceAssertions" /> object that can be used to assert
    ///     the current <see cref="Microsoft.CodeAnalysis.GeneratedSourceResult" />.
    /// </summary>
    /// <param name="subject">The generated source result to assert on.</param>
    /// <returns>An assertion object for fluent chaining.</returns>
    /// <example>
    ///     <code>
    /// var source = result.Results.First().GeneratedSources.First();
    /// source.Should().HaveContent("public class Foo { }", exactMatch: true);
    /// </code>
    /// </example>
    public static GeneratedSourceAssertions Should(this GeneratedSourceResult subject)
    {
        return new GeneratedSourceAssertions(subject, AssertionChain.GetOrCreate());
    }

    /// <summary>
    ///     Asserts that the object (expected to be a <see cref="Microsoft.CodeAnalysis.Location" />) is at the specified location.
    /// </summary>
    /// <param name="expected">The expected location to compare against.</param>
    /// <param name="because">
    ///     A formatted phrase explaining why the assertion should be satisfied.
    ///     If the phrase does not start with "because", it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <param name="assertions">The object assertions instance.</param>
    /// <returns>An <see cref="AndConstraint{TParent}" /> for chaining further assertions.</returns>
    /// <remarks>
    ///     This assertion compares source locations including file path, line, and column.
    ///     It handles both in-source and metadata locations appropriately.
    /// </remarks>
    /// <example>
    ///     <code>
    /// diagnostic.Location.Should().BeAt(expectedLocation);
    /// </code>
    /// </example>
    public static AndConstraint<ObjectAssertions> BeAt(this ObjectAssertions assertions, Location expected,
        string because = "", params object[] becauseArgs)
    {
        var actual = assertions.Subject as Location;
        if (actual == null)
        {
            assertions.Subject.Should().NotBeNull(because, becauseArgs);
            return new AndConstraint<ObjectAssertions>(assertions);
        }

        var actualSpan = actual.GetMappedLineSpan();
        var expectedSpan = expected.GetMappedLineSpan();

        using AssertionScope scope = new("location comparison");

        actual.Kind.Should().Be(expected.Kind, "location kind should match");

        if (actual.IsInSource && expected.IsInSource)
            actualSpan.Should().BeEquivalentTo(expectedSpan,
                options => options.ComparingByMembers<FileLinePositionSpan>().Using<LinePosition>(ctx =>
                {
                    ctx.Subject.Should().Be(ctx.Expectation, "at line {0}, column {1}", ctx.Expectation.Line + 1,
                        ctx.Expectation.Character + 1);
                }).WhenTypeIs<LinePosition>(), because, becauseArgs);
        else if (actual.IsInSource != expected.IsInSource)
            throw new AssertionFailedException(
                $"Expected location to have IsInSource={expected.IsInSource}, but found {actual.IsInSource}");

        return new AndConstraint<ObjectAssertions>(assertions);
    }

    /// <summary>
    ///     Asserts that a diagnostic is at the specified source location.
    /// </summary>
    /// <param name="line">The expected 1-based line number.</param>
    /// <param name="column">The expected 1-based column number.</param>
    /// <param name="filePath">The expected file path (optional, defaults to empty which means any file).</param>
    /// <param name="because">
    ///     A formatted phrase explaining why the assertion should be satisfied.
    /// </param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <param name="assertions">The object assertions instance.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
    /// <remarks>
    ///     Line and column numbers are 1-based to match editor conventions.
    ///     If <paramref name="filePath" /> is empty or null, the file path is not validated.
    /// </remarks>
    /// <example>
    ///     <code>
    /// diagnostic.Should().BeAtLocation(10, 5, "MyFile.cs");
    /// </code>
    /// </example>
    public static AndConstraint<ObjectAssertions> BeAtLocation(this ObjectAssertions assertions, int line, int column,
        string filePath = "", string because = "", params object[] becauseArgs)
    {
        if (assertions.Subject is not Diagnostic diagnostic)
        {
            assertions.Subject.Should()
                .NotBeNull($"diagnostic should not be null{(string.IsNullOrEmpty(because) ? "" : $" {because}")}",
                    becauseArgs);
            return new AndConstraint<ObjectAssertions>(assertions);
        }

        using AssertionScope scope = new("diagnostic location");
        var location = diagnostic.Location;
        var mappedSpan = location.GetMappedLineSpan();

        if (!location.IsInSource)
            throw new AssertionFailedException(
                "Expected diagnostic to have a source location, but it was not in source");

        var actualLine = mappedSpan.StartLinePosition.Line + 1;
        var actualColumn = mappedSpan.StartLinePosition.Character + 1;

        actualLine.Should().Be(line,
            $"diagnostic should be at line {line}{(string.IsNullOrEmpty(because) ? "" : $" {because}")}",
            becauseArgs);
        actualColumn.Should().Be(column,
            $"diagnostic should be at column {column}{(string.IsNullOrEmpty(because) ? "" : $" {because}")}",
            becauseArgs);

        if (string.IsNullOrEmpty(filePath)) return new AndConstraint<ObjectAssertions>(assertions);
        var normalizedActual = TextUtilities.NormalizePath(mappedSpan.Path);
        var normalizedExpected = TextUtilities.NormalizePath(filePath);
        normalizedActual.Should().Be(normalizedExpected,
            $"diagnostic should be in file {filePath}{(string.IsNullOrEmpty(because) ? "" : $" {because}")}",
            becauseArgs);

        return new AndConstraint<ObjectAssertions>(assertions);
    }
}

/// <summary>
///     Contains assertions for <see cref="GeneratorCachingReport" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This assertion class validates that an incremental generator correctly implements caching.
///         Proper caching is essential for generator performance in IDEs, where generators run frequently
///         as the user types.
///     </para>
///     <para>
///         The assertions check for:
///         <list type="bullet">
///             <item>
///                 <description>No forbidden Roslyn types cached (ISymbol, Compilation, SyntaxNode, etc.)</description>
///             </item>
///             <item>
///                 <description>Pipeline steps produce cached/unchanged outputs on second run</description>
///             </item>
///             <item>
///                 <description>Generator actually produces meaningful output</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
/// <example>
///     <code>
/// var report = GeneratorCachingReport.Create(firstRun, secondRun, typeof(MyGenerator));
///
/// // Validate all user-defined steps are cached
/// report.Should().BeValidAndCached();
///
/// // Or validate specific steps
/// report.Should().BeValidAndCached(["ParseStep", "TransformStep"]);
/// </code>
/// </example>
public sealed class CachingReportAssertions : ReferenceTypeAssertions<GeneratorCachingReport, CachingReportAssertions>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CachingReportAssertions" /> class.
    /// </summary>
    /// <param name="subject">The caching report to assert on.</param>
    /// <param name="chain">The assertion chain for error reporting.</param>
    public CachingReportAssertions(GeneratorCachingReport subject, AssertionChain chain) : base(subject, chain)
    {
    }

    /// <inheritdoc />
    protected override string Identifier => $"CachingReport[{Subject?.GeneratorName}]";

    /// <summary>
    ///     Asserts that the generator caching report indicates valid caching behavior.
    /// </summary>
    /// <param name="requiredSteps">
    ///     Optional array of step names to validate. If provided, only these steps are checked for caching.
    ///     If omitted, all observable (non-infrastructure) steps are validated.
    /// </param>
    /// <param name="because">
    ///     A formatted phrase explaining why the assertion should be satisfied.
    /// </param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
    /// <remarks>
    ///     <para>
    ///         This is the primary assertion for validating generator caching. It checks:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>No forbidden Roslyn runtime types are cached in pipeline outputs</description>
    ///         </item>
    ///         <item>
    ///             <description>Tracked steps report Cached or Unchanged (not Modified, New, or Removed) on second run</description>
    ///         </item>
    ///         <item>
    ///             <description>The generator produced meaningful (non-infrastructure) output</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Forbidden types include: <see cref="ISymbol" />, <see cref="Compilation" />,
    ///         <see cref="SemanticModel" />, <see cref="SyntaxNode" />, <see cref="SyntaxTree" />, and IOperation.
    ///         Caching these types causes memory leaks and IDE performance degradation.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Validate all user-defined steps
    /// report.Should().BeValidAndCached();
    ///
    /// // Validate only specific steps
    /// report.Should().BeValidAndCached(["MyTransformStep", "MyCollectStep"]);
    ///
    /// // With custom message
    /// report.Should().BeValidAndCached(
    ///     ["TransformStep"],
    ///     because: "the generator should cache transformed results");
    /// </code>
    /// </example>
    [CustomAssertion]
    public AndConstraint<CachingReportAssertions> BeValidAndCached(string[]? requiredSteps = null, string because = "",
        params object[] becauseArgs)
    {
        CurrentAssertionChain.BecauseOf(because, becauseArgs).WithExpectation(
            $"Expected {Identifier} to be valid and optionally cached, ", ch => ch.Given(() => Subject)
                .ForCondition(report => report is not null).FailWith("but the CachingReport was <null>.").Then
                .Given(report =>
                {
                    // Only check steps that user explicitly wants to track
                    var stepsToCheck = requiredSteps is { Length: > 0 }
                        ? new HashSet<string>(requiredSteps, StringComparer.Ordinal)
                        : null;

                    var finalFailedCaching = report.ObservableSteps
                        .Where(s => stepsToCheck is null || stepsToCheck.Contains(s.StepName))
                        .Where(s => !s.IsCachedSuccessfully)
                        .ToList();

                    // Only count forbidden violations in user-specified steps (skip Roslyn internals)
                    var relevantViolations = stepsToCheck is not null
                        ? report.ForbiddenTypeViolations.Where(v => stepsToCheck.Contains(v.StepName)).ToList()
                        : report.ForbiddenTypeViolations;

                    return new
                    {
                        Report = report,
                        ValidateCaching = requiredSteps is { Length: > 0 },
                        FailedCaching = finalFailedCaching,
                        ForbiddenCount = relevantViolations.Count,
                        report.ProducedOutput
                    };
                }).ForCondition(x =>
                    x.ForbiddenCount is 0 && (!x.ValidateCaching || x.FailedCaching.Count is 0) && (x.ProducedOutput ||
                        !(x.ForbiddenCount > 0 || (x.ValidateCaching && x.FailedCaching.Count > 0)))).FailWith(
                    "{0}\n{1}",
                    x => BuildSummary(x.ForbiddenCount, x.ValidateCaching ? x.FailedCaching.Count : 0,
                        x.ProducedOutput),
                    x => x.Report.BuildComprehensiveFailureReport(x.ValidateCaching ? x.FailedCaching : [],
                        requiredSteps)));

        return new AndConstraint<CachingReportAssertions>(this);

        static string BuildSummary(int forbiddenCount, int failedCount, bool producedOutput)
        {
            List<string> reasons = new(3);
            if (forbiddenCount > 0) reasons.Add("Forbidden Types Detected");
            if (failedCount > 0) reasons.Add($"Caching Failures ({failedCount} steps)");
            if (!producedOutput) reasons.Add("No Meaningful Output");
            return $"Pipeline validation failed due to: {string.Join(", ", reasons)}.";
        }
    }
}

/// <summary>
///     Contains assertions for <see cref="GeneratedSourceResult" /> instances.
/// </summary>
/// <remarks>
///     This class provides assertions for validating the content of a single generated source file.
///     It supports both exact matching and contains matching, with options for newline normalization.
/// </remarks>
/// <example>
///     <code>
/// // Exact content match
/// generatedSource.Should().HaveContent("public class Foo { }");
///
/// // Contains match (for partial validation)
/// generatedSource.Should().HaveContent("public class Foo", exactMatch: false);
///
/// // With newline normalization disabled
/// generatedSource.Should().HaveContent(expected, exactMatch: true, normalizeNewlines: false);
/// </code>
/// </example>
public sealed class
    GeneratedSourceAssertions : ReferenceTypeAssertions<GeneratedSourceResult, GeneratedSourceAssertions>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratedSourceAssertions" /> class.
    /// </summary>
    /// <param name="subject">The generated source result to assert on.</param>
    /// <param name="chain">The assertion chain for error reporting.</param>
    public GeneratedSourceAssertions(GeneratedSourceResult subject, AssertionChain chain) : base(subject, chain)
    {
    }

    /// <inheritdoc />
    protected override string Identifier => $"GeneratedSource[{Subject.HintName}]";

    /// <summary>
    ///     Asserts that the generated source has the expected content.
    /// </summary>
    /// <param name="expected">The expected content to match against.</param>
    /// <param name="exactMatch">
    ///     If <c>true</c> (default), the entire content must match exactly.
    ///     If <c>false</c>, the expected content must be contained within the actual content.
    /// </param>
    /// <param name="normalizeNewlines">
    ///     If <c>true</c> (default), line endings are normalized to Unix style (\n) before comparison.
    ///     This ensures consistent matching across different platforms.
    /// </param>
    /// <param name="because">
    ///     A formatted phrase explaining why the assertion should be satisfied.
    /// </param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
    /// <remarks>
    ///     <para>
    ///         When the assertion fails, a detailed diff is displayed showing:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>The exact location of the first difference</description>
    ///             </item>
    ///             <item>
    ///                 <description>Context lines around the difference</description>
    ///             </item>
    ///             <item>
    ///                 <description>A caret (^) pointing to the mismatch position</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Exact match
    /// source.Should().HaveContent(@"
    /// // &lt;auto-generated/&gt;
    /// public class Person
    /// {
    ///     public string Name { get; set; }
    /// }
    /// ");
    ///
    /// // Partial match - verify class declaration exists
    /// source.Should().HaveContent("public class Person", exactMatch: false);
    /// </code>
    /// </example>
    [CustomAssertion]
    public AndConstraint<GeneratedSourceAssertions> HaveContent(string expected, bool exactMatch = true,
        bool normalizeNewlines = true, string because = "", params object[] becauseArgs)
    {
        var actual = Subject.SourceText.ToString();
        if (normalizeNewlines)
        {
            actual = TextUtilities.NormalizeNewlines(actual);
            expected = TextUtilities.NormalizeNewlines(expected);
        }

        CurrentAssertionChain.BecauseOf(because, becauseArgs).WithExpectation("Expected {0} content to match, ",
            c => c.Given(() => exactMatch ? actual == expected : actual.Contains(expected, StringComparison.Ordinal))
                .ForCondition(ok => ok).FailWith("{0}", _ => BuildContentFailureReport(actual, expected, exactMatch)));

        return new AndConstraint<GeneratedSourceAssertions>(this);
    }

    private string BuildContentFailureReport(string actual, string expected, bool exactMatch)
    {
        StringBuilder sb = new();
        sb.AppendLine();
        sb.AppendLine("GENERATION ASSERTION FAILED");
        sb.AppendLine("===========================");
        sb.AppendLine($"File: {Subject.HintName} | Match Type: {(exactMatch ? "Exact" : "Contains")}");
        sb.AppendLine();

        if (exactMatch)
        {
            var differenceIndex = TextUtilities.FirstDiffIndex(expected, actual);
            if (differenceIndex < 0) differenceIndex = Math.Min(expected.Length, actual.Length);

            sb.AppendLine("Failure: Content mismatch detected.");
            sb.AppendLine(TextUtilities.BuildContextualDiff(expected, actual, differenceIndex));

            var (expectedLine, actualLine) = TextUtilities.GetLineAtIndex(expected, actual, differenceIndex);
            sb.AppendLine("One-line caret:");
            sb.AppendLine(TextUtilities.BuildOneLineCaret(expectedLine, actualLine));
        }
        else
        {
            sb.AppendLine("Failure: Expected content not found in generated file.");
            sb.AppendLine($"Expected to find: \"{expected}\"");
            sb.AppendLine($"In generated content of {actual.Length} characters");
        }

        return sb.ToString();
    }
}

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
/// </remarks>
/// <example>
///     <code>
/// var result = driver.GetRunResult();
///
/// // Verify a specific file was generated and get a reference to it
/// result.Should().HaveGeneratedSource("Person.Builder.g.cs")
///       .Which.Should().HaveContent("public class PersonBuilder");
///
/// // Verify no diagnostics
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
    public GeneratorRunResultAssertions(GeneratorDriverRunResult subject, AssertionChain chain) : base(subject, chain)
    {
    }

    /// <inheritdoc />
    protected override string Identifier => "generator result";

    /// <summary>
    ///     Asserts that a source file with the specified hint name was generated.
    /// </summary>
    /// <param name="hintName">The expected hint name of the generated file (e.g., "Person.g.cs").</param>
    /// <param name="because">
    ///     A formatted phrase explaining why the assertion should be satisfied.
    /// </param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>
    ///     An <see cref="AndWhichConstraint{TParent,TSubject}" /> that allows further assertions
    ///     on the generated source file content.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The hint name is case-sensitive and must match exactly. If the file is not found,
    ///         the error message lists all available generated files to help diagnose the issue.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Simple existence check
    /// result.Should().HaveGeneratedSource("MyType.g.cs");
    ///
    /// // Chain to content assertions
    /// result.Should().HaveGeneratedSource("MyType.g.cs")
    ///       .Which.Should().HaveContent("public class MyType");
    ///
    /// // Using the And constraint
    /// result.Should().HaveGeneratedSource("TypeA.g.cs")
    ///       .And.HaveGeneratedSource("TypeB.g.cs");
    /// </code>
    /// </example>
    [CustomAssertion]
    public AndWhichConstraint<GeneratorRunResultAssertions, GeneratedSourceResult> HaveGeneratedSource(string hintName,
        string because = "", params object[] becauseArgs)
    {
        var allSources = Subject.Results.SelectMany(r => r.GeneratedSources).ToList();
        var found = allSources.Any(s => s.HintName == hintName);
        var available = string.Join(", ", allSources.Select(s => s.HintName));

        CurrentAssertionChain.BecauseOf(because, becauseArgs).ForCondition(found).FailWith(
            "Expected generated source with hint name {0}, but it was not found. Available: [{1}]", hintName,
            available);

        var which = allSources.First(s => s.HintName == hintName);
        return new AndWhichConstraint<GeneratorRunResultAssertions, GeneratedSourceResult>(this, which);
    }

    /// <summary>
    ///     Asserts that the generator produced no diagnostics.
    /// </summary>
    /// <param name="because">
    ///     A formatted phrase explaining why the assertion should be satisfied.
    /// </param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
    /// <remarks>
    ///     <para>
    ///         This assertion fails if any diagnostic was reported by the generator, regardless of severity.
    ///         For generators that intentionally report informational diagnostics, use
    ///         <see cref="DiagnosticAssertionExtensions.BeEquivalentToDiagnostics" /> to assert specific expectations.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Assert no diagnostics were produced
    /// result.Should().HaveNoDiagnostics();
    ///
    /// // Combine with other assertions
    /// result.Should()
    ///       .HaveNoDiagnostics()
    ///       .And.HaveGeneratedSource("Output.g.cs");
    /// </code>
    /// </example>
    [CustomAssertion]
    public AndConstraint<GeneratorRunResultAssertions> HaveNoDiagnostics(string because = "",
        params object[] becauseArgs)
    {
        var diagnostics = Subject.Results.SelectMany(r => r.Diagnostics).ToList();
        CurrentAssertionChain.BecauseOf(because, becauseArgs).ForCondition(diagnostics.Count is 0).FailWith(
            "Expected no diagnostics, but found {0}:\n{1}", diagnostics.Count,
            string.Join("\n", diagnostics.Select(d => "  - " + DiagnosticComparable.FromDiagnostic(d).Format())));
        return new AndConstraint<GeneratorRunResultAssertions>(this);
    }

    /// <summary>
    ///     Asserts that no forbidden Roslyn runtime types are cached in generator pipeline outputs.
    /// </summary>
    /// <param name="because">
    ///     A formatted phrase explaining why the assertion should be satisfied.
    /// </param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
    /// <remarks>
    ///     <para>
    ///         Forbidden types include: <see cref="ISymbol" />, <see cref="Compilation" />,
    ///         <see cref="SemanticModel" />, <see cref="SyntaxNode" />, <see cref="SyntaxTree" />, and IOperation.
    ///     </para>
    ///     <para>
    ///         Caching these types in generator pipeline outputs causes:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Memory leaks as old compilations cannot be garbage collected</description>
    ///             </item>
    ///             <item>
    ///                 <description>IDE performance degradation over time</description>
    ///             </item>
    ///             <item>
    ///                 <description>Potential correctness issues with stale data</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Note: This assertion requires step tracking to be enabled when creating the generator driver.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// result.Should().NotHaveForbiddenTypes();
    /// </code>
    /// </example>
    [CustomAssertion]
    public AndConstraint<GeneratorRunResultAssertions> NotHaveForbiddenTypes(string because = "",
        params object[] becauseArgs)
    {
        var trackingEnabled = Subject!.Results.Any(r => r.TrackedSteps is { Count: > 0 });
        CurrentAssertionChain.BecauseOf(because, becauseArgs).WithExpectation(
            "Expected {0} to not cache forbidden Roslyn types (ISymbol, Compilation, SyntaxNode, etc.).",
            ch => ch.ForCondition(trackingEnabled)
                .FailWith("but step tracking was disabled, preventing analysis. (Framework: ensure trackSteps=true).")
                .Then.Given(() => ForbiddenTypeAnalyzer.AnalyzeGeneratorRun(Subject!))
                .ForCondition(violations => violations.Count is 0).FailWith("but found {0} violations:\n{1}",
                    violations => violations.Count, BuildViolationReport));

        return new AndConstraint<GeneratorRunResultAssertions>(this);

        static string BuildViolationReport(IEnumerable<ForbiddenTypeViolation> violations)
        {
            StringBuilder sb = new();
            sb.AppendLine("  CRITICAL: Caching Roslyn runtime types leads to IDE performance/memory issues.");
            foreach (var group in violations.GroupBy(v => v.StepName))
            {
                sb.AppendLine($"  - Step '{group.Key}':");
                foreach (var violation in group)
                    sb.AppendLine($"      - {violation.ForbiddenType.FullName} at {violation.Path}");
            }

            return sb.ToString();
        }
    }
}

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
    /// <param name="because">
    ///     A formatted phrase explaining why the assertion should be satisfied.
    /// </param>
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
    /// <param name="because">
    ///     A formatted phrase explaining why the assertion should be satisfied.
    /// </param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
    public AndConstraint<DiagnosticCollectionAssertions> BeEquivalentToDiagnostics(
        IEnumerable<DiagnosticResult>? expected, string because = "", params object[] becauseArgs)
    {
        var actualDiagnostics = _subject.Select(DiagnosticComparable.FromDiagnostic).ToList();
        var expectedDiagnostics = (expected ?? [])
            .Select(DiagnosticComparable.FromResult).ToList();

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
                DiagnosticComparable.FindFirstPropertyDifference(expectedDiagnostics[i], actualDiagnostics[i]);
            if (difference is not null)
                differences.Add((i, difference.Value.Property, difference.Value.Expected, difference.Value.Actual));
        }

        if (differences.Count > 0)
        {
            var comprehensiveReport = BuildAllDifferencesReport(differences, expectedDiagnostics, actualDiagnostics);
            _chain.ForCondition(false).BecauseOf(because, becauseArgs).FailWith("{0}", comprehensiveReport);
        }

        return new AndConstraint<DiagnosticCollectionAssertions>(this);

        static List<DiagnosticComparable> OrderDiagnostics(IEnumerable<DiagnosticComparable> diagnostics)
        {
            return diagnostics.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Line)
                .ThenBy(x => x.Column)
                .ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
        }

        static string BuildAllDifferencesReport(
            List<(int Index, string Property, string Expected, string Actual)> differences,
            List<DiagnosticComparable> expectedDiagnostics, List<DiagnosticComparable> actualDiagnostics)
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

        static string BuildCountMismatchReport(List<DiagnosticComparable> actual,
            List<DiagnosticComparable> expectedList)
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