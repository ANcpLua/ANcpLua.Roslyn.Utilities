// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/TestingExecutor.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows;

// Walks a pre-baked list of actions; set loop=true to cycle forever.
// LinkCancellation wires external tokens to the internal CTS so SetCancel()
// cancels every linked source at once. Use it to drive cancellation tests
// deterministically.
internal abstract partial class TestingExecutor<TIn, TOut> : Executor, IDisposable
{
    private readonly bool _loop;
    private readonly Func<TIn, IWorkflowContext, CancellationToken, ValueTask<TOut>>[] _actions;
    private readonly HashSet<CancellationToken> _linkedTokens = [];
    private CancellationTokenSource _internalCts = new();
    private int _nextActionIndex;

    protected TestingExecutor(string id, bool loop = false, params Func<TIn, IWorkflowContext, CancellationToken, ValueTask<TOut>>[] actions)
        : base(id)
    {
        this._loop = loop;
        this._actions = actions;
    }

    public int Iterations { get; private set; }

    public bool AtEnd => this._nextActionIndex >= this._actions.Length;

    public bool Completed => !this._loop && this.AtEnd;

    public void UnlinkCancellation(CancellationToken cancellationToken) =>
        this._linkedTokens.Remove(cancellationToken);

    public void LinkCancellation(CancellationToken cancellationToken)
    {
        this._linkedTokens.Add(cancellationToken);
        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource([.. this._linkedTokens]);
        tokenSource = Interlocked.Exchange(ref this._internalCts, tokenSource);
        tokenSource.Dispose();
    }

    public void SetCancel() => Volatile.Read(ref this._internalCts).Cancel();

    [MessageHandler]
    public ValueTask<TOut> RouteToActionsAsync(TIn message, IWorkflowContext context)
    {
        if (this.AtEnd)
        {
            if (!this._loop)
            {
                throw new InvalidOperationException("No more actions to execute and looping is disabled.");
            }

            this.Iterations++;
            this._nextActionIndex = 0;
        }

        try
        {
            var action = this._actions[this._nextActionIndex];
            return action(message, context, Volatile.Read(ref this._internalCts).Token);
        }
        finally
        {
            this._nextActionIndex++;
        }
    }

    ~TestingExecutor() => this.Dispose(false);

    protected virtual void Dispose(bool disposing) => this._internalCts.Dispose();

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
}
