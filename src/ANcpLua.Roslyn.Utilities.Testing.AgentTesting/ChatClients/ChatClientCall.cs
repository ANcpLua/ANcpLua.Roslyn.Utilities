// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.ChatClients;

/// <summary>
///     Records a single call to <see cref="FakeChatClient" />. Exposed to tests via
///     <see cref="FakeChatClient.Calls" /> so assertions can inspect what the code under test
///     actually sent.
/// </summary>
/// <param name="Messages">The chat messages passed to the call.</param>
/// <param name="Options">The chat options passed to the call, if any.</param>
public sealed record ChatClientCall(
    IReadOnlyList<ChatMessage> Messages,
    ChatOptions? Options);
