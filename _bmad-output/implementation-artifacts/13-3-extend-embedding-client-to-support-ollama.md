# Story 13.3: Extend EmbeddingClient to Support Ollama

Status: ready-for-dev

**Effort estimate:** ~1.25-1.75 working days. Breakdown:

- **0.15 day - Task 0:** Verify prerequisites, current embedding client shape, current `TenantEmbeddingConfig` OIDC fields, and Story 13.2 token-provider API.
- **0.20 day - Task 1:** Add a colon-preserving provider/model parser contract for persisted `EmbeddingProvider` values and any local dispatch helpers needed by `EmbeddingClient`.
- **0.35 day - Task 2:** Refactor `EmbeddingClient` dispatch into provider-specific request building, auth injection, retry, and response parsing while preserving Google behavior.
- **0.35 day - Task 3:** Add Ollama-focused `EmbeddingClientTests` for request shape, bearer injection, response parsing, dimension mismatch, unsupported provider, and 401 retry paths.
- **0.20 day - Task 4:** Run focused ingestion tests and a server build/test slice when SDK constraints allow; record exact outcomes.

**HARD prerequisite:** Story 13.1 must be `done` before implementation starts because this story depends on `EmbeddingProviderDefaults.OllamaProviderName`, `OllamaModelName`, and the colon-enabled model validation. Story 13.2 must be `done` before implementation starts because this story consumes `IOidcTokenProvider.GetAccessTokenAsync(...)` and `InvalidateAndRefreshAsync(...)`. Story 13.4 must be `done` before implementation starts because this story needs `TenantEmbeddingConfig.BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, and `OidcScope`; without those fields the Ollama path cannot compile cleanly. This story may remain `ready-for-dev` to maintain the buffer, but dev work must stop if any prerequisite is not complete.

**SOFT prerequisite:** Keep Story 13.2's implementation notes open while developing this story. If the token-provider API names or exception types differ from the planned names, adapt to the actual committed 13.2 surface rather than reintroducing a second token client.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

Extend `EmbeddingClient` so `GenerateAsync(...)` still serves Google exactly as it does today, but dispatches Ollama tenants to the Ollama-native endpoint:

```text
POST {BaseUrl}/api/embed
Authorization: Bearer <token>
Content-Type: application/json

{ "model": "qwen3-embedding:4b", "input": "<text>" }
```

The Ollama response shape is `{ "embeddings": [[...]] }`; return the first vector and assert its length equals `config.Dimensions`. Resolve the OIDC `client_secret` from DAPR using `config.ApiSecretKeyName`, acquire the bearer token via `IOidcTokenProvider`, retry once on 401/403 by calling `InvalidateAndRefreshAsync(...)`, and never log or surface bearer tokens, client secrets, or full input text.

This story also inherits the parser obligation from Story 13.1 review: any persisted `{provider}:{model}` splitter must preserve model names containing colons. The contract test must pin `ollama:qwen3-embedding:4b -> provider=ollama, model=qwen3-embedding:4b`.

## Story

As a **backend developer**,
I want `EmbeddingClient` to dispatch to the Ollama-native HTTP API when the tenant's provider is `ollama`, with `Authorization: Bearer <jwt>` injected from `IOidcTokenProvider`,
so that the existing ingestion workflow can produce tenant-aware embeddings against the self-hosted gateway with no caller-side changes.

## Acceptance Criteria

1. **AC1 - Google flow remains behaviorally unchanged.** A tenant configured with `Provider = "google"` still calls `https://generativelanguage.googleapis.com/v1beta/models/{model}:embedContent`, sends `x-goog-api-key`, uses the existing Google request body shape, parses `embedding.values`, and refreshes the DAPR API key once on 401/403. Existing `EmbeddingClientTests` Google scenarios continue to pass without rewriting their intent.

2. **AC2 - Ollama request uses the native endpoint and body shape.** A tenant configured with `Provider = "ollama"`, `BaseUrl = "https://llm.tache.ai"`, `Model = "qwen3-embedding:4b"`, and `Dimensions = 2560` sends a `POST` to `https://llm.tache.ai/api/embed` with JSON body containing exactly the configured model and input text. The implementation must avoid double slashes when `BaseUrl` has a trailing slash.

3. **AC3 - OIDC bearer token is acquired via Story 13.2 provider.** For `AuthMode = "oidc-client-credentials"`, `EmbeddingClient` resolves the `client_secret` from DAPR Secrets store using `config.ApiSecretKeyName`, then calls `IOidcTokenProvider.GetAccessTokenAsync(config.OidcTokenEndpoint, config.OidcClientId, clientSecret, config.OidcScope, ct)` and attaches `Authorization: Bearer <token>`.

4. **AC4 - Ollama response parsing returns the first embedding.** The response `{ "embeddings": [[...]] }` is parsed and `embeddings[0]` is returned as `float[]`. Missing `embeddings`, an empty array, a null first vector, invalid JSON, or a vector length that differs from `config.Dimensions` throws `EmbeddingApiException` with a clear message containing the expected and actual dimension counts when applicable.

5. **AC5 - 401/403 retry invalidates OIDC token exactly once.** If the first Ollama response is 401 or 403, `EmbeddingClient` calls `IOidcTokenProvider.InvalidateAndRefreshAsync(...)` exactly once, retries the same Ollama request once with the refreshed token, and returns the retry result when successful. If the retry also returns 401/403, throw `EmbeddingApiException`; do not loop.

6. **AC6 - Secret and token redaction is preserved.** Unit tests prove `client_secret` and bearer `access_token` do not appear in exception messages, response previews, captured logs, or assertion-facing diagnostics. The request body may contain the input text because it is sent to Ollama, but production Info+ logs must not include the full text.

7. **AC7 - Unsupported providers fail defensively.** If dispatch sees a provider other than `google` or `ollama`, throw `NotSupportedException` or `ArgumentException` with a message listing the supported providers. This is defense in depth; `EmbeddingProviderDefaults.Validate` should normally reject unsupported values first.

8. **AC8 - Colon-preserving provider/model parse is pinned.** Add a focused contract test for persisted embedding-provider strings using first-colon splitting only: `ollama:qwen3-embedding:4b` parses to provider `ollama` and model `qwen3-embedding:4b`. The test must fail if code uses `Split(':')` and drops the `:4b` tag.

9. **AC9 - API-key mode is explicit.** If Story 13.4 lands `AuthMode = "api-key"` support for Ollama, this story may support it only by sending a provider-appropriate auth header documented in 13.4. If no committed contract exists, fail fast for Ollama `api-key` mode with an actionable exception instead of guessing.

10. **AC10 - Tests cover the new Ollama path.** `EmbeddingClientTests` adds coverage for: Ollama URL/body shape, bearer header injection, successful response parsing, dimension mismatch, malformed response, unsupported provider, 401 invalidate-and-retry success, 401/403 retry failure, and redaction. Use xUnit, Shouldly, NSubstitute, and local scripted `DelegatingHandler` patterns already present in the test project.

11. **AC11 - Sibling story scopes remain untouched.** Do not add or modify `TenantEmbeddingConfig` fields in this story (13.4), do not implement or refactor `OidcTokenProvider` (13.2), do not change tenant actor surfaces (13.5), do not add migration tooling (13.6), and do not add docs/AppHost/integration wiring (13.7) unless a prerequisite story's committed API requires a tiny compile fix that is explicitly recorded.

## Tasks / Subtasks

- [ ] Task 0 - Verify prerequisites and current code surface (AC: #1, #3, #11)
  - [ ] Confirm `13-1-extend-embedding-provider-defaults-to-accept-ollama` is `done`; if still `review` or lower, stop.
  - [ ] Confirm `13-2-implement-oidc-token-provider` is `done` and inspect the actual `IOidcTokenProvider` method names, cancellation-token behavior, and exception type.
  - [ ] Confirm `13-4-extend-tenant-embedding-config-with-additive-oidc-fields` is `done` and inspect the actual `TenantEmbeddingConfig` property names/defaults.
  - [ ] Read `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` completely before editing. Preserve the fake-embedding branch, DAPR secret cache, rate-limit handling, timeout behavior, and Google retry behavior.
  - [ ] Read `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` completely and reuse its scripted `DelegatingHandler`, NSubstitute, and Shouldly style.

- [ ] Task 1 - Add dispatch helpers and parser contract (AC: #2, #7, #8)
  - [ ] Add a small helper for provider dispatch that uses `string.Equals(..., StringComparison.OrdinalIgnoreCase)` or a case-insensitive switch against `EmbeddingProviderDefaults.GoogleProviderName` and `OllamaProviderName`.
  - [ ] Add a first-colon parser helper only where the current code needs to parse persisted `{provider}:{model}` values. Do not use `Split(':')` without a maximum count.
  - [ ] Add tests that pin `google:gemini-embedding-001` and `ollama:qwen3-embedding:4b`; the Ollama test must preserve the model tag.
  - [ ] Ensure unsupported-provider messages list both supported provider names.

- [ ] Task 2 - Implement Ollama request building and auth (AC: #2, #3, #5, #6, #9)
  - [ ] Build the Ollama endpoint from `config.BaseUrl` and `/api/embed` using `Uri` or equivalent path-safe joining; reject relative or blank `BaseUrl` if validation has not already done so.
  - [ ] Resolve the client secret through the existing DAPR secret path with `config.ApiSecretKeyName`; keep the secret cache keyed by secret name only unless Story 13.2/13.4 records a different requirement.
  - [ ] For `AuthMode = "oidc-client-credentials"`, call `IOidcTokenProvider.GetAccessTokenAsync(...)` and attach the returned token via `request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token)`.
  - [ ] Send `{ model = config.Model, input = text }` for Ollama. Do not include Google-only `content.parts` or `output_dimensionality` in the Ollama body.
  - [ ] On Ollama 401/403, call `InvalidateAndRefreshAsync(...)` once, rebuild the request with the fresh token, and retry once.
  - [ ] Do not log `clientSecret`, bearer token, full request JSON, or full input text at Info+.

- [ ] Task 3 - Split response parsing without regressing Google (AC: #1, #4)
  - [ ] Keep Google parsing of `embedding.values` intact.
  - [ ] Add Ollama parsing of `embeddings[0]` with clear malformed-response errors for missing array, empty array, non-array first item, null deserialization, and invalid JSON.
  - [ ] Keep 429 mapping to `EmbeddingRateLimitException` for both providers unless a committed design says Ollama rate limits should differ.
  - [ ] Update dimension-mismatch wording so it is provider-neutral or explicitly names the configured provider; do not leave a Google-only message on the Ollama path.

- [ ] Task 4 - Add focused tests (AC: #1-#10)
  - [ ] Add or extend `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` rather than creating a parallel style unless the existing file becomes too large to read.
  - [ ] Add `GenerateAsync_Ollama_SendsNativeRequestWithBearerToken`.
  - [ ] Add `GenerateAsync_Ollama_SuccessfulResponse_ReturnsVectorWithConfiguredDimensions`.
  - [ ] Add `GenerateAsync_Ollama_WrongDimensionCount_ThrowsEmbeddingApiException`.
  - [ ] Add `GenerateAsync_Ollama_MalformedResponse_ThrowsEmbeddingApiException`.
  - [ ] Add `GenerateAsync_Ollama_Unauthorized_InvalidatesTokenAndRetriesOnce`.
  - [ ] Add `GenerateAsync_Ollama_UnauthorizedTwice_ThrowsEmbeddingApiException`.
  - [ ] Add `GenerateAsync_UnsupportedProvider_ListsSupportedProviders`.
  - [ ] Add `ParseEmbeddingProvider_PreservesOllamaModelTag`.
  - [ ] Add redaction assertions for `client_secret` and bearer token in exception/log surfaces that the implementation exposes.

- [ ] Task 5 - Validate and record completion (AC: #10, #11)
  - [ ] Run focused tests for `EmbeddingClientTests`.
  - [ ] Run the relevant ingestion test slice if local SDK constraints allow it.
  - [ ] Run `dotnet build Hexalith.Memories.slnx` if local SDK constraints allow it.
  - [ ] Record exact commands and outcomes in the Dev Agent Record. If the SDK pin in `global.json` blocks validation, record the exact SDK error and do not claim green tests.

## Dev Notes

### Current Implementation State

- `EmbeddingClient.GenerateAsync(...)` currently validates `TenantEmbeddingConfig`, returns deterministic fake vectors when `Memories:Testing:UseFakeEmbedding` is true, resolves a DAPR secret via `GetApiKeyAsync(...)`, builds only the Google endpoint, sends `x-goog-api-key`, retries once on 401/403 by evicting the secret cache, and parses `embedding.values`.
- `EmbeddingClient.BuildEndpointUrl(...)` currently supports only `EmbeddingProviderDefaults.GoogleProviderName` and throws for everything else.
- `EmbeddingClient.SendEmbeddingRequestAsync(...)` currently assumes Google auth and always adds `x-goog-api-key`.
- `EmbeddingClient.ParseEmbeddingResponse(...)` currently assumes the Google response shape and its dimension-mismatch message says "Google API may have returned..."; this wording must not be reused for Ollama errors.
- `TenantEmbeddingConfig` in the current repository still has only the original seven properties. Story 13.4 is responsible for adding `BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, and `OidcScope`; do not add them here.
- `EmbeddingProviderDefaults` already contains `GoogleProviderName`, `OllamaProviderName`, `GoogleModelName`, `OllamaModelName`, `Google()`, `Ollama()`, colon-enabled model validation, and per-provider rate-limit ceilings after Story 13.1 implementation, but Story 13.1 is currently recorded as `review`; do not implement 13.3 until review closes to `done`.
- `IOidcTokenProvider` does not exist in the current repository until Story 13.2 is implemented. This story consumes that API; it does not define a second token acquisition abstraction.

### File Scope

**Expected edited files:**

- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs`

**Possible edited files only if prerequisite APIs require it:**

- `src/Hexalith.Memories.Server/Program.cs` only if constructor injection for `EmbeddingClient` changes and DI must supply `IOidcTokenProvider`.

**Do not edit in this story:**

- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` (Story 13.4)
- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs` and `IOidcTokenProvider.cs` (Story 13.2)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` except for a tiny compile fix caused by the committed prerequisite surface
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` (Story 13.5)
- `src/Hexalith.Memories.AppHost/Program.cs` (Story 13.7)
- `docs/operations/embedding-providers.md` (Story 13.7)
- vector migration tooling or index naming code (Story 13.6)

### Implementation Guidance

- Prefer a small provider-specific internal flow over a large strategy abstraction. There are only two providers in scope.
- Keep the public method name `GenerateAsync(...)` unless the current code already changed in a prerequisite story. The epic text says `GenerateEmbeddingAsync(...)`, but the actual implementation today is `GenerateAsync(...)`.
- Continue using `IHttpClientFactory.CreateClient("EmbeddingClient")` unless a prerequisite story changed the registration. Do not create a raw `new HttpClient()` in production code.
- Build a fresh `HttpRequestMessage` for the retry path. `HttpRequestMessage` and its content cannot be safely re-sent.
- Use `System.Text.Json`; do not introduce Newtonsoft.Json.
- Keep cancellation tokens flowing into DAPR secret retrieval, token-provider calls, `SendAsync`, and response-body reads.
- If response-body previews are added to exceptions, cap them before logging or exposing them. The current `EmbeddingApiException` stores response bodies; avoid putting secrets or tokens into those bodies.
- The Ollama gateway is Keycloak-protected externally, but the application should not bake in `llm.tache.ai` or `auth.tache.ai`; those values come from tenant config once Story 13.4 lands.

### Security Requirements

- `ApiSecretKeyName` names the DAPR secret containing the OIDC `client_secret` in OIDC mode. The value must never appear in logs, exceptions, telemetry tags, or test failure messages.
- Bearer access tokens returned by `IOidcTokenProvider` must only be placed in the HTTP `Authorization` header. Do not cache them in `EmbeddingClient`; token caching belongs to `OidcTokenProvider`.
- Do not include full input text in Info+ logs. If debug logging is added, cap text length and record the cap in tests.
- Do not include request bodies in exception messages for Ollama auth failures; the request body contains user content.

### Testing Requirements

- Use xUnit + Shouldly + NSubstitute, matching existing `EmbeddingClientTests`.
- Reuse local scripted `DelegatingHandler` helpers for HTTP assertions; do not call a real Ollama or Keycloak instance.
- Test the exact outbound method, URI, headers, and JSON body for the Ollama success path.
- For retry tests, script two HTTP responses and assert the token provider was called first with `GetAccessTokenAsync(...)` and then exactly once with `InvalidateAndRefreshAsync(...)`.
- For Google regression, prefer keeping existing tests unchanged. Add one narrow assertion only if the refactor makes request inspection easier and does not weaken the existing tests.
- For parser tests, avoid a private-method-only test unless the parser has clear observable use. If the parser must stay private, pin the behavior through the public path that consumes persisted `{provider}:{model}` values.

### Previous Story Intelligence

- Story 13.1's party/elicitation notes explicitly moved the `ParseProvider` obligation out of 13.1 and into 13.3. The risk is real: a naive parser turns `ollama:qwen3-embedding:4b` into provider `ollama`, model `qwen3-embedding`, dropping the `:4b` tag and corrupting the configured model.
- Story 13.1 also established strict file-scope discipline. Treat sibling-story boundaries as acceptance criteria, not preferences.
- Story 13.2 is designed to own token acquisition, cache, forced invalidation, concurrency, and redaction. This story should only consume it.
- Story 13.4 owns additive config fields and validation for `BaseUrl` / `AuthMode` / OIDC metadata. If those fields are not present, this story cannot be implemented cleanly.

### Anti-Patterns to Avoid

- Do not implement a second token cache in `EmbeddingClient`.
- Do not parse provider/model with `Split(':')` unless it is limited to two pieces.
- Do not send Google request JSON to Ollama.
- Do not send `x-goog-api-key` to Ollama.
- Do not send Bearer tokens to Google.
- Do not silently fall back from Ollama to Google on unsupported auth or malformed response.
- Do not hard-code tenant/operator URLs into `EmbeddingClient`.
- Do not rework the whole embedding abstraction into a provider registry unless a prerequisite story has already introduced one.

## References

- [Source: `_bmad-output/planning-artifacts/epics.md` Epic 13 Story 13.3] - Ollama-native request shape, bearer injection, response parsing, retry, and test expectations.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-29.md` Sections 1, 2.3, 2.4] - self-hosted Ollama gateway, Keycloak protection, `EmbeddingClient` code impact, and `{provider}:{model}` colon risk.
- [Source: `_bmad-output/implementation-artifacts/13-1-extend-embedding-provider-defaults-to-accept-ollama.md`] - prior story notes that `ParseProvider` belongs in Story 13.3 and must preserve `qwen3-embedding:4b`.
- [Source: `_bmad-output/implementation-artifacts/13-2-implement-oidc-token-provider.md`] - planned token-provider API, cache, forced invalidation, exception, DI, and redaction contract.
- [Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`] - current Google-only endpoint, DAPR secret retrieval, fake embedding branch, 401/403 secret refresh, 429 mapping, and Google response parsing.
- [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs`] - existing test style and current Google behavior coverage.
- [Source: `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`] - current config shape; additive OIDC fields are not present until Story 13.4.
- [Source: `_bmad-output/planning-artifacts/architecture.md`] - persisted `EmbeddingProvider` field format and vector-index versioning context.

## Project Context Reference

The BMad persistent-facts glob did not find a Memories-local `project-context.md` during this automation run. Repository-specific story constraints, planning artifacts, and the current source files above are therefore the authoritative context for this story.

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- Story authored on 2026-05-01 by the recurring pre-dev hardening automation after preflight JSON timestamp `2026-05-01T15:45:30Z`.
- Preflight reported a working-tree cleanliness failure only. It was classified as an active-dev-story soft warning because `12-3-story-file-scope-enforcement.md` and the matching `sprint-status.yaml` entry are `in-progress`; other dirty paths in the JSON are ordinary development/tooling paths.
- No code implementation was performed in this run; this is a create-story artifact only.

### Completion Notes List

- Story created with status `ready-for-dev`.
- Sprint status updated from `backlog` to `ready-for-dev` for `13-3-extend-embedding-client-to-support-ollama`.
- Implementation is explicitly gated on Stories 13.1, 13.2, and 13.4 reaching `done`.

### File List

- `_bmad-output/implementation-artifacts/13-3-extend-embedding-client-to-support-ollama.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

| Date       | Change                                                                                                                                                                                                                                                                              | Author |
|------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|
| 2026-05-01 | Story 13.3 context created: scoped Ollama dispatch in `EmbeddingClient`, OIDC bearer token consumption, 401/403 invalidation retry, Ollama response parsing, colon-preserving `{provider}:{model}` parser contract, tests, redaction constraints, and prerequisite gates on 13.1/13.2/13.4. | Codex |
