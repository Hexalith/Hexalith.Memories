# Deferred Work

## Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)

- **DataContract/DataMember attributes missing on V1 contracts** — Systematic gap across all V1 contracts (ExtractionInput, ExtractionResult, MemoryUnit, GraphEdge, etc.). None use DataContract/DataMember/JsonPropertyOrder/JsonConstructor attributes per project-context.md rules. Should be addressed as a batch across all V1 types.
- **No transient/permanent exception classification for Kreuzberg errors** — AC4 is met (exceptions propagate for DAPR Workflow retry). However, permanent failures (corrupt files) will be retried indefinitely. Future work: classify KreuzbergValidationException as non-retriable, KreuzbergOcrException as retriable.
- **Large byte[] in ExtractionInput persisted to workflow state store** — DAPR Workflow serializes activity inputs to state store. For 1MB files, this means ~1.33MB base64 per workflow instance. Accepted per D13 (MVP payloads ≤1MB). Future work: consider streaming or external storage for larger payloads.
- **byte[] mutable on immutable record** — ExtractionInput uses byte[] which is mutable, breaking record immutability semantics and reference-based equality. No practical alternative exists in .NET for JSON-serializable binary data (ImmutableArray/ReadOnlyMemory don't serialize well).

## Deferred from: code review of 1-4-embedding-generation (2026-03-29)

- **End-to-end embedding flow is not wired into orchestration** — Deferred because orchestration and memory-unit persistence belong to upcoming ingestion workflow work and depend on the final pipeline shape.
- **Rate-limiting scope conflicts with credential scope** — Deferred because Story 1.7 introduces provider configuration and is the right place to decide per-tenant vs per-credential quota enforcement.
- **Story transition rationale is comment-only and not machine-readable** — The sprint tracking update relies on a free-form YAML comment for rationale, so tooling cannot query or validate why a story moved between workflow states.
- **Story status requires manual dual-write across tracking files** — The workflow duplicates status in both the story artifact and `sprint-status.yaml`, which is a pre-existing consistency risk whenever one file changes without the other.

## Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-29)

- **ValidateResult.IsValid/ErrorMessage is dead code on failure path** — ValidateContentActivity throws exceptions for invalid input; the ValidateResult record is only used for the success path. Spec mandates this contract shape, so keeping as-is.
- **SaveDedupKeyActivity: no TTL on dedup keys** — Dedup keys persist forever in DAPR state store, preventing re-ingestion of deleted content. Cleanup mechanism belongs to Epic 8 (Story 8.2 consistency verification).
- **ContentBytes serialized inline in DAPR workflow state** — Base64-encoded byte[] in IngestionInput causes replay amplification for large files. Accepted per D13 (MVP payloads <= 1MB). Same issue as Story 1.3 ExtractionInput. Future: external blob storage for content.

## Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-30)

- **Duplicate dedup entries are returned without confirming the referenced memory unit still exists** — The workflow fast-returns on a dedup hit without verifying that the stored `MemoryUnitId` is still present in the indexed backends, so manual cleanup or backend drift can leave callers with a duplicate response that points at missing data.

## Deferred from: adversarial code review of 1-6-ingestion-workflow-orchestration (2026-03-30)

- **indexedAt set to ingestedAt in GraphQueryBuilder** — `BuildMergeMemoryUnitNode` sets the FalkorDB `indexedAt` property to the workflow's `ingestedAt` timestamp. These are semantically different (when ingestion started vs when the graph write happened). Fixing requires adding a separate `indexedAt` parameter to `IndexInput`, which is a cross-story contract change (Story 1.5).
- **CaseId not validated for special characters** — `TenantId` has a strict alphanumeric+hyphen regex via `TenantIdGuard.Validate`, but `CaseId` only checks for null/empty. Not spec-required; CaseId is used as hash field values (not key names or graph names), so the blast radius is limited to potential key scan interference.

## Deferred from: code review of 2-6-explain-mode-and-confidence-scores (2026-04-02)

- **Return `offset` and `maxResults` pagination metadata in search response envelopes** — AC 3 still calls for `offset`, `maxResults`, and `totalCount` in paginated responses, but the response contracts still expose only `TotalCount`. This appears to predate the explain-mode change and would require a broader response-contract update across `SearchResult` and `HybridSearchResult`.

## Deferred from: code review of 2-7-benchmark-suite-and-thesis-validation (2026-04-12)

- **InternalsVisibleTo in packable library without strong-name key** — `Hexalith.Memories.Redis.csproj` has `IsPackable=true` and `InternalsVisibleTo` for Benchmarks without a strong-name key. Any consumer assembly named `Hexalith.Memories.Benchmarks` could access internals. Pre-existing pattern across the project; low practical risk.
- **FusionEngine non-finite handling asymmetry across axes** — Graph axis skips non-finite scores entirely (`continue` in FusionEngine), while syntactic/semantic axes normalize non-finite to 0.0 via ScoreNormalizer. Both paths produce safe results, but the mechanism differs: a document with a NaN graph-only score is excluded from fusion, while a NaN syntactic-only score becomes 0.0 and is included. Defensible design — graph scores bypass normalizer.

## Deferred from: code review of 3-1-create-and-list-cases (2026-04-12)

- **Case creation is non-atomic across Redis and FalkorDB** — `CreateCaseAsync` writes the Redis hash before creating the FalkorDB case node, so a graph failure can leave a Redis-visible phantom case. The story already records this as an accepted MVP gap, so it remains deferred for now.

## Deferred from: code review of 3-2-case-status-and-activity.md (2026-04-12)

- **Case creation is non-atomic across Redis and FalkorDB** — `CreateCaseAsync` still writes the Redis hash before creating the FalkorDB case node, so a graph failure can leave a Redis-visible phantom case. This remains a pre-existing MVP gap from Story 3.1 rather than a regression introduced by Story 3.2.

## Deferred from: 3-3-case-member-management (2026-04-12)

- **Case deletion (Story 3.5) must cascade-delete `{tenantId}:case:{caseId}:members` key** — Story 3.3 introduces a `:members` Redis Hash key per case for member storage. When Story 3.5 implements case deletion, it must also delete this key alongside the case hash and `:activity` stream to avoid orphaned data.

## Deferred from: 3-5-memory-unit-deletion-and-case-deletion (2026-04-12)

- **Dedup key orphaning after MU deletion** — Deleting a memory unit removes it from RediSearch, Redis Vector, and FalkorDB, but the DAPR state store dedup key persists. Re-ingesting identical content is silently blocked by dedup detection, returning a stale MU ID. Fix: add dedup key TTL or explicit dedup key deletion during MU deletion. Belongs in Epic 8 (Story 8.2 consistency verification).
- **Ingestion workflow must check `CaseStatus.Deleting`** — Story 3.5 sets case status to `Deleting` during case deletion, but the ingestion workflow (`ValidateContentActivity`) does not yet check this status before creating CONTAINS edges. A concurrent ingestion during case deletion could create orphaned MUs. Wire the status check into ingestion validation.
- **Story 3.6 must extend `DeleteMemoryUnitAsync` for annotation cascade** — Story 3.5's `DETACH DELETE` removes `annotates` edges but leaves connected annotation MU nodes intact. When Story 3.6 implements annotations, `DeleteMemoryUnitAsync` must first traverse outgoing `annotates` edges, recursively delete annotation MUs, then delete the target MU.

## Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)

- **metadataQuery no length/content validation** — The `metadataQuery` query parameter has no length limit or format validation at the endpoint level, unlike `sourceType` (enum-validated) and `caseId` (existence-checked). General input validation concern across all query parameters. [Program.cs:436]
- **cancellationToken not propagated in ResolveNamesAsync** — `CaseService.ResolveNamesAsync` accepts a CancellationToken but never passes it to Redis batch operations or Task.WhenAll. StackExchange.Redis batch ops have limited cancellation support; pre-existing pattern in other batch methods. [CaseService.cs:321]
- **No input validation on caseId format before Redis key construction** — `caseId` undergoes no format validation (unlike `tenantId` which has `TenantIdGuard`). A caseId containing `:` is used directly in Redis key patterns. Defense-in-depth gap, though read-only lookups limit blast radius. [Program.cs:472]
- **No error handling for Redis failure in case name enrichment** — If Redis fails during the optional `ResolveNamesAsync` call, the entire search request returns 500 even though core search results are already available. Should degrade gracefully by returning results without case names. [Program.cs:988]

## Deferred from: code review of 5-5-tenant-configuration-and-listing (2026-04-14)

- **Keep actor-proxy fallback for tenant summaries instead of forcing the Task 1.6 state-store bypass** — Deferred by review decision. Reason: state-store key format is not empirically verified yet, so the actor fallback is the safer MVP path for now. [src/Hexalith.Memories.Server/Program.cs:1829]
- **Breaking-change conflict response still returns the wrong error contract** — `CreateEmbeddingConfigConflictResponse` still emits `error = "EmbeddingConfigChangeRequired"` instead of the pinned `EMBEDDING_CONFIG_BREAKING_CHANGE` response contract. This predates Story 5.5 and was not introduced by the current diff, so it remains deferred here. [src/Hexalith.Memories.Server/Program.cs:1888]

## Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)

- **Per-run Docker named volumes are never torn down** — Fixture generates `hexalith-memories-it-<guid>` volumes for test isolation but nothing cleans them up. CI hosts accumulate disk usage over time. [src/Hexalith.Memories.AppHost/Program.cs:175-181]
- **`_logProvider` in the fixture accumulates entries across restart lifetimes** — `RestartTopologyAsync` does not reset the shared log provider, so any future test code that captures a pre-restart index and inspects post-restart entries will see mixed lifetimes. Latent trap rather than a current bug. [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs]
- **`[DataMember]` attributes omit explicit `Name`** — Property renames on `CorpusStatistics`, `RateLimitState`, and `CaseIngestionCounts` will silently break wire format for existing persisted actor state. Set `[DataMember(Name = "...")]` explicitly before the next rename. [src/Hexalith.Memories.Server/Actors/CorpusStatistics.cs, RateLimitState.cs; src/Hexalith.Memories.Contracts/V1/CaseIngestionCounts.cs]
- **`BuildDedupKey` duplicates server-side hash logic in the test** — `PipelinePersistenceIntegrationTests.BuildDedupKey` recomputes `SHA256(sourceUri)` exactly the way the server does today. Any future change to URI normalization on the server will be invisible to the test. Replace with a server-side dedup-inspection query or an exposed helper. [tests/Hexalith.Memories.IntegrationTests/Ingestion/PipelinePersistenceIntegrationTests.cs:770-774]
- **AppHost token propagation uses process-env side effects** — `ApplyProcessEnvironmentTokens` seeds `APP_API_TOKEN` / `DAPR_API_TOKEN` into the AppHost process environment because CommunityToolkit.Aspire.Hosting.Dapr 9.7 does not expose a sidecar-scoped env API. Tokens leak to every child container/subprocess and are never unset. Revisit when the toolkit exposes a sidecar-env builder. [src/Hexalith.Memories.AppHost/Program.cs:183-198]
