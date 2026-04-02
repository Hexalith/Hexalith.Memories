# Story 2.7: Benchmark Suite & Thesis Validation

Status: ready-for-dev

## Prerequisites

- Story 2.5 (Fusion Algorithm & Hybrid Search) must be `done` — benchmark compares hybrid vs single-axis using the fusion infrastructure
- Story 2.4 (Score Normalization) must be `done` — NDCG@10 scoring relies on normalized scores
- Stories 2.1-2.3 must be `done` — each single-axis search is tested independently against hybrid

## Story

As a developer,
I want to run automated benchmark comparisons of hybrid vs single-axis search results,
So that I can validate the three-axis thesis with reproducible, scored evidence.

## Acceptance Criteria

1. **Given** a synthetic benchmark dataset with known relationships and controlled vocabulary (D11)
   **When** the dataset is loaded
   **Then** it contains sufficient memory units with defined ground truth results for each benchmark query
   **And** relationships (causal edges, correlations, references) are pre-defined for graph axis testing

2. **Given** 5-10 benchmark queries that require all three axes
   **When** each query is executed in hybrid mode and in each single-axis mode
   **Then** results are scored by NDCG@10 (Normalized Discounted Cumulative Gain at rank 10) against ground truth

3. **Given** the benchmark suite is run twice against the same dataset
   **When** NDCG@10 scores are computed
   **Then** identical scores are produced (NFR26: reproducible)

4. **Given** benchmark results for hybrid vs single-axis
   **When** the comparison is evaluated
   **Then** the output clearly shows: hybrid NDCG@10 score, each single-axis NDCG@10 score, and whether hybrid outperforms on each query (FR25)
   **And** the 80% threshold (hybrid outperforms single-axis on 80%+ of benchmarks) is evaluated and reported

5. **Given** the benchmark suite
   **When** it runs in CI
   **Then** it completes within a reasonable time and produces a machine-readable results file
   **And** results include per-query breakdown suitable for analysis

## Tasks / Subtasks

- [ ] Task 1: Create `BenchmarkResult` and `BenchmarkQueryResult` contracts (AC: 2, 4, 5)
  - [ ] 1.1 Create `tests/Hexalith.Memories.Benchmarks/Models/BenchmarkQueryResult.cs` as `public sealed record` with properties:
    - `string QueryId` — unique query identifier (e.g., "BQ-01")
    - `string QueryDescription` — human-readable description of what the query tests
    - `double HybridNdcg10` — NDCG@10 score for hybrid search
    - `double SyntacticNdcg10` — NDCG@10 for syntactic-only
    - `double SemanticNdcg10` — NDCG@10 for semantic-only
    - `double GraphNdcg10` — NDCG@10 for graph-only (0.0 if graph axis was skipped because query has no `GraphStartNodeId`)
    - `bool GraphAxisActive` — true if graph axis was executed for this query (false when `GraphStartNodeId` is null)
    - `double HybridPrecisionAt3` — secondary metric: fraction of top-3 hybrid results that are in ground truth (maps to LLM agent use case — agents read top-3, not top-10)
    - `double BestSingleAxisPrecisionAt3` — highest Precision@3 among active single-axis searches
    - `bool HybridOutperforms` — true if hybrid > max(**active** single-axis scores). Skipped axes (e.g., graph when `GraphAxisActive == false`) are excluded from the comparison — hybrid must beat only the axes that actually ran
  - [ ] 1.2 Create `tests/Hexalith.Memories.Benchmarks/Models/BenchmarkSuiteResult.cs` as `public sealed record` with properties:
    - `IReadOnlyList<BenchmarkQueryResult> QueryResults` — per-query results
    - `int TotalQueries` — total number of benchmark queries
    - `int HybridWins` — count where hybrid outperforms all single-axis
    - `double HybridWinRate` — percentage (HybridWins / TotalQueries)
    - `bool ThesisValidated` — true if HybridWinRate >= 0.80
    - `DateTimeOffset RunTimestamp` — when the suite was executed
    - `string Caveat` — standard caveat message: "Results use synthetic pre-computed vectors, not real embeddings. This validates fusion algorithm correctness, not production search quality. Real-world validation with actual embeddings is planned for Phase 1.5."

- [ ] Task 2: Implement NDCG@10 scoring function (AC: 2, 3)
  - [ ] 2.1 Create `tests/Hexalith.Memories.Benchmarks/Scoring/NdcgScorer.cs` as `internal static class`
  - [ ] 2.2 Implement `ComputeNdcg(IReadOnlyList<string> rankedResults, IReadOnlyList<string> groundTruth, int k = 10) -> double`:
    - Compute DCG@k: `sum(relevance(i) / log2(i + 2))` for `i = 0..k-1` where `relevance(i) = 1.0` if `rankedResults[i]` is in `groundTruth`, else `0.0`. Use binary relevance (present/absent in ground truth) for simplicity
    - Compute IDCG@k: DCG of the ideal ranking (all relevant docs first, up to k)
    - Return DCG / IDCG. If IDCG is 0 (no relevant docs in ground truth), return 0.0
  - [ ] 2.3 Implement `ComputePrecisionAtK(IReadOnlyList<string> rankedResults, IReadOnlyList<string> groundTruth, int k = 3) -> double`:
    - Count how many of the top-k results appear in `groundTruth`
    - Return count / k. If rankedResults has fewer than k items, divide by actual count
    - Secondary metric — maps to LLM agent use case (agents read top-3 results)
  - [ ] 2.4 Both functions are pure: no I/O, no state. All data passed as parameters. Deterministic output for identical inputs (NFR26)

- [ ] Task 3: Create synthetic benchmark corpus (`synthetic-corpus.json`) (AC: 1)
  - [ ] 3.1 Create `tests/Hexalith.Memories.Benchmarks/Data/synthetic-corpus.json` with 30-50 memory units
  - [ ] 3.2 Design the corpus around a realistic scenario (e.g., a software team investigating a production incident):
    - Memory units representing: incident reports, deployment logs, code review comments, architecture decision records, team discussion transcripts, root cause analyses, post-mortem documents
    - Controlled vocabulary: specific terms like "payment processing", "database timeout", "March deployment", "API redesign" that appear in known documents
    - Each memory unit has: `id`, `content` (text), `sourceUri`, `sourceType`, `tenantId` (all same tenant for benchmark), `caseId`
  - [ ] 3.3 Define graph relationships between memory units:
    - `caused_by` edges: incident → root cause, deployment → outage
    - `correlated_with` edges: related discussions, parallel investigations
    - `references` edges: ADRs referencing incidents, reviews referencing code
    - `contains` edges: case → memory units
    - **CRITICAL: Graph connectivity requirement** — the graph must form a connected component for benchmark purposes. Every memory unit must be reachable via at least one edge from at least one other unit. Isolated nodes with no graph edges make the graph axis return empty results, diluting hybrid scores and invalidating the benchmark
  - [ ] 3.4 Include memory units that are ONLY discoverable via specific axes:
    - **Syntactic-only relevant:** Documents with exact keyword matches but no semantic similarity to the query
    - **Semantic-only relevant:** Documents that answer the intent but use different vocabulary (e.g., "claim denied" vs "payment rejection")
    - **Graph-only relevant:** Documents reachable only through graph traversal (not returned by text or vector search)
    - **Multi-axis relevant:** Documents that appear across multiple axes — these should rank higher in hybrid
  - [ ] 3.5 Ensure the corpus is deterministic: no random content generation, all content is hardcoded JSON

- [ ] Task 4: Create ground truth definitions (`ground-truth.json`) (AC: 1, 2)
  - [ ] 4.1 Create `tests/Hexalith.Memories.Benchmarks/Data/ground-truth.json` with 5-10 benchmark queries
  - [ ] 4.2 Each query entry contains:
    - `queryId`: unique identifier (e.g., "BQ-01")
    - `query`: the search query text
    - `description`: what this query tests and why all three axes are needed
    - `expectedResults`: ordered list of memory unit IDs representing ideal ranking (best first)
    - `graphStartNodeId`: optional starting node ID for graph-scoped search (required for queries with graph component; null for queries that don't test graph axis)
    - `requiredAxes`: which axes are needed to find all expected results (for documentation)
  - [ ] 4.3 Design queries that specifically require all three axes for optimal results:
    - **BQ-01:** "What caused the payment processing outage in March?" — requires syntactic ("payment processing", "March"), semantic (meaning of "outage" → related to "downtime", "service disruption"), graph (causal chain from deployment to incident)
    - **BQ-02:** "Find all discussions related to the API redesign decision" — requires syntactic ("API redesign"), semantic (discussions about "architectural changes"), graph (references edges to ADR)
    - **BQ-03:** "What were the consequences of the database migration?" — requires syntactic ("database migration"), semantic (impact/consequences language), graph (caused_by chain from migration event)
    - Plus 2-7 additional queries following the same pattern — each query MUST have at least one ground truth document that is ONLY reachable via graph traversal and at least one that is ONLY reachable via semantic similarity
    - **Adversarial calibration:** At least 2 of the 5-10 queries must be designed so that the best single-axis NDCG@10 is close to hybrid (tight margin). This prevents a benchmark that's trivially won by hybrid and ensures the 80% threshold is meaningful. Example: a query where semantic search alone gets 7/10 relevant docs but hybrid gets 8/10
  - [ ] 4.4 Ground truth ranking must be pre-defined and deterministic (D11)

- [ ] Task 5: Create benchmark data seeding infrastructure (AC: 1)
  - [ ] 5.1 Create `tests/Hexalith.Memories.Benchmarks/Data/BenchmarkCorpusLoader.cs` as `internal static class`
  - [ ] 5.2 Implement `LoadCorpus() -> BenchmarkCorpus` — deserializes `synthetic-corpus.json` into a typed model:
    - `BenchmarkCorpus` record with `IReadOnlyList<BenchmarkMemoryUnit> MemoryUnits` and `IReadOnlyList<BenchmarkEdge> Edges`
    - `BenchmarkMemoryUnit` record with `string Id, string Content, string SourceUri, SourceType SourceType, string TenantId, string CaseId, float[] Vector` — the `Vector` field carries the pre-computed 768-dimensional embedding vector from the corpus JSON, used by the seeder to populate Redis Vector index
    - `BenchmarkEdge` record with `string SourceId, string TargetId, EdgeType EdgeType, EdgeOrigin Origin`
  - [ ] 5.3 Implement `LoadGroundTruth() -> IReadOnlyList<BenchmarkQuery>` — deserializes `ground-truth.json`:
    - `BenchmarkQuery` record with `string QueryId, string Query, string Description, IReadOnlyList<string> ExpectedResults, string? GraphStartNodeId, IReadOnlyList<string> RequiredAxes`
  - [ ] 5.4 Add post-load validation to both loaders:
    - `LoadCorpus()`: verify all memory units have non-empty `Content`, non-empty `Id`, and `Vector` arrays of exactly 768 elements. Also verify no vector is all-zeros (a zero vector has undefined cosine similarity and can cause NaN in distance computations). Throw `InvalidOperationException` with descriptive message if validation fails
    - `LoadGroundTruth()`: verify all queries have non-empty `QueryId`, `Query`, and `ExpectedResults` with at least 3 entries per query (minimum for meaningful NDCG@10 discrimination — fewer than 3 relevant docs reduces NDCG to near-binary MRR behavior)
  - [ ] 5.7 Add cross-validation between corpus and ground truth: verify all document IDs in every `BenchmarkQuery.ExpectedResults` list exist in `BenchmarkCorpus.MemoryUnits`. Throw if any phantom document ID is found — a ground truth entry referencing a non-existent document can never be satisfied and silently penalizes NDCG scores
  - [ ] 5.5 Use embedded resources (`EmbeddedResource` in `.csproj`) to load JSON files, ensuring they travel with the test assembly
  - [ ] 5.6 Use `System.Text.Json` with `JsonNamingPolicy.CamelCase` for deserialization — consistent with project conventions

- [ ] Task 6: Create benchmark seeder for Redis + FalkorDB (AC: 1)
  - [ ] 6.1 Create `tests/Hexalith.Memories.Benchmarks/Infrastructure/BenchmarkSeeder.cs` as `internal sealed class`
  - [ ] 6.2 Implement `SeedAsync(BenchmarkCorpus corpus, IConnectionMultiplexer redis, IConnectionMultiplexer falkorDb, string tenantId)`:
    - Create RediSearch index (`{tenantId}:memories:idx`) and seed all memory units with text content
    - Create Redis Vector index (`{tenantId}:memories:vec`) and seed embedding vectors. Use **pre-computed deterministic vectors** stored in the corpus JSON (not live embedding API calls) to ensure reproducibility (NFR26)
    - Create FalkorDB graph nodes and edges using `FalkorDbGraphQueryBuilder` (reuse existing builder from `Hexalith.Memories.Redis`)
  - [ ] 6.3 The seeder must be idempotent — calling it twice produces the same indexed state
  - [ ] 6.5 After seeding, verify index integrity: assert that document count in RediSearch index and vector count in Redis Vector index both match `corpus.MemoryUnits.Count`, and node count in FalkorDB matches corpus size. Throw if mismatch — prevents silent seeding failures
  - [ ] 6.4 Pre-computed vectors: generate 768-dimensional vectors using a **deterministic one-time script** (seeded PRNG, not hand-crafted) and commit the output in `synthetic-corpus.json`. Use a topic-cluster approach: assign each document to a cluster, generate a base vector per cluster, then add small per-document perturbations with a fixed seed. The script is a dev tool (e.g., a simple .NET console app or Python script), not part of the test project itself. Output requirements:
    - Semantically similar documents (same cluster) should have high cosine similarity (>0.8)
    - Unrelated documents (different clusters) should have low cosine similarity (<0.3)
    - The vectors are test fixtures, not production-quality embeddings — they encode desired similarity relationships, not actual semantic meaning

- [ ] Task 7: Create `Hexalith.Memories.Benchmarks` test project (AC: 5)
  - [ ] 7.1 Create `tests/Hexalith.Memories.Benchmarks/Hexalith.Memories.Benchmarks.csproj`:
    - Target: `net10.0`
    - References: `Hexalith.Memories.Contracts`, `Hexalith.Memories.Server`, `Hexalith.Memories.Redis`, `Hexalith.Memories.TestHelpers`
    - Test SDK: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Shouldly`, `NSubstitute`, `Testcontainers`, `coverlet.collector`
    - Embedded resources: `Data/synthetic-corpus.json`, `Data/ground-truth.json`
    - **CRITICAL:** `Hexalith.Memories.Server` and `Hexalith.Memories.Redis` use `internal` classes. Verify that both projects have `[InternalsVisibleTo("Hexalith.Memories.Benchmarks")]` in their `AssemblyInfo` or `.csproj`. If not, add it — follow the same pattern used for `Hexalith.Memories.Server.Tests` and `Hexalith.Memories.IntegrationTests`
  - [ ] 7.2 Add the project to `Hexalith.Memories.slnx` under the `tests` folder
  - [ ] 7.3 Create `tests/Hexalith.Memories.Benchmarks/Fixtures/BenchmarkFixture.cs` implementing `IAsyncLifetime`:
    - Start Redis Stack and FalkorDB containers (reuse `Testcontainers` patterns from `CompositeSearchFixture`)
    - Load corpus and ground truth via `BenchmarkCorpusLoader`
    - Seed all data via `BenchmarkSeeder`
    - After seeding, compute and cache `CorpusStatistics` (document count and average document length from the seeded corpus) — needed for BM25 normalization when calling `FusionEngine.Fuse()` directly
    - **Smoke test:** After seeding, execute one syntactic search for a unique term that appears in exactly one corpus document. Verify that document is returned. This catches index schema mismatches (wrong field names, missing indexes) that would silently produce all-zero NDCG scores — making a seeding bug look like a thesis failure
    - Expose `IConnectionMultiplexer Redis`, `IConnectionMultiplexer FalkorDb`, `BenchmarkCorpus Corpus`, `IReadOnlyList<BenchmarkQuery> GroundTruth`, `CorpusStatistics CorpusStats`
  - [ ] 7.4 Create `[CollectionDefinition("Benchmark")]` and `[Collection("Benchmark")]` for shared fixture

- [ ] Task 8: Implement `NdcgScorerTests` unit tests (AC: 2, 3)
  - [ ] 8.1 Create `tests/Hexalith.Memories.Benchmarks/Scoring/NdcgScorerTests.cs`
  - [ ] 8.2 Test: perfect ranking → NDCG@10 = 1.0 (all relevant docs in correct positions)
  - [ ] 8.3 Test: completely irrelevant results → NDCG@10 = 0.0
  - [ ] 8.4 Test: partial matches → NDCG@10 between 0 and 1 with known expected value
  - [ ] 8.5 Test: fewer results than k → handles gracefully (no index out of bounds)
  - [ ] 8.6 Test: empty ground truth → returns 0.0
  - [ ] 8.7 Test: determinism — same inputs always produce same output (run 10 times, verify identical results)
  - [ ] 8.8 Test: `ComputePrecisionAtK` — all top-3 relevant → 1.0
  - [ ] 8.9 Test: `ComputePrecisionAtK` — no top-3 relevant → 0.0
  - [ ] 8.10 Test: `ComputePrecisionAtK` — 2 of 3 relevant → 0.667
  - [ ] 8.11 Test: `ComputePrecisionAtK` — fewer results than k → divides by actual count

- [ ] Task 9: Implement `BenchmarkSuiteTests` integration tests (AC: 1, 2, 3, 4, 5)
  - [ ] 9.1 Create `tests/Hexalith.Memories.Benchmarks/BenchmarkSuiteTests.cs` using `[Collection("Benchmark")]` fixture
  - [ ] 9.2 Implement `RunBenchmarkSuite_ProducesValidResults` as `[Fact]` — the **infrastructure test** (should always pass):
    - For each benchmark query in ground truth:
      1. Execute syntactic-only search via `SyntacticSearchService` (real Redis backend)
      2. Execute semantic-only search via `SemanticSearchService` (real Redis backend)
      3. For graph-only scoring: traverse from query's `GraphStartNodeId` via FalkorDB, rank reachable nodes by **hop-distance** using `ScoreNormalizer.NormalizeGraphProximity(hopDistance)` — this isolates the pure graph signal. Do NOT use graph-scoped search (which mixes in syntactic/semantic). Skip if `GraphStartNodeId` is null
      - **Parallelism:** Steps 1-3 should execute in parallel (`Task.WhenAll`) per query — matches production behavior and reduces total execution time
      4. Compose hybrid results via `FusionEngine.Fuse()` directly — pass the three axis result sets plus pre-computed corpus stats from the fixture. **Do NOT use `HybridSearchService`** — it depends on `IActorProxyFactory` / DAPR sidecar which is not available in the benchmark context. The benchmark tests the fusion *algorithm*, not the service orchestration (which is covered by `HybridSearchServiceTests`)
      5. Score each result set (hybrid, syntactic, semantic, graph) with `NdcgScorer.ComputeNdcg()` against ground truth
      6. Record `BenchmarkQueryResult` (set `GraphAxisActive = false` when graph was skipped)
    - Compile `BenchmarkSuiteResult` (populate `Caveat` with standard constant)
    - Write results to `benchmark-results.json` in the test output directory (`Path.Combine(AppContext.BaseDirectory, "benchmark-results.json")`) — this lands in `bin/Debug/net10.0/` and is collectible as a CI artifact via `actions/upload-artifact`
    - Log human-readable report via `ITestOutputHelper`
    - Store result in a `BenchmarkSuiteResult? _cachedResult` field for reuse by other tests in the same class
    - Assert: all NDCG@10 scores are in valid range [0.0, 1.0], results file was written, all queries were executed
    - **Do NOT assert `ThesisValidated` here** — this test validates infrastructure, not the thesis
  - [ ] 9.3 Implement `ThesisValidation_HybridOutperforms80Percent` as a **separate `[Fact]`** — the **thesis gate**:
    - Read from `_cachedResult` if populated (same test run as 9.2); otherwise, run the full benchmark suite
    - Asserts `result.ThesisValidated.ShouldBeTrue($"Hybrid win rate: {result.HybridWinRate:P0}")`
    - This test failing means "thesis not validated" — a product decision point, not a code bug
    - Mark with `[Trait("Category", "ThesisValidation")]` for separate CI filtering if needed
  - [ ] 9.4 Mark all tests with `[Trait("Category", "Benchmark")]` and `[Trait("Category", "Integration")]`
  - [ ] 9.5 Add `[Timeout(300_000)]` (5 minutes) on the main benchmark test to prevent CI hangs if container startup fails

- [ ] Task 10: Implement reproducibility test (AC: 3)
  - [ ] 10.1 Create `ReproducibilityTests` in `BenchmarkSuiteTests.cs`
  - [ ] 10.2 Test: Run the full benchmark suite twice against the same seeded dataset (same fixture, same container state)
  - [ ] 10.3 Assert: Compare the two `BenchmarkSuiteResult` objects **in-memory** (not via file output — the second run would overwrite the first file). Assert that every `BenchmarkQueryResult.HybridNdcg10`, `SyntacticNdcg10`, `SemanticNdcg10`, `GraphNdcg10` pair is identical between runs (zero variance)
  - [ ] 10.4 This validates NFR26 — reproducible benchmark results

- [ ] Task 11: Implement benchmark results output (AC: 4, 5)
  - [ ] 11.1 Create `tests/Hexalith.Memories.Benchmarks/Reporting/BenchmarkReporter.cs` as `internal static class`
  - [ ] 11.2 Implement `WriteResults(BenchmarkSuiteResult result, string outputPath)`:
    - Serialize to JSON with `System.Text.Json` using `JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }`
    - Include per-query breakdown with all NDCG@10 scores
    - Include thesis validation result with 80% threshold evaluation
  - [ ] 11.3 Implement `FormatConsoleReport(BenchmarkSuiteResult result) -> string`:
    - Human-readable table showing: Query ID, Description, Hybrid NDCG, Syntactic NDCG, Semantic NDCG, Graph NDCG, Hybrid P@3, Best Single P@3, Winner
    - Summary line: "Thesis Validation: PASSED/FAILED (X/Y queries, XX% hybrid win rate)"
    - **Caveat line:** "NOTE: Results use synthetic pre-computed vectors, not real embeddings. This validates fusion algorithm correctness, not production search quality. Real-world validation with actual embeddings is planned for Phase 1.5."
    - This output is logged by the test runner for CI visibility
  - [ ] 11.4 Populate the `Caveat` property on `BenchmarkSuiteResult` (defined in Task 1.2) with the standard caveat constant when compiling results

## Dev Notes

### Implementation Order

This story has 11 tasks with implicit dependencies. Build in this order to minimize context switching and ensure each phase is independently testable:

1. **Models + Scorers** (Tasks 1, 2, 8) — Pure code, no infrastructure. Create `BenchmarkQueryResult`, `BenchmarkSuiteResult`, `NdcgScorer`, `ComputePrecisionAtK`, and their unit tests. All tests pass without containers.
2. **Corpus + Ground Truth** (Tasks 3, 4) — Content authoring. Write `synthetic-corpus.json` and `ground-truth.json`. Generate vectors via deterministic script.
3. **Loader + Validation** (Tasks 5) — Pure code. Create `BenchmarkCorpusLoader` with post-load validation and cross-validation. Test that JSON loads and validates correctly.
4. **Project Scaffold** (Task 7.1, 7.2) — Create `.csproj`, add to `.slnx`, verify `InternalsVisibleTo` on Server and Redis projects.
5. **Seeder + Fixture** (Tasks 6, 7.3, 7.4) — Infrastructure. Create `BenchmarkSeeder` and `BenchmarkFixture`. Verify seeding with smoke test. First container-dependent step.
6. **Benchmark Tests** (Tasks 9, 10) — Integration. Create `BenchmarkSuiteTests` with infrastructure test, thesis test, and reproducibility test.
7. **Reporter** (Task 11) — Output formatting. Create `BenchmarkReporter` for JSON and console output.

### Implementation Overview

This story creates the Gate 1 validation infrastructure — the benchmark suite that determines whether the three-axis hypothesis holds. The suite runs automated NDCG@10 comparisons of hybrid search vs each single-axis search mode against a synthetic dataset with pre-defined ground truth.

**This is NOT a BenchmarkDotNet performance benchmark.** It is a search quality/relevance benchmark that measures information retrieval effectiveness via NDCG@10 scoring. The project name `Benchmarks` follows the architecture's file structure plan.

### What Already Exists (Do NOT Rebuild)

1. **`FusionEngine.Fuse()`** (`Server/Search/FusionEngine.cs`) — pure fusion function, produces `FusedScoredResult` with per-axis scores. Use as-is for hybrid search scoring.

2. **`HybridSearchService`** (`Server/Search/HybridSearchService.cs`) — orchestrates parallel axis calls via DAPR actor for corpus stats. **NOT used in benchmarks** because it depends on `IActorProxyFactory` (DAPR sidecar). Instead, the benchmark composes `FusionEngine.Fuse()` directly with pre-computed corpus stats. Service orchestration is already tested by `HybridSearchServiceTests`.

3. **`ScoreNormalizer`** (`Server/Search/ScoreNormalizer.cs`) — normalizes BM25, cosine, and graph proximity scores to 0.0-1.0.

4. **`SyntacticSearchService`** (`Server/Search/SyntacticSearchService.cs`) — RediSearch BM25 search. Requires `IConnectionMultiplexer` and index name.

5. **`SemanticSearchService`** (`Server/Search/SemanticSearchService.cs`) — Redis Vector KNN search. Requires `IConnectionMultiplexer` and index name.

6. **`GraphScopedSearch`** (`Server/Search/GraphScopedSearch.cs`) — FalkorDB graph traversal + enrichment from Redis.

7. **`CompositeSearchFixture`** (`IntegrationTests/Fixtures/CompositeSearchFixture.cs`) — manages Redis Stack + FalkorDB Testcontainers. Reuse this pattern for `BenchmarkFixture`.

8. **`FalkorDbGraphQueryBuilder`** (`Redis/Graph/FalkorDbGraphQueryBuilder.cs`) — builds parameterized Cypher queries for graph operations. Use for seeding graph data.

9. **Test data factories** in `TestHelpers/Factories/` — patterns for creating `ScoredResult`, `IndexInput`, `IngestionInput`. Follow factory conventions.

10. **Contract types** — `SearchQuery`, `SearchResult`, `ScoredResult`, `HybridSearchResult`, `FusedScoredResult`, `FusionWeights`, `CorpusStatistics`, `SourceType`, `EdgeType`, `EdgeOrigin` all exist in `Contracts/V1/`.

### NDCG@10 Scoring Algorithm

NDCG (Normalized Discounted Cumulative Gain) at rank 10:

```
DCG@k = sum(rel(i) / log2(i + 2)) for i = 0..k-1
IDCG@k = DCG of ideal ranking (all relevant docs first)
NDCG@k = DCG@k / IDCG@k
```

Using **binary relevance**: `rel(i) = 1.0` if `rankedResults[i]` appears in `groundTruth`, else `0.0`.

This is the industry-standard metric for search result quality. A score of 1.0 means perfect ranking; 0.0 means no relevant results in top-k.

### Synthetic Corpus Design Strategy

The corpus simulates a software team investigating a production incident. This domain is chosen because:
- It naturally requires all three search axes
- It has clear causal chains (deployment → incident → investigation → fix)
- It has semantic relationships (different teams using different vocabulary for the same concepts)
- It has explicit document references (ADRs citing incidents, reviews citing code)

**Vector Generation:** Pre-computed 768-dimensional vectors should be generated via a **deterministic one-time script** (seeded PRNG), not hand-crafted. Use a topic-cluster approach:
- Assign each document to a topic cluster (e.g., payment, deployment, architecture)
- Generate a base vector per cluster using a fixed random seed
- Add small per-document perturbations (also seeded) to break exact ties
- Documents in the same cluster have high cosine similarity (>0.8)
- Documents in different clusters have low cosine similarity (<0.3)
- Commit the generated JSON output; the script itself is a dev tool, not part of the test project

This avoids calling external embedding APIs during tests, ensuring reproducibility (NFR26).

### Document ID Naming and Tie-Breaking

`FusionEngine.Fuse()` breaks ties by `MemoryUnitId` (lexicographic). If two documents have the same composite score but different relevance, the one with the alphabetically-earlier ID ranks higher. When designing the corpus, use neutral ID naming (e.g., `mu-001`, `mu-002`) — do NOT name relevant documents with alphabetically-early IDs (e.g., `aaa-relevant`) and irrelevant ones with late IDs (e.g., `zzz-noise`), as this would bias tie-breaking in favor of relevant docs and inflate NDCG.

### Threshold Sensitivity

The 80% threshold with 5-10 queries has high sensitivity: flipping one query from win to loss changes the win rate by 10-20%. With 5 queries, 80% means exactly 4/5 — one query can invalidate the thesis. With 10 queries, it's 8/10 — two queries can. This is a **directional signal for a product decision**, not a statistically rigorous study. The PRD mandates this threshold explicitly. Phase 1.5 with real-world data and larger query sets provides stronger evidence.

### Corpus Discrimination Verification

With only 30-50 documents, BM25 corpus statistics may produce poor score discrimination — all syntactic scores clustering in a narrow band (e.g., 0.5-0.7) instead of spreading across 0.0-1.0. After seeding, verify that BM25 raw scores for a test query produce meaningful spread. If scores cluster, either increase corpus size or add documents with deliberately dissimilar term frequencies (e.g., short vs long documents, domain-specific vs generic vocabulary). This benchmark validates **ranking quality**, not performance at scale — the 10K-document latency targets (NFR1-3) are tested separately in integration tests.

### Fixture Pattern

Follow existing `CompositeSearchFixture` pattern:

```csharp
[CollectionDefinition("Benchmark")]
public class BenchmarkCollection : ICollectionFixture<BenchmarkFixture>;

public sealed class BenchmarkFixture : IAsyncLifetime
{
    // Start Redis Stack + FalkorDB containers
    // Load corpus + ground truth from embedded resources
    // Seed all data into backends
    // Expose connections and loaded data for tests
}
```

### Test Categorization

- `[Trait("Category", "Benchmark")]` — benchmark-specific marker for CI filtering
- `[Trait("Category", "Integration")]` — requires Docker containers
- These tests are Tier 3 (Aspire e2e level) — nightly/optional, not on every PR

### Results Output

The benchmark produces two output formats:
1. **`benchmark-results.json`** — machine-readable, serialized `BenchmarkSuiteResult`, suitable for CI artifact collection
2. **Console report** — human-readable table logged via `ITestOutputHelper`, visible in test runner output

### 80% Threshold Evaluation

From the PRD: "80% of benchmark queries must show measurably better results from hybrid retrieval than any single axis alone."

For each query, hybrid outperforms if:
```
HybridNdcg10 > max(SyntacticNdcg10, SemanticNdcg10, GraphNdcg10)
```

The threshold is: `(HybridWins / TotalQueries) >= 0.80`

**Infrastructure vs thesis — two separate tests:**
- `RunBenchmarkSuite_ProducesValidResults` — validates that the benchmark infrastructure works (seeding, searching, scoring, reporting). This `[Fact]` should always pass. It produces evidence.
- `ThesisValidation_HybridOutperforms80Percent` — asserts the 80% threshold. This `[Fact]` failing means "thesis not validated" — a product decision point, not a code bug. The team must then decide whether to adjust fusion weights, add more benchmark queries, or re-evaluate the graph axis. Marked with `[Trait("Category", "ThesisValidation")]` so CI can filter it separately from infrastructure tests.

### Thesis Failure Decision Tree

If the 80% threshold is not met, the team should evaluate based on the win rate:
- **60-79%:** Investigate fusion weights — the default 0.4/0.4/0.2 split may not be optimal. Try per-query weight tuning or adjust the graph weight. Also review ground truth rankings for accuracy.
- **40-59%:** Graph axis may not add significant value for these query types. Consider disabling graph scorer in fusion (D2 kill switch — config change, not rearchitecture). Re-run benchmark with dual-axis (syntactic + semantic) only.
- **<40%:** Fundamental thesis may be invalid for the tested domain. Pivot to dual-axis retrieval and re-evaluate whether graph traversal provides value only as a standalone feature (causal chain queries), not as a fusion contributor.

### Ground Truth Validity

**Ground truth quality is the single biggest risk in this story.** The entire NDCG@10 score is only as good as the ground truth ranking. Pre-defined ground truth must be reviewed by Jerome before the benchmark is considered valid — this matches the PRD's "Jerome + 2 independent reviewers" protocol. The automated suite uses pre-defined ground truth; human review is a manual follow-up if automated scores and judgment diverge.

### Synthetic Vectors Limitation

Pre-computed vectors guarantee reproducibility (NFR26) but do NOT test actual embedding quality. Vectors crafted to make the benchmark pass prove the *fusion algorithm* works correctly with favorable inputs — they do not prove the thesis holds with real-world embeddings. **Document this limitation in the benchmark results output** (both JSON and console report) so nobody misreads a passing benchmark as "thesis proven in production." Real-world validation with actual embeddings is deferred to Phase 1.5.

### Graph-Only Ranking in Benchmark Context

For the graph-only NDCG column, the benchmark uses **pure hop-distance ranking** — traverse from `GraphStartNodeId`, rank all reachable nodes by `ScoreNormalizer.NormalizeGraphProximity(hopDistance)` (closer nodes rank higher). This isolates the graph axis signal.

This is deliberately different from `GraphScopedSearch` (which performs a two-stage query: traverse → then search within that set using syntactic/semantic). Using `GraphScopedSearch` for the graph-only column would mix in syntactic/semantic signals, making it impossible to measure the graph axis contribution independently.

Implementation: use FalkorDB Cypher to get reachable nodes with hop distances, apply `NormalizeGraphProximity`, sort by score descending. No Redis search involved for the graph-only column.

For benchmarks, each query that has a graph component specifies a `GraphStartNodeId` in the ground truth. The graph NDCG is 0.0 and `GraphAxisActive = false` when `GraphStartNodeId` is null.

### CorpusStatistics Consistency

The `BenchmarkFixture` computes `CorpusStatistics` from the synthetic corpus after seeding. This must match what `CorpusStatisticsActor` would produce in production. Verify the computation uses the same length metric:
- `DocumentCount = corpus.MemoryUnits.Count`
- `AverageDocumentLength` — check `CorpusStatisticsActor` implementation to determine whether it uses character count (`Content.Length`) or word count (`Content.Split().Length`). The fixture must use the same formula, otherwise BM25 saturation normalization (`k = log2(docCount+1) * (avgDocLen/100)`) will produce different normalization constants than production, making the benchmark results non-representative.

### Benchmark Output Files

`benchmark-results.json` is written to `AppContext.BaseDirectory` during test execution. Add this file to `.gitignore` to prevent accidental commits of benchmark output. CI should collect it as an artifact via `actions/upload-artifact` targeting the test output directory.

AC5 specifies "completes within a reasonable time" — this is operationally defined by the `[Timeout(300_000)]` attribute (5 minutes max per test). Container startup is collection-scoped (once per collection, not per test), so the full 5 minutes is available for search execution and scoring.

### HybridSearchService Wiring Verification (Dependency)

The benchmark bypasses `HybridSearchService` and composes `FusionEngine.Fuse()` directly. This means the benchmark does NOT test that `HybridSearchService` correctly passes parameters to `FusionEngine.Fuse()`. If the service has a subtle wiring bug (e.g., wrong parameter order), the benchmark wouldn't catch it.

**Dependency:** `HybridSearchServiceTests` (in `Server.Tests`) should include a test verifying that `HybridSearchService.SearchAsync()` produces the same top-3 results as direct `FusionEngine.Fuse()` composition for a known input set. This is a wiring verification — out of scope for this story but important for confidence in the overall search pipeline.

### Project Structure Notes

New project and files:
```
tests/Hexalith.Memories.Benchmarks/
  Hexalith.Memories.Benchmarks.csproj     # NEW test project
  BenchmarkSuiteTests.cs                  # NEW — main benchmark test class
  Data/
    synthetic-corpus.json                  # NEW — 30-50 memory units with pre-computed vectors
    ground-truth.json                      # NEW — 5-10 queries with expected rankings
    BenchmarkCorpusLoader.cs              # NEW — JSON loader for corpus + ground truth
  Models/
    BenchmarkQueryResult.cs               # NEW — per-query result record
    BenchmarkSuiteResult.cs               # NEW — overall suite result record
    BenchmarkCorpus.cs                    # NEW — corpus data model
    BenchmarkQuery.cs                     # NEW — ground truth query model
  Scoring/
    NdcgScorer.cs                         # NEW — pure NDCG@10 computation
    NdcgScorerTests.cs                    # NEW — unit tests for scorer
  Infrastructure/
    BenchmarkSeeder.cs                    # NEW — seeds Redis + FalkorDB with corpus
  Fixtures/
    BenchmarkFixture.cs                   # NEW — Testcontainers fixture
  Reporting/
    BenchmarkReporter.cs                  # NEW — JSON + console output
```

Modified files:
```
Hexalith.Memories.slnx                    # ADD Benchmarks project
```

All files use:
- ITANEO copyright header
- File-scoped namespace: `namespace Hexalith.Memories.Benchmarks.{Folder};`
- `sealed` classes (no inheritance)
- `internal` for implementation classes, `public` for result records (consumed by reporting)

### Testing Standards

- **Framework:** xUnit 2.9.3
- **Assertions:** Shouldly (`score.ShouldBe(expected, tolerance: 0.001)`, `result.ThesisValidated.ShouldBeTrue()`)
- **Containers:** Testcontainers 4.3.0 with `IAsyncLifetime` pattern
- **JSON:** `System.Text.Json` with `JsonNamingPolicy.CamelCase`
- **No mocking needed for `NdcgScorer`** — pure static function
- **Integration tests:** Real Redis Stack + FalkorDB containers via `BenchmarkFixture`

### What This Story Does NOT Implement

- **CLI `--benchmark` command** — CLI is Epic 7. This story creates the test infrastructure that validates the thesis.
- **Real-world data benchmarks** — D11 specifies synthetic dataset for MVP. Real-world validation is deferred to Phase 1.5.
- **Independent reviewer scoring** — The PRD mentions "Jerome + 2 independent reviewers." This automated suite uses pre-defined ground truth. Human review is a manual follow-up if automated scores and judgment diverge.
- **BenchmarkDotNet performance microbenchmarks** — This is a search relevance/quality benchmark (NDCG@10), not a performance benchmark.
- **Fusion weight tuning** — Default weights (0.4/0.4/0.2) are used. If thesis validation fails, weight tuning is a separate investigation.
- **Periodic recall benchmarks** — Growth-phase feature for detecting silent HNSW degradation.
- **Graded relevance scoring** — Current NDCG uses binary relevance (relevant/not-relevant). Graded relevance (0/1/2/3) would capture "doc A is more relevant than doc B." This is a one-line change to `NdcgScorer` and a ground truth schema change. Consider if binary proves insufficient.
- **Scale benchmarks** — This corpus (30-50 docs) validates ranking quality, not performance. Scale benchmarks with 10K+ docs are a separate concern for latency validation.
- **Ground truth iteration** — Ground truth is a living artifact that may need updating after initial results reveal ranking inaccuracies. The corpus and ground truth are embedded resources, so updates require recompilation — acceptable for MVP.
- **Extensible axis model** — `BenchmarkQueryResult` hardcodes `SyntacticNdcg10, SemanticNdcg10, GraphNdcg10` fields rather than using a dictionary keyed by axis name. Acceptable for 3 known axes; refactor to `IReadOnlyDictionary<string, double>` if a 4th axis (e.g., temporal) is added.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.7 — Benchmark Suite & Thesis Validation]
- [Source: _bmad-output/planning-artifacts/prd.md#FR25 (automated benchmark comparisons with scored output)]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR26 (reproducible benchmark results)]
- [Source: _bmad-output/planning-artifacts/prd.md#Measurable Outcomes — 80% threshold, NDCG@10 scoring protocol]
- [Source: _bmad-output/planning-artifacts/prd.md#D11 (synthetic dataset with known relationships)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Benchmark Query Gap — 3-5 example queries requiring all three axes]
- [Source: _bmad-output/planning-artifacts/architecture.md#Gate Implementation Strategy — Gate 1 three-axis validation]
- [Source: _bmad-output/planning-artifacts/architecture.md#Three-Tier Test Structure — Benchmarks are Tier 3]
- [Source: _bmad-output/planning-artifacts/architecture.md#Silent Failure Modes — benchmark suite as MVP detection mechanism]
- [Source: _bmad-output/planning-artifacts/architecture.md#file structure — tests/Hexalith.Memories.Benchmarks/ with Data/ subfolder]
- [Source: src/Hexalith.Memories.Server/Search/FusionEngine.cs — pure fusion function]
- [Source: src/Hexalith.Memories.Server/Search/HybridSearchService.cs — hybrid search orchestrator]
- [Source: src/Hexalith.Memories.Server/Search/ScoreNormalizer.cs — normalization functions]
- [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs — BM25 search]
- [Source: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs — vector KNN search]
- [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs — graph traversal + enrichment]
- [Source: tests/Hexalith.Memories.IntegrationTests/Fixtures/CompositeSearchFixture.cs — Redis + FalkorDB fixture pattern]
- [Source: src/Hexalith.Memories.Contracts/V1/ — SearchQuery, SearchResult, ScoredResult, HybridSearchResult, FusionWeights, CorpusStatistics, SourceType, EdgeType, EdgeOrigin]

### Previous Story Intelligence (Story 2.6)

From Story 2.6 (Explain Mode & Confidence Scores):
- `SearchExplanation`, `AxisExplanation` types added to contracts — benchmark results don't need these, but the explain mode endpoint can be used for debugging benchmark queries
- `ExplainMetadataBuilder` is a static helper — follows same pattern as the benchmark's `NdcgScorer` and `BenchmarkReporter` (internal static classes for pure functions)
- `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` pattern used for optional fields — apply same for nullable benchmark result fields
- All Story 2.5 and 2.6 types registered in `MemoriesJsonContext` — benchmark models in the test project don't need AOT context registration (test code only)

### Git Intelligence

Recent commits confirm the search axis implementation trajectory:
- `40b79fc` feat(search): add hybrid fusion (#7)
- `2ecbbaf` feat: Implement CorpusStatistics actor for caching per-tenant RediSearch statistics
- `81057a3` feat: Implement GraphScopedSearch for traversing FalkorDB and enriching results from Redis
- `5c39312` feat: Implement Semantic Search Service with KNN vector search capabilities
- `0d104b7` feat: Implement Syntactic Search Service with BM25 ranking and related data models

Pattern: Each search service is a standalone `internal sealed class`. Test projects use `CompositeSearchFixture` for multi-backend tests. Benchmark tests should follow the same fixture + seeding + assertion pattern used in `HybridSearchApiIntegrationTests`.

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
