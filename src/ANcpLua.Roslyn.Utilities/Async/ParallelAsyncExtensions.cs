#if !NETSTANDARD
using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ANcpLua.Roslyn.Utilities.Async;

/// <summary>
///     Channel-based parallel fan-out for <see cref="IAsyncEnumerable{T}" />.
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
        ArgumentNullException.ThrowIfNull(source);
        Guard.NotNull(selector);
        Guard.Positive(degreeOfParallelism);

        return Core(source, degreeOfParallelism, selector, cancellationToken);

        static async IAsyncEnumerable<TResult> Core(
            IAsyncEnumerable<TSource> source,
            int degreeOfParallelism,
            Func<TSource, CancellationToken, ValueTask<TResult>> selector,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var capacity = degreeOfParallelism * 2;
            var input = Channel.CreateBounded<TSource>(new BoundedChannelOptions(capacity)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });

            var output = Channel.CreateBounded<TResult>(new BoundedChannelOptions(capacity)
            {
                SingleWriter = false,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });

            Exception? failure = null;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = cts.Token;

            void RecordFailure(Exception ex)
            {
                if (ex is OperationCanceledException)
                    return;

                if (Interlocked.CompareExchange(ref failure, ex, null) is null)
                    cts.Cancel();
            }

            var producer = Task.Run(
                async () =>
                {
                    try
                    {
                        await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                            await input.Writer.WriteAsync(item, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                    }
                    catch (Exception ex)
                    {
                        RecordFailure(ex);
                    }
                    finally
                    {
                        input.Writer.TryComplete(failure);
                    }
                },
                token);

            var workers = new Task[degreeOfParallelism];
            for (var w = 0; w < degreeOfParallelism; w++)
            {
                workers[w] = Task.Run(
                    async () =>
                    {
                        try
                        {
                            while (await input.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                            {
                                while (input.Reader.TryRead(out var item))
                                {
                                    var result = await selector(item, token).ConfigureAwait(false);
                                    await output.Writer.WriteAsync(result, token).ConfigureAwait(false);
                                }
                            }
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                        }
                        catch (Exception ex)
                        {
                            RecordFailure(ex);
                        }
                    },
                    token);
            }

            var completion = Task.Run(
                async () =>
                {
                    try
                    {
                        await producer.ConfigureAwait(false);
                        await Task.WhenAll(workers).ConfigureAwait(false);
                    }
                    finally
                    {
                        output.Writer.TryComplete(failure);
                    }
                },
                CancellationToken.None);

            // Code after this try/finally runs only once the reader has drained the channel. A consumer that
            // disposes mid-stream leaves through the finally, so secondary worker errors never reach it.
            try
            {
                while (await output.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (output.Reader.TryRead(out var item))
                        yield return item;
                }
            }
            finally
            {
#pragma warning disable CA1849 // CancelAsync is unavailable on netstandard2.0
                cts.Cancel();
#pragma warning restore CA1849
                try
                {
                    await completion.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
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
    public static IAsyncEnumerable<TResult> SelectParallelOrdered<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        int degreeOfParallelism,
        Func<TSource, CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        Guard.NotNull(selector);
        Guard.Positive(degreeOfParallelism);

        return CoreOrdered(source, degreeOfParallelism, selector, cancellationToken);

        static async IAsyncEnumerable<TResult> CoreOrdered(
            IAsyncEnumerable<TSource> source,
            int degreeOfParallelism,
            Func<TSource, CancellationToken, ValueTask<TResult>> selector,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var capacity = degreeOfParallelism * 2;
            var input = Channel.CreateBounded<(int Index, TSource Item)>(new BoundedChannelOptions(capacity)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });

            var output = Channel.CreateBounded<(int Index, TResult Result)>(new BoundedChannelOptions(capacity)
            {
                SingleWriter = false,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });

            Exception? failure = null;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = cts.Token;

            void RecordFailure(Exception ex)
            {
                if (ex is OperationCanceledException)
                    return;

                if (Interlocked.CompareExchange(ref failure, ex, null) is null)
                    cts.Cancel();
            }

            var producer = Task.Run(
                async () =>
                {
                    try
                    {
                        var index = 0;
                        await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                            await input.Writer.WriteAsync((index++, item), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                    }
                    catch (Exception ex)
                    {
                        RecordFailure(ex);
                    }
                    finally
                    {
                        input.Writer.TryComplete(failure);
                    }
                },
                token);

            var workers = new Task[degreeOfParallelism];
            for (var w = 0; w < degreeOfParallelism; w++)
            {
                workers[w] = Task.Run(
                    async () =>
                    {
                        try
                        {
                            while (await input.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                            {
                                while (input.Reader.TryRead(out var entry))
                                {
                                    var result = await selector(entry.Item, token).ConfigureAwait(false);
                                    await output.Writer.WriteAsync((entry.Index, result), token).ConfigureAwait(false);
                                }
                            }
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                        }
                        catch (Exception ex)
                        {
                            RecordFailure(ex);
                        }
                    },
                    token);
            }

            var completion = Task.Run(
                async () =>
                {
                    try
                    {
                        await producer.ConfigureAwait(false);
                        await Task.WhenAll(workers).ConfigureAwait(false);
                    }
                    finally
                    {
                        output.Writer.TryComplete(failure);
                    }
                },
                CancellationToken.None);

            var pending = new SortedDictionary<int, TResult>();
            var nextExpected = 0;

            // As in Core(...): the failure rethrow and the gap check below run only after a full drain.
            try
            {
                while (await output.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (output.Reader.TryRead(out var entry))
                    {
                        pending[entry.Index] = entry.Result;

                        while (pending.TryGetValue(nextExpected, out var next))
                        {
                            pending.Remove(nextExpected);
                            yield return next;
                            nextExpected++;
                        }
                    }
                }
            }
            finally
            {
#pragma warning disable CA1849 // CancelAsync is unavailable on netstandard2.0
                cts.Cancel();
#pragma warning restore CA1849
                try
                {
                    await completion.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();

            if (pending.Count > 0)
                throw new InvalidOperationException("Ordered parallel sequence completed with a gap in the result stream.");
        }
    }
}
#endif
