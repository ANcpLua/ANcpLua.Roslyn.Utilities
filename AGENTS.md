# AGENTS.md

**Scope:** This repository root and all child directories unless a deeper `AGENTS.md` overrides it.

## Role of this repository

Source of truth for `ANcpLua.Roslyn.Utilities` — a 5-layer utility library consumed by qyl (AI observability), Qyl.Agents (MCP source generator), and standalone analyzers. All three consumers now live in the qyl monorepo at `/Users/ancplua/qyl`.

## Architecture: 5 Layers

| Layer | Package | Target | Dependencies |
|-------|---------|--------|-------------|
| 1. Runtime | `ANcpLua.Roslyn.Utilities` | netstandard2.0 | BCL only (no Roslyn) |
| 2. Roslyn | `ANcpLua.Roslyn.Utilities` | netstandard2.0 | Microsoft.CodeAnalysis |
| 3. Testing | `ANcpLua.Roslyn.Utilities.Testing` | net10.0 | xUnit v3 / NUnit / TUnit / Bunit, Roslyn, ASP.NET Core |
| 4. AOT Testing | `ANcpLua.Roslyn.Utilities.Testing.Aot` | netstandard2.0 | None — attributes + polyfills only |
| 5. Polyfills | `.Sources` / `.Polyfills` | netstandard2.0 | None |

Layer 1 (runtime) has NO Roslyn dependency — data structures, parsing, streaming, math, and domain utilities usable by any .NET project. Layer 2 adds Roslyn-specific symbol analysis and code generation in the same package. Layer 3 is test-time only (Roslyn generator/analyzer/codefix tests, MSBuild/NuGet integration tests, cross-framework web testing, OTel instrumentation helpers, plus the `BitNetFixture` live-LLM integration). Layer 4 is AOT/trim test attributes + MSBuild orchestration for standalone AOT verification. Layer 5 is source-only distribution for source generators that can't load NuGet DLLs at runtime.

Agent and workflow testing — `FakeChatClient`, the provider-agnostic MAF conformance suite, provider fixtures (OpenAI / Azure / Anthropic / Ollama / Gemini / OpenRouter), `WorkflowFixture<TInput>` — live in the sibling package [`ANcpLua.Agents`](https://www.nuget.org/packages/ANcpLua.Agents/).

## Hard rules

- No dependency on `ANcpLua.NET.Sdk` from this repo.
- Prefer composition reuse: check existing models/helpers before adding new code.
- **New packages require justification**: downstream-unavoidable, pinned through `Version.props` under a dedicated MSBuild variable, and added to `Directory.Packages.props` under the existing groupings. When adding a package family, add all siblings in one commit; bump consumers in a follow-up commit once the new package version has published to nuget.org.
- Preserve public APIs unless there is a justified versioning reason.
- Use the existing coding style (nullable-aware, null-guarded, small focused helpers).
- Do not run `dotnet test` unless explicitly asked by the user.

## Check Before Writing

When implementing a new helper, check these places first:

1. **Runtime utilities** — `Result<T>`, `EquatableArray<T>`, `Guard`, `HashCombiner`, `TryExtensions`, `CircularBuffer`, `ExpiringCache`
2. **Roslyn pipeline** — `DiagnosticFlow<T>`, `SemanticGuard`, `TypeCache<TEnum>`, `IndentedStringBuilder`
3. **Symbol analysis** — `SymbolExtensions`, `TypeSymbolExtensions`, `MethodSymbolExtensions`, `InvocationExtensions`
4. **Matching DSL** — `InvocationMatch`, `SymbolMatch` in `Matching/`
5. **Contexts** — `OTelContext`, `AwaitableContext`, `CollectionContext`, `DisposableContext`
6. **Testing** — `AnalyzerTest<T>`, `CodeFixTest<T,F>`, `Test<TGenerator>`, `GeneratorTestHelper.RunGenerator<T>()`, `ProjectBuilder`, `PackageTestBase<TFixture>`, `IntegrationTestBase<T>` / `KestrelTestBase<T>` (xUnit/NUnit/TUnit/Bunit variants), `BitNetFixture`

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
  ANcpLua.Roslyn.Utilities.Sources/               # Layer 5: source-only redistribution
  ANcpLua.Roslyn.Utilities.Polyfills/             # Layer 5: polyfills-only redistribution
  ANcpLua.Roslyn.Utilities.Testing/               # Layer 3: Roslyn + OTel test infrastructure + BitNet fixture
  ANcpLua.Roslyn.Utilities.Testing.Aot/           # Layer 4: AOT/trim test attributes + MSBuild orchestration
  ANcpLua.AotReflection/                          # AOT reflection source generator
  ANcpLua.AotReflection.Attributes/               # AOT reflection attribute contracts
```
