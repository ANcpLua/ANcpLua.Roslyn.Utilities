#pragma warning disable MA0004, MA0006, MA0007, MA0016, MA0041, MA0048, MA0076
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
///     Captures completed <see cref="Activity"/> instances from named
///     <see cref="ActivitySource"/>s for test assertions.
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
    private readonly ActivityListener _listener;
    private readonly ConcurrentBag<Activity> _activities = [];
    private readonly HashSet<string>? _sourceFilter;

    /// <summary>
    ///     Creates a collector that captures activities from the specified source names.
    ///     If no names are provided, captures from all sources.
    /// </summary>
    /// <param name="sourceNames">
    ///     Optional source names to filter on. Pass none to capture all sources.
    /// </param>
    public ActivityCollector(params string[] sourceNames)
    {
        _sourceFilter = sourceNames.Length > 0 ? [..sourceNames] : null;

        _listener = new ActivityListener
        {
            ShouldListenTo = ShouldListen,
            Sample = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _activities.Add(activity)
        };

        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>All captured activities.</summary>
    public IReadOnlyList<Activity> Activities => [.. _activities];

    /// <summary>Returns the single activity matching the operation name prefix.</summary>
    /// <param name="operationNamePrefix">Prefix to match against <see cref="Activity.OperationName"/>.</param>
    public Activity FindSingle(string operationNamePrefix)
    {
        var matches = _activities
            .Where(a => a.OperationName.StartsWith(operationNamePrefix, StringComparison.Ordinal))
            .ToList();

        Assert.True(matches.Count is 1,
            $"Expected exactly 1 activity matching '{operationNamePrefix}', found {matches.Count}. " +
            $"All activities: [{string.Join(", ", _activities.Select(static a => a.OperationName))}]");

        return matches[0];
    }

    /// <summary>Returns all activities matching the operation name prefix.</summary>
    public IReadOnlyList<Activity> Where(string operationNamePrefix)
    {
        return _activities
            .Where(a => a.OperationName.StartsWith(operationNamePrefix, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>Asserts that no activities were captured.</summary>
    public ActivityCollector ShouldBeEmpty()
    {
        Assert.True(_activities.IsEmpty,
            $"Expected no activities, found {_activities.Count}: " +
            $"[{string.Join(", ", _activities.Select(static a => a.OperationName))}]");
        return this;
    }

    /// <summary>Asserts that at least <paramref name="expected"/> activities were captured.</summary>
    public ActivityCollector ShouldHaveCount(int expected)
    {
        Assert.True(_activities.Count >= expected,
            $"Expected at least {expected} activities, found {_activities.Count}.");
        return this;
    }

    /// <inheritdoc />
    public void Dispose() => _listener.Dispose();

    private bool ShouldListen(ActivitySource source) =>
        _sourceFilter is null || _sourceFilter.Contains(source.Name);
}

/// <summary>
///     Fluent assertion extensions for <see cref="Activity"/>.
/// </summary>
public static class ActivityAssert
{
    /// <summary>Asserts that the activity has a tag with the expected value.</summary>
    public static Activity AssertTag(this Activity activity, string key, object? expected)
    {
        var actual = activity.GetTagItem(key);
        Assert.True(Equals(actual, expected),
            $"Activity '{activity.OperationName}': expected tag '{key}' = '{expected}', got '{actual}'.");
        return activity;
    }

    /// <summary>Asserts that the activity has a tag (any value).</summary>
    public static Activity AssertHasTag(this Activity activity, string key)
    {
        var actual = activity.GetTagItem(key);
        Assert.True(actual is not null,
            $"Activity '{activity.OperationName}': expected tag '{key}' to be present, but it was not.");
        return activity;
    }

    /// <summary>Asserts that the activity does NOT have a tag.</summary>
    public static Activity AssertNoTag(this Activity activity, string key)
    {
        var actual = activity.GetTagItem(key);
        Assert.True(actual is null,
            $"Activity '{activity.OperationName}': expected tag '{key}' to be absent, but found '{actual}'.");
        return activity;
    }

    /// <summary>Asserts the activity status code.</summary>
    public static Activity AssertStatus(this Activity activity, ActivityStatusCode expected)
    {
        Assert.True(activity.Status == expected,
            $"Activity '{activity.OperationName}': expected status '{expected}', got '{activity.Status}'.");
        return activity;
    }

    /// <summary>Asserts the activity has an event with the given name.</summary>
    public static Activity AssertHasEvent(this Activity activity, string eventName)
    {
        Assert.True(activity.Events.Any(e => e.Name == eventName),
            $"Activity '{activity.OperationName}': expected event '{eventName}', " +
            $"found: [{string.Join(", ", activity.Events.Select(static e => e.Name))}].");
        return activity;
    }

    /// <summary>Asserts the activity kind.</summary>
    public static Activity AssertKind(this Activity activity, ActivityKind expected)
    {
        Assert.True(activity.Kind == expected,
            $"Activity '{activity.OperationName}': expected kind '{expected}', got '{activity.Kind}'.");
        return activity;
    }

    /// <summary>Asserts the activity duration is within a range.</summary>
    public static Activity AssertDuration(this Activity activity, TimeSpan min, TimeSpan max)
    {
        Assert.True(activity.Duration >= min && activity.Duration <= max,
            $"Activity '{activity.OperationName}': duration {activity.Duration} not in [{min}, {max}].");
        return activity;
    }
}
