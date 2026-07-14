# Index Rebuild and Recovery Decisions

## Purpose and scope

Owner: data-platform operations. Review cadence: quarterly and after consistency, index schema,
embedding migration, restore, tenant provisioning/deletion, or EventStore deduplication changes. Last
verified: 2026-07-14 at repository revision
`1553ee6708f644f3a4bc3638d3aaceed682b2371`.

There is no generic index-rebuild command. This runbook selects only the recovery paths supported by
the current implementation: bounded consistency verify/repair, blue/green embedding migration,
logical import or physical restore, and destructive tenant reprovisioning followed by original-source
re-ingestion/republishing. It covers one tenant and its syntactic, semantic, natural-language semantic,
and FalkorDB graph projections. Shared Redis/FalkorDB pressure can expand the blast radius to all
tenants.

EventStore command/event history is authoritative for domain mutations; Redis and FalkorDB hold
projections/read models. The current consistency implementation still uses the syntactic Redis hash as
its repair input, so it cannot recreate a missing syntactic record. Never improvise a projection write
around these ownership boundaries.

## Prerequisites and authorization

- Assign a data owner, tenant owner, operator, change/incident ID, rollback owner, and observation
  window. Consistency verification is read-only; every repair, migration, restore, delete/reprovision,
  or republish action requires tenant/data-owner approval.
- Before any mutation, take and verify a logical export and a physically consistent paired Redis/
  FalkorDB backup. Record both snapshot identities/timestamps and the restore rehearsal evidence.
- Quiesce or coordinate tenant intake and in-flight workflows. Confirm no migration marker or other
  owner is active before changing aliases or tenant lifecycle state.
- Initialize and confirm only non-secret scope:

  ```bash
  NAMESPACE=hexalith-memories
  TENANT_ID="${TENANT_ID:-rebuild-canary}"
  CANARY_CASE_ID="${CANARY_CASE_ID:-rebuild-canary-case}"
  CHANGE_ID="${CHANGE_ID:-rebuild-$(date -u +%Y%m%dT%H%M%SZ)}"
  printf 'change=%s namespace=%s tenant=%s canaryCase=%s\n' \
    "$CHANGE_ID" "$NAMESPACE" "$TENANT_ID" "$CANARY_CASE_ID"
  ```

- Destructive steps are limited to the approved restore or tenant delete/reprovision paths. Stop when
  scope, backup, workflow state, active/previous aliases, original source availability, or tenant
  isolation is uncertain.

## Signals and evidence

Capture revision/image digests, tenant embedding config, active/staging/previous index aliases,
RediSearch `FT.INFO` counts/failures/memory, chunked vector key counts, graph node/edge counts,
consistency verify/inspect output, workflow/migration status, per-axis canary search results, backup
identities, and health JSON. Evidence must be tenant-scoped and redact tokens, connection strings,
content, user identifiers, and Secret values.

Current limitations and ownership are decision inputs:

- Consistency verify/inspect is read-only presence analysis, not EventStore history replay.
- Supported repair can re-merge a missing graph node and remove orphan semantic/graph data. It does
  not rebuild a missing syntactic record.
- `SemanticIndexer.ReIndexFromSyntacticAsync` currently throws `NotSupportedException`; semantic
  re-index and combined semantic/graph re-index recommendations fail rather than silently repair.
- Semantic storage is chunked as `{tenantId}:vec:{memoryUnitId}:{sequence}`. A source-document count
  is not a stored-vector count.
- Tenant provisioning owns RediSearch/Vector index creation. Ingestion verifies readiness; it does not
  own `FT.CREATE`.
- EventStore replay with the same CloudEvent ID normally hits permanent deduplication and returns the
  existing unit. There is no `forceReplay` bypass. A replay into an existing tenant is not a rebuild.
- Blue/green migration retains active, staging, previous targets and switches raw/NL aliases together.
  Preserve old targets until verification and the rollback window finish.

## Procedure

### 1. Select a supported path

| Observation / objective | Supported path | What it can change | Key stop condition | Recovery route |
|---|---|---|---|---|
| Need a read-only inventory or a bounded missing/orphan assessment | A — consistency verify/inspect | nothing | truncated/ambiguous evidence or unhealthy backend | restore backend health and repeat verify |
| Missing graph projection with syntactic source present; orphan semantic/graph projection | A — explicit consistency repair | graph re-merge and orphan removal | recommendation includes semantic re-index, missing syntactic source, non-convergence, or widened scope | stop; restore/re-ingest via C/D |
| Provider/model/dimensions or vector namespace transition | B — blue/green embedding migration | staged chunk vectors/indexes, aliases, tenant config | failures, marker owner mismatch, count/search parity failure | resume, abort, or owner-checked rollback while old targets remain |
| Known-good logical export or paired physical snapshot exists | C — import/restore | projection state from retained evidence | manifest/config mismatch, unverified snapshot pair, or failed canary | abandon additive import or restore prior retained snapshot |
| Full projection rebuild is required and original sources/events are available | D — tenant reprovision then controlled re-ingestion/republishing | all tenant projections and tenant-local lifecycle state | missing approval/export/backup/source, cross-tenant references, or control-tenant regression | restore original tenant from retained evidence |

If no row fits, stop and escalate. Do not convert an unsupported semantic repair into a manual backend
operation.

### 2. Path A — verify, inspect, and bounded repair

1. Confirm `/ready` and backend health. Run read-only verify and inspect recommendations first:

   ```bash
   memories consistency verify --tenant "$TENANT_ID" --wait
   memories consistency inspect --tenant "$TENANT_ID" --id "${MEMORY_UNIT_ID:-canary-unit}"
   ```

2. Review enumeration/discrepancy truncation, every recommendation, and current backend health.
3. Approve repair only for the supported graph re-merge/orphan-removal cases. Run the normal explicit
   confirmation path and poll to terminal state:

   ```bash
   memories consistency repair --tenant "$TENANT_ID" --yes --wait
   ```

4. Stop on `ReIndexSemantic`, `ReIndexSemanticAndGraph`, `Unrepairable`, failed actions, or a three-pass
   non-convergence result. Missing syntactic data requires restore/re-ingestion; semantic repair remains
   unsupported. Do not loop repairs to hide a failure.

### 3. Path B — blue/green embedding migration

Use [`tools/MigrateEmbeddingVectors`](../../tools/MigrateEmbeddingVectors/) only through the
[Embedding Provider Migration Runbook](./embedding-providers.md#migration-runbook). Follow dry-run,
live, resume, abort, rollback, and final dry-run steps there. Record target provider/model/dimensions,
chunk/vector counts, marker/owner, active/staging/previous indexes, raw/NL alias targets, failures, and
canary search.

Do not delete previous targets or aliases after cutover until count, dimensions, attribution, per-axis
search, canary ingestion, workflow progress, and the observation window pass. If verification fails,
use the tool's owner-checked rollback/abort; do not alter aliases manually.

### 4. Path C — logical import or physical restore

Choose logical import for portable/scope-aware recovery and paired physical restore for a verified
same-layout snapshot. Follow [Backup and Restore](./backup-restore.md) and
[Disaster Recovery](./disaster-recovery.md). Coordinate intake/workflows, verify target provisioning and
embedding attribution, canary first, poll asynchronous restore, and compare syntactic units, stored
chunks/vectors, graph nodes/edges, consistency, and search axes.

Logical import is additive/idempotent but can re-embed and consume provider capacity. Physical restore
replaces durable state and must keep Redis/FalkorDB at one coordinated boundary. Never copy a live,
mutating AOF directory or restore only one backend without an explicitly accepted reconciliation plan.

### 5. Path D — destructive full tenant rebuild

1. Freeze intake; drain or record in-flight workflows; inventory cross-tenant references and external
   secret ownership. Confirm original source/event republishing is feasible.
2. Produce logical export and paired physical backup, rehearse the recovery route, and obtain tenant,
   data, retention/legal, and incident/change approvals.
3. Delete the tenant only through authenticated `DELETE /api/v1/tenants/{tenantId}` and poll while the
   registry exists. Follow the independent absence verification in
   [Tenant Onboarding and Offboarding](./tenant-onboarding-offboarding.md).
4. Recreate the tenant through authenticated provisioning with the intended dimensions, wait for the
   workflow to complete, and independently verify `Active`. Provisioning—not ingestion—creates the
   indexes and graph.
5. Restore retained data or republish/re-ingest original sources through supported APIs/workflows. A
   simple replay into the old tenant would have been deduplicated; there is no force-replay switch.
6. Start with the tenant-scoped canary. Compare counts and search/consistency behavior before expanding.
   Preserve the pre-change export/snapshots through the rollback window.

Never run manual `FT.DROPINDEX`, ingestion-owned `FT.CREATE`, raw graph/Redis deletes, dedup-key edits,
or a made-up generic rebuild command.

### 6. Verify every path

For the canary and then the approved tenant scope, compare:

- syntactic source-unit count and sample field integrity;
- semantic stored-chunk/vector count, dimensions, provider/model attribution, and raw/NL active alias;
- graph node/edge counts and tenant-scoped traversal;
- consistency verify/inspect outcomes;
- syntactic, semantic, hybrid, and graph per-axis search results/latency;
- ingestion/workflow progress, failures, health entries, and an independent control tenant.

## Verification and evidence

Completion requires a terminal workflow/tool result, no unexplained discrepancies, count/parity evidence
for every affected axis, successful tenant canary and control tenant, healthy or explicitly accepted
degraded JSON, no active migration/repair owner left behind, and a retained feasible rollback/recovery
route. `Command exited 0` alone is not verification.

Retain the change ID, approvals, revision/image/config, path decision, pre-change export and paired
snapshot identities, intake/workflow coordination evidence, sanitized commands/results, active/staging/
previous alias map, counts, consistency results, per-axis searches, stop decisions, and observation-window
sign-off. Never retain credentials or unredacted content in the shared packet.

## Rollback, recovery, and stop conditions

- Path A: verification needs no rollback; stop repair on unsupported recommendations or non-convergence.
  Recover missing syntactic/semantic data through C or D.
- Path B: retain old targets and use the migration tool's resume/abort/rollback contract.
- Path C: stop additive logical import before consumer handoff or restore the retained known-good paired
  snapshot. Do not combine arbitrary backend times.
- Path D: keep intake frozen and restore the original tenant from retained logical/physical evidence if
  provisioning, republishing, parity, isolation, or canary verification fails.

Stop immediately on tenant-isolation suspicion, unknown alias owner, missing backup/source, direct-state
edit requirement, control-tenant regression, write rejection, or expanding blast radius. Escalate rather
than deleting the active index to make a partial repair appear clean.

## Escalation evidence

Provide tenant/change/incident scope, revision/image/config, selected/rejected matrix paths, approvals,
health JSON, consistency recommendations/actions, exact unsupported failure, active/staging/previous alias
map, source/chunk/vector/graph counts, workflow/migration status, backup identities, canary/control results,
and safe stop point. Redact tokens, connection strings, Secret values, content, and unrelated tenants.

## Related runbooks and sources

- [Consistency Verification and Repair](../dev/consistency.md)
- [EventStore Integration](../dev/eventstore-integration.md)
- [Embedding Provider Migration](./embedding-providers.md#migration-runbook)
- [Backup and Restore](./backup-restore.md)
- [Disaster Recovery](./disaster-recovery.md)
- [Tenant Onboarding and Offboarding](./tenant-onboarding-offboarding.md)
- [Incident Response](./incident-response.md)
- [Deployment Configuration](./deployment-configuration.md)
- [Health Checks](../dev/health-checks.md)
- [`IndexSchemaDefinitions`](../../src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs)
- [`SemanticIndexer`](../../src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs)
