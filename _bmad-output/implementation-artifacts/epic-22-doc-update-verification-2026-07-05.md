# Epic 22 Documentation Update Verification

Project: Hexalith.Memories
Epic: 22 - RAG Retrieval Quality and Correctness
Recorded: 2026-07-05
Mode: Autonomous code/doc consistency verification after retrospective.

## Verification Method

For each candidate document, the current document text was compared with the implemented code and story evidence for Epic 22. Updates were applied only where a discrepancy was verified.

Primary implementation evidence:

- `src/Hexalith.Memories.Server/Search/FusionEngine.cs`
- `src/Hexalith.Memories.Server/Search/ExplainMetadataBuilder.cs`
- `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs`
- `src/Hexalith.Memories.Server/Search/SearchPaginationOptions.cs`
- `src/Hexalith.Memories.Server/Search/NaturalLanguageSemanticSearchService.cs`
- `src/Hexalith.Memories.Server/Search/IResultFuser.cs`
- `src/Hexalith.Memories.Contracts/V1/SearchQuery.cs`
- `src/Hexalith.Memories.Contracts/V1/FusionWeights.cs`
- `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`
- `src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`
- Epic 22 story records `22-1` through `22-7`

## Applied Updates

| Document | Verified discrepancy | Update applied |
|---|---|---|
| `_bmad-output/planning-artifacts/prd.md` | The PRD still described hybrid scores as weighted averages of normalized BM25/cosine/graph magnitudes. Code now uses weighted reciprocal-rank fusion and exposes hybrid per-axis rank contributions; `FusionWeights` also includes `NlWeight`. | Updated the score semantics table and NFR24 row to describe single-axis semantics separately from hybrid RRF semantics, including NL. |
| `_bmad-output/planning-artifacts/architecture.md` | Architecture still framed fusion as normalized raw-score fusion with injected corpus statistics. Code now keeps `FusionEngine.Fuse` pure but uses weighted RRF and no longer depends on corpus statistics for hybrid scoring. | Updated algorithmic quality, the fusion driver, testability rows, and `CorpusStatisticsActor` purpose. |
| `docs/dev/cli-output-formats.md` | CLI example still showed hybrid explain output as BM25 saturation/cosine raw semantics and omitted NL score semantics. Code emits hybrid rank-contribution explanations and `FusedScoredResult` includes `NlScore`. | Updated the explain examples to use `rrf_rank_contribution` and include `nlScore`/NL explain text. |
| `docs/dev/mcp-server.md` | MCP `search_memory` documented only `Syntactic`, `Semantic`, and `Hybrid`. Code exposes `SearchAxis.Nl` and tool descriptions mention `nl`. | Added `Nl` to the documented axis enum. |
| `docs/dev/eventstore-integration.md` | The NL section still said `NaturalLanguageSemanticSearchService` was library-only and not wired into hybrid. Story 22.7 wired REST/CLI/MCP `axis=nl` and explicit hybrid NL. | Updated NL risk text to describe explicit `axis=nl` and explicit hybrid NL availability, while noting default hybrid stability. |
| `README.md` | CLI summary still described only three-axis search. CLI now accepts syntactic, semantic, natural-language, graph, and hybrid modes. | Updated the CLI summary to say multi-axis search and list the supported modes. |

## Candidates Discarded After Verification

| Document | Verification result | Decision |
|---|---|---|
| `docs/dev/evidence-packet.md` | The document maps axes generically from lower-level search results and does not hard-code the old weighted-average algorithm. Its caveat remains correct: scores describe retrieval evidence, not factual truth. | No update needed. |
| `docs/operations/embedding-providers.md` | The document already describes NL index migration and blue/green raw/NL alias behavior from Epic 21. Epic 22 did not change embedding-provider configuration or migration procedure. | No update needed. |
| `docs/operations/deployment-configuration.md` | Epic 22 did not add or rename deployment environment variables or Dapr component names. | No update needed. |
| `docs/dev/consistency.md` | Epic 22 did not change consistency inspect API behavior. The carried Epic 21 action item remains open, but no Epic 22 discrepancy was verified. | No update applied in this pass. |
| `docs/dev/mcp-server.md` ingest `sourceType` row | The source-type ingest note is outside Epic 22 retrieval scope and was not contradicted by the searched MCP implementation evidence. | No update applied. |

## Follow-Up

- Keep hybrid docs aligned with `ExplainMetadataBuilder` whenever score semantics change.
- Keep surface docs aligned with `SearchAxis` / CLI axis parsing when new axes are added.
- Carry retrieval invariants into Epic 23 ingestion stories so ingestion output continues to satisfy the hardened search contracts.
