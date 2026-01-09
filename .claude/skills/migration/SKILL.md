# MIGRATION SKILL - ANcpLua Ecosystem Consolidation

> **Mission:** Convert ANcpLua.Roslyn.Utilities to source-only package, remove SDK submodule, migrate ErrorOrX to single
> package.

---

## 🎯 EXECUTION ORDER (CRITICAL - DO NOT SKIP)

```
┌─────────────────────────────────────────────────────────────────┐
│  PHASE 1: Roslyn.Utilities → Source-Only Package                │
│  ════════════════════════════════════════════════════════════   │
│  Location: ~/RiderProjects/ANcpLua.Roslyn.Utilities/            │
│  Output: ANcpLua.Roslyn.Utilities.Sources.nupkg                 │
│  MUST COMPLETE BEFORE PHASE 2                                   │
├─────────────────────────────────────────────────────────────────┤
│  PHASE 2: SDK Submodule Cleanup                                 │
│  ════════════════════════════════════════════════════════════   │
│  Location: ~/ANcpLua.NET.Sdk/                                   │
│  Action: Remove submodule, use source-only package              │
│  MUST COMPLETE BEFORE PHASE 3                                   │
├─────────────────────────────────────────────────────────────────┤
│  PHASE 3: ErrorOrX Single-Package Migration                     │
│  ════════════════════════════════════════════════════════════   │
│  Location: ~/ErrorOrX/                                          │
│  Output: ErrorOr.nupkg (runtime + generator bundled)            │
│  DEPENDS ON: Phase 1 + Phase 2                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## PHASE 1: Source-Only Package

### Current State

```
~/RiderProjects/ANcpLua.Roslyn.Utilities/
├── ANcpLua.Roslyn.Utilities/
│   ├── ANcpLua.Roslyn.Utilities/           ← Main library (compiled DLL)
│   │   ├── EquatableArray.cs
│   │   ├── DiagnosticFlow.cs
│   │   ├── DiagnosticInfo.cs
│   │   └── ...
│   └── ANcpLua.Roslyn.Utilities.Testing/   ← Test helpers
├── Directory.Build.props
├── Directory.Packages.props
└── ANcpLua.Roslyn.Utilities.slnx
```

### Target State

```
~/RiderProjects/ANcpLua.Roslyn.Utilities/
├── ANcpLua.Roslyn.Utilities/
│   ├── ANcpLua.Roslyn.Utilities/           ← Compiled DLL (unchanged)
│   ├── ANcpLua.Roslyn.Utilities.Sources/   ← NEW: Source-only package
│   │   ├── ANcpLua.Roslyn.Utilities.Sources.csproj
│   │   ├── build/
│   │   │   ├── ANcpLua.Roslyn.Utilities.Sources.props
│   │   │   └── ANcpLua.Roslyn.Utilities.Sources.targets
│   │   ├── buildTransitive/
│   │   │   ├── ANcpLua.Roslyn.Utilities.Sources.props
│   │   │   └── ANcpLua.Roslyn.Utilities.Sources.targets
│   │   └── README.md
│   └── ANcpLua.Roslyn.Utilities.Testing/
└── ...
```

### Step 1.1: Create .Sources Project

```bash
cd ~/RiderProjects/ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities
mkdir -p ANcpLua.Roslyn.Utilities.Sources/build
mkdir -p ANcpLua.Roslyn.Utilities.Sources/buildTransitive
```

### Step 1.2: Create .Sources.csproj

**File: `ANcpLua.Roslyn.Utilities.Sources/ANcpLua.Roslyn.Utilities.Sources.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    
    <!-- Source-only: no DLL output -->
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <IncludeSymbols>false</IncludeSymbols>
    <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
    <DevelopmentDependency>true</DevelopmentDependency>
    
    <!-- Package metadata -->
    <PackageId>ANcpLua.Roslyn.Utilities.Sources</PackageId>
    <Description>Source-only Roslyn utilities for analyzers and generators. Embeds as internal types.</Description>
    <PackageTags>roslyn;source-generator;source-only;analyzer</PackageTags>
    
    <IsPackable>true</IsPackable>
    <NoPackageAnalysis>true</NoPackageAnalysis>
  </PropertyGroup>

  <!-- Source files as contentFiles -->
  <ItemGroup>
    <None Include="../ANcpLua.Roslyn.Utilities/**/*.cs"
          Exclude="../ANcpLua.Roslyn.Utilities/obj/**;../ANcpLua.Roslyn.Utilities/bin/**"
          Pack="true"
          PackagePath="contentFiles/cs/any/ANcpLua.Roslyn.Utilities/"
          Visible="false" />
  </ItemGroup>

  <!-- Build integration -->
  <ItemGroup>
    <None Include="build/*.props" Pack="true" PackagePath="build/" />
    <None Include="build/*.targets" Pack="true" PackagePath="build/" />
    <None Include="buildTransitive/*.props" Pack="true" PackagePath="buildTransitive/" />
    <None Include="buildTransitive/*.targets" Pack="true" PackagePath="buildTransitive/" />
  </ItemGroup>

  <!-- nuspec for contentFiles metadata -->
  <PropertyGroup>
    <NuspecFile>ANcpLua.Roslyn.Utilities.Sources.nuspec</NuspecFile>
  </PropertyGroup>
</Project>
```

### Step 1.3: Create nuspec (Required for contentFiles)

**File: `ANcpLua.Roslyn.Utilities.Sources/ANcpLua.Roslyn.Utilities.Sources.nuspec`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
  <metadata>
    <id>ANcpLua.Roslyn.Utilities.Sources</id>
    <version>$version$</version>
    <authors>ANcpLua</authors>
    <description>Source-only Roslyn utilities for analyzers and generators.</description>
    <developmentDependency>true</developmentDependency>
    <contentFiles>
      <files include="cs/any/**/*.cs" buildAction="Compile" copyToOutput="false" />
    </contentFiles>
  </metadata>
</package>
```

### Step 1.4: Create build props/targets

**File: `build/ANcpLua.Roslyn.Utilities.Sources.props`**

```xml
<Project>
  <PropertyGroup>
    <!-- Consumers can override namespace -->
    <ANcpLuaRoslynUtilitiesNamespace Condition="'$(ANcpLuaRoslynUtilitiesNamespace)' == ''">ANcpLua.Roslyn.Utilities</ANcpLuaRoslynUtilitiesNamespace>
  </PropertyGroup>
</Project>
```

**File: `build/ANcpLua.Roslyn.Utilities.Sources.targets`**

```xml
<Project>
  <!-- Source files are automatically included via contentFiles -->
  <!-- This file exists for future extensibility -->
</Project>
```

**Copy to buildTransitive/ (same content)**

### Step 1.5: Add Conditional Visibility to Source Files

Each .cs file in ANcpLua.Roslyn.Utilities needs this pattern at the top:

```csharp
// Before:
namespace ANcpLua.Roslyn.Utilities;

public readonly struct EquatableArray<T> { }

// After:
#if ANCPLUA_ROSLYN_PUBLIC
namespace ANcpLua.Roslyn.Utilities;
public
#else
namespace ANcpLua.Roslyn.Utilities;
internal
#endif
readonly struct EquatableArray<T> { }
```

**Files to modify:**

- EquatableArray.cs
- DiagnosticFlow.cs
- DiagnosticInfo.cs
- All other public types

**Shortcut:** Use sed/regex to transform all files:

```bash
# Pattern for class/struct/record/interface/enum declarations
```

### Step 1.6: Update Solution

```bash
cd ~/RiderProjects/ANcpLua.Roslyn.Utilities
dotnet sln add ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities.Sources/ANcpLua.Roslyn.Utilities.Sources.csproj
```

### Step 1.7: Build and Pack

```bash
dotnet build ANcpLua.Roslyn.Utilities.slnx
dotnet pack ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities.Sources -o ./nupkgs

# Verify package structure
unzip -l nupkgs/ANcpLua.Roslyn.Utilities.Sources.*.nupkg | grep contentFiles
```

**Expected output:**

```
contentFiles/cs/any/ANcpLua.Roslyn.Utilities/EquatableArray.cs
contentFiles/cs/any/ANcpLua.Roslyn.Utilities/DiagnosticFlow.cs
...
```

### Step 1.8: Test in Consumer

Create temp test project:

```bash
mkdir /tmp/test-sources && cd /tmp/test-sources
dotnet new classlib -n TestConsumer --framework netstandard2.0
cd TestConsumer
dotnet add package ANcpLua.Roslyn.Utilities.Sources --source ~/RiderProjects/ANcpLua.Roslyn.Utilities/nupkgs
dotnet build
```

Verify:

- Source files compile into TestConsumer.dll
- Types are `internal`
- No runtime dependency on ANcpLua.Roslyn.Utilities.dll

### Step 1.9: Publish

```bash
dotnet nuget push nupkgs/ANcpLua.Roslyn.Utilities.Sources.*.nupkg --source nuget.org
```

**PHASE 1 CHECKPOINT:**

- [ ] .Sources.nupkg created
- [ ] contentFiles structure correct
- [ ] Test consumer compiles
- [ ] Published to NuGet

---

## PHASE 2: SDK Submodule Cleanup

### Current State

```
~/ANcpLua.NET.Sdk/
├── eng/
│   ├── submodules/
│   │   └── Roslyn.Utilities/     ← Git submodule (TO REMOVE)
│   ├── .generated/
│   │   └── SourceGen/            ← Transformed .cs files (TO REMOVE)
│   └── Transform-RoslynUtilities.ps1  ← Transform script (TO REMOVE)
└── src/
    └── ANcpLua.NET.Sdk/
        └── Sdk/
            └── Sdk.targets       ← References transformed files
```

### Target State

```
~/ANcpLua.NET.Sdk/
├── eng/
│   └── (submodules removed)
└── src/
    └── ANcpLua.NET.Sdk/
        └── Sdk/
            └── Sdk.targets       ← References source-only package
```

### Step 2.1: Update SDK to Use Source-Only Package

**File: `src/ANcpLua.NET.Sdk/Sdk/Sdk.targets`**

Find where it references the transformed files and replace:

```xml
<!-- BEFORE -->
<Compile Include="$(MSBuildThisFileDirectory)../../../eng/.generated/SourceGen/**/*.cs"
         Link="Generated/%(RecursiveDir)%(Filename)%(Extension)" />

<!-- AFTER -->
<PackageReference Include="ANcpLua.Roslyn.Utilities.Sources" 
                  Version="$(ANcpLuaRoslynUtilitiesVersion)"
                  PrivateAssets="all" />
```

### Step 2.2: Remove Submodule

```bash
cd ~/ANcpLua.NET.Sdk

# Deinit
git submodule deinit eng/submodules/Roslyn.Utilities

# Remove from index
git rm eng/submodules/Roslyn.Utilities

# Remove .git/modules entry
rm -rf .git/modules/eng/submodules/Roslyn.Utilities

# Remove .gitmodules entry if it becomes empty
git add .gitmodules

# Remove generated files
rm -rf eng/.generated/SourceGen

# Remove transform script
rm eng/Transform-RoslynUtilities.ps1
```

### Step 2.3: Update Directory.Packages.props

```xml
<PackageVersion Include="ANcpLua.Roslyn.Utilities.Sources" Version="1.0.0" />
```

### Step 2.4: Build and Test SDK

```bash
dotnet build ANcpLua.NET.Sdk.slnx
dotnet test ANcpLua.NET.Sdk.slnx
```

### Step 2.5: Commit and Tag

```bash
git commit -m "chore: replace Roslyn.Utilities submodule with source-only package

BREAKING: Removes eng/submodules/Roslyn.Utilities
- Uses ANcpLua.Roslyn.Utilities.Sources NuGet package instead
- Removes Transform-RoslynUtilities.ps1 script
- Removes eng/.generated/SourceGen directory"

git tag v1.6.0
git push origin master --tags
```

### Step 2.6: Publish SDK

```bash
dotnet pack -o ./nupkgs
dotnet nuget push nupkgs/ANcpLua.NET.Sdk.*.nupkg --source nuget.org
```

**PHASE 2 CHECKPOINT:**

- [ ] Submodule removed
- [ ] Transform script removed
- [ ] SDK builds with source-only package
- [ ] SDK v1.6.0 published

---

## PHASE 3: ErrorOrX Single-Package Migration

### Current State

```
~/ErrorOrX/
├── src/
│   ├── ErrorOr.Core/                 ← net10.0 runtime
│   ├── ErrorOr.Endpoints/            ← netstandard2.0 generator
│   └── ErrorOr.Endpoints.CodeFixes/  ← netstandard2.0 codefixes
└── global.json                        ← SDK 1.5.1
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
└── global.json                        ← SDK 1.6.0
```

### Step 3.1: Update SDK Version

**File: `global.json`**

```json
{
  "sdk": { "version": "10.0.100" },
  "msbuild-sdks": {
    "ANcpLua.NET.Sdk": "1.6.0"
  }
}
```

### Step 3.2: Create Shared/ErrorType.cs

```bash
mkdir -p ~/ErrorOrX/src/Shared
```

**File: `src/Shared/ErrorType.cs`**

```csharp
#if ERROROR_GENERATOR
namespace ErrorOr.Generators.Internal;
internal enum ErrorType
#else
namespace ErrorOr;
public enum ErrorType
#endif
{
    Failure = 0,
    Unexpected = 1,
    Validation = 2,
    Conflict = 3,
    NotFound = 4,
    Unauthorized = 5,
    Forbidden = 6,
}
```

### Step 3.3: Restructure Directories

```bash
cd ~/ErrorOrX/src

# Rename ErrorOr.Core → ErrorOr
mv ErrorOr.Core ErrorOr

# Rename ErrorOr.Endpoints → ErrorOr.Generators  
mv ErrorOr.Endpoints ErrorOr.Generators

# Move Attributes into ErrorOr
mv ErrorOr.Generators/Attributes.cs ErrorOr/Endpoints/

# Remove CodeFixes (bundle into Generators or remove)
rm -rf ErrorOr.Endpoints.CodeFixes
```

### Step 3.4: Update ErrorOr.csproj

**File: `src/ErrorOr/ErrorOr.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>ErrorOr</PackageId>
    <RootNamespace>ErrorOr</RootNamespace>
  </PropertyGroup>

  <!-- Shared ErrorType -->
  <ItemGroup>
    <Compile Include="../Shared/ErrorType.cs" Link="ErrorType.cs" />
  </ItemGroup>

  <!-- Bundle generator as analyzer -->
  <ItemGroup>
    <ProjectReference Include="../ErrorOr.Generators/ErrorOr.Generators.csproj"
                      PrivateAssets="all"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

### Step 3.5: Update ErrorOr.Generators.csproj

**File: `src/ErrorOr.Generators/ErrorOr.Generators.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <RootNamespace>ErrorOr.Generators</RootNamespace>
    
    <!-- Generator settings -->
    <IsRoslynComponent>true</IsRoslynComponent>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    
    <!-- Conditional compile for internal ErrorType -->
    <DefineConstants>$(DefineConstants);ERROROR_GENERATOR</DefineConstants>
    
    <!-- Not independently packable -->
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <!-- Shared ErrorType (compiled as internal) -->
  <ItemGroup>
    <Compile Include="../Shared/ErrorType.cs" Link="Internal/ErrorType.cs" />
  </ItemGroup>

  <!-- Roslyn utilities (source-only) -->
  <ItemGroup>
    <PackageReference Include="ANcpLua.Roslyn.Utilities.Sources" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

### Step 3.6: Find/Replace Namespaces

```bash
cd ~/ErrorOrX

# ErrorOr.Core → ErrorOr
find . -name "*.cs" -exec sed -i '' 's/namespace ErrorOr\.Core/namespace ErrorOr/g' {} \;
find . -name "*.cs" -exec sed -i '' 's/using ErrorOr\.Core/using ErrorOr/g' {} \;

# ErrorOr.Endpoints.Generators → ErrorOr.Generators
find . -name "*.cs" -exec sed -i '' 's/namespace ErrorOr\.Endpoints\.Generators/namespace ErrorOr.Generators/g' {} \;
find . -name "*.cs" -exec sed -i '' 's/namespace ErrorOr\.Endpoints\.Analyzers/namespace ErrorOr.Generators.Analyzers/g' {} \;
find . -name "*.cs" -exec sed -i '' 's/using ErrorOr\.Endpoints/using ErrorOr.Generators/g' {} \;
```

### Step 3.7: Remove Duplicate ErrorType Definitions

Delete the internal ErrorType copies:

- `src/ErrorOr.Generators/Generators/Descriptors.cs` → remove `internal enum ErrorType`
- Any other duplicates

### Step 3.8: Update Solution

```bash
cd ~/ErrorOrX
dotnet sln remove src/ErrorOr.Core/ErrorOr.Core.csproj
dotnet sln remove src/ErrorOr.Endpoints/ErrorOr.Endpoints.csproj
dotnet sln remove src/ErrorOr.Endpoints.CodeFixes/ErrorOr.Endpoints.CodeFixes.csproj

dotnet sln add src/ErrorOr/ErrorOr.csproj
dotnet sln add src/ErrorOr.Generators/ErrorOr.Generators.csproj
```

### Step 3.9: Update Tests

Update test project references and namespaces.

### Step 3.10: Build and Test

```bash
dotnet build ErrorOrX.slnx
dotnet test ErrorOrX.slnx
```

### Step 3.11: Pack and Verify

```bash
dotnet pack src/ErrorOr/ErrorOr.csproj -o ./nupkgs

# Verify structure
unzip -l nupkgs/ErrorOr.*.nupkg | grep -E "(lib/|analyzers/)"
```

**Expected:**

```
lib/net10.0/ErrorOr.dll
analyzers/dotnet/cs/ErrorOr.Generators.dll
```

### Step 3.12: Update Sample Project

```xml
<!-- BEFORE -->
<PackageReference Include="ErrorOr.Core" Version="3.0.0" />
<PackageReference Include="ErrorOr.Endpoints" Version="1.0.0" />

<!-- AFTER -->
<PackageReference Include="ErrorOr" Version="4.0.0" />
```

### Step 3.13: Commit and Tag

```bash
git add -A
git commit -m "feat!: merge into single ErrorOr package (v4.0.0)

BREAKING CHANGES:
- Namespace: ErrorOr.Core → ErrorOr
- Namespace: ErrorOr.Endpoints → ErrorOr.Generators (internal)
- Single package: ErrorOr (replaces ErrorOr.Core + ErrorOr.Endpoints)

Migration:
- Replace both package references with: ErrorOr 4.0.0
- Update using statements: ErrorOr.Core → ErrorOr"

git tag v4.0.0
```

**PHASE 3 CHECKPOINT:**

- [ ] Single ErrorOr.nupkg created
- [ ] lib/net10.0/ contains ErrorOr.dll
- [ ] analyzers/dotnet/cs/ contains ErrorOr.Generators.dll
- [ ] Sample project builds
- [ ] All tests pass

---

## VERIFICATION MATRIX

| Phase | Artifact              | Verification Command                               |
|-------|-----------------------|----------------------------------------------------|
| 1     | .Sources package      | `unzip -l *.nupkg \| grep contentFiles`            |
| 2     | SDK without submodule | `ls eng/submodules/` (should be empty)             |
| 3     | ErrorOr package       | `unzip -l *.nupkg \| grep -E "(lib/\|analyzers/)"` |

## ROLLBACK PROCEDURES

### Phase 1 Rollback

```bash
cd ~/RiderProjects/ANcpLua.Roslyn.Utilities
rm -rf ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities.Sources
git checkout .
```

### Phase 2 Rollback

```bash
cd ~/ANcpLua.NET.Sdk
git checkout HEAD~1
git submodule update --init
```

### Phase 3 Rollback

```bash
cd ~/ErrorOrX
git checkout HEAD~1
```

---

## FAILURE CONDITIONS

You have **FAILED** if:

1. ❌ Execute phases out of order
2. ❌ Skip verification checkpoints
3. ❌ Publish package without testing consumer
4. ❌ Leave duplicate ErrorType definitions
5. ❌ Claim "build passes" without `dotnet build` output
6. ❌ Package missing required content (lib/, analyzers/, contentFiles/)

## SUCCESS CONDITIONS

You have **SUCCEEDED** when:

1. ✅ ANcpLua.Roslyn.Utilities.Sources published with contentFiles
2. ✅ ANcpLua.NET.Sdk builds without submodule
3. ✅ ErrorOr single package contains both runtime and generator
4. ✅ All three repos have clean `git status`
5. ✅ Consumer projects build with new packages