# Health checks — operator reference

Operator-facing reference for the Memories Server's liveness and readiness endpoints.
Describes the probe contract, response shape, orchestrator wiring, and capability
semantics. Complements [telemetry.md](./telemetry.md) (metrics + traces + audit
events): health checks answer **"is the service up?"**; telemetry answers
**"is it working well?"**.

Shipped in Story 8.1.

## Endpoint summary

| Endpoint  | Path      | Predicate                | Typical consumer                         | Aggregate on failure                                           | HTTP status                                          |
| --------- | --------- | ------------------------ | ---------------------------------------- | -------------------------------------------------------------- | ---------------------------------------------------- |
| `/health` | `/health` | _(none — union)_         | Dashboards / diagnostics                 | `Degraded` or `Unhealthy` (depends on which check fails)       | `Healthy` → 200, `Degraded` → 200, `Unhealthy` → 503 |
| `/alive`  | `/alive`  | `Tags.Contains("live")`  | Kubernetes `livenessProbe` → pod restart | `Unhealthy`                                                    | 200 / 503 (no `Degraded` entries by design)          |
| `/ready`  | `/ready`  | `Tags.Contains("ready")` | Kubernetes `readinessProbe` → LB gate    | `Degraded` for backend failures; `Unhealthy` for Dapr failures | `Healthy` / `Degraded` → 200, `Unhealthy` → 503      |
| DAPR API health | `/api/v1/health` | `Tags.Contains("ready")` | MCP-to-Server DAPR invocation | Same as `/ready` | `Healthy` / `Degraded` → 200, `Unhealthy` → 503 |

The status-code map lives at [`Extensions.cs`](../../src/Hexalith.Memories.ServiceDefaults/Extensions.cs) — `Healthy` and `Degraded` both resolve to `200 OK`; only `Unhealthy` resolves to `503 Service Unavailable`. This inversion is deliberate (see [Probe tuning guidance](#probe-tuning-guidance)).

## Check inventory

| Name              | Tags            | Probe                                                 | Failure status | Affected capabilities                                               |
| ----------------- | --------------- | ----------------------------------------------------- | -------------- | ------------------------------------------------------------------- |
| `self`            | `live`          | Always `Healthy`                                      | _n/a_          | _n/a_                                                               |
| `dapr-sidecar`    | `live`, `ready` | `DaprClient.CheckHealthAsync` (gRPC round-trip)       | `Unhealthy`    | `all-service-invocation`, `workflow-orchestration`, `actor-runtime` |
| `dapr-statestore` | `ready`         | Dapr state-store probe read of `__health_probe__`     | `Unhealthy`    | `workflow-state-persistence`, `actor-state-persistence`             |
| `redisearch`      | `ready`         | `FT._LIST` against the keyed `redis` multiplexer      | `Degraded`     | `syntactic-search`, `hybrid-search-syntactic-axis`                  |
| `redis-vector`    | `ready`         | `MODULE LIST` (looks for the `search` module)         | `Degraded`     | `semantic-search`, `hybrid-search-semantic-axis`                    |
| `falkordb`        | `ready`         | `GRAPH.LIST` against the keyed `falkordb` multiplexer | `Degraded`     | `graph-traversal`, `graph-scoped-search`                            |

All five registrations share a single 3-second timeout (`Program.cs` — `healthCheckTimeout`). Backend checks fail as `Degraded` so one backend outage does not remove the pod from service rotation — the request pipeline's per-capability routing (Story 5.6) handles the reduced capability mix.

MCP adds `memories-upstream` to readiness. An unavailable Server is immediately `Unhealthy`; there is no initial degraded strike window because MCP cannot serve safely without that dependency. `/api/v1/health` is anonymous at the OIDC layer so the health call cannot deadlock on bearer acquisition, but the DAPR application-token middleware and deny-by-default workload ACL still constrain it to the `memories-mcp` sidecar identity.

## Response shape

All three endpoints serialize the `HealthReport` via `BackendHealthResponseWriter`
(in `Hexalith.Memories.ServiceDefaults/Health/`). The payload is UTF-8 JSON with
camelCase field names and is pinned to **schema version 1**:

```json
{
    "schemaVersion": 1,
    "status": "Healthy",
    "totalDurationMs": 42,
    "entries": {
        "dapr-sidecar": {
            "status": "Healthy",
            "description": "Dapr sidecar is responsive.",
            "durationMs": 3,
            "affectedCapabilities": []
        },
        "dapr-statestore": {
            "status": "Healthy",
            "description": "Dapr state store 'statestore' is accessible.",
            "durationMs": 9,
            "affectedCapabilities": []
        },
        "redisearch": {
            "status": "Healthy",
            "description": "RediSearch module reachable; 3 indexes loaded.",
            "durationMs": 5,
            "affectedCapabilities": []
        },
        "redis-vector": {
            "status": "Healthy",
            "description": "Redis Vector capability reachable.",
            "durationMs": 4,
            "affectedCapabilities": []
        },
        "falkordb": {
            "status": "Healthy",
            "description": "FalkorDB reachable; 1 graphs.",
            "durationMs": 6,
            "affectedCapabilities": []
        }
    }
}
```

### Example: one backend degraded

```json
{
    "schemaVersion": 1,
    "status": "Degraded",
    "totalDurationMs": 3006,
    "entries": {
        "dapr-sidecar": {
            "status": "Healthy",
            "description": "Dapr sidecar is responsive.",
            "durationMs": 2,
            "affectedCapabilities": []
        },
        "dapr-statestore": {
            "status": "Healthy",
            "description": "Dapr state store 'statestore' is accessible.",
            "durationMs": 8,
            "affectedCapabilities": []
        },
        "redisearch": {
            "status": "Healthy",
            "description": "RediSearch module reachable; 2 indexes loaded.",
            "durationMs": 4,
            "affectedCapabilities": []
        },
        "redis-vector": {
            "status": "Healthy",
            "description": "Redis Vector capability reachable.",
            "durationMs": 3,
            "affectedCapabilities": []
        },
        "falkordb": {
            "status": "Degraded",
            "description": "FalkorDB unreachable: RedisConnectionException",
            "durationMs": 3000,
            "affectedCapabilities": ["graph-traversal", "graph-scoped-search"]
        }
    }
}
```

Schema is versioned by the `schemaVersion` field. **Additive** field changes (new
backends, new top-level informational fields) keep `schemaVersion: 1`; **breaking**
changes (rename, removal, type change) bump to `schemaVersion: 2` with a migration
note in this document.

### `affectedCapabilities` — consumer guidance

The array ships as an operator-facing diagnostic signal. No production gateway or
proxy today auto-routes on `affectedCapabilities` — capability-aware routing is a
future story. Clients that care about capability-specialized requests (graph-only,
vector-only) MUST read the array themselves and decide; they cannot assume a
gateway does it for them.

### Debugging failed probes

V1 does not include a `probeId` or `traceId` field — health paths are excluded
from OpenTelemetry by design (Story 7.5 AC #5). Correlate failing probes with
sidecar/backend logs by timestamp and `durationMs`. If correlation IDs become
necessary in practice, a future story can add a V1-compatible `probeId` field
(monotonic counter, zero trace overhead).

## Orchestrator probe configuration

### Kubernetes

The production containers listen on port `8080`. Because `APP_API_TOKEN` is mandatory in Production, probes execute in the application container and send the secret-backed `dapr-api-token` header. The startup probe parses the aggregate JSON and accepts only `"status":"Healthy"` for at most 60 seconds after the application container enters Running. Readiness then accepts either `Healthy` or `Degraded` through the endpoint's HTTP mapping, so losing only RediSearch, Redis Vector, or FalkorDB does not remove otherwise useful search axes from service. Liveness calls `/alive`. See the exact commands and thresholds in `deploy/kubernetes/base/server-deployment.yaml` and `mcp-deployment.yaml`.

Redis Stack and FalkorDB have separate, longer startup probes for persistent-data recovery. Their ConfigMap name hashes are part of the StatefulSet pod templates, so persistence-setting changes trigger controlled rollouts while preserving the frozen resource/PVC defaults.

### Docker (plain / Podman)

```dockerfile
# In the Dockerfile, after EXPOSE:
HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=3 \
  CMD wget --header="dapr-api-token: $APP_API_TOKEN" -qO- http://localhost:8080/alive >/dev/null || exit 1
```

Notes on the Docker form:

- `--start-period=20s` parallels Kubernetes' `initialDelaySeconds: 15` plus a
  5-second buffer for image cold-start.
- Docker has no equivalent of Kubernetes' separate readiness probe — `HEALTHCHECK`
  controls the container status only, not upstream load-balancer rotation. For
  Docker Swarm / external LB environments, probe `/ready` via an external sidecar
  rather than `HEALTHCHECK`.
- The `curl` path requires `curl` in the image. For distroless or
  `mcr.microsoft.com/dotnet/runtime-deps` images, use `wget -q --spider` or a
  compiled health-check binary.

### Docker Compose

```yaml
services:
    memories-server:
        healthcheck:
            test:
                [
                    "CMD-SHELL",
                    "wget --header='dapr-api-token: $$APP_API_TOKEN' -qO- http://localhost:8080/alive >/dev/null || exit 1",
                ]
            interval: 10s
            timeout: 5s
            start_period: 20s
            retries: 3
```

## Aspire dashboard

Local development via `dotnet run --project src/Hexalith.Memories.AppHost` exposes
the Aspire dashboard at [http://localhost:18888](http://localhost:18888). The
resource list shows each container's computed health state; the Memories Server
resource surfaces the aggregate `/health` status in the Health column.

## Capability-affected mapping

The `BackendCapabilityCatalog` (in `Hexalith.Memories.ServiceDefaults/Health/`)
is the single source of truth:

| Check             | Capabilities affected when the check fails                          |
| ----------------- | ------------------------------------------------------------------- |
| `redisearch`      | `syntactic-search`, `hybrid-search-syntactic-axis`                  |
| `redis-vector`    | `semantic-search`, `hybrid-search-semantic-axis`                    |
| `falkordb`        | `graph-traversal`, `graph-scoped-search`                            |
| `dapr-sidecar`    | `all-service-invocation`, `workflow-orchestration`, `actor-runtime` |
| `dapr-statestore` | `workflow-state-persistence`, `actor-state-persistence`             |

Add new entries alongside new check registrations;
`BackendCapabilityCatalogTests` enforces the 1-to-1 mapping at build time.

## Probe tuning guidance

- **`readinessProbe.initialDelaySeconds ≥ 15`** — the shared Redis / FalkorDB
  multiplexers open connections lazily on the first probe. A probe-before-connect
  surfaces as `Degraded` for 5–10 seconds. Tuning `initialDelaySeconds` above 15
  eliminates the startup flicker.
- **`livenessProbe.failureThreshold ≥ 3` and `periodSeconds ≥ 10`** — the
  `dapr-sidecar` check is shared by both `/alive` and `/ready`. A DAPR
  control-plane glitch is correlated across all pods in the deployment; a tight
  liveness probe turns a 30-second control-plane blip into a minutes-long full
  outage as every pod restarts in lockstep. The sidecar has its own auto-restart
  loop; pod restart should only trigger once sidecar auto-restart has failed
  multiple times.

### Blast radius

Because `dapr-sidecar` participates in both liveness and readiness, a correlated
sidecar outage affects the whole deployment simultaneously. Keep the defaults
above unless you have a specific reason to lower them — and document that reason
next to the orchestrator config.

## Known gaps and limitations

- **Graph-axis-optional deployments.** The architecture allows FalkorDB to be
  omitted from a deployment. The readiness endpoint cannot currently distinguish
  "FalkorDB is down" from "FalkorDB is intentionally disabled"; operators must
  filter `falkordb` alerts in graph-disabled deployments. A Phase 2 story may add
  an axis-optionality signal in the check registration.
- **Capability-aware routing.** `affectedCapabilities` is a diagnostic signal.
  Today no gateway or proxy auto-routes around degraded capabilities — clients
  must honor the array themselves.
- **Probe correlation.** V1 has no `probeId` / `traceId` field by design (health
  paths are excluded from tracing). Correlation is by timestamp and sidecar /
  backend logs.

## Out of scope

- Alert thresholds / SLO definitions — endpoints emit signal; alerting wiring is
  downstream.
- Consistency verification across backends — shipped by Story 8.2 (FR73).
- Data export functionality — shipped by Story 8.3 (FR71).
- Per-tenant index health — use
  [`GET /api/v1/tenants/{tenantId}/configuration`](../../src/Hexalith.Memories.Server/Tenants/)
  (Story 5.5) or
  [`GET /api/v1/tenants/{tenantId}/telemetry/summary`](../../src/Hexalith.Memories.Server/Telemetry/)
  (Story 7.5). Probing every tenant's index per probe would be O(tenants × axes)
  of backend load — off-contract.
- A separate `/metrics` Prometheus endpoint — metrics flow via the OTLP exporter
  configured in ServiceDefaults (Story 7.5).
- `Retry-After` headers on 503 responses — orchestrator probes have their own
  retry cadence (`periodSeconds`).

## See also

- [consistency.md](./consistency.md) — per-tenant data-consistency verification
  and repair workflows (Story 8.2). Orthogonal to health checks: `/ready`
  answers "is the backend reachable?", while consistency answers "is the data
  consistent?"
- [telemetry.md](./telemetry.md) — rolling counters, trace propagation, access
  telemetry.
