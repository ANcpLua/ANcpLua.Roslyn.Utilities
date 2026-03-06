# CLAUDE.md - ANcpLua.Roslyn.Utilities.Testing

## Scope

This file governs testing utilities under `src/ANcpLua.Roslyn.Utilities.Testing`.

## Core contracts

- Keep testing helpers stable and deterministic.
- Do not change public test API behavior without strong reason.
- Prefer compatibility in assertions and builder APIs.

## Tooling preference

- Use `mcp__rider__*` for search/edit/build diagnostics.
- Use `mcp__playwright__*` for UI verification only.

## Build policy

- `dotnet build -c Release`
- `dotnet pack -c Release`
- `dotnet test` remains opt-in by explicit request.

## Review mindset

When making changes, prioritize:

1. Determinism of generated outputs and assertions.
2. Test ergonomics (clear, low-noise failures).
3. API consistency (constructor and fluent-builder patterns).
4. Avoid widening behavioral scope accidentally.

## Modernization rule

Favor tiny refactors: extract shared logic over broad rewrites; preserve test semantics and output expectations.
