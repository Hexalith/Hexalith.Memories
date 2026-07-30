---
title: 'Fix story-gate hook sequencing'
type: 'bugfix'
created: '2026-07-30'
status: 'done'
baseline_commit: '4a6f0d33689fde8335b5c7a8d429d885fa82040a'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-30-story-gate-hook-sequencing.md'
  - '{project-root}/_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md'
---

<frozen-after-approval reason="Administrator-approved intent — do not widen without a new decision">

## Intent

**Problem:** The local pre-commit hook runs before a proposed commit message exists but treats an
unresolved owner as terminal. On main, this prevents the later commit-msg hook from reading the
Story or Story-Key trailer that Story 12.3 explicitly permits.

**Approach:** Add a phase-specific defer-unresolved-owner mode to the shared validator and use it
only from pre-commit. Owner-named branches continue to receive immediate scope validation.
commit-msg and CI remain definitive and fail closed.

## Boundaries & Constraints

**Always:** Require one exact existing story/spec owner before a changed commit is created; run the
full existing File Scope check whenever an owner resolves; preserve conflict, forbidden-default,
override, and CI behavior; keep shell wrappers thin.

**Ask First:** Relaxing the final owner requirement; using deferral in commit-msg or CI; changing
scope-override authority; editing a completed story, epic, sprint-status, runtime path, or submodule
pointer.

**Never:** Defer malformed, partial, multiple, or conflicting owner metadata; bypass hooks; stage,
commit, reset, or overwrite existing user work; change product behavior.

## I/O & Edge-Case Matrix

| Scenario | Input / state | Expected behavior |
| :------- | :------------ | :---------------- |
| Trailer-only owner on main, pre-commit phase | Changed files, branch main, no message yet | Print an explicit deferral and exit 0 so commit-msg can read the trailer. |
| Trailer-only owner on main, commit-msg phase | Same files and a valid Story-Key trailer | Resolve the trailer and enforce the artifact File Scope. |
| Unowned changed commit | No valid branch or trailer owner | pre-commit defers, then commit-msg exits 1; no commit is created. |
| Owner branch with in-scope files | Full owner key in branch | Validate immediately and pass. |
| Owner branch with out-of-scope files | Full owner key in branch | Validate immediately and fail; do not defer. |
| Invalid owner metadata | Malformed, partial, multiple, or conflicting key | Fail in every phase. |
| CI caller | Existing branch/message/diff inputs | Preserve current fail-closed behavior; no defer option. |

</frozen-after-approval>

## File Scope

Allowed files for this story:

- `.githooks/pre-commit` - opt into phase-specific unresolved-owner deferral.
- `tools/check-story-file-scope.py` - implement the narrowly scoped validator mode.
- `tests/tooling/story_scope/story_scope_validator_test.py` - add behavior and wiring regressions.
- `CONTRIBUTING.md` - document provisional pre-commit and definitive commit-msg behavior.
- `_bmad-output/implementation-artifacts/spec-fix-story-gate-hook-sequencing.md` - this spec and implementation evidence.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-30-story-gate-hook-sequencing.md` - record final approval.

Read/verify only:

- `.githooks/commit-msg`
- `.github/workflows/ci.yml`
- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md`
- `_bmad-output/implementation-artifacts/14-1-ci-story-scope-enforcement-hardening.md`
- `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
- `_bmad-output/project-context.md`
- `_bmad/custom/story-scope-guard.md`
- `_bmad/custom/epic-ac-verification.md`

Forbidden by default:

- commit-msg or CI weakening
- story, epic, sprint-status, PRD, architecture, or UX changes
- runtime/product source and tests
- submodule contents or pointer changes
- staging, committing, or rewriting unrelated user work

## Code Map

- `tools/check-story-file-scope.py` owns argument parsing, owner resolution, and File Scope validation.
- `.githooks/pre-commit` gathers the staged path set before a commit message exists.
- `.githooks/commit-msg` is the definitive local gate and already supplies the proposed message.
- `tests/tooling/story_scope/story_scope_validator_test.py` covers resolution, scope, override, and failure contracts.
- `CONTRIBUTING.md` is the contributor-facing hook and owner-source contract.

## Tasks & Acceptance

### Tasks

- [x] Add an explicit defer-unresolved-owner validator option without changing default behavior.
- [x] Use that option only from pre-commit.
- [x] Add regressions for deferral, fail-closed defaults, invalid metadata, scope violations, and caller wiring.
- [x] Update contributor guidance.
- [x] Run focused validation and reconcile this record.

### Acceptance Criteria

1. Given a non-empty staged set on main before a commit message exists, when pre-commit runs, then it
   explicitly defers unresolved ownership to commit-msg instead of failing.
2. Given an owner resolves during pre-commit, when scope validation runs, then all current in-scope,
   out-of-scope, forbidden-default, override, and conflict behavior remains enforced.
3. Given commit-msg or CI receives changed files without an owner, when the validator runs without
   the phase-specific option, then it fails closed exactly as before.
4. Given a valid Story or Story-Key trailer, when commit-msg runs, then the trailer resolves and the
   staged paths are checked against its artifact.
5. Given contributor guidance, when the two hook phases are described, then it says pre-commit is
   provisional only for absent ownership and commit-msg is definitive.

## Historical Context Classification

| Reference | Classification | Permitted influence |
| :-------- | :------------- | :------------------ |
| Story 12.3 ownership, File Scope, and shared-validator invariant | `current-narrow-pattern` | Preserve exact-owner resolution, early validation when possible, fail-closed conflicts, and one shared validator. Do not reuse the completed story's full shape. |
| Story 12.3 pre-commit missing-owner sequencing | `anti-template` | Defect evidence only. Do not reproduce a phase that recommends inputs it cannot observe. |
| spec-resolve-story-gate-commit-path | `historical-reference-only` | Preserve its evidence that trailers cannot rescue the earlier hook and that unowned main stays fail-closed. Do not edit or reuse its frozen scope. |
| Story 14.1 CI hardening | `current-narrow-pattern` | Preserve loud fail-closed CI behavior; never pass the local deferral option to CI. |

## Slice Proof

This spec owns one outcome: make an approved commit-trailer owner reachable without permitting an
unowned commit. Validator behavior, hook wiring, focused tests, and matching guidance are one
coherent implementation slice. It does not alter CI, commit-msg, scope matching, override authority,
story readiness, tenant-isolation evidence, commitlint, runtime behavior, or submodule state.

## Dev Notes

### Epic AC Verification

Verified 2026-07-30 against
`4a6f0d33689fde8335b5c7a8d429d885fa82040a` plus preserved user changes.

| Inherited claim | Class | Command / evidence | Observed | Verdict |
| :-------------- | :---- | :----------------- | :------- | :------ |
| "A commit may reference a story via branch name, conventional commit footer, or explicit annotation." | Behavioral | `sed -n '2634,2645p' _bmad-output/planning-artifacts/epics.md` | Story 12.3 names all three owner sources. | confirmed |
| "pre-commit validates branch or caller-provided context while commit-msg validates trailers." | Behavioral / location | `nl -ba .githooks/pre-commit; sed -n '1,43p' .githooks/commit-msg` | pre-commit supplies no message file; commit-msg supplies one. | confirmed |
| "A changed set on main with no owner still fails closed." | Behavioral / policy | `sed -n '56,102p' CONTRIBUTING.md` | The policy requires the final failure; this spec moves it to the phase that can read all approved sources. | confirmed |
| "The current pre-commit fails before trailer resolution." | Behavioral | `.githooks/pre-commit` with the preserved staged EventStore pointer on main | Exit 1 with "No story key resolved." | confirmed |
| "The focused validator baseline is green." | Quantitative | `python3 -m unittest discover -s tests/tooling/story_scope -p '*_test.py'` | 51 tests passed before implementation. | confirmed |

No inherited claim is corrected. This spec fixes implementation sequencing while preserving the
desired epic and story outcomes, so no planning-artifact correction is required.

## Verification

**Commands and results:**

- `python3 -m unittest discover -s tests/tooling/story_scope -p '*_test.py'` - 55 tests passed.
- `python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py'` - 4 tests passed.
- `.githooks/pre-commit` on `main` with the preserved staged EventStore pointer - exit 0 with the
  explicit "deferring story-scope validation to commit-msg" diagnostic.
- `python3 tools/check-story-file-scope.py --branch-name main --changed-file references/Hexalith.EventStore`
  - exit 1 with the unchanged definitive no-owner diagnostic.
- `python3 tools/check-story-file-scope.py --defer-unresolved-owner --branch-name fix/spec-invalid_owner --changed-file CONTRIBUTING.md`
  - exit 1; malformed owner metadata is not deferred.
- `python3 tools/check-story-file-scope.py --defer-unresolved-owner --branch-name fix/spec-resolve-story-gate-commit-path --changed-file references/Hexalith.EventStore`
  - exit 0 after full File Scope validation, proving resolved owners are validated rather than deferred.
- `python3 tools/check-story-file-scope.py --story-key spec-fix-story-gate-hook-sequencing` with all six
  changed paths - exit 0; every changed path is inside this spec's File Scope.
- `python3 tools/check-story-review-readiness.py --story-key spec-fix-story-gate-hook-sequencing`
  with all six changed paths - exit 0; C1 matched 6/6 paths, standalone-spec sprint-status handling
  was correct, and no phase-ledger or checkpoint table was applicable.
- `python3 tools/check-story-slice-scope.py --story-key spec-fix-story-gate-hook-sequencing --require-record`
  - exit 0 with "no governed story file changed"; the executable subset currently ignores standalone
  specs, so the required Historical Context Classification and Slice Proof were reviewed directly.
- `git diff --check` over the six owned paths - exit 0.

### Completion Notes

- Added a dedicated `MissingOwnerError` so only total source absence can be deferred; all other
  validation failures retain their existing fail-closed path.
- Kept the opt-in out of `commit-msg` and CI, guarded by a focused wiring regression.
- Preserved the existing staged submodule pointer and all unrelated worktree changes; nothing was
  staged or committed.

## Dev Agent Record

### File List

- `.githooks/pre-commit` - UPDATED: opt into unresolved-owner deferral before a message exists.
- `CONTRIBUTING.md` - UPDATED: document provisional pre-commit and definitive commit-msg behavior.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-30-story-gate-hook-sequencing.md` - UPDATED: final approval recorded.
- `_bmad-output/implementation-artifacts/spec-fix-story-gate-hook-sequencing.md` - NEW: approved implementation owner and evidence record.
- `tests/tooling/story_scope/story_scope_validator_test.py` - UPDATED: add deferral, fail-closed, scope, invalid-owner, and wiring regressions.
- `tools/check-story-file-scope.py` - UPDATED: add the pre-commit-only unresolved-owner deferral mode.

### Change Log

- 2026-07-30: Created from the Administrator-approved Sprint Change Proposal; status set to in-progress.
- 2026-07-30: Implemented the phase-aware deferral, added focused regressions and guidance, and
  recorded green validation without staging or committing user work.
- 2026-07-30: Reconciled the 6/6 File List, passed standalone-spec review readiness, and set status
  to done.
