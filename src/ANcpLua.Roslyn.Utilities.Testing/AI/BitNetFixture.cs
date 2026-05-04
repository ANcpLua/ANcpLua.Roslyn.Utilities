using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AI;

/// <summary>
///     Shared xUnit v3 fixture that probes a BitNet (llama.cpp) server and exposes an <see cref="IChatClient" />.
///     Guard tests with <c>Skip.IfNot(fixture.IsAvailable, "BitNet server not running")</c>.
/// </summary>
/// <remarks>
///     <c>BITNET_URL</c> overrides the default endpoint. <c>BITNET_MODEL</c> overrides the default model.
///     Health probe retries 3 times with 1-second backoff.
/// </remarks>
public sealed class BitNetFixture : IAsyncLifetime
{
    private static readonly Uri s_defaultEndpoint = new("http://localhost:8080");
    private const string DefaultModel = "bitnet-b1.58-2B-4T";
    private const int MaxRetries = 3;

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <summary>Chat client wired to the BitNet server. Only valid when <see cref="IsAvailable" />.</summary>
    public IChatClient ChatClient { get; private set; } = null!;

    /// <summary>Whether the server responded during initialization.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Resolved endpoint URI.</summary>
    public Uri Endpoint { get; private set; } = s_defaultEndpoint;

    /// <summary>Resolved model name.</summary>
    public string Model { get; private set; } = DefaultModel;

    public async ValueTask InitializeAsync()
    {
        Endpoint = Environment.GetEnvironmentVariable("BITNET_URL") is { Length: > 0 } url
            ? new Uri(url)
            : s_defaultEndpoint;

        Model = Environment.GetEnvironmentVariable("BITNET_MODEL") is { Length: > 0 } model
            ? model
            : DefaultModel;

        IsAvailable = await ProbeWithRetryAsync().ConfigureAwait(false);
        if (!IsAvailable) return;

        var client = new OpenAIClient(
            new ApiKeyCredential("unused"),
            new OpenAIClientOptions { Endpoint = Endpoint });

        ChatClient = (IChatClient)client.GetChatClient(Model);
    }

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        (ChatClient as IDisposable)?.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task<bool> ProbeWithRetryAsync()
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var response = await _http.GetAsync(new Uri(Endpoint, "/health"), cts.Token)
                    .ConfigureAwait(false);
                if (response.IsSuccessStatusCode) return true;
            }
            catch
            {
                // Server not up yet
            }

            if (attempt < MaxRetries - 1)
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        return false;
    }
}
