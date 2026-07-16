# ANcpLua.Roslyn.Utilities engineering contract

This is the repository's single policy + navigation file for AI agents and
contributors (`CLAUDE.md` is a symlink to this file). Keep findings in issues,
PRs, and tests, not in this document. This repo lives in
`~/RiderProjects/qyl-workspace/`; workspace-level rules are in the router at
`../AGENTS.md`. Downstream chain: this repo → `ANcpLua.NET.Sdk` (GitHub-only) →
`ANcpLua.Agents` → qyl agent runtime, so a broken restore or a signature change
here propagates the whole way down.

## Project index

1. [AOT reflection generator](src/ANcpLua.AotReflection/CLAUDE.md)
2. [AOT reflection attributes](src/ANcpLua.AotReflection.Attributes/CLAUDE.md)
3. [Discriminated union generator](src/ANcpLua.DiscriminatedUnion/CLAUDE.md)
4. [Extensible enum mirror generator](src/ANcpLua.ExtensibleEnumMirror/CLAUDE.md)
5. [Core Roslyn utilities](src/ANcpLua.Roslyn.Utilities/CLAUDE.md)
6. [Polyfills package](src/ANcpLua.Roslyn.Utilities.Polyfills/CLAUDE.md)
7. [Source-only package](src/ANcpLua.Roslyn.Utilities.Sources/CLAUDE.md)
8. [Testing utilities](src/ANcpLua.Roslyn.Utilities.Testing/CLAUDE.md)
9. [AOT testing utilities](src/ANcpLua.Roslyn.Utilities.Testing.Aot/CLAUDE.md)

## Nearby repos

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk): shared SDK/version truth for the ANcpLua repos.
- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers): analyzer consumer of the source-only utilities.
- [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents): successor location for agent workflow/test helpers; do not describe this repo as the MAF runtime home.

## Purpose

Foundation Roslyn helpers, source generators, and `netstandard2.0` utilities shared
across the ANcpLua framework. Two families ship from `src/`:

- **`ANcpLua.Analyzers.*` generators** — AOT reflection (`AotReflection` +
  `AotReflection.Attributes` runtime metadata types), discriminated unions, and the
  extensible-enum mirror. These are the product.
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
  every sibling worker; the `completedReading` flag suppresses secondary errors on
  consumer-side dispose by design.
- `ExpiringCache<TKey,TValue>` is access-order LRU + single-flight via
  `Lazy<TValue?>`; the factory runs outside the `_lock` so cache reads never block on
  the factory.
- `ClassMetadata.InvokeMethod` / `CreateInstance` disambiguate same-name, same-arity
  overloads by matching each argument's runtime type against `ParameterMetadata.Type`:
  the parameter-type-exact overload wins, with the first name+arity match as the
  fallback when no exact match exists (e.g. all-null arguments). Guarded by
  `ClassMetadataDispatchTests`.

## Conventions

Imported from `ANcpLua.NET.Sdk`. Repo-specific points that are deliberate (not
oversights — earlier review flagged them and they resolved to these choices):

- Generator projects set `ImplicitUsings=disable` and pin analyzer/CodeAnalysis
  `PackageReference`s and the utilities `ProjectReference` with `PrivateAssets="all"`
  so nothing leaks transitively into consumers.
- Public-surface visibility is gated by the `ANCPLUA_ROSLYN_PUBLIC` compilation
  symbol; the source-only `.Sources` package collapses those guards to `internal` at
  pack time (`Transform-Sources.ps1`), so it is always internal to its consumer.
- The `.Sources` pack step shells out to `pwsh`. That is an accepted CI-time
  dependency; if you move packing off PowerShell, remove the dependency rather than
  documenting around it.

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
on **push to `main`** — not on a manual tag. CI reads the latest `v*` tag, auto-bumps
the patch, builds and tests on Linux + Windows, and publishes only when the diff since
that tag touches a shipped surface (the gate watches `src/**`, `tests/**`,
`README.md`, `Version.props`, `Directory.Packages.props`, and the workflow file
itself — root dep bumps change shipped nuspec floors and must publish); it then
creates the matching `v*` tag and GitHub release. A doc-only or
CI-only change builds but publishes nothing. Never add a long-lived NuGet API key or
publish locally, and a build-broken `main` cannot publish — the version is CI-computed,
so just push a fix and the next patch ships.
