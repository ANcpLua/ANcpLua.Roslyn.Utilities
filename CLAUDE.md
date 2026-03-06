# CLAUDE.md - ANcpLua.Roslyn.Utilities

**Source of truth:** `ANcpLua.Roslyn.Utilities` is the foundation package for downstream Roslyn tooling in this ecosystem. Use this file as the first reference before authoring helpers.

## Scope and constraints

- This repo should remain independent of `ANcpLua.NET.Sdk`.
- Modernize by reuse: prefer existing utility types over new abstraction.
- Keep packages coherent by preserving existing public behavior unless a behavior bug is fixed.
- Prefer small, composable changes over broad rewrites.

## Platform context

- Language: C# 14
- SDK baseline: .NET 10 (Nov 2025+)
- Core package target: `netstandard2.0`

## Build commands

- `dotnet build -c Release`
- `dotnet pack -c Release`
- (By policy) do not run `dotnet test` unless explicitly requested.

## Core packages in this repo

- `ANcpLua.Roslyn.Utilities`: runtime + Roslyn helpers.
- `ANcpLua.Roslyn.Utilities.Sources`: source-only package for internalized helpers.
- `ANcpLua.Roslyn.Utilities.Polyfills`: API backports, no Roslyn dependency.
- `ANcpLua.Roslyn.Utilities.Testing`: test infrastructure for analyzers/generators.
- `ANcpLua.AotReflection`: source generator + metadata emission pipeline.
- `ANcpLua.AotReflection.Attributes`: attribute contracts consumed by reflection metadata.

## “Check before writing” workflow

When implementing a new helper, check these places first in order:

1. Existing utility types (`EquatableArray`, `Result`, `DiagnosticFlow`, `Guard`, `HashCombiner`).
2. Matching DSL (`Match.*`, `Invoke.*`) before introducing custom symbol checks.
3. Existing extensions in `SymbolExtensions`, `TypeSymbolExtensions`, `OperationExtensions`.
4. Existing analyzers/tests in `ANcpLua.Roslyn.Utilities.Testing` for expected behavior.

## AOT Reflection pipeline quick path

- Extractors build immutable metadata models.
- Generators materialize metadata payload classes in `*.AotReflection.g.cs`.
- Keep generated metadata deterministic and null-safe.

## Recommended modernization priorities

1. Correctness first (semantics, accessibility, constants, nullability).
2. Metadata completeness (avoid dropping extracted fields).
3. Composition cleanup (reduce repeated patterns in generators/extractors).
4. Anti-pattern reduction only after tests/behavior review proves it is safe.

## Review rubric

When reviewing code, rank issues by:

- **P1:** Behavioral correctness / semantic mismatch.
- **P2:** Safety risk (nullability, reflection edge-cases, overflow). 
- **P3:** Maintainability (duplication, brittle stringly typing).
- **P4:** Style and performance unless proven impact.

## Code style defaults

- Prefer explicit intent over cleverness.
- Keep generated code minimal and deterministic.
- Prefer helper methods for repeated construction logic.
- Keep extension methods small and single-purpose.

## Practical reminder

If you need an API shape in doubt, follow the implementation in `src/ANcpLua.Roslyn.Utilities` rather than docs snippets. This file may lag implementation details and should be corrected when mismatches appear.