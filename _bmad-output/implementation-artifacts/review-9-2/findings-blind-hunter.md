# Blind Hunter Findings (committed-only diff, story 9.2)

## 1. `CleanupSemanticActivity` log placeholder ordering is broken
- **Location:** `src/Hexalith.Memories.Server/Activities/Indexing/CleanupSemanticActivity.cs`
- **Severity:** High
- **Evidence:** Template uses `{Key}, {Deleted}, {NlKey}, {NlDeleted}, {MemoryUnitId}`; downstream scrapers that cached the old signature read mismatched structured fields.
- **Fix direction:** Rename structured fields to `RawKey/RawDeleted/NlKey/NlDeleted`.

## 2. `PersistInMetadata` branch is statically unreachable
- **Location:** `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` + `GenerateNaturalLanguageDescriptionActivity.cs`
- **Severity:** High
- **Evidence:** Activity hardcodes `estimatedConfidence = null, confidenceSource = Constant`. Workflow gate requires `EstimatedConfidence is float measuredConfidence`. Therefore `event.naturalLanguageDescription` metadata is never written regardless of `PersistInMetadata` flag. Tests stub non-null confidence, which is unreachable production-side.
- **Fix direction:** Persist description independently of confidence presence; use nullable confidence field.

## 3. `OrphanSemanticIndexReconciler` delete-by-pattern is a cross-tenant footgun
- **Location:** `src/Hexalith.Memories.Server/Hosting/OrphanSemanticIndexReconciler.cs`
- **Severity:** Critical
- **Evidence:** Tenant IDs ending in `nl` (e.g. `tenant-final-nl`) collide with `NaturalLanguageSemanticIndexSuffix = ":memories:vec:nl"`. On startup, reconciler could drop that tenant's raw vector index with `DD`, destroying semantic hashes.
- **Fix direction:** Match on colon-delimited segments with unique terminators; unit test `nl`-ending tenant IDs.

## 4. `FailedNaturalLanguageEmbeddingRegistry` ZREM by exact JSON equality
- **Location:** `src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs`
- **Severity:** High
- **Evidence:** `SortedSetRemoveAsync(LiveKey, existingJson)` matches byte-equal members. Byte drift from JSON source-gen version bumps silently fails removal; transaction commits new incremented record → unbounded queue growth with duplicates.
- **Fix direction:** Key entries by stable ID (memoryUnitId as member, JSON in parallel hash), or assert `removed==true` and retry.

## 5. `FileSystemComponentYamlReader.TryParseDuration` greedy unit match
- **Location:** `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptionsValidator.cs`
- **Severity:** Medium
- **Evidence:** Parser uses greedy letter loop + switch on `m`, `ms`, `s`, `h`. `"500m"` is silently parsed as 500 minutes when author likely meant 500 ms. Gates privacy-critical ack validator.
- **Fix direction:** Use ISO8601 or explicit parser; unit test `"500m"`.

## 6. `WorkflowReplaySafetyHostedService.TryGetWorkflowName` reflection on non-existent property
- **Location:** `src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs`
- **Severity:** High
- **Evidence:** Reflection looks for public `Name` property on `WorkflowState`. In Dapr.Workflow ≥ 1.17 the property is typically `WorkflowName`. Reflection returns null → `ShouldCountWorkflow` always returns false → gate is a silent no-op.
- **Fix direction:** Bind to actual SDK type; test against real `WorkflowState` fake; fail loud if property missing.

## 7. Replay-safety gate fails open on sidecar unreachable
- **Location:** `src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs`
- **Severity:** High
- **Evidence:** `if (inFlight is null) return;` — sidecar unreachable is the exact rolling-deployment scenario the gate exists to protect against. "Runbook quiesce" is not a code control.
- **Fix direction:** Fail closed for a bounded window; env-var escape hatch; couple to sidecar-health readiness.

## 8. Duration histogram double-counts failures without outcome tag
- **Location:** `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs`
- **Severity:** Medium
- **Evidence:** `finally { Stopwatch.Stop(); RecordNaturalLanguageDescriptionDuration(...); }` records duration on all paths. Timeouts always show as 15000 ms dominating p99; no outcome/success tag.
- **Fix direction:** Add `result` tag (`success` / `timeout` / `grpc` / `http` / `dapr` / `cleaner-rejected`), or record only on success.

## 9. `TryApplyMaxTokenHint` silently swallows reflection failure
- **Location:** `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs`
- **Severity:** Medium
- **Evidence:** Under Dapr.AI SDK version drift, `Parameters` rename → token capping stops silently → LLM cost blows past envelope with no telemetry.
- **Fix direction:** Log single Warning on first reflection failure; emit `max_tokens_applied=false` metric; unit test asserting one candidate path succeeds.

## 10. `IndexGraphActivity.TryEmitStubResolvedTelemetry` mislabels `CausingEventId`
- **Location:** `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs`
- **Severity:** Medium
- **Evidence:** `LogStubResolved(..., input.CausationId ?? string.Empty, ...)` — field named as causing event, but when the resolution was triggered by a root event's MERGE, `CausationId` is not the correct resolver identity.
- **Fix direction:** Rename field to `resolverEventId = input.MemoryUnitId`, or document semantic precisely.

## 11. `NaturalLanguageEmbeddingRetryHostedService` dequeue-before-schedule is not crash-safe
- **Location:** `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedService.cs`
- **Severity:** High
- **Evidence:** `DequeueBatchAsync` is read-only (`SortedSetRangeByRank`). Crash between schedule and finalize leaves record in queue; next tick re-dequeues same record; if workflow actually completed, `CompleteAsync` finally runs — but attempts can silently exceed `MaxRetryAttempts` via legitimate completions the service never observed.
- **Fix direction:** `ZPOPMIN` or lease pattern (in-flight set with TTL, reclaim on restart).

## 12. `EventType` metadata lookup may use wrong key casing
- **Location:** `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`
- **Severity:** Medium
- **Evidence:** `input.Metadata.TryGetValue("cloudevent.type", ...)` — if Story 9.1 mapper writes different casing, every `SourceType.Event` ingest receives `eventType = "(unknown)"`; LLM prompt degrades silently. Nothing asserts key exists.
- **Fix direction:** Pin Metadata dictionary comparer `OrdinalIgnoreCase`; centralize key constants; log Information when key missing.

## 13. `NaturalLanguageConsistencyState` discrepancy with `NoOp` recommendation contradicts repair contract
- **Location:** `src/Hexalith.Memories.Server/Consistency/NaturalLanguageConsistencyState.cs` + `ConsistencyVerificationWorkflow.cs`
- **Severity:** High
- **Evidence:** Discrepancy surfaced but recommendation is `NoOp` → repair planner skips. Metrics show "1 inconsistent" but payload says "no repair needed". Tests pin confusing shape as correct.
- **Fix direction:** Add `ConsistencyRepairRecommendation.RepairNaturalLanguageSemantic`; route through existing planner.

## 14. `OrphanSemanticIndexReconciler.ExecuteAsync` runs once, never rechecks
- **Location:** `src/Hexalith.Memories.Server/Hosting/OrphanSemanticIndexReconciler.cs`
- **Severity:** Medium
- **Evidence:** `ExecuteAsync` runs once and exits; post-startup SIGKILL-during-provisioning leaves orphan until next pod restart. Comment claims it handles that case.
- **Fix direction:** Interval loop (24h) inside `while (!stoppingToken.IsCancellationRequested)`; or pair to compensation path directly.

## 15. `DeleteRedisVectorIndexActivity` missing `DD` flag on DropIndex
- **Location:** `src/Hexalith.Memories.Server/Activities/Tenants/DeleteRedisVectorIndexActivity.cs`
- **Severity:** High
- **Evidence:** `db.FT().DropIndex(nlIndexName)` — sibling `DeleteRedisVectorActivity` uses `ExecuteAsync("FT.DROPINDEX", nlIndexName, "DD")`. Without `DD`, every `{tenant}:vec:nl:*` hash leaks on tenant deletion compensation, defeating NFR11 tenant isolation.
- **Fix direction:** Switch to `ExecuteAsync("FT.DROPINDEX", nlIndexName, "DD")`.

## 16. Duration histogram cardinality policy forbids `outcome` label
- **Location:** `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs` + `TelemetryMetricsRecorder.cs`
- **Severity:** Medium
- **Evidence:** Tag policy: `[NaturalLanguageDescriptionDurationName] = new[] { "tenant_id" }`. Forbids outcome tag at recorder level; SLO queries can never split success vs timeout.
- **Fix direction:** Add `outcome` tag to policy and emit on every record.

## 17. `OrphanSemanticIndexReconciler.ReadIndexNames` swallows InvalidCastException silently
- **Location:** `src/Hexalith.Memories.Server/Hosting/OrphanSemanticIndexReconciler.cs`
- **Severity:** Low
- **Evidence:** Returns `[]` on `InvalidCastException`; future Redis Stack `FT._LIST` shape change silently reports "0 indexes swept" — indistinguishable from healthy empty.
- **Fix direction:** Log Warning with observed type; metric counter.

## 18. Tests mutate process-global singletons without collection-level isolation
- **Location:** `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/NaturalLanguageDescriptionOptionsValidatorTests.cs` + `Workflows/IngestionWorkflowDualEmbeddingTests.cs`
- **Severity:** Medium
- **Evidence:** `NaturalLanguageDescriptionOptionsSnapshot.ResetToDefaults()` and `Environment.SetEnvironmentVariable(CacheAckEnvVar, ...)` mutate static state. Multiple test classes mutate across parallel runs → order-dependent flakes.
- **Fix direction:** Single `[Collection(...)]` with disabled parallelization for all tests that touch process-global state.

## 19. `FalkorDbSemanticAttributeProcessor` hostname resolution races Aspire endpoint publication
- **Location:** `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`
- **Severity:** Medium
- **Evidence:** `ResolveFalkorDbHostnames` executes at first-span resolution; fallback "if Count==1, try GetEndPoints()" is a non-determinism code smell. Tests do not cover case where `configuredOnly:true` is empty but `configuredOnly:false` yields a different host.
- **Fix direction:** Unit test documenting invariant; accept default only when both enumerations fail.

## 20. NL description activity retry re-billing on every attempt
- **Location:** `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs`
- **Severity:** Low
- **Evidence:** No idempotency token on `ConverseAsync`; comment claims "Idempotency delegated to DAPR Workflow replay" which is inaccurate — workflow replays activity inputs, not outputs; activity re-runs on each retry. `MaxAttempts=2` × retry workflow's `MaxRetryAttempts=5` = up to 10 billed calls per event worst-case.
- **Fix direction:** Correct the comment; consider idempotency key keyed on `(TenantId, MemoryUnitId)` in Redis with short TTL.
