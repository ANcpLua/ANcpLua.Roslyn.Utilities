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
///         <item><description>Provides safe conversions with default value fallbacks.</description></item>
///         <item><description>Supports nullable return types for optional attribute arguments.</description></item>
///         <item><description>Handles boolean, enum, and other primitive type conversions.</description></item>
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
    public static bool ToBoolean(this TypedConstant typedConstant, bool defaultValue = false) =>
        typedConstant.Value switch
        {
            bool b => b,
            _ => defaultValue
        };

    /// <summary>
    ///     Converts the <see cref="TypedConstant" /> to a nullable <see cref="bool" /> value.
    /// </summary>
    /// <param name="typedConstant">The typed constant to convert.</param>
    /// <returns>
    ///     The boolean value contained in <paramref name="typedConstant" /> if it is a boolean;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <seealso cref="ToBoolean" />
    public static bool? ToNullableBoolean(this TypedConstant typedConstant) =>
        typedConstant.Value switch
        {
            bool b => b,
            _ => null
        };

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
    public static T ToEnum<T>(this TypedConstant typedConstant, T defaultValue) where T : Enum =>
        (T)(typedConstant.Value ?? defaultValue);

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
        if (typedConstant.Value is null) return null;

        return (T)typedConstant.Value;
    }
}