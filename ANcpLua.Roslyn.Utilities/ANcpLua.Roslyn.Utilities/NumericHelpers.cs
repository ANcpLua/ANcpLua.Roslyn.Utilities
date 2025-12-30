namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Helper methods for numeric type operations.
/// </summary>
public static class NumericHelpers
{
    /// <summary>
    ///     Checks if a boxed value represents zero.
    /// </summary>
    public static bool IsZero(object? value) =>
        value switch
        {
            int i => i is 0,
            long l => l == 0,
            double d => d is 0.0,
            float f => f is 0.0f,
            decimal dec => dec is 0m,
            byte b => b is 0,
            sbyte sb => sb is 0,
            short s => s is 0,
            ushort us => us is 0,
            uint ui => ui == 0,
            ulong ul => ul == 0,
            _ => false,
        };
}
