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

    /// <summary>
    ///     Validates that a <see cref="Type" /> is not a <see cref="Nullable{T}" />.
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type" /> is a <see cref="Nullable{T}" />.</exception>
    /// <example>
    ///     <code>
    /// public void Register(Type type)
    /// {
    ///     Guard.NotNullableType(type);
    ///     _registry.Add(type);
    /// }
    /// </code>
    /// </example>
    public static Type NotNullableType(
        [NotNull] Type? type,
        [CallerArgumentExpression(nameof(type))]
        string? paramName = null)
    {
        NotNull(type, paramName);

        return Nullable.GetUnderlyingType(type) is not null
            ? throw new ArgumentException("Nullable types are not supported.", paramName)
            : type;
    }

    /// <summary>
    ///     Validates that a <see cref="Type" /> is assignable to the specified type.
    /// </summary>
    /// <typeparam name="T">The type that <paramref name="type" /> must be assignable to.</typeparam>
    /// <param name="type">The type to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="type" /> is not assignable to <typeparamref name="T" />
    ///     .
    /// </exception>
    public static Type AssignableTo<T>(
        [NotNull] Type? type,
        [CallerArgumentExpression(nameof(type))]
        string? paramName = null)
    {
        NotNull(type, paramName);

        return !typeof(T).IsAssignableFrom(type)
            ? throw new ArgumentException($"Type {type.Name} is not assignable to {typeof(T).Name}.", paramName)
            : type;
    }

    /// <summary>
    ///     Validates that a <see cref="Type" /> is assignable from the specified type.
    /// </summary>
    /// <typeparam name="T">The type that must be assignable to <paramref name="type" />.</typeparam>
    /// <param name="type">The type to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <typeparamref name="T" /> is not assignable to <paramref name="type" />
    ///     .
    /// </exception>
    public static Type AssignableFrom<T>(
        [NotNull] Type? type,
        [CallerArgumentExpression(nameof(type))]
        string? paramName = null)
    {
        NotNull(type, paramName);

        return !type.IsAssignableFrom(typeof(T))
            ? throw new ArgumentException($"Type {typeof(T).Name} is not assignable to {type.Name}.", paramName)
            : type;
    }
}
