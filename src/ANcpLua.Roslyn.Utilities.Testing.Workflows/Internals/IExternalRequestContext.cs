// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: IExternalRequestContext.cs

﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI.Workflows.Execution;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

internal interface IExternalRequestContext
{
    IExternalRequestSink RegisterPort(RequestPort port);
}
