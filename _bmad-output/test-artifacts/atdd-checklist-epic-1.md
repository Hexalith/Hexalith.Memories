---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-generation-mode', 'step-03-test-strategy', 'step-04-generate-tests', 'step-05-validate-and-complete']
lastStep: 'step-05-validate-and-complete'
lastSaved: '2026-03-29'
detectedStack: backend
generationMode: ai-generation
inputDocuments:
  - _bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md
  - _bmad-output/implementation-artifacts/1-2-memory-unit-domain-model-and-contracts.md
  - _bmad-output/implementation-artifacts/1-3-content-extraction-via-kreuzberg.md
  - _bmad-output/implementation-artifacts/1-4-embedding-generation.md
  - _bmad-output/implementation-artifacts/1-5-three-backend-indexing.md
  - _bmad-output/implementation-artifacts/1-6-ingestion-workflow-orchestration.md
  - _bmad/tea/testarch/knowledge/data-factories.md
  - _bmad/tea/testarch/knowledge/test-quality.md
  - _bmad/tea/testarch/knowledge/test-levels-framework.md
  - _bmad/tea/testarch/knowledge/test-priorities-matrix.md
---

# ATDD Checklist — Epic 1: Foundation, Ingestion & Graph Edge Indexing

## Test Strategy Overview

- **Stack:** .NET 10 / C# 14 / xUnit + Shouldly + NSubstitute
- **Test Projects:** Contracts.Tests, Server.Tests, IntegrationTests, TestHelpers
- **Test Levels:** Unit (mocked deps), Integration (real Redis/FalkorDB), no E2E/browser
- **Red Phase Pattern:** `[Fact(Skip = "ATDD: not yet implemented")]` for pending tests
- **Existing Coverage:** ~95+ tests across 4 projects

---

## Story 1.1: Project Scaffolding & Single-Command Boot (DONE)

### AC → Test Mapping

| AC# | Acceptance Criterion | Test Level | Priority | Existing Test? | Gap? |
|-----|---------------------|------------|----------|----------------|------|
| 1 | Single command boots Redis, FalkorDB, Server+DAPR, Aspire Dashboard | Integration | P1 | No automated test | YES — manual verification only |
| 2 | `dotnet build` succeeds with all projects; missing submodules show helpful error | Build/Unit | P0 | `MemoriesInfoTests.Name_ShouldBeCorrect` (smoke) | PARTIAL — no submodule error test |
| 3 | Aspire Dashboard shows health; OTEL configured | Integration | P2 | No automated test | YES — manual verification only |

### Gap Analysis

- **Covered:** Build smoke test confirms test framework wired
- **Missing (P1):** No automated test for AppHost boot sequence. Acceptable — Aspire integration tests require `DistributedApplicationTestingBuilder` (deferred to Epic 11 CI story)
- **Missing (P0):** No test for submodule-missing error message. Low risk — MSBuild target tested manually during story
- **Recommendation:** Defer AppHost integration tests to Story 11.1. Add submodule detection test as tech debt item.

---

## Story 1.2: Memory Unit Domain Model & Contracts (DONE)

### AC → Test Mapping

| AC# | Acceptance Criterion | Test Level | Priority | Existing Test? | Gap? |
|-----|---------------------|------------|----------|----------------|------|
| 1 | MemoryUnit has all required fields | Unit | P0 | `MemoryUnitSerializationTests` (7 tests) | NO |
| 2 | GraphEdge has all fields + default confidence constants | Unit | P0 | `GraphEdgeSerializationTests` (5 tests) | PARTIAL — EdgeTypeDefaults not tested directly |
| 3 | ErrorResponse format matches expected JSON | Unit | P1 | `ErrorResponseSerializationTests` (2 tests) | NO |
| 4 | JSON round-trip for all V1 types | Unit | P0 | All *SerializationTests (36 tests total) | NO |

### Gap Analysis

- **Covered:** Comprehensive serialization round-trips, enum camelCase, nullable fields, boundary values, metadata null resilience
- **Missing (P1):** `EdgeTypeDefaults` constants not explicitly tested (e.g., `EdgeTypeDefaults.CausedBy.ShouldBe(1.0f)`)
- **Missing (P2):** No test that MemoryUnit fields match the exhaustive field inventory from architecture doc
- **Recommendation:** Add `EdgeTypeDefaultsTests.cs` — 5 simple constant assertions. Low effort, high confidence.

---

## Story 1.3: Content Extraction via Kreuzberg (DONE)

### AC → Test Mapping

| AC# | Acceptance Criterion | Test Level | Priority | Existing Test? | Gap? |
|-----|---------------------|------------|----------|----------------|------|
| 1 | Plain text extraction returns raw text | Integration | P0 | `ContentExtractionClientTests` [Trait Integration] | NO |
| 2 | PDF extraction returns text | Integration | P0 | `ContentExtractionClientTests` [Trait Integration] | NO |
| 3 | Markdown preserved (not rendered to HTML) | Integration | P1 | `ContentExtractionClientTests` [Trait Integration] | NO |
| 4 | Kreuzberg exception propagates for DAPR retry | Unit | P0 | `ExtractContentActivityTests.RunAsync_WhenClientThrowsException_ShouldPropagate` | NO |
| 5 | Aspire Dashboard shows trace span | Integration | P2 | No automated test | YES — OTEL auto-instrumented |
| 6 | Empty content throws InvalidOperationException | Unit | P1 | `ExtractContentActivityTests.RunAsync_WhenClientThrowsInvalidOperationException_ShouldPropagate` | NO |

### Gap Analysis

- **Covered:** All core ACs (1-4, 6) have unit and integration tests
- **Missing (P2):** AC5 (tracing) — no test for OTEL span presence. Acceptable: DAPR auto-instruments workflow activities
- **Covered extras:** SHA-256 hash verification, ContentType defaulting, serialization round-trips for DTOs
- **Recommendation:** No action needed. Coverage is solid.

---

## Story 1.4: Embedding Generation (DONE)

### AC → Test Mapping

| AC# | Acceptance Criterion | Test Level | Priority | Existing Test? | Gap? |
|-----|---------------------|------------|----------|----------------|------|
| 1 | Google embedding API returns 768-dim vector | Unit | P0 | `EmbeddingClientTests` (mock HttpHandler) + `GenerateEmbeddingActivityTests.RunAsync_SuccessfulEmbedding` | NO |
| 2 | Rate limiter actor checks budget before embedding | Unit | P0 | `RateLimiterLogicTests` (8 tests) + `GenerateEmbeddingActivityTests.RunAsync_RateLimitExhausted` | NO |
| 3 | HTTP 429 → EmbeddingRateLimitException → DAPR retry | Unit | P0 | `EmbeddingClientTests` (429 test) + `GenerateEmbeddingActivityTests.RunAsync_EmbeddingClientThrows` | NO |
| 4 | API key from DAPR Secrets API, never in config | Unit | P1 | `EmbeddingClientTests` (secret retrieval + unavailable tests) | NO |

### Gap Analysis

- **Covered:** All 4 ACs covered comprehensively at unit level
- **Covered extras:** Invalid tenant ID, prime API key failure, dimension mismatch, malformed JSON, timeout, window boundary (59s vs 60s), custom ceiling, non-positive ceiling
- **Missing (P2):** No integration test with real Google API (expected — costs money, rate-limited)
- **Recommendation:** No action needed. Excellent coverage with mocked boundaries.

---

## Story 1.5: Three-Backend Indexing (REVIEW)

### AC → Test Mapping

| AC# | Acceptance Criterion | Test Level | Priority | Existing Test? | Gap? |
|-----|---------------------|------------|----------|----------------|------|
| 1 | RediSearch indexing with tenant namespace | Unit + Integration | P0 | `IndexSyntacticActivityTests` (3 tests) + `IndexSyntacticIntegrationTests` (3 tests) | NO |
| 2 | Redis Vector storage with tenant namespace | Unit | P0 | `IndexSemanticActivityTests` (6 tests) | PARTIAL — no integration test |
| 3 | FalkorDB graph: node, edges (caused_by, correlated_with, contains) | Unit + Integration | P0 | `IndexGraphActivityTests` (8 tests) + `GraphQueryBuilderIntegrationTests` (4 tests) | NO |
| 4 | IGraphQueryBuilder: parameterized only, no raw Cypher | Unit | P0 | `GraphQueryBuilderTests` injection prevention (4 tests) | NO |
| 5 | Tenant-namespaced index naming | Unit + Integration | P0 | `*Tests.RunAsync_ShouldUseTenantNamespacedKey` + `IntegrationTests.TenantIsolation_*` | NO |

### Gap Analysis

- **Covered:** Core indexing for all 3 backends, injection prevention, tenant isolation, gold-value vector conversion
- **Missing (P1):** `IndexSemanticActivity` has no integration test with real Redis Vector Search (syntactic and graph have integration tests)
- **Missing (P2):** No test for index naming extensibility to `{tenantId}:{model-version}:syntactic` (AC5 future-proofing, doc-only)
- **Missing (P2):** No test for `BuildMergeStubNode` in integration context
- **Recommendation:**
  1. Add `IndexSemanticIntegrationTests.cs` — verify vector stored and retrievable via KNN in real Redis Stack
  2. Add `BuildMergeStubNode` integration test in `GraphQueryBuilderIntegrationTests`

---

## Story 1.6: Ingestion Workflow Orchestration (READY-FOR-DEV)

### AC → Test Mapping — ATDD Red Phase

| AC# | Acceptance Criterion | Test Scenarios | Level | Priority |
|-----|---------------------|----------------|-------|----------|
| 1 | Full pipeline: Validate → Extract → Embed → Fan-out Index → Verify | Happy path: all activities called in order; status transitions queued→indexed | Unit | P0 |
| 1 | Fan-out: 3 indexing activities execute in parallel | Verify Task.WhenAll used for IndexSyntactic + IndexSemantic + IndexGraph | Unit | P0 |
| 2 | VerifyConsistencyActivity checks all 3 backends | Query RediSearch, Redis Vector, FalkorDB for memory unit existence | Unit | P0 |
| 2 | Consistency discrepancy: report missing backends | Missing backend → logged warning, not workflow failure | Unit | P1 |
| 3 | Saga compensation: only cleanup succeeded backends | Syntactic OK + semantic FAIL → only CleanupSyntactic called | Unit | P0 |
| 3 | Failed unit gets status=failed + FailureDetails | FailureDetails populated with stage, error code, retry count | Unit | P0 |
| 4 | Provenance: IngestedBy, IngestedAt, metadata tracking | Fields populated from workflow context | Unit | P1 |
| 5 | DAPR sidecar recovery: workflow resumes | Durable Task Framework persists state | Integration | P1 |
| 6 | Duplicate detection by source identifier | Second ingestion returns early with existing MemoryUnitId | Unit | P1 |
| 6 | SaveDedupKeyActivity writes key after success | Dedup key written to DAPR state store | Unit | P1 |

### Additional Edge Cases (Risk-Derived)

| Scenario | Level | Priority |
|----------|-------|----------|
| ValidateContentActivity: null TenantId → ArgumentException (no retry) | Unit | P0 |
| ValidateContentActivity: empty ContentBytes → ArgumentException | Unit | P0 |
| CheckIdempotencyActivity: state store unavailable → exception propagates | Unit | P1 |
| All 3 indexing activities fail → no cleanup needed (nothing to compensate) | Unit | P1 |
| VerifyConsistencyActivity timeout → workflow retry handles | Unit | P2 |
| IngestionInput/IngestionResult serialization round-trip | Unit | P0 |

### Fixture Needs (Story 1.6)

- `IngestionInputFactory` — builds complete IngestionInput with defaults
- Mock `WorkflowContext` for workflow orchestration tests
- Mock `DaprClient` for state store (dedup key) operations
- Reuse existing `IndexInputFactory`, `ExtractionInputFactory`

---

## Summary: Priority Distribution

| Priority | Stories 1.1-1.5 Existing | Stories 1.1-1.5 Gaps | Story 1.6 New |
|----------|--------------------------|---------------------|---------------|
| P0 | 75+ tests | 2 tests | 10 tests |
| P1 | 15+ tests | 2 tests | 6 tests |
| P2 | 3 tests | 3 tests | 1 test |

## Identified Gaps (Ordered by Risk)

1. **[P1] IndexSemanticIntegrationTests** — no real Redis Vector integration test (Story 1.5)
2. **[P1] EdgeTypeDefaultsTests** — confidence constants untested (Story 1.2)
3. **[P1] BuildMergeStubNode integration** — stub node not tested against real FalkorDB (Story 1.5)
4. **[P2] Submodule detection test** — MSBuild error message untested (Story 1.1)
5. **[P2] OTEL tracing assertions** — no automated trace verification (Stories 1.1, 1.3)

## Generated ATDD Tests (Story 1.6 — Red Phase)

### Test Files Created

| File | Tests | Covers |
|------|-------|--------|
| `Workflows/IngestionWorkflowTests.cs` | 12 | AC1-AC6: full pipeline, fan-out, consistency, compensation, provenance, dedup |
| `Activities/Ingestion/ValidateContentActivityTests.cs` | 7 | AC1 Task 2: input validation (TenantId, CaseId, SourceUri, ContentBytes, IngestedBy) |
| `Activities/Ingestion/CheckIdempotencyActivityTests.cs` | 4 | AC6 Task 3: dedup check via DAPR state store |
| `Activities/Indexing/VerifyConsistencyActivityTests.cs` | 6 | AC2 Task 4: three-backend existence check |
| `Activities/Indexing/CleanupActivityTests.cs` | 6 | AC3 Task 5: saga compensation (syntactic, semantic, graph cleanup) |
| `Activities/Ingestion/SaveDedupKeyActivityTests.cs` | 3 | AC6 Task 5b: dedup key persistence |
| **Total** | **38** | All 6 ACs + edge cases |

### Factory Created (Guarded)

| File | Status |
|------|--------|
| `TestHelpers/Factories/IngestionInputFactory.cs` | `#if false` — enable when `IngestionInput` contract exists |

### Build & Test Results

- **Build:** 0 warnings, 0 errors (all projects)
- **Existing tests:** 82 passed, 0 failed
- **ATDD tests:** 39 skipped (red phase — `[Fact(Skip = "ATDD Red Phase: ...")]`)
- **Total:** 121 tests (82 passed + 39 skipped)

### How to Use These Tests

1. **Start Story 1.6 implementation** — create contracts (Task 1)
2. **Enable `IngestionInputFactory`** — remove `#if false` guard
3. **Implement each activity** — remove `Skip` annotation from corresponding tests
4. **Run tests** — they should go GREEN as implementation completes
5. **Fill in Arrange/Act/Assert** — replace placeholder assertions with real mocks and assertions
