# Copilot PR-review instructions for ANcpLua.Roslyn.Utilities

Utility library for Roslyn incremental generators, analyzers, and code-fix
providers. Ships five NuGet packages: a binary library, a `Sources` source-only
package (rewritten to `internal` at pack via `Transform-Sources.ps1`), `Polyfills`,
`Testing` (analyzer/generator/codefix harness), and `Testing.Aot`. Main library +
`Sources` target `netstandard2.0` so Roslyn-hosted consumers can absorb them;
`Testing` targets `net10.0`. `TreatWarningsAsErrors=true` repo-wide; all analyzer
diagnostics elevated to error in `.editorconfig`. This file scopes to PR review only.

## Flag

- New API in the main library (or `Sources`) that pulls in dependencies not
  available on `netstandard2.0` — `System.Text.Json`, modern BCL methods, anything
  ns2.1+. Such code can land in `Testing` or downstream consumers, but not in
  the analyzer-hosted surface.
- New `public` type or member added to the `Sources` project without a
  `#if ANCPLUA_ROSLYN_PUBLIC public #else internal #endif` guard — the Sources
  package rewrites visibility on pack, so adding bare `public` breaks the
  internal-on-pack contract.
- Storing `ISymbol`, `SyntaxNode`, `SyntaxTree`, or any Roslyn reference type as
  a `HashSet<T>` / `Dictionary<TKey, …>` key — reference equality, cache misses
  on rebuild, breaks generator incrementality.
- New allocations in generator hot paths: `.ToArray()` / `.ToList()` on a `Span`
  inside `Append` loops, capturing closures in `Dict.GetOrAdd`/`GetOrInsert`,
  boxing of value types via `object`. The `closure-free` `GetOrInsert<TContext>`
  + `static` lambda + `ValueStringBuilder` patterns are the established shape.
- Public types intended for generator payloads added without value equality —
  use `readonly record struct` (or `EquatableArray<T>` for collections), never
  classes or non-record structs.
- File-I/O via `System.IO.File` / `System.IO.Directory` outside `Guard.cs` —
  `Guard.cs` is the single allow-listed call site (suppressed `RS1035`).

## utilities-specific

- The `Sources` package is for source generators that can't take a binary
  reference. Any new helper meant for that consumer set goes in
  `src/ANcpLua.Roslyn.Utilities/` (the shared tree) and gets exposed via the
  `Sources` package on pack.
- `Polyfills` package supplies `init`, `required`, `Index`/`Range`, nullable +
  trim attributes for `netstandard2.0` consumers. Don't duplicate polyfills in
  the main library.
- `Testing` ships a fluent generator/analyzer/codefix harness. New test helpers
  belong there, not in individual consumer test projects.
- `EquatableArray<T>` is `ref`-struct-like (value-equality wrapper); use it for
  collection fields in generator records, not `ImmutableArray<T>` (no equality)
  or `T[]` (reference equality).

## Do not flag

- Allow-listed suppressions in `Guard.cs`: `#pragma warning disable RS1035`
  (file I/O legal here, non-analyzer call sites only).
- Polyfills suppressions: `CA1019`, `RCS1251`, `IDE0300`, `CA1064`, `CA1812`,
  `SA1623`, `RCS1157` — all on shim types.
- Testing-csproj suppressions: `NU1903`, `RS1036`, `RS1038`, `RS1041`, `CA1019`,
  `NU5104`, `CA1859`, `RS0030`, `CA1307`, `IDE1006`, `CA1002`, `CA1000`,
  `CS1574`, `CS1591` — test-infra concessions.
- `[ExcludeFromCodeCoverage]` only appears on polyfills, not on mainline types
  — that's intentional, mainline gets coverage.
- `Testing` package's use of `System.Text.Json` and modern BCL — it's `net10.0`,
  not the constrained surface.

## Project context

Solo-dev repo. The Sources package is consumed by `ANcpLua.Analyzers` and
`ANcpLua.Agents.Testing` (and downstream generators); regressions ripple. Breaking
changes are allowed in the same session — bump major, fix consumers, ship.
Don't suggest backwards-compat shims or feature flags within a single PR.
