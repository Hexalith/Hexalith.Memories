---
baseline_commit: 906f819
---

# Story 23.3: Retry-After-Aware 429 Orchestration

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want provider 429s handled by a durable Retry-After timer,
so that a transient rate limit does not become a permanent failed unit (NFR22).

Story 23.3 follows Story 23.9, Story 23.1, and Story 23.2 in `story_execution_order.epic-23`. The provider strategy/batch API, raw-content chunking, and claim-check payload slimming are already done. This story must preserve those contracts while changing how provider 429s are retried by the workflow.

## Acceptance Criteria

1. Provider 429s are classified in workflow code without relying on activity retry exhaustion. Given `GenerateChunkEmbeddingsActivity` and `GenerateEmbeddingActivity` can throw `EmbeddingRateLimitException`, when the workflow observes an activity failure caused by that exception, then `IngestionWorkflow` detects it through Dapr `WorkflowTaskFailedException.FailureDetails.IsCausedBy<EmbeddingRateLimitException>()` or an equivalent tested helper, extracts a sanitized retry-after value, and does not treat the first provider 429 as terminal failure.

2. Raw payload embedding uses a durable Retry-After wait. Given `GenerateChunkEmbeddingsActivity` is now the raw payload embedding activity after Story 23.1, when it fails because the provider returned 429, then `IngestionWorkflow` waits with `WorkflowContext.CreateTimer(...)` for the Retry-After duration before calling the activity again. The wait must be a Dapr durable timer, not `Task.Delay`, a thread sleep, a hosted-service queue, or actor timer orchestration outside the workflow.

3. Natural-language event embedding gets the same durable 429 handling. Given event ingestion may call `GenerateEmbeddingActivity` for the natural-language description embedding, when that activity fails because the provider returned 429, then the workflow uses the same durable timer retry behavior before re-calling the activity. Natural-language description unavailability must still queue retry exactly as before; only provider 429 embedding failures are handled by this story.

4. Retry-After duration is explicit, bounded, and deterministic. Given provider transport already maps absent, malformed, past, zero, and positive Retry-After values into `EmbeddingRateLimitException.RetryAfterSeconds`, when workflow 429 handling chooses a timer delay, then absent/non-positive values use the existing 30 second default, positive values stay clamped to `[1, 3600]`, and any workflow-loop maximum is deterministic and covered by tests.

5. Activity retry no longer burns through the short generic retry budget for provider 429s. Given the current default retry policy is 5 attempts with first interval 2s and coefficient 1.5, when a provider Retry-After is 90 seconds or longer, then the memory unit remains in the workflow's embedding stage during the durable timer wait and eventually indexes after the provider recovers, instead of transitioning to failed because the generic activity retry schedule was exhausted.

6. The rate-limiter actor window-open math is corrected. Given `RateLimiterLogic.ReportRateLimited` currently sets `WindowStart = now + retryAfter`, and `TryConsume` only refills after `WindowStart + 60s`, when a provider Retry-After is reported, then the next allowed local consume occurs at the intended retry-open instant, not one additional full rate-limit window later. Tests must cover Retry-After values of 30s and 90s and show the budget remains closed before the open instant and refills at/after it.

7. Provider 429 reporting to `EmbeddingRateLimiterActor` remains activity-owned and idempotent per provider call. Given `GenerateChunkEmbeddingsActivity` and `GenerateEmbeddingActivity` currently report provider 429s to `IEmbeddingRateLimiterActor.ReportRateLimitedAsync(...)`, when this story completes, then that actor feedback still happens once per provider 429, uses the effective retry-after value, and no workflow code directly mutates actor state.

8. Claim-checked payloads and chunk semantics survive timer replay. Given Story 23.2 stores extracted text, chunk text, and chunk vectors behind payload references and Story 23.1 stores raw semantic chunks under `{tenant}:vec:{memoryUnitId}:{seq}`, when a durable timer is inserted before an embedding retry, then transient payload references remain available for the retry, cleanup still runs on success/failure/duplicate paths, chunk sequence/order remain unchanged, and workflow history stays slim.

9. Non-429 failures still fail or compensate through the existing paths. Given embedding can fail because of validation, unsupported provider, migration-marker mismatch, missing payload, malformed provider response, transport error, cancellation, or local rate-limit exhaustion, when those failures occur, then the workflow must not silently durable-timer retry them as provider 429s unless they are explicitly classified as `EmbeddingRateLimitException` from provider 429 handling.

10. Tests prove durable recovery and documentation is updated. Given A13 is specifically about provider 429 recovery, when this story completes, then unit tests prove workflow timer scheduling and re-call behavior, rate-limiter window math, and Retry-After extraction/defaulting. Add or update an integration-style test or harness note for provider 429 recovery so `docs/operations/rate-limiting.md` no longer says `Retry-After > ~26s` fails permanently.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm A13 and inventory current 429 paths before editing (AC: 1-10)
  - [x] Read `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` completely, especially the raw `GenerateChunkEmbeddingsActivity` call, the event natural-language `GenerateEmbeddingActivity` call, failure handling, `AttachFailureDetails`, failed-unit persistence, and transient payload cleanup.
  - [x] Read `GenerateChunkEmbeddingsActivity`, `GenerateEmbeddingActivity`, `EmbeddingProviderTransport`, `EmbeddingRateLimitException`, `RetryPolicyBuilder`, `ActivityRetryPolicy`, `RateLimiterLogic`, and `EmbeddingRateLimiterActor`.
  - [x] Confirm how Dapr wraps activity failures in this SDK version. Use `WorkflowTaskFailedException.FailureDetails` and `WorkflowTaskFailureDetails.IsCausedBy<T>()`; do not assume the workflow catches a raw `EmbeddingRateLimitException`.
  - [x] Preserve Story 23.1 and 23.2 invariants: `GenerateChunkEmbeddingsActivity` remains the raw payload embedding activity, claim-checked references remain in activity inputs/results, and chunk order/keys remain stable.

- [x] Task 2 - Add a workflow-safe provider-429 retry helper (AC: 1-5, 7-9)
  - [x] Add a small private helper in `IngestionWorkflow` or a focused internal helper type that calls an embedding activity, catches only Dapr task failures caused by `EmbeddingRateLimitException`, schedules `context.CreateTimer(delay, CancellationToken.None)`, and retries the activity.
  - [x] Keep the retry loop bounded and deterministic. Prefer a named constant or config captured outside nondeterministic reads; do not introduce `DateTime.UtcNow`, random jitter, mutable process config reads, network calls, or actor calls inside the workflow loop.
  - [x] Do not use `Task.Delay`, `Thread.Sleep`, background services, actor reminders/timers, or host-local in-memory state for Retry-After waiting.
  - [x] Preserve existing failure details, custom status, failed-unit persistence, compensation, and cleanup behavior when the bounded durable retry loop ultimately fails.

- [x] Task 3 - Surface effective Retry-After to workflow safely (AC: 1, 4)
  - [x] Determine whether `WorkflowTaskFailureDetails.ErrorMessage` reliably contains the retry-after value. If not, change the activity-thrown exception message or add a small serializable non-secret detail so the workflow can recover the effective retry-after without parsing provider bodies.
  - [x] Keep error details sanitized: tenant ID and retry-after seconds are acceptable; provider response bodies, source text, vectors, bearer tokens, API keys, and payload references are not.
  - [x] Centralize effective-delay calculation so absent/non-positive retry-after still means 30 seconds and positive values are bounded to `[1, 3600]`.
  - [x] Add focused tests for positive Retry-After, missing Retry-After/default, malformed/non-parseable details, and excessive values.

- [x] Task 4 - Apply durable retry to raw payload embedding (AC: 2, 4-9)
  - [x] Replace the direct `context.CallActivityAsync<ChunkEmbeddingBatchResult>(nameof(GenerateChunkEmbeddingsActivity), ...)` call with the durable 429-aware helper.
  - [x] Reuse the existing per-activity `WorkflowTaskOptions` for non-429 retry behavior, but ensure provider 429 does not consume the generic retry sequence to terminal failure before the durable timer path can run.
  - [x] Keep `AddPayloadReferences(transientPayloads, embedding)` after a successful result only, and ensure a failed retry path does not leak claim-checked payloads.
  - [x] Verify a provider 429 before any chunk vectors are returned does not create duplicate semantic chunk payloads or duplicate vector writes.

- [x] Task 5 - Apply durable retry to natural-language embedding only (AC: 3, 8-9)
  - [x] Wrap only the `GenerateEmbeddingActivity` call that embeds `nlResult.Description`; do not wrap `GenerateNaturalLanguageDescriptionActivity` because LLM unavailability already has a separate queueing path.
  - [x] Preserve `NaturalLanguageDescriptionUnavailableException` handling and `QueueNaturalLanguageEmbeddingRetryActivity` semantics.
  - [x] Ensure event ingestion still sets `NaturalLanguageEmbeddingStatus.Indexed`, `Queued`, or `NotApplicable` exactly as before outside the provider-429 wait path.

- [x] Task 6 - Correct rate-limiter window math (AC: 6-7)
  - [x] Change `RateLimiterLogic.ReportRateLimited` so `TryConsume` reopens at the intended Retry-After instant. One acceptable implementation is to set `WindowStart = now + retryAfter - WindowDuration` while keeping `Remaining = 0`, with a floor that does not accidentally reopen immediately for small values unless tests prove that is desired.
  - [x] Keep `SetCeilingAsync`, `TryConsumeAsync`, actor persistence, and `RateLimitingLog` behavior intact.
  - [x] Update XML comments and docs to explain that provider 429 closes local admission until the provider Retry-After instant, not Retry-After plus another minute.

- [x] Task 7 - Update tests and validation evidence (AC: 1-10)
  - [x] Add workflow unit tests under `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` for raw embedding: first call fails with `WorkflowTaskFailedException` caused by `EmbeddingRateLimitException`, workflow schedules `CreateTimer`, second call succeeds, indexing proceeds, and failed-unit persistence is not called.
  - [x] Add equivalent event/NL workflow coverage in `IngestionWorkflowDualEmbeddingTests` or the existing workflow test file.
  - [x] Add negative workflow coverage proving non-429 `WorkflowTaskFailedException` still goes to the existing failure/compensation path.
  - [x] Update `RateLimiterLogicTests` and `EmbeddingRateLimiterActorTests` for 30s and 90s Retry-After reopen behavior.
  - [x] Extend `GenerateEmbeddingActivityTests` and `GenerateChunkEmbeddingsActivityTests` only where needed to pin effective retry-after reporting and sanitized failure details.
  - [x] Update or unskip/refine `tests/Hexalith.Memories.IntegrationTests/Ingestion/RateLimitingIntegrationTests.cs` with a deterministic 429-then-success scenario if the existing Aspire fixture can support it. If the fixture cannot support it yet, add a clear runnable harness note and unit-level proof instead of leaving stale Story 6.3 wording.
  - [x] Run `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run focused xUnit v3 tests for workflows, activities, and rate-limiter logic. If VSTest is blocked by the known sandbox TCP-listener issue, use the established `DiffEngine_Disabled=true dotnet exec ...Hexalith.Memories.Server.Tests.dll` fallback and record exact counts.
  - [x] Run `git diff --check` and record the existing CRLF caveat only if it recurs.

## Dev Notes

### Current State and Code Anchors

`IngestionWorkflow` currently calls `GenerateChunkEmbeddingsActivity` once for raw payload embedding, passing `EmbeddingInput` with either inline extracted content or `ExtractionResult.ExtractedContentReference`. The call uses `For(nameof(GenerateChunkEmbeddingsActivity))`, so provider 429s are governed by the same generic `WorkflowRetryPolicy` that was already identified as too short for long Retry-After windows. [Source: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`; `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A13`]

Event ingestion may also call `GenerateEmbeddingActivity` for a natural-language description after `GenerateNaturalLanguageDescriptionActivity` succeeds. That call should get durable provider-429 handling, but the `NaturalLanguageDescriptionUnavailableException` branch that queues `QueueNaturalLanguageEmbeddingRetryActivity` is a different degradation path and must not be folded into 429 handling. [Source: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`; `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`]

`GenerateChunkEmbeddingsActivity` owns the raw chunk provider calls. It resolves claim-checked extracted text when present, chunks with `ContentChunker`, reads `TenantEmbeddingConfig`, checks active migration markers, primes credentials, sets the per-tenant rate-limit ceiling, consumes one rate-limit token per bounded provider batch, calls `EmbeddingClient.GenerateBatchAsync(...)`, stores chunk text/vector payload references, and reports provider 429s to the actor only while a provider call is in progress. Preserve this boundary. [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs`; `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md`; `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`]

`GenerateEmbeddingActivity` still owns single-text embedding for natural-language descriptions, query/migration-like consumers, and compatibility. It reports provider 429s to the rate limiter, applies a 30 second default when `RetryAfterSeconds <= 0`, and currently applies host-local retry jitter on repeated executions. This story should not move tenant config, actor, migration marker, telemetry, or provider calls out of the activity. [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs`; `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md`]

`EmbeddingProviderTransport` is the HTTP boundary that maps provider 429s to `EmbeddingRateLimitException` and parses `Retry-After`. Story 23.9 review fixed `Retry-After: 0` to remain `0`, malformed Google response redaction, and Ollama response-count mismatch tolerance. Do not loosen those contracts or duplicate provider-specific HTTP logic in the workflow. [Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderTransport.cs`; `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md#Senior-Developer-Review-AI`]

`ActivityRetryPolicy` defaults to five attempts, 2 second first retry interval, 1.5 backoff, and 5 minute max interval. The planned A13 fix is not to increase this generic policy globally; it is to keep provider 429s in a workflow-controlled durable wait path while preserving normal retry behavior for transient non-429 activity failures. [Source: `src/Hexalith.Memories.Server/Ingestion/ActivityRetryPolicy.cs`; `src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs`; `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs`]

`RateLimiterLogic.ReportRateLimited` currently sets `WindowStart = now + retryAfter`. Because `TryConsume` refills only when `now - WindowStart >= 1 minute`, a 90 second Retry-After actually reopens after about 150 seconds. The story must fix that math and update tests/comments/docs. [Source: `src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs`; `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs`; `docs/operations/rate-limiting.md#Provider-429-handling`]

### Architecture Constraints

- Dapr Workflow code must remain deterministic and side-effect free. Use workflow tasks returned by `WorkflowContext` only; no `Task.Delay`, random values, wall-clock reads, direct Redis/Dapr state calls, HTTP calls, or mutable config reads inside workflow orchestration. [Source: `_bmad-output/project-context.md#Framework-Specific-Rules`; `Dapr.Workflow.Abstractions.xml` from local package `Dapr.Workflow` 1.18.4]
- The pinned Dapr Workflow package exposes `WorkflowContext.CreateTimer(TimeSpan, CancellationToken)` and `CreateTimer(DateTime, CancellationToken)` as durable timers. Use those methods for Retry-After waits. [Source: `/home/administrator/.nuget/packages/dapr.workflow/1.18.4/lib/net10.0/Dapr.Workflow.Abstractions.xml`]
- Activity failures are surfaced as `WorkflowTaskFailedException`; the underlying task failure details are in `WorkflowTaskFailedException.FailureDetails`, including `ErrorType`, `ErrorMessage`, and `IsCausedBy<T>()`. This is the safe classification path for workflow code. [Source: `/home/administrator/.nuget/packages/dapr.workflow/1.18.4/lib/net10.0/Dapr.Workflow.Abstractions.xml`]
- Keep actor state updates in activities. Workflows orchestrate sequencing and timers; `EmbeddingRateLimiterActor` remains the per-tenant stateful singleton that records provider 429 feedback. [Source: `_bmad-output/planning-artifacts/architecture.md#Complete-Decision-Registry`; `_bmad-output/project-context.md#Framework-Specific-Rules`]
- Keep claim-check and cleanup semantics from Story 23.2. Durable waits must not delete transient payload references before retry and must not expose raw content, vectors, or payload values through status/errors/logs. [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`]
- No package upgrade is required. Dapr packages are pinned through central package management; do not add versions to `.csproj`. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`; `Directory.Packages.props`]

### Previous Story Intelligence

Story 23.9 is done. Provider-specific request/auth/response behavior is behind provider strategies, with shared transport and `EmbeddingRateLimitException` mapping. Reuse those surfaces; do not reintroduce provider branching in workflow code. [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md`]

Story 23.1 is done. Raw payload embedding now chunks extracted content and calls `EmbeddingClient.GenerateBatchAsync(...)` through `GenerateChunkEmbeddingsActivity`; raw semantic vectors are stored under `{tenant}:vec:{memoryUnitId}:{seq}` and search dedupes by base memory unit. Durable 429 retry must preserve chunk sequence/order and chunked semantic storage. [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md`]

Story 23.2 is done. New ingestion paths should keep large content/vectors out of workflow history with `WorkflowPayloadReference`. Durable retry must preserve referenced extracted text across a timer wait and run existing cleanup paths only after success, duplicate, compensation, or terminal failure. [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`]

Story 23.2 senior review fixed duplicate-path payload cleanup and File List hygiene. Do not reintroduce payload leaks when adding a retry loop around embedding. [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md#Dev-Agent-Record`]

### Git Intelligence

Recent commits before story creation:

- `906f819 feat(story-23.2): Claim-Check Workflow Payloads`
- `6935421 feat(story-23.1): Content Chunking & Batch Embedding`
- `a645b96 feat(story-23.9): EmbeddingClient Provider Strategy`
- `ae8bb1e docs(epic-22): close retrospective and sync retrieval docs`
- `28ab3d3 feat(story-22.7): Retrieval Feature Completion`

Pattern: Epic 23 work is source-anchored and guarded by focused unit tests plus xUnit v3 fallback validation. Continue that pattern. Story 23.3 changes orchestration behavior and rate-limit math, so workflow tests and actor logic tests are mandatory.

### Latest Technical / Library Notes

- The local `Dapr.Workflow` package is 1.18.4 and includes durable `WorkflowContext.CreateTimer` overloads for both `TimeSpan` and `DateTime`. Prefer `TimeSpan` for Retry-After seconds because the provider gives a relative delay and it avoids wall-clock math in workflow code. [Source: `/home/administrator/.nuget/packages/dapr.workflow/1.18.4/lib/net10.0/Dapr.Workflow.Abstractions.xml`]
- The same package documents deterministic workflow restrictions: activities perform external work, workflow code may replay, and awaits should be restricted to workflow tasks or wrappers such as `Task.WhenAll`. Durable timer awaits are valid workflow tasks; `Task.Delay` is not. [Source: `/home/administrator/.nuget/packages/dapr.workflow/1.18.4/lib/net10.0/Dapr.Workflow.Abstractions.xml`]
- No dependency or SDK upgrade is required for this story.

### Scope Boundaries

In scope:
- Workflow-level durable Retry-After handling for provider 429s from raw payload embedding and event natural-language embedding.
- A deterministic helper for detecting activity failures caused by `EmbeddingRateLimitException` and selecting the effective timer delay.
- Correcting `RateLimiterLogic.ReportRateLimited` / `TryConsume` reopen behavior.
- Updating `docs/operations/rate-limiting.md` and stale integration-test notes that still describe long Retry-After as terminal.
- Focused workflow, activity, actor logic, and optional integration harness coverage.

Out of scope:
- Provider strategy, HTTP request/response shapes, secret retrieval, OIDC refresh, and `EmbeddingClient.GenerateBatchAsync(...)` behavior. That was Story 23.9.
- Chunking algorithm, chunk key shape, semantic search dedupe, graph-scope chunk expansion, and migration/chunk consistency. That was Story 23.1.
- Claim-check payload store design, payload reference contracts, and broad workflow-history slimming. That was Story 23.2.
- Making failed non-URL re-ingestion work. That is Story 23.4.
- Rate-limiter admission API redesign, single-round-trip actor calls, Redis Lua token bucket, and tenant config caching. That is Story 23.5.
- Directory batch scalability, index provisioning ownership, workflow config determinism beyond this timer loop, UI/web work, and submodule changes.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`.
- Tests belong under matching folders: `Workflows`, `Activities/Ingestion`, `Actors`, and `Ingestion` for docs/harness-facing cases.
- Workflow tests should verify `CreateTimer` is scheduled with the expected delay and that the embedding activity is called again after the timer path. If `WorkflowContext.CreateTimer` is hard to substitute directly, add a narrow workflow timer seam or helper that remains test-only/internal and does not hide nondeterminism.
- Activity tests should not make live Google, Ollama, Redis, or Dapr sidecar calls. Provider 429s should be simulated through `EmbeddingRateLimitException`.
- If normal `dotnet test` is blocked by the known VSTest TCP-listener sandbox issue, use the established xUnit v3 in-process `dotnet exec` fallback and record exact commands/counts.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story-23.3` - story statement and A13 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A13` - finding: retry budget shorter than rate-limit window]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-23` - approved A13 remediation scope]
- [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md` - provider 429 mapping and Retry-After parsing]
- [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md` - chunked raw embedding path]
- [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md` - claim-check payload references and cleanup]
- [Source: `_bmad-output/project-context.md` - .NET 10/C# 14, Dapr, testing, workflow, and tenant-isolation rules]
- [Source: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` - current embedding orchestration]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs` - raw chunk embedding and provider 429 reporting]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` - single-text/NL embedding and provider 429 reporting]
- [Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderTransport.cs` - HTTP 429 to `EmbeddingRateLimitException` mapping]
- [Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingRateLimitException.cs` - retry-after contract]
- [Source: `src/Hexalith.Memories.Server/Ingestion/ActivityRetryPolicy.cs` and `RetryPolicyBuilder.cs` - generic activity retry schedule]
- [Source: `src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs` - current window math]
- [Source: `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs` - current actor logic coverage]
- [Source: `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` - current workflow retry-policy tests]
- [Source: `docs/operations/rate-limiting.md#Provider-429-handling` - operations documentation to update]
- [Source: `/home/administrator/.nuget/packages/dapr.workflow/1.18.4/lib/net10.0/Dapr.Workflow.Abstractions.xml` - durable timer and failure-detail API surface]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-05: Loaded repository AGENTS instructions and `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`.
- 2026-07-05: Loaded `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- 2026-07-05: Resolved workflow customization with `_bmad/scripts/resolve_customization.py`; activation steps were empty, persistent facts were `file:{project-root}/**/project-context.md`, and `workflow.on_complete` was empty.
- 2026-07-05: Loaded BMM config: user `Jerome`, project `memories`, planning artifacts `_bmad-output/planning-artifacts`, implementation artifacts `_bmad-output/implementation-artifacts`, English communication/output.
- 2026-07-05: Target story supplied by user as `23.3`; selected story key `23-3-retry-after-aware-429-orchestration`.
- 2026-07-05: Confirmed sprint status before creation: `epic-23: in-progress`, `23-1` done, `23-2` done, `23-3` backlog, `23-9` done.
- 2026-07-05: Loaded project context, Epic 23 source, A13 audit finding, sprint-change proposal, architecture workflow/actor rules, current workflow/activity/rate-limiter source files, prior Epic 23 story files, local Dapr Workflow package XML, docs, tests, and recent git commits.
- 2026-07-05: Validated story context against checklist concerns: avoided provider strategy reinvention, preserved chunking and claim-check scope, called out Dapr activity failure wrapping, required durable timer tests, and bounded scope away from Stories 23.4-23.8.
- 2026-07-05: Dev-story activation loaded `.agents/skills/bmad-dev-story/SKILL.md`, `checklist.md`, BMM config, project context, sprint status, and Hexalith LLM/state instructions.
- 2026-07-05: Confirmed Dapr Workflow 1.18.4 activity failures surface as `WorkflowTaskFailedException` with `WorkflowTaskFailureDetails.IsCausedBy<T>()`; implemented provider-429 classification through sanitized retry-after failure details.
- 2026-07-05: Implemented bounded durable provider-429 retry in `IngestionWorkflow` for raw chunk embedding and event natural-language embedding, with one-attempt activity scheduling and workflow `CreateTimer` waits.
- 2026-07-05: Corrected `RateLimiterLogic.ReportRateLimited` window math so local admission reopens at the provider Retry-After instant.
- 2026-07-05: `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` passed with 0 warnings, 0 errors.
- 2026-07-05: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed with 0 warnings, 0 errors.
- 2026-07-05: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~IngestionWorkflowTests|FullyQualifiedName~IngestionWorkflowDualEmbeddingTests|FullyQualifiedName~RateLimiterLogicTests|FullyQualifiedName~EmbeddingRateLimiterActorTests|FullyQualifiedName~GenerateEmbeddingActivityTests|FullyQualifiedName~GenerateChunkEmbeddingsActivityTests|FullyQualifiedName~EmbeddingRateLimitRetryAfterTests"` was blocked by the known VSTest sandbox TCP listener issue: `SocketException (13): Permission denied`.
- 2026-07-05: Focused xUnit fallback passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowDualEmbeddingTests -class Hexalith.Memories.Server.Tests.Actors.RateLimiterLogicTests -class Hexalith.Memories.Server.Tests.Actors.EmbeddingRateLimiterActorTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateChunkEmbeddingsActivityTests -class Hexalith.Memories.Server.Tests.Ingestion.EmbeddingRateLimitRetryAfterTests -parallel none -noLogo` -> 114 total, 0 failed, 0 skipped.
- 2026-07-05: Full Server.Tests xUnit fallback passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo` -> 2348 total, 0 failed, 1 skipped.
- 2026-07-05: `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` passed with 0 warnings, 0 errors.
- 2026-07-05: `git diff --check` passed clean.

### Completion Notes List

- Provider 429s from raw chunk embedding and event natural-language embedding now stay in workflow-owned durable Retry-After waits and retry the embedding activity without burning through Dapr's short generic retry budget.
- Provider retry-after values are surfaced to workflow failure details through a sanitized marker containing only tenant context and effective seconds; local actor-budget denials do not take the provider durable-timer path.
- `RateLimiterLogic.ReportRateLimited` now reopens local admission at the intended Retry-After instant, with 30s and 90s behavior covered.
- Operations documentation and the skipped integration harness note no longer claim long Retry-After values fail permanently; the remaining real-sidecar 429-then-success integration gap is documented.
- Senior review corrected stale validation counts in the Dev Agent Record, marked the story done, and synced sprint status.

### File List

- `_bmad-output/implementation-artifacts/23-3-retry-after-aware-429-orchestration.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/operations/rate-limiting.md`
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs`
- `src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingRateLimitException.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingRateLimitRetryAfter.cs`
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/RateLimitingIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateChunkEmbeddingsActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/EmbeddingRateLimiterActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingRateLimitRetryAfterTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowDualEmbeddingTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-05

Outcome: Approved after automatic fixes.

Findings:

- [x] [AI-Review][Medium] Dev Agent Record validation counts were stale after review reruns: focused suite was 114 tests and full Server.Tests was 2348 tests. Fixed the recorded counts.
- [x] [AI-Review][Medium] Story and sprint tracking were still in `review` after all critical issues were resolved. Fixed story `Status: done` and `development_status[23-3-retry-after-aware-429-orchestration] = done`.

Validation:

- `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` -> passed, 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -> passed, 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` -> passed, 0 warnings, 0 errors.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~IngestionWorkflowTests|FullyQualifiedName~IngestionWorkflowDualEmbeddingTests|FullyQualifiedName~RateLimiterLogicTests|FullyQualifiedName~EmbeddingRateLimiterActorTests|FullyQualifiedName~GenerateEmbeddingActivityTests|FullyQualifiedName~GenerateChunkEmbeddingsActivityTests|FullyQualifiedName~EmbeddingRateLimitRetryAfterTests"` -> blocked by known VSTest sandbox TCP listener issue: `SocketException (13): Permission denied`.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowDualEmbeddingTests -class Hexalith.Memories.Server.Tests.Actors.RateLimiterLogicTests -class Hexalith.Memories.Server.Tests.Actors.EmbeddingRateLimiterActorTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateChunkEmbeddingsActivityTests -class Hexalith.Memories.Server.Tests.Ingestion.EmbeddingRateLimitRetryAfterTests -parallel none -noLogo` -> 114 total, 0 failed, 0 skipped.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo` -> 2348 total, 0 failed, 1 skipped.
- `git diff --check` -> passed clean.

### Change Log

| Date | Version | Description |
| --- | --- | --- |
| 2026-07-05 | 1.0 | Implemented retry-after-aware provider 429 orchestration, corrected actor reopen math, updated docs/harness notes, and added focused workflow/activity/actor tests. |
| 2026-07-05 | 1.1 | Senior review approved story after validation-count correction and sprint status sync. |
