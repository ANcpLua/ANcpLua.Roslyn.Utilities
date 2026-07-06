using System.Linq;
using ANcpLua.Roslyn.Utilities.Models;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class TreeBoundLocationTests
{
    private const string Source = """
namespace Sample;

public class Widget
{
}
""";

    private static readonly DiagnosticDescriptor Descriptor = new(
        "TST001",
        "Test title",
        "{0}",
        "Tests",
        DiagnosticSeverity.Warning,
        true);

    [Fact]
    public void ToLocation_WithOriginTree_ReturnsTreeBoundLocation()
    {
        var (tree, node) = ParseWidget();
        var info = LocationInfo.From(node);

        var location = info.ToLocation(tree);

        location.IsInSource.Should().BeTrue();
        location.SourceTree.Should().BeSameAs(tree);
        location.SourceSpan.Should().Be(node.Span);
    }

    [Fact]
    public void ToLocation_WithNullTree_ReturnsPathBasedLocation()
    {
        var (_, node) = ParseWidget();
        var info = LocationInfo.From(node);

        var location = info.ToLocation(null);

        location.IsInSource.Should().BeFalse();
        location.GetLineSpan().Path.Should().Be("Widget.cs");
        location.SourceSpan.Should().Be(node.Span);
    }

    [Fact]
    public void ToLocation_WithTreeTooShortForSpan_FallsBackToPathBasedLocation()
    {
        var (_, node) = ParseWidget();
        var info = LocationInfo.From(node);
        var unrelatedShortTree = CSharpSyntaxTree.ParseText("//");

        var location = info.ToLocation(unrelatedShortTree);

        location.IsInSource.Should().BeFalse();
        location.GetLineSpan().Path.Should().Be("Widget.cs");
    }

    [Fact]
    public void ToDiagnostic_WithTree_ReportsTreeBoundLocation()
    {
        var (tree, node) = ParseWidget();
        var info = DiagnosticInfo.Create(Descriptor, node, "message");

        var diagnostic = info.ToDiagnostic(tree);

        diagnostic.Location.IsInSource.Should().BeTrue();
        diagnostic.Location.SourceTree.Should().BeSameAs(tree);
        diagnostic.GetMessage().Should().Be("message");
    }

    [Fact]
    public void ToDiagnostic_WithNullTree_MatchesParameterlessBehavior()
    {
        var (_, node) = ParseWidget();
        var info = DiagnosticInfo.Create(Descriptor, node, "message");

        var withNull = info.ToDiagnostic(null);
        var parameterless = info.ToDiagnostic();

        withNull.Location.IsInSource.Should().BeFalse();
        withNull.Location.GetLineSpan().Should().Be(parameterless.Location.GetLineSpan());
        withNull.GetMessage().Should().Be(parameterless.GetMessage());
    }

    private static (SyntaxTree Tree, ClassDeclarationSyntax Node) ParseWidget()
    {
        var tree = CSharpSyntaxTree.ParseText(Source, path: "Widget.cs");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        return (tree, node);
    }
}
