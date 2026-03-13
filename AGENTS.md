# AGENTS.md

**Scope:** This repository root and all child directories unless a deeper `AGENTS.md` overrides it.

## Role of this repository

This is the source of truth for `ANcpLua.Roslyn.Utilities` and downstream utility packages. Use this file before introducing new helpers.

## Hard rules

- No dependency on `ANcpLua.NET.Sdk` from this repo.
- Prefer composition reuse: check existing models/helpers before adding new code.
- Do not introduce new packages.
- Preserve public APIs unless there is a justified versioning reason.
- Use the existing coding style (nullable-aware, null-guarded, small focused helpers).
- Do not run `dotnet test` unless explicitly asked by the user.

## Preferred toolchain

- **Source search/edit/builds:** `mcp__rider__*` tools.
- **UI verification:** `mcp__playwright__*` tools.
- **No SDK dependency assumptions:** keep netstandard-first utility logic reusable.

## Runtime/compiler context

- C# 14 language features are available.
- .NET 10 SDK baseline is available.
- Core package target remains `netstandard2.0`.

## Editing guardrails

- Read each file once before changing in a run.
- Prefer targeted replacements over large rewrites.
- Keep changes scoped; avoid broad refactors without explicit direction.

## Build and delivery

- Core verification command: `dotnet build -c Release`.
- Packaging command: `dotnet pack -c Release`.
- `dotnet test` remains intentionally disabled by policy.

## Quick model map

- Core package: `src/ANcpLua.Roslyn.Utilities`
- Source-only packages: `src/ANcpLua.Roslyn.Utilities.Sources`, `src/ANcpLua.Roslyn.Utilities.Polyfills`
- Testing framework: `src/ANcpLua.Roslyn.Utilities.Testing`
- AOT reflection package: `src/ANcpLua.AotReflection`
- Attribute contracts package: `src/ANcpLua.AotReflection.Attributes`

## Check Before Writing

When implementing a new helper, check these places first in order:

1. Existing utility types (`EquatableArray`, `Result`, `DiagnosticFlow`, `Guard`, `HashCombiner`).
2. Matching DSL (`Match.*`, `Invoke.*`) before introducing custom symbol checks.
3. Existing extensions in `SymbolExtensions`, `TypeSymbolExtensions`, `OperationExtensions`.
4. Existing analyzers/tests in `ANcpLua.Roslyn.Utilities.Testing` for expected behavior.

## AOT Reflection Quick Path

- Extractors build immutable metadata models.
- Generators materialize metadata payload classes in `*.AotReflection.g.cs`.
- Keep generated metadata deterministic and null-safe.

## Practical review mindset

When auditing code, prioritize:

1. Correctness defects (nullability, accessibility, semantics).
2. Context-loss anti-patterns (duplicate logic, duplicated mappings, dead metadata fields).
3. Complexity / maintenance smell (high duplication, hidden assumptions).
4. Performance hazards only when measurable or structural.

## Decision rule

If you need behavior changes, keep blast radius local. For metadata or generation behavior changes, ensure emitted shape remains consistent and deterministic.

## Code Style Defaults

- Prefer explicit intent over cleverness.
- Keep generated code minimal and deterministic.
- Prefer helper methods for repeated construction logic.
- Keep extension methods small and single-purpose.

## Practical Reminder

If an API shape is in doubt, trust the implementation under `src/ANcpLua.Roslyn.Utilities` over stale doc snippets and update this file when mismatches appear.
