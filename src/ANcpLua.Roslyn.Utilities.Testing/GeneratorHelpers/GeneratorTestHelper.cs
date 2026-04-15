// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — GeneratorTestHelper.cs
//
// Modifications from upstream:
// - Generic: RunGenerator<TGenerator>(...) so the helper is reusable across any
//   IIncrementalGenerator, not just the upstream ExecutorRouteGenerator.
// - AwesomeAssertions replaces FluentAssertions (repo standard).

using System.Collections.Immutable;
using System.Reflection;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities.Testing.GeneratorHelpers;

/// <summary>
/// Helpers for testing <see cref="IIncrementalGenerator"/>-based source generators.
/// <see cref="RunGenerator{TGenerator}(string)"/> returns the driver run result, the output
/// compilation, and the generator diagnostics as a single record.
/// </summary>
/// <remarks>
/// This is a lightweight, assertion-oriented helper that complements the in-tree
/// <c>GeneratorTestEngine</c> fluent builder. Use <c>GeneratorTestHelper</c> for small single-source
/// assertions where a one-liner is cleaner than a full fluent test. Use <c>GeneratorTestEngine</c>
/// when you need caching reports, forbidden-type analysis, references, or multi-run verification.
/// </remarks>
public static class GeneratorTestHelper
{
    /// <summary>Run <typeparamref name="TGenerator"/> over a single source file.</summary>
    public static GeneratorRunResult RunGenerator<TGenerator>(string source)
        where TGenerator : IIncrementalGenerator, new()
        => RunGenerator<TGenerator>([source]);

    /// <summary>Run <typeparamref name="TGenerator"/> across multiple source files.</summary>
    public static GeneratorRunResult RunGenerator<TGenerator>(params string[] sources)
        where TGenerator : IIncrementalGenerator, new()
    {
        var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();
        var references = GetBaseReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new TGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return new GeneratorRunResult(driver.GetRunResult(), outputCompilation, diagnostics);
    }

    /// <summary>Assert that <typeparamref name="TGenerator"/> generates exactly one file containing <paramref name="expectedGeneratedSource"/>.</summary>
    public static void AssertGeneratesSource<TGenerator>(string source, string expectedGeneratedSource)
        where TGenerator : IIncrementalGenerator, new()
    {
        var result = RunGenerator<TGenerator>(source);
        result.RunResult.GeneratedTrees.Should().HaveCount(1, "expected exactly one generated file");
        result.RunResult.GeneratedTrees[0].ToString().Should().Contain(expectedGeneratedSource);
    }

    /// <summary>Assert that <typeparamref name="TGenerator"/> emits no generated files for the given source.</summary>
    public static void AssertGeneratesNoSource<TGenerator>(string source)
        where TGenerator : IIncrementalGenerator, new()
        => RunGenerator<TGenerator>(source).RunResult.GeneratedTrees.Should().BeEmpty("expected no generated files");

    /// <summary>Assert that <typeparamref name="TGenerator"/> produces a diagnostic with the given ID.</summary>
    public static void AssertProducesDiagnostic<TGenerator>(string source, string diagnosticId)
        where TGenerator : IIncrementalGenerator, new()
    {
        var result = RunGenerator<TGenerator>(source);
        result.RunResult.Diagnostics.Should().Contain(d => d.Id == diagnosticId, $"expected diagnostic {diagnosticId} to be produced");
    }

    /// <summary>Assert that the output compilation (source + generator output) has no errors.</summary>
    public static void AssertCompilationSucceeds<TGenerator>(string source)
        where TGenerator : IIncrementalGenerator, new()
    {
        var result = RunGenerator<TGenerator>(source);
        var errors = result.OutputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        errors.Should().BeEmpty("compilation should succeed without errors");
    }

    private static ImmutableArray<MetadataReference> GetBaseReferences()
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.ISet<>).Assembly.Location),
        };

        var netstandardAssembly = Assembly.Load("netstandard, Version=2.0.0.0");
        references.Add(MetadataReference.CreateFromFile(netstandardAssembly.Location));

        var runtimeAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var systemRuntimePath = Path.Combine(runtimeAssemblyPath, "System.Runtime.dll");
        if (File.Exists(systemRuntimePath))
        {
            references.Add(MetadataReference.CreateFromFile(systemRuntimePath));
        }

        return [.. references.Distinct()];
    }
}

/// <summary>Result of a single generator run: driver result, output compilation, and diagnostics.</summary>
public sealed record GeneratorRunResult(
    GeneratorDriverRunResult RunResult,
    Compilation OutputCompilation,
    ImmutableArray<Diagnostic> Diagnostics);
