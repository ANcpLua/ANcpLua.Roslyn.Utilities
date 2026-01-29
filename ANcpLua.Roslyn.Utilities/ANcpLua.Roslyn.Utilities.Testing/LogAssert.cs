using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides fluent assertion extension methods for <see cref="FakeLogCollector" />.
/// </summary>
/// <remarks>
///     <para>
///         This class enables expressive, chainable assertions for validating log output
///         in tests. All assertion methods return the original <see cref="FakeLogCollector" />
///         to support fluent chaining.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Log count validation</description>
///         </item>
///         <item>
///             <description>Content and pattern matching</description>
///         </item>
///         <item>
///             <description>Log level filtering</description>
///         </item>
///         <item>
///             <description>Async polling for eventual conditions</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <para>
///         Basic log assertions:
///     </para>
///     <code>
/// collector
///     .ShouldHaveCount(3)
///     .ShouldContain("started")
///     .ShouldHaveNoErrors();
/// </code>
///     <para>
///         Async waiting for log entries:
///     </para>
///     <code>
/// await collector.ShouldEventuallyContain("completed");
/// </code>
/// </example>
/// <seealso cref="FakeLogCollector" />
/// <seealso cref="FakeLogRecord" />
public static class LogAssert
{
    /// <summary>
    ///     Formats log records for display in assertion messages.
    /// </summary>
    /// <param name="collector">The log collector to format.</param>
    /// <returns>A formatted string containing all log records.</returns>
    public static string FormatLogs(this FakeLogCollector collector)
    {
        var logs = collector.GetSnapshot();
        if (logs.Count is 0)
            return "(empty)";

        var sb = new StringBuilder();
        foreach (var r in logs)
            sb.AppendLine($"  {r.Level}: {r.Message}");
        return sb.ToString();
    }

    // =========================================================================
    // Count Assertions
    // =========================================================================

    /// <summary>
    ///     Asserts that the collector has at least N log entries.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="expected">The minimum number of entries expected.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldHaveCount(this FakeLogCollector collector, int expected)
    {
        var logs = collector.GetSnapshot();
        Assert.True(logs.Count >= expected,
            $"Expected at least {expected} log entries, got {logs.Count}.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    /// <summary>
    ///     Asserts that the collector has exactly N log entries.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="expected">The exact number of entries expected.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldHaveExactCount(this FakeLogCollector collector, int expected)
    {
        var logs = collector.GetSnapshot();
        Assert.True(logs.Count == expected,
            $"Expected exactly {expected} log entries, got {logs.Count}.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    /// <summary>
    ///     Asserts that the collector has no log entries.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldBeEmpty(this FakeLogCollector collector)
    {
        var logs = collector.GetSnapshot();
        Assert.True(logs.Count is 0,
            $"Expected no log entries, got {logs.Count}.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    // =========================================================================
    // Content Assertions
    // =========================================================================

    /// <summary>
    ///     Asserts that at least one log contains the specified text.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="text">The text to search for.</param>
    /// <param name="comparison">The string comparison type. Defaults to <see cref="StringComparison.Ordinal" />.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldContain(
        this FakeLogCollector collector,
        string text,
        StringComparison comparison = StringComparison.Ordinal)
    {
        var logs = collector.GetSnapshot();
        Assert.True(logs.Any(r => r.Message.Contains(text, comparison)),
            $"No log message contains '{text}'.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    /// <summary>
    ///     Asserts that no log contains the specified text.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="text">The text that should not be present.</param>
    /// <param name="comparison">The string comparison type. Defaults to <see cref="StringComparison.Ordinal" />.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldNotContain(
        this FakeLogCollector collector,
        string text,
        StringComparison comparison = StringComparison.Ordinal)
    {
        var logs = collector.GetSnapshot();
        Assert.True(logs.All(r => !r.Message.Contains(text, comparison)),
            $"Log should not contain '{text}' but does.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    /// <summary>
    ///     Asserts that at least one log matches the specified regex pattern.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="pattern">The regex pattern to match.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldMatch(this FakeLogCollector collector, string pattern)
    {
        var regex = new Regex(pattern);
        var logs = collector.GetSnapshot();
        Assert.True(logs.Any(r => regex.IsMatch(r.Message)),
            $"No log message matches pattern '{pattern}'.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    // =========================================================================
    // Level Assertions
    // =========================================================================

    /// <summary>
    ///     Asserts that at least one log has the specified level.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="level">The log level to check for.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldHaveLevel(this FakeLogCollector collector, LogLevel level)
    {
        var logs = collector.GetSnapshot();
        Assert.True(logs.Any(r => r.Level == level),
            $"No log entry with level {level}.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    /// <summary>
    ///     Asserts that no log has the specified level.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="level">The log level that should not be present.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldNotHaveLevel(this FakeLogCollector collector, LogLevel level)
    {
        var logs = collector.GetSnapshot();
        Assert.True(logs.All(r => r.Level != level),
            $"Log should not contain level {level} but does.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    /// <summary>
    ///     Asserts that no errors were logged.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldHaveNoErrors(this FakeLogCollector collector) =>
        collector.ShouldNotHaveLevel(LogLevel.Error);

    /// <summary>
    ///     Asserts that no warnings were logged.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldHaveNoWarnings(this FakeLogCollector collector) =>
        collector.ShouldNotHaveLevel(LogLevel.Warning);

    /// <summary>
    ///     Asserts that no errors or warnings were logged.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldBeClean(this FakeLogCollector collector) =>
        collector.ShouldHaveNoErrors().ShouldHaveNoWarnings();

    // =========================================================================
    // Combined Assertions
    // =========================================================================

    /// <summary>
    ///     Asserts that a log exists with the specified level and containing the text.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="level">The log level to check for.</param>
    /// <param name="containsText">The text that should be present in the message.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldHave(
        this FakeLogCollector collector,
        LogLevel level,
        string containsText)
    {
        var logs = collector.GetSnapshot();
        Assert.True(logs.Any(r => r.Level == level && r.Message.Contains(containsText)),
            $"No {level} log containing '{containsText}'.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    // =========================================================================
    // Predicate Assertions
    // =========================================================================

    /// <summary>
    ///     Asserts that at least one log matches the predicate.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="because">Optional custom failure message.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldHaveAny(
        this FakeLogCollector collector,
        Func<FakeLogRecord, bool> predicate,
        string? because = null)
    {
        var logs = collector.GetSnapshot();
        Assert.True(logs.Any(predicate),
            because ?? $"No log entry matches the predicate.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    /// <summary>
    ///     Asserts that all logs match the predicate.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="predicate">The predicate that all logs must match.</param>
    /// <param name="because">Optional custom failure message.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldHaveAll(
        this FakeLogCollector collector,
        Func<FakeLogRecord, bool> predicate,
        string? because = null)
    {
        var logs = collector.GetSnapshot();
        Assert.True(logs.All(predicate),
            because ?? $"Not all log entries match the predicate.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    /// <summary>
    ///     Asserts that no log matches the predicate.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="predicate">The predicate that no logs should match.</param>
    /// <param name="because">Optional custom failure message.</param>
    /// <returns>The same <see cref="FakeLogCollector" /> instance for fluent chaining.</returns>
    public static FakeLogCollector ShouldHaveNone(
        this FakeLogCollector collector,
        Func<FakeLogRecord, bool> predicate,
        string? because = null)
    {
        var logs = collector.GetSnapshot();
        Assert.True(!logs.Any(predicate),
            because ?? $"A log entry unexpectedly matched the predicate.\nActual logs:\n{collector.FormatLogs()}");
        return collector;
    }

    // =========================================================================
    // Async Waiting
    // =========================================================================

    /// <summary>
    ///     Waits for a log containing text, throws if timeout.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="text">The text to wait for.</param>
    /// <param name="timeout">The maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the log is found or throws on timeout.</returns>
    public static async Task ShouldEventuallyContain(
        this FakeLogCollector collector,
        string text,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var found = await WaitForCondition(
            collector,
            logs => logs.Any(r => r.Message.Contains(text)),
            timeout,
            ct);

        Assert.True(found,
            $"Timed out waiting for log containing '{text}'.\nActual logs:\n{collector.FormatLogs()}");
    }

    /// <summary>
    ///     Waits for at least N logs, throws if timeout.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="count">The minimum number of logs to wait for.</param>
    /// <param name="timeout">The maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the count is reached or throws on timeout.</returns>
    public static async Task ShouldEventuallyHaveCount(
        this FakeLogCollector collector,
        int count,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var found = await WaitForCondition(
            collector,
            logs => logs.Count >= count,
            timeout,
            ct);

        Assert.True(found,
            $"Timed out waiting for {count} logs, got {collector.GetSnapshot().Count}.\nActual logs:\n{collector.FormatLogs()}");
    }

    /// <summary>
    ///     Waits for a log with the specified level, throws if timeout.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="level">The log level to wait for.</param>
    /// <param name="timeout">The maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when a log with the level is found or throws on timeout.</returns>
    public static async Task ShouldEventuallyHaveLevel(
        this FakeLogCollector collector,
        LogLevel level,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var found = await WaitForCondition(
            collector,
            logs => logs.Any(r => r.Level == level),
            timeout,
            ct);

        Assert.True(found,
            $"Timed out waiting for {level} log.\nActual logs:\n{collector.FormatLogs()}");
    }

    /// <summary>
    ///     Waits for a condition to be satisfied.
    /// </summary>
    /// <param name="collector">The log collector to validate.</param>
    /// <param name="condition">The condition to wait for.</param>
    /// <param name="because">Optional custom failure message.</param>
    /// <param name="timeout">The maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the condition is satisfied or throws on timeout.</returns>
    public static async Task ShouldEventuallySatisfy(
        this FakeLogCollector collector,
        Func<IReadOnlyList<FakeLogRecord>, bool> condition,
        string? because = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var found = await WaitForCondition(collector, condition, timeout, ct);

        Assert.True(found,
            because ?? $"Timed out waiting for condition.\nActual logs:\n{collector.FormatLogs()}");
    }

    private static async Task<bool> WaitForCondition(
        FakeLogCollector collector,
        Func<IReadOnlyList<FakeLogRecord>, bool> condition,
        TimeSpan? timeout,
        CancellationToken ct)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        var pollInterval = TimeSpan.FromMilliseconds(25);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout.Value);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (condition(collector.GetSnapshot()))
                    return true;
                await Task.Delay(pollInterval, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timeout - check one more time
        }

        return condition(collector.GetSnapshot());
    }
}