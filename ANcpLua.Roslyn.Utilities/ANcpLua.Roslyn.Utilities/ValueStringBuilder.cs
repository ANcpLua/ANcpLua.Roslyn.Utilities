// Licensed under the MIT License.

using System.Buffers;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     A lightweight, pooled string builder for low-allocation source generation.
/// </summary>
/// <remarks>
///     <para>
///         This type minimizes allocations by using a caller-provided buffer first and
///         renting larger buffers from <see cref="ArrayPool{T}" /> when needed.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Use <see cref="Dispose" /> to return pooled buffers.</description>
///         </item>
///         <item>
///             <description>Designed for short-lived, hot-path string construction.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="ArrayPool{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    ref struct ValueStringBuilder
{
    private char[]? _arrayFromPool;
    private Span<char> _span;
    private int _pos;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValueStringBuilder" /> with an initial buffer.
    /// </summary>
    /// <param name="initialBuffer">The initial buffer used before renting from the pool.</param>
    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _arrayFromPool = null;
        _span = initialBuffer;
        _pos = 0;
    }

    /// <summary>
    ///     Returns any rented buffers to the shared pool.
    /// </summary>
    public readonly void Dispose()
    {
        if (_arrayFromPool is not null)
            ArrayPool<char>.Shared.Return(_arrayFromPool);
    }

    /// <summary>
    ///     Appends a string to the builder.
    /// </summary>
    /// <param name="str">The string to append.</param>
    public void Append(string str)
    {
        if (str.Length > _span.Length - _pos)
            Grow(str.Length);

        str.AsSpan().CopyTo(_span[_pos..]);
        _pos += str.Length;
    }

    /// <summary>
    ///     Appends a span of characters to the builder.
    /// </summary>
    /// <param name="str">The span to append.</param>
    public void Append(ReadOnlySpan<char> str)
    {
        if (str.Length > _span.Length - _pos)
            Grow(str.Length);

        str.CopyTo(_span[_pos..]);
        _pos += str.Length;
    }

    /// <summary>
    ///     Appends a single character to the builder.
    /// </summary>
    /// <param name="c">The character to append.</param>
    public void Append(char c)
    {
        if (_pos >= _span.Length)
            Grow(1);

        _span[_pos++] = c;
    }

    private void Grow(int additional)
    {
        var newSize = Math.Max(_span.Length * 2, _pos + additional);
        var newArray = ArrayPool<char>.Shared.Rent(newSize);
        _span.CopyTo(newArray);

        if (_arrayFromPool is not null)
            ArrayPool<char>.Shared.Return(_arrayFromPool);

        _arrayFromPool = newArray;
        _span = _arrayFromPool.AsSpan(0, newSize);
    }

    /// <summary>
    ///     Returns the current contents as a trimmed string.
    /// </summary>
    /// <returns>The trimmed string representation.</returns>
    public readonly string ToTrimString() => new(_span[.._pos].Trim().ToArray());

    /// <summary>
    ///     Returns the current contents as a string.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override readonly string ToString() => new(_span[.._pos].ToArray());
}
