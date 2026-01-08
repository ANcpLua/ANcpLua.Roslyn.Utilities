# CLAUDE.md

## Project Overview

**ANcpLua.Roslyn.Utilities** provides reusable utilities for Roslyn incremental source generators.

**Current SDK Version:** 1.3.22 (ANcpLua.NET.Sdk)

## Build Commands

```bash
# Build
dotnet build -c Release

# Pack
dotnet pack -c Release
```

No test project exists in this repository.

## Structure

```
ANcpLua.Roslyn.Utilities/
├── ANcpLua.Roslyn.Utilities/        # netstandard2.0 - Core library
└── ANcpLua.Roslyn.Utilities.Testing/ # net10.0 - Testing framework
```

## SDK Policies (ANcpLua.NET.Sdk)

### Banned Packages

| Package | Reason | Replacement |
|---------|--------|-------------|
| `Microsoft.NET.Test.Sdk` | VSTest legacy | `xunit.v3.mtp-v2` |
| `FluentAssertions` | Abandoned | `AwesomeAssertions` |
| `xunit.runner.visualstudio` | VSTest adapter | Use MTP runner |
| `coverlet.*` | Legacy coverage | MTP built-in coverage |

### SDK-Owned Properties (DO NOT set in csproj)

- `LangVersion` - Set by SDK
- `Nullable` - Set by SDK
- `Version` / `VersionPrefix` / `VersionSuffix` - Use Directory.Build.props

### Required Configurations

**Directory.Packages.props:**
- `ManagePackageVersionsCentrally=true`
- `CentralPackageTransitivePinningEnabled=true`

**Directory.Build.props:**
- `Deterministic=true`
- `ContinuousIntegrationBuild=true` (conditional on `$(CI)`)

### GitHub Actions Versions

```yaml
- uses: actions/checkout@v6
- uses: actions/setup-dotnet@v5
- uses: actions/upload-artifact@v6
```

## Key Libraries

### Main Library (ANcpLua.Roslyn.Utilities)

- `EquatableArray<T>` - Value-equal array for generator caching
- Symbol extensions for attribute lookup, type hierarchy, accessibility
- Pipeline extensions for filtered syntax providers
- Configuration extensions for MSBuild property access

### Testing Library (ANcpLua.Roslyn.Utilities.Testing)

Fluent testing framework for incremental generators:
- Caching validation
- Forbidden type detection (ISymbol, Compilation)
- Comprehensive assertion support