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
