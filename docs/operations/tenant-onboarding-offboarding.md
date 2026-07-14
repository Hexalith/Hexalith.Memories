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
  MEMORIES_BASE_URL="${MEMORIES_BASE_URL:-https://memories.example.invalid}"
  TENANT_ID="${TENANT_ID:-tenant-canary}"
  DISPLAY_NAME="${DISPLAY_NAME:-Tenant Canary}"
  VECTOR_DIMENSIONS="${VECTOR_DIMENSIONS:-768}"
  : "${TOKEN:?TOKEN must be supplied by the approved identity flow}"
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
   location, then extract the returned workflow instance without displaying the bearer:

   ```bash
   RESPONSE_HEADERS="$(mktemp)"
   RESPONSE_BODY="$(mktemp)"
   trap 'rm -f "$RESPONSE_HEADERS" "$RESPONSE_BODY"' EXIT
   jq -n --arg tenantId "$TENANT_ID" --arg displayName "$DISPLAY_NAME" \
     --argjson vectorDimensions "$VECTOR_DIMENSIONS" \
     '{tenantId:$tenantId,displayName:$displayName,vectorDimensions:$vectorDimensions}' \
     | curl -fsS -X POST "$MEMORIES_BASE_URL/api/v1/tenants" \
         -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
         --data-binary @- -D "$RESPONSE_HEADERS" -o "$RESPONSE_BODY"
   WORKFLOW_ID="$(jq -er '.workflowInstanceId' "$RESPONSE_BODY")"
   STATUS_URL="$MEMORIES_BASE_URL/api/v1/tenants/$TENANT_ID/provision-status/$WORKFLOW_ID"
   printf 'tenant=%s workflow=%s statusUrl=%s\n' "$TENANT_ID" "$WORKFLOW_ID" "$STATUS_URL"
   ```

3. Poll the returned provisioning status with bounded backoff. Stop on failed/terminated workflow,
   timeout, compensation failure, Dapr/state-store failure, or a changed tenant scope. A terminal
   successful workflow is necessary but not sufficient.
4. Independently require the tenant registry state to be `Active`:

   ```bash
   curl -fsS "$MEMORIES_BASE_URL/api/v1/tenants/$TENANT_ID" \
     -H "Authorization: Bearer $TOKEN" | jq -e '.status == "Active"'
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

### Offboarding

1. Freeze new intake/publishers and drain or record in-flight workflows. Inventory cross-tenant graph/
   domain references; deletion does not remove references owned by another tenant automatically.
2. Obtain retention/legal and stakeholder approval. Produce a tenant logical export and a physically
   consistent paired Redis/FalkorDB backup per [Backup and Restore](./backup-restore.md). Verify both
   before deletion.
3. Submit authenticated delete and retain its returned `Location`/workflow ID:

   ```bash
   DELETE_HEADERS="$(mktemp)"
   DELETE_BODY="$(mktemp)"
   trap 'rm -f "$DELETE_HEADERS" "$DELETE_BODY"' EXIT
   curl -fsS -X DELETE "$MEMORIES_BASE_URL/api/v1/tenants/$TENANT_ID" \
     -H "Authorization: Bearer $TOKEN" -D "$DELETE_HEADERS" -o "$DELETE_BODY"
   WORKFLOW_ID="$(jq -er '.workflowInstanceId' "$DELETE_BODY")"
   STATUS_URL="$MEMORIES_BASE_URL/api/v1/tenants/$TENANT_ID/deletion-status/$WORKFLOW_ID"
   printf 'tenant=%s workflow=%s statusUrl=%s\n' "$TENANT_ID" "$WORKFLOW_ID" "$STATUS_URL"
   ```

4. Poll deletion status only while the registry entry exists. Treat workflow `Failed` or
   `CompensationFailed` as incomplete; after diagnosis, re-trigger the same DELETE route, whose current
   workflow is idempotent/resumable across already-cleaned axes.
5. A `404 TENANT_NOT_FOUND` from the status route is expected after successful registry removal but is
   not sufficient evidence. Never wait for a `Deleted` state. Verify all of the following independently:

   - `GET /api/v1/tenants/{tenantId}` returns `404 TENANT_NOT_FOUND` and tenant list omits the ID;
   - RediSearch/Vector active, staging, previous, and natural-language indexes/aliases for the tenant
     are absent;
   - read-only Redis scans find no `{tenantId}:mu:*`, `{tenantId}:vec:*`, `{tenantId}:vecnl:*`,
     `{tenantId}:case:*`, `{tenantId}:eventstore:*`, or `{tenantId}:embedding-migration:*` keys;
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
  control tests fail. Re-trigger only through the supported provisioning lifecycle after diagnosis.
- Offboarding before DELETE: unfreeze intake only if approvals and the tenant owner cancel the change.
- Offboarding after DELETE: do not attempt direct backend reversal. Keep intake/identity disabled and
  recover the same tenant ID through reprovision plus retained logical import or paired physical restore.
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
