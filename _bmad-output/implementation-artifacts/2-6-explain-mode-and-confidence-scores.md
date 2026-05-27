# Story 2.6: Explain Mode & Confidence Scores

Status: done

## Prerequisites

- Story 2.5 (Fusion Algorithm & Hybrid Search) must be `done` — this story adds explain metadata to the hybrid search infrastructure built in 2.5

## Story

As a developer,
I want to see per-axis score breakdowns and composite confidence scores for each search result, with pagination support,
So that I understand why each result appeared and can debug relevance issues.

## Acceptance Criteria

1. **Given** a search query with explain mode enabled (`explain=true`)
   **When** results are returned
   **Then** each result includes: composite confidence score (0.0-1.0), per-axis breakdown (syntactic score, semantic score, graph score), and the normalization method applied per axis (FR19, FR63)

2. **Given** explain mode output
   **When** I inspect the response
   **Then** the response-level `caveat` field is included with the value: "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness"

3. **Given** a search query returns more results than the page size
   **When** I request paginated results (FR22)
   **Then** results are returned with total count and pagination metadata (`offset`, `maxResults`, `totalCount`)
   **And** pagination preserves score ordering across pages

4. **Given** a search result
   **When** I inspect the origin information
   **Then** it includes `SourceUri` (file path, URL, or event ID) and `SourceType` (FR24)

## Tasks / Subtasks

- [x] Task 1: Create `SearchExplanation` record in Contracts (AC: 1, 2)
    - [x] 1.1 Create `src/Hexalith.Memories.Contracts/V1/SearchExplanation.cs` as `public sealed record` with properties:
        - `string Caveat` — the confidence caveat string (always the standard message when explain=true)
        - `IReadOnlyDictionary<string, AxisExplanation> AxisDetails` — keyed by axis name ("syntactic", "semantic", "graph"), describes normalization for each active axis
        - `FusionWeights? WeightsUsed` — the fusion weights applied (only set for hybrid search; null for single-axis)
    - [x] 1.2 Create `AxisExplanation` record in the same file with properties:
        - `string NormalizationMethod` — machine-readable name (e.g., `"bm25_saturation"`, `"cosine_clamp"`, `"inverse_hop_decay"`)
        - `string Description` — human-readable one-liner (e.g., `"BM25 saturation normalization: score / (score + k), where k adapts to corpus size and average document length"`)
    - [x] 1.3 Add `SearchExplanation` and `AxisExplanation` to `MemoriesJsonContext` (`[JsonSerializable]` attributes)
    - [x] 1.4 Also add `Dictionary<string, AxisExplanation>` (the **concrete** type, not `IReadOnlyDictionary`) to `MemoriesJsonContext` for AOT serialization — `System.Text.Json` source generators need the concrete type for correct AOT code generation

- [x] Task 2: Add `Explanation` property to response types (AC: 1, 2)
    - [x] 2.1 Add `public SearchExplanation? Explanation { get; init; }` to `HybridSearchResult` — optional, null when explain=false. **CRITICAL:** Annotate with `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` to ensure `null` values are **omitted** from JSON output (not serialized as `"explanation": null`). Without this attribute, `System.Text.Json` default behavior writes `null`, which breaks backward compatibility for strict-schema consumers
    - [x] 2.2 Add `public SearchExplanation? Explanation { get; init; }` to `SearchResult` — same pattern with same `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` attribute (enables explain for single-axis searches too)

- [x] Task 3: Create `ExplainMetadataBuilder` static helper (AC: 1, 2)
    - [x] 3.1 Create `src/Hexalith.Memories.Server/Search/ExplainMetadataBuilder.cs` as `internal static class`
    - [x] 3.2 Implement constant fields for normalization metadata:
        ```csharp
        internal const string Caveat = "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness";
        ```
    - [x] 3.3 Implement `BuildForHybrid(IReadOnlySet<string> activeAxes, FusionWeights weights)` returning `SearchExplanation`:
        - Build `AxisDetails` dictionary for each active axis with its normalization method:
            - `"syntactic"` → `NormalizationMethod = "bm25_saturation"`, `Description = "BM25 saturation normalization: score / (score + k), where k = log2(docCount + 1) * (avgDocLen / 100)"`
            - `"semantic"` → `NormalizationMethod = "cosine_clamp"`, `Description = "Cosine similarity in [0.0, 1.0] with defensive clamp (Redis vector already returns similarity)"`
            - `"graph"` → `NormalizationMethod = "inverse_hop_decay"`, `Description = "Inverse hop distance with decay: 1.0 / (1.0 + hopDistance)"`
        - **SYNC WARNING:** Add a code comment at the top of the normalization descriptions block: `// Descriptions must stay in sync with ScoreNormalizer methods. Update here when normalization formulas change.`
        - **EXTENSIBILITY:** Store axis→explanation mappings in a `private static readonly Dictionary<string, AxisExplanation>` field rather than inline construction. This makes adding a future 4th axis (e.g., "temporal") a single dictionary entry addition instead of scattered if-else changes
        - Set `WeightsUsed = weights`
        - Set `Caveat` to the constant
    - [x] 3.4 Implement `BuildForSingleAxis(string axisName)` returning `SearchExplanation`:
        - Build `AxisDetails` with only the single axis
        - Set `WeightsUsed = null` (no fusion in single-axis mode)
        - Set `Caveat` to the constant

- [x] Task 4: Wire `explain` query parameter into `/api/search` endpoint (AC: 1, 2, 3)
    - [x] 4.1 Add `[FromQuery] bool explain = false` parameter to the endpoint delegate in `Program.cs`
    - [x] 4.2 In the `axis=hybrid` branch: when `explain == true`, call `ExplainMetadataBuilder.BuildForHybrid(enabledAxes, weights)` and set it on the `HybridSearchResult.Explanation` property before returning
    - [x] 4.3 In the `axis=syntactic` branch: when `explain == true`, call `ExplainMetadataBuilder.BuildForSingleAxis("syntactic")` and set it on `SearchResult.Explanation` before returning
    - [x] 4.4 In the `axis=semantic` branch: same pattern with `"semantic"`
    - [x] 4.5 In the `axis=graph` branch: same pattern with `"graph"`
    - [x] 4.6 When `explain == false` (default): leave `Explanation = null` — no overhead, no extra serialization, backward-compatible

- [x] Task 5: Unit tests for `ExplainMetadataBuilder` (AC: 1, 2)
    - [x] 5.1 Create `tests/Hexalith.Memories.Server.Tests/Search/ExplainMetadataBuilderTests.cs`
    - [x] 5.2 Test: `BuildForHybrid` with all three axes → `AxisDetails` has 3 entries with correct normalization methods
    - [x] 5.3 Test: `BuildForHybrid` with `{"syntactic", "semantic"}` → only 2 axis entries, no graph
    - [x] 5.4 Test: `BuildForHybrid` sets `WeightsUsed` to the provided weights
    - [x] 5.5 Test: `BuildForHybrid` sets `Caveat` to the exact standard message
    - [x] 5.6 Test: `BuildForSingleAxis("syntactic")` → single entry, `WeightsUsed` is null, `Caveat` is set
    - [x] 5.7 Test: `BuildForSingleAxis("semantic")` → correct normalization method name
    - [x] 5.8 Test: `BuildForSingleAxis("graph")` → correct normalization method name
    - [x] 5.9 Test: `Caveat` constant matches exact wording from PRD

- [x] Task 6: Contract serialization tests (AC: 1)
    - [x] 6.1 Create `tests/Hexalith.Memories.Contracts.Tests/V1/SearchExplanationSerializationTests.cs`
    - [x] 6.2 Test: `SearchExplanation` round-trip serialization with all fields populated
    - [x] 6.3 Test: `AxisExplanation` round-trip serialization
    - [x] 6.4 Test: `SearchExplanation` with `WeightsUsed = null` (single-axis mode) serializes and deserializes correctly
    - [x] 6.5 Test: camelCase property naming in serialized JSON
    - [x] 6.6 Test: `HybridSearchResult` with non-null `Explanation` serializes correctly (new field doesn't break existing consumers)
    - [x] 6.7 Test: `SearchResult` with non-null `Explanation` serializes correctly
    - [x] 6.8 Test: `HybridSearchResult` with `Explanation = null` produces same JSON shape as before (backward compatibility) — verify `"explanation"` key is **completely absent** from serialized JSON, not present as `null`
    - [x] 6.9 Test: `SearchResult` with `Explanation = null` produces same JSON shape as before (backward compatibility) — same omission check as 6.8

- [x] Task 7: Integration test for explain endpoint (AC: 1, 2, 3, 4)
    - [x] 7.1 Create `tests/Hexalith.Memories.Server.Tests/Search/ExplainEndpointTests.cs` (or add to existing search endpoint tests)
    - [x] 7.2 Test: `GET /api/search?axis=hybrid&explain=true&tenantId=t1&query=test` → response has non-null `Explanation` with correct caveat
    - [x] 7.3 Test: `GET /api/search?axis=hybrid&explain=false&tenantId=t1&query=test` → response has null `Explanation`
    - [x] 7.4 Test: `GET /api/search?axis=syntactic&explain=true&tenantId=t1&query=test` → response has `Explanation` with single axis detail
    - [x] 7.5 Test: pagination metadata is present in response (totalCount) and explain does not affect pagination behavior

## Dev Notes

### Implementation Overview

This story adds **explain mode** to the existing search endpoints — a diagnostic decorator that reveals _how_ each result was scored. The core infrastructure (fusion, per-axis scores, normalization) was built in Stories 2.1-2.5. This story exposes that information to the API consumer.

**Explain mode is a response decorator, NOT a separate query path** (architecture decision — ensures Phase 4 ACL filtering applies equally to results and explain metadata).

### What Already Exists (Do NOT Rebuild)

The following are fully implemented and should be used as-is:

1. **`FusedScoredResult`** (`Contracts/V1/HybridSearchResult.cs`) — already stores `CompositeScore`, `SyntacticScore?`, `SemanticScore?`, `GraphScore?`. These ARE the per-axis breakdown. Story 2.6 does NOT need to add more per-result score fields. It only needs to add the normalization _method names_ at the response level.

2. **`FusionEngine.Fuse()`** (`Server/Search/FusionEngine.cs`) — pure fusion function, produces `FusedScoredResult` with all per-axis scores already populated. No changes needed.

3. **`HybridSearchService`** (`Server/Search/HybridSearchService.cs`) — orchestrates parallel axis calls and returns `HybridSearchResult` with pagination. No changes to core logic needed — only add the `Explanation` field to the returned object.

4. **`ScoreNormalizer`** (`Server/Search/ScoreNormalizer.cs`) — the three normalization methods:
    - `NormalizeBm25(rawScore, docCount, avgDocLen)` — saturation normalization: `score / (score + k)` where `k = log2(docCount+1) * (avgDocLen/100)`
    - `NormalizeCosine(cosineScore)` — defensive clamp to [0.0, 1.0]
    - `NormalizeGraphProximity(hopDistance)` — inverse hop decay: `1.0 / (1.0 + hopDistance)`

5. **Pagination** — already implemented in `HybridSearchService` (offset-based: skip `query.Offset`, take `query.MaxResults`, return `TotalCount`). No changes needed.

6. **Origin info** — `SourceUri` and `SourceType` already present on both `ScoredResult` and `FusedScoredResult`.

### Design: Explain as Response-Level Metadata

Per-axis normalization methods are **constant per axis** — every BM25 score uses the same normalization formula. So normalization method names belong at the **response envelope level**, not per-result. This avoids repeating identical strings 10+ times per response.

```
// When explain=false (default):
HybridSearchResult {
  Results: [{ CompositeScore: 0.82, SyntacticScore: 0.75, SemanticScore: 0.91, ... }],
  TotalCount: 42,
  Explanation: null  ← omitted from JSON
}

// When explain=true:
HybridSearchResult {
  Results: [{ CompositeScore: 0.82, SyntacticScore: 0.75, SemanticScore: 0.91, ... }],
  TotalCount: 42,
  Explanation: {
    caveat: "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness",
    axisDetails: {
      "syntactic": { normalizationMethod: "bm25_saturation", description: "..." },
      "semantic":  { normalizationMethod: "cosine_clamp", description: "..." }
    },
    weightsUsed: { syntacticWeight: 0.4, semanticWeight: 0.4, graphWeight: 0.2 }
  }
}
```

For single-axis searches (`axis=syntactic|semantic|graph`), explain mode adds the same `Explanation` field to `SearchResult` — but with only one axis in `axisDetails` and `weightsUsed = null`.

### Backward Compatibility

Adding optional `Explanation` property to existing `HybridSearchResult` and `SearchResult` is backward-compatible:

- When `explain=false` (default): `Explanation` is null → **omitted** from JSON serialization via `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`
- **CRITICAL:** Without this attribute, `System.Text.Json` serializes `null` as `"explanation": null`, which changes the JSON shape and can break strict-schema consumers. The attribute ensures the key is completely absent when explain is off
- Existing API consumers receive the exact same JSON shape as before
- No new required fields, no schema-breaking changes

### Security Consideration (Growth Phase)

Explain mode exposes `FusionWeights` — the exact algorithmic weights used for ranking. A tenant could theoretically reverse-engineer how to optimize content for the highest-weighted axis. For MVP thesis validation this is acceptable and intentional (explain mode exists for transparency). **Growth phase:** Consider whether weight exposure needs per-tenant access control or rate limiting on explain queries

### Normalization Method Registry

Use string constants for normalization method names (machine-readable, stable across API versions):

| Axis      | `NormalizationMethod` | `Description`                                                                                            |
| --------- | --------------------- | -------------------------------------------------------------------------------------------------------- |
| Syntactic | `"bm25_saturation"`   | `"BM25 saturation normalization: score / (score + k), where k = log2(docCount + 1) * (avgDocLen / 100)"` |
| Semantic  | `"cosine_clamp"`      | `"Cosine similarity in [0.0, 1.0] with defensive clamp (Redis vector already returns similarity)"`       |
| Graph     | `"inverse_hop_decay"` | `"Inverse hop distance with decay: 1.0 / (1.0 + hopDistance)"`                                           |

### Endpoint Changes in Program.cs

The `/api/search` endpoint (line 209 of `Program.cs`) currently accepts these params:

```
tenantId, query, caseId, maxResults, offset, axis, axes, startNodeId, depth
```

Add: `[FromQuery] bool explain = false`

In each axis branch, after getting the result but before returning:

```csharp
if (explain)
{
    result = result with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("syntactic") };
}
return Results.Ok(result);
```

For hybrid:

```csharp
if (explain)
{
    hybridResult = hybridResult with { Explanation = ExplainMetadataBuilder.BuildForHybrid(enabledAxes, weights) };
}
return Results.Ok(hybridResult);
```

The `with` expression works cleanly because both are `record` types.

### What This Story Does NOT Implement

- **CLI `--explain` output** — CLI is Epic 7. This story adds the API-level explain data that the CLI will consume.
- **Per-tenant weight tuning** — Growth-phase feature. Weights are hardcoded defaults (0.4/0.4/0.2).
- **Cursor-based pagination** — The AC says "total count, page number, and next page token" but the existing infrastructure uses offset-based pagination. The story adds `Explanation` without changing the pagination model. Offset + maxResults + totalCount is sufficient for MVP. If cursor-based is needed later, it's a separate enhancement.
- **Metadata confidence** (origin tracking per metadata field) — That's FR64, tracked separately. This story covers search result confidence only.
- **Raw pre-normalization scores in explain output** — `FusedScoredResult.SyntacticScore` is already normalized. Exposing raw BM25/cosine/graph scores alongside normalized values would help developers diagnose whether the normalizer or the search backend is responsible for unexpected rankings. Deferred to a future enhancement or debug-verbose explain tier.

### Project Structure Notes

New files follow existing conventions:

```
src/Hexalith.Memories.Contracts/V1/
  SearchExplanation.cs                # NEW — explain mode metadata types

src/Hexalith.Memories.Server/Search/
  ExplainMetadataBuilder.cs           # NEW — builds SearchExplanation for each search mode

tests/Hexalith.Memories.Server.Tests/Search/
  ExplainMetadataBuilderTests.cs      # NEW — unit tests for builder

tests/Hexalith.Memories.Contracts.Tests/V1/
  SearchExplanationSerializationTests.cs  # NEW — serialization round-trip tests
```

Modified files:

```
src/Hexalith.Memories.Contracts/V1/
  HybridSearchResult.cs               # ADD optional Explanation property
  SearchResult.cs                      # ADD optional Explanation property
  MemoriesJsonContext.cs               # ADD new type registrations

src/Hexalith.Memories.Server/
  Program.cs                           # ADD explain parameter + wiring
```

All files use:

- ITANEO copyright header
- File-scoped namespace: `namespace Hexalith.Memories.{Project}.{Folder};`
- `sealed` classes (no inheritance)
- `internal` for implementation classes, `public` for contracts/interfaces

### Testing Standards

- **Framework:** xUnit 2.9.3
- **Assertions:** Shouldly (`result.ShouldBe(expected)`, `result.ShouldNotBeNull()`)
- **Serialization tests:** Follow `ScoredResultSerializationTests.cs` pattern — serialize with `MemoriesJsonContext.Options`, deserialize, verify equality and camelCase property names
- **No mocking needed for `ExplainMetadataBuilder`** — pure static functions with no I/O
- **Endpoint tests:** If using WebApplicationFactory, test the full HTTP request/response cycle. Otherwise, test the builder in isolation and verify wiring via inspection of Program.cs logic

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.6 — lines 733-757]
- [Source: _bmad-output/planning-artifacts/prd.md#FR19 (per-axis score breakdown with normalization method)]
- [Source: _bmad-output/planning-artifacts/prd.md#FR22 (pagination)]
- [Source: _bmad-output/planning-artifacts/prd.md#FR24 (origin identifier: SourceUri, SourceType)]
- [Source: _bmad-output/planning-artifacts/prd.md#FR63 (composite confidence scores with per-axis breakdowns)]
- [Source: _bmad-output/planning-artifacts/prd.md#lines 454-471 (confidence score semantics, caveat requirement)]
- [Source: _bmad-output/planning-artifacts/architecture.md#line 207 (explain as decorator on search results, not separate path)]
- [Source: _bmad-output/planning-artifacts/architecture.md#lines 86-94 (fusion algorithm, BM25 normalization, NFR24-26)]
- [Source: src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs — FusedScoredResult with per-axis scores]
- [Source: src/Hexalith.Memories.Contracts/V1/SearchResult.cs — single-axis response envelope]
- [Source: src/Hexalith.Memories.Server/Search/ScoreNormalizer.cs — normalization functions]
- [Source: src/Hexalith.Memories.Server/Search/FusionEngine.cs — pure fusion function]
- [Source: src/Hexalith.Memories.Server/Search/HybridSearchService.cs — hybrid orchestrator]
- [Source: src/Hexalith.Memories.Server/Program.cs — lines 209-355 (search endpoint routing)]
- [Source: src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs — AOT serialization context]

### Previous Story Intelligence (Story 2.5)

From Story 2.5 implementation:

- `FusionEngine.Fuse()` is a pure static function — returns `IReadOnlyList<FusedScoredResult>` with per-axis scores already populated. Do NOT add explain logic inside `FusionEngine` — it stays pure. Explain is a decorator applied by the caller
- `HybridSearchService` accepts `Func<>` delegates for each axis (not direct service references). Tests use lambda stubs. The explain functionality is NOT in `HybridSearchService` — it's applied in `Program.cs` endpoint after getting the result
- `FusedScoredResult` already carries `SyntacticScore?`, `SemanticScore?`, `GraphScore?` — these are the per-axis breakdown. Story 2.6 does NOT need to add per-result fields, only response-level normalization method metadata
- `HybridSearchService.FindInvalidAxis()` validates axis names — reuse for any explain-related axis validation
- All Story 2.5 types (`FusedScoredResult`, `HybridSearchResult`, `FusionWeights`) are already registered in `MemoriesJsonContext`
- Keyed DI: `[FromKeyedServices("redis")]` for Redis, `[FromKeyedServices("falkordb")]` for FalkorDB
- `sealed partial class` with `[LoggerMessage]` for structured logging (use this pattern if adding any logging)

### Git Intelligence

Recent commits show the search axis implementation trajectory:

- `2ecbbaf` feat: Implement CorpusStatistics actor for caching per-tenant RediSearch statistics
- `81057a3` feat: Implement GraphScopedSearch for traversing FalkorDB and enriching results from Redis
- `5c39312` feat: Implement Semantic Search Service with KNN vector search capabilities
- `0d104b7` feat: Implement Syntactic Search Service with BM25 ranking and related data models

Pattern: Each search service is a standalone `internal sealed class` registered as singleton via explicit factory in `Program.cs`. New utility classes (like `ExplainMetadataBuilder`) follow the `internal static class` pattern used by `ScoreNormalizer` and `FusionEngine`.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

None — clean implementation with no failures or retries.

### Completion Notes List

- **Task 1:** Created `SearchExplanation` and `AxisExplanation` sealed records in `Contracts/V1/SearchExplanation.cs`. Both types plus the concrete `Dictionary<string, AxisExplanation>` registered in `MemoriesJsonContext` for AOT support.
- **Task 2:** Added optional `Explanation` property with `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` to both `HybridSearchResult` and `SearchResult`. Backward compatible — null values are omitted from JSON.
- **Task 3:** Created `ExplainMetadataBuilder` as `internal static class` with `BuildForHybrid` and `BuildForSingleAxis` methods. Axis explanations stored in a static dictionary for extensibility. Sync warning comment added per spec.
- **Task 4:** Wired `[FromQuery] bool explain = false` into the `/api/search` endpoint. All 6 return paths (graph pure, hybrid, graph+semantic inner, graph+syntactic inner, semantic standalone, syntactic default) decorated with explain metadata when `explain=true`.
- **Task 5:** 8 unit tests for `ExplainMetadataBuilder` — all pass. Covers all axis combinations, weight propagation, caveat wording, and single-axis modes.
- **Task 6:** 8 serialization tests — all pass. Covers round-trip, camelCase naming, null WeightsUsed omission, backward compatibility (explanation key absent when null), and HybridSearchResult/SearchResult with Explanation.
- **Task 7:** Added end-to-end explain coverage in `tests/Hexalith.Memories.IntegrationTests/Search/ExplainSearchApiIntegrationTests.cs`. The suite now exercises default syntactic, semantic, pure graph, graph-scoped syntactic, graph-scoped semantic, hybrid explain on/off, and hybrid skipped-axis explanation behavior against the real Aspire topology.
- **Review fixes:** Hybrid explain metadata now derives its axis list from the axes that actually executed, removing skipped or unavailable axes before serializing `Explanation.AxisDetails`.

**Test results:** 24 targeted explain tests pass (8 integration + 16 unit/serialization).

### File List

New files:

- `src/Hexalith.Memories.Contracts/V1/SearchExplanation.cs`
- `src/Hexalith.Memories.Server/Search/ExplainMetadataBuilder.cs`
- `tests/Hexalith.Memories.IntegrationTests/Search/ExplainSearchApiIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/ExplainMetadataBuilderTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/SearchExplanationSerializationTests.cs`

Modified files:

- `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`
- `src/Hexalith.Memories.Contracts/V1/SearchResult.cs`
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`
- `src/Hexalith.Memories.Server/Program.cs`

### Change Log

- 2026-04-01: Implemented explain mode and confidence scores (Story 2.6) — added SearchExplanation/AxisExplanation contracts, ExplainMetadataBuilder helper, wired explain query parameter into all search endpoint branches, 16 new tests (8 unit + 8 serialization)
- 2026-04-02: Applied code-review fixes — added explain-mode API integration coverage and aligned hybrid explanation axes with actual executed axes

### Review Findings

- [x] [Review][Patch] Add `/api/search` explain-mode integration coverage for the changed HTTP branches [src/Hexalith.Memories.Server/Program.cs:222]
- [x] [Review][Patch] Build hybrid explanation metadata from axes that actually executed, not just requested axes [src/Hexalith.Memories.Server/Program.cs:368]
- [x] [Review][Defer] Return `offset` and `maxResults` pagination metadata in search response envelopes [src/Hexalith.Memories.Contracts/V1/SearchResult.cs:1] — deferred, pre-existing
