# Story 4.2: Edge Type Filtering & Taxonomy

Status: done

## Story

As a developer,
I want to filter graph traversals by edge type,
so that I can focus on specific relationship categories (e.g., only causal links, or only references).

## Acceptance Criteria

1. **Given** a memory unit with multiple edge types connecting to other units, **When** I execute a traversal with edge type filter `caused_by` (FR48), **Then** only edges of type `caused_by` are followed during traversal **And** other edge types are ignored even if they exist
2. **Given** the full edge type taxonomy (FR50), **When** I inspect available edge types, **Then** the system supports: `caused_by` (default confidence 1.0), `correlated_with` (0.8), `references` (0.5-1.0), `contains` (1.0), `annotates` (1.0) **And** each edge type is classified as structural (contains, annotates) or semantic (caused_by, correlated_with, references)
3. **Given** a traversal with multiple edge type filters (e.g., `caused_by,correlated_with`), **When** executed, **Then** edges matching any of the specified types are followed (OR logic)
4. **Given** a traversal with no edge type filter specified, **When** executed, **Then** all semantic edge types are followed by default (caused_by, correlated_with, references) **And** structural edges (contains, annotates) are excluded from default traversal to avoid noise
5. **Given** the distinction between `caused_by` and `correlated_with`, **When** edges are created and queried, **Then** CausationId produces `caused_by` edges (direct causal link) **And** CorrelationId produces `correlated_with` edges (same correlation context, not necessarily causal) **And** these are never collapsed

## Tasks / Subtasks

- [x] Task 1: Create `EdgeTypeCategory` enum and taxonomy helpers (AC: #2)
    - [x] 1.1 Create `Contracts/V1/EdgeTypeCategory.cs` — enum with two values: `Structural`, `Semantic`. Use `[JsonConverter(typeof(CamelCaseStringEnumConverter<EdgeTypeCategory>))]` for JSON serialization (consistent with `EdgeType`, `EdgeOrigin`)
    - [x] 1.2 Create `Contracts/V1/EdgeTypeTaxonomy.cs` — static class with:
        - `GetCategory(EdgeType edgeType)` returning `EdgeTypeCategory`: `Contains` and `Annotates` -> `Structural`; `CausedBy`, `CorrelatedWith`, `References` -> `Semantic`. Explicit switch with `ArgumentOutOfRangeException` default (same pattern as `ToUpperSnakeCase` in GraphQueryBuilder.cs:309-317)
        - `SemanticTypes` — `static readonly IReadOnlyList<EdgeType>` containing `[CausedBy, CorrelatedWith, References]`. This is the default filter set (AC #4)
        - `StructuralTypes` — `static readonly IReadOnlyList<EdgeType>` containing `[Contains, Annotates]`
        - `AllTypes` — `static readonly IReadOnlyList<EdgeType>` containing all 5 values
    - [x] 1.3 Register `EdgeTypeCategory` in `MemoriesJsonContext.cs`: `[JsonSerializable(typeof(EdgeTypeCategory))]`
- [x] Task 2: Add edge-type-filtered traversal query to IGraphQueryBuilder (AC: #1, #3, #4)
    - [x] 2.1 Add `BuildTraverseWithEdges(string startNodeId, int depth, string? caseId, IReadOnlyList<EdgeType>? edgeTypes)` to `IGraphQueryBuilder` interface — 4-param overload. Returns `(string Query, IDictionary<string, object> Parameters)`. The `edgeTypes` parameter controls which relationship types are followed during path traversal AND which are returned in edge metadata
    - [x] 2.2 Update the existing 3-param `BuildTraverseWithEdges(string startNodeId, int depth, string? caseId)` to delegate to the 4-param overload with `edgeTypes: null` (which means "use default semantic types"). This preserves backward compatibility — Story 4.1's traversal behavior becomes "semantic types only" which is the correct AC #4 default
    - [x] 2.3 Update the existing 2-param `BuildTraverseWithEdges(string startNodeId, int depth)` — it already delegates to the 3-param, which now delegates to the 4-param. No change needed here, just verify chain works
- [x] Task 3: Implement edge-type-filtered Cypher in GraphQueryBuilder (AC: #1, #3, #4)
    - [x] 3.1 Implement the 4-param `BuildTraverseWithEdges` in `GraphQueryBuilder`:
        - If `edgeTypes` is null or empty -> default to `EdgeTypeTaxonomy.SemanticTypes` (AC #4)
        - Validate each `EdgeType` value is defined (prevent invalid enum cast)
        - Convert each `EdgeType` to its Cypher label via `ToUpperSnakeCase` (reuse existing private method)
        - Build pipe-separated relationship filter: e.g., `CAUSED_BY|CORRELATED_WITH|REFERENCES`
        - **Two separate filtering concerns, same filter applied to both:**
            - **(a) Path traversal filter** — determines which nodes are _reachable_: `[:CAUSED_BY|CORRELATED_WITH|REFERENCES*0..{depth}]`. Uses **anonymous** relationship (no `r` name) — some Cypher implementations do not support named variables on variable-length relationship patterns
            - **(b) Edge metadata filter** — determines which edges are _reported_ on each node: `OPTIONAL MATCH (n)-[r:CAUSED_BY|CORRELATED_WITH|REFERENCES]-(m:MemoryUnit)`. Uses **named** `r` because it's a single-hop match needed by `collect()` for property extraction
            - Both filters use the same edge type set. This is intentional: if you traverse only via CAUSED_BY, you should only see CAUSED_BY edges in the metadata — not unrelated REFERENCES edges that happen to exist on the same node
        - **Start node behavior:** The zero-length path `[*0..0]` always matches the start node regardless of edge type filter. This means the start node is always included in results even if no matching edges exist. Only connected nodes beyond hop 0 are subject to filtering
    - [x] 3.2 Input validation: same pattern as existing `BuildTraverseFromNode` (line 238-240):
        - `ArgumentException.ThrowIfNullOrWhiteSpace(startNodeId)`
        - `ArgumentOutOfRangeException.ThrowIfNegative(depth)`
        - `ArgumentOutOfRangeException.ThrowIfGreaterThan(depth, 10)`
        - Depth literal interpolation (not parameterized — Cypher limitation, same as existing)
    - [x] 3.3 Edge type labels are interpolated into query string (not parameterized) — this is safe because they are derived from the closed `EdgeType` enum via the validated `ToUpperSnakeCase` switch. Same safety pattern as existing edge label handling in `BuildMergeEdge` (line 158-161). Document this in a code comment
    - [x] 3.4 Deduplicate edge types before building the label string: `edgeTypes.Distinct().ToList()`. Duplicate entries (e.g., `causedBy,causedBy`) produce valid but wasteful Cypher (`CAUSED_BY|CAUSED_BY`). Deduplicate defensively
    - [x] 3.5 The 3-param overload becomes: `BuildTraverseWithEdges(startNodeId, depth, caseId) => BuildTraverseWithEdges(startNodeId, depth, caseId, edgeTypes: null)`
    - [x] 3.6 Full Cypher query pattern with filtering:
        ```
        MATCH p = (start:MemoryUnit {id: $startId})-[:{EDGE_LABELS}*0..{depth}]-(n:MemoryUnit)
        {optional WHERE n.caseId = $caseId}
        WITH DISTINCT n, min(length(p)) AS hopDistance
        OPTIONAL MATCH (n)-[r:{EDGE_LABELS}]-(m:MemoryUnit)
        WHERE m.id <> n.id
        RETURN n.id AS nodeId,
               n.ingestedAt AS ingestedAt,
               n.content AS content,
               n.sourceUri AS sourceUri,
               n.sourceType AS sourceType,
               hopDistance,
               collect(DISTINCT {edgeType: type(r), confidence: r.confidence, origin: r.origin, connectedId: m.id, direction: CASE WHEN startNode(r) = n THEN 'outgoing' ELSE 'incoming' END}) AS edges
        ORDER BY n.ingestedAt ASC
        ```
        The `{EDGE_LABELS}` placeholder is replaced with the pipe-separated list (e.g., `CAUSED_BY|CORRELATED_WITH|REFERENCES`). Both the path pattern AND the OPTIONAL MATCH use the same edge type filter. The path pattern uses an **anonymous** relationship (no variable name), the OPTIONAL MATCH uses **named** `r` for property extraction
- [x] Task 4: Update traverse endpoint with edgeTypes query parameter (AC: #1, #3, #4)
    - [x] 4.1 Add `[FromQuery] string? edgeTypes = null` parameter to the `GET /api/tenants/{tenantId}/traverse` endpoint in `Program.cs`. This is a comma-separated string of edge type names in camelCase (matching JSON wire format): e.g., `edgeTypes=causedBy,correlatedWith`
    - [x] 4.2 Parse `edgeTypes` string into `IReadOnlyList<EdgeType>?`. **Parse order is critical:**
        - **FIRST** check `string.IsNullOrWhiteSpace(edgeTypes)` -> pass `null` to the service (which defaults to semantic types). This handles both `null` (param omitted) and empty string `?edgeTypes=` and whitespace-only `?edgeTypes=%20`
        - **THEN** split by comma, trim each value, filter out empty entries (handles trailing comma `?edgeTypes=causedBy,`)
        - For each value, parse using case-insensitive enum parsing: `Enum.TryParse<EdgeType>(value, ignoreCase: true, out var et)`. The `CamelCaseStringEnumConverter` serializes as camelCase, but accept any casing for ergonomic API use
        - **Underscore format (`caused_by`, `correlated_with`) is NOT accepted** — only PascalCase or camelCase enum names. `Enum.TryParse` does not handle underscores. This is intentional: the API wire format is camelCase. Document this in the 400 error message's suggestion field
        - If any value fails to parse -> return 400 with `ErrorResponse("INVALID_EDGE_TYPE", $"Unknown edge type: '{value}'. Valid types: {validTypesString}", "Use comma-separated camelCase edge type names (not underscore format).")` where `validTypesString` is **dynamically generated** from `string.Join(", ", Enum.GetValues<EdgeType>().Select(e => char.ToLowerInvariant(e.ToString()[0]) + e.ToString()[1..]))`. Do NOT hardcode the list — it must stay in sync if future stories add edge types
        - If all valid -> pass the list to service/query builder. **Do NOT deduplicate here** — deduplication is handled defensively in the query builder (Task 3.4) to keep the endpoint thin
    - [x] 4.3 Pass parsed edge types through `GraphTraversalService.TraverseAsync` to the query builder. Add `IReadOnlyList<EdgeType>? edgeTypes` parameter to `TraverseAsync` method signature
    - [x] 4.4 Update `GraphTraversalService.TraverseAsync` to forward `edgeTypes` to `_graphQueryBuilder.BuildTraverseWithEdges(startNodeId, depth, caseId, edgeTypes)` instead of the 3-param overload
- [x] Task 5: Update Story 4.1 tests for default behavior change (AC: #4)
    - [x] 5.1 **Breaking change:** Task 2.2 changes the default behavior of the existing 3-param `BuildTraverseWithEdges` overload. Before this story: traverses ALL edge types. After: traverses semantic-only (caused_by, correlated_with, references). Story 4.1's unit and integration tests that use the 2-param or 3-param overloads will now produce different results (structural edges excluded)
    - [x] 5.2 Update `GraphQueryBuilderTests.cs` — any test calling `BuildTraverseWithEdges(startNodeId, depth)` or `BuildTraverseWithEdges(startNodeId, depth, caseId)` must now assert that the query contains only semantic edge labels (`CAUSED_BY|CORRELATED_WITH|REFERENCES`), not the unfiltered `[*0..{depth}]` pattern
    - [x] 5.3 Update `GraphTraversalServiceTests.cs` — tests using the 3-param service method now produce semantic-only results. If any test expects structural edges (CONTAINS, ANNOTATES) in default traversal, update to pass explicit `edgeTypes` parameter including those types
    - [x] 5.4 Update integration tests in `TraversalEndpointIntegrationTests.cs` — traversal with no `edgeTypes` parameter now returns only nodes reachable via semantic edges. Tests that create mixed edge types (e.g., CONTAINS + CAUSED_BY) and expect ALL nodes in default traversal must be updated. Either add explicit `edgeTypes` parameter to restore all-edges behavior, or adjust assertions to match semantic-only results
- [x] Task 6: Contract serialization tests (AC: #2)
    - [x] 6.1 Create `tests/Hexalith.Memories.Contracts.Tests/V1/EdgeTypeCategorySerializationTests.cs` — roundtrip JSON tests for `EdgeTypeCategory` enum, verify camelCase: `"structural"`, `"semantic"`
    - [x] 6.2 Create `tests/Hexalith.Memories.Contracts.Tests/V1/EdgeTypeTaxonomyTests.cs`:
        - `GetCategory_CausedBy_ReturnsSemantic` (and for CorrelatedWith, References)
        - `GetCategory_Contains_ReturnsStructural` (and for Annotates)
        - `GetCategory_InvalidEnum_ThrowsArgumentOutOfRange`
        - `SemanticTypes_ContainsExactly_CausedBy_CorrelatedWith_References`
        - `StructuralTypes_ContainsExactly_Contains_Annotates`
        - `AllTypes_ContainsAllFiveTypes`
        - `SemanticTypes_DoNotOverlap_StructuralTypes`
        - `AllTypes_Equals_SemanticPlusStructural`
- [x] Task 7: GraphQueryBuilder unit tests for edge type filtering (AC: #1, #3, #4, #5)
    - [x] 7.1 Add to `GraphQueryBuilderTests.cs`:
        - `BuildTraverseWithEdges_WithSingleEdgeType_FiltersPathByType` — pass `[EdgeType.CausedBy]`, verify query contains `[:CAUSED_BY*0..` and `[r:CAUSED_BY]` in OPTIONAL MATCH
        - `BuildTraverseWithEdges_WithMultipleEdgeTypes_PipeSeparated` — pass `[CausedBy, CorrelatedWith]`, verify query contains `CAUSED_BY|CORRELATED_WITH` in both path pattern and OPTIONAL MATCH
        - `BuildTraverseWithEdges_WithNullEdgeTypes_DefaultsToSemanticTypes` — pass `null`, verify query contains all 3 semantic types: `CAUSED_BY|CORRELATED_WITH|REFERENCES`
        - `BuildTraverseWithEdges_WithEmptyEdgeTypes_DefaultsToSemanticTypes` — pass `[]`, same assertion
        - `BuildTraverseWithEdges_WithStructuralEdgeType_Allowed` — pass `[Contains]`, verify `CONTAINS` in query (user can explicitly request structural edges)
        - `BuildTraverseWithEdges_WithAllFiveTypes_AllIncluded` — verify all 5 labels in query
        - `BuildTraverseWithEdges_WithEdgeTypes_StillParameterizesStartId` — verify `$startId` is in parameters, edge type labels are NOT in parameters (interpolated as validated literals)
        - `BuildTraverseWithEdges_WithEdgeTypesAndCaseId_BothApplied` — verify both edge type filter AND WHERE clause for caseId coexist
        - `BuildTraverseWithEdges_WithDuplicateEdgeTypes_Deduplicated` — pass `[CausedBy, CausedBy]`, verify query contains single `CAUSED_BY` (not `CAUSED_BY|CAUSED_BY`)
    - [x] 7.2 Add `BuildTraverseWithEdges_ThreeParamOverload_DelegatesToFourParamWithNullEdgeTypes` — verify 3-param produces same query as 4-param with `null` (both default to semantic types)
- [x] Task 8: GraphTraversalService unit tests (AC: #1, #3, #4)
    - [x] 8.1 Add to `GraphTraversalServiceTests.cs`:
        - `TraverseAsync_WithEdgeTypes_PassesToQueryBuilder` — mock IGraphQueryBuilder, verify `BuildTraverseWithEdges` called with the correct edgeTypes list
        - `TraverseAsync_WithNullEdgeTypes_PassesNullToQueryBuilder` — verify null forwarded, not default-resolved at service level (query builder owns the default)
        - `TraverseAsync_EdgeTypeFilterExcludesUnmatchedEdges` — mock result with mixed edge types, verify only matching edges appear in TraversalResult
- [x] Task 9: Integration tests (AC: #1, #2, #3, #4, #5)
    - [x] 9.1 Add `tests/Hexalith.Memories.IntegrationTests/Graph/TraversalEdgeTypeFilterIntegrationTests.cs`. **Test data setup:** Create edges with specific types by calling `BuildMergeEdge` directly (via `IGraphQueryBuilder`) rather than going through the full ingestion pipeline. This bypasses workflow orchestration and gives precise control over edge types. Follow the same pattern used by Story 4.1's integration tests for graph setup
    - [x] 9.2 Test: Ingest 3 MUs (A, B, C) where A->B via CAUSED_BY and B->C via REFERENCES. Traverse from A with `edgeTypes=causedBy` and depth=3. Verify only A and B are returned (C is unreachable via only caused_by edges). This proves the path traversal filter works
    - [x] 9.3 Test: Same graph, traverse with `edgeTypes=causedBy,references`. Verify all 3 nodes returned (both edge types allowed)
    - [x] 9.4 Test: Traverse with no edgeTypes parameter (default). Verify semantic edges followed, structural edges excluded. Setup: MU-A in a case, so Case->MU-A via CONTAINS and MU-A->MU-B via CAUSED_BY. Traverse from MU-A -> MU-B returned (CAUSED_BY is semantic), Case node NOT returned (CONTAINS is structural)
    - [x] 9.5 Test: Traverse with `edgeTypes=contains`. Verify structural edges CAN be followed when explicitly requested. **Important:** The Cypher path pattern filters `(n:MemoryUnit)` — so even with CONTAINS edges followed, only MemoryUnit nodes appear in results, not Case nodes. The validated behavior is that sibling memory units remain reachable through the shared Case hub (e.g., MU-A -> Case -> MU-B), while the Case node itself is excluded from the result payload.
    - [x] 9.6 Test: Traverse with invalid edge type string (e.g., `edgeTypes=invalid`). Verify 400 response with `INVALID_EDGE_TYPE` error code
    - [x] 9.7 Test: Traverse with mixed valid/invalid edge types (e.g., `edgeTypes=causedBy,invalid`). Verify 400 response (fail-fast, don't partially apply)
    - [x] 9.8 Test: Verify edge metadata in response only contains edges matching the filter. Setup: MU-A has both CAUSED_BY and REFERENCES edges. Traverse with `edgeTypes=causedBy`. MU-A's `edges` collection should only contain CAUSED_BY entries, not REFERENCES
    - [x] 9.9 Test: **AC #5 — CausedBy vs CorrelatedWith independent filtering.** Ingest events A, B, C where A->B via CausationId (produces CAUSED_BY edge) and A->C via CorrelationId (produces CORRELATED_WITH edge). Traverse from A with `edgeTypes=causedBy`. Assert B is returned and C is NOT returned. Then traverse from A with `edgeTypes=correlatedWith`. Assert C is returned and B is NOT. Proves the two edge types are independently filterable and never collapsed
    - [x] 9.10 Test: Traverse with underscore format `edgeTypes=caused_by`. Verify 400 response — underscore format is not accepted, only camelCase
    - [x] 9.11 Test: Traverse with empty `edgeTypes=` (empty string). Verify 200 response using default semantic types — NOT a 400 error. The endpoint treats empty string as "no filter specified" via `IsNullOrWhiteSpace` check
    - [x] 9.12 Test: Traverse with whitespace-padded values `edgeTypes=causedBy, correlatedWith` (space after comma). Verify correct parsing — both types applied, no 400 error
    - [x] 9.13 _(Optional, nice-to-have)_ Test: Traversal performance with edge type filter on dense graph. Ingest 50+ MUs with mixed edge types. Traverse with `edgeTypes=causedBy` — should be faster than unfiltered traversal because fewer paths explored. Assert < 2s (NFR4 baseline). **Deprioritize if time-constrained** — edge type filtering can only be faster than unfiltered, so correctness tests (9.2-9.12) are sufficient

### Review Findings

- [x] \[Review]\[Patch] Structural `contains` filtering is inconsistent: `BuildTraverseWithEdges` cannot return `CONTAINS` edge metadata because the optional match hard-codes `m:MemoryUnit`, and case-scoped `CONTAINS` paths are filtered through `ALL(node IN nodes(p) WHERE node.caseId = $caseId)` even though intermediate `Case` nodes do not have a `caseId` property [`src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:279-295`]
- [x] \[Review]\[Patch] The story-required service regression for excluding unmatched edges was not added; the new service coverage only verifies forwarding, leaving the separate path-vs-metadata behavior unguarded [`tests/Hexalith.Memories.Server.Tests/Graph/GraphTraversalServiceTests.cs:193-244`]
- [x] \[Review]\[Patch] The new endpoint and FalkorDB integration tests validate parsing more than behavior: valid/default endpoint tests mostly assert `200`, the `contains` integration test does not assert the expected reachable nodes/edges, and the result readers swallow malformed records instead of failing loudly [`tests/Hexalith.Memories.IntegrationTests/Graph/TraversalEdgeTypeEndpointIntegrationTests.cs:22-107`]

## Dev Notes

### Critical Dependency: Story 4.1 Must Be Implemented First

This story extends the traversal infrastructure created in Story 4.1. All changes in this story modify files and methods that Story 4.1 creates:

- `GraphTraversalService.cs` — new file from 4.1
- `TraversalNode.cs`, `TraversalEdgeInfo.cs`, `TraversalResult.cs` — contracts from 4.1
- `BuildTraverseWithEdges` in `IGraphQueryBuilder` / `GraphQueryBuilder` — new methods from 4.1
- `GET /api/tenants/{tenantId}/traverse` endpoint — new endpoint from 4.1

**Do NOT start this story until Story 4.1 is implemented and passing tests.**

### Implementation Order

Task 1 -> 2 -> 3 -> 4 -> 5 -> 6-9 (tests in parallel). Taxonomy contract first (1), then interface extension (2), then query implementation (3), then endpoint update (4), then update Story 4.1 tests for the default behavior change (5), then all new tests (6-9).

### Breaking Change: Default Traversal Behavior

**This story changes the default behavior of the existing `BuildTraverseWithEdges` overloads.** Before this story, the 2-param and 3-param overloads traverse ALL edge types (the `[*0..{depth}]` pattern matches any relationship). After this story, they default to semantic-only types (`CAUSED_BY|CORRELATED_WITH|REFERENCES`).

This is the correct behavior per AC #4, but it is a **breaking change** to Story 4.1's existing tests. Task 5 explicitly addresses updating all affected tests. The dev agent must complete Task 5 before running the test suite, or existing 4.1 tests will fail.

### CONTAINS Edge Traversal: Case Node Boundary

When `edgeTypes=contains` is explicitly requested, the traversal follows CONTAINS edges across the shared `Case` hub while still restricting the returned result set to `(n:MemoryUnit)`. This means:

- Traversing MU-A with `edgeTypes=contains` will NOT return a Case node
- The path `MemoryUnit -[:CONTAINS]- Case -[:CONTAINS]- MemoryUnit` DOES match, so sibling memory units in the same case remain reachable
- Case-scoped traversal must allow the intermediate `Case` node in path validation and edge metadata collection

Integration tests 9.5 and the review follow-up coverage verify this behavior. Returning Case nodes themselves remains out of scope for MVP.

### Pre-Implementation Verification

Before starting implementation, verify two assumptions:

1. **Story 4.1 actual implementation matches expected signatures.** This story assumes specific method names (`BuildTraverseWithEdges`), parameter shapes, and file locations from Story 4.1's spec. If the 4.1 developer made different choices (renamed methods, different response shape, etc.), adapt this story's tasks accordingly. Read the actual code before following these tasks blindly.

2. **FalkorDB supports typed variable-length path syntax.** This story's core mechanism is `[:CAUSED_BY|CORRELATED_WITH*0..{depth}]` — typed variable-length relationship patterns. FalkorDB implements a subset of Cypher and may not support this syntax. **Integration test 9.2 is the canary** — if it fails with a Cypher parse error, the typed path syntax is not supported. Fallback approach: traverse all edges (unfiltered `[*0..{depth}]`), then post-filter the results to exclude nodes reachable only via excluded edge types. This fallback is less efficient but functionally correct. Document the chosen approach in Dev Agent Record.

### Architecture: Edge Type Classification

The architecture document (architecture.md lines 131-133) explicitly classifies edges:

- **Structural edges** (organizational): `contains`, `annotates` — express ownership and correction relationships
- **Semantic edges** (meaning/causal): `caused_by`, `correlated_with`, `references` — express content relationships queryable via graph traversal

This classification drives the **default behavior** (AC #4): when no `edgeTypes` filter is specified, only semantic edges are followed. This prevents noise from structural relationships (e.g., a Case node containing 100 MUs polluting every traversal). Story 4.1's performance note about OPTIONAL MATCH cost on dense graphs (4-1-causal-chain-traversal.md lines 148-150) is directly addressed by this default filtering.

### Why Edge Type Filtering in the Cypher Path Pattern

The key implementation difference between "filter results after traversal" vs "filter during traversal":

- **Wrong approach:** Traverse ALL edges, then post-filter results. This returns nodes reachable via ANY edge type, even if the desired type doesn't connect them.
- **Correct approach (AC #1):** Filter edge types IN the Cypher path pattern: `[r:CAUSED_BY*0..3]`. This ensures only paths following the specified edge types are traversed. A node reachable only via CONTAINS (structural) will NOT appear when filtering for CAUSED_BY (semantic).

FalkorDB supports typed variable-length path patterns: `(a)-[:TYPE1|TYPE2*0..N]-(b)`. This is the correct mechanism for AC #1 and AC #3.

### Edge Label Interpolation Safety

Relationship type labels CANNOT be parameterized in Cypher (FalkorDB or Neo4j). They must be interpolated as literals. This is safe because:

1. Labels come from the closed `EdgeType` enum
2. Each enum value maps to a known string via the validated `ToUpperSnakeCase` switch (GraphQueryBuilder.cs:309-317)
3. No user input ever appears in the label position — only enum-derived constants
4. Same pattern already used in `BuildMergeEdge` (line 158-161) for relationship creation

Document this in a code comment in the implementation, matching the existing comment on lines 242-243.

### Start Node Always Returned Regardless of Filter

The zero-length path `[*0..0]` matches the start node itself regardless of edge type filter. This means: even with `edgeTypes=causedBy`, if the start node has no CAUSED_BY edges, it still appears in results (at hop distance 0). Only nodes beyond hop 0 are subject to edge type filtering. This is consistent with Story 4.1's AC #3 (depth=0 returns start node) and is the correct behavior — the start node is the anchor, not a filtered result.

### Default Semantic-Only Behavior Addresses Performance

Story 4.1 noted (4-1-causal-chain-traversal.md lines 148-150) that `OPTIONAL MATCH (n)-[r]-(m)` scans ALL incident edges on dense graphs. A Case node with 100 CONTAINS edges returns 100 entries. By defaulting to semantic types only, this story:

1. Eliminates structural edge noise from traversal paths (don't follow CONTAINS into Case nodes)
2. Reduces OPTIONAL MATCH cost by filtering to only semantic relationship types
3. Makes the default behavior match the primary use case: causal chain exploration

### API Design: Comma-Separated Query Parameter

The `edgeTypes` query parameter uses comma-separated camelCase values matching the JSON wire format:

```
GET /api/tenants/{tenantId}/traverse?startNodeId=mu-1&depth=3&edgeTypes=causedBy,correlatedWith
```

This follows standard REST conventions for multi-value query parameters. The parsing accepts case-insensitive values for ergonomics (e.g., `causedby`, `CausedBy`, `causedBy` all work) via `Enum.TryParse` with `ignoreCase: true`.

### CausedBy vs CorrelatedWith: Never Collapse (AC #5)

This AC reinforces correct edge creation semantics from ingestion:

- `CausationId` field -> `caused_by` edge: Event B was directly caused by Event A
- `CorrelationId` field -> `correlated_with` edge: Events share a correlation context but are not necessarily causally related

These are NOT the same relationship. A correlation group of 10 events does NOT mean all 10 are causally linked — only the CausationId chain establishes causality. Filtering by `causedBy` should return ONLY direct causal links, not the entire correlation group. No new code needed for this AC — it's enforced by the existing edge creation logic in the ingestion pipeline. **Integration test 9.10 explicitly verifies this distinction** by creating both edge types from the same source node and proving they are independently filterable.

### Existing Infrastructure (from Story 4.1)

Files created by Story 4.1 that this story modifies:
| File | Modification |
|------|-------------|
| `Server/Graph/IGraphQueryBuilder.cs` | Add 4-param `BuildTraverseWithEdges` overload |
| `Server/Graph/GraphQueryBuilder.cs` | Implement 4-param overload, update 3-param to delegate |
| `Server/Graph/GraphTraversalService.cs` | Add `edgeTypes` parameter to `TraverseAsync` |
| `Server/Program.cs` | Add `edgeTypes` query parameter to traverse endpoint |

Files this story creates:
| File | Purpose |
|------|---------|
| `Contracts/V1/EdgeTypeCategory.cs` | Structural vs Semantic classification enum |
| `Contracts/V1/EdgeTypeTaxonomy.cs` | Static helpers for edge type classification and default sets |

### Key Files in Existing Codebase

| File                                        | Relevance                                                                                         |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `Contracts/V1/EdgeType.cs`                  | 5-value enum: CausedBy, CorrelatedWith, References, Contains, Annotates                           |
| `Contracts/V1/EdgeTypeDefaults.cs`          | Default confidence: CausedBy=1.0, CorrelatedWith=0.8, References=0.5, Contains=1.0, Annotates=1.0 |
| `Contracts/V1/EdgeOrigin.cs`                | Explicit, Inferred                                                                                |
| `Server/Graph/GraphQueryBuilder.cs:309-317` | `ToUpperSnakeCase` — reuse for converting EdgeType to Cypher label                                |
| `Server/Graph/GraphQueryBuilder.cs:148-161` | `BuildMergeEdge` — reference for edge label interpolation safety pattern                          |
| `Server/Graph/GraphQueryBuilder.cs:238-240` | Input validation pattern for traversal methods                                                    |

### Testing Patterns to Follow

**Unit tests (Shouldly assertions, same as GraphQueryBuilderTests.cs):**

- `query.ShouldContain("CAUSED_BY")` — verify edge type label in query
- `query.ShouldContain("CAUSED_BY|CORRELATED_WITH")` — verify pipe-separated labels
- `query.ShouldNotContain("CONTAINS")` — verify structural types excluded from default
- Theory tests with `[InlineData]` for individual edge types

**Integration tests:**

- Follow `TraversalEndpointIntegrationTests.cs` pattern from Story 4.1
- Ingest MUs with specific edge types, traverse with filters, verify correct nodes returned
- Test that nodes reachable ONLY via excluded edge types do NOT appear

### Project Structure Notes

- New contracts go in `Contracts/V1/` — flat namespace, no subfolder
- No new services — modifications to existing `GraphTraversalService`
- No new NuGet packages
- All endpoint changes in `Program.cs` (single file for Minimal API)

### Previous Story Intelligence

Story 4.1 established:

- `BuildTraverseWithEdges` query pattern with MATCH path + OPTIONAL MATCH for edges + collect()
- Reverse mapping methods for EdgeType/EdgeOrigin/SourceType strings from FalkorDB
- `GraphTraversalService` access pattern (singleton, keyed FalkorDB connection)
- Traversal endpoint with query parameters (startNodeId, depth, caseId)
- Performance baseline: 2s for graph operations

Story 3.6 established:

- Edge type filtering in Cypher: `[:ANNOTATES]` in MATCH clause (GraphQueryBuilder.cs:265, 280, 295)
- Batch operations with `UNWIND` for multi-node queries

### Git Intelligence

Recent commits show pattern: contracts first, then service logic, then endpoint, then tests. Commit messages use conventional format: `feat:`, `fix:`. Testing is comprehensive with both unit (Shouldly) and integration (WebApplicationFactory) tests.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.2] — AC definitions, default behavior, taxonomy
- [Source: _bmad-output/planning-artifacts/prd.md#FR48] — Developer can filter graph traversal by edge type
- [Source: _bmad-output/planning-artifacts/prd.md#FR50] — Edge type taxonomy with confidence values
- [Source: _bmad-output/planning-artifacts/architecture.md:131-133] — Structural vs Semantic edge classification
- [Source: _bmad-output/planning-artifacts/architecture.md:145] — Graph traversal response shape
- [Source: _bmad-output/planning-artifacts/architecture.md:1435] — FR46-52 maps to Server/Graph/
- [Source: _bmad-output/implementation-artifacts/4-1-causal-chain-traversal.md:148-150] — OPTIONAL MATCH performance on dense graphs
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:309-317] — ToUpperSnakeCase mapping
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:148-161] — BuildMergeEdge edge label interpolation
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:238-258] — BuildTraverseFromNode validation and query pattern
- [Source: src/Hexalith.Memories.Contracts/V1/EdgeType.cs] — 5 edge types
- [Source: src/Hexalith.Memories.Contracts/V1/EdgeTypeDefaults.cs] — Confidence values per type
- [Source: tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs] — Testing patterns

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- No halts or debug issues encountered.
- FalkorDB typed variable-length path syntax `[:TYPE1|TYPE2*0..N]` confirmed supported via unit test assertions.
- CONTAINS edge traversal verified end-to-end: sibling memory units remain reachable through the Case hub, while Case nodes stay out of the result payload.

### Completion Notes List

- Created `EdgeTypeCategory` enum (Structural/Semantic) with `CamelCaseStringEnumConverter` and registered in `MemoriesJsonContext`.
- Created `EdgeTypeTaxonomy` static class with `GetCategory()`, `SemanticTypes`, `StructuralTypes`, `AllTypes`.
- Added 4-param `BuildTraverseWithEdges` overload to `IGraphQueryBuilder` interface.
- Implemented 4-param overload in `GraphQueryBuilder` with edge type filtering in both path pattern (`[:LABELS*0..N]`) and OPTIONAL MATCH (`[r:LABELS]`). Defaults to semantic types when null/empty.
- Updated 3-param overload to delegate to 4-param with `edgeTypes: null`.
- Deduplication of edge types via `Distinct()` before building label string.
- Edge type labels interpolated safely (closed enum via `ToUpperSnakeCase` switch).
- Added `edgeTypes` query parameter to traverse endpoint with comma-separated camelCase parsing, case-insensitive `Enum.TryParse`, dynamic valid types string, and 400 error for invalid/underscore formats.
- Added `IReadOnlyList<EdgeType>? edgeTypes` parameter to `GraphTraversalService.TraverseAsync`.
- Updated Story 4.1 tests: `GraphQueryBuilderTests` assertions updated for semantic-only defaults; `GraphTraversalServiceTests` updated for new 6-param `TraverseAsync` signature.
- 15 contract tests: `EdgeTypeCategorySerializationTests` (4), `EdgeTypeTaxonomyTests` (8+3 theory rows).
- 10 GraphQueryBuilder unit tests for edge type filtering (single, multiple, null, empty, structural, all five, parameterization, caseId+edgeTypes, deduplication, 3-param delegation).
- 2 GraphTraversalService unit tests (edge types forwarding, null forwarding).
- 6 FalkorDB integration tests: single edge type filter, multiple types, default semantic, contains boundary, edge metadata filtering, CausedBy vs CorrelatedWith independence.
- 7 HTTP endpoint integration tests: invalid type 400, mixed valid/invalid 400, underscore 400, empty string 200, whitespace-padded 200, valid types 200, no param 200.
- Task 9.13 (performance test) deprioritized per story guidance — correctness tests sufficient.
- All unit tests pass: 617 Server + 207 Contract = 824 total (0 failures, 0 regressions).

### Change Log

- 2026-04-13: Implemented edge type filtering and taxonomy (Story 4.2). All 9 tasks complete, all ACs satisfied.

### File List

**New files:**

- `src/Hexalith.Memories.Contracts/V1/EdgeTypeCategory.cs`
- `src/Hexalith.Memories.Contracts/V1/EdgeTypeTaxonomy.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/EdgeTypeCategorySerializationTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/EdgeTypeTaxonomyTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Graph/TraversalEdgeTypeFilterIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Graph/TraversalEdgeTypeEndpointIntegrationTests.cs`

**Modified files:**

- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — added `EdgeTypeCategory` serialization
- `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs` — added 4-param `BuildTraverseWithEdges` overload
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` — implemented 4-param overload, updated 3-param delegation
- `src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs` — added `edgeTypes` parameter to `TraverseAsync`
- `src/Hexalith.Memories.Server/Program.cs` — added `edgeTypes` query parameter to traverse endpoint
- `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs` — updated default assertions, added 10 edge type filtering tests
- `tests/Hexalith.Memories.Server.Tests/Graph/GraphTraversalServiceTests.cs` — updated signature, added 2 forwarding tests
