using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
/// NuGet package reference for project building.
/// </summary>
public readonly record struct NuGetReference(string Name, string Version);

/// <summary>
/// Fluent builder for creating and building isolated .NET projects for testing.
/// </summary>
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
    /// Creates a new ProjectBuilder with isolated temp directory.
    /// </summary>
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

    /// <summary>Gets the root folder of the temporary project.</summary>
    public FullPath RootFolder => _directory.FullPath;

    /// <summary>GitHub environment variables for CI simulation.</summary>
    public IEnumerable<(string Name, string Value)> GitHubEnvironmentVariables
    {
        get
        {
            yield return ("GITHUB_ACTIONS", "true");
            yield return ("GITHUB_STEP_SUMMARY", _githubStepSummaryFile);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _directory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the GitHub step summary content if available.</summary>
    public string? GetGitHubStepSummaryContent()
    {
        return File.Exists(_githubStepSummaryFile) ? File.ReadAllText(_githubStepSummaryFile) : null;
    }

    /// <summary>Adds a file to the project directory.</summary>
    public FullPath AddFile(string relativePath, string content)
    {
        var path = _directory.FullPath / relativePath;
        path.CreateParentDirectory();
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Configures the NuGet.config for the project.</summary>
    public ProjectBuilder WithNuGetConfig(string nugetConfigContent)
    {
        _directory.CreateTextFile("NuGet.config", nugetConfigContent);
        return this;
    }

    /// <summary>Adds a package source to NuGet.config.</summary>
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

    /// <summary>Sets the .NET SDK version to use.</summary>
    public ProjectBuilder WithDotnetSdkVersion(NetSdkVersion dotnetSdkVersion)
    {
        _sdkVersion = dotnetSdkVersion;
        return this;
    }

    /// <summary>Enables Microsoft Testing Platform mode.</summary>
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

    /// <summary>Sets the target framework.</summary>
    public ProjectBuilder WithTargetFramework(string tfm)
    {
        _properties.Add((Prop.TargetFramework, tfm));
        return this;
    }

    /// <summary>Sets the output type (Library, Exe).</summary>
    public ProjectBuilder WithOutputType(string type)
    {
        _properties.Add((Prop.OutputType, type));
        return this;
    }

    /// <summary>Sets the language version.</summary>
    public ProjectBuilder WithLangVersion(string version = Val.Latest)
    {
        _properties.Add((Prop.LangVersion, version));
        return this;
    }

    /// <summary>Sets an arbitrary MSBuild property.</summary>
    public ProjectBuilder WithProperty(string name, string value)
    {
        _properties.Add((name, value));
        return this;
    }

    /// <summary>Sets multiple MSBuild properties.</summary>
    public ProjectBuilder WithProperties(params (string Key, string Value)[] properties)
    {
        _properties.AddRange(properties);
        return this;
    }

    /// <summary>Adds a source file to the project.</summary>
    public ProjectBuilder AddSource(string filename, string content)
    {
        _sourceFiles.Add((filename, content));
        return this;
    }

    /// <summary>Adds a NuGet package reference.</summary>
    public ProjectBuilder WithPackage(string name, string version)
    {
        _nugetPackages.Add(new NuGetReference(name, version));
        return this;
    }

    /// <summary>Sets the project filename.</summary>
    public ProjectBuilder WithFilename(string filename)
    {
        _projectFilename = filename;
        return this;
    }

    /// <summary>Sets the root SDK (e.g., "Microsoft.NET.Sdk", "Microsoft.NET.Sdk.Web").</summary>
    public ProjectBuilder WithRootSdk(string sdk)
    {
        _rootSdk = sdk;
        return this;
    }

    /// <summary>Adds a Directory.Build.props file.</summary>
    public ProjectBuilder WithDirectoryBuildProps(string content)
    {
        AddFile("Directory.Build.props", content);
        return this;
    }

    /// <summary>Adds a Directory.Packages.props file for CPM.</summary>
    public ProjectBuilder WithDirectoryPackagesProps(string content)
    {
        AddFile("Directory.Packages.props", content);
        return this;
    }

    /// <summary>Builds the project and returns the result.</summary>
    public async Task<BuildResult> BuildAsync(string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in _sourceFiles)
            AddFile(name, content);

        return await ExecuteDotnetCommandAsync("build", buildArguments, environmentVariables);
    }

    /// <summary>Runs the project and returns the result.</summary>
    public async Task<BuildResult> RunAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in _sourceFiles)
            AddFile(name, content);

        return await ExecuteDotnetCommandAsync("run", ["--", .. arguments ?? []], environmentVariables);
    }

    /// <summary>Tests the project and returns the result.</summary>
    public async Task<BuildResult> TestAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in _sourceFiles)
            AddFile(name, content);

        return await ExecuteDotnetCommandAsync("test", arguments, environmentVariables);
    }

    /// <summary>Packs the project and returns the result.</summary>
    public async Task<BuildResult> PackAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in _sourceFiles)
            AddFile(name, content);

        return await ExecuteDotnetCommandAsync("pack", arguments, environmentVariables);
    }

    /// <summary>Restores the project and returns the result.</summary>
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

    /// <summary>Executes a dotnet command and returns the result.</summary>
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
