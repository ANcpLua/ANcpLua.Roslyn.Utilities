using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Primary public API for testing Roslyn <see cref="IIncrementalGenerator" /> implementations.
/// </summary>
/// <remarks>
///     <para>
///         This class provides a fluent, string-extension based API for testing incremental generators.
///         It is designed for table-driven tests and fast authoring of generator validation.
///     </para>
///     <para>
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
/// </remarks>
/// <example>
///     Basic generation test:
///     <code>
/// [Fact]
/// public async Task Generates_builder_for_class()
/// {
///     await """
///         [GenerateBuilder]
///         public class Person { public string Name { get; set; } }
///     """.ShouldGenerate&lt;BuilderGenerator&gt;("Person.Builder.g.cs", "public class PersonBuilder");
/// }
/// </code>
///     Diagnostic test:
///     <code>
/// [Fact]
/// public async Task Reports_error_for_invalid_class()
/// {
///     await "public class NoAttribute { }"
///         .ShouldHaveDiagnostic&lt;MyGenerator&gt;("GEN001", DiagnosticSeverity.Error);
/// }
/// </code>
///     Caching test:
///     <code>
/// [Fact]
/// public async Task Caches_pipeline_correctly()
/// {
///     await "public class Person { }".ShouldBeCached&lt;MyGenerator&gt;("TransformStep");
/// }
/// </code>
/// </example>
public static class GeneratorTest
{
    static GeneratorTest()
    {
        TestFormatters.Initialize();
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticResult" /> with the specified ID and severity.
    /// </summary>
    /// <param name="id">The diagnostic ID (e.g., "GEN001").</param>
    /// <param name="severity">The expected severity. Defaults to <see cref="DiagnosticSeverity.Error" />.</param>
    /// <returns>A new <see cref="DiagnosticResult" /> for use in diagnostic assertions.</returns>
    /// <remarks>
    ///     This is a convenience factory method that makes test code more readable.
    ///     The returned result can be further configured with location and message expectations.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var expected = GeneratorTest.Diagnostic("GEN001", DiagnosticSeverity.Warning)
    ///     .WithSpan(10, 1, 10, 20)
    ///     .WithMessage("Expected message");
    /// </code>
    /// </example>
    public static DiagnosticResult Diagnostic(string id, DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        return new DiagnosticResult(id, severity);
    }

    /// <summary>
    ///     Asserts that running the specified generator on this source code produces
    ///     a generated file with the expected hint name and content.
    /// </summary>
    /// <typeparam name="TGenerator">
    ///     The generator type under test. Must implement <see cref="IIncrementalGenerator" />
    ///     and have a parameterless constructor.
    /// </typeparam>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
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
    /// <returns>A task that completes when the assertion is finished.</returns>
    /// <example>
    ///     <code>
    /// await """
    ///     [GenerateBuilder]
    ///     public class Person { public string Name { get; set; } }
    /// """.ShouldGenerate&lt;BuilderGenerator&gt;("Person.Builder.g.cs", "public class PersonBuilder");
    /// </code>
    /// </example>
    public static async Task ShouldGenerate<TGenerator>(this string source, string hintName, string expectedContent,
        bool exactMatch = true, bool normalizeNewlines = true) where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();
        var (firstRun, _) = await engine.WithSource(source).RunTwiceAsync();

        using AssertionScope scope = new($"Generated file '{hintName}'");
        TestFormatters.ApplyToScope(scope);

        // Pass source to Should() for full "State of the World" context on failure
        var generated = firstRun.Should(source).HaveGeneratedSource(hintName).Which;
        generated.Should().HaveContent(expectedContent, exactMatch, normalizeNewlines);
        firstRun.Should(source).HaveNoDiagnostics();
    }

    /// <summary>
    ///     Asserts that <typeparamref name="TGenerator" /> produces a diagnostic with the given
    ///     ID and severity.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <param name="diagnosticId">Expected diagnostic ID (e.g., <c>GEN001</c>).</param>
    /// <param name="severity">Expected diagnostic severity. Defaults to <see cref="DiagnosticSeverity.Error" />.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <example>
    ///     <code>
    /// await "public class Invalid { }"
    ///     .ShouldHaveDiagnostic&lt;MyGenerator&gt;("GEN001", DiagnosticSeverity.Warning);
    /// </code>
    /// </example>
    public static Task ShouldHaveDiagnostic<TGenerator>(this string source, string diagnosticId,
        DiagnosticSeverity severity = DiagnosticSeverity.Error) where TGenerator : IIncrementalGenerator, new()
    {
        DiagnosticResult expected = new(diagnosticId, severity);
        return source.ShouldHaveDiagnostics<TGenerator>(expected);
    }

    /// <summary>
    ///     Asserts that running the specified generator on this source code produces
    ///     the expected diagnostics.
    /// </summary>
    /// <typeparam name="TGenerator">The generator type under test.</typeparam>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <param name="expected">The expected diagnostics. Pass an empty array or omit to assert no diagnostics.</param>
    /// <returns>A task that completes when the assertion is finished.</returns>
    /// <example>
    ///     <code>
    /// await "public class Invalid { }".ShouldHaveDiagnostics&lt;MyGenerator&gt;(
    ///     new DiagnosticResult("GEN001", DiagnosticSeverity.Error)
    ///         .WithMessage("Missing required attribute")
    /// );
    /// </code>
    /// </example>
    public static async Task ShouldHaveDiagnostics<TGenerator>(this string source, params DiagnosticResult[] expected)
        where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();
        var (firstRun, _) = await engine.WithSource(source).RunTwiceAsync();

        using AssertionScope scope = new("Diagnostics");
        TestFormatters.ApplyToScope(scope);

        // Inject source context for failures
        AssertionChain.GetOrCreate().AddReportable("Input Source",
            () => GeneratorDiagnosticFormatter.Format(firstRun, source));

        var diagnostics = firstRun.Results.SelectMany(r => r.Diagnostics).ToList();
        diagnostics.BeEquivalentToDiagnostics(expected);
    }

    /// <summary>
    ///     Asserts that running the specified generator on this source code produces no diagnostics.
    /// </summary>
    /// <typeparam name="TGenerator">The generator type under test.</typeparam>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when the assertion is finished.</returns>
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
    ///     Asserts that a diagnostic with the given ID and severity is produced and that its message
    ///     contains the specified text.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <param name="diagnosticId">Diagnostic ID.</param>
    /// <param name="severity">Diagnostic severity.</param>
    /// <param name="messageContains">A substring that must appear in the diagnostic message.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <example>
    ///     <code>
    /// await "public class Invalid { }"
    ///     .ShouldProduceDiagnostic&lt;MyGenerator&gt;(
    ///         "GEN002",
    ///         DiagnosticSeverity.Warning,
    ///         "Missing required attribute");
    /// </code>
    /// </example>
    public static Task ShouldProduceDiagnostic<TGenerator>(this string source, string diagnosticId,
        DiagnosticSeverity severity, string messageContains) where TGenerator : IIncrementalGenerator, new()
    {
        var expected = new DiagnosticResult(diagnosticId, severity).WithMessage(messageContains);
        return source.ShouldHaveDiagnostics<TGenerator>(expected);
    }

    /// <summary>
    ///     Asserts that <typeparamref name="TGenerator" /> does NOT produce a diagnostic with the given ID.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <param name="diagnosticId">The diagnostic ID that should NOT be present.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <example>
    ///     <code>
    /// await "[ValidAttribute] public class Valid { }"
    ///     .ShouldNotHaveDiagnostic&lt;MyGenerator&gt;("GEN001");
    /// </code>
    /// </example>
    public static async Task ShouldNotHaveDiagnostic<TGenerator>(this string source, string diagnosticId)
        where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();
        var (firstRun, _) = await engine.WithSource(source).RunTwiceAsync();

        using AssertionScope scope = new("Diagnostics");
        TestFormatters.ApplyToScope(scope);

        // Inject source context for failures
        AssertionChain.GetOrCreate().AddReportable("Generator Context",
            () => GeneratorDiagnosticFormatter.Format(firstRun, source));

        var diagnostics = firstRun.Results.SelectMany(r => r.Diagnostics).ToList();
        var found = diagnostics.Any(d => d.Id == diagnosticId);
        found.Should().BeFalse($"Expected no diagnostic with ID '{diagnosticId}', but found one");
    }

    /// <summary>
    ///     Asserts that <typeparamref name="TGenerator" /> produces no error-level diagnostics.
    ///     Info and warning diagnostics are ignored.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <remarks>
    ///     This is useful for generators that intentionally emit informational or warning
    ///     diagnostics. If you need to assert zero diagnostics of any severity,
    ///     use <see cref="ShouldHaveNoDiagnostics{TGenerator}(string)" />.
    /// </remarks>
    public static async Task ShouldCompile<TGenerator>(this string source)
        where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();
        var (firstRun, _) = await engine.WithSource(source).RunTwiceAsync();

        using AssertionScope scope = new("Compilation");
        TestFormatters.ApplyToScope(scope);

        // Inject source context for failures
        AssertionChain.GetOrCreate().AddReportable("Generator Context",
            () => GeneratorDiagnosticFormatter.Format(firstRun, source));

        var errors = firstRun.Results
            .SelectMany(r => r.Diagnostics)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty("Expected no error diagnostics, but found: {0}",
            string.Join(", ", errors.Select(d => $"{d.Id}: {d.GetMessage()}")));
    }

    /// <summary>
    ///     Asserts that the specified generator correctly caches its pipeline outputs
    ///     when run twice on identical source code.
    /// </summary>
    /// <typeparam name="TGenerator">The generator type under test.</typeparam>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <param name="trackingNames">
    ///     Optional array of step names to validate for caching. If provided, only these
    ///     steps are checked. If omitted, all observable (non-infrastructure) steps are validated.
    /// </param>
    /// <returns>A task that completes when the assertion is finished.</returns>
    /// <remarks>
    ///     <para>
    ///         This method validates three aspects of generator caching:
    ///         <list type="number">
    ///             <item>
    ///                 <description>No forbidden types: Ensures no Roslyn runtime types are cached</description>
    ///             </item>
    ///             <item>
    ///                 <description>Proper caching: Steps report Cached or Unchanged on second run</description>
    ///             </item>
    ///             <item>
    ///                 <description>Performance: The second run should not be materially slower</description>
    ///             </item>
    ///         </list>
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
        var (firstRun, secondRun) = await engine.WithSource(source).RunTwiceAsync();

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

    /// <summary>
    ///     Validates caching and behavior across a controlled <see cref="Compilation" /> change
    ///     that simulates an IDE edit.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <param name="makeChange">
    ///     A pure function that returns a modified compilation representing the edit.
    /// </param>
    /// <param name="validate">
    ///     Optional custom validations against the resulting <see cref="CompilationCacheResult" />.
    /// </param>
    /// <returns>A task that completes when all validations finish.</returns>
    /// <example>
    ///     <code>
    /// await "public class Person { }"
    ///     .ShouldCacheWithCompilationUpdate&lt;MyGenerator&gt;(
    ///         comp => comp.AddSyntaxTrees(CSharpSyntaxTree.ParseText("public class Address { }")),
    ///         result => result.ShouldHaveCached("Person.Builder.g.cs"));
    /// </code>
    /// </example>
    public static async Task ShouldCacheWithCompilationUpdate<TGenerator>(this string source,
        Func<Compilation, Compilation> makeChange,
        Action<CompilationCacheResult>? validate = null)
        where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();

        var compilation1 = await engine.CreateCompilationAsync(source);
        var driver = GeneratorTestEngine<TGenerator>.CreateDriver(true);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation1, out var output1, out var diagnostics1);

        var compilation2 = makeChange(compilation1);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation2, out var output2, out var diagnostics2);

        CompilationCacheResult result = new(output1, output2, diagnostics1, diagnostics2, driver.GetRunResult());

        result.ValidateCaching();
        validate?.Invoke(result);
    }

    /// <summary>
    ///     Convenience overload that simulates replacing the first syntax tree with edited source.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="source">Original C# source code.</param>
    /// <param name="editedSource">Edited source for the first document.</param>
    /// <returns>A task that completes when validations finish.</returns>
    /// <example>
    ///     <code>
    /// await "public class Person { }"
    ///     .ShouldRegenerate&lt;MyGenerator&gt;("""
    ///         public class Person
    ///         {
    ///             public string Name { get; set; }
    ///         }
    ///     """);
    /// </code>
    /// </example>
    public static Task ShouldRegenerate<TGenerator>(this string source, string editedSource)
        where TGenerator : IIncrementalGenerator, new()
    {
        return source.ShouldCacheWithCompilationUpdate<TGenerator>(compilation =>
        {
            var parseOptions = new CSharpParseOptions(TestConfiguration.LanguageVersion);
            var tree = CSharpSyntaxTree.ParseText(editedSource, parseOptions);
            return compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), tree);
        });
    }

    /// <summary>
    ///     Convenience overload that simulates adding a new document to the project.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="source">Original C# source code.</param>
    /// <param name="newFileContent">Content of the additional C# file to add.</param>
    /// <returns>A task that completes when validations finish.</returns>
    /// <remarks>
    ///     This is useful for testing that adding an unrelated file doesn't cause
    ///     unnecessary regeneration of existing outputs.
    /// </remarks>
    /// <example>
    ///     <code>
    /// await "public class Person { }"
    ///     .ShouldNotRegenerate&lt;MyGenerator&gt;("public class UnrelatedClass { }");
    /// </code>
    /// </example>
    public static Task ShouldNotRegenerate<TGenerator>(this string source, string newFileContent)
        where TGenerator : IIncrementalGenerator, new()
    {
        var parseOptions = new CSharpParseOptions(TestConfiguration.LanguageVersion);
        return source.ShouldCacheWithCompilationUpdate<TGenerator>(compilation =>
            compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(newFileContent, parseOptions)));
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
}