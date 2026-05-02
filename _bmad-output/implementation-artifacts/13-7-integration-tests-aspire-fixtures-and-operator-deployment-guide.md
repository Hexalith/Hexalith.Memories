# Story 13.7: Integration Tests, Aspire Fixtures & Operator Deployment Guide

Status: ready-for-dev

**Effort estimate:** ~1.25-1.75 working days. Breakdown:

- **0.15 day - Task 0:** Verify Epic 13 prerequisite stories are done and inspect current fixture/docs surfaces.
- **0.35 day - Task 1:** Parameterize provider integration coverage and add deterministic Ollama/Keycloak stubs.
- **0.30 day - Task 2:** Add the Ollama end-to-end Aspire integration path and dimension/provider assertions.
- **0.35 day - Task 3:** Write the operator deployment guide and developer cross-reference.
- **0.20 day - Task 4:** Run focused Tier-2/Tier-3/docs validation and record exact outcomes.

**HARD prerequisite:** Stories 13.2, 13.3, 13.4, 13.5, and 13.6 must be `done` before implementation starts. Verify both the matching `_bmad-output/implementation-artifacts/sprint-status.yaml` entries and the top-level `Status:` line in each story artifact. This story is the final Epic 13 proof-and-documentation pass; it depends on the committed token provider, Ollama dispatch, additive tenant config, actor/config surfaces, and migration command/runbook output. If any prerequisite remains `ready-for-dev`, `in-progress`, `review`, `blocked`, or has partially merged behavior, stop before editing code and record the affected ACs as blocked rather than inventing fallback contracts.

**SOFT prerequisite:** Keep the Story 13.6 Dev Agent Record open while writing the migration runbook section. The operator guide must describe the actual command names, flags, output fields, abort/resume behavior, and rollback limits that Story 13.6 committed, not the earlier planning shorthand.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

Finish Epic 13 by proving the Google and Ollama embedding paths through the integration harness and by shipping the operator-facing guide at `docs/operations/embedding-providers.md`.

The implementation must not require a real Keycloak realm, real Ollama server, or real `llm.tache.ai` endpoint in unit/Tier-2 coverage or routine CI. Add deterministic stubs/fakes that mimic the committed contracts:

- Ollama embed endpoint: `POST /api/embed` with `{ "model": "...", "input": "..." }`, returning `{ "embeddings": [[...]] }`.
- Keycloak token endpoint: client_credentials form post, returning a bearer token with audience `llm.tache.ai` or the configured audience.
- Tier-2 vectors: deterministic 2560-dim values for Ollama so Redis Vector and consistency assertions can pin dimensions.

The operator journey must be documented as observable checkpoints: gateway reachable, Keycloak client configured, tenant embedding config written, migration completed or explicitly skipped when no prior data exists, and post-migration verification completed.

The operator guide must be generic and anonymized. Use placeholders such as `{ISSUER}`, `{AUDIENCE}`, `{JWKS_URL}`, `{HOSTNAME}`, `{TOKEN_ENDPOINT}`, `{CLIENT_ID}`, and `{SECRET_NAME}`. Do not publish real client secrets, bearer tokens, host credentials, or tenant data.

## Story

As an **operator and developer**,
I want the Aspire test fixtures and integration tests to exercise both Google and Ollama embedding provider paths and a deployment guide that documents the gateway contract end-to-end,
so that a new operator can stand up the Ollama gateway, wire Keycloak, configure a tenant, migrate existing data, and verify the result against a documented expectation.

## Acceptance Criteria

1. **AC1 - Provider integration suite covers Google and Ollama.** The embedding integration suite is parameterized over `provider in {google, ollama}` with named xUnit cases or separate facts that map to AC IDs. Google continues to use the existing fake path, does not require OIDC, and must not call the new Ollama or Keycloak fake endpoints. Ollama uses a deterministic Ollama-compatible HTTP fake and Keycloak/OIDC fake for Tier-2/Tier-3 coverage. The bounded matrix is exactly: Google success regression, Ollama success with bearer token, Ollama auth/config failure evidence, vector dimension/provider-marker assertion, and hybrid-search observable result. Do not expand this into a provider/auth/documentation cross-product. Tier-2 coverage does not require Docker, real DAPR sidecars, real Keycloak, or real Ollama unless the existing test category already requires them.

2. **AC2 - Ollama end-to-end Aspire test proves the committed runtime path.** A new `OllamaEmbeddingEndToEnd` integration test provisions or configures an Ollama tenant through the committed tenant configuration API, ingests one content unit, verifies persisted embedding dimensions are 2560, verifies the committed provider/model metadata fields preserve `provider = "ollama"` and `model = "qwen3-embedding:4b"` or the committed combined marker `ollama:qwen3-embedding:4b`, and verifies hybrid search returns that unit through an observable API result. The test must use a unique tenant/case identifier plus a syntactic canary token so a stale Redis record, prior test run, or purely lexical result cannot satisfy the assertion accidentally.

3. **AC3 - Test fixtures provide deterministic 2560-dim Ollama vectors.** The Ollama fake returns repeatable 2560-length vectors derived from `model + "\n" + input` or an equivalently documented deterministic seed. Values must be ordered, finite, stable across test runs and cultures, non-zero for at least one element, and distinguishable from the existing Google fake dimensions so dimension drift cannot pass accidentally. Tests should assert length exactly 2560 and compare deterministic output with an explicit tolerance suitable for the numeric type.

4. **AC4 - OIDC token flow is exercised without leaking credentials.** The fake Keycloak/token endpoint asserts `application/x-www-form-urlencoded` client_credentials form shape, including `grant_type=client_credentials`, `client_id`, `client_secret`, and optional `scope`, and rejects missing or malformed values. The Ollama fake must fail any request that does not use `POST /api/embed` and must prove the outbound request carries `Authorization: Bearer <token>` without storing the raw token in assertion output. Tests prove captured logs, exception text, serialized payloads, docs examples, and committed artifacts do not contain raw sample values such as `super-secret-client-secret`, `client_secret=`, `Bearer eyJ`, or raw Google API keys.

5. **AC5 - Aspire/AppHost wiring remains local and opt-in.** AppHost or fixture changes may inject local test configuration for Ollama base URL, token endpoint, client id, scope, and DAPR secretstore entries only when an Ollama-specific test enables them. They must not hard-code real `llm.tache.ai` / `auth.tache.ai` as required runtime values, must not change default provider behavior, must not break the existing `Memories__Testing__UseFakeEmbedding=true` fixture path, and must keep Google/local tests green without real Google, Ollama, Keycloak, or gateway credentials.

6. **AC6 - Operator guide documents the gateway contract.** `docs/operations/embedding-providers.md` documents the Ollama-native HTTP contract: `POST /api/embed`, request body `{model,input}`, response body `{embeddings:[[...]]}`, bearer JWT authentication, audience claim validation, JWKS validation expectations, and the fact that Ollama's default local API base is `/api` while the configured gateway base URL should be joined safely with `/api/embed`.

7. **AC7 - Operator guide includes anonymized Envoy + Ollama example.** The guide provides a generic Envoy or equivalent gateway example using placeholders only: `{ISSUER}`, `{AUDIENCE}`, `{JWKS_URL}`, `{HOSTNAME}`, and backend Ollama target. It explains that TLS certificate management, cert-manager installation, wildcard DNS, GPU scheduling, and production Keycloak hosting are operator-owned infrastructure and are not shipped by this repository.

8. **AC8 - Operator guide includes the provider configuration matrix.** The guide lists every `TenantEmbeddingConfig` field operators must supply for each supported option: Google api-key, self-hosted Ollama with OIDC client_credentials, and Ollama local/no-auth or upstream-api-key mode. It must explain `ApiSecretKeyName` as a DAPR secret-name reference and, in OIDC mode, the secret value is the OIDC `client_secret`.

9. **AC9 - Operator guide includes the Keycloak setup recipe.** The guide documents the realm/client setup from the sprint change proposal: confidential client, service accounts enabled, standard/direct/implicit flows disabled where applicable, token endpoint, `memories-embedding` client id, `openid` scope if used, access-token lifespan guidance, and a hardcoded or resolved audience mapper that produces audience `llm.tache.ai` or the configured gateway audience.

10. **AC10 - Operator guide includes DAPR secretstore layout.** The guide documents local `secretstores.local.file` shape and production secret-store expectations for `memories-embedding-client-secret` or tenant-specific names. It must state that secret values are never stored in `TenantEmbeddingConfig`, never returned from configuration APIs, and never logged.

11. **AC11 - Story 13.6 migration runbook is included.** The guide carries the actual committed migration command sequence from Story 13.6: dry-run, live tenant migration, resume after interruption, final verification, and rollback behavior. It includes expected human and JSON output shapes when available and explicitly says Path B coexistence/rollback is only available when retained previous-version indexes exist. If Story 13.6 is not `done` or its Dev Agent Record lacks committed command/output evidence, this AC is blocked; Story 13.7 must not invent migration commands.

12. **AC12 - Developer documentation cross-references the operator guide.** `docs/dev/embedding-providers.md` is created or the closest existing developer-facing docs page is updated to link to the operator guide and summarize the provider decision matrix: Google api-key, Ollama OIDC, and Ollama local/no-auth. The developer doc must stay concise and point operators to `docs/operations/embedding-providers.md` for runbook detail.

13. **AC13 - Existing provider behavior is not regressed.** Existing Google/fake embedding tests continue to pass. Existing `AspireIngestionPipelineFixture` consumers continue to boot without requiring the new Ollama fake unless the specific Ollama test enables it.

14. **AC14 - Documentation examples are redacted and stable.** All docs examples use placeholders, fake tokens, or secret names. Acceptable placeholder style is `{TOKEN_ENDPOINT}`, `{CLIENT_ID}`, `{SECRET_NAME}`, `{AUDIENCE}`, `{JWKS_URL}`, `{HOSTNAME}`, or obviously fake values prefixed with `example-`. No real tenant IDs, real client secrets, bearer tokens, raw Google API keys, host credentials, or production hostnames are committed. Markdown links are relative and valid.

15. **AC15 - Validation evidence is recorded.** The Dev Agent Record lists exact commands, filters, test names, and pass/fail/skip outcomes for focused provider tests, the new Ollama end-to-end test or skipped reason, docs link checks when available, and `dotnet build Hexalith.Memories.slnx` if the local SDK allows it. If an Aspire/Tier-3 test is environment-gated, the skip must state the missing prerequisite, and Tier-2 contract/fake tests must still run.

## Tasks / Subtasks

- [ ] Task 0 - Verify prerequisites and current surfaces (AC: #1-#15)
  - [ ] Confirm Stories 13.2, 13.3, 13.4, 13.5, and 13.6 are `done`; if any is not done, stop.
  - [ ] Confirm each prerequisite with both the sprint-status entry and the story artifact `Status:` line. Treat `blocked` and partial merge evidence as a hard stop.
  - [ ] Read `src/Hexalith.Memories.AppHost/Program.cs`; preserve current DAPR sidecar, secretstore, fake embedding, Redis/FalkorDB, and MCP wiring.
  - [ ] Read `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`; preserve process-wide env-var scoping, randomized DAPR app id / Redis volume, `Memories__Testing__UseFakeEmbedding=true`, and existing fixture startup behavior.
  - [ ] Read `tests/Hexalith.Memories.IntegrationTests/Fixtures/ScriptedHttpServer.cs`; reuse or extend it for Ollama/Keycloak fakes before adding another local HTTP server.
  - [ ] Read the committed Story 13.6 migration tool story and implementation record; copy actual command names and output fields into the runbook.
  - [ ] Read the committed Story 13.3 and 13.6 implementation records for exact provider/model metadata field names before writing Redis assertions.
  - [ ] Inspect existing docs under `docs/operations/` and `docs/dev/` for tone, headings, and relative-link style.

- [ ] Task 1 - Add provider-aware integration fixture support (AC: #1, #3, #4, #5, #13)
  - [ ] Add a small provider fixture model, for example `EmbeddingProviderTestMode.GoogleFake` / `OllamaOidcFake`, without changing the default fixture behavior for existing tests.
  - [ ] Keep the existing `Memories__Testing__UseFakeEmbedding=true` path for tests that are not explicitly validating the real provider dispatch path.
  - [ ] For the Ollama mode, configure the server under test with the fake Ollama base URL, fake token endpoint, client id, scope, and DAPR secret name expected by the committed Story 13.4/13.5 config surface.
  - [ ] Store the fake OIDC `client_secret` in the local DAPR secretstore or test configuration exactly the way the committed `EmbeddingClient` resolves it; do not bypass production secret retrieval in an integration test.
  - [ ] If existing seams cannot validate production secret retrieval without semantic changes to token provider, EmbeddingClient, tenant config, or AppHost defaults, stop and record a deferred architecture decision instead of expanding scope.
  - [ ] Reuse `EnvVarScope` for all process-wide overrides and dispose scopes on fixture initialization failure.
  - [ ] Keep Ollama/Keycloak fake base URLs scoped to the selected provider mode and reset them between tests so parallel or later Google/fake-path tests cannot inherit provider-specific state.
  - [ ] Ensure the fake server captures sanitized request evidence for method, path, count, client identity where safe, and JSON/form shape without logging bearer tokens or secrets.
  - [ ] Add a regression proving the default fake embedding path starts and runs without configuring Ollama/Keycloak fakes and without calling either fake endpoint.

- [ ] Task 2 - Add Ollama and Keycloak HTTP fakes (AC: #2, #3, #4, #6)
  - [ ] Implement an Ollama-compatible fake endpoint for `POST /api/embed`.
  - [ ] Fail the test if production code calls `/api/embeddings`, an OpenAI-compatible endpoint, the wrong HTTP method, or any path other than `/api/embed`.
  - [ ] Assert request JSON contains the configured `model` and `input`; do not require a specific property order.
  - [ ] Return `{ "model": "<model>", "embeddings": [[...]] }` with exactly 2560 floats for `qwen3-embedding:4b`, generated from a documented deterministic seed and asserted with an explicit tolerance.
  - [ ] Add failure hooks only if useful for existing retry tests; do not duplicate Story 13.3 unit-test coverage at integration level.
  - [ ] Implement a Keycloak token fake for the `client_credentials` grant. It should validate form encoding and return `access_token`, `expires_in`, and `token_type`.
  - [ ] Reject missing `Content-Type`, missing `grant_type`, missing `client_id`, missing `client_secret`, and malformed form bodies in the token fake.
  - [ ] If a JWT is needed for gateway validation tests, mint a test JWT with `aud = llm.tache.ai` or the configured audience. Otherwise, a synthetic opaque bearer is enough when only the app's outbound header is under test.

- [ ] Task 3 - Parameterize provider tests and add Ollama end-to-end coverage (AC: #1, #2, #3, #4, #13)
  - [ ] Identify the smallest existing embedding integration slice to parameterize without exploding Tier-3 runtime.
  - [ ] Add or update tests so Google still proves the existing fake/provider path.
  - [ ] Add `OllamaEmbeddingEndToEnd` or an equivalent focused test in `tests/Hexalith.Memories.IntegrationTests/`.
  - [ ] Provision/configure an Ollama tenant through the committed tenant configuration API as the preferred acceptance path; use actor-level setup only for a separate focused helper test or if the committed API is unavailable and that blocker is recorded.
  - [ ] Ingest one content unit through the normal ingestion endpoint/workflow.
  - [ ] Verify Redis semantic hash metadata includes target provider/model, 2560 dimensions, tenant/config source where committed, and content-unit correlation key using the committed field names from Stories 13.3/13.6.
  - [ ] Verify hybrid search returns the memory unit. If semantic scoring is nondeterministic, include a syntactic canary token so the assertion cannot pass from stale data.
  - [ ] Assert returned case/content identifiers match the new unit, not just non-empty search results or a high score.
  - [ ] Assert the fake Ollama endpoint received at least one embed request and the fake token endpoint received the expected token request count.

- [ ] Task 4 - Write `docs/operations/embedding-providers.md` (AC: #6-#11, #14)
  - [ ] Start with operator decision guidance: when to choose Google api-key, Ollama OIDC, or Ollama local/no-auth.
  - [ ] Label local/no-auth Ollama as local development or trusted-network only; do not present it as acceptable for exposed production ingress.
  - [ ] Split operator-owned infrastructure from repository-owned configuration and commands.
  - [ ] Include observable checkpoints: gateway reachable, Keycloak client configured, tenant config written, migration completed or explicitly skipped, and verification complete.
  - [ ] Document the Ollama gateway contract using the committed `EmbeddingClient` behavior and the current official Ollama `/api/embed` shape.
  - [ ] Document Keycloak client_credentials setup with audience mapper guidance and token lifetime recommendations.
  - [ ] Add the `TenantEmbeddingConfig` field matrix, including required/optional fields and examples for each provider option.
  - [ ] Add DAPR secretstore examples for local file secretstore and production secret-manager equivalents. Keep values as placeholders.
  - [ ] Add the Story 13.6 migration runbook from committed evidence only: dry-run, live, resume, verify, rollback guardrails, and expected output.
  - [ ] If Story 13.6 still lacks committed command/output evidence, include a short blocked note in the Dev Agent Record and leave the migration command section as a placeholder-free explanation of the dependency; do not invent command names.
  - [ ] Add troubleshooting for common failures: wrong vector dimensions, missing audience, invalid client secret, missing DAPR secret, 401/403 after token refresh, hybrid search empty after migration, and accidental Path B rollback expectation.

- [ ] Task 5 - Add developer-facing cross-reference (AC: #12, #14)
  - [ ] Create `docs/dev/embedding-providers.md` if it does not exist, or update the closest existing developer provider/config docs.
  - [ ] Keep the developer page brief: implementation surfaces, where tests live, how to run provider-specific tests, and a link to the operator guide.
  - [ ] Cross-link from `README.md` or `docs/dev/quickstart.md` only if the repository already uses those pages for provider setup; avoid broad doc churn.

- [ ] Task 6 - Validate and record completion (AC: #1-#15)
  - [ ] Run focused unit/Tier-2 tests for any new fake servers or fixture helpers.
  - [ ] Run the new Ollama end-to-end test when Docker/DAPR/Aspire prerequisites are available.
  - [ ] If the Ollama end-to-end test is skipped, record the exact missing prerequisite and the Tier-2 tests that still ran.
  - [ ] Run existing Google/fake embedding integration tests touched by parameterization.
  - [ ] Run docs checks if the repo has one; otherwise manually verify relative links in the new docs.
  - [ ] Run `dotnet build Hexalith.Memories.slnx` if the local SDK allows it.
  - [ ] Record exact commands and outcomes in the Dev Agent Record. If the SDK pin, Docker, DAPR, or environment blocks validation, record the exact blocker and do not claim green tests.

## Dev Notes

### Current Implementation State

- `AspireIngestionPipelineFixture` currently starts the full AppHost through `DistributedApplicationTestingBuilder.CreateAsync<Projects.Hexalith_Memories_AppHost>()`, sets `Memories__Testing__UseFakeEmbedding=true`, wires randomized DAPR app id and Redis volume through `EnvVarScope`, and exposes `MemoriesClient`, `McpClient`, Redis, FalkorDB, and actor proxies.
- `src/Hexalith.Memories.AppHost/Program.cs` creates a local DAPR `secretstores.local.file` component backed by repo-root `secrets.json`, a deterministic `conversation.echo` component for natural-language descriptions, Redis Stack, FalkorDB, Memories Server with a DAPR sidecar, and MCP as a sibling sidecar service.
- `ScriptedHttpServer` already provides a local loopback Kestrel test server with arbitrary request handling. Prefer extending/reusing it for Ollama and token-endpoint fakes instead of introducing WireMock or another dependency unless the committed code needs features it cannot provide.
- Existing integration tests are split between `RedisStack` and `AspireIngestionPipeline` collections. The new provider end-to-end test belongs in the Aspire collection if it exercises the full server/DAPR path.
- `docs/operations/rate-limiting.md`, `docs/operations/failure-recovery.md`, and `docs/dev/quickstart.md` show the repository's preferred doc style: direct operator instructions, explicit known limitations, concrete command examples, and no marketing copy.
- The current docs do not contain `docs/operations/embedding-providers.md` or `docs/dev/embedding-providers.md`; this story owns creating them unless a prerequisite story added one.
- Official Ollama docs identify `/api/embed` as the current embedding endpoint, with `model` and `input` required and response `embeddings` as a two-dimensional number array. The older `/api/embeddings` endpoint must not be used for new docs or tests.
- Official Keycloak docs describe client_credentials as a service-account token flow and audience mappers as the mechanism to put the intended resource server in the access token `aud` claim. The guide should document audience validation as a gateway responsibility.
- Aspire testing docs recommend `DistributedApplicationTestingBuilder` for AppHost-based integration tests and note that environment variables override appsettings/secrets. This matches the current fixture's `EnvVarScope` pattern.

### File Scope

**Expected edited files:**

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/ScriptedHttpServer.cs` or new focused fake helpers under `tests/Hexalith.Memories.IntegrationTests/Fixtures/`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/*Ollama*Tests.cs` or another provider-focused integration-test file
- `docs/operations/embedding-providers.md`
- `docs/dev/embedding-providers.md`
- `_bmad-output/implementation-artifacts/13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide.md`

**Possible edited files only if the committed prerequisite surfaces require it:**

- `src/Hexalith.Memories.AppHost/Program.cs` for opt-in test/local env propagation or local fake endpoint configuration.
- `src/Hexalith.Memories.AppHost/appsettings.json` and `appsettings.Development.json` for non-secret defaults only.
- `README.md` or `docs/dev/quickstart.md` for one-line cross-links.
- `tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj` if new test helper files require package references. Do not add package versions there.

**Do not edit in this story:**

- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs`, `IOidcTokenProvider.cs`, or token-provider behavior from Story 13.2.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` except for tiny prerequisite fallout. Provider dispatch belongs to Story 13.3.
- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` or `EmbeddingProviderDefaults.cs` except for tiny prerequisite fallout. Config semantics belong to Story 13.4.
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` except for tiny prerequisite fallout. Actor exposure belongs to Story 13.5.
- Story 13.6 migration tool behavior except documentation of its committed commands.
- `Hexalith.Commons`, `Hexalith.EventStore`, nested submodule contents, or recursive submodule initialization.

### Implementation Guidance

- Make provider mode an explicit fixture/test choice. Do not flip the global default fixture away from fake embeddings; many existing integration tests rely on deterministic low-cost startup.
- Keep fake servers in-process and loopback-only. Avoid requiring containerized Keycloak/Ollama for routine CI unless explicitly placed in Tier-3/nightly.
- Treat real external provider certification as out of scope. The acceptance proof is fake-first contract coverage plus optional operator guidance, not live `llm.tache.ai`, live Keycloak, or live Google certification.
- If AppHost needs to consume fake URLs from environment variables, name them under a clear `Embedding__...` or existing config prefix and ensure they are optional. Production deployments should continue to bind values from environment/secrets.
- Use `MemoriesJsonContext.Options` for JSON payloads where contract types are involved.
- Use Shouldly and existing fixture naming conventions. Avoid raw `Assert.*`.
- Preserve cancellation tokens and timeouts in new fixture startup paths. A fake server startup failure should tear down env-var scopes just like the existing fixture does.
- Do not implement JWT validation inside Memories Server for the Ollama gateway. The gateway validates inbound bearer tokens; Memories Server is the client that obtains and sends them.
- Do not invent gateway JWT audience/JWKS policy beyond the committed gateway expectations and sprint-change proposal. If the expected policy is unclear, record a deferred architecture decision.
- For docs, distinguish the three bases clearly: tenant `BaseUrl` for the gateway/Ollama API, `OidcTokenEndpoint` for Keycloak, and DAPR `secretstore` for the client secret value.
- Do not tell operators that changing provider/model/dimensions is a simple config edit. Link it to Story 13.6 migration and explain Path A downtime/resume semantics.
- "Tiny prerequisite fallout" in possible source edits means compile/test adaptation only. No semantic changes to token acquisition, embedding dispatch, tenant config, actor behavior, or migration behavior are allowed without a story update.

### Security Requirements

- No raw `client_secret`, Google API key, or bearer token may appear in committed docs, test names, logs, assertion messages, or result artifacts.
- `ApiSecretKeyName` is safe to document and serialize because it is a secret-name reference.
- Audience validation belongs at the gateway. The guide must tell operators to reject tokens whose `aud` does not match the gateway audience.
- Local/no-auth Ollama mode must be documented as local/development or trusted-network only unless protected by an upstream gateway. Do not imply it is acceptable for exposed production ingress.
- The AppHost secretstore may reference repo-root `secrets.json`, but docs must call out that the file is local/dev and production deployments should use platform secret management.
- Redaction proof should scan only committed docs, captured fake evidence, log output available to the test, and serialized payload examples. Do not add a broad repository-wide secret scan that can fail on unrelated historical fixtures or local audit files.

### Testing Requirements

- Use xUnit + Shouldly and the existing integration fixture style.
- Tier-2 fake tests should not require Docker, DAPR sidecars, Keycloak, or Ollama.
- Tier-3 Aspire tests may require Docker/DAPR/Aspire and should remain in the `AspireIngestionPipeline` collection.
- Keep Tier-2 contract/fake tests mandatory even when a Tier-3 Aspire test is environment-gated; silent skips do not satisfy the story.
- Include a redaction test that scans captured logs and serialized docs/examples for sample raw secrets/tokens.
- Include a dimension assertion that fails if Ollama vectors are 768 or any value other than 2560.
- Include a provider/model assertion that preserves the colon in `qwen3-embedding:4b`.
- Keep one Google-path regression test in the parameterized suite so provider abstraction changes cannot silently break existing tenants.
- Name or trait new tests with story/AC breadcrumbs where practical, for example `Story13_7_AC2`, so validation evidence can be traced without rereading test bodies.

### Previous Story Intelligence

- Story 13.1 established the `ollama` provider name, `qwen3-embedding:4b`, and 2560 dimensions. It also left follow-up pressure to keep provider/model/dimension contracts explicit.
- Story 13.2 owns token acquisition, caching, invalidation, typed failures, and redaction. This story should test that path through fakes, not reimplement it.
- Story 13.3 owns `EmbeddingClient` Ollama dispatch, `/api/embed` shape, bearer injection, 401/403 retry, and colon-preserving provider/model parsing.
- Story 13.4 owns `TenantEmbeddingConfig` fields, auth mode strings, URL validation, secret-name semantics, and `BaseUrl` breaking-change detection.
- Story 13.5 owns actor/configuration API exposure. Use those surfaces to configure test tenants.
- Story 13.6 owns migration commands and resume/rollback behavior. The guide must follow actual committed behavior rather than expanding migration scope.
- Story 12.3 through 12.6 reinforced strict file-scope and release-lane discipline. Keep this story focused on integration fixtures/tests and documentation.

### Anti-Patterns to Avoid

- Do not require real `llm.tache.ai`, real `auth.tache.ai`, a real Keycloak realm, or a real Ollama model for routine tests.
- Do not use `/api/embeddings` in new tests or docs; use `/api/embed`.
- Do not publish an Envoy/Keycloak example with real hostnames or secrets in non-placeholder fields.
- Do not silently skip Ollama assertions when the fake endpoint was not called.
- Do not change the default integration fixture to Ollama and break unrelated tests.
- Do not duplicate the migration tool's logic in docs examples. The runbook should invoke the tool, not restate an ad-hoc Redis CLI procedure.
- Do not broaden Epic 13 into cert-manager, GPU scheduling, multi-node Ollama, Path B coexistence, or production Kubernetes deployment automation.

## References

- [Source: `_bmad-output/planning-artifacts/epics.md` Epic 13 Story 13.7] - integration tests, Aspire fixtures, operator guide, gateway contract, Keycloak setup, and DAPR secret layout.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-29.md` Sections 4.4, 4.5, and 5] - Story 13.7 scope, Keycloak client values, DAPR secret name, and implementation handoff.
- [Source: `_bmad-output/implementation-artifacts/13-2-implement-oidc-token-provider.md`] - token-provider dependency and redaction behavior.
- [Source: `_bmad-output/implementation-artifacts/13-3-extend-embedding-client-to-support-ollama.md`] - Ollama `/api/embed` dispatch, bearer injection, response parsing, and provider/model marker.
- [Source: `_bmad-output/implementation-artifacts/13-4-extend-tenant-embedding-config-with-additive-oidc-fields.md`] - provider config fields, auth modes, URL validation, and secret-name semantics.
- [Source: `_bmad-output/implementation-artifacts/13-5-surface-new-fields-via-tenant-configuration-actor.md`] - actor/config API surface for carrying Ollama metadata.
- [Source: `_bmad-output/implementation-artifacts/13-6-vector-migration-tool.md`] - migration runbook scope and Story 13.7 documentation handoff.
- [Source: `src/Hexalith.Memories.AppHost/Program.cs`] - current AppHost, DAPR component, secretstore, fake embedding, and service wiring.
- [Source: `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`] - current Aspire integration-test fixture and environment override pattern.
- [Source: `tests/Hexalith.Memories.IntegrationTests/Fixtures/ScriptedHttpServer.cs`] - reusable local HTTP fake server.
- [Source: `docs/operations/failure-recovery.md` and `docs/operations/rate-limiting.md`] - operator documentation style and embedding-rate-limit context.
- [Source: `docs/dev/quickstart.md`] - developer documentation style and local AppHost guidance.
- [Source: Ollama official API docs, "Generate embeddings", accessed 2026-05-02] - `/api/embed` request/response shape: https://docs.ollama.com/api/embed
- [Source: Keycloak Server Administration Guide, "Client credentials grant" and "Audience support", accessed 2026-05-02] - service-account token and audience mapper guidance: https://www.keycloak.org/docs/latest/server_admin/
- [Source: Aspire official docs, "Manage the AppHost in tests", accessed 2026-05-02] - `DistributedApplicationTestingBuilder` and environment override behavior: https://learn.microsoft.com/en-us/dotnet/aspire/testing/manage-app-host

## Project Context Reference

The BMad persistent-facts glob found `Hexalith.Commons/_bmad-output/project-context.md` but no Memories-local `project-context.md`. Treat the Commons context as general Hexalith ecosystem guidance only. Repository-specific constraints in this story and in the Memories planning artifacts take precedence.

## Party-Mode Review

- **Date/time:** 2026-05-02T18:18:22+02:00
- **Selected story key:** `13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide`
- **Command/skill invocation used:** `/bmad-party-mode 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- **Findings summary:** The story is valuable as Epic 13's final proof-and-documentation pass, but the review found pre-dev ambiguity around the hard prerequisite gate, provider test matrix, fake-first runtime proof, deterministic vector contract, exact provider/model metadata assertions, Story 13.6 migration dependency, redaction evidence, Tier-2 versus Tier-3 boundaries, and local/no-auth adopter warnings.
- **Changes applied:** Tightened the hard prerequisite to require both sprint-status and story-artifact `Status:` evidence; bounded the Google/Ollama provider test matrix; clarified the Ollama end-to-end path as fake-first and tenant-configuration-API driven; required `/api/embed` failure on wrong method/path; specified deterministic 2560-vector generation and tolerance expectations; added exact redaction forbidden substrings; added default fake-path negative assertions; split operator documentation into observable checkpoints and repository-owned versus operator-owned responsibilities; made Story 13.6 command/runbook evidence a blocking dependency; and narrowed "tiny prerequisite fallout" to compile/test adaptation only.
- **Findings deferred:** Live external provider certification, provider capability redesign, gateway JWT/JWKS policy invention, production Kubernetes/cert-manager/GPU guidance, local/no-auth security posture beyond warning language, and migration UX changes beyond documenting committed Story 13.6 behavior remain out of scope or deferred to later product/architecture decisions.
- **Final recommendation:** `ready-for-dev`

## Advanced Elicitation

- **Date/time:** 2026-05-02T18:47:03+02:00
- **Selected story key:** `13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide`
- **Command/skill invocation used:** `/bmad-advanced-elicitation 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide`
- **Batch 1 method names:** Security Audit Personas; Failure Mode Analysis; Pre-mortem Analysis; Comparative Analysis Matrix; Critique and Refine.
- **Reshuffled Batch 2 method names:** Red Team vs Blue Team; First Principles Analysis; Self-Consistency Validation; User Persona Focus Group; Expand or Contract for Audience.
- **Findings summary:** The story was already ready for development, but the elicitation pass found residual ambiguity around matrix expansion risk, stale-data false positives, cross-test fixture contamination, migration-runbook dependency handling, redaction scan boundaries, and validation traceability.
- **Changes applied:** Bounded AC1 to the exact finite provider matrix; strengthened AC2 against stale Redis/search results with unique case identifiers; made deterministic vectors culture-stable; added fixture reset and cross-test isolation requirements; required search-result identity assertions; clarified Story 13.6 blocked-runbook handling; scoped redaction proof to committed docs/test evidence; and added AC breadcrumb guidance for validation evidence.
- **Findings deferred:** No product, architecture, or cross-story contract changes were applied. Live provider certification, gateway policy design, migration command invention before Story 13.6 evidence, and broad repository secret scanning remain out of scope.
- **Final recommendation:** `ready-for-dev`

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- Story authored on 2026-05-02 by the recurring pre-dev hardening automation after preflight JSON timestamp `2026-05-02T08:49:49Z`.
- Preflight reported a working-tree cleanliness failure only. It was classified as an active-dev-story soft warning because `13-2-implement-oidc-token-provider.md` and the matching `sprint-status.yaml` entry are `in-progress`; other dirty paths in the JSON are ordinary implementation paths for Story 13.2.
- No code implementation was performed in this run; this is a create-story artifact only.

### Completion Notes List

- Story created with status `ready-for-dev`.
- Sprint status updated from `backlog` to `ready-for-dev` for `13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide`.
- Implementation is explicitly gated on Stories 13.2, 13.3, 13.4, 13.5, and 13.6 reaching `done`.

### File List

- `_bmad-output/implementation-artifacts/13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

| Date       | Change                                                                                                                                                                                                                                   | Author |
|------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|
| 2026-05-02 | Advanced elicitation completed; tightened finite matrix language, stale-data guards, deterministic vector stability, fixture isolation, migration dependency handling, redaction scan boundaries, and validation traceability. | Codex |
| 2026-05-02 | Party-mode review completed; clarified prerequisite gating, finite provider test matrix, fake-first `/api/embed` and OIDC assertions, deterministic 2560-vector contract, redaction evidence, Tier-2/Tier-3 skip policy, Story 13.6 runbook dependency, and operator documentation checkpoints. | Codex |
| 2026-05-02 | Story 13.7 context created: provider-parameterized integration coverage, Ollama/Keycloak fakes, Aspire fixture boundaries, operator deployment guide, provider config matrix, migration runbook handoff, and redaction constraints. | Codex |
