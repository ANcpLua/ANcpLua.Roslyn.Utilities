namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods providing try-pattern alternatives for common operations,
///     reducing boilerplate code when working with dictionaries, parsing, and lookups.
/// </summary>
/// <remarks>
///     <para>
///         These extensions eliminate verbose <c>TryGetValue</c> and <c>TryParse</c> patterns,
///         providing null-returning alternatives that work better with null-conditional operators.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Dictionary lookups:</b> <see cref="GetOrNull{TKey,TValue}(IDictionary{TKey,TValue},TKey)" />,
///                 <see cref="GetOrDefault{TKey,TValue}(IDictionary{TKey,TValue},TKey,TValue)" />,
///                 <see cref="GetOrElse{TKey,TValue}(IDictionary{TKey,TValue},TKey,Func{TValue})" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Integer parsing:</b> <see cref="TryParseByte(string?)" />, <see cref="TryParseSByte(string?)" />,
///                 <see cref="TryParseInt16(string?)" />, <see cref="TryParseUInt16(string?)" />,
///                 <see cref="TryParseInt32(string?)" />, <see cref="TryParseUInt32(string?)" />,
///                 <see cref="TryParseInt64(string?)" />, <see cref="TryParseUInt64(string?)" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Floating-point parsing:</b> <see cref="TryParseSingle(string?)" />,
///                 <see cref="TryParseDouble(string?)" />, <see cref="TryParseDecimal(string?)" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Other parsing:</b> <see cref="TryParseBool(string?)" />, <see cref="TryParseChar(string?)" />,
///                 <see cref="TryParseGuid(string?)" />, <see cref="TryParseEnum{TEnum}(string?,bool)" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Date/time parsing:</b> <see cref="TryParseDateTime(string?)" />,
///                 <see cref="TryParseDateTimeOffset(string?)" />, <see cref="TryParseTimeSpan(string?)" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Collection access:</b> <see cref="ElementAtOrNull{T}(IList{T},int)" />,
///                 <see cref="ValueAtOrNull{T}(IList{T},int)" />,
///                 <see cref="ElementAtOrDefault{T}(IList{T},int,T)" />
///             </description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class TryExtensions
{
    // ========== Dictionary Extensions (Reference Types) ==========

    /// <summary>
    ///     Gets the value associated with the specified key, or <c>null</c> if not found.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary (must be a reference type).</typeparam>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>
    ///     The value associated with <paramref name="key" /> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method provides a cleaner alternative to <c>TryGetValue</c> when you just
    ///         need the value or <c>null</c>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Before
    /// if (dict.TryGetValue(key, out var value))
    /// {
    ///     DoSomething(value);
    /// }
    ///
    /// // After - works with null-conditional
    /// dict.GetOrNull(key)?.DoSomething();
    ///
    /// // Or in a chain
    /// var result = dict.GetOrNull(key)?.Process();
    /// </code>
    /// </example>
    /// <seealso cref="GetOrDefault{TKey,TValue}(IDictionary{TKey,TValue},TKey,TValue)" />
    public static TValue? GetOrNull<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : class
        => dictionary.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    ///     Gets the value associated with the specified key, or <c>null</c> if not found.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary (must be a reference type).</typeparam>
    /// <param name="dictionary">The read-only dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>
    ///     The value associated with <paramref name="key" /> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <seealso cref="GetOrNull{TKey,TValue}(IDictionary{TKey,TValue},TKey)" />
    public static TValue? GetOrNull<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : class
        => dictionary.TryGetValue(key, out var value) ? value : null;

    // ========== Dictionary Extensions (Value Types) ==========

    /// <summary>
    ///     Gets the value associated with the specified key, or <c>null</c> if not found.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary (must be a value type).</typeparam>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>
    ///     The value associated with <paramref name="key" /> wrapped in a nullable if found;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// Dictionary&lt;string, int&gt; counts = ...;
    /// int? count = counts.GetValueOrNull("key");
    /// </code>
    /// </example>
    public static TValue? GetValueOrNull<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : struct
        => dictionary.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    ///     Gets the value associated with the specified key, or <c>null</c> if not found.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary (must be a value type).</typeparam>
    /// <param name="dictionary">The read-only dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>
    ///     The value associated with <paramref name="key" /> wrapped in a nullable if found;
    ///     otherwise, <c>null</c>.
    /// </returns>
    public static TValue? GetValueOrNull<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : struct
        => dictionary.TryGetValue(key, out var value) ? value : null;

    // ========== Dictionary Extensions (Default Value) ==========

    /// <summary>
    ///     Gets the value associated with the specified key, or a default value if not found.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The value to return if the key is not found.</param>
    /// <returns>
    ///     The value associated with <paramref name="key" /> if found;
    ///     otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// var timeout = settings.GetOrDefault("Timeout", 30);
    /// var name = users.GetOrDefault(userId, "Unknown");
    /// </code>
    /// </example>
    /// <seealso cref="GetOrNull{TKey,TValue}(IDictionary{TKey,TValue},TKey)" />
    public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        => dictionary.TryGetValue(key, out var value) ? value : defaultValue;

    /// <summary>
    ///     Gets the value associated with the specified key, or a default value if not found.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The read-only dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The value to return if the key is not found.</param>
    /// <returns>
    ///     The value associated with <paramref name="key" /> if found;
    ///     otherwise, <paramref name="defaultValue" />.
    /// </returns>
    public static TValue GetOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        => dictionary.TryGetValue(key, out var value) ? value : defaultValue;

    /// <summary>
    ///     Gets the value associated with the specified key, or computes a default using a factory if not found.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="factory">A factory function to compute the default value (only called if key is not found).</param>
    /// <returns>
    ///     The value associated with <paramref name="key" /> if found;
    ///     otherwise, the result of <paramref name="factory" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Use this when the default value is expensive to compute and should only be
    ///         calculated when the key is not found.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var config = cache.GetOrElse(key, () => LoadExpensiveConfig());
    /// </code>
    /// </example>
    public static TValue GetOrElse<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
        => dictionary.TryGetValue(key, out var value) ? value : factory();

    /// <summary>
    ///     Gets the value associated with the specified key, or computes a default using a factory if not found.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The read-only dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="factory">A factory function to compute the default value.</param>
    /// <returns>
    ///     The value associated with <paramref name="key" /> if found;
    ///     otherwise, the result of <paramref name="factory" />.
    /// </returns>
    public static TValue GetOrElse<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
        => dictionary.TryGetValue(key, out var value) ? value : factory();

    // ========== 8-bit Integer Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as an unsigned 8-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed byte if successful; otherwise, <c>null</c>.</returns>
    public static byte? TryParseByte(this string? value)
        => byte.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as an unsigned 8-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed byte if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static byte TryParseByte(this string? value, byte defaultValue)
        => byte.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    ///     Attempts to parse the string as a signed 8-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed sbyte if successful; otherwise, <c>null</c>.</returns>
    public static sbyte? TryParseSByte(this string? value)
        => sbyte.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a signed 8-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed sbyte if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static sbyte TryParseSByte(this string? value, sbyte defaultValue)
        => sbyte.TryParse(value, out var result) ? result : defaultValue;

    // ========== 16-bit Integer Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a signed 16-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed short if successful; otherwise, <c>null</c>.</returns>
    public static short? TryParseInt16(this string? value)
        => short.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a signed 16-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed short if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static short TryParseInt16(this string? value, short defaultValue)
        => short.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    ///     Attempts to parse the string as an unsigned 16-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed ushort if successful; otherwise, <c>null</c>.</returns>
    public static ushort? TryParseUInt16(this string? value)
        => ushort.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as an unsigned 16-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed ushort if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static ushort TryParseUInt16(this string? value, ushort defaultValue)
        => ushort.TryParse(value, out var result) ? result : defaultValue;

    // ========== 32-bit Integer Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a signed 32-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed integer if successful; otherwise, <c>null</c>.</returns>
    /// <example>
    ///     <code>
    /// int? count = input.TryParseInt32() ?? 0;
    ///
    /// if (value.TryParseInt32() is { } parsed)
    /// {
    ///     ProcessNumber(parsed);
    /// }
    /// </code>
    /// </example>
    public static int? TryParseInt32(this string? value)
        => int.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a signed 32-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed integer if successful; otherwise, <paramref name="defaultValue" />.</returns>
    /// <example>
    ///     <code>
    /// int page = queryString["page"].TryParseInt32(1);
    /// </code>
    /// </example>
    public static int TryParseInt32(this string? value, int defaultValue)
        => int.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    ///     Attempts to parse the string as an unsigned 32-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed uint if successful; otherwise, <c>null</c>.</returns>
    public static uint? TryParseUInt32(this string? value)
        => uint.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as an unsigned 32-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed uint if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static uint TryParseUInt32(this string? value, uint defaultValue)
        => uint.TryParse(value, out var result) ? result : defaultValue;

    // ========== 64-bit Integer Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a signed 64-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed long if successful; otherwise, <c>null</c>.</returns>
    public static long? TryParseInt64(this string? value)
        => long.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a signed 64-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed long if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static long TryParseInt64(this string? value, long defaultValue)
        => long.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    ///     Attempts to parse the string as an unsigned 64-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed ulong if successful; otherwise, <c>null</c>.</returns>
    public static ulong? TryParseUInt64(this string? value)
        => ulong.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as an unsigned 64-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed ulong if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static ulong TryParseUInt64(this string? value, ulong defaultValue)
        => ulong.TryParse(value, out var result) ? result : defaultValue;

    // ========== Floating-Point Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a single-precision floating-point number.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed float if successful; otherwise, <c>null</c>.</returns>
    public static float? TryParseSingle(this string? value)
        => float.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a single-precision floating-point number, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed float if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static float TryParseSingle(this string? value, float defaultValue)
        => float.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    ///     Attempts to parse the string as a double-precision floating-point number.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed number if successful; otherwise, <c>null</c>.</returns>
    public static double? TryParseDouble(this string? value)
        => double.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a double-precision floating-point number, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed number if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static double TryParseDouble(this string? value, double defaultValue)
        => double.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    ///     Attempts to parse the string as a decimal number.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed decimal if successful; otherwise, <c>null</c>.</returns>
    public static decimal? TryParseDecimal(this string? value)
        => decimal.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a decimal number, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed decimal if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static decimal TryParseDecimal(this string? value, decimal defaultValue)
        => decimal.TryParse(value, out var result) ? result : defaultValue;

    // ========== Boolean and Character Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a boolean.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed boolean if successful; otherwise, <c>null</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         Accepts "true"/"false" (case-insensitive) as valid boolean values.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// bool isEnabled = config["Enabled"].TryParseBool() ?? false;
    /// </code>
    /// </example>
    public static bool? TryParseBool(this string? value)
        => bool.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a boolean, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed boolean if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static bool TryParseBool(this string? value, bool defaultValue)
        => bool.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    ///     Attempts to parse the string as a single character.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed character if successful; otherwise, <c>null</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         The string must be exactly one character long for parsing to succeed.
    ///     </para>
    /// </remarks>
    public static char? TryParseChar(this string? value)
        => char.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a single character, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed character if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static char TryParseChar(this string? value, char defaultValue)
        => char.TryParse(value, out var result) ? result : defaultValue;

    // ========== GUID Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a <see cref="Guid" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed GUID if successful; otherwise, <c>null</c>.</returns>
    /// <example>
    ///     <code>
    /// Guid? id = idString.TryParseGuid();
    /// </code>
    /// </example>
    public static Guid? TryParseGuid(this string? value)
        => Guid.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a <see cref="Guid" />, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed GUID if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static Guid TryParseGuid(this string? value, Guid defaultValue)
        => Guid.TryParse(value, out var result) ? result : defaultValue;

    // ========== Enum Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as an enum value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The string to parse.</param>
    /// <param name="ignoreCase">Whether to ignore case when parsing.</param>
    /// <returns>The parsed enum value if successful; otherwise, <c>null</c>.</returns>
    /// <example>
    ///     <code>
    /// var status = statusString.TryParseEnum&lt;OrderStatus&gt;();
    ///
    /// // Case-insensitive parsing
    /// var level = "warning".TryParseEnum&lt;LogLevel&gt;(ignoreCase: true);
    /// </code>
    /// </example>
    public static TEnum? TryParseEnum<TEnum>(this string? value, bool ignoreCase = false)
        where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, ignoreCase, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as an enum value, returning a default on failure.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <param name="ignoreCase">Whether to ignore case when parsing.</param>
    /// <returns>The parsed enum value if successful; otherwise, <paramref name="defaultValue" />.</returns>
    /// <example>
    ///     <code>
    /// var status = statusString.TryParseEnum(OrderStatus.Pending);
    /// </code>
    /// </example>
    public static TEnum TryParseEnum<TEnum>(this string? value, TEnum defaultValue, bool ignoreCase = false)
        where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, ignoreCase, out var result) ? result : defaultValue;

    // ========== Date/Time Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a <see cref="DateTime" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed date/time if successful; otherwise, <c>null</c>.</returns>
    public static DateTime? TryParseDateTime(this string? value)
        => DateTime.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a <see cref="DateTime" />, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed date/time if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static DateTime TryParseDateTime(this string? value, DateTime defaultValue)
        => DateTime.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    ///     Attempts to parse the string as a <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed date/time offset if successful; otherwise, <c>null</c>.</returns>
    public static DateTimeOffset? TryParseDateTimeOffset(this string? value)
        => DateTimeOffset.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a <see cref="DateTimeOffset" />, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed date/time offset if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static DateTimeOffset TryParseDateTimeOffset(this string? value, DateTimeOffset defaultValue)
        => DateTimeOffset.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    ///     Attempts to parse the string as a <see cref="TimeSpan" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed time span if successful; otherwise, <c>null</c>.</returns>
    public static TimeSpan? TryParseTimeSpan(this string? value)
        => TimeSpan.TryParse(value, out var result) ? result : null;

    /// <summary>
    ///     Attempts to parse the string as a <see cref="TimeSpan" />, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed time span if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static TimeSpan TryParseTimeSpan(this string? value, TimeSpan defaultValue)
        => TimeSpan.TryParse(value, out var result) ? result : defaultValue;

    // ========== Collection Safe Access ==========

    /// <summary>
    ///     Gets the element at the specified index, or <c>null</c> if the index is out of bounds.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to access.</param>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>
    ///     The element at <paramref name="index" /> if within bounds; otherwise, <c>null</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// var first = items.ElementAtOrNull(0);
    /// var last = items.ElementAtOrNull(items.Count - 1);
    /// </code>
    /// </example>
    /// <seealso cref="ElementAtOrDefault{T}(IList{T},int,T)" />
    public static T? ElementAtOrNull<T>(this IList<T> list, int index) where T : class
        => index >= 0 && index < list.Count ? list[index] : null;

    /// <summary>
    ///     Gets the element at the specified index, or <c>null</c> if the index is out of bounds.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The read-only list to access.</param>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>
    ///     The element at <paramref name="index" /> if within bounds; otherwise, <c>null</c>.
    /// </returns>
    public static T? ElementAtOrNull<T>(this IReadOnlyList<T> list, int index) where T : class
        => index >= 0 && index < list.Count ? list[index] : null;

    /// <summary>
    ///     Gets the value type element at the specified index, or <c>null</c> if the index is out of bounds.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list (must be a value type).</typeparam>
    /// <param name="list">The list to access.</param>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>
    ///     The element at <paramref name="index" /> wrapped in a nullable if within bounds;
    ///     otherwise, <c>null</c>.
    /// </returns>
    public static T? ValueAtOrNull<T>(this IList<T> list, int index) where T : struct
        => index >= 0 && index < list.Count ? list[index] : null;

    /// <summary>
    ///     Gets the value type element at the specified index, or <c>null</c> if the index is out of bounds.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list (must be a value type).</typeparam>
    /// <param name="list">The read-only list to access.</param>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>
    ///     The element at <paramref name="index" /> wrapped in a nullable if within bounds;
    ///     otherwise, <c>null</c>.
    /// </returns>
    public static T? ValueAtOrNull<T>(this IReadOnlyList<T> list, int index) where T : struct
        => index >= 0 && index < list.Count ? list[index] : null;

    /// <summary>
    ///     Gets the element at the specified index, or a default value if the index is out of bounds.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to access.</param>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <param name="defaultValue">The value to return if the index is out of bounds.</param>
    /// <returns>
    ///     The element at <paramref name="index" /> if within bounds;
    ///     otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// var item = items.ElementAtOrDefault(index, fallbackItem);
    /// </code>
    /// </example>
    public static T ElementAtOrDefault<T>(this IList<T> list, int index, T defaultValue)
        => index >= 0 && index < list.Count ? list[index] : defaultValue;

    /// <summary>
    ///     Gets the element at the specified index, or a default value if the index is out of bounds.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The read-only list to access.</param>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <param name="defaultValue">The value to return if the index is out of bounds.</param>
    /// <returns>
    ///     The element at <paramref name="index" /> if within bounds;
    ///     otherwise, <paramref name="defaultValue" />.
    /// </returns>
    public static T ElementAtOrDefault<T>(this IReadOnlyList<T> list, int index, T defaultValue)
        => index >= 0 && index < list.Count ? list[index] : defaultValue;
}
