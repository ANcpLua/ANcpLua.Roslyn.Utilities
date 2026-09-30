using System.Globalization;
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

#pragma warning disable RS2008 // release tracking does not apply to a test-only descriptor
    private static readonly DiagnosticDescriptor Descriptor = new(
        "TST001",
        "Test title",
        "{0}",
        "Tests",
        DiagnosticSeverity.Warning,
        true);
#pragma warning restore RS2008

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
        var unrelatedShortTree = CSharpSyntaxTree.ParseText("//", cancellationToken: TestContext.Current.CancellationToken);

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
        diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Be("message");
    }

    [Fact]
    public void ToDiagnostic_WithoutTree_ReportsPathBasedLocation()
    {
        var (_, node) = ParseWidget();
        var info = DiagnosticInfo.Create(Descriptor, node, "message");

        var diagnostic = info.ToDiagnostic();

        diagnostic.Location.IsInSource.Should().BeFalse();
        diagnostic.Location.GetLineSpan().Path.Should().Be("Widget.cs");
        diagnostic.Location.SourceSpan.Should().Be(node.Span);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Be("message");
    }

    private static (SyntaxTree Tree, ClassDeclarationSyntax Node) ParseWidget()
    {
        var tree = CSharpSyntaxTree.ParseText(Source, path: "Widget.cs");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        return (tree, node);
    }
}
