using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for retrieving location and line span information from syntax elements.
/// </summary>
/// <remarks>
///     <para>
///         This class contains helper methods that simplify working with source code locations in Roslyn.
///         All methods support cancellation and return nullable values when the syntax tree is unavailable.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Line span methods return <see cref="FileLinePositionSpan" /> with file path, line, and column
///                 information.
///             </description>
///         </item>
///         <item>
///             <description>Line number methods return zero-based line indices.</description>
///         </item>
///         <item>
///             <description>Multi-line detection methods check if elements span across multiple source lines.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="FileLinePositionSpan" />
/// <seealso cref="SyntaxToken" />
/// <seealso cref="SyntaxNode" />
/// <seealso cref="SyntaxTrivia" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class LocationExtensions
{
    /// <summary>
    ///     Gets the file line position span for the specified syntax token.
    /// </summary>
    /// <param name="token">The syntax token to get the line span for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     A <see cref="FileLinePositionSpan" /> containing the file path, start line/column, and end line/column
    ///     for the <paramref name="token" />; or <c>null</c> if the token has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetLine(SyntaxToken, CancellationToken)" />
    /// <seealso cref="GetEndLine(SyntaxToken, CancellationToken)" />
    public static FileLinePositionSpan? GetLineSpan(this SyntaxToken token,
        CancellationToken cancellationToken = default)
    {
        return token.SyntaxTree?.GetLineSpan(token.Span, cancellationToken);
    }

    /// <summary>
    ///     Gets the file line position span for the specified syntax node.
    /// </summary>
    /// <param name="node">The syntax node to get the line span for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     A <see cref="FileLinePositionSpan" /> containing the file path, start line/column, and end line/column
    ///     for the <paramref name="node" />; or <c>null</c> if the node has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetLine(SyntaxNode, CancellationToken)" />
    /// <seealso cref="GetEndLine(SyntaxNode, CancellationToken)" />
    /// <seealso cref="SpansMultipleLines(SyntaxNode, CancellationToken)" />
    public static FileLinePositionSpan?
        GetLineSpan(this SyntaxNode node, CancellationToken cancellationToken = default)
    {
        return node.SyntaxTree.GetLineSpan(node.Span, cancellationToken);
    }

    /// <summary>
    ///     Gets the file line position span for the specified syntax trivia.
    /// </summary>
    /// <param name="trivia">The syntax trivia to get the line span for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     A <see cref="FileLinePositionSpan" /> containing the file path, start line/column, and end line/column
    ///     for the <paramref name="trivia" />; or <c>null</c> if the trivia has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetLine(SyntaxTrivia, CancellationToken)" />
    /// <seealso cref="GetEndLine(SyntaxTrivia, CancellationToken)" />
    /// <seealso cref="SpansMultipleLines(SyntaxTrivia, CancellationToken)" />
    public static FileLinePositionSpan? GetLineSpan(this SyntaxTrivia trivia,
        CancellationToken cancellationToken = default)
    {
        return trivia.SyntaxTree?.GetLineSpan(trivia.Span, cancellationToken);
    }

    /// <summary>
    ///     Gets the file line position span for the specified syntax node or token.
    /// </summary>
    /// <param name="nodeOrToken">The syntax node or token to get the line span for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     A <see cref="FileLinePositionSpan" /> containing the file path, start line/column, and end line/column
    ///     for the <paramref name="nodeOrToken" />; or <c>null</c> if there is no associated syntax tree.
    /// </returns>
    public static FileLinePositionSpan? GetLineSpan(this SyntaxNodeOrToken nodeOrToken,
        CancellationToken cancellationToken = default)
    {
        return nodeOrToken.SyntaxTree?.GetLineSpan(nodeOrToken.Span, cancellationToken);
    }

    /// <summary>
    ///     Gets the zero-based starting line number where the specified token occurs.
    /// </summary>
    /// <param name="token">The syntax token to get the line number for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     The zero-based line number where the <paramref name="token" /> starts;
    ///     or <c>null</c> if the token has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetEndLine(SyntaxToken, CancellationToken)" />
    /// <seealso cref="GetLineSpan(SyntaxToken, CancellationToken)" />
    public static int? GetLine(this SyntaxToken token, CancellationToken cancellationToken = default)
    {
        return token.GetLineSpan(cancellationToken)?.StartLinePosition.Line;
    }

    /// <summary>
    ///     Gets the zero-based starting line number where the specified node occurs.
    /// </summary>
    /// <param name="node">The syntax node to get the line number for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     The zero-based line number where the <paramref name="node" /> starts;
    ///     or <c>null</c> if the node has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetEndLine(SyntaxNode, CancellationToken)" />
    /// <seealso cref="GetLineSpan(SyntaxNode, CancellationToken)" />
    public static int? GetLine(this SyntaxNode node, CancellationToken cancellationToken = default)
    {
        return node.GetLineSpan(cancellationToken)?.StartLinePosition.Line;
    }

    /// <summary>
    ///     Gets the zero-based starting line number where the specified trivia occurs.
    /// </summary>
    /// <param name="trivia">The syntax trivia to get the line number for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     The zero-based line number where the <paramref name="trivia" /> starts;
    ///     or <c>null</c> if the trivia has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetEndLine(SyntaxTrivia, CancellationToken)" />
    /// <seealso cref="GetLineSpan(SyntaxTrivia, CancellationToken)" />
    public static int? GetLine(this SyntaxTrivia trivia, CancellationToken cancellationToken = default)
    {
        return trivia.GetLineSpan(cancellationToken)?.StartLinePosition.Line;
    }

    /// <summary>
    ///     Gets the zero-based ending line number where the specified token ends.
    /// </summary>
    /// <param name="token">The syntax token to get the ending line number for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     The zero-based line number where the <paramref name="token" /> ends;
    ///     or <c>null</c> if the token has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetLine(SyntaxToken, CancellationToken)" />
    /// <seealso cref="GetLineSpan(SyntaxToken, CancellationToken)" />
    public static int? GetEndLine(this SyntaxToken token, CancellationToken cancellationToken = default)
    {
        return token.GetLineSpan(cancellationToken)?.EndLinePosition.Line;
    }

    /// <summary>
    ///     Gets the zero-based ending line number where the specified node ends.
    /// </summary>
    /// <param name="node">The syntax node to get the ending line number for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     The zero-based line number where the <paramref name="node" /> ends;
    ///     or <c>null</c> if the node has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetLine(SyntaxNode, CancellationToken)" />
    /// <seealso cref="GetLineSpan(SyntaxNode, CancellationToken)" />
    public static int? GetEndLine(this SyntaxNode node, CancellationToken cancellationToken = default)
    {
        return node.GetLineSpan(cancellationToken)?.EndLinePosition.Line;
    }

    /// <summary>
    ///     Gets the zero-based ending line number where the specified trivia ends.
    /// </summary>
    /// <param name="trivia">The syntax trivia to get the ending line number for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     The zero-based line number where the <paramref name="trivia" /> ends;
    ///     or <c>null</c> if the trivia has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetLine(SyntaxTrivia, CancellationToken)" />
    /// <seealso cref="GetLineSpan(SyntaxTrivia, CancellationToken)" />
    public static int? GetEndLine(this SyntaxTrivia trivia, CancellationToken cancellationToken = default)
    {
        return trivia.GetLineSpan(cancellationToken)?.EndLinePosition.Line;
    }

    /// <summary>
    ///     Determines whether the specified syntax node spans multiple source text lines.
    /// </summary>
    /// <param name="node">The syntax node to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     <c>true</c> if the <paramref name="node" /> starts on a different line than it ends;
    ///     otherwise, <c>false</c>. Returns <c>false</c> if the node has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetLine(SyntaxNode, CancellationToken)" />
    /// <seealso cref="GetEndLine(SyntaxNode, CancellationToken)" />
    public static bool SpansMultipleLines(this SyntaxNode node, CancellationToken cancellationToken = default)
    {
        var lineSpan = node.GetLineSpan(cancellationToken);
        return lineSpan is not null && lineSpan.Value.StartLinePosition.Line < lineSpan.Value.EndLinePosition.Line;
    }

    /// <summary>
    ///     Determines whether the specified syntax trivia spans multiple source text lines.
    /// </summary>
    /// <param name="trivia">The syntax trivia to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     <c>true</c> if the <paramref name="trivia" /> starts on a different line than it ends;
    ///     otherwise, <c>false</c>. Returns <c>false</c> if the trivia has no associated syntax tree.
    /// </returns>
    /// <seealso cref="GetLine(SyntaxTrivia, CancellationToken)" />
    /// <seealso cref="GetEndLine(SyntaxTrivia, CancellationToken)" />
    public static bool SpansMultipleLines(this SyntaxTrivia trivia, CancellationToken cancellationToken = default)
    {
        var lineSpan = trivia.GetLineSpan(cancellationToken);
        return lineSpan is not null && lineSpan.Value.StartLinePosition.Line < lineSpan.Value.EndLinePosition.Line;
    }
}