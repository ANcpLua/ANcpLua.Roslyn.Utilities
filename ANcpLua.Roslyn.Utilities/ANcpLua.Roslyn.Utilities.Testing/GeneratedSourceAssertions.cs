using System;
using System.Text;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Contains assertions for <see cref="GeneratedSourceResult" /> instances.
/// </summary>
/// <remarks>
///     This class provides assertions for validating the content of a single generated source file.
///     It supports both exact matching and contains matching, with options for newline normalization.
/// </remarks>
/// <example>
///     <code>
/// generatedSource.Should().HaveContent("public class Foo { }");
/// generatedSource.Should().HaveContent("public class Foo", exactMatch: false);
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
    /// </param>
    /// <param name="because">A formatted phrase explaining why the assertion should be satisfied.</param>
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
    /// source.Should().HaveContent(@"
    /// // &lt;auto-generated/&gt;
    /// public class Person
    /// {
    ///     public string Name { get; set; }
    /// }
    /// ");
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