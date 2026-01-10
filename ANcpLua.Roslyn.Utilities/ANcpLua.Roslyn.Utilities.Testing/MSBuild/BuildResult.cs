using System.Text.Json.Serialization;
using Meziantou.Framework;
using Microsoft.Build.Logging.StructuredLogger;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
/// Represents the result of a dotnet build/test/pack command with fluent assertions.
/// </summary>
public sealed record BuildResult(
    int ExitCode,
    ProcessOutputCollection ProcessOutput,
    SarifFile? SarifFile,
    byte[] BinaryLogContent)
{
    private Build? _cachedBuild;

    /// <summary>Gets the process output.</summary>
    public ProcessOutputCollection Output => ProcessOutput;

    /// <summary>Gets whether the build succeeded (exit code 0).</summary>
    public bool Succeeded => ExitCode is 0;

    /// <summary>Gets whether the build failed (non-zero exit code).</summary>
    public bool Failed => ExitCode is not 0;

    private Build GetBuild()
    {
        if (_cachedBuild is not null)
            return _cachedBuild;

        using var stream = new MemoryStream(BinaryLogContent);
        _cachedBuild = Serialization.ReadBinLog(stream);
        return _cachedBuild;
    }

    /// <summary>Checks if the output contains the specified value.</summary>
    public bool OutputContains(string value, StringComparison stringComparison = StringComparison.Ordinal)
    {
        return ProcessOutput.Any(line => line.Text.Contains(value, stringComparison));
    }

    /// <summary>Checks if the output does not contain the specified value.</summary>
    public bool OutputDoesNotContain(string value, StringComparison stringComparison = StringComparison.Ordinal)
    {
        return !ProcessOutput.Any(line => line.Text.Contains(value, stringComparison));
    }

    /// <summary>Checks if the build has any error.</summary>
    public bool HasError()
    {
        return SarifFile?.AllResults().Any(static r => r.Level == "error") ?? false;
    }

    /// <summary>Checks if the build has a specific error.</summary>
    public bool HasError(string ruleId)
    {
        return SarifFile?.AllResults().Any(r => r.Level == "error" && r.RuleId == ruleId) ?? false;
    }

    /// <summary>Checks if the build has any warning.</summary>
    public bool HasWarning()
    {
        return SarifFile?.AllResults().Any(static r => r.Level == "warning") ?? false;
    }

    /// <summary>Checks if the build has a specific warning.</summary>
    public bool HasWarning(string ruleId)
    {
        return SarifFile?.AllResults().Any(r => r.Level == "warning" && r.RuleId == ruleId) ?? false;
    }

    /// <summary>Checks if the build has a specific note.</summary>
    public bool HasNote(string ruleId)
    {
        return SarifFile?.AllResults().Any(r => r.Level == "note" && r.RuleId == ruleId) ?? false;
    }

    /// <summary>Checks if the build has a specific info diagnostic.</summary>
    public bool HasInfo(string ruleId)
    {
        return SarifFile?.AllResults().Any(r =>
            r.Level is "note" or "none" && r.RuleId == ruleId) ?? false;
    }

    /// <summary>Gets all files from the binary log.</summary>
    public IReadOnlyCollection<string> GetBinLogFiles()
    {
        var build = GetBuild();
        return [.. build.SourceFiles.Select(static file => file.FullPath)];
    }

    /// <summary>Gets all MSBuild items with the specified name.</summary>
    public List<string> GetMsBuildItems(string name)
    {
        var result = new List<string>();
        var build = GetBuild();
        build.VisitAllChildren<Microsoft.Build.Logging.StructuredLogger.Item>(item =>
        {
            if (item.Parent is AddItem parent && parent.Name == name) result.Add(item.Text);
        });

        return result;
    }

    /// <summary>Gets the value of an MSBuild property.</summary>
    public string? GetMsBuildPropertyValue(string name)
    {
        var build = GetBuild();
        return build.FindLastDescendant<Property>(e => e.Name == name)?.Value;
    }

    /// <summary>Checks if an MSBuild target was executed.</summary>
    public bool IsMsBuildTargetExecuted(string name)
    {
        var build = GetBuild();
        var target = build.FindLastDescendant<Target>(e => e.Name == name);
        if (target is null)
            return false;

        return !target.Skipped;
    }

    /// <summary>Gets all SARIF results.</summary>
    public IEnumerable<SarifFileRunResult> GetAllDiagnostics()
    {
        return SarifFile?.AllResults() ?? [];
    }

    /// <summary>Gets all errors from SARIF.</summary>
    public IEnumerable<SarifFileRunResult> GetErrors()
    {
        return GetAllDiagnostics().Where(static r => r.Level == "error");
    }

    /// <summary>Gets all warnings from SARIF.</summary>
    public IEnumerable<SarifFileRunResult> GetWarnings()
    {
        return GetAllDiagnostics().Where(static r => r.Level == "warning");
    }
}

/// <summary>SARIF file representation.</summary>
public class SarifFile
{
    [JsonPropertyName("runs")] public SarifFileRun[]? Runs { get; set; }

    /// <summary>Gets all results from all runs.</summary>
    public IEnumerable<SarifFileRunResult> AllResults()
    {
        return Runs?.SelectMany(static r => r.Results ?? []) ?? [];
    }
}

/// <summary>SARIF run result.</summary>
public class SarifFileRunResult
{
    [JsonPropertyName("ruleId")] public string? RuleId { get; set; }

    [JsonPropertyName("level")] public string? Level { get; set; }

    [JsonPropertyName("message")] public SarifFileRunResultMessage? Message { get; set; }

    public override string ToString()
    {
        return $"{Level}:{RuleId} {Message}";
    }
}

/// <summary>SARIF result message.</summary>
public class SarifFileRunResultMessage
{
    [JsonPropertyName("text")] public string? Text { get; set; }

    public override string ToString()
    {
        return Text ?? "";
    }
}

/// <summary>SARIF run.</summary>
public class SarifFileRun
{
    [JsonPropertyName("results")] public SarifFileRunResult[]? Results { get; set; }
}
