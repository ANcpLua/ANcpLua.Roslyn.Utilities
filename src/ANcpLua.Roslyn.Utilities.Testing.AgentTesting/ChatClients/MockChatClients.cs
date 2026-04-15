// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — MockChatClients.cs
//
// Reusable mock IChatClient implementations for unit tests:
//   SimpleMockChatClient             — fixed text response, captures last ChatOptions
//   StatefulMockChatClient           — different response per call
//   ConversationMemoryMockChatClient — captures the full message history per call
//   FunctionCallMockChatClient       — returns a single FunctionCallContent
//   ToolCallMockChatClient           — function-call with parsed JSON arguments
//   CustomContentMockChatClient      — content selected per-message via callback
//
// NOTE: consider using <see cref="FakeChatClient"/> for new tests — it offers a
// fluent builder API, streaming support, fallback factories, and call recording
// in a single class. These variants are retained for scenarios where an explicit
// type name communicates intent (teaching examples, single-purpose unit tests).

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.ChatClients;

public static class TestHelpers
{
    private static readonly ChatClientMetadata s_metadata = new("Test", new Uri("https://test.example.com"), "test-model");

    private static UsageDetails Usage(int messageCount) => new()
    {
        InputTokenCount = 10 + (messageCount * 5),
        OutputTokenCount = 5,
        TotalTokenCount = 15 + (messageCount * 5),
    };

    private static ChatResponse BuildResponse(string text, int messageCount, ChatFinishReason finishReason = default) =>
        new([new ChatMessage(ChatRole.Assistant, text)])
        {
            ModelId = "test-model",
            FinishReason = finishReason == default ? ChatFinishReason.Stop : finishReason,
            Usage = Usage(messageCount),
        };

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamWords(string text, int messageCount, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        string[] words = text.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            string content = i < words.Length - 1 ? words[i] + " " : words[i];
            ChatResponseUpdate update = new() { Contents = [new TextContent(content)], Role = ChatRole.Assistant };
            if (i == words.Length - 1)
            {
                update.Contents.Add(new UsageContent(Usage(messageCount)));
            }
            yield return update;
        }
    }

    public sealed class SimpleMockChatClient(string responseText = "Test response") : IChatClient
    {
        public ChatOptions? LastChatOptions { get; private set; }
        public ChatClientMetadata Metadata { get; } = s_metadata;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (options is not null) { this.LastChatOptions = options; }
            return Task.FromResult(BuildResponse(responseText, messages.Count()));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (options is not null) { this.LastChatOptions = options; }
            return StreamWords(responseText, messages.Count(), cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
        public void Dispose() { }
    }

    public sealed class StatefulMockChatClient(string[] responseTexts) : IChatClient
    {
        private int _callIndex;
        public ChatClientMetadata Metadata { get; } = s_metadata;

        private string Next() => responseTexts[Math.Min(this._callIndex++, responseTexts.Length - 1)];

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(BuildResponse(this.Next(), messages.Count()));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => StreamWords(this.Next(), messages.Count(), cancellationToken);

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
        public void Dispose() { }
    }

    public sealed class ConversationMemoryMockChatClient(string responseText = "Test response") : IChatClient
    {
        /// <summary>Each entry is the messages list received for that call.</summary>
        public List<List<ChatMessage>> CallHistory { get; } = [];
        public ChatClientMetadata Metadata { get; } = s_metadata;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            this.CallHistory.Add(messages.ToList());
            return Task.FromResult(BuildResponse(responseText, 1));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            this.CallHistory.Add(messages.ToList());
            return StreamWords(responseText, 1, cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
        public void Dispose() { }
    }

    public sealed class FunctionCallMockChatClient(string functionName = "test_function", string arguments = """{"param":"value"}""") : IChatClient
    {
        private readonly Dictionary<string, object?> _arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(arguments) ?? [];
        public ChatClientMetadata Metadata { get; } = s_metadata;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            ChatMessage message = new(ChatRole.Assistant, [new FunctionCallContent("call_123", functionName) { Arguments = this._arguments }]);
            return Task.FromResult(new ChatResponse([message])
            {
                ModelId = "test-model",
                FinishReason = ChatFinishReason.ToolCalls,
                Usage = new UsageDetails { InputTokenCount = 80, OutputTokenCount = 25, TotalTokenCount = 105 },
            });
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            yield return new ChatResponseUpdate
            {
                Contents = [
                    new FunctionCallContent("call_123", functionName) { Arguments = this._arguments },
                    new UsageContent(new UsageDetails { InputTokenCount = 80, OutputTokenCount = 25, TotalTokenCount = 105 }),
                ],
                Role = ChatRole.Assistant,
            };
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
        public void Dispose() { }
    }

    public sealed class ToolCallMockChatClient : IChatClient
    {
        private readonly string _functionName;
        private readonly Dictionary<string, object?> _arguments;

        public ToolCallMockChatClient(string functionName, string argumentsJson)
        {
            this._functionName = functionName;
            using var doc = JsonDocument.Parse(argumentsJson);
            this._arguments = [];
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                this._arguments[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => prop.Value.ToString(),
                };
            }
        }

        public ChatClientMetadata Metadata { get; } = s_metadata;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            int messageCount = messages.Count();
            FunctionCallContent functionCall = new("call_test123", this._functionName, this._arguments);
            ChatMessage message = new(ChatRole.Assistant, [functionCall]);
            return Task.FromResult(new ChatResponse([message])
            {
                ModelId = "test-model",
                FinishReason = ChatFinishReason.ToolCalls,
                Usage = Usage(messageCount),
            });
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            int messageCount = messages.Count();
            FunctionCallContent functionCall = new("call_test123", this._functionName, this._arguments);
            yield return new ChatResponseUpdate
            {
                Contents = [functionCall, new UsageContent(Usage(messageCount))],
                Role = ChatRole.Assistant,
            };
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
        public void Dispose() { }
    }

    public sealed class CustomContentMockChatClient(Func<ChatMessage, IEnumerable<AIContent>> contentProvider) : IChatClient
    {
        public ChatClientMetadata Metadata { get; } = s_metadata;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            ChatMessage lastMessage = messages.Last();
            ChatMessage message = new(ChatRole.Assistant, contentProvider(lastMessage).ToList());
            return Task.FromResult(new ChatResponse([message])
            {
                ModelId = "test-model",
                FinishReason = ChatFinishReason.Stop,
                Usage = Usage(0),
            });
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            List<AIContent> contentList = contentProvider(messages.Last()).ToList();
            for (int i = 0; i < contentList.Count; i++)
            {
                List<AIContent> updateContents = [contentList[i]];
                if (i == contentList.Count - 1)
                {
                    updateContents.Add(new UsageContent(Usage(0)));
                }
                yield return new ChatResponseUpdate { Contents = updateContents, Role = ChatRole.Assistant };
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
        public void Dispose() { }
    }
}
