---
baseline_commit: ae9558f
---

# Story 20.3: Tenant-Scope Workflow & Batch Status Endpoints

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want workflow and batch status endpoints scoped to the caller's tenant,
so that a leaked or guessed instance id cannot expose another tenant's document content.

## Acceptance Criteria

1. Given Story 20.1 Server authentication and Story 20.2 tenant-claim normalization are present, when an authenticated caller requests `GET /api/ingest/{instanceId}`, then the endpoint reads the workflow input tenant, verifies that tenant against the caller's normalized tenant claims, and returns HTTP 403 with structured `TENANT_FORBIDDEN` when the principal is not authorized for the stored tenant.

2. Given `GET /api/ingest/{instanceId}` currently returns the raw Dapr `WorkflowState`, when a status is returned after this story, then the response is an additive public DTO such as `IngestionWorkflowStatus` containing only safe fields (`instanceId`, `tenantId`, `caseId`, runtime status, timestamps, optional memoryUnitId/failure summary) and never raw input, raw output, content bytes, metadata, provider payloads, or `WorkflowState` internals.

3. Given `GET /api/ingest/batches/{batchId}` reads `DirectoryBatchState` from Dapr state and then fans out to workflow state reads, when the stored batch tenant is not authorized for the caller, then the endpoint returns `TENANT_FORBIDDEN` before per-file workflow-state reads and before returning source paths, instance rows, counts, or batch metadata.

4. Given a batch belongs to an authorized tenant, when `GET /api/ingest/batches/{batchId}` succeeds, then it preserves the existing `BatchStatusResponse` public shape and continues to project per-instance workflow status through `DirectoryBatchStatusMapper` rather than returning raw workflow states.

5. Given missing, expired, malformed, or unreadable workflow/batch state can occur, when status endpoints cannot identify a stored tenant, then they fail closed with a structured error or not-found response that does not expose raw Dapr state or document content.

6. Given Epic 20 closes audit finding A6, when implementation finishes, then focused tests prove cross-tenant single-workflow and batch status denial, no raw `WorkflowState` JSON contract leakage, no per-file fan-out before batch tenant authorization, matching-tenant success, missing-state behavior, and continued 20.1/20.2 route authorization guard coverage.

## Tasks / Subtasks

- [x] Task 1 - Re-run the audit-anchor preflight before editing (AC: 1, 2, 3, 6)
  - [x] Confirm `src/Hexalith.Memories.Server/Program.cs` still maps `GET /api/ingest/{instanceId}` at the current raw-state implementation and `GET /api/ingest/batches/{batchId}` at the current batch status implementation.
  - [x] Confirm `TenantAuthorizationMiddleware` still covers only `/api/tenants/{tenantId}` and `/api/search?tenantId=...`, and that body-tenant ingest scheduling uses `TenantAuthorizationEndpointFilter`.
  - [x] Confirm `WorkflowState.ReadInputAs<IngestionInput>()` is available from the pinned `Dapr.Workflow` package before relying on workflow input tenant extraction.
  - [x] Record the preflight result in the Dev Agent Record with the current commit, moved anchors, and any implementation adaptation.

- [x] Task 2 - Add safe single-workflow status projection (AC: 1, 2, 5)
  - [x] Add a public contract record under `src/Hexalith.Memories.Contracts/V1/`, for example `IngestionWorkflowStatus.cs`, with XML docs and safe fields only.
  - [x] Register the new DTO in `MemoriesJsonContext`.
  - [x] Add a Server-side mapper/helper, for example `IngestionWorkflowStatusMapper`, that maps `WorkflowState` to the DTO and reads `IngestionInput` through `ReadInputAs<IngestionInput>()`.
  - [x] Read `IngestionResult` from completed workflow output only to expose safe completion fields such as `MemoryUnitId` and `MemoryUnitStatus`; swallow deserialization failures into a safe degraded/unknown status rather than returning raw serialized output.
  - [x] Do not include `ContentBytes`, raw metadata, source document text, provider payloads, raw custom status, raw exception stacks, or the full `WorkflowState`.

- [x] Task 3 - Tenant-authorize single workflow status (AC: 1, 2, 5, 6)
  - [x] Update `GET /api/ingest/{instanceId}` to accept `HttpContext`, an authorization logger, and cancellation.
  - [x] Fetch workflow state with output/input included as needed, return not found for missing/nonexistent state, and extract `IngestionInput.TenantId`.
  - [x] Call `TenantAuthorizationEndpointFilter.TryAuthorizeTenant(httpContext, storedTenantId, "/api/ingest/{instanceId}", logger, out result)` before returning any projected status.
  - [x] Return the structured denial result unchanged on mismatch, missing tenant, malformed tenant, unauthenticated principal, or unreadable tenant input.
  - [x] Preserve Story 20.1 bearer auth and Story 20.2 normalized tenant-claim behavior; do not add a parallel auth scheme or tenant-claim parser.

- [x] Task 4 - Tenant-authorize batch status before fan-out (AC: 3, 4, 5, 6)
  - [x] Update `GET /api/ingest/batches/{batchId}` to accept `HttpContext`, an authorization logger, and cancellation.
  - [x] Load the `DirectoryBatchState` only to identify ownership, then authorize `state.TenantId` before building `BatchInstanceStatus` tasks.
  - [x] On unauthorized batch tenant, return `TENANT_FORBIDDEN` before `workflowClient.GetWorkflowStateAsync(file.InstanceId)` is called for any file.
  - [x] Preserve `BatchStatusResponse`, `BatchStatusCounts`, and `BatchInstanceStatus` wire shape for authorized callers unless an additive field is strictly required.
  - [x] Keep the existing bounded fan-out gate or make it more conservative; do not create unbounded workflow-state reads.

- [x] Task 5 - Add focused tests and contract guards (AC: 1-6)
  - [x] Add mapper tests for null/nonexistent workflow state, unreadable input, running state, completed indexed output, completed failed output, and output deserialization failure.
  - [x] Add endpoint tests proving `GET /api/ingest/{instanceId}` with tenant A token cannot read tenant B status and returns `TENANT_FORBIDDEN` without raw `WorkflowState` fields.
  - [x] Add endpoint tests proving matching tenant can read projected single-workflow status.
  - [x] Add endpoint tests proving `GET /api/ingest/batches/{batchId}` denies mismatched tenant before workflow fan-out and matching tenant preserves `BatchStatusResponse`.
  - [x] Add a contract serialization test for the new `IngestionWorkflowStatus` DTO under `tests/Hexalith.Memories.Contracts.Tests/V1/` if the existing reflection sweep does not pick it up automatically.
  - [x] Keep `ServerEndpointAuthorizationTests.ApiRoutes_DoNotCarryAnonymousMetadata`, `AnonymousRoutes_AreLimitedToNamedInfrastructureAndDaprActorRuntime`, and the Story 20.2 tenant authorization tests passing.

- [x] Task 6 - Validate and document completion (AC: 1-6)
  - [x] Run focused Server ingestion/status/auth tests and contract serialization tests.
  - [x] Run `dotnet build Hexalith.Memories.slnx` or document the exact blocker.
  - [x] Run `git diff --check -- src tests _bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md`.
  - [x] Update this story's Dev Agent Record with commands, outcomes, changed files, and any validation blockers.

## Dev Notes

This story is implementation scope and closes audit finding A6. It builds on 20.1's fallback bearer authentication and 20.2's normalized tenant claims; it must not reopen authentication foundation, tenant-claim parsing, principal-derived audit identity, MCP signing-key hardening, rate limiting, RediSearch escaping, or Program.cs decomposition. Those are Stories 20.1, 20.2, 20.4, 20.5, 20.6, and Epic 25 respectively. [Source: _bmad-output/planning-artifacts/epics.md#Story-20.3; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A6]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; key section is Story 20.3 under Epic 20 API Security & Tenant Authorization.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; key rules are tenant isolation, Dapr Workflow ownership of durable orchestration, and API/CLI status surface constraints.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; key requirements are FR10 ingestion status, FR44 tenant enforcement, NFR8 zero cross-tenant leakage, and NFR19 failure visibility.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no UI work is in scope, but future status surfaces rely on safe ingestion status descriptors.
- Loaded persistent facts from `_bmad-output/project-context.md`.
- Loaded previous story `_bmad-output/implementation-artifacts/20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md`.

### Audit-Anchor Preflight

Re-verified on 2026-07-04 against current `HEAD` `ae9558f` plus the dirty working tree:

- `GET /api/ingest/{instanceId}` still returns raw `WorkflowState` directly: `WorkflowState? state = await workflowClient.GetWorkflowStateAsync(instanceId); return state is null ? Results.NotFound() : Results.Ok(state);`. [Source: src/Hexalith.Memories.Server/Program.cs:512-516]
- `GET /api/ingest/batches/{batchId}` loads `DirectoryBatchState`, then starts per-file `GetWorkflowStateAsync(file.InstanceId)` tasks, then returns `BatchStatusResponse`. It does not authorize `state.TenantId` first. [Source: src/Hexalith.Memories.Server/Program.cs:766-833]
- The audit's original `Program.cs:488-492,740-807` anchors moved after Story 20.2. Use current anchors above, not the stale audit line numbers. [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A6]
- Story 20.2 added `TenantAuthorizationMiddleware` after `UseAuthorization()`, but the middleware only extracts tenants from `/api/tenants/{tenantId}` and `/api/search?tenantId=...`. It does not cover `/api/ingest/{instanceId}` or `/api/ingest/batches/{batchId}` because those routes carry no tenant in path/query. [Source: src/Hexalith.Memories.Server/Program.cs:375-377; src/Hexalith.Memories.Server/Authentication/TenantAuthorizationMiddleware.cs:27-42]
- Story 20.2 added `TenantAuthorizationEndpointFilter.TryAuthorizeTenant(...)`, which can be reused after the stored workflow/batch tenant is known. It returns structured `TENANT_FORBIDDEN` and snapshots the authorized tenant in `HttpContext.Items`. [Source: src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs:32-81,110-132]
- `Dapr.Workflow` is pinned to 1.18.4 and local package XML confirms `WorkflowState.ReadInputAs<T>()` exists. [Source: references/Hexalith.Builds/Props/Directory.Packages.props:130; /home/administrator/.nuget/packages/dapr.workflow/1.18.4/lib/net10.0/Dapr.Workflow.xml:1789]

If any anchor moves before dev starts, update this section first. Epics 20-26 require current-code re-verification before implementation. [Source: _bmad-output/planning-artifacts/epics.md#Phase-Post-MVP-Audit-Remediation]

### Existing Patterns to Reuse

- Reuse `TenantAuthorizationEndpointFilter.TryAuthorizeTenant(...)` for stored-tenant checks after reading the workflow/batch owner. Do not duplicate tenant-claim parsing. [Source: src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs]
- Reuse `ServerTenantClaimsTransformation` and `ServerTestBearerToken.Create(tenants: [...])` for tests. [Source: src/Hexalith.Memories.Server/Authentication/ServerTenantClaimsTransformation.cs; tests/Hexalith.Memories.Server.Tests/Authentication/ServerTestBearerToken.cs]
- Reuse `DirectoryBatchStatusMapper` for authorized batch per-instance projections; extend it only if single-workflow status can share safe status mapping. [Source: src/Hexalith.Memories.Server/Ingestion/DirectoryBatchStatusMapper.cs]
- Follow `ConsistencyWorkflowService` for projecting workflow state into public DTOs and swallowing custom-status/output deserialization errors safely. [Source: src/Hexalith.Memories.Server/Consistency/ConsistencyWorkflowService.cs:71-114]
- Use `EventStoreWebAppFactory` rather than creating a new host fixture. Extend it only if tests need a controllable workflow-client seam. [Source: tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs]

### Architecture and Security Constraints

- Tenant isolation is a hard gate. Status endpoints must not return another tenant's document paths, content bytes, metadata, workflow output, workflow custom status, or raw provider exceptions. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules; _bmad-output/planning-artifacts/prd.md#NFR8]
- A stored tenant must be identified from durable state before any status body is returned. If the tenant cannot be identified, fail closed instead of returning raw state.
- It is acceptable for the endpoint to perform the minimal state lookup needed to discover ownership because the current routes have no tenant path segment. It is not acceptable to return or fan out status details before authorization.
- Use `ErrorResponse` for structured HTTP errors. Keep response/log text sanitized; do not echo bearer tokens, raw JWT payloads, raw workflow serialized content, content bytes, or local file paths in forbidden/error responses.
- Keep changes scoped. Do not introduce route groups, shared route tables, broad endpoint factorization, CLI status implementation, MCP tooling, Web UI, or route versioning in this story.
- No new package should be needed. If a dependency is unavoidable, add a versionless `PackageReference` and use central package management.

### File Structure Guidance

Expected production files:

- `src/Hexalith.Memories.Contracts/V1/IngestionWorkflowStatus.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` (update)
- `src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowStatusMapper.cs` or equivalent (new)
- `src/Hexalith.Memories.Server/Ingestion/DirectoryBatchStatusMapper.cs` (update only if reuse is needed)
- `src/Hexalith.Memories.Server/Program.cs` (update the two GET status endpoints only)

Expected test files:

- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionWorkflowStatusMapperTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` or a focused `IngestionStatusEndpointAuthorizationTests.cs` (update/new)
- `tests/Hexalith.Memories.Server.Tests/Ingestion/DirectoryBatchStatusMapperTests.cs` (update only for mapper behavior changes)
- `tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs` or the equivalent contract reflection sweep (update if needed)
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs` (update only if a workflow-client seam is needed)

### Testing Standards

Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`. Test names should be behavior-focused PascalCase. [Source: _bmad-output/project-context.md#Testing-Rules]

Minimum focused validation:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Ingestion.IngestionWorkflowStatusMapperTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj --disable-build-servers -m:1 /nr:false
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false
git diff --check -- src tests _bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md
```

If `dotnet test` hits the known sandbox/VSTest TCP listener limitation, use the built xUnit v3 executable pattern and record the exact fallback command. [Source: _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md#Testing-Standards]

### Scope Boundaries

- Do not change scheduling endpoints beyond whatever accepted status location or DTO links are required.
- Do not add tenant query requirements unless implementation proves the no-query path cannot be made safe; if a query parameter is introduced, preserve a clear compatibility/deprecation note.
- Do not implement rate limiting, audit completeness, Redis query escaping, MCP production signing-key hardening, workflow claim-check payloads, directory batch scalability, or Program.cs route factorization.
- Do not return raw `WorkflowState`, raw `DirectoryBatchState`, raw `IngestionInput`, raw `IngestionResult`, serialized custom status, local stack traces, or document content.
- Do not initialize or update nested submodules.

### Previous Story Intelligence

Story 20.2 completed tenant authorization for tenant path routes, `/api/search`, and body-tenant ingest scheduling. Carry these learnings into 20.3:

- The Server now normalizes configured `tenant_id`, `tenants`, `tid`, and `tenant` claims into `memories:tenant`; reuse this normalized claim instead of parsing original claims again.
- Cross-tenant denials should happen before avoidable downstream dependencies and should return structured `TENANT_FORBIDDEN`.
- `ServerEndpointAuthorizationTests` already proves anonymous API access fails, narrow infrastructure routes remain anonymous, route metadata has no anonymous `/api/**`, and body-tenant scheduling rejects mismatches.
- `EventStoreWebAppFactory` has fakes for DaprClient, actors, Redis, FalkorDB, preflight dedup, and audit logs, but not yet an obvious fake for `DaprWorkflowClient`. Prefer adding a narrow status service seam if endpoint tests cannot control `DaprWorkflowClient` directly.

[Source: _bmad-output/implementation-artifacts/20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md#Completion-Notes-List; git commit `ae9558f`]

### Git Intelligence

Recent commits show Epic 20 is in active implementation:

- `ae9558f feat(story-20.2): Tenant Authorization Filter & Principal-Derived Audit Identity` added Server tenant authorization middleware/filter, normalized claims, and tests.
- `b48a519 feat(story-20.1): Server Authentication Foundation` added bearer authentication, fallback authorization, anonymous route guardrails, and Server auth tests.
- `416882c Add orchestration state document, complexity data, and policy snapshot for Epic 20` and `c3c2dfe feat: add preflight complexity and snapshot files for Epics 20-26` added Epic 20-26 orchestration metadata.

### Project Structure Notes

This story should touch contracts, Server ingestion/status mapping, the two status endpoints in `Program.cs`, and focused tests. It should not alter MCP, CLI, Web UI, storage schemas, Dapr component files, or route topology beyond the two endpoint handlers.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-20.3 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A6 - raw workflow/batch status leak]
- [Source: _bmad-output/planning-artifacts/prd.md#FR10-FR44-NFR8-NFR19 - ingestion status, tenant enforcement, zero leakage, failure visibility]
- [Source: _bmad-output/planning-artifacts/architecture.md#Dapr-Workflow - durable orchestration/status ownership]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Ingestion-status - future status descriptor expectations]
- [Source: _bmad-output/project-context.md - C#, testing, package, tenant isolation, and submodule rules]
- [Source: _bmad-output/implementation-artifacts/20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md - previous story implementation and review learnings]
- [Source: src/Hexalith.Memories.Server/Program.cs:512-516,766-833 - current vulnerable status endpoints]
- [Source: src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs - reusable authorization helper]
- [Source: src/Hexalith.Memories.Server/Ingestion/DirectoryBatchStatusMapper.cs - existing batch projection]
- [Source: src/Hexalith.Memories.Contracts/V1/BatchStatusResponse.cs - existing batch response contract]
- [Source: /home/administrator/.nuget/packages/dapr.workflow/1.18.4/lib/net10.0/Dapr.Workflow.xml:1789 - `WorkflowState.ReadInputAs<T>()` availability]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-04 Task 1 preflight: current commit `ae9558fe1d8a4912779782aca8194e56e724de26`; `Program.cs` still maps `GET /api/ingest/{instanceId}` at lines 512-516 returning raw `WorkflowState`, and batch status at lines 766-833 still fans out workflow reads before tenant authorization. `TenantAuthorizationMiddleware` still extracts tenants only from `/api/tenants/{tenantId}` and `/api/search?tenantId=...`; body-tenant ingest scheduling remains covered by `TenantAuthorizationEndpointFilter`. `WorkflowState.ReadInputAs<T>()` is present in Dapr Workflow 1.18.4 XML. Adaptation: use current `Program.cs` anchors 512-516 and 766-833, not stale audit anchors.
- 2026-07-04 RED phase: added focused mapper, endpoint authorization, and contract serialization tests; initial builds failed on missing `IngestionWorkflowStatus`, `IngestionWorkflowStatusMapper`, and workflow-state reader seam as expected.
- 2026-07-04 validation: `dotnet test Hexalith.Memories.slnx --no-build --disable-build-servers -m:1 /nr:false` is blocked by sandbox `SocketException (13) Permission denied` from VSTest TCP listener startup.
- 2026-07-04 xUnit fallback: full built-assembly run passed Contracts (598), EventStore (94), MCP (83), Server (2011, 1 skipped), and Web (475). Benchmarks and Integration tests are blocked by unavailable Docker/Testcontainers and local TCP listener restrictions; CLI full run has two sandbox port-listener failures, with catalog drift fixed and focused CLI catalog tests passing.
- 2026-07-04 final gates: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` passed with 0 warnings/errors; `git diff --check -- src tests _bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md` passed; no unchecked story tasks remain.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added safe `IngestionWorkflowStatus` public DTO and JSON source-generation registration.
- Replaced raw single-workflow status response with tenant-first authorization and safe status projection.
- Added `IIngestionWorkflowStateReader` seam for status reads, backed by `DaprIngestionWorkflowStateReader`, and reused it in tests.
- Authorized batch status against `DirectoryBatchState.TenantId` before per-file workflow fan-out while preserving `BatchStatusResponse`.
- Added focused mapper, endpoint, and contract tests for cross-tenant denial, matching-tenant success, missing/unreadable state, output deserialization degradation, no raw workflow JSON leakage, and batch fan-out prevention.
- Added CLI error-catalog entries for the new server structured errors (`TENANT_FORBIDDEN`, `INGESTION_STATUS_NOT_FOUND`, `INGESTION_STATUS_UNREADABLE`) to keep drift guards green.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-04

Outcome: Approved after automatic fixes. Status moved to `done`.

Findings fixed:

- [HIGH] `GET /api/ingest/{instanceId}` did not handle `IIngestionWorkflowStateReader.GetWorkflowStateAsync(...)` failures. An unreadable Dapr workflow state could bypass the story's fail-closed structured/not-found behavior and surface as an unstructured server failure. Fixed by catching non-cancellation state-read exceptions and returning the existing structured `INGESTION_STATUS_NOT_FOUND` path. Added `SingleWorkflowStatus_WhenWorkflowStateCannotBeRead_ReturnsStructuredNotFound`.
- [MEDIUM] Changed source/test lines included line-ending churn that caused `git diff --check -- src tests _bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md` to fail under the current repository git whitespace settings. Normalized the changed files so the whitespace gate passes.

Validation:

- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` passed.
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj --disable-build-servers -m:1 /nr:false` passed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Ingestion.IngestionWorkflowStatusMapperTests -class Hexalith.Memories.Server.Tests.Authentication.IngestionStatusEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` passed: 41 tests.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.IngestionWorkflowStatusSerializationTests` passed: 2 tests.
- `git diff --check -- src tests _bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md` passed.

### File List

- `_bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs`
- `src/Hexalith.Memories.Contracts/V1/IngestionWorkflowStatus.cs`
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowStateReader.cs`
- `src/Hexalith.Memories.Server/Ingestion/IIngestionWorkflowStateReader.cs`
- `src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowStatusMapper.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/IngestionWorkflowStatusSerializationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/IngestionStatusEndpointAuthorizationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionWorkflowStatusMapperTests.cs`

### Change Log

- 2026-07-04: Implemented tenant-scoped workflow and batch status endpoints, safe single-workflow DTO projection, focused tests, and CLI structured-error catalog coverage for new status errors.
- 2026-07-04: Senior developer review auto-fixed unreadable single-workflow state handling, added endpoint regression coverage, normalized changed-file whitespace, and marked the story done.
