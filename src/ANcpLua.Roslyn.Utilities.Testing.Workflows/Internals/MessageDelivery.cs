// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/MessageDelivery.cs

﻿// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

internal sealed class MessageDelivery
{
    [JsonConstructor]
    internal MessageDelivery(MessageEnvelope envelope, string targetId)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(targetId);
        this.Envelope = envelope;
        this.TargetId = targetId;
    }

    internal MessageDelivery(MessageEnvelope envelope, Executor target)
        : this(envelope, target.Id)
    {
        ArgumentNullException.ThrowIfNull(target);
        this.TargetCache = target;
    }

    public string TargetId { get; }
    public MessageEnvelope Envelope { get; }

    [JsonIgnore]
    internal Executor? TargetCache { get; set; }
}
