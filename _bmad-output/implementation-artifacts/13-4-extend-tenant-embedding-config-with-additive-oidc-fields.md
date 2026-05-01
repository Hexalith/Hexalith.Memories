# Story 13.4: Extend TenantEmbeddingConfig with Additive OIDC Fields

Status: ready-for-dev

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

2. **AC2 - Historical JSON remains wire-compatible.** Existing serialized payloads that contain only `provider`, `model`, `dimensions`, `rateLimitPerMinute`, `apiSecretKeyName`, and `reindexRequired` deserialize successfully via `MemoriesJsonContext.Options`; the new nullable fields are `null` and `AuthMode` is `"api-key"`.

3. **AC3 - Source-generated JSON context remains valid.** `MemoriesJsonContext` continues to serialize and deserialize `TenantEmbeddingConfig` with camel-case property names and without reflection-only requirements. No new custom converter is introduced.

4. **AC4 - Google validation remains behaviorally unchanged.** `EmbeddingProviderDefaults.Google()` and historical Google configs without the new fields still pass `Validate(...)`. Google does not require `BaseUrl`, `OidcTokenEndpoint`, `OidcClientId`, or `OidcScope`.

5. **AC5 - Ollama defaults include OIDC-ready metadata.** `EmbeddingProviderDefaults.Ollama()` populates `BaseUrl = "https://llm.tache.ai"`, `AuthMode = "oidc-client-credentials"`, `OidcTokenEndpoint = "https://auth.tache.ai/realms/tache/protocol/openid-connect/token"`, `OidcClientId = "memories-embedding"`, and `OidcScope = "openid"` in addition to the Story 13.1 defaults.

6. **AC6 - OIDC mode validates required fields.** When `AuthMode = "oidc-client-credentials"`, validation requires non-empty `BaseUrl`, `OidcTokenEndpoint`, and `OidcClientId`; failures throw `ArgumentException` and the message names the missing field. `OidcScope` is optional.

7. **AC7 - Ollama API-key mode still needs a target URL.** When `Provider = "ollama"` and `AuthMode = "api-key"`, validation requires non-empty `BaseUrl`. This covers local-no-auth or upstream-API-key gateway deployments without guessing their auth header behavior.

8. **AC8 - Auth mode values are pinned.** The supported values are exactly `"api-key"` and `"oidc-client-credentials"` with ordinal-ignore-case comparison. Unsupported values throw `ArgumentException` whose message lists both supported values. Do not use the stale camelCase planning spelling `oidcClientCredentials` as the canonical value.

9. **AC9 - URL fields are validated as absolute HTTP(S) URLs.** Non-empty `BaseUrl` and `OidcTokenEndpoint` must parse as absolute `http` or `https` URLs. `http://localhost` is allowed for local gateway tests; relative paths and non-HTTP schemes are rejected.

10. **AC10 - Secret semantics are documented.** The XML doc for `ApiSecretKeyName` explicitly states that in OIDC mode the secret value is the OIDC `client_secret`; it is still only a secret name/reference and remains safe to expose through configuration responses.

11. **AC11 - Breaking-change detection includes Ollama BaseUrl.** `GetBreakingChangeFields(current, proposed)` still reports `provider`, `model`, and `dimensions` exactly as before. If both configs are Ollama and `BaseUrl` changes ignoring case/trailing slash normalization, it also reports `baseUrl`. Auth mode, token endpoint, client ID, and scope changes do not require vector reindexing by themselves.

12. **AC12 - Sibling story scopes remain untouched.** This story does not implement `IOidcTokenProvider`, does not modify `EmbeddingClient`, does not change `TenantConfigurationActor` storage behavior beyond accepting the expanded record, does not add migration tooling, and does not add AppHost/docs/integration-test wiring.

## Tasks / Subtasks

- [ ] Task 0 - Verify current state and prerequisite boundaries (AC: #4, #12)
  - [ ] Confirm Story 13.1 is `done` before editing code. If it is still `review` or lower, stop and report the prerequisite blocker.
  - [ ] Read `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` completely before editing.
  - [ ] Read `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` completely; preserve the Google validation path and the Story 13.1 Ollama provider/model constants.
  - [ ] Read `tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs` and append tests in the existing style.
  - [ ] Inspect the existing endpoint exposure points (`GET/PUT /api/tenants/{tenantId}/embedding-config` and `TenantConfigurationView`) so tests pin the fact that `ApiSecretKeyName` remains a reference, not a secret value.

- [ ] Task 1 - Extend the contract record additively (AC: #1, #2, #3, #10)
  - [ ] Add `BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, and `OidcScope` to `TenantEmbeddingConfig`.
  - [ ] Do not mark the new fields `required`; historical JSON must not fail deserialization.
  - [ ] Initialize `AuthMode` to `"api-key"` on the property so constructor/default deserialization behavior is stable.
  - [ ] Keep the existing properties and JSON names unchanged.
  - [ ] Update XML docs, especially `ApiSecretKeyName`, to cover both API-key and OIDC `client_secret` semantics.
  - [ ] Do not add a custom `JsonConverter`; the current `MemoriesJsonContext` source-generation registration should remain enough.

- [ ] Task 2 - Update validation and defaults (AC: #4-#9, #11)
  - [ ] Add or reuse small constants for auth modes (`api-key`, `oidc-client-credentials`) in `EmbeddingProviderDefaults`; avoid scattering magic strings.
  - [ ] Update `EmbeddingProviderDefaults.Ollama()` to populate the OIDC-ready defaults from the Sprint Change Proposal.
  - [ ] Keep `EmbeddingProviderDefaults.Google()` unchanged except for whatever the new record initializer requires implicitly.
  - [ ] Validate `AuthMode` with `StringComparison.OrdinalIgnoreCase`; reject unsupported values with a message listing both valid modes.
  - [ ] Validate `BaseUrl` only when provider/auth mode requires it, and validate it as an absolute `http` or `https` URL.
  - [ ] Validate `OidcTokenEndpoint` as an absolute `http` or `https` URL when `AuthMode = "oidc-client-credentials"`.
  - [ ] Require `OidcClientId` for OIDC mode; leave `OidcScope` optional.
  - [ ] Preserve `ApiSecretKeyNamePattern()` validation for all auth modes.
  - [ ] Extend `GetBreakingChangeFields(...)` to add `baseUrl` only for changed Ollama-to-Ollama base URLs after simple normalization (trim trailing slash and compare case-insensitively).

- [ ] Task 3 - Add focused tests (AC: #1-#11)
  - [ ] Add `RoundTrip_OllamaOidcFields_ShouldPreserveAllValues` to `TenantEmbeddingConfigSerializationTests`.
  - [ ] Add `Deserialize_LegacyGoogleJson_ShouldDefaultNewFields` to prove old payload compatibility.
  - [ ] Extend `PropertyNames_ShouldBeCamelCase` assertions to include `baseUrl`, `authMode`, `oidcTokenEndpoint`, `oidcClientId`, and `oidcScope`.
  - [ ] Add `Ollama_ShouldReturnOidcReadyDefaults` or extend the existing 13.1 defaults test to assert the new fields.
  - [ ] Add validation tests for missing `BaseUrl`, missing `OidcTokenEndpoint`, missing `OidcClientId`, unsupported `AuthMode`, relative URL rejection, and `http://localhost` acceptance.
  - [ ] Add `Validate_GoogleLegacyConfigWithoutOidcFields_ShouldNotThrow`.
  - [ ] Add `GetBreakingChangeFields_OllamaBaseUrlChanged_ShouldIncludeBaseUrl`.
  - [ ] Add `GetBreakingChangeFields_OidcMetadataChanged_ShouldNotRequireReindex`.
  - [ ] Add or update a configuration-view serialization test to assert `apiSecretKeyName` remains visible as a secret-name reference and no `client_secret` value is introduced.

- [ ] Task 4 - Validate and record completion (AC: #3, #12)
  - [ ] Run focused contract tests for `TenantEmbeddingConfigSerializationTests`.
  - [ ] Run focused server tests for `EmbeddingProviderDefaultsTests` and endpoint/config tests touched by this story.
  - [ ] Run `dotnet build Hexalith.Memories.slnx` if the local SDK allows it.
  - [ ] Record exact commands and outcomes in the Dev Agent Record. If the SDK pin in `global.json` blocks validation, record the exact SDK error and do not claim green tests.

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

### Completion Notes List

- Story created with status `ready-for-dev`.
- Sprint status updated from `backlog` to `ready-for-dev` for `13-4-extend-tenant-embedding-config-with-additive-oidc-fields`.
- Implementation is explicitly gated on Story 13.1 reaching `done`.

### File List

- `_bmad-output/implementation-artifacts/13-4-extend-tenant-embedding-config-with-additive-oidc-fields.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

| Date       | Change                                                                                                                                                                                                                                                         | Author |
|------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|
| 2026-05-01 | Story 13.4 context created: additive `TenantEmbeddingConfig` fields, legacy JSON compatibility, OIDC/URL validation, Ollama default metadata, `ApiSecretKeyName` client-secret semantics, `BaseUrl` reindex detection, tests, and sibling-story boundaries. | Codex |
