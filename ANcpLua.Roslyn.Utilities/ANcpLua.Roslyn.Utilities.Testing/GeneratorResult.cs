using ANcpLua.Roslyn.Utilities.Testing.Formatting;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Unified result object for generator tests. Collects ALL failures before throwing.
/// </summary>
public sealed class GeneratorResult : IDisposable
{
    private readonly Lazy<GeneratorCachingReport> _cachingReport;
    private readonly Lazy<IReadOnlyList<Diagnostic>> _diagnostics;
    private readonly List<string> _failures = [];
    private readonly Lazy<IReadOnlyList<GeneratedFile>> _files;
    private readonly Type _generatorType;
    private readonly string? _source;
    private bool _disposed;

    internal GeneratorResult(
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
    public IEnumerable<GeneratedFile> Files => _files.Value;

    /// <summary>
    ///     Gets all diagnostics reported by the generator.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Value;

    /// <summary>
    ///     Gets the caching report comparing first and second runs.
    /// </summary>
    public GeneratorCachingReport CachingReport => _cachingReport.Value;

    /// <summary>
    ///     Gets the result of the first generator run.
    /// </summary>
    public GeneratorDriverRunResult FirstRun { get; }

    /// <summary>
    ///     Gets the result of the second generator run (used for caching verification).
    /// </summary>
    public GeneratorDriverRunResult SecondRun { get; }

    /// <summary>
    ///     Gets a generated file by its hint name.
    /// </summary>
    /// <param name="hintName">The hint name of the file to find.</param>
    /// <returns>The generated file if found; otherwise, <c>null</c>.</returns>
    public GeneratedFile? this[string hintName] =>
        Files.FirstOrDefault(f => f.HintName.Equals(hintName, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
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
    /// <param name="exactMatch">If <c>true</c>, content must match exactly; otherwise, content must contain the expected text.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public GeneratorResult Produces(string hintName, string expectedContent, bool exactMatch = true)
    {
        var file = this[hintName];
        if (file is null)
        {
            Fail($"Missing file '{hintName}'", AssertionHelpers.FormatFileList(Files));
            return this;
        }

        var actual = TextUtilities.NormalizeNewlines(file.Content);
        var expected = TextUtilities.NormalizeNewlines(expectedContent);

        if (exactMatch ? actual != expected : !actual.Contains(expected, StringComparison.Ordinal))
        {
            Fail($"Content mismatch in '{hintName}'",
                ReportFormatter.FormatContentFailure(hintName, actual, expected, exactMatch));
        }

        return this;
    }

    /// <summary>
    ///     Asserts that the generator produced a file with the specified hint name.
    /// </summary>
    /// <param name="hintName">The hint name of the expected file.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public GeneratorResult Produces(string hintName)
    {
        if (this[hintName] is null)
        {
            Fail($"Missing file '{hintName}'", AssertionHelpers.FormatFileList(Files));
        }

        return this;
    }

    /// <summary>
    ///     Asserts that the generator produced no diagnostics.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public GeneratorResult IsClean()
    {
        if (Diagnostics.Count > 0)
        {
            Fail($"{Diagnostics.Count} unexpected diagnostics", AssertionHelpers.FormatDiagnosticList(Diagnostics));
        }

        return this;
    }

    /// <summary>
    ///     Asserts that the generated code compiles without errors.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public GeneratorResult Compiles()
    {
        var errors = Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            Fail($"{errors.Count} compilation errors", AssertionHelpers.FormatErrorList(Diagnostics));
        }

        return this;
    }

    /// <summary>
    ///     Asserts that generator outputs are properly cached across runs.
    /// </summary>
    /// <param name="stepNames">Optional step names to check; if empty, all steps are verified.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public GeneratorResult IsCached(params string[] stepNames)
    {
        var report = CachingReport;

        if (report.ForbiddenTypeViolations.Count > 0)
        {
            Fail("Forbidden types cached", ViolationFormatter.FormatGrouped(report.ForbiddenTypeViolations));
        }

        var stepsToCheck = stepNames.Length > 0
            ? report.ObservableSteps.Where(s => stepNames.Contains(s.StepName, StringComparer.Ordinal)).ToList()
            : report.ObservableSteps;

        var failedSteps = stepsToCheck.Where(static s => !s.IsCachedSuccessfully).ToList();
        if (failedSteps.Count > 0)
        {
            Fail($"{failedSteps.Count} steps not cached", AssertionHelpers.FormatFailedSteps(failedSteps));
        }

        return this;
    }

    /// <summary>
    ///     Asserts that the generator produced a diagnostic with the specified ID.
    /// </summary>
    /// <param name="id">The expected diagnostic ID.</param>
    /// <param name="severity">Optional severity filter.</param>
    /// <returns>This instance for fluent chaining.</returns>
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
    public GeneratorResult HasNoDiagnostic(string id)
    {
        if (Diagnostics.Any(d => d.Id == id))
        {
            Fail($"Unexpected diagnostic '{id}'", "");
        }

        return this;
    }

    /// <summary>
    ///     Asserts that no forbidden types are present in the generated output.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public GeneratorResult HasNoForbiddenTypes()
    {
        var violations = ForbiddenTypeAnalyzer.AnalyzeGeneratorRun(FirstRun);
        if (violations.Count > 0)
        {
            Fail("Forbidden types detected", ViolationFormatter.FormatGrouped(violations));
        }

        return this;
    }

    /// <summary>
    ///     Executes a custom assertion on a generated file's content.
    /// </summary>
    /// <param name="hintName">The hint name of the file to inspect.</param>
    /// <param name="assert">The assertion action to execute on the file content.</param>
    /// <returns>This instance for fluent chaining.</returns>
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
    ///     Throws if any assertions failed. Called automatically on Dispose.
    /// </summary>
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
///     Represents a generated source file.
/// </summary>
public sealed record GeneratedFile(string HintName, string Content);

/// <summary>
///     Exception thrown when generator assertions fail.
/// </summary>
public sealed class GeneratorAssertionException : Exception
{
    /// <inheritdoc />
    public GeneratorAssertionException() { }

    /// <inheritdoc />
    public GeneratorAssertionException(string message) : base(message) { }

    /// <inheritdoc />
    public GeneratorAssertionException(string message, Exception innerException) : base(message, innerException) { }
}
