---
title: 'Infrastructure-Dependency Abstraction (Dapr / Aspire)'
type: 'refactor'
created: '2026-07-17'
status: 'backlog'
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
- [ ] **A1 (F1)** — `EmbeddingProviderDefaults.cs` + `appsettings.json`: remove the `https://llm.tache.ai` / `https://auth.tache.ai/...` / `memories-embedding` / `openid` literals from the shared Ollama default; add an `EmbeddingProviders:Ollama` config section and bind `EmbeddingProviderDefaultsOptions`; seed the registry default from config. Keep produced default values and the pinned validation order identical. Add a test asserting no endpoint literal remains in the type.
- [ ] **A2 (F2)** — `GoogleEmbeddingProvider.cs` + registry: remove `const ApiBaseUrl`; source the Google base URL from `EmbeddingProviders:Google:ApiBaseUrl` (same value as today's default). Inject the resolved URL rather than a `const`.
- [ ] **A3 (F5)** — Relocate the two keyed `IConnectionMultiplexer` registrations + `ConnectRequiredMultiplexer` from `MemoriesServerServiceCollectionExtensions.cs` into `ServiceDefaults` **or** adopt Aspire `AddKeyedRedisClient("redis")` / `AddKeyedRedisClient("falkordb")`. Preserve the fail-fast guard message and keyed names. Add an AppHost boot smoke test proving both keyed connections resolve.
- [ ] **A4 (F6, migrate variant)** — Migrate `RedisAggregateCaseMappingStore` and `RedisObservedEventTypeStore` to the Dapr state store (`statestore`), modeling the set as per-type keys or an ETag-guarded collection and using bulk/transactional state ops for batches. Keep `RedisPreflightDedupStore` direct. Write the ADR recording the split; add a pointer comment at each of the three sites. Add duplicate / late / out-of-order write tests on the migrated stores.
- [ ] **A5 (F3, F4)** — Source the CLI tier-4 default endpoint (`DefaultConfigurationSource.cs`) and the local OTLP dev endpoint (`CliTelemetryBootstrap.cs`) from config defaults instead of pinned literals; preserve the current effective fallback values and resolution precedence.
- [ ] **A6 (F7, F8, F9)** — Add an ADR/comment note that the `DAPR_API_TOKEN` / `APP_API_TOKEN` env reads are the sanctioned Dapr-platform token contract; move the MCP upstream app-id and EventStore topic-name reads to injected `IConfiguration`/`IOptions`; confirm `RedisPlaceholder` port constants are unreferenced and schedule removal on the next owned major.
- [ ] **A7 (F10)** — Add Decision **D30** ("No infrastructure dependency in product code") plus the sanctioned-exceptions list (search/vector/graph direct clients isolated + Aspire-injected; Dapr-platform env contracts; CLI minimal direct-HTTP adapter) to `architecture.md`; add a matching framework rule to `project-context.md` referencing D30 and the F6 ADR.

**Acceptance:**
- [ ] No infrastructure endpoint/host/port literal remains in product projects except the CLI direct-HTTP default, which is now config-sourced.
- [ ] Redis/FalkorDB connection construction lives in a boundary project; product code only consumes keyed connections; AppHost boots and both resolve.
- [ ] The two pure KV/set EventStore stores use the Dapr state store and pass duplicate/late-write tests; the atomic-reserve dedup store remains direct; the ADR records the split.
- [ ] Embedding defaults are config-sourced with the pinned validation ordering contract and produced default values unchanged.
- [ ] D30 + sanctioned exceptions are in `architecture.md`; the invariant rule is in `project-context.md`.
- [ ] Full build green under warnings-as-errors; focused embedding-provider, hosting/composition, and EventStore store tests pass (sandbox procedure: `dotnet exec` on the xUnit v3 dll, `DiffEngine_Disabled=true`).
