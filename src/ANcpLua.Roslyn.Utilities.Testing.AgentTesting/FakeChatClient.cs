// Licensed to the .NET Foundation under one or more agreements.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
/// A configurable fake <see cref="IChatClient"/> for testing code that depends on LLM interactions.
/// Uses factory methods for common response patterns instead of requiring specialized mock subclasses.
/// </summary>
/// <remarks>
/// <para>
/// All calls are tracked in <see cref="CallHistory"/> and the most recent <see cref="ChatOptions"/>
/// is captured in <see cref="LastOptions"/> automatically.
/// </para>
/// <para>
/// Streaming responses expand <see cref="TextContent"/> into word-level chunks
/// to simulate realistic token-by-token delivery.
/// </para>
/// </remarks>
public sealed class FakeChatClient : IChatClient
{
    private readonly Func<RequestContext, IReadOnlyList<AIContent>> _contentFactory;
    private readonly List<IList<ChatMessage>> _callHistory = [];
    private int _callIndex;

    /// <summary>
    /// The <see cref="ChatOptions"/> from the most recent call.
    /// </summary>
    public ChatOptions? LastOptions { get; private set; }

    /// <summary>
    /// Every call's message list, in order. Use to verify conversation history
    /// is passed correctly through middleware or pipelines.
    /// </summary>
    public IReadOnlyList<IList<ChatMessage>> CallHistory => _callHistory;

    /// <summary>
    /// Number of completed calls.
    /// </summary>
    public int CallCount => _callIndex;

    /// <summary>
    /// The finish reason included in responses. Defaults to <see cref="ChatFinishReason.Stop"/>.
    /// </summary>
    public ChatFinishReason FinishReason { get; set; } = ChatFinishReason.Stop;

    /// <summary>
    /// The model ID included in responses.
    /// </summary>
    public string ModelId { get; set; } = "test-model";

    /// <summary>
    /// Gets or sets the metadata describing this client instance.
    /// </summary>
    public ChatClientMetadata Metadata { get; set; } =
        new("FakeChatClient", new Uri("https://test.example.com"), "test-model");

    private FakeChatClient(Func<RequestContext, IReadOnlyList<AIContent>> contentFactory) =>
        _contentFactory = contentFactory;

    /// <summary>
    /// Creates a fake that always returns a fixed text response.
    /// </summary>
    public static FakeChatClient WithText(string text = "Test response") =>
        new(_ => [new TextContent(text)]);

    /// <summary>
    /// Creates a fake that returns different text for each successive call.
    /// After exhausting the sequence, the last response repeats.
    /// </summary>
    public static FakeChatClient WithSequence(params string[] responses) =>
        new(ctx => [new TextContent(
            ctx.CallIndex < responses.Length
                ? responses[ctx.CallIndex]
                : responses[^1]),]);

    /// <summary>
    /// Creates a fake that returns a <see cref="FunctionCallContent"/> response
    /// with <see cref="ChatFinishReason.ToolCalls"/>.
    /// </summary>
    public static FakeChatClient WithFunctionCall(
        string functionName,
        IDictionary<string, object?>? arguments = null,
        string? callId = null) =>
        new(_ => [new FunctionCallContent(
            callId ?? $"call_{Guid.NewGuid():N}",
            functionName,
            arguments),])
        { FinishReason = ChatFinishReason.ToolCalls };

    /// <summary>
    /// Creates a fake that returns the specified content items.
    /// Use for image, audio, mixed content, or any custom <see cref="AIContent"/> combination.
    /// </summary>
    public static FakeChatClient WithContent(params AIContent[] content) =>
        new(_ => content);

    /// <summary>
    /// Creates a fake that delegates content production to a factory function.
    /// The <see cref="RequestContext"/> provides the message list, options, and call index.
    /// </summary>
    public static FakeChatClient WithFactory(Func<RequestContext, IEnumerable<AIContent>> factory) =>
        new(ctx =>
        {
            IEnumerable<AIContent> result = factory(ctx);
            return result as IReadOnlyList<AIContent> ?? [.. result];
        });

    /// <summary>
    /// Creates a fake that throws the specified exception on every call.
    /// </summary>
    public static FakeChatClient WithError(Exception exception) =>
        new(_ => throw exception);

    /// <summary>
    /// Creates a fake that throws the specified exception type on every call.
    /// </summary>
    public static FakeChatClient WithError<TException>() where TException : Exception, new() =>
        WithError(new TException());

    /// <inheritdoc/>
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<AIContent> contents, int messageCount) = CaptureAndProduce(messages, options);

        ChatResponse response = new([new ChatMessage(ChatRole.Assistant, [.. contents])])
        {
            ModelId = ModelId,
            FinishReason = FinishReason,
            Usage = CreateUsage(messageCount),
        };
        return Task.FromResult(response);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<AIContent> contents, int messageCount) = CaptureAndProduce(messages, options);

        List<AIContent> expanded = ExpandTextForStreaming(contents);
        UsageDetails usage = CreateUsage(messageCount);

        for (int i = 0; i < expanded.Count; i++)
        {
            List<AIContent> updateContents = [expanded[i]];

            if (i == expanded.Count - 1)
            {
                updateContents.Add(new UsageContent(usage));
            }

            yield return new ChatResponseUpdate
            {
                Contents = updateContents,
                Role = ChatRole.Assistant,
            };

            await Task.Yield();
        }
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(ChatClientMetadata))
            return Metadata;

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    private (IReadOnlyList<AIContent> Contents, int MessageCount)
        CaptureAndProduce(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        IList<ChatMessage> messageList = messages as IList<ChatMessage> ?? messages.ToList();
        LastOptions = options;
        _callHistory.Add(messageList);
        int callIndex = _callIndex++;

        RequestContext ctx = new(messageList, options, callIndex);
        IReadOnlyList<AIContent> contents = _contentFactory(ctx);

        return (contents, messageList.Count);
    }

    private static UsageDetails CreateUsage(int messageCount) =>
        new()
        {
            InputTokenCount = 10 + (messageCount * 5),
            OutputTokenCount = 5,
            TotalTokenCount = 15 + (messageCount * 5),
        };

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
            }
            else
            {
                expanded.Add(content);
            }
        }

        return expanded;
    }

    /// <summary>
    /// Context provided to <see cref="WithFactory"/> delegates containing
    /// the current request state.
    /// </summary>
    /// <param name="Messages">The messages sent to the client.</param>
    /// <param name="Options">The options sent to the client, if any.</param>
    /// <param name="CallIndex">Zero-based sequential call index.</param>
    public readonly record struct RequestContext(
        IList<ChatMessage> Messages,
        ChatOptions? Options,
        int CallIndex);
}
