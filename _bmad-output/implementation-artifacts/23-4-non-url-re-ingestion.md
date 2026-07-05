---
baseline_commit: 1ef8a18
---

# Story 23.4: Non-URL Re-Ingestion

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want re-ingestion of failed non-URL units to work or fail clearly,
so that FR12 retries do not silently loop back to failed.

Story 23.4 follows Story 23.9, Story 23.1, Story 23.2, and Story 23.3 in `story_execution_order.epic-23`. The provider strategy, chunked batch embedding, claim-check payload store, and durable provider-429 retry path are already done. This story must close the remaining A14 gap without weakening those contracts.

## Acceptance Criteria

1. Failed non-URL records persist a reusable source payload reference. Given new file, directory, annotation, command/projection, or event ingests are claim-checked before workflow scheduling, when a workflow reaches terminal failure, then `PersistFailedUnitActivity` stores enough optional source-byte `WorkflowPayloadReference` metadata for `ReIngestionCoordinator` to rebuild a valid non-URL `IngestionInput` without raw `ContentBytes`.

2. Re-ingestion of new failed file units schedules a valid workflow. Given a failed `SourceType.File` record contains a valid tenant-scoped source-byte payload reference, when an operator calls single re-ingestion, then the coordinator schedules a workflow using the same memory-unit id as instance id, same tenant/case/source/content metadata, `ContentBytes = null`, and `PayloadReference` populated.

3. Re-ingestion of new failed event units schedules a valid workflow. Given a failed `SourceType.Event` record contains a valid source-byte payload reference, when an operator re-ingests it, then the workflow is scheduled with the original event source URI, metadata required by existing event/NL paths, and a source-byte payload reference. Do not reintroduce raw event JSON into workflow history.

4. Legacy or expired non-URL failures are rejected clearly. Given an old failed non-URL record has no source payload reference, or the stored reference is invalid/expired/mismatched, when re-ingestion is requested, then the API returns a structured, actionable error and does not claim the failed-unit record or delete its dedup key. The error must distinguish "cannot re-ingest this non-URL record because source content is unavailable" from not-found, case-mismatch, and conflict.

5. URL re-ingestion behavior remains unchanged. Given `SourceType.Url` failed records never need stored bytes because the server fetches the URL again, when URL re-ingestion is requested, then the current scheduling path still works and no source payload reference is required.

6. Claim-and-schedule remains atomic and recoverable. Given the current failed-unit claim removes the failed-unit hash, case sorted-set entry, and dedup key atomically, when scheduling fails after a claim, then `RestoreAsync` restores every persisted failed-unit field, including the optional payload reference metadata. When a non-URL record is unsupported before scheduling, no claim should be taken.

7. Public failed-unit summaries stay support-safe. Given `FailedUnitSummary` is public API, when source payload metadata is added for internal re-ingestion, then summaries, status endpoints, logs, telemetry, and error responses expose only safe metadata such as source type, stage, code, retry counts, and an actionable suggestion. They must not expose raw payload bytes, raw event JSON, vectors, provider secrets, bearer tokens, or internal stack traces.

8. Bulk re-ingestion reports mixed unsupported outcomes. Given a bulk request includes URL, supported non-URL, legacy non-URL, missing, and conflicted failed units, when it runs, then supported units schedule, unsupported units return a per-unit structured non-scheduled outcome, and the aggregate response counts do not hide unsupported units as generic scheduler errors.

9. Payload retention policy is explicit. Given Story 23.2 created transient workflow payload TTL, when a failed non-URL record stores a source reference for later re-ingestion, then retention is either long enough and documented for failed-unit recovery or the story introduces a failed-source payload retention path distinct from transient cleanup. Re-ingestion must not depend on payloads that the workflow already deletes on terminal failure.

10. Tests prove the regression is closed. Given A14 is specifically about `ReIngestionCoordinator` rebuilding invalid non-URL input, when this story completes, then focused unit tests prove file/event re-ingestion schedules with `PayloadReference`, legacy/expired records reject without claim, URL records still schedule without a reference, restore preserves new fields, bulk responses include unsupported units, and validator coverage prevents scheduling doomed non-URL inputs.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm A14 and current post-23.2 behavior before editing (AC: 1-10)
  - [x] Read `src/Hexalith.Memories.Server/Ingestion/ReIngestionCoordinator.cs` completely. Confirm `BuildIngestionInput` currently sets `ContentBytes = null`, no `PayloadReference`, and no metadata.
  - [x] Read `src/Hexalith.Memories.Server/Activities/Ingestion/IngestionInputValidator.cs` completely. Confirm non-URL ingestion requires either non-empty `ContentBytes` or a valid `WorkflowPayloadReference` of kind `SourceBytes`.
  - [x] Read `PersistFailedUnitActivity`, `FailedUnitsRegistry`, `FailedUnitRecord`, `FailedUnitInput`, `FailedUnitSummary`, and `WorkflowPayloadReference`.
  - [x] Read `IngestionWorkflow.TryPersistFailedUnit` and transient payload cleanup logic so source payload retention is not accidentally deleted before a failed-unit retry can use it.
  - [x] Preserve URL re-ingestion, case mismatch, not-found, conflict, claim restore, and bulk mixed-outcome behavior.

- [x] Task 2 - Add internal failed-unit source-payload metadata (AC: 1, 4, 6-7, 9)
  - [x] Add optional source payload reference data to `FailedUnitInput` and internal `FailedUnitRecord`. Prefer reusing `WorkflowPayloadReference` as a nullable property rather than duplicating its fields in multiple contracts.
  - [x] Persist the optional reference in `PersistFailedUnitActivity` as JSON or explicit Redis hash fields. Keep field names centralized beside the existing failed-unit field constants.
  - [x] Update `FailedUnitsRegistry.ParseRecord` to read old hashes without the field and new hashes with the field. Old records must remain readable.
  - [x] Update `FailedUnitsRegistry.RestoreAsync` to write the optional payload reference back when a scheduling claim must be restored.
  - [x] Do not add the reference to `FailedUnitSummary` unless there is a specific safe operator need. The story only needs the internal record to rebuild re-ingestion input.

- [x] Task 3 - Preserve source bytes for failed non-URL records (AC: 1, 3, 7, 9)
  - [x] Change workflow failure handling so the original source-byte payload reference can be retained when terminal failure creates a failed non-URL record.
  - [x] Avoid deleting the retained source-byte payload through `CleanupWorkflowPayloadsActivity` on terminal failure if the failed record needs it for FR12 recovery. Continue cleaning extracted text, chunk text, vectors, fetched URL bytes, and other transient payloads according to Story 23.2.
  - [x] If retention cannot safely reuse the transient payload store TTL, add a documented failed-source retention path or options field. Do not rely on the default 24 hour transient TTL without naming that operator limitation in docs/tests.
  - [x] Keep source payload keys tenant-scoped and memory-unit-scoped. Do not scan Redis/Dapr state to find payloads during re-ingestion.
  - [x] Ensure raw bytes and raw event JSON are never emitted in workflow status, failed-unit summaries, logs, telemetry, or API errors.

- [x] Task 4 - Rebuild valid non-URL ingestion inputs in the coordinator (AC: 2-6)
  - [x] Update `ReIngestionCoordinator` so `SourceType.Url` keeps the existing refetch path.
  - [x] For non-URL records with a valid source-byte payload reference, build `IngestionInput` with `ContentBytes = null`, `PayloadReference = record.SourcePayloadReference`, original `ContentType`, original `SourceUri`, original `SourceType`, original `IngestedBy`, and preserved metadata if available.
  - [x] If failed-unit metadata currently omits ingestion metadata needed by event/NL indexing, either persist and restore it or explicitly prove the existing workflow paths do not require it for re-ingestion. Event ingestion should not lose CloudEvent metadata silently.
  - [x] For non-URL records without a usable reference, return a new non-scheduled outcome before calling `RemoveAsync`. Do not delete the failed-unit hash, sorted-set entry, or dedup key for unsupported legacy records.
  - [x] Keep scheduling through `IIngestionWorkflowScheduler` so new inputs still pass through `IngestionPayloadClaimCheck` and Dapr workflow scheduling consistently.

- [x] Task 5 - Add structured API and bulk outcome mapping (AC: 4, 7-8)
  - [x] Add a coordinator outcome such as `UnsupportedSourcePayload` or `SourcePayloadUnavailable` with an error code like `NON_URL_REINGESTION_UNAVAILABLE`.
  - [x] Map single re-ingestion unsupported outcome to a non-2xx structured `ErrorResponse` with a clear suggestion, for example: re-ingest from the original file/event source if available, or ingest the content again.
  - [x] Extend `BulkReIngestionResponse` handling so unsupported units are counted and reported distinctly from scheduler exceptions. Avoid putting unsupported non-URL units into the generic `Errored` bucket unless the contract intentionally says so and tests pin it.
  - [x] Preserve `MEMORY_UNIT_NOT_FOUND`, `CASE_MISMATCH`, and `RE_INGESTION_IN_PROGRESS` behavior.

- [x] Task 6 - Update docs and operator-facing comments (AC: 4, 7, 9)
  - [x] Update relevant retry/failure operations docs or skipped integration-test notes so they no longer imply non-URL re-ingestion always schedules successfully.
  - [x] Document the failed-source payload retention window and what happens after expiration.
  - [x] Update stale comments in `FailedUnitInput` that currently say failed-unit records carry everything needed to rebuild `IngestionInput` except `ContentBytes`.

- [x] Task 7 - Focused tests and validation evidence (AC: 1-10)
  - [x] Add `ReIngestionCoordinatorTests` for supported file records, supported event records, URL records, missing source payload reference, invalid payload kind, tenant/memory-unit mismatch, case mismatch, conflict, and scheduling-failure restore.
  - [x] Add `FailedUnitsRegistryTests` for parsing old records without a payload reference, parsing new records with one, and restoring the optional field.
  - [x] Add `PersistFailedUnitActivityTests` proving the optional payload reference is written and remains support-safe.
  - [x] Add workflow tests proving terminal failure persists the source payload reference and cleanup does not delete the retained source payload while still deleting other transient payloads.
  - [x] Add API endpoint or minimal handler tests for single unsupported non-URL error mapping if an existing endpoint-test harness can cover it without standing up Aspire.
  - [x] Add bulk response tests for mixed scheduled/unsupported/missing/conflict results.
  - [x] Update contract serialization tests if any public V1 contract changes. Prefer internal-only record changes where possible.
  - [x] Run `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run focused xUnit v3 tests for ingestion workflow, failed-unit registry, re-ingestion coordinator, failed-unit persistence, payload store, and validator. If VSTest is blocked by the known sandbox TCP-listener issue, use the established `DiffEngine_Disabled=true dotnet exec ...Hexalith.Memories.Server.Tests.dll` fallback and record exact counts.
  - [x] Run `git diff --check`.

## Dev Notes

### Current State and Code Anchors

`ReIngestionCoordinator.TryScheduleAsync` reads a failed-unit record, checks case ownership, atomically removes the failed-unit hash/sorted-set/dedup key through `FailedUnitsRegistry.RemoveAsync`, and then schedules a workflow using `BuildIngestionInput(record)`. The builder always sets `ContentBytes = null`, `Metadata = []`, and does not set `PayloadReference`. That remains valid for URL sources only. [Source: `src/Hexalith.Memories.Server/Ingestion/ReIngestionCoordinator.cs`; `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A14`]

`IngestionInputValidator` now correctly rejects non-URL workflow inputs unless they carry either non-empty `ContentBytes` or a tenant-matching `PayloadReference` whose `ContentKind` is `WorkflowPayloadKind.SourceBytes`. It also rejects URL inline bytes and requires URL source URIs to be absolute `http(s)`. Do not weaken this validator to make re-ingestion pass. [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/IngestionInputValidator.cs`]

Story 23.2 introduced `WorkflowPayloadReference`, `WorkflowPayloadKind.SourceBytes`, `IWorkflowPayloadStore`, `DaprWorkflowPayloadStore`, and `IngestionPayloadClaimCheck.PrepareAsync`. New non-URL schedules already move source bytes to the payload store before Dapr Workflow start. Reuse that contract for re-ingestion; do not reintroduce inline bytes into workflow history. [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`; `src/Hexalith.Memories.Server/Ingestion/IngestionPayloadClaimCheck.cs`; `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs`]

`PersistFailedUnitActivity` currently persists only tenant, case, source URI/type, ingested-by, content type, stage, error details, retry count, and failed-at fields. `FailedUnitRecord` mirrors those fields and has no payload reference or metadata. `FailedUnitsRegistry.ParseRecord` must remain backward compatible with old hashes. [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/PersistFailedUnitActivity.cs`; `src/Hexalith.Memories.Server/Ingestion/FailedUnitRecord.cs`; `src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs`]

`FailedUnitsRegistry.RemoveAsync` deletes the failed-unit hash, sorted-set entry, and `DedupKeyBuilder.BuildKey(tenantId, caseId, sourceUri)` in one Lua script. Unsupported non-URL records must be detected before calling `RemoveAsync`; otherwise the failed record and dedup guard can be lost even though no valid workflow can be scheduled. [Source: `src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs`]

`IngestionWorkflow.TryPersistFailedUnit` constructs `FailedUnitInput` from the original workflow `IngestionInput`. It has access to `input.PayloadReference` and the memory-unit id at terminal failure time. This is the narrowest source for storing the original source-byte pointer. [Source: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`]

`CleanupWorkflowPayloadsActivity` deletes every distinct reference passed to it when tenant and memory-unit scope match. Since `IngestionWorkflow` adds `input.PayloadReference` to `transientPayloads` at workflow start, terminal failure cleanup can currently delete the only source-byte payload that Story 23.4 needs. Fix the retention decision before claiming non-URL re-ingestion works. [Source: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`; `src/Hexalith.Memories.Server/Activities/Ingestion/CleanupWorkflowPayloadsActivity.cs`]

The single re-ingestion endpoint maps only `NotFound`, `CaseMismatch`, `Conflict`, and `Scheduled`; bulk re-ingestion counts scheduled/not-found/conflicted/errored. Story 23.4 needs an explicit unsupported-source outcome or a deliberately tested structured error mapping so legacy non-URL records do not look like scheduler failures. [Source: `src/Hexalith.Memories.Server/Program.cs`]

### Architecture Constraints

- Dapr Workflow remains the durable orchestration boundary. Workflows orchestrate, activities and scheduler services perform I/O. Do not add direct Dapr state, Redis, filesystem, network, or payload-store reads inside `IngestionWorkflow`. [Source: `_bmad-output/planning-artifacts/architecture.md#DAPR-Workflow-Patterns`; `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`]
- Keep workflow orchestration replay-safe. Use deterministic values from workflow input and `context.CurrentUtcDateTime`; do not read wall-clock time, random values, mutable process config, or external services in orchestration code. [Source: `_bmad-output/project-context.md#Framework-Specific-Rules`]
- Tenant isolation remains explicit. Payload references must carry tenant id and memory-unit id, and reads must validate both before returning bytes. [Source: `src/Hexalith.Memories.Contracts/V1/WorkflowPayloadReference.cs`; `src/Hexalith.Memories.Server/Ingestion/DaprWorkflowPayloadStore.cs`; `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`]
- Keep contract changes additive and JSON-safe. Existing workflow histories and failed-unit Redis hashes must deserialize/read without the new field. [Source: `_bmad-output/project-context.md#Critical-Implementation-Rules`]
- Use existing structured error models (`ErrorResponse`, per-unit `ReIngestedUnitInfo`) rather than ad hoc strings or unhandled exceptions for operator-facing failures. [Source: `_bmad-output/project-context.md#Critical-Implementation-Rules`; `_bmad-output/planning-artifacts/architecture.md#Complete-Decision-Registry`]
- Keep package versions centralized. No dependency upgrade is required for this story. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`; `Directory.Packages.props`]

### Previous Story Intelligence

Story 23.9 is done. Provider-specific request/auth/response behavior is behind provider strategies and `EmbeddingClient.GenerateBatchAsync(...)`. This story should not touch provider strategy code. [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md`]

Story 23.1 is done. Raw documents are chunked and embedded in batches; semantic chunk keys and dedupe behavior must not change. Re-ingestion should schedule the same pipeline, not create a special non-chunked path. [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md`]

Story 23.2 is done. Claim-check payloads keep large source bytes, extracted text, chunk text, and vectors out of workflow history. The story explicitly left non-URL re-ingestion behavior unchanged and preserved compatibility for failed-unit/re-ingestion records. Story 23.4 is the follow-up that consumes the source-byte reference. [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`]

Story 23.2 senior review fixed duplicate-path payload cleanup. Do not regress cleanup hygiene while retaining source bytes for failed-unit retry; the retained reference should be intentional and bounded, not an accidental leak. [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md#Senior-Developer-Review-AI`]

Story 23.3 is done. Provider 429s now use workflow-owned durable Retry-After waits and rate-limiter math was corrected. Story 23.4 should not reopen provider retry behavior, generic activity retry policy, or rate-limiter actor API. [Source: `_bmad-output/implementation-artifacts/23-3-retry-after-aware-429-orchestration.md`]

### Git Intelligence

Recent commits before story creation:

- `1ef8a18 feat(references): update Hexalith.FrontComposer subproject commit`
- `acfeca8 feat(story-23.3): update subproject references and finalize story status`
- `c77c723 feat(story-23.3): Retry-After-Aware 429 Orchestration`
- `906f819 feat(story-23.2): Claim-Check Workflow Payloads`
- `6935421 feat(story-23.1): Content Chunking & Batch Embedding`

Pattern: Epic 23 stories are source-anchored, heavily unit-tested, and explicit about Dapr workflow constraints. Continue that pattern and avoid broad refactors while changing failed-unit persistence and re-ingestion scheduling.

### Latest Technical / Library Notes

- No new external library or API upgrade is needed. The implementation should use the existing .NET 10 / C# 14, Dapr 1.18.4, xUnit v3, Shouldly, NSubstitute, Dapr state, Redis, and workflow-payload-store patterns already pinned in the repository. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`]
- Dapr state-store TTL is already used by `DaprWorkflowPayloadStore` through `ttlInSeconds`. If failed-source payloads need longer retention than transient workflow payloads, add an explicit option and tests instead of silently depending on the transient 24-hour default. [Source: `src/Hexalith.Memories.Server/Ingestion/DaprWorkflowPayloadStore.cs`; `src/Hexalith.Memories.Server/Ingestion/WorkflowPayloadStoreOptions.cs`]
- No web research changed the story guidance; local pinned versions and existing repository APIs are authoritative for this implementation.

### Scope Boundaries

In scope:

- Persisting optional source-byte payload reference metadata with failed-unit records.
- Retaining or separately storing source-byte payloads long enough for failed non-URL re-ingestion.
- Rebuilding valid `IngestionInput` for failed file/event/non-URL records using `PayloadReference`.
- Clear structured rejection for legacy, expired, or invalid non-URL failed records.
- Single and bulk re-ingestion outcome mapping and tests.
- Focused workflow, registry, coordinator, persistence, validator, and API/response tests.

Out of scope:

- URL fetch, extraction, chunking, embedding provider strategy, provider 429 durable timers, rate-limiter API redesign, directory batch checkpointing, index-provisioning memoization, and workflow config determinism.
- Adding new ingestion source types or changing `SourceType` enum semantics.
- Reworking CLI/MCP/Web surfaces beyond any required structured error propagation tests.
- Deleting/reindexing already failed legacy non-URL records automatically.
- Submodule updates or package upgrades.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`.
- Tests belong under matching folders: `Ingestion`, `Activities/Ingestion`, `Workflows`, and endpoint/contract folders only if touched.
- Unit tests should not require live Dapr sidecars, Redis, Google, Ollama, or external network calls.
- Integration tests may remain skipped only with an explicit `RunnableSkippedFact` reason and a clear end-state assertion plan.
- If normal `dotnet test` is blocked by the known VSTest TCP-listener sandbox issue, use the established xUnit v3 in-process `dotnet exec` fallback and record exact counts.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story-23.4` - story statement and A14 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A14` - finding: non-URL re-ingestion rebuilds input with null bytes]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-23` - approved A14 remediation scope]
- [Source: `_bmad-output/planning-artifacts/prd.md#Knowledge-Ingestion` - FR12 manual re-ingestion]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story-6.3` - failed units remain visible until re-ingested or deleted]
- [Source: `_bmad-output/planning-artifacts/architecture.md#DAPR-Workflow-Patterns` - workflows orchestrate, activities perform I/O]
- [Source: `_bmad-output/project-context.md` - .NET 10/C# 14, Dapr, testing, workflow, tenant-isolation, and contract rules]
- [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md` - source-byte claim-check prerequisite]
- [Source: `_bmad-output/implementation-artifacts/23-3-retry-after-aware-429-orchestration.md` - immediate previous story and validation pattern]
- [Source: `src/Hexalith.Memories.Server/Ingestion/ReIngestionCoordinator.cs` - current invalid non-URL rebuild path]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/IngestionInputValidator.cs` - current non-URL bytes/reference validation]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/PersistFailedUnitActivity.cs` - failed-unit hash writer]
- [Source: `src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs` - failed-unit parser, claim, restore]
- [Source: `src/Hexalith.Memories.Server/Ingestion/FailedUnitRecord.cs` - internal failed-unit record]
- [Source: `src/Hexalith.Memories.Contracts/V1/FailedUnitInput.cs` - workflow failed-unit activity input]
- [Source: `src/Hexalith.Memories.Contracts/V1/FailedUnitSummary.cs` - public failed-unit projection]
- [Source: `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs` - payload reference field]
- [Source: `src/Hexalith.Memories.Contracts/V1/WorkflowPayloadReference.cs` - source payload pointer contract]
- [Source: `src/Hexalith.Memories.Server/Ingestion/IngestionPayloadClaimCheck.cs` - scheduling claim-check helper]
- [Source: `src/Hexalith.Memories.Server/Ingestion/DaprWorkflowPayloadStore.cs` - payload persistence and TTL]
- [Source: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` - failed-unit persistence and payload cleanup]
- [Source: `src/Hexalith.Memories.Server/Program.cs` - single and bulk re-ingestion API mapping]
- [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/ReIngestionCoordinatorTests.cs` - current coordinator coverage]
- [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/FailedUnitsRegistryTests.cs` - registry coverage]
- [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/PersistFailedUnitActivityTests.cs` - failed-unit persistence coverage]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-05: Loaded repository AGENTS instructions and `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`.
- 2026-07-05: Used `.agents/skills/bmad-create-story/SKILL.md`; loaded `discover-inputs.md`, `template.md`, and `checklist.md`.
- 2026-07-05: Resolved workflow customization with `_bmad/scripts/resolve_customization.py`; activation prepend/append steps were empty, persistent facts were `file:{project-root}/**/project-context.md`, and `workflow.on_complete` was empty.
- 2026-07-05: Loaded BMM config: user `Jerome`, project `memories`, planning artifacts `_bmad-output/planning-artifacts`, implementation artifacts `_bmad-output/implementation-artifacts`, English communication/output.
- 2026-07-05: Target story supplied by user as `23.4`; selected story key `23-4-non-url-re-ingestion`.
- 2026-07-05: Confirmed sprint status before creation: `epic-23: in-progress`, `23-1` done, `23-2` done, `23-3` done, `23-4` backlog, `23-9` done.
- 2026-07-05: Loaded project context, EventStore/Tenants/FrontComposer project-context facts, Epic 23 source, A14 audit finding, sprint-change proposal, PRD FR12/NFR context, architecture workflow rules, prior Epic 23 story files, relevant source files, relevant tests, and recent git commits.
- 2026-07-05: Validation pass applied checklist concerns: prevented weakening validator rules, avoided inline payload reinvention, preserved URL behavior, required legacy non-URL rejection before claim, required source-payload retention proof, and bounded scope away from Stories 23.5-23.8.
- 2026-07-05: Executed `.agents/skills/bmad-dev-story/SKILL.md`; loaded `.agents/skills/bmad-dev-story/checklist.md`.
- 2026-07-05: Reconfirmed A14 anchors before editing: `ReIngestionCoordinator` rebuilt non-URL inputs with `ContentBytes = null`, no `PayloadReference`, and empty metadata; `IngestionInputValidator` requires non-URL bytes or a `SourceBytes` payload reference; failed-unit persistence/registry/workflow cleanup lacked source-payload retention.
- 2026-07-05: Implemented internal failed-unit source payload reference and metadata persistence, backward-compatible failed-unit parsing, restore preservation, non-URL payload availability validation before claim, structured unsupported-source outcomes, and workflow terminal-failure source-byte retention.
- 2026-07-05: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~ReIngestionCoordinatorTests|FullyQualifiedName~FailedUnitsRegistryTests|FullyQualifiedName~PersistFailedUnitActivityTests|FullyQualifiedName~IngestionWorkflowTests|FullyQualifiedName~IngestionInputValidatorTests|FullyQualifiedName~WorkflowPayloadStoreTests"` was blocked by the known VSTest sandbox TCP listener issue: `System.Net.Sockets.SocketException (13): Permission denied`.
- 2026-07-05: `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` passed.
- 2026-07-05: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed.
- 2026-07-05: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Ingestion.ReIngestionCoordinatorTests -class Hexalith.Memories.Server.Tests.Ingestion.FailedUnitsRegistryTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.PersistFailedUnitActivityTests -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.IngestionInputValidatorTests -class Hexalith.Memories.Server.Tests.Ingestion.WorkflowPayloadStoreTests` passed: total 97, failed 0, skipped 0.
- 2026-07-05: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` passed: total 2361, failed 0, skipped 1 (`SubmoduleGuardTests.CheckSubmodulesTarget_FailsBuildWhenSubmoduleGitMarkerIsMissing`, existing explicit skipped guard).
- 2026-07-05: `git diff --check` passed.
- 2026-07-05: Senior review found that event source payloads can be scoped to the deterministic EventStore dedup workflow id while the workflow generates a separate memory-unit id; the original implementation rejected that reference on terminal failure and would not rebuild event re-ingestion inputs from it.
- 2026-07-05: Senior review auto-fix allowed event source payload references scoped to the original dedup key, kept extracted/chunk payloads scoped to the final memory-unit id, made NL source-payload reads use the reference scope, grouped cleanup by each payload reference scope, and added regression coverage.
- 2026-07-05: `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` passed.
- 2026-07-05: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed.
- 2026-07-05: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Ingestion.ReIngestionCoordinatorTests -class Hexalith.Memories.Server.Tests.Ingestion.FailedUnitsRegistryTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.PersistFailedUnitActivityTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.ExtractContentActivityTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.QueueNaturalLanguageEmbeddingRetryActivityTests -class Hexalith.Memories.Server.Tests.NaturalLanguage.GenerateNaturalLanguageDescriptionActivityTests -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.IngestionInputValidatorTests -class Hexalith.Memories.Server.Tests.Ingestion.WorkflowPayloadStoreTests -class Hexalith.Memories.Server.Tests.Endpoints.ReIngestionEndpointE2ETests` passed: total 128, failed 0, skipped 0.
- 2026-07-05: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` passed after review fixes: total 2367, failed 0, skipped 1 (`SubmoduleGuardTests.CheckSubmodulesTarget_FailsBuildWhenSubmoduleGitMarkerIsMissing`, existing explicit skipped guard).

### Completion Notes List

- Persisted optional internal `WorkflowPayloadReference` source-byte metadata, ingestion metadata, and correlation fields with failed-unit records without adding payload references to public failed-unit summaries.
- Retained the original non-URL `SourceBytes` payload reference on terminal workflow failure while still cleaning extracted text, chunk text, vectors, fetched URL bytes, and other transient payloads.
- Updated re-ingestion to validate non-URL source payload availability before taking the failed-unit claim, preserving failed-unit hashes, case sorted-set rows, and dedup keys for legacy, expired, invalid, or mismatched payload references.
- Rebuilt valid file/event re-ingestion inputs with `ContentBytes = null`, `PayloadReference`, original source/content fields, and preserved metadata for event/NL paths; URL re-ingestion continues to schedule without a payload reference.
- Added structured `NON_URL_REINGESTION_UNAVAILABLE` single and bulk outcomes, plus an `Unsupported` aggregate count, without folding unsupported units into generic scheduler errors.
- Documented the failed-source payload retention policy: retained source bytes use `Ingestion:WorkflowPayloadStore:TtlHours`, default 24 hours, after which non-URL re-ingestion fails clearly without claim.
- Senior review fixed the remaining event-specific gap: EventStore claim-check source payloads scoped to the deterministic dedup workflow id are now retained and validated for the generated memory-unit failure record, while derived payloads and cleanup stay scoped to their own references.

### File List

- `_bmad-output/implementation-artifacts/23-4-non-url-re-ingestion.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/operations/failure-recovery.md`
- `src/Hexalith.Memories.Contracts/V1/BulkReIngestionResponse.cs`
- `src/Hexalith.Memories.Contracts/V1/FailedUnitInput.cs`
- `src/Hexalith.Memories.Contracts/V1/ReIngestedUnitInfo.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/ExtractContentActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/QueueNaturalLanguageEmbeddingRetryActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/PersistFailedUnitActivity.cs`
- `src/Hexalith.Memories.Server/Ingestion/FailedUnitRecord.cs`
- `src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs`
- `src/Hexalith.Memories.Server/Ingestion/ReIngestionCoordinator.cs`
- `src/Hexalith.Memories.Server/Ingestion/WorkflowPayloadStoreOptions.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`
- `src/Hexalith.Memories.Server/appsettings.json`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/PersistFailedUnitActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/ExtractContentActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/QueueNaturalLanguageEmbeddingRetryActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ReIngestionEndpointE2ETests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/FailedUnitsRegistryTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/ReIngestionCoordinatorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/GenerateNaturalLanguageDescriptionActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs`

### Change Log

- 2026-07-05: Implemented Story 23.4 non-URL re-ingestion support and clear unsupported-source failure handling; story moved to review.
- 2026-07-05: Senior developer review auto-fixed event dedup-scoped source payload retention/re-ingestion and moved story to done.

## Senior Developer Review (AI)

### Review Findings

- HIGH: Failed event ingests scheduled from EventStore claim-check source bytes stored the original payload under the deterministic `dedup:{tenantId}:{caseId}:{hash}` workflow id, but `IngestionWorkflow.GetRetainedSourcePayloadReference` only accepted references scoped to the generated memory-unit id. Terminal failed event records therefore lost `SourcePayloadReference`, violating AC1 and AC3.
- HIGH: Event re-ingestion with a dedup-scoped source payload could not validate the payload because `ReIngestionCoordinator` always read source payloads under the failed memory-unit id. Valid event source references were treated as unavailable, violating AC3 and AC4.
- MEDIUM: Event/NL source-payload reads used the generated memory-unit id instead of the reference scope, and extracted payloads inherited the source reference scope. This made event claim-check payload flows brittle and could leave cleanup unable to delete derived payloads consistently.
- MEDIUM: Story File List omitted the endpoint-level re-ingestion test and the additional activity/NL tests added during review.

### Auto-Fixes Applied

- Allowed event source payload retention and re-ingestion validation when the reference is scoped to the original EventStore dedup key.
- Saved extracted text under the actual workflow memory-unit id, while reading source bytes and NL raw payloads using the payload reference scope.
- Grouped workflow payload cleanup by each reference's `MemoryUnitId` so mixed dedup-scoped source references and memory-unit-scoped derived references are handled deterministically.
- Added regression tests for event dedup-scoped source retention, coordinator scheduling, extraction scope, NL raw-payload reads, retry queue payload reads, and endpoint unsupported/bulk outcomes.

### Review Outcome

Approved after automatic fixes. No critical issues remain.
