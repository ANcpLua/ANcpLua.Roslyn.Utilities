using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     An <see cref="AnalyzerConfigOptionsProvider" /> backed by dictionaries, for analyzer and generator tests that
///     read MSBuild properties (<c>build_property.*</c>) or <c>.editorconfig</c> keys without running a build.
/// </summary>
/// <remarks>
///     As in a real build, the global options also apply to every syntax tree and additional file. The file options
///     apply to every syntax tree and additional file on top of them and win on a shared key; they never reach
///     <see cref="GlobalOptions" />. Keys compare with <see cref="AnalyzerConfigOptions.KeyComparer" />.
/// </remarks>
/// <example>
///     <code>
/// var options = new AnalyzerOptions([], new DictionaryAnalyzerConfigOptionsProvider(
///     new Dictionary&lt;string, string&gt; { ["build_property.EnableFeature"] = "true" }));
/// var diagnostics = await compilation.WithAnalyzers([analyzer], options).GetAnalyzerDiagnosticsAsync();
/// </code>
/// </example>
public sealed class DictionaryAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly Options _fileOptions;
    private readonly Options _globalOptions;

    /// <summary>
    ///     Creates a provider that serves <paramref name="globalOptions" /> globally and, together with
    ///     <paramref name="fileOptions" />, for every syntax tree and additional file.
    /// </summary>
    /// <param name="globalOptions">Global keys, such as <c>build_property.TargetFramework</c>.</param>
    /// <param name="fileOptions">Per-file keys, such as <c>dotnet_diagnostic.XX0001.severity</c>.</param>
    public DictionaryAnalyzerConfigOptionsProvider(
        IReadOnlyDictionary<string, string> globalOptions,
        IReadOnlyDictionary<string, string>? fileOptions = null)
    {
        _globalOptions = new Options(globalOptions, null);
        _fileOptions = new Options(globalOptions, fileOptions);
    }

    /// <inheritdoc />
    public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _fileOptions;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _fileOptions;

    private sealed class Options : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values = new(KeyComparer);

        public Options(IReadOnlyDictionary<string, string> values, IReadOnlyDictionary<string, string>? overrides)
        {
            foreach (var pair in values)
                _values[pair.Key] = pair.Value;
            foreach (var pair in overrides ?? new Dictionary<string, string>())
                _values[pair.Key] = pair.Value;
        }

        public override IEnumerable<string> Keys => _values.Keys;

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
            _values.TryGetValue(key, out value);
    }
}
