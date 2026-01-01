using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities;

public static class OperationExtensions
{
    public static IEnumerable<IOperation> Ancestors(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            yield return parent;
            parent = parent.Parent;
        }
    }

    public static T? FindAncestor<T>(this IOperation operation) where T : class, IOperation
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is T result)
                return result;
            parent = parent.Parent;
        }

        return null;
    }

    public static bool IsDescendantOf<T>(this IOperation operation) where T : class, IOperation =>
        operation.FindAncestor<T>() is not null;

    public static bool IsInNameofOperation(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent.Kind is OperationKind.NameOf)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    public static bool IsInExpressionTree(this IOperation operation, INamedTypeSymbol? expressionSymbol)
    {
        if (expressionSymbol is null)
            return false;

        foreach (var op in operation.Ancestors())
        {
            if (op is IArgumentOperation { Parameter.Type: { } paramType } && paramType.InheritsFrom(expressionSymbol))
                return true;

            if (op is IConversionOperation { Type: { } convType } && convType.InheritsFrom(expressionSymbol))
                return true;
        }

        return false;
    }

    public static bool IsInStaticContext(this IOperation operation, CancellationToken cancellationToken)
    {
        foreach (var member in operation.Syntax.Ancestors())
        {
            if (member is LocalFunctionStatementSyntax localFunction)
            {
                var symbol = operation.SemanticModel?.GetDeclaredSymbol(localFunction, cancellationToken);
                if (symbol is { IsStatic: true })
                    return true;
            }
            else if (member is LambdaExpressionSyntax lambdaExpression)
            {
                var symbol = operation.SemanticModel?.GetSymbolInfo(lambdaExpression, cancellationToken).Symbol;
                if (symbol is { IsStatic: true })
                    return true;
            }
            else if (member is AnonymousMethodExpressionSyntax anonymousMethod)
            {
                var symbol = operation.SemanticModel?.GetSymbolInfo(anonymousMethod, cancellationToken).Symbol;
                if (symbol is { IsStatic: true })
                    return true;
            }
            else if (member is MethodDeclarationSyntax methodDeclaration)
            {
                var symbol = operation.SemanticModel?.GetDeclaredSymbol(methodDeclaration, cancellationToken);
                return symbol is { IsStatic: true };
            }
            else if (member is PropertyDeclarationSyntax propertyDeclaration)
            {
                var symbol = operation.SemanticModel?.GetDeclaredSymbol(propertyDeclaration, cancellationToken);
                return symbol is { IsStatic: true };
            }
            else if (member is FieldDeclarationSyntax)
            {
                return true; // Field initializers are in static context if the field is static, but default to true for safety
            }
        }

        return false;
    }

    public static IOperation UnwrapImplicitConversions(this IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
            operation = conversion.Operand;
        return operation;
    }

    public static IOperation UnwrapAllConversions(this IOperation operation)
    {
        while (operation is IConversionOperation conversion)
            operation = conversion.Operand;
        return operation;
    }

    public static IOperation UnwrapParenthesized(this IOperation operation)
    {
        while (operation is IParenthesizedOperation parenthesized)
            operation = parenthesized.Operand;
        return operation;
    }

    public static IOperation? UnwrapLabeledOperations(this IOperation operation)
    {
        if (operation is ILabeledOperation label)
            return label.Operation?.UnwrapLabeledOperations();
        return operation;
    }

    public static ITypeSymbol? GetActualType(this IOperation operation) =>
        operation.UnwrapAllConversions().Type;

    public static bool IsConstantZero(this IOperation operation) =>
        operation is { ConstantValue: { HasValue: true, Value: 0 or 0L or 0u or 0uL or 0f or 0d or 0m } };

    public static bool IsConstantNull(this IOperation operation) =>
        operation is { ConstantValue: { HasValue: true, Value: null } };

    public static bool IsConstant(this IOperation operation, out object? value)
    {
        if (operation.ConstantValue is { HasValue: true } constant)
        {
            value = constant.Value;
            return true;
        }

        value = null;
        return false;
    }

    public static bool TryGetConstantValue<T>(this IOperation operation, out T value)
    {
        if (operation.ConstantValue is { HasValue: true, Value: T typedValue })
        {
            value = typedValue;
            return true;
        }

        value = default!;
        return false;
    }

    public static IMethodSymbol? GetContainingMethod(this IOperation operation, CancellationToken cancellationToken)
    {
        if (operation.SemanticModel is null)
            return null;

        foreach (var syntax in operation.Syntax.AncestorsAndSelf())
        {
            switch (syntax)
            {
                case MethodDeclarationSyntax method:
                    return operation.SemanticModel.GetDeclaredSymbol(method, cancellationToken);
                case LocalFunctionStatementSyntax localFunction:
                    return operation.SemanticModel.GetDeclaredSymbol(localFunction, cancellationToken);
                case AccessorDeclarationSyntax accessor:
                    return operation.SemanticModel.GetDeclaredSymbol(accessor, cancellationToken);
                case LambdaExpressionSyntax lambda:
                    return operation.SemanticModel.GetSymbolInfo(lambda, cancellationToken).Symbol as IMethodSymbol;
            }
        }

        return null;
    }

    public static INamedTypeSymbol? GetContainingType(this IOperation operation, CancellationToken cancellationToken)
    {
        if (operation.SemanticModel is null)
            return null;

        foreach (var syntax in operation.Syntax.AncestorsAndSelf())
        {
            if (syntax is TypeDeclarationSyntax typeDecl)
                return operation.SemanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);
        }

        return null;
    }

    public static LanguageVersion GetCSharpLanguageVersion(this IOperation operation)
    {
        if (operation.Syntax.SyntaxTree.Options is CSharpParseOptions options)
            return options.LanguageVersion;
        return LanguageVersion.Default;
    }

    public static IEnumerable<IOperation> DescendantsAndSelf(this IOperation operation)
    {
        yield return operation;
        foreach (var child in operation.ChildOperations)
        foreach (var descendant in child.DescendantsAndSelf())
            yield return descendant;
    }

    public static IEnumerable<IOperation> Descendants(this IOperation operation)
    {
        foreach (var child in operation.ChildOperations)
        foreach (var descendant in child.DescendantsAndSelf())
            yield return descendant;
    }

    public static IEnumerable<T> DescendantsOfType<T>(this IOperation operation) where T : IOperation
    {
        foreach (var descendant in operation.Descendants())
        {
            if (descendant is T typed)
                yield return typed;
        }
    }

    public static bool ContainsOperation<T>(this IOperation operation) where T : IOperation =>
        operation.DescendantsOfType<T>().Any();

    public static bool IsInsideLoop(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ILoopOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    public static bool IsInsideTryBlock(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ITryOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    public static bool IsInsideCatchBlock(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ICatchClauseOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    public static bool IsInsideFinallyBlock(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ITryOperation tryOp && tryOp.Finally?.Operations.Any(op => op.Syntax.Contains(operation.Syntax)) is true)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    public static bool IsInsideLockStatement(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ILockOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    public static bool IsInsideUsingStatement(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is IUsingOperation or IUsingDeclarationOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    public static IBlockOperation? GetContainingBlock(this IOperation operation) =>
        operation.FindAncestor<IBlockOperation>();

    public static bool IsAssignmentTarget(this IOperation operation)
    {
        var parent = operation.Parent;
        if (parent is ISimpleAssignmentOperation assignment)
            return ReferenceEquals(assignment.Target, operation);
        if (parent is ICompoundAssignmentOperation compound)
            return ReferenceEquals(compound.Target, operation);
        if (parent is IIncrementOrDecrementOperation)
            return true;
        return false;
    }

    public static bool IsLeftSideOfAssignment(this IOperation operation)
    {
        var unwrapped = operation.UnwrapImplicitConversions();
        return unwrapped.Parent is IAssignmentOperation assignment && ReferenceEquals(assignment.Target, unwrapped);
    }

    public static bool IsPassedByRef(this IOperation operation)
    {
        if (operation.Parent is IArgumentOperation { Parameter: { RefKind: not RefKind.None } })
            return true;
        return false;
    }
}
