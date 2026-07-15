# Upgrade and Migration

## Purpose and scope

Owner: release/platform operations. Review cadence: every release that changes application images,
Kubernetes/Dapr/runtime/CRDs, workflow shape, Redis Stack/FalkorDB, schemas/indexes, or durable data,
and quarterly otherwise. Last verified: 2026-07-14 at repository revision
`1553ee6708f644f3a4bc3638d3aaceed682b2371`.

This runbook separates stateless rollout from durable-data migration and recovery. It covers Server/MCP
Deployments, Dapr control plane/sidecars/CRDs, the target Kubernetes cluster and kubectl client, Redis
Stack, FalkorDB, workflow compatibility, Kustomize configuration, Secrets, canary tenants, and retained
backup evidence. Application changes can affect all tenants; backend/schema/data migration can affect
all data on the shared backend.

Repository pins and image digests are the deployment input. Current pins include .NET SDK 10.0.301,
Dapr .NET packages 1.18.4, Redis Stack 7.4.0-v8, and FalkorDB 4.12.0. A package pin does not prove the
cluster runtime/control-plane version. No component version is changed by this runbook.

## Prerequisites and authorization

- Assign release owner, platform/Dapr owner, data owner, tenant canary owner, security/secret owner,
  operator, rollback owner, and observation-window approver.
- Record target/current cluster server version, kubectl version, Dapr CLI/control-plane/sidecar/CRD
  versions, container runtime/architecture, CSI snapshot support, application/image digests, Redis and
  FalkorDB versions/data layouts, .NET/base-image compatibility, and provider dependencies.
- Validate Kubernetes client/server/component version skew against the target cluster policy. The repo
  does not pin Kubernetes, so capture the actual target rather than inferring it.
- Render and review the production Kustomize diff, image provenance, OIDC/Dapr/Redis/provider Secret
  references, config, RBAC/ACLs, probes, resources, and PVC identities. Secret values must not appear in
  renders, logs, diffs, or evidence.
- Produce an immutable logical export plus physically consistent paired Redis/FalkorDB backup and pass a
  restore rehearsal before rollout. Confirm CSI VolumeSnapshot CRDs/controller/driver support; retain a
  logical-export fallback because snapshot capability is cluster-specific.
- Destructive data/schema steps require data/tenant/release approval and a tested reverse or forward-
  recovery path. Stop when compatibility, backup, source/target layout, workflow state, or rollback
  ownership is uncertain.

Initialize non-secret change inputs and require an immutable, digest-verified baseline. The newly rendered
artifact is created with restrictive permissions; callers do not choose its path, so it cannot alias or
truncate `PREVIOUS_RENDER`:

```bash
set -euo pipefail
umask 077
NAMESPACE=hexalith-memories
CANARY_TENANT_ID="${CANARY_TENANT_ID:-upgrade-canary}"
CHANGE_ID="${CHANGE_ID:-upgrade-$(date -u +%Y%m%dT%H%M%SZ)}"
: "${PREVIOUS_RENDER:?PREVIOUS_RENDER must identify the retained prior rendered artifact}"
: "${PREVIOUS_RENDER_SHA256:?PREVIOUS_RENDER_SHA256 must identify its approved digest}"
test -f "$PREVIOUS_RENDER" && test ! -L "$PREVIOUS_RENDER"
printf '%s  %s\n' "$PREVIOUS_RENDER_SHA256" "$PREVIOUS_RENDER" | sha256sum --check --status
NEW_RENDER="$(mktemp "${TMPDIR:-/tmp}/hexalith-memories-production.XXXXXX")"
RENDER_STAGE="$(mktemp "${TMPDIR:-/tmp}/hexalith-memories-render-stage.XXXXXX")"
trap 'rm -f "$RENDER_STAGE"' EXIT HUP INT TERM
test "$PREVIOUS_RENDER" != "$NEW_RENDER"
printf 'change=%s namespace=%s canary=%s previous=%s new=%s\n' \
  "$CHANGE_ID" "$NAMESPACE" "$CANARY_TENANT_ID" "$PREVIOUS_RENDER" "$NEW_RENDER"
```

## Signals and evidence

Collect versions/digests, rendered diff/dry-run, compatibility decisions, Secret/config reference
validation, backup/restore rehearsal, workflow/queue drain, rollout events/status, structured health JSON,
replay-safety events 9171/9172/9173, Dapr workflow stalled/failed state, Kubernetes restarts/OOM, Redis
persistence/memory/PVC, FalkorDB graph/counts, per-axis search latency/results, ingestion progress,
consistency, export/restore parity, isolation, and control-tenant results.

Workflow changes require special evidence. Dapr workflows replay event-sourced history and must remain
deterministic. Use compatible patches/named versions and retain old workflow versions while any in-flight
or dormant instance can reference them. During mixed-version rollout a workflow can be temporarily
stalled; an unavailable old version can leave it stalled. Verify instance-ID reuse and any required purge
against the exact deployed runtime, control-plane, sidecar, and CLI versions discovered in preflight—not
the repository's 1.18.4 SDK pin—before reusing an ID. Purge is destructive and requires workflow/data-owner
approval plus retained history/evidence.

The local `WorkflowReplaySafetyHostedService` delays for active ingestion workflows but proceeds after
five minutes and fails open on Dapr/state-registry query timeout or exception. Events 9172/9173 mean the
rollout gate did not prove drain. Operator-side quiescence and direct workflow evidence remain mandatory.

## Procedure

### 1. Inventory and compatibility gate

1. Capture `kubectl version`, cluster nodes/runtime, Dapr control-plane/sidecar/CLI and CRD versions,
   CSI snapshot capability, current images/digests, config and Secret *references*, package/runtime pins,
   Redis/FalkorDB versions, PVC/storage class, workflow types/versions, and active instances.
2. Check the supported incremental Dapr upgrade path. When Dapr/CRDs change, follow the official release
   sequence for the exact current/target versions: install forward-compatible CRDs/resources as required,
   upgrade the control plane, verify it, then restart application Deployments so new sidecars are injected.
   Do not copy a generic latest-version command into production approval.
3. Verify Kubernetes/kubectl/component skew, container base/native dependencies, security context/PVC
   ownership, Redis/FalkorDB data-format compatibility, provider SDK/API, and rollback compatibility.
4. Treat Redis Stack-to-Redis 8 as a separately staged migration plan. Redis Stack 7.4.0-v8 is the pinned
   line; no image swap belongs here. Treat any FalkorDB image/data-layout advance as a staged backup,
   restore, graph/search parity migration, not a routine Pod rollout.

### 2. Render, diff, and preflight

```bash
if ! kubectl kustomize deploy/kubernetes/overlays/production > "$RENDER_STAGE"; then
  rm -f "$RENDER_STAGE"
  exit 1
fi
test -s "$RENDER_STAGE"
mv -f "$RENDER_STAGE" "$NEW_RENDER"
chmod 600 "$NEW_RENDER"
kubectl apply --dry-run=client -f "$NEW_RENDER"
kubectl apply --dry-run=server -f "$NEW_RENDER"

if diff -u "$PREVIOUS_RENDER" "$NEW_RENDER"; then
  RENDER_DIFF_RC=0
else
  RENDER_DIFF_RC=$?
  test "$RENDER_DIFF_RC" = 1 || exit "$RENDER_DIFF_RC"
fi

if kubectl diff -f "$NEW_RENDER"; then
  KUBECTL_DIFF_RC=0
else
  KUBECTL_DIFF_RC=$?
  test "$KUBECTL_DIFF_RC" = 1 || exit "$KUBECTL_DIFF_RC"
fi
```

Review every image digest, environment/config/Secret reference, Dapr annotation/component/ACL, probe,
resource, replica, StatefulSet, service, and PVC difference. Reject unapproved mutable tags, placeholder
release tags, secret values, resource/PVC deletion, or unexplained durable changes.

Exit code 1 means expected differences for both `diff` and `kubectl diff`; any greater code is an execution
failure and stops the change. Before approval, compare the digest-verified `PREVIOUS_RENDER` resource
inventory with the live cluster and explain every drift/defaulted field. A stale baseline is not a rollback
artifact even when the new-render diff is clean.

Run the Release solution build/tests and deployment artifact verifier appropriate to the release. Record
real results; a skipped infrastructure lane is not passing evidence.

### 3. Backup, rehearse, and quiesce

1. Follow [Backup and Restore](./backup-restore.md) to freeze intake, drain workflows, capture logical
   exports and paired physical snapshots, complete Redis persistence, and verify backup identities.
2. Restore into an isolated rehearsal target and run tenant/config/count/search/consistency checks. For a
   binary, schema, index, or data-layout migration, rehearse both the source rollback layout and the exact
   target runtime/backend versions and target data layout; a source-only restore does not prove target
   recoverability. Do not rehearse over production.
3. Before the real change, pause publishers/intake and independently enumerate workflow/queue state until
   terminal/drained. If workflow shape changes, retain the old workflow version and decide version/patch/
   purge behavior before deploying.

### 4. Staging, production rollout, and tenant canary validation

1. The complete production render is an all-tenant environment change, not a tenant canary. Stage that
   render on a representative non-production cluster with the exact target versions first. If the platform
   supports progressive application delivery, use a separately reviewed canary-specific overlay that
   contains only the intended application Deployments and excludes shared StatefulSets, Dapr control-plane
   resources/components, RBAC, namespace, and durable configuration. This repository does not commit such
   an overlay, so do not infer one from `CANARY_TENANT_ID`.
2. Before a full production apply, retain a live resource/generation inventory and require explicit
   production approval. `kubectl apply` is not atomic: if any object fails, keep intake quiesced, capture
   every resource that changed, compare it with both renders, and reconcile each partial change from the
   last verified state. Never retry or roll back the whole render blindly.
3. Apply the approved production render and observe rollout with bounded waits. HTTP 200 with `Degraded`
   is not a fully healthy canary:

   ```bash
   ROLLOUT_TIMEOUT="${ROLLOUT_TIMEOUT:-10m}"
   : "${PRODUCTION_APPLY_APPROVED:?Set PRODUCTION_APPLY_APPROVED=yes only after recorded approval}"
   test "$PRODUCTION_APPLY_APPROVED" = yes
   PRE_APPLY_INVENTORY="$(mktemp "${TMPDIR:-/tmp}/hexalith-pre-apply.XXXXXX")"
   kubectl -n "$NAMESPACE" get deployment,statefulset,configmap,service -o yaml > "$PRE_APPLY_INVENTORY"
   if ! kubectl -n "$NAMESPACE" apply -f "$NEW_RENDER"; then
     kubectl -n "$NAMESPACE" get deployment,statefulset,configmap,service -o yaml
     printf 'partial apply: reconcile changed generations against %s and %s\n' \
       "$PREVIOUS_RENDER" "$NEW_RENDER" >&2
     exit 1
   fi
   kubectl -n "$NAMESPACE" rollout status deployment/memories --timeout="$ROLLOUT_TIMEOUT"
   kubectl -n "$NAMESPACE" rollout status deployment/memories-mcp --timeout="$ROLLOUT_TIMEOUT"
   if test "${BACKEND_ROLLOUT_APPROVED:-no}" = yes; then
     kubectl -n "$NAMESPACE" rollout status statefulset/redis-stack --timeout="$ROLLOUT_TIMEOUT"
     kubectl -n "$NAMESPACE" rollout status statefulset/falkordb --timeout="$ROLLOUT_TIMEOUT"
   fi
   ```

   Run the StatefulSet waits only when the explained render diff contains an independently approved backend
   change; a stateless rollout must not restart shared data backends.
4. Inspect replay-safety logs and Dapr workflow state. Stop on events 9172/9173, unexplained stalled or
   failed workflows, mixed-version incompatibility, Dapr/CRD error, or data-format warning.
5. After the changed application is serving, use `CANARY_TENANT_ID` for validation—not deployment scope.
   Verify canary tenant authorization/isolation, health, ingest and workflow completion, syntactic/
   semantic/hybrid/graph search, counts, consistency, telemetry, export, and an isolated restore rehearsal.
   Run the same control checks against an unaffected tenant.
6. Expand only when an approved progressive-delivery mechanism actually left capacity/traffic unexpanded.
   A full production render has no post-canary expansion step: keep intake quiesced or limited until the
   canary/control observation window and owner sign-off complete. Retain old images/workflow versions,
   scoped manifests, render/export/snapshots, and the pre-apply inventory through the rollback window.

### 5. Post-upgrade migration checks

- Verify actual runtime/sidecar versions, not only SDK/package versions.
- Verify Dapr CRs/components/ACLs, workflow scheduling/replay/status, actor/state-store access, and no
  unexpected instance-ID collisions.
- Compare pre/post tenant registry, index/vector/graph counts, active aliases, consistency, per-axis
  search/latency, ingestion freshness/throughput, queues/failures, and PVC/persistence state.
- For provider/dimension changes, use the existing blue/green
  [Embedding Provider Migration Runbook](./embedding-providers.md#migration-runbook); do not combine an
  unreviewed provider migration with a routine application rollout.

## Verification and evidence

Completion requires approved compatibility inventory, clean explained render diff, immutable backup and
successful restore rehearsal, proven workflow/intake drain, terminal rollout, structured `Healthy` status
for required readiness entries, no replay-safety fail-open event, successful canary/control tenant
isolation/ingestion/search/consistency/export-restore checks, stable observation-window metrics, and a
retained feasible rollback/data-recovery route.

Any `Degraded` or missing required capability stops completion and routes to
[Incident Response](./incident-response.md); an approval note cannot turn degraded evidence into a healthy
canary. Resume or expand only after the required capability is `Healthy` and the failed checks are repeated.

Retain change ID, owners/approvals, current/target version matrix, image/render digests, sanitized diff/
dry-run, Secret/config reference validation, backup identities/rehearsal, workflow inventory/versioning/
purge decision, rollout/health/log/metric results, canary/control parity, and rollback-window sign-off.
Redact credentials, Secret objects/values, content, users, and unrelated tenant details.

## Rollback, recovery, and stop conditions

For a bad stateless Pod-template/image/config rollout, stop expansion and roll back only the affected
Deployment through a reviewed Deployment revision or a scoped retained manifest. Verify that the target
Pod template references the retained immutable ConfigMap/Secret versions before restarting it. Never
reapply the complete `PREVIOUS_RENDER` as a stateless rollback: it also contains shared StatefulSets,
Dapr/RBAC/namespace resources, and other configuration outside the failed Deployment. After the scoped
rollback, verify sidecar/runtime compatibility and all canary/control checks.

A scoped Deployment rollback does not reverse CRDs, workflow history, index/schema changes,
Redis/FalkorDB data layouts, configuration written to durable state, or PVC contents.

Durable-data recovery uses retained logical exports and paired snapshots through
[Backup and Restore](./backup-restore.md) and [Disaster Recovery](./disaster-recovery.md). Never delete
Redis/FalkorDB PVCs as a normal rollback step. Forward recovery may be safer than downgrade when the old
binary cannot read a new data layout; decide this before the change.

Stop on unexplained render drift, missing backup/rehearsal, unsupported version/skew, workflow drain
uncertainty, replay-safety fail-open/stall, failed canary/control isolation, count/search/consistency
regression, persistence/PVC pressure, or any step whose reversal is unproven. Keep intake quiesced and
escalate from the last verified state.

## Escalation evidence

Provide change/release ID, revision, current/target inventory, compatibility/skew sources, image/render
digests, sanitized diff, Dapr CRD/control-plane/sidecar and workflow state, replay-safety events, backup/
rehearsal identities, rollout/Kubernetes events, health JSON, Redis/FalkorDB persistence/counts, canary/
control results, exact stop condition, rollback/data-recovery feasibility, and requested decision. Never
attach credentials, Secret values, content, or unrelated tenant evidence.

## Related runbooks and sources

- [Release Runbook](../dev/release-runbook.md)
- [Deployment Configuration](./deployment-configuration.md)
- [Backup and Restore](./backup-restore.md)
- [Disaster Recovery](./disaster-recovery.md)
- [Embedding Provider Migration](./embedding-providers.md#migration-runbook)
- [Tenant Onboarding and Offboarding](./tenant-onboarding-offboarding.md)
- [Incident Response](./incident-response.md)
- [Health Checks](../dev/health-checks.md)
- [Telemetry](../dev/telemetry.md)
- [`WorkflowReplaySafetyHostedService`](../../src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs)
- [Production Kustomize overlay](../../deploy/kubernetes/overlays/production/)
- [Dapr workflow versioning](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-versioning/)
- [Dapr Kubernetes upgrade](https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-upgrade/)
- [Kubernetes version-skew policy](https://kubernetes.io/releases/version-skew-policy/)
