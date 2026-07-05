---
baseline_commit: 6935421
---

# Story 23.2: Claim-Check Workflow Payloads

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want large content and vectors kept out of workflow history,
so that history size and replay cost stay bounded for high-throughput ingestion.

Story 23.2 follows Story 23.9 and Story 23.1. The provider batch API, raw-content chunking, and chunked raw semantic vector writes are already done; this story must preserve those behaviors while changing how large payloads move between workflow activities.

## Acceptance Criteria

1. A tenant-scoped claim-check payload contract exists. Given `IngestionWorkflow` currently passes raw bytes, extracted text, chunk text, and vectors through workflow input/output records, when this story completes, then large payloads are stored outside Dapr Workflow history and workflow activity inputs/results carry only slim references containing at least `{ id, sha256Hash, byteLength, contentKind }` plus tenant/memory-unit scope.

2. New ingestion schedules avoid inline file/event payload bytes. Given `IngestionInput.ContentBytes` is currently serialized into the workflow instance for non-URL sources, when REST/file/directory/event ingestion schedules new work, then non-URL payload bytes are claim-checked before or at the scheduling boundary and the workflow input carries a payload reference instead of the byte array. Legacy histories and existing public callers that still deserialize `ContentBytes` must remain compatible.

3. URL fetch, extraction, chunk embedding, and indexing use references instead of large activity payloads. Given `FetchUrlActivity`, `ExtractContentActivity`, `GenerateChunkEmbeddingsActivity`, `IndexSyntacticActivity`, `IndexSemanticChunksActivity`, and `IndexGraphActivity` currently exchange full content and/or vectors, when this story completes, then each producing activity persists its large output and returns a reference, and each consuming activity resolves the reference inside the activity boundary before using the data.

4. Workflow orchestration remains deterministic and side-effect free. Given Dapr Workflow records activity inputs/results in history and replays orchestrator code, when this story completes, then blob reads/writes/deletes happen only in activities or endpoint/scheduler services, not directly in `IngestionWorkflow`. The orchestrator may compose reference records, but must not perform Redis/Dapr state calls, hashing I/O streams, network calls, or wall-clock dependent cleanup decisions.

5. Story 23.1 chunk semantics remain intact. Given raw semantic chunk vectors are stored under `{tenant}:vec:{memoryUnitId}:{seq}` and search deduplicates chunk hits by base memory unit, when claim-checking is added, then chunk sequence, source offsets, chunk text used for indexing/debugging, vector order, embedding provider/model/dimensions, migration marker checks, and rate-limit accounting remain unchanged.

6. Syntactic and graph indexing still persist searchable content. Given RediSearch and FalkorDB currently receive `IndexInput.Content`, when large content is no longer passed through workflow history, then the indexing activities resolve the extracted-content reference and write the same syntactic `content` field and graph node content/contentHash as before. Search, lookup, and consistency behavior must not regress.

7. Natural-language event flow stays compatible. Given event ingestion may generate a natural-language description from the raw event payload and queue retry input on LLM unavailability, when claim-checking is added, then NL description generation resolves the raw payload reference inside an activity boundary, `QueueNaturalLanguageEmbeddingRetryActivity` does not receive a large raw JSON payload through workflow history, and NL semantic indexing keeps its existing key shape and retry semantics.

8. Blob lifecycle is explicit and bounded. Given claim-checked data can otherwise leak, when ingestion succeeds, duplicates after post-index cleanup, compensation, or terminal failure occurs, then transient content/vector blobs are deleted or retained according to an explicit retention policy with TTL. Retention must be tenant-scoped, deterministic from activity inputs, and tested for success, failure, and compensation paths.

9. Failed-unit and re-ingestion boundaries are preserved. Given Story 23.4 separately owns non-URL re-ingestion behavior, when this story completes, then it must not silently claim that file/event re-ingestion is fixed. It may persist enough source payload reference metadata for Story 23.4 to consume later, but any operator-visible re-ingestion behavior change must be either explicitly tested here or left unchanged and documented.

10. Workflow status and authorization surfaces do not expose payloads. Given Story 20.3 already guards workflow and batch status endpoints from leaking raw workflow state, when claim-check references are added, then status endpoints, failed-unit summaries, logs, telemetry tags, and errors expose only IDs, hashes, sizes, stages, and error codes. They must not expose raw text, vectors, source bytes, bearer tokens, or provider secrets.

11. Tests prove history payload slimming, not only behavior. Given A11 is specifically about workflow history size and replay cost, when this story completes, then tests assert the activity inputs/results scheduled by `IngestionWorkflow` do not contain large byte arrays, extracted content strings, chunk text collections, or float vectors. Focused unit tests must also prove hash mismatch rejection, missing blob failure, tenant/case scope validation, cleanup, and legacy `ContentBytes` compatibility.

## Tasks / Subtasks

- [x] Task 1 - Inventory all large workflow payloads before editing (AC: 1-3, 7, 11)
  - [x] Read `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` and list every activity call currently carrying `ContentBytes`, `UrlFetchResult.ContentBytes`, `ExtractionResult.ExtractedContent`, `EmbeddingInput.ContentText`, `ChunkEmbeddingBatchResult.Chunks[*].Text`, `ChunkEmbeddingBatchResult.Chunks[*].Vector`, `IndexInput.Content`, `IndexInput.EmbeddingVector`, and `SemanticChunkIndexInput.Chunks[*].Vector`.
  - [x] Read direct consumers before changing contracts: `FetchUrlActivity`, `ExtractContentActivity`, `GenerateChunkEmbeddingsActivity`, `IndexSyntacticActivity`, `IndexSemanticChunksActivity`, `IndexGraphActivity`, `GenerateNaturalLanguageDescriptionActivity`, `QueueNaturalLanguageEmbeddingRetryActivity`, `PersistFailedUnitActivity`, `FailedUnitsRegistry`, `ReIngestionCoordinator`, `DirectoryIngestionService`, and REST ingestion scheduling in `Program.cs`.
  - [x] Preserve Story 18.6 `MemoryUnitId` stability and Story 23.1 chunk identities. Claim-check IDs must not replace memory unit IDs, chunk sequences, dedup keys, source URI lookup keys, graph node IDs, or semantic vector keys.

- [x] Task 2 - Add payload reference and store abstractions (AC: 1, 4, 8, 10)
  - [x] Add focused records such as `WorkflowPayloadReference`, `WorkflowPayloadKind`, and small stage-specific inputs/results under the existing server ingestion/activity folders. Keep one C# type per file and add source-generation metadata if any record is serialized by Dapr Workflow or public endpoints.
  - [x] Add a `IWorkflowPayloadStore` or equivalent helper that can save, read, verify, and delete tenant-scoped payloads by reference. Prefer existing Dapr state-store patterns when practical; if Redis direct access is used, keep keys tenant-prefixed and covered by tests.
  - [x] Persist hash and length with each payload and verify both on read. A missing payload, wrong tenant, wrong hash, wrong length, or unsupported kind must fail with a structured, non-secret exception that maps cleanly through existing workflow failure handling.
  - [x] Define key shape and retention metadata explicitly, for example `"{tenant}:workflow-payload:{memoryUnitId}:{kind}:{hash}"`, without relying on unbounded scans for normal reads.

- [x] Task 3 - Slim scheduling and validation for inline payloads (AC: 1, 2, 10, 11)
  - [x] Add an additive `IngestionInput` reference field or server-only scheduling input that lets new non-URL workflows carry a payload reference instead of `ContentBytes`.
  - [x] Update REST ingestion and `DirectoryIngestionService` scheduling so new file/event/command/projection/discussion ingests do not place large `ContentBytes` in Dapr Workflow input. Keep `ContentBytes` accepted for backward compatibility and old histories.
  - [x] Update `IngestionInputValidator` and tests so non-URL ingestion requires either valid inline `ContentBytes` or a valid payload reference, never neither. URL ingestion must still reject inline bytes and fetch through `FetchUrlActivity`.
  - [x] Keep audit query params and telemetry size tags based on declared or referenced payload length, without logging or returning payload content.

- [x] Task 4 - Convert fetch and extraction to claim-check outputs (AC: 1, 3, 4, 6, 8)
  - [x] For URL sources, make `FetchUrlActivity` persist fetched bytes and return a slim fetch result containing final URL, content type, length, HTTP status, and payload reference.
  - [x] Make `ExtractContentActivity` read source bytes from a reference when present, extract through the existing Kreuzberg path, persist extracted text, and return a slim extraction result with content hash, extracted length, extracted timestamp, and extracted-text reference.
  - [x] Preserve the existing `ExtractionInput`/`ExtractionResult` shape for replay compatibility, or add new records and have `IngestionWorkflow` call the new records only for new schedules.
  - [x] Ensure extraction failures and validation errors clean up any source-byte blobs whose lifecycle is owned by this workflow, unless retention policy says the blob must be retained for diagnostics.

- [x] Task 5 - Convert chunk embedding and semantic indexing payloads (AC: 1, 3, 5, 8, 11)
  - [x] Change raw payload embedding to accept an extracted-text reference. `GenerateChunkEmbeddingsActivity` must resolve text inside the activity, chunk it exactly as Story 23.1 does today, call `EmbeddingClient.GenerateBatchAsync(...)`, persist any large chunk text/vector payload needed by downstream activities, and return only chunk metadata plus references.
  - [x] Keep `ContentChunkingOptions.MaxChunksPerBatch`, rate-limit consumption per provider batch, provider/model/dimensions metadata, and migration marker write-block checks intact.
  - [x] Change `IndexSemanticChunksActivity` to resolve chunk text/vector references inside the activity, write the same Redis chunk hashes and fields as Story 23.1, and reject hash/length mismatches before any Redis vector write.
  - [x] Add tests that fail if `ChunkEmbeddingBatchResult` or `SemanticChunkIndexInput` still carries raw `float[]` vectors or large chunk text for new workflow paths.

- [x] Task 6 - Convert syntactic, graph, and NL payload consumers (AC: 3, 6, 7, 10)
  - [x] Change syntactic and graph indexing inputs so the workflow passes extracted-content reference metadata instead of `IndexInput.Content` for new paths. Activities should resolve content internally and persist the same RediSearch and FalkorDB data as before.
  - [x] Keep `ContentHash`, `SourceUri`, `SourceType`, metadata comparer behavior, `EmbeddingProvider`, `EmbeddingModel`, and `EmbeddingDimensions` threading unchanged.
  - [x] For `SourceType.Event`, stop decoding raw JSON from `input.ContentBytes` in the workflow. Resolve event raw payload in an activity before `GenerateNaturalLanguageDescriptionActivity` and avoid queueing large raw JSON through workflow history when NL retry is needed.
  - [x] Preserve NL sibling cleanup and natural-language semantic key shape from Stories 9.2 and 21.3.

- [x] Task 7 - Cleanup, failure, and compatibility hardening (AC: 8-10)
  - [x] Add cleanup activity coverage for transient source bytes, fetched URL bytes, extracted text, chunk text, and vector payload blobs. Cleanup must run on success, duplicate-after-index compensation, indexing compensation, and failed terminal paths as appropriate.
  - [x] Decide and document whether failed workflows retain any source/extracted references for Story 23.4. If retained, use explicit TTL and failed-unit metadata; if not retained, leave re-ingestion behavior unchanged and make the limitation visible in dev notes/tests.
  - [x] Keep `FailedUnitInput`, `FailedUnitRecord`, `FailedUnitsRegistry`, and `ReIngestionCoordinator` compatible with existing failed-unit hashes. Any new persisted fields must be optional on read so old hashes still parse.
  - [x] Ensure status endpoints and batch status responses never serialize blob values. They may show hashes, lengths, and payload IDs only where useful and non-sensitive.

- [x] Task 8 - Focused tests and validation evidence (AC: 1-11)
  - [x] Add payload-store unit tests for save/read/hash mismatch/missing blob/delete/TTL metadata/tenant mismatch.
  - [x] Update workflow tests to assert new activity call payloads are slim and still call activities in the correct order.
  - [x] Update activity tests for fetch, extraction, chunk embedding, semantic indexing, syntactic indexing, graph indexing, NL description/retry, failed-unit persistence, and re-ingestion compatibility where touched.
  - [x] Add serialization/source-generation completeness tests for new Dapr Workflow payload records.
  - [x] Run `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run focused xUnit v3 tests for workflow, ingestion activities, indexing activities, failed-unit/re-ingestion, and payload store. If VSTest is blocked by the known sandbox TCP-listener issue, use the established `DiffEngine_Disabled=true dotnet exec ...Hexalith.Memories.Server.Tests.dll` fallback and record exact counts.
  - [x] Run `git diff --check` and record the existing CRLF caveat only if it recurs.

## Dev Notes

### Current State and Code Anchors

`IngestionWorkflow` still carries large values through multiple history events. For the current post-23.1 path, large values appear in the workflow input (`IngestionInput.ContentBytes` for non-URL sources), URL fetch result (`UrlFetchResult.ContentBytes`), extraction output (`ExtractionResult.ExtractedContent`), raw embedding input (`EmbeddingInput.ContentText`), raw chunk embedding result (`ChunkEmbeddingBatchResult.Chunks[*].Text` and `Vector`), syntactic/graph indexing (`IndexInput.Content` and first `EmbeddingVector`), and semantic chunk indexing (`SemanticChunkIndexInput.Chunks[*].Vector`). [Source: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`; `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`; `src/Hexalith.Memories.Contracts/V1/ExtractionResult.cs`; `src/Hexalith.Memories.Server/Activities/Ingestion/ChunkEmbeddingResult.cs`]

The audit finding for this story is A11: full content plus vectors are serialized into workflow history 6-8 times per document, causing multi-MB history per document and GB-scale churn for large batches. The accepted remediation is a claim-check pattern between activities. [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A11`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-23`]

`IngestionInputValidator` currently requires `ContentBytes` for every non-URL source and rejects URL bytes because the workflow fetches URL bodies itself. Story 23.2 must loosen this only by adding a valid payload-reference alternative; it must not allow non-URL workflows with neither content nor reference. [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/IngestionInputValidator.cs`]

`DirectoryIngestionService` currently reads each file into memory and schedules the workflow with `ContentBytes = bytes`, then rewrites batch state per scheduled file. Story 23.2 may remove workflow-inline bytes here, but Story 23.6 owns the broader directory batching O(n^2), bounded-parallel scheduling, and extension allowlist work. [Source: `src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs`; `_bmad-output/planning-artifacts/epics.md#Story-23.6`]

`IndexSyntacticActivity` persists the full extracted content into the syntactic hash field `content`; `IndexGraphActivity` persists content/contentHash into FalkorDB memory-unit nodes. Claim-checking must move content transfer out of workflow history, not remove searchable persisted content. [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs`; `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs`]

Story 23.1 replaced one raw vector with chunked semantic vectors. `GenerateChunkEmbeddingsActivity` chunks extracted text, calls `EmbeddingClient.GenerateBatchAsync(...)`, consumes rate limits per provider batch, and returns ordered `ChunkEmbeddingResult` records. `IndexSemanticChunksActivity` writes each chunk under `IndexSchemaDefinitions.BuildSemanticChunkKey(input.TenantId, input.MemoryUnitId, chunk.Sequence)` with `chunkText`, offsets, provider/model/dimensions, and the raw vector bytes. Preserve this semantic storage contract. [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md`; `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs`; `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticChunksActivity.cs`]

Failed-unit persistence currently stores no source payload pointer. `PersistFailedUnitActivity`, `FailedUnitsRegistry`, and `FailedUnitRecord` store tenant/case/source/stage/error metadata only; `ReIngestionCoordinator` rebuilds non-URL workflows with `ContentBytes = null`, which is exactly the Story 23.4 gap. Do not mark non-URL re-ingestion fixed in this story unless you deliberately implement and test that operator behavior. [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/PersistFailedUnitActivity.cs`; `src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs`; `src/Hexalith.Memories.Server/Ingestion/ReIngestionCoordinator.cs`; `_bmad-output/planning-artifacts/epics.md#Story-23.4`]

### Architecture Constraints

- Activities are the allowed side-effect boundary. Dapr docs describe workflow activities as the units that call services, state stores, pub/sub brokers, and third-party services; this repository's architecture says workflows orchestrate and activities perform I/O. [Source: `_bmad-output/planning-artifacts/architecture.md#DAPR-Workflow-Patterns`; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/]
- Workflow orchestration must be replay-safe: no direct state store I/O, network calls, random/wall-clock behavior, or hidden mutable process configuration inside `IngestionWorkflow`. [Source: `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`]
- Dapr state supports key/value state, ETag-based optimistic concurrency, consistency metadata, bulk/transaction operations, and per-state TTL. If the payload store uses Dapr state, set TTL intentionally for transient blobs rather than relying on indefinite retention. [Source: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/]
- Tenant isolation remains physical. Payload keys must be tenant-scoped and must not rely on filters alone. Tenant IDs must remain explicit through workflow, activity, storage, telemetry, and tests. [Source: `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`]
- Central package management is mandatory. Do not add package versions to `.csproj`; use `Directory.Packages.props` if a new dependency is truly required. Prefer existing BCL, Dapr client, Redis, and System.Text.Json patterns. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`]
- Keep contract changes additive. Existing workflow histories may deserialize old records that contain `ContentBytes`, `ExtractionResult.ExtractedContent`, or `EmbeddingInput.ContentText`; do not break replay of in-flight or historical workflows casually. [Source: `_bmad-output/project-context.md#Critical-Implementation-Rules`]

### Previous Story Intelligence

Story 23.9 is done. Reuse `EmbeddingClient.GenerateBatchAsync(...)`; do not reintroduce provider-specific branching into workflows or activities. Keep the `EmbeddingClient` facade and tests compatible unless a narrow, tested change is necessary. [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md`]

Story 23.1 is done. It added deterministic content chunking, `GenerateChunkEmbeddingsActivity`, `IndexSemanticChunksActivity`, chunk-suffixed semantic keys, semantic result dedupe, graph-scope chunk expansion, and consistency/cleanup/migration compatibility. Claim-checking must preserve those invariants and should add payload-reference tests around them rather than refactor retrieval again. [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md`]

Story 23.1 senior review fixed three important issues: whitespace-only chunk windows must fail deterministically, provider embedding batches are bounded by `MaxChunksPerBatch`, and graph-scoped semantic chunk hits are deduped before pagination. Do not loosen any of those contracts while moving chunk payloads behind references. [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md#Senior-Developer-Review-AI`]

Carry-forward from Epic 22 still applies: semantic pagination, graph-scoped search, fusion case attribution, case-scoped traversal, post-filter recall, and NL axis wiring must not regress while ingestion output contracts change. [Source: `_bmad-output/implementation-artifacts/22-1-semantic-axis-pagination.md`; `_bmad-output/implementation-artifacts/22-3-graph-scoped-and-hybrid-pagination-correctness.md`; `_bmad-output/implementation-artifacts/22-6-post-filter-recall.md`; `_bmad-output/implementation-artifacts/22-7-retrieval-feature-completion.md`]

### Git Intelligence

Recent commits before story creation:

- `6935421 feat(story-23.1): Content Chunking & Batch Embedding`
- `a645b96 feat(story-23.9): EmbeddingClient Provider Strategy`
- `ae8bb1e docs(epic-22): close retrospective and sync retrieval docs`
- `28ab3d3 feat(story-22.7): Retrieval Feature Completion`
- `df3c9b6 feat(story-22.6): Post-Filter Recall`

Pattern: Epic 22 and 23 stories are source-anchored, heavily tested, and explicit about validation blockers. Continue that pattern. This story is a behavioral scalability fix with user-visible ingestion throughput/reliability impact; if committed later, a `feat(story-23.2): ...` commit type is likely appropriate.

### Latest Technical / Library Notes

- Local project context pins Dapr packages at `1.18.4`; current Dapr docs show v1.18 as latest stable and v1.19 as preview. Do not chase preview APIs for this story. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/]
- Dapr Workflow is stateful and records workflow execution state/history; activities are the intended side-effect boundary. Claim-checking should therefore reduce activity input/result payloads, not hide large values in another workflow record. [Source: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/]
- Dapr state TTL exists for per-state expiration. Use it deliberately if payloads are transient, and test metadata/configuration so blobs do not live forever by accident. [Source: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/]
- No package upgrade is required for this story.

### Scope Boundaries

In scope:
- Claim-check reference contracts and payload store for workflow-carried content/vector payloads.
- Slim new workflow/activity inputs and outputs for source bytes, fetched URL bytes, extracted text, raw chunk text, raw chunk vectors, syntactic/graph indexing content, and NL raw event payload references.
- Scheduling changes needed to prevent new non-URL workflow inputs from embedding large `ContentBytes`.
- Cleanup and retention policy for claim-checked transient blobs.
- Tests that assert activity payloads are slim and behavior remains equivalent.

Out of scope:
- Changing the Story 23.1 chunking algorithm, raw semantic key shape, semantic search dedupe, graph-scope expansion, migration marker policy, or retrieval ranking behavior.
- Durable `Retry-After` timers for provider 429s. That is Story 23.3.
- Making failed non-URL re-ingestion work end-to-end. That is Story 23.4 unless explicitly implemented and tested here.
- Rate-limiter API redesign or Redis Lua token bucket. That is Story 23.5.
- Directory batch checkpointing/bounded-parallel scheduling and extension allowlist. That is Story 23.6.
- Index provisioning memoization and `Thread.Sleep` cleanup. That is Story 23.7.
- Workflow config determinism beyond avoiding new replay hazards. That is Story 23.8.
- Adding new embedding providers, changing tenant provider config, or mutating Redis vector dimensions.
- UI/web work and submodule changes.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`.
- Put tests under matching feature folders: `Activities/Ingestion`, `Activities/Indexing`, `Ingestion`, `Workflows`, `NaturalLanguage`, and failed-unit/re-ingestion folders as touched.
- Do not require live Google, Ollama, or external network calls. Payload-store tests should use substitutes or repository-standard Redis/Dapr fixtures.
- Add negative tests for missing payload, hash mismatch, wrong tenant, wrong kind, and cleanup idempotency.
- Use serialization/source-generation tests for every new Dapr Workflow input/result record.
- If normal `dotnet test` is blocked by the known VSTest TCP-listener sandbox issue, use the established xUnit v3 in-process `dotnet exec` fallback and record exact counts.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story-23.2` - story statement and A11 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-23` - approved A11 remediation scope]
- [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A11` - finding: content plus vectors serialized repeatedly into workflow history]
- [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md` - prerequisite provider batch API]
- [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md` - immediate previous story and chunked semantic invariants]
- [Source: `_bmad-output/project-context.md` - .NET 10/C# 14, Dapr 1.18.4, Redis/FalkorDB, testing, workflow, and tenant-isolation rules]
- [Source: `_bmad-output/planning-artifacts/architecture.md#DAPR-Workflow-Patterns` - workflows orchestrate, activities perform I/O]
- [Source: `_bmad-output/planning-artifacts/prd.md#Async-Ingestion-Pipeline` - ingestion stage and NFR5 context]
- [Source: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` - current large workflow payload flow]
- [Source: `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs` - current inline content bytes contract]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/IngestionInputValidator.cs` - current non-URL content validation]
- [Source: `src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs` - current directory scheduling with inline bytes]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/FetchUrlActivity.cs` - URL body fetch boundary]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/ExtractContentActivity.cs` - extraction boundary]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs` - chunking and batch embedding boundary]
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs` - syntactic content persistence]
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticChunksActivity.cs` - chunk vector persistence]
- [Source: `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs` - graph content persistence]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/PersistFailedUnitActivity.cs` - current failed-unit persistence]
- [Source: `src/Hexalith.Memories.Server/Ingestion/ReIngestionCoordinator.cs` - current non-URL re-ingestion limitation]
- [Source: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/ - Dapr Workflow activities and current v1.18 docs]
- [Source: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/ - Dapr state and TTL behavior]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (create-story context engineering)

### Debug Log References

- 2026-07-05: Loaded repository AGENTS instructions, `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`, `.agents/skills/bmad-dev-story/SKILL.md`, `.agents/skills/bmad-dev-story/checklist.md`, BMM config, and workflow customization before implementation.
- 2026-07-05: Loaded project context from root and root-declared reference project contexts; no submodules were initialized or updated.
- 2026-07-05: Inventoried workflow-carried large payloads in `IngestionWorkflow` and direct activity/scheduler consumers before contract edits.
- 2026-07-05: Implemented tenant-scoped claim-check contracts, Dapr state payload store, scheduling helpers, activity-side reference resolution, transient cleanup activity, and slim workflow orchestration paths.
- 2026-07-05: Preserved legacy inline `ContentBytes`, additive extraction/indexing/NL contracts, Story 23.1 chunk sequence/key semantics, and Story 23.4 non-URL re-ingestion boundary.
- 2026-07-05: Validation: server build passed; server test build passed; `dotnet test` was blocked by sandbox TCP listener permission; xUnit v3 in-process fallback passed focused and full server lanes; contract build and serialization coverage passed; `git diff --check` passed.
- 2026-07-05: Senior developer review completed with automatic fixes for duplicate-path payload cleanup and File List hygiene.

### Completion Notes List

- Added `WorkflowPayloadReference` and `WorkflowPayloadKind` to public contracts, with additive reference fields on ingestion, extraction, URL fetch, indexing, and natural-language retry inputs.
- Added a Dapr state-backed workflow payload store with tenant-prefixed keys, TTL metadata, SHA-256 and byte-length verification, structured non-secret failure codes, and delete support.
- Updated REST, directory, workflow scheduler, and annotation ingestion scheduling so new non-URL work claim-checks source bytes before workflow start while preserving legacy inline compatibility.
- Converted URL fetch, extraction, chunk embedding, semantic indexing, syntactic indexing, graph indexing, and natural-language event/retry flows to resolve large payloads inside services or activities instead of the orchestrator.
- Added transient cleanup for source/fetched bytes, extracted text, chunk text, and vector payload references across success, duplicate-after-index, compensation, and terminal failure paths. Failed-unit and re-ingestion persisted contracts remain unchanged; Story 23.4 still owns non-URL re-ingestion behavior.
- Senior review fixed the pre-validation duplicate path so claim-checked source payloads are cleaned when idempotency short-circuits before extraction.
- Added payload-store tests, validator tests, slim workflow-history assertions, and fallback validation coverage for touched activity/re-ingestion paths.
- Validation results:
  - `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` passed.
  - `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed.
  - `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter ...` blocked before discovery with `System.Net.Sockets.SocketException (13): Permission denied`.
  - `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class ...WorkflowPayloadStoreTests -class ...IngestionInputValidatorTests -class ...IngestionWorkflowTests` passed: total 65, failed 0, skipped 0.
  - `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` passed: total 2325, failed 0, skipped 1.
  - `dotnet build tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed.
  - `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.IngestionInputSerializationTests -class Hexalith.Memories.Contracts.Tests.V1.PublicContractSerializationCoverageTests` passed: total 158, failed 0.
  - `git diff --check` passed.

### File List

- `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-20-20260704-091304.md`
- `src/Hexalith.Memories.Contracts/V1/ExtractionInput.cs`
- `src/Hexalith.Memories.Contracts/V1/ExtractionResult.cs`
- `src/Hexalith.Memories.Contracts/V1/IndexInput.cs`
- `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`
- `src/Hexalith.Memories.Contracts/V1/NaturalLanguageDescriptionInput.cs`
- `src/Hexalith.Memories.Contracts/V1/QueueNaturalLanguageEmbeddingRetryInput.cs`
- `src/Hexalith.Memories.Contracts/V1/UrlFetchResult.cs`
- `src/Hexalith.Memories.Contracts/V1/WorkflowPayloadKind.cs`
- `src/Hexalith.Memories.Contracts/V1/WorkflowPayloadReference.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/ScheduleAnnotationIngestionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticChunksActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/ResolvedSemanticChunk.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/ChunkEmbeddingResult.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/CleanupWorkflowPayloadsActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/CleanupWorkflowPayloadsInput.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingInput.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/ExtractContentActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/FetchUrlActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/IngestionInputValidator.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/QueueNaturalLanguageEmbeddingRetryActivity.cs`
- `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs`
- `src/Hexalith.Memories.Server/Ingestion/DaprWorkflowPayloadStore.cs`
- `src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs`
- `src/Hexalith.Memories.Server/Ingestion/IWorkflowPayloadStore.cs`
- `src/Hexalith.Memories.Server/Ingestion/IngestionPayloadClaimCheck.cs`
- `src/Hexalith.Memories.Server/Ingestion/WorkflowPayloadException.cs`
- `src/Hexalith.Memories.Server/Ingestion/WorkflowPayloadStoreEntry.cs`
- `src/Hexalith.Memories.Server/Ingestion/WorkflowPayloadStoreOptions.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/ExtractContentActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/FetchUrlActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateChunkEmbeddingsActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/IngestionInputValidatorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/QueueNaturalLanguageEmbeddingRetryActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionPayloadClaimCheckTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/WorkflowPayloadStoreTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs`

### Senior Developer Review (AI)

Reviewer: Codex on 2026-07-05

Outcome: Approved after automatic fixes.

Findings fixed:

- HIGH: `IngestionWorkflow` added claim-checked source payloads to the cleanup list only after idempotency. A duplicate detected before validation returned without deleting the scheduler-created source blob. Fixed by tracking `input.PayloadReference` before idempotency and invoking `CleanupWorkflowPayloadsActivity` on the duplicate return path. Regression coverage added in `RunAsync_DuplicateClaimCheckedSource_ShouldCleanupInputPayloadReference`.
- MEDIUM: The story File List did not match git reality. Several changed test files, `IngestionPayloadClaimCheckTests`, test-summary, and story-automator files were missing. Fixed by updating this File List.

Review validation:

- Dapr workflow/state documentation fallback checked: Dapr v1.18 docs still place state store and external I/O in workflow activities and document state TTL support.
- Acceptance criteria cross-check found the main claim-check flow implemented with activity-boundary reads/writes, slim workflow payloads, tenant-scoped references, hash/length validation, and cleanup coverage after the duplicate-path fix.
- `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` passed with one transient MSB3026 copy retry warning.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~IngestionWorkflowTests" --logger "console;verbosity=minimal"` blocked before discovery with `System.Net.Sockets.SocketException (13): Permission denied`.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class ...WorkflowPayloadStoreTests -class ...IngestionPayloadClaimCheckTests -class ...IngestionInputValidatorTests -class ...ExtractContentActivityTests -class ...FetchUrlActivityTests -class ...GenerateChunkEmbeddingsActivityTests -class ...QueueNaturalLanguageEmbeddingRetryActivityTests -class ...IndexSyntacticActivityTests -class ...IngestionWorkflowTests` passed: total 97, failed 0, skipped 0.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` passed: total 2333, failed 0, skipped 1.
- `dotnet build tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.IngestionInputSerializationTests -class Hexalith.Memories.Contracts.Tests.V1.PublicContractSerializationCoverageTests` passed: total 158, failed 0, skipped 0.
- `git diff --check` passed.

## Change Log

- 2026-07-05: Implemented Story 23.2 claim-check workflow payloads and moved status to review.
- 2026-07-05: Senior developer review completed with automatic fixes; status moved to done.
