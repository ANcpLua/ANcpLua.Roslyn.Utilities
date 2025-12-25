using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides string extension methods for fluent, concise generator testing.
/// </summary>
/// <remarks>
///     <para>
///         This class enables a fluent, table-driven testing style where C# source code strings
///         can be directly tested against generator expectations using extension methods.
///         All methods automatically handle:
///         <list type="bullet">
///             <item>
///                 <description>Compilation creation with proper references</description>
///             </item>
///             <item>
///                 <description>Generator driver setup with step tracking</description>
///             </item>
///             <item>
///                 <description>Two-pass execution for caching validation</description>
///             </item>
///             <item>
///                 <description>Assertion scope setup with proper formatters</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         The static constructor ensures <see cref="TestFormatters" /> are initialized
///         before any test method is called.
///     </para>
/// </remarks>
/// <example>
///     Simple generation test:
///     <code>
/// await """
///     [MyAttribute]
///     public class Person { public string Name { get; set; } }
/// """.ShouldGenerate&lt;MyGenerator&gt;("Person.g.cs", "public class PersonProxy");
/// </code>
///     Diagnostic test:
///     <code>
/// await "public class Invalid { }".ShouldHaveDiagnostics&lt;MyGenerator&gt;(
///     new DiagnosticResult("GEN001", DiagnosticSeverity.Error)
/// );
/// </code>
///     Caching test:
///     <code>
/// await "public class Person { }".ShouldBeCached&lt;MyGenerator&gt;("TransformStep");
/// </code>
/// </example>
public static class GeneratorTestExtensions
{
    static GeneratorTestExtensions()
    {
        TestFormatters.Initialize();
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticResult" /> with the specified ID and severity.
    /// </summary>
    /// <param name="id">The diagnostic ID (e.g., "GEN001").</param>
    /// <param name="severity">The expected severity. Defaults to <see cref="JSType.Error" />.</param>
    /// <returns>A new <see cref="DiagnosticResult" /> for use in diagnostic assertions.</returns>
    /// <remarks>
    ///     This is a convenience factory method that makes test code more readable.
    ///     The returned result can be further configured with location and message expectations.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var expected = GeneratorTestExtensions.Diagnostic("GEN001", DiagnosticSeverity.Warning)
    ///     .WithSpan(10, 1, 10, 20)
    ///     .WithMessage("Expected message");
    /// </code>
    /// </example>
    public static DiagnosticResult Diagnostic(string id, DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        return new DiagnosticResult(id, severity);
    }

    private static string BuildStepValidationError(IEnumerable<string> missingSteps, IEnumerable<string> availableSteps)
    {
        StringBuilder sb = new();
        sb.AppendLine("CACHING VALIDATION ERROR: Missing tracking step name(s)");
        foreach (var step in missingSteps) sb.AppendLine($"  - {step}");
        sb.AppendLine("\nAvailable steps:");
        foreach (var step in availableSteps.OrderBy(x => x))
            sb.AppendLine(
                $"  {(GeneratorStepAnalyzer.IsInfrastructureStep(step) ? "[Sink]" : "[Observable]")} {step}");
        return sb.ToString();
    }

    private static void ValidateStepPerformance(GeneratorDriverRunResult firstRun, GeneratorDriverRunResult secondRun,
        IEnumerable<string> stepNames)
    {
        var firstSteps = GeneratorStepAnalyzer.ExtractSteps(firstRun);
        var secondSteps = GeneratorStepAnalyzer.ExtractSteps(secondRun);

        foreach (var stepName in stepNames)
        {
            var first = firstSteps[stepName];
            var second = secondSteps[stepName];
            first.Should().HaveSameCount(second, $"step {stepName} should run same number of times");

            for (var i = 0; i < first.Length; i++)
            {
                var allowedTime = first[i].ElapsedTime + Max(TestConfiguration.PerformanceToleranceAbsolute,
                    TimeSpan.FromTicks(
                        (long)(first[i].ElapsedTime.Ticks * TestConfiguration.PerformanceTolerancePercent)));

                second[i].ElapsedTime.Should().BeLessThanOrEqualTo(allowedTime,
                    $"cached run of {stepName}[{i}] should not be materially slower than baseline");
            }
        }

        return;

        static TimeSpan Max(TimeSpan a, TimeSpan b)
        {
            return a >= b ? a : b;
        }
    }

    /// <summary>
    ///     Asserts that running the specified generator on this source code produces
    ///     a generated file with the expected hint name and content.
    /// </summary>
    /// <typeparam name="TGenerator">
    ///     The generator type under test. Must implement <see cref="IIncrementalGenerator" />
    ///     and have a parameterless constructor.
    /// </typeparam>
    /// <param name="hintName">
    ///     The expected hint name of the generated file (e.g., "Person.Builder.g.cs").
    ///     This must match exactly, including the .g.cs suffix.
    /// </param>
    /// <param name="expectedContent">The expected content of the generated file.</param>
    /// <param name="exactMatch">
    ///     If <c>true</c> (default), the entire content must match exactly.
    ///     If <c>false</c>, the expected content must be contained within the actual content.
    /// </param>
    /// <param name="normalizeNewlines">
    ///     If <c>true</c> (default), line endings are normalized to Unix style before comparison.
    /// </param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when the assertion is finished.</returns>
    /// <remarks>
    ///     <para>
    ///         This method performs the following steps:
    ///         <list type="number">
    ///             <item>
    ///                 <description>Creates a compilation from the source code with configured references</description>
    ///             </item>
    ///             <item>
    ///                 <description>Runs the generator twice (for caching validation)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Asserts that the expected file was generated with matching content</description>
    ///             </item>
    ///             <item>
    ///                 <description>Asserts that no diagnostics were reported</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// await """
    ///     [GenerateBuilder]
    ///     public class Person
    ///     {
    ///         public string Name { get; set; }
    ///     }
    /// """.ShouldGenerate&lt;BuilderGenerator&gt;("Person.Builder.g.cs", """
    ///     public class PersonBuilder
    ///     {
    ///         public PersonBuilder WithName(string name) { ... }
    ///     }
    /// """);
    /// </code>
    /// </example>
    public static async Task ShouldGenerate<TGenerator>(this string source, string hintName, string expectedContent,
        bool exactMatch = true, bool normalizeNewlines = true) where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();
        var (firstRun, _) = await engine.ExecuteTwiceAsync(source, true);

        using AssertionScope scope = new($"Generated file '{hintName}'");
        TestFormatters.ApplyToScope(scope);

        var generated = firstRun.Should().HaveGeneratedSource(hintName).Which;
        generated.Should().HaveContent(expectedContent, exactMatch, normalizeNewlines);
        firstRun.Should().HaveNoDiagnostics();
    }

    /// <summary>
    ///     Asserts that running the specified generator on this source code produces
    ///     the expected diagnostics.
    /// </summary>
    /// <typeparam name="TGenerator">
    ///     The generator type under test. Must implement <see cref="IIncrementalGenerator" />
    ///     and have a parameterless constructor.
    /// </typeparam>
    /// <param name="expected">
    ///     The expected diagnostics. Pass an empty array or omit to assert no diagnostics.
    /// </param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when the assertion is finished.</returns>
    /// <remarks>
    ///     <para>
    ///         Diagnostics are compared by ID, severity, location (if specified), and message (if specified).
    ///         Order matters: diagnostics are sorted by location then ID before comparison.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Expect specific diagnostic
    /// await "public class Invalid { }".ShouldHaveDiagnostics&lt;MyGenerator&gt;(
    ///     new DiagnosticResult("GEN001", DiagnosticSeverity.Error)
    ///         .WithMessage("Missing required attribute")
    /// );
    /// 
    /// // Expect no diagnostics (equivalent to ShouldHaveNoDiagnostics)
    /// await "public class Valid { }".ShouldHaveDiagnostics&lt;MyGenerator&gt;();
    /// </code>
    /// </example>
    public static async Task ShouldHaveDiagnostics<TGenerator>(this string source, params DiagnosticResult[] expected)
        where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();
        var (firstRun, _) = await engine.ExecuteTwiceAsync(source, false);

        using AssertionScope scope = new("Diagnostics");
        TestFormatters.ApplyToScope(scope);

        var diagnostics = firstRun.Results.SelectMany(r => r.Diagnostics).ToList();
        diagnostics.BeEquivalentToDiagnostics(expected);
    }

    /// <summary>
    ///     Asserts that running the specified generator on this source code produces no diagnostics.
    /// </summary>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <typeparam name="TGenerator">
    ///     The generator type under test. Must implement <see cref="IIncrementalGenerator" />
    ///     and have a parameterless constructor.
    /// </typeparam>
    /// <returns>A task that completes when the assertion is finished.</returns>
    /// <remarks>
    ///     This is equivalent to calling <see cref="ShouldHaveDiagnostics{TGenerator}(string,DiagnosticResult[])" />
    ///     with an empty array of expected diagnostics.
    /// </remarks>
    /// <example>
    ///     <code>
    /// await "[ValidAttribute] public class Valid { }".ShouldHaveNoDiagnostics&lt;MyGenerator&gt;();
    /// </code>
    /// </example>
    public static Task ShouldHaveNoDiagnostics<TGenerator>(this string source)
        where TGenerator : IIncrementalGenerator, new()
    {
        return source.ShouldHaveDiagnostics<TGenerator>();
    }

    /// <summary>
    ///     Asserts that the specified generator correctly caches its pipeline outputs
    ///     when run twice on identical source code.
    /// </summary>
    /// <typeparam name="TGenerator">
    ///     The generator type under test. Must implement <see cref="IIncrementalGenerator" />
    ///     and have a parameterless constructor.
    /// </typeparam>
    /// <param name="trackingNames">
    ///     Optional array of step names to validate for caching. If provided, only these
    ///     steps are checked. If omitted, all observable (non-infrastructure) steps are validated.
    /// </param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when the assertion is finished.</returns>
    /// <remarks>
    ///     <para>
    ///         This method validates three aspects of generator caching:
    ///         <list type="number">
    ///             <item>
    ///                 <description>
    ///                     <b>No forbidden types:</b> Ensures no Roslyn runtime types (ISymbol, Compilation,
    ///                     SyntaxNode, etc.) are cached in pipeline outputs.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <b>Proper caching:</b> On the second run, tracked steps should report
    ///                     Cached or Unchanged, not Modified, New, or Removed.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <b>Performance:</b> The second run should not be materially slower than the first,
    ///                     within configured tolerance.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         To use this method effectively, generator pipeline steps should be named using
    ///         <c>WithTrackingName("StepName")</c> in the generator implementation.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Validate all observable steps
    /// await "public class Person { }".ShouldBeCached&lt;MyGenerator&gt;();
    /// 
    /// // Validate specific steps
    /// await "public class Person { }".ShouldBeCached&lt;MyGenerator&gt;("TransformStep", "CollectStep");
    /// </code>
    /// </example>
    public static async Task ShouldBeCached<TGenerator>(this string source, params string[] trackingNames)
        where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();
        var (firstRun, secondRun) =
            await engine.ExecuteTwiceAsync(source, true);

        var availableSteps =
            firstRun.Results.SelectMany(r => r.TrackedSteps).Select(kv => kv.Key).Distinct().ToList();
        string[] stepsToTrack;
        if (trackingNames is { Length: > 0 })
        {
            var missingSteps = trackingNames.Except(availableSteps, StringComparer.Ordinal).ToArray();
            if (missingSteps.Length > 0)
                throw new InvalidOperationException(BuildStepValidationError(missingSteps, availableSteps));
            stepsToTrack = trackingNames;
        }
        else
        {
            stepsToTrack = availableSteps.Where(s => !GeneratorStepAnalyzer.IsInfrastructureStep(s)).ToArray();
            if (stepsToTrack.Length is 0 && availableSteps.Count > 0)
                throw new InvalidOperationException(
                    "Auto-Discovery Failed: No observable user steps found. Ensure pipeline steps are named (e.g., .WithTrackingName(\"MyStep\")).\n" +
                    BuildStepValidationError([], availableSteps));
        }

        var report = GeneratorCachingReport.Create(firstRun, secondRun, typeof(TGenerator));

        using AssertionScope scope = new($"{typeof(TGenerator).Name} Caching Pipeline");
        TestFormatters.ApplyToScope(scope);

        report.Should().BeValidAndCached(stepsToTrack);

        if (stepsToTrack is { Length: > 0 })
            ValidateStepPerformance(firstRun, secondRun, stepsToTrack);
    }
}