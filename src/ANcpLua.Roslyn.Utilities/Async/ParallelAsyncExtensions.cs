#if !NETSTANDARD
using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ANcpLua.Roslyn.Utilities.Async;

/// <summary>
///     Channel-based parallel fan-out for <see cref="IAsyncEnumerable{T}" />.
///     <para>
///         Uses a bounded input channel for backpressure and an unbounded output channel
///         for maximum throughput. Results arrive in completion order, not source order.
///     </para>
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ParallelAsyncExtensions
{
    /// <summary>
    ///     Projects each element through <paramref name="selector" /> using
    ///     <paramref name="degreeOfParallelism" /> concurrent workers backed by channels.
    /// </summary>
    /// <remarks>
    ///     <para>Results arrive in <b>completion order</b>, not source order.</para>
    ///     <para>
    ///         The first exception from any worker is captured and re-thrown with its original
    ///         stack trace after the output channel drains.
    ///     </para>
    /// </remarks>
    public static IAsyncEnumerable<TResult> SelectParallel<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        int degreeOfParallelism,
        Func<TSource, CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        Guard.NotNull(selector);
        Guard.Positive(degreeOfParallelism);
        return Core(source, degreeOfParallelism, selector, cancellationToken);

        static async IAsyncEnumerable<TResult> Core(
            IAsyncEnumerable<TSource> source,
            int degreeOfParallelism,
            Func<TSource, CancellationToken, ValueTask<TResult>> selector,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var input = Channel.CreateBounded<TSource>(new BoundedChannelOptions(degreeOfParallelism * 2)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });

            var output = Channel.CreateUnbounded<TResult>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });

            Exception? failure = null;

            var producer = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                        await input.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Expected cancellation — let finally complete the channel.
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref failure, ex, null);
                }
                finally
                {
                    input.Writer.TryComplete(failure);
                }
            }, CancellationToken.None);

            var workers = new Task[degreeOfParallelism];
            for (var w = 0; w < degreeOfParallelism; w++)
            {
                workers[w] = Task.Run(async () =>
                {
                    try
                    {
                        while (await input.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            while (input.Reader.TryRead(out var item))
                            {
                                var result = await selector(item, cancellationToken).ConfigureAwait(false);
                                await output.Writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref failure, ex, null);
                    }
                }, CancellationToken.None);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await producer.ConfigureAwait(false);
                    await Task.WhenAll(workers).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref failure, ex, null);
                }
                finally
                {
                    output.Writer.TryComplete(failure);
                }
            }, CancellationToken.None);

            while (await output.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (output.Reader.TryRead(out var item))
                    yield return item;
            }

            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    /// <summary>
    ///     Projects each element through <paramref name="selector" /> using
    ///     <paramref name="degreeOfParallelism" /> concurrent workers backed by channels,
    ///     yielding results in the <b>original source order</b>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="SelectParallel{TSource,TResult}" /> which returns results
    ///         in completion order, this method buffers out-of-order results and yields them
    ///         only when all preceding items have been emitted.
    ///     </para>
    ///     <para>
    ///         The reorder buffer is bounded by the number of in-flight items
    ///         (<paramref name="degreeOfParallelism" /> × 2 channel capacity + workers),
    ///         so memory usage stays proportional to concurrency, not source size.
    ///     </para>
    ///     <para>
    ///         The first exception from any worker is captured and re-thrown with its original
    ///         stack trace after all successfully completed results have been yielded.
    ///     </para>
    /// </remarks>
    public static IAsyncEnumerable<TResult> SelectParallelOrdered<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        int degreeOfParallelism,
        Func<TSource, CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        Guard.NotNull(selector);
        Guard.Positive(degreeOfParallelism);
        return CoreOrdered(source, degreeOfParallelism, selector, cancellationToken);

        static async IAsyncEnumerable<TResult> CoreOrdered(
            IAsyncEnumerable<TSource> source,
            int degreeOfParallelism,
            Func<TSource, CancellationToken, ValueTask<TResult>> selector,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var input = Channel.CreateBounded<(int Index, TSource Item)>(
                new BoundedChannelOptions(degreeOfParallelism * 2)
                {
                    SingleWriter = true,
                    SingleReader = false,
                    FullMode = BoundedChannelFullMode.Wait
                });

            var output = Channel.CreateUnbounded<(int Index, TResult Result)>(
                new UnboundedChannelOptions
                {
                    SingleWriter = false,
                    SingleReader = true
                });

            Exception? failure = null;

            var producer = Task.Run(async () =>
            {
                try
                {
                    var index = 0;
                    await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                        await input.Writer.WriteAsync((index++, item), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref failure, ex, null);
                }
                finally
                {
                    input.Writer.TryComplete(failure);
                }
            }, CancellationToken.None);

            var workers = new Task[degreeOfParallelism];
            for (var w = 0; w < degreeOfParallelism; w++)
            {
                workers[w] = Task.Run(async () =>
                {
                    try
                    {
                        while (await input.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            while (input.Reader.TryRead(out var entry))
                            {
                                var result = await selector(entry.Item, cancellationToken).ConfigureAwait(false);
                                await output.Writer.WriteAsync((entry.Index, result), cancellationToken)
                                    .ConfigureAwait(false);
                            }
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref failure, ex, null);
                    }
                }, CancellationToken.None);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await producer.ConfigureAwait(false);
                    await Task.WhenAll(workers).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref failure, ex, null);
                }
                finally
                {
                    output.Writer.TryComplete(failure);
                }
            }, CancellationToken.None);

            // Reorder buffer: hold results that arrived ahead of their turn
            var pending = new SortedDictionary<int, TResult>();
            var nextExpected = 0;

            while (await output.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (output.Reader.TryRead(out var entry))
                {
                    pending[entry.Index] = entry.Result;

                    // Flush consecutive items starting from nextExpected
                    while (pending.TryGetValue(nextExpected, out var next))
                    {
                        pending.Remove(nextExpected);
                        yield return next;
                        nextExpected++;
                    }
                }
            }

            // Drain any remaining buffered items in order
            foreach (var remaining in pending.Values)
                yield return remaining;

            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
#endif