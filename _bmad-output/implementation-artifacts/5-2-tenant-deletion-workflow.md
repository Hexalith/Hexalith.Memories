# Story 5.2: Tenant Deletion Workflow

Status: ready-for-dev

## Story

As an operator,
I want to delete a tenant and all its data across all backends,
so that I can fulfill erasure requirements and reclaim resources.

## Acceptance Criteria

1. **Given** a tenant with memory units, cases, and graph data
   **When** `TenantDeletionWorkflow` is started
   **Then** it orchestrates: `DeleteRediSearchActivity` -> `DeleteRedisVectorActivity` -> `DeleteFalkorDbActivity`
   **And** all RediSearch indexes for the tenant are dropped
   **And** all Redis Vector indexes for the tenant are dropped
   **And** the FalkorDB database for the tenant is deleted

2. **Given** a large tenant with many graph nodes
   **When** `DeleteFalkorDbActivity` executes
   **Then** deletion is batched (N nodes per activity invocation, yield between batches)
   **And** batched deletion does not block other tenants' graph queries

3. **Given** a deletion is in progress
   **When** a search or ingestion request targets the deleting tenant
   **Then** the request is rejected with error code `TENANT_DELETING`

4. **Given** the tenant deletion completes
   **When** I list tenants
   **Then** the deleted tenant no longer appears
   **And** any search across all axes returns zero results for the deleted tenant ID

## Tasks / Subtasks

> **PREREQUISITE GATE:** Story 5-1 (Tenant Provisioning Workflow) MUST be complete before starting this story. Verify these components exist: `TenantStatus.cs`, `TenantInfo.cs`, `TenantRegistryService.cs`, `UpdateTenantStatusActivity.cs`, `RemoveTenantRegistryActivity.cs`, `IndexSchemaDefinitions.cs`. If ANY are missing, STOP and implement Story 5-1 first.

### Task 1: Create tenant deletion contracts (AC #1, #3, #4)

- [ ] 1.1 `Contracts/V1/TenantDeletionInput.cs` -- sealed record: `TenantId` (positional). Validate via `TenantIdGuard.Validate()`.
- [ ] 1.2 `Contracts/V1/TenantDeletionResult.cs` -- sealed record: `TenantId` (positional), `Status` (positional, `TenantStatus`), `Message` (positional), `DeletedBackends` (init, `IReadOnlyList<string>?`, nullable).
- [ ] 1.3 `Contracts/V1/BatchedGraphDeletionInput.cs` -- sealed record: `TenantId` (positional), `BatchSize` (positional, int, default 500), `BatchNumber` (positional, int).
- [ ] 1.4 `Contracts/V1/BatchedGraphDeletionResult.cs` -- sealed record: `RemainingNodes` (positional, long), `DeletedInBatch` (positional, int), `IsComplete` (positional, bool).
- [ ] 1.5 Register all new types in `MemoriesJsonContext.cs` with `[JsonSerializable(...)]` attributes.

### Task 2: Create deletion activities (AC #1, #2)

> **REUSE:** Story 5-1 creates `DeleteRediSearchIndexActivity`, `DeleteRedisVectorIndexActivity`, and `DeleteFalkorDbGraphActivity` as compensation activities. These handle single-index drops. Story 5-2 extends these with batched graph deletion for large tenants.

- [ ] 2.1 `Server/Activities/Tenants/DeleteRediSearchActivity.cs` -- extends `WorkflowActivity<TenantDeletionInput, bool>`. Drops `{tenantId}:memories:idx` via `FT.DROPINDEX {tenantId}:memories:idx DD` (the `DD` flag deletes associated document hashes under `{tenantId}:mu:*` prefix). Swallow "Unknown index" `RedisServerException`. Inject `IConnectionMultiplexer` (keyed `"redis"`). Idempotent: success even if index already gone.
- [ ] 2.2 `Server/Activities/Tenants/DeleteRedisVectorActivity.cs` -- extends `WorkflowActivity<TenantDeletionInput, bool>`. Drops `{tenantId}:memories:vec` via `FT.DROPINDEX {tenantId}:memories:vec DD` (the `DD` flag deletes associated vector hashes under `{tenantId}:vec:*` prefix). Same idempotency as 2.1.
- [ ] 2.3 `Server/Activities/Tenants/DeleteFalkorDbBatchActivity.cs` -- extends `WorkflowActivity<BatchedGraphDeletionInput, BatchedGraphDeletionResult>`. Executes batched node deletion on the tenant's FalkorDB graph:
  - Count remaining nodes: `MATCH (n) RETURN count(n)` on graph `{tenantId}`
  - If count == 0, return `IsComplete = true`
  - Delete batch: `MATCH (n) WITH n LIMIT $batchSize DETACH DELETE n RETURN count(n)` (parameterized via `IGraphQueryBuilder`)
  - Return `BatchedGraphDeletionResult` with remaining count and batch stats
  - Inject `IConnectionMultiplexer` (keyed `"falkordb"`)
  - Handle `RedisServerException` for graph-not-found gracefully (return `IsComplete = true`)
- [ ] 2.4 `Server/Activities/Tenants/DeleteFalkorDbGraphFinalizerActivity.cs` -- extends `WorkflowActivity<TenantDeletionInput, bool>`. Deletes the empty FalkorDB graph itself after all nodes are batched out. Executes `GRAPH.DELETE {tenantId}`. Swallow graph-not-found errors. Idempotent.
- [ ] 2.5 `Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs` -- extends `WorkflowActivity<TenantDeletionInput, bool>`. Deletes remaining Redis data keys NOT covered by `FT.DROPINDEX DD` (which handles `mu:*` and `vec:*` hashes):
  - Scan for `{tenantId}:case:*` keys (case hashes, members lists, activity logs) via `SCAN` with `COUNT 1000`
  - Scan for `{tenantId}:dedup:*` keys (dedup entries) via `SCAN` with `COUNT 1000`
  - Delete in batches of 1000 via `KeyDeleteAsync`
  - Return true when all scans complete with zero remaining keys
  - Inject `IConnectionMultiplexer` (keyed `"redis"`)

### Task 3: Add graph query builder methods for batched deletion (AC #2)

- [ ] 3.1 Add to `IGraphQueryBuilder`:
  - `BuildCountAllNodes()` -- returns `(string Query, IDictionary<string, object> Parameters)` for `MATCH (n) RETURN count(n)`
  - `BuildBatchDeleteNodes(int batchSize)` -- returns parameterized query: `MATCH (n) WITH n LIMIT $batchSize DETACH DELETE n RETURN count(n)`
- [ ] 3.2 Implement in `GraphQueryBuilder` with parameterized queries (no raw string interpolation for batchSize -- use `$batchSize` parameter).

### Task 4: Create TenantDeletionWorkflow (AC #1, #2, #3, #4)

- [ ] 4.1 Create `Server/Workflows/TenantDeletionWorkflow.cs` -- extends `Workflow<TenantDeletionInput, TenantDeletionResult>`.
- [ ] 4.2 Orchestration logic:
  1. Validate input via `TenantIdGuard.Validate()`
  2. Call `GetTenantRegistryActivity` -- retrieve `TenantInfo`. If not found, return error result with `TENANT_NOT_FOUND`. If status is `Deleting`, log idempotent re-entry and continue from step 4 (DAPR replay safety -- re-executes all cleanup activities; already-completed activities return immediately since they are idempotent). If status is `Provisioning`, return error `TENANT_PROVISIONING`.
  3. Call `UpdateTenantStatusActivity` -- set status to `TenantStatus.Deleting` (prevents concurrent operations)
  4. Sequential backend cleanup (intentionally sequential, not parallel -- simpler to reason about for MVP; RediSearch and RedisVector drops could be parallelized as a future optimization):
     - Call `DeleteRediSearchActivity` (drop syntactic index)
     - Call `DeleteRedisVectorActivity` (drop semantic index)
     - Batched FalkorDB deletion loop:
       - `batchNumber = 0`
       - Compute `maxBatches` safety valve: if initial count is available, `(initialCount / batchSize * 2) + 10`; otherwise default to 10000. This prevents infinite loops if FalkorDB returns non-zero count but deletes zero nodes.
       - Loop: Call `DeleteFalkorDbBatchActivity` with `BatchedGraphDeletionInput(tenantId, batchSize: 500, batchNumber++)`
       - Continue until `BatchedGraphDeletionResult.IsComplete == true` OR `batchNumber >= maxBatches`
       - If loop exits via maxBatches: set status to `TenantStatus.Failed` with message "Batch loop exceeded maximum iterations ({maxBatches}). {RemainingNodes} nodes remain. Re-trigger deletion to retry."
       - Then call `DeleteFalkorDbGraphFinalizerActivity` to delete the empty graph
     - Call `DeleteTenantDataKeysActivity` (clean up Redis hash data, dedup keys, case keys)
  5. Call `RemoveTenantRegistryActivity` -- remove tenant from registry and index
  6. Return success result with list of deleted backends
- [ ] 4.3 Retry policy: `maxAttempts=5, firstInterval=2s, backoff=2.0, maxInterval=5min` (same as provisioning)
- [ ] 4.4 Compensation: Deletion is destructive and non-reversible. On partial failure, update status to `TenantStatus.Failed` with message listing which backends were successfully cleaned and which remain. Operator can re-trigger deletion to resume (idempotent activities handle already-deleted backends).
- [ ] 4.5 Use `context.CreateReplaySafeLogger<TenantDeletionWorkflow>()` for logging. Log: `DeletionStarted`, `BackendDeleted(backendName)`, `GraphBatchCompleted(batchNumber, remainingNodes)`, `DeletionCompleted`, `DeletionFailed`.
- [ ] 4.6 Create `GetTenantRegistryActivity` -- extends `WorkflowActivity<string, TenantInfo?>`. Calls `TenantRegistryService.GetTenantAsync(tenantId)`. Returns null if not found.

### Task 5: Add tenant status guard to existing endpoints (AC #3)

> **SCOPE NOTE:** This task implements the deletion-specific subset of Story 5-4's tenant context enforcement. Story 5-4 should reference this work and avoid duplicating these guards.

- [ ] 5.1 Create `Server/Tenants/TenantStatusGuard.cs` -- sealed class (instance service, not static) with constructor dependency on `TenantRegistryService`:
  - `ValidateTenantActiveAsync(string tenantId, CancellationToken ct)` -> `Task<ErrorResponse?>`
  - Internally calls `TenantRegistryService.GetTenantAsync(tenantId, ct)` to resolve tenant
  - Returns `TENANT_NOT_FOUND` if tenant is null
  - Returns `TENANT_DELETING` (HTTP 409 Conflict) if status is `Deleting`
  - Returns `TENANT_PROVISIONING` (HTTP 409 Conflict) if status is `Provisioning`
  - Returns `TENANT_FAILED` (HTTP 409 Conflict) if status is `Failed` or `CompensationFailed`
  - Returns null if `Active`
  - Register as singleton in DI (Task 7)
  - **Rationale:** Instance service encapsulates registry lookup, reducing each endpoint from 3 lines (fetch + validate + check) to 1 line. Natural stepping stone to Story 5-4's `TenantAuthorizationMiddleware` which can delegate to this service.
- [ ] 5.2 Add tenant status checks to ingestion endpoint (`POST /api/ingest`):
  - Inject `TenantStatusGuard` into endpoint delegate
  - Call `guard.ValidateTenantActiveAsync(tenantId, ct)`
  - Return 409 Conflict with appropriate error code if not active
- [ ] 5.3 Add tenant status checks to search endpoints:
  - `POST /api/search` -- inject guard, validate tenant active before searching
  - `GET /api/tenants/{tenantId}/cases/{caseId}/search` -- inject guard, validate tenant active
- [ ] 5.4 Add tenant status checks to case management endpoints:
  - `POST /api/tenants/{tenantId}/cases` -- validate before creating case
  - `DELETE /api/tenants/{tenantId}/cases/{caseId}` -- validate before deleting
  - `DELETE /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` -- validate before deleting MU
  - `POST /api/tenants/{tenantId}/cases/{caseId}/ingest` -- validate before ingesting

### Task 6: Add tenant deletion endpoint (AC #1, #4)

- [ ] 6.1 Add `DELETE /api/tenants/{tenantId}` endpoint (Minimal API in `Program.cs`):
  - Validate `tenantId` via `TenantIdGuard.Validate()`
  - Check tenant exists via `TenantRegistryService.GetTenantAsync()`
  - If not found, return 404 with `TENANT_NOT_FOUND`
  - If already `Deleting`, return 202 Accepted with message "Deletion already in progress"
  - Schedule `TenantDeletionWorkflow` with instance ID `delete-{tenantId}-{guid}` (non-deterministic for retry support)
  - Return 202 Accepted with `workflowInstanceId`
  - Catch `DaprException` and return 503 with `DAPR_UNAVAILABLE`
- [ ] 6.2 Add `GET /api/tenants/{tenantId}/deletion-status/{instanceId}` endpoint:
  - Query workflow status via `DaprWorkflowClient.GetWorkflowStateAsync(instanceId)`
  - Return workflow state or 404

### Task 7: Register workflow, activities, and services in DI (AC #1)

- [ ] 7.1 In `Program.cs` within `AddDaprWorkflow(options => { ... })`:
  - `options.RegisterWorkflow<TenantDeletionWorkflow>()`
  - `options.RegisterActivity<DeleteRediSearchActivity>()`
  - `options.RegisterActivity<DeleteRedisVectorActivity>()`
  - `options.RegisterActivity<DeleteFalkorDbBatchActivity>()`
  - `options.RegisterActivity<DeleteFalkorDbGraphFinalizerActivity>()`
  - `options.RegisterActivity<DeleteTenantDataKeysActivity>()`
  - `options.RegisterActivity<GetTenantRegistryActivity>()`
  - Verify `UpdateTenantStatusActivity` and `RemoveTenantRegistryActivity` are registered (created by Story 5-1). If not, register them here.
- [ ] 7.2 Ensure `TenantRegistryService` is registered as singleton (done in Story 5-1, verify only).
- [ ] 7.3 Register `TenantStatusGuard` as singleton: `builder.Services.AddSingleton<TenantStatusGuard>()`.

### Task 8: Contract serialization tests (AC #1)

- [ ] 8.1 `TenantDeletionInputSerializationTests` -- roundtrip JSON, camelCase properties
- [ ] 8.2 `TenantDeletionResultSerializationTests` -- roundtrip, verify `DeletedBackends` omitted when null
- [ ] 8.3 `BatchedGraphDeletionInputSerializationTests` -- roundtrip with default values
- [ ] 8.4 `BatchedGraphDeletionResultSerializationTests` -- roundtrip, verify `IsComplete` serialization

### Task 9: Activity unit tests (AC #1, #2)

- [ ] 9.1 `DeleteRediSearchActivityTests` -- verify `FT.DROPINDEX` called; verify idempotent on "Unknown index"
- [ ] 9.2 `DeleteRedisVectorActivityTests` -- same pattern as 9.1
- [ ] 9.3 `DeleteFalkorDbBatchActivityTests`:
  - Verify count query executes on correct graph
  - Verify batch delete with parameterized `$batchSize`
  - Verify `IsComplete = true` when count == 0
  - Verify graceful handling of graph-not-found `RedisServerException`
- [ ] 9.4 `DeleteFalkorDbGraphFinalizerActivityTests` -- verify `GRAPH.DELETE` called; idempotent on not-found
- [ ] 9.5 `DeleteTenantDataKeysActivityTests`:
  - Verify SCAN pattern matches `{tenantId}:case:*` and `{tenantId}:dedup:*` only (mu:* and vec:* are handled by `FT.DROPINDEX DD`)
  - Verify batched deletion via `KeyDeleteAsync`
- [ ] 9.6 `GetTenantRegistryActivityTests` -- verify returns TenantInfo or null
- [ ] 9.7 `GraphQueryBuilderTests` -- test `BuildCountAllNodes()` and `BuildBatchDeleteNodes(batchSize)` produce correct parameterized Cypher

### Task 10: TenantStatusGuard unit tests (AC #3)

- [ ] 10.1 `TenantStatusGuardTests` -- mock `TenantRegistryService` via NSubstitute:
  - `ValidateTenantActiveAsync_TenantNotFound_ReturnsTenantNotFound` -- registry returns null
  - `ValidateTenantActiveAsync_ActiveTenant_ReturnsNull` -- registry returns Active tenant
  - `ValidateTenantActiveAsync_DeletingTenant_ReturnsTenantDeleting` -- registry returns Deleting tenant
  - `ValidateTenantActiveAsync_ProvisioningTenant_ReturnsTenantProvisioning` -- registry returns Provisioning tenant
  - `ValidateTenantActiveAsync_FailedTenant_ReturnsTenantFailed` -- registry returns Failed tenant

### Task 12: Workflow orchestration unit tests (AC #1, #2, #4)

- [ ] 12.1 Create `Hexalith.Memories.Server.Tests/Workflows/TenantDeletionWorkflowTests.cs`
- [ ] 12.2 `DeletionWorkflow_HappyPath_CallsAllActivitiesInOrder` -- verify sequential activity calls: GetTenantRegistry -> UpdateStatus(Deleting) -> DeleteRediSearch -> DeleteRedisVector -> batched FalkorDb loop -> GraphFinalizer -> DeleteDataKeys -> RemoveRegistry
- [ ] 12.3 `DeletionWorkflow_TenantNotFound_ReturnsError` -- verify workflow returns error result without calling any deletion activities
- [ ] 12.4 `DeletionWorkflow_BatchedLoop_TerminatesWhenComplete` -- mock `DeleteFalkorDbBatchActivity` to return `IsComplete=false` for 3 batches then `IsComplete=true`, verify loop calls activity exactly 4 times then calls finalizer
- [ ] 12.5 `DeletionWorkflow_PartialFailure_SetsStatusToFailed` -- mock one activity to throw, verify `UpdateTenantStatusActivity` called with `TenantStatus.Failed`
- [ ] 12.6 `DeletionWorkflow_AlreadyDeleting_ContinuesIdempotently` -- mock `GetTenantRegistryActivity` returning tenant with `Deleting` status, verify workflow continues (replay safety)
- [ ] 12.7 `DeletionWorkflow_ProvisioningTenant_ReturnsError` -- verify workflow rejects deletion of a tenant still provisioning

### Task 11: Integration tests (AC #1, #2, #3, #4)

- [ ] 11.1 Create `Hexalith.Memories.IntegrationTests/Tenants/TenantDeletionIntegrationTests.cs`
- [ ] 11.2 `DeleteTenant_DropsAllThreeBackendIndexes` -- provision tenant, ingest data, delete, verify all indexes gone
- [ ] 11.3 `DeleteTenant_NonExistent_Returns404`
- [ ] 11.4 `DeleteTenant_AlreadyDeleting_Returns202WithMessage`
- [ ] 11.5 `BatchedGraphDeletion_LargeTenant_CompletesInBatches` -- create tenant with >500 graph nodes, verify batched deletion completes
- [ ] 11.6 `DeleteTenant_RemovedFromRegistry` -- after deletion, `GET /api/tenants` does not include deleted tenant
- [ ] 11.7 `DeleteTenant_SearchReturnsZero` -- after deletion, search across all axes returns empty
- [ ] 11.8 `TenantStatusGuard_RejectsDeletingTenant` -- ingestion and search requests return 409 during deletion
- [ ] 11.9 `DeleteTenant_IdempotentRerun` -- trigger deletion twice, second run completes without errors
- [ ] 11.10 `DropIndexDD_OnlyDeletesIndexedKeys` -- provision tenant, ingest data (creates mu:* and vec:* keys), also create case keys; drop RediSearch index with DD; verify mu:* keys are deleted AND case:*/dedup:* keys survive
- [ ] 11.11 `BatchLoop_MaxIterations_FailsSafely` -- mock or configure a scenario where batch deletion stalls, verify workflow exits loop and sets status to Failed

## Dev Notes

### Critical Dependency: Story 5-1 Must Be Completed First

Story 5-2 depends on the following components created by Story 5-1 (tenant provisioning):

| Component | Location | What 5-2 Reuses |
|---|---|---|
| `TenantStatus` enum | `Contracts/V1/TenantStatus.cs` | `Deleting` status value |
| `TenantInfo` record | `Contracts/V1/TenantInfo.cs` | Tenant registry lookup |
| `TenantRegistryService` | `Server/Tenants/TenantRegistryService.cs` | `GetTenantAsync`, `UpdateTenantStatusAsync`, `RemoveTenantAsync`, `ListTenantsAsync` |
| `UpdateTenantStatusActivity` | `Server/Activities/Tenants/UpdateTenantStatusActivity.cs` | Status transition to `Deleting` |
| `RemoveTenantRegistryActivity` | `Server/Activities/Tenants/RemoveTenantRegistryActivity.cs` | Remove tenant from registry after cleanup |
| `TenantStatusUpdateInput` | `Contracts/V1/TenantStatusUpdateInput.cs` | Activity input |
| `IndexSchemaDefinitions` | `Server/Infrastructure/IndexSchemaDefinitions.cs` | Index naming patterns for drop commands |
| `TenantIdGuard` | `Server/Activities/Indexing/TenantIdGuard.cs` | Tenant ID validation (already exists) |
| Tenant listing endpoints | `Program.cs` | `GET /api/tenants`, `GET /api/tenants/{tenantId}` |

**If Story 5-1 is NOT complete**, the dev agent must implement its Task 1 (contracts), Task 1b (index schema definitions), Task 2 (registry service), and Task 4.5 (registry activities) first.

### Architecture Decision: DAPR Workflow for Deletion (D23)

Architecture mandates DAPR Workflow for all multi-step orchestrations. `TenantDeletionWorkflow` follows the same pattern as `TenantProvisioningWorkflow` and `IngestionWorkflow`. Workflows must NEVER call external services directly -- all I/O happens in activities.

### Batched FalkorDB Deletion (NFR8, Architecture Mandate)

Architecture explicitly states: "Tenant deletion at scale is a potentially blocking operation -- async deletion with progress tracking required; graph deletion must not block other tenants' queries (batched deletion: delete N nodes per transaction, yield between batches)."

Implementation approach:
- Workflow calls `DeleteFalkorDbBatchActivity` in a loop
- Each invocation deletes up to 500 nodes via `MATCH (n) WITH n LIMIT $batchSize DETACH DELETE n`
- `DETACH DELETE` removes the node AND all its edges (same as case deletion in Story 3-5)
- Between activity invocations, DAPR yields execution, allowing other tenants' queries to proceed
- After all nodes deleted, `DeleteFalkorDbGraphFinalizerActivity` drops the empty graph via `GRAPH.DELETE`

Why sequential activities instead of a single long-running query:
1. FalkorDB holds a global lock during write operations -- large deletes block ALL tenants
2. DAPR Workflow activity boundaries create natural yield points
3. Progress tracking via `BatchedGraphDeletionResult.RemainingNodes`
4. If deletion fails mid-way, workflow resumes from the last successful batch (durable)

### Deletion is Non-Reversible -- No Compensation

Unlike provisioning (which has saga rollback), deletion is destructive and non-reversible. If deletion fails partway:
1. Tenant status stays `Deleting` (prevents new operations via `TenantStatusGuard`)
2. `TenantDeletionResult.Message` lists which backends were cleaned and which remain
3. Operator re-triggers `DELETE /api/tenants/{tenantId}` to resume
4. All activities are idempotent: dropping an already-dropped index or deleting from an empty graph succeeds silently

### Redis Data Key Cleanup

Dropping a RediSearch index via `FT.DROPINDEX` does NOT delete the underlying hash keys by default. The `DD` flag on `FT.DROPINDEX` deletes associated documents, but this only works for indexes with a unique prefix. Since our indexes use `{tenantId}:mu:*` and `{tenantId}:vec:*` prefixes (from `IndexSchemaDefinitions`), we can use `FT.DROPINDEX {index} DD` to drop both index and data.

**Decision**: Use `FT.DROPINDEX {index} DD` for both RediSearch and Redis Vector. The `DD` flag drops the index AND deletes all document hashes matching the index prefix. This eliminates the need to separately SCAN and delete `{tenantId}:mu:*` and `{tenantId}:vec:*` keys.

**However**, `FT.DROPINDEX DD` does not clean up:
- Case hash keys (`{tenantId}:case:*`, `{tenantId}:case:*:members`, `{tenantId}:case:*:activity`)
- Dedup keys (`{tenantId}:dedup:*`)

`DeleteTenantDataKeysActivity` handles these remaining keys via SCAN + batch delete. Use `SCAN 0 MATCH {tenantId}:case:* COUNT 1000` and `SCAN 0 MATCH {tenantId}:dedup:* COUNT 1000` patterns.

### Tenant Status Guard Pattern

`TenantStatusGuard` is an instance service (unlike `CaseValidator` which is static for pure input validation). The distinction: `CaseValidator` does format checks (no I/O), while `TenantStatusGuard` performs stateful validation (requires registry lookup). It returns structured `ErrorResponse` objects consistent with existing error handling:

```csharp
public sealed class TenantStatusGuard(TenantRegistryService registry)
{
    public async Task<ErrorResponse?> ValidateTenantActiveAsync(
        string tenantId, CancellationToken ct)
    {
        TenantInfo? tenant = await registry.GetTenantAsync(tenantId, ct);
        if (tenant is null)
            return new ErrorResponse("TENANT_NOT_FOUND", $"Tenant '{tenantId}' not found.", "List available tenants with GET /api/tenants");
        return tenant.Status switch
        {
            TenantStatus.Active => null,
            TenantStatus.Deleting => new ErrorResponse("TENANT_DELETING", $"Tenant '{tenantId}' is being deleted.", "Wait for deletion to complete."),
            TenantStatus.Provisioning => new ErrorResponse("TENANT_PROVISIONING", $"Tenant '{tenantId}' is still provisioning.", "Wait for provisioning to complete."),
            _ => new ErrorResponse("TENANT_UNAVAILABLE", $"Tenant '{tenantId}' is in state '{tenant.Status}'.", "Check tenant status and retry.")
        };
    }
}
```

### Endpoint Pattern Reference

Follow exact patterns from `Program.cs`:
- Async workflow scheduling: return 202 Accepted with `workflowInstanceId` (same as `POST /api/ingest`)
- Workflow status polling: `DaprWorkflowClient.GetWorkflowStateAsync(instanceId)` (same as `GET /api/ingest/{instanceId}`)
- Error responses: return `Results.BadRequest(errorResponse)` for validation, `Results.Conflict(errorResponse)` for status guards, `Results.NotFound()` for missing tenants

### Existing Code to Reuse

| Component | Location | Reuse |
|---|---|---|
| `TenantIdGuard.Validate()` | `Server/Activities/Indexing/TenantIdGuard.cs` | Tenant ID format validation |
| `TenantRegistryService` | `Server/Tenants/TenantRegistryService.cs` (from 5-1) | Registry CRUD operations |
| `UpdateTenantStatusActivity` | `Server/Activities/Tenants/` (from 5-1) | Status transitions |
| `RemoveTenantRegistryActivity` | `Server/Activities/Tenants/` (from 5-1) | Registry cleanup |
| `GetTenantRegistryActivity` | New in this story but follows 5-1 activity pattern | Registry lookup in workflow |
| `ErrorResponse` | `Contracts/V1/ErrorResponse.cs` | Structured error responses |
| `IngestionWorkflow` | `Server/Workflows/IngestionWorkflow.cs` | Workflow pattern, retry policies, replay-safe logging |
| `IGraphQueryBuilder` | `Server/Graph/IGraphQueryBuilder.cs` | Parameterized Cypher query builder |
| `GraphQueryBuilder` | `Server/Graph/GraphQueryBuilder.cs` | Add `BuildCountAllNodes()` and `BuildBatchDeleteNodes()` |
| `MemoriesJsonContext` | `Contracts/V1/MemoriesJsonContext.cs` | JSON serialization registration |
| Case deletion patterns | `Server/Cases/CaseService.cs` lines 618-700 | `DETACH DELETE` pattern, multi-backend cleanup |
| `CaseValidator` pattern | `Server/Cases/CaseValidator.cs` | Static validation pattern (TenantStatusGuard differs: instance service with DI, since it needs I/O) |

### Anti-Patterns to Avoid

1. **DO NOT delete all graph nodes in a single query** -- large `MATCH (n) DETACH DELETE n` blocks ALL tenants. Architecture mandates batched deletion.
2. **DO NOT call Redis/FalkorDB directly from workflow** -- only through activities.
3. **DO NOT implement compensation/rollback for deletion** -- deletion is destructive and non-reversible. Failed deletions are resumed, not rolled back.
4. **DO NOT skip the status guard** -- setting tenant to `Deleting` before cleanup is the only way to prevent concurrent ingestion/search.
5. **DO NOT assume all backends exist** -- handle gracefully if an index was already dropped or graph was already deleted (idempotent).
6. **DO NOT create synchronous deletion** -- use DAPR Workflow for durability, retry, and progress tracking.
7. **DO NOT use `KEYS` command in Redis** -- use `SCAN` for key enumeration to avoid blocking the Redis event loop.
8. **DO NOT hardcode batch size** -- pass via `BatchedGraphDeletionInput` for configurability.
9. **DO NOT forget to clean Redis data keys** -- dropping a RediSearch index may not delete underlying hash data depending on `DD` flag usage.

### Previous Story Intelligence

**From Story 5-1 (Tenant Provisioning):**
- `TenantRegistryService` uses DAPR state store with two-key pattern: individual tenant entry + tenant index list
- Concurrency handled via ETags for optimistic concurrency on index key
- `InitializeTenantRegistryActivity` provides atomic check-and-register pattern
- Compensation activities (`DeleteRediSearchIndexActivity`, `DeleteRedisVectorIndexActivity`, `DeleteFalkorDbGraphActivity`) exist but handle single operations (not batched)
- Workflow instance ID format: `provision-{tenantId}-{guid}` (non-deterministic). Use same pattern: `delete-{tenantId}-{guid}`
- FalkorDB graph initialization uses write+delete pattern; graph deletion is the reverse
- **Verify:** `RemoveTenantRegistryActivity` must handle ETag conflicts on the `tenant-registry-index` key with retry (compare-and-swap loop). If 5-1's implementation doesn't retry, this is a bug that could leave ghost entries in the tenant list.

**From Story 3-5 (Memory Unit & Case Deletion):**
- `DETACH DELETE` removes node AND all relationships (edges)
- Case deletion sets status to `Deleting` before cleanup (status guard pattern)
- Multi-backend deletion runs in parallel per memory unit via `Task.WhenAll()`
- `BuildDeleteMemoryUnitNode` and `BuildDeleteCaseNode` in `GraphQueryBuilder` use parameterized `$id` queries

**From Story 4-1/4-2 (Causal Chain Traversal):**
- FalkorDB `RedisServerException` for graph-not-found must be caught gracefully
- `Stopwatch.Elapsed.TotalMilliseconds` (not `.Milliseconds`) for telemetry

**From IngestionWorkflow:**
- `context.CreateReplaySafeLogger<T>()` for logging inside workflows
- `WorkflowRetryPolicy` with backoff coefficient for retry
- `WorkflowTaskOptions` wraps retry policy
- `CreateCompensationRetry()` helper pattern for retry policy reuse

### Implementation Order

1. Task 3 (graph query builder methods) -- no external dependencies
2. Task 1 (contracts) -- depends on 5-1 contracts existing
3. Task 2 (activities) -- depends on Task 1 and Task 3
4. Task 4 (workflow) -- depends on Task 2
5. Task 5 (status guard) -- can be done in parallel with Tasks 2-4
6. Task 6 (endpoint) -- depends on Task 4
7. Task 7 (DI registration) -- depends on Tasks 2, 4, 6
8. Tasks 8-12 (tests) -- in parallel after implementation tasks

### Project Structure Notes

New files follow existing feature-based namespace organization:

| Namespace | New Files |
|---|---|
| `Hexalith.Memories.Contracts.V1` | `TenantDeletionInput.cs`, `TenantDeletionResult.cs`, `BatchedGraphDeletionInput.cs`, `BatchedGraphDeletionResult.cs` |
| `Hexalith.Memories.Server.Activities.Tenants` | `DeleteRediSearchActivity.cs`, `DeleteRedisVectorActivity.cs`, `DeleteFalkorDbBatchActivity.cs`, `DeleteFalkorDbGraphFinalizerActivity.cs`, `DeleteTenantDataKeysActivity.cs`, `GetTenantRegistryActivity.cs` |
| `Hexalith.Memories.Server.Workflows` | `TenantDeletionWorkflow.cs` |
| `Hexalith.Memories.Server.Tenants` | `TenantStatusGuard.cs` |

Files to modify:

| File | Change |
|---|---|
| `Contracts/V1/MemoriesJsonContext.cs` | Register 4 new contract types |
| `Server/Graph/IGraphQueryBuilder.cs` | Add `BuildCountAllNodes()`, `BuildBatchDeleteNodes(int)` |
| `Server/Graph/GraphQueryBuilder.cs` | Implement new methods |
| `Server/Program.cs` | Register workflow + 6 activities, add 2 endpoints, add status guards to existing endpoints |

Tests follow existing structure:
- `Contracts.Tests/V1/` for serialization tests
- `Server.Tests/Activities/Tenants/` for activity tests
- `Server.Tests/Tenants/` for TenantStatusGuard tests
- `Server.Tests/Graph/GraphQueryBuilderTests.cs` for new builder methods
- `IntegrationTests/Tenants/` for integration tests

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.2] -- AC definitions, batched deletion, TENANT_DELETING error code
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns: Tenant Isolation] -- Batched deletion mandate, async with progress tracking
- [Source: _bmad-output/planning-artifacts/architecture.md#DAPR Workflows: TenantDeletionWorkflow] -- Activity sequence: DeleteRediSearch -> DeleteRedisVector -> DeleteFalkorDb (batched)
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Dependencies: Tenant Deletion Workflow] -- FalkorDB performance concern, batched N nodes per activity
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Boundaries] -- Index naming: `{tenantId}:memories:idx`, `{tenantId}:memories:vec`, FalkorDB graph per tenant
- [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines] -- Activities do I/O, workflows orchestrate; DAPR Workflow for all multi-step orchestrations
- [Source: _bmad-output/planning-artifacts/architecture.md#D23] -- DAPR Workflows for tenant provisioning/deletion
- [Source: _bmad-output/implementation-artifacts/5-1-tenant-provisioning-workflow.md] -- Registry service, compensation activities, status enum, endpoint patterns
- [Source: _bmad-output/implementation-artifacts/3-5-memory-unit-deletion-and-case-deletion.md] -- DETACH DELETE pattern, status guard pattern, multi-backend cleanup
- [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs] -- Workflow pattern: retry policy, compensation, replay-safe logging
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs] -- Parameterized Cypher query patterns, BuildDeleteMemoryUnitNode, BuildDeleteCaseNode

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
