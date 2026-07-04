---
baseline_commit: 8a3725360eeb
---

# Story 21.1: Consistency Model Decision

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a solution architect,
I want a ratified consistency model for `Case`, `MemoryUnit`, and `Tenant`,
so that multi-backend writes stop diverging without a rebuild path.

## Acceptance Criteria

1. Given the current direct triple-writes (`CaseService.cs:64-112,646-694`) contradict architecture decision D3 (workflow saga/compensation), when this story completes, then the team ratifies either event-sourced aggregates with the three backends as rebuildable projections, or workflow-wrapped compensated multi-writes, and updates `architecture.md` D3.

2. Given this is decision-first, when the decision is pending, then no production code in Epic 21 dependent on the model begins. Frames A3.

## Tasks / Subtasks

- [x] Task 1 - Re-run the audit-anchor preflight before editing (AC: 1, 2)
  - [x] Confirm `src/Hexalith.Memories.Server/Cases/CaseService.cs` still writes case state directly to Redis, FalkorDB, and case activity storage in `CreateCaseAsync`.
  - [x] Confirm `DeleteMemoryUnitAsync` still deletes directly across Redis/FalkorDB and records activity without a workflow-owned transaction or event replay path.
  - [x] Confirm `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs` still uses direct Dapr state writes for tenant registry entry/status/index updates, including `UpdateTenantStatusAsync` without ETag CAS.
  - [x] Confirm the existing positive pattern still exists in `TenantProvisioningWorkflow`, `TenantDeletionWorkflow`, and `IngestionWorkflow`: orchestration code calls activities, uses retry policies, compensates partial backend writes, and avoids service I/O inside workflow logic.
  - [x] Record the current commit, moved anchors, and any adaptation in the Dev Agent Record before changing docs.

- [x] Task 2 - Decide and document the consistency model (AC: 1, 2)
  - [x] Evaluate the two permitted models from the epic: event-sourced aggregates with rebuildable projections, or workflow-wrapped compensated multi-writes.
  - [x] Apply Hexalith state rules as the default decision filter: domain state should be persisted through Hexalith.EventStore; Redis, Redis Vector, and FalkorDB should be projections/read models unless an explicit architecture exception is ratified.
  - [x] Name the source of truth for `Case`, `MemoryUnit`, and `Tenant` separately. Do not leave "authoritative syntactic hash" and "EventStore source of truth" both true without a precedence rule.
  - [x] Define what is rebuildable, what is compensatable, and what is operator-recoverable for each aggregate.
  - [x] Define the minimum implementation requirements that Story 21.2 must satisfy before claiming A3 closed.
  - [x] Preserve the decision-first gate: dependent Epic 21 production changes remain blocked until D3 is updated and this story is complete.

- [x] Task 3 - Update architecture and related consistency docs (AC: 1)
  - [x] Update `_bmad-output/planning-artifacts/architecture.md` decision D3 and the Multi-Backend Consistency section so they describe the ratified model, not only the older ingestion-only saga statement.
  - [x] Reconcile any architecture text that still implies all multi-backend mutations already use workflow saga/compensation.
  - [x] If the selected model changes the current "syntactic hash is authoritative" operator rule, update `docs/dev/consistency.md` or add a short note that the current repair workflow is pre-21.2 behavior until the ratified model is implemented.
  - [x] Cross-link audit finding A3 and Story 21.2 so future implementation work cannot skip the ratified model.

- [x] Task 4 - Capture implementation guardrails for dependent Epic 21 stories (AC: 1, 2)
  - [x] Add or update notes in this story's Dev Agent Record describing which model was ratified, why the alternative was not selected, and what code areas 21.2 must change first.
  - [x] State that Stories 21.3-21.10 may do scoped remediation only when they do not depend on unresolved source-of-truth semantics; otherwise they wait for 21.2.
  - [x] Carry Epic 20's learning: re-check current source anchors during each Epic 21 story because architecture audit line numbers are already drift-prone.
  - [x] Carry the Story 19.4 migration-marker target-consistency cluster forward into Stories 21.9 and 21.10; do not resolve it in this decision story.

- [x] Task 5 - Validate documentation-only completion (AC: 1, 2)
  - [x] Run `git diff --check -- _bmad-output/planning-artifacts/architecture.md docs/dev/consistency.md _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md _bmad-output/implementation-artifacts/sprint-status.yaml`, omitting `docs/dev/consistency.md` if untouched.
  - [x] If only Markdown/YAML changed, no `dotnet build` is required; record that this is documentation/architecture scope.
  - [x] If any production code changes are made, stop and justify why the decision-first boundary was insufficient before running the focused build/test set for the touched area.

## Dev Notes

Story 21.1 frames audit finding A3. It is intentionally decision-first: the expected output is a ratified architecture decision and updated documentation, not production data-path code. The next implementation story, 21.2, owns the actual mutation-path refactor that closes A3. [Source: _bmad-output/planning-artifacts/epics.md#Story-21.1; _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-21]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; key section is Epic 21 and Story 21.1 under Post-MVP Audit Remediation.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant sections are Multi-Backend Consistency, PRD deviations, D3, tenant provisioning/deletion workflows, and consistency verification.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are FR13, FR39, FR43, FR70, FR73, FR74, NFR8, NFR15, and NFR16-NFR19.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no module UI work is in scope.
- Loaded persistent facts from `_bmad-output/project-context.md` and root-declared reference project-context files under `references/`.
- Loaded Hexalith state instructions from `references/Hexalith.AI.Tools/hexalith-state-instructions.md` because this story decides domain persistence semantics.
- Loaded previous Epic 20 retrospective and Story 19.4 migration-marker notes because Epic 21 inherits those carry-forward risks.

### Audit-Anchor Preflight

Re-verified during story creation on 2026-07-04 against `HEAD` `8a3725360eeb`:

- A3 remains the governing audit finding: `CaseService` and tenant registry paths use direct Redis/FalkorDB/Dapr state mutations without an outbox or event replay path. The audit recommends either event-sourced aggregates plus projections or workflow-wrapped multi-backend mutations. [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A3]
- `CaseService.CreateCaseAsync` currently writes a Redis hash, merges a FalkorDB case node, records a case activity event, and returns the `Case` without workflow ownership or an event-sourced source of truth. [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs:64]
- `CaseService.DeleteMemoryUnitAsync` verifies the Redis syntactic hash, queries FalkorDB for annotations, deletes each unit through a direct helper, records activity, and returns success. It is not currently modeled as a workflow saga or EventStore command. [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs:646]
- `TenantRegistryService.UpdateTenantStatusAsync` reads Dapr state, mutates the status, and calls `SaveStateAsync` without the ETag CAS pattern used by sibling paths. [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:150]
- Tenant registry entry and index registration are separate Dapr state writes with best-effort rollback if index update fails; Story 21.8 owns the CAS/rollback integrity implementation after the model is ratified. [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:80; _bmad-output/planning-artifacts/epics.md#Story-21.8]
- `TenantProvisioningWorkflow` is the existing positive workflow/compensation pattern: it registers the tenant, provisions RediSearch, Redis Vector, and FalkorDB with retries, verifies, and compensates completed backends on failure. [Source: src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs]
- `TenantDeletionWorkflow` is the existing resumable cleanup pattern: it marks deleting, drops RediSearch, Redis Vector, FalkorDB, remaining Redis data keys, and registry state, and returns a resumable failed status on partial failure. [Source: src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs]
- `IngestionWorkflow` already implements retry/compensation around indexing fan-out and consistency verification, but audit finding A3 is broader than ingestion; `Case` and `Tenant` mutation paths are not covered by that model. [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs]

If anchors move before dev starts, re-run this preflight and update the story before deciding.

### Decision Requirements

The selected model must answer these questions explicitly:

- What is the authoritative write model for `Case`, `MemoryUnit`, and `Tenant`?
- Are Redis syntactic hashes, Redis Vector entries, FalkorDB nodes/edges, case activity streams, and tenant registry records source-of-truth data or rebuildable projections/read models?
- If the team selects event-sourced aggregates/projections, which EventStore commands/events/projections are required first for `Case`, `MemoryUnit`, and `Tenant`?
- If the team selects workflow-wrapped compensated multi-writes, what explicit exception to Hexalith state instructions is being accepted, and how will replay/rebuild gaps be mitigated?
- Which invariants must Story 21.2 enforce: idempotency, retry boundaries, compensation order, failure status, operator repair path, and tests proving no permanent divergence after injected backend failures?

Default recommendation: prefer event-sourced aggregates with Redis/FalkorDB as projections unless the architect records a deliberate exception. Hexalith state instructions say domain data must persist through Hexalith.EventStore and state is the fold of events, not a mutated row. [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md]

### Architecture Constraints

- Architecture D3 currently says no distributed transaction exists across Redis and FalkorDB, so the intended answer is eventual consistency plus Dapr Workflow saga/compensation. Story 21.1 must update that decision so it applies to all `Case`, `MemoryUnit`, and `Tenant` mutation paths, not only ingestion. [Source: _bmad-output/planning-artifacts/architecture.md#D3]
- Existing architecture states `TenantProvisioningWorkflow` owns tenant index/database creation and `TenantDeletionWorkflow` owns deletion. Do not regress those workflows into ad hoc direct writes. [Source: _bmad-output/planning-artifacts/architecture.md#Core-Architecture-Components]
- Existing consistency docs treat `{tenantId}:mu:{memoryUnitId}` as the authoritative source for repair because that was the Story 8.2 implementation model. If 21.1 ratifies EventStore as the source of truth, the docs must distinguish current repair behavior from the target model. [Source: docs/dev/consistency.md#Authoritative-source]
- `ConsistencyVerificationWorkflow` and repair flows remain operator safety nets, not a substitute for a durable write model. Do not claim A3 closed by relying on repair after divergence. [Source: docs/dev/consistency.md#Safety-model]
- Tenant isolation, authorization, audit identity, rate-limiting, and RediSearch escaping guardrails from Epic 20 must remain intact during Epic 21 data-path changes. [Source: _bmad-output/implementation-artifacts/epic-20-retro-2026-07-04.md#Next-Epic-Preparation]

### Existing Patterns to Reuse

- Dapr workflows should call activities for side effects, use replay-safe loggers, capture deterministic time from workflow context, and keep non-deterministic I/O out of workflow orchestration code. [Source: _bmad-output/project-context.md#Framework-Specific-Rules]
- `TenantProvisioningWorkflow` shows compensation over provisioned backends, with `TenantStatus.Failed` and `TenantStatus.CompensationFailed` outcomes for operator visibility. [Source: src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs]
- `TenantDeletionWorkflow` shows resumable cleanup and idempotent re-entry for tenants already in `Deleting` state. [Source: src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs]
- EventStore domain modules should use pure aggregate handlers and projection/read-model handlers. Do not hand-roll Dapr state-store calls for domain state if the selected model is event-sourced. [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md]
- Use xUnit v3, Shouldly, and NSubstitute for any tests added by dependent stories. [Source: _bmad-output/project-context.md#Testing-Rules]

### Previous Story Intelligence

Epic 20 close-out produced direct carry-forward guidance for Epic 21:

- Keep the audit-anchor preflight pattern for every Epic 21 story; implementation work has already moved line numbers.
- Resolve 21.1 before implementing 21.2; direct multi-backend mutation fixes depend on the ratified model.
- Carry the Story 19.4 migration-marker target-consistency cluster into 21.9 and 21.10.
- Preserve Epic 20 security regression guards during Epic 21 data-path changes.
- Review documentation drift after each Epic 21 story because `architecture.md` still contains older consistency and phase assumptions.

[Source: _bmad-output/implementation-artifacts/epic-20-retro-2026-07-04.md]

Story 19.4 classified the migration-marker target-consistency cluster (`15.3-RV15`, `15.3-RV16`, `15.3-RV27`) as mandatory before the next provider-migration investment. Story 21.1 should not implement that cluster, but it must keep it visible for 21.9 and 21.10. [Source: _bmad-output/implementation-artifacts/19-4-provider-registry-and-migration-residual-sweep.md; _bmad-output/implementation-artifacts/deferred-work.md]

### Git Intelligence

Recent commits show Epic 20 just completed security remediation and operational documentation:

- `8a37253 docs(epic-20): close retrospective and sync operations docs`
- `5b2b117 feat(story-20.6): RediSearch Query-Injection Hardening`
- `d942058 feat(story-20.5): Inbound Rate Limiting, Quotas & Audit Completeness`
- `e444331 feat(story-20.4): MCP Production Signing-Key Hardening`
- `ef57bd5 feat(story-20.3): Tenant-Scope Workflow & Batch Status Endpoints`

The practical lesson is that remediation stories must name current code anchors, keep scope narrow, and preserve regression tests from the prior security work.

### Latest Technical Notes

No external web research is required for this story. The decision must follow the repository-pinned stack and local Hexalith rules: .NET 10/C# 14, Dapr 1.18.4, xUnit v3, and Hexalith.EventStore as the domain persistence platform. Do not use story 21.1 to upgrade Dapr, Redis, FalkorDB, or EventStore packages.

### Expected File Touches

Required:

- `_bmad-output/planning-artifacts/architecture.md` - update D3 and related consistency text with the ratified model.
- `_bmad-output/implementation-artifacts/21-1-consistency-model-decision.md` - update Dev Agent Record and completion evidence.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - status tracking only if not already updated by create-story.

Conditional:

- `docs/dev/consistency.md` - update if the selected model changes or qualifies the current authoritative-source statement.
- `_bmad-output/implementation-artifacts/deferred-work.md` - update only if the decision changes disposition of an existing deferred entry. Do not bulk-migrate unrelated entries.

Out of scope for 21.1:

- Production code under `src/`.
- Test code under `tests/`.
- Migration subsystem implementation.
- Tenant registry CAS implementation.
- Key schema refactor.
- Natural-language vector namespace migration.

### Scope Boundaries

- Do not implement Story 21.2 in this story. The output is the ratified model and documentation guardrails.
- Do not resolve A4, A5, A16, A17, A22, A27, A28, A44, or A47 here; those have dedicated Epic 21 stories.
- Do not claim A3 closed until Story 21.2 implements the chosen model and proves failure-injection behavior.
- Do not weaken Hexalith state rules silently. If workflow-wrapped multi-write is selected over EventStore, the architecture must state the accepted exception, owner, rationale, and revisit trigger.
- Do not treat consistency repair as the source-of-truth strategy. It is an operator repair tool.
- Do not initialize or update nested submodules.

### Testing Standards

For this documentation-only decision story, `git diff --check` over touched Markdown/YAML is sufficient. If production code changes anyway, run the focused tests for the touched surface and the full solution build unless blocked by environment constraints.

For future implementation stories, use xUnit v3, Shouldly, and NSubstitute; test folders should mirror product areas; workflow tests should assert activity order, retry/compensation behavior, idempotency, custom status, and replay-safe assumptions. [Source: _bmad-output/project-context.md#Testing-Rules]

### Project Structure Notes

Keep the decision in planning artifacts and this story file. Do not create a new ADR folder unless the repo already has a specific architecture-decision placement for Memories; `architecture.md` D3 is the required source to update. If an additional decision note is added, cross-link it from `architecture.md` and keep generated/BMAD artifacts under `_bmad-output/`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-21.1 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A3 - direct triple-writes without event sourcing or outbox]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-21 - approved remediation scope and sequencing]
- [Source: _bmad-output/planning-artifacts/architecture.md#D3 - current eventual consistency plus workflow saga/compensation decision]
- [Source: _bmad-output/planning-artifacts/prd.md#FR13-FR39-FR43-FR70-FR73-FR74 - consistency, deletion, migration, and repair requirements]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md - Hexalith.EventStore persistence rules]
- [Source: _bmad-output/project-context.md - repo-wide C#, Dapr workflow, testing, package, tenant isolation, and submodule rules]
- [Source: docs/dev/consistency.md - current operator consistency and repair model]
- [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs - current direct case and memory-unit mutation anchors]
- [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs - current tenant registry state anchors]
- [Source: src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs - existing saga/compensation pattern]
- [Source: src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs - existing resumable deletion pattern]
- [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs - existing ingestion retry/compensation pattern]
- [Source: _bmad-output/implementation-artifacts/epic-20-retro-2026-07-04.md - Epic 21 handoff and regression guardrails]
- [Source: _bmad-output/implementation-artifacts/19-4-provider-registry-and-migration-residual-sweep.md - migration-marker carry-forward cluster]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-04: create-story workflow loaded required skill, discovery protocol, template, checklist, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith state instructions, and current code anchors.
- 2026-07-04: story creation preflight confirmed 21.1 is the first Epic 21 story and is backlog before creation; `epic-21` must move to `in-progress` and `21-1-consistency-model-decision` to `ready-for-dev`.
- 2026-07-04: audit anchors rechecked at `8a3725360eeb`; `CaseService` still has direct Redis/FalkorDB mutation paths, `TenantRegistryService.UpdateTenantStatusAsync` still uses non-CAS `SaveStateAsync`, and workflow compensation patterns exist in provisioning/deletion/ingestion.
- 2026-07-04: dev-story preflight rechecked anchors at `8a3725360eeb`. `CreateCaseAsync` still writes Redis hash + FalkorDB node + case activity directly; `DeleteMemoryUnitAsync` still deletes Redis/FalkorDB records and records activity directly; `UpdateTenantStatusAsync` still reads and `SaveStateAsync`s Dapr state without ETag CAS. No line-anchor adaptation was needed.
- 2026-07-04: positive workflow pattern still exists in `TenantProvisioningWorkflow`, `TenantDeletionWorkflow`, and `IngestionWorkflow`: workflow orchestration calls activities, uses retry policies, compensates partial backend writes, and keeps service I/O in activities.
- 2026-07-04: selected EventStore aggregates with rebuildable projections as the ratified model. Workflow-wrapped compensated multi-writes was not selected because it would make a silent exception to Hexalith state rules and still lack an event replay/rebuild path for domain state.
- 2026-07-04: validation passed: `git diff --check -- _bmad-output/planning-artifacts/architecture.md docs/dev/consistency.md _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md _bmad-output/implementation-artifacts/sprint-status.yaml`.
- 2026-07-04: QA documentation guard added `ConsistencyModelDecisionTests`; review found stale story File List/test evidence and residual old consistency wording in the operator guide and architecture gate summary.
- 2026-07-04: senior review auto-fixed the residual wording, updated the story record, and reran focused validation.

### Implementation Notes

- Ratified model: `Case`, `MemoryUnit`, and `Tenant` domain state is sourced from Hexalith.EventStore events. Redis syntactic hashes, Redis Vector entries, FalkorDB nodes/edges, case activity streams, and tenant registry/read records are rebuildable projections/read models.
- Source of truth by aggregate: `Case` commands/events own case identity, status, membership/activity semantics, and case-to-memory-unit relationships; `MemoryUnit` commands/events own unit identity, source, content/metadata, deletion, and indexing intent; `Tenant` commands/events own tenant registry/status semantics. Tenant infrastructure creation/deletion remains workflow-owned side-effect orchestration because indexes/databases are not domain state.
- Rebuildable: syntactic hashes, semantic vector entries, natural-language vector entries, graph nodes/edges, case activity/read streams, tenant registry/read records, and consistency-verification views. Compensatable: projection fan-out writes and tenant infrastructure side effects after activity failure. Operator-recoverable: stuck/failed workflows, projection divergence, orphaned projection records, and tenant provisioning/deletion states surfaced as failed or compensation-failed outcomes.
- Story 21.2 minimum requirements before A3 can be claimed closed: introduce EventStore command/event handling for `Case`, `MemoryUnit`, and `Tenant` mutation semantics; route current direct `CaseService` and tenant registry mutation paths through that model; project to Redis/FalkorDB through idempotent workflow activities; preserve tenant isolation/security guards from Epic 20; add failure-injection tests proving no permanent divergence after Redis, Redis Vector, FalkorDB, registry, or activity-recording failures; document replay/rebuild and operator recovery.
- Dependent Epic 21 gate: Stories 21.3-21.10 may perform scoped remediation only when their changes do not depend on unresolved source-of-truth semantics. Any source-of-truth-dependent work waits for 21.2. Each Epic 21 story must re-check current source anchors because audit line numbers have already drifted.
- Story 19.4 migration-marker target-consistency cluster remains carried forward to Stories 21.9 and 21.10. This decision story does not resolve it.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 21.1 created as a decision-first architecture story. Dependent Epic 21 production code remains blocked until D3 is ratified and updated.
- No production code changes are part of this create-story run.
- Dev-story ratified EventStore aggregates with rebuildable projections as the consistency target for `Case`, `MemoryUnit`, and `Tenant`.
- Updated `architecture.md` D3 and Multi-Backend Consistency so A3 and Story 21.2 cannot be implemented against the older ingestion-only saga statement.
- Updated `docs/dev/consistency.md` to state that syntactic hash repair is current pre-21.2 operator behavior, while EventStore is the target domain source of truth.
- QA added a documentation guard test for architecture D3, the operator consistency guide, and this story record.
- Senior review removed residual old-model wording from the architecture gate summary and operator guide, updated story tracking, and completed focused validation. No production code changed.

### File List

- `_bmad-output/planning-artifacts/architecture.md`
- `docs/dev/consistency.md`
- `_bmad-output/implementation-artifacts/21-1-consistency-model-decision.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/Hexalith.Memories.Server.Tests/Architecture/ConsistencyModelDecisionTests.cs`

### Change Log

- 2026-07-04: Ratified EventStore aggregate source-of-truth consistency model, updated architecture D3 and operator consistency docs, and marked Story 21.1 ready for review.
- 2026-07-04: Senior review auto-fixed residual old-model wording, documented QA guardrail test artifacts, and marked Story 21.1 done.

## Senior Developer Review (AI)

### Review Outcome

Approved after automatic fixes. Remaining critical issues: 0.
MCP resource discovery returned no exposed resources; this review used repository-pinned Hexalith/project documentation and no external web fallback was required.

### Findings Fixed

- [HIGH] `docs/dev/consistency.md` still described a missing "authoritative syntactic record" in the detection summary even though Story 21.1 ratified Hexalith.EventStore as the target domain source of truth. Fixed the wording to "current repair-source syntactic record".
- [MEDIUM] `_bmad-output/planning-artifacts/architecture.md` still listed "Eventual consistency + compensation" in the gate summary after D3 had been updated. Fixed the row to the EventStore source-of-truth/projection-compensation model.
- [MEDIUM] The story File List omitted the QA-generated architecture guard test and shared test summary. Added both files and updated completion notes.

### Validation

- `git diff --check -- _bmad-output/planning-artifacts/architecture.md docs/dev/consistency.md _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/tests/test-summary.md tests/Hexalith.Memories.Server.Tests/Architecture/ConsistencyModelDecisionTests.cs`
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false`
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.ConsistencyModelDecisionTests`
