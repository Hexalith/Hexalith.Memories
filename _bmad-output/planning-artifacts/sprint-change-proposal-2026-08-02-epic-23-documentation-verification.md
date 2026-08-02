# Sprint Change Proposal: Epic 23 Documentation Verification Pass

**Date:** 2026-08-02  
**Project:** Hexalith.Memories  
**Change trigger:** Add an Epic 23 documentation verification pass for rate limiting, failure recovery, directory ingestion, index readiness, and workflow determinism  
**Mode:** Batch  
**Status:** Approved — implementation pending  
**Recommended scope:** Minor / direct documentation adjustment
**Approved by:** Administrator on 2026-08-02

> The normal dated output path is already occupied by the approved Epic 23 invariant-review proposal. This descriptive suffix preserves that artifact instead of overwriting it.

## 1. Issue Summary

Epic 23 is complete: the epic, Stories 23.1-23.9, and its retrospective are all `done`. The retrospective nevertheless left Action Item 4 open:

> Add an Epic 23 documentation verification pass for rate limiting, failure recovery, directory ingestion, index readiness, and workflow determinism.

The current documentation coverage is uneven:

- `docs/operations/rate-limiting.md` already describes provider 429 durable waits, one-operation embedding admission, and tenant embedding-config cache freshness.
- `docs/operations/failure-recovery.md` already describes retained non-URL payload references, pre-claim rejection, URL refetch, and mixed bulk outcomes.
- Directory ingestion appears only as a route-table entry and scattered references; its allowlist, scheduling bound, checkpoint cadence, deterministic accounting, and unscheduled-payload cleanup are not documented as an operational contract.
- Index-rebuild and incident docs warn against ingestion-owned `FT.CREATE`, but the memoized readiness contract and provisioning ownership are not documented together.
- No contributor-facing document states that ingestion retry and natural-language configuration is captured at scheduling time and must remain replay-safe.

This is a documentation-governance gap, not a newly discovered product defect. A focused current-tree lane covering all five implementation areas passed 127 of 127 tests at `main@feac22bbc78c290f7ed8b1c2d5e1bfedf4dab133`.

## 2. Impact Analysis

### Epic and story impact

- **Epic 23:** Remains `done`. The change closes one retrospective documentation action; it does not reopen implementation scope or acceptance criteria.
- **Stories 23.3-23.8:** `historical-reference-only`. They supply implemented behavior and evidence anchors, not templates for a new story.
- **New, renamed, split, or removed stories:** None.
- **Dependencies and execution order:** No change. `story_execution_order.epic-23` remains intact.
- **Remaining epics:** No scope, priority, sequence, or dependency change.

### Planning and product artifacts

| Artifact | Impact | Decision |
|---|---|---|
| PRD | FR12, FR69, NFR17, NFR22, NFR24, NFR25, and NFR26 already express the relevant outcomes. | No edit. |
| Architecture | Already assigns orchestration determinism, activity-owned I/O, provider-rate handling, and tenant provisioning/index ownership. | No edit. |
| UX | No interface, flow, accessibility, or component behavior changes. | No edit. |
| `epics.md` | Epic 23 scope and completed story definitions remain valid. | No edit and no reopening. |

### Documentation and tracking artifacts

| Artifact | Current state | Required action |
|---|---|---|
| `docs/operations/rate-limiting.md` | Substantial Epic 23 coverage exists. | Verify every governed claim against current source/tests; correct drift, distinguish inbound request quotas from embedding-provider admission, and cross-link related recovery/directory guidance. |
| `docs/operations/failure-recovery.md` | Substantial Story 23.4 coverage exists. | Verify retained-payload, pre-claim rejection, URL, restore, retention, and bulk-outcome claims; correct drift and cross-link related ingestion guidance. |
| `docs/operations/directory-ingestion.md` | Does not exist. | Create the authoritative operator contract for configuration, security, allowlist filtering, bounded scheduling, checkpointing, deterministic final accounting, status, cancellation, and payload cleanup. |
| `docs/operations/index-rebuild.md` | Contains manual-recovery warnings but not the complete ingestion-readiness contract. | Add a focused section that separates provisioning, ingestion-time readiness verification, additive schema upgrade, missing/incompatible-index failure, and operator rebuild/reprovision actions. |
| `docs/dev/ingestion-workflow-determinism.md` | Does not exist. | Create the contributor contract for scheduling-time configuration capture, scheduler entry points, child workflow handling, activity/orchestrator boundaries, and replay guards. |
| `_bmad-output/implementation-artifacts/epic-23-documentation-verification-2026-08-02.md` | Does not exist. | Create the dated five-checkpoint verification record defined below. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Epic 23 Action Item 4 is `open`; Epic 23 is `done`. | Change only the action item to `done` after all five checkpoints are `confirmed` or `corrected` and reverified. Keep the epic and story rows `done`. |

### Technical and operational impact

- No production code, test code, API, schema, infrastructure, deployment, package, submodule, or runtime configuration change is authorized by this proposal.
- Documentation corrections that reveal a product-scope or behavior defect must stop the pass, record `unverifiable`, leave the action open, and return through Correct Course for a separate scope decision.
- No tenant routing, case routing, storage selector, or tenant-authorization behavior changes. Tenant negative-evidence expansion is therefore not applicable.

### Change Navigation Checklist

| Section | Result |
|---|---|
| 1. Trigger and context | Done — exact retrospective action and open sprint-status row confirmed. |
| 2. Epic impact | Done — completed Epic 23 remains closed; no story or dependency change. |
| 3. Artifact impact | Done — five documentation surfaces, one evidence artifact, and one action-status row identified. |
| 4. Path forward | Done — direct adjustment is viable; rollback and MVP review are not justified. |
| 5. Proposal components | Done — old→new edits, checkpoint ownership, evidence rules, and handoff are defined below. |
| 6. Final review and handoff | Done — Administrator approved the proposal; the Minor change is routed to the Developer agent. Implementation and fail-closed verification remain pending. |

## 3. Recommended Approach

Select **Option 1: Direct Adjustment**.

- **Effort:** Low to moderate — verify two existing pages, create two focused pages, add one readiness section, create one evidence record, and close one action item.
- **Risk:** Low — documentation-only, with source/test evidence and fail-closed verdicts.
- **Schedule impact:** No implementation sequencing impact.
- **Rationale:** This closes the exact retrospective obligation without inventing a tenth Epic 23 story, rewriting completed history, or altering product scope.

**Option 2: Potential Rollback** is not viable. No implemented behavior has been shown to require reversal, and rollback would not repair missing operator/contributor guidance.

**Option 3: PRD MVP Review** is not viable. The PRD and architecture already support the five implemented behaviors; reducing or redefining product scope is unrelated to the documentation gap.

## 4. Detailed Change Proposals

### 4.1 Add a fail-closed verification record

**Artifact:** `_bmad-output/implementation-artifacts/epic-23-documentation-verification-2026-08-02.md`

**OLD:**

No dated artifact records a current-tree verdict for all five documentation areas.

**NEW:**

Create a verification record containing the baseline branch/commit, changed-doc inventory, reviewer/date, exact commands, observed output, and this checkpoint table:

| Checkpoint | Quoted implementation claim to verify | Documentation owner | Evidence owner | Review status | Completion status |
|---|---|---|---|---|---|
| Rate limiting | “Embedding admission uses one actor call per single provider call or bounded provider batch; provider 429 feedback remains activity-owned and workflow recovery uses a bounded durable timer.” | Paige | Amelia | Pending | Pending |
| Failure recovery | “Failed non-URL re-ingestion uses a retained tenant-scoped source payload reference or returns `NON_URL_REINGESTION_UNAVAILABLE` before claiming the failed record; URL re-ingestion still refetches.” | Paige | Amelia | Pending | Pending |
| Directory ingestion | “Directory ingestion applies the supported-extension allowlist before reading bytes, uses bounded scheduling and checkpointing, cleans unscheduled payloads, and produces deterministic final accounting.” | Paige | Amelia | Pending | Pending |
| Index readiness | “Ingestion memoizes tenant/index/schema readiness, fails clearly for missing or incompatible indexes, and does not create indexes on demand; tenant provisioning remains the creation owner.” | Paige | Amelia | Pending | Pending |
| Workflow determinism | “Retry and natural-language workflow configuration is captured at scheduling time; `IngestionWorkflow` does not read mutable host snapshots during orchestration.” | Paige | Amelia | Pending | Pending |

For every row, quote each claim from the final documentation, provide a re-runnable command, record observed output, and assign exactly one verdict:

- `confirmed` — documentation already matches verified behavior;
- `corrected` — documentation was changed and the corrected claim was reverified;
- `unverifiable` — proof is missing, contradictory, blocked, or implies scope change.

The pass succeeds only when every row is `confirmed` or `corrected`, the completion status is `complete`, and no affected claim remains `unverifiable`.

**Rationale:** The five topics are independently verifiable, so the approved checkpoint table supplies explicit ownership, evidence, review, and completion without creating a multi-outcome story.

### 4.2 Verify and correct rate-limiting documentation

**Artifact:** `docs/operations/rate-limiting.md`

**OLD:**

The page already covers inbound quotas, per-tenant embedding admission, the 30-second default embedding-config cache, shared-provider limits, provider Retry-After handling, extraction concurrency, and jitter. It has not been closed through a dated Epic 23 verification pass.

**NEW:**

Verify and, where needed, correct the page so it explicitly preserves these distinctions:

1. ASP.NET inbound request limiting is separate from embedding-provider admission.
2. `GenerateEmbeddingActivity` admits once for its provider call; `GenerateChunkEmbeddingsActivity` admits once per bounded provider batch.
3. Provider 429 feedback occurs only while a provider call is in progress; local admission denial does not report a provider 429.
4. Workflow retry uses a bounded Dapr durable timer and sanitized effective Retry-After; ordinary failures retain their normal retry/failure path.
5. Per-tenant admission does not isolate tenants sharing the same provider key.
6. Embedding-config cache freshness, ceiling changes, and secret-handling statements match current options and implementation.

Add links to failure recovery and directory ingestion where those behaviors interact.

### 4.3 Verify and correct failure-recovery documentation

**Artifact:** `docs/operations/failure-recovery.md`

**OLD:**

The page already describes Story 23.4 behavior inside a document still framed primarily as Story 6.3. It has not been closed through a dated Epic 23 verification pass.

**NEW:**

Verify and, where needed, correct the page so it states:

1. Which failure paths persist a reusable source payload reference and which payloads remain transient.
2. The configured retention bound and the operator consequence after expiry.
3. Non-URL validation happens before claim and returns `NON_URL_REINGESTION_UNAVAILABLE` without deleting the failed-unit record or dedup key.
4. URL failures refetch; supported non-URL failures schedule with `ContentBytes = null` and `PayloadReference` populated.
5. Scheduling failure restores all claimed fields, including the optional source reference.
6. Bulk mixed outcomes remain per-unit and do not hide unsupported records as generic scheduling errors.

Refresh the page framing so the Epic 23 correction is visible without erasing its Story 6.3 origins, and link to directory ingestion and rate limiting.

### 4.4 Add directory-ingestion operational guidance

**Artifact:** `docs/operations/directory-ingestion.md`

**OLD:**

No dedicated document exists. `docs/operations/route-surface.md` only identifies the directory-ingest and batch-status routes.

**NEW:**

Create an operator document covering:

- endpoint and authorization context;
- disabled-by-default `AllowedDirectoryRoots` behavior and path/reparse-point safety;
- `SupportedExtensions` allowlist plus stricter `UnsupportedExtensions` overlay;
- default and clamped scheduling parallelism (`4`, range `1..32`);
- default and clamped checkpoint size (`50`, range `1..250`);
- initial, bounded-progress, and final batch-state persistence;
- deterministic final source-URI ordering despite out-of-order scheduling completion;
- skip reasons, skipped-report truncation, batch limits, status lookup, and counts;
- cancellation and partial-failure behavior, including deletion of claim-checked payloads for unscheduled files;
- links to rate limiting, failure recovery, and route-surface documentation.

All numeric and behavioral claims must be copied only after current source/test verification.

### 4.5 Consolidate index-readiness and provisioning ownership

**Artifact:** `docs/operations/index-rebuild.md`

**OLD:**

The runbook warns operators not to issue ingestion-owned `FT.CREATE`, but it does not explain the normal ingestion-time readiness boundary as a complete contract.

**NEW:**

Add `## Ingestion-time readiness vs. provisioning ownership` covering:

1. `TenantProvisioningWorkflow` and its provisioning activities create the required indexes.
2. Ingestion activities call `ITenantIndexReadinessVerifier` before writes and never repair a missing index by creating it on demand.
3. Successful readiness is process-local and memoized by tenant, index family, and schema-sensitive dimensions.
4. Missing or incompatible indexes fail closed before the hash/vector write.
5. Only the approved additive syntactic TAG-field upgrade may occur before readiness is cached; incompatible drift still fails.
6. The operator decision path distinguishes transient readiness verification, tenant reprovisioning, and the destructive rebuild procedures already documented by the runbook.

Cross-link tenant onboarding/offboarding where active/provisioned tenant state is relevant.

### 4.6 Add workflow-determinism contributor guidance

**Artifact:** `docs/dev/ingestion-workflow-determinism.md`

**OLD:**

No contributor-facing document defines the Story 23.8 scheduling and replay contract.

**NEW:**

Create a developer document covering:

- captured `IngestionWorkflowConfiguration` on durable `IngestionInput`;
- `IIngestionWorkflowScheduler` as the normal scheduling seam;
- capture-before-claim-check/scheduling behavior and preservation through payload slimming;
- direct and child-workflow entry-point obligations;
- the rule that orchestration cannot read mutable options/statics, perform I/O, use wall-clock/random values, or move provider/actor/state behavior out of activities;
- legacy/default contract behavior and source-generated JSON requirements;
- the determinism and scheduling source guards contributors must run;
- a short review checklist for every new ingestion entry point.

### 4.7 Close only the retrospective action item

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**OLD:**

```yaml
- epic: 23
  action: "Add an Epic 23 documentation verification pass for rate limiting, failure recovery, directory ingestion, index readiness, and workflow determinism"
  owner: "Paige, Amelia"
  status: open
```

**NEW, only after all five verification rows pass:**

```yaml
- epic: 23
  action: "Add an Epic 23 documentation verification pass for rate limiting, failure recovery, directory ingestion, index readiness, and workflow determinism"
  owner: "Paige, Amelia"
  status: done  # 2026-08-02: all five current-tree documentation checkpoints are confirmed/corrected in epic-23-documentation-verification-2026-08-02.md; focused lane passed 127/127.
```

Do not change `epic-23`, any `23-*` story row, or `epic-23-retrospective` from `done`.

## 5. Implementation Handoff

**Classification:** Minor  
**Route:** Technical Writer with Developer verification

### Responsibilities

1. Create the verification record with all five rows initially pending.
2. Inspect each final claim against current production source and focused tests.
3. Update the two existing pages, create the two missing pages, and add the readiness section exactly within the approved scope.
4. Change each row to `confirmed`, `corrected`, or `unverifiable`; never infer success from a historical story status.
5. Re-run the focused test lane, the static documentation-presence/content checks, link/path checks available in the repository, and `git diff --check`.
6. Keep the sprint action `open` if any row is incomplete or `unverifiable`; escalate any behavior/scope mismatch through Correct Course.
7. When all rows pass, change only the named action item to `done` and record the exact evidence artifact.

### Success criteria

- All five topics have an authoritative operator or contributor documentation surface.
- Every verifiable claim has a quoted statement, re-runnable command, observed output, reviewer/date, and allowed verdict.
- Rate limiting distinguishes inbound request quotas, local embedding admission, shared-provider quotas, and provider 429 recovery.
- Failure recovery explains supported and unsupported non-URL re-ingestion without exposing raw payloads or internal references.
- Directory guidance pins the actual defaults, clamps, ordering, checkpoint, cancellation, and cleanup behavior.
- Index guidance preserves provisioning ownership and fail-closed readiness semantics.
- Workflow guidance prevents new scheduling paths from reintroducing mutable orchestration configuration.
- The focused current-tree lane passes, documentation diffs are clean, and the action item is closed without reopening Epic 23.

### Historical context classification and slice proof

- Stories 23.3, 23.4, 23.5, 23.6, 23.7, and 23.8 are `historical-reference-only`; their numeric adjacency has no relevance.
- No prior story is used as a whole-shape template, and no story is created, renamed, or split.
- The deliverable is one independently demonstrable outcome: a complete Epic 23 documentation pass. Because it contains five independent checks, Section 4.1 is the approved checkpoint table with explicit owner, evidence, review, and completion status.

## 6. Proposal Claim Verification

Verification baseline: branch `main`, commit `feac22bbc78c290f7ed8b1c2d5e1bfedf4dab133`, clean working tree before this proposal was written.

### V1 — The trigger and current status are exact

**Quoted claim:** “Epic 23 is done, all registered Epic 23 stories are done, and the requested documentation action is open.”

```bash
rg -n -C 3 'epic-23:|23-[0-9]+-|Add an Epic 23 documentation verification pass' _bmad-output/implementation-artifacts/sprint-status.yaml
```

**Observed:** `epic-23`, Stories 23.1-23.9, and the retrospective are `done`; the exact Action Item 4 text is `open`.  
**Verdict:** `confirmed`.

### V2 — Existing rate-limiting and failure-recovery coverage exists

**Quoted claim:** “The rate-limiting and failure-recovery pages already carry substantial Epic 23 behavior.”

```bash
rg -n 'TryConsumeWithCeilingAsync|Provider 429 handling|durable timer|SourcePayloadReference|NON_URL_REINGESTION_UNAVAILABLE|Validate non-URL source payload availability before claim' docs/operations/rate-limiting.md docs/operations/failure-recovery.md
```

**Observed:** The rate-limiting page records one actor admission per provider call/batch and durable provider-429 handling; the failure page records retained references, pre-claim validation, and `NON_URL_REINGESTION_UNAVAILABLE`.  
**Verdict:** `confirmed`.

### V3 — Directory, readiness, and determinism documentation gaps exist

**Quoted claim:** “Directory ingestion lacks dedicated operational guidance, index readiness is scattered rather than consolidated, and workflow determinism has no contributor contract.”

```bash
rg -n -i 'directory ingestion|ingest/directory|DirectorySchedulingParallelism|DirectoryBatchCheckpointSize' docs --glob '*.md'
rg -n -i 'TenantIndexReadiness|index readiness|memoized.*readiness|sole.*creation owner|FT\.CREATE' docs --glob '*.md'
rg -n -i 'IIngestionWorkflowScheduler|WorkflowConfiguration|captured.*config|mutable host config|workflow determinism' docs --glob '*.md'
```

**Observed:** Directory coverage is limited to the route table and incidental references; readiness coverage is limited to `FT.CREATE` warnings; the workflow-determinism contract terms do not appear.  
**Verdict:** `confirmed`.

### V4 — All five inherited implementation premises are present in current source/tests

| Quoted claim | Re-runnable static command | Observed | Verdict |
|---|---|---|---|
| “Single-call/batch admission and activity-owned 429 reporting feed workflow durable timers.” | `rg -n 'TryConsumeWithCeilingAsync|ReportRateLimitedAsync|CreateTimer|EmbeddingRateLimitException' src/Hexalith.Memories.Server/Activities/Ingestion/Generate*EmbeddingActivity.cs src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs tests/Hexalith.Memories.Server.Tests/{Activities/Ingestion,Workflows}` | Production and assertions cover single/batch admission, provider feedback, and workflow timers. | `confirmed` |
| “Supported non-URL recovery uses a retained source reference; unsupported recovery returns the actionable code.” | `rg -n 'SourcePayloadReference|NON_URL_REINGESTION_UNAVAILABLE|ContentBytes = null|PayloadReference = record' src/Hexalith.Memories.Server/Ingestion src/Hexalith.Memories.Server/Activities/Ingestion tests/Hexalith.Memories.Server.Tests/Ingestion` | Coordinator, persistence, registry, and tests contain the retained-reference and negative paths. | `confirmed` |
| “Directory ingestion enforces the allowlist, bounded settings, checkpointing, cleanup, and deterministic final order.” | `rg -n 'DirectorySchedulingParallelism|DirectoryBatchCheckpointSize|SupportedExtensions|UNSUPPORTED_EXTENSION|SemaphoreSlim|DeleteAsync|OrderBy' src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs tests/Hexalith.Memories.Server.Tests/Ingestion/DirectoryIngestionServiceTests.cs` | Defaults/clamps, filtering, checkpointing, cleanup, and ordinal source-URI ordering are present and tested. | `confirmed` |
| “Indexing verifies memoized readiness and does not own index creation.” | `rg -n 'ITenantIndexReadinessVerifier|EnsureReadyAsync|FT\.CREATE|Thread\.Sleep|TenantProvisioningWorkflow' src/Hexalith.Memories.Server/Activities/Indexing src/Hexalith.Memories.Server/Infrastructure tests/Hexalith.Memories.Server.Tests/{Infrastructure,Architecture,Activities/Indexing} --glob '*.cs'` | Four indexing paths use the verifier; readiness and hot-path guards cover missing/schema/concurrency/no-create/no-sleep behavior. | `confirmed` |
| “Workflow configuration is captured at scheduling and orchestration does not read mutable snapshots.” | `rg -n 'workflowConfigurationCapture\.Apply|WorkflowConfiguration|DoesNotReadMutableConfigurationSnapshots|CapturedRetryConfig' src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs tests/Hexalith.Memories.Server.Tests/{Ingestion,Workflows,Architecture}` | Scheduler capture, input consumption, replay test, and source guard are present. | `confirmed` |

### V5 — Focused behavioral lane passes

**Quoted claims:** The five implementation premises in V4 remain passing at the proposal baseline.

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll \
  -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityTests \
  -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateChunkEmbeddingsActivityTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.ReIngestionCoordinatorTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.FailedUnitsRegistryTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.DirectoryIngestionServiceTests \
  -class Hexalith.Memories.Server.Tests.Infrastructure.TenantIndexReadinessVerifierTests \
  -class Hexalith.Memories.Server.Tests.Architecture.IndexingHotPathGuardTests \
  -class Hexalith.Memories.Server.Tests.Architecture.IngestionWorkflowDeterminismGuardTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.DaprIngestionWorkflowSchedulerTests \
  -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests \
  -parallel none -noLogo
```

**Observed:** 127 total, 0 errors, 0 failed, 0 skipped, 0 not run.  
**Verdict:** `confirmed`.

## 7. Approval and Handoff Record

- **Decision:** Approved
- **Approved by:** Administrator
- **Approval date:** 2026-08-02
- **Final scope:** Minor / Direct Adjustment
- **Routed to:** Developer agent for direct documentation implementation, with Technical Writer ownership and Developer verification
- **Implementation input:** Sections 4 and 5, including the five-row fail-closed verification table
- **Implementation status:** Pending
- **Next action:** Implement the approved documentation and evidence changes, then close only the named sprint action if all five checkpoints are `confirmed` or `corrected`

Approval authorizes only the documentation and tracking changes listed in Sections 4 and 5. No documentation, implementation-artifact, or sprint-status change described by this proposal has been applied by the Correct Course workflow. Any product-behavior mismatch must return for revised scope.

## 8. Workflow Execution Log

| Date | Event | Result |
|---|---|---|
| 2026-08-02 | Trigger confirmed from Epic 23 retrospective Action Item 4 and sprint status | Complete |
| 2026-08-02 | PRD, epics, architecture, UX, implementation evidence, and documentation surfaces assessed | Complete |
| 2026-08-02 | Historical-slice and Epic-claim verification guards applied | Complete |
| 2026-08-02 | Focused current-tree verification lane executed | Passed — 127/127 |
| 2026-08-02 | Batch proposal reviewed by Administrator | Continued |
| 2026-08-02 | Sprint Change Proposal explicitly approved by Administrator | Approved |
| 2026-08-02 | Minor Direct Adjustment routed to the Developer agent | Complete |
