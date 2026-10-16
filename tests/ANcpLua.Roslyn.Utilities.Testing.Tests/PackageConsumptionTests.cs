using ANcpLua.Roslyn.Utilities.Testing.MSBuild;
using Meziantou.Framework;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

// Consumes .Sources and .Polyfills the way a netstandard2.0 generator does: packed from this checkout,
// restored from a throwaway feed, compiled by the installed SDK with warnings as errors.
public sealed class PackageConsumptionTests(LocalPackageFeed feed) : IClassFixture<LocalPackageFeed>
{
    private const string UsesUtilities = """
        using System.Linq;
        using ANcpLua.Roslyn.Utilities;
        using Microsoft.CodeAnalysis;
        using Microsoft.CodeAnalysis.Operations;

        namespace Consumer;

        public sealed record Cat(string Name);

        internal static class Uses
        {
            public static int Operations(IOperation operation) =>
                operation.Descendants().Count() + operation.DescendantsAndSelf().Count() +
                operation.DescendantsOfType<IInvocationOperation>().Count();

            public static int Linq(int[] values) =>
                values.DistinctBy(static v => v).Count() + values.SkipLast(1).Count() + values.TakeLast(1).Count();

            public static EquatableArray<int> Equatable(int[] values) => values.ToEquatableArray();

            // A List<T> is a reference type, so a nullable combinator (T? where T : class) under a LINQ name applied to
            // it with T = List<Cat> whenever the lambda bound for the list as well, and its identity conversion beat
            // Enumerable's: this returned string?. The combinators are Map, Bind and Filter for that reason.
            public static System.Collections.Generic.IEnumerable<string> Names(System.Collections.Generic.List<Cat> cats) =>
                cats.Where(static cat => cat.GetHashCode() != 0).Select(static cat => cat.ToString());
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

    // A consumer without any Microsoft.CodeAnalysis reference: an application or tool that only wants the
    // Roslyn-free helpers. Every call below resolves to a file the package ships outside Roslyn/.
    private const string UsesRoslynFreeHelpers = """
        using System.Linq;
        using ANcpLua.Roslyn.Utilities;
        using ANcpLua.Roslyn.Utilities.Security;

        namespace Consumer;

        internal static class Uses
        {
            public static string Guarded(string? name) => Guard.NotNullOrWhiteSpace(name);

            // NullableExtensions ships outside Roslyn/ (its Match method shares the name of the Roslyn-side Match class).
            public static string Fallback(string? name) => name.Match(static n => n, static () => "none");

            public static System.Collections.Generic.IEnumerable<string> Names(System.Collections.Generic.List<string> values) =>
                values.Where(static v => v.GetHashCode() != 0).Select(static v => v.ToString());

            public static bool Same(string a, string b) => a.EqualsIgnoreCase(b) && a.StartsWithOrdinal("x");

            public static int? Parsed(string text) => text.TryParseInt32();

            public static string Hashed(string text) => Sha256Hex.Hash(text);

            public static int Inserted(System.Collections.Generic.Dictionary<string, int> counts) =>
                counts.GetOrInsert("key", 1, static seed => seed);

            // Any IDictionary, not only Dictionary: a SortedDictionary is the common other one.
            public static System.Collections.Generic.List<string> Members(System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.List<string>> groups) =>
                groups.GetOrAdd("key");
        }
        """;

    [Fact]
    public async Task TestingCorePackage_RunsCompilationWithoutInstallingAnyTestFramework()
    {
        await using var project = new ProjectBuilder(static message => TestContext.Current.TestOutputHelper?.WriteLine(message));
        project.WithDotnetSdkVersion(NetSdkVersion.Ambient)
            .WithNuGetConfig(feed.NuGetConfig)
            .WithTargetFramework(Tfm.Net100)
            .WithOutputType(Val.Exe)
            .WithPackage("ANcpLua.Roslyn.Utilities.Testing", LocalPackageFeed.Version)
            .AddSource("Program.cs", """
                using System;
                using ANcpLua.Roslyn.Utilities.Testing;
                if (args.Length != 0) return 2;
                var result = Compile.Source("public class C { }").Build();
                if (!result.Succeeded || !result.ContainsType("C")) return 1;
                Console.WriteLine("neutral compilation passed");
                return 0;
                """);
        var result = await project.RunAsync();
        result.ShouldSucceed();
        Assert.True(result.OutputContains("neutral compilation passed"));
        Assert.NotEmpty(result.BinaryLogContent);
        using var assets = JsonDocument.Parse(await File.ReadAllTextAsync(
            project.RootFolder / "obj" / "project.assets.json", TestContext.Current.CancellationToken));
        var packages = assets.RootElement.GetProperty("libraries").EnumerateObject().Select(static entry => entry.Name).ToArray();
        Assert.Contains(packages, static package => package.StartsWith("ANcpLua.Roslyn.Utilities.Testing/", StringComparison.Ordinal));
        Assert.DoesNotContain(packages, static package =>
            package.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
            package.StartsWith("NUnit/", StringComparison.OrdinalIgnoreCase) ||
            package.StartsWith("TUnit", StringComparison.OrdinalIgnoreCase) ||
            package.StartsWith("bunit/", StringComparison.OrdinalIgnoreCase));
    }

    private static readonly string[] BuildArguments = ["-nodeReuse:false"];

    // Documentation is generated too: the compiler then resolves every cref, so a Roslyn-free file must not point
    // at a type that ships under Roslyn/.
    [Fact]
    public async Task SourcesPackage_CompilesWithoutRoslyn_WhenRoslynSourcesAreNotInjected()
    {
        await using var project = new ProjectBuilder(static message => TestContext.Current.TestOutputHelper?.WriteLine(message));
        project
            .WithDotnetSdkVersion(NetSdkVersion.Ambient)
            .WithNuGetConfig(feed.NuGetConfig)
            .WithTargetFramework(Tfm.Net100)
            .WithLangVersion()
            .WithProperty("Nullable", "enable")
            .WithProperty("GenerateDocumentationFile", Val.True)
            .WithProperty(Prop.TreatWarningsAsErrors, Val.True)
            .WithProperty(Prop.InjectRoslynSources, Val.False)
            .WithPackage("ANcpLua.Roslyn.Utilities.Sources", LocalPackageFeed.Version)
            .AddSource("Uses.cs", UsesRoslynFreeHelpers);

        (await project.BuildAsync(BuildArguments)).ShouldSucceed();
    }

    // A NativeAOT tool compiles every injected file under the trim and AOT analyzers, reachable or not, so the
    // Roslyn-free helpers carry their reflection requirements as annotations (IL2070, IL2075, IL2091 otherwise).
    [Fact]
    public async Task SourcesPackage_IsTrimAndAotAnalyzerClean_WithoutRoslyn()
    {
        await using var project = new ProjectBuilder(static message => TestContext.Current.TestOutputHelper?.WriteLine(message));
        project
            .WithDotnetSdkVersion(NetSdkVersion.Ambient)
            .WithNuGetConfig(feed.NuGetConfig)
            .WithTargetFramework(Tfm.Net100)
            .WithLangVersion()
            .WithProperty("Nullable", "enable")
            .WithProperty("IsAotCompatible", Val.True)
            .WithProperty(Prop.TreatWarningsAsErrors, Val.True)
            .WithProperty(Prop.InjectRoslynSources, Val.False)
            .WithPackage("ANcpLua.Roslyn.Utilities.Sources", LocalPackageFeed.Version)
            .AddSource("Uses.cs", UsesRoslynFreeHelpers);

        (await project.BuildAsync(BuildArguments)).ShouldSucceed();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SourcesPackage_CompilesInGenerator_WithAndWithoutPolyfills(bool withPolyfills)
    {
        await using var project = new ProjectBuilder(static message => TestContext.Current.TestOutputHelper?.WriteLine(message));
        AsGenerator(project, withPolyfills).AddSource("Uses.cs", UsesUtilities);

        (await project.BuildAsync(BuildArguments)).ShouldSucceed();
    }

    // The oldest Microsoft.CodeAnalysis the .Sources package supports; newer enum members and APIs stay out of it.
    [Fact]
    public async Task SourcesPackage_CompilesInGenerator_OnRoslyn414()
    {
        await using var project = new ProjectBuilder(static message => TestContext.Current.TestOutputHelper?.WriteLine(message));
        AsGenerator(project, withPolyfills: true, roslynVersion: "4.14.0").AddSource("Uses.cs", UsesUtilities);

        (await project.BuildAsync(BuildArguments)).ShouldSucceed();
    }

    [Fact]
    public async Task OptOutSwitches_YieldToConsumerOwnPolyfills()
    {
        await using var project = new ProjectBuilder(static message => TestContext.Current.TestOutputHelper?.WriteLine(message));
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

    private ProjectBuilder AsGenerator(ProjectBuilder project, bool withPolyfills, string? roslynVersion = null)
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
            .WithPackage("Microsoft.CodeAnalysis.CSharp", roslynVersion ?? typeof(SyntaxNode).Assembly.GetName().Version!.ToString(3))
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
        foreach (var package in (string[])["ANcpLua.Roslyn.Utilities.Sources", "ANcpLua.Roslyn.Utilities.Polyfills", "ANcpLua.Roslyn.Utilities", "ANcpLua.Roslyn.Utilities.Testing"])
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
