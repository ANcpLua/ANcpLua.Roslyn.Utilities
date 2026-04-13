// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/ExecutionExtensions.cs

using System;
using Microsoft.Agents.AI.Workflows.InProc;

namespace Noty.Workflows.Tests;

// Parametric [Theory] axis. Pair with:
//   [Theory]
//   [InlineData(ExecutionEnvironment.InProcess_OffThread)]
//   [InlineData(ExecutionEnvironment.InProcess_Lockstep)]
// so the same workflow test runs across every in-process scheduler.
public enum ExecutionEnvironment
{
    InProcess_OffThread,
    InProcess_Lockstep,
    InProcess_Concurrent,
}

internal static class ExecutionExtensions
{
    public static InProcessExecutionEnvironment ToWorkflowExecutionEnvironment(this ExecutionEnvironment environment)
        => environment switch
        {
            ExecutionEnvironment.InProcess_OffThread => InProcessExecution.OffThread,
            ExecutionEnvironment.InProcess_Lockstep => InProcessExecution.Lockstep,
            ExecutionEnvironment.InProcess_Concurrent => InProcessExecution.Concurrent,
            _ => throw new InvalidOperationException($"Unknown execution environment {environment}"),
        };
}
