using ANcpLua.Roslyn.Utilities.Testing.MSBuild;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class AnalyzerTestFixtureTests
{
    [Fact]
    public void DictionaryAnalyzerConfigOptionsProvider_LayersFileOptionsOverGlobalOptions()
    {
        var provider = new DictionaryAnalyzerConfigOptionsProvider(
            new Dictionary<string, string> { ["build_property.Mode"] = "global", ["build_property.Only"] = "global" },
            new Dictionary<string, string> { ["build_property.Mode"] = "file" });
        var tree = CSharpSyntaxTree.ParseText("class C { }", cancellationToken: TestContext.Current.CancellationToken);

        provider.GlobalOptions.TryGetValue("BUILD_PROPERTY.MODE", out var global).Should().BeTrue();
        global.Should().Be("global");
        provider.GetOptions(tree).TryGetValue("build_property.Mode", out var file).Should().BeTrue();
        file.Should().Be("file");
        provider.GetOptions(tree).TryGetValue("build_property.Only", out var inherited).Should().BeTrue();
        inherited.Should().Be("global");
        provider.GlobalOptions.Keys.Should().BeEquivalentTo("build_property.Mode", "build_property.Only");
    }

    [Fact]
    public void SourceFixture_InMethod_CompilesTheStatementsInsideTheMethod()
    {
        var source = SourceFixture.InMethod(
            "var total = values.Count + Offset;",
            declarations: "using System.Collections.Generic;",
            members: "private const int Offset = 1;",
            parameters: "List<int> values");

        Compile.Source(source).Build().ShouldSucceed();
        source.Should().Contain("internal void Emit(List<int> values)");
    }

    [Fact]
    public void RepositoryRoot_LocateFrom_FindsTheMarkerAboveTheStartDirectory()
    {
        var root = RepositoryRoot.LocateFrom(AppContext.BaseDirectory, "ANcpLua.Roslyn.Utilities.slnx");

        File.Exists(root["ANcpLua.Roslyn.Utilities.slnx"]).Should().BeTrue();
        AppContext.BaseDirectory.Should().StartWith(root.FullPath.Value);
    }
}
