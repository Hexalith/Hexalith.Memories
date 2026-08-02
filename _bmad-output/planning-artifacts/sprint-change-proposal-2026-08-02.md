# Sprint Change Proposal: Preserve Epic 23 Ingestion Invariants in Epic 24 and Epic 25 Reviews

**Date:** 2026-08-02  
**Project:** Hexalith.Memories  
**Change trigger:** Keep Epic 23 ingestion invariants on Epic 24 and Epic 25 review checklists  
**Mode:** Batch  
**Status:** Approved and implemented  
**Recommended scope:** Minor / direct documentation adjustment
**Approved by:** Administrator on 2026-08-02

## 1. Issue Summary

Epic 23 retrospective action item 2 required Epic 24 and Epic 25 review checklists to preserve six ingestion invariants: claim-check payloads, captured workflow configuration, chunked semantic vectors, source-payload retention, tenant index readiness, and single-operation rate-limit admission.

Epic 24's retrospective carried the item forward as **In progress** and explicitly stated that Epic 25 still needed the checklist. Epic 25's retrospective did not carry that action item into its previous-retrospective follow-through table and did not record a six-invariant verdict. The affected epics are complete, so the defect is an audit and review-governance omission rather than an unimplemented product requirement.

The current tree still preserves all six invariants. Static source/test inspection passed, and an expanded xUnit v3 named-class selection covering the relevant contracts, workflows, activities, chunk storage/base-ID parsing/semantic-result deduplication, coordinators, determinism/readiness/ownership guards, and rate-limiter behavior passed 337 of 337 tests. The correction must record both facts honestly:

1. The original Epic 24 review contained only a partial narrative summary, and the Epic 25 review omitted the requested checklist.
2. A dated corrective review against the current tree passes all six invariants.

No story is created, renamed, split, or used as a template by this proposal. Epic 23 story references are classified as `historical-reference-only`; their current source and test anchors were independently re-verified before use.

## 2. Impact Analysis

### Epic impact

- **Epic 23:** No scope or acceptance-criteria change. Its retrospective action item remains the source of the six-invariant obligation.
- **Epic 24:** No implementation or story reopening. Add the missing explicit six-row review gate to its canonical planning/context record and a dated corrective review addendum to its retrospective.
- **Epic 25:** No implementation or story reopening. Add the missing explicit six-row review gate to its canonical planning/context record and a dated corrective review addendum that acknowledges the original omission.
- **Remaining epics:** No dependency, order, priority, or scope change. The correction is bounded to the two named historical epics.
- **New or removed epics/stories:** None.

### Artifact impact

| Artifact | Impact | Required action |
|---|---|---|
| `_bmad-output/planning-artifacts/epics.md` | The canonical Epic 24/25 definitions lack the carried review gate. | Add a dated shared gate under both epic definitions. |
| `_bmad-output/implementation-artifacts/epic-24-context.md` | The generated/editable context has no explicit six-row review checklist. | Add the checklist with corrective-pass links to the retrospective addendum. |
| `_bmad-output/implementation-artifacts/epic-25-context.md` | The generated/editable context has no explicit six-row review checklist. | Add the checklist with corrective-pass links to the retrospective addendum. |
| `_bmad-output/implementation-artifacts/epic-24-retro-2026-07-06.md` | It records the carry-forward as in progress but only summarizes a subset of the six invariants. | Preserve the original row and append a dated corrective evidence matrix. |
| `_bmad-output/implementation-artifacts/epic-25-retro-2026-07-12.md` | It omits the Epic 23 carry-forward item entirely. | Preserve the original retrospective and append a dated corrective evidence matrix that names the omission. |
| `_bmad-output/implementation-artifacts/spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md` | Quick-dev requires a reviewable control artifact for this non-story correction. | Track workflow state, locked intent, review-loop corrections, and verification separately from the six approved handoff artifacts. |

### PRD, architecture, UX, and technical impact

- **PRD:** No conflict and no edit. FR12, FR69, NFR5, NFR17, and NFR22 already express the affected product/reliability outcomes.
- **Architecture:** No conflict and no edit. The architecture already assigns tenant infrastructure creation solely to `TenantProvisioningWorkflow`, requires replay-safe Dapr workflows and activity-owned I/O, and assigns per-tenant admission to `EmbeddingRateLimiterActor`.
- **UX:** No interface, flow, accessibility, or component change. The proposed correction only improves review evidence.
- **Code/infrastructure/deployment:** No production code, schema, API, IaC, monitoring, CI/CD, or deployment change.
- **Sprint tracking:** No `sprint-status.yaml` change because no epic/story is added, removed, renumbered, or reopened.

### Change Navigation Checklist result

| Checklist area | Result |
|---|---|
| 1. Trigger and context | Done — Epic 23 action item, Epic 24 carry-forward, and Epic 25 omission are directly evidenced. |
| 2. Epic impact | Done — only documentation/audit records for Epics 24 and 25 change; no replanning. |
| 3. Artifact conflicts | Done — PRD, architecture, and UX remain valid; five planning/implementation artifacts need documentation edits. |
| 4. Path forward | Done — direct adjustment is viable; rollback and MVP review are not justified. |
| 5. Proposal components | Done — explicit edits, evidence, scope, and handoff are defined below. |
| 6. Final review and handoff | Done — Administrator approved the proposal; the five target artifacts were updated and verified, and the quick-dev control spec records the workflow review. Sprint-status update is N/A. |

## 3. Recommended Approach

Select **Option 1: Direct Adjustment**.

- **Effort:** Low — one canonical planning amendment, two context checklist amendments, and two retrospective addenda.
- **Risk:** Low — documentation-only and additive; historical text is retained.
- **Timeline impact:** None on implementation sequencing or the active sprint.
- **Rationale:** This closes the exact governance gap without reopening completed stories, altering product scope, or falsely rewriting the historical record. The dated addenda distinguish original review evidence from corrective current-tree verification.

**Option 2: Potential Rollback** is not viable. No implementation regression was found, and reverting Epic 24 or 25 work would introduce high cost and risk without repairing the missing review record.

**Option 3: PRD MVP Review** is not viable. The MVP requirements and architecture remain achievable and already encode the protected behavior; reducing or redefining scope would be unrelated to the issue.

## 4. Detailed Change Proposals

### 4.1 Canonical Epic 24 and Epic 25 review gate

**Artifact:** `_bmad-output/planning-artifacts/epics.md`  
**Sections:** `Epic 24: Observability & Performance Hardening` and `Epic 25: Architecture Factorization & Code Health`

**OLD:**

Each epic proceeds directly from its description and metadata into its story definitions. Neither epic contains the Epic 23 ingestion-invariant review gate.

**NEW:**

Add this dated block to both epic definitions, before the first story:

> **Epic 23 ingestion invariant review gate (corrected 2026-08-02):** Every story review that touches ingestion, workflow scheduling/orchestration, payload persistence, semantic indexing, tenant lifecycle/readiness, Story 23.5 ingestion embedding provider calls, or factorization of those surfaces must record a command-backed verdict for each applicable invariant below. Aggregate epic closeout must contain one explicit verdict per row. `N/A` is permitted only when diff inspection proves the reviewed change cannot affect the invariant; a blocker must name its owner, consequence, proof boundary, and reopen trigger.
>
> 1. Non-URL source bytes and large intermediate text/vectors remain claim-checked; raw payloads do not leak into workflow history.
> 2. Retry and natural-language workflow configuration is captured at scheduling time; orchestrator logic does not read mutable host configuration.
> 3. Raw semantic vectors remain chunk-addressed under `{tenant}:vec:{memoryUnitId}:{sequence}` while the base memory-unit identifier remains the product identity.
> 4. Failed non-URL ingestion retains a valid source-payload reference for re-ingestion or returns the actionable `NON_URL_REINGESTION_UNAVAILABLE` outcome without scheduling a doomed retry.
> 5. Ingestion/indexing verifies memoized tenant/index/schema readiness and never creates indexes on demand; `TenantProvisioningWorkflow` remains the sole creation owner, while Story 23.7's approved in-place upgrades for known additive TAG fields remain allowed before readiness is cached.
> 6. Story 23.5 ingestion embedding generation performs exactly one rate-limiter admission for the single operation in `GenerateEmbeddingActivity` and one admission per bounded batch in `GenerateChunkEmbeddingsActivity`.
>
> Each verdict records the evidence command or artifact, reviewer, date, and pass/fail/blocked result. The corrective 2026-08-02 verdicts for these completed epics are recorded in their retrospective addenda.

**Rationale:** Places the obligation in the canonical epic source, scopes applicability precisely, defines fail-closed evidence, and prevents a narrative summary from substituting for six independently reviewable rows.

### 4.2 Epic context review checklists

**Artifacts:**

- `_bmad-output/implementation-artifacts/epic-24-context.md`
- `_bmad-output/implementation-artifacts/epic-25-context.md`

**Section:** Add `## Review Checklist — Epic 23 Ingestion Invariants` before `## Cross-Story Dependencies`.

**OLD:**

Neither context has an ingestion-invariant review checklist.

**NEW:**

Add the following table to both contexts:

| Invariant | Required review evidence | Status |
|---|---|---|
| Claim-check workflow payloads | Inspect scheduler/claim-check/workflow inputs and tests; prove raw source bytes and large intermediate values are replaced by scoped references. | Correctively passed 2026-08-02; see the epic retrospective addendum. |
| Captured workflow configuration | Inspect scheduler capture and orchestrator reads; prove retry/NL settings come from durable workflow input. | Correctively passed 2026-08-02; see the epic retrospective addendum. |
| Chunked semantic vectors | Inspect chunk key construction/index writes, base-ID parsing, and semantic-result deduplication tests; prove `{tenant}:vec:{memoryUnitId}:{sequence}` while hashes and parsed/results-facing identity retain the base `MemoryUnitId`. | Correctively passed 2026-08-02; see the epic retrospective addendum. |
| Source-payload retention | Inspect failed-unit persistence/cleanup/re-ingestion and negative tests; prove retained pointer or actionable rejection. | Correctively passed 2026-08-02; see the epic retrospective addendum. |
| Tenant index readiness | Inspect all four indexing activities, readiness verification, maintenance adapter, provisioning activities/workflow, and ownership guards; prove no on-demand index creation, fail-closed missing-index behavior, sole provisioning ownership, and only Story 23.7-approved in-place upgrades for known additive TAG fields before readiness is cached. | Correctively passed 2026-08-02; see the epic retrospective addendum. |
| Single-operation admission | Inspect Story 23.5 ingestion embedding activities, captured configuration, and actor/logic assertions; prove one admission for `GenerateEmbeddingActivity`'s single provider operation and one per bounded batch in `GenerateChunkEmbeddingsActivity`. | Correctively passed 2026-08-02; see the epic retrospective addendum. |

Follow the table with the canonical gate's verdict-recording rule and the statement that these statuses are dated corrective verification, not evidence that the checklist was executed during every original story review.

**Rationale:** Makes the gate visible in the artifact used to guide story execution/review while preserving the distinction between original and corrective evidence.

### 4.3 Epic 24 retrospective addendum

**Artifact:** `_bmad-output/implementation-artifacts/epic-24-retro-2026-07-06.md`  
**Section:** Append `## Corrective Review Addendum — Epic 23 Ingestion Invariants (2026-08-02)` before `## Closure`.

**OLD:**

The previous-retrospective table says:

> Keep Epic 23 ingestion invariants on Epic 24 and Epic 25 review checklists. | Epic 24 preserved claim-check, captured workflow config, trace context, tenant index readiness, and deterministic scheduler assumptions. Epic 25 still needs the same checklist. | In progress

This is a useful carry-forward but not the required six-row checklist: it substitutes trace context for chunked semantic vectors, source retention, and single-operation admission.

**NEW:**

Retain the original row unchanged and add a dated note stating that its **In progress** status was accurate on 2026-07-06. Add the six-row matrix from section 4.2 with:

- `Original Epic 24 record`: `Partial narrative; no six-row checklist` for every row.
- `Corrective verdict`: `Pass at main@8d0c1a5873d65e2ead93f50e84cd7ce20c99a92e` for every row.
- `Evidence`: the source/test anchors and commands from section 6 below.

Close the addendum with:

> This addendum closes the Epic 24 side of Epic 23 retrospective action item 2 by recording the missing explicit checklist. It verifies the current tree and does not claim that every original Epic 24 story review executed this checklist.

**Rationale:** Completes the requested review record without rewriting the historical retrospective state.

### 4.4 Epic 25 retrospective addendum

**Artifact:** `_bmad-output/implementation-artifacts/epic-25-retro-2026-07-12.md`  
**Section:** Append `## Corrective Review Addendum — Epic 23 Ingestion Invariants (2026-08-02)` before `## Closure`.

**OLD:**

The `Previous Retrospective Follow-Through` table contains six Epic 24 actions but omits the carried Epic 23 ingestion-invariant action entirely. No six-row invariant verdict appears elsewhere in the retrospective.

**NEW:**

Add a dated note explicitly acknowledging the omitted carry-forward, followed by the six-row matrix from section 4.2 with:

- `Original Epic 25 record`: `Omitted` for every row.
- `Corrective verdict`: `Pass at main@8d0c1a5873d65e2ead93f50e84cd7ce20c99a92e` for every row.
- `Evidence`: the source/test anchors and commands from section 6 below.

Close the addendum with:

> This addendum closes the Epic 25 side of Epic 23 retrospective action item 2 by recording the missing explicit checklist. It verifies the current tree and does not claim that the checklist was present during the original Epic 25 reviews.

**Rationale:** Repairs the exact omission while maintaining an honest audit trail.

## 5. Implementation Handoff

**Classification:** Minor  
**Route:** Developer agent for direct documentation implementation; completed 2026-08-02

### Responsibilities

1. Apply the five additive documentation edits exactly as proposed.
2. Preserve the original retrospective rows and narrative; use dated addenda rather than silent rewrites.
3. Keep Epic 23 references classified as historical-reference-only; create no story and change no story scope/status.
4. Re-run the static evidence commands and the expanded 337-test named-class xUnit v3 selection after editing.
5. Prove tracked `src/` and `tests/` still match the pinned baseline and contain no untracked inputs, run `git diff --check`, cover the two untracked workflow artifacts with fail-closed no-index whitespace checks, and inspect status for only the five approved target artifacts, this proposal, and the quick-dev control spec.
6. Do not modify `sprint-status.yaml`.

### Success criteria

- Both canonical epic definitions contain the same six-invariant review gate.
- Both epic contexts contain one independently reviewable row per invariant.
- Both retrospectives contain dated corrective matrices that distinguish original evidence from current-tree verification.
- The Epic 25 omission is explicitly acknowledged rather than obscured.
- All six corrective verdicts remain command-backed and passing, including the exact chunk-key contract, claim-checked extracted/chunk/vector payloads, workflow determinism guard, no-on-demand-create guard, approved additive TAG-field upgrade, and provisioning-owner workflow.
- No production code, PRD, architecture, UX, story, or sprint-status artifact changes.

## 6. Proposal Claim Verification

Verification baseline: branch `main`, commit `8d0c1a5873d65e2ead93f50e84cd7ce20c99a92e`, clean working tree before this proposal was written.

### V1 — The review-governance gap exists

**Quoted claim:** "Epic 23 required the invariant checklist; Epic 24 carried it as in progress; Epic 25 omitted it."

```bash
rg -n -F 'Keep Epic 23 ingestion invariants on Epic 24 and Epic 25 review checklists.' _bmad-output/implementation-artifacts/epic-2*-retro-*.md
```

**Observed:** Matches occur in Epic 23 at line 126 and Epic 24 at line 57; no Epic 25 match.  
**Verdict:** Pass.

### V2 — Claim-check payloads remain preserved

**Quoted claim:** "Non-URL source bytes are removed from durable workflow input and replaced with a workflow payload reference."

```bash
rg -n 'ContentBytes = null|PayloadReference = reference|NonUrlPayload_SavesSourceBytesAndReturnsSlimInput|ClaimChecksExtractedText|ReturnsChunkReferencesWithoutTextOrVectors' src/Hexalith.Memories.Server/Ingestion/IngestionPayloadClaimCheck.cs tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionPayloadClaimCheckTests.cs tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/ExtractContentActivityTests.cs tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateChunkEmbeddingsActivityTests.cs tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs
```

**Observed:** Production assignments occur at lines 43-44; focused tests cover source-byte slimming, extracted-text claim-checking, and chunk text/vector references without raw values in workflow inputs.  
**Verdict:** Pass.

### V3 — Workflow configuration remains captured

**Quoted claim:** "The scheduler captures configuration into workflow input, and the orchestrator reads that captured input rather than mutable host options."

```bash
rg -n 'workflowConfigurationCapture\.Apply|input\.WorkflowConfiguration|CapturedRetryConfig_ShouldIgnoreLaterGlobalRetryChanges|DoesNotReadMutableConfigurationSnapshots' src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowSchedulerTests.cs tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs tests/Hexalith.Memories.Server.Tests/Architecture/IngestionWorkflowDeterminismGuardTests.cs
```

**Observed:** Scheduler capture appears at line 102; workflow reads appear at lines 45 and 295; the replay/config-change regression and source guard prohibit mutable process-global reads in orchestration.  
**Verdict:** Pass.

### V4 — Semantic vectors remain chunk-addressed

**Quoted claim:** "Raw semantic writes use the base memory-unit identity plus chunk sequence under the tenant-scoped vector key."

```bash
rg -n 'BuildSemanticChunkKey|chunkSequence|memoryUnitId|ReturnsTenantScopedChunkVectorHashKey|ChunkKey_ReturnsBaseId|DeduplicateKnnResults_WithChunkHits' src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticChunksActivity.cs src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticChunksActivityTests.cs tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs tests/Hexalith.Memories.Server.Tests/Search/SemanticSearchServiceTests.cs
```

**Observed:** The indexer and schema helper build tenant/base-ID/sequence keys, the exact `tenant-a:vec:mu-1:3` contract is tested, chunk-hash fields and parsing retain the base ID, and semantic results deduplicate on that base identity. This verdict does not claim unexecuted cleanup, consistency, enumeration, repair, migration-store, or graph-scope expansion coverage.  
**Verdict:** Pass.

### V5 — Non-URL source payloads remain recoverable or fail actionably

**Quoted claim:** "Failed non-URL ingestion retains a valid source reference, while an unavailable reference produces `NON_URL_REINGESTION_UNAVAILABLE` without scheduling a doomed retry."

```bash
rg -n 'GetRetainedSourcePayloadReference|retainedSourcePayload|NON_URL_REINGESTION_UNAVAILABLE|NonUrlFailure_PersistsSourcePayloadReference' src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs src/Hexalith.Memories.Server/Ingestion/ReIngestionCoordinator.cs tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs tests/Hexalith.Memories.Server.Tests/Ingestion/ReIngestionCoordinatorTests.cs
```

**Observed:** Failure paths preserve the retained reference before cleanup; the dedicated retention test begins at line 1719; actionable rejection is implemented at coordinator line 308 and asserted in re-ingestion tests.  
**Verdict:** Pass.

### V6 — Tenant index readiness remains fail-closed and creation-free on ingestion

**Quoted claim:** "Indexing verifies existing tenant index readiness and does not create indexes on demand; a missing index fails closed, while Story 23.7's known additive TAG-field upgrades remain allowed before readiness is cached."

```bash
rg -n 'EnsureReadyAsync|FT\.CREATE|FT\.ALTER|TenantIndexNotProvisionedException|MissingAdditiveTagField_UpgradesInPlaceBeforeCaching|IndexWriteHotPaths_DoNotCreateIndexesOnDemand|TenantProvisioningWorkflow' src/Hexalith.Memories.Server/Activities/Indexing src/Hexalith.Memories.Server/Infrastructure/TenantIndexReadinessVerifier.cs src/Hexalith.Memories.Server/EventStoreIntegration src/Hexalith.Memories.Server/Activities/Tenants src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs tests/Hexalith.Memories.Server.Tests/Activities/Indexing tests/Hexalith.Memories.Server.Tests/Infrastructure/TenantIndexReadinessVerifierTests.cs tests/Hexalith.Memories.Server.Tests/Architecture/IndexingHotPathGuardTests.cs tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/RedisSearchIndexMaintenanceAdapterTests.cs tests/Hexalith.Memories.Server.Tests/Activities/Tenants tests/Hexalith.Memories.Server.Tests/Workflows/TenantProvisioningWorkflowTests.cs
```

**Observed:** All four indexing activities call `EnsureReadyAsync` and issue no on-demand `FT.CREATE`; missing indexes fail closed; maintenance remains creation-free; provisioning activities/workflow own creation; the architecture guard covers every indexing hot path; and the verifier test proves the approved additive `FT.ALTER` path before caching readiness.  
**Verdict:** Pass.

### V7 — Story 23.5 ingestion admission remains one call per provider operation or batch

**Quoted claim:** "`GenerateEmbeddingActivity` admits once for its single provider operation, and `GenerateChunkEmbeddingsActivity` admits once per bounded batch."

```bash
rg -n 'TryConsumeWithCeilingAsync|GenerateAsync|GenerateBatchAsync|ConsumesOncePerProviderBatch|Received\(1\)|Received\(2\)' src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityConfigTests.cs tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateChunkEmbeddingsActivityTests.cs tests/Hexalith.Memories.Server.Tests/Ingestion/TenantEmbeddingConfigProviderTests.cs tests/Hexalith.Memories.Server.Tests/Actors/EmbeddingRateLimiterActorTests.cs tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs
```

**Observed:** Story 23.5's single-text activity admits once for its single provider operation, and the chunk activity admits once per bounded batch; configuration-cache and actor/logic tests preserve the ceiling and serialized admission contract. Search, restore, and migration provider paths are outside this ingestion invariant.  
**Verdict:** Pass.

### V8 — Focused executable evidence passes

**Quoted claim:** "The current tree passes focused verification of all six invariants."

```bash
set -e
git diff --quiet 8d0c1a5873d65e2ead93f50e84cd7ce20c99a92e -- src tests
test -z "$(git status --short --untracked-files=all -- src tests)"
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -class Hexalith.Memories.Server.Tests.Ingestion.IngestionPayloadClaimCheckTests -class Hexalith.Memories.Server.Tests.Ingestion.DaprIngestionWorkflowSchedulerTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.ExtractContentActivityTests -class Hexalith.Memories.Server.Tests.Ingestion.ContentChunkerTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateChunkEmbeddingsActivityTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityConfigTests -class Hexalith.Memories.Server.Tests.Ingestion.TenantEmbeddingConfigProviderTests -class Hexalith.Memories.Server.Tests.Actors.EmbeddingRateLimiterActorTests -class Hexalith.Memories.Server.Tests.Actors.RateLimiterLogicTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.IndexSyntacticActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.IndexSemanticActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.IndexSemanticChunksActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.IndexNaturalLanguageSemanticActivityTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests -class Hexalith.Memories.Server.Tests.Search.SemanticSearchServiceTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.CleanupActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.VerifyConsistencyActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.EnumerateMemoryUnitIdsActivityTests -class Hexalith.Memories.Server.Tests.Ingestion.ReIngestionCoordinatorTests -class Hexalith.Memories.Server.Tests.Ingestion.FailedUnitsRegistryTests -class Hexalith.Memories.Server.Tests.Infrastructure.TenantIndexReadinessVerifierTests -class Hexalith.Memories.Server.Tests.Architecture.IndexingHotPathGuardTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.RedisSearchIndexMaintenanceAdapterTests -class Hexalith.Memories.Server.Tests.Activities.Tenants.ProvisionRediSearchActivityTests -class Hexalith.Memories.Server.Tests.Activities.Tenants.ProvisionRedisVectorActivityTests -class Hexalith.Memories.Server.Tests.Architecture.IngestionWorkflowDeterminismGuardTests -class Hexalith.Memories.Server.Tests.Workflows.TenantProvisioningWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests
```

**Observed:** Tracked `src`/`tests` match the pinned baseline with no untracked inputs; the test project builds successfully; expanded named-class execution reports total 337, errors 0, failed 0, skipped 0, not run 0.  
**Verdict:** Pass.

## 7. Approval Gate

Administrator explicitly approved this proposal with `yes` on 2026-08-02 and approved the quick-dev spec at its checkpoint. Codex quick-dev review loop 2 is durably identified by `_bmad-output/implementation-artifacts/spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md`; the Administrator-approved handoff applied the five documentation edits listed in section 2, with no story, sprint-status, production-code, PRD, architecture, or UX change.

Post-implementation verification re-ran V2-V8 successfully. The expanded named-class selection passed all 337 tests with zero errors, failures, skips, or not-run tests. `git diff --check` passed for tracked files; fail-closed no-index checks reported no whitespace errors for the two new files. The five approved target edits plus this finalized proposal are the six handoff artifacts, and the quick-dev spec is a seventh workflow control artifact rather than a product/story scope expansion.
