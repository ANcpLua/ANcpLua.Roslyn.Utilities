// Licensed to the .NET Foundation under one or more agreements.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Diagnostics;

/// <summary>
///     Captures completed <see cref="Activity" /> instances from named
///     <see cref="ActivitySource" />s for test assertions.
/// </summary>
/// <remarks>
///     <para>Usage:</para>
///     <code>
///     using var collector = new ActivityCollector("Qyl.Agents");
///
///     // ... exercise code that creates activities ...
///
///     var span = collector.FindSingle("chat gpt-4");
///     span.AssertTag("gen_ai.provider.name", "openai");
///     span.AssertStatus(ActivityStatusCode.Ok);
///     </code>
/// </remarks>
public sealed class ActivityCollector : IDisposable
{
    private readonly ConcurrentBag<Activity> _activities = [];
    private readonly ActivityListener _listener;
    private readonly HashSet<string>? _sourceFilter;

    /// <summary>
    ///     Creates a collector that captures activities from the specified source names.
    ///     If no names are provided, captures from all sources.
    /// </summary>
    /// <param name="sourceNames">
    ///     Source names to filter on. Pass none to capture from every registered source.
    /// </param>
    public ActivityCollector(params string[] sourceNames)
    {
        _sourceFilter = sourceNames.Length > 0 ? [.. sourceNames] : null;

        _listener = new ActivityListener
        {
            ShouldListenTo = ShouldListen,
            Sample = SampleAlways,
            ActivityStopped = _activities.Add,
        };

        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>Snapshot of all captured activities, in the order they were stopped.</summary>
    public IReadOnlyList<Activity> Activities => [.. _activities];

    /// <inheritdoc />
    public void Dispose() => _listener.Dispose();

    /// <summary>Returns the single activity whose operation name starts with the given prefix.</summary>
    /// <param name="operationNamePrefix">Prefix to match against <see cref="Activity.OperationName" />.</param>
    public Activity FindSingle(string operationNamePrefix)
    {
        var matches = _activities
            .Where(a => a.OperationName.StartsWith(operationNamePrefix, StringComparison.Ordinal))
            .ToList();

        Assert.True(
            matches.Count is 1,
            string.Format(
                CultureInfo.InvariantCulture,
                "Expected exactly 1 activity matching '{0}', found {1}. All activities: [{2}]",
                operationNamePrefix,
                matches.Count,
                string.Join(", ", _activities.Select(static a => a.OperationName))));

        return matches[0];
    }

    /// <summary>Returns all activities whose operation name starts with the given prefix.</summary>
    public IReadOnlyList<Activity> Where(string operationNamePrefix) =>
        [.. _activities.Where(a => a.OperationName.StartsWith(operationNamePrefix, StringComparison.Ordinal))];

    /// <summary>Asserts that no activities were captured. Returns self for fluent chaining.</summary>
    public ActivityCollector ShouldBeEmpty()
    {
        Assert.True(
            _activities.IsEmpty,
            string.Format(
                CultureInfo.InvariantCulture,
                "Expected no activities, found {0}: [{1}]",
                _activities.Count,
                string.Join(", ", _activities.Select(static a => a.OperationName))));
        return this;
    }

    /// <summary>Asserts that at least <paramref name="expected" /> activities were captured.</summary>
    public ActivityCollector ShouldHaveCount(int expected)
    {
        Assert.True(
            _activities.Count >= expected,
            string.Format(
                CultureInfo.InvariantCulture,
                "Expected at least {0} activities, found {1}.",
                expected,
                _activities.Count));
        return this;
    }

    private bool ShouldListen(ActivitySource source) =>
        _sourceFilter is null || _sourceFilter.Contains(source.Name);

    private static ActivitySamplingResult SampleAlways(ref ActivityCreationOptions<ActivityContext> options) =>
        ActivitySamplingResult.AllDataAndRecorded;
}
