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

## Cross-Repo Awareness — was passiert, wenn du Versionen anfasst

Diese vier Repos bilden eine Bootstrap-Kette: `Roslyn.Utilities → NET.Sdk → (Analyzers, Agents)`. Truth-Source für Paket-Versionen ist **`ANcpLua.NET.Sdk/src/Build/Common/Version.props`**, in den SDK-NuGet-Packages gepackt und in jedes Consumer-Projekt geladen. Dein lokales `Version.props` (sofern vorhanden) wird *nach* der SDK-Datei importiert (last-wins) — gedacht, um lokal AHEAD der gerade-publizierten SDK zu pinnen.

Bevor du eine Variable in Truth oder im lokalen Override bumpst:

- **Truth fließt durch GlobalPackageReference.** Pakete wie `ANcpLua.Analyzers` werden von der SDK in *jedes* Consumer-Projekt injiziert. Wenn Truth auf eine Version zeigt, die noch nicht auf nuget.org liegt, scheitert jeder Restore mit `NU1102` — auch die SDK-eigenen Tests (sie packen ein Sample.csproj und builden es). Saubere Reihenfolge: zuerst das ausgeschriebene Repo taggen + auf NuGet bringen, dann Truth nachziehen.

- **Self-Reference: die eigene Paket-Version zeigt auf last-PUBLISHED.** Wenn ein lokales `Version.props` eine Variable für das *eigene* Paket des Repos hat (z.B. `ANcpLuaAnalyzersVersion` in `ANcpLua.Analyzers/Version.props`), muss sie auf die zuletzt-publizierte Version zeigen, nicht auf die hochzukommende. csproj/Tests-Files referenzieren das Paket via `PackageReference` und ziehen es beim Restore aus NuGet; während Restore (vor Pack) gibt's die hochzukommende Version noch nicht. CI stampt die neue Version per `-p:Version=X.Y.Z` erst zur Pack-Time.

- **Bumps haben transitive Konsequenzen unter CPM.** Z.B. `Meziantou.Framework.DependencyScanning 2.0.11` zieht `YamlDotNet ≥ 17.0.1`. Bei `ManagePackageVersionsCentrally=true` ist Downgrade ein Hard-Error (`NU1109`). Wenn ein Bump nicht greift, steht der Grund in der Restore-Fehlermeldung — vor dem nächsten Versuch lesen.

- **Lokales Override gleich/unter Truth ist Müll.** Gleich = Doppelpflege, unter = stille Regression. Pruning sinnvoll, sobald die SDK mit matching Werten publisht.

- **Publish triggert auf Tag-Push `v*`, gegated durch Tests.** Ein Tag auf einen build-broken Commit publisht nicht, bleibt aber als Ghost-Tag remote. Statt remote zu re-assignen (≈ Force-Push), nächste Patch-Version verwenden.

- **Verifiziere Versionen vor dem Bump.** Ein Tippfehler (`2.0.20` statt `2.0.11`) bricht die Topo-Kette, weil Truth in alle Konsumenten fließt. NuGet-API: `https://api.nuget.org/v3-flatcontainer/<lowercased-id>/index.json`.

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

## ANcpLua Ecosystem

| Repo | Purpose | NuGet | CI checks required |
|---|---|---|---|
| [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) | Opinionated MSBuild SDK — standardized defaults, policy enforcement, analyzer injection | [nuget.org](https://www.nuget.org/packages/ANcpLua.NET.Sdk) | `compute_version`, `lint_config`, `test (ubuntu/windows/macos)`, `create_nuget` |
| [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) | Custom Roslyn analyzers (auto-injected by the SDK) | [nuget.org](https://www.nuget.org/packages/ANcpLua.Analyzers) | `build`, `test (ubuntu/windows/macos)` |
| [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) | Source generator utilities, TryParse extensions, polyfills | [nuget.org](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities) | `build (ubuntu/windows)`, `version` |
| [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents) | MAF runtime helpers + agent test infrastructure | [nuget.org](https://www.nuget.org/packages/ANcpLua.Agents) | `build (ubuntu/windows/macos)`, `version` |

### Branch protection (all 4 repos)

- PR required to merge into `main` (0 approvals, squash preferred)
- Required status checks must pass (CI jobs listed above)
- Branch must be up-to-date with `main` before merge
- Force push and branch deletion blocked on `main`
- Optional checks (CodeRabbit, GitGuardian, Copilot review, auto-merge) do not block merges

### Dependency graph

```
ANcpLua.NET.Sdk
  ├── injects ANcpLua.Analyzers (compile-time)
  └── ships Version.props (version truth for all consumers)

ANcpLua.Analyzers
  └── consumes ANcpLua.Roslyn.Utilities.Sources (source-only, internal)

ANcpLua.Roslyn.Utilities
  └── standalone (no first-party deps)

ANcpLua.Agents
  └── standalone (no first-party deps)
```

### Release flow

Manual-tag-triggers-publish. The workflow runs on `push: main` for build + test only (publish job gated by `is_release=true`); the tag push triggers the publish path.

1. PR to `main` via squash merge — workflow runs build + test; publish job skipped (`is_release=false`)
2. After merge: `git tag vX.Y.Z && git push --tags` — version comes from `${GITHUB_REF_NAME#v}`, `is_release=true`
3. Publish job pushes to NuGet via trusted publishing, then `gh release create v$VERSION` auto-creates the GitHub release
4. NuGet indexes in ~4-8 minutes — downstream repos pick up via Renovate

Note: ANcpLua.NET.Sdk uses a different pattern (auto-bump-on-merge + auto-tag); ANcpLua.Analyzers uses the same manual-tag pattern as this repo but does **not** auto-create the GH release.
