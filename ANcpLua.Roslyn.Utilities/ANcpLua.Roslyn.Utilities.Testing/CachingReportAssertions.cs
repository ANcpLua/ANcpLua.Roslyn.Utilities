using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

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
///     <para>
///         On failure, comprehensive context is automatically included showing all pipeline steps,
///         their caching states, forbidden type violations, and timing information.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// var report = GeneratorCachingReport.Create(firstRun, secondRun, typeof(MyGenerator));
/// report.Should().BeValidAndCached();
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
        chain.AddReportable("Caching Pipeline Overview", BuildPipelineOverview);
    }

    /// <inheritdoc />
    protected override string Identifier => $"CachingReport[{Subject?.GeneratorName}]";

    private string BuildPipelineOverview()
    {
        if (Subject is null) return "(null report)";

        StringBuilder sb = new();
        sb.AppendLine();
        sb.AppendLine($"╔══════════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║  CACHING PIPELINE: {Subject.GeneratorName,-41} ║");
        sb.AppendLine($"╚══════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        // Summary
        var totalSteps = Subject.ObservableSteps.Count;
        var cachedSteps = Subject.ObservableSteps.Count(s => s.IsCachedSuccessfully);
        var forbiddenCount = Subject.ForbiddenTypeViolations.Count;
        sb.AppendLine($"  📊 Steps: {cachedSteps}/{totalSteps} cached | Forbidden types: {forbiddenCount} | Output: {(Subject.ProducedOutput ? "✓" : "✗")}");
        sb.AppendLine();

        // Forbidden type violations (critical - show first)
        if (forbiddenCount > 0)
        {
            sb.AppendLine("─── 💥 FORBIDDEN TYPE VIOLATIONS ───");
            foreach (var group in Subject.ForbiddenTypeViolations.GroupBy(v => v.StepName))
            {
                sb.AppendLine($"  Step '{group.Key}':");
                foreach (var v in group)
                    sb.AppendLine($"    ✗ {v.ForbiddenType.Name} at {v.Path}");
            }
            sb.AppendLine();
        }

        // Observable steps
        sb.AppendLine("─── OBSERVABLE STEPS ───");
        foreach (var step in Subject.ObservableSteps.OrderBy(s => s.StepName))
        {
            var icon = step.IsCachedSuccessfully ? "✓" : "✗";
            var forbidden = step.HasForbiddenTypes ? " [FORBIDDEN]" : "";
            sb.AppendLine($"  {icon} {step.StepName}: {step.FormatBreakdown()}{forbidden}");
            sb.AppendLine($"      Time: {step.FormatPerformance()}");
        }
        sb.AppendLine();

        // Sink steps (infrastructure)
        if (Subject.SinkSteps.Count > 0)
        {
            sb.AppendLine("─── SINK STEPS (infrastructure) ───");
            foreach (var step in Subject.SinkSteps.OrderBy(s => s.StepName))
                sb.AppendLine($"  ⚙ {step.StepName}: {step.FormatBreakdown()}");
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Asserts that the generator caching report indicates valid caching behavior.
    /// </summary>
    /// <param name="requiredSteps">
    ///     Optional array of step names to validate. If provided, only these steps are checked for caching.
    ///     If omitted, all observable (non-infrastructure) steps are validated.
    /// </param>
    /// <param name="because">A formatted phrase explaining why the assertion should be satisfied.</param>
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
    ///             <description>Tracked steps report Cached or Unchanged on second run</description>
    ///         </item>
    ///         <item>
    ///             <description>The generator produced meaningful output</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Forbidden types include: <see cref="ISymbol" />, <see cref="Compilation" />,
    ///         <see cref="SemanticModel" />, <see cref="SyntaxNode" />, <see cref="SyntaxTree" />, and
    ///         <see cref="IOperation" />.
    ///     </para>
    /// </remarks>
    [CustomAssertion]
    public AndConstraint<CachingReportAssertions> BeValidAndCached(string[]? requiredSteps = null, string because = "",
        params object[] becauseArgs)
    {
        CurrentAssertionChain.BecauseOf(because, becauseArgs).WithExpectation(
            $"Expected {Identifier} to be valid and optionally cached, ", ch => ch.Given(() => Subject)
                .ForCondition(report => report is not null).FailWith("but the CachingReport was <null>.").Then
                .Given(report =>
                {
                    var stepsToCheck = requiredSteps is { Length: > 0 }
                        ? new HashSet<string>(requiredSteps, StringComparer.Ordinal)
                        : null;

                    var finalFailedCaching = report.ObservableSteps
                        .Where(s => stepsToCheck is null || stepsToCheck.Contains(s.StepName))
                        .Where(s => !s.IsCachedSuccessfully)
                        .ToList();

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