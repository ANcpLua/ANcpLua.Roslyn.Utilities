using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for converting <see cref="TypedConstant" /> values.
/// </summary>
public static class ConvertExtensions
{
    /// <summary>
    ///     Converts the typed constant to a boolean.
    /// </summary>
    /// <param name="defaultValue"></param>
    /// <param name="typedConstant"></param>
    /// <returns></returns>
    public static bool ToBoolean(this TypedConstant typedConstant, bool defaultValue = false) =>
        typedConstant.Value switch
        {
            bool b => b,
            _ => defaultValue
        };

    /// <summary>
    ///     Converts the typed constant to a nullable boolean.
    /// </summary>
    /// <param name="typedConstant"></param>
    /// <returns></returns>
    public static bool? ToNullableBoolean(this TypedConstant typedConstant) =>
        typedConstant.Value switch
        {
            bool b => b,
            _ => null
        };

    /// <summary>
    ///     Converts the typed constant to an enum value.
    /// </summary>
    /// <param name="defaultValue"></param>
    /// <param name="typedConstant"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T ToEnum<T>(this TypedConstant typedConstant, T defaultValue) where T : Enum =>
        (T)(typedConstant.Value ?? defaultValue);

    /// <summary>
    ///     Converts the typed constant to a nullable enum value.
    /// </summary>
    /// <param name="typedConstant"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? ToEnum<T>(this TypedConstant typedConstant) where T : struct, Enum
    {
        if (typedConstant.Value is null) return null;

        return (T)typedConstant.Value;
    }
}