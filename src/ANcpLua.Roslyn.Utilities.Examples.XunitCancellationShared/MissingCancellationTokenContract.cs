using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Roslyn.Utilities.Examples.XunitCancellationShared;

public static class MissingCancellationTokenContract
{
    public const string DiagnosticId = "ANCP0001";
    public const string ParameterNameProperty = "ParameterName";
    public const string ParameterIndexProperty = "ParameterIndex";
    public const string ReplacementExpression = "TestContext.Current.CancellationToken";

    public static bool IsDefaultCancellationTokenArgument(IOperation operation, ITypeSymbol cancellationTokenType)
    {
        if (operation is null)
            return false;

        operation = operation.UnwrapAllConversions().UnwrapParenthesized();

        if (operation.Syntax.IsKind(SyntaxKind.DefaultExpression)
            || operation.Syntax.IsKind(SyntaxKind.DefaultLiteralExpression))
            return true;

        return operation is IPropertyReferenceOperation
        {
            Property: { Name: nameof(CancellationToken.None), ContainingType: { } containingType }
        } && containingType.IsEqualTo(cancellationTokenType);
    }

    public static ExpressionSyntax CreateReplacementTokenExpression() =>
        SyntaxFactory.ParseExpression(ReplacementExpression);

}
