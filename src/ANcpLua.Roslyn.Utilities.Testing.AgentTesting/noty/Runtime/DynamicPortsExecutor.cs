// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/DynamicPortsExecutor.cs

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows;

namespace Noty.Workflows.Tests;

// Owns N dynamic request ports and records every response it sees, keyed by port id.
// Combine with TestRunContext to verify that the EdgeMap routes responses to the
// right port without going through the real InProcessExecution.
internal sealed class DynamicPortsExecutor<TRequest, TResponse>(string id, params IEnumerable<string> ports)
    : Executor(id)
{
    public Dictionary<string, PortBinding> PortBindings { get; } = new();

    public ConcurrentDictionary<string, ConcurrentQueue<TResponse>> ReceivedResponses { get; } = new();

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
        => protocolBuilder.ConfigureRoutes(routeBuilder =>
        {
            foreach (string portId in ports)
            {
                routeBuilder = routeBuilder.AddPortHandler<TRequest, TResponse>(portId,
                    (response, context, cancellationToken) =>
                    {
                        this.ReceivedResponses.GetOrAdd(portId, _ => new()).Enqueue(response);
                        return default;
                    },
                    out var binding);

                this.PortBindings[portId] = binding;
            }
        });

    public ValueTask PostRequestAsync(string portId, TRequest request, TestRunContext testContext, string? requestId = null)
    {
        var binding = this.PortBindings[portId];
        return binding.Sink.PostAsync(ExternalRequest.Create(binding.Port, request, requestId));
    }
}
