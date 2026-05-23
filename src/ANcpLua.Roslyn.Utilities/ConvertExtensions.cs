using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for converting <see cref="TypedConstant" /> values to strongly-typed representations.
/// </summary>
/// <remarks>
///     <para>
///         These extension methods simplify the extraction of typed values from Roslyn's
///         <see cref="TypedConstant" /> structure, which is commonly encountered when processing
///         attribute arguments in analyzers and source generators.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Provides safe conversions with default value fallbacks.</description>
///         </item>
///         <item>
///             <description>Supports nullable return types for optional attribute arguments.</description>
///         </item>
///         <item>
///             <description>Handles boolean, enum, and other primitive type conversions.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="TypedConstant" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ConvertExtensions
{
    /// <summary>
    ///     Converts the <see cref="TypedConstant" /> to a <see cref="bool" /> value.
    /// </summary>
    /// <param name="typedConstant">The typed constant to convert.</param>
    /// <param name="defaultValue">
    ///     The default value to return if the typed constant does not contain a boolean value.
    ///     Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     The boolean value contained in <paramref name="typedConstant" /> if it is a boolean;
    ///     otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <seealso cref="ToNullableBoolean" />
    public static bool ToBoolean(this TypedConstant typedConstant, bool defaultValue = false)
    {
        return typedConstant.Value switch
        {
            bool b => b,
            _ => defaultValue
        };
    }

    /// <summary>
    ///     Converts the <see cref="TypedConstant" /> to a nullable <see cref="bool" /> value.
    /// </summary>
    /// <param name="typedConstant">The typed constant to convert.</param>
    /// <returns>
    ///     The boolean value contained in <paramref name="typedConstant" /> if it is a boolean;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <seealso cref="ToBoolean" />
    public static bool? ToNullableBoolean(this TypedConstant typedConstant)
    {
        return typedConstant.Value switch
        {
            bool b => b,
            _ => null
        };
    }

    /// <summary>
    ///     Converts the <see cref="TypedConstant" /> to an enum value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The enum type to convert to. Must be a reference-compatible enum type.</typeparam>
    /// <param name="typedConstant">The typed constant to convert.</param>
    /// <param name="defaultValue">
    ///     The default value to return if the typed constant value is <c>null</c>.
    /// </param>
    /// <returns>
    ///     The enum value contained in <paramref name="typedConstant" /> cast to <typeparamref name="T" />,
    ///     or <paramref name="defaultValue" /> if the value is <c>null</c>.
    /// </returns>
    /// <seealso cref="ToEnum{T}(TypedConstant)" />
    public static T ToEnum<T>(this TypedConstant typedConstant, T defaultValue) where T : struct, Enum
    {
        if (!typedConstant.TryGetEnumValue<T>(out var value))
            return defaultValue;

        return value.GetValueOrDefault();
    }

    /// <summary>
    ///     Converts the <see cref="TypedConstant" /> to a nullable enum value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The enum type to convert to. Must be a value type enum.</typeparam>
    /// <param name="typedConstant">The typed constant to convert.</param>
    /// <returns>
    ///     The enum value contained in <paramref name="typedConstant" /> cast to <typeparamref name="T" />,
    ///     or <c>null</c> if the typed constant value is <c>null</c>.
    /// </returns>
    /// <seealso cref="ToEnum{T}(TypedConstant, T)" />
    public static T? ToEnum<T>(this TypedConstant typedConstant) where T : struct, Enum
    {
        if (!typedConstant.TryGetEnumValue<T>(out var value))
            return null;

        return value;
    }

    private static bool TryGetEnumValue<T>(this TypedConstant typedConstant, out T? value) where T : struct, Enum
    {
        if (typedConstant.Value is null || typedConstant.Value is not object rawValue)
        {
            value = null;
            return false;
        }

        if (rawValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        try
        {
            value = (T)Enum.ToObject(typeof(T), rawValue);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            value = null;
            return false;
        }
    }

    // ========== Hex Conversion ==========

    /// <summary>
    ///     Converts a read-only byte span to a lowercase hexadecimal string, or <see cref="string.Empty" /> if the span is empty.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <returns>
    ///     A lowercase hex string of length <c>2 * bytes.Length</c>, or an empty string when <paramref name="bytes" /> is empty.
    /// </returns>
    public static string ToHexLowerOrEmpty(this ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return string.Empty;

#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(bytes);
#elif NET5_0_OR_GREATER
        return Convert.ToHexString(bytes).ToLowerInvariant();
#else
        var sb = new StringBuilder(bytes.Length * 2);
        for (var i = 0; i < bytes.Length; i++)
            sb.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
#endif
    }

    /// <summary>
    ///     Converts a byte array to a lowercase hexadecimal string, or <see cref="string.Empty" /> if the array is null or empty.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <returns>A lowercase hex string, or an empty string for null/empty input.</returns>
    public static string ToHexLowerOrEmpty(this byte[]? bytes)
    {
        if (bytes is null || bytes.Length is 0) return string.Empty;
        return ToHexLowerOrEmpty((ReadOnlySpan<byte>)bytes);
    }
}
