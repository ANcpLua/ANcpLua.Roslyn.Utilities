# AGENTS.md

**Scope:** This repository root and all child directories unless a deeper `AGENTS.md` overrides it.

## Role of this repository

Source of truth for `ANcpLua.Roslyn.Utilities` — a 5-layer utility library consumed by qyl (AI observability), Qyl.Agents (MCP source generator), and standalone analyzers. All three consumers now live in the qyl monorepo at `/Users/ancplua/qyl`.

## Architecture: 6 Layers

| Layer | Package | Target | Dependencies |
|-------|---------|--------|-------------|
| 1. Runtime | `ANcpLua.Roslyn.Utilities` | netstandard2.0 | BCL only (no Roslyn) |
| 2. Roslyn | `ANcpLua.Roslyn.Utilities` | netstandard2.0 | Microsoft.CodeAnalysis |
| 3. Testing | `ANcpLua.Roslyn.Utilities.Testing` | net10.0 | xUnit v3, Roslyn, ASP.NET Core |
| 4. Agent Testing | `ANcpLua.Roslyn.Utilities.Testing.AgentTesting` | net10.0 | Microsoft.Extensions.AI, Moq, provider SDKs |
| 5. Workflow Testing | `ANcpLua.Roslyn.Utilities.Testing.Workflows` | net10.0 | Microsoft.Agents.AI.Workflows, AwesomeAssertions |
| 6. Polyfills | `.Sources` / `.Polyfills` | netstandard2.0 | None |

Layer 1 (runtime) has NO Roslyn dependency. It provides data structures, parsing, streaming, math, and domain utilities usable by any .NET project. Layer 2 adds Roslyn-specific symbol analysis and code generation. Layers 3-5 are test-time only. Layer 6 is source-only distribution.

**Layer 4 (Agent Testing)** ships `FakeChatClient`, `FakeAgentBase` + variants, `ChatClientAgentTestHelper`, `MockChatClients`, `AGUITestServer`, `ActivityCollector`, the MAF `Conformance/` suite (`RunTests`, `RunStreamingTests`, `ChatClientAgentRunTests`, `ChatClientAgentRunStreamingTests`, `StructuredOutputRunTests`), and reference `IChatClientAgentFixture` implementations for OpenAI, Azure OpenAI, Anthropic, Ollama, Google Gemini, and OpenRouter in `Conformance/Examples/`.

**Layer 5 (Workflow Testing)** is a sibling of Agent Testing for `Microsoft.Agents.AI.Workflows`-based tests. It ships `WorkflowFixture<TInput>`, `TestEchoAgent`, `TestReplayAgent`, `RoleCheckAgent`, `TestRequestAgent`, the `ExecutionEnvironment` parametric axis, and the `Framework/` declarative-workflow harness (`WorkflowHarness`, `WorkflowEvents`, `Testcase`). Harvested from `microsoft/agent-framework` upstream test tree.

## Hard rules

- No dependency on `ANcpLua.NET.Sdk` from this repo.
- Prefer composition reuse: check existing models/helpers before adding new code.
- **New packages require justification**: downstream-unavoidable (e.g., `Microsoft.Agents.AI.Workflows` for workflow test infrastructure, provider SDKs for conformance fixtures), pinned through `Version.props` under a dedicated MSBuild variable, and added to `Directory.Packages.props` under the existing groupings. When adding a package family, add all siblings in one commit; bump consumers in a follow-up commit once the new package version has published to nuget.org.
- Preserve public APIs unless there is a justified versioning reason.
- Use the existing coding style (nullable-aware, null-guarded, small focused helpers).
- Do not run `dotnet test` unless explicitly asked by the user.
- **Upstream-harvest exception**: files under `Testing.Workflows` and `Testing.AgentTesting/ChatClients`, `/Conformance/Examples` are intentionally kept close to their upstream `microsoft/agent-framework` shape. Those projects carry a project-scoped `<NoWarn>` for style analyzer drift (Meziantou + CA + CS1591) per the `~/.claude/CLAUDE.md` "upstream sample repos" clause. Do not propagate these suppressions beyond the harvest projects.

## Check Before Writing

When implementing a new helper, check these places first:

1. **Runtime utilities** — `Result<T>`, `EquatableArray<T>`, `Guard`, `HashCombiner`, `TryExtensions`, `CircularBuffer`, `ExpiringCache`
2. **Roslyn pipeline** — `DiagnosticFlow<T>`, `SemanticGuard`, `TypeCache<TEnum>`, `IndentedStringBuilder`
3. **Symbol analysis** — `SymbolExtensions`, `TypeSymbolExtensions`, `MethodSymbolExtensions`, `InvocationExtensions`
4. **Matching DSL** — `InvocationMatch`, `SymbolMatch` in `Matching/`
5. **Contexts** — `OTelContext`, `AwaitableContext`, `CollectionContext`, `DisposableContext`
6. **Testing** — `GeneratorTestEngine`, `FakeChatClient`, `ActivityCollector`, `IntegrationTestBase`

## Accessibility Pattern

All types use conditional compilation:
```csharp
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
```
Define `ANCPLUA_ROSLYN_PUBLIC` to make types public. The Sources package embeds everything as `internal`.

## Performance Conventions

- Zero-allocation on success paths (`Result<T>`, ref structs, spans)
- ArrayPool-backed buffers (`ValueStringBuilder`)
- Static lambdas via context parameters (no closure allocations)
- Span-based parsing throughout
- No finalizers, minimal GC pressure

## Semconv Relationship

This repo provides compile-time type analysis (`OTelContext.cs` caches `Activity`, `Meter`, etc. as `INamedTypeSymbol`). The qyl monorepo has a separate semconv generator (`eng/semconv/`) that produces runtime string constants (`GenAiAttributes.g.cs`). These are complementary — one for generators, one for application code. Do not merge them.

## Build and Delivery

- Core verification: `dotnet build -c Release`
- Packaging: `dotnet pack -c Release`
- `dotnet test` disabled by policy — only run when explicitly asked

## Preferred Toolchain

- **Source search/edit/builds:** `mcp__rider__*` tools
- **UI verification:** `mcp__playwright__*` tools
- **No SDK dependency assumptions:** keep netstandard-first utility logic reusable

## Runtime Context

- C# 14 language features available
- .NET 10 SDK baseline
- Core package targets `netstandard2.0`

## Decision Rules

- If you need behavior changes, keep blast radius local.
- For generated code changes, ensure emitted shape remains consistent and deterministic.
- Trust the implementation under `src/ANcpLua.Roslyn.Utilities` over stale doc snippets.
- Update this file when mismatches appear.

## Quick Model Map

```
src/
  ANcpLua.Roslyn.Utilities/                       # Layer 1+2: runtime + Roslyn utilities
  ANcpLua.Roslyn.Utilities.Sources/               # Layer 6: source-only redistribution
  ANcpLua.Roslyn.Utilities.Polyfills/             # Layer 6: polyfills-only redistribution
  ANcpLua.Roslyn.Utilities.Testing/               # Layer 3: Roslyn + OTel test infrastructure
  ANcpLua.Roslyn.Utilities.Testing.AgentTesting/  # Layer 4: AI agent test doubles, MAF conformance, provider fixtures
  ANcpLua.Roslyn.Utilities.Testing.Workflows/     # Layer 5: MAF workflow test infrastructure
  ANcpLua.Roslyn.Utilities.Testing.Aot/           # AOT testing infrastructure
  ANcpLua.AotReflection/                          # AOT reflection source generator
  ANcpLua.AotReflection.Attributes/               # AOT reflection attribute contracts
```
