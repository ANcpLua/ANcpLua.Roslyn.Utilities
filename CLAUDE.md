# CLAUDE.md

## CRITICAL: Dependency Direction

```
THIS REPO IS UPSTREAM - IT CANNOT DEPEND ON ANcpLua.NET.Sdk

Layer 0: ANcpLua.Roslyn.Utilities ← YOU ARE HERE (publishes first)
    ↓
Layer 1: ANcpLua.NET.Sdk (consumes this package)
    ↓
Layer 2: Downstream repos (consume SDK)
```

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

### Testing Library (ANcpLua.Roslyn.Utilities.Testing)

Fluent testing framework for incremental generators:
- Caching validation
- Forbidden type detection (ISymbol, Compilation)
- Comprehensive assertion support

## Polyfills

This repo uses `PolySharp` for netstandard2.0 polyfills (IsExternalInit, NotNullWhen, etc.) instead of SDK-provided polyfills.
