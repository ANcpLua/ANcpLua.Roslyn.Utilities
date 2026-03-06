using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities.Performance;

/// <summary>
///     Allocation-free tokenizer over <see cref="ReadOnlySpan{T}" /> of <see langword="char" />.
///     Supports <c>foreach</c> via duck-typed enumerator pattern.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    ref struct SpanTokenizer
{
    private ReadOnlySpan<char> _remaining;
    private readonly char _separator;
    private bool _done;

    /// <summary>
    ///     Creates a tokenizer that splits <paramref name="span" /> on <paramref name="separator" />.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanTokenizer(ReadOnlySpan<char> span, char separator)
    {
        _remaining = span;
        _separator = separator;
        _done = false;
        Current = default;
    }

    /// <summary>Gets the current token.</summary>
    public ReadOnlySpan<char> Current { get; private set; }

    /// <summary>Advances to the next token. Returns <see langword="false" /> when exhausted.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        if (_done)
            return false;

        var idx = _remaining.IndexOf(_separator);
        if (idx < 0)
        {
            Current = _remaining;
            _done = true;
            return true;
        }

        Current = _remaining.Slice(0, idx);
        _remaining = _remaining.Slice(idx + 1);
        return true;
    }

    /// <summary>Returns this instance as the enumerator (duck-typed <c>foreach</c> support).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanTokenizer GetEnumerator() => this;
}
