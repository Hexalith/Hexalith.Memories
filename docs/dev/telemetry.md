# Hexalith.Memories — Observability & Telemetry (Story 7.5)

This document describes the observability contract shipped by Story 7.5: structured JSON logging with
OpenTelemetry correlation (NFR27), distributed trace propagation CLI → Server → backends (NFR28),
per-tenant custom metrics (NFR29), and per-tenant audit events for every search + access operation
(FR67).

> **Compliance disclaimer (PRD line 449).** Memories is open-source software; security / compliance
> certifications (SOC 2, GDPR Article 30, HIPAA) apply to the **deploying organization**, not the
> artifact. This story provides the **evidence plumbing** — correlation IDs, audit events with a
> fixed schema, trace propagation — which operators map to their compliance controls.

---

## What you need to see these

| Environment            | Tool                  | Endpoint / config                                              |
| ---------------------- | --------------------- | -------------------------------------------------------------- |
| Local dev (Aspire)     | Aspire Dashboard      | `http://localhost:18888` (UI), `http://localhost:18889` (OTLP) |
| Production — Jaeger    | Jaeger OTLP collector | `OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317`               |
| Production — Tempo     | Grafana Tempo         | `OTEL_EXPORTER_OTLP_ENDPOINT=http://tempo:4317`                |
| Production — Datadog   | Datadog Agent         | `OTEL_EXPORTER_OTLP_ENDPOINT=http://dd-agent:4318`             |
| Production — Honeycomb | Honeycomb ingest      | `OTEL_EXPORTER_OTLP_ENDPOINT=https://api.honeycomb.io:443`     |

CLI-side telemetry is **opt-in** — set `HEXALITH_MEMORIES_OTEL_ENDPOINT` or pass `--telemetry` to any
`memories` subcommand to export CLI spans.

---

## ActivitySource names

Memories pins a single source name: **`Hexalith.Memories`** (declared by
`Hexalith.Memories.Telemetry.MemoriesActivitySource.SourceName`). Future submodule sources follow
the convention `Hexalith.Memories.<subsystem>` (e.g., `Hexalith.Memories.Mcp` for the Phase 1.5 MCP
server). Do NOT create per-request sources.

### Activity names

| Activity               | Emitted by                                 | Tags                                                                |
| ---------------------- | ------------------------------------------ | ------------------------------------------------------------------- |
| `memories.search`      | `GET /api/search`                          | `memories.tenant_id`, `memories.operation`, `memories.axis`         |
| `memories.ingest`      | `POST /api/ingest` (+`/url`, `/directory`) | `memories.tenant_id`, `memories.operation`, `memories.source_type`  |
| `memories.traverse`    | `GET /api/tenants/{id}/traverse`           | `memories.tenant_id`, `memories.operation`                          |
| `memories.case-access` | `GET /api/.../memory-units/{id}`           | `memories.tenant_id`, `memories.case_id`, `memories.memory_unit_id` |
| `memories.cli.invoke`  | CLI root span (opt-in)                     | `memories.command`                                                  |

---

## Meter name + metric catalog

Single meter: **`Hexalith.Memories`** (declared by `MemoriesMeter.Name`).

| Metric                                                  | Type                        | Unit             | Tag keys                    | Description                                                  |
| ------------------------------------------------------- | --------------------------- | ---------------- | --------------------------- | ------------------------------------------------------------ |
| `memories.ingestion.documents`                          | Counter&lt;long&gt;         | `{documents}`    | `tenant_id`                 | Total documents ingested successfully                        |
| `memories.ingestion.failures`                           | Counter&lt;long&gt;         | `{documents}`    | `tenant_id`, `error_code`   | Total ingestion scheduling failures                          |
| `memories.search.requests`                              | Counter&lt;long&gt;         | `{requests}`     | `tenant_id`, `axis`         | Total search requests per resolved axis                      |
| `memories.search.duration`                              | Histogram&lt;double&gt;     | `ms`             | `tenant_id`, `axis`         | Request latency for the resolved axis                        |
| `memories.rate.limit.rejections`                        | Counter&lt;long&gt;         | `{requests}`     | `tenant_id`, `error_code`   | Inbound request rate-limit rejections                        |
| `memories.index.size`                                   | ObservableGauge&lt;long&gt; | `{documents}`    | `tenant_id`, `axis`         | Per-tenant per-axis index size                               |
| `memories.pipeline.queue.depth`                         | ObservableGauge&lt;int&gt;  | `{items}`        | `tenant_id`                 | Per-tenant ingestion queue depth                             |
| `memories.natural.language.description.duration`        | Histogram&lt;double&gt;     | `ms`             | `tenant_id`                 | Natural-language description latency                         |
| `memories.natural.language.embedding.queue.depth`       | ObservableGauge&lt;long&gt; | `{items}`        | `tenant_id`                 | Natural-language embedding retry queue depth                 |
| `memories.natural.language.embedding.queue.bytes`       | ObservableGauge&lt;long&gt; | `By`             | `tenant_id`                 | Natural-language embedding retry queue size in bytes         |
| `memories.embedding.api.calls`                          | Counter&lt;long&gt;         | `{calls}`        | `tenant_id`, `content_kind` | Embedding provider calls by payload/description content kind |
| `memories.conversation.cache.hits`                      | Counter&lt;long&gt;         | `{calls}`        | `tenant_id`, `cache_status` | Reserved Dapr Conversation cache hit/miss surface            |
| `memories.handlers.registered`                          | ObservableGauge&lt;int&gt;  | `{handlers}`     | `tenant_id`                 | Per-tenant count of registered event handlers                |
| `memories.handlers.mismatches`                          | Counter&lt;long&gt;         | `{mismatches}`   | `tenant_id`, `severity`     | Detected event-handler mismatches                            |
| `memories.handlers.observations.dropped`                | Counter&lt;long&gt;         | `{observations}` | `reason`                    | Dropped observation-store writes                             |

### Tag-key policy

`case_id`, `user`, `memory_unit_id` are **NEVER** metric tag keys — Risk #1 cardinality mitigation.
Case id goes in the audit log (free-form, bounded by request rate); user identity likewise.

Adding a new tag key requires:

1. Updating `MemoriesMeter.MetricTagKeyPolicy` constant.
2. Updating `MemoriesMetricsTests.AllRegisteredMetricsHaveExpectedTagKeys`.
3. Updating this document.

The `memories.ingestion.failures` and `memories.search.requests` counters may carry the synthetic tag
`tenant_id = "__rejected__"` when the tenant guard rejected the request — bounded cardinality,
discoverable by operators, no attacker amplification.

---

## Audit event schema (FR67)

Every audited search, ingest, traverse, case-access, delete, tenant-lifecycle, tenant-config,
case-member, and annotation operation emits **exactly one** `AccessTelemetryEvent` via
`[LoggerMessage]` source-generated emitters. Dedicated logger category:
`Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory`.

EventId bank: **7500-7599** (success 7501-7509, error 7511-7519).

```jsonc
{
    "schemaVersion": 1, // bump only on breaking schema changes
    "eventId": 7501, // 7500-7599 bank
    "timestamp": "2026-04-17T12:34:56.789Z",
    "tenantId": "acme", // or "__rejected__" if guard rejected
    "operationType": "search", // search | ingest | traverse | case-access | delete | tenant-lifecycle | tenant-config | case-member | annotation
    "caseId": null,
    "user": "anonymous", // ADR-7.5-004 resolution rules
    "queryParams": { "query": "...", "axis": "hybrid", "maxResults": 10 },
    "resultCount": 3, // null for write/schedule operations
    "durationMs": 42,
    "outcome": "ok", // ok | partial | error
    "errorCode": null,
    "traceId": "<W3C trace id>",
    "spanId": "<W3C span id>",
}
```

### Versioning policy

- **Additive** field additions keep `schemaVersion = 1`. Consumers MUST ignore unknown fields.
- **Breaking** changes (rename, removal, type change) bump `schemaVersion` to `2` — AND the test
  `AccessTelemetryEventSchemaTests.V1_FieldNames_AreFrozen` guards against silent renames.

### User-identity resolution (ADR-7.5-004)

1. **Server API paths** — principal-derived identity from authenticated claims. Preferred sources are
   stable subject/name claims; spoofable `x-user-id` headers and request-body attribution fields do
   not supply the audit identity.
2. **Fallback** — if an authenticated principal has no usable identity claim, the audit user is
   `"anonymous"`.
3. **Wizard-originated operations** — any wizard tagging must come from trusted principal context or
   external operator logging; the Server no longer trusts `x-user-id=quickstart-wizard`.

---

## Audit log routing recipe

Search queries may contain privacy-sensitive terms on regulated tenants. The dedicated
`Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory` category lets operators route audit
events to a separate sink, distinct from operational logs consumed by sysadmins for general
deployment troubleshooting.

Example `appsettings.Production.json`:

```jsonc
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory": "Information",
        },
        "Console": {
            "LogLevel": {
                "Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory": "None",
            },
        },
        // Direct the audit category to a dedicated JSON file sink via your log pipeline of choice
        // (Serilog, Seq, filebeat, fluentd). The structured JSON produced by AddJsonConsole is the
        // canonical form; route it per your org policy.
    },
}
```

### Log-level config gate

For high-traffic tenants, operators throttle to `Warning` to keep error events (7511-7515) flowing
while suppressing successful-operation events (7501-7505). **Trade-off:** losing `ok`-outcome audit
trail for successes. Regulated tenants MUST keep the default (`Information`) and scale the log
pipeline instead.

---

## Trace propagation

- **CLI** is opt-in (env var or `--telemetry` flag).
- **Server** is always-on: DAPR sidecars auto-inject W3C TraceContext; the `/health`, `/alive`,
  `/ready` filters remain byte-for-byte preserved (quickstart's 1s polling would otherwise flood the
  collector).
- **AddHttpClientInstrumentation** is registered in both Server and (opt-in) CLI.

### What happens when OTLP is down

The OTLP exporter is **non-blocking** — endpoint unavailability (connection refused, DNS failure,
HTTP 5xx) silently drops spans. CLI / Server user-visible behavior is unchanged. If you enable
telemetry but see no traces in the collector, check collector health first; do NOT suspect
Memories-side retry / buffering logic (there is none by design — the collector is responsible for
ingest resilience).

---

## Cardinality risk + operator mitigation

Per-tenant tags × per-axis sub-tags × per-case possible tagging = O(tenants × axes × cases) unique
time series. MVP keeps tagging to the bounded keys listed in `MemoriesMeter.MetricTagKeyPolicy`
(Risk #1). Hard cardinality caps are deferred to Phase 2.

If tenant count grows beyond ~100:

1. **Drop high-cardinality dimensions at the OTLP collector.** Example (OpenTelemetry Collector
   `processors/filter`):

    ```yaml
    processors:
        filter/drop_tenant_axis_metrics:
            metrics:
                include:
                    match_type: regexp
                    metric_names: ["memories\\.search\\..*"]
                exclude:
                    resource_attributes:
                        - key: service.name
                          value: hexalith.memories
    ```

2. **Sample traces aggressively.** Per-activity sampling policies at the collector.
3. **Move to a pull-based metrics pipeline** (Prometheus) with recording rules that aggregate away
   tenant-level detail for rollups.

---

## Example Prometheus queries

Prometheus query names use collector-normalized underscores derived from the dot-separated
instrument names above.

```promql
# Tenant ingestion throughput (last 5m, per tenant)
sum by (tenant_id) (rate(memories_ingestion_documents_total[5m]))

# Per-axis p99 search latency (last 5m)
histogram_quantile(0.99, sum by (tenant_id, axis, le) (rate(memories_search_duration_milliseconds_bucket[5m])))

# Hot tenants with elevated ingestion failure rate
sum by (tenant_id) (rate(memories_ingestion_failures_total{error_code!=""}[5m]))
  / sum by (tenant_id) (rate(memories_ingestion_documents_total[5m]))

# Queue depth alert (>10 for any tenant for 5m)
max_over_time(memories_pipeline_queue_depth[5m]) > 10
```

The committed starter Grafana dashboard is
[`../../deploy/grafana/dashboards/memories-operability.json`](../../deploy/grafana/dashboards/memories-operability.json).
Prometheus query names in that dashboard use collector-normalized underscores derived from the
dot-separated instrument names above.

---

## Audit log volume estimates

Back-of-envelope (MVP assumptions):

| Parameter               | Value                                         |
| ----------------------- | --------------------------------------------- |
| Event size              | ~300 bytes (compact JSON)                     |
| Search rate per tenant  | 10 req/s (high end)                           |
| Active tenants per node | 10                                            |
| Nodes                   | 1 (single-node dev); scales with tenant count |

Per-node volume: 10 × 10 × 300 B = **30 KB/s** audit log throughput ≈ 2.5 GB/day per node.
`AddJsonConsole` is async in the default `ILogger` pipeline — does not block the request path. Log
pipeline throughput is the operator's responsibility; Memories does NOT throttle.

---

## CLI telemetry envelope (status telemetry)

```bash
# Human (default)
memories status telemetry --tenant acme

# JSON envelope (ADR-7.2-001)
memories status telemetry --tenant acme --format json
```

JSON shape:

```json
{
    "schemaVersion": 1,
    "command": "status telemetry",
    "data": {
        "tenantId": "acme",
        "asOf": "2026-04-17T...",
        "indexSizes": {
            "rediSearchKeyCount": 1234,
            "redisVectorKeyCount": 1230,
            "falkorDbNodeCount": 5000
        },
        "indexHealth": {
            "rediSearch": "Ready",
            "redisVector": "Ready",
            "falkorDb": "Ready"
        },
        "searchMetrics": {
            "syntactic": { "requestsLast5m": 120, "errorsLast5m": 0 },
            "semantic": { "requestsLast5m": 45, "errorsLast5m": 0 },
            "graph": { "requestsLast5m": 10, "errorsLast5m": 0 },
            "hybrid": { "requestsLast5m": 30, "errorsLast5m": 0 }
        },
        "ingestionMetrics": {
            "documentsLast5m": 40,
            "failuresLast5m": 0,
            "queueDepth": 0
        }
    }
}
```

The endpoint is an **operator-facing read-only poke** (ADR-7.5-003). Aspire Dashboard + OTLP
collector remain the source of truth for time-series + alerting. Latency percentiles (p50/p99) are
NOT returned — query the OTLP collector's aggregation over the `memories.search.duration` histogram.

---

## Phase 1.5 coverage gaps

NFR28 is validated in 7.5 for:

- CLI → Server (Tier-2 `WebApplicationFactory` + `AddInMemoryExporter`).
- CLI → Server → Redis (Tier-3 Aspire fixture, requires Docker).

Story 24.1 extends the trace contract across the durable ingestion workflow boundary. Ingestion
scheduling captures W3C `traceparent` and optional `tracestate` into `IngestionInput.TraceContext`
before Dapr Workflow scheduling. Workflow orchestration only passes that serialized value; it must
not read `Activity.Current` or start spans because replay may re-execute orchestration code. Workflow
activities inherit the linked-span base class, which emits `memories.workflow.activity` spans with an
`ActivityLink` to the original request context when the serialized trace context is valid.

**Not covered** (yet):

- **MCP Server → Memories Server trace propagation** (via DAPR service invocation). MCP does not
  exist as of 7.5. The Epic 10 story introducing `Hexalith.Memories.Mcp` MUST include an end-to-end
  trace test covering the MCP → Server DAPR hop as part of its Definition of Done. Flag this
  dependency in Epic 10's References section.

---

## End-to-end trace verification (Story 8.4 — Tier-3 Aspire integration tests)

Story 8.4 closes Story 7.5 Tasks 11.3 + 11.4 with two `[Trait("Category", "Integration")]` test
classes that prove the NFR28 HTTP-hop gate (W3C `traceparent` propagation across CLI → Server) and
the FR67 authoritative gate (audit events surface on the Server container's stdout) on the deployed
stack.

### Tier split

| Tier                             | Runs on                    | Gates                                                                                                                                                                                                                                                                              | Story                       |
| -------------------------------- | -------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------- |
| Tier-1 (unit)                    | Every PR                   | Substrate correctness — `MemoriesActivitySource`, `MemoriesMeter`, `EndpointTelemetryScope`, `RollingCounterStore`, `AccessTelemetryLog`, `OpenTelemetryRegistrationTests` (incl. Story 8.4's env-var-branch happy path + Risk 7 parse-strictness theory + AC #5 regression guard) | 7.5 Tasks 8-10 + 8.4 Task 1 |
| Tier-2 (`WebApplicationFactory`) | Every PR                   | In-process trace + audit invariants — `TracePropagationNoDockerTests`, `AuditLogStreamTests`, `TelemetrySummaryEndpointTests`, `TelemetryHealthExclusionTests`                                                                                                                     | 7.5 Tasks 11.1, 11.2        |
| Tier-3 (Aspire fixture, Docker)  | Merge-queue / nightly lane | End-to-end NFR28 HTTP-hop gate + FR67 audit-stream gate against the deployed stack                                                                                                                                                                                                 | **8.4 (this story)**        |

### How to run locally

Tier-3 tests need Docker running (the Aspire fixture spins up Redis + FalkorDB + the DAPR sidecar +
the Memories Server). With Docker available:

```bash
# Tier-3 only (Aspire telemetry tests + the rest of the Aspire-backed integration suite)
dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj \
    --filter "Category=Integration"

# Just the new Story 8.4 telemetry classes (faster iteration)
dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj \
    --filter "FullyQualifiedName~AspireEndToEndTraceTests|FullyQualifiedName~AuditLogStreamIntegrationTests"
```

The fixture cold-boot is ~30–60s; once boot completes, both 8.4 test classes share the same Aspire
environment via `[Collection("AspireIngestionPipeline")]` so the boot cost is amortized across all
methods. For per-PR runs, exclude the Tier-3 lane: `--filter "Category!=Integration"`.

### What each captured signal proves

- **CLI in-memory span collector (`InMemorySpanCollector`)** — captures the CLI root span
  (`memories.cli.invoke`) and the outbound `System.Net.Http.HttpRequestOut` span. Proves the CLI
  side of the W3C trace context (root TraceId + HttpClient instrumentation injecting `traceparent`).
- **Server activity breadcrumb (`ServerActivityStreamReader`)** — parses the test-only
  `__hexalith_activity__{...}` stderr breadcrumbs emitted for relevant server spans when
  `HEXALITH_MEMORIES_TELEMETRY_INMEMORY=1`. Proves the deployed server process created the
  AspNetCore inbound span and the `memories.search` activity for the same trace the CLI started.
- **Redis client span (via the same `ServerActivityStreamReader`)** — the Story 8.5 Redis OTEL
  instrumentation emits spans on the `OpenTelemetry.Instrumentation.StackExchangeRedis` source
  for every RediSearch / Redis Vector / FalkorDB-protocol command. The breadcrumb filter at
  `IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb` accepts these spans only when the
  parent chain reaches a Memories or AspNetCore ancestor (housekeeping PINGs are silently
  dropped; a DEBUG log entry records the drop reason for triage). Proves per-backend span
  attribution end-to-end: operators can bucket search latency into BM25 (RediSearch) vs vector
  (Redis Vector) vs graph (FalkorDB) lanes in Grafana / Datadog / the Aspire dashboard instead
  of guessing from ad-hoc post-hoc logging. Story 8.4 AC #2 is a hard assertion via this signal
  from Story 8.5 forward.
- **Server stdout audit log line (`AuditEventStreamReader`)** — parses Aspire-captured Server
  stdout JSON and extracts the `AccessTelemetryEvent` ToString payload (AddJsonConsole does not
  natively destructure record-typed `{@AuditEvent}` placeholders to JSON, so the reader extracts
  fields from the formatted message via regex). Proves FR67 — the Server's audit emission survives
  the deployed pipeline and reaches stdout where an operator's SIEM / log aggregator can consume it.
- **Trace/span cross-reference** — the Server-side `memories.search` activity and the correlated
  audit event MUST share the same `TraceId` and `SpanId`. This is the W3C HTTP-hop propagation +
  correlation invariant (NFR28 HTTP-hop gate per Story 8.4 AC #8 — the DAPR service-invocation hop
  is deferred to Epic 10's MCP story).

### Failure interpretation cheatsheet

- **Tier-1 (unit) failure** → substrate regression (activity source rename, meter rename, EventId
  bank drift, env-var contract loosening). Fix at the substrate; never make Tier-3 paper over it.
- **Tier-2 (`WebApplicationFactory`) failure** → in-process trace or audit emission regression
  (e.g. `EndpointTelemetryScope` dispose order, validation-fail audit emission missing, audit log
  category mis-spelled). Fix at the endpoint wrapper.
- **Tier-3 `AspireEndToEndTraceTests` failure** → either OTLP / HttpClient instrumentation broke
  on the CLI side (no in-memory spans captured), OR the Server stopped propagating the W3C
  traceparent header (the captured server-side AspNetCore / `memories.search` breadcrumbs carry a
  different TraceId), OR Aspire stopped surfacing Server stderr/stdout into the AppHost log
  pipeline (no activity breadcrumbs or audit lines arrive within the configured timeout — see
  `TELEMETRY_E2E_STDOUT_TIMEOUT_SECONDS`).
- **Tier-3 `AuditLogStreamIntegrationTests` failure** → audit-event emission broke at the deployed
  stack (Server's `AddJsonConsole` mis-configured, the `[LoggerMessage]` source-generated emitter
  removed, EventId outside the 7501-7599 bank, or the `Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory`
  category renamed without updating `AuditEventStreamReader`'s suffix filter).

### CI configuration knobs

- `TELEMETRY_E2E_STDOUT_TIMEOUT_SECONDS` (default `10`) — overrides the per-test polling timeout
  for `AuditEventStreamReader.ReadAsync`. Bump on slow merge-queue runners; lower for local
  iteration.
- `HEXALITH_MEMORIES_TELEMETRY_INMEMORY=1` — activates the env-var branch in
  `ServiceDefaults.AddOpenTelemetryExporters` that appends a `CollectingActivityProcessor`
  resolving an optional `IActivityCollector` from DI and emits server-side activity breadcrumbs for
  the telemetry integration tests. Production deployments leave this unset; AC #5 regression guard
  test pins that "unset" means zero extra in-memory/test-only telemetry processors.

---

## Verify Redis spans locally (Story 8.5)

Without spinning up the full Tier-3 suite, a developer can sanity-check Redis OTEL instrumentation
against a running Aspire stack in five steps:

1. **Boot the Aspire stack** — `dotnet run --project src/Hexalith.Memories.AppHost`. The AppHost
   prints the Aspire dashboard URL on startup (typically `http://localhost:18888`).
2. **Trigger a Redis-path request** — in a separate terminal:

    ```bash
    dotnet run --project src/Hexalith.Memories.Cli -- search query \
        --tenant acme \
        --query canary \
        --axis syntactic
    ```

    (Or hit the REST endpoint directly: `curl http://localhost:5000/api/search?tenant=acme&query=canary&axis=syntactic`.)

3. **Open the Aspire dashboard Traces view** at the URL printed on boot, pick the most recent
   trace, and expand the child spans. You should see at least one span per backend Redis call.
4. **Expected span shape** (text-diffable YAML):

    ```yaml
    Source.Name: OpenTelemetry.Instrumentation.StackExchangeRedis
    OperationName: OpenTelemetry.Instrumentation.StackExchangeRedis.Execute
    tags:
        db.system: redis # FalkorDB spans rewrite to "falkordb" via FalkorDbSemanticAttributeProcessor
        db.system.name: redis # (same rewrite applies)
        db.redis.database_index: 0
    TraceId: <same as parent HTTP-in span>
    parent.chain: memories.cli.invoke → System.Net.Http → Microsoft.AspNetCore → memories.search → (this Redis span)
    ```

5. **Troubleshooting — "No Redis spans appear"**:
    - Check `AddServiceDefaults(...)` is invoked by the host (`Program.cs`).
    - Check BOTH keyed `IConnectionMultiplexer` registrations exist at startup (`redis` +
      `falkordb` in `src/Hexalith.Memories.Server/Program.cs`).
    - Check the DI-guard didn't throw at startup — scan the server stdout for
      `"Keyed IConnectionMultiplexer"` (the eager-fail message from
      `Extensions.AddRedisKeyedConnectionGuard`).
    - If the server boots clean but spans are still missing, check the breadcrumb filter's DEBUG
      log output (`IntegrationActivityProcessor` emits a `redis breadcrumb dropped: {reason} ...`
      line when a Redis activity's parent chain doesn't reach a Memories or AspNetCore ancestor).

---

## Instrumentation Inventory (Story 8.5 Task 4.3)

Canonical code ↔ doc parity table for every OpenTelemetry `ActivitySource` the Memories Server
subscribes to. A Tier-2 test (`InstrumentationInventoryTests`) parses this table on every build
and asserts each `ActivitySource.Name` resolves `ActivitySamplingResult.AllData` on the built
`TracerProvider` — the next instrumentation gap fails a unit test instead of a Tier-3 assertion
six months after ship.

| ActivitySource.Name                                       | Registration site (`file:line`)                                                             | Coverage                                                             | Tier-2 registration test                                                                        |
| :-------------------------------------------------------- | :------------------------------------------------------------------------------------------ | :------------------------------------------------------------------- | :---------------------------------------------------------------------------------------------- |
| `Microsoft.AspNetCore`                                    | `ServiceDefaults/Extensions.cs` `AddAspNetCoreInstrumentation`                              | Inbound HTTP request spans on the Memories Server                    | `OpenTelemetryRegistrationTests.ShouldTraceHttpRequest_IncludesApplicationEndpoints`            |
| `System.Net.Http`                                         | `ServiceDefaults/Extensions.cs` `AddHttpClientInstrumentation`                              | Outbound HttpClient spans (CLI → Server + any Server-side outbound)  | `TracePropagationNoDockerTests` (Tier-2 WAF)                                                    |
| `Hexalith.Memories` (`MemoriesActivitySource.SourceName`) | `ServiceDefaults/Extensions.cs` `AddSource(MemoriesActivitySource.SourceName)`              | Application-level spans (`memories.search`, `memories.ingest`, etc.) | `OpenTelemetryRegistrationTests.ConfigureOpenTelemetry_RegistersMemoriesActivitySource_Runtime` |
| `Dapr.Workflow`                                           | `ServiceDefaults/Extensions.cs` `AddSource(Extensions.DaprWorkflowActivitySourceName)`      | Dapr workflow engine spans for workflow scheduling/activity execution | `OpenTelemetryRegistrationTests.ConfigureOpenTelemetry_RegistersDaprWorkflowActivitySource_Runtime` |
| `OpenTelemetry.Instrumentation.StackExchangeRedis`        | `ServiceDefaults/Extensions.cs` `AddRedisInstrumentation` + `ConfigureRedisInstrumentation` | RediSearch + Redis Vector + FalkorDB-protocol command spans          | `RedisInstrumentationRegistrationTests.TracerRegistration_IncludesRedisInstrumentationSource`   |
| `<Environment.ApplicationName>` (default source)          | `ServiceDefaults/Extensions.cs` `AddSource(builder.Environment.ApplicationName)`            | Default application-named spans for opt-in emitters                  | `OpenTelemetryRegistrationTests.AddServiceDefaults_ProducesBuildableContainer`                  |

Adding a new instrumentation:

1. Register the `ActivitySource` (or call the instrumentation package's `AddXxxInstrumentation()`
   extension) inside `ConfigureOpenTelemetry`'s `WithTracing` lambda.
2. Add a row to the table above.
3. Add a Tier-2 registration test whose name matches the row's last column.
4. Run `InstrumentationInventoryTests` — the parity check must stay green.

The parity test tolerates minor whitespace changes as long as column names stay stable; see the
test source for the exact regex used to parse the table.

---

## ADR-8.4-001 — CI lane split (Tier-2 per-PR vs Tier-3 merge-queue)

**Status:** Accepted (2026-04-20). **Decision:** Story 8.4's two `[Trait("Category", "Integration")]`
test classes run on the **merge-queue / release lane**, NOT the per-PR lane. **Rationale:** running
two Tier-3 classes on every PR roughly doubles CI minutes (Aspire cold-boot is ~30–60s; the existing
~40 Aspire-backed integration tests already incur this cost on the merge-queue lane only, per the
existing `Category=Integration` filter convention from Stories 7.1–7.4). The Tier-2 variants
(`TracePropagationNoDockerTests`, `AuditLogStreamTests`, `TelemetrySummaryEndpointTests`) gate every
PR and cover ~80% of what can break without Docker; Tier-3 gates release promotion.

**Consequences:** A regression in the deployed-stack-only path (e.g. AddJsonConsole behavior under
Aspire orchestration, container stdout buffering) lands in main between merge-queue runs and the
PR author who introduced it doesn't see immediate CI feedback. **Mitigation:** the Tier-2 coverage
catches the substrate-level regressions; the Tier-3 lane is a **required blocking check** on the
merge-queue (Task 4.4 — see Epic 11 CI/CD when it lands). Until Epic 11's merge-queue workflow ships,
a `nightly.yml` bridge workflow MUST run Tier-3 once per day (DoD item 8 from Story 8.4); the
nightly bridge is itself the blocking gate when Epic 11 hasn't landed.

**Note (2026-04-21):** Epic 11 (CI/CD pipeline) is still in `backlog`, so the merge-queue workflow +
protected-branch required-check configuration have not landed yet. The bridge is now committed as
`.github/workflows/nightly.yml`, which runs `bash ./tools/test.sh --filter "Category=Integration"`
on a nightly schedule and via manual dispatch. When Epic 11 lands, update this ADR with the exact
merge-queue workflow filename + required-check rule and either retire or repurpose the nightly bridge.

---

## ADR-8.4-002 — Test-only `InMemorySpanCollector` placement (option B)

**Status:** Accepted (2026-04-20, Winston party-mode review). **Decision:** the
`InMemorySpanCollector` implementation lives at
`tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/InMemorySpanCollector.cs` (NOT
in `src/Hexalith.Memories.Telemetry/`). The interface `IActivityCollector` lives in the production
`Hexalith.Memories.Telemetry` assembly so `ServiceDefaults.AddOpenTelemetryExporters` can resolve it
from DI without taking a test-only dependency, but the concrete sink is test-only.

**Rationale:** a production-visible static mutable activity collector in a consumer-facing package
is a foot-gun for plugin authors across the Hexalith ecosystem, even when gated with
`[Experimental("HXL008")]` + `[EditorBrowsable(Never)]`. Test-only placement keeps the blast radius
bounded.

**Addendum (Story 8.4 implementation):** the Tier-3 `AspireIngestionPipelineFixture` runs the
Memories Server resource as a **separate process** (Aspire 13's default project-resource execution
model). Cross-process DI sharing of `IActivityCollector` is therefore architecturally infeasible —
the original Task 1.1.6 "AsyncLocal vs static accessor" pivot does not apply. The Tier-3 tests use
a **hybrid capture model**: CLI-side spans captured in-test-process via `IActivityCollector`;
Server-side activity evidence proxied through the audit log's `TraceId`/`SpanId` fields (which
`AccessTelemetryLog.CreateEvent` populates from `Activity.Current` on the Server side at audit
emission time). The Task 1.1 production wiring still ships; it's used CLI-side today and is ready
for any future in-process Server hosting variant.

---

## ADR-8.4-003 — Audit-event capture path per test

**Status:** Accepted (2026-04-20, Rev 0.5 elicitation pass) — implemented per the cross-process
adaptation. **Decision:** the audit event capture path is uniform under the cross-process model:
the Server's stdout JSON line is the single source of truth for both `AspireEndToEndTraceTests`
(AC #4 cross-reference between activity and audit event) and `AuditLogStreamIntegrationTests`
(AC #3 FR67 gate). The reader (`AuditEventStreamReader`) parses Aspire-captured stdout lines and
extracts `AccessTelemetryEvent` fields from the C# record's `ToString()` output (AddJsonConsole
doesn't natively destructure record-typed `{@AuditEvent}` placeholders to JSON in
`Microsoft.Extensions.Logging` versions current at story merge time).

**Why one path, not two:** the original Rev 0.5 design split capture into "in-memory log exporter
(in-process, deterministic)" for AC #4 vs "container stdout via Aspire log stream" for AC #3. With
the Server running as a separate process, the in-memory log exporter cannot capture Server log
records — they emit in the Server's process and are inaccessible from the test. So both ACs go
through the stdout reader. The reader is fast enough (Tier-3 audit tests run in 200-300ms each)
that the original "use stdout for the slow case, in-memory for the fast case" optimization does
not apply.

**Future re-evaluation:** if Aspire / .NET ships an in-process project-resource execution mode (or
Microsoft.Extensions.Logging adds native record-destructuring to AddJsonConsole), revisit this ADR
and consider re-splitting the capture path per the original Rev 0.5 framing.

---

## ADR-8.5-001 — Redis OTEL instrumentation package + registration shape

**Status:** Accepted (2026-04-23, Story 8.5). **Decision:** register
`OpenTelemetry.Instrumentation.StackExchangeRedis 1.15.1-beta.1` on the Memories Server's tracer
pipeline via the shared `AddServiceDefaults()` / `ConfigureOpenTelemetry()` path using one
`AddRedisInstrumentation(ConfigureRedisInstrumentation)` call plus a post-build
`ConfigureRedisInstrumentation((sp, instrumentation) => ...)` callback that attaches both keyed
`IConnectionMultiplexer` instances (`"redis"` + `"falkordb"`). Each key is fronted by a
DI-keyed-service guard that throws `InvalidOperationException` at `TracerProvider.Build()` when
the expected keyed multiplexer is absent. Ships Story 8.4 AC #2 as a hard end-to-end assertion.

### (a) Package choice

**Picked:** `OpenTelemetry.Instrumentation.StackExchangeRedis` (official OpenTelemetry contrib
package; targets `net8.0` + `net10.0`; requires `StackExchange.Redis >= 2.6.122`, satisfied by
Hexalith's pinned `2.12.4`).

**Rejected:** `StackExchange.Redis.Extensions.OpenTelemetry`. Rationale: tracks the
`StackExchange.Redis.Extensions` higher-level wrapper library — which Hexalith does **not** use —
so adopting it would drag an unused dependency surface into the Server composition for no
instrumentation benefit.

### (b) Prerelease tag + supply-chain mitigation

Version `1.15.1-beta.1` is the latest release as of 2026-04-21. We explicitly accept the `-beta.N`
tag because the upstream GA window for the 1.15 series has not opened yet, and deferring Story 8.5
until GA would let Story 8.4's AC #2 soft-skip rot indefinitely.

Supply-chain mitigations:

1. **Exact version pin** in `Directory.Packages.props` (no floating `*`, no `[1.15, 2.0)` range).
2. **`NuGet.config` with `packageSourceMapping`** at the repo root. Pins all `OpenTelemetry.*`
   packages to the official `nuget.org` source. During the prerelease window, a typosquat-adjacent
   attacker uploading a malicious `OpenTelemetry.Instrumentation.StackExchangeRedis.Extensions`
   (note the trailing `.Extensions`) to a secondary feed would otherwise resolve before the
   legitimate package if the feed were ever added. Source-mapping forecloses that route.
3. **`NuGet.config` also sets `signatureValidationMode=require` and trusts the `nuget.org`
   repository signer** via the current official repository-certificate fingerprints published in
   the NuGet docs. That closes the unsigned / unknown-signer gap the prerelease window would
   otherwise leave open. When nuget.org rotates repository certificates, refresh the
   `trustedSigners` block from the official docs (or sync it with
   `nuget trusted-signers sync -Name nuget.org`) before the next restore.

### (c) ActivitySource + OperationName pins

- **`ActivitySource.Name`** (verified against upstream
  `StackExchangeRedisConnectionInstrumentation.cs` — `Assembly.GetName().Name`):
  `"OpenTelemetry.Instrumentation.StackExchangeRedis"`.
- **Span `OperationName`**: `"OpenTelemetry.Instrumentation.StackExchangeRedis.Execute"`.

Tier-2 tests MUST pin the source name as a constant; Tier-3 tests MUST assert
`activity.Source.Name == "OpenTelemetry.Instrumentation.StackExchangeRedis"` verbatim. Upstream
renames ship as major-version bumps, so pinning as a string is safe between minor releases.

### (d) Keyed-services registration pattern

Hexalith registers two keyed `IConnectionMultiplexer` entries (`"redis"` → RediSearch + Redis
Vector; `"falkordb"` → the Redis-protocol-speaking graph DB). The instrumentation attaches
per-multiplexer, so **both** must be registered.

The original story spec assumed a
`AddRedisInstrumentation(object serviceKey, Action<StackExchangeRedisInstrumentationOptions>)`
keyed-DI overload — that overload does **not** exist in `1.15.1-beta.1`. Pinned registration
shape:

1. `tracing.AddRedisInstrumentation(ConfigureRedisInstrumentation)` — registers the
   `"OpenTelemetry.Instrumentation.StackExchangeRedis"` ActivitySource and the shared
   `StackExchangeRedisInstrumentation` singleton with the `FlushInterval = 100ms` options from
   (e).
2. `tracing.ConfigureRedisInstrumentation((sp, instrumentation) => { ... })` — called during
   `TracerProvider` service resolution. Resolves BOTH keyed `IConnectionMultiplexer` instances
   via `sp.GetRequiredKeyedService<IConnectionMultiplexer>(key)` and calls
   `instrumentation.AddConnection(key, mux)` for each.

An explicit top-level `AddSource("OpenTelemetry.Instrumentation.StackExchangeRedis")` is NOT
needed — `AddRedisInstrumentation` does it internally.

### (e) Flush-semantics policy — 100 ms in ALL environments

`FlushInterval = TimeSpan.FromMilliseconds(100)` in both production and test. The originally
drafted env-gated test-only override (keyed on `InMemoryTelemetryEnvironment.EnvVar`) was dropped
as unnecessary config surface. Production trivially absorbs a 10Hz drain-thread wake per
multiplexer; coupling the Redis flush gate to a test-only env var would leak into any future
non-Tier-3 in-memory path and create a second way for the two values to diverge.

The shared `ConfigureRedisInstrumentation(StackExchangeRedisInstrumentationOptions options)`
callback sets `FlushInterval = TimeSpan.FromMilliseconds(100)` and is passed to
`AddRedisInstrumentation(ConfigureRedisInstrumentation)`. Task 2.4(e) pins
the invariant that both keyed connections are attached under that same `FlushInterval = 100ms`
policy — this guards against a future refactor that wires divergent callbacks or a different
post-build attachment path even though upstream stores `FlushInterval` on a shared singleton.

### (f) Missing-key eager-fail discipline — DI-guard path

The shipped `ConfigureRedisInstrumentation((sp, instrumentation) => ...)` callback runs during
`TracerProvider` service resolution, after DI is built. A misconfiguration that drops the
`falkordb` registration would therefore otherwise surface late (or regress to a silent skip if a
future refactor weakened the keyed lookup).

**Pinned remediation:** `ConfigureOpenTelemetry` calls `AddRedisKeyedConnectionGuard(tracing,
key)` once per keyed connection BEFORE the `AddRedisInstrumentation(ConfigureRedisInstrumentation)`
call. The guard adds a DI-resolution callback via `tracing.AddInstrumentation(sp => ...)` that
asserts `sp.GetKeyedService<IConnectionMultiplexer>(serviceKey) is not null` and throws
`InvalidOperationException($"Keyed IConnectionMultiplexer '{serviceKey}' not registered — Story
8.5 Redis OTEL needs both 'redis' and 'falkordb' keys")` on miss. The callback returns the
private no-op guard handle as the "instrumentation" payload — meaningful in diagnostic surfaces,
but detached from the live multiplexer so the guard cannot expose or dispose the real connection.

The subsequent `ConfigureRedisInstrumentation((sp, instrumentation) => { ... })` callback
resolves the two keyed multiplexers via `GetRequiredKeyedService` and calls
`instrumentation.AddConnection(key, mux)`. Because the guard fires first (at the same
`TracerProvider.Build()` point), a missing key surfaces the descriptive message from
`AddRedisKeyedConnectionGuard` rather than the less-informative `InvalidOperationException`
that `GetRequiredKeyedService` would throw.

The "rely on upstream-native throw" path is **REJECTED** because upstream is silent-null today;
test class `OpenTelemetryRegistrationTests.TracerRegistration_MissingKeyedMultiplexer_FailsEagerly`
pins the DI-guard behavior, and a companion integration test exercises the canonical public entry
(`AddServiceDefaults` → `Build()`) so a future refactor that splits the guard into a separate
extension cannot leave it unreachable.

### (g) Upgrade-on-GA trigger (dated, not loose)

Revisit this prerelease pin within **14 days of `OpenTelemetry.Instrumentation.StackExchangeRedis
1.15.0`** (non-prerelease) shipping on nuget.org, **OR** by **2026-09-30**, whichever comes first.
Tracking lives in `_bmad-output/implementation-artifacts/deferred-work.md` (not a loose
`// TODO:` code comment — the loose-comment form was explicitly rejected). Owner:
Memories release-manager rotation; review-by date: `2026-09-30`.

### (h) FalkorDB `db.system` semantic-conventions debt — Path A (ship now)

The instrumentation tags BOTH `redis` and `falkordb` connections with `db.system=redis` and
`db.system.name=redis`. Upstream cannot distinguish FalkorDB from Redis at the protocol level
(FalkorDB speaks the Redis wire protocol). Without remediation, APM backends (Honeycomb,
Datadog, Grafana Tempo) would misclassify FalkorDB graph queries as generic Redis commands.

**Pinned path — Path A (ship now):** add `FalkorDbSemanticAttributeProcessor : BaseProcessor<Activity>`
in `src/Hexalith.Memories.ServiceDefaults/Telemetry/`. On `OnEnd`, inspect
`activity.GetTagItem("server.address")` (with `net.peer.name` fallback). If the value matches the
configured FalkorDB host set (default `"falkordb"` plus any aliases / endpoint hosts resolved from
the keyed FalkorDB multiplexer), rewrite `db.system` and `db.system.name` to `"falkordb"`.

The processor is registered **inside** the `WithTracing` lambda via
`tracing.AddProcessor(sp => new FalkorDbSemanticAttributeProcessor(ResolveFalkorDbHostnames(...)))`,
placed AFTER both `AddRedisKeyedConnectionGuard` calls but BEFORE
`builder.AddOpenTelemetryExporters()`. Co-locating
the processor registration inside the same lambda guarantees deterministic processor-vs-exporter
order — a third-party processor registered via the parallel
`builder.Services.ConfigureOpenTelemetryTracerProvider(...)` extension could otherwise finalize
the activity before the FalkorDB rewrite fires.

**Path B (documented debt) — REJECTED** because APM misclassification is operator-visible and
cheap to fix now; ~80 lines + 5 unit tests (rewrite-hit / alias-hit / rewrite-miss /
non-Redis-source skip / processor-order) is a
better trade than deferring to 2026-09-30 per the Path B alternative framing in Task 2.7.

### (i) Conservative attribution shape — ≥1 Redis span

End-to-end tests assert **at least one** Redis-source activity per captured trace, not per-keyed
connection. This is the cheaper assertion and avoids Tier-3 flake when a test scenario happens to
route entirely through RediSearch without touching FalkorDB (or vice versa). Per-connection
attribution (≥1 span per keyed connection) becomes a follow-up if operator feedback demands it;
`server.address` + `db.system` (post-Path-A) are already the canonical discriminators when the
operator wants to split the backends in Grafana / Datadog.

---

## Cross-references

- [cli-config.md](cli-config.md) — `--telemetry` flag + `HEXALITH_MEMORIES_OTEL_ENDPOINT` env var.
- [cli-output-formats.md](cli-output-formats.md) — `status telemetry` envelope shape.
- [experimental-apis.md](experimental-apis.md) — `HXL001` now includes `GetTelemetrySummaryAsync`.
- [quickstart.md](quickstart.md) — the post-wizard "regulated environments" caveat is resolved by
  this story's FR67 audit events (operators capture the log stream for SOC 2 / GDPR / HIPAA
  evidence).
- [health-checks.md](health-checks.md) — liveness/readiness endpoints (Story 8.1): `/alive`,
  `/ready`, and `/health` contracts for orchestrator probes; health paths are deliberately excluded
  from this document's AspNetCore trace emission (see AC #5).
- [consistency.md](consistency.md) — tenant-scoped consistency verification and repair workflows
  (Story 8.2). The five consistency endpoints are NOT in the audited scope; a regression guard in
  `ConsistencyEndpointTests` pins this invariant. Current audited operation types are search,
  ingest, traverse, case-access, delete, tenant-lifecycle, tenant-config, case-member, and
  annotation.
- [export.md](export.md) — case and tenant data export (Story 8.3). Export endpoints are
  deliberately NOT in the `AccessTelemetryEvent` audited scope. A follow-up story will ship a
  dedicated `ExportTelemetryEvent` bank (EventId 8320-8329 reserved) so operators get a
  per-export audit trail.

## Story 9.3 — handler registry + mismatch detector metrics

Three new instruments:

- `memories.handlers.registered` — observable gauge; tags: `tenant_id`. Per-tenant count of
  registered `SourceToTenantMap` sources. Observer reads `IOptionsMonitor<TenantEventRoutingOptions>`
  on every metric export (no Redis round-trip).
- `memories.handlers.mismatches` — counter; tags: `tenant_id`, `severity`. Emitted by
  `HandlerMismatchDetector.DetectAsync` per detected mismatch. Low-cardinality by design:
  2 severities × 4 categories × tenant count. Story 16.1 adds the
  `ProjectionBindingMissing` category alongside `StaleHandler`, `UnhandledEventType`, and
  `VersionMismatch`; the metric tag remains `severity` only — the category is not added as a
  tag to keep cardinality stable. Story 16.1 also reserves log-event bank 9150–9159 for
  provider-failure (`9150`), snapshot tenant-mismatch (`9151`), and null-bindings (`9152`)
  Warnings.
- `memories.handlers.observations.dropped` — counter; tags: `reason` ∈
  `{backpressure, timeout, redis_error}`. Emitted by the bounded fire-and-forget observation write
  path when in-flight cap / timeout / Redis error drops an observation. **No `tenant_id` tag** — it
  would re-introduce the Risk #4 cardinality concern and the drop is a store-side condition, not
  a tenant-scoped event.

### Substrate separation (ADR-9.3-002)

9.3's `IObservedEventTypeStore` is Redis-backed with a 24h rolling window. It is **deliberately
separate** from Story 7.5's in-process `RollingCounterStore` (5-minute / 5-slot ring). The two
stores have different invariants:

| Store | Window | Backing | Access pattern | Failure posture |
|---|---|---|---|---|
| `RollingCounterStore` (7.5) | 5 min | In-process (per-pod) | MeterListener hot path on every metric emission | Must stay bounded to avoid memory growth |
| `IObservedEventTypeStore` (9.3) | 24 h | Redis | Rare, operator-triggered reads; fire-and-forget writes from the ingestion hot path | Fail-open on write errors; authoritative reads |

Future contributors MUST NOT extend the 5m ring to cover handler observation. Coupling them binds
future changes to both substrates. If a unification is ever warranted, it should land via an
explicit ADR that reconciles the invariants rather than an incremental extension.
