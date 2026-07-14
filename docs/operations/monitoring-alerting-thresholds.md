# Monitoring and Alerting Thresholds

## Purpose and scope

Owner: observability/platform operations. Review cadence: monthly during initial tuning, quarterly after
stabilization, and after metric, dashboard, health, SLO, provider, topology, or incident changes. Last
verified: 2026-07-14 at repository revision
`1553ee6708f644f3a4bc3638d3aaceed682b2371`.

This runbook maps product targets and provisional operational thresholds to real application,
OpenTelemetry, health, Kubernetes, Dapr, Redis, and FalkorDB signals. The committed dashboard at
[`deploy/grafana/dashboards/memories-operability.json`](../../deploy/grafana/dashboards/memories-operability.json)
observes metrics; this story deploys no alert rules, contact points, or paging infrastructure. Operators
must implement, validate, own, and tune alerts in their platform.

Blast radius is explicit per condition: tenant, search axis, provider, backend, workload, or all tenants.
Alert labels must remain low-cardinality. Application metrics permit only the source-defined bounded tag
keys; never add case, user, memory-unit, workflow, correlation, source URI, or content as metric labels.

## Prerequisites and authorization

- Assign an alert owner, response owner, service/tenant scope, paging destination, maintenance policy,
  NoData behavior, tuning window, and linked incident/runbook before enabling a rule.
- Verify `OTEL_EXPORTER_OTLP_ENDPOINT`, collector/exporter, Prometheus scrape/remote-write, Grafana data
  source, Kubernetes/Redis/Dapr exporters, and health JSON parsing. Missing application OTLP export is
  non-fatal to the service, so monitoring must detect it independently.
- Use a representative baseline by workload class, tenant size, provider, vector dimensions, cluster,
  and release. Infrastructure thresholds without a product requirement are provisional until measured.
- Initialize non-secret review scope:

  ```bash
  NAMESPACE=hexalith-memories
  TENANT_ID="${TENANT_ID:-monitoring-canary}"
  REVIEW_ID="${REVIEW_ID:-alert-review-$(date -u +%Y%m%dT%H%M%SZ)}"
  printf 'review=%s namespace=%s tenant=%s\n' "$REVIEW_ID" "$NAMESPACE" "$TENANT_ID"
  ```

- Alert changes require observability owner review; page-policy/contact changes also require incident-
  response owner approval. This runbook contains no destructive action.

## Signals and evidence

### Signal inventory

| Class | Real source | Examples |
|---|---|---|
| Application meter | `Hexalith.Memories` via OpenTelemetry | `memories.ingestion.documents`, `memories.ingestion.failures`, `memories.search.duration`, `memories.index.size`, `memories.pipeline.queue.depth`, `memories.rate.limit.rejections`, `memories.handlers.mismatches`, `memories.handlers.observations.dropped` |
| Committed Prometheus dashboard form | operability dashboard | `memories_ingestion_documents_total`, `memories_search_duration_milliseconds_bucket`, `memories_pipeline_queue_depth`, other normalized names below |
| Structured health | `/health`, `/alive`, `/ready` schema v1 | top-level/entry `status`, `durationMs`, `affectedCapabilities`; `Degraded` can be HTTP 200 |
| Kubernetes | metrics-server/cAdvisor/kube-state-metrics/events | CPU, memory/RSS, throttling, PVC used/capacity, restarts, OOMKilled, readiness, startup time |
| Redis | approved Redis exporter or authenticated collection | `INFO memory`, `INFO persistence`, `FT.INFO`, rejected writes, AOF/RDB/rewrite status |
| FalkorDB | approved exporter or authenticated collection | reachability, `GRAPH.MEMORY USAGE`, graph/node/edge/count query results |
| Dapr/workflows | Dapr metrics/logs/status plus application workflow signals | sidecar/state-store health, active/stalled/failed workflows, events 9171–9173 |
| Operator-derived SLO | recorded traces/logs/probes/controlled benchmark | publish-to-search freshness, cold-start-to-Healthy, throughput under the PRD baseline |

`memories.index.size` and `memories_index_size` count documents per tenant/axis, not bytes. Use Redis,
FalkorDB, Kubernetes working-set, and PVC signals for capacity.

### Product requirement targets

These are hard PRD targets, not measured production results:

- syntactic search p95 `<200 ms`, semantic p95 `<500 ms`, hybrid p95 `<1 s`, and graph traversal p95
  `<2 s`, under the documented 10-concurrent-query/10K-unit conditions (graph depth at most 5);
- ingestion `>100 units/min` for payloads `<=10 KB` and `>10 units/min` for payloads `<=1 MB` per tenant
  under the documented baseline;
- EventStore publication-to-search freshness `<5 s` under normal conditions, with provider-rate-limit
  degradation recorded; and
- application cold start within 60 seconds (`<=60 s`) from containers running to accepting queries,
  excluding image pull.

Do not claim these targets pass from an alert query alone. Preserve controlled benchmark/probe evidence.

### Recommended alert matrix

`Provisional` means baseline-dependent and not a product requirement. Warning levels on hard-target rows
are tuning aids; the critical condition carries the exact requirement boundary.

| Signal / class | Warning | Critical | Window | NoData behavior | Blast radius and action | Verification |
|---|---|---|---|---|---|---|
| Search p95 from `memories_search_duration_milliseconds_bucket` by `tenant_id,axis` | Provisional: p95 at 80% of the axis target | Hard target violated: syntactic `>=200 ms`, semantic `>=500 ms`, hybrid `>=1 s`, graph `>=2 s` | 5m for 2 evaluations | Alert when traffic exists but histogram disappears; normal when request rate is zero | tenant + axis; use [Incident Response](./incident-response.md), preserve unaffected axes | controlled query and trace comparison; confirm units/tenant/concurrency/depth |
| Controlled ingestion throughput from `rate(memories_ingestion_documents_total[1m])` | Provisional: within 20% of applicable target | Hard target not exceeded: `<=100/min` for `<=10 KB`, `<=10/min` for `<=1 MB` | 10m controlled load | Unknown/alert if load generator proves submissions but metric absent | tenant/provider/pipeline; inspect queue, failures, provider and capacity | retained workload manifest and accepted/completed unit counts |
| Event freshness from publish timestamp to first searchable trace/probe result | Provisional: `>=4 s` | Hard target `>=5 s` under normal conditions | 5m rolling probes | Alert: required derived signal missing while events are published | tenant/event intake/provider; inspect pub/sub, topic, workflow, indexing | trace/probe correlation, not wall-clock guesses from unrelated hosts |
| Cold start from Kubernetes container Running to `/ready` JSON `Healthy` | Provisional: `>48 s` | Hard target `>60 s` | each rollout/start | Alert if either timestamp or structured health result is absent | workload/all tenants during rollout; stop expansion | Kubernetes timestamps plus parsed health JSON |
| Readiness aggregate/entries | any `Degraded` or unexpected capability loss | `Unhealthy`, HTTP 503, or Dapr/state-store unhealthy | 2 consecutive probes | Alert if probe cannot execute; HTTP 200 without parsed JSON is invalid evidence | backend axis or all tenants; follow incident decision tree | parse schema/status/entries/affected capabilities |
| Ingestion failure ratio using `memories_ingestion_failures_total` over successes+failures | Provisional: `>1%` | Provisional: `>5%` | 10m, minimum event volume set by owner | Alert when submissions exist but both series vanish | tenant/error code/provider/pipeline | compare structured failed-unit records and workflow outcomes |
| Rate-limit ratio using `memories_rate_limit_rejections_total` over accepted+rejected requests | Provisional: `>1%` | Provisional: `>5%` | 10m, minimum request volume | Alert when access traffic exists but rejection series/telemetry disappears | tenant/endpoint/provider; tune quota or intake, never bypass limiter | API responses, audit/error codes, provider quota |
| Pipeline stall: `memories_pipeline_queue_depth > 0` or rising **and** no increase in `memories_ingestion_documents_total` | Provisional: no progress 5m | Provisional: no progress 15m or oldest-work age exceeds policy | 5m/15m | Alert if queue signal or progress counter disappears during known intake | tenant/workflow; inspect stage/retries/state store | workflow statuses and successful progress after recovery |
| NL retry stall from `memories_natural_language_embedding_queue_depth`/`_bytes` and no progress | Provisional: nonzero without progress 10m | Provisional: growth for 30m or configured payload-store headroom threatened | 10m/30m | Alert when NL processing is enabled and series vanish | tenant/provider | queue drain, provider success, retained-payload TTL/headroom |
| Handler mismatch from `memories_handlers_mismatches_total` | any sustained warning-severity rate | any critical-severity increase | 5m | Alert if handler registry should exist but metric/dashboard series disappears | tenant/event routing | handler registry/mismatch endpoint and event intake logs |
| Dropped observations from `memories_handlers_observations_dropped_total` | any increase | Provisional: sustained increase for 10m | 5m/10m | Alert when handler telemetry otherwise exists but this series disappears | all/affected reason; telemetry/event-routing review | reconcile emitted/processed/drop counts and logs |
| Redis memory/RSS/headroom from exporter or `INFO memory` | Provisional: capacity policy warning headroom | Provisional: capacity stop headroom, rejected writes, or `noeviction` write failure | 10m and immediate on rejection | Alert when Redis is expected but exporter/collection missing | all tenants on shared Redis | authenticated `INFO memory`, capacity worksheet, write recovery |
| Redis persistence from `INFO persistence` | rewrite/save active beyond measured baseline or last status not `ok` | persistence failure, repeated rewrite failure, or recovery workspace exhausted | operation-specific + 5m | Alert: persistence series missing | all tenants/durable recovery | `rdb_last_bgsave_status`, `aof_last_bgrewrite_status`, PVC and backup evidence |
| PVC used/capacity for `data-redis-stack-0` and `data-falkordb-0` | Provisional: owner-set warning from growth forecast | Provisional: owner-set stop threshold or projected exhaustion before response window | 15m plus forecast | Alert if Bound PVC lacks utilization series | all tenants/backend | `kubectl get pvc`, CSI/storage metrics, capacity plan |
| Pod memory/restart/OOM from Kubernetes | Provisional: sustained working set near measured limit or restart increase | OOMKilled/crash loop or restart storm | 10m/immediate | Alert if expected workload series disappear | workload; axis/all tenants depending on pod | events, previous logs, limits, structured health |
| FalkorDB reachability/memory from health and `GRAPH.MEMORY USAGE` | `Degraded` or provisional growth/headroom warning | graph unavailable or provisional stop headroom | 2 probes / 15m | Alert when graph is configured but signal missing | graph axis/all tenants on shared backend | health entry, graph list/count, memory evidence |
| Dapr sidecar/state store/workflow status | health failure, active workflows not progressing, event 9171 sustained | sidecar/state-store unhealthy, stalled/failed workflows, event 9172/9173 | 2 probes / workflow policy | Alert if Dapr metrics/status query missing | all service invocation/workflows/actors | Dapr/control-plane/sidecar logs and terminal workflow state |
| OTLP/collector/scrape health | export warning, scrape gap, remote-write backlog | no application telemetry for 5m while health/traffic proves service active | 5m | Alert; never map missing telemetry to normal | observability/all affected services | exporter logs, collector health, Prometheus `up`, direct health and traffic |

## Procedure

1. Inventory emitted application names from `MemoriesMeter`, normalized names/queries from the committed
   dashboard, structured health entries, and infrastructure/exporter signals actually installed. Reject a
   proposed alert that cannot name a real source and owner.
2. Classify the threshold as hard PRD, policy, measured baseline, or provisional recommendation. Record
   workload and minimum-volume assumptions; ratios without a denominator/sample floor are noise.
3. Build the query grouped only by approved bounded dimensions. Use rates for counters, histogram buckets
   for p95, and pair queue depth with successful progress/oldest-work evidence.
4. Configure evaluation window, pending duration, severity, explicit NoData/error behavior, blast radius,
   runbook link, owner, labels, maintenance silence, and verification probe. Grafana/Prometheus do not make
   missing series actionable unless the rule handles them explicitly.
5. Test normal, warning, critical, recovery, query error, complete NoData, one-series-missing, rollout, and
   maintenance behavior in a non-production or approved canary scope. Confirm notifications contain no
   secrets, content, user/case/unit identifiers, or unbounded labels.
6. Observe for at least one representative workload cycle. Tune provisional thresholds from measured
   percentiles/growth and incident usefulness; preserve hard target boundaries unchanged.
7. Review alert volume, action taken, false-positive/negative evidence, ownership, NoData transitions, and
   stale-series eviction monthly until stable. Remove or merge alerts that are unactionable, never by
   silently mapping missing data to healthy.

## Verification and evidence

For every enabled rule retain: source/query, signal class, threshold classification, warning/critical
condition, window/pending duration, sample floor, NoData/error/missing-series behavior, severity, blast
radius, owner/contact policy, runbook/action, normal/firing/recovery tests, maintenance behavior, baseline
dataset, tuning decision/date, dashboard link, and sanitized notification example.

Verify application metric names against `MemoriesMeter`, normalized queries against the committed
dashboard, health semantics against `health-checks.md`, and hard targets against the PRD. A dashboard
panel, successful query, missing series, or untested rule is not deployed paging assurance.

## Rollback, recovery, and stop conditions

Alert-rule changes do not mutate application data. Roll back a noisy or broken rule through the alert
platform's reviewed configuration history while keeping the underlying signal/dashboard. Stop rollout if
the rule leaks sensitive/high-cardinality labels, pages without an actionable scope, treats NoData as
healthy contrary to policy, conflicts with a hard target, or cannot be verified.

During an application incident, silence only through the approved incident/maintenance policy with owner,
reason, scope, expiry, and alternate observation. Do not disable telemetry or delete evidence to clear an
alert. Use [Incident Response](./incident-response.md) and [Capacity Planning](./capacity-planning.md) for
service recovery.

## Escalation evidence

Provide alert/review ID, owner, revision/dashboard/query, source/exporter, threshold class, workload/sample
floor, warning/critical/window, NoData/error behavior, labels/cardinality audit, firing/recovery timeline,
health and corroborating backend/workflow evidence, notification route, maintenance/silence state, tuning
history, false-positive/negative assessment, and requested decision. Redact tokens, Secret values, content,
users, cases, memory units, workflow IDs, and unrelated tenant details.

## Related runbooks and sources

- [Incident Response](./incident-response.md)
- [Capacity Planning](./capacity-planning.md)
- [Deployment Configuration](./deployment-configuration.md)
- [Failure Recovery](./failure-recovery.md)
- [Rate Limiting](./rate-limiting.md)
- [Pipeline Persistence](./pipeline-persistence.md)
- [Health Checks](../dev/health-checks.md)
- [Telemetry](../dev/telemetry.md)
- [`MemoriesMeter`](../../src/Hexalith.Memories.Telemetry/MemoriesMeter.cs)
- [Committed Grafana dashboard](../../deploy/grafana/dashboards/memories-operability.json)
- [Prometheus alerting practices](https://prometheus.io/docs/practices/alerting/)
- [Grafana missing-data handling](https://grafana.com/docs/grafana/latest/alerting/guides/missing-data/)
