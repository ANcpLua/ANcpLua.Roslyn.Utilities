namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Nullable-returning <c>Try*</c> alternatives that chain with <c>?.</c> better than the BCL's <c>bool + out</c> shape:
///     dictionary <c>GetValueOrNull/Else</c>, <c>TryParse</c> for every BCL numeric/bool/char/Guid/enum/DateTime*/TimeSpan,
///     and <c>ElementAtOrNull/Default</c> for <see cref="IReadOnlyList{T}" />.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class TryExtensions
{
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
    public static TValue? GetValueOrNull<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : struct
    {
        return dictionary.TryGetValue(key, out var value) ? value : null;
    }

    // ========== Dictionary Extensions (Lazy Factory) ==========

    /// <summary>
    ///     Gets the value associated with the specified key, or computes a default using a factory if not found.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="factory">A factory function to compute the default value.</param>
    /// <returns>
    ///     The value associated with <paramref name="key" /> if found;
    ///     otherwise, the result of <paramref name="factory" />.
    /// </returns>
    public static TValue GetOrElse<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key,
        Func<TValue> factory)
    {
        return dictionary.TryGetValue(key, out var value) ? value : factory();
    }

    // ========== 8-bit Integer Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as an unsigned 8-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed byte if successful; otherwise, <c>null</c>.</returns>
    public static byte? TryParseByte(this string? value)
    {
        return byte.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as an unsigned 8-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed byte if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static byte TryParseByte(this string? value, byte defaultValue)
    {
        return byte.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Attempts to parse the string as a signed 8-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed sbyte if successful; otherwise, <c>null</c>.</returns>
    public static sbyte? TryParseSByte(this string? value)
    {
        return sbyte.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a signed 8-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed sbyte if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static sbyte TryParseSByte(this string? value, sbyte defaultValue)
    {
        return sbyte.TryParse(value, out var result) ? result : defaultValue;
    }

    // ========== 16-bit Integer Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a signed 16-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed short if successful; otherwise, <c>null</c>.</returns>
    public static short? TryParseInt16(this string? value)
    {
        return short.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a signed 16-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed short if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static short TryParseInt16(this string? value, short defaultValue)
    {
        return short.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Attempts to parse the string as an unsigned 16-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed ushort if successful; otherwise, <c>null</c>.</returns>
    public static ushort? TryParseUInt16(this string? value)
    {
        return ushort.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as an unsigned 16-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed ushort if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static ushort TryParseUInt16(this string? value, ushort defaultValue)
    {
        return ushort.TryParse(value, out var result) ? result : defaultValue;
    }

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
    {
        return int.TryParse(value, out var result) ? result : null;
    }

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
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Attempts to parse the string as an unsigned 32-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed uint if successful; otherwise, <c>null</c>.</returns>
    public static uint? TryParseUInt32(this string? value)
    {
        return uint.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as an unsigned 32-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed uint if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static uint TryParseUInt32(this string? value, uint defaultValue)
    {
        return uint.TryParse(value, out var result) ? result : defaultValue;
    }

    // ========== 64-bit Integer Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a signed 64-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed long if successful; otherwise, <c>null</c>.</returns>
    public static long? TryParseInt64(this string? value)
    {
        return long.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a signed 64-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed long if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static long TryParseInt64(this string? value, long defaultValue)
    {
        return long.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Attempts to parse the string as an unsigned 64-bit integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed ulong if successful; otherwise, <c>null</c>.</returns>
    public static ulong? TryParseUInt64(this string? value)
    {
        return ulong.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as an unsigned 64-bit integer, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed ulong if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static ulong TryParseUInt64(this string? value, ulong defaultValue)
    {
        return ulong.TryParse(value, out var result) ? result : defaultValue;
    }

    // ========== Floating-Point Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a single-precision floating-point number.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed float if successful; otherwise, <c>null</c>.</returns>
    public static float? TryParseSingle(this string? value)
    {
        return float.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a single-precision floating-point number, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed float if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static float TryParseSingle(this string? value, float defaultValue)
    {
        return float.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Attempts to parse the string as a double-precision floating-point number.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed number if successful; otherwise, <c>null</c>.</returns>
    public static double? TryParseDouble(this string? value)
    {
        return double.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a double-precision floating-point number, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed number if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static double TryParseDouble(this string? value, double defaultValue)
    {
        return double.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Attempts to parse the string as a decimal number.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed decimal if successful; otherwise, <c>null</c>.</returns>
    public static decimal? TryParseDecimal(this string? value)
    {
        return decimal.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a decimal number, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed decimal if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static decimal TryParseDecimal(this string? value, decimal defaultValue)
    {
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }

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
    {
        return bool.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a boolean, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed boolean if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static bool TryParseBool(this string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

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
    {
        return char.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a single character, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed character if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static char TryParseChar(this string? value, char defaultValue)
    {
        return char.TryParse(value, out var result) ? result : defaultValue;
    }

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
    {
        return Guid.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a <see cref="Guid" />, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed GUID if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static Guid TryParseGuid(this string? value, Guid defaultValue)
    {
        return Guid.TryParse(value, out var result) ? result : defaultValue;
    }

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
    {
        return Enum.TryParse<TEnum>(value, ignoreCase, out var result) ? result : null;
    }

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
    {
        return Enum.TryParse<TEnum>(value, ignoreCase, out var result) ? result : defaultValue;
    }

    // ========== Date/Time Parsing ==========

    /// <summary>
    ///     Attempts to parse the string as a <see cref="DateTime" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed date/time if successful; otherwise, <c>null</c>.</returns>
    public static DateTime? TryParseDateTime(this string? value)
    {
        return DateTime.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a <see cref="DateTime" />, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed date/time if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static DateTime TryParseDateTime(this string? value, DateTime defaultValue)
    {
        return DateTime.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Attempts to parse the string as a <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed date/time offset if successful; otherwise, <c>null</c>.</returns>
    public static DateTimeOffset? TryParseDateTimeOffset(this string? value)
    {
        return DateTimeOffset.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a <see cref="DateTimeOffset" />, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed date/time offset if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static DateTimeOffset TryParseDateTimeOffset(this string? value, DateTimeOffset defaultValue)
    {
        return DateTimeOffset.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Attempts to parse the string as a <see cref="TimeSpan" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed time span if successful; otherwise, <c>null</c>.</returns>
    public static TimeSpan? TryParseTimeSpan(this string? value)
    {
        return TimeSpan.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    ///     Attempts to parse the string as a <see cref="TimeSpan" />, returning a default on failure.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed time span if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public static TimeSpan TryParseTimeSpan(this string? value, TimeSpan defaultValue)
    {
        return TimeSpan.TryParse(value, out var result) ? result : defaultValue;
    }

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
    public static T? ElementAtOrNull<T>(this IReadOnlyList<T> list, int index) where T : class
    {
        return index >= 0 && index < list.Count ? list[index] : null;
    }

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
    public static T? ValueAtOrNull<T>(this IReadOnlyList<T> list, int index) where T : struct
    {
        return index >= 0 && index < list.Count ? list[index] : null;
    }

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
    public static T ElementAtOrDefault<T>(this IReadOnlyList<T> list, int index, T defaultValue)
    {
        return index >= 0 && index < list.Count ? list[index] : defaultValue;
    }

    // ========== Base64Url Try-Parsers ==========

    /// <summary>
    ///     Attempts to decode a URL-safe Base64 string (padded or unpadded) to bytes, returning <c>null</c> on failure.
    /// </summary>
    /// <param name="input">The URL-safe Base64 string. May be <c>null</c>, empty, padded, or unpadded.</param>
    /// <returns>
    ///     The decoded bytes, or <c>null</c> when the input is null, whitespace, or malformed.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Swallows both <see cref="FormatException" /> and <see cref="ArgumentException" /> —
    ///         a narrow <see cref="FormatException" /> catch would let <see cref="ArgumentException" />
    ///         bubble up on certain malformed lengths.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Base64Url.TryDecode(string?, out byte[])" />
    public static byte[]? TryParseBase64Url(this string? input)
    {
        return Base64Url.TryDecode(input, out var bytes) ? bytes : null;
    }
}
