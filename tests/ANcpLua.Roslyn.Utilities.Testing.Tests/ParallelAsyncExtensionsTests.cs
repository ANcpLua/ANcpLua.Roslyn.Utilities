using System.Runtime.CompilerServices;
using ANcpLua.Roslyn.Utilities.Async;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class ParallelAsyncExtensionsTests
{
    [Fact]
    public async Task SelectParallelOrdered_YieldsResultsInSourceOrder_WhenWorkCompletesOutOfOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var results = await WithTimeout(CollectAsync(Range(16, ct).SelectParallelOrdered(
            4,
            async (item, ct) =>
            {
                await Task.Delay((16 - item) * 2, ct);
                return item;
            },
            ct), ct));

        results.Should().Equal(Enumerable.Range(0, 16));
    }

    [Fact]
    public async Task SelectParallel_RethrowsSelectorExceptionWithoutHanging()
    {
        var ct = TestContext.Current.CancellationToken;
        var act = () => WithTimeout(CollectAsync(Range(32, ct).SelectParallel(
            4,
            async (item, ct) =>
            {
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
                if (item == 7)
                    throw new InvalidOperationException("parallel boom");

                return item;
            },
            ct), ct));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*parallel boom*");
    }

    [Fact]
    public async Task SelectParallel_DisposeStopsProducerAndWorkers_WhenConsumerStopsEarly()
    {
        var ct = TestContext.Current.CancellationToken;
        var enumerator = Endless(ct).SelectParallel(
                4,
                async (item, ct) =>
                {
                    await Task.Delay(5, ct);
                    return item;
                },
                ct)
            .GetAsyncEnumerator(ct);

        try
        {
            var moved = await WithTimeout(enumerator.MoveNextAsync().AsTask());
            moved.Should().BeTrue();
        }
        finally
        {
            await WithTimeout(enumerator.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task SelectParallelOrdered_DisposeDoesNotTreatPendingResultsAsCorruption()
    {
        var ct = TestContext.Current.CancellationToken;
        var enumerator = Range(32, ct).SelectParallelOrdered(
                4,
                async (item, ct) =>
                {
                    await Task.Delay((32 - item) * 2, ct);
                    return item;
                },
                ct)
            .GetAsyncEnumerator(ct);

        try
        {
            var moved = await WithTimeout(enumerator.MoveNextAsync().AsTask());
            moved.Should().BeTrue();
            enumerator.Current.Should().Be(0);
        }
        finally
        {
            await WithTimeout(enumerator.DisposeAsync().AsTask());
        }
    }

    private static async Task<List<T>> CollectAsync<T>(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var result = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
            result.Add(item);
        return result;
    }

    private static async Task<T> WithTimeout<T>(Task<T> task)
    {
        return await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    private static async Task WithTimeout(Task task)
    {
        await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    private static async IAsyncEnumerable<int> Range(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<int> Endless(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var i = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i++;
            await Task.Delay(1, cancellationToken);
        }
    }
}
