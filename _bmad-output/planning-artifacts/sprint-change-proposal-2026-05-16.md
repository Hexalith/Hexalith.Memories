---
project: Hexalith.Memories
date: 2026-05-16
trigger: implementation-readiness-report-2026-05-16
status: applied
mode: batch
scope: moderate
readiness_status: not-ready-for-phase-4
---

# Sprint Change Proposal - 2026-05-16

## 1. Issue Summary

The 2026-05-16 implementation-readiness assessment found that Hexalith.Memories is **not ready for Phase 4 implementation**. The planning documents are rich enough to implement from, but they currently disagree on sequencing, ownership, and phase boundaries.

The core issue is that the current epic plan asks early ingestion, indexing, case, search, and graph stories to use tenants, cases, tenant-scoped indexes, and tenant-dedicated graph databases before the stories that create and enforce those foundations. Tenant isolation is a hard MVP gate and a physical architecture invariant, so it cannot be treated as a later retrofit.

Evidence from the readiness report:

- Critical blocker: Epic 1 depends on tenant and case infrastructure that appears later in Epics 3 and 5.
- Critical blocker: Tenant isolation is sequenced after ingestion/search/case/graph implementation even though NFR8 makes it an MVP hard gate.
- Critical blocker: Story 1.5 and Story 5.1 both imply ownership over tenant index/database creation.
- Major blocker: FR71 export appears in MVP Epic 8 while architecture explicitly defers FR71 to Phase 2.
- Major blocker: CLI is MVP-critical in the PRD and epics, but architecture labels `Hexalith.Memories.Cli` as Phase 1.5 in several places.
- UX/architecture gap: the UX evidence-packet model is foundational, but no explicit contract mapping exists across `SearchResult`, `ScoredResult`, CLI JSON, MCP schema, and future web UI composition.

## 2. Change Analysis Checklist

| Item | Status | Notes |
|---|---|---|
| 1.1 Triggering story identified | [x] Done | Trigger is a planning-readiness report, not a single implementation story. The affected stories are primarily 1.5, 1.6, 3.1, 5.1, 7.1, 8.3, and 10.2. |
| 1.2 Core problem defined | [x] Done | Misunderstanding of implementation order and artifact ownership. Tenant/case foundations are used before they are created. |
| 1.3 Supporting evidence gathered | [x] Done | Evidence comes from `implementation-readiness-report-2026-05-16.md`, `prd.md`, `architecture.md`, `epics.md`, and `ux-design-specification.md`. |
| 2.1 Current epic impact | [x] Done | Epic 1 cannot stand alone unless tenant and case bootstrap exists first. |
| 2.2 Epic-level changes | [x] Done | Introduce an explicit MVP Foundation Slice 0 and clarify ownership without reopening completed code. |
| 2.3 Future epic review | [x] Done | Epics 3, 5, 7, 8, and 10 need scope/phase adjustments. |
| 2.4 Obsolete/new epics | [x] Done | No new product epic required. A planning slice/story amendment is enough. |
| 2.5 Epic order/priority | [x] Done | Tenant provisioning and minimal case bootstrap must precede ingestion/indexing/search. |
| 3.1 PRD conflicts | [x] Done | PRD already supports the desired MVP sequence; it needs traceability clarification, not a strategy change. |
| 3.2 Architecture conflicts | [x] Done | CLI phase labels, FR71 status, evidence contract ownership, and tenant lifecycle ownership need alignment. |
| 3.3 UX conflicts | [x] Done | Epics still say no UX document exists. Evidence Packet contract must be pulled into implementation artifacts. |
| 3.4 Secondary artifacts | [x] Done | `sprint-status.yaml` was reviewed and left unchanged because no new tracked implementation stories were created. |
| 4.1 Direct Adjustment | [x] Viable | Best path. Artifact edits can resolve readiness without code rollback. |
| 4.2 Rollback | [x] Not viable | Reverting completed stories would add risk and does not fix the planning contradictions by itself. |
| 4.3 PRD MVP Review | [x] Not viable | MVP thesis remains sound. Scope needs reconciliation, not reduction. |
| 4.4 Recommended path | [x] Done | Direct Adjustment with coordinated PRD/architecture/epics edits. |
| 5.1 Issue summary | [x] Done | See Section 1. |
| 5.2 Impact summary | [x] Done | See Section 3. |
| 5.3 Recommended path | [x] Done | See Section 4. |
| 5.4 MVP action plan | [x] Done | See Sections 4 and 5. |
| 5.5 Handoff plan | [x] Done | See Section 6. |
| 6.1 Checklist completion | [x] Done | All required analysis items are complete. |
| 6.2 Proposal accuracy | [x] Done | Proposal references current artifact conflicts. |
| 6.3 User approval | [x] Done | Approved by JeromePiquot on 2026-05-16 before artifact edits were applied. |
| 6.4 Sprint status update | [N/A] Skip | No sprint-status edit until approved amendment stories are created. |
| 6.5 Handoff confirmation | [x] Done | Handoff is documented in Section 6; no sprint-status change was required. |

## 3. Impact Analysis

### Epic Impact

Epic 1 needs an explicit prerequisite foundation: active tenant infrastructure and a minimal active case must exist before ingestion, indexing, search, or graph operations write data. This can be handled without renumbering every epic by adding a "Foundation Slice 0" inside the MVP plan and amending Stories 1.5, 1.6, 3.1, and 5.1.

Epic 3 remains the owner of full case management, but minimal case creation must be available before ingestion creates `contains` edges. Epic 5 remains the owner of full tenant lifecycle and isolation verification, but tenant infrastructure creation must be treated as a first implementation slice, not a later gate.

Epic 7 must be split into MVP CLI essentials and Phase 1.5 CLI expansion. The PRD already says MVP CLI essentials are `ingest`, `search --explain`, `case create/delete`, and `tenant create/delete/verify`; architecture and epics should use the same split.

Epic 8 should stop carrying FR71 in MVP if architecture's Phase 2 deferral remains the decision. Health, consistency check, and repair stay in MVP operations; data export becomes a Phase 2 candidate.

Epic 10 needs token-budget expansion semantics: omitted fields, deterministic expansion handles, follow-up expansion behavior, and schema stability.

### Story Impact

Affected current stories:

- Story 1.5: must stop implying that indexing creates tenant infrastructure.
- Story 1.6: must fail clearly when tenant or case context is missing, before any backend writes.
- Story 3.1: must reference an active tenant provisioned by the foundation slice.
- Story 5.1: must be declared the sole owner of tenant index/database lifecycle and pulled into the first implementation slice.
- Story 7.1: must distinguish MVP command groups from Phase 1.5 command groups.
- Story 8.3: should move out of MVP if FR71 remains Phase 2.
- Story 10.2: must include omitted-detail expansion handles.

Recommended new/amended planning story:

- Story 1.0 or 1.1A: **Minimal Tenant and Case Bootstrap**. This is a planning amendment, not a code reopening instruction by itself. It establishes tenant registry, tenant infrastructure provisioning through `TenantProvisioningWorkflow`, minimal case create/list, and server-side validation before ingestion/indexing begins.

### Artifact Conflicts

PRD:

- Mostly consistent with the correction. It already states cases and multi-tenancy are from day one and CLI MVP essentials are trimmed to benchmark needs.
- Needs a traceability note that FR71 is Phase 2 if that remains the selected architecture decision.

Architecture:

- Must move MVP CLI essentials before thesis validation in build order and package topology.
- Must state that `TenantProvisioningWorkflow` is the sole owner of tenant index/database creation.
- Must add explicit Evidence Packet contract ownership in `Contracts.V1`.
- Must ensure FR71 remains Phase 2 consistently if export is deferred.

Epics:

- Must replace the stale "No UX Design document" statement.
- Must add an early tenant/case foundation slice.
- Must amend Story 1.5, 1.6, 3.1, 5.1, 7.1, 8.3, and 10.2.
- Must split product capability epics from operational/release governance epics in readiness reporting.

UX:

- No UX scope reduction needed.
- The future web UI guidance should remain future-phase guidance.
- Evidence Packet semantics must move into a shared contract/story now so CLI, MCP, and future UI do not drift.

### Technical Impact

No code rollback is recommended. The next implementation work should not proceed until the artifact amendments are approved and applied.

Implementation impact after approval:

- Contract surface: add or map Evidence Packet fields in `Contracts.V1`.
- Tenant lifecycle: ensure provisioning owns index/database creation.
- Ingestion/indexing: require active tenant and active case before writes.
- CLI: MVP command subset is implementation-critical; full CLI polish remains Phase 1.5.
- MCP: omitted-detail expansion handles become acceptance criteria.

## 4. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The product thesis and PRD remain valid.
- The defects are sequencing and ownership defects, not evidence that the MVP should be abandoned.
- Rollback would be expensive and would not by itself clarify artifact ownership.
- A coordinated planning correction can make the next implementation pass safer without destabilizing completed work.

Effort: Medium.
Risk: Medium if applied carefully; high if ignored.
Timeline impact: One planning pass before new Phase 4 work.
Scope classification: Moderate.

Do not reopen completed stories solely because the artifact wording changes. Use the edits to guide future work, readiness checks, and any corrective implementation stories that are explicitly approved.

## 5. Detailed Change Proposals

### Proposal A: Add MVP Foundation Slice 0

Artifact: `_bmad-output/planning-artifacts/epics.md`
Section: high-level epic sequence and detailed Epic 1 introduction

OLD:

```markdown
### Epic 1: Foundation, Ingestion & Graph Edge Indexing
Developer can boot the full stack with a single command, ingest content from local files, and see it persisted and searchable across all three backends...
**FRs covered:** FR1, FR4, FR5, FR6, FR7, FR13, FR46, FR65, FR68
```

NEW:

```markdown
### MVP Foundation Slice 0: Tenant and Case Bootstrap
Before any ingestion, indexing, search, or graph story writes data, the system must support an active tenant with physically isolated backend infrastructure and an active case within that tenant. `TenantProvisioningWorkflow` is the sole owner of tenant index/database creation. Ingestion and indexing require an active tenant and case and fail clearly before backend writes when either is missing.

Minimum foundation coverage: FR26 (minimal case create/list), FR38 (tenant create), FR44 (tenant context validation), NFR8 (zero cross-tenant leakage invariant).

### Epic 1: Foundation, Ingestion & Graph Edge Indexing
Developer can boot the full stack, provision or select an active tenant, create or select an active case, ingest content from local files, and see it persisted and searchable across all three backends...
**FRs covered:** FR1, FR4, FR5, FR6, FR7, FR13, FR46, FR65, FR68, plus foundation dependencies FR26, FR38, FR44, NFR8
```

Justification: This resolves the forward dependency without forcing broad epic renumbering.

### Proposal B: Make TenantProvisioningWorkflow the Sole Infrastructure Owner

Artifact: `_bmad-output/planning-artifacts/epics.md`
Sections: Story 1.5 and Story 5.1

OLD:

```markdown
Given indexes are created for a tenant
When I inspect the index naming
Then the naming scheme supports concurrent versions (`{tenantId}:{model-version}:syntactic`) for future model migration
```

NEW:

```markdown
Given an active tenant provisioned by `TenantProvisioningWorkflow`
When `IndexSyntacticActivity`, `IndexSemanticActivity`, or `IndexGraphActivity` executes
Then the activities write only to the tenant infrastructure created by `TenantProvisioningWorkflow`
And they do not create or mutate tenant index/database lifecycle state

Given tenant infrastructure is missing or inactive
When any indexing activity is invoked
Then the activity fails before writing data with a structured `TENANT_NOT_PROVISIONED` error and recovery suggestion
```

Story 5.1 amendment:

```markdown
Given a tenant is created
When `TenantProvisioningWorkflow` completes
Then it is the sole owner of RediSearch, Redis Vector, and FalkorDB tenant infrastructure creation
And ingestion, search, graph, CLI, and MCP paths must treat missing or inactive tenant infrastructure as a validation failure rather than creating indexes on demand
```

Justification: This removes duplicate lifecycle ownership between Story 1.5 and Story 5.1.

### Proposal C: Make Missing Tenant/Case a Pre-Write Failure

Artifact: `_bmad-output/planning-artifacts/epics.md`
Sections: Story 1.6 and Story 3.1

OLD:

```markdown
Given a valid file and a tenant/case context
When `IngestionWorkflow` is started
Then it orchestrates: `ValidateContentActivity` -> `ExtractContentActivity` -> `GenerateEmbeddingActivity` -> fan-out...
```

NEW:

```markdown
Given a valid file, an active tenant, and an active case
When `IngestionWorkflow` is started
Then it validates tenant and case existence before extraction, embedding, or backend writes
And only then orchestrates: `ValidateContentActivity` -> `ExtractContentActivity` -> `GenerateEmbeddingActivity` -> fan-out...

Given the tenant or case is missing, inactive, or mismatched
When `IngestionWorkflow` is started
Then it fails before extraction, embedding, or indexing
And returns a structured error (`TENANT_NOT_FOUND`, `TENANT_NOT_ACTIVE`, `CASE_NOT_FOUND`, or `CASE_TENANT_MISMATCH`) with a recovery suggestion
```

Story 3.1 amendment:

```markdown
Given an active tenant provisioned by the foundation slice
When I create a case with a name and optional description
Then a case is created within that tenant...
```

Justification: This protects NFR8 and prevents accidental default-tenant behavior.

### Proposal D: Resolve FR71 Phase Conflict

Artifacts: `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/prd.md`, `_bmad-output/planning-artifacts/architecture.md`

Recommended decision: keep architecture's current decision: **FR71 export is Phase 2**.

OLD:

```markdown
### Epic 8: Observability & System Health
Operator can verify consistency across all three backends, detect and repair index/graph divergence, export memory units/metadata/graph edges in a portable format...
**FRs covered:** FR71, FR72, FR73, FR74
```

NEW:

```markdown
### Epic 8: Observability & System Health
Operator can verify consistency across all three backends, detect and repair index/graph divergence, and observe the system via readiness/liveness health checks, structured logging, distributed traces, and custom metrics.
**FRs covered:** FR72, FR73, FR74

FR71 data export is deferred to Phase 2 and must not be counted as MVP readiness unless a later sprint change explicitly pulls it forward.
```

Justification: Architecture already records FR71 as Phase 2. Epics should not simultaneously count it as MVP.

### Proposal E: Split MVP CLI Essentials from Phase 1.5 CLI Expansion

Artifacts: `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/epics.md`

OLD:

```markdown
| 6 | `Hexalith.Memories.Cli` | Developer interface (Gate 3 polish) |
```

NEW:

```markdown
| 6 | `Hexalith.Memories.Cli` | MVP CLI essentials: `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, benchmark support |
| 7 | `Hexalith.Memories.Cli` expansion | Phase 1.5 CLI polish: `explore`, `status`, `handlers`, `quickstart`, batch directory ingestion, richer diagnostics |
```

Story 7.1 amendment:

```markdown
Given the MVP CLI is installed
When I run `memories`
Then I see the MVP command groups required for thesis validation: `ingest`, `search`, `case`, `tenant`, and benchmark commands
And Phase 1.5 command groups (`explore`, `status`, `handlers`, `quickstart`, batch directory ingestion) are tracked separately and do not block MVP thesis validation unless explicitly pulled forward
```

Justification: The PRD already defines this split; architecture and epics should follow it.

### Proposal F: Add Explicit Evidence Packet Contract Mapping

Artifacts: `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/ux-design-specification.md`

NEW architecture section:

```markdown
### Evidence Packet Contract

`Contracts.V1` owns the shared Evidence Packet grammar used by CLI JSON output, MCP tool responses, and future web UI composition.

Minimum fields:
- scope: tenant ID, case ID, scope status, isolation status
- result: answer summary or ranked result summary
- sources: source references, origin identifiers, source type, freshness
- evidence: evidence strength, confidence caveat, retrieval axes used, per-axis score summary
- graph: graph relationship summary, gap markers when applicable
- state: complete, partial, weak, empty, stale, degraded, unauthorized, pending expansion
- omittedDetails: omitted count, omitted field names, deterministic expansion handles
- recoveryActions: next safe action plus optional secondary actions

`SearchResult` and `ScoredResult` remain lower-level retrieval contracts. Evidence Packet is the cross-surface response envelope that composes retrieval output, scope, state, omitted details, and recovery guidance.
```

Recommended story amendment:

```markdown
### Story 2.x: Evidence Packet Contract Mapping

As a developer and LLM-agent integrator,
I want search and diagnostic responses to share an Evidence Packet contract,
So that CLI, MCP, and future web UI expose the same trust semantics.

Acceptance Criteria:
- `Contracts.V1` defines the Evidence Packet response shape or explicitly maps existing contracts to it.
- CLI JSON search output can expose scope, sources, evidence, state, omitted details, and recovery actions.
- MCP responses use the same field semantics instead of prose-only explanations.
- Contract serialization tests cover complete, degraded, empty, and token-budget-compressed packets.
```

Justification: UX says implementation should begin with this contract before web UI composition. The readiness report flagged this as a blocker for cross-surface consistency.

### Proposal G: Update Stale UX Statement in Epics

Artifact: `_bmad-output/planning-artifacts/epics.md`
Section: `### UX Design Requirements`

OLD:

```markdown
No UX Design document - this project is a Developer Tool / API Backend with no UI component. Developer experience is addressed via CLI (Epic 7) and MCP (Epic 10).
```

NEW:

```markdown
UX guidance is captured in `ux-design-specification.md`. For MVP and Phase 1.5, the UX requirements apply primarily to CLI, MCP, structured errors, Evidence Packet semantics, tenant/case scope visibility, degraded-state handling, omitted-detail expansion, and recovery actions. Future Fluent UI / FrontComposer web UI guidance remains future-phase design guidance unless a later sprint change explicitly pulls web UI work into scope.
```

Justification: The current statement is false and can cause agents to ignore the UX artifact.

### Proposal H: Add MCP Expansion Handle Acceptance Criteria

Artifact: `_bmad-output/planning-artifacts/epics.md`
Section: Story 10.2

OLD:

```markdown
And the response includes `omitted_count` indicating how many results were omitted
And the total response stays within the specified token budget
```

NEW:

```markdown
And the response includes `omitted_count` indicating how many results or fields were omitted
And the response includes deterministic expansion handles for omitted detail groups
And a follow-up expansion request can retrieve omitted details without changing the original result identity, tenant scope, case scope, or ranking context
And omitted fields are named explicitly so agents can decide whether to expand, refine, request ingestion, or escalate
And the total response stays within the specified token budget
```

Justification: This aligns MCP behavior with the UX Evidence Packet journey and prevents silent loss of details.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Route to: Product Owner / Developer / Architect coordination.

Responsibilities:

- Product Owner: approve the phase decisions, especially FR71 Phase 2 deferral and MVP CLI essential scope.
- Architect: apply architecture amendments for build order, tenant lifecycle ownership, Evidence Packet contract ownership, and CLI phase split.
- Developer: apply epics/story amendments after approval and create any new implementation stories only if needed.
- UX owner: verify that the updated epics acknowledge `ux-design-specification.md` without accidentally pulling future web UI into MVP.

Success criteria:

- Tenant provisioning and minimal case bootstrap are explicitly before ingestion/indexing/search.
- `TenantProvisioningWorkflow` is the only tenant infrastructure lifecycle owner.
- Ingestion/indexing fail before writes when tenant or case context is invalid.
- FR71 is consistently either Phase 2 or MVP across PRD, architecture, and epics. Recommended: Phase 2.
- MVP CLI essentials and Phase 1.5 CLI expansion are clearly split across PRD, architecture, and epics.
- Evidence Packet fields are mapped to `Contracts.V1`, CLI JSON output, MCP schemas, and future UI composition.
- The stale epics UX statement is replaced.
- Story 10.2 includes deterministic omitted-detail expansion handles.

## 7. Approval Request

This proposal was approved by JeromePiquot on 2026-05-16.

The Direct Adjustment path has been applied to the planning artifacts before new Phase 4 implementation work.

No sprint-status change was made because the approved correction amended planning artifacts rather than creating new tracked implementation stories.
