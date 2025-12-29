using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Validates <see cref="SyntaxTree" /> identity across generator runs to ensure proper caching.
/// </summary>
/// <remarks>
///     <para>
///         Compares two post-generation <see cref="Compilation" /> instances to verify that unchanged
///         generated trees are reused (reference equality) rather than recreated. This is critical
///         for IDE performance where generators run frequently as the user types.
///     </para>
///     <para>
///         Use <see cref="ShouldHaveCached" /> to assert specific files were cached, or
///         <see cref="ShouldHaveRegenerated" /> to verify files were properly regenerated after changes.
///         Access <see cref="RunResult" /> for detailed pipeline step analysis.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// await """
///     [GenerateBuilder]
///     public class Person { }
/// """.ShouldCacheWithCompilationUpdate&lt;BuilderGenerator&gt;(
///     compilation => compilation.WithOptions(
///         compilation.Options.WithOptimizationLevel(OptimizationLevel.Release)),
///     result =>
///     {
///         result.ShouldHaveCached("Person.Builder.g.cs");
///         result.SecondDiagnostics.Should().BeEmpty();
///     });
/// </code>
/// </example>
/// <seealso cref="GeneratorCachingReport" />
public class CompilationCacheResult
{
    private readonly List<SyntaxTree> _firstGeneratedTrees;
    private readonly List<SyntaxTree> _secondGeneratedTrees;

    /// <summary>
    ///     Initializes a new <see cref="CompilationCacheResult" />.
    /// </summary>
    /// <param name="first">First post-generation compilation (from <c>RunGeneratorsAndUpdateCompilation</c>).</param>
    /// <param name="second">Second post-generation compilation after the edit.</param>
    /// <param name="firstDiags">Diagnostics reported for the first compilation.</param>
    /// <param name="secondDiags">Diagnostics reported for the second compilation.</param>
    /// <param name="runResult">The run result captured after the second run.</param>
    public CompilationCacheResult(Compilation first, Compilation second, IEnumerable<Diagnostic> firstDiags,
        IEnumerable<Diagnostic> secondDiags, GeneratorDriverRunResult runResult)
    {
        FirstCompilation = first;
        SecondCompilation = second;
        FirstDiagnostics = firstDiags;
        SecondDiagnostics = secondDiags;
        RunResult = runResult;

        // Use the run result's hint names instead of guessing by ".g.cs".
        _firstGeneratedTrees = ExtractGeneratedTrees(first, runResult);
        _secondGeneratedTrees = ExtractGeneratedTrees(second, runResult);
    }

    /// <summary>
    ///     Gets the first post-generation compilation.
    /// </summary>
    /// <remarks>
    ///     This is the compilation after the generator ran on the original source code,
    ///     including both the original syntax trees and the generated ones.
    /// </remarks>
    public Compilation FirstCompilation { get; }

    /// <summary>
    ///     Gets the second post-generation compilation after applying the edit.
    /// </summary>
    /// <remarks>
    ///     This is the compilation after the generator ran on the modified source code,
    ///     including both the modified syntax trees and the (possibly cached) generated ones.
    /// </remarks>
    public Compilation SecondCompilation { get; }

    /// <summary>
    ///     Gets diagnostics produced during the first run (source + generated).
    /// </summary>
    public IEnumerable<Diagnostic> FirstDiagnostics { get; }

    /// <summary>
    ///     Gets diagnostics produced during the second run (source + generated).
    /// </summary>
    public IEnumerable<Diagnostic> SecondDiagnostics { get; }

    /// <summary>
    ///     Gets the aggregated run result for the second run.
    /// </summary>
    /// <remarks>
    ///     Includes tracked steps and generated sources, useful for detailed analysis
    ///     of caching behavior at the pipeline step level.
    /// </remarks>
    public GeneratorDriverRunResult RunResult { get; }

    /// <summary>
    ///     Validates that the generator produced output and that unchanged generated files
    ///     are cached (same <see cref="SyntaxTree" /> instances in both compilations).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method performs the following validations:
    ///         <list type="number">
    ///             <item>
    ///                 <description>Generator produced at least one output file</description>
    ///             </item>
    ///             <item>
    ///                 <description>All unchanged generated trees are the same instance (reference equality)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Note that this does NOT check for forbidden types. Use
    ///         <see cref="GeneratorTestExtensions.ShouldBeCached{TGenerator}(string,string[])" />
    ///         with explicit tracking names for comprehensive caching validation.
    ///     </para>
    /// </remarks>
    public void ValidateCaching()
    {
        using AssertionScope scope = new("Compilation-level caching");

        _firstGeneratedTrees.Should().NotBeEmpty("generator should produce output");

        foreach (var (first, second) in GetUnchangedTrees())
            ReferenceEquals(first, second).Should().BeTrue(
                $"unchanged tree '{GetHintName(first)}' should be cached (same instance)");
    }

    /// <summary>
    ///     Asserts that the specified hint names were cached (identity-equal across runs).
    /// </summary>
    /// <param name="hintNames">
    ///     Hint names to check (e.g., <c>Person.Builder.g.cs</c>).
    /// </param>
    /// <returns>The current <see cref="CompilationCacheResult" /> for further chaining.</returns>
    /// <example>
    ///     <code>
    /// result.ShouldHaveCached("Person.Builder.g.cs", "Person.Validator.g.cs");
    /// </code>
    /// </example>
    public CompilationCacheResult ShouldHaveCached(params string[] hintNames)
    {
        var unchanged =
            GetUnchangedTrees().Where(pair => hintNames.Contains(GetHintName(pair.First))).ToList();

        unchanged.Should().HaveCount(hintNames.Length, "all specified files should exist and be unchanged");

        foreach (var (first, second) in unchanged)
            ReferenceEquals(first, second).Should().BeTrue($"tree '{GetHintName(first)}' should be cached");

        return this;
    }

    /// <summary>
    ///     Asserts that the specified hint names were regenerated (distinct instances across runs).
    /// </summary>
    /// <param name="hintNames">Hint names to check.</param>
    /// <returns>The current <see cref="CompilationCacheResult" /> for further chaining.</returns>
    /// <remarks>
    ///     Use this to verify that changes to source code properly trigger regeneration
    ///     of affected output files.
    /// </remarks>
    /// <example>
    ///     <code>
    /// await "public class Person { }"
    ///     .ShouldCacheWithCompilationUpdate&lt;MyGenerator&gt;(
    ///         comp => comp.ReplaceSyntaxTree(
    ///             comp.SyntaxTrees.First(),
    ///             CSharpSyntaxTree.ParseText("public class Person { public string Name { get; set; } }")),
    ///         result => result.ShouldHaveRegenerated("Person.Builder.g.cs"));
    /// </code>
    /// </example>
    public CompilationCacheResult ShouldHaveRegenerated(params string[] hintNames)
    {
        var changed =
            GetChangedTrees().Where(pair => hintNames.Contains(GetHintName(pair.First))).ToList();

        changed.Should().HaveCount(hintNames.Length, "all specified files should exist and be changed");

        foreach (var (first, second) in changed)
            ReferenceEquals(first, second).Should().BeFalse($"tree '{GetHintName(first)}' should be regenerated");

        return this;
    }

    private List<(SyntaxTree First, SyntaxTree Second)> GetUnchangedTrees()
    {
        var secondByHint = new Dictionary<string, SyntaxTree>(StringComparer.Ordinal);
        foreach (var tree in _secondGeneratedTrees)
            secondByHint[GetHintName(tree)] = tree;

        List<(SyntaxTree First, SyntaxTree Second)> result = [];
        foreach (var first in _firstGeneratedTrees)
        {
            if (secondByHint.TryGetValue(GetHintName(first), out var second) &&
                ReferenceEquals(first, second))
            {
                result.Add((first, second));
            }
        }

        return result;
    }

    private List<(SyntaxTree First, SyntaxTree Second)> GetChangedTrees()
    {
        var secondByHint = new Dictionary<string, SyntaxTree>(StringComparer.Ordinal);
        foreach (var tree in _secondGeneratedTrees)
            secondByHint[GetHintName(tree)] = tree;

        List<(SyntaxTree First, SyntaxTree Second)> result = [];
        foreach (var first in _firstGeneratedTrees)
        {
            if (secondByHint.TryGetValue(GetHintName(first), out var second) &&
                !ReferenceEquals(first, second))
            {
                result.Add((first, second));
            }
        }

        return result;
    }

    private static List<SyntaxTree> ExtractGeneratedTrees(Compilation compilation, GeneratorDriverRunResult runResult)
    {
        var hintNames = runResult.Results.SelectMany(r => r.GeneratedSources).Select(gs => gs.HintName)
            .ToHashSet(StringComparer.Ordinal);

        return compilation.SyntaxTrees.Where(t => hintNames.Contains(Path.GetFileName(t.FilePath))).ToList();
    }

    private static string GetHintName(SyntaxTree tree)
    {
        return Path.GetFileName(tree.FilePath);
    }
}