using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ANcpLua.Roslyn.Utilities.CodeFixes;

/// <summary>
/// Base class for code fixes that transform a specific syntax node type.
/// </summary>
/// <typeparam name="TSyntax">The syntax node type to transform.</typeparam>
public abstract class CodeFixProviderBase<TSyntax> : CodeFixProvider
    where TSyntax : SyntaxNode
{
    /// <summary>Code fix title shown to user.</summary>
    protected abstract string Title { get; }

    /// <summary>Diagnostic IDs this provider fixes.</summary>
    public abstract override ImmutableArray<string> FixableDiagnosticIds { get; }

    /// <summary>Transform the syntax node. Return null to skip the fix.</summary>
    protected abstract TSyntax? Transform(
        TSyntax node,
        SemanticModel semanticModel,
        Diagnostic diagnostic,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<TSyntax>();
        if (node is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: ct => ApplyFixAsync(context.Document, root, node, diagnostic, ct),
                equivalenceKey: Title),
            diagnostic);
    }

    private async Task<Document> ApplyFixAsync(
        Document document,
        SyntaxNode root,
        TSyntax node,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null) return document;

        var newNode = Transform(node, semanticModel, diagnostic, cancellationToken);
        if (newNode is null || ReferenceEquals(newNode, node)) return document;

        var newRoot = root.ReplaceNode(node, newNode);
        return document.WithSyntaxRoot(newRoot);
    }
}