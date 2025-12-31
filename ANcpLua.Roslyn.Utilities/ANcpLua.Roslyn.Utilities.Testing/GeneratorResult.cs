using ANcpLua.Roslyn.Utilities.Testing.Formatting;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Unified result object for generator tests. Collects ALL failures before throwing.
/// </summary>
public sealed class GeneratorResult : IDisposable
{
    private readonly string? _source;
    private readonly Type _generatorType;
    private readonly List<string> _failures = [];
    private readonly Lazy<GeneratorCachingReport> _cachingReport;
    private readonly Lazy<IReadOnlyList<GeneratedFile>> _files;
    private readonly Lazy<IReadOnlyList<Diagnostic>> _diagnostics;
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

    #region Data Properties

    public IEnumerable<GeneratedFile> Files => _files.Value;
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Value;
    public GeneratorCachingReport CachingReport => _cachingReport.Value;
    public GeneratorDriverRunResult FirstRun { get; }
    public GeneratorDriverRunResult SecondRun { get; }

    public GeneratedFile? this[string hintName] =>
        Files.FirstOrDefault(f => f.HintName.Equals(hintName, StringComparison.OrdinalIgnoreCase));

    #endregion

    #region Assertions (collect failures, don't throw)

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

    public GeneratorResult Produces(string hintName)
    {
        if (this[hintName] is null)
        {
            Fail($"Missing file '{hintName}'", AssertionHelpers.FormatFileList(Files));
        }

        return this;
    }

    public GeneratorResult IsClean()
    {
        if (Diagnostics.Count > 0)
        {
            Fail($"{Diagnostics.Count} unexpected diagnostics", AssertionHelpers.FormatDiagnosticList(Diagnostics));
        }

        return this;
    }

    public GeneratorResult Compiles()
    {
        var errors = Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            Fail($"{errors.Count} compilation errors", AssertionHelpers.FormatErrorList(Diagnostics));
        }

        return this;
    }

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

    public GeneratorResult HasDiagnostic(string id, DiagnosticSeverity? severity = null)
    {
        var matches = Diagnostics.Where(d => d.Id == id && (severity is null || d.Severity == severity)).ToList();
        if (matches.Count == 0)
        {
            var what = severity.HasValue ? $"'{id}' ({severity})" : $"'{id}'";
            Fail($"Missing diagnostic {what}", AssertionHelpers.FormatDiagnosticIds(Diagnostics));
        }

        return this;
    }

    public GeneratorResult HasNoDiagnostic(string id)
    {
        if (Diagnostics.Any(d => d.Id == id))
        {
            Fail($"Unexpected diagnostic '{id}'", "");
        }

        return this;
    }

    public GeneratorResult HasNoForbiddenTypes()
    {
        var violations = ForbiddenTypeAnalyzer.AnalyzeGeneratorRun(FirstRun);
        if (violations.Count > 0)
        {
            Fail("Forbidden types detected", ViolationFormatter.FormatGrouped(violations));
        }

        return this;
    }

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

    #endregion

    #region Failure Collection

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
        if (_failures.Count == 0) return;

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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Verify();
    }

    #endregion
}

/// <summary>
///     Represents a generated source file.
/// </summary>
public sealed record GeneratedFile(string HintName, string Content)
{
    public bool Contains(string text) => Content.Contains(text, StringComparison.Ordinal);

    public bool Matches(string expected) =>
        TextUtilities.NormalizeNewlines(Content) == TextUtilities.NormalizeNewlines(expected);
}

/// <summary>
///     Exception thrown when generator assertions fail.
/// </summary>
public sealed class GeneratorAssertionException(string message) : Exception(message);