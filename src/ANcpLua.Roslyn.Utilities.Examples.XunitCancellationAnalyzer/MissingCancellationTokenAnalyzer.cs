using System.Collections.Immutable;
using System.Linq;
using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Analyzers;
using ANcpLua.Roslyn.Utilities.Examples.XunitCancellationShared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities.Examples.XunitCancellationAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingCancellationTokenAnalyzer : DiagnosticAnalyzerBase
{
    public const string DiagnosticId = MissingCancellationTokenContract.DiagnosticId;

    private static readonly DiagnosticDescriptor s_rule = new(
        id: DiagnosticId,
        title: "Pass the xUnit cancellation token",
        messageFormat: "Call '{0}' with TestContext.Current.CancellationToken",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Calls from xUnit test methods should pass TestContext.Current.CancellationToken when a single CancellationToken parameter is omitted or defaulted.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [s_rule];

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
            .Add(MissingCancellationTokenContract.ParameterNameProperty, parameter.Name)
            .Add(
                MissingCancellationTokenContract.ParameterIndexProperty,
                parameter.Ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture));

        context.ReportDiagnostic(
            Diagnostic.Create(
                descriptor: s_rule,
                location: invocation.Syntax.GetLocation(),
                properties: properties,
                messageArgs: invocation.TargetMethod.Name));
    }

    private static bool IsXunitTestMethod(IMethodSymbol method)
    {
        return method.HasAttribute("Xunit.FactAttribute")
               || method.HasAttribute("Xunit.TheoryAttribute");
    }

    private static bool IsDefaultLike(IOperation operation, ITypeSymbol cancellationTokenType) =>
        MissingCancellationTokenContract.IsDefaultCancellationTokenArgument(operation, cancellationTokenType);
}
