using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for working with <see cref="IInvocationOperation" /> and
///     <see cref="IObjectCreationOperation" />.
/// </summary>
/// <remarks>
///     <para>
///         This class provides utilities for analyzing method invocations and object creations in Roslyn operations,
///         including argument extraction, method identification, and context detection.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Retrieve arguments by name or index from invocations and object creations</description>
///         </item>
///         <item>
///             <description>Extract constant argument values with type-safe conversion</description>
///         </item>
///         <item>
///             <description>Identify method characteristics such as LINQ methods, async methods, and extension methods</description>
///         </item>
///         <item>
///             <description>Analyze receiver types and cancellation token usage</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="IInvocationOperation" />
/// <seealso cref="IObjectCreationOperation" />
/// <seealso cref="IArgumentOperation" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class InvocationExtensions
{
    /// <summary>
    ///     Gets an argument from an invocation operation by parameter name.
    /// </summary>
    /// <param name="operation">The invocation operation to search.</param>
    /// <param name="parameterName">The name of the parameter to find.</param>
    /// <returns>
    ///     The <see cref="IArgumentOperation" /> matching the specified parameter name,
    ///     or <c>null</c> if no matching argument is found.
    /// </returns>
    /// <seealso cref="GetArgument(IInvocationOperation, int)" />
    public static IArgumentOperation? GetArgument(this IInvocationOperation operation, string parameterName)
    {
        foreach (var arg in operation.Arguments)
            if (arg.Parameter?.Name == parameterName)
                return arg;

        return null;
    }

    /// <summary>
    ///     Gets an argument from an invocation operation by parameter index.
    /// </summary>
    /// <param name="operation">The invocation operation to search.</param>
    /// <param name="index">The zero-based ordinal index of the parameter.</param>
    /// <returns>
    ///     The <see cref="IArgumentOperation" /> at the specified index,
    ///     or <c>null</c> if the index is out of range or no matching argument is found.
    /// </returns>
    /// <seealso cref="GetArgument(IInvocationOperation, string)" />
    public static IArgumentOperation? GetArgument(this IInvocationOperation operation, int index)
    {
        if (index < 0 || index >= operation.Arguments.Length)
            return null;

        foreach (var arg in operation.Arguments)
            if (arg.Parameter?.Ordinal == index)
                return arg;

        return null;
    }

    /// <summary>
    ///     Gets an argument from an object creation operation by parameter name.
    /// </summary>
    /// <param name="operation">The object creation operation to search.</param>
    /// <param name="parameterName">The name of the parameter to find.</param>
    /// <returns>
    ///     The <see cref="IArgumentOperation" /> matching the specified parameter name,
    ///     or <c>null</c> if no matching argument is found.
    /// </returns>
    /// <seealso cref="GetArgument(IObjectCreationOperation, int)" />
    public static IArgumentOperation? GetArgument(this IObjectCreationOperation operation, string parameterName)
    {
        foreach (var arg in operation.Arguments)
            if (arg.Parameter?.Name == parameterName)
                return arg;

        return null;
    }

    /// <summary>
    ///     Gets an argument from an object creation operation by parameter index.
    /// </summary>
    /// <param name="operation">The object creation operation to search.</param>
    /// <param name="index">The zero-based ordinal index of the parameter.</param>
    /// <returns>
    ///     The <see cref="IArgumentOperation" /> at the specified index,
    ///     or <c>null</c> if the index is out of range or no matching argument is found.
    /// </returns>
    /// <seealso cref="GetArgument(IObjectCreationOperation, string)" />
    public static IArgumentOperation? GetArgument(this IObjectCreationOperation operation, int index)
    {
        if (index < 0 || index >= operation.Arguments.Length)
            return null;

        foreach (var arg in operation.Arguments)
            if (arg.Parameter?.Ordinal == index)
                return arg;

        return null;
    }

    /// <summary>
    ///     Determines whether the invocation has an argument of the specified type.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <param name="type">The type to search for among argument types.</param>
    /// <param name="inherits">
    ///     If <c>true</c>, also matches arguments whose types inherit from <paramref name="type" />.
    ///     Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if any argument has a type matching or inheriting from the specified type;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="TypeSymbolExtensions.IsOrInheritsFrom" />
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

    /// <summary>
    ///     Tries to get a constant argument value by parameter name.
    /// </summary>
    /// <typeparam name="T">The expected type of the constant value.</typeparam>
    /// <param name="operation">The invocation operation to search.</param>
    /// <param name="paramName">The name of the parameter to find.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the constant value of the argument;
    ///     otherwise, contains the default value of <typeparamref name="T" />.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the argument was found and has a constant value of type <typeparamref name="T" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="TryGetConstantArgument{T}(IInvocationOperation, int, out T)" />
    /// <seealso cref="OperationExtensions.TryGetConstantValue{T}" />
    public static bool TryGetConstantArgument<T>(this IInvocationOperation operation, string paramName, [NotNullWhen(true)] out T value)
    {
        var arg = operation.GetArgument(paramName);
        if (arg is not null && arg.Value.TryGetConstantValue(out value))
            return true;

        value = default!;
        return false;
    }

    /// <summary>
    ///     Tries to get a constant argument value by parameter index.
    /// </summary>
    /// <typeparam name="T">The expected type of the constant value.</typeparam>
    /// <param name="operation">The invocation operation to search.</param>
    /// <param name="index">The zero-based ordinal index of the parameter.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the constant value of the argument;
    ///     otherwise, contains the default value of <typeparamref name="T" />.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the argument was found and has a constant value of type <typeparamref name="T" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="TryGetConstantArgument{T}(IInvocationOperation, string, out T)" />
    /// <seealso cref="OperationExtensions.TryGetConstantValue{T}" />
    public static bool TryGetConstantArgument<T>(this IInvocationOperation operation, int index, [NotNullWhen(true)] out T value)
    {
        var arg = operation.GetArgument(index);
        if (arg is not null && arg.Value.TryGetConstantValue(out value))
            return true;

        value = default!;
        return false;
    }

    /// <summary>
    ///     Tries to get the constant value of an argument.
    /// </summary>
    public static bool TryGetArgumentValue<T>(this IInvocationOperation operation, string paramName, [NotNullWhen(true)] out T value)
        => operation.TryGetConstantArgument(paramName, out value);

    /// <summary>
    ///     Tries to get the constant value of an argument.
    /// </summary>
    public static bool TryGetArgumentValue<T>(this IInvocationOperation operation, int index, [NotNullWhen(true)] out T value)
        => operation.TryGetConstantArgument(index, out value);

    /// <summary>
    ///     Tries to get a string constant argument value by parameter name.
    /// </summary>
    /// <param name="operation">The invocation operation to search.</param>
    /// <param name="paramName">The name of the parameter to find.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the string value of the argument
    ///     (which may be <c>null</c> if the constant value is null);
    ///     otherwise, contains <c>null</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the argument was found and has a constant string value (including <c>null</c>);
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="TryGetStringArgument(IInvocationOperation, int, out string?)" />
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

    /// <summary>
    ///     Tries to get a string constant argument value by parameter index.
    /// </summary>
    /// <param name="operation">The invocation operation to search.</param>
    /// <param name="index">The zero-based ordinal index of the parameter.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the string value of the argument
    ///     (which may be <c>null</c> if the constant value is null);
    ///     otherwise, contains <c>null</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the argument was found and has a constant string value (including <c>null</c>);
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="TryGetStringArgument(IInvocationOperation, string, out string?)" />
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

    /// <summary>
    ///     Determines whether the invocation targets a method with the specified containing type and name.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <param name="containingTypeName">The fully qualified name of the containing type.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <returns>
    ///     <c>true</c> if the target method has the specified name and containing type;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsMethodNamed(IInvocationOperation, ITypeSymbol?, string)" />
    public static bool IsMethodNamed(this IInvocationOperation operation, string containingTypeName, string methodName)
    {
        return operation.TargetMethod.Name == methodName &&
               operation.TargetMethod.ContainingType?.ToDisplayString() == containingTypeName;
    }

    /// <summary>
    ///     Determines whether the invocation targets a method with the specified containing type and name.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <param name="containingType">The containing type symbol to match.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <returns>
    ///     <c>true</c> if the target method has the specified name and containing type;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsMethodNamed(IInvocationOperation, string, string)" />
    public static bool IsMethodNamed(this IInvocationOperation operation, ITypeSymbol? containingType,
        string methodName)
    {
        return containingType is not null &&
               operation.TargetMethod.Name == methodName &&
               operation.TargetMethod.ContainingType.IsEqualTo(containingType);
    }

    /// <summary>
    ///     Determines whether the invocation is an extension method call on the specified receiver type.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <param name="receiverType">The expected receiver type for the extension method.</param>
    /// <returns>
    ///     <c>true</c> if the invocation is an extension method and the first argument's type
    ///     is or inherits from the specified receiver type; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsInstanceMethodOn" />
    /// <seealso cref="GetReceiverType" />
    public static bool IsExtensionMethodOn(this IInvocationOperation operation, ITypeSymbol? receiverType)
    {
        if (receiverType is null || !operation.TargetMethod.IsExtensionMethod)
            return false;

        var receiver = operation.Arguments.FirstOrDefault()?.Value.Type;
        return receiver is not null && receiver.IsOrInheritsFrom(receiverType);
    }

    /// <summary>
    ///     Determines whether the invocation is an instance method call on the specified receiver type.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <param name="receiverType">The expected instance type for the method call.</param>
    /// <returns>
    ///     <c>true</c> if the invocation has an instance receiver whose type is or inherits from
    ///     the specified receiver type; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsExtensionMethodOn" />
    /// <seealso cref="GetReceiverType" />
    public static bool IsInstanceMethodOn(this IInvocationOperation operation, ITypeSymbol? receiverType)
    {
        if (receiverType is null || operation.Instance?.Type is not { } instanceType)
            return false;

        return instanceType.IsOrInheritsFrom(receiverType);
    }

    /// <summary>
    ///     Gets the receiver type of the invocation, handling both instance and extension methods.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>
    ///     The type of the instance receiver for instance methods, the type of the first argument
    ///     for extension methods, or <c>null</c> for static non-extension methods.
    /// </returns>
    /// <seealso cref="IsExtensionMethodOn" />
    /// <seealso cref="IsInstanceMethodOn" />
    public static ITypeSymbol? GetReceiverType(this IInvocationOperation operation)
    {
        if (operation.Instance is not null)
            return operation.Instance.Type;

        if (operation.TargetMethod.IsExtensionMethod && operation.Arguments.Length > 0)
            return operation.Arguments[0].Value.Type;

        return null;
    }

    /// <summary>
    ///     Determines whether the invoked method returns void.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns><c>true</c> if the target method returns void; otherwise, <c>false</c>.</returns>
    public static bool ReturnsVoid(this IInvocationOperation operation)
    {
        return operation.TargetMethod.ReturnsVoid;
    }

    /// <summary>
    ///     Determines whether the invoked method is marked with the async modifier.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns><c>true</c> if the target method is async; otherwise, <c>false</c>.</returns>
    /// <seealso cref="HasCancellationTokenParameter" />
    /// <seealso cref="IsCancellationTokenPassed" />
    public static bool IsAsyncMethod(this IInvocationOperation operation)
    {
        return operation.TargetMethod.IsAsync;
    }

    /// <summary>
    ///     Determines whether the invoked method has a <see cref="System.Threading.CancellationToken" /> parameter.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>
    ///     <c>true</c> if any parameter of the target method is of type
    ///     <see cref="System.Threading.CancellationToken" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsCancellationTokenPassed" />
    /// <seealso cref="IsAsyncMethod" />
    public static bool HasCancellationTokenParameter(this IInvocationOperation operation)
    {
        foreach (var param in operation.TargetMethod.Parameters)
            if (param.Type.ToDisplayString() == "System.Threading.CancellationToken")
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether a non-null <see cref="System.Threading.CancellationToken" /> is passed to the invocation.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>
    ///     <c>true</c> if a non-null cancellation token argument is provided; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="HasCancellationTokenParameter" />
    /// <seealso cref="IsAsyncMethod" />
    public static bool IsCancellationTokenPassed(this IInvocationOperation operation)
    {
        foreach (var arg in operation.Arguments)
            if (arg.Parameter?.Type.ToDisplayString() == "System.Threading.CancellationToken" &&
                !arg.Value.IsConstantNull())
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the invocation is a LINQ method from <c>System.Linq.Enumerable</c> or
    ///     <c>System.Linq.Queryable</c>.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>
    ///     <c>true</c> if the target method is defined on <c>System.Linq.Enumerable</c> or
    ///     <c>System.Linq.Queryable</c>; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsStringMethod" />
    /// <seealso cref="IsObjectMethod" />
    public static bool IsLinqMethod(this IInvocationOperation operation)
    {
        var containingType = operation.TargetMethod.ContainingType?.ToDisplayString();
        return containingType is "System.Linq.Enumerable" or "System.Linq.Queryable";
    }

    /// <summary>
    ///     Determines whether the invocation is a method on <see cref="string" />.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>
    ///     <c>true</c> if the target method is defined on <see cref="string" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsLinqMethod" />
    /// <seealso cref="IsObjectMethod" />
    public static bool IsStringMethod(this IInvocationOperation operation)
    {
        return operation.TargetMethod.ContainingType?.SpecialType is SpecialType.System_String;
    }

    /// <summary>
    ///     Determines whether the invocation is a method on <see cref="object" />.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>
    ///     <c>true</c> if the target method is defined on <see cref="object" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsLinqMethod" />
    /// <seealso cref="IsStringMethod" />
    public static bool IsObjectMethod(this IInvocationOperation operation)
    {
        return operation.TargetMethod.ContainingType?.SpecialType is SpecialType.System_Object;
    }

    /// <summary>
    ///     Gets the number of arguments in the invocation.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>The total number of arguments, including both explicit and implicit arguments.</returns>
    /// <seealso cref="GetExplicitArguments" />
    /// <seealso cref="HasOptionalArgumentsNotProvided" />
    public static int GetArgumentCount(this IInvocationOperation operation)
    {
        return operation.Arguments.Length;
    }

    /// <summary>
    ///     Determines whether there are optional parameters that were not explicitly provided.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>
    ///     <c>true</c> if the number of explicit arguments is less than the total number of
    ///     method parameters; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="GetExplicitArguments" />
    /// <seealso cref="GetArgumentCount" />
    public static bool HasOptionalArgumentsNotProvided(this IInvocationOperation operation)
    {
        var providedCount = operation.Arguments.Count(a => !a.IsImplicit);
        var totalParams = operation.TargetMethod.Parameters.Length;
        return providedCount < totalParams;
    }

    /// <summary>
    ///     Gets only the explicitly provided arguments from the invocation.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>
    ///     An enumerable of <see cref="IArgumentOperation" /> representing arguments that were
    ///     explicitly specified in the source code.
    /// </returns>
    /// <seealso cref="GetArgumentCount" />
    /// <seealso cref="HasOptionalArgumentsNotProvided" />
    public static IEnumerable<IArgumentOperation> GetExplicitArguments(this IInvocationOperation operation)
    {
        foreach (var arg in operation.Arguments)
            if (!arg.IsImplicit)
                yield return arg;
    }

    /// <summary>
    ///     Determines whether all arguments in the invocation are compile-time constants.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>
    ///     <c>true</c> if every argument has a constant value; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="TryGetConstantArgument{T}(IInvocationOperation, string, out T)" />
    public static bool AllArgumentsAreConstant(this IInvocationOperation operation)
    {
        foreach (var arg in operation.Arguments)
            if (!arg.Value.ConstantValue.HasValue)
                return false;

        return true;
    }

    /// <summary>
    ///     Determines whether the invocation is part of a null-conditional access expression.
    /// </summary>
    /// <param name="operation">The invocation operation to examine.</param>
    /// <returns>
    ///     <c>true</c> if the invocation syntax is within a <c>?.</c> or <c>?[]</c> access;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNullConditionalAccess(this IInvocationOperation operation)
    {
        return operation.Syntax.Parent is ConditionalAccessExpressionSyntax;
    }
}