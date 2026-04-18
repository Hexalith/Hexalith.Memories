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

| Environment | Tool | Endpoint / config |
|---|---|---|
| Local dev (Aspire) | Aspire Dashboard | `http://localhost:18888` (UI), `http://localhost:18889` (OTLP) |
| Production — Jaeger | Jaeger OTLP collector | `OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317` |
| Production — Tempo | Grafana Tempo | `OTEL_EXPORTER_OTLP_ENDPOINT=http://tempo:4317` |
| Production — Datadog | Datadog Agent | `OTEL_EXPORTER_OTLP_ENDPOINT=http://dd-agent:4318` |
| Production — Honeycomb | Honeycomb ingest | `OTEL_EXPORTER_OTLP_ENDPOINT=https://api.honeycomb.io:443` |

CLI-side telemetry is **opt-in** — set `HEXALITH_MEMORIES_OTEL_ENDPOINT` or pass `--telemetry` to any
`memories` subcommand to export CLI spans.

---

## ActivitySource names

Memories pins a single source name: **`Hexalith.Memories`** (declared by
`Hexalith.Memories.Telemetry.MemoriesActivitySource.SourceName`). Future submodule sources follow
the convention `Hexalith.Memories.<subsystem>` (e.g., `Hexalith.Memories.Mcp` for the Phase 1.5 MCP
server). Do NOT create per-request sources.

### Activity names

| Activity | Emitted by | Tags |
|---|---|---|
| `memories.search` | `GET /api/search` | `memories.tenant_id`, `memories.operation`, `memories.axis` |
| `memories.ingest` | `POST /api/ingest` (+`/url`, `/directory`) | `memories.tenant_id`, `memories.operation`, `memories.source_type` |
| `memories.traverse` | `GET /api/tenants/{id}/traverse` | `memories.tenant_id`, `memories.operation` |
| `memories.case-access` | `GET /api/.../memory-units/{id}` | `memories.tenant_id`, `memories.case_id`, `memories.memory_unit_id` |
| `memories.cli.invoke` | CLI root span (opt-in) | `memories.command` |

---

## Meter name + metric catalog

Single meter: **`Hexalith.Memories`** (declared by `MemoriesMeter.Name`).

| Metric | Type | Unit | Tag keys | Description |
|---|---|---|---|---|
| `memories.ingestion.documents` | Counter&lt;long&gt; | `{documents}` | `tenant_id` | Total documents ingested successfully |
| `memories.ingestion.failures` | Counter&lt;long&gt; | `{documents}` | `tenant_id`, `error_code` | Total ingestion scheduling failures |
| `memories.search.requests` | Counter&lt;long&gt; | `{requests}` | `tenant_id`, `axis` | Total search requests per resolved axis |
| `memories.search.duration` | Histogram&lt;double&gt; | `ms` | `tenant_id`, `axis` | Request latency for the resolved axis |
| `memories.index.size` | ObservableGauge&lt;long&gt; | `{documents}` | `tenant_id`, `axis` | Per-tenant per-axis index size |
| `memories.pipeline.queue_depth` | ObservableGauge&lt;int&gt; | `{items}` | `tenant_id` | Per-tenant ingestion queue depth |

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

Every search / ingest / traverse / case-access / delete operation emits **exactly one**
`AccessTelemetryEvent` via `[LoggerMessage]` source-generated emitters. Dedicated logger category:
`Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory`.

EventId bank: **7500–7599** (success 7501–7505, error 7511–7515).

```jsonc
{
  "schemaVersion": 1,           // bump only on breaking schema changes
  "eventId": 7501,              // 7500-7599 bank
  "timestamp": "2026-04-17T12:34:56.789Z",
  "tenantId": "acme",           // or "__rejected__" if guard rejected
  "operationType": "search",    // search | ingest | traverse | case-access | delete
  "caseId": null,
  "user": "anonymous",          // ADR-7.5-004 resolution rules
  "queryParams": { "query": "...", "axis": "hybrid", "maxResults": 10 },
  "resultCount": 3,             // null for write/schedule operations
  "durationMs": 42,
  "outcome": "ok",              // ok | partial | error
  "errorCode": null,
  "traceId": "<W3C trace id>",
  "spanId": "<W3C span id>"
}
```

### Versioning policy

- **Additive** field additions keep `schemaVersion = 1`. Consumers MUST ignore unknown fields.
- **Breaking** changes (rename, removal, type change) bump `schemaVersion` to `2` — AND the test
  `AccessTelemetryEventSchemaTests.V1_FieldNames_AreFrozen` guards against silent renames.

### User-identity resolution (ADR-7.5-004)

1. **Ingest paths** — `IngestionInput.IngestedBy` (always present; `required` contract field).
2. **Search / traverse / case-access paths** — `x-user-id` HTTP header (Phase 1.5 contract) → else
   `"anonymous"`.
3. **Wizard-originated operations** — `HEXALITH_MEMORIES_WIZARD_INVOCATION_ID` env set → user =
   `"quickstart-wizard"`.

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
      "Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory": "Information"
    },
    "Console": {
      "LogLevel": {
        "Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory": "None"
      }
    }
    // Direct the audit category to a dedicated JSON file sink via your log pipeline of choice
    // (Serilog, Seq, filebeat, fluentd). The structured JSON produced by AddJsonConsole is the
    // canonical form; route it per your org policy.
  }
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
time series. MVP keeps tagging at `tenant_id` + `axis` + `source_type` + `error_code` only
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
           metric_names: [ "memories\\.search\\..*" ]
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

```promql
# Tenant ingestion throughput (last 5m, per tenant)
sum by (tenant_id) (rate(memories_ingestion_documents_total[5m]))

# Per-axis p99 search latency (last 5m)
histogram_quantile(0.99, sum by (axis, le) (rate(memories_search_duration_ms_bucket[5m])))

# Hot tenants with elevated ingestion failure rate
sum by (tenant_id) (rate(memories_ingestion_failures_total{error_code!=""}[5m]))
  / sum by (tenant_id) (rate(memories_ingestion_documents_total[5m]))

# Queue depth alert (>10 for any tenant for 5m)
max_over_time(memories_pipeline_queue_depth[5m]) > 10
```

These are illustrative starter queries — not a shipped dashboard. Operators compose dashboards to
their observability stack.

---

## Audit log volume estimates

Back-of-envelope (MVP assumptions):

| Parameter | Value |
|---|---|
| Event size | ~300 bytes (compact JSON) |
| Search rate per tenant | 10 req/s (high end) |
| Active tenants per node | 10 |
| Nodes | 1 (single-node dev); scales with tenant count |

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
    "indexSizes": { "rediSearchKeyCount": 1234, "redisVectorKeyCount": 1230, "falkorDbNodeCount": 5000 },
    "indexHealth": { "rediSearch": "Ready", "redisVector": "Ready", "falkorDb": "Ready" },
    "searchMetrics": {
      "syntactic": { "requestsLast5m": 120, "errorsLast5m": 0 },
      "semantic":  { "requestsLast5m": 45,  "errorsLast5m": 0 },
      "graph":     { "requestsLast5m": 10,  "errorsLast5m": 0 },
      "hybrid":    { "requestsLast5m": 30,  "errorsLast5m": 0 }
    },
    "ingestionMetrics": { "documentsLast5m": 40, "failuresLast5m": 0, "queueDepth": 0 }
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

**Not covered** (yet):

- **MCP Server → Memories Server trace propagation** (via DAPR service invocation). MCP does not
  exist as of 7.5. The Epic 10 story introducing `Hexalith.Memories.Mcp` MUST include an end-to-end
  trace test covering the MCP → Server DAPR hop as part of its Definition of Done. Flag this
  dependency in Epic 10's References section.

---

## Cross-references

- [cli-config.md](cli-config.md) — `--telemetry` flag + `HEXALITH_MEMORIES_OTEL_ENDPOINT` env var.
- [cli-output-formats.md](cli-output-formats.md) — `status telemetry` envelope shape.
- [experimental-apis.md](experimental-apis.md) — `HXL001` now includes `GetTelemetrySummaryAsync`.
- [quickstart.md](quickstart.md) — the post-wizard "regulated environments" caveat is resolved by
  this story's FR67 audit events (operators capture the log stream for SOC 2 / GDPR / HIPAA
  evidence).
