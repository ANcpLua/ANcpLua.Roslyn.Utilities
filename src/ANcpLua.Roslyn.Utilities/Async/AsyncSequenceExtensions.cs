#if !NETSTANDARD
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ANcpLua.Roslyn.Utilities.Async;

/// <summary>
///     LINQ-style composition helpers for <see cref="IAsyncEnumerable{T}" />.
///     Fills gaps not covered by <c>System.Linq.AsyncEnumerable</c>: chunking, flattening,
///     side-effect taps, null filtering, and sequential SelectMany.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class AsyncSequenceExtensions
{
    /// <summary>
    ///     Materializes the async sequence into an <see cref="IReadOnlyList{T}" />.
    /// </summary>
    public static async ValueTask<IReadOnlyList<T>> ToReadOnlyListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            list.Add(item);
        return list;
    }

    /// <summary>
    ///     Materializes the async sequence into an array.
    /// </summary>
    public static async ValueTask<T[]> ToArrayAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            list.Add(item);
        return list.ToArray();
    }

    /// <summary>
    ///     Injects a synchronous side-effect for each element without altering the sequence.
    /// </summary>
    public static async IAsyncEnumerable<T> Tap<T>(
        this IAsyncEnumerable<T> source,
        Action<T> onNext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        Guard.NotNull(onNext);

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            onNext(item);
            yield return item;
        }
    }

    /// <summary>
    ///     Injects an asynchronous side-effect for each element without altering the sequence.
    /// </summary>
    public static async IAsyncEnumerable<T> TapAsync<T>(
        this IAsyncEnumerable<T> source,
        Func<T, CancellationToken, ValueTask> onNext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        Guard.NotNull(onNext);

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await onNext(item, cancellationToken).ConfigureAwait(false);
            yield return item;
        }
    }

    /// <summary>
    ///     Filters out <see langword="null" /> elements from an async sequence of reference types.
    /// </summary>
    public static async IAsyncEnumerable<T> WhereNotNull<T>(
        this IAsyncEnumerable<T?> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : class
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (item is not null)
                yield return item;
        }
    }

    /// <summary>
    ///     Splits the async sequence into fixed-size chunks.
    /// </summary>
    public static async IAsyncEnumerable<IReadOnlyList<T>> Chunked<T>(
        this IAsyncEnumerable<T> source,
        int size,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

        var buffer = new List<T>(size);

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            buffer.Add(item);

            if (buffer.Count == size)
            {
                yield return buffer.ToArray();
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
            yield return buffer.ToArray();
    }

    /// <summary>
    ///     Flattens an async sequence of batches into a single async sequence.
    /// </summary>
    public static async IAsyncEnumerable<T> Flatten<T>(
        this IAsyncEnumerable<IEnumerable<T>> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        await foreach (var batch in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (var item in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }

    /// <summary>
    ///     Projects each element to an async sequence and flattens the results sequentially.
    /// </summary>
    public static async IAsyncEnumerable<TResult> SelectManySequential<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, IAsyncEnumerable<TResult>> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        Guard.NotNull(selector);

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await foreach (var result in selector(item, cancellationToken)
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false))
            {
                yield return result;
            }
        }
    }
}
#endif