# Story 3.6: Annotations & Corrections

Status: ready-for-dev

## Story

As a developer,
I want to annotate or correct a memory unit, with annotations tracked as linked memory units,
so that human knowledge and corrections are preserved alongside the original content.

## Acceptance Criteria

1. **Given** an existing memory unit, **When** I create an annotation with text content and metadata, **Then** a new memory unit is created with the annotation content **And** an `annotates` edge (confidence 1.0, origin: explicit) is created from the annotation to the original memory unit in FalkorDB (FR37) **And** the annotation memory unit has its own embeddings and is independently searchable
2. **Given** a correction annotation, **When** I create it with type "correction", **Then** the metadata field `annotation_type` is set to "correction" with origin "human" and confidence 1.0 **And** the original memory unit is not modified — corrections are additive, not destructive
3. **Given** a memory unit with annotations, **When** I search and find the original memory unit, **Then** the result includes an `annotations_count` field **And** annotations can be retrieved by traversing the `annotates` edges from the result
4. **Given** the original memory unit is deleted, **When** the deletion is processed, **Then** the annotation memory units are also deleted (cascade via `annotates` edges)

## Tasks / Subtasks

- [ ] Task 1: Add `CreateAnnotationInput` contract and activity event type (AC: #1, #2)
  - [ ] 1.1 Create `Contracts/V1/CreateAnnotationInput.cs` — record with `TenantId`, `CaseId`, `TargetMemoryUnitId`, `Content`, `AnnotationType` (string, nullable — "correction", "clarification", "enrichment"), `IngestedBy`. **No `Metadata` field** — all annotation metadata is server-generated in Task 5.5 to prevent user override of `_system.*` keys
  - [ ] 1.2 Add `AnnotationCreated` value to `CaseActivityEventType` enum in `Contracts/V1/CaseActivityEventType.cs`
  - [ ] 1.3 Register `CreateAnnotationInput` and `List<MemoryUnit>` in `MemoriesJsonContext.cs` (`[JsonSerializable(typeof(CreateAnnotationInput))]`, `[JsonSerializable(typeof(List<MemoryUnit>))]` — the list type is needed for the GET annotations endpoint response)
  - [ ] 1.4 Add `Annotation` value to `SourceType` enum in `Contracts/V1/SourceType.cs` — semantically accurate for annotation MUs (preferred over reusing `Discussion`). Add roundtrip serialization test in `EnumSerializationTests.cs`
- [ ] Task 2: Add `AnnotationsCount` to `ScoredResult` (AC: #3)
  - [ ] 2.1 Add `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int AnnotationsCount { get; init; }` to `ScoredResult` record in `Contracts/V1/ScoredResult.cs`
- [ ] Task 3: Add graph query builder methods for annotations (AC: #3, #4)
  - [ ] 3.1 Add `BuildCountAnnotations(string memoryUnitId)` to `IGraphQueryBuilder` — returns `(string Query, IDictionary<string, object> Parameters)`
  - [ ] 3.2 Add `BuildListAnnotationIds(string memoryUnitId)` to `IGraphQueryBuilder` — returns list of annotation MU IDs
  - [ ] 3.3 Implement `BuildCountAnnotations` in `GraphQueryBuilder`: `MATCH (a:MemoryUnit)-[:ANNOTATES]->(m:MemoryUnit {id: $memoryUnitId}) RETURN count(a) AS count`
  - [ ] 3.4 Implement `BuildListAnnotationIds` in `GraphQueryBuilder`: `MATCH (a:MemoryUnit)-[:ANNOTATES]->(m:MemoryUnit {id: $memoryUnitId}) RETURN a.id AS annotationId`
- [ ] Task 4: Add validation for annotation creation (AC: #1, #2)
  - [ ] 4.1 Add `ValidateCreateAnnotation(string tenantId, string caseId, string targetMemoryUnitId, CreateAnnotationInput input)` to `CaseValidator` — validates tenantId, caseId, targetMemoryUnitId (not null/empty, safe regex), content not empty/max 50000 chars, annotationType if present must be in allowed set
  - [ ] 4.2 Reject nested annotations: verify target MU's metadata does NOT contain `_system.annotation_target` key (if it does, the target is itself an annotation — return error `NESTED_ANNOTATION_NOT_ALLOWED` with suggestion "Annotate the original memory unit instead")
- [ ] Task 5: Add annotation service methods to CaseService (AC: #1, #2, #3)
  - [ ] 5.1 Add `CreateAnnotationAsync(CreateAnnotationInput input, CancellationToken)` to `CaseService` — returns `MemoryUnit`
  - [ ] 5.2 Verify target MU exists and is indexed: `HashGetAsync({tenantId}:mu:{targetMemoryUnitId}, ["caseId", "status"])` — checks existence, validates case ownership, and confirms status
  - [ ] 5.3 Verify returned `caseId` matches `input.CaseId` (return 404 if mismatch — prevents cross-case annotation). Verify `status` equals `"indexed"` — reject with `MEMORY_UNIT_NOT_INDEXED` and suggestion "Wait for ingestion to complete before annotating" if status is queued/extracting/embedding/failed
  - [ ] 5.4 Generate annotation MU ID via `BaUlid.New(UlidOptions).ToString()`
  - [ ] 5.5 Build annotation metadata: always include `_system.annotation_target` = targetMemoryUnitId (origin: Human, confidence: 1.0); if `AnnotationType` is provided, add `_system.annotation_type` field (origin: Human, confidence: 1.0). The `_system.` prefix is a reserved namespace for server-generated metadata — prevents collision with user-supplied metadata keys
  - [ ] 5.6 Reuse ingestion pipeline: schedule `IngestionWorkflow` with annotation content as `ContentBytes` (UTF-8 encoded), `SourceUri` = `annotation:{targetMemoryUnitId}`, `SourceType` = `SourceType.Annotation`, metadata includes annotation fields, `CausationId` = targetMemoryUnitId
  - [ ] 5.7 Create stub node + edge BEFORE scheduling workflow: call `BuildMergeStubNode(annotationMuId)` then `BuildMergeEdge(annotationMuId, targetMemoryUnitId, EdgeType.Annotates, EdgeTypeDefaults.Annotates, EdgeOrigin.Explicit)` via FalkorDB. Then schedule `IngestionWorkflow`. **Wrap workflow scheduling in try/catch** — on failure, call `BuildDeleteMemoryUnitNode(annotationMuId)` to clean up the stub node and edge before re-throwing
  - [ ] 5.8 Record `AnnotationCreated` activity event via `_activityService.RecordEventAsync()` with `memoryUnitId: annotationMuId`
  - [ ] 5.9 Return annotation MemoryUnit with status `Queued` (embedding and indexing happen async via workflow)
  - [ ] 5.10 **Compensation guard:** If the `IngestionWorkflow` later fails for this annotation MU, the existing `CleanupGraphActivity` will remove the annotation node via `DETACH DELETE` — this also removes the `ANNOTATES` edge created in 5.7. Verify that `CleanupGraphActivity` is wired as compensation in the workflow for the annotation path. No new code needed if the workflow already compensates on failure, but confirm this during implementation
- [ ] Task 6: Add annotation count enrichment to search results (AC: #3)
  - [ ] 6.1 Add `BuildBatchCountAnnotations(IReadOnlyList<string> memoryUnitIds)` to `IGraphQueryBuilder` and implement in `GraphQueryBuilder` using batch Cypher: `UNWIND $ids AS muId OPTIONAL MATCH (a:MemoryUnit)-[:ANNOTATES]->(m:MemoryUnit {id: muId}) RETURN muId, count(a) AS count`
  - [ ] 6.2 Create `EnrichResultWithAnnotationCountsAsync` static method in `Program.cs` (follow `EnrichResultWithCaseAttributionAsync` pattern) — collect all `MemoryUnitId` values, execute single batch query via `BuildBatchCountAnnotations`, map counts back to results
  - [ ] 6.3 Enrich each `ScoredResult` with `AnnotationsCount` using `with` expression
  - [ ] 6.4 Wire enrichment into all search paths in `Program.cs` — call after `EnrichResultWithCaseAttributionAsync`
- [ ] Task 7: Extend deletion to cascade annotations (AC: #4) **[Depends on Story 3.5 deletion endpoints being implemented. If 3.5 is not yet done, defer this task — all other tasks are independent.]**
  - [ ] 7.1 Before deleting a MU (in the existing Story 3.5 deletion path), query annotation IDs via `BuildListAnnotationIds`
  - [ ] 7.2 For each annotation MU ID: delete from all 3 backends (same pattern as MU deletion — `KeyDeleteAsync` for Redis hash + vector, `BuildDeleteMemoryUnitNode` for FalkorDB)
  - [ ] 7.3 Then delete the target MU itself (existing `BuildDeleteMemoryUnitNode` with `DETACH DELETE` already cleans up the `ANNOTATES` edges)
  - [ ] 7.4 Note: Case deletion already works via `BuildListCaseMemoryUnitIds` which returns ALL MUs in the case including annotations (they share the same `caseId`), so case deletion automatically handles annotations
- [ ] Task 8: Add `POST` endpoint for annotation creation (AC: #1, #2)
  - [ ] 8.1 Add `POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations` in `Program.cs`
  - [ ] 8.2 Validate via `CaseValidator.ValidateCreateAnnotation()`, verify case exists via `GetCaseAsync()`, check case status is not `Deleting`
  - [ ] 8.3 Call `CaseService.CreateAnnotationAsync()`, return `202 Accepted` with annotation MU and workflow instance ID
- [ ] Task 9: Add `GET` endpoint for listing annotations (AC: #3)
  - [ ] 9.1 Add `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations` in `Program.cs`
  - [ ] 9.2 Query annotation IDs via `BuildListAnnotationIds`, load each annotation MU from Redis hash `{tenantId}:mu:{annotationId}`
  - [ ] 9.3 Return list of annotation MemoryUnit records
- [ ] Task 10: Unit tests for contract changes (AC: #1, #2, #3)
  - [ ] 10.1 Add `CaseActivityEventType.AnnotationCreated` roundtrip test to `EnumSerializationTests.cs`
  - [ ] 10.2 Add `CreateAnnotationInputSerializationTests.cs` — roundtrip JSON serialization test
  - [ ] 10.3 Add `ScoredResult` test verifying `AnnotationsCount` serialization (default 0 omitted, non-zero included)
- [ ] Task 11: Unit tests for graph query builder (AC: #3, #4)
  - [ ] 11.1 Add `BuildCountAnnotations_*` tests to `GraphQueryBuilderTests.cs` (valid ID returns parameterized query, null/empty throws)
  - [ ] 11.2 Add `BuildListAnnotationIds_*` tests to `GraphQueryBuilderTests.cs`
  - [ ] 11.3 Add `BuildBatchCountAnnotations_*` tests to `GraphQueryBuilderTests.cs` (empty list, single ID, multiple IDs)
- [ ] Task 12: Unit tests for CaseValidator (AC: #1, #2)
  - [ ] 12.1 Add `ValidateCreateAnnotation_*` tests to `CaseValidatorTests.cs` (valid, null targetId, empty content, too-long content, invalid annotation type, nested annotation rejected)
- [ ] Task 13: Unit tests for CaseService annotation methods (AC: #1, #2, #3, #4)
  - [ ] 13.1 Add `CreateAnnotationAsync_*` tests to `CaseServiceTests.cs`: target MU found + annotation created, target MU not found (returns null/error), target MU wrong case, target MU not indexed (status=failed → rejected), activity event recorded, metadata includes `_system.annotation_target` field
  - [ ] 13.2 Add cascade deletion tests: MU with 2 annotations deleted — verify all 3 MUs cleaned from all backends
- [ ] Task 14: Integration tests (AC: #1, #2, #3, #4)
  - [ ] 14.1 Create annotation roundtrip: ingest MU, POST annotation, verify 202, verify annotation MU exists in Redis
  - [ ] 14.2 Create correction annotation: POST with `annotationType: "correction"`, verify metadata field `annotation_type` on stored MU
  - [ ] 14.3 List annotations: create 2 annotations on same MU, GET annotations endpoint, verify 2 returned
  - [ ] 14.4 Search enrichment: ingest MU, create annotation, **poll workflow status until `indexed`**, then search, verify `annotationsCount: 1` in result (avoid flaky test — do not search before workflow completes)
  - [ ] 14.5 Cascade deletion: ingest MU, create annotation, delete MU, verify annotation MU also deleted from Redis
  - [ ] 14.6 Annotation on non-existent MU: POST annotation for unknown MU, verify 404
  - [ ] 14.7 Annotation on wrong case: create MU in case A, POST annotation via case B, verify 404
  - [ ] 14.8 Nested annotation rejected: create MU, create annotation on MU, attempt annotation on the annotation, verify 400 with `NESTED_ANNOTATION_NOT_ALLOWED`
  - [ ] 14.9 Annotation on non-indexed MU: ingest MU but intercept before indexing completes (or use a known-failed MU), POST annotation, verify 400 with `MEMORY_UNIT_NOT_INDEXED`

## Dev Notes

### Implementation Order

Task 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10-14 (tests in parallel). Contracts first (1-2), then graph queries (3), then validation (4), then service logic (5-7), then endpoints (8-9), then all tests.

### Architecture Decision: Annotations as MemoryUnits with Workflow Reuse

Annotations are regular MemoryUnits that go through the existing `IngestionWorkflow`. This means:
1. Annotations get their own embeddings — independently searchable via semantic axis
2. Annotations get indexed in RediSearch — independently searchable via syntactic axis
3. Annotations get a FalkorDB node — can be traversed via graph axis
4. The only additional graph operation is creating the `ANNOTATES` edge after scheduling the workflow

**Why reuse IngestionWorkflow instead of a separate AnnotationWorkflow:**
- Annotations need the same pipeline: validation, extraction (trivial for text), embedding, three-backend indexing, consistency verification
- Avoids duplicating workflow infrastructure
- Annotation-specific metadata (`_system.annotation_target`, `_system.annotation_type`) is carried via the `Metadata` dictionary — no schema changes needed
- The `CausationId` field links the annotation to its target for tracing
- The `SourceUri` = `annotation:{targetMuId}` distinguishes annotations from ingested content

**What happens during annotation creation:**
1. Validate inputs + verify target MU exists, belongs to specified case, and has status `Indexed`
2. Verify target MU is not itself an annotation (reject nested annotations)
3. Generate annotation MU ID (ULID)
4. Create stub node + `ANNOTATES` edge in FalkorDB (before workflow scheduling)
5. Schedule `IngestionWorkflow` with annotation content + metadata (try/catch — cleanup stub on failure)
6. Record `AnnotationCreated` activity event
7. Return 202 with annotation MU ID and workflow instance ID (status: Queued)

**Edge creation timing:** The `ANNOTATES` edge references the annotation MU ID. Since `BuildMergeEdge` uses `MATCH` (not `MERGE` for nodes), the edge creation must happen after the workflow's `IndexGraphActivity` creates the annotation node. Two approaches:
- **Option A (recommended):** Create a stub node via `BuildMergeStubNode(annotationMuId)` immediately, then create the edge. The workflow's `BuildMergeMemoryUnitNode` will update the stub with full metadata later.
- **Option B:** Add the edge creation as a post-indexing step in the endpoint (poll workflow status). More complex, not needed for MVP.

Use **Option A**: `BuildMergeStubNode` + `BuildMergeEdge` in the service method, before scheduling the workflow.

**Compensation guard:** If the workflow fails, the existing `CleanupGraphActivity` runs `DETACH DELETE` on the annotation MU node — this removes both the stub node AND the `ANNOTATES` edge. No orphan nodes or edges persist after workflow failure. Verify this compensation path is wired during implementation (Task 5.10).

### Authorization: No Case Membership Check at MVP

Annotation creation does NOT check case membership for MVP. This is consistent with all other case endpoints (create case, add member, search, delete) which also skip authorization checks. Authorization enforcement is deferred to the tenant isolation story (Epic 5). If this decision changes, add a `CaseService.IsMemberAsync` check in the endpoint before calling `CreateAnnotationAsync`.

### Nested Annotations Are Rejected

Annotations on annotations are NOT allowed. Task 4.2 validates that the target MU's metadata does not contain an `annotation_target` key. If it does, the target is itself an annotation and the request is rejected with `NESTED_ANNOTATION_NOT_ALLOWED`. This prevents unbounded annotation chains and keeps the graph structure flat: original MU -> annotation(s), never annotation -> annotation.

### Corrections Are Metadata, Not a Separate Type

Corrections are annotations with `_system.annotation_type: "correction"` in their metadata. No separate contract type needed. The metadata distinguishes them:

```csharp
// Correction annotation metadata — all keys use _system. prefix (reserved namespace)
new Dictionary<string, MetadataField>
{
    ["_system.annotation_target"] = new MetadataField(targetMemoryUnitId, MetadataOrigin.Human, 1.0f),
    ["_system.annotation_type"] = new MetadataField("correction", MetadataOrigin.Human, 1.0f),
}
```

The `_system.` prefix is a reserved namespace for server-generated metadata. This prevents collisions with user-supplied metadata on ingested content (e.g., a document that legitimately has an "annotation_target" field). The nested annotation check (Task 4.2) looks for `_system.annotation_target` — only server-generated annotation MUs will have this key.

`CreateAnnotationInput` intentionally has NO `Metadata` field. All annotation metadata is built server-side in Task 5.5 to prevent user override of `_system.*` keys.

The original memory unit is NEVER modified — corrections are additive.

### Cascade Deletion Strategy

Two scenarios:

**Individual MU deletion (Story 3.5 path):**
1. Before deleting target MU, query `BuildListAnnotationIds` to find annotations
2. Delete each annotation MU from all 3 backends (Redis hash, Redis vector, FalkorDB node)
3. Then delete target MU (existing `DETACH DELETE` cleans up `ANNOTATES` edges from graph)

**Case deletion (Story 3.5 path):**
- Already works: `BuildListCaseMemoryUnitIds` returns ALL MUs in the case via `CONTAINS` edges
- Annotations share the same `caseId` as their target, so they are included in the case's MU list
- The case deletion loop deletes all of them — no code change needed for case deletion

### Search Enrichment: Batch Annotation Counts

Follow the `EnrichResultWithCaseAttributionAsync` pattern (Program.cs:949-986):
1. Collect all `MemoryUnitId` values from search results
2. Execute single batch query via `BuildBatchCountAnnotations` — **always use batch, never per-result queries** (50 results = 50 round-trips is unacceptable)
3. Use `with` expression to set `AnnotationsCount` on each `ScoredResult`

Batch Cypher (implemented in `BuildBatchCountAnnotations`):
```cypher
UNWIND $ids AS muId
OPTIONAL MATCH (a:MemoryUnit)-[:ANNOTATES]->(m:MemoryUnit {id: muId})
RETURN muId, count(a) AS count
```

### Verified: `caseId` Field Exists in MU Redis Hashes

The target MU lookup in Task 5.2 (`HashGetAsync({tenantId}:mu:{targetMemoryUnitId}, "caseId")`) is safe. Both `IndexSyntacticActivity.cs:83` and `IndexSemanticActivity.cs:88` store `caseId` as a `HashEntry` in the MU hash. No additional lookups needed.

### Independent Annotation Deletion

Deleting a single annotation (not via cascade) is already supported by the existing Story 3.5 `DELETE /api/tenants/{tid}/cases/{cid}/memory-units/{muId}` endpoint. Annotation MUs have a `caseId` and are regular MUs — no additional code needed. The `DETACH DELETE` on the annotation node removes its `ANNOTATES` edge automatically.

### Contradictory Corrections Are Intentional

Multiple users can create contradictory corrections on the same MU (e.g., "date was Feb 14" vs "date was Feb 16"). Both are preserved with `_system.annotation_type: correction` and confidence 1.0. The system does not resolve conflicts — it preserves all human input. The consumer sees both annotations and decides which to trust. This is a feature, not a bug.

### Known Limitations (MVP)

1. **Cascade deletion race condition:** Between querying `BuildListAnnotationIds` and deleting annotations, a new annotation could be created on the target MU. The new annotation becomes an orphan (stub node + edge in FalkorDB, Redis keys for the annotation MU). At MVP scale with single-developer usage, this is acceptable. DAPR Workflow orchestration in Epic 6 will address this with proper locking.
2. **No authorization check:** Annotation creation does not verify case membership (consistent with all other endpoints — deferred to Epic 5).
3. **No CLI integration:** Annotation creation is API-only. CLI command (e.g., `memories annotate <muId> --correction "text"`) is deferred to Epic 7 (CLI & Developer Experience).
4. **No annotation type breakdown in search results:** `annotationsCount` is a single integer. A richer structure (`{ total: 3, corrections: 1, clarifications: 2 }`) is a future enhancement — requires extending the batch Cypher query to group by `_system.annotation_type` metadata.

### Key Files to Create

| File | Purpose |
|------|---------|
| `Contracts/V1/CreateAnnotationInput.cs` | Input record for annotation creation |

### Key Files to Modify

| File | Change |
|------|--------|
| `Contracts/V1/CaseActivityEventType.cs` | Add `AnnotationCreated` enum value |
| `Contracts/V1/SourceType.cs` | Add `Annotation` enum value |
| `Contracts/V1/ScoredResult.cs` | Add `AnnotationsCount` property |
| `Contracts/V1/MemoriesJsonContext.cs` | Register `CreateAnnotationInput` |
| `Server/Graph/IGraphQueryBuilder.cs` | Add `BuildCountAnnotations`, `BuildListAnnotationIds`, `BuildBatchCountAnnotations` |
| `Server/Graph/GraphQueryBuilder.cs` | Implement the three new methods |
| `Server/Cases/CaseValidator.cs` | Add `ValidateCreateAnnotation` method |
| `Server/Cases/CaseService.cs` | Add `CreateAnnotationAsync`, `ListAnnotationsAsync` methods |
| `Server/Program.cs` | Add `POST` and `GET` annotation endpoints, add `EnrichResultWithAnnotationCountsAsync`, wire into search paths |

### Existing Infrastructure Already Ready

- `EdgeType.Annotates` — exists in `Contracts/V1/EdgeType.cs:13`
- `EdgeTypeDefaults.Annotates = 1.0f` — exists in `Contracts/V1/EdgeTypeDefaults.cs:10`
- `GraphQueryBuilder.BuildMergeEdge()` — already handles `Annotates` via `ToUpperSnakeCase` switch (line 240) and `GetNodeLabels` (line 247)
- `GraphQueryBuilder.BuildMergeStubNode()` — creates placeholder node for edge attachment (lines 145-157)
- `GraphQueryBuilder.BuildDeleteMemoryUnitNode()` — uses `DETACH DELETE` which removes all incident edges including `ANNOTATES` (line 179)
- `IngestionWorkflow` — reusable for annotation content (text in, embeddings + indexing out)
- `CaseActivityService.RecordEventAsync()` — records events to case activity stream
- `MetadataField` record with `Origin` and `Confidence` — perfect for annotation metadata
- `MetadataOrigin.Human` — annotation metadata origin

### Project Structure Notes

- Annotation contracts go in `Contracts/V1/` alongside existing contracts (flat namespace, no subfolder)
- Service methods added to existing `CaseService` (annotations are a case feature, not a separate service)
- Graph queries added to existing `GraphQueryBuilder` (following established pattern)
- Validation added to existing `CaseValidator` (following established pattern)
- Endpoints added to `Program.cs` (Minimal API pattern, same file as all other endpoints)
- No new projects, no new services, no new actors needed

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.6] — AC definitions, edge type specs
- [Source: _bmad-output/planning-artifacts/prd.md#FR37] — Annotations tracked as linked memory units
- [Source: _bmad-output/planning-artifacts/prd.md#FR50] — Edge type taxonomy, `annotates` confidence 1.0
- [Source: _bmad-output/planning-artifacts/architecture.md#D9] — Cypher injection prevention via IGraphQueryBuilder
- [Source: src/Hexalith.Memories.Contracts/V1/EdgeType.cs:13] — `Annotates` enum already defined
- [Source: src/Hexalith.Memories.Contracts/V1/EdgeTypeDefaults.cs:10] — `Annotates = 1.0f` already defined
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:240] — `ANNOTATES` Cypher label mapping
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:145-157] — `BuildMergeStubNode` for stub creation
- [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs:50-98] — `CreateCaseAsync` pattern to follow
- [Source: src/Hexalith.Memories.Server/Program.cs:949-986] — `EnrichResultWithCaseAttributionAsync` enrichment pattern
- [Source: _bmad-output/implementation-artifacts/3-5-memory-unit-deletion-and-case-deletion.md] — Deletion architecture, synchronous with status guard

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
