# CLAUDE.md — ANcpLua.Roslyn.Utilities

## MISSION: Migrate to ANcpLua.NET.Sdk

**Current NuGet:** 1.2.7 | **Target SDK:** 1.2.4

### Rules

- ✅ Breaking changes FINE — pre-v1.0 semantics
- ✅ DELETE PolySharp — SDK provides polyfills
- ✅ DELETE LangVersion/Nullable from Directory.Build.props
- ❌ NO fallbacks, NO "just in case" code

### Critical Facts

| Fact                        | Action                                                      |
|-----------------------------|-------------------------------------------------------------|
| SDK provides polyfills      | DELETE PolySharp entirely                                   |
| SDK sets LangVersion=latest | DELETE from Directory.Build.props                           |
| No circular dependency      | PackageId=Dummy NOT needed                                  |
| Nested project structure    | Paths: `ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities/` |

### Target State

```xml
<Project Sdk="ANcpLua.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
</Project>
```

### Commands

```bash
dotnet build
dotnet test --project ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities.Testing/
dotnet pack
```

### GitHub Actions Versions (Dec 2025)

```yaml
- uses: actions/checkout@v6
- uses: actions/setup-dotnet@v5
- uses: actions/upload-artifact@v6
```

### DELETE Checklist

- [ ] PolySharp PackageReference (Directory.Packages.props + csproj)
- [ ] `<LangVersion>` from Directory.Build.props
- [ ] `<Nullable>` from Directory.Build.props
- [ ] Any hardcoded versions outside CPM

### Architecture Role

```
ANcpLua.Roslyn.Utilities (THIS REPO)
         │
         ├──► ANcpLua.Analyzers (NuGet reference)
         │
         └──► ANcpLua.NET.Sdk (git submodule, source embedding)
```