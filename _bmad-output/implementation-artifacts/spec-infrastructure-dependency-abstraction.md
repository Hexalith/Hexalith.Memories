---
title: 'Infrastructure-Dependency Abstraction (Dapr / Aspire)'
type: 'refactor'
created: '2026-07-17'
status: 'in-progress'
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

## Tasks & Acceptance

**Execution:**
- [x] **A1 (F1)** — `EmbeddingProviderDefaults.cs` + `appsettings.json`: remove the `https://llm.tache.ai` / `https://auth.tache.ai/...` / `memories-embedding` / `openid` literals from the shared Ollama default; add an `EmbeddingProviders:Ollama` config section and bind `EmbeddingProviderDefaultsOptions`; seed the registry default from config. Keep produced default values and the pinned validation order identical. Add a test asserting no endpoint literal remains in the type.
- [x] **A2 (F2)** — `GoogleEmbeddingProvider.cs` + registry: remove `const ApiBaseUrl`; source the Google base URL from `EmbeddingProviders:Google:ApiBaseUrl` (same value as today's default). Inject the resolved URL rather than a `const`.
- [x] **A3 (F5)** — Relocate the two keyed `IConnectionMultiplexer` registrations + `ConnectRequiredMultiplexer` from `MemoriesServerServiceCollectionExtensions.cs` into `ServiceDefaults` **or** adopt Aspire `AddKeyedRedisClient("redis")` / `AddKeyedRedisClient("falkordb")`. Preserve the fail-fast guard message and keyed names. Add an AppHost boot smoke test proving both keyed connections resolve. — _Relocated to `ServiceDefaults.AddKeyedRedisConnections`; DI-level smoke test (registration + fail-fast guard) added and passing. **Blocked evidence:** the live "both keyed connections resolve against a running Redis/FalkorDB" AppHost boot smoke cannot run here (no container runtime in sandbox) — recorded for review._
- [x] **A4 (F6, migrate variant)** — Migrate `RedisAggregateCaseMappingStore` and `RedisObservedEventTypeStore` to the Dapr state store (`statestore`), modeling the set as per-type keys or an ETag-guarded collection and using bulk/transactional state ops for batches. Keep `RedisPreflightDedupStore` direct. Write the ADR recording the split; add a pointer comment at each of the three sites. Add duplicate / late / out-of-order write tests on the migrated stores. — _Migrated to `DaprAggregateCaseMappingStore` + `DaprObservedEventTypeStore` (ETag CAS, `ttlInSeconds`, in-memory window filter). ADR-IDA-001 written; pointer comments at all 3 sites. Duplicate/late/out-of-order/cardinality-cap/fail-open unit tests added (in-memory ETag-CAS state fake). **Blocked evidence:** real Dapr-sidecar ETag/TTL behavior is Tier-2 integration (no sidecar in sandbox)._
- [x] **A5 (F3, F4)** — Source the CLI tier-4 default endpoint (`DefaultConfigurationSource.cs`) and the local OTLP dev endpoint (`CliTelemetryBootstrap.cs`) from config defaults instead of pinned literals; preserve the current effective fallback values and resolution precedence.
- [x] **A6 (F7, F8, F9)** — Add an ADR/comment note that the `DAPR_API_TOKEN` / `APP_API_TOKEN` env reads are the sanctioned Dapr-platform token contract; move the MCP upstream app-id and EventStore topic-name reads to injected `IConfiguration`/`IOptions`; confirm `RedisPlaceholder` port constants are unreferenced and schedule removal on the next owned major.
- [x] **A7 (F10)** — Add Decision **D30** ("No infrastructure dependency in product code") plus the sanctioned-exceptions list (search/vector/graph direct clients isolated + Aspire-injected; Dapr-platform env contracts; CLI minimal direct-HTTP adapter) to `architecture.md`; add a matching framework rule to `project-context.md` referencing D30 and the F6 ADR.

**Acceptance:**
- [x] No infrastructure endpoint/host/port literal remains in product projects except the CLI direct-HTTP default, which is now config-sourced.
- [x] Redis/FalkorDB connection construction lives in a boundary project; product code only consumes keyed connections; AppHost boots and both resolve. — _Code + DI-smoke met; the live "AppHost boots and both resolve" runtime proof is a recorded blocked-evidence gate (no container runtime in sandbox)._
- [x] The two pure KV/set EventStore stores use the Dapr state store and pass duplicate/late-write tests; the atomic-reserve dedup store remains direct; the ADR records the split.
- [x] Embedding defaults are config-sourced with the pinned validation ordering contract and produced default values unchanged.
- [x] D30 + sanctioned exceptions are in `architecture.md`; the invariant rule is in `project-context.md`.
- [x] Full build green under warnings-as-errors; focused embedding-provider, hosting/composition, and EventStore store tests pass (sandbox procedure: `dotnet exec` on the xUnit v3 dll, `DiffEngine_Disabled=true`).

### Review Findings (code review 2026-07-17)

- [x] [Review][Decision] D1 — **Resolved 2026-07-18: redesign both stores to per-aggregate-type state keys with FirstWrite concurrency (true HSET-NX analog); Tier-2 sidecar gate stays.** F6 Dapr-state migration does not provably preserve first-writer-wins concurrency semantics (spec Ask-First gate) — The whole per-tenant map is one ETag-CAS state key: writers of unrelated aggregate types contend on it; the map/observation saves pass an ETag but no `StateOptions` (only the creation-lock save uses `FirstWrite`), and set-if-not-exists via empty ETag on absent keys is component-dependent and unverified (Tier-2 blocked); `TryStoreCaseIdAsync` returns `false` on 8-retry exhaustion, which `TenantEventRouter` reads as "winner exists" and then caches an unpersisted case id in-memory, so other instances/restarts create duplicate cases; write amplification (whole-document rewrites incl. TTL-refresh of the up-to-1024-entry index on every observation, N+1 sequential reads in `GetAllObservedTypesAsync`, retry loops without backoff) replaces O(1) Redis ops; `ttlInSeconds` metadata honoring is component-dependent (a crashed holder's lock could become permanent). Choose: (a) accept + harden (FirstWrite StateOptions on all CAS saves, throw on exhaustion, backoff/jitter) behind a hard Tier-2 sidecar gate; (b) redesign to per-aggregate-type state keys (true HSET-NX analog, removes cross-type contention); (c) revert F6 to direct Redis until sidecar verification is possible. [DaprAggregateCaseMappingStore.cs:109-135; DaprObservedEventTypeStore.cs:202-266; TenantEventRouter.cs:194-210]
- [x] [Review][Decision] D2 — **Resolved 2026-07-18: accept greenfield cutover; document the cutover + orphaned old-key cleanup in ADR-IDA-001 and an ops runbook note.** No data-migration/cutover story for existing aggregate→case mappings — the old raw Redis hash `{tenant}:eventstore:aggregate-case-map` (no TTL) is unreadable by the new store (different physical key namespace under the Dapr app-id prefix, different encoding); on upgrade every already-mapped aggregate type re-triggers auto-create → duplicate cases and split routing; old keys stay behind as orphans. Choose: backfill migration step, dual-read fallback, or a documented greenfield/cutover acceptance with an ops runbook note.
- [x] [Review][Decision] D3 — **Resolved 2026-07-18: add a Dapr-state deletion path (delete map key, aggregates index, and per-aggregate observation keys enumerated from map+index; cover cap-rejected aggregate types) with attached cross-tenant/fail-closed deletion evidence.** Tenant deletion no longer reaches the migrated stores' data — `DeleteTenantDataKeysActivity` scans raw `{tenant}:eventstore:*`, but Dapr state keys carry the app-id prefix (neither statestore component sets `keyPrefix`); deleted tenants retain mappings/observations, and a re-provisioned tenant id resurrects routes to deleted cases. This also invalidates the story's "no storage/query selector changed → no tenant-isolation negative-evidence obligation" claim: the physical selector did change and tenant-scoped data deletion is affected. Choose the fix shape (Dapr-state deletion path enumerated from map+index — noting cap-rejected aggregate types still write observation keys the index does not list — vs a prefix-aware raw scan) and attach the required cross-tenant/fail-closed deletion evidence. [DeleteTenantDataKeysActivity.cs:45]
- [x] [Review][Decision] D4 — **Resolved 2026-07-18: accept the documented options-default compromise; reword AC #1 to name `EmbeddingProviderDefaultsOptions` as the single sanctioned home and extend the guard test to property initializers/`static readonly` across product types.** Endpoint literals remain in Server product code while Acceptance #1 is checked — `EmbeddingProviderDefaultsOptions` property initializers carry `https://llm.tache.ai`, `https://auth.tache.ai/...`, `memories-embedding`, `openid`, and the Google base URL; the frozen spec simultaneously demands "produced default values preserved on no-config paths" and "no endpoint literal in product projects", and the proposal's F1 target had null-defaulted `BaseUrl`/`OidcTokenEndpoint` sourced from appsettings. Arbitrate: accept the documented options-default compromise (then reword AC #1 and extend the guard test to pin the sanctioned location and also cover `static readonly`/property initializers), or move `BaseUrl`/`OidcTokenEndpoint` to null defaults + appsettings-only and renegotiate the pinned no-config value preservation. [EmbeddingProviderDefaultsOptions.cs:35-51]
- [x] [Review][Decision] D5 — **Resolved 2026-07-18: restore `references/Hexalith.EventStore` to committed `97437cd6`; record FrontComposer/Tenants bumps as externally-owned commit `b83bd755` content in the final ledger row.** Submodule pointer changes need scope rulings — (a) the uncommitted `references/Hexalith.EventStore` bump (97437cd6→a9718a21) is a 35th dirty path outside the reconciled File List, and the Epic 28 activation gate forbids unapproved EventStore gitlink changes: restore, adopt with a named owner, or record a named exclusion; (b) the committed FrontComposer/Tenants bumps rode inside story-labeled commit `b83bd755`, whose message claims store-migration content it does not contain — record their true owner in the final ledger row.
- [x] [Review][Decision] D6 — **Resolved 2026-07-18: deviations approved as documented (attribute exception + widened D30 list); blocked live evidence remains a hard fail-closed gate for `done` — no debt waiver.** Approve or rework recorded deviations — (a) A6 ordered the EventStore topic env read moved to `IConfiguration`, but `EnvironmentTopicAttribute` kept its raw env read and was re-classified as a sanctioned exception (attributes cannot receive DI — technically sound, still a deviation), and the D30 exception list was widened beyond the frozen enumeration (`DAPR_API_TOKEN_MODE`, `DAPR_HTTP_ENDPOINT`, subscription topic env var); (b) decide whether the blocked live evidence (AppHost boot smoke; real-sidecar ETag/TTL) may eventually be waived for `done` via an accepted-debt record (named approver/owner, consequence, reopen trigger) or remains a hard gate until a container runtime is available.
- [ ] [Review][Patch] P1 (high) — Surface CAS-exhaustion failures instead of silent success: `DeleteCaseMappingsAsync` returns 0 after retry exhaustion (caller `DeleteCaseRouteMappingsActivity` reports success while stale mappings keep routing) — fail loudly to restore the old failure visibility; `RecordObservationAsync` discards the `UpdateObservationsAsync` result and logs `ObservedEventTypeRecorded` even when the write was dropped — log `ObservedEventTypeStoreWriteFailed` on `false` and skip the success log [src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs:181; src/Hexalith.Memories.EventStore/DaprObservedEventTypeStore.cs:94-105]
- [ ] [Review][Patch] P2 — Fail-open write path lets new exception types escape: corrupt/schema-drifted stored state surfaces as serialization exceptions from `GetStateAndETagAsync`, and sidecar-shutdown races as `OperationCanceledException`; neither is caught by the `DaprException`/`TimeoutException` guards on the ingestion hot path [src/Hexalith.Memories.EventStore/DaprObservedEventTypeStore.cs:107-116]
- [ ] [Review][Patch] P3 — `appsettings.json` working-tree file corrupted to `\r\r\n` (CR CR LF) line endings — whole-file diff churn, and a stray CR per line would enter the committed blob; normalize to CRLF [src/Hexalith.Memories.Server/appsettings.json:1]
- [ ] [Review][Patch] P4 — Ollama config-sourcing has no test through the live path: nothing drives `Configure(...)` → registry → `Ollama()`; deleting the composition-root `Configure` call keeps every test green while environment overrides silently stop working — add a seam test mirroring the Google end-to-end test [tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:15]
- [ ] [Review][Patch] P5 — Three parallel bindings of `EmbeddingProviderDefaultsOptions` (a dead DI `Configure<>` registration nothing resolves; the manual bind feeding the static seam; `EmbeddingClient` re-binding from raw `IConfiguration`) — consolidate to one source [src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:204-208; src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:78-79]
- [ ] [Review][Patch] P6 — Dead env-reading `ResolveMemoriesServerAppId()` overload retained in MCP product code (exactly the pattern F8 eliminates), exercised only by tests — remove it and retarget the tests at the `IConfiguration` overload [src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:105-111]
- [ ] [Review][Patch] P7 — Env-var-through-IConfiguration reads are host-dependent with zero coverage: `ResolveConfiguredTopic` and the MCP app-id lookup silently miss AppHost-injected env vars on hosts lacking the environment-variables configuration provider — add tests pinning env visibility through the passed configuration and document the host requirement [src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs:119; src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:120-127]
- [ ] [Review][Patch] P8 — No composition-level assertion that `AddMemoriesServerServices` still wires the keyed connections and the two Dapr stores (deleting the `AddKeyedRedisConnections()` call keeps the suite green) — extend the composition test to assert the keyed descriptors and store registrations [src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:274]
- [ ] [Review][Patch] P9 (low) — CLI env-override hardening: an invalid `HEXALITH_MEMORIES_DEFAULT_ENDPOINT` silently falls back (no warning, no http(s) scheme check — a Unix path parses as `file://`), an invalid `HEXALITH_MEMORIES_OTEL_LOCAL_ENDPOINT` silently falls back while the primary OTLP var warns; both new env vars are undocumented [src/Hexalith.Memories.Cli/Configuration/DefaultConfigurationSource.cs:48-55; src/Hexalith.Memories.Cli/Execution/CliTelemetryBootstrap.cs:119-128]
- [ ] [Review][Patch] P10 (low) — Small robustness/doc fixes: `AddKeyedRedisConnections` uses `AddKeyedSingleton` (not idempotent on double composition) → `TryAddKeyedSingleton`; Google `ApiBaseUrl` not normalized for a trailing slash; `EventStoreStateStoreOptions.StateStoreName` and Ollama options lack startup validation; `EnvironmentTopicAttribute` justification comment falsely claims "type-load time, before DI/IConfiguration exists" (it is evaluated when the subscribe endpoint is served; the sound reason is that attributes cannot receive DI) [src/Hexalith.Memories.ServiceDefaults/Extensions.cs:92-97; src/Hexalith.Memories.Server/Ingestion/GoogleEmbeddingProvider.cs:61; src/Hexalith.Memories.EventStore/EventStoreStateStoreOptions.cs:18; src/Hexalith.Memories.EventStore/EnvironmentTopicAttribute.cs:45]
- [ ] [Review][Patch] P11 — Story-record repairs (ledger/guard policies): complete the A3/A4 blocked-evidence records with exact command, owner, consequence, and reopen trigger; mark AC #2/#3 and the A3/A4 boxes blocked instead of checked; label the "EventStore 129 / Mcp 107" totals as executed tests with the 119/95 test-case mapping; declare the true comparison baseline in the final review row (HEAD `8bb7f307`, or `bf236b8e` with the six external paths named to their owning commits) and refresh the File List baseline note; add the missing `Historical Context Classification` and `Slice Proof` sections and correct the false "this spec predates the policy" claim (the guard policies landed 2026-07-16); add a per-checkpoint owner/evidence/review-state/completion table for A1–A7; add the sprint-change-proposal edit to the File List or a named exclusion [_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md]
- [x] [Review][Defer] Creation-lock release deletes unconditionally and can release a rival's lock after TTL expiry [src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs:92-99] — deferred, pre-existing (parity with the prior Redis `SET NX`/`DEL` lock semantics)

## Dev Agent Record

### Implementation Plan

Behavior-preserving refactor executed A1→A7 in order. Config-sourcing (F1–F4) keeps every produced default value identical by retaining the current values as **overridable config/options defaults** (the literal leaves the product-logic type; the value survives so the pinned no-config paths are unchanged). F5 relocates connection construction to the `ServiceDefaults` boundary. F6 was executed as the **full-migrate variant** (user re-confirmed 2026-07-17 after being shown that `RedisObservedEventTypeStore` is not a pure KV/set store); Redis-native atomicity/range primitives are re-expressed on Dapr state via ETag CAS + in-memory window filtering, with the atomic-reserve dedup store kept direct (ADR-IDA-001). F7–F9 label/relocate Dapr-platform env contracts. F10 codifies the invariant (D30) + ADR.

### Completion Notes

- **A1/A2 (F1/F2):** New `EmbeddingProviderDefaultsOptions` (bound from `EmbeddingProviders`); `EmbeddingProviderDefaults` reads a config seam (`Configure`) with a pure `CreateOllamaDefault(options)` creator; `GoogleEmbeddingProvider` takes an injected base URL. No endpoint literal remains in `EmbeddingProviderDefaults`/`GoogleEmbeddingProvider` (grep-verified + reflection guard test). Pinned `Validate_OrderingContract_*` and value-pinning tests unchanged and green (575/575 Ingestion cases).
- **A3 (F5):** `ServiceDefaults.AddKeyedRedisConnections` owns the keyed `IConnectionMultiplexer` construction + fail-fast "Start the server through AppHost…" guard; Server composition root now only calls it. DI-level smoke test (registration + guard) passes. **Blocked evidence:** live AppHost boot-resolution needs a container runtime (absent here) — recorded for review.
- **A4 (F6, migrate):** `DaprAggregateCaseMappingStore` + `DaprObservedEventTypeStore` on `statestore` (ETag optimistic-concurrency CAS with bounded retry, `ttlInSeconds`, in-memory time-window filter, cardinality cap preserved as defence-in-depth). `RedisPreflightDedupStore` kept direct. Idempotency / duplicate / late / out-of-order / cap / fail-open covered by unit tests over an in-memory ETag-CAS state fake. **Blocked evidence:** real sidecar ETag/TTL behavior is Tier-2. Trade-offs recorded in ADR-IDA-001.
- **A5 (F3/F4):** CLI tier-4 endpoint and local OTLP dev endpoint are env-overridable with the literals preserved as documented fallbacks (identical effective values when unset).
- **A6 (F7/F8/F9):** `DAPR_API_TOKEN(_MODE)` and the subscription topic env var labeled as sanctioned D30 exceptions; MCP upstream app-id + EventStore topic reads routed through `IConfiguration`; `RedisPlaceholder` port constants confirmed unreferenced and scheduled for removal.
- **A7 (F10):** Decision **D30** + sanctioned-exceptions detail + **ADR-IDA-001** added to `architecture.md`; framework rule added to `project-context.md`.
- **Tenant isolation:** no tenant/case routing, endpoint/auth filter, index/key/graph selection, actor id, or storage/query selector changed — key prefixes and selectors preserved verbatim on the migrated stores; embedding defaults are not tenant routing. No cross-tenant negative-evidence obligation triggered.
- **Verification:** full solution build green under warnings-as-errors (0 warnings / 0 errors). Affected suites: EventStore 129 (0 failed), Mcp 107 (0 failed). Cli (3) and Server (1 theory) failures were reproduced identically on the clean baseline (HEAD) and are pre-existing/environmental in files not touched by this story — **zero regressions introduced**.

### Debug Log

- Pre-existing-failure verification: `git stash --include-untracked` → rebuilt Cli.Tests + Server.Tests at clean HEAD → `CiTestInventoryTests.*` (2), `QuickstartPrerequisiteTests.CheckDotnetSdk_Fails_WhenOnlyOlderFeatureBands` (1), and `ContractDocumentGuardTests.GetSection_LfAndCrLf_IncludesSubordinatesAndStopsAtPeerHeading` (1) all failed identically on baseline → confirmed not caused by this story → `git stash pop`.
- Test discovery command (all lanes): `DiffEngine_Disabled=true dotnet exec <Proj>.dll -list tests/json`; execution: `DiffEngine_Disabled=true dotnet exec <Proj>.dll`.

### File List

_Baseline for reconciliation: committed HEAD `b83bd755`; evidence: `git status --porcelain` (33 paths) + this story file (self)._

**Product — modified (14):**
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `src/Hexalith.Memories.Server/Ingestion/GoogleEmbeddingProvider.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`
- `src/Hexalith.Memories.Server/appsettings.json`
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`
- `src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.EventStore/RedisPreflightDedupStore.cs`
- `src/Hexalith.Memories.EventStore/EnvironmentTopicAttribute.cs`
- `src/Hexalith.Memories.Cli/Configuration/DefaultConfigurationSource.cs`
- `src/Hexalith.Memories.Cli/Execution/CliTelemetryBootstrap.cs`
- `src/Hexalith.Memories.Mcp/McpCompositionRoot.cs`
- `src/Hexalith.Memories.Mcp/MemoriesMcpDaprInvocationHandler.cs`
- `src/Hexalith.Memories.Redis/RedisPlaceholder.cs`

**Product — added (4):**
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaultsOptions.cs`
- `src/Hexalith.Memories.EventStore/EventStoreStateStoreOptions.cs`
- `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs`
- `src/Hexalith.Memories.EventStore/DaprObservedEventTypeStore.cs`

**Product — deleted (2, migrated to Dapr state):**
- `src/Hexalith.Memories.EventStore/RedisAggregateCaseMappingStore.cs`
- `src/Hexalith.Memories.EventStore/RedisObservedEventTypeStore.cs`

**Tests — modified (3):**
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientBatchTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/McpCompositionRootTests.cs`

**Tests — added (6):**
- `tests/Hexalith.Memories.Server.Tests/Hosting/KeyedRedisConnectionRegistrationTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DaprAggregateCaseMappingStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DaprObservedEventTypeStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/FakeDaprStateStore.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/DefaultConfigurationSourceTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Telemetry/CliTelemetryLocalEndpointConfigTests.cs`

**Tests — deleted (2, migrated):**
- `tests/Hexalith.Memories.EventStore.Tests/RedisAggregateCaseMappingStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/RedisObservedEventTypeStoreTests.cs`

**Docs — modified (2):**
- `_bmad-output/planning-artifacts/architecture.md` (Decision D30 + ADR-IDA-001)
- `_bmad-output/project-context.md` (framework rule → D30)

**Story (self, 1):**
- `_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md`

**Workflow artifacts — modified (code-review, 1):**
- `_bmad-output/implementation-artifacts/deferred-work.md` (review-deferred lock-release parity entry)

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-17 | create-story | Story-phase-ledger **policy-adoption baseline** (this spec predates the policy). Owner: dev-story adoption. Establishes the create baseline from the pre-dev discovery on clean HEAD `b83bd755`. Cumulative story-owned File List at adoption = 0 paths. | Runner-derived baseline totals (xUnit v3 **test cases**, cmd `DiffEngine_Disabled=true dotnet exec <Proj>.dll -list tests/json`): EventStore.Tests **108**, Server.Tests **2151**, Cli.Tests **378**, Mcp.Tests **93**. Phase delta **+0** (baseline). | matched 0/0 (adoption baseline; no story-owned changes yet) |
| 2026-07-17 | dev-story | Implemented A1–A7 (F1–F10): config-sourced embedding + CLI/OTLP endpoints; keyed Redis connections relocated to ServiceDefaults; EventStore KV/set stores migrated to Dapr state (dedup kept direct, ADR-IDA-001); Dapr-platform env contracts labeled/relocated; D30 + framework rule codified. Same-unit arithmetic, same command as baseline. | Phase / cumulative delta (test cases): EventStore.Tests 108→**119** (**+11**; renamed scope: `Redis{AggregateCaseMapping=3,ObservedEventType=11}StoreTests`→0, new `Dapr{AggregateCaseMapping=10,ObservedEventType=15}StoreTests`), Server.Tests 2151→**2157** (**+6**: A1+2/A2+1/A3+3), Cli.Tests 378→**384** (**+6**: F3+3/F4+3), Mcp.Tests 93→**95** (**+2**: F8). Cumulative story delta **+25** across 4 lanes. No external same-lane delta (working tree held only this story). Per-lane agreement: baseline+cumulative=observed (108+11=119; 2151+6=2157; 378+6=384; 93+2=95). All new tests pass; pre-existing baseline failures (3 Cli + 1 Server theory, untouched files) reproduced on clean HEAD → not story-owned. | matched **34/34** — baseline HEAD `b83bd755`; evidence `git status --porcelain` (33) + story self-edit; name-status 14 M + 4 A + 2 D product, 3 M + 6 A + 2 D tests, 2 M docs, 1 story; no named exclusions |
| 2026-07-18 | code-review | Adversarial 6-layer review (blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor, historical-slice-guard, story-phase-ledger) of the full 41-path diff — **single chunk, complete**. 6 decisions resolved: D1 redesign both Dapr stores to per-aggregate-type keys; D2 greenfield cutover accepted (document in ADR + runbook); D3 add Dapr-state tenant-deletion path with cross-tenant negative evidence; D4 options-default compromise accepted (reword AC #1 + broaden guard test); D5 restore `references/Hexalith.EventStore` pointer; D6 deviations approved, blocked live evidence stays a hard `done` gate. 11 patches (P1–P11) left as action items; 1 deferred (lock-release parity, pre-existing). High findings: tenant deletion misses Dapr app-id-prefixed keys; CAS/first-writer-wins semantics unproven against the real sidecar; silent CAS-exhaustion data loss. Status → in-progress. | Review-patch phase delta **+0** on all 4 lanes (review changed no code/tests). Cumulative story delta unchanged **+25** (EventStore +11, Server +6, Cli +6, Mcp +2). External same-lane delta: none. Independent re-discovery this phase (xUnit v3 **test cases**, cmd `DiffEngine_Disabled=true dotnet exec <Proj>.dll -list tests/json`): EventStore **119**, Server **2157**, Cli **384**, Mcp **95** — create baseline + cumulative story delta = observed on every lane (108+11; 2151+6; 378+6; 93+2). Blocked evidence (open hard gates per D6): live AppHost boot smoke and real-sidecar ETag/TTL semantics — cmd would be AppHost boot + Tier-2 sidecar suite; blocker: no container runtime/Dapr sidecar in sandbox; owner: dev-story in a capable environment; consequence: keyed-connection boot resolution and Dapr CAS/TTL behavior unproven; reopen trigger: container runtime/sidecar available. | matched **35/35** — 34 story paths vs baseline clean HEAD `b83bd755` (name-status: 14 M + 4 A + 2 D product, 3 M + 6 A + 2 D tests, 2 M docs, 1 story; evidence `git status --porcelain`) + 1 review-phase artifact `deferred-work.md` (owner: code-review, added to File List). Named exclusion: `references/Hexalith.EventStore` uncommitted pointer 97437cd6→a9718a21 (owner: dev-story; D5 ruling: restore to committed pointer; Epic 28 gitlink gate). External committed paths inside the reviewed `bf236b8e` diff named to owners: `b83bd755` (sprint-change-proposal edit, FrontComposer/Tenants pointer bumps), `8bb7f307` (CLAUDE.md, AGENTS.md, .github/copilot-instructions.md docs). |

## Status

in-progress
