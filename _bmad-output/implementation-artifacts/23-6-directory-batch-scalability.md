---
baseline_commit: 8dced2b
---

# Story 23.6: Directory-Batch Scalability

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want directory batches scheduled efficiently with an extension allowlist,
so that large batches do not stall on O(n^2) state writes or waste budget on unsupported files.

Story 23.6 follows Story 23.9, Story 23.1, Story 23.2, Story 23.3, Story 23.4, and Story 23.5 in `story_execution_order.epic-23`. Provider strategy, chunked batch embedding, claim-check payloads, durable provider 429 retry, non-URL re-ingestion, and single-operation embedding admission are already done. This story closes A33 by making directory scheduling scalable without changing the downstream ingestion workflow semantics.

## Acceptance Criteria

1. Batch state is checkpointed instead of fully rewritten per file. Given `DirectoryIngestionService.IngestAsync` currently saves a full `DirectoryBatchState` before scheduling and after every scheduled or unreadable file, when a directory of N accepted files is ingested, then the implementation avoids O(n^2) serialized state churn. Persist an initial state, bounded checkpoints, and a final state, or use an equivalent append/checkpoint design with a clear bound. Tests must prove save calls do not scale linearly with every scheduled file at the default checkpoint size.

2. Directory scheduling is bounded-parallel. Given scheduling one workflow per candidate is currently sequential, when multiple accepted files are present, then file read, claim-check preparation, and workflow scheduling run with a bounded degree of parallelism. The bound must be configurable or derived from an existing safe setting, must clamp to a positive value, must honor cancellation, and must not create unbounded tasks for large directories.

3. `SupportedExtensions` is the effective extension allowlist. Given `IngestionSettings.SupportedExtensions` exists but is unused and `UnsupportedExtensions` is currently the only filter, when a file extension is not in `SupportedExtensions`, then the file is skipped with `UNSUPPORTED_EXTENSION` before bytes are read or payloads are saved. Extension comparison must be case-insensitive and normalized to lowercase with a leading dot. `UnsupportedExtensions` can remain as a denylist overlay, but it must not allow unknown extensions through.

4. Directory security and validation remain unchanged. Given path validation resolves reparse points and enforces `AllowedDirectoryRoots`, when batch scalability changes are made, then root allowlist, traversal rejection, outside-root skipping, inaccessible-file handling, zero-byte skipping, max-size skipping, `MaxBatchSize`, and skipped-report truncation behavior remain compatible.

5. Claim-check and workflow input semantics are preserved. Given Story 23.2 moved non-URL source bytes into `IWorkflowPayloadStore`, when directory files are scheduled, then raw bytes are not placed in workflow history when the payload store is available, `PayloadReference` remains tenant and requested-instance scoped, `SourceType.File`, content type inference, request metadata, causation id, and batch correlation id are preserved for every scheduled file.

6. Partial scheduling failures have safe state and cleanup semantics. Given the existing service returns a non-success result if Dapr scheduling fails after some work, when a parallel scheduling task fails, then the returned error remains `DAPR_UNAVAILABLE` or `BATCH_SCHEDULING_FAILED` as appropriate, persisted batch state does not claim unscheduled files as enqueued, and any payload reference created for a file whose workflow was not scheduled is deleted or otherwise proven not to leak. Cancellation must not leave a successful batch response.

7. Batch response and status contracts stay backward compatible. Given `DirectoryIngestionOutcome`, `DirectoryBatchState`, `BatchFileRef`, and `GET /api/ingest/batches/{batchId}` are existing contract surfaces, when the state persistence approach changes, then accepted responses still include batch id, discovered count, enqueued count, skipped list/truncation flag, instance ids, tenant id, and case id. Status lookup must still resolve source URI per instance and count queued/extracting/embedding/indexing/indexed/failed correctly.

8. Bounded parallelism preserves deterministic accounting. Given workflow scheduling can complete out of order under bounded parallelism, when the batch completes, then `InstanceIds` and `Files` are complete, duplicate-free, and stable enough for status lookup and tests. If order changes, it must be explicitly documented and tests must avoid depending on filesystem enumeration order unless the service sorts candidates deliberately.

9. Tests prove A33 is closed. Given A33 names O(n^2) state writes, sequential scheduling, and unused `SupportedExtensions`, when the story completes, then tests cover checkpoint frequency, bounded parallel scheduling behavior, allowlist-only filtering, unsupported-extension skip before read/payload save/schedule, preservation of path security rules, state contents after unreadable/skipped files, and failure cleanup for payloads created before a scheduling failure.

10. Documentation and story evidence explain the operational bound. Given directory ingestion is an operator-facing API, when implementation completes, then this story's Dev Agent Record and any touched operations or config docs state the chosen scheduling parallelism, checkpoint cadence, default settings, and validation commands. Do not add package upgrades or submodule changes.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm A33 and current directory ingestion behavior before editing (AC: 1-10)
  - [x] Read `src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs` completely. Confirm current full-state save points: initial save, unreadable-file save, and per-scheduled-file save.
  - [x] Read `src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs` completely. Confirm `SupportedExtensions` exists and is not currently used by `DirectoryIngestionService`.
  - [x] Read `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs`, `IIngestionWorkflowScheduler.cs`, `IngestionPayloadClaimCheck.cs`, and `IWorkflowPayloadStore.cs`.
  - [x] Read `Program.cs` directory ingest and batch-status endpoints so response/error mapping and tenant authorization behavior are preserved.
  - [x] Read existing tests: `DirectoryIngestionServiceTests`, `DirectoryIngestionPathValidationTests`, `DirectoryBatchStatusMapperTests`, `IngestionEndpointLogTests`, and relevant authorization/status endpoint tests.

- [x] Task 2 - Add directory batch scalability settings with safe defaults (AC: 1-3, 8-10)
  - [x] Add focused settings such as `DirectorySchedulingParallelism` and `DirectoryBatchCheckpointSize` to `IngestionSettings`, or deliberately reuse an existing safe setting if it truly matches scheduling work.
  - [x] Clamp the scheduling parallelism to a positive bounded value. Do not allow zero, negative, or very large configured values to create unbounded task fan-out.
  - [x] Clamp checkpoint size to a positive bounded value. The default should reduce state writes for the current `MaxBatchSize = 500` while still exposing progress for status reads.
  - [x] Update `src/Hexalith.Memories.Server/appsettings.json` only if the new defaults need to be visible in config. Keep default directory ingestion disabled by `AllowedDirectoryRoots: []`.

- [x] Task 3 - Make `SupportedExtensions` the allowlist (AC: 3-4, 9)
  - [x] Normalize configured supported and unsupported extensions once per request or through a small helper. Accept both `.txt` and `txt` config values by normalizing to `.txt`, or reject malformed config with a documented fallback.
  - [x] Skip any extension not in `SupportedExtensions` as `UNSUPPORTED_EXTENSION` before opening or reading file bytes.
  - [x] Preserve the existing denylist behavior for `UnsupportedExtensions` as a stricter overlay if both settings are present.
  - [x] Add tests for uppercase supported extensions, unknown extensions, denylisted extensions, extensionless files, and the guarantee that unsupported files are not scheduled or claim-checked.

- [x] Task 4 - Replace per-file full-state rewrites with bounded checkpointing (AC: 1, 6-9)
  - [x] Introduce a small batch progress accumulator or checkpoint helper so `DirectoryBatchState` creation is not duplicated through the scheduling loop.
  - [x] Persist initial batch state before scheduling begins.
  - [x] Persist progress only after every configured checkpoint interval, after unreadable-file discoveries that need to be visible, and at final completion. It is acceptable for skipped entries to accumulate until the next checkpoint as long as the final state is complete and tests pin the chosen behavior.
  - [x] Keep `ttlInSeconds` metadata on every state save.
  - [x] If a state-save failure happens after some workflows were scheduled, preserve the existing non-success behavior and do not fabricate a successful accepted batch.
  - [x] Add tests proving save calls are bounded for a multi-file batch and that final state contains every scheduled file and bounded skipped entry.

- [x] Task 5 - Implement bounded-parallel scheduling safely (AC: 2, 5-8)
  - [x] Prefer reusing `IIngestionWorkflowScheduler` so directory ingestion uses the same claim-check-and-schedule path as re-ingestion. Because `DirectoryIngestionService` is currently public and `IIngestionWorkflowScheduler` is internal, choose a valid accessibility/DI path: make the service internal if appropriate, add an internal constructor plus public compatibility constructor, or introduce an equivalent public/internal-safe seam. Do not create a public constructor with a less-accessible parameter.
  - [x] Use `SemaphoreSlim`, `Parallel.ForEachAsync`, `Channel`, or another simple bounded pattern. Do not create one unbounded task per candidate for large batches.
  - [x] Preserve per-file input fields: tenant id, case id, source URI, inferred content type, `SourceType.File`, ingested by, cloned metadata, causation id, and `CorrelationId = batchId`.
  - [x] Preserve requested instance id generation using ULID-style values and use that id for payload claim-check scoping.
  - [x] Ensure cancellation flows into file reads, payload saving, workflow scheduling where supported, checkpoint saves, and cleanup.
  - [x] If order matters, sort candidates before scheduling. If order does not matter, document that `InstanceIds`/`Files` are complete but not filesystem-order guaranteed.

- [x] Task 6 - Handle partial failures and payload cleanup (AC: 5-7, 9)
  - [x] Track payload references created before workflow scheduling succeeds.
  - [x] If scheduling fails after claim-check preparation but before a workflow is accepted, delete that file's source payload reference using `IWorkflowPayloadStore.DeleteAsync`.
  - [x] On a batch-level failure, do not delete payloads for workflows that were successfully scheduled; the workflow owns their lifecycle.
  - [x] Keep `DAPR_UNAVAILABLE` for `DaprException` and `BATCH_SCHEDULING_FAILED` for other scheduling failures.
  - [x] Add tests for a scheduling failure after one payload save and before schedule success, proving unscheduled payload cleanup and no false enqueued state.

- [x] Task 7 - Preserve endpoint/status compatibility (AC: 4, 7-8)
  - [x] Re-run existing endpoint and authorization tests impacted by `DirectoryBatchState` or service constructor changes.
  - [x] Ensure `GET /api/ingest/batches/{batchId}` still reads the state key `ingestion-batch:{batchId}` and maps every `BatchFileRef` through `DirectoryBatchStatusMapper`.
  - [x] Do not rename public JSON fields in `DirectoryIngestionRequest`, `DirectoryIngestionOutcome`, `BatchStatusResponse`, or `BatchInstanceStatus`.

- [x] Task 8 - Focused validation evidence (AC: 1-10)
  - [x] Run `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run focused xUnit v3 tests for directory ingestion service, path validation, batch status mapping, endpoint authorization/status, ingestion payload claim-check, and any new scheduler/checkpoint helper tests. If VSTest is blocked by the known sandbox TCP-listener issue, use the established `DiffEngine_Disabled=true dotnet exec ...Hexalith.Memories.Server.Tests.dll` fallback and record exact counts.
  - [x] Run `git diff --check`.
  - [x] Record the checkpoint cadence, scheduling parallelism, and validation results in this story's Dev Agent Record.

## Dev Notes

### Current State and Code Anchors

`DirectoryIngestionService.IngestAsync` validates the requested directory against `AllowedDirectoryRoots`, enumerates files, resolves each path through reparse points, verifies each file remains inside the canonical root, filters `UnsupportedExtensions`, rejects unreadable/empty/too-large files, and collects candidate file paths before scheduling. Preserve those security checks. [Source: `src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs`; `tests/Hexalith.Memories.Server.Tests/Ingestion/DirectoryIngestionPathValidationTests.cs`]

The A33 O(n^2) state-write issue is concrete in `DirectoryIngestionService`: it creates and saves full `DirectoryBatchState` once before scheduling, again for every unreadable file during scheduling, and again after every scheduled file. Because the state contains growing `InstanceIds`, `Files`, and `Skipped` arrays, total serialized bytes grow quadratically with batch size. [Source: `src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs`; `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A33`]

Scheduling is currently sequential. For each candidate, the service reads all bytes, builds an `IngestionInput`, optionally calls `IngestionPayloadClaimCheck.PrepareAsync`, calls `DaprWorkflowClient.ScheduleNewWorkflowAsync`, appends the returned instance id/file mapping, then saves full state. This is the scheduling path to make bounded-parallel. [Source: `src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs`]

`IngestionSettings.SupportedExtensions` contains the intended supported file allowlist (`.md`, `.txt`, `.pdf`, `.docx`, `.doc`, `.html`, `.htm`, `.xlsx`, `.xls`, `.pptx`, `.ppt`, `.csv`, `.json`, `.rtf`, `.epub`) but `DirectoryIngestionService` currently checks only `UnsupportedExtensions`. A33 explicitly requires applying `SupportedExtensions` as an allowlist. [Source: `src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-23`]

`DaprIngestionWorkflowScheduler` already wraps `IIngestionWorkflowScheduler` around `IngestionPayloadClaimCheck.PrepareAsync` and `DaprWorkflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId, slimInput)`. Reuse this existing seam if possible instead of duplicating claim-check scheduling logic in `DirectoryIngestionService`. Mind accessibility: `IIngestionWorkflowScheduler` is internal and `DirectoryIngestionService` is currently public. [Source: `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs`; `src/Hexalith.Memories.Server/Ingestion/IIngestionWorkflowScheduler.cs`; `src/Hexalith.Memories.Server/Program.cs`]

Directory batch status reads the persisted `DirectoryBatchState` by `ingestion-batch:{batchId}`, then maps every `BatchFileRef` through `DirectoryBatchStatusMapper`. If final state is incomplete, batch status becomes dishonest. Any checkpointing design must always write a complete final state on success. [Source: `src/Hexalith.Memories.Server/Program.cs`; `src/Hexalith.Memories.Server/Ingestion/DirectoryBatchStatusMapper.cs`]

Existing directory tests intentionally avoid the scheduling path by using an uninitialized `DaprWorkflowClient`. Story 23.6 should add a testable scheduling seam or helper so bounded parallel scheduling, failure cleanup, and checkpoint counts can be tested without live Dapr sidecars. [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/DirectoryIngestionServiceTests.cs`]

### Architecture Constraints

- Dapr Workflow remains the ingestion orchestration boundary. Directory ingestion schedules one `IngestionWorkflow` per accepted file; do not introduce a new batch workflow unless the architecture docs are updated and tests prove parity. [Source: `_bmad-output/planning-artifacts/architecture.md#Complete-Decision-Registry`; `_bmad-output/project-context.md#Framework-Specific-Rules`]
- Keep large non-URL bytes out of workflow history. Use the existing claim-check path when a payload store is available; do not regress to inline `ContentBytes` in scheduled workflow input. [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`; `src/Hexalith.Memories.Server/Ingestion/IngestionPayloadClaimCheck.cs`]
- Tenant isolation remains non-negotiable. Batch state, workflow input, payload references, telemetry tags, and endpoint authorization must retain tenant and case identifiers. [Source: `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`]
- Use existing structured errors and endpoint result mapping. Do not replace `ErrorResponse` mapping with thrown exceptions or ad hoc response strings. [Source: `src/Hexalith.Memories.Server/Program.cs`; `_bmad-output/project-context.md#Critical-Implementation-Rules`]
- No dependency upgrade is required. Use .NET 10/C# 14, Dapr 1.18.4, xUnit v3, Shouldly, and NSubstitute already pinned in the repo. Package versions stay centralized. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`; `Directory.Packages.props`]

### Previous Story Intelligence

Story 23.9 is done. Provider-specific request/auth/response behavior is behind provider strategies and `EmbeddingClient.GenerateBatchAsync(...)`; directory scheduling must not add provider logic. [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md`]

Story 23.1 is done. Raw documents are chunked downstream by `GenerateChunkEmbeddingsActivity`; directory scheduling should still schedule one workflow per file and let the workflow handle chunking. [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md`]

Story 23.2 is done. Claim-check payload references are the safe way to pass source bytes into workflows; directory ingestion already has an optional payload store path but duplicates scheduling logic. Preserve source-byte payload scoping and cleanup ownership. [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`]

Story 23.3 is done. Provider 429 durable timers and rate-limiter math are workflow/activity concerns. Directory batch parallelism must not bypass or duplicate those retry paths. [Source: `_bmad-output/implementation-artifacts/23-3-retry-after-aware-429-orchestration.md`]

Story 23.4 is done. Failed non-URL re-ingestion relies on retained source-byte payload references and clear unsupported legacy errors. Directory scheduling must keep `SourceType.File`, source URI, payload reference, metadata, and correlation semantics intact so failed-unit retry keeps working. [Source: `_bmad-output/implementation-artifacts/23-4-non-url-re-ingestion.md`]

Story 23.5 is done. Embedding admission now uses a single actor operation per provider call/batch plus a tenant embedding config cache. Directory scheduling can increase workflow starts, but it must not change embedding admission or tenant config caching behavior. [Source: `_bmad-output/implementation-artifacts/23-5-rate-limiter-admission-simplification.md`]

### Git Intelligence

Recent commits before story creation:

- `8dced2b feat(story-23.5): Rate-Limiter Admission Simplification`
- `54f6292 feat(story-23.4): Non-URL Re-Ingestion`
- `1ef8a18 feat(references): update Hexalith.FrontComposer subproject commit`
- `acfeca8 feat(story-23.3): update subproject references and finalize story status`
- `c77c723 feat(story-23.3): Retry-After-Aware 429 Orchestration`
- `906f819 feat(story-23.2): Claim-Check Workflow Payloads`
- `6935421 feat(story-23.1): Content Chunking & Batch Embedding`
- `a645b96 feat(story-23.9): EmbeddingClient Provider Strategy`

Pattern: Epic 23 stories are tightly scoped to one audit finding, reuse existing seams, and validate with focused server tests plus xUnit v3 fallback when VSTest is blocked. Continue that pattern.

### Latest Technical / Library Notes

- No external API or package research changes this story. The repository-pinned local stack is authoritative: .NET 10/C# 14, Dapr 1.18.4, central package management, xUnit v3, Shouldly, and NSubstitute. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`]
- Bounded parallelism can be implemented with BCL primitives already available in .NET. Do not add TPL Dataflow, channels packages, or other dependencies unless there is a clear repo-approved reason. [Source: `_bmad-output/project-context.md#Code-Quality-Style-Rules`]

### Scope Boundaries

In scope:

- Applying `SupportedExtensions` as the directory file allowlist.
- Bounded-parallel file read, claim-check preparation, and workflow scheduling.
- Bounded batch-state checkpointing and final state correctness.
- Safe cleanup for payloads created before failed scheduling.
- Focused tests for state-save count, parallelism bound, allowlist filtering, status compatibility, and failure paths.
- Minimal docs/config comments if new settings are introduced.

Out of scope:

- Provider strategy, chunking algorithm, embedding admission, Retry-After workflow timers, failed-unit re-ingestion, index provisioning ownership, workflow config determinism, MCP/CLI/Web UI work, benchmark changes, submodule updates, and package upgrades.
- Changing public directory request/response JSON shape unless strictly additive and covered by contract serialization tests.
- Replacing one-workflow-per-file semantics with a new aggregate workflow.
- Making unsupported file types extractable; unsupported files should be skipped.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`.
- Tests belong under matching folders: `Ingestion`, `Authentication`/endpoint status where touched, and `Activities/Ingestion` only if a helper there changes.
- Unit tests should not require live Dapr sidecars, Redis, Google, Ollama, or filesystem paths outside temp test directories.
- For parallel tests, avoid timing-only assertions. Use a fake scheduler/helper that records concurrent in-flight calls and blocks deterministically until the test releases them.
- If normal `dotnet test` is blocked by the known VSTest TCP-listener sandbox issue, use the established xUnit v3 in-process `dotnet exec` fallback and record exact commands/counts.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story-23.6` - story statement and A33 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A33` - finding: per-file full-batch state rewrite and sequential scheduling]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-23` - approved A33 remediation scope]
- [Source: `_bmad-output/project-context.md` - .NET 10/C# 14, Dapr, testing, workflow, tenant-isolation, and central package rules]
- [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md` - provider strategy prerequisite]
- [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md` - chunked workflow prerequisite]
- [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md` - payload reference prerequisite]
- [Source: `_bmad-output/implementation-artifacts/23-3-retry-after-aware-429-orchestration.md` - provider retry prerequisite]
- [Source: `_bmad-output/implementation-artifacts/23-4-non-url-re-ingestion.md` - failed non-URL retry prerequisite]
- [Source: `_bmad-output/implementation-artifacts/23-5-rate-limiter-admission-simplification.md` - immediate previous story and admission/cache prerequisite]
- [Source: `src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs` - current directory enumeration, scheduling, state persistence]
- [Source: `src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs` - `SupportedExtensions`, `UnsupportedExtensions`, batch settings]
- [Source: `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs` - reusable claim-check scheduling seam]
- [Source: `src/Hexalith.Memories.Server/Ingestion/IIngestionWorkflowScheduler.cs` - scheduler interface]
- [Source: `src/Hexalith.Memories.Server/Ingestion/IngestionPayloadClaimCheck.cs` - source-byte payload claim-check helper]
- [Source: `src/Hexalith.Memories.Server/Ingestion/IWorkflowPayloadStore.cs` - payload cleanup API]
- [Source: `src/Hexalith.Memories.Server/Program.cs` - directory endpoint and batch status endpoint]
- [Source: `src/Hexalith.Memories.Contracts/V1/DirectoryIngestionRequest.cs` - request contract]
- [Source: `src/Hexalith.Memories.Contracts/V1/DirectoryIngestionOutcome.cs` - response contract]
- [Source: `src/Hexalith.Memories.Server/Ingestion/DirectoryBatchStatusMapper.cs` - batch status mapping]
- [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/DirectoryIngestionServiceTests.cs` - current state and skipped-file tests]
- [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/DirectoryIngestionPathValidationTests.cs` - path security tests]
- [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/DirectoryBatchStatusMapperTests.cs` - status mapper tests]
- [Source: `tests/Hexalith.Memories.IntegrationTests/Ingestion/DirectoryIngestionIntegrationTests.cs` - skipped Aspire integration scenarios]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-05: Loaded repository instructions from user-provided `AGENTS.md` content and `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`.
- 2026-07-05: Used `.agents/skills/bmad-create-story/SKILL.md`; loaded `discover-inputs.md`, `template.md`, and `checklist.md`.
- 2026-07-05: Resolved workflow customization with `_bmad/scripts/resolve_customization.py`; activation prepend/append steps were empty, persistent facts were `file:{project-root}/**/project-context.md`, and `workflow.on_complete` was empty.
- 2026-07-05: Loaded BMM config: user `Jerome`, project `memories`, planning artifacts `_bmad-output/planning-artifacts`, implementation artifacts `_bmad-output/implementation-artifacts`, English communication/output.
- 2026-07-05: Target story supplied by user as `23.6`; selected story key `23-6-directory-batch-scalability`.
- 2026-07-05: Confirmed sprint status before creation: `epic-23: in-progress`; `23-1`, `23-2`, `23-3`, `23-4`, `23-5`, and `23-9` done; `23-6` backlog.
- 2026-07-05: Loaded project context, Epic 23 source, A33 audit finding, sprint-change proposal, architecture/prd directory-ingestion mentions, previous Epic 23 story files, current directory ingestion source files, current directory tests, integration placeholder, and recent git commits.
- 2026-07-05: Discovery results: no sharded planning directories were present; loaded relevant sections from `_bmad-output/planning-artifacts/epics.md`, `architecture.md`, `prd.md`, `sprint-change-proposal-2026-07-04.md`, `research/architecture-audit-2026-07-04.md`, plus `_bmad-output/project-context.md` and prior story files.
- 2026-07-05: Validation pass applied checklist concerns: prevented bypassing claim-check payloads, prevented unbounded task fan-out, required allowlist semantics for `SupportedExtensions`, required checkpoint/final-state proof, required payload cleanup on partial scheduling failure, and bounded scope away from Stories 23.7 and 23.8.
- 2026-07-05: Used `.agents/skills/bmad-dev-story/SKILL.md`; resolved workflow customization with `_bmad/scripts/resolve_customization.py`; activation prepend/append steps were empty and persistent facts loaded `_bmad-output/project-context.md`.
- 2026-07-05: Reconfirmed current A33 behavior before editing: `DirectoryIngestionService` rewrote full state initially, for unreadable scheduled candidates, and after every scheduled file; `SupportedExtensions` existed in settings but directory ingestion enforced only `UnsupportedExtensions`.
- 2026-07-05: Made `DirectoryIngestionService` internal and injected `IIngestionWorkflowScheduler` so directory ingestion uses the existing scheduling seam without exposing a less-accessible public constructor.
- 2026-07-05: Added `DirectorySchedulingParallelism` default 4, clamped 1..32, and `DirectoryBatchCheckpointSize` default 50, clamped 1..250. With the current `MaxBatchSize = 500`, default successful batches save initial state, up to 10 progress checkpoints, and final state instead of saving after every file.
- 2026-07-05: Applied `SupportedExtensions` as the effective allowlist with case-insensitive normalization to lowercase leading-dot values; `UnsupportedExtensions` remains a denylist overlay.
- 2026-07-05: Replaced sequential scheduling with bounded `Parallel.ForEachAsync`; candidates are sorted before scheduling and final `InstanceIds`/`Files` are sorted by source URI for deterministic accounting.
- 2026-07-05: Preserved claim-check semantics by preparing file payloads with `IWorkflowPayloadStore` before scheduling and deleting newly created source payload references when scheduling fails before workflow acceptance.
- 2026-07-05: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~DirectoryIngestion|FullyQualifiedName~DirectoryBatchStatusMapper|FullyQualifiedName~IngestionEndpointLog|FullyQualifiedName~IngestionStatusEndpointAuthorization"` was blocked by the known VSTest `SocketException (13): Permission denied`.
- 2026-07-05: Focused xUnit v3 fallback passed: `DiffEngine_Disabled=true tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -noColor -class Hexalith.Memories.Server.Tests.Ingestion.DirectoryIngestionServiceTests -class Hexalith.Memories.Server.Tests.Ingestion.DirectoryIngestionPathValidationTests -class Hexalith.Memories.Server.Tests.Ingestion.DirectoryBatchStatusMapperTests -class Hexalith.Memories.Server.Tests.Ingestion.IngestionEndpointLogTests -class Hexalith.Memories.Server.Tests.Authentication.IngestionStatusEndpointAuthorizationTests` -> Total 43, Failed 0.
- 2026-07-05: Scheduler/claim-check regression xUnit v3 fallback passed: `DiffEngine_Disabled=true tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -noColor -class Hexalith.Memories.Server.Tests.Ingestion.IngestionPayloadClaimCheckTests -class Hexalith.Memories.Server.Tests.Ingestion.ReIngestionCoordinatorTests -class Hexalith.Memories.Server.Tests.Endpoints.ReIngestionEndpointE2ETests` -> Total 18, Failed 0.
- 2026-07-05: Required builds passed: `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`; `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
- 2026-07-05: Full server in-process regression passed: `DiffEngine_Disabled=true tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -noColor` -> Total 2383, Failed 0, Skipped 1.
- 2026-07-05: `git diff --check` passed.
- 2026-07-05: Senior developer review used `.agents/skills/bmad-story-automator-review/SKILL.md`, loaded `workflow.yaml`, `instructions.xml`, and `checklist.md`, then reviewed the story File List plus git-discovered changes.
- 2026-07-05: MCP resource discovery returned no available resources; verified Dapr workflow cancellation support through the pinned local NuGet XML docs at `~/.nuget/packages/dapr.workflow/1.18.4/lib/net10.0/Dapr.Workflow.xml`.
- 2026-07-05: Senior review auto-fixes applied: cancellation cleanup for unscheduled claim-check payloads, Dapr workflow scheduler cancellation propagation, re-ingestion scheduler cancellation propagation, ReIngestion endpoint test stub alignment, File List hygiene, and review/status sync.
- 2026-07-05: Senior review focused validation passed: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`; `DiffEngine_Disabled=true tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -noColor -class Hexalith.Memories.Server.Tests.Ingestion.DirectoryIngestionServiceTests -class Hexalith.Memories.Server.Tests.Endpoints.DirectoryIngestionEndpointE2ETests -class Hexalith.Memories.Server.Tests.Ingestion.ReIngestionCoordinatorTests` -> Total 24, Failed 0.
- 2026-07-05: Senior review full server regression passed after endpoint test alignment: `DiffEngine_Disabled=true tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -noColor` -> Total 2386, Failed 0, Skipped 1.

### Completion Notes List

- Implemented A33 directory-batch scalability with bounded state checkpointing, bounded-parallel scheduling, effective `SupportedExtensions` allowlist filtering, deterministic final batch accounting, and partial-failure source-payload cleanup.
- Operational defaults: directory scheduling parallelism defaults to 4 and clamps to 1..32; directory batch checkpoint cadence defaults to every 50 scheduled files and clamps to 1..250. `AllowedDirectoryRoots` remains empty by default.
- `DirectoryIngestionService` now uses `IIngestionWorkflowScheduler`; the service is internal to keep accessibility valid while preserving endpoint DI and testability.
- Added focused tests for allowlist/denylist behavior, unsupported-file no claim-check/no schedule, bounded checkpoint saves, deterministic final state, bounded scheduling concurrency, and unscheduled payload cleanup.

### File List

- `_bmad-output/implementation-artifacts/23-6-directory-batch-scalability.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Server/EventStoreIntegration/EventIngestionWorkflowSchedulerAdapter.cs`
- `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs`
- `src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs`
- `src/Hexalith.Memories.Server/Ingestion/IIngestionWorkflowScheduler.cs`
- `src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs`
- `src/Hexalith.Memories.Server/Ingestion/ReIngestionCoordinator.cs`
- `src/Hexalith.Memories.Server/appsettings.json`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/DirectoryIngestionEndpointE2ETests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ReIngestionEndpointE2ETests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/DirectoryIngestionServiceTests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-20-20260704-091304.md`

### Senior Developer Review (AI)

- [CRITICAL] Cancellation after source-byte claim-check creation could leave an unscheduled payload behind. `DirectoryIngestionService.ProcessCandidateAsync` only cleaned payload references for `DaprException` and non-cancellation exceptions; a cancellation from another failing worker or request cancellation after `PrepareAsync` could skip deletion. Fixed by moving claim-check preparation inside the scheduling try/catch, deleting created references on `OperationCanceledException`, and adding `IngestAsync_WhenSchedulingIsCanceledAfterClaimCheck_ShouldDeleteUnscheduledPayloadAndNotSucceed`.
- [HIGH] The production Dapr scheduler accepted a `CancellationToken` through `IIngestionWorkflowScheduler` but did not pass it to `DaprWorkflowClient.ScheduleNewWorkflowAsync`. Fixed by using the Dapr 1.18.4 overload with `startTime: null` and the caller token.
- [MEDIUM] `ReIngestionCoordinator` was still calling the shared scheduler seam without forwarding its cancellation token. Fixed so re-ingestion scheduling uses the same cancellation path.
- [MEDIUM] The re-ingestion endpoint E2E scheduler substitute matched only the optional default token, so it failed once the coordinator forwarded the real request token. Fixed the setup and verification to include `Arg.Any<CancellationToken>()`.
- [MEDIUM] The story File List omitted git-discovered changes: endpoint E2E coverage, `EventStoreWebAppFactory`, `tests/test-summary.md`, `ReIngestionCoordinator`, and the story-automator orchestration artifact. Fixed by updating the File List.

Checklist summary:

- Story status was reviewable and is now `done`.
- Acceptance Criteria 1-10 rechecked against source and tests.
- No critical issues remain after automatic fixes.
- Sprint status synced to `done`.

### Change Log

- 2026-07-05: Completed Story 23.6 implementation for A33 directory batch scalability; added bounded scheduling/checkpointing settings, allowlist enforcement, scheduler seam use, failure cleanup, and focused validation coverage.
- 2026-07-05: Senior developer review completed with automatic fixes for cancellation cleanup, scheduler token propagation, and File List hygiene. Status moved to done.
