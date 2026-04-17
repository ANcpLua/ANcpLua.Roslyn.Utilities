// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/IRunnerContext.cs

// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows.Observability;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

internal interface IRunnerContext : IExternalRequestSink, ISuperStepJoinContext
{
    WorkflowTelemetryContext TelemetryContext { get; }

    ValueTask AddEventAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default);
    ValueTask SendMessageAsync(string sourceId, object message, string? targetId = null, CancellationToken cancellationToken = default);

    ValueTask<StepContext> AdvanceAsync(CancellationToken cancellationToken = default);
    IWorkflowContext BindWorkflowContext(string executorId, Dictionary<string, string>? traceContext = null);
    ValueTask<Executor> EnsureExecutorAsync(string executorId, IStepTracer? tracer, CancellationToken cancellationToken = default);
}
