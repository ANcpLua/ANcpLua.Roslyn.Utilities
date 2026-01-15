using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Base class for code fix unit tests that provides a fluent, pre-configured testing infrastructure
///     for validating Roslyn code fix providers.
/// </summary>
/// <typeparam name="TAnalyzer">
///     The diagnostic analyzer type that produces diagnostics to be fixed.
///     Must have a parameterless constructor.
/// </typeparam>
/// <typeparam name="TCodeFix">
///     The code fix provider type that offers fixes for diagnostics produced by <typeparamref name="TAnalyzer" />.
///     Must have a parameterless constructor.
/// </typeparam>
/// <remarks>
///     <para>
///         This class provides a streamlined approach for testing code fix providers by:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Pre-configuring reference assemblies for .NET 10 or .NET Standard 2.0</description>
///         </item>
///         <item>
///             <description>Normalizing line endings to ensure cross-platform test consistency</description>
///         </item>
///         <item>
///             <description>Integrating with the Microsoft.CodeAnalysis.Testing framework</description>
///         </item>
///     </list>
///     <para>
///         Inherit from this class and call <see cref="VerifyAsync" /> in your test methods to validate
///         that your code fix transforms source code as expected.
///     </para>
/// </remarks>
/// <example>
///     <para>
///         Basic usage in a test class:
///     </para>
///     <code>
/// public class MyCodeFixTests : CodeFixTest&lt;MyAnalyzer, MyCodeFix&gt;
/// {
///     [Fact]
///     public async Task FixesIssue()
///     {
///         const string source = @"
///             class C
///             {
///                 void M()
///                 {
///                     var x = {|MY001:problematic_code|};
///                 }
///             }";
/// 
///         const string fixedSource = @"
///             class C
///             {
///                 void M()
///                 {
///                     var x = correct_code;
///                 }
///             }";
/// 
///         await VerifyAsync(source, fixedSource);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="AnalyzerTest{TAnalyzer}" />
/// <seealso cref="CodeFixTestWithEditorConfig{TAnalyzer,TCodeFix}" />
/// <seealso cref="DiagnosticAnalyzer" />
/// <seealso cref="CodeFixProvider" />
public abstract class CodeFixTest<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    /// <summary>
    ///     Reference assemblies configuration for .NET 10.0 target framework.
    /// </summary>
    private static readonly ReferenceAssemblies Net100Tfm = new("net10.0");

    /// <summary>
    ///     Reference assemblies configuration for .NET Standard 2.0 target framework.
    /// </summary>
    private static readonly ReferenceAssemblies NetStandard20Tfm = new("netstandard2.0");

    /// <summary>
    ///     Verifies that the code fix transforms the source code as expected.
    /// </summary>
    /// <param name="source">
    ///     The source code containing diagnostic markers. Use the format <c>{|DIAGNOSTIC_ID:code|}</c>
    ///     to mark locations where diagnostics are expected. For example: <c>{|MY001:badCode|}</c>.
    /// </param>
    /// <param name="fixedSource">
    ///     The expected source code after the code fix has been applied.
    ///     This should contain the corrected code without diagnostic markers.
    /// </param>
    /// <param name="useNet10References">
    ///     <para>
    ///         Specifies which target framework's reference assemblies to use:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description><c>true</c> (default): Uses .NET 10 reference assemblies with full modern API surface</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>false</c>: Uses .NET Standard 2.0 reference assemblies for testing cross-platform
    ///                 compatibility
    ///             </description>
    ///         </item>
    ///     </list>
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous verification operation.
    ///     The task completes successfully if the code fix produces the expected transformation,
    ///     or throws an assertion exception if the transformation does not match.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method performs the following operations:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Normalizes line endings in both source and fixed source for cross-platform consistency</description>
    ///         </item>
    ///         <item>
    ///             <description>Creates a compilation with the appropriate reference assemblies</description>
    ///         </item>
    ///         <item>
    ///             <description>Runs the analyzer to produce diagnostics at marked locations</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Applies the code fix and verifies the result matches <paramref name="fixedSource" />
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Testing a code fix that renames a method:
    ///     </para>
    ///     <code>
    /// [Fact]
    /// public async Task RenamesMethodCorrectly()
    /// {
    ///     const string source = @"
    ///         public class Example
    ///         {
    ///             public void {|NAMING001:badName|}() { }
    ///         }";
    /// 
    ///     const string fixedSource = @"
    ///         public class Example
    ///         {
    ///             public void GoodName() { }
    ///         }";
    /// 
    ///     await VerifyAsync(source, fixedSource);
    /// }
    /// </code>
    /// </example>
    /// <example>
    ///     <para>
    ///         Testing with .NET Standard 2.0 references:
    ///     </para>
    ///     <code>
    /// [Fact]
    /// public async Task WorksOnNetStandard20()
    /// {
    ///     await VerifyAsync(source, fixedSource, useNet10References: false);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="CSharpCodeFixTest{TAnalyzer,TCodeFix,TVerifier}" />
    protected static Task VerifyAsync(string source, string fixedSource, bool useNet10References = true)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = source.ReplaceLineEndings(),
            FixedCode = fixedSource.ReplaceLineEndings(),
            ReferenceAssemblies = useNet10References ? Net100Tfm : NetStandard20Tfm
        };

        test.TestState.AdditionalReferences.AddRange(
            useNet10References ? Net100.References.All : NetStandard20.References.All);

        return test.RunAsync();
    }
}