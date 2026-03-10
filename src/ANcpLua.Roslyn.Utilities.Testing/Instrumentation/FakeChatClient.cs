using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.Instrumentation;

/// <summary>
///     Configurable <see cref="IChatClient"/> test double that returns canned responses
///     and records all calls for assertion. Supports both non-streaming and streaming paths.
/// </summary>
/// <remarks>
///     <para>Usage:</para>
///     <code>
///     using var client = new FakeChatClient()
///         .WithResponse("Hello!")
///         .WithStreamingResponse("Hel", "lo!");
///
///     var response = await client.GetResponseAsync([new(ChatRole.User, "Hi")]);
///     Assert.Equal("Hello!", response.Text);
///     Assert.Single(client.Calls);
///     </code>
/// </remarks>
public sealed class FakeChatClient : IChatClient
{
    private readonly Queue<object> _responses = new(); // ChatResponse | ChatResponseUpdate[] | Exception
    private readonly Lock _lock = new();

    /// <summary>
    ///     All calls made to <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/>,
    ///     in order. Each entry records the messages and options passed by the caller.
    /// </summary>
    public List<ChatClientCall> Calls { get; } = [];

    /// <summary>
    ///     Metadata returned by <see cref="GetService"/> when the requested type
    ///     is <see cref="ChatClientMetadata"/>. Defaults to provider "fake", model "fake-model".
    /// </summary>
    public ChatClientMetadata Metadata { get; set; } = new("fake", null, "fake-model");

    // ── Builder methods ──────────────────────────────────────────────────────

    /// <summary>
    ///     Enqueues a canned non-streaming response with the given text.
    /// </summary>
    public FakeChatClient WithResponse(
        string text,
        ChatFinishReason? finishReason = null,
        UsageDetails? usage = null,
        string? modelId = null)
    {
        var message = new ChatMessage(ChatRole.Assistant, text);
        var response = new ChatResponse(message)
        {
            FinishReason = finishReason ?? ChatFinishReason.Stop,
            ModelId = modelId,
            Usage = usage
        };

        using (_lock.EnterScope())
            _responses.Enqueue(response);

        return this;
    }

    /// <summary>
    ///     Enqueues a canned streaming response that yields one update per chunk.
    /// </summary>
    public FakeChatClient WithStreamingResponse(params string[] chunks)
    {
        var updates = chunks
            .Select(static chunk => new ChatResponseUpdate(ChatRole.Assistant, chunk))
            .ToArray();

        using (_lock.EnterScope())
            _responses.Enqueue(updates);

        return this;
    }

    /// <summary>
    ///     Enqueues an exception to be thrown on the next call (streaming or non-streaming).
    /// </summary>
    public FakeChatClient WithError(Exception exception)
    {
        using (_lock.EnterScope())
            _responses.Enqueue(exception);

        return this;
    }

    /// <summary>
    ///     Enqueues a typed exception to be thrown on the next call.
    /// </summary>
    public FakeChatClient WithError<TException>() where TException : Exception, new()
    {
        return WithError(new TException());
    }

    // ── IChatClient ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        RecordCall(messages, options);

        var next = DequeueNext();

        return next switch
        {
            Exception ex => Task.FromException<ChatResponse>(ex),
            ChatResponse response => Task.FromResult(response),
            ChatResponseUpdate[] updates => Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, string.Concat(updates.Select(static u => u.Text))))
            {
                FinishReason = ChatFinishReason.Stop
            }),
            _ => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty)))
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        RecordCall(messages, options);

        var next = DequeueNext();

        switch (next)
        {
            case Exception ex:
                throw ex;

            case ChatResponseUpdate[] updates:
                foreach (var update in updates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return update;
                }
                break;

            case ChatResponse response:
                yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
                break;

            default:
                yield break;
        }

        // Suppress compiler warning — await is required for async IAsyncEnumerable
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(ChatClientMetadata))
            return Metadata;

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to release.
    }

    // ── Internals ────────────────────────────────────────────────────────────

    private void RecordCall(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        Calls.Add(new ChatClientCall(messages.ToList(), options));
    }

    private object? DequeueNext()
    {
        using (_lock.EnterScope())
            return _responses.Count > 0 ? _responses.Dequeue() : null;
    }
}

/// <summary>
///     Records a single call to <see cref="FakeChatClient"/>.
/// </summary>
/// <param name="Messages">The chat messages passed to the call.</param>
/// <param name="Options">The chat options passed to the call, if any.</param>
public sealed record ChatClientCall(
    IReadOnlyList<ChatMessage> Messages,
    ChatOptions? Options);
