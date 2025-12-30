using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides fluent assertion extension methods for generator testing.
/// </summary>
/// <remarks>
///     <para>
///         This class extends AwesomeAssertions to provide specialized assertion capabilities for
///         Roslyn <see cref="IIncrementalGenerator" /> implementations. These assertions are designed
///         to produce clear, actionable failure messages that help diagnose generator issues quickly.
///     </para>
///     <para>
///         The assertions cover three main areas:
///         <list type="bullet">
///             <item>
///                 <description>Caching validation via <see cref="CachingReportAssertions" /></description>
///             </item>
///             <item>
///                 <description>Output validation via <see cref="GeneratedSourceAssertions" /></description>
///             </item>
///             <item>
///                 <description>Diagnostic validation via <see cref="DiagnosticCollectionAssertions" /></description>
///             </item>
///         </list>
///     </para>
/// </remarks>
/// <example>
///     <code>
/// var report = GeneratorCachingReport.Create(firstRun, secondRun, typeof(MyGenerator));
/// report.Should().BeValidAndCached(["TransformStep", "CollectStep"]);
/// 
/// var result = driver.GetRunResult();
/// result.Should().HaveGeneratedSource("MyType.g.cs")
///       .Which.Should().HaveContent("public class MyType { }");
/// </code>
/// </example>
public static class GeneratorTestAssertions
{
    /// <summary>
    ///     Returns a <see cref="CachingReportAssertions" /> object for the caching report.
    /// </summary>
    /// <param name="subject">The caching report to assert on.</param>
    /// <returns>An assertion object for fluent chaining.</returns>
    public static CachingReportAssertions Should(this GeneratorCachingReport subject)
    {
        return new CachingReportAssertions(subject, AssertionChain.GetOrCreate());
    }

    /// <summary>
    ///     Returns a <see cref="GeneratorRunResultAssertions" /> object for the run result.
    /// </summary>
    /// <param name="subject">The generator run result to assert on.</param>
    /// <returns>An assertion object for fluent chaining.</returns>
    public static GeneratorRunResultAssertions Should(this GeneratorDriverRunResult subject)
    {
        return new GeneratorRunResultAssertions(subject, AssertionChain.GetOrCreate());
    }

    /// <summary>
    ///     Returns a <see cref="GeneratedSourceAssertions" /> object for the generated source.
    /// </summary>
    /// <param name="subject">The generated source result to assert on.</param>
    /// <returns>An assertion object for fluent chaining.</returns>
    public static GeneratedSourceAssertions Should(this GeneratedSourceResult subject)
    {
        return new GeneratedSourceAssertions(subject, AssertionChain.GetOrCreate());
    }

    /// <summary>
    ///     Asserts that the object (expected to be a <see cref="Location" />) is at the specified location.
    /// </summary>
    /// <param name="assertions">The object assertions instance.</param>
    /// <param name="expected">The expected location to compare against.</param>
    /// <param name="because">A formatted phrase explaining why the assertion should be satisfied.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{TParent}" /> for chaining further assertions.</returns>
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
    /// <param name="assertions">The object assertions instance.</param>
    /// <param name="line">The expected 1-based line number.</param>
    /// <param name="column">The expected 1-based column number.</param>
    /// <param name="filePath">The expected file path (optional).</param>
    /// <param name="because">A formatted phrase explaining why the assertion should be satisfied.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> for chaining further assertions.</returns>
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