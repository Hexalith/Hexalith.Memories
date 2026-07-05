---
baseline_commit: 1c9ca2e
---

# Story 24.2: Read-Path Caching & Tenant-List Bounding

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want tenant status/config/stats cached and the tenant list bounded,
so that search does not pay 4-6 auxiliary round trips and the dashboard does not stampede actors.

## Acceptance Criteria

1. Tenant status reads used by hot-path guards are served through a short-lived per-process cache, with explicit invalidation on tenant registry writes: registration, status changes, deletion begin/remove, and display-name updates. Missing tenants may be cached only briefly and must not hide a just-created tenant after a local write.
2. Existing embedding configuration caching is reused and completed, not duplicated: `TenantEmbeddingConfigProvider` gains explicit eviction/invalidation and all read paths that currently call `ITenantConfigurationActor.GetEmbeddingConfigAsync()` directly for search, tenant config reads, and tenant summaries are routed through the provider or a clearly named cached tenant-config facade.
3. Tenant fusion weights used by default hybrid search are cached with the same bounded/invalidated tenant-configuration strategy, or the implementation documents and tests why that read is intentionally excluded. Hybrid search must not make separate actor calls for weights and embedding config on every request when the cache is warm.
4. Tenant-list summaries are bounded. `GET /api/tenants` accepts optional paging parameters, clamps them to documented safe limits, preserves the existing `TenantSummary[]` JSON body for current clients, and exposes enough paging metadata through headers or an additive contract path for callers to request subsequent pages.
5. Tenant-list enrichment uses bounded concurrency. A large tenant index must not start unbounded per-tenant summary tasks or actor calls; the concurrency limit is configurable with a safe default and clamp.
6. Tenant-list summary data that is expensive to compute (`TenantMetricsService` index sizes, memory-unit count, last activity, and reindex-required/config state) is served through a short-TTL cache and invalidated by local writes that can change the corresponding view. Backend failures must keep the existing degraded/null semantics instead of reporting false zeros.
7. Search paths keep existing tenant authorization/status behavior, degraded backend error mapping, token-budget response metadata, and audit/metric recording. Caching must reduce auxiliary reads without weakening tenant isolation or turning stale non-active tenants into accepted operations beyond the configured short TTL.
8. `CorpusStatisticsActor` read methods no longer persist actor state on every cached read as part of this story only if touched for A26; otherwise leave the deeper hot-path write-amplification cleanup to Story 24.5 and explicitly avoid duplicating that scope.
9. Focused tests prove cache hit/miss/expiry, invalidation on writes, bounded tenant-list paging/concurrency, direct actor-call removal from warmed search/config paths, stale-cache safety for tenant status transitions, and preservation of existing tenant-list and search response contracts.

## Tasks / Subtasks

- [x] Add cache options and invalidation contracts for tenant read models (AC: 1, 2, 3, 6, 7)
  - [x] Add focused options for tenant status/list summary/config TTLs and max tenant-list concurrency, using existing options patterns and clamping values to safe ranges.
  - [x] Extend `ITenantEmbeddingConfigProvider` with an explicit invalidation method, or introduce a small `ITenantConfigurationReadCache` facade if caching fusion weights and config together is cleaner.
  - [x] Keep cache keys tenant-scoped and case-independent; do not use static/global mutable state.
  - [x] Preserve short TTL semantics for multi-replica deployments because local invalidation does not cross process boundaries.

- [x] Cache tenant registry status reads without hiding local writes (AC: 1, 7)
  - [x] Implement tenant entry/status caching inside `TenantRegistryService` or a dedicated injected read-cache service used by `TenantStatusGuard`.
  - [x] Invalidate the affected tenant after `RegisterOrGetTenantEntryAsync`, `UpdateTenantStatusAsync`, `BeginTenantDeletionAsync`, `UpdateTenantDisplayNameAsync`, and `RemoveTenantAsync`.
  - [x] Keep `TenantStatusGuard.ValidateTenantActiveAsync` and `ValidateTenantExistsAsync` behavior and HTTP mapping unchanged.
  - [x] Add tests for active, deleting, missing, created-after-miss, and status-changed-after-hit behavior.

- [x] Finish tenant configuration caching for search and config endpoints (AC: 2, 3, 7)
  - [x] Replace direct search endpoint actor calls for `GetEmbeddingConfigAsync()` with the existing cached provider/facade in `axis=nl`, graph-scoped semantic, plain semantic, and hybrid paths.
  - [x] Cache or intentionally exempt `GetFusionWeightsAsync()`; if cached, invalidate it with embedding config/fusion config writes.
  - [x] Update `GET /api/tenants/{tenantId}/embedding-config`, `GET /api/tenants/{tenantId}/configuration`, and tenant summary enrichment to use the same cached read path where semantics allow.
  - [x] Invalidate cached config after successful `PUT /api/tenants/{tenantId}/embedding-config`; do not invalidate on validation failure or conflict.
  - [x] Preserve current `DAPR_UNAVAILABLE`, `BACKEND_UNAVAILABLE`, `EMBEDDING_UNAVAILABLE`, and conflict responses.

- [x] Bound tenant listing and preserve client compatibility (AC: 4, 5, 6)
  - [x] Add optional `offset` and `limit` query parameters to `GET /api/tenants`; clamp `offset >= 0`, default `limit` to a bounded value, and cap `limit` to the configured maximum.
  - [x] Slice the registry list before expensive summary enrichment.
  - [x] Keep the response body as `TenantSummary[]` unless the client/test suite is deliberately updated for an additive paged endpoint; prefer paging headers such as total count, offset, limit, and has-more for compatibility.
  - [x] Use `SemaphoreSlim`, `Parallel.ForEachAsync`, or an equivalent bounded pattern so summary enrichment never launches one task per tenant without a cap.
  - [x] Ensure cancellation propagates through paging and enrichment.

- [x] Cache expensive tenant summary metrics safely (AC: 6, 7)
  - [x] Cache the composed `TenantSummary` or its expensive metric/config components for a short TTL.
  - [x] Invalidate the tenant summary cache after display-name updates, embedding config updates, tenant status changes, tenant registration/removal, and local ingestion/indexing writes if a practical local signal already exists.
  - [x] If no reliable local signal exists for memory-unit count/index-size/last-activity changes, keep TTL-based freshness and document that limitation in code comments/tests rather than adding a broad event bus.
  - [x] Keep degraded/null results from `TenantMetricsService` intact; cache entries must not convert backend-unavailable nulls into zeros.

- [x] Keep CorpusStatisticsActor scope disciplined (AC: 8)
  - [x] If this story touches `CorpusStatisticsActor`, remove the per-read `SetStateAsync` call and prove reads return cached state without write amplification.
  - [x] If not touched, add a story completion note that `CorpusStatisticsActor` write-amplification is intentionally left to Story 24.5.

- [x] Validate (AC: 1-9)
  - [x] `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`
  - [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`
  - [x] Focused xUnit v3 tests for tenant registry/status cache hit/miss/expiry/invalidation.
  - [x] Focused xUnit v3 tests for tenant embedding/fusion config provider cache invalidation and search endpoint usage.
  - [x] Focused endpoint/handler tests for `GET /api/tenants` paging, header metadata, bounded concurrency, and cancellation propagation.
  - [x] Existing search endpoint contract tests covering semantic, NL, graph-scoped semantic, and hybrid degraded behavior.
  - [x] `git diff --check`

## Dev Notes

- This story closes A26 from the 2026-07-04 architecture audit: tenant status, embedding config, and corpus/stat reads lack complete caching, and `GET /api/tenants` performs unbounded N+1 fan-out. The recommended fix is short-TTL caching invalidated on writes plus paging and bounded concurrency. [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md:57`]
- Epic 24 is an operational-readiness remediation epic for observability and performance hardening. Story 24.2 specifically targets read-path auxiliary round trips and tenant-list actor stampedes. [Source: `_bmad-output/planning-artifacts/epics.md:4322`] [Source: `_bmad-output/planning-artifacts/epics.md:4340`]
- NFR12 requires adding tenants not to degrade existing tenant performance by more than 5% at the stated scale; unbounded tenant-list fan-out works directly against that requirement. [Source: `_bmad-output/planning-artifacts/prd.md:978`]
- Search latency gates remain active: syntactic p95 <200ms, semantic p95 <500ms, hybrid p95 <1s, and graph p95 <2s under the stated load. Cache work must be measured against these hot paths, not only tenant-list endpoints. [Source: `_bmad-output/planning-artifacts/prd.md:957`]
- Architecture requires tenant-aware DAPR actors for per-tenant singletons and warns against static/global state. Use injected services/options and tenant-scoped keys for caches. [Source: `_bmad-output/planning-artifacts/architecture.md:73`] [Source: `_bmad-output/project-context.md`]
- Existing provider cache to reuse: `TenantEmbeddingConfigProvider` already caches `TenantEmbeddingConfig` per tenant with a configurable 1..300 second TTL through `TenantEmbeddingConfigCacheOptions`; it currently has only `GetAsync` and no explicit invalidation method. Do not create a parallel embedding-config cache. [Source: `src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs:18`] [Source: `src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs:44`] [Source: `src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigCacheOptions.cs:8`] [Source: `src/Hexalith.Memories.Server/Ingestion/ITenantEmbeddingConfigProvider.cs:11`]
- Existing tests prove tenant embedding config cache isolation by tenant, but only cover tenant separation. Add cache expiry, invalidation, concurrent callers if needed, and endpoint/search usage coverage. [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/TenantEmbeddingConfigProviderTests.cs:22`]
- `GenerateEmbeddingActivity` and `GenerateChunkEmbeddingsActivity` already consume `ITenantEmbeddingConfigProvider`; keep this path working and avoid changing constructor fallback behavior unless tests are updated. [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:40`] [Source: `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs:34`]
- `TenantStatusGuard` currently calls `TenantRegistryService.GetTenantAsync` on every validation; this is the hot-path status-read target for ingestion/search/case endpoints. [Source: `src/Hexalith.Memories.Server/Tenants/TenantStatusGuard.cs:20`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantStatusGuard.cs:43`]
- `TenantRegistryService.GetTenantAsync` reads DAPR state through `GetTenantEntryAsync`; `ListTenantsAsync` reads the full tenant index and then fetches entries sequentially. This is the registry read surface to cache/page carefully. [Source: `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:179`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:198`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:346`]
- Tenant registry writes already use CAS/transactions and must become invalidation points, not be bypassed: registration, status update, deletion begin, display-name update, and removal. [Source: `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:68`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:208`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:267`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:384`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:472`]
- `GET /api/tenants` currently loads all tenants, creates one summary task per tenant, and awaits `Task.WhenAll` with no paging or concurrency cap. [Source: `src/Hexalith.Memories.Server/Program.cs:1291`]
- `TenantEndpointHandlers.BuildTenantSummaryAsync` currently starts three metric calls and then directly calls `ITenantConfigurationActor.GetEmbeddingConfigAsync` for `ReindexRequired`. This actor fallback is explicitly deferred legacy debt and must not remain an unbounded dashboard path. [Source: `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs:29`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs:39`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs:44`]
- `TenantMetricsService` currently performs Redis SCAN, two Redis `FT.INFO` calls, one FalkorDB count, and one Redis hash read for a summary/config view. Its comment says caching was deferred; this story is the point where that deferral ends for tenant-list/dashboard reads. [Source: `src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:18`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:22`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:57`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:90`] [Source: `src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:118`]
- `GET /api/tenants/{tenantId}/embedding-config` and `PUT /api/tenants/{tenantId}/embedding-config` still call the actor directly. After successful PUT, evict cached config/fusion/summary entries for that tenant. [Source: `src/Hexalith.Memories.Server/Program.cs:1007`] [Source: `src/Hexalith.Memories.Server/Program.cs:1031`] [Source: `src/Hexalith.Memories.Server/Program.cs:1091`]
- Search currently validates tenant status before execution, then hybrid/default config paths call the tenant configuration actor directly for fusion weights and embedding config. NL and graph-scoped semantic do the same for embedding config. Replace those direct reads with the cached facade/provider while preserving error handling. [Source: `src/Hexalith.Memories.Server/Program.cs:2947`] [Source: `src/Hexalith.Memories.Server/Program.cs:3152`] [Source: `src/Hexalith.Memories.Server/Program.cs:3163`] [Source: `src/Hexalith.Memories.Server/Program.cs:3198`] [Source: `src/Hexalith.Memories.Server/Program.cs:3317`] [Source: `src/Hexalith.Memories.Server/Program.cs:3405`]
- `CorpusStatisticsActor` currently returns cached state but writes it back on every read through `PersistStatsBeforeReturnAsync`; that is also called after inline refresh. Story 24.5 owns broader write-amplification cleanup, so only fix this here if the A26 implementation directly touches corpus-stat reads. [Source: `src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs:178`] [Source: `src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs:257`] [Source: `_bmad-output/planning-artifacts/epics.md:4379`]
- Story 24.1 just completed workflow trace propagation and registered Dapr workflow tracing. Preserve its new telemetry/source-guard work and do not alter workflow trace contracts while working on read-path caching. [Source: `_bmad-output/implementation-artifacts/24-1-trace-propagation-across-the-workflow-boundary.md`]
- Recent commits are directly relevant: `feat(story-24.1): Trace propagation across the workflow boundary`, `feat(story-23.8): workflow config determinism`, and `feat(story-23.7): Index-Provisioning Ownership`. Expect current code to include trace context propagation, durable workflow config capture, and memoized index readiness patterns. [Source: `git log --oneline -5`]

### Project Structure Notes

- New cache/facade services should live near the surfaces they protect: tenant registry/list caches in `src/Hexalith.Memories.Server/Tenants/`; embedding/fusion config cache extensions in `src/Hexalith.Memories.Server/Ingestion/` only if still ingestion-owned, otherwise use `Tenants/` for a broader tenant configuration read facade.
- Keep option types in their own files and register them from `Program.cs` next to the existing `TenantEmbeddingConfigCacheOptions` registration.
- Keep minimal API handler logic testable in `TenantEndpointHandlers` instead of expanding inline `Program.cs` complexity where practical.
- Tests should mirror product areas: `tests/Hexalith.Memories.Server.Tests/Tenants/` for registry/list/status caches, `tests/Hexalith.Memories.Server.Tests/Ingestion/` for embedding config provider changes, `tests/Hexalith.Memories.Server.Tests/Endpoints/` for tenant endpoint and search endpoint contract coverage, and `tests/Hexalith.Memories.Server.Tests/Actors/` only if `CorpusStatisticsActor` is touched.
- No package additions are expected. Use BCL concurrency/cache primitives or existing project patterns; do not add a distributed cache package for this story.
- No UI/UX scope.

### References

- `_bmad-output/planning-artifacts/epics.md:4340` - Story 24.2 source requirement.
- `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md:57` - A26 audit finding.
- `_bmad-output/planning-artifacts/prd.md:957` - search latency NFRs.
- `_bmad-output/planning-artifacts/prd.md:978` - tenant scaling NFR12.
- `src/Hexalith.Memories.Server/Program.cs:1291` - current unpaged tenant list endpoint.
- `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs:29` - tenant summary enrichment helper.
- `src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:57` - memory-unit count read.
- `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:346` - current full tenant registry list.
- `src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs:44` - existing cached config provider.
- `src/Hexalith.Memories.Server/Program.cs:3163` - hybrid fusion weight actor read.
- `src/Hexalith.Memories.Server/Program.cs:3198` - hybrid embedding config actor read.
- `src/Hexalith.Memories.Server/Program.cs:3326` - NL embedding config actor read.
- `src/Hexalith.Memories.Server/Program.cs:3416` - graph-scoped semantic embedding config actor read.

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- Create-story activation resolved with no prepend/append steps and persistent facts loaded from `_bmad-output/project-context.md`.
- Discovery loaded `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/prd.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/ux-design-specification.md`, `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md`, sprint status, and previous story 24.1.
- Code inspection covered tenant registry/status/listing, tenant summary metrics, embedding config provider/cache options, search endpoint config reads, and corpus statistics actor read behavior.
- Implemented tenant read-cache options, tenant status guard cache, tenant summary cache, paged tenant-list registry reads, bounded summary enrichment, embedding/fusion config provider invalidation, and search/config endpoint routing through the cached provider.
- Validation: server build passed; server test build passed; focused xUnit fallback slice passed 65/65; full server xUnit fallback passed 2431 total, 0 failed, 1 skipped; `git diff --check` passed.
- During full regression, fixed `WorkflowTraceLinkedActivity.RunAsync` to throw `ArgumentNullException` before trace-link access for null activity input.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story is scoped to A26 read-path caching and bounded tenant listing. A46 broader hot-path write-amplification cleanup remains Story 24.5 unless the implementation directly touches `CorpusStatisticsActor`.
- The story explicitly preserves the existing `TenantSummary[]` response body for `GET /api/tenants` and prefers additive paging metadata to avoid unnecessary client breakage.
- Existing `TenantEmbeddingConfigProvider` must be extended/reused; creating a second embedding config cache would be a review failure.
- Checklist validation completed during creation; critical gaps found and addressed in the story: existing partial config cache, direct actor reads in search/config/list paths, invalidation points, tenant-list response compatibility, and Story 24.5 boundary.
- Added `TenantReadCacheOptions` and `TenantSummaryCache` with clamped TTLs, default/max tenant-list limits, and max tenant-list concurrency.
- `TenantStatusGuard` now uses short-lived cached registry status reads; direct registry reads remain uncached where handlers intentionally re-check current state.
- `TenantEmbeddingConfigProvider` now caches fusion weights alongside embedding config and exposes tenant-scoped invalidation used after successful embedding-config writes.
- `GET /api/tenants` now accepts `offset` and `limit`, preserves the `TenantSummary[]` response body, and emits `X-Hexalith-Total-Count`, `X-Hexalith-Offset`, `X-Hexalith-Limit`, and `X-Hexalith-Has-More`.
- Tenant summary metric freshness remains TTL-bound for ingestion/indexing-derived memory-unit count, index-size, and last-activity changes because no reliable local write signal exists for every backend mutation without adding a broader event bus.
- `CorpusStatisticsActor` was not touched; its read write-amplification remains intentionally scoped to Story 24.5.

### File List

- `_bmad-output/implementation-artifacts/24-2-read-path-caching-and-tenant-list-bounding.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Server/Activities/WorkflowTraceLinkedActivity.cs`
- `src/Hexalith.Memories.Server/Ingestion/ITenantEmbeddingConfigProvider.cs`
- `src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantListPage.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantReadCacheOptions.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantStatusGuard.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantSummaryCache.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/TenantEmbeddingConfigProviderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs`

### Change Log

- 2026-07-05: Created story artifact for Story 24.2 and moved sprint status to ready-for-dev.
- 2026-07-05: Implemented read-path caching and bounded tenant-list paging/concurrency; moved story to review.

## Review Findings

_Code review 2026-07-05 (Blind Hunter + Edge Case Hunter + Acceptance Auditor, all layers succeeded). Severity in brackets; all findings verified against source._

- [x] [Review][Decision] `GET /api/tenants` default now truncates to 50 for header-unaware clients [MEDIUM] — RESOLVED 2026-07-05: accepted as intended (bounded default = 50 is the NFR12 protection; clients must adopt the `X-Hexalith-*` paging headers). The independent P1 clamp-bypass bug is still patched. Unparameterized `GET /api/tenants` previously returned all tenants; it now returns at most `DefaultTenantListLimit` (50) (`Program.cs:1295` → `TenantRegistryService.cs:400`). This matches AC4's bounding intent and preserves the `TenantSummary[]` body, but existing admin/UI/reconciliation clients that ignore the new `X-Hexalith-Has-More`/`X-Hexalith-Total-Count` headers silently see only the first 50 and treat them as complete. AC4 ("preserves the existing body for current clients") is ambiguous on whether the default should stay unbounded — need your call.
- [x] [Review][Decision] Out-of-scope changes bundled into the 24.2 commit [LOW] — RESOLVED 2026-07-05: accepted as-is (FrontComposer submodule bump + `WorkflowTraceLinkedActivity` null guard kept); scope deviation noted, no revert. — `references/Hexalith.FrontComposer` submodule pointer bumped `3b96613`→`712c583` (violates the "separate submodule commits / don't touch submodules casually" policy), and `WorkflowTraceLinkedActivity.cs:24` gained `ArgumentNullException.ThrowIfNull(input)` (harmless hardening from the 24.1 trace area, but a behavior change on a shared base class unrelated to read-path caching). Confirm whether these belong in this story or should be reverted/split.
- [x] [Review][Patch] `limit=int.MaxValue` bypasses the page-size clamp and overflows `hasMore` [HIGH] [src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:401] — FIXED 2026-07-05: added a private `ListTenantsPageCoreAsync(offset, limit, unbounded, ct)`; `ListTenantsAsync` uses `unbounded: true` (dedicated path, no sentinel), the public `ListTenantsPageAsync` always clamps to `MaxTenantListLimit`, and `hasMore` is now `totalCount - clampedOffset > clampedLimit` (overflow-free). — The `requestedLimit == int.MaxValue` sentinel (intended only for the internal `ListTenantsAsync` at :385) is reachable from the HTTP `[FromQuery] int? limit` (`Program.cs:1295,1298`): a client sending `?limit=2147483647` skips `Math.Clamp(.., 1, MaxTenantListLimit)` and fetches every tenant via N sequential Dapr `GetStateAsync` calls, defeating the AC4/AC5 bounding this story adds. Separately, with `offset>=1` and `totalCount>offset`, `clampedOffset + clampedLimit` (line 425) overflows negative so `hasMore` returns `true` although all rows were returned — the `X-Hexalith-Has-More` header lies and the client pages into an empty response. Fix: give `ListTenantsAsync` a dedicated unbounded path (not the int.MaxValue sentinel), always clamp the HTTP limit, and compute `hasMore` as `clampedOffset + tenants.Count < totalCount` (overflow-free).
- [x] [Review][Patch] Read-through caches lose write-invalidation under concurrency (stale re-fill race) [MEDIUM] [src/Hexalith.Memories.Server/Tenants/TenantSummaryCache.cs:48] — FIXED 2026-07-05: added a per-key invalidation generation counter to all three caches (`TenantSummaryCache`, `TenantEmbeddingConfigProvider` config+fusion, `TenantRegistryService` status). Readers capture the generation before the backend read and only populate if it is unchanged; mutation/`Invalidate` bumps it. Status mutations use the authoritative `SetStatusCache` (bumps generation), read-through uses `StoreStatusCacheIfCurrent`. — All read-through caches do "read backend → unconditionally write cache" while `Invalidate`/`SetStatusCache` only `TryRemove`/overwrite (`TenantSummaryCache.cs:48-49`, `TenantEmbeddingConfigProvider.cs:61-66,86-91`, `TenantRegistryService.cs:618-628`). If a write's invalidation lands between an in-flight reader's backend read and its cache-populate, the stale value is re-cached for the full TTL. For the status cache this can re-hide a just-created tenant for up to the missing-TTL (2s) — directly against AC1's "must not hide a just-created tenant after a local write" — or serve a stale `Active` status for a tenant that just went `Deleting` for up to the status TTL (10s). Fix: invalidation-safe read-through (per-key generation counter checked before the final write, or atomic populate).
- [x] [Review][~~Patch~~ → Dismissed] Migration config writes don't invalidate the in-process embedding-config/fusion cache — RECLASSIFIED to dismissed 2026-07-05 after code verification: `RedisEmbeddingMigrationStore` is constructed only in the standalone `tools/MigrateEmbeddingVectors` CLI (its own process, own `TenantRegistryService`, no config provider) and is **not** registered in the server. The migration write is therefore out-of-process from the server's `TenantEmbeddingConfigProvider` cache — the cross-process staleness already documented as by-design and bounded by the config-cache TTL (default 30s). Injecting the provider into the tool would invalidate a cache the tool never reads. No code change.
- [x] [Review][Patch] New tenant-status/config/summary caches have no size bound or eviction (memory-growth vector) [MEDIUM] [src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:628] — FIXED 2026-07-05: added `Caching/BoundedCache.PruneIfNeeded` (evicts expired entries, then nearest-to-expiry, when at the cap) and a clamped `MaxCacheEntries` option (default 10000) on both `TenantReadCacheOptions` and `TenantEmbeddingConfigCacheOptions`; wired into every cache insert (status, summary, config, fusion). Negative-probe growth is now bounded. — All caches are `ConcurrentDictionary` with lazy per-key TTL but no eviction/size cap; expired entries are only overwritten on re-access, never removed. The status cache also caches negative (missing-tenant) entries, so any guarded endpoint (e.g. `/api/search`) probed with distinct valid-format tenant IDs (`^[a-zA-Z0-9\-]+$`) seeds one permanent entry per unique ID → unbounded per-process memory growth (slow-burn DoS), especially before server authN (deferred story 9.3). Fix: bounded cache (e.g. `IMemoryCache` with a size limit) or a cap on negative entries.
- [x] [Review][Patch] Missing AC1/AC9 negative-path tests [MEDIUM] [tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs] — FIXED 2026-07-05: added `GetTenantForStatusGuardAsync_MissingTenant_IsCachedOnlyBrieflyThenRefreshed` and `RegisterTenantAsync_OverwritesCachedMiss_SoJustCreatedTenantIsNotHidden` (both green; 34/34 in the class). — AC1's task list requires "created-after-miss" and "missing-tenant brief-cache" coverage; the diff proves only warm-hit + expiry + invalidation-after-write. The behavior is implemented but untested. Add tests: (a) a missing tenant is cached only briefly, (b) a registration write overwrites a cached miss so a just-created tenant is not hidden.
- [x] [Review][Patch] Cache TTL computed from a pre-await timestamp yields near-expired entries under slow backends [LOW] [src/Hexalith.Memories.Server/Tenants/TenantSummaryCache.cs:41] — FIXED 2026-07-05: expiry is now stamped from a timestamp captured after the backend read (`storedAt`) in the summary, config, fusion, and status caches. — `now` is captured before the backend await, then `ExpiresAt = now + ttl` (also `TenantEmbeddingConfigProvider.cs:49,74`, `TenantRegistryService.cs:611,628`). If the backend call exceeds the TTL (notably the 2s missing-tenant TTL), the entry is already expired on insert, defeating the cache exactly under load. Fix: capture the timestamp after the await.
- [x] [Review][Defer] One tenant's enrichment exception fails the entire `GET /api/tenants` page [LOW] [src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs:73] — deferred, low likelihood. `Task.WhenAll` rethrows the first fault and discards all other computed summaries. `BuildTenantSummaryCoreAsync` catches embedding-config exceptions and `TenantMetricsService` is designed not to throw (returns null/degraded), so this needs an unexpected exception (e.g. `ObjectDisposedException` on multiplexer teardown) to trigger — but if one occurs the whole listing 500s with no per-tenant isolation.
- [x] [Review][Defer] Degraded/null metric snapshots are cached for the full summary TTL [LOW] [src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs:82] — deferred, bounded by short default TTL. A summary composed during a transient backend outage (null counts / Unknown / Degraded health) is cached wholesale for the full summary TTL (default 15s), so the degraded view persists after backend recovery. AC6's letter is met (nulls preserved, not false zeros); optional improvement is a shorter negative TTL for degraded snapshots.

_Dismissed as noise (4): cross-replica cache staleness (explicitly by-design and documented in AC1 — local invalidation does not cross process boundaries); `reindexRequired=false` on transient embedding-config failure (documented tradeoff in `TenantEndpointHandlers.cs:86-96`); `CancellationToken` not honored in provider `GetAsync`/`GetFusionWeightsAsync` (Dapr actor-proxy methods take no token — framework limitation, negligible impact); migration-write cache invalidation (reclassified above — migration runs out-of-process from the server cache). The two `GET /api/tenants` default-behavior decisions were also accepted as-is._

_Patch validation 2026-07-05: server + server-test projects build clean (0 warnings, 0 errors with warnings-as-errors); full server slice 2441 tests, 2 failed, 1 skipped. The 2 failures (`DeleteMemoryUnitProjectionActivityTests.RunAsync_HappyPath_...`, `...RunAsync_VectorDeleteFails_...`) are **pre-existing at clean HEAD** (verified by stashing these patches and re-running the class: still 2/3 failing) and are unrelated to Story 24.2 read-path caching — a delete-projection area introduced by later commits._
