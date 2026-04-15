// Licensed to the .NET Foundation under one or more agreements.
//
// Reference IChatClientAgentFixture for Azure OpenAI ChatCompletion.
// Authenticates via API key (TestSettings.AzureOpenAIApiKey) against the deployment
// named TestSettings.AzureOpenAIChatDeploymentName at TestSettings.AzureOpenAIEndpoint.

using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Examples;

public class AzureOpenAIChatCompletionFixture : IChatClientAgentFixture
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
        var endpoint = new Uri(TestConfiguration.GetRequiredValue(TestSettings.AzureOpenAIEndpoint));
        var apiKey = TestConfiguration.GetRequiredValue(TestSettings.AzureOpenAIApiKey);
        var deployment = TestConfiguration.GetRequiredValue(TestSettings.AzureOpenAIChatDeploymentName);

        IChatClient chatClient = new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey))
            .GetChatClient(deployment)
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
