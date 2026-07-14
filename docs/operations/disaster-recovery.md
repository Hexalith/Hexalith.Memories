# Disaster Recovery (Story 26.2)

This runbook gives the executable recovery path for the three durable-state loss scenarios in a
Hexalith.Memories deployment: **Redis-pod loss**, **FalkorDB-pod loss**, and **full-cluster loss**. It builds
on the Story 26.1 production topology and the AOF/restart durability guarantees, and it uses the logical
export → import restore path documented in [backup-restore.md](./backup-restore.md).

Only two workloads hold durable data (Server, MCP, and Dapr sidecars are stateless — no PVC): Redis Stack
(`20Gi` at `/data`) and FalkorDB (`10Gi` at `/var/lib/falkordb/data`). See
[deployment-configuration.md](./deployment-configuration.md) for the full topology, PVCs, and components.

Cross-links: [backup-restore.md](./backup-restore.md) (backup + restore procedure),
[deployment-configuration.md](./deployment-configuration.md) (Story 26.1 topology),
[pipeline-persistence.md](./pipeline-persistence.md) (AOF/NFR16 durability evidence),
[failure-recovery.md](./failure-recovery.md) (re-ingestion of failed units),
[incident-response.md](./incident-response.md) (incident command), [index-rebuild.md](./index-rebuild.md)
(supported rebuild decisions), and [upgrade-migration.md](./upgrade-migration.md) (upgrade rollback boundaries).

## Recovery evidence (what backs the guarantees below)

- **AOF durability (NFR16).** Redis and FalkorDB run append-only. Redis: `deploy/redis/redis.conf`
  (`appendonly yes`, `appendfsync everysec`, `aof-use-rdb-preamble yes`, `maxmemory-policy noeviction`);
  `AppHost/Program.cs` throws if the file is missing or lacks `appendonly yes`. FalkorDB:
  `deploy/kubernetes/base/kustomization.yaml` `FALKORDB_PERSISTENCE_ARGS=--appendonly yes ...`. **Do not
  re-add AOF config — cite it.**
- **Restart durability, proven.** `Ingestion/PipelinePersistenceIntegrationTests.RestartTopology_ShouldPreserveIndexedRedisBackedDataAcrossControlledRestart`
  restarts the topology against the named Redis volume and asserts the syntactic/semantic/dedup keys and
  `Indexed` status survive **with zero memory-unit loss** (no lost and no duplicate `{tenantId}:mu:*` key).
- **Export → import fidelity, proven.** `Restore/BackupRestoreFidelityIntegrationTests.ExportThenImport_RestoresEveryHashAndEdge`
  snapshots the stores, exports, wipes the data plane, restores, and asserts every syntactic/case/members hash
  and every graph edge round-trips with re-derived vectors present.

> **Recovery time hierarchy.** Prefer, in order: (1) pod reschedule onto the **same PVC** (fastest — AOF
> replays, no data movement); (2) **PVC volume-snapshot** restore (same-cluster point-in-time); (3) **logical
> export → import** restore (cross-cluster / last resort — re-embeds every unit, so it is the slowest).

## Scenario 1 — Redis-pod loss (PVC intact)

The common case: the Redis pod is rescheduled but its `20Gi` PVC survives.

```bash
# 1. Confirm the PVC is Bound and will re-attach.
kubectl -n hexalith-memories get pvc data-redis-stack-0
# 2. Let the StatefulSet reschedule the pod; AOF replays from /data on start.
kubectl -n hexalith-memories delete pod redis-stack-0 # only with incident-command approval
kubectl -n hexalith-memories rollout status statefulset/redis-stack
# 3. Verify AOF replayed and data is present.
kubectl -n hexalith-memories exec redis-stack-0 -- \
  sh -ec 'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning INFO persistence | grep -E "^(loading|aof_enabled|aof_last_write_status):"'
kubectl -n hexalith-memories exec redis-stack-0 -- \
  sh -ec 'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning DBSIZE'
```

If the PVC is **lost**, restore a PVC volume snapshot (see backup-restore.md), or fall back to logical
restore (Scenario 3) from the latest export.

## Scenario 2 — FalkorDB-pod loss (originates the FalkorDB backup/restore procedure)

FalkorDB is a Redis-module server persisting to `/var/lib/falkordb/data` (AOF + RDB). No committed operator
procedure existed before this story; this section originates it.

**Pod reschedule (PVC intact):**

```bash
kubectl -n hexalith-memories get pvc data-falkordb-0
kubectl -n hexalith-memories rollout status statefulset/falkordb
# AOF replays on start; verify the per-tenant graphs exist and hold nodes.
kubectl -n hexalith-memories exec falkordb-0 -- \
  sh -ec 'redis-cli -a "$FALKORDB_PASSWORD" --no-auth-warning GRAPH.LIST'
kubectl -n hexalith-memories exec falkordb-0 -- \
  sh -ec 'redis-cli -a "$FALKORDB_PASSWORD" --no-auth-warning GRAPH.QUERY "$1" "MATCH (n) RETURN count(n)"' -- "$TENANT"
```

**FalkorDB physical backup (take on a schedule):**

```bash
# Freeze intake and drain in-flight workflows before this save.
kubectl -n hexalith-memories exec falkordb-0 -- \
  sh -ec 'redis-cli -a "$FALKORDB_PASSWORD" --no-auth-warning BGSAVE'
kubectl -n hexalith-memories exec falkordb-0 -- \
  sh -ec 'until redis-cli -a "$FALKORDB_PASSWORD" --no-auth-warning INFO persistence | grep -q "rdb_bgsave_in_progress:0"; do sleep 2; done; redis-cli -a "$FALKORDB_PASSWORD" --no-auth-warning INFO persistence | grep -q "rdb_last_bgsave_status:ok"'
kubectl -n hexalith-memories exec falkordb-0 -- \
  sh -ec 'redis-cli -a "$FALKORDB_PASSWORD" --no-auth-warning INFO persistence | grep -E "^(aof_enabled|aof_last_write_status|aof_last_bgrewrite_status):"'
```

Take a verified `VolumeSnapshot` of `data-falkordb-0` while intake remains frozen, or copy artifacts from a
read-only mount of a quiesced snapshot. Never copy the live AOF directory. Pair the FalkorDB snapshot ID and
timestamp with the Redis snapshot ID and timestamp for the same recovery point; resume intake only after both
backups pass metadata, checksum, and restore-readiness verification.

**FalkorDB physical restore (PVC lost):**

```bash
# 1. Scale FalkorDB down so the data dir is quiescent.
kubectl -n hexalith-memories scale statefulset/falkordb --replicas=0
# 2. Provision a fresh 10Gi PVC (or restore its VolumeSnapshot), then stage the backup into /var/lib/falkordb/data
#    (dump.rdb + appendonlydir) using an init job or `kubectl cp` into a maintenance pod that mounts the PVC.
# 3. Scale back up; FalkorDB loads dump.rdb / replays AOF on start.
kubectl -n hexalith-memories scale statefulset/falkordb --replicas=1
kubectl -n hexalith-memories rollout status statefulset/falkordb
kubectl -n hexalith-memories exec falkordb-0 -- \
  sh -ec 'redis-cli -a "$FALKORDB_PASSWORD" --no-auth-warning GRAPH.QUERY "$1" "MATCH ()-[r]->() RETURN count(r)"' -- "$TENANT"
```

If no FalkorDB backup exists, the graph can be **rebuilt by logical restore** (Scenario 3): restore re-MERGEs
every node and edge (and rebuilds CONTAINS from each unit's `caseId`).

## Scenario 3 — Full-cluster loss (cross-cluster recovery)

Both PVCs are gone; recover onto a fresh cluster from the latest **logical export** (same tenant ids).
Obtain `$TOKEN` through the approved identity workflow and keep shell tracing disabled; never print the token.

1. **Redeploy the topology** on the new cluster:
   `kubectl apply -k deploy/kubernetes/overlays/production` (Server + MCP Deployments; Redis Stack + FalkorDB
   StatefulSets with fresh PVCs; the 4 Dapr components; `secretstores.kubernetes`). See
   [deployment-configuration.md](./deployment-configuration.md).
2. **Restore embedding secrets** into the secret store (they are **not** in the export — only their key
   names). Each tenant's `apiSecretKeyName` must resolve.
3. **Provision each tenant** with the **same id** and the **same embedding `(provider, model, dimensions)`**
   the export was taken with (`POST /api/v1/tenants` → wait for `Active`).
4. **Restore each tenant** from its export (see [backup-restore.md](./backup-restore.md)):

   ```bash
   for f in exports/*-export.json; do
     tenant="$(jq -r .manifest.tenantId "$f")"
     curl -fsS -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
       --data-binary "@$f" "https://$MEMORIES_HOST/api/v1/tenants/$tenant/import"
   done
   ```

5. **Verify** memory-unit hash counts, semantic-vector counts, and graph-edge counts against the export
   `statistics`, and confirm search returns results (verification queries in
   [backup-restore.md](./backup-restore.md)).

## Caveats (hardening carried from Story 26.1 review)

- Redis and FalkorDB currently run as **root** (no `runAsNonRoot`; `fsGroup`/PVC-permission hardening remains
  an explicitly tracked gap). Ensure restored PVCs keep data-dir ownership compatible with the container.
- There are **no NetworkPolicies** yet. On a rebuilt cluster, restrict data-plane access to the Server/MCP
  pods as part of hardening. These fixes belong to a dedicated hardening story, not to a recovery run.
