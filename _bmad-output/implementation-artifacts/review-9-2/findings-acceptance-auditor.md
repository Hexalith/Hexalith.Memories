# Acceptance Auditor Findings — Story 9.2 (committed-only diff)

## Summary
- **AC coverage:** 14 of 17 met (AC #6 missing retry backpressure; AC #11 relaxed "version-mismatched" → "any in-flight"; AC #17 missing "LLM hallucination posture" section)
- **Tasks coverage:** ~8 of 10 complete in diff. Task 8 partial (8.7/8.8 deferred). Task 9 deferred wholesale. Task 10 docs near-complete.
- **Risk guard tests:** ~10 of 17 present.
- **Overall verdict:** Substantially compliant with the core shipping surface (dual-embedding pipeline, degraded retry queue, CorrelationId/CausationId self-edge guards, `isStub` gap markers, NL-axis consistency inspection, tenant provisioning/deletion of NL index). Session 5 "shipped" claims (IsStubBackfillMigration DI registration, NaturalLanguageDescriptionGeneration ActivitySource span, ConsistencyNoteKind typed enum, ConversationCacheHit counter, queue-bytes gauge, PII_ACKNOWLEDGMENT.md, dedicated hallucination posture section) are NOT present in the committed diff.

---

## Missing (claimed-shipped-but-not-in-committed-diff)

### IsStubBackfillMigration never registered
- **Kind:** Missing
- **AC / Constraint:** Review Finding D3 (Session 5); Task 7.6
- **Spec says:** "Shipped IsStubBackfillMigrationHostedService … registered in Program.cs:~155"
- **Diff shows:** `IsStubBackfillMigration.cs` present; no `IsStubBackfillMigrationHostedService.cs` in diff; no registration in `Program.cs`
- **Severity:** High
- **Recommendation:** Ship hosted-service wrapper + registration, or update story notes.

### MemoriesActivitySource.NaturalLanguageDescriptionGeneration span missing
- **Kind:** Missing
- **AC / Constraint:** Task 2.5; Review Finding D5
- **Spec says:** "Added MemoriesActivitySource.NaturalLanguageDescriptionGeneration + StartActivity(ActivityKind.Client)"
- **Diff shows:** `MemoriesActivitySource.cs` not in diff; `GenerateNaturalLanguageDescriptionActivity.cs` has no `StartActivity` call
- **Severity:** Medium
- **Recommendation:** Add the const + span, or formally defer.

### memories_conversation_cache_hit_total counter + memories_natural_language_embedding_queue_bytes gauge absent
- **Kind:** Missing
- **AC / Constraint:** Risk #16 guard; Session 5 D6
- **Spec says:** "Added MemoriesMeter.ConversationCacheHit counter" and "queue_bytes observable gauge"
- **Diff shows:** `MemoriesMeter.cs` adds only `NaturalLanguageDescriptionDuration`, `NaturalLanguageEmbeddingQueueDepth`, `EmbeddingApiCalls`. No `ConversationCacheHit` counter or queue-bytes gauge.
- **Severity:** Medium
- **Recommendation:** Wire instruments into MemoriesMeter + TelemetrySnapshotCache, or update expectations.

### ConsistencyNoteKind typed enum not present
- **Kind:** Missing
- **AC / Constraint:** AC #10; Session 5 D7
- **Spec says:** "Shipped the enum in Hexalith.Memories.Contracts.V1.ConsistencyNoteKind … Threaded through ConsistencyResult, ConsistencyDiscrepancy, ConsistencyInspectionResult"
- **Diff shows:** No `ConsistencyNoteKind.cs` file added. Free-form `ConsistencyNote` string only.
- **Severity:** Medium
- **Recommendation:** Ship enum and thread through, or accept free-form.

### docs/governance/PII_ACKNOWLEDGMENT.md not in diff
- **Kind:** Missing
- **AC / Constraint:** Task 10.1.10; Review Decision D10
- **Spec says:** "Created docs/governance/PII_ACKNOWLEDGMENT.md"
- **Diff shows:** No such file; eventstore doc has PII bullet only.
- **Severity:** Medium
- **Recommendation:** Commit artifact or formally defer.

### "LLM hallucination posture" not a distinct section
- **Kind:** Violation
- **AC / Constraint:** AC #17; Task 10.1.9; Review Decision D9
- **Spec says:** AC #17 requires section distinct from "Known limitations"
- **Diff shows:** Hallucination bullets folded into "Known limitations"
- **Severity:** Low
- **Recommendation:** Extract into own level-2 section, or relax AC #17.

## Missing (acknowledged deferred)

### RateLimiterSizingValidator / 9163 (Task 8.7) — deferred, no action
### Retry backpressure + 9174 + exponential backoff (Task 8.5 / D2) — **Medium**, documented re-open trigger
### Logprobs extraction (Task 2.5 / D1) — deferred (SDK blocker)
### Integration test suite (Task 9.1–9.6) — **High** for AC #14/#15/#16 empirical verification; deferred
### CLI dead-letter surface (Task 8.8) — deferred

## Risk guard tests absent
- `DualEmbeddingLatencyTests` (Risk #2)
- `CorrelatedWith_InboundDirection_ReturnsCorrelatedSiblings` (Risk #3)
- Three `GraphTraversalServiceTests` stub tests (Risk #4)
- `EmbeddingRateLimiterActorTests.BothContentKinds_ConsumeSameBudget` (Risk #6)
- `DaprConversationIntegrationTests.ApiSurfaceSmokeTest` (Risk #11)
- `BuildMergeStubNode_OnExistingNonStub_DoesNotRegressIsStubFlag` (Risk #12)
- `OrphanStubQuery_ReturnsStubsOlderThanThreshold` (AC #3)
- **Severity:** Medium (Tier-1 variants where possible)

## Deviations (spec-accepted)

### AC #11 relaxation (version-mismatched → any in-flight)
- Severity: Low — explicitly accepted in spec text

### Spike 0.1 payload-by-value fallback (not payload-by-reference)
- Severity: Low — fallback documented in decision tree

## Over-delivery (approved)

### `9156 CausationIdSelfEdgeSkipped` — accepted in Session 4 patch batch
### `CheckMemoryUnitExistsActivity` pre-LLM existence check — defensible (avoids wasted LLM spend)
