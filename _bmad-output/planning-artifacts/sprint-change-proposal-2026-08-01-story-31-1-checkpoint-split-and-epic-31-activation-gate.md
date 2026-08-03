# Sprint Change Proposal — 2026-08-01 (Story 31.1 checkpoint split and Epic 31 activation gate)

**Stories:** `31-1-openbao-platform-hardening-and-documentation`, `31-2-runtime-dapr-secret-store-migration`
**Workflow:** `correct-course` (Developer route)
**Decision owner:** Administrator (`jpiquot`)
**Status:** Approved 2026-08-01 by Administrator (`jpiquot`) and implemented 2026-08-01
**Change classification:** Moderate
**Evidence base:** current worktree at HEAD `1d9e9c89ef53d877b4ec09face575c36e5889854`
**Trigger:** `dev-story 31.2` halted at its own Task 0 activation gate on 2026-08-01

> **Correction 2026-08-02:** This proposal amended Epic 31's activation-gate copy but did not amend Story
> 31.2's local `## Blocking Activation Gate` or Task 0. Those copies retained the superseded requirement that
> Story 31.1 be `done`, so the claim below that Story 31.2 became developable was incomplete. The
> Administrator-approved 2026-08-02 Story 31.2 Dapr/OpenBao artifact-alignment proposal corrects both local
> copies and adds a planned executable coherence guard. Historical text below remains unchanged for
> provenance.

> **Distinct from** `sprint-change-proposal-2026-08-01.md` (Story 27.3 / Stories 27.5–27.6 registration
> rollback). That proposal states at its section 3 that "Story 31.2 remains the source of the future
> non-vault-typed end state referenced by AC6". This proposal does not disturb that; it changes when Story
> 31.2 may be developed, not what it owns.

## 1. Issue Summary

A `dev-story` execution against Story 31.2 halted at Task 0. The story's Blocking Activation Gate requires
Story 31.1 to be `done`; Story 31.1 is `in-progress`, and **no development on either story can change that**.

The blocker chain is precise:

1. Story 31.1 has **zero unchecked task boxes** — all tasks and all 38 code-review findings are `[x]`.
2. Its checkpoints C4 and C5 read `pending` / `not complete`. Each bundles **two distinct gates** under one
   shared state: a documentation obligation, and an independent security-reviewer countersignature.
3. The documentation half of both is **complete and asserted by an executed guard** (verified below, 3/3
   passing).
4. The countersignature half was never obtained. It is carried by checkpoint C7, which was closed on
   2026-07-28 by an approved time-bounded waiver expiring 2026-10-26 — a waiver deliberately scoped so that
   it "waives **no** acceptance criterion, neither accepted limitation, and **no other checkpoint**"
   (`31-1-openbao-platform-evidence.md:443`).
5. `story-phase-ledger.md` forbids `done` while any evidence row reads `pending`. So Story 31.1 cannot reach
   `done`, and Story 31.2 cannot be developed.

The narrow waiver scoping was correct and is not the defect. The defect is that Story 31.2's activation gate
binds on `done` — a status that encodes an obligation the gate's own stated rationale does not mention.

### The gate says what it is for, and that condition is already met

> **Activation gate:** Story 31.2 must not enter implementation until Story 31.1 is `done`, **so that the
> migration is evaluated against a documented platform whose accepted limitations are already on record.**
> — `epics.md:5300`

The stated purpose is a *documented platform with its accepted limitations on record*. That is true today and
is executably asserted. What is missing — an independent countersignature evaluating those limitations — is a
real and open obligation, but it is not the condition the gate names, and Story 31.2's development neither
depends on it nor can produce it.

### Two compound checkpoint rows violate `epics.md:554`

> "A single table row covering multiple gates does not satisfy it — one row per gate is required, because a
> shared review state and completion date cannot record partial completion." — `epics.md:554`

C4 and C5 each carry two gates under one state. This is the same defect Story 31.1's own second-pass code
review found in C2 and cured by splitting the `helm diff` reproduction gate into its own row. C4 and C5 were
not given the same treatment.

## 2. Impact Analysis

### Epic impact

| Epic | Impact |
| :--- | :----- |
| Epic 31 | Story 31.2's activation gate is rebound to the condition the gate states. Story 31.1's checkpoint table gains two rows by splitting two existing rows. No acceptance criterion is added, removed, or weakened in either story. |
| Epic 29 | None. Story 29.2 (`backlog`) still owns provider-neutral Aspire composition and `deploy/dapr/**`. |
| Epic 27 | None. Story 27.3's C1 registration rollback under `sprint-change-proposal-2026-08-01.md` is untouched. |
| All other epics | No scope, sequencing, or status change. |

### Story impact

- **Story 31.1** — stays `in-progress`. C4 splits to C4a/C4b, C5 splits to C5a/C5b. The documentation gates
  close on already-executed evidence; the countersignature gates stay open with owner, consequence, and
  reopen trigger. Story 31.1 still does not reach `done` under this proposal.
- **Story 31.2** — stays `ready-for-dev` and becomes **developable**. Gains the missing
  `### Epic AC Verification` section, a widened `epics.md` Owned-paths list (its Open Decision D3), and a
  corrected testing baseline.

### Artifact conflicts

| Artifact | Conflict | Resolution |
| :------- | :------- | :--------- |
| `sprint-change-proposal-2026-07-28-story-31-1-deployed-profile-ac2-ratification.md` success criterion 5 | States "Both accepted limitations remain in scope with checkpoint rows C4 and C5 `pending` / `not complete`." | **Superseded in part, explicitly.** The limitations stay in scope; the countersignature rows stay `pending` / `not complete`. Only the documentation half moves, into its own rows. Recorded in §4.6. |
| `sprint-change-proposal-2026-07-28-story-31-1-scope-ratifications.md` success criterion 5 | "Checkpoints C4, C5 and C7 are unchanged by this proposal." | Not a constraint on future changes — it scopes that proposal. C7 is untouched here. |
| `epics.md:5300` gate reading ratified 2026-07-28 | "must not be developed until Story 31.1 is `done`" | Amended by this proposal, with the reason recorded inline. The `dev-story`-not-preparation reading is **kept**. |
| `epics.md:5321` Owned paths | Lists one path; Story 31.2's own Implementation-evidence clause requires an inventory, a structural proof, and a negative proof that cannot live in it. Story 31.2 routed this as Open Decision D3. | Widened in §4.3. |
| Story 31.2 missing `### Epic AC Verification` | `_bmad/custom/epic-ac-verification.md` fail-closes `ready-for-dev` without it. Story 31.1 has one at line 340; Story 31.2 has none. | Added in §4.4. |

### Technical impact

**None.** No source file, manifest, Dapr component, cluster object, pipeline, or dependency changes. No
`helm` command, no `kubectl` mutation, no rotation of any credential. `helm` remains absent from this
environment. This correction edits planning artifacts, story files, and one sprint-status comment block.

## 3. Recommended Approach

**Option 1 — Direct Adjustment. Selected.** Effort: Low. Risk: Low.

Amend Epic 31's activation gate to bind on the documented-platform condition it already states, and split the
two compound checkpoint rows so each gate carries its own state.

| Option | Verdict | Reason |
| :----- | :------ | :----- |
| 1. Direct Adjustment | **Selected** | Closes no security gate, claims no evaluation that did not happen, and leaves every real obligation open with its owner. Also cures a live `epics.md:554` violation using the precedent Story 31.1's own review set for C2. |
| 2. Potential Rollback | Not viable | Nothing needs reverting. Story 31.1's work is complete and evidenced; the waiver's narrow scoping was a correct decision worth preserving. |
| 3. MVP Review | Not applicable | Epic 31 is `operational-readiness`, `mvpReadiness: excluded`. No MVP scope is touched. |
| Alternative A — widen the C7 waiver to cover C4/C5 | Rejected | Reverses a deliberate scoping decision and closes three gates on one approver already recorded as non-independent — the pairing Story 27.3 withdrew on 2026-07-26. Same practical result, far higher governance cost. |
| Alternative B — obtain the security evaluation | Not rejected; **not available now** | The honest discharge, and it remains the reopen path for C4b/C5b/C7. The waiver exists precisely because no independent reviewer was available. This proposal does not foreclose it. |

**What this proposal does not do:** it does not set Story 31.1 to `done`, does not close or extend the C7
waiver, does not claim any security evaluation occurred, and does not weaken either accepted limitation.

## 4. Exact Authorized Planning Changes

Approval authorizes all edits in this section as one atomic correction. A partial application is not
authorized.

### 4.1 `epics.md:5300` — Story 31.2 activation gate

OLD:

> **Activation gate:** Story 31.2 must not enter implementation until Story 31.1 is `done`, so that the
> migration is evaluated against a documented platform whose accepted limitations are already on record.
> **Gate reading ratified 2026-07-28 by the Administrator, during Story 31.1's second-pass code review:**
> "enter implementation" means a `dev-story` execution, not story preparation. Story 31.2 may sit at
> `ready-for-dev` while Story 31.1 is `review`; it must not be developed until Story 31.1 is `done`. This
> reading is recorded because `sprint-status.yaml` and this file had drifted apart on it.

NEW:

> **Activation gate:** Story 31.2 must not enter implementation until Story 31.1's platform documentation is
> complete and guard-asserted — specifically until Story 31.1 checkpoints C1, C2, C3, C4a, C5a and C6 all
> read `complete` — so that the migration is evaluated against a documented platform whose accepted
> limitations are already on record. **Gate reading ratified 2026-07-28 by the Administrator, during Story
> 31.1's second-pass code review, and retained:** "enter implementation" means a `dev-story` execution, not
> story preparation. **Amended 2026-08-01 by approved Sprint Change Proposal 2026-08-01 (Story 31.1
> checkpoint split and Epic 31 activation gate):** the gate previously bound on Story 31.1 reaching `done`.
> Story 31.1 cannot reach `done` while checkpoints C4b, C5b and C7 record an un-obtained independent security
> countersignature, and no development on either story can produce that countersignature — so the `done` form
> made Story 31.2 permanently undevelopable for a reason the gate does not state. The gate now binds on the
> condition it does state. The countersignature obligation is neither waived nor moved: it stays open on
> Story 31.1 as C4b, C5b and C7 with their owners and reopen triggers, and Story 31.1 does not reach `done`
> until it is discharged.

**Rationale:** binds the gate to its stated purpose, which is executably verified, instead of to a status
that encodes an unrelated and currently unobtainable obligation.

### 4.2 Story 31.1 — split checkpoints C4 and C5

Replace the single `C4` row with `C4a` and `C4b`, and the single `C5` row with `C5a` and `C5b`. Owners,
evidence commands, and the existing review-state prose carry over unchanged to the half they belong to.

| Checkpoint | Accountable owner | Required evidence artifact and command | Review state | Completion state | Completion date |
| :--------- | :---------------- | :------------------------------------- | :----------- | :--------------- | :-------------- |
| C4a - Accepted limitation **documented**: static file-based seal | Hexalith Platform Operations (`jpiquot`) | The `Static file-based seal` row of the accepted-limitations table in `docs/operations/openbao.md`, with owner, consequence, compensating controls, and reopen trigger all substantive. Own command: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -method Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests.AcceptedLimitations_HaveExactHeaderTwoKeyedRowsAndNoEmptyCell -method Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests.AcceptedLimitations_AreNeverDescribedWithStrengthVocabulary -parallel none -noLogo` | verified — guard executed 2026-07-28, its compensating-controls cell corrected by code review 2026-07-28, and re-executed 2026-08-01 (3 methods with C5a's selector, 0 failed) | complete | 2026-08-01 |
| C4b - Independent security **countersignature**: static file-based seal | Security reviewer — **unassigned**; the `murat-tea-for-jpiquot` persona is recorded non-independent in `31-1-openbao-platform-evidence.md:441`. Escalation owner: Administrator (`jpiquot`) | A dated, named evaluation by an authority independent of the Platform Operations owner, recorded in `31-1-openbao-platform-evidence.md` section 4, countersigning the seal limitation **as measured**. Own command: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -method Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests.PlatformEvidence_RecordsExecutedSmokeTestAndReviewerState -parallel none -noLogo` | pending — no independent countersignature obtained. Consequence: the static file-based seal stands accepted without an independent authority having evaluated it. Reopen trigger: the C7 waiver expiry 2026-10-26, or any earlier trigger in evidence section 4 | not complete | — |
| C5a - Accepted limitation **documented**: namespace-wide port 8200 ingress | Hexalith Platform Operations (`jpiquot`) | The `Namespace-wide port 8200 ingress` row of the same table, naming the admitted namespaces and the pods they cover as measured. Own command: the two C4a selectors, **plus** `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -method Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests.DeployedProfileTable_PinsEveryMeasuredRowByKey -parallel none -noLogo` | verified — guard executed 2026-07-28 and re-executed 2026-08-01 (3 methods, 0 failed). The `DeployedProfileTable_PinsEveryMeasuredRowByKey` selector was added by second-pass code review 2026-07-28 because the two C4a selectors assert no namespace or pod name | complete | 2026-08-01 |
| C5b - Independent security **countersignature**: namespace-wide port 8200 ingress | Security reviewer — **unassigned**, as C4b. Escalation owner: Administrator (`jpiquot`) | As C4b, countersigning the port 8200 ingress limitation as measured | pending — no independent countersignature obtained. Consequence: the namespace-wide port 8200 ingress stands accepted without an independent authority having evaluated it. Reopen trigger: as C4b | not complete | — |

Add immediately below the table:

> **Split recorded 2026-08-01** by approved Sprint Change Proposal 2026-08-01 (Story 31.1 checkpoint split and
> Epic 31 activation gate). C4 and C5 each carried two gates — a documentation obligation and an independent
> countersignature — under one shared review and completion state, against `epics.md:554`. This is the same
> defect this story's second-pass code review cured for C2 by splitting out the `helm diff` gate. Splitting
> closes nothing that was open: the documentation halves were already asserted by an executed guard, and the
> countersignature halves remain `pending` / `not complete`. Story 31.1 still does not reach `done`.

### 4.3 `epics.md:5321` — Story 31.2 Owned paths (discharges Story 31.2 Open Decision D3)

OLD:

> **Owned paths:** `deploy/kubernetes/base/dapr/secretstore.yaml`.

NEW:

> **Owned paths:** `deploy/kubernetes/base/dapr/secretstore.yaml`;
> `deploy/kubernetes/base/service-accounts-rbac.yaml` (only if the per-Secret inventory dispositions residual
> grants); the per-Secret justification inventory appended to `docs/operations/openbao.md` (jointly owned —
> Story 31.1's measured-platform sections are frozen);
> `tests/Hexalith.Memories.Server.Tests/Deployment/RuntimeSecretStoreMigrationTests.cs`;
> `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` (only on collision,
> with no assertion deleted to get green);
> `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md`;
> `_bmad-output/implementation-artifacts/deferred-work.md` (the `DW 27.3-CR17` disposition only); and
> `tools/verify-production-deployment.ps1` (only under Open Decision D2, and only with the two cross-pinned
> lanes re-run). **Widened 2026-08-01** by approved Sprint Change Proposal 2026-08-01, discharging Story
> 31.2's Open Decision D3: the single-path list contradicted this story's own Implementation-evidence clause,
> which requires an inventory, a structural proof, and a negative proof that cannot live in a Dapr component
> manifest. Story 31.1 hit the identical contradiction and its code review raised it as a finding.

### 4.4 Story 31.2 — add the missing `### Epic AC Verification` section

`_bmad/custom/epic-ac-verification.md` requires this section under `## Dev Notes` and fail-closes
`ready-for-dev` without it. Story 31.2 has none. Insert, before `### Scope Boundary`:

> #### Epic AC Verification
>
> Verified 2026-08-01 against HEAD `1d9e9c89`, by approved Sprint Change Proposal 2026-08-01. The story was
> registered 2026-07-28 without this section; this is a compliance repair, not a reconstruction of history.
> Every row was re-derived against current source.
>
> | Epic claim | Class | Command / evidence | Observed | Verdict |
> | :--------- | :---- | :----------------- | :------- | :------ |
> | "`deploy/kubernetes/base/dapr/secretstore.yaml` uses `hashicorp.vault` with the `eventstore` and `memories` scopes" | Existence | `grep -n 'type:\|scopes:' -A2 deploy/kubernetes/base/dapr/secretstore.yaml` | To be re-derived by Task 2 before any edit | `confirmed` at creation via commit `4d2e4e2f`; **Task 2 must re-derive** |
> | "no product project contains an OpenBao SDK, HTTP client, endpoint, or provider credential" | Absence | The Task 5 structural guard, once authored | No such guard exists today — Task 5 creates it | `confirmed` as a gap to close, not as an existing property |
> | "Story 31.1 is `done`" (inherited activation gate) | Behavioral | `grep -n '31-1-openbao' _bmad-output/implementation-artifacts/sprint-status.yaml` | `in-progress`, and unreachable while C4b/C5b/C7 are open | **`corrected`** — planning artifact corrected by §4.1 of this proposal |
> | "Epic 31 Owned paths for Story 31.2 is `deploy/kubernetes/base/dapr/secretstore.yaml`" | Location | `sed -n '5321p' _bmad-output/planning-artifacts/epics.md` | Exactly one path, contradicting the story's own Implementation-evidence clause | **`corrected`** — planning artifact corrected by §4.3 of this proposal |
> | "no workload in namespace `hexalith-memories` carries app-id `eventstore`" | Existence | `kubectl -n hexalith-memories get pods -o jsonpath='{range .items[*]}{.metadata.annotations.dapr\.io/app-id}{"\n"}{end}'` | Measured 2026-07-28; **Task 1 must re-derive** before Task 4 relies on it | `confirmed` at creation; re-derivation required |
> | "`Hexalith.Memories.Server.Tests` discovers 2,200 xUnit methods (create baseline)" | Quantitative | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo` | **2,203** at HEAD `1d9e9c89` | **`corrected`** — +3 external, owned by Story 31.1; see §4.5 |
> | "`deploy/dapr/components/secretstore.yaml` still declares `type: secretstores.kubernetes`" | Existence | `grep -n 'type:' deploy/dapr/components/secretstore.yaml` | To be re-derived by Task 7 before it scopes its assertion | `confirmed` at creation; Task 7 must re-derive |

### 4.5 Story 31.2 — correct the stale testing baseline

Story 31.2 records a create baseline of **2,200** methods with sorted method-set SHA-256
`eab323be548a65055fe86d0a421909b238bdaa33975719b6c3fd50ee02b656ae`, and asserts it "carries **no** external
same-lane delta to separate". Both statements are now stale.

Measured 2026-08-01 at HEAD `1d9e9c89`, after a clean Release build (0 warnings, 0 errors):

| Lane | Recorded create baseline | Observed 2026-08-01 | External delta |
| :--- | -----------------------: | ------------------: | -------------: |
| `Hexalith.Memories.Server.Tests`, all xUnit methods | 2,200 | **2,203** | **+3** |
| `Deployment` namespace | 58 | **61** | **+3** |
| `OpenBaoPlatformDocumentationTests` | 10 | **13** | **+3** |
| `ProductionDeploymentArtifactsTests` | 9 | 9 | +0 |
| `RuntimeSecretStoreMigrationTests` | 0 | 0 | +0 |

Sorted method-set SHA-256: `aacac6f53f49f2a7450345646e76fc7ffde2e896d716f8785ab08457f347f22a`.

The three methods are `DeployedProfileTable_PinsEveryMeasuredRowByKey`,
`OperationalSections_StayBoundToTheirRecordedRemediations`, and
`SecretShapeGuard_ExcusesALabelledDigestAndReportsAnUnlabelledPaste`, added by commit `a4517654` and owned by
**Story 31.1's second-pass code review**. Verified with
`git diff 115d30b5..HEAD -- tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs`.

The cause is worth recording: commit `a4517654` bundled Story 31.2's create-story output **and** Story 31.1's
post-review test additions, while Story 31.2's `baseline_commit` is `115d30b5`, that commit's parent. The
baseline was therefore measured one commit before three methods that landed alongside the story file itself.

Amend Story 31.2's `### Testing Baseline and Planned Delta` to carry both numbers, so the reconciliation is
`create baseline 2,200 + story delta 0 + external delta +3 = observed 2,203`. Do **not** rewrite the
`create-story` Change Log row; the correction belongs in the Dev Notes baseline section and in the
`correct-course` ledger row.

### 4.6 Superseded prior approval — stated explicitly

`sprint-change-proposal-2026-07-28-story-31-1-deployed-profile-ac2-ratification.md` success criterion 5 reads:
"Both accepted limitations remain in scope with checkpoint rows C4 and C5 `pending` / `not complete`."

This proposal **supersedes it in part**. Both limitations remain in scope — unchanged. The countersignature
gates remain `pending` / `not complete` — unchanged, now as C4b and C5b. Only the documentation gates move to
`complete`, in rows of their own, on evidence that was already executed when that criterion was written.
Nothing that criterion protected is weakened; it is recorded at finer granularity.

### 4.7 `sprint-status.yaml` — comment only

No status value changes. Story 31.1 stays `in-progress`; Story 31.2 stays `ready-for-dev`. Append to the
`epic-31` comment block:

> `# Story 31.2's activation gate amended 2026-08-01 by approved Sprint Change Proposal 2026-08-01`
> `# (Story 31.1 checkpoint split and Epic 31 activation gate). It now binds on Story 31.1's documentation`
> `# checkpoints C1/C2/C3/C4a/C5a/C6 rather than on Story 31.1 reaching 'done'. Story 31.1 stays in-progress:`
> `# C4b, C5b and C7 carry an un-obtained independent security countersignature, reopen trigger 2026-10-26.`

### 4.8 Story phase ledger rows

`story-phase-ledger.md` admits `correct-course` as a canonical phase, required when that phase actually ran.
On application, append one `correct-course` row to **each** story's Change Log with actual phase delta `+0`,
the observed totals above, the named external delta and its Story 31.1 owner, and a reconciled cumulative
File List.

## 5. Epic AC Verification for this proposal

Required by `_bmad/custom/epic-ac-verification.md`, which binds this route. Every verifiable claim this
proposal asserts, re-derived 2026-08-01 at HEAD `1d9e9c89`.

| Claim | Class | Command / evidence | Observed | Verdict |
| :---- | :---- | :----------------- | :------- | :------ |
| Story 31.1 is not `done` | Existence | `grep -n '31-1-openbao' _bmad-output/implementation-artifacts/sprint-status.yaml`; `grep -n '^Status:' 31-1-…md` | `in-progress` in both | `confirmed` |
| Story 31.1 has zero unchecked task boxes | Quantitative | `grep -c '\[ \]' 31-1-…md` | `0` | `confirmed` |
| C4/C5 read `pending` / `not complete` | Existence | Read `31-1-…md:229-230` | Both do | `confirmed` |
| The C4/C5 documentation halves are asserted by an executed, passing guard | Behavioral | `dotnet exec … -method AcceptedLimitations_HaveExactHeaderTwoKeyedRowsAndNoEmptyCell -method AcceptedLimitations_AreNeverDescribedWithStrengthVocabulary -method DeployedProfileTable_PinsEveryMeasuredRowByKey -parallel none -noLogo` | **Total: 3, Failed: 0** | `confirmed` |
| The C7 waiver is scoped to C7 only | Existence | Read `31-1-openbao-platform-evidence.md:443` | "waives **no** acceptance criterion, neither accepted limitation, and no other checkpoint" | `confirmed` |
| The waiver approver is recorded non-independent | Behavioral | Read `31-1-openbao-platform-evidence.md:441` | "Not independent, and recorded as a defect"; cites Story 27.3's 2026-07-26 withdrawal of the same pairing | `confirmed` |
| `epics.md:554` requires one row per gate | Location | `sed -n '554p' _bmad-output/planning-artifacts/epics.md` | "A single table row covering multiple gates does not satisfy it — one row per gate is required" | `confirmed` |
| Story 31.1's review split C2's `helm diff` gate for this reason | Behavioral | Read `31-1-…md:208` | Finding cites `epics.md:554` and requires the split | `confirmed` |
| The `helm diff` obligation is carved out of Story 31.1 | Behavioral | Read `31-1-…md:311` | Carved out to Platform Operations by the approved 2026-07-28 scope ratifications | `confirmed` |
| `helm` is absent from this environment | Existence | `command -v helm` | Not found (`kubectl` and `docker` present) | `confirmed` |
| Story 31.2 has no `### Epic AC Verification` section | Absence | `grep -in 'epic ac verification' 31-2-…md` | 0 matches; Story 31.1 has one at line 340 | `confirmed` |
| `epics.md` Story 31.2 Owned paths lists one path | Quantitative | `sed -n '5321p' _bmad-output/planning-artifacts/epics.md` | Exactly `deploy/kubernetes/base/dapr/secretstore.yaml` | `confirmed` |
| Story 31.2's recorded 2,200-method baseline is current | Quantitative | `dotnet exec … -list methods -noLogo` after a clean Release build | **2,203** — stale by +3 | **`corrected`** — corrected in §4.5 |
| The +3 is owned by Story 31.1, via `a4517654` | Location | `git diff 115d30b5..HEAD -- …/OpenBaoPlatformDocumentationTests.cs`; `git log --oneline -- <same>` | Three named methods added by `a4517654` | `confirmed` |

**Historical slice guard applicability:** this correction **creates, renames, and splits no story**. It splits
two checkpoint *rows* within an existing story. `_bmad/custom/story-scope-guard.md` binds story creation and
registration, so no `Historical Context Classification` or `Slice Proof` section is generated by this change.
Stated explicitly rather than left implied.

## 6. Implementation Handoff

**Scope classification: Moderate** — planning-artifact and story-file reorganization with no code change,
requiring Product Owner / Developer coordination but no PM or Architect replan.

| Recipient | Responsibility |
| :-------- | :------------- |
| Administrator (`jpiquot`) | Approve or reject. §4.6 supersedes part of a prior approval and needs an explicit decision. |
| Developer agent | Apply §4.1–§4.8 atomically once approved, then append both `correct-course` ledger rows. |
| Hexalith Platform Operations (`jpiquot`) | Retains C4b/C5b/C7. Unchanged by this proposal. |
| Security reviewer (**unassigned**) | C4b/C5b/C7 remain open pending assignment of an authority independent of the Platform Operations owner. |

### Success criteria

1. Story 31.2's activation gate binds on Story 31.1's documentation checkpoints, not on `done`.
2. Story 31.1 carries C4a/C4b/C5a/C5b, one gate per row, with the split note recorded.
3. Story 31.1 remains `in-progress`; C4b, C5b and C7 remain `pending` / `not complete` with owners and the
   2026-10-26 reopen trigger intact.
4. No security gate is closed and no evaluation is claimed. Both accepted limitations remain in scope.
5. Story 31.2 carries an `### Epic AC Verification` section and a corrected testing baseline naming the +3
   external delta and its Story 31.1 owner.
6. `epics.md` Story 31.2 Owned paths covers the paths its own Implementation-evidence clause requires.
7. A subsequent `dev-story 31.2` passes Task 0 rather than halting.

### Explicitly out of scope

Story 31.2's Open Decisions **D1** (the `eventstore` scope is unprovable by live read — no workload carries
that app-id) and **D2** (how `DW 27.3-CR17` is discharged) are **not** resolved here. Both are genuine scope
questions that Story 31.2 correctly routed for a human decision, and both will surface again during its
development. This proposal makes the story developable; it does not pre-decide those two.
