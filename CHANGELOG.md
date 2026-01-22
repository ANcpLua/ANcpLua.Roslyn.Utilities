# Changelog

All notable changes to ANcpLua.Roslyn.Utilities will be documented in this file.

## [1.18.0] - 2026-01-22

### Added

- **Guard** - Lazy `Func<T>` overloads for all `OrElse` methods (performance optimization)
  - `NotNullOrElse<T>(T?, Func<T>)` - Reference type lazy fallback
  - `NotNullOrElse<T>(T?, Func<T>)` - Value type lazy fallback
  - `NotNullOrEmptyOrElse(string?, Func<string>)` - String lazy fallback
  - `NotNullOrWhiteSpaceOrElse(string?, Func<string>)` - Whitespace-aware lazy fallback

- **CompilationExtensions** - Added `IsNet10OrGreater()` for .NET 10 target framework detection

### Changed

- **Guard** - Renamed `NotNullOr*` methods to `NotNullOrElse*` for clarity
  - **BREAKING:** `NotNullOr<T>` → `NotNullOrElse<T>`
  - **BREAKING:** `NotNullOrEmptyOr` → `NotNullOrEmptyOrElse`
  - **BREAKING:** `NotNullOrWhiteSpaceOr` → `NotNullOrWhiteSpaceOrElse`

- **ObjectExtensions** - Removed redundant methods that duplicated `NullableExtensions`
  - **BREAKING:** Removed `IfNotNull<T>(Action<T>)` → Use `NullableExtensions.Do()`
  - **BREAKING:** Removed `IfNotNull<T,TResult>(Func<T,TResult>)` → Use `NullableExtensions.Select()`
  - **BREAKING:** Removed `IfNotNull<T,TResult>(Func<T,TResult>, TResult)` → Use `.Select().Or()`
  - **BREAKING:** Removed `EqualsTo<T>()` → Use native `Equals()` or `==`

### Migration Guide

**Guard renames:**
```csharp
// BEFORE                                    // AFTER
Guard.NotNullOr(value, fallback)            → Guard.NotNullOrElse(value, fallback)
Guard.NotNullOrEmptyOr(str, fallback)       → Guard.NotNullOrEmptyOrElse(str, fallback)
Guard.NotNullOrWhiteSpaceOr(str, fallback)  → Guard.NotNullOrWhiteSpaceOrElse(str, fallback)

// NEW: Lazy evaluation for expensive defaults
Guard.NotNullOrElse(value, () => ExpensiveDefault())
```

**ObjectExtensions removals:**
```csharp
// BEFORE                                    // AFTER
obj.IfNotNull(x => x.Length)                → obj.Select(x => x.Length)
obj.IfNotNull(x => Process(x))              → obj.Do(x => Process(x))
obj.IfNotNull(x => x.Name, "default")       → obj.Select(x => x.Name).Or("default")
obj.EqualsTo(other)                         → obj?.Equals(other) ?? false
```

---

## [1.17.0] - 2026-01-22

### Added

- **StringExtensions** - Type name manipulation utilities
  - `StripGlobalPrefix()` - Remove `global::` prefix from type names
  - `NormalizeTypeName()` - Remove all `global::` prefixes and trailing nullable marker
  - `UnwrapNullable()` - Unwrap `Nullable<T>` or `T?` to underlying type
  - `ExtractShortTypeName()` - Get short name from FQN (e.g., "List" from "System.Collections.Generic.List")
  - `GetCSharpKeyword()` - Map BCL types to C# keywords (System.Int32 → int)
  - `TypeNamesEqual()` - Compare type names handling aliases and prefixes
  - `IsStringType()`, `IsPrimitiveJsonType()` - Type classification helpers
  - `StripSuffix()`, `StripPrefix()` - Suffix/prefix removal utilities

### Changed

- Updated Version.props SDK reference to 1.6.25

## [1.11.0] - 2026-01-14

### Fixed

- **ForbiddenTypeAnalyzer** stack overflow on recursive type hierarchies
- **IsCached** step filtering for generator caching validation

### Changed

- Documentation moved to [ANcpLua Docs](https://ancplua.mintlify.app)
- Updated ANcpLuaAnalyzersVersion to 1.6.1
- Updated metadata and docs to mention `ValueStringBuilder` and `TypedConstantExtensions`

## [1.10.0] - 2026-01-12

### Changed

- **ProjectBuilder** now supports inheritance for customization

## [1.9.0] - 2026-01-11

### Added

- Comprehensive XML documentation across all public APIs

## [1.8.0] - 2026-01-10

### Added

- **MSBuild project testing infrastructure** in Testing library
  - `ProjectBuilder` - Fluent builder for isolated .NET project tests
  - `BuildResult` + `BuildResultAssertions` - SARIF parsing, binary log analysis
  - `DotNetSdkHelpers` - Automatic SDK download and caching
  - MSBuild constants: `Tfm`, `Prop`, `Val`, `Item`, `Attr`

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
