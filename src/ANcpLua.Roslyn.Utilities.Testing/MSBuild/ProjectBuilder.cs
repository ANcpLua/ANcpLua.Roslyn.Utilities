using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
///     NuGet package reference for project building.
/// </summary>
/// <param name="Name">The package identifier (e.g., "Microsoft.CodeAnalysis.CSharp").</param>
/// <param name="Version">The package version (e.g., "4.12.0").</param>
/// <seealso cref="ProjectBuilder.WithPackage" />
public readonly record struct NuGetReference(string Name, string Version);

/// <summary>
///     Fluent builder for creating and building isolated .NET projects for testing.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="ProjectBuilder" /> provides a complete solution for integration testing of MSBuild-based
///         projects, source generators, and analyzers in an isolated environment. Each instance creates a
///         temporary directory with its own global.json, NuGet configuration, and project files.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Creates isolated temporary directories with automatic cleanup via
///                 <see cref="IAsyncDisposable" />.
///             </description>
///         </item>
///         <item>
///             <description>Downloads and caches .NET SDK versions automatically via <see cref="DotNetSdkHelpers" />.</description>
///         </item>
///         <item>
///             <description>Generates SARIF output and binary logs for comprehensive build analysis.</description>
///         </item>
///         <item>
///             <description>Supports GitHub Actions CI simulation with step summary output.</description>
///         </item>
///         <item>
///             <description>Provides fluent API for configuring MSBuild properties, packages, and source files.</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// await using var builder = new ProjectBuilder(testOutputHelper);
/// 
/// var result = await builder
///     .WithTargetFramework(Tfm.Net100)
///     .WithOutputType(Val.Library)
///     .WithPackage("Microsoft.CodeAnalysis.CSharp", "4.12.0")
///     .AddSource("Program.cs", "namespace Test; public class Foo { }")
///     .BuildAsync();
/// 
/// result.ShouldSucceed();
/// </code>
/// </example>
/// <seealso cref="BuildResult" />
/// <seealso cref="BuildResultAssertions" />
/// <seealso cref="NetSdkVersion" />
public partial class ProjectBuilder : IAsyncDisposable
{
    /// <summary>
    ///     The SARIF output filename used for diagnostic output.
    /// </summary>
    protected const string SarifFileName = "BuildOutput.sarif";

    /// <summary>
    ///     Creates a new <see cref="ProjectBuilder" /> with an isolated temporary directory.
    /// </summary>
    /// <param name="testOutputHelper">
    ///     Optional xUnit test output helper for logging build output and file contents during test execution.
    ///     When provided, enables verbose logging of all files and build commands.
    /// </param>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Creates a unique temporary directory for complete build isolation.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Generates a default global.json configured for .NET 10.0 SDK with latestMinor rollForward
    ///                 policy.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Prepares a file for GitHub step summary simulation.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Without test output (silent mode)
    /// await using var builder = new ProjectBuilder();
    ///
    /// // With xUnit test output for debugging
    /// await using var builder = new ProjectBuilder(testOutputHelper);
    /// </code>
    /// </example>
    public ProjectBuilder(ITestOutputHelper? testOutputHelper = null)
    {
        TestOutputHelper = testOutputHelper;
        Directory = TemporaryDirectory.Create();
        GithubStepSummaryFile = Directory.CreateEmptyFile("GITHUB_STEP_SUMMARY.txt");

        // Create isolated global.json
        Directory.CreateTextFile("global.json", """
                                                {
                                                  "sdk": {
                                                    "rollForward": "latestMinor",
                                                    "version": "10.0.100"
                                                  }
                                                }
                                                """);
    }

    /// <summary>
    ///     The temporary directory for the project files.
    /// </summary>
    protected TemporaryDirectory Directory { get; }

    /// <summary>
    ///     Path to the GitHub step summary file for CI simulation.
    /// </summary>
    protected FullPath GithubStepSummaryFile { get; }

    /// <summary>
    ///     The NuGet package references configured for this project.
    /// </summary>
    protected List<NuGetReference> NuGetPackages { get; } = [];

    /// <summary>
    ///     The MSBuild properties configured for this project.
    /// </summary>
    protected List<(string Key, string Value)> Properties { get; } = [];

    /// <summary>
    ///     The source files to be added to the project.
    /// </summary>
    protected List<(string Name, string Content)> SourceFiles { get; } = [];

    /// <summary>
    ///     Names of MSBuild properties to record during the build.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When non-empty, <see cref="GenerateCsprojFile" /> emits a
    ///         <c>_WriteRecordedProperties</c> target that runs <c>AfterTargets="Build"</c>
    ///         and writes one <c>obj/recorded.{TargetFramework}.properties</c> file per TFM
    ///         containing <c>Name=Value</c> lines for every requested property.
    ///     </para>
    ///     <para>
    ///         <see cref="ExecuteDotnetCommandAsync" /> reads those files into
    ///         <see cref="BuildResult.RecordedProperties" />. Compared to scanning the binlog
    ///         this is deterministic across cross-targeting builds (one file per TFM, no
    ///         shared sink) and immune to structured-logger cold-cache event loss observed on
    ///         Windows runners. Mirrors <c>dotnet/sdk</c>'s <c>GetValuesCommand</c> pattern.
    ///     </para>
    /// </remarks>
    protected HashSet<string> PropertiesToRecord { get; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     The test output helper for logging, if provided.
    /// </summary>
    protected ITestOutputHelper? TestOutputHelper { get; }

    /// <summary>
    ///     Counter for the number of build operations performed.
    /// </summary>
    protected int BuildCount { get; set; }

    /// <summary>
    ///     The project filename (defaults to "TestProject.csproj").
    /// </summary>
    protected string? ProjectFilename { get; set; } = "TestProject.csproj";

    /// <summary>
    ///     The root SDK for the project (defaults to "Microsoft.NET.Sdk").
    /// </summary>
    protected string RootSdk { get; set; } = "Microsoft.NET.Sdk";

    /// <summary>
    ///     The .NET SDK version to use for builds.
    /// </summary>
    protected NetSdkVersion SdkVersion { get; set; } = NetSdkVersion.Net100;

    /// <summary>
    ///     Gets the root folder path of the temporary project directory.
    /// </summary>
    /// <value>The full path to the temporary directory containing the project files.</value>
    /// <remarks>
    ///     Use this property to access generated files, add additional files, or inspect the project structure
    ///     after build operations complete.
    /// </remarks>
    public FullPath RootFolder => Directory.FullPath;

    /// <summary>
    ///     Gets environment variables that simulate a GitHub Actions CI environment.
    /// </summary>
    /// <value>An enumerable of name-value tuples for GitHub Actions environment variables.</value>
    /// <remarks>
    ///     <para>Returns the following environment variables:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>GITHUB_ACTIONS</term>
    ///             <description>Set to "true" to indicate GitHub Actions environment.</description>
    ///         </item>
    ///         <item>
    ///             <term>GITHUB_STEP_SUMMARY</term>
    ///             <description>Path to the step summary file for workflow annotations.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var result = await builder.BuildAsync(
    ///     environmentVariables: builder.GitHubEnvironmentVariables.ToArray());
    /// </code>
    /// </example>
    /// <seealso cref="GetGitHubStepSummaryContent" />
    public IEnumerable<(string Name, string Value)> GitHubEnvironmentVariables
    {
        get
        {
            yield return ("GITHUB_ACTIONS", "true");
            yield return ("GITHUB_STEP_SUMMARY", GithubStepSummaryFile);
        }
    }

    /// <summary>
    ///     Disposes the builder and cleans up the temporary directory.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous dispose operation.</returns>
    /// <remarks>
    ///     This method removes all files and directories created during the build process.
    ///     Always use <c>await using</c> to ensure proper cleanup.
    /// </remarks>
    public virtual async ValueTask DisposeAsync()
    {
        await Directory.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Gets the content of the GitHub step summary file if it exists.
    /// </summary>
    /// <returns>
    ///     The content of the step summary file as a string, or <see langword="null" /> if the file does not exist or is
    ///     empty.
    /// </returns>
    /// <remarks>
    ///     This is useful for testing analyzers or generators that write GitHub workflow annotations.
    ///     The step summary file is populated when builds are run with <see cref="GitHubEnvironmentVariables" />.
    /// </remarks>
    /// <seealso cref="GitHubEnvironmentVariables" />
    public string? GetGitHubStepSummaryContent()
    {
        return File.Exists(GithubStepSummaryFile) ? File.ReadAllText(GithubStepSummaryFile) : null;
    }

    /// <summary>
    ///     Adds a file with the specified content to the project directory.
    /// </summary>
    /// <param name="relativePath">The relative path from the project root where the file should be created.</param>
    /// <param name="content">The text content to write to the file.</param>
    /// <returns>The full path to the created file.</returns>
    /// <remarks>
    ///     Parent directories are created automatically if they do not exist.
    ///     This method can be used to add any type of file including source files, configuration files, or resources.
    /// </remarks>
    /// <example>
    ///     <code>
    /// builder.AddFile("src/Models/User.cs", "namespace Models; public record User(string Name);");
    /// builder.AddFile("appsettings.json", "{ \"key\": \"value\" }");
    /// </code>
    /// </example>
    public FullPath AddFile(string relativePath, string content)
    {
        var path = Directory.FullPath / relativePath;
        path.CreateParentDirectory();
        File.WriteAllText(path, content);
        return path;
    }
}
