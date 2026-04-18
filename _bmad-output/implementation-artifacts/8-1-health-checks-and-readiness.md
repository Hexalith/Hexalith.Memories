# Story 8.1: Health Checks & Readiness

Status: ready-for-dev

**Effort estimate:** ~5 working days end-to-end — 3 days implementation (Tasks 1-5), 1 day docs (Task 7), 1 day integration wiring + test debugging (Task 6 + dotnet-format/CI reconciliation). Adjust if 7.5 is not yet merged at start — rebase cost can add 0.5-1 day (see "Previous story intelligence" + the 7.5 merge-conflict protocol in Dev Notes).

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** Operator-facing **readiness and liveness health checks** that verify all three backends (RediSearch, Redis Vector, FalkorDB) plus the DAPR sidecar, integrate with Aspire's existing ServiceDefaults wiring, and expose per-backend detail + capability-affected messaging for orchestrator health probes. Closes **FR72** and kicks off Epic 8 (Observability & System Health, Phase MVP — Operations).

**What already exists (do NOT rebuild):**

1. **ServiceDefaults endpoint scaffolding** — `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:93-124` already maps three endpoints via `MapDefaultEndpoints()`:
   - `/health` — aggregate of all checks (no predicate)
   - `/alive` — predicate `r => r.Tags.Contains("live")`
   - `/ready` — predicate `r => r.Tags.Contains("ready")`
   - Status-code mapping already configured: `Healthy→200`, `Degraded→200`, `Unhealthy→503` (line 97-102). This is the hinge AC3 depends on — **do NOT change it**.
2. **`self` liveness check** — `ServiceDefaults.Extensions.AddDefaultHealthChecks` (line 84-91) registers `"self"` returning `HealthCheckResult.Healthy()` tagged `["live"]`. Reuse as-is; it's the "process is alive and serving requests" sentinel.
3. **`DaprSidecarHealthCheck`** — `src/Hexalith.Memories.Server/HealthChecks/DaprSidecarHealthCheck.cs` already implemented via `DaprClient.CheckHealthAsync`. Registered in `Program.cs:38-43` with tag `["ready"]` and 3-second timeout. Unit tests at `tests/Hexalith.Memories.Server.Tests/HealthChecks/DaprSidecarHealthCheckTests.cs` (4 tests, `NSubstitute` + `Shouldly`, matching project test conventions).
4. **`DaprStateStoreHealthCheck`** — probes DAPR state store via `GetStateAsync<byte[]>` with a `__health_probe__` key. Registered in `Program.cs:44-51` tagged `["ready"]`. Unit tests at `tests/Hexalith.Memories.Server.Tests/HealthChecks/DaprStateStoreHealthCheckTests.cs`.
5. **Per-backend probe patterns in `TenantMetricsService.GetIndexSizesAsync`** (`src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:90-107`) — Redis probed via `FT.INFO <indexName>`; FalkorDB probed via NFalkorDB's `MATCH (n) RETURN count(n)`. Exception handling maps to `IndexHealth.Ready` | `Missing` | `Degraded` | `Unknown`. Do NOT duplicate this classifier; reuse the same pattern (backend ping, not tenant-scoped query) for health checks.
6. **`IndexHealth` enum** in `src/Hexalith.Memories.Contracts/V1/IndexHealth.cs` distinguishes data state (Ready/Missing/Degraded) from availability state (Unknown). Reuse in the readiness JSON response contract so the `/ready` payload shape matches the existing `TelemetrySummary.IndexHealth` field shape (Story 7.5 — `src/Hexalith.Memories.Contracts/V1/TelemetrySummary.cs:27`).
7. **Trace-exclusion filter** — `ConfigureOpenTelemetry` (`ServiceDefaults/Extensions.cs` — predicate body at lines 59-63, inside the `WithTracing` lambda opened at line 56) explicitly excludes `/health`, `/alive`, `/ready` from AspNetCore tracing. 8.1 MUST preserve this — otherwise every 1-Hz orchestrator probe floods the trace collector. Story 7.5 AC #5 pinned this as a regression invariant; 8.1 inherits the invariant.

**What 8.1 adds:**

1. **Three new backend health check classes** in `src/Hexalith.Memories.Server/HealthChecks/`:
   - `RediSearchHealthCheck` — probes the shared Redis instance via keyed `IConnectionMultiplexer("redis")`; verifies RediSearch module loaded via `FT._LIST` (cheap; no tenant scope; module failure distinct from connectivity failure).
   - `RedisVectorHealthCheck` — probes the same Redis instance; verifies vector capability via `MODULE LIST` search for `search` / `ReJSON` (Redis Stack bundles vector into `search` module). NOTE: MVP deploys RediSearch + Redis Vector on **one** physical Redis instance per architecture.md line 228 — two logical checks per FR72 AC so operators see the capability breakdown, not one merged check.
   - `FalkorDbHealthCheck` — probes keyed `IConnectionMultiplexer("falkordb")`; verifies FalkorDB via `GRAPH.LIST` (cheap; instance-scoped, not tenant-scoped).
2. **One shared helper** `BackendHealthCheckBase` (abstract; optional — see Dev Notes "One-class vs. three-class decision") OR inline status-classification logic per check. Pick whichever keeps each check <60 LOC (same bar as the existing `DaprStateStoreHealthCheck`).
3. **DAPR sidecar re-tagging** — change the registration at `Program.cs:42` from `tags: ["ready"]` to `tags: ["live", "ready"]` so liveness also verifies sidecar connectivity (epic AC4: *"liveness probe checks the Memories Server process health and DAPR sidecar connectivity"*). No new check class; just the tag list.
4. **Custom JSON response writer** `BackendHealthResponseWriter` that renders the `HealthReport` for `/ready` and `/health` as JSON matching the AC5 schema (per-backend status + description + capability-affected message). The default ASP.NET Core response writer produces plain text — replace it via `HealthCheckOptions.ResponseWriter`. Wire via a new extension `MapBackendAwareDefaultEndpoints(this WebApplication app)` in `ServiceDefaults/Extensions.cs` that delegates to the existing `MapDefaultEndpoints` but overrides the response writer — OR modify `MapDefaultEndpoints` in place (one project uses `ServiceDefaults`; no cross-repo surface). Choose in-place modification; document the response-writer change in `docs/dev/health-checks.md`.
5. **Program.cs registration updates** — add the three backend checks alongside the existing DAPR checks; each tagged `["ready"]` with 3-second timeout; each failing with `HealthStatus.Degraded` (NOT `Unhealthy`) on connectivity failure so the aggregate `/ready` response is `Degraded` (200 OK) when one backend is down, `Unhealthy` (503) only when the service is truly non-functional (DAPR sidecar dead). See Dev Notes "Status-aggregation design" for the full matrix.
6. **`docs/dev/health-checks.md`** — operator-facing doc covering endpoints, response shapes, capability-affected semantics, orchestrator-probe configuration snippets (Kubernetes `livenessProbe` + `readinessProbe` YAML; Aspire dashboard linkage).
7. **Unit tests** at `tests/Hexalith.Memories.Server.Tests/HealthChecks/`:
   - `RediSearchHealthCheckTests.cs` — healthy / connection-refused / LOADING response / module-missing / null-context paths.
   - `RedisVectorHealthCheckTests.cs` — same matrix.
   - `FalkorDbHealthCheckTests.cs` — healthy / connection-refused / unknown-error.
   - `BackendHealthResponseWriterTests.cs` — JSON shape assertions for healthy / partially-degraded / fully-unhealthy reports + capability-affected message mapping.
8. **Integration tests** at `tests/Hexalith.Memories.IntegrationTests/Health/`:
   - `HealthEndpointIntegrationTests.cs` `[Trait("Category","Integration")]` — exercises `/health`, `/alive`, `/ready` against the Aspire fixture; asserts 200 + per-backend payload when all healthy; `[Fact(Skip)]` pattern acceptable per Story 5.6 deferral if the pre-existing `CS0311 on IDaprSidecarResource` AppHost build error has not been resolved (see Story 5.6 Dev Notes). If the build error has been resolved by landing time of 8.1, un-skip.

**What does NOT ship:**

- **Deep per-tenant index health probing in `/ready`.** The readiness endpoint is instance-scoped (the Redis / FalkorDB *connection* is healthy), not tenant-scoped (index X for tenant Y is healthy). Tenant-scoped index health is already exposed via `GET /api/tenants/{tenantId}/configuration` (Story 5.5 `TenantIndexStatus`) and via `GET /api/tenants/{tenantId}/telemetry/summary` (Story 7.5). Do NOT probe every tenant's index in the health check — that is O(tenants × axes) backend load per probe and flips the semantics from "service is reachable" to "every tenant's data is reachable", which is Story 8.2 (Consistency Verification) territory, not 8.1.
- **Alert thresholds / SLO definitions.** The endpoints emit the signal; operator wiring to alerts / PagerDuty / etc. is downstream. Documented as out-of-scope in `docs/dev/health-checks.md`.
- **A separate `/metrics` Prometheus endpoint.** Metrics flow via the OTLP exporter wired in Story 7.5 `ConfigureOpenTelemetry`. Health checks are a complementary signal (liveness/readiness boolean-ish) not a metric ingestion path.
- **Circuit-breaker state exposure.** The three backend checks are point-in-time probes; Polly circuit-breaker state (if any is ever added) is NOT surfaced here. If a future story adds circuit breakers, it extends the JSON response writer.
- **Consistency verification across backends.** Story 8.2 ships `ConsistencyVerificationWorkflow` (FR73). 8.1 only reports backend *connectivity*, not cross-backend *data consistency*.
- **Data export functionality.** Story 8.3 (FR71). Orthogonal.
- **Rate-limiting of probe requests.** Orchestrator probes are typically 1-5 Hz; the three backend pings (FT._LIST + MODULE LIST + GRAPH.LIST) are each <1 ms against a healthy local Redis. No throttling needed; documented in `docs/dev/health-checks.md` under "Probe cost".
- **Retry-After header on 503 responses.** ASP.NET Core's default health-check handler does NOT emit `Retry-After`. Story 5.6 adds `Retry-After: 5` to search-endpoint 503s, but that is request-flow semantic — orchestrator probes have their own retry cadence (`periodSeconds` in Kubernetes). Leave absent to avoid surprising probe behavior.
- **Per-backend timeout overrides.** All three backend checks share the existing 3-second timeout already used for DAPR checks (`Program.cs:37`). Do NOT introduce per-backend timeout configuration in MVP — adds config surface for no current use case.
- **Graph axis optionality signal.** Architecture line 151 says graph axis is "architecturally optional"; if FalkorDB is intentionally disabled, the check still reports `Degraded` + capability message — it does NOT self-configure to skip. Phase 2 concern if graph-optional deployments become common.

**Primary risks:**

1. **Status-aggregation mismatch breaks orchestrator integration.** If a backend check returns `HealthStatus.Unhealthy`, ASP.NET Core aggregates the `/ready` response as `Unhealthy` → 503. Kubernetes then marks the pod NotReady and removes it from Service rotation — **the opposite of the AC3 intent** ("status is `Degraded` with the unhealthy backend identified"). **Mitigation:** backend checks return `HealthStatus.Degraded` (not `Unhealthy`) on connectivity failure. Aggregate stays `Degraded` → 200 OK (per existing `ResultStatusCodes` map in ServiceDefaults line 100). Only DAPR sidecar failure keeps `Unhealthy` (sidecar dead = service genuinely can't serve requests). A unit test `BackendHealthResponseWriter_OneBackendDown_ReturnsDegraded` + a wire-level integration test guard this. **Caveat — graph-only / vector-only tenants:** the Degraded=200 choice optimizes for hybrid-search traffic. A tenant whose workload is 100% graph traversal will receive 500 responses at the endpoint layer while the pod stays in rotation, because readiness doesn't know the tenant's capability mix. In a multi-pod deployment with partial failure (one backend down on some pods), this still works — the endpoint handler's per-request capability check (Story 5.6) routes around it. In a **cluster-wide** backend outage (e.g., FalkorDB cluster fully down), ALL pods are Degraded and there's no healthier alternative to route to; graph-only traffic fails end-to-end regardless of readiness semantics. Document in `docs/dev/health-checks.md` so operators know the Degraded=200 signal does NOT guarantee successful requests for capability-specialized tenants — it only preserves the hybrid-search path.
2. **Liveness probe retaining sidecar dependency causes pod-restart loop (cascading-outage blast radius).** Re-tagging `DaprSidecarHealthCheck` to `["live", "ready"]` means the liveness probe fails if the sidecar is flapping. Kubernetes restarts the pod; a flapping sidecar triggers an infinite restart loop. Worse, a DAPR control-plane blip is correlated across pods — every pod fails liveness simultaneously and restarts in lockstep, turning a 30-second control-plane glitch into a minutes-long full outage. **Mitigation:** architecture line 152 explicitly endorses this pattern (`"Kubernetes liveness probe; workflow + actor state survives in Redis; sidecar auto-restart"`). The sidecar has its own auto-restart logic; a pod restart only happens if sidecar auto-restart has already failed N times. Operator orchestrator config (`livenessProbe.failureThreshold`) should be ≥3 to tolerate transient sidecar blips. `docs/dev/health-checks.md` MUST include a "Blast radius" callout making the correlated-failure mode explicit and pointing ops at `failureThreshold` + `periodSeconds` tuning. **Future hardening** (out of scope for 8.1): if incidents surface, consider a stabilization window inside the check itself — only flip to `Unhealthy` after N consecutive failed probes — so the behavior is robust to operator misconfig.
3. **Redis Stack module detection is fragile across versions.** `MODULE LIST` returns an array of module tuples; the field names/case vary across Redis Stack minor versions. If the parser is overly strict it falsely reports `Degraded` on a healthy Redis. **Mitigation:** check for presence of the module name (`"search"` for RediSearch, present in all Redis Stack versions used in the project per architecture line 228 — `redis/redis-stack` is pinned in AppHost), but classify ambiguous responses as `Healthy` with a description of "module presence unverified" rather than `Degraded`. A unit test with a mocked `ExecuteAsync("MODULE LIST")` returning known-good shapes guards the parser.
4. **Probe storm on cold start.** Orchestrator probes begin as soon as the container starts. If the three backend checks run before `IConnectionMultiplexer.Connect` has completed, they throw `RedisConnectionException` immediately and flap between `Degraded` and `Healthy` for the first 5-10 seconds. **Mitigation:** (a) Kubernetes `initialDelaySeconds: 15` for the readiness probe (documented in the probe-tuning section); (b) the health check catches `RedisConnectionException` and maps to `Degraded` (not thrown) — which is what the existing `DaprStateStoreHealthCheck` does. A one-off startup blip is expected and acceptable behavior.
5. **FalkorDB connection is Redis-protocol — wrong classifier ambiguity.** FalkorDB exposes a Redis-protocol endpoint on port 6380 (AppHost line 50). `RedisConnectionException` from the FalkorDB multiplexer is semantically "graph DB down", not "Redis Stack down". **Mitigation:** the FalkorDB check uses `GetRequiredKeyedService<IConnectionMultiplexer>("falkordb")` (NOT `"redis"`) — the key namespace already disambiguates. A unit test verifies the check resolves the correct keyed multiplexer. The response-writer capability-affected message says "Graph traversal and graph-scoped search unavailable" (not "Redis unavailable").
6. **JSON response contract drift vs. Story 5.5 `TenantIndexStatus`.** Both expose per-backend status, but 8.1's readiness payload is instance-scoped + different field names (`rediSearch` vs. `RediSearch`) would confuse operators reading both. **Mitigation:** reuse the `IndexHealth` enum serialization (CamelCase string via `CamelCaseStringEnumConverter`) and mirror field naming — `rediSearch`, `redisVector`, `falkorDb`, `daprSidecar`, `daprStateStore`. Pin via `BackendHealthResponseWriterTests.JsonShape_MatchesDocumentedSchema`.
7. **Health-endpoint trace exclusion regression.** If the dev adds the custom response writer via a `MapGet` that doesn't inherit the ServiceDefaults filter, the trace-exclusion gets re-established only per endpoint — and a future dev removing the filter wouldn't notice. **Mitigation:** Task 5 explicitly runs the existing regression test `Telemetry_HealthEndpointNotTraced` (added in Story 7.5 Task 11) and adds `BackendHealthResponseWriter_Invocation_DoesNotCreateMemoriesSpan` to pin the invariant from the custom-writer angle.
8. **Capability-affected message coupling to runtime axis state.** The AC says "response indicates which capabilities are affected (e.g., 'Graph traversal unavailable')". If message text is hard-coded per backend, future work adding new backends (e.g., a new search axis in Phase 2) has to edit the response writer. **Mitigation:** put the `backend → affected-capability` mapping in a `BackendCapabilityCatalog` static class — one dictionary, easy to extend. A unit test asserts every backend check name registered in `Program.cs` has an entry in the catalog (compile-time safety via a `nameof()` snapshot).

**Risk → Guard test mapping** (each risk's mitigation is pinned by a specific test):

| # | Risk | Guard test |
|---|------|-----------|
| 1 | Status-aggregation breaks orchestration | `BackendHealthResponseWriterTests.OneBackendDown_ReturnsDegraded` + `ReadyEndpointAggregationTests` (in-memory WebApplicationFactory) + `HealthEndpointIntegrationTests.ReadyEndpoint_FalkorDbDown_ReturnsDegradedWithCapabilities` (if Aspire fixture builds) |
| 2 | Sidecar in liveness → cascading restarts | `ProgramHealthCheckRegistrationTests` asserts `dapr-sidecar` tag set = `["live", "ready"]`; operational mitigation is documented `failureThreshold ≥3` in `docs/dev/health-checks.md` |
| 3 | Redis Stack module detection fragility | `RedisVectorHealthCheckTests` — known-good `MODULE LIST` shapes + lenient parser for ambiguous responses |
| 4 | Probe storm on cold start | Documented `initialDelaySeconds ≥15` in docs (orchestrator config); no unit-level guard — failure mode is operator-configurable |
| 5 | FalkorDB connection classifier ambiguity | `FalkorDbHealthCheckTests` — verifies keyed multiplexer resolution + capability message is "graph" not "Redis" |
| 6 | JSON contract drift vs. `TenantIndexStatus` | `BackendHealthResponseWriterTests.JsonShape_MatchesDocumentedSchema` pins camelCase + field names |
| 7 | Trace-exclusion regression via custom writer | `Telemetry_HealthEndpointNotTraced` (pre-existing from 7.5) + `BackendHealthResponseWriter_Invocation_DoesNotCreateMemoriesSpan` (new) |
| 8 | Capability catalog drift from registrations | `BackendCapabilityCatalogTests` — every registered check name has an entry |

## Story

As an operator,
I want readiness and liveness health checks that verify all backends,
so that I can detect infrastructure issues before they impact users and integrate with orchestrator health probes.

## Acceptance Criteria

1. **Per-backend readiness check with independent reporting (FR72).**
   **Given** the Memories Server is running with Aspire ServiceDefaults,
   **When** `GET /ready` is called,
   **Then** the handler executes three new backend checks — `redisearch`, `redis-vector`, `falkordb` — alongside the existing `dapr-sidecar` and `dapr-statestore` checks
   **And** each check reports independently: the JSON response's `entries` object contains one key per check name with `status` (one of `Healthy` | `Degraded` | `Unhealthy`) and `description` (human-readable reason or success message)
   **And** each check has an independent 3-second timeout (same as existing DAPR checks at `Program.cs:37`)
   **And** each check is registered with tag `"ready"` and `failureStatus: HealthStatus.Degraded` (NOT `Unhealthy` — per Risk #1 mitigation).

2. **All-healthy case returns `Healthy` with per-backend detail.**
   **Given** all three backends are healthy (RediSearch, Redis Vector, FalkorDB all responding) plus both DAPR checks pass,
   **When** the readiness probe returns,
   **Then** HTTP status is `200 OK`
   **And** the JSON response body for `GET /ready` has shape (five entries — the `ready`-tagged checks; `self` does NOT appear here because it is tagged `live` only):
   ```json
   {
     "schemaVersion": 1,
     "status": "Healthy",
     "totalDurationMs": <int>,
     "entries": {
       "dapr-sidecar":   { "status": "Healthy", "description": "Dapr sidecar is responsive.", "durationMs": <int> },
       "dapr-statestore":{ "status": "Healthy", "description": "Dapr state store 'statestore' is accessible.", "durationMs": <int> },
       "redisearch":     { "status": "Healthy", "description": "RediSearch module reachable; N indexes loaded.", "durationMs": <int> },
       "redis-vector":   { "status": "Healthy", "description": "Redis Vector capability reachable.", "durationMs": <int> },
       "falkordb":       { "status": "Healthy", "description": "FalkorDB reachable; N graphs.", "durationMs": <int> }
     }
   }
   ```
   **And** the JSON is served with `Content-Type: application/json` (the default plain-text writer is replaced by `BackendHealthResponseWriter`).
   **And** endpoint-to-entries composition is:
   - `GET /alive` → two entries: `self` + `dapr-sidecar` (both `live`-tagged).
   - `GET /ready` → five entries: `dapr-sidecar` + `dapr-statestore` + `redisearch` + `redis-vector` + `falkordb` (all `ready`-tagged). Note `dapr-sidecar` appears in BOTH endpoints because it carries both tags.
   - `GET /health` → six entries: the union of all registered checks (no predicate).

3. **One backend unhealthy → aggregate `Degraded` with capability-affected message (AC3 from epic).**
   **Given** one backend is unhealthy (e.g., FalkorDB down — `RedisConnectionException` on `GRAPH.LIST`),
   **When** the readiness probe returns,
   **Then** HTTP status is `200 OK` (NOT 503 — the ServiceDefaults status-code map promotes Degraded to 200 at `Extensions.cs:100`)
   **And** the aggregate `status` field is `"Degraded"`
   **And** the failing backend's entry has `status: "Degraded"` (NOT `Unhealthy`, per Risk #1) with `description` containing the failure reason
   **And** the failing backend's entry includes an `affectedCapabilities` array — for `falkordb` that is `["graph-traversal", "graph-scoped-search"]`; for `redisearch` `["syntactic-search", "hybrid-search-syntactic-axis"]`; for `redis-vector` `["semantic-search", "hybrid-search-semantic-axis"]`. Mapping owned by `BackendCapabilityCatalog` (Risk #8 mitigation).

4. **Liveness probe checks process + DAPR sidecar only (AC4 from epic).**
   **Given** the liveness probe at `GET /alive`,
   **When** called,
   **Then** it executes ONLY the `self` check AND the `dapr-sidecar` check — NO backend checks are executed (backend checks are NOT tagged `"live"`)
   **And** `dapr-sidecar` is tagged `["live", "ready"]` (the only tag change in this story)
   **And** the response is `Healthy` → 200 when the sidecar is responsive; `Unhealthy` → 503 when the sidecar is unreachable (sidecar failure IS fatal — unlike backend failure — per Risk #2 rationale)
   **And** probe execution does NOT perform any backend probing (only `self` + `dapr-sidecar` are tagged `live`). No specific latency target is asserted — a quantitative target is out of scope for 8.1 and would require a benchmark harness not yet in place.

5. **Endpoints available at standard paths + integrate with Aspire ServiceDefaults.**
   **Given** a Kubernetes or container-orchestrator deployment,
   **When** health checks are configured,
   **Then** `/ready` is available (readiness endpoint — per ServiceDefaults line 117-121, already wired)
   **And** `/alive` is available (liveness endpoint — per ServiceDefaults line 111-115, already wired)
   **And** both integrate with the existing Aspire ServiceDefaults health-check wiring — NO new endpoint path is added; the endpoints are the existing ones with expanded check registrations.
   **And** the `/health` aggregate endpoint (line 109) returns the union of all checks (same behavior as before 8.1, now with more entries).

6. **Trace exclusion preserved (regression invariant from Story 7.5 AC #5).**
   **Given** the existing trace filter at `ServiceDefaults/Extensions.cs:56-63` excludes `/health`, `/alive`, `/ready` paths,
   **When** a probe hits any of the three paths,
   **Then** no span is emitted on the `Hexalith.Memories` ActivitySource OR the default ASP.NET Core source (pre-existing)
   **And** no `AccessTelemetryEvent` is emitted (health probes are not one of the four audited operation types — per Story 7.5 AC #4 scope)
   **And** the pre-existing test `Telemetry_HealthEndpointNotTraced` at `tests/Hexalith.Memories.Server.Tests/Telemetry/` continues to pass unmodified.

7. **Response writer emits well-formed, stable JSON schema.**
   **Given** the `BackendHealthResponseWriter` replaces the default text writer,
   **When** the writer serializes a `HealthReport`,
   **Then** the output is `application/json` with UTF-8 encoding
   **And** field names are camelCase (matches project-wide JSON convention — MemoriesJsonContext policy)
   **And** the schema documented in AC #2 is **the** schema — field additions are additive (new backends, new capabilities); renames/removals are breaking and require a `schemaVersion` bump + `docs/dev/health-checks.md` migration note
   **And** the top-level `schemaVersion` integer field is emitted on every response (V1 = literal `1`); clients may key migration logic off this field
   **And** a test `BackendHealthResponseWriter_JsonShape_MatchesDocumentedSchema` pins the frozen V1 field manifest (including `schemaVersion: 1`).

8. **Backward compatibility with existing DAPR checks + Story 7.5 telemetry wiring.**
   **Given** the pre-existing `DaprSidecarHealthCheck` + `DaprStateStoreHealthCheck` registrations at `Program.cs:38-51`,
   **When** 8.1 adds the three backend checks + re-tags the sidecar check,
   **Then** the existing DAPR unit tests (`DaprSidecarHealthCheckTests`, `DaprStateStoreHealthCheckTests`) pass without modification (their behavior is unchanged — only the tag list differs, and the existing tests don't assert tags)
   **And** Story 7.5's `Telemetry_HealthEndpointNotTraced` passes without modification
   **And** Story 7.5's `ConfigureOpenTelemetry` flow is NOT modified by this story (8.1 does not touch `ServiceDefaults/Extensions.cs` apart from the response-writer wiring; the meter / source / health-path filter remain exactly as 7.5 left them).

9. **Tests cover the full health-check surface.** *(AC #9 is the **authoritative** source for test-class inventory and per-class test counts. The "Testing standards" section in Dev Notes documents conventions only — if a count in that section conflicts with a number here, this AC wins.)*
   **Given** the consolidated test projects,
   **When** `dotnet test` runs,
   **Then** the following classes exist and pass (Tier 1 — unit — unless marked Integration):
   - `tests/Hexalith.Memories.Server.Tests/HealthChecks/RediSearchHealthCheckTests.cs` — healthy / connection-refused (`RedisConnectionException`) / LOADING response / module-missing / null-context paths.
   - `tests/Hexalith.Memories.Server.Tests/HealthChecks/RedisVectorHealthCheckTests.cs` — 6 tests: same matrix as RediSearch + an explicit "MODULE LIST returned but vector module absent" case. Authoritative count; supersedes any earlier "5" figure.
   - `tests/Hexalith.Memories.Server.Tests/HealthChecks/FalkorDbHealthCheckTests.cs` — healthy / `RedisConnectionException` / `RedisServerException` / driver-level `Exception` (per `TenantMetricsService.GetFalkorDbNodeCountAsync` pattern lines 241-248).
   - `tests/Hexalith.Memories.Server.Tests/HealthChecks/BackendHealthResponseWriterTests.cs` — JSON schema (AC #7), all-healthy shape, one-backend-degraded shape, fully-unhealthy shape, capability-affected message mapping, **AOT-guard roundtrip** (serialize then deserialize; assert every documented V1 property is present with the expected name — catches the silent anonymous-type/AOT regression described in Task 3.3).
   - `tests/Hexalith.Memories.Server.Tests/HealthChecks/BackendCapabilityCatalogTests.cs` — asserts every registered backend check name resolves to a non-empty `affectedCapabilities` list.
   - `tests/Hexalith.Memories.Server.Tests/HealthChecks/ProgramHealthCheckRegistrationTests.cs` — asserts (a) three backend checks registered with tag `"ready"`; (b) `dapr-sidecar` tagged `["live", "ready"]`; (c) `dapr-statestore` tagged `["ready"]` (unchanged); (d) aggregate `Degraded` → 200 mapping preserved. Uses `IHealthChecksBuilder` inspection or `HealthCheckService` resolution via the test host.
   - `tests/Hexalith.Memories.Server.Tests/HealthChecks/ReadyEndpointAggregationTests.cs` — **Tier-1 in-memory end-to-end** test using `WebApplicationFactory<Program>` with a fake failing backend check substituted via DI override. Hits `GET /ready` and asserts: aggregate `status=Degraded`, HTTP `200 OK`, `entries[<fake>].status=Degraded`, `entries[<fake>].affectedCapabilities` non-empty. **Independent of the Aspire fixture** — closes the aggregate-behavior verification gap when `HealthEndpointIntegrationTests` is `[Fact(Skip)]`'d due to the 5.6 CS0311 AppHost build error. This test is the definitive runtime guarantee that Risk #1's mitigation actually works end-to-end.
   - `tests/Hexalith.Memories.IntegrationTests/Health/HealthEndpointIntegrationTests.cs` `[Trait("Category","Integration")]` — three scenarios: (1) all-healthy returns 200 with full payload; (2) FalkorDB down (stop container) returns 200 with `status=Degraded` + `falkordb.status=Degraded` + capability message; (3) DAPR sidecar down returns 503 with `status=Unhealthy`. `[Fact(Skip)]` pattern acceptable if pre-existing CS0311 AppHost build error from 5.6 Dev Notes remains unresolved; un-skip otherwise.

10. **`docs/dev/health-checks.md` documents the health contract.**
    **Given** an operator or developer wants to wire Memories into an orchestrator,
    **When** they read `docs/dev/health-checks.md`,
    **Then** the doc covers:
    - **Endpoint summary table** — `/health`, `/alive`, `/ready` with intended consumer (orchestrator vs. dashboard), aggregate semantics, HTTP status mapping.
    - **Per-check inventory** — table of check name → tag(s) → what it probes → failure semantics (Degraded vs. Unhealthy) → affected capabilities.
    - **Kubernetes probe snippet** — `livenessProbe` (path `/alive`, initialDelaySeconds, failureThreshold ≥3 per Risk #2) and `readinessProbe` (path `/ready`, initialDelaySeconds ≥15 per Risk #4, periodSeconds).
    - **Aspire dashboard linkage** — how the local dashboard surfaces the endpoints during dev.
    - **JSON response schema (AC #7)** — the frozen V1 field manifest.
    - **Capability-affected mapping** — documented table (backend → capabilities) so operators can decode the `affectedCapabilities` array without reading code.
    - **Probe tuning guidance** — `initialDelaySeconds` ≥15, `periodSeconds` 5-10, `failureThreshold` ≥3 (Risk #2 + Risk #4 rationale).
    - **Out-of-scope disclaimer** — no alert thresholds shipped; no consistency verification (Story 8.2); no per-tenant index health (use `/api/tenants/{tenantId}/configuration` or `/api/tenants/{tenantId}/telemetry/summary`).

## Tasks / Subtasks

### Task Summary (orientation)

7 top-level tasks in a mostly-linear order. Tasks 1 + 2 + 3 are parallelizable substrate work (three check classes + catalog + writer); Tasks 4-5 depend on 1-3; Tasks 6-7 close the loop with tests + docs.

- **Substrate:** Tasks 1, 2, 3 (three checks + capability catalog + response writer)
- **Integration:** Tasks 4, 5 (Program.cs wiring + ServiceDefaults writer wiring)
- **Verification:** Tasks 6, 7 (tests + docs)

---

- [ ] **Task 1: Create `RediSearchHealthCheck` + `RedisVectorHealthCheck` (AC: #1, #2, #3, #7, #9)**
  - [ ] 1.1 Create `src/Hexalith.Memories.Server/HealthChecks/RediSearchHealthCheck.cs` — `public sealed class` implementing `IHealthCheck`. Constructor takes `[FromKeyedServices("redis")] IConnectionMultiplexer redis` (mirrors `TenantMetricsService` line 40-41 pattern — the KeyedServices attribute is tolerated on constructor params for ASP.NET Core DI). Implementation: on `CheckHealthAsync`, execute `FT._LIST` against `redis.GetDatabase()` via `db.ExecuteAsync("FT._LIST")`. Return `HealthCheckResult.Healthy($"RediSearch module reachable; {indexCount} indexes loaded.")` on success (parse result as `RedisResult[]` and use `.Length` for `indexCount`). On `RedisConnectionException` / `RedisServerException` LOADING|BUSY → `new HealthCheckResult(context.Registration.FailureStatus, $"RediSearch unreachable: {ex.GetType().Name}", ex)`. On generic `RedisException` → same Degraded. The `context.Registration.FailureStatus` resolves to `Degraded` because Task 4 registers the check with `failureStatus: HealthStatus.Degraded`.
  - [ ] 1.2 Copyright header matches the existing `DaprSidecarHealthCheck.cs` block (ITANEO header). Use file-scoped namespace + primary-constructor pattern (`public sealed class RediSearchHealthCheck([FromKeyedServices("redis")] IConnectionMultiplexer redis) : IHealthCheck` — same shape as existing `DaprStateStoreHealthCheck` line 13). Validate non-null in the primary-constructor-to-field assignment (same pattern as existing checks).
  - [ ] 1.2a **Log-level guidance.** The per-probe code path emits at most one log on state transition — NOT per successful probe. Inject an `ILogger<RediSearchHealthCheck>` and use a `[LoggerMessage(EventId = 8101, Level = LogLevel.Warning, Message = "RediSearch probe transitioned to Degraded: {reason}")]` partial method. Do NOT log on every Healthy result — orchestrator probes at 5Hz × 10 pods × 3 backends = 150 log entries/sec of noise. The framework-level `Microsoft.Extensions.Diagnostics.HealthChecks` logger emits its own Debug-level trace of each probe; that's sufficient for diagnostic purposes. EventIds 8101-8103 for RediSearch / 8111-8113 for Redis Vector / 8121-8123 for FalkorDB (state-transitions-only from the 8100-8199 bank reserved by this story — see "Previous story intelligence").
  - [ ] 1.3 Create `src/Hexalith.Memories.Server/HealthChecks/RedisVectorHealthCheck.cs` — same shape. Implementation: execute `MODULE LIST` via `db.ExecuteAsync("MODULE", "LIST")`; iterate the returned `RedisResult[]`, parse each entry as an array, find one whose `name` field is `"search"` (Redis Stack bundles RediSearch + Vector into the `search` module — verified against architecture.md line 228 `redis/redis-stack`). Return `Healthy("Redis Vector capability reachable.")` if found; `Degraded("Vector module absent from MODULE LIST response.")` if the module is not present. On exception: same pattern as 1.1.
  - [ ] 1.4 Do NOT add tenant-scoped probing (no `FT.INFO {tenantId}`). Instance-level probe only — per "What does NOT ship" bullet #1.
  - [ ] 1.5 Unit tests `tests/Hexalith.Memories.Server.Tests/HealthChecks/RediSearchHealthCheckTests.cs` — mirror the `DaprSidecarHealthCheckTests` shape (5 tests: happy / connection-refused / LOADING / module-missing / null-context). Use `NSubstitute.For<IConnectionMultiplexer>()` + mocked `IDatabase.ExecuteAsync` returning canned `RedisResult`. Shouldly assertions. Tests inherit the existing `Hexalith.Memories.Server.Tests` test project setup — no new fixture wiring needed.
  - [ ] 1.6 Unit tests `...RedisVectorHealthCheckTests.cs` — same matrix + specific case for "MODULE LIST returned but vector module absent".

- [ ] **Task 2: Create `FalkorDbHealthCheck` (AC: #1, #3, #9)**
  - [ ] 2.1 Create `src/Hexalith.Memories.Server/HealthChecks/FalkorDbHealthCheck.cs` — `public sealed class` implementing `IHealthCheck`. Constructor takes `[FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb`. Implementation: wrap a `GRAPH.LIST` call. Use `NFalkorDB.FalkorDB falkor = new(falkorDb.GetDatabase())` + `falkor.ListAsync()` if NFalkorDB exposes it; otherwise fall back to `falkorDb.GetDatabase().ExecuteAsync("GRAPH.LIST")` and parse the result array. Return `Healthy($"FalkorDB reachable; {graphCount} graphs.")` on success.
  - [ ] 2.2 Exception handling mirror the pattern at `TenantMetricsService.GetFalkorDbNodeCountAsync` lines 225-248: `RedisServerException` with "no such graph"/"unknown graph" → `Healthy` (empty instance is still healthy — NOT a failure); other `RedisServerException` → Degraded; `RedisConnectionException` → Degraded; generic `Exception` catch-all (NFalkorDB can surface driver-level parse failures) → Degraded. Never throw from the method.
  - [ ] 2.3 Unit tests `tests/Hexalith.Memories.Server.Tests/HealthChecks/FalkorDbHealthCheckTests.cs` — 4 tests (healthy with graphs / healthy empty / connection-refused / server-exception).

- [ ] **Task 3: Create `BackendCapabilityCatalog` + `BackendHealthResponseWriter` (AC: #2, #3, #7, #8)**
  - [ ] 3.1 Create `src/Hexalith.Memories.Server/HealthChecks/BackendCapabilityCatalog.cs` — `internal static class` with a `public static IReadOnlyDictionary<string, IReadOnlyList<string>> Map` containing:
    ```csharp
    ["redisearch"]    = ["syntactic-search", "hybrid-search-syntactic-axis"],
    ["redis-vector"]  = ["semantic-search", "hybrid-search-semantic-axis"],
    ["falkordb"]      = ["graph-traversal", "graph-scoped-search"],
    ["dapr-sidecar"]  = ["all-service-invocation", "workflow-orchestration", "actor-runtime"],
    ["dapr-statestore"] = ["workflow-state-persistence", "actor-state-persistence"]
    ```
    Expose a `static IReadOnlyList<string> GetCapabilities(string checkName)` that returns `Map[checkName]` or an empty list. Use `StringComparer.Ordinal` for the dictionary (check names are internal IDs, not user-facing text).
  - [ ] 3.2 Create `src/Hexalith.Memories.Server/HealthChecks/BackendHealthResponseWriter.cs` — `internal static class` with `public static Task WriteAsync(HttpContext context, HealthReport report)`. Serializes to JSON via `System.Text.Json.JsonSerializer` using `MemoriesJsonContext.Options` (the camelCase + source-gen resolver). Shape matches AC #2 exactly:
    ```csharp
    new
    {
        schemaVersion = 1, // Pinned V1 per AC #7. Increment (+ migration note in docs/dev/health-checks.md) on breaking rename/removal. Additive fields stay at 1.
        status = report.Status.ToString(), // "Healthy" | "Degraded" | "Unhealthy"
        totalDurationMs = (int)report.TotalDuration.TotalMilliseconds,
        entries = report.Entries.ToDictionary(
            kv => kv.Key,
            kv => new
            {
                status = kv.Value.Status.ToString(),
                description = kv.Value.Description ?? string.Empty,
                durationMs = (int)kv.Value.Duration.TotalMilliseconds,
                affectedCapabilities = kv.Value.Status == HealthStatus.Healthy
                    ? Array.Empty<string>()
                    : BackendCapabilityCatalog.GetCapabilities(kv.Key).ToArray()
            })
    }
    ```
    Set `context.Response.ContentType = "application/json; charset=utf-8"`. Use `await context.Response.WriteAsync(json)`.
  - [ ] 3.3 Do NOT register `BackendHealthResponseWriter` types in `MemoriesJsonContext` — the writer uses anonymous objects and falls back to the reflection-based resolver in `MemoriesJsonContext.Options` (the `Combine(MemoriesJsonSourceGenerationContext.Default, new DefaultJsonTypeInfoResolver())` at line 126-128). AOT build is NOT expected on the Server project (per csproj — `Microsoft.NET.Sdk.Web`, no `PublishAot`). If Phase 2 enables AOT on the server, a follow-up story swaps the anonymous types for named records and adds them to the JSON context.
  - [ ] 3.3a **No probe correlation ID in V1.** The response entries do NOT include a `probeId`, `traceId`, or `traceparent`-echo field. Rationale: health paths are deliberately excluded from tracing (Story 7.5 AC #5) — generating a trace just for correlation would flip that invariant. Operators debugging a failed probe correlate by timestamp: the `durationMs` + the DAPR sidecar log (DaprClient emits its own correlation) + the system clock. If operators ask for correlation IDs later, a future story can add a V1-compatible `probeId` field (monotonic counter from `Interlocked.Increment` on a static `long`; zero allocation, no trace dependency). Document this decision in `docs/dev/health-checks.md` under "Debugging failed probes" so operators aren't surprised.
  - [ ] 3.4 Unit tests `tests/Hexalith.Memories.Server.Tests/HealthChecks/BackendHealthResponseWriterTests.cs` — 6 tests: (a) all-healthy JSON shape; (b) one-backend-Degraded with affectedCapabilities populated; (c) unhealthy DAPR sidecar with its own capability list; (d) unknown check name gets empty capabilities (graceful degradation, no exception); (e) `ContentType` + encoding assertion; (f) **AOT-guard roundtrip** — deserialize the emitted JSON back into a `Dictionary<string,JsonElement>` and assert each documented V1 property name is present with the expected type (string for `status` / `description`, number for `durationMs` / `totalDurationMs`, array for `affectedCapabilities`). Rationale: the writer uses anonymous types intentionally (see Task 3.3); if `PublishAot` is ever enabled on Server without the follow-up conversion to named records, serialization could silently emit `{}`. This test traps the regression without requiring an AOT build. Build a fake `HealthReport` via `new HealthReport(entries, status, duration)` — all constructors are public.
  - [ ] 3.5 Unit test `tests/Hexalith.Memories.Server.Tests/HealthChecks/BackendCapabilityCatalogTests.cs` — asserts every key in the catalog map matches one of the five check names used in `Program.cs` registrations (use `nameof`-based snapshot or a pinned array — Task 4.3 updates the array alongside the registrations).

- [ ] **Task 4: Register new checks + re-tag sidecar in `Program.cs` (AC: #1, #4, #5, #8)**
  - [ ] 4.1 Open `src/Hexalith.Memories.Server/Program.cs` at lines 37-51. Extend the existing `AddHealthChecks()` chain:
    ```csharp
    .AddCheck<DaprSidecarHealthCheck>(
        "dapr-sidecar",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["live", "ready"],        // CHANGED — was ["ready"]
        timeout: healthCheckTimeout)
    .Add(new HealthCheckRegistration(
        "dapr-statestore",
        sp => new DaprStateStoreHealthCheck(
            sp.GetRequiredService<DaprClient>(),
            "statestore"),
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: healthCheckTimeout))
    .AddCheck<RediSearchHealthCheck>(
        "redisearch",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready"],
        timeout: healthCheckTimeout)
    .AddCheck<RedisVectorHealthCheck>(
        "redis-vector",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready"],
        timeout: healthCheckTimeout)
    .AddCheck<FalkorDbHealthCheck>(
        "falkordb",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready"],
        timeout: healthCheckTimeout);
    ```
  - [ ] 4.2 The three new checks resolve their `IConnectionMultiplexer` via `[FromKeyedServices]` attributes on their constructors (Task 1.1, 1.3, 2.1). `AddCheck<T>` needs `T` to be constructable from DI — verify by running the unit tests before wiring if unsure (a missing keyed-service resolution throws at first probe, not at registration).
  - [ ] 4.3 If Task 3.5 uses a pinned array of check names, update it to match the five names above.
  - [ ] 4.4 Unit test `tests/Hexalith.Memories.Server.Tests/HealthChecks/ProgramHealthCheckRegistrationTests.cs` — uses `WebApplicationFactory<Program>` (or a minimal test host with `Program.cs`'s `AddHealthChecks` segment refactored into a test-accessible extension IF needed — see Dev Notes "Program.cs testability"). Assert: resolve `IOptions<HealthCheckServiceOptions>` (or iterate the registered `HealthCheckRegistration` via `IHealthChecksBuilder` inspection pattern) → five checks registered → tag sets match AC #4 + AC #8. This test is the guard for the tag-list regression risk.

- [ ] **Task 5: Wire `BackendHealthResponseWriter` into `MapDefaultEndpoints` (AC: #2, #5, #6, #7)**
  - [ ] 5.1 Open `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` at lines 93-124. Modify `MapDefaultEndpoints` to set `ResponseWriter` on each `HealthCheckOptions`:
    ```csharp
    var healthOptions = new HealthCheckOptions
    {
        ResultStatusCodes = statusCodes,
        ResponseWriter = Hexalith.Memories.Server.HealthChecks.BackendHealthResponseWriter.WriteAsync,
    };
    ```
    Apply the same `ResponseWriter` to the `/alive` and `/ready` options too (each `new HealthCheckOptions { ... }` gets the writer).
  - [ ] 5.2 **Referencing across projects:** `ServiceDefaults` does NOT currently reference `Server`. Avoid a new project reference — inverting the dependency breaks the clean "ServiceDefaults is the foundation" layering. **Resolution:** move `BackendHealthResponseWriter` to `ServiceDefaults` itself (not `Server`) at `src/Hexalith.Memories.ServiceDefaults/Health/BackendHealthResponseWriter.cs`. The `BackendCapabilityCatalog` ALSO moves to `ServiceDefaults/Health/` since the writer needs it. The three check classes STAY in `Server` (they have `Dapr.Client` / `StackExchange.Redis` / NFalkorDB dependencies that don't belong in `ServiceDefaults`). **Revise Task 3.1-3.2 paths accordingly.** `MemoriesJsonContext` lives in `Contracts` — accessible from `ServiceDefaults` because `ServiceDefaults` can reference `Contracts` cleanly (check the csproj — if it doesn't already, add the ProjectReference).
  - [ ] 5.3 After the path change, `ServiceDefaults/Hexalith.Memories.ServiceDefaults.csproj` MUST reference `Hexalith.Memories.Contracts.csproj` (for `MemoriesJsonContext`). Check the csproj; add `<ProjectReference Include="..\Hexalith.Memories.Contracts\Hexalith.Memories.Contracts.csproj" />` if missing.
  - [ ] 5.4 Preserve the trace-exclusion filter at `ServiceDefaults/Extensions.cs:56-63` **byte-for-byte**. Do not reorder lines in `ConfigureOpenTelemetry`. The 7.5 regression test `Telemetry_HealthEndpointNotTraced` is the guard.
  - [ ] 5.5 **Define health path constants as a single source of truth.** Add `internal static class HealthEndpointPaths` in `src/Hexalith.Memories.ServiceDefaults/Health/HealthEndpointPaths.cs` with three consts: `public const string Health = "/health";`, `public const string Alive = "/alive";`, `public const string Ready = "/ready";`. Replace the literal strings in BOTH `MapDefaultEndpoints` (lines 109, 113, 119) AND the trace-exclusion filter (lines 59-63) with references to these constants. Rationale: today if a dev renames `/health` → `/healthz` for an orchestrator convention, they could update the endpoint mapping but miss the filter predicate — silently breaking the Story 7.5 trace-exclusion invariant. The `Telemetry_HealthEndpointNotTraced` test would still pass against the new path, giving false assurance. One constant = one change point = no drift.
  - [ ] 5.6 After wiring, run `dotnet build src/Hexalith.Memories.ServiceDefaults` + `dotnet build src/Hexalith.Memories.Server` to confirm the cross-project reference resolves and no `CS0246` surfaces.

- [ ] **Task 6: Integration test + optional AppHost smoke (AC: #9)**
  - [ ] 6.1 Create `tests/Hexalith.Memories.IntegrationTests/Health/HealthEndpointIntegrationTests.cs` with `[Trait("Category","Integration")]`. Three scenarios:
    - `ReadyEndpoint_AllHealthy_Returns200WithFiveEntries` — fixture starts Aspire, hits `GET /ready`, parses JSON, asserts `status="Healthy"` + five entries + `redisearch.status="Healthy"` etc.
    - `ReadyEndpoint_FalkorDbDown_ReturnsDegradedWithCapabilities` — stop the FalkorDB container resource via `IDistributedApplicationTestingBuilder` (Aspire testing supports `resource.Stop`), hit `/ready`, assert `status="Degraded"` + `falkordb.status="Degraded"` + `falkordb.affectedCapabilities=["graph-traversal","graph-scoped-search"]` + HTTP status 200.
    - `AliveEndpoint_Default_Returns200WithSidecarCheck` — hits `/alive`, asserts entries contain `self` + `dapr-sidecar` ONLY (no backend checks).
  - [ ] 6.2 If the Aspire fixture CS0311 build error from Story 5.6 Dev Notes is still unresolved at 8.1 landing time, apply `[Fact(Skip = "Aspire fixture build failure tracked in 5.6 Dev Notes")]` to the three tests — same deferral pattern 5.6 established. Un-skip once the fixture builds. Document this status in Completion Notes.
  - [ ] 6.3 Do NOT write a CLI-side test for 8.1 — the CLI does not currently expose a health-check subcommand and adding one is out of scope (Story 8.2 / 8.3 may add `memories status` if operator need surfaces; don't speculate here).

- [ ] **Task 7: Author `docs/dev/health-checks.md` (AC: #10)**
  - [ ] 7.1 Create `docs/dev/health-checks.md`. Open with a one-paragraph statement of purpose: "Operator-facing reference for the Memories Server's liveness and readiness endpoints. Describes the probe contract, response shape, orchestrator wiring, and capability semantics. Complements [docs/dev/telemetry.md](./telemetry.md) (metrics + traces + audit events) — health checks answer 'is the service up?'; telemetry answers 'is it working well?'".
  - [ ] 7.2 Section: **Endpoint summary** — table `Endpoint | Path | Predicate | Typical Consumer | Aggregate on failure | HTTP status`. Three rows: `/health` (no predicate, dashboard), `/alive` (live tag, Kubernetes livenessProbe → pod restart), `/ready` (ready tag, Kubernetes readinessProbe → service-rotation gate).
  - [ ] 7.3 Section: **Check inventory** — table of five checks from Task 4 (`self`, `dapr-sidecar`, `dapr-statestore`, `redisearch`, `redis-vector`, `falkordb`). Columns: name / tags / probe description / failure status / affected capabilities. Values from `BackendCapabilityCatalog.Map` (Task 3.1).
  - [ ] 7.4 Section: **Response shape** — the frozen V1 JSON schema from AC #2 + #7 including the top-level `schemaVersion: 1` field. Include a worked example of a degraded response (FalkorDB down). Add a note: "Schema is versioned by the `schemaVersion` field. Additive field changes keep `schemaVersion: 1`; rename/removal bumps to `schemaVersion: 2` with a migration note in this doc." Add a **sub-note on `affectedCapabilities` consumers**: "V1 ships this array as an operator-facing diagnostic signal. No production gateway or proxy today auto-routes on `affectedCapabilities` — capability-aware routing is a future story. Clients that care about capability-specialized requests (graph-only, vector-only) MUST read the array themselves and decide; they cannot assume a gateway does it for them." Add a **sub-note on debugging failed probes**: "V1 does not include a `probeId` or `traceId` field — health paths are excluded from OpenTelemetry by design (Story 7.5). Correlate failing probes with sidecar/backend logs by timestamp and `durationMs`. If correlation IDs become necessary in practice, a future story adds a V1-compatible `probeId` field (monotonic counter, zero trace overhead)."
  - [ ] 7.5 Section: **Orchestrator probe configuration** — **Kubernetes** YAML snippet:
    ```yaml
    livenessProbe:
      httpGet: { path: /alive, port: 5000 }
      initialDelaySeconds: 10
      periodSeconds: 10
      failureThreshold: 3     # tolerate sidecar blips per Story 8.1 Risk #2
    readinessProbe:
      httpGet: { path: /ready, port: 5000 }
      initialDelaySeconds: 15 # per Story 8.1 Risk #4 — allow multiplexer warmup
      periodSeconds: 5
      failureThreshold: 2
    ```
    **Docker** `HEALTHCHECK` (plain-Docker / Podman deployments without K8s):
    ```dockerfile
    # In the Dockerfile, after EXPOSE:
    HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=3 \
      CMD curl --fail --silent --show-error http://localhost:5000/alive || exit 1
    ```
    Notes on the Docker form: (a) `--start-period=20s` parallels `initialDelaySeconds: 15` + 5s buffer for image cold-start; (b) Docker has no equivalent of Kubernetes' separate readiness probe — `HEALTHCHECK` controls container status only, not upstream load-balancer rotation. For environments that need readiness semantics (e.g., Docker Swarm with external LB), probe `/ready` via an external sidecar rather than `HEALTHCHECK`; (c) the `curl` path requires the base image to include curl — for distroless or `mcr.microsoft.com/dotnet/runtime-deps` images, use `wget -q --spider` or a compiled health-check binary.
    **Docker Compose** override (maps `HEALTHCHECK` via compose YAML without rebuilding the image):
    ```yaml
    services:
      memories-server:
        healthcheck:
          test: ["CMD-SHELL", "curl --fail --silent http://localhost:5000/alive || exit 1"]
          interval: 10s
          timeout: 5s
          start_period: 20s
          retries: 3
    ```
  - [ ] 7.6 Section: **Aspire dashboard** — paragraph pointing to the local dashboard (http://localhost:18888) and its resource-health column; one screenshot-caption placeholder if the team wants to add one later (don't embed an image — document-only).
  - [ ] 7.7 Section: **Capability-affected mapping** — reproduce the `BackendCapabilityCatalog.Map` contents as a Markdown table so operators can decode `affectedCapabilities` arrays without reading code.
  - [ ] 7.8 Section: **Probe tuning guidance** — expand on 7.5 with the rationale from Risks #2 and #4 (sidecar auto-restart, multiplexer warmup). One paragraph each. Include a **"Blast radius"** sub-note explaining that the `dapr-sidecar` check is shared by both `/alive` AND `/ready` — a DAPR control-plane glitch is correlated across all pods and can trigger simultaneous liveness-probe failures. Recommend `failureThreshold ≥3` + `periodSeconds ≥10` for liveness to tolerate transient blips; warn against lowering these defaults without understanding the correlated-failure mode.
  - [ ] 7.9 Section: **Known gaps and limitations** — document the "architecturally optional graph axis" gap explicitly: if FalkorDB is intentionally omitted from a deployment (architecture.md line 151 flags graph as optional), the readiness endpoint will permanently report `falkordb.status=Degraded` with capability-affected message. Operators cannot currently distinguish "FalkorDB is down" from "FalkorDB is intentionally disabled" from the `/ready` payload. Workaround: set orchestrator alerting rules to ignore `falkordb` failures in graph-disabled deployments. Full resolution deferred to Phase 2 (axis-optionality signal in the check registration). This paragraph also notes the "capability-affected routing" gap: the `affectedCapabilities` array is consumed manually today; no gateway/proxy layer automatically routes away from degraded capabilities — clients MUST honor the array themselves.
  - [ ] 7.10 Section: **Out of scope** — short bulleted list: alert thresholds, consistency verification (→ Story 8.2), data export (→ Story 8.3), per-tenant index health (→ use `/api/tenants/{tenantId}/configuration` or `/api/tenants/{tenantId}/telemetry/summary`).
  - [ ] 7.11 Link `docs/dev/health-checks.md` from `docs/dev/telemetry.md` (Story 7.5's doc) under its "See also" section if that section exists — light cross-reference, not a full re-doc.

## Dev Notes

### Pre-flight verification (run before Task 1)

Run these commands at the start of the implementation session to catch state drift and confirm the baseline the story assumes:

1. **Confirm sprint + story status consistent with this file.**
   ```bash
   grep "8-1-health-checks-and-readiness" _bmad-output/implementation-artifacts/sprint-status.yaml
   # Expect: 8-1-health-checks-and-readiness: ready-for-dev
   ```
2. **Understand the 7.5 state in the working tree** (see Previous story intelligence — 7.5 is in-progress at 8.1 planning time).
   ```bash
   git status --short | grep -E "Telemetry|ServiceDefaults|Extensions\.cs"
   # If 7.5 artifacts appear, decide per the landing-order coordination bullet: rebase on 7.5 merge or coordinate on the active branch.
   ```
3. **Confirm the baseline test suite is green for health-check templates** (these are the shape you will copy).
   ```bash
   dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~HealthChecks"
   # Expect: DaprSidecarHealthCheckTests + DaprStateStoreHealthCheckTests all passing.
   ```
4. **Confirm ServiceDefaults builds before you add the Contracts ProjectReference** (Task 5.3). Capture current state; re-run after the csproj edit to prove no CS0246 regression.
   ```bash
   dotnet build src/Hexalith.Memories.ServiceDefaults/Hexalith.Memories.ServiceDefaults.csproj
   ```
5. **Read the trace-exclusion filter as-is so you can preserve it byte-for-byte.**
   ```bash
   sed -n '56,65p' src/Hexalith.Memories.ServiceDefaults/Extensions.cs
   # The predicate body is lines 59-63; line 56 is the WithTracing lambda open.
   ```
6. **Verify the Aspire testing API used by Task 6.1 matches the pinned package version.** `IDistributedApplicationTestingBuilder` + per-resource `Stop`/start control moved between Aspire 8.x and 9.x; the exact API surface depends on what's in `Directory.Packages.props`.
   ```bash
   grep -E "Aspire\.Hosting\.Testing|Aspire\.Hosting" Directory.Packages.props
   # Note the version. Cross-check the documented Task 6.1 pattern (`resource.Stop` via IDistributedApplicationTestingBuilder) against the Aspire docs for that version BEFORE writing the test. If the API has drifted, adjust the test fixture plan — do NOT invent a resource-lifecycle API that doesn't exist in the pinned package.
   ```

If any step surfaces unexpected state (e.g., baseline tests red, 7.5 artifacts committed under a different shape, Aspire testing API drift), **stop and sync with the SM / user before coding** — the assumptions in this story are pinned to the state captured on 2026-04-17.

### Architecture alignment

- **Follow the existing `DaprSidecarHealthCheck` + `DaprStateStoreHealthCheck` pattern** (`src/Hexalith.Memories.Server/HealthChecks/*.cs`) for new check classes: file-scoped namespace, ITANEO copyright header, `public sealed class` implementing `IHealthCheck`, primary-constructor null validation (via fields), `try/catch` producing `HealthCheckResult` — never throwing. The existing unit test shape (`DaprSidecarHealthCheckTests.cs`) is the template for new tests — copy the `CreateContext()` helper, use `NSubstitute` + `Shouldly`.
- **Reuse keyed `IConnectionMultiplexer` services** registered at `Program.cs:80-83` (`"redis"` + `"falkordb"`). The new checks resolve via `[FromKeyedServices]` on constructor params — ASP.NET Core DI honors the attribute on check classes instantiated via `AddCheck<T>`. The `TenantMetricsService` ctor at `TenantMetricsService.cs:40-41` demonstrates the exact pattern.
- **Reuse `IndexHealth` enum semantics**: the enum distinguishes data state from availability state. For health checks the distinction collapses — connectivity failure + "no such index at instance level" both surface as `Degraded` in AC3. The `affectedCapabilities` array encodes the "what broke" dimension; the `IndexHealth`-equivalent (`status` in the JSON) encodes the "how broken" dimension (Healthy / Degraded / Unhealthy).
- **Status-aggregation design** (Risk #1 matrix):
  | Check               | failureStatus       | Aggregate when failing alone | HTTP |
  |---------------------|---------------------|------------------------------|------|
  | dapr-sidecar        | Unhealthy           | Unhealthy                    | 503  |
  | dapr-statestore     | Unhealthy           | Unhealthy                    | 503  |
  | redisearch          | **Degraded**        | Degraded                     | 200  |
  | redis-vector        | **Degraded**        | Degraded                     | 200  |
  | falkordb            | **Degraded**        | Degraded                     | 200  |
  The rule: "backend unavailable" = service can still serve a reduced set of operations → Degraded/200. "DAPR unavailable" = service can serve almost nothing → Unhealthy/503 → orchestrator pulls it from rotation. This inversion is deliberate and matches Story 5.6's partial-failure philosophy (hybrid search goes Degraded when one axis fails; 503 only when all axes fail).
- **Trace-exclusion preservation**: the filter predicate at `ServiceDefaults/Extensions.cs` lines 59-63 (within the `WithTracing` lambda opened at line 56) is load-bearing — Story 7.5 AC #5 pins it. Do NOT move `/ready` to a different path. Do NOT add the paths to a separate `MapGet`. The existing three `MapHealthChecks` calls stay; only the options object changes.
- **Writer placement**: per Task 5.2, the response writer MUST live in `ServiceDefaults`, not `Server`. `ServiceDefaults` already depends on `Contracts` implicitly (via the AspNetCore/OTel/health-check packages); verify by csproj.

### Previous story intelligence

**Story 7.5 (Search & Access Telemetry) — in-progress, most recent.** Key alignment points:

- **Landing-order coordination (7.5 not yet merged).** At 8.1 planning time, 7.5 shows status `in-progress` in `_bmad-output/implementation-artifacts/sprint-status.yaml`. `git status` shows `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` modified, `src/Hexalith.Memories.Server/Telemetry/` directory untracked, and `tests/Hexalith.Memories.Server.Tests/Telemetry/` untracked. Implication for 8.1 dev: **(a)** prefer rebasing on / waiting for 7.5's merge before starting 8.1 so the Telemetry + ServiceDefaults edits don't collide; **(b)** if 8.1 MUST start with 7.5 still in-flight, coordinate on the same working branch — the trace-exclusion filter at `ServiceDefaults/Extensions.cs` lines 59-63 (predicate body; wrapper `WithTracing` starts at line 56) MUST be preserved byte-for-byte even when the 7.5 regression test is not yet committed; **(c)** 7.5 Task 9.4 creates the regression test as `tests/Hexalith.Memories.Server.Tests/Telemetry/TelemetryHealthExclusionTests.cs` containing test method `Telemetry_HealthEndpointNotTraced` — if the file does not yet exist when 8.1 runs `dotnet test`, that is expected; do NOT create a placeholder.
- 7.5 explicitly calls out in Risk #8 that the health-endpoint filter at `ServiceDefaults/Extensions.cs` lines 59-63 is a pinned invariant. 8.1 inherits the invariant — do NOT move the filter or change path strings. 7.5's regression test method `Telemetry_HealthEndpointNotTraced` (in file `TelemetryHealthExclusionTests.cs`) is the cross-story guard once landed.
- **Merge-conflict protocol with 7.5** (both stories touch `ServiceDefaults/Extensions.cs`):
  - **Collision zone:** lines 56-63 (trace-exclusion filter) — 7.5 is adding the filter, 8.1 is preserving it byte-for-byte. Direct collision if 7.5's exact final shape differs from what 8.1 assumes. Lines 93-124 (`MapDefaultEndpoints`) — 8.1 modifies the response writer; 7.5 does not, but close enough that rebasing context-diffs may conflict.
  - **Preferred order:** 7.5 lands first. 8.1 rebases onto main after the 7.5 PR is merged. This is the lowest-risk sequencing because 8.1's trace-exclusion invariant is defined relative to 7.5's final filter shape.
  - **If 8.1 MUST start before 7.5 merges** (sprint pressure): branch from a shared base with the 7.5 dev; coordinate on a single working branch; rebase forward when 7.5's `ServiceDefaults/Extensions.cs` reaches its final form. Do NOT land 8.1 with a different filter shape than 7.5 — `Telemetry_HealthEndpointNotTraced` will fail in main after both merge.
  - **Conflict-resolution rule:** if rebase produces conflict markers in lines 56-63, the 7.5 shape wins verbatim; 8.1 only adds the `ResponseWriter` wiring at lines 93-124. No 8.1 edit should alter the predicate body or the ActivitySource name.
- 7.5 adds `TelemetrySummaryService` that computes per-tenant index sizes + health — operator-facing at `/api/tenants/{tenantId}/telemetry/summary`. 8.1 is instance-scoped; the two endpoints are complementary (8.1 = "is the service reachable?"; 7.5 summary = "how is tenant X's index doing?"). Do NOT duplicate the per-tenant index probing in 8.1 — the call stack for 7.5 already lazy-reads `TenantMetricsService.GetIndexSizesAsync`.
- 7.5 pins EventId bank `7500-7599`. Stories before that used banks `5400, 5500, 5600, 6100, 6200, 6300`. **Story 8.1 uses EventId bank `8100-8199`** for any new `[LoggerMessage]` emitters. If 8.1 emits no structured logs (the health checks already produce `HealthCheckResult` which flows through ASP.NET Core's standard logger), no EventIds are consumed — acceptable. Guideline: only emit an 81xx log event for operator-actionable state transitions (e.g., first-time-degraded detection) — not per-probe chatter.

**Story 5.6 (Graceful Degradation) — latest done story in Epic 5.** Key alignment:

- 5.6 establishes the "Degraded-is-not-Unhealthy" pattern for search endpoints: per-axis failures return 200 OK with `degraded=true`. 8.1 applies the same philosophy to backend health checks — connectivity failure → Degraded (200), not Unhealthy (503). The aggregation behavior must match so operators see a consistent "service is still serving partial capability" signal across both the request path AND the health-probe path.
- 5.6 notes the AppHost has a pre-existing `CS0311 on IDaprSidecarResource` build error that prevents the IntegrationTests project from building. 8.1 Task 6.2 inherits the same `[Fact(Skip)]` deferral pattern if the build error remains. If resolved by a prior PR, un-skip.
- 5.6's `SearchEndpointDegradationLog` (EventIds 5601-5603) establishes the one-file-per-log-category-partial-class pattern. 8.1 does not add new log categories — the health checks use the existing Microsoft.Extensions.Diagnostics.HealthChecks logger.

**Story 5.5 (Tenant Configuration & Listing) — direct dependency.** Key alignment:

- 5.5 ships `TenantMetricsService` with the exact Redis + FalkorDB probe patterns 8.1 should mirror (`FT.INFO` for Redis, `MATCH (n) RETURN count(n)` for FalkorDB). Task 1 + 2 implementations are essentially "the 5.5 pattern, instance-scoped instead of tenant-scoped, called from `IHealthCheck.CheckHealthAsync` instead of a per-endpoint handler".
- 5.5 introduces `IndexHealth` enum and `TenantIndexStatus` record. 8.1 **does NOT reuse `TenantIndexStatus` as its response schema** — 8.1's readiness payload has different field names and includes DAPR checks (`TenantIndexStatus` has only three axes). Using the same type would force `TenantIndexStatus` to absorb health-check concerns or would create a confusing partial-overlap. Keep the readiness JSON shape separate and documented; they are complementary outputs for different consumers.

### Project structure notes

**Paths (canonical):**

- `src/Hexalith.Memories.Server/HealthChecks/RediSearchHealthCheck.cs` (new)
- `src/Hexalith.Memories.Server/HealthChecks/RedisVectorHealthCheck.cs` (new)
- `src/Hexalith.Memories.Server/HealthChecks/FalkorDbHealthCheck.cs` (new)
- `src/Hexalith.Memories.ServiceDefaults/Health/BackendCapabilityCatalog.cs` (new) — **NOT under `Server/`** per Task 5.2 decision.
- `src/Hexalith.Memories.ServiceDefaults/Health/BackendHealthResponseWriter.cs` (new) — same rationale.
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` (modified — writer wiring lines 93-124).
- `src/Hexalith.Memories.Server/Program.cs` (modified — lines 38-51 check registrations + sidecar tag).
- `src/Hexalith.Memories.ServiceDefaults/Hexalith.Memories.ServiceDefaults.csproj` (modified — add Contracts ProjectReference if missing).
- `tests/Hexalith.Memories.Server.Tests/HealthChecks/RediSearchHealthCheckTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/HealthChecks/RedisVectorHealthCheckTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/HealthChecks/FalkorDbHealthCheckTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/HealthChecks/BackendHealthResponseWriterTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/HealthChecks/BackendCapabilityCatalogTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/HealthChecks/ProgramHealthCheckRegistrationTests.cs` (new)
- `tests/Hexalith.Memories.IntegrationTests/Health/HealthEndpointIntegrationTests.cs` (new; `[Trait("Category","Integration")]`)
- `docs/dev/health-checks.md` (new)

**Program.cs testability (Task 4.4):** If `ProgramHealthCheckRegistrationTests` cannot cleanly reach the `AddHealthChecks` chain via `WebApplicationFactory<Program>`, extract the registration block into a static helper method `Hexalith.Memories.Server.HealthChecks.HealthCheckRegistrations.RegisterAll(IServiceCollection, TimeSpan)` and call it from both `Program.cs` and the test. Do NOT extract into ServiceDefaults — the registrations reference `DaprClient` + `DaprStateStoreHealthCheck` which are Server-side concerns. The helper stays in `src/Hexalith.Memories.Server/HealthChecks/`. Decide at implementation time: if `WebApplicationFactory<Program>` + DI inspection works (it should; `HealthCheckService` is DI-resolvable), skip the extraction.

**One-class vs. three-class decision (mentioned in TL;DR #2):** The three backend checks (Task 1 + 2) share ~40% of their scaffolding (null-check, try/catch, result construction). **Decision:** keep them as three separate classes, NOT an abstract base. Rationale: (a) the existing `DaprSidecarHealthCheck` and `DaprStateStoreHealthCheck` are separate concrete classes — no base class — and match project convention; (b) abstractions for three short classes are speculative (CLAUDE-level guidance "three similar lines is better than a premature abstraction"); (c) each check's Redis-command specifics (`FT._LIST` vs `MODULE LIST` vs `GRAPH.LIST`) diverge enough that extraction would be lossy. If a fourth backend lands in Phase 2, revisit.

**Capability-catalog placement decision:** Task 5.2 moves the catalog to `ServiceDefaults/Health/` so the response writer can use it without the writer itself being in `Server/` (which would force `ServiceDefaults → Server` reference inversion). The catalog is a pure static class with string constants — no Redis/Dapr/NFalkorDB dependencies — so ServiceDefaults is the right home.

**Why a custom response writer instead of `AspNetCore.HealthChecks.UI.Client.UIResponseWriter`:** The community package ships a JSON writer with a fixed shape that does NOT include `affectedCapabilities` and uses PascalCase field names by convention. Adopting it would require a wrapper to inject the capability array and re-case the output — ending up with the same custom-writer complexity but with an extra dependency layer. The custom writer is ~40 LOC; the community package is a full dependency for marginal benefit. Trade-off accepted: we own the shape, we own the forward compatibility (additive via `schemaVersion`). Documented here so future readers don't re-ask.

**Why RediSearch and Redis Vector are two logical checks against one physical multiplexer:** both checks resolve the same keyed `IConnectionMultiplexer("redis")` (Redis Stack bundles both modules into one instance). The incremental cost of having two checks instead of one is ONE extra `MODULE LIST` roundtrip per probe — the multiplexer is already connected. In exchange, operators get axis-level granularity in `/ready` (FR72 requires per-capability reporting). If axis-level alerting is never used in practice, Phase 2 may collapse them into one `RedisStackHealthCheck`.

**Future evolution — unified backend-check descriptor registry (Phase 2+):** The current three-class design (Task 1 + 2) is deliberately duplicative to preserve convention with the existing `DaprSidecarHealthCheck` / `DaprStateStoreHealthCheck` pattern. The "three similar lines > premature abstraction" trade-off holds at three backends. **Refactor trigger:** when a fourth backend is proposed (Phase 2 axis additions, new storage engines, etc.), collapse the three classes into a single `BackendHealthCheck` driven by a descriptor registry — `record BackendDescriptor(string Name, Func<IServiceProvider, CancellationToken, Task<ProbeResult>> Probe, IReadOnlyList<string> Capabilities)`. Benefits the refactor unlocks: (a) catalog + registration single-source-of-truth (closes the Scenario C drift where a new backend is registered but has no capability entry); (b) adding backends = one descriptor entry, not three file creations; (c) testing tests the loop once + each probe function in isolation. Cost: diverges from the Dapr-check convention — but at four+ backends the Dapr pattern itself becomes the outlier. Document this evolution as an explicit follow-up in Phase 2 architecture notes; do NOT pre-factor in 8.1.

### Testing standards

- **Unit test conventions** (from `.editorconfig` + existing `Server.Tests` projects):
  - xUnit `[Fact]` / `[Theory]`; NSubstitute for mocking; Shouldly for assertions; **NOT** FluentAssertions (project standard per existing test files).
  - Test classes: `ClassNameTests`, methods: `MethodName_Scenario_Expected` (e.g., `CheckHealthAsync_WhenSidecarThrows_ShouldReturnFailureWithException`).
  - Arrange / Act / Assert comments present in existing tests; preserve the style.
  - Use the `CreateContext()` helper pattern from `DaprSidecarHealthCheckTests.cs:101-111` for `HealthCheckContext` fabrication.
- **Integration test conventions** (Story 5.6 pattern):
  - Apply `[Trait("Category","Integration")]`.
  - Use the existing Aspire fixture (`AspireIngestionPipelineFixture`) OR inherit from a new `AspireHealthFixture` if the ingestion fixture's warmup is too heavy. Check `tests/Hexalith.Memories.IntegrationTests/Fixtures/` for reuse.
  - `[Fact(Skip)]` with a clear reason string is acceptable when the Aspire fixture CS0311 build error blocks execution.
- **Test count target (informational — AC #9 is authoritative):** ~20+ new unit tests + 3 integration tests (integration possibly skipped). If a count below contradicts AC #9, update here — AC #9 wins. Distribution snapshot:
  - RediSearchHealthCheckTests: 5
  - RedisVectorHealthCheckTests: 6 (includes module-absent case)
  - FalkorDbHealthCheckTests: 4
  - BackendHealthResponseWriterTests: 6 (includes AOT-guard roundtrip)
  - BackendCapabilityCatalogTests: 1 (+ a `[Theory]` iterating check names if the dict grows)
  - ProgramHealthCheckRegistrationTests: 4
  - HealthEndpointIntegrationTests: 3 (possibly skipped)
  - ReadyEndpointAggregationTests: 3 (in-memory WebApplicationFactory)

### Story-shape template (reusable pattern for Epic 8)

This story's structure worked well for infrastructure-heavy work and should be the template for Stories 8.2 and 8.3. Reusable sections (in order):

1. **TL;DR** with four subsections: *What already exists (do NOT rebuild)*, *What 8.x adds*, *What does NOT ship*, *Primary risks*. The "What does NOT ship" list consistently prevented scope creep in 8.1 review and is the highest-leverage pattern.
2. **Risks → Guard tests table** (see above). Every risk with a named test pinning its mitigation. If a risk has no guard test, either add one or downgrade the risk to a Dev Note caveat.
3. **ACs with frozen JSON schemas**. When the story defines a contract (JSON, CLI args, config shape), include the authoritative example inline with a `schemaVersion` / version-pinning strategy.
4. **AC #9 = authoritative test inventory**. Any other test-count reference in the story is informational and may contradict AC #9 — AC #9 wins.
5. **Previous story intelligence** with an explicit merge-conflict protocol if the prior story is in-flight. Soft "sync with SM" language is not enough when two stories touch the same file region.
6. **Pre-flight verification** as a bash-executable checklist capturing assumptions (file paths, line numbers, package versions). Invalid assumption → stop and sync.
7. **Anti-patterns** as negative space (don't do X because Y). Complements the positive-space Tasks.
8. **Effort estimate** at the top, with breakdown by task category (code / docs / integration wiring).

Fragile patterns (known limitations of this template):

- **File:line citations drift.** Capture a git SHA + `captured on <date>` header for citation-heavy stories; re-verify at dev-start if the SHA has moved significantly.
- **Test-count enumeration tends to duplicate** (it did here). The AC #9 / Testing standards precedence rule mitigates but doesn't eliminate.
- **AC → Task traceability is unidirectional.** Task lists AC refs; no reverse index. For stories with >5 ACs and >5 Tasks, consider adding a reverse-lookup table.

### Anti-patterns to avoid

1. **Don't probe per-tenant state in health checks.** The endpoint is instance-scoped. `TenantMetricsService.GetIndexSizesAsync` takes a `tenantId` — do NOT call it from the checks. Use the instance-level probes (`FT._LIST`, `MODULE LIST`, `GRAPH.LIST`) documented in Tasks 1-2.
2. **Don't use `HealthStatus.Unhealthy` as the failure status for backend checks.** That breaks AC3 (Degraded is the intended aggregate when one backend is down). Only DAPR sidecar/statestore use Unhealthy.
3. **Don't introduce a new endpoint path.** Reuse `/alive` and `/ready` from ServiceDefaults. New paths fragment the health-check surface and break Story 7.5's trace-exclusion filter which lists the three paths explicitly.
4. **Don't add the response writer to `MemoriesJsonContext` as a typed contract.** The writer produces anonymous-object JSON intentionally — the shape is not a domain contract consumed by clients via `MemoriesJsonContext`, it's an operator-facing diagnostic output. Typed contracts go through `Contracts/V1/`; health responses stay in ServiceDefaults/Server only.
5. **Don't change the existing `ResultStatusCodes` map** at `Extensions.cs:97-102` (Healthy→200, Degraded→200, Unhealthy→503). That map is the hinge making AC3 possible. Changing it ripples into orchestrator behavior across all deployments.
6. **Don't add a `/metrics` Prometheus endpoint** in this story. Metrics flow via OTLP (Story 7.5). Health is a separate signal.
7. **Don't add `Retry-After` headers to 503 responses.** Orchestrators have their own probe cadence (`periodSeconds`); `Retry-After` would override/confuse it. Story 5.6's search-endpoint 503s use `Retry-After: 5` because those are request-flow semantics, not probe semantics.
8. **Don't re-instrument the health-check paths for OpenTelemetry.** Story 7.5 explicitly excludes the three paths from tracing; re-instrumenting flips that. If a dev accidentally adds an activity inside a health check, the trace-exclusion test fails.
9. **Don't probe FalkorDB with a tenant-scoped `MATCH (n) RETURN count(n)` query.** The check uses `GRAPH.LIST` (instance-level), not a per-tenant `MATCH`. A per-tenant query runs once per probe × per tenant — O(tenant-count) load on every readiness probe.
10. **Don't move the trace-exclusion filter.** `ServiceDefaults/Extensions.cs` lines 59-63 (the three `StartsWithSegments` predicates) stay byte-for-byte; the surrounding `WithTracing` lambda at line 56 also stays. 7.5 Task 2.2 already pinned this; 8.1 inherits the pin.
11. **Don't introduce a separate health-check project.** One project = one Health subfolder. Splitting creates import chains and multiple places to update when the catalog grows.
12. **Don't swallow health-check exceptions silently.** Every `catch` in the new checks produces a `HealthCheckResult` with `description` + `exception` populated. Silent catches hide root causes from operators reading the JSON response.
13. **Don't make the `BackendHealthResponseWriter` async-over-sync or block on `.Result`.** The writer is called from ASP.NET Core's middleware pipeline — it MUST be async-all-the-way (`await context.Response.WriteAsync(...)`).

### Git history context

Recent relevant commits (run `git log --oneline` to confirm ordering is unchanged):

- `958164b Add integration and unit tests for Quickstart CLI functionality` — Story 7.4 closing commits; no health-check impact.
- `948b8a5 feat: Add search endpoint degradation logging and response handling` — Story 5.6 merge; establishes the Degraded-is-not-Unhealthy pattern reused in 8.1 aggregation.
- `30f86c2 Add TenantEndpointHandlers for tenant configuration and listing endpoints` — Story 5.5; ships the `TenantMetricsService` probe patterns mirrored in Tasks 1-2.

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 8 overview (lines 1527-1530) and Story 8.1 ACs (lines 1531-1563)]
- [Source: _bmad-output/planning-artifacts/prd.md — FR72 (readiness/liveness checking all backends)]
- [Source: _bmad-output/planning-artifacts/architecture.md — lines 63 (Aspire health checks), 152 (sidecar + liveness), 437-454 (Aspire ServiceDefaults), 488 (ServiceDefaults gate-2 role)]
- [Source: _bmad-output/implementation-artifacts/5-5-tenant-configuration-and-listing.md — TenantMetricsService probe patterns]
- [Source: _bmad-output/implementation-artifacts/5-6-graceful-degradation-on-backend-failure.md — Degraded-vs-Unhealthy philosophy; deferred-integration-test pattern]
- [Source: _bmad-output/implementation-artifacts/7-5-search-and-access-telemetry.md — trace-exclusion invariant (AC #5), ServiceDefaults contract, EventId bank policy, regression test file `TelemetryHealthExclusionTests.cs` / method `Telemetry_HealthEndpointNotTraced` (Task 9.4)]
- [Source: src/Hexalith.Memories.ServiceDefaults/Extensions.cs:84-124 — existing health-check wiring + status-code map]
- [Source: src/Hexalith.Memories.Server/HealthChecks/DaprSidecarHealthCheck.cs — template for new check classes]
- [Source: src/Hexalith.Memories.Server/HealthChecks/DaprStateStoreHealthCheck.cs — template with primary-constructor + keyed-service use]
- [Source: src/Hexalith.Memories.Server/Program.cs:37-51 — AddHealthChecks chain to extend]
- [Source: src/Hexalith.Memories.Server/Program.cs:80-83 — keyed IConnectionMultiplexer registrations to consume]
- [Source: src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:161-248 — Redis and FalkorDB probe exception-handling pattern to mirror]
- [Source: src/Hexalith.Memories.Contracts/V1/IndexHealth.cs — data-state vs. availability-state enum]
- [Source: src/Hexalith.Memories.AppHost/Program.cs:29-79 — container + sidecar topology; informs the probe targets]
- [Source: tests/Hexalith.Memories.Server.Tests/HealthChecks/DaprSidecarHealthCheckTests.cs — new-test-class template (NSubstitute + Shouldly + `CreateContext` helper)]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
