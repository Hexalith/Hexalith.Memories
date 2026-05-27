# Story 1.7: Embedding Provider Configuration

Status: done

## Story

As a developer,
I want to configure the embedding provider and model per tenant,
so that different tenants can use different embedding providers and the system is ready for multi-provider support.

## Acceptance Criteria

1. **Given** a new tenant is being configured
   **When** I set the embedding provider configuration
   **Then** I can specify: provider (google), model (gemini-embedding-001), dimensions (768), rateLimitPerMinute (1500)
   **And** the configuration is stored as part of the tenant configuration

2. **Given** the tenant configuration supports the provider/model/dimensions/rateLimit fields
   **When** the `GenerateEmbeddingActivity` runs for a tenant
   **Then** it reads the tenant's provider configuration to determine which embedding API to call
   **And** it reads the tenant's rate limit to configure the `EmbeddingRateLimiterActor`

3. **Given** MVP supports Google only
   **When** I inspect the configuration structure
   **Then** the provider field accepts an enum/string that can be extended to openai, mistral, custom in future phases
   **And** the `IEmbeddingProvider` pattern (concrete class, not interface) supports addition of new providers without refactoring

4. **Given** switching embedding providers requires full reindex
   **When** a tenant's provider configuration is changed
   **Then** the system warns that existing vectors are incompatible and a reindex is required
   **And** the change is not silently applied without acknowledgment

## Tasks / Subtasks

- [x] Task 1: Create `TenantEmbeddingConfig` sealed record (AC: #1, #3)
    - [x] 1.1 Create single `TenantEmbeddingConfig` record in `Contracts/V1/` with all fields: Provider (string), Model (string), Dimensions (int), RateLimitPerMinute (int), ApiSecretKeyName (string), ReindexRequired (bool, default false). One record — no separate `EmbeddingProviderConfig` (Occam's Razor: both are exposed via REST, splitting adds indirection without value)
    - [x] 1.2 Register in `MemoriesJsonContext` for serialization
    - [x] 1.3 Write serialization round-trip tests in `Contracts.Tests`

- [x] Task 2: Create `EmbeddingProviderDefaults` static class (AC: #3)
    - [x] 2.1 Create `EmbeddingProviderDefaults` in `Server/Ingestion/` with static factory methods returning default configs per known provider
    - [x] 2.2 Google default: provider="google", model="gemini-embedding-001", dimensions=768, rateLimitPerMinute=1500
    - [x] 2.3 Validation method: `Validate(TenantEmbeddingConfig)` that checks dimensions > 0, rateLimitPerMinute > 0 and <= MaxRateLimitPerMinute (3000 for Google — prevents one tenant from monopolizing shared API key), provider and model not null/empty, apiSecretKeyName matches `^[a-z0-9-]+$` (alphanumeric + hyphens only — prevents path traversal or cross-tenant key references)

- [x] Task 3: Create `TenantConfigurationActor` for per-tenant config storage (AC: #1, #2, #4)
    - [x] 3.1 Create `ITenantConfigurationActor` interface in `Server/Actors/` with methods: `GetEmbeddingConfigAsync()`, `SetEmbeddingConfigAsync(TenantEmbeddingConfig, bool forceReindex)` — no `GetFullConfigAsync()` (YAGNI per D9; GET endpoint can call `GetEmbeddingConfigAsync` directly)
    - [x] 3.2 Create `TenantConfigurationActor` DAPR actor implementation — actor ID = tenant ID
    - [x] 3.3 Store embedding config in actor state key `"embeddingConfig"`
    - [x] 3.4 On `SetEmbeddingConfigAsync`: if provider/model/dimensions changed AND `forceReindex` is false, throw `EmbeddingConfigChangeException` warning reindex is required (AC #4). If `forceReindex` is true, save the new config AND set `ReindexRequired = true` on the stored `TenantEmbeddingConfig` (enforcement deferred to Epic 5, but the flag is tracked now)
    - [x] 3.5 If no config exists (new tenant), return `EmbeddingProviderDefaults.Google()` as default
    - [x] 3.6 Register actor in `Program.cs`
    - [x] 3.7 Defensive deserialization in `GetEmbeddingConfigAsync()`: catch `JsonException` from `StateManager.GetStateAsync<TenantEmbeddingConfig>()`, log warning, return `EmbeddingProviderDefaults.Google()` — prevents permanent tenant lockout after schema evolution

- [x] Task 4: Refactor `EmbeddingClient` to support configurable provider (AC: #2, #3)
    - [x] 4.1 Extract constructor parameters to accept model, endpoint URL, and expected dimensions from config rather than hardcoded constants
    - [x] 4.2 Add `output_dimensionality` field to request JSON payload (required by gemini-embedding-001 when using non-default dimensions)
    - [x] 4.3 Update `ExpectedDimensions` to come from config, not const
    - [x] 4.4 Update endpoint URL to be configurable (gemini-embedding-001 uses `/v1beta/` not `/v1/`)
    - [x] 4.5 Keep API key retrieval via DAPR Secrets (secret key name from config)
    - [x] 4.6 Update existing `EmbeddingClient` tests for new constructor shape
    - [x] 4.7 Change `EmbeddingClient` registration from `AddHttpClient<EmbeddingClient>()` to `AddSingleton<EmbeddingClient>()` with `IHttpClientFactory` injected. Replace the single `_apiKey` field with `ConcurrentDictionary<string, string>` keyed by `apiSecretKeyName`. Singleton lifetime means the cache persists across requests. No separate `EmbeddingSecretCache` class needed (Occam's Razor: one dictionary on one class vs a whole new service)

- [x] Task 5: Refactor `GenerateEmbeddingActivity` to read tenant config (AC: #2)
    - [x] 5.1 Add `IActorProxyFactory` call to `TenantConfigurationActor` to retrieve `TenantEmbeddingConfig`
    - [x] 5.2 Pass config values (model, dimensions, endpoint URL) to `EmbeddingClient`
    - [x] 5.3 Call `SetCeilingAsync(config.RateLimitPerMinute)` unconditionally on `EmbeddingRateLimiterActor` before `TryConsumeAsync()` — idempotent call, no need to read current state first (Occam's Razor: skip the read-compare-write cycle; actor can short-circuit internally if unchanged)
    - [x] 5.4 Update `EmbeddingResult` to use provider/dimensions from config (not hardcoded)
    - [x] 5.5 Update existing activity tests

- [x] Task 6: Add REST endpoints for tenant embedding configuration (AC: #1, #4)
    - [x] 6.1 `GET /api/tenants/{tenantId}/embedding-config` — returns current config including `reindexRequired` flag (partial FR45 satisfaction)
    - [x] 6.2 `PUT /api/tenants/{tenantId}/embedding-config` — updates config; accepts `forceReindex` query param. On 409 Conflict (config change without forceReindex), return structured JSON error body: `{ "error": "EmbeddingConfigChangeRequired", "message": "...", "currentConfig": {...}, "proposedConfig": {...}, "affectedFields": ["dimensions"] }`
    - [x] 6.3 Register endpoints in `Program.cs`

- [x] Task 7: Update `EmbeddingInput`/`EmbeddingResult` records (AC: #2)
    - [x] 7.1 `EmbeddingInput`: no change needed (TenantId is sufficient — activity reads config via actor)
    - [x] 7.2 `EmbeddingResult`: Provider and Dimensions already dynamic — verify they come from config

- [x] Task 8: Write comprehensive tests (all ACs)
    - [x] 8.1 `TenantConfigurationActor` tests: get default config, set config, change detection, forceReindex bypass
    - [x] 8.2 `EmbeddingProviderDefaults` tests: Google default values, validation logic
    - [x] 8.3 `GenerateEmbeddingActivity` tests: reads tenant config, passes config to client, updates rate limiter ceiling
    - [x] 8.4 Serialization tests for new records
    - [x] 8.5 REST endpoint tests: PUT returns 409 Conflict when config changes affect provider/model/dimensions and `forceReindex=false`; GET returns default config for unconfigured tenant
    - [x] 8.6 `EmbeddingClient` test: request JSON includes exact `"output_dimensionality"` field (snake_case, NOT camelCase) — Google API silently ignores unknown fields and returns 3072-dim vectors if name is wrong
    - [x] 8.7 `EmbeddingClient` singleton test: two concurrent tenants with different `apiSecretKeyName` values both retrieve correct API keys from `ConcurrentDictionary` cache
    - [x] 8.8 `EmbeddingProviderDefaults` validation tests: reject `apiSecretKeyName` with special chars, reject `rateLimitPerMinute` > 3000, reject dimensions <= 0
    - [x] 8.9 `TenantConfigurationActor` test: `forceReindex=true` sets `ReindexRequired` flag on stored config
    - [x] 8.10 `TenantConfigurationActor` test: corrupted actor state (bad JSON) returns default config instead of throwing — prevents permanent tenant lockout

### Review Findings

- [x] \[Review\]\[Patch\] Provider-specific configs are accepted even though `EmbeddingClient` always targets Google [src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:76]
- [x] \[Review\]\[Patch\] Persisting synthesized defaults makes a tenant's first custom config look like a breaking reindex change [src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs:49]
- [x] \[Review\]\[Patch\] `PUT /embedding-config` returns the request body instead of the actor's persisted state [src/Hexalith.Memories.Server/Program.cs:153]
- [x] \[Review\]\[Patch\] Non-breaking updates can silently clear the server-tracked `ReindexRequired` flag [src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs:69]
- [x] \[Review\]\[Patch\] Tenant embedding-config endpoints skip tenant ID validation before actor access [src/Hexalith.Memories.Server/Program.cs:116]
- [x] \[Review\]\[Patch\] Conflict handling depends on a custom actor exception crossing the Dapr actor boundary intact [src/Hexalith.Memories.Server/Program.cs:163]
- [x] \[Review\]\[Patch\] Deserialized actor state is trusted without re-validating the stored config [src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs:73]
- [x] \[Review\]\[Patch\] API key caching never invalidates rotated secrets that reuse the same key name [src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:99]

## Dev Notes

### Architecture Patterns & Constraints

**Decision D4:** Google embedding only in MVP. IEmbeddingProvider abstraction makes additions trivial.
**Decision D9:** No premature interfaces — concrete classes until second implementation. The AC phrase "IEmbeddingProvider pattern (concrete class, not interface)" means: use a concrete `EmbeddingClient` class (as exists today), structured so adding a second provider is trivial (new class + DI registration). Do NOT create an `IEmbeddingProvider` interface now.
**Decision D24:** DAPR Actors for per-tenant stateful singletons. Actor ID = tenant ID.
**Decision D25:** Workflows orchestrate, actors maintain state, activities do I/O.

### Critical: Google text-embedding-004 Deprecation

**text-embedding-004 was deprecated on January 14, 2026.** The current EmbeddingClient hardcodes this deprecated model.

**Replacement:** `gemini-embedding-001`

- REST endpoint: `https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent`
- Default dimensions: 3072 (supports 768, 1536, 3072 via `output_dimensionality` param)
- Same API key format (`x-goog-api-key` header)
- Request payload now requires `output_dimensionality` field when using non-default dimensions

**Action required in this story:**

1. Update the default config to use `gemini-embedding-001` with `output_dimensionality: 768` for backward compatibility with existing 768-dim vectors
2. Make the endpoint URL, model name, and dimensions configurable per tenant
3. Add `output_dimensionality` to the request JSON payload in `EmbeddingClient`

**Epics AC deviation:** The epics file AC #1 example references `text-embedding-004`. This story intentionally overrides to `gemini-embedding-001` based on the January 2026 deprecation. The AC intent (configurable provider/model) is preserved; only the example default value changes.

### Configuration Storage Pattern

Use a **DAPR Actor** (`TenantConfigurationActor`) for tenant configuration. This aligns with D24 (actors for per-tenant stateful singletons) and the existing `EmbeddingRateLimiterActor` pattern.

**Why actor over state store directly?**

- Actor state persistence before every response (architecture constraint line 73)
- Actor ID = tenant ID (consistent with rate limiter pattern)
- Default config returned for new tenants without explicit provisioning
- Future: will hold more tenant config beyond embedding (rate limits, backend selection)

**State key:** `"embeddingConfig"` stores `TenantEmbeddingConfig` as JSON in Redis actor state store.

**Scaling note:** Each `GenerateEmbeddingActivity` invocation reads config from `TenantConfigurationActor` (one actor call per embedding). At NFR5 throughput (100 units/min), this is 100 actor reads/min. Acceptable for MVP — actors are lightweight singletons backed by local Redis. Document as known scaling consideration for Phase 3.

### Provider Configuration Fields (from PRD)

| Field                | Purpose                                                  | Source                          |
| -------------------- | -------------------------------------------------------- | ------------------------------- |
| `provider`           | google / openai / mistral / custom                       | Tenant config                   |
| `model`              | Specific model ID (e.g., "gemini-embedding-001")         | Tenant config                   |
| `dimensions`         | Vector dimensions (determines Redis Vector index schema) | Derived from provider/model     |
| `rateLimitPerMinute` | Throttle ceiling for embedding rate limiter actor        | Tenant config                   |
| `apiSecretKeyName`   | DAPR secret key name for API key                         | Tenant config                   |
| `reindexRequired`    | Flag tracking pending reindex after config change        | Actor state (set automatically) |

All fields are on a single `TenantEmbeddingConfig` record in `Contracts/V1/`.

### Reindex Warning (AC #4)

**Critical constraint from PRD:** "Redis Vector Search index schema is fixed at creation — switching embedding providers requires full reindex of that tenant's data. This is a migration operation, not a configuration change."

When `SetEmbeddingConfigAsync` detects a change to provider, model, or dimensions:

- If `forceReindex` is false: throw `EmbeddingConfigChangeException` with message explaining reindex requirement
- If `forceReindex` is true: save the new config with `ReindexRequired = true` (actual reindex workflow is Epic 5+, not this story — but tracking the flag now saves a refactor later)

**Eventual consistency of config changes:** Configuration changes take effect on the next `GenerateEmbeddingActivity` invocation. In-flight ingestion workflows continue with previous configuration. Document this in the PUT endpoint response: "Changes apply to new ingestion requests. In-flight workflows are unaffected."

### FR70: Tracking Provider Per Memory Unit

`EmbeddingResult` already carries `Provider` and `Dimensions` fields. These flow through the ingestion workflow into `MemoryUnit.EmbeddingProvider` and `MemoryUnit.EmbeddingDimensions`. This story ensures these values come from tenant config rather than hardcoded constants.

### EmbeddingClient Refactoring Approach

The current `EmbeddingClient` has hardcoded constants. Refactor to accept configuration via a method parameter or factory pattern:

**Approach: Pass config to GenerateAsync, register client as singleton**

```csharp
// Current:
public virtual async Task<float[]> GenerateAsync(string text, string tenantId, CancellationToken ct)

// Refactored:
public virtual async Task<float[]> GenerateAsync(
    string text, string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
```

This keeps `EmbeddingClient` as a single class (no interface needed) while making it config-driven. The `GenerateEmbeddingActivity` reads tenant config from actor, then passes it to the client.

**Registration change:** `AddHttpClient<EmbeddingClient>()` becomes `AddSingleton<EmbeddingClient>()` with `IHttpClientFactory` injected. This ensures the `ConcurrentDictionary<string, string>` API key cache persists across requests. `IHttpClientFactory` handles `HttpClient` pooling and socket lifecycle internally.

**Endpoint URL derivation:** Construct from provider + model:

```
Google: https://generativelanguage.googleapis.com/v1beta/models/{model}:embedContent
```

For MVP, only Google URL pattern is needed. When OpenAI is added (Phase 1.5), a new concrete client class or URL pattern is added.

### Rate Limiter Integration

The `EmbeddingRateLimiterActor` already has `SetCeilingAsync(int ceiling)`. The `GenerateEmbeddingActivity` should:

1. Read tenant config from `TenantConfigurationActor`
2. Call `rateLimiter.SetCeilingAsync(config.RateLimitPerMinute)` unconditionally before `TryConsumeAsync()`
3. This ensures the rate limiter uses the tenant-specific ceiling, not the hardcoded 1500 default

**Shared API key caveat:** Per-tenant rate limiters enforce independent ceilings, but if tenants share the same API key (same `apiSecretKeyName`), they share Google's actual rate limit. Tenant A bursting can cause 429s for Tenant B despite Tenant B having local budget remaining. This is a known MVP limitation (PRD line 695, architecture Phase 3 deferred). Document in PUT endpoint response.

### Existing Files to Modify

| File                                                       | Change                                                                                                                                                                                                                                                                                       |
| ---------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Server/Ingestion/EmbeddingClient.cs`                      | Accept `TenantEmbeddingConfig` param in GenerateAsync, construct URL from config, add output_dimensionality to payload, remove hardcoded constants, replace `_apiKey` field with `ConcurrentDictionary<string, string>`, change from typed HttpClient to singleton with `IHttpClientFactory` |
| `Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` | Read tenant config from TenantConfigurationActor, pass to EmbeddingClient, set rate limiter ceiling                                                                                                                                                                                          |
| `Server/Activities/Ingestion/EmbeddingResult.cs`           | No change needed (Provider/Dimensions already dynamic)                                                                                                                                                                                                                                       |
| `Server/Activities/Ingestion/EmbeddingInput.cs`            | No change needed (TenantId suffices)                                                                                                                                                                                                                                                         |
| `Server/Program.cs`                                        | Register TenantConfigurationActor, add REST endpoints                                                                                                                                                                                                                                        |

### New Files to Create

| File                                                   | Purpose                                                                                           |
| ------------------------------------------------------ | ------------------------------------------------------------------------------------------------- |
| `Contracts/V1/TenantEmbeddingConfig.cs`                | Sealed record: Provider, Model, Dimensions, RateLimitPerMinute, ApiSecretKeyName, ReindexRequired |
| `Server/Ingestion/EmbeddingProviderDefaults.cs`        | Static class with Google() factory + Validate()                                                   |
| `Server/Ingestion/EmbeddingConfigChangeException.cs`   | Exception for reindex-required config changes                                                     |
| `Server/Actors/ITenantConfigurationActor.cs`           | DAPR actor interface                                                                              |
| `Server/Actors/TenantConfigurationActor.cs`            | Actor implementation                                                                              |
| `Tests/.../EmbeddingProviderDefaultsTests.cs`          | Default config and validation tests                                                               |
| `Tests/.../TenantConfigurationActorTests.cs`           | Actor behavior tests                                                                              |
| `Tests/.../TenantEmbeddingConfigSerializationTests.cs` | Serialization round-trip tests                                                                    |

### Project Structure Notes

- New contracts go in `src/Hexalith.Memories.Contracts/V1/` (consistent with MemoryUnit, IngestionInput)
- New actors go in `src/Hexalith.Memories.Server/Actors/` (consistent with EmbeddingRateLimiterActor)
- New ingestion-related classes go in `src/Hexalith.Memories.Server/Ingestion/` (consistent with EmbeddingClient)
- Tests mirror source structure under `tests/`

### Testing Standards

- **Framework:** xUnit + Shouldly + NSubstitute
- **Actor testing:** Extract business logic into testable classes (like RateLimiterLogic). Test actor via logic class, not DAPR infrastructure.
- **Activity testing:** Mock IActorProxyFactory, substitute ITenantConfigurationActor, call RunAsync directly
- **Serialization:** Round-trip test every new sealed record via MemoriesJsonContext
- **Time control:** FakeTimeProvider where time-dependent logic exists
- **Patterns:** `result.ShouldBe(expected)`, `Should.Throw<T>()`, `Substitute.For<IInterface>()`

### Code Conventions

- Sealed records with `required init` properties for mandatory fields
- `field ??= []` for collection initialization
- File-scoped namespaces, Allman braces
- Activities: no exception catching, no CancellationToken handling (propagate to workflow)
- Actor state: persist before every response via `StateManager.SetStateAsync()`
- Copyright header on every file (ITANEO)

### Previous Story Intelligence

**From Story 1.4 (Embedding Generation):**

- EmbeddingClient uses typed HttpClient with 30s timeout
- API key cached in `_apiKey` field per instance — **this story must change `EmbeddingClient` to singleton with `IHttpClientFactory` and replace `_apiKey` with `ConcurrentDictionary<string, string>` keyed by `apiSecretKeyName`**
- Secret retrieval via `DaprClient.GetSecretAsync("secretstore", "google-embedding-api-key")`
- Rate limiter logic extracted into `RateLimiterLogic` for testability (TimeProvider-injected)
- 429 responses throw `EmbeddingRateLimitException`, workflow retry handles it

**From Story 1.6 (Workflow Orchestration):**

- GenerateEmbeddingActivity returns hardcoded `new EmbeddingResult(vector, "google:text-embedding-004", 768)` — must be dynamic
- Workflow calls activities via `context.CallActivityAsync<EmbeddingResult>(nameof(GenerateEmbeddingActivity), input, retryOptions)`
- EmbeddingInput already carries TenantId — no input changes needed

**From Story 1.5 (Three-Backend Indexing):**

- Index naming: `{tenantId}:{model-version}:syntactic` supports future model versions (D10)
- Vector dimensions must match index schema — mismatch causes Redis error

### Anti-Patterns to Avoid

1. **DO NOT create an IEmbeddingProvider interface** — D9 says no premature interfaces. Concrete EmbeddingClient is sufficient for MVP (Google only).
2. **DO NOT use IConfiguration/appsettings for tenant config** — per-tenant config must be in actor state (DAPR actor pattern), not static config files.
3. **DO NOT build a full tenant provisioning workflow** — that's Epic 5. This story only adds embedding config storage and retrieval.
4. **DO NOT implement actual reindex logic** — AC #4 only requires a warning/exception, not execution of reindex.
5. **DO NOT add OpenAI/Mistral provider implementations** — MVP is Google only (D4). The pattern should make additions easy, but don't implement them.
6. **DO NOT break existing tests** — refactoring EmbeddingClient and GenerateEmbeddingActivity must maintain all existing test scenarios.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.7] — Acceptance criteria and user story
- [Source: _bmad-output/planning-artifacts/architecture.md#D4] — Google embedding only in MVP
- [Source: _bmad-output/planning-artifacts/architecture.md#D9] — No premature interfaces
- [Source: _bmad-output/planning-artifacts/architecture.md#D24] — DAPR Actors for per-tenant singletons
- [Source: _bmad-output/planning-artifacts/architecture.md#D25] — Workflow-Actor-Activity separation
- [Source: _bmad-output/planning-artifacts/prd.md#Embedding Provider Management] — FR68, FR69, FR70
- [Source: _bmad-output/planning-artifacts/prd.md#Configuration per tenant table] — Field definitions
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting #5] — Rate limiting pattern
- [Source: _bmad-output/planning-artifacts/architecture.md#Line 260] — ITenantInfrastructureResolver pattern
- [Source: _bmad-output/implementation-artifacts/1-6-ingestion-workflow-orchestration.md] — Previous story patterns
- [Source: Google AI Docs] — gemini-embedding-001 replaces deprecated text-embedding-004 (Jan 14, 2026)

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- NSubstitute cannot proxy `ILogger<T>` when T is `internal sealed` (Castle.DynamicProxy limitation). Fixed by using `NullLogger<T>.Instance`.
- `HttpClient` disposed after `using` block in `GenerateAsync` — mock `IHttpClientFactory` must return new `HttpClient` per call in tests that invoke `GenerateAsync` multiple times.

### Completion Notes List

- Task 1: Created `TenantEmbeddingConfig` sealed record in `Contracts/V1/` with all 6 fields. Registered in `MemoriesJsonContext`. 4 serialization tests pass.
- Task 2: Created `EmbeddingProviderDefaults` static class with `Google()` factory and `Validate()` method using source-generated regex for apiSecretKeyName validation. 21 tests pass including valid/invalid key names, boundary conditions.
- Task 3: Created `ITenantConfigurationActor` interface and `TenantConfigurationActor` DAPR actor. Implements change detection (provider/model/dimensions), forceReindex bypass with ReindexRequired flag, defensive JSON deserialization fallback to defaults. Registered in Program.cs. 9 actor tests pass.
- Task 4: Refactored `EmbeddingClient` from typed HttpClient to singleton with `IHttpClientFactory`. Replaced `_apiKey` with `ConcurrentDictionary<string, string>` keyed by `apiSecretKeyName`. `GenerateAsync` now accepts `TenantEmbeddingConfig`, builds endpoint URL dynamically (`/v1beta/models/{model}:embedContent`), includes `output_dimensionality` in request JSON (snake_case). All 13 existing tests updated and passing.
- Task 5: Refactored `GenerateEmbeddingActivity` to read tenant config from `TenantConfigurationActor`, pass config to `EmbeddingClient`, call `SetCeilingAsync` before `TryConsumeAsync`, return dynamic `EmbeddingResult` with `{provider}:{model}` format. All 6 existing tests updated and passing.
- Task 6: Added `GET /api/tenants/{tenantId}/embedding-config` and `PUT /api/tenants/{tenantId}/embedding-config?forceReindex=false` endpoints in Program.cs. PUT validates config, returns 409 Conflict with structured error body on breaking changes. 4 endpoint tests pass.
- Task 7: Verified `EmbeddingInput` needs no changes (TenantId suffices). Updated `EmbeddingResult` doc comments to reflect configurable model.
- Task 8: Implemented all ATDD tests (previously skipped). 6 EmbeddingClientConfig tests, 4 GenerateEmbeddingActivityConfig tests, plus all comprehensive tests from Tasks 1-6. Full suite: 331 tests pass (84 contracts + 228 server + 19 integration), 0 failures, 0 skipped.
- Created `EmbeddingConfigChangeException` with TenantId, CurrentConfig, ProposedConfig, AffectedFields properties for structured 409 responses.

### Change Log

- 2026-03-31: Story 1.7 implemented — embedding provider configuration with per-tenant config storage, configurable EmbeddingClient, REST endpoints, and comprehensive test coverage.

### File List

**New files:**

- src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs
- src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
- src/Hexalith.Memories.Server/Ingestion/EmbeddingConfigChangeException.cs
- src/Hexalith.Memories.Server/Actors/ITenantConfigurationActor.cs
- src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs

**Modified files:**

- src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs
- src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs
- src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs
- src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingResult.cs
- src/Hexalith.Memories.Server/Program.cs

**Test files (new/updated):**

- tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs
- tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs
- tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs
- tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs
- tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientConfigTests.cs
- tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs
- tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityConfigTests.cs
- tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs
