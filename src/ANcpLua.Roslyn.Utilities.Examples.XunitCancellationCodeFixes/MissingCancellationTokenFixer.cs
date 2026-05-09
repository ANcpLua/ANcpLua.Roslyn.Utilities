using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ANcpLua.Roslyn.Utilities.Examples.XunitCancellationAnalyzer;
using ANcpLua.Roslyn.Utilities.Examples.XunitCancellationShared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ANcpLua.Roslyn.Utilities.Examples.XunitCancellationCodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MissingCancellationTokenFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [MissingCancellationTokenAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnostic = context.Diagnostics[0];
        if (root.FindNode(diagnostic.Location.SourceSpan) is not InvocationExpressionSyntax invocation)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add TestContext.Current.CancellationToken",
                createChangedDocument: cancellationToken => ApplyAsync(context.Document, invocation, diagnostic, cancellationToken),
                equivalenceKey: nameof(MissingCancellationTokenFixer)),
            diagnostic);
    }

    private static async Task<Document> ApplyAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        if (!diagnostic.Properties.TryGetValue(MissingCancellationTokenContract.ParameterNameProperty, out var parameterName)
            || string.IsNullOrWhiteSpace(parameterName)
            || !diagnostic.Properties.TryGetValue(MissingCancellationTokenContract.ParameterIndexProperty, out var parameterIndexText)
            || !int.TryParse(parameterIndexText, out var parameterIndex))
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var arguments = invocation.ArgumentList.Arguments.ToList();
        var tokenExpression = MissingCancellationTokenContract.CreateReplacementTokenExpression();
        var safeParameterName = parameterName ?? string.Empty;

        var argumentIndex = arguments.FindIndex(argument =>
            argument.NameColon?.Name.Identifier.ValueText == safeParameterName);

        if (argumentIndex >= 0)
        {
            arguments[argumentIndex] = arguments[argumentIndex].WithExpression(tokenExpression);
        }
        else if (parameterIndex < arguments.Count)
        {
            arguments[parameterIndex] = arguments[parameterIndex].WithExpression(tokenExpression);
        }
        else
        {
            var argument = Argument(tokenExpression);

            if (parameterIndex > arguments.Count || arguments.Any(existingArgument => existingArgument.NameColon is not null))
                argument = argument.WithNameColon(NameColon(IdentifierName(safeParameterName)));

            arguments.Add(argument);
        }

        editor.ReplaceNode(
            invocation,
            invocation.WithArgumentList(ArgumentList(SeparatedList(arguments))));

        return editor.GetChangedDocument();
    }

}
