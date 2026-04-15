// Licensed to the .NET Foundation under one or more agreements.
//
// Reference IChatClientAgentFixture for Ollama — local-only, no API key required.
// Point TestSettings.OllamaEndpoint at your local Ollama server (default http://localhost:11434).
// This is the most accessible fixture: a budget-zero consumer can run conformance tests against
// a local Ollama install without any cloud signup.

using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Examples;

public class OllamaChatCompletionFixture : IChatClientAgentFixture
{
    private const string DefaultEndpoint = "http://localhost:11434";

    private ChatClientAgent _agent = null!;
    private OllamaApiClient? _ollamaClient;

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
        var endpoint = TestConfiguration.GetValue(TestSettings.OllamaEndpoint) ?? DefaultEndpoint;
        var model = TestConfiguration.GetRequiredValue(TestSettings.OllamaChatModelName);

        this._ollamaClient = new OllamaApiClient(new Uri(endpoint), model);
        IChatClient chatClient = this._ollamaClient;

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
        this._ollamaClient?.Dispose();
        GC.SuppressFinalize(this);
        return default;
    }
}
