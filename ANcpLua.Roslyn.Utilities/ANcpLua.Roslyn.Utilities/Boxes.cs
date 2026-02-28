using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides cached boxed values for common value types to avoid repeated allocations.
/// </summary>
/// <remarks>
///     <para>
///         Boxing a value type allocates a new object on the heap every time. In analyzer hot paths
///         (dictionary lookups, event args, parameter arrays), this can generate significant GC pressure.
///         This class caches commonly used boxed values so the same object is reused.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Boolean values are always cached — there are only two possible values.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Integer values in the range [-1..10] are cached, covering most loop counters,
///                 array indices, and sentinel values.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Character values in the ASCII range [0..127] are cached.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Zero values are cached for all numeric types.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// // Instead of boxing a bool on every call:
/// dict[key] = true;  // allocates each time
///
/// // Use cached boxed value:
/// dict[key] = Boxes.BoxedTrue;  // reuses same object
///
/// // Or use the Box method for dynamic values:
/// object boxed = Boxes.Box(someCondition);  // returns cached instance
///
/// // Small integers are cached too:
/// object zero = Boxes.Box(0);   // cached
/// object ten  = Boxes.Box(10);  // cached
/// object big  = Boxes.Box(999); // not cached, normal boxing
/// </code>
/// </example>
/// <seealso cref="HashCombiner" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class Boxes
{
    /// <summary>
    ///     A cached boxed <see cref="bool" /> value of <c>true</c>.
    /// </summary>
    public static readonly object BoxedTrue = true;

    /// <summary>
    ///     A cached boxed <see cref="bool" /> value of <c>false</c>.
    /// </summary>
    public static readonly object BoxedFalse = false;

    /// <summary>
    ///     A cached boxed <see cref="int" /> value of <c>0</c>.
    /// </summary>
    public static readonly object BoxedInt32Zero = 0;

    /// <summary>
    ///     A cached boxed <see cref="int" /> value of <c>1</c>.
    /// </summary>
    public static readonly object BoxedInt32One = 1;

    /// <summary>
    ///     A cached boxed <see cref="long" /> value of <c>0</c>.
    /// </summary>
    public static readonly object BoxedInt64Zero = 0L;

    /// <summary>
    ///     A cached boxed <see cref="double" /> value of <c>0.0</c>.
    /// </summary>
    public static readonly object BoxedDoubleZero = 0.0;

    /// <summary>
    ///     A cached boxed <see cref="decimal" /> value of <c>0m</c>.
    /// </summary>
    public static readonly object BoxedDecimalZero = 0m;

    /// <summary>
    ///     A cached boxed <see cref="byte" /> value of <c>0</c>.
    /// </summary>
    public static readonly object BoxedByteZero = (byte)0;

    /// <summary>
    ///     A cached boxed <see cref="sbyte" /> value of <c>0</c>.
    /// </summary>
    public static readonly object BoxedSByteZero = (sbyte)0;

    /// <summary>
    ///     A cached boxed <see cref="short" /> value of <c>0</c>.
    /// </summary>
    public static readonly object BoxedInt16Zero = (short)0;

    /// <summary>
    ///     A cached boxed <see cref="ushort" /> value of <c>0</c>.
    /// </summary>
    public static readonly object BoxedUInt16Zero = (ushort)0;

    /// <summary>
    ///     A cached boxed <see cref="uint" /> value of <c>0</c>.
    /// </summary>
    public static readonly object BoxedUInt32Zero = 0u;

    /// <summary>
    ///     A cached boxed <see cref="ulong" /> value of <c>0</c>.
    /// </summary>
    public static readonly object BoxedUInt64Zero = 0ul;

    /// <summary>
    ///     A cached boxed <see cref="float" /> value of <c>0.0f</c>.
    /// </summary>
    public static readonly object BoxedSingleZero = 0.0f;

    private static readonly object[] CachedInt32 = CreateInt32Cache();
    private static readonly object[] CachedChar = CreateCharCache();

    /// <summary>
    ///     Returns a cached boxed <see cref="bool" /> value.
    /// </summary>
    /// <param name="value">The boolean value to box.</param>
    /// <returns>
    ///     <see cref="BoxedTrue" /> if <paramref name="value" /> is <c>true</c>;
    ///     otherwise <see cref="BoxedFalse" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(bool value)
    {
        return value ? BoxedTrue : BoxedFalse;
    }

    /// <summary>
    ///     Returns a boxed <see cref="int" /> value, using a cached instance for values in the range [-1..10].
    /// </summary>
    /// <param name="value">The integer value to box.</param>
    /// <returns>A cached boxed instance for small values, or a new boxed instance for larger values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(int value)
    {
        // Cache range: -1 to 10 (index 0 to 11 in the array)
        int index = value + 1;
        if ((uint)index < (uint)CachedInt32.Length)
        {
            return CachedInt32[index];
        }

        return value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="long" /> value, using a cached instance for zero.
    /// </summary>
    /// <param name="value">The value to box.</param>
    /// <returns>A cached boxed instance for zero, or a new boxed instance for other values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(long value)
    {
        return value == 0 ? BoxedInt64Zero : value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="double" /> value, using a cached instance for zero.
    /// </summary>
    /// <param name="value">The value to box.</param>
    /// <returns>A cached boxed instance for zero, or a new boxed instance for other values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(double value)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return value == 0.0 ? BoxedDoubleZero : value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="decimal" /> value, using a cached instance for zero.
    /// </summary>
    /// <param name="value">The value to box.</param>
    /// <returns>A cached boxed instance for zero, or a new boxed instance for other values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(decimal value)
    {
        return value == 0m ? BoxedDecimalZero : value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="byte" /> value, using a cached instance for zero.
    /// </summary>
    /// <param name="value">The value to box.</param>
    /// <returns>A cached boxed instance for zero, or a new boxed instance for other values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(byte value)
    {
        return value == 0 ? BoxedByteZero : value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="sbyte" /> value, using a cached instance for zero.
    /// </summary>
    /// <param name="value">The value to box.</param>
    /// <returns>A cached boxed instance for zero, or a new boxed instance for other values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(sbyte value)
    {
        return value == 0 ? BoxedSByteZero : value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="short" /> value, using a cached instance for zero.
    /// </summary>
    /// <param name="value">The value to box.</param>
    /// <returns>A cached boxed instance for zero, or a new boxed instance for other values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(short value)
    {
        return value == 0 ? BoxedInt16Zero : value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="ushort" /> value, using a cached instance for zero.
    /// </summary>
    /// <param name="value">The value to box.</param>
    /// <returns>A cached boxed instance for zero, or a new boxed instance for other values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(ushort value)
    {
        return value == 0 ? BoxedUInt16Zero : value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="uint" /> value, using a cached instance for zero.
    /// </summary>
    /// <param name="value">The value to box.</param>
    /// <returns>A cached boxed instance for zero, or a new boxed instance for other values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(uint value)
    {
        return value == 0 ? BoxedUInt32Zero : value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="ulong" /> value, using a cached instance for zero.
    /// </summary>
    /// <param name="value">The value to box.</param>
    /// <returns>A cached boxed instance for zero, or a new boxed instance for other values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(ulong value)
    {
        return value == 0 ? BoxedUInt64Zero : value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="float" /> value, using a cached instance for zero.
    /// </summary>
    /// <param name="value">The value to box.</param>
    /// <returns>A cached boxed instance for zero, or a new boxed instance for other values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(float value)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return value == 0.0f ? BoxedSingleZero : value;
    }

    /// <summary>
    ///     Returns a boxed <see cref="char" /> value, using a cached instance for ASCII characters [0..127].
    /// </summary>
    /// <param name="value">The character value to box.</param>
    /// <returns>A cached boxed instance for ASCII characters, or a new boxed instance for others.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(char value)
    {
        if ((uint)value < (uint)CachedChar.Length)
        {
            return CachedChar[value];
        }

        return value;
    }

    private static object[] CreateInt32Cache()
    {
        // Cache values from -1 to 10 (12 entries)
        var cache = new object[12];
        for (int i = 0; i < cache.Length; i++)
        {
            cache[i] = i - 1;
        }

        return cache;
    }

    private static object[] CreateCharCache()
    {
        // Cache ASCII characters 0-127
        var cache = new object[128];
        for (int i = 0; i < cache.Length; i++)
        {
            cache[i] = (char)i;
        }

        return cache;
    }
}
