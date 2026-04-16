# Analysis: MAF Type Visibility Audit & Namespace Drift Investigation

**Session date**: 2026-04-15 to 2026-04-16
**Model**: Claude Opus 4.6 (1M context)
**Purpose**: Research archive — captures investigation methodology, findings, and corrections. Not an action document; see `2026-04-16-restore-internal-test-infra.md` for the task handoff.

---

## 1. Investigation Origin

A parallel Claude agent working on `ANcpLua.Roslyn.Utilities.Testing.Workflows` reported:

> "The Declarative.Events namespace types aren't publicly accessible in 1.0.0-rc6. Stripping WorkflowEvents to base-Workflows-only events and parking WorkflowHarness."

The user asked me to verify this claim. The investigation escalated from a simple visibility check into a full audit of 8 categories of parked files, revealing a pattern of misdiagnosed compiler errors.

## 2. Methodology

### 2.1 Binary metadata inspection (ground truth)

Built a throwaway `dotnet run` project at `/tmp/rc6dump/` that reads type visibility directly from the shipped DLL metadata via `System.Reflection.Metadata`:

```csharp
using var fs = File.OpenRead(dllPath);
using var pe = new PEReader(fs);
var mr = pe.GetMetadataReader();
foreach (var th in mr.TypeDefinitions) {
    var td = mr.GetTypeDefinition(th);
    bool isPublic = (td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public;
    // ...
}
```

This is authoritative — it reads the actual IL metadata, not source code, not XML docs, not NuGet package descriptions. Every visibility claim in this document was verified this way against the cached packages at:
- `~/.nuget/packages/microsoft.agents.ai.workflows/1.1.0/lib/net10.0/Microsoft.Agents.AI.Workflows.dll`
- `~/.nuget/packages/microsoft.agents.ai.workflows.declarative/1.0.0-rc6/lib/net10.0/Microsoft.Agents.AI.Workflows.Declarative.dll`

### 2.2 Source cross-reference

Upstream source at `/Users/ancplua/compare-otel/agent-framework/dotnet/src/` was used to find file paths and read type definitions, but NEVER as the authority on visibility. The source is at an unknown commit relative to the shipped packages — types may have changed visibility between the shipped version and current `main`.

### 2.3 Namespace verification

For the namespace drift investigation, verified actual namespaces by reading the `namespace` declaration lines in source files, NOT by inferring from directory paths. This was the key insight — MAF puts files in `Events/` directories but declares parent namespaces.

## 3. Complete Type Visibility Table

### 3.1 Microsoft.Agents.AI.Workflows 1.1.0

| Type | Namespace | Visibility | Location |
|------|-----------|------------|----------|
| `Executor` | `Microsoft.Agents.AI.Workflows` | **PUBLIC** | root |
| `ProtocolBuilder` | `Microsoft.Agents.AI.Workflows` | **PUBLIC** | root |
| `AgentWorkflowBuilder` | `Microsoft.Agents.AI.Workflows` | **PUBLIC** | root |
| `AgentResponseEvent` | `Microsoft.Agents.AI.Workflows` | **PUBLIC** | root |
| `JsonCheckpointStore` | `Microsoft.Agents.AI.Workflows.Checkpointing` | **PUBLIC** | Checkpointing/ |
| `IRunnerContext` | `Microsoft.Agents.AI.Workflows.Execution` | internal | Execution/ |
| `ISuperStepJoinContext` | `Microsoft.Agents.AI.Workflows.Execution` | internal | Execution/ |
| `ISuperStepRunner` | `Microsoft.Agents.AI.Workflows.Execution` | internal | Execution/ |
| `StepContext` | `Microsoft.Agents.AI.Workflows.Execution` | internal | Execution/ |
| `IStepTracer` | `Microsoft.Agents.AI.Workflows.Execution` | internal | Execution/ |
| `EdgeMap` | `Microsoft.Agents.AI.Workflows.Execution` | internal | Execution/ |
| `MessageEnvelope` | `Microsoft.Agents.AI.Workflows.Execution` | internal | Execution/ |
| `IExternalRequestSink` | `Microsoft.Agents.AI.Workflows.Execution` | internal | Execution/ |
| `DeliveryMapping` | `Microsoft.Agents.AI.Workflows.Execution` | internal | Execution/ |
| `ExecutorIdentity` | `Microsoft.Agents.AI.Workflows.Execution` | internal | Execution/ |
| `IExternalRequestContext` | `Microsoft.Agents.AI.Workflows` | internal | root |
| `ExecutorInfo` | `Microsoft.Agents.AI.Workflows.Checkpointing` | internal | Checkpointing/ |
| `SessionCheckpointCache<T>` | `Microsoft.Agents.AI.Workflows.Checkpointing` | internal | Checkpointing/ |
| `WorkflowTelemetryContext` | `Microsoft.Agents.AI.Workflows.Observability` | internal | Observability/ |

### 3.2 Microsoft.Agents.AI.Workflows.Declarative 1.0.0-rc6

| Type | Namespace | Visibility | Location |
|------|-----------|------------|----------|
| `DeclarativeActionInvokedEvent` | `Microsoft.Agents.AI.Workflows.Declarative` | **PUBLIC** | Events/ |
| `DeclarativeActionCompletedEvent` | `Microsoft.Agents.AI.Workflows.Declarative` | **PUBLIC** | Events/ |
| `ConversationUpdateEvent` | `Microsoft.Agents.AI.Workflows.Declarative` | **PUBLIC** | Events/ |
| `ExternalInputResponse` | `Microsoft.Agents.AI.Workflows.Declarative.Events` | **PUBLIC** | Events/ |
| `ResponseAgentProvider` | `Microsoft.Agents.AI.Workflows.Declarative` | **PUBLIC** | root |
| `WorkflowFormulaState` | `Microsoft.Agents.AI.Workflows.Declarative.PowerFx` | internal | PowerFx/ |

### 3.3 InternalsVisibleTo grants in the upstream repo

From `src/Microsoft.Agents.AI.Workflows/Microsoft.Agents.AI.Workflows.csproj`:
```xml
<InternalsVisibleTo Include="Microsoft.Agents.AI.Workflows.UnitTests" />
<InternalsVisibleTo Include="Microsoft.Agents.AI.Workflows.Generators.UnitTests" />
```

From `src/Microsoft.Agents.AI.Workflows.Declarative/Microsoft.Agents.AI.Workflows.Declarative.csproj`:
```xml
<InternalsVisibleTo Include="Microsoft.Agents.AI.Workflows.Declarative.UnitTests" />
<InternalsVisibleTo Include="Microsoft.Agents.AI.Workflows.Declarative.IntegrationTests" />
```

Neither package carries InternalsVisibleTo for external consumers. NuGet packages never do.

## 4. The Namespace Drift Root Cause

### 4.1 The trap

In the upstream MAF source, several Declarative event types are stored in an `Events/` subdirectory:

```
src/Microsoft.Agents.AI.Workflows.Declarative/
  Events/
    DeclarativeActionInvokedEvent.cs
    DeclarativeActionCompletedEvent.cs
    ConversationUpdateEvent.cs
    ExternalInputResponse.cs
```

Intuition says: "files in the `Events/` folder should be in the `.Events` namespace." But three of the four types declare the **parent** namespace:

```csharp
// File: Events/DeclarativeActionInvokedEvent.cs
namespace Microsoft.Agents.AI.Workflows.Declarative;  // NOT .Events!
```

Only `ExternalInputResponse` actually uses the `.Events` namespace:
```csharp
// File: Events/ExternalInputResponse.cs
namespace Microsoft.Agents.AI.Workflows.Declarative.Events;  // This one IS in .Events
```

### 4.2 Why upstream tests work anyway

The upstream integration test project has namespace:
```csharp
namespace Microsoft.Agents.AI.Workflows.Declarative.IntegrationTests.Framework;
```

C# ambient namespace resolution walks UP the hierarchy:
1. `...Declarative.IntegrationTests.Framework` (self)
2. `...Declarative.IntegrationTests`
3. `...Declarative` ← **finds ConversationUpdateEvent, DeclarativeActionInvokedEvent, etc. here**
4. `...Workflows` ← **finds ExecutorInvokedEvent, AgentResponseEvent, etc. here**

Result: zero `using` directives needed. The upstream `WorkflowEvents.cs` has only `using System;`, `using System.Collections.Generic;`, `using System.Linq;` — no MAF usings at all.

### 4.3 Why the port broke

The port project's root namespace is `ANcpLua.Roslyn.Utilities.Testing.Workflows` — NOT a child of `Microsoft.Agents.AI.Workflows.Declarative`. Ambient resolution walks up `ANcpLua` → nothing MAF.

The harvested `using Microsoft.Agents.AI.Workflows.Declarative.Events;` imports ONLY `ExternalInputResponse` (the one type actually in that namespace). The other three Declarative event types remain unresolved.

Compiler emits **CS0246** ("type or namespace 'ConversationUpdateEvent' could not be found"). The previous agent read this as a visibility problem (CS0122) and concluded the types were internal. They are not.

### 4.4 The fix

Add a global using to the csproj:
```xml
<Using Include="Microsoft.Agents.AI.Workflows.Declarative" />
```

This simulates what upstream gets via ambient resolution. Applied in commit `bae4d33`.

## 5. Error Pattern: CS0246 Misdiagnosed as CS0122

The previous agent made the same error six times:

| Claimed diagnosis | Actual error | Actual root cause |
|---|---|---|
| `ConversationUpdateEvent` internal in rc6 | CS0246 | Missing using for parent namespace |
| `DeclarativeActionInvokedEvent` internal in rc6 | CS0246 | Same |
| `DeclarativeActionCompletedEvent` internal in rc6 | CS0246 | Same |
| `AgentResponseEvent` internal in rc6 | CS0246 | Type is in `Microsoft.Agents.AI.Workflows`, not `.Declarative.Events` |
| `ExternalInputResponse` internal in rc6 | CS0246 | Was actually accessible; agent never checked |
| `ResponseAgentProvider` non-public ctor | CS0246 | Missing using for `Microsoft.Agents.AI.Workflows.Declarative` |

**The structural difference**: CS0246 = "I can't find this name anywhere in scope" (namespace/using issue). CS0122 = "I found it but its protection level prevents access" (visibility issue). These have different root causes and different fixes. The agent treated them as identical.

**Contributing factor**: the agent was operating during an Anthropic outage (14:53–16:01 UTC on 2026-04-15, per status.anthropic.com). It may have been degraded or rushed. The user retried 3x before the session stabilized.

## 6. The Other Agent's Post-Mortem (and its errors)

After I flagged the visibility problem, the other agent issued a detailed 8-category post-mortem. Assessment:

| Category | Agent's diagnosis | Verified? |
|---|---|---|
| 1. Runtime (IRunnerContext etc.) | All types internal | **CORRECT** — all 10 confirmed internal |
| 2. Checkpointing (InMemoryJsonStore) | SessionCheckpointCache internal + API evolved | **CORRECT** |
| 3. Assertions (DeliveryMapping etc.) | Types internal | **CORRECT** |
| 4. MockAgentProvider "non-public ctor" | ResponseAgentProvider has non-public ctor | **WRONG** — public abstract class with implicit public ctor |
| 5. Declarative.Events types internal | Internal in rc6 | **WRONG** — all public, proved via DLL inspection |
| 6. Azure Foundry (Shared.IntegrationTests) | Microsoft-internal packages | **CORRECT** |
| 7. Samples: AgentWorkflowBuilder internal | AgentWorkflowBuilder is internal | **WRONG** — public static partial class |
| 8. ConformanceTestBase (Hosting.OpenAI) | Namespace mismatch in alpha package | **PLAUSIBLE** but unverified |

The agent then issued a "Meta" table claiming certain types were "actually public — my 'internal' claim was fabricated." Four of those flips were themselves wrong:

| Type agent flipped to "public" | Actual visibility in shipped DLL |
|---|---|
| `WorkflowTelemetryContext` | **internal** (agent was originally correct) |
| `ISuperStepRunner` | **internal** (agent was originally correct) |
| `ExecutorIdentity` | **internal** (agent was originally correct) |
| `WorkflowFormulaState` | **internal** (agent was originally correct) |

The agent over-corrected: when caught being wrong about Declarative events, it flipped too many types to "public" to appear thorough. The real error surface was smaller than the correction implied.

## 7. Version Landscape

| Package | Version pinned | Source of truth |
|---|---|---|
| `Microsoft.Agents.AI` | 1.1.0 | `~/ANcpLua.Roslyn.Utilities/Version.props` |
| `Microsoft.Agents.AI.Workflows` | 1.1.0 | same |
| `Microsoft.Agents.AI.Workflows.Declarative` | 1.0.0-rc6 | same |

The upstream source at `/Users/ancplua/compare-otel/agent-framework/dotnet` is at an unknown commit relative to these shipped versions. Samples in that repo reference `1.0.0-rc4`. There is no `rc6` reference anywhere in that repo — rc6 is only the Declarative package on nuget.org.

The upstream `main` branch shows all Declarative event types as `public sealed` — consistent with the shipped rc6 DLL. The internal types in Workflows are also consistent between source and shipped 1.1.0.

## 8. Architectural Insight: Test Code vs Production Code

The user's key pushback on my initial deletion approach:

> "Why would test code be updated? Its testing code LOL"
> "Why wouldn't we copy Microsoft.Agents.AI.Workflows.Testing when its the only way we can test our code in qyl?"

This is correct. The "layer violation" and "maintenance burden" arguments I made apply to production library code, not test infrastructure:

1. **Version pin control**: The port pins to Workflows 1.1.0. Internal types don't change until the user bumps. When they do, they diff and update — bounded cost.
2. **Test infra SHOULD reach into internals**: That's literally what `InternalsVisibleTo` is for. Microsoft's own test projects use it. Since the NuGet doesn't grant it, copying the types is the correct alternative.
3. **No public testing package exists**: Microsoft doesn't ship `Microsoft.Agents.AI.Workflows.Testing`. Until they do, the only way to unit-test executor message routing is to copy the internal types.

## 9. What Was Committed

| Commit | What | State |
|---|---|---|
| `bae4d33` | Deleted 18 parked files, unparked ForwardMessageExecutor + MockAgentProvider, restored WorkflowEvents + WorkflowHarness with Declarative projections + RunTestcaseAsync | Partially reverted by other agent |
| `8959bc5` (amended) | Handoff document for restoration task + chained MAF skill leaves | Active |

The WorkflowEvents.cs and WorkflowHarness.cs files were subsequently reverted to their stripped versions by the other agent working in a parallel session. The Declarative event projections and RunTestcaseAsync method will need to be re-applied after the restore.

## 10. Lessons for Future Agents

1. **Never infer visibility from compiler error messages without reading the error code.** CS0246 (not found) and CS0122 (inaccessible) have different root causes.
2. **Directory path != namespace** in MAF. Always read the `namespace` declaration in the file, never assume from the folder structure.
3. **Verify against shipped binaries, not source.** The source may be ahead of the shipped package. Use `~/.nuget/packages/<id>/<version>/lib/<tfm>/<assembly>.dll` with `System.Reflection.Metadata` for ground truth.
4. **Test code has different rules than production code.** Copying internal types into a test project is acceptable when the version is pinned and no public testing surface exists.
5. **When an agent's diagnosis is wrong, its self-correction may also be wrong.** Over-correction (flipping too many items) is a common pattern. Verify each correction independently.
6. **Global usings in csproj can simulate ambient namespace resolution.** When porting code from a project whose namespace hierarchy provided implicit resolution, a single `<Using Include="...">` in the csproj replaces what the original project got for free.