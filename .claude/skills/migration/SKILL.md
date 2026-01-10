# MIGRATION SKILL - ANcpLua Ecosystem Consolidation

> **Status:** Phase 1 ✅ | Phase 2 ✅ | Phase 3 ⏳ Pending

---

## 🎯 EXECUTION ORDER

```
┌─────────────────────────────────────────────────────────────────┐
│  PHASE 1: Roslyn.Utilities → Source-Only Package      ✅ DONE   │
│  ════════════════════════════════════════════════════════════   │
│  Published: ANcpLua.Roslyn.Utilities.Sources v1.7.0             │
│  Published: ANcpLua.Roslyn.Utilities.Testing v1.7.0             │
├─────────────────────────────────────────────────────────────────┤
│  PHASE 2: SDK Uses Source-Only Package                ✅ DONE   │
│  ════════════════════════════════════════════════════════════   │
│  ANcpLua.NET.Sdk now references Sources package                 │
│  Analyzer test fixtures moved to Roslyn.Utilities.Testing       │
├─────────────────────────────────────────────────────────────────┤
│  PHASE 3: ErrorOrX Single-Package Migration           ⏳ TODO   │
│  ════════════════════════════════════════════════════════════   │
│  Location: ~/ErrorOrX/                                          │
│  Output: ErrorOr.nupkg (runtime + generator bundled)            │
└─────────────────────────────────────────────────────────────────┘
```

---

## PHASE 1: Source-Only Package ✅ COMPLETE

### Published Packages

| Package | Version | Purpose |
|---------|---------|---------|
| ANcpLua.Roslyn.Utilities | 1.7.0 | Compiled DLL for direct reference |
| ANcpLua.Roslyn.Utilities.Sources | 1.7.0 | Source-only for generators (embeds as internal) |
| ANcpLua.Roslyn.Utilities.Testing | 1.7.0 | Test infrastructure for analyzers/generators |

### Key Features

- **DiagnosticFlow** - Railway-oriented diagnostics pipeline
- **EquatableArray** - Immutable array with value equality for generators
- **SymbolPattern** - Composable symbol matching
- **SemanticGuard** - Declarative validation

---

## PHASE 2: SDK Integration ✅ COMPLETE

### Current State

```
~/ANcpLua.NET.Sdk/
├── src/
│   └── common/
│       └── LegacySupport.targets    ← References Sources package
└── Version.props                     ← ANcpLuaRoslynUtilitiesSourcesVersion=1.5.1
```

### How SDK Uses Sources Package

**File: `src/common/LegacySupport.targets` line 208**

```xml
<PackageReference Include="ANcpLua.Roslyn.Utilities.Sources"
                  Version="$(ANcpLuaRoslynUtilitiesSourcesVersion)"
                  PrivateAssets="all"
                  IsImplicitlyDefined="true"/>
```

### Analyzer Test Infrastructure Migration (2026-01-10)

The following files were moved from SDK to Roslyn.Utilities.Testing:

| Old Location (SDK) | New Location (Testing) |
|--------------------|------------------------|
| `Shared/AnalyzerTest.cs` | `ANcpLua.Roslyn.Utilities.Testing` |
| `Shared/CodeFixTest.cs` | `ANcpLua.Roslyn.Utilities.Testing` |
| `Shared/CodeFixTestWithEditorConfig.cs` | `ANcpLua.Roslyn.Utilities.Testing` |

**Remaining in SDK (not moved):**
- `Shared/FakeLoggerExtensions.cs`
- `Shared/IntegrationTestBase.cs`
- `Shared/KestrelTestBase.cs`

### Version Sync Note

SDK Version.props has `ANcpLuaRoslynUtilitiesSourcesVersion=1.5.1` while NuGet has 1.7.0.
This is intentional - SDK pins to tested version.

---

## PHASE 3: ErrorOrX Single-Package Migration ⏳ TODO

### Goal

Merge `ErrorOr.Core` + `ErrorOr.Endpoints` into single `ErrorOr` package.

### Current State

```
~/ErrorOrX/
├── src/
│   ├── ErrorOr.Core/                 ← net10.0 runtime
│   ├── ErrorOr.Endpoints/            ← netstandard2.0 generator
│   └── ErrorOr.Endpoints.CodeFixes/  ← netstandard2.0 codefixes
└── global.json
```

### Target State

```
~/ErrorOrX/
├── src/
│   ├── Shared/
│   │   └── ErrorType.cs              ← Single source of truth
│   ├── ErrorOr/                      ← net10.0 (runtime + attributes)
│   │   └── ErrorOr.csproj            ← Bundles generator as analyzer
│   └── ErrorOr.Generators/           ← netstandard2.0 generator
│       └── ErrorOr.Generators.csproj
└── global.json
```

### Migration Steps

1. **Create Shared/ErrorType.cs** with conditional compilation
2. **Restructure directories** (Core→ErrorOr, Endpoints→Generators)
3. **Update ErrorOr.csproj** to bundle generator as analyzer
4. **Update namespaces** (ErrorOr.Core→ErrorOr)
5. **Update solution** and test references
6. **Pack and verify** lib/ + analyzers/ structure

### ErrorOr.csproj (Target)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>ErrorOr</PackageId>
  </PropertyGroup>

  <!-- Bundle generator as analyzer -->
  <ItemGroup>
    <ProjectReference Include="../ErrorOr.Generators/ErrorOr.Generators.csproj"
                      PrivateAssets="all"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

### Expected Package Structure

```
ErrorOr.nupkg
├── lib/net10.0/ErrorOr.dll
└── analyzers/dotnet/cs/ErrorOr.Generators.dll
```

---

## QUICK REFERENCE

### Check Current Versions

```bash
# NuGet published versions
curl -s "https://api.nuget.org/v3-flatcontainer/ancplua.roslyn.utilities.sources/index.json" | jq '.versions[-1]'
curl -s "https://api.nuget.org/v3-flatcontainer/ancplua.net.sdk/index.json" | jq '.versions[-1]'

# Local Version.props
grep -E "ANcpLuaRoslynUtilities|ANcpSdkPackageVersion" ~/ANcpLua.NET.Sdk/src/common/Version.props
```

### Verify Source-Only Package

```bash
# Check contentFiles structure
unzip -l ~/.nuget/packages/ancplua.roslyn.utilities.sources/1.7.0/*.nupkg | grep contentFiles
```

### Verify SDK Package

```bash
# Check SDK includes Sources reference
grep -r "Roslyn.Utilities.Sources" ~/ANcpLua.NET.Sdk/src/
```
