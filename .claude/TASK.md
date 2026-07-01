# TASK — add `Delta` / `SetDelta<T>` set-difference utility + ship + wire into qyl

## Goal
Add a genuinely useful, on-theme utility to `ANcpLua.Roslyn.Utilities` (set difference for
incremental "what changed between snapshots" diffing), publish a new patch version via the repo's
trusted-publishing pipeline, and — once indexed on nuget.org — bump the reference in the
`qyl-workspace/qyl` repo.

## Plan / checklist
- [x] Study repo conventions (dual-visibility `ANCPLUA_ROSLYN_PUBLIC`, `EquatableArray<T>`, `HashCombiner`, xUnit v3 + AwesomeAssertions, GenerateDocumentationFile + CI warnings-as-errors).
- [x] Add `src/ANcpLua.Roslyn.Utilities/Delta.cs` — `Delta.Difference` (relative complement) + `Delta.Compute` → `SetDelta<T>` {Added, Removed}; ImmutableArray + EquatableArray overloads; full XML docs.
- [x] Add `tests/.../DeltaTests.cs` — 14 tests (order/dup/symmetry/overload-agreement/record-model/value-equality).
- [x] Document in packaged `README.md` (Dictionary & collection ergonomics).
- [x] Local verify: `CI=true dotnet build -c Release` (warnings-as-errors green) + full test project (174 passed, 0 failed).
- [ ] Commit on `claude/delta-set-diff`, push, open PR to main.
- [ ] CI green → merge (manual; repo auto-merge is codex/copilot-only, merge is pre-authorized).
- [ ] Push-to-main CI auto-bumps to **2.2.29**, publishes 5 pkgs via trusted publishing (OIDC), tags `v2.2.29`.
- [ ] Verify `ANcpLua.Roslyn.Utilities` 2.2.29 indexed on nuget.org.
- [ ] Bump reference in `qyl-workspace/qyl` (global.json msbuild-sdks pin 2.2.28 → 2.2.29 and/or CPM) → build green → PR.

## Notes
- Latest published/tag: 2.2.28. Next auto-bump: 2.2.29.
- Publish pipeline: `nuget-publish.yml` (name: CI). Publish job runs on push to main, gated by
  "Must Publish Packages" diff of `src/** tests/** README.md` since latest tag — this change touches all three.
- qyl consumes these as `msbuild-sdks` in `global.json` (currently `ANcpLua.Roslyn.Utilities` = 2.2.28).
