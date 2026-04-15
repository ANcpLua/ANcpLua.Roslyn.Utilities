// Licensed to the .NET Foundation under one or more agreements.
//
// Reference IChatClientAgentFixture for Google Gemini via Mscc.GenerativeAI.
// Gemini has a free tier — set TestSettings.GoogleGeminiApiKey and ChatModelName
// (e.g. "gemini-2.0-flash") to run conformance tests against it.

using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Microsoft;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Examples;

public class GoogleGeminiChatCompletionFixture : IChatClientAgentFixture
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
        var apiKey = TestConfiguration.GetRequiredValue(TestSettings.GoogleGeminiApiKey);
        var modelName = TestConfiguration.GetRequiredValue(TestSettings.GoogleGeminiChatModelName);

        IChatClient chatClient = new GeminiChatClient(apiKey: apiKey, model: modelName, logger: null);

        return Task.FromResult(new ChatClientAgent(chatClient, options: new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions { Instructions = instructions, Tools = aiTools, ModelId = modelName },
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
