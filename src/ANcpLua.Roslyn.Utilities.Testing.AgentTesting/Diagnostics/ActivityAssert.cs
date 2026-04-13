// Licensed to the .NET Foundation under one or more agreements.

using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Diagnostics;

/// <summary>
///     Fluent assertion extensions for <see cref="Activity" />. Every assertion returns the
///     activity so multiple checks chain naturally:
///     <code>
///     span.AssertKind(ActivityKind.Client)
///         .AssertTag("gen_ai.provider.name", "openai")
///         .AssertStatus(ActivityStatusCode.Ok);
///     </code>
/// </summary>
public static class ActivityAssert
{
    /// <summary>Asserts that the activity has a tag with the expected value.</summary>
    public static Activity AssertTag(this Activity activity, string key, object? expected)
    {
        var actual = activity.GetTagItem(key);
        Assert.True(
            Equals(actual, expected),
            string.Format(
                CultureInfo.InvariantCulture,
                "Activity '{0}': expected tag '{1}' = '{2}', got '{3}'.",
                activity.OperationName, key, expected, actual));
        return activity;
    }

    /// <summary>Asserts that the activity has a tag with any non-null value.</summary>
    public static Activity AssertHasTag(this Activity activity, string key)
    {
        var actual = activity.GetTagItem(key);
        Assert.True(
            actual is not null,
            string.Format(
                CultureInfo.InvariantCulture,
                "Activity '{0}': expected tag '{1}' to be present, but it was not.",
                activity.OperationName, key));
        return activity;
    }

    /// <summary>Asserts that the activity does NOT have a tag with the given key.</summary>
    public static Activity AssertNoTag(this Activity activity, string key)
    {
        var actual = activity.GetTagItem(key);
        Assert.True(
            actual is null,
            string.Format(
                CultureInfo.InvariantCulture,
                "Activity '{0}': expected tag '{1}' to be absent, but found '{2}'.",
                activity.OperationName, key, actual));
        return activity;
    }

    /// <summary>Asserts the activity status code.</summary>
    public static Activity AssertStatus(this Activity activity, ActivityStatusCode expected)
    {
        Assert.True(
            activity.Status == expected,
            string.Format(
                CultureInfo.InvariantCulture,
                "Activity '{0}': expected status '{1}', got '{2}'.",
                activity.OperationName, expected, activity.Status));
        return activity;
    }

    /// <summary>Asserts the activity has an event with the given name.</summary>
    public static Activity AssertHasEvent(this Activity activity, string eventName)
    {
        var hasEvent = activity.Events.Any(e => string.Equals(e.Name, eventName, StringComparison.Ordinal));
        Assert.True(
            hasEvent,
            string.Format(
                CultureInfo.InvariantCulture,
                "Activity '{0}': expected event '{1}', found: [{2}].",
                activity.OperationName,
                eventName,
                string.Join(", ", activity.Events.Select(static e => e.Name))));
        return activity;
    }

    /// <summary>Asserts the activity kind.</summary>
    public static Activity AssertKind(this Activity activity, ActivityKind expected)
    {
        Assert.True(
            activity.Kind == expected,
            string.Format(
                CultureInfo.InvariantCulture,
                "Activity '{0}': expected kind '{1}', got '{2}'.",
                activity.OperationName, expected, activity.Kind));
        return activity;
    }

    /// <summary>Asserts the activity duration is within the given closed range.</summary>
    public static Activity AssertDuration(this Activity activity, TimeSpan min, TimeSpan max)
    {
        Assert.True(
            activity.Duration >= min && activity.Duration <= max,
            string.Format(
                CultureInfo.InvariantCulture,
                "Activity '{0}': duration {1} not in [{2}, {3}].",
                activity.OperationName, activity.Duration, min, max));
        return activity;
    }
}
