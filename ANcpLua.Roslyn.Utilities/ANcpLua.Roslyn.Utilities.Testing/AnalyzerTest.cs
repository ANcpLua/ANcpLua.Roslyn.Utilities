using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides a base class for unit testing Roslyn diagnostic analyzers with pre-configured
///     reference assemblies for .NET 10 and .NET Standard 2.0 target frameworks.
/// </summary>
/// <typeparam name="TAnalyzer">
///     The type of the <see cref="DiagnosticAnalyzer" /> to test.
///     Must have a parameterless constructor.
/// </typeparam>
/// <remarks>
///     <para>
///         This class simplifies analyzer testing by providing:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Pre-configured reference assemblies for common target frameworks</description>
///         </item>
///         <item>
///             <description>Automatic line ending normalization for cross-platform compatibility</description>
///         </item>
///         <item>
///             <description>Integration with Microsoft.CodeAnalysis.Testing infrastructure</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <para>
///         Create a test class that inherits from <see cref="AnalyzerTest{TAnalyzer}" />:
///     </para>
///     <code>
/// public class MyAnalyzerTests : AnalyzerTest&lt;MyAnalyzer&gt;
/// {
///     [Fact]
///     public async Task ReportsWarning_ForInvalidCode()
///     {
///         var source = """
///             class Test
///             {
///                 void Method() { /* invalid code */ }
///             }
///             """;
/// 
///         await VerifyAsync(source);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="DiagnosticAnalyzer" />
/// <seealso cref="CSharpAnalyzerTest{TAnalyzer,TVerifier}" />
public abstract class AnalyzerTest<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
{

    /// <summary>
    ///     Verifies that the analyzer produces the expected diagnostics for the given source code.
    /// </summary>
    /// <param name="source">
    ///     The C# source code to analyze. Line endings are automatically normalized
    ///     for cross-platform compatibility.
    /// </param>
    /// <param name="useNet10References">
    ///     <see langword="true" /> to use .NET 10 reference assemblies (default);
    ///     <see langword="false" /> to use .NET Standard 2.0 reference assemblies.
    ///     Use <see langword="false" /> when testing analyzers that target netstandard2.0,
    ///     such as source generator analyzers.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> that completes when the verification is finished.
    ///     The task will throw if any expected diagnostics are missing or if
    ///     unexpected diagnostics are reported.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method performs the following steps:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Normalizes line endings in the source code using <see cref="string.ReplaceLineEndings()" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Configures the appropriate reference assemblies based on the
    ///                 <paramref name="useNet10References" /> parameter
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Runs the analyzer and compares results against expected diagnostics marked in the source</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         To specify expected diagnostics in the source code, use the standard diagnostic markup
    ///         format supported by Microsoft.CodeAnalysis.Testing. For example:
    ///     </para>
    ///     <code>
    /// var source = """
    ///     class Test
    ///     {
    ///         void Method({|DiagnosticId:problematicCode|}) { }
    ///     }
    ///     """;
    /// </code>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Test an analyzer with .NET 10 references (default):
    ///     </para>
    ///     <code>
    /// [Fact]
    /// public async Task Analyzer_ReportsWarning()
    /// {
    ///     var source = """
    ///         class Test
    ///         {
    ///             void {|MY001:BadMethod|}() { }
    ///         }
    ///         """;
    /// 
    ///     await VerifyAsync(source);
    /// }
    /// </code>
    ///     <para>
    ///         Test an analyzer with .NET Standard 2.0 references:
    ///     </para>
    ///     <code>
    /// [Fact]
    /// public async Task Analyzer_WorksWithNetStandard20()
    /// {
    ///     var source = "class Test { }";
    ///     await VerifyAsync(source, useNet10References: false);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="CSharpAnalyzerTest{TAnalyzer, TVerifier}" />
    protected static Task VerifyAsync(string source, bool useNet10References = true)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source.ReplaceLineEndings(),
            ReferenceAssemblies = useNet10References ? TestConfiguration.Net100Tfm : TestConfiguration.NetStandard20Tfm
        };

        test.TestState.AdditionalReferences.AddRange(
            useNet10References ? Net100.References.All : NetStandard20.References.All);

        return test.RunAsync();
    }

    /// <summary>
    ///     Verifies that the analyzer produces the expected diagnostics for the given source code
    ///     with additional files included in the compilation.
    /// </summary>
    /// <param name="source">
    ///     The C# source code to analyze. Line endings are automatically normalized
    ///     for cross-platform compatibility.
    /// </param>
    /// <param name="additionalFiles">
    ///     A collection of additional files to include in the compilation.
    ///     Each tuple contains the file name and its content.
    /// </param>
    /// <param name="expectedDiagnostics">
    ///     Optional collection of expected diagnostics. If null, diagnostics are expected
    ///     to be marked in the source code using the standard markup format.
    /// </param>
    /// <param name="useNet10References">
    ///     <see langword="true" /> to use .NET 10 reference assemblies (default);
    ///     <see langword="false" /> to use .NET Standard 2.0 reference assemblies.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> that completes when the verification is finished.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This overload is useful for testing analyzers that inspect additional files
    ///         such as <c>Directory.Build.props</c>, <c>Directory.Packages.props</c>, or
    ///         other MSBuild files.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Test an analyzer that inspects Directory.Build.props:
    ///     </para>
    ///     <code>
    /// [Fact]
    /// public async Task Analyzer_ReportsWhenVersionPropsNotImported()
    /// {
    ///     const string source = "public class C { }";
    ///     const string directoryBuildProps = """
    ///         &lt;Project&gt;
    ///             &lt;PropertyGroup&gt;
    ///                 &lt;SomeProperty&gt;Value&lt;/SomeProperty&gt;
    ///             &lt;/PropertyGroup&gt;
    ///         &lt;/Project&gt;
    ///         """;
    /// 
    ///     var expected = new DiagnosticResult("AL0018", DiagnosticSeverity.Warning)
    ///         .WithLocation("Directory.Build.props", 1, 1);
    /// 
    ///     await VerifyAsync(
    ///         source,
    ///         [("Directory.Build.props", directoryBuildProps)],
    ///         [expected]);
    /// }
    /// </code>
    /// </example>
    protected static Task VerifyAsync(
        string source,
        IEnumerable<(string fileName, string content)> additionalFiles,
        IEnumerable<DiagnosticResult>? expectedDiagnostics = null,
        bool useNet10References = true)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source.ReplaceLineEndings(),
            ReferenceAssemblies = useNet10References ? TestConfiguration.Net100Tfm : TestConfiguration.NetStandard20Tfm
        };

        foreach (var (fileName, content) in additionalFiles)
            test.TestState.AdditionalFiles.Add((fileName, content.ReplaceLineEndings()));

        if (expectedDiagnostics is not null)
            test.ExpectedDiagnostics.AddRange(expectedDiagnostics);

        test.TestState.AdditionalReferences.AddRange(
            useNet10References ? Net100.References.All : NetStandard20.References.All);

        return test.RunAsync();
    }
}
