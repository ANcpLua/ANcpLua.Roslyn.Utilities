using System;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for converting <see cref="TypedConstant" /> values.
/// </summary>
public static class ConvertExtensions
{
    /// <param name="typedConstant"></param>
    extension(TypedConstant typedConstant)
    {
        /// <summary>
        ///     Converts the typed constant to a boolean.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public bool ToBoolean(bool defaultValue = false)
        {
            if (typedConstant.Value == null) return defaultValue;

            return (bool)typedConstant.Value!;
        }

        /// <summary>
        ///     Converts the typed constant to a nullable boolean.
        /// </summary>
        /// <returns></returns>
        public bool? ToNullableBoolean()
        {
            if (typedConstant.Value == null) return null;

            return (bool)typedConstant.Value!;
        }

        /// <summary>
        ///     Converts the typed constant to an enum value.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ToEnum<T>(T defaultValue) where T : Enum
        {
            return (T)(typedConstant.Value ?? defaultValue);
        }

        /// <summary>
        ///     Converts the typed constant to a nullable enum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? ToEnum<T>() where T : struct, Enum
        {
            if (typedConstant.Value == null) return null;

            return (T)typedConstant.Value;
        }
    }
}