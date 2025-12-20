using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
/// Instance-based, typed test facade that eliminates the need to repeat the generator type
/// in each test method.
/// </summary>
/// <typeparam name="TGenerator">
/// The generator under test. Must implement <see cref="IIncrementalGenerator"/>
/// and have a parameterless constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// This class provides the same functionality as <see cref="GeneratorTest"/> and
/// <see cref="GeneratorTestExtensions"/> but in an instance-based form. This is useful when:
/// <list type="bullet">
///   <item><description>You have many tests for the same generator</description></item>
///   <item><description>You want to avoid repeating the generic type parameter</description></item>
///   <item><description>You prefer a more object-oriented testing style</description></item>
/// </list>
/// </para>
/// <para>
/// For an inheritance-based approach, see <see cref="GeneratorTestBase{TGenerator}"/>.
/// </para>
/// </remarks>
/// <example>
/// Using <see cref="GeneratorTester{TGenerator}"/> in a test class:
/// <code>
/// public class BuilderGeneratorTests
/// {
///     private readonly GeneratorTester&lt;BuilderGenerator&gt; G = GeneratorTester&lt;BuilderGenerator&gt;.Create();
///
///     [Fact]
///     public async Task Generates_builder_class()
///     {
///         await G.ShouldGenerate(
///             "[GenerateBuilder] public class Person { }",
///             "Person.Builder.g.cs",
///             "public class PersonBuilder");
///     }
///
///     [Fact]
///     public async Task Caches_correctly()
///     {
///         await G.ShouldCache("[GenerateBuilder] public class Person { }");
///     }
/// }
/// </code>
/// </example>
public sealed class GeneratorTester<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    private GeneratorTester()
    {
    }

    /// <summary>
    /// Creates a new tester bound to <typeparamref name="TGenerator"/>.
    /// </summary>
    /// <returns>A new <see cref="GeneratorTester{TGenerator}"/> instance.</returns>
    /// <example>
    /// <code>
    /// var tester = GeneratorTester&lt;MyGenerator&gt;.Create();
    /// await tester.ShouldGenerate(source, "Output.g.cs", expectedContent);
    /// </code>
    /// </example>
    public static GeneratorTester<TGenerator> Create()
    {
        return new GeneratorTester<TGenerator>();
    }

    /// <summary>
    /// Verifies that running the generator on the source produces a file with
    /// the expected hint name and content.
    /// </summary>
    /// <param name="source">C# source code to compile.</param>
    /// <param name="hintName">Expected hint name of the generated file.</param>
    /// <param name="expectedContent">Expected content of the generated file.</param>
    /// <param name="exactMatch">If <c>true</c>, requires exact content match.</param>
    /// <param name="normalizeNewlines">If <c>true</c>, normalizes line endings before comparison.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <inheritdoc cref="GeneratorTest.ShouldGenerate{TGenerator}(string,string,string)"/>
    public Task ShouldGenerate(string source, string hintName, string expectedContent, bool exactMatch = true,
        bool normalizeNewlines = true)
    {
        return source.ShouldGenerate<TGenerator>(hintName, expectedContent, exactMatch, normalizeNewlines);
    }

    /// <summary>
    /// Asserts that diagnostics match the provided expectations.
    /// </summary>
    /// <param name="source">C# source code to compile.</param>
    /// <param name="expected">Expected diagnostics.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <seealso cref="GeneratorTest.ShouldHaveDiagnostic{TGenerator}(string,string,DiagnosticSeverity)"/>
    public Task ShouldHaveDiagnostics(string source, params DiagnosticResult[] expected)
    {
        return source.ShouldHaveDiagnostics<TGenerator>(expected);
    }

    /// <summary>
    /// Asserts that the generator produces no diagnostics.
    /// </summary>
    /// <param name="source">C# source code to compile.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <inheritdoc cref="GeneratorTest.ShouldCompile{TGenerator}(string)"/>
    public Task ShouldCompile(string source)
    {
        return source.ShouldHaveNoDiagnostics<TGenerator>();
    }

    /// <summary>
    /// Verifies that pipeline steps are cached correctly.
    /// </summary>
    /// <param name="source">C# source code to compile.</param>
    /// <param name="trackingNames">Optional step names to validate.</param>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <inheritdoc cref="GeneratorTest.ShouldCache{TGenerator}(string,string[])"/>
    public Task ShouldCache(string source, params string[] trackingNames)
    {
        return source.ShouldBeCached<TGenerator>(trackingNames);
    }

    /// <summary>
    /// Validates caching behavior across a compilation change.
    /// </summary>
    /// <param name="source">C# source code to compile.</param>
    /// <param name="makeChange">Function that modifies the compilation.</param>
    /// <param name="validate">Optional validation callback.</param>
    /// <returns>A task that completes when validations finish.</returns>
    /// <inheritdoc cref="GeneratorTest.ShouldCacheWithCompilationUpdate{TGenerator}(string,Func{Compilation,Compilation},Action{CompilationCacheResult}?)"/>
    public Task ShouldCacheWithCompilationUpdate(string source, Func<Compilation, Compilation> makeChange,
        Action<CompilationCacheResult>? validate = null)
    {
        return source.ShouldCacheWithCompilationUpdate<TGenerator>(makeChange, validate);
    }

    /// <summary>
    /// Verifies that modifying the source causes regeneration.
    /// </summary>
    /// <param name="source">Original C# source code.</param>
    /// <param name="editedSource">Modified source code.</param>
    /// <returns>A task that completes when validations finish.</returns>
    /// <inheritdoc cref="GeneratorTest.ShouldRegenerate{TGenerator}(string,string)"/>
    public Task ShouldRegenerate(string source, string editedSource)
    {
        return source.ShouldRegenerate<TGenerator>(editedSource);
    }

    /// <summary>
    /// Verifies that adding an unrelated file does not cause regeneration.
    /// </summary>
    /// <param name="source">Original C# source code.</param>
    /// <param name="newFileContent">Content of the new file to add.</param>
    /// <returns>A task that completes when validations finish.</returns>
    /// <inheritdoc cref="GeneratorTest.ShouldNotRegenerate{TGenerator}(string,string)"/>
    public Task ShouldNotRegenerate(string source, string newFileContent)
    {
        return source.ShouldNotRegenerate<TGenerator>(newFileContent);
    }
}

/// <summary>
/// Base class that exposes the same API as <see cref="GeneratorTester{TGenerator}"/>
/// as protected methods, so test classes can inherit and call methods directly.
/// </summary>
/// <typeparam name="TGenerator">
/// The generator under test. Must implement <see cref="IIncrementalGenerator"/>
/// and have a parameterless constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// This base class provides an alternative to <see cref="GeneratorTester{TGenerator}"/>
/// for developers who prefer an inheritance-based approach. By deriving from this class,
/// you can call test methods like <c>ShouldGenerate(...)</c> directly without a field reference.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class BuilderGeneratorTests : GeneratorTestBase&lt;BuilderGenerator&gt;
/// {
///     [Fact]
///     public async Task Generates_expected_file()
///     {
///         await ShouldGenerate(
///             "[GenerateBuilder] class C { }",
///             "C.Builder.g.cs",
///             "class CBuilder");
///     }
///
///     [Fact]
///     public async Task Caches_correctly()
///     {
///         await ShouldCache("[GenerateBuilder] class C { }");
///     }
/// }
/// </code>
/// </example>
public abstract class GeneratorTestBase<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    /// <summary>
    /// Gets the typed tester instance bound to <typeparamref name="TGenerator"/>.
    /// </summary>
    /// <remarks>
    /// This property can be used directly if you need to pass the tester instance
    /// to helper methods, or you can use the protected methods that delegate to it.
    /// </remarks>
    protected GeneratorTester<TGenerator> Test { get; } = GeneratorTester<TGenerator>.Create();

    /// <summary>
    /// Verifies that running the generator produces a file with the expected content.
    /// </summary>
    /// <inheritdoc cref="GeneratorTest.ShouldGenerate{TGenerator}(string,string,string)"/>
    protected Task ShouldGenerate(string source, string hintName, string expectedContent, bool exactMatch = true,
        bool normalizeNewlines = true)
    {
        return Test.ShouldGenerate(source, hintName, expectedContent, exactMatch, normalizeNewlines);
    }

    /// <summary>
    /// Asserts that diagnostics match the provided expectations.
    /// </summary>
    protected Task ShouldHaveDiagnostics(string source, params DiagnosticResult[] expected)
    {
        return Test.ShouldHaveDiagnostics(source, expected);
    }

    /// <summary>
    /// Asserts that the generator produces no diagnostics.
    /// </summary>
    /// <inheritdoc cref="GeneratorTest.ShouldCompile{TGenerator}(string)"/>
    protected Task ShouldCompile(string source)
    {
        return Test.ShouldCompile(source);
    }

    /// <summary>
    /// Verifies that pipeline steps are cached correctly.
    /// </summary>
    /// <inheritdoc cref="GeneratorTest.ShouldCache{TGenerator}(string,string[])"/>
    protected Task ShouldCache(string source, params string[] trackingNames)
    {
        return Test.ShouldCache(source, trackingNames);
    }

    /// <summary>
    /// Validates caching behavior across a compilation change.
    /// </summary>
    /// <inheritdoc cref="GeneratorTest.ShouldCacheWithCompilationUpdate{TGenerator}(string,Func{Compilation,Compilation},Action{CompilationCacheResult}?)"/>
    protected Task ShouldCacheWithCompilationUpdate(string source, Func<Compilation, Compilation> makeChange,
        Action<CompilationCacheResult>? validate = null)
    {
        return Test.ShouldCacheWithCompilationUpdate(source, makeChange, validate);
    }

    /// <summary>
    /// Verifies that modifying the source causes regeneration.
    /// </summary>
    /// <inheritdoc cref="GeneratorTest.ShouldRegenerate{TGenerator}(string,string)"/>
    protected Task ShouldRegenerate(string source, string editedSource)
    {
        return Test.ShouldRegenerate(source, editedSource);
    }

    /// <summary>
    /// Verifies that adding an unrelated file does not cause regeneration.
    /// </summary>
    /// <inheritdoc cref="GeneratorTest.ShouldNotRegenerate{TGenerator}(string,string)"/>
    protected Task ShouldNotRegenerate(string source, string newFileContent)
    {
        return Test.ShouldNotRegenerate(source, newFileContent);
    }
}
