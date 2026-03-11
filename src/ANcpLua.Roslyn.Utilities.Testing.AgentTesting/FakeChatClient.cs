#pragma warning disable MA0004, MA0006, MA0007, MA0016, MA0041, MA0048, MA0076
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
///     Configurable <see cref="IChatClient"/> test double that supports scripted responses,
///     fallback factories, streaming updates, and call recording for assertions.
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
    private readonly Queue<object> _responses = new(); // PreparedResponse | ChatResponseUpdate[] | Exception
    private readonly Lock _lock = new();
    private Func<RequestContext, PreparedResponse>? _fallbackFactory;
    private int _callIndex;

    /// <summary>
    ///     All calls made to <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/>,
    ///     in order. Each entry records the messages and options passed by the caller.
    /// </summary>
    public List<ChatClientCall> Calls { get; } = [];

    /// <summary>
    ///     The <see cref="ChatOptions"/> from the most recent call.
    /// </summary>
    public ChatOptions? LastOptions { get; private set; }

    /// <summary>
    ///     Number of completed calls.
    /// </summary>
    public int CallCount
    {
        get
        {
            using (_lock.EnterScope())
                return _callIndex;
        }
    }

    /// <summary>
    ///     The finish reason used when a queued response does not provide one explicitly.
    /// </summary>
    public ChatFinishReason FinishReason { get; set; } = ChatFinishReason.Stop;

    /// <summary>
    ///     The model ID used when a queued response does not provide one explicitly.
    /// </summary>
    public string ModelId { get; set; } = "test-model";

    /// <summary>
    ///     Metadata returned by <see cref="GetService"/> when the requested type
    ///     is <see cref="ChatClientMetadata"/>.
    /// </summary>
    public ChatClientMetadata Metadata { get; set; } =
        new("FakeChatClient", new Uri("https://test.example.com"), "test-model");

    /// <summary>
    ///     Creates a fake that always returns a fixed text response.
    /// </summary>
    public static FakeChatClient WithText(string text = "Test response")
    {
        var client = new FakeChatClient();
        return client.UseFactory(_ => client.CreatePreparedResponse([new TextContent(text)]));
    }

    /// <summary>
    ///     Creates a fake that returns different text for successive calls.
    ///     After exhausting the sequence, the last response repeats.
    /// </summary>
    public static FakeChatClient WithSequence(params string[] responses)
    {
        if (responses.Length == 0)
            throw new ArgumentException("At least one response is required.", nameof(responses));

        var client = new FakeChatClient();
        return client.UseFactory(ctx => client.CreatePreparedResponse(
            [new TextContent(ctx.CallIndex < responses.Length ? responses[ctx.CallIndex] : responses[^1])]));
    }

    /// <summary>
    ///     Creates a fake that always returns the specified content items.
    /// </summary>
    public static FakeChatClient WithContent(params AIContent[] content)
    {
        var client = new FakeChatClient();
        return client.UseFactory(_ => client.CreatePreparedResponse(content));
    }

    /// <summary>
    ///     Creates a fake that always returns a function-call response.
    /// </summary>
    public static FakeChatClient WithFunctionCall(
        string functionName,
        IDictionary<string, object?>? arguments = null,
        string? callId = null)
    {
        var client = new FakeChatClient();
        return client.UseFactory(_ => client.CreatePreparedResponse(
            [new FunctionCallContent(callId ?? $"call_{Guid.NewGuid():N}", functionName, arguments)],
            ChatFinishReason.ToolCalls));
    }

    /// <summary>
    ///     Creates a fake that delegates content production to a factory function.
    /// </summary>
    public static FakeChatClient WithFactory(Func<RequestContext, IEnumerable<AIContent>> factory)
    {
        var client = new FakeChatClient();
        return client.UseFactory(ctx =>
        {
            IEnumerable<AIContent> result = factory(ctx);
            return client.CreatePreparedResponse(result as IReadOnlyList<AIContent> ?? [.. result]);
        });
    }

    /// <summary>
    ///     Creates a fake that throws the specified exception on every call.
    /// </summary>
    public static FakeChatClient WithException(Exception exception) =>
        new FakeChatClient().UseFactory(_ => throw exception);

    /// <summary>
    ///     Creates a fake that throws the specified exception type on every call.
    /// </summary>
    public static FakeChatClient WithException<TException>() where TException : Exception, new() =>
        WithException(new TException());

    /// <summary>
    ///     Enqueues a canned non-streaming response with the given text.
    /// </summary>
    public FakeChatClient WithResponse(
        string text,
        ChatFinishReason? finishReason = null,
        UsageDetails? usage = null,
        string? modelId = null) =>
        WithResponse(
            [new TextContent(text)],
            finishReason,
            usage,
            modelId);

    /// <summary>
    ///     Enqueues a canned non-streaming response with the given content items.
    /// </summary>
    public FakeChatClient WithResponse(
        IReadOnlyList<AIContent> contents,
        ChatFinishReason? finishReason = null,
        UsageDetails? usage = null,
        string? modelId = null)
    {
        using (_lock.EnterScope())
            _responses.Enqueue(CreatePreparedResponse(contents, finishReason, usage, modelId));

        return this;
    }

    /// <summary>
    ///     Enqueues a canned non-streaming response with the given content items.
    /// </summary>
    public FakeChatClient WithResponse(
        ChatFinishReason? finishReason = null,
        UsageDetails? usage = null,
        string? modelId = null,
        params AIContent[] contents) =>
        WithResponse(contents, finishReason, usage, modelId);

    /// <summary>
    ///     Enqueues a canned streaming response that yields one update per chunk.
    /// </summary>
    public FakeChatClient WithStreamingResponse(params string[] chunks)
    {
        var updates = chunks
            .Select(static chunk => new ChatResponseUpdate(ChatRole.Assistant, chunk))
            .ToArray();

        return WithStreamingResponse(updates);
    }

    /// <summary>
    ///     Enqueues explicit streaming updates for the next streaming call.
    /// </summary>
    public FakeChatClient WithStreamingResponse(params ChatResponseUpdate[] updates)
    {
        using (_lock.EnterScope())
            _responses.Enqueue(updates);

        return this;
    }

    /// <summary>
    ///     Enqueues an exception to be thrown on the next call.
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
    public FakeChatClient WithError<TException>() where TException : Exception, new() =>
        WithError(new TException());

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RequestContext context = RecordCall(messages, options);
        object? next = DequeueNext(context);

        return next switch
        {
            Exception ex => Task.FromException<ChatResponse>(ex),
            ChatResponseUpdate[] updates => Task.FromResult(CreateResponseFromUpdates(updates)),
            PreparedResponse prepared => Task.FromResult(CreateResponse(prepared)),
            _ => Task.FromResult(CreateResponse(CreatePreparedResponse([new TextContent(string.Empty)])))
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        RequestContext context = RecordCall(messages, options);
        object? next = DequeueNext(context);

        switch (next)
        {
            case Exception ex:
                throw ex;

            case ChatResponseUpdate[] updates:
                foreach (ChatResponseUpdate update in updates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return update;
                }

                yield break;

            case PreparedResponse prepared:
                await foreach (ChatResponseUpdate update in StreamPreparedResponse(prepared, cancellationToken))
                    yield return update;
                yield break;

            default:
                yield break;
        }
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
    }

    private FakeChatClient UseFactory(Func<RequestContext, PreparedResponse> factory)
    {
        using (_lock.EnterScope())
            _fallbackFactory = factory;

        return this;
    }

    private RequestContext RecordCall(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        IReadOnlyList<ChatMessage> snapshot = messages as IReadOnlyList<ChatMessage> ?? [.. messages];

        using (_lock.EnterScope())
        {
            LastOptions = options;
            Calls.Add(new ChatClientCall(snapshot, options));

            return new RequestContext(snapshot, options, _callIndex++);
        }
    }

    private object? DequeueNext(RequestContext context)
    {
        using (_lock.EnterScope())
        {
            if (_responses.Count > 0)
                return _responses.Dequeue();

            return _fallbackFactory?.Invoke(context);
        }
    }

    private PreparedResponse CreatePreparedResponse(
        IReadOnlyList<AIContent> contents,
        ChatFinishReason? finishReason = null,
        UsageDetails? usage = null,
        string? modelId = null) =>
        new(contents, finishReason ?? FinishReason, usage, modelId ?? ModelId);

    private static ChatResponse CreateResponse(PreparedResponse prepared) =>
        new(new ChatMessage(ChatRole.Assistant, [.. prepared.Contents]))
        {
            FinishReason = prepared.FinishReason,
            Usage = prepared.Usage,
            ModelId = prepared.ModelId
        };

    private static ChatResponse CreateResponseFromUpdates(ChatResponseUpdate[] updates)
    {
        List<AIContent> contents = [];
        UsageDetails? usage = null;

        foreach (ChatResponseUpdate update in updates)
        {
            if (update.Contents is { Count: > 0 })
            {
                foreach (AIContent content in update.Contents)
                {
                    if (content is UsageContent usageContent)
                    {
                        usage = usageContent.Details;
                        continue;
                    }

                    contents.Add(content);
                }

                continue;
            }

            if (!string.IsNullOrEmpty(update.Text))
                contents.Add(new TextContent(update.Text));
        }

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, [.. contents]))
        {
            FinishReason = ChatFinishReason.Stop,
            Usage = usage
        };
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamPreparedResponse(
        PreparedResponse prepared,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        List<AIContent> expanded = ExpandTextForStreaming(prepared.Contents);

        if (expanded.Count == 0)
        {
            if (prepared.Usage is not null)
            {
                yield return new ChatResponseUpdate
                {
                    Contents = [new UsageContent(prepared.Usage)],
                    Role = ChatRole.Assistant
                };
            }

            yield break;
        }

        for (int i = 0; i < expanded.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<AIContent> updateContents = [expanded[i]];

            if (i == expanded.Count - 1 && prepared.Usage is not null)
                updateContents.Add(new UsageContent(prepared.Usage));

            yield return new ChatResponseUpdate
            {
                Contents = updateContents,
                Role = ChatRole.Assistant
            };

            await Task.Yield();
        }
    }

    private static List<AIContent> ExpandTextForStreaming(IReadOnlyList<AIContent> contents)
    {
        List<AIContent> expanded = [];

        foreach (AIContent content in contents)
        {
            if (content is TextContent { Text: { Length: > 0 } text })
            {
                string[] words = text.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    string word = i < words.Length - 1 ? words[i] + " " : words[i];
                    expanded.Add(new TextContent(word));
                }

                continue;
            }

            expanded.Add(content);
        }

        return expanded;
    }

    private readonly record struct PreparedResponse(
        IReadOnlyList<AIContent> Contents,
        ChatFinishReason FinishReason,
        UsageDetails? Usage,
        string? ModelId);

    /// <summary>
    ///     Context provided to <see cref="WithFactory"/> delegates containing
    ///     the current request state.
    /// </summary>
    /// <param name="Messages">The messages sent to the client.</param>
    /// <param name="Options">The options sent to the client, if any.</param>
    /// <param name="CallIndex">Zero-based sequential call index.</param>
    public readonly record struct RequestContext(
        IReadOnlyList<ChatMessage> Messages,
        ChatOptions? Options,
        int CallIndex);
}

/// <summary>
///     Records a single call to <see cref="FakeChatClient"/>.
/// </summary>
/// <param name="Messages">The chat messages passed to the call.</param>
/// <param name="Options">The chat options passed to the call, if any.</param>
public sealed record ChatClientCall(
    IReadOnlyList<ChatMessage> Messages,
    ChatOptions? Options);
