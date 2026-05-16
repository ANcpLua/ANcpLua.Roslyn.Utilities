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
    ///     Validates that a double is not zero and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NotZero(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is 0.0)
            ThrowOutOfRange(paramName, value, "Value cannot be zero.");
        return value;
    }

    /// <summary>
    ///     Validates that a double is not negative and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NotNegative(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!(value >= 0.0)) // Handles NaN correctly
            ThrowOutOfRange(paramName, value, "Value cannot be negative.");
        return value;
    }

    /// <summary>
    ///     Validates that a double is positive (greater than zero) and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Positive(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!(value > 0.0)) // Handles NaN correctly
            ThrowOutOfRange(paramName, value, "Value must be positive.");
        return value;
    }

    /// <summary>
    ///     Validates that a double is not greater than the specified maximum and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NotGreaterThan(double value, double max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (!(value <= max)) // Handles NaN correctly
            ThrowOutOfRange(paramName, value, $"Value must not be greater than {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that a double is not less than the specified minimum and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NotLessThan(double value, double min,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (!(value >= min)) // Handles NaN correctly
            ThrowOutOfRange(paramName, value, $"Value must not be less than {min}.");
        return value;
    }

    /// <summary>
    ///     Validates that a double is less than the specified maximum and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double LessThan(double value, double max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (!(value < max)) // Handles NaN correctly
            ThrowOutOfRange(paramName, value, $"Value must be less than {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that a double is greater than the specified minimum and returns it.
    /// </summary>
    /// <remarks>NaN values will throw.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GreaterThan(double value, double min,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (!(value > min)) // Handles NaN correctly
            ThrowOutOfRange(paramName, value, $"Value must be greater than {min}.");
        return value;
    }

    /// <summary>
    ///     Validates that a double is not NaN and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NotNaN(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (double.IsNaN(value))
            ThrowOutOfRange(paramName, value, "Value cannot be NaN.");
        return value;
    }

    /// <summary>
    ///     Validates that a double is finite (not NaN or infinity) and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Finite(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            ThrowOutOfRange(paramName, value, "Value must be finite.");
        return value;
    }
}
