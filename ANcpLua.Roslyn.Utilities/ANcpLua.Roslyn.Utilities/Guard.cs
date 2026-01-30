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
    static class Guard
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
    public static T NotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value ?? throw new ArgumentNullException(paramName);

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
        => value ?? defaultValue;

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
        => value ?? factory();

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
        => value ?? defaultValue;

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
        => value ?? factory();

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
    public static string NotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        if (value.Length is 0)
            throw new ArgumentException("Value cannot be empty.", paramName);
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
    public static string NotNullOrWhiteSpace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
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
        => value is { Length: > 0 } ? value : defaultValue;

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
        => value is { Length: > 0 } ? value : factory();

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
        => value is not null && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;

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
        => value is not null && !string.IsNullOrWhiteSpace(value) ? value : factory();

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
    public static T InRange<T>(T value, T min, T max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
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
    public static int ValidIndex(int index, int count, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(paramName, index, $"Index must be between 0 and {count - 1}.");
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
    public static void That(
        [DoesNotReturnIf(false)] bool condition,
        string message,
        [CallerArgumentExpression(nameof(condition))]
        string? paramName = null)
    {
        if (!condition)
            throw new ArgumentException(message, paramName);
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
        var seen = new HashSet<T>();
        foreach (var item in value)
        {
            if (!seen.Add(item))
                throw new ArgumentException($"Duplicate value found: {item}", paramName);
        }
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
        var seen = new HashSet<T>(comparer);
        foreach (var item in value)
        {
            if (!seen.Add(item))
                throw new ArgumentException($"Duplicate value found: {item}", paramName);
        }
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
        if (value is null)
            throw new ArgumentNullException(paramName);

        var seen = new HashSet<T>();
        foreach (var item in value)
        {
            if (!seen.Add(item))
                throw new ArgumentException($"Duplicate value found: {item}", paramName);
        }

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
        if (value is null)
            throw new ArgumentNullException(paramName);

        var seen = new HashSet<T>(comparer);
        foreach (var item in value)
        {
            if (!seen.Add(item))
                throw new ArgumentException($"Duplicate value found: {item}", paramName);
        }

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
        return File.Exists(path)
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
        return Directory.Exists(path)
            ? path
            : throw new ArgumentException($"Directory not found: {path}", paramName);
    }

    #endregion

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
            throw new ArgumentOutOfRangeException(paramName, value, $"Undefined enum value for {typeof(T).Name}: {value}");

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
        if (member is null)
            throw new ArgumentException($"Member {memberName} of {paramName} is null", paramName);

        return member;
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
        if (member is null)
            throw new ArgumentException($"Member {memberName} of {paramName} is null", paramName);

        return member;
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
        var location = string.IsNullOrEmpty(filePath)
            ? memberName
            : $"{memberName} ({Path.GetFileName(filePath)}:{lineNumber})";

        throw new InvalidOperationException(
            string.IsNullOrEmpty(message)
                ? $"Unreachable code executed in {location}"
                : $"{message} (in {location})");
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
        Unreachable(message, memberName, filePath, lineNumber);
        return default!; // Never reached
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
        {
            var location = string.IsNullOrEmpty(filePath)
                ? memberName
                : $"{memberName} ({Path.GetFileName(filePath)}:{lineNumber})";

            var fullMessage = string.IsNullOrEmpty(message)
                ? $"Unreachable code executed in {location}. Condition: {conditionExpression}"
                : $"{message} (in {location}). Condition: {conditionExpression}";

            throw new InvalidOperationException(fullMessage);
        }
    }

    #endregion

    #region Numeric - Int32

    /// <summary>
    ///     Validates that an integer is not zero and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NotZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value is 0
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be zero.")
            : value;

    /// <summary>
    ///     Validates that an integer is not negative and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NotNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value < 0
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.")
            : value;

    /// <summary>
    ///     Validates that an integer is positive (greater than zero) and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Positive(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value <= 0
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive.")
            : value;

    /// <summary>
    ///     Validates that an integer is not greater than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NotGreaterThan(int value, int max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value > max
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must not be greater than {max}.")
            : value;

    /// <summary>
    ///     Validates that an integer is not less than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NotLessThan(int value, int min, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value < min
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must not be less than {min}.")
            : value;

    /// <summary>
    ///     Validates that an integer is less than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LessThan(int value, int max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value >= max
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must be less than {max}.")
            : value;

    /// <summary>
    ///     Validates that an integer is greater than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GreaterThan(int value, int min, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value <= min
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must be greater than {min}.")
            : value;

    #endregion

    #region Numeric - Int64

    /// <summary>
    ///     Validates that a long is not zero and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NotZero(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value is 0L
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be zero.")
            : value;

    /// <summary>
    ///     Validates that a long is not negative and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NotNegative(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value < 0L
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.")
            : value;

    /// <summary>
    ///     Validates that a long is positive (greater than zero) and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Positive(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value <= 0L
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive.")
            : value;

    /// <summary>
    ///     Validates that a long is not greater than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NotGreaterThan(long value, long max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value > max
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must not be greater than {max}.")
            : value;

    /// <summary>
    ///     Validates that a long is not less than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NotLessThan(long value, long min, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value < min
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must not be less than {min}.")
            : value;

    /// <summary>
    ///     Validates that a long is less than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long LessThan(long value, long max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value >= max
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must be less than {max}.")
            : value;

    /// <summary>
    ///     Validates that a long is greater than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GreaterThan(long value, long min, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value <= min
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must be greater than {min}.")
            : value;

    #endregion

    #region Numeric - Double

    /// <summary>
    ///     Validates that a double is not zero and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NotZero(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value is 0.0
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be zero.")
            : value;

    /// <summary>
    ///     Validates that a double is not negative and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NotNegative(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => !(value >= 0.0) // Handles NaN correctly
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.")
            : value;

    /// <summary>
    ///     Validates that a double is positive (greater than zero) and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Positive(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => !(value > 0.0) // Handles NaN correctly
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive.")
            : value;

    /// <summary>
    ///     Validates that a double is not greater than the specified maximum and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NotGreaterThan(double value, double max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => !(value <= max) // Handles NaN correctly
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must not be greater than {max}.")
            : value;

    /// <summary>
    ///     Validates that a double is not less than the specified minimum and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NotLessThan(double value, double min, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => !(value >= min) // Handles NaN correctly
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must not be less than {min}.")
            : value;

    /// <summary>
    ///     Validates that a double is less than the specified maximum and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double LessThan(double value, double max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => !(value < max) // Handles NaN correctly
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must be less than {max}.")
            : value;

    /// <summary>
    ///     Validates that a double is greater than the specified minimum and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GreaterThan(double value, double min, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => !(value > min) // Handles NaN correctly
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must be greater than {min}.")
            : value;

    /// <summary>
    ///     Validates that a double is not NaN and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NotNaN(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => double.IsNaN(value)
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be NaN.")
            : value;

    /// <summary>
    ///     Validates that a double is finite (not NaN or infinity) and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Finite(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => double.IsNaN(value) || double.IsInfinity(value)
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value must be finite.")
            : value;

    #endregion

    #region Path Validation

    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    private static readonly char[] InvalidPathChars = Path
        .GetInvalidPathChars()
        .Concat(InvalidFileNameChars.Except(new[] { '/', '\\', ':' }))
        .Distinct()
        .ToArray();

    /// <summary>
    ///     Validates that a string is a valid file name (contains no invalid characters) and returns it.
    /// </summary>
    /// <param name="value">The file name to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated file name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains invalid characters.</exception>
    /// <example>
    ///     <code>
    /// public void SaveFile(string fileName)
    /// {
    ///     var validName = Guard.ValidFileName(fileName);
    ///     File.WriteAllText(Path.Combine(_directory, validName), content);
    /// }
    /// </code>
    /// </example>
    public static string ValidFileName(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        NotNullOrEmpty(value, paramName);

        foreach (var invalidChar in InvalidFileNameChars)
        {
            if (value.IndexOf(invalidChar) != -1)
                throw new ArgumentException($"Invalid character '{invalidChar}' in file name: {value}", paramName);
        }

        return value;
    }

    /// <summary>
    ///     Validates that a string is a valid file name if not null.
    /// </summary>
    /// <param name="value">The file name to validate, or <c>null</c>.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated file name, or <c>null</c> if input was <c>null</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains invalid characters.</exception>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ValidFileNameOrNull(
        string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null)
            return null;

        return ValidFileName(value, paramName);
    }

    /// <summary>
    ///     Validates that a string is a valid directory/path name (contains no invalid characters) and returns it.
    /// </summary>
    /// <param name="value">The path to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains invalid characters.</exception>
    public static string ValidPath(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        NotNullOrEmpty(value, paramName);

        foreach (var invalidChar in InvalidPathChars)
        {
            if (value.IndexOf(invalidChar) != -1)
                throw new ArgumentException($"Invalid character '{invalidChar}' in path: {value}", paramName);
        }

        return value;
    }

    /// <summary>
    ///     Validates that a string is a valid directory/path name if not null.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ValidPathOrNull(
        string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null)
            return null;

        return ValidPath(value, paramName);
    }

    /// <summary>
    ///     Validates that a string is a valid file extension (no leading dot, no path separators) and returns it.
    /// </summary>
    /// <param name="value">The extension to validate (e.g., "txt", "json").</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated extension without a leading dot.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="value" /> is empty, starts with a dot, or contains path separators.
    /// </exception>
    /// <example>
    ///     <code>
    /// public void RegisterExtension(string extension)
    /// {
    ///     var valid = Guard.ValidExtension(extension);
    ///     _extensions.Add("." + valid);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="NormalizedExtension" />
    public static string ValidExtension(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        NotNullOrEmpty(value, paramName);

        if (value.StartsWith("."))
            throw new ArgumentException("Extension must not start with a period ('.').", paramName);

        if (value.Contains('\\') || value.Contains('/'))
            throw new ArgumentException("Extension must not contain path separators.", paramName);

        return value;
    }

    /// <summary>
    ///     Validates and normalizes a file extension to include a leading dot.
    ///     Accepts both "txt" and ".txt" formats.
    /// </summary>
    /// <param name="value">The extension to normalize (e.g., "txt" or ".txt").</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The extension with a leading dot (e.g., ".txt").</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains path separators.</exception>
    /// <example>
    ///     <code>
    /// var ext1 = Guard.NormalizedExtension("txt");   // returns ".txt"
    /// var ext2 = Guard.NormalizedExtension(".txt");  // returns ".txt"
    /// </code>
    /// </example>
    /// <seealso cref="ValidExtension" />
    public static string NormalizedExtension(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        NotNullOrEmpty(value, paramName);

        if (value.Contains('\\') || value.Contains('/'))
            throw new ArgumentException("Extension must not contain path separators.", paramName);

        return value.StartsWith(".") ? value : "." + value;
    }

    #endregion

    #region Type Validation

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

        if (Nullable.GetUnderlyingType(type) is not null)
            throw new ArgumentException("Nullable types are not supported.", paramName);

        return type;
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
    /// <exception cref="ArgumentException">Thrown when <paramref name="type" /> is not assignable to <typeparamref name="T" />.</exception>
    public static Type AssignableTo<T>(
        [NotNull] Type? type,
        [CallerArgumentExpression(nameof(type))]
        string? paramName = null)
    {
        NotNull(type, paramName);

        if (!typeof(T).IsAssignableFrom(type))
            throw new ArgumentException($"Type {type.Name} is not assignable to {typeof(T).Name}.", paramName);

        return type;
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
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="T" /> is not assignable to <paramref name="type" />.</exception>
    public static Type AssignableFrom<T>(
        [NotNull] Type? type,
        [CallerArgumentExpression(nameof(type))]
        string? paramName = null)
    {
        NotNull(type, paramName);

        if (!type.IsAssignableFrom(typeof(T)))
            throw new ArgumentException($"Type {typeof(T).Name} is not assignable to {type.Name}.", paramName);

        return type;
    }

    #endregion

    #region Numeric - Decimal

    /// <summary>
    ///     Validates that a decimal is not zero and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal NotZero(decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value == 0m
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be zero.")
            : value;

    /// <summary>
    ///     Validates that a decimal is not negative and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal NotNegative(decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value < 0m
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.")
            : value;

    /// <summary>
    ///     Validates that a decimal is positive (greater than zero) and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal Positive(decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value <= 0m
            ? throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive.")
            : value;

    /// <summary>
    ///     Validates that a decimal is not greater than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal NotGreaterThan(decimal value, decimal max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value > max
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must not be greater than {max}.")
            : value;

    /// <summary>
    ///     Validates that a decimal is not less than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal NotLessThan(decimal value, decimal min, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value < min
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must not be less than {min}.")
            : value;

    /// <summary>
    ///     Validates that a decimal is less than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal LessThan(decimal value, decimal max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value >= max
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must be less than {max}.")
            : value;

    /// <summary>
    ///     Validates that a decimal is greater than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal GreaterThan(decimal value, decimal min, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value <= min
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must be greater than {min}.")
            : value;

    #endregion
}