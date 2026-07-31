# Sprint Change Proposal — 2026-07-31

**Story 27.3 code-review governance remediation: C1 gate registration, AC6 contract alignment, and scope restatement**

- **Date:** 2026-07-31
- **Author:** `correct-course` (Developer route)
- **Approver:** Administrator
- **Trigger:** Code review of Story 27.3, 2026-07-31
- **Scope classification:** Moderate — backlog reorganization within Epic 27
- **Status:** approved 2026-07-31 and implemented

---

## 1. Issue Summary

The 2026-07-31 adversarial code review of Story 27.3 reviewed a diff that was itself the
patch output of the 2026-07-30 review. Three of that prior correction's fixes did not
work, and five findings resolved by the Administrator could not be executed by a review
workflow: creating stories, registering stories, amending acceptance criteria, and
re-approving a proposal are `correct-course` routes. `story-scope-guard.md:38-40` binds
the splitting route for every story it creates, so a review workflow performing them
would be exactly the overreach these policies exist to prevent.

Those five decisions were recorded as `DW 27.3-CR31` through `DW 27.3-CR35`. This
proposal executes them.

### What was actually wrong

1. **Story 27.5 was registered with no gate record on any binding surface.** The
   2026-07-30 correction removed the `Historical Context Classification`, `Slice Proof`,
   and 25-row checkpoint table from `epics.md` and moved them to Annex A of
   `sprint-change-proposal-2026-07-30.md`, marked "held, **not registered**". Story 27.5
   remains live at `sprint-status.yaml:459` and no `27-5-*` story file exists, so neither
   branch of the `epics.md:555` guard is satisfied. `story-scope-guard.md:27-31` binds at
   registration at any status, including `backlog`.
2. **AC6 states a contract the shipped lane does not implement.** Both governed copies pin
   "the two" vault-typed Components and list "required substitution" as a failing step,
   while the lane enumerates dynamically and deliberately treats zero vault-typed
   Components as a pass.
3. **The 2026-07-30 proposal's recorded approval does not cover what the document
   contains.** Roughly 52 lines of normative content and a rewritten authorization row
   were added after approval, and four of its statements were falsified by its own diff.
4. **C1.13 was parked on unit-test evidence against a running-target-bound criterion.**
5. **Story 27.3's title and `## Story` statement still claim scope it no longer holds.**

### Evidence of discovery

The review ran six independent no-context subagent layers; all six returned non-empty and
none failed. 68 raw findings reduced to 43 distinct, yielding 7 decisions (all resolved),
36 patches (34 applied, 2 routed), and 1 dismissed. Verified rather than asserted: the
tooling test count moved 67 → 102, and 12 of 12 previously-surviving mutations are now
killed, including a disposable-cluster owner-check deletion reproduced independently
before the reporting layer was trusted. Both scripts were md5-verified restored after the
mutation run.

One finding was self-inflicted and is recorded here for the retrospective: running a
mutation-testing layer concurrently with reader layers left a transient false worktree,
and another layer built a High-severity finding on it. It was dismissed, the baseline
re-verified byte-identical, and the lesson saved so it does not recur.

---

## 2. Impact Analysis

### 2.1 Epic impact

Epic 27 completes as originally planned. No epic is added, removed, redefined, or
resequenced. All changes are story-level within Epic 27.

### 2.2 Story impact

| Story | Impact |
| :---- | :----- |
| 27.3 | Title and `## Story` statement restated to retained C0/C2-C4 scope with a dated supersede note. AC6 amended to the dynamic-enumeration contract. AC1-AC5 annotated as transferred, text retained unchanged. Stays `in-progress`. |
| 27.4 | No change. Stays `backlog`. |
| 27.5 | Re-registered with eleven evidence-bearing gate rows (C1.15-C1.25), guard records, and slice proof. Stays `backlog`. |
| 27.6 | **New.** Fourteen activation-blocked gate rows (C1.1-C1.14). Registered `backlog`. |
| 31.2 | No change. Still owns the real OpenBao runtime path and DW 27.3-CR29/CR30. |

### 2.3 Artifact conflicts

| Artifact | Impact |
| :------- | :----- |
| PRD | No change. NFR9 still requires the Dapr Secrets API backed by OpenBao in deployed environments. |
| Architecture | No change. D31 and the Story 31.2 boundary still own the real OpenBao runtime path. |
| UX | Not applicable; no interface or journey changes. |
| `epics.md` | Story 27.3 title/statement/AC6/AC1-AC5 annotations; Story 27.5 re-registration; new Story 27.6 block. |
| Story 27.3 artifact | Mirror the title, statement, and AC6 edits. |
| `sprint-status.yaml` | Register Story 27.6 `backlog` and add it to the Epic 27 story list. No status value advances. |
| `sprint-change-proposal-2026-07-30.md` | Four dated correction notes, an Annex A supersede header, and a dated second approval. |
| `deferred-work.md` | New entry `27.3-CR36`; CR31-CR35 flip to `resolved` on execution. |

### 2.4 Technical and delivery impact

No product code changes. No test changes. No CI pipeline changes. This correction is
entirely governance-record repair, plus two acceptance criteria brought into agreement
with code that already ships and is already verified.

---

## 3. Recommended Approach

**Selected: Direct Adjustment.**

| Option | Verdict | Reasoning |
| :----- | :------ | :-------- |
| 1. Direct Adjustment | **Viable — selected** | Modify Stories 27.3/27.5 and add Story 27.6 within the existing Epic 27 structure. Effort: Medium. Risk: Low. No timeline impact — every affected story is already `backlog` or `in-progress` and none becomes reachable-by-done. |
| 2. Potential Rollback | Not viable | Reverting the 2026-07-30 correction would reopen the C1 umbrella in Story 27.3 that the Administrator closed by approved decision, and would discard verified code fixes (67 → 102 tests, 12/12 mutations killed). The governance records are wrong; the code is right. |
| 3. PRD MVP Review | Not viable | No MVP impact. PRD NFR9 and architecture D31 are unaffected. Nothing is deferred out of MVP. |

---

## 4. Detailed Change Proposals

### E1 — Story 27.3 title and statement (DW 27.3-CR35)

**Artifacts:** `epics.md:4903-4907`; `27-3-production-adapter-and-deployment-profile.md:13`, `:27-29`

**OLD:**

```text
### Story 27.3: Production Adapter and Deployment Profile

As a Platform Operations and security review pair,
I want one immutable Production state-store profile qualified against every C1 gate,
So that deployment-shaped lifecycle verification starts only on an atomic, durable,
capacity-funded, and approved adapter.
```

**NEW:**

```text
### Story 27.3: Production Adapter Manifest, Unit, and Deployment-Lane Qualification

As a Platform Operations and security review pair,
I want the Production state-store adapter's manifests, adapter code contract, and
deployment-verification lane qualified on repository-executable evidence,
So that the running-target C1 capability qualification in Stories 27.5 and 27.6 starts
from a reviewed, statically-bound, and lane-verified adapter.
```

Followed, inside the `epics.md` Story 27.3 block and after the story artifact's existing
`Superseded 2026-07-30` line at `:21`, by:

> **Scope superseded 2026-07-31 by approved Sprint Change Proposal 2026-07-31.** All
> twenty-five C1 gates now belong to Stories 27.5 and 27.6. Story 27.3 retains C0 and the
> independent C2/C3/C4 lanes only. The prior statement — "one immutable Production
> state-store profile qualified against every C1 gate" — described pre-transfer scope and
> no longer binds. Story 27.3 reaching `done` advances no C1 gate, enables no Production
> lifecycle write, and does not advance Story 27.4.

**Rationale:** the absence of a supersede note inside the `epics.md` Story 27.3 block is
the specific defect CR35 names. After the transfer removed all C1 ownership, both copies
asserted both readings simultaneously.

### E2 — AC6 dynamic-enumeration contract (DW 27.3-CR32)

**Artifacts:** `epics.md:4916`; `27-3-…md:55`

Five substantive changes, applied to both governed copies:

| # | OLD | NEW |
| :- | :-- | :-- |
| 1 | "discovers **the two** applied Dapr secret-store Components" | "enumerates the applied Dapr secret-store Components from the cluster … every Component whose rendered type is `secretstores.hashicorp.vault`" |
| 2 | "any failed … **required substitution** … fails the lane" | removed; replaced by explicit modes: disposable-context assertion, Component enumeration, patch, post-patch readback |
| 3 | *(absent)* | "validates that evidence, **and uploads the evidence artifact**; any failed … **evidence production** … or evidence upload" |
| 4 | *(absent from `epics.md`)* | "**rather than being skipped**" |
| 5 | "no health stage exercises **either** vault-typed secret store" | "**any** vault-typed secret store" |

Plus two clauses the code enforces that neither copy stated: the disposable-context
refusal (`verify-production-deployment.ps1:118`, `Assert-DisposableClusterContext`) and
the disclosure-before-failure ordering (`:163-169`, pinned at
`production_deployment_evidence_test.py:869-871`).

**NEW `epics.md:4916`** (the story copy carries identical substance in Given/When/Then
form, retaining its anti-skip clause and evidence-upload step):

> 6. The kind-based production-deployment-verification lane renders and applies the
> production manifests verbatim to a disposable cluster from the four release OCI
> archives. Because that disposable cluster has no OpenBao runtime, the lane refuses to
> run against any non-disposable context, then enumerates the applied Dapr secret-store
> Components from the cluster and substitutes the live `spec.type` of every Component
> whose rendered type is `secretstores.hashicorp.vault` with `secretstores.kubernetes`
> before the health stages. The substituted set is discovered by type, never by a fixed
> set of Component names, so an added vault-typed Component cannot be silently left
> unpatched. Zero vault-typed Components is a passing observation, not a failure: it is
> the Story 31.2 end state in which the production manifests apply unmodified, and the
> lane must remain able to reach it. A failed Component enumeration is distinguished from
> that end state and fails the lane. The lane records the substitution and the
> per-Component observed post-patch types read back from the cluster in
> `secret-store-substitution.json`, writes that disclosure before raising any
> substitution failure, produces verification evidence, validates that evidence, and
> uploads the evidence artifact; any failed render, verbatim apply, disposable-context
> assertion, Component enumeration, patch, post-patch readback, health stage, evidence
> production, evidence validation, or evidence upload fails the lane rather than being
> skipped. The resulting health proof is for a cluster that differs from the rendered
> Production manifests: every health-stage observation occurs after the live
> substitution, so no health stage exercises any vault-typed secret store and the lane
> does not prove the Production OpenBao/vault secret-resolution path. *Added 2026-07-27
> by approved Sprint Change Proposal 2026-07-27 (DW 27.3-CR15); amended 2026-07-30 by
> approved Sprint Change Proposal 2026-07-30 to disclose the deliberate runtime
> deviation; amended 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW
> 27.3-CR32) to state the dynamic-enumeration contract the shipped lane implements,
> restore evidence production and upload to the fail list, and harmonize both governed
> copies. This criterion is independent of AC1-AC5: it neither requires nor unblocks C1,
> and passing it advances no C1 gate and enables no Production lifecycle write. AC5
> remains unchanged.*

**Guard note.** Change 2 is not a relaxation. `verify-production-deployment.ps1:147-151`
records that no throw fires when nothing is vault-typed *by design*, because Story 31.2
delivers the real OpenBao path, after which a run applies the production manifests
unmodified — the desired end state, and the state `DW 27.3-CR29`/`CR30` must be able to
discharge. Making the substitution mandatory would render AC6's own lane permanently
incapable of proving the path it exists to qualify. `epic-ac-verification.md:105-107` is
satisfied: this is the ratified end state, not an unmet implementation gap. The separate
zero-Component enumeration throw at `:140-142` is retained and is now stated in AC6.

### E3 — Story 27.5 registered with eleven evidence-bearing rows (DW 27.3-CR31, CR34)

**Artifact:** `epics.md` Story 27.5 block, replacing the "held, not registered"
placeholder inserted on 2026-07-30.

**Scope statement — OLD → NEW:**

```text
OLD:  I want all twenty-five C1 capability gates for the exact running PG-ONPREM-1
      target proven on their required evidence,

NEW:  I want the eleven C1 identity, declared-fault durability, and approval gates for
      the exact running PG-ONPREM-1 target proven on their authored evidence,
```

#### Historical Context Classification — Story 27.5

| Reference | Classification | Permitted influence on Story 27.5 |
| :-------- | :------------- | :-------------------------------- |
| Story 27.3 whole-story shape, including its original 25-gate C1 bundle | `anti-template` | Transfer only the exact ratified C1 gate identifiers, owners, evidence definitions, and fail-closed rules. Do not copy Story 27.3's tasks, non-C1 checkpoints, File List, ledger breadth, review history, or status shape. |
| Approved 2026-07-28 C1 split proposal | `historical-reference-only` | Authority and provenance for the Story 27.5 activation gate; not a template for another partial split. |
| Approved 2026-07-30 proposal | `historical-reference-only` | Authority and provenance for all-C1 ownership and preserved fail-closed consequences. |
| Current ADR 27.1 `PG-ONPREM-1` profile and current C1 evidence definitions | `current-narrow-pattern` | Re-verified exact profile identity, thresholds, faults, and the one-row-per-gate evidence contract only; whole-story shapes remain excluded. |
| Approved 2026-07-31 proposal (this document) | `historical-reference-only` | Authority for the 11/14 registration split and the C1.13 running-target rebinding; never evidence that a gate passed. |

#### Slice Proof — Story 27.5

Story 27.5 has one outcome: accept or reject the exact running `PG-ONPREM-1` profile on
its authored-evidence gates — identity and epoch (C1.15-C1.18), declared-fault durability
and recovery boundaries (C1.19-C1.22), and the approval record (C1.23-C1.25). It remains
one explicitly approved checkpoint story because all eleven appear in one table, each with
an accountable owner, its own evidence artifact, review state, completion state,
consequence, and reopen trigger. No shared umbrella state discharges a gate. Story 27.5
owns no C0, C2, C3, C4, runbook, A41 mutation, product-code, or manifest-authoring
outcome, and no C1.1-C1.14 gate.

**Boundary disclosure (2026-07-31).** The split from Story 27.6 is by **record shape**,
not delivery independence: these eleven carry an authored evidence artifact, while
C1.1-C1.14 carry only an accepted activation blocker. Both stories unblock at the same
moment — when the `PG-ONPREM-1` lifecycle Deployments scale above zero. Only C1.20 and
C1.24, both published statements, are authorable before then. This boundary is recorded
as guard-satisfying registration under `story-scope-guard.md:41-43`, not claimed as two
independently deliverable outcomes.

#### C1 Checkpoint Table — Story 27.5

Eleven rows, transferred verbatim from `sprint-change-proposal-2026-07-30.md` Annex A
rows C1.15-C1.25 (`:454-464`) with identifiers, accountable owners, evidence definitions,
consequences, and reopen triggers unchanged. Every row registers as
`pending | not complete | —`. Transfer completes no gate.

### E4 — New Story 27.6 and the AC1-AC5 consequence (DW 27.3-CR31, CR34)

#### (a) New `epics.md` block, placed after Story 27.5

```text
### Story 27.6: Running PG-ONPREM-1 Data-Path and Capability Gate Proof

As a Platform Operations and security review pair,
I want the fourteen C1 data-path, throughput, isolation, and capacity gates proven
against the exact running PG-ONPREM-1 target,
So that the Production adapter's runtime behavior is qualified rather than asserted.
```

**Origin:** split out of Story 27.5 on 2026-07-31 by this approved proposal, executing
Administrator decisions `DW 27.3-CR31` and `DW 27.3-CR34`. No gate is dropped, weakened,
renumbered, or made discharge-able by a unit lane.

**Activation gate:** unchanged and inherited — `ready-for-dev` is forbidden until the
`PG-ONPREM-1` lifecycle Deployments scale above zero. Story 27.6 stays `backlog` until
then. Production writes remain disabled, Story 27.4 remains `backlog`, and A41 remains
open while any gate is unproven.

The `Historical Context Classification` mirrors Story 27.5's with one added
`anti-template` row for Story 27.5's own 25-gate bundle: transfer only the C1.1-C1.14
identifiers and evidence definitions; do not copy its slice proof or table breadth. The
`Slice Proof` carries the same boundary disclosure recorded under E3.

**Checkpoint table:** fourteen rows, C1.1-C1.14, transferred verbatim from Annex A with
identifiers, owners, accepted activation blockers, consequences, and reopen triggers
unchanged, all `pending | not complete | —`. One row is rewritten:

| Gate | AC | Accountable owner | Required evidence observation or accepted blocker | Consequence and reopen trigger | Review state | Completion state | Completion date |
| :--- | :- | :---------------- | :------------------------------------------------ | :----------------------------- | :----------- | :--------------- | :-------------- |
| C1.13 Capacity | AC2 | Deployment adapter owner + Hexalith Platform Operations | **Accepted activation blocker:** required configured-capacity admission against the exact running target over the 1h / 24h / 7d horizons against the approved 70/80/90% threshold table (its byte values, and the rule that exactly 80% is critical rather than an admissible peak, live in `27-3-production-adapter-and-deployment-profile.md` under `### Production-Shaped Execution Contract`); the command is not authorable until activation. **Precondition, not discharge:** `AdapterProfileTests.test_capacity_inputs_fail_closed` and `AdapterProfileTests.test_capacity_result_is_admitted_against_profile_thresholds` must pass, but passing them advances no gate — `epics.md:4984-4986` and AC2 bind admission to "the exact running target", and `story-scope-guard.md:68-69` forbids closing on internal-only proof. This withdraws the 2026-07-30 ruling that C1.13 "needs no activation gate". | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens, this row's running-target command is authored, and reviewer confirmation exists. | pending | not complete | — |

#### (b) `sprint-status.yaml`

Add `27-6-running-pg-onprem-1-data-path-and-capability-proof: backlog` after `:459`, and
add the same key to the Epic 27 story list at `:160-164`, with a dated comment recording
the 11/14 split and its authority. No status value advances. This file is `eol=lf` per
`.gitattributes:10`.

#### (c) Companion edit — AC1-AC5 in the Story 27.3 block

Not named in CR31-CR35; discovered while drafting E4. Two defects follow mechanically
from moving C1.13:

1. `epics.md:4912` (AC2) still reads that the thirteen narrowed gates "transfer to
   **Story 27.5**". They now belong to Story 27.6.
2. AC2's own subject *is* C1.13 — capacity over the 1h/24h/7d horizons. Once C1.13
   leaves, AC1-AC5 have no retained subject in Story 27.3, yet all five still sit in the
   block reading as if they bind.

Per `epic-ac-verification.md:105-107` no criterion is deleted or weakened. Each retains
its full text and gains:

> *Transferred 2026-07-31 by approved Sprint Change Proposal 2026-07-31. Every gate this
> criterion governs is owned by Story 27.5 (C1.15-C1.25) or Story 27.6 (C1.1-C1.14). The
> criterion text is retained unchanged as the governing definition those stories inherit;
> it states no obligation Story 27.3 can discharge, and Story 27.3 reaching `done`
> satisfies none of it. AC5's fail-closed rule continues to bind both successor stories.*

AC2's stale "transfer to Story 27.5" clause is corrected to "to Stories 27.5 and 27.6",
with C1.13 added to its enumerated gate list.

### E5 — 2026-07-30 proposal corrections and dated re-approval (DW 27.3-CR33)

**Artifact:** `sprint-change-proposal-2026-07-30.md`

Per `epic-ac-verification.md:98-100`, dated correction notes are appended at each claim
rather than rewriting it, so the record of what was believed on 2026-07-30 stays legible.

| Line | Falsified claim | Correction note appended |
| :--- | :-------------- | :----------------------- |
| `:102-104` | "The story receives the missing `Historical Context Classification`, `Slice Proof`, and a one-row-per-gate checkpoint table" | *Corrected 2026-07-31 (DW 27.3-CR33). As executed, these records reached no registration surface — they were moved to Annex A of this document, marked "held, not registered". The 2026-07-31 proposal registers them into `epics.md`: Story 27.5 (C1.15-C1.25) and new Story 27.6 (C1.1-C1.14).* |
| `:115` | "`epics.md` … add Story 27.5 guard records and checkpoint table" | *Corrected 2026-07-31 (DW 27.3-CR33). The diff removed these from `epics.md` rather than adding them. Registered by the 2026-07-31 proposal.* |
| `:305` | "The implementation edit expands this summary into twenty-five rows in `epics.md`" | *Corrected 2026-07-31 (DW 27.3-CR33). No rows were written to `epics.md`; the twenty-five rows went to Annex A. They are registered as 11 + 14 across Stories 27.5 and 27.6 by the 2026-07-31 proposal.* |
| `:365-368` | Success criteria 4 and 5 | *Not met as executed on 2026-07-30; corrected 2026-07-31 (DW 27.3-CR33). Neither was true — Story 27.5 had no story file and no gate record on any binding surface. Both are satisfied by the 2026-07-31 proposal, with the gate set split 11/14 across Stories 27.5 and 27.6 per DW 27.3-CR31 and CR34.* |

**Annex A header note** at `:413`: retained as the provenance record of the 2026-07-30
transfer and explicitly no longer a live contract — the rows are registered in `epics.md`,
which is authoritative where the two differ.

**Second approval record**, appended to §7:

> **Second approval recorded 2026-07-31.** Approximately 52 lines of normative content
> (Annex A, `:413-464`) and the rewritten authorization row at `:118` were added after the
> 2026-07-30 approval. That approval, at `:351`, authorizes "only the artifact edits
> enumerated here" and, at `:410-411`, records approval of "the complete proposal" as
> presented on 2026-07-30 — so it does not cover the post-approval content. The
> Administrator re-approved this document as amended on 2026-07-31, together with the four
> dated correction notes above. Status: **approved as amended**.

### E6 — Deferred work and bookkeeping

#### (a) New entry `DW 27.3-CR36`

Records the startup-budget false-red risk. The 2026-07-31 fix caps the effective startup
total at `$TimeoutSeconds` and credits no runner-side port-forward capture, curing a prior
form whose effective ceiling reached `2 x $TimeoutSeconds` — a container Kubernetes
recorded Ready 119s after start passed a "60-second startup limit". The trade-off is
disclosed in-code at `verify-production-deployment.ps1:795-797`: a stage whose
port-forward capture is genuinely large can now fail a container that became ready inside
its own budget. No such false red has been observed; the entry exists so the first one is
read as a known trade-off rather than a new defect.

- **Owner:** the production-deployment-verification lane owner.
- **Re-open trigger:** the first lane failure whose terminal message is the
  `$TimeoutSeconds`-second startup-limit throw and whose reported uncredited port-forward
  capture is a material fraction of the budget.
- **Discharged when:** either the budget is re-sized against observed cold-start data, or
  a bounded capture credit is restored with its ceiling stated in AC6.
- **Consequence:** a cold-start CI environment can red the lane on capture overhead the
  contract does not charge to the container.

#### (b) CR31-CR35 disposition

On execution each flips `open` → `resolved` with its discharging reference: CR31 → E3+E4;
CR32 → E2; CR33 → E5; CR34 → E4(a) C1.13 row; CR35 → E1. `DW 27.3-CR28`, `CR29`, `CR30`
and the thirteen open chunk-1 patches remain untouched and open.

#### (c) Submodule drift — recorded as scope exclusion, not repaired (registered as `DW 27.3-CR37`)

Three `references/` gitlinks are dirty in the working tree and are excluded from this
correction's File Scope:

```text
references/Hexalith.Builds      61e43b18b59176e33ef8d389028900292905fbad -> e85a319ecd80f82c3090c49979d4580f07697742
references/Hexalith.EventStore  a40ab8a63271b1d186b75a0d8181f66893fe91d4 -> e4618d9114c8824fd50fdfc8d135438aa261377c
references/Hexalith.Tenants     33abe276bffbd779ad28eec21668cd89ed703e57 -> 625061bd4858d34263c2deef6a705742ac68ed37
```

They must not ride along in a Story 27.3 commit. They are **not** reverted here: a
concurrent session's deliberate bump is not this route's to discard, and
`project-context.md:112` requires explicit intent plus separate submodule commits. The
SHAs are recorded so their owner can confirm intent. If they prove to be inherited drift
rather than a deliberate bump, `git submodule update -- <paths>` restores them.

**Superseded 2026-07-31 by the Administrator's instruction to accept the bumps.** By the
time they were accepted, all three submodules had advanced past the SHAs recorded above -
a concurrent session was still moving them. The committed pointers are `Hexalith.Builds`
-> `b529b665`, `Hexalith.EventStore` -> `3ca3cbbf`, `Hexalith.Tenants` -> `2cd7edf5`, each
clean and at its own `main` tip at commit time. Landed as commit `b391731c` under
`Story-Key: spec-resolve-story-gate-commit-path`, which already owns these three pointer
paths and whose non-goals exclude attributing root submodule pointer changes to a story
owner - so the separation `DW 27.3-CR37` required is preserved and no gate was bypassed.
This is the same lesson L14 records, applied to gitlinks rather than line anchors: a
recorded SHA is advisory until re-derived at the moment of use.

### E7 — Anchor-drift lesson (added post-approval 2026-07-31)

**Artifact:** `_bmad-output/process-notes/story-creation-lessons.md`

**Not part of the approved E1-E6 set.** Authorized separately by the Administrator on
2026-07-31, after this proposal was approved and implemented, and recorded here as a dated
amendment rather than folded silently into §4 — the exact failure this proposal's E5
corrects in `sprint-change-proposal-2026-07-30.md`, where roughly 52 lines of normative
content were added after approval and left the recorded approval not covering the
document's contents.

**Change:** appends `## L14 - Cited Line Anchors Are Advisory Until Re-Derived`.

**Rationale:** the §5 verification found five of twenty-one inherited claims carried a
wrong line anchor while the underlying defect was real in every one. That is structural,
not incidental — a `[Review][Decision]` item records a line number against the worktree as
it stood during the review, and the same review's patches then move it. L14 extends L12's
reasoning from claims to the anchors those claims cite, adds the file-resolution failure
mode (`:494`/`:497` resolved to the story artifact, not the proposal named in the same
sentence), and records that a decision's literal reading can contradict a deliberate
in-code design choice — `DW 27.3-CR32` being the worked example, where the correct
execution was the opposite of the literal reading.

**Why this route.** `story-creation-lessons.md` is an open ledger whose header invites
entries for story creation, party-mode review, advanced elicitation, and code-review
automation; L09-L13 each landed alongside a correction. Nothing forbade including this in
§4 — it was omitted because the pattern was recognized only while writing the completion
summary, although the evidence for it existed before §4 was drafted.

---

## 5. Epic AC Verification

Verified 2026-07-31 against the working tree at branch `main` (commit `a1f64d55` plus the
uncommitted Story 27.3 review patch output).

Twenty-one verifiable claims inherited from `DW 27.3-CR31` through `CR35` and asserted by
this proposal. Sixteen `confirmed`, five `corrected`. Every `corrected` verdict is anchor
drift with the underlying defect intact; each correction is applied to this document's own
text, and the source `deferred-work.md` rationales carry a dated correction note.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| "Story 27.5 is registered at `sprint-status.yaml:459`" | Location | `sed -n '459p' _bmad-output/implementation-artifacts/sprint-status.yaml` | `27-5-running-pg-onprem-1-capability-qualification: backlog` | `confirmed` |
| "no `27-5-*` story file exists" | Existence | `ls _bmad-output/implementation-artifacts/27-5*` | `No such file or directory` | `confirmed` |
| "neither branch of `epics.md:555` is satisfied" | Behavioral | `sed -n '555p' _bmad-output/planning-artifacts/epics.md` | Guard requires child story files **or** a checklist evidence table; neither exists on a registration surface | `confirmed` |
| "`story-scope-guard.md:27-31` binds at registration at any status, including `backlog`" | Location | read `_bmad/custom/story-scope-guard.md:27-31` | Exact text present | `confirmed` |
| Split arithmetic "11 evidence-bearing + 14 activation-blocked = 25" | Quantitative | C1.15-C1.24 = 10, +C1.25 = 11; C1.1-C1.12 = 12, +C1.14 = 13, +C1.13 = 14; 11+14 = 25 | Arithmetic holds | `confirmed` |
| "AC6 pins **the two** vault-typed Components" | Existence | `sed -n '4916p' epics.md`; `sed -n '55p'` story | "the two applied Dapr secret-store Components" in both copies | `confirmed` |
| "`verify-production-deployment.ps1:918-926` deleted the zero-component throw" | Location | `awk 'NR>=905 && NR<=935'` | `:918-926` is `Set-CapacityPreservingMemoriesRollout`. The deleted throw was at `:863-865` pre-patch (story `:498`); the deliberate no-throw is now at `:147-151`. A distinct zero-Component enumeration throw exists at `:140-142`. Substance holds; anchor wrong. | `corrected` |
| "`production_deployment_evidence_test.py:855` forbids the named-two shape" | Location | `awk 'NR>=840 && NR<=875'` | `:855` is `assertLess(substitution_call, scale_down)`. The prohibition is `assertNotIn("'patch', 'component', 'secretstore'")` at `:862`; dynamic enumeration pinned at `:860`. Substance holds; anchor wrong. | `corrected` |
| "the story copy at `:55` carries the anti-skip clause and the evidence-upload step, `epics.md:4916` carries neither" | Behavioral | `sed -n '55p'` story vs `sed -n '4916p'` epics | Story: "and the evidence artifact is uploaded … rather than being skipped". `epics.md`: neither phrase. | `confirmed` |
| "both drop 'evidence production' from the approved fail list" | Existence | same two reads | Both fail lists enumerate render, verbatim apply, required substitution, health stage, evidence-validation. Neither lists evidence production or upload. | `confirmed` |
| "approximately 55 lines of normative content (Annex A)" | Quantitative | `grep -n "Annex A"`; `wc -l` | Annex A spans `:413`-`:464` = 52 lines | `corrected` |
| "a rewritten `:118` authorization row" | Location | `awk 'NR>=100 && NR<=120'` | `:118` reads "**Amended 2026-07-30 by code review, ratified by the Administrator.**" | `confirmed` |
| "`:351` authorizes **only** the artifact edits enumerated here" | Location | `awk 'NR>=349 && NR<=353'` | Exact text present | `confirmed` |
| "`:404` records approval of **the complete proposal**, undated for the amendment" | Location | `awk 'NR>=402 && NR<=406'`; `grep -n -i approv` | `:404` reads "Administrator approved implementation on 2026-07-30"; the "complete proposal" wording is at `:410-411`. Substance holds; anchor split. | `corrected` |
| "`:115`, `:102-104`, `:305`, and success criteria 4 and 5 still assert `epics.md` gains the guard records and checkpoint table" | Behavioral | reads at each anchor; `awk 'NR>=360 && NR<=373'` | All four still assert it; success criteria 4 and 5 are at `:365-368` | `confirmed` |
| "`epics.md:4984-4986` and AC2 at `:4912` bind configured-capacity admission to **the exact running target**" | Location | `awk 'NR>=4980 && NR<=4990'`; `sed -n '4912p'` | `:4984` "Given the exact running target"; `:4986` "configured-capacity admission" | `confirmed` |
| "`story-scope-guard.md:68-69` forbids closing on internal-only proof" | Location | read `:68-69` | "Confirm externally observable proof is present wherever current artifacts require API, CLI, contract, trace, integration, or downstream-consumer proof." | `confirmed` |
| "the 2026-07-30 finding at `:497` ruled C1.13 needs no activation gate" | Location | `grep -n "needs no activation gate"` | Present at story `:497`; withdrawal already recorded at story `:560`. The DW rationale names no file; it resolves to the story artifact, not the proposal. | `confirmed` |
| "story `:13-14` and `:27-29` still read 'qualified against every C1 gate'" | Location | `awk 'NR>=11 && NR<=30'` story | `:13` is the title and carries no C1 phrase; `:14` is blank; the phrase is at `:28`. Both named targets (title, statement) are real. | `corrected` |
| "`epics.md:4903-4907` is identical and carries no supersede note" | Behavioral | `awk 'NR>=4903 && NR<=4907'` | Identical wording; no supersede note inside the block | `confirmed` |
| **New —** the lane deliberately does not throw when zero Components are vault-typed | Behavioral | `awk` over `verify-production-deployment.ps1:147-151` | "No throw when nothing is vault-typed. Story 31.2 delivers the real OpenBao path … the state DW 27.3-CR29/CR30 must be able to discharge." Constrains E2: AC6 must drop "required substitution", not re-tighten it. | `confirmed` |

No claim is left without a verdict. No `corrected` claim lacks its planning-artifact
correction. No `unverifiable` claim is used as load-bearing justification for scope.

---

## 6. Change Navigation Checklist

| Section | Status | Notes |
| :------ | :----- | :---- |
| 1. Trigger and context | `[x] Done` | Story 27.3 code review 2026-07-31; failed-approach category; evidence recorded in §1 |
| 2. Epic impact | `[x] Done` | Epic 27 completes as planned; no epic added, removed, or resequenced |
| 3. Artifact conflicts | `[x] Done` | PRD `[N/A]`, Architecture `[N/A]`, UX `[N/A]`; five artifacts `[!]` and addressed in §4 |
| 4. Path forward | `[x] Done` | Direct Adjustment selected; rollback and MVP review evaluated and rejected with reasons |
| 5. Proposal components | `[x] Done` | Before/after edits, verification table, handoff, success criteria, validation plan |
| 6. Final review/handoff | `[x] Done` | Administrator approved 2026-07-31; edits E1-E6 applied by `correct-course` the same day |

---

## 7. Implementation Handoff

**Scope classification: Moderate.** Backlog reorganization plus acceptance-criterion
amendment; no fundamental replan and no architectural change.

| Recipient | Responsibility |
| :-------- | :------------- |
| Administrator | Approve, reject, or revise this complete proposal. Approval authorizes only the artifact edits enumerated in §4, and separately re-approves `sprint-change-proposal-2026-07-30.md` as amended per E5. |
| Developer (`correct-course`) | After approval, apply E1-E6 to `epics.md`, the Story 27.3 artifact, `sprint-status.yaml`, `sprint-change-proposal-2026-07-30.md`, and `deferred-work.md`. Preserve unrelated dirty-worktree changes, including the three excluded `references/` gitlinks. Run the validation plan. Do not advance any status. |
| `create-story` for Stories 27.5 and 27.6 | When the activation gate opens, author each implementation story file from its registered checkpoint contract and declare future producer paths. Not authorized by this proposal. |
| Story 31.2 owner | Unchanged. Still owns `DW 27.3-CR29` and `CR30` and the real OpenBao runtime path. |
| Production-deployment lane owner | Owns `DW 27.3-CR36`. |

### Success criteria

1. Story 27.3's title, `## Story` statement, and `epics.md` block describe the retained
   C0/C2-C4 scope, and the supersede note is present inside the `epics.md` Story 27.3
   block.
2. Both governed AC6 copies state the dynamic-enumeration contract in identical terms,
   list evidence production and upload as failing steps, carry the anti-skip clause, and
   do **not** make substitution mandatory.
3. Story 27.5 carries `Historical Context Classification`, `Slice Proof`, and eleven
   one-row-per-gate checkpoint rows in `epics.md`, all `pending | not complete | —`.
4. Story 27.6 exists in `epics.md` with fourteen one-row-per-gate rows and is registered
   `backlog` in `sprint-status.yaml`, appearing in both the Epic 27 story list and the
   status map.
5. C1.13's registered row states a running-target observation as its evidence and names
   the unit lane only as a precondition.
6. AC1-AC5 retain their full text and each carries the transfer annotation; AC2's stale
   "Story 27.5" clause is corrected and includes C1.13.
7. `sprint-change-proposal-2026-07-30.md` carries four dated correction notes, the Annex A
   supersede header, and the dated second approval.
8. `DW 27.3-CR36` is recorded; CR31-CR35 are `resolved`; CR28, CR29, CR30 and the thirteen
   chunk-1 patches remain open and untouched.
9. Production writes stay disabled; Stories 27.4, 27.5, and 27.6 stay `backlog`; Story
   27.3 stays `in-progress`; A41 stays open.
10. The three excluded `references/` gitlinks are unchanged by this correction.
11. *(E7, post-approval)* `story-creation-lessons.md` carries `## L14`, and this proposal
    records E7 as a dated amendment rather than as part of the approved E1-E6 set.

### Validation plan after approval

```text
PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p '*_test.py' -v
PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/bmad_customization -p '*_test.py' -v
python3 tools/check-story-slice-scope.py --story-key 27-3-production-adapter-and-deployment-profile
git diff --stat -- references/
```

The `check-story-slice-scope.py` invocation validates the Story 27.3 record only. It
proves the classification and slice-proof records exist and are well formed; it does not
judge whether a classification is correct or whether two outcomes are genuinely
independent. Per `story-scope-guard.md:58-61` a green gate is evidence the record exists,
never evidence the record is right. The `git diff --stat -- references/` check confirms
the three excluded gitlinks are untouched.

---

## 8. Approval

- [x] Administrator approves this Sprint Change Proposal for implementation.
- [x] Administrator re-approves `sprint-change-proposal-2026-07-30.md` as amended (E5).

**Approval recorded:** Administrator replied `yes` on 2026-07-31 after the complete proposal
was presented for approval, having separately approved each of E1-E6 during incremental review.
Status: **approved and implemented**.

**Execution note.** E6(c) was strengthened during application: rather than recording the three
excluded `references/` gitlinks in this proposal alone, they are also registered as
`DW 27.3-CR37` with an owner, a reopen trigger keyed to "before any Story 27.3 commit is
created", and both discharge paths. A prose exclusion in a proposal is not a gate; a ledger
entry with a reopen trigger is the same reasoning `story-phase-ledger.md` applies to blocked
discovery. Nothing else departed from the approved text.

**Post-approval amendment 2026-07-31.** E7 was authorized by the Administrator after this
proposal was approved and implemented, and is recorded in §4 as a dated amendment with its
own authorization line. The approval above covers E1-E6; E7 carries its own. This is the
shape E5 requires of `sprint-change-proposal-2026-07-30.md`, applied to this document.
