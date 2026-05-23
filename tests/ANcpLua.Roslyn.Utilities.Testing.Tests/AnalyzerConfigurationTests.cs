using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
                globalOptions: [],
                treeOptions: new Dictionary<string, string?> { ["feature"] = text }));

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
            new Dictionary<string, string?>
            {
                ["build_property.Feature"] = text
            },
            treeOptions: []);

        provider.TryGetGlobalBool("Feature", out var actual).Should().BeTrue();
        actual.Should().Be(expected);
    }

    private sealed class DictionaryAnalyzerConfigOptionsProvider(
        Dictionary<string, string?> globalOptions,
        Dictionary<string, string?> treeOptions)
        : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions = new DictionaryAnalyzerConfigOptions(globalOptions);
        private readonly AnalyzerConfigOptions _treeOptions = new DictionaryAnalyzerConfigOptions(treeOptions);

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return _treeOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return new DictionaryAnalyzerConfigOptions([]);
        }
    }

    private sealed class DictionaryAnalyzerConfigOptions(Dictionary<string, string?> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            return values.TryGetValue(key, out value) && value is not null;
        }
    }
}
