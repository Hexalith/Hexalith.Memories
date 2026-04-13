# Story 5.1: Tenant Provisioning Workflow

Status: ready-for-dev

## Story

As an operator,
I want to create a tenant with physically separate indexes across all three backends in a single command,
so that each tenant has isolated infrastructure with rollback protection if provisioning fails.

## Acceptance Criteria

1. **Given** a new tenant ID and display name, **When** `TenantProvisioningWorkflow` is started, **Then** it orchestrates: `ProvisionRediSearchActivity` -> `ProvisionRedisVectorActivity` -> `ProvisionFalkorDbActivity` -> `VerifyTenantActivity` **And** RediSearch creates tenant-namespaced indexes (`{tenantId}:memories:idx`) **And** Redis Vector creates tenant-namespaced indexes (`{tenantId}:memories:vec`) **And** FalkorDB creates a dedicated database for the tenant (physical isolation at database level, not label level)
2. **Given** `ProvisionFalkorDbActivity` fails after RediSearch and Redis Vector indexes are created, **When** the workflow handles the failure, **Then** compensation activities delete the successfully created RediSearch and Redis Vector indexes (saga rollback) **And** the tenant is not left in a partially provisioned state **And** the error is reported with details of what failed and what was rolled back
3. **Given** `VerifyTenantActivity` runs after all backends are provisioned, **When** verification completes, **Then** it confirms: all three backend indexes exist, are empty, and are accessible **And** the tenant is marked as active in the tenant registry
4. **Given** a tenant is successfully provisioned, **When** I inspect the provisioning time, **Then** it completes in <5 minutes (single CLI command, per Kenji's journey)
5. **Given** a tenant provisioning request with an already-existing tenant ID, **When** the workflow starts, **Then** it rejects with error code `TENANT_ALREADY_EXISTS` and clear error message before creating any indexes

## Tasks / Subtasks

- [ ] Task 1: Create tenant provisioning contracts (AC: #1, #3, #5)
    - [ ] 1.1 Create `Contracts/V1/TenantStatus.cs` -- sealed enum with values: `Provisioning`, `Active`, `Deleting`, `Failed`, `CompensationFailed`. `CompensationFailed` indicates provisioning failed AND cleanup of orphaned resources also failed -- operator must manually clean up. Apply `[JsonConverter(typeof(CamelCaseStringEnumConverter<TenantStatus>))]` for camelCase JSON serialization (same pattern as `EdgeType`, `EdgeOrigin`)
    - [ ] 1.2 Create `Contracts/V1/TenantInfo.cs` -- sealed record: `string Id` (positional), `string DisplayName` (positional), `TenantStatus Status` (positional), `DateTimeOffset CreatedAt` (positional), `string? EmbeddingProvider` (init, nullable -- from `TenantEmbeddingConfig.Provider`), `string? EmbeddingModel` (init, nullable -- from `TenantEmbeddingConfig.Model`). This is the public-facing tenant representation. Use `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` on optional fields
    - [ ] 1.3 Create `Contracts/V1/TenantProvisioningInput.cs` -- sealed record: `string TenantId` (positional), `string DisplayName` (positional), `int VectorDimensions` (init, default 768 from `EmbeddingProviderDefaults.DefaultDimensions`). Dimensions are resolved at the endpoint before scheduling the workflow -- if the tenant already has an embedding config via `TenantConfigurationActor`, use its dimensions; otherwise default to 768. This keeps provisioning activities dependency-free (no actor/DaprClient injection needed in `ProvisionRedisVectorActivity`). **Dimensions validation:** must be in range 1-4096 (`ArgumentOutOfRangeException.ThrowIfLessThan(dimensions, 1)`, `ArgumentOutOfRangeException.ThrowIfGreaterThan(dimensions, 4096)`) -- range check is more future-proof than an allowlist of known provider dimensions. TenantId must match `TenantIdGuard` regex `^[a-zA-Z0-9\-]+$` AND must not be a reserved name (see Task 1.6)
    - [ ] 1.6 Add reserved tenant ID blocklist to `TenantIdGuard` or as a static helper in `TenantProvisioningInput`: reject tenant IDs `statestore`, `memories`, `dapr`, `system`, `admin`, `default`, `global`. These could collide with Redis key patterns, DAPR internal keys, or cause confusion. Return 400 with message "'{tenantId}' is a reserved name and cannot be used as a tenant ID."
    - [ ] 1.4 Create `Contracts/V1/TenantProvisioningResult.cs` -- sealed record: `string TenantId` (positional), `TenantStatus Status` (positional), `string Message` (positional), `IReadOnlyList<string>? CompensatedBackends` (init, nullable -- populated on failure to list which backends were cleaned up). Use `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` on `CompensatedBackends`
    - [ ] 1.5 Register all new types in `MemoriesJsonContext.cs`: `[JsonSerializable(typeof(TenantStatus))]`, `[JsonSerializable(typeof(TenantInfo))]`, `[JsonSerializable(typeof(TenantProvisioningInput))]`, `[JsonSerializable(typeof(TenantProvisioningResult))]`, `[JsonSerializable(typeof(IReadOnlyList<TenantInfo>))]`

- [ ] Task 1b: Create shared index schema definitions (AC: #1) -- **IMPLEMENTATION FIREBREAK: Complete this task as a separate commit FIRST. Run all existing tests to verify no regressions before proceeding to Tasks 2-11. This refactors proven ingestion code -- verify it still works before building provisioning on top.**
    - [ ] 1b.1 Create `Server/Infrastructure/IndexSchemaDefinitions.cs` -- static class with constants for both RediSearch and Redis Vector index schemas. This is the single source of truth for index field definitions, preventing schema drift between provisioning (Story 5.1) and ingestion (`IndexSyntacticActivity`, `IndexSemanticActivity`). Extract schema definitions from existing `IndexSyntacticActivity` and `IndexSemanticActivity` into this shared class
    - [ ] 1b.2 Refactor `IndexSyntacticActivity` to use `IndexSchemaDefinitions` for its `FT.CREATE` command instead of hardcoded schema
    - [ ] 1b.3 Refactor `IndexSemanticActivity` to use `IndexSchemaDefinitions` for its `FT.CREATE` command instead of hardcoded schema
    - [ ] 1b.4 Run ALL existing tests (824+) -- verify zero regressions from the refactor
    - [ ] 1b.5 Commit: `refactor: Extract shared index schema definitions for provisioning alignment`
    - [ ] 1b.6 Provisioning activities (Tasks 3.1, 3.2) will reference `IndexSchemaDefinitions` for identical schema -- no drift possible

- [ ] Task 2: Create tenant registry service (AC: #3, #5)
    - [ ] 2.1 Create `Server/Tenants/TenantRegistryService.cs` -- sealed partial class (partial for LoggerMessage). This service manages the tenant registry using DAPR state store. Constructor: `DaprClient daprClient`, `ILogger<TenantRegistryService> logger`
    - [ ] 2.2 Methods:
        - `RegisterTenantAsync(string tenantId, string displayName, CancellationToken ct)` -- saves `TenantInfo` with `Status = Provisioning` to DAPR state store. State key: `tenant-registry-{tenantId}`. Returns `TenantInfo`
        - `GetTenantAsync(string tenantId, CancellationToken ct)` -- retrieves `TenantInfo` from DAPR state store. Returns null if not found
        - `UpdateTenantStatusAsync(string tenantId, TenantStatus status, CancellationToken ct)` -- updates the status field on existing tenant entry
        - `ListTenantsAsync(CancellationToken ct)` -- retrieves tenant list from DAPR state store. Use a separate state key `tenant-registry-index` containing `List<string>` of tenant IDs. When registering, add ID to index; when listing, iterate index and load each tenant. This pattern avoids DAPR state store query limitations
        - `TenantExistsAsync(string tenantId, CancellationToken ct)` -- returns bool, used for duplicate detection
        - `RemoveTenantAsync(string tenantId, CancellationToken ct)` -- deletes tenant entry and removes from index (used by compensation on failed provisioning)
    - [ ] 2.3 DAPR state store name: use the existing `"statestore"` component (same Redis-backed state store used by actors and workflows)
    - [ ] 2.4 Add partial logging methods: `LogTenantRegistered(tenantId, displayName)`, `LogTenantStatusUpdated(tenantId, status)`, `LogTenantNotFound(tenantId)`, `LogTenantAlreadyExists(tenantId)`

- [ ] Task 3: Create provisioning activities (AC: #1, #2)
    - [ ] 3.1 Create `Server/Activities/Tenants/ProvisionRediSearchActivity.cs` -- extends `WorkflowActivity<TenantProvisioningInput, bool>`. Creates the RediSearch index `{tenantId}:memories:idx` using schema from `IndexSchemaDefinitions`. Use `NRedisStack` `FT.CREATE` via `SearchCommands`. Inject `IConnectionMultiplexer` (keyed "redis"). Index prefix: `{tenantId}:mu:`. Return `true` on success. If index already exists (RediSearch returns error "Index already exists"), call `FT.INFO` to verify the existing index schema matches `IndexSchemaDefinitions` -- if schema matches, log warning and return `true` (idempotent); if schema differs, throw `InvalidOperationException` with details of the mismatch (prevents silent corruption from stale indexes left by a previous failed provisioning attempt)
    - [ ] 3.2 Create `Server/Activities/Tenants/ProvisionRedisVectorActivity.cs` -- extends `WorkflowActivity<TenantProvisioningInput, bool>`. Creates the Redis Vector index `{tenantId}:memories:vec` using schema from `IndexSchemaDefinitions` with HNSW algorithm, `DIM = input.VectorDimensions`. Dimensions come from `TenantProvisioningInput.VectorDimensions` (resolved at endpoint before workflow scheduling). Inject only `IConnectionMultiplexer` (keyed "redis"). Use `NRedisStack` `FT.CREATE`. Index prefix: `{tenantId}:vec:`. If index already exists, call `FT.INFO` to verify schema matches (especially dimensions) -- if dimensions differ, throw `InvalidOperationException` with current vs expected dimensions; if schema matches, log warning and return `true` (idempotent)
    - [ ] 3.3 Create `Server/Activities/Tenants/ProvisionFalkorDbActivity.cs` -- extends `WorkflowActivity<TenantProvisioningInput, bool>`. Creates a FalkorDB graph for the tenant by executing a write-then-delete query: `CREATE (n:_SystemInit {ts: timestamp()}) WITH n DELETE n RETURN 1`. A write operation is required because FalkorDB may not persist graphs on read-only queries (`RETURN 1` alone may not force graph creation). The write+delete pattern forces graph persistence while leaving the graph empty. The graph ID = `tenantId`. Inject `IConnectionMultiplexer` (keyed "falkordb"). Use `new FalkorDB(db).QueryAsync(tenantId, ...)` pattern. Return `true` on success. If graph already exists, the query is idempotent (creates and deletes a temporary node)
    - [ ] 3.4 Create `Server/Activities/Tenants/VerifyTenantActivity.cs` -- extends `WorkflowActivity<TenantProvisioningInput, bool>`. Verifies all three backends:
        - RediSearch: `FT.INFO {tenantId}:memories:idx` returns without error
        - Redis Vector: `FT.INFO {tenantId}:memories:vec` returns without error
        - FalkorDB: `MATCH (n) RETURN count(n)` on graph `tenantId` returns 0 (empty graph)
        - If any check fails, throw exception with details of which backend failed
        - Inject `IConnectionMultiplexer` (keyed "redis"), `IConnectionMultiplexer` (keyed "falkordb")
    - [ ] 3.5 Create compensation activities in same folder:
        - `DeleteRediSearchIndexActivity.cs` -- extends `WorkflowActivity<TenantProvisioningInput, bool>`. Drops `{tenantId}:memories:idx` via `FT.DROPINDEX`. Swallow "Unknown index" errors (index may not have been created). Return `true`
        - `DeleteRedisVectorIndexActivity.cs` -- extends `WorkflowActivity<TenantProvisioningInput, bool>`. Drops `{tenantId}:memories:vec` via `FT.DROPINDEX`. Swallow "Unknown index" errors. Return `true`
        - `DeleteFalkorDbGraphActivity.cs` -- extends `WorkflowActivity<TenantProvisioningInput, bool>`. Deletes FalkorDB graph: `MATCH (n) DETACH DELETE n` on graph `tenantId`. Swallow graph-not-found `RedisServerException`. Return `true`

- [ ] Task 4: Create TenantProvisioningWorkflow (AC: #1, #2, #3, #5)
    - [ ] 4.1 Create `Server/Workflows/TenantProvisioningWorkflow.cs` -- extends `Workflow<TenantProvisioningInput, TenantProvisioningResult>`
    - [ ] 4.2 Workflow orchestration logic:
        ```
        1. Validate input (TenantId format via TenantIdGuard, DisplayName not empty, reserved name check)
        2. Call InitializeTenantRegistryActivity -- atomically checks existence and registers with Status = Provisioning. If tenant already exists AND status is Failed, reset to Provisioning (allow retry). If tenant exists with any other status, return TENANT_ALREADY_EXISTS error
        3. try:
             await ProvisionRediSearchActivity (with retryOptions)
             await ProvisionRedisVectorActivity (with retryOptions)
             await ProvisionFalkorDbActivity (with retryOptions)
             await VerifyTenantActivity (no retry -- verification is deterministic)
             Update tenant status to Active
             Return success result
           catch (WorkflowTaskFailedException):
             try:
               Compensation: delete created indexes (with compensationRetryOptions)
               Update tenant status to Failed
             catch:
               Update tenant status to CompensationFailed (orphaned resources need manual cleanup)
               Log error with list of orphaned resources
             Return failure result with CompensatedBackends list
        ```
    - [ ] 4.2b **Audit logging:** Each provisioning activity must emit structured log entries via `ILogger` with: `TenantId`, `ActivityName`, `Result` (success/failure), `DurationMs`, `Timestamp`. The workflow itself logs: `ProvisioningStarted`, `ProvisioningCompleted`, `ProvisioningFailed`, `CompensationStarted`, `CompensationCompleted`, `CompensationFailed`. These satisfy Journey 5 (Kenji) audit trail requirement. Use `[LoggerMessage]` source generator pattern consistent with other services
    - [ ] 4.3 Retry policy (same as IngestionWorkflow): `new WorkflowRetryPolicy(maxNumberOfAttempts: 5, firstRetryInterval: TimeSpan.FromSeconds(2), backoffCoefficient: 2.0, maxRetryInterval: TimeSpan.FromMinutes(5))`
    - [ ] 4.4 Compensation retry policy (lighter): `new WorkflowRetryPolicy(maxNumberOfAttempts: 3, firstRetryInterval: TimeSpan.FromSeconds(1), maxRetryInterval: TimeSpan.FromSeconds(30))`
    - [ ] 4.5 **Tenant registry access in workflow:** Workflows must NOT call external services directly -- only activities do I/O. The tenant-exists check and status updates must be activities too. Create:
        - `InitializeTenantRegistryActivity.cs` -- extends `WorkflowActivity<TenantProvisioningInput, TenantInfo>`. Atomically checks if tenant exists AND registers if not found. Uses `TenantRegistryService.GetTenantAsync()` then `TenantRegistryService.RegisterTenantAsync()`. **Idempotency for DAPR replay safety:** if tenant exists with status `Provisioning`, return the existing `TenantInfo` (this is our own in-flight provisioning being replayed after a sidecar restart -- do NOT throw). If tenant exists with status `Failed` or `CompensationFailed`, reset status to `Provisioning` and return (allow retry). If tenant exists with status `Active` or `Deleting`, throw `InvalidOperationException("TENANT_ALREADY_EXISTS")`. If tenant does not exist, register new with `Provisioning` status. This merges existence check + registration into a single activity, eliminates race conditions, and handles DAPR Workflow replay correctly. Returns `TenantInfo`
        - `UpdateTenantStatusActivity.cs` -- extends `WorkflowActivity<TenantStatusUpdateInput, bool>`. Uses `TenantRegistryService.UpdateTenantStatusAsync()`. Needs a simple input record since tuples don't serialize well -- create `Contracts/V1/TenantStatusUpdateInput.cs`: sealed record `string TenantId`, `TenantStatus Status`
        - `RemoveTenantRegistryActivity.cs` -- extends `WorkflowActivity<string, bool>`. Uses `TenantRegistryService.RemoveTenantAsync()`. Used during compensation when provisioning fails entirely
    - [ ] 4.6 Register `TenantStatusUpdateInput` in `MemoriesJsonContext.cs`

- [ ] Task 5: Register workflow, activities, and services in DI (AC: #1)
    - [ ] 5.1 In `Program.cs`, within `AddDaprWorkflow(options => { ... })`:
        - `options.RegisterWorkflow<TenantProvisioningWorkflow>()`
        - `options.RegisterActivity<ProvisionRediSearchActivity>()`
        - `options.RegisterActivity<ProvisionRedisVectorActivity>()`
        - `options.RegisterActivity<ProvisionFalkorDbActivity>()`
        - `options.RegisterActivity<VerifyTenantActivity>()`
        - `options.RegisterActivity<DeleteRediSearchIndexActivity>()`
        - `options.RegisterActivity<DeleteRedisVectorIndexActivity>()`
        - `options.RegisterActivity<DeleteFalkorDbGraphActivity>()`
        - `options.RegisterActivity<InitializeTenantRegistryActivity>()`
        - `options.RegisterActivity<UpdateTenantStatusActivity>()`
        - `options.RegisterActivity<RemoveTenantRegistryActivity>()`
    - [ ] 5.2 Register `TenantRegistryService` as singleton: `builder.Services.AddSingleton<TenantRegistryService>()`

- [ ] Task 6: Add tenant provisioning endpoint (AC: #1, #4, #5)
    - [ ] 6.1 Add `POST /api/tenants` endpoint in `Program.cs` (Minimal API pattern)
    - [ ] 6.2 Request body: `TenantProvisioningInput` (tenantId + displayName)
    - [ ] 6.3 Validate input: `TenantIdGuard.Validate(input.TenantId)` for format, `string.IsNullOrWhiteSpace(input.DisplayName)` returns 400
    - [ ] 6.4 Resolve vector dimensions before scheduling: try to query `TenantConfigurationActor` via `DaprClient` for existing embedding config. If config exists, use its dimensions; if actor call fails (DAPR not ready, actor timeout), catch and default to `EmbeddingProviderDefaults.DefaultDimensions` (768) -- log warning but do not fail the request. Validate dimensions are in range 1-4096 -- return 400 if not. Set `input = input with { VectorDimensions = resolvedDimensions }`
    - [ ] 6.5 Schedule workflow: generate instance ID as `$"provision-{input.TenantId}-{Guid.NewGuid():N}"` to allow retries after failed provisioning (a deterministic ID like `provision-{tenantId}` would prevent retries because DAPR Workflow rejects duplicate instance IDs even for completed/failed workflows). Call `await daprWorkflowClient.ScheduleNewWorkflowAsync(...)`. Wrap in try/catch for `DaprException` -- if DAPR sidecar is unavailable, return `Results.StatusCode(503)` with `ErrorResponse("DAPR_UNAVAILABLE", "DAPR sidecar is not ready.", "Check service health via /healthz and retry.")` rather than letting the exception propagate as a 500
    - [ ] 6.6 Return `Results.Accepted($"/api/tenants/{input.TenantId}/provision-status/{instanceId}", new { workflowInstanceId = instanceId })` -- 202 Accepted because provisioning is async
    - [ ] 6.7 Add `GET /api/tenants/{tenantId}/provision-status/{instanceId}` endpoint to query workflow status via `daprWorkflowClient.GetWorkflowStateAsync(instanceId)`. Return workflow state including output (TenantProvisioningResult) when complete

- [ ] Task 7: Add tenant listing and info endpoints (AC: #3)
    - [ ] 7.1 Add `GET /api/tenants` endpoint -- calls `TenantRegistryService.ListTenantsAsync()`, returns `IReadOnlyList<TenantInfo>`. Returns 200 with array (empty array if no tenants)
    - [ ] 7.2 Add `GET /api/tenants/{tenantId}` endpoint -- calls `TenantRegistryService.GetTenantAsync()`, returns `TenantInfo` or 404 with `ErrorResponse("TENANT_NOT_FOUND", "Tenant '{tenantId}' not found.", "Use GET /api/tenants to list available tenants.")`

- [ ] Task 8: Contract serialization tests (AC: #1, #3)
    - [ ] 8.1 Create `tests/Hexalith.Memories.Contracts.Tests/V1/TenantStatusSerializationTests.cs` -- verify all enum values serialize as camelCase strings ("provisioning", "active", "deleting", "failed", "compensationFailed")
    - [ ] 8.2 Create `tests/Hexalith.Memories.Contracts.Tests/V1/TenantInfoSerializationTests.cs` -- roundtrip JSON test, verify camelCase property names, verify nullable fields omitted when null
    - [ ] 8.3 Create `tests/Hexalith.Memories.Contracts.Tests/V1/TenantProvisioningInputSerializationTests.cs` -- roundtrip test
    - [ ] 8.4 Create `tests/Hexalith.Memories.Contracts.Tests/V1/TenantProvisioningResultSerializationTests.cs` -- roundtrip test, verify `CompensatedBackends` omitted when null, present when populated
    - [ ] 8.5 Create `tests/Hexalith.Memories.Contracts.Tests/V1/TenantStatusUpdateInputSerializationTests.cs` -- roundtrip test

- [ ] Task 9: TenantRegistryService unit tests (AC: #3, #5)
    - [ ] 9.1 Create `tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs`
    - [ ] 9.2 Test: `RegisterTenantAsync_CreatesEntryWithProvisioningStatus` -- mock DaprClient, verify state saved with correct key and Status = Provisioning
    - [ ] 9.3 Test: `GetTenantAsync_ReturnsTenantInfo` -- mock DaprClient with existing state, verify correct TenantInfo returned
    - [ ] 9.4 Test: `GetTenantAsync_NotFound_ReturnsNull` -- mock DaprClient returning null, verify null result
    - [ ] 9.5 Test: `TenantExistsAsync_ExistingTenant_ReturnsTrue` / `TenantExistsAsync_NonExistent_ReturnsFalse`
    - [ ] 9.6 Test: `UpdateTenantStatusAsync_UpdatesStatus` -- verify state saved with new status
    - [ ] 9.7 Test: `ListTenantsAsync_ReturnsAllRegisteredTenants` -- mock index state and individual tenant states
    - [ ] 9.8 Test: `RemoveTenantAsync_DeletesEntryAndUpdatesIndex` -- verify both state key and index updated

- [ ] Task 10: Activity unit tests (AC: #1, #2)
    - [ ] 10.1 Create `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionRediSearchActivityTests.cs` -- mock IConnectionMultiplexer + IDatabase, verify `FT.CREATE` command executed with correct index name and schema. Test idempotency: "Index already exists" error returns true
    - [ ] 10.2 Create `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionRedisVectorActivityTests.cs` -- mock IConnectionMultiplexer, verify `FT.CREATE` with HNSW algorithm and correct dimensions. Test idempotency
    - [ ] 10.3 Create `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/ProvisionFalkorDbActivityTests.cs` -- mock IConnectionMultiplexer, verify graph init query executed on correct graph ID (= tenantId)
    - [ ] 10.4 Create `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/VerifyTenantActivityTests.cs` -- verify all three backend checks. Test failure: one backend check fails, activity throws with details
    - [ ] 10.5 Create `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteRediSearchIndexActivityTests.cs` -- verify `FT.DROPINDEX` called. Test: "Unknown index" error swallowed gracefully
    - [ ] 10.6 Create `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteRedisVectorIndexActivityTests.cs` -- same pattern as 10.5
    - [ ] 10.7 Create `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteFalkorDbGraphActivityTests.cs` -- verify `DETACH DELETE` on correct graph. Test: graph-not-found exception swallowed

- [ ] Task 11: Integration tests (AC: #1, #2, #3, #4, #5)
    - [ ] 11.1 Create `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantProvisioningIntegrationTests.cs`
    - [ ] 11.2 Test: `ProvisionTenant_CreatesAllThreeBackendIndexes` -- POST to `/api/tenants`, poll workflow status, verify RediSearch index exists (`FT.INFO`), Redis Vector index exists (`FT.INFO`), FalkorDB graph accessible. **Latency check:** assert total provisioning completes in <30s (NFR: <5min, integration test uses tighter bound for fast feedback)
    - [ ] 11.3 Test: `ProvisionTenant_DuplicateId_RejectsWithError` -- provision tenant, attempt second provisioning with same ID, verify workflow returns `TENANT_ALREADY_EXISTS` error
    - [ ] 11.4 Test: `ProvisionTenant_InvalidTenantId_Returns400` -- POST with invalid characters (spaces, special chars), verify 400 response
    - [ ] 11.5 Test: `CompensationActivities_CleanUpCreatedResources` -- test compensation activities independently against live backends (clear strategy, no failure injection needed):
        - Create a RediSearch index manually via `FT.CREATE`, then run `DeleteRediSearchIndexActivity`, verify `FT.INFO` returns error (index gone)
        - Create a Redis Vector index manually via `FT.CREATE`, then run `DeleteRedisVectorIndexActivity`, verify `FT.INFO` returns error (index gone)
        - Create a FalkorDB graph via `RETURN 1`, then run `DeleteFalkorDbGraphActivity`, verify graph query throws `RedisServerException` (graph gone)
        - Run each delete activity against a non-existent resource, verify it returns `true` without throwing (idempotent cleanup)
        This validates that compensation activities work correctly in isolation -- the workflow saga logic is verified by unit tests on the workflow orchestration
    - [ ] 11.6 Test: `ListTenants_ReturnsProvisionedTenants` -- provision 2 tenants, call `GET /api/tenants`, verify both appear with Status = Active
    - [ ] 11.7 Test: `GetTenant_NotFound_Returns404` -- call `GET /api/tenants/nonexistent`, verify 404 with `TENANT_NOT_FOUND`
    - [ ] 11.8 Test: `ProvisionedTenant_CanIngestAndSearch` -- provision a new tenant, ingest a memory unit into it, search for it, verify results scoped to that tenant. This is the end-to-end golden path validating that provisioned indexes actually work
    - [ ] 11.9 Test: `GetTenant_DuringProvisioning_ReturnsProvisioningStatus` -- POST to `/api/tenants`, immediately call `GET /api/tenants/{tenantId}` before workflow completes, verify 200 response with `status: "provisioning"` (not 404). Confirms the tenant is visible in the registry from the moment provisioning starts
    - [ ] 11.10 Test: `ProvisionTenant_RetryAfterFailure_Succeeds` -- provision a tenant, manually update its registry status to `Failed` (simulating a failed provisioning), re-provision with the same tenant ID, verify the workflow succeeds and tenant reaches `Active` status. Validates the retry-after-failure path added to `InitializeTenantRegistryActivity`
    - [ ] 11.11 Test: `ProvisionTenant_ReservedName_Returns400` -- POST with reserved names (`statestore`, `dapr`, `system`), verify 400 response with reserved name error message

## Dev Notes

### Implementation Order

Task 1b FIRST (separate commit with test verification firebreak), then Task 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8-11 (tests in parallel). Shared schema refactor first (1b), then contracts (1), registry service (2), activities (3), workflow (4), DI registration (5), endpoints (6, 7), then all tests.

### CompensationFailed Recovery Guidance

When a tenant reaches `CompensationFailed` status, it means provisioning failed AND the cleanup of partially-created backend resources also failed. The operator has orphaned indexes/graphs. Recovery path:
1. Check `TenantProvisioningResult.Message` for the list of orphaned backends
2. Use Story 5.2's `TenantDeletionWorkflow` to clean up orphaned resources (when available)
3. If Story 5.2 is not yet implemented, manually drop indexes via Redis CLI: `FT.DROPINDEX {tenantId}:memories:idx`, `FT.DROPINDEX {tenantId}:memories:vec`, and delete FalkorDB graph
4. After cleanup, re-provision the tenant -- `InitializeTenantRegistryActivity` allows re-provisioning from `CompensationFailed` status

### Architecture Decision: DAPR Workflow for Provisioning (D23)

The architecture mandates DAPR Workflow for all multi-step orchestrations (enforcement guideline). `TenantProvisioningWorkflow` follows the saga/compensation pattern specified in the architecture doc. The workflow is the single source of truth for provisioning state -- the tenant registry is updated by activities within the workflow, not by the API endpoint directly.

### Activities Do I/O, Workflows Orchestrate (Enforcement Rule)

Workflows must NEVER call external services directly. All I/O (Redis commands, FalkorDB queries, DAPR state reads/writes) happens in activities. The workflow only orchestrates activity calls, handles failures, and executes compensation logic. This is a hard architectural enforcement from the 19 enforcement guidelines.

### Persist Actor State Before Every Response (Enforcement Rule)

Not directly relevant to this story (no new actors), but the `TenantConfigurationActor` already exists and persists state via `StateManager`. The provisioning endpoint reads actor state (for embedding dimensions) at the handler level before scheduling the workflow -- dimensions are passed via `TenantProvisioningInput.VectorDimensions`, keeping activities dependency-free.

### Tenant Registry Design

The tenant registry uses DAPR state store (Redis-backed, component name `"statestore"`). State keys:
- `tenant-registry-{tenantId}` -- individual tenant entry (`TenantInfo` JSON)
- `tenant-registry-index` -- list of all tenant IDs (`List<string>` JSON)

This two-key pattern is necessary because DAPR state store does not support query-all semantics on the default Redis state store. The index key is updated atomically when registering/removing tenants.

**Concurrency:** DAPR state store supports ETags for optimistic concurrency on the index key. When updating `tenant-registry-index`:
1. Read current index with `GetStateAsync<List<string>>` -- returns value + ETag
2. Append new tenant ID to the list
3. Write back with `TrySaveStateAsync` passing the received ETag
4. If the write fails (ETag mismatch from concurrent update), retry: re-read the index, verify tenant ID not already present, append, and write again
5. Implement as a compare-and-swap retry loop with max 3 retries

The `InitializeTenantRegistryActivity` atomically checks existence and registers in a single activity call, eliminating the race condition window that existed with separate check + register activities. For the individual tenant key (`tenant-registry-{tenantId}`), use `SaveStateAsync` with `ConsistencyMode.Strong` -- if the key already exists, the tenant was registered by a concurrent workflow and this one should fail gracefully.

### Index Schema Alignment (Shared Constants)

Index schemas are defined in `Server/Infrastructure/IndexSchemaDefinitions.cs` -- the single source of truth. Both provisioning activities (Task 3) and ingestion activities (`IndexSyntacticActivity`, `IndexSemanticActivity`) reference this shared class. This eliminates schema drift entirely.

**RediSearch (`{tenantId}:memories:idx`):**
- Prefix: `{tenantId}:mu:`
- Fields: `tenantId TAG`, `caseId TAG`, `content TEXT`, `contentHash TAG`, `sourceUri TAG`, `sourceType TAG`

**Redis Vector (`{tenantId}:memories:vec`):**
- Prefix: `{tenantId}:vec:`
- Fields: `tenantId TAG`, `caseId TAG`, `embedding VECTOR HNSW { TYPE FLOAT32, DIM {dimensions}, DISTANCE_METRIC COSINE }`

**CRITICAL:** Task 1b extracts these schemas from existing ingestion activities into shared constants. Provisioning activities and ingestion activities both reference `IndexSchemaDefinitions` -- if the schema changes, it changes in one place. When a provisioning activity encounters "Index already exists", it verifies the existing index schema matches via `FT.INFO` before returning success. Schema mismatches (from stale indexes left by previous failed provisioning) throw `InvalidOperationException` with details.

### FalkorDB Graph Initialization

FalkorDB creates graphs implicitly on first query, but only write operations guarantee graph persistence. The provisioning activity executes `CREATE (n:_SystemInit {ts: timestamp()}) WITH n DELETE n RETURN 1` -- a write+delete that forces graph creation while leaving the graph empty. The `RETURN 1` alone (read-only) was rejected: FalkorDB may not persist graphs on read-only queries. The write+delete is idempotent -- on an existing graph, it creates and deletes a temporary node with no side effects.

### Vector Dimensions Resolution

Redis Vector index schema is immutable after creation -- dimensions must be known at provisioning time. Dimensions are resolved at the **endpoint handler** before scheduling the workflow, not inside the activity:

1. The `POST /api/tenants` endpoint checks if `TenantConfigurationActor` already has a config for this tenant ID (via `DaprClient` actor invocation)
2. If config exists, use its dimensions
3. If no config, default to 768 (`EmbeddingProviderDefaults.DefaultDimensions` -- Google `text-embedding-004`)
4. Pass the resolved dimensions in `TenantProvisioningInput.VectorDimensions`

This keeps `ProvisionRedisVectorActivity` dependency-free (only needs `IConnectionMultiplexer`, no `DaprClient`). Dimensions are validated as range 1-4096 (not an allowlist of known provider values) to remain future-proof as new embedding providers are added. The `PUT /api/tenants/{tenantId}/embedding-config` endpoint already validates that changing dimensions requires reindex (409 Conflict), so provisioning with default dimensions is safe. If the actor call fails at the endpoint level, default to 768 and log a warning -- do not fail the provisioning request.

### Async Provisioning Pattern

Provisioning is asynchronous -- the `POST /api/tenants` endpoint schedules the workflow and returns 202 Accepted with a workflow instance ID. The caller polls `GET /api/tenants/{tenantId}/provision-status/{instanceId}` to check completion. This is consistent with the DAPR Workflow pattern used by `IngestionWorkflow`.

Workflow instance ID format: `provision-{tenantId}-{guid}` -- non-deterministic to allow retries after failed provisioning. DAPR Workflow rejects duplicate instance IDs even for completed/failed workflows, so a deterministic ID would prevent retries. The `InitializeTenantRegistryActivity` handles duplicate detection by checking the tenant registry, not the workflow instance ID. If the tenant exists with `Failed` or `CompensationFailed` status, re-provisioning is allowed (status reset to `Provisioning`).

### Compensation Strategy

The saga compensation follows the exact pattern from the architecture doc:

```csharp
// Inside TenantProvisioningWorkflow.RunAsync
var compensationRetryOptions = new WorkflowRetryPolicy(
    maxNumberOfAttempts: 3,
    firstRetryInterval: TimeSpan.FromSeconds(1),
    maxRetryInterval: TimeSpan.FromSeconds(30));

var provisionRetryOptions = new WorkflowRetryPolicy(
    maxNumberOfAttempts: 5,
    firstRetryInterval: TimeSpan.FromSeconds(2),
    backoffCoefficient: 2.0,
    maxRetryInterval: TimeSpan.FromMinutes(5));

List<string> completedBackends = [];
try
{
    await context.CallActivityAsync(nameof(ProvisionRediSearchActivity), input, provisionRetryOptions);
    completedBackends.Add("RediSearch");

    await context.CallActivityAsync(nameof(ProvisionRedisVectorActivity), input, provisionRetryOptions);
    completedBackends.Add("RedisVector");

    await context.CallActivityAsync(nameof(ProvisionFalkorDbActivity), input, provisionRetryOptions);
    completedBackends.Add("FalkorDB");

    await context.CallActivityAsync(nameof(VerifyTenantActivity), input);
    // Update status to Active
    await context.CallActivityAsync(nameof(UpdateTenantStatusActivity),
        new TenantStatusUpdateInput(input.TenantId, TenantStatus.Active));

    return new TenantProvisioningResult(input.TenantId, TenantStatus.Active,
        "Tenant provisioned successfully.");
}
catch (WorkflowTaskFailedException ex)
{
    // Compensate: delete created backends
    try
    {
        if (completedBackends.Contains("RediSearch"))
            await context.CallActivityAsync(nameof(DeleteRediSearchIndexActivity), input, compensationRetryOptions);
        if (completedBackends.Contains("RedisVector"))
            await context.CallActivityAsync(nameof(DeleteRedisVectorIndexActivity), input, compensationRetryOptions);
        if (completedBackends.Contains("FalkorDB"))
            await context.CallActivityAsync(nameof(DeleteFalkorDbGraphActivity), input, compensationRetryOptions);

        // Compensation succeeded -- mark as Failed (retryable)
        await context.CallActivityAsync(nameof(UpdateTenantStatusActivity),
            new TenantStatusUpdateInput(input.TenantId, TenantStatus.Failed), compensationRetryOptions);

        return new TenantProvisioningResult(input.TenantId, TenantStatus.Failed,
            $"Provisioning failed: {ex.FailureDetails?.ErrorMessage}. Cleanup completed.",
            CompensatedBackends: completedBackends);
    }
    catch (WorkflowTaskFailedException compensationEx)
    {
        // Compensation itself failed -- orphaned resources exist
        // Log: which backends were created but NOT cleaned up
        await context.CallActivityAsync(nameof(UpdateTenantStatusActivity),
            new TenantStatusUpdateInput(input.TenantId, TenantStatus.CompensationFailed), compensationRetryOptions);

        return new TenantProvisioningResult(input.TenantId, TenantStatus.CompensationFailed,
            $"Provisioning failed AND cleanup failed. Orphaned resources in: [{string.Join(", ", completedBackends)}]. Manual cleanup required.",
            CompensatedBackends: []);
    }
}
```

### Existing Code to Reuse

| Component | Location | Reuse |
|---|---|---|
| `TenantIdGuard.Validate()` | `Server/Activities/Indexing/TenantIdGuard.cs` | Tenant ID format validation |
| `TenantConfigurationActor` | `Server/Actors/TenantConfigurationActor.cs` | Read embedding config for dimensions (at endpoint level, not in activity) |
| `EmbeddingProviderDefaults` | `Contracts/V1/EmbeddingProviderDefaults.cs` | Default dimensions (768) |
| `TenantEmbeddingConfig` | `Contracts/V1/TenantEmbeddingConfig.cs` | Existing config model |
| `ErrorResponse` | Used in endpoints | Error response pattern (code, message, suggestion) |
| `IngestionWorkflow` | `Server/Workflows/IngestionWorkflow.cs` | Workflow pattern reference |
| `MemoriesJsonContext` | `Contracts/V1/MemoriesJsonContext.cs` | JSON serialization registration |
| `CamelCaseStringEnumConverter<T>` | Contracts V1 | Enum serialization |
| DI registration patterns | `Program.cs` lines 53-128 | Service registration |
| Endpoint patterns | `Program.cs` | Minimal API delegate patterns |

### Anti-Patterns to Avoid

1. **DO NOT create indexes lazily in provisioning** -- the whole point is explicit upfront creation with verification
2. **DO NOT call Redis/FalkorDB directly from the workflow** -- only through activities
3. **DO NOT use static/global state for tenant tracking** -- use DAPR state store (enforcement guideline)
4. **DO NOT assume FalkorDB graph exists** -- handle `RedisServerException` for graph-not-found
5. **DO NOT hardcode index schemas** -- read from existing activity source code to ensure alignment
6. **DO NOT make provisioning synchronous** -- DAPR Workflow handles retry, persistence, and restart tolerance
7. **DO NOT create a new DAPR state store component** -- use existing `"statestore"`
8. **DO NOT add TenantAuthorizationMiddleware** -- deferred to Phase 1.5 (D8). MVP validates tenant ID format only

### Project Structure Notes

New files follow existing feature-based namespace organization:

| Namespace | New Files |
|---|---|
| `Hexalith.Memories.Contracts.V1` | `TenantStatus.cs`, `TenantInfo.cs`, `TenantProvisioningInput.cs`, `TenantProvisioningResult.cs`, `TenantStatusUpdateInput.cs` |
| `Hexalith.Memories.Server.Tenants` | `TenantRegistryService.cs` |
| `Hexalith.Memories.Server.Activities.Tenants` | 9 activity files (provision, verify, delete/compensate, registry) |
| `Hexalith.Memories.Server.Workflows` | `TenantProvisioningWorkflow.cs` |

Tests follow existing structure:
- `Server.Tests/Tenants/` for TenantRegistryService tests
- `Server.Tests/Activities/Tenants/` for activity tests
- `Contracts.Tests/V1/` for serialization tests
- `IntegrationTests/Tenants/` for integration tests

### Git Intelligence

Recent commits follow `feat: <description>` pattern (conventional commits). The last 5 commits:
- `feat: Implement gap detection and confidence promotion for causal chains`
- `feat: Add traversal and annotation models with serialization tests`
- `feat: Implement causal chain traversal feature`
- `feat: Add case-scoped search, cross-case attribution, and metadata filtering`

Code patterns are stable: contracts as sealed records, services as sealed partial classes, activities as individual classes extending `WorkflowActivity<TInput, TOutput>`.

### Previous Story Intelligence

**From Story 4.1 (Causal Chain Traversal):**
- FalkorDB `collect()` result types can vary by NFalkorDB driver version -- parse defensively
- `RedisServerException` for graph-not-found must be caught gracefully
- `Stopwatch.Elapsed.TotalMilliseconds` (not `.Milliseconds`) for telemetry
- Content truncation at 200 chars with word boundary
- New services go in dedicated folders, not extensions of existing services

**From Story 4.2 (Edge Type Filtering):**
- FalkorDB typed variable-length path syntax confirmed working
- Review found: defensive parsing for edge collection shapes is critical

**From Story 3.5 (Deletion):**
- `DETACH DELETE` pattern for node + relationship cleanup in FalkorDB
- Cascade deletion patterns relevant for compensation activities

### Key Files to Create

| File | Purpose |
|---|---|
| `Contracts/V1/TenantStatus.cs` | Enum: Provisioning, Active, Deleting, Failed |
| `Contracts/V1/TenantInfo.cs` | Public tenant representation |
| `Contracts/V1/TenantProvisioningInput.cs` | Workflow input |
| `Contracts/V1/TenantProvisioningResult.cs` | Workflow output |
| `Contracts/V1/TenantStatusUpdateInput.cs` | Status update activity input |
| `Server/Infrastructure/IndexSchemaDefinitions.cs` | Shared index schema constants (single source of truth) |
| `Server/Tenants/TenantRegistryService.cs` | Tenant registry via DAPR state store |
| `Server/Activities/Tenants/ProvisionRediSearchActivity.cs` | Create RediSearch index |
| `Server/Activities/Tenants/ProvisionRedisVectorActivity.cs` | Create Redis Vector index |
| `Server/Activities/Tenants/ProvisionFalkorDbActivity.cs` | Initialize FalkorDB graph |
| `Server/Activities/Tenants/VerifyTenantActivity.cs` | Verify all 3 backends |
| `Server/Activities/Tenants/DeleteRediSearchIndexActivity.cs` | Compensation: drop RediSearch |
| `Server/Activities/Tenants/DeleteRedisVectorIndexActivity.cs` | Compensation: drop Vector |
| `Server/Activities/Tenants/DeleteFalkorDbGraphActivity.cs` | Compensation: delete graph |
| `Server/Activities/Tenants/InitializeTenantRegistryActivity.cs` | Atomic check-and-register in tenant registry |
| `Server/Activities/Tenants/UpdateTenantStatusActivity.cs` | Update tenant status |
| `Server/Activities/Tenants/RemoveTenantRegistryActivity.cs` | Remove from registry |
| `Server/Workflows/TenantProvisioningWorkflow.cs` | Workflow orchestration |

### Key Files to Modify

| File | Change |
|---|---|
| `Contracts/V1/MemoriesJsonContext.cs` | Register all new contract types |
| `Server/Program.cs` | Register workflow, activities, service; add 4 endpoints |
| `Server/Activities/Indexing/IndexSyntacticActivity.cs` | Refactor to use `IndexSchemaDefinitions` |
| `Server/Activities/Indexing/IndexSemanticActivity.cs` | Refactor to use `IndexSchemaDefinitions` |
| `Server/Activities/Indexing/TenantIdGuard.cs` | Add reserved tenant ID blocklist |

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.1] -- AC definitions, saga rollback requirements
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns: Tenant Isolation] -- 4-layer isolation model
- [Source: _bmad-output/planning-artifacts/architecture.md#DAPR Workflows: TenantProvisioningWorkflow] -- Activity sequence and compensation
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Boundaries] -- Index naming: `{tenantId}:memories:idx`, `{tenantId}:memories:vec`, FalkorDB per-tenant database
- [Source: _bmad-output/planning-artifacts/architecture.md#Saga Compensation Pattern] -- Compensation code pattern
- [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines] -- Activities do I/O, workflows orchestrate; DAPR Workflow for all multi-step orchestrations
- [Source: _bmad-output/planning-artifacts/architecture.md#D23] -- DAPR Workflows for tenant provisioning/deletion
- [Source: _bmad-output/planning-artifacts/architecture.md#D8] -- TenantAuthorizationMiddleware deferred to Phase 1.5
- [Source: _bmad-output/planning-artifacts/architecture.md#D15] -- Actor ID format: `{actorType}-{tenantId}`
- [Source: _bmad-output/planning-artifacts/architecture.md#ITenantInfrastructureResolver] -- Single impl MVP, all tenants same instance
- [Source: _bmad-output/planning-artifacts/architecture.md#Technical Constraints] -- Physical tenant isolation, defense-in-depth
- [Source: _bmad-output/planning-artifacts/prd.md#Journey 5: Kenji] -- Provisioning in <5 min, single command
- [Source: _bmad-output/planning-artifacts/prd.md#FR38] -- Create tenant with physically separate indexes
- [Source: _bmad-output/planning-artifacts/prd.md#NFR8] -- Zero cross-tenant data leakage (hard gate)
- [Source: _bmad-output/planning-artifacts/prd.md#NFR12] -- Linear tenant scaling
- [Source: _bmad-output/planning-artifacts/prd.md#Embedding Provider Configuration] -- Per-tenant config, immutable index dimensions
- [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs] -- Workflow pattern reference
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/TenantIdGuard.cs] -- TenantId validation regex
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs] -- RediSearch index schema reference
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs] -- Redis Vector index schema reference
- [Source: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs] -- Existing per-tenant config actor
- [Source: src/Hexalith.Memories.Contracts/V1/EmbeddingProviderDefaults.cs] -- Default dimensions (768)
- [Source: _bmad-output/implementation-artifacts/4-1-causal-chain-traversal.md#Dev Notes] -- FalkorDB access patterns, error handling, review findings

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
