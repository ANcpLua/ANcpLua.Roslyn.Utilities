using ANcpLua.Roslyn.Utilities.Testing.Formatting;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Unified result object for generator tests that collects all assertions and failures before throwing.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="GeneratorResult" /> implements a deferred assertion pattern. Assertions are accumulated
///         during the test and only evaluated when <see cref="Verify" /> is called or when the object is disposed.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>All assertions are collected; the test does not fail on the first assertion failure.</description>
///         </item>
///         <item>
///             <description>Dispose automatically calls <see cref="Verify" />, making <c>using</c> statements convenient.</description>
///         </item>
///         <item>
///             <description>Failures include rich context: generator name, generated files, and source code.</description>
///         </item>
///         <item>
///             <description>Fluent API allows chaining multiple assertions in a single expression.</description>
///         </item>
///     </list>
///     <example>
///         <code>
///         using var result = await Test&lt;MyGenerator&gt;.Run(source);
///         result
///             .Produces("Output.g.cs", expectedContent)
///             .IsCached()
///             .IsClean();
///         // Verify() called automatically on dispose
///         </code>
///     </example>
/// </remarks>
/// <seealso cref="GeneratedFile" />
/// <seealso cref="GeneratorCachingReport" />
/// <seealso cref="GeneratorAssertionException" />
public sealed class GeneratorResult : IDisposable
{
    private readonly Lazy<GeneratorCachingReport> _cachingReport;
    private readonly Lazy<IReadOnlyList<Diagnostic>> _diagnostics;
    private readonly List<string> _failures = [];
    private readonly Lazy<IReadOnlyList<GeneratedFile>> _files;
    private readonly Type _generatorType;
    private readonly string? _source;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratorResult" /> class.
    /// </summary>
    /// <param name="firstRun">The result of the first generator driver run.</param>
    /// <param name="secondRun">The result of the second generator driver run, used for caching verification.</param>
    /// <param name="source">The original source code used for the test, if available.</param>
    /// <param name="generatorType">The type of the generator being tested.</param>
    public GeneratorResult(
        GeneratorDriverRunResult firstRun,
        GeneratorDriverRunResult secondRun,
        string? source,
        Type generatorType)
    {
        FirstRun = firstRun;
        SecondRun = secondRun;
        _source = source;
        _generatorType = generatorType;

        _files = new Lazy<IReadOnlyList<GeneratedFile>>(() =>
            FirstRun.Results
                .SelectMany(static r => r.GeneratedSources)
                .Select(static s => new GeneratedFile(s.HintName, s.SourceText.ToString()))
                .ToList());

        _diagnostics = new Lazy<IReadOnlyList<Diagnostic>>(() =>
            FirstRun.Results
                .SelectMany(static r => r.Diagnostics)
                .ToList());

        _cachingReport = new Lazy<GeneratorCachingReport>(() =>
            GeneratorCachingReport.Create(FirstRun, SecondRun, _generatorType));
    }

    /// <summary>
    ///     Gets the generated source files from the first run.
    /// </summary>
    /// <value>
    ///     An enumerable of <see cref="GeneratedFile" /> instances representing all files
    ///     produced by the generator during the first run.
    /// </value>
    /// <remarks>
    ///     <para>Files are lazily loaded on first access.</para>
    /// </remarks>
    /// <seealso cref="GeneratedFile" />
    public IEnumerable<GeneratedFile> Files => _files.Value;

    /// <summary>
    ///     Gets all diagnostics reported by the generator.
    /// </summary>
    /// <value>
    ///     A read-only list of <see cref="Diagnostic" /> instances reported during generation.
    /// </value>
    /// <remarks>
    ///     <para>Diagnostics are lazily loaded on first access.</para>
    ///     <para>Use <see cref="HasDiagnostic" /> or <see cref="HasNoDiagnostic" /> for assertions.</para>
    /// </remarks>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Value;

    /// <summary>
    ///     Gets the caching report comparing first and second runs.
    /// </summary>
    /// <value>
    ///     A <see cref="GeneratorCachingReport" /> containing detailed caching analysis.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         The caching report is lazily computed on first access. It analyzes the
    ///         incremental generator's caching behavior by comparing two consecutive runs.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GeneratorCachingReport" />
    /// <seealso cref="IsCached" />
    public GeneratorCachingReport CachingReport => _cachingReport.Value;

    /// <summary>
    ///     Gets the result of the first generator run.
    /// </summary>
    /// <value>
    ///     The <see cref="GeneratorDriverRunResult" /> from the initial generator execution.
    /// </value>
    /// <remarks>
    ///     <para>This is the primary run used for output verification.</para>
    /// </remarks>
    public GeneratorDriverRunResult FirstRun { get; }

    /// <summary>
    ///     Gets the result of the second generator run (used for caching verification).
    /// </summary>
    /// <value>
    ///     The <see cref="GeneratorDriverRunResult" /> from the second generator execution.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         The second run uses the same input as the first run to verify that
    ///         the incremental generator properly caches its outputs.
    ///     </para>
    /// </remarks>
    /// <seealso cref="IsCached" />
    /// <seealso cref="CachingReport" />
    public GeneratorDriverRunResult SecondRun { get; }

    /// <summary>
    ///     Gets a generated file by its hint name.
    /// </summary>
    /// <param name="hintName">The hint name of the file to find.</param>
    /// <returns>
    ///     The <see cref="GeneratedFile" /> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>The comparison is case-insensitive.</para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var file = result["MyClass.g.cs"];
    ///     if (file is not null)
    ///     {
    ///         Console.WriteLine(file.Content);
    ///     }
    ///     </code>
    /// </example>
    public GeneratedFile? this[string hintName] =>
        Files.FirstOrDefault(f => f.HintName.Equals(hintName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Disposes the result and verifies all accumulated assertions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method calls <see cref="Verify" /> to throw a <see cref="GeneratorAssertionException" />
    ///         if any assertions failed. It is safe to call multiple times.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Verify" />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Verify();
    }

    /// <summary>
    ///     Asserts that the generator produced a file with the expected content.
    /// </summary>
    /// <param name="hintName">The hint name of the expected file.</param>
    /// <param name="expectedContent">The expected content of the file.</param>
    /// <param name="exactMatch">
    ///     If <c>true</c>, content must match exactly; otherwise, content must contain the expected text.
    /// </param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Line endings are normalized before comparison.</description>
    ///         </item>
    ///         <item>
    ///             <description>If the file is missing, the assertion fails with a list of available files.</description>
    ///         </item>
    ///         <item>
    ///             <description>Content mismatch shows a detailed diff in the failure message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     result
    ///         .Produces("Output.g.cs", expectedCode, exactMatch: true)
    ///         .Produces("Partial.g.cs", "partial class", exactMatch: false);
    ///     </code>
    /// </example>
    /// <seealso cref="Produces(string)" />
    /// <seealso cref="File" />
    public GeneratorResult Produces(string hintName, string expectedContent, bool exactMatch = true)
    {
        var file = this[hintName];
        if (file is null)
        {
            Fail($"Missing file '{hintName}'", AssertionHelpers.FormatFileList(Files));
            return this;
        }

        var actual = file.Content.NormalizeLineEndings();
        var expected = expectedContent.NormalizeLineEndings();

        if (exactMatch ? actual != expected : !actual.Contains(expected, StringComparison.Ordinal))
            Fail($"Content mismatch in '{hintName}'",
                ReportFormatter.FormatContentFailure(hintName, actual, expected, exactMatch));

        return this;
    }

    /// <summary>
    ///     Asserts that the generator produced a file with the specified hint name.
    /// </summary>
    /// <param name="hintName">The hint name of the expected file.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This overload only checks for file existence, not content.
    ///         Use <see cref="Produces(string, string, bool)" /> to also verify content.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     result.Produces("Generated.g.cs");
    ///     </code>
    /// </example>
    /// <seealso cref="Produces(string, string, bool)" />
    public GeneratorResult Produces(string hintName)
    {
        if (this[hintName] is null) Fail($"Missing file '{hintName}'", AssertionHelpers.FormatFileList(Files));

        return this;
    }

    /// <summary>
    ///     Asserts that the generator produced no diagnostics.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         A "clean" run means zero diagnostics of any severity (errors, warnings, info, hidden).
    ///         Use <see cref="Compiles" /> if you only want to check for compilation errors.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     result.IsClean(); // Fails if any diagnostics exist
    ///     </code>
    /// </example>
    /// <seealso cref="Compiles" />
    /// <seealso cref="HasNoDiagnostic" />
    public GeneratorResult IsClean()
    {
        if (Diagnostics.Count > 0)
            Fail($"{Diagnostics.Count} unexpected diagnostics", AssertionHelpers.FormatDiagnosticList(Diagnostics));

        return this;
    }

    /// <summary>
    ///     Asserts that the generated code compiles without errors.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This assertion only checks for <see cref="DiagnosticSeverity.Error" /> diagnostics.
    ///         Warnings and other severities are allowed. Use <see cref="IsClean" /> to reject all diagnostics.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     result.Compiles(); // Fails only on errors, warnings are OK
    ///     </code>
    /// </example>
    /// <seealso cref="IsClean" />
    public GeneratorResult Compiles()
    {
        var errors = Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0) Fail($"{errors.Count} compilation errors", AssertionHelpers.FormatErrorList(Diagnostics));

        return this;
    }

    /// <summary>
    ///     Asserts that generator outputs are properly cached across runs.
    /// </summary>
    /// <param name="stepNames">
    ///     Optional step names to verify exist in the pipeline. When provided, the assertion
    ///     verifies these steps are present (using prefix matching for hierarchical names).
    ///     All observable steps are always validated for cache failures regardless of this parameter.
    /// </param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Always validates ALL observable steps for cache invalidation (Modified/New/Removed).
    ///                 Step names do not filter which steps are checked — they assert step existence.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Checks for forbidden types (ISymbol, Compilation) in cached outputs.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Step name matching uses prefix comparison: <c>"EndpointBindingFlow"</c> matches
    ///                 <c>"EndpointBindingFlow"</c>, <c>"EndpointBindingFlow.Get"</c>, etc.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     result.IsCached();                                    // Check all steps
    ///     result.IsCached("TransformStep", "CombineStep");      // Check all steps + verify these exist
    ///     result.IsCached("EndpointBindingFlow");               // Matches EndpointBindingFlow.Get, .Post, etc.
    ///     </code>
    /// </example>
    /// <seealso cref="CachingReport" />
    /// <seealso cref="HasNoForbiddenTypes" />
    public GeneratorResult IsCached(params string[] stepNames)
    {
        var report = CachingReport;

        // Forbidden type check: when step names are provided, only check matching steps.
        // This allows intentional Roslyn type usage in upstream steps (e.g., ErrorOrContext holds
        // INamedTypeSymbol fields by design, mitigated via IEquatable with null-presence bitmask).
        var violationsToCheck = stepNames.Length > 0
            ? report.ForbiddenTypeViolations
                .Where(v => stepNames.Any(name => MatchesStepName(v.StepName, name)))
                .ToList()
            : report.ForbiddenTypeViolations;

        if (violationsToCheck.Count > 0)
            Fail("Forbidden types cached", ViolationFormatter.FormatGrouped(violationsToCheck));

        // Caching check: ALWAYS check ALL observable steps for cache invalidation.
        // Observable steps exclude Roslyn internals (Compilation, ForAttributeWithMetadataName, etc.)
        // and only contain user-defined pipeline steps. This prevents false positives where a
        // downstream step appears cached (Unchanged) but an upstream step has broken equality
        // (Modified), causing unnecessary re-computation.
        var failedSteps = report.ObservableSteps.Where(static s => !s.IsCachedSuccessfully).ToList();
        if (failedSteps.Count > 0)
            Fail($"{failedSteps.Count} steps not cached", AssertionHelpers.FormatFailedSteps(failedSteps));

        // When step names are provided, verify those steps exist in the pipeline.
        // Uses prefix matching to support hierarchical step names (e.g., "Flow" matches "Flow.Get").
        if (stepNames.Length > 0)
        {
            var observedNames = report.ObservableSteps.Select(static s => s.StepName).ToList();
            foreach (var required in stepNames)
            {
                if (!observedNames.Any(name => MatchesStepName(name, required)))
                    Fail($"Required pipeline step not found: '{required}'",
                        $"Available steps: {AssertionHelpers.FormatList(observedNames)}");
            }
        }

        return this;
    }

    /// <summary>
    ///     Checks if a step name matches a required name using prefix matching.
    ///     Supports hierarchical names: "Flow" matches "Flow" and "Flow.Get".
    /// </summary>
    private static bool MatchesStepName(string stepName, string required) =>
        stepName.Equals(required, StringComparison.Ordinal) ||
        stepName.StartsWith(required + ".", StringComparison.Ordinal);

    /// <summary>
    ///     Asserts that the generator produced a diagnostic with the specified ID.
    /// </summary>
    /// <param name="id">The expected diagnostic ID (e.g., "CS0001", "MYDIAG001").</param>
    /// <param name="severity">Optional severity filter. If provided, the diagnostic must also match this severity.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Use this to verify that the generator produces expected warnings or errors.
    ///         The assertion fails if no diagnostic with the specified ID (and optional severity) exists.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     result
    ///         .HasDiagnostic("GEN001")
    ///         .HasDiagnostic("GEN002", DiagnosticSeverity.Warning);
    ///     </code>
    /// </example>
    /// <seealso cref="HasNoDiagnostic" />
    /// <seealso cref="Diagnostics" />
    public GeneratorResult HasDiagnostic(string id, DiagnosticSeverity? severity = null)
    {
        var matches = Diagnostics.Where(d => d.Id == id && (severity is null || d.Severity == severity)).ToList();
        if (matches.Count is 0)
        {
            var what = severity.HasValue ? $"'{id}' ({severity})" : $"'{id}'";
            Fail($"Missing diagnostic {what}", AssertionHelpers.FormatDiagnosticIds(Diagnostics));
        }

        return this;
    }

    /// <summary>
    ///     Asserts that the generator did not produce a diagnostic with the specified ID.
    /// </summary>
    /// <param name="id">The diagnostic ID that should not be present.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Use this to verify that certain diagnostics are not emitted.
    ///         The assertion fails if any diagnostic with the specified ID exists.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     result.HasNoDiagnostic("GEN001"); // Fails if GEN001 is present
    ///     </code>
    /// </example>
    /// <seealso cref="HasDiagnostic" />
    /// <seealso cref="IsClean" />
    public GeneratorResult HasNoDiagnostic(string id)
    {
        if (Diagnostics.Any(d => d.Id == id)) Fail($"Unexpected diagnostic '{id}'", "");

        return this;
    }

    /// <summary>
    ///     Asserts that no forbidden types are present in the generated output.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Forbidden types include <c>ISymbol</c> and <c>Compilation</c> - types that should never
    ///         be cached by incremental generators because they are not value-equal across runs.
    ///     </para>
    ///     <para>
    ///         This is automatically checked by <see cref="IsCached" /> but can be called separately
    ///         if you only want to verify forbidden type usage without full caching validation.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     result.HasNoForbiddenTypes();
    ///     </code>
    /// </example>
    /// <seealso cref="IsCached" />
    /// <seealso cref="ForbiddenTypeAnalyzer" />
    public GeneratorResult HasNoForbiddenTypes()
    {
        var violations = ForbiddenTypeAnalyzer.AnalyzeGeneratorRun(FirstRun);
        if (violations.Count > 0) Fail("Forbidden types detected", ViolationFormatter.FormatGrouped(violations));

        return this;
    }

    /// <summary>
    ///     Executes a custom assertion on a generated file's content.
    /// </summary>
    /// <param name="hintName">The hint name of the file to inspect.</param>
    /// <param name="assert">The assertion action to execute on the file content.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>If the file is missing, the assertion fails without invoking the action.</description>
    ///         </item>
    ///         <item>
    ///             <description>Exceptions thrown by the action are caught and recorded as assertion failures.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Use this for complex content validations that cannot be expressed with
    ///                 <see cref="Produces(string, string, bool)" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     result.File("Output.g.cs", content =>
    ///     {
    ///         Assert.Contains("public partial class", content);
    ///         Assert.DoesNotContain("private", content);
    ///     });
    ///     </code>
    /// </example>
    /// <seealso cref="Produces(string, string, bool)" />
    public GeneratorResult File(string hintName, Action<string> assert)
    {
        var file = this[hintName];
        if (file is null)
        {
            Fail($"Missing file '{hintName}'", AssertionHelpers.FormatFileList(Files));
            return this;
        }

        try
        {
            assert(file.Content);
        }
        catch (Exception ex)
        {
            Fail($"Custom assertion failed for '{hintName}'", ex.Message);
        }

        return this;
    }

    private void Fail(string issue, string details)
    {
        var msg = string.IsNullOrEmpty(details) ? issue : $"{issue}\n{details}";
        _failures.Add(msg);
    }

    /// <summary>
    ///     Throws a <see cref="GeneratorAssertionException" /> if any assertions failed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is called automatically on <see cref="Dispose" />. It can also be called
    ///         explicitly if you want to verify at a specific point without disposing.
    ///     </para>
    ///     <para>
    ///         The exception message includes:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>The generator type name and total failure count.</description>
    ///         </item>
    ///         <item>
    ///             <description>Each failure with its details, numbered sequentially.</description>
    ///         </item>
    ///         <item>
    ///             <description>Generator context including generated files and original source.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <exception cref="GeneratorAssertionException">Thrown when one or more assertions failed.</exception>
    /// <seealso cref="Dispose" />
    public void Verify()
    {
        if (_failures.Count is 0) return;

        var sb = new StringBuilder();
        sb.AppendLine($"Generator '{_generatorType.Name}' failed {_failures.Count} assertion(s):");
        sb.AppendLine();

        for (var i = 0; i < _failures.Count; i++)
        {
            sb.AppendLine($"─── FAILURE {i + 1} ───");
            sb.AppendLine(_failures[i]);
            sb.AppendLine();
        }

        sb.AppendLine("═══ GENERATOR CONTEXT ═══");
        sb.AppendLine(ReportFormatter.FormatGeneratorContext(FirstRun, _source));

        throw new GeneratorAssertionException(sb.ToString());
    }
}

/// <summary>
///     Represents a generated source file with its hint name and content.
/// </summary>
/// <param name="HintName">The hint name used to identify the generated file.</param>
/// <param name="Content">The full source code content of the generated file.</param>
/// <remarks>
///     <para>
///         The hint name is typically the filename (e.g., "MyClass.g.cs") and is used
///         to identify generated files in assertions and diagnostics.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     var file = result["MyClass.g.cs"];
///     Console.WriteLine($"File: {file.HintName}");
///     Console.WriteLine($"Content: {file.Content}");
///     </code>
/// </example>
/// <seealso cref="GeneratorResult.Files" />
/// <seealso cref="GeneratorResult.this[string]" />
public sealed record GeneratedFile(string HintName, string Content);

/// <summary>
///     Exception thrown when generator assertions fail.
/// </summary>
/// <remarks>
///     <para>
///         This exception is thrown by <see cref="GeneratorResult.Verify" /> when one or more
///         assertions have failed. The exception message contains detailed information about
///         all failures and the generator context.
///     </para>
/// </remarks>
/// <seealso cref="GeneratorResult" />
/// <seealso cref="GeneratorResult.Verify" />
public sealed class GeneratorAssertionException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratorAssertionException" /> class.
    /// </summary>
    public GeneratorAssertionException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratorAssertionException" /> class
    ///     with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the assertion failures.</param>
    public GeneratorAssertionException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratorAssertionException" /> class
    ///     with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the assertion failures.</param>
    /// <param name="innerException">
    ///     The exception that is the cause of this exception, or <c>null</c> if no inner exception is specified.
    /// </param>
    public GeneratorAssertionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}