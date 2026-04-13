// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance;

/// <summary>
///     Extension of <see cref="IAgentFixture" /> for providers backed by an <see cref="IChatClient" />.
///     Adds the ability to construct fresh agents on demand (for tests that need their own
///     instructions or tool sets) and to delete agents on providers that require cleanup.
/// </summary>
public interface IChatClientAgentFixture : IAgentFixture
{
    /// <summary>The shared chat client backing <see cref="IAgentFixture.Agent" />.</summary>
    IChatClient ChatClient { get; }

    /// <summary>Create a new <see cref="ChatClientAgent" /> with the supplied configuration.</summary>
    Task<ChatClientAgent> CreateChatClientAgentAsync(
        string name = "HelpfulAssistant",
        string instructions = "You are a helpful assistant.",
        IList<AITool>? aiTools = null);

    /// <summary>Delete an agent on the provider, if applicable. No-op for stateless providers.</summary>
    Task DeleteAgentAsync(ChatClientAgent agent);
}
