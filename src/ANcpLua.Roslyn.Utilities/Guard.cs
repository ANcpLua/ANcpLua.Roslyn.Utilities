using System.IO;
using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides guard clauses for argument validation with clean, expressive syntax.
/// </summary>
/// <remarks>
///     <para>
///         This class eliminates common boilerplate patterns like <c>value ?? throw new ArgumentNullException()</c>
///         and null-forgiving operator (<c>!</c>) usage by providing fluent guard methods that return the validated value.
///     </para>
///     <para>
///         All methods use <see cref="CallerArgumentExpressionAttribute" /> to automatically capture
///         the parameter name, eliminating the need to pass <c>nameof(parameter)</c>.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="NotNull{T}" /> - Validates non-null and returns the value (throws on null)
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="NotNullOrElse{T}(T?, T)" /> - Returns value or fallback (reference/value types)
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="NotNullOrElse{T}(T?, Func{T})" /> - Returns value or lazily computed fallback
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="NotNullOrEmpty(string?, string?)" /> - Validates non-null and non-empty strings
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="NotNullOrWhiteSpace(string?, string?)" /> - Validates meaningful string content
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// // Before: Manual validation with verbose syntax
/// public void Process(string? name, IService? service)
/// {
///     _name = name ?? throw new ArgumentNullException(nameof(name));
///     _service = service ?? throw new ArgumentNullException(nameof(service));
/// }
/// 
/// // After: Clean, expressive guards
/// public void Process(string? name, IService? service)
/// {
///     _name = Guard.NotNull(name);
///     _service = Guard.NotNull(service);
/// }
/// </code>
/// </example>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class Guard
{
    /// <summary>
    ///     Validates that a value is not <c>null</c> and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated non-null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <remarks>
    ///     <para>
    ///         This method eliminates the need for the null-forgiving operator (<c>!</c>) and
    ///         verbose <c>?? throw</c> patterns while maintaining proper null-state analysis.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Validates and returns the value, throws if null
    /// var validated = Guard.NotNull(possiblyNullValue);
    ///
    /// // Can be used inline
    /// ProcessItem(Guard.NotNull(item));
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T NotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        return value ?? ThrowArgumentNull<T>(paramName);
    }

    /// <summary>
    ///     Returns the value if not <c>null</c>, otherwise returns the specified default value.
    /// </summary>
    /// <typeparam name="T">The type of the value (must be a reference type).</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="defaultValue">The default value to return if <paramref name="value" /> is <c>null</c>.</param>
    /// <returns>The original <paramref name="value" /> if not <c>null</c>; otherwise, <paramref name="defaultValue" />.</returns>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="NotNull{T}" />, this method does not throw an exception.
    ///         Use this when a fallback value is acceptable.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var name = Guard.NotNullOrElse(user?.Name, "Anonymous");
    /// var service = Guard.NotNullOrElse(optionalService, DefaultService.Instance);
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrElse{T}(T?, Func{T})" />
    public static T NotNullOrElse<T>(T? value, T defaultValue) where T : class
    {
        return value ?? defaultValue;
    }

    /// <summary>
    ///     Returns the value if not <c>null</c>, otherwise computes a default using the factory.
    /// </summary>
    /// <typeparam name="T">The type of the value (must be a reference type).</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="factory">
    ///     A factory function to compute the default value (only called if <paramref name="value" /> is
    ///     <c>null</c>).
    /// </param>
    /// <returns>
    ///     The original <paramref name="value" /> if not <c>null</c>; otherwise, the result of
    ///     <paramref name="factory" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Use this when the default value is expensive to compute and should only
    ///         be calculated when needed (lazy evaluation).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var config = Guard.NotNullOrElse(cachedConfig, () => LoadConfigFromDisk());
    /// var service = Guard.NotNullOrElse(injectedService, () => new ExpensiveService());
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrElse{T}(T?, T)" />
    public static T NotNullOrElse<T>(T? value, Func<T> factory) where T : class
    {
        return value ?? factory();
    }

    /// <summary>
    ///     Returns the value if it has a value, otherwise returns the specified default value.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value to check.</param>
    /// <param name="defaultValue">The default value to return if <paramref name="value" /> has no value.</param>
    /// <returns>The <paramref name="value" /> if it has a value; otherwise, <paramref name="defaultValue" />.</returns>
    /// <remarks>
    ///     <para>
    ///         This is the value type overload of <see cref="NotNullOrElse{T}(T?, T)" />.
    ///         Useful for nullable value types like <c>int?</c>, <c>bool?</c>, etc.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// int? maybeCount = GetCount();
    /// var count = Guard.NotNullOrElse(maybeCount, 0);
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrElse{T}(T?, Func{T})" />
    public static T NotNullOrElse<T>(T? value, T defaultValue) where T : struct
    {
        return value ?? defaultValue;
    }

    /// <summary>
    ///     Returns the value if it has a value, otherwise computes a default using the factory.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value to check.</param>
    /// <param name="factory">
    ///     A factory function to compute the default value (only called if <paramref name="value" /> has no
    ///     value).
    /// </param>
    /// <returns>The <paramref name="value" /> if it has a value; otherwise, the result of <paramref name="factory" />.</returns>
    /// <remarks>
    ///     <para>
    ///         Use this when the default value is expensive to compute and should only
    ///         be calculated when needed (lazy evaluation).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// int? maybeCount = GetCount();
    /// var count = Guard.NotNullOrElse(maybeCount, () => ComputeExpensiveDefault());
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrElse{T}(T?, T)" />
    public static T NotNullOrElse<T>(T? value, Func<T> factory) where T : struct
    {
        return value ?? factory();
    }

    /// <summary>
    ///     Validates that a string is not <c>null</c> or empty and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated non-null, non-empty string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty.</exception>
    /// <example>
    ///     <code>
    /// public void SetName(string? name)
    /// {
    ///     _name = Guard.NotNullOrEmpty(name);
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NotNullOrEmpty([NotNull] string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) ThrowArgumentNull(paramName);
        if (value.Length is 0) ThrowArgument("Value cannot be empty.", paramName);
        return value;
    }

    /// <summary>
    ///     Validates that a string is not <c>null</c>, empty, or whitespace-only and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated string containing meaningful content.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains only whitespace.</exception>
    /// <example>
    ///     <code>
    /// public void SetDescription(string? description)
    /// {
    ///     _description = Guard.NotNullOrWhiteSpace(description);
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NotNullOrWhiteSpace([NotNull] string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) ThrowArgumentNull(paramName);
        if (string.IsNullOrWhiteSpace(value)) ThrowArgument("Value cannot be empty or whitespace.", paramName);
        return value;
    }

    /// <summary>
    ///     Returns the string if not <c>null</c> or empty, otherwise returns the specified default value.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="defaultValue">The default value to return if <paramref name="value" /> is <c>null</c> or empty.</param>
    /// <returns>
    ///     The original <paramref name="value" /> if not <c>null</c> or empty; otherwise,
    ///     <paramref name="defaultValue" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// var displayName = Guard.NotNullOrEmptyOrElse(user.DisplayName, user.Username);
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrEmptyOrElse(string?, Func{string})" />
    public static string NotNullOrEmptyOrElse(string? value, string defaultValue)
    {
        return value is { Length: > 0 } ? value : defaultValue;
    }

    /// <summary>
    ///     Returns the string if not <c>null</c> or empty, otherwise computes a default using the factory.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="factory">
    ///     A factory function to compute the default value (only called if <paramref name="value" /> is
    ///     <c>null</c> or empty).
    /// </param>
    /// <returns>
    ///     The original <paramref name="value" /> if not <c>null</c> or empty; otherwise, the result of
    ///     <paramref name="factory" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// var displayName = Guard.NotNullOrEmptyOrElse(user.DisplayName, () => GenerateDisplayName(user));
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrEmptyOrElse(string?, string)" />
    public static string NotNullOrEmptyOrElse(string? value, Func<string> factory)
    {
        return value is { Length: > 0 } ? value : factory();
    }

    /// <summary>
    ///     Returns the string if not <c>null</c>, empty, or whitespace, otherwise returns the specified default value.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="defaultValue">
    ///     The default value to return if <paramref name="value" /> is <c>null</c>, empty, or whitespace.
    /// </param>
    /// <returns>
    ///     The original <paramref name="value" /> if it contains meaningful content;
    ///     otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// var title = Guard.NotNullOrWhiteSpaceOrElse(article.Title, "Untitled");
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrWhiteSpaceOrElse(string?, Func{string})" />
    public static string NotNullOrWhiteSpaceOrElse(string? value, string defaultValue)
    {
        return value is not null && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
    }

    /// <summary>
    ///     Returns the string if not <c>null</c>, empty, or whitespace, otherwise computes a default using the factory.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="factory">
    ///     A factory function to compute the default value (only called if <paramref name="value" /> is <c>null</c>, empty, or
    ///     whitespace).
    /// </param>
    /// <returns>
    ///     The original <paramref name="value" /> if it contains meaningful content;
    ///     otherwise, the result of <paramref name="factory" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// var title = Guard.NotNullOrWhiteSpaceOrElse(article.Title, () => GenerateTitle(article));
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrWhiteSpaceOrElse(string?, string)" />
    public static string NotNullOrWhiteSpaceOrElse(string? value, Func<string> factory)
    {
        return value is not null && !string.IsNullOrWhiteSpace(value) ? value : factory();
    }

    /// <summary>
    ///     Validates that a collection is not <c>null</c> or empty and returns it.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated non-null, non-empty collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty.</exception>
    /// <example>
    ///     <code>
    /// public void ProcessItems(IReadOnlyList&lt;Item&gt;? items)
    /// {
    ///     var validItems = Guard.NotNullOrEmpty(items);
    ///     foreach (var item in validItems) { ... }
    /// }
    /// </code>
    /// </example>
    public static IReadOnlyCollection<T> NotNullOrEmpty<T>(
        [NotNull] IReadOnlyCollection<T>? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        return value.Count is 0 ? throw new ArgumentException("Collection cannot be empty.", paramName) : value;
    }

    /// <summary>
    ///     Validates that a value is within the specified range (inclusive).
    /// </summary>
    /// <typeparam name="T">The type of the value (must implement <see cref="IComparable{T}" />).</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="value" /> is less than <paramref name="min" /> or greater than <paramref name="max" />.
    /// </exception>
    /// <example>
    ///     <code>
    /// public void SetPage(int page)
    /// {
    ///     _page = Guard.InRange(page, 1, 100);
    /// }
    /// </code>
    /// </example>
    public static T InRange<T>(T value, T min, T max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that an index is within valid bounds for a collection.
    /// </summary>
    /// <param name="index">The index to validate.</param>
    /// <param name="count">The total count of elements (exclusive upper bound).</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="index" /> is negative or greater than or equal to <paramref name="count" />.
    /// </exception>
    /// <example>
    ///     <code>
    /// public T GetItem(int index)
    /// {
    ///     Guard.ValidIndex(index, _items.Count);
    ///     return _items[index];
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ValidIndex(int index, int count,
        [CallerArgumentExpression(nameof(index))]
        string? paramName = null)
    {
        // Single branch: (uint) cast makes negative values wrap to large positive,
        // so both index < 0 and index >= count are caught by one unsigned comparison.
        if ((uint)index >= (uint)count)
            ThrowIndexOutOfRange(index, count, paramName);
        return index;
    }

    /// <summary>
    ///     Validates that a condition is <c>true</c>.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="message">The error message if the condition is <c>false</c>.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="condition" /> is <c>false</c>.</exception>
    /// <example>
    ///     <code>
    /// public void SetAge(int age)
    /// {
    ///     Guard.That(age >= 0, "Age cannot be negative.");
    ///     _age = age;
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void That(
        [DoesNotReturnIf(false)] bool condition,
        string message,
        [CallerArgumentExpression(nameof(condition))]
        string? paramName = null)
    {
        if (!condition)
            ThrowArgument(message, paramName);
    }

    /// <summary>
    ///     Validates that a value matches a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="predicate">The predicate that must return <c>true</c> for the value to be valid.</param>
    /// <param name="message">The error message if the predicate returns <c>false</c>.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="predicate" /> returns <c>false</c>.</exception>
    /// <example>
    ///     <code>
    /// public void SetEmail(string email)
    /// {
    ///     _email = Guard.Satisfies(email, e => e.Contains('@'), "Invalid email format.");
    /// }
    /// </code>
    /// </example>
    public static T Satisfies<T>(
        T value,
        Func<T, bool> predicate,
        string message,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        return !predicate(value) ? throw new ArgumentException(message, paramName) : value;
    }

    #region String Length

    /// <summary>
    ///     Validates that a string has exactly the specified length and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="length">The required length.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> does not have the required length.</exception>
    /// <example>
    ///     <code>
    /// public void SetCountryCode(string code)
    /// {
    ///     _code = Guard.HasLength(code, 2); // ISO country codes
    /// }
    /// </code>
    /// </example>
    public static string HasLength(
        [NotNull] string? value,
        int length,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));

        if (value.Length != length)
            throw new ArgumentException($"String must be exactly {length} characters. Actual: {value.Length}.",
                paramName);

        return value;
    }

    /// <summary>
    ///     Validates that a string has at least the specified minimum length and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="minLength">The minimum required length.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="value" /> is shorter than <paramref name="minLength" />
    ///     .
    /// </exception>
    /// <example>
    ///     <code>
    /// public void SetPassword(string password)
    /// {
    ///     _password = Guard.HasMinLength(password, 8);
    /// }
    /// </code>
    /// </example>
    public static string HasMinLength(
        [NotNull] string? value,
        int minLength,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));

        if (value.Length < minLength)
            throw new ArgumentException($"String must be at least {minLength} characters. Actual: {value.Length}.",
                paramName);

        return value;
    }

    /// <summary>
    ///     Validates that a string does not exceed the specified maximum length and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> exceeds <paramref name="maxLength" />.</exception>
    /// <example>
    ///     <code>
    /// public void SetUsername(string username)
    /// {
    ///     _username = Guard.HasMaxLength(username, 50);
    /// }
    /// </code>
    /// </example>
    public static string HasMaxLength(
        [NotNull] string? value,
        int maxLength,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));

        if (value.Length > maxLength)
            throw new ArgumentException($"String must not exceed {maxLength} characters. Actual: {value.Length}.",
                paramName);

        return value;
    }

    /// <summary>
    ///     Validates that a string length is within the specified range (inclusive) and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="minLength">The minimum required length.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> length is outside the specified range.</exception>
    /// <example>
    ///     <code>
    /// public void SetDisplayName(string name)
    /// {
    ///     _name = Guard.HasLengthBetween(name, 3, 100);
    /// }
    /// </code>
    /// </example>
    public static string HasLengthBetween(
        [NotNull] string? value,
        int minLength,
        int maxLength,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));

        if (value.Length < minLength || value.Length > maxLength)
            throw new ArgumentException(
                $"String length must be between {minLength} and {maxLength}. Actual: {value.Length}.", paramName);

        return value;
    }

    #endregion

    #region Collections

    /// <summary>
    ///     Validates that a collection contains no duplicate elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> contains duplicate elements.</exception>
    /// <example>
    ///     <code>
    /// public void SetIds(IEnumerable&lt;int&gt; ids)
    /// {
    ///     Guard.NoDuplicates(ids);
    /// }
    /// </code>
    /// </example>
    public static void NoDuplicates<T>(
        IEnumerable<T> value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));
        CheckNoDuplicates(value, null, paramName);
    }

    /// <summary>
    ///     Validates that a collection contains no duplicate elements using a custom equality comparer.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="comparer">The equality comparer to use for duplicate detection.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> contains duplicate elements.</exception>
    /// <example>
    ///     <code>
    /// public void SetNames(IEnumerable&lt;string&gt; names)
    /// {
    ///     Guard.NoDuplicates(names, StringComparer.OrdinalIgnoreCase);
    /// }
    /// </code>
    /// </example>
    public static void NoDuplicates<T>(
        IEnumerable<T> value,
        IEqualityComparer<T> comparer,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));
        CheckNoDuplicates(value, comparer, paramName);
    }

    /// <summary>
    ///     Validates that a collection contains no duplicate elements and returns it.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> contains duplicate elements.</exception>
    public static IReadOnlyList<T> NoDuplicates<T>(
        [NotNull] IReadOnlyList<T>? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));
        CheckNoDuplicates(value, null, paramName);
        return value;
    }

    /// <summary>
    ///     Validates that a collection contains no duplicate elements using a custom equality comparer and returns it.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="comparer">The equality comparer to use for duplicate detection.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> contains duplicate elements.</exception>
    public static IReadOnlyList<T> NoDuplicates<T>(
        [NotNull] IReadOnlyList<T>? value,
        IEqualityComparer<T> comparer,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));
        CheckNoDuplicates(value, comparer, paramName);
        return value;
    }

    #endregion

    #region File System

    /// <summary>
    ///     Validates that a file exists at the specified path and returns the path.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is empty or the file does not exist.</exception>
    /// <example>
    ///     <code>
    /// public void LoadConfig(string path)
    /// {
    ///     var validPath = Guard.FileExists(path);
    ///     var content = File.ReadAllText(validPath);
    /// }
    /// </code>
    /// </example>
    public static string FileExists(
        [NotNull] string? path,
        [CallerArgumentExpression(nameof(path))]
        string? paramName = null)
    {
        NotNullOrEmpty(path, paramName);
#pragma warning disable RS1035 // File I/O is valid for non-analyzer callers (testing, CLI tools)
        return File.Exists(path)
#pragma warning restore RS1035
            ? path
            : throw new ArgumentException($"File not found: {path}", paramName);
    }

    /// <summary>
    ///     Validates that a directory exists at the specified path and returns the path.
    /// </summary>
    /// <param name="path">The directory path to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is empty or the directory does not exist.</exception>
    /// <example>
    ///     <code>
    /// public void ProcessDirectory(string path)
    /// {
    ///     var validPath = Guard.DirectoryExists(path);
    ///     foreach (var file in Directory.GetFiles(validPath)) { }
    /// }
    /// </code>
    /// </example>
    public static string DirectoryExists(
        [NotNull] string? path,
        [CallerArgumentExpression(nameof(path))]
        string? paramName = null)
    {
        NotNullOrEmpty(path, paramName);
#pragma warning disable RS1035 // File I/O is valid for non-analyzer callers (testing, CLI tools)
        return Directory.Exists(path)
#pragma warning restore RS1035
            ? path
            : throw new ArgumentException($"Directory not found: {path}", paramName);
    }

    #endregion
}
