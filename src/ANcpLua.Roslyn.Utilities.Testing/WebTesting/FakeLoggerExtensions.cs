namespace ANcpLua.Roslyn.Utilities.Testing.WebTesting;

using System.Text;
using Microsoft.Extensions.Logging.Testing;

/// <summary>
/// Extension methods for <see cref="FakeLogCollector"/> to simplify log assertions in tests.
/// </summary>
public static class FakeLoggerExtensions
{
    /// <summary>
    /// Retrieves all collected log entries as a single formatted string.
    /// </summary>
    /// <param name="source">The <see cref="FakeLogCollector"/> containing the log records.</param>
    /// <param name="formatter">
    /// An optional function to format each <see cref="FakeLogRecord"/>.
    /// If null, defaults to "{Level} - {Message}" format.
    /// </param>
    /// <returns>A string containing all log entries, each on a separate line.</returns>
    public static string GetFullLoggerText(
        this FakeLogCollector source,
        Func<FakeLogRecord, string>? formatter = null)
    {
        var sb = new StringBuilder();
        var snapshot = source.GetSnapshot();
        formatter ??= record => $"{record.Level} - {record.Message}";

        foreach (var record in snapshot)
            sb.AppendLine(formatter(record));

        return sb.ToString();
    }

    /// <summary>
    /// Asynchronously waits for a log condition to be satisfied within a specified timeout.
    /// </summary>
    /// <param name="source">The <see cref="FakeLogCollector"/> to monitor.</param>
    /// <param name="condition">A predicate that returns true when the expected condition is met.</param>
    /// <param name="timeout">The maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="pollInterval">The interval between checks. Defaults to 25ms.</param>
    /// <param name="cancellationToken">A token to cancel the wait operation.</param>
    /// <returns>True if the condition was satisfied within the timeout; otherwise, false.</returns>
    public static async Task<bool> WaitForLogAsync(
        this FakeLogCollector source,
        Func<IReadOnlyList<FakeLogRecord>, bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        pollInterval ??= TimeSpan.FromMilliseconds(25);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout.Value);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (condition(source.GetSnapshot())) return true;
                await Task.Delay(pollInterval.Value, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }

        return condition(source.GetSnapshot());
    }

    /// <summary>
    /// Asynchronously waits until a specified number of log entries matching a predicate have been collected.
    /// </summary>
    public static Task<bool> WaitForLogCountAsync(
        this FakeLogCollector source,
        Func<FakeLogRecord, bool> predicate,
        int expectedCount,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        return source.WaitForLogAsync(
            logs => logs.Count(predicate) >= expectedCount,
            timeout,
            cancellationToken: cancellationToken);
    }
}
