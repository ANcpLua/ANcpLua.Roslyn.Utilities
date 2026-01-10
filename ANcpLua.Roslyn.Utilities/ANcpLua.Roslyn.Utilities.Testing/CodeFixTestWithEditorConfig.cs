using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
/// Enhanced code fix test base class that supports EditorConfig configuration.
/// Use this when testing analyzers that read configuration from .editorconfig.
/// </summary>
/// <typeparam name="TAnalyzer">The analyzer type that produces the diagnostics.</typeparam>
/// <typeparam name="TCodeFix">The code fix provider to test.</typeparam>
public abstract class CodeFixTestWithEditorConfig<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new() {

    private static readonly ReferenceAssemblies Net100Tfm = new("net10.0");
    private static readonly ReferenceAssemblies NetStandard20Tfm = new("netstandard2.0");

    /// <summary>
    /// Verifies analyzer and code fix behavior with optional EditorConfig settings.
    /// </summary>
    /// <param name="source">Source code with diagnostic markers.</param>
    /// <param name="fixedSource">Expected source after fix is applied.</param>
    /// <param name="editorConfig">Optional EditorConfig key-value pairs.</param>
    /// <param name="additionalSources">Optional additional source files as (filename, content) tuples.</param>
    /// <param name="useNet10References">
    /// If true (default), uses .NET 10 references.
    /// If false, uses .NET Standard 2.0 references.
    /// </param>
    protected static Task VerifyAsync(
        string source,
        string fixedSource,
        Dictionary<string, string>? editorConfig = null,
        (string FileName, string Content)[]? additionalSources = null,
        bool useNet10References = true) {
        var test = new CustomCodeFixTest(
            editorConfig ?? [],
            additionalSources ?? [],
            useNet10References) {
            TestCode = source.ReplaceLineEndings(),
            FixedCode = fixedSource.ReplaceLineEndings()
        };

        return test.RunAsync();
    }

    private sealed class CustomCodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> {
        public CustomCodeFixTest(
            Dictionary<string, string> editorConfig,
            (string FileName, string Content)[] additionalSources,
            bool useNet10References) {
            ReferenceAssemblies = useNet10References ? Net100Tfm : NetStandard20Tfm;
            TestState.AdditionalReferences.AddRange(
                useNet10References ? Net100.References.All : NetStandard20.References.All);

            ApplyEditorConfig(editorConfig);
            ApplyAdditionalSources(additionalSources);
        }

        private void ApplyEditorConfig(Dictionary<string, string> editorConfig) {
            if (editorConfig.Count == 0) return;

            var globalLines = new List<string> { "is_global = true", "" };
            foreach (var kvp in editorConfig)
                globalLines.Add($"{kvp.Key} = {kvp.Value}");

            TestState.AnalyzerConfigFiles.Add(("/.globalconfig", string.Join("\n", globalLines)));

            var editorConfigLines = new List<string> { "root = true", "", "[*.cs]" };
            foreach (var kvp in editorConfig) {
                var value = kvp.Value.Contains(';') ? $"\"{kvp.Value}\"" : kvp.Value;
                editorConfigLines.Add($"{kvp.Key} = {value}");
            }

            TestState.AnalyzerConfigFiles.Add(("/0/.editorconfig", string.Join("\n", editorConfigLines)));
        }

        private void ApplyAdditionalSources((string FileName, string Content)[] additionalSources) {
            foreach (var (fileName, content) in additionalSources) {
                TestState.Sources.Add((fileName, content));
                FixedState.Sources.Add((fileName, content));
            }
        }
    }
}
