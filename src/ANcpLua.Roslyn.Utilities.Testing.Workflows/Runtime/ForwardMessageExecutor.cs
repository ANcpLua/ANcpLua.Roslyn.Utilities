// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/ForwardMessageExecutor.cs

using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows;

// Minimal pass-through. Use it to build edge-routing tests cheaply.
internal sealed class ForwardMessageExecutor<TMessage>(string id) : Executor(id)
    where TMessage : notnull
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        protocolBuilder.RouteBuilder.AddHandler<TMessage>((message, ctx) => ctx.SendMessageAsync(message));
        return protocolBuilder.SendsMessage<TMessage>();
    }
}
