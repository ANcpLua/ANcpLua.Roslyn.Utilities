using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     A builder and runner for executing Roslyn generator tests.
/// </summary>
/// <typeparam name="TGenerator">The generator type to test.</typeparam>
public sealed class GeneratorTestEngine<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    private readonly List<AdditionalText> _additionalTexts = [];
    private readonly List<MetadataReference> _references = [];
    private readonly List<SyntaxTree> _sources = [];
    private AnalyzerConfigOptionsProvider? _analyzerConfigOptions;
    private LanguageVersion _languageVersion = TestConfiguration.LanguageVersion;
    private ReferenceAssemblies _referenceAssemblies = TestConfiguration.ReferenceAssemblies;
    private readonly bool _trackSteps = true; // Default to true for safer defaults in tests

    /// <summary>
    ///     Adds source code to the compilation.
    /// </summary>
    public GeneratorTestEngine<TGenerator> WithSource(string source)
    {
        var parseOptions = new CSharpParseOptions(_languageVersion, DocumentationMode.Diagnose);
        _sources.Add(CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8), parseOptions));
        return this;
    }

    /// <summary>
    ///     Adds a metadata reference to the compilation.
    /// </summary>
    public GeneratorTestEngine<TGenerator> WithReference(MetadataReference reference)
    {
        _references.Add(reference);
        return this;
    }

    /// <summary>
    ///     Adds an additional file to the generator driver.
    /// </summary>
    public GeneratorTestEngine<TGenerator> WithAdditionalText(string path, string text)
    {
        _additionalTexts.Add(new InMemoryAdditionalText(path, text));
        return this;
    }

    /// <summary>
    ///     Sets the analyzer config options provider.
    /// </summary>
    public GeneratorTestEngine<TGenerator> WithAnalyzerConfigOptions(AnalyzerConfigOptionsProvider options)
    {
        _analyzerConfigOptions = options;
        return this;
    }

    /// <summary>
    ///     Sets the language version for parsing.
    /// </summary>
    public GeneratorTestEngine<TGenerator> WithLanguageVersion(LanguageVersion version)
    {
        _languageVersion = version;
        return this;
    }

    /// <summary>
    ///     Sets the reference assemblies to use (default: .NET 10).
    /// </summary>
    public GeneratorTestEngine<TGenerator> WithReferenceAssemblies(ReferenceAssemblies assemblies)
    {
        _referenceAssemblies = assemblies;
        return this;
    }

    /// <summary>
    ///     Creates a compilation from source code (for advanced test scenarios).
    /// </summary>
    public async Task<CSharpCompilation> CreateCompilationAsync(string source, CancellationToken cancellationToken = default)
    {
        return await WithSource(source).CreateCompilationAsync(cancellationToken);
    }

    /// <summary>
    ///     Creates a generator driver (for advanced test scenarios).
    /// </summary>
    public static GeneratorDriver CreateDriver(bool trackSteps = true)
    {
        var parseOptions = new CSharpParseOptions(TestConfiguration.LanguageVersion);
        var generator = new TGenerator().AsSourceGenerator();
        return CSharpGeneratorDriver.Create(
            [generator],
            parseOptions: parseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackSteps));
    }

    /// <summary>
    ///     Executes the generator twice (standard caching check).
    /// </summary>
    public async Task<(GeneratorDriverRunResult FirstRun, GeneratorDriverRunResult SecondRun)> RunTwiceAsync(
        CancellationToken cancellationToken = default)
    {
        var compilation = await CreateCompilationAsync(cancellationToken);
        var driver = CreateDriver();

        // First run
        driver = driver.RunGenerators(compilation, cancellationToken);
        var firstRun = driver.GetRunResult();

        // Second run (clone compilation to simulate "next key press" or essentially no change)
        var secondCompilation = CSharpCompilation.Create(
            compilation.AssemblyName,
            compilation.SyntaxTrees,
            compilation.References,
            compilation.Options);

        driver = driver.RunGenerators(secondCompilation, cancellationToken);
        var secondRun = driver.GetRunResult();

        return (firstRun, secondRun);
    }

    private async Task<CSharpCompilation> CreateCompilationAsync(CancellationToken cancellationToken)
    {
        var resolvedReferences = await _referenceAssemblies.ResolveAsync(LanguageNames.CSharp, cancellationToken);

        var allReferences = resolvedReferences
            .Concat(_references)
            .Concat(TestConfiguration.AdditionalReferences);

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable,
            allowUnsafe: true);

        return CSharpCompilation.Create(
            "TestAssembly",
            _sources,
            allReferences,
            compilationOptions);
    }

    private GeneratorDriver CreateDriver()
    {
        var parseOptions = new CSharpParseOptions(_languageVersion);
        var generator = new TGenerator().AsSourceGenerator();

        return CSharpGeneratorDriver.Create(
            [generator],
            _additionalTexts,
            parseOptions,
            _analyzerConfigOptions,
            new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, _trackSteps));
    }

    // Simple in-memory additional text
    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(text, Encoding.UTF8);

        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}