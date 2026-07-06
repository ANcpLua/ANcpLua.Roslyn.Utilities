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
- [x] After merge: cancel auto-dispatched run (28765141614), dispatch with `version=2.2.32` → success.
- [x] Verify tag `v2.2.32` + nuget.org indexing (both packages, ~4 min).
- [x] Bump qyl `global.json` pins 2.2.30 → 2.2.32 (qyl#490, merged).

## Extension: CodeRabbit findings on #152 → hint-name encoding fix (→ 2.2.33)
Both PR #152 review findings survived adversarial verification (real, constructible collisions
in an API documented as collision-free):
1. `_N` arity suffix is forgeable by an identifier: `class Result_1` vs `Result<T>` collide.
2. `.` for both namespace and nesting: `A { B { C } }` vs `A.B { C }` collide.
Fix (before anyone depends on the format): arity `(N)`, nesting separator `-` — characters no
identifier can contain, so the encoding is injective. Verified accepted by Roslyn hint-name
validation via a real CSharpGeneratorDriver probe test.
- [x] New encoding + regression tests for both collision classes + driver-validation test (215 passed).
- [x] Fix new ambiguous `AddSource` cref from #152 while touching the file.
- [ ] PR `claude/hint-name-collisions`, auto-merge, auto-publish **2.2.33** (normal auto-bump — no override).
- [ ] Verify v2.2.33 + indexing, bump qyl 2.2.32 → 2.2.33.
- [ ] Remove this file in a final `.claude/`-only PR.

## Notes
- Latest published: 2.2.30 (v2.2.30 tag). 2.2.31 intentionally skipped per request.
- Publish gate diffs `src/** tests/** README.md` since latest tag — this change opens it.
