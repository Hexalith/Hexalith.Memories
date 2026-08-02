# Epic 23 Documentation Verification — 2026-08-02

## Review identity and baseline

- Branch: `main`
- Baseline commit: `feac22bbc78c290f7ed8b1c2d5e1bfedf4dab133`
- Review date: 2026-08-02
- Documentation owner/reviewer: Paige
- Evidence owner/reviewer: Amelia

## Changed-document inventory

- `docs/operations/rate-limiting.md`
- `docs/operations/failure-recovery.md`
- `docs/operations/directory-ingestion.md`
- `docs/operations/index-rebuild.md`
- `docs/dev/ingestion-workflow-determinism.md`

## Checkpoint status

| Checkpoint | Quoted implementation claim to verify | Documentation owner | Evidence owner | Review status | Completion status | Verdict |
|---|---|---|---|---|---|---|
| Rate limiting | “Embedding admission uses one actor call per single provider call or bounded provider batch; provider 429 feedback remains activity-owned and workflow recovery uses a bounded durable timer.” | Paige | Amelia | Reviewed | complete | `corrected` |
| Failure recovery | “Failed non-URL re-ingestion uses a retained tenant-scoped source payload reference or returns `NON_URL_REINGESTION_UNAVAILABLE` before claiming the failed record; URL re-ingestion still refetches.” | Paige | Amelia | Reviewed | complete | `corrected` |
| Directory ingestion | “Directory ingestion applies the supported-extension allowlist before reading bytes, uses bounded scheduling and checkpointing, cleans unscheduled payloads, and produces deterministic final accounting.” | Paige | Amelia | Reviewed | complete | `corrected` |
| Index readiness | “Ingestion memoizes tenant/index/schema readiness, fails clearly for missing or incompatible indexes, and does not create indexes on demand; tenant provisioning remains the creation owner.” | Paige | Amelia | Reviewed | complete | `corrected` |
| Workflow determinism | “Retry and natural-language workflow configuration is captured at scheduling time; `IngestionWorkflow` does not read mutable host snapshots during orchestration.” | Paige | Amelia | Reviewed | complete | `corrected` |

All rows were changed or newly documented and then reverified, so `corrected` is the
applicable allowed verdict. No row is `unverifiable`.

## Checkpoint evidence

### Rate limiting — `corrected`

Quoted from the final documentation:

> `GenerateEmbeddingActivity` makes one `TryConsumeWithCeilingAsync(rateLimitPerMinute)` actor call before its single provider call. `GenerateChunkEmbeddingsActivity` makes one admission call before each bounded provider batch.

> They report feedback only for an exception raised while a provider call is in progress. A local admission denial happens before the provider call and does **not** report a provider 429.

Re-runnable evidence:

```bash
rg -n 'TryConsumeWithCeilingAsync|providerCallInProgress|ReportRateLimitedAsync|_embeddingProviderRateLimitMaxDurableRetries|CreateTimer|CacheTtlSeconds|MaxCacheEntries|JitterMaxExclusiveMilliseconds' \
  src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs \
  src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs \
  src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs \
  src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs \
  src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigCacheOptions.cs
```

Observed: single embedding admits once before its provider call; chunk embedding
admits inside the bounded batch loop; chunk feedback is guarded by
`providerCallInProgress`; the activity normalizes/reports provider feedback; the
workflow allows five provider-rate durable waits and uses `CreateTimer`; cache TTL and
entry counts are clamped in their option consumers. The focused lane passed all rate
limiting tests. The stale numeric provider-quota example and first-attempt jitter claim
were removed.

### Failure recovery — `corrected`

Quoted from the final documentation:

> After source-payload expiry, or when a record never had a valid retained source, re-ingestion returns `NON_URL_REINGESTION_UNAVAILABLE` and leaves the failed-unit hash, case sorted-set row, and dedup key untouched.

> URL records refetch from `SourceUri`; supported non-URL records schedule with `ContentBytes = null` and the validated retained source reference.

Re-runnable evidence:

```bash
rg -n 'SourcePayloadReference|ValidateSourcePayloadAsync|NON_URL_REINGESTION_UNAVAILABLE|ContentBytes = null|PayloadReference =|RemoveAsync|RestoreAsync|TtlHours|Math\.Max\(1' \
  src/Hexalith.Memories.Server/Ingestion/ReIngestionCoordinator.cs \
  src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs \
  src/Hexalith.Memories.Server/Ingestion/WorkflowPayloadStoreOptions.cs \
  src/Hexalith.Memories.Server/Ingestion/DaprWorkflowPayloadStore.cs \
  src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs \
  tests/Hexalith.Memories.Server.Tests/Ingestion/{ReIngestionCoordinatorTests,FailedUnitsRegistryTests,WorkflowPayloadStoreTests}.cs
```

Observed: non-URL reference validation precedes atomic removal; URL input omits a
payload reference; supported non-URL input carries no inline bytes; scheduling failure
restores every field serialized by the registry, including the optional source
reference; payload TTL is `max(1, TtlHours)` with default 24. Bulk tests preserve the
unsupported-source outcome instead of converting it to a generic scheduling error.

### Directory ingestion — `corrected`

Quoted from the final documentation:

> Filtering is deliberately fail-closed and occurs before file bytes are read.

> Before every persisted snapshot it sorts scheduled file rows by source URI using `StringComparer.Ordinal` and rebuilds the instance-ID array in the same order.

> This cleanup is **best effort**: deletion failure is logged and TTL expiry remains the backstop.

Re-runnable evidence:

```bash
rg -n 'SupportedExtensions|UnsupportedExtensions|File\.ReadAllBytesAsync|DirectorySchedulingParallelism|DirectoryBatchCheckpointSize|Parallel\.ForEachAsync|TrySaveBatchStateAsync|DeleteCreatedPayloadAsync|OrderBy|SkippedTruncated' \
  src/Hexalith.Memories.Server/Ingestion/{DirectoryIngestionService,IngestionSettings}.cs \
  src/Hexalith.Memories.Contracts/V1/DirectoryIngestionOutcome.cs \
  tests/Hexalith.Memories.Server.Tests/Ingestion/{DirectoryIngestionServiceTests,DirectoryIngestionPathValidationTests,DirectoryBatchStatusMapperTests}.cs
```

Observed: allowlist/deny-overlay, path and size checks precede byte reads; scheduling
parallelism clamps to 1..32 and checkpoint size to 1..250; initial, bounded progress,
failure-attempt, and final saves are present; unscheduled per-file payload deletion is
best effort; snapshot/final instance order is ordinal by source URI. The final guide
also records that truncated skip rows are not a total count and caller cancellation
can leave the last snapshot behind already scheduled work.

### Index readiness — `corrected`

Quoted from the final documentation:

> The verifier checks the existing index with `FT.INFO`; it never repairs a missing index with `FT.CREATE` and never writes the hash/vector until readiness succeeds.

> The only automatic schema changes are additive missing `TAG` fields, and only when the actual field set is otherwise an exact subset of the expected schema.

Re-runnable evidence:

```bash
rg -n 'SyntacticAdditiveFields|SemanticAdditiveFields|EnsureReadyAsync|FT\.INFO|FT\.CREATE|FT\.ALTER|ReadinessKey|TryRemove|TenantIndexNotProvisionedException|TenantIndexSchemaMismatchException' \
  src/Hexalith.Memories.Server/Infrastructure/{TenantIndexReadinessVerifier,IndexSchemaDefinitions}.cs \
  src/Hexalith.Memories.Server/Activities/Indexing/Index{Syntactic,Semantic,SemanticChunks,NaturalLanguageSemantic}Activity.cs \
  tests/Hexalith.Memories.Server.Tests/{Infrastructure/TenantIndexReadinessVerifierTests,Architecture/IndexingHotPathGuardTests}.cs
```

Observed: all four indexing paths await readiness before their hash/vector write;
successful checks are process-local and keyed by tenant/family/dimensions; failed
checks are evicted; missing and incompatible indexes throw distinct exceptions; no
ingestion hot path contains `FT.CREATE`. The verified upgrade matrix is syntactic
`cloudeventSubject`/`attributeTags`, raw semantic `cloudeventSubject`, and no
natural-language additive field. The content is an H3 under the existing runbook
section so the exact shared Story 26.5 H2 contract remains intact; the operational
runbook guard passed.

### Workflow determinism — `corrected`

Quoted from the final documentation:

> Its Dapr implementation applies configuration and trace capture, then runs the payload claim-check, tracks the in-flight instance, and finally calls `ScheduleNewWorkflowAsync`.

> Once scheduled, replay must use the serialized values; a host configuration reload or server restart must not change an existing workflow's decisions.

Re-runnable evidence:

```bash
rg -n 'workflowConfigurationCapture\.Apply|WorkflowConfiguration|PrepareInputAsync|IngestionPayloadClaimCheck|ScheduleNewWorkflowAsync|RetryPolicyBuilder|NaturalLanguageDescriptionOptionsSnapshot|CapturedRetryConfig' \
  src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs \
  src/Hexalith.Memories.Server/Workflows/{IngestionWorkflow,AnnotationProjectionWorkflow}.cs \
  src/Hexalith.Memories.Server/Activities/Cases/ScheduleAnnotationIngestionActivity.cs \
  src/Hexalith.Memories.Contracts/V1/{IngestionInput,IngestionWorkflowConfiguration,MemoriesJsonContext}.cs \
  tests/Hexalith.Memories.Server.Tests/{Architecture,Ingestion,Workflows} \
  tests/Hexalith.Memories.Contracts.Tests/V1/IngestionInputSerializationTests.cs
```

Observed: the normal scheduler applies durable configuration before its claim-check
and schedules only the slim result; direct URL capture precedes direct scheduling;
annotation parent/child paths preserve the captured contract; `IngestionWorkflow`
consumes the durable retry/NL values and source guards reject the known mutable
snapshots. Source-generated JSON registrations include the input, configuration, retry
and natural-language contract types. Supporting capture/child and contract
serialization lanes passed.

## Global verification commands and results

### Approved focused behavioral lane

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll \
  -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityTests \
  -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateChunkEmbeddingsActivityTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.ReIngestionCoordinatorTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.FailedUnitsRegistryTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.DirectoryIngestionServiceTests \
  -class Hexalith.Memories.Server.Tests.Infrastructure.TenantIndexReadinessVerifierTests \
  -class Hexalith.Memories.Server.Tests.Architecture.IndexingHotPathGuardTests \
  -class Hexalith.Memories.Server.Tests.Architecture.IngestionWorkflowDeterminismGuardTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.DaprIngestionWorkflowSchedulerTests \
  -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests \
  -parallel none -noLogo
```

Observed: **127 total, 0 errors, 0 failed, 0 skipped, 0 not run**.

### Supporting directory, payload, scheduling, and child-workflow lane

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll \
  -class Hexalith.Memories.Server.Tests.Ingestion.DirectoryIngestionPathValidationTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.DirectoryBatchStatusMapperTests \
  -class Hexalith.Memories.Server.Tests.Endpoints.DirectoryIngestionEndpointE2ETests \
  -class Hexalith.Memories.Server.Tests.Ingestion.IngestionPayloadClaimCheckTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.WorkflowPayloadStoreTests \
  -class Hexalith.Memories.Server.Tests.Serialization.UrlAndDirectoryIngestionSerializationTests \
  -class Hexalith.Memories.Server.Tests.Workflows.AnnotationProjectionWorkflowTests \
  -parallel none -noLogo
```

Observed: **45 total, 0 errors, 0 failed, 0 skipped, 0 not run**.

### Contract serialization lane

```bash
dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll \
  -class Hexalith.Memories.Contracts.Tests.V1.IngestionInputSerializationTests \
  -parallel none -noLogo
```

Observed: **11 total, 0 errors, 0 failed, 0 skipped, 0 not run**.

### Operational runbook contract

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll \
  -class Hexalith.Memories.Server.Tests.Deployment.OperationalRunbookSetTests \
  -parallel none -noLogo
```

Observed: **9 total, 0 errors, 0 failed, 0 skipped, 0 not run**.

### Static anchors and relative links

```bash
set -euo pipefail
rg -n 'TryConsumeWithCeilingAsync|SourcePayloadReference|DirectorySchedulingParallelism|ITenantIndexReadinessVerifier|WorkflowConfiguration' docs src tests --glob '*.{md,cs}' > /tmp/epic23-static-anchors.txt
printf 'anchor_matches=%s\n' "$(wc -l < /tmp/epic23-static-anchors.txt)"
printf 'anchor_files=%s\n' "$(cut -d: -f1 /tmp/epic23-static-anchors.txt | sort -u | wc -l)"
for doc in docs/operations/rate-limiting.md docs/operations/failure-recovery.md docs/operations/directory-ingestion.md docs/operations/index-rebuild.md docs/dev/ingestion-workflow-determinism.md; do
  while IFS= read -r target; do
    case "$target" in http://*|https://*|mailto:*|'') continue ;; esac
    test -e "${doc%/*}/$target" || exit 1
  done < <(perl -ne 'while (/\]\(([^)#?]+)(?:[?#][^)]*)?\)/g) { print "$1\n" }' "$doc")
done
```

Observed in the final rerun: **163 anchor matches across 62 files**; every relative
link in the five documentation surfaces resolved.

### Fail-closed matrix audit

```bash
set -euo pipefail
matrix_gate() {
  case "$1:$2" in
    confirmed:done|corrected:done|unverifiable:open) return 0 ;;
    *) return 1 ;;
  esac
}
matrix_gate confirmed done
matrix_gate corrected done
matrix_gate unverifiable open
checkpoint_rows=$(awk '/^## Checkpoint status/{capture=1;next} /^## Checkpoint evidence/{capture=0} capture' \
  _bmad-output/implementation-artifacts/epic-23-documentation-verification-2026-08-02.md | rg -c '\| `corrected` \|')
test "$checkpoint_rows" -eq 5
action_state=$(awk '/action: "Add an Epic 23 documentation verification pass/{getline; getline; sub(/^[[:space:]]*status:[[:space:]]*/, ""); sub(/[[:space:]]+#.*/, ""); print; exit}' \
  _bmad-output/implementation-artifacts/sprint-status.yaml)
test "$action_state" = done
printf 'matrix=3/3 checkpoint_rows=%s action=%s\n' "$checkpoint_rows" "$action_state"
```

Observed: **matrix 3/3**, including the synthetic `unverifiable` → `open`
fail-closed branch; **5** final checkpoint rows were `corrected`; the exact action was
`done`. The current-tree path and both non-current branches therefore have explicit
executed coverage without modifying product or test code.

### Final cleanliness and approval-block guard

```bash
git diff --check
if rg -n '[[:blank:]]+$' \
    docs/operations/rate-limiting.md \
    docs/operations/failure-recovery.md \
    docs/operations/directory-ingestion.md \
    docs/operations/index-rebuild.md \
    docs/dev/ingestion-workflow-determinism.md \
    _bmad-output/implementation-artifacts/epic-23-documentation-verification-2026-08-02.md \
    _bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md; then
  exit 1
fi
awk '/<frozen-after-approval/{capture=1} capture{print} /<\/frozen-after-approval>/{exit}' \
  _bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md | sha256sum
```

Observed: `git diff --check` exited 0 with no output; the explicit trailing-whitespace
scan returned no matches; the frozen approval block remained
`e731c017452e202a8c657958554a6267be26cfae3cea3060443c79ae00a069b7`.

## Final decision

**Pass.** All five checkpoints are complete with allowed `corrected` verdicts, all
required verification lanes pass, and there is no mismatch, product defect, or
unverifiable proof. The exact Epic 23 retrospective documentation action may be
changed to `done`; Epic 23, Stories 23.1-23.9, and the retrospective remain unchanged.
