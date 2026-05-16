using System.IO;
using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class Guard
{
    #region Enums

    /// <summary>
    ///     Validates that an enum value is defined and returns it.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated enum value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value" /> is not a defined enum value.</exception>
    /// <example>
    ///     <code>
    /// public void SetStatus(Status status)
    /// {
    ///     _status = Guard.DefinedEnum(status);
    /// }
    /// </code>
    /// </example>
    public static T DefinedEnum<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct, Enum
    {
#if NET5_0_OR_GREATER
        if (!Enum.IsDefined(value))
#else
        if (!Enum.IsDefined(typeof(T), value))
#endif
            throw new ArgumentOutOfRangeException(paramName, value,
                $"Undefined enum value for {typeof(T).Name}: {value}");

        return value;
    }

    #endregion

    #region Member Validation

    /// <summary>
    ///     Validates that an object is not <c>null</c> and that a member of that object is also not <c>null</c>.
    /// </summary>
    /// <typeparam name="TParam">The type of the parameter.</typeparam>
    /// <typeparam name="TMember">The type of the member.</typeparam>
    /// <param name="argument">The object to validate.</param>
    /// <param name="member">The member to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="memberName">The name of the member.</param>
    /// <returns>The validated member value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="argument" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="member" /> is <c>null</c>.</exception>
    /// <example>
    ///     <code>
    /// public void Process(Config? config)
    /// {
    ///     var connectionString = Guard.NotNullWithMember(config, config?.ConnectionString);
    /// }
    /// </code>
    /// </example>
    [return: NotNull]
    public static TMember NotNullWithMember<TParam, TMember>(
        [NotNull] TParam? argument,
        [NotNull] TMember? member,
        [CallerArgumentExpression(nameof(argument))]
        string? paramName = null,
        [CallerArgumentExpression(nameof(member))]
        string? memberName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
        return member is null
            ? throw new ArgumentException($"Member {memberName} of {paramName} is null", paramName)
            : member;
    }

    /// <summary>
    ///     Validates that a member of an object is not <c>null</c>.
    /// </summary>
    /// <typeparam name="TParam">The type of the parameter (must be non-null).</typeparam>
    /// <typeparam name="TMember">The type of the member.</typeparam>
    /// <param name="argument">The object containing the member.</param>
    /// <param name="member">The member to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="memberName">The name of the member.</param>
    /// <returns>The validated member value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="member" /> is <c>null</c>.</exception>
    /// <example>
    ///     <code>
    /// public void Process(Config config)
    /// {
    ///     var connectionString = Guard.MemberNotNull(config, config.ConnectionString);
    /// }
    /// </code>
    /// </example>
    [return: NotNull]
    public static TMember MemberNotNull<TParam, TMember>(
        TParam argument,
        [NotNull] TMember? member,
        [CallerArgumentExpression(nameof(argument))]
        string? paramName = null,
        [CallerArgumentExpression(nameof(member))]
        string? memberName = null)
        where TParam : notnull
    {
        _ = argument;
        return member is null
            ? throw new ArgumentException($"Member {memberName} of {paramName} is null", paramName)
            : member;
    }

    #endregion

    #region Unreachable

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

    #endregion

    #region Set Membership

    /// <summary>
    ///     Validates that a value is one of the allowed values and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="allowed">The set of allowed values.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not in <paramref name="allowed" />.</exception>
    /// <example>
    ///     <code>
    /// public void SetProtocol(string protocol)
    /// {
    ///     _protocol = Guard.OneOf(protocol, "http", "https", "ftp");
    /// }
    /// </code>
    /// </example>
    public static T OneOf<T>(
        T value,
        T[] allowed,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        foreach (var item in allowed)
            if (EqualityComparer<T>.Default.Equals(value, item))
                return value;

        var allowedStr = string.Join(", ", allowed);
        throw new ArgumentException($"Value must be one of: [{allowedStr}]. Actual: {value}.", paramName);
    }

    /// <summary>
    ///     Validates that a value is one of the allowed values and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="allowed">The set of allowed values.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not in <paramref name="allowed" />.</exception>
    public static T OneOf<T>(
        T value,
        HashSet<T> allowed,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (allowed.Contains(value))
            return value;

        var allowedStr = string.Join(", ", allowed);
        throw new ArgumentException($"Value must be one of: [{allowedStr}]. Actual: {value}.", paramName);
    }

    /// <summary>
    ///     Validates that a value is not one of the disallowed values and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="disallowed">The set of disallowed values.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is in <paramref name="disallowed" />.</exception>
    /// <example>
    ///     <code>
    /// public void SetPort(int port)
    /// {
    ///     _port = Guard.NotOneOf(port, 0, 80, 443); // Reserved ports
    /// }
    /// </code>
    /// </example>
    public static T NotOneOf<T>(
        T value,
        T[] disallowed,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        foreach (var item in disallowed)
            if (EqualityComparer<T>.Default.Equals(value, item))
            {
                var disallowedStr = string.Join(", ", disallowed);
                throw new ArgumentException($"Value must not be one of: [{disallowedStr}]. Actual: {value}.",
                    paramName);
            }

        return value;
    }

    /// <summary>
    ///     Validates that a value is not one of the disallowed values and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="disallowed">The set of disallowed values.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is in <paramref name="disallowed" />.</exception>
    public static T NotOneOf<T>(
        T value,
        HashSet<T> disallowed,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (disallowed.Contains(value))
        {
            var disallowedStr = string.Join(", ", disallowed);
            throw new ArgumentException($"Value must not be one of: [{disallowedStr}]. Actual: {value}.", paramName);
        }

        return value;
    }

    #endregion
}
