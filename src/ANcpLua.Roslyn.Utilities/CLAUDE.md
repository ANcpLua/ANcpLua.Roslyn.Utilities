# CLAUDE.md - ANcpLua.Roslyn.Utilities

## Scope

This file is authoritative for `src/ANcpLua.Roslyn.Utilities` and its subfolders.

## Core contracts

- **No `ANcpLua.NET.Sdk` dependency** in this package.
- Reuse existing helpers before writing new ones.
- Preserve public behavior unless a correctness issue is fixed.
- Prefer minimal, composable changes.

## Modernization priorities

1. Correctness (nullability, semantics, reflection/matching correctness).
2. Completeness (emit/consume fields consistently; avoid dropped model data).
3. Maintainability (remove duplication, reduce brittle stringly-typed logic).
4. Performance only where behavioral or structural risk exists.

## Tooling preference

- Use `mcp__rider__*` for discovery, edits, diagnostics.
- Use `mcp__playwright__*` only for UI/UX verification.

## Build policy

- `dotnet build -c Release`
- `dotnet pack -c Release`
- `dotnet test` only on explicit instruction.

## AOT reflection quick rules (within this subtree)

- Keep generated metadata deterministic.
- Prefer explicit helper methods over repeated ad-hoc codegen formatting.
- Preserve source-level behavior when refactoring generators.

## Language/runtime

- C# 14 available.
- Use current APIs where available but keep `netstandard2.0` compatibility in utility surfaces.
