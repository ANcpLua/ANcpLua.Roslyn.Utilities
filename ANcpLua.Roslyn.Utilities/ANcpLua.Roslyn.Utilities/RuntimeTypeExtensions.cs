using System.Reflection;
using System.Threading.Tasks;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="Type" /> providing runtime type checking utilities
///     for common async and generic patterns.
/// </summary>
/// <remarks>
///     <para>
///         These helpers address common reflection patterns when building runtime dispatch tables,
///         handler registries, or middleware pipelines that need to inspect return types
///         and generic type arguments at runtime.
///     </para>
///     <para>
///         For compile-time Roslyn symbol analysis of task types, see <c>TypeSymbolExtensions</c>
///         and <c>AwaitableContext</c>. This class operates on <see cref="System.Type" /> at runtime.
///     </para>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class RuntimeTypeExtensions
{
    /// <summary>
    ///     Determines whether the type is a closed generic <see cref="Task{TResult}" />.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    ///     <c>true</c> if the type is <c>Task&lt;T&gt;</c> for some <c>T</c>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Returns <c>false</c> for non-generic <see cref="Task" />. Use <see cref="IsTaskLike" />
    ///     to match both generic and non-generic task types.
    /// </remarks>
    public static bool IsGenericTask(this Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);

    /// <summary>
    ///     Determines whether the type is a closed generic <see cref="ValueTask{TResult}" />.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    ///     <c>true</c> if the type is <c>ValueTask&lt;T&gt;</c> for some <c>T</c>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Returns <c>false</c> for non-generic <see cref="ValueTask" />. Use <see cref="IsTaskLike" />
    ///     to match both generic and non-generic task types.
    /// </remarks>
    public static bool IsGenericValueTask(this Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>);

    /// <summary>
    ///     Determines whether the type is any of the common task types:
    ///     <see cref="Task" />, <see cref="Task{TResult}" />,
    ///     <see cref="ValueTask" />, or <see cref="ValueTask{TResult}" />.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the type is a task-like type; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     This is useful when building handler registries that need to determine whether
    ///     a method is async based on its return type.
    /// </remarks>
    public static bool IsTaskLike(this Type type) =>
        type == typeof(Task) ||
        type == typeof(ValueTask) ||
        type.IsGenericTask() ||
        type.IsGenericValueTask();

    /// <summary>
    ///     Gets the result type from a <see cref="Task{TResult}" /> or <see cref="ValueTask{TResult}" /> type.
    /// </summary>
    /// <param name="type">The task type to extract the result type from.</param>
    /// <returns>
    ///     The <c>TResult</c> type argument if <paramref name="type" /> is <c>Task&lt;T&gt;</c>
    ///     or <c>ValueTask&lt;T&gt;</c>; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     Returns <c>null</c> for non-generic <see cref="Task" />, non-generic <see cref="ValueTask" />,
    ///     and all non-task types.
    /// </remarks>
    public static Type? GetTaskResultType(this Type type) =>
        type.IsGenericTask() || type.IsGenericValueTask()
            ? type.GetGenericArguments()[0]
            : null;

    /// <summary>
    ///     Determines whether the type implements a specific open generic interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="openGenericInterface">
    ///     The open generic interface type (e.g., <c>typeof(IHandler&lt;&gt;)</c>).
    /// </param>
    /// <returns>
    ///     <c>true</c> if the type implements the specified open generic interface; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This is useful for handler discovery patterns where you scan types for implementations
    ///     of a generic interface (e.g., <c>IMessageHandler&lt;T&gt;</c>) without knowing the
    ///     concrete type argument.
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (type.ImplementsOpenGeneric(typeof(IMessageHandler&lt;&gt;)))
    /// {
    ///     var handlerInterfaces = type.GetClosedImplementations(typeof(IMessageHandler&lt;&gt;));
    /// }
    /// </code>
    /// </example>
    public static bool ImplementsOpenGeneric(this Type type, Type openGenericInterface)
    {
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == openGenericInterface)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets all closed implementations of an open generic interface from a type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="openGenericInterface">
    ///     The open generic interface type (e.g., <c>typeof(IHandler&lt;&gt;)</c>).
    /// </param>
    /// <returns>
    ///     An array of closed generic interface types that the type implements.
    ///     Empty if the type does not implement the interface.
    /// </returns>
    /// <remarks>
    ///     A type may implement the same open generic interface multiple times with different
    ///     type arguments (e.g., <c>class MyHandler : IHandler&lt;Foo&gt;, IHandler&lt;Bar&gt;</c>).
    ///     This method returns all such implementations.
    /// </remarks>
    /// <example>
    ///     <code>
    /// // class MyHandler : IMessageHandler&lt;StartCommand&gt;, IMessageHandler&lt;StopCommand&gt;
    /// var implementations = typeof(MyHandler).GetClosedImplementations(typeof(IMessageHandler&lt;&gt;));
    /// // returns [IMessageHandler&lt;StartCommand&gt;, IMessageHandler&lt;StopCommand&gt;]
    /// foreach (var impl in implementations)
    /// {
    ///     var messageType = impl.GetGenericArguments()[0];
    /// }
    /// </code>
    /// </example>
    public static Type[] GetClosedImplementations(this Type type, Type openGenericInterface)
    {
        var interfaces = type.GetInterfaces();
        var results = new List<Type>();

        foreach (var iface in interfaces)
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == openGenericInterface)
                results.Add(iface);
        }

        return results.ToArray();
    }
}
