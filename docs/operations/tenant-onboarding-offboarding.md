# Tenant Onboarding and Offboarding

## Purpose and scope

Owner: tenant lifecycle operations. Review cadence: quarterly and after route, authorization, tenant
workflow, embedding configuration, deletion cleanup, export/restore, or CLI-surface changes. Last
verified: 2026-07-14 at repository revision
`1553ee6708f644f3a4bc3638d3aaceed682b2371`.

This runbook executes the current authenticated, asynchronous tenant lifecycle contract. It covers one
tenant's registry entry, physical Redis indexes/prefixes, FalkorDB graph, tenant state, provider/secret
references, quota, canary, and handoff/retirement evidence. Backend capacity and lifecycle mistakes can
affect all tenants, so the platform operator must confirm shared-backend headroom and isolation.

The shipped CLI supports tenant listing only. Create, provision-status, verify, delete, and
deletion-status operations use the REST paths in `MemoriesRoutes`. The typed REST client supports
`CreateTenantAsync(tenantId, displayName, cancellationToken)` but has no dimensions parameter; it sends
the contract default of 768. Use the REST body when another dimension is required.

## Prerequisites and authorization

- Onboarding approvals: tenant/business owner, identity owner for claim mapping, platform/data owner for
  capacity and dimensions, and provider/secret owner for quota and secret references.
- Offboarding approvals: tenant/business owner, retention/legal owner, data owner, identity/secret owner,
  and platform operator. Record retention, export destination, deletion window, and rollback limits.
- All lifecycle calls require the deployment's approved bearer/OIDC path and a principal authorized for
  the target tenant. Acquire the bearer from the approved identity flow; never print or persist it in
  shell history/evidence.
- Initialize non-secret scope and require the secret token without assigning an example value:

  ```bash
  set -euo pipefail
  MEMORIES_BASE_URL="${MEMORIES_BASE_URL:-https://memories.example.invalid}"
  MEMORIES_BASE_URL="${MEMORIES_BASE_URL%/}"
  TENANT_ID="${TENANT_ID:-tenant-canary}"
  DISPLAY_NAME="${DISPLAY_NAME:-Tenant Canary}"
  VECTOR_DIMENSIONS="${VECTOR_DIMENSIONS:-768}"
  POLL_INTERVAL_SECONDS="${POLL_INTERVAL_SECONDS:-5}"
  POLL_DEADLINE_SECONDS="${POLL_DEADLINE_SECONDS:-900}"
  : "${TOKEN:?TOKEN must be supplied by the approved identity flow}"
  case "$TENANT_ID" in
    ''|*[!A-Za-z0-9-]*) printf 'invalid tenant id\n' >&2; exit 1 ;;
  esac
  printf 'base=%s tenant=%s dimensions=%s\n' "$MEMORIES_BASE_URL" "$TENANT_ID" "$VECTOR_DIMENSIONS"
  ```

- Validate `VECTOR_DIMENSIONS` against the selected provider/model and the accepted range 1–4096.
- Confirm physical capacity/quota, tenant ID/claim mapping, external Secret reference ownership, and no
  conflicting tenant/graph/index before create.
- Tenant delete is destructive and non-reversible without retained export/backup. Stop when approvals,
  original scope, backup consistency, workflow state, or cross-tenant reference ownership is uncertain.

## Signals and evidence

Retain the sanitized request scope, HTTP status/`Location`, workflow instance ID/runtime status/output,
registry state, embedding provider/model/dimensions and secret *reference*, isolation verification,
index/graph evidence, canary ingest/search results, queue/error/telemetry evidence, and handoff or deletion
approvals. Redact bearer tokens, Secret values/manifests, content, users, and unrelated tenant data.

Canonical lifecycle paths:

| Operation | Method and path | Success contract |
|---|---|---|
| Create | `POST /api/v1/tenants` | `202 Accepted`, provisioning workflow status `Location` |
| Provision status | `GET /api/v1/tenants/{tenantId}/provision-status/{instanceId}` | workflow state; poll to terminal completion |
| Independent tenant state | `GET /api/v1/tenants/{tenantId}` | tenant `status` must be `Active` before handoff |
| Isolation verify | `POST /api/v1/tenants/{tenantId}/verify` | all required checks pass |
| Delete | `DELETE /api/v1/tenants/{tenantId}` | `202 Accepted`, deletion workflow status `Location` |
| Deletion status | `GET /api/v1/tenants/{tenantId}/deletion-status/{instanceId}` | available only while the registry entry exists |

An initial 202 proves scheduling, not completion. Successful deletion removes the registry entry, so
subsequent deletion-status and tenant requests can return `404 TENANT_NOT_FOUND`. There is no durable
`Deleted` tenant state to await.

## Procedure

### Onboarding

1. Record identity-to-tenant claim mapping, provider/model/dimensions, secret reference, provider quota,
   shared capacity, retention owner, and expected physical index/graph names.
2. Submit the exact create contract. Capture headers and body in an access-controlled temporary
   location, require the current `202 Accepted` contract, and use the server-returned `Location`. Do not
   synthesize a status URL or continue after a missing/malformed response:

   ```bash
   RESPONSE_HEADERS="$(mktemp)"
   RESPONSE_BODY="$(mktemp)"
   trap 'rm -f "$RESPONSE_HEADERS" "$RESPONSE_BODY"' EXIT HUP INT TERM
   HTTP_STATUS="$(
     jq -n --arg tenantId "$TENANT_ID" --arg displayName "$DISPLAY_NAME" \
       --argjson vectorDimensions "$VECTOR_DIMENSIONS" \
       '{tenantId:$tenantId,displayName:$displayName,vectorDimensions:$vectorDimensions}' \
       | curl -sS -X POST "$MEMORIES_BASE_URL/api/v1/tenants" \
           -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
           --data-binary @- -D "$RESPONSE_HEADERS" -o "$RESPONSE_BODY" \
           --write-out '%{http_code}'
   )"
   test "$HTTP_STATUS" = 202 || { printf 'tenant create returned HTTP %s\n' "$HTTP_STATUS" >&2; exit 1; }
   WORKFLOW_ID="$(jq -er '.workflowInstanceId | select(type == "string" and length > 0)' "$RESPONSE_BODY")"
   RETURNED_LOCATION="$(awk 'tolower($1) == "location:" { sub(/\r$/, "", $2); print $2; exit }' "$RESPONSE_HEADERS")"
   EXPECTED_LOCATION="/api/v1/tenants/$TENANT_ID/provision-status/$WORKFLOW_ID"
   test "$RETURNED_LOCATION" = "$EXPECTED_LOCATION" || { printf 'unexpected provisioning Location\n' >&2; exit 1; }
   STATUS_URL="$MEMORIES_BASE_URL$RETURNED_LOCATION"
   printf 'tenant=%s workflow=%s statusUrl=%s\n' "$TENANT_ID" "$WORKFLOW_ID" "$STATUS_URL"
   ```

3. Poll the returned provisioning status to a bounded deadline. `runtimeStatus` can be serialized as a
   name or Dapr ordinal, so accept only the known terminal forms. Stop on failed/canceled/terminated
   workflow, HTTP/JSON failure, timeout, Dapr/state-store failure, or changed tenant scope:

   ```bash
   POLL_DEADLINE_EPOCH=$(( $(date +%s) + POLL_DEADLINE_SECONDS ))
   while :; do
     HTTP_STATUS="$(curl -sS "$STATUS_URL" -H "Authorization: Bearer $TOKEN" \
       -o "$RESPONSE_BODY" --write-out '%{http_code}')"
     test "$HTTP_STATUS" = 200 || { printf 'provision status returned HTTP %s\n' "$HTTP_STATUS" >&2; exit 1; }
     RUNTIME_STATUS="$(jq -er '.runtimeStatus | if type == "number" then tostring else . end' "$RESPONSE_BODY")"
     case "$RUNTIME_STATUS" in
       Completed|3) break ;;
       Failed|5|Canceled|6|Terminated|7)
         printf 'provision workflow stopped in %s\n' "$RUNTIME_STATUS" >&2; exit 1 ;;
     esac
     test "$(date +%s)" -lt "$POLL_DEADLINE_EPOCH" || { printf 'provision polling timed out\n' >&2; exit 1; }
     sleep "$POLL_INTERVAL_SECONDS"
   done
   ```

   A Dapr workflow can be `Completed` while its typed result reports `Failed` or `CompensationFailed`;
   runtime completion is necessary but not sufficient. If the registry is `Failed`, confirm compensation
   completed before correcting the cause and submitting a supported provisioning retry. If it is
   `CompensationFailed`, inventory the reported orphaned axes, keep the tenant disabled, and use the
   authenticated DELETE lifecycle to clean them. Verify complete registry/index/graph/state absence before
   attempting a fresh create; never delete orphaned backend state manually.
4. Independently require the tenant registry state to be `Active`:

   ```bash
   curl -fsS "$MEMORIES_BASE_URL/api/v1/tenants/$TENANT_ID" \
     -H "Authorization: Bearer $TOKEN" | jq -e '.status == "Active"'
   rm -f "$RESPONSE_HEADERS" "$RESPONSE_BODY"
   trap - EXIT HUP INT TERM
   ```

5. Configure/verify provider, model, dimensions, secret-store reference, allowed-secret scope, and
   provider quota through [Embedding Providers](./embedding-providers.md). Never send a secret value in
   tenant configuration.
6. Run tenant isolation verification and require every result to pass:

   ```bash
   curl -fsS -X POST "$MEMORIES_BASE_URL/api/v1/tenants/$TENANT_ID/verify" \
     -H "Authorization: Bearer $TOKEN" | jq -e '.allPassed == true'
   ```

7. Create an approved canary case, ingest a non-sensitive canary through the canonical routes, poll its
   workflow, and verify syntactic/semantic/hybrid search plus graph behavior when applicable. Verify
   tenant-scoped telemetry, rate-limit/quota behavior, queue progress, and absence from a control tenant.
8. Hand off the tenant ID, owner, identity mapping, dimensions/provider/secret reference, quota,
   capacity assumptions, canary results, dashboards/runbooks, escalation contacts, and review cadence.

### Graph isolation evidence boundary

`GraphIsolation` is structural database-existence evidence only: the runtime verifier confirms that
the tenant-named database appears in `GRAPH.LIST`; it does not query graph content. Independent
execution of the real-backend method
`TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`, which
seeds colliding graph identifiers for two tenants and traverses each through its authenticated tenant
context, is required for content-isolation evidence. From the repository root, build the integration
assembly first:

```bash
dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false
```

If Dapr placement and scheduler are not exposed at the CLI defaults (`localhost:50005` and
`localhost:50006`), discover the active local service endpoints from the local Dapr or container
runtime configuration and export them before running the proof:

```bash
export MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=<active-placement-host:port>
export MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=<active-scheduler-host:port>
```

Then run the exact proof invocation:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes
```

Record this result separately from the authenticated canary traversal in onboarding step 7. Neither a successful
`GRAPH.LIST` lookup nor an onboarding canary substitutes for the two-tenant collision proof.

### Offboarding

1. Freeze new intake/publishers and require every in-flight workflow to reach a terminal state, or execute
   an explicitly approved cancellation/disposition that prevents late writes. Merely recording active
   workflows is not a deletion boundary. Inventory cross-tenant graph/domain references; deletion does
   not remove references owned by another tenant automatically.
2. Obtain retention/legal and stakeholder approval. Produce a tenant logical export and a physically
   consistent paired Redis/FalkorDB backup per [Backup and Restore](./backup-restore.md). Verify both
   before deletion.
3. Submit authenticated delete and retain its returned `Location`/workflow ID:

   ```bash
   DELETE_HEADERS="$(mktemp)"
   DELETE_BODY="$(mktemp)"
   trap 'rm -f "$DELETE_HEADERS" "$DELETE_BODY"' EXIT HUP INT TERM
   HTTP_STATUS="$(curl -sS -X DELETE "$MEMORIES_BASE_URL/api/v1/tenants/$TENANT_ID" \
     -H "Authorization: Bearer $TOKEN" -D "$DELETE_HEADERS" -o "$DELETE_BODY" \
     --write-out '%{http_code}')"
   test "$HTTP_STATUS" = 202 || { printf 'tenant delete returned HTTP %s\n' "$HTTP_STATUS" >&2; exit 1; }
   WORKFLOW_ID="$(jq -er '.workflowInstanceId | select(type == "string" and length > 0)' "$DELETE_BODY")"
   RETURNED_LOCATION="$(awk 'tolower($1) == "location:" { sub(/\r$/, "", $2); print $2; exit }' "$DELETE_HEADERS")"
   EXPECTED_LOCATION="/api/v1/tenants/$TENANT_ID/deletion-status/$WORKFLOW_ID"
   test "$RETURNED_LOCATION" = "$EXPECTED_LOCATION" || { printf 'unexpected deletion Location\n' >&2; exit 1; }
   STATUS_URL="$MEMORIES_BASE_URL$RETURNED_LOCATION"
   printf 'tenant=%s workflow=%s statusUrl=%s\n' "$TENANT_ID" "$WORKFLOW_ID" "$STATUS_URL"
   ```

4. Poll deletion status only while the registry entry exists, using the same bounded interval/deadline.
   A `404 TENANT_NOT_FOUND` ends polling only when the structured error code matches; it starts the
   independent absence checks and is not completion by itself:

   ```bash
   POLL_DEADLINE_EPOCH=$(( $(date +%s) + POLL_DEADLINE_SECONDS ))
   while :; do
     HTTP_STATUS="$(curl -sS "$STATUS_URL" -H "Authorization: Bearer $TOKEN" \
       -o "$DELETE_BODY" --write-out '%{http_code}')"
     if test "$HTTP_STATUS" = 404; then
       jq -e '.code == "TENANT_NOT_FOUND"' "$DELETE_BODY" >/dev/null
       break
     fi
     test "$HTTP_STATUS" = 200 || { printf 'deletion status returned HTTP %s\n' "$HTTP_STATUS" >&2; exit 1; }
     RUNTIME_STATUS="$(jq -er '.runtimeStatus | if type == "number" then tostring else . end' "$DELETE_BODY")"
     case "$RUNTIME_STATUS" in
       Completed|3) break ;;
       Failed|5|Canceled|6|Terminated|7)
         printf 'delete workflow stopped in %s\n' "$RUNTIME_STATUS" >&2; exit 1 ;;
     esac
     test "$(date +%s)" -lt "$POLL_DEADLINE_EPOCH" || { printf 'deletion polling timed out\n' >&2; exit 1; }
     sleep "$POLL_INTERVAL_SECONDS"
   done
   ```

   Treat a surviving registry state of `Failed` or `CompensationFailed` as incomplete; after diagnosis,
   re-trigger the same DELETE route, whose current workflow is idempotent/resumable across already-cleaned
   axes.
5. A `404 TENANT_NOT_FOUND` from the status route is expected after successful registry removal but is
   not sufficient evidence. Never wait for a `Deleted` state. Verify all of the following independently:

   - `GET /api/v1/tenants/{tenantId}` returns `404 TENANT_NOT_FOUND` and tenant list omits the ID;
   - RediSearch/Vector active, staging, previous, and natural-language indexes/aliases for the tenant
     are absent;
   - read-only Redis scans find no `{tenantId}:mu:*`, `{tenantId}:vec:*`, `{tenantId}:vecnl:*`, legacy
     `{tenantId}:vec:nl:*`, `{tenantId}:case:*`, `dedup:{tenantId}:*`,
     `{tenantId}:eventstore:*`, or `{tenantId}:embedding-migration:*` keys—the complete key families owned
     by the current deletion activity;
   - `GRAPH.LIST` omits the tenant graph and workflow deletion evidence records graph/state/registry
     cleanup;
   - an authorized stale tenant request is denied/not found and cannot search or mutate data; and
   - a control tenant remains operational and isolated.

6. Reconcile external cross-tenant references with their owners. Only after deletion verification,
   retire externally managed Kubernetes/Dapr/provider secret material and identity/quota assignments.
7. Record the export/backup retention disposition, deletion workflow/result, independent absence
   evidence, stale-access denial, external secret retirement, unresolved cross-references, and approvers.

Telemetry and ordinary access logs are operational evidence, not automatically a tamper-evident
compliance/audit record. Preserve any compliance evidence through the organization's approved system.

## Verification and evidence

Onboarding is complete only when workflow success, independent `Active`, provider/dimensions/secret
reference, physical isolation, canary ingest/search, telemetry, quota, capacity, control-tenant denial,
and handoff evidence all pass. Offboarding is complete only after accepted deletion, independent
registry/index/graph/state absence, stale-access denial, retained recovery evidence, cross-reference
disposition, and post-verification secret retirement.

Record actual UTC times and results; do not turn pending workflow, missing telemetry, a 202, or a 404
alone into success. Retain redacted HTTP statuses/locations, workflow IDs/states, isolation results,
index/graph/state inventories, canary/control results, backup identities, approvals, and ownership
handoff/retirement evidence.

## Rollback, recovery, and stop conditions

- Onboarding: stop before handoff if provisioning is non-terminal, tenant is not independently Active,
  dimensions/provider/indexes disagree, isolation fails, provider quota is insufficient, or canary/
  control tests fail. A `Failed` tenant may use the supported provisioning retry only after compensation
  evidence is complete. A `CompensationFailed` tenant requires orphan-axis inventory and supported DELETE
  cleanup with full absence verification before a fresh create.
- Offboarding before DELETE: unfreeze intake only if approvals and the tenant owner cancel the change.
- Offboarding after DELETE: do not attempt direct backend reversal. Keep intake/identity disabled and
  recover the same tenant ID through reprovision plus retained logical import. A paired physical snapshot
  of the shared Redis/FalkorDB backends may be restored only into an isolated recovery environment to
  extract/import this tenant, or through an explicitly approved all-tenant disaster-recovery operation;
  never restore it in place as a single-tenant rollback.
- Failed/compensation-failed deletion: preserve the workflow ID and cleaned-axis evidence, fix the
  blocking dependency, and re-trigger DELETE through the idempotent recovery path.

Stop immediately on tenant-scope ambiguity, missing/invalid backup, cross-tenant data suspicion,
unexpected surviving data, control-tenant regression, or a request to delete external secrets before
absence verification.

## Escalation evidence

Provide lifecycle/change ID, tenant and owner, revision/image/config, identity mapping, dimensions/
provider/secret reference (never the value), quota/capacity evidence, HTTP statuses/locations, workflow
IDs/states/output, registry/index/graph/state evidence, canary/control results, export and paired-backup
identities, cross-reference findings, approvals, stop point, and requested decision. Redact credentials,
content, user identities, and unrelated tenants.

## Related runbooks and sources

- [Route Surface](./route-surface.md)
- [Deployment Configuration](./deployment-configuration.md)
- [Embedding Providers](./embedding-providers.md)
- [Rate Limiting](./rate-limiting.md)
- [Capacity Planning](./capacity-planning.md)
- [Backup and Restore](./backup-restore.md)
- [Disaster Recovery](./disaster-recovery.md)
- [Incident Response](./incident-response.md)
- [Index Rebuild](./index-rebuild.md)
- [Telemetry](../dev/telemetry.md)
- [`MemoriesRoutes`](../../src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs)
- [`TenantLifecycleEndpoints`](../../src/Hexalith.Memories.Server/Endpoints/TenantLifecycleEndpoints.cs)
