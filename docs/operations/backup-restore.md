# Backup & Restore (Story 26.2)

This runbook documents how to back up and restore Hexalith.Memories data. It closes the feature portion of
audit finding A25 (missing restore path) and reinforces NFR16 (zero memory-unit loss). It covers two
complementary layers:

- **Logical backup** — a portable JSON snapshot produced by the export endpoints and consumed by the new
  import/restore endpoints. Survives cluster migrations and schema-compatible upgrades.
- **Physical backup** — the Redis and FalkorDB append-only files (AOF) plus the persistent-volume (PVC)
  snapshots that back a same-cluster recovery.

Restore fidelity is proved end to end by the integration test
`tests/Hexalith.Memories.IntegrationTests/Restore/BackupRestoreFidelityIntegrationTests.cs` (AC7).

Cross-links: [deployment-configuration.md](./deployment-configuration.md) (topology, PVCs, AOF enforcement),
[pipeline-persistence.md](./pipeline-persistence.md) (AOF/NFR16 durability), [disaster-recovery.md](./disaster-recovery.md)
(pod/cluster-loss recovery), [failure-recovery.md](./failure-recovery.md) (re-ingestion of failed units).

## What is (and is not) captured

The export is a **logical** snapshot, not a byte-image of Redis. Restore fidelity is defined per data family:

| Data | Redis/graph object | On restore | Fidelity |
|------|--------------------|-----------|----------|
| Memory unit (syntactic) | `{tenantId}:mu:{id}` (HASH) | Written verbatim from the export | Field-for-field equal |
| Case record | `{tenantId}:case:{id}` (HASH) | Written verbatim from the export | Field-for-field equal |
| Case members | `{tenantId}:case:{id}:members` (HASH) | Written verbatim from the export | Member set + types equal |
| Graph nodes + edges | per-tenant FalkorDB graph | Nodes from export; edges from `edges[]`; CONTAINS rebuilt from `caseId` | Every edge `(source, target, type, confidence, origin, verifiedBy, previousConfidence)` equal |
| Semantic vectors | `{tenantId}:vec:{id}:{seq}` (HASH) | **Re-derived** (re-embedded) | Present + dimensions match; byte-equal under a deterministic provider |
| NL vectors | `{tenantId}:vecnl:{id}` (HASH) | **Not re-derived** by restore | Rebuilt on next re-index/event replay (see note) |

> **Why vectors are re-derived, not copied.** The export JSON does **not** contain embedding vectors or
> natural-language (NL) descriptions — only the `(provider, model, dimensions)` attribution. The restore
> workflow re-embeds each unit's content with the **target tenant's configured provider**, reproducing the
> chunked `{tenantId}:vec:{id}:{seq}` hashes. Under a deterministic provider the re-derived vectors are
> byte-identical to the originals; under a non-deterministic hosted provider they are semantically equivalent
> but not bit-identical.

> **NL vectors (decision D1c).** NL descriptions are AI-generated (non-deterministic LLM) and exist only for
> event-sourced units. Restore does not regenerate them; they are rebuilt on the next re-index / event replay.
> For file-sourced corpora there are no NL vectors to lose.

> **Secrets are never in the export.** Only secret-store **key names** (`apiSecretKeyName`) travel in the
> snapshot — never secret values. The target tenant's embedding secret must already exist in the target secret
> store before restore.

## Logical backup (export)

Export produces the portable JSON envelope (schema version 1, `X-Export-Schema-Version: 1`).

```bash
# Tenant-scoped export (all cases)
curl -fsS -H "Authorization: Bearer $TOKEN" \
  "https://$MEMORIES_HOST/api/v1/tenants/$TENANT/export" -o "$TENANT-export.json"

# Case-scoped export
curl -fsS -H "Authorization: Bearer $TOKEN" \
  "https://$MEMORIES_HOST/api/v1/tenants/$TENANT/cases/$CASE/export" -o "$TENANT-$CASE-export.json"
```

The `MemoriesClient.ExportTenantAsync` / `ExportCaseAsync` methods stream the same envelope for programmatic
backups. Store snapshots encrypted at rest; they contain memory content but no provider secrets.

## Physical backup (Redis + FalkorDB AOF / PVC)

Only two workloads hold durable state (the Server, MCP, and Dapr sidecars are stateless — no PVC):

| Workload | Mount | PVC size | Persistence |
|----------|-------|----------|-------------|
| Redis Stack | `/data` | `20Gi` | AOF (`appendonly yes`, `appendfsync everysec`, `aof-use-rdb-preamble yes`) + RDB save points |
| FalkorDB | `/var/lib/falkordb/data` | `10Gi` | AOF via `FALKORDB_PERSISTENCE_ARGS=--appendonly yes ...` |

AOF configuration is **repo-owned and already enforced** (`deploy/redis/redis.conf`;
`AppHost/Program.cs` throws if `appendonly yes` is absent; `deploy/kubernetes/base/kustomization.yaml`
`FALKORDB_PERSISTENCE_ARGS`). Do **not** re-add it — see [deployment-configuration.md](./deployment-configuration.md).

Physical backup options, in order of preference:

1. **PVC volume snapshot (primary).** Use your CSI driver's `VolumeSnapshot` on the Redis (`20Gi`) and
   FalkorDB (`10Gi`) PVCs. Snapshot both together to keep the data plane and graph consistent.

   ```bash
   kubectl -n memories apply -f - <<'EOF'
   apiVersion: snapshot.storage.k8s.io/v1
   kind: VolumeSnapshot
   metadata: { name: redis-data-snap, namespace: memories }
   spec: { volumeSnapshotClassName: csi-snapclass, source: { persistentVolumeClaimName: redis-data } }
   EOF
   ```

2. **File-level backup (portable).** Trigger a background save, then copy the AOF/RDB artifacts off the PVC.

   ```bash
   # Redis
   kubectl -n memories exec redis-0 -- redis-cli BGSAVE
   kubectl -n memories cp redis-0:/data ./redis-backup

   # FalkorDB (originates a repo procedure — see Disaster Recovery for full detail)
   kubectl -n memories exec falkordb-0 -- redis-cli BGSAVE
   kubectl -n memories cp falkordb-0:/var/lib/falkordb/data ./falkordb-backup
   ```

## Restore procedure

Restore consumes the **exact** export envelope and runs as a durable Dapr workflow (`RestoreWorkflow`) so a
large restore that re-embeds every unit is resumable, retried, and observable. Story 26.2 scopes restore to
**same-tenant-id disaster recovery** (decision D2): the export is restored into a tenant with the same id.

### Prerequisites

- The target tenant is **provisioned and Active** (RediSearch + vector indexes + FalkorDB graph created by
  `TenantProvisioningWorkflow`). Restore verifies index readiness and refuses to write hashes that would be
  unsearchable.
- The target tenant's embedding config matches the export's `(provider, model, dimensions)`. Restore
  re-embeds with the **target** tenant's provider — a mismatched dimension fails readiness verification.
- The embedding provider **secret** named by `apiSecretKeyName` exists in the target secret store.
- The export JSON is available and its `manifest.tenantId` equals the target tenant id.

### Procedure

```bash
# Tenant restore — returns 202 Accepted + a Location for the restore status.
curl -fsS -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data-binary "@$TENANT-export.json" \
  -D - "https://$MEMORIES_HOST/api/v1/tenants/$TENANT/import"

# Poll the restore status until "completed".
curl -fsS -H "Authorization: Bearer $TOKEN" \
  "https://$MEMORIES_HOST/api/v1/tenants/$TENANT/restore/$INSTANCE_ID"
```

The case route is symmetric: `POST /api/v1/tenants/{tenantId}/cases/{caseId}/import`.

Restore phases (surfaced via the status `status` field): `restoring-data-plane` → `reindexing` →
`cleaning-up` → `completed`. Restore is **idempotent** (Redis `HSET` overwrite + graph `MERGE`), so a retried
or resumed restore converges to the same state.

### Rejections (400)

| Code | Meaning |
|------|---------|
| `IMPORT_SCHEMA_VERSION_UNSUPPORTED` | The envelope's `manifest.schemaVersion` is not `1`. |
| `IMPORT_SCOPE_MISMATCH` | Tenant JSON posted to the case route (or vice-versa). |
| `IMPORT_TENANT_MISMATCH` | `manifest.tenantId` ≠ the target tenant (cross-tenant remap is out of scope). |
| `IMPORT_CASE_MISMATCH` | `manifest.caseId` ≠ the target case. |
| `IMPORT_TOO_LARGE` (413) | Body exceeds the 512 MB ceiling — restore case-by-case (see Scale note). |

### Verification

```bash
# Memory-unit hashes restored
kubectl -n memories exec redis-0 -- redis-cli --scan --pattern "$TENANT:mu:*" | wc -l

# Semantic vectors re-derived
kubectl -n memories exec redis-0 -- redis-cli --scan --pattern "$TENANT:vec:*" | wc -l

# Graph edges restored (per tenant graph)
kubectl -n memories exec falkordb-0 -- redis-cli GRAPH.QUERY "$TENANT" "MATCH ()-[r]->() RETURN count(r)"
```

A search against the restored tenant should return results (confirms the re-derived vectors are indexed).

### Rollback

Restore is additive and idempotent; it does not delete data. To abandon a restore, stop before searching and
either delete the tenant (`DELETE /api/v1/tenants/{tenantId}` — tenant deletion workflow) and re-provision, or
restore a known-good earlier snapshot over the top (MERGE/overwrite converges).

## Scale note (decision D5)

The current staging path buffers the import body once (bounded by a documented **512 MB** `RequestSizeLimit`
ceiling). A tenant export of ~100K units is ≈500 MB. For corpora beyond the ceiling, restore case-by-case; a
streaming/chunked staging store is the documented follow-up.
