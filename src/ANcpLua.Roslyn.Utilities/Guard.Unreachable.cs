using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class Guard
{
    /// <summary>
    ///     Throws an exception indicating unreachable code was executed.
    /// </summary>
    /// <param name="message">Optional message describing why this code should be unreachable.</param>
    /// <param name="memberName">The calling member name (automatically captured).</param>
    /// <param name="filePath">The source file path (automatically captured).</param>
    /// <param name="lineNumber">The line number (automatically captured).</param>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    /// <example>
    ///     <code>
    /// switch (status)
    /// {
    ///     case Status.Active: return "Active";
    ///     case Status.Inactive: return "Inactive";
    ///     default: Guard.Unreachable();
    /// }
    /// </code>
    /// </example>
    [DoesNotReturn]
    public static void Unreachable(
        string? message = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        ThrowInvalidOperation(BuildUnreachableMessage(message, memberName, filePath, lineNumber));
    }

    /// <summary>
    ///     Throws an exception indicating unreachable code was executed.
    ///     Use this overload in expression contexts where a return value is expected.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="message">Optional message describing why this code should be unreachable.</param>
    /// <param name="memberName">The calling member name (automatically captured).</param>
    /// <param name="filePath">The source file path (automatically captured).</param>
    /// <param name="lineNumber">The line number (automatically captured).</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    /// <example>
    ///     <code>
    /// return status switch
    /// {
    ///     Status.Active => "Active",
    ///     Status.Inactive => "Inactive",
    ///     _ => Guard.Unreachable&lt;string&gt;()
    /// };
    /// </code>
    /// </example>
    [DoesNotReturn]
    public static T Unreachable<T>(
        string? message = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        throw new InvalidOperationException(BuildUnreachableMessage(message, memberName, filePath, lineNumber));
    }

    /// <summary>
    ///     Throws an exception if the condition is <c>true</c>, indicating unreachable code was executed.
    /// </summary>
    /// <param name="condition">The condition that should never be true.</param>
    /// <param name="message">Optional message describing why this code should be unreachable.</param>
    /// <param name="conditionExpression">The condition expression (automatically captured).</param>
    /// <param name="memberName">The calling member name (automatically captured).</param>
    /// <param name="filePath">The source file path (automatically captured).</param>
    /// <param name="lineNumber">The line number (automatically captured).</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="condition" /> is <c>true</c>.</exception>
    /// <example>
    ///     <code>
    /// Guard.UnreachableIf(list.Count &lt; 0, "Count should never be negative");
    /// </code>
    /// </example>
    public static void UnreachableIf(
        [DoesNotReturnIf(true)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))]
        string conditionExpression = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (condition)
            ThrowInvalidOperation(
                $"{BuildUnreachableMessage(message, memberName, filePath, lineNumber)}. Condition: {conditionExpression}");
    }
}
