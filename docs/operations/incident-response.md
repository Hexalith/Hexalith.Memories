# Incident Response

## Purpose and scope

Owner: incident-response lead. Review cadence: quarterly and after every severity 0/1 incident or
material health, telemetry, deployment, workflow, provider, or recovery change. Last verified:
2026-07-15 against repository revision `ca0a6266598e0f4231e2ff332d0e2131bfda75c6`.

This runbook defines first response, containment, recovery routing, verification, and escalation for
Hexalith.Memories. Classify blast radius before changing anything: one tenant, one case/workflow,
one search axis/backend, multiple tenants, or all tenants. Redis/Dapr state failures can affect all
tenants; FalkorDB or Redis Search/Vector failures may leave unaffected axes safely available.

Suspected cross-tenant data exposure, authorization bypass, or secret exposure is severity 0 even if
availability appears healthy. Preserve evidence, stop the affected access path, engage security and
tenant owners, and do not continue normal diagnosis through exposed data.

### Severity, timing, and roles

| Severity | Definition | Acknowledge / commander assigned | Communication |
|---|---|---|---|
| SEV0 critical | suspected tenant/secret exposure, destructive corruption, or broad unrecoverable loss | 5 minutes / immediately | security, platform, data, product, and affected tenant owners |
| SEV1 high | all-tenant outage, complete tenant outage with no safe unaffected path, Dapr/state-store failure, sustained ingestion halt, or imminent data loss | 10 minutes / 10 minutes | platform/data/provider owners and stakeholders every 30 minutes |
| SEV2 medium | tenant- or axis-scoped degradation with a safe unaffected path | 30 minutes / 30 minutes | affected owner; hourly until stable |
| SEV3 low | bounded defect with workaround and no active loss/security risk | 1 business day / named owner | normal operations channel |

The incident commander owns decisions and communications; the operations lead runs commands; the
scribe records a UTC timeline and redacted evidence; backend/provider/security/tenant owners approve
specialist or destructive actions. One person must not both authorize and execute a destructive
recovery unless emergency policy explicitly permits it and records the exception.

## Prerequisites and authorization

- Establish an incident ID, commander, scribe, severity, approved scope, communication channel, and
  evidence location before mutation. Read-only evidence capture may begin immediately.
- Initialize non-secret scope and confirm it aloud before tenant/backend actions:

  ```bash
  NAMESPACE=hexalith-memories
  TENANT_ID="${TENANT_ID:-incident-scope-unset}"
  CASE_ID="${CASE_ID:-case-scope-unset}"
  WORKFLOW_ID="${WORKFLOW_ID:-workflow-scope-unset}"
  INCIDENT_ID="${INCIDENT_ID:-incident-$(date -u +%Y%m%dT%H%M%SZ)}"
  printf 'incident=%s namespace=%s tenant=%s case=%s workflow=%s\n' \
    "$INCIDENT_ID" "$NAMESPACE" "$TENANT_ID" "$CASE_ID" "$WORKFLOW_ID"
  ```

- Use approved cluster, identity-provider, Dapr, backend, and embedding-provider access. Never echo,
  copy, or attach tokens, passwords, Secret manifests, memory content, or raw user identifiers.
- Destructive steps require the incident commander, data owner, and affected tenant owner; restore or
  rebuild additionally requires a verified pre-change backup and rollback/recovery route.
- Safe stop conditions apply at every step: scope is uncertain, evidence conflicts, backup identity is
  missing, tenant isolation is suspect, the proposed action could affect another tenant, or a command
  would edit Redis/FalkorDB/Dapr state directly.

## Signals and evidence

Capture a bounded UTC timeline with deployment revision/image digests, Kubernetes events/restarts,
structured `/health`, `/alive`, and `/ready` JSON, Dapr/control-plane and application logs, workflow
state/status, provider status/rate-limit responses, queue/progress metrics, failed-unit status,
capacity/persistence signals, and the last known-good paired Redis/FalkorDB backup identities.

The health contract is structured, not status-code-only:

- `/alive`: restart-oriented; `Unhealthy` returns HTTP 503.
- `/ready`: traffic/capability-oriented; backend `Degraded` returns HTTP 200, while Dapr sidecar/state
  store `Unhealthy` returns 503.
- `/health`: union diagnostic; `Healthy` and `Degraded` return 200, `Unhealthy` returns 503.
- Parse `schemaVersion`, top-level `status`, every entry `status`, and `affectedCapabilities`.

Collect it from every Server pod without exposing the application token. `wget -S` emits the HTTP
status line; the explicit `if` prevents an expected 503 from aborting later endpoints or pods. Keep the
bounded output in the incident evidence store, not a shared terminal transcript:

```bash
for pod in $(kubectl -n "$NAMESPACE" get pod \
  -l app.kubernetes.io/name=memories -o jsonpath='{.items[*].metadata.name}'); do
  kubectl -n "$NAMESPACE" exec "$pod" -- sh -ec '
    for path in health alive ready; do
      output=""
      if output=$(wget -S -O- --header="dapr-api-token: ${APP_API_TOKEN}" \
          "http://127.0.0.1:8080/${path}" 2>&1); then
        exit_code=0
      else
        exit_code=$?
      fi
      printf "pod=%s path=/%s wgetExit=%s\n%s\n" "$HOSTNAME" "$path" "$exit_code" "$output"
    done'
done
```

For each pod/path, retain the HTTP status and a parseable JSON body with the required schema fields.
If the body is absent/malformed, `schemaVersion` is unsupported, or any required entry/capability field
cannot be interpreted, stop mutations and treat health as unknown/unhealthy until the contract is
recovered. Do not route recovery from a status line alone.

Useful application signals include `memories.ingestion.documents`, `memories.ingestion.failures`,
`memories.search.duration`, `memories.rate.limit.rejections`, `memories.pipeline.queue.depth`,
`memories.natural.language.embedding.queue.depth`, `memories.handlers.mismatches`, and
`memories.handlers.observations.dropped`. Missing OTLP data is unknown evidence, not health.

For import/restore failures, retain the structured code without the payload. Current families are
`IMPORT_SCHEMA_VERSION_UNSUPPORTED`, `IMPORT_SCOPE_MISMATCH`, `IMPORT_TENANT_MISMATCH`,
`IMPORT_CASE_MISMATCH`, `IMPORT_TOO_LARGE`, `IMPORT_ABORTED`, `IMPORT_EMPTY`,
`IMPORT_MANIFEST_UNREADABLE`, `RESTORE_STATUS_NOT_FOUND`, `RESTORE_TARGET_BUSY`, and
`RESTORE_TARGET_NOT_CLEAN`. For re-ingestion retain
`NON_URL_REINGESTION_UNAVAILABLE`; for infrastructure/provider pressure retain
`DAPR_UNAVAILABLE`, `RATE_LIMIT_EXCEEDED`, and the provider response category. Use the
[`ErrorMessageCatalog`](../../src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs) for cataloged
operator messages. `NON_URL_REINGESTION_UNAVAILABLE` is emitted by
[`ReIngestionCoordinator`](../../src/Hexalith.Memories.Server/Ingestion/ReIngestionCoordinator.cs) and
is not currently cataloged; preserve its structured code and escalate instead of inventing guidance.

## Procedure

### 1. Declare, scope, and contain

1. Assign severity/roles and record first-response time.
2. Determine whether symptoms are tenant, case/workflow, backend/axis, multi-tenant, or all-tenant.
3. For isolation or secret suspicion, stop the affected ingress/identity/tenant access path, preserve
   logs and authorization evidence, rotate exposed credentials through the secret owner, and escalate
   as SEV0. Do not query other tenants to "compare" data.
4. Otherwise, start read-only. Evaluate every degraded entry and take the union of its
   `affectedCapabilities`; do not stop after the first matching row. Retain healthy search axes only
   when an approved ingress/feature control actually blocks every affected flow and a probe proves the
   block. No gateway automatically routes on `affectedCapabilities`. If caller compliance or the
   capability mapping cannot be proved, fail closed by pausing the full search/mutation scope rather
   than advertising partial service.

### 2. Follow the decision tree

| Observation | Read-only confirmation | Containment and next path |
|---|---|---|
| Pod not started within 60 seconds, restart loop, or `/alive` 503 | Every pod's events, previous logs, image/config/Secret references, startup JSON | Stop rollout. For an image or Pod-template fault, restore the prior rendered workload. For a mutable ConfigMap/Secret reference, reapply its previously approved rendered value through change control and restart the workload; `rollout undo` alone reuses the current external configuration. Do not delete PVCs. |
| `/ready` 503 with `dapr-sidecar` or `dapr-statestore` unhealthy | Dapr control plane/sidecar health, component errors, state-store reachability | Pause new writes/workflows because orchestration/actor durability is affected; route to deployment/failure recovery. |
| `/ready` 200 with `redisearch` degraded | Entry capabilities and syntactic/hybrid errors | Disable syntactic-dependent flows; semantic/graph-only service may continue if verified. |
| `/ready` 200 with `redis-vector` degraded | Entry capabilities, module/index status, semantic errors | Disable semantic/hybrid-semantic flows; syntactic/graph-only service may continue if verified. |
| `/ready` 200 with `falkordb` degraded | Entry capabilities, `GRAPH.LIST`, graph query errors | Disable graph traversal/graph-scoped search; retain verified non-graph axes. |
| Provider errors, 429s, or rising rate-limit rejections | Provider status/quota, configured provider/model, retry telemetry | Reduce intake; preserve queued work; follow rate-limit/provider guidance. Do not bypass the tenant rate limiter. |
| Queue nonzero/rising with no successful progress | Compare queue gauges with ingestion-success deltas, workflow states, retry logs | Pause intake, identify the stalled stage, preserve payload TTL constraints, then use workflow/failure recovery. Depth alone is not a stall. |
| Failed ingestion registry grows | Case failed-units API, codes/stages, source-payload retention | Use the supported re-ingestion API; when retained bytes expired, republish from the original source. Never prune state during triage. |
| Healthy pods but zero EventStore ingestion | `/dapr/subscribe`, pub/sub delivery logs, rendered Server environment | Confirm `MEMORIES_EVENTSTORE_TOPIC` exists and matches routing. It has no runtime fallback outside AppHost injection; fix deployment config through review. |
| Consistency/isolation discrepancy | Tenant-scoped verify/inspect result and affected axes | For isolation suspicion use SEV0 containment. Otherwise use read-only consistency verify and the supported repair decision runbook. |
| PVC, RSS, OOM, `noeviction`, AOF/RDB, or rewrite pressure | Kubernetes/PVC metrics plus Redis `INFO memory`/`INFO persistence` | Stop intake/load before writes are rejected or recovery workspace is exhausted; use capacity/backup guidance. |
| Missing telemetry or dashboard series | OTLP endpoint/exporter logs, collector/scrape health, direct health JSON | Open an observability incident; do not infer service health from missing data. |
| Import/restore error | Structured error code, manifest scope/version, workflow status, target readiness | Stop retries that could amplify cost; follow backup/restore. Never edit staging, workflow, Redis, or graph state manually. |
| Multiple or unrecognized degraded entries | Union of every entry's `affectedCapabilities`, direct axis probes, schema version | Apply every matching containment row. If any capability is unknown or cannot be blocked and verified, pause the full request scope and escalate; do not infer an unaffected path. |

### 3. Recover through the owning procedure

- Retry/re-ingest and failed-unit behavior: [Failure Recovery](./failure-recovery.md).
- Data-plane backup, import, and physical restore: [Backup and Restore](./backup-restore.md).
- Pod/PVC/cluster loss: [Disaster Recovery](./disaster-recovery.md).
- Consistency and index decisions: [Index Rebuild](./index-rebuild.md) and
  [Consistency](../dev/consistency.md).
- Capacity pressure: [Capacity Planning](./capacity-planning.md).
- Provider/rate limit: [Embedding Providers](./embedding-providers.md) and
  [Rate Limiting](./rate-limiting.md).

Do not run manual backend edits, `FT.DROPINDEX`, ingestion-owned `FT.CREATE`, graph deletes, Dapr state
edits, or workflow-history changes. Recovery proceeds only after diagnosis, verified backup, explicit
authorization, and a feasible rollback/recovery route.

### 4. Communicate and follow up

Communicate severity, confirmed scope, customer-visible behavior, unaffected capabilities, containment,
next decision time, and uncertainty. After recovery, open a post-incident review for SEV0/1 and any
incident with data, security, repeated stall, or restore impact; assign corrective actions and verify
runbook/alert updates.

## Verification and evidence

Recovery is complete only when:

- structured `/alive`, `/ready`, and `/health` match the intended capability state for the full
  observation window;
- the affected tenant/case and an independent control tenant pass authorized search/ingestion checks
  without cross-tenant data;
- queues/workflows make sustained progress and failed-unit growth has stopped;
- backend counts/consistency, provider success, PVC/persistence status, and telemetry export are
  independently verified where relevant;
- security/secret containment has a security-owner decision; and
- communications record residual degradation and the rollback point honestly.

Retain sanitized health JSON, metric queries/results, bounded logs, error codes, workflow/correlation
IDs, deployment/image revision, backup identities, approvals, commands, UTC timeline, verification
results, and follow-up owners. Redact secrets, tokens, content, user identities, and unrelated tenants.

## Rollback, recovery, and stop conditions

Stop an action when scope expands, a control tenant regresses, evidence contradicts the hypothesis,
readiness becomes more severe, queues cease progress, backup verification is missing, or the action
requires direct state edits. Return to the last safe containment state and escalate.

Stateless image/Pod-template rollback uses the prior rendered artifact or `kubectl rollout undo` for the
affected Deployment. A mutable ConfigMap, Secret reference, or external provider setting must be restored
to its prior approved rendered value separately before pods restart; rollout history does not version those
objects. Neither route reverses durable state, schemas, indexes, or PVC contents. Durable recovery uses
retained paired snapshots/logical exports through the linked procedures; PVC deletion is never a normal
rollback step. Resume intake only after verification and commander approval.

## Escalation evidence

Escalate with incident/severity, UTC timeline, owner/approvers, exact tenant/case/backend blast radius,
revision/image digests, structured health entries, bounded redacted logs, metrics and NoData state,
workflow/correlation IDs, error codes, provider status, PVC/persistence evidence, backup identities,
actions/results, safe stop point, and next requested decision. Never attach credentials, Secret objects,
memory content, or another tenant's evidence.

## Related runbooks and sources

- [Deployment Configuration](./deployment-configuration.md)
- [Failure Recovery](./failure-recovery.md)
- [Backup and Restore](./backup-restore.md)
- [Disaster Recovery](./disaster-recovery.md)
- [Index Rebuild](./index-rebuild.md)
- [Capacity Planning](./capacity-planning.md)
- [Monitoring and Alerting Thresholds](./monitoring-alerting-thresholds.md)
- [Health Checks](../dev/health-checks.md)
- [Telemetry](../dev/telemetry.md)
- [Consistency](../dev/consistency.md)
- [Route Surface](./route-surface.md)
- [Pipeline Persistence](./pipeline-persistence.md)
