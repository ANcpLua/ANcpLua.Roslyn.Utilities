using System.Text.Json.Serialization;
using Meziantou.Framework;
using Microsoft.Build.Logging.StructuredLogger;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
///     Represents the result of a dotnet build/test/pack command with fluent assertions.
/// </summary>
/// <remarks>
///     <para>
///         This record encapsulates the complete output of an MSBuild operation, including:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Process exit code and console output</description>
///         </item>
///         <item>
///             <description>SARIF diagnostics file for structured error/warning analysis</description>
///         </item>
///         <item>
///             <description>Binary log content for detailed build introspection</description>
///         </item>
///     </list>
///     <para>
///         The binary log is parsed on-demand and cached for subsequent queries, enabling
///         efficient extraction of MSBuild properties, items, and target execution status.
///     </para>
/// </remarks>
/// <param name="ExitCode">The process exit code (0 indicates success).</param>
/// <param name="ProcessOutput">The collection of process output lines (stdout and stderr).</param>
/// <param name="SarifFile">The parsed SARIF file containing diagnostics, or <see langword="null" /> if not available.</param>
/// <param name="BinaryLogContent">The raw binary log content for detailed analysis.</param>
/// <seealso cref="SarifFile" />
/// <seealso cref="ProcessOutputCollection" />
public sealed record BuildResult(
    int ExitCode,
    ProcessOutputCollection ProcessOutput,
    SarifFile? SarifFile,
    byte[] BinaryLogContent)
{
    private Build? _cachedBuild;

    /// <summary>
    ///     Gets the process output collection.
    /// </summary>
    /// <value>The collection of output lines from the build process.</value>
    /// <seealso cref="OutputContains(string, StringComparison)" />
    /// <seealso cref="OutputDoesNotContain(string, StringComparison)" />
    public ProcessOutputCollection Output => ProcessOutput;

    /// <summary>
    ///     Gets a value indicating whether the build succeeded.
    /// </summary>
    /// <value><see langword="true" /> if the exit code is 0; otherwise, <see langword="false" />.</value>
    /// <seealso cref="Failed" />
    /// <seealso cref="ExitCode" />
    public bool Succeeded => ExitCode is 0;

    /// <summary>
    ///     Gets a value indicating whether the build failed.
    /// </summary>
    /// <value><see langword="true" /> if the exit code is non-zero; otherwise, <see langword="false" />.</value>
    /// <seealso cref="Succeeded" />
    /// <seealso cref="ExitCode" />
    public bool Failed => ExitCode is not 0;

    /// <summary>
    ///     Gets or parses the binary log build object.
    /// </summary>
    /// <returns>The parsed <see cref="Build" /> object from the binary log.</returns>
    /// <remarks>
    ///     The binary log is parsed on first access and cached for subsequent calls.
    /// </remarks>
    private Build GetBuild()
    {
        if (_cachedBuild is not null)
            return _cachedBuild;

        using var stream = new MemoryStream(BinaryLogContent);
        _cachedBuild = Serialization.ReadBinLog(stream);
        return _cachedBuild;
    }

    /// <summary>
    ///     Checks if the process output contains the specified value.
    /// </summary>
    /// <param name="value">The string value to search for in the output.</param>
    /// <param name="stringComparison">The comparison type to use. Defaults to <see cref="StringComparison.Ordinal" />.</param>
    /// <returns><see langword="true" /> if any output line contains the specified value; otherwise, <see langword="false" />.</returns>
    /// <seealso cref="OutputDoesNotContain(string, StringComparison)" />
    /// <seealso cref="Output" />
    public bool OutputContains(string value, StringComparison stringComparison = StringComparison.Ordinal)
    {
        return ProcessOutput.Any(line => line.Text.Contains(value, stringComparison));
    }

    /// <summary>
    ///     Checks if the process output does not contain the specified value.
    /// </summary>
    /// <param name="value">The string value to search for in the output.</param>
    /// <param name="stringComparison">The comparison type to use. Defaults to <see cref="StringComparison.Ordinal" />.</param>
    /// <returns><see langword="true" /> if no output line contains the specified value; otherwise, <see langword="false" />.</returns>
    /// <seealso cref="OutputContains(string, StringComparison)" />
    /// <seealso cref="Output" />
    public bool OutputDoesNotContain(string value, StringComparison stringComparison = StringComparison.Ordinal)
    {
        return !ProcessOutput.Any(line => line.Text.Contains(value, stringComparison));
    }

    /// <summary>
    ///     Checks if the build has any error diagnostic.
    /// </summary>
    /// <returns><see langword="true" /> if any SARIF result has level "error"; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     Returns <see langword="false" /> if no SARIF file is available.
    /// </remarks>
    /// <seealso cref="HasError(string)" />
    /// <seealso cref="GetErrors" />
    public bool HasError()
    {
        return SarifFile?.AllResults().Any(static r => r.Level == "error") ?? false;
    }

    /// <summary>
    ///     Checks if the build has an error with the specified rule ID.
    /// </summary>
    /// <param name="ruleId">The diagnostic rule ID to search for (e.g., "CS0001").</param>
    /// <returns><see langword="true" /> if an error with the specified rule ID exists; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     Returns <see langword="false" /> if no SARIF file is available.
    /// </remarks>
    /// <seealso cref="HasError()" />
    /// <seealso cref="GetErrors" />
    public bool HasError(string ruleId)
    {
        return SarifFile?.AllResults().Any(r => r.Level == "error" && r.RuleId == ruleId) ?? false;
    }

    /// <summary>
    ///     Checks if the build has any warning diagnostic.
    /// </summary>
    /// <returns><see langword="true" /> if any SARIF result has level "warning"; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     Returns <see langword="false" /> if no SARIF file is available.
    /// </remarks>
    /// <seealso cref="HasWarning(string)" />
    /// <seealso cref="GetWarnings" />
    public bool HasWarning()
    {
        return SarifFile?.AllResults().Any(static r => r.Level == "warning") ?? false;
    }

    /// <summary>
    ///     Checks if the build has a warning with the specified rule ID.
    /// </summary>
    /// <param name="ruleId">The diagnostic rule ID to search for (e.g., "CS0162").</param>
    /// <returns><see langword="true" /> if a warning with the specified rule ID exists; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     Returns <see langword="false" /> if no SARIF file is available.
    /// </remarks>
    /// <seealso cref="HasWarning()" />
    /// <seealso cref="GetWarnings" />
    public bool HasWarning(string ruleId)
    {
        return SarifFile?.AllResults().Any(r => r.Level == "warning" && r.RuleId == ruleId) ?? false;
    }

    /// <summary>
    ///     Checks if the build has a note diagnostic with the specified rule ID.
    /// </summary>
    /// <param name="ruleId">The diagnostic rule ID to search for.</param>
    /// <returns><see langword="true" /> if a note with the specified rule ID exists; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     Returns <see langword="false" /> if no SARIF file is available.
    /// </remarks>
    /// <seealso cref="HasInfo(string)" />
    public bool HasNote(string ruleId)
    {
        return SarifFile?.AllResults().Any(r => r.Level == "note" && r.RuleId == ruleId) ?? false;
    }

    /// <summary>
    ///     Checks if the build has an info-level diagnostic with the specified rule ID.
    /// </summary>
    /// <param name="ruleId">The diagnostic rule ID to search for.</param>
    /// <returns>
    ///     <see langword="true" /> if an info diagnostic with the specified rule ID exists; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method matches diagnostics with level "note" or "none", which correspond
    ///         to the Info severity in Roslyn diagnostics.
    ///     </para>
    ///     <para>
    ///         Returns <see langword="false" /> if no SARIF file is available.
    ///     </para>
    /// </remarks>
    /// <seealso cref="HasNote(string)" />
    public bool HasInfo(string ruleId)
    {
        return SarifFile?.AllResults().Any(r =>
            r.Level is "note" or "none" && r.RuleId == ruleId) ?? false;
    }

    /// <summary>
    ///     Gets all source file paths from the binary log.
    /// </summary>
    /// <returns>A read-only collection of full file paths referenced in the build.</returns>
    /// <remarks>
    ///     This method parses the binary log to extract all source files that were part of the build.
    /// </remarks>
    /// <seealso cref="GetMsBuildItems(string)" />
    public IReadOnlyCollection<string> GetBinLogFiles()
    {
        var build = GetBuild();
        return [.. build.SourceFiles.Select(static file => file.FullPath)];
    }

    /// <summary>
    ///     Gets all MSBuild items with the specified name.
    /// </summary>
    /// <param name="name">The MSBuild item name to search for (e.g., "Compile", "Reference").</param>
    /// <returns>A list of item values (text representations) for all matching items.</returns>
    /// <remarks>
    ///     <para>
    ///         This method traverses the binary log to find all items added via the specified item group.
    ///         Common item names include:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>"Compile" - Source files to compile</description>
    ///         </item>
    ///         <item>
    ///             <description>"Reference" - Assembly references</description>
    ///         </item>
    ///         <item>
    ///             <description>"PackageReference" - NuGet package references</description>
    ///         </item>
    ///         <item>
    ///             <description>"Content" - Content files to include</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GetMsBuildPropertyValue(string)" />
    /// <seealso cref="GetBinLogFiles" />
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

    /// <summary>
    ///     Gets the value of an MSBuild property.
    /// </summary>
    /// <param name="name">The property name to retrieve (e.g., "TargetFramework", "OutputPath").</param>
    /// <returns>The property value, or <see langword="null" /> if the property was not found.</returns>
    /// <remarks>
    ///     <para>
    ///         This method finds the last occurrence of the specified property in the binary log,
    ///         which represents the final evaluated value after all property modifications.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetMsBuildItems(string)" />
    public string? GetMsBuildPropertyValue(string name)
    {
        var build = GetBuild();
        return build.FindLastDescendant<Property>(e => e.Name == name)?.Value;
    }

    /// <summary>
    ///     Checks if an MSBuild target was executed (not skipped).
    /// </summary>
    /// <param name="name">The target name to check (e.g., "Build", "CoreCompile").</param>
    /// <returns>
    ///     <see langword="true" /> if the target was found and executed; <see langword="false" /> if not found or
    ///     skipped.
    /// </returns>
    /// <remarks>
    ///     A target is considered executed if it exists in the binary log and was not skipped.
    ///     Targets may be skipped due to conditions or up-to-date checks.
    /// </remarks>
    public bool IsMsBuildTargetExecuted(string name)
    {
        var build = GetBuild();
        var target = build.FindLastDescendant<Target>(e => e.Name == name);
        if (target is null)
            return false;

        return !target.Skipped;
    }

    /// <summary>
    ///     Gets all diagnostic results from the SARIF file.
    /// </summary>
    /// <returns>An enumerable of all SARIF results across all runs, or an empty enumerable if no SARIF file is available.</returns>
    /// <seealso cref="GetErrors" />
    /// <seealso cref="GetWarnings" />
    /// <seealso cref="SarifFileRunResult" />
    public IEnumerable<SarifFileRunResult> GetAllDiagnostics() => SarifFile?.AllResults() ?? [];

    /// <summary>
    ///     Gets all error diagnostics from the SARIF file.
    /// </summary>
    /// <returns>An enumerable of SARIF results with level "error".</returns>
    /// <seealso cref="GetAllDiagnostics" />
    /// <seealso cref="GetWarnings" />
    /// <seealso cref="HasError()" />
    public IEnumerable<SarifFileRunResult> GetErrors()
    {
        return GetAllDiagnostics().Where(static r => r.Level == "error");
    }

    /// <summary>
    ///     Gets all warning diagnostics from the SARIF file.
    /// </summary>
    /// <returns>An enumerable of SARIF results with level "warning".</returns>
    /// <seealso cref="GetAllDiagnostics" />
    /// <seealso cref="GetErrors" />
    /// <seealso cref="HasWarning()" />
    public IEnumerable<SarifFileRunResult> GetWarnings()
    {
        return GetAllDiagnostics().Where(static r => r.Level == "warning");
    }
}

/// <summary>
///     Represents a SARIF (Static Analysis Results Interchange Format) file.
/// </summary>
/// <remarks>
///     <para>
///         SARIF is a standard format for the output of static analysis tools.
///         This class provides a simplified representation for accessing diagnostic results
///         from MSBuild operations.
///     </para>
///     <para>
///         For more information about SARIF, see the OASIS SARIF specification.
///     </para>
/// </remarks>
/// <seealso cref="SarifFileRun" />
/// <seealso cref="SarifFileRunResult" />
public sealed class SarifFile
{
    /// <summary>
    ///     Gets or sets the collection of runs in this SARIF file.
    /// </summary>
    /// <value>An array of <see cref="SarifFileRun" /> objects, or <see langword="null" /> if not present.</value>
    [JsonPropertyName("runs")]
    public SarifFileRun[]? Runs { get; init; }

    /// <summary>
    ///     Gets all results from all runs in this SARIF file.
    /// </summary>
    /// <returns>An enumerable of all <see cref="SarifFileRunResult" /> objects across all runs.</returns>
    /// <remarks>
    ///     Returns an empty enumerable if <see cref="Runs" /> is <see langword="null" />.
    /// </remarks>
    public IEnumerable<SarifFileRunResult> AllResults()
    {
        return Runs?.SelectMany(static r => r.Results ?? []) ?? [];
    }
}

/// <summary>
///     Represents a single diagnostic result within a SARIF run.
/// </summary>
/// <remarks>
///     Each result corresponds to a single diagnostic reported by the analysis tool,
///     such as an error, warning, or informational message.
/// </remarks>
/// <seealso cref="SarifFile" />
/// <seealso cref="SarifFileRun" />
/// <seealso cref="SarifFileRunResultMessage" />
public sealed class SarifFileRunResult
{
    /// <summary>
    ///     Gets or sets the rule ID that produced this result.
    /// </summary>
    /// <value>The diagnostic rule identifier (e.g., "CS0001", "CA1000"), or <see langword="null" /> if not specified.</value>
    [JsonPropertyName("ruleId")]
    public string? RuleId { get; init; }

    /// <summary>
    ///     Gets or sets the severity level of this result.
    /// </summary>
    /// <value>
    ///     The severity level as a string. Common values are:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>"error" - Compilation error</description>
    ///         </item>
    ///         <item>
    ///             <description>"warning" - Compilation warning</description>
    ///         </item>
    ///         <item>
    ///             <description>"note" - Informational message</description>
    ///         </item>
    ///         <item>
    ///             <description>"none" - No severity specified</description>
    ///         </item>
    ///     </list>
    /// </value>
    [JsonPropertyName("level")]
    public string? Level { get; init; }

    /// <summary>
    ///     Gets or sets the message associated with this result.
    /// </summary>
    /// <value>
    ///     The <see cref="SarifFileRunResultMessage" /> containing the diagnostic message, or <see langword="null" /> if
    ///     not present.
    /// </value>
    [JsonPropertyName("message")]
    public SarifFileRunResultMessage? Message { get; init; }

    /// <summary>
    ///     Returns a string representation of this result.
    /// </summary>
    /// <returns>A string in the format "level:ruleId message".</returns>
    public override string ToString() => $"{Level}:{RuleId} {Message}";
}

/// <summary>
///     Represents the message content of a SARIF result.
/// </summary>
/// <seealso cref="SarifFileRunResult" />
public sealed class SarifFileRunResultMessage
{
    /// <summary>
    ///     Gets or sets the text content of the message.
    /// </summary>
    /// <value>The diagnostic message text, or <see langword="null" /> if not specified.</value>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>
    ///     Returns the message text.
    /// </summary>
    /// <returns>The <see cref="Text" /> value, or an empty string if <see cref="Text" /> is <see langword="null" />.</returns>
    public override string ToString() => Text ?? "";
}

/// <summary>
///     Represents a single run within a SARIF file.
/// </summary>
/// <remarks>
///     A SARIF file can contain multiple runs, each representing a separate analysis execution.
///     Each run contains its own collection of results.
/// </remarks>
/// <seealso cref="SarifFile" />
/// <seealso cref="SarifFileRunResult" />
public sealed class SarifFileRun
{
    /// <summary>
    ///     Gets or sets the results produced by this run.
    /// </summary>
    /// <value>An array of <see cref="SarifFileRunResult" /> objects, or <see langword="null" /> if no results.</value>
    [JsonPropertyName("results")]
    public SarifFileRunResult[]? Results { get; init; }
}
