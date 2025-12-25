# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
# Build entire solution
dotnet build ANcpLua.Roslyn.Utilities.slnx

# Run all tests
dotnet test ANcpLua.Roslyn.Utilities.slnx

# Build/test specific project
dotnet build ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities.csproj
dotnet build ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities.Testing/ANcpLua.Roslyn.Utilities.Testing.csproj

# Pack NuGet packages (GeneratePackageOnBuild is enabled)
dotnet pack ANcpLua.Roslyn.Utilities.slnx
```

## Architecture Relationship

This repository is the **single source of truth** for Roslyn source generator utilities across the ANcpLua ecosystem.

```
┌─────────────────────────────────────────────────────────────────────────┐
│              ANcpLua.Roslyn.Utilities (THIS REPO)                       │
│                  Single Source of Truth                                  │
└─────────────────────────────────────────────────────────────────────────┘
                    │                               │
          NuGet reference                   Git submodule
                    │                               │
                    ▼                               ▼
┌───────────────────────────────────┐   ┌─────────────────────────────────┐
│ ANcpLua.Analyzers                 │   │ ANcpLua.NET.Sdk                 │
│ (binary reference via NuGet)      │   │ (source embedding via submodule)│
│ Uses: .Testing package            │   │ Transforms: namespace, visibility│
└───────────────────────────────────┘   └─────────────────────────────────┘
```

**Two consumption patterns:**

| Consumer | Method | Why |
|----------|--------|-----|
| `ANcpLua.Analyzers` | NuGet reference to `.Testing` | Analyzers can use binary references |
| `ANcpLua.NET.Sdk` | Source embedding via submodule | Source generators cannot reference NuGet |

**SDK embedding process:**
1. Submodule at `eng/submodules/Roslyn.Utilities`
2. `Transform-RoslynUtilities.ps1` transforms files:
   - Namespace: `ANcpLua.Roslyn.Utilities` → `ANcpLua.SourceGen`
   - Visibility: `public` → `internal`
   - Adds `#if ANCPLUA_SOURCEGEN_HELPERS` guard
3. Output: `eng/.generated/SourceGen/` (gitignored)

## Architecture Overview

This is a two-package library for Roslyn incremental source generator development:

**ANcpLua.Roslyn.Utilities** (netstandard2.0) - Core utilities consumed by generator projects:
- `EquatableArray<T>` - Value-equal immutable array wrapper essential for generator caching (Roslyn requires value equality for incremental caching to work)
- Pipeline extensions (`AddSource`, `CollectAsEquatableArray`, `SelectAndReportExceptions`) for building incremental pipelines
- Syntax extensions (`ForAttributeWithMetadataNameOfClassesAndRecords`, `SelectAllAttributes`)
- String utilities for code generation (`ToPropertyName`, `ToParameterName`, `ExtractNamespace`, `WithGlobalPrefix`)
- Models: `FileWithName`, `ResultWithDiagnostics<T>`, `ClassWithAttributesContext`

**ANcpLua.Roslyn.Utilities.Testing** (net10.0) - Fluent testing framework:
- `GeneratorTest` static extensions: `ShouldGenerate<T>()`, `ShouldHaveDiagnostics<T>()`, `ShouldBeCached<T>()`
- `GeneratorTester<T>` instance API for shared test configuration
- Caching validation detects forbidden cached types (ISymbol, Compilation, SyntaxNode, SemanticModel, SyntaxTree)
- Built on AwesomeAssertions and Microsoft.CodeAnalysis testing packages

## Key Roslyn Concepts

**Value equality is critical**: Incremental generators only skip work when cached values are equal. `EquatableArray<T>` exists because `ImmutableArray<T>` uses reference equality. Any model stored in pipeline outputs must implement value-based `Equals`/`GetHashCode`.

**Forbidden types in caching**: Never cache ISymbol, Compilation, SyntaxNode, SemanticModel, or SyntaxTree - these contain compilation state and break incremental caching. Extract only the primitive data you need.

## Code Patterns

- Extension method chains are the primary API style (sealed static classes with extension methods)
- `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on performance-critical paths (e.g., EquatableArray accessors)
- File-scoped namespaces, nullable references enabled throughout
- Record structs for immutable data models
- XML documentation with code examples on public APIs

## Key Files

| File | Purpose |
|------|---------|
| `EquatableArray.cs` | Value-equal immutable array wrapper (critical for caching) |
| `SymbolExtensions.cs` | HasAttribute, GetAttribute, IsOrInheritsFrom |
| `SyntaxExtensions.cs` | GetMethodName, HasModifier, IsPartial |
| `SemanticModelExtensions.cs` | IsConstant, GetConstantValueOrDefault |
| `IncrementalValuesProviderExtensions.cs` | AddSource, SelectAndReportExceptions |
| `Models/DiagnosticInfo.cs` | Cache-safe diagnostic representation |
| `Models/LocationInfo.cs` | Cache-safe location representation |
| `Models/EquatableMessageArgs.cs` | Value-equal message arguments |
