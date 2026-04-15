// Licensed to the .NET Foundation under one or more agreements.
//
// Reference IChatClientAgentFixture for OpenRouter (https://openrouter.ai).
// OpenRouter is a meta-provider that fronts 100+ models behind an OpenAI-compatible
// ChatCompletion endpoint, so we reuse the OpenAI SDK with a custom base URL.

using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Examples;

public class OpenRouterChatCompletionFixture : IChatClientAgentFixture
{
    private const string DefaultBaseUrl = "https://openrouter.ai/api/v1";

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
        var apiKey = TestConfiguration.GetRequiredValue(TestSettings.OpenRouterApiKey);
        var model = TestConfiguration.GetRequiredValue(TestSettings.OpenRouterChatModelName);
        var baseUrl = TestConfiguration.GetValue(TestSettings.OpenRouterBaseUrl) ?? DefaultBaseUrl;

        var options = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
        IChatClient chatClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options)
            .GetChatClient(model)
            .AsIChatClient();

        return Task.FromResult(new ChatClientAgent(chatClient, options: new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions { Instructions = instructions, Tools = aiTools },
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
