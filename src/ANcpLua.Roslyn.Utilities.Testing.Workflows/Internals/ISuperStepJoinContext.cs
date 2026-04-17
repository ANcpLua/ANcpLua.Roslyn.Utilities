// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/ISuperStepJoinContext.cs

// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

internal interface ISuperStepJoinContext
{
    bool IsCheckpointingEnabled { get; }
    bool ConcurrentRunsEnabled { get; }

    ValueTask ForwardWorkflowEventAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default);
    ValueTask SendMessageAsync<TMessage>(string senderId, [DisallowNull] TMessage message, CancellationToken cancellationToken = default);
    ValueTask YieldOutputAsync<TOutput>(string senderId, [DisallowNull] TOutput output, CancellationToken cancellationToken = default);

    ValueTask<string> AttachSuperstepAsync(ISuperStepRunner superStepRunner, CancellationToken cancellationToken = default);
    ValueTask<bool> DetachSuperstepAsync(string id);
}
