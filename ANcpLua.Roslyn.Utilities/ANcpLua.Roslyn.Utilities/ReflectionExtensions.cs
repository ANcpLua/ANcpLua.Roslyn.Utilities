using System.Reflection;
using System.Runtime.ExceptionServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="MethodInfo" /> and <see cref="Type" /> providing
///     multi-TFM reflection utilities that abstract over runtime differences.
/// </summary>
/// <remarks>
///     <para>
///         These helpers address common pain points when writing reflection-based code that must
///         work on both .NET Framework / netstandard2.0 and modern .NET:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Exception unwrapping:</b> <see cref="InvokeUnwrapped" /> avoids
///                 <see cref="TargetInvocationException" /> wrapping, letting original exceptions
///                 propagate with their stack traces intact.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Generic method lookup:</b> <see cref="GetMethodFromGenericDefinition" />
///                 finds a method on a specialized generic type from its open generic definition's method.
///             </description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ReflectionExtensions
{
    /// <summary>
    ///     Invokes a method without wrapping exceptions in <see cref="TargetInvocationException" />.
    /// </summary>
    /// <param name="method">The method to invoke.</param>
    /// <param name="target">
    ///     The object on which to invoke the method, or <c>null</c> for static methods.
    /// </param>
    /// <param name="arguments">The arguments to pass to the method.</param>
    /// <returns>The return value of the invoked method, or <c>null</c> for void methods.</returns>
    /// <remarks>
    ///     <para>
    ///         On .NET 6+ this uses <c>BindingFlags.DoNotWrapExceptions</c> to avoid
    ///         wrapping the original exception. On older runtimes it catches
    ///         <see cref="TargetInvocationException" /> and re-throws the inner exception
    ///         using <see cref="ExceptionDispatchInfo" /> to preserve the original stack trace.
    ///     </para>
    ///     <para>
    ///         This is particularly useful when building runtime dispatch tables where the caller
    ///         expects to handle the original exception type, not a reflection wrapper.
    ///     </para>
    /// </remarks>
    public static object? InvokeUnwrapped(this MethodInfo method, object? target, params object?[] arguments)
    {
#if NET6_0_OR_GREATER
        return method.Invoke(target, BindingFlags.DoNotWrapExceptions, binder: null, arguments, culture: null);
#else
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw; // unreachable, but required by the compiler
        }
#endif
    }

    /// <summary>
    ///     Finds a method on a specialized generic type that corresponds to a method from
    ///     the open generic type definition.
    /// </summary>
    /// <param name="specializedType">
    ///     The closed generic type (e.g., <c>typeof(List&lt;int&gt;)</c>).
    /// </param>
    /// <param name="genericMethodDefinition">
    ///     A <see cref="MethodInfo" /> obtained from the open generic type definition
    ///     (e.g., a method from <c>typeof(List&lt;&gt;)</c>).
    /// </param>
    /// <returns>
    ///     The <see cref="MethodInfo" /> on the specialized type that corresponds to the given
    ///     generic method definition.
    /// </returns>
    /// <exception cref="MissingMethodException">
    ///     Thrown when no matching method is found on the specialized type.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         On .NET 6+ this uses <c>Type.GetMemberWithSameMetadataDefinitionAs</c> for an
    ///         efficient single-call lookup. On older runtimes it falls back to scanning all methods
    ///         and matching by <see cref="MemberInfo.MetadataToken" />.
    ///     </para>
    ///     <para>
    ///         This is useful when you have a <see cref="MethodInfo" /> from an open generic type
    ///         (obtained via <c>typeof(Foo&lt;&gt;).GetMethod("Bar")</c>) and need to invoke it
    ///         on a concrete instantiation (e.g., <c>Foo&lt;int&gt;</c>).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var openMethod = typeof(Handler&lt;&gt;).GetMethod("Execute");
    /// var closedType = typeof(Handler&lt;MyMessage&gt;);
    /// var closedMethod = closedType.GetMethodFromGenericDefinition(openMethod);
    /// closedMethod.InvokeUnwrapped(handlerInstance, message);
    /// </code>
    /// </example>
    public static MethodInfo GetMethodFromGenericDefinition(this Type specializedType, MethodInfo genericMethodDefinition)
    {
#if NET6_0_OR_GREATER
        return (MethodInfo)specializedType.GetMemberWithSameMetadataDefinitionAs(genericMethodDefinition);
#else
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        foreach (var m in specializedType.GetMethods(all))
        {
            if (m.MetadataToken == genericMethodDefinition.MetadataToken)
                return m;
        }

        throw new MissingMethodException(specializedType.FullName, genericMethodDefinition.Name);
#endif
    }
}
