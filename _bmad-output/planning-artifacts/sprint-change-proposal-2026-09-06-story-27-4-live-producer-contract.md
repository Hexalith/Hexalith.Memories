# Sprint Change Proposal — Story 27.4 Live Producer Contract

**Date:** 2026-09-06  
**Mode:** Batch  
**Status:** Approved — planning handoff complete; Developer re-derivation pending  
**Trigger:** Story 27.4 review loop 6 halted after the workflow cap of 5; verified `bad_spec` findings make the current trusted-evidence contract unsatisfiable on a live qualification target  
**Change scope:** Moderate — amend Story 27.4's governing spec and two sequencing sentences in `epics.md` / `architecture.md`; then re-derive implementation. No new story registration. No Production enablement. A41 stays open.

The Administrator approved this proposal on 2026-09-06. Approval authorizes only the Direct Adjustment of Story 27.4's executable contract, the sequencing sentences in `epics.md` and `architecture.md`, the 15-minute runbook cap, and the subsequent Developer re-derivation from the amended spec. Approval does not register a story, change a sprint state, enable Production lifecycle writes, close A41, mutate a runtime, or authorize a commit or push. Unrelated dirty paths (including `spec-31-1-openbao-platform-hardening-and-documentation.md`) are not absorbed.

## Historical Context Classification

This correction amends the already-registered Story 27.4 tracking spec. It does **not** create, split, rename, or register a story.

| Prior influence | Classification | Permitted use in this correction |
| :-------------- | :------------- | :------------------------------- |
| Story 27.4 (current spec) | current work under amendment | Keep the approved umbrella with its existing C0–C6 checkpoint table. Amend the executable contract so live producers can satisfy the same checkpoints. |
| Story 27.3 | `historical-reference-only` | Predecessor C0 / adapter C2–C4 context only. Do not copy its transferred C1 umbrella or task list. |
| Story 27.21 | `historical-reference-only` | Proof that one C1 gate is registered and still `pending`. Do not copy its producer as 27.4's C2–C4 shape. |
| Stories 27.5 / 27.6 | `anti-template` | Withdrawn 11/14-gate split. Not reused, not re-registered, not a template for a 27.4 split. |
| Story 27.2 portable tests | `current-narrow-pattern` | Lower-layer evidence only; never a running-target substitute. |

**Slice proof:** no new story is authored. Story 27.4 remains the explicitly approved checkpoint tracking story for deployment-shaped C2–C6, runbooks, and A41 close-out machinery. Every checkpoint keeps its own owner, evidence command, review state, and completion state. A split of 27.4 is rejected here because it would recreate the umbrella/anti-template shape L09 and DW 27.3-CR16 exist to prevent, and because the Administrator asked to amend the spec rather than split.

## Epic AC Verification

Verified 2026-09-06 against worktree HEAD `115e2839d8902a3b20b866913a70fb8474b94f83`. The worktree is dirty with Story 27.4 implementation plus unrelated files; observations are read-only.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| “Story 27.4 … cannot start while any required successor file/registration is absent or any C1 gate is held, unproven, stale, or failed.” (`epics.md` Epic 27 qualification split) | Behavior / sequencing | `rg -n "cannot start while" _bmad-output/planning-artifacts/epics.md`; `rg -n "27-4-retention" _bmad-output/implementation-artifacts/sprint-status.yaml`; spec Intent “awaiting-operator” | Epic text forbids start. Sprint row is `in-progress`. Spec already treats missing cluster/C1 as an operator tail, not a blocked implementation. | `corrected` — distinguish repository machinery from close-out. Do **not** mark the predecessor gate satisfied. |
| “Actual Story 27.7 through Story 27.31 files must exist, pass their creation guards, be registered by approved changes, and be `done`.” | Existence | `ls _bmad-output/implementation-artifacts/spec-27-*.md 2>/dev/null; rg -n "27-21-runtime" _bmad-output/implementation-artifacts/sprint-status.yaml` | Only 27.21 is registered (`in-progress`). 27.7–27.20 and 27.22–27.31 files are absent. | `confirmed` as an **unsatisfied completion predecessor**. Keep it. Do not invent successor files in this correction. |
| “All twenty-five C1 child gates (C1.1-C1.25) are `passed` on their own required running-target evidence.” | Behavior | Canonical matrix `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`; Story 27.21 AC “`gateStatus: not-evaluated`” | C1 rows are `operator-pending` / not-evaluated. | `confirmed` gap versus the **desired close-out end state**. Do not weaken the AC into offline-only. |
| “Story 27.3 is `done` with C0 and C2-C4 complete against the immutable `PG-ONPREM-1` profile” | Existence | `rg -n "27-3-production-adapter" _bmad-output/implementation-artifacts/sprint-status.yaml` | Registry row is `done`. | `confirmed` as current sprint registry state. This proposal does not re-open 27.3. |
| First Story 27.4 AC: focused evidence proves acknowledgement, durable recovery, expiry/purge, newer-record preservation, audit continuity, and tenant/privacy denial on a production-shaped deployment | Intent / desired end-state | Spec Trusted Evidence Contract vs review loop 6 `bad_spec` table | Desired live proofs remain the goal. Current contract couples emit cadence to acknowledgement wait, uses local `dotnet` as C2 proof, scrapes a 30m Prometheus window, puts JWT on wget argv, and measures privacy on the wrong meter. | `confirmed` gap in **implementation versus desired end-state**. Amend the *how*, not the *what*. |
| Second Story 27.4 AC: operators have telemetry, deployment, capacity, monitoring, incident, recovery, adapter-reclamation, and decommission documentation | Existence | `test -f docs/operations/access-telemetry-lifecycle.md docs/operations/access-telemetry-adapter-production.md` | Files exist. | `confirmed` that the docs exist. |
| Runbook “open the exact-profile gate for at most 45 minutes” vs executable 15-minute cap | Quantitative | `rg -n "45 minutes" docs/operations/access-telemetry-lifecycle.md`; `rg -n "MaximumGateLifetime\|_QUALIFICATION_SESSION_SECONDS" src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationGate.cs tools/access_telemetry_producer_common.py` | Runbook line 81 says 45 minutes. Server `MaximumGateLifetime` and host `_QUALIFICATION_SESSION_SECONDS` are 15 minutes. | `corrected` — documentation must say 15 minutes with renewal. The 15-minute executable cap is the authority. |
| Third Story 27.4 AC: A41 closes only after C2–C6, terminal validation, and publish verification pass against the unchanged approved profile | Intent | DW-17 / spec Never / `deferred-work.md` | A41 remains open / carried-forward. | `confirmed` desired end-state. This correction does not close A41. |
| Architecture: “Story 27.4 … remains blocked until all twenty-five C1 gates pass” | Sequencing | `rg -n "remains blocked until all twenty-five" _bmad-output/planning-artifacts/architecture.md` | Same “blocked” wording as the epic start-gate. | `corrected` in lockstep with the epic sequencing sentence. Close-out stays blocked; repository producer work may proceed as `awaiting-operator`. |

PRD and UX inherited text for this slice contain no additional verifiable Story 27.4 claim.

**Escalation (not absorbed):** none of the desired live end-states are being dropped. The only planning-text correction is sequencing: repository-complete 27.4 work may be `in-progress` / `awaiting-operator` while C1 remains unproven. Completion, Production writes, and A41 mutation still require the predecessor gate.

## Change-analysis checklist

Executed 2026-09-06 in Batch mode.

### 1. Understand the Trigger and Context

- **1.1** [x] Triggering story: `27-4-retention-verification-operations-runbook-and-a41-close-out`. Goal remains repository-owned C2–C4 producers, fail-closed validators, runbooks, dashboard, and A41 close-out machinery, with Production writes disabled until live C0–C6 plus publish.
- **1.2** [x] Core problem: **technical limitation discovered during implementation**, caused by a **trusted-evidence contract that cannot be satisfied by a correct live implementation**. Several required proofs contradict each other or the reviewed least-privilege overlay (one-second emit bound includes up-to-five-minute acknowledgement; namespaced Role vs cluster-scoped `kubectl get namespace`; loop-5 stdin JWT still lands on wget argv; reporter TTL 600 s vs 9-day C3 resume; local `dotnet` as C2 pass).
- **1.3** [x] Evidence: spec Review loop 6 finding table (BH6-01..VG6-04) plus the re-runnable observations in §1.1 of this proposal.

### 2. Epic Impact Assessment

- **2.1** [x] Epic 27 can still complete. Story 27.4's *completion* contract is unchanged; its *executable how* and one sequencing sentence must change.
- **2.2** [x] Modify existing epic sequencing text only. No new epic. Do not defer Epic 27.
- **2.3** [x] Epics 28–31: no required change. C1 successors 27.7–27.31 (except registered 27.21) remain held and are still the close-out predecessor.
- **2.4** [x] No planned epic is obsolete. No new epic is required.
- **2.5** [x] Do not resequence epics. Do not move 27.4 ahead of unproven C1 for *close-out*. Do not roll 27.4 back to `backlog` solely because C1 is pending; the spec already names that operator tail.

### 3. Artifact Conflict and Impact Analysis

- **3.1** [x] PRD: no conflict with MVP goals (Kenji access telemetry as infrastructure telemetry). No PRD edit.
- **3.2** [x] Architecture: one sequencing sentence (see proposal 4.2). No ADR decision change. Qualification session duration is not a 45-minute ADR rule; active-purge grace of 15 minutes is a different concept and stays.
- **3.3** [N/A] UX specification has no access-telemetry / A41 surface.
- **3.4** [x] Secondary artifacts after approval: Story 27.4 spec trusted-evidence contract and tasks; `docs/operations/access-telemetry-lifecycle.md` 45→15; qualification overlay reporter Job; host producers and Server qualification/delivery code; focused tests. CI pipelines unchanged except as tests require.

### 4. Path Forward Evaluation

- **4.1** Direct Adjustment — **Viable**. Effort: Medium (spec + re-derive). Risk: Medium (live cluster still operator-owned). Timeline: Epic 27 stays in progress; A41 still downstream of live evidence.
- **4.2** Potential Rollback — **Not viable**. Rolling back 27.2/27.3 or deleting the host runner would discard KEEP architecture (host-side runner, one Qualification endpoint, unprivileged reporter, external journal, zero-by-default overlays).
- **4.3** PRD MVP Review — **Not viable**. Epics 0–8 and user-facing MVP are unaffected.
- **4.4** [x] Selected: **Option 1 — Direct Adjustment** of Story 27.4's non-frozen executable contract, plus the two sequencing sentences. Justification: the desired live proofs are still right; the current *measurement coupling* is wrong. Splitting 27.4 would violate the historical-slice guard. Weakening ACs to “offline tests suffice for A41” would change epic intent and is rejected.

### 5. Sprint Change Proposal Components

Covered by sections 1–5 of this document.

### 6. Final Review and Handoff

- **6.1** [x] Applicable checklist items completed.
- **6.2** [x] This draft is the proposal under review.
- **6.3** [x] Administrator approved 2026-09-06 (`approve`).
- **6.4** [N/A] No epic added/removed/renumbered. No new story row. `sprint-status.yaml` already has 27.4 `in-progress`; this correction does not mutate it (implementation agents still must not write it).
- **6.5** [x] Handoff: planning edits applied in this change. Developer re-derives 27.4 from the amended spec (`bmad-build` / review_loop 0). Success: loop-6 `bad_spec` groups are specified as implementable; offline tests still pass; Production disabled; A41 open.

---

## 1. Issue Summary

Review loop 6 of Story 27.4 exceeded the workflow cap of 5 and halted for human escalation. Independent reviewers verified that a faithful implementation of the current Trusted Evidence Contract **cannot pass on a live qualification target** without violating least-privilege RBAC, leaking the qualification JWT on process argv, treating workstation `dotnet` tests as C2 evidence, or failing a correctly paced 1 Hz emit because acknowledgement wait is inside the one-second proof.

The KEEP architecture is still right: host-side Python runner, one bodyless Qualification-only workload endpoint, Dapr app-token (not user JWT) on that route, unprivileged reporter Job, external journal, zero-by-default overlays, Production writes disabled, A41 open, read-only `sprint-status.yaml`, pending canonical matrix.

What must change is the **executable contract** so those KEEP pieces can produce the live proofs Epic 27 still requires.

### 1.1 Source-backed loop-6 anchors

| Finding group | Class | Command / evidence | Observed | Verdict |
| :------------ | :---- | :----------------- | :------- | :------ |
| C2 emit bound includes ack wait | Behavior | `sed -n '187,214p' src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationWorkloadRunner.cs` | `finishedUtcMs` is stamped after the 5-minute acknowledgement loop. | `confirmed` |
| Delivery worker terminals on `RecordIdConflict` | Behavior | `sed -n '94,109p' src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryWorker.cs` | `_terminal = true` on conflict, including valid C2 replay. | `confirmed` |
| Cluster-scoped namespace get | Existence | `rg -n 'get", \"namespace\"' tools/access_telemetry_producer_common.py` | `qualification-target-identity` runs `kubectl get namespace`. Overlay Role is namespaced. | `confirmed` |
| JWT on wget argv | Behavior | `sed -n '214,235p' tools/access_telemetry_producer_common.py` | `--header="Authorization: Bearer $bearer"` after stdin read. | `confirmed` |
| Business canary is `/api/v1/handlers` | Location | same lines; `MemoriesRoutes.Handlers` mapping in `TenantLifecycleEndpoints.cs` | Experimental handler registry, not tenant-scoped. `GET /api/v1/tenants/{tenantId}` exists (`MemoriesRoutes.Tenant`). | `confirmed` |
| Throughput uses `increase(...[30m])` | Location | `tools/access_telemetry_producer_common.py:494-501` | Query is a 30-minute Prometheus increase, not a C2-interval before/after. | `confirmed` |
| Reporter TTL 600 s | Existence | `rg -n ttlSecondsAfterFinished deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml` | `ttlSecondsAfterFinished: 600`. | `confirmed` |
| Prefix hash is tip metadata | Behavior | `tools/access_telemetry_producer_common.py:1456-1466` | Hash of `{context_sha256, last_entry_sha256, entry_count}`. | `confirmed` |
| Local `dotnet` as C2 proof | Behavior | `_fixed_test_commands` at line 347; `idempotence-conflict-proof` at 503-508 | Workstation `dotnet build` + `dotnet exec` of xUnit methods. | `confirmed` |
| Gate 15 vs runbook 45 | Quantitative | see Epic AC table | 15-minute executable cap; runbook 45. | `corrected` in docs |

---

## 2. Impact Analysis

### 2.1 Epic impact

- **Epic 27:** remains the owner. Completion still requires live C0–C6, post-evidence C5/C6, terminal validation, and publish before A41. Add a dated sequencing note that 27.4 **repository machinery** may be implemented as `awaiting-operator` while C1 is unproven; 27.4 **done** and A41 mutation may not.
- **Epic 20 / Story 20.5:** remain historical `done`. No reopen.
- **Epics 28–31:** no change.
- **C1 successors:** still required for close-out. This proposal does not register 27.7–27.31.

### 2.2 Story impact

- **Story 27.4:** amend Trusted Evidence Contract, reset the producer/Server execution checkboxes that the new contract invalidates, append a spec change-log entry, set `review_loop_iteration` to `0` after the amendment so implementation can re-derive, keep `operator_actions` non-empty.
- **Story 27.21 / held C1 gates:** unchanged.
- **Story 27.3:** unchanged.

### 2.3 Artifact conflicts

| Artifact | Current conflict | Required change after approval |
| :------- | :--------------- | :----------------------------- |
| Story 27.4 spec | Loop-6 `bad_spec` vs KEEP architecture | Direct Adjustment of the trusted-evidence bullets listed in §4.3 |
| `epics.md` | “cannot start” vs `in-progress` + awaiting-operator Intent | Dated sequencing correction only |
| `architecture.md` | “remains blocked until all twenty-five C1 gates pass” | Same sequencing correction |
| Runbook | 45-minute gate | 15 minutes with renewal |
| Overlay reporter Job | TTL 600 s / `backoffLimit: 0` | Contract in spec; implement after re-derivation |
| PRD | None | No change |
| UX | None | No change |
| ADR 27.1-001 | No 45-minute qualification-session rule | No change |
| `sprint-status.yaml` | 27.4 already `in-progress` | No change in this correction |

### 2.4 Technical impact

After approval, implementation must re-derive (not patch around) the host producers, qualification workload runner, delivery-worker conflict handling, overlay Job, and tests. Production overlay stays zero-replica/disabled. Qualification overlay stays zero by default.

---

## 3. Recommended Approach

**Option 1 — Direct Adjustment** inside Story 27.4.

Keep every live end-state AC. Change only the measurement and authority contracts that made those ACs unimplementable.

### 3.1 Alternatives rejected

| Option | Verdict | Reason |
| :----- | :------ | :----- |
| Split 27.4 into C2/C3/C4/A41 stories | Rejected | Recreates the 27.5/27.6 anti-template; Administrator asked to amend the spec. |
| Weaken ACs so offline tests close A41 | Rejected | Changes epic intent. Policy: do not weaken a desired end-state to match today's code. |
| Roll back 27.2/27.3 or delete the host runner | Rejected | Throws away KEEP architecture. |
| Add cluster-admin / Job `create` / user JWT on the Qualification route | Rejected | Violates accepted least-privilege and Dapr-token design. |
| Enable Production writes in this correction | Rejected | Explicit Never. |

### 3.2 Effort, risk, timeline

- **Planning effort:** Low–Medium (this file plus spec/epic/architecture hunks).
- **Implementation effort:** Medium (re-derive producers/Server/overlay tests; live cluster still operator-owned).
- **Risk:** Medium if re-derivation is skipped in favor of local-test green; Low if the new contract is the review baseline.
- **Timeline:** no MVP impact. A41 remains open until live C0–C6 and publish.

---

## 4. Detailed Change Proposals

### 4.1 `epics.md` — Epic 27 sequencing (not the three ACs)

**Section:** Epic 27 “Qualification and close-out split” paragraph (`epics.md` ~4944) and Story 27.4 Predecessor Gate lead-in.

**OLD:**

> Story 27.4 owns deployment-shaped lifecycle evidence, operations documentation, and A41 close-out, but cannot start while any required successor file/registration is absent or any C1 gate is held, unproven, stale, or failed.

**NEW:**

> Story 27.4 owns deployment-shaped lifecycle evidence, operations documentation, and A41 close-out. **Corrected 2026-09-06 by approved Sprint Change Proposal `sprint-change-proposal-2026-09-06-story-27-4-live-producer-contract.md`:** repository-owned producers, validators, runbooks, dashboard, and close-out *guards* may be implemented and reviewed as `awaiting-operator` while C1 successor files are still absent or unproven. That repository work does not pass any C1 gate, enable Production lifecycle writes, mark Story 27.4 `done`, or close A41. Story 27.4 completion and A41 mutation still require the Predecessor Gate below: Story 27.3 `done` on C0/C2–C4, actual Story 27.7–27.31 files registered and `done`, all twenty-five C1 gates `passed` on running-target evidence, and the same immutable profile hash.

**Rationale:** matches current sprint state and spec Intent without weakening the close-out predecessor.

Do **not** edit the three Story 27.4 Given/When/Then acceptance criteria except that they continue to describe the live end-state. The Trusted Evidence Contract in the spec is where the *how* is amended.

### 4.2 `architecture.md` — access-telemetry ownership sentence

**Section:** Access telemetry lifecycle bullet (~line 227).

**OLD:**

> Story 27.4 owns deployment-shaped lifecycle verification, the operations runbook, and A41 close-out, and it remains blocked until all twenty-five C1 gates pass under those later-compliant registrations.

**NEW:**

> Story 27.4 owns deployment-shaped lifecycle verification, the operations runbook, and A41 close-out. Repository machinery may proceed as `awaiting-operator`; Story 27.4 completion and A41 close-out remain blocked until all twenty-five C1 gates pass under those later-compliant registrations. **Sequencing corrected 2026-09-06 by approved Sprint Change Proposal `sprint-change-proposal-2026-09-06-story-27-4-live-producer-contract.md`.**

**Rationale:** same sequencing truth as 4.1. No ADR or topology change.

### 4.3 Story 27.4 spec — Trusted Evidence Contract

File: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out.md`.

KEEP unchanged: host-side runner; one bodyless Qualification endpoint; Dapr application token (not user JWT) on that route; unprivileged reporter (no SA token, no Kubernetes RBAC on the Job); external journal; zero-by-default overlays; Production disabled; A41 open; read-only sprint-status; pending canonical matrix; C2 30-minute two-writer scenario; C3 1h/24h/168h cohorts; C4 closed 21-lane inventory; C5/C6 post-evidence approvals; 15-minute C1 freshness at authorization only.

Apply the following old→new replacements.

#### 4.3.1 C2 one-second proof vs acknowledgement

**Section:** “C2 execution topology and proof” and “Correlated workload and exact accounting”.

**OLD (coupling):**

> The host drives individually bounded one-second segments … The rate is computed from successful interval timestamps with a narrow tolerance around 1,800 seconds  
> Segment responses bind … `finishedUtcMs`  
> (implementation stamps `finishedUtcMs` after the acknowledgement wait; host aborts when dispatch lag exceeds 250 ms including HTTP round-trip)

**NEW:**

- Each one-second **emit** interval binds `startedUtcMs` and `emitFinishedUtcMs` taken **before** the acknowledgement wait. `950 <= emitFinishedUtcMs - startedUtcMs <= 1250` is the only one-second bound.
- Acknowledgement is a separate observation: `acknowledgedUtcMs`, per-disposition counts, and the 5-minute ack budget. Exact acknowledgement remains mandatory; it must not be folded into the emit bound.
- Host cadence is 1 Hz **dispatch of emit requests** against wall-clock segment slots. Dispatch lag is `actual_emit_start - scheduled_emit_start` and must stay within 250 ms. In-flight acknowledgement waits may overlap subsequent emit requests up to a documented bounded concurrency (the current four in-flight HTTP calls are acceptable only if they wait on emit completion, not on ack).
- Replacement, overlap, and 1,800-second rate proofs use emit timestamps. Survival of acknowledged records uses the acknowledgement observation and store/actor reads, not the emit clock.

**Rationale:** BH6-01, BH6-02, EC6-02. A correct 1 s emit plus slower Dapr persist must still be able to pass.

#### 4.3.2 Sticky Lazy and conflict-terminal delivery

**Section:** “C2 execution topology and proof” (idempotent replay).

**OLD:**

> Retry across a lost response or replaced pod is durable and idempotent: deterministic qualification record identities … make a replay return the original receipt or attributable conflicts without emitting a second logical workload.

**NEW (add, do not delete the identity rule):**

- A faulted or canceled segment `Lazy<Task>` MUST be evicted. The same segment identity may be retried after the qualification gate revalidates. A sticky cached fault must not pin the process for the replica lifetime.
- `RecordIdConflict` on a deterministic qualification identity is an **attributable conflict** for C2 replay accounting. It MUST NOT set the Server delivery worker `_terminal`. `ConfigurationInvalid` remains process-terminal for that worker. After Server replacement, later unique segments on the new or recovered worker must still deliver.

**Rationale:** BH6-03, BH6-04, EC6-01.

#### 4.3.3 Namespace identity without cluster-scoped get

**Section:** “Qualification filesystem, identity, and recovery” / “Exact reporter and operator authority”.

**OLD:** identity packet implied `kubectl get namespace` (cluster-scoped).

**NEW:**

- Do **not** require `kubectl get namespace`. Do **not** add a ClusterRole for namespaces.
- Bind namespace identity from the allowlisted scenario-input namespace plus a **namespaced** observation the overlay Role already grants (named workload get/list, or `auth can-i` for the reviewed verbs in that namespace). A subject limited to the overlay Role must be able to complete the identity packet.

**Rationale:** BH6-05.

#### 4.3.4 JWT never on argv; re-check expiry

**Section:** “C4 executable lanes” (loop-5 bearer paragraph).

**OLD:**

> streamed only over stdin to the fixed in-pod request command

**NEW (replace the in-pod delivery sentence):**

- The operator-provisioned bearer is still read only from `HEXALITH_STORY_27_4_BUSINESS_BEARER_FILE` on the host and streamed over `kubectl exec -i` stdin. It is never copied into argv, environment, logs, journals, or packets.
- Inside the pod, stdin is written to a `0600` temporary header file. The fixed `wget` invocation uses `--header-file` (or equivalent file-based header input). `$bearer` MUST NOT appear on wget argv. The file is unlinked in `trap`.
- Before each business/privacy probe, the producer validates JWT `exp` (and `nbf` if present) without logging the token. Remaining lifetime MUST exceed that probe's timeout. A short, missing, over-permissive, or leaked credential fails closed before the gate opens or, if discovered mid-C4, before the next probe, and restores disable/zero.

**Rationale:** BH6-06, EC6-07, EC6-16.

#### 4.3.5 Tenant-scoped business canary and Dapr-backed privacy

**Section:** “C4 executable lanes” and “Same-record sink and privacy evidence”.

**OLD:** “fixed authenticated business operation” (implementation used `GET /api/v1/handlers`); privacy scraped `dapr_http_client_completed_count` around localhost Kestrel GETs to `/api/v1/tenants/{id}`.

**NEW:**

- Business continuity probe: `GET /api/v1/tenants/{allowedQualificationTenant}` (`MemoriesRoutes.Tenant`). Forbidden: `/api/v1/handlers` and any experimental/global registry.
- Privacy denial probe: `GET /api/v1/tenants/{id}/configuration` (`MemoriesRoutes.TenantConfiguration`) for the allowed qualification tenant and the closed mismatched tenant. That handler performs a Dapr-backed embedding-config read (see `GetTenantConfigurationAsync` `DaprException` path). Before/after dependency counters MUST be scraped from the **same pod's Dapr sidecar** around that denied call. A denied request MUST NOT increase the selected Dapr client/invocation counter. Localhost-only Kestrel hits that never invoke Dapr cannot satisfy privacy-before-dependency.
- Passing evidence remains bounded HTTP outcomes, correlations, same-interval audit records, and isolated target-derived counters. No token or tenant content.

**Rationale:** BH6-07, BH6-08. Verified: `MemoriesRoutes.Tenant` and `TenantConfiguration` exist; `Handlers` is the experimental HXL002 registry.

#### 4.3.6 C2 throughput is interval-bound, not `increase[30m]`

**Section:** “C2 execution topology and proof”.

**OLD:** “component/purge throughput comes from an isolated, operation-specific before/after counter bound to that interval.”

**NEW (make the isolation executable):**

- Capture the named counter `memories_access_telemetry_lifecycle_state_operations_total` from the **lifecycle (or Server) `/metrics` endpoint on the qualification target** immediately before the first emit and immediately after the last emit of the 1,800-second window (or from an instant PromQL `query` at those two UTC times, never `increase[30m]`).
- `operation_delta` is `after - before` for that exact interval. Cluster Prometheus `increase(...[30m])` is forbidden as C2 proof.
- Workstation tests may assert the parser; they cannot pass the checkpoint.

**Rationale:** BH6-09.

#### 4.3.7 Reporter Job lifetime, retries, and prefix hash

**Section:** “C3 schedule, identity, and recovery” / “Fixed observations”.

**OLD:** short-lived Job; loop-5 resume of completed Job logs; `authenticated_prefix_sha256` as complete-prefix hash.

**NEW:**

- The reviewed reporter Job MUST NOT set `ttlSecondsAfterFinished` to a value shorter than the remaining C3 wrapper bound (omit TTL, or set it ≥ remaining C3 timeout). Kubernetes MUST NOT delete a Completed Job while C3 resume still needs its logs/receipt.
- `backoffLimit` MUST be ≥ 2 so a transient reporter failure retries the **same** Job object. Exhausted retries write a blocker packet. A failed Job MUST NOT be treated as the loop-5 identical success receipt.
- `authenticated_prefix_sha256` is SHA-256 of the authenticated complete journal/transcript **prefix bytes** (context header plus every completed JSONL entry in order), not a hash of `{context_sha256, last_entry_sha256, entry_count}`.
- Resume after a seed that completed in-cluster but not in the journal **reuses the journaled or Job-observed record IDs**. It MUST NOT emit a second 125-id set for that horizon.

**Rationale:** BH6-14, BH6-15, EC6-11, EC6-12, EC6-19.

#### 4.3.8 Target-side mechanism proof; local `dotnet` is supporting only

**Section:** “Bounded command capture” and C2/C4 scenario bullets.

**OLD:** producer executes every child command used to decide a checkpoint.

**NEW (explicit exclusion):**

- `idempotence-conflict-proof` and every C4 mechanism proof MUST be a target-side persist/conflict/fault observation (Dapr/state/actor or in-pod command against the running qualification workload).
- Host `dotnet build` / `dotnet exec` of xUnit methods is **supporting evidence only** and MUST NOT set a checkpoint field to passed.

**Rationale:** BH6-16.

#### 4.3.9 C4 restore-then-measure, mixed dispositions, renew, exec-time timestamps

**Section:** “C4 executable lanes” and “C4 isolation and continuity”.

**NEW bullets (replace the contradictory implementation-shaped readings):**

- Zero-loss outage lanes: inject fault → restore → reselect ready pods → then business and fixed-workload probes. Workload/business **before** `__FAULT_RESTORE__` cannot prove post-restore persist.
- Intentional rejection/drop lanes may probe during the fault when the scenario declares that mix.
- A lane is `exercised` when `persisted + rejected + dropped == attempted` **and** the mix matches that lane's declared expectation. Mixed queue-exhaustion is a valid declared mix, not an automatic fail.
- Every C3 wait that can exceed remaining Lease/gate lifetime, and **every** C4 lane including `continuity` / `observability` / `privacy-denial`, MUST `qualification-renew` before remaining lifetime drops below the next command bound.
- `_fixed_workload_shell` (and any `emitted-utc-ms`) is bound at **exec** time, not when the argv tuple is first constructed. A C4 wait must not send a timestamp outside the Server 15-minute qualification window.

**Rationale:** EC6-08, EC6-09, EC6-06, EC6-14, EC6-15.

#### 4.3.10 AcceptedAtUtc and stored RecordId

**Section:** “C3 logical and physical proof”.

**NEW:**

- Qualification `AcceptedAtUtc` is the **store persist time**, not a copy of the emit timestamp. C3 `emitted <= accepted <= expires <= purged` MUST be able to fail on delayed accept.
- SQL and Dapr reads address record IDs **read back from the store** (or from the authenticated runner receipt **after** the store accepted the enqueue). Runner-invented IDs that were never queued cannot pass.

**Rationale:** BH6-18, VG6-02.

#### 4.3.11 Gate clock skew

**Section:** “Qualification filesystem, identity, and recovery”.

**NEW:**

- Server `TryValidate` accepts `expiresUtcMs <= now + MaximumGateLifetime + 5s` (same 5-second slack the host already applies when asserting the projected gate). A host clock slightly ahead of the pod MUST NOT reject a just-written 15-minute gate.

**Rationale:** EC6-03.

#### 4.3.12 Test-only doubles and runbook duration (implementation-binding)

Carry the previously moot patches into the contract so they cannot regress:

- `HEXALITH_STORY_27_4_INLINE_KUBECTL` is ignored unless a second test-only variable set only by repository unittests is present; the live CLI rejects that combination when a real kubeconfig / live `--evidence-root` is in use.
- `MeterListener` tests listen to `RecordStateOperations` on the PostgreSQL adapter path used for C2 throughput.
- C# and Python Crockford qualification IDs share one golden vector.
- Cancelled `WaitAsync` on the Qualification route maps to a bounded non-500 outcome.
- Runbook step 6: gate open “at most **15 minutes**” per document, renewed by the producer; never 45.

**Rationale:** BH6-10, EC6-05, EC6-18, VG6-01, VG6-03, VG6-04.

### 4.4 Spec tasks, status, and review loop

**Execution checkboxes:** reopen (set `[ ]`) the producer, Server/lifecycle, overlay, and tooling-test tasks that the amended contract invalidates. Leave checked: runbook/appendix existence, dashboard panels that already bind real series (after the unlabeled `state_operations` add), canonical pending matrix, and the A41-open / no-sprint-status-write obligations.

**Acceptance Criteria (spec):** keep the five Given/When/Then rows. Add one sentence to the live-producer criterion: emit cadence and acknowledgement are separately bound; C2–C4 command vectors follow §4.3.

**Frontmatter:** after the amendment is applied, set `status: in-progress`, `review_loop_iteration: 0`, `followup_review_recommended: false`. Do not keep iteration 6 as the baseline for the next `bmad-build` review.

**Spec Change Log:** append a 2026-09-06 Direct Adjustment entry citing this proposal and listing the loop-6 groups closed by specification (not yet by implementation).

**PRD:** no change.  
**UX:** no change.  
**ADR 27.1-001:** no change.

### 4.5 Runbook (same change set, documentation)

**File:** `docs/operations/access-telemetry-lifecycle.md` step 6.

**OLD:** `open the exact-profile gate for at most 45 minutes`

**NEW:** `open the exact-profile gate for at most 15 minutes, and renew the Lease and gate before expiry for the remaining C2/C3/C4 bound`

Structure-aware tests that pin the 45-minute string must be updated in the same implementation change.

---

## 5. Implementation Handoff

**Scope classification:** Moderate (planning sentences + spec contract, then Developer re-derivation). Not Major: MVP and ADR topology unchanged. Not Minor: the trusted-evidence contract is the review baseline.

| Role | Responsibility after approval |
| :--- | :---------------------------- |
| Product Owner / this route | Apply §4.1–4.2 to `epics.md` and `architecture.md`; apply §4.3–4.4 to the Story 27.4 spec; do not write `sprint-status.yaml`. |
| Developer (`bmad-build` 27.4) | Re-derive producers, Server qualification/delivery, overlay Job, tests, and runbook 15-minute wording from the amended spec. Reset review from iteration 0. |
| Platform Operations / Security | Unchanged operator tail: live C0–C6, C5/C6, A41 publish. |
| Implementation agents | Still must not mutate `sprint-status.yaml`, enable Production writes, or close A41. |

**Success criteria:**

1. Spec Trusted Evidence Contract no longer requires the loop-6 contradictions.
2. Epic/architecture sequencing matches `awaiting-operator` vs close-out.
3. Re-derived implementation has focused tests for emit-vs-ack timestamps, Lazy eviction, non-terminal qualification conflicts, `--header-file` JWT, tenant-scoped canaries, interval-bound counters, complete-prefix hash, target-side conflict proof, C4 restore-then-measure, renew on C3/C4, exec-time `emitted-utc-ms`, store `AcceptedAtUtc`, and 5-second gate slack.
4. Offline Python and focused .NET lanes pass. Live packets remain `operator-pending`.
5. Production overlay remains zero/disabled. A41 remains open.

**Handoff recorded 2026-09-06:** planning edits applied (epics, architecture, spec contract, runbook 15-minute cap, operations-contract pin). Next Developer action is `/bmad-build` on Story 27.4 against the amended spec (`review_loop_iteration: 0`). Do not write `sprint-status.yaml`. Production writes stay disabled. A41 stays open.
