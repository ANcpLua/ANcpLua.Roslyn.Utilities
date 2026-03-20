#if !NETSTANDARD
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ANcpLua.Roslyn.Utilities.Streaming;

/// <summary>
///     A generic Server-Sent Events (SSE) client with automatic reconnection using exponential backoff.
///     Events are surfaced through a <see cref="Channel{T}" />-backed <see cref="IAsyncEnumerable{T}" />.
/// </summary>
/// <remarks>
///     <para>
///         The client does not own the <see cref="HttpClient" /> — callers are responsible for its lifetime.
///         The URL is fully configurable; no endpoints are hardcoded.
///     </para>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class SseClient(HttpClient httpClient)
{
    private const int MaxBackoffSeconds = 30;

    /// <summary>
    ///     Connects to the given SSE endpoint and streams parsed events.
    ///     Automatically reconnects with exponential backoff on transient failures.
    /// </summary>
    /// <param name="url">The fully-qualified URL of the SSE endpoint.</param>
    /// <param name="ct">A token to cancel the stream and stop reconnection.</param>
    /// <returns>An async sequence of <see cref="SseEvent" /> instances.</returns>
    public IAsyncEnumerable<SseEvent> StreamAsync(string url, CancellationToken ct)
    {
        Guard.NotNullOrEmpty(url);

        var channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions { SingleReader = true });

        _ = ProduceWithReconnectAsync(url, channel.Writer, ct);

        return channel.Reader.ReadAllAsync(ct);
    }

    private async Task ProduceWithReconnectAsync(string url, ChannelWriter<SseEvent> writer, CancellationToken ct)
    {
        var backoff = 1;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await ProduceEventsAsync(url, writer, ct).ConfigureAwait(false);
                    backoff = 1;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch
                {
                    // Connection lost — will retry after backoff
                }

                if (ct.IsCancellationRequested)
                    return;

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(backoff), ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                backoff = Math.Min(backoff * 2, MaxBackoffSeconds);
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task ProduceEventsAsync(string url, ChannelWriter<SseEvent> writer, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? eventType = null;
        var dataLines = new List<string>();

        while (!ct.IsCancellationRequested)
        {
            if (await reader.ReadLineAsync(ct).ConfigureAwait(false) is not { } line)
                return;

            if (line.Length is 0)
            {
                if (dataLines.Count > 0)
                {
                    var data = string.Join('\n', dataLines);
                    await writer.WriteAsync(new SseEvent(eventType ?? "message", data), ct).ConfigureAwait(false);
                    eventType = null;
                    dataLines.Clear();
                }

                continue;
            }

            if (line.StartsWithOrdinal("event:"))
            {
                eventType = line[6..].Trim();
            }
            else if (line.StartsWithOrdinal("data:"))
            {
                dataLines.Add(line[5..].TrimStart());
            }
        }
    }
}
#endif
