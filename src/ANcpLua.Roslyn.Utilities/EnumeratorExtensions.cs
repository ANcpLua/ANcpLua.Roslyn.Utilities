using System.Collections;
using System.Collections.Generic;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for adapting enumerator shapes.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class EnumeratorExtensions
{
    /// <summary>
    ///     Wraps a key-value-pair enumerator as an <see cref="IDictionaryEnumerator" />.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="enumerator">The enumerator to wrap.</param>
    /// <returns>A dictionary enumerator over the same sequence.</returns>
    public static IDictionaryEnumerator ToDictionaryEnumerator<TKey, TValue>(
        this IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
        where TKey : notnull
    {
        return new DictionaryEnumerator<TKey, TValue>(Guard.NotNull(enumerator));
    }

    private sealed class DictionaryEnumerator<TKey, TValue> : IDictionaryEnumerator, IDisposable
        where TKey : notnull
    {
        private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

        public DictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
        {
            _enumerator = enumerator;
        }

        public DictionaryEntry Entry =>
            new(_enumerator.Current.Key, _enumerator.Current.Value);

        public object Key => Entry.Key;

        public object? Value => Entry.Value;

        public object? Current => Entry;

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
