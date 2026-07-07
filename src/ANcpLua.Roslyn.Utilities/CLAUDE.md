# ANcpLua.Roslyn.Utilities

Core utilities (netstandard2.0) for Roslyn incremental generators/analyzers:
`EquatableArray`, pipeline extensions, `Guard`, diagnostic and code-generation
helpers.

- Project: [ANcpLua.Roslyn.Utilities.csproj](ANcpLua.Roslyn.Utilities.csproj)
- Source of truth: the `*.cs` files here (`EquatableArray.cs`, `SymbolExtensions.cs`, `OperationExtensions.cs`, `Guard.*.cs`) and folders `Async/`, `Caching/`, `Text/`, `Web/`.
- Hot-path invariants: root `AGENTS.md`.
