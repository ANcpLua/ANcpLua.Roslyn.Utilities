using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class LocationExtensionsTests
{
    [Fact]
    public void GetLineSpan_ForDetachedSyntaxNode_ReturnsNull()
    {
        var detachedNode = SyntaxFactory.IdentifierName("value");

        var cancellationToken = TestContext.Current.CancellationToken;

        detachedNode.GetLineSpan(cancellationToken).Should().BeNull();
        detachedNode.GetLine(cancellationToken).Should().BeNull();
        detachedNode.GetEndLine(cancellationToken).Should().BeNull();
        detachedNode.SpansMultipleLines(cancellationToken).Should().BeFalse();
    }

    [Fact]
    public void GetLineSpan_ForDetachedSyntaxToken_ReturnsNull()
    {
        var detachedToken = SyntaxFactory.Identifier("value");

        var cancellationToken = TestContext.Current.CancellationToken;

        detachedToken.GetLineSpan(cancellationToken).Should().BeNull();
        detachedToken.GetLine(cancellationToken).Should().BeNull();
        detachedToken.GetEndLine(cancellationToken).Should().BeNull();
    }

    [Fact]
    public void GetLineSpan_ForAttachedNode_StillReturnsLinePositions()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var tree = CSharpSyntaxTree.ParseText(
            "\nnamespace Probe { class A { } }\n",
            cancellationToken: cancellationToken);
        var node = tree.GetRoot(cancellationToken).DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax>().Single();

        node.GetLineSpan(cancellationToken).Should().NotBeNull();
        node.GetLine(cancellationToken).Should().Be(1);
        node.GetEndLine(cancellationToken).Should().Be(1);
        node.SpansMultipleLines(cancellationToken).Should().BeFalse();
    }
}
