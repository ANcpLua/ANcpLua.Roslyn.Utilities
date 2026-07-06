# TASK — TypeDeclarationInfo.GetHintName + ToDiagnostic cref fix → 2.2.32

Status: active

## Goal
Two changes, one PR, published as **2.2.32** (explicit workflow_dispatch version override):

1. **`TypeDeclarationInfo.GetHintName()`** — deterministic, collision-free hint name for
   `SourceProductionContext.AddSource`: namespace + per-level containing-type chain + type name,
   generic levels marked with `_N` arity (e.g. `Deep.Outer_1.Middle.Inner_1.g.cs`). Companion to
   the 2.2.30 `TypeDeclarationInfo`; every generator hand-rolls this.
2. **Docs fix** — ambiguous `<see cref="ToDiagnostic" />` in `Models/DiagnosticInfo.cs` remarks
   (ambiguous since the `ToDiagnostic(SyntaxTree?)` overload landed in 2.2.30) → `ToDiagnostic()`.

## Version 2.2.32 mechanics (repo publishes via dispatch, not push)
Native auto-merge (github-actions token) ⇒ no push CI run ⇒
`dispatch-publish-after-auto-merge.yml` dispatches `nuget-publish.yml` **without** a version input
(auto-bump ⇒ would be 2.2.31). To publish as 2.2.32 instead:
- After merge, **cancel the auto-dispatched CI run** during its build window (~2 min before publish).
- Then `gh workflow run nuget-publish.yml --ref main -f version=2.2.32`.
- Do NOT dispatch first: the auto-dispatcher only skips when a `push` run exists, so it would
  still dispatch and double-publish (its auto-bump would compute 2.2.33 off the new tag).

## Plan / checklist
- [x] Implement `GetHintName()` + tests (nested generic chain, global ns, arity disambiguation).
- [x] Fix ambiguous cref in `Models/DiagnosticInfo.cs` (verified: 0 ambiguous-cref warnings).
- [x] README: mention `GetHintName` in the TypeDeclarationInfo bullet.
- [x] Local verify: solution build 0 errors + 212 tests passed (4 new).
- [ ] Commit on `claude/hint-name`, push, PR (auto-merge enables itself for claude/ branches).
- [ ] After merge: cancel auto-dispatched run, dispatch with `version=2.2.32`, watch to success.
- [ ] Verify tag `v2.2.32` + nuget.org indexing (active wait ≤5 min; else report pending, verify later).
- [ ] Bump qyl `global.json` pins 2.2.30 → 2.2.32, build, PR, merge.
- [ ] Remove this file in a final `.claude/`-only PR.

## Notes
- Latest published: 2.2.30 (v2.2.30 tag). 2.2.31 intentionally skipped per request.
- Publish gate diffs `src/** tests/** README.md` since latest tag — this change opens it.
