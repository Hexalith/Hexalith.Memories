---
baseline_commit: b0ff9bf5d10d9f89d47c277a5f4f5dca5f34686b
---

# Story 21.5: Deletion Completeness

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want case and tenant deletion to remove every associated key,
so that a re-created case/tenant cannot inherit stale routing or a write-blocking marker.

## Acceptance Criteria

1. Given `DeleteCaseAsync` accepts a case-deletion command and schedules `CaseDeletionProjectionWorkflow`, when the projection cleanup completes, then every aggregate-case-map hash field that points at the deleted case is removed with `HDEL` semantics and the in-process event-router cache can no longer route future events to that deleted case. Closes A16.

2. Given `TenantEventRouter` caches `(tenantId, aggregateType) -> caseId` and the Redis mapping store persists the same route under `{tenantId}:eventstore:aggregate-case-map`, when a case mapping is removed or expires, then routing either revalidates the cached case against the persisted map or invalidates matching cache entries before accepting a route. Stale cached routes to deleted cases must not survive indefinitely across long-running server instances.

3. Given `DeleteTenantDataKeysActivity` currently scans only `{tenantId}:case:*` and `dedup:{tenantId}:*`, when a tenant deletion runs, then it also removes tenant-scoped `eventstore:*`, `embedding-migration:*`, and defensive memory/vector key leftovers not covered by `FT.DROPINDEX DD`, including syntactic, raw semantic, current natural-language semantic, and legacy natural-language semantic prefixes. Closes A17.

4. Given tenant deletion already drops RediSearch, raw Redis Vector, natural-language Redis Vector, and FalkorDB before registry removal, when the new data-key sweep runs, then it remains idempotent, retry-safe, batch-bounded, and does not fail if a prior activity already deleted a key family or index.

5. Given this story touches persisted read models and event-routing infrastructure, when implementation completes, then focused unit tests plus at least one Redis-backed end-state test prove deleted cases/tenants leave no aggregate-case-map, case cache route, migration marker, EventStore route metadata, or orphan memory/vector keys behind.

## Tasks / Subtasks

- [x] Task 1 - Add an aggregate-case-map cleanup contract (AC: 1, 2)
  - [x] Extend `IAggregateCaseMappingStore` with a deletion method that removes all aggregate-type fields whose stored value equals the deleted `caseId`; do not assume the hash field name is the case ID.
  - [x] Implement the Redis method in `RedisAggregateCaseMappingStore` by scanning `{tenantId}:eventstore:aggregate-case-map` and `HashDeleteAsync`/`HDEL`ing matching aggregate-type fields in bounded batches.
  - [x] Keep existing first-time creation lock behavior intact; do not rename `{tenantId}:eventstore:aggregate-case-lock:{aggregateType}` keys.
  - [x] Add unit coverage proving multiple aggregate types pointing at one case are removed, other case mappings survive, missing maps are idempotent, and invalid tenant/case inputs fail before Redis calls.

- [x] Task 2 - Invalidate or revalidate event-router cache entries (AC: 1, 2)
  - [x] Add a narrow invalidation path for `TenantEventRouter` so deletion cleanup can remove cached entries whose case ID equals the deleted case.
  - [x] Protect multi-instance/long-lived-process correctness by either revalidating cache hits against `IAggregateCaseMappingStore` before accepting them or adding a bounded TTL with tests proving stale routes age out.
  - [x] Preserve curated `SearchIndexEntryChanged` / `SearchIndexEntryRemoved` routing behavior: curated search-index events still bypass case auto-creation and do not require cache invalidation.
  - [x] Extend `TenantEventRouterTests` so a case route cached before deletion is not returned after the persisted mapping is removed or invalidated.

- [x] Task 3 - Wire case deletion projection cleanup (AC: 1, 2, 4)
  - [x] Update `CaseDeletionProjectionWorkflow` / `DeleteCaseProjectionActivity` or add a dedicated workflow activity so aggregate-case-map cleanup occurs after the delete command is accepted and before the workflow reports success.
  - [x] Keep Story 21.2's ordering invariant: `MarkCaseDeletingActivity` runs before destructive projection cleanup, and failures surface to workflow retry instead of silently marking success.
  - [x] Ensure projection cleanup stays idempotent: reruns after keys are already gone return success, while Redis failures surface for Dapr Workflow retry.
  - [x] Extend `CaseDeletionProjectionWorkflowTests` and/or `DeleteCaseProjectionActivity` tests to prove cleanup ordering and retry behavior.

- [x] Task 4 - Expand tenant data-key deletion coverage (AC: 3, 4)
  - [x] Extend `DeleteTenantDataKeysActivity` scan patterns to include `{tenantId}:eventstore:*` and `{tenantId}:embedding-migration:*`.
  - [x] Add defensive orphan sweeps using `IndexSchemaDefinitions` helpers for syntactic `mu`, raw semantic `vec`, current NL `vecnl`, and legacy NL `vec:nl` keys. Do not reintroduce raw `:mu:`/`:vec:` production literals that would violate Story 21.4's architecture guard.
  - [x] Preserve existing `{tenantId}:case:*` and `dedup:{tenantId}:*` cleanup and batching behavior.
  - [x] Keep the activity Docker-free and retry-safe: missing connected Redis server still throws, empty key families still succeed, and each scan deletes in bounded batches.

- [x] Task 5 - Prove deletion end state (AC: 1, 3, 5)
  - [x] Update `DeleteTenantDataKeysActivityTests` to assert all expected scan patterns and batched deletes.
  - [x] Add tests for `RedisAggregateCaseMappingStore` under `tests/Hexalith.Memories.EventStore.Tests` or the closest existing EventStore test project.
  - [x] Extend `TenantDeletionIntegrationTests.DeleteTenant_WithIndexedData_ShouldRemoveRegistryAndBackendState` or add a sibling integration test that seeds `eventstore:*`, `embedding-migration:*`, and orphan memory/vector keys, runs tenant deletion, and asserts real Redis end state is empty.
  - [x] Add a case-deletion test that creates or substitutes a cached route, deletes the case, and proves the next event for that aggregate type cannot route to the deleted case.

- [x] Task 6 - Validate and record evidence (AC: 1-5)
  - [x] Run focused unit tests for `TenantEventRouterTests`, aggregate-case mapping store tests, `CaseDeletionProjectionWorkflowTests`, `DeleteTenantDataKeysActivityTests`, and affected `CaseServiceTests`.
  - [x] Run the Redis-backed tenant deletion integration test if Docker/Aspire services are available; otherwise record the exact blocker and keep unit end-state evidence.
  - [x] Run `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`.
  - [x] If normal `dotnet test` is blocked by the known VSTest TCP listener sandbox issue, use the in-process xUnit runner fallback and record both commands.
  - [x] Update this story's Dev Agent Record, File List, Completion Notes, and Change Log.

## Dev Notes

Story 21.5 closes audit findings A16 and A17. It is a deletion-completeness story, not a new deletion API, registry-CAS story, or key-schema refactor. The implementation must remove stale route/migration/key leftovers created by existing workflows while preserving the EventStore source-of-truth model established by Stories 21.1 and 21.2. [Source: _bmad-output/planning-artifacts/epics.md#Story-21.5; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A16-A17]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 21 owns consistency, namespace, deletion, routing, dedup, registry, and migration-safety remediation.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are EventStore source-of-truth with Redis/FalkorDB read-model projections, Dapr Workflow retry/compensation, physical tenant isolation, and async tenant deletion.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are FR13 partial-failure recovery, FR27 case deletion, FR35 memory-unit deletion, and FR39 tenant deletion.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no module UI work is in scope, but destructive-operation state must remain honest and support-safe.
- Loaded persistent facts from `_bmad-output/project-context.md` and root-declared reference project-context files under `references/`.
- Loaded Hexalith state instructions because this story touches persisted domain/read-model data; domain state remains Hexalith.EventStore events, while Redis/FalkorDB keys are projections, routing metadata, locks, and operational markers.
- Loaded previous Story 21.4 and recent commits `53cc9c2`, `c350b7a`, `1b072f4`, and `b0ff9bf`.

### Current State and Code Anchors

`CaseService.DeleteCaseAsync` verifies the Redis case key, gathers memory-unit IDs from FalkorDB, accepts `DeleteCaseCommand`, and schedules `CaseDeletionProjectionWorkflow`. It does not delete read-model keys directly after Story 21.2 and must keep that command-first boundary. [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs]

`CaseDeletionProjectionWorkflow` currently calls `MarkCaseDeletingActivity` and then `DeleteCaseProjectionActivity`. `DeleteCaseProjectionActivity` deletes memory-unit syntactic/semantic/NL vector hashes, graph nodes, case graph node, case members, case activity, and case hash. It has no dependency on aggregate-case mapping cleanup or router cache invalidation today. [Source: src/Hexalith.Memories.Server/Workflows/CaseDeletionProjectionWorkflow.cs; src/Hexalith.Memories.Server/Activities/Cases/DeleteCaseProjectionActivity.cs]

`IAggregateCaseMappingStore` supports get/count/lock/store only. `RedisAggregateCaseMappingStore` stores aggregate-type fields under `{tenantId}:eventstore:aggregate-case-map` and creation locks under `{tenantId}:eventstore:aggregate-case-lock:{aggregateType}`; there is no delete method. [Source: src/Hexalith.Memories.EventStore/IAggregateCaseMappingStore.cs; src/Hexalith.Memories.EventStore/RedisAggregateCaseMappingStore.cs]

`TenantEventRouter` caches resolved routes in a private process-local dictionary keyed by tenant and aggregate type. Once cached, it returns a cached case without consulting Redis again. A case deletion in the same or another server process can therefore leave a stale route unless this story adds invalidation, revalidation, or TTL behavior. [Source: src/Hexalith.Memories.EventStore/TenantEventRouter.cs]

`DeleteTenantDataKeysActivity` currently scans `{tenantId}:case:*` and `dedup:{tenantId}:*` only. The activity runs after `DeleteRediSearchActivity`, `DeleteRedisVectorActivity`, and FalkorDB deletion in `TenantDeletionWorkflow`, so it is the right cleanup backstop for route metadata, migration markers, and orphan memory/vector keys that index `DD` did not remove. [Source: src/Hexalith.Memories.Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs; src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs]

`DeleteRediSearchActivity` and `DeleteRedisVectorActivity` already drop tenant indexes with `FT.DROPINDEX DD`. `DeleteRedisVectorActivity` also drops the natural-language semantic index and current `vecnl` hashes. Story 21.5 must not rely solely on `DD` because the audit found orphanable keys and missing key families; add a defensive sweep after these activities. [Source: src/Hexalith.Memories.Server/Activities/Tenants/DeleteRediSearchActivity.cs; src/Hexalith.Memories.Server/Activities/Tenants/DeleteRedisVectorActivity.cs]

`IndexSchemaDefinitions` is the single source of truth for memory/vector key prefixes after Story 21.4. Use `GetSyntacticKeyPrefix`, `GetSemanticKeyPrefix`, `GetNaturalLanguageSemanticKeyPrefix`, and `GetLegacyNaturalLanguageSemanticKeyPrefix` for defensive sweeps. Do not add raw production `:mu:`, `:vec:`, or `:vecnl:` literals outside that class. [Source: src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs; _bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md]

### Architecture Constraints

- Domain writes for cases and memory units must continue through EventStore commands first; Redis/FalkorDB deletion remains projection cleanup and retryable workflow side effects. Do not bypass `_commandStore.AcceptAsync(...)` or reintroduce direct multi-backend mutation in `CaseService`. [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency; _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md]
- Dapr workflow code must stay replay-safe. Do not add wall-clock reads, Redis calls, network I/O, random IDs, or hidden mutable state to workflow orchestrators; put side effects in activities/services. [Source: _bmad-output/project-context.md#Framework-Specific-Rules]
- Tenant deletion must stay resumable. Missing keys and already-dropped indexes are success states, while Redis/graph failures surface to workflow retry and leave the tenant in a retryable deletion/failed state. [Source: _bmad-output/planning-artifacts/architecture.md#Failure-Modes]
- Tenant isolation remains physical/prefix/index based. Defensive sweeps must be tenant-scoped and must not scan unbounded global `eventstore:*` or `embedding-migration:*` patterns without the tenant prefix. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Do not initialize/update nested submodules, change package versions, add new Redis libraries, or modify reference submodule contents.

### Previous Story Intelligence

Story 21.1 ratified EventStore aggregates with Redis/FalkorDB as rebuildable projections. This story cleans up projections and routing metadata; it does not move the source of truth away from EventStore. [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md]

Story 21.2 moved case, annotation, memory-unit deletion, and case deletion mutations through EventStore command acceptance before projection fan-out. Preserve the invariant that service-level deletion methods do not delete read-model keys directly after command acceptance. [Source: _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md]

Story 21.3 introduced current natural-language vector keys under `{tenant}:vecnl:*` and retained legacy `{tenant}:vec:nl:*` only for migration. Tenant deletion's defensive sweep must remove both current and legacy NL vector leftovers. [Source: _bmad-output/implementation-artifacts/21-3-natural-language-vector-namespace-separation.md]

Story 21.4 consolidated memory/vector key helpers and explicitly left `case:*`, `dedup:*`, `eventstore:*`, and `embedding-migration:*` namespaces for 21.5/21.7/21.9. Use helper APIs for memory/vector keys and keep adjacent namespaces scoped to this story's cleanup needs. [Source: _bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md]

Story 21.4 validation reported normal VSTest may be blocked in this sandbox by TCP listener permissions; the in-process xUnit fallback passed full server tests. Use normal test commands first and record the fallback if needed. [Source: _bmad-output/implementation-artifacts/21-4-key-schema-single-source-of-truth.md#Debug-Log-References]

### Git Intelligence

Recent commits:

- `b0ff9bf feat(story-21.4): Key-Schema Single Source of Truth`
- `1b072f4 feat(story-21.3): Natural-Language Vector Namespace Separation`
- `95048df feat: Implement natural-language vector namespace separation`
- `c350b7a feat(story-21.2): Transactional Multi-Backend Mutation`
- `53cc9c2 feat(story-21.1): Consistency Model Decision`

The Epic 21 pattern is narrow audit remediation with explicit source anchors, focused regression tests, architecture guard awareness, and story File List hygiene. Keep 21.5 similarly bounded.

### Latest Technical Notes

No external technical research is required. The story uses repo-pinned .NET 10, Dapr Workflow 1.18.4, StackExchange.Redis/NRedisStack, and existing Redis `SCAN`/`HDEL`/`KEYDELETE` patterns. Do not introduce new dependencies or upgrade Redis/Dapr packages.

### Scope Boundaries

- In scope: aggregate-case-map cleanup for deleted cases, event-router stale-cache prevention, tenant deletion sweep expansion for `eventstore:*`, `embedding-migration:*`, and orphan memory/vector key families, plus focused tests.
- In scope: adding a narrow cache invalidation/revalidation abstraction if needed to keep `TenantEventRouter` correct after case deletion.
- Out of scope: changing tenant registry CAS/rollback semantics (Story 21.8), dedup TOCTOU fixes (Story 21.7), unknown/unavailable tenant event retry/dead-letter behavior (Story 21.6), blue/green migration marker locking/abort (Story 21.9), and route/client consolidation (Story 25.3).
- Out of scope: changing runtime memory/vector key shapes, relaxing Story 21.4 literal guard, broadening tenant deletion into backup/restore, or changing public API response contracts unless tests prove it is required for existing behavior.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute for focused tests. [Source: _bmad-output/project-context.md#Testing-Rules]
- Keep unit tests Docker-free except existing integration tests that already require Aspire/Redis/FalkorDB.
- Test deletion as end state, not just call count: aggregate-case-map hash fields gone, cache route not reused, seeded `eventstore:*`/`embedding-migration:*`/orphan memory-vector keys gone.
- Preserve existing `TenantDeletionWorkflowTests` ordering expectations and add only assertions needed for the expanded data-key cleanup.
- Run focused tests first, then `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-21.5 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-21 - approved 21.5 coverage]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A16-A17 - deletion completeness findings]
- [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency - EventStore source-of-truth and projection model]
- [Source: _bmad-output/project-context.md - repo-wide C#, Dapr workflow, Redis, tenant isolation, testing, and submodule rules]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md - Hexalith.EventStore persistence and projection rules]
- [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs - command-first case deletion scheduling]
- [Source: src/Hexalith.Memories.Server/Workflows/CaseDeletionProjectionWorkflow.cs - case deletion projection ordering]
- [Source: src/Hexalith.Memories.Server/Activities/Cases/DeleteCaseProjectionActivity.cs - current case/key cleanup]
- [Source: src/Hexalith.Memories.EventStore/IAggregateCaseMappingStore.cs - missing delete contract]
- [Source: src/Hexalith.Memories.EventStore/RedisAggregateCaseMappingStore.cs - aggregate-case-map key ownership]
- [Source: src/Hexalith.Memories.EventStore/TenantEventRouter.cs - process-local route cache]
- [Source: src/Hexalith.Memories.Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs - current tenant data-key sweep]
- [Source: tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteTenantDataKeysActivityTests.cs - existing cleanup activity tests]
- [Source: tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs - router cache and shared mapping tests]
- [Source: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantDeletionIntegrationTests.cs - Redis-backed tenant deletion end-state tests]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-04: create-story workflow loaded BMAD skill, discovery protocol, template, checklist, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM/state instructions, previous Story 21.4, architecture audit A16/A17, current code anchors, existing tests, and recent commits.
- 2026-07-04: story target came from user request `21.5`; sprint status had `21-5-deletion-completeness: backlog` and `epic-21: in-progress`.
- 2026-07-04: no module UI work detected; UX context loaded only for destructive-operation trust/state constraints.
- 2026-07-04: checklist validation applied after creation; no blocking gaps remained after resolving the template agent-model placeholder.
- 2026-07-04: dev-story workflow resolved customization (`activation_steps_prepend`/`append` empty; persistent facts from project-context files), loaded BMAD config, project context, and Hexalith state instructions before implementation.
- 2026-07-04: normal focused `dotnet test` commands compiled but VSTest aborted with sandbox TCP listener `SocketException (13): Permission denied`; fallback used xUnit v3 in-process runner.
- 2026-07-04: focused EventStore fallback passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll -class Hexalith.Memories.EventStore.Tests.TenantEventRouterTests -class Hexalith.Memories.EventStore.Tests.RedisAggregateCaseMappingStoreTests` -> 22 total, 0 failed.
- 2026-07-04: focused Server fallback passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Workflows.CaseDeletionProjectionWorkflowTests -class Hexalith.Memories.Server.Tests.Activities.Cases.DeleteCaseRouteMappingsActivityTests -class Hexalith.Memories.Server.Tests.Activities.Tenants.DeleteTenantDataKeysActivityTests` -> 8 total, 0 failed.
- 2026-07-04: affected `CaseServiceTests` passed with xUnit in-process runner -> 40 total, 0 failed.
- 2026-07-04: Story 21.4 architecture guard passed after the new tenant sweep used `IndexSchemaDefinitions` helpers -> 7 total, 0 failed.
- 2026-07-04: Redis-backed tenant deletion integration test was updated and compiled, but execution was blocked because `docker info --format '{{.ServerVersion}}'` failed with permission denied on `/var/run/docker.sock`.
- 2026-07-04: full solution validation passed: `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` -> 0 warnings, 0 errors.
- 2026-07-04: `git diff --check` passed.
- 2026-07-04: story-automator review loaded requested review skill/workflow/instructions/checklist, project context, and Hexalith state instructions; normal `dotnet test` remained blocked by sandbox `SocketException (13): Permission denied`, so focused validation used single-node builds plus xUnit v3 in-process runner.
- 2026-07-04: review fix registered `DeleteCaseRouteMappingsActivity` with the Dapr workflow worker in `Program.cs`; without this, `CaseDeletionProjectionWorkflow` could call an unregistered activity at runtime.
- 2026-07-04: review focused EventStore fallback passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll -class Hexalith.Memories.EventStore.Tests.TenantEventRouterTests -class Hexalith.Memories.EventStore.Tests.RedisAggregateCaseMappingStoreTests` -> 22 total, 0 failed.
- 2026-07-04: review focused Server fallback passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Workflows.CaseDeletionProjectionWorkflowTests -class Hexalith.Memories.Server.Tests.Activities.Cases.DeleteCaseRouteMappingsActivityTests -class Hexalith.Memories.Server.Tests.Activities.Tenants.DeleteTenantDataKeysActivityTests` -> 8 total, 0 failed.
- 2026-07-04: review full solution validation passed: `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` -> 0 warnings, 0 errors; `git diff --check` passed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 21.5 created as the A16/A17 implementation story after Story 21.4 completed.
- Added aggregate-case-map deletion to the shared EventStore mapping store; Redis cleanup scans hash entries and deletes fields whose value equals the deleted case ID in bounded batches while leaving creation-lock keys unchanged.
- Added process-local route invalidation plus persisted-map revalidation on `TenantEventRouter` cache hits, so stale cached routes are not accepted after mappings are removed.
- Added `DeleteCaseRouteMappingsActivity` after projection deletion in `CaseDeletionProjectionWorkflow`; failures surface to workflow retry and successful cleanup invalidates matching in-process routes.
- Registered `DeleteCaseRouteMappingsActivity` with the Dapr workflow worker so the case deletion projection workflow can execute the new cleanup activity at runtime.
- Expanded tenant deletion data-key sweeps to tenant-scoped `eventstore:*`, `embedding-migration:*`, syntactic, raw semantic, current natural-language semantic, and legacy natural-language semantic key families.
- Added focused unit coverage and compiled Redis-backed tenant deletion end-state coverage for aggregate maps, route cache behavior, workflow ordering/retry, tenant scan patterns, and orphan key families.

### File List

- `_bmad-output/implementation-artifacts/21-5-deletion-completeness.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.EventStore/IAggregateCaseMappingStore.cs`
- `src/Hexalith.Memories.EventStore/ITenantEventRouteCacheInvalidator.cs`
- `src/Hexalith.Memories.EventStore/RedisAggregateCaseMappingStore.cs`
- `src/Hexalith.Memories.EventStore/TenantEventRouter.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/DeleteCaseRouteMappingsActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs`
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/Workflows/CaseDeletionProjectionWorkflow.cs`
- `tests/Hexalith.Memories.EventStore.Tests/RedisAggregateCaseMappingStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantDeletionIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Cases/DeleteCaseRouteMappingsActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteTenantDataKeysActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/CaseDeletionProjectionWorkflowTests.cs`

### Change Log

- 2026-07-04: Created story context for deletion completeness covering aggregate-case-map cleanup, router cache stale-route prevention, tenant data-key sweep expansion, and focused end-state validation.
- 2026-07-04: Implemented deletion completeness for case route mappings, event-router stale-cache prevention, tenant data-key sweeps, and focused validation; moved story to review.
- 2026-07-04: Senior developer review fixed missing Dapr workflow activity registration for route-map cleanup, validated focused tests/build, and moved story to done.

## Senior Developer Review (AI)

### Reviewer

Codex GPT-5

### Review Date

2026-07-04

### Outcome

Approved after automatic fix. Story status set to `done`.

### Findings Fixed

- [Critical] `CaseDeletionProjectionWorkflow` invoked `DeleteCaseRouteMappingsActivity`, but the activity was not registered with the Dapr workflow worker. This would let unit tests pass while the runtime workflow failed to dispatch the cleanup activity. Fixed by registering `DeleteCaseRouteMappingsActivity` in `src/Hexalith.Memories.Server/Program.cs`.

### Validation Notes

- Acceptance Criteria 1-2: aggregate-case-map deletion removes matching hash fields by value, route cache entries are invalidated, and cache hits revalidate against persisted mappings before accepting a route.
- Acceptance Criteria 3-4: tenant data-key cleanup remains tenant-scoped, batch-bounded, idempotent for empty key families, and uses `IndexSchemaDefinitions` helpers for memory/vector prefixes.
- Acceptance Criterion 5: focused unit tests cover route-map deletion, stale cache prevention, workflow ordering/retry propagation, and tenant sweep patterns. Redis-backed integration execution is still blocked by Docker socket permissions, but the integration test compiles during full solution build.
- MCP/doc search: no external package/API research was required; repository-pinned Dapr, Redis, and xUnit APIs were validated through existing project references and build/test execution.

### Validation Commands

- `dotnet test tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj --no-restore --filter "FullyQualifiedName~TenantEventRouterTests|FullyQualifiedName~RedisAggregateCaseMappingStoreTests"` -> blocked by sandbox `SocketException (13): Permission denied`.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~CaseDeletionProjectionWorkflowTests|FullyQualifiedName~DeleteCaseRouteMappingsActivityTests|FullyQualifiedName~DeleteTenantDataKeysActivityTests"` -> blocked by sandbox `SocketException (13): Permission denied`.
- `dotnet build tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj -m:1 /nodeReuse:false --no-restore` -> passed.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -> passed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll -class Hexalith.Memories.EventStore.Tests.TenantEventRouterTests -class Hexalith.Memories.EventStore.Tests.RedisAggregateCaseMappingStoreTests` -> 22 total, 0 failed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Workflows.CaseDeletionProjectionWorkflowTests -class Hexalith.Memories.Server.Tests.Activities.Cases.DeleteCaseRouteMappingsActivityTests -class Hexalith.Memories.Server.Tests.Activities.Tenants.DeleteTenantDataKeysActivityTests` -> 8 total, 0 failed.
- `docker info --format '{{.ServerVersion}}'` -> blocked by `/var/run/docker.sock` permission denied.
- `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore` -> passed, 0 warnings, 0 errors.
- `git diff --check` -> passed.
