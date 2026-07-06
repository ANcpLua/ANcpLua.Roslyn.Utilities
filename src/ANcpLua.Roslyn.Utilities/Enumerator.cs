using System.Collections;
using System.Collections.Generic;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides cached empty enumerators.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class Enumerator
{
    /// <summary>
    ///     Gets an empty non-generic enumerator.
    /// </summary>
    /// <returns>An empty enumerator.</returns>
    public static IEnumerator Empty()
    {
        return EmptyEnumeratorCache.Instance;
    }

    /// <summary>
    ///     Gets an empty generic enumerator.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>An empty enumerator.</returns>
    public static IEnumerator<T> Empty<T>()
    {
        return EmptyEnumeratorCache<T>.Instance;
    }

    private static class EmptyEnumeratorCache
    {
        public static readonly IEnumerator Instance = new NonGenericEnumerator();
    }

    private sealed class NonGenericEnumerator : IEnumerator
    {
        public object? Current => null;

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }
    }

    private static class EmptyEnumeratorCache<T>
    {
        public static readonly IEnumerator<T> Instance = new GenericEnumerator<T>();
    }

    private sealed class GenericEnumerator<T> : IEnumerator<T>
    {
        /// <summary>
        ///     Always returns <c>default(T)</c>; never observable since <see cref="MoveNext" /> always returns <c>false</c>.
        /// </summary>
        public T Current => default!;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
        }
    }
}
