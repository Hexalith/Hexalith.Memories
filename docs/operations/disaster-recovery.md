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
[failure-recovery.md](./failure-recovery.md) (re-ingestion of failed units).

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
kubectl -n memories get pvc redis-data
# 2. Let the StatefulSet reschedule the pod; AOF replays from /data on start.
kubectl -n memories delete pod redis-0        # (only if stuck; normally auto-reschedules)
kubectl -n memories rollout status statefulset/redis
# 3. Verify AOF replayed and data is present.
kubectl -n memories exec redis-0 -- redis-cli DBSIZE
kubectl -n memories exec redis-0 -- redis-cli --scan --pattern "*:mu:*" | head
```

If the PVC is **lost**, restore a PVC volume snapshot (see backup-restore.md), or fall back to logical
restore (Scenario 3) from the latest export.

## Scenario 2 — FalkorDB-pod loss (originates the FalkorDB backup/restore procedure)

FalkorDB is a Redis-module server persisting to `/var/lib/falkordb/data` (AOF + RDB). No committed operator
procedure existed before this story; this section originates it.

**Pod reschedule (PVC intact):**

```bash
kubectl -n memories get pvc falkordb-data
kubectl -n memories rollout status statefulset/falkordb
# AOF replays on start; verify the per-tenant graphs exist and hold nodes.
kubectl -n memories exec falkordb-0 -- redis-cli GRAPH.LIST
kubectl -n memories exec falkordb-0 -- redis-cli GRAPH.QUERY "$TENANT" "MATCH (n) RETURN count(n)"
```

**FalkorDB physical backup (take on a schedule):**

```bash
# Trigger a background save (writes dump.rdb + appendonlydir under the data dir).
kubectl -n memories exec falkordb-0 -- redis-cli BGSAVE
kubectl -n memories exec falkordb-0 -- redis-cli INFO persistence | grep -E "aof_enabled|rdb_last_save_time"
# Copy the data directory off the PVC (or snapshot the 10Gi PVC).
kubectl -n memories cp falkordb-0:/var/lib/falkordb/data ./falkordb-backup
```

**FalkorDB physical restore (PVC lost):**

```bash
# 1. Scale FalkorDB down so the data dir is quiescent.
kubectl -n memories scale statefulset/falkordb --replicas=0
# 2. Provision a fresh 10Gi PVC (or restore its VolumeSnapshot), then stage the backup into /var/lib/falkordb/data
#    (dump.rdb + appendonlydir) using an init job or `kubectl cp` into a maintenance pod that mounts the PVC.
# 3. Scale back up; FalkorDB loads dump.rdb / replays AOF on start.
kubectl -n memories scale statefulset/falkordb --replicas=1
kubectl -n memories rollout status statefulset/falkordb
kubectl -n memories exec falkordb-0 -- redis-cli GRAPH.QUERY "$TENANT" "MATCH ()-[r]->() RETURN count(r)"
```

If no FalkorDB backup exists, the graph can be **rebuilt by logical restore** (Scenario 3): restore re-MERGEs
every node and edge (and rebuilds CONTAINS from each unit's `caseId`).

## Scenario 3 — Full-cluster loss (cross-cluster recovery)

Both PVCs are gone; recover onto a fresh cluster from the latest **logical export** (same tenant ids).

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

- Redis and FalkorDB currently run as **root** (no `runAsNonRoot`; an `fsGroup`/PVC-permission hardening TODO
  remains). Ensure restored PVCs keep data-dir ownership compatible with the container.
- There are **no NetworkPolicies** yet. On a rebuilt cluster, restrict data-plane access to the Server/MCP
  pods as part of hardening. These fixes belong to a dedicated hardening story, not to a recovery run.
