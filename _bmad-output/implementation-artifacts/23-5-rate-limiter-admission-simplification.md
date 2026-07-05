---
baseline_commit: 54f6292
---

# Story 23.5: Rate-Limiter Admission Simplification

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want embedding admission control to cost one round trip,
so that the limiter is not the throughput ceiling (NFR5).

Story 23.5 follows Story 23.9, Story 23.1, Story 23.2, Story 23.3, and Story 23.4 in `story_execution_order.epic-23`. The provider strategy, batch embedding, claim-check payloads, durable provider-429 retry, and non-URL re-ingestion are already done. This story closes A15 by simplifying the admission path without changing provider transport, chunking, workflow retry, or failed-unit recovery semantics.

## Acceptance Criteria

1. Single provider-call admission RPC. Given `GenerateEmbeddingActivity` currently calls tenant config, then `SetCeilingAsync`, then `TryConsumeAsync` before a single provider call, when this story completes, then local embedding admission for that provider call is one remote admission operation after config is known. The actor option should be a method such as `TryConsumeAsync(int ceiling)` or equivalent; a Redis Lua token bucket is allowed only if it is tenant-scoped, atomic, and does not bypass existing Dapr/state ownership without explicit tests.

2. Chunked batch embedding uses one admission per provider batch, not per chunk. Given Story 23.1 moved raw payload embedding to `GenerateChunkEmbeddingsActivity`, when the activity embeds multiple bounded batches, then each provider `GenerateBatchAsync` call performs one admission operation with the current ceiling, and no separate `SetCeilingAsync` call is made for that batch.

3. Single-text and natural-language embedding paths use the same simplified admission contract. Given `GenerateEmbeddingActivity` is still used for natural-language description embeddings and retry workflows, when it runs, then it uses the same single admission operation and preserves existing local-denial behavior, telemetry tags, migration-marker checks, provider 429 reporting, and sanitized retry-after exceptions.

4. Tenant embedding configuration is cached safely. Given both embedding activities currently call `TenantConfigurationActor.GetEmbeddingConfigAsync()` on every activity run, when repeated embedding calls occur for the same tenant, then provider/model/dimensions/rate-limit config is cached per process with a short bounded TTL or explicit invalidation on `SetEmbeddingConfigAsync`. The cache must not leak config across tenants, must not cache invalid/corrupted fallback decisions indefinitely, and must still observe tenant rate-limit updates within the documented bound.

5. Admission remains tenant-isolated and durable. Given the existing `EmbeddingRateLimiterActor` persists `RateLimitState` before returning observable results, when admission is simplified, then state updates remain atomic per tenant and persisted before the activity proceeds to the provider call. Concurrent callers for the same tenant must not oversubscribe `CeilingPerMinute`; callers for different tenants must remain isolated.

6. Provider 429 feedback remains separate and idempotent. Given Story 23.3 made provider 429s report `ReportRateLimitedAsync(retryAfter)` and use workflow-owned durable timers, when this story completes, then provider 429 feedback still happens only for provider 429s while a provider call is in progress, and local admission denials still do not call `ReportRateLimitedAsync`.

7. Backward compatibility is preserved for actor callers and tests. Given older code/tests may still call `SetCeilingAsync` and parameterless `TryConsumeAsync`, when changing `IEmbeddingRateLimiterActor`, then either keep compatible members for non-admission callers or update every caller/test in the repo deliberately. Do not leave two competing production admission paths in embedding activities.

8. No broad rate-limiter redesign is introduced. Given A15 targets admission round trips, when implementing this story, then do not change provider strategy, `EmbeddingClient` request/response parsing, chunking rules, workflow timer retry logic, non-URL re-ingestion, migration marker semantics, or inbound ASP.NET rate limiting except where tests need to be adjusted to the new admission contract.

9. Tests prove the throughput bottleneck is removed. Given A15 is specifically about the limiter becoming the throughput ceiling, when the story completes, then tests prove `GenerateEmbeddingActivity` and `GenerateChunkEmbeddingsActivity` no longer call `SetCeilingAsync` before every consume, actor/logic tests prove `TryConsume(ceiling)` updates ceiling and consumes atomically, and a concurrency test proves one tenant cannot admit more than its configured budget under parallel calls.

10. Documentation and story evidence explain the chosen design. Given the sprint change proposal allowed either a single actor method or Redis Lua token bucket, when the story completes, then the story record and any touched operations docs state which option was chosen, why it preserves tenant isolation, how tenant config cache freshness works, and what validation was run.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm A15 and current admission flow before editing (AC: 1-10)
  - [x] Read `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` completely. Confirm the current sequence is `GetEmbeddingConfigAsync` -> migration marker check -> `PrimeApiKeyAsync` -> `SetCeilingAsync` -> `TryConsumeAsync` -> provider call.
  - [x] Read `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs` completely. Confirm the current sequence is one `SetCeilingAsync` before the loop and one `TryConsumeAsync` per provider batch.
  - [x] Read `src/Hexalith.Memories.Server/Actors/IEmbeddingRateLimiterActor.cs`, `EmbeddingRateLimiterActor.cs`, and `RateLimiterLogic.cs` completely.
  - [x] Read tenant config surfaces: `ITenantConfigurationActor`, `TenantConfigurationActor`, `TenantEmbeddingConfig`, and `EmbeddingProviderDefaults`.
  - [x] Read tests that pin the old call sequence: `GenerateEmbeddingActivityTests`, `GenerateEmbeddingActivityConfigTests`, `GenerateChunkEmbeddingsActivityTests`, `EmbeddingRateLimiterActorTests`, and `RateLimiterLogicTests`.

- [x] Task 2 - Implement single-operation actor admission (AC: 1, 2, 3, 5-7)
  - [x] Prefer adding `Task<bool> TryConsumeAsync(int ceiling)` to `IEmbeddingRateLimiterActor`.
  - [x] Implement it in `EmbeddingRateLimiterActor` by loading current `RateLimitState`, applying the ceiling, trying to consume, and saving the resulting state exactly once before returning.
  - [x] Add a testable `RateLimiterLogic.TryConsume(RateLimitState currentState, int ceiling)` or equivalent helper that validates positive ceilings, clamps remaining budget to lower ceilings, preserves lower remaining values when ceilings rise, resets expired windows to the current ceiling, and decrements atomically.
  - [x] Preserve `ReportRateLimitedAsync(int retryAfterSeconds)` semantics from Story 23.3.
  - [x] Keep existing `SetCeilingAsync` and parameterless `TryConsumeAsync` only if needed by non-admission callers/tests; embedding activities should use the single-operation method.

- [x] Task 3 - Add safe tenant embedding config cache (AC: 4, 8, 10)
  - [x] Add a small internal service or helper for tenant embedding config lookup. Prefer an injected `ITenantEmbeddingConfigProvider` over static dictionaries in activities.
  - [x] Cache by tenant id only, with a bounded TTL that is short enough to keep rate-limit updates observable. If choosing explicit invalidation, wire it through the existing `SetEmbeddingConfigAsync` endpoint/actor write path and test it.
  - [x] Do not cache provider secrets; cache only `TenantEmbeddingConfig` values returned by the tenant configuration actor.
  - [x] Do not cache invalid/corrupted state fallback forever. If `TenantConfigurationActor` falls back to defaults because stored state is invalid, the cache must expire normally and allow later repair to be observed.
  - [x] Register the cache/provider in existing composition roots without package upgrades and without adding versions to `.csproj` files.

- [x] Task 4 - Update embedding activities to use simplified admission (AC: 1-3, 6, 8)
  - [x] Update `GenerateEmbeddingActivity` to obtain config through the cache/provider, preserve the migration marker check, prime provider credentials, and call a single admission method with `config.RateLimitPerMinute`.
  - [x] Update `GenerateChunkEmbeddingsActivity` to obtain config through the cache/provider, preserve the migration marker check and credential priming, and call the single admission method once per `GenerateBatchAsync` provider batch.
  - [x] Preserve local admission denial behavior: log `LogRateLimitExceededLocally`, throw `EmbeddingRateLimitException(input.TenantId)`, do not call provider APIs, and do not call `ReportRateLimitedAsync`.
  - [x] Preserve provider 429 behavior: only call `ReportRateLimitedAsync(retryAfter)` when a provider call was in progress and the provider throws `EmbeddingRateLimitException`.
  - [x] Do not move migration marker reads, API key priming, provider selection, chunking, payload-store reads/writes, or telemetry counters into the actor.

- [x] Task 5 - Update or replace old call-sequence tests (AC: 1-4, 7, 9)
  - [x] Replace tests that expect `SetCeilingAsync` followed by `TryConsumeAsync` with assertions that embedding activities call the single admission method with the configured ceiling.
  - [x] Add `GenerateChunkEmbeddingsActivityTests` coverage proving multi-batch payloads call single admission once per provider batch and never call `SetCeilingAsync`.
  - [x] Add `GenerateEmbeddingActivityTests` and/or `GenerateEmbeddingActivityConfigTests` coverage proving repeated same-tenant calls use cached config within the freshness bound and fetch again after expiration/invalidation.
  - [x] Add negative tests for local admission refusal and provider 429 reporting so Story 23.3 behavior is not regressed.
  - [x] Update natural-language retry or content-kind tests if their actor substitutes need the new admission method.

- [x] Task 6 - Add concurrency and isolation proof (AC: 5, 9)
  - [x] Add `RateLimiterLogicTests` for concurrent-equivalent serialized consumption with a ceiling of 1 and many attempts. Exactly one should be admitted in the active window.
  - [x] Add `EmbeddingRateLimiterActorTests` proving `TryConsumeAsync(ceiling)` persists one state update per admission operation and clamps/downshifts the ceiling correctly.
  - [x] Add a focused parallel test at the actor or logic seam that simulates same-tenant concurrent admissions without requiring live Dapr sidecars. If the Dapr actor test host cannot run true parallel calls, test the serialized actor contract plus an integration-style note for real sidecar validation.
  - [x] Add a different-tenant isolation test if a new cache/provider service is introduced, proving tenant A config/admission does not affect tenant B.

- [x] Task 7 - Documentation and validation evidence (AC: 9-10)
  - [x] Update `docs/operations/rate-limiting.md` only if it describes the old multi-call admission or cache behavior. Do not create docs churn otherwise.
  - [x] Record the chosen design in this story's Dev Agent Record: actor single method or Redis Lua token bucket, config-cache TTL/invalidation, and concurrency proof.
  - [x] Run `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
  - [x] Run focused xUnit v3 tests for activities, actor logic, config cache, and rate-limiter concurrency. If VSTest is blocked by the known sandbox TCP-listener issue, use the established `DiffEngine_Disabled=true dotnet exec ...Hexalith.Memories.Server.Tests.dll` fallback and record exact counts.
  - [x] Run `git diff --check`.

## Dev Notes

### Current State and Code Anchors

`GenerateEmbeddingActivity.RunAsync` currently creates a tenant configuration actor proxy, calls `GetEmbeddingConfigAsync()`, verifies migration marker compatibility, primes the provider API key, creates the rate-limiter actor proxy, calls `SetCeilingAsync(config.RateLimitPerMinute)`, then calls `TryConsumeAsync()` before `EmbeddingClient.GenerateAsync(...)`. That is the old single-text path named in A15. [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs`; `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A15`]

`GenerateChunkEmbeddingsActivity.RunAsync` is now the raw document path after Story 23.1. It resolves extracted text payloads, chunks content, reads tenant config, checks migration markers, primes the provider API key, calls `SetCeilingAsync(config.RateLimitPerMinute)` once, and then calls `TryConsumeAsync()` once per bounded provider batch before `EmbeddingClient.GenerateBatchAsync(...)`. Story 23.5 must optimize this path too; optimizing only `GenerateEmbeddingActivity` leaves the main document-ingestion path behind. [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs`; `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md`]

`IEmbeddingRateLimiterActor` currently exposes parameterless `TryConsumeAsync()`, `SetCeilingAsync(int)`, `ResetAsync()`, `GetStateAsync()`, and `ReportRateLimitedAsync(int)`. `EmbeddingRateLimiterActor` loads state and persists state once per method call. A single `TryConsumeAsync(int ceiling)` actor method is the least disruptive A15 fix because it preserves the Dapr actor as the per-tenant stateful singleton while collapsing the `SetCeilingAsync` and `TryConsumeAsync` RPC pair into one. [Source: `src/Hexalith.Memories.Server/Actors/IEmbeddingRateLimiterActor.cs`; `src/Hexalith.Memories.Server/Actors/EmbeddingRateLimiterActor.cs`; `_bmad-output/planning-artifacts/architecture.md#DAPR-Actor-Patterns`]

`RateLimiterLogic` already has pure logic for `SetCeiling`, `TryConsume`, `Reset`, and `ReportRateLimited`. Story 23.3 corrected `ReportRateLimited` so Retry-After opens at the intended instant. Do not regress that math while adding a combined consume-with-ceiling helper. [Source: `src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs`; `_bmad-output/implementation-artifacts/23-3-retry-after-aware-429-orchestration.md`]

`TenantConfigurationActor.GetEmbeddingConfigAsync()` returns stored config or `EmbeddingProviderDefaults.Google()` if unconfigured, invalid, or corrupted. Caching should happen outside the actor or through a focused provider service so activities do not own ad hoc static cache state. The cache must respect tenant id boundaries and the update freshness requirement from Story 6.2/5.5 tests. [Source: `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs`; `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs`; `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`]

Existing tests currently pin the old two-call rate-limiter sequence. `RunAsync_PropagatesConfiguredRateLimitCeilingToRateLimiter`, `RunAsync_CeilingChangedBetweenInvocations_ReflectsLatestConfig`, and `GenerateEmbeddingActivityConfigTests` will need deliberate updates to the new contract. Do not leave tests asserting stale behavior or delete the config-change coverage without replacing it with cache freshness/invalidation coverage. [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs`; `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityConfigTests.cs`]

### Architecture Constraints

- Dapr actors remain the preferred per-tenant stateful singleton mechanism in the current architecture. Use the actor single-method path unless there is a concrete reason to choose Redis Lua. If Redis Lua is chosen, the implementation must use tenant-scoped keys, atomic scripts, TTL/window semantics equivalent to `RateLimiterLogic`, and tests for rollback from actor ownership. [Source: `_bmad-output/planning-artifacts/architecture.md#Technology-Stack-Architecture-Decisions`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-23`]
- Actor state must be persisted before returning observable admission results. Do not use in-memory-only counters for admission. [Source: `_bmad-output/planning-artifacts/architecture.md#Technology-Stack-Architecture-Decisions`; `_bmad-output/project-context.md#Framework-Specific-Rules`]
- Workflow code must remain deterministic. This story should touch activities/actors/services, not add direct state reads, wall-clock reads, random values, Redis scripts, or HTTP calls inside `IngestionWorkflow`. [Source: `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`]
- Tenant isolation is non-negotiable. Cache keys, actor IDs, telemetry tags, and rate-limit state must remain tenant-scoped. [Source: `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`]
- Keep provider contracts behind `EmbeddingClient` and provider strategies. Do not reintroduce provider-specific admission logic or transport/auth branching in activities. [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md`]
- No dependency upgrade is required. Use .NET 10/C# 14, Dapr 1.18.4, xUnit v3, Shouldly, and NSubstitute already pinned in the repo. Package versions remain centralized. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`; `Directory.Packages.props`]

### Previous Story Intelligence

Story 23.9 is done. Provider-specific request/auth/response behavior is behind provider strategies and `EmbeddingClient.GenerateBatchAsync(...)`; Story 23.5 must not move rate limiting into provider implementations. [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md`]

Story 23.1 is done. Raw content uses chunked batch embedding through `GenerateChunkEmbeddingsActivity`; the admission unit should be a provider batch call, not each chunk and not the whole workflow if multiple provider calls are required. [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md`]

Story 23.2 is done. Claim-check payload references and cleanup must remain unchanged; admission simplification should not read payloads, write payloads, or alter workflow history. [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md`]

Story 23.3 is done. Provider 429s are workflow-retried through durable timers and `RateLimiterLogic.ReportRateLimited` now opens at the provider Retry-After instant. Preserve `ReportRateLimitedAsync` and provider-call-in-progress guards. [Source: `_bmad-output/implementation-artifacts/23-3-retry-after-aware-429-orchestration.md`]

Story 23.4 is done. Failed non-URL re-ingestion now relies on source-byte payload references. This story should not change re-ingestion coordinator, failed-unit registry, failed-unit API mapping, or payload retention behavior. [Source: `_bmad-output/implementation-artifacts/23-4-non-url-re-ingestion.md`]

### Git Intelligence

Recent commits before story creation:

- `54f6292 feat(story-23.4): Non-URL Re-Ingestion`
- `1ef8a18 feat(references): update Hexalith.FrontComposer subproject commit`
- `acfeca8 feat(story-23.3): update subproject references and finalize story status`
- `c77c723 feat(story-23.3): Retry-After-Aware 429 Orchestration`
- `906f819 feat(story-23.2): Claim-Check Workflow Payloads`

Pattern: Epic 23 stories are source-anchored, scoped to the audit finding, and validated with focused server tests plus xUnit v3 fallback when VSTest is blocked. Continue that pattern.

### Latest Technical / Library Notes

- No external API or library research changes the implementation guidance. The story should use repository-pinned Dapr actors, StackExchange.Redis only if the Redis Lua option is chosen deliberately, and existing xUnit v3/Shouldly/NSubstitute testing patterns. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`]
- If implementing a cache with `IMemoryCache`, verify the package is already available through the shared ASP.NET Core stack before adding references. Do not add a new package version to a `.csproj`. [Source: `_bmad-output/project-context.md#Code-Quality-Style-Rules`]

### Scope Boundaries

In scope:

- Combined admission API for rate limiter actor or an explicitly justified tenant-scoped Redis Lua token bucket.
- Config caching for tenant embedding config used by embedding activities.
- Updating `GenerateEmbeddingActivity` and `GenerateChunkEmbeddingsActivity` to use the simplified admission path.
- Unit/concurrency tests proving no oversubscription and no stale old call sequence.
- Minimal documentation/evidence updates for chosen design and cache freshness.

Out of scope:

- Provider strategy, transport/auth behavior, provider request/response parsing, OIDC token handling, and `EmbeddingClient` refactors.
- Chunking algorithm, chunk vector key shape, payload claim-check design, failed-unit/re-ingestion behavior, workflow durable timer retry behavior, migration execution, directory batch scalability, index provisioning memoization, and workflow config determinism.
- Inbound ASP.NET request rate limiting, MCP/CLI/Web work, submodule updates, package upgrades, and broad architecture rewrites.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`.
- Tests belong under matching folders: `Activities/Ingestion`, `Actors`, and `Ingestion` or a new focused cache/provider test folder only if a new service is introduced.
- Activity tests should not require live Dapr sidecars, Redis, Google, Ollama, or external network calls.
- Concurrency tests should be deterministic. Prefer pure `RateLimiterLogic` and actor-host tests; only use integration tests if they already have a reliable sidecar fixture.
- If normal `dotnet test` is blocked by the known VSTest TCP-listener sandbox issue, use the established xUnit v3 in-process `dotnet exec` fallback and record exact commands/counts.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story-23.5` - story statement and A15 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A15` - finding: three serialized actor round trips per embedding call]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-23` - approved A15 remediation scope]
- [Source: `_bmad-output/planning-artifacts/architecture.md#DAPR-Actor-Patterns` - existing actor design]
- [Source: `_bmad-output/project-context.md` - .NET 10/C# 14, Dapr, testing, workflow, tenant-isolation, and contract rules]
- [Source: `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md` - provider strategy prerequisite]
- [Source: `_bmad-output/implementation-artifacts/23-1-content-chunking-and-batch-embedding.md` - chunked batch embedding prerequisite]
- [Source: `_bmad-output/implementation-artifacts/23-2-claim-check-workflow-payloads.md` - payload reference prerequisite]
- [Source: `_bmad-output/implementation-artifacts/23-3-retry-after-aware-429-orchestration.md` - provider 429 and rate-limiter math prerequisite]
- [Source: `_bmad-output/implementation-artifacts/23-4-non-url-re-ingestion.md` - immediate previous story and scope boundary]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` - current single-text/NL admission path]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs` - current raw chunked admission path]
- [Source: `src/Hexalith.Memories.Server/Actors/IEmbeddingRateLimiterActor.cs` - actor interface]
- [Source: `src/Hexalith.Memories.Server/Actors/EmbeddingRateLimiterActor.cs` - actor state persistence]
- [Source: `src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs` - pure limiter logic]
- [Source: `src/Hexalith.Memories.Server/Actors/ITenantConfigurationActor.cs` - tenant config actor interface]
- [Source: `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` - tenant config storage/fallback behavior]
- [Source: `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` - cached config contract]
- [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` - current activity admission/config tests]
- [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityConfigTests.cs` - current config call sequence tests]
- [Source: `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateChunkEmbeddingsActivityTests.cs` - chunk batch activity tests]
- [Source: `tests/Hexalith.Memories.Server.Tests/Actors/EmbeddingRateLimiterActorTests.cs` - actor persistence tests]
- [Source: `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs` - limiter logic tests]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-05: Loaded repository `AGENTS.md` instructions from the user prompt and `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`.
- 2026-07-05: Used `.agents/skills/bmad-create-story/SKILL.md`; loaded `discover-inputs.md`, `template.md`, and `checklist.md`.
- 2026-07-05: Resolved workflow customization with `_bmad/scripts/resolve_customization.py`; activation prepend/append steps were empty, persistent facts were `file:{project-root}/**/project-context.md`, and `workflow.on_complete` was empty.
- 2026-07-05: Loaded BMM config: user `Jerome`, project `memories`, planning artifacts `_bmad-output/planning-artifacts`, implementation artifacts `_bmad-output/implementation-artifacts`, English communication/output.
- 2026-07-05: Target story supplied by user as `23.5`; selected story key `23-5-rate-limiter-admission-simplification`.
- 2026-07-05: Confirmed sprint status before creation: `epic-23: in-progress`; `23-1`, `23-2`, `23-3`, `23-4`, and `23-9` done; `23-5` backlog.
- 2026-07-05: Loaded project context plus EventStore/Tenants persistent facts, Epic 23 source, A15 audit finding, sprint-change proposal, architecture actor/workflow rules, previous Epic 23 story files, current admission source files, current admission/config tests, and recent git commits.
- 2026-07-05: Discovery results: no sharded planning directories were present; loaded the relevant Epic 23/A15 sections from `_bmad-output/planning-artifacts/epics.md`, `architecture.md`, `sprint-change-proposal-2026-07-04.md`, and `research/architecture-audit-2026-07-04.md`, plus project-context facts and prior story files.
- 2026-07-05: Validation pass applied checklist concerns: included `GenerateChunkEmbeddingsActivity` despite A15 naming `GenerateEmbeddingActivity`, preserved Story 23.3 provider 429 behavior, required tenant-safe config caching, required concurrency proof, and bounded scope away from Stories 23.6-23.8.
- 2026-07-05: Used `.agents/skills/bmad-dev-story/SKILL.md`; loaded `checklist.md`, resolved workflow customization, loaded `_bmad-output/project-context.md`, selected story `23-5-rate-limiter-admission-simplification`, and marked story/sprint status `in-progress`.
- 2026-07-05: Reconfirmed pre-change flow: `GenerateEmbeddingActivity` used tenant config -> marker check -> credential prime -> `SetCeilingAsync` -> parameterless `TryConsumeAsync` -> provider call; `GenerateChunkEmbeddingsActivity` set the ceiling once and consumed once per provider batch.
- 2026-07-05: Implemented actor-owned single admission through `TryConsumeWithCeilingAsync(int ceiling)`. Initial overloaded `TryConsumeAsync(int)` design built but failed full server tests because Dapr actor interfaces disallow overloaded method names; renamed to a distinct method while preserving parameterless `TryConsumeAsync` and `SetCeilingAsync` compatibility members.
- 2026-07-05: Added `RateLimiterLogic.TryConsume(state, ceiling)` and tests for lower-ceiling clamp, higher-ceiling preservation, expired-window reset to current ceiling, non-positive ceiling validation, and serialized same-tenant no-oversubscription with ceiling 1.
- 2026-07-05: Added `ITenantEmbeddingConfigProvider` / `TenantEmbeddingConfigProvider` with tenant-id-keyed per-process TTL cache, default 30 seconds, bounded to 1-300 seconds, backed by `TenantConfigurationActor`.
- 2026-07-05: Updated `GenerateEmbeddingActivity` and `GenerateChunkEmbeddingsActivity` to obtain config through the provider and call `TryConsumeWithCeilingAsync(config.RateLimitPerMinute)` once per provider call/batch, with marker checks, credential priming, telemetry, local denial behavior, and provider 429 reporting preserved.
- 2026-07-05: Updated old call-sequence tests, natural-language content-kind substitutes, and integration placeholder comment to the new admission contract.
- 2026-07-05: Updated `docs/operations/rate-limiting.md` because it described the old per-invocation config read and parameterless consume behavior.
- 2026-07-05: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter ...` aborted with the known VSTest `SocketException (13): Permission denied` TCP-listener sandbox issue.
- 2026-07-05: Focused fallback passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityConfigTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateChunkEmbeddingsActivityTests -class Hexalith.Memories.Server.Tests.Actors.EmbeddingRateLimiterActorTests -class Hexalith.Memories.Server.Tests.Actors.RateLimiterLogicTests -class Hexalith.Memories.Server.Tests.Ingestion.TenantEmbeddingConfigProviderTests -class Hexalith.Memories.Server.Tests.NaturalLanguage.EmbeddingInputContentKindTests -parallel none -noLogo` -> 73 total, 0 failed, 0 skipped.
- 2026-07-05: Full server fallback passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo` -> 2377 total, 0 failed, 1 pre-existing skip.
- 2026-07-05: Final validation passed: `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`; `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`; `git diff --check`.
- 2026-07-05: Used `.agents/skills/bmad-story-automator-review/SKILL.md`; loaded `workflow.yaml`, `instructions.xml`, and `checklist.md`; no MCP resources were configured for doc lookup, so review used local story, project context, source, docs, and tests.
- 2026-07-05: Senior developer review found and fixed two issues: first-use combined actor admission persisted a default state before the consumed state, and stale test comments still implied rate-limit config updates always apply on the next ingest instead of within the cache freshness bound.
- 2026-07-05: Review validation passed after fixes: `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`; `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`; focused xUnit fallback -> 75 total, 0 failed, 0 skipped; full server xUnit fallback -> 2379 total, 0 failed, 1 pre-existing skip; `git diff --check`.

### Completion Notes List

- Created comprehensive ready-for-dev story context for A15 rate-limiter admission simplification.
- Story directs the developer toward a single actor admission method as the lowest-risk path, while preserving the approved Redis Lua alternative only with explicit tenant-scoped atomicity tests.
- Story highlights that both `GenerateEmbeddingActivity` and `GenerateChunkEmbeddingsActivity` must be updated because chunked batch embedding is now the primary raw document path.
- Story requires tenant embedding config caching with bounded freshness/invalidation and tests replacing stale old call-sequence assertions.
- Implemented the Dapr actor single-method path using `TryConsumeWithCeilingAsync(int ceiling)` instead of Redis Lua. The distinct method name is required because Dapr actor interfaces cannot overload method names; it still collapses the former `SetCeilingAsync` + parameterless `TryConsumeAsync` production admission path into one actor RPC.
- Kept compatibility members `SetCeilingAsync` and parameterless `TryConsumeAsync` for non-admission callers/tests, but embedding activities no longer use them for provider admission.
- Added a tenant-id-keyed config provider cache with a documented default 30-second TTL and 1-300 second bound. The cache stores only the `TenantEmbeddingConfig` contract, not secret values, and expired entries allow invalid/corrupted fallback repairs and rate-limit updates to be observed within the bound.
- Updated single-text, natural-language, and chunked batch embedding paths to call one admission operation per provider call/batch while preserving migration marker checks, provider credential priming, local denial behavior, telemetry tags, and provider 429 reporting.
- Added actor/logic tests proving atomic ceiling+consume behavior and serialized no-oversubscription with ceiling 1. Added config cache hit/expiry and cross-tenant isolation tests.
- Review fix: first-use mutating actor operations now load a default state without persisting it, then persist only the final computed state. Added coverage proving `TryConsumeWithCeilingAsync` persists one consumed state update when no prior tenant state exists.
- Review fix: updated stale test comments so the cache freshness contract is documented as "within the configured bound" rather than "next ingest."

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-05

Outcome: Approved after automatic fixes. Story 23.5 is complete and status was moved to done.

Findings fixed:

- [HIGH] `TryConsumeWithCeilingAsync` did not satisfy the "one state update per admission operation" proof for a first-ever tenant. `GetOrCreateStateAsync` persisted a default `RateLimitState` before the combined admission persisted the consumed state, producing two state writes on the first admission. Fixed by changing mutating actor operations to load existing state or use an unpersisted default, then persist only the final computed state. Regression coverage added in `EmbeddingRateLimiterActorTests.TryConsumeWithCeilingAsync_WhenNoStateExists_ShouldPersistConsumedStateOnce`.
- [MEDIUM] The integration placeholder comment for FR69 still claimed a rate-limit update applies on the next ingest, which no longer matches the Story 23.5 TTL-cache contract. Fixed to state that updates are observed after the configured embedding-config cache freshness bound.
- [LOW] An activity-test comment still described the old "ceiling is pulled per invocation" framing. Fixed to describe the new single-admission operation contract.

Checklist summary:

- Acceptance Criteria 1-10 rechecked against source and tests.
- Story File List matched the relevant 23.5 implementation files; unrelated dirty worktree changes from other stories were not reviewed as part of this story.
- No remaining critical issues after fixes.
- Sprint status synced to done.

### File List

- `_bmad-output/implementation-artifacts/23-5-rate-limiter-admission-simplification.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/operations/rate-limiting.md`
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs`
- `src/Hexalith.Memories.Server/Actors/EmbeddingRateLimiterActor.cs`
- `src/Hexalith.Memories.Server/Actors/IEmbeddingRateLimiterActor.cs`
- `src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs`
- `src/Hexalith.Memories.Server/Ingestion/ITenantEmbeddingConfigProvider.cs`
- `src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigCacheOptions.cs`
- `src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantConfigurationIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateChunkEmbeddingsActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityConfigTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/EmbeddingRateLimiterActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/TenantEmbeddingConfigProviderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs`

### Change Log

- 2026-07-05: Implemented Story 23.5 A15 remediation: single actor admission method, bounded tenant config cache, updated embedding activities, tests, and operations documentation. Status moved to review.
- 2026-07-05: Senior developer review completed with automatic fixes for first-use actor persistence count and stale cache-freshness comments. Status moved to done.
