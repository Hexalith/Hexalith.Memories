---
baseline_commit: 33b99f5
---

# Story 21.8: Tenant Registry CAS & Rollback Integrity

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want tenant status updates and registry rollback to be race-safe,
so that a deletion claim cannot be clobbered and a failed add cannot leave an invisible tenant.

## Acceptance Criteria

1. Given `TenantRegistryService.UpdateTenantStatusAsync` currently reads `tenant-registry-{tenantId}` with `GetStateAsync` and writes it back with `SaveStateAsync`, when two lifecycle updates race, then status updates use `GetStateAndETagAsync` plus `TrySaveStateAsync` with retry and never use last-write-wins `SaveStateAsync`. Closes A47.

2. Given deletion start already uses an ETag claim in `BeginTenantDeletionAsync`, when `UpdateTenantStatusAsync` is called during provisioning, deletion failure handling, or Dapr-unavailable rollback, then it must not overwrite a newer `Deleting` claim, must preserve or clear `WorkflowInstanceId` according to the winning lifecycle transition, and must fail clearly after the CAS retry budget instead of silently clobbering the newer entry.

3. Given `RegisterOrGetTenantEntryAsync` currently saves the tenant entry and then updates `tenant-registry-index` as a separate step, when tenant registration succeeds, then the entry and index membership are committed as one atomic state transaction or an equivalent repo-approved atomic operation so a tenant can never exist only as an unlisted registry entry.

4. Given registration may fail after accepting the EventStore tenant command or after a contended index update, when registration aborts or exhausts retries, then rollback must leave a consistent end state: either both entry and index contain the tenant, or neither contains it. Rollback must not delete a tenant entry created or claimed by another workflow after the failed attempt.

5. Given `RemoveTenantAsync` currently deletes the tenant entry before removing it from the index, when tenant deletion removes registry state, then entry removal and index removal are transactional or owner-checked so `ListTenantsAsync`, tenant verification, migration, and startup routing cannot observe a stale index row or an invisible orphan after a partial failure.

6. Given Dapr v1.18 state APIs support optimistic concurrency with ETags and state transactions, when this story completes, then the implementation uses the repository-pinned Dapr packages without upgrading package versions, keeps `statestore` as the existing state component, and adds focused tests that assert ETag retry, conflict exhaustion, transaction operation shape, and state-store end state.

7. Given Story 21.1 ratified EventStore as source of truth and registry rows as read models, when changing tenant registry persistence, then do not create a new authoritative domain store. Any tenant lifecycle command acceptance remains through `IMemoriesCommandStore`; Dapr state writes remain read-model/projection updates and are documented as such in code or tests where ambiguity exists.

## Tasks / Subtasks

- [x] Task 1 - Re-run the A47 anchor preflight before editing (AC: 1, 3, 5)
  - [x] Confirm `UpdateTenantStatusAsync` still uses `GetStateAsync` plus `SaveStateAsync` without ETag.
  - [x] Confirm `RegisterOrGetTenantEntryAsync` still saves `tenant-registry-{tenantId}` and then calls `AddToIndexAsync`.
  - [x] Confirm `RegisterOrGetTenantEntryAsync` still rolls back index-update failure with unconditional `DeleteStateAsync`.
  - [x] Confirm `RemoveTenantAsync` still deletes the tenant entry before calling `RemoveFromIndexAsync`.
  - [x] Confirm no existing story file for 21.8 was implemented before this artifact; if code has already changed, reconcile the file against current code before continuing.

- [x] Task 2 - Make status updates ETag CAS and workflow-owner aware (AC: 1, 2, 7)
  - [x] Change `UpdateTenantStatusAsync` to use `GetStateAndETagAsync<TenantRegistryEntry?>` and `TrySaveStateAsync` with a bounded retry loop.
  - [x] Preserve `IMemoriesCommandStore.AcceptAsync` for lifecycle command acceptance, but do not accept the same command repeatedly on each CAS retry.
  - [x] Prevent rollback to an older status from clobbering a newer `Deleting` claim owned by another `WorkflowInstanceId`.
  - [x] Keep `WorkflowInstanceId` semantics explicit: provisioning owns provisioning retries, deleting owns deletion, terminal active/failed transitions clear ownership unless the current workflow must remain the owner.
  - [x] Return or throw a precise error when CAS retry budget is exhausted.

- [x] Task 3 - Make tenant entry and index writes atomic (AC: 3, 4, 6)
  - [x] Replace the separate entry-save plus `AddToIndexAsync` commit with `DaprClient.ExecuteStateTransactionAsync` using `StateTransactionRequest` operations, or document and test an equivalent atomic path if the pinned SDK/store prevents transactions.
  - [x] Include both `tenant-registry-{tenantId}` and `tenant-registry-index` in the same commit boundary.
  - [x] If ETags are used in transaction requests, ensure stale ETags reject the whole transaction and the retry loop reloads both entry and index.
  - [x] Remove or harden rollback so it never unconditionally deletes an entry that another concurrent workflow owns.
  - [x] Keep duplicate registration idempotent: existing tenant returns the existing entry and does not append duplicate index values.

- [x] Task 4 - Make registry removal consistent (AC: 5)
  - [x] Update `RemoveTenantAsync` so entry deletion and index removal are one atomic transaction or an owner-checked sequence with end-state repair.
  - [x] Ensure stale index rows are either impossible or cleaned in the same call before returning success.
  - [x] Preserve tenant deletion workflow idempotency: repeated deletion of an already-removed tenant still returns success at the workflow level.
  - [x] Do not change backend data deletion order from Story 21.5 except for the final registry read-model removal.

- [x] Task 5 - Update activities and endpoint rollback behavior (AC: 2, 4, 5)
  - [x] Keep `InitializeTenantRegistryActivity` replay-safe for the workflow owner and failed-state retries.
  - [x] Ensure `UpdateTenantStatusActivity` passes workflow ownership when needed, or add a narrow input field if the activity needs to preserve owner semantics.
  - [x] Review `Program.cs` tenant delete Dapr-unavailable rollback path so the rollback cannot undo a newer deletion claim.
  - [x] Keep public tenant lifecycle route names and JSON response shapes unchanged unless a test proves an additive field is required.

- [x] Task 6 - Add focused tests and drift guards (AC: 1-7)
  - [x] Add `TenantRegistryServiceTests` coverage for `UpdateTenantStatusAsync` CAS success, retry after one conflict, conflict exhaustion, missing tenant, owner preservation, and stale rollback blocked by newer deletion ownership.
  - [x] Add tests proving registration entry+index commit is atomic: success writes both; transaction failure writes neither; concurrent existing entry returns existing without duplicate index append.
  - [x] Add tests proving removal cannot leave stale index membership or invisible orphan state.
  - [x] Add workflow/activity tests for provisioning retry from failed states and deletion status rollback semantics if activity input changes.
  - [x] Add an integration or state-store end-state test using the existing Dapr/Testcontainers fixture if available; otherwise record why unit-level transaction-shape tests are the highest runnable evidence in this environment.

- [x] Task 7 - Validate and record evidence (AC: 1-7)
  - [x] Run focused tests: `TenantRegistryServiceTests`, `TenantProvisioningWorkflowTests`, `TenantDeletionWorkflowTests`, `TenantContextEnforcementTests`, and tenant endpoint tests touched by rollback behavior.
  - [x] Run relevant integration tests if Docker/Dapr sidecar permissions allow: `TenantDeletionIntegrationTests`, `TenantConfigurationIntegrationTests`, and tenant isolation/verification tests.
  - [x] Run `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`.
  - [x] If normal `dotnet test` is blocked by the known VSTest TCP-listener sandbox issue, use the in-process xUnit runner fallback and record both commands.
  - [x] Update this story's Dev Agent Record, File List, Completion Notes, and sprint status during implementation.

## Dev Notes

Story 21.8 closes audit finding A47. It is a narrow tenant-registry integrity story, not a tenant lifecycle redesign, not a new EventStore aggregate story, and not blue/green migration work. The defect is that some registry paths already use CAS while the central status-update and entry/index commit paths can still produce last-write-wins or split-brain read-model state. [Source: _bmad-output/planning-artifacts/epics.md#Story-21.8; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A47]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 21 covers consistency, namespace, deletion, routing, dedup, registry, and migration-safety remediation.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are EventStore source-of-truth, Dapr Workflow for infrastructure side effects, tenant isolation, and registry/read records as projections/read models.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are tenant create/delete/list/configuration, zero cross-tenant leakage, consistency recovery, restart durability, and operator-visible tenant state.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no module UI work is in scope, but tenant lifecycle outcomes must remain clear and recoverable for operators.
- Loaded persistent facts from `_bmad-output/project-context.md`, Hexalith LLM instructions, and Hexalith state instructions. Durable domain truth must remain EventStore-backed; Dapr state registry rows are read-model/projection state, not a new source of truth.
- Loaded previous Story 21.7, A47 audit anchor, current tenant registry/provisioning/deletion code, existing tenant tests, official Dapr state documentation, pinned Dapr.Client XML docs, and recent commits through `33b99f5`.

### Current State and Code Anchors

`TenantRegistryService.UpdateTenantStatusAsync` is the direct A47 clobber risk. It reads with `GetStateAsync<TenantRegistryEntry?>`, accepts an `UpdateTenantLifecycleStatusCommand`, builds a new entry, and writes with `SaveStateAsync` without ETag. This can overwrite a newer deletion claim or workflow owner. [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs]

`BeginTenantDeletionAsync` is the local positive pattern: it reads `GetStateAndETagAsync`, conditionally accepts the command once, writes with `TrySaveStateAsync`, retries on conflict, preserves the deletion owner, and returns the current owner when a retry should not steal the claim. Mirror this CAS style for generic status updates instead of inventing a second concurrency model. [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs]

`RegisterOrGetTenantEntryAsync` already uses ETag for the tenant entry itself, but its commit is split: save entry first, then `AddToIndexAsync`. If index update fails, it deletes the tenant entry with no owner/ETag guard. This can leave either an unlisted tenant entry or delete a concurrently claimed entry during rollback. [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs]

`AddToIndexAsync` and `RemoveFromIndexAsync` use CAS on the shared `tenant-registry-index` list. That prevents lost updates to the index list, but it does not make entry+index changes atomic across keys. The story must preserve index de-duplication while changing the commit boundary. [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs]

`RemoveTenantAsync` deletes the tenant entry, then removes the tenant ID from `tenant-registry-index`. A failure between those operations can leave a stale index row that causes `ListTenantsAsync` to probe a missing tenant or an orphaned visibility state for verification/migration workflows. [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs]

`InitializeTenantRegistryActivity` treats `Provisioning` owned by the same workflow as replay-safe, allows retry from `Failed`/`CompensationFailed` by calling `UpdateTenantStatusAsync(... Provisioning, workflowInstanceId)`, and throws `TENANT_ALREADY_EXISTS` otherwise. If status update semantics change, preserve this owner-based retry contract. [Source: src/Hexalith.Memories.Server/Activities/Tenants/InitializeTenantRegistryActivity.cs]

`TenantProvisioningWorkflow` initializes the registry before backend provisioning, then marks `Active` after `VerifyTenantActivity`. On provisioning failure it compensates created backends and marks `Failed` or `CompensationFailed`. The status activities are the only place this story should affect workflow behavior. [Source: src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs]

`TenantDeletionWorkflow` marks `Deleting` before backend cleanup and finally calls `RemoveTenantRegistryActivity`. It relies on idempotent re-entry when a tenant is already `Deleting`; do not make registry removal break deletion resume behavior. [Source: src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs]

The tenant delete endpoint claims deletion before scheduling the Dapr workflow. If scheduling fails due to Dapr unavailability and the previous status was not already `Deleting`, it attempts to roll back to the previous status via `UpdateTenantStatusAsync`. That rollback must not undo a newer deletion claim if another request won the race after the first claim. [Source: src/Hexalith.Memories.Server/Program.cs]

### Architecture Constraints

- EventStore is the authoritative domain persistence model for `Tenant`; tenant registry/read records are rebuildable projections/read models. Do not introduce a parallel authoritative registry. [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency; references/Hexalith.AI.Tools/hexalith-state-instructions.md]
- Tenant provisioning workflow remains the sole owner of tenant backend infrastructure creation. Ingestion/search must validate active tenant infrastructure and must not create tenant resources on demand. [Source: _bmad-output/planning-artifacts/architecture.md#Component-Architecture; _bmad-output/planning-artifacts/prd.md#Implementation-Sequencing]
- Dapr state writes without ETags are last-write-wins. Dapr supports optimistic concurrency with ETags and recommends retrying ETag conflicts in application code. [Source: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/]
- Dapr state supports transactions where multiple operations succeed or fail as a transactional set. Use this for entry+index consistency unless the pinned SDK/store proves unsuitable. [Source: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/; https://docs.dapr.io/developing-applications/building-blocks/state-management/howto-get-save-state/]
- The pinned `Dapr.Client` package exposes `GetStateAndETagAsync`, `TrySaveStateAsync`, `ExecuteStateTransactionAsync`, and `StateTransactionRequest` with optional ETag support. No Dapr package upgrade is needed. [Source: /home/administrator/.nuget/packages/dapr.client/1.18.4/lib/net8.0/Dapr.Client.xml]
- Keep C# one-type-per-file, nullable/warnings-as-errors, centralized package versions, and xUnit v3 + Shouldly + NSubstitute testing conventions. [Source: _bmad-output/project-context.md]

### Previous Story Intelligence

Story 21.1 ratified EventStore as source of truth with Redis/FalkorDB/Dapr state records as projections/read models. Story 21.8 should harden the registry projection/read model without making it the domain source of truth. [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md]

Story 21.2 moved domain mutations toward EventStore command acceptance and workflow projection fan-out. Preserve `IMemoriesCommandStore.AcceptAsync` at lifecycle command boundaries; do not bypass it with a registry-only domain write. [Source: _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md]

Story 21.5 tightened tenant deletion completeness and route-cache cleanup. Story 21.8 must not regress deletion data-key cleanup, route mapping cleanup, or deletion workflow activity registration. [Source: _bmad-output/implementation-artifacts/21-5-deletion-completeness.md]

Story 21.6 changed unknown/deleting/unavailable tenant event intake to retry instead of ACK/drop. Story 21.8 must keep tenant status semantics stable so event routing still treats `Deleting`, `Failed`, and `CompensationFailed` consistently. [Source: _bmad-output/implementation-artifacts/21-6-event-routing-for-unknown-unavailable-tenants.md]

Story 21.7's review fixed owner-checked deletion for dedup cleanup. Apply the same principle here: cleanup must only remove state owned by the failed operation, never a newer winner's registry entry. [Source: _bmad-output/implementation-artifacts/21-7-dedup-race-and-duplicate-instance-handling.md]

### Git Intelligence

Recent commits:

- `33b99f5 feat(story-21.8): Update orchestration state and progress for story 21.8`
- `39d4c21 feat(story-21.7): Dedup Race & Duplicate-Instance Handling`
- `56598ac feat(story-21.6): Event Routing for Unknown/Unavailable Tenants`
- `e64459b chore(story-automator): record story 21.5 completion`
- `c4df92b feat(story-21.5): Deletion Completeness`

The latest `story-21.8` commit only moved story-automator progress to 21.8 and did not create this story file or update sprint status. Treat current production/test code as the implementation starting point; do not revert the orchestration commit or submodule pointers.

### Scope Boundaries

- In scope: `TenantRegistryService` status CAS, entry+index atomic registration/removal, owner-checked rollback, `InitializeTenantRegistryActivity` and `UpdateTenantStatusActivity` input/owner semantics if needed, focused tenant registry/workflow/endpoint tests, and documentation/comments that clarify registry read-model semantics.
- In scope: small helper records/classes needed to express registry transaction results. Keep one C# type per file.
- Out of scope: blue/green embedding migration, migration marker locks, tenant physical isolation redesign, replacing all registry Dapr state with a new EventStore projection subsystem, changing public tenant route names, changing tenant status enum values, changing Dapr component names, or broad Program.cs decomposition.
- Out of scope: performance work for `GET /api/tenants` N+1 actor fan-out; that is A26, not A47.
- Out of scope: adding new package versions or upgrading Dapr; use existing repo pins.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. [Source: _bmad-output/project-context.md#Testing-Rules]
- Unit tests should assert the exact Dapr calls where practical: `GetStateAndETagAsync`, `TrySaveStateAsync`, `ExecuteStateTransactionAsync`, transaction operation keys, and no fallback to unguarded `SaveStateAsync`.
- Add state end-state tests for both success and failure, not just mock call counts. Hexalith state instructions explicitly require persisted end-state evidence for persistence changes.
- Keep workflow tests replay-safe: assert activity ordering and inputs, not wall-clock or random behavior.
- Use integration tests only where the existing fixture can run without Docker/Dapr permission blockers. If blocked, record the blocker and keep unit tests strong enough to verify transaction shape and conflict behavior.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-21.8 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-21 - approved A47 remediation scope]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A47 - tenant registry CAS and rollback finding]
- [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency - EventStore source-of-truth and registry/read model framing]
- [Source: _bmad-output/planning-artifacts/architecture.md#Component-Architecture - TenantProvisioningWorkflow owner boundary]
- [Source: _bmad-output/planning-artifacts/prd.md#Tenant-management - FR38-FR45 tenant lifecycle requirements]
- [Source: _bmad-output/project-context.md - Dapr, workflow, state, testing, style, and package rules]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md - EventStore persistence and read-model rules]
- [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs - A47 implementation anchor]
- [Source: src/Hexalith.Memories.Server/Activities/Tenants/InitializeTenantRegistryActivity.cs - provisioning replay and failed-state retry]
- [Source: src/Hexalith.Memories.Server/Activities/Tenants/UpdateTenantStatusActivity.cs - status activity boundary]
- [Source: src/Hexalith.Memories.Server/Activities/Tenants/RemoveTenantRegistryActivity.cs - registry removal activity]
- [Source: src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs - registry initialization and active/failed status flow]
- [Source: src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs - deleting status and final registry removal flow]
- [Source: src/Hexalith.Memories.Server/Program.cs - tenant delete scheduling rollback path]
- [Source: tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs - current registry coverage and missing CAS tests]
- [Source: tests/Hexalith.Memories.Server.Tests/Workflows/TenantProvisioningWorkflowTests.cs - provisioning workflow coverage]
- [Source: tests/Hexalith.Memories.Server.Tests/Workflows/TenantDeletionWorkflowTests.cs - deletion workflow coverage]
- [Source: /home/administrator/.nuget/packages/dapr.client/1.18.4/lib/net8.0/Dapr.Client.xml - pinned Dapr.Client state/transaction API surface]
- [Source: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/ - Dapr ETag OCC and transaction semantics]
- [Source: https://docs.dapr.io/developing-applications/building-blocks/state-management/howto-get-save-state/ - Dapr state transaction usage]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-04: create-story workflow loaded local BMAD skill, discovery protocol, template, checklist, customization block, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM/state instructions, previous Story 21.7, A47 audit anchor, current code anchors, pinned Dapr.Client XML docs, official Dapr state docs, and recent commits.
- 2026-07-04: story target came from user request `21.8`; sprint status had `21-8-tenant-registry-cas-and-rollback-integrity: backlog` and `epic-21: in-progress`.
- 2026-07-04: no module UI work detected; UX context was discovered but not needed for implementation scope.
- 2026-07-04: checklist validation applied after creation; story includes A47 anchors, anti-clobber owner semantics, entry+index atomicity requirements, previous-story guardrails, and focused validation requirements.
- 2026-07-04: dev-story workflow loaded local BMAD skill/checklist, Hexalith LLM/state instructions, BMAD config, project context, sprint status, and complete Story 21.8.
- 2026-07-04: A47 preflight confirmed current anchors before editing: `UpdateTenantStatusAsync` used `GetStateAsync`/`SaveStateAsync`, registration saved entry then added index with unconditional rollback delete, and removal deleted entry before index cleanup.
- 2026-07-04: implemented status CAS with `GetStateAndETagAsync`/`TrySaveStateAsync`, bounded retries, single command acceptance per requested lifecycle update, and stale `Deleting` owner clobber protection.
- 2026-07-04: implemented registration/removal atomic registry read-model updates with `ExecuteStateTransactionAsync` over `tenant-registry-{tenantId}` and `tenant-registry-index`; removed unconditional rollback delete behavior.
- 2026-07-04: normal `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter FullyQualifiedName~TenantRegistryServiceTests --no-restore --logger "console;verbosity=normal"` failed before test execution with MSBuild `SocketException (13): Permission denied`; used documented xUnit v3 in-process fallback.
- 2026-07-04: Docker integration lane blocked in this environment: `docker info --format '{{.ServerVersion}}'` returned permission denied for `/var/run/docker.sock`. Unit-level transaction-shape and end-state fallback tests are the highest runnable evidence here.
- 2026-07-04: senior developer review found and auto-fixed an existing-entry registration repair gap where missing index membership could remain invisible; added a regression test and revalidated focused workflows plus serialized solution build.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 21.8 created as the A47 implementation story after Story 21.7 completed.
- The story explicitly preserves EventStore as domain source of truth and scopes Dapr registry changes to read-model/projection consistency.
- The story identifies existing partial CAS patterns and prevents implementation from adding a second tenant lifecycle mechanism.
- A47 anchor preflight completed before production code edits; implementation starts from the expected vulnerable registry paths.
- `UpdateTenantStatusAsync` now uses ETag CAS, preserves workflow ownership for provisioning/deleting transitions, clears ownership for terminal lifecycle transitions, and blocks stale rollbacks from overwriting a newer deletion owner.
- Tenant registration and registry removal now commit entry/index mutations through Dapr state transactions with ETags and retry/end-state checks, keeping registry rows as read-model state while lifecycle commands continue through `IMemoriesCommandStore`.
- `TenantStatusUpdateInput` gained an optional `WorkflowInstanceId`; workflows and `UpdateTenantStatusActivity` pass owner context without changing route names.
- Senior review auto-fix now repairs existing tenant entries that are missing from `tenant-registry-index` through the same ETag-guarded Dapr transaction boundary, preventing invisible tenant state from persisting.
- Senior review corrected File List drift for workflow test files changed by the implementation.
- Validation passed through focused in-process xUnit suites, full Server/Contracts in-process suites, serialized solution build, and `git diff --check`; Docker/Dapr integration tests were not runnable due local Docker socket permission denial.

### File List

- `_bmad-output/implementation-artifacts/21-8-tenant-registry-cas-and-rollback-integrity.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Contracts/V1/TenantStatusUpdateInput.cs`
- `src/Hexalith.Memories.Server/Activities/Tenants/UpdateTenantStatusActivity.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs`
- `src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs`
- `src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/TenantStatusUpdateInputSerializationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/TenantDeletionWorkflowTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/TenantProvisioningWorkflowTests.cs`

### Change Log

| Date | Version | Description |
| ---- | ------- | ----------- |
| 2026-07-04 | 1.0 | Implemented Story 21.8 A47 remediation: CAS status updates, atomic tenant registry entry/index transactions, owner-aware rollback protection, workflow owner propagation, and focused registry/workflow/endpoint validation. |
| 2026-07-04 | 1.1 | Senior developer review auto-fix: repaired existing-entry missing-index path transactionally, added regression coverage, corrected File List drift, and marked story done. |

### Validation Evidence

- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter FullyQualifiedName~TenantRegistryServiceTests --no-restore --logger "console;verbosity=normal"`: blocked before test execution by MSBuild `SocketException (13): Permission denied`.
- `dotnet build tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj -m:1 /nodeReuse:false --no-restore`: passed, 0 warnings/errors.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`: passed, 0 warnings/errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantRegistryServiceTests`: 29 passed, 0 failed, 0 skipped.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.TenantStatusUpdateInputSerializationTests`: 3 passed, 0 failed, 0 skipped.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Workflows.TenantProvisioningWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.TenantDeletionWorkflowTests`: 12 passed, 0 failed, 0 skipped.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Endpoints.TenantConfigurationEndpointTests -class Hexalith.Memories.Server.Tests.Tenants.TenantContextEnforcementTests`: 31 passed, 0 failed, 0 skipped.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll`: 2185 total, 0 failed, 1 skipped.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll`: 598 passed, 0 failed, 0 skipped.
- `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`: passed, 0 warnings/errors.
- `git diff --check`: passed.
- `docker info --format '{{.ServerVersion}}'`: blocked by Docker socket permission denial, so Docker/Dapr integration tests were not runnable in this sandbox.

### Senior Developer Review (AI)

Reviewer: Codex GPT-5 on 2026-07-04

Outcome: Approved after automatic fixes. No critical issues remain.

Findings:

- HIGH fixed: `RegisterOrGetTenantEntryAsync` returned an existing tenant entry before verifying `tenant-registry-index` membership. That left a previously split entry/index state invisible to `ListTenantsAsync`, contrary to AC3/AC4. Fixed by repairing missing index membership through `ExecuteStateTransactionAsync` using the current entry and index ETags.
- MEDIUM fixed: The story File List omitted changed workflow test files discovered by git, which made the implementation record incomplete. Fixed by adding both workflow test files to the File List.

Post-fix validation:

- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`: passed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantRegistryServiceTests`: 29 passed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Workflows.TenantProvisioningWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.TenantDeletionWorkflowTests`: 12 passed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll`: 2185 total, 0 failed, 1 skipped.
- `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`: passed, 0 warnings/errors.
- `git diff --check`: passed.
