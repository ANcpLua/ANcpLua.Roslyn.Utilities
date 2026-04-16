// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — WorkflowEvents.cs

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Framework;

/// <summary>
/// Strongly-typed projection of a flat <see cref="WorkflowEvent"/> list into the categories
/// that test assertions actually care about (actions, executors, conversations, inputs, agent responses).
/// Constructed once after a run completes — every property is a frozen <see cref="IReadOnlyList{T}"/>.
/// </summary>
public sealed class WorkflowEvents
{
    /// <summary>
    /// Projects the flat event list into typed sublists.
    /// </summary>
    /// <param name="workflowEvents">The raw event sequence from a workflow run.</param>
    public WorkflowEvents(IReadOnlyList<WorkflowEvent> workflowEvents)
    {
        this.Events = workflowEvents;
        this.EventCounts = workflowEvents.GroupBy(e => e.GetType()).ToDictionary(e => e.Key, e => e.Count());
        this.ActionInvokeEvents = workflowEvents.OfType<DeclarativeActionInvokedEvent>().ToList();
        this.ActionCompleteEvents = workflowEvents.OfType<DeclarativeActionCompletedEvent>().ToList();
        this.ConversationEvents = workflowEvents.OfType<ConversationUpdateEvent>().ToList();
        this.ExecutorInvokeEvents = workflowEvents.OfType<ExecutorInvokedEvent>().ToList();
        this.ExecutorCompleteEvents = workflowEvents.OfType<ExecutorCompletedEvent>().ToList();
        this.InputEvents = workflowEvents.OfType<RequestInfoEvent>().ToList();
        this.AgentResponseEvents = workflowEvents.OfType<AgentResponseEvent>().ToList();
        this.SuperStepEvents = workflowEvents.OfType<SuperStepCompletedEvent>().ToList();
        this.ErrorEvents = workflowEvents.OfType<WorkflowErrorEvent>().ToList();
        this.OutputEvents = workflowEvents.OfType<WorkflowOutputEvent>().ToList();
    }

    /// <summary>The raw event list in order of emission.</summary>
    public IReadOnlyList<WorkflowEvent> Events { get; }

    /// <summary>Per-type event count histogram.</summary>
    public IReadOnlyDictionary<Type, int> EventCounts { get; }

    /// <summary>Declarative action invocation events.</summary>
    public IReadOnlyList<DeclarativeActionInvokedEvent> ActionInvokeEvents { get; }

    /// <summary>Declarative action completion events.</summary>
    public IReadOnlyList<DeclarativeActionCompletedEvent> ActionCompleteEvents { get; }

    /// <summary>Conversation creation/update events.</summary>
    public IReadOnlyList<ConversationUpdateEvent> ConversationEvents { get; }

    /// <summary>Executor invocation events.</summary>
    public IReadOnlyList<ExecutorInvokedEvent> ExecutorInvokeEvents { get; }

    /// <summary>Executor completion events.</summary>
    public IReadOnlyList<ExecutorCompletedEvent> ExecutorCompleteEvents { get; }

    /// <summary>External input request events (HITL pause points).</summary>
    public IReadOnlyList<RequestInfoEvent> InputEvents { get; }

    /// <summary>Agent response events (text or tool-call results).</summary>
    public IReadOnlyList<AgentResponseEvent> AgentResponseEvents { get; }

    /// <summary>Super-step completion events with checkpoint info.</summary>
    public IReadOnlyList<SuperStepCompletedEvent> SuperStepEvents { get; }

    /// <summary>Workflow error events.</summary>
    public IReadOnlyList<WorkflowErrorEvent> ErrorEvents { get; }

    /// <summary>Workflow output events.</summary>
    public IReadOnlyList<WorkflowOutputEvent> OutputEvents { get; }
}
