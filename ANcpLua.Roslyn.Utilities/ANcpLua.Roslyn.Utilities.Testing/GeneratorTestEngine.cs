using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     A fluent builder and runner for executing Roslyn incremental generator tests.
/// </summary>
internal sealed class GeneratorTestEngine
{
    private readonly List<AdditionalText> _additionalTexts = [];
    private readonly List<IIncrementalGenerator> _generators = [];
    private readonly List<MetadataReference> _references = [];
    private readonly List<SyntaxTree> _sources = [];
    private AnalyzerConfigOptionsProvider? _analyzerConfigOptions;
    private LanguageVersion _languageVersion = TestConfiguration.LanguageVersion;
    private ReferenceAssemblies _referenceAssemblies = TestConfiguration.ReferenceAssemblies;
    private bool _trackSteps = true;

    /// <summary>
    ///     Adds a generator to the test.
    /// </summary>
    /// <param name="generator">The incremental generator to include in the test.</param>
    /// <returns>The current engine instance for method chaining.</returns>
    public GeneratorTestEngine WithGenerator(IIncrementalGenerator generator)
    {
        _generators.Add(generator);
        return this;
    }

    /// <summary>
    ///     Adds source code to the compilation.
    /// </summary>
    /// <param name="source">
    ///     The C# source code to include in the test compilation.
    ///     The source is parsed using the configured <see cref="LanguageVersion" />.
    /// </param>
    /// <returns>The current engine instance for method chaining.</returns>
    public GeneratorTestEngine WithSource(string source)
    {
        var parseOptions = new CSharpParseOptions(_languageVersion, DocumentationMode.Diagnose);
        _sources.Add(CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8), parseOptions));
        return this;
    }

    /// <summary>
    ///     Adds a metadata reference to the compilation.
    /// </summary>
    /// <param name="reference">The metadata reference to add.</param>
    /// <returns>The current engine instance for method chaining.</returns>
    public GeneratorTestEngine WithReference(MetadataReference reference)
    {
        _references.Add(reference);
        return this;
    }

    /// <summary>
    ///     Adds an additional file to the generator driver.
    /// </summary>
    /// <param name="path">The virtual file path for the additional text.</param>
    /// <param name="text">The content of the additional file.</param>
    /// <returns>The current engine instance for method chaining.</returns>
    public GeneratorTestEngine WithAdditionalText(string path, string text)
    {
        _additionalTexts.Add(new InMemoryAdditionalText(path, text));
        return this;
    }

    /// <summary>
    ///     Sets the analyzer config options provider for MSBuild property access.
    /// </summary>
    /// <param name="options">The options provider.</param>
    /// <returns>The current engine instance for method chaining.</returns>
    public GeneratorTestEngine WithAnalyzerConfigOptions(AnalyzerConfigOptionsProvider options)
    {
        _analyzerConfigOptions = options;
        return this;
    }

    /// <summary>
    ///     Sets the C# language version for parsing source code.
    /// </summary>
    /// <param name="version">The language version to use.</param>
    /// <returns>The current engine instance for method chaining.</returns>
    public GeneratorTestEngine WithLanguageVersion(LanguageVersion version)
    {
        _languageVersion = version;
        return this;
    }

    /// <summary>
    ///     Sets the reference assemblies to use for the test compilation.
    /// </summary>
    /// <param name="assemblies">The reference assemblies package to resolve.</param>
    /// <returns>The current engine instance for method chaining.</returns>
    public GeneratorTestEngine WithReferenceAssemblies(ReferenceAssemblies assemblies)
    {
        _referenceAssemblies = assemblies;
        return this;
    }

    /// <summary>
    ///     Enables or disables step tracking for caching analysis.
    /// </summary>
    /// <param name="trackSteps">Whether to enable step tracking.</param>
    /// <returns>The current engine instance for method chaining.</returns>
    public GeneratorTestEngine WithStepTracking(bool trackSteps = true)
    {
        _trackSteps = trackSteps;
        return this;
    }

    /// <summary>
    ///     Creates a compilation from all configured sources and references.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="CSharpCompilation" />.</returns>
    public async Task<CSharpCompilation> CreateCompilationAsync(CancellationToken cancellationToken = default)
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

    /// <summary>
    ///     Creates a generator driver with all configured options.
    /// </summary>
    /// <returns>A <see cref="GeneratorDriver" /> configured with all options.</returns>
    public GeneratorDriver CreateDriver()
    {
        var parseOptions = new CSharpParseOptions(_languageVersion);
        var generators = _generators.Select(static g => g.AsSourceGenerator()).ToArray();

        return CSharpGeneratorDriver.Create(
            generators,
            _additionalTexts,
            parseOptions,
            _analyzerConfigOptions,
            new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, _trackSteps));
    }

    /// <summary>
    ///     Executes the generator(s) twice to validate incremental caching behavior.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a tuple containing the results of both runs.</returns>
    internal async Task<(GeneratorDriverRunResult FirstRun, GeneratorDriverRunResult SecondRun)> RunTwiceAsync(
        CancellationToken cancellationToken = default)
    {
        var compilation = await CreateCompilationAsync(cancellationToken);
        var driver = CreateDriver();

        // First run
        driver = driver.RunGenerators(compilation, cancellationToken);
        var firstRun = driver.GetRunResult();

        // Second run (clone compilation)
        var secondCompilation = CSharpCompilation.Create(
            compilation.AssemblyName,
            compilation.SyntaxTrees,
            compilation.References,
            compilation.Options);

        driver = driver.RunGenerators(secondCompilation, cancellationToken);
        var secondRun = driver.GetRunResult();

        return (firstRun, secondRun);
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(text, Encoding.UTF8);
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}

internal class InClassName(bool trackSteps)
{
    public bool TrackSteps { get; } = trackSteps;
}

/// <summary>
///     Generic version of <see cref="GeneratorTestEngine" /> for a single generator.
/// </summary>
/// <typeparam name="TGenerator">
///     The incremental generator type to test. Must implement <see cref="IIncrementalGenerator" />
///     and have a parameterless constructor.
/// </typeparam>
internal sealed class GeneratorTestEngine<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    private readonly GeneratorTestEngine _engine = new();

    public GeneratorTestEngine()
    {
        _engine.WithGenerator(new TGenerator());
    }

    public GeneratorTestEngine<TGenerator> WithSource(string source)
    {
        _engine.WithSource(source);
        return this;
    }

    public GeneratorTestEngine<TGenerator> WithReference(MetadataReference reference)
    {
        _engine.WithReference(reference);
        return this;
    }

    public GeneratorTestEngine<TGenerator> WithAdditionalText(string path, string text)
    {
        _engine.WithAdditionalText(path, text);
        return this;
    }

    public GeneratorTestEngine<TGenerator> WithAnalyzerConfigOptions(AnalyzerConfigOptionsProvider options)
    {
        _engine.WithAnalyzerConfigOptions(options);
        return this;
    }

    public GeneratorTestEngine<TGenerator> WithLanguageVersion(LanguageVersion version)
    {
        _engine.WithLanguageVersion(version);
        return this;
    }

    public GeneratorTestEngine<TGenerator> WithReferenceAssemblies(ReferenceAssemblies assemblies)
    {
        _engine.WithReferenceAssemblies(assemblies);
        return this;
    }

    public Task<CSharpCompilation> CreateCompilationAsync(CancellationToken cancellationToken = default) => _engine.CreateCompilationAsync(cancellationToken);

    public Task<CSharpCompilation> CreateCompilationAsync(string source, CancellationToken cancellationToken = default) => WithSource(source).CreateCompilationAsync(cancellationToken);

    public static GeneratorDriver CreateDriver(InClassName inClassName)
    {
        var trackSteps = inClassName.TrackSteps;
        return new GeneratorTestEngine<TGenerator>().WithStepTracking(trackSteps).CreateDriver();
    }

    public GeneratorTestEngine<TGenerator> WithStepTracking(bool trackSteps = true)
    {
        _engine.WithStepTracking(trackSteps);
        return this;
    }

    public GeneratorDriver CreateDriver() => _engine.CreateDriver();

    internal Task<(GeneratorDriverRunResult FirstRun, GeneratorDriverRunResult SecondRun)> RunTwiceAsync(
        CancellationToken cancellationToken = default) =>
        _engine.RunTwiceAsync(cancellationToken);
}
