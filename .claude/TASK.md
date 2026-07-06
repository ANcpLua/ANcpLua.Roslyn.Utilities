# TASK — two arcs: (A) hygiene: real warnings-as-errors + zero-warning build → 2.2.34; (B) features: GetFullyQualifiedMetadataName + cachability assertions → 2.2.35

Status: active

## Arc A — hygiene (this branch: `claude/warnings-as-errors`)
The `TreatWarningsAsErrors` gate keys on `ContinuousIntegrationBuild`, which CI never sets — so CI
builds green with ~236 unique analyzer warnings. Plan:
- [x] Map `CI=true` → `ContinuousIntegrationBuild=true` in `Directory.Build.props`.
- [x] Fixed in code: CA1305 (TypeCache invariant culture), CA1307 (Guard.Path ordinal Contains,
      test Replace calls), CA2007 (Testing await-using ConfigureAwait scope pattern ×5),
      CA1063 (SolutionRefactoringTest full Dispose pattern), CA1859 (3 private helpers → concrete
      return types), CA1822 (dead Dispose on private NonGenericEnumerator removed), RS2008
      (pragma, test-only descriptor), CA1849 (pragma ×2 — CancelAsync unavailable on netstandard2.0),
      plus 6 xUnit1051 errors the armed gate caught in the 2.2.30–33 tests.
- [x] Configured off with written justification in `.editorconfig`: CA1062 (redundant with
      enforced NRT), CA1815, CA1031, CA1034, CA1054/CA1055, CA1028, CA1024, CA1710/CA1711/CA1721,
      CA2225, CA1819.
- [x] `CI=true` clean rebuild: **0 warnings, 0 errors**; 215 tests passed.
- [ ] PR → auto-merge → publish **2.2.34** (auto-bump), verify tag+index, bump qyl.

## Arc B — features (branch: `claude/metadata-name-caching-assertions`, after A merged)
**Scope correction:** the pitched cachability assertion already exists —
`GeneratorResult.IsCached(params string[])` is the run-twice, all-steps-cached, forbidden-types
one-liner, more complete than proposed. Arc B is therefore the honest remainder: complete the
TypeDeclarationInfo round-trip surface.
- [x] `GetFullyQualifiedMetadataName()` — ``Deep.Outer`1+Middle+Inner`1``; round-trip test via
      `GetTypeByMetadataName` resolves the original symbol (SymbolEqualityComparer).
- [x] `GetFullyQualifiedName()` — `global::Deep.Outer<T>.Middle.Inner<U>`; compile-test proves it
      is valid as a self-reference inside the generated partial.
- [x] Tests (6 new, 221 total green), README, clean rebuild 0 warnings / 0 errors under armed gate.
- [ ] PR → merge → publish **2.2.35**, verify, bump qyl 2.2.33 → 2.2.35 (2.2.34 qyl bump
      intentionally skipped — consolidated into one bump since both arcs were commissioned together).
- [ ] Remove this file in a final `.claude/`-only PR.

## Notes
- Latest: 2.2.33 (qyl on 2.2.33 via qyl#492).
- cref discipline: overloaded members always get explicit parameter lists (memory: cref-overloads-explicit).
- Known ambient: none — ambiguous crefs were cleared in 2.2.33.
