# ANcpLua.Roslyn.Utilities - Consolidated Review Log

Only this file is kept for review outcomes to avoid spreading findings across directories.
## Framework conventions

Branch protection, auto-merge, CodeRabbit posture, release flow, dependency
graph, and the cross-repo bootstrap rules for the four ANcpLua framework
repos are documented in one place at
[O-ANcppLua/renovate-config](https://github.com/O-ANcppLua/renovate-config#ancplua-framework-conventions--renovate-config).
This file documents conventions specific to this repo only.


## 1) src/ANcpLua.AotReflection/ANcpLua.Analyzers.AotReflection.csproj
- No blocking defects confirmed in first pass.
- Follow-ups to decide:
  - Confirm whether analyzer consumers must manually reference `ANcpLua.AotReflection.Attributes`.
  - Confirm `LangVersion=latest` policy if repo-wide pinning is adopted.
- One second-pass reviewer flagged packaging/lock-in risks; keep an eye on analyzer dependency closure and namespace/API stability.

## 2) src/ANcpLua.AotReflection.Attributes/ANcpLua.Analyzers.AotReflection.Attributes.csproj
- High: `ClassMetadata.InvokeMethod` matches by name + arity only, not parameter types; overload calls can dispatch to wrong target when arities collide.
- Medium: null instance can leak into generated convenience paths (`GetPropertyValue`/`SetPropertyValue` and non-static `InvokeMethod`), producing confusing runtime errors.

## 3) src/ANcpLua.DiscriminatedUnion/ANcpLua.Analyzers.DiscriminatedUnion.csproj
- No verified defects found.
- Note: no build/pack validation was run in this review pass.

## 4) src/ANcpLua.ExtensibleEnumMirror/ANcpLua.Analyzers.ExtensibleEnumMirror.csproj
- No blocking issues found.
- Low: keep package metadata text/provider tags stable to avoid drift.

## 5) src/ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities.csproj
- Medium: `LangVersion=latest` is non-reproducible across SDK changes.
- Medium: `AutoUnifyAssemblyReferences` should be removed or documented.
- Medium: `ANCPLUA_ROSLYN_PUBLIC` should stay controlled per-consumer; avoid bleeding API-visibility mode into source-only/internal pack surfaces.
- Medium: `ImplicitUsings` and package input handling should be explicit for reproducibility.
- Medium: add explicit `PrivateAssets` guardrails where needed to control public restore surface.
- Non-blocking: README/LICENSE are included as package inputs; verify this is intended.

## 6) src/ANcpLua.Roslyn.Utilities.Polyfills/ANcpLua.Roslyn.Utilities.Polyfills.csproj
- No blocking issues identified.
- Packaging path logic was reviewed as consistent for deterministic source packaging.

## 7) src/ANcpLua.Roslyn.Utilities.Sources/ANcpLua.Roslyn.Utilities.Sources.csproj
- Medium: pack-time hard dependency on `pwsh` in `_TransformSourcesForPack` can fail on nodes without PowerShell.
- Low: `-SourceDir` and `-OutputDir` args are unquoted and can break on paths with spaces.
- Medium: declaration rewrite logic is pattern-based and brittle for atypical modifier ordering.

## 8) src/ANcpLua.Roslyn.Utilities.Testing/ANcpLua.Roslyn.Utilities.Testing.csproj
- Medium: packages may flow transitively due to missing `PrivateAssets` controls.

## 9) src/ANcpLua.Roslyn.Utilities.Testing.Aot/ANcpLua.Roslyn.Utilities.Testing.Aot.csproj
- Medium: `LangVersion=latest` is non-deterministic across SDK updates.
- Note: `build` vs `buildTransitive` expectations should be validated explicitly.
- Note: `NoWarn` includes `CS1591` and `NETSDK1212` (watch for masked diagnostics).

## Validation that was already run
- `dotnet test -c Release` passed previously in the reviewed scope (51 total, 51 passed).