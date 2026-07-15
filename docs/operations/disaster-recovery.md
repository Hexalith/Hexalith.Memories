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
- `kubectl`, `curl`, `jq`, `sha256sum`, `awk`, `install`, `python3`, and `memories`, plus permission to scale
  StatefulSets and create/delete the same-name PVCs owned by their `volumeClaimTemplates`;
- restored Redis/FalkorDB credentials and tenant embedding secret references, without displaying their values.

```bash
set -euo pipefail
: "${MEMORIES_BASE_URL:?set the Memories HTTPS base URL}"
: "${TOKEN:?obtain an approved bearer token}"
: "${NAMESPACE:=hexalith-memories}"
: "${RESTORE_STORAGE_CLASS:?set the approved restore StorageClass}"
: "${RECOVERY_ID:?select an approved recovery-point identifier}"
: "${RECOVERY_MANIFEST:?set the immutable recovery manifest for RECOVERY_ID}"
printf '%s\n' "$RECOVERY_ID" | grep -Eq '^[a-z0-9]([-a-z0-9.]*[a-z0-9])?$'
export HEXALITH_MEMORIES_ENDPOINT="$MEMORIES_BASE_URL"
export HEXALITH_MEMORIES_API_TOKEN="$TOKEN"
EVIDENCE_DIR="${EVIDENCE_DIR:-$PWD/recovery-evidence/$RECOVERY_ID}"
mkdir -p "$EVIDENCE_DIR"
```

Stop when snapshot identity/readiness, source checksums, tenant scope, authorization, or quiescence evidence is
missing or contradictory. Keep intake paused until post-recovery verification succeeds.

The immutable recovery manifest is deployment-catalog evidence, not a file discovered ad hoc during an
incident. It must bind one recovery ID to the paired provider snapshot handles and every tenant/case export
basename plus SHA-256 digest. Keep the exports beside the manifest; the procedures resolve each basename
relative to the manifest directory. Validate the manifest before changing state:

```bash
jq -e --arg recoveryId "$RECOVERY_ID" '
  .schemaVersion == 1 and .recoveryId == $recoveryId and
  (.redisSnapshotHandle | type == "string" and length > 0) and
  (.falkorDbSnapshotHandle | type == "string" and length > 0) and
  (.exports | type == "array" and length > 0) and
  all(.exports[];
    (.tenantId | type == "string" and length > 0) and
    (.scope == "tenant" or .scope == "case") and
    (.restore | type == "boolean") and
    (.path | type == "string" and test("^[A-Za-z0-9._-]+$")) and
    (.sha256 | test("^[A-Fa-f0-9]{64}$")) and
    (if .scope == "case" then (.caseId | type == "string" and length > 0) else true end)) and
  ([.exports[] | select(.scope == "tenant") | .tenantId] | unique | length) ==
    ([.exports[].tenantId] | unique | length) and
  ([.exports[] | select(.restore) | .tenantId] | unique | length) ==
    ([.exports[].tenantId] | unique | length)
' "$RECOVERY_MANIFEST" >/dev/null
jq -e '
  . as $manifest |
  ([.exports[].tenantId] | unique) as $tenantIds |
  all($tenantIds[]; . as $tenantId |
    ([$manifest.exports[] | select(.tenantId == $tenantId and .scope == "tenant")] | length) == 1)
' "$RECOVERY_MANIFEST" >/dev/null
RECOVERY_MANIFEST_DIR="$(cd -- "$(dirname -- "$RECOVERY_MANIFEST")" && pwd -P)"
```

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

### Paired PVC restore after either backend PVC is lost

Redis and FalkorDB form one physical recovery boundary. The repository does not emit a cross-backend marker
that can prove a surviving live backend is still byte-for-byte at an older snapshot boundary. Therefore, if
either PVC must be restored from a snapshot, restore **both** members of the approved pair. Never combine one
older snapshot with the other backend's newer live state. The following recreates both exact PVC names consumed
by the StatefulSets.

```bash
: "${REDIS_SNAPSHOT:=redis-stack-$RECOVERY_ID}"
: "${FALKORDB_SNAPSHOT:=falkordb-$RECOVERY_ID}"
kubectl -n "$NAMESPACE" get volumesnapshot "$REDIS_SNAPSHOT" \
  -o jsonpath='{.status.readyToUse}' | grep -qx true
kubectl -n "$NAMESPACE" get volumesnapshot "$FALKORDB_SNAPSHOT" \
  -o jsonpath='{.status.readyToUse}' | grep -qx true

kubectl -n "$NAMESPACE" scale statefulset/redis-stack statefulset/falkordb --replicas=0
if kubectl -n "$NAMESPACE" get pod redis-stack-0 >/dev/null 2>&1; then
  kubectl -n "$NAMESPACE" wait --for=delete pod/redis-stack-0 --timeout=10m
fi
if kubectl -n "$NAMESPACE" get pod falkordb-0 >/dev/null 2>&1; then
  kubectl -n "$NAMESPACE" wait --for=delete pod/falkordb-0 --timeout=10m
fi

# Destructive: incident command must approve rewinding both backends to the paired recovery point.
kubectl -n "$NAMESPACE" delete pvc data-redis-stack-0 --ignore-not-found
kubectl -n "$NAMESPACE" delete pvc data-falkordb-0 --ignore-not-found
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
---
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
  --for=jsonpath='{.status.phase}'=Bound pvc/data-redis-stack-0 --timeout=10m
kubectl -n "$NAMESPACE" wait \
  --for=jsonpath='{.status.phase}'=Bound pvc/data-falkordb-0 --timeout=10m
kubectl -n "$NAMESPACE" scale statefulset/redis-stack statefulset/falkordb --replicas=1
kubectl -n "$NAMESPACE" rollout status statefulset/redis-stack --timeout=10m
kubectl -n "$NAMESPACE" rollout status statefulset/falkordb --timeout=10m
kubectl -n "$NAMESPACE" exec redis-stack-0 -- stat -c '%u:%g %a %n' /data \
  > "$EVIDENCE_DIR/redis-data-ownership.txt"
kubectl -n "$NAMESPACE" exec falkordb-0 -- \
  stat -c '%u:%g %a %n' /var/lib/falkordb/data \
  > "$EVIDENCE_DIR/falkordb-data-ownership.txt"
```

If either PVC does not bind, either pod does not become ready, or ownership is incompatible, scale both
StatefulSets to zero, retain the failed PVCs and events for forensics, and retry from the original immutable
pair only after incident-command approval. Do not resume intake.

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
originates the FalkorDB backup evidence required by AC9. If the FalkorDB PVC is lost, execute the
[paired PVC restore](#paired-pvc-restore-after-either-backend-pvc-is-lost); do not restore FalkorDB alone.

For a file-level `dump.rdb`/`appendonlydir` recovery, use the deployment-supplied maintenance-pod playbook
recorded in the recovery policy. It must mount the stopped PVC, verify artifact checksums before staging,
preserve data-directory ownership, and never copy from or into a live data-plane pod. If that playbook or its
evidence is unavailable, use the VolumeSnapshot path above or rebuild the graph through logical restore.

## Scenario 3 — Full-cluster loss

1. Restore every external input listed in
   [Deployment Configuration](./deployment-configuration.md#required-external-inputs) through the approved
   secret/configuration process **before** starting workloads: registry credentials, Redis/FalkorDB passwords,
   app-to-Dapr and Dapr-to-app tokens, LLM/default/OIDC embedding secret material, and the production OIDC
   ConfigMap values. Do not apply placeholder values or print recovered secrets.
2. Deploy `deploy/kubernetes/overlays/production` on the new cluster, then require image pulls, both stateful
   backends, Server/MCP, and all four Dapr components to become healthy.
3. For each tenant, follow the exact create/status/`Active`/isolation-verification procedure in
   [Tenant Onboarding and Offboarding](./tenant-onboarding-offboarding.md#onboarding), using the same id and
   provider/model/dimensions recorded in the export and recovery policy.
4. Restore only the logical exports enumerated by `$RECOVERY_MANIFEST`. This loop verifies every SHA-256,
   validates each envelope against its manifest record, selects the matching route, requires the 202/Location
   contract, and stops on the first malformed, request, or terminal error.

```bash
wait_restore() {
  status_url="$1"
  body="$2"
  instance_id="$3"
  evidence_file="$4"
  deadline=$(( $(date +%s) + 1800 ))
  while [ "$(date +%s)" -lt "$deadline" ]; do
    if ! curl -fsS -H "Authorization: Bearer $TOKEN" "$status_url" -o "$body" ||
        ! jq -e --arg instance "$instance_id" '.instanceId == $instance' "$body" >/dev/null; then
      if [ -s "$body" ]; then install -m 600 "$body" "$evidence_file"; fi
      return 1
    fi
    status="$(jq -er '.status' "$body")"
    case "$status" in
      Completed)
        install -m 600 "$body" "$evidence_file"
        return 0
        ;;
      Failed|Canceled|Terminated)
        install -m 600 "$body" "$evidence_file"
        jq -r '{failureCode,failureMessage,failureSuggestion}' "$body" >&2
        return 1
        ;;
      *) sleep 5 ;;
    esac
  done
  if [ -s "$body" ]; then install -m 600 "$body" "$evidence_file"; fi
  return 1
}

while IFS= read -r export_record; do
  tenant="$(jq -er '.tenantId' <<< "$export_record")"
  scope="$(jq -er '.scope' <<< "$export_record")"
  export_name="$(jq -er '.path' <<< "$export_record")"
  export_file="$RECOVERY_MANIFEST_DIR/$export_name"
  expected_sha="$(jq -er '.sha256 | ascii_downcase' <<< "$export_record")"
  [ -f "$export_file" ]
  actual_sha="$(sha256sum "$export_file" | awk '{print tolower($1)}')"
  [ "$actual_sha" = "$expected_sha" ]

  case_id=''
  case "$scope" in
    tenant)
      import_url="$MEMORIES_BASE_URL/api/v1/tenants/$tenant/import"
      ;;
    case)
      case_id="$(jq -er '.caseId' <<< "$export_record")"
      import_url="$MEMORIES_BASE_URL/api/v1/tenants/$tenant/cases/$case_id/import"
      ;;
  esac
  jq -e --arg tenant "$tenant" --arg scope "$scope" --arg caseId "$case_id" '
    .manifest.tenantId == $tenant and .manifest.scope == $scope and
    (if $scope == "case" then .manifest.caseId == $caseId else true end)
  ' "$export_file" >/dev/null

  response_headers="$(mktemp)"
  response_body="$(mktemp)"
  if ! http_status="$(curl -sS --fail-with-body -X POST "$import_url" \
      -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
      --data-binary "@$export_file" -D "$response_headers" -o "$response_body" \
      -w '%{http_code}')" || [ "$http_status" != 202 ]; then
    rm -f "$response_headers" "$response_body"
    exit 1
  fi
  instance_id="$(jq -er '.instanceId | select(type == "string" and length > 0)' "$response_body")"
  status_path="$(jq -er '.statusLocation | select(type == "string" and startswith("/"))' "$response_body")"
  returned_location="$(tr -d '\r' < "$response_headers" | awk 'tolower($1) == "location:" { print $2 }' | tail -n 1)"
  [ "$returned_location" = "$status_path" ]
  status_url="${MEMORIES_BASE_URL%/}$status_path"
  status_evidence="$EVIDENCE_DIR/$tenant-$instance_id-status.json"
  if ! wait_restore "$status_url" "$response_body" "$instance_id" "$status_evidence"; then
    rm -f "$response_headers" "$response_body"
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
  install -m 600 "$response_body" "$status_evidence"
  rm -f "$response_headers" "$response_body"
done < <(jq -c '.exports[] | select(.restore)' "$RECOVERY_MANIFEST")
```

The loop supports the case-scoped recovery points required when a tenant exceeds the 512 MiB import ceiling.
It never posts a case envelope to the tenant route.

## Verification and evidence

After physical recovery or logical imports, verify **every** tenant against the immutable, pre-loss consolidated
tenant export recorded for the selected recovery point. Never generate the verifier baseline from recovered
state: that would make lost data disappear from both expected and actual counts. `statistics.edgeCount` excludes
rebuilt `CONTAINS`; the verifier expects total graph relationships to equal `edgeCount + memoryUnitCount`.

```bash
jq -r '[.exports[].tenantId] | unique[]' "$RECOVERY_MANIFEST" |
while IFS= read -r tenant; do
  expected_export_name="$(jq -er --arg tenant "$tenant" '
    [.exports[] | select(.tenantId == $tenant and .scope == "tenant")] |
    if length == 1 then .[0].path else error("exactly one tenant baseline is required") end
  ' "$RECOVERY_MANIFEST")"
  expected_export="$RECOVERY_MANIFEST_DIR/$expected_export_name"
  expected_sha="$(jq -er --arg tenant "$tenant" '
    [.exports[] | select(.tenantId == $tenant and .scope == "tenant")] |
    if length == 1 then .[0].sha256 | ascii_downcase else error("missing tenant baseline") end
  ' "$RECOVERY_MANIFEST")"
  actual_sha="$(sha256sum "$expected_export" | awk '{print tolower($1)}')"
  [ "$actual_sha" = "$expected_sha" ]
  python3 tools/verify-backup-recovery.py \
    --namespace "$NAMESPACE" --tenant "$tenant" --export "$expected_export" \
    --evidence-output "$EVIDENCE_DIR/$tenant-recovery-verification.json"
done
```

Also retain every restore status body, source/export checksum, VolumeSnapshot/PVC UID, pod events, ownership
check, tenant isolation result, and a search smoke test. A healthy pod, `DBSIZE`, or one non-zero count alone
is not proof of recovery.

## Rollback, stop conditions, and resume

- On paired PVC restore failure, scale both StatefulSets to zero, preserve events and failed PVCs for
  forensics, and retry from the immutable pair only with incident-command approval. A pod-only restart with
  both original PVCs intact may still isolate the affected StatefulSet.
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
