using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for <see cref="SemanticModel" /> to simplify constant value analysis.
/// </summary>
/// <remarks>
///     <para>
///         These extensions wrap common patterns for working with constant expressions in Roslyn,
///         providing a more ergonomic API for analyzer and generator authors.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Check if expressions are compile-time constants</description>
///         </item>
///         <item>
///             <description>Extract typed constant values with null-safety</description>
///         </item>
///         <item>
///             <description>Detect null literal expressions</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="SemanticModel.GetConstantValue(SyntaxNode, CancellationToken)" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SemanticModelExtensions
{
    /// <summary>
    ///     Checks if a syntax node represents a compile-time constant value.
    /// </summary>
    /// <param name="model">The semantic model to use for analysis.</param>
    /// <param name="node">The syntax node to evaluate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="node" /> represents a compile-time constant;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AllConstant" />
    /// <seealso cref="GetConstantValueOrDefault{T}" />
    public static bool IsConstant(this SemanticModel model, SyntaxNode node,
        CancellationToken cancellationToken = default) =>
        model.GetConstantValue(node, cancellationToken).HasValue;

    /// <summary>
    ///     Checks if all syntax nodes in a collection represent compile-time constant values.
    /// </summary>
    /// <param name="model">The semantic model to use for analysis.</param>
    /// <param name="nodes">The collection of syntax nodes to evaluate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if all nodes in <paramref name="nodes" /> represent compile-time constants;
    ///     <c>false</c> if any node is not a constant or if the collection is empty.
    /// </returns>
    /// <remarks>
    ///     This method uses short-circuit evaluation and returns <c>false</c> as soon as
    ///     a non-constant node is encountered.
    /// </remarks>
    /// <seealso cref="IsConstant" />
    public static bool AllConstant(this SemanticModel model, IEnumerable<SyntaxNode> nodes,
        CancellationToken cancellationToken = default)
    {
        foreach (var node in nodes)
            if (!model.GetConstantValue(node, cancellationToken).HasValue)
                return false;

        return true;
    }

    /// <summary>
    ///     Gets the constant value of a syntax node if present, otherwise returns the default value for the type.
    /// </summary>
    /// <typeparam name="T">The expected type of the constant value.</typeparam>
    /// <param name="model">The semantic model to use for analysis.</param>
    /// <param name="node">The syntax node to evaluate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The constant value of type <typeparamref name="T" /> if <paramref name="node" /> represents
    ///     a compile-time constant of that type; otherwise, <c>default</c>.
    /// </returns>
    /// <remarks>
    ///     Returns <c>default</c> if:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>The node is not a constant expression</description>
    ///         </item>
    ///         <item>
    ///             <description>The constant value cannot be cast to <typeparamref name="T" /></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="IsConstant" />
    /// <seealso cref="IsNullConstant" />
    public static T? GetConstantValueOrDefault<T>(this SemanticModel model, SyntaxNode node,
        CancellationToken cancellationToken = default)
    {
        var optional = model.GetConstantValue(node, cancellationToken);
        return optional is { HasValue: true, Value: T value } ? value : default;
    }

    /// <summary>
    ///     Checks if the expression evaluates to a <c>null</c> constant.
    /// </summary>
    /// <param name="model">The semantic model to use for analysis.</param>
    /// <param name="node">The syntax node to evaluate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="node" /> represents a compile-time <c>null</c> constant;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This method returns <c>true</c> only when the node is a constant expression
    ///     with a <c>null</c> value, such as the <c>null</c> literal or a <c>default</c>
    ///     expression for a reference type.
    /// </remarks>
    /// <seealso cref="IsConstant" />
    /// <seealso cref="GetConstantValueOrDefault{T}" />
    public static bool IsNullConstant(this SemanticModel model, SyntaxNode node,
        CancellationToken cancellationToken = default)
    {
        var optional = model.GetConstantValue(node, cancellationToken);
        return optional is { HasValue: true, Value: null };
    }
}
