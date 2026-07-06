# TASK — TypeDeclarationInfo + tree-bound location round-trip

Status: active

## Goal
Ship two utilities inspired by (and fixing flaws in) Microsoft.Agents.AI.Workflows.Generators models:

1. **`TypeDeclarationInfo`** (`Models/`) — cacheable, value-equatable snapshot of a type declaration
   for incremental generators: namespace, name, declaration keyword (class/struct/record/record
   struct/interface), generic parameter clause, partial-ness, and the containing-type chain as
   **per-level declarations** (keyword + name + generic clause + partial-ness), not a flat name
   string — so nested-in-generic and nested-in-struct emission actually compiles.
   Plus `From(INamedTypeSymbol)` factory and `WriteOpening`/`WriteClosing` emit helpers for the
   nested partial wrapper.
2. **Tree-bound location round-trip** — `LocationInfo.ToLocation(SyntaxTree?)` +
   `DiagnosticInfo.ToDiagnostic(SyntaxTree?)` overloads that bind the recreated `Location` to a live
   syntax tree when available (IDE navigation/squiggles), falling back to the path-based form.

Then release via trusted publishing (auto-bump 2.2.30) and bump qyl.

## Plan / checklist
- [x] Implement `Models/TypeDeclarationInfo.cs` (+ `ContainingTypeInfo` level record + `TypeDeclarationScope`).
- [x] Add `ToLocation(SyntaxTree?)` to `Models/LocationInfo.cs`, `ToDiagnostic(SyntaxTree?)` to `Models/DiagnosticInfo.cs`.
- [x] **Bonus bug fix found by new tests:** `GetGenericParameterClause` used `IsGenericType`, which is true when any *containing* type is generic → returned bogus `"<>"` for non-generic nested types. Now keyed on `TypeParameters.Length`.
- [x] Tests in `tests/ANcpLua.Roslyn.Utilities.Testing.Tests/` (TypeDeclarationInfoTests ×11, TreeBoundLocationTests ×5).
- [x] Document in packaged `README.md`.
- [x] Local verify: `CI=true dotnet build ANcpLua.Roslyn.Utilities.slnx -c Release` (0 errors) + full test run (208 passed, 0 failed).
- [ ] Commit on `claude/type-declaration-info`, push, open PR to main.
- [ ] CI green → merge.
- [ ] Push-to-main CI auto-bumps to **2.2.30**, publishes via trusted publishing, tags `v2.2.30`.
- [ ] Verify 2.2.30 indexed on nuget.org.
- [ ] Bump qyl `global.json` msbuild-sdks 2.2.29 → 2.2.30, PR, merge.
- [ ] Update this file's Status to done **inside the final PR** (or delete it there).

## Notes
- Dual-visibility via `ANCPLUA_ROSLYN_PUBLIC`; XML docs mandatory (CI warnings-as-errors).
- MS flaw being fixed: their `ContainingTypeChain` is a flat `"Outer.Middle"` string — loses
  container type kind + generics, so emitted `partial class Outer {` fails for `struct`/generic containers.
- Latest published: 2.2.29. Next auto-bump: 2.2.30.
