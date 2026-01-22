using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Base class for code refactoring unit tests that provides a fluent, pre-configured testing infrastructure
///     for validating Roslyn code refactoring providers.
/// </summary>
/// <typeparam name="TRefactoring">
///     The code refactoring provider type to test.
///     Must have a parameterless constructor.
/// </typeparam>
/// <remarks>
///     <para>
///         This class provides a streamlined approach for testing code refactoring providers by:
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
///         that your code refactoring transforms source code as expected.
///     </para>
/// </remarks>
/// <example>
///     <para>
///         Basic usage in a test class:
///     </para>
///     <code>
/// public class MyRefactoringTests : RefactoringTest&lt;MyRefactoring&gt;
/// {
///     [Fact]
///     public async Task RefactorsCorrectly()
///     {
///         const string source = @"
///             class C
///             {
///                 void M()
///                 {
///                     var x = [|value|];
///                 }
///             }";
///
///         const string fixedSource = @"
///             class C
///             {
///                 void M()
///                 {
///                     var x = refactoredValue;
///                 }
///             }";
///
///         await VerifyAsync(source, fixedSource);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="AnalyzerTest{TAnalyzer}" />
/// <seealso cref="CodeFixTest{TAnalyzer,TCodeFix}" />
/// <seealso cref="CodeRefactoringProvider" />
public abstract class RefactoringTest<TRefactoring>
    where TRefactoring : CodeRefactoringProvider, new()
{
    /// <summary>
    ///     Verifies that the code refactoring transforms the source code as expected.
    /// </summary>
    /// <param name="source">
    ///     The source code containing refactoring span markers. Use the format <c>[|code|]</c>
    ///     to mark the span where the refactoring should be triggered.
    /// </param>
    /// <param name="fixedSource">
    ///     The expected source code after the refactoring has been applied.
    ///     This should contain the transformed code without span markers.
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
    ///     The task completes successfully if the refactoring produces the expected transformation,
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
    ///             <description>Triggers the refactoring at the marked span location</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Applies the refactoring and verifies the result matches <paramref name="fixedSource" />
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Testing a refactoring that adds a static modifier to a lambda:
    ///     </para>
    ///     <code>
    /// [Fact]
    /// public async Task AddsStaticToLambda()
    /// {
    ///     const string source = @"
    ///         using System;
    ///         public class Example
    ///         {
    ///             Func&lt;int, int&gt; f = [|x => x * 2|];
    ///         }";
    /// 
    ///     const string fixedSource = @"
    ///         using System;
    ///         public class Example
    ///         {
    ///             Func&lt;int, int&gt; f = static x => x * 2;
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
    /// <seealso cref="CSharpCodeRefactoringTest{TRefactoring,TVerifier}" />
    protected static Task VerifyAsync(string source, string fixedSource, bool useNet10References = true)
    {
        var test = new CSharpCodeRefactoringTest<TRefactoring, DefaultVerifier>
        {
            TestCode = source.ReplaceLineEndings(),
            FixedCode = fixedSource.ReplaceLineEndings(),
            ReferenceAssemblies = useNet10References ? TestConfiguration.Net100Tfm : TestConfiguration.NetStandard20Tfm
        };

        test.TestState.AdditionalReferences.AddRange(
            useNet10References ? Net100.References.All : NetStandard20.References.All);

        return test.RunAsync();
    }

    /// <summary>
    ///     Verifies that no refactoring is offered for the given source code.
    /// </summary>
    /// <param name="source">
    ///     The source code containing refactoring span markers. Use the format <c>[|code|]</c>
    ///     to mark the span where no refactoring should be triggered.
    /// </param>
    /// <param name="useNet10References">
    ///     <see langword="true" /> to use .NET 10 reference assemblies (default);
    ///     <see langword="false" /> to use .NET Standard 2.0 reference assemblies.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous verification operation.
    ///     The task completes successfully if no refactoring is offered.
    /// </returns>
    /// <example>
    ///     <para>
    ///         Testing that a refactoring is not offered when already applied:
    ///     </para>
    ///     <code>
    /// [Fact]
    /// public async Task DoesNotOfferWhenAlreadyStatic()
    /// {
    ///     const string source = @"
    ///         using System;
    ///         public class Example
    ///         {
    ///             Func&lt;int, int&gt; f = [|static x => x * 2|];
    ///         }";
    /// 
    ///     await VerifyNoRefactoringAsync(source);
    /// }
    /// </code>
    /// </example>
    protected static Task VerifyNoRefactoringAsync(string source, bool useNet10References = true)
    {
        var test = new CSharpCodeRefactoringTest<TRefactoring, DefaultVerifier>
        {
            TestCode = source.ReplaceLineEndings(),
            FixedCode = source.ReplaceLineEndings(),
            ReferenceAssemblies = useNet10References ? TestConfiguration.Net100Tfm : TestConfiguration.NetStandard20Tfm
        };

        test.TestState.AdditionalReferences.AddRange(
            useNet10References ? Net100.References.All : NetStandard20.References.All);

        return test.RunAsync();
    }
}
