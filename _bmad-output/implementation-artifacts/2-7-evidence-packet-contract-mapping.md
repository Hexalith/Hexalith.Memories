# Story 2.7: Evidence Packet Contract Mapping

Status: ready-for-dev

## Story

As a developer and LLM-agent integrator,
I want search and diagnostic responses to share an Evidence Packet contract,
so that CLI, MCP, and future web UI expose the same trust semantics.

## Historical Alias and Sprint Ledger Note

- This story was previously identified as Story `2.6A`; current tooling and future references must use Story `2.7`.
- The file `_bmad-output/implementation-artifacts/2-7-benchmark-suite-and-thesis-validation.md` is a historical benchmark artifact only. Benchmark Suite and Thesis Validation is now Story `2.8` and is represented by `_bmad-output/implementation-artifacts/2-8-benchmark-suite-and-thesis-validation.md`.
- This story owns `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md`. Do not update the benchmark artifacts for this implementation unless a narrow reference correction is unavoidable.
- The sprint ledger previously marked `epic-2` as `done` while this story remained `backlog`; creating this story reopens Epic 2 to `in-progress` until Story 2.7 is completed.

## Acceptance Criteria

1. Given `Contracts.V1` defines or maps the Evidence Packet response shape; when search, explain, empty-result, degraded-result, or diagnostic responses are produced; then the response can expose tenant/case scope, result summary, sources, evidence strength, confidence caveat, retrieval axes used, graph summary, state, omitted details, and recovery actions.
2. Given CLI JSON search output is requested; when an Evidence Packet is emitted; then it uses the same field semantics as MCP responses and future UI composition descriptors, and no surface invents conflicting confidence, degraded, omitted, or recovery definitions.
3. Given a response is compressed by token budget or density; when details are omitted; then omitted fields are named explicitly and deterministic expansion handles or equivalent retrieval guidance identify how to retrieve omitted detail groups.
4. Given Evidence Packet contract tests run; when complete, degraded, empty, unauthorized, and token-budget-compressed packets are serialized; then JSON round trips preserve contract shape and required fields.

## Tasks / Subtasks

- [ ] Task 1: Add the canonical Evidence Packet grammar in `Contracts.V1` (AC: 1, 4)
  - [ ] Add a small additive record set under `src/Hexalith.Memories.Contracts/V1/` for the shared envelope. Use clear public names such as `EvidencePacket`, `EvidencePacketScope`, `EvidencePacketResultSummary`, `EvidencePacketSource`, `EvidencePacketEvidence`, `EvidencePacketGraphSummary`, `EvidencePacketState`, `EvidencePacketOmittedDetails`, `EvidencePacketExpansionHandle`, and `EvidencePacketRecoveryAction` unless an existing local naming pattern makes a tighter split obvious.
  - [ ] Include fields that map directly to the architecture grammar: `scope`, `result`, `sources`, `evidence`, `graph`, `state`, `omittedDetails`, and `recovery`.
  - [ ] Keep `SearchResult`, `HybridSearchResult`, `ScoredResult`, `FusedScoredResult`, and `SearchExplanation` as lower-level retrieval contracts. Do not rename or remove existing JSON fields.
  - [ ] Add all new public contract types to `MemoriesJsonContext` source-generation metadata.

- [ ] Task 2: Map existing search and diagnostic outputs into Evidence Packet semantics (AC: 1, 3)
  - [ ] Build a pure mapper or factory that composes an Evidence Packet from existing `SearchResult`, `HybridSearchResult`, `SearchExplanation`, `ErrorResponse`, and request scope data.
  - [ ] Pass request scope into the mapper explicitly because current lower-level results carry `Query` but do not carry `TenantId` or `CaseId`.
  - [ ] Map single-axis and hybrid result sources from `SourceUri`, `SourceType`, `MemoryUnitId`, snippets, case metadata, and score metadata.
  - [ ] Map evidence from `SearchExplanation.Caveat`, per-axis `AxisDetails`, `AxesUsed`, normalized axis scores, composite score, `Degraded`, and `UnavailableAxes`.
  - [ ] Map states consistently: complete, partial, weak, empty, stale, degraded, unauthorized, and pending expansion. Use only states the current response can justify; do not claim factual accuracy or data completeness from relevance scores.
  - [ ] Keep diagnostics sanitized. Do not include secrets, tokens, raw embeddings, full prompts, local absolute paths, or unsanitized exception text in Evidence Packet diagnostics.

- [ ] Task 3: Apply the shared contract to CLI JSON and MCP search results (AC: 2)
  - [ ] Update CLI JSON search output so consumers can read the Evidence Packet semantics without a separate CLI-specific vocabulary. Either emit `CliOutputEnvelope<EvidencePacket>` or include a clearly named `evidencePacket` member in the existing JSON envelope; choose the option that best preserves existing CLI compatibility.
  - [ ] Update `CliJsonContext` and any payload registry entries required by the chosen CLI JSON shape.
  - [ ] Preserve existing human and table search output behavior unless a small addition is necessary for the evidence semantics.
  - [ ] Update `search_memory` MCP structured results to expose the same Evidence Packet semantics for single-axis and hybrid search.
  - [ ] Preserve `McpToolResultSerializer.Success` behavior that returns both serialized JSON text content and `StructuredContent`.
  - [ ] Preserve MCP error behavior, including `IsError = true`, structured error code/service/tool fields, and sanitized messages. Error responses may include packet-style state and recovery fields, but must not lose protocol-level error semantics.

- [ ] Task 4: Make omitted-detail and expansion semantics explicit (AC: 3)
  - [ ] Reuse the existing `OmittedCount`, `EstimatedTokensTotal`, `OmittedReason`, `AxesUsed`, and `UnavailableAxes` metadata produced by search response metadata application.
  - [ ] Add explicit omitted field names or omitted detail groups for token-budget and density-compressed packets.
  - [ ] Add deterministic expansion handles or equivalent retrieval guidance. If no expansion endpoint exists, use guidance backed by current capabilities, such as re-running with a larger `tokenBudget` or `maxResults`, fetching a memory unit by id, or using traversal/search commands that already exist.
  - [ ] Ensure empty and degraded results still include recovery actions that are safe and specific.

- [ ] Task 5: Add focused serialization, surface, and regression tests (AC: 1, 2, 3, 4)
  - [ ] Add contract tests for complete, degraded, empty, unauthorized, and token-budget-compressed Evidence Packets.
  - [ ] Update `PublicContractSerializationCoverageTests` or equivalent coverage so every new public contract type is source-generation covered.
  - [ ] Add CLI JSON formatter or command tests proving the CLI uses the same confidence, degraded, omitted, and recovery semantics as the shared contract.
  - [ ] Add MCP `SearchMemoryToolTests` proving `StructuredContent` contains the Evidence Packet shape and text content remains a serialized fallback.
  - [ ] Add server or mapper tests proving tenant/case scope, empty results, partial backend degradation, all-backend/unauthorized diagnostics, and token-budget omitted metadata map correctly.
  - [ ] Add at least one negative/sanitization assertion for diagnostic packets so internal exception details and secrets are not leaked.

- [ ] Task 6: Documentation and validation (AC: 1, 2, 3, 4)
  - [ ] Update developer-facing CLI/MCP/API documentation if this repo already documents search JSON output or tool schemas.
  - [ ] Document that Evidence Packet confidence is query-result relevance/evidence strength, not factual truth or completeness.
  - [ ] Do not add web UI or Fluent UI implementation in this story. Future web UI composition is a contract consumer only.
  - [ ] Run focused tests for changed projects, then run `git diff --check`.

## Dev Notes

### Current Implementation State

- `src/Hexalith.Memories.Contracts/V1/SearchResult.cs` is the lower-level single-axis search envelope. It already carries `Results`, `TotalCount`, `HasIndexedMemoryUnits`, `Query`, optional `Explanation`, `CaseGroups`, `OmittedCount`, `EstimatedTokensTotal`, `OmittedReason`, `Degraded`, `UnavailableAxes`, and `AxesUsed`.
- `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs` is the lower-level hybrid envelope. It carries fused results, `Degraded`, `UnavailableAxes`, `AllEnabledAxesUnavailable`, `Explanation`, `CaseGroups`, omitted metadata, and `AxesUsed`.
- `src/Hexalith.Memories.Contracts/V1/ScoredResult.cs` and `FusedScoredResult` expose memory unit id, scores, snippets, source uri/type, case metadata, and annotation counts. These are source inputs for Evidence Packet `sources` and `evidence`.
- `src/Hexalith.Memories.Contracts/V1/SearchExplanation.cs` already defines `Caveat`, per-axis details, and optional fusion weights. Reuse this for the packet evidence caveat and explain summary.
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` is the source-generated JSON context for contract types. New public Evidence Packet types must be registered here.
- `src/Hexalith.Memories.Mcp/McpToolResultSerializer.cs` serializes with `MemoriesJsonContext.Options` and currently writes both text JSON and `StructuredContent`. Preserve this pattern because MCP structured results still need a text fallback for compatibility.
- `src/Hexalith.Memories.Mcp/McpErrorMapper.cs` creates sanitized structured tool errors with `code`, `service`, `tool`, `message`, and `suggestion`; it also sets `IsError = true`. Do not regress that behavior.
- `src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs` routes `search_memory` to `SearchAsync` or `HybridSearchAsync` and forwards `tokenBudget`.
- `src/Hexalith.Memories.Cli/SearchQueryCommand.cs` routes search calls and currently keeps JSON output machine-clean while writing degradation notices only for non-JSON formats.
- `src/Hexalith.Memories.Cli/Output/Formatters/SearchResultJsonFormatter.cs` and `HybridSearchResultJsonFormatter.cs` currently pass lower-level search payloads through `CliOutputEnvelope<T>`.
- `src/Hexalith.Memories.Cli/CliJsonContext.cs` combines CLI and memories source-generated JSON metadata. If CLI JSON starts emitting `EvidencePacket`, update this context.
- `src/Hexalith.Memories.Server/Search/SearchResponseMetadataApplier.cs` already applies token-budget truncation, omitted counts, estimated token totals, omitted reasons, degraded flags, unavailable axes, and axes-used metadata.
- `src/Hexalith.Memories.Server/Search/TokenBudgetTruncator.cs` truncates by rank using deterministic token estimates. Reuse its output metadata; do not add a second truncation system.
- `src/Hexalith.Memories.Server/Search/SearchEndpointDegradationResponses.cs` and `SearchEndpointErrorResponseFactory.cs` centralize structured search diagnostics. Prefer mapping these diagnostics into packet state/recovery instead of duplicating error vocabulary.

### Contract Semantics

- Evidence Packet is the cross-surface envelope. Retrieval contracts remain lower-level building blocks.
- Required packet sections from architecture are:
  - `scope`: tenant/case scope, isolation status, and permissions context.
  - `result`: answer or ranked result summary.
  - `sources`: origin id/type, snippets, source confidence/freshness where known.
  - `evidence`: evidence strength, confidence caveat, retrieval axes used, and per-axis score summary.
  - `graph`: related path, edge types, and gap markers when available.
  - `state`: complete, partial, weak, empty, stale, degraded, unauthorized, or pending expansion.
  - `omittedDetails`: omitted count, omitted field names, and deterministic expansion handles.
  - `recovery`: safest next action plus optional secondary actions.
- Confidence and score fields represent query-result relevance and retrieval evidence. They must not be described as factual accuracy, source truthfulness, or completeness guarantees.
- Tenant/case identifiers must be explicit in packet scope so CLI, MCP, and future web UI can render the same trust boundary.
- Use additive JSON contract changes. Preserve existing lower-level property names and camelCase output.

### Project Structure Notes

- Add shared contract records in `src/Hexalith.Memories.Contracts/V1/`.
- Add contract serialization tests in `tests/Hexalith.Memories.Contracts.Tests/V1/`.
- Add CLI JSON coverage near `tests/Hexalith.Memories.Cli.Tests/Cli/SearchResultFormatterTests.cs` and related command/error tests.
- Add MCP structured-content coverage near `tests/Hexalith.Memories.Mcp.Tests/SearchMemoryToolTests.cs`, `McpToolSchemaTests`, and `McpErrorMapperTests`.
- If mapping lives server-side, add mapper/endpoint coverage under `tests/Hexalith.Memories.Server.Tests/`.
- Do not edit root-level or nested submodules for this story.
- Do not add package versions to project files; package versions are centrally managed in `Directory.Packages.props`.
- The repository currently targets `net10.0` with nullable enabled and warnings as errors.

### Previous Story Intelligence

- Story 2.6 added explain mode and confidence-score semantics. Reuse `SearchExplanation` and preserve the caveat-first trust behavior for human-facing explain output.
- Story 7.2 established CLI JSON envelopes that pass through full `SearchResult` and `HybridSearchResult` payloads. Keep JSON output stable or make any new packet member additive.
- Story 7.3 moved degradation notices out of JSON formatters and into command handling for non-JSON output. Do not pollute JSON with stderr-style warning strings.
- Story 10.1 established MCP structured content with shared serialization. Preserve both `StructuredContent` and text JSON fallback.
- Story 10.2 added token-budget metadata to lower-level search contracts. Use that metadata instead of inventing a new truncation model.
- Story 2.8 benchmark artifacts are historical/renumbered and out of scope for this implementation.

### Latest Technical Information

- Microsoft Learn documents source-generation-backed `System.Text.Json` and the use of `JsonTypeInfoResolver.Combine` for combining contracts from multiple source-generated contexts. This matches the existing `MemoriesJsonContext` and `CliJsonContext` pattern.
- The MCP tool specification documents `structuredContent` as a JSON object on tool results and recommends returning serialized JSON in a `TextContent` block for backwards compatibility. This matches the existing MCP serializer and should be preserved.

### Suggested Validation Commands

```powershell
dotnet test tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj --filter "FullyQualifiedName~EvidencePacket|FullyQualifiedName~SearchResultSerializationTests|FullyQualifiedName~HybridSearchResultSerializationTests|FullyQualifiedName~PublicContractSerializationCoverageTests"
dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~SearchResultFormatterTests|FullyQualifiedName~JsonErrorEnvelopeTests|FullyQualifiedName~SearchQueryCommandTests"
dotnet test tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --filter "FullyQualifiedName~SearchMemoryToolTests|FullyQualifiedName~McpToolSchemaTests|FullyQualifiedName~McpErrorMapperTests"
dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~SearchResponseMetadataApplier|FullyQualifiedName~HybridSearchServiceTests|FullyQualifiedName~SearchEndpoint"
git diff --check
```

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 2 and Story 2.7 acceptance criteria.
- `_bmad-output/planning-artifacts/prd.md` - FR23, FR24, FR54, FR55, FR58, FR63, FR66; NFR18, NFR20, NFR24; confidence caveat and token-budget requirements.
- `_bmad-output/planning-artifacts/architecture.md` - Evidence Packet Contract and Contracts.V1 ownership.
- `_bmad-output/planning-artifacts/ux-design-specification.md` - recoverable trust, Evidence Packet invariants, and MCP/CLI bounded-response requirements.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-17-readiness-backlog-structure.md` - Story 2.6A to 2.7 renumbering and benchmark Story 2.8 alias.
- Microsoft Learn: `System.Text.Json` source generation and combined source-generation contexts, https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation.
- Model Context Protocol tools specification: structured content and text fallback, https://modelcontextprotocol.io/specification/2025-06-18/server/tools.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Created from sprint status backlog item `2-7-evidence-packet-contract-mapping`.
- Loaded project config, persistent project context, planning artifacts, current contract/search/CLI/MCP/server code, recent story artifacts, recent git history, and current official documentation for `System.Text.Json` source generation and MCP structured tool results.
- No product code implementation performed in this create-story workflow.

### Completion Notes List

- Ready-for-dev story created on 2026-05-20.
- Story includes the 2.6A to 2.7 alias, the benchmark Story 2.8 collision warning, and implementation guardrails for additive contract mapping.
- Sprint status updated so `2-7-evidence-packet-contract-mapping` is ready for development and `epic-2` is no longer marked done while an Epic 2 story remains unfinished.

### File List

- `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-05-20: Created ready-for-dev story artifact for Evidence Packet Contract Mapping.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.
