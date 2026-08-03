# Sprint Change Proposal — Story 31.2 Dapr/OpenBao Artifact Alignment

**Date:** 2026-08-02  
**Project:** Hexalith.Memories  
**Workflow:** `correct-course` (Developer route)  
**Mode:** Batch  
**Decision owner:** Administrator  
**Status:** Approved by Administrator and planning correction implemented 2026-08-02; runtime Story 31.2 implementation handed off  
**Change classification:** Minor planning correction; underlying Story 31.2 implementation remains medium-risk security/infrastructure work  
**Evidence base:** HEAD `3f758f9a`, current planning and implementation artifacts, and read-only probes against cluster context `jpiquot@local`

## 1. Issue Summary

Story 31.2 is the existing implementation slice for the Dapr/OpenBao runtime secret boundary. The deployed
cluster is not missing OpenBao: three OpenBao pods are Running, the Memories and MCP pods are `2/2 Running`,
and the live `secretstore` and `access-telemetry-secrets` Dapr components both report
`secretstores.hashicorp.vault`.

The immediate blocker is artifact drift left by two approved 2026-08-01 course corrections:

1. Epic 31 correctly changed Story 31.2's activation gate from “Story 31.1 is `done`” to the exact Story 31.1
   documentation checkpoints `C1`, `C2`, `C3`, `C4a`, `C5a`, and `C6`. All six currently read `complete`.
2. Story 31.2's own `## Blocking Activation Gate` and Task 0 still contain the superseded `done` condition,
   so a conforming `dev-story` run must halt even though the canonical epic gate is open.
3. Story 31.2 has already retargeted its active Dapr/OpenBao proof from `DW 27.3-CR17` to the open
   `DW 27.3-CR29` and `DW 27.3-CR30` entries, and correctly marks
   `tools/verify-production-deployment.ps1` out of scope. Epic 31's Story 31.2 **Owned paths** paragraph still
   routes ownership to the `CR17` disposition and conditionally includes that verifier.
4. The two source proposals are marked “implemented” without dated notes acknowledging these surviving
   omissions. Leaving those claims uncorrected would preserve false planning history.

This is a failed course-correction application and planning-artifact synchronization defect. It is not
evidence that the production Dapr/OpenBao secret-resolution path is complete. The component type and healthy
sidecars are confirmed; the bodyless, scoped Dapr secret read required to discharge `CR29` and `CR30` has not
been executed by this workflow and must remain a Story 31.2 implementation gate.

## 2. Impact Analysis

### Epic and story impact

| Artifact | Impact | Required action |
| :------- | :----- | :-------------- |
| Epic 31 | Intent and sequencing remain valid, but Story 31.2's Owned-paths routing is stale. | Retarget the deferred-work ownership to `CR29`/`CR30`, remove the verifier from ownership, and name the access-telemetry component and verifier as read/verify-only context. |
| Story 31.2 | `ready-for-dev` is correct, but its local gate forces a false halt. | Synchronize the blocking gate and Task 0 with the canonical checkpoint condition; add an executable coherence guard to the already-planned story test class. |
| Story 31.1 | No scope, acceptance-criterion, checkpoint, or status change. | Read only: its six documentation checkpoints remain the activation evidence. C4b/C5b/C7 remain open and continue blocking Story 31.1 `done`. |
| Epic 27 / Story 27.3 | No scope or status change. | `CR17` remains Story 27.3's checkpoint-C2 arm; `CR29`/`CR30` remain Story 31.2's runtime OpenBao proof gates. |
| Epic 29 | No scope or status change. | Aspire/AppHost-local and standalone Dapr template work remains outside Story 31.2. |
| Sprint status | Already coherent: Epic 31 and Story 31.1 are `in-progress`; Story 31.2 is `ready-for-dev`. | No `sprint-status.yaml` value change. |

### PRD, architecture, UX, and MVP impact

- **PRD:** No change. NFR9 already requires product services to retrieve runtime secrets through the Dapr
  Secrets API backed by OpenBao and requires integration proof without disclosure.
- **Architecture:** No change. D30 keeps infrastructure clients out of product code; D31 already requires
  successful Dapr reads, denial evidence, provider-dependency absence, and secret-safe diagnostics.
- **UX:** No flow, component, or accessibility change. The existing prohibition on exposing secrets in
  accessible text remains applicable to later implementation evidence.
- **MVP:** No scope change. Epic 31 remains operational-readiness work and the existing Story 31.2 slice is
  retained.
- **Technical implementation:** No runtime, manifest, cluster, or test change is authorized before proposal
  approval. After alignment, Story 31.2 still owns live scoped-read/denial evidence, the Kubernetes Secret
  inventory, no-SDK/no-leak guards, fail-closed cutover proof, and `CR29`/`CR30` disposition.

## 3. Recommended Approach

Select **Option 1 — Direct Adjustment**.

| Option | Verdict | Effort | Risk | Rationale |
| :----- | :------ | :----- | :--- | :-------- |
| Direct adjustment | **Selected** | Low for the planning correction; medium for the existing Story 31.2 implementation | Low planning risk; medium security/infrastructure implementation risk | Synchronizes the already-approved gate and ownership decisions without changing scope or inventing a second story. |
| Potential rollback | Not viable | High | High | The tracked and live Dapr components already use `secretstores.hashicorp.vault`; rollback would move away from PRD NFR9 and architecture D31. |
| PRD/MVP review | Not applicable | Unnecessary | Low value | The defect does not invalidate the product requirement, architecture, or MVP strategy. |

**Timeline impact:** no epic resequencing and no new story. Once the approved artifact correction is applied,
Story 31.2 can enter its existing `dev-story` flow immediately. Completion timing remains dependent on live
cluster evidence and security review, not on Story 31.1 reaching `done`.

## 4. Detailed Change Proposals

Approval authorizes all edits in this section as one correction. It does not authorize implementation of
Story 31.2's runtime tasks; that follows through the Developer handoff in §5.

### 4.1 Story 31.2 — synchronize the Blocking Activation Gate

**Artifact:** `_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md`  
**Section:** `## Blocking Activation Gate`

**OLD:**

> **Do not start implementation until Story 31.1 is `done`.** Epic 31 states: *“Story 31.2 must not enter
> implementation until Story 31.1 is `done`...”*

**NEW:**

> **Do not start implementation until Story 31.1 checkpoints C1, C2, C3, C4a, C5a, and C6 all read
> `complete`.** This is Epic 31's canonical activation gate. The six documentation and guard checkpoints are
> complete as re-derived on 2026-08-02, so the gate is open. Story 31.1 remains `in-progress` because
> C4b/C5b/C7 retain the independent-security-countersignature obligation; those rows still block Story 31.1
> `done` but do not block Story 31.2 development.

Retain the approved 2026-08-01 rationale and link it to
`sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md`.

**Rationale:** Story 31.2's local gate must not contradict its canonical epic or its own Epic AC Verification
row, which already records the `done` premise as `corrected`.

### 4.2 Story 31.2 — make Task 0 evaluate the real gate

**Artifact:** `_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md`  
**Section:** `Task 0 - Verify the activation gate before any other work`

**OLD:**

> Re-read `sprint-status.yaml`. If `31-1-openbao-platform-hardening-and-documentation` is not `done`, halt.

**NEW:**

> Re-read Story 31.1's Implementation Checkpoints and verify that C1, C2, C3, C4a, C5a, and C6 each read
> `complete`. If any named row is absent, duplicated, or not complete, halt before Task 1 and report the exact
> row. Do not require Story 31.1 itself to be `done`; C4b, C5b, and C7 remain independent Story 31.1 closure
> gates.

Add a planned method to
`tests/Hexalith.Memories.Server.Tests/Deployment/RuntimeSecretStoreMigrationTests.cs` that binds the epic gate,
the story gate, and Task 0 to the same six-row set and mutation-proves that reintroducing the `done` condition
or removing one checkpoint fails. This is part of the existing Story 31.2 proof slice, not a new story or
independent outcome.

**Rationale:** A prose-only correction already failed to reach every governed copy. The executable guard
prevents the same split-brain state from recurring.

### 4.3 Epic 31 — correct Story 31.2 Owned paths

**Artifact:** `_bmad-output/planning-artifacts/epics.md`  
**Section:** `Story 31.2: Runtime Dapr Secret-Store Migration to hashicorp.vault` / `Owned paths`

**OLD excerpt:**

> `_bmad-output/implementation-artifacts/deferred-work.md` (the `DW 27.3-CR17` disposition only); and
> `tools/verify-production-deployment.ps1` (only under Open Decision D2...)

**NEW excerpt:**

> `_bmad-output/implementation-artifacts/deferred-work.md` (the `DW 27.3-CR29` and `DW 27.3-CR30`
> dispositions only). Read/verify-only context, not Story 31.2 ownership:
> `deploy/kubernetes/base/dapr/access-telemetry-secrets.yaml` for the `CR29` live-type and consumer-resolution
> proof, and `tools/verify-production-deployment.ps1` for historical/disposable-lane context. The verifier and
> its Kubernetes-store substitution remain Story 27.3-owned and must not be edited by Story 31.2.

Keep the rest of the approved widened path list unchanged.

**Rationale:** `CR17` was split and now covers Story 27.3 only. The open Story 31.2 gates are `CR29` and
`CR30`; the disposable-cluster verifier is explicitly out of Story 31.2 scope.

### 4.4 Correct the two historical “implemented” claims

Add dated correction notes without rewriting historical text:

1. In
   `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md`,
   record that the Epic gate was amended but Story 31.2's local Blocking Activation Gate and Task 0 retained
   the superseded `done` condition. Its “Story 31.2 becomes developable” claim was therefore incomplete until
   this proposal.
2. In
   `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-2-reserved-eventstore-scope-and-deferred-work-retarget.md`,
   record that Story 31.2 was retargeted to `CR29`/`CR30` and removed the verifier from its own scope, while
   the Epic 31 Owned-paths copy retained `CR17` and the verifier. Its “implemented” status was therefore
   incomplete until this proposal.

**Rationale:** The epic-AC verification policy requires a corrected source claim to carry its planning-artifact
correction or a dated correction note; silently fixing only the newest copies would leave two false historical
authorities.

### 4.5 Sprint tracking

No `sprint-status.yaml` value changes. Story 31.2 remains `ready-for-dev`; Story 31.1 and Epic 31 remain
`in-progress`. Append a `correct-course` Change Log row to Story 31.2 when the approved edits are applied,
recording zero implementation/test delta and the exact planning paths changed.

### 4.6 Historical Context Classification and Slice Proof

| Prior influence | Classification | Permitted use |
| :-------------- | :------------- | :------------ |
| Story 31.1 checkpoints C1/C2/C3/C4a/C5a/C6 | `current-narrow-pattern` | Current activation evidence only; no Story 31.1 task, scope, or whole-story shape is reused. |
| Story 31.1 checkpoints C4b/C5b/C7 | `historical-reference-only` | Explain why Story 31.1 remains `in-progress`; they are not transferred or weakened. |
| `DW 27.3-CR17` | `historical-reference-only` | Provenance for fail-closed Dapr behavior and the 2026-07-29 split only; never Story 31.2's active gate. |
| `DW 27.3-CR29` and `DW 27.3-CR30` | `current-narrow-pattern` | Current Story 31.2 completion gates, re-derived from the live register. |
| Pre-split Story 31.1 runtime-migration scope | `anti-template` | No whole-story structure is reused; the approved split into 31.1/31.2 remains authoritative. |

**Slice proof:** this correction has one independently demonstrable outcome: every governed Story 31.2 copy
uses the approved checkpoint activation gate and current `CR29`/`CR30` ownership. It creates, renames, splits,
or registers no story, and it adds no acceptance criterion.

## 5. Implementation Handoff

**Planning-correction scope:** Minor.  
**Route after approval:** Developer agent for direct artifact correction, followed by the existing Story 31.2
Developer flow.

### Responsibilities

- **Developer / Product Owner:** apply §§4.1–4.5 atomically, keep sprint statuses unchanged, run the proposed
  coherence guard and relevant existing story-artifact checks, and append the Story 31.2 ledger row.
- **Story 31.2 Developer:** proceed through the existing tasks only after the corrected Task 0 passes; execute
  the positive scoped read and denial proofs without storing or displaying a secret value.
- **Hexalith Platform Operations:** provide the live `jpiquot@local` execution context and verify component,
  pod, and Secret-name/type state. No secret contents are to be printed or persisted.
- **Security reviewer:** review the no-SDK/no-leak guards, mutation evidence, fail-closed behavior, and the
  per-Secret justification inventory.
- **Story 27.3 register owner:** review the evidence used to discharge or re-disposition `CR29` and `CR30`.

### Success criteria

1. Story 31.2's Blocking Activation Gate, Task 0, Epic AC Verification, and Epic 31 gate all name the exact
   checkpoint set `C1/C2/C3/C4a/C5a/C6`; none requires Story 31.1 `done`.
2. A focused executable guard fails if those governed copies drift or if the superseded `done` condition is
   reintroduced.
3. Epic 31's Story 31.2 Owned paths target `CR29`/`CR30` and do not grant ownership of
   `tools/verify-production-deployment.ps1`.
4. Both 2026-08-01 source proposals carry dated correction notes describing their partial application.
5. Sprint status values remain unchanged and Story 31.2 reaches Task 1 on its next `dev-story` run.
6. `CR29` and `CR30` remain open until an executed lane resolves a secretKeyRef through the vault-typed Dapr
   path without recording the value; component type or pod readiness alone is never cited as discharge.
7. No secret content is read into a planning artifact, log, telemetry output, CLI output, test snapshot, or
   command transcript.

## 6. Change Navigation Checklist

| Checklist area | Status | Finding |
| :------------- | :----- | :------ |
| 1. Trigger and context | [x] Done | Story 31.2's local gate and ownership copies contradict the approved canonical decisions. Concrete source and live-cluster evidence exists. |
| 2. Epic impact | [x] Done | Epic 31 remains viable; no new, removed, or reordered epic/story is needed. |
| 3. Artifact conflicts | [x] Done | Story 31.2, Epic 31 Owned paths, and two source proposals need correction. PRD, architecture, UX, code, manifests, and sprint values do not. |
| 4. Path forward | [x] Done | Direct adjustment selected; rollback and MVP review rejected. |
| 5. Proposal components | [x] Done | Issue, impact, exact edits, evidence, handoff, risks, and success criteria are defined. |
| 6. Final review and handoff | [x] Done | Administrator approved the proposal; the five authorized planning/story paths were updated and the runtime work was routed to the Story 31.2 Developer, Platform Operations, security reviewer, and Story 27.3 register owner. |

## 7. Epic AC Verification

Verified 2026-08-02 against HEAD `3f758f9a` and read-only cluster context `jpiquot@local`.

| Quoted claim | Class | Command / evidence | Observed | Verdict |
| :----------- | :---- | :----------------- | :------- | :------ |
| “Story 31.2 is `ready-for-dev`.” | Existence | `rg -n '^Status:' _bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md` and `rg -n '^  31-2-runtime-dapr' _bmad-output/implementation-artifacts/sprint-status.yaml` | Both sources report `ready-for-dev`. | `confirmed` |
| “Do not start implementation until Story 31.1 is `done`.” | Existence/location | Before/after: `sed -n '12,48p' _bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md` | Pre-change, the superseded condition appeared in the Blocking Activation Gate and Task 0. Post-change, both bind on `C1/C2/C3/C4a/C5a/C6`; `done` appears only in the explanation of what no longer blocks Story 31.2. | `corrected` by the approved change |
| “Story 31.2 must not enter implementation until Story 31.1 checkpoints C1, C2, C3, C4a, C5a and C6 all read `complete`.” | Existence/location | `rg -n '^\*\*Activation gate:' _bmad-output/planning-artifacts/epics.md` | Epic 31 carries the amended checkpoint gate. | `confirmed` |
| “Story 31.1 checkpoints C1, C2, C3, C4a, C5a and C6 all read `complete`.” | Behavioral | `rg -n '^\| C(1|2|3|4a|5a|6) ' _bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` | All six rows are present once and their completion-state cells read `complete` (qualified text retained where present). | `confirmed` |
| “Epic 31's Story 31.2 Owned paths still target the `DW 27.3-CR17` disposition and conditionally own `tools/verify-production-deployment.ps1`.” | Existence/location | `sed -n '5348,5356p' _bmad-output/planning-artifacts/epics.md` | Pre-change, the Story 31.2 paragraph contained both stale clauses. Post-change, it owns only `CR29`/`CR30` dispositions and names the verifier as Story 27.3-owned read/verify-only context. | `corrected` by the approved change |
| “`DW 27.3-CR29` and `DW 27.3-CR30` are open and fire before Story 31.2 is set `done`.” | Behavioral | `sed -n '2811,2830p' _bmad-output/implementation-artifacts/deferred-work.md` | Both entries are `open`; each names the Story 31.2 pre-`done` trigger. | `confirmed` |
| “The live Dapr components `secretstore` and `access-telemetry-secrets` use `secretstores.hashicorp.vault`.” | Behavioral | `kubectl -n hexalith-memories get components.dapr.io secretstore access-telemetry-secrets -o custom-columns='NAME:.metadata.name,TYPE:.spec.type,SCOPES:.scopes[*]'` | Both live components report `secretstores.hashicorp.vault`; scopes match their tracked roles. | `confirmed` |
| “The live Memories and MCP workloads have ready Dapr sidecars, and OpenBao is running.” | Behavioral | `kubectl -n openbao get pods -l app.kubernetes.io/name=openbao ...` and `kubectl -n hexalith-memories get pods ...` | Three OpenBao pods are Running; two Memories and two MCP pods are `true,true` / Running. | `confirmed` |
| “The production Dapr/OpenBao path has resolved the consumers' secretKeyRefs.” | Behavioral | Required Story 31.2 Task 4 bodyless scoped-read/denial transcript; intentionally not executed by this planning workflow | Component type and pod readiness do not prove the required secretKeyRef resolution. Owner: Story 31.2 Developer + Platform Operations. Consequence: `CR29`/`CR30` cannot close. Reopen trigger: Task 4 executes the read and denial proof while recording status/key name only. | `unverifiable` in this workflow; fail closed |
| “This correction creates or registers no new story.” | Existence | Compare the approved changed paths and `sprint-status.yaml` | No story creation, registration, renumbering, or status-value edit occurred. | `confirmed`; story-scope creation gate is not triggered |

## 8. Approval and Handoff Record

**Decision:** Approved by Administrator on 2026-08-02.

**Planning correction:** Implemented on 2026-08-02. The Story 31.2 gate and Task 0, Epic 31 Owned paths, and
both historical correction notes were updated. Sprint statuses were intentionally unchanged.

**Runtime handoff:** Story 31.2 Developer, Hexalith Platform Operations, security reviewer, and Story 27.3
register owner. The next `dev-story 31.2` run begins at corrected Task 0 and must keep `CR29`/`CR30` open
until the bodyless scoped-read and denial proof executes without secret disclosure.
