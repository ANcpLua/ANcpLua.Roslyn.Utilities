namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for nullable reference types and nullable value types,
///     providing fluent alternatives to common null-handling patterns.
/// </summary>
/// <remarks>
///     <para>
///         These extensions help reduce boilerplate when working with nullable values,
///         providing a more functional style of null handling.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Transformation:</b> <see cref="Select{T,TResult}(T?, Func{T,TResult})" /> for mapping nullable values.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Filtering:</b> <see cref="Where{T}(T?, Func{T,bool})" /> for conditional unwrapping.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Side effects:</b> <see cref="Do{T}(T?, Action{T})" /> for executing actions on non-null values.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Coalescing:</b> <see cref="Or{T}(T?, T)" /> and <see cref="OrElse{T}(T?, Func{T})" /> for default values.
///             </description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class NullableExtensions
{
    // ========== Reference Type Extensions ==========

    /// <summary>
    ///     Transforms a nullable reference value using the specified selector.
    /// </summary>
    /// <typeparam name="T">The type of the source value.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="value">The nullable value to transform.</param>
    /// <param name="selector">The transformation function.</param>
    /// <returns>
    ///     The result of <paramref name="selector" /> if <paramref name="value" /> is not <c>null</c>;
    ///     otherwise, <c>default</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This provides a more functional alternative to the null-conditional operator
    ///         when you need to transform a value.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Get length of a nullable string
    /// int? length = nullableString.Select(s => s.Length);
    ///
    /// // Chain transformations
    /// var result = user.Select(u => u.Address).Select(a => a.City);
    /// </code>
    /// </example>
    /// <seealso cref="SelectMany{T,TResult}(T?, Func{T,TResult?})" />
    public static TResult? Select<T, TResult>(this T? value, Func<T, TResult> selector)
        where T : class
        => value is not null ? selector(value) : default;

    /// <summary>
    ///     Transforms a nullable reference value using a selector that also returns nullable.
    /// </summary>
    /// <typeparam name="T">The type of the source value.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="value">The nullable value to transform.</param>
    /// <param name="selector">The transformation function that returns a nullable result.</param>
    /// <returns>
    ///     The result of <paramref name="selector" /> if <paramref name="value" /> is not <c>null</c>;
    ///     otherwise, <c>default</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Use this when the selector itself returns a nullable value, flattening the result.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Navigate nested nullable properties
    /// var city = order.SelectMany(o => o.Customer).SelectMany(c => c.Address).Select(a => a.City);
    /// </code>
    /// </example>
    /// <seealso cref="Select{T,TResult}(T?, Func{T,TResult})" />
    public static TResult? SelectMany<T, TResult>(this T? value, Func<T, TResult?> selector)
        where T : class
        where TResult : class
        => value is not null ? selector(value) : default;

    /// <summary>
    ///     Filters a nullable value based on a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The nullable value to filter.</param>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>
    ///     The original <paramref name="value" /> if not <c>null</c> and <paramref name="predicate" /> returns <c>true</c>;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// // Only keep non-empty strings
    /// var nonEmpty = str.Where(s => s.Length > 0);
    ///
    /// // Filter based on conditions
    /// var validUser = user.Where(u => u.IsActive);
    /// </code>
    /// </example>
    /// <seealso cref="Select{T,TResult}(T?, Func{T,TResult})" />
    public static T? Where<T>(this T? value, Func<T, bool> predicate)
        where T : class
        => value is not null && predicate(value) ? value : null;

    /// <summary>
    ///     Executes an action if the value is not <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="action">The action to execute with the value.</param>
    /// <returns>The original <paramref name="value" /> (for chaining).</returns>
    /// <remarks>
    ///     <para>
    ///         This method enables side effects in a chain of nullable operations.
    ///         The original value is returned to allow continued chaining.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Log and continue processing
    /// var result = item
    ///     .Do(i => logger.Log(i.Name))
    ///     .Select(i => Process(i));
    ///
    /// // Conditional side effects
    /// user.Do(u => NotifyUser(u));
    /// </code>
    /// </example>
    public static T? Do<T>(this T? value, Action<T> action) where T : class
    {
        if (value is not null)
            action(value);
        return value;
    }

    /// <summary>
    ///     Returns the value if not <c>null</c>, otherwise returns the specified default.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="defaultValue">The default value to return if <paramref name="value" /> is <c>null</c>.</param>
    /// <returns>
    ///     The <paramref name="value" /> if not <c>null</c>; otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This is equivalent to the null-coalescing operator (<c>??</c>) but provides
    ///         a more fluent syntax for chaining.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var name = user.Select(u => u.Name).Or("Guest");
    /// </code>
    /// </example>
    /// <seealso cref="OrElse{T}(T?, Func{T})" />
    [return: NotNull]
    public static T Or<T>(this T? value, [NotNull] T defaultValue) where T : class
        => value ?? defaultValue;

    /// <summary>
    ///     Returns the value if not <c>null</c>, otherwise computes a default using the factory.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="factory">A factory function to compute the default value.</param>
    /// <returns>
    ///     The <paramref name="value" /> if not <c>null</c>; otherwise, the result of <paramref name="factory" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Use this when the default value is expensive to compute and should only
    ///         be calculated when needed.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var config = cachedConfig.OrElse(() => LoadConfigFromDisk());
    /// </code>
    /// </example>
    /// <seealso cref="Or{T}(T?, T)" />
    [return: NotNull]
    public static T OrElse<T>(this T? value, [NotNull] Func<T> factory) where T : class
        => value ?? factory();

    /// <summary>
    ///     Throws an exception if the value is <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="exceptionFactory">A factory function to create the exception to throw.</param>
    /// <returns>The non-null <paramref name="value" />.</returns>
    /// <exception cref="Exception">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <example>
    ///     <code>
    /// var user = GetUser(id).OrThrow(() => new NotFoundException($"User {id} not found"));
    /// </code>
    /// </example>
    /// <seealso cref="Or{T}(T?, T)" />
    [return: NotNull]
    public static T OrThrow<T>(this T? value, Func<Exception> exceptionFactory) where T : class
        => value ?? throw exceptionFactory();

    /// <summary>
    ///     Converts a nullable reference to a single-element or empty sequence.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <returns>
    ///     A sequence containing <paramref name="value" /> if not <c>null</c>; otherwise, an empty sequence.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Useful for integrating nullable values with LINQ operations that expect sequences.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Combine multiple nullable values into a sequence
    /// var items = value1.ToEnumerable()
    ///     .Concat(value2.ToEnumerable())
    ///     .Concat(value3.ToEnumerable());
    /// </code>
    /// </example>
    public static IEnumerable<T> ToEnumerable<T>(this T? value) where T : class
    {
        if (value is not null)
            yield return value;
    }

    /// <summary>
    ///     Matches the nullable value against success and failure handlers.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="some">The handler to invoke if <paramref name="value" /> is not <c>null</c>.</param>
    /// <param name="none">The handler to invoke if <paramref name="value" /> is <c>null</c>.</param>
    /// <returns>The result of whichever handler is invoked.</returns>
    /// <example>
    ///     <code>
    /// var message = user.Match(
    ///     some: u => $"Welcome, {u.Name}!",
    ///     none: () => "Please log in"
    /// );
    /// </code>
    /// </example>
    public static TResult Match<T, TResult>(
        this T? value,
        Func<T, TResult> some,
        Func<TResult> none) where T : class
        => value is not null ? some(value) : none();

    // ========== Nullable Value Type Extensions ==========

    /// <summary>
    ///     Transforms a nullable value type using the specified selector.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="value">The nullable value to transform.</param>
    /// <param name="selector">The transformation function.</param>
    /// <returns>
    ///     The result of <paramref name="selector" /> if <paramref name="value" /> has a value;
    ///     otherwise, <c>default</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// int? count = GetCount();
    /// string? countStr = count.Select(c => c.ToString());
    /// </code>
    /// </example>
    public static TResult? Select<T, TResult>(this T? value, Func<T, TResult> selector)
        where T : struct
        => value.HasValue ? selector(value.Value) : default;

    /// <summary>
    ///     Transforms a nullable value type using a selector that returns nullable.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <typeparam name="TResult">The underlying type of the result.</typeparam>
    /// <param name="value">The nullable value to transform.</param>
    /// <param name="selector">The transformation function.</param>
    /// <returns>
    ///     The result of <paramref name="selector" /> if <paramref name="value" /> has a value;
    ///     otherwise, <c>null</c>.
    /// </returns>
    public static TResult? SelectMany<T, TResult>(this T? value, Func<T, TResult?> selector)
        where T : struct
        where TResult : struct
        => value.HasValue ? selector(value.Value) : null;

    /// <summary>
    ///     Filters a nullable value type based on a predicate.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value to filter.</param>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>
    ///     The original <paramref name="value" /> if it has a value and <paramref name="predicate" /> returns <c>true</c>;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// int? positiveOnly = number.Where(n => n > 0);
    /// </code>
    /// </example>
    public static T? Where<T>(this T? value, Func<T, bool> predicate)
        where T : struct
        => value.HasValue && predicate(value.Value) ? value : null;

    /// <summary>
    ///     Executes an action if the nullable value type has a value.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="action">The action to execute with the value.</param>
    /// <returns>The original <paramref name="value" /> (for chaining).</returns>
    public static T? Do<T>(this T? value, Action<T> action) where T : struct
    {
        if (value.HasValue)
            action(value.Value);
        return value;
    }

    /// <summary>
    ///     Returns the value if it has a value, otherwise returns the specified default.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>
    ///     The <paramref name="value" /> if it has a value; otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// int count = nullableCount.Or(0);
    /// </code>
    /// </example>
    public static T Or<T>(this T? value, T defaultValue) where T : struct
        => value ?? defaultValue;

    /// <summary>
    ///     Returns the value if it has a value, otherwise computes a default using the factory.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="factory">A factory function to compute the default value.</param>
    /// <returns>
    ///     The <paramref name="value" /> if it has a value; otherwise, the result of <paramref name="factory" />.
    /// </returns>
    public static T OrElse<T>(this T? value, Func<T> factory) where T : struct
        => value ?? factory();

    /// <summary>
    ///     Converts a nullable value type to a single-element or empty sequence.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <returns>
    ///     A sequence containing <paramref name="value" /> if it has a value; otherwise, an empty sequence.
    /// </returns>
    public static IEnumerable<T> ToEnumerable<T>(this T? value) where T : struct
    {
        if (value.HasValue)
            yield return value.Value;
    }

    /// <summary>
    ///     Matches the nullable value type against success and failure handlers.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="some">The handler to invoke if <paramref name="value" /> has a value.</param>
    /// <param name="none">The handler to invoke if <paramref name="value" /> is <c>null</c>.</param>
    /// <returns>The result of whichever handler is invoked.</returns>
    public static TResult Match<T, TResult>(
        this T? value,
        Func<T, TResult> some,
        Func<TResult> none) where T : struct
        => value.HasValue ? some(value.Value) : none();

    // ========== Conversion Helpers ==========

    /// <summary>
    ///     Converts a nullable reference type to a boolean indicating presence.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <returns><c>true</c> if <paramref name="value" /> is not <c>null</c>; otherwise, <c>false</c>.</returns>
    /// <example>
    ///     <code>
    /// bool hasUser = user.HasValue();
    /// </code>
    /// </example>
    public static bool HasValue<T>(this T? value) where T : class
        => value is not null;

    /// <summary>
    ///     Returns <c>null</c> if the value equals the specified sentinel value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="sentinel">The sentinel value that should be treated as <c>null</c>.</param>
    /// <returns>
    ///     <c>null</c> if <paramref name="value" /> equals <paramref name="sentinel" />;
    ///     otherwise, <paramref name="value" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Useful for converting sentinel values (like -1, empty string, etc.) to proper nulls.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Convert -1 to null
    /// int? index = GetIndex().NullIf(-1);
    ///
    /// // Convert empty string to null
    /// string? name = GetName().NullIf("");
    /// </code>
    /// </example>
    public static T? NullIf<T>(this T value, T sentinel) where T : class
        => EqualityComparer<T>.Default.Equals(value, sentinel) ? null : value;

    /// <summary>
    ///     Returns <c>null</c> if the value type equals the specified sentinel value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="sentinel">The sentinel value that should be treated as <c>null</c>.</param>
    /// <returns>
    ///     <c>null</c> if <paramref name="value" /> equals <paramref name="sentinel" />;
    ///     otherwise, <paramref name="value" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// int? count = GetCount().NullIfValue(0);
    /// </code>
    /// </example>
    public static T? NullIfValue<T>(this T value, T sentinel) where T : struct
        => EqualityComparer<T>.Default.Equals(value, sentinel) ? null : value;
}
