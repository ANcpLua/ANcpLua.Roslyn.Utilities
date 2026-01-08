using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities;

public static class InvocationExtensions
{
    public static IArgumentOperation? GetArgument(this IInvocationOperation operation, string parameterName)
    {
        foreach (var arg in operation.Arguments)
        {
            if (arg.Parameter?.Name == parameterName)
                return arg;
        }

        return null;
    }

    public static IArgumentOperation? GetArgument(this IInvocationOperation operation, int index)
    {
        if (index < 0 || index >= operation.Arguments.Length)
            return null;

        foreach (var arg in operation.Arguments)
        {
            if (arg.Parameter?.Ordinal == index)
                return arg;
        }

        return null;
    }

    public static IArgumentOperation? GetArgument(this IObjectCreationOperation operation, string parameterName)
    {
        foreach (var arg in operation.Arguments)
        {
            if (arg.Parameter?.Name == parameterName)
                return arg;
        }

        return null;
    }

    public static IArgumentOperation? GetArgument(this IObjectCreationOperation operation, int index)
    {
        if (index < 0 || index >= operation.Arguments.Length)
            return null;

        foreach (var arg in operation.Arguments)
        {
            if (arg.Parameter?.Ordinal == index)
                return arg;
        }

        return null;
    }

    public static bool HasArgumentOfType(this IInvocationOperation operation, ITypeSymbol? type, bool inherits = false)
    {
        if (type is null)
            return false;

        if (inherits && type.IsSealed)
            inherits = false;

        foreach (var arg in operation.Arguments)
        {
            var argType = arg.Value.Type;
            if (argType is null)
                continue;

            if (inherits)
            {
                if (argType.IsOrInheritsFrom(type))
                    return true;
            }
            else if (argType.IsEqualTo(type))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetConstantArgument<T>(this IInvocationOperation operation, string paramName, out T value)
    {
        var arg = operation.GetArgument(paramName);
        if (arg is not null && arg.Value.TryGetConstantValue(out value))
            return true;

        value = default!;
        return false;
    }

    public static bool TryGetConstantArgument<T>(this IInvocationOperation operation, int index, out T value)
    {
        var arg = operation.GetArgument(index);
        if (arg is not null && arg.Value.TryGetConstantValue(out value))
            return true;

        value = default!;
        return false;
    }

    public static bool TryGetStringArgument(this IInvocationOperation operation, string paramName, out string? value)
    {
        var arg = operation.GetArgument(paramName);
        if (arg is not null)
        {
            if (arg.Value.ConstantValue is { HasValue: true, Value: string str })
            {
                value = str;
                return true;
            }

            if (arg.Value.ConstantValue is { HasValue: true, Value: null })
            {
                value = null;
                return true;
            }
        }

        value = null;
        return false;
    }

    public static bool TryGetStringArgument(this IInvocationOperation operation, int index, out string? value)
    {
        var arg = operation.GetArgument(index);
        if (arg is not null)
        {
            if (arg.Value.ConstantValue is { HasValue: true, Value: string str })
            {
                value = str;
                return true;
            }

            if (arg.Value.ConstantValue is { HasValue: true, Value: null })
            {
                value = null;
                return true;
            }
        }

        value = null;
        return false;
    }

    public static bool IsMethodNamed(this IInvocationOperation operation, string containingTypeName, string methodName) =>
        operation.TargetMethod.Name == methodName &&
        operation.TargetMethod.ContainingType?.ToDisplayString() == containingTypeName;

    public static bool IsMethodNamed(this IInvocationOperation operation, ITypeSymbol? containingType, string methodName) =>
        containingType is not null &&
        operation.TargetMethod.Name == methodName &&
        operation.TargetMethod.ContainingType.IsEqualTo(containingType);

    public static bool IsExtensionMethodOn(this IInvocationOperation operation, ITypeSymbol? receiverType)
    {
        if (receiverType is null || !operation.TargetMethod.IsExtensionMethod)
            return false;

        var receiver = operation.Arguments.FirstOrDefault()?.Value.Type;
        return receiver is not null && receiver.IsOrInheritsFrom(receiverType);
    }

    public static bool IsInstanceMethodOn(this IInvocationOperation operation, ITypeSymbol? receiverType)
    {
        if (receiverType is null || operation.Instance?.Type is not { } instanceType)
            return false;

        return instanceType.IsOrInheritsFrom(receiverType);
    }

    public static ITypeSymbol? GetReceiverType(this IInvocationOperation operation)
    {
        if (operation.Instance is not null)
            return operation.Instance.Type;

        if (operation.TargetMethod.IsExtensionMethod && operation.Arguments.Length > 0)
            return operation.Arguments[0].Value.Type;

        return null;
    }

    public static bool ReturnsVoid(this IInvocationOperation operation) =>
        operation.TargetMethod.ReturnsVoid;

    public static bool IsAsyncMethod(this IInvocationOperation operation) =>
        operation.TargetMethod.IsAsync;

    public static bool HasCancellationTokenParameter(this IInvocationOperation operation)
    {
        foreach (var param in operation.TargetMethod.Parameters)
        {
            if (param.Type.ToDisplayString() == "System.Threading.CancellationToken")
                return true;
        }

        return false;
    }

    public static bool IsCancellationTokenPassed(this IInvocationOperation operation)
    {
        foreach (var arg in operation.Arguments)
        {
            if (arg.Parameter?.Type.ToDisplayString() == "System.Threading.CancellationToken" &&
                !arg.Value.IsConstantNull())
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsLinqMethod(this IInvocationOperation operation)
    {
        var containingType = operation.TargetMethod.ContainingType?.ToDisplayString();
        return containingType is "System.Linq.Enumerable" or "System.Linq.Queryable";
    }

    public static bool IsStringMethod(this IInvocationOperation operation) =>
        operation.TargetMethod.ContainingType?.SpecialType is SpecialType.System_String;

    public static bool IsObjectMethod(this IInvocationOperation operation) =>
        operation.TargetMethod.ContainingType?.SpecialType is SpecialType.System_Object;

    public static int GetArgumentCount(this IInvocationOperation operation) =>
        operation.Arguments.Length;

    public static bool HasOptionalArgumentsNotProvided(this IInvocationOperation operation)
    {
        var providedCount = operation.Arguments.Count(a => !a.IsImplicit);
        var totalParams = operation.TargetMethod.Parameters.Length;
        return providedCount < totalParams;
    }

    public static IEnumerable<IArgumentOperation> GetExplicitArguments(this IInvocationOperation operation)
    {
        foreach (var arg in operation.Arguments)
        {
            if (!arg.IsImplicit)
                yield return arg;
        }
    }

    public static bool AllArgumentsAreConstant(this IInvocationOperation operation)
    {
        foreach (var arg in operation.Arguments)
        {
            if (!arg.Value.ConstantValue.HasValue)
                return false;
        }

        return true;
    }

    public static bool IsNullConditionalAccess(this IInvocationOperation operation) =>
        operation.Syntax.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalAccessExpressionSyntax;
}