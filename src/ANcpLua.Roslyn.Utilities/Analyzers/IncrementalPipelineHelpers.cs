using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities.Analyzers;

/// <summary>
///     Shared helpers for <see cref="Microsoft.CodeAnalysis.IIncrementalGenerator" /> pipelines.
///     Syntactic helpers (<c>CouldBeInvocation</c>, <c>GetInvokedMethodName</c>) belong in the
///     <c>predicate</c> half of <c>CreateSyntaxProvider</c>; the remaining methods reach into
///     the semantic model and belong in the <c>transform</c> half.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class IncrementalPipelineHelpers
{
    /// <summary>
    ///     Fast syntactic pre-filter: is this syntax node an invocation expression?
    /// </summary>
    /// <remarks>
    ///     Safe to call from the <c>predicate</c> half of <c>CreateSyntaxProvider</c>.
    ///     Runs on every syntax node change, so it must stay cheap.
    /// </remarks>
    public static bool CouldBeInvocation(SyntaxNode node, CancellationToken _) =>
        node.IsKind(SyntaxKind.InvocationExpression);

    /// <summary>
    ///     Extracts the invoked method name directly from invocation syntax, without touching
    ///     the semantic model. Returns <see langword="null" /> when the invocation shape is
    ///     not a recognized member access, member binding, or identifier.
    /// </summary>
    public static string? GetInvokedMethodName(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return null;

        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax id } => id.Identifier.ValueText,
            MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } => generic.Identifier.ValueText,
            MemberBindingExpressionSyntax { Name: IdentifierNameSyntax id } => id.Identifier.ValueText,
            MemberBindingExpressionSyntax { Name: GenericNameSyntax generic } => generic.Identifier.ValueText,
            IdentifierNameSyntax id => id.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            _ => null
        };
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the file path ends with <c>.g.cs</c> or
    ///     <c>.generated.cs</c>, matching the conventional naming for generator output.
    /// </summary>
    public static bool IsGeneratedFile(string filePath) =>
        filePath.EndsWithIgnoreCase(".g.cs") ||
        filePath.EndsWithIgnoreCase(".generated.cs");

    /// <summary>
    ///     Attempts to resolve the syntax node in <paramref name="context" /> to an
    ///     <see cref="IInvocationOperation" />.
    /// </summary>
    public static bool TryGetInvocationOperation(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out IInvocationOperation? invocation)
    {
        if (context.SemanticModel.GetOperation(context.Node, cancellationToken)
            is IInvocationOperation op)
        {
            invocation = op;
            return true;
        }

        invocation = null;
        return false;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when another source generator has already registered an
    ///     interceptor for this invocation. Used to avoid double-interception.
    /// </summary>
    /// <seealso href="https://github.com/dotnet/roslyn/issues/72093" />
    public static bool IsAlreadyIntercepted(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Node is not InvocationExpressionSyntax invocationSyntax)
            return false;

        var interceptor = context.SemanticModel.GetInterceptorMethod(invocationSyntax, cancellationToken);
        return interceptor is not null;
    }

    /// <summary>
    ///     Formats a syntax node location as a deterministic sort key in the form
    ///     <c>{filePath}:{line}:{character}</c>. Useful for stable ordering of
    ///     pipeline output before hashing or emitting.
    /// </summary>
    public static string FormatSortKey(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        var start = span.StartLinePosition;
        return $"{node.SyntaxTree.FilePath}:{start.Line}:{start.Character}";
    }

    /// <summary>
    ///     Finds an attribute whose class matches <paramref name="fullMetadataName" />.
    ///     Resolves the target type via <see cref="Compilation.GetTypeByMetadataName(string)" />
    ///     and compares by symbol identity.
    /// </summary>
    public static AttributeData? FindAttributeByName(
        ImmutableArray<AttributeData> attributes,
        Compilation compilation,
        string fullMetadataName)
    {
        if (compilation.GetTypeByMetadataName(fullMetadataName) is not { } targetType)
            return null;

        return attributes.FirstOrDefault(attr => attr.AttributeClass.IsEqualTo(targetType));
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the method returns
    ///     <see cref="System.Threading.Tasks.Task" /> or <see cref="System.Threading.Tasks.ValueTask" />
    ///     (generic or non-generic).
    /// </summary>
    public static bool IsAsyncReturnType(IMethodSymbol method)
    {
        var returnTypeName = method.ReturnType.ToDisplayString();
        return returnTypeName.StartsWithOrdinal("System.Threading.Tasks.Task") ||
               returnTypeName.StartsWithOrdinal("System.Threading.Tasks.ValueTask");
    }
}
