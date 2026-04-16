# Handoff: Restore Internal MAF Test Infrastructure

**Created**: 2026-04-16 03:45 UTC
**Project**: `/Users/ancplua/ANcpLua.Roslyn.Utilities`
**Branch**: `main`
**Last commit**: `bae4d33` — refactor(testing.workflows): remove parked harvest, fix namespace drift

## Current State Summary

Commit `bae4d33` deleted 18 files from `src/ANcpLua.Roslyn.Utilities.Testing.Workflows/` that were previously `<Compile Remove>`'d. Two files were correctly unparked (ForwardMessageExecutor, MockAgentProvider) and two were restored with namespace-drift fixes (WorkflowEvents, WorkflowHarness). The deletion was premature — the user wants ALL 18 files restored and made compilable by copying the required internal type definitions from the upstream MAF source.

**The user's directive**: "do it right this time" — restore every deleted file, copy internal type signatures from upstream, add Microsoft.CodeAnalysis to the csproj, and convert partial-class samples to manual ConfigureProtocol overrides. Test infrastructure should reach into internals. This is test code, not production code.

## Important Context

### Two repos involved

| Repo | Path | Role |
|------|------|------|
| Upstream MAF source | `/Users/ancplua/compare-otel/agent-framework/dotnet` | Source of truth for internal type definitions |
| Port project | `/Users/ancplua/ANcpLua.Roslyn.Utilities` | Consumer via NuGet (Workflows 1.1.0, Declarative 1.0.0-rc6) |

### Original harvested files backup

The user has a backup of all original harvested files at `~/Desktop/maftests/`. This contains the pre-port versions before any modifications.

### Version pins (from `/Users/ancplua/ANcpLua.Roslyn.Utilities/Version.props`)

```
Microsoft.Agents.AI = 1.1.0
Microsoft.Agents.AI.Workflows = 1.1.0
Microsoft.Agents.AI.Workflows.Declarative = 1.0.0-rc6
```

### Namespace drift root cause (CRITICAL — discovered in this session)

The upstream integration test project (`Microsoft.Agents.AI.Workflows.Declarative.IntegrationTests`) works with ZERO explicit MAF usings because its namespace is a child of `Microsoft.Agents.AI.Workflows.Declarative` — C# ambient namespace resolution finds all types up the hierarchy. The port project's root namespace is `ANcpLua.Roslyn.Utilities.Testing.Workflows` — NOT in the MAF tree — so ambient lookup fails.

**Fix already applied**: The csproj now has `<Using Include="Microsoft.Agents.AI.Workflows.Declarative" />` as a global using. This resolves all Declarative types. For `.Events` sub-namespace types (like `ExternalInputResponse`), per-file usings are still needed.

### DLL-verified type visibility (from this session's metadata inspection)

**Confirmed INTERNAL in shipped Workflows 1.1.0 DLL** (these need to be copied):
- `IRunnerContext` (Execution/)
- `ISuperStepJoinContext` (Execution/)
- `ISuperStepRunner` (Execution/)
- `StepContext` (Execution/)
- `IStepTracer` (Execution/)
- `EdgeMap` (Execution/)
- `MessageEnvelope` (Execution/)
- `IExternalRequestSink` (Execution/)
- `IExternalRequestContext` (root)
- `DeliveryMapping` (Execution/)
- `ExecutorIdentity` (Execution/)
- `ExecutorInfo` (Checkpointing/)
- `SessionCheckpointCache<T>` (Checkpointing/)
- `WorkflowTelemetryContext` (Observability/)

**Confirmed PUBLIC in shipped DLLs** (no copying needed):
- `Executor`, `ProtocolBuilder`, `AgentWorkflowBuilder` (Workflows 1.1.0)
- `JsonCheckpointStore`, `CheckpointManager`, `CheckpointInfo` (Workflows 1.1.0)
- `ResponseAgentProvider`, `DeclarativeWorkflowBuilder` (Declarative rc6)
- All event types: `ConversationUpdateEvent`, `DeclarativeActionInvokedEvent`, `DeclarativeActionCompletedEvent`, `AgentResponseEvent`, `ExternalInputResponse`, `RequestInfoEvent`, `ExecutorInvokedEvent`, `ExecutorCompletedEvent`, `SuperStepCompletedEvent`, `WorkflowErrorEvent`, `WorkflowOutputEvent`

### What qyl needs from this

qyl uses MAF Workflows for agent orchestration (see: `Qyl.Agents.Abstractions`, `Qyl.Agents.Generator`, `Qyl.Agents`). The Testing.Workflows project is the test harness. Without the internal types, qyl cannot:
- Unit-test individual executor message routing
- Validate messages arrive at correct executor ports
- Write custom test runners with step-by-step execution control
- Build parametric executor tests

## Files to Restore

### Step 1: Restore from git

```bash
cd /Users/ancplua/ANcpLua.Roslyn.Utilities
git checkout bae4d33^ -- \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Runtime/TestRunContext.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Runtime/TestWorkflowContext.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Runtime/TestRunState.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Runtime/TestingExecutor.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Runtime/DynamicPortsExecutor.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Checkpointing/InMemoryJsonStore.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Assertions/MessageDeliveryValidation.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Assertions/ValidationExtensions.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Assertions/SyntaxTreeFluentExtensions.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Samples/01_Sequential.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Samples/02_Conditional.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Samples/03_Loop.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Samples/04_ExternalRequest.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Samples/05_Checkpointing.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Samples/06_Concurrent.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Samples/07_Subworkflow.cs \
  src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Samples/Quickstart.cs
```

### Step 2: Copy internal type definitions from upstream

Source: `/Users/ancplua/compare-otel/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/`

Create a new folder `src/ANcpLua.Roslyn.Utilities.Testing.Workflows/Internals/` to hold copied internal types. Each file should:
- Keep the original copyright header
- Add a comment: `// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)`
- Change namespace to `ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals`
- Make the type `internal` (it already is, but be explicit)

Types to copy (find them via grep in the upstream source):

| Type | Source path (under src/Microsoft.Agents.AI.Workflows/) |
|------|------|
| `IRunnerContext` | `Execution/IRunnerContext.cs` |
| `ISuperStepJoinContext` | `Execution/ISuperStepJoinContext.cs` |
| `ISuperStepRunner` | `Execution/ISuperStepRunner.cs` |
| `StepContext` | `Execution/StepContext.cs` |
| `IStepTracer` | `Execution/IStepTracer.cs` |
| `EdgeMap` | `Execution/EdgeMap.cs` |
| `MessageEnvelope` | `Execution/MessageEnvelope.cs` |
| `IExternalRequestSink` | `Execution/IExternalRequestSink.cs` |
| `IExternalRequestContext` | `IExternalRequestContext.cs` |
| `DeliveryMapping` | `Execution/DeliveryMapping.cs` |
| `ExecutorIdentity` | `Execution/ExecutorIdentity.cs` |
| `ExecutorInfo` | `Checkpointing/ExecutorInfo.cs` |
| `SessionCheckpointCache<T>` | `Checkpointing/SessionCheckpointCache.cs` |
| `WorkflowTelemetryContext` | `Observability/WorkflowTelemetryContext.cs` |

**IMPORTANT**: These types may reference OTHER internal types transitively. You MUST do a transitive dependency crawl:
1. Copy a type
2. Try to build
3. If CS0246/CS0122, find the missing type in upstream source
4. Copy it too
5. Repeat until clean

### Step 3: Fix InMemoryJsonStore

The `JsonCheckpointStore` abstract method signatures evolved in 1.1.0. You need to:
1. Read the current `JsonCheckpointStore` public API from the shipped DLL XML docs at `~/.nuget/packages/microsoft.agents.ai.workflows/1.1.0/lib/net10.0/Microsoft.Agents.AI.Workflows.xml`
2. Update `InMemoryJsonStore.cs` to implement the current abstract signatures
3. Also handle `SessionCheckpointCache<T>` (now in your copied internals)

### Step 4: Convert Samples to manual ConfigureProtocol

The 8 samples use `partial class` + source generator. Convert each to manual `ConfigureProtocol` override:

```csharp
// BEFORE (generator pattern — doesn't work without internal generator):
public partial class MyExecutor(string id) : Executor(id)
{
    // Generator emits: protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder pb) { ... }
}

// AFTER (manual pattern — matches Simple_Sequential.cs):
public class MyExecutor(string id) : Executor(id)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        // Copy the protocol configuration from the generator output or upstream test
        return protocolBuilder;
    }
}
```

Look at `Simple_Sequential.cs` for the canonical pattern. Each sample's ConfigureProtocol body should match what the upstream source generator would emit — you can infer this from the executor's message handler registrations and port declarations.

### Step 5: Add Microsoft.CodeAnalysis to csproj

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
```

(Already in CPM — check `Directory.Packages.props` for the version variable.)

This unblocks `SyntaxTreeFluentExtensions.cs`.

### Step 6: Build and iterate

```bash
dotnet build src/ANcpLua.Roslyn.Utilities.Testing.Workflows/ANcpLua.Roslyn.Utilities.Testing.Workflows.csproj -c Release --tl:off
```

The transitive dependency crawl (Step 2) will likely require 3-5 build-fix-build cycles. Trust the compiler errors — each CS0246 tells you exactly which type is missing.

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| Copy internal types rather than delete dependent files | Test infrastructure SHOULD reach into internals. The port is version-pinned to 1.1.0 — internal types don't change until the pin is bumped. |
| Put copies in `Internals/` subfolder | Clear provenance. Easy to diff against upstream on version bump. |
| Manual ConfigureProtocol over generator shimming | The source generator is internal to MAF's build. Manual overrides are explicit and don't depend on infrastructure we don't ship. |
| Keep `Microsoft.CodeAnalysis` in test project | Base Testing project already references it. No layer violation for test code. |
| CS1591 stays suppressed | Too many undocumented upstream-ported members. Document incrementally. |

## Key Patterns Discovered

1. **Namespace != directory** in MAF: Declarative event types live in `/Events/` folder but declare `namespace Microsoft.Agents.AI.Workflows.Declarative;` (parent). This causes CS0246 if you use `using ...Declarative.Events;` expecting them there.

2. **Global using fix**: `<Using Include="Microsoft.Agents.AI.Workflows.Declarative" />` in csproj simulates what upstream gets via child-namespace ambient resolution.

3. **CS0246 vs CS0122**: The previous agent conflated "type not found" (namespace issue) with "inaccessible protection level" (visibility issue). Always read the actual error code.

4. **Upstream test code relies heavily on InternalsVisibleTo**: The upstream repo grants `[InternalsVisibleTo("Microsoft.Agents.AI.Workflows.UnitTests")]` — their test code freely uses internal types. Our port must copy those types since the NuGet package doesn't carry InternalsVisibleTo for external consumers.

## Potential Gotchas

- **Transitive internal dependencies**: Copying `IRunnerContext` may pull in 5+ other internal types. Don't guess — let the compiler tell you.
- **API shape drift between source and shipped 1.1.0**: The upstream `main` branch may have diverged past 1.1.0. Always verify against the SHIPPED DLL/XML, not current source. Use `~/.nuget/packages/microsoft.agents.ai.workflows/1.1.0/` as ground truth.
- **The csproj was modified by the user** after my last commit — `Microsoft.Agents.AI` PackageReference line appears to have been edited. Read the csproj fresh before making changes.
- **Sample 06 had an additional issue**: referenced `ANcpLua.Roslyn.Utilities.Testing.AgentTesting` namespace, creating a reverse dependency. May need a using alias or project reference.
- **`Executor.ConfigureProtocol` is `protected abstract`**: The manual override must be `protected override`. The `ProtocolBuilder` fluent API methods like `RouteBuilder.AddHandler<T>()`, `SendsMessage<T>()`, `ReceivesMessage<T>()` are all public.

## Verification

After all changes:
```bash
dotnet build src/ANcpLua.Roslyn.Utilities.Testing.Workflows/ANcpLua.Roslyn.Utilities.Testing.Workflows.csproj -c Release --tl:off
# Must show: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet build ANcpLua.Roslyn.Utilities.slnx -c Release --tl:off
# Full solution must also pass
```

## Additional Files to Port (not in the 18 deleted, but missing from the project)

The user has a curated catalog at `~/Desktop/maftests/README.md` — read it for full context. Three files from that catalog are NOT in the 18-file restore but SHOULD be ported:

### ChatMessageBuilder.cs (from `~/Desktop/maftests/engopus/`)

Fluent extensions for building test message data: `string.ToContentStream()`, `string.ToAgentRunStream()`, `IEnumerable<AIContent>.ToChatMessage()`, `ChatMessage.StreamMessage()`. No internal deps, pure utility. The original agent dropped it as "qyl-specific" but it's generic. Port to `Testing.Workflows/Framework/` alongside WorkflowHarness.

### WorkflowTest.cs assertion helpers (from `~/Desktop/maftests/workflow-framework/`)

`AssertWorkflow` with sub-checks: `CheckConversation`, `CheckEventCounts`, `CheckResponses`, `CheckEventSequence`, `CheckMessagesAsync`. The file itself inherits from `IntegrationTest` which has Azure Foundry dependencies — extract ONLY the assertion methods into a standalone `WorkflowAssertions` static class or extension methods on `WorkflowEvents`. Drop the Foundry-specific wiring.

### IntegrationTest.cs base configuration (from `~/Desktop/maftests/workflow-framework/`)

Pattern: `IConfiguration` from json + env + UserSecrets, TestOutputAdapter wiring (Console.SetOut redirect to xUnit ITestOutputHelper), DeclarativeWorkflowOptions factory. The Azure credentials parts (`TestAzureCliCredentials`, `ProductContext.SetContext`) are droppable. Extract the configuration + output-adapter pattern into the `WorkflowFixture` or a new `WorkflowTestBase`.

### Reference file

The full catalog is at `~/Desktop/maftests/README.md` — the next agent should read it for the "TL;DR — three things I'd copy first" prioritization and the per-file descriptions.

## Chained Task: MAF SKILL.md Post-Merge Leaves

**After the restore is complete and builds green**, execute the 9 leaf edits from the MAF skill handoff at `~/qyl/.claude/handoffs/2026-04-15-174757-maf-skill-post-merge-leaves.md`.

**Target file changed**: the SKILL.md is now at `~/.claude/skills/microsoft-agent-framework/SKILL.md` (global, 1258 lines), NOT the qyl-local path. The qyl overlay at `~/qyl/.claude/skills/microsoft-agent-framework/SKILL.md` (164 lines) is done and does not need edits.

**Critical dependency**: L6 (new §12.8 Workflow testing) documents the Testing.Workflows project surface. It MUST be written AFTER the restore completes so it describes the actual shipped state — all 18 files restored, `Internals/` folder with copied types, manual ConfigureProtocol samples, Microsoft.CodeAnalysis reference for SyntaxTreeFluentExtensions.

### Execution order

1. **First**: complete all 6 restore steps above (git checkout → internal types → InMemoryJsonStore → samples → Microsoft.CodeAnalysis → build green)
2. **Then**: read the full MAF handoff at `~/qyl/.claude/handoffs/2026-04-15-174757-maf-skill-post-merge-leaves.md`
3. **Execute L1-L5, L7-L9** against `~/.claude/skills/microsoft-agent-framework/SKILL.md` (these don't depend on the restore)
4. **Execute L6** (§12.8 Workflow testing) last — write it based on the actual restored file inventory, not the plan

### L6 content guidance (updated post-restore)

§12.8 should document the full Testing.Workflows surface including:
- `Internals/` folder: copied internal MAF types with version-pin discipline (diff on bump)
- `Runtime/TestRunContext`, `TestWorkflowContext`, `TestRunState` — runner context mocks
- `Runtime/TestingExecutor`, `DynamicPortsExecutor`, `ForwardMessageExecutor` — test executors
- `Runtime/ExecutionEnvironment` — parametric test axis
- `Checkpointing/InMemoryJsonStore` — in-memory checkpoint store
- `Assertions/MessageDeliveryValidation`, `ValidationExtensions` — message routing validation
- `Assertions/SyntaxTreeFluentExtensions` — Roslyn assertion helpers
- `Assertions/SubstitutionVisitor` — AST rewriting for test verification
- `Framework/WorkflowHarness`, `WorkflowEvents`, `Testcase` — integration test harness
- `Fixtures/WorkflowFixture` — primary entry point
- `Declarative/MockAgentProvider` — Moq-based ResponseAgentProvider
- `Samples/Simple_Sequential` through `07_Subworkflow` — reference executors
- `Agents/TestEchoAgent`, `TestReplayAgent`, `TestRequestAgent`, `RoleCheckAgent` — workflow-level agent doubles

qyl callout: "qyl.loom workflow tests should use `WorkflowFixture` + `WorkflowHarness` to assert on emitted events without standing up a full host. For executor-level unit tests that verify message routing, use `TestRunContext` + `EdgeMap` from the `Internals/` folder."

## Related Context

- qyl projects that consume this: `Qyl.Agents.Abstractions`, `Qyl.Agents.Generator`, `Qyl.Agents`
- Loom compiler also uses MAF Workflows
- The `microsoft-agent-framework` skill has MAF consumption patterns for qyl
- The `ancplua-roslyn-utilities` skill catalogs the Testing project's API surface
- MAF skill handoff with full 9-leaf spec: `~/qyl/.claude/handoffs/2026-04-15-174757-maf-skill-post-merge-leaves.md`
