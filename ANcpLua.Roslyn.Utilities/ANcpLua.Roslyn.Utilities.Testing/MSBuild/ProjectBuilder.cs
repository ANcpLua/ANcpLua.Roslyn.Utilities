using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
/// NuGet package reference for project building.
/// </summary>
/// <param name="Name">The package identifier (e.g., "Microsoft.CodeAnalysis.CSharp").</param>
/// <param name="Version">The package version (e.g., "4.12.0").</param>
/// <seealso cref="ProjectBuilder.WithPackage"/>
public readonly record struct NuGetReference(string Name, string Version);

/// <summary>
/// Fluent builder for creating and building isolated .NET projects for testing.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ProjectBuilder"/> provides a complete solution for integration testing of MSBuild-based
/// projects, source generators, and analyzers in an isolated environment. Each instance creates a
/// temporary directory with its own global.json, NuGet configuration, and project files.
/// </para>
/// <list type="bullet">
///     <item>
///         <description>Creates isolated temporary directories with automatic cleanup via <see cref="IAsyncDisposable"/>.</description>
///     </item>
///     <item>
///         <description>Downloads and caches .NET SDK versions automatically via <see cref="DotNetSdkHelpers"/>.</description>
///     </item>
///     <item>
///         <description>Generates SARIF output and binary logs for comprehensive build analysis.</description>
///     </item>
///     <item>
///         <description>Supports GitHub Actions CI simulation with step summary output.</description>
///     </item>
///     <item>
///         <description>Provides fluent API for configuring MSBuild properties, packages, and source files.</description>
///     </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
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
/// <seealso cref="BuildResult"/>
/// <seealso cref="BuildResultAssertions"/>
/// <seealso cref="NetSdkVersion"/>
public class ProjectBuilder : IAsyncDisposable
{
    private const string SarifFileName = "BuildOutput.sarif";

    private readonly TemporaryDirectory _directory;
    private readonly FullPath _githubStepSummaryFile;
    private readonly List<NuGetReference> _nugetPackages = [];
    private readonly List<(string Key, string Value)> _properties = [];
    private readonly List<(string Name, string Content)> _sourceFiles = [];
    private readonly ITestOutputHelper? _testOutputHelper;
    private int _buildCount;
    private NetSdkVersion _sdkVersion = NetSdkVersion.Net100;
    private string? _projectFilename = "TestProject.csproj";
    private string _rootSdk = "Microsoft.NET.Sdk";

    /// <summary>
    /// Creates a new <see cref="ProjectBuilder"/> with an isolated temporary directory.
    /// </summary>
    /// <param name="testOutputHelper">
    /// Optional xUnit test output helper for logging build output and file contents during test execution.
    /// When provided, enables verbose logging of all files and build commands.
    /// </param>
    /// <remarks>
    /// <list type="bullet">
    ///     <item>
    ///         <description>Creates a unique temporary directory for complete build isolation.</description>
    ///     </item>
    ///     <item>
    ///         <description>Generates a default global.json configured for .NET 10.0 SDK with latestMinor rollForward policy.</description>
    ///     </item>
    ///     <item>
    ///         <description>Prepares a file for GitHub step summary simulation.</description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Without test output (silent mode)
    /// await using var builder = new ProjectBuilder();
    ///
    /// // With xUnit test output for debugging
    /// await using var builder = new ProjectBuilder(testOutputHelper);
    /// </code>
    /// </example>
    public ProjectBuilder(ITestOutputHelper? testOutputHelper = null)
    {
        _testOutputHelper = testOutputHelper;
        _directory = TemporaryDirectory.Create();
        _githubStepSummaryFile = _directory.CreateEmptyFile("GITHUB_STEP_SUMMARY.txt");

        // Create isolated global.json
        _directory.CreateTextFile("global.json", """
                                                 {
                                                   "sdk": {
                                                     "rollForward": "latestMinor",
                                                     "version": "10.0.100"
                                                   }
                                                 }
                                                 """);
    }

    /// <summary>
    /// Gets the root folder path of the temporary project directory.
    /// </summary>
    /// <value>The full path to the temporary directory containing the project files.</value>
    /// <remarks>
    /// Use this property to access generated files, add additional files, or inspect the project structure
    /// after build operations complete.
    /// </remarks>
    public FullPath RootFolder => _directory.FullPath;

    /// <summary>
    /// Gets environment variables that simulate a GitHub Actions CI environment.
    /// </summary>
    /// <value>An enumerable of name-value tuples for GitHub Actions environment variables.</value>
    /// <remarks>
    /// <para>Returns the following environment variables:</para>
    /// <list type="bullet">
    ///     <item>
    ///         <term>GITHUB_ACTIONS</term>
    ///         <description>Set to "true" to indicate GitHub Actions environment.</description>
    ///     </item>
    ///     <item>
    ///         <term>GITHUB_STEP_SUMMARY</term>
    ///         <description>Path to the step summary file for workflow annotations.</description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await builder.BuildAsync(
    ///     environmentVariables: builder.GitHubEnvironmentVariables.ToArray());
    /// </code>
    /// </example>
    /// <seealso cref="GetGitHubStepSummaryContent"/>
    public IEnumerable<(string Name, string Value)> GitHubEnvironmentVariables
    {
        get
        {
            yield return ("GITHUB_ACTIONS", "true");
            yield return ("GITHUB_STEP_SUMMARY", _githubStepSummaryFile);
        }
    }

    /// <summary>
    /// Disposes the builder and cleans up the temporary directory.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    /// <remarks>
    /// This method removes all files and directories created during the build process.
    /// Always use <c>await using</c> to ensure proper cleanup.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        await _directory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the content of the GitHub step summary file if it exists.
    /// </summary>
    /// <returns>
    /// The content of the step summary file as a string, or <see langword="null"/> if the file does not exist or is empty.
    /// </returns>
    /// <remarks>
    /// This is useful for testing analyzers or generators that write GitHub workflow annotations.
    /// The step summary file is populated when builds are run with <see cref="GitHubEnvironmentVariables"/>.
    /// </remarks>
    /// <seealso cref="GitHubEnvironmentVariables"/>
    public string? GetGitHubStepSummaryContent()
    {
        return File.Exists(_githubStepSummaryFile) ? File.ReadAllText(_githubStepSummaryFile) : null;
    }

    /// <summary>
    /// Adds a file with the specified content to the project directory.
    /// </summary>
    /// <param name="relativePath">The relative path from the project root where the file should be created.</param>
    /// <param name="content">The text content to write to the file.</param>
    /// <returns>The full path to the created file.</returns>
    /// <remarks>
    /// Parent directories are created automatically if they do not exist.
    /// This method can be used to add any type of file including source files, configuration files, or resources.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.AddFile("src/Models/User.cs", "namespace Models; public record User(string Name);");
    /// builder.AddFile("appsettings.json", "{ \"key\": \"value\" }");
    /// </code>
    /// </example>
    public FullPath AddFile(string relativePath, string content)
    {
        var path = _directory.FullPath / relativePath;
        path.CreateParentDirectory();
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Configures the NuGet.config file with custom content.
    /// </summary>
    /// <param name="nugetConfigContent">The complete XML content for the NuGet.config file.</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// This method replaces any existing NuGet.config file in the project directory.
    /// For simpler package source configuration, consider using <see cref="WithPackageSource"/> instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.WithNuGetConfig("""
    ///     &lt;configuration&gt;
    ///         &lt;packageSources&gt;
    ///             &lt;clear /&gt;
    ///             &lt;add key="myFeed" value="https://pkgs.dev.azure.com/org/feed/nuget/v3/index.json" /&gt;
    ///         &lt;/packageSources&gt;
    ///     &lt;/configuration&gt;
    ///     """);
    /// </code>
    /// </example>
    /// <seealso cref="WithPackageSource"/>
    public ProjectBuilder WithNuGetConfig(string nugetConfigContent)
    {
        _directory.CreateTextFile("NuGet.config", nugetConfigContent);
        return this;
    }

    /// <summary>
    /// Adds a package source to the NuGet configuration with optional package pattern mapping.
    /// </summary>
    /// <param name="name">The name of the package source (e.g., "local", "myFeed").</param>
    /// <param name="path">The path or URL to the package source.</param>
    /// <param name="packagePatterns">
    /// Optional package patterns to restrict which packages come from this source.
    /// When specified, package source mapping is enabled with nuget.org as fallback for unmatched patterns.
    /// </param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// <list type="bullet">
    ///     <item>
    ///         <description>Configures an isolated global packages folder within the source path to prevent cache pollution.</description>
    ///     </item>
    ///     <item>
    ///         <description>Clears existing package sources and adds the specified source plus nuget.org.</description>
    ///     </item>
    ///     <item>
    ///         <description>When package patterns are provided, enables package source mapping for security and reproducibility.</description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple local source
    /// builder.WithPackageSource("local", "/path/to/packages");
    ///
    /// // With package pattern mapping
    /// builder.WithPackageSource("myFeed", "/path/to/packages", "MyCompany.*", "MyOrg.*");
    /// </code>
    /// </example>
    /// <seealso cref="WithNuGetConfig"/>
    public ProjectBuilder WithPackageSource(string name, string path, params string[] packagePatterns)
    {
        var patternElements = packagePatterns.Length > 0
            ? $"""
               <packageSourceMapping>
                   <packageSource key="{name}">
                       {string.Join('\n', packagePatterns.Select(p => $"<package pattern=\"{p}\" />"))}
                   </packageSource>
                   <packageSource key="nuget.org">
                       <package pattern="*" />
                   </packageSource>
               </packageSourceMapping>
               """
            : "";

        var config = $"""
                      <configuration>
                          <config>
                              <add key="globalPackagesFolder" value="{path}/packages" />
                          </config>
                          <packageSources>
                              <clear />
                              <add key="{name}" value="{path}" />
                              <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                          </packageSources>
                          {patternElements}
                      </configuration>
                      """;
        _directory.CreateTextFile("NuGet.config", config);
        return this;
    }

    /// <summary>
    /// Sets the .NET SDK version to use for building the project.
    /// </summary>
    /// <param name="dotnetSdkVersion">The SDK version to use.</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// The SDK is automatically downloaded and cached by <see cref="DotNetSdkHelpers"/> if not already present.
    /// The default SDK version is <see cref="NetSdkVersion.Net100"/>.
    /// </remarks>
    /// <seealso cref="NetSdkVersion"/>
    /// <seealso cref="DotNetSdkHelpers"/>
    public ProjectBuilder WithDotnetSdkVersion(NetSdkVersion dotnetSdkVersion)
    {
        _sdkVersion = dotnetSdkVersion;
        return this;
    }

    /// <summary>
    /// Enables Microsoft Testing Platform (MTP) mode in the global.json configuration.
    /// </summary>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// This modifies the global.json to include the MTP test runner configuration,
    /// which is required for using the new Microsoft.Testing.Platform-based test execution model.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await builder
    ///     .WithMtpMode()
    ///     .WithPackage("Microsoft.Testing.Platform", "1.0.0")
    ///     .AddSource("Tests.cs", testCode)
    ///     .TestAsync();
    /// </code>
    /// </example>
    public ProjectBuilder WithMtpMode()
    {
        _directory.CreateTextFile("global.json", """
                                                 {
                                                   "sdk": {
                                                     "rollForward": "latestMinor",
                                                     "version": "10.0.100"
                                                   },
                                                   "test": {
                                                     "runner": "Microsoft.Testing.Platform"
                                                   }
                                                 }
                                                 """);
        return this;
    }

    /// <summary>
    /// Sets the target framework for the project.
    /// </summary>
    /// <param name="tfm">The target framework moniker (e.g., "net10.0", "netstandard2.0").</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// Use constants from <see cref="Tfm"/> for common target framework values.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.WithTargetFramework(Tfm.Net100);
    /// builder.WithTargetFramework("net8.0-windows");
    /// </code>
    /// </example>
    /// <seealso cref="Tfm"/>
    public ProjectBuilder WithTargetFramework(string tfm)
    {
        _properties.Add((Prop.TargetFramework, tfm));
        return this;
    }

    /// <summary>
    /// Sets the output type of the project.
    /// </summary>
    /// <param name="type">The output type (e.g., "Library", "Exe", "WinExe").</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// Use constants from <see cref="Val"/> for common output type values.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.WithOutputType(Val.Library);
    /// builder.WithOutputType(Val.Exe);
    /// </code>
    /// </example>
    /// <seealso cref="Val"/>
    public ProjectBuilder WithOutputType(string type)
    {
        _properties.Add((Prop.OutputType, type));
        return this;
    }

    /// <summary>
    /// Sets the C# language version for the project.
    /// </summary>
    /// <param name="version">
    /// The language version (e.g., "12.0", "latest", "preview").
    /// Defaults to <see cref="Val.Latest"/>.
    /// </param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <seealso cref="Val.Latest"/>
    /// <seealso cref="Val.Preview"/>
    public ProjectBuilder WithLangVersion(string version = Val.Latest)
    {
        _properties.Add((Prop.LangVersion, version));
        return this;
    }

    /// <summary>
    /// Sets an arbitrary MSBuild property on the project.
    /// </summary>
    /// <param name="name">The property name (e.g., "Nullable", "ImplicitUsings").</param>
    /// <param name="value">The property value.</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// Use constants from <see cref="Prop"/> for common property names and <see cref="Val"/> for common values.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder
    ///     .WithProperty(Prop.Nullable, Val.Enable)
    ///     .WithProperty(Prop.ImplicitUsings, Val.Enable)
    ///     .WithProperty("CustomProperty", "CustomValue");
    /// </code>
    /// </example>
    /// <seealso cref="Prop"/>
    /// <seealso cref="Val"/>
    /// <seealso cref="WithProperties"/>
    public ProjectBuilder WithProperty(string name, string value)
    {
        _properties.Add((name, value));
        return this;
    }

    /// <summary>
    /// Sets multiple MSBuild properties on the project.
    /// </summary>
    /// <param name="properties">An array of key-value tuples representing property names and values.</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.WithProperties(
    ///     (Prop.Nullable, Val.Enable),
    ///     (Prop.ImplicitUsings, Val.Enable),
    ///     (Prop.TreatWarningsAsErrors, Val.True));
    /// </code>
    /// </example>
    /// <seealso cref="WithProperty"/>
    public ProjectBuilder WithProperties(params (string Key, string Value)[] properties)
    {
        _properties.AddRange(properties);
        return this;
    }

    /// <summary>
    /// Adds a source file to the project.
    /// </summary>
    /// <param name="filename">The filename for the source file (e.g., "Program.cs", "Models/User.cs").</param>
    /// <param name="content">The C# source code content.</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// Source files are written to the project directory when <see cref="BuildAsync"/>, <see cref="RunAsync"/>,
    /// <see cref="TestAsync"/>, or <see cref="PackAsync"/> is called.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder
    ///     .AddSource("Program.cs", """
    ///         Console.WriteLine("Hello, World!");
    ///         """)
    ///     .AddSource("Models/User.cs", """
    ///         namespace Models;
    ///         public record User(string Name);
    ///         """);
    /// </code>
    /// </example>
    /// <seealso cref="AddFile"/>
    public ProjectBuilder AddSource(string filename, string content)
    {
        _sourceFiles.Add((filename, content));
        return this;
    }

    /// <summary>
    /// Adds a NuGet package reference to the project.
    /// </summary>
    /// <param name="name">The package identifier (e.g., "Microsoft.CodeAnalysis.CSharp").</param>
    /// <param name="version">The package version (e.g., "4.12.0").</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder
    ///     .WithPackage("Microsoft.CodeAnalysis.CSharp", "4.12.0")
    ///     .WithPackage("xunit", "2.9.2");
    /// </code>
    /// </example>
    /// <seealso cref="NuGetReference"/>
    public ProjectBuilder WithPackage(string name, string version)
    {
        _nugetPackages.Add(new NuGetReference(name, version));
        return this;
    }

    /// <summary>
    /// Sets the project filename.
    /// </summary>
    /// <param name="filename">The filename for the .csproj file (e.g., "MyProject.csproj").</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// The default filename is "TestProject.csproj".
    /// </remarks>
    public ProjectBuilder WithFilename(string filename)
    {
        _projectFilename = filename;
        return this;
    }

    /// <summary>
    /// Sets the root SDK for the project.
    /// </summary>
    /// <param name="sdk">The SDK identifier (e.g., "Microsoft.NET.Sdk", "Microsoft.NET.Sdk.Web", "Microsoft.NET.Sdk.Worker").</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// The default SDK is "Microsoft.NET.Sdk".
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.WithRootSdk("Microsoft.NET.Sdk.Web");
    /// </code>
    /// </example>
    public ProjectBuilder WithRootSdk(string sdk)
    {
        _rootSdk = sdk;
        return this;
    }

    /// <summary>
    /// Adds a Directory.Build.props file to the project directory.
    /// </summary>
    /// <param name="content">The XML content for the Directory.Build.props file.</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// Directory.Build.props files are evaluated before the project file and are useful for
    /// setting properties that should apply to all projects in a directory hierarchy.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.WithDirectoryBuildProps("""
    ///     &lt;Project&gt;
    ///         &lt;PropertyGroup&gt;
    ///             &lt;TreatWarningsAsErrors&gt;true&lt;/TreatWarningsAsErrors&gt;
    ///         &lt;/PropertyGroup&gt;
    ///     &lt;/Project&gt;
    ///     """);
    /// </code>
    /// </example>
    /// <seealso cref="WithDirectoryPackagesProps"/>
    public ProjectBuilder WithDirectoryBuildProps(string content)
    {
        AddFile("Directory.Build.props", content);
        return this;
    }

    /// <summary>
    /// Adds a Directory.Packages.props file for Central Package Management (CPM).
    /// </summary>
    /// <param name="content">The XML content for the Directory.Packages.props file.</param>
    /// <returns>The current <see cref="ProjectBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// Central Package Management allows you to manage package versions in a single location.
    /// When using CPM, package references in project files should not specify versions.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.WithDirectoryPackagesProps("""
    ///     &lt;Project&gt;
    ///         &lt;PropertyGroup&gt;
    ///             &lt;ManagePackageVersionsCentrally&gt;true&lt;/ManagePackageVersionsCentrally&gt;
    ///         &lt;/PropertyGroup&gt;
    ///         &lt;ItemGroup&gt;
    ///             &lt;PackageVersion Include="xunit" Version="2.9.2" /&gt;
    ///         &lt;/ItemGroup&gt;
    ///     &lt;/Project&gt;
    ///     """);
    /// </code>
    /// </example>
    /// <seealso cref="WithDirectoryBuildProps"/>
    public ProjectBuilder WithDirectoryPackagesProps(string content)
    {
        AddFile("Directory.Packages.props", content);
        return this;
    }

    /// <summary>
    /// Builds the project and returns the result.
    /// </summary>
    /// <param name="buildArguments">Optional additional arguments to pass to the dotnet build command.</param>
    /// <param name="environmentVariables">Optional environment variables to set during the build.</param>
    /// <returns>A <see cref="BuildResult"/> containing the build output, SARIF diagnostics, and binary log.</returns>
    /// <remarks>
    /// <list type="bullet">
    ///     <item>
    ///         <description>Generates the .csproj file from the configured properties and packages.</description>
    ///     </item>
    ///     <item>
    ///         <description>Writes all source files to the project directory.</description>
    ///     </item>
    ///     <item>
    ///         <description>Executes <c>dotnet build</c> with automatic binary log generation.</description>
    ///     </item>
    ///     <item>
    ///         <description>Parses SARIF output for structured diagnostic access.</description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await builder
    ///     .WithTargetFramework(Tfm.Net100)
    ///     .AddSource("Program.cs", code)
    ///     .BuildAsync();
    ///
    /// result.ShouldSucceed();
    /// Assert.False(result.HasWarning("CS0618"));
    /// </code>
    /// </example>
    /// <seealso cref="BuildResult"/>
    /// <seealso cref="RunAsync"/>
    /// <seealso cref="TestAsync"/>
    /// <seealso cref="PackAsync"/>
    public async Task<BuildResult> BuildAsync(string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in _sourceFiles)
            AddFile(name, content);

        return await ExecuteDotnetCommandAsync("build", buildArguments, environmentVariables);
    }

    /// <summary>
    /// Builds and runs the project, returning the result.
    /// </summary>
    /// <param name="arguments">Optional arguments to pass to the application after the <c>--</c> separator.</param>
    /// <param name="environmentVariables">Optional environment variables to set during execution.</param>
    /// <returns>A <see cref="BuildResult"/> containing the run output and any diagnostics.</returns>
    /// <remarks>
    /// This method is suitable for testing console applications. The project must have an executable output type.
    /// Arguments are passed after <c>--</c> to separate them from dotnet CLI arguments.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await builder
    ///     .WithTargetFramework(Tfm.Net100)
    ///     .WithOutputType(Val.Exe)
    ///     .AddSource("Program.cs", "Console.WriteLine(args[0]);")
    ///     .RunAsync(["Hello"]);
    ///
    /// result.ShouldSucceed();
    /// Assert.True(result.OutputContains("Hello"));
    /// </code>
    /// </example>
    /// <seealso cref="BuildAsync"/>
    public async Task<BuildResult> RunAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in _sourceFiles)
            AddFile(name, content);

        return await ExecuteDotnetCommandAsync("run", ["--", .. arguments ?? []], environmentVariables);
    }

    /// <summary>
    /// Builds and tests the project, returning the result.
    /// </summary>
    /// <param name="arguments">Optional additional arguments to pass to the dotnet test command.</param>
    /// <param name="environmentVariables">Optional environment variables to set during testing.</param>
    /// <returns>A <see cref="BuildResult"/> containing the test output and any diagnostics.</returns>
    /// <remarks>
    /// The project should reference a test framework (xUnit, NUnit, MSTest) and contain test classes.
    /// Consider using <see cref="WithMtpMode"/> for Microsoft Testing Platform-based test execution.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await builder
    ///     .WithTargetFramework(Tfm.Net100)
    ///     .WithPackage("xunit", "2.9.2")
    ///     .WithPackage("xunit.runner.visualstudio", "3.0.0")
    ///     .AddSource("Tests.cs", """
    ///         using Xunit;
    ///         public class Tests { [Fact] public void Test() => Assert.True(true); }
    ///         """)
    ///     .TestAsync();
    ///
    /// result.ShouldSucceed();
    /// </code>
    /// </example>
    /// <seealso cref="WithMtpMode"/>
    /// <seealso cref="BuildAsync"/>
    public async Task<BuildResult> TestAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in _sourceFiles)
            AddFile(name, content);

        return await ExecuteDotnetCommandAsync("test", arguments, environmentVariables);
    }

    /// <summary>
    /// Packs the project into a NuGet package, returning the result.
    /// </summary>
    /// <param name="arguments">Optional additional arguments to pass to the dotnet pack command.</param>
    /// <param name="environmentVariables">Optional environment variables to set during packing.</param>
    /// <returns>A <see cref="BuildResult"/> containing the pack output and any diagnostics.</returns>
    /// <remarks>
    /// The project should be configured as packable (IsPackable=true or default for library projects).
    /// The resulting .nupkg file will be in the bin/Release directory.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await builder
    ///     .WithTargetFramework(Tfm.NetStandard20)
    ///     .WithProperty(Prop.Version, "1.0.0")
    ///     .WithProperty(Prop.PackageId, "MyPackage")
    ///     .AddSource("Library.cs", "public class MyLib { }")
    ///     .PackAsync();
    ///
    /// result.ShouldSucceed();
    /// </code>
    /// </example>
    /// <seealso cref="BuildAsync"/>
    public async Task<BuildResult> PackAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in _sourceFiles)
            AddFile(name, content);

        return await ExecuteDotnetCommandAsync("pack", arguments, environmentVariables);
    }

    /// <summary>
    /// Restores NuGet packages for the project.
    /// </summary>
    /// <param name="arguments">Optional additional arguments to pass to the dotnet restore command.</param>
    /// <param name="environmentVariables">Optional environment variables to set during restore.</param>
    /// <returns>A <see cref="BuildResult"/> containing the restore output and any diagnostics.</returns>
    /// <remarks>
    /// Restore is typically called automatically by build, run, test, and pack commands.
    /// Use this method when you need to explicitly restore packages or diagnose restore issues.
    /// </remarks>
    /// <seealso cref="BuildAsync"/>
    public async Task<BuildResult> RestoreAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        return await ExecuteDotnetCommandAsync("restore", arguments, environmentVariables);
    }

    private void GenerateCsprojFile()
    {
        var propertiesElement = new XElement("PropertyGroup");
        foreach (var prop in _properties)
            propertiesElement.Add(new XElement(prop.Key, prop.Value));

        var packagesElement = new XElement("ItemGroup");
        foreach (var package in _nugetPackages)
            packagesElement.Add(new XElement("PackageReference",
                new XAttribute("Include", package.Name),
                new XAttribute("Version", package.Version)));

        var content = $"""
                       <Project Sdk="{_rootSdk}">
                           <PropertyGroup>
                               <ErrorLog>{SarifFileName},version=2.1</ErrorLog>
                           </PropertyGroup>
                           {propertiesElement}
                           {packagesElement}
                       </Project>
                       """;

        var fullPath = _directory.FullPath / (_projectFilename ?? "TestProject.csproj");
        fullPath.CreateParentDirectory();
        File.WriteAllText(fullPath, content);
    }

    /// <summary>
    /// Executes an arbitrary dotnet command and returns the result.
    /// </summary>
    /// <param name="command">The dotnet command to execute (e.g., "build", "run", "test", "pack", "restore", "new").</param>
    /// <param name="arguments">Optional additional arguments to pass to the command.</param>
    /// <param name="environmentVariables">Optional environment variables to set during execution.</param>
    /// <returns>A <see cref="BuildResult"/> containing the command output, SARIF diagnostics (if available), and binary log.</returns>
    /// <remarks>
    /// <para>This method provides low-level access to the dotnet CLI for custom scenarios.</para>
    /// <list type="bullet">
    ///     <item>
    ///         <description>Automatically adds <c>/bl</c> flag for binary log generation.</description>
    ///     </item>
    ///     <item>
    ///         <description>Isolates the build environment by removing interfering environment variables (CI, GITHUB_*, MSBuild*, RUNNER_*).</description>
    ///     </item>
    ///     <item>
    ///         <description>Configures DOTNET_ROOT and related variables for proper SDK resolution.</description>
    ///     </item>
    ///     <item>
    ///         <description>Logs all files and command output when a test output helper is configured.</description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Custom dotnet command
    /// var result = await builder.ExecuteDotnetCommandAsync("format", ["--verify-no-changes"]);
    ///
    /// // dotnet new with template
    /// var result = await builder.ExecuteDotnetCommandAsync("new", ["console", "-n", "MyApp"]);
    /// </code>
    /// </example>
    /// <seealso cref="BuildAsync"/>
    /// <seealso cref="BuildResult"/>
    public async Task<BuildResult> ExecuteDotnetCommandAsync(string command, string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        _buildCount++;

        if (_testOutputHelper is not null)
        {
            foreach (var file in Directory.GetFiles(_directory.FullPath, "*", SearchOption.AllDirectories))
            {
                _testOutputHelper.WriteLine("File: " + file);
                var content = await File.ReadAllTextAsync(file);
                _testOutputHelper.WriteLine(content);
            }

            _testOutputHelper.WriteLine("-------- dotnet " + command);
        }

        var psi = new ProcessStartInfo(await DotNetSdkHelpers.Get(_sdkVersion))
        {
            WorkingDirectory = _directory.FullPath,
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

        _testOutputHelper?.WriteLine("Executing: " + psi.FileName + " " + string.Join(' ', psi.ArgumentList));

        var result = await psi.RunAsTaskAsync();

        _testOutputHelper?.WriteLine("Process exit code: " + result.ExitCode);
        _testOutputHelper?.WriteLine(result.Output.ToString());

        var sarifPath = _directory.FullPath / SarifFileName;
        SarifFile? sarif = null;
        if (File.Exists(sarifPath))
        {
            var bytes = await File.ReadAllBytesAsync(sarifPath);
            sarif = JsonSerializer.Deserialize<SarifFile>(bytes);
        }

        var binlogContent = await File.ReadAllBytesAsync(_directory.FullPath / "msbuild.binlog");

        return new BuildResult(result.ExitCode, result.Output, sarif, binlogContent);
    }
}
