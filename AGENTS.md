# AGENTS.md

**Scope:** This repository root and all child directories unless a deeper `AGENTS.md` overrides it.

## Role of this repository

Source of truth for `ANcpLua.Roslyn.Utilities` — a 5-layer utility library consumed by qyl (AI observability monorepo at `/Users/ancplua/qyl/`), the sibling `ANcpLua.Agents` repo (MAF helpers + test infrastructure at `/Users/ancplua/framework/ANcpLua.Agents/`), and standalone analyzers in `/Users/ancplua/framework/ANcpLua.Analyzers/`.

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

When implementing a new helper, check these places first (authoritative inventory: `~/.agents/skills/ancplua-roslyn-utilities/SKILL.md`):

1. **Runtime utilities** — `EquatableArray<T>`, `Guard`, `HashCombiner`, `TryExtensions`, `ExpiringCache`, `ByteSize`, `ShortId`, `Base64Url`, `BearerHeader`, `Pkce`, `Sha256Hex`, `CryptoCompare`, `QueryString`, `MarkdownText`, `EnvConfig`, `TimeConversions`, `SqlLikeEscape`, `DataReaderExtensions`, `AsyncSequenceExtensions`, `ParallelAsyncExtensions`
2. **Roslyn pipeline** — `DiagnosticFlow<T>`, `ResultWithDiagnostics<T>`, `SemanticGuard`, `TypeCache<TEnum>`, `IndentedStringBuilder`, `GeneratedCodeHelpers`, `ValueStringBuilder`, `FileWithName`
3. **Symbol analysis** — `SymbolExtensions`, `TypeSymbolExtensions`, `MethodSymbolExtensions`, `InvocationExtensions`, `AttributeExtensions`, `OperationExtensions`
4. **Matching DSL** — `InvocationMatch`, `SymbolMatch` in `Matching/`
5. **Cached contexts** — `DeprecatedOtelAttributes` (semconv v1.40), `SemconvVersion`, `TypeCache<TEnum>` primitive for building your own
6. **Analyzer authoring** — `DiagnosticAnalyzerBase`, `DiagnosticCategories`, `IncrementalPipelineHelpers`, `SyntaxBuilders`
7. **Testing** — `AnalyzerTest`, `CodeFixTest<TAnalyzer,TCodeFix>`, `Test`, `GeneratorTestHelper.RunGenerator<T>()`, `ProjectBuilder`, `PackageTestBase<TFixture>`, `IntegrationTestBase` / `KestrelTestBase` (xUnit / NUnit / TUnit / Bunit variants), `BitNetFixture`

Agent and workflow testing (`FakeChatClient`, conformance suites, provider fixtures, `WorkflowFixture<TInput>`) lives in the sibling `ANcpLua.Agents` repo, not here.

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

- Zero-allocation on success paths (`DiagnosticFlow<T>` value-struct chains, ref structs, spans)
- ArrayPool-backed buffers (`ValueStringBuilder`)
- Static lambdas via context parameters (no closure allocations)
- Span-based parsing throughout
- No finalizers, minimal GC pressure

## Semconv Relationship

This repo ships `DeprecatedOtelAttributes` (dictionary of deprecated semconv attribute names → replacements, semconv v1.40.0) + `SemconvVersion` constants, used by analyzers that flag deprecated attribute usage. The qyl monorepo has its own semconv generator under `eng/semconv/` that produces runtime string constants (`QylAttributes.g.cs` + upstream `packages/Qyl.OpenTelemetry.SemanticConventions*/`) for application code. The two surfaces are complementary — one for generator-side type checking, one for application-side attribute authoring. Do not merge them.

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
