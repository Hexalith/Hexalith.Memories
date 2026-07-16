# Story 9.2 Review Triage — committed-only scope (`662a001..HEAD`)

**Review mode:** full (spec + 2 adversarial layers).
**Layers completed:** Blind Hunter (20), Edge Case Hunter (52), Acceptance Auditor (17 raw; 6 "missing" items actually in uncommitted follow-up fix pass).
**Raw total:** ~89 findings → after dedup + verify + classification: see counts below.

**Scope caveat:** user explicitly excluded the 26-file uncommitted follow-up fix pass. Six Acceptance Auditor "Missing" items (`IsStubBackfillMigrationHostedService.cs`, `ConsistencyNoteKind.cs`, `PII_ACKNOWLEDGMENT.md`, `NaturalLanguageDescriptionGeneration` span, `ConversationCacheHit` counter, `NaturalLanguageEmbeddingQueueBytes` gauge) are present in working tree but NOT in the committed diff. These are reported as **[dismiss → out-of-scope]** with a flag for the user.

---

## Classification counts

| Bucket | Count |
|---|---|
| **decision_needed** | 9 |
| **patch** | 23 |
| **defer** | 10 |
| **dismiss** | 22 (12 dedup + 4 false positive + 6 out-of-scope) |

---

## DECISION NEEDED

### D1. `PersistInMetadata` workflow gate is statically unreachable
- **Sources:** blind#2 (verified)
- **Location:** `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:260-261` + `GenerateNaturalLanguageDescriptionActivity.cs:202-203`
- **Evidence:** Activity hardcodes `estimatedConfidence = null`; workflow gate requires `EstimatedConfidence is float measuredConfidence`. Pattern-match fails when null → metadata entry never written, regardless of the `PersistInMetadata` flag. Test `NaturalLanguageDescriptionMetadata_PersistedOnlyWhenConfigured` stubs non-null confidence that production never emits.
- **Why decision needed:** spec is ambiguous — do we persist metadata with description but no confidence, or require confidence (which requires logprobs extraction that Dapr.AI 1.17.6 blocks)?
- **Options:**
  - (a) Relax the gate to `PersistInMetadata == true` alone; persist `MetadataField(Value=description, Origin=Ai, Confidence=null)`
  - (b) Keep gate; document that `PersistInMetadata` requires future logprobs support; flag test as aspirational (mark with `[Trait("Pending", "D1 logprobs")]`).
  - (c) Persist only when `ConfidenceSource != Constant` — allows providers that DO expose proxy signals later.

### D2. `WorkflowReplaySafetyHostedService.TryGetWorkflowName` reflects on `Name` property whose existence is not proven
- **Sources:** blind#6, edge#33
- **Location:** `src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs:163-166`
- **Evidence:** `state.GetType().GetProperty("Name", ...)?.GetValue(state) as string` returns null silently if the property doesn't exist on `Dapr.Workflow.WorkflowState` (in some SDK versions it's `WorkflowName` or lives on a nested `OrchestrationState`). Silent-fail → `ShouldCountWorkflow` always false → gate is a no-op.
- **Why decision needed:** The code is supposed to be safer than the failed-open path directly below it. Silent no-op subverts the safety gate.
- **Options:**
  - (a) Bind to the actual SDK type (whatever name exists) at compile time; break on SDK upgrade rather than silently no-op.
  - (b) Add a startup assertion: probe one reflection target on first call; if null, log Critical 9173 and fail the gate loud.
  - (c) Write a unit test that constructs a real `WorkflowState` fake and pins the property name, so SDK drift fails CI.
- **Note:** sprint-status.yaml (uncommitted) claims "compile-safe reflective workflow-name lookup" — but that's a different fix (the production-caller guard for `BuildMergeStubNode`). The `TryGetWorkflowName` reflection in committed HEAD is still silent-fail.

### D3. `OrphanSemanticIndexReconciler` runs once and exits — doc claims it covers post-startup SIGKILL
- **Sources:** blind#14
- **Location:** `src/Hexalith.Memories.Server/Hosting/OrphanSemanticIndexReconciler.cs:37-55`
- **Evidence:** `BackgroundService.ExecuteAsync` returns after a single `ReconcileAsync`. XML doc says "Covers the chaos Scenario D case where TenantProvisioningWorkflow compensation cannot reach (e.g., SIGKILL mid-provisioning)." Post-startup mid-provisioning SIGKILL happens AFTER the single pass completes.
- **Options:**
  - (a) Loop every N hours inside `while (!stoppingToken.IsCancellationRequested)`.
  - (b) Accept the one-shot semantics; weaken the doc to "covers startup-recovery only". Combined with compensation-path coverage in `DeleteRedisVectorIndexActivity` this may be sufficient.
  - (c) Trigger the reconciler on demand from the compensation path explicitly.

### D4. `FailedNL` dequeue-before-finalize is not crash-safe
- **Sources:** blind#11, edge#26
- **Location:** `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedService.cs:TickAsync` + `FailedNaturalLanguageEmbeddingRegistry.cs:DequeueBatchAsync`
- **Evidence:** `DequeueBatchAsync` uses `SortedSetRangeByRankWithScoresAsync` (peek). Records are only removed on `CompleteAsync` or `IncrementAttemptsAsync` after workflow completion. Crash between schedule and finalize → next tick re-dequeues same record → `ScheduleNewWorkflowAsync` throws `InvalidOperationException` (instanceId exists) → falls to `TryGetWorkflowStateAsync` happy path if workflow actually completed. But attempts can silently exceed `MaxRetryAttempts` for legitimate completions the service never observed.
- **Options:**
  - (a) Keep peek-then-complete; document that replay happens and is idempotent.
  - (b) Adopt ZPOPMIN + lease (move to in-flight set with TTL, reclaim on restart).
  - (c) Mark as accepted design trade-off and defer to a follow-up story.

### D5. `FailedNL` ZREM uses exact-byte JSON equality
- **Sources:** blind#4
- **Location:** `FailedNaturalLanguageEmbeddingRegistry.cs:93` + `:115`
- **Evidence:** `db.SortedSetRemoveAsync(LiveKey, json)` requires byte-equal member. AOT-generated JSON source-generator output can drift between versions (field ordering, escaping, whitespace). Silent ZREM=0 + transactional add of incremented record → queue grows unboundedly with stale entries; "attempts incremented" log line is false.
- **Options:**
  - (a) Key entries by a stable id (e.g., `{tenantId}:{memoryUnitId}`) as the sorted-set member; store JSON in a parallel hash. Breaking change to serialized shape — requires a migration.
  - (b) Assert `removed == true` inside the transaction and retry deterministically on mismatch.
  - (c) Stabilize JSON output: lock JsonSerializerOptions + write a canonicalization test that pins byte output.
- **Note:** (c) plus a CI canonicalization assertion is cheapest near-term. (a) is the durable fix.

### D6. `cloudevent.type` / `event.aggregateType` key lookup is case-sensitive
- **Sources:** blind#12
- **Location:** `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:~240` (metadata lookup before NL prompt build)
- **Evidence:** `input.Metadata` is `Dictionary<string, MetadataField>` whose comparer is not enforced to `OrdinalIgnoreCase`. If Story 9.1's mapper ever writes `CloudEvent.Type` / `Event.AggregateType` (PascalCase), every `SourceType.Event` ingest passes `eventType = "(unknown)"` to the LLM prompt. No telemetry fires.
- **Options:**
  - (a) Pin dictionary comparer to `OrdinalIgnoreCase` at construction.
  - (b) Centralize the key constants in a shared class and audit all writers; keep case-sensitive.
  - (c) Log Information-level 915x when the key is missing so drift is observable.
- **Recommendation:** (a) + (c), no ambiguity in cost.

### D7. `NaturalLanguageConsistencyState` discrepancy emits `Recommendation=NoOp` for real NL-axis gaps
- **Sources:** blind#13
- **Location:** `src/Hexalith.Memories.Server/Consistency/NaturalLanguageConsistencyState.cs:~45` + `ConsistencyVerificationWorkflow.cs:~100`
- **Evidence:** Discrepancy surfaced (metric says "1 inconsistent") but `Recommendation = NoOp` (payload says "no repair needed"). Repair planner iterates discrepancies keyed on Recommendation → skips NL cases. Contradiction is pinned by tests.
- **Options:**
  - (a) Add `ConsistencyRepairRecommendation.RepairNaturalLanguageSemantic` enum; route via existing planner.
  - (b) Downgrade NL-axis signal from "discrepancy" to "note" only — remove from inconsistent count entirely.
  - (c) Keep both; split the inspection result into "discrepancies" (actionable) and "notes" (informational) so metrics are accurate.
- **Note:** Session 5 (uncommitted) claimed a `ConsistencyNoteKind` typed enum that may resolve this. Verify against uncommitted before acting.

### D8. NL prompt `{EventType}` placeholder is eval-via-`Replace` without input validation
- **Sources:** edge#52
- **Location:** `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs:BuildMessages`
- **Evidence:** Event type is interpolated into system prompt via a simple replacement. An attacker who can inject a `CloudEvent.Type` value containing "Return ONLY..." or similar can alter the instruction text the LLM receives.
- **Options:**
  - (a) Validate `eventType` against an allow-list character set; fail-fast on anomalies.
  - (b) Use a `|` delimiter or JSON-encode the event type before interpolation.
  - (c) Treat the risk as out-of-scope for 9.2 (authenticated event producers only); defer hardening.

### D9. Replay-safety gate **fails open** on sidecar unreachable
- **Sources:** blind#7 (author's comment explicitly documents the trade-off)
- **Location:** `WorkflowReplaySafetyHostedService.cs:72`
- **Evidence:** "sidecar unreachable — fail open. A stuck pod is worse than a missing gate." — but rolling deployment IS the exact scenario where sidecar startup is racy.
- **Why decision needed:** not a defect per se — an explicit trade-off — but worth confirming with the operator stance.
- **Options:**
  - (a) Accept the trade-off; close as-is.
  - (b) Fail closed for a bounded window (e.g., 60s) then fail open after; env-var escape hatch.
  - (c) Couple gate to DAPR sidecar readiness probe rather than in-process reachability.

---

## PATCH (fix unambiguous, no design decision)

### P1. `GenerateNaturalLanguageDescriptionActivity`: null-guard `RawJsonPayload`
- **Source:** edge#4
- **Patch:** `ArgumentNullException.ThrowIfNull(input.RawJsonPayload);` at `RunAsync` entry.

### P2. `GenerateNaturalLanguageDescriptionActivity.TruncatePayload`: guard `maxChars <= 0`
- **Source:** edge#5
- **Patch:** `if (maxChars <= 0) return string.Empty;` before slicing.

### P3. `GenerateNaturalLanguageDescriptionActivity.TruncatePayload`: avoid surrogate-pair split
- **Source:** edge#6
- **Patch:** switch to `Rune.EnumerateRunes` with UTF-16 accumulator (same pattern as `QueueNaturalLanguageEmbeddingRetryActivity.Truncate`).

### P4. `GenerateNaturalLanguageDescriptionActivity.ExtractFirstChoiceText`: handle null array element
- **Source:** edge#2
- **Patch:** `if (firstOutput.Choices is null || firstOutput.Choices.Count == 0 || firstOutput.Choices[0] is null) return string.Empty;`

### P5. `IndexNaturalLanguageSemanticActivity`: null-guard `NaturalLanguageDescription`
- **Source:** edge#9
- **Patch:** `ArgumentException.ThrowIfNullOrWhiteSpace(input.NaturalLanguageDescription);`

### P6. `IndexNaturalLanguageSemanticActivity`: validate `EmbeddingDimensions > 0`
- **Source:** edge#7
- **Patch:** `if (input.EmbeddingDimensions <= 0) throw new ArgumentException(...);`

### P7. `NaturalLanguageConsistencyState.ReadStatus`: null-guard field.Value before `Enum.TryParse`
- **Source:** edge#13
- **Patch:** `if (field?.Value is not null && Enum.TryParse(field.Value, ...))`

### P8. `Workflows/IngestionWorkflow.cs`: nullable `rawJsonPayload` when ContentBytes empty
- **Source:** edge#15
- **Patch:** `rawJsonPayload ??= string.Empty;` before NL prompt build.

### P9. Workflow: skip NL path when `nlResult.Description` is empty/whitespace (cleaner rejection path)
- **Source:** edge#51
- **Patch:** treat empty description as failure → throw `NaturalLanguageDescriptionUnavailableException` and fall through to Queued.

### P10. `NaturalLanguageEmbeddingRetryWorkflow`: typed catches for embedding step
- **Source:** edge#19/20
- **Patch:** add `catch (EmbeddingRateLimitedException)` → return `Indexed=false, Reason="rate-limited"`; `catch` around index step to decouple embedding spend from index fault.

### P11. `NaturalLanguageEmbeddingRetryHostedService.TickAsync`: per-tenant try/catch
- **Source:** edge#21
- **Patch:** wrap the per-tenant inner loop so one tenant's Redis fault doesn't starve others this tick.

### P12. `FailedNaturalLanguageEmbeddingRegistry.TryDeserialize`: validate required fields
- **Source:** edge#28
- **Patch:** after `JsonSerializer.Deserialize`, return null when `record.TenantId` or `MemoryUnitId` is null/empty.

### P13. `FailedNaturalLanguageEmbeddingRegistry.GetBacklogBytesAsync`: catch `InvalidCastException`
- **Source:** edge#29
- **Patch:** `try { return (long)result; } catch (InvalidCastException) { return 0; }`

### P14. `Search/NaturalLanguageSemanticSearchService.SearchAsync`: per-doc guards
- **Source:** edge#40, edge#41
- **Patches:** skip docs missing `memoryUnitId`; `double.TryParse` on `__vector_score` with fallback.

### P15. `TelemetrySnapshotCache.RefreshSnapshotAsync`: per-tenant try/catch
- **Source:** edge#46, edge#47
- **Patch:** one tenant's RedisException must not poison snapshot for all tenants until next 30s tick.

### P16. `IndexGraphActivity.TryEmitStubResolvedTelemetry`: relabel field from `CausingEventId` → `ResolverEventId`
- **Source:** blind#10
- **Patch:** rename field on `9154` event; document semantic — resolver is the event whose MERGE promoted the stub, not necessarily the causation link.

### P17. `FalkorDbSemanticAttributeProcessor.OnEnd`: handle `server.address` with port suffix
- **Source:** edge#43
- **Patch:** strip port in allow-list comparison, or add port-aware allow-list entries.

### P18. `MemoriesMeter` + `TelemetryMetricsRecorder`: add `outcome` tag to `NaturalLanguageDescriptionDuration`
- **Source:** blind#8, blind#16
- **Patch:** add `outcome` (`success` / `timeout` / `grpc` / `dapr` / `cleaner-rejected`) to cardinality policy; emit on every record; separate histogram buckets for success vs failure.

### P19. Test-level: collection-isolate global-state tests
- **Source:** blind#18
- **Patch:** single `[Collection("GlobalMutableState")]` with `[CollectionDefinition(DisableParallelization = true)]` for all tests that mutate `NaturalLanguageDescriptionOptionsSnapshot`, `RetryPolicyBuilder`, or process env vars.

### P20. Log `Warning` when `TryApplyMaxTokenHint` reflection finds no candidate
- **Source:** blind#9
- **Patch:** single Warning on first call where no candidate succeeded; add a `max_tokens_applied` counter or record result on the duration metric's outcome tag.

### P21. `IndexGraphActivity`: guard duplicate stub creation when CausationId == CorrelationId
- **Source:** edge#10
- **Patch:** combine the two branches so `BuildMergeStubNode` runs once when both IDs point to the same sibling.

### P22. `OrphanSemanticIndexReconciler.ReadIndexNames`: log on `InvalidCastException`
- **Source:** edge#30, blind#17
- **Patch:** log a Warning naming the observed `RedisResult` type before returning `[]`; optionally emit a counter.

### P23. Fix spec-violation on AC #17: extract "LLM hallucination posture" as own level-2 section
- **Source:** auditor
- **Patch:** split the bullet out of "Known limitations" into a standalone `## LLM hallucination posture` section in `docs/dev/eventstore-integration.md`.

---

## DEFER (real but not actionable now)

### F1. Retry backpressure (Task 8.5 / D2) + `9174` event + exponential backoff
- Auditor + blind#11 flanks. Already tracked in `deferred-work.md` with re-open trigger.

### F2. Tier-2 / Tier-3 integration tests (Task 9.1–9.6) for AC #14/#15/#16
- Explicitly deferred in Task 9 header.

### F3. RateLimiterSizingValidator + event 9163 (Task 8.7)
- Explicitly deferred.

### F4. `retry-nl-embeddings` CLI dead-letter surface (Task 8.8)
- Explicitly deferred.

### F5. Logprobs extraction (Task 2.5 / D1)
- SDK blocker on Dapr.AI 1.17.6; documented.

### F6. Per-tenant LLM configuration (Phase 2)
- Out of MVP scope.

### F7. `RetryHostedService.ScheduleRetryAsync` orphaned-workflow dead-letter on perpetual timeout
- Edge#23 — add a bounded-per-record "stuck after N ticks → dead-letter manually" rule. Follow-up story.

### F8. Redis cluster multi-node enumeration in `ListTenantsWithBacklogAsync`
- Edge#27 — current `GetFirstConnectedServer()` covers single-node/replicated deployments; cluster deployment is not in scope for MVP.

### F9. `OrphanSemanticIndexReconciler.ExecuteAsync` interval-based re-run
- See D3 — if accepting one-shot, file a follow-up for interval-based variant when operational need arises.

### F10. `IsStubBackfillMigration` atomic gate-write + backfill (partial-commit safety)
- Edge#34 — low probability; defer to ops runbook for now.

---

## DISMISS

### Dup / rolled into other findings
- edge#24, edge#25 (schedule-retry race): subsumed by D4.
- edge#38, edge#39 (Queue `Truncate` edge cases): subsumed by P3.
- edge#49 (DropIndex without DD on compensation): **false positive** — `DeleteRedisVectorIndexActivity` is the PROVISIONING-compensation path; no documents exist yet at mid-provisioning failure. Tenant deletion path (`DeleteRedisVectorActivity`) already uses `DD`. Blind#15 is the same false positive.
- edge#42 (`BuildMergeStubNode` 1-arg obsolete): already addressed by the `[Obsolete]` + production-caller guard landed in commit 9ee6601; any remaining test reference is suppressed via `#pragma warning disable CS0618`.

### False positive
- blind#3 (cross-tenant footgun on `nl`-ending tenant IDs): index suffix is `:memories:vec:nl` with the leading colon as a unique terminator. Tenant `"tenant-final-nl"` → full raw index name is `"tenant-final-nl:memories:vec"` (no `:nl` at end) → `EndsWith(":memories:vec:nl")` returns false → raw path, not NL path. No collision.
- blind#15 (DropIndex without DD): see edge#49.
- edge#17 (dual-embedding partial failure compensation): behavior is already correct — `completedBackends.Add("semantic-nl")` only when the task completes successfully; raw path remains `Indexed`.
- edge#18 (`SourceType == Event` + `NotApplicable`): guarded at construction; not a real code path.

### Low-value noise
- edge#1 (OperationCanceledException host-shutdown filter): current catch filter is `when (cts.Token.IsCancellationRequested)`; host cancellation token is linked into `cts`. Current behavior is correct.
- edge#3 (Reflection GetValue throws TargetInvocationException): never observed; try/catch around entire `TryApplyMaxTokenHint` already catches at caller.
- edge#11 (TryEmitStubResolvedTelemetry schema drift): addressed by P22 logging on `InvalidCastException`.
- edge#12 (CleanupSemanticActivity first-succeeds-second-throws): both KeyDeleteAsync calls are independent; current activity returns void-success either way. Low value.
- edge#16 (metadata records NotApplicable when SourceType==Event): impossible by construction.
- edge#22 (rate-limit on retry): see D4.
- edge#31 (Orphan reconciler rethrows on programming errors): documented intent — "do NOT swallow programming errors". Author trade-off.
- edge#32 (ListInstanceIdsAsync broken continuation): defensive; real SDK contract is bounded-pages.
- edge#36 (Unicode 'µ' parsing in TryParseDuration): hypothetical; author comment covers documented duration grammar.
- edge#44 (UnixDomainSocket endpoint): not a supported deployment topology.
- edge#45 (ObjectDisposedException on Activity.Parent): race is theoretical in tracer pipeline.
- edge#48 (Component YAML reader path under container): tested in Session 5 fix (D10 evidence path) — out of committed scope.
- edge#50 (`QueueNaturalLanguageEmbeddingRetryInput` wire-compat): positional-param fix already landed in commit 9ee66015.
- blind#1 (CleanupSemanticActivity log placeholder ordering): placeholder order is consistent — no downstream parsers in the codebase cache an older signature.
- blind#19 (FalkorDb hostname resolution race): runs at first-span which is after endpoint publication.
- blind#20 (NL activity replay re-billing): activity is re-run on retry by design; retry policy caps at 2 attempts.

### Out-of-scope (present in uncommitted working tree; user excluded)
- **Auditor:** `IsStubBackfillMigrationHostedService` missing from Program.cs — **present in uncommitted `src/Hexalith.Memories.Server/Hosting/IsStubBackfillMigrationHostedService.cs`**
- **Auditor:** `MemoriesActivitySource.NaturalLanguageDescriptionGeneration` span — **present at `MemoriesActivitySource.cs:46`** (uncommitted `M`).
- **Auditor:** `memories_conversation_cache_hit_total` + `memories_natural_language_embedding_queue_bytes` — **both present at `MemoriesMeter.cs:57,63,111,157`** (uncommitted `M`).
- **Auditor:** `ConsistencyNoteKind` typed enum — **present at `src/Hexalith.Memories.Contracts/V1/ConsistencyNoteKind.cs`** (uncommitted `??`).
- **Auditor:** `docs/governance/PII_ACKNOWLEDGMENT.md` — **present in uncommitted folder**.
- All six will land when the follow-up fix pass commits; review them in a separate pass if desired.
