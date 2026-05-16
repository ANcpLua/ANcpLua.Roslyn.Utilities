using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

public partial class ProjectBuilder
{
    /// <summary>
    ///     Configures the NuGet.config file with custom content.
    /// </summary>
    /// <param name="nugetConfigContent">The complete XML content for the NuGet.config file.</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     This method replaces any existing NuGet.config file in the project directory.
    ///     For simpler package source configuration, consider using <see cref="WithPackageSource" /> instead.
    /// </remarks>
    /// <example>
    ///     <code>
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
    /// <seealso cref="WithPackageSource" />
    public ProjectBuilder WithNuGetConfig(string nugetConfigContent)
    {
        Directory.CreateTextFile("NuGet.config", nugetConfigContent);
        return this;
    }

    /// <summary>
    ///     Adds a package source to the NuGet configuration with optional package pattern mapping.
    /// </summary>
    /// <param name="name">The name of the package source (e.g., "local", "myFeed").</param>
    /// <param name="path">The path or URL to the package source.</param>
    /// <param name="packagePatterns">
    ///     Optional package patterns to restrict which packages come from this source.
    ///     When specified, package source mapping is enabled with nuget.org as fallback for unmatched patterns.
    /// </param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Configures an isolated global packages folder within the source path to prevent cache
    ///                 pollution.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Clears existing package sources and adds the specified source plus nuget.org.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 When package patterns are provided, enables package source mapping for security and
    ///                 reproducibility.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Simple local source
    /// builder.WithPackageSource("local", "/path/to/packages");
    ///
    /// // With package pattern mapping
    /// builder.WithPackageSource("myFeed", "/path/to/packages", "MyCompany.*", "MyOrg.*");
    /// </code>
    /// </example>
    /// <seealso cref="WithNuGetConfig" />
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
        Directory.CreateTextFile("NuGet.config", config);
        return this;
    }

    /// <summary>
    ///     Sets the .NET SDK version to use for building the project.
    /// </summary>
    /// <param name="dotnetSdkVersion">The SDK version to use.</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     The SDK is automatically downloaded and cached by <see cref="DotNetSdkHelpers" /> if not already present.
    ///     The default SDK version is <see cref="NetSdkVersion.Net100" />.
    /// </remarks>
    /// <seealso cref="NetSdkVersion" />
    /// <seealso cref="DotNetSdkHelpers" />
    public ProjectBuilder WithDotnetSdkVersion(NetSdkVersion dotnetSdkVersion)
    {
        SdkVersion = dotnetSdkVersion;
        return this;
    }

    /// <summary>
    ///     Enables Microsoft Testing Platform (MTP) mode in the global.json configuration.
    /// </summary>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     This modifies the global.json to include the MTP test runner configuration,
    ///     which is required for using the new Microsoft.Testing.Platform-based test execution model.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var result = await builder
    ///     .WithMtpMode()
    ///     .WithPackage("Microsoft.Testing.Platform", "1.0.0")
    ///     .AddSource("Tests.cs", testCode)
    ///     .TestAsync();
    /// </code>
    /// </example>
    public ProjectBuilder WithMtpMode()
    {
        Directory.CreateTextFile("global.json", """
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
    ///     Sets the target framework for the project.
    /// </summary>
    /// <param name="tfm">The target framework moniker (e.g., "net10.0", "netstandard2.0").</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     Use constants from <see cref="Tfm" /> for common target framework values.
    /// </remarks>
    /// <example>
    ///     <code>
    /// builder.WithTargetFramework(Tfm.Net100);
    /// builder.WithTargetFramework("net8.0-windows");
    /// </code>
    /// </example>
    /// <seealso cref="Tfm" />
    public ProjectBuilder WithTargetFramework(string tfm)
    {
        Properties.Add((Prop.TargetFramework, tfm));
        return this;
    }

    /// <summary>
    ///     Sets the output type of the project.
    /// </summary>
    /// <param name="type">The output type (e.g., "Library", "Exe", "WinExe").</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     Use constants from <see cref="Val" /> for common output type values.
    /// </remarks>
    /// <example>
    ///     <code>
    /// builder.WithOutputType(Val.Library);
    /// builder.WithOutputType(Val.Exe);
    /// </code>
    /// </example>
    /// <seealso cref="Val" />
    public ProjectBuilder WithOutputType(string type)
    {
        Properties.Add((Prop.OutputType, type));
        return this;
    }

    /// <summary>
    ///     Sets the C# language version for the project.
    /// </summary>
    /// <param name="version">
    ///     The language version (e.g., "12.0", "latest", "preview").
    ///     Defaults to <see cref="Val.Latest" />.
    /// </param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <seealso cref="Val.Latest" />
    /// <seealso cref="Val.Preview" />
    public ProjectBuilder WithLangVersion(string version = Val.Latest)
    {
        Properties.Add((Prop.LangVersion, version));
        return this;
    }

    /// <summary>
    ///     Sets an arbitrary MSBuild property on the project.
    /// </summary>
    /// <param name="name">The property name (e.g., "Nullable", "ImplicitUsings").</param>
    /// <param name="value">The property value.</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     Use constants from <see cref="Prop" /> for common property names and <see cref="Val" /> for common values.
    /// </remarks>
    /// <example>
    ///     <code>
    /// builder
    ///     .WithProperty(Prop.Nullable, Val.Enable)
    ///     .WithProperty(Prop.ImplicitUsings, Val.Enable)
    ///     .WithProperty("CustomProperty", "CustomValue");
    /// </code>
    /// </example>
    /// <seealso cref="Prop" />
    /// <seealso cref="Val" />
    /// <seealso cref="WithProperties" />
    public ProjectBuilder WithProperty(string name, string value)
    {
        Properties.Add((name, value));
        return this;
    }

    /// <summary>
    ///     Sets multiple MSBuild properties on the project.
    /// </summary>
    /// <param name="properties">An array of key-value tuples representing property names and values.</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <example>
    ///     <code>
    /// builder.WithProperties(
    ///     (Prop.Nullable, Val.Enable),
    ///     (Prop.ImplicitUsings, Val.Enable),
    ///     (Prop.TreatWarningsAsErrors, Val.True));
    /// </code>
    /// </example>
    /// <seealso cref="WithProperty" />
    public ProjectBuilder WithProperties(params (string Key, string Value)[] properties)
    {
        Properties.AddRange(properties);
        return this;
    }

    /// <summary>
    ///     Records the value of one or more MSBuild properties during the build for later assertion.
    /// </summary>
    /// <param name="propertyNames">The names of properties to record (e.g., "TargetFramework", "OutputType").</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Mirrors <c>dotnet/sdk</c>'s <c>GetValuesCommand</c> pattern. The emitted csproj
    ///         contains a <c>_WriteRecordedProperties</c> target that runs <c>AfterTargets="Build"</c>
    ///         and writes <c>obj/recorded.{TargetFramework}.properties</c> with <c>Name=Value</c>
    ///         lines. After the build, retrieve values via
    ///         <see cref="BuildResult.GetRecordedProperty" /> or assert via
    ///         <c>BuildResultAssertions.ShouldHaveRecordedProperty</c>.
    ///     </para>
    ///     <para>
    ///         Prefer this over <see cref="BuildResult.GetMsBuildPropertyValue" /> (binlog parsing) —
    ///         binlog scans can lose late property events under cold-cache parallel restore on
    ///         Windows, and produce a single shared sink under cross-targeting. Recorded properties
    ///         are written deterministically per-TFM by MSBuild itself.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var result = await builder
    ///     .RecordProperties("TargetFramework", "_IsSourceGeneratorProject")
    ///     .BuildAsync();
    ///
    /// result.ShouldHaveRecordedProperty("_IsSourceGeneratorProject", "true");
    /// </code>
    /// </example>
    /// <seealso cref="BuildResult.GetRecordedProperty" />
    public ProjectBuilder RecordProperties(params string[] propertyNames)
    {
        foreach (var name in propertyNames)
            PropertiesToRecord.Add(name);

        return this;
    }

    /// <summary>
    ///     Builds the inline MSBuild target XML that records requested properties to disk.
    /// </summary>
    /// <returns>
    ///     XML for an inline <c>&lt;Target&gt;</c> emitting one <c>obj/recorded.{TargetFramework}.properties</c>
    ///     per TFM, or <see cref="string.Empty" /> when no properties are recorded.
    /// </returns>
    /// <remarks>
    ///     Concrete builders should embed this XML in their generated csproj alongside
    ///     <c>&lt;PropertyGroup&gt;</c> and <c>&lt;ItemGroup&gt;</c> blocks. The target uses
    ///     <c>WriteLinesToFile</c> with <c>Overwrite="true"</c>, so re-runs of the same TFM
    ///     produce idempotent output.
    /// </remarks>
    protected string GetRecordPropertiesTargetXml()
    {
        if (PropertiesToRecord.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var name in PropertiesToRecord.OrderBy(static n => n, StringComparer.Ordinal))
        {
            if (sb.Length > 0)
                sb.AppendLine();

            sb.Append("      <_Recorded Include=\"").Append(name).Append("=$(").Append(name).Append(")\" />");
        }

        var items = sb.ToString();

        return $"""
                  <Target Name="_WriteRecordedProperties" AfterTargets="Build">
                    <ItemGroup>
                {items}
                    </ItemGroup>
                    <WriteLinesToFile File="$(MSBuildProjectDirectory)\obj\recorded.$(TargetFramework).properties"
                                      Lines="@(_Recorded)"
                                      Overwrite="true" />
                  </Target>
                """;
    }

    /// <summary>
    ///     Loads recorded property files written by <c>_WriteRecordedProperties</c> into a TFM-keyed dictionary.
    /// </summary>
    /// <param name="projectDirectory">The project directory containing the <c>obj/</c> folder.</param>
    /// <returns>
    ///     A dictionary keyed by target framework moniker, each entry mapping property name to value.
    ///     Empty when no recording was requested or no files were produced.
    /// </returns>
    protected static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadRecordedProperties(string projectDirectory)
    {
        var objDir = Path.Combine(projectDirectory, "obj");
        if (!System.IO.Directory.Exists(objDir))
            return new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var file in System.IO.Directory.EnumerateFiles(objDir, "recorded.*.properties"))
        {
            var stem = Path.GetFileNameWithoutExtension(file);
            const string prefix = "recorded.";
            if (!stem.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var tfm = stem[prefix.Length..];
            var props = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in File.ReadLines(file))
            {
                var idx = line.IndexOf('=');
                if (idx <= 0)
                    continue;

                props[line[..idx]] = line[(idx + 1)..];
            }

            result[tfm] = props;
        }

        return result;
    }

    /// <summary>
    ///     Adds a source file to the project.
    /// </summary>
    /// <param name="filename">The filename for the source file (e.g., "Program.cs", "Models/User.cs").</param>
    /// <param name="content">The C# source code content.</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     Source files are written to the project directory when <see cref="BuildAsync" />, <see cref="RunAsync" />,
    ///     <see cref="TestAsync" />, or <see cref="PackAsync" /> is called.
    /// </remarks>
    /// <example>
    ///     <code>
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
    /// <seealso cref="AddFile" />
    public ProjectBuilder AddSource(string filename, string content)
    {
        SourceFiles.Add((filename, content));
        return this;
    }

    /// <summary>
    ///     Adds a NuGet package reference to the project.
    /// </summary>
    /// <param name="name">The package identifier (e.g., "Microsoft.CodeAnalysis.CSharp").</param>
    /// <param name="version">The package version (e.g., "4.12.0").</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <example>
    ///     <code>
    /// builder
    ///     .WithPackage("Microsoft.CodeAnalysis.CSharp", "4.12.0")
    ///     .WithPackage("xunit", "2.9.2");
    /// </code>
    /// </example>
    /// <seealso cref="NuGetReference" />
    public ProjectBuilder WithPackage(string name, string version)
    {
        NuGetPackages.Add(new NuGetReference(name, version));
        return this;
    }

    /// <summary>
    ///     Sets the project filename.
    /// </summary>
    /// <param name="filename">The filename for the .csproj file (e.g., "MyProject.csproj").</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     The default filename is "TestProject.csproj".
    /// </remarks>
    public ProjectBuilder WithFilename(string filename)
    {
        ProjectFilename = filename;
        return this;
    }

    /// <summary>
    ///     Sets the root SDK for the project.
    /// </summary>
    /// <param name="sdk">The SDK identifier (e.g., "Microsoft.NET.Sdk", "Microsoft.NET.Sdk.Web", "Microsoft.NET.Sdk.Worker").</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     The default SDK is "Microsoft.NET.Sdk".
    /// </remarks>
    /// <example>
    ///     <code>
    /// builder.WithRootSdk("Microsoft.NET.Sdk.Web");
    /// </code>
    /// </example>
    public ProjectBuilder WithRootSdk(string sdk)
    {
        RootSdk = sdk;
        return this;
    }

    /// <summary>
    ///     Adds a Directory.Build.props file to the project directory.
    /// </summary>
    /// <param name="content">The XML content for the Directory.Build.props file.</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     Directory.Build.props files are evaluated before the project file and are useful for
    ///     setting properties that should apply to all projects in a directory hierarchy.
    /// </remarks>
    /// <example>
    ///     <code>
    /// builder.WithDirectoryBuildProps("""
    ///     &lt;Project&gt;
    ///         &lt;PropertyGroup&gt;
    ///             &lt;TreatWarningsAsErrors&gt;true&lt;/TreatWarningsAsErrors&gt;
    ///         &lt;/PropertyGroup&gt;
    ///     &lt;/Project&gt;
    ///     """);
    /// </code>
    /// </example>
    /// <seealso cref="WithDirectoryPackagesProps" />
    public ProjectBuilder WithDirectoryBuildProps(string content)
    {
        AddFile("Directory.Build.props", content);
        return this;
    }

    /// <summary>
    ///     Adds a Directory.Packages.props file for Central Package Management (CPM).
    /// </summary>
    /// <param name="content">The XML content for the Directory.Packages.props file.</param>
    /// <returns>The current <see cref="ProjectBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     Central Package Management allows you to manage package versions in a single location.
    ///     When using CPM, package references in project files should not specify versions.
    /// </remarks>
    /// <example>
    ///     <code>
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
    /// <seealso cref="WithDirectoryBuildProps" />
    public ProjectBuilder WithDirectoryPackagesProps(string content)
    {
        AddFile("Directory.Packages.props", content);
        return this;
    }
}
