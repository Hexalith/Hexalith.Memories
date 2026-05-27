# Story 3.1: Create and List Cases

Status: done

## Prerequisites

- Epic 1 (Foundation, Ingestion & Graph Edge Indexing) must be `done` — case creation depends on the FalkorDB graph infrastructure, Redis indexing, and ingestion pipeline built in Epic 1
- Epic 2 (Three-Axis Search, Fusion & Benchmark Validation) should be `done` — search scoping by `CaseId` already exists as a query parameter; this story adds the Case entity that the parameter references

## Story

As a developer,
I want to create cases within a tenant and list existing cases,
So that I can organize memory units into meaningful groups with strict ownership boundaries.

## Acceptance Criteria

1. **Given** a valid tenant context
   **When** I create a case with a name and optional description
   **Then** a case is created with a unique ID (ULID), tenant association, creation timestamp, and status "active"
   **And** a case node is created in FalkorDB within the tenant's database
   **And** the case is immediately visible in the case list

2. **Given** a tenant with multiple cases
   **When** I list cases
   **Then** all cases for the tenant are returned with ID, name, description, status, creation date, and memory unit count (FR30)

3. **Given** a memory unit is ingested into a case
   **When** the ingestion completes
   **Then** the memory unit belongs to exactly one case — no multi-case membership (FR32)
   **And** a `contains` edge is created from the case node to the memory unit node in FalkorDB (FR33)

4. **Given** a memory unit already belongs to a case
   **When** an attempt is made to assign it to a different case
   **Then** the operation is rejected with error code `SINGLE_CASE_OWNERSHIP` and suggestion "Delete the unit and re-ingest into the target case"

## Definition of Done

- All acceptance criteria verified (AC 1-4)
- All unit, serialization, and integration tests pass
- No new compiler warnings (`warnings as errors` is enabled)
- `dotnet build` succeeds for solution
- Code follows .editorconfig conventions (file-scoped namespaces, Allman braces, sealed classes, ITANEO header)

## Recommended Implementation Order

`Task 1 (Contracts) → Task 5 (Graph Builder) → Task 3 (Validator) → Task 2 (Service) → Task 4 (Endpoints) → Task 7 (DI) → Task 6 (Verify) → Tasks 8-11 (Tests)`

Task numbering below groups by component, but implementation should follow the dependency chain above. Task 5 (graph builder extensions) must be complete before Task 2 (service) can compile.

## Tasks / Subtasks

- [x] Task 1: Create `Case` domain model in Contracts (AC: 1, 2)
    - [x] 1.1 Create `src/Hexalith.Memories.Contracts/V1/Case.cs` as `public sealed record`:
        ```csharp
        public sealed record Case(
            string Id,                    // ULID, globally unique, time-sortable
            string TenantId,              // Physical index routing key
            string Name,                  // Human-readable case name
            string? Description,          // Optional description
            CaseStatus Status,            // Active, Closed, etc.
            DateTimeOffset CreatedAt,     // Creation timestamp
            DateTimeOffset LastUpdated,   // Last modification timestamp
            int MemoryUnitCount);         // Count of memory units in this case
        ```
    - [x] 1.2 Create `src/Hexalith.Memories.Contracts/V1/CaseStatus.cs` as enum with `CamelCaseStringEnumConverter<CaseStatus>`:
        ```csharp
        [JsonConverter(typeof(CamelCaseStringEnumConverter<CaseStatus>))]
        public enum CaseStatus { Active, Closed }
        ```
    - [x] 1.3 Create `src/Hexalith.Memories.Contracts/V1/CreateCaseInput.cs` as `public sealed record`:
        ```csharp
        public sealed record CreateCaseInput(
            string TenantId,    // Required — tenant context
            string Name,        // Required — case display name
            string? Description); // Optional
        ```
        Non-nullable `string` parameters are sufficient — `System.Text.Json` will throw `JsonException` (returning 400 via ASP.NET) if `TenantId` or `Name` is missing from the request body. No `[JsonRequired]` attribute needed
    - [x] 1.4 Register `Case`, `CaseStatus`, `CreateCaseInput`, and `List<Case>` in `MemoriesJsonContext` with `[JsonSerializable]` attributes. **AOT CRITICAL:** Register `List<Case>` (concrete collection type), not just `Case` — `System.Text.Json` source generators need concrete collection types for correct AOT code generation

- [x] Task 2: Create `CaseService` in Server (AC: 1, 2, 3, 4) — **DEPENDS ON Task 5** (graph builder extensions must exist before service can call them; implement Task 5 first or together with Task 2)
    - [x] 2.1 Create directory `src/Hexalith.Memories.Server/Cases/`
    - [x] 2.2 Create `src/Hexalith.Memories.Server/Cases/CaseService.cs` as `internal sealed class` with constructor-injected dependencies:
        - `[FromKeyedServices("redis")] IConnectionMultiplexer redis` — for case metadata storage in Redis Hash
        - `[FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb` — for executing FalkorDB graph queries (create `NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase())` same as `IndexGraphActivity`)
        - `IGraphQueryBuilder graphQueryBuilder` — for building parameterized Cypher queries (builds only, does NOT execute)
        - `ILogger<CaseService> logger` — structured logging
    - [x] 2.3 Implement `CreateCaseAsync(CreateCaseInput input, CancellationToken cancellationToken)`:
        - Generate ULID for `Id` (use `Ulid.NewUlid().ToString()`) — check `Directory.Packages.props` for existing ULID package (`Cysharp/Ulid` or `RobThree/NUlid`) and match existing usage pattern
        - Validate input via `CaseValidator` (name not empty, tenant not empty)
        - Store case metadata in Redis Hash at key `{tenantId}:case:{caseId}` with fields: `id`, `tenantId`, `name`, `description`, `status` ("active"), `createdAt`, `lastUpdated`, `memoryUnitCount` (0)
        - Create case node in FalkorDB within tenant's graph database: use the **new overload** `IGraphQueryBuilder.BuildMergeCaseNode(caseId, name, tenantId, createdAt)` (added in Task 5.1). Execute via `NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase()); await falkor.QueryAsync(tenantId, query, parameters)` — **parameterized query only**, no raw Cypher string construction. See `IndexGraphActivity.cs` lines 41-46 for execution pattern
        - Return the created `Case` record
    - [x] 2.4 Extract `private async Task<int> GetMemoryUnitCountSafe(NFalkorDB.FalkorDB falkor, string tenantId, string caseId)` helper:
        - Calls `BuildCountCaseMemoryUnits(caseId)`, executes via `falkor.QueryAsync(tenantId, query, parameters)`
        - On success: parse result, return count (treat empty result set as 0)
        - On failure (any exception): log warning with `_logger`, return 0. Never throw
        - Reused by both `ListCasesAsync` and `GetCaseAsync`
    - [x] 2.5 Implement `ListCasesAsync(string tenantId, int maxResults = 100, CancellationToken cancellationToken = default)`:
        - Scan Redis keys matching `{tenantId}:case:*` pattern
        - For each key, read hash fields and construct `Case` record. **Race condition guard:** If hash read returns empty (case deleted between SCAN and read), skip that key — do not throw or return a partial record
        - For `MemoryUnitCount`: call `GetMemoryUnitCountSafe` (Task 2.4). **N+1 risk:** This issues one FalkorDB query per case. Acceptable for MVP (cases per tenant expected < 100), but document as a known optimization target for Story 3.4 (batch query or graph-side aggregation)
        - Return `List<Case>` ordered by `CreatedAt` descending, capped at `maxResults` (default 100). This guards against unbounded N+1 queries if a tenant creates thousands of cases
    - [x] 2.6 Implement `GetCaseAsync(string tenantId, string caseId, CancellationToken cancellationToken)`:
        - Read Redis Hash at `{tenantId}:case:{caseId}`
        - If not found, return `null`
        - Call `GetMemoryUnitCountSafe` (Task 2.4) for memory unit count — `// Graph ID is tenantId, NOT caseId — each tenant has one FalkorDB database`
        - Return `Case` record
    - [x] 2.7 **Single-case ownership enforcement** — This is already structurally enforced: `MemoryUnit.CaseId` is a `required string` set at ingestion time, and the `IngestionWorkflow` creates a `contains` edge from the case node to the memory unit node. Re-assignment is not possible without deleting and re-ingesting. Add a validation check in `CaseService` that returns `ErrorResponse` with code `SINGLE_CASE_OWNERSHIP` if any future endpoint attempts reassignment.

- [x] Task 3: Create `CaseValidator` (AC: 1)
    - [x] 3.1 Create `src/Hexalith.Memories.Server/Cases/CaseValidator.cs` as `internal static class`
    - [x] 3.2 Implement `ValidateCreateCase(CreateCaseInput input)` returning `ErrorResponse?`:
        - `TenantId` required, validated via `TenantIdGuard.Validate` (same guard used by `IngestionInputValidator` and `IndexGraphActivity`) — catch `ArgumentException` from `TenantIdGuard` and convert to `ErrorResponse`
        - `Name` required, max 200 characters
        - `Description` optional, max 2000 characters if provided
        - Return `null` if valid, `ErrorResponse` with appropriate code and suggestion if invalid

- [x] Task 4: Create API endpoints (AC: 1, 2)
    - [x] 4.1 Add `POST /api/tenants/{tenantId}/cases` endpoint in `Program.cs`:
        ```csharp
        app.MapPost("/api/tenants/{tenantId}/cases", async (
            string tenantId,
            CreateCaseInput input,
            CaseService caseService,
            CancellationToken cancellationToken) =>
        {
            // Override tenantId from route (never trust body)
            var validatedInput = input with { TenantId = tenantId };
            ErrorResponse? error = CaseValidator.ValidateCreateCase(validatedInput);
            if (error is not null) return Results.BadRequest(error);
            Case created = await caseService.CreateCaseAsync(validatedInput, cancellationToken);
            return Results.Created($"/api/tenants/{tenantId}/cases/{created.Id}", created);
        });
        ```
    - [x] 4.2 Add `GET /api/tenants/{tenantId}/cases` endpoint in `Program.cs`:
        ```csharp
        app.MapGet("/api/tenants/{tenantId}/cases", async (
            string tenantId,
            int? limit,
            CaseService caseService,
            CancellationToken cancellationToken) =>
        {
            try { TenantIdGuard.Validate(tenantId); }
            catch (ArgumentException) { return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed.")); }
            int effectiveLimit = Math.Clamp(limit ?? 100, 1, 500);
            List<Case> cases = await caseService.ListCasesAsync(tenantId, effectiveLimit, cancellationToken);
            return Results.Ok(cases);
        });
        ```
    - [x] 4.3 Add `GET /api/tenants/{tenantId}/cases/{caseId}` endpoint in `Program.cs`:
        ```csharp
        app.MapGet("/api/tenants/{tenantId}/cases/{caseId}", async (
            string tenantId,
            string caseId,
            CaseService caseService,
            CancellationToken cancellationToken) =>
        {
            try { TenantIdGuard.Validate(tenantId); }
            catch (ArgumentException) { return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed.")); }
            Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
            return caseResult is null
                ? Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."))
                : Results.Ok(caseResult);
        });
        ```
    - [x] 4.4 **Tenant ID trust boundary:** The `tenantId` from the route parameter is used as the authoritative value. The body's `TenantId` is overridden via `with { TenantId = tenantId }`. This matches the architecture: "tenant ID from request payload is never trusted"
    - [x] 4.5 Add inline comment on each GET endpoint: `// TODO: Extract TenantIdValidationFilter when endpoint count > 5 (Story 3.4+)` — the try-catch `TenantIdGuard` pattern is duplicated across endpoints; acceptable for 3 endpoints, extract when more are added

- [x] Task 5: Extend `IGraphQueryBuilder` for case operations (AC: 1, 2)
    - [x] 5.1 Add a **new overload** to `IGraphQueryBuilder` — do NOT modify the existing `BuildMergeCaseNode(string caseId)` signature. Add:
        ```csharp
        (string Query, IDictionary<string, object> Parameters) BuildMergeCaseNode(
            string caseId, string name, string tenantId, DateTimeOffset createdAt);
        ```
        Implementation in `GraphQueryBuilder`:
        ```cypher
        MERGE (c:Case {id: $caseId}) SET c.name = $name, c.tenantId = $tenantId, c.createdAt = $createdAt
        ```
    - [x] 5.2 **Backward compatibility:** The existing `BuildMergeCaseNode(string caseId)` at `IGraphQueryBuilder.cs:26` remains untouched — `IndexGraphActivity.cs:45` continues to call it as-is. The new overload is used exclusively by `CaseService.CreateCaseAsync` for richer node creation. Two signatures, zero breaking changes
    - [x] 5.3 Add `BuildCountCaseMemoryUnits(string caseId)` to `IGraphQueryBuilder`:
        ```cypher
        MATCH (c:Case {id: $caseId})-[:CONTAINS]->(m) RETURN count(m) AS count
        ```
    - [x] 5.4 Update `GraphQueryBuilder` implementation for both new methods with parameter validation

- [x] Task 6: Verify `CONTAINS` edge creation in ingestion workflow (AC: 3)
    - [x] 6.1 **ALREADY IMPLEMENTED** — `IndexGraphActivity.cs` (lines 63-70) already creates the `CONTAINS` edge via `_graphQueryBuilder.BuildMergeEdge(input.CaseId, input.MemoryUnitId, EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit)`. Verify this works correctly with the new `CaseService`-created case nodes by running an integration test
    - [x] 6.2 **Edge case:** If the case node doesn't exist yet in FalkorDB (e.g., case was created via `CaseService` but graph node creation failed), the `MERGE` in `IndexGraphActivity` step 1 (`BuildMergeCaseNode`) will create a minimal stub. Verify that the `CaseService.CreateCaseAsync` graph node creation is consistent with what `IndexGraphActivity` expects

- [x] Task 7: Register `CaseService` in DI (AC: 1, 2)
    - [x] 7.1 In `Program.cs`, register `CaseService`:
        ```csharp
        builder.Services.AddScoped<CaseService>();
        ```
    - [x] 7.2 Follow existing DI pattern — `CaseService` receives `[FromKeyedServices("redis")] IConnectionMultiplexer`, `[FromKeyedServices("falkordb")] IConnectionMultiplexer`, and `IGraphQueryBuilder` via constructor injection (same keyed DI pattern as `IndexGraphActivity`)

- [x] Task 8: Unit tests for Contracts (AC: 1, 2)
    - [x] 8.1 Create `tests/Hexalith.Memories.Contracts.Tests/V1/CaseSerializationTests.cs`:
        - Test `Case` serialization round-trip with `MemoriesJsonContext.Options`
        - Verify camelCase property names in JSON output (`id`, `tenantId`, `name`, `description`, `status`, `createdAt`, `lastUpdated`, `memoryUnitCount`)
        - Verify `CaseStatus` serializes as camelCase string (`"active"`, `"closed"`) — test BOTH values in `List<Case>` round-trip to catch AOT source generator gaps
        - Verify `Description` null omission behavior (use `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` on `Description`)
    - [x] 8.2 Create `tests/Hexalith.Memories.Contracts.Tests/V1/CreateCaseInputSerializationTests.cs`:
        - Test `CreateCaseInput` serialization round-trip
        - Verify optional `Description` handling

- [x] Task 9: Unit tests for CaseValidator (AC: 1)
    - [x] 9.1 Create `tests/Hexalith.Memories.Server.Tests/Cases/CaseValidatorTests.cs`:
        - Test valid input returns `null`
        - Test empty `TenantId` returns error with code and suggestion
        - Test invalid `TenantId` characters returns error
        - Test empty `Name` returns error
        - Test `Name` exceeding 200 chars returns error
        - Test `Description` exceeding 2000 chars returns error
        - Test null `Description` is valid
    - Use Shouldly assertions: `result.ShouldBeNull()`, `result.ShouldNotBeNull()`, `result!.Code.ShouldBe("INVALID_TENANT_ID")`

- [x] Task 10: Unit tests for CaseService (AC: 1, 2, 3)
    - [x] 10.1 Create `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs`:
        - Mock `IConnectionMultiplexer` and `IGraphQueryBuilder` with NSubstitute
        - Test `CreateCaseAsync` stores hash in Redis and creates FalkorDB node
        - Test `ListCasesAsync` returns all cases for tenant with correct memory unit counts
        - Test `GetCaseAsync` returns case when found
        - Test `GetCaseAsync` returns null when not found
    - [x] 10.2 Use NSubstitute for mocking: `var redis = Substitute.For<IConnectionMultiplexer>(); var falkorDb = Substitute.For<IConnectionMultiplexer>();`
    - [x] 10.3 **NSubstitute `IConnectionMultiplexer` setup:** Mocking Redis requires chaining `IDatabase` — set up `redis.GetDatabase().Returns(mockDb)` then mock `mockDb.HashSetAsync(...)`, `mockDb.ExecuteAsync("SCAN", ...)` etc. For FalkorDB, mock `falkorDb.GetDatabase()` similarly. This is the trickiest mock setup in the story — follow existing test patterns if available

- [x] Task 11: Integration test (AC: 1, 2, 3)
    - [x] 11.1 Create `tests/Hexalith.Memories.Server.Tests/Cases/CaseEndpointIntegrationTests.cs`:
        - Test `POST /api/tenants/{tenantId}/cases` returns 201 Created — assert: `Id` is valid ULID, `TenantId` matches route, `Name` matches input, `Status` is `"active"`, `CreatedAt` is recent, `MemoryUnitCount` is 0, `Location` header contains case ID
        - Test `GET /api/tenants/{tenantId}/cases` returns list including created case — assert: list length >= 1, created case present by ID, all fields match POST response
        - Test `GET /api/tenants/{tenantId}/cases/{caseId}` returns 404 for nonexistent case — assert: response body is `ErrorResponse` with code `CASE_NOT_FOUND`
        - Test creating a case, ingesting a memory unit, then GET case — assert: `MemoryUnitCount` is 1, `CONTAINS` edge exists in FalkorDB
    - [x] 11.2 **Boundary coverage** (primary quality gate per test architect review):
        - Test `ListCasesAsync` with **0 cases** — returns empty list, no FalkorDB queries issued
        - Test `ListCasesAsync` with **1 case** — correct memory unit count returned
        - Test `ListCasesAsync` with **10+ cases** — verify N+1 query pattern performs acceptably, all counts correct
        - Test case creation with maximum length `Name` (200 chars) and `Description` (2000 chars)
    - [x] 11.3 **CI compatibility:** Mark integration tests with `[Trait("Category", "Integration")]`. These tests require Redis + FalkorDB running. If Testcontainers is available, use it; otherwise, tests should be skippable in CI via environment variable check (e.g., `Skip = Environment.GetEnvironmentVariable("SKIP_INTEGRATION") != null`)
    - Use existing integration test patterns from Search tests

### Review Findings

- [x] [Review][Patch] Generate real ULIDs for new cases instead of UUIDv7 strings [src/Hexalith.Memories.Server/Cases/CaseService.cs:37]
- [x] [Review][Patch] Sort all candidate cases before applying `maxResults`, otherwise newest cases can be omitted [src/Hexalith.Memories.Server/Cases/CaseService.cs:85]
- [x] [Review][Patch] Add the missing case endpoint/integration coverage (including `ListCasesAsync` and `CaseEndpointIntegrationTests`) [tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs:21]
- [x] [Review][Defer] Case creation is non-atomic across Redis and FalkorDB [src/Hexalith.Memories.Server/Cases/CaseService.cs:43] — deferred, pre-existing

## Error Code Registry

| Code                       | HTTP | Returned By                                                   | Suggestion                                            |
| -------------------------- | ---- | ------------------------------------------------------------- | ----------------------------------------------------- |
| `INVALID_TENANT_ID`        | 400  | Endpoints 4.1, 4.2, 4.3 via `TenantIdGuard` / `CaseValidator` | "Only alphanumeric and hyphens allowed."              |
| `INVALID_CASE_NAME`        | 400  | Endpoint 4.1 via `CaseValidator`                              | "Name is required, max 200 characters."               |
| `INVALID_CASE_DESCRIPTION` | 400  | Endpoint 4.1 via `CaseValidator`                              | "Description must not exceed 2000 characters."        |
| `CASE_NOT_FOUND`           | 404  | Endpoint 4.3                                                  | "Run 'memories case list' to see available cases."    |
| `SINGLE_CASE_OWNERSHIP`    | 400  | `CaseService` (future reassignment attempt)                   | "Delete the unit and re-ingest into the target case." |

## Dev Notes

### Quick Start (Read First)

1. **Implementation order:** Task 1 → 5 → 3 → 2 → 4 → 7 → 6 → 8-11 (NOT numeric order — Task 5 must precede Task 2)
2. **New files:** 3 contracts in `Contracts/V1/`, 2 classes in `Server/Cases/`, 2 methods added to `IGraphQueryBuilder` + `GraphQueryBuilder`, endpoints in `Program.cs`, 4 test files
3. **Critical:** `CaseService` needs BOTH `[FromKeyedServices("redis")]` AND `[FromKeyedServices("falkordb")]` `IConnectionMultiplexer` — graph builder only builds queries, doesn't execute them
4. **Anti-patterns:** No DAPR Actor, no DAPR Workflow, no `KEYS` command, no raw Cypher, no trusted body TenantId, no cached MemoryUnitCount
5. **FalkorDB resilience:** All count queries must catch failures and return 0 — never crash list/get operations. Extract a `GetMemoryUnitCountSafe` private helper to avoid try-catch duplication across `ListCasesAsync` and `GetCaseAsync`

### Architecture Compliance

- **Domain model:** `public sealed record` in `Contracts/V1/` — matches `MemoryUnit.cs`, `SearchResult.cs` patterns
- **Service class:** `internal sealed class` in `Server/Cases/` — matches `SyntacticSearchService`, `SemanticSearchService` patterns
- **Validator:** `internal static class` in `Server/Cases/` — matches `IngestionInputValidator` pattern. **Note:** `CaseValidator` returns `ErrorResponse?` (for API endpoints), while `IngestionInputValidator` throws `ArgumentException` (for workflow activities). Different error handling is deliberate — API callers get structured errors, internal activities use exceptions
- **Endpoints:** Minimal API in `Program.cs` — matches existing `MapPost`/`MapGet` patterns (lines ~124-523)
- **Error responses:** `ErrorResponse(Code, Message, Suggestion)` — matches existing error pattern
- **JSON serialization:** `camelCase` via `MemoriesJsonContext` with AOT source generators
- **Async pattern:** `Task<T>` with `Async` suffix, `CancellationToken` as last parameter on every async method

### Code Style

Follow `.editorconfig` — key rules: file-scoped namespaces, Allman braces, `sealed` on implementation classes, ITANEO copyright header, `_camelCase` private fields, warnings as errors.

### Storage Strategy

- **Case metadata** stored in Redis Hash: key `{tenantId}:case:{caseId}`, fields match `Case` record properties
- **Case graph node** in FalkorDB: `(:Case {id, name, tenantId, createdAt})` in tenant's database
- **`CONTAINS` edge:** `(:Case)-[:CONTAINS {confidence: 1.0, origin: 'explicit'}]->(:MemoryUnit)` — structural edge type already defined in architecture, UPPER_SNAKE_CASE per `GraphQueryBuilder.ToUpperSnakeCase`. Already created by `IndexGraphActivity` during ingestion
- **Memory unit count:** Derived from FalkorDB edge count query, not stored redundantly
- **Tenant isolation:** Case keys are prefixed with `{tenantId}:case:` — physical isolation per tenant

### Critical Anti-Patterns to Avoid

1. **DO NOT** create a DAPR Actor for case state — cases are simple CRUD entities, not stateful singletons. Actors are for per-tenant rate limiters and statistics caches
2. **DO NOT** create a DAPR Workflow for case creation — it's a single-step operation (Redis write + FalkorDB node), not a multi-step orchestration requiring compensation
3. **DO NOT** use `KEYS` command in Redis for listing — use `SCAN` with pattern matching for production safety
4. **DO NOT** construct raw Cypher strings — use `IGraphQueryBuilder` with parameterized queries only (injection prevention)
5. **DO NOT** trust `TenantId` from request body — override with route parameter
6. **DO NOT** store `MemoryUnitCount` in Redis Hash — derive from FalkorDB edge count to avoid stale data
7. **KNOWN N+1:** `ListCasesAsync` issues one FalkorDB count query per case — acceptable for MVP (< 100 cases per tenant). Do NOT pre-optimize with batch queries or caching in this story; document as optimization target for Story 3.4

### Existing Code to Reuse

- `IGraphQueryBuilder` + `GraphQueryBuilder` (`src/Hexalith.Memories.Server/Graph/`) — for building parameterized Cypher queries. **Must add:** new overload `BuildMergeCaseNode(caseId, name, tenantId, createdAt)` — existing `BuildMergeCaseNode(caseId)` stays untouched for `IndexGraphActivity` backward compat. Also add new `BuildCountCaseMemoryUnits(caseId)` method
- `NFalkorDB.FalkorDB` — instantiate from `[FromKeyedServices("falkordb")] IConnectionMultiplexer` via `new NFalkorDB.FalkorDB(falkorDb.GetDatabase())`, then `falkor.QueryAsync(tenantId, query, parameters)` (see `IndexGraphActivity.cs` for pattern)
- `ErrorResponse` (`src/Hexalith.Memories.Contracts/V1/ErrorResponse.cs`) — for error responses
- `MemoriesJsonContext` (`src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`) — register new types here
- `CamelCaseStringEnumConverter<T>` (`src/Hexalith.Memories.Contracts/V1/CamelCaseStringEnumConverter.cs`) — already exists, use for `CaseStatus` enum
- `TenantIdGuard.Validate` — reuse for tenant ID validation (same guard used by `IngestionInputValidator` and `IndexGraphActivity`)
- `[FromKeyedServices("redis")] IConnectionMultiplexer` — existing keyed DI for Redis (registered at `Program.cs` line 50)
- `[FromKeyedServices("falkordb")] IConnectionMultiplexer` — existing keyed DI for FalkorDB (registered at `Program.cs` line 52)

### Package Management

- **Directory.Packages.props** for all NuGet versions — never add version in `.csproj`
- `.slnx` solution format — never create `.sln`
- `ByteAether.Ulid` added via central package management for real ULID generation in `CaseService`

### Known Scope Gaps (Deferred)

- **Human-readable case ID / slug:** PRD Journey 9 shows `memories case create --id claims-pilot` implying user-chosen IDs. This story auto-generates ULIDs. A `slug` field for human-friendly lookup is deferred to Epic 7 (CLI). The API contract (`CreateCaseInput`) can be extended with an optional `string? Slug` without breaking changes
- **Atomicity gap:** `CreateCaseAsync` writes Redis hash then FalkorDB node as two separate operations. If FalkorDB fails after Redis succeeds, a phantom case exists in the list but has no graph node. Recovery: retry `POST` (FalkorDB MERGE is idempotent). Not worth a DAPR Workflow for this — see anti-pattern #2
- **Case name non-uniqueness:** Multiple cases can have the same `Name` within a tenant — this is by design. ULIDs are the unique identifiers, not names. Duplicate names are a valid user scenario (e.g., "Claims Q1", "Claims Q1" for different teams). No uniqueness constraint needed
- **Case existence not enforced at ingestion:** The ingestion workflow does not verify that a case exists (via `CaseService`) before creating the `CONTAINS` edge. `IndexGraphActivity.BuildMergeCaseNode(caseId)` creates a stub node if the case doesn't exist. This means a memory unit can be ingested into a "case" that was never created via the API. The stub node will have no `name`/`tenantId`/`createdAt` properties. Acceptable for MVP — a future story should add case existence validation to the ingestion workflow

### Architecture Decision Records

- **ADR-1: Redis Hash over Redis JSON for case metadata.** Cases are flat records (8 fields, no nesting). Hash is simpler, faster, and doesn't require the RedisJSON module. Revisit if case metadata grows complex
- **ADR-2: Derived MemoryUnitCount over cached counter.** Truth lives in the graph. Derived count avoids stale data from failed ingestions or compensation. N+1 query cost acceptable for MVP (< 100 cases per tenant). Anti-pattern #6 enforces this

### FalkorDB Graph Schema Extension

The `Case` node type is new to the graph. Existing node types: `MemoryUnit`. Existing edge types: `CAUSED_BY`, `CORRELATED_WITH`, `REFERENCES`, `CONTAINS`, `ANNOTATES` (UPPER_SNAKE_CASE per `GraphQueryBuilder.ToUpperSnakeCase`). The `CONTAINS` edge type already exists in the architecture — this story uses it for `Case → MemoryUnit` relationships.

<!-- context-only: Previous story learnings already embedded in task details (AOT in 1.4, JsonIgnore in 8.1, test patterns in 8-11, keyed DI in 2.2) -->

<!-- context-only: Git pattern — sealed records in Contracts/V1/, services in feature subfolder under Server/, endpoints in Program.cs, tests split by project -->

<!-- context-only: All new files specified in task subtasks. No new projects — fits in existing Contracts + Server -->

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 3, Story 3.1]
- [Source: _bmad-output/planning-artifacts/architecture.md — Memory Organization FR26-37, Data Model, Graph Edge Model, Code Structure]
- [Source: _bmad-output/planning-artifacts/prd.md — FR26, FR30, FR32, FR33, Journey 9: "The First Case"]
- [Source: _bmad-output/implementation-artifacts/2-6-explain-mode-and-confidence-scores.md — AOT serialization patterns, testing patterns]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Real ULID generation now uses `ByteAether.Ulid` 1.3.2 directly in `Hexalith.Memories.Server`, with monotonic increment enabled to preserve chronological ordering semantics.
- `Shouldly.Case` naming conflict with `Hexalith.Memories.Contracts.V1.Case` — resolved by using `CaseRecord` type alias in new test files and qualifying `Shouldly.Case.Sensitive` in existing test files.
- `NSubstitute` cannot proxy `ILogger<T>` when `T` is an `internal` type — used `NullLogger<CaseService>.Instance` instead, matching existing test patterns (e.g., `HybridSearchServiceTests`).
- `IServer.ScanKeysAsync` does not exist in StackExchange.Redis — used `IServer.Keys(pattern:)` which uses SCAN internally.

### Completion Notes List

- Task 1: Created `Case.cs`, `CaseStatus.cs`, `CreateCaseInput.cs` as `public sealed record` types in `Contracts/V1/`. Registered all types including `List<Case>` in `MemoriesJsonContext` for AOT. `Description` uses `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`.
- Task 5: Added `BuildMergeCaseNode(caseId, name, tenantId, createdAt)` overload and `BuildCountCaseMemoryUnits(caseId)` to `IGraphQueryBuilder` and `GraphQueryBuilder`. Existing `BuildMergeCaseNode(string caseId)` untouched — zero breaking changes.
- Task 3: Created `CaseValidator` with `ValidateCreateCase()` returning `ErrorResponse?`. Uses `TenantIdGuard.Validate` for tenant ID validation, catches `ArgumentException` and converts to `ErrorResponse`.
- Task 2: Created `CaseService` with `CreateCaseAsync`, `ListCasesAsync`, `GetCaseAsync`, and `GetMemoryUnitCountSafe`. Uses keyed DI for both Redis and FalkorDB. SCAN via `IServer.Keys()` for listing, with race condition guard on empty hash reads.
- Task 4: Added 3 endpoints — `POST /api/tenants/{tenantId}/cases`, `GET /api/tenants/{tenantId}/cases`, `GET /api/tenants/{tenantId}/cases/{caseId}`. Tenant ID trust boundary enforced via route param override.
- Task 7: Registered `CaseService` as scoped in `Program.cs`.
- Task 6: Verified `IndexGraphActivity` CONTAINS edge creation is consistent with CaseService. Both use MERGE on `{id: $caseId}` — idempotent and compatible.
- Tasks 8-11: Created serialization tests for `Case` and `CreateCaseInput`, enum tests for `CaseStatus`, validator tests covering all validation rules and boundary cases, service tests for create/get/not-found. Added `ListCasesAsync` ordering/limit coverage plus `CaseEndpointIntegrationTests` for POST/list/get/ingest-count behavior. Graph query builder tests cover the new methods including injection prevention.

### Change Log

- 2026-04-12: Implemented Story 3.1 — Create and List Cases. All unit and serialization tests pass (534 total, 0 failures). Full solution builds with 0 warnings.
- 2026-04-12: Code review follow-up — switched case IDs to real ULIDs, fixed case list ordering before limit, and added missing case endpoint integration coverage.

### File List

**New files:**

- src/Hexalith.Memories.Contracts/V1/Case.cs
- src/Hexalith.Memories.Contracts/V1/CaseStatus.cs
- src/Hexalith.Memories.Contracts/V1/CreateCaseInput.cs
- src/Hexalith.Memories.Server/Cases/CaseService.cs
- src/Hexalith.Memories.Server/Cases/CaseValidator.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/CaseSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/CreateCaseInputSerializationTests.cs
- tests/Hexalith.Memories.IntegrationTests/Cases/CaseEndpointIntegrationTests.cs
- tests/Hexalith.Memories.Server.Tests/Cases/CaseValidatorTests.cs
- tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs

**Modified files:**

- Directory.Packages.props (added central `ByteAether.Ulid` version for ULID generation)
- src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs (added Case, CaseStatus, CreateCaseInput, List<Case> registrations)
- src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj (added direct `ByteAether.Ulid` package reference)
- src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs (added BuildMergeCaseNode overload + BuildCountCaseMemoryUnits)
- src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs (implemented new interface methods)
- src/Hexalith.Memories.Server/Program.cs (added CaseService DI registration + 3 case endpoints)
- tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs (added tests for new methods)
- tests/Hexalith.Memories.Contracts.Tests/V1/EnumSerializationTests.cs (added CaseStatus tests)
- tests/Hexalith.Memories.Contracts.Tests/V1/ErrorResponseSerializationTests.cs (qualified Shouldly.Case.Sensitive)
- tests/Hexalith.Memories.Contracts.Tests/V1/ScoredResultSerializationTests.cs (qualified Shouldly.Case.Sensitive)
- tests/Hexalith.Memories.Contracts.Tests/V1/SearchQuerySerializationTests.cs (qualified Shouldly.Case.Sensitive)
- tests/Hexalith.Memories.Contracts.Tests/V1/SearchResultSerializationTests.cs (qualified Shouldly.Case.Sensitive)
- tests/Hexalith.Memories.Contracts.Tests/V1/SearchExplanationSerializationTests.cs (qualified Shouldly.Case.Sensitive)
- tests/Hexalith.Memories.Contracts.Tests/V1/HybridSearchResultSerializationTests.cs (qualified Shouldly.Case.Sensitive)
- tests/Hexalith.Memories.Contracts.Tests/V1/FusionWeightsSerializationTests.cs (qualified Shouldly.Case.Sensitive)
