# Failure Recovery & Re-Ingestion (Story 6.3)

This page documents the ingestion-pipeline observability and recovery layer that ships in Story 6.3:
configurable per-activity retry, the durable failed-units registry, the per-case ingestion counter
actor, and the operator-driven re-ingestion endpoints.

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

Every workflow that exhausts retries writes a durable record before re-throwing — NFR19 (never silently
dropped) is enforced by **persistence**, not just by an event on the activity stream.

Key shapes:

| Key | Purpose |
|-----|---------|
| `{tenantId}:failed-unit:{memoryUnitId}` (HASH) | Full failure context — source URI, stage, error code/message, retry count, timestamps, JSON-serialized `FailureDetails`. |
| `{tenantId}:case:{caseId}:failed-units` (ZSET) | Per-case index, scored by `failedAt` unix-ms; supports `O(log N)` recency-ordered pagination. |

Hash + ZADD execute in one Lua round-trip — no half-write.

**Retention:** failed units accumulate indefinitely (NFR19). Operators must re-ingest (which deletes the
record on success) or, for outright pruning, delete via the Redis CLI. A `DELETE /failed-units/{id}`
endpoint is Phase 2.

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
POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest
POST /api/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest
   body: { "memoryUnitIds": ["m1","m2"] }   OR
         { "all": true, "limit": 500 }
```

Per-unit flow:

1. **Read** the failed-unit hash → `FailedUnitRecord`.
2. **Atomically claim**: delete the hash + sorted-set entry + dedup key in one Lua call. If the hash
   was already gone (concurrent re-ingestion), return **409 Conflict** (`RE_INGESTION_IN_PROGRESS`).
3. **Rebuild** an `IngestionInput` from the persisted record (`ContentBytes` is **not** stored — the
   new workflow re-fetches/re-extracts from `SourceUri`).
4. **Schedule** a new `IngestionWorkflow` instance, passing `memoryUnitId` as the DAPR workflow
   `instanceId`. The workflow's existing `context.InstanceId`-based memory-unit-id fallback picks it
   up — **annotations and graph edges survive** because the id is preserved.

Bulk endpoint enumerates per-unit outcomes; one missing/conflicted/error unit does **not** abort the
batch. The endpoint returns 200 OK with a `BulkReIngestionResponse` listing each outcome — only request
validation (400) or missing case (404) abort.

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
  `POST /api/ingest` of the same source URI hits the duplicate short-circuit. To re-ingest a deleted
  unit, use the failed-units re-ingest endpoint (which clears the key). This asymmetry is intentional.
