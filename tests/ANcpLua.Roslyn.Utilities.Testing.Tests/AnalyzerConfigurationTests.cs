using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class AnalyzerConfigurationTests
{
    [Theory]
    [InlineData(" true ", true)]
    [InlineData(" FALSE ", false)]
    [InlineData(" 1 ", true)]
    [InlineData(" 0 ", false)]
    public void AnalyzerOptionsBooleanConfiguration_TrimsWhitespace(string text, bool expected)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            "class C { }",
            cancellationToken: TestContext.Current.CancellationToken);
        var options = new AnalyzerOptions(
            ImmutableArray<AdditionalText>.Empty,
            new DictionaryAnalyzerConfigOptionsProvider(
                globalOptions: new Dictionary<string, string>(),
                fileOptions: new Dictionary<string, string> { ["feature"] = text }));

        options.GetConfigurationValue(syntaxTree, "feature", !expected).Should().Be(expected);
    }

    [Theory]
    [InlineData(" true ", true)]
    [InlineData(" FALSE ", false)]
    [InlineData(" 1 ", true)]
    [InlineData(" 0 ", false)]
    public void AnalyzerConfigOptionsProviderGlobalBool_TrimsWhitespace(string text, bool expected)
    {
        var provider = new DictionaryAnalyzerConfigOptionsProvider(
            new Dictionary<string, string>
            {
                ["build_property.Feature"] = text
            });

        provider.TryGetGlobalBool("Feature", out var actual).Should().BeTrue();
        actual.Should().Be(expected);
    }
}
