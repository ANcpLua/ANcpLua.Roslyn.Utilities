using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
/// Engine for executing generator tests.
/// </summary>
/// <typeparam name="TGenerator">The generator type to test.</typeparam>
internal sealed class GeneratorTestEngine<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    private readonly CSharpCompilationOptions _compilationOptions = new(OutputKind.DynamicallyLinkedLibrary,
        nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true);

    private readonly CSharpParseOptions _parseOptions =
        new(TestConfiguration.LanguageVersion, DocumentationMode.Diagnose);

    /// <summary>
    /// Executes the generator twice with the same source to test caching.
    /// </summary>
    /// <param name="source">The source code to compile.</param>
    /// <param name="trackSteps">Whether to track pipeline steps.</param>
    /// <returns>The results from both runs.</returns>
    public async Task<(GeneratorDriverRunResult FirstRun, GeneratorDriverRunResult SecondRun)> ExecuteTwiceAsync(
        string source, bool trackSteps)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8), _parseOptions);
        var references =
            await TestConfiguration.ReferenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
        var compilation =
            CSharpCompilation.Create("TestAssembly", [syntaxTree], references, _compilationOptions);

        var driver = CreateGeneratorDriver(trackSteps, _parseOptions);

        driver = driver.RunGenerators(compilation);
        var firstRun = driver.GetRunResult();

        var secondCompilation = CSharpCompilation.Create(compilation.AssemblyName!,
            compilation.SyntaxTrees, compilation.References, _compilationOptions);
        driver = driver.RunGenerators(secondCompilation);
        var secondRun = driver.GetRunResult();

        return (firstRun, secondRun);
    }

    /// <summary>
    /// Creates a compilation from source code.
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

    private static GeneratorDriver CreateGeneratorDriver(bool trackSteps, CSharpParseOptions parseOptions)
    {
        return CSharpGeneratorDriver.Create([new TGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackSteps),
            parseOptions: parseOptions);
    }
}