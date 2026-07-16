# Story 3.2: Case Status & Activity

Status: done

## Prerequisites

- Story 3.1 (Create and List Cases) must be `done` — this story extends `CaseService`, adds endpoints alongside existing case endpoints, and records activity events against cases created by Story 3.1
- Epic 1 ingestion pipeline must be `done` — ingestion workflow is modified to record activity events on success/failure

## Story

As a developer,
I want to view case status and recent activity,
So that I can monitor the health and usage of each case.

## Acceptance Criteria

1. **Given** a case with indexed memory units
   **When** I view case status (FR31)
   **Then** I see: memory unit count, last activity timestamp, and health indicators (all-backends-indexed count vs total, any failed units)

2. **Given** a case with recent operations
   **When** I view recent activity (FR36)
   **Then** I see a chronological list of events: ingestion events (unit added/failed), search queries against this case, membership changes (member added/removed)
   **And** each event includes timestamp, event type, actor (user/system), and brief description

3. **Given** a case with no activity
   **When** I view recent activity
   **Then** an empty activity list is returned with the case creation event as the only entry

## Definition of Done

- All acceptance criteria verified (AC 1-3)
- All unit, serialization, and integration tests pass
- No new compiler warnings (`warnings as errors` is enabled)
- `dotnet build` succeeds for solution
- Code follows .editorconfig conventions (file-scoped namespaces, Allman braces, sealed classes, ITANEO header)

## Recommended Implementation Order

`Task 1 (Contracts) → Task 2 (ActivityService) → Task 3 (Extend CaseService) → Task 4 (Endpoints) → Task 5 (DI) → Task 6 (Integration: CaseService) → Task 7 (Integration: IngestionWorkflow) → Task 8 (Integration: Search) → Tasks 9-13 (Tests)`

Task numbering below groups by component, but implementation should follow the dependency chain above. Task 2 (CaseActivityService) must be complete before Tasks 3, 6, 7, 8 can compile.

## Tasks / Subtasks

- [x] Task 1: Create activity event contracts in Contracts/V1 (AC: 2, 3)
    - [x] 1.1 Create `src/Hexalith.Memories.Contracts/V1/CaseActivityEventType.cs` as enum with `CamelCaseStringEnumConverter<CaseActivityEventType>`:
        ```csharp
        [JsonConverter(typeof(CamelCaseStringEnumConverter<CaseActivityEventType>))]
        public enum CaseActivityEventType
        {
            CaseCreated,
            MemoryUnitIngested,
            IngestionFailed,
            SearchExecuted,
            MemberAdded,
            MemberRemoved,
        }
        ```
        Note: `MemberAdded` and `MemberRemoved` are defined now for forward compatibility with Story 3.3 but not recorded in this story
    - [x] 1.2 Create `src/Hexalith.Memories.Contracts/V1/CaseActivityEvent.cs` as `public sealed record`:
        ```csharp
        public sealed record CaseActivityEvent(
            string Id,                             // Redis Stream entry ID (e.g., "1712345678901-0")
            DateTimeOffset Timestamp,              // Event timestamp (parsed from Stream ID)
            CaseActivityEventType EventType,       // Type of event
            string Actor,                          // User ID or "system"
            string Description,                    // Human-readable event description
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            string? MemoryUnitId);                 // Associated memory unit (for ingestion events)
        ```
    - [x] 1.3 Create `src/Hexalith.Memories.Contracts/V1/CaseStatusDetail.cs` as `public sealed record`:
        ```csharp
        public sealed record CaseStatusDetail(
            string Id,
            string TenantId,
            string Name,
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            string? Description,
            CaseStatus Status,
            DateTimeOffset CreatedAt,
            DateTimeOffset LastUpdated,
            int MemoryUnitCount,                   // Total indexed in all backends (CONTAINS edge count)
            DateTimeOffset? LastActivityAt,         // Timestamp of most recent activity event
            int IndexedCount,                      // Fully indexed memory units (same as MemoryUnitCount)
            int FailedCount);                      // Count of failed ingestion attempts (from activity stream)
        ```
        **Design note:** `IndexedCount` equals `MemoryUnitCount` because CONTAINS edges are only created after all 3 backends index successfully (via `IngestionWorkflow` compensation logic). `FailedCount` is derived from the activity stream, not from graph edges. Together they give the health picture: `IndexedCount` + `FailedCount` = total submitted.
        **Composition tradeoff:** `CaseStatusDetail` flattens fields from `Case` rather than composing (`Case Case, ...`) for API ergonomics — callers get a single flat JSON object. Tradeoff: if `Case` adds a field, `CaseStatusDetail` must be updated too. Acceptable for MVP (Case record is stable after Story 3.1)
    - [x] 1.4 Register new types in `MemoriesJsonContext`:
        ```csharp
        [JsonSerializable(typeof(CaseActivityEvent))]
        [JsonSerializable(typeof(CaseActivityEventType))]
        [JsonSerializable(typeof(CaseStatusDetail))]
        [JsonSerializable(typeof(List<CaseActivityEvent>))]
        ```
        **AOT CRITICAL:** Register `List<CaseActivityEvent>` (concrete collection type) for correct source generator code generation

- [x] Task 2: Create `CaseActivityService` in Server/Cases (AC: 2, 3)
    - [x] 2.1 Create `src/Hexalith.Memories.Server/Cases/CaseActivityService.cs` as `internal sealed class` with constructor-injected dependencies:
        - `[FromKeyedServices("redis")] IConnectionMultiplexer redis` — for Redis Stream operations
        - `ILogger<CaseActivityService> logger`
    - [x] 2.2 Define Redis Stream key pattern: `{tenantId}:case:{caseId}:activity`
    - [x] 2.3 Implement `RecordEventAsync(string tenantId, string caseId, CaseActivityEventType eventType, string actor, string description, string? memoryUnitId = null, CancellationToken cancellationToken = default)`:
        - Use `IDatabase.StreamAddAsync(key, fields)` where key = `{tenantId}:case:{caseId}:activity`
        - Fields to write using `NameValueEntry` (NOT `HashEntry` — streams use `NameValueEntry`, hashes use `HashEntry`; they are not interchangeable): `type` (camelCase enum string), `actor`, `description`, `memoryUnitId` (only if not null)
        - Use auto-generated Stream ID (`*`) — Redis generates millisecond-precision timestamp ID automatically
        - **Error handling:** Wrap in try-catch, log warning on failure. Activity recording must NEVER throw — it is a non-critical side-effect. Return `bool` (true on success, false on failure)
        - **Do NOT create the stream explicitly** — `XADD` auto-creates the stream on first write
    - [x] 2.4 Implement `GetRecentActivityAsync(string tenantId, string caseId, int maxEvents = 50, CancellationToken cancellationToken = default)`:
        - Use `IDatabase.StreamRangeAsync(key, minId: null, maxId: null, count: maxEvents, messageOrder: Order.Descending)` to get most recent events first
        - Parse each `StreamEntry` into `CaseActivityEvent`:
            - `Id` = `entry.Id.ToString()`
            - `Timestamp` = Parse Redis Stream ID to `DateTimeOffset` (format: `{milliseconds}-{sequence}` — extract milliseconds part and convert via `DateTimeOffset.FromUnixTimeMilliseconds`)
            - `EventType` = Parse from `type` field using case-insensitive string match
            - `Actor` = from `actor` field
            - `Description` = from `description` field
            - `MemoryUnitId` = from `memoryUnitId` field (null if not present)
        - **CRITICAL null guard:** `StreamRangeAsync` returns `null` (not empty array) when the stream key doesn't exist. Guard with `?? Array.Empty<StreamEntry>()` before iterating. Without this, `foreach` throws `NullReferenceException`
        - Return `List<CaseActivityEvent>` — empty list if stream doesn't exist
        - **Clamp maxEvents:** `Math.Clamp(maxEvents, 1, 500)` to prevent unbounded reads
    - [x] 2.5 Implement `GetFailedCountAsync(string tenantId, string caseId, CancellationToken cancellationToken = default)`:
        - Read ALL events from the activity stream and count entries where `type` == `"ingestionFailed"`
        - **Design intent:** Intentionally reads raw `StreamEntry` arrays and counts matching `type` fields instead of reusing `GetRecentActivityAsync` — avoids the overhead of parsing every entry into `CaseActivityEvent` objects when only a count is needed
        - **Null guard:** Apply the same `?? Array.Empty<StreamEntry>()` guard as Task 2.4
        - **Performance note:** For MVP, reading the full stream is acceptable (expected < 1000 events per case). Future optimization: maintain a separate Redis counter incremented on each failure, or use Redis Stream consumer groups
        - On error, log warning and return 0 — never throw
    - [x] 2.6 Implement `GetLastActivityTimestampAsync(string tenantId, string caseId, CancellationToken cancellationToken = default)`:
        - Use `IDatabase.StreamRangeAsync(key, minId: null, maxId: null, count: 1, messageOrder: Order.Descending)` to get the single most recent event
        - Parse the Stream entry ID to `DateTimeOffset` (same parsing as 2.4)
        - Return `DateTimeOffset?` — null if stream doesn't exist or is empty

- [x] Task 3: Extend `CaseService` with status computation (AC: 1)
    - [x] 3.1 Add `CaseActivityService` as constructor dependency to `CaseService`:
        ```csharp
        private readonly CaseActivityService _activityService;
        ```
        Add to constructor parameters — **this modifies the existing constructor signature**, update DI registration in Task 5.
        **WARNING: Breaking change for existing tests.** All tests in `CaseServiceTests.cs` that construct `CaseService` will fail until the constructor call is updated to include the new `CaseActivityService` parameter. Fix by creating a `CaseActivityService` with a mocked `IConnectionMultiplexer` and passing it to `CaseService`
    - [x] 3.2 Implement `GetCaseStatusAsync(string tenantId, string caseId, CancellationToken cancellationToken)`:
        - First call `GetCaseAsync(tenantId, caseId, cancellationToken)` — if null, return null
        - Then query activity service for health indicators in parallel:
            ```csharp
            Task<DateTimeOffset?> lastActivityTask = _activityService.GetLastActivityTimestampAsync(tenantId, caseId, cancellationToken);
            Task<int> failedCountTask = _activityService.GetFailedCountAsync(tenantId, caseId, cancellationToken);
            await Task.WhenAll(lastActivityTask, failedCountTask);
            ```
        - Construct `CaseStatusDetail`:
            - Copy fields from `Case` record
            - `IndexedCount` = `case.MemoryUnitCount` (CONTAINS edges = fully indexed)
            - `FailedCount` = from activity service
            - `LastActivityAt` = from activity service (null if no activity recorded)
        - Return `CaseStatusDetail`

- [x] Task 4: Create API endpoints (AC: 1, 2, 3)
    - [x] 4.1 Add `GET /api/tenants/{tenantId}/cases/{caseId}/status` endpoint in `Program.cs`:

        ```csharp
        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/status", async (
            string tenantId,
            string caseId,
            CaseService caseService,
            CancellationToken cancellationToken) =>
        {
            // TODO: Extract TenantIdValidationFilter when endpoint count > 5 (Story 3.4+)
            try { TenantIdGuard.Validate(tenantId); }
            catch (ArgumentException) { return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed.")); }

            CaseStatusDetail? status = await caseService.GetCaseStatusAsync(tenantId, caseId, cancellationToken);
            return status is null
                ? Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."))
                : Results.Ok(status);
        });
        ```

    - [x] 4.2 Add `GET /api/tenants/{tenantId}/cases/{caseId}/activity` endpoint in `Program.cs`:

        ```csharp
        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/activity", async (
            string tenantId,
            string caseId,
            int? limit,
            CaseService caseService,
            CaseActivityService activityService,
            CancellationToken cancellationToken) =>
        {
            // TODO: Extract TenantIdValidationFilter when endpoint count > 5 (Story 3.4+)
            try { TenantIdGuard.Validate(tenantId); }
            catch (ArgumentException) { return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed.")); }

            // Verify case exists before querying activity
            Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            if (caseResult is null)
            {
                return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
            }

            int effectiveLimit = Math.Clamp(limit ?? 50, 1, 500);
            List<CaseActivityEvent> events = await activityService.GetRecentActivityAsync(tenantId, caseId, effectiveLimit, cancellationToken);
            return Results.Ok(events);
        });
        ```

        **AC #3 clarification:** If the activity list is empty AND the case exists, this indicates the case creation event was not recorded (best-effort recording failed). Return the empty list as-is — do NOT synthesize a fake creation event. AC #3 is aspirational under normal conditions; the system does not guarantee activity recording (per ADR-2)

    - [x] 4.3 Place new endpoints AFTER the existing `GET /api/tenants/{tenantId}/cases/{caseId}` endpoint and BEFORE the `app.MapGet("/api/search", ...)` endpoint — maintain logical grouping of case endpoints

- [x] Task 5: Update DI registration (AC: 1, 2)
    - [x] 5.1 Register `CaseActivityService` in `Program.cs` as **singleton** (not scoped):
        ```csharp
        builder.Services.AddSingleton<CaseActivityService>(sp =>
            new CaseActivityService(
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
                sp.GetRequiredService<ILogger<CaseActivityService>>()));
        ```
        **Why singleton:** Task 8 uses fire-and-forget (`_ = activityService.RecordEventAsync(...)`) in the search endpoint. If `CaseActivityService` were scoped, the DI scope (and `IConnectionMultiplexer` reference) could be disposed before the fire-and-forget task completes, causing silent failures under load. Singleton avoids this because `IConnectionMultiplexer` is already registered as singleton (keyed). Place AFTER the existing `builder.Services.AddScoped<CaseService>();` line
    - [x] 5.2 Update `CaseService` constructor to accept `CaseActivityService` — since `CaseService` is `AddScoped` and `CaseActivityService` is now `AddSingleton`, a scoped service can safely depend on a singleton (scoped → singleton is always valid; the reverse — singleton → scoped — would be a captive dependency error). Standard constructor injection works. **No manual factory needed**

- [x] Task 6: Integration — Record case creation activity (AC: 3)
    - [x] 6.1 Modify `CaseService.CreateCaseAsync()` to record a `CaseCreated` activity event after successful creation:
        ```csharp
        // After Redis hash write + FalkorDB node creation, record activity
        _ = await _activityService.RecordEventAsync(
            input.TenantId,
            caseId,
            CaseActivityEventType.CaseCreated,
            "system",
            $"Case '{input.Name}' created",
            memoryUnitId: null,
            cancellationToken);
        ```
    - [x] 6.2 Place the activity recording AFTER both storage writes succeed but BEFORE the return statement
    - [x] 6.3 **Do NOT wrap in try-catch** — `RecordEventAsync` already handles errors internally and never throws. The `_ = await` discards the bool return (success/failure doesn't affect case creation). **Note:** This differs from Task 7.5 which DOES wrap in try-catch — that's because Task 7.5 calls `RecordEventAsync` indirectly via `context.CallActivityAsync`, and `CallActivityAsync` can throw DAPR infrastructure exceptions (serialization, timeout) even when the inner activity logic catches application exceptions

- [x] Task 7: Integration — Record ingestion activity in workflow (AC: 2)
    - [x] 7.1 Create `src/Hexalith.Memories.Server/Activities/Ingestion/RecordCaseActivityActivity.cs`:

        ```csharp
        internal sealed class RecordCaseActivityActivity : WorkflowActivity<CaseActivityInput, bool>
        {
            private readonly CaseActivityService _activityService;

            public RecordCaseActivityActivity(CaseActivityService activityService)
            {
                _activityService = activityService;
            }

            public override async Task<bool> RunAsync(WorkflowActivityContext context, CaseActivityInput input)
            {
                return await _activityService.RecordEventAsync(
                    input.TenantId,
                    input.CaseId,
                    input.EventType,
                    input.Actor,
                    input.Description,
                    input.MemoryUnitId);
            }
        }
        ```

    - [x] 7.2 Create `src/Hexalith.Memories.Contracts/V1/CaseActivityInput.cs` as `public sealed record` — **MUST be in Contracts**, not co-located in Server, because DAPR workflow serializes activity inputs via the shared `MemoriesJsonContext`:
        ```csharp
        public sealed record CaseActivityInput(
            string TenantId,
            string CaseId,
            CaseActivityEventType EventType,
            string Actor,
            string Description,
            string? MemoryUnitId);
        ```
        **AOT CRITICAL:** Register `CaseActivityInput` in `MemoriesJsonContext` with `[JsonSerializable(typeof(CaseActivityInput))]` — DAPR workflow serializes activity inputs/outputs via the shared JSON context
    - [x] 7.3 Register activity in `Program.cs` workflow configuration:
        ```csharp
        options.RegisterActivity<RecordCaseActivityActivity>();
        ```
    - [x] 7.4 Modify `IngestionWorkflow.RunAsync()` to record activity on **success** — add AFTER the `SaveDedupKeyActivity` call (line ~208) and BEFORE `currentStatus = TransitionStatus(...)`:
        ```csharp
        // Record ingestion success activity (best-effort, no retry needed)
        await context.CallActivityAsync<bool>(
            nameof(RecordCaseActivityActivity),
            new CaseActivityInput(
                input.TenantId,
                input.CaseId,
                CaseActivityEventType.MemoryUnitIngested,
                input.IngestedBy,
                $"Memory unit {memoryUnitId} indexed from {input.SourceUri}",
                memoryUnitId));
        ```
        **No WorkflowTaskOptions (no retry)** — activity recording is best-effort. If it fails, ingestion still succeeds. The activity service internally catches exceptions
    - [x] 7.5 Modify `IngestionWorkflow.RunAsync()` to record activity on **failure** — add in the first `catch` block for indexing failure (line ~151), AFTER the `CompensateAsync` call:
        ```csharp
        // Best-effort activity recording for failure
        try
        {
            await context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity),
                new CaseActivityInput(
                    input.TenantId,
                    input.CaseId,
                    CaseActivityEventType.IngestionFailed,
                    input.IngestedBy,
                    $"Ingestion failed for {input.SourceUri} at stage {currentStage}",
                    memoryUnitId));
        }
        catch
        {
            // Activity recording failure must not mask the original ingestion failure
        }
        ```
        Wrap in try-catch because we're already in an error path and about to re-throw
    - [x] 7.6 **DAPR Workflow replay safety:** The `RecordCaseActivityActivity` uses `XADD` with auto-generated Redis Stream IDs. On workflow replay, this could create duplicate entries. This is **acceptable for MVP** — activity streams are for human consumption and duplicate entries don't break any logic. For production: use deterministic IDs (e.g., `context.NewGuid()`) to achieve idempotent `XADD`
    - [x] 7.7 **In-flight workflow compatibility:** Adding a new activity to an existing workflow is safe as long as no in-flight workflows exist. Since this is MVP with no production traffic, this is acceptable. Document the constraint

- [x] Task 8: Integration — Record search activity (AC: 2)
    - [x] 8.1 Modify the search endpoint in `Program.cs` — add search activity recording when `caseId` is provided
    - [x] 8.2 After the search result is computed and BEFORE returning `Results.Ok(...)`, add:
        ```csharp
        if (!string.IsNullOrWhiteSpace(caseId))
        {
            // Fire-and-forget: record search activity without blocking the response
            // CaseActivityService internally catches all exceptions, so this is safe
            _ = activityService.RecordEventAsync(
                tenantId,
                caseId,
                CaseActivityEventType.SearchExecuted,
                "system",  // MVP: no user identity in search path
                $"Search '{query}' executed via {axis} axis",
                memoryUnitId: null);
        }
        ```
    - [x] 8.3 Inject `CaseActivityService activityService` into the search endpoint lambda parameters. **Parameter count risk:** The search endpoint already has 10+ parameters. If adding `CaseActivityService` exceeds the ASP.NET Minimal API binding limit, extract the search endpoint into a `static` method or use `[AsParameters]` request class
    - [x] 8.4 **Performance:** `RecordEventAsync` is not awaited (fire-and-forget pattern). Redis `XADD` is sub-millisecond, but we don't want even that latency in the search hot path. The `_ =` suppresses the CS4014 warning
    - [x] 8.5 **Use a local function** to avoid duplicating the recording logic across 5+ return sites in the search endpoint:
        ```csharp
        void RecordSearchActivity()
        {
            if (!string.IsNullOrWhiteSpace(caseId))
            {
                _ = activityService.RecordEventAsync(tenantId, caseId!, CaseActivityEventType.SearchExecuted, "system", $"Search '{query}' via {axis}", null);
            }
        }
        ```
        Call `RecordSearchActivity()` before each `return Results.Ok(...)` in the search endpoint. This prevents accidental double-recording and keeps the guard logic in one place
    - [x] 8.6 **DI clarification:** `CaseActivityService` is registered as singleton (Task 5.1) — inject it normally in the search endpoint lambda, **not via keyed DI**. Only `IConnectionMultiplexer` uses `[FromKeyedServices]`. Singleton registration means the fire-and-forget pattern is safe (no scope disposal risk)

- [x] Task 9: Unit tests for Contracts (AC: 1, 2)
    - [x] 9.1 Create `tests/Hexalith.Memories.Contracts.Tests/V1/CaseStatusDetailSerializationTests.cs`:
        - Test `CaseStatusDetail` round-trip serialization with `MemoriesJsonContext.Options`
        - Verify camelCase property names in JSON output
        - Verify `Description` null omission behavior
        - Verify `LastActivityAt` null handling
        - Test with `IndexedCount > 0` and `FailedCount > 0` to verify health indicators serialize correctly
    - [x] 9.2 Create `tests/Hexalith.Memories.Contracts.Tests/V1/CaseActivityEventSerializationTests.cs`:
        - Test `CaseActivityEvent` round-trip serialization
        - Verify `MemoryUnitId` null omission
        - Test `CaseActivityEventType` serializes as camelCase strings (`"caseCreated"`, `"memoryUnitIngested"`, `"ingestionFailed"`, `"searchExecuted"`)
        - Test `List<CaseActivityEvent>` round-trip for AOT validation
    - [x] 9.3 Add `CaseActivityEventType` to existing `EnumSerializationTests.cs`:
        - Test ALL enum values serialize/deserialize correctly as camelCase strings

- [x] Task 10: Unit tests for CaseActivityService (AC: 2, 3)
    - [x] 10.1 Create `tests/Hexalith.Memories.Server.Tests/Cases/CaseActivityServiceTests.cs`:
        - Mock `[FromKeyedServices("redis")] IConnectionMultiplexer` with NSubstitute
        - Test `RecordEventAsync` calls `StreamAddAsync` with correct key pattern and fields
        - Test `RecordEventAsync` returns true on success, false on exception (no throw)
        - Test `RecordEventAsync` does NOT include `memoryUnitId` field when null
        - Test `GetRecentActivityAsync` returns parsed events in reverse chronological order
        - Test `GetRecentActivityAsync` returns empty list when stream doesn't exist
        - Test `GetRecentActivityAsync` clamps maxEvents to [1, 500]: verify `maxEvents = 0` clamps to 1, `maxEvents = int.MaxValue` clamps to 500, `maxEvents = 50` passes through unchanged
        - Test `GetFailedCountAsync` counts only `ingestionFailed` events
        - Test `GetFailedCountAsync` returns 0 when stream doesn't exist
        - Test `GetLastActivityTimestampAsync` returns timestamp of most recent event
        - Test `GetLastActivityTimestampAsync` returns null for empty/nonexistent stream
    - [x] 10.2 **Redis Stream mocking:** `IDatabase.StreamAddAsync(RedisKey, NameValueEntry[], RedisValue?, int?, bool, CommandFlags)` and `IDatabase.StreamRangeAsync(RedisKey, RedisValue?, RedisValue?, int?, Order, CommandFlags)` — mock these specific overloads. StackExchange.Redis uses `RedisValue` for stream IDs and `NameValueEntry` for fields
    - [x] 10.3 **Stream ID parsing test:** Create separate test methods for the millisecond-to-DateTimeOffset conversion logic:
        - Valid ID: `"1712345678901-0"` → `DateTimeOffset.FromUnixTimeMilliseconds(1712345678901)` → `2024-04-05T19:41:18.901+00:00`
        - Malformed ID (no dash): guard against `FormatException` — return fallback `DateTimeOffset.MinValue` or skip entry
        - Empty stream entries: verify service handles `StreamEntry` with null/empty values gracefully
    - [x] 10.4 **Redis connection failure test:** Test that `RecordEventAsync` catches `RedisConnectionException` and `RedisTimeoutException`, logs warning, and returns `false` — never throws
    - Use Shouldly assertions and `NullLogger<CaseActivityService>.Instance` (matching existing test patterns — NSubstitute cannot proxy `ILogger<T>` for internal types)

- [x] Task 11: Unit tests for CaseService.GetCaseStatusAsync (AC: 1)
    - [x] 11.1 Extend `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs`:
        - Test `GetCaseStatusAsync` returns null when case doesn't exist
        - Test `GetCaseStatusAsync` returns `CaseStatusDetail` with correct health indicators
        - Test `IndexedCount` equals `MemoryUnitCount` (verified via graph edge count)
        - Test `FailedCount` from activity service is included
        - Test `LastActivityAt` from activity service is included
    - [x] 11.2 **New mock dependency:** Add `CaseActivityService` mock to `CaseServiceTests`. Since `CaseActivityService` is a concrete class (not an interface), use NSubstitute `Substitute.ForPartsOf<CaseActivityService>(...)` or extract a method to test. **Better approach:** Make the activity service methods virtual, or test through the service directly by mocking Redis
    - [x] 11.3 **Alternative if mocking is problematic:** Test `GetCaseStatusAsync` by creating a real `CaseActivityService` with a mocked `IConnectionMultiplexer`, and inject that into `CaseService`. This avoids partial mocking

- [x] Task 12: Integration test for workflow activity recording (AC: 2)
    - [x] 12.1 Create `tests/Hexalith.Memories.Server.Tests/Cases/RecordCaseActivityActivityTests.cs`:
        - Test that `RecordCaseActivityActivity.RunAsync` calls `CaseActivityService.RecordEventAsync` with correct parameters
        - Test success path returns true
        - Test failure path returns false (activity service catches errors)
    - [x] 12.2 Create `tests/Hexalith.Memories.Contracts.Tests/V1/CaseActivityInputSerializationTests.cs`:
        - Test `CaseActivityInput` round-trip serialization (required for DAPR workflow serialization)
    - [x] 12.3 Mark integration tests with `[Trait("Category", "Integration")]` where appropriate

- [x] Task 13: **BLOCKER** — Workflow resilience test for activity recording failure (AC: 2)
    - [x] 13.1 Add test in `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs`:
        - **Test: IngestionWorkflow succeeds even when RecordCaseActivityActivity throws.**
        - Mock `RecordCaseActivityActivity` to throw `RedisConnectionException`
        - Assert that the workflow still returns `IngestionResult` with `Status == MemoryUnitStatus.Indexed`
        - Assert that the memory unit is fully indexed in all 3 backends despite the activity recording failure
        - This test enforces the "best-effort" contract — activity recording failure must NEVER cause ingestion failure
    - [x] 13.2 Add test for failure path:
        - Mock both indexing (to fail) and `RecordCaseActivityActivity` (to throw)
        - Assert that the original indexing exception propagates, not the activity recording exception
        - This verifies the try-catch in Task 7.5 correctly masks the activity recording failure

### Review Findings

- [x] [Review][Patch] ListCasesAsync scans activity stream keys as case hashes, which can trigger Redis WRONGTYPE failures once `:activity` streams exist [`src/Hexalith.Memories.Server/Cases/CaseService.cs:98`] — fixed
- [x] [Review][Patch] GetRecentActivityAsync can still throw on Redis failures even though the story requires CaseActivityService methods to degrade safely [`src/Hexalith.Memories.Server/Cases/CaseActivityService.cs:64`] — fixed
- [x] [Review][Patch] RecordEventAsync writes PascalCase activity `type` values instead of the story-required camelCase enum strings [`src/Hexalith.Memories.Server/Cases/CaseActivityService.cs:44`] — fixed
- [x] [Review][Defer] Case creation is non-atomic across Redis and FalkorDB [`src/Hexalith.Memories.Server/Cases/CaseService.cs:53`] — deferred, pre-existing

## Error Code Registry

| Code                | HTTP | Returned By                            | Suggestion                                         |
| ------------------- | ---- | -------------------------------------- | -------------------------------------------------- |
| `INVALID_TENANT_ID` | 400  | Endpoints 4.1, 4.2 via `TenantIdGuard` | "Only alphanumeric and hyphens allowed."           |
| `CASE_NOT_FOUND`    | 404  | Endpoints 4.1, 4.2                     | "Run 'memories case list' to see available cases." |

## Dev Notes

### Quick Start (Read First)

1. **Implementation order:** Task 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9-13 (follow dependency chain)
2. **New files:** 4 contracts in `Contracts/V1/` (`CaseActivityEventType.cs`, `CaseActivityEvent.cs`, `CaseStatusDetail.cs`, `CaseActivityInput.cs`), 1 service in `Server/Cases/`, 1 activity in `Server/Activities/Ingestion/`, endpoints in `Program.cs`, 5+ test files
3. **Modified files:** `CaseService.cs` (add activity dependency + `GetCaseStatusAsync` + creation recording), `IngestionWorkflow.cs` (add activity recording on success/failure), `Program.cs` (DI + 2 endpoints + search activity), `MemoriesJsonContext.cs` (new type registrations)
4. **Critical:** `CaseActivityService` must NEVER throw exceptions — all methods catch internally. Activity recording is best-effort
5. **Anti-patterns:** No DAPR Actor for activity, no separate database for events, no `KEYS` command, no blocking the search hot path
6. **Redis Streams key:** `{tenantId}:case:{caseId}:activity` — auto-created on first `XADD`, no explicit creation needed

### Architecture Compliance

- **Contract types:** `public sealed record` in `Contracts/V1/` — matches `Case.cs`, `SearchResult.cs` patterns
- **Service class:** `internal sealed class` in `Server/Cases/` — matches `CaseService` pattern
- **Workflow activity:** `internal sealed class` in `Server/Activities/Ingestion/` — matches `ValidateContentActivity`, `CheckIdempotencyActivity` patterns
- **Endpoints:** Minimal API in `Program.cs` — matches existing `MapGet` patterns
- **Error responses:** `ErrorResponse(Code, Message, Suggestion)` — matches existing pattern
- **JSON serialization:** camelCase via `MemoriesJsonContext` with AOT source generators
- **Async pattern:** `Task<T>` with `Async` suffix, `CancellationToken` on every async method
- **ITANEO copyright header** on all new files
- **File-scoped namespaces**, Allman braces, `sealed` on implementation classes

### Storage Strategy

- **Activity events** stored in Redis Streams: key `{tenantId}:case:{caseId}:activity`
    - Redis Streams are append-only, chronologically ordered, and auto-ID'd with millisecond timestamps
    - Fields per entry: `type`, `actor`, `description`, `memoryUnitId` (optional)
    - Auto-created on first `XADD` — no explicit stream creation needed
    - No TTL/MAXLEN in MVP — streams grow unbounded. Future: add `MAXLEN ~1000` to cap stream size
- **Case status** is computed, not stored — health indicators are derived from graph edge count + activity stream
- **Tenant isolation:** Stream keys are prefixed with `{tenantId}:case:` — same physical isolation as case metadata

### Critical Anti-Patterns to Avoid

1. **DO NOT** create a DAPR Actor for activity tracking — activity events are simple append-only writes, not stateful singletons requiring turn-based access
2. **DO NOT** create a separate database or table for activity events — Redis Streams provide the right abstraction with minimal infrastructure
3. **DO NOT** use `KEYS` or `SCAN` to find activity streams — the stream key is deterministic from tenantId + caseId
4. **DO NOT** await activity recording in the search hot path — use fire-and-forget pattern (`_ = service.RecordEventAsync(...)`)
5. **DO NOT** let activity recording failures propagate — `RecordEventAsync` catches all exceptions internally
6. **DO NOT** use `XREAD` (blocking consumer) — use `XRANGE`/`XREVRANGE` for on-demand reads. Activity events are pull-based, not push-based
7. **DO NOT** store health indicators in Redis Hash — derive from graph edges + activity stream to avoid stale data
8. **DO NOT** add `CaseService` as a dependency of `CaseActivityService` — this creates a circular DI dependency that crashes at startup. `CaseActivityService` must only depend on `IConnectionMultiplexer` and `ILogger`, never on `CaseService`
9. **DO NOT** duplicate activity events via `ILogger.LogInformation` — `CaseActivityService` is the single authoritative source for case activity. Logging the same events via structured logging creates confusion about which source is canonical and wastes disk

### Existing Code to Reuse

- `CaseService` (`src/Hexalith.Memories.Server/Cases/CaseService.cs`) — extend with `GetCaseStatusAsync`, add `CaseActivityService` dependency, add activity recording to `CreateCaseAsync`
- `CaseValidator` (`src/Hexalith.Memories.Server/Cases/CaseValidator.cs`) — no changes needed, reused for case existence checks
- `IGraphQueryBuilder.BuildCountCaseMemoryUnits(caseId)` — already exists from Story 3.1, provides indexed count for health indicators
- `ErrorResponse` (`src/Hexalith.Memories.Contracts/V1/ErrorResponse.cs`) — for error responses
- `MemoriesJsonContext` (`src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`) — register new types
- `CamelCaseStringEnumConverter<T>` — for `CaseActivityEventType` enum serialization
- `TenantIdGuard.Validate` — for tenant ID validation in new endpoints
- `IngestionWorkflow` (`src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`) — modify to record activity on success/failure
- `[FromKeyedServices("redis")] IConnectionMultiplexer` — existing keyed DI for Redis
- `NullLogger<T>.Instance` — for test logger (matching existing test patterns)

### Redis Streams API Reference (StackExchange.Redis)

```csharp
// Write an event
await db.StreamAddAsync(
    key: $"{tenantId}:case:{caseId}:activity",
    streamPairs: [
        new NameValueEntry("type", "memoryUnitIngested"),
        new NameValueEntry("actor", "user-123"),
        new NameValueEntry("description", "Memory unit abc indexed"),
        new NameValueEntry("memoryUnitId", "abc"),
    ]);

// Read recent events (newest first)
StreamEntry[] entries = await db.StreamRangeAsync(
    key: $"{tenantId}:case:{caseId}:activity",
    minId: null,  // from beginning
    maxId: null,  // to latest
    count: 50,
    messageOrder: Order.Descending);

// Parse Stream ID to timestamp
string entryId = "1712345678901-0";
long millis = long.Parse(entryId.Split('-')[0]);
DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(millis);

// Parse StreamEntry fields (CRITICAL: use NameValueEntry, not HashEntry)
// StreamRangeAsync returns null for nonexistent streams — always guard
StreamEntry[]? rawEntries = await db.StreamRangeAsync(key, null, null, 50, Order.Descending);
StreamEntry[] entries = rawEntries ?? Array.Empty<StreamEntry>();

foreach (StreamEntry entry in entries)
{
    // Read a field value — returns RedisValue.Null if field not present
    string? type = entry.Values.FirstOrDefault(v => v.Name == "type").Value;
    string? actor = entry.Values.FirstOrDefault(v => v.Name == "actor").Value;
    string? memoryUnitId = entry.Values.FirstOrDefault(v => v.Name == "memoryUnitId").Value;
    // RedisValue implicit conversion to string? returns null for RedisValue.Null
}
```

### Package Management

- **Directory.Packages.props** for all NuGet versions — never add version in `.csproj`
- `.slnx` solution format — never create `.sln`
- **No new NuGet packages needed** — Redis Streams support is built into StackExchange.Redis (already a dependency)

### Previous Story Intelligence (Story 3.1)

**Key learnings from Story 3.1 development:**

- `Guid.CreateVersion7().ToString("N")` is used for ID generation (not ULID — `System.Ulid` is not in .NET 10 BCL)
- `Shouldly.Case` naming conflict with `Hexalith.Memories.Contracts.V1.Case` — resolved by qualifying `Shouldly.Case.Sensitive`. Watch for the same conflict with `CaseActivityEventType` or `CaseStatusDetail` if any Shouldly assertion method collides
- `NSubstitute` cannot proxy `ILogger<T>` for internal types — use `NullLogger<T>.Instance` instead
- `IServer.Keys(pattern:)` uses SCAN internally — safe for production
- Keyed DI pattern: `[FromKeyedServices("redis")] IConnectionMultiplexer` is used consistently across services and activities
- `CaseService` is registered as `AddScoped` (not singleton) because it depends on scoped `IConnectionMultiplexer.GetDatabase()` calls
- Redis Hash field values are always strings — parse `DateTimeOffset` with `DateTimeOffset.TryParse()`, parse enums with string comparison
- FalkorDB queries use tenant ID as graph database name: `falkor.QueryAsync(tenantId, query, parameters)`

### Known Scope Gaps (Deferred)

- **Membership change activity:** `MemberAdded`/`MemberRemoved` event types are defined in the enum but not recorded in this story — deferred to Story 3.3 (Case Member Management)
- **Stream size management:** No MAXLEN on activity streams in MVP. For production, cap at ~1000 events with approximate trimming: `XADD ... MAXLEN ~ 1000`
- **Activity pagination:** The `GET .../activity` endpoint returns a flat list with a `limit` parameter. Redis Stream-native cursor pagination (using entry IDs as cursors) is deferred — current approach is sufficient for MVP
- **Workflow replay idempotency:** On DAPR workflow replay, `RecordCaseActivityActivity` may create duplicate stream entries. Acceptable for MVP (activity streams are informational). Production fix: use deterministic entry IDs
- **Search actor identity:** Search activity events record `actor` as `"system"` because the search endpoint has no user identity in MVP. Phase 1.5 adds `TenantAuthorizationMiddleware` which will provide authenticated identity
- **Aggregated health dashboard:** FR31 mentions "health indicators" — this story provides per-case health. A tenant-wide health dashboard across all cases is out of scope
- **Real-time activity streaming:** WebSocket or SSE-based real-time activity feed is out of scope — this story provides pull-based activity retrieval

### Architecture Decision Records

- **ADR-1: Redis Streams over Redis List/Sorted Set for activity events.** Redis Streams provide auto-timestamped IDs, built-in ordering, and efficient range queries. Lists would require manual timestamp management. Sorted Sets would work but Streams are semantically correct for event logging
- **ADR-2: Activity recording is best-effort (fire-and-forget).** Activity events are observational, not transactional. Losing an activity event is acceptable; blocking a search query or ingestion workflow to guarantee activity recording is not. All recording methods catch exceptions and return bool
- **ADR-3: Health indicators derived, not cached.** `IndexedCount` from graph edge count + `FailedCount` from activity stream. No cached counters that could go stale. Acceptable performance for MVP (<100 cases, <1000 events per case). **Documented escape hatch:** When stream size grows beyond ~5000 events, replace `GetFailedCountAsync` full-stream scan with an atomic Redis counter at `{tenantId}:case:{caseId}:failed-count`, incremented via `INCR` in `RecordEventAsync` when event type is `IngestionFailed`. This is a 2-line change in `CaseActivityService` — no contract or endpoint changes needed
- **ADR-4: Modify IngestionWorkflow directly rather than pub/sub.** Adding a workflow activity is simpler and more reliable than setting up a DAPR pub/sub subscription for ingestion events. The tradeoff is modifying an existing workflow, but the change is additive (new activity call) and non-breaking

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 3, Story 3.2]
- [Source: _bmad-output/planning-artifacts/architecture.md — Memory Organization FR26-37, Redis Streams, Workflow Activities]
- [Source: _bmad-output/planning-artifacts/prd.md — FR31 Case status, FR36 Activity events, FR65 ingested_by provenance]
- [Source: _bmad-output/implementation-artifacts/3-1-create-and-list-cases.md — CaseService patterns, keyed DI, Redis Hash operations, test patterns]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- NSubstitute StreamRangeAsync nullable return type mismatch — resolved with lambda `.Returns(_ => ...)` pattern
- NSubstitute StreamAddAsync overload mismatch — resolved with `ReceivedCalls()` inspection pattern
- Success-path activity recording needed try-catch wrapper for best-effort semantics

### Completion Notes List

- All 3 acceptance criteria satisfied (AC1: case status with health indicators, AC2: activity event recording, AC3: empty activity for new cases)
- 4 new contract types created: `CaseActivityEventType`, `CaseActivityEvent`, `CaseStatusDetail`, `CaseActivityInput`
- `CaseActivityService` created with Redis Streams for activity recording (best-effort, never throws)
- `CaseService` extended with `CaseActivityService` dependency and `GetCaseStatusAsync` method
- 2 new API endpoints: `GET .../status` and `GET .../activity`
- `IngestionWorkflow` modified to record activity on both success and failure paths (both wrapped in try-catch)
- `CaseService.CreateCaseAsync` records `CaseCreated` activity event
- Search endpoint records `SearchExecuted` activity via fire-and-forget pattern
- 581 total tests passing (143 Contracts + 424 Server + 14 Benchmarks), 0 failures, 0 warnings

### File List

**New files:**

- `src/Hexalith.Memories.Contracts/V1/CaseActivityEventType.cs`
- `src/Hexalith.Memories.Contracts/V1/CaseActivityEvent.cs`
- `src/Hexalith.Memories.Contracts/V1/CaseStatusDetail.cs`
- `src/Hexalith.Memories.Contracts/V1/CaseActivityInput.cs`
- `src/Hexalith.Memories.Server/Cases/CaseActivityService.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/RecordCaseActivityActivity.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/CaseStatusDetailSerializationTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/CaseActivityEventSerializationTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/CaseActivityInputSerializationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseActivityServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Cases/RecordCaseActivityActivityTests.cs`

**Modified files:**

- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` (registered new types)
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` (added CaseActivityService dependency, GetCaseStatusAsync, activity recording in CreateCaseAsync)
- `src/Hexalith.Memories.Server/Program.cs` (DI registration, 2 new endpoints, search activity recording)
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` (activity recording on success/failure)
- `tests/Hexalith.Memories.Contracts.Tests/V1/EnumSerializationTests.cs` (added CaseActivityEventType tests)
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs` (updated constructor, added GetCaseStatusAsync tests)
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` (updated for new activity, added resilience tests)

### Change Log

- 2026-04-12: Story 3.2 implemented — case status, activity recording via Redis Streams, 2 new endpoints, workflow integration, 11 new files, 7 modified files
