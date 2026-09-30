using ANcpLua.Roslyn.Utilities.Testing.MSBuild;
using Meziantou.Framework;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

// Consumes .Sources and .Polyfills the way a netstandard2.0 generator does: packed from this checkout,
// restored from a throwaway feed, compiled by the installed SDK with warnings as errors.
public sealed class PackageConsumptionTests(LocalPackageFeed feed) : IClassFixture<LocalPackageFeed>
{
    private const string UsesUtilities = """
        using System.Globalization;
        using System.Linq;
        using ANcpLua.Roslyn.Utilities;
        using Microsoft.CodeAnalysis;
        using Microsoft.CodeAnalysis.Operations;

        namespace Consumer;

        public sealed record Cat(string Name);

        public sealed record Dog(int Age);

        public union Pet(Cat, Dog);

        internal static class Uses
        {
            public static int Operations(IOperation operation) =>
                operation.Descendants().Count() + operation.DescendantsAndSelf().Count() +
                operation.DescendantsOfType<IInvocationOperation>().Count();

            public static int Linq(int[] values) =>
                values.DistinctBy(static v => v).Count() + values.SkipLast(1).Count() + values.TakeLast(1).Count();

            public static string Name(Pet pet) => pet switch
            {
                Cat cat => cat.Name,
                Dog dog => dog.Age.ToString(CultureInfo.InvariantCulture)
            };

            public static EquatableArray<int> Equatable(int[] values) => values.ToEquatableArray();
        }
        """;

    private const string OwnIsExternalInit = """
        namespace System.Runtime.CompilerServices
        {
            internal static class IsExternalInit
            {
            }
        }
        """;

    private const string OwnDistinctBy = """
        namespace System.Linq
        {
            internal static class OwnEnumerable
            {
                public static System.Collections.Generic.IEnumerable<T> DistinctBy<T, TKey>(
                    this System.Collections.Generic.IEnumerable<T> source, System.Func<T, TKey> keySelector) =>
                    source.GroupBy(keySelector).Select(static group => group.First());
            }
        }
        """;

    private const string UsesOwnPolyfills = """
        using System.Linq;

        namespace Consumer;

        public sealed record Cat(string Name);

        internal static class Uses
        {
            public static int Distinct(Cat[] cats) => cats.DistinctBy(static cat => cat.Name).Count();
        }
        """;

    private static readonly string[] BuildArguments = ["-nodeReuse:false"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SourcesPackage_CompilesInGenerator_WithAndWithoutPolyfills(bool withPolyfills)
    {
        await using var project = new ProjectBuilder(TestContext.Current.TestOutputHelper);
        AsGenerator(project, withPolyfills).AddSource("Uses.cs", UsesUtilities);

        (await project.BuildAsync(BuildArguments)).ShouldSucceed();
    }

    [Fact]
    public async Task OptOutSwitches_YieldToConsumerOwnPolyfills()
    {
        await using var project = new ProjectBuilder(TestContext.Current.TestOutputHelper);
        AsGenerator(project, withPolyfills: true)
            .WithProperty(Prop.InjectIsExternalInitOnLegacy, Val.False)
            .WithProperty(Prop.InjectLinqPolyfill, Val.False)
            // Paths the package rules would match (/Polyfills/, /IsExternalInit.cs, /Linq/): only the
            // NuGetPackageId scoping keeps them in the build.
            .AddSource("Own/Polyfills/IsExternalInit.cs", OwnIsExternalInit)
            .AddSource("Own/Linq/OwnDistinctBy.cs", OwnDistinctBy)
            .AddSource("Uses.cs", UsesOwnPolyfills);

        (await project.BuildAsync(BuildArguments)).ShouldSucceed();
    }

    private ProjectBuilder AsGenerator(ProjectBuilder project, bool withPolyfills)
    {
        project
            .WithDotnetSdkVersion(NetSdkVersion.Ambient)
            .WithNuGetConfig(feed.NuGetConfig)
            .WithTargetFramework(Tfm.NetStandard20)
            .WithLangVersion()
            .WithProperty("Nullable", "enable")
            .WithProperty("IsRoslynComponent", Val.True)
            .WithProperty("EnforceExtendedAnalyzerRules", Val.True)
            .WithProperty(Prop.TreatWarningsAsErrors, Val.True)
            .WithPackage("Microsoft.CodeAnalysis.CSharp", typeof(SyntaxNode).Assembly.GetName().Version!.ToString(3))
            .WithPackage("ANcpLua.Roslyn.Utilities.Sources", LocalPackageFeed.Version);

        return withPolyfills ? project.WithPackage("ANcpLua.Roslyn.Utilities.Polyfills", LocalPackageFeed.Version) : project;
    }
}

// Packs .Sources and .Polyfills from this checkout into a throwaway feed, once for the test class.
public sealed class LocalPackageFeed : IAsyncLifetime
{
    public const string Version = "0.0.0-regression";

    private readonly TemporaryDirectory _directory = TemporaryDirectory.Create();

    // The feed's own packages extract into the throwaway folder; everything else comes read-only from the
    // machine's NuGet cache, so nothing is downloaded twice and nothing leaks into that cache.
    public string NuGetConfig => $"""
        <configuration>
            <config>
                <add key="globalPackagesFolder" value="{_directory.FullPath / "packages"}" />
            </config>
            <packageSources>
                <clear />
                <add key="local" value="{_directory.FullPath}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
            </packageSources>
            <fallbackPackageFolders>
                <add key="machine" value="{MachinePackages}" />
            </fallbackPackageFolders>
        </configuration>
        """;

    private static string MachinePackages =>
        Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } packages
            ? packages
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

    public async ValueTask InitializeAsync()
    {
        FullPath root = RepositoryRoot.Locate();
        foreach (var package in (string[])["ANcpLua.Roslyn.Utilities.Sources", "ANcpLua.Roslyn.Utilities.Polyfills"])
        {
            await using var packer = new ProjectBuilder();
            var result = await packer.WithDotnetSdkVersion(NetSdkVersion.Ambient).ExecuteDotnetCommandAsync("pack",
            [
                root / "src" / package / $"{package}.csproj", "-c", "Release", "-o", _directory.FullPath,
                "-p:VersionPrefix=0.0.0", "-p:VersionSuffix=regression", "-nodeReuse:false"
            ]);
            result.ShouldSucceed();
        }
    }

    public ValueTask DisposeAsync()
    {
        _directory.Dispose();
        return ValueTask.CompletedTask;
    }
}
