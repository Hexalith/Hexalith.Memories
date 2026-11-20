# Index Rebuild and Recovery Decisions

## Purpose and scope

Owner: data-platform operations. Review cadence: quarterly and after consistency, index schema,
embedding migration, restore, tenant provisioning/deletion, or EventStore deduplication changes. Last
verified: 2026-07-15 against repository revision
`ca0a6266598e0f4231e2ff332d0e2131bfda75c6`.

This runbook covers bounded consistency repair, blue/green embedding migration, logical import and paired physical restore. A case-scoped import affects its selected case. Repair and migration can affect the tenant. A physical Redis/FalkorDB restore rewinds shared storage for every tenant. Tenant deletion permanently retires the identifier.

EventStore command/event history is authoritative for domain mutations; Redis and FalkorDB hold
projections/read models. The current consistency implementation still uses the syntactic Redis hash as
its repair input, so it cannot recreate a missing syntactic record. Never improvise a projection write
around these ownership boundaries.

## Prerequisites and authorization

- Assign a data owner, tenant owner, operator, change/incident ID, rollback owner, and observation
  window. Consistency verification is read-only; every repair, migration, restore,
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

- Keep the live tenant identity intact during recovery. Stop when
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
- Verify/inspect probes the NL sibling only for units enumerated from syntactic, raw-semantic, or graph
  storage and reports NL drift as an informational note. Tenant-wide enumeration does not discover an
  NL-only orphan, and repair planning neither rebuilds nor removes NL projections.
- Logical export/import does not capture or regenerate NL vector bytes or the generated description.
  If NL parity is required and no verified physical recovery point covers it, escalate under Path D.

### Ingestion-time readiness vs. provisioning ownership

`TenantProvisioningWorkflow` and its RediSearch/Redis Vector provisioning activities
own creation of the tenant's syntactic, raw semantic, and natural-language semantic
indexes. The tenant must complete provisioning and become active before normal
ingestion. See [Tenant Onboarding and Offboarding](./tenant-onboarding-offboarding.md)
for the lifecycle gates; ingestion is not a substitute for provisioning or a way to
reactivate a deleted/inactive tenant.

Before a hash/vector write, `IndexSyntacticActivity`, `IndexSemanticActivity`,
`IndexSemanticChunksActivity`, and `IndexNaturalLanguageSemanticActivity` call
`ITenantIndexReadinessVerifier`. The verifier checks the existing index with
`FT.INFO`; it never repairs a missing index with `FT.CREATE` and never writes the
hash/vector until readiness succeeds.

Successful readiness is memoized only in the current server process, keyed by tenant,
index family, and expected vector dimensions (syntactic uses no dimension). Concurrent
first writes for the same key share one verification. A server restart clears the
cache. Failures are never cached: a later write re-runs readiness after an operator has
restored the prerequisite. Incomplete `FT.INFO` metadata alone receives up to ten
non-blocking checks separated by 100 ms. If metadata is still incomplete after that
budget, the verifier fails closed with `TenantIndexSchemaMismatchException` (same as
genuine schema drift) and does not cache readiness. Missing indexes fail closed as
`TenantIndexNotProvisionedException`.

The only automatic schema changes are additive missing `TAG` fields, and only when
the actual field set is otherwise an exact subset of the expected schema:

| Index family | Fields allowed to be added with `FT.ALTER ... SCHEMA ADD ... TAG` |
|--------------|------------------------------------------------------------------|
| Syntactic | `cloudeventSubject`, `attributeTags` |
| Raw semantic | `cloudeventSubject` |
| Natural-language semantic | None |

An unexpected field, missing non-approved field, wrong prefix, or wrong vector
dimension remains `TenantIndexSchemaMismatchException`; an absent index becomes
`TenantIndexNotProvisionedException`. Neither path is cached as ready.

Use this operator decision path:

1. For a transient backend/readiness-verification failure, restore Redis health and
   retry ingestion; the failed check will execute again.
2. For an absent index on a tenant that did not finish provisioning, complete or
   retry the approved provisioning workflow for that live, non-erased identifier. Do not issue
   ingestion-owned `FT.CREATE`.
3. For an intentional provider/model/dimension transition, use Path B's blue/green
   migration. A dimension mismatch is not fixed by retrying readiness.
4. For incompatible drift or projection loss, stop intake and select the supported
   consistency or restore path in the matrix below. Do not use
   the additive upgrade allowance to conceal unrelated schema drift.

## Procedure

### 1. Select a supported path

| Observation / objective | Supported path | What it can change | Key stop condition | Recovery route |
|---|---|---|---|---|
| Need a read-only inventory or a bounded missing/orphan assessment | A — consistency verify/inspect | nothing | truncated/ambiguous evidence or unhealthy backend | restore backend health and repeat verify |
| Missing graph projection with syntactic source present; orphan raw-semantic/graph projection | A — explicit consistency repair | graph re-merge and raw-semantic/graph orphan removal | recommendation includes semantic re-index, missing syntactic source, NL drift, non-convergence, or widened scope | stop and evaluate C, then escalate under D if NL remains missing |
| Provider/model/dimensions or vector namespace transition | B — blue/green embedding migration | staged chunk vectors/indexes, aliases, tenant config | failures, marker owner mismatch, count/search parity failure | resume, abort, or owner-checked rollback while old targets remain |
| Known-good logical export and a clean tenant/case target exist | C1 — logical import | exported syntactic, raw-semantic, case, and graph state; not NL state | dirty target, manifest/config mismatch, workflow failure, missing NL recovery requirement, or failed canary | apply the logical restore stop conditions and preserve the live target |
| Coordinated shared-backend rollback is approved for every tenant | C2 — paired physical restore | all Redis/FalkorDB state at the retained boundary | unverified snapshot pair, layout mismatch, any tenant owner missing, or failed control tenant | keep intake frozen and restore the approved all-tenant recovery point or escalate |
| Full projection rebuild is required and original sources/events are available | D — escalate an unsupported full rebuild | none | recovery would require erasing the tenant or bypassing deduplication | preserve the live tenant and its evidence |

If no row fits, stop and escalate. Do not convert an unsupported semantic repair into a manual backend
operation.

### 2. Path A — verify, inspect, and bounded repair

1. Confirm `/ready` and backend health. Run read-only verify and inspect recommendations first:

   ```bash
   memories consistency verify --tenant "$TENANT_ID" --wait
   : "${MEMORY_UNIT_ID:?set an identifier reported by consistency verify}"
   memories consistency inspect --tenant "$TENANT_ID" --id "$MEMORY_UNIT_ID"
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

Choose logical import only for a clean target belonging to the same live, non-erased tenant. It can re-embed raw vectors
and consume provider capacity, but it does not restore NL vectors/descriptions and is therefore not a
complete route when NL parity is required. The clean-target guard rejects layering an older export over
existing state. On partial failure, freeze intake and follow the [logical restore stop conditions](./backup-restore.md#rollback-and-stop-conditions).

Choose paired physical restore only for a verified same-layout snapshot and an explicitly approved
all-tenant recovery. Quiesce every tenant/workflow, record every tenant owner/approver and control result,
and follow [Backup and Restore](./backup-restore.md) and [Disaster Recovery](./disaster-recovery.md).
Physical restore replaces shared durable state and must keep Redis/FalkorDB at one coordinated boundary.
Never copy a live, mutating AOF directory or restore only one backend without an explicitly accepted
reconciliation plan.

### 5. Path D — escalate a full rebuild

If Paths A–C cannot restore the required projections, stop intake for the affected tenant. Record the missing axes, available source data, workflow state, recovery point and the effect on a control tenant. Escalate to the data owner and incident commander. Ordinary replay can deduplicate an event whose projection is still missing. A full rebuild needs a rehearsal proving the live identity, deduplication guarantees and per-axis results survive. Erasure permanently retires the tenant ID.

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
  Evaluate C for missing syntactic/semantic data. Escalate under D if C cannot restore it.
- Path B: retain old targets and use the migration tool's resume/abort/rollback contract.
- Path C1: apply the [logical restore stop conditions](./backup-restore.md#rollback-and-stop-conditions). Keep a partially written target offline.
- Path C2: keep all-tenant intake frozen and restore only the approved paired recovery point. Do not
  combine arbitrary backend times or represent a shared-PVC rollback as tenant-scoped.
- Path D: keep intake frozen and the live identity intact until a recovery owner demonstrates a viable route for the affected projections.

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
