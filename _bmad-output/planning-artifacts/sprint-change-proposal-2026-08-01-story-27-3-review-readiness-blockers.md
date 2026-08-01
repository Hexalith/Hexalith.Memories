# Sprint Change Proposal: Story 27.3 Review-Readiness Blockers

**Date:** 2026-08-01  
**Triggering story:** `27-3-production-adapter-and-deployment-profile`  
**Mode:** Batch  
**Status:** Approved by the Administrator's initiating instruction and implemented  
**Change scope:** Minor direct adjustment with Developer and CI-owner handoff

## 1. Issue Summary

Story 27.3 cannot truthfully enter review while five planning and handoff defects remain:

1. C0 is recorded complete even though open predecessor gaps `DW 27.3-CR42` through
   `DW 27.3-CR46` limit five of the eight Story 27.2 portable-checkpoint methods.
2. The current held C1.13 definition cites an unanchored 70/80/90% table and omits the
   controlling byte values and the exactly-80% disposition.
3. AC6 omits the unavoidable interval after the production manifests are applied verbatim
   and before the live vault-component substitution, during which consumer pods may be
   admitted against a vault-typed store and crash-loop.
4. Story 27.3's 61-path File Scope is two paths behind its 63-path cumulative File List.
5. C2 names a fresh kind-backed run as its reopen trigger but does not give the next owner
   one exact ordered implementation, CI, retrieval, validation, and checkpoint-close sequence.

These are record-truth and evidence-routing defects. They do not authorize Production
enablement, C1 completion, Story 27.4 advancement, A41 mutation, a live deployment, a commit,
or a push.

## 2. Impact Analysis

### Epic and story impact

- **Epic 27:** remains viable and in progress. No story is created, removed, split, or
  renumbered. The Story 27.4 predecessor chain remains fail closed.
- **Story 27.2:** remains `done`; its implementation is not rewritten by this planning
  correction. CR42-CR46 retain Story 27.2 lifecycle-checkpoint ownership and state the exact
  proof limitations that must be remediated before C0 can close again.
- **Story 27.3:** remains `in-progress`. C0 changes from complete to blocked/not complete;
  C2 remains blocked/not complete; C3 and C4 remain complete. Its File Scope and File List
  become the same 64-path set after this proposal joins both.
- **Stories 27.5/27.6:** remain unregistered held definitions. The correction to C1.13 is a
  held-definition repair only and is not registration or completion evidence.
- **Story 27.4 and A41:** remain blocked/open exactly as before.

### Artifact conflicts

| Artifact | Impact | Disposition |
| :------- | :----- | :---------- |
| PRD | No product requirement or MVP conflict | No change |
| Architecture | Already assigns Story 27.3 C0/C2-C4 and makes C1 held | No change |
| UX specification | No UI, journey, accessibility, or component impact | No change |
| `epics.md` | AC2/C1.13 anchor and AC6 limitation are incomplete; C0 current state is absent | Amend |
| Story 27.3 file | C0, AC6, review items, File Scope/File List, and C2 handoff are stale/incomplete | Amend |
| Sprint status | Story 27.3 is already `in-progress`; Story 27.2 is already `done` | Inspect; no state change |
| 2026-08-01 held-definition proposal | C1.13 lost its explicit threshold contract | Add dated correction |
| Deferred-work register | CR42-CR46 are already structured, open, and correctly owned | No state change |

### Technical and CI impact

The correction adds no runtime code. It exposes that review readiness depends on two later
execution outcomes: Story 27.2-owned proof for CR42-CR46, followed by a fresh successful
`production-deployment-verification` CI job against the reviewed Story 27.3 source. The prior
run `30405437576` and artifact `8706483105` remain historical and cannot close C2 because the
current validator rejects their older substitution-disclosure schema.

## 3. Recommended Approach

Use a direct adjustment. Preserve the current story boundaries and status, repair the
governing records, and route the remaining executable work to the existing owners.

- **Effort:** low for planning correction; medium for predecessor test remediation plus CI.
- **Risk:** low for the record edits; medium for C0 because five behavioral claims need real
  concurrent/rate/time/structure-aware proof; medium for C2 because kind-backed evidence is
  available only in CI.
- **Timeline effect:** Story 27.3 remains in progress until C0 and C2 both re-close. No estimate
  is manufactured for external CI availability.
- **Rollback and MVP review:** not viable or necessary. No completed runtime implementation is
  reverted and the PRD MVP is unchanged.

## 4. Detailed Change Proposals

### 4.1 Reopen C0 and bind it to CR42-CR46

**Artifact:** `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`

**Old:** C0 is `reviewed 2026-07-19 | complete` on the Story 27.2 8/8 citation.

**New:** C0 is `blocked 2026-08-01 | not complete`. The row preserves the observed 8/8 result
but states that it proves only execution, not the five overclaimed behaviors. Owner,
consequence, and reopen trigger name `DW 27.3-CR42` through `DW 27.3-CR46` exactly.

**Rationale:** a green selector cannot discharge behaviors its tests do not demonstrate.
The gaps stay with the Story 27.2 lifecycle-checkpoint owner; Story 27.3 does not absorb them.

### 4.2 Restore C1.13's threshold authority

**Artifacts:** `epics.md` and
`sprint-change-proposal-2026-08-01.md` Annexes A/B.

**Old:** “running-target 1h/24h/7d admission against 70/80/90%.”

**New:** cite
`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md#capacity-evidence-and-admission-envelope`
and Story 27.3's `### Production-Shaped Execution Contract`; state the 400 GiB profile size
`429,496,729,600` bytes, 70% `300,647,710,720`, 80% `343,597,383,680`, and 90%
`386,547,056,640`; state that 100% occupancy is inadmissible and exactly 80% is critical,
not an admissible reclamation peak.

**Rationale:** C1.13 must remain independently executable and reviewable after its definition
moves. A bare percentage reference is not an evidence contract.

### 4.3 Amend both governed AC6 copies

**Artifacts:** `epics.md` and Story 27.3.

**Old:** AC6 discloses post-substitution health proof but not the apply-to-substitution interval.

**New:** disclose that verbatim apply creates consumer Deployments at their manifest replica
counts before substitution; pods may therefore start, attempt to load the vault-typed store,
and crash-loop. The interval cannot be closed by the current lane because the Components do not
exist to patch until after apply. State that the disposable-context guard limits blast radius
but does not turn this interval into Production OpenBao proof.

**Rationale:** reviewers must know exactly what the kind lane proves and what it cannot prove.

### 4.4 Reconcile File Scope and File List

**Artifact:** Story 27.3.

**Old:** File Scope has 61 paths; File List has 63 and additionally names
`AccessTelemetryLifecycleProcessor.cs` and `IAccessTelemetryStateStore.cs`.

**New:** add both runtime-contract paths and this approved proposal to both lists. The current
sets become identical at 64 unique paths. Preserve all named exclusions.

**Rationale:** this story explicitly promises the two lists stay identical even though the
repository-wide policy does not generally require set equality.

### 4.5 Exact ordered C2 unblock sequence

The implementation and CI owner must execute these steps in order. A later step never repairs
or waives a failed earlier step.

1. **Freeze the reviewed source.** Resolve C0 first, apply the approved planning corrections,
   and identify the exact commit SHA that contains the current verifier, validator, fixtures,
   AC6 copies, and Story 27.3 record. Do not use a dirty-worktree-only source as CI evidence.
2. **Run the local tooling contract.** Execute:

   ```bash
   PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p '*_test.py' -v
   ```

   Require the complete discovered inventory to pass with zero failures, errors, or skips.
3. **Run the static manifest checkpoint.** Execute the C4 Release build and exact
   `ProductionDeploymentArtifactsTests` selector from Story 27.3's Checkpoint Execution
   Contract. Require a fresh assembly, nonzero discovery, and zero errors/failures/not-run.
4. **Make the reviewed SHA available to CI.** Commit and push are separate user-authorized Git
   operations; do not perform them implicitly. Once authorized, publish the exact reviewed SHA
   to a branch on which `.github/workflows/ci.yml` runs.
5. **Run the named CI job.** The `production-deployment-verification` job must execute, in its
   workflow order: checkout; initialize build submodules and .NET; initialize kubectl; install
   kind; install Dapr CLI; publish the four local Release OCI archives; verify the disposable
   production rollout; validate evidence with `if: always()`; upload
   `production-deployment-evidence` with `if: always()`.
6. **Require a qualifying job result.** The run head SHA must equal the reviewed SHA; the job
   conclusion must be `success`; archive publication, rollout verification, evidence validation,
   and evidence upload must each be present and `success`, with none skipped or cancelled.
7. **Retrieve by API, not `gh run download`.** Record `<run-id>`, `<job-id>`, and `<artifact-id>`;
   locate the artifact with:

   ```bash
   gh api repos/:owner/:repo/actions/runs/<run-id>/artifacts
   gh api repos/:owner/:repo/actions/artifacts/<artifact-id>/zip > evidence.zip
   ```

   The second command must target the artifact named `production-deployment-evidence`.
8. **Validate the downloaded packet.** Extract to a fresh directory and run the reviewed
   `tools/validate-production-deployment-evidence.ps1` against its
   `artifacts/production-deployment-verification` directory. Require validator exit `0`,
   `verification-result.json` status `succeeded` and terminal stage
   `required-server-mcp-restored`, a schema-v2 `secret-store-substitution.json`, non-empty
   Components enumeration, per-component post-patch readback when substitution occurred,
   both authenticated HTTP 200 and 503 health evidence, current/previous logs, and all four
   release archives.
9. **Close C2 only in the owning review phase.** Replace the blocked row with `reviewed
   <date>`, record the run/job/artifact IDs and reviewed SHA, state `complete`, use a bare ISO
   completion date, and independently re-run the retrieval/validation checks. A successful
   upload after a failed verifier or validator does not close C2.
10. **Re-run story gates.** Run the story slice, File Scope, and review-readiness gates over the
    literal governed changed set. Story 27.3 may enter `review` only if C0 and C2 are complete,
    every other review/deferred obligation required before review is closed, and the final gate
    exits `0`.

## 5. Epic-Claim Verification

Verified 2026-08-01 against branch `main` at `74e527f7` plus this planning correction.

| Claim | Class | Re-runnable command / evidence | Observed | Verdict |
| :---- | :---- | :----------------------------- | :------- | :------ |
| “C0 is complete on Story 27.2's eight portable methods.” | Behavioral | `sed -n '2711,2760p' _bmad-output/implementation-artifacts/deferred-work.md` and the exact C0 selector | The selector passes 8/8, but CR42-CR46 identify five behaviors the methods do not prove. | corrected — C0 reopened |
| “C1.13 uses the approved 70/80/90% table.” | Quantitative/location | `sed -n '282,341p;964,981p' docs/dev/adr-27.1-001-access-telemetry-lifecycle.md; sed -n '768,793p' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` | Stable headings and exact values exist; the held C1.13 row omitted them. | corrected — values and anchors restored |
| “Verbatim apply precedes substitution.” | Behavioral/location | `rg -n "kubectl @\\('apply'|Invoke-SecretStoreSubstitution" tools/verify-production-deployment.ps1` | The live apply call precedes `Invoke-SecretStoreSubstitution`; consumer Deployments are admitted in that interval. | confirmed |
| “File Scope is two paths behind File List.” | Quantitative/existence | extract backticked bullets from `## File Scope` and `### File List` and compare with `comm -3` | Pre-correction: 61 scope, 63 list; only the processor and store-interface paths were list-only. | confirmed |
| “C2 requires a fresh kind-backed CI run.” | Behavioral/existence | `sed -n '421,487p' .github/workflows/ci.yml; sed -n '778,783p'` of Story 27.3 | The job installs kind and owns archive publication, rollout, validation, and upload; the current C2 row is blocked on a fresh run. | confirmed |
| PRD, architecture, and UX need no semantic amendment. | Applicability | PRD/architecture/UX heading and Story 27.3 ownership review | PRD has no adapter-checkpoint contract; architecture already assigns C0/C2-C4 and held C1; UX has no affected surface. | confirmed / N/A |

## 6. Change Navigation Checklist

| Item | Status | Finding |
| :--- | :----- | :------ |
| 1.1-1.3 Trigger/context/evidence | [x] | Story 27.3 review findings 418, 506, 517; File Scope/List delta; blocked C2 row. |
| 2.1-2.5 Epic impact/order | [x] | Epic 27 remains viable; no epic/story registration or order change. |
| 3.1 PRD | [x] | No conflict or MVP change. |
| 3.2 Architecture | [x] | Existing ownership paragraph is already consistent. |
| 3.3 UX | [N/A] | No UI or journey impact. |
| 3.4 Other artifacts | [x] | Story, epics, held proposal, CI handoff, scope/list, and sprint state inspected. |
| 4.1 Direct adjustment | [x] viable | Low planning effort; medium execution risk. |
| 4.2 Rollback | [x] not viable | No runtime rollback simplifies the evidence obligations. |
| 4.3 MVP review | [x] not viable | MVP and PRD goals are unchanged. |
| 4.4 Recommended path | [x] | Direct adjustment plus Developer/CI handoff. |
| 5.1-5.5 Proposal components | [x] | Issue, impact, edits, MVP statement, sequence, and owners are explicit. |
| 6.1-6.2 Review/accuracy | [x] | Claims re-derived from current source and records. |
| 6.3 Approval | [x] | The initiating instruction explicitly authorizes all listed planning/story corrections. |
| 6.4 Sprint status | [x] | Inspected; no status advancement or registration change is truthful. |
| 6.5 Handoff | [x] | Story 27.2 evidence owner, Story 27.3 owner, CI owner, and reviewer responsibilities are ordered. |

## 7. Implementation Handoff

| Recipient | Responsibility |
| :-------- | :------------- |
| Story 27.2 lifecycle-checkpoint owner | Implement and execute CR42-CR46 without moving the work into Story 27.3; update each deferred entry only on real evidence. |
| Story 27.3 developer | After all five predecessor gaps close, re-run the canonical 8-method selector and re-close C0; then complete C2 steps 1-8. |
| CI owner | Run the current workflow at the exact reviewed SHA and preserve run/job/artifact identities. |
| Code reviewer | Independently verify C0 and the downloaded C2 packet, close the rows in canonical shape, reconcile the ledger/File List, and run the final readiness gate. |

### Success criteria

1. Review findings 418, 506, and 517 are checked with dated applied notes.
2. C0 is blocked on CR42-CR46 and cannot be read as complete.
3. C1.13 carries stable anchors, all four byte figures, and the exactly-80% rule.
4. Both AC6 copies contain the same admission-window disclosure.
5. Story 27.3 File Scope and File List contain the same 64 unique paths.
6. C2's exact sequence is present in the story and this proposal.
7. Story 27.3 remains `in-progress`; Story 27.2 remains `done`; Story 27.4 remains
   `backlog`; C1 remains held; Production writes remain disabled; A41 remains open.

## 8. Approval

- [x] Administrator approves the five corrections and their synchronized artifact edits.
- [x] Administrator authorizes the planning/story edits only; no commit, push, live deployment,
  Production enablement, or status advancement is implied.

**Approval source:** the initiating command on 2026-08-01: “Apply all authorized planning and
story corrections, update sprint artifacts consistently, and report the next BMAD command.”

