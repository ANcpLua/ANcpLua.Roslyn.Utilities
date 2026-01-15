using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     A fluent builder and runner for executing Roslyn incremental generator tests.
/// </summary>
/// <typeparam name="TGenerator">
///     The incremental generator type to test. Must implement <see cref="IIncrementalGenerator" />
///     and have a parameterless constructor.
/// </typeparam>
/// <remarks>
///     <para>
///         This class provides a fluent API for configuring and running generator tests,
///         including support for caching validation through the <see cref="RunTwiceAsync" /> method.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Configure source code, references, and additional texts via fluent methods</description>
///         </item>
///         <item>
///             <description>Supports custom language versions and reference assemblies</description>
///         </item>
///         <item>
///             <description>Step tracking is enabled by default for comprehensive test diagnostics</description>
///         </item>
///         <item>
///             <description>Creates isolated compilations per test to avoid cross-test interference</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///     var engine = new GeneratorTestEngine&lt;MyGenerator&gt;()
///         .WithSource(@"
///             using System;
///             [MyAttribute]
///             public class Foo { }
///         ")
///         .WithReference(MetadataReference.CreateFromFile(typeof(MyAttribute).Assembly.Location))
///         .WithLanguageVersion(LanguageVersion.CSharp12);
/// 
///     var (firstRun, secondRun) = await engine.RunTwiceAsync();
///     // Assert caching behavior by comparing firstRun and secondRun
///     </code>
/// </example>
/// <seealso cref="Test{TGenerator}" />
/// <seealso cref="GeneratorResult{TGenerator}" />
/// <seealso cref="TestConfiguration" />
public sealed class GeneratorTestEngine<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    private const bool _trackSteps = true; // Default to true for safer defaults in tests
    private readonly List<AdditionalText> _additionalTexts = [];
    private readonly List<MetadataReference> _references = [];
    private readonly List<SyntaxTree> _sources = [];
    private AnalyzerConfigOptionsProvider? _analyzerConfigOptions;
    private LanguageVersion _languageVersion = TestConfiguration.LanguageVersion;
    private ReferenceAssemblies _referenceAssemblies = TestConfiguration.ReferenceAssemblies;

    /// <summary>
    ///     Adds source code to the compilation.
    /// </summary>
    /// <param name="source">
    ///     The C# source code to include in the test compilation.
    ///     The source is parsed using the configured <see cref="LanguageVersion" />.
    /// </param>
    /// <returns>The current engine instance for method chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Multiple sources can be added by calling this method repeatedly</description>
    ///         </item>
    ///         <item>
    ///             <description>Source is parsed with UTF-8 encoding and documentation mode enabled</description>
    ///         </item>
    ///         <item>
    ///             <description>Each call creates a new syntax tree in the compilation</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var engine = new GeneratorTestEngine&lt;MyGenerator&gt;()
    ///         .WithSource("public class A { }")
    ///         .WithSource("public class B { }");
    ///     </code>
    /// </example>
    public GeneratorTestEngine<TGenerator> WithSource(string source)
    {
        var parseOptions = new CSharpParseOptions(_languageVersion, DocumentationMode.Diagnose);
        _sources.Add(CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8), parseOptions));
        return this;
    }

    /// <summary>
    ///     Adds a metadata reference to the compilation.
    /// </summary>
    /// <param name="reference">
    ///     The metadata reference to add. Use <see cref="MetadataReference.CreateFromFile" />
    ///     to create references from assembly paths.
    /// </param>
    /// <returns>The current engine instance for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         References are combined with <see cref="TestConfiguration.AdditionalReferences" />
    ///         and the resolved <see cref="ReferenceAssemblies" /> when creating the compilation.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var engine = new GeneratorTestEngine&lt;MyGenerator&gt;()
    ///         .WithReference(MetadataReference.CreateFromFile(typeof(MyAttribute).Assembly.Location));
    ///     </code>
    /// </example>
    /// <seealso cref="WithReferenceAssemblies" />
    public GeneratorTestEngine<TGenerator> WithReference(MetadataReference reference)
    {
        _references.Add(reference);
        return this;
    }

    /// <summary>
    ///     Adds an additional file to the generator driver.
    /// </summary>
    /// <param name="path">
    ///     The virtual file path for the additional text. This path is visible to the generator
    ///     via <see cref="AdditionalText.Path" />.
    /// </param>
    /// <param name="text">The content of the additional file.</param>
    /// <returns>The current engine instance for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Additional texts are commonly used for configuration files, templates, or other
    ///         non-C# files that generators may process.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var engine = new GeneratorTestEngine&lt;MyGenerator&gt;()
    ///         .WithAdditionalText("config.json", @"{ ""enabled"": true }");
    ///     </code>
    /// </example>
    public GeneratorTestEngine<TGenerator> WithAdditionalText(string path, string text)
    {
        _additionalTexts.Add(new InMemoryAdditionalText(path, text));
        return this;
    }

    /// <summary>
    ///     Sets the analyzer config options provider for MSBuild property access.
    /// </summary>
    /// <param name="options">
    ///     The options provider that supplies MSBuild properties and per-file options
    ///     to the generator.
    /// </param>
    /// <returns>The current engine instance for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Use this method to simulate MSBuild properties that your generator reads via
    ///         <c>AnalyzerConfigOptionsProvider.GlobalOptions</c>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var options = new TestAnalyzerConfigOptionsProvider(
    ///         globalOptions: new Dictionary&lt;string, string&gt;
    ///         {
    ///             ["build_property.RootNamespace"] = "MyApp"
    ///         });
    ///     var engine = new GeneratorTestEngine&lt;MyGenerator&gt;()
    ///         .WithAnalyzerConfigOptions(options);
    ///     </code>
    /// </example>
    public GeneratorTestEngine<TGenerator> WithAnalyzerConfigOptions(AnalyzerConfigOptionsProvider options)
    {
        _analyzerConfigOptions = options;
        return this;
    }

    /// <summary>
    ///     Sets the C# language version for parsing source code.
    /// </summary>
    /// <param name="version">
    ///     The language version to use. Defaults to <see cref="TestConfiguration.LanguageVersion" />.
    /// </param>
    /// <returns>The current engine instance for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This affects how source code is parsed and which language features are available.
    ///         Call this method before <see cref="WithSource" /> to ensure sources are parsed
    ///         with the correct language version.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var engine = new GeneratorTestEngine&lt;MyGenerator&gt;()
    ///         .WithLanguageVersion(LanguageVersion.CSharp11)
    ///         .WithSource("file-scoped namespace Test;");
    ///     </code>
    /// </example>
    /// <seealso cref="TestConfiguration.WithLanguageVersion" />
    public GeneratorTestEngine<TGenerator> WithLanguageVersion(LanguageVersion version)
    {
        _languageVersion = version;
        return this;
    }

    /// <summary>
    ///     Sets the reference assemblies to use for the test compilation.
    /// </summary>
    /// <param name="assemblies">
    ///     The reference assemblies package to resolve. Defaults to .NET 10 via
    ///     <see cref="TestConfiguration.ReferenceAssemblies" />.
    /// </param>
    /// <returns>The current engine instance for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Reference assemblies determine the base class library available to the compilation.
    ///         Use this to test against different .NET versions.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var engine = new GeneratorTestEngine&lt;MyGenerator&gt;()
    ///         .WithReferenceAssemblies(ReferenceAssemblies.Net.Net80);
    ///     </code>
    /// </example>
    /// <seealso cref="TestConfiguration.WithReferenceAssemblies" />
    public GeneratorTestEngine<TGenerator> WithReferenceAssemblies(ReferenceAssemblies assemblies)
    {
        _referenceAssemblies = assemblies;
        return this;
    }

    /// <summary>
    ///     Creates a compilation from source code for advanced test scenarios.
    /// </summary>
    /// <param name="source">The C# source code to compile.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    ///     A task that resolves to a <see cref="CSharpCompilation" /> containing the source
    ///     and all configured references.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This is a convenience overload that calls <see cref="WithSource" /> followed by
    ///         the internal <c>CreateCompilationAsync</c> method. Use this for simple single-source
    ///         test scenarios where you need direct access to the compilation.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var engine = new GeneratorTestEngine&lt;MyGenerator&gt;();
    ///     var compilation = await engine.CreateCompilationAsync("public class Foo { }");
    ///     var driver = GeneratorTestEngine&lt;MyGenerator&gt;.CreateDriver();
    ///     driver = driver.RunGenerators(compilation);
    ///     </code>
    /// </example>
    public async Task<CSharpCompilation> CreateCompilationAsync(string source,
        CancellationToken cancellationToken = default)
    {
        return await WithSource(source).CreateCompilationAsync(cancellationToken);
    }

    /// <summary>
    ///     Creates a generator driver configured with step tracking for advanced test scenarios.
    /// </summary>
    /// <param name="trackSteps">
    ///     Whether to enable step tracking for caching analysis. Defaults to <c>true</c>.
    /// </param>
    /// <returns>
    ///     A <see cref="GeneratorDriver" /> configured with the generator and the specified
    ///     tracking options.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This static method creates a minimal driver without additional texts or
    ///         analyzer config options. Use this for advanced scenarios where you need
    ///         direct control over the driver configuration.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Uses <see cref="TestConfiguration.LanguageVersion" /> for parsing</description>
    ///         </item>
    ///         <item>
    ///             <description>Step tracking enables <see cref="GeneratorDriverRunResult.TrackedSteps" /></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var driver = GeneratorTestEngine&lt;MyGenerator&gt;.CreateDriver(trackSteps: true);
    ///     driver = driver.RunGenerators(compilation);
    ///     var result = driver.GetRunResult();
    ///     foreach (var step in result.TrackedSteps)
    ///     {
    ///         Console.WriteLine(step.Key);
    ///     }
    ///     </code>
    /// </example>
    /// <seealso cref="GeneratorDriverRunResult.TrackedSteps" />
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
    ///     Executes the generator twice to validate incremental caching behavior.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    ///     A task that resolves to a tuple containing the results of both generator runs.
    ///     <c>FirstRun</c> is the initial execution; <c>SecondRun</c> uses a cloned compilation
    ///     to simulate an unchanged recompilation.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is the core of caching validation. By running the generator twice
    ///         with an equivalent compilation, you can verify that pipeline steps correctly
    ///         report <see cref="IncrementalStepRunReason.Cached" /> or
    ///         <see cref="IncrementalStepRunReason.Unchanged" /> on the second run.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>First run initializes the generator and produces output</description>
    ///         </item>
    ///         <item>
    ///             <description>Second run uses a cloned compilation to simulate "next key press"</description>
    ///         </item>
    ///         <item>
    ///             <description>Compare <see cref="GeneratorDriverRunResult.TrackedSteps" /> between runs</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var engine = new GeneratorTestEngine&lt;MyGenerator&gt;()
    ///         .WithSource("public class Foo { }");
    /// 
    ///     var (firstRun, secondRun) = await engine.RunTwiceAsync();
    /// 
    ///     // Verify caching: second run should have cached/unchanged steps
    ///     foreach (var step in secondRun.TrackedSteps)
    ///     {
    ///         var outputs = step.Value.SelectMany(s => s.Outputs);
    ///         Assert.All(outputs, o => Assert.True(
    ///             o.Reason == IncrementalStepRunReason.Cached ||
    ///             o.Reason == IncrementalStepRunReason.Unchanged));
    ///     }
    ///     </code>
    /// </example>
    /// <seealso cref="GeneratorResult{TGenerator}.IsCached" />
    /// <seealso cref="GeneratorCachingReport" />
    internal async Task<(GeneratorDriverRunResult FirstRun, GeneratorDriverRunResult SecondRun)> RunTwiceAsync(
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

    /// <summary>
    ///     Creates a compilation from all configured sources and references.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    ///     A task that resolves to a <see cref="CSharpCompilation" /> configured with
    ///     all sources, references, and nullable context enabled.
    /// </returns>
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

    /// <summary>
    ///     Creates a generator driver with all configured options.
    /// </summary>
    /// <returns>
    ///     A <see cref="GeneratorDriver" /> configured with additional texts,
    ///     analyzer config options, and step tracking enabled.
    /// </returns>
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

    /// <summary>
    ///     An in-memory implementation of <see cref="AdditionalText" /> for testing.
    /// </summary>
    /// <param name="path">The virtual file path.</param>
    /// <param name="text">The file content.</param>
    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(text, Encoding.UTF8);

        /// <summary>
        ///     Gets the virtual file path for this additional text.
        /// </summary>
        public override string Path { get; } = path;

        /// <summary>
        ///     Gets the source text content.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation (unused).</param>
        /// <returns>The source text content.</returns>
        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return _text;
        }
    }
}