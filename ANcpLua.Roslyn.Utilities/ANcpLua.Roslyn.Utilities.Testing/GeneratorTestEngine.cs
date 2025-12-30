using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Engine for executing generator tests and creating configured generator drivers.
/// </summary>
/// <typeparam name="TGenerator">The generator type to test.</typeparam>
/// <remarks>
///     <para>
///         This class provides the core test execution infrastructure for incremental generators.
///         It handles compilation creation, generator driver setup, and two-pass execution for caching validation.
///     </para>
///     <para>
///         Use <see cref="CreateDriver" /> to create a standalone driver for advanced scenarios,
///         or <see cref="ExecuteTwiceAsync" /> for standard caching validation tests.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// var engine = new GeneratorTestEngine&lt;MyGenerator&gt;();
/// var (firstRun, secondRun) = await engine.ExecuteTwiceAsync(source, trackSteps: true);
///
/// // Or create a driver directly for custom scenarios
/// var driver = GeneratorTestEngine&lt;MyGenerator&gt;.CreateDriver(trackSteps: true);
/// </code>
/// </example>
public sealed class GeneratorTestEngine<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    private readonly CSharpCompilationOptions _compilationOptions = new(OutputKind.DynamicallyLinkedLibrary,
        nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true);

    private readonly CSharpParseOptions _parseOptions =
        new(TestConfiguration.LanguageVersion, DocumentationMode.Diagnose);

    /// <summary>
    ///     Creates a <see cref="GeneratorDriver" /> configured for the specified generator.
    /// </summary>
    /// <param name="trackSteps">
    ///     If <c>true</c>, enables pipeline step tracking for caching analysis.
    ///     This is required for <see cref="GeneratorCachingReport" /> and related functionality.
    /// </param>
    /// <returns>A configured <see cref="GeneratorDriver" /> ready to be run against a compilation.</returns>
    /// <remarks>
    ///     <para>
    ///         The driver is configured with:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>The specified generator wrapped as a source generator</description>
    ///             </item>
    ///             <item>
    ///                 <description>Step tracking as specified by <paramref name="trackSteps" /></description>
    ///             </item>
    ///             <item>
    ///                 <description>Parse options from <see cref="TestConfiguration.LanguageVersion" /></description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Step tracking has a small performance overhead, so it should only be enabled
    ///         when caching validation is needed.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // For generation tests (no step tracking needed)
    /// var driver = GeneratorTestEngine&lt;MyGenerator&gt;.CreateDriver(false);
    /// 
    /// // For caching tests (step tracking required)
    /// var driver = GeneratorTestEngine&lt;MyGenerator&gt;.CreateDriver(true);
    /// driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
    /// </code>
    /// </example>
    public static GeneratorDriver CreateDriver(bool trackSteps)
    {
        var parseOptions = new CSharpParseOptions(TestConfiguration.LanguageVersion);
        return CSharpGeneratorDriver.Create(
            [new TGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackSteps),
            parseOptions: parseOptions);
    }

    /// <summary>
    ///     Executes the generator twice with the same source to test caching.
    /// </summary>
    /// <param name="source">The source code to compile.</param>
    /// <param name="trackSteps">Whether to track pipeline steps.</param>
    /// <returns>The results from both runs.</returns>
    /// <remarks>
    ///     <para>
    ///         This method runs the generator twice on equivalent compilations to verify
    ///         that caching works correctly. The second run should show cached results
    ///         for unchanged pipeline outputs.
    ///     </para>
    /// </remarks>
    public async Task<(GeneratorDriverRunResult FirstRun, GeneratorDriverRunResult SecondRun)> ExecuteTwiceAsync(
        string source, bool trackSteps)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8), _parseOptions);
        var references =
            await TestConfiguration.ReferenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
        var compilation =
            CSharpCompilation.Create("TestAssembly", [syntaxTree], references, _compilationOptions);

        var driver = CreateDriverInternal(trackSteps, _parseOptions);

        driver = driver.RunGenerators(compilation);
        var firstRun = driver.GetRunResult();

        var secondCompilation = CSharpCompilation.Create(compilation.AssemblyName!,
            compilation.SyntaxTrees, compilation.References, _compilationOptions);
        driver = driver.RunGenerators(secondCompilation);
        var secondRun = driver.GetRunResult();

        return (firstRun, secondRun);
    }

    /// <summary>
    ///     Creates a compilation from source code.
    /// </summary>
    /// <param name="source">The source code to compile.</param>
    /// <returns>The compiled Compilation.</returns>
    public async Task<Compilation> CreateCompilationAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, _parseOptions);
        var references =
            await TestConfiguration.ReferenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
        return CSharpCompilation.Create("TestAssembly", [syntaxTree], references, _compilationOptions);
    }

    private static GeneratorDriver CreateDriverInternal(bool trackSteps, CSharpParseOptions parseOptions)
    {
        return CSharpGeneratorDriver.Create([new TGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackSteps),
            parseOptions: parseOptions);
    }
}