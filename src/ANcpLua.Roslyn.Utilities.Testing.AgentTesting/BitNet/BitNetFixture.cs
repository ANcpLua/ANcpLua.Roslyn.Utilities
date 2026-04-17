using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.BitNet;

/// <summary>
///     Shared fixture that probes a BitNet (llama.cpp) server and exposes an <see cref="IChatClient" />.
///     Tests should guard with <c>Skip.IfNot(bitnet.IsAvailable, "BitNet not running")</c>.
/// </summary>
/// <remarks>
///     <para>Configuration:</para>
///     <list type="bullet">
///         <item><c>BITNET_URL</c> env var overrides the default <c>http://localhost:8080</c> endpoint.</item>
///         <item>The fixture probes <c>/health</c> with a 3-second timeout during <see cref="InitializeAsync" />.</item>
///     </list>
/// </remarks>
public sealed class BitNetFixture : IAsyncLifetime
{
    private static readonly Uri DefaultEndpoint = new("http://localhost:8080");

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <summary>
    ///     Chat client connected to the BitNet server. Only usable when <see cref="IsAvailable" /> is
    ///     <see langword="true" />.
    /// </summary>
    public IChatClient ChatClient { get; private set; } = null!;

    /// <summary>
    ///     Whether the BitNet server responded to the health probe during initialization.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("BITNET_URL") is { Length: > 0 } url
            ? new Uri(url)
            : DefaultEndpoint;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var response = await _http.GetAsync(new Uri(endpoint, "/health"), cts.Token)
                .ConfigureAwait(false);
            IsAvailable = response.IsSuccessStatusCode;
        }
        catch
        {
            IsAvailable = false;
        }

        if (!IsAvailable) return;

        var options = new OpenAIClientOptions { Endpoint = endpoint };
        var client = new OpenAIClient(new ApiKeyCredential("unused"), options);
        ChatClient = (IChatClient)client.GetChatClient("bitnet-b1.58-2B-4T");
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        ChatClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
