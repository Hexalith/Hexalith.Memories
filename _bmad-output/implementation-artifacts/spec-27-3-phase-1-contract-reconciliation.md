---
title: 'Story 27.3 Phase 1 contract reconciliation'
type: 'refactor'
created: '2026-08-03'
status: 'done'
baseline_commit: '3f758f9ab019ca64a793e268470a7e4663cbc1fa'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md'
  - '{project-root}/_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.3 still binds AC1-AC5 even though it cannot discharge them, and its current Story 30.3 predecessor claim contradicts the checked-in same-SHA archive producer and CI sequence. This makes the completion and dependency contracts internally false.

**Approach:** Retain only C0 and AC6/AC7/AC8 (C2/C3/C4) as Story 27.3 authority, preserve transferred C1 material solely as explicit non-binding history, and correct only the Story 27.3 and Epic 30 archive-boundary text.

## Boundaries & Constraints

**Always:** Preserve AC6-AC8 numbering and historical citations; retain earlier phase/dependency decisions as superseded append-only history; keep Story 27.3 `in-progress`, Story 27.4 `backlog`, Production writes disabled, and A41 open; preserve unrelated dirty content, especially the Epic 31 `epics.md` hunk.

**Ask First:** Any need to edit a current artifact outside the Story 27.3 file or the Story 27.3/Epic 30 portions of `epics.md`; any status, sprint, A41, architecture, deferred-work, CI, producer, verifier, dependency, or runtime mutation.

**Never:** Create/register Stories 27.7-27.31; reuse 27.5/27.6; renumber criteria/history; rewrite earlier review/ledger records; edit Story 27.4; stage, commit, push, pull, branch, update dependencies/submodules, or absorb user changes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Binding/history | Current AC1-AC8 and C0-C4 | Binding AC6-AC8 and C0/C2/C3/C4; exact AC1-AC5 once in non-binding history | Live, altered, or duplicated C1 history fails |
| Archive dependency | Producer then verifier in one CI job/SHA | Story 27.3 consumes local archives; Story 30.3 owns future hardening/non-regression | Preserve old decisions as superseded history |
| Dirty shared file | Existing Epic 31 diff | Add narrow earlier hunks; leave it unchanged | Stop on overlap |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` -- authoritative Story 27.3 contract, checkpoints, historical records, and append-only phase ledger.
- `_bmad-output/planning-artifacts/epics.md` -- governed Story 27.3 copy and Epic 30/Story 30.3 publication boundary; contains an unrelated dirty Epic 31 hunk.
- `.github/workflows/ci.yml` -- read-only same-job producer/consumer sequence.
- `tools/publish-containers.ps1` -- read-only four-archive contract.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` -- move exact AC1-AC5 and transferred C1 execution/checkpoint material behind non-binding historical headings; retain AC6-AC8 and C0/C2/C3/C4; apply the same-SHA AC6 opening without changing its remaining body; append the dependency correction and `correct-course` ledger row.
- [x] `_bmad-output/planning-artifacts/epics.md` -- remove AC1-AC5 from the Story 27.3 binding copy, apply the matching AC6 opening/direct no-C1 wording, and clarify the Epic 30 scope boundary plus Story 30.3 downstream obligation as future hardening/non-regression rather than a predecessor-status gate.

**Acceptance Criteria:**
- Given both governed Story 27.3 acceptance sections, when their numbered criteria are extracted, then each returns exactly `6 7 8` in that order.
- Given the pre-edit AC1-AC5 text, when the story's non-binding annex is compared with HEAD, then all five criteria are preserved exactly once and confer no completion or registration authority.
- Given the checkpoint sections, when current authority is inspected, then only C0/C2/C3/C4 bind; C1 execution material and the administrative row remain explicit non-binding history.
- Given current source and CI, when dependency text is read, then Story 27.3 consumes four local archives at the reviewed SHA and backlog Story 30.3 retains only future hardening/stable-name non-regression.
- Given the initial worktree, when the final diff is reviewed, then status/A41/write invariants and the unrelated Epic 31 hunk are unchanged, no successor story exists, and no out-of-scope artifact was mutated by the correction.

## Spec Change Log

## Design Notes

Keep the old Story 30.1/30.3 dependency decisions and every existing phase row byte-stable. Add dated supersession at the current Task 2/C2 boundary and in the historical transfer record; do not retroactively make old observations read as if the correction was already known.

## Verification

**Commands:**
- `python3 tools/check-story-file-scope.py --story-key 27-3-production-adapter-and-deployment-profile --changed-file _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md --changed-file _bmad-output/planning-artifacts/epics.md` -- expected: both correction paths declared.
- `python3 tools/check-story-review-readiness.py --story-key 27-3-production-adapter-and-deployment-profile --changed-file _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md --changed-file _bmad-output/planning-artifacts/epics.md` -- expected: ledger/status/evidence structure remains valid.
- `python3 tools/check-story-slice-scope.py --story-key 27-3-production-adapter-and-deployment-profile --require-record` -- expected: historical classification, slice proof, and live checkpoint records pass.
- `test "$(perl -pe 's/\r$//' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md | sed -n '/^## Acceptance Criteria$/,/^## Historical C1 Transfer Record/p' | sed -n 's/^\([0-9][0-9]*\)\..*/\1/p' | tr '\n' ' ')" = "6 7 8 "` and the matching Story 27.3 extraction bounded by Story 27.4 in `epics.md` -- expected: both exit 0.
- `diff -u <(git show 3f758f9ab019ca64a793e268470a7e4663cbc1fa:_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md | sed -n '/^## Acceptance Criteria$/,/^6\./p' | sed -n '/^[1-5]\. /p') <(sed -n '/^## Historical C1 Transfer Record (Non-Binding)$/,/^## /p' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md | sed -n '/^[1-5]\. /p')` -- expected: empty diff.
- Focused checkpoint extraction -- expected: the current table returns `C0 C2 C3 C4`; historical C1 headings contain the execution contract, administrative row, and evidence record.
- Append-only extraction pinned to baseline `3f758f9ab019ca64a793e268470a7e4663cbc1fa` -- expected: removing the final 2026-08-03 row from the current Change Log reproduces the baseline Change Log byte-for-byte.
- Focused archive-order extraction from `.github/workflows/ci.yml` and `tools/publish-containers.ps1` -- expected: the producer precedes the verifier in `production-deployment-verification`, and both surfaces contain all four stable archive names.
- `git diff --check 3f758f9ab019ca64a793e268470a7e4663cbc1fa -- _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md _bmad-output/planning-artifacts/epics.md` -- expected: exit 0.
- `git diff 3f758f9ab019ca64a793e268470a7e4663cbc1fa -- _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md _bmad-output/planning-artifacts/epics.md` -- expected: approved Phase 1 hunks plus untouched concurrent/user-owned hunks, with no Phase 1 attribution assigned to those unrelated hunks.

## Suggested Review Order

**Binding authority**

- Start with the authoritative stable AC6-AC8 contract.
  [`27-3-production-adapter-and-deployment-profile.md:37`](27-3-production-adapter-and-deployment-profile.md#L37)

- Confirm exact AC1-AC5 provenance is explicitly non-binding.
  [`27-3-production-adapter-and-deployment-profile.md:48`](27-3-production-adapter-and-deployment-profile.md#L48)

- Verify current checkpoint authority contains only C0/C2/C3/C4.
  [`27-3-production-adapter-and-deployment-profile.md:802`](27-3-production-adapter-and-deployment-profile.md#L802)

- Check stale Story 27.5 attribution is preserved only as history.
  [`27-3-production-adapter-and-deployment-profile.md:765`](27-3-production-adapter-and-deployment-profile.md#L765)

**Archive dependency**

- Read the dated same-SHA correction beside the superseded dependency.
  [`27-3-production-adapter-and-deployment-profile.md:759`](27-3-production-adapter-and-deployment-profile.md#L759)

- Confirm Epic 30 owns future publication hardening, not predecessor status.
  [`epics.md:5142`](../planning-artifacts/epics.md#L5142)

- Confirm Story 30.3 preserves archive names without blocking Story 27.3.
  [`epics.md:5248`](../planning-artifacts/epics.md#L5248)

**Governance evidence**

- Inspect the single appended phase-local Change Log row.
  [`27-3-production-adapter-and-deployment-profile.md:1622`](27-3-production-adapter-and-deployment-profile.md#L1622)

- Re-run the baseline-pinned focused validation commands.
  [`spec-27-3-phase-1-contract-reconciliation.md:67`](spec-27-3-phase-1-contract-reconciliation.md#L67)
