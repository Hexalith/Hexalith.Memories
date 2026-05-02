# Story 13.4: Extend TenantEmbeddingConfig with Additive OIDC Fields

Status: done

**Effort estimate:** ~0.75-1.0 working day. Breakdown:

- **0.10 day - Task 0:** Verify Story 13.1 surface, current contract serialization, current validation, and endpoint exposure points.
- **0.25 day - Task 1:** Extend `TenantEmbeddingConfig` with additive OIDC fields and backward-compatible defaults.
- **0.20 day - Task 2:** Tighten `EmbeddingProviderDefaults.Validate(...)`, `Ollama()` defaults, and `GetBreakingChangeFields(...)`.
- **0.20 day - Task 3:** Add focused contract/server tests for legacy JSON, camel-case wire shape, OIDC required fields, Ollama `api-key` mode, and `BaseUrl` breaking-change behavior.
- **0.10 day - Task 4:** Run focused tests/build slice and record exact outcomes.

**HARD prerequisite:** Story 13.1 must be `done` before implementation starts because this story extends the validation and Ollama default surface introduced there. This story may remain `ready-for-dev` to maintain the buffer, but dev work must stop while `13-1-extend-embedding-provider-defaults-to-accept-ollama` is still `review` or lower.

**SOFT prerequisite:** Keep Stories 13.2 and 13.3 open while implementing. Story 13.4 defines the config fields they consume, but it must not implement token acquisition or embedding-client dispatch.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

Add five non-breaking properties to `TenantEmbeddingConfig`:

```csharp
public string? BaseUrl { get; init; }
public string AuthMode { get; init; } = "api-key";
public string? OidcTokenEndpoint { get; init; }
public string? OidcClientId { get; init; }
public string? OidcScope { get; init; }
```

Historical tenant JSON without these fields must still deserialize via `MemoriesJsonContext.Options`; `AuthMode` must default to `"api-key"` and nullable fields must default to `null`. Existing Google tenants continue to validate without reprovisioning.

For Ollama tenants, validation becomes conditional:

- `Provider = "ollama"` always requires a non-empty absolute `BaseUrl`.
- `AuthMode = "oidc-client-credentials"` requires non-empty absolute `BaseUrl`, non-empty absolute `OidcTokenEndpoint`, and non-empty `OidcClientId`; `OidcScope` remains optional.
- `ApiSecretKeyName` still names a DAPR secret. In OIDC mode it names the secret that stores the OIDC `client_secret`, not an API key value.
- `GetBreakingChangeFields(...)` keeps `provider`, `model`, and `dimensions` behavior unchanged, and additionally reports `baseUrl` when two Ollama configs point at different base URLs.

Do not switch unconfigured tenants from Google to Ollama in this story. The active default-provider rollout happens only after Epic 13 proves the full Ollama path.

## Story

As a **backend developer**,
I want `TenantEmbeddingConfig` extended with non-breaking optional fields for base URL, auth mode, and OIDC client metadata,
so that Ollama tenants can carry the configuration required by the self-hosted Keycloak-protected gateway while existing Google tenants continue to deserialize and validate without re-provisioning.

## Acceptance Criteria

1. **AC1 - Additive config fields are exposed.** `TenantEmbeddingConfig` exposes `string? BaseUrl`, `string AuthMode = "api-key"`, `string? OidcTokenEndpoint`, `string? OidcClientId`, and `string? OidcScope` in addition to the existing fields.

2. **AC2 - Historical JSON remains wire-compatible.** Existing serialized payloads that contain only `provider`, `model`, `dimensions`, `rateLimitPerMinute`, `apiSecretKeyName`, and `reindexRequired` deserialize successfully via `MemoriesJsonContext.Options`; the new nullable fields are `null` and missing `AuthMode` defaults to `"api-key"`. Explicit `null`, empty, or whitespace `AuthMode` values are invalid during validation and must not be treated as legacy defaults.

3. **AC3 - Source-generated JSON context remains valid.** `MemoriesJsonContext` continues to serialize and deserialize `TenantEmbeddingConfig` with camel-case property names and without reflection-only requirements. No new custom converter is introduced.

4. **AC4 - Google validation remains behaviorally unchanged.** `EmbeddingProviderDefaults.Google()` and historical Google configs without the new fields still pass `Validate(...)`. Google does not require `BaseUrl`, `OidcTokenEndpoint`, `OidcClientId`, or `OidcScope`; populated OIDC metadata on non-Ollama providers is configuration metadata only in this story and must not enable token acquisition or dispatch behavior.

5. **AC5 - Ollama defaults include OIDC-ready metadata.** `EmbeddingProviderDefaults.Ollama()` populates `BaseUrl = "https://llm.tache.ai"`, `AuthMode = "oidc-client-credentials"`, `OidcTokenEndpoint = "https://auth.tache.ai/realms/tache/protocol/openid-connect/token"`, `OidcClientId = "memories-embedding"`, and `OidcScope = "openid"` in addition to the Story 13.1 defaults.

6. **AC6 - OIDC mode validates required fields.** When `AuthMode = "oidc-client-credentials"`, validation requires non-empty `BaseUrl`, `OidcTokenEndpoint`, and `OidcClientId`; failures throw `ArgumentException` and the message names the missing field. `OidcScope` is optional, and `ApiSecretKeyName` remains the existing secret-name reference that identifies the DAPR secret containing the client secret.

7. **AC7 - Ollama API-key mode still needs a target URL.** When `Provider = "ollama"` and `AuthMode = "api-key"`, validation requires non-empty `BaseUrl`. This covers local-no-auth or upstream-API-key gateway deployments without guessing their auth header behavior.

8. **AC8 - Auth mode values are pinned.** The supported values are exactly `"api-key"` and `"oidc-client-credentials"` with ordinal-ignore-case comparison and no trimming. Unsupported values, including `null`, empty, whitespace-only, whitespace-wrapped supported values, `api_key`, `api key`, and stale camelCase `oidcClientCredentials`, throw `ArgumentException` whose message lists both supported values.

9. **AC9 - URL fields are validated as absolute HTTP(S) URLs.** Non-empty `BaseUrl` and `OidcTokenEndpoint` must parse as absolute `http` or `https` URLs. `http://localhost` is allowed for local gateway tests; relative paths and non-HTTP schemes are rejected.

10. **AC10 - Secret semantics are documented.** The XML doc for `ApiSecretKeyName` explicitly states that in OIDC mode the secret value is the OIDC `client_secret`; it is still only a secret name/reference and remains safe to expose through configuration responses.

11. **AC11 - Breaking-change detection includes Ollama BaseUrl.** `GetBreakingChangeFields(current, proposed)` still reports `provider`, `model`, and `dimensions` exactly as before. If both configs are Ollama and `BaseUrl` changes after simple string normalization (trim whitespace, trim trailing `/`, and compare the resulting string with `StringComparison.OrdinalIgnoreCase`), it also reports `baseUrl`. Do not use `Uri` canonicalization, DNS resolution, network calls, or path rewriting. Auth mode, token endpoint, client ID, secret key name, and scope changes do not require vector reindexing by themselves.

12. **AC12 - Sibling story scopes remain untouched.** This story does not implement `IOidcTokenProvider`, does not modify `EmbeddingClient`, does not change `TenantConfigurationActor` storage behavior beyond accepting the expanded record, does not add migration tooling, and does not add AppHost/docs/integration-test wiring.

## Tasks / Subtasks

- [x] Task 0 - Verify current state and prerequisite boundaries (AC: #4, #12)
  - [x] Confirm Story 13.1 is `done` before editing code. If it is still `review` or lower, stop and report the prerequisite blocker.
  - [x] Read `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` completely before editing.
  - [x] Read `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` completely; preserve the Google validation path and the Story 13.1 Ollama provider/model constants.
  - [x] Read `tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs` and append tests in the existing style.
  - [x] Inspect the existing endpoint exposure points (`GET/PUT /api/tenants/{tenantId}/embedding-config` and `TenantConfigurationView`) so tests pin the fact that `ApiSecretKeyName` remains a reference, not a secret value.

- [x] Task 1 - Extend the contract record additively (AC: #1, #2, #3, #10)
  - [x] Add `BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, and `OidcScope` to `TenantEmbeddingConfig`.
  - [x] Do not mark the new fields `required`; historical JSON must not fail deserialization.
  - [x] Initialize `AuthMode` to `"api-key"` on the property so constructor/default deserialization behavior is stable.
  - [x] Add validation coverage proving missing `AuthMode` defaults to `"api-key"` while explicit `null`, empty, whitespace-only, and whitespace-wrapped values fail validation.
  - [x] Keep the existing properties and JSON names unchanged.
  - [x] Update XML docs, especially `ApiSecretKeyName`, to cover both API-key and OIDC `client_secret` semantics.
  - [x] Do not add a custom `JsonConverter`; the current `MemoriesJsonContext` source-generation registration should remain enough.

- [x] Task 2 - Update validation and defaults (AC: #4-#9, #11)
  - [x] Add or reuse small constants for auth modes (`api-key`, `oidc-client-credentials`) in `EmbeddingProviderDefaults`; avoid scattering magic strings.
  - [x] Update `EmbeddingProviderDefaults.Ollama()` to populate the OIDC-ready defaults from the Sprint Change Proposal.
  - [x] Keep `EmbeddingProviderDefaults.Google()` unchanged except for whatever the new record initializer requires implicitly.
  - [x] Validate `AuthMode` with `StringComparison.OrdinalIgnoreCase`; reject unsupported values with a message listing both valid modes.
  - [x] Validate `BaseUrl` only when provider/auth mode requires it, and validate it as an absolute `http` or `https` URL.
  - [x] Validate `OidcTokenEndpoint` as an absolute `http` or `https` URL when `AuthMode = "oidc-client-credentials"`.
  - [x] Require `OidcClientId` for OIDC mode; leave `OidcScope` optional.
  - [x] Do not require or interpret OIDC metadata when `AuthMode = "api-key"` or when validating historical Google configs; this story stores metadata only and does not implement authentication behavior.
  - [x] Preserve `ApiSecretKeyNamePattern()` validation for all auth modes.
  - [x] Extend `GetBreakingChangeFields(...)` to add `baseUrl` only for changed Ollama-to-Ollama base URLs after simple normalization (trim whitespace, trim trailing slash, and compare case-insensitively) without broader URI canonicalization.

- [x] Task 3 - Add focused tests (AC: #1-#11)
  - [x] Add `RoundTrip_OllamaOidcFields_ShouldPreserveAllValues` to `TenantEmbeddingConfigSerializationTests`.
  - [x] Add `Deserialize_LegacyGoogleJson_ShouldDefaultNewFields` to prove old payload compatibility.
  - [x] Extend `PropertyNames_ShouldBeCamelCase` assertions to include `baseUrl`, `authMode`, `oidcTokenEndpoint`, `oidcClientId`, and `oidcScope`.
  - [x] Add `Ollama_ShouldReturnOidcReadyDefaults` or extend the existing 13.1 defaults test to assert the new fields.
  - [x] Add validation tests for missing `BaseUrl`, missing `OidcTokenEndpoint`, missing `OidcClientId`, unsupported `AuthMode`, relative URL rejection, and `http://localhost` acceptance.
  - [x] Add per-field URL tests covering `http`, `https`, `localhost`, `127.0.0.1`, relative paths, scheme-less values, malformed values, and non-HTTP schemes for every URL field that is validated in this story.
  - [x] Add `Validate_GoogleLegacyConfigWithoutOidcFields_ShouldNotThrow`.
  - [x] Add `GetBreakingChangeFields_OllamaBaseUrlChanged_ShouldIncludeBaseUrl`.
  - [x] Add `GetBreakingChangeFields_OidcMetadataChanged_ShouldNotRequireReindex`, covering auth mode, token endpoint, client ID, secret key name, and scope changes.
  - [x] Add or update a configuration-view serialization test that inspects raw JSON and asserts `apiSecretKeyName` remains visible as a secret-name reference while `client_secret`, `clientSecret`, and resolved secret values are absent.

- [x] Task 4 - Validate and record completion (AC: #3, #12)
  - [x] Run focused contract tests for `TenantEmbeddingConfigSerializationTests`.
  - [x] Run focused server tests for `EmbeddingProviderDefaultsTests` and endpoint/config tests touched by this story.
  - [x] Run `dotnet build Hexalith.Memories.slnx` if the local SDK allows it.
  - [x] Record exact commands and outcomes in the Dev Agent Record. If the SDK pin in `global.json` blocks validation, record the exact SDK error and do not claim green tests.

## Dev Notes

### Current Implementation State

- `TenantEmbeddingConfig` currently has only `Provider`, `Model`, `Dimensions`, `RateLimitPerMinute`, `ApiSecretKeyName`, and `ReindexRequired`.
- `MemoriesJsonContext` already includes `[JsonSerializable(typeof(TenantEmbeddingConfig))]` and combines the generated context with `DefaultJsonTypeInfoResolver`. Microsoft Learn's System.Text.Json source-generation docs confirm public properties on a `[JsonSerializable]` type are the intended source-generation path; no converter is needed for this additive record change.
- `TenantConfigurationActor` stores and returns `TenantEmbeddingConfig` directly. Its `TryGetStoredEmbeddingConfigAsync()` validates stored state and falls back to Google defaults on invalid/corrupt state. This story should make historical state valid, not trigger fallback.
- `GET /api/tenants/{tenantId}/embedding-config` and `TenantConfigurationView.EmbeddingConfig` expose the full config object. That is acceptable because `ApiSecretKeyName` is a reference to a DAPR secret name, not the secret value.
- `EmbeddingProviderDefaults.Ollama()` already exists from Story 13.1, but current code does not yet include `BaseUrl`, `AuthMode`, or OIDC metadata because the record lacks those fields.
- `TenantProvisioningInput` currently carries only `VectorDimensions`; Story 13.5/13.7 can broaden provisioning and docs. Do not expand provisioning workflow input in this story.

### File Scope

**Expected edited files:**

- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`

**Possible edited files if tests need endpoint-shape coverage:**

- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs`

**Do not edit in this story:**

- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` (Story 13.3)
- `src/Hexalith.Memories.Server/Ingestion/IOidcTokenProvider.cs`, `OidcTokenProvider.cs`, `OidcTokenAcquisitionException.cs` (Story 13.2)
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` unless a compile-only constructor/record shape issue appears; behavior changes belong to Story 13.5.
- `src/Hexalith.Memories.AppHost/Program.cs` and `src/Hexalith.Memories.Server/appsettings.json` (Story 13.7/operator wiring)
- `docs/operations/embedding-providers.md` (Story 13.7)
- Vector migration tooling, Redis index naming, or reindex orchestration (Story 13.6)

### Implementation Guidance

- Keep this an additive contract change. Do not convert `TenantEmbeddingConfig` to a primary-constructor record because that would risk breaking historical JSON and source-generation assumptions.
- Prefer `public string AuthMode { get; init; } = "api-key";` rather than `required string AuthMode`. The default is the compatibility feature.
- Use helper methods for URL validation and base URL normalization only if they keep `EmbeddingProviderDefaults` readable. Avoid introducing a provider registry abstraction in this story.
- Base URL normalization for breaking-change detection can be simple: trim whitespace, trim trailing `/`, compare with `StringComparison.OrdinalIgnoreCase`. Do not resolve DNS, call the URL, or normalize paths beyond trailing slash handling.
- Keep validation errors actionable and field-specific. The tests should assert that missing-field messages name `BaseUrl`, `OidcTokenEndpoint`, or `OidcClientId`.
- Do not accept `oidcClientCredentials` as an alias unless a product decision is recorded. The rest of Epic 13 has standardized on `oidc-client-credentials`.
- Leave `OidcScope` as a string. Do not parse scopes or enforce `openid`; some Keycloak clients may not require it.
- Do not hide `ApiSecretKeyName` from configuration responses. It is still the secret-name reference operators need to troubleshoot, not the secret value.
- Treat mixed-mode configuration predictably: OIDC metadata may be present on stored config, but this story only validates and acts on it when `AuthMode = "oidc-client-credentials"`. Authentication behavior remains owned by Stories 13.2 and 13.3.

### Security Requirements

- The OIDC `client_secret` value is never stored in `TenantEmbeddingConfig`; only `ApiSecretKeyName` is stored.
- `OidcClientId`, `OidcTokenEndpoint`, `BaseUrl`, and `OidcScope` are configuration metadata, not credential material. They may be serialized through existing config views.
- Do not introduce logs, exception text, or tests that contain sample real secrets. Use placeholder names like `memories-embedding-client-secret`.
- Do not change DAPR secret-store access in this story. Secret retrieval belongs to `EmbeddingClient` / Story 13.3.

### Testing Requirements

- Use xUnit + Shouldly, matching existing contract and server test files.
- Serialization tests must use `MemoriesJsonContext.Options`, not default `JsonSerializerOptions`, because source-generation compatibility is the point of AC2/AC3.
- Legacy JSON test should be a literal JSON string that omits every new property.
- Validation tests should call `EmbeddingProviderDefaults.Validate(...)` directly; no DAPR actor or HTTP host is needed.
- Endpoint/config-view tests should serialize contracts directly or use existing extracted handlers. Do not spin up Aspire or real DAPR for this story.

### Previous Story Intelligence

- Story 13.1's review notes emphasized strict file-scope discipline. Treat "do not touch sibling story files" as an acceptance criterion.
- Story 13.1 added the `ollama` provider, the `qwen3-embedding:4b` model, 2560-dimension validation, and the higher self-hosted rate-limit ceiling. This story should extend that surface, not rework it.
- Story 13.2 owns token acquisition, in-memory token caching, forced invalidation, and redaction. This story only carries the fields required for 13.2/13.3 to receive endpoint/client metadata.
- Story 13.3 is blocked on these fields. If a dev agent tries to implement Ollama dispatch before this story lands, it will be forced to invent config fields locally; this story prevents that duplication.

### Anti-Patterns to Avoid

- Do not make `BaseUrl` required for Google tenants.
- Do not mark `AuthMode` as `required`; that breaks old payloads at compile-time/object-init boundaries and makes deserialization compatibility harder to reason about.
- Do not store the OIDC `client_secret` value in the config record.
- Do not change unconfigured tenant defaults from Google to Ollama yet.
- Do not add a custom JSON converter for simple public init properties.
- Do not treat `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, or `OidcScope` changes as vector-reindex requirements unless product changes the definition of `GetBreakingChangeFields(...)`.

## References

- [Source: `_bmad-output/planning-artifacts/epics.md` Epic 13 Story 13.4] - Additive `TenantEmbeddingConfig` fields, historical JSON compatibility, conditional validation, `ApiSecretKeyName` OIDC semantics, and `BaseUrl` breaking-change behavior.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-29.md` Sections 2.4, 4.1, 4.5] - Ollama gateway URL, Keycloak token endpoint, auth mode values, DAPR secret name, and Epic 13 config matrix.
- [Source: `_bmad-output/implementation-artifacts/13-1-extend-embedding-provider-defaults-to-accept-ollama.md`] - Previous provider-defaults foundation and file-scope constraints.
- [Source: `_bmad-output/implementation-artifacts/13-2-implement-oidc-token-provider.md`] - Planned OIDC token-provider API that will consume these config values.
- [Source: `_bmad-output/implementation-artifacts/13-3-extend-embedding-client-to-support-ollama.md`] - Downstream `EmbeddingClient` dependency on `BaseUrl`, `AuthMode`, and OIDC metadata.
- [Source: `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`] - Current record shape and XML doc location for `ApiSecretKeyName`.
- [Source: `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`] - Current provider defaults, validation, and breaking-change detection.
- [Source: `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs`] - Actor-state validation/fallback behavior that old JSON must continue to satisfy.
- [Source: Microsoft Learn, "How to use source generation in System.Text.Json", accessed 2026-05-01] - Source-generation context pattern for public serializable properties: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation

## Project Context Reference

The BMad persistent-facts glob found `Hexalith.Commons/_bmad-output/project-context.md` but no Memories-local `project-context.md`. Treat the Commons context as general Hexalith ecosystem guidance only. Repository-specific instructions in this story and in the Memories planning artifacts take precedence.

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- Story authored on 2026-05-01 by the recurring pre-dev hardening automation after preflight JSON timestamp `2026-05-01T20:49:41Z`.
- Preflight reported a working-tree cleanliness failure only, with stdout `" M Hexalith.EventStore\n"`. It was classified as a soft working-tree warning because the dirty path is outside BMAD-owned story-operation paths.
- No code implementation was performed in this run; this is a create-story artifact only.
- 2026-05-02 implementation red phase: `dotnet test tests\Hexalith.Memories.Contracts.Tests\Hexalith.Memories.Contracts.Tests.csproj --filter TenantEmbeddingConfigSerializationTests` failed as expected because `TenantEmbeddingConfig` did not yet expose `BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, or `OidcScope`. The parallel server red-phase command hit a transient compiler file lock while the contract project was also building.
- 2026-05-02 focused green phase: `dotnet test tests\Hexalith.Memories.Contracts.Tests\Hexalith.Memories.Contracts.Tests.csproj --filter TenantEmbeddingConfigSerializationTests` passed 6/6.
- 2026-05-02 focused green phase: `dotnet test tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --filter "EmbeddingProviderDefaultsTests|TenantConfigurationEndpointTests"` passed 106/106.
- 2026-05-02 build validation: `dotnet build Hexalith.Memories.slnx` succeeded with 0 warnings and 0 errors.
- 2026-05-02 regression validation: `dotnet test Hexalith.Memories.slnx --no-build` timed out after 15 minutes in the integration topology lane; the test-spawned `dotnet test`, integration runner, Server, and MCP child processes were stopped.
- 2026-05-02 project regression slices: Contracts 470/470, Server 1604/1604, CLI 335/335, EventStore 84/84, and MCP 76/76 passed with `--no-build`.
- 2026-05-02 integration relevance check: `dotnet test tests\Hexalith.Memories.IntegrationTests\Hexalith.Memories.IntegrationTests.csproj --no-build --filter FullyQualifiedName~TenantConfigurationIntegrationTests` returned 0 failed, 0 passed, 10 skipped because the project integration gating skipped the live-topology tenant configuration class.

### Completion Notes List

- Story 13.4 implementation complete and ready for review.
- Added additive `TenantEmbeddingConfig` fields for `BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, and `OidcScope`; missing legacy JSON `authMode` defaults through the source-generated JSON constructor while explicit invalid values remain validation failures.
- Added OIDC-ready Ollama defaults, pinned auth modes, conditional BaseUrl/OIDC validation, HTTP(S)-only URL checks, optional `OidcScope`, preserved Google legacy validation, and extended Ollama-to-Ollama `baseUrl` breaking-change detection.
- Added focused serialization, validation, breaking-change, and configuration-view JSON tests proving secret-name exposure remains safe and secret values are absent.
- Scope preserved: no `IOidcTokenProvider`, `EmbeddingClient`, tenant actor storage behavior, AppHost/docs, or migration tooling changes were made.
- Full solution aggregate test did not complete locally; all non-integration project test suites passed and the relevant integration class was skipped by the repository's integration gate.
- Task 0 complete: verified Story 13.1 is `done`, read the scoped contract/defaults/tests, and inspected the GET/PUT embedding-config plus `TenantConfigurationView` exposure path before implementation.
- Story created with status `ready-for-dev`.
- Sprint status updated from `backlog` to `ready-for-dev` for `13-4-extend-tenant-embedding-config-with-additive-oidc-fields`.
- Implementation is explicitly gated on Story 13.1 reaching `done`.

### File List

- `_bmad-output/implementation-artifacts/13-4-extend-tenant-embedding-config-with-additive-oidc-fields.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`

## Party-Mode Review

- **Date/time:** 2026-05-02T10:04:31Z
- **Selected story key:** `13-4-extend-tenant-embedding-config-with-additive-oidc-fields`
- **Command/skill invocation used:** `/bmad-party-mode 13-4-extend-tenant-embedding-config-with-additive-oidc-fields; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- **Findings summary:** Story scope is sound, but the review found pre-dev ambiguity around legacy `AuthMode` defaults versus invalid explicit values, conditional OIDC validation boundaries, mixed-mode metadata behavior, per-field URL validation, secret-name exposure assertions, and simple `BaseUrl` comparison semantics for reindex detection.
- **Changes applied:** Tightened AC2, AC4, AC6, AC8, AC11, Task 1, Task 2, Task 3, and Implementation Guidance to pin missing-versus-invalid `AuthMode`, avoid accidental OIDC behavior in API-key or Google validation paths, require raw JSON secret non-exposure checks, expand URL/auth-mode/reindex negative tests, and clarify simple `BaseUrl` string normalization.
- **Findings deferred:** Generic OAuth2-versus-OIDC model expansion, token acquisition behavior, EmbeddingClient dispatch, TenantConfigurationActor API/storage behavior beyond compile compatibility, AppHost/docs/operator wiring, migration tooling, and any broader provider registry abstraction remain out of scope for Stories 13.2, 13.3, 13.5, 13.6, or 13.7.
- **Final recommendation:** ready-for-dev

### Change Log

| Date       | Change                                                                                                                                                                                                                                                         | Author |
|------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|
| 2026-05-02 | Code review (adversarial 3-layer) closed 3 decisions and 3 patches: tightened `ModelNamePattern` to alnum-start regex with positive/negative tests; explicitly rejected `oidc-client-credentials` on non-Ollama providers (AC4 enforcement); tightened URL-shape validation to apply unconditionally on non-empty `BaseUrl`/`OidcTokenEndpoint` (AC9 strict); replaced JSON-ctor `string? authMode!` type lie with non-nullable `string authMode = "api-key"`; added `DescribeAuthMode` placeholder for blank-class messages; tightened endpoint test to assert exact JSON key shape. Contracts 6/6, server defaults+endpoints 136/136, full solution build 0/0. Story 13.4 review → done. | Claude |
| 2026-05-02 | Implemented additive TenantEmbeddingConfig OIDC fields, OIDC-ready Ollama defaults, conditional validation, BaseUrl reindex detection, focused tests, and moved story to review. | Codex |
| 2026-05-02 | Party-mode review completed; clarified `AuthMode` invalid-value handling, conditional OIDC validation boundaries, raw secret non-exposure tests, URL/reindex edge cases, and mixed-mode metadata scope while preserving Story 13.4 boundaries. | Codex |
| 2026-05-01 | Story 13.4 context created: additive `TenantEmbeddingConfig` fields, legacy JSON compatibility, OIDC/URL validation, Ollama default metadata, `ApiSecretKeyName` client-secret semantics, `BaseUrl` reindex detection, tests, and sibling-story boundaries. | Codex |

### Review Findings

_Adversarial code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) on 2026-05-02 — full mode, spec-anchored. Scope: 5 files, +422/-6 lines._

**Decisions resolved:**

- [x] [Review][Decision] **F1 — `ModelNamePattern` regex tightening: partial fulfillment of deferred 13.1-RV11.** Resolved by tightening regex to the deferred-suggested `^[A-Za-z0-9][A-Za-z0-9.:_-]*$` (alnum START) with a clearer error message ("Model must start with a letter or number…"). Added 9 positive `Validate_ModelNameStartsWithAlphanumeric_ShouldNotThrow` cases and 11 negative `Validate_ModelNameStartsWithPunctuation_ShouldThrow` cases. The provider→model→dim-allowlist registry and cross-pollination tests remain in deferred-work for a future dedicated story (still tracked by 13.1-RV11).
- [x] [Review][Decision] **F2 — Cross-provider OIDC: Google + `AuthMode = "oidc-client-credentials"` is implicitly accepted.** Resolved by explicitly rejecting `oidc-client-credentials` on non-Ollama providers in `Validate(...)`: `if (isOidcClientCredentials && !isOllama) throw`. AC4's "metadata-only on non-Ollama" is now enforced at the auth-mode boundary rather than left as an emergent property. Added test `Validate_GoogleWithOidcClientCredentialsAuthMode_ShouldThrow`.
- [x] [Review][Decision] **F3 — AC9 strict reading vs. AC4 conditional metadata.** Resolved by tightening implementation to validate non-empty `BaseUrl` / `OidcTokenEndpoint` shape unconditionally (new `ValidateOptionalHttpUrl(...)` helper). AC4 metadata-only stays true (no behavioral activation), but stored garbage URLs are now rejected at validation regardless of mode. Updated `Validate_GoogleWithOidcMetadata_ShouldNotRequireOidcMode` → `Validate_GoogleWithValidOidcMetadata_ShouldNotRequireOidcMode` to use a real URL; added 6 negative cases in `Validate_GoogleWithMalformedUrlMetadata_ShouldThrow`.

**Patches applied:**

- [x] [Review][Patch] **P1 — `[JsonConstructor]` + null-forgiving + parameterless ctor weakened AuthMode and `required` invariants.** [`src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`] Replaced the type lie: changed JSON ctor parameter `string? authMode = "api-key"` to non-nullable `string authMode = "api-key"` and removed the `AuthMode = authMode!` force-assignment. The JSON ctor remains (it's the only mechanism that distinguishes "missing in legacy JSON → default 'api-key'" from "explicit JSON null → kept and fails validation"), but its parameter type now matches the property type, eliminating the compiler lie. Required-string ctor parameters were already non-nullable, so JSON null on `provider`/`model`/`apiSecretKeyName` will surface as a JSON deserialization error or be caught by `Validate`'s `ThrowIfNullOrWhiteSpace`.
- [x] [Review][Patch] **P2 — Validate's AuthMode error renders empty `''` for null/whitespace inputs.** [`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`] Added `DescribeAuthMode(...)` helper rendering `<null>`, `<empty>`, `<whitespace>`, or `'value'` markers in the error message so blank-class inputs are diagnostically distinguishable. Added `Validate_BlankAuthMode_ShouldDescribeBlankClassInMessage` test.
- [x] [Review][Patch] **P3 — Test assertions on `client_secret`/`clientSecret` substrings are brittle.** [`tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs`] Tightened to assert exact JSON key shape `"\"clientSecret\":"` / `"\"client_secret\":"` so a future benign metadata-key rename does not falsely fail the test.

**Deferred (pre-existing or out of story scope):**

- [x] [Review][Defer] **W1 — `RateLimitPerMinute` boundary / arithmetic overflow concerns.** [`EmbeddingProviderDefaults.cs:147-161`] — deferred, pre-existing. Validator caps the value but downstream arithmetic on it is not audited.
- [x] [Review][Defer] **W2 — `OidcScope` whitespace-only not validated.** [`EmbeddingProviderDefaults.cs:163-181`] — deferred. Spec leaves scope optional and unvalidated; whitespace-only would surface at IdP.
- [x] [Review][Defer] **W3 — OIDC mode does not enforce `ApiSecretKeyName` distinctness/role.** [`EmbeddingProviderDefaults.cs:163-181`] — deferred. A tenant could carry over a Google API-key secret name when flipping to OIDC; out of story scope.
- [x] [Review][Defer] **W4 — No assertion that endpoint paths invoke `Validate`.** [`Hexalith.Memories.Server`] — deferred. Validator hardening is dead code if no caller invokes it on POST/PUT; cross-cutting concern, addressed by Story 13.5 / 13.7.
- [x] [Review][Defer] **W5 — URLs with userinfo (`https://user:pw@host`) are accepted.** [`EmbeddingProviderDefaults.cs:214-226`] — deferred, mirrors Story 13.2's deferred 13.2-RV5; defensive rejection should apply uniformly across providers.

**Dismissed as noise (not persisted): 9 items** — auth-mode/OIDC field changes ignored by `GetBreakingChangeFields` (AC11 intentional); `NormalizeBaseUrl` simple-trim edge cases (AC11 mandates simple normalization, no Uri canonicalization); speculative `affectedFields` double-add; hardcoded operator URLs in `Ollama()` (AC5 mandates exact values); JSON null serialization cosmetic concerns; multi-roundtrip validation UX (first-fail is conventional); test path-case sensitivity (spec-required `OrdinalIgnoreCase`); pre-existing `reindexRequired = false` ctor default; validation-ordering refactor risk (style nit).
