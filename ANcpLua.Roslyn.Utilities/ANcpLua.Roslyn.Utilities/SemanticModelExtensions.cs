using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for SemanticModel.
/// </summary>
public static class SemanticModelExtensions
{
    /// <summary>
    ///     Checks if a syntax node represents a constant value.
    /// </summary>
    public static bool IsConstant(this SemanticModel model, SyntaxNode node,
        CancellationToken cancellationToken = default)
    {
        return model.GetConstantValue(node, cancellationToken).HasValue;
    }

    /// <summary>
    ///     Checks if all syntax nodes represent constant values.
    /// </summary>
    public static bool AllConstant(this SemanticModel model, IEnumerable<SyntaxNode> nodes,
        CancellationToken cancellationToken = default)
    {
        foreach (var node in nodes)
            if (!model.GetConstantValue(node, cancellationToken).HasValue)
                return false;

        return true;
    }

    /// <summary>
    ///     Gets the constant value if present, otherwise default.
    /// </summary>
    public static T? GetConstantValueOrDefault<T>(this SemanticModel model, SyntaxNode node,
        CancellationToken cancellationToken = default)
    {
        var optional = model.GetConstantValue(node, cancellationToken);
        return optional is { HasValue: true, Value: T value } ? value : default;
    }

    /// <summary>
    ///     Checks if the expression evaluates to null constant.
    /// </summary>
    public static bool IsNullConstant(this SemanticModel model, SyntaxNode node,
        CancellationToken cancellationToken = default)
    {
        var optional = model.GetConstantValue(node, cancellationToken);
        return optional is { HasValue: true, Value: null };
    }
}