using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ANcpLua.Roslyn.Utilities.Performance;

/// <summary>
///     Low-level memory reinterpretation helpers backed by <see cref="MemoryMarshal" />.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class MemoryHelpers
{
    /// <summary>
    ///     Reinterprets a <see cref="Span{T}" /> of value types as a <see cref="Span{T}" /> of bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> AsBytes<T>(Span<T> span)
        where T : struct
    {
        return MemoryMarshal.AsBytes(span);
    }

    /// <summary>
    ///     Reinterprets a <see cref="ReadOnlySpan{T}" /> of value types as a <see cref="ReadOnlySpan{T}" /> of bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> AsBytes<T>(ReadOnlySpan<T> span)
        where T : struct
    {
        return MemoryMarshal.AsBytes(span);
    }

    /// <summary>
    ///     Casts a <see cref="Span{T}" /> of one value type to another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<TTo> Cast<TFrom, TTo>(Span<TFrom> span)
        where TFrom : struct
        where TTo : struct
    {
        return MemoryMarshal.Cast<TFrom, TTo>(span);
    }

    /// <summary>
    ///     Casts a <see cref="ReadOnlySpan{T}" /> of one value type to another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<TTo> Cast<TFrom, TTo>(ReadOnlySpan<TFrom> span)
        where TFrom : struct
        where TTo : struct
    {
        return MemoryMarshal.Cast<TFrom, TTo>(span);
    }

    /// <summary>
    ///     Returns a reference to the first element of the span, or a null reference if empty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReference<T>(Span<T> span)
    {
        return ref MemoryMarshal.GetReference(span);
    }

    /// <summary>
    ///     Returns a reference to the first element of the span, or a null reference if empty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReference<T>(ReadOnlySpan<T> span)
    {
        return ref MemoryMarshal.GetReference(span);
    }
}