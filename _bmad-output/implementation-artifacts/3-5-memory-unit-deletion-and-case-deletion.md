# Story 3.5: Memory Unit Deletion & Case Deletion

Status: ready-for-dev

## Story

As a developer,
I want to delete individual memory units and entire cases,
so that I can manage knowledge lifecycle and clean up outdated content.

## Acceptance Criteria

1. **Given** a memory unit in a case, **When** I delete the memory unit (FR35), **Then** it is removed from RediSearch (syntactic index entry) **And** it is removed from Redis Vector (semantic vector) **And** it is removed from FalkorDB (node and all connected edges) **And** a deletion activity event is recorded in the case activity log
2. **Given** a case with memory units, **When** I delete the case (FR27), **Then** all memory units in the case are deleted from all three backends **And** the case node and all case-scoped edges are removed from FalkorDB **And** the case is removed from the case list **And** the operation completes reliably with status tracking to prevent partial state
3. **Given** a case deletion is in progress, **When** a new ingestion request targets the case, **Then** the ingestion is rejected with error code `CASE_DELETING` and a suggestion to wait
4. **Given** a memory unit has graph edges connecting it to other memory units, **When** the memory unit is deleted, **Then** all edges to and from that memory unit are also deleted **And** the connected memory units are not affected

## Tasks / Subtasks

- [ ] Task 1: Add `Deleting` status and new activity event types (AC: #2, #3)
  - [ ] 1.1 Add `Deleting` value to `CaseStatus` enum in `Contracts/V1/CaseStatus.cs`
  - [ ] 1.2 Add `MemoryUnitDeleted` and `CaseDeleted` values to `CaseActivityEventType` enum in `Contracts/V1/CaseActivityEventType.cs`
  - [ ] 1.3 Update `ParseCaseFromHash` in `CaseService.cs` to parse `"deleting"` status string to `CaseStatus.Deleting`
- [ ] Task 2: Add graph query builder methods (AC: #1, #2)
  - [ ] 2.1 Add `BuildListCaseMemoryUnitIds(string caseId)` to `IGraphQueryBuilder` interface
  - [ ] 2.2 Add `BuildDeleteCaseNode(string caseId)` to `IGraphQueryBuilder` interface
  - [ ] 2.3 Implement `BuildListCaseMemoryUnitIds` in `GraphQueryBuilder`: `MATCH (c:Case {id: $caseId})-[:CONTAINS]->(m:MemoryUnit) RETURN m.id AS memoryUnitId`
  - [ ] 2.4 Implement `BuildDeleteCaseNode` in `GraphQueryBuilder`: `MATCH (c:Case {id: $caseId}) DETACH DELETE c`
- [ ] Task 3: Add validation methods (AC: #1)
  - [ ] 3.1 Add `ValidateMemoryUnitId(string memoryUnitId)` to `CaseValidator` — not null/empty, alphanumeric+hyphens regex (`SafeCaseIdRegex`), max 200 chars
  - [ ] 3.2 Add `ValidateDeleteMemoryUnit(string tenantId, string caseId, string memoryUnitId)` to `CaseValidator` — validates all three IDs
- [ ] Task 4: Add deletion methods to CaseService (AC: #1, #2, #3, #4)
  - [ ] 4.1 Add `DeleteMemoryUnitAsync(string tenantId, string caseId, string memoryUnitId, CancellationToken)` — returns `bool` (true=deleted, false=not found)
  - [ ] 4.2 Verify MU exists and read case ownership via `HashGetAsync(muKey, "caseId")` — single call checks existence + reads caseId field
  - [ ] 4.3 Verify returned `caseId` matches the specified case (return false if mismatch — prevents cross-case deletion)
  - [ ] 4.4 Delete from all 3 backends in parallel: `KeyDeleteAsync({tenantId}:mu:{muId})`, `KeyDeleteAsync({tenantId}:vec:{muId})`, `BuildDeleteMemoryUnitNode(muId)` via FalkorDB
  - [ ] 4.5 Record `MemoryUnitDeleted` activity event (await pattern, matching `CreateCaseAsync`)
  - [ ] 4.6 Add `DeleteCaseAsync(string tenantId, string caseId, CancellationToken)` — returns `bool` (true=deleted, false=not found)
  - [ ] 4.7 Verify case exists via `KeyExistsAsync` on `{tenantId}:case:{caseId}` (cheaper than `HashGetAllAsync` — we don't need hash contents)
  - [ ] 4.8 Set case status to `"deleting"` via `HashSetAsync(caseKey, "status", "deleting")` (AC #3 guard)
  - [ ] 4.9 Find all MU IDs via `BuildListCaseMemoryUnitIds` graph query
  - [ ] 4.10 For each MU ID: delete from all 3 backends in parallel (same pattern as 4.4)
  - [ ] 4.11 Delete case node from FalkorDB via `BuildDeleteCaseNode`
  - [ ] 4.12 Delete Redis keys: `{tenantId}:case:{caseId}:members`, `{tenantId}:case:{caseId}:activity`, `{tenantId}:case:{caseId}`
  - [ ] 4.13 Log case deletion at Information level (no activity event — the stream is being deleted)
- [ ] Task 5: Add endpoints to Program.cs (AC: #1, #2, #3)
  - [ ] 5.1 Add `DELETE /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` endpoint
  - [ ] 5.2 Add `DELETE /api/tenants/{tenantId}/cases/{caseId}` endpoint
  - [ ] 5.3 Both endpoints: validate inputs via `CaseValidator`, verify case exists via `GetCaseAsync`, check case status is not `Deleting` (for MU deletion), call service method, return appropriate status code
- [ ] Task 6: Unit tests for contract changes (AC: #2, #3)
  - [ ] 6.1 Add `CaseStatus.Deleting` roundtrip and string reject tests to `EnumSerializationTests.cs`
  - [ ] 6.2 Add `CaseActivityEventType.MemoryUnitDeleted` and `CaseActivityEventType.CaseDeleted` tests to `EnumSerializationTests.cs`
  - [ ] 6.3 Update `CaseStatusDetailSerializationTests.cs` if `CaseStatusDetail` constructor changes
- [ ] Task 7: Unit tests for graph query builder (AC: #1, #2)
  - [ ] 7.1 Add `BuildListCaseMemoryUnitIds_*` tests to `GraphQueryBuilderTests.cs`
  - [ ] 7.2 Add `BuildDeleteCaseNode_*` tests to `GraphQueryBuilderTests.cs`
- [ ] Task 8: Unit tests for CaseValidator (AC: #1)
  - [ ] 8.1 Add `ValidateMemoryUnitId_*` tests to `CaseValidatorTests.cs` (valid, null, empty, special chars, max length)
  - [ ] 8.2 Add `ValidateDeleteMemoryUnit_*` tests covering all three ID validations
- [ ] Task 9: Unit tests for CaseService deletion (AC: #1, #2, #3, #4)
  - [ ] 9.1 Add `DeleteMemoryUnitAsync_*` tests to `CaseServiceTests.cs`: MU found + deleted, MU not found, MU wrong case, activity event recorded
  - [ ] 9.2 Add `DeleteCaseAsync_*` tests: case found + deleted (0 MUs), case found + deleted (3 MUs), case not found, status set to deleting before cleanup
  - [ ] 9.3 Verify `BuildDeleteMemoryUnitNode` called for each MU in case deletion
  - [ ] 9.4 Verify all 3 Redis keys deleted for case: `{tenantId}:case:{caseId}`, `:members`, `:activity`
  - [ ] 9.5 Verify graph case node deleted via `BuildDeleteCaseNode`
- [ ] Task 10: Integration tests (AC: #1, #2, #3, #4)
  - [ ] 10.1 Delete MU roundtrip: ingest MU into case, delete MU, verify 204, verify GET search no longer returns it
  - [ ] 10.2 Delete MU 404: delete non-existent MU, verify 404
  - [ ] 10.3 Delete MU wrong case: create MU in case A, try delete via case B, verify 404/403
  - [ ] 10.4 Delete case roundtrip: create case with MUs, delete case, verify 204, verify GET case returns 404
  - [ ] 10.5 Delete case with members: create case + add members, delete case, verify member key cleaned up
  - [ ] 10.6 Delete empty case: create case with no MUs, delete, verify 204
  - [ ] 10.7 Verify `ListCasesAsync` no longer returns deleted case
  - [ ] 10.8 Delete non-existent case: verify 404

## Dev Notes

### Implementation Order

Task 1 -> 2 -> 3 -> 4 -> 5 -> 6-10 (tests in parallel). Contracts first (1), then graph queries (2), then validation (3), then service logic (4), then endpoints (5), then all tests.

### Deletion Architecture Decision: Synchronous with Status Guard

The AC suggests DAPR Workflow for atomicity. For MVP, this story uses **synchronous deletion with a "deleting" status guard**:
1. Case status set to `"deleting"` atomically before any resource cleanup
2. All MUs deleted sequentially from all 3 backends (each MU's 3 deletions run in parallel)
3. Case resources cleaned up last

**Why not DAPR Workflow for MVP:**
- No existing deletion workflow infrastructure in the codebase
- Case MU counts at MVP scale are manageable (< 1000)
- The "deleting" status guard provides the same ingestion protection
- Retry is naturally idempotent: `BuildListCaseMemoryUnitIds` returns only remaining MUs

**What happens on partial failure:**
- Case stays in `Deleting` status — visible to users, prevents new ingestion
- Retry of DELETE endpoint picks up remaining MUs (graph query returns only undeleted ones)
- Already-deleted Redis keys are idempotent (`KeyDeleteAsync` returns false for missing keys)
- DAPR Workflow orchestration planned for Epic 6 (pipeline resilience) when production-grade deletion is needed

### CaseStatus Enum Change

Add `Deleting` to `CaseStatus` (Contracts/V1/CaseStatus.cs):
```csharp
[JsonConverter(typeof(CamelCaseStringEnumConverter<CaseStatus>))]
public enum CaseStatus { Active, Closed, Deleting }
```

Update `ParseCaseFromHash` in CaseService.cs (lines 356-358) to handle the new status:
```csharp
CaseStatus status = statusStr switch
{
    _ when string.Equals(statusStr, "deleting", StringComparison.OrdinalIgnoreCase) => CaseStatus.Deleting,
    _ when string.Equals(statusStr, "closed", StringComparison.OrdinalIgnoreCase) => CaseStatus.Closed,
    _ => CaseStatus.Active,
};
```

### CaseActivityEventType Additions

Add to `CaseActivityEventType` enum (Contracts/V1/CaseActivityEventType.cs):
```csharp
public enum CaseActivityEventType
{
    CaseCreated,
    MemoryUnitIngested,
    IngestionFailed,
    SearchExecuted,
    MemberAdded,
    MemberRemoved,
    MemoryUnitDeleted,  // NEW
    CaseDeleted,        // NEW
}
```

`MemoryUnitDeleted` is recorded when a single MU is deleted via the MU deletion endpoint. `CaseDeleted` is NOT recorded to the case's activity stream (the stream is deleted during case cleanup). It is logged via `ILogger` at Information level.

### Graph Query Builder Methods

**IGraphQueryBuilder** — add two methods:

```csharp
/// <summary>Lists all memory unit IDs linked to a case via CONTAINS edges.</summary>
(string Query, IDictionary<string, object> Parameters) BuildListCaseMemoryUnitIds(string caseId);

/// <summary>Deletes a case node and all its remaining relationships.</summary>
(string Query, IDictionary<string, object> Parameters) BuildDeleteCaseNode(string caseId);
```

**GraphQueryBuilder** implementations:

```csharp
public (string Query, IDictionary<string, object> Parameters) BuildListCaseMemoryUnitIds(string caseId)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(caseId);

    const string query = "MATCH (c:Case {id: $caseId})-[:CONTAINS]->(m:MemoryUnit) RETURN m.id AS memoryUnitId";

    Dictionary<string, object> parameters = new()
    {
        ["caseId"] = caseId,
    };

    return (query, parameters);
}

public (string Query, IDictionary<string, object> Parameters) BuildDeleteCaseNode(string caseId)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(caseId);

    const string query = "MATCH (c:Case {id: $caseId}) DETACH DELETE c";

    Dictionary<string, object> parameters = new()
    {
        ["caseId"] = caseId,
    };

    return (query, parameters);
}
```

**Why `DETACH DELETE` on case node:** Even though all CONTAINS edges should be gone after MU deletion, `DETACH DELETE` is defensive — handles edge cases where an MU deletion failed silently or edges of unexpected types exist.

**Why `BuildListCaseMemoryUnitIds` and NOT `BuildCountCaseMemoryUnits`:** Count only tells us how many MUs exist; list gives us the IDs needed to delete Redis hashes. The existing `BuildCountCaseMemoryUnits` (line 103-115 of GraphQueryBuilder.cs) uses the same `MATCH` pattern but `RETURN count(m)` instead of `RETURN m.id`.

### CaseValidator Additions

Add to `CaseValidator` (Cases/CaseValidator.cs):

```csharp
public ErrorResponse? ValidateMemoryUnitId(string memoryUnitId)
{
    if (string.IsNullOrWhiteSpace(memoryUnitId))
    {
        return new ErrorResponse(
            "INVALID_MEMORY_UNIT_ID",
            "MemoryUnitId is required.",
            "Provide a valid memory unit identifier.");
    }

    if (memoryUnitId.Length > 200)
    {
        return new ErrorResponse(
            "INVALID_MEMORY_UNIT_ID",
            "MemoryUnitId must not exceed 200 characters.",
            "Provide a shorter identifier.");
    }

    if (!SafeCaseIdRegex().IsMatch(memoryUnitId))
    {
        return new ErrorResponse(
            "INVALID_MEMORY_UNIT_ID",
            "MemoryUnitId contains invalid characters.",
            "Only alphanumeric characters and hyphens are allowed.");
    }

    return null;
}

public ErrorResponse? ValidateDeleteMemoryUnit(string tenantId, string caseId, string memoryUnitId)
{
    ErrorResponse? tenantError = ValidateTenantId(tenantId);
    if (tenantError is not null) return tenantError;

    ErrorResponse? caseError = ValidateCaseId(caseId);
    if (caseError is not null) return caseError;

    return ValidateMemoryUnitId(memoryUnitId);
}
```

Reuse existing `SafeCaseIdRegex` (`^[a-zA-Z0-9\-]+$`) — ULIDs are alphanumeric and a subset of this pattern. Prevents Redis key injection via `:` characters in `{tenantId}:mu:{memoryUnitId}`.

### CaseService Deletion Methods

Add to CaseService.cs after `GetMemberCountAsync` (line 313):

**DeleteMemoryUnitAsync:**
```csharp
public async Task<bool> DeleteMemoryUnitAsync(
    string tenantId, string caseId, string memoryUnitId, CancellationToken cancellationToken)
{
    IDatabase db = _redis.GetDatabase();
    string muKey = $"{tenantId}:mu:{memoryUnitId}";

    // Verify MU exists by checking the syntactic hash key
    RedisValue storedCaseId = await db.HashGetAsync(muKey, "caseId").ConfigureAwait(false);
    if (!storedCaseId.HasValue)
    {
        return false; // MU not found in syntactic index
    }

    // Verify MU belongs to the specified case
    if (!string.Equals(storedCaseId.ToString(), caseId, StringComparison.Ordinal))
    {
        return false; // MU exists but belongs to a different case
    }

    // Delete from all 3 backends in parallel
    string vecKey = $"{tenantId}:vec:{memoryUnitId}";
    NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
    (string graphQuery, IDictionary<string, object> graphParams) = _graphQueryBuilder.BuildDeleteMemoryUnitNode(memoryUnitId);

    await Task.WhenAll(
        db.KeyDeleteAsync(muKey).AsTask(),
        db.KeyDeleteAsync(vecKey).AsTask(),
        falkor.QueryAsync(tenantId, graphQuery, graphParams)).ConfigureAwait(false);

    // Record deletion activity
    _ = await _activityService.RecordEventAsync(
        tenantId, caseId, CaseActivityEventType.MemoryUnitDeleted, "system",
        $"Memory unit '{memoryUnitId}' deleted", memoryUnitId, cancellationToken).ConfigureAwait(false);

    _logger.LogInformation(
        "Deleted memory unit {MemoryUnitId} from case {CaseId} in tenant {TenantId}",
        memoryUnitId, caseId, tenantId);

    return true;
}
```

**Key design points:**
- `HashGetAsync(muKey, "caseId")` is a single-field read, not `HashGetAllAsync` — minimizes data transfer
- Case ownership check prevents cross-case deletion (a MU can only be deleted via its owning case)
- `Task.WhenAll` parallelizes the 3 backend deletions — no ordering dependency between them
- `KeyDeleteAsync` returns `bool` but we don't check it — idempotent (already deleted = fine)
- `BuildDeleteMemoryUnitNode` uses `DETACH DELETE` which removes all edges (AC #4)
- Activity event uses `memoryUnitId` parameter (CaseActivityService already supports this field)

**DeleteCaseAsync:**
```csharp
public async Task<bool> DeleteCaseAsync(
    string tenantId, string caseId, CancellationToken cancellationToken)
{
    IDatabase db = _redis.GetDatabase();
    string caseKey = $"{tenantId}:case:{caseId}";

    // Verify case exists
    bool exists = await db.KeyExistsAsync(caseKey).ConfigureAwait(false);
    if (!exists)
    {
        return false;
    }

    // Set status to "deleting" — prevents concurrent ingestion (AC #3)
    await db.HashSetAsync(caseKey, "status", "deleting").ConfigureAwait(false);

    // Find all memory unit IDs from graph
    NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
    (string listQuery, IDictionary<string, object> listParams) = _graphQueryBuilder.BuildListCaseMemoryUnitIds(caseId);
    NFalkorDB.ResultSet result = await falkor.QueryAsync(tenantId, listQuery, listParams).ConfigureAwait(false);

    // Delete each MU from all 3 backends
    foreach (NFalkorDB.Record record in result)
    {
        string muId = record.Values[0].ToString()!;

        (string delQuery, IDictionary<string, object> delParams) = _graphQueryBuilder.BuildDeleteMemoryUnitNode(muId);

        await Task.WhenAll(
            db.KeyDeleteAsync($"{tenantId}:mu:{muId}").AsTask(),
            db.KeyDeleteAsync($"{tenantId}:vec:{muId}").AsTask(),
            falkor.QueryAsync(tenantId, delQuery, delParams)).ConfigureAwait(false);
    }

    // Delete case node from FalkorDB (DETACH DELETE handles any remaining edges)
    (string caseDelQuery, IDictionary<string, object> caseDelParams) = _graphQueryBuilder.BuildDeleteCaseNode(caseId);
    await falkor.QueryAsync(tenantId, caseDelQuery, caseDelParams).ConfigureAwait(false);

    // Delete case Redis resources
    await Task.WhenAll(
        db.KeyDeleteAsync($"{tenantId}:case:{caseId}:members").AsTask(),
        db.KeyDeleteAsync($"{tenantId}:case:{caseId}:activity").AsTask(),
        db.KeyDeleteAsync(caseKey).AsTask()).ConfigureAwait(false);

    _logger.LogInformation(
        "Deleted case {CaseId} with {MemoryUnitCount} memory units from tenant {TenantId}",
        caseId, result.Count, tenantId);

    return true;
}
```

**Key design points:**
- `KeyExistsAsync` is cheaper than `HashGetAllAsync` for existence check
- Status set to `"deleting"` BEFORE any resource deletion — if method crashes here, case is left in "deleting" state (safe, retriable)
- MU deletion loops sequentially per MU, but each MU's 3 backend deletions run in parallel
- No activity event recorded for case deletion — the activity stream (`{tenantId}:case:{caseId}:activity`) is deleted as part of cleanup. Log via `ILogger` instead
- Case hash key deleted LAST — if any prior step fails, the case still exists in "deleting" state for retry
- `KeyDeleteAsync` is idempotent for all 3 case keys

**Retry idempotency:** If `DeleteCaseAsync` fails mid-way and is retried:
1. `KeyExistsAsync(caseKey)` returns true (case hash still exists)
2. Status is already "deleting" — `HashSetAsync` is idempotent
3. `BuildListCaseMemoryUnitIds` returns only remaining MUs (already-deleted MUs have no CONTAINS edge)
4. Backend deletions are idempotent (`KeyDeleteAsync` returns false for missing keys, `DETACH DELETE` on non-existent node is a no-op)

### API Endpoints

**DELETE** `/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}`:

```csharp
app.MapDelete("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}", async (
    string tenantId,
    string caseId,
    string memoryUnitId,
    CaseService caseService,
    CaseValidator validator,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? validationError = validator.ValidateDeleteMemoryUnit(tenantId, caseId, memoryUnitId);
    if (validationError is not null)
    {
        return Results.BadRequest(validationError);
    }

    Case? targetCase = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
    if (targetCase is null)
    {
        return Results.NotFound(new ErrorResponse(
            "CASE_NOT_FOUND",
            $"Case '{caseId}' not found in tenant '{tenantId}'.",
            $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
    }

    if (targetCase.Status == CaseStatus.Deleting)
    {
        return Results.Conflict(new ErrorResponse(
            "CASE_DELETING",
            $"Case '{caseId}' is being deleted.",
            "Wait for deletion to complete or retry later."));
    }

    bool deleted = await caseService.DeleteMemoryUnitAsync(tenantId, caseId, memoryUnitId, cancellationToken);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new ErrorResponse(
            "MEMORY_UNIT_NOT_FOUND",
            $"Memory unit '{memoryUnitId}' not found in case '{caseId}'.",
            $"Use GET /api/search?tenantId={tenantId}&caseId={caseId} to find available memory units."));
});
```

**DELETE** `/api/tenants/{tenantId}/cases/{caseId}`:

```csharp
app.MapDelete("/api/tenants/{tenantId}/cases/{caseId}", async (
    string tenantId,
    string caseId,
    CaseService caseService,
    CaseValidator validator,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? tenantError = validator.ValidateTenantId(tenantId);
    if (tenantError is not null)
    {
        return Results.BadRequest(tenantError);
    }

    ErrorResponse? caseError = validator.ValidateCaseId(caseId);
    if (caseError is not null)
    {
        return Results.BadRequest(caseError);
    }

    bool deleted = await caseService.DeleteCaseAsync(tenantId, caseId, cancellationToken);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new ErrorResponse(
            "CASE_NOT_FOUND",
            $"Case '{caseId}' not found in tenant '{tenantId}'.",
            $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
});
```

**Endpoint placement in Program.cs:** Add DELETE MU endpoint after the GET members endpoint (line ~414). Add DELETE case endpoint after the DELETE MU endpoint. Group all case resource endpoints together.

**Why no case existence check in DELETE case:** `DeleteCaseAsync` internally checks existence via `KeyExistsAsync` and returns false if not found. An extra `GetCaseAsync` round-trip is unnecessary — the service method handles it.

**Why `Results.Conflict` (409) for CASE_DELETING:** HTTP 409 Conflict communicates that the request cannot be processed because of the current state of the resource (case is being deleted). This is more semantically correct than 400 Bad Request.

### Redis Key Cleanup Inventory

When deleting a **memory unit**, delete these keys:
| Key | Backend | Data |
|-----|---------|------|
| `{tenantId}:mu:{muId}` | Redis (RediSearch hash) | Syntactic index document |
| `{tenantId}:vec:{muId}` | Redis (Vector hash) | Semantic embedding vector |
| FalkorDB node `MemoryUnit {id: muId}` | FalkorDB | Graph node + all edges |

When deleting a **case**, delete all the above for each MU, plus:
| Key | Backend | Data |
|-----|---------|------|
| `{tenantId}:case:{caseId}` | Redis (Hash) | Case metadata |
| `{tenantId}:case:{caseId}:members` | Redis (Hash) | Member storage (Story 3.3) |
| `{tenantId}:case:{caseId}:activity` | Redis (Stream) | Activity event log (Story 3.2) |
| FalkorDB node `Case {id: caseId}` | FalkorDB | Case graph node |

### Error Code Registry

Existing codes (unchanged): `INVALID_TENANT_ID` (400), `CASE_NOT_FOUND` (404), `INVALID_CASE_ID` (400), `INVALID_MEMBER_ID` (400), `MEMBER_NOT_FOUND` (404), `MEMBER_LIMIT_EXCEEDED` (400)

New codes:
- `INVALID_MEMORY_UNIT_ID` (400) — "MemoryUnitId contains invalid characters" or "MemoryUnitId is required"
- `MEMORY_UNIT_NOT_FOUND` (404) — "Memory unit '{memoryUnitId}' not found in case '{caseId}'"
- `CASE_DELETING` (409) — "Case '{caseId}' is being deleted"

### Critical Anti-Patterns to Avoid

1. **Do NOT create a DAPR Workflow for case deletion** — synchronous with status guard is the MVP approach. DAPR Workflow is planned for Epic 6 (pipeline resilience)
2. **Do NOT create a `CaseDeletionService` or `MemoryUnitDeletionService`** — add methods to existing `CaseService`. One class for all case operations (same rationale as Story 3.3)
3. **Do NOT use `KEYS` or `SCAN` command to find memory unit Redis keys** — use FalkorDB graph query (`BuildListCaseMemoryUnitIds`) to get MU IDs, then construct deterministic keys
4. **Do NOT delete the case hash FIRST** — delete it LAST. If cleanup fails mid-way, the case in "deleting" status remains visible for retry. Deleting it first makes the case invisible but leaves orphaned data
5. **Do NOT record a `CaseDeleted` activity event** on the case activity stream — the stream is deleted as part of cleanup. Use `ILogger.LogInformation` instead
6. **Do NOT cascade-delete annotation memory units** — annotation cascade is Story 3.6's responsibility. `DETACH DELETE` on MU nodes removes annotates edges but leaves connected annotation MUs intact (AC #4: "connected memory units are not affected")
7. **Do NOT delete dedup keys from DAPR state store** — dedup key cleanup is in deferred-work.md as a separate concern. Deleting MU data from the 3 backends is sufficient for this story
8. **Do NOT modify the ingestion workflow** to check case deletion status — the `Deleting` enum value is available for ingestion code to check, but modifying `IngestionWorkflow` is outside this story's scope
9. **Do NOT skip the case ownership check** when deleting a MU — verify `caseId` field in the MU hash matches the URL caseId. Prevents cross-case deletion via path manipulation
10. **Do NOT use `HashGetAllAsync` to check MU existence** — use `HashGetAsync(muKey, "caseId")` to read only the field needed for ownership verification. Cheaper than loading the full hash

### Architecture Decision Records

**ADR-1: Synchronous deletion with status guard (not DAPR Workflow)**
AC suggests DAPR Workflow for atomicity. The real requirement is "partial deletion is not acceptable" — meaning invisible partial state, not that partial state can never exist transiently. MVP uses synchronous deletion because: (1) no existing deletion workflow infrastructure, (2) case MU counts manageable at MVP scale, (3) "deleting" status provides same ingestion protection, (4) retry is naturally idempotent via graph query. The `Deleting` status guard makes any partial state visible and retriable — satisfying the spirit of the AC. Production-grade async deletion with progress tracking planned for Epic 6.

**ADR-2: 3-backend parallel deletion per MU (not sequential)**
Each MU's Redis hash, vector hash, and FalkorDB node are deleted via `Task.WhenAll`. These operations are independent (no cross-backend transaction). If one fails, the MU is partially deleted — but all three operations are idempotent, so retry cleans up the remainder. Sequential deletion would be 3x slower for case deletion with many MUs.

**ADR-3: Graph-driven MU discovery (not Redis SCAN)**
`BuildListCaseMemoryUnitIds` queries the FalkorDB graph for MU IDs linked to a case via CONTAINS edges. Alternative: SCAN Redis for `{tenantId}:mu:*` keys and filter by `caseId` field. Graph query wins because: (1) exact O(k) result set (k = MUs in case), not O(N) scan over all tenant MUs, (2) SCAN can return deleted keys during deletion, causing confusion, (3) CONTAINS edges are the authoritative source of case membership.

**ADR-4: Deletion order: status → MUs → case node → Redis keys**
1. Status set to "deleting" (crash-safe guard)
2. MUs deleted from all backends (graph-driven discovery, naturally shrinks as MUs are removed)
3. Case node deleted from FalkorDB (DETACH DELETE handles any remaining edges)
4. Redis case keys deleted last (case remains visible until fully cleaned up)

This order maximizes crash recoverability. At any failure point, the case is in a consistent "partially deleted" state that can be retried. The case hash is deleted LAST because it is the "liveness probe" for the operation — all validation checks (`GetCaseAsync`, `KeyExistsAsync`) and retry logic depend on it existing. Deleting it first would make the case invisible but leave orphaned MU and metadata keys.

### Known Limitations

1. **Synchronous timeout risk for large cases**: Deleting a case with N memory units executes N sequential iterations (each with 3 parallel backend calls, ~50ms). Cases with 500+ MUs may exceed HTTP timeout (~30s). For MVP, this is acceptable (expected case sizes < 100 MUs). DAPR Workflow upgrade in Epic 6 adds async deletion with progress tracking for large cases.

2. **Dedup key orphaning**: Deleted MU's dedup keys persist in DAPR state store. Re-ingesting the same content (identical source URI + content hash) will be silently blocked by dedup detection, returning the old MU ID that no longer exists. No error is surfaced — the user sees a "duplicate" response pointing to missing data. Fix: add dedup key TTL or deletion (deferred to Epic 8, Story 8.2 consistency verification).

3. **AC #3 ingestion guard is partially satisfied**: This story sets `CaseStatus.Deleting` and the MU deletion endpoint returns 409 `CASE_DELETING`. However, the ingestion workflow (DAPR Workflow) does not yet check case status before creating CONTAINS edges. A concurrent ingestion during case deletion could create an orphaned MU. The `Deleting` status field is available for ingestion code to check — wiring it into `ValidateContentActivity` is deferred to a future ingestion hardening task.

4. **Phantom case edge case**: If a case exists in Redis but not in FalkorDB (due to the non-atomic create from Story 3.1), `BuildListCaseMemoryUnitIds` returns empty. Any MU Redis hashes with `caseId` pointing to this case are pre-existing orphans — not created by this story. Case deletion cleans up the Redis case keys but cannot discover MUs that were never linked via graph CONTAINS edges.

5. **Annotation MU orphaning**: `DETACH DELETE` on a MU node removes `annotates` edges but leaves connected annotation MU nodes intact (AC #4). When Story 3.6 implements annotations, `DeleteMemoryUnitAsync` must be extended to cascade-delete annotation MUs by traversing outgoing `annotates` edges before deleting the target MU.

### Previous Story Intelligence

**From Story 3.4 (ready-for-dev):**
- Story 3.4 adds `CaseId`/`CaseName` fields to `ScoredResult` and `FusedScoredResult`
- `CaseService.ResolveNamesAsync` resolves caseId to name — after deletion, resolution returns caseId as fallback
- Case existence validation added to search endpoint via `GetCaseAsync`
- `CASE_NOT_FOUND` error code pattern already established for search
- Story 3.4 does NOT create case deletion infrastructure — clean separation of concerns

**From Story 3.3 (review):**
- `CaseService` now has 8 public methods + 2 private helpers (~373 lines). Adding 2 deletion methods keeps it under ~500 lines
- `CaseValidator` has `ValidateTenantId`, `ValidateCaseId`, `ValidateMemberId`, `ValidateAddMember`, `ValidateRemoveMember` + regex patterns
- `SafeCaseIdRegex` (`^[a-zA-Z0-9\-]+$`) reusable for `memoryUnitId` validation
- `{tenantId}:case:{caseId}:members` key introduced — MUST be cleaned up during case deletion
- `ListCasesAsync` already filters `:activity` and `:members` suffixes — no new suffix patterns
- NSubstitute mocking patterns for `IConnectionMultiplexer`, `IDatabase`, `IGraphQueryBuilder`
- Integration tests use `_fixture.MemoriesClient` (HttpClient) and `MemoriesJsonContext.Options`

**From Story 3.2 (done):**
- `CaseActivityService.RecordEventAsync` is fire-and-forget safe (catches all exceptions)
- `{tenantId}:case:{caseId}:activity` stream key pattern — MUST be cleaned up during case deletion
- `CaseActivityEventType` enum has 6 values, adding 2 more for deletion events

**From Story 3.1 (done):**
- `CreateCaseAsync` creates both Redis hash and FalkorDB case node — deletion must clean up both
- `ParseCaseFromHash` currently only handles "active" and "closed" — needs "deleting" support
- `Shouldly.Case` naming conflict: qualify as `Shouldly.Case.Sensitive` in test files
- `ByteAether.Ulid` generates MU IDs (26 alphanumeric chars) — MU ID validation regex covers this

**From existing cleanup activities (compensation):**
- `CleanupSyntacticActivity` deletes `{tenantId}:mu:{muId}` — same key pattern used here
- `CleanupSemanticActivity` deletes `{tenantId}:vec:{muId}` — same key pattern
- `CleanupGraphActivity` uses `BuildDeleteMemoryUnitNode` with `DETACH DELETE` — reused directly
- These are DAPR Workflow activities; our service methods do the same operations inline (no workflow needed for MVP)

**From deferred-work.md:**
- "Case deletion (Story 3.5) must cascade-delete `{tenantId}:case:{caseId}:members` key" — addressed in Task 4.12
- "SaveDedupKeyActivity: no TTL on dedup keys" — NOT addressed by this story; dedup keys persist after MU deletion (known limitation)

### Git Intelligence

Recent commits (last 5):
- `bb30f0a` 3.2 review
- `0f8dec3` Add unit and integration tests for case management features
- `e2a5b38` Add benchmark models, scoring logic, and reporting tools
- `a0d6e4b` feat: Add search explanation metadata and serialization support
- `40b79fc` feat(search): add hybrid fusion

Patterns from recent work:
- Endpoint pattern: validate → check case exists → call service → return result
- Error response pattern: `new ErrorResponse("CODE", "Message.", "Suggestion.")`
- All public methods in CaseService are `async Task<T>` with `CancellationToken`
- ConfigureAwait(false) on all awaited calls
- Integration tests use real Redis and FalkorDB via Aspire test host

### Project Structure Notes

No new files created by this story — all changes are extensions to existing files.

Modified files (12):
- `src/Hexalith.Memories.Contracts/V1/CaseStatus.cs` (add `Deleting` enum value)
- `src/Hexalith.Memories.Contracts/V1/CaseActivityEventType.cs` (add `MemoryUnitDeleted`, `CaseDeleted`)
- `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs` (add `BuildListCaseMemoryUnitIds`, `BuildDeleteCaseNode`)
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` (implement 2 new methods)
- `src/Hexalith.Memories.Server/Cases/CaseValidator.cs` (add `ValidateMemoryUnitId`, `ValidateDeleteMemoryUnit`)
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` (add `DeleteMemoryUnitAsync`, `DeleteCaseAsync`, update `ParseCaseFromHash`)
- `src/Hexalith.Memories.Server/Program.cs` (add 2 DELETE endpoints)
- `tests/Hexalith.Memories.Contracts.Tests/V1/EnumSerializationTests.cs` (add Deleting, MemoryUnitDeleted, CaseDeleted tests)
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseValidatorTests.cs` (add MU ID validation tests)
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs` (add deletion tests)
- `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs` (add new query tests)
- `tests/Hexalith.Memories.IntegrationTests/Cases/CaseEndpointIntegrationTests.cs` (add deletion roundtrip tests)

### Testing Patterns

Follow established patterns from stories 3.1-3.4:

**Enum serialization tests** (`Contracts.Tests/V1/EnumSerializationTests.cs`):
- Roundtrip: serialize → deserialize → verify equality
- String representation: verify camelCase output (e.g., `"deleting"`, `"memoryUnitDeleted"`)
- Invalid string rejection: verify `JsonException` on unknown values

**Validator tests** (`Server.Tests/Cases/CaseValidatorTests.cs`):
- Extend existing file (do NOT create new file)
- Test valid input → null, invalid → `ErrorResponse` with correct code
- `ValidateMemoryUnitId` tests: valid ULID, null, empty, special chars (`:`, `/`), over 200 chars
- `ValidateDeleteMemoryUnit` tests: all three IDs validated, first error returned

**Graph query builder tests** (`Server.Tests/Graph/GraphQueryBuilderTests.cs`):
- Verify Cypher query string contains expected clauses
- Verify parameters dictionary contains expected keys/values
- Verify `ArgumentException` on null/empty input

**Service tests** (`Server.Tests/Cases/CaseServiceTests.cs`):
- Extend existing file
- NSubstitute mocks for `IConnectionMultiplexer`, `IDatabase`, `IGraphQueryBuilder`, `CaseActivityService`
- `DeleteMemoryUnitAsync` tests:
  - MU found: mock `HashGetAsync("caseId")` returning matching caseId → verify 3 backend deletions + activity event
  - MU not found: mock `HashGetAsync("caseId")` returning `RedisValue.Null` → verify false returned, no deletions
  - MU wrong case: mock `HashGetAsync("caseId")` returning different caseId → verify false, no deletions
  - Verify `BuildDeleteMemoryUnitNode` called with correct memoryUnitId
- `DeleteCaseAsync` tests:
  - Case with 0 MUs: mock `KeyExistsAsync` true, mock graph query returning empty result → verify case keys deleted
  - Case with 3 MUs: mock graph returning 3 IDs → verify 3x3=9 backend deletions + case cleanup
  - Case not found: mock `KeyExistsAsync` false → verify false returned, no deletions
  - Verify status set to "deleting" before cleanup (verify `HashSetAsync(caseKey, "status", "deleting")` call order)
  - Verify `BuildDeleteCaseNode` called
  - Verify all 3 case Redis keys deleted

**Integration tests** (`IntegrationTests/Cases/CaseEndpointIntegrationTests.cs`):
- Extend existing file
- Full HTTP roundtrip with real Redis and FalkorDB
- **CRITICAL**: For tests that ingest MUs then delete them, MUST wait for full indexing (CONTAINS edge present in FalkorDB) before calling DELETE. Use the existing `WaitForContainsEdgeAsync` pattern from Story 3.1 integration tests. Without this wait, `BuildListCaseMemoryUnitIds` may return empty during case deletion
- Delete MU: ingest MU → wait for indexing → verify exists → DELETE → verify 204 → verify GET case shows 0 MUs
- Delete case: create case → ingest MU(s) → wait for indexing → DELETE case → verify 204 → verify GET case returns 404
- Delete case with members: create case → add members → DELETE → verify 204 → verify members key gone
- Delete empty case (no MUs): create case → DELETE → verify 204 (no indexing wait needed)
- 404 scenarios: delete non-existent MU, delete non-existent case
- Idempotent retry: DELETE case → verify 204 → DELETE same case → verify 404

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 3, Story 3.5]
- [Source: _bmad-output/planning-artifacts/prd.md#FR27, FR35]
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns, Failure Propagation]
- [Source: _bmad-output/implementation-artifacts/3-3-case-member-management.md#Dev Notes, ADR-2, File List]
- [Source: _bmad-output/implementation-artifacts/3-4-case-scoped-and-cross-case-search.md#Dev Notes]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Case deletion cascade]
- [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs#CreateCaseAsync, ParseCaseFromHash]
- [Source: src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs#BuildDeleteMemoryUnitNode, BuildCountCaseMemoryUnits]
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs#BuildDeleteMemoryUnitNode, BuildCountCaseMemoryUnits]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/CleanupSyntacticActivity.cs]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/CleanupSemanticActivity.cs]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/CleanupGraphActivity.cs]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

### Change Log
