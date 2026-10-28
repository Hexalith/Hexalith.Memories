---
title: Stable BM25 raw search windows for Tenants DW-122
status: done
created: 2026-10-28
baseline_commit: 72c36c7113c750c8b809f9d72b945a2a8c2d4555
---

# Stable BM25 raw search windows

This owning-repository companion records the upstream portion of the approved Tenants
`spec-dw-122-334-358-579-595-search-and-dependency-qualification` batch. The user approved
implementation and main publication on 2026-10-28. It adds no new product scope.

For an unchanged query/index, select raw positions by BM25 relevance descending and unique
Redis key ascending before pagination. Preserve tenant/graph scope, highlighting, summaries,
malformed-document raw positions, totals and existing error classification. No schema,
package-version, cursor, authorization or production-provider changes are included.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` - preserve endpoint backend-unavailable mapping for incomplete search.
- `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs` - stable raw ordering and hydration.
- `tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs` - real Redis contracts.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs` - expose pinned image to dedicated test servers; preserve timeout policy.
- `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs` - graph scope regression.
- `_bmad-output/implementation-artifacts/spec-dw-122-stable-search-windows.md` - owning scope and evidence.

Read/verify only:

- `CONTRIBUTING.md`
- `tools/tenant-isolation-surfaces.txt`

## Acceptance and verification

Given tied scores across page boundaries, adjacent/repeated windows return each eligible
identity once in deterministic relevance order under RESP2 and RESP3. Excluded graph keys
remain excluded before selection. Invalid documents consume positions before filtering.

Executed Debug/source builds and direct xUnit assembly runs pass with zero build warnings/errors:

- Integration classes `SyntacticSearchIntegrationTests` and `GraphScopedSearchIntegrationTests`: 82/82, no skips.
- Server namespace `Hexalith.Memories.Server.Tests.Search.*`: 271 Search plus 25 endpoint cases; 296/296, no skips.
- Original production source fails 18 expanded cases (36 run); first-pass four-case failure remains recorded in Tenants evidence.

Exact commands:

```bash
dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
MEMORIES_TEST_OWNED_NATIVE=1 MEMORIES_TEST_REDISSTACK_CONNECTION=127.0.0.1:56818 dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Search.SyntacticSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.GraphScopedSearchIntegrationTests
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class 'Hexalith.Memories.Server.Tests.Search.*'
```

Tenants retains complete raw outputs and preserved failed attempts under
`_bmad-output/implementation-artifacts/evidence/dw-122-334-358-579-595/` in its owning repository.
The temporary owned Redis container was removed after execution; recreating it is required
to reuse the recorded endpoint. The guarantee is for an unchanged index, without snapshot
isolation across mutations. No package publication or full-suite qualification is claimed.

## Cross-Tenant Negative Evidence

**Surfaces:** Tenant-scoped index and validated graph key selection remain in the existing service.
**Tests:** `SyntacticSearchServiceTests.SearchAsync_WithGraphScopeKeys_WhenKeyIsNotTenantScoped_ShouldThrowBeforeRedisCommand`
**Command:** `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class 'Hexalith.Memories.Server.Tests.Search.*'`
**Result:** 271 passed; the existing cross-tenant key test rejects before any Redis ExecuteAsync command. Graph exclusions also pass the real integration suite. The unchanged Story 20.2 and 24.3 authority/marker boundaries remain in force.

## Dev Agent Record

### File List

- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs`
- `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs`
- `tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs`
- `_bmad-output/implementation-artifacts/spec-dw-122-stable-search-windows.md`

Iteration 1 keeps bounded server timeouts and uses FT.PROFILE completion warnings in both
protocols. A mismatched aggregate cap or hydration count fails explicitly, preserving safe
successful raw windows. Whitespace case IDs normalize to null. The real matrix covers
curated tenant upserts/source identities, malformed/exhausted windows, timeout RETURN,
unequal caps and a query term literally named warning. The shared fixture FAIL policy
remains unchanged; dedicated disposable servers exercise alternate settings. No server-global
production configuration is mutated. Source SHA-256 is
dda55f1f015a168f2d23682f0e04a8dd7d1329ca4ad5090256598fe06f6f2f14.

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-10-28 | create-story | Transcribed already-approved upstream scope into owning repository | 0; scope transcription | matched 4/4 approved initial paths |
| 2026-10-28 | dev-story | Initial stable windows and regressions | 43 integration and 271 search unit passed | matched 4/4 initial owning paths |
| 2026-10-28 | code-review | First independent review identified completeness/normalization gaps and evidence-publication corrections | 43 integration and 271 unit initial evidence retained | matched 4/4 initial owning paths |
| 2026-10-28 | dev-story | Re-derived bounded complete windows, expanded matrix and preserved shared fixture policy | 66 integration and 271 search unit passed | matched 5/5 current owning paths |
| 2026-10-28 | code-review | Final review requested typed API failures and hydration identity/race controls | 66 integration and 271 unit prior evidence retained | matched 5/5 pre-patch owning paths |
| 2026-10-28 | dev-story | Typed backend failures, exact raw hydration identities, endpoint mapping and race/protocol controls | 82 integration and 296 Search/endpoint passed | matched 6/6 final owning paths |
| 2026-10-28 | code-review | Independent scoped blind and verification rechecks clear; parent final tests and package/live binding pass | 82 integration and 296 Search/endpoint passed | matched 6/6 final owning paths |

Final checks use the self-contained standard integration fixtures (omit the historical native
endpoint override) and the full changed SearchEndpointContractTests class in addition to Search units.
No new deferred work was created. The initial baseline is retained; three independent upstream
release/evidence commits were fast-forwarded before publication without overlapping search files.
