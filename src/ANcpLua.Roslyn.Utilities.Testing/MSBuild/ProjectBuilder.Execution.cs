using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

public partial class ProjectBuilder
{
    /// <summary>
    ///     Executes an arbitrary dotnet command and returns the result.
    /// </summary>
    /// <param name="command">The dotnet command to execute (e.g., "build", "run", "test", "pack", "restore", "new").</param>
    /// <param name="arguments">Optional additional arguments to pass to the command.</param>
    /// <param name="environmentVariables">Optional environment variables to set during execution.</param>
    /// <returns>A <see cref="BuildResult" /> containing the command output, SARIF diagnostics (if available), and binary log.</returns>
    /// <remarks>
    ///     <para>This method provides low-level access to the dotnet CLI for custom scenarios.</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Automatically adds <c>/bl</c> flag for binary log generation.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Isolates the build environment by removing interfering environment variables (CI, GITHUB_*,
    ///                 MSBuild*, RUNNER_*).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Configures DOTNET_ROOT and related variables for proper SDK resolution.</description>
    ///         </item>
    ///         <item>
    ///             <description>Logs all files and command output when a test output helper is configured.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Custom dotnet command
    /// var result = await builder.ExecuteDotnetCommandAsync("format", ["--verify-no-changes"]);
    ///
    /// // dotnet new with template
    /// var result = await builder.ExecuteDotnetCommandAsync("new", ["console", "-n", "MyApp"]);
    /// </code>
    /// </example>
    /// <seealso cref="BuildAsync" />
    /// <seealso cref="BuildResult" />
    public virtual async Task<BuildResult> ExecuteDotnetCommandAsync(string command, string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        BuildCount++;

        if (TestOutputHelper is not null)
        {
            foreach (var file in System.IO.Directory.GetFiles(Directory.FullPath, "*", SearchOption.AllDirectories))
            {
                TestOutputHelper.WriteLine("File: " + file);
                var content = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                TestOutputHelper.WriteLine(content);
            }

            TestOutputHelper.WriteLine("-------- dotnet " + command);
        }

        var psi = new ProcessStartInfo(await DotNetSdkHelpers.Get(SdkVersion).ConfigureAwait(false))
        {
            WorkingDirectory = Directory.FullPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add(command);
        if (arguments is not null)
            foreach (var arg in arguments)
                psi.ArgumentList.Add(arg);

        psi.ArgumentList.Add("/bl");

        // Remove parent environment variables that can interfere
        psi.Environment.Remove("CI");
        psi.Environment.Remove("DOTNET_ENVIRONMENT");
        foreach (var kvp in psi.Environment.ToArray())
            if (kvp.Key.StartsWith("GITHUB", StringComparison.Ordinal) ||
                kvp.Key.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.StartsWith("RUNNER_", StringComparison.Ordinal))
                psi.Environment.Remove(kvp.Key);

        psi.Environment["MSBUILDLOGALLENVIRONMENTVARIABLES"] = "true";
        psi.Environment["DOTNET_ROOT"] = Path.GetDirectoryName(psi.FileName);
        psi.Environment["DOTNET_ROOT_X64"] = Path.GetDirectoryName(psi.FileName);
        psi.Environment["DOTNET_HOST_PATH"] = psi.FileName;

        if (environmentVariables is not null)
            foreach (var env in environmentVariables)
                psi.Environment[env.Name] = env.Value;

        TestOutputHelper?.WriteLine("Executing: " + psi.FileName + " " + string.Join(' ', psi.ArgumentList));

        var result = await psi.RunAsTaskAsync().ConfigureAwait(false);

        TestOutputHelper?.WriteLine("Process exit code: " + result.ExitCode);
        TestOutputHelper?.WriteLine(result.Output.ToString());

        var sarif = await LoadSarifAsync(Directory.FullPath).ConfigureAwait(false);
        var binlogContent = await File.ReadAllBytesAsync(Directory.FullPath / "msbuild.binlog").ConfigureAwait(false);
        var recordedProperties = LoadRecordedProperties(Directory.FullPath);

        return new BuildResult(result.ExitCode, result.Output, sarif, binlogContent)
        {
            RecordedProperties = recordedProperties
        };
    }

    /// <summary>
    ///     Loads the SARIF file produced by the build, transparently handling per-TFM outputs.
    /// </summary>
    /// <param name="projectDirectory">The project directory containing the SARIF file(s).</param>
    /// <returns>
    ///     A merged <see cref="SarifFile" /> containing every diagnostic across all TFMs, or
    ///     <see langword="null" /> when no SARIF file was emitted.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The default <c>GenerateCsprojFile</c> writes <c>BuildOutput.$(TargetFramework).sarif</c>
    ///         which yields a single file for single-targeted builds and one file per TFM for
    ///         cross-targeting builds. This method collapses both shapes into one
    ///         <see cref="SarifFile" /> so callers don't branch on cross-targeting.
    ///     </para>
    ///     <para>
    ///         The legacy <c>BuildOutput.sarif</c> path (without TFM suffix) is also recognised so
    ///         that callers overriding <c>GenerateCsprojFile</c> with the older single-file
    ///         <c>ErrorLog</c> declaration continue to work.
    ///     </para>
    /// </remarks>
    private static async Task<SarifFile?> LoadSarifAsync(FullPath projectDirectory)
    {
        var sarifFiles = System.IO.Directory.EnumerateFiles(projectDirectory, "BuildOutput*.sarif")
            .OrderBy(static f => f, StringComparer.Ordinal)
            .ToList();

        if (sarifFiles.Count == 0)
            return null;

        if (sarifFiles.Count == 1)
        {
            var bytes = await File.ReadAllBytesAsync(sarifFiles[0]).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SarifFile>(bytes);
        }

        var allRuns = new List<SarifFileRun>();
        foreach (var path in sarifFiles)
        {
            var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            var sarif = JsonSerializer.Deserialize<SarifFile>(bytes);
            if (sarif?.Runs is not null)
                allRuns.AddRange(sarif.Runs);
        }

        return new SarifFile { Runs = [.. allRuns] };
    }
}
