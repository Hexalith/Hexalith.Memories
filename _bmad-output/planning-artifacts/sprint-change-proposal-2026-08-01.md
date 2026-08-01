# Sprint Change Proposal — 2026-08-01

**Story:** `27-3-production-adapter-and-deployment-profile`
**Workflow:** `correct-course` (Developer route)
**Decision owner:** Administrator
**Status:** Approved and implemented 2026-08-01
**Change classification:** Major
**Evidence base:** current worktree at reachable HEAD `1d9e9c89ef53d877b4ec09face575c36e5889854`

**Dated correction 2026-08-01.** The held C1.13 definition below had regressed to an
unanchored “70/80/90%” reference. Approved Sprint Change Proposal
`sprint-change-proposal-2026-08-01-story-27-3-review-readiness-blockers.md` restores the stable
ADR/story anchors, the 400 GiB byte total, all three threshold byte values, and the controlling
rule that exactly 80% is critical rather than an admissible reclamation peak. This correction
does not register or complete C1.13.

## 1. Executive Decision

Adopt arm **(a)** of the central `DW 27.3-CR31` decision:

1. Withdraw Stories 27.5 and 27.6 from every binding registration surface.
2. Preserve their C1 definitions in this proposal's annex as **held, not registered**.
3. Do **not** amend `_bmad/custom/story-scope-guard.md:41-43` to treat an
   activation blocker as an evidence command or artifact.
4. Do **not** invent producer commands in a planning correction. Real producers
   must be authored and reviewed before either successor is registered again.
5. Require actual `27-5-*` and `27-6-*` story files. An `epics.md` block or
   `sprint-status.yaml` row does not satisfy the story-file branch of
   `epics.md:555`.

This is a registration rollback, not a C1 scope deletion. All twenty-five gates,
their identifiers, their fail-closed consequences, and the intended 11/14
evidence-domain allocation remain held in Annex A. None may be cited as owned,
passed, ready for development, or guard-compliant while held.

The replacement boundary is not “evidence rows versus activation blockers.”
It is a candidate evidence-domain split:

- future Story 27.5: identity, declared-fault durability/recovery boundaries,
  and independent approvals (`C1.15-C1.25`);
- future Story 27.6: data path, recovery behavior, workload, isolation,
  encryption, capacity, and reclamation (`C1.1-C1.14`).

Those are independently reviewable evidence outcomes after their shared
environment activation. That conceptual boundary does not authorize
registration. Registration requires both story files and a real producer in
every checkpoint row.

## 2. Trigger, Scope, and Verified Premises

### 2.1 Trigger

The trigger is the nine interdependent `[Review][Decision]` items recorded in
the Story 27.3 `### Review Findings` section on 2026-07-31 and 2026-08-01:

- AC6 zero-total-Components carve-out at the story artifact's current line 604;
- activation-blocker admissibility / Story 27.6 registration at line 605;
- Historical Context Classification re-derivation at line 606;
- Story 27.4 predecessor fail-open at line 641;
- false 11/14 partition premise at line 642;
- unexecuted AC5 and Task 1 instructions at line 643;
- inherited Story 27.6 anti-template at line 644;
- orphaned C1.13 criterion at line 645; and
- the unsatisfied `epics.md:555` story-file guard at line 646.

The same proposal also covers the AC5 destination defect and AC1-AC5 governed-copy
divergence at current story lines 653-654, plus ratification/correction of
`DW 27.3-CR37`, `CR38`, and `CR39` at lines 659-660.

### 2.2 Epic AC Verification — executed before drafting

Every location below was re-derived from the current worktree before this
proposal was written, as required by
`_bmad-output/process-notes/story-creation-lessons.md:199-236`.

| Claim | Class | Re-runnable command | Observed result | Verdict |
| :---- | :---- | :------------------ | :-------------- | :------ |
| The checkpoint-umbrella exception requires a per-checkpoint evidence command/artifact; “activation blocker” is not an admitted substitute. | policy | `nl -ba _bmad/custom/story-scope-guard.md \| sed -n '35,56p'; grep -ic 'activation blocker' _bmad/custom/story-scope-guard.md` | The requirement is at current lines 41-43; grep prints `0` and exits `1`. | `confirmed` |
| A split that creates stories must satisfy the policy for every story and must not reproduce the cured shape. | policy | `nl -ba _bmad/custom/story-scope-guard.md \| sed -n '35,47p'` | Current lines 38-40 state both obligations. | `confirmed` |
| The epic guard requires the story file, not merely a binding registration surface. | planning | `nl -ba _bmad-output/planning-artifacts/epics.md \| sed -n '551,556p'` | Current line 555 explicitly says “the story file” and names child story files or a checklist table. | `confirmed` |
| No Story 27.5 or 27.6 implementation file exists. | repository | `find _bmad-output/implementation-artifacts -maxdepth 1 -type f \( -name '27-5-*.md' -o -name '27-6-*.md' \) -print` | No output. | `confirmed` |
| The executable slice checker covers Story 27.3 only for the current registration diff. | executable gate | `printf '%s\n' '_bmad-output/planning-artifacts/epics.md' '_bmad-output/implementation-artifacts/sprint-status.yaml' '_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md' > /tmp/story-27-3-slice-files.txt && python3 tools/check-story-slice-scope.py --changed-files-file /tmp/story-27-3-slice-files.txt` | `OK - 1 story file(s) checked`, naming only Story 27.3. | `confirmed`; green is vacuous for 27.5/27.6 |
| The registered rows are 11 packet observations plus 14 accepted activation blockers; C1.25 also carries a different accepted blocker. | planning | `awk '((NR>=5005&&NR<=5015)\|\|(NR>=5082&&NR<=5095)){if($0~/^\| C1\./)r++;if($0~/Packet observation:/)p++;if($0~/Accepted activation blocker/)a++;if($0~/Accepted blocker:/)b++}END{print r,p,a,b}' _bmad-output/planning-artifacts/epics.md` | `25 11 14 1`. No row contains a completed gate producer; the first 11 describe future packet fields and the other 14 say the command is not authorable. | `corrected` — the 11/14 evidence-status premise is false |
| The Story 27.5 slice proof contradicts its own boundary disclosure. | planning | `nl -ba _bmad-output/planning-artifacts/epics.md \| sed -n '4993,5015p'` | Line 4995 claims eleven evidence artifacts; line 4997 says only C1.20/C1.24 are authorable; rows 5005-5015 are observations, with a blocker on C1.25. | `corrected` |
| Story 27.6 inherited the anti-template AC body. | provenance | `diff -u <(git show a1f64d55:_bmad-output/planning-artifacts/epics.md \| sed -n '4968,4982p' \| tr -d '\r') <(sed -n '5035,5049p' _bmad-output/planning-artifacts/epics.md \| tr -d '\r')` | Exit `0`: the four substantive paragraphs are byte-identical after line-ending normalization. | `confirmed` |
| Story 27.6 falsely says the 25-gate bundle was registered on 2026-07-30. | planning | `nl -ba _bmad-output/planning-artifacts/epics.md \| sed -n '5059,5069p'; nl -ba _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-30.md \| sed -n '100,110p;436,446p'` | Epic line 5068 says registered; the proposal records that the rows were held, not registered, and moved to Annex A. | `corrected` |
| C1.13's running-target criterion is in Story 27.5 while its row is in Story 27.6. | planning | `rg -n 'configured-capacity admission' _bmad-output/planning-artifacts/epics.md` | Exactly current lines 4973 and 5094. | `confirmed` |
| Story 27.4 can currently advance without Story 27.6. | planning | `nl -ba _bmad-output/planning-artifacts/epics.md \| sed -n '4851,4858p;4928,4933p'` | Preamble names only 27.5; predecessor line 4931 assigns all 25 gates to 27.5; the profile-hash gate omits 27.6. | `confirmed` |
| Story 27.3 AC5 still binds its status to successor-only conditions and names an obsolete thirteen-gate destination. | acceptance contract | `nl -ba _bmad-output/planning-artifacts/epics.md \| sed -n '4913,4918p'; nl -ba _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md \| sed -n '35,57p'` | Both AC5 copies retain `Story 27.3 in-progress` and the thirteen-gate Story 27.5 clause. | `confirmed` |
| The AC1-AC5 governed copies diverged. | acceptance contract | `grep -c 'Transferred 2026-07-31' <(sed -n '4913,4917p' _bmad-output/planning-artifacts/epics.md); grep -c 'Transferred 2026-07-31' <(sed -n '35,53p' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md)` | Epic copy has five annotations; story copy has zero, and its AC2/AC5 destinations are stale. | `confirmed` |
| Unchecked transferred Task 1 permanently blocks the story's own review transition. | executable contract | `nl -ba _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md \| sed -n '75,88p'; nl -ba _bmad-output/implementation-artifacts/tests/27-3-create-story-scope-evidence.md \| sed -n '267,274p'; rg -n 'Task 1' _bmad-output/implementation-artifacts/deferred-work.md` | Task 1 plus eleven subtasks remain unchecked; verifier line 270 forbids any unchecked task at `review`/`done`; deferred search finds no Story 27.3 Task 1 tracking entry. | `confirmed` |
| AC6 lacks the zero-total carve-out while the shipped verifier fails closed on it. | acceptance/runtime parity | `nl -ba tools/verify-production-deployment.ps1 \| sed -n '150,166p'; nl -ba _bmad-output/planning-artifacts/epics.md \| sed -n '4918,4918p'` | Verifier lines 162-164 reject zero total Components as render/apply regression; AC6 speaks only of zero vault-typed Components. | `corrected` — AC6 must distinguish the two states |
| `DW 27.3-CR37` cites an unreachable commit and its trigger has re-fired. | Git/deferred record | `git merge-base --is-ancestor b391731c HEAD; git ls-tree HEAD references/Hexalith.Builds references/Hexalith.EventStore references/Hexalith.Tenants; git submodule status references/Hexalith.Builds` | Ancestor exit `1`; reachable HEAD records `b529b665`/`3ca3cbbf`/`2cd7edf5`; Builds reports `+9bdb368d...`. | `corrected` — status must be open |
| CR38/CR39 were not authorized by the 2026-07-31 proposal. | governance | `grep -c CR38 _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31.md; grep -c CR39 _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31.md; nl -ba _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31.md \| sed -n '565,575p'` | Both counts are `0`; current line 575 says nothing else departed. | `confirmed` |
| The required tooling lane is green at 116 tests. | test evidence | `python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p '*_test.py'` | `Ran 116 tests in 178.777s`, `OK`. The explicit pattern is mandatory. | `confirmed` |
| Current cumulative File Scope and File List reconcile before this proposal joins the story. | phase ledger | Extract the allowed-files and File List blocks, compare with `comm -3`, then pass the File List to `git diff --name-status 272c33bc5d30d71ac46f20e703b9d5456e75a093 --`. | Scope `60`, List `60`, no difference; `60` rows = `39 A / 21 M`. | `confirmed` |

No `corrected` verdict above is presented as already repaired. Sections 4-7 name
the exact planning-artifact correction that approval would authorize.

## 3. Correct-Course Checklist Result

| Item | Resolution in this proposal |
| :--- | :-------------------------- |
| D2 — AC6 zero Components | Keep the shipped fail-closed guard and add the same zero-total carve-out to both AC6 copies. |
| D3 — activation blocker substitute | Reject the substitute; select arm (a), held/not registered. |
| D4 — Historical Context Classification | Replace the inherited table with the row-by-row re-derived table in `4.7`. |
| D7 — Story 27.4 predecessor fail-open | Require 27.5 and 27.6 files, registration, `done` states, all 25 passed gates, and a common profile hash. |
| D8 — false 11/14 premise | Withdraw both registrations; preserve 11/14 only as a candidate evidence-domain allocation, never as current compliance. |
| D9 — unexecuted AC5/Task 1 decision | Retarget AC5; remove Task 1 from Story 27.3's active task set; explicitly withdraw immediate transfer until compliant owner files exist; track it as new `DW 27.3-CR40`. |
| D10 — Story 27.6 anti-template | Replace its held AC and slice-proof draft with the one-gate-per-criterion form in Annex B; correct the false 2026-07-30 registration claim. |
| D11 — orphaned C1.13 | Remove configured-capacity admission from the Story 27.5 draft; bind it to Story 27.6 held AC13 and C1.13. Reopen CR34 while unregistered. |
| D12 — `epics.md:555` | Rule that story files are required. Epic-only registration does not satisfy the guard. |
| AC patch 1 | Remove the obsolete “thirteen gates transferred to Story 27.5” wording from both AC5 copies. |
| AC patch 2 | Replace both AC1-AC5 copies from one canonical block and one identical per-criterion governance annotation. |
| CR37 | Correct unreachable evidence, set `open`, and preserve the concurrent-owner boundary. |
| CR38 | Ratify its creation and resolve it only on the fresh `60 -> 61` implementation reconciliation. |
| CR39 | Ratify its creation; leave `open` for the external story-gate owner. |

### 3.1 Impact assessment

- **PRD:** no requirement change. Production remains Dapr-only and fail closed.
- **Architecture:** one correction is required at current
  `architecture.md:227`. Story 27.3 owns C0/C2-C4 adapter qualification; exact
  running-target C1 qualification is held for future compliant successor
  story files. Story 27.4 still owns deployment-shaped lifecycle evidence,
  runbooks, and A41 close-out.
- **UX:** no impact.
- **Epic 27:** sequencing and registration change; no gate, profile, or
  fail-closed consequence is weakened.
- **Other epics:** no scope or status change. Story 31.2 remains the source of
  the future non-vault-typed end state referenced by AC6.
- **Runtime/deployment:** no source, manifest, cluster, pipeline, or evidence
  packet mutation is authorized.

## 4. Exact Authorized Planning Changes

Approval authorizes all edits in this section as one atomic correction. A
partial application is not authorized.

### 4.1 Withdraw the two non-compliant registrations

In `_bmad-output/planning-artifacts/epics.md`:

1. Remove the binding `### Story 27.5` block currently at lines 4950-5015.
2. Remove the binding `### Story 27.6` block currently at lines 5017-5095.
3. Replace them with a non-story heading:
   `#### Held C1 successor definitions — not registered`.
4. State that Annexes A and B of this proposal preserve the candidate
   definitions; neither candidate is a story, backlog item, owner, or completion
   surface until an approved correction registers actual story files.
5. Amend current `epics.md:555` so its audit trail says Stories 27.5/27.6 were
   withdrawn on 2026-08-01 for failing the guard and must not return to the
   “known instances” list without their files and producer-complete tables.

In `_bmad-output/implementation-artifacts/sprint-status.yaml`:

1. Remove Story 27.5 and Story 27.6 from the `epic-27.order` list currently at
   lines 163-164.
2. Remove their `development_status` rows and the obsolete 11/14 comments
   currently at lines 454-474.
3. Replace those comments with a short held/not-registered note pointing to
   this approved proposal.
4. Keep Epic 27 `in-progress`, Story 27.3 `in-progress`, and Story 27.4
   `backlog`. No status advances.

This is the fail-closed implementation of D3, D8, and D12. It does not select
policy arm (b) and does not pretend to execute producer-authoring arm (c).

### 4.2 Repair the Epic 27 release chain

Replace the current Epic 27 qualification preamble at `epics.md:4853-4857`
with a current statement:

- Story 27.3 owns C0 and independent C2/C3/C4 adapter qualification against
  the immutable `PG-ONPREM-1` definition.
- All twenty-five running-target C1 gates are held without a registered owner.
- Production lifecycle writes remain disabled and A41 remains open while any
  C1 gate is held, unregistered, unproven, stale, or failed.
- Story 27.3 reaching `done` never enables writes or advances Story 27.4.

Replace Story 27.4's predecessor gate at current `epics.md:4928-4932` with:

1. Story 27.3 is `done` with C0 and C2-C4 complete, all its own remediation
   and ledger obligations closed, and the immutable profile hash recorded.
2. Actual Story 27.5 and Story 27.6 files exist, satisfy
   `story-scope-guard.md` and `epics.md:555`, are registered by an approved
   correction, and are both `done`.
3. C1.1-C1.25 are each `passed` on their own required running-target evidence.
   Absence of either successor registration fails this condition.
4. Stories 27.3, 27.5, 27.6, and 27.4 use the same profile hash. Any mismatch
   keeps writes disabled, Story 27.4 `backlog`, and A41 open.

### 4.3 Correct Story 27.3's statement and architecture ownership

In both `epics.md` and the Story 27.3 artifact, replace the current story
outcome wording that presupposes registered Stories 27.5/27.6 with:

> Story 27.3 qualifies the reviewed Production adapter through C0 and the
> independent C2/C3/C4 manifest, unit-contract, and deployment-lane evidence,
> so that future compliantly authored running-target C1 stories can start from
> a reviewed adapter without Story 27.3 claiming any C1 gate.

Amend `architecture.md:227` consistently. The architecture must not say Story
27.3 owns exact running-target C1 qualification and must not present held
successors as registered.

### 4.4 Replace AC1-AC5 from one canonical contract

Use the same canonical AC1-AC5 text in `epics.md` and the Story 27.3 artifact.
Formatting may wrap, but normative words and annotations must be identical.

1. The exact `PG-ONPREM-1` runtime, component, PostgreSQL 18.4 backend, Dapr
   control plane, application images, component/config manifests,
   actor/Scheduler identities, configuration epoch, profile hash, node/storage
   capacity, and operating cost are captured from the running target as
   immutable C1 evidence.
2. Capacity and behavior are proven for the 1-hour, configured 24-hour, and
   7-day horizons with integer-normalized operands, checked arithmetic, and
   admission against the approved 70/80/90% table; C1.1-C1.14 remain
   running-target obligations and no unit lane discharges them.
3. Forced PostgreSQL pod/process replacement proves zero loss of acknowledged
   records while the node and retained local volume remain healthy; node,
   local-volume, control-plane, and site loss stay outside profile, and
   backup/restore plus nonzero RPO/RTO are published without an HA claim.
4. Hexalith Platform Operations separately approves capacity, cost, operation,
   bounded fault, backup/restore, upgrade, rollback, reclamation, and the
   non-HA boundary; an independent security reviewer separately approves
   identity, secrets, TLS, network, authorization, encryption, privacy, and
   evidence integrity.
5. Any held or unregistered C1 gate, missing digest, placeholder, profile
   drift, failed probe, missing approval, or unreserved capacity keeps
   Production lifecycle writes disabled, Story 27.4 `backlog`, and A41 open.
   Story 27.3's own status is determined only by C0, C2-C4, its remediation
   register, review obligations, and phase ledger. Story 27.3 reaching `done`
   neither enables writes nor advances Story 27.4.

Append this exact annotation to each of AC1-AC5 in both copies:

> *Transferred 2026-07-31 and held, not registered, by approved Sprint Change
> Proposal 2026-08-01. This criterion defines C1 evidence that Story 27.3
> cannot discharge. The twenty-five C1 gates have no registered story owner
> until compliant Story 27.5/27.6 files with real per-gate producers are
> approved. No held definition is completion evidence.*

This supersedes the stale thirteen-gate AC5 destination and the contradictory
`Story 27.3 remains in-progress` consequent while preserving the production
fail-closed rule.

### 4.5 Amend both AC6 copies identically

Keep the existing AC6 text and insert this exact carve-out immediately after
the zero-vault-typed passing statement:

> A successful enumeration that reports zero total Dapr Components is not that
> end state: it is a render/apply regression and fails the lane. The passing
> zero-vault-typed observation requires a successfully enumerated, non-empty
> Component set containing no vault-typed Component.

Do not change `tools/verify-production-deployment.ps1`. Its current lines
158-164 already implement this distinction.

### 4.6 Remove Task 1 from Story 27.3's active task set

Remove current Story 27.3 lines 75-88 from `## Tasks / Subtasks`. Replace them
with a non-checkbox historical transfer note:

> Task 1 was removed from Story 27.3's active task set by approved Sprint Change
> Proposal 2026-08-01. It was not completed. Its C1.1-C1.25 work is preserved
> in that proposal's held annex and may move only into future compliant
> successor story files.

The 2026-07-30 instruction to move Task 1 immediately into a registered Story
27.5 task set is explicitly withdrawn because no compliant owning story file
exists. The intent to transfer—not complete—the work remains. Add
`DW 27.3-CR40`:

`rg -n '27\.3-CR40' deferred-work.md` returned no match during this proposal
pass, so CR40 is the next free identifier now. Re-derive that fact immediately
before implementation; if another authorized session has claimed it, allocate
the next free identifier and update this proposal's implementation citations
atomically.

- **Status:** `open`.
- **Owner:** Product Owner plus Hexalith Platform Operations.
- **Target:** future Story 27.5 and Story 27.6 files.
- **Reopen trigger:** before either candidate is registered or set
  `ready-for-dev`.
- **Discharge:** each gate's work appears in exactly one owning story file,
  every task maps to its gate, and every checkpoint row has a real evidence
  command/artifact.
- **Consequence:** held C1 work cannot be selected, cited, or completed.

This removes the permanent unchecked-task contradiction at
`tests/27-3-create-story-scope-evidence.md:270` without representing the work
as done.

### 4.7 Re-derive Story 27.3 Historical Context Classification

Delete the provenance disclaimer and inherited table currently at story lines
904-933. Replace it with the table below, which was evaluated against the
current retained C0/C2/C3/C4 outcome after the registration decision in `1`.

| Reference | Re-derived classification | Permitted influence on retained Story 27.3 |
| :-------- | :------------------------ | :----------------------------------------- |
| Story 27.1 whole-story record | `historical-reference-only` | Decision provenance only; no task, checkpoint, or review shape. |
| Current ADR 27.1 `PG-ONPREM-1` contract | `current-narrow-pattern` | Immutable profile identity and fail-closed technical facts used by C0/C2-C4. |
| Superseded Story 27.1 Redis/Kubernetes iterations | `anti-template` | Never restore backend SDK, Kubernetes identity, or orchestrator dependencies to application code. |
| Story 27.2 whole-story record | `historical-reference-only` | Completed predecessor handoff only; no scope or File List reuse. |
| `AccessTelemetryLifecycleIntegrationCheckpointTests` seam | `current-narrow-pattern` | C0 predecessor verification mechanics only. |
| Story 7.5 | `anti-template` | FR67 emission provenance only; broad stdout/observability proof is not adapter qualification. |
| Story 8.4 | `historical-reference-only` | Test-helper and emission provenance only. |
| Story 8.5 | `anti-template` | Do not import its bundled operational outcome. |
| Story 20.2 | `historical-reference-only` | Denial-before-dependency provenance only; no C1 isolation discharge. |
| Story 20.5 | `anti-template` | Residual/A41 provenance only; do not reopen its umbrella shape. |
| Story 21.1 | `current-narrow-pattern` | Structure-aware document-guard mechanics only. |
| Story 24.3 | `historical-reference-only` | Verifier and tenant-marker provenance only; no running-target claim. |
| Story 24.4 | `current-narrow-pattern` | Finite validation/metric mechanics only where C2/C4 use them. |
| Stories 26.1 and 26.5 as whole stories | `anti-template` | Current manifest facts may be read; broad infrastructure/runbook/checkpoint shapes may not be copied. |
| `OperationalRunbookSetTests` and `MemoriesDashboardTests` | `historical-reference-only` | Prior guard provenance only; runbook/dashboard delivery belongs outside retained Story 27.3. |
| Story 26.6 | `current-narrow-pattern` | Focused rollback/restoration observation mechanics used by the deployment lane only. |
| Story 26.8 and Epic 20/21 retrospectives | `historical-reference-only` | Close-out chronology only. |
| Retention-visibility proposal | `historical-reference-only` | Premature-closure guard provenance only. |
| Superseded pre-split Story 27.3 | `anti-template` | Finding/anchor provenance only; never reuse its task density, AC density, checkpoint breadth, File List, or proof shape. |
| Story 26.1-origin render/verify harness | `current-narrow-pattern` | Focused C2/C4 render, apply, context-guard, and observation mechanics only. |
| Commit `86f51865`-origin validator/test fixture | `current-narrow-pattern` | Focused evidence validation and fixture mechanics only. |
| Approved 2026-07-28 proposal | `historical-reference-only` | AC7/AC8 and C3/C4 provenance; its C1 partition is superseded. |
| Approved 2026-07-30 proposal | `historical-reference-only` | AC6 and C0/C2-C4 narrowing provenance; its immediate Story 27.5 transfer is superseded. |
| Approved 2026-07-31 proposal | `anti-template` | Title/AC6 provenance only; do not reuse its 11/14 rationale, story registration, AC bundle, slice proof, or checkpoint-table shape. |
| Approved 2026-08-01 proposal | `historical-reference-only` | Authority for held C1 state, corrected ACs, task transfer record, and fail-closed sequencing; never C1 completion evidence. |

Replace the current Slice Proof with a concise retained-scope proof: Story 27.3
has one outcome—qualify or reject the reviewed adapter through C0 and independent
C2/C3/C4 evidence. Its C1 umbrella is an administrative transfer record only.
Held C1 definitions, producer authoring, running-target proof, runbooks, Story
27.4 execution, and A41 mutation are excluded. Passing Story 27.3 enables no
Production write.

After this replacement, mark `DW 27.3-CR26` `resolved` with evidence that all
24 current rows were re-evaluated and the new proposal row was added. Do not
use “deferred,” “pre-existing,” or the old copied-table disclaimer as evidence.

### 4.8 Correct the held Story 27.5/27.6 definitions

Annex A is the sole preserved checkpoint inventory. Annex B is the re-authored
Story 27.6 AC and slice-proof draft.

- Remove “configured-capacity admission” from the held Story 27.5 AC set.
- Place configured-capacity admission in held Story 27.6 AC13 beside C1.13.
- Correct the Story 27.6 historical row to say the Story 27.5 25-gate bundle
  was **proposed and held, not registered, on 2026-07-30**.
- Do not carry forward the old four-paragraph AC bundle or its slice proof.
- Do not describe either annex as guard-satisfying.

### 4.9 Deferred-work reconciliation and ratification

Apply these updates in `deferred-work.md`:

- **CR26:** `resolved` only after `4.7` lands.
- **CR31:** remain `open`. Record that invalid registration was withdrawn;
  discharge requires both successor files and producer-complete registered
  rows before either is `ready-for-dev` or any C1 gate is cited.
- **CR34:** change `Status` to `open`. Its running-target rule is preserved in
  held Story 27.6 AC13, but no registered criterion currently owns C1.13.
- **CR37:** change `Status` to `open`. Replace unreachable-commit discharge
  evidence with the reachable HEAD gitlinks observed in `2.2, record that
  `b391731c` is not an ancestor, and retain the live `+9bdb368d...` Builds
  blocker. Do not absorb or revert the dependency owner's work.
- **CR38:** ratify its creation as authorized by this proposal. Resolve it only
  after the implementation row proves File Scope = File List = `61` and the
  proposal path is in both.
- **CR39:** ratify its creation as authorized by this proposal and leave it
  `open` under `spec-resolve-story-gate-commit-path` ownership.
- **CR40:** add the Task 1 transfer record defined in `4.6`.

Do not rewrite the approved 2026-07-31 proposal to pretend CR38/CR39 were in its
original authorization. This proposal is their dated ratification.

### 4.10 Phase ledger, File Scope, File List, and exclusions

Add this proposal path to both Story 27.3 `## File Scope` and `### File List`:

`_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01.md`

Append a `2026-08-01 | correct-course` Change Log row after implementation:

- **Change:** all `4.1-4.9` governance edits applied atomically; no status
  advancement; no runtime/deployment mutation; C1 held and Production
  fail-closed.
- **Test count:** same-unit `116 -> 116`, phase delta `+0`, cumulative Story
  27.3 delta `+102` from create baseline `14`, external delta `0`, observed
  `116`. Exact command:
  `python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p '*_test.py'`.
- **File reconciliation:** `60 -> 61`. File Scope `61` = File List `61`,
  `scope_only=0`, `list_only=0`. Against baseline
  `272c33bc5d30d71ac46f20e703b9d5456e75a093`, expected cumulative shape is
  the current 60 rows (`39 A / 21 M`) plus this proposal (`1 A`), for
  `61 = 40 A / 21 M`. While the proposal is untracked, combine the 60-row
  baseline-relative diff with its explicit `git status --short
  --untracked-files=all` entry; after tracking, the ordinary baseline diff
  must return all 61.

Re-run counts immediately before and after implementation. If concurrent work
changes the lane or path set, record the external delta/owner rather than
copying the figures above.

Preserve the eight existing machine-readable exclusions owned by
`spec-gh-30655137033-fix-ci-cd-issues`:

- `_bmad-output/implementation-artifacts/spec-gh-30655137033-fix-ci-cd-issues.md`
- `src/Hexalith.Memories.AppHost/Program.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostOpenBaoConfigurationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Mcp/McpServerIntegrationTests.cs`
- `tools/integration-fast-required-surfaces.txt`

Also preserve the three existing `references/` gitlink exclusions. Nothing in
this proposal claims, absorbs, resets, stages, or commits another session's
work.

## 5. Status and Guardrails

Story 27.3 remains `in-progress`. This proposal does not authorize `review` or
`done`. Independently open after this correction include at least C2
`blocked 2026-07-31`, `DW 27.3-CR17`, `CR28`, `CR31`, `CR34`, `CR36`,
`CR37`, `CR39`, `CR40`, the 31 unchecked earlier review items, and chunk 3 of
the separate 2026-07-29 review.

The production safety state is invariant:

- Production lifecycle writes disabled.
- Story 27.4 `backlog`.
- A41 open.
- No C1 gate passed.
- No held successor ready for development.
- No profile substitution or gate weakening.

## 6. Runtime, Tenant-Isolation, and Rollback Applicability

**Remediation runtime checklist:** not applicable — this proposal changes
planning/governance records only and touches no workflow dispatch,
registration, cleanup, dedup, migration, rollback, or staging behavior.
Category 5 is satisfied through `4.10` rather than duplicated.

**Tenant isolation:** no tenant-facing behavior changes. Held C1.11 continues
to require focused negative evidence against the exact running profile; the
existing one-key envelope-hash unit test remains insufficient.

**Rollback:** before approval, delete or revise only this unapproved proposal.
After approval, do not restore the invalid registration by copying old tables.
Any return to registration requires a new approved correction with the two
story files, producer-complete checkpoint tables, fresh classifications,
slice proofs, executable gate results, and the Story 27.4 predecessor update
kept intact.

## 7. Handoff, Effort, Risk, and Success Criteria

### 7.1 Handoff

Because this removes two registered stories and redefines the Epic 27 release
chain, classification is **Major**. Approval and implementation ownership:

- Administrator / Product Owner: approve the scope and held registration state.
- Solution Architect: confirm the corrected Epic 27/architecture ownership.
- Developer correct-course route: apply the atomic artifact edits.
- Future `create-story` route: author Story 27.5/27.6 files only after all
  producers exist; not authorized by this proposal.

Estimated implementation effort after approval: one focused governance-edit
session plus verification. Producer authoring, environment activation, and
future story creation are separate work.

### 7.2 Risks

- **Primary risk:** a held definition is later mistaken for a registered owner.
  Mitigation: remove story headings/status rows and label every annex surface.
- **Safety risk:** Story 27.4 advances on a partial C1 result. Mitigation:
  explicit two-file/two-story/all-25 predecessor gate.
- **Drift risk:** AC copies diverge again. Mitigation: one canonical block and
  a normalized equality check during implementation.
- **Concurrent-work risk:** excluded CI/CD or submodule work is absorbed.
  Mitigation: preserve named exclusions and reconcile only the declared set.
- **Schedule risk:** C1 remains unscheduled until producers exist. This is an
  explicit fail-closed consequence, not hidden progress.

### 7.3 Success criteria

Implementation is complete only when all are true:

1. `epics.md` and `sprint-status.yaml` contain no registered Story 27.5/27.6.
2. No `### Story 27.5` or `### Story 27.6` binding block remains.
3. Annexes A/B are visibly held, not registered.
4. `story-scope-guard.md:41-43` is unchanged.
5. Story 27.4 requires both future successor files/stories and all 25 passed gates.
6. AC1-AC5 governed copies normalize to identical normative text.
7. AC5 no longer holds Story 27.3 `in-progress` on successor conditions and
   names no obsolete thirteen-gate destination.
8. Both AC6 copies carry the identical zero-total carve-out.
9. Story 27.3 has no unchecked transferred Task 1 in its active task section.
10. CR40 tracks the held Task 1 transfer.
11. Story 27.3's Historical Context Classification is the re-derived table in
    `4.7` and CR26 closes on that evidence.
12. C1.13 appears in held Story 27.6 AC13, not Story 27.5.
13. CR31/CR34/CR37/CR39/CR40 remain open as specified; CR38 closes only on
    the fresh 61/61 reconciliation.
14. The required test command reports `Ran 116 tests ... OK` unless a named
    external delta is recorded.
15. File Scope = File List = `61`, with all eight concurrent-session and three
    gitlink exclusions preserved.
16. Story 27.3 remains `in-progress`, Story 27.4 remains `backlog`, Production
    writes remain disabled, and A41 remains open.

## 8. Approval

Approval authorizes only the atomic planning/governance edits in `4`. It does
not authorize story creation, producer implementation, environment activation,
Production writes, A41 mutation, status advancement, dependency changes,
commit, or push.

- [x] **Approve** this complete proposal for atomic implementation.
- [ ] **Request revisions** before implementation.

**Approval recorded:** the Administrator replied `approve` on 2026-08-01
after the complete proposal was presented. Atomic implementation is authorized;
the exclusions and non-authorizations above remain binding.

**Implementation recorded:** all authorized sections 4.1-4.10 were applied on
2026-08-01 without story-status, runtime, deployment, policy, dependency,
commit, or push mutation. The explicit-pattern lane remained `116 -> 116`
(`Ran 116 tests in 228.371s`, `OK` after implementation), and the
runner-derived cumulative reconciliation is File Scope 61 = File List 61,
`scope_only=0`, `list_only=0`, combined `40 A / 21 M`. All eight
concurrent-session exclusions and three reference-gitlink exclusions remain.

## Annex A — Held C1 Inventory (Not Registered)

**Binding state:** held, not registered.
**Completion state:** none.
**Producer state:** missing for all twenty-five gates.
**Use:** preservation, future authoring input, and fail-closed traceability only.

| Gate | Candidate future story | Accountable owner | Required observation | Current producer state |
| :--- | :--------------------- | :---------------- | :------------------- | :--------------------- |
| C1.1 CRUD | 27.6 | Deployment adapter owner | Running-target create/read/update/delete round trip. | Missing — activation blocker is not a producer. |
| C1.2 Strong reads | 27.6 | Deployment adapter owner | Post-write strong-consistency read. | Missing — activation blocker is not a producer. |
| C1.3 ETags | 27.6 | Deployment adapter owner | Match, mismatch, stale ETag, and `FirstWrite` insertion semantics. | Missing — activation blocker is not a producer. |
| C1.4 Rollback-atomic transaction | 27.6 | Deployment adapter owner | Later-operation fault with no partial record/index commit. | Missing — activation blocker is not a producer. |
| C1.5 TTL | 27.6 | Deployment adapter owner | Effective running-target TTL expiry. | Missing — activation blocker is not a producer. |
| C1.6 Actor reactivation | 27.6 | Deployment adapter owner | State survival across actor deactivation/reactivation. | Missing — activation blocker is not a producer. |
| C1.7 Placement/Scheduler/reminder recovery | 27.6 | Deployment adapter owner | Reconnection and reminder firing after disruption. | Missing — activation blocker is not a producer. |
| C1.8 Request bounds | 27.6 | Deployment adapter owner | Running-target size/count bound enforcement. | Missing — activation blocker is not a producer. |
| C1.9 Two-writer 500 events/s | 27.6 | Adapter owner + Platform Operations | Thirty-minute ADR workload with latency/loss thresholds. | Missing — activation blocker is not a producer. |
| C1.10 150,000-record purge catch-up | 27.6 | Adapter owner + Platform Operations | Ten-minute backlog and bounded drain/oldest-due result. | Missing — activation blocker is not a producer. |
| C1.11 Isolation | 27.6 | Adapter owner + security reviewer | Physical cross-tenant denial with attached negative evidence. | Missing — activation blocker is not a producer. |
| C1.12 Encryption | 27.6 | Adapter owner + security reviewer | TLS `verify-full` and at-rest posture observation. | Missing — activation blocker is not a producer. |
| C1.13 Capacity | 27.6 | Adapter owner + Platform Operations | Running-target 1h/24h/7d admission against the table anchored at `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md#capacity-evidence-and-admission-envelope` and Story 27.3 `### Production-Shaped Execution Contract`: 400 GiB = 429,496,729,600 bytes; maximum steady-state admission 70% = 300,647,710,720 bytes; critical boundary 80% = 343,597,383,680 bytes; Unhealthy boundary 90% = 386,547,056,640 bytes. The threshold table controls over the raw profile size: 100% occupancy is inadmissible and exactly 80% is critical, not an admissible reclamation peak. | Missing — unit tests are preconditions, not discharge. |
| C1.14 Physical reclamation | 27.6 | Adapter owner + Platform Operations | Named collector/bound and cohort-attributed reclaimed space. | Missing — activation blocker is not a producer. |
| C1.15 Runtime/control-plane identity | 27.5 | Deployment adapter owner | Runtime, sidecar digest, Scheduler, actors, features, alpha opt-in. | Missing — packet schema is not a producer. |
| C1.16 Component/backend identity | 27.5 | Deployment adapter owner | Component/API/capabilities/backend/PostgreSQL identity. | Missing — packet schema is not a producer. |
| C1.17 Image/manifest/epoch identity | 27.5 | Deployment adapter owner | Image digests, manifest identity, epoch, profile hash/coverage. | Missing — packet schema is not a producer. |
| C1.18 Node/storage/cost | 27.5 | Platform Operations | Capacity, host headroom, and operating cost. | Missing — packet schema is not a producer. |
| C1.19 Declared-fault durability | 27.5 | Deployment adapter owner | Forced pod/process replacement with zero acknowledged loss. | Missing — packet schema is not a producer. |
| C1.20 Out-of-profile statement | 27.5 | Platform Operations | Published node/volume/control-plane/site exclusions. | Missing — an authorable statement is not yet an artifact. |
| C1.21 Backup/restore | 27.5 | Platform Operations | Named destination and successful restore. | Missing — packet schema is not a producer. |
| C1.22 RPO/RTO and no-HA claim | 27.5 | Platform Operations | Published nonzero RPO/RTO and explicit HA boundary. | Missing — packet schema is not a producer. |
| C1.23 Operations approval | 27.5 | Platform Operations | Separate approval of capacity/cost/operation/recovery/rollback/reclamation. | Missing — packet schema is not a producer. |
| C1.24 Non-HA acknowledgement | 27.5 | Platform Operations | Explicit node/disk/site non-HA acknowledgement. | Missing — an authorable statement is not yet an artifact. |
| C1.25 Security approval | 27.5 | Independent security reviewer | Separate security approval over the named surfaces. | Missing — approver assignment blocker is not activation evidence. |

Every row must gain an actual command or authored artifact before future
registration. Replacing “Missing” with “accepted blocker” is not permitted.

## Annex B — Held Story 27.6 AC and Slice-Proof Draft (Not Registered)

**Historical classification correction:** the Story 27.5 25-gate bundle was
proposed and held, **not registered**, on 2026-07-30. It is an
`anti-template`. Only stable gate identifiers, technical evidence definitions,
and fail-closed consequences may influence this draft; its AC density, slice
proof, table breadth, and registration claim may not be copied.

### Candidate acceptance criteria — one criterion per gate

1. **C1.1:** running-target CRUD succeeds through `state.postgresql/v2`.
2. **C1.2:** a post-write strong read returns the acknowledged value.
3. **C1.3:** ETag match/mismatch, stale rejection, and `FirstWrite` semantics
   hold on the running target.
4. **C1.4:** fault injection on a later transaction operation leaves no partial
   record or expiry-index commit.
5. **C1.5:** effective TTL expiration is observed on the running target.
6. **C1.6:** actor state survives deactivation and reactivation.
7. **C1.7:** Placement/Scheduler reconnect and the required reminder fires
   after the declared disruption.
8. **C1.8:** request size and count bounds fail closed.
9. **C1.9:** the ADR two-writer 500 events/s, 30-minute workload passes its
   zero-loss and latency thresholds.
10. **C1.10:** the 150,000-record backlog drains within five minutes after its
    ten-minute setup and oldest-due age stays below fifteen minutes.
11. **C1.11:** focused running-profile evidence proves physical cross-tenant
    denial; the envelope-hash unit test is not discharge.
12. **C1.12:** TLS `verify-full` and the approved at-rest encryption posture
    are observed on the exact target.
13. **C1.13:** configured capacity is admitted against the exact running target
    for 1h/24h/7d using the threshold table anchored at
    `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md#capacity-evidence-and-admission-envelope`
    and Story 27.3 `### Production-Shaped Execution Contract`: 400 GiB =
    429,496,729,600 bytes; 70% = 300,647,710,720 bytes; 80% =
    343,597,383,680 bytes; and 90% = 386,547,056,640 bytes. The table controls
    over raw profile size: 100% occupancy is inadmissible and exactly 80% is
    critical, not an admissible reclamation peak. Adapter-profile unit tests
    are mandatory preconditions and never discharge this criterion.
14. **C1.14:** a named collector and bound attribute physical reclaimed space
    to the tested purge cohort.

Candidate-wide fail-closed rule: any missing producer, unproven gate, profile
drift, stale evidence, or failed gate keeps Production writes disabled, Story
27.4 `backlog`, and A41 open.

### Candidate slice proof

The candidate has one bounded evidence outcome: accept or reject the exact
running profile's data-path and capacity behavior across C1.1-C1.14. It is
separate from the C1.15-C1.25 identity/durability/approval record because the
two evidence packets have different accountable review decisions and can be
reviewed independently after the shared environment activates.

The candidate is **not currently a compliant umbrella**. Fourteen descriptions
of future observations and fourteen activation blockers are not evidence
commands or artifacts. It may become an explicitly approved checkpoint story
only after:

1. a real producer is present in every C1.1-C1.14 row;
2. its actual story file carries this re-authored classification and slice proof;
3. `tools/check-story-slice-scope.py --require-record` evaluates that file;
4. `create-story` validates the row-to-criterion mapping; and
5. a later approved correction registers the file and updates Story 27.4's
   predecessor chain without weakening it.

Until all five conditions hold, Annex B is planning input only.
