# Story 14.3: OIDC and Embedding Security Hardening

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want OIDC token acquisition and embedding-client error handling hardened,
so that cancellation, credential rotation, malformed URLs, token refresh storms, and transport errors do not leak secrets or produce avoidable outages.

## Acceptance Criteria

1. Given several callers wait on the same OIDC token acquisition, when the caller that started the fetch cancels, then remaining waiters are not forced to refire the HTTP token request solely because the leader cancelled.

2. Given OIDC and embedding clients are registered in DI, when their HttpClient lifetime is inspected, then the implementation follows the chosen `IHttpClientFactory` or typed-client pattern without singleton-captured stale handlers.

3. Given provider URLs and OIDC token endpoints are validated, when a URL contains userinfo such as `https://user:pw@host`, then validation rejects it for both `OidcTokenProvider` and `EmbeddingProviderDefaults`.

4. Given several callers force-refresh the same token concurrently, when invalidation occurs, then refresh requests collapse where practical or are explicitly bounded and covered by tests.

5. Given OIDC or embedding transport fails because of network, timeout, or IO errors, when the caller receives the failure, then it is wrapped in the project-specific typed exception expected by callers and secret values and bearer tokens are not present in exception text or logs.

6. Given an Ollama tenant's DAPR secret has rotated, when the first request returns 401 or 403 and retry is attempted, then stale `client_secret` cache state is evicted symmetrically with the Google API-key path.

7. Given redaction handles sensitive values, when values overlap, are short, or appear in upstream error payloads, then redaction is length-aware, longest-value-first, and tested with realistic OIDC and embedding failure text.

## Tasks / Subtasks

- [x] Task 1 - Detach shared OIDC acquisitions from leader cancellation (AC: 1)
  - [x] Refactor `OidcTokenProvider.GetOrFetchAsync(...)` so the in-flight same-key acquisition is not cancelled solely by the caller that first entered the per-key guard.
  - [x] Preserve caller cancellation for each waiting caller by using `WaitAsync(ct)` or equivalent wait cancellation around a shared acquisition task.
  - [x] Ensure a failed token endpoint response still fails all current waiters, does not populate `_cache`, and allows a later caller to retry.
  - [x] Add a deterministic test where the first caller cancels after the HTTP request starts and a second same-key waiter still receives the original token without causing a second HTTP request.

- [x] Task 2 - Fix HttpClient lifetime without broad DI churn (AC: 2)
  - [x] Resolve the singleton-captured `HttpClient` issue for `OidcTokenProvider`. Prefer injecting `IHttpClientFactory` and creating `CreateClient(OidcTokenProvider.HttpClientName)` per token fetch, or convert to a short-lived typed-client pattern that does not capture a typed client in a singleton.
  - [x] Inspect `EmbeddingClient` registration and constructor surface. Keep `EmbeddingClient` singleton only if it continues to hold `IHttpClientFactory` rather than a captive `HttpClient`.
  - [x] Remove the optional default value from the 5-argument `EmbeddingClient` constructor if the DI ambiguity can be closed without breaking existing tests or deliberate 4-argument construction.
  - [x] Preserve `builder.AddServiceDefaults()` and its global `AddStandardResilienceHandler()` pipeline. Do not stack duplicate resilience handlers.
  - [x] Add focused DI/constructor tests only where they can prove lifetime behavior without requiring a full host integration run.

- [x] Task 3 - Reject credential-bearing URLs consistently (AC: 3)
  - [x] Update `OidcTokenProvider.ValidateAndCreateKey(...)` so token endpoints with non-empty `Uri.UserInfo`, query secrets, or fragments are rejected with a sanitized `ArgumentException`.
  - [x] Update `EmbeddingProviderDefaults.ValidateOptionalHttpUrl(...)` so `BaseUrl` and `OidcTokenEndpoint` reject userinfo and fragments. Query strings should be rejected for provider base URLs; if token endpoint queries are intentionally retained per OAuth endpoint rules, document and test the chosen exception explicitly.
  - [x] Add negative tests for `https://user:pw@host`, `https://token.example/realm?client_secret=value`, and fragment-bearing URLs. Assertion text must not echo embedded credentials.

- [x] Task 4 - Bound forced-refresh storms (AC: 4, 6)
  - [x] Make concurrent `InvalidateAndRefreshAsync(...)` calls for the same `(tokenEndpoint, clientId, scope)` collapse to one fresh token fetch where practical, or enforce a documented upper bound that cannot hammer Keycloak during an Ollama 401 storm.
  - [x] Keep forced refresh scoped to the exact normalized endpoint, client ID, and scope. Do not evict other tenants, clients, or scopes.
  - [x] In `EmbeddingClient.GenerateOllamaAsync(...)`, evict the cached DAPR secret (`_apiKeyCache`) before the 401/403 retry so a rotated `client_secret` is re-read like the Google API-key path.
  - [x] Add tests proving Ollama secret rotation on 401 uses a newly fetched DAPR secret and does not reuse the stale value.

- [x] Task 5 - Wrap transport failures without leaking credentials (AC: 5, 7)
  - [x] Wrap `HttpRequestException`, `TaskCanceledException` caused by timeout, and `IOException` from token fetches in `OidcTokenAcquisitionException` with sanitized endpoint/client/correlation metadata.
  - [x] Preserve caller-requested cancellation as `OperationCanceledException` when the supplied cancellation token is cancelled.
  - [x] Wrap Ollama token-refresh and embedding transport failures that cross the `EmbeddingClient` boundary in `EmbeddingApiException`, keeping the original exception as `InnerException`.
  - [x] Replace `HandleEmbeddingResponseAsync(..., params string?[] sensitiveValues)` with an explicit immutable collection or typed redaction context so accidental positional arguments cannot silently become redaction values.
  - [x] Validate whitespace bearer-token returns before constructing `AuthenticationHeaderValue`; fail with a sanitized `EmbeddingApiException`.

- [x] Task 6 - Make redaction deterministic and realistic (AC: 5, 7)
  - [x] Replace substring-order redaction in `EmbeddingClient.RedactSensitiveValues(...)` with a helper that filters null/blank values, applies a minimum length floor for raw value redaction, orders by descending length, and avoids masking common short words or full input text accidentally.
  - [x] Keep `OidcTokenProvider.SanitizePreview(...)` redacting token-shaped JSON fields before truncation. Add coverage for overlapping `access_token`, `refresh_token`, `id_token`, `client_secret`, and bearer values.
  - [x] Use representative OIDC and Ollama error payloads containing client secrets, bearer tokens, Google API keys, duplicate token-shaped JSON fields, and short benign substrings.
  - [x] Do not redact secret-name references such as `memories-embedding-client-secret` unless the implementation explicitly chooses stricter operator-visible behavior and records why.

- [x] Task 7 - Update deferred-work bookkeeping and validation evidence (AC: 1-7)
  - [x] Resolve or carry forward targeted deferred IDs: `13.2-RV1`, `13.2-RV2`, `13.2-RV3`, `13.2-RV5`, `13.2-RV6`, `13.3-RV6`, `13.3-RV7`, `13.3-RV11`, `13.3-RV12`, `13.3-RV14`, `13.3-RV15`, and `13.4-RV5`.
  - [x] Do not close `13.2-RV4`, `13.2-RV7`, `13.2-RV8`, `13.2-RV9`, `13.3-RV8`, `13.3-RV9`, `13.3-RV10`, `13.3-RV13`, or migration/integration-test IDs unless implementation genuinely resolves them and the story notes why they became in scope.
- [x] Run focused OIDC and embedding tests, then record exact commands and outcomes in this story's Dev Agent Record.
- [x] Run `git diff --check -- src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs src/Hexalith.Memories.Server/Program.cs tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs _bmad-output/implementation-artifacts/deferred-work.md`.

### Review Findings

- [x] [Review][Patch] Google embedding transport failures still surface raw [src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:314] - fixed in review close-out; Google transport failures now wrap in `EmbeddingApiException`.
- [x] [Review][Patch] Forced refresh can join a stale-secret in-flight token acquisition [src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs:138] - fixed in review close-out; forced and non-forced in-flight acquisitions are separated.
- [x] [Review][Patch] Cached-token recheck can throw KeyNotFoundException during concurrent invalidation [src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs:178] - fixed in review close-out; cache recheck now uses the captured token value.
- [x] [Review][Patch] Invalid non-blank bearer tokens can escape as raw FormatException [src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:334] - fixed in review close-out; bearer header creation is validated and wrapped.
- [x] [Review][Patch] OIDC content-read transport failures are not wrapped [src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs:265] - fixed in review close-out; token response read failures are wrapped.
- [x] [Review][Patch] OIDC response preview does not redact bearer-token text [src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs:397] - fixed in review close-out; bearer-shaped text is redacted.
- [x] [Review][Patch] Short input text can remain visible in provider error bodies [src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:174] - fixed in review close-out; full input text is force-redacted separately from short secret fragments.
- [x] [Review][Patch] Already-canceled callers can still launch detached token fetches [src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs:133] - fixed in review close-out; cancellation is checked before in-flight creation.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs` - UPDATE. Cancellation, per-key refresh collapse, HttpClient acquisition, URL validation, transport wrapping, and redaction.
- `src/Hexalith.Memories.Server/Ingestion/OidcTokenAcquisitionException.cs` - UPDATE only if new typed transport metadata is required.
- `src/Hexalith.Memories.Server/Ingestion/IOidcTokenProvider.cs` - UPDATE only if the implementation needs a documented contract clarification; avoid signature churn.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` - UPDATE. Ollama 401 secret-cache eviction, typed wrapping, constructor cleanup, bearer validation, and redaction helper hardening.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` - UPDATE. Userinfo/fragment/query validation and any OIDC URL validation clarifications.
- `src/Hexalith.Memories.Server/Program.cs` - UPDATE. OIDC/embedding HttpClient registration changes only.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs` - UPDATE. Cancellation, concurrent invalidation, URL validation, transport wrapping, and redaction coverage.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` - UPDATE. Secret rotation, wrapper exceptions, bearer validation, redaction, and constructor/lifetime regressions.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs` - UPDATE. Userinfo/fragment/query validation coverage.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Resolve or carry forward targeted deferred IDs with validation evidence.
- `_bmad-output/implementation-artifacts/14-3-oidc-and-embedding-security-hardening.md` - UPDATE. Record implementation notes, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-03.md`
- `_bmad-output/implementation-artifacts/13-2-implement-oidc-token-provider.md`
- `_bmad-output/implementation-artifacts/13-3-extend-embedding-client-to-support-ollama.md`
- `_bmad-output/implementation-artifacts/13-4-extend-tenant-embedding-config-with-additive-oidc-fields.md`
- `_bmad-output/implementation-artifacts/13-5-surface-new-fields-via-tenant-configuration-actor.md`
- `_bmad-output/implementation-artifacts/13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide.md`
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`
- `docs/operations/embedding-providers.md`

Forbidden by default:

- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs`
- `src/Hexalith.Memories.Server/Migration/**`
- `tests/Hexalith.Memories.IntegrationTests/**`
- `docs/**` except if a validation or operator-warning decision must be documented with explicit story evidence
- `deploy/**`
- `Directory.Packages.props`
- `Directory.Build.props`
- `Hexalith.AI.Tools/**`
- `Hexalith.Commons/**`
- `Hexalith.EventStore/**`

## Dev Notes

### Current Implementation State

`OidcTokenProvider` is a singleton service with `_cache` and `_guards` dictionaries keyed by normalized `(tokenEndpoint, clientId, scope)`. `GetAccessTokenAsync(...)` and `InvalidateAndRefreshAsync(...)` both flow the caller's cancellation token into the guarded fetch path. The existing waiter-cancellation test covers a waiting caller, but the leader's cancellation can still cancel `_httpClient.SendAsync(...)` and force remaining waiters to refire the token request. `InvalidateAndRefreshAsync(...)` removes the cache entry before entering the guard and passes `forceRefresh: true`, so concurrent forced refresh callers can each perform a token endpoint call.

`Program.cs` registers a named `OidcTokenProvider` HttpClient, immediately creates it through `IHttpClientFactory.CreateClient(...)`, and stores it inside the singleton provider. Microsoft documents that factory-created clients and typed clients are expected to be short-lived; capturing them in a singleton can prevent handler rotation and DNS updates from taking effect. `EmbeddingClient` is also a singleton, but it currently stores `IHttpClientFactory`, which is the safer pattern. Do not convert it to a captured `HttpClient`.

`EmbeddingProviderDefaults.ValidateOptionalHttpUrl(...)` currently accepts absolute HTTP(S) URLs with userinfo, queries, and fragments. `OidcTokenProvider.ValidateAndCreateKey(...)` also accepts userinfo and strips query/fragment by normalizing to scheme/server/path. This story should fail closed for credential-bearing URL shapes and make any token-endpoint query exception deliberate and tested.

`EmbeddingClient.GenerateOllamaAsync(...)` gets the DAPR secret once, calls `IOidcTokenProvider.GetAccessTokenAsync(...)`, and on 401/403 calls `InvalidateAndRefreshAsync(...)` using the same `clientSecret`. The Google path evicts `_apiKeyCache` before retry; the Ollama path does not, so a rotated DAPR secret can remain stale until process restart.

`EmbeddingClient.RedactSensitiveValues(...)` currently performs raw substring replacement in argument order. That can over-redact short benign strings and under-redact overlapping values. The existing `params` signature after a `CancellationToken` also makes every future positional argument part of the redaction set whether the caller intended it or not.

### Deferred IDs Targeted

This story is the normal lifecycle home for:

- `13.2-RV1`: leader cancellation poisoning shared OIDC fetch.
- `13.2-RV2`: singleton-captured OIDC HttpClient and related handler rotation risk.
- `13.2-RV3`: token transport failures not wrapped in `OidcTokenAcquisitionException`.
- `13.2-RV5`: token endpoint userinfo accepted.
- `13.2-RV6`: concurrent forced refresh calls can each hit the token endpoint.
- `13.3-RV6`: constructor surface / DI ambiguity in `EmbeddingClient`.
- `13.3-RV7`: sensitive-value redaction is raw substring replacement.
- `13.3-RV11`: `params string?[]` redaction values after `CancellationToken`.
- `13.3-RV12`: whitespace bearer token can crash `AuthenticationHeaderValue`.
- `13.3-RV14`: token-refresh exceptions leak past `EmbeddingClient`.
- `13.3-RV15`: Ollama 401 retry does not evict stale DAPR `client_secret`.
- `13.4-RV5`: provider/token URLs with userinfo accepted.

Adjacent but out of scope unless touched by the implementation:

- `13.2-RV4` production TLS enforcement for `http://` token endpoints; local Keycloak and fake servers still need loopback HTTP.
- `13.2-RV7` cache/guard eviction lifecycle beyond the refresh-collapse changes.
- `13.2-RV8` adversarial large-token-body limits.
- `13.2-RV9` test handler thread-safety unless the concurrency tests are already being refactored.
- `13.3-RV9` circuit breaker for persistent 401s.
- `13.3-RV10` Ollama 429/Retry-After test.
- `13.3-RV13` base URL query/fragment dropping, except where URL validation directly addresses it.
- Migration redaction and fake-server malformed-token branch tests belong to Story 14.4.

### Implementation Guardrails

- Keep this as security hardening for existing OIDC and embedding surfaces. Do not redesign provider dispatch, add a provider registry, change vector dimensions, or alter tenant configuration contracts.
- Do not log request bodies, bearer tokens, `client_secret`, DAPR secret values, Google API keys, or full input text.
- Prefer explicit typed exceptions at service boundaries. `OidcTokenAcquisitionException` should represent token acquisition failures; `EmbeddingApiException` should represent embedding provider failures reaching ingestion/search callers.
- Preserve `OperationCanceledException` when the caller's cancellation token is the cause. Do not convert user-requested cancellation into a transport failure.
- Keep tests deterministic with scripted `DelegatingHandler`, `TaskCompletionSource`, `FakeTimeProvider`, Shouldly, and existing helper patterns. Do not require Keycloak, Ollama, DAPR, Docker, or Aspire for this story's core validation.
- Do not initialize or update nested submodules. Do not change root-level submodule pointers.

### Technical Constraints and References

- `IHttpClientFactory` exists to manage `HttpClient`/handler lifetimes; Microsoft documents that handlers must be recycled so DNS changes are observed and that typed clients captured in singletons can defeat that behavior. Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory
- The project's `AddServiceDefaults()` already configures standard HTTP resilience. Microsoft documents `AddStandardResilienceHandler()` as the standard HTTP resilience pipeline; avoid adding a duplicate handler unless a focused test proves the current client lacks it. Source: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience
- OAuth 2.0 client credentials uses `application/x-www-form-urlencoded` with `grant_type=client_credentials` and optional `scope`; refresh tokens should not be included for this grant. Source: https://www.rfc-editor.org/rfc/rfc6749#section-4.4.2 and https://www.rfc-editor.org/rfc/rfc6749#section-4.4.3
- RFC 6749 also allows token endpoint query components in general endpoint syntax, but this repository should reject credential-bearing or ambiguous URL shapes unless a specific operator requirement exists. Source: https://www.rfc-editor.org/rfc/rfc6749#section-3.2

### Testing Requirements

Minimum validation before review:

```powershell
dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~OidcTokenProviderTests"
dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingClientTests"
dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingProviderDefaultsTests"
git diff --check -- src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs src/Hexalith.Memories.Server/Ingestion/OidcTokenAcquisitionException.cs src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs src/Hexalith.Memories.Server/Program.cs tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs _bmad-output/implementation-artifacts/deferred-work.md
```

Additional probes to record when relevant:

- Same-key leader cancellation produces one token endpoint request and the non-cancelled waiter succeeds.
- Concurrent forced refresh for one key does not produce unbounded token endpoint calls.
- `https://user:pw@host/...` fails validation without echoing `user:pw`.
- Token endpoint timeout/network/IO failures produce sanitized `OidcTokenAcquisitionException`.
- Ollama 401 after DAPR secret rotation re-reads the secret before retry.
- Redaction handles overlapping secrets longest-value-first and does not mask benign short substrings.

## Project Structure Notes

- This is a server ingestion hardening story. Expected implementation stays under `src/Hexalith.Memories.Server/Ingestion`, `src/Hexalith.Memories.Server/Program.cs`, matching server tests, and BMAD deferred-work bookkeeping.
- Use existing C# conventions: copyright header on new files, XML documentation on public/internal members, nullable-safe validation, xUnit + Shouldly tests, and no package versions in project files.
- The `Hexalith.Commons` project context discovered by the persistent-facts glob is background Hexalith guidance only. Repository-specific story scope and current Memories code are authoritative.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 14 and Story 14.3 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-03.md` - approved Epic 14 scope and targeted deferred IDs.
- `_bmad-output/implementation-artifacts/deferred-work.md` - source of `13.2`, `13.3`, and `13.4` deferred IDs.
- `_bmad-output/implementation-artifacts/13-2-implement-oidc-token-provider.md` - OIDC provider contract, review findings, and validation history.
- `_bmad-output/implementation-artifacts/13-3-extend-embedding-client-to-support-ollama.md` - EmbeddingClient Ollama path, review findings, and redaction gaps.
- `_bmad-output/implementation-artifacts/13-4-extend-tenant-embedding-config-with-additive-oidc-fields.md` - URL validation and OIDC config constraints.
- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs` - current token cache, guards, URL normalization, transport call, and preview sanitizer.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` - current Google/Ollama dispatch, DAPR secret cache, 401 retry, and redaction helper.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` - provider/auth-mode URL validation.
- `src/Hexalith.Memories.Server/Program.cs` - current singleton, named HttpClient, and service registration pattern.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs` - existing deterministic token-provider test harness.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` - existing scripted HTTP and redaction test patterns.
- Microsoft `IHttpClientFactory` docs: https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory
- Microsoft HTTP resilience docs: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience
- OAuth 2.0 RFC 6749 client credentials grant: https://www.rfc-editor.org/rfc/rfc6749#section-4.4

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Pre-dev hardening preflight JSON timestamp `2026-05-03T11:24:04Z` failed only `working tree cleanliness` with stdout ` M Hexalith.EventStore`; classified as a soft working-tree warning because the dirty path is outside BMAD-owned pre-dev story-operation paths.
- Story selection chose `14-3-oidc-and-embedding-security-hardening` because `ready_count` was below the target of `5` and this was the first backlog story in sprint-status order.
- `/bmad-create-story 14-3-oidc-and-embedding-security-hardening` context gathering loaded Epic 14 planning, the approved 2026-05-03 sprint-change proposal, Stories 13.2-13.4 and 14.1-14.2 context, current OIDC/embedding source and tests, deferred-work entries, recent git history, and current Microsoft/OAuth documentation.

### Completion Notes List

- Story context created on 2026-05-03; implementation completed on 2026-05-04.
- Scope was limited to OIDC token acquisition, EmbeddingClient transport/error/redaction hardening, provider URL validation, targeted deferred-work closure, and focused tests.
- Migration redaction and Ollama fake-server branch coverage are intentionally left for Story 14.4.
- The existing `Hexalith.EventStore` dirty submodule state was not touched.

#### Implementation Summary (Tasks 1-7)

- **Task 1 (AC1):** `OidcTokenProvider` now uses a `ConcurrentDictionary<key, Task<CachedOidcToken>> _inflight` to collapse concurrent fetches for the same key. The leader runs `RunDetachedFetchAsync` which calls `httpClient.SendAsync(request, CancellationToken.None)` so caller cancellation cannot poison the shared HTTP fetch. Per-caller cancellation flows through `Task.WaitAsync(ct)` at the public surface; `OperationCanceledException` propagates without contaminating other waiters or the cache. Failures call `tcs.SetException(...)` so all current waiters fail; a later caller re-creates the inflight task and retries.
- **Task 2 (AC2):** `OidcTokenProvider`'s constructor now takes `IHttpClientFactory` and resolves `factory.CreateClient(HttpClientName)` per fetch so handler rotation (DNS, TLS pooling, `PooledConnectionLifetime`) is honored. `EmbeddingClient` continues to take `IHttpClientFactory` (already correct). The 5-arg `EmbeddingClient` constructor lost its optional `= null` default so DI ambiguity is closed without breaking the 4-arg call sites used by tests. `Program.cs` registration replaced the eagerly-resolved client with the factory itself; `tools/MigrateEmbeddingVectors/Program.cs` was updated as a Scope-Override to compile the standalone tool against the new constructor (`SimpleHttpClientFactory` already implements `IHttpClientFactory`).
- **Task 3 (AC3):** Both `OidcTokenProvider.ValidateAndCreateKey` and `EmbeddingProviderDefaults.ValidateOptionalHttpUrl` reject URLs with non-empty `Uri.UserInfo`, query strings, and fragments. Token-endpoint queries are rejected uniformly for safety per Story Implementation Guardrails (RFC 6749 §3.2 permits queries in the abstract, but we reject so credential-bearing query strings cannot slip past validation). Error text deliberately does not echo any embedded credential, query value, or fragment.
- **Task 4 (AC4, AC6):** Concurrent forced refresh callers for the same key collapse to a single in-flight fetch (storm-collapse via `_inflight`). `EmbeddingClient.GenerateOllamaAsync` now evicts `_apiKeyCache.TryRemove(...)` before the 401/403 retry, then re-fetches the DAPR `client_secret` and uses the rotated value when calling `tokenProvider.InvalidateAndRefreshAsync(...)`.
- **Task 5 (AC5, AC7):** `OidcTokenProvider.FetchTokenAsync` wraps `HttpRequestException`, `TaskCanceledException` (timeout — fetches are detached, so any TCE here is a Timeout), and `IOException` in `OidcTokenAcquisitionException` with sanitized correlation/endpoint/client metadata. `EmbeddingClient.GenerateOllamaAsync` wraps both token-acquisition and Ollama transport failures in `EmbeddingApiException` with the original exception as `InnerException`. Caller cancellation surfaces as `OperationCanceledException`. `HandleEmbeddingResponseAsync` replaced `params string?[] sensitiveValues` with `IReadOnlyCollection<string?> sensitiveValues`. `EnsureNonBlankBearerToken` rejects whitespace tokens before any `AuthenticationHeaderValue` construction. Google's transport failures intentionally remain raw to preserve existing workflow retry semantics; this is documented inline.
- **Task 6 (AC5, AC7):** `EmbeddingClient.RedactSensitiveValues` is now an internal static helper that filters null/blank, applies `RedactionMinLength = 8`, deduplicates, and replaces in length-descending order so longer secrets containing shorter substrings are masked atomically and short benign words like "ai" or "ok" are not. `OidcTokenProvider.SanitizePreview` keeps the redact-then-truncate order from Story 13.2 and now also redacts `refresh_token` / `id_token` JSON properties. New tests cover overlapping `access_token`, `refresh_token`, `id_token`, `client_secret`, and bearer values; short benign substrings are explicitly asserted unmasked. Secret-name references like `memories-embedding-client-secret` are intentionally not in the redaction set.
- **Task 7 (AC1-7):** `_bmad-output/implementation-artifacts/deferred-work.md` was updated with a "Closed by: Story 14.3" section listing the twelve resolved IDs, plus inline `[resolved in 14.3]` markers on each entry. Adjacent IDs explicitly out of scope (`13.2-RV4`, `13.2-RV7`, `13.2-RV8`, `13.2-RV9`, `13.3-RV8`, `13.3-RV9`, `13.3-RV10`, `13.3-RV13`) were not closed.

#### Validation Evidence

```powershell
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"
$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"

dotnet build .\Hexalith.Memories.slnx -c Debug --nologo -v q
# La génération a réussi. 0 Avertissement(s) 0 Erreur(s)

dotnet test .\tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~OidcTokenProviderTests"
# Réussi! - échec : 0, réussite : 28, ignorée(s) : 0, total : 28

dotnet test .\tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~EmbeddingClientTests"
# Réussi! - échec : 0, réussite : 56, ignorée(s) : 0, total : 56

dotnet test .\tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~EmbeddingProviderDefaultsTests"
# Réussi! - échec : 0, réussite : 130, ignorée(s) : 0, total : 130

dotnet test .\tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build --nologo
# Réussi! - échec : 0, réussite : 1725, ignorée(s) : 0, total : 1725, durée : 2 m 26 s

# Review close-out validation after applying all 8 review patches:

dotnet test .\tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~OidcTokenProviderTests"
# Réussi! - échec : 0, réussite : 31, ignorée(s) : 0, total : 31

dotnet test .\tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~EmbeddingClientTests"
# Réussi! - échec : 0, réussite : 62, ignorée(s) : 0, total : 62

dotnet test .\tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~EmbeddingProviderDefaultsTests"
# Réussi! - échec : 0, réussite : 130, ignorée(s) : 0, total : 130

dotnet test .\tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build --nologo
# Réussi! - échec : 0, réussite : 1734, ignorée(s) : 0, total : 1734, durée : 2 m 27 s

git diff --check -- src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs `
                    src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs `
                    src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs `
                    src/Hexalith.Memories.Server/Program.cs `
                    tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs `
                    tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs `
                    tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs `
                    _bmad-output/implementation-artifacts/deferred-work.md
# Clean except non-blocking LF/CRLF warnings (consistent with prior story close-outs).
```

Local SDK note: this machine had only `10.0.103` installed and `global.json` pins `10.0.201`. The dev-story session installed `10.0.201` user-locally via `dotnet-install.ps1` to `$USERPROFILE/.dotnet/`; no machine-wide installer was used and `global.json` was not modified.

#### Scope-Justification

- `tools/MigrateEmbeddingVectors/Program.cs` (1 line): the `OidcTokenProvider` constructor change required by Story 14.3 AC2 broke this standalone migration tool's call site. The fix swaps `_httpClientFactory.GetSharedClient()` for `_httpClientFactory` directly (the `SimpleHttpClientFactory` already implements `IHttpClientFactory`). `tools/**` is not in the story's "Forbidden by default" list; this build-only fix has no behavior change for the tool. Recorded here so Senior Review can flag as a Scope-Override.

### File List

- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`
- `tools/MigrateEmbeddingVectors/Program.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/14-3-oidc-and-embedding-security-hardening.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-03: Created Story 14.3 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-04: Implemented Tasks 1-7 (OIDC leader-cancellation detachment, IHttpClientFactory injection, credential-bearing URL rejection, forced-refresh storm collapse, Ollama 401 secret rotation, transport-exception wrapping, length-aware redaction). Closed deferred IDs 13.2-RV1/2/3/5/6, 13.3-RV6/7/11/12/14/15, 13.4-RV5. Transitioned `in-progress` -> `review`. Validation: solution build 0W/0E, focused suites OidcTokenProvider 28/28, EmbeddingClient 56/56, EmbeddingProviderDefaults 130/130, full Server.Tests 1725/1725, `git diff --check` clean except non-blocking LF/CRLF warnings.
- 2026-05-04: Code review close-out applied 8/8 review patches: separated forced-refresh in-flight token acquisitions from normal cache-miss fetches, removed cache recheck race, wrapped Google embedding transport failures, wrapped response-read transport failures, validated invalid bearer header values, redacted bearer-shaped OIDC previews, force-redacted full input text including short values, and short-circuited already-canceled token callers before detached fetch creation. Transitioned `review` -> `done`. Validation: OidcTokenProviderTests 31/31, EmbeddingClientTests 62/62, EmbeddingProviderDefaultsTests 130/130, full Server.Tests 1734/1734, `git diff --check` clean except non-blocking LF/CRLF warnings.

## Story Completion Status

Code review complete. All review findings resolved and validation passed. Status set to `done`.
