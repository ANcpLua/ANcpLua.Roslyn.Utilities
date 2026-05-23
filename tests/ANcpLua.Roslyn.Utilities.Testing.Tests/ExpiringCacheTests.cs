using System.Threading;
using System.Threading.Tasks;
using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class ExpiringCacheTests
{
    [Fact]
    public async Task GetOrAdd_ExpiredValueIsNotReturnedAndIsRecomputed()
    {
        var cache = new ExpiringCache<int, int>(idleTimeout: TimeSpan.FromMilliseconds(10));

        var first = cache.GetOrAdd(1, () => 1);
        await Task.Delay(25, TestContext.Current.CancellationToken);
        var second = cache.GetOrAdd(1, () => 2);

        first.Should().Be(1);
        second.Should().Be(2);
        cache.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetOrAdd_SingleFlightPerKeyComputesValueOnceUnderConcurrentMisses()
    {
        var cache = new ExpiringCache<int, int>(maxEntries: 3);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;

        int Factory()
        {
            if (Interlocked.Increment(ref calls) == 1)
                startGate.SetResult();

            gate.Task.Wait();
            return 1;
        }

        var tasks = new Task<int>[16];

        for (var i = 0; i < tasks.Length; i++)
            tasks[i] = Task.Run(() => cache.GetOrAdd(42, Factory));

        await startGate.Task;
        gate.SetResult();
        var values = await Task.WhenAll(tasks);

        values.Should().AllSatisfy(v => v.Should().Be(1));
        calls.Should().Be(1);
    }

    [Fact]
    public void GetOrAdd_EvictionUsesAccessOrderLru()
    {
        var cache = new ExpiringCache<int, int>(maxEntries: 2, idleTimeout: TimeSpan.FromMinutes(60));

        cache.GetOrAdd(1, () => 1);
        cache.GetOrAdd(2, () => 2);
        cache.GetOrAdd(1, () => 10).Should().Be(1);
        cache.GetOrAdd(3, () => 3).Should().Be(3);

        cache.GetOrAdd(2, () => 4).Should().Be(4);
        cache.Count.Should().Be(2);
    }
}
