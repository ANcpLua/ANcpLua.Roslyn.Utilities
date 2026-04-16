// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: PortBinding.cs

﻿// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows.Execution;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

internal class PortBinding(RequestPort port, IExternalRequestSink sink)
{
    public RequestPort Port => port;
    public IExternalRequestSink Sink => sink;

    public ValueTask PostRequestAsync<TRequest>(TRequest request, string? requestId = null, CancellationToken cancellationToken = default)
    {
        ExternalRequest externalRequest = ExternalRequest.Create(this.Port, request, requestId);
        return this.Sink.PostAsync(externalRequest);
    }
}
