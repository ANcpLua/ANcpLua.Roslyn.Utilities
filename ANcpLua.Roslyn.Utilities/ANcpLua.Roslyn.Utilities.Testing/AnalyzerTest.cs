using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
/// Base class for analyzer unit tests.
/// Provides pre-configured reference assemblies for .NET 10 and .NET Standard 2.0.
/// </summary>
/// <typeparam name="TAnalyzer">The analyzer type to test.</typeparam>
public abstract class AnalyzerTest<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new() {
    private static readonly ReferenceAssemblies Net100Tfm = new("net10.0");
    private static readonly ReferenceAssemblies NetStandard20Tfm = new("netstandard2.0");

    /// <summary>
    /// Verifies that the analyzer produces expected diagnostics for the given source.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="useNet10References">
    /// If true (default), uses .NET 10 reference assemblies.
    /// If false, uses .NET Standard 2.0 reference assemblies.
    /// </param>
    protected static Task VerifyAsync(string source, bool useNet10References = true) {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> {
            TestCode = source.ReplaceLineEndings(),
            ReferenceAssemblies = useNet10References ? Net100Tfm : NetStandard20Tfm
        };

        test.TestState.AdditionalReferences.AddRange(
            useNet10References ? Net100.References.All : NetStandard20.References.All);

        return test.RunAsync();
    }
}
