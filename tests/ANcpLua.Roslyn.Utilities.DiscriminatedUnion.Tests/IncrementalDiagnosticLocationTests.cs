using ANcpLua.Roslyn.Utilities.Testing.GeneratorHelpers;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ANcpLua.Analyzers.DiscriminatedUnion.Tests;

/// <summary>
///     Regression guard for the bug class fixed by dotnet/roslyn#82113 (issue #82032): a generator
///     diagnostic whose location was captured against a longer tree must not survive a shrinking
///     incremental edit as an out-of-range span.
/// </summary>
/// <remarks>
///     Our diagnostics flow through <c>DiagnosticInfo</c>/<c>LocationInfo</c>, which reconstruct an
///     <em>external-file</em> location (never source-tree-bound) at report time. The driver never
///     range-validates external locations, so a shrinking edit can neither crash the generator nor
///     emit a stale span. This test fails if that guarantee regresses — e.g. if <c>ToLocation()</c>
///     ever produced a source-tree-bound location.
/// </remarks>
public sealed class IncrementalDiagnosticLocationTests
{
    [Fact]
    public void Shrinking_Edit_Keeps_Diagnostic_Location_In_Range_Without_Crashing()
    {
        // A union root with no cases reports AL0301 at the whole record declaration. The padded
        // namespace/identifier pushes that span far past the length of the shrunk tree below.
        const string longSource = """
                                  using ANcpLua.Analyzers.DiscriminatedUnion;

                                  namespace NamespacePaddingToPushTheDiagnosticSpanFarPastTheShrunkTree
                                  {
                                      [DiscriminatedUnion]
                                      public partial record RootWithNoCasesAndADeliberatelyLongIdentifier
                                      {
                                      }
                                  }
                                  """;

        // Still reports AL0301, but the tree is far shorter than the original diagnostic span.
        const string shortSource =
            "using ANcpLua.Analyzers.DiscriminatedUnion;[DiscriminatedUnion]public partial record R{}";

        var compilation = GeneratorTestHelper.CreateCompilation(longSource);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new DiscriminatedUnionGenerator().AsSourceGenerator());

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var firstDiagnostic = driver.GetRunResult().Diagnostics.Single(d => d.Id == "AL0301");

        var oldTree = compilation.SyntaxTrees.Single();
        var newTree = CSharpSyntaxTree.ParseText(
            shortSource,
            (CSharpParseOptions)oldTree.Options,
            cancellationToken: TestContext.Current.CancellationToken);
        var shrunk = compilation.ReplaceSyntaxTree(oldTree, newTree);

        // Precondition: the original span genuinely would be out of range for the shrunk tree —
        // otherwise the edit would not exercise the bug class at all.
        firstDiagnostic.Location.SourceSpan.End.Should()
            .BeGreaterThan(newTree.Length, "the edit must shrink the tree below the original diagnostic span");

        driver = driver.RunGenerators(shrunk, TestContext.Current.CancellationToken);
        var afterEdit = driver.GetRunResult().Diagnostics;

        // No crash diagnostic (e.g. CS8785 "generator failed"): the only generator diagnostic is AL0301.
        afterEdit.Should().ContainSingle()
            .Which.Id.Should().Be("AL0301", "the generator must re-report AL0301 and must not crash on the edit");

        var second = afterEdit[0];
        second.Location.IsInSource.Should()
            .BeFalse("LocationInfo reconstructs an external-file location the driver never range-validates");
        second.Location.SourceSpan.End.Should()
            .BeLessThanOrEqualTo(newTree.Length, "the reported location must stay within the current tree after the edit");
    }
}
