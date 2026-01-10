using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
/// Base class for code fix unit tests.
/// Provides pre-configured reference assemblies for .NET 10.
/// </summary>
/// <typeparam name="TAnalyzer">The analyzer type that produces the diagnostics.</typeparam>
/// <typeparam name="TCodeFix">The code fix provider to test.</typeparam>
public abstract class CodeFixTest<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new() {
    private static readonly ReferenceAssemblies Net100Tfm = new("net10.0");
    private static readonly ReferenceAssemblies NetStandard20Tfm = new("netstandard2.0");

    /// <summary>
    /// Verifies that the code fix transforms the source as expected.
    /// </summary>
    /// <param name="source">The source code with diagnostic markers.</param>
    /// <param name="fixedSource">The expected source after the fix is applied.</param>
    /// <param name="useNet10References">
    /// If true (default), uses .NET 10 reference assemblies.
    /// If false, uses .NET Standard 2.0 reference assemblies.
    /// </param>
    protected static Task VerifyAsync(string source, string fixedSource, bool useNet10References = true) {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> {
            TestCode = source.ReplaceLineEndings(),
            FixedCode = fixedSource.ReplaceLineEndings(),
            ReferenceAssemblies = useNet10References ? Net100Tfm : NetStandard20Tfm
        };

        test.TestState.AdditionalReferences.AddRange(
            useNet10References ? Net100.References.All : NetStandard20.References.All);

        return test.RunAsync();
    }
}
