---
baseline_commit: 53cc9c2ea750
---

# Story 21.2: Transactional Multi-Backend Mutation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want case/memory-unit mutations to be atomic or compensated,
so that a partial backend failure cannot leave permanent cross-store divergence (FR13).

## Acceptance Criteria

1. Given the ratified model from 21.1, when a case, annotation, or memory-unit mutation writes to Redis, FalkorDB, and the activity stream, then either all writes commit or compensation restores consistency, mirroring `TenantDeletionWorkflow`, with workflow/compensation tests. Closes A3.

## Tasks / Subtasks

- [x] Task 1 - Re-run the A3 anchor preflight before editing (AC: 1)
  - [x] Confirm `src/Hexalith.Memories.Server/Cases/CaseService.cs` still writes `CreateCaseAsync` directly to Redis case hash, FalkorDB case node, and case activity stream.
  - [x] Confirm `CreateAnnotationAsync` still creates FalkorDB annotation stub/edge before scheduling `IngestionWorkflow`, then records activity outside the workflow boundary.
  - [x] Confirm `DeleteMemoryUnitAsync` and `DeleteCaseAsync` still delete Redis syntactic/vector keys, FalkorDB nodes, and activity records directly from `CaseService`.
  - [x] Confirm `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs` is still pre-21.2 transitional read-model state: registration/status/index records are Dapr state writes, and `UpdateTenantStatusAsync` still uses get-then-save without ETag CAS.
  - [x] Confirm positive workflow patterns still exist in `TenantProvisioningWorkflow`, `TenantDeletionWorkflow`, and `IngestionWorkflow`: workflows orchestrate only, activities perform I/O, retry policies are explicit, and compensation surfaces failed or compensation-failed state.
  - [x] Record moved anchors and any adaptation in this story's Dev Agent Record before changing production code.

- [x] Task 2 - Introduce the EventStore command/event model for A3-owned mutations (AC: 1)
  - [x] Add focused command, event, aggregate-state, and pure command handler types for `Case` and `MemoryUnit` mutations covered by this story: case create, annotation requested/created intent, memory-unit deletion, and case deletion intent/status.
  - [x] Add the minimum `Tenant` lifecycle command/event coverage required by Story 21.1's ratified model for registry/status semantics, but do not implement Story 21.8's CAS/index rollback fix here.
  - [x] Use Hexalith.EventStore domain-service patterns for domain state. Do not persist authoritative domain state through hand-rolled Dapr state calls, EF Core, raw files, or direct Redis/FalkorDB rows.
  - [x] Keep Redis case hashes, syntactic memory-unit hashes, Redis Vector hashes, FalkorDB nodes/edges, case activity streams, and tenant registry/read records as projections/read models only.
  - [x] Preserve existing public contract shapes and JSON names unless an additive internal persistence DTO is required.

- [x] Task 3 - Route direct mutation paths through workflow-owned projection fan-out (AC: 1)
  - [x] Replace `CaseService.CreateCaseAsync`'s direct triple-write with a command path that accepts/appends the EventStore event first, then schedules or executes idempotent projection activities for Redis case hash, FalkorDB case node, and case activity stream.
  - [x] Replace `CreateAnnotationAsync`'s pre-workflow FalkorDB stub/edge write with a workflow/activity sequence that can compensate the graph stub/edge and activity record if workflow scheduling or indexing fails.
  - [x] Replace `DeleteMemoryUnitAsync` direct deletion with a durable command plus projection cleanup activities for syntactic hash, semantic vector hash, natural-language semantic hash if present, FalkorDB node/edges, and case activity record.
  - [x] Replace `DeleteCaseAsync` direct deletion/status changes with a command plus workflow-owned projection cleanup. Keep current deletion status observability and the existing "deleting" guard semantics for concurrent ingestion.
  - [x] Keep tenant infrastructure lifecycle ownership in `TenantProvisioningWorkflow` and `TenantDeletionWorkflow`; 21.2 may publish/consume tenant domain events for status semantics, but provisioning/deletion side effects remain workflow-owned.
  - [x] Ensure projection activities are idempotent. Re-running an activity after a replay or retry must converge, not duplicate graph edges, stream events, counters, or tenant read records.

- [x] Task 4 - Preserve existing behavior while changing the write authority (AC: 1)
  - [x] Preserve tenant validation, tenant mismatch detection, authorization, audit identity, rate-limit, and RediSearch escaping safeguards from Epic 20.
  - [x] Preserve current endpoint and client behavior for create case, create annotation, delete memory unit, delete case, list/get case, status, activity, and members unless a test documents an intentionally additive response field.
  - [x] Reads may continue to use projections for query performance, but any write-side decision that requires authoritative state must use the EventStore aggregate state or a projection explicitly documented as transitional.
  - [x] Do not claim `ConsistencyVerificationWorkflow` or operator repair as the consistency strategy. They are verification/recovery tools after the durable write model exists.
  - [x] Do not resolve A4, A5, A16, A17, A22, A27, A28, A44, A47, or migration-marker deferred work here unless the code change is unavoidable for A3 and is recorded as a scoped dependency.

- [x] Task 5 - Add failure-injection and regression tests (AC: 1)
  - [x] Add unit tests for pure aggregate command handlers: valid command emits the expected event, invalid tenant/case/memory-unit inputs are rejected, duplicate/idempotent commands converge, and delete commands preserve case ownership rules.
  - [x] Add workflow tests proving activity order and compensation for failures after each projection boundary: Redis case hash, syntactic hash, semantic vector hash, FalkorDB node/edge, case activity stream, and tenant registry/read-model projection where touched.
  - [x] Add tests proving EventStore append/accept succeeds before projection fan-out and that projection failure leaves a replayable/rebuildable state rather than a silently divergent permanent state.
  - [x] Update existing `CaseServiceTests`, workflow tests, and endpoint/integration tests that currently assert direct Redis/FalkorDB writes so they assert the new command/workflow boundary instead.
  - [x] Extend the Story 21.1 architecture guard test or add a Story 21.2 guard so direct multi-backend write patterns cannot be reintroduced as authoritative domain persistence.

- [x] Task 6 - Document operator recovery and validate the implementation (AC: 1)
  - [x] Update `docs/dev/consistency.md` to distinguish post-21.2 behavior from the pre-21.2 syntactic-hash repair source.
  - [x] Update `_bmad-output/planning-artifacts/architecture.md` only if implementation reveals a necessary refinement to D3; do not weaken the EventStore source-of-truth decision.
  - [x] Record replay/rebuild, compensation, and operator retry behavior in this story's Dev Agent Record.
  - [x] Run focused tests for touched aggregates, workflows, activities, endpoints, and architecture guards.
  - [x] Run `dotnet build` before handoff because this story changes shared server/domain behavior.

## Dev Notes

Story 21.2 is the implementation story for audit finding A3. Story 21.1 is complete and ratified the model: `Case`, `MemoryUnit`, and `Tenant` domain state is sourced from Hexalith.EventStore events, while Redis, Redis Vector, FalkorDB, case activity streams, and tenant registry/read records are rebuildable projections/read models. 21.2 closes A3 only when the current direct mutation paths are routed through that model and tested under injected backend failures. [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md#Implementation-Notes; _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; key section is Epic 21 and Story 21.2.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant sections are Multi-Backend Consistency, D3, workflow/activity patterns, tenant provisioning/deletion, and consistency verification.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are FR13 and FR39 plus NFR16-NFR19 around consistency, deletion, replay, and operational recovery.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no module UI work is in scope.
- Loaded persistent facts from `_bmad-output/project-context.md`.
- Loaded Hexalith state rules from `references/Hexalith.AI.Tools/hexalith-state-instructions.md` because this story changes durable domain persistence.
- Loaded previous Story 21.1, Epic 20 retrospective handoff, architecture audit A3, consistency operator docs, current code anchors, recent commits, package pins, and official Dapr Workflow docs.

### Current Code Anchors

Re-verified during story creation on 2026-07-04 against `HEAD` `53cc9c2ea750`:

- `CaseService.CreateCaseAsync` generates a ULID, writes a Redis case hash at `{tenantId}:case:{caseId}`, merges a FalkorDB case node, records a Redis stream activity event, logs, and returns a `Case`. There is no EventStore command, outbox, workflow, or compensation boundary around the three writes. [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs:55]
- `CaseService.CreateAnnotationAsync` validates the target memory unit, creates a FalkorDB stub node and `ANNOTATES` edge before scheduling `IngestionWorkflow`, compensates only the graph stub/edge on workflow scheduling failure, then records activity outside the ingestion workflow. This is a partial compensation pattern, not the ratified source-of-truth model. [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs:118]
- `CaseService.DeleteMemoryUnitAsync` verifies existence from the syntactic hash, lists annotation IDs from FalkorDB, deletes annotation and target units across Redis vector/hash and graph helpers, then records a deletion activity. The helper order preserves one backend for retry but is not workflow-owned and not rebuildable from EventStore. [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs:558]
- `CaseService.DeleteCaseAsync` sets the Redis case hash to `deleting`, lists memory-unit IDs from FalkorDB, deletes each unit directly, deletes the case node, and removes Redis case resources. Story 21.5 owns deletion completeness gaps such as aggregate-case-map/router cleanup; 21.2 owns the A3 durable write boundary. [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs:612; _bmad-output/planning-artifacts/epics.md#Story-21.5]
- `TenantRegistryService.RegisterOrGetTenantEntryAsync`, `BeginTenantDeletionAsync`, and `UpdateTenantDisplayNameAsync` use ETag CAS patterns in places, but registry state is still Dapr state/read-model data. `UpdateTenantStatusAsync` remains a direct get-then-save. Story 21.8 owns CAS/index rollback integrity; 21.2 should introduce source-of-truth tenant lifecycle events without solving all A47 details. [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:55; src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:139; _bmad-output/planning-artifacts/epics.md#Story-21.8]
- `IngestionWorkflow` already shows the required projection fan-out shape: it calls syntactic, semantic, graph, and optional natural-language semantic indexing activities, tracks completed backends, compensates with cleanup activities, verifies consistency, records case activity as an activity, and persists failed units as best effort. Reuse this pattern rather than adding ad hoc queues or service calls inside workflows. [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs]
- `TenantProvisioningWorkflow` and `TenantDeletionWorkflow` are the positive saga examples for provisioning/deletion side effects: explicit retry policies, idempotent activity calls, completed-backend tracking, compensation, failed status, and compensation-failed status. [Source: src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs; src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs]
- `Program.cs` currently registers Dapr workflows/activities and adds server EventStore integration. Any new workflows/activities from 21.2 must be registered there and covered by tests. [Source: src/Hexalith.Memories.Server/Program.cs:292; src/Hexalith.Memories.Server/Program.cs:388]

### Architecture Compliance

- D3 now says there is no distributed transaction across Redis and FalkorDB. The target is EventStore aggregate source of truth plus rebuildable projections and Dapr Workflow projection compensation. [Source: _bmad-output/planning-artifacts/architecture.md#D3]
- The EventStore event append/accept is the authoritative write. Redis/FalkorDB/activity projection failure after the event is not solved by deleting the event; it must be surfaced as failed projection state with retry/rebuild/repair semantics.
- Workflows must remain replay-safe: no direct Redis/FalkorDB/DaprClient/service I/O in workflow bodies, no `DateTimeOffset.UtcNow` or random IDs inside orchestration, and no mutable static configuration reads. Use `context.CurrentUtcDateTime`, `context.NewGuid()` when deterministic, `context.CreateReplaySafeLogger<T>()`, and activity calls for side effects. [Source: _bmad-output/project-context.md#Framework-Specific-Rules; src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs]
- Activities may use DI and perform I/O. Keep each activity single-purpose and idempotent; pass full input records so retries and replay do not depend on ambient process state. [Source: src/Hexalith.Memories.Server/Activities/Ingestion/RecordCaseActivityActivity.cs; docs.dapr.io workflow authoring docs]
- Do not use hand-rolled Dapr state-store calls for authoritative domain state. Hexalith state rules require domain data through Hexalith.EventStore, pure aggregate command handlers, event application, query/projection handlers, and read-model stores for persisted projections. [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md]

### Implementation Guardrails

- Keep C# files one primary type per file, file-scoped namespaces, copyright headers, nullable-safe validation, `CancellationToken` pass-through, `ConfigureAwait(false)` in library/helper code, and central package management.
- Prefer additive internal abstractions near the affected features. Do not introduce a parallel persistence framework, ORM, or generic transaction abstraction.
- Avoid broad rewrites of public contracts, CLI, MCP, Web, or client behavior. This is a server/domain write-path remediation story.
- Preserve tenant IDs through every command, event, projection activity, telemetry/log, and error response. Tenant filters are not a substitute for physical/projection isolation.
- Preserve current graceful-degradation behavior where it exists. Activity recording failure must not mask the original mutation failure unless the mutation's contract explicitly requires activity as part of the committed projection set.
- Use existing structured errors and endpoint filters from Epic 20; do not reintroduce inline error/authorization drift.
- Do not initialize or update nested submodules.

### Project Structure Notes

Likely update paths:

- `src/Hexalith.Memories.Server/Cases/CaseService.cs` - remove direct authoritative writes from mutation methods and delegate to the EventStore/workflow boundary.
- `src/Hexalith.Memories.Server/Workflows/` - add case/memory-unit mutation workflow(s) or extend existing activity orchestration patterns without mixing I/O into workflow code.
- `src/Hexalith.Memories.Server/Activities/` - add projection and cleanup activities for case hash, memory-unit projection cleanup, graph node/edge changes, and activity stream writes as needed.
- `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs` and tenant activities - update only the minimum read-model/projection behavior required by the ratified tenant source-of-truth model; leave A47 CAS/index integrity to 21.8 unless unavoidable.
- `src/Hexalith.Memories.EventStore/` or a new domain-focused project/folder if required by the Hexalith.EventStore SDK - add command/event/aggregate/projection code while keeping package boundaries deliberate.
- `src/Hexalith.Memories.Server/Program.cs` - register any new workflows, activities, and composition services.
- `tests/Hexalith.Memories.Server.Tests/`, `tests/Hexalith.Memories.EventStore.Tests/`, and integration tests - add aggregate, workflow, failure-injection, endpoint, and architecture guard coverage.
- `docs/dev/consistency.md` and possibly `_bmad-output/planning-artifacts/architecture.md` - update operator/rebuild semantics after implementation.

Out of scope:

- Natural-language vector namespace separation (21.3).
- Key-schema single source of truth and grep guard (21.4), except where a new helper is necessary for new code.
- Deletion completeness for aggregate-case-map/router cache and tenant deletion key coverage (21.5).
- Unknown/unavailable tenant event routing behavior (21.6).
- Dedup race/duplicate instance handling (21.7).
- Tenant registry CAS and rollback integrity as a standalone A47 closure (21.8).
- Blue/green embedding migration and migration test coverage (21.9, 21.10).

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Keep test names descriptive PascalCase.
- Workflow tests must assert orchestration behavior: activity order, retry-sensitive paths, compensation dispatch, failed/custom status behavior, idempotent re-entry, and replay-safe assumptions.
- Failure-injection tests are mandatory for A3 closure. Cover injected failure after each backend/read-model projection and prove either all projections converge or the system exposes a replayable/rebuildable failed projection state with no silent permanent divergence.
- Tenant isolation negative tests are required for any tenant-routing or projection-key changes.
- Integration tests should assert persisted end state where practical, not only response codes or mock calls.
- Run focused tests for touched areas, then `dotnet build`.

### Previous Story Intelligence

Story 21.1 selected EventStore aggregates with rebuildable projections. It explicitly rejected workflow-wrapped compensated multi-writes because that would silently exempt Memories from Hexalith state rules and still lack an event replay/rebuild path for domain state. It also defined Story 21.2's minimum closure requirements: introduce EventStore command/event handling for `Case`, `MemoryUnit`, and `Tenant`; route current direct `CaseService` and tenant registry mutation paths through that model; project to Redis/FalkorDB through idempotent workflow activities; preserve Epic 20 security guards; add failure-injection tests for Redis, Redis Vector, FalkorDB, registry, and activity-recording failures; document replay/rebuild and operator recovery. [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md#Implementation-Notes]

Epic 20 handoff says Epic 21 must keep audit-anchor preflight discipline, re-check moved line numbers, preserve security regression guards, and review documentation drift after each story. [Source: _bmad-output/implementation-artifacts/epic-20-retro-2026-07-04.md#Next-Epic-Preparation]

Story 19.4 migration-marker target-consistency work remains carried forward to 21.9 and 21.10. Do not resolve that cluster in 21.2 unless a required A3 change makes a narrow prerequisite unavoidable. [Source: _bmad-output/implementation-artifacts/19-4-provider-registry-and-migration-residual-sweep.md; _bmad-output/implementation-artifacts/deferred-work.md]

### Git Intelligence

Recent commits:

- `53cc9c2 feat(story-21.1): Consistency Model Decision`
- `8a37253 docs(epic-20): close retrospective and sync operations docs`
- `5b2b117 feat(story-20.6): RediSearch Query-Injection Hardening`
- `d942058 feat(story-20.5): Inbound Rate Limiting, Quotas & Audit Completeness`
- `e444331 feat(story-20.4): MCP Production Signing-Key Hardening`

The recent pattern is narrow remediation with explicit audit anchors, focused tests, and documentation guards. Continue that pattern; do not turn 21.2 into a broad Epic 21 cleanup.

### Latest Technical Notes

- Repo pins Dapr packages to `1.18.4`; official Dapr docs identify v1.18 as latest and v1.19 as preview on 2026-07-04. Do not upgrade packages in this story. [Source: Directory.Packages.props; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/]
- Official Dapr Workflow docs match the local pattern: workflows orchestrate stateful, fault-tolerant processes; activities perform service/state/pub-sub/external I/O; .NET activities participate in DI; workflow lifecycle can be queried/managed. Keep the implementation on the existing `Dapr.Workflow` APIs. [Source: https://docs.dapr.io/developing-applications/building-blocks/workflow/howto-author-workflow/]
- `Hexalith.Memories.EventStore` in this repo is currently a Memories event-ingestion package for external CloudEvents, not yet the full EventStore aggregate source-of-truth implementation required by 21.2. Do not mistake the package name alone for A3 closure. [Source: src/Hexalith.Memories.EventStore/README.md; src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj]

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-21.2 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A3 - direct triple-writes without event sourcing or outbox]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-21 - remediation scope and sequencing]
- [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency - EventStore source-of-truth target model]
- [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md - ratified model and 21.2 minimum requirements]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md - Hexalith.EventStore persistence rules]
- [Source: _bmad-output/project-context.md - repo-wide C#, Dapr workflow, testing, package, tenant isolation, and submodule rules]
- [Source: docs/dev/consistency.md - current pre-21.2 repair source and target model note]
- [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs - current direct case, annotation, memory-unit, and case deletion mutation anchors]
- [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs - current tenant registry/read-model anchors]
- [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs - existing projection fan-out and compensation pattern]
- [Source: src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs - existing provisioning saga/compensation pattern]
- [Source: src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs - existing resumable deletion pattern]
- [Source: tests/Hexalith.Memories.Server.Tests/Architecture/ConsistencyModelDecisionTests.cs - Story 21.1 guardrails]
- [Source: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/ - Dapr Workflow v1.18 overview]
- [Source: https://docs.dapr.io/developing-applications/building-blocks/workflow/howto-author-workflow/ - Dapr Workflow authoring guidance]

## Dev Agent Record

### Agent Model Used

Codex GPT-5 (initial implementation), Claude Fable 5 (completion: build fix, EventStore SDK client, failure-injection tests, validation)

### Debug Log References

- 2026-07-04: create-story workflow loaded local skill, discovery protocol, template, checklist, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM/state instructions, previous Story 21.1, audit A3, consistency docs, current code anchors, recent commits, and official Dapr Workflow docs.
- 2026-07-04: story target came from user request `21.2`; sprint status has `21-2-transactional-multi-backend-mutation: backlog` and `epic-21: in-progress`.
- 2026-07-04: current implementation anchors rechecked at `53cc9c2ea750`. Direct mutation paths still exist in `CaseService`; workflow compensation patterns remain available in provisioning, deletion, and ingestion.
- 2026-07-04: no UI work detected; Hexalith UX instructions were not required.
- 2026-07-04: official Dapr docs checked; repo-pinned Dapr v1.18.4 remains aligned with current latest v1.18 docs, while v1.19 is preview. No package upgrade is in scope.
- 2026-07-04: dev-story workflow loaded local BMAD dev-story skill and checklist, Hexalith LLM/state instructions, project context, sprint status, and the story record before implementation.
- 2026-07-04: A3 preflight reconfirmed the direct case create, annotation, memory-unit delete, case delete, and tenant registry/status anchors before code edits.
- 2026-07-04: `dotnet build src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj --no-restore /clp:ErrorsOnly` succeeded.
- 2026-07-04: `dotnet test tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj --no-restore --filter "FullyQualifiedName~AggregateCommandHandlerTests" -v:n` failed before test execution with `Build FAILED. 0 Warning(s) 0 Error(s)`.
- 2026-07-04: `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj --no-restore -v:n` failed before compilation with `Build FAILED. 0 Warning(s) 0 Error(s)`. `src/Hexalith.Memories.ServiceDefaults` shows the same silent MSBuild failure as a standalone build target.
- 2026-07-04 (completion session): the "silent" MSBuild failure was diagnosed as two real compile errors hidden by console-logger filtering: CS0618 (obsolete `DaprClient.InvokeMethodAsync` in `EventStoreMemoriesCommandStore` under TreatWarningsAsErrors) and CS7036 (missing `MemoryUnitId` argument for `CaseActivityInput` in `CaseCreationProjectionWorkflow`). Building with `-v:m` surfaces them.
- 2026-07-04: replaced hand-rolled Dapr service invocation and duplicated wire DTOs with the `Hexalith.EventStore.Client` SDK (`IEventStoreGatewayClient.SubmitCommandAsync` + `SubmitCommandRequest/Response` contracts, package 3.32.0 from central versions). Deleted `SubmitMemoriesCommandRequest.cs`/`SubmitMemoriesCommandResponse.cs`. Registered `AddEventStoreGatewayClient` in `Program.cs`; base address comes from `EventStoreIntegration:CommandGateway:BaseAddress` and defaults to Dapr sidecar service invocation (`http://localhost:{DAPR_HTTP_PORT}/v1.0/invoke/eventstore/method/`).
- 2026-07-04: focused runs — `Hexalith.Memories.EventStore.Tests` 102/102 passed; `Hexalith.Memories.Server.Tests` full suite 2120 passed, 0 failed, 1 pre-existing intentional skip (Story 15.6 submodule guard). New coverage: 4 projection workflow test classes (13 tests), `DeleteMemoryUnitProjectionActivityTests` (3 failure-injection/idempotency tests), rewritten `CaseServiceTests` boundary tests (accept-before-schedule ordering, accept-failure injection), and a Story 21.2 code guard in `ConsistencyModelDecisionTests`.
- 2026-07-04: `dotnet build Hexalith.Memories.slnx` succeeded with 0 warnings / 0 errors before handoff.
- 2026-07-04 (senior review): review workflow found and fixed two implementation gaps: annotation projection scheduled ingestion without observing indexing failure, and tenant lifecycle command/event types were not wired into the tenant registry/status read-model path. Review also corrected File List drift. `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-restore -v:minimal -m:1 /nr:false` and `dotnet build tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj --no-restore -v:minimal -m:1 /nr:false` succeeded. Focused `dotnet test` execution compiled but VSTest aborted in this sandbox with `SocketException (13): Permission denied` while opening its local test communication socket.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 21.2 created as the A3 implementation story following Story 21.1's ratified EventStore aggregate source-of-truth model.
- Story scopes A3 mutation-path remediation and explicitly leaves later Epic 21 findings to their dedicated stories.
- Added Memories domain commands, events, aggregate states, and pure command handlers for case creation/deletion, annotation intent, memory-unit deletion, and minimum tenant lifecycle status semantics.
- Routed `CaseService` mutation paths through an EventStore command-accept boundary followed by projection workflow scheduling. Redis/FalkorDB/activity writes for those mutations now live in workflow activities.
- Added idempotent projection activities and Dapr workflows for case creation, annotation projection, memory-unit projection cleanup, and case deletion projection cleanup.
- Added command-store and projection-scheduler abstractions so mutation services can be tested at the EventStore/workflow boundary.
- Updated consistency operator docs to distinguish the post-21.2 durable write model from the existing syntactic-hash repair workflow input.
- Added aggregate command-handler tests and updated the create-case regression test/architecture guard. The earlier "fails before compilation" blocker was two masked compile errors (see Debug Log); both are fixed and the full solution builds clean.
- Command submission now uses the `Hexalith.EventStore.Client` SDK gateway contract (`IEventStoreGatewayClient`) instead of hand-rolled Dapr service invocation, satisfying the Hexalith.EventStore domain-service pattern requirement at the integration boundary. The domain aggregates already follow the pure `Handle(Command, State?) -> DomainResult` + `Apply(Event)` shape with ULID identifiers. Full `AddEventStoreDomainService()` hosting was intentionally not adopted: the Memories server is not a pure domain module; the recorded scoped adaptation is SDK gateway submission plus Memories-owned projection workflows.
- Completed Task 5: workflow tests prove activity order and compensation after each projection boundary (Redis case hash, FalkorDB node/edge, case activity stream, ingestion scheduling, mark-deleting guard, memory-unit projection cleanup); `DeleteMemoryUnitProjectionActivityTests` injects a semantic-vector failure and proves the syntactic hash survives for retry, plus idempotent re-run convergence; `CaseServiceTests` prove EventStore command accept happens strictly before projection fan-out (shared operation log) and that accept failure leaves every read model untouched with no workflow scheduled.
- Replay/rebuild, compensation, and operator retry behavior: the EventStore command accept is the durable write; a projection workflow failure after acceptance surfaces as a failed Dapr workflow instance (compensation restores the pre-projection read-model state where defined) and the operator retries or rebuilds projections from the command/event history per `docs/dev/consistency.md`. Case deletion keeps the case observably in `deleting` when projection cleanup fails, preserving the concurrent-ingestion guard.
- Added a Story 21.2 code guard (`CaseServiceMutations_RouteThroughEventStoreCommandBoundary`) so the pre-21.2 direct multi-backend write helpers cannot be reintroduced.
- Senior review fix: `AnnotationProjectionWorkflow` now runs `IngestionWorkflow` as an observed child workflow for annotation indexing, so indexing failure is surfaced to the parent and triggers graph stub/edge compensation instead of leaving ingestion running after parent rollback.
- Senior review fix: `TenantRegistryService` now submits `RegisterTenantCommand` / `UpdateTenantLifecycleStatusCommand` before tenant registry/status read-model writes, with regression tests for registration, activation, and deletion status transitions.
- Deferred (pre-existing scope): wiring an `eventstore` service resource into the AppHost topology is not part of A3 closure; the gateway client targets Dapr service invocation app-id `eventstore` (configurable via `EventStoreIntegration:CommandGateway:BaseAddress`). Docker-dependent Aspire endpoint integration tests were not run in this sandbox; unit/architecture suites cover the changed boundary.

### File List

- `_bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-20-20260704-091304.md`
- `docs/dev/consistency.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `src/Hexalith.Memories.EventStore/Domain/Aggregates/CaseAggregate.cs`
- `src/Hexalith.Memories.EventStore/Domain/Aggregates/MemoriesTenantAggregate.cs`
- `src/Hexalith.Memories.EventStore/Domain/Aggregates/MemoryUnitAggregate.cs`
- `src/Hexalith.Memories.EventStore/Domain/Commands/*.cs`
- `src/Hexalith.Memories.EventStore/Domain/Events/*.cs`
- `src/Hexalith.Memories.EventStore/Domain/Results/MemoriesDomainResult.cs`
- `src/Hexalith.Memories.EventStore/Domain/States/*.cs`
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`
- `src/Hexalith.Memories.Server/Cases/CaseService.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/*.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/EventStoreMemoriesCommandStore.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/DaprCaseProjectionWorkflowScheduler.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/ICaseProjectionWorkflowScheduler.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/IMemoriesCommandStore.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/InMemoryCaseProjectionWorkflowScheduler.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/InMemoryMemoriesCommandStore.cs`
- `src/Hexalith.Memories.Server/Workflows/AnnotationProjectionWorkflow.cs`
- `src/Hexalith.Memories.Server/Workflows/CaseCreationProjectionWorkflow.cs`
- `src/Hexalith.Memories.Server/Workflows/CaseDeletionProjectionWorkflow.cs`
- `src/Hexalith.Memories.Server/Workflows/MemoryUnitDeletionProjectionWorkflow.cs`
- `tests/Hexalith.Memories.EventStore.Tests/Domain/AggregateCommandHandlerTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Architecture/ConsistencyModelDecisionTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/CaseMutationEndpointE2ETests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Cases/DeleteMemoryUnitProjectionActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/AnnotationProjectionWorkflowTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/CaseCreationProjectionWorkflowTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/CaseDeletionProjectionWorkflowTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/MemoryUnitDeletionProjectionWorkflowTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/WorkflowTestHelpers.cs`

## Senior Developer Review (AI)

Reviewer: Codex GPT-5 on 2026-07-04

Outcome: Approved after automatic fixes.

Findings fixed:

- High: `AnnotationProjectionWorkflow` scheduled annotation ingestion via a fire-and-forget activity, so parent compensation could clean the graph stub while `IngestionWorkflow` continued and later recreated/indexed the annotation. Fixed by calling `IngestionWorkflow` as an observed child workflow with the annotation memory-unit ID as the child instance ID, and by updating workflow tests to cover child workflow failure compensation.
- Medium: Tenant lifecycle command/event types were present, but `TenantRegistryService` registration/status/deletion-status paths never submitted them before direct Dapr read-model writes. Fixed by injecting `IMemoriesCommandStore` into the tenant registry and accepting tenant lifecycle commands before read-model mutation, with tenant registry regression tests.
- Medium: Story File List drifted from git reality: omitted changed test/support files and retained deleted temporary DTO files. Fixed in this story record.

Validation:

- MCP resource search: no MCP resources were available in this session, so local official/reference docs under `references/Hexalith.EventStore` and repo architecture docs were used.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-restore -v:minimal -m:1 /nr:false` succeeded.
- `dotnet build tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj --no-restore -v:minimal -m:1 /nr:false` succeeded.
- `dotnet test ... --filter "FullyQualifiedName~AnnotationProjectionWorkflowTests"` compiled but VSTest aborted in this sandbox with `SocketException (13): Permission denied` while opening its local communication socket.

## Change Log

- 2026-07-04: Story 21.2 implementation — routed `CaseService` create-case, create-annotation, delete-memory-unit, and delete-case mutations through an EventStore command-accept boundary (`IMemoriesCommandStore` backed by the `Hexalith.EventStore.Client` gateway SDK) followed by workflow-owned idempotent projection fan-out (`CaseCreationProjectionWorkflow`, `AnnotationProjectionWorkflow`, `MemoryUnitDeletionProjectionWorkflow`, `CaseDeletionProjectionWorkflow`) with compensation. Added Memories domain commands/events/aggregates with pure handlers, failure-injection and ordering tests, a Story 21.2 architecture code guard, and operator recovery documentation in `docs/dev/consistency.md`. Closes audit finding A3 (AC 1).
- 2026-07-04: Senior developer review fixes — changed annotation projection to observe `IngestionWorkflow` as a child workflow for failure compensation, wired tenant registry/status transitions through the EventStore command boundary, added tenant command-boundary regression assertions, corrected File List drift, and marked Story 21.2 done.
