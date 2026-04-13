// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance;

/// <summary>
///     Per-provider fixture contract for the conformance test suites in
///     <see cref="RunTests{TFixture}" /> and friends. One implementation per provider unlocks
///     the entire conformance test set against that provider.
/// </summary>
public interface IAgentFixture : IAsyncLifetime
{
    /// <summary>The agent under test for run-suite scenarios. Built once per fixture.</summary>
    AIAgent Agent { get; }

    /// <summary>Return the chat history persisted by the provider for the given session.</summary>
    Task<IReadOnlyList<ChatMessage>> GetChatHistoryAsync(AIAgent agent, AgentSession session);

    /// <summary>Delete a session on the provider, if applicable. No-op for stateless providers.</summary>
    Task DeleteSessionAsync(AgentSession session);
}
