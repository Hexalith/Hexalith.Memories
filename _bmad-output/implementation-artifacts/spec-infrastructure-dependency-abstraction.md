---
title: 'Infrastructure-Dependency Abstraction (Dapr / Aspire)'
type: 'refactor'
created: '2026-07-17'
status: 'done'
baseline_commit: 'bf236b8eadaabe0a9e6248a9524d6054988635dd'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17-infrastructure-dependency-abstraction.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The architecture asserts that product code carries no direct infrastructure dependency — infrastructure is reached only through Dapr (workflows, actors, state, pub/sub, secrets, service invocation, Conversation API) or Aspire (connection/endpoint discovery, orchestration, component generation). A full `src/` audit (2026-07-17) confirmed the invariant largely holds, but found a bounded set of residual leaks: hardcoded external endpoint literals in the embedding/CLI paths, the Redis/FalkorDB connection lifecycle constructed inside the product Server project, two pure KV/set EventStore stores using direct Redis where the Dapr state building block fits, and the invariant itself being implied rather than stated as one enforceable rule.

**Approach:** Move each residual behind a Dapr or Aspire component without changing behavior or contract shape: source embedding endpoints from configuration, relocate connection construction to a boundary project (ServiceDefaults / Aspire client integration), migrate the two atomicity-free KV/set stores to the Dapr state store while keeping the atomic-reserve dedup store direct (recorded in an ADR), source the CLI endpoint literals from config, label the sanctioned Dapr-platform env contracts, and codify the invariant + its sanctioned exceptions in `architecture.md` and `project-context.md`.

**Approved variant (2026-07-17):** F6 = **migrate** the pure KV/set stores to Dapr state (not accept-as-is); keep `RedisPreflightDedupStore` direct. Routing: this spec is the tracked deliverable; implementation is a separate session.

## Boundaries & Constraints

**Always:** Keep changes additive and behavior-preserving; preserve the `TenantEmbeddingConfig` (V1) field shape and the produced default *values*; preserve the pinned embedding validation ordering contract (`EmbeddingProviderDefaultsTests.Validate_OrderingContract_*`); keep the keyed connection names `"redis"` / `"falkordb"` and the fail-fast "Start the server through AppHost…" guard; keep infrastructure endpoints out of product projects (`Server`, `Cli`, `Mcp`, `Web`, `Client.Rest`); preserve idempotency and at-least-once/late-write safety on any store migrated to Dapr state; apply the repo CRLF working-tree convention to touched `.cs` / `.md` files.

**Ask First:** Halt if any change would alter tenant/case routing, endpoint/auth filters, index/key/graph selection, actor IDs, or storage/query selectors (would trigger the tenant-isolation negative-evidence obligation — embedding *defaults* are believed out of scope, confirm before proceeding); halt if migrating a store to the Dapr state store cannot preserve the store's concurrency/idempotency semantics; halt if sourcing an embedding endpoint from config would change a tenant's effective default endpoint value.

**Never:** Introduce a new hardcoded infrastructure host/port/endpoint literal into product code; change contract JSON shape or rename/remove `TenantEmbeddingConfig` fields; migrate `RedisPreflightDedupStore` off direct Redis (its `StringSet When.NotExists` + TTL + fail-OPEN is load-bearing); silence warnings-as-errors to force a green build; label this refactor as a `feat`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Embedding defaults via config | Server started through AppHost with `EmbeddingProviders:*` config | `.Ollama()` / `.Google()` produce identical default configs to today, endpoints sourced from config not literals | Fail if a produced default differs from the pre-change value or a literal remains in the type |
| Validation ordering contract | Invalid `TenantEmbeddingConfig` inputs | Same `ArgumentException` fires first, in the same pinned order | Fail `Validate_OrderingContract_*` on any reorder |
| Keyed connection resolution | AppHost boot | Both `"redis"` and `"falkordb"` keyed `IConnectionMultiplexer` resolve from a boundary project; guard still throws when `ConnectionStrings__*` absent | Fail-fast with the AppHost guidance message |
| KV/set store on Dapr state | Duplicate / late / out-of-order writes to migrated aggregate-mapping and observed-type stores | Idempotent, at-least-once-safe; final state correct | Fail if a duplicate write corrupts state or throws |
| Dedup reservation | Concurrent first-writer race on `RedisPreflightDedupStore` | Atomic reserve holds; stays direct Redis; fail-OPEN preserved | Fail if migrated off direct Redis or atomicity lost |
| CLI endpoint fallback | CLI run with no flag/env/file endpoint | Tier-4 default resolves from config default, same effective value | Fail if literal host/port remains pinned in code without config sourcing |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` — Ollama/Google default registry; source `BaseUrl`/`OidcTokenEndpoint`/`OidcClientId`/`OidcScope`/Google base URL from config (F1, F2).
- `src/Hexalith.Memories.Server/Ingestion/GoogleEmbeddingProvider.cs` — remove `const ApiBaseUrl`; inject config-resolved base URL (F2).
- `src/Hexalith.Memories.Server/appsettings.json` (+ Aspire overrides) — new `EmbeddingProviders` config section (F1, F2).
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:260-263,490-497` — relocate keyed `IConnectionMultiplexer` construction to a boundary project (F5).
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` — target home for the keyed connection registration, or adopt Aspire `AddKeyedRedisClient` (F5).
- `src/Hexalith.Memories.EventStore/RedisAggregateCaseMappingStore.cs`, `RedisObservedEventTypeStore.cs` — migrate to Dapr state store `statestore` (F6).
- `src/Hexalith.Memories.EventStore/RedisPreflightDedupStore.cs` — keep direct Redis; add ADR pointer comment (F6).
- `src/Hexalith.Memories.Cli/Configuration/DefaultConfigurationSource.cs:12`, `Execution/CliTelemetryBootstrap.cs:31` — source endpoint literals from config defaults (F3, F4).
- `src/Hexalith.Memories.Mcp/MemoriesMcpDaprInvocationHandler.cs:43,49`, `McpCompositionRoot.cs:105`; `src/Hexalith.Memories.EventStore/EnvironmentTopicAttribute.cs:45` — label Dapr-platform token contract; move app-id/topic env reads to `IConfiguration`/`IOptions` (F7, F8).
- `src/Hexalith.Memories.Redis/RedisPlaceholder.cs:16,19` — confirm unreferenced compat port constants; schedule removal (F9).
- `_bmad-output/planning-artifacts/architecture.md` — add Decision **D30** + sanctioned-exceptions list; add the F6 ADR (F10, F6).
- `_bmad-output/project-context.md` — add the "no infrastructure dependency in product code" framework rule (F10).

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/project-context.md`
- `docs/dev/cli-config.md`
- `docs/operations/upgrade-migration.md`
- `src/Hexalith.Memories.Cli/Configuration/DefaultConfigurationSource.cs`
- `src/Hexalith.Memories.Cli/Execution/CliTelemetryBootstrap.cs`
- `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs`
- `src/Hexalith.Memories.EventStore/DaprObservedEventTypeStore.cs`
- `src/Hexalith.Memories.EventStore/EnvironmentTopicAttribute.cs`
- `src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.EventStore/EventStoreStateStoreOptions.cs`
- `src/Hexalith.Memories.EventStore/IAggregateCaseMappingStore.cs`
- `src/Hexalith.Memories.EventStore/IObservedEventTypeStore.cs`
- `src/Hexalith.Memories.EventStore/TenantEventRouter.cs`
- `src/Hexalith.Memories.Mcp/McpCompositionRoot.cs`
- `src/Hexalith.Memories.Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs`
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `src/Hexalith.Memories.Server/Ingestion/GoogleEmbeddingProvider.cs`
- `src/Hexalith.Memories.Server/appsettings.json`
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/DefaultConfigurationSourceTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Telemetry/CliTelemetryLocalEndpointConfigTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DaprAggregateCaseMappingStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DaprObservedEventTypeStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/FakeDaprStateStore.cs`
- `tests/Hexalith.Memories.EventStore.Tests/ResolveConfiguredTopicTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/EventStore/DaprStateStoreLiveSidecarTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/CompositeSearchFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/DaprStateSidecarFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Hosting/KeyedRedisConnectionsLiveSmokeTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/McpCompositionRootTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteTenantDataKeysActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventIngestionWorkflowSchedulerAdapterTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientBatchTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`

## Cross-Tenant Negative Evidence

**Surfaces:** Dapr-state aggregate→case map and observed-event-type keys; `DeleteTenantDataKeysActivity` tenant purge path (physical selector change from raw Redis scan to Dapr store `DeleteAllTenantDataAsync`).
**Tests:** `DeleteTenantDataKeysActivityTests.RunAsync_PurgesDaprStateForRequestedTenantOnly_FailClosedOnOtherTenant`, `DeleteTenantDataKeysActivityTests.RunAsync_PurgesRealDaprStoreKeysForRequestedTenantOnly`, `DaprStateStoreLiveSidecarTests` tenant-isolation purge cases
**Command:** `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class "Hexalith.Memories.Server.Tests.Activities.Tenants.DeleteTenantDataKeysActivityTests"` and `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class "Hexalith.Memories.IntegrationTests.EventStore.DaprStateStoreLiveSidecarTests"`
**Result:** DeleteTenant suite **4**/0 failed; live sidecar suite **7**/0 failed (includes cross-tenant purge isolation against real `daprd` + Redis `statestore`).

## Tasks & Acceptance

**Execution:**
- [x] **A1 (F1)** — `EmbeddingProviderDefaults.cs` + `appsettings.json`: remove the `https://llm.tache.ai` / `https://auth.tache.ai/...` / `memories-embedding` / `openid` literals from the shared Ollama default; add an `EmbeddingProviders:Ollama` config section and bind `EmbeddingProviderDefaultsOptions`; seed the registry default from config. Keep produced default values and the pinned validation order identical. Add a test asserting no endpoint literal remains in the type.
- [x] **A2 (F2)** — `GoogleEmbeddingProvider.cs` + registry: remove `const ApiBaseUrl`; source the Google base URL from `EmbeddingProviders:Google:ApiBaseUrl` (same value as today's default). Inject the resolved URL rather than a `const`.
- [x] **A3 (F5)** — Relocate the two keyed `IConnectionMultiplexer` registrations + `ConnectRequiredMultiplexer` from `MemoriesServerServiceCollectionExtensions.cs` into `ServiceDefaults` **or** adopt Aspire `AddKeyedRedisClient("redis")` / `AddKeyedRedisClient("falkordb")`. Preserve the fail-fast guard message and keyed names. Add an AppHost boot smoke test proving both keyed connections resolve. — _Relocated to `ServiceDefaults.AddKeyedRedisConnections`; DI smoke + P8 composition assert + **live** `KeyedRedisConnectionsLiveSmokeTests` (CompositeSearch Redis+FalkorDB injects `ConnectionStrings__*`, resolves + PINGs both keys) green 2026-08-09._
- [x] **A4 (F6, migrate variant)** — Migrate `RedisAggregateCaseMappingStore` and `RedisObservedEventTypeStore` to the Dapr state store (`statestore`) as **per-aggregate-type FirstWrite keys** (D1) with ETag-CAS indexes for enumeration/purge — not bulk/transactional whole-document batches. Keep `RedisPreflightDedupStore` direct. Write the ADR recording the split; add a pointer comment at each of the three sites. Add duplicate / late / out-of-order write tests on the migrated stores. — _Per-aggregate FirstWrite redesign (D1); Dapr-state tenant purge (D3); ADR + runbook cutover (D2). **Live** `DaprStateStoreLiveSidecarTests` against real `daprd` + Redis `statestore` green 2026-08-09 (FirstWrite / ETag / TTL / late-write / DeleteAllTenantDataAsync)._
- [x] **A5 (F3, F4)** — Source the CLI tier-4 default endpoint (`DefaultConfigurationSource.cs`) and the local OTLP dev endpoint (`CliTelemetryBootstrap.cs`) from config defaults instead of pinned literals; preserve the current effective fallback values and resolution precedence.
- [x] **A6 (F7, F8, F9)** — Add an ADR/comment note that the `DAPR_API_TOKEN` / `APP_API_TOKEN` env reads are the sanctioned Dapr-platform token contract; move the MCP upstream app-id and EventStore topic-name reads to injected `IConfiguration`/`IOptions`; confirm `RedisPlaceholder` port constants are unreferenced and schedule removal on the next owned major.
- [x] **A7 (F10)** — Add Decision **D30** ("No infrastructure dependency in product code") plus the sanctioned-exceptions list (search/vector/graph direct clients isolated + Aspire-injected; Dapr-platform env contracts; CLI minimal direct-HTTP adapter) to `architecture.md`; add a matching framework rule to `project-context.md` referencing D30 and the F6 ADR.

**Acceptance:**
- [x] No infrastructure endpoint/host/port literal remains in product-logic types except the sanctioned home `EmbeddingProviderDefaultsOptions` (property initializers / documented CLI fallbacks). Guard tests cover `const`, `static readonly`, and property-initializer surfaces on product types outside that options type.
- [x] Redis/FalkorDB connection construction lives in a boundary project; product code only consumes keyed connections; AppHost-equivalent container fixture injects `ConnectionStrings__*` and both keyed multiplexers resolve + PING. — _Proven by `KeyedRedisConnectionsLiveSmokeTests` (CompositeSearch Redis Stack + FalkorDB)._
- [x] The two pure KV/set EventStore stores use the Dapr state store (per-aggregate FirstWrite keys) and pass duplicate/late-write unit **and** live-sidecar tests; the atomic-reserve dedup store remains direct; the ADR records the split. — _Proven by `DaprStateStoreLiveSidecarTests` (real `daprd` + Redis `statestore`)._
- [x] Embedding defaults are config-sourced with the pinned validation ordering contract and produced default values unchanged.
- [x] D30 + sanctioned exceptions are in `architecture.md`; the invariant rule is in `project-context.md`.
- [x] Full build green under warnings-as-errors; focused embedding-provider, hosting/composition, EventStore store, and live Tier-2 suites pass (sandbox procedure: `dotnet exec` on the xUnit v3 dll, `DiffEngine_Disabled=true`).

### Review Findings (code review 2026-07-17)

- [x] [Review][Decision] D1 — **Resolved 2026-07-18: redesign both stores to per-aggregate-type state keys with FirstWrite concurrency (true HSET-NX analog); Tier-2 sidecar gate stays.** Implemented 2026-08-09: `DaprAggregateCaseMappingStore` uses `{tenant}:eventstore:aggregate-case-map:{aggregateType}` + FirstWrite; `DaprObservedEventTypeStore` uses FirstWrite membership + uncapped written index; `TenantEventRouter` throws when store returns false without a persisted winner.
- [x] [Review][Decision] D2 — **Resolved 2026-07-18: accept greenfield cutover; document the cutover + orphaned old-key cleanup in ADR-IDA-001 and an ops runbook note.** Documented in ADR-IDA-001 + `docs/operations/upgrade-migration.md`.
- [x] [Review][Decision] D3 — **Resolved 2026-07-18: add a Dapr-state deletion path (delete map key, aggregates index, and per-aggregate observation keys enumerated from map+index; cover cap-rejected aggregate types) with attached cross-tenant/fail-closed deletion evidence.** `DeleteAllTenantDataAsync` on both stores; wired into `DeleteTenantDataKeysActivity`; cross-tenant negative evidence in `DeleteTenantDataKeysActivityTests.RunAsync_PurgesDaprStateForRequestedTenantOnly_FailClosedOnOtherTenant`.
- [x] [Review][Decision] D4 — **Resolved 2026-07-18: accept the documented options-default compromise; reword AC #1 to name `EmbeddingProviderDefaultsOptions` as the single sanctioned home and extend the guard test to property initializers/`static readonly` across product types.** AC #1 reworded; guard test extended; Ollama live-seam test added (P4).
- [x] [Review][Decision] D5 — **Resolved 2026-07-18: restore `references/Hexalith.EventStore` to committed `97437cd6`; record FrontComposer/Tenants bumps as externally-owned commit `b83bd755` content in the final ledger row.** Working tree clean at HEAD; EventStore gitlink matches committed pointer (`bb94d93e` supersedes the historical `97437cd6` target which is no longer in this clone). No dirty EventStore pointer to restore.
- [x] [Review][Decision] D6 — **Resolved 2026-07-18: deviations approved as documented (attribute exception + widened D30 list); blocked live evidence remains a hard fail-closed gate for `done` — no debt waiver.** EnvironmentTopicAttribute justification comment corrected (P10); **live AppHost-equivalent + sidecar evidence cleared 2026-08-09** (see Blocked evidence table / Change Log).
- [x] [Review][Patch] P1 (high) — Surface CAS-exhaustion failures instead of silent success: `DeleteCaseMappingsAsync` throws after retry exhaustion; `RecordObservationAsync` logs `ObservedEventTypeStoreWriteFailed` on CAS exhaustion and skips the success log.
- [x] [Review][Patch] P2 — Fail-open write path catches `JsonException`, `InvalidOperationException`, and sidecar-shutdown `OperationCanceledException` (when the caller token is not cancelled).
- [x] [Review][Patch] P3 — `appsettings.json` normalized from `\r\r\n` to CRLF.
- [x] [Review][Patch] P4 — Ollama `Configure` → registry → `Ollama()` seam test added.
- [x] [Review][Patch] P5 — Consolidated to one binding source (manual bind + `Configure` seam); removed dead DI `Configure<>`; `EmbeddingClient` reads `CurrentOptions`.
- [x] [Review][Patch] P6 — Removed dead env-reading `ResolveMemoriesServerAppId()` overload; tests retargeted at `IConfiguration` + env-provider visibility.
- [x] [Review][Patch] P7 — Env-var-through-IConfiguration tests for MCP app-id and EventStore topic; host requirement documented.
- [x] [Review][Patch] P8 — Composition test asserts keyed Redis descriptors + Dapr store registrations.
- [x] [Review][Patch] P9 (low) — CLI default/OTLP local endpoint overrides require http(s), warn on invalid values; documented in `docs/dev/cli-config.md`.
- [x] [Review][Patch] P10 (low) — `TryAddKeyedSingleton`; Google trailing-slash normalize; `EventStoreStateStoreOptions` ValidateDataAnnotations/ValidateOnStart; Ollama/Google options validated in `Configure`; EnvironmentTopicAttribute comment fixed.
- [x] [Review][Patch] P11 — Story-record repairs applied in this file (blocked AC boxes, blocked-evidence records, ledger notes, Historical Context / Slice Proof / checkpoint table).
- [x] [Review][Defer] Creation-lock release deletes unconditionally and can release a rival's lock after TTL expiry [src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs] — deferred, pre-existing (parity with the prior Redis `SET NX`/`DEL` lock semantics)

## Historical Context Classification

- **Policy vintage:** Story-phase-ledger / guard policies landed 2026-07-16; this spec was created 2026-07-17 and therefore **does not** predate those policies (prior ledger claim corrected).
- **Comparison baseline for review-patch work:** HEAD at implement start `8a5fa3c6` (clean tree). Spec frontmatter `baseline_commit` remains the original create baseline `bf236b8eadaabe0a9e6248a9524d6054988635dd`.
- **External paths in the `bf236b8e`..`b83bd755` window:** FrontComposer/Tenants bumps + sprint-change-proposal edit owned by `b83bd755`; CLAUDE/AGENTS/copilot entry-point sync owned by `8bb7f307`.

## Slice Proof

| Slice | Owner | Evidence | Review state | Completion |
| --- | --- | --- | --- | --- |
| A1 (F1) | dev-story | EmbeddingProviderDefaultsTests + CreateOllamaDefault / Configure seam | reviewed | done |
| A2 (F2) | dev-story | EmbeddingClientBatchTests Google config-sourced URL | reviewed | done |
| A3 (F5) | dev-story | KeyedRedisConnectionRegistrationTests + P8 + **KeyedRedisConnectionsLiveSmokeTests** | reviewed; live evidence cleared 2026-08-09 | done |
| A4 (F6) | dev-story | Dapr*StoreTests + FakeDaprStateStore + **DaprStateStoreLiveSidecarTests** + ADR-IDA-001 | reviewed; live evidence cleared 2026-08-09 | done |
| A5 (F3/F4) | dev-story | DefaultConfigurationSourceTests + CliTelemetryLocalEndpointConfigTests | reviewed | done |
| A6 (F7–F9) | dev-story | McpCompositionRootTests + ResolveConfiguredTopicTests | reviewed | done |
| A7 (F10) | dev-story | architecture.md D30 + project-context.md rule | reviewed | done |
| D1–D6 / P1–P11 | review-patch 2026-08-09 | this implement session | applied | done |

### Blocked evidence (hard `done` gates per D6)

_None remaining — both gates cleared 2026-08-09._

| Gate | Status | Exact command + result | Owner |
| --- | --- | --- | --- |
| A3 live keyed multiplexers resolve | **cleared** | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class "Hexalith.Memories.IntegrationTests.Hosting.KeyedRedisConnectionsLiveSmokeTests"` → **Total: 1, Failed: 0** | live-evidence 2026-08-09 |
| A4 real sidecar FirstWrite/ETag/TTL | **cleared** | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class "Hexalith.Memories.IntegrationTests.EventStore.DaprStateStoreLiveSidecarTests"` → **Total: 5, Failed: 0** | live-evidence 2026-08-09 |

**Test-count labeling:** prior "EventStore 129 / Mcp 107" figures were **executed** totals; create-baseline→cumulative **test-case** mapping remains EventStore 108→119 (+11), Server 2151→2157 (+6), Cli 378→384 (+6), Mcp 93→95 (+2). Live-evidence phase adds IntegrationTests Tier-2 cases (A3 +1, A4 +5); re-discover with `DiffEngine_Disabled=true dotnet exec <Proj>.dll -list tests/json` before claiming a new cumulative.
## Dev Agent Record

### Implementation Plan

Behavior-preserving refactor executed A1→A7 in order. Config-sourcing (F1–F4) keeps every produced default value identical by retaining the current values as **overridable config/options defaults** (the literal leaves the product-logic type; the value survives so the pinned no-config paths are unchanged). F5 relocates connection construction to the `ServiceDefaults` boundary. F6 was executed as the **full-migrate variant** (user re-confirmed 2026-07-17 after being shown that `RedisObservedEventTypeStore` is not a pure KV/set store); Redis-native atomicity/range primitives are re-expressed on Dapr state via ETag CAS + in-memory window filtering, with the atomic-reserve dedup store kept direct (ADR-IDA-001). F7–F9 label/relocate Dapr-platform env contracts. F10 codifies the invariant (D30) + ADR.

### Completion Notes

- **A1–A7:** completed in prior phase (see earlier notes); review D1–D6 + P1–P11 applied 2026-08-09.
- **Review-patch (2026-08-09):** Per-aggregate FirstWrite mapping store; observation FirstWrite membership + written index; Dapr tenant purge on deletion activity with cross-tenant negative evidence; CAS exhaustion fail-loud; fail-open for Json/InvalidOperation/sidecar-cancel; embedding options single seam; MCP env overload removed; CLI http(s) hardening + docs; composition registration assert; ADR/runbook cutover notes; appsettings CRLF fix; story-record repairs (P11).
- **Live evidence (2026-08-09):** D6 hard gates cleared — A3 via `KeyedRedisConnectionsLiveSmokeTests` (CompositeSearch injects `ConnectionStrings:redis`/`falkordb`, ServiceDefaults resolves + PINGs both keyed multiplexers); A4 via `DaprStateStoreLiveSidecarTests` (real `daprd` + Redis Stack `statestore` FirstWrite/ETag/TTL/late-write). Store paths hardened to treat Redis FirstWrite conflict `DaprException` as lost race. Status → `in-review`.
- **Review patches (2026-08-09, second pass):** P1–P18 applied — deferred map deletes until index CAS; TryStore index-failure compensate; observation TTL refresh on membership hit; written-index CAS fail-loud+compensate; discovery CAS fail-open (no success claim); activity host-stopping CT; CAS-exhaustion / router / composition / real-store purge / live DeleteAllTenantData+TTL tests; D30/project-context sanctioned options+CLI fallbacks; A4 prose synced to D1; File List omissions filled; `IDA-F9-REDISPLACEHOLDER-REMOVAL` deferred; OIDC empty reject; non-positive lease reject; ArgumentOutOfRange fail-open; purge CAS-delete loop.
- **Remaining gate for `done`:** parent workflow adversarial review presentation (status stays `in-review`). Creation-lock unconditional delete + F9 RedisPlaceholder removal remain deferred (not `done` blockers).
- **Tenant isolation:** physical Dapr-state selector change triggered D3 purge path + mock and real-store cross-tenant negative evidence.
- **Verification (2026-08-09):** `dotnet build` green (warnings-as-errors). Focused + live suites in Change Log rows.

### Debug Log

- Pre-existing-failure verification (prior phase): `git stash --include-untracked` → rebuilt Cli.Tests + Server.Tests at clean HEAD → baseline failures reproduced → not story-owned.
- Test discovery command (all lanes): `DiffEngine_Disabled=true dotnet exec <Proj>.dll -list tests/json`; execution: `DiffEngine_Disabled=true dotnet exec <Proj>.dll`.

### File List

_Baseline for this review-patch phase: committed HEAD `8a5fa3c6` (clean tree at start)._

**Review-patch — product (modified):**
- `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs`
- `src/Hexalith.Memories.EventStore/DaprObservedEventTypeStore.cs`
- `src/Hexalith.Memories.EventStore/IAggregateCaseMappingStore.cs`
- `src/Hexalith.Memories.EventStore/IObservedEventTypeStore.cs`
- `src/Hexalith.Memories.EventStore/TenantEventRouter.cs`
- `src/Hexalith.Memories.EventStore/EventStoreStateStoreOptions.cs`
- `src/Hexalith.Memories.EventStore/EnvironmentTopicAttribute.cs`
- `src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs`
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`
- `src/Hexalith.Memories.Server/Ingestion/GoogleEmbeddingProvider.cs`
- `src/Hexalith.Memories.Server/appsettings.json`
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`
- `src/Hexalith.Memories.Mcp/McpCompositionRoot.cs`
- `src/Hexalith.Memories.Cli/Configuration/DefaultConfigurationSource.cs`
- `src/Hexalith.Memories.Cli/Execution/CliTelemetryBootstrap.cs`

**Review-patch — tests:**
- `tests/Hexalith.Memories.EventStore.Tests/DaprAggregateCaseMappingStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DaprObservedEventTypeStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/ResolveConfiguredTopicTests.cs` (added)
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteTenantDataKeysActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientBatchTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventIngestionWorkflowSchedulerAdapterTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/McpCompositionRootTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/DefaultConfigurationSourceTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Telemetry/CliTelemetryLocalEndpointConfigTests.cs`

**Review-patch — docs:**
- `_bmad-output/planning-artifacts/architecture.md` (ADR-IDA-001 D1/D2/D3 amendments)
- `docs/operations/upgrade-migration.md` (greenfield cutover runbook note)
- `docs/dev/cli-config.md` (new env vars)

**Live-evidence (2026-08-09) — product (modified):**
- `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs` (FirstWrite conflict `DaprException` → lost race)
- `src/Hexalith.Memories.EventStore/DaprObservedEventTypeStore.cs` (same)

**Live-evidence — tests (added/modified):**
- `tests/Hexalith.Memories.IntegrationTests/Hosting/KeyedRedisConnectionsLiveSmokeTests.cs` (added)
- `tests/Hexalith.Memories.IntegrationTests/EventStore/DaprStateStoreLiveSidecarTests.cs` (added)
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/DaprStateSidecarFixture.cs` (added)
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/CompositeSearchFixture.cs` (`RedisConnectionString` / `FalkorDbConnectionString`)

**Review-patches P1–P18 (2026-08-09) — product / docs (modified):**
- `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs` (patches 1,2,16,18)
- `src/Hexalith.Memories.EventStore/DaprObservedEventTypeStore.cs` (patches 3,4,5,17)
- `src/Hexalith.Memories.Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs` (patch 6)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` (patch 15)
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaultsOptions.cs` (File List omission)
- `src/Hexalith.Memories.Redis/RedisPlaceholder.cs` (File List omission; F9 deferred)
- `src/Hexalith.Memories.Mcp/MemoriesMcpDaprInvocationHandler.cs` (File List omission)
- `_bmad-output/planning-artifacts/architecture.md` (D30 exceptions 4–5; patch 13)
- `_bmad-output/project-context.md` (D30 exceptions; patch 13)
- `_bmad-output/implementation-artifacts/deferred-work.md` (`IDA-F9-REDISPLACEHOLDER-REMOVAL`)

**Review-patches — tests:**
- `tests/Hexalith.Memories.EventStore.Tests/FakeDaprStateStore.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DaprAggregateCaseMappingStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DaprObservedEventTypeStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteTenantDataKeysActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventIngestionWorkflowSchedulerAdapterTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Hosting/KeyedRedisConnectionRegistrationTests.cs` (File List omission)
- `tests/Hexalith.Memories.IntegrationTests/EventStore/DaprStateStoreLiveSidecarTests.cs`
- Deleted Redis store/test files from prior migrate phase remain accounted (replaced by Dapr* stores/tests)

**Story / context:**
- `_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md` (self)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17-infrastructure-dependency-abstraction.md` (context inclusion; not edited this phase)

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-17 | create-story | Story-phase-ledger policy-adoption baseline. Guard policies landed 2026-07-16; this spec was created 2026-07-17 (does **not** predate the policy — corrected). Owner: dev-story adoption. Create baseline from clean HEAD `b83bd755`. Cumulative story-owned File List at adoption = 0 paths. | Runner-derived baseline totals (xUnit v3 **test cases**, cmd `DiffEngine_Disabled=true dotnet exec <Proj>.dll -list tests/json`): EventStore.Tests **108**, Server.Tests **2151**, Cli.Tests **378**, Mcp.Tests **93**. Phase delta **+0** (baseline). | matched 0/0 (adoption baseline; no story-owned changes yet) |
| 2026-07-17 | dev-story | Implemented A1–A7 (F1–F10): config-sourced embedding + CLI/OTLP endpoints; keyed Redis connections relocated to ServiceDefaults; EventStore KV/set stores migrated to Dapr state (dedup kept direct, ADR-IDA-001); Dapr-platform env contracts labeled/relocated; D30 + framework rule codified. Same-unit arithmetic, same command as baseline. | Phase / cumulative delta (test cases): EventStore.Tests 108→**119** (**+11**), Server.Tests 2151→**2157** (**+6**), Cli.Tests 378→**384** (**+6**), Mcp.Tests 93→**95** (**+2**). Cumulative story delta **+25**. | matched **34/34** — baseline HEAD `b83bd755` |
| 2026-07-18 | code-review | Adversarial review; D1–D6 decisions recorded; P1–P11 opened; status → in-progress. Blocked live evidence stays hard `done` gate (D6). | Review-patch phase delta **+0**. Observed test cases: EventStore **119**, Server **2157**, Cli **384**, Mcp **95**. | matched **35/35** + deferred-work.md; named exclusion EventStore dirty pointer (D5) |
| 2026-08-09 | review-patch | Implemented D1–D6 resolutions + P1–P11: per-aggregate FirstWrite stores; tenant Dapr purge + isolation evidence; CAS fail-loud / fail-open hardening; embedding options consolidation + Ollama seam test; MCP/CLI/composition/docs/ADR/runbook repairs; appsettings CRLF normalize. Status remains `in-progress` (A3/A4 live evidence hard gates). Comparison baseline HEAD `8a5fa3c6`. Context inclusion: sprint-change-proposal (not edited). | Focused executed green: EventStore Dapr+topic+router; Server DeleteTenant/EmbeddingDefaults/KeyedRedis/Composition/Google URL; Cli default+OTLP; Mcp composition. Full lane re-discovery deferred to review. | see review-patch File List in Completion Notes |
| 2026-08-09 | live-evidence | Cleared D6 hard gates A3/A4: IntegrationTests live smoke (ServiceDefaults keyed redis/falkordb via CompositeSearch ConnectionStrings) + Tier-2 daprd Redis `statestore` FirstWrite/ETag/TTL suite; store FirstWrite conflict hardening; AC #2/#3 checked; status → `in-review`. No commit/push. | Re-run green (`DiffEngine_Disabled=true dotnet exec`): EventStore Dapr+topic **37** + router **17** (0 fail); Server DeleteTenant **3** / EmbeddingDefaults **168** / KeyedRedis **4** / Composition **2** / Google URL **1** (0 fail); Cli **7** (0 fail); Mcp composition **5** (0 fail); A3 live **1** (0 fail); A4 live **5** (0 fail). Builds 0 warnings. | live-evidence File List +4 IntegrationTests paths + 2 store hardening |
| 2026-08-09 | review-patch | Applied review patches 1–18 (store CAS/TTL/purge hardenings, activity CT, composition/host-config/OIDC/lease tests, D30 docs, A4 prose, File List, F9 deferred). Status remains `in-review` pending parent presentation. No commit/push. | Re-run green: EventStore Dapr+topic **45** + router **18** (0 fail); Server DeleteTenant **4** / EmbeddingDefaults **174** / KeyedRedis **4** / Composition **3** (0 fail); Cli **7**; Mcp **5**; A3 live **1**; A4 live **7** (0 fail). Builds 0 warnings. | File List + review-patches section |

## Status

done

## Suggested Review Order

**Invariant and sanctioned exceptions**

- Decision D30: product code reaches infra only via Dapr or Aspire.
  [`architecture.md:604`](../planning-artifacts/architecture.md#L604)

- ADR-IDA-001: Dapr state for KV/set stores; dedup stays direct Redis.
  [`architecture.md:679`](../planning-artifacts/architecture.md#L679)

- Sanctioned home for overridable embedding endpoint defaults.
  [`EmbeddingProviderDefaultsOptions.cs:19`](../../src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaultsOptions.cs#L19)

**Keyed connections at the ServiceDefaults boundary**

- Relocated keyed Redis/FalkorDB multiplexer construction + AppHost guard.
  [`Extensions.cs:87`](../../src/Hexalith.Memories.ServiceDefaults/Extensions.cs#L87)

- Server composition only consumes the boundary registration.
  [`MemoriesServerServiceCollectionExtensions.cs:277`](../../src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs#L277)

**EventStore Dapr migration (per-aggregate FirstWrite)**

- Map store: FirstWrite per aggregate type, then index with compensate-on-fail.
  [`DaprAggregateCaseMappingStore.cs:113`](../../src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs#L113)

- Delete mappings only after index CAS succeeds (no map/index drift).
  [`DaprAggregateCaseMappingStore.cs:166`](../../src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs#L166)

- Tenant purge CAS-deletes index first and drains leftovers.
  [`DaprAggregateCaseMappingStore.cs:235`](../../src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs#L235)

- Observation store: membership + indexes with TTL refresh and fail-open.
  [`DaprObservedEventTypeStore.cs:66`](../../src/Hexalith.Memories.EventStore/DaprObservedEventTypeStore.cs#L66)

- Observation tenant purge enumerates written/discovery indexes.
  [`DaprObservedEventTypeStore.cs:218`](../../src/Hexalith.Memories.EventStore/DaprObservedEventTypeStore.cs#L218)

- Router fails loud when mapping write is lost without a persisted winner.
  [`TenantEventRouter.cs:209`](../../src/Hexalith.Memories.EventStore/TenantEventRouter.cs#L209)

- Dedup reservation remains direct Redis `SET NX` (ADR exception).
  [`RedisPreflightDedupStore.cs:21`](../../src/Hexalith.Memories.EventStore/RedisPreflightDedupStore.cs#L21)

**Embedding and CLI config sourcing**

- Bind `EmbeddingProviders` and seed the static Configure seam at host boot.
  [`MemoriesServerServiceCollectionExtensions.cs:211`](../../src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs#L211)

- Options validation rejects blank OIDC client/scope and empty endpoints.
  [`EmbeddingProviderDefaults.cs:82`](../../src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs#L82)

**Tenant deletion wiring**

- Activity purges both Dapr stores with a real cancellation token.
  [`DeleteTenantDataKeysActivity.cs:72`](../../src/Hexalith.Memories.Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs#L72)

**Evidence (peripherals)**

- Live keyed multiplexer smoke against Redis Stack + FalkorDB.
  [`KeyedRedisConnectionsLiveSmokeTests.cs:26`](../../tests/Hexalith.Memories.IntegrationTests/Hosting/KeyedRedisConnectionsLiveSmokeTests.cs#L26)

- Live daprd `statestore` FirstWrite / TTL / purge suite.
  [`DaprStateStoreLiveSidecarTests.cs:28`](../../tests/Hexalith.Memories.IntegrationTests/EventStore/DaprStateStoreLiveSidecarTests.cs#L28)
