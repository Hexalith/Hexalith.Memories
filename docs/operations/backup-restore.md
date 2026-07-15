# Backup & Restore (Story 26.2)

This runbook defines logical and physical backup and restore for Hexalith.Memories. It closes the feature
portion of audit finding A25 and reinforces NFR16's controlled-restart zero-loss guarantee.

- **Logical backup** is the portable JSON envelope produced by export and consumed by import/restore.
- **Physical backup** is a coordinated Redis/FalkorDB recovery point made from their persistent volumes.

Restore fidelity is proved by
`tests/Hexalith.Memories.IntegrationTests/Restore/BackupRestoreFidelityIntegrationTests.cs`. Controlled Redis
restart durability is proved by
`Ingestion/PipelinePersistenceIntegrationTests.RestartTopology_ShouldPreserveIndexedRedisBackedDataAcrossControlledRestart`.

Cross-links: [deployment-configuration.md](./deployment-configuration.md) (topology and persistence),
[pipeline-persistence.md](./pipeline-persistence.md) (NFR16 evidence),
[disaster-recovery.md](./disaster-recovery.md) (pod/PVC/cluster recovery),
[failure-recovery.md](./failure-recovery.md) (failed-unit re-ingestion),
[incident-response.md](./incident-response.md) (incident command),
[tenant-onboarding-offboarding.md](./tenant-onboarding-offboarding.md) (clean tenant provisioning), and
[upgrade-migration.md](./upgrade-migration.md) (upgrade backup gates).

## What is (and is not) captured

The export is a logical snapshot, not a byte-image of Redis.

| Data | Redis/graph object | On restore | Fidelity |
|---|---|---|---|
| Memory unit | `{tenantId}:mu:{id}` (HASH) | Written from export | Exact field set and values |
| Case record | `{tenantId}:case:{id}` (HASH) | Written from export | Exact field set and values |
| Case members | `{tenantId}:case:{id}:members` (HASH) | Written from export | Exact member set and types |
| Case activity | `{tenantId}:case:{id}:activity` + `:activity:summary` | Not restored | Operational read-model; new activity accrues after restore |
| Graph | Per-tenant FalkorDB graph | Exported edges restored; `CONTAINS` rebuilt from `caseId` | Direction, type, `createdAt`, confidence, origin, `verifiedBy`, and `previousConfidence` preserved |
| Semantic chunks | `{tenantId}:vec:{id}:{seq}` (HASH) | Re-chunked and re-embedded | Attribution/dimensions equal; bytes equal for deterministic providers |
| NL vectors | `{tenantId}:vecnl:{id}` (HASH) | Not restored | No generic semantic re-index or force-replay path exists; recover through the supported original-source republication/re-ingestion path in [Index Rebuild](./index-rebuild.md) |

Vectors are re-derived because export contains provider/model/dimensions attribution but no vector bytes or
AI-generated NL description. Secret values are never exported: `apiSecretKeyName` is only a secret-store key
name, so the target deployment must restore the referenced secret independently.

## Backup policy, prerequisites, and authorization

The repository owns the safety invariants and evidence schema. Each deployment owns the concrete values and
commands that satisfy them. Before any backup, record and approve:

- `RPO`, logical-export cadence, physical-snapshot cadence, retention, backup owner, and restore-test cadence;
- an encrypted, immutable, off-cluster destination that survives loss of the application cluster;
- the deployment's approved intake-quiescence and resume playbook URI/version;
- the CSI `VolumeSnapshotClass`, restore `StorageClass`, namespace, and permission owner;
- the recovery-point identifier that binds the logical export, Redis snapshot, FalkorDB snapshot, checksums,
  source PVC UIDs, and restore-rehearsal result.

Required tools are `memories`, `curl`, `jq`, `sha256sum`, `python3`, and `kubectl` with permission to read/scale
the two StatefulSets and create/read `VolumeSnapshot` and PVC resources. Obtain `$TOKEN` through the approved
identity workflow, keep shell tracing disabled, and never print it.

```bash
set -euo pipefail
: "${MEMORIES_BASE_URL:?set the Memories HTTPS base URL}"
: "${TOKEN:?obtain an approved bearer token}"
: "${TENANT:?set the tenant id}"
: "${NAMESPACE:=hexalith-memories}"
: "${SNAPSHOT_CLASS:?set the approved VolumeSnapshotClass}"
: "${RESTORE_STORAGE_CLASS:?set the approved restore StorageClass}"
: "${BACKUP_DESTINATION:?set the immutable off-cluster destination}"
: "${BACKUP_OWNER:?set the accountable backup owner}"
: "${RPO:?set the approved recovery-point objective}"
: "${RETENTION:?set the approved retention policy}"
: "${QUIESCE_PLAYBOOK:?set the approved deployment-specific quiescence playbook URI/version}"
: "${QUIESCE_EVIDENCE:?set the access-controlled evidence file written by that playbook}"
: "${MAX_QUIESCE_EVIDENCE_AGE_SECONDS:?set the approved maximum evidence age in seconds}"
export HEXALITH_MEMORIES_ENDPOINT="$MEMORIES_BASE_URL"
export HEXALITH_MEMORIES_API_TOKEN="$TOKEN"
RECOVERY_ID="${RECOVERY_ID:-$(date -u +%Y%m%dt%H%M%Sz)}"
printf '%s\n' "$RECOVERY_ID" | grep -Eq '^[a-z0-9]([-a-z0-9.]*[a-z0-9])?$'
[ "${#RECOVERY_ID}" -le 220 ]
BACKUP_WORKDIR="${BACKUP_WORKDIR:-$PWD/backups/$RECOVERY_ID}"
mkdir -p "$BACKUP_WORKDIR"
```

The deployment-specific quiescence playbook must pause every ingress/publisher path, prevent new scheduling,
and enumerate the app-owned in-flight workflow registry plus Dapr workflow state until every tracked ingestion,
restore, provisioning, deletion, and repair workflow is terminal. Its evidence must name the paused controls,
the zero-active result, timestamps, deployment revision, and resume owner. If the evidence is missing, stale,
non-zero, or cannot be independently verified, stop: do not take independent Redis/FalkorDB snapshots.

```bash
# Run the approved deployment-owned playbook outside this document, then enforce the repository-owned gate.
test -s "$QUIESCE_EVIDENCE"
jq -e --arg playbook "$QUIESCE_PLAYBOOK" \
  '.playbook == $playbook and .intakePaused == true and .activeWorkflows == 0 and
   (.capturedAt | type == "string") and (.resumeOwner | type == "string" and length > 0)' \
  "$QUIESCE_EVIDENCE" >/dev/null
printf '%s\n' "$MAX_QUIESCE_EVIDENCE_AGE_SECONDS" | grep -Eq '^[1-9][0-9]*$'
CAPTURED_AT_EPOCH="$(jq -er '.capturedAt | fromdateiso8601' "$QUIESCE_EVIDENCE")"
NOW_EPOCH="$(date -u +%s)"
EVIDENCE_AGE_SECONDS=$((NOW_EPOCH - CAPTURED_AT_EPOCH))
[ "$EVIDENCE_AGE_SECONDS" -ge 0 ]
[ "$EVIDENCE_AGE_SECONDS" -le "$MAX_QUIESCE_EVIDENCE_AGE_SECONDS" ]

jq -n --arg recoveryId "$RECOVERY_ID" --arg owner "$BACKUP_OWNER" --arg rpo "$RPO" \
  --arg retention "$RETENTION" --arg destination "$BACKUP_DESTINATION" \
  --arg quiescenceEvidence "$QUIESCE_EVIDENCE" \
  '{schemaVersion:1,recoveryId:$recoveryId,owner:$owner,rpo:$rpo,retention:$retention,
    destination:$destination,quiescenceEvidence:$quiescenceEvidence}' \
  > "$BACKUP_WORKDIR/recovery-policy.json"
```

Keep intake paused until both physical snapshots and their metadata are verified. The resume playbook must be
an explicit incident-command action; never resume automatically after a failed or timed-out backup.

## Logical backup (export)

Use the CLI as the primary path. Its `--output` writer writes a `.part` file and atomically renames on success,
so an interrupted request cannot truncate the preceding recovery point.

```bash
TENANT_EXPORT="$BACKUP_WORKDIR/$TENANT-tenant-export.json"
memories export tenant --tenant "$TENANT" --output "$TENANT_EXPORT" --allow-absolute-path
jq -e --arg tenant "$TENANT" \
  '.manifest.schemaVersion == 1 and .manifest.scope == "tenant" and .manifest.tenantId == $tenant and
   (.statistics.memoryUnitCount | type == "number") and (.statistics.edgeCount | type == "number")' \
  "$TENANT_EXPORT" >/dev/null
sha256sum "$TENANT_EXPORT" > "$TENANT_EXPORT.sha256"

# Optional case-scoped backup when CASE is set by policy or needed for the 512 MiB import ceiling.
if [ -n "${CASE:-}" ]; then
  CASE_EXPORT="$BACKUP_WORKDIR/$TENANT-$CASE-case-export.json"
  memories export case --tenant "$TENANT" --case "$CASE" --output "$CASE_EXPORT" --allow-absolute-path
  jq -e --arg tenant "$TENANT" --arg caseId "$CASE" \
    '.manifest.schemaVersion == 1 and .manifest.scope == "case" and
     .manifest.tenantId == $tenant and .manifest.caseId == $caseId' "$CASE_EXPORT" >/dev/null
  sha256sum "$CASE_EXPORT" > "$CASE_EXPORT.sha256"
fi
```

For endpoint-only environments, retain the same atomic and validation behavior:

```bash
TMP_EXPORT="$(mktemp "$BACKUP_WORKDIR/export.XXXXXX.part")"
trap 'rm -f "$TMP_EXPORT"' EXIT
curl -fsS -H "Authorization: Bearer $TOKEN" \
  "$MEMORIES_BASE_URL/api/v1/tenants/$TENANT/export" -o "$TMP_EXPORT"
jq -e --arg tenant "$TENANT" \
  '.manifest.schemaVersion == 1 and .manifest.scope == "tenant" and .manifest.tenantId == $tenant' \
  "$TMP_EXPORT" >/dev/null
mv "$TMP_EXPORT" "$TENANT_EXPORT"
sha256sum "$TENANT_EXPORT" > "$TENANT_EXPORT.sha256"
trap - EXIT
```

Upload the JSON, checksum, `recovery-policy.json`, and quiescence evidence through the deployment's approved
immutable-storage playbook. Verify the destination object identity/checksum and record that evidence before
considering the logical backup complete.

## Physical backup (Redis + FalkorDB)

| Workload | Mount | PVC | Persistence |
|---|---|---|---|
| Redis Stack | `/data` | `data-redis-stack-0` (`20Gi`) | AOF + RDB |
| FalkorDB | `/var/lib/falkordb/data` | `data-falkordb-0` (`10Gi`) | AOF + RDB |

Redis persistence is configured in `deploy/redis/redis.conf` and enforced by
`src/Hexalith.Memories.AppHost/Program.cs`. FalkorDB persistence is configured by
`deploy/kubernetes/base/kustomization.yaml`. Do not redefine either configuration in an operator session.

### 1. Prove a fresh RDB save and healthy AOF

Each bounded command requires the just-requested save to advance `LASTSAVE`; it then requires exact healthy
AOF values. A timeout or mismatch is a hard stop.

```bash
kubectl -n "$NAMESPACE" exec redis-stack-0 -- sh -ec '
  export REDISCLI_AUTH="$REDIS_PASSWORD"
  before="$(redis-cli --no-auth-warning --raw LASTSAVE)"
  redis-cli --no-auth-warning --raw BGSAVE | grep -Eq "Background saving (started|scheduled)"
  deadline=$(( $(date +%s) + 600 ))
  while [ "$(redis-cli --no-auth-warning --raw LASTSAVE)" -le "$before" ]; do
    [ "$(date +%s)" -lt "$deadline" ] || exit 124
    sleep 2
  done
  info="$(redis-cli --no-auth-warning --raw INFO persistence | tr -d "\r")"
  printf "%s\n" "$info" | grep -qx "rdb_last_bgsave_status:ok"
  printf "%s\n" "$info" | grep -qx "aof_enabled:1"
  printf "%s\n" "$info" | grep -qx "aof_last_write_status:ok"
  printf "%s\n" "$info" | grep -qx "aof_last_bgrewrite_status:ok"
'

kubectl -n "$NAMESPACE" exec falkordb-0 -- sh -ec '
  export REDISCLI_AUTH="$FALKORDB_PASSWORD"
  before="$(redis-cli --no-auth-warning --raw LASTSAVE)"
  redis-cli --no-auth-warning --raw BGSAVE | grep -Eq "Background saving (started|scheduled)"
  deadline=$(( $(date +%s) + 600 ))
  while [ "$(redis-cli --no-auth-warning --raw LASTSAVE)" -le "$before" ]; do
    [ "$(date +%s)" -lt "$deadline" ] || exit 124
    sleep 2
  done
  info="$(redis-cli --no-auth-warning --raw INFO persistence | tr -d "\r")"
  printf "%s\n" "$info" | grep -qx "rdb_last_bgsave_status:ok"
  printf "%s\n" "$info" | grep -qx "aof_enabled:1"
  printf "%s\n" "$info" | grep -qx "aof_last_write_status:ok"
  printf "%s\n" "$info" | grep -qx "aof_last_bgrewrite_status:ok"
'
```

`REDISCLI_AUTH` keeps the credential out of `redis-cli` process arguments. It remains an in-container secret;
never print the environment or enable shell tracing.

### 2. Create the paired recovery point

Use provider-supported atomic group snapshots when available. Otherwise the verified quiescence gate above is
mandatory and intake stays paused until both independent snapshots are ready.

```bash
REDIS_SNAPSHOT="redis-stack-$RECOVERY_ID"
FALKORDB_SNAPSHOT="falkordb-$RECOVERY_ID"

kubectl -n "$NAMESPACE" apply -f - <<EOF
apiVersion: snapshot.storage.k8s.io/v1
kind: VolumeSnapshot
metadata:
  name: $REDIS_SNAPSHOT
spec:
  volumeSnapshotClassName: $SNAPSHOT_CLASS
  source:
    persistentVolumeClaimName: data-redis-stack-0
---
apiVersion: snapshot.storage.k8s.io/v1
kind: VolumeSnapshot
metadata:
  name: $FALKORDB_SNAPSHOT
spec:
  volumeSnapshotClassName: $SNAPSHOT_CLASS
  source:
    persistentVolumeClaimName: data-falkordb-0
EOF

kubectl -n "$NAMESPACE" wait \
  --for=jsonpath='{.status.readyToUse}'=true "volumesnapshot/$REDIS_SNAPSHOT" --timeout=10m
kubectl -n "$NAMESPACE" wait \
  --for=jsonpath='{.status.readyToUse}'=true "volumesnapshot/$FALKORDB_SNAPSHOT" --timeout=10m
kubectl -n "$NAMESPACE" get volumesnapshot "$REDIS_SNAPSHOT" "$FALKORDB_SNAPSHOT" -o json \
  > "$BACKUP_WORKDIR/volume-snapshots.json"
kubectl -n "$NAMESPACE" get pvc data-redis-stack-0 data-falkordb-0 -o json \
  > "$BACKUP_WORKDIR/source-pvcs.json"

REDIS_SNAPSHOT_CONTENT="$(kubectl -n "$NAMESPACE" get volumesnapshot "$REDIS_SNAPSHOT" \
  -o jsonpath='{.status.boundVolumeSnapshotContentName}')"
FALKORDB_SNAPSHOT_CONTENT="$(kubectl -n "$NAMESPACE" get volumesnapshot "$FALKORDB_SNAPSHOT" \
  -o jsonpath='{.status.boundVolumeSnapshotContentName}')"
[ -n "$REDIS_SNAPSHOT_CONTENT" ]
[ -n "$FALKORDB_SNAPSHOT_CONTENT" ]
kubectl get volumesnapshotcontent "$REDIS_SNAPSHOT_CONTENT" "$FALKORDB_SNAPSHOT_CONTENT" -o json \
  > "$BACKUP_WORKDIR/volume-snapshot-contents.json"
jq -e '
  .items | length == 2 and
  all(.[]; (.spec.driver | type == "string" and length > 0) and
           (.status.snapshotHandle | type == "string" and length > 0) and
           .status.readyToUse == true)
' "$BACKUP_WORKDIR/volume-snapshot-contents.json" >/dev/null

REDIS_SNAPSHOT_HANDLE="$(jq -er --arg name "$REDIS_SNAPSHOT_CONTENT" \
  '.items[] | select(.metadata.name == $name) | .status.snapshotHandle' \
  "$BACKUP_WORKDIR/volume-snapshot-contents.json")"
FALKORDB_SNAPSHOT_HANDLE="$(jq -er --arg name "$FALKORDB_SNAPSHOT_CONTENT" \
  '.items[] | select(.metadata.name == $name) | .status.snapshotHandle' \
  "$BACKUP_WORKDIR/volume-snapshot-contents.json")"
TENANT_EXPORT_SHA256="$(sha256sum "$TENANT_EXPORT" | awk '{print toupper($1)}')"
TENANT_EXPORT_NAME="$(basename "$TENANT_EXPORT")"
jq -n --arg recoveryId "$RECOVERY_ID" --arg redisSnapshotHandle "$REDIS_SNAPSHOT_HANDLE" \
  --arg falkorDbSnapshotHandle "$FALKORDB_SNAPSHOT_HANDLE" --arg tenantId "$TENANT" \
  --arg path "$TENANT_EXPORT_NAME" --arg sha256 "$TENANT_EXPORT_SHA256" '
  {schemaVersion:1,recoveryId:$recoveryId,redisSnapshotHandle:$redisSnapshotHandle,
   falkorDbSnapshotHandle:$falkorDbSnapshotHandle,
   exports:[{tenantId:$tenantId,scope:"tenant",restore:true,path:$path,sha256:$sha256}]}
' > "$BACKUP_WORKDIR/recovery-manifest.json"
if [ -n "${CASE:-}" ]; then
  CASE_EXPORT_SHA256="$(sha256sum "$CASE_EXPORT" | awk '{print toupper($1)}')"
  CASE_EXPORT_NAME="$(basename "$CASE_EXPORT")"
  MANIFEST_TMP="$(mktemp "$BACKUP_WORKDIR/recovery-manifest.XXXXXX.part")"
  jq --arg tenantId "$TENANT" --arg caseId "$CASE" --arg path "$CASE_EXPORT_NAME" \
    --arg sha256 "$CASE_EXPORT_SHA256" '
    .exports += [{tenantId:$tenantId,scope:"case",caseId:$caseId,restore:false,
                  path:$path,sha256:$sha256}]
  ' "$BACKUP_WORKDIR/recovery-manifest.json" > "$MANIFEST_TMP"
  mv "$MANIFEST_TMP" "$BACKUP_WORKDIR/recovery-manifest.json"
fi
```

If either wait fails, keep intake paused, capture `kubectl describe volumesnapshot`, and have incident command
either delete both failed recovery-point resources and retry with a new ID or abandon the physical backup.
Never pair a newly retried snapshot with one from the failed attempt.

Record both provider snapshot handles, CSI drivers, `VolumeSnapshotContent` names, creation timestamps, source
PVC UIDs, policy evidence, and logical-export checksums together. File-level copies are allowed only through a
deployment-owned maintenance-pod playbook
mounting a quiesced snapshot read-only; never copy a live AOF directory.

The manifest's `restore` flag selects payloads, while its one tenant-scoped export per tenant remains the
immutable verification baseline. For a tenant larger than the import ceiling, add every case export, set the
tenant export to `restore:false`, and mark the complete non-overlapping case set `restore:true`. Validate the
catalog and upload it atomically with the other recovery-point evidence before resuming intake.

## Logical restore procedure

Restore is same-tenant-id only and asynchronous. The target tenant/case must be provisioned, `Active`, and
clean: no indexed units, case hashes, or graph artifacts. Its provider/model/dimensions and secret reference
must match the export. `RestoreTargetGuard` rejects a non-clean target.

```bash
submit_and_wait_restore() {
  export_file="$1"
  import_url="$2"
  expected_units="$(jq -er '.statistics.memoryUnitCount | select(type == "number")' "$export_file")"
  expected_cases="$(jq -er '.statistics.caseCount | select(type == "number")' "$export_file")"
  expected_edges="$(jq -er '.statistics.edgeCount | select(type == "number")' "$export_file")"
  response_headers="$(mktemp)"
  response_body="$(mktemp)"
  if ! http_status="$(curl -sS --fail-with-body -X POST "$import_url" \
      -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
      --data-binary "@$export_file" -D "$response_headers" -o "$response_body" \
      -w '%{http_code}')"; then
    rm -f "$response_headers" "$response_body"
    return 1
  fi
  if [ "$http_status" != 202 ]; then
    rm -f "$response_headers" "$response_body"
    return 1
  fi

  instance_id="$(jq -er '.instanceId | select(type == "string" and length > 0)' "$response_body")"
  status_path="$(jq -er '.statusLocation | select(type == "string" and startswith("/"))' "$response_body")"
  returned_location="$(tr -d '\r' < "$response_headers" | awk 'tolower($1) == "location:" { print $2 }' | tail -n 1)"
  [ "$returned_location" = "$status_path" ]
  status_url="${MEMORIES_BASE_URL%/}$status_path"
  status_evidence="$BACKUP_WORKDIR/restore-$instance_id-status.json"
  deadline=$(( $(date +%s) + 1800 ))
  status=''
  while [ "$(date +%s)" -lt "$deadline" ]; do
    if ! curl -fsS -H "Authorization: Bearer $TOKEN" "$status_url" -o "$response_body" ||
        ! jq -e --arg instance "$instance_id" '.instanceId == $instance' "$response_body" >/dev/null; then
      if [ -s "$response_body" ]; then install -m 600 "$response_body" "$status_evidence"; fi
      rm -f "$response_headers" "$response_body"
      return 1
    fi
    status="$(jq -er '.status' "$response_body")"
    case "$status" in
      Completed)
        if ! jq -e --argjson units "$expected_units" --argjson cases "$expected_cases" \
            --argjson edges "$expected_edges" \
            '.skippedRecords == 0 and
             .restoredMemoryUnits == $units and
             .restoredCases == $cases and
             .restoredEdges == $edges' "$response_body" >/dev/null; then
          install -m 600 "$response_body" "$status_evidence"
          rm -f "$response_headers" "$response_body"
          return 1
        fi
        install -m 600 "$response_body" "$status_evidence"
        rm -f "$response_headers" "$response_body"
        return 0
        ;;
      Failed|Canceled|Terminated)
        install -m 600 "$response_body" "$status_evidence"
        jq -r '{failureCode,failureMessage,failureSuggestion}' "$response_body" >&2
        rm -f "$response_headers" "$response_body"
        return 1
        ;;
      *) sleep 5 ;;
    esac
  done

  printf 'restore %s did not reach a terminal state before the deadline\n' "$instance_id" >&2
  if [ -s "$response_body" ]; then install -m 600 "$response_body" "$status_evidence"; fi
  rm -f "$response_headers" "$response_body"
  return 1
}

submit_and_wait_restore "$TENANT_EXPORT" \
  "$MEMORIES_BASE_URL/api/v1/tenants/$TENANT/import"

# Case restore:
# submit_and_wait_restore "$CASE_EXPORT" \
#   "$MEMORIES_BASE_URL/api/v1/tenants/$TENANT/cases/$CASE/import"
```

### Client errors

| HTTP | Code | Meaning |
|---|---|---|
| 400 | `IMPORT_SCHEMA_VERSION_UNSUPPORTED` | `manifest.schemaVersion` is not `1`. |
| 400 | `IMPORT_SCOPE_MISMATCH` | Tenant JSON used on the case route or vice versa. |
| 400 | `IMPORT_TENANT_MISMATCH` | Manifest tenant differs from the route tenant. |
| 400 | `IMPORT_CASE_MISMATCH` | Manifest case differs from the route case. |
| 409 | `RESTORE_TARGET_BUSY` | Another restore owns the tenant-wide lease. |
| 409 | `RESTORE_TARGET_NOT_CLEAN` | Existing data would make exact restore impossible. |
| 413 | `IMPORT_TOO_LARGE` | Payload exceeds 512 MiB; restore case-scoped exports. |

## Verification and evidence

Run the repository verifier after logical or physical recovery. It asserts both PVCs are bound; exact Redis
and FalkorDB AOF health; memory-unit and case counts from export statistics; at least one semantic chunk per
unit; and total graph relationships equal `statistics.edgeCount + statistics.memoryUnitCount` (the second term
is the rebuilt `CONTAINS` set). It emits sanitized JSON evidence and fails non-zero on mismatch.

```bash
python3 tools/verify-backup-recovery.py \
  --namespace "$NAMESPACE" \
  --tenant "$TENANT" \
  --export "$TENANT_EXPORT" \
  --evidence-output "$BACKUP_WORKDIR/recovery-verification.json"
```

`statistics.edgeCount` counts only edges serialized in `edges[]`; never compare it directly with an all-edge
FalkorDB count. For case-scoped restore sets, require every restore status counter to match its source export,
then take a fresh tenant export and run the verifier against that consolidated snapshot. Preserve the verifier
output, restore status bodies, checksums, search smoke-test evidence, and incident approvals together.

## Rollback and stop conditions

There is no supported "restore an older snapshot over the top." Additive `HSET`/`MERGE` cannot remove newer
objects, and the clean-target guard rejects them. On upload, validation, lease, workflow, or verification
failure:

1. Stop new intake and keep it stopped.
2. Wait for the restore to become terminal, or have incident command terminate it through the approved Dapr
   workflow operation; never delete the tenant while restore activities can still write.
3. Delete the failed target through the tenant deletion workflow and wait for deletion evidence.
4. Re-provision the same tenant id/configuration, require `Active` plus isolation verification, and retry the
   known-good export into the clean target.

If deletion or termination cannot be proved, stop and escalate. Do not search, resume intake, or layer another
restore over the uncertain target.

## Scale limit

Import accepts at most 512 MiB. It stages 1 MiB Redis chunks, processes at most 100 units per re-index activity,
and renews staging/lease retention for up to 12 hours. Larger recovery points must use case-scoped exports.
