namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class EnumerableExtensions
{
    /// <summary>
    ///     Returns the single element of the sequence, or a default value if the sequence is empty
    ///     or contains more than one element.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>
    ///     The single element in <paramref name="source" />, or <c>default</c> if the sequence
    ///     is empty or contains more than one element.
    /// </returns>
    /// <remarks>
    ///     Unlike <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource})" />,
    ///     this method does not throw an exception when the sequence contains multiple elements.
    /// </remarks>
    public static T? SingleOrDefaultIfMultiple<T>(this IEnumerable<T> source)
    {
        Guard.NotNull(source);

        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            return default;

        var result = enumerator.Current;
        return enumerator.MoveNext() ? default : result;
    }

    /// <summary>
    ///     Returns the only element of the sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>The only element in <paramref name="source" />.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="source" /> contains zero elements or more than one element.
    /// </exception>
    /// <seealso cref="OnlyOrDefault{T}(IEnumerable{T})" />
    public static T Only<T>(this IEnumerable<T> source)
    {
        Guard.That(
            TryOnly(source, out var result),
            "Sequence must contain exactly one element.",
            nameof(source));
        return result!;
    }

    /// <summary>
    ///     Returns the only element of the sequence that satisfies a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="predicate">A function used to test each element.</param>
    /// <returns>The only matching element in <paramref name="source" />.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="source" /> contains zero matching elements or more than one matching element.
    /// </exception>
    /// <seealso cref="OnlyOrDefault{T}(IEnumerable{T},Func{T,bool})" />
    public static T Only<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        Guard.That(
            TryOnly(source, predicate, out var result),
            "Sequence must contain exactly one matching element.",
            nameof(source));
        return result!;
    }

    /// <summary>
    ///     Returns the only element of the sequence, or <c>default</c> if the sequence does not contain exactly one element.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>The only element in <paramref name="source" />, or <c>default</c>.</returns>
    /// <seealso cref="Only{T}(IEnumerable{T})" />
    public static T? OnlyOrDefault<T>(this IEnumerable<T> source)
    {
        TryOnly(source, out var result);
        return result;
    }

    /// <summary>
    ///     Returns the only element of the sequence that satisfies a predicate, or <c>default</c> if there is not exactly one.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="predicate">A function used to test each element.</param>
    /// <returns>The only matching element in <paramref name="source" />, or <c>default</c>.</returns>
    /// <seealso cref="Only{T}(IEnumerable{T},Func{T,bool})" />
    public static T? OnlyOrDefault<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        TryOnly(source, predicate, out var result);
        return result;
    }

    private static bool TryOnly<T>(IEnumerable<T> source, out T? result)
    {
        Guard.NotNull(source);

        using var enumerator = source.GetEnumerator();
        if (enumerator.MoveNext())
        {
            result = enumerator.Current;
            if (!enumerator.MoveNext())
                return true;
        }

        result = default;
        return false;
    }

    private static bool TryOnly<T>(IEnumerable<T> source, Func<T, bool> predicate, out T? result)
    {
        Guard.NotNull(source);
        Guard.NotNull(predicate);

        result = default;
        var found = false;

        foreach (var item in source)
        {
            if (!predicate(item))
                continue;

            if (found)
            {
                result = default;
                return false;
            }

            result = item;
            found = true;
        }

        return found;
    }
}
