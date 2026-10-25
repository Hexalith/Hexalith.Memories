# Failure Recovery & Re-Ingestion

This page retains the Story 6.3 ingestion-pipeline recovery contract and records its
Epic 23 verification corrections: captured retry behavior, failed-unit persistence,
retained non-URL source payloads, and operator-driven re-ingestion.

## Per-Activity Retry Configuration

Retry behavior is controlled per activity class via `appsettings.json`:

```jsonc
"Ingestion": {
  "RetryPolicies": {
    "GenerateEmbeddingActivity": {
      "MaxAttempts": 5,
      "FirstRetryIntervalSeconds": 2.0,
      "BackoffCoefficient": 1.5,
      "MaxRetryIntervalSeconds": 300.0
    }
  }
}
```

- Keys are activity class names (`nameof(...)`); missing keys use the default
  `(MaxAttempts=5, FirstRetryInterval=2s, Backoff=1.5, MaxInterval=5min)`.
- `MaxAttempts <= 0` fails fast at startup with `RETRY_CONFIG_INVALID` — no silent weakening.
- Hot-reload is **not supported** in MVP. Config changes require a restart.
- Compensation retries (cleanup activities) are operationally tuned and not exposed; do not edit.

## Failed-Units Registry

Every workflow that reaches its terminal failure path attempts to write a durable
record before re-throwing. Failed-unit persistence is best effort: if that write also
fails, event 6309 records the persistence failure without masking the original
workflow exception. Operators must treat that log as a recovery-evidence gap.

Key shapes:

| Key | Purpose |
|-----|---------|
| `{tenantId}:failed-unit:{memoryUnitId}` (HASH) | Full failure context — source URI, stage, error code/message, retry count, timestamps, JSON-serialized `FailureDetails`, and an internal source-payload reference for supported non-URL failures. |
| `{tenantId}:case:{caseId}:failed-units` (ZSET) | Per-case index, scored by `failedAt` unix-ms; supports `O(log N)` recency-ordered pagination. |

Hash + ZADD execute in one Lua round-trip — no half-write.

**Retention has two different bounds.** The failed-unit hash and case sorted-set row
have no automatic TTL; they remain until a re-ingestion claim removes them or an
operator-managed backend action. A scheduling failure restores the claimed record.
The source bytes referenced by a supported non-URL
failure are separate state and carry a Dapr TTL of
`max(1, Ingestion:WorkflowPayloadStore:TtlHours)` hours: the default is **24 hours**
and non-positive configured values become **1 hour**. The implementation does not
apply an upper clamp.

The registry's internal `SourcePayloadReference` field stores only an opaque,
tenant-scoped source reference, never raw file bytes or event JSON. A normal non-URL
scheduling path first moves source bytes into the payload store; on terminal failure,
that source reference can be retained while
derived extraction, chunk-text, and vector payloads remain transient and are cleaned
up on a best-effort basis. URL fetch payloads are transient because URL re-ingestion
refetches. Legacy or direct scheduling paths without a valid source reference cannot
be reconstructed after failure.

After source-payload expiry, or when a record never had a valid retained source,
re-ingestion returns `NON_URL_REINGESTION_UNAVAILABLE` and leaves the failed-unit
hash, case sorted-set row, and dedup key untouched. Ingest again from the original
file, event, annotation, command, or projection source. Do not copy or expose the
opaque internal reference as a substitute for the original source.

## `FailedCount` vs `FailedUnitsPage.TotalCount`

These two metrics intentionally diverge — operators MUST understand the difference:

- **`CaseStatusDetail.FailedCount`** — **historical** count of `IngestionFailed` events recorded on the
  case activity stream. Monotonically increasing within a case lifetime; never decreases. Driven by
  `CaseActivityService.GetFailedCountAsync`.
- **`FailedUnitsPage.TotalCount`** — **currently-unresolved** count of failed units whose hash still
  exists in Redis. Decreases on successful re-ingestion (claim removes the hash + sorted-set entry) or
  explicit delete.

So an operator who re-ingests all 50 failures successfully sees `TotalCount=0` but `FailedCount=50` —
the case **had** 50 failures historically and **has** 0 unresolved. **Not a bug.**

## Re-Ingestion Contract

```
POST /api/v1/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest
POST /api/v1/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest
   body: { "memoryUnitIds": ["m1","m2"] }   OR
         { "all": true, "limit": 500 }
```

Per-unit flow:

1. **Read** the failed-unit hash → `FailedUnitRecord`.
2. **Validate non-URL source payload availability before claim.** URL records skip
   this because the server fetches the URL again. File, directory, annotation,
   command/projection, and event records require a readable reference with the
   expected tenant, payload kind, and memory-unit/dedup scope. Validation failure
   returns `NON_URL_REINGESTION_UNAVAILABLE` before any registry or dedup deletion.
3. **Atomically claim**: delete the hash + sorted-set entry + dedup key in one Lua call. If the hash
   was already gone (concurrent re-ingestion), return **409 Conflict** (`RE_INGESTION_IN_PROGRESS`).
4. **Rebuild** an `IngestionInput` from the persisted record. URL records refetch
   from `SourceUri`; supported non-URL records schedule with `ContentBytes = null`
   and the validated retained source reference.
5. **Schedule** a new `IngestionWorkflow` instance, passing `memoryUnitId` as the DAPR workflow
   `instanceId`. The workflow's existing `context.InstanceId`-based memory-unit-id fallback picks it
   up — **annotations and graph edges survive** because the id is preserved.
6. If scheduling fails after claim, restore the complete original failed-unit record:
   tenant/case/source/audit fields, failure details and timestamps, metadata,
   causation/correlation identifiers, and the optional source reference. If restore
   itself fails, the coordinator raises a combined failure for operator escalation.

Bulk endpoint enumerates per-unit outcomes; one missing, conflicted, unsupported, or
errored unit does **not** abort the batch. Unsupported sources keep the explicit
`unsupported-source-payload` outcome and `NON_URL_REINGESTION_UNAVAILABLE` code; they
are not collapsed into generic scheduling errors. The endpoint returns 200 OK with a
`BulkReIngestionResponse` listing each outcome — only request validation (400) or a
missing case (404) aborts the request.

`IngestedBy` is preserved from the failed-unit record (audit trail), not the caller's identity.

## `CaseIngestionCounterActor`

Per-case DAPR actor (Actor ID = `"{tenantId}:{caseId}"`) maintaining four `int` buckets:

```
Queued | Extracting | Embedding | Indexing
```

`Indexed` and `Failed` counts are **not** in the actor — they continue to source from FalkorDB
(node count) and the activity stream (event count) respectively.

- O(1) read via `GetCountsAsync` — no fan-out, no 1000-instance cap, **no `IsApproximate` flag**.
- Each transition carries a `transitionId = "{instanceId}:{sequence}"` so workflow replay re-invocations
  are **idempotent** (the actor stores `LastTransitionId` and treats matching ids as no-ops).
- Decrements are **zero-clamped** — defensive against catastrophic actor-side drift.
- Counter actor unreachable on a status read → `CaseService.GetCaseStatusAsync` reports zero in-flight
  counts and logs at Warning. The status endpoint never fails because of counter-actor drift.

## Structured Log Events 6301–6310

| ID | Level | Name | Fields |
|----|-------|------|--------|
| 6301 | Debug | RetryAttemptStarted | activityName, memoryUnitId, attempt (best-effort) |
| 6302 | Warning | RetryExhausted | activityName, memoryUnitId, finalErrorCode |
| 6303 | Information | FailedUnitPersisted | tenantId, memoryUnitId, stage, errorCode |
| 6304 | Information | ReIngestionScheduled | tenantId, caseId, memoryUnitId, newWorkflowInstanceId |
| 6305 | Warning | BulkReIngestionUnitSkipped | tenantId, memoryUnitId, reason |
| 6306 | Debug | FailedUnitsListQueried | tenantId, caseId, limit, offset, returnedCount, totalCount |
| 6307 | Debug | CounterActorTransitionApplied | tenantId, caseId, previousStage, nextStage, transitionId |
| 6308 | Debug | CounterActorTransitionIdempotent | tenantId, caseId, transitionId |
| 6309 | Error | FailedUnitPersistenceFailed | memoryUnitId, reason |
| 6310 | Warning | CounterTransitionFailed | tenantId, caseId, previousStage, nextStage, reason |

## Known MVP Limitations

- No per-tenant retry policy override (global only).
- No TTL / automatic retention of failed units.
- No `DELETE /failed-units/{id}` endpoint (Phase 2).
- `RetryPolicyBuilder` is not hot-reloadable.
- `FailureDetails.RetryCount` is per-workflow-instance, not cumulative across re-ingestions.
- Re-ingestion of a unit whose source is gone (file deleted, URL 404) fails identically — same
  memory-unit-id, retry-count reset to the new instance's budget.
- Dedup key is **not** cleared on `DELETE /memory-units/{id}` for indexed units — re-ingestion via
  `POST /api/v1/ingest` of the same source URI hits the duplicate short-circuit. To re-ingest a deleted
  unit, use the failed-units re-ingest endpoint (which clears the key). This asymmetry is intentional.

## Operational runbooks

- Access-telemetry queue, retry, actor/reminder, clock, adapter-fault, and
  reclamation recovery are separate from ingestion reprocessing. Follow
  [Access Telemetry Lifecycle Operations](./access-telemetry-lifecycle.md) and
  its [PostgreSQL 18.4 appendix](./access-telemetry-adapter-production.md).

- [Rate limiting and provider recovery](./rate-limiting.md)
- [Directory ingestion](./directory-ingestion.md)
- [Ingestion workflow determinism (contributor)](../dev/ingestion-workflow-determinism.md)
- [Capacity planning](./capacity-planning.md)
- [Incident response](./incident-response.md)
- [Index rebuild and recovery decisions](./index-rebuild.md)
- [Tenant onboarding and offboarding](./tenant-onboarding-offboarding.md)
- [Upgrade and migration](./upgrade-migration.md)
- [Monitoring and alerting thresholds](./monitoring-alerting-thresholds.md)


## Natural-language erasure and legacy reconciliation

Unit and case cleanup install permanent unit tombstones before removing Redis
or graph content. Both natural-language namespaces are deleted. The legacy
namespace migrator copies, checks and retires a scoped source hash in one atomic
unit/source-fenced operation; it preserves a newer valid canonical target.
Drain older unfenced writers before a deployed migration or erasure cutover.

Retry queue admission, attempt increments and overflow moves observe the same
unit and tenant fences atomically. Cleanup removes exact-unit fields and old
JSON members from both live and dead queues. New retry workflows carry no raw
payload and resolve content inside a single activity, keeping raw and derived
intermediates out of durable workflow history. Historical retry workflows are
terminated, physically purged and read back before cleanup succeeds. An absent
workflow client or unconfirmed termination/purge blocks completion. New instance
IDs hash an unambiguous tenant/unit tuple in a namespace disjoint from historic
concatenated IDs. Cleanup checks authoritative stored input before terminating
or purging either address. A historic address belonging to another scope is
left intact; missing, malformed or duplicate scope fields block cleanup.
Matching legacy instances fail closed by default with
`nl-retry-legacy-scheduler-quiescence-required`. After the owner verifies all
old retry schedulers have stopped and cannot restart or reuse a historic ID,
set `NaturalLanguage:LegacyRetrySchedulersDrainedForErasure=true` to qualify
legacy reconciliation. Keep that qualification valid throughout cleanup and
remove the opt-in if legacy scheduling can resume. Dapr's instance-ID mutation
API has no conditional execution token. Modern cleanup and proven foreign
legacy nonmutation need no opt-in; current schedulers never create legacy IDs. Producer
failure reconciliation uses the same workflow cleanup dependency.

Semantic cleanup uses bounded exact-key provenance (maximum 4,096 retained
keys). Modern publication writers register every materialized key. For legacy
units, an exact-unit native TAG lookup discovers additional obligations and
validates index schema, tenant prefix, background readiness, indexing failures,
key scope and `ON_TIMEOUT=FAIL`. An optional TAG cannot prove that all historical
hashes populated it: unknown coverage remains incomplete even after a successful
query. Missing-TAG chunks remain erasure obligations. The adapter never scans
the Redis keyspace to infer completeness and never changes server configuration.

A qualified owner must drain older writers and reconcile the entire historical
unit inventory, including keys lacking the optional TAG, into its exact key set
before recording `legacy-owner-reconciled-v1` provenance. This marker is an
explicit owner attestation; automatic TAG discovery cannot issue it. Older
`legacy-complete` markers issued by automatic discovery are rejected. No local
test grants production owner approval or inventory completeness. An incomplete
canonical NL target with valid legacy content blocks migration with
`nl-namespace-canonical-owner-reconciliation-required`; the valid legacy hash
is retained for explicit repair.

JSON-only legacy live retry members are atomically promoted to current payload
storage under unit/tenant fences before scheduling. The activity requires the
canonical syntactic unit before description/embedding work and before indexing,
and valid overflow moves always enforce the configured dead-payload cap.

The integration fixtures optionally accept owned native RedisSearch/FalkorDB
processes when `MEMORIES_TEST_OWNED_NATIVE=1` and the two
`MEMORIES_TEST_REDISSTACK_CONNECTION` / `MEMORIES_TEST_FALKORDB_CONNECTION`
variables each select an unprivileged `127.0.0.1` port. The caller owns process
startup, `ON_TIMEOUT=FAIL`, persistence and cleanup.
`RetryWorkflowErasureIntegrationTests` uses the existing owned Dapr sidecar
fixture against the collection's Redis resource and has both `Integration` and
`IntegrationSlow` traits. The maintained nightly slow lane initializes Dapr
placement/scheduler and runs these assertions without optional endpoint skips.
The class verifies actual content-bearing legacy description activity history,
termination/purge, a late activity completion and worker/Redis-adapter recreation,
in addition to scoped identity collisions and the retained timer controls.

Semantic deletion also validates stored hash ownership atomically. Legacy NL
names overlap valid raw-semantic names for colon-containing unit identifiers in
both directions. Known foreign owners survive; missing ownership, incompatible
types or conflicting tenant metadata require explicit reconciliation before
cleanup can complete. A qualified owner must resolve stored ownership as well
as complete the exact-key inventory; a key-shaped name alone is insufficient.
Unit/case deletion qualification invokes the actual activities with retry queues
and durable history present. Tenant-wide retry cleanup and malformed historical
retry addressing remain separately tracked work.
