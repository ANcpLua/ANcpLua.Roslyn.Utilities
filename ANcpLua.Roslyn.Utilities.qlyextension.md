

ANcpLua.Roslyn.Utilities interesting for qyl

## 1. Attributes

### Full Ecosystem Attribute Inventory (38 total across 5 repos)

#### QYL Instrumentation — Marker Attributes (12)

| Attribute | Namespace | Zweck |
|-----------|-----------|-------|
| `[Traced]` | `Qyl.Instrumentation.Instrumentation` | Klasse/Methode für OTel-Tracing markieren |
| `[NoTrace]` | `Qyl.Instrumentation.Instrumentation` | Methode von Class-Level-Tracing ausschließen |
| `[AgentTraced]` | `Qyl.Instrumentation.Instrumentation` | Agent-Invocation-Tracing |
| `[Counter]` | `Qyl.Instrumentation.Instrumentation` | Counter-Metrik |
| `[Histogram]` | `Qyl.Instrumentation.Instrumentation` | Histogram-Metrik |
| `[UpDownCounter]` | `Qyl.Instrumentation.Instrumentation` | Up-Down-Counter-Metrik |
| `[Gauge]` | `Qyl.Instrumentation.Instrumentation` | Gauge-Metrik |
| `[Meter]` | `Qyl.Instrumentation.Instrumentation` | Meter-Klasse markieren |
| `[Tag]` | `Qyl.Instrumentation.Instrumentation` | Parameter als Span-Tag |
| `[TracedTag]` | `Qyl.Instrumentation.Instrumentation` | Expliziter Span-Tag |
| `[TracedReturn]` | `Qyl.Instrumentation.Instrumentation` | Return-Value als Span-Attribut |
| `[OTel]` | `Qyl.Instrumentation.Instrumentation` | Semconv-Key-Supplier |

Generator: `qyl/src/qyl.instrumentation.generators/`

#### QYL Loom Workflow — Marker Attributes (9)

| Attribute | Namespace | Zweck |
|-----------|-----------|-------|
| `[LoomTool]` | `Qyl.Instrumentation.Instrumentation.Loom` | Methode als Workflow-Tool |
| `[LoomContract]` | `Qyl.Instrumentation.Instrumentation.Loom` | Typ als Workflow-Contract |
| `[LoomStep]` | `Qyl.Instrumentation.Instrumentation.Loom` | Klasse als Workflow-Step |
| `[LoomWorkflow]` | `Qyl.Instrumentation.Instrumentation.Loom` | Klasse als kompletter Workflow |
| `[RequiresCapability]` | `Qyl.Instrumentation.Instrumentation.Loom` | Benötigte Capabilities deklarieren |
| `[RequiresApproval]` | `Qyl.Instrumentation.Instrumentation.Loom` | Approval-Gate |
| `[ToolSideEffect]` | `Qyl.Instrumentation.Instrumentation.Loom` | Side-Effects deklarieren |
| `[EmitsStructuredOutput]` | `Qyl.Instrumentation.Instrumentation.Loom` | Structured-Output-Typ |
| `[LoomBudget]` | `Qyl.Instrumentation.Instrumentation.Loom` | Execution-Budget setzen |

Source: `qyl/src/qyl.instrumentation/Instrumentation/Loom/LoomAttributes.cs`

#### QYL DuckDB Storage — Marker Attributes (3)

| Attribute | Namespace | Zweck |
|-----------|-----------|-------|
| `[DuckDbTable]` | `Qyl.Collector.Storage` | Typ für DuckDB-Helpers generieren |
| `[DuckDbColumn]` | `Qyl.Collector.Storage` | Property → DuckDB-Column-Mapping |
| `[DuckDbIgnore]` | `Qyl.Collector.Storage` | Property von Mapping ausschließen |

Generator: `qyl/src/qyl.collector.storage.generators/DuckDbInsertGenerator.cs`
Attributes emitted via PostInitializationOutput from `DuckDbAttributes.cs`

#### Netagents MCP — Marker Attributes (4)

| Attribute | Namespace | Zweck |
|-----------|-----------|-------|
| `[McpServer]` | `Qyl.Agents` | Partial Class als MCP-Server |
| `[Tool]` | `Qyl.Agents` | Methode als AI-callable Tool |
| `[Prompt]` | `Qyl.Agents` | Methode als MCP-Prompt |
| `[Resource]` | `Qyl.Agents` | Methode als MCP-Resource |

Generator: `netagents/src/Qyl.Agents.Generator/McpServerGenerator.cs`

#### QYL Contracts — Generated Semconv Output (3)

| Klasse | Namespace | Semconv |
|--------|-----------|---------|
| `DbAttributes` | `qyl.contracts.Attributes` | OTel 1.40.0 Database |
| `GenAiAttributes` | `qyl.contracts.Attributes` | OTel 1.40.0 GenAI |
| `McpAttributes` | `qyl.contracts.Attributes` | OTel 1.40.0 MCP |

Source: `qyl/src/qyl.contracts/Attributes/`

#### ANcpLua.Helpers.Utilities — AotReflection (7)

| Attribute / Typ | Rolle | Zweck |
|-----------------|-------|-------|
| `[AotReflection]` | Marker | Triggert den Generator |
| `ClassMetadata` | Generated | Klassen-Reflection-Daten |
| `MethodMetadata` | Generated | Methoden-Reflection-Daten |
| `PropertyMetadata` | Generated | Property-Reflection-Daten |
| `FieldMetadata` | Generated | Field-Reflection-Daten |
| `ConstructorMetadata` | Generated | Konstruktor-Reflection-Daten |
| `ParameterMetadata` | Generated | Parameter-Reflection-Daten |

Generator: `ANcpLua.Helpers.Utilities/src/ANcpLua.AotReflection/AotReflectionGenerator.cs`

### Other attributes in this repo

**Custom attribute definitions:**
- `src/ANcpLua.AotReflection.Attributes/AotReflectionAttribute.cs` — marks types for AOT metadata generation
- `src/ANcpLua.Roslyn.Utilities.Testing.Aot/` — `AotTestAttribute`, `AotSafeAttribute`, `AotUnsafeAttribute`, `TrimSafeAttribute`, `TrimTestAttribute`, `TrimUnsafeAttribute`
- `src/ANcpLua.Roslyn.Utilities.Testing.AgentTesting/BitNetAttribute.cs` — marks tests needing BitNet fixture

**Attribute analysis/extraction:**
- `src/ANcpLua.Roslyn.Utilities/AttributeExtensions.cs` — GetConstructorArgument, GetNamedArgument from AttributeData
- `src/ANcpLua.Roslyn.Utilities/SymbolExtensions.cs` — HasAttribute, GetAttribute, GetAttributes

**Metadata models (runtime reflection):**
- `src/ANcpLua.AotReflection.Attributes/` — `ClassMetadata`, `PropertyMetadata`, `MethodMetadata`, `FieldMetadata`, `ConstructorMetadata`, `ParameterMetadata`

**Polyfills (20+ netstandard2.0 backports):**
- `Polyfills/TrimAttributes/` — DynamicallyAccessedMembers, RequiresUnreferencedCode, RequiresDynamicCode
- `Polyfills/LanguageFeatures/` — RequiredMember, CallerArgumentExpression, CompilerFeatureRequired, ParamCollection
- `Polyfills/DiagnosticAttributes/` — NullableAttributes, MemberNotNull, StackTraceHidden, ExperimentalAttribute

## 2. OpenTelemetry

**Core analysis infrastructure:**
- `src/ANcpLua.Roslyn.Utilities/Contexts/OTelContext.cs` — cached type symbols for Activity, ActivitySource, Meter, Counter, Histogram, TracerProvider, MeterProvider, etc.
- `src/ANcpLua.Roslyn.Utilities/Contexts/DeprecatedOtelAttributes.cs` — 50+ deprecated semconv attribute mappings (v1.40.0)
- `src/ANcpLua.Roslyn.Utilities/Models/OTelEnums.cs` — SpanKind, SpanStatusCode matching OTel protobuf spec

**Instrumentation helpers (in Testing):**
- `Testing/Instrumentation/ActivityInstrumentation.cs` — ActivitySourceFactory, 100+ semconv constants, ScopedActivity, GenAI attributes
- `Testing/Instrumentation/MetricsInstrumentation.cs` — MeterFactory, MetricUnits, DurationRecorder, ActiveTracker
- `Testing/Instrumentation/LoggingConventions.cs` — standardized EventId ranges, 100+ LogTags constants
- `Testing/Instrumentation/LogEnricherInfrastructure.cs` — TraceContextEnricher, GenAiContextEnricher
- `Testing/Instrumentation/DataClassificationHelpers.cs` — PII/Secret redaction framework
- `src/ANcpLua.Roslyn.Utilities/Telemetry/AssemblyVersionExtensions.cs` — version extraction for telemetry

**No qyl code lives in this repo.** The `Qyl.Agents` references in `workflows/nuget-publish.yml` and `ActivityCollector` docs point to an external repo.

## 3. MCP

**No MCP implementation code.** Only `.mcp.json` configuring client connections (JetBrains, NuGet MCP servers). No ModelContextProtocol library references in any csproj.

## 4. Agents

**Full agent testing library** (`src/ANcpLua.Roslyn.Utilities.Testing.AgentTesting/`):
- `AGUITestServer.cs` — in-memory test server with AG-UI endpoint
- `FakeAgentBase.cs`, `FakeEchoAgent.cs`, `FakeTextStreamingAgent.cs`, `FakeMultiMessageAgent.cs`, `FakeReplayAgent.cs`, `DelegatingTestAgent.cs` — test doubles for Microsoft.Agents.AI
- `FakeChatClient.cs` — IChatClient test double with scripted responses
- `FakeHttpMessageHandler.cs` — URL-pattern matching HTTP handler with SSE support
- `SseResponseParser.cs` — SSE response parsing
- `ActivityCollector.cs` — captures Activity instances for telemetry assertions
- `BitNetFixture.cs` / `BitNetTestGroup.cs` — llama.cpp/BitNet LLM test infrastructure
- `ChatMessageExtensions.cs`, `AsyncEnumerableExtensions.cs` — message builders and async helpers
- `TestOutputAdapter.cs`, `LogRecord.cs`, `RecordedRequest.cs` — test output/logging infrastructure

**Dependencies:** Microsoft.Agents.AI, Microsoft.Agents.AI.Hosting.AGUI.AspNetCore, Microsoft.Extensions.AI, OpenAI, xunit.v3

## AotReflection — Generator-Attributes Architecture

**Roslyn source generator that replaces runtime reflection with compile-time generated metadata.** The whole point is AOT/trimming compatibility: `typeof(T).GetProperties()` and friends get trimmed away or fail in NativeAOT, so this generator does the work at compile time instead.

### Generator (`src/ANcpLua.AotReflection/`)

- `AotReflectionGenerator.cs` — the `[Generator]` entry point (IIncrementalGenerator)
- `DiagnosticDescriptors.cs` — analyzer diagnostics for the generator

**Extractors** (compile-time symbol analysis → immutable models):

| Extractor | Output Model | What it extracts |
|-----------|-------------|-----------------|
| `TypeExtractor` | `TypeModel` | Type metadata (name, namespace, accessibility, generics) |
| `PropertyExtractor` | `PropertyModel` | Properties with getter/setter info |
| `MethodExtractor` | `MethodModel` | Methods with parameters and return types |
| `FieldExtractor` | `FieldModel` | Fields with type and default values |
| `ConstructorExtractor` | `ConstructorModel` | Constructors with parameters |
| `ParameterExtractor` | `ParameterModel` | Parameter types, defaults, attributes |
| `DeclarationChainExtractor` | `TypeDeclarationModel` | Nesting chain for partial types |
| `LiteralFormatter` | (helper) | Default value literal formatting |

**Code Generators** (models → emitted C# source):

| Generator | Emits |
|-----------|-------|
| `ClassMetadataGenerator` | Orchestrates full output, top-level structure |
| `PropertyCodeGenerator` | Getter/setter delegate expressions |
| `MethodCodeGenerator` | Invoker delegate expressions |
| `FieldCodeGenerator` | Field access delegate expressions |
| `OutputGenerator` | Final `.g.cs` file assembly |
| `GenerationHelpers` | Shared emit logic (indent, escaping, types) |

**Models** (value-equatable for incremental caching):
- `TypeModel`, `PropertyModel`, `MethodModel`, `FieldModel`, `ConstructorModel`, `ParameterModel`
- `TypeDeclarationModel` — nesting chain
- `AotReflectionOptions` — controls what gets included (properties, methods, fields, constructors, inherited, private)
- `ReturnKind` — void/value/task/valuetask classification
- `AccessibilityExtensions` — maps Roslyn accessibility to C# keywords

### Attributes Contract (`src/ANcpLua.AotReflection.Attributes/`)

Runtime metadata types that the generator populates. Shipped as a separate package so consumers only depend on the thin contracts, not the generator.

| Type | Purpose |
|------|---------|
| `AotReflectionAttribute` | Marks types for metadata generation |
| `ClassMetadata` | Generated type-level metadata container |
| `PropertyMetadata` | Property name, type, getter/setter delegates |
| `MethodMetadata` | Method name, return type, invoker delegate |
| `FieldMetadata` | Field name, type, access delegates |
| `ConstructorMetadata` | Constructor parameters, invoker delegate |
| `ParameterMetadata` | Parameter name, type, default value, attributes |

### Pipeline

```
[AotReflection] attribute on a class
        ↓
Extractors (compile-time symbol analysis)
  TypeExtractor → TypeModel
  PropertyExtractor → PropertyModel
  MethodExtractor → MethodModel
  FieldExtractor → FieldModel
  ConstructorExtractor → ConstructorModel
  ParameterExtractor → ParameterModel
  DeclarationChainExtractor → TypeDeclarationModel (nesting)
  LiteralFormatter → default value literals
        ↓
Code Generators (emit C# source)
  ClassMetadataGenerator → orchestrates output
  PropertyCodeGenerator → getter/setter delegates
  MethodCodeGenerator → invoker delegates
  FieldCodeGenerator → field access delegates
  OutputGenerator → final .g.cs file
  GenerationHelpers → shared emit logic
        ↓
Runtime metadata (in Attributes package)
  ClassMetadata, PropertyMetadata, MethodMetadata,
  FieldMetadata, ConstructorMetadata, ParameterMetadata
```

So instead of `typeof(Foo).GetMethod("Bar").Invoke(...)` at runtime, you get a generated `FooMetadata` class with strongly-typed delegates — no reflection, no trimming warnings, NativeAOT safe.

### Testing.Aot (`src/ANcpLua.Roslyn.Utilities.Testing.Aot/`)

Companion package — attributes and helpers to verify that your code actually survives `PublishAot=true` and `PublishTrimmed=true` builds. Includes `AotTestAttribute`, `AotSafeAttribute`, `AotUnsafeAttribute`, `TrimSafeAttribute`, `TrimTestAttribute`, `TrimUnsafeAttribute`, and feature switch infrastructure.

## For qyl.instrumentation.generators & Qyl.Agents.Generator

**Matching DSL** (`Matching/SymbolMatch.cs`, `InvocationMatch.cs`) — fluent matchers like `Match.Method().Named("Foo").OnType("Bar").ReturnsTask()` and `Invoke.Method().Named("X").WithArguments(...)`. Replaces stringly-typed symbol comparisons in analyzers. This is the right way to detect patterns like `[Traced]` or `[AgentTraced]` methods.

**DiagnosticFlow<T>** (`DiagnosticFlow.cs`) — railway-oriented pipeline: carries value + accumulated diagnostics through `Then()`/`Select()`/`Warn()`/`Error()` chains. Value-equatable so incremental caching works. Your generators should be using this instead of manual if/else diagnostic reporting.

**IncrementalValuesProviderExtensions** — `AddSource()`, `AddSources()`, `GroupBy()`, `Distinct()`, `Take()`, `Skip()`, plus DiagnosticFlow integration. Eliminates the boilerplate in every `Initialize()` method.

**Sources package** (`ANcpLua.Roslyn.Utilities.Sources`) — embeds all utilities as **internal source** into generator assemblies. Generators can't load NuGet DLLs at runtime — this is how they consume the library.

**GeneratorTestEngine** + **GeneratorCachingReport** — fluent test builder with caching validation (compares two runs to detect cache hits/misses). Your generators should be testing caching behavior.

**CodeGeneration.cs** — `IndentedStringBuilder` with `BeginBlock()`/`EndBlock()` scopes, plus `GeneratedCodeHelpers` for standard headers, attributes, pragmas.

**DataFlowAnalyzer<T>** (`FlowAnalysis/`) — abstract forward dataflow over `ControlFlowGraph`. Worklist algorithm with lattice operations. Could power disposal analysis or taint tracking in qyl.instrumentation.

## For qyl.loom & Qyl.Agents (runtime)

**Result<T>** (`Result.cs`) — general-purpose Ok/Fail with `Match()`, `Select()`, `Then()`, `Where()`. Unlike DiagnosticFlow, this has no Roslyn dependency — fits agent/app domain code.

**AsyncSequenceExtensions** (`Async/`) — `ToReadOnlyListAsync()`, `Tap()`, `WhereNotNull()`, chunking for `IAsyncEnumerable<T>`. Directly useful for streaming agent responses in qyl.loom.

**ValueStringBuilder** — ref struct using `Span<char>` → `ArrayPool<char>`. Zero-alloc for small strings. Good for hot paths in loom's HTTP layer.

**Boxes** (`Boxes.cs`) — cached boxed values (bool, int[-1..10], char[0..127]). Eliminates boxing allocations for common values in tag lists / attribute dictionaries.

## For all downstream

**OverloadFinder** — finds method overloads by parameter types. Your CancellationToken analyzer uses this to suggest "use the async overload".

**SemanticGuard** — fluent validation that accumulates all violations into DiagnosticFlow. Replaces nested if-chains in generator validation.

**CodeStylePreferences** — reads EditorConfig settings so generated code respects the project's style (expression bodies, file-scoped namespaces, etc.).

**TypeCache<TEnum>** — O(1) enum-indexed symbol lookup. Your Context classes (OTelContext, etc.) are built on this.

**Polyfills package** — source-only `Index`/`Range`, `init`, `required`, trim attributes for netstandard2.0. Consumed by every generator targeting netstandard2.0.

## 5. Downstream consumers already using this repo

### qyl.mcp.generators (compile-time)

`qyl.mcp.generators` references `ANcpLua.Roslyn.Utilities.Sources` (source-embedded) and already uses:
- **EquatableArray<T>** in `ToolManifestGenerator.cs` and `ToolManifestModels.cs` for incremental caching
- **ToEquatableArray()** in `ToolManifestAnalyzer.cs`

`ToolManifestEmitter.cs` uses raw `StringBuilder` with hardcoded indentation — should be using **IndentedStringBuilder** + **GeneratedCodeHelpers**.

### qyl.mcp runtime

Has `ANcpLua.Roslyn.Utilities` as a PackageReference + global using but doesn't actually use it in runtime code yet. 77+ tool files use plain `StringBuilder` where `IndentedStringBuilder` would help.

### Qyl.Agents.Generator (netagents)

Consumes `ANcpLua.Roslyn.Utilities.Sources` for the same generator infrastructure. The convergence plan targets unifying netagents' `[McpServer]`/`[Tool]` generator with qyl.mcp's `ToolManifestGenerator` — the Matching DSL, DiagnosticFlow, and GeneratorTestEngine become critical when that merge happens.

### qyl.loom

Tight compile-time MCP surface (`LoomGodAnalyzerServer.cs`, 3 tools, 1 prompt). Good fit for the current utilities. `Result<T>` and `AsyncSequenceExtensions` are directly useful for its runtime code.

## 6. Where ANcpLua.Roslyn.Utilities fits in the MCP landscape

This repo provides **compile-time server authoring** infrastructure. That's the gap in the broader ecosystem:

| Concern | Who owns it | ANcpLua.Roslyn.Utilities role |
|---------|-------------|-------------------------------|
| Compile-time tool discovery & codegen | netagents (`Qyl.Agents.Generator`) + qyl.mcp (`ToolManifestGenerator`) | **Primary foundation** — EquatableArray, DiagnosticFlow, Matching DSL, IndentedStringBuilder, Sources package, GeneratorTestEngine |
| Transport (Streamable HTTP, stdio) | Official C# SDK + qyl.mcp hosting | None — runtime concern |
| Runtime orchestration & approval flows | MAF (`DefaultMcpToolHandler`, `InvokeMcpToolExecutor`) | None — MAF's strength |
| Azure Functions MCP hosting | MAF (`BuiltInFunctions.cs`) | None — hosting concern |
| OpenAI/Foundry protocol bridging | MAF (`mcp_list_tools`, `mcp_call` items) | None — protocol concern |
| OTel for MCP spans | qyl.instrumentation + qyl.instrumentation.generators | **Direct** — OTelContext, DeprecatedOtelAttributes (v1.40.0), semconv constants |
| Agent test doubles | This repo (`Testing.AgentTesting`) | **Primary** — FakeAgent*, FakeChatClient, AGUITestServer, ActivityCollector |
| AOT/trim safety | This repo (`AotReflection` + `Testing.Aot`) | **Primary** — compile-time metadata generation, publish verification |

MAF's MCP code is strongest at orchestration, transport, hosted execution, and protocol bridging. It is not strongest at compile-time server authoring or large-scale tool inventory management. That's exactly where this repo's generator infrastructure fills the gap.

**Key constraint:** MAF's declarative bridge (`McpServerToolExtensions.cs`) only supports `AnonymousConnection`. For authenticated MCP product surfaces (qyl.mcp with JWT/Keycloak/OAuth2), that's a real limitation — the compile-time authoring path through netagents + official SDK is the only viable route for production auth.