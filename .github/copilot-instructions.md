# Copilot Instructions — ANcpLua.Roslyn.Utilities

## Build & Test

```bash
dotnet build -c Release            # Build all projects
dotnet pack -c Release             # Create NuGet packages
dotnet test                        # Run full test suite (no test projects currently in-repo)
```

Requires .NET SDK 10.0+ (`global.json` pins `10.0.103` with `latestMinor` rollforward).

## Architecture

This is **LAYER 0** — the upstream source of truth for all Roslyn utilities. Other repos (ANcpLua.NET.Sdk, ANcpLua.Analyzers, ErrorOrX, qyl) consume packages published from here.

### Packages

| Package | TFM | Delivery |
|---------|-----|----------|
| `ANcpLua.Roslyn.Utilities` | `netstandard2.0` | DLL — core utilities for generators/analyzers |
| `ANcpLua.Roslyn.Utilities.Sources` | `netstandard2.0` | Source-only — embeds all code as `internal` via `Transform-Sources.ps1` |
| `ANcpLua.Roslyn.Utilities.Polyfills` | `netstandard2.0` | Source-only — polyfills only, no Roslyn dependency |
| `ANcpLua.Roslyn.Utilities.Testing` | `net10.0` | DLL — testing framework for analyzers/generators |
| `ANcpLua.Roslyn.Utilities.Testing.Aot` | `netstandard2.0` | AOT/trim testing attributes |

### Critical Constraints

- **No circular dependency**: This repo CANNOT reference `ANcpLua.NET.Sdk` — it is upstream of the SDK.
- **Publish order**: Packages here must be published to NuGet before the SDK or downstream repos can update.
- **Version.props**: Centralized version file shared across the ecosystem. Downstream repos sync from here.

### Dual Visibility Pattern

All types use `#if ANCPLUA_ROSLYN_PUBLIC` to control visibility:
- **Core project** defines `ANCPLUA_ROSLYN_PUBLIC` → types are `public`
- **Sources package** strips the guards via `Transform-Sources.ps1` → types become `internal`

When adding new public types, follow this pattern:

```csharp
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class MyExtensions { }
```

## Key Conventions

### Check Before Writing

Before writing ANY helper code, search existing utilities. The codebase has extensive coverage:

- **Symbol matching**: `Match.Method()`, `Match.Type()`, `Invoke.Method()` — fluent DSL in `Matching/`
- **Validation**: `Guard.NotNull()`, `Guard.NotNullOrElse()`, `SemanticGuard<T>` — in `Guard.cs`, `SemanticGuard.cs`
- **Generator caching**: `EquatableArray<T>` — value-equality array wrapper
- **Error pipelines**: `DiagnosticFlow<T>` — railway-oriented error accumulation
- **Domain contexts**: `AwaitableContext`, `AspNetContext`, `DisposableContext`, `CollectionContext` — cache well-known types per compilation
- **Code generation**: `IndentedStringBuilder`, `GeneratedCodeHelpers` — in `CodeGeneration.cs`
- **Extensions**: `SymbolExtensions`, `TypeSymbolExtensions`, `EnumerableExtensions`, `StringExtensions`, etc.

### Matchers Mutate

`Match.*` and `Invoke.*` matchers are mutable builders. Always create a fresh instance per pattern:

```csharp
// Correct — fresh matcher for each pattern
static MethodMatcher PublicInstance() => Match.Method().Public().NotStatic();

// Wrong — second call modifies the first matcher
var matcher = Match.Method().Public();
var async = matcher.Async();    // also mutates `matcher`!
```

### Analyzer Base Classes

Extend `DiagnosticAnalyzerBase` (auto-configures `GeneratedCodeAnalysisFlags.None` + `EnableConcurrentExecution`) and `CodeFixProviderBase<T>` for code fixes.

### Package Management

- **Central Package Management** via `Directory.Packages.props` — versions come from `Version.props`
- `ManagePackageVersionsCentrally` and `CentralPackageTransitivePinningEnabled` are both enabled
- Build output goes to `artifacts/` (configured in `Directory.Build.props`)

### Polyfills

All polyfills in `Polyfills/` are `#if`-gated by TFM and compile only on older frameworks. They're included in both `.Sources` and `.Polyfills` packages. Consumers can opt out per category (`InjectXxxOnLegacy=false`) or globally (`InjectAllPolyfillsOnLegacy=false`).
