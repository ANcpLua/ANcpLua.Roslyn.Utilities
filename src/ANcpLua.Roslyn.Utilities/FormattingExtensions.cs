using System.Text;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for low-allocation formatting and copying helpers.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class FormattingExtensions
{
    /// <summary>
    ///     Tries to copy a string into a destination span.
    /// </summary>
    /// <param name="source">The string to copy.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">The number of characters copied when successful; otherwise, <c>0</c>.</param>
    /// <returns><c>true</c> when the destination was large enough; otherwise, <c>false</c>.</returns>
    public static bool TryCopyTo(this string source, Span<char> destination, out int charsWritten)
    {
        Guard.NotNull(source);
        var copied = source.AsSpan().TryCopyTo(destination);
        charsWritten = copied ? source.Length : 0;
        return copied;
    }

    /// <summary>
    ///     Tries to copy a span into a destination span.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source span.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="itemsWritten">The number of items copied when successful; otherwise, <c>0</c>.</param>
    /// <returns><c>true</c> when the destination was large enough; otherwise, <c>false</c>.</returns>
    public static bool TryCopyTo<T>(this Span<T> source, Span<T> destination, out int itemsWritten)
    {
        var copied = source.TryCopyTo(destination);
        itemsWritten = copied ? source.Length : 0;
        return copied;
    }

    /// <summary>
    ///     Tries to copy a read-only span into a destination span.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source span.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="itemsWritten">The number of items copied when successful; otherwise, <c>0</c>.</param>
    /// <returns><c>true</c> when the destination was large enough; otherwise, <c>false</c>.</returns>
    public static bool TryCopyTo<T>(this ReadOnlySpan<T> source, Span<T> destination, out int itemsWritten)
    {
        var copied = source.TryCopyTo(destination);
        itemsWritten = copied ? source.Length : 0;
        return copied;
    }

    /// <summary>
    ///     Converts a <see cref="StringBuilder" /> to a string and clears it for reuse.
    /// </summary>
    /// <param name="source">The builder to read and clear.</param>
    /// <returns>The builder contents before it was cleared.</returns>
    public static string ToStringAndClear(this StringBuilder source)
    {
        Guard.NotNull(source);
        var value = source.ToString();
        source.Clear();
        return value;
    }
}
