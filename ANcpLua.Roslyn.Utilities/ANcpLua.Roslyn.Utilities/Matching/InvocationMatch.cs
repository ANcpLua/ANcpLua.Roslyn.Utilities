using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities.Matching;

/// <summary>
///     Entry point for fluent invocation matching DSL.
/// </summary>
/// <remarks>
///     <para>
///         Provides factory methods for creating <see cref="InvocationMatcher" /> instances
///         that can be used to match <see cref="IInvocationOperation" /> nodes in Roslyn analyzers.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Use <see cref="Method()" /> to create a matcher without initial constraints.</description>
///         </item>
///         <item>
///             <description>Use <see cref="Method(string)" /> to create a matcher targeting a specific method name.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="InvocationMatcher" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class Invoke
{
    /// <summary>
    ///     Creates an invocation matcher without initial constraints.
    /// </summary>
    /// <returns>
    ///     A new <see cref="InvocationMatcher" /> instance that matches all invocations
    ///     until additional predicates are applied.
    /// </returns>
    /// <seealso cref="Method(string)" />
    public static InvocationMatcher Method()
    {
        return new InvocationMatcher();
    }

    /// <summary>
    ///     Creates an invocation matcher for a method with the specified name.
    /// </summary>
    /// <param name="name">The exact name of the method to match.</param>
    /// <returns>
    ///     A new <see cref="InvocationMatcher" /> instance configured to match invocations
    ///     of methods with the specified <paramref name="name" />.
    /// </returns>
    /// <seealso cref="Method()" />
    /// <seealso cref="InvocationMatcher.Named(string)" />
    public static InvocationMatcher Method(string name)
    {
        return new InvocationMatcher().Named(name);
    }
}

/// <summary>
///     Fluent matcher for <see cref="IInvocationOperation" /> - the bread and butter of analyzers.
/// </summary>
/// <remarks>
///     <para>
///         This class provides a fluent API for building predicates to match method invocations
///         in Roslyn operation trees. Predicates are combined with AND semantics - all predicates
///         must match for the overall match to succeed.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Chain method calls to build complex matching criteria.</description>
///         </item>
///         <item>
///             <description>Use <see cref="Matches(IInvocationOperation?)" /> to test invocations.</description>
///         </item>
///         <item>
///             <description>Use <see cref="Where(Func{TResult})" /> for custom conditions.</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///     var matcher = Invoke.Method("Dispose")
///         .OnTypeImplementing("IDisposable")
///         .WithNoArguments();
///
///     if (matcher.Matches(invocation))
///     {
///         // Handle Dispose call
///     }
///     </code>
/// </example>
/// <seealso cref="Invoke" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class InvocationMatcher
{
    private readonly List<Func<IInvocationOperation, bool>> _predicates = [];

    private InvocationMatcher AddPredicate(Func<IInvocationOperation, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    /// <summary>
    ///     Tests if an invocation matches all configured predicates.
    /// </summary>
    /// <param name="invocation">The invocation operation to test, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="invocation" /> is not <c>null</c> and satisfies
    ///     all configured predicates; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Matches(IOperation?)" />
    public bool Matches(IInvocationOperation? invocation)
    {
        if (invocation is null)
            return false;

        foreach (var predicate in _predicates)
            if (!predicate(invocation))
                return false;

        return true;
    }

    /// <summary>
    ///     Tests if an operation is an invocation that matches all configured predicates.
    /// </summary>
    /// <param name="operation">The operation to test, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="operation" /> is an <see cref="IInvocationOperation" />
    ///     that satisfies all configured predicates; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Matches(IInvocationOperation?)" />
    public bool Matches(IOperation? operation)
    {
        return operation is IInvocationOperation invocation && Matches(invocation);
    }

    // Method name matching

    /// <summary>
    ///     Matches invocations of methods with the specified exact name.
    /// </summary>
    /// <param name="name">The exact method name to match.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="NameStartsWith(string)" />
    /// <seealso cref="NameEndsWith(string)" />
    /// <seealso cref="NameContains(string)" />
    public InvocationMatcher Named(string name)
    {
        return AddPredicate(i => i.TargetMethod.Name == name);
    }

    /// <summary>
    ///     Matches invocations where the method name starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match against method names.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="Named(string)" />
    /// <seealso cref="NameEndsWith(string)" />
    /// <seealso cref="NameContains(string)" />
    public InvocationMatcher NameStartsWith(string prefix)
    {
        return AddPredicate(i => i.TargetMethod.Name.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Matches invocations where the method name ends with the specified suffix.
    /// </summary>
    /// <param name="suffix">The suffix to match against method names.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="Named(string)" />
    /// <seealso cref="NameStartsWith(string)" />
    /// <seealso cref="NameContains(string)" />
    public InvocationMatcher NameEndsWith(string suffix)
    {
        return AddPredicate(i => i.TargetMethod.Name.EndsWith(suffix, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Matches invocations where the method name contains the specified substring.
    /// </summary>
    /// <param name="substring">The substring to search for in method names.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="Named(string)" />
    /// <seealso cref="NameStartsWith(string)" />
    /// <seealso cref="NameEndsWith(string)" />
    public InvocationMatcher NameContains(string substring)
    {
        return AddPredicate(i => i.TargetMethod.Name.Contains(substring, StringComparison.Ordinal));
    }

    // Receiver type matching

    /// <summary>
    ///     Matches invocations on a receiver with the specified type name.
    /// </summary>
    /// <param name="typeName">The simple type name of the receiver.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <remarks>
    ///     For extension methods, the receiver is the first argument.
    ///     For static methods, the containing type is used.
    /// </remarks>
    /// <seealso cref="OnTypeInheritingFrom(string)" />
    /// <seealso cref="OnTypeImplementing(string)" />
    public InvocationMatcher OnType(string typeName)
    {
        return AddPredicate(i => GetReceiverTypeName(i) == typeName);
    }

    /// <summary>
    ///     Matches invocations where the receiver type inherits from the specified base type.
    /// </summary>
    /// <param name="baseTypeName">The simple name or fully qualified name of the base type.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="OnType(string)" />
    /// <seealso cref="OnTypeImplementing(string)" />
    public InvocationMatcher OnTypeInheritingFrom(string baseTypeName)
    {
        return AddPredicate(i =>
        {
            var receiverType = GetReceiverType(i);
            return receiverType is not null && InheritsFromName(receiverType, baseTypeName);
        });
    }

    /// <summary>
    ///     Matches invocations where the receiver type implements the specified interface.
    /// </summary>
    /// <param name="interfaceName">The simple name or fully qualified name of the interface.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="OnType(string)" />
    /// <seealso cref="OnTypeInheritingFrom(string)" />
    public InvocationMatcher OnTypeImplementing(string interfaceName)
    {
        return AddPredicate(i =>
        {
            var receiverType = GetReceiverType(i);
            return receiverType is not null && ImplementsInterface(receiverType, interfaceName);
        });
    }

    // Method characteristics

    /// <summary>
    ///     Matches extension method invocations.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="NotExtension" />
    /// <seealso cref="Static" />
    /// <seealso cref="Instance" />
    public InvocationMatcher Extension()
    {
        return AddPredicate(i => i.TargetMethod.IsExtensionMethod);
    }

    /// <summary>
    ///     Matches non-extension method invocations.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="Extension" />
    public InvocationMatcher NotExtension()
    {
        return AddPredicate(i => !i.TargetMethod.IsExtensionMethod);
    }

    /// <summary>
    ///     Matches static method invocations that are not extension methods.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="Instance" />
    /// <seealso cref="Extension" />
    public InvocationMatcher Static()
    {
        return AddPredicate(i => i.TargetMethod is { IsStatic: true, IsExtensionMethod: false });
    }

    /// <summary>
    ///     Matches instance method invocations, including extension methods called on an instance.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="Static" />
    /// <seealso cref="Extension" />
    public InvocationMatcher Instance()
    {
        return AddPredicate(i => !i.TargetMethod.IsStatic || i.TargetMethod.IsExtensionMethod);
    }

    /// <summary>
    ///     Matches async method invocations.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="ReturningTask" />
    public InvocationMatcher Async()
    {
        return AddPredicate(i => i.TargetMethod.IsAsync);
    }

    /// <summary>
    ///     Matches generic method invocations.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    public InvocationMatcher Generic()
    {
        return AddPredicate(i => i.TargetMethod.IsGenericMethod);
    }

    // Return type

    /// <summary>
    ///     Matches invocations of methods that return void.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="ReturningTask" />
    /// <seealso cref="Returning(string)" />
    public InvocationMatcher ReturningVoid()
    {
        return AddPredicate(i => i.TargetMethod.ReturnsVoid);
    }

    /// <summary>
    ///     Matches invocations of methods that return <see cref="System.Threading.Tasks.Task" />
    ///     or <see cref="System.Threading.Tasks.ValueTask" />.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="Async" />
    /// <seealso cref="ReturningVoid" />
    /// <seealso cref="Returning(string)" />
    public InvocationMatcher ReturningTask()
    {
        return AddPredicate(i => i.TargetMethod.ReturnType.Name is "Task" or "ValueTask" ||
                                 i.TargetMethod.ReturnType.OriginalDefinition.Name is "Task" or "ValueTask");
    }

    /// <summary>
    ///     Matches invocations of methods that return the specified type.
    /// </summary>
    /// <param name="typeName">The simple type name of the return type.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="ReturningVoid" />
    /// <seealso cref="ReturningTask" />
    public InvocationMatcher Returning(string typeName)
    {
        return AddPredicate(i => i.TargetMethod.ReturnType.Name == typeName);
    }

    // Arguments

    /// <summary>
    ///     Matches invocations with exactly the specified number of arguments.
    /// </summary>
    /// <param name="count">The exact number of arguments required.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="WithNoArguments" />
    /// <seealso cref="WithMinArguments(int)" />
    public InvocationMatcher WithArguments(int count)
    {
        return AddPredicate(i => i.Arguments.Length == count);
    }

    /// <summary>
    ///     Matches invocations with no arguments.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="WithArguments(int)" />
    /// <seealso cref="WithMinArguments(int)" />
    public InvocationMatcher WithNoArguments()
    {
        return AddPredicate(i => i.Arguments.Length is 0);
    }

    /// <summary>
    ///     Matches invocations with at least the specified number of arguments.
    /// </summary>
    /// <param name="count">The minimum number of arguments required.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="WithArguments(int)" />
    /// <seealso cref="WithNoArguments" />
    public InvocationMatcher WithMinArguments(int count)
    {
        return AddPredicate(i => i.Arguments.Length >= count);
    }

    /// <summary>
    ///     Matches invocations where the argument at the specified index is a constant value.
    /// </summary>
    /// <param name="index">The zero-based index of the argument to check.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="WithConstantStringArg(int)" />
    /// <seealso cref="WithNullArg(int)" />
    /// <seealso cref="WithAllConstantArgs" />
    public InvocationMatcher WithConstantArg(int index)
    {
        return AddPredicate(i => i.Arguments.Length > index &&
                                 i.Arguments[index].Value.ConstantValue.HasValue);
    }

    /// <summary>
    ///     Matches invocations where the argument at the specified index is a constant string.
    /// </summary>
    /// <param name="index">The zero-based index of the argument to check.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="WithConstantArg(int)" />
    /// <seealso cref="WithNullArg(int)" />
    public InvocationMatcher WithConstantStringArg(int index)
    {
        return AddPredicate(i => i.Arguments.Length > index &&
                                 i.Arguments[index].Value.ConstantValue is { HasValue: true, Value: string });
    }

    /// <summary>
    ///     Matches invocations where the argument at the specified index is <c>null</c>.
    /// </summary>
    /// <param name="index">The zero-based index of the argument to check.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="WithConstantArg(int)" />
    /// <seealso cref="WithConstantStringArg(int)" />
    public InvocationMatcher WithNullArg(int index)
    {
        return AddPredicate(i => i.Arguments.Length > index &&
                                 i.Arguments[index].Value.ConstantValue is { HasValue: true, Value: null });
    }

    /// <summary>
    ///     Matches invocations where the argument at the specified index has the specified type.
    /// </summary>
    /// <param name="index">The zero-based index of the argument to check.</param>
    /// <param name="typeName">The simple type name of the expected argument type.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="WithConstantArg(int)" />
    public InvocationMatcher WithArgOfType(int index, string typeName)
    {
        return AddPredicate(i => i.Arguments.Length > index &&
                                 i.Arguments[index].Value.Type?.Name == typeName);
    }

    /// <summary>
    ///     Matches invocations where all arguments are constant values.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="WithConstantArg(int)" />
    public InvocationMatcher WithAllConstantArgs()
    {
        return AddPredicate(AllArgumentsConstant);
    }

    // Namespace

    /// <summary>
    ///     Matches invocations of methods defined in the specified namespace.
    /// </summary>
    /// <param name="namespaceName">The fully qualified namespace name.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="InNamespaceStartingWith(string)" />
    public InvocationMatcher InNamespace(string namespaceName)
    {
        return AddPredicate(i => i.TargetMethod.ContainingNamespace?.ToDisplayString() == namespaceName);
    }

    /// <summary>
    ///     Matches invocations of methods defined in namespaces starting with the specified prefix.
    /// </summary>
    /// <param name="prefix">The namespace prefix to match.</param>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="InNamespace(string)" />
    public InvocationMatcher InNamespaceStartingWith(string prefix)
    {
        return AddPredicate(i => i.TargetMethod.ContainingNamespace?.ToDisplayString()
            ?.StartsWith(prefix, StringComparison.Ordinal) == true);
    }

    // Common patterns

    /// <summary>
    ///     Matches LINQ extension method invocations from the <c>System.Linq</c> namespace.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="Extension" />
    /// <seealso cref="InNamespace(string)" />
    public InvocationMatcher Linq()
    {
        return InNamespace("System.Linq").Extension();
    }

    /// <summary>
    ///     Matches method invocations on <see cref="string" /> instances.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="OnType(string)" />
    public InvocationMatcher OnString()
    {
        return OnType("String");
    }

    /// <summary>
    ///     Matches method invocations on <see cref="System.Threading.Tasks.Task" /> instances.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="OnType(string)" />
    /// <seealso cref="ReturningTask" />
    public InvocationMatcher OnTask()
    {
        return OnType("Task");
    }

    /// <summary>
    ///     Matches method invocations on <see cref="Console" /> instances.
    /// </summary>
    /// <returns>This matcher for method chaining.</returns>
    /// <seealso cref="OnType(string)" />
    public InvocationMatcher OnConsole()
    {
        return OnType("Console");
    }

    /// <summary>
    ///     Adds a custom matching condition to this matcher.
    /// </summary>
    /// <param name="predicate">
    ///     A function that returns <c>true</c> if the invocation matches the custom condition.
    /// </param>
    /// <returns>This matcher for method chaining.</returns>
    /// <remarks>
    ///     Use this method when the built-in matching methods are insufficient
    ///     for your use case.
    /// </remarks>
    public InvocationMatcher Where(Func<IInvocationOperation, bool> predicate)
    {
        return AddPredicate(predicate);
    }

    // Helper methods
    private static string? GetReceiverTypeName(IInvocationOperation invocation)
    {
        if (invocation.Instance is not null)
            return invocation.Instance.Type?.Name;

        if (invocation.TargetMethod.IsExtensionMethod && invocation.Arguments.Length > 0)
            return invocation.Arguments[0].Value.Type?.Name;

        return invocation.TargetMethod.ContainingType?.Name;
    }

    private static INamedTypeSymbol? GetReceiverType(IInvocationOperation invocation)
    {
        if (invocation.Instance?.Type is INamedTypeSymbol instanceType)
            return instanceType;

        if (invocation.TargetMethod.IsExtensionMethod &&
            invocation.Arguments.Length > 0 &&
            invocation.Arguments[0].Value.Type is INamedTypeSymbol argType)
            return argType;

        return invocation.TargetMethod.ContainingType;
    }

    private static bool InheritsFromName(ITypeSymbol type, string name)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.Name == name || current.ToDisplayString() == name)
                return true;
            current = current.BaseType;
        }

        return false;
    }

    private static bool ImplementsInterface(ITypeSymbol type, string name)
    {
        foreach (var iface in type.AllInterfaces)
            if (iface.Name == name || iface.ToDisplayString() == name)
                return true;

        return false;
    }

    private static bool AllArgumentsConstant(IInvocationOperation invocation)
    {
        foreach (var arg in invocation.Arguments)
            if (!arg.Value.ConstantValue.HasValue)
                return false;

        return true;
    }
}