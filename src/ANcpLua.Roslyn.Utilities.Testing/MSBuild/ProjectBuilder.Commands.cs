using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

public partial class ProjectBuilder
{
    /// <summary>
    ///     Builds the project and returns the result.
    /// </summary>
    /// <param name="buildArguments">Optional additional arguments to pass to the dotnet build command.</param>
    /// <param name="environmentVariables">Optional environment variables to set during the build.</param>
    /// <returns>A <see cref="BuildResult" /> containing the build output, SARIF diagnostics, and binary log.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Generates the .csproj file from the configured properties and packages.</description>
    ///         </item>
    ///         <item>
    ///             <description>Writes all source files to the project directory.</description>
    ///         </item>
    ///         <item>
    ///             <description>Executes <c>dotnet build</c> with automatic binary log generation.</description>
    ///         </item>
    ///         <item>
    ///             <description>Parses SARIF output for structured diagnostic access.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var result = await builder
    ///     .WithTargetFramework(Tfm.Net100)
    ///     .AddSource("Program.cs", code)
    ///     .BuildAsync();
    ///
    /// result.ShouldSucceed();
    /// Assert.False(result.HasWarning("CS0618"));
    /// </code>
    /// </example>
    /// <seealso cref="BuildResult" />
    /// <seealso cref="RunAsync" />
    /// <seealso cref="TestAsync" />
    /// <seealso cref="PackAsync" />
    public Task<BuildResult> BuildAsync(string[]? buildArguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in SourceFiles)
            AddFile(name, content);

        return ExecuteDotnetCommandAsync("build", buildArguments, environmentVariables);
    }

    /// <summary>
    ///     Builds and runs the project, returning the result.
    /// </summary>
    /// <param name="arguments">Optional arguments to pass to the application after the <c>--</c> separator.</param>
    /// <param name="environmentVariables">Optional environment variables to set during execution.</param>
    /// <returns>A <see cref="BuildResult" /> containing the run output and any diagnostics.</returns>
    /// <remarks>
    ///     This method is suitable for testing console applications. The project must have an executable output type.
    ///     Arguments are passed after <c>--</c> to separate them from dotnet CLI arguments.
    /// </remarks>
    /// <example>
    ///     <code>
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
    /// <seealso cref="BuildAsync" />
    public Task<BuildResult> RunAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in SourceFiles)
            AddFile(name, content);

        return ExecuteDotnetCommandAsync("run", ["--", .. arguments ?? []], environmentVariables);
    }

    /// <summary>
    ///     Builds and tests the project, returning the result.
    /// </summary>
    /// <param name="arguments">Optional additional arguments to pass to the dotnet test command.</param>
    /// <param name="environmentVariables">Optional environment variables to set during testing.</param>
    /// <returns>A <see cref="BuildResult" /> containing the test output and any diagnostics.</returns>
    /// <remarks>
    ///     The project should reference a test framework (xUnit, NUnit, MSTest) and contain test classes.
    ///     Consider using <see cref="WithMtpMode" /> for Microsoft Testing Platform-based test execution.
    /// </remarks>
    /// <example>
    ///     <code>
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
    /// <seealso cref="WithMtpMode" />
    /// <seealso cref="BuildAsync" />
    public Task<BuildResult> TestAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in SourceFiles)
            AddFile(name, content);

        return ExecuteDotnetCommandAsync("test", arguments, environmentVariables);
    }

    /// <summary>
    ///     Packs the project into a NuGet package, returning the result.
    /// </summary>
    /// <param name="arguments">Optional additional arguments to pass to the dotnet pack command.</param>
    /// <param name="environmentVariables">Optional environment variables to set during packing.</param>
    /// <returns>A <see cref="BuildResult" /> containing the pack output and any diagnostics.</returns>
    /// <remarks>
    ///     The project should be configured as packable (IsPackable=true or default for library projects).
    ///     The resulting .nupkg file will be in the bin/Release directory.
    /// </remarks>
    /// <example>
    ///     <code>
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
    /// <seealso cref="BuildAsync" />
    public Task<BuildResult> PackAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        foreach (var (name, content) in SourceFiles)
            AddFile(name, content);

        return ExecuteDotnetCommandAsync("pack", arguments, environmentVariables);
    }

    /// <summary>
    ///     Restores NuGet packages for the project.
    /// </summary>
    /// <param name="arguments">Optional additional arguments to pass to the dotnet restore command.</param>
    /// <param name="environmentVariables">Optional environment variables to set during restore.</param>
    /// <returns>A <see cref="BuildResult" /> containing the restore output and any diagnostics.</returns>
    /// <remarks>
    ///     Restore is typically called automatically by build, run, test, and pack commands.
    ///     Use this method when you need to explicitly restore packages or diagnose restore issues.
    /// </remarks>
    /// <seealso cref="BuildAsync" />
    public Task<BuildResult> RestoreAsync(string[]? arguments = null,
        (string Name, string Value)[]? environmentVariables = null)
    {
        GenerateCsprojFile();

        return ExecuteDotnetCommandAsync("restore", arguments, environmentVariables);
    }

    /// <summary>
    ///     Generates the .csproj file from the configured properties and packages.
    /// </summary>
    /// <remarks>
    ///     Override this method in derived classes to customize the project file generation,
    ///     such as adding SDK import styles or additional project elements.
    /// </remarks>
    protected virtual void GenerateCsprojFile()
    {
        var propertiesElement = new XElement("PropertyGroup");
        foreach (var prop in Properties)
            propertiesElement.Add(new XElement(prop.Key, prop.Value));

        var packagesElement = new XElement("ItemGroup");
        foreach (var package in NuGetPackages)
            packagesElement.Add(new XElement("PackageReference",
                new XAttribute("Include", package.Name),
                new XAttribute("Version", package.Version)));

        // Per-TFM SARIF path: cross-targeting builds otherwise race for a single
        // BuildOutput.sarif sink and produce concatenated invalid JSON. Each TFM
        // gets its own file; BuildResult merges them on read.
        var content = $"""
                       <Project Sdk="{RootSdk}">
                           <PropertyGroup>
                               <ErrorLog>BuildOutput.$(TargetFramework).sarif,version=2.1</ErrorLog>
                           </PropertyGroup>
                           {propertiesElement}
                           {packagesElement}
                       {GetRecordPropertiesTargetXml()}
                       </Project>
                       """;

        var fullPath = Directory.FullPath / (ProjectFilename ?? "TestProject.csproj");
        fullPath.CreateParentDirectory();
        File.WriteAllText(fullPath, content);
    }
}
