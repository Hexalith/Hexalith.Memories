---
change_trigger: "Story 31.1 second-pass code review found two scope facts recorded only inside the story itself: the epics.md Owned-paths widening, and the retention of the unprovable manifest-reproduces-release outcome"
mode: batch
status: approved-and-applied
requested_by: Administrator
approved_by: Administrator
project: Hexalith.Memories
date: 2026-07-28
scope_classification: minor
supersedes: null
follows:
  - sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md
  - sprint-change-proposal-2026-07-28-story-31-1-deployed-profile-ac2-ratification.md
---

# Sprint Change Proposal: Story 31.1 Scope Ratifications

Date: 2026-07-28
Project: Hexalith.Memories
Scope: Minor. Ratifies two scope facts about Story 31.1 that were previously recorded only inside the
story's own review-resolutions table. It adds no acceptance criterion, changes no accepted limitation,
and makes no change to the running platform.

## 1. Issue Summary

Story 31.1's second-pass code review found that two decisions with epic-level effect had been recorded
only in the story file. `_bmad/custom/story-scope-guard.md` lines 5-7 state that current epics and
**approved sprint changes** define present scope, and that historical story artifacts do not — so a story
cannot widen its own epic-declared scope by writing the widening into itself.

### 1.1 `epics.md` Owned paths were widened by the story

`epics.md` Story 31.1 **Owned paths** was amended to add
`tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` and
`tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs`. The only proposal
governing this story — the 2026-07-28 deployed-profile AC2 ratification — authorizes exactly two `epics.md`
amendments (its §4.1 AC2 and §4.2 Implementation evidence). The Owned-paths edit is in neither, and the
justification written into the clause is self-referential: "required by this story's own 'exact evidence
command' clause".

### 1.2 An unprovable outcome was retained without a checkpoint row

"The reconciled `values.yaml` reproduces the deployed release" has no producer in this environment: `helm`
is absent (`which helm` returns nothing, re-confirmed 2026-07-28), and the only other source is the
`sh.helm.release.v1.hexalith-keys.v9` Secret payload that Task 1 forbids reading. The first review routed
this as a decision and the resolution "keep it and make it an explicit `done` gate" was recorded in the
story's own resolutions table. `epics.md` line 554 requires one checkpoint row per gate — "A single table
row covering multiple gates does not satisfy it, because a shared review state and completion date cannot
record partial completion" — and checkpoint C2 carries both gates in one compound completion cell.

## 2. Decisions

| # | Decision | Rationale |
| :- | :------- | :-------- |
| 1 | **Ratify the Owned-paths widening.** The two test paths are Story 31.1's owned scope. | The story's Implementation-evidence clause requires an exact evidence command per checkpoint row; those commands execute these two classes. The paths are genuinely owned; only the authority for saying so was missing. |
| 2 | **Carve the manifest-reproduces-release outcome out of Story 31.1.** Story 31.1 is reduced to AC1's literal documentation scope. Proving the reconciled manifest reproduces the deployed release becomes Platform Operations work, tracked as its own register entry. | The outcome is independently demonstrable, has no producer any Story 31.1 owner can run, and would otherwise hold the story open indefinitely on an environment limitation. The reconciliation stays documented as a named divergence with an owner, so nothing is silently dropped. |

## 3. Effects

- `epics.md` Story 31.1 **Owned paths** stands as written; this proposal is its authority.
- Story 31.1's `### Scope Boundary` records the carve-out.
- Checkpoint C2 is restated to the single claim its evidence supports: the drift was measured and recorded
  as an owned gap. The reproduction claim is no longer a Story 31.1 gate.
- The `Reconciled values.yaml has not been re-applied` row in `docs/operations/openbao.md` keeps its owner
  and reopen trigger, and is no longer described as a Story 31.1 `done` gate.
- No acceptance criterion is added, removed, or weakened. AC1 never required the manifest to be applied;
  it required the deployed configuration to be documented.

## 4. Explicitly out of scope

- Both accepted limitations, and checkpoints C4, C5 and C7, are untouched. The approved 2026-07-28
  deployed-profile ratification keeps C4 and C5 `pending` / `not complete`, and this proposal does not
  move them.
- No change to the running platform. No `helm` command was run.
- Story 31.2's scope is unchanged.

## 5. Success criteria

1. `epics.md` Story 31.1 Owned paths names the two test files and this proposal is cited as its authority.
2. Story 31.1's Scope Boundary records the manifest-reproduction carve-out with a named owner.
3. Checkpoint C2 carries one claim and one completion state.
4. `docs/operations/openbao.md` keeps the divergence row, without describing it as a Story 31.1 `done` gate.
5. Checkpoints C4, C5 and C7 are unchanged by this proposal.
