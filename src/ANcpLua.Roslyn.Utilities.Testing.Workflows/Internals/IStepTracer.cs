// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/IStepTracer.cs

﻿// Copyright (c) Microsoft. All rights reserved.

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

internal interface IStepTracer
{
    void TraceActivated(string executorId);
    void TraceCheckpointCreated(CheckpointInfo checkpoint);
    void TraceIntantiated(string executorId);
    void TraceStatePublished();
}
