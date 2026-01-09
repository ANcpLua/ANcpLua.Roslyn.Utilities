using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities.Matching;

/// <summary>
///     Entry point for fluent invocation matching DSL.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class Invoke
{
    /// <summary>Creates an invocation matcher.</summary>
    public static InvocationMatcher Method() => new();

    /// <summary>Creates an invocation matcher for specific method name.</summary>
    public static InvocationMatcher Method(string name) => new InvocationMatcher().Named(name);
}

/// <summary>
///     Fluent matcher for IInvocationOperation - the bread and butter of analyzers.
/// </summary>
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

    /// <summary>Tests if an invocation matches all predicates.</summary>
    public bool Matches(IInvocationOperation? invocation)
    {
        if (invocation is null)
            return false;

        foreach (var predicate in _predicates)
        {
            if (!predicate(invocation))
                return false;
        }

        return true;
    }

    /// <summary>Tests if an operation is a matching invocation.</summary>
    public bool Matches(IOperation? operation) =>
        operation is IInvocationOperation invocation && Matches(invocation);

    // Method name matching
    /// <summary>Matches invocations of method with exact name.</summary>
    public InvocationMatcher Named(string name) =>
        AddPredicate(i => i.TargetMethod.Name == name);

    /// <summary>Matches invocations where method name starts with prefix.</summary>
    public InvocationMatcher NameStartsWith(string prefix) =>
        AddPredicate(i => i.TargetMethod.Name.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>Matches invocations where method name ends with suffix.</summary>
    public InvocationMatcher NameEndsWith(string suffix) =>
        AddPredicate(i => i.TargetMethod.Name.EndsWith(suffix, StringComparison.Ordinal));

    /// <summary>Matches invocations where method name contains substring.</summary>
    public InvocationMatcher NameContains(string substring) =>
        AddPredicate(i => i.TargetMethod.Name.Contains(substring, StringComparison.Ordinal));

    // Receiver type matching
    /// <summary>Matches invocations on specified type name.</summary>
    public InvocationMatcher OnType(string typeName) =>
        AddPredicate(i => GetReceiverTypeName(i) == typeName);

    /// <summary>Matches invocations where receiver inherits from base type.</summary>
    public InvocationMatcher OnTypeInheritingFrom(string baseTypeName) =>
        AddPredicate(i =>
        {
            var receiverType = GetReceiverType(i);
            return receiverType is not null && InheritsFromName(receiverType, baseTypeName);
        });

    /// <summary>Matches invocations where receiver implements interface.</summary>
    public InvocationMatcher OnTypeImplementing(string interfaceName) =>
        AddPredicate(i =>
        {
            var receiverType = GetReceiverType(i);
            return receiverType is not null && ImplementsInterface(receiverType, interfaceName);
        });

    // Method characteristics
    /// <summary>Matches extension method invocations.</summary>
    public InvocationMatcher Extension() =>
        AddPredicate(i => i.TargetMethod.IsExtensionMethod);

    /// <summary>Matches non-extension method invocations.</summary>
    public InvocationMatcher NotExtension() =>
        AddPredicate(i => !i.TargetMethod.IsExtensionMethod);

    /// <summary>Matches static method invocations.</summary>
    public InvocationMatcher Static() =>
        AddPredicate(i => i.TargetMethod.IsStatic && !i.TargetMethod.IsExtensionMethod);

    /// <summary>Matches instance method invocations.</summary>
    public InvocationMatcher Instance() =>
        AddPredicate(i => !i.TargetMethod.IsStatic || i.TargetMethod.IsExtensionMethod);

    /// <summary>Matches async method invocations.</summary>
    public InvocationMatcher Async() =>
        AddPredicate(i => i.TargetMethod.IsAsync);

    /// <summary>Matches generic method invocations.</summary>
    public InvocationMatcher Generic() =>
        AddPredicate(i => i.TargetMethod.IsGenericMethod);

    // Return type
    /// <summary>Matches invocations returning void.</summary>
    public InvocationMatcher ReturningVoid() =>
        AddPredicate(i => i.TargetMethod.ReturnsVoid);

    /// <summary>Matches invocations returning Task/ValueTask.</summary>
    public InvocationMatcher ReturningTask() =>
        AddPredicate(i => i.TargetMethod.ReturnType.Name is "Task" or "ValueTask" ||
                         i.TargetMethod.ReturnType.OriginalDefinition.Name is "Task" or "ValueTask");

    /// <summary>Matches invocations returning specified type name.</summary>
    public InvocationMatcher Returning(string typeName) =>
        AddPredicate(i => i.TargetMethod.ReturnType.Name == typeName);

    // Arguments
    /// <summary>Matches invocations with specified argument count.</summary>
    public InvocationMatcher WithArguments(int count) =>
        AddPredicate(i => i.Arguments.Length == count);

    /// <summary>Matches invocations with no arguments.</summary>
    public InvocationMatcher WithNoArguments() =>
        AddPredicate(i => i.Arguments.Length == 0);

    /// <summary>Matches invocations with at least specified argument count.</summary>
    public InvocationMatcher WithMinArguments(int count) =>
        AddPredicate(i => i.Arguments.Length >= count);

    /// <summary>Matches invocations with constant argument at index.</summary>
    public InvocationMatcher WithConstantArg(int index) =>
        AddPredicate(i => i.Arguments.Length > index &&
                         i.Arguments[index].Value.ConstantValue.HasValue);

    /// <summary>Matches invocations with constant string argument at index.</summary>
    public InvocationMatcher WithConstantStringArg(int index) =>
        AddPredicate(i => i.Arguments.Length > index &&
                         i.Arguments[index].Value.ConstantValue is { HasValue: true, Value: string });

    /// <summary>Matches invocations with null argument at index.</summary>
    public InvocationMatcher WithNullArg(int index) =>
        AddPredicate(i => i.Arguments.Length > index &&
                         i.Arguments[index].Value.ConstantValue is { HasValue: true, Value: null });

    /// <summary>Matches invocations with argument of specified type at index.</summary>
    public InvocationMatcher WithArgOfType(int index, string typeName) =>
        AddPredicate(i => i.Arguments.Length > index &&
                         i.Arguments[index].Value.Type?.Name == typeName);

    /// <summary>Matches invocations with all constant arguments.</summary>
    public InvocationMatcher WithAllConstantArgs() =>
        AddPredicate(i => AllArgumentsConstant(i));

    // Namespace
    /// <summary>Matches invocations to methods in specified namespace.</summary>
    public InvocationMatcher InNamespace(string namespaceName) =>
        AddPredicate(i => i.TargetMethod.ContainingNamespace?.ToDisplayString() == namespaceName);

    /// <summary>Matches invocations to methods in namespace starting with prefix.</summary>
    public InvocationMatcher InNamespaceStartingWith(string prefix) =>
        AddPredicate(i => i.TargetMethod.ContainingNamespace?.ToDisplayString()
            ?.StartsWith(prefix, StringComparison.Ordinal) == true);

    // Common patterns
    /// <summary>Matches LINQ extension method invocations.</summary>
    public InvocationMatcher Linq() =>
        InNamespace("System.Linq").Extension();

    /// <summary>Matches string method invocations.</summary>
    public InvocationMatcher OnString() => OnType("String");

    /// <summary>Matches Task method invocations.</summary>
    public InvocationMatcher OnTask() => OnType("Task");

    /// <summary>Matches Console method invocations.</summary>
    public InvocationMatcher OnConsole() => OnType("Console");

    /// <summary>Adds custom matching condition.</summary>
    public InvocationMatcher Where(Func<IInvocationOperation, bool> predicate) =>
        AddPredicate(predicate);

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
        {
            if (iface.Name == name || iface.ToDisplayString() == name)
                return true;
        }

        return false;
    }

    private static bool AllArgumentsConstant(IInvocationOperation invocation)
    {
        foreach (var arg in invocation.Arguments)
        {
            if (!arg.Value.ConstantValue.HasValue)
                return false;
        }

        return true;
    }
}
