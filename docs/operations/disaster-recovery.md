# Disaster Recovery (Story 26.2)

This runbook gives the recovery path for Redis pod/PVC loss, FalkorDB pod/PVC loss, and full-cluster loss.
Only Redis Stack (`20Gi` at `/data`) and FalkorDB (`10Gi` at `/var/lib/falkordb/data`) hold durable state.
Server, MCP, and Dapr sidecars are stateless.

Cross-links: [backup-restore.md](./backup-restore.md) (policy, backup, logical restore, and verifier),
[deployment-configuration.md](./deployment-configuration.md) (production topology),
[pipeline-persistence.md](./pipeline-persistence.md) (controlled-restart durability),
[failure-recovery.md](./failure-recovery.md) (failed-unit re-ingestion),
[incident-response.md](./incident-response.md) (incident command), and
[tenant-onboarding-offboarding.md](./tenant-onboarding-offboarding.md) (same-id clean-target lifecycle).

## Prerequisites and authorization

Incident command must approve every destructive step. Before recovery, establish:

- the loss boundary (pod only, PVC only, or cluster) and the last known-good recovery-point ID;
- verified Redis/FalkorDB snapshot handles or logical-export checksums from the same approved recovery point;
- the deployment-supplied quiescence/resume playbook, RPO, retention, immutable off-cluster location, owner,
  CSI `VolumeSnapshotClass`, and restore `StorageClass` required by
  [Backup and Restore](./backup-restore.md#backup-policy-prerequisites-and-authorization);
- `kubectl`, `jq`, `python3`, and `memories`, plus permission to scale StatefulSets and create/delete the
  same-name PVCs owned by their `volumeClaimTemplates`;
- restored Redis/FalkorDB credentials and tenant embedding secret references, without displaying their values.

```bash
set -euo pipefail
: "${MEMORIES_BASE_URL:?set the Memories HTTPS base URL}"
: "${TOKEN:?obtain an approved bearer token}"
: "${TENANT:?set the tenant id}"
: "${NAMESPACE:=hexalith-memories}"
: "${RESTORE_STORAGE_CLASS:?set the approved restore StorageClass}"
: "${RECOVERY_ID:?select an approved recovery-point identifier}"
EVIDENCE_DIR="${EVIDENCE_DIR:-$PWD/recovery-evidence/$RECOVERY_ID}"
mkdir -p "$EVIDENCE_DIR"
```

Stop when snapshot identity/readiness, source checksums, tenant scope, authorization, or quiescence evidence is
missing or contradictory. Keep intake paused until post-recovery verification succeeds.

## Recovery evidence

- Redis uses `appendonly yes`, `appendfsync everysec`, and `aof-use-rdb-preamble yes` from
  `deploy/redis/redis.conf`; `src/Hexalith.Memories.AppHost/Program.cs` enforces AOF configuration.
- FalkorDB uses the repo-owned `FALKORDB_PERSISTENCE_ARGS` in
  `deploy/kubernetes/base/kustomization.yaml`.
- `PipelinePersistenceIntegrationTests.RestartTopology_ShouldPreserveIndexedRedisBackedDataAcrossControlledRestart`
  proves controlled Redis restart with no lost or duplicate memory-unit key.
- `BackupRestoreFidelityIntegrationTests.ExportThenImport_RestoresEveryHashAndEdge` proves exact logical
  export/import fidelity, including graph `createdAt` and audit properties plus re-derived semantic chunks.

Recovery order is: same PVC (AOF replay), PVC from snapshot, then logical export/import into a clean target.

## Scenario 1 — Redis pod or PVC loss

### Pod loss with PVC intact

```bash
kubectl -n "$NAMESPACE" get pvc data-redis-stack-0 \
  -o jsonpath='{.status.phase}' | grep -qx Bound
kubectl -n "$NAMESPACE" delete pod redis-stack-0 --ignore-not-found
kubectl -n "$NAMESPACE" rollout status statefulset/redis-stack --timeout=10m
```

Run the verifier in [Verification and evidence](#verification-and-evidence). It requires Redis `loading:0`,
healthy AOF status, tenant-specific counts, and the expected graph state; `DBSIZE` alone is not recovery proof.

### PVC loss with a verified snapshot

The following recreates the exact PVC name consumed by the StatefulSet. `$REDIS_SNAPSHOT` must be the Redis
member of the approved paired recovery point.

```bash
: "${REDIS_SNAPSHOT:=redis-stack-$RECOVERY_ID}"
kubectl -n "$NAMESPACE" get volumesnapshot "$REDIS_SNAPSHOT" \
  -o jsonpath='{.status.readyToUse}' | grep -qx true

kubectl -n "$NAMESPACE" scale statefulset/redis-stack --replicas=0
if kubectl -n "$NAMESPACE" get pod redis-stack-0 >/dev/null 2>&1; then
  kubectl -n "$NAMESPACE" wait --for=delete pod/redis-stack-0 --timeout=10m
fi

# Destructive: incident command must confirm the original PVC is lost/unusable and the snapshot is verified.
kubectl -n "$NAMESPACE" delete pvc data-redis-stack-0 --ignore-not-found
kubectl -n "$NAMESPACE" apply -f - <<EOF
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: data-redis-stack-0
spec:
  storageClassName: $RESTORE_STORAGE_CLASS
  dataSource:
    name: $REDIS_SNAPSHOT
    kind: VolumeSnapshot
    apiGroup: snapshot.storage.k8s.io
  accessModes: [ReadWriteOnce]
  resources:
    requests:
      storage: 20Gi
EOF
kubectl -n "$NAMESPACE" wait \
  --for=jsonpath='{.status.phase}'=Bound pvc/data-redis-stack-0 --timeout=10m
kubectl -n "$NAMESPACE" scale statefulset/redis-stack --replicas=1
kubectl -n "$NAMESPACE" rollout status statefulset/redis-stack --timeout=10m
kubectl -n "$NAMESPACE" exec redis-stack-0 -- stat -c '%u:%g %a %n' /data \
  > "$EVIDENCE_DIR/redis-data-ownership.txt"
```

If the PVC does not bind, the pod does not become ready, or ownership is incompatible, scale Redis to zero,
retain the failed PVC and events for forensics, and retry from the original immutable snapshot with a new PVC
only after incident-command approval. Do not resume intake.

## Scenario 2 — FalkorDB pod or PVC loss

### Pod loss with PVC intact

```bash
kubectl -n "$NAMESPACE" get pvc data-falkordb-0 \
  -o jsonpath='{.status.phase}' | grep -qx Bound
kubectl -n "$NAMESPACE" delete pod falkordb-0 --ignore-not-found
kubectl -n "$NAMESPACE" rollout status statefulset/falkordb --timeout=10m
```

### Physical backup and PVC restore

The paired backup procedure in [Backup and Restore](./backup-restore.md#physical-backup-redis--falkordb)
forces a fresh FalkorDB `BGSAVE`, proves exact AOF health, and snapshots `data-falkordb-0`. This procedure
restores that snapshot; it is the repository-originated FalkorDB backup/restore contract required by AC9.

```bash
: "${FALKORDB_SNAPSHOT:=falkordb-$RECOVERY_ID}"
kubectl -n "$NAMESPACE" get volumesnapshot "$FALKORDB_SNAPSHOT" \
  -o jsonpath='{.status.readyToUse}' | grep -qx true

kubectl -n "$NAMESPACE" scale statefulset/falkordb --replicas=0
if kubectl -n "$NAMESPACE" get pod falkordb-0 >/dev/null 2>&1; then
  kubectl -n "$NAMESPACE" wait --for=delete pod/falkordb-0 --timeout=10m
fi

# Destructive: incident command must confirm the original PVC is lost/unusable and the snapshot is verified.
kubectl -n "$NAMESPACE" delete pvc data-falkordb-0 --ignore-not-found
kubectl -n "$NAMESPACE" apply -f - <<EOF
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: data-falkordb-0
spec:
  storageClassName: $RESTORE_STORAGE_CLASS
  dataSource:
    name: $FALKORDB_SNAPSHOT
    kind: VolumeSnapshot
    apiGroup: snapshot.storage.k8s.io
  accessModes: [ReadWriteOnce]
  resources:
    requests:
      storage: 10Gi
EOF
kubectl -n "$NAMESPACE" wait \
  --for=jsonpath='{.status.phase}'=Bound pvc/data-falkordb-0 --timeout=10m
kubectl -n "$NAMESPACE" scale statefulset/falkordb --replicas=1
kubectl -n "$NAMESPACE" rollout status statefulset/falkordb --timeout=10m
kubectl -n "$NAMESPACE" exec falkordb-0 -- \
  stat -c '%u:%g %a %n' /var/lib/falkordb/data \
  > "$EVIDENCE_DIR/falkordb-data-ownership.txt"
```

For a file-level `dump.rdb`/`appendonlydir` recovery, use the deployment-supplied maintenance-pod playbook
recorded in the recovery policy. It must mount the stopped PVC, verify artifact checksums before staging,
preserve data-directory ownership, and never copy from or into a live data-plane pod. If that playbook or its
evidence is unavailable, use the VolumeSnapshot path above or rebuild the graph through logical restore.

## Scenario 3 — Full-cluster loss

1. Deploy `deploy/kubernetes/overlays/production` on the new cluster and require all four Dapr components plus
   Redis/FalkorDB StatefulSets to become healthy.
2. Restore Redis/FalkorDB credentials and embedding secret values through the approved secret-store process.
3. For each tenant, follow the exact create/status/`Active`/isolation-verification procedure in
   [Tenant Onboarding and Offboarding](./tenant-onboarding-offboarding.md#onboarding), using the same id and
   provider/model/dimensions recorded in the export and recovery policy.
4. Restore every logical export. This loop validates manifest scope, selects the matching route, fails on the
   first malformed/request/terminal error, and does not advance until each workflow is `Completed` with zero
   skipped records.

```bash
wait_restore() {
  status_url="$1"
  body="$2"
  instance_id="$3"
  deadline=$(( $(date +%s) + 1800 ))
  while [ "$(date +%s)" -lt "$deadline" ]; do
    curl -fsS -H "Authorization: Bearer $TOKEN" "$status_url" -o "$body"
    jq -e --arg instance "$instance_id" '.instanceId == $instance' "$body" >/dev/null
    status="$(jq -er '.status' "$body")"
    case "$status" in
      Completed)
        return 0
        ;;
      Failed|Canceled|Terminated)
        jq -r '{failureCode,failureMessage,failureSuggestion}' "$body" >&2
        return 1
        ;;
      *) sleep 5 ;;
    esac
  done
  return 1
}

shopt -s nullglob
exports=(exports/*-export.json)
((${#exports[@]} > 0))
for export_file in "${exports[@]}"; do
  tenant="$(jq -er '.manifest.tenantId | select(type == "string" and length > 0)' "$export_file")"
  scope="$(jq -er '.manifest.scope | select(. == "tenant" or . == "case")' "$export_file")"
  case "$scope" in
    tenant)
      import_url="$MEMORIES_BASE_URL/api/v1/tenants/$tenant/import"
      ;;
    case)
      case_id="$(jq -er '.manifest.caseId | select(type == "string" and length > 0)' "$export_file")"
      import_url="$MEMORIES_BASE_URL/api/v1/tenants/$tenant/cases/$case_id/import"
      ;;
  esac

  response_body="$(mktemp)"
  if ! curl -fsS -X POST "$import_url" \
      -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
      --data-binary "@$export_file" -o "$response_body"; then
    rm -f "$response_body"
    exit 1
  fi
  instance_id="$(jq -er '.instanceId | select(type == "string" and length > 0)' "$response_body")"
  if ! status_url="$(jq -er '.statusLocation | select(type == "string" and length > 0)' "$response_body")" ||
      ! wait_restore "$status_url" "$response_body" "$instance_id"; then
    rm -f "$response_body"
    exit 1
  fi
  expected_units="$(jq -er '.statistics.memoryUnitCount | select(type == "number")' "$export_file")"
  expected_cases="$(jq -er '.statistics.caseCount | select(type == "number")' "$export_file")"
  expected_edges="$(jq -er '.statistics.edgeCount | select(type == "number")' "$export_file")"
  jq -e --argjson units "$expected_units" --argjson cases "$expected_cases" \
    --argjson edges "$expected_edges" \
    '.skippedRecords == 0 and
     .restoredMemoryUnits == $units and
     .restoredCases == $cases and
     .restoredEdges == $edges' "$response_body" >/dev/null
  rm -f "$response_body"
done
```

The loop supports the case-scoped recovery points required when a tenant exceeds the 512 MiB import ceiling.
It never posts a case envelope to the tenant route.

## Verification and evidence

After physical recovery, or after all case-scoped imports for a tenant complete, create a fresh consolidated
tenant export and run the repository verifier. `statistics.edgeCount` excludes rebuilt `CONTAINS`; the verifier
correctly expects total graph relationships to equal `edgeCount + memoryUnitCount`.

```bash
VERIFY_EXPORT="$EVIDENCE_DIR/$TENANT-post-recovery-export.json"
memories export tenant --tenant "$TENANT" --output "$VERIFY_EXPORT" --allow-absolute-path
python3 tools/verify-backup-recovery.py \
  --namespace "$NAMESPACE" --tenant "$TENANT" --export "$VERIFY_EXPORT" \
  --evidence-output "$EVIDENCE_DIR/$TENANT-recovery-verification.json"
```

Also retain every restore status body, source/export checksum, VolumeSnapshot/PVC UID, pod events, ownership
check, tenant isolation result, and a search smoke test. A healthy pod, `DBSIZE`, or one non-zero count alone
is not proof of recovery.

## Rollback, stop conditions, and resume

- On pod/PVC restore failure, scale the affected StatefulSet to zero, preserve events and the failed PVC for
  forensics, and retry from the immutable snapshot only with incident-command approval.
- On logical restore failure, wait for or terminate the workflow, delete the failed tenant through the tenant
  deletion workflow, wait for deletion proof, and re-provision a clean same-id target. Never restore over a
  non-clean target.
- If Redis and FalkorDB recovery points cannot be proven paired, prefer the logical export/import path. Do not
  combine snapshots from different recovery IDs.
- Resume intake only after repository verification, tenant `Active`/isolation checks, search smoke tests, and
  incident-command sign-off all pass. Record the resume timestamp and playbook revision.

Redis and FalkorDB currently run as root and have no NetworkPolicies. Preserve compatible data-directory
ownership during recovery and track those hardening gaps separately; do not improvise security changes during
an active restore.
