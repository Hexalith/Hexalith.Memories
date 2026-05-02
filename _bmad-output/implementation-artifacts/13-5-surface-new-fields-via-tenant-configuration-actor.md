# Story 13.5: Surface New Fields via TenantConfigurationActor

Status: ready-for-dev

**Effort estimate:** ~0.75-1.0 working day. Breakdown:

- **0.10 day - Task 0:** Verify prerequisite story status and current actor / endpoint surfaces.
- **0.20 day - Task 1:** Prove legacy actor state remains readable after the Story 13.4 additive fields.
- **0.20 day - Task 2:** Pin Ollama config write/read round-trip through `TenantConfigurationActor` and the PUT/GET embedding-config endpoint shape.
- **0.20 day - Task 3:** Pin public listing/configuration serialization so metadata fields are exposed and secret values are not.
- **0.10 day - Task 4:** Run focused actor / endpoint / contract tests and record exact outcomes.

**HARD prerequisite:** Story 13.4 must be `done` before implementation starts because this story depends on `TenantEmbeddingConfig.BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, and `OidcScope` already existing. Story 13.1 must also be `done` because actor writes call `EmbeddingProviderDefaults.Validate(...)`, including the `ollama` provider path. If either prerequisite is still `review` or lower, stop before editing code.

**SOFT prerequisite:** Story 13.5 does not require Stories 13.2 or 13.3 to be implemented. Token acquisition and Ollama HTTP dispatch can remain open while the actor surface proves that tenant configuration can carry the fields end-to-end.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

After Story 13.4 adds OIDC metadata to `TenantEmbeddingConfig`, prove the existing tenant configuration actor and HTTP configuration surfaces carry those fields without data loss:

- old Google actor state without the new fields still reads as `BaseUrl = null`, `AuthMode = "api-key"`, `OidcTokenEndpoint = null`, `OidcClientId = null`, and `OidcScope = null`;
- `SetEmbeddingConfigAsync(...)` persists a full Ollama OIDC config and `GetEmbeddingConfigAsync()` returns every new field after reactivation;
- `GET /api/tenants/{tenantId}/embedding-config` and `GET /api/tenants/{tenantId}/configuration` expose provider/model/dimensions/baseUrl/authMode/OIDC metadata plus `apiSecretKeyName` as a secret-name reference;
- no actor, endpoint, listing, serialized JSON, or test snapshot surface returns `client_secret`, `clientSecret`, or an actual OIDC client secret value.

Do not introduce a separate DTO just to hide `apiSecretKeyName`. The current public contract intentionally returns `TenantEmbeddingConfig` directly because the field is a DAPR secret name, not secret material.

## Story

As a **backend developer**,
I want `TenantConfigurationActor.GetEmbeddingConfigAsync()` and the corresponding write/listing surfaces to persist and return the new OIDC embedding config fields,
so that Ollama tenants can be provisioned, listed, and configured end-to-end through the existing tenant configuration flow without state loss or credential leakage.

## Acceptance Criteria

1. **AC1 - Legacy actor state remains readable.** Existing stored tenant embedding configs that predate `BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, and `OidcScope` deserialize through `MemoriesJsonContext.Options` and are accepted by `TenantConfigurationActor.GetEmbeddingConfigAsync()` with defaults: `BaseUrl = null`, `AuthMode = "api-key"`, `OidcTokenEndpoint = null`, `OidcClientId = null`, `OidcScope = null`.

2. **AC2 - Invalid legacy fallback behavior is unchanged.** Corrupted JSON or validation-invalid stored config still logs a warning and returns `EmbeddingProviderDefaults.Google()` without writing replacement state, emitting a migration write, or clearing `ReindexRequired` during the read path.

3. **AC3 - Ollama config round-trips through actor state.** `SetEmbeddingConfigAsync(config, forceReindex: false)` persists a valid Ollama OIDC `TenantEmbeddingConfig` including `BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, `OidcScope`, and `ApiSecretKeyName`, and a later `GetEmbeddingConfigAsync()` returns the same metadata values.

4. **AC4 - First write still clears client-supplied reindex flag.** When the first stored config is an Ollama OIDC config with `ReindexRequired = true`, the actor stores it with `ReindexRequired = false`, preserving the existing first-write behavior.

5. **AC5 - Breaking-change behavior uses the committed 13.4 contract.** Provider/model/dimensions changes still require `forceReindex`. Ollama-to-Ollama `BaseUrl` changes require `forceReindex` only when Story 13.4's `GetBreakingChangeFields(...)` reports `baseUrl` after simple normalization: trim whitespace, trim trailing `/`, then compare with `StringComparison.OrdinalIgnoreCase`. `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, `ApiSecretKeyName`, and `OidcScope` changes do not require reindex by themselves.

6. **AC6 - PUT/GET embedding-config surfaces carry new fields.** A valid `PUT /api/tenants/{tenantId}/embedding-config` body containing the Ollama fields is validated by the committed Story 13.4 rules, passed to `SetEmbeddingConfigAsync(...)`, and the `200 OK` response includes the same `baseUrl`, `authMode`, `oidcTokenEndpoint`, `oidcClientId`, `oidcScope`, `apiSecretKeyName`, provider, model, dimensions, rate limit, and reindex flag.

7. **AC7 - Configuration listing exposes metadata but not secrets.** `TenantConfigurationView.EmbeddingConfig` serializes the full embedded `TenantEmbeddingConfig`, including `baseUrl`, `authMode`, `oidcTokenEndpoint`, `oidcClientId`, `oidcScope`, and `apiSecretKeyName` as a secret-name reference. Tests prove `client_secret`, `clientSecret`, and a sample secret value such as `super-secret-client-secret` are not present in the JSON.

8. **AC8 - No duplicate projection is introduced.** Do not create a parallel `TenantEmbeddingConfigView`, anonymous response object, or manual field copy unless a committed prerequisite makes direct serialization impossible. The existing `TenantConfigurationView` embeds the full `TenantEmbeddingConfig` by design, and this story should preserve that single contract source.

9. **AC9 - Provisioning workflow remains dimension-driven only.** Do not broaden `TenantProvisioningInput` or `TenantProvisioningWorkflow` to accept OIDC fields in this story. Provisioning currently resolves vector dimensions from `TenantConfigurationActor`; full operator defaults/AppHost/docs belong to Story 13.7, and migration orchestration belongs to Story 13.6.

10. **AC10 - Sibling story scopes remain untouched.** This story does not implement `IOidcTokenProvider`, does not modify `EmbeddingClient`, does not add AppHost/appsettings/docs wiring, does not create vector migration tooling, and does not change `TenantEmbeddingConfig` field definitions except for tiny compile fixes caused by the already-committed Story 13.4 surface.

## Tasks / Subtasks

- [ ] Task 0 - Verify prerequisites and current surfaces (AC: #1-#10)
  - [ ] Confirm `13-1-extend-embedding-provider-defaults-to-accept-ollama` is `done`; if still `review` or lower, stop.
  - [ ] Confirm `13-4-extend-tenant-embedding-config-with-additive-oidc-fields` is `done`; if still `review` or lower, stop.
  - [ ] Read `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` completely. Preserve `StateName = "embeddingConfig"`, validation-before-store, fallback-to-Google behavior, and first-write `ReindexRequired = false` behavior.
  - [ ] Read `src/Hexalith.Memories.Server/Actors/ITenantConfigurationActor.cs`; do not change the actor API unless a committed prerequisite requires it.
  - [ ] Read the embedding-config minimal API delegates in `src/Hexalith.Memories.Server/Program.cs` before changing endpoint tests.
  - [ ] Read `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs` and `src/Hexalith.Memories.Contracts/V1/TenantConfigurationView.cs`; the listing surface already embeds `TenantEmbeddingConfig` directly.

- [ ] Task 1 - Add actor state migration and round-trip tests (AC: #1-#5)
  - [ ] In `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`, add a legacy JSON test that deserializes a pre-13.4 Google payload via `MemoriesJsonContext.Options`, feeds it through the mock state manager, and asserts the new fields have 13.4 defaults.
  - [ ] Add `GetEmbeddingConfigAsync_LegacyState_ShouldNotWriteReplacementState` to prove read migration is non-destructive.
  - [ ] Add an Ollama helper config using the actual Story 13.4 field names/defaults and `EmbeddingProviderDefaults.Ollama()` as the base.
  - [ ] Add `SetEmbeddingConfigAsync_OllamaOidcConfig_ShouldPersistAllMetadataFields`.
  - [ ] Add `GetEmbeddingConfigAsync_OllamaOidcState_ShouldReturnAllMetadataFields`.
  - [ ] Add or update a breaking-change test for Ollama `BaseUrl` to match Story 13.4's `GetBreakingChangeFields(...)` contract: whitespace trim, trailing-slash trim, ordinal-ignore-case comparison, and no broader URI canonicalization.
  - [ ] Add or update a test proving `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, `ApiSecretKeyName`, and `OidcScope` changes alone do not force reindex.
  - [ ] Keep the existing corrupt-state and invalid-state fallback tests passing without changing their intent.
  - [ ] Extend corrupt-state and invalid-state fallback tests to assert no repaired state is written and no reindex flag is cleared during read fallback.

- [ ] Task 2 - Pin embedding-config endpoint serialization (AC: #6, #10)
  - [ ] Extend `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs` with a conflict/response serialization test using an Ollama OIDC config.
  - [ ] If a direct handler extraction already exists for `PUT /embedding-config`, use it. If not, keep the test at contract/response-shape level and do not refactor `Program.cs` just for testability unless the diff stays small.
  - [ ] Assert camel-case JSON names: `baseUrl`, `authMode`, `oidcTokenEndpoint`, `oidcClientId`, `oidcScope`, and `apiSecretKeyName`.
  - [ ] Assert the response body exposes the secret name, for example `memories-embedding-client-secret`, and does not expose `client_secret`, `clientSecret`, or a sample secret value.

- [ ] Task 3 - Pin tenant configuration/listing surface (AC: #7, #8)
  - [ ] Extend `TenantConfigurationEndpointTests.TenantConfigurationView_EmbedsFullEmbeddingConfig_NotProjected` or add a focused sibling test for an Ollama OIDC config.
  - [ ] Assert `TenantConfigurationView` still serializes the embedded `TenantEmbeddingConfig` directly with every OIDC metadata field.
  - [ ] Assert no `client_secret`, `clientSecret`, or sample raw secret string appears in serialized JSON.
  - [ ] Do not add a masking layer for `apiSecretKeyName`; document in the test comment that the field is safe because it is a secret-name reference.

- [ ] Task 4 - Validate and record completion (AC: #1-#10)
  - [ ] Run focused actor tests: `TenantConfigurationActorTests`.
  - [ ] Run focused endpoint tests: `TenantEmbeddingConfigEndpointTests` and `TenantConfigurationEndpointTests`.
  - [ ] Run focused contract serialization tests for `TenantEmbeddingConfigSerializationTests` if Story 13.4 created them.
  - [ ] Run `dotnet build Hexalith.Memories.slnx` if the local SDK allows it.
  - [ ] Record exact commands and outcomes in the Dev Agent Record. If `global.json` SDK pinning blocks validation, record the exact SDK error and do not claim green tests.

## Dev Notes

### Current Implementation State

- `TenantConfigurationActor` already stores a `TenantEmbeddingConfig` value under actor state key `"embeddingConfig"` and returns it from `GetEmbeddingConfigAsync()`. There is no custom state DTO today.
- `SetEmbeddingConfigAsync(...)` validates via `EmbeddingProviderDefaults.Validate(config)`, compares `EmbeddingProviderDefaults.GetBreakingChangeFields(current, config)`, throws `EmbeddingConfigChangeException` when a breaking change lacks `forceReindex`, and stores `config with { ReindexRequired = ... }`.
- On first write, the actor stores `config with { ReindexRequired = false }` even when the caller supplied `true`. Preserve this behavior for Ollama configs.
- `TryGetStoredEmbeddingConfigAsync()` returns Google defaults on `JsonException` or `ArgumentException`. This is read-only fallback for corrupt or invalid state, not a migration write-back path, repaired-state persistence path, or reindex-flag cleanup path.
- `GET /api/tenants/{tenantId}/embedding-config` and `PUT /api/tenants/{tenantId}/embedding-config` in `Program.cs` pass `TenantEmbeddingConfig` directly. Once Story 13.4 adds fields, the minimal API binder and `MemoriesJsonContext.Options` should carry them without a separate DTO.
- `TenantEndpointHandlers.GetTenantConfigurationAsync(...)` returns `TenantConfigurationView` with `EmbeddingConfig = embeddingConfig`; `TenantConfigurationView` intentionally embeds the full config.
- `TenantProvisioningWorkflow` only receives `TenantProvisioningInput.VectorDimensions`. `Program.cs` resolves the dimension from `TenantConfigurationActor.GetEmbeddingConfigAsync()` before starting the workflow. This story should not broaden provisioning input to carry the full embedding config.

### File Scope

**Expected edited files:**

- `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs`

**Possible edited files only if prerequisite APIs require a small compile-safe adjustment:**

- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs`
- `src/Hexalith.Memories.Server/Actors/ITenantConfigurationActor.cs`
- `src/Hexalith.Memories.Contracts/V1/TenantConfigurationView.cs`

**Do not edit in this story:**

- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` except for prerequisite compile fallout already introduced by Story 13.4.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` except for prerequisite compile fallout already introduced by Story 13.4.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` (Story 13.3).
- `src/Hexalith.Memories.Server/Ingestion/IOidcTokenProvider.cs`, `OidcTokenProvider.cs`, or `OidcTokenAcquisitionException.cs` (Story 13.2).
- `src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs` and `src/Hexalith.Memories.Contracts/V1/TenantProvisioningInput.cs`.
- `src/Hexalith.Memories.AppHost/Program.cs`, `src/Hexalith.Memories.Server/appsettings.json`, and `docs/operations/embedding-providers.md` (Story 13.7).
- Vector migration tooling or Redis index naming code (Story 13.6).

### Implementation Guidance

- The likely production-code diff is zero or very small. If Story 13.4 made `TenantEmbeddingConfig` additive and source-generated serialization works, the actor and endpoints already carry the fields. Prefer tests that pin the behavior over unnecessary production refactors.
- Do not add a `TenantEmbeddingConfigView`, anonymous endpoint projection, or manual copy layer unless direct serialization fails for a concrete reason. A projection creates a new place to forget future fields.
- Do not add new `ApiSecretKeyName` required/nullable/trim behavior in this story. Consume the committed Story 13.4 validation contract exactly; if it is still unclear at implementation time, stop and record a deferred product/architecture decision instead of inventing stricter behavior.
- Use literal legacy JSON in tests to prove old actor state compatibility. Do not construct the old shape with the current record initializer because that hides missing-field deserialization problems.
- For "reactivation" round-trip tests, mock the state manager to return the same `TenantEmbeddingConfig` value that was captured from `SetStateAsync(...)`; do not spin up real DAPR actors.
- If adding endpoint tests around anonymous conflict bodies, parse with `JsonDocument` and assert field names directly. Keep the test focused on serialized contract shape.
- Use `StringComparison.OrdinalIgnoreCase` semantics already owned by `EmbeddingProviderDefaults` for any local assertions involving auth/provider values.

### Security Requirements

- `apiSecretKeyName` is safe to expose because it is a reference to a DAPR secret, not the secret value.
- Do not introduce any path that fetches DAPR secret values in actor, endpoint, listing, export, or tests for this story.
- Test data may include a sample raw secret string only to assert it is absent from serialized output; also assert `client_secret` and `clientSecret` are absent so no accidental raw-secret field shape appears.
- Do not log `OidcClientId` together with a raw secret. The actor should not log successful config contents at all.

### Testing Requirements

- Use xUnit + Shouldly + NSubstitute, matching the current actor and endpoint tests.
- Keep tests deterministic and in-memory; no Docker, real DAPR sidecar, Aspire fixture, Redis, Keycloak, or Ollama instance is needed.
- Serialization assertions must use `MemoriesJsonContext.Options`.
- Preserve the existing tests for Google defaults, invalid stored config fallback, corrupt state fallback, and rate-limit-only non-breaking changes.
- Focused test names should follow existing conventions, for example `SetEmbeddingConfigAsync_OllamaOidcConfig_ShouldPersistAllMetadataFields`.

### Previous Story Intelligence

- Story 13.1 added `ollama` provider validation and explicitly deferred `TenantEmbeddingConfig` OIDC fields to Story 13.4 and actor surface work to Story 13.5.
- Story 13.4 owns the field definitions, validation, `ApiSecretKeyName` client-secret semantics, and `BaseUrl` breaking-change detection. Story 13.5 should consume that committed surface instead of redefining it.
- Story 13.2 owns token acquisition and redaction. Story 13.5 should not call or mock token-provider behavior.
- Story 13.3 owns `EmbeddingClient` dispatch and 401/403 retry. This actor story only ensures config can reach that client later.
- Story 12.3/12.4/12.5 reinforced strict file-scope discipline: if this story can be completed by tests alone, do not touch runtime source for symmetry.

### Anti-Patterns to Avoid

- Do not make actor read migration destructive by writing defaulted state back during `GetEmbeddingConfigAsync()`.
- Do not hide or remove `apiSecretKeyName` from public config responses.
- Do not expose, fake-store, or serialize the actual OIDC `client_secret` value.
- Do not duplicate `TenantEmbeddingConfig` into a separate view type that can drift.
- Do not broaden tenant provisioning input or workflow state in this story.
- Do not mark an Ollama config invalid in actor tests because Story 13.2/13.3 are incomplete; validation should only depend on Story 13.1/13.4 committed config rules.

## References

- [Source: `_bmad-output/planning-artifacts/epics.md` Epic 13 Story 13.5] - Actor state migration, write/read surface, listing exposure, and secret-name semantics.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-29.md` Sections 2.4 and 4.4] - `TenantConfigurationActor` file impact and Story 13.5 acceptance summary.
- [Source: `_bmad-output/implementation-artifacts/13-1-extend-embedding-provider-defaults-to-accept-ollama.md`] - Provider-defaults foundation and file-scope constraints.
- [Source: `_bmad-output/implementation-artifacts/13-4-extend-tenant-embedding-config-with-additive-oidc-fields.md`] - Required OIDC fields, defaults, validation, and `BaseUrl` breaking-change contract.
- [Source: `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs`] - Current actor read/write state behavior.
- [Source: `src/Hexalith.Memories.Server/Actors/ITenantConfigurationActor.cs`] - Current actor API surface.
- [Source: `src/Hexalith.Memories.Server/Program.cs`] - Current `GET/PUT /api/tenants/{tenantId}/embedding-config` delegates and provisioning dimension resolution.
- [Source: `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs`] - Current `GET /api/tenants/{tenantId}/configuration` handler.
- [Source: `src/Hexalith.Memories.Contracts/V1/TenantConfigurationView.cs`] - Current embedded `TenantEmbeddingConfig` listing contract.
- [Source: `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`] - Existing actor test style and state-manager substitute helpers.
- [Source: `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs`] - Existing listing/configuration serialization tests.

## Project Context Reference

The BMad persistent-facts glob found `Hexalith.Commons/_bmad-output/project-context.md` but no Memories-local `project-context.md`. Treat the Commons context as general Hexalith ecosystem guidance only. Repository-specific constraints in this story and the Memories planning artifacts take precedence.

## Party-Mode Review

- **Date/time:** 2026-05-02T14:01:41+02:00
- **Selected story key:** `13-5-surface-new-fields-via-tenant-configuration-actor`
- **Command/skill invocation used:** `/bmad-party-mode 13-5-surface-new-fields-via-tenant-configuration-actor; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- **Findings summary:** The story is ready for a bounded implementation pass, but the review found pre-dev ambiguity around exact OIDC field propagation, read-only fallback semantics, direct full-config projection, raw secret non-exposure, and `BaseUrl` reindex behavior. The agents also flagged `ApiSecretKeyName` required/nullable/trim behavior as a Story 13.4 contract dependency that Story 13.5 must consume rather than redefine.
- **Changes applied:** Tightened AC2, AC3, AC5, AC6, AC7, AC8, Task 1, Task 2, Task 3, Current Implementation State, Implementation Guidance, and Security Requirements to pin read-only corrupt/invalid fallback, exact Story 13.4 field names, simple `BaseUrl` normalization, no reindex for non-BaseUrl OIDC metadata changes, direct `TenantEmbeddingConfig` serialization, and hard negative JSON assertions for `client_secret`, `clientSecret`, and sample raw secret values.
- **Findings deferred:** Product/architecture decisions remain deferred if implementation discovers committed Story 13.4 does not clearly define `ApiSecretKeyName` required/nullable/trim behavior or if a prerequisite makes direct `TenantEmbeddingConfig` serialization impossible. Localization/accessibility/admin-UI labeling remains out of this backend contract story and should be handled by a later UI/operator-experience story if needed.
- **Final recommendation:** `ready-for-dev`

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- Story authored on 2026-05-02 by the recurring pre-dev hardening automation after preflight JSON timestamp `2026-05-02T06:51:11Z`.
- Preflight result was `pass` with `working tree cleanliness` reporting `0 dirty paths`.
- No code implementation was performed in this run; this is a create-story artifact only.

### Completion Notes List

- Story created with status `ready-for-dev`.
- Sprint status updated from `backlog` to `ready-for-dev` for `13-5-surface-new-fields-via-tenant-configuration-actor`.
- Implementation is explicitly gated on Stories 13.1 and 13.4 reaching `done`.

### File List

- `_bmad-output/implementation-artifacts/13-5-surface-new-fields-via-tenant-configuration-actor.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

| Date       | Change                                                                                                                                                                                                                                                      | Author |
|------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|
| 2026-05-02 | Party-mode review completed; clarified exact OIDC field propagation, read-only invalid fallback, direct full-config serialization, raw secret non-exposure assertions, and Story 13.4 `BaseUrl` reindex semantics while deferring any unresolved `ApiSecretKeyName` validation policy back to the prerequisite contract. | Codex |
| 2026-05-02 | Story 13.5 context created: actor state migration, Ollama config round-trip, embedding-config and configuration-view serialization, secret-name exposure without secret-value leakage, and strict boundaries against token provider, client dispatch, docs, provisioning, and migration scopes. | Codex |
