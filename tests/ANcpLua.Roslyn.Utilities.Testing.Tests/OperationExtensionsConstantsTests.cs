using System.Linq;
using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class OperationExtensionsConstantsTests
{
    // Regression test for IsConstantZero: the C# pattern `Value: 0` alone matches int 0 only,
    // so the helper must enumerate every numeric-type zero (long, uint, ulong, float, double,
    // decimal) explicitly. This was broken between 2.2.5 and 2.2.20 and only resurfaced when
    // ANcpLua.Analyzers tried to bump — keep this test as a permanent guard.
    [Theory]
    [InlineData("0")]
    [InlineData("0L")]
    [InlineData("0u")]
    [InlineData("0uL")]
    [InlineData("0.0f")]
    [InlineData("0.0")]
    [InlineData("0m")]
    public void IsConstantZero_MatchesEveryBuiltinNumericZeroLiteral(string zeroLiteral)
    {
        GetLiteralOperation(zeroLiteral).IsConstantZero().Should().BeTrue();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1L")]
    [InlineData("1.0")]
    [InlineData("\"0\"")]
    public void IsConstantZero_RejectsNonZeroLiterals(string nonZeroLiteral)
    {
        GetLiteralOperation(nonZeroLiteral).IsConstantZero().Should().BeFalse();
    }

    private static IOperation GetLiteralOperation(string literalExpression)
    {
        var source = $$"""
            public class Subject
            {
                public object M() => {{literalExpression}};
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "ConstantsProbe",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var model = compilation.GetSemanticModel(tree);
        var returnExpression = tree.GetRoot()
            .DescendantNodes()
            .OfType<ArrowExpressionClauseSyntax>()
            .Single()
            .Expression;

        return model.GetOperation(returnExpression)
               ?? throw new InvalidOperationException("Expected an operation for the literal expression.");
    }
}
