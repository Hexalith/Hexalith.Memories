# Story 1.5: Three-Backend Indexing

Status: done

## Story

As a developer,
I want ingested content to be indexed across RediSearch (syntactic), Redis Vector (semantic), and FalkorDB (graph) with tenant-namespaced indexes,
So that memory units are searchable across all three axes after ingestion.

## Acceptance Criteria

1. **Given** a memory unit with extracted content and generated embedding
   **When** `IndexSyntacticActivity` executes
   **Then** the memory unit is indexed in RediSearch with tenant-namespaced index (`{tenantId}:memories:idx`)
   **And** the content, metadata, and source information are searchable via full-text query

2. **Given** a memory unit with a generated embedding vector
   **When** `IndexSemanticActivity` executes
   **Then** the vector is stored in Redis Vector Search with tenant-namespaced index (`{tenantId}:memories:vec`)
   **And** the vector is retrievable via KNN similarity search

3. **Given** a memory unit with source information
   **When** `IndexGraphActivity` executes
   **Then** a node is created in FalkorDB in the tenant's dedicated database (physical isolation at database level)
   **And** if the source contains CausationId, a `caused_by` edge is created (confidence 1.0, origin: explicit)
   **And** if the source contains CorrelationId, a `correlated_with` edge is created (confidence 0.8, origin: explicit)
   **And** a `contains` edge is created from the case node to the memory unit node (confidence 1.0)

4. **Given** the `IGraphQueryBuilder` is used for all FalkorDB queries
   **When** any graph operation is performed
   **Then** only parameterized Cypher queries are used — no raw Cypher string construction
   **And** this is enforced structurally by the interface design

5. **Given** indexes are created for a tenant
   **When** I inspect the index naming
   **Then** MVP uses `{tenantId}:memories:idx` and `{tenantId}:memories:vec` (per Data Boundaries table)
   **And** the naming scheme is documented as extensible to support concurrent versions (`{tenantId}:{model-version}:syntactic`) for future model migration (Decision D10 — accept degradation in MVP, design for versions later)

## Tasks / Subtasks

- [x] Task 1: Create indexing input/output types (AC: #1, #2, #3)
    - [x] 1.1 Create `Contracts/V1/IndexInput.cs` — sealed record with **`required` properties and init-only setters** (same pattern as `MemoryUnit`), NOT a positional record. 13 constructor parameters is too many for readability. Fields: MemoryUnitId (required string), TenantId (required string), CaseId (required string), Content (required string), ContentHash (required string), SourceUri (required string), SourceType (required SourceType), EmbeddingVector (required float[]), EmbeddingProvider (required string), EmbeddingDimensions (required int), Metadata (Dictionary<string, MetadataField> — lazy init like MemoryUnit), CausationId (string?, optional), CorrelationId (string?, optional). This is the shared input for all three indexing activities — the IngestionWorkflow constructs it from extraction + embedding results.
    - [x] 1.2 Create `Contracts/V1/IndexResult.cs` — sealed record: Backend (string — "syntactic", "semantic", or "graph"), MemoryUnitId (string), TenantId (string). Success is implicit (activity returned without throwing). Failure is an exception (propagated to workflow retry). Do NOT add `Success` or `ErrorMessage` fields — they are unreachable code since activities never return failure; they throw instead.

- [x] Task 2: Create `IGraphQueryBuilder` safety interface (AC: #4)
    - [x] 2.1 Create `Server/Graph/IGraphQueryBuilder.cs` — interface with methods for all graph operations needed by this story. This is a **safety interface** (Decision D9) — it exists to structurally prevent Cypher injection, not for extensibility. Methods must accept only typed parameters, never raw Cypher strings. Design:

        ```csharp
        public interface IGraphQueryBuilder
        {
            // Merges a memory unit node (idempotent — creates or updates)
            (string Query, IDictionary<string, object> Parameters) BuildMergeMemoryUnitNode(
                string memoryUnitId, string caseId, string content, string contentHash,
                string sourceUri, SourceType sourceType, string embeddingProvider,
                int embeddingDimensions, DateTimeOffset indexedAt);

            // Creates a case node if it doesn't exist (MERGE pattern)
            (string Query, IDictionary<string, object> Parameters) BuildMergeCaseNode(
                string caseId);

            // Creates a typed edge between two nodes
            (string Query, IDictionary<string, object> Parameters) BuildMergeEdge(
                string sourceNodeId, string targetNodeId, EdgeType edgeType,
                float confidence, EdgeOrigin origin);

            // Creates a stub node for a referenced memory unit that may not be ingested yet
            (string Query, IDictionary<string, object> Parameters) BuildMergeStubNode(
                string memoryUnitId);
        }
        ```

    - [x] 2.2 Every method returns `(string Query, IDictionary<string, object> Parameters)` tuple — the query string contains `$paramName` placeholders, the dictionary supplies values. The caller (activity) passes both to `graph.QueryAsync(query, parameters)`. No method ever accepts or returns raw Cypher.

- [x] Task 3: Create `GraphQueryBuilder` implementation (AC: #3, #4)
    - [x] 3.1 Create `Server/Graph/GraphQueryBuilder.cs` — concrete class implementing `IGraphQueryBuilder`. Register as singleton in DI (stateless, thread-safe).
    - [x] 3.2 `BuildMergeMemoryUnitNode` (renamed from `BuildCreateMemoryUnitNode`): Generate Cypher `MERGE (m:MemoryUnit {id: $id}) SET m.caseId = $caseId, m.content = $content, m.contentHash = $contentHash, m.sourceUri = $sourceUri, m.sourceType = $sourceType, m.embeddingProvider = $provider, m.embeddingDimensions = $dims, m.indexedAt = $indexedAt`. **Uses MERGE + SET, not CREATE.** This makes the operation idempotent — re-ingesting the same memory unit updates the existing node rather than creating a duplicate. All values via `$` parameters. Enum values stored as camelCase strings (matching JSON serialization convention). Update the `IGraphQueryBuilder` interface method name accordingly.
    - [x] 3.3 `BuildMergeCaseNode`: Generate Cypher `MERGE (c:Case {id: $caseId})`. Uses MERGE (not CREATE) so case node is idempotent — multiple memory units in the same case share the case node.
    - [x] 3.4 `BuildMergeEdge`: Generate Cypher `MATCH (s {id: $sourceId}), (t {id: $targetId}) MERGE (s)-[:CAUSED_BY {confidence: $confidence, origin: $origin}]->(t)` (example for CausedBy). Uses **MERGE** (not CREATE) for idempotent edge creation — re-ingesting the same content doesn't create duplicate edges. Note: FalkorDB does not support parameterized relationship types in Cypher — the edge type label must be interpolated into the query string. Use UPPER*SNAKE_CASE for Cypher relationship labels (standard Neo4j/Cypher convention, eases future migration). Implement a `ToUpperSnakeCase` conversion: `CausedBy` → `CAUSED_BY`, `CorrelatedWith` → `CORRELATED_WITH`, `Contains` → `CONTAINS`, `References` → `REFERENCES`, `Annotates` → `ANNOTATES`. Use a private helper method with regex `Regex.Replace(name, "([a-z])([A-Z])", "$1*$2").ToUpperInvariant()`or an explicit switch on the`EdgeType`enum (preferred — closed set, no regex needed). Mitigate injection risk by validating against the`EdgeType` enum before interpolation. Document this as a known FalkorDB limitation.
    - [x] 3.5 `BuildMergeStubNode(string memoryUnitId)`: Generate Cypher `MERGE (m:MemoryUnit {id: $id})`. Simple MERGE with only the ID — no SET clause. Creates a minimal stub node for memory units referenced by CausedBy/CorrelatedWith edges that haven't been ingested yet. When the unit is later ingested, `BuildMergeMemoryUnitNode` will SET the full properties on this existing node.
    - [x] 3.6 Input validation in every method: `ArgumentException.ThrowIfNullOrWhiteSpace` for string params. Reject unknown `EdgeType` values. The edge type conversion in 3.4 MUST use `Enum.IsDefined<EdgeType>()` or a switch expression that throws on unrecognized values — never blindly `.ToString()` an enum.

- [x] Task 4: Create `IndexSyntacticActivity` (AC: #1, #5)
    - [x] 4.1 Create `Server/Activities/Indexing/IndexSyntacticActivity.cs` — inherits `WorkflowActivity<IndexInput, IndexResult>`. Constructor takes `IConnectionMultiplexer` and `ILogger<IndexSyntacticActivity>` via DI. Log at `Information` level on success: `"Indexed memory unit {MemoryUnitId} in RediSearch for tenant {TenantId}"`. Log at `Warning` level on "Index already exists" catch. This applies to all three activities — each should inject `ILogger<T>` and log backend name, tenant ID, memory unit ID, and outcome.
    - [x] 4.2 In `RunAsync`:
        1. Get `IDatabase` from the multiplexer
        2. Get `SearchCommands` via `db.FT()`
        3. Construct the RediSearch index name: `{input.TenantId}:memories:idx` (per architecture Data Boundaries table)
        4. Ensure the index exists (call `FT.CREATE` wrapped in try/catch for "Index already exists" — idempotent creation). Schema:
            - `content` → TextField (weight 1.0, full-text searchable)
            - `sourceUri` → TagField (exact match filterable)
            - `sourceType` → TagField (exact match filterable)
            - `contentHash` → TagField (dedup lookup)
            - `caseId` → TagField (case-scoped queries)
            - `embeddingProvider` → TagField
        5. Store the memory unit as a Redis HASH with key `{tenantId}:mu:{memoryUnitId}`. Use `db.HashSetAsync()` with fields matching the schema above.
        6. Return `new IndexResult("syntactic", input.MemoryUnitId, input.TenantId)`
    - [x] 4.3 Let exceptions propagate to workflow retry policy (Decision D25). Do NOT catch exceptions in the activity.
    - [x] 4.4 Index creation must use `FTCreateParams().On(IndexDataType.HASH).Prefix($"{input.TenantId}:mu:")` — the prefix ensures this index only covers documents belonging to this tenant.

- [x] Task 5: Create `IndexSemanticActivity` (AC: #2, #5)
    - [x] 5.1 Create `Server/Activities/Indexing/IndexSemanticActivity.cs` — inherits `WorkflowActivity<IndexInput, IndexResult>`. Constructor takes `IConnectionMultiplexer` (Redis Stack, port 6379) and `ILogger<IndexSemanticActivity>` via DI.
    - [x] 5.2 In `RunAsync`:
        1. Get `IDatabase` from the multiplexer
        2. Get `SearchCommands` via `db.FT()`
        3. Construct the Redis Vector index name: `{input.TenantId}:memories:vec` (per architecture Data Boundaries table)
        4. Ensure the vector index exists (idempotent `FT.CREATE` with try/catch). Schema:
            - `embedding` → VectorField with HNSW algorithm, `TYPE=FLOAT32`, `DIM=768` (from `input.EmbeddingDimensions`), `DISTANCE_METRIC=COSINE`
            - `memoryUnitId` → TagField (for join-back to syntactic index)
            - `caseId` → TagField (case-scoped vector search)
        5. Store the vector as a Redis HASH with key `{tenantId}:vec:{memoryUnitId}`. Fields:
            - `embedding` → byte[] (convert float[] to byte[] — Redis expects FLOAT32 binary blob for vector fields)
            - `memoryUnitId` → string
            - `caseId` → string
        6. Return `new IndexResult("semantic", input.MemoryUnitId, input.TenantId)`
    - [x] 5.3 **Input validation and vector conversion:** (a) `ArgumentNullException.ThrowIfNull(input.EmbeddingVector)`, throw `ArgumentException` if `Length == 0`. (b) Convert: `MemoryMarshal.AsBytes(input.EmbeddingVector.AsSpan()).ToArray()` (requires `using System.Runtime.InteropServices;`). (c) Post-conversion validation: verify `vectorBytes.Length == input.EmbeddingDimensions * sizeof(float)`. If mismatch, throw `InvalidOperationException($"Vector byte length {vectorBytes.Length} does not match expected {input.EmbeddingDimensions * 4} bytes for {input.EmbeddingDimensions} dimensions")`. This catches silent corruption before it reaches the vector index.
    - [x] 5.4 Index creation must use `FTCreateParams().On(IndexDataType.HASH).Prefix($"{input.TenantId}:vec:")` — vector documents use a separate prefix from syntactic documents.

- [x] Task 6: Create `IndexGraphActivity` (AC: #3, #4)
    - [x] 6.1 Create `Server/Activities/Indexing/IndexGraphActivity.cs` — inherits `WorkflowActivity<IndexInput, IndexResult>`. Constructor takes a **FalkorDB-specific** `IConnectionMultiplexer` (NOT the Redis Stack one), `IGraphQueryBuilder`, and `ILogger<IndexGraphActivity>` via DI. **CRITICAL:** FalkorDB runs on port 6380, Redis Stack on port 6379 (per architecture deployment topology). These are separate servers. Use keyed/named DI registration to distinguish them (e.g., `[FromKeyedServices("falkordb")] IConnectionMultiplexer falkorMultiplexer`). See Task 7 for registration details.
    - [x] 6.2 In `RunAsync`:
        1. Get the tenant's FalkorDB graph: `new FalkorDB(db).SelectGraph(input.TenantId)` — physical isolation at database level (each tenant = separate FalkorDB database/graph). **Important:** Check if `Graph` implements `IDisposable`. If so, wrap in `using`. If not, no action needed. NFalkorDB 1.0.0 docs are sparse — verify via source at https://github.com/FalkorDB/NFalkorDB
        2. Use `IGraphQueryBuilder` for ALL Cypher operations:
           a. `BuildMergeCaseNode(input.CaseId)` → execute via `graph.QueryAsync(query, parameters)`. **Set timeout on all graph operations:** NFalkorDB supports an optional timeout parameter (milliseconds) on `Query`/`QueryAsync`. Use 10000ms (10 seconds) — generous ceiling that prevents indefinite blocking if FalkorDB is slow or the graph is large. If timeout is exceeded, the operation throws and the workflow retry policy handles it.
           b. `BuildMergeMemoryUnitNode(...)` with all indexed properties (idempotent — creates or updates)
           c. `BuildMergeEdge(input.CaseId, input.MemoryUnitId, EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit)` — case→unit containment edge
           d. If `input.CausationId` is not null: First `MERGE` a stub node for the source: `BuildMergeStubNode(input.CausationId)` (creates `(:MemoryUnit {id: $id})` if it doesn't exist — the full properties will be SET when that unit is ingested). Then `BuildMergeEdge(input.CausationId, input.MemoryUnitId, EdgeType.CausedBy, EdgeTypeDefaults.CausedBy, EdgeOrigin.Explicit)`. This handles the case where the CausationId references a memory unit that hasn't been ingested yet.
           e. If `input.CorrelationId` is not null: Same pattern — `BuildMergeStubNode(input.CorrelationId)` then `BuildMergeEdge(input.CorrelationId, input.MemoryUnitId, EdgeType.CorrelatedWith, EdgeTypeDefaults.CorrelatedWith, EdgeOrigin.Explicit)`
        3. Return `new IndexResult("graph", input.MemoryUnitId, input.TenantId)`
    - [x] 6.3 Do NOT construct any Cypher strings in the activity — delegate ALL query building to `IGraphQueryBuilder`. The activity only calls the builder and executes the returned query+params.
    - [x] 6.4 Let exceptions propagate (Decision D25).
    - [x] 6.5 **Defensive TenantId validation:** Before calling `SelectGraph(input.TenantId)`, validate that TenantId matches safe format: `Regex.IsMatch(input.TenantId, @"^[a-zA-Z0-9\-]+$")`. FalkorDB uses the graph name as a Redis key internally — special characters could cause unexpected behavior. Throw `ArgumentException` if invalid. Note: primary validation is upstream in `IngestionValidator` (Decision D12), but defense-in-depth at the activity boundary prevents misuse if the activity is ever called outside the workflow.

- [x] Task 7: Add project reference and register activities/services (AC: #1, #2, #3)
    - [x] 7.1 **Add ProjectReference from Server to Redis project.** The Server `.csproj` currently does NOT reference `Hexalith.Memories.Redis` — but the indexing activities need `NRedisStack`, `StackExchange.Redis`, and `NFalkorDB` which are in the Redis project's `.csproj`. Add to `Hexalith.Memories.Server.csproj`: `<ProjectReference Include="..\Hexalith.Memories.Redis\Hexalith.Memories.Redis.csproj" />`. This brings in all three NuGet packages transitively. Do NOT add the NuGet packages directly to Server — avoid duplication.
    - [x] 7.2 Register `IndexSyntacticActivity`, `IndexSemanticActivity`, `IndexGraphActivity` in `AddDaprWorkflow()`:
        ```csharp
        options.RegisterActivity<IndexSyntacticActivity>();
        options.RegisterActivity<IndexSemanticActivity>();
        options.RegisterActivity<IndexGraphActivity>();
        ```
    - [x] 7.3 Register `IGraphQueryBuilder` → `GraphQueryBuilder` as singleton:
        ```csharp
        builder.Services.AddSingleton<IGraphQueryBuilder, GraphQueryBuilder>();
        ```
    - [x] 7.4 Register **two separate** `IConnectionMultiplexer` instances — one for Redis Stack (port 6379, used by syntactic + semantic activities) and one for FalkorDB (port 6380, used by graph activity). **CRITICAL: these are different servers.**
          **Option A (Keyed Services — preferred, .NET 8+):**
        ```csharp
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("redis", (sp, _) =>
            ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("redis") ?? "localhost:6379"));
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("falkordb", (sp, _) =>
            ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("falkordb") ?? "localhost:6380"));
        ```
        Activities use `[FromKeyedServices("redis")] IConnectionMultiplexer` and `[FromKeyedServices("falkordb")] IConnectionMultiplexer` in constructors.
        **Option B (If Aspire already registers Redis):** Check AppHost — Aspire may register the Redis Stack multiplexer automatically. Only add the FalkorDB keyed registration manually. Syntactic/semantic activities use the default `IConnectionMultiplexer`; graph activity uses `[FromKeyedServices("falkordb")]`.
        **MUST CHECK:** Verify Aspire/ServiceDefaults before adding manual registrations. Double registration causes runtime errors.

- [x] Task 8: Add serialization support for IndexInput/IndexResult (AC: #1)
    - [x] 8.1 Add `IndexInput` and `IndexResult` to `MemoriesJsonContext` if using source-generated serialization, or verify they serialize correctly with `MemoriesJsonContext.Options`. These types cross the DAPR Workflow activity boundary and must serialize/deserialize cleanly.
    - [x] 8.2 For `float[]` in `IndexInput.EmbeddingVector`: standard `System.Text.Json` handles `float[]` natively — no custom converter needed. **Size note:** a 768-element float[] serializes to ~6KB JSON. This is stored in DAPR workflow state on every fan-out (3x for the three activities = ~18KB per memory unit ingestion). Acceptable for MVP. Future optimization: pass a vector reference (state store key) instead of the full array, or Base64-encode the bytes.
    - [x] 8.3 For `Dictionary<string, MetadataField>`: already handled by `MemoriesJsonContext.Options` (established in Story 1.2).

- [x] Task 9: Unit tests for `GraphQueryBuilder` (AC: #4) **MUST**
    - [x] 9.1 Create `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs`
    - [x] 9.2 Test: `BuildMergeMemoryUnitNode` — verify returned query uses MERGE + SET (not CREATE); verify `$id`, `$caseId`, `$content` parameters (never raw values); verify parameters dictionary contains all expected keys with correct values
    - [x] 9.3 Test: `BuildMergeCaseNode` — verify MERGE keyword (not CREATE); verify `$caseId` parameter
    - [x] 9.4 Test: `BuildMergeEdge` for each `EdgeType` — verify correct edge type label in query; verify confidence and origin parameters
    - [x] 9.5 Test: `BuildMergeEdge` with `Contains` type — verify confidence=1.0, origin=explicit
    - [x] 9.6 Test: `BuildMergeEdge` with `CausedBy` type — verify confidence=1.0
    - [x] 9.7 Test: `BuildMergeEdge` with `CorrelatedWith` type — verify confidence=0.8
    - [x] 9.8 Test: null/empty input validation — verify `ArgumentException` for null memoryUnitId, null caseId, etc.
    - [x] 9.9 **Injection prevention test**: Use distinctive, easily searchable input strings (e.g., `memoryUnitId = "INJECT_TEST_ID_12345"`, `content = "Robert'; DROP TABLE Students;--"`). Verify that NO returned query string contains any of these raw input values — all values MUST be in the parameters dictionary only. Test ALL builder methods with these adversarial inputs. This is the critical Gate 2 safety test.

- [x] Task 10: Unit tests for `IndexSyntacticActivity` (AC: #1) **MUST**
    - [x] 10.1 Create `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs`
    - [x] 10.2 Test: successful indexing — mock `IConnectionMultiplexer` → `IDatabase`, verify `HashSetAsync` called with correct key `{tenantId}:mu:{memoryUnitId}` and fields
    - [x] 10.3 Test: index already exists — verify activity handles "Index already exists" response gracefully (idempotent)
    - [x] 10.4 Test: Redis connection failure — verify exception propagates (not caught)
    - [x] 10.5 Test: verify tenant-namespaced key format

- [x] Task 11: Unit tests for `IndexSemanticActivity` (AC: #2) **MUST**
    - [x] 11.1 Create `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs`
    - [x] 11.2 Test: successful vector storage — mock `IConnectionMultiplexer` → `IDatabase`, verify `HashSetAsync` called with correct key `{tenantId}:vec:{memoryUnitId}`
    - [x] 11.3 Test: vector byte conversion — use **known gold values**: `new float[] { 1.0f, 0.0f, -1.0f }` converts to exactly 12 bytes. Verify exact byte equality: `1.0f` = `0x00 0x00 0x80 0x3F` (little-endian IEEE 754), `0.0f` = `0x00 0x00 0x00 0x00`, `-1.0f` = `0x00 0x00 0x80 0xBF`. Use `BitConverter.GetBytes(1.0f)` to compute expected bytes programmatically and assert `ShouldBe()` on the full byte array. This catches endianness or alignment bugs that silently corrupt every vector.
    - [x] 11.4 Test: index already exists — idempotent handling
    - [x] 11.5 Test: verify tenant-namespaced key format
    - [x] 11.6 Test: null or empty `EmbeddingVector` — verify `ArgumentException` or `ArgumentNullException` is thrown before attempting byte conversion. The activity MUST validate the vector is non-null and non-empty at the start of `RunAsync`.

- [x] Task 12: Unit tests for `IndexGraphActivity` (AC: #3, #4) **MUST**
    - [x] 12.1 Create `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexGraphActivityTests.cs`
    - [x] 12.2 Test: successful graph indexing — mock `IGraphQueryBuilder` and `IConnectionMultiplexer`, verify builder methods called in correct order: MergeCaseNode → MergeMemoryUnitNode → CreateEdge(Contains)
    - [x] 12.3 Test: with CausationId — verify `BuildMergeEdge` called with `CausedBy` type and the causation source
    - [x] 12.4 Test: with CorrelationId — verify `BuildMergeEdge` called with `CorrelatedWith` type
    - [x] 12.5 Test: without CausationId or CorrelationId — verify only Contains edge created (no CausedBy or CorrelatedWith)
    - [x] 12.6 Test: with both CausationId and CorrelationId — verify three edges created (Contains + CausedBy + CorrelatedWith)
    - [x] 12.7 Test: FalkorDB connection failure — verify exception propagates
    - [x] 12.8 Test: verify tenant database isolation — `SelectGraph(input.TenantId)` called with correct tenant ID

- [x] Task 13: Serialization round-trip tests for IndexInput/IndexResult (AC: #1) **MUST**
    - [x] 13.1 Create `tests/Hexalith.Memories.Contracts.Tests/V1/IndexInputSerializationTests.cs`
    - [x] 13.2 Test: round-trip serialization — create `IndexInput` with all fields populated (including `float[]` vector, `Dictionary<string, MetadataField>`, nullable CausationId/CorrelationId), serialize to JSON via `MemoriesJsonContext.Options`, deserialize back, serialize again, assert JSON strings are identical
    - [x] 13.3 Test: nullable fields — verify `CausationId = null` and `CorrelationId = null` serialize correctly (omitted or null in JSON)
    - [x] 13.4 Test: `IndexResult` round-trip — test all three backend values ("syntactic", "semantic", "graph") with Success=true/false and nullable ErrorMessage
    - [x] 13.5 Test: `SourceType` enum in `IndexInput` — verify camelCase serialization (matching Story 1.2 enum convention)

- [x] Task 14: Build and verify (AC: all) **MUST**
    - [x] 14.1 Run `dotnet build` — zero warnings, zero errors
    - [x] 14.2 Run `dotnet test` — all tests pass (existing + new)
    - [x] 14.3 Verify existing tests still pass (regression check)

## Dev Notes

### Story Dependencies

**This story is independent of Story 1.4 (Embedding Generation) merge status.** Story 1.4 is currently in "review" but this story does NOT depend on its code. `IndexInput` is a new contract type that the IngestionWorkflow (Story 1.6) will populate from prior activity outputs (`ExtractionResult` + `EmbeddingResult`). The `EmbeddingVector` field in `IndexInput` is a `float[]` — it does not reference `EmbeddingResult` directly. Both stories can be developed in parallel.

### Architecture Compliance

- **Namespace for indexing activities:** `Hexalith.Memories.Server.Activities.Indexing` — note this is `Indexing`, NOT `Ingestion`. Activities are in `Activities/Indexing/` subfolder per architecture file structure.
- **Namespace for graph services:** `Hexalith.Memories.Server.Graph` — for `IGraphQueryBuilder` and `GraphQueryBuilder`
- **Namespace for contract types:** `Hexalith.Memories.Contracts.V1` — for `IndexInput` and `IndexResult`
- **File-scoped namespaces:** `namespace Hexalith.Memories.Server.Activities.Indexing;` (Allman braces per .editorconfig)
- **Decision D1:** FalkorDB for MVP with escape hatch via `IGraphQueryBuilder`
- **Decision D3:** Eventual consistency + DAPR Workflow saga/compensation (no distributed transactions)
- **Decision D9:** `IGraphQueryBuilder` is a **safety interface** (prevents Cypher injection) — this is an exception to "no premature interfaces". All other classes remain concrete.
- **Decision D10:** Index naming supports concurrent versions for future model migration (`{tenantId}:{model-version}:syntactic`)
- **Decision D25:** Workflows orchestrate, activities do I/O, exceptions propagate to workflow retry policy
- **Package management:** Do NOT add version numbers to `.csproj` — use `Directory.Packages.props`. NRedisStack 1.3.0, StackExchange.Redis 2.12.4, NFalkorDB 1.0.0 are already in CPM.

### Critical Architectural Constraints

1. **Three-backend eventual consistency.** There is NO distributed transaction across Redis + FalkorDB. The IngestionWorkflow (Story 1.6) will fan-out indexing activities in parallel with retry policies. If one backend fails, compensation activities clean up partial writes. This story creates the individual activities; Story 1.6 orchestrates them.
2. **`IGraphQueryBuilder` is mandatory for ALL Cypher operations.** No raw Cypher string construction anywhere. This is a security gate (Gate 2). The builder returns `(query, parameters)` tuples; the activity executes them. CVE-2026-32247 (Cypher injection in Graphiti) validates this design choice.
3. **Physical FalkorDB isolation at database level.** Each tenant gets its own FalkorDB graph (database), not just label-based separation. This is a Gate 2 requirement — zero cross-tenant data leakage.
4. **RediSearch/Vector indexes are prefixed per tenant.** `{tenantId}:memories:idx` for syntactic, `{tenantId}:memories:vec` for semantic. The `Prefix()` parameter in `FTCreateParams` ensures each index only covers that tenant's documents. **Shared-resource note:** prefix isolation is logical, not physical — all tenants share the same Redis instance memory and CPU. A tenant with massive data could impact others. Physical Redis isolation (separate instances per tenant) is a future scaling concern, not MVP scope.
5. **Activities do NOT catch exceptions.** Let exceptions propagate to the DAPR Workflow retry policy (Decision D25).
6. **Redis HASH keys are namespaced per tenant.** Syntactic: `{tenantId}:mu:{memoryUnitId}`. Vector: `{tenantId}:vec:{memoryUnitId}`. This ensures tenant isolation at the key level.
7. **Index creation is idempotent.** Activities must handle "Index already exists" responses gracefully — wrapping `FT.CREATE` in try/catch for this specific error. **Tech debt note:** calling `FT.CREATE` inside `RunAsync` on every activity invocation is wasteful. For MVP this is acceptable (idempotent try/catch). Story 5.1 (Tenant Provisioning) should move index creation to `ProvisionRediSearchActivity` and `ProvisionRedisVectorActivity`, so indexing activities only write data. **Schema evolution warning:** the idempotent try/catch assumes identical schemas across calls. If the schema changes (e.g., adding a new field), the existing index silently retains the old schema. Schema changes require `FT.DROPINDEX` + `FT.CREATE` — this is NOT handled by the try/catch pattern. Document as a known limitation for the dev agent.
8. **Vector storage format.** Redis Vector Search expects vectors as `byte[]` (FLOAT32 binary blob). Convert `float[]` using `MemoryMarshal.AsBytes()` — do NOT iterate and convert element-by-element.

### NRedisStack API Specifics (v1.3.0)

**Creating a RediSearch index:**

```csharp
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;

SearchCommands ft = db.FT();
try
{
    ft.Create(
        $"{tenantId}:memories:idx",
        new FTCreateParams()
            .On(IndexDataType.HASH)
            .Prefix($"{tenantId}:mu:"),
        new Schema()
            .AddTextField("content", 1.0)
            .AddTagField("sourceUri")
            .AddTagField("sourceType")
            .AddTagField("contentHash")
            .AddTagField("caseId")
            .AddTagField("embeddingProvider"));
}
catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
{
    // Idempotent — index was created by a previous run
}
```

**Creating a Redis Vector index (HNSW):**

```csharp
SearchCommands ft = db.FT();
try
{
    ft.Create(
        $"{tenantId}:memories:vec",
        new FTCreateParams()
            .On(IndexDataType.HASH)
            .Prefix($"{tenantId}:vec:"),
        new Schema()
            .AddVectorField("embedding",
                VectorField.VectorAlgo.HNSW,
                new Dictionary<string, object>()
                {
                    ["TYPE"] = "FLOAT32",
                    ["DIM"] = input.EmbeddingDimensions.ToString(),
                    ["DISTANCE_METRIC"] = "COSINE"
                })
            .AddTagField("memoryUnitId")
            .AddTagField("caseId"));
}
catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
{
    // Idempotent
}
```

**Storing a HASH document:**

```csharp
await db.HashSetAsync(
    $"{tenantId}:mu:{memoryUnitId}",
    [
        new HashEntry("content", content),
        new HashEntry("sourceUri", sourceUri),
        new HashEntry("sourceType", sourceType.ToString()),
        new HashEntry("contentHash", contentHash),
        new HashEntry("caseId", caseId),
        new HashEntry("embeddingProvider", embeddingProvider),
    ]);
```

**Storing a vector:**

```csharp
byte[] vectorBytes = MemoryMarshal.AsBytes(input.EmbeddingVector.AsSpan()).ToArray();
await db.HashSetAsync(
    $"{tenantId}:vec:{memoryUnitId}",
    [
        new HashEntry("embedding", vectorBytes),
        new HashEntry("memoryUnitId", memoryUnitId),
        new HashEntry("caseId", caseId),
    ]);
```

### NFalkorDB API Specifics (v1.0.0)

**Connecting and selecting a graph:**

```csharp
using FalkorDB;

// db is IDatabase from IConnectionMultiplexer
FalkorDB.FalkorDB falkor = new(db);
Graph graph = falkor.SelectGraph(tenantId); // tenant = separate graph database
```

**Executing parameterized queries:**

```csharp
// NFalkorDB Query method accepts query string + parameters dictionary
var (query, parameters) = _graphQueryBuilder.BuildMergeMemoryUnitNode(...);
await graph.QueryAsync(query, parameters);
```

**CRITICAL: Verify `Graph.QueryAsync` signature before implementing.** NFalkorDB 1.0.0 is new and docs are sparse. The actual method signature may differ from the assumed `QueryAsync(string query, IDictionary<string, object> parameters)`. Check the source at https://github.com/FalkorDB/NFalkorDB — inspect `Graph.cs` for the exact overloads. FalkorDB's parameterized query protocol uses the `CYPHER` prefix format: `CYPHER param1=value1 param2=value2 MATCH ...`. NFalkorDB may expect parameters in this format rather than a dictionary. If the API doesn't accept a dictionary, the `IGraphQueryBuilder` return type may need adjustment — adapt before implementing activities.

**FalkorDB Cypher parameterized syntax:**

```cypher
MERGE (m:MemoryUnit {id: $id}) SET m.caseId = $caseId, m.content = $content
MERGE (c:Case {id: $caseId})
MATCH (s {id: $sourceId}), (t {id: $targetId}) MERGE (s)-[:CAUSED_BY {confidence: $confidence}]->(t)
```

**Known FalkorDB limitation:** Relationship types cannot be parameterized in Cypher. The edge type label (e.g., `CAUSED_BY`, `CONTAINS`) must be interpolated into the query string. Mitigate injection risk by:

1. Only accepting `EdgeType` enum values (closed set)
2. Converting enum to UPPER_SNAKE_CASE string (e.g., `CausedBy` → `CAUSED_BY`)
3. Validating the enum value before interpolation
4. Documenting this as a known limitation in `GraphQueryBuilder`

### Manual Verification Commands (Smoke Tests)

After implementing, the dev agent can verify data was written correctly using these CLI commands:

```bash
# Verify RediSearch index exists and has data
redis-cli -p 6379 FT.INFO {tenantId}:memories:idx
redis-cli -p 6379 FT.SEARCH {tenantId}:memories:idx "*" LIMIT 0 5

# Verify Redis Vector index exists and has data
redis-cli -p 6379 FT.INFO {tenantId}:memories:vec
redis-cli -p 6379 FT.SEARCH {tenantId}:memories:vec "*" RETURN 1 memoryUnitId LIMIT 0 5

# Verify FalkorDB graph has nodes and edges
redis-cli -p 6380 GRAPH.QUERY {tenantId} "MATCH (n) RETURN labels(n), count(n)"
redis-cli -p 6380 GRAPH.QUERY {tenantId} "MATCH ()-[r]->() RETURN type(r), count(r)"
```

Note: these require the DAPR infrastructure to be running (`docker compose up` or Aspire AppHost). Replace `{tenantId}` with your test tenant ID.

### Data Boundaries (from Architecture)

| Backend                 | Index Name                | Key Pattern                     | Tenant Isolation  |
| ----------------------- | ------------------------- | ------------------------------- | ----------------- |
| RediSearch (syntactic)  | `{tenantId}:memories:idx` | `{tenantId}:mu:{memoryUnitId}`  | Prefix-based      |
| Redis Vector (semantic) | `{tenantId}:memories:vec` | `{tenantId}:vec:{memoryUnitId}` | Prefix-based      |
| FalkorDB (graph)        | `{tenantId}` (graph name) | N/A (graph-level isolation)     | Physical database |

### Project Structure Notes

```
src/Hexalith.Memories.Contracts/V1/
├── IndexInput.cs                                # NEW — sealed record (shared input for all 3 activities)
└── IndexResult.cs                               # NEW — sealed record (per-activity result)

src/Hexalith.Memories.Server/
├── Activities/
│   └── Indexing/                                 # NEW folder (NOT Ingestion!)
│       ├── IndexSyntacticActivity.cs             # NEW — RediSearch write
│       ├── IndexSemanticActivity.cs              # NEW — Redis Vector write
│       └── IndexGraphActivity.cs                 # NEW — FalkorDB write
├── Graph/                                        # NEW folder
│   ├── IGraphQueryBuilder.cs                     # NEW — safety interface (Decision D9)
│   └── GraphQueryBuilder.cs                      # NEW — concrete implementation
└── Program.cs                                    # MODIFIED — register 3 activities + IGraphQueryBuilder

tests/Hexalith.Memories.Server.Tests/
├── Activities/
│   └── Indexing/                                 # NEW folder
│       ├── IndexSyntacticActivityTests.cs        # NEW
│       ├── IndexSemanticActivityTests.cs         # NEW
│       └── IndexGraphActivityTests.cs            # NEW
└── Graph/                                        # NEW folder
    └── GraphQueryBuilderTests.cs                 # NEW — injection prevention tests
```

Alignment: Matches architecture.md file structure exactly. `Activities/Indexing/` is a SEPARATE folder from `Activities/Ingestion/` — indexing is a distinct pipeline stage. `Graph/` folder matches architecture for `GraphQueryBuilder.cs`.

### Testing Requirements

- **Framework:** xUnit + Shouldly + NSubstitute (aligned with EventStore per Decision D16)
- **GraphQueryBuilder testing:** Test the builder directly — it's a pure function (input → query+params). No mocks needed. Verify parameterized output, validate no raw values in query strings, check all edge types.
- **Activity testing pattern:** Mock `IConnectionMultiplexer` → returns mock `IDatabase`. For syntactic/semantic activities, verify `HashSetAsync` calls. For graph activity, mock `IGraphQueryBuilder` and verify call sequence.
- **NFalkorDB mocking strategy:** The `Graph` class from NFalkorDB may not be easily mockable (depends on implementation). If `Graph.QueryAsync` is virtual, mock it directly. If sealed, create a thin wrapper interface (`IGraphClient` with `QueryAsync`) — but ONLY if mocking is impossible without it. Check NFalkorDB source first. Prefer the simplest approach.
- **Redis mocking:** `IConnectionMultiplexer` and `IDatabase` are interfaces in StackExchange.Redis — `NSubstitute.Substitute.For<IDatabase>()` works. `SearchCommands` from NRedisStack is likely a concrete sealed class — `db.FT()` is an extension method that returns it. **If `SearchCommands` is not mockable:** (a) For `HashSetAsync` tests — mock `IDatabase` directly, this is the standard path. (b) For `FT.CREATE` (index creation) tests — extract index creation to a private helper method and test the activity's data-writing logic separately from index creation. Or mark index creation tests as `[Trait("Category", "Integration")]` requiring a real Redis Stack instance. (c) As last resort, create a thin `ISearchIndex` wrapper — but ONLY if simpler approaches fail.
- **Assertion:** Use `.ShouldBe()` for value comparisons, `.ShouldContain()` for query string checks, `Should.ThrowAsync<T>()` for exception assertions.
- **Test data construction:** Activities are tested independently — no workflow engine needed. Construct `IndexInput` manually in each test with `new IndexInput { MemoryUnitId = "test-id", TenantId = "test-tenant", ... }`. The `required` properties pattern (not positional) makes test construction readable. Consider a test helper method `CreateTestIndexInput()` that returns a fully populated `IndexInput` with sensible defaults — individual tests override specific fields.

### Previous Story Intelligence (from 1-4)

**Patterns established:**

- Dapr WorkflowActivity pattern: `WorkflowActivity<TInput, TOutput>` with constructor DI, single-responsibility `RunAsync`, exceptions propagate
- Typed record inputs/outputs: `sealed record` for all activity I/O types
- DI registration: activities registered in `AddDaprWorkflow()` options, services registered via `AddSingleton<>` or `AddHttpClient<>`
- Test pattern: mock dependencies via NSubstitute, test activity `RunAsync` directly
- `EmbeddingClient` changed from `sealed` to non-sealed with `virtual GenerateAsync` for NSubstitute mocking — same pattern may apply to `GraphQueryBuilder` if needed (but prefer interface since `IGraphQueryBuilder` is a safety interface anyway)

**Debug learnings from 1-4:**

- `WorkflowActivityContext` does not expose `CancellationToken` — use `CancellationToken.None`
- NSubstitute cannot proxy sealed classes — mark classes non-sealed if they need mocking, or use interfaces
- All existing 88 tests must remain passing (regression guard)

**Files to preserve (do NOT modify or delete):**

- All existing V1 contract types in `src/Hexalith.Memories.Contracts/V1/`
- All existing activities in `src/Hexalith.Memories.Server/Activities/Ingestion/`
- All existing actors in `src/Hexalith.Memories.Server/Actors/`
- All existing ingestion services in `src/Hexalith.Memories.Server/Ingestion/`
- `tests/Hexalith.Memories.Contracts.Tests/` — all existing tests
- `tests/Hexalith.Memories.Server.Tests/` — all existing tests
- `src/Hexalith.Memories.Server/Program.cs` — modify (add registrations), do NOT rewrite

### Git Intelligence

Recent commits show:

- Story 1.3 replaced Apache Tika with Kreuzberg for content extraction (29 files, 3089 insertions)
- Story 1.2 established all V1 contract types (MemoryUnit, GraphEdge, EdgeType, EdgeOrigin, EdgeTypeDefaults)
- Story 1.4 added embedding generation (EmbeddingClient, RateLimiterLogic, GenerateEmbeddingActivity) — currently in review
- `Hexalith.Memories.Redis` project exists with `NRedisStack`, `StackExchange.Redis`, `NFalkorDB` packages already in .csproj — but currently only contains `RedisPlaceholder.cs`
- `Directory.Packages.props` already has all required packages: NRedisStack 1.3.0, StackExchange.Redis 2.12.4, NFalkorDB 1.0.0

### Anti-Patterns to Avoid

- **DO NOT construct raw Cypher strings in activities or anywhere outside `GraphQueryBuilder`.** ALL Cypher must go through `IGraphQueryBuilder`. This is a security gate. CVE-2026-32247 demonstrates real Cypher injection risk.
- **DO NOT use JSON data type for Redis indexes.** Use HASH data type — simpler, more performant for this use case. RediSearch `FT.CREATE` with `On(IndexDataType.HASH)`.
- **DO NOT store vectors as JSON arrays.** Redis Vector Search expects binary `FLOAT32` blobs. Convert `float[]` to `byte[]` via `MemoryMarshal.AsBytes()`.
- **DO NOT create a shared Redis index across tenants.** Each tenant gets tenant-namespaced indexes with `Prefix()` isolation. Shared indexes violate Gate 2 requirements.
- **DO NOT use FalkorDB label-based tenant separation.** Use separate graph databases per tenant (`SelectGraph(tenantId)`) — physical isolation, not logical.
- **DO NOT add VerifyConsistencyActivity in this story.** That's Story 1.6 (IngestionWorkflow orchestration). This story creates the three indexing activities only.
- **DO NOT add compensation activities in this story.** Compensation (cleanup on failure) is part of the IngestionWorkflow in Story 1.6.
- **DO NOT create the IngestionWorkflow in this story.** This story creates individual activities. Story 1.6 orchestrates them.
- **DO NOT catch exceptions in activities.** Let them propagate to the workflow retry policy (Decision D25).
- **DO NOT put indexing activities in `Activities/Ingestion/`.** They go in `Activities/Indexing/` — a separate pipeline stage per architecture file structure.
- **DO NOT add Redis connection management to the Server project.** The `Hexalith.Memories.Redis` project owns Redis infrastructure (architecture defines `RediSearchIndexManager`, `RediSearchQueryExecutor`, `FalkorDbQueryExecutor` there). **However, for MVP, activities directly use `IConnectionMultiplexer`** — the Redis project currently only has a placeholder file. Epic 2 (Search stories) will extract query/index logic to Redis project services (`RediSearchQueryExecutor`, `RedisVectorQueryExecutor`, `FalkorDbQueryExecutor`). This is an intentional MVP simplification, not a deviation. Keep it simple.
- **DO NOT use `db.Execute("FT.CREATE", ...)` raw commands.** Use the `SearchCommands` fluent API (`db.FT().Create(...)`) provided by NRedisStack.
- **DO NOT iterate float[] to convert to byte[].** Use `MemoryMarshal.AsBytes()` for zero-copy conversion.

### Cross-Cutting Dependency Map

```
Contracts.V1 (IndexInput, IndexResult, MemoryUnit, GraphEdge, EdgeType) ← Server (this story adds to Server)
                                                                           ↑
                                                                    IConnectionMultiplexer (Redis)
                                                                    NRedisStack (SearchCommands, Schema)
                                                                    NFalkorDB (FalkorDB, Graph)
                                                                    IGraphQueryBuilder (safety interface)
```

### References

- [Source: architecture.md#Data Boundaries] — `{tenantId}:memories:idx`, `{tenantId}:memories:vec`, separate database per tenant for FalkorDB
- [Source: architecture.md#FalkorDB Decision] — IGraphQueryBuilder as extraction boundary and injection prevention
- [Source: architecture.md#Security Architecture] — IGraphQueryBuilder structural Cypher injection prevention
- [Source: architecture.md#Architectural Components Summary] — IGraphQueryBuilder is MVP-critical safety interface
- [Source: architecture.md#DAPR Workflow Patterns] — Fan-out/fan-in pattern for parallel indexing
- [Source: architecture.md#File Structure] — Activities/Indexing/ folder, Server/Graph/ folder
- [Source: architecture.md#Decision D1] — FalkorDB for MVP
- [Source: architecture.md#Decision D3] — Eventual consistency + compensation
- [Source: architecture.md#Decision D9] — Safety interfaces (IGraphQueryBuilder) are interfaces
- [Source: architecture.md#Decision D10] — Index naming supports concurrent versions
- [Source: architecture.md#Decision D25] — Workflows orchestrate, activities do I/O
- [Source: architecture.md#Cross-Cutting Concern #5] — RediSearch/Vector index schemas immutable after creation
- [Source: architecture.md#PRD Deviations] — No distributed transaction across Redis + FalkorDB
- [Source: epics.md#Story 1.5] — Acceptance criteria, user story
- [Source: epics.md#Story 1.6] — Ingestion workflow orchestration (next story, consumes these activities)
- [Source: prd.md#FR6] — Memory unit fully searchable across all axes after ingestion
- [Source: prd.md#NFR8] — Zero cross-tenant data leakage (hard gate)

## Implementation Readiness Addendum (2026-05-18)

This story is historical completed scope and may remain closed. If it is reopened, reimplemented, or used as a template for a future technical story, completion must include observable proof that one tenant-scoped memory unit is discoverable from all relevant retrieval backends.

Required future-rework evidence:

1. RediSearch proof that the memory unit is searchable by tenant-scoped full-text query.
2. Redis Vector proof that the same memory unit is retrievable by tenant-scoped vector lookup.
3. FalkorDB proof that the same memory unit appears in the tenant graph with the expected case and optional causation/correlation edges.
4. Negative tenant-isolation evidence showing another tenant cannot see the indexed unit.
5. If a backend is unavailable in the proof environment, an explicit degraded-state explanation and follow-up owner.

Activity unit tests and graph query builder tests alone are not sufficient evidence for future work.

## Definition of Done

1. `IndexSyntacticActivity` creates tenant-namespaced RediSearch index and stores memory unit as HASH with full-text searchable content
2. `IndexSemanticActivity` creates tenant-namespaced Redis Vector index (HNSW/COSINE) and stores embedding as FLOAT32 binary blob
3. `IndexGraphActivity` merges memory unit node (idempotent) + case node + edges (Contains, optional CausedBy, optional CorrelatedWith) in tenant-isolated FalkorDB graph via separate FalkorDB IConnectionMultiplexer (port 6380)
4. `IGraphQueryBuilder` enforces parameterized-only Cypher queries — no raw string construction anywhere
5. `GraphQueryBuilder` generates correct Cypher with `$` parameter placeholders for all operations
6. `IndexInput` and `IndexResult` contract types support DAPR Workflow serialization with round-trip JSON tests
7. Index creation is idempotent (handles "already exists" gracefully)
8. All unit tests pass (GraphQueryBuilder injection prevention with adversarial inputs, activity behavior, edge combinations, vector byte gold-value, null vector validation, serialization round-trips)
9. `dotnet build` zero warnings, `dotnet test` all pass
10. No regression in existing tests

## Change Log

- 2026-03-29: Story created — comprehensive three-backend indexing guide with NRedisStack, NFalkorDB API specifics, IGraphQueryBuilder safety interface, and tenant isolation patterns
- 2026-03-29: Party mode review applied — 11 improvements: (1) AC #5 version naming clarified for MVP vs future, (2) EdgeType UPPER_SNAKE_CASE conversion logic specified with explicit switch/regex, (3) Index creation in RunAsync flagged as tech debt for Story 5.1, (4) Graph disposability check for NFalkorDB documented, (5) Aspire Redis registration double-check instructions added, (6) Serialization round-trip tests added as Task 13 for IndexInput/IndexResult, (7) Story dependency independence from 1.4 documented, (8) Null/empty EmbeddingVector validation test added, (9) Injection prevention test strengthened with adversarial inputs, (10) Vector byte conversion gold-value assertion specified, (11) MVP bypass of Redis project services documented as intentional
- 2026-03-29: Advanced elicitation round 2 (5 methods: Occam's razor, critique/refine, war room, Feynman technique, comparative analysis) — 12 improvements: (OR-1) Removed YAGNI BuildNodeExistsCheck, (OR-2) Simplified IndexResult to Backend+MemoryUnitId+TenantId (dropped dead Success/ErrorMessage), (OR-3) Merged vector validation subtasks, (CR-1) Added Task 3.5 BuildMergeStubNode implementation, (CR-2) Updated DoD for MERGE pattern, (CR-3) Renumbered Task 7 subtasks, (CR-4) Added ILogger to semantic/graph activities, (WR-1) Added manual verification CLI commands, (WR-2) IndexInput uses required+init pattern (not positional record), (FT-1) CRITICAL: separate IConnectionMultiplexer for FalkorDB port 6380 with keyed DI, (FT-2) Test helper note for IndexInput construction, (CA-1) BuildCreateEdge→BuildMergeEdge with MERGE Cypher for idempotent edges
- 2026-03-29: Advanced elicitation round 1 (5 methods: pre-mortem, red team, failure mode, first principles, security audit) — 12 improvements: (PM-1) NFalkorDB QueryAsync signature verification task with CYPHER prefix format warning, (PM-2) SearchCommands mockability fallback strategy documented, (PM-3) Server→Redis ProjectReference added as Task 7.0, (PM-4) float[] serialization size ~6KB/18KB documented, (PM-5) Index schema collision warning for schema evolution, (RT-1) Defensive TenantId format validation in IndexGraphActivity, (RT-2) 10-second timeout on all FalkorDB graph operations, (FM-1) Vector byte length validation before storing, (FM-2) MERGE+SET instead of CREATE for memory unit node (idempotency), (FM-3) BuildMergeStubNode for CausedBy/CorrelatedWith targets that may not exist yet, (FP-1) Redis shared-resource risk documented, (SA-1) Structured ILogger logging for all three activities
- 2026-03-29: Story implementation completed — all 14 tasks done, 153 tests pass (63 contracts + 90 server), 0 warnings
- 2026-03-29: Code review patch fixes applied — resolved 10 review findings covering graph edge safety, full-text indexing coverage, semantic dimension validation, Aspire wiring, and deterministic integration fixtures

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- NFalkorDB 1.0.0 API verified: no `Graph` class or `SelectGraph` method. Used `NFalkorDB.FalkorDB.QueryAsync(graphId, query, parameters)` directly with tenantId as graphId.
- NRedisStack 1.3.0: `db.FT()` extension method is in `NRedisStack.RedisStackCommands` namespace (not `NRedisStack`). `VectorField` is a nested class under `Schema` — use `Schema.VectorField.VectorAlgo.HNSW`.
- SearchCommands.Create() internally calls `db.Execute(string, ICollection<object>, CommandFlags)` overload — unit tests mock this to throw "Index already exists" to bypass index creation and test data-writing logic.
- NFalkorDB.FalkorDB.QueryAsync() internally calls `db.ExecuteAsync` — tests provide a fake 3-element RedisResult array: [headers, data, statistics].

### Completion Notes List

- Task 1: Created `IndexInput` (sealed record with required+init properties) and `IndexResult` (positional record) in Contracts/V1/
- Task 2: Created `IGraphQueryBuilder` safety interface in Server/Graph/ with 4 methods returning (Query, Parameters) tuples
- Task 3: Implemented `GraphQueryBuilder` with MERGE Cypher, UPPER_SNAKE_CASE edge labels via switch expression, ArgumentException validation, and injection prevention
- Task 4: Created `IndexSyntacticActivity` — RediSearch index creation (idempotent try/catch), tenant-namespaced HASH storage, keyed DI for Redis
- Task 5: Created `IndexSemanticActivity` — Redis Vector index (HNSW/COSINE), MemoryMarshal vector conversion with byte length validation, keyed DI
- Task 6: Created `IndexGraphActivity` — FalkorDB graph operations via IGraphQueryBuilder, tenant validation regex, 10s timeout, separate keyed DI for port 6380
- Task 7: Registered 3 activities in AddDaprWorkflow, registered IGraphQueryBuilder as singleton, registered two keyed IConnectionMultiplexer instances (redis/falkordb)
- Task 8: No changes needed — MemoriesJsonContext.Options handles float[], Dictionary, and enums natively
- Tasks 9-12: 27 unit tests covering GraphQueryBuilder (injection prevention, edge types, validation), activity behavior (key format, exception propagation, vector conversion gold values, null/empty validation)
- Task 13: 8 serialization round-trip tests for IndexInput and IndexResult
- Task 14: `dotnet build` 0 warnings, `dotnet test` 153 passed (63 existing + 90 server including 35 new)

### File List

- src/Hexalith.Memories.Contracts/V1/IndexInput.cs (NEW)
- src/Hexalith.Memories.Contracts/V1/IndexResult.cs (NEW)
- src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs (NEW)
- src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs (NEW)
- src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs (NEW)
- src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs (NEW)
- src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs (NEW)
- src/Hexalith.Memories.Server/Program.cs (MODIFIED — added usings, keyed DI registrations, activity registrations)
- tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs (NEW)
- tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs (NEW)
- tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs (NEW)
- tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexGraphActivityTests.cs (NEW)
- tests/Hexalith.Memories.Contracts.Tests/V1/IndexInputSerializationTests.cs (NEW)
- tests/Hexalith.Memories.Contracts.Tests/V1/IndexResultSerializationTests.cs (NEW)

### Review Findings

- [x] [Review][Patch] Unlabelled edge matches can attach graph relationships to the wrong nodes [src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:86]
- [x] [Review][Patch] Edge MERGE keys on confidence and origin, so updates can create duplicate relationships [src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:86]
- [x] [Review][Patch] Syntactic indexing drops metadata and only indexes source fields as tags, not full-text content [src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs:51]
- [x] [Review][Patch] Enum values are persisted with PascalCase instead of the required camelCase convention [src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:49]
- [x] [Review][Patch] Tenant ID validation is inconsistent across Redis and FalkorDB indexing paths [src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs:41]
- [x] [Review][Patch] Semantic index creation never verifies an existing tenant index uses the same vector dimensions [src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs:70]
- [x] [Review][Patch] Redis and FalkorDB multiplexer setup relies on localhost fallbacks instead of explicit Aspire resource references [src/Hexalith.Memories.Server/Program.cs:44]
- [x] [Review][Patch] FalkorDB integration tests assert ResultSet.Count instead of the returned count value, masking broken graph behavior [tests/Hexalith.Memories.IntegrationTests/Graph/GraphQueryBuilderIntegrationTests.cs:40]
- [x] [Review][Patch] Graph activity unit tests never verify the tenant graph ID passed into FalkorDB queries [tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexGraphActivityTests.cs:149]
- [x] [Review][Patch] Integration fixtures pin `latest` images and only wait for open ports, making test runs nondeterministic [tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs:23]
