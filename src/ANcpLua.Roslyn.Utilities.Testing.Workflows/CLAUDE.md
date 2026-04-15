# CLAUDE.md - ANcpLua.Roslyn.Utilities.Testing.Workflows

## Scope

This file governs workflow test infrastructure under `src/ANcpLua.Roslyn.Utilities.Testing.Workflows`. It is the sibling package to `Testing.AgentTesting`: that one owns agent test doubles, this one owns workflow test doubles.

## What lives here

| Folder | Purpose |
|--------|---------|
| `Fixtures/` | `WorkflowFixture<TInput>` — the 3-line entry point (derive, override `BuildWorkflow`, assert) |
| `Runtime/` | In-memory `IRunnerContext` / `IWorkflowContext` / state types, scriptable `TestingExecutor`, `DynamicPortsExecutor`, `ForwardMessageExecutor`, `ExecutionExtensions` |
| `Agents/` | Workflow-oriented fake agents with session state — `TestRequestAgent`, `TestEchoAgent`, `TestReplayAgent`, `RoleCheckAgent` |
| `Assertions/` | Message delivery validation, syntax-tree fluent extensions, Moq expression validators, substitution visitor |
| `Checkpointing/` | `InMemoryJsonStore` — filesystem-free `JsonCheckpointStore` for tests |
| `Declarative/` | `MockAgentProvider` (unit-test variant, Mock&lt;ResponseAgentProvider&gt;) |
| `Framework/` | Declarative workflow integration-test harness — `Testcase` JSON schema, `WorkflowHarness`, `WorkflowTestSimple`, `WorkflowEvents`, `MockAgentProvider` (integration-test variant) |
| `Samples/` | 9 worked workflow examples (7 canonical + 2 upstream samples) |

## Core contracts

- **Deterministic output.** TimeProvider everywhere, no `DateTime.UtcNow`, no `Environment.TickCount`.
- **No filesystem.** In-memory checkpoint store by default. Tests that need persistence opt in.
- **Cohesion boundary.** This package owns workflow tests. Chat-client-level or agent-level tests belong in `Testing.AgentTesting`.

## Build policy

- `dotnet build -c Release`
- `dotnet pack -c Release`
- `dotnet test` opt-in only.

## Dependencies

- `Microsoft.Agents.AI.Workflows` (1.1.0) — includes `InProcessExecution`, `Checkpointing` namespaces
- `Microsoft.Agents.AI.Workflows.Declarative` (1.0.0-rc6) — declarative `DeclarativeWorkflowOptions`, `ResponseAgentProvider`, event types
- `Microsoft.Agents.AI` — `AIAgent`, `AgentSession`, `AgentRunOptions`, `AgentResponseUpdate`
- `Moq` (4.20.72) — used by `MockAgentProvider`
- `AwesomeAssertions` — the fluent assertion library in use repo-wide (MIT fork of FluentAssertions). **Never add `FluentAssertions` as a new package.** When porting code from upstream that uses FluentAssertions, replace the `using` with `using AwesomeAssertions;` — the API is identical.

## Harvest origin

Everything under `Runtime/`, `Agents/`, `Assertions/`, `Checkpointing/`, `Declarative/`, `Samples/`, and `Fixtures/WorkflowFixture.cs` originates from the `microsoft/agent-framework` test tree and was harvested into this repo via `~/Desktop/maftests/noty/`. Upstream namespace was `Microsoft.Agents.AI.Workflows.UnitTests` and `Noty.Workflows.Tests`. Remapped to `ANcpLua.Roslyn.Utilities.Testing.Workflows.<Subfolder>`.

`Framework/` originates from `~/Desktop/maftests/workflow-framework/`. The Azure Foundry-specific `IntegrationTest.cs` and `WorkflowTest.cs` are intentionally NOT ported — they pull internal-only packages (`Shared.IntegrationTests`, `Microsoft.Agents.ObjectModel`, `Microsoft.Agents.AI.Workflows.Declarative.PowerFx`) that aren't publicly available. A simpler variant may land later.

## Modernization rule

Favor tiny refactors. Don't widen behavioral scope accidentally. Preserve upstream semantics so upgrades from `microsoft/agent-framework` stay low-friction.
