# Changelog

All notable changes to ANcpLua.Roslyn.Utilities will be documented in this file.

## [1.7.0] - 2026-01-10

### Added

- **DisposableContext** - Pre-cached symbol lookups for disposable patterns
  - `IsDisposable()`, `IsSyncDisposable()`, `IsAsyncDisposable()`
  - `HasDisposeMethod()`, `HasDisposeAsyncMethod()`
  - `IsInsideUsing()`, `IsUsingStatement()`
  - Type checks: `IsStream()`, `IsDbConnection()`, `IsHttpClient()`, `IsSynchronizationPrimitive()`
  - `ShouldBeDisposed()` with smart filtering (e.g., HttpClient typically injected)

- **ANcpLua.Roslyn.Utilities.Testing** - Complete analyzer test infrastructure
  - `AnalyzerTest<TAnalyzer>` base class
  - `CodeFixTest<TAnalyzer, TCodeFix>` base class
  - `CodeFixTestWithEditorConfig<TAnalyzer, TCodeFix>` base class
  - Migrated from ANcpLua.NET.Sdk to centralize test infrastructure

### Changed

- Consolidated extension methods and removed redundant code
- Suppressed CS1591 warnings for missing XML docs

## [1.6.0] - 2026-01-09

### Added

- **Sources package** (`ANcpLua.Roslyn.Utilities.Sources`)
  - Source-only NuGet for embedding utilities as internal types
  - Conditional compilation with `ANCPLUA_ROSLYN_PUBLIC` define

- **Domain Contexts**
  - `AwaitableContext` - Task/ValueTask/async pattern detection
  - `AspNetContext` - Controller/Action/attribute detection
  - `CollectionContext` - Immutable/enumerable type detection

### Changed

- Dual-package architecture: compiled DLL + source-only options
- Version synchronization with ANcpLua.NET.Sdk

## [1.5.0] - 2026-01-08

### Added

- **DiagnosticFlow** - Railway-oriented diagnostics pipeline
- **EquatableArray** - Immutable array with value equality
- **SymbolPattern** - Composable symbol matching
- **SemanticGuard** - Declarative validation

### Changed

- Upgraded to .NET 10
- Added allocation-free extensions
