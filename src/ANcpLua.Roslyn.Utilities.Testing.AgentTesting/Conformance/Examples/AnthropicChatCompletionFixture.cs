// Licensed to the .NET Foundation under one or more agreements.
//
// Reference IChatClientAgentFixture for Anthropic (Claude).
// Uses Anthropic.SDK 5.x — its AnthropicClient.Messages exposes an IChatClient directly.

using Anthropic.SDK;
using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Examples;

public class AnthropicChatCompletionFixture : IChatClientAgentFixture
{
    private ChatClientAgent _agent = null!;

    public AIAgent Agent => this._agent;

    public IChatClient ChatClient => this._agent.ChatClient;

    public Task<IReadOnlyList<ChatMessage>> GetChatHistoryAsync(AIAgent agent, AgentSession session)
    {
        var provider = (agent as ChatClientAgent)?.ChatHistoryProvider as InMemoryChatHistoryProvider;
        IReadOnlyList<ChatMessage> messages = provider?.GetMessages(session).ToList() ?? [];
        return Task.FromResult(messages);
    }

    public Task<ChatClientAgent> CreateChatClientAgentAsync(
        string name = "HelpfulAssistant",
        string instructions = "You are a helpful assistant.",
        IList<AITool>? aiTools = null)
    {
        var apiKey = TestConfiguration.GetRequiredValue(TestSettings.AnthropicApiKey);
        var model = TestConfiguration.GetRequiredValue(TestSettings.AnthropicChatModelName);

        IChatClient chatClient = new AnthropicClient(new APIAuthentication(apiKey))
            .Messages
            .AsBuilder()
            .ConfigureOptions(opts => opts.ModelId ??= model)
            .Build();

        return Task.FromResult(new ChatClientAgent(chatClient, options: new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions { Instructions = instructions, Tools = aiTools, ModelId = model },
        }));
    }

    public Task DeleteAgentAsync(ChatClientAgent agent) => Task.CompletedTask;

    public Task DeleteSessionAsync(AgentSession session) => Task.CompletedTask;

    public async ValueTask InitializeAsync() => this._agent = await this.CreateChatClientAgentAsync().ConfigureAwait(false);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return default;
    }
}
