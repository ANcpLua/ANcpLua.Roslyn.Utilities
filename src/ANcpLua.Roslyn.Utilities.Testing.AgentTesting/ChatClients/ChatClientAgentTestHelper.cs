// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — ChatClientAgentTestHelper.cs

using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.ChatClients;

/// <summary>
/// Shared test helper for <see cref="ChatClientAgent"/> integration tests. Builds a sequential mock
/// <see cref="IChatClient"/>, captures inputs, supports multi-turn reuse via shared callIndex/capturedInputs,
/// and structurally verifies persisted history against <see cref="ExpectedMessage"/> patterns.
/// </summary>
/// <remarks>
/// This is a higher-level complement to <see cref="FakeChatClient"/>: FakeChatClient mocks an
/// <see cref="IChatClient"/> in isolation, whereas <c>ChatClientAgentTestHelper</c> orchestrates the
/// full agent-&gt;chat-client loop with history verification. Use FakeChatClient for unit tests of
/// chat-client-level behavior; use this helper when exercising multi-turn agent conversations with
/// structural assertions over the persisted chat history.
/// </remarks>
public static class ChatClientAgentTestHelper
{
    /// <summary>Expected service call: optional input verifier and the response to return.</summary>
    public sealed record ServiceCallExpectation(
        ChatResponse Response,
        Action<List<ChatMessage>>? VerifyInput = null);

    /// <summary>Expected message shape for structural history comparison.</summary>
    public sealed record ExpectedMessage(
        ChatRole Role,
        string? TextContains = null,
        Type[]? ContentTypes = null);

    /// <summary>The result of a RunAsync invocation with full diagnostics for further assertions.</summary>
    public sealed record RunResult(
        AgentResponse Response,
        ChatClientAgentSession Session,
        ChatClientAgent Agent,
        Mock<IChatClient> MockService,
        int TotalServiceCalls,
        List<List<ChatMessage>> CapturedServiceInputs);

    public static Mock<IChatClient> CreateSequentialMock(
        List<ServiceCallExpectation> expectations,
        Ref<int> callIndex,
        List<List<ChatMessage>> capturedInputs)
    {
        Mock<IChatClient> mock = new();
        mock.Setup(s => s.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((msgs, _, _) =>
            {
                int idx = callIndex.Value++;
                var messageList = msgs.ToList();
                capturedInputs.Add(messageList);

                if (idx >= expectations.Count)
                {
                    throw new InvalidOperationException(
                        $"Mock received unexpected service call #{idx + 1}. Only {expectations.Count} call(s) were expected.");
                }

                var expectation = expectations[idx];
                expectation.VerifyInput?.Invoke(messageList);
                return Task.FromResult(expectation.Response);
            });
        return mock;
    }

    public static async Task<RunResult> RunAsync(
        List<ChatMessage> inputMessages,
        List<ServiceCallExpectation> serviceCallExpectations,
        ChatClientAgentOptions? agentOptions = null,
        ChatClientAgentSession? existingSession = null,
        ChatClientAgent? existingAgent = null,
        Mock<IChatClient>? existingMock = null,
        Ref<int>? callIndex = null,
        List<List<ChatMessage>>? capturedInputs = null,
        List<ChatMessage>? initialChatHistory = null,
        AgentRunOptions? runOptions = null,
        int? expectedServiceCallCount = null,
        List<ExpectedMessage>? expectedHistory = null)
    {
        callIndex ??= new Ref<int>(0);
        capturedInputs ??= [];
        var mock = existingMock ?? CreateSequentialMock(serviceCallExpectations, callIndex, capturedInputs);
        agentOptions ??= new ChatClientAgentOptions();

        var agent = existingAgent ?? new ChatClientAgent(
            mock.Object,
            options: agentOptions,
            services: new ServiceCollection().BuildServiceProvider());

        var session = existingSession ?? (await agent.CreateSessionAsync().ConfigureAwait(false) as ChatClientAgentSession)!;

        if (initialChatHistory is not null)
        {
            (agent.ChatHistoryProvider as InMemoryChatHistoryProvider)
                ?.SetMessages(session, new List<ChatMessage>(initialChatHistory));
        }

        var response = await agent.RunAsync(inputMessages, session, runOptions).ConfigureAwait(false);

        var result = new RunResult(response, session, agent, mock, callIndex.Value, capturedInputs);

        if (expectedServiceCallCount.HasValue)
        {
            Assert.Equal(expectedServiceCallCount.Value, callIndex.Value);
        }

        if (expectedHistory is not null)
        {
            var history = GetPersistedHistory(agent, session);
            AssertMessagesMatch(history, expectedHistory);
        }

        return result;
    }

    public static void AssertMessagesMatch(List<ChatMessage> actual, List<ExpectedMessage> expected)
    {
        Assert.True(
            expected.Count == actual.Count,
            $"Expected {expected.Count} message(s) but found {actual.Count}.\nActual messages:\n{FormatMessages(actual)}");

        for (int i = 0; i < expected.Count; i++)
        {
            var exp = expected[i];
            var act = actual[i];

            Assert.True(
                exp.Role == act.Role,
                $"Message [{i}]: expected role {exp.Role} but found {act.Role}.\nActual messages:\n{FormatMessages(actual)}");

            if (exp.TextContains is not null)
            {
                Assert.Contains(exp.TextContains, act.Text, StringComparison.Ordinal);
            }

            if (exp.ContentTypes is not null)
            {
                AssertContentTypes(act.Contents, exp.ContentTypes, i);
            }
        }
    }

    public static List<ChatMessage> GetPersistedHistory(ChatClientAgent agent, AgentSession session) =>
        (agent.ChatHistoryProvider as InMemoryChatHistoryProvider)?.GetMessages(session) ?? [];

    public static string FormatMessages(IEnumerable<ChatMessage> messages)
    {
        var sb = new StringBuilder();
        int index = 0;
        foreach (var msg in messages)
        {
            sb.AppendLine($"  [{index}] Role={msg.Role}, Text=\"{msg.Text}\", Contents=[{string.Join(", ", msg.Contents.Select(c => c.GetType().Name))}]");
            index++;
        }

        return sb.ToString();
    }

    /// <summary>Mutable reference wrapper for value types so multi-turn callers can share one counter.</summary>
    public sealed class Ref<T>(T value) where T : struct
    {
        public T Value { get; set; } = value;
    }

    private static void AssertContentTypes(IList<AIContent> contents, Type[] expectedTypes, int messageIndex)
    {
        Assert.True(
            contents.Count >= expectedTypes.Length,
            $"Message [{messageIndex}]: expected at least {expectedTypes.Length} content(s) but found {contents.Count}. " +
            $"Actual types: [{string.Join(", ", contents.Select(c => c.GetType().Name))}]");

        foreach (var expectedType in expectedTypes)
        {
            Assert.True(
                contents.Any(c => expectedType.IsInstanceOfType(c)),
                $"Message [{messageIndex}]: expected content of type {expectedType.Name} but found [{string.Join(", ", contents.Select(c => c.GetType().Name))}]");
        }
    }
}
