using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Primary public API for testing Roslyn <see cref="IIncrementalGenerator" /> implementations.
/// </summary>
/// <remarks>
///     <para>
///         This class provides a fluent, string-extension based API for testing incremental generators.
///         It is designed for table-driven tests and fast authoring of generator validation.
///     </para>
///     <para>
///         <b>API Styles:</b>
///         <list type="bullet">
///             <item>
///                 <description>
///                     <b>String extensions (recommended for simple tests):</b> Use the extension methods
///                     like <see cref="ShouldGenerate{TGenerator}(string,string,string)" /> directly on source strings.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Instance-based (for shared configuration):</b> Use <see cref="GeneratorTester{TGenerator}" />
///                     or derive from <see cref="GeneratorTestBase{TGenerator}" /> to avoid repeating the
///                     generator type parameter.
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Design Principles:</b>
///         <list type="bullet">
///             <item>
///                 <description>
///                     <b>Deterministic inputs:</b> All helpers create a fresh <see cref="Compilation" /> per run
///                     to accurately model IDE/compiler behavior. Generated trees are never fed back into subsequent runs.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>High-signal assertions:</b> Failures render clear, actionable messages via custom
///                     formatters (see <see cref="TestFormatters" />) and FluentAssertions' improved patterns.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Caching validation:</b> <see cref="ShouldCache{TGenerator}(string,string[])" /> inspects
///                     tracked steps; <see cref="ShouldCacheWithCompilationUpdate{TGenerator}" /> validates
///                     identity-based caching of generated syntax trees across controlled edits.
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
/// <example>
///     Basic generation test:
///     <code>
/// [Fact]
/// public async Task Generates_builder_for_class()
/// {
///     await """
///         [GenerateBuilder]
///         public class Person { public string Name { get; set; } }
///     """.ShouldGenerate&lt;BuilderGenerator&gt;("Person.Builder.g.cs", "public class PersonBuilder");
/// }
/// </code>
///     Diagnostic test:
///     <code>
/// [Fact]
/// public async Task Reports_error_for_invalid_class()
/// {
///     await "public class NoAttribute { }"
///         .ShouldHaveDiagnostic&lt;MyGenerator&gt;("GEN001", DiagnosticSeverity.Error);
/// }
/// </code>
///     Caching test:
///     <code>
/// [Fact]
/// public async Task Caches_pipeline_correctly()
/// {
///     await "public class Person { }".ShouldCache&lt;MyGenerator&gt;("TransformStep");
/// }
/// </code>
/// </example>
public static class GeneratorTest
{
    /// <summary>
    ///     Verifies that running <typeparamref name="TGenerator" /> on the source produces a file
    ///     with the specified hint name and content.
    /// </summary>
    /// <typeparam name="TGenerator">
    ///     The generator under test. Must implement <see cref="IIncrementalGenerator" />
    ///     and have a parameterless constructor.
    /// </typeparam>
    /// <param name="hintName">
    ///     The expected generated hint name (e.g., <c>Person.Builder.g.cs</c>).
    /// </param>
    /// <param name="expectedContent">
    ///     The expected file content. Exact match is enforced by default.
    ///     Use <see cref="GeneratorTestExtensions.ShouldGenerate{TGenerator}(string,string,string,bool,bool)" />
    ///     for advanced control.
    /// </param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <remarks>
    ///     <para>
    ///         This is a convenience overload that calls
    ///         <see cref="GeneratorTestExtensions.ShouldGenerate{TGenerator}(string,string,string,bool,bool)" />
    ///         with <c>exactMatch: true</c>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// await """
    ///     [GenerateBuilder]
    ///     public class Person { string Name { get; set; } }
    /// """.ShouldGenerate&lt;MyGenerator&gt;("Person.Builder.g.cs", "public class PersonBuilder");
    /// </code>
    /// </example>
    /// <seealso cref="GeneratorTestExtensions.ShouldGenerate{TGenerator}(string,string,string,bool,bool)" />
    public static Task ShouldGenerate<TGenerator>(this string source, string hintName, string expectedContent)
        where TGenerator : IIncrementalGenerator, new()
    {
        return source.ShouldGenerate<TGenerator>(hintName, expectedContent, true);
    }

    /// <summary>
    ///     Asserts that <typeparamref name="TGenerator" /> produces a diagnostic with the given
    ///     ID and severity.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="diagnosticId">Expected diagnostic ID (e.g., <c>GEN001</c>).</param>
    /// <param name="severity">
    ///     Expected diagnostic severity. Defaults to <see cref="JSType.Error" />.
    /// </param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <example>
    ///     <code>
    /// await "public class Invalid { }"
    ///     .ShouldHaveDiagnostic&lt;MyGenerator&gt;("GEN001", DiagnosticSeverity.Warning);
    /// </code>
    /// </example>
    /// <seealso cref="ShouldProduceDiagnostic{TGenerator}(string,string,DiagnosticSeverity,string)" />
    public static Task ShouldHaveDiagnostic<TGenerator>(this string source, string diagnosticId,
        DiagnosticSeverity severity = DiagnosticSeverity.Error) where TGenerator : IIncrementalGenerator, new()
    {
        DiagnosticResult expected = new(diagnosticId, severity);
        return source.ShouldHaveDiagnostics<TGenerator>(expected);
    }

    /// <summary>
    ///     Asserts that a diagnostic with the given ID and severity is produced and that its message
    ///     contains the specified text.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="diagnosticId">Diagnostic ID.</param>
    /// <param name="severity">Diagnostic severity.</param>
    /// <param name="messageContains">A substring that must appear in the diagnostic message.</param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <example>
    ///     <code>
    /// await "public class Invalid { }"
    ///     .ShouldProduceDiagnostic&lt;MyGenerator&gt;(
    ///         "GEN002",
    ///         DiagnosticSeverity.Warning,
    ///         "Missing required attribute");
    /// </code>
    /// </example>
    public static Task ShouldProduceDiagnostic<TGenerator>(this string source, string diagnosticId,
        DiagnosticSeverity severity, string messageContains) where TGenerator : IIncrementalGenerator, new()
    {
        var expected = new DiagnosticResult(diagnosticId, severity).WithMessage(messageContains);
        return source.ShouldHaveDiagnostics<TGenerator>([expected]);
    }

    /// <summary>
    ///     Asserts that <typeparamref name="TGenerator" /> does NOT produce a diagnostic with the given ID.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="diagnosticId">The diagnostic ID that should NOT be present.</param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <example>
    ///     <code>
    /// await "[ValidAttribute] public class Valid { }"
    ///     .ShouldNotHaveDiagnostic&lt;MyGenerator&gt;("GEN001");
    /// </code>
    /// </example>
    public static async Task ShouldNotHaveDiagnostic<TGenerator>(this string source, string diagnosticId)
        where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();
        var (firstRun, _) = await engine.ExecuteTwiceAsync(source, false);

        using AssertionScope scope = new("Diagnostics");
        TestFormatters.ApplyToScope(scope);

        var diagnostics = firstRun.Results.SelectMany(r => r.Diagnostics).ToList();
        var found = diagnostics.Any(d => d.Id == diagnosticId);
        found.Should().BeFalse($"Expected no diagnostic with ID '{diagnosticId}', but found one");
    }

    /// <summary>
    ///     Verifies that observable pipeline steps of <typeparamref name="TGenerator" />
    ///     are cached across two identical runs.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="trackingNames">
    ///     Optional explicit step names to validate. When omitted, steps are auto-discovered
    ///     (excluding infrastructure sinks).
    /// </param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <remarks>
    ///     <para>
    ///         Uses Roslyn's tracked step instrumentation; asserts no forbidden Roslyn runtime types
    ///         are cached (e.g., <see cref="ISymbol" />).
    ///     </para>
    /// </remarks>
    /// <seealso
    ///     cref="ShouldCacheWithCompilationUpdate{TGenerator}(string,System.Func{Microsoft.CodeAnalysis.Compilation,Microsoft.CodeAnalysis.Compilation}(Microsoft.CodeAnalysis.Compilation),Action{CompilationCacheResult}?)" />
    /// />
    public static Task ShouldCache<TGenerator>(this string source, params string[] trackingNames)
        where TGenerator : IIncrementalGenerator, new()
    {
        return source.ShouldBeCached<TGenerator>(trackingNames);
    }

    /// <summary>
    ///     Asserts that <typeparamref name="TGenerator" /> produces no error-level diagnostics.
    ///     Info and warning diagnostics are ignored.
    /// </summary>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <remarks>
    ///     This is useful for generators that intentionally emit informational or warning
    ///     diagnostics. If you need to assert zero diagnostics of any severity,
    ///     use <see cref="GeneratorTestExtensions.ShouldHaveNoDiagnostics{TGenerator}(string)" />.
    /// </remarks>
    public static async Task ShouldCompile<TGenerator>(this string source)
        where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();
        var (firstRun, _) = await engine.ExecuteTwiceAsync(source, false);

        using AssertionScope scope = new("Compilation");
        TestFormatters.ApplyToScope(scope);

        var errors = firstRun.Results
            .SelectMany(r => r.Diagnostics)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty("Expected no error diagnostics, but found: {0}",
            string.Join(", ", errors.Select(d => $"{d.Id}: {d.GetMessage()}")));
    }

    /// <summary>
    ///     Validates caching and behavior across a controlled <see cref="Compilation" /> change
    ///     that simulates an IDE edit.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="makeChange">
    ///     A pure function that returns a modified compilation representing the edit.
    ///     The input is the original compilation (not the generated one).
    /// </param>
    /// <param name="validate">
    ///     Optional custom validations against the resulting <see cref="CompilationCacheResult" />.
    /// </param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when all validations finish.</returns>
    /// <remarks>
    ///     <para>
    ///         This method runs the generator with
    ///         <see
    ///             cref="GeneratorDriver.RunGeneratorsAndUpdateCompilation(Compilation,out Compilation,out System.Collections.Immutable.ImmutableArray{Diagnostic},System.Threading.CancellationToken)" />
    ///         ,
    ///         applies <paramref name="makeChange" /> to the original input compilation,
    ///         runs again, and exposes a <see cref="CompilationCacheResult" /> for fine-grained checks.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     Add a new file:
    ///     <code>
    /// await "public class Person { }"
    ///     .ShouldCacheWithCompilationUpdate&lt;MyGenerator&gt;(
    ///         comp => comp.AddSyntaxTrees(CSharpSyntaxTree.ParseText("public class Address { }")),
    ///         result => result.ShouldHaveCached("Person.Builder.g.cs"));
    /// </code>
    /// </example>
    /// <seealso cref="ShouldRegenerate{TGenerator}(string,string)" />
    /// <seealso cref="ShouldNotRegenerate{TGenerator}(string,string)" />
    public static async Task ShouldCacheWithCompilationUpdate<TGenerator>(this string source,
        Func<Compilation, Compilation> makeChange,
        Action<CompilationCacheResult>? validate = null)
        where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorTestEngine<TGenerator> engine = new();

        var compilation1 = await engine.CreateCompilationAsync(source);
        var driver = GeneratorDriverFactory.CreateDriver<TGenerator>(true);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation1, out var output1,
            out var diagnostics1);

        // Apply the controlled edit to the original input
        var compilation2 = makeChange(compilation1);

        // Run #2
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation2, out var output2,
            out var diagnostics2);

        CompilationCacheResult result = new(output1, output2, diagnostics1, diagnostics2, driver.GetRunResult());

        // Default validations
        result.ValidateCaching();

        // Custom validations
        validate?.Invoke(result);
    }

    /// <summary>
    ///     Convenience overload that simulates replacing the first syntax tree with edited source.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="editedSource">Edited source for the first document.</param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when validations finish.</returns>
    /// <example>
    ///     <code>
    /// await "public class Person { }"
    ///     .ShouldRegenerate&lt;MyGenerator&gt;("""
    ///         public class Person
    ///         {
    ///             public string Name { get; set; } // Added property
    ///         }
    ///     """);
    /// </code>
    /// </example>
    public static Task ShouldRegenerate<TGenerator>(this string source, string editedSource)
        where TGenerator : IIncrementalGenerator, new()
    {
        return source.ShouldCacheWithCompilationUpdate<TGenerator>(compilation =>
        {
            var parseOptions = new CSharpParseOptions(TestConfiguration.LanguageVersion);
            var tree = CSharpSyntaxTree.ParseText(editedSource, parseOptions);
            return compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), tree);
        });
    }

    /// <summary>
    ///     Convenience overload that simulates adding a new document to the project.
    /// </summary>
    /// <typeparam name="TGenerator">The generator under test.</typeparam>
    /// <param name="newFileContent">Content of the additional C# file to add.</param>
    /// <param name="source">C# source code to compile and feed into the generator.</param>
    /// <returns>A task that completes when validations finish.</returns>
    /// <remarks>
    ///     This is useful for testing that adding an unrelated file doesn't cause
    ///     unnecessary regeneration of existing outputs.
    /// </remarks>
    /// <example>
    ///     <code>
    /// await "public class Person { }"
    ///     .ShouldNotRegenerate&lt;MyGenerator&gt;("public class UnrelatedClass { }");
    /// </code>
    /// </example>
    public static Task ShouldNotRegenerate<TGenerator>(this string source, string newFileContent)
        where TGenerator : IIncrementalGenerator, new()
    {
        var parseOptions = new CSharpParseOptions(TestConfiguration.LanguageVersion);
        return source.ShouldCacheWithCompilationUpdate<TGenerator>(compilation =>
            compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(newFileContent, parseOptions)));
    }
}