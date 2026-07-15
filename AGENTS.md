# ANcpLua.Roslyn.Utilities engineering contract

This is the repository's policy file for AI agents and contributors. `CLAUDE.md`
is the navigation index (project map + sibling repos), not a second rules file;
keep findings in issues, PRs, and tests, not in this document. This repo lives in
`~/RiderProjects/qyl-workspace/`; workspace-level rules are in the router at
`../AGENTS.md`. Downstream chain: this repo → `ANcpLua.NET.Sdk` (GitHub-only) →
`ANcpLua.Agents` → qyl agent runtime, so a broken restore or a signature change
here propagates the whole way down.

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

## Known limitations (open)

- `ClassMetadata.InvokeMethod` and `CreateInstance` (AOT reflection Attributes)
  resolve a call by **method/constructor name + argument arity only** — same-arity
  overloads are not disambiguated by parameter type, so the first arity match wins.
  `ParameterMetadata.Type` is available, so the planned resolution is to prefer a
  parameter-type-exact overload and fall back to the current first-match behavior.
  Track this until fixed; do not silently drop it.

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

Publication is GitHub Actions OIDC trusted publishing, triggered on a `v*` tag push
and gated by CI. Never add a long-lived NuGet API key or publish locally. Per the
framework bootstrap rules, a self-referencing package version points at the
last-published release, not the version coming up — CI stamps the new version at
pack time. Tag a build-broken commit and it will not publish; use the next patch
rather than reassigning a ghost tag.
