# Story 13.5: Surface New Fields via TenantConfigurationActor

Status: done

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

- [x] Task 0 - Verify prerequisites and current surfaces (AC: #1-#10)
  - [x] Confirm `13-1-extend-embedding-provider-defaults-to-accept-ollama` is `done`; if still `review` or lower, stop.
  - [x] Confirm `13-4-extend-tenant-embedding-config-with-additive-oidc-fields` is `done`; if still `review` or lower, stop.
  - [x] Read `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` completely. Preserve `StateName = "embeddingConfig"`, validation-before-store, fallback-to-Google behavior, and first-write `ReindexRequired = false` behavior.
  - [x] Read `src/Hexalith.Memories.Server/Actors/ITenantConfigurationActor.cs`; do not change the actor API unless a committed prerequisite requires it.
  - [x] Read the embedding-config minimal API delegates in `src/Hexalith.Memories.Server/Program.cs` before changing endpoint tests.
  - [x] Read `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs` and `src/Hexalith.Memories.Contracts/V1/TenantConfigurationView.cs`; the listing surface already embeds `TenantEmbeddingConfig` directly.

- [x] Task 1 - Add actor state migration and round-trip tests (AC: #1-#5)
  - [x] In `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`, add a legacy JSON test that deserializes a pre-13.4 Google payload via `MemoriesJsonContext.Options`, feeds it through the mock state manager, and asserts the new fields have 13.4 defaults.
  - [x] Add `GetEmbeddingConfigAsync_LegacyState_ShouldNotWriteReplacementState` to prove read migration is non-destructive.
  - [x] Add an Ollama helper config using the actual Story 13.4 field names/defaults and `EmbeddingProviderDefaults.Ollama()` as the base.
  - [x] Add `SetEmbeddingConfigAsync_OllamaOidcConfig_ShouldPersistAllMetadataFields`.
  - [x] Add `GetEmbeddingConfigAsync_OllamaOidcState_ShouldReturnAllMetadataFields`.
  - [x] Add or update a breaking-change test for Ollama `BaseUrl` to match Story 13.4's `GetBreakingChangeFields(...)` contract: whitespace trim, trailing-slash trim, ordinal-ignore-case comparison, and no broader URI canonicalization.
  - [x] Add or update a test proving `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, `ApiSecretKeyName`, and `OidcScope` changes alone do not force reindex.
  - [x] Keep the existing corrupt-state and invalid-state fallback tests passing without changing their intent.
  - [x] Extend corrupt-state and invalid-state fallback tests to assert no repaired state is written and no reindex flag is cleared during read fallback.

- [x] Task 2 - Pin embedding-config endpoint serialization (AC: #6, #10)
  - [x] Extend `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs` with a conflict/response serialization test using an Ollama OIDC config.
  - [x] If a direct handler extraction already exists for `PUT /embedding-config`, use it. If not, keep the test at contract/response-shape level and do not refactor `Program.cs` just for testability unless the diff stays small.
  - [x] Assert camel-case JSON names: `baseUrl`, `authMode`, `oidcTokenEndpoint`, `oidcClientId`, `oidcScope`, and `apiSecretKeyName`.
  - [x] Assert the response body exposes the secret name, for example `memories-embedding-client-secret`, and does not expose `client_secret`, `clientSecret`, or a sample secret value.

- [x] Task 3 - Pin tenant configuration/listing surface (AC: #7, #8)
  - [x] Extend `TenantConfigurationEndpointTests.TenantConfigurationView_EmbedsFullEmbeddingConfig_NotProjected` or add a focused sibling test for an Ollama OIDC config.
  - [x] Assert `TenantConfigurationView` still serializes the embedded `TenantEmbeddingConfig` directly with every OIDC metadata field.
  - [x] Assert no `client_secret`, `clientSecret`, or sample raw secret string appears in serialized JSON.
  - [x] Do not add a masking layer for `apiSecretKeyName`; document in the test comment that the field is safe because it is a secret-name reference.

- [x] Task 4 - Validate and record completion (AC: #1-#10)
  - [x] Run focused actor tests: `TenantConfigurationActorTests`.
  - [x] Run focused endpoint tests: `TenantEmbeddingConfigEndpointTests` and `TenantConfigurationEndpointTests`.
  - [x] Run focused contract serialization tests for `TenantEmbeddingConfigSerializationTests` if Story 13.4 created them.
  - [x] Run `dotnet build Hexalith.Memories.slnx` if the local SDK allows it.
  - [x] Record exact commands and outcomes in the Dev Agent Record. If `global.json` SDK pinning blocks validation, record the exact SDK error and do not claim green tests.

### Review Findings

Adversarial 3-layer review run 2026-05-02 (Blind Hunter, Edge Case Hunter, Acceptance Auditor). All `decision-needed` findings resolved autonomously per `feedback_review_autonomy.md`; all `patch` items below have been applied and validated (`dotnet test --filter "FullyQualifiedName~TenantConfigurationActorTests|FullyQualifiedName~TenantConfigurationEndpointTests|FullyQualifiedName~TenantEmbeddingConfigEndpointTests"` → 48/48 passed).

**Patches applied:**

- [x] [Review][Patch] AC5 normalization test bundled all three rules — split into whitespace-only / trailing-slash-only / casing-only deltas so a regression dropping any single rule is localized [`tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:174-247`].
- [x] [Review][Patch] AC5 OidcMetadataChanged bundled 5 field changes — split into `AuthModeOnlyDelta` / `OidcTokenEndpointOnlyDelta` / `OidcClientIdOnlyDelta` / `ApiSecretKeyNameOnlyDelta` / `OidcScopeOnlyDelta`; each predicate now also pins the persisted field value, and the AuthMode test exercises ordinal-ignore-case validation [`tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:249-358`].
- [x] [Review][Patch] AC5 missing positive force-reindex Ollama BaseUrl path — added `SetEmbeddingConfigAsync_OllamaBaseUrlChanged_WithForceReindex_ShouldSaveAndSetReindexRequired` so the throw and save branches are both pinned for Ollama, not only transitively via Google [`tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:174-191`].
- [x] [Review][Patch] AC7 listing test used substring matching — converted `TenantConfigurationView_EmbedsFullEmbeddingConfig_NotProjected` to `JsonDocument.GetProperty("embeddingConfig")` structural assertions so a regression that emits OIDC fields outside `embeddingConfig` (or under a wrong nesting) cannot pass [`tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs:138-176`].
- [x] [Review][Patch] AC7 raw-secret negative assertions only covered `client_secret` / `clientSecret` — added `"oidcClientSecret":` and `"oidc_client_secret":` absence on all three serialization tests so the canonical leak shape that would appear if a secret-value field were ever added to `TenantEmbeddingConfig` is now guarded [`tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs:172-176`, `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs:88-92,121-125`].

**Deferred (added to `_bmad-output/implementation-artifacts/deferred-work.md`):**

- [x] [Review][Defer] 13.5-RV1 — `Hexalith.EventStore` submodule pointer bump (`f812bfb` → `f8e8f14`) is outside Story 13.5's declared file scope; drift content verified innocuous (5 doc/story commits authored by Jerome) so accepted in-place. Process note: future feat commits should isolate ecosystem submodule bumps into separate `chore: update subproject commit reference` commits. Re-open trigger: any future feat commit that bundles a submodule pointer change without a separating commit.
- [x] [Review][Defer] 13.5-RV2 — AC6 PUT/Conflict body not pinned end-to-end through ASP.NET Core's `HttpJsonOptions` pipeline. All new tests serialize via `MemoriesJsonContext.Options` directly; production uses `Results.Ok(updatedConfig)` and `Results.Conflict(body)`. If runtime HTTP JSON options ever diverge (different naming policy / converters), tests stay green while real bodies change. Story 13.7's integration suite is the natural enforcement point.
- [x] [Review][Defer] 13.5-RV3 — No Ollama-flavored Provider/Model/Dimensions breaking-change actor tests. Existing breaking-change coverage (Model, Dimensions) is Google-flavored only; the Ollama-specific `Validate(...)` ceilings (qwen3 dimension lock at 2560, rate-limit ceiling 60_000) are exercised by `EmbeddingProviderDefaultsTests` separately. Re-open trigger: a second Ollama model lands and the dimension/provider breaking-change matrix grows.
- [x] [Review][Defer] 13.5-RV4 — Legacy `provider="ollama"` payload with missing OIDC fields not exercised by `DeserializeLegacyGoogleConfig`. Pre-13.4 actor state cannot be Ollama because the provider was added in Story 13.1; the deserialize-then-Validate fallback path for a hypothetical injected legacy Ollama payload is currently un-pinned. Re-open trigger: any operational incident where an actor state predates the current provider list.
- [x] [Review][Defer] 13.5-RV5 — Whitespace-only / empty-string `BaseUrl` legacy state behavior not pinned. `ValidateOptionalHttpUrl` early-returns on whitespace for non-Ollama providers and the empty value persists into `TenantConfigurationView`; for Ollama, validation rejects and the read path falls back to Google. Low likelihood, low impact. Re-open trigger: a tenant config audit that surfaces an empty/whitespace `BaseUrl` in the wild.
- [x] [Review][Defer] 13.5-RV6 — `SetEmbeddingConfigAsync_FirstOllamaWrite_ShouldIgnoreClientSuppliedReindexFlag` passes both signals (`forceReindex: true` and `newConfig.ReindexRequired = true`), so a regression that respects only one of the two while ignoring the other would still pass. Mirrors the pre-existing Google `FirstWrite_ShouldIgnoreClientSuppliedReindexFlag` pattern; not a 13.5-introduced regression. Re-open trigger: a refactor of `TenantConfigurationActor`'s first-write semantics where the two signals are split into distinct branches.

**Dismissed (false positives or noise):**

- AC5 normalization test "asserts un-normalized stored value" (Blind Hunter) — production normalization is comparison-only via `EmbeddingProviderDefaults.NormalizeBaseUrl` (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:259-260`); stored form is intentionally unchanged. Test name "after normalization" refers to *input equivalence*, not stored form.
- `GetEmbeddingConfigAsync_OllamaOidcState_ShouldReturnAllMetadataFields` is a tautology (Blind Hunter) — round-trip via mock pins "no fields are dropped on read" which is meaningful defensive coverage even with weak signal.
- Legacy-default test asserts `AuthMode == "api-key"` from JSON that omits the field (Blind Hunter) — this correctly pins Story 13.4's record-default contract from the receive side; nothing to fix.
- AC9 negative `TenantProvisioningInput` shape pin absent (Edge Case Hunter) — story explicitly bounds AC9 to "do not change those files"; absence-of-test for a "do not modify" constraint is normal.
- AC2 reindex-flag-preservation-across-fallback test (Edge Case Hunter) — logically subsumed by the existing `DidNotReceive().SetStateAsync(...)` assertion (no write means no clearing).

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
- 2026-05-02: Verified sprint prerequisites: Story 13.1 and Story 13.4 are `done` in `sprint-status.yaml`.
- 2026-05-02: Read `TenantConfigurationActor`, `ITenantConfigurationActor`, `Program.cs` embedding-config delegates, `TenantEndpointHandlers`, and `TenantConfigurationView`; no runtime API or projection change was required.
- 2026-05-02: Added actor tests for legacy JSON defaults, read-only fallback/no repair writes, Ollama OIDC persistence/readback, first-write `ReindexRequired = false`, `baseUrl` breaking-change behavior, and non-breaking OIDC metadata changes.
- 2026-05-02: Added embedding-config response/conflict serialization coverage for Ollama OIDC metadata and raw-secret non-exposure.
- 2026-05-02: Extended `TenantConfigurationView` serialization coverage to assert full embedded `TenantEmbeddingConfig` OIDC metadata, `apiSecretKeyName` as a secret-name reference, and no raw client-secret field/value exposure.
- Validation: `dotnet test tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --filter FullyQualifiedName~TenantConfigurationActorTests` -> Passed 21/21.
- Validation: `dotnet test tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --filter FullyQualifiedName~TenantEmbeddingConfigEndpointTests` -> Passed 6/6.
- Validation: `dotnet test tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --filter FullyQualifiedName~TenantConfigurationEndpointTests` -> Passed 14/14.
- Validation: `dotnet test tests\Hexalith.Memories.Contracts.Tests\Hexalith.Memories.Contracts.Tests.csproj --filter FullyQualifiedName~TenantEmbeddingConfigSerializationTests` -> Passed 6/6.
- Validation: `dotnet build Hexalith.Memories.slnx` -> Succeeded, 0 warnings, 0 errors.
- Regression validation: `dotnet test tests\Hexalith.Memories.Contracts.Tests\Hexalith.Memories.Contracts.Tests.csproj --no-build` -> Passed 470/470.
- Regression validation: `dotnet test tests\Hexalith.Memories.Server.Tests\Hexalith.Memories.Server.Tests.csproj --no-build` -> Passed 1676/1676.
- Regression validation: `dotnet test tests\Hexalith.Memories.EventStore.Tests\Hexalith.Memories.EventStore.Tests.csproj --no-build` -> Passed 84/84.
- Regression validation: `dotnet test tests\Hexalith.Memories.Mcp.Tests\Hexalith.Memories.Mcp.Tests.csproj --no-build` -> Passed 76/76.
- Regression validation: `dotnet test Hexalith.Memories.slnx --no-build` timed out after 10 minutes without a completed test result; `dotnet test tests\Hexalith.Memories.Cli.Tests\Hexalith.Memories.Cli.Tests.csproj --no-build` timed out after 4 minutes without a completed test result.

### Completion Notes List

- Runtime implementation remained unchanged: the committed actor and endpoint surfaces already carry the additive Story 13.4 `TenantEmbeddingConfig` fields directly.
- Added focused actor coverage proving legacy Google state defaults new fields, corrupt/invalid read fallbacks are non-destructive, Ollama OIDC metadata persists and reads back, first-write reindex clearing is preserved, `baseUrl` changes require reindex only per Story 13.4 normalization, and other OIDC metadata changes do not force reindex.
- Added endpoint/listing serialization coverage proving `baseUrl`, `authMode`, `oidcTokenEndpoint`, `oidcClientId`, `oidcScope`, and `apiSecretKeyName` are exposed as metadata while `client_secret`, `clientSecret`, and sample raw secret values are absent.
- Did not modify `TenantEmbeddingConfig`, `EmbeddingProviderDefaults`, `EmbeddingClient`, OIDC token-provider code, provisioning workflow/input, AppHost/appsettings/docs, or migration tooling.
- Focused story validation and main non-integration regression suites are green; full solution and CLI project test commands timed out locally and are recorded above without claiming green results.

### File List

- `_bmad-output/implementation-artifacts/13-5-surface-new-fields-via-tenant-configuration-actor.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs`

### Change Log

| Date       | Change                                                                                                                                                                                                                                                      | Author |
|------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|
| 2026-05-02 | Implemented Story 13.5 as focused test coverage only: actor legacy/default/fallback/Ollama metadata behavior, embedding-config response/conflict serialization, configuration-view serialization, and secret-value non-exposure. Runtime surfaces required no changes. | Codex |
| 2026-05-02 | Party-mode review completed; clarified exact OIDC field propagation, read-only invalid fallback, direct full-config serialization, raw secret non-exposure assertions, and Story 13.4 `BaseUrl` reindex semantics while deferring any unresolved `ApiSecretKeyName` validation policy back to the prerequisite contract. | Codex |
| 2026-05-02 | Story 13.5 context created: actor state migration, Ollama config round-trip, embedding-config and configuration-view serialization, secret-name exposure without secret-value leakage, and strict boundaries against token provider, client dispatch, docs, provisioning, and migration scopes. | Codex |
