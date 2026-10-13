# ANcpLua.Roslyn.Utilities engineering contract

This is the repository's single policy + navigation file for AI agents and
contributors (`CLAUDE.md` is a symlink to this file). Keep findings in issues,
PRs, and tests, not in this document. Downstream chain: this repo → `ANcpLua.NET.Sdk` (GitHub-only) →
`ANcpLua.Agents` → qyl agent runtime, so a broken restore or a signature change
here propagates the whole way down.

## Release lines

Until .NET 11 GA two lines ship from this repository:

| Branch | Line | Toolchain | NuGet version | GitHub release |
|---|---|---|---|---|
| `main` | 2.2, stable | .NET SDK 10 (`global.json`), net10.0, no C# 15 unions | `2.2.N` | `v2.2.N`, marked latest |
| `release/5.0` | 5.0, preview | .NET SDK 11 RC, net10.0 + net11.0, C# 15 union polyfills | `5.0.0-preview.N` | `v5.0.0-preview.N`, pre-release |

- NuGet has no branches: the SemVer suffix makes a prerelease. nuget.org lists `5.0.0-preview.N` as
  prerelease and never as the latest version; a consumer opts in by naming the exact version.
- `nuget-publish.yml` derives both versions from tags: on `main` the latest stable `v*` tag plus one patch,
  on `release/X.Y` the next `vX.Y.0-preview.N`, and on `main` after `release/X.Y` is merged in, `X.Y.0`.
  Each publish creates its tag and GitHub release.
- A fix both lines need lands on `main` by PR and is cherry-picked onto `release/5.0`. Never merge `main`
  into `release/5.0`: that would carry the SDK 10 rollback across. SDK 11 and union work goes to
  `release/5.0` only.
- At .NET 11 GA: move `release/5.0` to the GA SDK, merge it into `main` with a merge commit, not a
  squash, so its preview tags enter main's history (on conflicts keep the `release/5.0` side of
  `global.json`, `Version.props`, the TFMs and the union files). The push publishes `5.0.0`; delete
  `release/5.0` afterwards. main's patch bump then continues from `v5.0.0`.

## Project index

1. [AOT reflection generator](src/ANcpLua.AotReflection/CLAUDE.md)
2. [AOT reflection attributes](src/ANcpLua.AotReflection.Attributes/CLAUDE.md)
3. [Extensible enum mirror generator](src/ANcpLua.ExtensibleEnumMirror/CLAUDE.md)
4. [Core Roslyn utilities](src/ANcpLua.Roslyn.Utilities/CLAUDE.md)
5. [Polyfills package](src/ANcpLua.Roslyn.Utilities.Polyfills/CLAUDE.md)
6. [Source-only package](src/ANcpLua.Roslyn.Utilities.Sources/CLAUDE.md)
7. [Testing utilities](src/ANcpLua.Roslyn.Utilities.Testing/CLAUDE.md)
8. [AOT testing utilities](src/ANcpLua.Roslyn.Utilities.Testing.Aot/CLAUDE.md)

## Nearby repos

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk): shared SDK/version truth for the ANcpLua repos.
- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers): analyzer consumer of the source-only utilities.
- [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents): successor location for agent workflow/test helpers; do not describe this repo as the MAF runtime home.

## Purpose

Foundation Roslyn helpers, source generators, and `netstandard2.0` utilities shared
across the ANcpLua framework. Two families ship from `src/`:

- **`ANcpLua.Analyzers.*` generators** — AOT reflection (`AotReflection` +
  `AotReflection.Attributes` runtime metadata types) and the extensible-enum mirror.
  These are the product.
- **`ANcpLua.Roslyn.Utilities.*` helpers** — the core analyzer/generator utility
  library, its `.Polyfills`, the source-only `.Sources` package, and the `.Testing`
  / `.Testing.Aot` harnesses.

The authoritative package set is the packable projects under `src/`; do not hardcode
a package count or a version in prose — `Version.props` owns the version line and git
tags (`v*`) own what is published. State the source of truth, not a snapshot.

## Framework conventions

Branch protection, auto-merge, CodeRabbit posture, release flow, dependency graph,
and the cross-repo bootstrap rules for the four ANcpLua framework repos live in one
place at
[ANcpLua/renovate-config](https://github.com/ANcpLua/renovate-config#ancplua-framework-conventions--renovate-config).
This file documents only what is specific to this repo.

## Hot-path invariants

These are correctness-critical and easy to regress. Each is guarded by a named test;
keep the test green rather than trusting the prose.

- `OperationExtensions.IsConstantZero` matches every built-in numeric zero (`0`,
  `0L`, `0u`, `0uL`, `0.0f`, `0.0`, `0m`). The `Value: 0` shortcut matches `int 0`
  only and was the AL0014 regression — keep the full alternation, guarded by
  `OperationExtensionsConstantsTests`.
- Symbol identity beats `ToDisplayString()` on hot paths. `IsTaskType` / `IsSpanType`
  / `IsMemoryType` / `IsCancellationTokenType` compare namespace + name via
  `INamespaceSymbol.GetMetadataName()` through one shared walker in
  `SymbolExtensions.cs`; do not re-introduce per-file copies.
- `IsEnumerableType` / `GetElementType` consult `OriginalDefinition.SpecialType`
  because closed generics like `IEnumerable<int>` carry `SpecialType.None` — the
  marker lives on the open generic only.
- `TryExtensions.TryParse*` is pinned to `CultureInfo.InvariantCulture` with explicit
  `NumberStyles` / `DateTimeStyles`; do not regress to current-culture overloads.
- `ParallelAsyncExtensions` uses a linked CTS so a single selector exception cancels
  every sibling worker, and `DisposeAsync` returns only after the producer and workers
  have stopped; a consumer that disposes early never sees secondary worker errors.
  Guarded by `ParallelAsyncExtensionsTests`.
- `ExpiringCache<TKey,TValue>` is access-order LRU + single-flight via
  `Lazy<TValue?>`; the factory runs outside the `_lock` so cache reads never block on
  the factory. The in-flight `Lazy` re-checks the cache before it runs the factory and
  the owner releases it only after publishing, so while an entry is live a miss never
  starts a second factory run and concurrent callers return the one published instance.
  Guarded by `ExpiringCacheTests`.
- `ClassMetadata.InvokeMethod` / `CreateInstance` disambiguate same-name, same-arity
  overloads by matching each argument's runtime type against `ParameterMetadata.Type`:
  the parameter-type-exact overload wins, with the first name+arity match as the
  fallback when no exact match exists (e.g. all-null arguments). Guarded by
  `ClassMetadataDispatchTests`.
- No public extension method repeats the name and parameter types of a BCL or Roslyn
  one: consumers import this namespace next to `System.Linq` and
  `Microsoft.CodeAnalysis.Operations`, so a duplicate makes every call CS0121. Guarded
  by `PublicSurfaceTests`.
- `.Sources` and `.Polyfills` compile side by side (the `.Sources` polyfill copy
  steps aside), and an `Inject*=false` switch removes only that package's files. The
  removal runs in a target because MSBuild applies per-item conditions only there.
  Guarded by `PackageConsumptionTests`.

## Conventions

Imported from `ANcpLua.NET.Sdk`. Repo-specific points that are deliberate (not
oversights — earlier review flagged them and they resolved to these choices):

- Generator projects set `ImplicitUsings=disable` and pin analyzer/CodeAnalysis
  `PackageReference`s and the utilities `ProjectReference` with `PrivateAssets="all"`
  so nothing leaks transitively into consumers.
- Public-surface visibility is gated by the `ANCPLUA_ROSLYN_PUBLIC` compilation
  symbol; the source-only `.Sources` package collapses those guards to `internal` at
  pack time (inline MSBuild task in its csproj), so it is always internal to its
  consumer.

## Build and test

```bash
dotnet build -c Release
dotnet test  -c Release   # xUnit v3 + Microsoft Testing Platform
```

Tests live under `tests/` across per-generator suites and the `Testing.Tests`
utility suite; the projects are the source of truth for what is covered. A public-API
change updates the analyzer-managed shipped/unshipped baselines in the same commit.

## Publishing

Publication is GitHub Actions OIDC trusted publishing (`nuget-publish.yml`) that runs
on **push to `main` or `release/**`** — not on a manual tag. CI computes the version from
the tags (see [Release lines](#release-lines)), builds and tests on Linux + Windows, and publishes only when the diff since
that tag touches a shipped surface (the gate watches `src/**`, `tests/**`,
`README.md`, `Version.props`, `Directory.Packages.props`, and the workflow file
itself — root dep bumps change shipped nuspec floors and must publish); it then
creates the matching `v*` tag and GitHub release. A doc-only or
CI-only change builds but publishes nothing. Never add a long-lived NuGet API key or
publish locally, and a build-broken `main` cannot publish — the version is CI-computed,
so just push a fix and the next patch ships.
