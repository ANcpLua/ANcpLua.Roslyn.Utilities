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
    ///     Validates that an integer is not zero and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NotZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is 0)
            ThrowOutOfRange(paramName, value, "Value cannot be zero.");
        return value;
    }

    /// <summary>
    ///     Validates that an integer is not negative and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NotNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 0)
            ThrowOutOfRange(paramName, value, "Value cannot be negative.");
        return value;
    }

    /// <summary>
    ///     Validates that an integer is positive (greater than zero) and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Positive(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
            ThrowOutOfRange(paramName, value, "Value must be positive.");
        return value;
    }

    /// <summary>
    ///     Validates that an integer is not greater than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NotGreaterThan(int value, int max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value > max)
            ThrowOutOfRange(paramName, value, $"Value must not be greater than {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that an integer is not less than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NotLessThan(int value, int min,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value < min)
            ThrowOutOfRange(paramName, value, $"Value must not be less than {min}.");
        return value;
    }

    /// <summary>
    ///     Validates that an integer is less than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LessThan(int value, int max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value >= max)
            ThrowOutOfRange(paramName, value, $"Value must be less than {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that an integer is greater than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GreaterThan(int value, int min,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value <= min)
            ThrowOutOfRange(paramName, value, $"Value must be greater than {min}.");
        return value;
    }

    /// <summary>
    ///     Validates that a long is not zero and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NotZero(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is 0L)
            ThrowOutOfRange(paramName, value, "Value cannot be zero.");
        return value;
    }

    /// <summary>
    ///     Validates that a long is not negative and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NotNegative(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 0L)
            ThrowOutOfRange(paramName, value, "Value cannot be negative.");
        return value;
    }

    /// <summary>
    ///     Validates that a long is positive (greater than zero) and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Positive(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0L)
            ThrowOutOfRange(paramName, value, "Value must be positive.");
        return value;
    }

    /// <summary>
    ///     Validates that a long is not greater than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NotGreaterThan(long value, long max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value > max)
            ThrowOutOfRange(paramName, value, $"Value must not be greater than {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that a long is not less than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NotLessThan(long value, long min,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value < min)
            ThrowOutOfRange(paramName, value, $"Value must not be less than {min}.");
        return value;
    }

    /// <summary>
    ///     Validates that a long is less than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long LessThan(long value, long max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value >= max)
            ThrowOutOfRange(paramName, value, $"Value must be less than {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that a long is greater than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GreaterThan(long value, long min,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value <= min)
            ThrowOutOfRange(paramName, value, $"Value must be greater than {min}.");
        return value;
    }

    /// <summary>
    ///     Validates that a decimal is not zero and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal NotZero(decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == 0m)
            ThrowOutOfRange(paramName, value, "Value cannot be zero.");
        return value;
    }

    /// <summary>
    ///     Validates that a decimal is not negative and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal NotNegative(decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 0m)
            ThrowOutOfRange(paramName, value, "Value cannot be negative.");
        return value;
    }

    /// <summary>
    ///     Validates that a decimal is positive (greater than zero) and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal Positive(decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0m)
            ThrowOutOfRange(paramName, value, "Value must be positive.");
        return value;
    }

    /// <summary>
    ///     Validates that a decimal is not greater than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal NotGreaterThan(decimal value, decimal max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value > max)
            ThrowOutOfRange(paramName, value, $"Value must not be greater than {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that a decimal is not less than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal NotLessThan(decimal value, decimal min,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value < min)
            ThrowOutOfRange(paramName, value, $"Value must not be less than {min}.");
        return value;
    }

    /// <summary>
    ///     Validates that a decimal is less than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal LessThan(decimal value, decimal max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value >= max)
            ThrowOutOfRange(paramName, value, $"Value must be less than {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that a decimal is greater than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal GreaterThan(decimal value, decimal min,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value <= min)
            ThrowOutOfRange(paramName, value, $"Value must be greater than {min}.");
        return value;
    }

    /// <summary>
    ///     Validates that a <see cref="TimeSpan" /> is not <see cref="TimeSpan.Zero" /> and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan NotZero(TimeSpan value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == TimeSpan.Zero)
            ThrowOutOfRange(paramName, value, "Duration cannot be zero.");
        return value;
    }

    /// <summary>
    ///     Validates that a <see cref="TimeSpan" /> is not negative (i.e. <c>&gt;= TimeSpan.Zero</c>) and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan NotNegative(TimeSpan value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value < TimeSpan.Zero)
            ThrowOutOfRange(paramName, value, "Duration cannot be negative.");
        return value;
    }

    /// <summary>
    ///     Validates that a <see cref="TimeSpan" /> is positive (i.e. <c>&gt; TimeSpan.Zero</c>) and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan Positive(TimeSpan value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= TimeSpan.Zero)
            ThrowOutOfRange(paramName, value, "Duration must be positive.");
        return value;
    }

    /// <summary>
    ///     Validates that a <see cref="TimeSpan" /> is not greater than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan NotGreaterThan(TimeSpan value, TimeSpan max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value > max)
            ThrowOutOfRange(paramName, value, $"Duration must not be greater than {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that a <see cref="TimeSpan" /> is not less than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan NotLessThan(TimeSpan value, TimeSpan min,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value < min)
            ThrowOutOfRange(paramName, value, $"Duration must not be less than {min}.");
        return value;
    }

    /// <summary>
    ///     Validates that a <see cref="TimeSpan" /> is strictly less than the specified maximum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan LessThan(TimeSpan value, TimeSpan max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value >= max)
            ThrowOutOfRange(paramName, value, $"Duration must be less than {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that a <see cref="TimeSpan" /> is strictly greater than the specified minimum and returns it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan GreaterThan(TimeSpan value, TimeSpan min,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value <= min)
            ThrowOutOfRange(paramName, value, $"Duration must be greater than {min}.");
        return value;
    }
}
