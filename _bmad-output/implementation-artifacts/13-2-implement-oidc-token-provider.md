# Story 13.2: Implement OidcTokenProvider

Status: ready-for-dev

**Effort estimate:** ~1.0-1.25 working days. Breakdown:

- **0.10 day - Task 0:** Pre-implementation verification of current DI, HttpClient resilience, logging, test helper, and story-boundary assumptions.
- **0.35 day - Task 1:** Add the OIDC token provider contract, request model, typed exception, and cache/concurrency implementation.
- **0.15 day - Task 2:** Wire the provider in `Program.cs` with the correct HttpClient lifetime, timeout, and resilience behavior.
- **0.35 day - Task 3:** Add focused unit tests for cache miss/hit, refresh-before-expiry, forced invalidation, concurrency, non-2xx handling, malformed responses, cancellation, and log redaction.
- **0.15 day - Task 4:** Validate the focused server test slice and solution build without touching sibling Epic 13 surfaces.

**HARD prerequisite:** Story 13.1 must be `done` before dev work begins. Story 13.2 can be created now to maintain the ready buffer, but implementation must not start while `13-1-extend-embedding-provider-defaults-to-accept-ollama` is still `review`, because 13.2 relies on the `ollama` provider constants and the Epic 13 sequencing established in Story 13.1.

**SOFT prerequisite:** None. Story 13.2 is parallel-safe with Epic 12 stories because it is confined to Memories Server ingestion/OIDC support and tests.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

Add a singleton `IOidcTokenProvider` and `OidcTokenProvider` under `src/Hexalith.Memories.Server/Ingestion/` that performs OAuth2/OIDC `client_credentials` token grants for the future Ollama embedding gateway. It posts URL-encoded form data to the configured token endpoint, caches successful tokens per `(tokenEndpoint, clientId)` until `expires_in - 30 seconds`, prevents duplicate concurrent fetches per key, supports a forced `InvalidateAndRefreshAsync(...)` path for Story 13.3's 401 retry, and never logs or surfaces the `client_secret` or `access_token`.

This story does **not** change `TenantEmbeddingConfig`, does **not** integrate with `EmbeddingClient`, and does **not** add Ollama request/response parsing. Those are Stories 13.4 and 13.3 respectively.

## Story

As a **backend developer**,
I want a thread-safe in-process OIDC token provider that performs `client_credentials` grants against Keycloak, caches access tokens until 30 seconds before expiry, and supports forced invalidation after an upstream 401,
so that Story 13.3 can attach `Authorization: Bearer <jwt>` to Ollama embedding requests without flooding Keycloak, leaking credentials, or breaking on routine expiry.

## Acceptance Criteria

1. **AC1 - First fetch posts the correct token request.** `GetAccessTokenAsync(tokenEndpoint, clientId, clientSecret, scope, ct)` sends an HTTP `POST` to `tokenEndpoint` with `application/x-www-form-urlencoded` content containing `grant_type=client_credentials`, `client_id`, `client_secret`, and `scope` only when `scope` is non-empty.

2. **AC2 - Successful responses are parsed and cached.** The provider parses JSON fields `access_token`, `expires_in`, and `token_type`; requires `token_type` to be `Bearer` case-insensitively; returns `access_token`; and caches it under `(tokenEndpoint, clientId)` with `expiresAt = now + expires_in - 30 seconds`.

3. **AC3 - Cache hit avoids HTTP.** A second call for the same `(tokenEndpoint, clientId)` while `now < expiresAt` returns the cached token without issuing another HTTP request, even if the caller passes the same secret again.

4. **AC4 - Expired or near-expired entries refresh.** When `now >= expiresAt`, the provider evicts the entry, fetches a new token, caches the new value, and returns it. For `expires_in <= 30`, treat the token as immediately refreshable after the current call rather than caching it as long-lived.

5. **AC5 - Forced invalidation supports Story 13.3.** `InvalidateAndRefreshAsync(tokenEndpoint, clientId, clientSecret, scope, ct)` forcibly removes the cached entry for `(tokenEndpoint, clientId)`, performs exactly one token fetch under the per-key guard, caches the returned token, and returns it.

6. **AC6 - Concurrent cache misses collapse to one HTTP request.** Two or more concurrent callers for the same `(tokenEndpoint, clientId)` receive the same token and trigger exactly one outbound HTTP request. Concurrent callers for different keys must not block each other.

7. **AC7 - Non-2xx token endpoint responses throw a typed exception.** A non-success response throws `OidcTokenAcquisitionException` carrying `StatusCode`, a response-body preview truncated to at most 1024 characters, `TokenEndpoint`, `ClientId`, and a generated correlation ID. The cache is not populated on failure.

8. **AC8 - Malformed success responses throw without caching.** Missing/blank `access_token`, missing/non-positive `expires_in`, unsupported `token_type`, invalid JSON, and empty endpoint/client ID/client secret inputs fail with clear exceptions and do not populate the cache.

9. **AC9 - DI registration is singleton and resilient.** `Program.cs` registers `IOidcTokenProvider` / `OidcTokenProvider` as singleton and configures a typed `HttpClient` with timeout <= 10 seconds. The client must use the repository's standard `Microsoft.Extensions.Http.Resilience` pipeline and must not stack duplicate resilience handlers.

10. **AC10 - Secrets and tokens never appear in logs or exception messages.** Unit tests prove that neither `client_secret` nor `access_token` appears in captured log output, `OidcTokenAcquisitionException.Message`, or response previews.

11. **AC11 - Focused tests cover the full behavior.** `OidcTokenProviderTests` covers cache miss, cache hit, refresh-before-expiry, forced invalidation, concurrent-callers-single-fetch, independent-key concurrency, non-2xx typed exception, malformed success responses, cancellation propagation, and secret/token redaction.

12. **AC12 - Sibling Epic 13 scopes remain untouched.** This story does not edit `TenantEmbeddingConfig.cs`, `EmbeddingClient.cs`, `EmbeddingProviderDefaults.cs`, tenant actors, AppHost, appsettings, docs/operations, migration tools, or vector-index code.

## Tasks / Subtasks

- [ ] Task 0 - Verify current boundaries and prerequisites (AC: #9, #12)
  - [ ] Confirm Story 13.1 is `done` before implementation starts; if it is still `review`, stop and report the prerequisite blocker rather than editing code.
  - [ ] Read `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` and confirm `builder.AddServiceDefaults()` already applies `ConfigureHttpClientDefaults(... AddStandardResilienceHandler() ...)` to all HttpClients.
  - [ ] Read `src/Hexalith.Memories.Server/Program.cs` and preserve the existing named `"EmbeddingClient"` registration unchanged.
  - [ ] Confirm `Microsoft.Extensions.Http.Resilience` is already centrally versioned and referenced by `Hexalith.Memories.ServiceDefaults`; do not add package versions to project files.

- [ ] Task 1 - Add the provider contract and implementation (AC: #1-#8, #10)
  - [ ] Add `src/Hexalith.Memories.Server/Ingestion/IOidcTokenProvider.cs` with two async methods:
    - `Task<string> GetAccessTokenAsync(string tokenEndpoint, string clientId, string clientSecret, string? scope, CancellationToken ct)`
    - `Task<string> InvalidateAndRefreshAsync(string tokenEndpoint, string clientId, string clientSecret, string? scope, CancellationToken ct)`
  - [ ] Add `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs` as a `sealed` singleton-safe class using typed `HttpClient`, `TimeProvider`, and `ILogger<OidcTokenProvider>`.
  - [ ] Use `ConcurrentDictionary<OidcTokenCacheKey, CachedOidcToken>` for token state, keyed only by normalized `tokenEndpoint` and `clientId`; do not include `clientSecret` in the key.
  - [ ] Use a separate `ConcurrentDictionary<OidcTokenCacheKey, SemaphoreSlim>` or equivalent per-key guard so identical cache misses collapse to one fetch while different tenants/client IDs proceed independently.
  - [ ] Build the token request with `FormUrlEncodedContent`. Include `scope` only when `!string.IsNullOrWhiteSpace(scope)`.
  - [ ] Parse the response with `System.Text.Json` and a small internal DTO/record. Do not introduce Newtonsoft.Json.
  - [ ] Treat `token_type` values other than `Bearer` (case-insensitive) as acquisition failures. Missing `token_type` may be accepted only if the code documents why, but tests must pin the chosen behavior.
  - [ ] Truncate non-success response bodies and malformed-response previews to 1024 characters before storing them on the exception.
  - [ ] Log only metadata: token endpoint host/path, client ID, correlation ID, HTTP status, and cache hit/miss/refresh state. Never log `clientSecret`, `access_token`, full form bodies, or raw response bodies that may contain tokens.
  - [ ] Clean up unused per-key semaphores after guarded fetches when safe; do not leak unbounded entries for every failed random client ID.

- [ ] Task 2 - Add the typed exception (AC: #7, #8, #10)
  - [ ] Add `src/Hexalith.Memories.Server/Ingestion/OidcTokenAcquisitionException.cs`.
  - [ ] Expose `HttpStatusCode? StatusCode`, `string ResponseBodyPreview`, `string TokenEndpoint`, `string ClientId`, and `string CorrelationId`.
  - [ ] Ensure the exception message is actionable but sanitized. It may include endpoint, client ID, status, and correlation ID; it must not include `clientSecret` or `access_token`.
  - [ ] Use the typed exception for token endpoint non-2xx responses and malformed success payloads. Use `ArgumentException.ThrowIfNullOrWhiteSpace` / `ArgumentNullException.ThrowIfNull` for programmer input validation.

- [ ] Task 3 - Wire DI and HttpClient registration (AC: #9)
  - [ ] In `Program.cs`, register `TimeProvider.System` only if the service is not already registered (`TryAddSingleton(TimeProvider.System)` already exists later for Story 9.3; reuse it rather than duplicating).
  - [ ] Register `OidcTokenProvider` and `IOidcTokenProvider` as singleton, following the existing concrete-plus-interface singleton pattern used for retry registries.
  - [ ] Register a typed HttpClient for `OidcTokenProvider` with `Timeout = TimeSpan.FromSeconds(10)` or lower.
  - [ ] Rely on the existing `AddServiceDefaults()` global standard resilience handler unless a focused registration test proves the typed client is missing it. If a direct handler is needed, add only one resilience handler and document why.
  - [ ] Do not modify the existing named `"EmbeddingClient"` HttpClient timeout or behavior in this story.

- [ ] Task 4 - Add focused tests (AC: #1-#11)
  - [ ] Add `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs`.
  - [ ] Use xUnit + Shouldly. Use a local `DelegatingHandler` to script token endpoint responses; do not call a real Keycloak instance.
  - [ ] Use `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`) to advance time deterministically for refresh-before-expiry tests.
  - [ ] Add tests named at minimum:
    - `GetAccessTokenAsync_CacheMiss_PostsClientCredentialsForm`
    - `GetAccessTokenAsync_CacheHit_DoesNotSendSecondHttpRequest`
    - `GetAccessTokenAsync_ExpiredEntry_FetchesNewToken`
    - `InvalidateAndRefreshAsync_EvictsAndFetchesExactlyOnce`
    - `GetAccessTokenAsync_ConcurrentSameKey_SendsSingleRequest`
    - `GetAccessTokenAsync_ConcurrentDifferentKeys_DoNotBlockEachOther`
    - `GetAccessTokenAsync_NonSuccess_ThrowsTypedExceptionWithoutCaching`
    - `GetAccessTokenAsync_MalformedSuccess_ThrowsTypedExceptionWithoutCaching`
    - `GetAccessTokenAsync_Cancellation_PropagatesOperationCanceledException`
    - `LogsAndExceptions_DoNotContainClientSecretOrAccessToken`
  - [ ] Add one DI/registration test only if it can be focused and stable; otherwise rely on a build plus direct provider tests and keep the story small.

- [ ] Task 5 - Validate and record completion (AC: #11, #12)
  - [ ] Run focused tests for `OidcTokenProviderTests`.
  - [ ] Run the relevant server ingestion test slice if local SDK constraints allow it.
  - [ ] Run `dotnet build Hexalith.Memories.slnx` if local SDK constraints allow it.
  - [ ] Record actual validation commands and outcomes in the Dev Agent Record. If `global.json` SDK pinning blocks local validation, record the exact SDK error and do not claim green tests.

## Dev Notes

### Current Implementation State

- `EmbeddingClient` currently supports only Google. It retrieves an API key from DAPR Secrets, builds `https://generativelanguage.googleapis.com/v1beta/models/{model}:embedContent`, sends `x-goog-api-key`, parses `embedding.values`, and refreshes the DAPR secret once on 401/403. Story 13.2 must not alter this path.
- `EmbeddingProviderDefaults` already contains `GoogleProviderName`, `OllamaProviderName`, and `OllamaModelName` after Story 13.1's implementation, but 13.2 should not edit that file.
- `TenantEmbeddingConfig` still has only the existing seven properties. `BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, and `OidcScope` belong to Story 13.4. Do not add them here.
- `Program.cs` currently calls `builder.AddServiceDefaults()` before registering application services. `AddServiceDefaults()` configures OpenTelemetry, health checks, service discovery, and global HttpClient defaults.
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` already calls `ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler(); http.AddServiceDiscovery(); )`. Microsoft's current .NET docs describe `AddStandardResilienceHandler` as the standard HttpClient resilience pipeline, with defaults including max retries `3`, exponential backoff with jitter, total timeout `30s`, and attempt timeout `10s`.

### File Scope

**Expected new files:**

- `src/Hexalith.Memories.Server/Ingestion/IOidcTokenProvider.cs`
- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs`
- `src/Hexalith.Memories.Server/Ingestion/OidcTokenAcquisitionException.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs`

**Expected edited files:**

- `src/Hexalith.Memories.Server/Program.cs`

**Do not edit in this story:**

- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` (Story 13.4)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` (Story 13.3)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` (Story 13.1)
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` (Story 13.5)
- `src/Hexalith.Memories.AppHost/Program.cs` (Story 13.7 or later wiring)
- `docs/operations/embedding-providers.md` (Story 13.7)
- vector migration tools or Redis/FalkorDB index naming (Story 13.6)

### Implementation Guidance

- Prefer a small internal value record for the cache key, for example `private sealed record OidcTokenCacheKey(string TokenEndpoint, string ClientId);`.
- Normalize `tokenEndpoint` by constructing an absolute `Uri` and using `Uri.AbsoluteUri`; reject relative or invalid endpoints early. Do not silently trim to host only because different realms use different paths.
- Use `TimeProvider.GetUtcNow()` for cache expiration so tests can advance time without sleeping.
- Cache only successful token acquisitions. Do not negative-cache failures; Keycloak/network failures should be retried on the next caller.
- After `InvalidateAndRefreshAsync`, do not perform two HTTP calls. The forced path should evict and then reuse the same guarded fetch primitive.
- Be careful with response preview truncation: the exception's `ResponseBodyPreview` must be capped to 1024 characters before it can hit logs or assertion output.
- If the provider logs success/failure events, use structured source-generated `LoggerMessage` helpers only if it keeps the diff small. A simple direct `ILogger` call is acceptable here if sanitized and tested.
- Do not log full endpoint URLs if query strings are present. Token endpoints should not include secrets in query strings, but the provider should still avoid copying query content into logs.
- Cancellation should flow into `SendAsync` and response-body reads. An `OperationCanceledException` triggered by the caller's token should not be wrapped in `OidcTokenAcquisitionException`.

### Security Requirements

- `client_secret` is a secret fetched from DAPR by Story 13.3 before calling this provider. In 13.2 tests it is supplied directly; never persist it.
- `access_token` is bearer credential material. Never log it, never include it in exception messages, and never expose it through public properties other than the `GetAccessTokenAsync` return value.
- Cache entries are in-memory only. Do not write tokens to configuration, DAPR state, files, environment variables, or telemetry attributes.
- The cache key intentionally excludes `clientSecret`; rotating a secret for the same `(tokenEndpoint, clientId)` requires `InvalidateAndRefreshAsync`, which Story 13.3 will call after 401/403.
- The `scope` value is not part of the initial planning key. If implementation finds that Keycloak can issue materially different tokens for the same client and different scopes, either include normalized scope in the cache key and document that adjustment in the Dev Agent Record, or stop for a product/security decision. Do not silently cache the wrong-scoped token.

### Testing Requirements

- Use xUnit + Shouldly only. Avoid raw `Assert.*`.
- Keep tests fully deterministic with scripted handlers and `FakeTimeProvider`.
- Tests should inspect captured `HttpRequestMessage` bodies and headers to prove URL-encoded form shape, not just count requests.
- For concurrency tests, avoid timing-based sleeps. Use `TaskCompletionSource` gates inside the test handler so both callers are pending before the first response completes.
- For log redaction, reuse the local capturing logger pattern from existing tests if helpful (`DirectoryIngestionServiceTests`, `RateLimitingLogTests`, `RetryFailureLogTests`), or add a small private logger inside `OidcTokenProviderTests`.
- No Docker, no real Keycloak, no Aspire fixture in this story.

### Previous Story Intelligence

- Story 13.1 completed the provider-name foundation but was intentionally narrowed after review. It explicitly pushed the persisted `{provider}:{model}` parser risk into Story 13.3, not 13.2. Do not absorb `ParseProvider` or `EmbeddingClient` parsing into this story.
- Story 13.1's file-scope discipline is part of the Epic 12/13 lesson pattern: when a story says "do not touch sibling files," treat that as an acceptance criterion, not a suggestion.
- Story 13.1's Dev Agent Record notes local SDK friction: `global.json` pins SDK `10.0.201` with `rollForward=latestFeature`, while some local runs only had `10.0.102/103`. Verify the current environment before promising full build/test results.

### Anti-Patterns to Avoid

- Do not implement a static global token cache. Use the DI singleton instance.
- Do not cache tokens by `clientId` alone; realm/token endpoint must be part of the key.
- Do not lock globally for all clients; that would serialize unrelated tenants and make Keycloak latency a system-wide bottleneck.
- Do not reuse the DAPR API-key cache from `EmbeddingClient`; this provider does not talk to DAPR and should stay provider-agnostic.
- Do not use Basic authentication for the token request unless the story is explicitly changed. The planned Keycloak path sends `client_id` and `client_secret` in the form body.
- Do not add appsettings defaults or tenant OIDC fields in this story. The token provider API is enough for tests and for Story 13.3 to consume later.

### Project Structure Notes

- Namespace must be `Hexalith.Memories.Server.Ingestion`.
- Every new `.cs` file needs the existing ITANEO MIT copyright header.
- Public/internal members require XML documentation because documentation generation is enabled.
- Keep records/classes sealed unless inheritance is intentional.
- Central package management is enforced. If a new package is truly needed, add its version only to `Directory.Packages.props` and a versionless reference in the consuming project, but this story should not need a new package.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` Epic 13 Story 13.2] - OIDC client credentials provider, cache, forced invalidation, concurrency, typed exception, DI, and redaction ACs.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-29.md` Sections 2.4, 4.4, 4.5] - `IOidcTokenProvider`, Keycloak client `memories-embedding`, DAPR secret name, and Epic 13 sequencing.
- [Source: `_bmad-output/implementation-artifacts/13-1-extend-embedding-provider-defaults-to-accept-ollama.md`] - Previous story scope constraints and decision that `ParseProvider` belongs to Story 13.3.
- [Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`] - Current Google-only secret retrieval, request send, response parse, and 401/403 refresh path that 13.2 must not modify.
- [Source: `src/Hexalith.Memories.Server/Program.cs`] - Current service and HttpClient registrations.
- [Source: `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`] - Global `ConfigureHttpClientDefaults(... AddStandardResilienceHandler() ...)` behavior.
- [Source: Microsoft Learn, "Build resilient HTTP apps: Key development patterns", accessed 2026-05-01] - `Microsoft.Extensions.Http.Resilience` / `AddStandardResilienceHandler` current defaults and guidance: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience
- [Source: `Directory.Packages.props`] - Central package versions, including `Microsoft.Extensions.Http.Resilience` 10.0.0 and test dependencies.

## Project Context Reference

The BMad `persistent_facts` glob found `Hexalith.Commons/_bmad-output/project-context.md` but no Memories-local `project-context.md`. Treat the Commons context as general Hexalith ecosystem guidance only. Repository-specific instructions in this story and in the Memories planning artifacts take precedence.

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- Story authored on 2026-05-01 by the recurring pre-dev hardening automation after pre-flight pass `2026-05-01T15:04:11Z`.
- No code implementation was performed in this run; this is a create-story artifact only.

### Completion Notes List

- Story created with status `ready-for-dev`.
- Sprint status updated from `backlog` to `ready-for-dev` for `13-2-implement-oidc-token-provider`.
- The story is ready for party-mode review / later development, with implementation gated on Story 13.1 reaching `done`.

### File List

- `_bmad-output/implementation-artifacts/13-2-implement-oidc-token-provider.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

| Date       | Change                                                                                                                                                                                                                                                                                                           | Author |
|------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|
| 2026-05-01 | Story 13.2 context created: scoped `IOidcTokenProvider` / `OidcTokenProvider` / typed exception / tests / DI registration; pinned cache, concurrency, forced invalidation, redaction, and sibling-story boundaries; status promoted backlog -> ready-for-dev.                                                        | Codex |
