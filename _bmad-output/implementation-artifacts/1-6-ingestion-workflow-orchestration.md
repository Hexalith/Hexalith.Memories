# Story 1.6: Ingestion Workflow Orchestration

Status: ready-for-dev

## Story

As a developer,
I want to ingest a local file and have it automatically processed through the full pipeline (validate → extract → embed → index across all backends → verify consistency),
so that a single API call results in a fully searchable memory unit with provenance tracking.

## Acceptance Criteria

1. **Given** a valid file and a tenant/case context **When** `IngestionWorkflow` is started **Then** it orchestrates: `ValidateContentActivity` → `ExtractContentActivity` → `GenerateEmbeddingActivity` → fan-out (`IndexSyntacticActivity` + `IndexSemanticActivity` + `IndexGraphActivity`) → `VerifyConsistencyActivity` **And** the memory unit status transitions: queued → extracting → embedding → indexing → indexed

2. **Given** `VerifyConsistencyActivity` runs after all indexing activities complete **When** it queries all three backends for the memory unit **Then** it confirms the unit exists in RediSearch, Redis Vector, and FalkorDB **And** if any backend is missing the unit, it reports the discrepancy

3. **Given** `IndexSemanticActivity` fails after `IndexSyntacticActivity` succeeds **When** the workflow retry policy is exhausted **Then** compensation activities clean up the successfully written RediSearch entry **And** the memory unit status is set to `failed` with FailureDetails (stage: indexing, error code, retry count) **And** the failed unit is never silently dropped

4. **Given** ingestion completes successfully **When** I inspect the memory unit **Then** `IngestedBy` contains the user or system identity **And** `IngestedAt` timestamp is set **And** metadata fields each track origin (human/ai) and confidence (0.0-1.0)

5. **Given** the DAPR sidecar restarts during an in-progress workflow **When** the sidecar recovers **Then** the workflow resumes from its last persisted state (Durable Task Framework) **And** no data loss occurs

6. **Given** the same content is ingested twice (duplicate detection) **When** the second ingestion is processed **Then** duplicate detection by source identifier prevents duplicate memory units

## Tasks / Subtasks

- [ ] Task 1: Create IngestionInput and IngestionResult contracts (AC: #1, #4)
  - [ ] 1.1 Create `IngestionInput` sealed record in `Contracts/V1/`
  - [ ] 1.2 Create `IngestionResult` sealed record in `Contracts/V1/`
  - [ ] 1.3 Register both types in `MemoriesJsonContext`
  - [ ] 1.4 Write serialization round-trip tests
- [ ] Task 2: Create ValidateContentActivity (AC: #1)
  - [ ] 2.1 Create `ValidateResult` sealed record in `Server/Activities/Ingestion/`: `(bool IsValid, string? ErrorMessage)`
  - [ ] 2.2 Implement `ValidateContentActivity : WorkflowActivity<IngestionInput, ValidateResult>` in `Activities/Ingestion/`
  - [ ] 2.3 Validate: TenantId, CaseId, SourceUri non-empty; ContentBytes non-null and non-empty; SourceType valid; IngestedBy non-empty
  - [ ] 2.4 Return `ValidateResult(true, null)` on success; throw `ArgumentException` with specific message on failure (no retry — invalid input stays invalid)
  - [ ] 2.5 Write unit tests with NSubstitute
- [ ] Task 3: Create CheckIdempotencyActivity (AC: #6)
  - [ ] 3.1 Implement `CheckIdempotencyActivity` in `Activities/Ingestion/`
  - [ ] 3.2 Check DAPR state store for existing SourceUri+TenantId+CaseId key
  - [ ] 3.3 Return `IdempotencyResult` with `IsDuplicate` flag and existing `MemoryUnitId` if found
  - [ ] 3.4 Write unit tests
- [ ] Task 4: Create VerifyConsistencyActivity (AC: #2)
  - [ ] 4.1 Implement `VerifyConsistencyActivity` in `Activities/Indexing/`
  - [ ] 4.2 Query RediSearch (`{tenantId}:mu:{memoryUnitId}`), Redis Vector (`{tenantId}:vec:{memoryUnitId}`), FalkorDB (tenant graph, node by id)
  - [ ] 4.3 Return `ConsistencyResult` listing which backends have the unit
  - [ ] 4.4 Write unit tests with mocked backends
- [ ] Task 5: Create compensation activities for indexing rollback (AC: #3)
  - [ ] 5.1 Implement `CleanupSyntacticActivity` — delete RediSearch HASH key
  - [ ] 5.2 Implement `CleanupSemanticActivity` — delete Redis Vector HASH key
  - [ ] 5.3 Implement `CleanupGraphActivity` — delete FalkorDB node via IGraphQueryBuilder
  - [ ] 5.3.1 If Story 1.5 did not add `BuildDeleteMemoryUnitNode` to `IGraphQueryBuilder`/`GraphQueryBuilder`, add it now (Cypher: `MATCH (m:MemoryUnit {id: $id}) DETACH DELETE m` with parameterized id)
  - [ ] 5.4 Write unit tests for each cleanup activity
- [ ] Task 5b: Create SaveDedupKeyActivity (AC: #6)
  - [ ] 5b.1 Implement `SaveDedupKeyActivity : WorkflowActivity<DedupKeyInput, bool>` in `Activities/Ingestion/`
  - [ ] 5b.2 Write dedup key to DAPR state store: `daprClient.SaveStateAsync("statestore", dedupKey, memoryUnitId)`
  - [ ] 5b.3 Create `DedupKeyInput` record: `(string DedupKey, string MemoryUnitId)`
  - [ ] 5b.4 Write unit tests
- [ ] Task 6: Implement IngestionWorkflow orchestration (AC: #1, #3, #4, #5, #6)
  - [ ] 6.1 Create `IngestionWorkflow : Workflow<IngestionInput, IngestionResult>` in `Workflows/`
  - [ ] 6.2 Implement sequential chain: CheckIdempotency → Validate → Extract → Embed
  - [ ] 6.3 Implement fan-out for parallel indexing via `Task.WhenAll` with `completedBackends` tracking list
  - [ ] 6.4 Implement tracked saga compensation — only clean up backends in `completedBackends` list (see updated pattern below)
  - [ ] 6.5 Implement VerifyConsistencyActivity after all indexing succeeds
  - [ ] 6.6 Populate provenance (IngestedBy, IngestedAt via `context.CurrentUtcDateTime`, metadata tracking)
  - [ ] 6.7 Call `SaveDedupKeyActivity` after VerifyConsistency succeeds (writes dedup key to state store)
  - [ ] 6.8 Return IngestionResult with MemoryUnitId and final status
- [ ] Task 7: Register workflow and activities in Program.cs (AC: #1)
  - [ ] 7.1 Register IngestionWorkflow in `AddDaprWorkflow()`
  - [ ] 7.2 Register all new activities (Validate, CheckIdempotency, VerifyConsistency, Cleanup*, SaveDedupKey)
  - [ ] 7.3 Add minimal REST endpoint `POST /api/ingest` that schedules the workflow
- [ ] Task 8: Write workflow orchestration tests (AC: #1, #3, #5, #6)
  - [ ] 8.1 Test happy path: all activities called in correct order
  - [ ] 8.2 Test fan-out: three indexing activities execute in parallel
  - [ ] 8.3 Test tracked compensation: only succeeded backends get cleanup (e.g., syntactic succeeds, semantic fails → only CleanupSyntactic called)
  - [ ] 8.4 Test duplicate detection: workflow returns early for duplicates
  - [ ] 8.5 Test provenance fields populated correctly
  - [ ] 8.6 Test SaveDedupKeyActivity called after successful ingestion
  - [ ] 8.7 (Tier 3, defer if Aspire test harness not ready) Create `IngestionPipelineTests.cs` in `Hexalith.Memories.IntegrationTests/` — end-to-end workflow with real Redis Stack + FalkorDB via Aspire `DistributedApplicationTestingBuilder` (D16)
- [ ] Task 9: Write serialization tests for all new contracts (AC: #1)
  - [ ] 9.1 Round-trip tests for IngestionInput, IngestionResult (Contracts/V1)
  - [ ] 9.2 Round-trip tests for IdempotencyInput, IdempotencyResult, ValidateResult, DedupKeyInput (Server activity records)
  - [ ] 9.3 Round-trip tests for ConsistencyInput, ConsistencyResult, CleanupInput (Server indexing records)

## Dev Notes

### HARD GATE: Story 1.5 Must Reach `done` Before Starting This Story

This story orchestrates activities created in Stories 1.3, 1.4, and **1.5**. Story 1.5 (`three-backend-indexing`, status: `ready-for-dev`) creates:
- `IndexSyntacticActivity`, `IndexSemanticActivity`, `IndexGraphActivity`
- `IndexInput` and `IndexResult` contracts
- `IGraphQueryBuilder` and `GraphQueryBuilder`
- Keyed DI registration for Redis (6379) and FalkorDB (6380)

**Do NOT start this story until Story 1.5 status is `done`.** If 1.5 ships with contract changes, this story's assumptions about IndexInput/IndexResult field names may need updating.

**Do not re-implement any Story 1.5 artifacts.** Import and use them directly.

### IngestionWorkflow Orchestration Flow

```
IngestionWorkflow<IngestionInput, IngestionResult>
│
├─ 1. CheckIdempotencyActivity(sourceUri, tenantId, caseId)
│     → IsDuplicate? return early with existing MemoryUnitId
│
├─ 2. ValidateContentActivity(ingestionInput)
│     → Validates all required fields, throws on invalid
│
├─ 3. ExtractContentActivity(extractionInput)        [Story 1.3]
│     → ExtractionResult { ExtractedContent, ContentHash, ExtractedAt }
│
├─ 4. GenerateEmbeddingActivity(embeddingInput)      [Story 1.4]
│     → EmbeddingResult { Vector, Provider, Dimensions }
│
├─ 5. Fan-out (parallel, with saga compensation):
│     ┌─ IndexSyntacticActivity(indexInput)           [Story 1.5]
│     ├─ IndexSemanticActivity(indexInput)             [Story 1.5]
│     └─ IndexGraphActivity(indexInput)                [Story 1.5]
│     On failure: CleanupSyntacticActivity + CleanupSemanticActivity + CleanupGraphActivity
│
├─ 6. VerifyConsistencyActivity(memoryUnitId, tenantId)
│     → ConsistencyResult { SyntacticExists, SemanticExists, GraphExists }
│
├─ 7. SaveDedupKeyActivity(dedupKey, memoryUnitId)
│     → Writes dedup key to DAPR state store (prevents future duplicates)
│
└─ Return IngestionResult { MemoryUnitId, Status=Indexed, IngestedAt }
```

### Workflow Definition Pattern (Exact API)

```csharp
namespace Hexalith.Memories.Server.Workflows;

public class IngestionWorkflow : Workflow<IngestionInput, IngestionResult>
{
    public override async Task<IngestionResult> RunAsync(
        WorkflowContext context, IngestionInput input)
    {
        var logger = context.CreateReplaySafeLogger<IngestionWorkflow>();
        var retryOptions = new WorkflowTaskOptions(
            RetryPolicy: new WorkflowRetryPolicy(
                maxNumberOfAttempts: 5,
                firstRetryInterval: TimeSpan.FromSeconds(2),
                backoffCoefficient: 2.0,
                maxRetryInterval: TimeSpan.FromMinutes(5)));

        // 1. Duplicate detection
        var idempotency = await context.CallActivityAsync<IdempotencyResult>(
            nameof(CheckIdempotencyActivity), idempotencyInput, retryOptions);
        if (idempotency.IsDuplicate) return earlyResult;

        // 2. Validation (no retry — invalid input stays invalid)
        await context.CallActivityAsync<ValidateResult>(
            nameof(ValidateContentActivity), input);

        // 3. Extract content
        var extraction = await context.CallActivityAsync<ExtractionResult>(
            nameof(ExtractContentActivity), extractionInput, retryOptions);

        // 4. Generate embedding
        var embedding = await context.CallActivityAsync<EmbeddingResult>(
            nameof(GenerateEmbeddingActivity), embeddingInput, retryOptions);

        // 5. Fan-out indexing with TRACKED saga compensation
        // Schedule all three indexing activities in parallel
        var syntacticTask = context.CallActivityAsync<IndexResult>(
            nameof(IndexSyntacticActivity), indexInput, retryOptions);
        var semanticTask = context.CallActivityAsync<IndexResult>(
            nameof(IndexSemanticActivity), indexInput, retryOptions);
        var graphTask = context.CallActivityAsync<IndexResult>(
            nameof(IndexGraphActivity), indexInput, retryOptions);

        try
        {
            await Task.WhenAll(syntacticTask, semanticTask, graphTask);
        }
        catch (WorkflowTaskFailedException)
        {
            // CRITICAL: Task.WhenAll throws on first faulted task, but other
            // tasks may have completed successfully. Check each task's status
            // to determine which backends actually wrote data and need cleanup.
            var completedBackends = new List<string>();
            if (syntacticTask.IsCompletedSuccessfully)
                completedBackends.Add(syntacticTask.Result.Backend);
            if (semanticTask.IsCompletedSuccessfully)
                completedBackends.Add(semanticTask.Result.Backend);
            if (graphTask.IsCompletedSuccessfully)
                completedBackends.Add(graphTask.Result.Backend);

            // Compensation: ONLY clean up backends that succeeded
            // Use shorter retry policy — compensation should be fast or fail
            var compensationRetry = new WorkflowTaskOptions(
                RetryPolicy: new WorkflowRetryPolicy(
                    maxNumberOfAttempts: 3,
                    firstRetryInterval: TimeSpan.FromSeconds(1),
                    backoffCoefficient: 2.0,
                    maxRetryInterval: TimeSpan.FromSeconds(30)));

            if (completedBackends.Contains("syntactic"))
                await context.CallActivityAsync(nameof(CleanupSyntacticActivity), cleanupInput, compensationRetry);
            if (completedBackends.Contains("semantic"))
                await context.CallActivityAsync(nameof(CleanupSemanticActivity), cleanupInput, compensationRetry);
            if (completedBackends.Contains("graph"))
                await context.CallActivityAsync(nameof(CleanupGraphActivity), cleanupInput, compensationRetry);
            throw; // Workflow fails with FailureDetails populated
        }

        // 6. Verify consistency
        var consistency = await context.CallActivityAsync<ConsistencyResult>(
            nameof(VerifyConsistencyActivity), verifyInput, retryOptions);

        // Consistency discrepancy: log warning but do NOT fail the workflow.
        // Consistency repair is handled by ConsistencyVerificationWorkflow (Epic 8).
        string? consistencyNote = null;
        if (!consistency.SyntacticExists || !consistency.SemanticExists || !consistency.GraphExists)
        {
            var missing = new List<string>();
            if (!consistency.SyntacticExists) missing.Add("syntactic");
            if (!consistency.SemanticExists) missing.Add("semantic");
            if (!consistency.GraphExists) missing.Add("graph");
            consistencyNote = $"Missing backends: {string.Join(", ", missing)}";
            logger.LogWarning("Consistency discrepancy for {MemoryUnitId}: {Note}",
                memoryUnitId, consistencyNote);
        }

        // 7. Persist dedup key (after all writes confirmed)
        string dedupKey = $"dedup:{input.TenantId}:{input.CaseId}:{ComputeHash(input.SourceUri)}";
        await context.CallActivityAsync<bool>(
            nameof(SaveDedupKeyActivity),
            new DedupKeyInput(dedupKey, memoryUnitId), retryOptions);

        return new IngestionResult(
            memoryUnitId,
            MemoryUnitStatus.Indexed,
            context.CurrentUtcDateTime,
            WasDuplicate: false,
            ConsistencyNote: consistencyNote);
    }
}
```

### Fan-out / Fan-in: Use `Task.WhenAll`

DAPR Workflow supports `Task.WhenAll` for parallel activity execution. All three indexing activities are scheduled simultaneously; the workflow waits for all to complete. If any fails after retry exhaustion, `WorkflowTaskFailedException` is caught and compensation runs.

Alternative: `context.ProcessInParallelAsync(workBatch, ...)` exists for dynamic batch sizes with max concurrency. Not needed here — three fixed activities.

### Retry Policy Configuration

```csharp
var retryOptions = new WorkflowTaskOptions(
    RetryPolicy: new WorkflowRetryPolicy(
        maxNumberOfAttempts: 5,
        firstRetryInterval: TimeSpan.FromSeconds(2),
        backoffCoefficient: 2.0,
        maxRetryInterval: TimeSpan.FromMinutes(5)));
```

- Apply `retryOptions` to ALL activities **except** `ValidateContentActivity` (invalid input stays invalid — no retry)
- DAPR Workflow handles retry automatically — activities must NOT implement their own retry loops
- Activities must NOT catch exceptions — let them propagate to the workflow retry policy
- `WorkflowTaskFailedException` is thrown when all retry attempts are exhausted

### Saga Compensation Pattern (Tracked)

On indexing failure, compensation activities run **only for backends that successfully wrote data**. The workflow maintains a `completedBackends` list tracking which `IndexResult.Backend` values were returned before the failure.

**Why tracked:** If `IndexSyntacticActivity` itself fails, there's nothing to clean up in RediSearch. Running `CleanupSyntacticActivity` unconditionally is wasteful and could mask errors. The `completedBackends` list ensures we only compensate what actually wrote data.

**Compensation activities:**
- `CleanupSyntacticActivity` — `db.KeyDeleteAsync($"{tenantId}:mu:{memoryUnitId}")` — only if `"syntactic"` in completedBackends
- `CleanupSemanticActivity` — `db.KeyDeleteAsync($"{tenantId}:vec:{memoryUnitId}")` — only if `"semantic"` in completedBackends
- `CleanupGraphActivity` — Delete node via `IGraphQueryBuilder.BuildDeleteMemoryUnitNode(memoryUnitId)` — only if `"graph"` in completedBackends

**Important:** If Story 1.5 did not add `BuildDeleteMemoryUnitNode` to `IGraphQueryBuilder`, implement it:
```csharp
// In IGraphQueryBuilder:
(string Query, IDictionary<string, object> Parameters) BuildDeleteMemoryUnitNode(string memoryUnitId);

// In GraphQueryBuilder:
public (string Query, IDictionary<string, object> Parameters) BuildDeleteMemoryUnitNode(string memoryUnitId)
    => ("MATCH (m:MemoryUnit {id: $id}) DETACH DELETE m", new Dictionary<string, object> { ["id"] = memoryUnitId });
```

Compensation activities must be **idempotent** — deleting a non-existent key is fine (no-op). They should NOT throw if the target doesn't exist.

**Compensation retry policy (separate from main):**
```csharp
var compensationRetryOptions = new WorkflowTaskOptions(
    RetryPolicy: new WorkflowRetryPolicy(
        maxNumberOfAttempts: 3,
        firstRetryInterval: TimeSpan.FromSeconds(1),
        backoffCoefficient: 2.0,
        maxRetryInterval: TimeSpan.FromSeconds(30)));
```
- Fewer attempts (3 vs 5) and shorter intervals — compensation should be fast or fail
- If compensation itself fails, the workflow still throws the original error
- Orphaned data from failed compensation is caught by `ConsistencyVerificationWorkflow` (Epic 8) or `memories tenant verify` (operator tool)
- Use `compensationRetryOptions` for all three cleanup activity calls in the catch block

### Contracts to Create

**IngestionInput** (`Contracts/V1/IngestionInput.cs`):
```csharp
namespace Hexalith.Memories.Contracts.V1;

public sealed record IngestionInput
{
    public required string TenantId { get; init; }
    public required string CaseId { get; init; }
    public required string SourceUri { get; init; }
    public required byte[] ContentBytes { get; init; }
    public required string ContentType { get; init; }
    public required SourceType SourceType { get; init; }
    public required string IngestedBy { get; init; }
    public Dictionary<string, MetadataField> Metadata
    {
        get => field ??= [];
        init => field = value ?? [];
    }
    public string? CausationId { get; init; }
    public string? CorrelationId { get; init; }
}
```
- Use `required` + `init` pattern (matches `MemoryUnit` record style)
- `byte[]` for content (matches `ExtractionInput` pattern) — MVP payloads <= 1MB (NFR5)
- `IngestedBy` is mandatory (FR65 provenance tracking)
- `CausationId`/`CorrelationId` optional (for EventStore graph edges, Story 1.5 already supports them in IndexInput)

**IngestionResult** (`Contracts/V1/IngestionResult.cs`):
```csharp
namespace Hexalith.Memories.Contracts.V1;

public sealed record IngestionResult(
    string MemoryUnitId,
    MemoryUnitStatus Status,
    DateTimeOffset IngestedAt,
    bool WasDuplicate,
    string? ConsistencyNote);
```
- `WasDuplicate` flag to differentiate "already existed" from "newly created"
- **Duplicate return:** `Status = Indexed, WasDuplicate = true, MemoryUnitId = existingId, IngestedAt = original ingestion time (or current time), ConsistencyNote = null`
- **Success return:** `Status = Indexed, WasDuplicate = false, MemoryUnitId = newId, IngestedAt = context.CurrentUtcDateTime, ConsistencyNote = null`
- **Consistency discrepancy:** `Status = Indexed, ConsistencyNote = "Missing backends: graph"` — log warning, do not fail (consistency repair is Epic 8, Story 8.2)
- **Failure return:** workflow throws `WorkflowTaskFailedException` — DAPR marks workflow as failed with error details

**ValidateResult** (`Server/Activities/Ingestion/ValidateResult.cs`):
```csharp
public sealed record ValidateResult(bool IsValid, string? ErrorMessage);
```
- Activity throws on invalid input (no retry — bad input stays bad)
- `ValidateResult` returned on success for workflow type safety

**IdempotencyInput** (`Server/Activities/Ingestion/IdempotencyInput.cs`):
```csharp
public sealed record IdempotencyInput(string SourceUri, string TenantId, string CaseId);
```

**IdempotencyResult** (`Server/Activities/Ingestion/IdempotencyResult.cs`):
```csharp
public sealed record IdempotencyResult(bool IsDuplicate, string? ExistingMemoryUnitId);
```

**ConsistencyInput** (`Server/Activities/Indexing/ConsistencyInput.cs`):
```csharp
public sealed record ConsistencyInput(string MemoryUnitId, string TenantId);
```

**ConsistencyResult** (`Server/Activities/Indexing/ConsistencyResult.cs`):
```csharp
public sealed record ConsistencyResult(
    bool SyntacticExists,
    bool SemanticExists,
    bool GraphExists);
```

**CleanupInput** (`Server/Activities/Indexing/CleanupInput.cs`):
```csharp
public sealed record CleanupInput(string MemoryUnitId, string TenantId);
```

**DedupKeyInput** (`Server/Activities/Ingestion/DedupKeyInput.cs`):
```csharp
public sealed record DedupKeyInput(string DedupKey, string MemoryUnitId);
```

### Memory Unit ID Generation

Generate `MemoryUnitId` at the start of the workflow. **Decision: Use `Guid.NewGuid().ToString()` for MVP.**

```csharp
string memoryUnitId = Guid.NewGuid().ToString();
```

- Architecture specifies ULID (`Id` field type is `string (ULID)`) but `Directory.Packages.props` has no ULID package
- `Guid.NewGuid()` is the MVP fallback — globally unique, no external dependency
- ULID migration (time-sortable IDs) can be done later by adding the `Ulid` NuGet package and changing this single line
- **CRITICAL:** Generate the ID inside the workflow using a deterministic approach — `context.NewGuid()` if available in DAPR Workflow SDK, otherwise `Guid.NewGuid()` (acceptable for MVP since ID generation is the first operation and replay will reuse the persisted result)

### Memory Unit Status Transitions

The workflow should conceptually track status transitions. Status transitions happen within the workflow context — they are not persisted to a separate store in this story. The `IngestionResult.Status` reflects the final state.

| After Activity | Status |
|---|---|
| Workflow start | `Queued` |
| ValidateContentActivity | `Extracting` |
| ExtractContentActivity | `Embedding` |
| GenerateEmbeddingActivity | `Indexing` |
| VerifyConsistencyActivity | `Indexed` |
| Any failure (retry exhausted) | `Failed` |

### Duplicate Detection via DAPR State Store

**Two activities work together for dedup:**

1. **`CheckIdempotencyActivity`** (start of workflow) — reads the state store to check for existing dedup key:
```csharp
// Key format: dedup:{tenantId}:{caseId}:{sourceUri-hash}
string dedupKey = $"dedup:{input.TenantId}:{input.CaseId}:{ComputeHash(input.SourceUri)}";
var existing = await daprClient.GetStateAsync<string>("statestore", dedupKey);
if (existing is not null)
    return new IdempotencyResult(true, existing);
return new IdempotencyResult(false, null);
```

2. **`SaveDedupKeyActivity`** (end of workflow, after VerifyConsistency) — writes the dedup key:
```csharp
await daprClient.SaveStateAsync("statestore", input.DedupKey, input.MemoryUnitId);
```

- Hash the SourceUri with SHA-256 to normalize key length and prevent key injection
- `ComputeHash` is a static helper — implement as:
  ```csharp
  static string ComputeHash(string input)
      => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
          System.Text.Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
  ```
  Place in the workflow class as a `private static` method (deterministic, no I/O — safe for replay)
- Store the `MemoryUnitId` as the value for dedup entries
- Dedup write via `SaveDedupKeyActivity` happens AFTER successful ingestion — workflows cannot call `DaprClient` directly
- Concurrent duplicate submissions can both proceed — acceptable for MVP. True atomic dedup requires locking (Phase 2)
- Known race window documented as accepted MVP limitation

### REST Endpoint for Triggering Ingestion

Minimal endpoint in `Program.cs` (or a dedicated controller):
```csharp
app.MapPost("/api/ingest", async (DaprWorkflowClient workflowClient, IngestionInput input) =>
{
    // Basic pre-flight validation before scheduling workflow
    if (string.IsNullOrWhiteSpace(input.TenantId) ||
        string.IsNullOrWhiteSpace(input.CaseId) ||
        string.IsNullOrWhiteSpace(input.SourceUri) ||
        input.ContentBytes is null or { Length: 0 })
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_INPUT",
            "TenantId, CaseId, SourceUri, and ContentBytes are required.",
            "Ensure all required fields are populated."));
    }

    string instanceId = await workflowClient.ScheduleNewWorkflowAsync(
        nameof(IngestionWorkflow), input: input);
    return Results.Accepted($"/api/ingest/{instanceId}", new { instanceId });
});

app.MapGet("/api/ingest/{instanceId}", async (DaprWorkflowClient workflowClient, string instanceId) =>
{
    WorkflowState? state = await workflowClient.GetWorkflowStateAsync(instanceId);
    return state is null ? Results.NotFound() : Results.Ok(state);
});
```
- `POST /api/ingest` → 400 `ErrorResponse` if required fields missing; 202 Accepted with instance ID on success
- `GET /api/ingest/{instanceId}` → returns workflow state (for polling status)
- `DaprWorkflowClient` is automatically registered by `AddDaprWorkflow()`
- `ErrorResponse` already exists in `Contracts/V1/ErrorResponse.cs` (code, message, suggestion)

### Provenance Tracking

- `IngestedBy` comes from `IngestionInput.IngestedBy` (caller provides identity)
- `IngestedAt` is set at workflow start: `context.CurrentUtcDateTime` (deterministic, replay-safe — NEVER use `DateTime.UtcNow` in workflows)
- Metadata fields pass through from `IngestionInput.Metadata` to `IndexInput.Metadata`
- EmbeddingProvider and EmbeddingDimensions come from `EmbeddingResult`

### Logging in Workflows

**CRITICAL:** Use `context.CreateReplaySafeLogger<IngestionWorkflow>()` — regular `ILogger` produces duplicate log entries during workflow replay. Log at each major step:
```csharp
logger.LogInformation("Ingestion started for {SourceUri} in tenant {TenantId}", input.SourceUri, input.TenantId);
logger.LogInformation("Content extracted: {ContentHash}, {Length} chars", extraction.ContentHash, extraction.ExtractedContent.Length);
logger.LogInformation("Embedding generated: {Provider}, {Dims} dimensions", embedding.Provider, embedding.Dimensions);
logger.LogInformation("Indexing complete, verifying consistency for {MemoryUnitId}", memoryUnitId);
```

### Data Flow Between Activities

Map IngestionInput to each activity's specific input type:

```
IngestionInput
  → IdempotencyInput(SourceUri, TenantId, CaseId)
  → ExtractionInput(SourceUri, ContentBytes, ContentType, SourceType)     [Contracts.V1]
  → EmbeddingInput(TenantId, extraction.ExtractedContent)                 [Server.Activities.Ingestion]
  → IndexInput(                                                            [Contracts.V1, from Story 1.5]
        MemoryUnitId = generated ULID,
        TenantId, CaseId,
        Content = extraction.ExtractedContent,
        ContentHash = extraction.ContentHash,
        SourceUri, SourceType,
        EmbeddingVector = embedding.Vector,
        EmbeddingProvider = embedding.Provider,
        EmbeddingDimensions = embedding.Dimensions,
        Metadata,
        CausationId, CorrelationId)
  → ConsistencyInput(MemoryUnitId, TenantId)
  → CleanupInput(MemoryUnitId, TenantId)
```

### Project Structure Notes

**New files to create:**

```
src/Hexalith.Memories.Contracts/V1/
  IngestionInput.cs
  IngestionResult.cs

src/Hexalith.Memories.Server/
  Workflows/
    IngestionWorkflow.cs                        # Main orchestrator
  Activities/
    Ingestion/
      ValidateContentActivity.cs                # Input validation
      ValidateResult.cs                         # Validation output type
      CheckIdempotencyActivity.cs               # Dedup check (read)
      SaveDedupKeyActivity.cs                   # Dedup persist (write)
      IdempotencyInput.cs
      IdempotencyResult.cs
      DedupKeyInput.cs
    Indexing/
      VerifyConsistencyActivity.cs              # Three-backend check
      ConsistencyInput.cs
      ConsistencyResult.cs
      CleanupSyntacticActivity.cs               # Compensation
      CleanupSemanticActivity.cs                # Compensation
      CleanupGraphActivity.cs                   # Compensation
      CleanupInput.cs

tests/Hexalith.Memories.Server.Tests/
  Workflows/
    IngestionWorkflowTests.cs
  Activities/
    Ingestion/
      ValidateContentActivityTests.cs
      CheckIdempotencyActivityTests.cs
      SaveDedupKeyActivityTests.cs
    Indexing/
      VerifyConsistencyActivityTests.cs
      CleanupSyntacticActivityTests.cs
      CleanupSemanticActivityTests.cs
      CleanupGraphActivityTests.cs

tests/Hexalith.Memories.Contracts.Tests/V1/
  IngestionInputTests.cs                        # Serialization round-trips
  IngestionResultTests.cs
```

**Files to modify:**
- `src/Hexalith.Memories.Server/Program.cs` — register workflow + activities + endpoints
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — add IngestionInput, IngestionResult

### Existing Activity Patterns to Follow

Match exactly the patterns established in Stories 1.3 and 1.4:

**Activity class pattern** (from `GenerateEmbeddingActivity.cs`):
- File-scoped namespace: `namespace Hexalith.Memories.Server.Activities.Ingestion;`
- Base class: `WorkflowActivity<TInput, TOutput>`
- Constructor DI for dependencies
- `public override async Task<TOutput> RunAsync(WorkflowActivityContext context, TInput input)`
- Validate input with `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`
- NO exception catching — let DAPR retry handle it
- NO `CancellationToken` in activity body (workflow handles lifecycle)

**Contract record pattern** (from `MemoryUnit.cs`, `ExtractionInput.cs`):
- `sealed record` with required+init properties for complex types
- Positional `sealed record` for simple data (e.g., `FailureDetails`, `ExtractionResult`)
- `Dictionary<string, MetadataField>` uses `field ??= []` pattern
- Nullable properties use `?` suffix

**Test pattern** (from `GenerateEmbeddingActivityTests.cs`):
- xUnit + Shouldly + NSubstitute
- Mock activity dependencies, instantiate activity directly, call `RunAsync`
- Use `Substitute.For<T>()` for interfaces and non-sealed classes
- Use `Received.InOrder()` for call sequencing verification
- Use `Arg.Any<T>()` for flexible matching

### DI Registration Updates for Program.cs

```csharp
builder.Services.AddDaprWorkflow(options =>
{
    // Existing
    options.RegisterActivity<ExtractContentActivity>();
    options.RegisterActivity<GenerateEmbeddingActivity>();

    // Story 1.5 (must exist)
    options.RegisterActivity<IndexSyntacticActivity>();
    options.RegisterActivity<IndexSemanticActivity>();
    options.RegisterActivity<IndexGraphActivity>();

    // Story 1.6 (new)
    options.RegisterWorkflow<IngestionWorkflow>();
    options.RegisterActivity<ValidateContentActivity>();
    options.RegisterActivity<CheckIdempotencyActivity>();
    options.RegisterActivity<SaveDedupKeyActivity>();
    options.RegisterActivity<VerifyConsistencyActivity>();
    options.RegisterActivity<CleanupSyntacticActivity>();
    options.RegisterActivity<CleanupSemanticActivity>();
    options.RegisterActivity<CleanupGraphActivity>();
});
```

Also add `DaprWorkflowClient` usage — it is already registered by `AddDaprWorkflow()`.

### Anti-Patterns to Avoid

- **DO NOT** use `DateTime.UtcNow` or `DateTimeOffset.UtcNow` in workflow code — use `context.CurrentUtcDateTime` (deterministic, replay-safe)
- **DO NOT** use regular `ILogger` in workflows — use `context.CreateReplaySafeLogger<T>()`
- **DO NOT** call services directly from the workflow — all I/O goes through activities
- **DO NOT** implement retry loops inside activities — DAPR `WorkflowRetryPolicy` handles this
- **DO NOT** catch exceptions in activities — let them propagate to the workflow
- **DO NOT** re-create activities from Story 1.3/1.4/1.5 — import and use them
- **DO NOT** create `IIngestionWorkflow` interface — no premature abstractions (D9)
- **DO NOT** use `Thread.Sleep` or `Task.Delay` in workflows — use `context.CreateTimer()` if needed
- **DO NOT** store mutable state in workflow fields — workflow class is re-instantiated on replay
- **DO NOT** add a full `MemoryUnit` persistence store (database table/collection) in this story — the three backends (RediSearch, Vector, FalkorDB) are the persistence layer for MVP
- **DO NOT** batch embed multiple documents — single-document per ingestion workflow (NFR5)
- **DO NOT** add `retryOptions` to `ValidateContentActivity` call — invalid input should fail immediately, not retry 5 times
- **DO NOT** use the `foreach`-over-tasks pattern for tracking completed backends — `Task.WhenAll` throws on first failure; check `.IsCompletedSuccessfully` on each task in the catch block instead

### Workflow Concurrency Note

DAPR Workflow has no built-in concurrency limit — all scheduled workflow instances run in parallel. With 50 concurrent `IngestionWorkflow` instances:
- **Extraction (Kreuzberg):** CPU-intensive, runs in-process. Concurrent extractions compete for server CPU. Acceptable for MVP payloads <= 1MB.
- **Embedding (Google API):** `EmbeddingRateLimiterActor` provides per-tenant backpressure. Workflows that exceed the rate limit throw `EmbeddingRateLimitException`, which triggers `WorkflowRetryPolicy` (exponential backoff). This is the designed throttling mechanism.
- **Indexing (Redis + FalkorDB):** Concurrent writes to Redis are fine (thread-safe). FalkorDB handles concurrent graph writes.

**No additional concurrency control needed for MVP.** The rate limiter actor is the natural backpressure point. Workflow-level concurrency limits (e.g., semaphore via DAPR actor) are a Phase 3 concern for large-scale batch ingestion (Story 6.2).
- **DO NOT** add URL or directory ingestion — file-only in this story (URLs in Story 6.1)
- **DO NOT** add EmbeddingProvider configuration — hardcoded Google MVP (Story 1.7)

### Compensation Activity Requirements

- Compensation activities must be **idempotent** — cleaning up a non-existent resource is not an error
- Compensation activities SHOULD log at Warning level when cleaning up
- Compensation activities SHOULD have their own retry policy (shorter, fewer attempts)
- CleanupGraphActivity needs `IGraphQueryBuilder` — if Story 1.5 doesn't include a `BuildDeleteMemoryUnitNode` method, add it to `IGraphQueryBuilder` and `GraphQueryBuilder`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic1-Story1.6] — Full story requirements and BDD scenarios
- [Source: _bmad-output/planning-artifacts/architecture.md#IngestionWorkflow] — Workflow orchestration pattern, activity chain, saga compensation
- [Source: _bmad-output/planning-artifacts/architecture.md#DAprWorkflow] — DAPR Workflow API patterns, retry policy, fan-out/fan-in
- [Source: _bmad-output/planning-artifacts/prd.md#FR1-FR13] — Ingestion functional requirements
- [Source: _bmad-output/planning-artifacts/prd.md#NFR5] — Ingestion throughput: >100 units/min (<=10KB), >10 units/min (<=1MB)
- [Source: _bmad-output/planning-artifacts/prd.md#NFR17] — Pipeline state survives process restarts
- [Source: _bmad-output/planning-artifacts/prd.md#NFR19] — Failed units never silently dropped
- [Source: _bmad-output/planning-artifacts/prd.md#NFR22] — Rate limit 429 handled gracefully
- [Source: _bmad-output/implementation-artifacts/1-4-embedding-generation.md] — EmbeddingClient, RateLimiterLogic, activity patterns
- [Source: _bmad-output/implementation-artifacts/1-5-three-backend-indexing.md] — IndexInput, IndexResult, IGraphQueryBuilder, backend isolation
- [Source: Dapr docs - workflow patterns] — Fan-out/fan-in via Task.WhenAll, WorkflowRetryPolicy, compensation
- [Source: Dapr .NET SDK] — Workflow<TInput, TOutput>, WorkflowActivity<TInput, TOutput>, CreateReplaySafeLogger

### Previous Story Intelligence

**From Story 1.4 (Embedding Generation):**
- `EmbeddingClient` is non-sealed (for NSubstitute mocking)
- `GenerateEmbeddingActivity` validates input, primes API key, checks rate limiter, then calls embedding client
- Actor proxy created via `IActorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(new ActorId(tenantId), nameof(EmbeddingRateLimiterActor))`
- API key retrieved from DAPR secret store: `DaprClient.GetSecretAsync("secretstore", "google-embedding-api-key")`
- `HttpClient` lifecycle managed via `AddHttpClient<EmbeddingClient>(client => { client.Timeout = TimeSpan.FromSeconds(30); })`
- 25 tests added: 11 EmbeddingClient, 8 RateLimiterLogic, 6 GenerateEmbeddingActivity

**From Story 1.5 (Three-Backend Indexing) design:**
- `IndexInput` uses `required` + `init` properties (not positional record) — shared input for all three activities
- `IndexResult` is positional: `(string Backend, string MemoryUnitId, string TenantId)` — success implicit, failure = exception
- `IGraphQueryBuilder` is a SAFETY interface — no raw Cypher anywhere
- FalkorDB uses keyed DI: `[FromKeyedServices("falkordb")] IConnectionMultiplexer`
- Redis Stack (syntactic/semantic) on port 6379, FalkorDB on port 6380
- Vector stored as byte[] via `MemoryMarshal.AsBytes()` — zero-copy conversion
- Index creation is idempotent (try/catch "Index already exists")
- TenantId validated with regex: `^[a-zA-Z0-9\-]+$` before use in FalkorDB graph name

**Deferred work from Story 1.3:**
- Large byte[] in ExtractionInput persisted to workflow state (~1.33MB base64 per 1MB file). Accepted per D13 (MVP <= 1MB).
- No transient/permanent exception classification for Kreuzberg

**Deferred work from Story 1.4:**
- End-to-end embedding flow not wired into orchestration — that is THIS story
- Rate-limiting scope conflicts with credential scope — deferred to Story 1.7

### Git Intelligence

Recent commits establishing patterns:
- `b8e3bab` feat(server): add embedding generation workflow activity (#2) — Story 1.4
- `f5d7a17` feat: Replace Apache Tika with Kreuzberg for content extraction — Story 1.3
- `2d8cd09` feat: Add memory graph model and serialization support — Story 1.2

Commit message convention: `feat(scope): description` using conventional commits.

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
