# CLAUDE.md - ANcpLua.Roslyn.Utilities

Reusable utilities for Roslyn incremental source generators.

## Ecosystem Position

```
LAYER 0: ANcpLua.Roslyn.Utilities  <-- YOU ARE HERE (UPSTREAM)
         | publishes .Sources
LAYER 1: ANcpLua.NET.Sdk           <-- SOURCE OF TRUTH (Version.props)
         | auto-syncs Version.props
LAYER 2: ANcpLua.Analyzers         <-- DOWNSTREAM (uses SDK)
         | consumed by
LAYER 3: qyl, other projects       <-- END USERS
```

### This Repo: LAYER 0 (Upstream)

| Property | Value |
|----------|-------|
| **Upstream dependencies** | None (Microsoft.NET.Sdk only) |
| **Downstream consumers** | ANcpLua.NET.Sdk |
| **Version.props** | CUSTOM (own Roslyn versions) |
| **Auto-sync** | NO (manual sync only) |

## CRITICAL: Dependency Direction

**THIS REPO IS UPSTREAM - IT CANNOT DEPEND ON ANcpLua.NET.Sdk**

**NEVER add:**
- `Sdk="ANcpLua.NET.Sdk"` in any csproj
- `msbuild-sdks` referencing ANcpLua.NET.Sdk in global.json
- Any PackageReference to ANcpLua.NET.Sdk packages

CI will fail if these are detected.

## Build Commands

```bash
# Build
dotnet build -c Release

# Pack
dotnet pack -c Release
```

## Structure

```
ANcpLua.Roslyn.Utilities/
├── ANcpLua.Roslyn.Utilities/        # netstandard2.0 - Core library
└── ANcpLua.Roslyn.Utilities.Testing/ # net10.0 - Testing framework
```

## Key Libraries

### Main Library (ANcpLua.Roslyn.Utilities)

- `EquatableArray<T>` - Value-equal array for generator caching
- Symbol extensions for attribute lookup, type hierarchy, accessibility
- Pipeline extensions for filtered syntax providers
- Configuration extensions for MSBuild property access
- Code generation helpers: `IndentedStringBuilder`, `ValueStringBuilder`, `TypedConstantExtensions`

### Testing Library (ANcpLua.Roslyn.Utilities.Testing)

Comprehensive testing framework for Roslyn tooling:

**Generator Testing:**
- `Test<TGenerator>` - Fluent API entry point for generator tests
- `GeneratorResult` - Assertions for output files, caching, diagnostics
- `GeneratorTestEngine<TGenerator>` - Low-level test compilation engine
- Caching validation and forbidden type detection (ISymbol, Compilation)

**Analyzer Testing:**
- `AnalyzerTest<TAnalyzer>` - Base class for analyzer tests
- `CodeFixTest<TAnalyzer, TCodeFix>` - Base class for code fix tests
- `CodeFixTestWithEditorConfig` - Tests with EditorConfig support

**MSBuild Integration Testing:**
- `ProjectBuilder` - Fluent builder for isolated .NET project tests
- `BuildResult` + `BuildResultAssertions` - SARIF parsing, binary log analysis
- `DotNetSdkHelpers` - Automatic SDK download and caching
- MSBuild constants: `Tfm`, `Prop`, `Val`, `Item`, `Attr`

## Polyfills

This repo uses `PolySharp` for netstandard2.0 polyfills (IsExternalInit, NotNullWhen, etc.) instead of SDK-provided polyfills.

## Release Order (CRITICAL!)

This repo must be published FIRST before SDK can update:

```
1. Roslyn.Utilities -> publish to NuGet  <-- YOU ARE HERE
2. SDK -> update Version.props -> publish to NuGet
3. THEN sync Version.props to Analyzers
4. Analyzers -> can now build
```

## Common CI Errors

### SDK Version Not Found (in downstream repos)
```
error: Unable to find package ANcpLua.NET.Sdk with version (= X.X.X)
```

**Cause:** global.json references SDK version not yet published to NuGet.

**Fix Options:**
1. **Downgrade:** Change global.json to latest published version
2. **Publish:** Tag and push in SDK repo: `git tag vX.X.X && git push --tags`

**Prevention:** Always publish SDK BEFORE syncing version to downstream repos.
