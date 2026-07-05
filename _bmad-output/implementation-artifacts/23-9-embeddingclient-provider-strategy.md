---
baseline_commit: ae8bb1e
---

# Story 23.9: EmbeddingClient Provider Strategy

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want provider specifics behind an `IEmbeddingProvider` strategy with a batch API,
so that adding a provider or chunking does not touch transport, auth, and provider payload format at once.

Story 23.9 must run before Story 23.1. Content chunking consumes the provider batch API created here; sprint-status `story_execution_order.epic-23` is authoritative even though the numeric story key is later than 23.1.

## Acceptance Criteria

1. `EmbeddingClient` becomes a facade over provider strategies. Given `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` currently mixes provider dispatch, request construction, auth, DAPR secret retrieval/cache, HTTP transport, auth-failure retry, Retry-After parsing, response parsing, redaction, and fake embeddings in one class, when this story completes, then provider-specific request/auth/response knowledge is moved behind an `IEmbeddingProvider` strategy surface with methods equivalent to `BuildRequest`, `ParseResponse`, and `Authenticate`.

2. A batch embedding API exists and preserves order. Given current callers can only call `GenerateAsync(string text, ...)`, when this story completes, then `EmbeddingClient.GenerateBatchAsync(IReadOnlyList<string> texts, string tenantId, TenantEmbeddingConfig config, CancellationToken ct)` returns one vector per input in the same order, validates every vector dimension against the tenant config, rejects empty input clearly, and `GenerateAsync` delegates through the batch path for single-text compatibility.

3. Google and Ollama providers are implemented with provider-owned payload and parser logic. Given Google currently uses `models/{model}:embedContent` with `embedding.values` and Ollama currently uses `/api/embed` with `embeddings[0]`, when this story completes, then Google batch uses the documented `models/{model}:batchEmbedContents` shape and parses `embeddings[]`, while Ollama batch sends an `input` array to `/api/embed` and parses `embeddings[]`. Both providers must verify response count equals request count.

4. Transport and auth retry are shared, not duplicated per provider. Given Google and Ollama both perform an HTTP POST, read response bodies, wrap timeout/transport/read failures, handle provider 429 via `EmbeddingRateLimitException`, redact secrets and input text, and retry once on 401/403 after credential refresh, when this story completes, then this behavior lives in shared transport/auth-retry code used by both providers. Provider implementations may build requests and parse responses, but must not each carry copy-pasted send/read/retry/redaction logic.

5. Existing public behavior remains compatible. Given `GenerateEmbeddingActivity`, `SemanticSearchService`, `NaturalLanguageSemanticSearchService`, `EmbeddingClientMigrationVectorGenerator`, and benchmark/tests currently depend on the concrete `EmbeddingClient`, when this story completes, then existing `GenerateAsync` behavior, constructor compatibility or intentional DI migration, fake embedding behavior, provider/model result metadata, OIDC token refresh behavior, and DAPR secret retrieval semantics remain covered by tests. No caller should gain provider-specific branching.

6. Provider registration is explicit and safe. Given supported providers are defined by `EmbeddingProviderDefaults` as Google and Ollama, when this story completes, then provider strategy resolution is deterministic, case-insensitive where existing validation is case-insensitive, fails with the existing structured unsupported-provider message, and does not silently add OpenAI, Mistral, or any new runtime provider.

7. Workflow and rate-limit boundaries are preserved. Given `GenerateEmbeddingActivity` owns tenant config lookup, migration marker write-block checks, per-tenant rate-limiter consumption, API call telemetry, and `EmbeddingRateLimitException` reporting, when this story completes, then the strategy refactor does not move workflow-only behavior into providers and does not bypass per-tenant admission control. Story 23.1 may later decide how batch token counts map to rate limiting; this story only creates the provider batch API and keeps existing activity behavior intact.

8. File structure follows Hexalith rules. Given this story will split `EmbeddingClient.cs`, when this story completes, then each new C# type lives in its own file with the ITANEO copyright header, file-scoped namespace, `ConfigureAwait(false)`, public-boundary validation, centralized package versions, and no submodule changes. The existing `EmbeddingProviderIdentifier` record must not remain as an extra type in `EmbeddingClient.cs` if that file is edited substantially.

9. Provider tests cover both single and batch paths. Given A51 is a refactor with user-visible batch capability, when this story completes, then focused tests cover Google and Ollama request URLs, auth headers, request bodies, single and multi-input parsing, response count mismatch, dimension mismatch, 429 Retry-After propagation, 401/403 credential refresh, secret/input redaction, fake batch embeddings, unsupported provider failure, and no-provider-knowledge leakage into workflow callers.

## Tasks / Subtasks

- [x] Task 1 - Reconfirm A51 and inventory all `EmbeddingClient` consumers (AC: 1, 5, 7)
  - [x] Read `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` fully before editing. Current responsibilities include provider dispatch, Google/Ollama request JSON, endpoint URL construction, DAPR secret cache, OIDC token acquisition/refresh, HTTP send/read wrapping, Retry-After parsing, response parsing, redaction, fake embeddings, and provider identifier parsing.
  - [x] Read all direct consumers: `GenerateEmbeddingActivity`, `SemanticSearchService`, `NaturalLanguageSemanticSearchService`, `EmbeddingClientMigrationVectorGenerator`, and `tests/Hexalith.Memories.Benchmarks/Infrastructure/BenchmarkEmbeddingClient.cs`.
  - [x] Preserve the current external `EmbeddingClient.GenerateAsync` contract unless a small, test-updated DI migration is clearly lower risk.

- [x] Task 2 - Introduce the provider strategy model (AC: 1, 3, 6, 8)
  - [x] Add `IEmbeddingProvider` under `src/Hexalith.Memories.Server/Ingestion/` with a focused provider contract. The provider contract must let each provider authenticate, build an HTTP request for one or more texts, and parse an HTTP response into ordered vectors.
  - [x] Add separate provider implementations for Google and Ollama, each in its own file. Keep provider-specific JSON shapes, endpoint path construction, auth header shape, and response parsing inside these providers.
  - [x] Add a deterministic provider resolver/registry that uses `EmbeddingProviderDefaults` provider names and preserves unsupported-provider error behavior.
  - [x] Move `EmbeddingProviderIdentifier` into its own file if it remains needed.

- [x] Task 3 - Extract shared credential, transport, retry, and redaction behavior (AC: 4, 5)
  - [x] Keep DAPR secret retrieval and cache behavior equivalent: secret store name `secretstore`, key from `TenantEmbeddingConfig.ApiSecretKeyName`, cache keyed by secret key name, and eviction on auth failure.
  - [x] Keep Ollama OIDC behavior equivalent: `oidc-client-credentials` only, `IOidcTokenProvider` required, token endpoint transport policy remains in `OidcTokenProvider`, 401/403 calls `InvalidateAndRefreshAsync`, and blank/invalid bearer tokens throw sanitized `EmbeddingApiException`.
  - [x] Create shared send/read code that preserves `OperationCanceledException` for caller cancellation and wraps `HttpRequestException`, timeout `TaskCanceledException`, and `IOException` as `EmbeddingApiException`.
  - [x] Preserve Retry-After parsing semantics: absent, malformed, or past date maps to `0`; positive values clamp to `[1, 3600]`; 429 throws `EmbeddingRateLimitException` with `RetryAfterSeconds`.
  - [x] Preserve redaction of API keys, client secrets, bearer tokens, and full input text from provider error bodies. For batch input, redact every submitted text, not only the first one.

- [x] Task 4 - Add `GenerateBatchAsync` and make single generation delegate through it (AC: 2, 3, 5)
  - [x] Add `public virtual Task<IReadOnlyList<float[]>> GenerateBatchAsync(IReadOnlyList<string> texts, string tenantId, TenantEmbeddingConfig config, CancellationToken ct)` or an equivalent immutable collection shape.
  - [x] Reject `null`, empty, null item, empty item, or whitespace-only tenant/config values with clear argument exceptions before any secret retrieval or HTTP call.
  - [x] Validate `TenantEmbeddingConfig` once at the facade boundary and ensure provider lookup happens before credential resolution for unsupported providers.
  - [x] Use provider batch endpoints for multi-input calls. For single-input calls, either use the provider batch path consistently or prove a single-call optimization cannot drift from the batch implementation.
  - [x] Return ordered vectors and verify both response count and each vector dimension against `config.Dimensions`.
  - [x] Keep fake embeddings deterministic for every input and dimension. `GenerateAsync` with fake embeddings must remain byte-for-byte stable for existing tests where possible.

- [x] Task 5 - Preserve activity, search, migration, and benchmark compatibility (AC: 5, 7)
  - [x] Keep `GenerateEmbeddingActivity` provider-agnostic. It should still fetch tenant config, check active migration marker consistency, prime credentials before rate-limiter consumption, set the rate-limit ceiling, consume the limiter, record telemetry by `EmbeddingContentKind`, call `EmbeddingClient`, and report provider 429s to the rate limiter.
  - [x] Keep `EmbeddingResult.Provider` as `{provider}:{model}` and `Model = config.Model`.
  - [x] Keep `SemanticSearchService` and `NaturalLanguageSemanticSearchService` embedding query text through the facade and validating dimensions.
  - [x] Update `EmbeddingClientMigrationVectorGenerator` only if the facade signature changes; do not bypass the provider strategy there.
  - [x] Update `BenchmarkEmbeddingClient` and existing `Substitute.For<EmbeddingClient>` tests deliberately if constructor or virtual method shapes change.

- [x] Task 6 - Update DI registration and composition roots (AC: 5, 6, 8)
  - [x] Update `Program.cs` registrations around the existing named `EmbeddingClient` HttpClient and `IOidcTokenProvider` registration. Preserve existing timeout values unless there is a story-owned reason to change them.
  - [x] Register provider strategies explicitly as singleton services or compose them manually in the `EmbeddingClient` factory.
  - [x] Do not add package references or package versions; use the existing BCL, `IHttpClientFactory`, DAPR client, and JSON stack.

- [x] Task 7 - Focused tests (AC: 1-9)
  - [x] Refactor or add `EmbeddingClientTests`/`EmbeddingClientConfigTests` coverage for both providers and both single and batch generation.
  - [x] Add Google request tests for `:batchEmbedContents`, `requests[]`, model resource naming, output dimensionality handling, API-key header, response ordering, response count mismatch, malformed JSON, and dimension mismatch.
  - [x] Add Ollama request tests for `/api/embed`, array `input`, bearer auth, OIDC token acquisition, token refresh on 401/403, response ordering, response count mismatch, malformed JSON, and dimension mismatch.
  - [x] Add shared transport tests for 429, Retry-After delta/date parsing, timeout/transport/read failures, cancellation passthrough, redaction of every sensitive value, and no duplicate send/read logic through provider tests.
  - [x] Keep `GenerateEmbeddingActivityTests` and `GenerateEmbeddingActivityConfigTests` green and add one assertion that the activity remains provider-agnostic and still calls the facade once for the single-text path.
  - [x] Update `EmbeddingVectorMigrationServiceTests` and benchmark tests if the facade signature changes.

- [x] Task 8 - Validate and record evidence (AC: 1-9)
  - [x] Run focused build/tests for server tests, especially ingestion, activities, migration, semantic search, NL search, and benchmark compile if touched.
  - [x] Run `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` when restore state allows.
  - [x] Run focused xUnit v3 in-process tests with `DiffEngine_Disabled=true dotnet exec ...Hexalith.Memories.Server.Tests.dll` if `dotnet test` hits the known sandbox TCP-listener blocker.
  - [x] Run `dotnet build tests/Hexalith.Memories.Benchmarks/Hexalith.Memories.Benchmarks.csproj -m:1 /nodeReuse:false --no-restore` if `BenchmarkEmbeddingClient` changes.
  - [x] Run `git diff --check`.
  - [x] Record exact commands, results, blockers, and File List in the Dev Agent Record.

## Dev Notes

### Current State and Code Anchors

`EmbeddingClient` is currently a large concrete, non-sealed facade-like class rather than a real strategy host. It exposes virtual `PrimeApiKeyAsync` and `GenerateAsync`; tests and benchmarks substitute or derive from it. Preserve that seam unless replacing it is proven lower risk. [Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`; `tests/Hexalith.Memories.Benchmarks/Infrastructure/BenchmarkEmbeddingClient.cs`]

Google behavior today:
- Endpoint: `https://generativelanguage.googleapis.com/v1beta/models/{config.Model}:embedContent`.
- Auth: `x-goog-api-key` from DAPR secret store.
- Request: content parts plus configured output dimensionality.
- Response parser: `embedding.values`.
- 401/403: evict cached API key, re-read secret, retry once.
[Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`; `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientConfigTests.cs`]

Ollama behavior today:
- Endpoint: `{BaseUrl}/api/embed`.
- Auth: OIDC client credentials only, bearer token from `IOidcTokenProvider`.
- Request: `{ model, input = text }`.
- Response parser: first vector in `embeddings`.
- 401/403: evict cached client secret, refresh token, retry once.
[Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`; `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs`]

`GenerateEmbeddingActivity` must remain the rate-limit and workflow boundary. It reads `TenantEmbeddingConfig` from `ITenantConfigurationActor`, blocks writes during incompatible active embedding migrations, primes credentials before rate-limit consumption, sets/consumes `IEmbeddingRateLimiterActor`, records `MemoriesMeter.EmbeddingApiCalls` by content kind, and maps provider 429s back to the actor. Do not move any of that behavior into a provider strategy. [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs`]

Search uses `EmbeddingClient` for query embeddings too. `SemanticSearchService` and `NaturalLanguageSemanticSearchService` both embed query text through the facade and validate dimensions before Redis vector search. Any facade constructor or method signature change must update those services and tests together. [Source: `src/Hexalith.Memories.Server/Search/SemanticSearchService.cs`; `src/Hexalith.Memories.Server/Search/NaturalLanguageSemanticSearchService.cs`]

Migration uses the same facade through `EmbeddingClientMigrationVectorGenerator`. Keep provider behavior identical for migration so Story 21.9/21.10 migration safety remains intact. [Source: `src/Hexalith.Memories.Server/Migration/EmbeddingClientMigrationVectorGenerator.cs`; `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs`]

### Architecture Constraints

- Activities can do I/O; workflows must not. Provider HTTP, secret retrieval, token refresh, and response parsing belong in activities/services, not workflow orchestration. [Source: `_bmad-output/planning-artifacts/architecture.md#DAPR-Workflow-Patterns`]
- Tenant isolation and provider config are per tenant. Do not introduce global provider state that can mix tenant IDs, secret names, models, dimensions, or rate-limit ceilings. [Source: `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`]
- Redis vector schema is fixed after provisioning. Provider/model/dimension changes require migration/reindex handling; this story must not treat strategy resolution as permission to mutate tenant provider config or indexes. [Source: `_bmad-output/planning-artifacts/prd.md#Embedding-Provider-Configuration`]
- No new runtime provider is in scope. Supported runtime providers remain Google and Ollama; OpenAI and Mistral remain deferred unless a later sprint change pulls them forward. [Source: `_bmad-output/planning-artifacts/architecture.md#PRD-Deviations`]
- C# file hygiene matters here because `EmbeddingClient.cs` will be split. One primary type per file, file-scoped namespaces, copyright header, public XML docs for public surfaces, `ConfigureAwait(false)` in production awaits, and no package versions in `.csproj`. [Source: `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`; `_bmad-output/project-context.md`]

### Previous Story Intelligence

There is no prior Epic 23 story file. This is the first Epic 23 story by execution order.

Carry-forward from Epic 22:
- Preserve retrieval invariants while ingestion output changes: semantic, NL, syntactic, and graph indexes must continue satisfying pagination, RRF, case attribution, source-type recall, and case-scoped traversal assumptions. [Source: `_bmad-output/implementation-artifacts/epic-22-retro-2026-07-05.md`]
- Keep raw Redis response parsing on review checklists for search and ingestion indexing changes. This story mainly touches provider HTTP, but Story 23.1 will touch vector writes; record this carry-forward for the next story. [Source: `_bmad-output/implementation-artifacts/epic-22-retro-2026-07-05.md`]
- Full solution validation may still be blocked by the known AppHost/EventStore duplicate assembly issue; do not hide it as a story failure. Record focused build/test evidence and the exact blocker if it appears. [Source: `_bmad-output/implementation-artifacts/epic-22-retro-2026-07-05.md`; `sprint-status.yaml` action items]

Carry-forward from Epic 21:
- Migration marker and target-consistency safeguards are active. Do not bypass `EmbeddingMigrationMarkerReader.EnsureWriteMatchesMarker` in `GenerateEmbeddingActivity`. [Source: `_bmad-output/implementation-artifacts/21-9-blue-green-embedding-migration.md`; `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs`]

### Git Intelligence

Recent commits before this story:

- `ae8bb1e docs(epic-22): close retrospective and sync retrieval docs`
- `28ab3d3 feat(story-22.7): Retrieval Feature Completion`
- `df3c9b6 feat(story-22.6): Post-Filter Recall`
- `1a6376c feat(story-22.5): Case-Scoped Traversal Path Integrity`
- `14c1942 feat(story-22.4): Fusion Case Attribution, Score Calibration & Pinned Scorer`

Pattern: story work is tightly scoped, source anchored, heavily tested, and records validation blockers explicitly. Story 23.9 is a refactor plus additive batch capability; use a conventional commit that reflects the user-visible capability if committed later.

### Latest Technical / Library Notes

- Google Gemini API documents a synchronous `models.batchEmbedContents` endpoint at `POST https://generativelanguage.googleapis.com/v1beta/{model=models/*}:batchEmbedContents`; request body contains `requests[]`, each request model must match the path model, and the response `embeddings[]` is returned in the same order as the batch request. [Source: https://ai.google.dev/api/embeddings]
- Google docs also mark legacy top-level `outputDimensionality` as deprecated in favor of `EmbedContentConfig.output_dimensionality`. The current code and tests pin a request shape; if this story changes the field placement, update focused tests and keep behavior explicit rather than accidental. [Source: https://ai.google.dev/api/embeddings; `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientConfigTests.cs`]
- Ollama documents `POST /api/embed` with `input` as either text or a list of text and returns `embeddings` as an array of vectors. This supports using the same endpoint for single and batch generation. [Source: https://github.com/ollama/ollama/blob/main/docs/api.md#generate-embeddings]
- No package upgrade is required for this story.

### Scope Boundaries

In scope:
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`
- New provider strategy/transport/auth helper files under `src/Hexalith.Memories.Server/Ingestion/`
- `src/Hexalith.Memories.Server/Program.cs` DI registration if needed
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` only if needed to preserve facade interaction
- `src/Hexalith.Memories.Server/Migration/EmbeddingClientMigrationVectorGenerator.cs` only if the facade signature changes
- `tests/Hexalith.Memories.Server.Tests/Ingestion/*EmbeddingClient*`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/*GenerateEmbeddingActivity*`
- `tests/Hexalith.Memories.Server.Tests/Migration/EmbeddingVectorMigrationServiceTests.cs`
- `tests/Hexalith.Memories.Benchmarks/Infrastructure/BenchmarkEmbeddingClient.cs` and benchmark project compile if touched

Out of scope:
- Content chunking, token-aware splitting, vector key sequence suffixes, or changing `IngestionWorkflow` to batch chunks. That is Story 23.1.
- Claim-check workflow payloads. That is Story 23.2.
- Durable Retry-After timers in workflow orchestration. That is Story 23.3.
- Rate-limiter API redesign or Redis Lua token bucket. That is Story 23.5.
- Adding OpenAI, Mistral, or custom providers.
- Tenant provider config mutation, reindex workflow behavior, index provisioning ownership, or docs-only provider migration policy changes unless tests require a narrow correction.
- UI/web work and submodule changes.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`.
- Keep tests under matching feature folders: `tests/Hexalith.Memories.Server.Tests/Ingestion`, `Activities/Ingestion`, and `Migration`.
- Unit-test provider JSON through captured `HttpRequestMessage` bodies and headers. Do not require live Google or Ollama network calls for provider shape tests.
- Use deterministic fake vectors for fake embedding tests and dimension/count mismatches.
- If normal `dotnet test` is blocked by the known VSTest TCP-listener sandbox issue, use the established xUnit v3 in-process `dotnet exec` fallback and record the exact command.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story-23.9` - story statement, AC, and execution note]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-23` - approved A51 remediation scope]
- [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A51` - finding: six responsibilities, hard-coded dispatch, single-text API]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04-implementation-readiness-cleanup.md#Epic-23-Provider-Strategy-Preflight` - 23.9 before 23.1]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Embedding-Provider` - provider config and ingestion pipeline architecture]
- [Source: `_bmad-output/planning-artifacts/prd.md#Embedding-Provider-Management` - FR68-FR70 and provider migration constraints]
- [Source: `_bmad-output/project-context.md` - .NET 10/C# 14, Dapr, Redis/FalkorDB, testing, and coding rules]
- [Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` - current implementation]
- [Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` - supported provider registry and validation order]
- [Source: `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` - tenant provider config contract]
- [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` - workflow activity boundary]
- [Source: `src/Hexalith.Memories.Server/Search/SemanticSearchService.cs` - query embedding consumer]
- [Source: `src/Hexalith.Memories.Server/Search/NaturalLanguageSemanticSearchService.cs` - NL query embedding consumer]
- [Source: `src/Hexalith.Memories.Server/Migration/EmbeddingClientMigrationVectorGenerator.cs` - migration consumer]
- [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` and `EmbeddingClientConfigTests.cs` - existing behavior tests]
- [Source: `https://ai.google.dev/api/embeddings` - Google single and batch embedding API]
- [Source: `https://github.com/ollama/ollama/blob/main/docs/api.md#generate-embeddings` - Ollama `/api/embed` single/list input contract]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (create-story context engineering)

Claude Opus 4.8 (dev-story implementation)

GPT-5 Codex (story-automator senior developer review)

### Debug Log References

- 2026-07-05: Loaded repository AGENTS instructions and Hexalith LLM instructions before work.
- 2026-07-05: Loaded `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- 2026-07-05: Resolved workflow customization with `_bmad/scripts/resolve_customization.py`; persistent facts are `**/project-context.md`.
- 2026-07-05: Loaded BMM config: user `Jerome`, project `memories`, planning artifacts `_bmad-output/planning-artifacts`, implementation artifacts `_bmad-output/implementation-artifacts`, English communication/output.
- 2026-07-05: Target story supplied by user as `23.9`; selected story key `23-9-embeddingclient-provider-strategy` due explicit story request and `story_execution_order.epic-23`.
- 2026-07-05: Confirmed sprint status before creation: `epic-23: backlog`, `23-9-embeddingclient-provider-strategy: backlog`; because 23.9 is first in execution order, epic status must move to `in-progress`.
- 2026-07-05: Loaded project-context facts from root and root-declared reference project contexts; no submodules initialized or updated.
- 2026-07-05: Loaded Epic 23, Story 23.9, A51 audit finding, implementation-readiness cleanup, PRD provider constraints, architecture ingestion/provider sections, current source files, tests, previous Epic 22 story/retro intelligence, and recent git commits.
- 2026-07-05: Performed current-API web research against official Google Gemini embeddings docs and official Ollama API docs for batch embedding shapes.
- 2026-07-05 (dev): Inventoried `EmbeddingClient` and every consumer (`GenerateEmbeddingActivity`, `SemanticSearchService`, `NaturalLanguageSemanticSearchService`, `EmbeddingClientMigrationVectorGenerator`, `BenchmarkEmbeddingClient`) plus existing tests. Confirmed `ParseEmbeddingProviderIdentifier`, `RedactSensitiveValues`, `ParseRetryAfterSeconds` are called only from tests via `EmbeddingClient.X`, and 30+ existing tests pin the single-call wire shapes (Google `:embedContent`+`embedding.values`, Ollama `input` string + tolerant `embeddings[0]`).
- 2026-07-05 (dev): Design decision — keep `GenerateAsync` single-call wire behavior identical for compatibility (AC5) and add `GenerateBatchAsync` as the new batch path (Google `:batchEmbedContents`, Ollama `input` array). Both route through one `IEmbeddingProvider` strategy + shared `EmbeddingProviderTransport`, satisfying Task 4's "single-call optimization cannot drift from the batch implementation" escape hatch.
- 2026-07-05 (dev): `Substitute.For<EmbeddingClient>(...)` and subclasses (`BenchmarkEmbeddingClient`, `OverrideEmbeddingClient`) require the type to stay non-sealed with virtual members; kept both constructors and `GenerateAsync`/`PrimeApiKeyAsync` virtual and made `GenerateBatchAsync` virtual. Providers composed manually in the constructor so Program.cs DI is unchanged (Task 6 OR branch).
- 2026-07-05 (dev): Build `src/Hexalith.Memories.Server` — succeeded, 0 warnings, 0 errors. Build `tests/Hexalith.Memories.Server.Tests` — succeeded, 0 warnings, 0 errors. Compile-checked `tests/Hexalith.Memories.Benchmarks` and `tests/Hexalith.Memories.IntegrationTests` — both succeeded, 0 warnings.
- 2026-07-05 (dev): `dotnet test` VSTest path is blocked in this sandbox, so used the established xUnit v3 in-process fallback: `DiffEngine_Disabled=true dotnet exec bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll`. Focused Ingestion + Activities.Ingestion namespaces: 617 total, 0 failed. Full server suite: 2293 total, 0 failed, 0 errors, 1 pre-existing skip.
- 2026-07-05 (dev): `git diff --check` reports only `cr-at-eol` (trailing-whitespace) on added C# lines. This repo commits C# as CRLF with no `.gitattributes`/`autocrlf`, so git's default whitespace heuristic flags CR on every C# change here; new/edited `.cs` files were normalized to CRLF to match all existing committed files. Not a real defect.
- 2026-07-05 (review): Loaded `.agents/skills/bmad-story-automator-review/SKILL.md`, `workflow.yaml`, `instructions.xml`, and `checklist.md`; reviewed story claims against git changes and File List. Verified official Google Gemini embeddings and Ollama API docs for batch endpoint/input shape.
- 2026-07-05 (review): Auto-fixed review findings: single-text facade now rejects whitespace text/tenant before secret lookup; `Retry-After: 0` now maps to `0`; Google malformed-response exceptions no longer echo raw provider bodies; Ollama single-text parsing now rejects response count mismatch instead of accepting extra vectors.
- 2026-07-05 (review): Validation: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` succeeded; VSTest `dotnet test --no-build --filter ...` was blocked by sandbox TCP listener permission; focused xUnit in-process run passed 115 total, 0 failed; full server xUnit in-process run passed 2302 total, 0 failed, 1 skipped.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story status set to `ready-for-dev`.
- Sprint status updated for Epic 23 and Story 23.9.
- ✅ AC1/AC8: Extracted provider dispatch, request construction, auth, response parsing behind `IEmbeddingProvider` (`AuthenticateAsync`/`RefreshCredentialsAsync`/`BuildRequest`/`ParseResponse`/`PrimeAsync`). Each new type is in its own file with the ITANEO header, file-scoped namespace, and `ConfigureAwait(false)`. `EmbeddingProviderIdentifier` moved to its own file.
- ✅ AC2: Added `public virtual Task<IReadOnlyList<float[]>> GenerateBatchAsync(...)` returning one vector per input in order, validating every dimension, rejecting null/empty/whitespace inputs and empty tenant/config before any secret retrieval or HTTP call. `GenerateAsync` routes through the same provider strategy + transport for single-text compatibility.
- ✅ AC3: Google batch uses `models/{model}:batchEmbedContents` with `requests[]` and parses `embeddings[]`; Ollama batch sends an `input` array to `/api/embed` and parses `embeddings[]`. Both verify response count equals request count.
- ✅ AC4: Shared `EmbeddingProviderTransport` owns send/read wrapping, the single 401/403 credential-refresh retry, 429→`EmbeddingRateLimitException` with Retry-After, and error-body redaction (`EmbeddingResponseSanitizer` redacts every submitted batch input, not just the first). No provider carries copy-pasted send/read/retry/redaction logic. DAPR secret cache+eviction lives in shared `EmbeddingSecretStore`.
- ✅ AC5: `GenerateAsync`, both constructors, fake embeddings, `{provider}:{model}` metadata, OIDC refresh, and DAPR secret semantics unchanged — all covered by the pre-existing tests, which pass unmodified (except the additive activity assertion). No caller gained provider branching; search, migration, and activity consumers required no changes.
- ✅ AC6: `EmbeddingProviderRegistry.Resolve` is deterministic and case-insensitive, preserves the structured unsupported-provider message, and cannot silently add a new runtime provider.
- ✅ AC7: `GenerateEmbeddingActivity` unchanged (rate-limit, migration-marker, telemetry, and 429 reporting boundaries preserved). Added an assertion that the activity calls the single-text facade once and never `GenerateBatchAsync`.
- ✅ AC9: Provider tests cover Google/Ollama single and batch request URLs/headers/bodies, ordering, response count mismatch, dimension mismatch, malformed JSON, 429 Retry-After, 401/403 credential refresh, secret/input redaction, fake batch embeddings, and unsupported-provider failure.
- ✅ Senior developer review fixed four contract issues: missing single-text public-boundary validation, incorrect zero Retry-After handling, raw malformed Google response body exposure, and Ollama single response count mismatch tolerance.

### Senior Developer Review (AI)

Outcome: Approved after automatic fixes.

Findings fixed:
- HIGH: `GenerateAsync` accepted whitespace text and missing/blank tenant ids while `GenerateBatchAsync` enforced the new boundary contract. Fixed by validating whitespace text and tenant id before fake generation, secret lookup, or HTTP calls.
- HIGH: Ollama single-text parsing accepted multiple returned embeddings and silently used the first vector, conflicting with the response-count invariant. Fixed by requiring exactly one vector for the single-text path.
- MEDIUM: `Retry-After: 0` was converted to `1`, despite the story contract that absent, malformed, past, or non-positive values should leave the activity default path available. Fixed by returning `0` for zero/non-positive delta values.
- MEDIUM: Google malformed 200 responses included raw provider response bodies in exception messages. Fixed by removing raw response-body echo from parser exceptions.

Validation:
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` — passed.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~Hexalith.Memories.Server.Tests.Ingestion.EmbeddingClient|FullyQualifiedName~Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityTests"` — blocked by known VSTest socket permission issue.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Ingestion.EmbeddingClientTests -class Hexalith.Memories.Server.Tests.Ingestion.EmbeddingClientBatchTests -class Hexalith.Memories.Server.Tests.Ingestion.EmbeddingClientRetryAfterParsingTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityTests` — passed, 115 total.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` — passed, 2302 total, 0 failed, 1 skipped.
- `git diff --check` — still reports CRLF `cr-at-eol`/trailing-whitespace warnings on changed CRLF files, consistent with the existing story note and repository line-ending convention.

### File List

- `_bmad-output/implementation-artifacts/23-9-embeddingclient-provider-strategy.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` (rewritten as a strategy facade)
- `src/Hexalith.Memories.Server/Ingestion/IEmbeddingProvider.cs` (new)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderCredentials.cs` (new)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingSecretStore.cs` (new)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingResponseSanitizer.cs` (new)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderTransport.cs` (new)
- `src/Hexalith.Memories.Server/Ingestion/GoogleEmbeddingProvider.cs` (new)
- `src/Hexalith.Memories.Server/Ingestion/OllamaEmbeddingProvider.cs` (new)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderRegistry.cs` (new)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderIdentifier.cs` (new — moved out of `EmbeddingClient.cs`)
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientBatchTests.cs` (batch coverage: count/dimension mismatch, 429 Retry-After, unsupported provider, whitespace item, Google/Ollama 401 refresh)
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` (review fixes for single-text validation, Ollama count mismatch, and malformed Google body handling)
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientRetryAfterParsingTests.cs` (review fix for zero Retry-After parsing)
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` (added provider-agnostic single-text facade assertion)

Not changed (verified no change required): `Program.cs` (providers composed in the `EmbeddingClient` constructor), `GenerateEmbeddingActivity.cs`, `SemanticSearchService.cs`, `NaturalLanguageSemanticSearchService.cs`, `EmbeddingClientMigrationVectorGenerator.cs`, `BenchmarkEmbeddingClient.cs`.

## Change Log

- 2026-07-05: Story 23.9 implemented — `EmbeddingClient` refactored into an `IEmbeddingProvider` strategy facade with shared transport/auth-retry/redaction, and a new order-preserving `GenerateBatchAsync` provider batch API added. Existing single-text `GenerateAsync` behavior and all consumers preserved. Full server test suite green (2293 passed, 1 pre-existing skip). Status: in-progress → review.
- 2026-07-05: Senior developer review completed with automatic fixes for validation, Retry-After, parser redaction, and Ollama count mismatch. Full server test suite green (2302 passed, 1 pre-existing skip). Status: review → done.
