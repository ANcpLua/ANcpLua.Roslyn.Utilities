using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Abstract base class for testing Roslyn code fix providers with EditorConfig support.
/// </summary>
/// <remarks>
///     <para>
///         This class provides a fluent testing API for analyzer and code fix verification
///         that supports EditorConfig-based configuration. Use this when testing analyzers
///         that read configuration from .editorconfig files.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Supports both .NET 10 and .NET Standard 2.0 reference assemblies</description>
///         </item>
///         <item>
///             <description>Automatically configures global and project-level EditorConfig files</description>
///         </item>
///         <item>
///             <description>Supports additional source files for multi-file test scenarios</description>
///         </item>
///         <item>
///             <description>Handles line ending normalization for cross-platform compatibility</description>
///         </item>
///     </list>
/// </remarks>
/// <typeparam name="TAnalyzer">
///     The diagnostic analyzer type that produces the diagnostics to be fixed.
///     Must have a parameterless constructor.
/// </typeparam>
/// <typeparam name="TCodeFix">
///     The code fix provider type that provides fixes for the analyzer's diagnostics.
///     Must have a parameterless constructor.
/// </typeparam>
/// <example>
///     <code>
/// public class MyCodeFixTests : CodeFixTestWithEditorConfig&lt;MyAnalyzer, MyCodeFix&gt;
/// {
///     [Fact]
///     public async Task FixesIssue_WithDefaultConfig()
///     {
///         const string source = @"
///             class C { void M() { var x = [|null|]; } }
///         ";
///         const string fixedSource = @"
///             class C { void M() { string? x = null; } }
///         ";
/// 
///         await VerifyAsync(source, fixedSource);
///     }
/// 
///     [Fact]
///     public async Task FixesIssue_WithCustomEditorConfig()
///     {
///         var editorConfig = new Dictionary&lt;string, string&gt;
///         {
///             ["dotnet_diagnostic.MY001.severity"] = "warning",
///             ["my_custom_option"] = "true"
///         };
/// 
///         await VerifyAsync(source, fixedSource, editorConfig);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="DiagnosticAnalyzer" />
/// <seealso cref="CodeFixProvider" />
/// <seealso cref="CSharpCodeFixTest{TAnalyzer,TCodeFix,TVerifier}" />
public abstract class CodeFixTestWithEditorConfig<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    /// <summary>
    ///     Reference assemblies for .NET 10.0 target framework.
    /// </summary>
    private static readonly ReferenceAssemblies Net100Tfm = new("net10.0");

    /// <summary>
    ///     Reference assemblies for .NET Standard 2.0 target framework.
    /// </summary>
    private static readonly ReferenceAssemblies NetStandard20Tfm = new("netstandard2.0");

    /// <summary>
    ///     Verifies that the analyzer produces expected diagnostics and the code fix
    ///     correctly transforms the source code.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method sets up a complete test environment including:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Reference assemblies for the specified target framework</description>
    ///         </item>
    ///         <item>
    ///             <description>EditorConfig files (both global and project-level)</description>
    ///         </item>
    ///         <item>
    ///             <description>Additional source files for multi-file scenarios</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The test verifies that:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>The analyzer produces diagnostics at the marked locations in the source</description>
    ///         </item>
    ///         <item>
    ///             <description>The code fix transforms the source to match the expected fixed source</description>
    ///         </item>
    ///         <item>
    ///             <description>No additional diagnostics are produced after the fix is applied</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <param name="source">
    ///     The source code to analyze, with diagnostic markers using the standard
    ///     Roslyn testing markup syntax (e.g., <c>[|code with diagnostic|]</c>).
    /// </param>
    /// <param name="fixedSource">
    ///     The expected source code after the code fix has been applied.
    /// </param>
    /// <param name="editorConfig">
    ///     Optional dictionary of EditorConfig key-value pairs to configure the analyzer.
    ///     These are applied to both a global .globalconfig and a project-level .editorconfig file.
    ///     Values containing semicolons are automatically quoted in the .editorconfig file.
    /// </param>
    /// <param name="additionalSources">
    ///     Optional array of additional source files specified as (filename, content) tuples.
    ///     These files are included in both the test state and fixed state.
    /// </param>
    /// <param name="useNet10References">
    ///     If <see langword="true" /> (the default), uses .NET 10 reference assemblies.
    ///     If <see langword="false" />, uses .NET Standard 2.0 reference assemblies.
    /// </param>
    /// <returns>A task that completes when the verification is finished.</returns>
    /// <exception cref="Microsoft.CodeAnalysis.Testing.DiagnosticMismatchException">
    ///     Thrown when the expected diagnostics do not match the actual diagnostics.
    /// </exception>
    /// <example>
    ///     <code>
    /// // Basic usage with diagnostic markers
    /// await VerifyAsync(
    ///     source: "class C { void M() { var x = [|null|]; } }",
    ///     fixedSource: "class C { void M() { string? x = null; } }");
    /// 
    /// // With EditorConfig settings
    /// await VerifyAsync(
    ///     source: sourceCode,
    ///     fixedSource: fixedCode,
    ///     editorConfig: new Dictionary&lt;string, string&gt;
    ///     {
    ///         ["dotnet_diagnostic.MY001.severity"] = "error"
    ///     });
    /// 
    /// // With additional source files
    /// await VerifyAsync(
    ///     source: mainSource,
    ///     fixedSource: fixedMainSource,
    ///     additionalSources: [("Helper.cs", helperSource)]);
    /// 
    /// // Targeting .NET Standard 2.0
    /// await VerifyAsync(
    ///     source: sourceCode,
    ///     fixedSource: fixedCode,
    ///     useNet10References: false);
    /// </code>
    /// </example>
    protected static Task VerifyAsync(
        string source,
        string fixedSource,
        Dictionary<string, string>? editorConfig = null,
        (string FileName, string Content)[]? additionalSources = null,
        bool useNet10References = true)
    {
        var test = new CustomCodeFixTest(
            editorConfig ?? [],
            additionalSources ?? [],
            useNet10References)
        {
            TestCode = source.ReplaceLineEndings(),
            FixedCode = fixedSource.ReplaceLineEndings()
        };

        return test.RunAsync();
    }

    /// <summary>
    ///     Internal test class that customizes the code fix test with EditorConfig
    ///     and additional source file support.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class extends the standard <see cref="CSharpCodeFixTest{TAnalyzer, TCodeFix, TVerifier}" />
    ///         to provide:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Configurable reference assemblies (NET 10 or .NET Standard 2.0)</description>
    ///         </item>
    ///         <item>
    ///             <description>EditorConfig file generation from key-value pairs</description>
    ///         </item>
    ///         <item>
    ///             <description>Support for multi-file test scenarios</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private sealed class CustomCodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CustomCodeFixTest" /> class
        ///     with the specified configuration.
        /// </summary>
        /// <param name="editorConfig">
        ///     Dictionary of EditorConfig settings to apply to the test.
        /// </param>
        /// <param name="additionalSources">
        ///     Array of additional source files as (filename, content) tuples.
        /// </param>
        /// <param name="useNet10References">
        ///     Whether to use .NET 10 references (<see langword="true" />) or
        ///     .NET Standard 2.0 references (<see langword="false" />).
        /// </param>
        public CustomCodeFixTest(
            Dictionary<string, string> editorConfig,
            (string FileName, string Content)[] additionalSources,
            bool useNet10References)
        {
            ReferenceAssemblies = useNet10References ? Net100Tfm : NetStandard20Tfm;
            TestState.AdditionalReferences.AddRange(
                useNet10References ? Net100.References.All : NetStandard20.References.All);

            ApplyEditorConfig(editorConfig);
            ApplyAdditionalSources(additionalSources);
        }

        /// <summary>
        ///     Applies EditorConfig settings by creating both a global .globalconfig
        ///     and a project-level .editorconfig file.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method creates two configuration files:
        ///     </para>
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <c>/.globalconfig</c> - A global config file with <c>is_global = true</c>
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>/0/.editorconfig</c> - A project-level config file with <c>root = true</c>
        ///                 and a <c>[*.cs]</c> section
        ///             </description>
        ///         </item>
        ///     </list>
        ///     <para>
        ///         Values containing semicolons are automatically quoted in the .editorconfig
        ///         file to handle multi-value settings correctly.
        ///     </para>
        /// </remarks>
        /// <param name="editorConfig">The EditorConfig settings to apply.</param>
        private void ApplyEditorConfig(Dictionary<string, string> editorConfig)
        {
            if (editorConfig.Count == 0) return;

            var globalLines = new List<string> { "is_global = true", "" };
            foreach (var kvp in editorConfig)
                globalLines.Add($"{kvp.Key} = {kvp.Value}");

            TestState.AnalyzerConfigFiles.Add(("/.globalconfig", string.Join("\n", globalLines)));

            var editorConfigLines = new List<string> { "root = true", "", "[*.cs]" };
            foreach (var kvp in editorConfig)
            {
                var value = kvp.Value.Contains(';') ? $"\"{kvp.Value}\"" : kvp.Value;
                editorConfigLines.Add($"{kvp.Key} = {value}");
            }

            TestState.AnalyzerConfigFiles.Add(("/0/.editorconfig", string.Join("\n", editorConfigLines)));
        }

        /// <summary>
        ///     Applies additional source files to both the test state and fixed state.
        /// </summary>
        /// <remarks>
        ///     Additional source files are added to both states to ensure they are
        ///     available during both the initial analysis and the verification of the fix.
        /// </remarks>
        /// <param name="additionalSources">
        ///     Array of source files as (filename, content) tuples to add.
        /// </param>
        private void ApplyAdditionalSources((string FileName, string Content)[] additionalSources)
        {
            foreach (var (fileName, content) in additionalSources)
            {
                TestState.Sources.Add((fileName, content));
                FixedState.Sources.Add((fileName, content));
            }
        }
    }
}