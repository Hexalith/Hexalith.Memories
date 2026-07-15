# Capacity Planning

## Purpose and scope

Owner: platform operations. Review cadence: quarterly and after any embedding model, index schema,
resource request, persistence, or topology change. Last verified: 2026-07-14 at repository revision
`1553ee6708f644f3a4bc3638d3aaceed682b2371`.

This runbook provides the measurement-first sizing method required by NFR14. It applies to one
tenant, a representative case/corpus, Redis Stack, FalkorDB, workflow/state data, and the Server/MCP
workloads. The blast radius of measurement load is the selected tenant and shared backend capacity;
an overloaded single-replica backend can affect all tenants. Run a production measurement only with
the tenant owner and platform operator's approval and within an agreed load window.

The committed Kubernetes values are bootstrap configuration, not capacity recommendations:

| Workload | CPU request / limit | Memory request / limit | Durable volume |
|---|---:|---:|---:|
| Server application | 500m / 2 | 512Mi / 2Gi | none |
| Server Dapr sidecar | 250m / 1 | 256Mi / 512Mi | none |
| MCP application | 100m / 500m | 128Mi / 512Mi | none |
| MCP Dapr sidecar | 100m / 500m | 128Mi / 256Mi | none |
| Redis Stack | 500m / 2 | 1Gi / 4Gi | 20Gi |
| FalkorDB | 500m / 2 | 1Gi / 4Gi | 10Gi |

The source is [`deploy/kubernetes/base`](../../deploy/kubernetes/base/), including the application
Deployments and backend StatefulSets. No HorizontalPodAutoscaler is committed. Redis Stack and
FalkorDB each run one replica; the topology must not be projected as linearly scalable or highly
available without measured evidence and a separately reviewed architecture change.

## Prerequisites and authorization

- Approvals: tenant owner for representative data/load, platform operator for cluster/backend
  observation, and capacity owner for any scale action.
- Use a representative tenant with known vector dimensions, content-size distribution, case/edge
  density, and chunk count. Do not use another tenant's data as a proxy.
- Confirm the namespace, pod names, index names, and non-secret scope before collection:

  ```bash
  NAMESPACE=hexalith-memories
  TENANT_ID="${TENANT_ID:-capacity-canary}"
  REDIS_POD=redis-stack-0
  FALKORDB_POD=falkordb-0
  SYNTACTIC_INDEX="${TENANT_ID}:memories:idx"
  SEMANTIC_INDEX="${TENANT_ID}:memories:vec"
  RUN_ID="$(date -u +%Y%m%dT%H%M%SZ)"
  printf 'namespace=%s tenant=%s run=%s\n' "$NAMESPACE" "$TENANT_ID" "$RUN_ID"
  ```

- Confirm the in-container `REDIS_PASSWORD` and `FALKORDB_PASSWORD` references exist without printing
  their values. Do not read Secret manifests into logs; base64 is encoding, not encryption.
- Establish an observation window that includes normal load, the representative ingestion load, and
  at least one scheduled or safely induced persistence rewrite. Do not induce a rewrite during an
  incident or when write headroom is already constrained.
- Destructive steps: none are required. Scaling, PVC expansion, model changes, eviction-policy
  changes, index replacement, and data deletion are outside this measurement procedure and require a
  separate approved change.

## Signals and evidence

Capture a timestamped, access-controlled evidence bundle with secrets and content redacted:

- Kubernetes CPU/memory working set, throttling, restarts/OOM events, pod limits, PVC capacity/used
  bytes, and node/storage pressure for the whole observation window.
- Redis `INFO memory` and `INFO persistence`; representative `MEMORY USAGE` samples; `FT.INFO` for
  syntactic, semantic, and natural-language indexes, including `vector_index_sz_mb`,
  `total_index_memory_sz_mb`, indexed document counts, and indexing failures.
- FalkorDB `GRAPH.MEMORY USAGE <graph> [SAMPLES n]`, especially `total_graph_sz_mb`,
  `indices_sz_mb`, and the node/edge/matrix breakdown. Higher sample counts cost more computation;
  use a moderate count during normal service and reserve detailed sampling for a maintenance window.
- Tenant model/provider, configured vector dimensions, source-unit count, stored-vector/chunk count,
  average and percentile content/metadata sizes, graph nodes/edges, workflow/state counts, AOF/RDB
  sizes, and backup/snapshot size.
- Application metrics and progress: `memories.ingestion.documents`,
  `memories.ingestion.failures`, `memories.pipeline.queue.depth`, and `memories.index.size`.

`memories.index.size` is a document-count gauge, not a byte measurement. Never use it as Redis or PVC
memory. Redis `maxmemory-policy noeviction` protects resident data by rejecting writes under pressure;
it does not make an undersized deployment safe.

### Worksheet

Label every input as `measured`, `configured`, `formula-derived`, or `assumed`:

| Input/result | Classification | Calculation or source |
|---|---|---|
| Vector dimensions `D` | configured | tenant embedding configuration |
| Stored-vector count `V` | measured | chunk-vector hashes, not source-document count |
| Raw float32 payload | formula-derived | `4 × D × V` bytes |
| Hash fields, metadata, content/chunk text | measured | sampled `MEMORY USAGE`, not a fixed multiplier |
| HNSW/RediSearch indexes | measured | `FT.INFO` memory fields and deltas |
| FalkorDB graph/indexes | measured | `GRAPH.MEMORY USAGE` component deltas |
| Workflow, actor, dedup, failure, and case state | measured | Redis key-family/PVC deltas |
| AOF/RDB and rewrite amplification | measured | `INFO persistence`, filesystem/PVC deltas |
| Allocator fragmentation | measured | `used_memory_rss`, `used_memory`, fragmentation fields |
| Replication/backup allowance | assumed until measured | current topology has no backend replica; record backup retention separately |
| Safety headroom | assumed then verified | capacity-owner policy validated under peak load/rewrite |

The float32 formula is a lower bound only. It excludes Redis hash/object overhead, stored content and
metadata, chunk text, HNSW structures, graph data, workflow/actor state, allocator fragmentation,
persistence copy-on-write/rewrite pressure, backups, and any future replication.

## Procedure

### 1. Freeze the baseline

1. Record tenant dimensions and a representative corpus manifest: units, bytes, chunks, vectors,
   cases, graph nodes/edges, provider quota, and the time window.
2. Capture the rendered resource/PVC configuration and current utilization:

   ```bash
   kubectl -n "$NAMESPACE" get deploy/memories deploy/memories-mcp \
     statefulset/redis-stack statefulset/falkordb -o yaml
   kubectl -n "$NAMESPACE" top pod
   kubectl -n "$NAMESPACE" get pvc data-redis-stack-0 data-falkordb-0
   ```

3. Capture backend baseline through the password already injected into each container:

   ```bash
   kubectl -n "$NAMESPACE" exec "$REDIS_POD" -- sh -ec \
     'export REDISCLI_AUTH="$REDIS_PASSWORD"; redis-cli --no-auth-warning INFO memory; redis-cli --no-auth-warning INFO persistence'
   kubectl -n "$NAMESPACE" exec "$REDIS_POD" -- sh -ec \
     'export REDISCLI_AUTH="$REDIS_PASSWORD"; redis-cli --no-auth-warning FT.INFO "$1"' sh "$SYNTACTIC_INDEX"
   kubectl -n "$NAMESPACE" exec "$REDIS_POD" -- sh -ec \
     'export REDISCLI_AUTH="$REDIS_PASSWORD"; redis-cli --no-auth-warning FT.INFO "$1"' sh "$SEMANTIC_INDEX"
   kubectl -n "$NAMESPACE" exec "$FALKORDB_POD" -- sh -ec \
     'export REDISCLI_AUTH="$FALKORDB_PASSWORD"; redis-cli --no-auth-warning GRAPH.MEMORY USAGE "$1" SAMPLES 100' sh "$TENANT_ID"
   ```

### 2. Load a representative increment

1. Ingest a recorded increment using the normal authenticated ingestion path. Keep source-size and
   chunk-count distributions representative; the extraction concurrency gate is process-local, so
   do not extrapolate one Server pod's saturation linearly across replicas.
2. Observe provider rate/quota signals and queue progress. A provider quota limit can be the capacity
   boundary before CPU, memory, or PVC capacity.
3. Stop the load if readiness becomes `Unhealthy`, ingestion stops making progress, error rates rise
   outside the approved baseline, `noeviction` rejects writes, a backend approaches its hard limit,
   or another tenant shows measurable degradation.

### 3. Measure deltas

1. Repeat all baseline captures at a stable post-ingestion point.
2. Sample representative keys without exposing stored content:

   ```bash
   kubectl -n "$NAMESPACE" exec "$REDIS_POD" -- sh -ec \
     'export REDISCLI_AUTH="$REDIS_PASSWORD"; key=$(redis-cli --no-auth-warning --scan --pattern "$1:mu:*" | head -n 1); test -n "$key"; redis-cli --no-auth-warning MEMORY USAGE "$key"' sh "$TENANT_ID"
   kubectl -n "$NAMESPACE" exec "$REDIS_POD" -- sh -ec \
     'export REDISCLI_AUTH="$REDIS_PASSWORD"; key=$(redis-cli --no-auth-warning --scan --pattern "$1:vec:*" | head -n 1); test -n "$key"; redis-cli --no-auth-warning MEMORY USAGE "$key"' sh "$TENANT_ID"
   ```

3. Calculate per-unit and per-tenant deltas independently:
   `resource delta / added source units`, `resource delta / added stored vectors`, and the absolute
   tenant delta. Do not divide graph growth only by vector count; retain graph node/edge density.
4. Compare the measured vector/hash/index total with `4 × D × V`. The difference is measured
   overhead, not evidence that the formula is wrong.

### 4. Exercise persistence headroom

During an approved window, observe a naturally scheduled or explicitly approved `BGSAVE`/AOF rewrite.
Record `rdb_bgsave_in_progress`, `rdb_last_bgsave_status`, `aof_rewrite_in_progress`,
`aof_last_bgrewrite_status`, RSS/used-memory gap, CPU, latency, and PVC growth through completion.
Do not copy a live mutating AOF directory. Use the coordinated procedure in
[Backup and Restore](./backup-restore.md).

### 5. Project and decide

Project the measured workload mix, growth horizon, persistence/restore workspace, and backup retention.
Use evidence-based triggers rather than the bootstrap values:

- scale or expand before forecast demand consumes the approved CPU, memory, provider-quota, or PVC
  headroom during peak ingestion and persistence rewrite;
- stop onboarding when the forecast cannot preserve recovery workspace and the approved observation
  margin;
- redesign/partition rather than linearly extrapolate when single-replica latency, write throughput,
  failover, or restore time is the limiting factor.

## Verification and evidence

Capacity evidence is acceptable only when:

- before/after measurements use the same tenant, workload definition, windows, and command versions;
- unit, vector/chunk, byte, graph, workflow, and persistence counts are recorded together;
- all worksheet values have a classification and source;
- peak ingestion plus persistence rewrite remains within the capacity owner's headroom policy;
- `/ready` JSON is parsed and affected capabilities remain within the approved test boundary;
- search latency/ingestion throughput and a second tenant's control measurements do not regress beyond
  their accepted baseline;
- the evidence records uncertainty and does not claim backend redundancy or linear scaling.

Retain the rendered manifest digest, timestamps, sanitized command output, metrics export, corpus
manifest, worksheet, stop/scale decision, approvers, and follow-up owner. Remove credentials, source
content, user/case identifiers, and unbounded tenant labels from shared evidence.

## Rollback, recovery, and stop conditions

This procedure does not mutate configuration. Stop load generation first, allow already accepted
workflows to drain, and verify queue progress and `/ready`. If the measurement causes pressure, preserve
evidence and use [Incident Response](./incident-response.md); do not change eviction policy or delete
keys. Recover persistent data only through [Backup and Restore](./backup-restore.md) or
[Disaster Recovery](./disaster-recovery.md).

If a later approved capacity change fails, roll back stateless resources to the previous rendered
manifest. A Pod-template rollback does not reverse PVC expansion, index/schema changes, or durable data;
use the change's retained backup and recovery plan.

## Escalation evidence

Escalate to the platform/data owners when headroom is below policy, writes are rejected, persistence
rewrite fails, restore time exceeds the recovery objective, or tenant isolation/performance is affected.
Provide revision/render digest, non-secret scope, workload manifest, before/after worksheet, Kubernetes
and backend measurements, health JSON, correlation/workflow identifiers, stop decision, and the latest
verified paired-backend backup identities. Never attach Secret values or source memory content.

## Related runbooks and sources

- [Deployment Configuration](./deployment-configuration.md)
- [Monitoring and Alerting Thresholds](./monitoring-alerting-thresholds.md)
- [Incident Response](./incident-response.md)
- [Backup and Restore](./backup-restore.md)
- [Disaster Recovery](./disaster-recovery.md)
- [Pipeline Persistence](./pipeline-persistence.md)
- [Rate Limiting](./rate-limiting.md)
- [Telemetry](../dev/telemetry.md)
- [`IndexSchemaDefinitions`](../../src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs)
- [Production Kubernetes base](../../deploy/kubernetes/base/)
