# noty — one import, no relearning

Testing a `Microsoft.Agents.AI.Workflows` workflow should be three lines:
derive a fixture, override `BuildWorkflow()`, assert.

```csharp
public sealed class MyFlowTests(ITestOutputHelper output)
    : WorkflowFixture<string>(output)
{
    protected override Workflow BuildWorkflow() => MyFlow.Build();

    [Fact]
    public async Task ReversesAfterUppercasingAsync()
    {
        var run = await this.RunAsync("Hello, World!");

        run.Should()
           .YieldOutput<string>(s => s.Should().Be("!DLROW ,OLLEH"))
           .And.CompletedExecutors("Uppercase", "ReverseText")
           .And.HaveNoErrors();
    }
}
```

That's the entire API surface for the common path. Checkpoints and external
requests add one more call each:

```csharp
var first   = await this.RunWithCheckpointingAsync(NumberSignal.Init);
var answered = await this.AnswerRequestAsync(first, data: 42);
answered.Should().HaveNoErrors();
```

## The one file that matters

**`WorkflowFixture.cs`** — 185 lines, self-contained. Gives you:

| Method | What it does |
| --- | --- |
| `RunAsync(input, env)` | Runs the workflow to quiescence. Returns `WorkflowRunResult`. |
| `RunWithCheckpointingAsync(input, env)` | Same, but captures a checkpoint on every super-step. |
| `ResumeAsync(response?, env)` | Resumes the most recent run from its last checkpoint, optionally pumping an external response. |
| `AnswerRequestAsync(pending, data)` | Convenience: finds the first pending request in a run, answers it, resumes. |

`WorkflowRunResult` is a record with typed `Outputs`, `CompletedExecutors`,
`SuperSteps`, `Errors`, `PendingRequests` and `LastCheckpoint`. Call `.Should()`
for fluent assertions: `YieldOutput<T>`, `Emit<TEvent>(count)`, `NotEmit<T>`,
`CompletedExecutors(...)`, `HaveNoErrors()`.

## What else is in here

Every remaining file is a **workflow-runtime-specific** helper the main fixture
composes. You rarely touch them directly, but they're here when you need to
drop below the fixture:

| Folder | Purpose |
| --- | --- |
| `Runtime/` | `TestRunContext`, `TestRunState`, `TestWorkflowContext`, `TestingExecutor<TIn,TOut>`, `ForwardMessageExecutor<T>`, `DynamicPortsExecutor<,>`, `ExecutionEnvironment`. The pieces you need if you want to unit-test an individual executor or edge runner without spinning up the full runtime. |
| `Agents/` | `TestRequestAgent` (paired/unpaired function-call interrupts), `RoleCheckAgent`. Unique — the host library's `FakeEchoAgent` / `FakeReplayAgent` cover the simpler cases. |
| `Assertions/` | `ValidationExtensions` (expression-tree `Moq.Verify` predicates for polymorphic types like `EdgeInfo`), `SubstitutionVisitor`, `MessageDeliveryValidation`, `SyntaxTreeFluentExtensions` (for workflow source-generator tests). |
| `Checkpointing/` | `InMemoryJsonStore` — `JsonCheckpointStore` backed by a `Dictionary`. Pair with `CheckpointManager.CreateInMemory()` when you want to inspect stored checkpoints by session. |
| `Declarative/` | `MockAgentProvider` — `Mock<ResponseAgentProvider>` with pre-canned messages and capture. Only needed for `Microsoft.Agents.AI.Workflows.Declarative` tests. |
| `Samples/` | Seven reference workflow shapes: sequential, conditional, loop, external-request, checkpointing, concurrent fan-out/fan-in, sub-workflow. Consume these from your own tests to verify the fixture against known-good flows. |
| `_build/` | `Directory.Build.props`, `Tests.Template.csproj`, `coverage.runsettings` — drop into your `tests/` folder and a new test project just works (xUnit v3 + MTP + FluentAssertions + xRetry). |
| `Tests/Quickstart.cs` | Two tests that cover the whole API. If you edit the fixture, run this first. |

## What noty does *not* ship

Because the host library (`ANcpLua.Roslyn.Utilities.Testing.AgentTesting`) already has richer versions:

- **Fake agents** (`FakeEchoAgent`, `FakeReplayAgent`, `FakeChatClient`, …) — use those.
- **HTTP fakes** (`FakeHttpMessageHandler` with URL-pattern matching, SSE, validators) — use it.
- **Chat-message builders** (`ChatMessageExtensions`) — use them.
- **Log assertions** (`LogAssert` over `FakeLogCollector`) — use them.
- **Kestrel in-memory hosting** (`WebTesting/KestrelTestBase`) — use it.
- **Output capture** (`TestOutputAdapter`) — use it.

Noty keeps only what the host library does not provide: the workflow runtime
contexts, the parametric executors, the expression-tree Moq validators, and the
one fixture that ties them together.

## Constraints

- C# 14, file-scoped namespaces, primary constructors.
- `TimeProvider.System.GetUtcNow()` — never `DateTime.UtcNow`.
- No `#pragma warning disable`, no `[SuppressMessage]`, no `<NoWarn>`.
- xUnit v3 + Microsoft Testing Platform (`xunit.v3.mtp-v2`).
