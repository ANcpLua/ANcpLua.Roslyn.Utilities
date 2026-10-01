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

    // B misses the cache, then A runs a whole miss (create, publish, release) before B registers as the
    // in-flight owner. B must take A's published value instead of running the factory a second time.
    // The key comparer is the only user code on that path, so it parks B exactly there.
    [Fact]
    public async Task GetOrAdd_CallerThatMissedBeforeAPublishReusesThePublishedValue()
    {
        var comparer = new ParkingComparer();
        var cache = new ExpiringCache<string, object>(keyComparer: comparer);
        cache.GetOrAdd("warm-up", static () => new object());
        var factoryRuns = 0;

        object Factory()
        {
            Interlocked.Increment(ref factoryRuns);
            return new object();
        }

        var late = Task.Run(() =>
        {
            comparer.ParkThisThreadOnSecondHash();
            return cache.GetOrAdd("k", Factory);
        }, TestContext.Current.CancellationToken);

        await comparer.Parked.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var published = cache.GetOrAdd("k", Factory);
        comparer.Release();

        (await late.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Should().BeSameAs(published);
        comparer.ParkedOutsideTheLock.Should().BeTrue();
        factoryRuns.Should().Be(1);
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

    // Parks one thread on its second key hash: after the warm-up the first is the cache lookup under the
    // lock, the second is the in-flight registration outside it.
    private sealed class ParkingComparer : IEqualityComparer<string>
    {
        private readonly TaskCompletionSource _parked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _parkingThread = -1;
        private int _hashes;

        public Task Parked => _parked.Task;

        // False when the park sat under the cache lock: the other caller could not finish, so no release came.
        public bool ParkedOutsideTheLock { get; private set; }

        public void ParkThisThreadOnSecondHash() => _parkingThread = Environment.CurrentManagedThreadId;

        public void Release() => _released.TrySetResult();

        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);

        public int GetHashCode(string obj)
        {
            if (Environment.CurrentManagedThreadId == _parkingThread && ++_hashes == 2)
            {
                _parked.TrySetResult();
                ParkedOutsideTheLock = _released.Task.Wait(TimeSpan.FromSeconds(5));
            }

            return StringComparer.Ordinal.GetHashCode(obj);
        }
    }
}
