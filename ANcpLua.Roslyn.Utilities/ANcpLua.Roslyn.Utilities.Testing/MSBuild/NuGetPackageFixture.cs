using System.Diagnostics;
using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
///     An xUnit AssemblyFixture that pre-warms NuGet packages for test execution.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="NuGetPackageFixture" /> provides an isolated NuGet package directory and pre-warming
///         infrastructure for integration tests. It handles both CI and local development modes, creating
///         a temporary directory structure for packages and cache files.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 In CI mode, loads packages from the <c>NUGET_DIRECTORY</c> environment variable.
///             </description>
///         </item>
///         <item>
///             <description>
///                 In local mode, creates temporary isolation for test package resolution.
///             </description>
///         </item>
///         <item>
///             <description>Pre-warms NuGet cache with external packages to improve test performance.</description>
///         </item>
///         <item>
///             <description>Configurable package list for different test scenarios.</description>
///         </item>
///         <item>
///             <description>Automatic cleanup via <see cref="IAsyncLifetime" /> integration.</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// [assembly: AssemblyFixture(typeof(NuGetPackageFixture))]
///
/// public class MyIntegrationTests
/// {
///     private readonly NuGetPackageFixture _fixture;
///
///     public MyIntegrationTests(NuGetPackageFixture fixture)
///     {
///         _fixture = fixture;
///     }
///
///     [Fact]
///     public async Task MyTest()
///     {
///         await using var builder = new PackageProjectBuilder(
///             null,
///             _fixture.PackageDirectory,
///             "MyPackage",
///             _fixture.Version);
///
///         var result = await builder.BuildAsync();
///         result.ShouldSucceed();
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="PackageProjectBuilder" />
public class NuGetPackageFixture : IAsyncLifetime
{
    /// <summary>
    ///     Default list of external packages to pre-warm in the NuGet cache.
    /// </summary>
    private static readonly (string Name, string Version)[] DefaultPreWarmPackages =
    [
        ("xunit", "2.9.3"),
        ("xunit.runner.visualstudio", "3.1.5"),
        ("System.Net.Http", "4.3.4"),
        ("OpenTelemetry", "1.15.0"),
        ("OpenTelemetry.Extensions.Hosting", "1.15.0")
    ];

    private readonly TemporaryDirectory _packageDirectory = TemporaryDirectory.Create();
    private readonly (string Name, string Version)[] _preWarmPackages;

    /// <summary>
    ///     Creates a new <see cref="NuGetPackageFixture" /> with default pre-warm packages.
    /// </summary>
    /// <remarks>
    ///     Uses <see cref="DefaultPreWarmPackages" /> for NuGet cache pre-warming.
    ///     For custom package lists, use the constructor that accepts an array of packages.
    /// </remarks>
    public NuGetPackageFixture() : this(DefaultPreWarmPackages)
    {
    }

    /// <summary>
    ///     Creates a new <see cref="NuGetPackageFixture" /> with custom pre-warm packages.
    /// </summary>
    /// <param name="preWarmPackages">
    ///     An array of (name, version) tuples specifying packages to pre-warm in the NuGet cache.
    /// </param>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Pre-warm packages are downloaded and cached during fixture initialization
    ///                 to improve subsequent test performance.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Pass an empty array to skip pre-warming entirely if your tests do not need
    ///                 any pre-cached packages.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public NuGetPackageFixture((string Name, string Version)[] preWarmPackages)
    {
        _preWarmPackages = preWarmPackages;
    }

    /// <summary>
    ///     Gets the package directory path.
    /// </summary>
    /// <value>The full path to the isolated NuGet package directory.</value>
    public FullPath PackageDirectory => _packageDirectory.FullPath;

    /// <summary>
    ///     Gets the package version from the environment or defaults to a test version.
    /// </summary>
    /// <value>
    ///     The version from <c>PACKAGE_VERSION</c> environment variable, or "999.9.9" if not set.
    /// </value>
    public string Version { get; } = Environment.GetEnvironmentVariable("PACKAGE_VERSION") ?? "999.9.9";

    /// <summary>
    ///     Initializes the fixture by setting up the package directory and pre-warming the NuGet cache.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 In CI mode (when <c>CI</c> environment variable is set), loads packages
    ///                 from the <c>NUGET_DIRECTORY</c> environment variable.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 In local development mode, creates an isolated temporary package directory
    ///                 with pre-warmed cache.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Automatically discovers and processes package files.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("CI") is not null)
        {
            if (Environment.GetEnvironmentVariable("NUGET_DIRECTORY") is { } path)
            {
                var files = Directory.GetFiles(path, "*.nupkg", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    foreach (var file in files)
                        File.Copy(file, _packageDirectory.FullPath / Path.GetFileName(file), true);

                    return;
                }

                throw new InvalidOperationException($"No .nupkg files found in {path}");
            }

            throw new InvalidOperationException("NUGET_DIRECTORY environment variable not set in CI mode");
        }

        // Local development mode: pre-warm the cache
        await PreWarmNuGetCacheAsync();
    }

    /// <summary>
    ///     Disposes the fixture and cleans up the temporary package directory.
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        await _packageDirectory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Pre-warms the NuGet cache by restoring configured packages.
    /// </summary>
    /// <remarks>
    ///     This method creates a temporary project that references all pre-warm packages
    ///     and runs a restore operation to populate the NuGet cache. This improves
    ///     performance of subsequent test builds.
    /// </remarks>
    private async Task PreWarmNuGetCacheAsync()
    {
        if (_preWarmPackages.Length is 0)
            return; // Skip if no packages to pre-warm

        var warmupDir = _packageDirectory.FullPath / "warmup";
        Directory.CreateDirectory(warmupDir);

        try
        {
            var nugetConfig = $"""
                               <configuration>
                                   <config>
                                       <add key="globalPackagesFolder" value="{_packageDirectory.FullPath}/packages" />
                                   </config>
                                   <packageSources>
                                       <clear />
                                       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                   </packageSources>
                               </configuration>
                               """;
            await File.WriteAllTextAsync(warmupDir / "NuGet.config", nugetConfig);

            var packageRefs = string.Join("\n        ",
                _preWarmPackages.Select(static p =>
                    $"""<PackageReference Include="{p.Name}" Version="{p.Version}" />"""));

            var csproj = $"""
                          <Project Sdk="Microsoft.NET.Sdk">
                              <PropertyGroup>
                                  <TargetFramework>net10.0</TargetFramework>
                              </PropertyGroup>
                              <ItemGroup>
                                  {packageRefs}
                              </ItemGroup>
                          </Project>
                          """;
            await File.WriteAllTextAsync(warmupDir / "warmup.csproj", csproj);

            var psi = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = warmupDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.AddRange("restore", "--no-cache");

            var result = await psi.RunAsTaskAsync(CancellationToken.None);
            if (result.ExitCode is not 0)
                throw new InvalidOperationException(
                    $"NuGet cache pre-warm failed with exit code {result.ExitCode}. Output: {result.Output}");
        }
        finally
        {
            if (Directory.Exists(warmupDir))
                Directory.Delete(warmupDir, true);
        }
    }
}
