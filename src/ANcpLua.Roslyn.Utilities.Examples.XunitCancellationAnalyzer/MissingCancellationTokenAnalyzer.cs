using System.Collections.Immutable;
using System.Linq;
using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities.Examples.XunitCancellationAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingCancellationTokenAnalyzer : DiagnosticAnalyzerBase
{
    public const string DiagnosticId = "ANCP0001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Pass the xUnit cancellation token",
        messageFormat: "Call '{0}' with TestContext.Current.CancellationToken",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Calls from xUnit test methods should pass TestContext.Current.CancellationToken when a single CancellationToken parameter is omitted or defaulted.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Rule];

    protected override void InitializeCore(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.ContainingSymbol is not IMethodSymbol method || !IsXunitTestMethod(method))
            return;

        var invocation = (IInvocationOperation)context.Operation;
        var cancellationTokenType = context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        if (cancellationTokenType is null)
            return;

        var expressionType = context.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
        if (invocation.IsInExpressionTree(expressionType))
            return;

        var cancellationTokenParameters = invocation.TargetMethod.Parameters
            .Where(parameter => parameter.Type.IsEqualTo(cancellationTokenType))
            .ToImmutableArray();

        if (cancellationTokenParameters.Length != 1)
            return;

        var parameter = cancellationTokenParameters[0];
        if (parameter.RefKind is not RefKind.None)
            return;

        var existingArgument = invocation.GetArgument(parameter.Ordinal);
        if (existingArgument is not null && !IsDefaultLike(existingArgument.Value, cancellationTokenType))
            return;

        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(MissingCancellationTokenDiagnosticProperties.ParameterName, parameter.Name)
            .Add(
                MissingCancellationTokenDiagnosticProperties.ParameterIndex,
                parameter.Ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture));

        context.ReportDiagnostic(
            Diagnostic.Create(
                descriptor: Rule,
                location: invocation.Syntax.GetLocation(),
                properties: properties,
                messageArgs: invocation.TargetMethod.Name));
    }

    private static bool IsXunitTestMethod(IMethodSymbol method)
    {
        return method.HasAttribute("Xunit.FactAttribute")
               || method.HasAttribute("Xunit.TheoryAttribute");
    }

    private static bool IsDefaultLike(IOperation operation, ITypeSymbol cancellationTokenType)
    {
        operation = operation.UnwrapAllConversions().UnwrapParenthesized();

        if (operation.Syntax.IsKind(SyntaxKind.DefaultExpression)
            || operation.Syntax.IsKind(SyntaxKind.DefaultLiteralExpression))
            return true;

        return operation is IPropertyReferenceOperation
               {
                   Property: { Name: "None", ContainingType: { } containingType }
               }
               && containingType.IsEqualTo(cancellationTokenType);
    }
}

internal static class MissingCancellationTokenDiagnosticProperties
{
    public const string ParameterName = nameof(ParameterName);
    public const string ParameterIndex = nameof(ParameterIndex);
}
