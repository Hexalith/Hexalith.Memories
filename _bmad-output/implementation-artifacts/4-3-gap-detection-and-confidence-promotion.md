# Story 4.3: Gap Detection & Confidence Promotion

Status: done

## Story

As a developer,
I want to see gap markers when intermediate nodes in a causal chain are missing, and promote AI-inferred edge confidence when I verify a relationship,
so that I can trust the completeness of causal chains and contribute human verification to improve data quality.

## Acceptance Criteria

1. **Given** a causal chain where A's CausationId points to B, and B's points to C, but B is not indexed, **When** traversal is executed from A, **Then** the chain includes a gap marker with B's identifier (FR49) **And** the missing node identifier is included so the gap is traceable **And** the system never silently skips missing nodes
2. **Given** a causal chain with multiple gaps, **When** traversal is executed, **Then** all gaps are flagged individually with their specific missing node identifiers **And** the chain structure remains intact around the gaps
3. **Given** an edge with AI-inferred confidence (e.g., references edge at 0.5), **When** a developer promotes the confidence to 1.0 (FR51), **Then** the edge confidence is updated to the promoted value **And** the edge origin remains unchanged (still `inferred`) but a new field `verifiedBy` records the promoting identity **And** the system never auto-promotes — only explicit human action changes confidence
4. **Given** an edge with explicit origin (e.g., caused_by from CausationId), **When** a developer attempts to change the confidence, **Then** the operation succeeds (human override is allowed) **And** the original confidence is preserved in an audit field (`previousConfidence`) for traceability
5. **Given** late-arriving events that fill a previously detected gap, **When** the missing node is ingested, **Then** the gap marker is retroactively resolved **And** the causal chain becomes complete without manual intervention

## Tasks / Subtasks

- [x] Task 1: Create gap marker contract and extend TraversalResult (AC: #1, #2)
    - [x] 1.1 Create `Contracts/V1/TraversalGapMarker.cs` — sealed record with `MissingNodeId` (string), `HopDistance` (int), `Edges` (IReadOnlyList\<TraversalEdgeInfo\>). Same edge metadata shape as TraversalNode, showing which edges reference the missing stub. This represents a node found during traversal that has no content — a stub created by `BuildMergeStubNode` during a prior ingestion where CausationId or CorrelationId pointed to a not-yet-ingested memory unit
    - [x] 1.2 Modify `Contracts/V1/TraversalResult.cs` — add a non-positional init property: `public IReadOnlyList<TraversalGapMarker> GapMarkers { get; init; } = [];`. This is non-breaking: existing `new TraversalResult(startId, depth, nodes, count)` still works, and callers that care about gaps use `new TraversalResult(...) { GapMarkers = gaps }`. Do NOT change the positional constructor signature — that would break Story 4.1's code and tests
    - [x] 1.3 Register new types in `MemoriesJsonContext.cs`: `[JsonSerializable(typeof(TraversalGapMarker))]`, `[JsonSerializable(typeof(IReadOnlyList<TraversalGapMarker>))]`

- [x] Task 2: Create confidence promotion contracts (AC: #3, #4)
    - [x] 2.1 Create `Contracts/V1/ConfidencePromotionRequest.cs` — sealed record with `SourceNodeId` (string), `TargetNodeId` (string), `EdgeType` (EdgeType), `NewConfidence` (float), `VerifiedBy` (string). `SourceNodeId` and `TargetNodeId` identify the directed edge: (source)-[r:TYPE]->(target). `VerifiedBy` is a free-form identity string (e.g., user ID, email) — no format validation beyond non-empty
    - [x] 2.2 Create `Contracts/V1/ConfidencePromotionResult.cs` — sealed record with `SourceNodeId` (string), `TargetNodeId` (string), `EdgeType` (EdgeType), `PreviousConfidence` (float), `NewConfidence` (float), `VerifiedBy` (string). `PreviousConfidence` is the value before promotion, stored on the edge for audit traceability (AC #4)
    - [x] 2.3 Register in `MemoriesJsonContext.cs`: `[JsonSerializable(typeof(ConfidencePromotionRequest))]`, `[JsonSerializable(typeof(ConfidencePromotionResult))]`

- [x] Task 3: Extend TraversalEdgeInfo with audit fields (AC: #3, #4)
    - [x] 3.1 Add non-positional init properties to `Contracts/V1/TraversalEdgeInfo.cs`: `public string? VerifiedBy { get; init; }` and `public float? PreviousConfidence { get; init; }`. Non-breaking — existing positional construction still works, and the fields are null by default (JSON: omitted when null via `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`)
    - [x] 3.2 Add `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` on both new properties to keep traversal responses clean when edges are not promoted. **Import needed:** `using System.Text.Json.Serialization;` (check if already present in file)
    - [x] 3.3 Update the edge collection map in the Cypher query of `BuildTraverseWithEdges` (GraphQueryBuilder.cs:275) — add `verifiedBy: r.verifiedBy, previousConfidence: r.previousConfidence` to the `collect(DISTINCT {...})` map. These properties will be null on edges that were never promoted, which is the correct behavior
    - [x] 3.4 Update `ParseEdges` in GraphTraversalService.cs to extract `verifiedBy` and `previousConfidence` from the edge map. Pattern: `string? verifiedBy = edgeMap.TryGetValue("verifiedBy", out object? vbVal) ? vbVal?.ToString() : null;` and `float? previousConfidence = edgeMap.TryGetValue("previousConfidence", out object? pcVal) && pcVal is double pcDbl ? (float)pcDbl : null;`. Pass these to the `TraversalEdgeInfo` constructor via init syntax: `new TraversalEdgeInfo(...) { VerifiedBy = verifiedBy, PreviousConfidence = previousConfidence }`

- [x] Task 4: Gap detection in GraphTraversalService (AC: #1, #2, #5)
    - [x] 4.1 Modify `TraverseAsync` in GraphTraversalService.cs (lines 56-80) to detect stub nodes and separate them into gap markers. **Detection mechanism:** stub nodes created by `BuildMergeStubNode` (GraphQueryBuilder.cs:175-187) have ONLY the `id` property — `content`, `ingestedAt`, `sourceUri`, `sourceType` are all absent/null. When the traversal query returns `n.content AS content`, it will be null for stubs. Check content nullity to distinguish stubs from full nodes:
        ```csharp
        List<TraversalNode> nodes = [];
        List<TraversalGapMarker> gapMarkers = [];
        foreach (Record record in resultSet)
        {
            string nodeId = record.GetValue<string>("nodeId");
            object? contentRaw = record.GetValue<object>("content");
            if (contentRaw is null or "")
            {
                // Stub node — gap marker (FR49)
                long hopDistance = record.GetValue<long>("hopDistance");
                List<TraversalEdgeInfo> edges = ParseEdges(record);
                gapMarkers.Add(new TraversalGapMarker(nodeId, (int)hopDistance, edges));
            }
            else
            {
                TraversalNode node = ParseTraversalNode(record);
                nodes.Add(node);
            }
        }
        ```
    - [x] 4.2 Update the return statement to include gap markers: `return new TraversalResult(startNodeId, depth, nodes, nodes.Count) { GapMarkers = gapMarkers };`. Note: `TotalNodeCount` should be `nodes.Count` (only full nodes), NOT `nodes.Count + gapMarkers.Count` — gap markers are explicitly NOT counted as real nodes. This distinction matters for consumers that use TotalNodeCount to allocate or paginate
    - [x] 4.3 Update the graph-not-found return: `return new TraversalResult(startNodeId, depth, [], 0);` — GapMarkers defaults to `[]` via init property, so no change needed here
    - [x] 4.4 **Retroactive gap resolution (AC #5):** No new code needed. When the missing node is eventually ingested, `BuildMergeMemoryUnitNode` (GraphQueryBuilder.cs:22-64) uses `MERGE (m:MemoryUnit {id: $id}) SET m.content = ...` — this finds the existing stub and fills in all properties. The next traversal will see `content != null` and classify it as a full node, not a gap. **Integration test 12.3 verifies this end-to-end.**
    - [x] 4.5 Refactor `ParseTraversalNode` to receive the raw content string as parameter instead of reading it from the record again. This avoids double-reading the same record value. Signature: `ParseTraversalNode(Record record, string content)` where content is the already-validated non-null string from the stub check

- [x] Task 5: Confidence promotion query in GraphQueryBuilder (AC: #3, #4)
    - [x] 5.1 Add `BuildUpdateEdgeConfidence(string sourceNodeId, string targetNodeId, EdgeType edgeType, float newConfidence, string verifiedBy)` to `IGraphQueryBuilder` interface. Returns `(string Query, IDictionary<string, object> Parameters)`. The Cypher query uses MATCH (not MERGE) because the edge must already exist — creating edges on promotion is wrong
    - [x] 5.2 Implement in `GraphQueryBuilder`:
        - Input validation: `ArgumentException.ThrowIfNullOrWhiteSpace(sourceNodeId)`, same for targetNodeId and verifiedBy. `ArgumentOutOfRangeException.ThrowIfLessThan(newConfidence, 0f)`, `ArgumentOutOfRangeException.ThrowIfGreaterThan(newConfidence, 1f)`
        - Resolve labels: `(string sourceLabel, string targetLabel) = GetNodeLabels(edgeType);` — reuse existing method (line 349-354). This correctly handles CONTAINS edges (Case→MemoryUnit) vs all other edges (MemoryUnit→MemoryUnit)
        - Convert edge type: `string edgeLabel = ToUpperSnakeCase(edgeType);` — reuse existing method (line 339-347)
        - Cypher query:
            ```
            MATCH (s:{sourceLabel} {id: $sourceId})-[r:{edgeLabel}]->(t:{targetLabel} {id: $targetId})
            SET r.previousConfidence = r.confidence, r.confidence = $newConfidence, r.verifiedBy = $verifiedBy
            RETURN r.confidence AS newConfidence, r.previousConfidence AS previousConfidence
            ```
        - Edge label is interpolated (not parameterized) — safe because derived from closed EdgeType enum via validated ToUpperSnakeCase switch. Same safety pattern as BuildMergeEdge (line 158-161). Document in code comment
        - `previousConfidence` is set to the CURRENT `r.confidence` BEFORE updating to newConfidence. This means: first promotion stores original default, subsequent promotions store the previous promoted value. Both are audit-useful
    - [x] 5.3 Parameters dict: `sourceId`, `targetId`, `newConfidence`, `verifiedBy`. Edge label and node labels NOT in parameters (interpolated from closed enum — same pattern as all other edge queries)

- [x] Task 6: Confidence promotion in GraphTraversalService (AC: #3, #4)
    - [x] 6.1 Add `PromoteEdgeConfidenceAsync(string tenantId, ConfidencePromotionRequest request, CancellationToken ct)` to GraphTraversalService. Returns `ConfidencePromotionResult?` — null means edge not found
    - [x] 6.2 Implementation:
        1. Validate tenantId: `ArgumentException.ThrowIfNullOrWhiteSpace(tenantId)`
        2. Build query: `_graphQueryBuilder.BuildUpdateEdgeConfidence(request.SourceNodeId, request.TargetNodeId, request.EdgeType, request.NewConfidence, request.VerifiedBy)`
        3. Execute: `await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout, ct)`
        4. Handle graph-not-found: catch `RedisServerException` via `IsGraphNotFoundError` — return null (edge can't exist if graph doesn't exist)
        5. Check result: if `ResultSet` has no records, return null (edge not found)
        6. If result has a record: extract `newConfidence` and `previousConfidence` from the record, return `ConfidencePromotionResult(request.SourceNodeId, request.TargetNodeId, request.EdgeType, previousConfidence, newConfidence, request.VerifiedBy)`
    - [x] 6.3 Add logging: `LogConfidencePromoted(tenantId, sourceNodeId, targetNodeId, edgeType, previousConfidence, newConfidence, verifiedBy)` at Information level. `LogEdgeNotFound(tenantId, sourceNodeId, targetNodeId, edgeType)` at Warning level. Follow existing LoggerMessage pattern (lines 185-192)

- [x] Task 7: Confidence promotion endpoint (AC: #3, #4)
    - [x] 7.1 Add `PATCH /api/tenants/{tenantId}/edges/confidence` endpoint in `Program.cs`. Place it after the existing traverse endpoint (line 1002) and before `app.Run()` (line 1004). Follow existing Minimal API pattern
    - [x] 7.2 Parameters: `{tenantId}` (route), `ConfidencePromotionRequest request` (from body — JSON deserialized)
    - [x] 7.3 Inject `GraphTraversalService traversalService` into the endpoint delegate
    - [x] 7.4 Validation sequence:
        1. TenantId validation: `ValidateTenantId(tenantId)` — reuse existing helper (same as traverse endpoint)
        2. SourceNodeId: if null/empty → 400 with `ErrorResponse("MISSING_SOURCE_NODE", "sourceNodeId is required.", "Provide the source node ID of the edge to promote.")`
        3. TargetNodeId: if null/empty → 400 with `ErrorResponse("MISSING_TARGET_NODE", "targetNodeId is required.", "Provide the target node ID of the edge to promote.")`
        4. VerifiedBy: if null/empty → 400 with `ErrorResponse("MISSING_VERIFIED_BY", "verifiedBy is required.", "Provide the identity of the person verifying the relationship.")`
        5. NewConfidence: if `< 0f` or `> 1f` → 400 with `ErrorResponse("INVALID_CONFIDENCE", $"Confidence must be between 0.0 and 1.0, got {request.NewConfidence}.", "Provide a confidence value in the range [0.0, 1.0].")`. **Boundary values 0.0 and 1.0 are valid.** Use `request.NewConfidence < 0f || request.NewConfidence > 1f` (strict inequality) — must match the query builder validation in Task 5.2 which uses `ThrowIfLessThan(0f)` and `ThrowIfGreaterThan(1f)` (both inclusive of boundaries)
    - [x] 7.5 Call service: `var result = await traversalService.PromoteEdgeConfidenceAsync(tenantId, request, cancellationToken)`
    - [x] 7.6 If result is null → return 404 with `ErrorResponse("EDGE_NOT_FOUND", $"No {request.EdgeType} edge found from '{request.SourceNodeId}' to '{request.TargetNodeId}'.", "Verify the edge exists by traversing from either node. Note: edges are directed — sourceNodeId must be the relationship origin (e.g., for causedBy, the CausationId is the source).")`
    - [x] 7.7 Return `Results.Ok(result)` — 200 OK with `ConfidencePromotionResult` JSON body (not 204 No Content, because the response includes the previous/new confidence values the caller needs for confirmation)

- [x] Task 8: Contract serialization tests (AC: #1, #2, #3, #4)
    - [x] 8.1 Create `tests/Hexalith.Memories.Contracts.Tests/V1/TraversalGapMarkerSerializationTests.cs` — roundtrip JSON test: serialize/deserialize TraversalGapMarker with MissingNodeId, HopDistance, and Edges list containing edge info. Verify camelCase: `missingNodeId`, `hopDistance`, `edges`
    - [x] 8.2 Create `tests/Hexalith.Memories.Contracts.Tests/V1/ConfidencePromotionRequestSerializationTests.cs` — roundtrip test with all fields, verify camelCase and EdgeType enum serialized as camelCase string
    - [x] 8.3 Create `tests/Hexalith.Memories.Contracts.Tests/V1/ConfidencePromotionResultSerializationTests.cs` — roundtrip test with all fields including PreviousConfidence
    - [x] 8.4 Add to existing `TraversalResultSerializationTests.cs`: test that TraversalResult with GapMarkers serializes/deserializes correctly. Verify empty GapMarkers `[]` appears in JSON (not omitted)
    - [x] 8.5 Add to existing `TraversalEdgeInfoSerializationTests.cs`: test that VerifiedBy and PreviousConfidence serialize when present, and are omitted from JSON when null (WhenWritingNull behavior)

- [x] Task 9: Gap detection unit tests (AC: #1, #2, #5)
    - [x] 9.1 Add to `GraphTraversalServiceTests.cs`:
        - `TraverseAsync_StubNodeDetectedAsGapMarker` — mock FalkorDB to return a record with null content. Verify the result has 0 nodes and 1 gap marker with the correct MissingNodeId
        - `TraverseAsync_FullNodeNotFlaggedAsGap` — mock record with non-null content. Verify 1 node, 0 gap markers
        - `TraverseAsync_MixedStubsAndFullNodes_SeparatedCorrectly` — mock 3 records: full, stub, full. Verify 2 nodes and 1 gap marker with correct IDs
        - `TraverseAsync_GapMarkerHasCorrectHopDistanceAndEdges` — mock stub with hopDistance=2 and edges. Verify gap marker properties match
        - `TraverseAsync_TotalNodeCountExcludesGapMarkers` — mock 2 full nodes + 1 stub. Verify TotalNodeCount=2 (not 3)

- [x] Task 10: Confidence promotion unit tests (AC: #3, #4)
    - [x] 10.1 Add to `GraphQueryBuilderTests.cs`:
        - `BuildUpdateEdgeConfidence_GeneratesCorrectCypher` — verify query contains MATCH, SET with previousConfidence and verifiedBy, RETURN
        - `BuildUpdateEdgeConfidence_UsesCorrectEdgeLabel` — Theory with all 5 EdgeType values, verify UPPER_SNAKE_CASE label in query
        - `BuildUpdateEdgeConfidence_UsesCorrectNodeLabels` — verify Contains uses (Case, MemoryUnit), others use (MemoryUnit, MemoryUnit)
        - `BuildUpdateEdgeConfidence_ParameterizesValues` — verify $sourceId, $targetId, $newConfidence, $verifiedBy in parameters dict, edge label NOT parameterized
        - `BuildUpdateEdgeConfidence_NullSourceNodeId_Throws` — ArgumentException
        - `BuildUpdateEdgeConfidence_NegativeConfidence_Throws` — ArgumentOutOfRangeException
        - `BuildUpdateEdgeConfidence_ConfidenceAboveOne_Throws` — ArgumentOutOfRangeException
        - `BuildUpdateEdgeConfidence_InjectionPrevention` — adversarial sourceNodeId NOT in query string, IS in parameters
    - [x] 10.2 Add to `GraphTraversalServiceTests.cs`:
        - `PromoteEdgeConfidenceAsync_CallsQueryBuilder` — mock IGraphQueryBuilder, verify BuildUpdateEdgeConfidence called with correct args
        - `PromoteEdgeConfidenceAsync_EdgeNotFound_ReturnsNull` — mock empty ResultSet, verify null returned
        - `PromoteEdgeConfidenceAsync_GraphNotFound_ReturnsNull` — mock RedisServerException, verify null returned
        - `PromoteEdgeConfidenceAsync_Success_ReturnsResult` — mock ResultSet with record, verify ConfidencePromotionResult fields

- [x] Task 11: Edge audit field tests (AC: #3, #4)
    - [x] 11.1 Add to `GraphQueryBuilderTests.cs`:
        - `BuildTraverseWithEdges_IncludesVerifiedByInEdgeMap` — verify Cypher query contains `verifiedBy: r.verifiedBy` in the collect() expression
        - `BuildTraverseWithEdges_IncludesPreviousConfidenceInEdgeMap` — verify Cypher query contains `previousConfidence: r.previousConfidence`
    - [x] 11.2 Add to `GraphTraversalServiceTests.cs`:
        - `ParseEdges_WithVerifiedBy_SetsProperty` — mock edge map with verifiedBy value, verify TraversalEdgeInfo.VerifiedBy is set
        - `ParseEdges_WithoutVerifiedBy_PropertyIsNull` — mock edge map without verifiedBy, verify null
        - `ParseEdges_WithPreviousConfidence_SetsProperty` — mock edge map with previousConfidence, verify float? value
        - `ParseEdges_WithoutPreviousConfidence_PropertyIsNull` — verify null when absent

- [x] Task 12: Integration tests (AC: #1, #2, #3, #4, #5)
    - [x] 12.1 Create `tests/Hexalith.Memories.IntegrationTests/Graph/GapDetectionIntegrationTests.cs`. **Test data setup:** Use `IGraphQueryBuilder` directly to create nodes and edges (same pattern as Story 4.1 integration tests). Create full nodes via `BuildMergeMemoryUnitNode` and stub nodes via `BuildMergeStubNode`, then edges via `BuildMergeEdge`
    - [x] 12.2 Test: **Single gap detection.** Create full MU-A (ingested), stub MU-B (not ingested), full MU-C (ingested). Create edges: B→A via CAUSED_BY, B→C via CAUSED_BY. Traverse from A with depth=3. Verify: A and C appear in `Nodes`, B appears in `GapMarkers` with `MissingNodeId = "MU-B"`. Verify TotalNodeCount=2 (excludes gap)
    - [x] 12.3 Test: **Multiple gaps.** Chain: A(full) ← B(stub) → C(stub) → D(full). Traverse from A with depth=5. Verify: A and D in Nodes, B and C in GapMarkers
    - [x] 12.4 Test: **Retroactive gap resolution (AC #5).** Step 1: Create full A, stub B, edge B→A. Traverse from A → 1 node (A), 1 gap marker (B). Step 2: Now ingest B fully via `BuildMergeMemoryUnitNode` (MERGE fills stub properties). Traverse from A again → 2 nodes (A, B), 0 gap markers. Proves the MERGE-based fill resolves the gap without manual intervention
    - [x] 12.5 Test: **No false gaps.** Ingest A and B fully, create B→A edge. Traverse from A. Verify 2 nodes, 0 gap markers. Full nodes must never be flagged as gaps
    - [x] 12.6 Test: **Gap marker has edges.** Stub B with edges to A and C. Traverse, verify B's gap marker contains edge metadata (type, confidence, direction) for both edges
    - [x] 12.7 Create `tests/Hexalith.Memories.IntegrationTests/Graph/ConfidencePromotionIntegrationTests.cs`
    - [x] 12.8 Test: **Promote inferred edge.** Create A→B edge with confidence=0.5, origin=inferred. PATCH /edges/confidence with newConfidence=1.0, verifiedBy="user@test.com". Verify: 200 response, previousConfidence=0.5, newConfidence=1.0. Traverse from A, verify B's edge shows confidence=1.0, verifiedBy="user@test.com", previousConfidence=0.5
    - [x] 12.9 Test: **Promote explicit edge (AC #4).** Create CausedBy edge with confidence=1.0, origin=explicit. Promote to confidence=0.9, verifiedBy="auditor". Verify: operation succeeds, previousConfidence=1.0, origin remains "explicit" (not changed to "inferred")
    - [x] 12.10 Test: **Double promotion preserves audit chain.** Promote edge from 0.5→0.8 (verifiedBy=user1). Then promote from 0.8→1.0 (verifiedBy=user2). Verify: previousConfidence=0.8 (from first promotion), verifiedBy="user2" (latest promoter). Note: only the most recent promotion audit is stored — full history requires a log (future work)
    - [x] 12.11 Test: **Edge not found returns 404.** PATCH with nonexistent source/target IDs. Verify 404 response with `EDGE_NOT_FOUND` error code
    - [x] 12.12 Test: **Invalid confidence returns 400.** PATCH with newConfidence=1.5. Verify 400 response with `INVALID_CONFIDENCE` error code
    - [x] 12.13 Test: **Missing verifiedBy returns 400.** PATCH with empty verifiedBy. Verify 400 with `MISSING_VERIFIED_BY`
    - [x] 12.14 Test: **Structural edge promotion.** Create CONTAINS edge (Case→MU) with confidence=1.0. Promote to confidence=0.7, verifiedBy="auditor". Verify: operation succeeds, MATCH uses `(Case)-[:CONTAINS]->(MemoryUnit)` node labels. Proves structural edges are promotable via the same API
    - [x] 12.15 Test: **Gap marker edge direction correctness.** Stub B with incoming edge from A (A←B via CAUSED_BY, meaning B→A) and outgoing edge to C (B→C via CAUSED_BY). Traverse from A, verify B's gap marker edges show correct `direction` values relative to B. If `startNode(r)` behaves differently for stubs vs full nodes in FalkorDB, this test catches it
    - [x] 12.16 _(Optional, nice-to-have)_ Test: **Gap detection on traversal with edge type filter.** If Story 4.2 is implemented, verify gap detection still works when `edgeTypes` query parameter is used. Stub B with only CAUSED_BY edges — filter by causedBy should still detect the gap. **Skip if 4.2 is not yet implemented.**

### Review Findings

- [x] [Review][Patch] Case-filtered traversal drops stub nodes before gap detection [src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:291]
- [x] [Review][Patch] PATCH /edges/confidence accepts missing value-type fields as valid defaults [src/Hexalith.Memories.Server/Program.cs:1028]
- [x] [Review][Patch] Traversal no longer uses Redis content fallback before classifying gaps [src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:81]
- [x] [Review][Patch] Story 4.3 tests still miss key traversal and promotion assertions [tests/Hexalith.Memories.IntegrationTests/Graph/ConfidencePromotionIntegrationTests.cs:68]

## Dev Notes

### Critical Dependency: Story 4.1 Must Be Implemented First

This story extends the traversal infrastructure created in Story 4.1. All changes modify files and methods from 4.1:

- `GraphTraversalService.cs` — modify TraverseAsync, ParseEdges, add PromoteEdgeConfidenceAsync
- `TraversalNode.cs`, `TraversalEdgeInfo.cs`, `TraversalResult.cs` — extend contracts
- `BuildTraverseWithEdges` in `GraphQueryBuilder.cs` — update Cypher edge collection map
- `GET /api/tenants/{tenantId}/traverse` endpoint — response now includes GapMarkers

**Do NOT start this story until Story 4.1 has moved to `done` status in sprint-status.yaml.** Story 4.1 is currently in `review` — if the code review surfaces changes to `GraphTraversalService` or `BuildTraverseWithEdges`, those changes ripple into this story's assumptions. Wait for review completion to avoid rework.

### Story 4.2 Independence

This story does NOT depend on Story 4.2 (Edge Type Filtering & Taxonomy). It works with the 2-param and 3-param `BuildTraverseWithEdges` overloads from Story 4.1. If Story 4.2 is already implemented when this story starts (adding a 4-param overload with edge type filtering), the gap detection logic still applies — stub detection is based on null content, independent of which edge types are followed. The only Task affected is integration test 12.14 (optional, only if 4.2 is done).

**Pre-implementation check:** Read `IGraphQueryBuilder.cs` and `GraphQueryBuilder.cs` to verify whether Story 4.2's 4-param `BuildTraverseWithEdges` overload exists. If it does, update the Cypher edge collection map in BOTH the 3-param and 4-param overloads (Task 3.3). If not, update only the 3-param overload.

### Implementation Order

Task 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8-12 (tests in parallel). Gap marker contract first (1), then promotion contracts (2), then edge audit fields (3), then gap detection logic (4), then promotion query builder (5), then promotion service (6), then promotion endpoint (7), then all tests.

### Recommended Test Execution Order

**Integration test 12.2 (single gap detection) is the canary test.** Run it FIRST — if FalkorDB returns something unexpected for null properties on stub nodes (empty string instead of null, a default value, or throws), the entire gap detection heuristic fails. If 12.2 passes, the approach is validated.

Recommended order: **12.2** (canary) → **9.1-9.5** (unit gap detection) → **12.4** (retroactive resolution) → **10.1-10.2** (unit promotion) → **12.8-12.9** (integration promotion) → everything else. This front-loads the highest-risk tests and validates core assumptions before writing peripheral test coverage.

### Gap Detection: How It Works

**The stub node pattern is the key insight.** During ingestion (IndexGraphActivity.cs:72-100), when a memory unit A has CausationId=B:

1. `BuildMergeStubNode(B)` creates a bare node: `MERGE (m:MemoryUnit {id: $id})` — ONLY the `id` property is set
2. `BuildMergeEdge(B, A, CausedBy, 1.0, Explicit)` creates the edge

The stub for B exists in the graph but has NO content, sourceUri, sourceType, or ingestedAt properties. When the traversal query returns `n.content AS content`, FalkorDB returns null for absent properties.

**Detection in C#:** In `ParseTraversalNode`, if `content` is null, the node is a stub → gap marker. If content is non-null, it's a fully ingested node. This is the ONLY reliable heuristic: stubs are created by `BuildMergeStubNode` which sets only `id`, while full nodes are created by `BuildMergeMemoryUnitNode` which sets ALL properties including content (validated as non-null/non-whitespace at line 37).

**Why not a separate `isStub` property?** Adding a boolean flag to nodes would require modifying both `BuildMergeStubNode` (set `isStub=true`) and `BuildMergeMemoryUnitNode` (set `isStub=false`). The content-nullity check is simpler, zero-change for existing code, and semantically correct: a stub IS a node without content. If a future story needs to distinguish "empty content" from "no content" (different semantics), add the explicit flag then.

**Known assumption:** This heuristic assumes `content == null` always means stub. Today this holds — `BuildMergeMemoryUnitNode` validates content as non-null at line 37. If a future story introduces a node type that legitimately has no content (e.g., a metadata-only unit), this heuristic breaks silently. The explicit `isStub` flag is the escape hatch — defer until needed.

**Canary test fallback plan:** If integration test 12.2 fails because FalkorDB returns an unexpected value for absent properties (not null, not empty string — e.g., a driver-specific sentinel), abandon the content-null heuristic and switch to an explicit `isStub` property: modify `BuildMergeStubNode` to `MERGE (m:MemoryUnit {id: $id}) SET m._stub = true` and `BuildMergeMemoryUnitNode` to `SET m._stub = false`. Detect via `n._stub AS isStub` in the traversal query. This is more invasive (touches two existing methods) but guaranteed reliable regardless of driver behavior. Only use this fallback if the canary fails.

### Source-Generated JSON Verification

After implementing Task 3.1 (`TraversalEdgeInfo` init properties with `WhenWritingNull`), run a quick compile + serialization test (Task 8.5) BEFORE writing further tests. The `MemoriesJsonContext` source generator should pick up attributes on non-positional init properties, but verify this produces the expected JSON shape (fields omitted when null, present when set). If the source generator doesn't handle it, switch to a `[JsonExtensionData]` approach or add the properties to the positional constructor as nullable parameters.

### Retroactive Gap Resolution: Zero New Code

When the missing event eventually arrives and is ingested:

1. `BuildMergeMemoryUnitNode` uses `MERGE (m:MemoryUnit {id: $id}) SET m.content = $content, ...` (GraphQueryBuilder.cs:44)
2. The MERGE finds the existing stub node by `id` and fills in all properties via SET
3. The next traversal sees `content != null` → classified as full node, not a gap

No new code needed. Integration test 12.4 verifies this end-to-end.

### Confidence Promotion: Edge Property Updates

FalkorDB stores edge properties as key-value pairs on relationships. The `SET r.previousConfidence = r.confidence` Cypher clause reads the current value and stores it as the audit field in a single atomic operation. Each individual query is atomic within FalkorDB.

**Concurrent promotion behavior (last-writer-wins):** If two PATCH requests hit the same edge simultaneously, both read the same `r.confidence`, both set `previousConfidence` to the same value, and one overwrites the other's promotion. The last writer wins. This is acceptable for MVP — confidence promotion is a low-frequency human action, not a high-concurrency path. Integration test 12.10 (double promotion) documents the sequential behavior; concurrent race conditions are a known accepted edge case.

**Audit limitation:** Only the MOST RECENT promotion is tracked (previousConfidence stores the value before the last promotion, not the full history). Full audit history would require an append-only structure (e.g., Redis Stream keyed by edge identity, or a dedicated event log) — not edge properties, which are mutable registers. Out of scope for MVP. The dev note in ConfidencePromotionResult should document this single-level limitation.

### Edge Direction Matters for Promotion

The confidence promotion endpoint requires `sourceNodeId` and `targetNodeId` because edges in FalkorDB are directed: `(source)-[r:TYPE]->(target)`. The Cypher MATCH uses `->` (directed match). If the developer gets the direction wrong, the edge won't be found → 404.

**Reference for edge direction in ingestion:**

- CausedBy: `(causationId)-[:CAUSED_BY]->(memoryUnitId)` — "causationId caused memoryUnitId" (IndexGraphActivity.cs:78-84)
- CorrelatedWith: `(correlationId)-[:CORRELATED_WITH]->(memoryUnitId)` (IndexGraphActivity.cs:93-99)
- Contains: `(caseId)-[:CONTAINS]->(memoryUnitId)` (IndexGraphActivity.cs:64-70)
- Annotates: `(annotationMuId)-[:ANNOTATES]->(targetMuId)` (CaseService.cs:157-164)

The error message for EDGE_NOT_FOUND should include a suggestion to verify edge direction.

### Structural Edge Promotion: Allowed but Edge Case

The `BuildUpdateEdgeConfidence` method reuses `GetNodeLabels(EdgeType)` which maps CONTAINS to (Case, MemoryUnit). This means promoting a CONTAINS or ANNOTATES edge is technically possible. The ACs only mention semantic edges (causedBy, references), but no AC forbids promoting structural edges. **Decision: allow it.** The API accepts all 5 EdgeType values for promotion — restricting to semantic-only would add validation complexity for no user benefit. If this proves confusing, a future story can add a guard. Integration test 12.15 verifies structural edge promotion works correctly.

### TraversalResult Non-Breaking Extension

The `GapMarkers` property is added as a non-positional init property with default `[]`. This means:

- Existing code: `new TraversalResult("id", 2, nodes, 5)` → still compiles, GapMarkers defaults to `[]`
- New code: `new TraversalResult("id", 2, nodes, 5) { GapMarkers = gapMarkers }` → sets gap markers
- JSON serialization: `gapMarkers` always appears in response (empty array `[]` when no gaps)

Similarly, `TraversalEdgeInfo` new properties are non-positional init with null defaults:

- Existing code: `new TraversalEdgeInfo(EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, "id", "outgoing")` → still works, VerifiedBy and PreviousConfidence are null
- JSON serialization: `verifiedBy` and `previousConfidence` omitted from JSON when null (via `WhenWritingNull`)

### Existing Infrastructure Reuse

| What to reuse                | Where                            | Why                                                  |
| ---------------------------- | -------------------------------- | ---------------------------------------------------- |
| `ToUpperSnakeCase(EdgeType)` | GraphQueryBuilder.cs:339-347     | Convert EdgeType to Cypher label for promotion query |
| `GetNodeLabels(EdgeType)`    | GraphQueryBuilder.cs:349-354     | Source/target labels for promotion MATCH pattern     |
| `IsGraphNotFoundError()`     | GraphTraversalService.cs:181-183 | Handle missing graph in promotion                    |
| `TenantIdGuard.Validate()`   | Program.cs:984                   | Reuse in promotion endpoint                          |
| `ValidateTenantId()` helper  | Program.cs                       | Reuse for 400 validation response                    |
| `ErrorResponse` pattern      | Contracts/V1/ErrorResponse.cs    | Standard 400/404 response shape                      |
| `ParseEdges()` method        | GraphTraversalService.cs:101-138 | Extend for verifiedBy/previousConfidence             |
| `BuildMergeStubNode()`       | GraphQueryBuilder.cs:174-187     | Understanding stub node shape for gap detection      |
| `GraphOperationTimeout`      | GraphTraversalService.cs:19      | Reuse for promotion timeout                          |

### Key Files to Modify

| File                                    | Change                                                                                    |
| --------------------------------------- | ----------------------------------------------------------------------------------------- |
| `Contracts/V1/TraversalResult.cs`       | Add `GapMarkers` init property                                                            |
| `Contracts/V1/TraversalEdgeInfo.cs`     | Add `VerifiedBy`, `PreviousConfidence` init properties                                    |
| `Contracts/V1/MemoriesJsonContext.cs`   | Register new types                                                                        |
| `Server/Graph/IGraphQueryBuilder.cs`    | Add `BuildUpdateEdgeConfidence` method                                                    |
| `Server/Graph/GraphQueryBuilder.cs`     | Implement `BuildUpdateEdgeConfidence`, update Cypher edge map in `BuildTraverseWithEdges` |
| `Server/Graph/GraphTraversalService.cs` | Gap detection in TraverseAsync, add PromoteEdgeConfidenceAsync, extend ParseEdges         |
| `Server/Program.cs`                     | Add PATCH /edges/confidence endpoint                                                      |

### Key Files to Create

| File                                         | Purpose                               |
| -------------------------------------------- | ------------------------------------- |
| `Contracts/V1/TraversalGapMarker.cs`         | Gap marker contract for missing nodes |
| `Contracts/V1/ConfidencePromotionRequest.cs` | Request body for confidence promotion |
| `Contracts/V1/ConfidencePromotionResult.cs`  | Response for confidence promotion     |

### Testing Patterns to Follow

**Unit tests (Shouldly assertions, same as GraphQueryBuilderTests.cs):**

- `query.ShouldContain("previousConfidence")` — verify audit field in query
- `query.ShouldContain("verifiedBy")` — verify audit field in query
- `result.GapMarkers.Count.ShouldBe(1)` — verify gap detection
- `result.GapMarkers[0].MissingNodeId.ShouldBe("expected-id")` — verify gap marker ID
- Theory tests with `[InlineData]` for edge type variations

**Integration tests:**

- Follow `TraversalEndpointIntegrationTests.cs` pattern from Story 4.1
- Use `BuildMergeStubNode` + `BuildMergeMemoryUnitNode` + `BuildMergeEdge` for precise graph setup
- HTTP client calls to traverse and promote endpoints
- Assert on JSON response structure including gapMarkers array

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
- `ParseEdges` method parsing edge map from FalkorDB collect() result
- Content truncation at 200 chars with word boundary

Story 4.2 (if implemented) established:

- `EdgeTypeCategory` enum (Structural, Semantic) and `EdgeTypeTaxonomy` static helpers
- 4-param `BuildTraverseWithEdges` overload with edgeTypes filtering
- `edgeTypes` query parameter on traverse endpoint
- Pipe-separated edge type labels in Cypher: `[:CAUSED_BY|CORRELATED_WITH*0..{depth}]`

Story 3.6 established:

- `BuildMergeStubNode` + `BuildMergeEdge` pattern for annotation edges
- Compensation pattern for graph cleanup on failure

### Git Intelligence

Recent commits show pattern: contracts first, then service logic, then endpoint, then tests. Commit messages use conventional format: `feat:`, `fix:`. Story 4.1's commit (`b8d8ea3`) added traversal models and serialization tests, followed by the traversal feature implementation (`2f63fc1`).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.3] — AC definitions, gap marker format, confidence promotion
- [Source: _bmad-output/planning-artifacts/prd.md#FR49] — Gap marker with missing node identifier
- [Source: _bmad-output/planning-artifacts/prd.md#FR51] — Promote AI-inferred edge confidence
- [Source: _bmad-output/planning-artifacts/prd.md:485] — Gap detection responsibility of Interpretation layer
- [Source: _bmad-output/planning-artifacts/prd.md:499] — Users can promote AI-inferred edge confidence, system never auto-promotes
- [Source: _bmad-output/planning-artifacts/architecture.md:49] — FR46-49 gap detection with retroactive gap-filling
- [Source: _bmad-output/planning-artifacts/architecture.md:74] — DAPR out-of-order delivery, causal chain gaps fillable retroactively
- [Source: _bmad-output/planning-artifacts/architecture.md:131-133] — Structural vs Semantic edge classification
- [Source: _bmad-output/planning-artifacts/architecture.md:1472-1474] — Traverse response: ordered nodes + edges + gap markers
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:174-187] — BuildMergeStubNode creating bare nodes
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:22-64] — BuildMergeMemoryUnitNode MERGE+SET pattern (fills stubs retroactively)
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:148-172] — BuildMergeEdge with confidence + origin
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:265-288] — BuildTraverseWithEdges Cypher query with edge collection
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:339-347] — ToUpperSnakeCase mapping
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:349-354] — GetNodeLabels for edge type → source/target label
- [Source: src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:40-81] — TraverseAsync current implementation
- [Source: src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:101-138] — ParseEdges current implementation
- [Source: src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:181-183] — IsGraphNotFoundError pattern
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs:72-100] — CausationId/CorrelationId edge creation with stubs
- [Source: src/Hexalith.Memories.Contracts/V1/ErrorResponse.cs] — Standard error response pattern
- [Source: src/Hexalith.Memories.Contracts/V1/EdgeType.cs] — 5 edge types
- [Source: src/Hexalith.Memories.Contracts/V1/EdgeTypeDefaults.cs] — Confidence values per type
- [Source: src/Hexalith.Memories.Contracts/V1/TraversalResult.cs] — Current traversal result shape
- [Source: src/Hexalith.Memories.Contracts/V1/TraversalEdgeInfo.cs] — Current edge info shape
- [Source: src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs] — JSON context registration pattern
- [Source: tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs] — Unit test patterns
- [Source: tests/Hexalith.Memories.Server.Tests/Graph/GraphTraversalServiceTests.cs] — Service test patterns
- [Source: _bmad-output/implementation-artifacts/4-1-causal-chain-traversal.md] — Story 4.1 implementation details
- [Source: _bmad-output/implementation-artifacts/4-2-edge-type-filtering-and-taxonomy.md] — Story 4.2 spec (may or may not be implemented)

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- All unit tests pass: 218 contract tests, 631+ server unit tests
- All builds succeed with 0 warnings, 0 errors
- Story 4.2 confirmed implemented — both 3-param and 4-param BuildTraverseWithEdges overloads updated with verifiedBy/previousConfidence edge map fields
- Content-null heuristic validated: stub nodes created by BuildMergeStubNode have only `id` property; content is null in FalkorDB for stubs
- ParseTraversalNodeAsync refactored to accept content parameter — avoids double-reading record values and enables gap detection before fallback

### Completion Notes List

- **Task 1:** Created `TraversalGapMarker` contract, added `GapMarkers` init property to `TraversalResult` (non-breaking), registered types in `MemoriesJsonContext`
- **Task 2:** Created `ConfidencePromotionRequest` and `ConfidencePromotionResult` contracts, registered in JSON context
- **Task 3:** Extended `TraversalEdgeInfo` with `VerifiedBy` and `PreviousConfidence` nullable init properties with `WhenWritingNull` serialization. Updated Cypher edge collection map in `BuildTraverseWithEdges`. Updated `ParseEdgeCollection` to extract and pass audit fields. Updated `TryCreateEdgeMapFromSequence` to handle 7-element arrays and `IsKnownEdgeFieldName` for new field names
- **Task 4:** Modified `TraverseAsync` to detect stub nodes (null content) and classify them as gap markers instead of skipping. Refactored `ParseTraversalNodeAsync` to accept pre-validated content string parameter. Gap markers include edge metadata. TotalNodeCount excludes gap markers
- **Task 5:** Added `BuildUpdateEdgeConfidence` to `IGraphQueryBuilder` and implemented in `GraphQueryBuilder` with full input validation and parameterized Cypher using `SET r.previousConfidence = r.confidence` for atomic audit trail
- **Task 6:** Added `PromoteEdgeConfidenceAsync` to `GraphTraversalService` with graph-not-found handling and LoggerMessage logging
- **Task 7:** Added `PATCH /api/tenants/{tenantId}/edges/confidence` endpoint with full validation (tenant, source/target/verifiedBy, confidence range), 404 for edge not found, 200 OK with result body
- **Tasks 8-12:** Created 3 new serialization test files, extended 2 existing. Added unit tests for gap detection, confidence promotion, edge audit fields. Created 2 integration test files for gap detection (6 tests) and confidence promotion (7 tests)

### File List

**New files:**

- src/Hexalith.Memories.Contracts/V1/TraversalGapMarker.cs
- src/Hexalith.Memories.Contracts/V1/ConfidencePromotionRequest.cs
- src/Hexalith.Memories.Contracts/V1/ConfidencePromotionResult.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/TraversalGapMarkerSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/ConfidencePromotionRequestSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/ConfidencePromotionResultSerializationTests.cs
- tests/Hexalith.Memories.IntegrationTests/Graph/GapDetectionIntegrationTests.cs
- tests/Hexalith.Memories.IntegrationTests/Graph/ConfidencePromotionIntegrationTests.cs

**Modified files:**

- src/Hexalith.Memories.Contracts/V1/TraversalResult.cs
- src/Hexalith.Memories.Contracts/V1/TraversalEdgeInfo.cs
- src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs
- src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs
- src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs
- src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs
- src/Hexalith.Memories.Server/Program.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/TraversalResultSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/TraversalEdgeInfoSerializationTests.cs
- tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs
- tests/Hexalith.Memories.Server.Tests/Graph/GraphTraversalServiceTests.cs

## Change Log

- 2026-04-13: Story 4.3 implementation complete — gap detection via content-null heuristic on stub nodes, confidence promotion with audit trail (previousConfidence, verifiedBy), PATCH endpoint, comprehensive unit and integration tests
