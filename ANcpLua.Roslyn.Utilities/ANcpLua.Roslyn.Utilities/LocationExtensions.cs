using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for location and line span operations.
/// </summary>
public static class LocationExtensions
{
    /// <summary>
    ///     Gets the location in terms of path, line and column for a given token.
    /// </summary>
    public static FileLinePositionSpan? GetLineSpan(this SyntaxToken token,
        CancellationToken cancellationToken = default) =>
        token.SyntaxTree?.GetLineSpan(token.Span, cancellationToken);

    /// <summary>
    ///     Gets the location in terms of path, line and column for a given node.
    /// </summary>
    public static FileLinePositionSpan?
        GetLineSpan(this SyntaxNode node, CancellationToken cancellationToken = default) =>
        node.SyntaxTree.GetLineSpan(node.Span, cancellationToken);

    /// <summary>
    ///     Gets the location in terms of path, line and column for a given trivia.
    /// </summary>
    public static FileLinePositionSpan? GetLineSpan(this SyntaxTrivia trivia,
        CancellationToken cancellationToken = default) =>
        trivia.SyntaxTree?.GetLineSpan(trivia.Span, cancellationToken);

    /// <summary>
    ///     Gets the location in terms of path, line and column for a given node or token.
    /// </summary>
    public static FileLinePositionSpan? GetLineSpan(this SyntaxNodeOrToken nodeOrToken,
        CancellationToken cancellationToken = default) =>
        nodeOrToken.SyntaxTree?.GetLineSpan(nodeOrToken.Span, cancellationToken);

    /// <summary>
    ///     Gets the line on which the given token occurs.
    /// </summary>
    public static int? GetLine(this SyntaxToken token, CancellationToken cancellationToken = default) =>
        token.GetLineSpan(cancellationToken)?.StartLinePosition.Line;

    /// <summary>
    ///     Gets the line on which the given node occurs.
    /// </summary>
    public static int? GetLine(this SyntaxNode node, CancellationToken cancellationToken = default) =>
        node.GetLineSpan(cancellationToken)?.StartLinePosition.Line;

    /// <summary>
    ///     Gets the line on which the given trivia occurs.
    /// </summary>
    public static int? GetLine(this SyntaxTrivia trivia, CancellationToken cancellationToken = default) =>
        trivia.GetLineSpan(cancellationToken)?.StartLinePosition.Line;

    /// <summary>
    ///     Gets the end line of the given token.
    /// </summary>
    public static int? GetEndLine(this SyntaxToken token, CancellationToken cancellationToken = default) =>
        token.GetLineSpan(cancellationToken)?.EndLinePosition.Line;

    /// <summary>
    ///     Gets the end line of the given node.
    /// </summary>
    public static int? GetEndLine(this SyntaxNode node, CancellationToken cancellationToken = default) =>
        node.GetLineSpan(cancellationToken)?.EndLinePosition.Line;

    /// <summary>
    ///     Gets the end line of the given trivia.
    /// </summary>
    public static int? GetEndLine(this SyntaxTrivia trivia, CancellationToken cancellationToken = default) =>
        trivia.GetLineSpan(cancellationToken)?.EndLinePosition.Line;

    /// <summary>
    ///     Checks if the given node spans multiple source text lines.
    /// </summary>
    public static bool SpansMultipleLines(this SyntaxNode node, CancellationToken cancellationToken = default)
    {
        var lineSpan = node.GetLineSpan(cancellationToken);
        return lineSpan is not null && lineSpan.Value.StartLinePosition.Line < lineSpan.Value.EndLinePosition.Line;
    }

    /// <summary>
    ///     Checks if the given trivia spans multiple source text lines.
    /// </summary>
    public static bool SpansMultipleLines(this SyntaxTrivia trivia, CancellationToken cancellationToken = default)
    {
        var lineSpan = trivia.GetLineSpan(cancellationToken);
        return lineSpan is not null && lineSpan.Value.StartLinePosition.Line < lineSpan.Value.EndLinePosition.Line;
    }
}
