using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
///     Enumeration for different SDK import styles in project files.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="Default" /> - Standard import via Project Sdk attribute.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="ProjectElement" /> - Import via Sdk element in project root.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="SdkElement" /> - Direct Sdk element reference.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="SdkElementDirectoryBuildProps" /> - Sdk element in Directory.Build.props file.
///             </description>
///         </item>
///     </list>
/// </remarks>
public enum PackageImportStyle
{
    /// <summary>
    ///     Standard import via the Project Sdk attribute.
    /// </summary>
    Default,

    /// <summary>
    ///     Import via a Sdk element as a child of the Project root.
    /// </summary>
    ProjectElement,

    /// <summary>
    ///     Direct Sdk element reference.
    /// </summary>
    SdkElement,

    /// <summary>
    ///     Sdk element defined in Directory.Build.props file.
    /// </summary>
    SdkElementDirectoryBuildProps
}

/// <summary>
///     Fluent builder for creating and building isolated .NET projects with custom package/SDK integration testing.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="PackageProjectBuilder" /> extends <see cref="ProjectBuilder" /> to support custom package
///         and SDK configurations for integration testing. It manages package sources, SDK import styles, and
///         provides convenience methods for package-specific project generation.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Supports configurable package names and versions via constructor.</description>
///         </item>
///         <item>
///             <description>Provides multiple SDK import styles for flexible project configuration.</description>
///         </item>
///         <item>
///             <description>Handles package directory configuration for isolated package resolution.</description>
///         </item>
///         <item>
///             <description>Includes retry logic for SDK resolution failures during builds.</description>
///         </item>
///         <item>
///             <description>Supports additional project elements and Directory.Build.props customization.</description>
///         </item>
///         <item>
///             <description>Provides git repository initialization helpers.</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// await using var builder = new PackageProjectBuilder(
///     testOutputHelper,
///     packageDirectory: "/path/to/packages",
///     packageName: "MyPackage",
///     packageVersion: "1.0.0");
///
/// var result = await builder
///     .WithTargetFramework(Tfm.Net100)
///     .WithOutputType(Val.Library)
///     .AddSource("Program.cs", "namespace Test; public class Foo { }")
///     .BuildAsync();
///
/// result.ShouldSucceed();
/// </code>
/// </example>
/// <seealso cref="ProjectBuilder" />
/// <seealso cref="NuGetPackageFixture" />
public sealed class PackageProjectBuilder : ProjectBuilder
{
    private readonly FullPath _packageDirectory;
    private readonly string _packageName;
    private readonly string _packageVersion;
    private readonly List<XElement> _additionalProjectElements = [];
    private PackageImportStyle _importStyle;

    /// <summary>
    ///     Creates a new <see cref="PackageProjectBuilder" /> with package configuration.
    /// </summary>
    /// <param name="testOutputHelper">Optional xUnit test output helper for logging.</param>
    /// <param name="packageDirectory">The directory containing NuGet packages.</param>
    /// <param name="packageName">The package name (e.g., "ANcpLua.NET.Sdk").</param>
    /// <param name="packageVersion">The package version (e.g., "1.0.0").</param>
    /// <param name="defaultImportStyle">The default SDK import style. Defaults to <see cref="PackageImportStyle.SdkElement" />.</param>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Creates an isolated temporary directory for the test project.</description>
    ///         </item>
    ///         <item>
    ///             <description>Configures NuGet to use the specified package directory as the package source.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Sets project filename to a package-specific default ("TestPackage.csproj") to avoid conflicts.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public PackageProjectBuilder(
        ITestOutputHelper? testOutputHelper,
        FullPath packageDirectory,
        string packageName,
        string packageVersion,
        PackageImportStyle defaultImportStyle = PackageImportStyle.SdkElement)
        : base(testOutputHelper)
    {
        _packageDirectory = packageDirectory;
        _packageName = packageName;
        _packageVersion = packageVersion;
        _importStyle = defaultImportStyle;

        ProjectFilename = "TestPackage.csproj";

        WithNuGetConfig($"""
                         <configuration>
                             <config>
                                 <add key="globalPackagesFolder" value="{_packageDirectory}/packages" />
                             </config>
                             <packageSources>
                                 <clear />
                                 <add key="PackageSource" value="{_packageDirectory}" />
                                 <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                             </packageSources>
                         </configuration>
                         """);

        if (defaultImportStyle is PackageImportStyle.SdkElementDirectoryBuildProps)
            AddDirectoryBuildPropsFile(string.Empty);
    }

    /// <summary>
    ///     Gets the package directory path.
    /// </summary>
    /// <value>The full path to the package directory.</value>
    public FullPath PackageDirectory => _packageDirectory;

    /// <summary>
    ///     Gets the package name.
    /// </summary>
    /// <value>The name of the package being tested.</value>
    public string PackageName => _packageName;

    /// <summary>
    ///     Gets the package version.
    /// </summary>
    /// <value>The version of the package being tested.</value>
    public string PackageVersion => _packageVersion;

    #region Fluent methods (covariant returns)

    /// <summary>
    ///     Sets the target framework for the project.
    /// </summary>
    /// <param name="tfm">The target framework moniker.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithTargetFramework(string tfm)
    {
        base.WithTargetFramework(tfm);
        return this;
    }

    /// <summary>
    ///     Sets the output type of the project.
    /// </summary>
    /// <param name="type">The output type (e.g., "Library", "Exe").</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithOutputType(string type)
    {
        base.WithOutputType(type);
        return this;
    }

    /// <summary>
    ///     Sets the C# language version for the project.
    /// </summary>
    /// <param name="version">The language version.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithLangVersion(string version = Val.Latest)
    {
        base.WithLangVersion(version);
        return this;
    }

    /// <summary>
    ///     Sets an arbitrary MSBuild property on the project.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithProperty(string name, string value)
    {
        base.WithProperty(name, value);
        return this;
    }

    /// <summary>
    ///     Sets multiple MSBuild properties on the project.
    /// </summary>
    /// <param name="properties">An array of key-value tuples representing properties.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithProperties(params (string Key, string Value)[] properties)
    {
        base.WithProperties(properties);
        return this;
    }

    /// <summary>
    ///     Adds a source file to the project.
    /// </summary>
    /// <param name="filename">The filename for the source file.</param>
    /// <param name="content">The C# source code content.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder AddSource(string filename, string content)
    {
        base.AddSource(filename, content);
        return this;
    }

    /// <summary>
    ///     Adds a NuGet package reference to the project.
    /// </summary>
    /// <param name="name">The package identifier.</param>
    /// <param name="version">The package version.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithPackage(string name, string version)
    {
        base.WithPackage(name, version);
        return this;
    }

    /// <summary>
    ///     Sets the project filename.
    /// </summary>
    /// <param name="filename">The filename for the .csproj file.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithFilename(string filename)
    {
        base.WithFilename(filename);
        return this;
    }

    /// <summary>
    ///     Sets the root SDK for the project.
    /// </summary>
    /// <param name="sdk">The SDK identifier.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithRootSdk(string sdk)
    {
        base.WithRootSdk(sdk);
        return this;
    }

    /// <summary>
    ///     Sets the .NET SDK version to use for building the project.
    /// </summary>
    /// <param name="dotnetSdkVersion">The SDK version to use.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithDotnetSdkVersion(NetSdkVersion dotnetSdkVersion)
    {
        base.WithDotnetSdkVersion(dotnetSdkVersion);
        return this;
    }

    /// <summary>
    ///     Enables Microsoft Testing Platform (MTP) mode.
    /// </summary>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithMtpMode()
    {
        base.WithMtpMode();
        return this;
    }

    /// <summary>
    ///     Adds a Directory.Build.props file to the project directory.
    /// </summary>
    /// <param name="content">The XML content for the Directory.Build.props file.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithDirectoryBuildProps(string content)
    {
        base.WithDirectoryBuildProps(content);
        return this;
    }

    /// <summary>
    ///     Adds a Directory.Packages.props file for Central Package Management (CPM).
    /// </summary>
    /// <param name="content">The XML content for the Directory.Packages.props file.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    public new PackageProjectBuilder WithDirectoryPackagesProps(string content)
    {
        base.WithDirectoryPackagesProps(content);
        return this;
    }

    #endregion

    #region Package-specific methods

    /// <summary>
    ///     Sets the SDK import style for the project.
    /// </summary>
    /// <param name="style">The import style to use.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     Different import styles can be used to test various SDK integration scenarios.
    /// </remarks>
    public PackageProjectBuilder WithImportStyle(PackageImportStyle style)
    {
        _importStyle = style;
        return this;
    }

    /// <summary>
    ///     Adds an additional XML element to the project file.
    /// </summary>
    /// <param name="element">The XML element to add.</param>
    /// <returns>The current <see cref="PackageProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     Additional elements are appended to the end of the project file before the closing Project tag.
    /// </remarks>
    public PackageProjectBuilder WithAdditionalProjectElement(XElement element)
    {
        _additionalProjectElements.Add(element);
        return this;
    }

    /// <summary>
    ///     Adds or updates a Directory.Build.props file for the project.
    /// </summary>
    /// <param name="postSdkContent">Content to appear after SDK elements.</param>
    /// <param name="preSdkContent">Optional content to appear before SDK elements.</param>
    /// <param name="packageName">Optional package name override.</param>
    /// <remarks>
    ///     This method provides finer-grained control over Directory.Build.props generation
    ///     compared to <see cref="WithDirectoryBuildProps" />.
    /// </remarks>
    public void AddDirectoryBuildPropsFile(string postSdkContent, string preSdkContent = "", string? packageName = null)
    {
        var sdk = _importStyle == PackageImportStyle.SdkElementDirectoryBuildProps
            ? GetSdkElementContent(packageName ?? _packageName)
            : string.Empty;

        var content = $"""
                       <Project>
                           <PropertyGroup>
                               <DisableVersionAnalyzer>true</DisableVersionAnalyzer>
                           </PropertyGroup>
                           {preSdkContent}
                           {sdk}
                           {postSdkContent}
                       </Project>
                       """;

        AddFile("Directory.Build.props", content);
    }

    /// <summary>
    ///     Initializes a git repository in the project directory.
    /// </summary>
    /// <remarks>
    ///     This method initializes a git repo, adds all files, creates an initial commit,
    ///     and configures a remote origin for testing source link and related features.
    /// </remarks>
    public async Task InitializeGitRepoAsync()
    {
        await ExecuteGitCommand("init");
        await ExecuteGitCommand("add", ".");
        await ExecuteGitCommand("commit", "-m", "Initial commit");
        await ExecuteGitCommand("remote", "add", "origin", "https://github.com/ancplua/sample.git");
    }

    /// <summary>
    ///     Executes a git command in the project directory.
    /// </summary>
    /// <param name="arguments">The git command arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ExecuteGitCommand(params string[]? arguments)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = Directory.FullPath,
            RedirectStandardInput = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        ICollection<KeyValuePair<string, string>> gitParameters =
        [
            KeyValuePair.Create("user.name", "test"),
            KeyValuePair.Create("user.email", "test@example.com"),
            KeyValuePair.Create("commit.gpgsign", "false"),
            KeyValuePair.Create("pull.rebase", "true"),
            KeyValuePair.Create("fetch.prune", "true"),
            KeyValuePair.Create("core.autocrlf", "false"),
            KeyValuePair.Create("core.longpaths", "true"),
            KeyValuePair.Create("rebase.autoStash", "true"),
            KeyValuePair.Create("submodule.recurse", "false"),
            KeyValuePair.Create("init.defaultBranch", "main")
        ];

        foreach (var param in gitParameters)
        {
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add($"{param.Key}={param.Value}");
        }

        if (arguments is not null)
            foreach (var arg in arguments)
                psi.ArgumentList.Add(arg);

        return psi.RunAsTaskAsync();
    }

    #endregion

    /// <summary>
    ///     Generates the .csproj file from the configured properties, packages, and SDK settings.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically during build operations and generates the project file
    ///     with the appropriate SDK import style and additional elements.
    /// </remarks>
    protected override void GenerateCsprojFile()
    {
        var rootSdkName = _importStyle == PackageImportStyle.ProjectElement
            ? $"{_packageName}/{_packageVersion}"
            : RootSdk;
        var innerSdkXmlElement = _importStyle == PackageImportStyle.SdkElement
            ? GetSdkElementContent(_packageName)
            : string.Empty;

        var propertiesElement = new XElement("PropertyGroup");
        foreach (var prop in Properties)
            propertiesElement.Add(new XElement(prop.Key, prop.Value));

        var packagesElement = new XElement("ItemGroup");
        foreach (var package in NuGetPackages)
            packagesElement.Add(new XElement("PackageReference",
                new XAttribute("Include", package.Name),
                new XAttribute("Version", package.Version)));

        var hasExplicitOutputType = Properties.Any(p => p.Key == Prop.OutputType);
        var defaultOutputType = hasExplicitOutputType ? "" : "<OutputType>exe</OutputType>";

        var content = $"""
                       <Project Sdk="{rootSdkName}">
                           {innerSdkXmlElement}
                           <PropertyGroup>
                               {defaultOutputType}
                               <ErrorLog>{SarifFileName},version=2.1</ErrorLog>
                               <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                           </PropertyGroup>
                           {propertiesElement}
                           {packagesElement}
                           {string.Join('\n', _additionalProjectElements.Select(static e => e.ToString()))}
                       </Project>
                       """;

        AddFile(ProjectFilename ?? "TestPackage.csproj", content);
    }

    /// <summary>
    ///     Executes a dotnet command with retry logic for SDK resolution failures.
    /// </summary>
    /// <param name="command">The dotnet command to execute.</param>
    /// <param name="arguments">Optional additional arguments.</param>
    /// <param name="environmentVariables">Optional environment variables.</param>
    /// <returns>A task representing the build result.</returns>
    /// <remarks>
    ///     This implementation adds retry logic to handle transient SDK resolution failures
    ///     (MSB4236, restore errors) by automatically retrying with exponential backoff.
    /// </remarks>
    public override async Task<BuildResult> ExecuteDotnetCommandAsync(
        string command,
        string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        BuildCount++;

        var psi = new ProcessStartInfo(await DotNetSdkHelpers.Get(SdkVersion))
        {
            WorkingDirectory = Directory.FullPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add(command);

        var dashDashIndex = arguments is not null ? Array.IndexOf(arguments, "--") : -1;
        if (command == "run")
        {
            psi.ArgumentList.Add("--project");
            psi.ArgumentList.Add(ProjectFilename ?? "TestPackage.csproj");

            if (dashDashIndex >= 0 && arguments is not null)
            {
                for (var i = 0; i < dashDashIndex; i++)
                    psi.ArgumentList.Add(arguments[i]);
                psi.ArgumentList.Add("/bl");
                for (var i = dashDashIndex; i < arguments.Length; i++)
                    psi.ArgumentList.Add(arguments[i]);
            }
            else
            {
                if (arguments is not null)
                    foreach (var arg in arguments)
                        psi.ArgumentList.Add(arg);
                psi.ArgumentList.Add("/bl");
            }
        }
        else
        {
            if (arguments is not null)
                foreach (var arg in arguments)
                    psi.ArgumentList.Add(arg);
            psi.ArgumentList.Add("/bl");
        }

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
        psi.Environment["NUGET_HTTP_CACHE_PATH"] = _packageDirectory / "http-cache";
        psi.Environment["NUGET_PACKAGES"] = _packageDirectory / "packages";
        psi.Environment["NUGET_SCRATCH"] = _packageDirectory / "nuget-scratch";
        psi.Environment["NUGET_PLUGINS_CACHE_PATH"] = _packageDirectory / "nuget-plugins-cache";

        if (environmentVariables is not null)
            foreach (var env in environmentVariables)
                psi.Environment[env.Name] = env.Value;

        var result = await psi.RunAsTaskAsync();

        // Retry logic for SDK resolution failures
        const int maxRetries = 5;
        for (var retry = 0; retry < maxRetries && result.ExitCode is not 0; retry++)
        {
            if (result.Output.Any(static line =>
                line.Text.Contains("error MSB4236", StringComparison.Ordinal) ||
                line.Text.Contains("The project file may be invalid or missing targets required for restore",
                    StringComparison.Ordinal)))
            {
                await Task.Delay(100 * (1 << retry));
                result = await psi.RunAsTaskAsync();
            }
            else
            {
                break;
            }
        }

        var sarifPath = Directory.FullPath / SarifFileName;
        SarifFile? sarif = null;
        if (File.Exists(sarifPath))
        {
            var bytes = await File.ReadAllBytesAsync(sarifPath);
            sarif = JsonSerializer.Deserialize<SarifFile>(bytes);
        }

        var binlogContent = await File.ReadAllBytesAsync(Directory.FullPath / "msbuild.binlog");

        return new BuildResult(result.ExitCode, result.Output, sarif, binlogContent);
    }

    private string GetSdkElementContent(string packageName) =>
        $"""<Sdk Name="{packageName}" Version="{_packageVersion}" />""";
}
