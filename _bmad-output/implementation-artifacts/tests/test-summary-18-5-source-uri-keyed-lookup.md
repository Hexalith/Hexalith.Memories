# Test Automation Summary — Story 18.5

**Feature:** Source-URI-Keyed Memory-Unit Lookup Endpoint
**Story:** `18-5-source-uri-keyed-memory-unit-lookup-endpoint`
**Workflow:** `bmad-qa-generate-e2e-tests` (gap-fill mode — story already implemented at status `review`)
**Date:** 2026-06-25
**Framework detected:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + NSubstitute (5.3.0). Matched the
project's existing test stack; no new framework introduced.
**Run command (sandbox):** built the test project, then ran the xUnit v3 assembly directly with
`DiffEngine_Disabled=true dotnet exec <test.dll> -class <FQN>` (`dotnet test`/VSTest socket is blocked
here — `SocketException 13`, per the story's Dev Agent Record).

## Scope

Story 18.5 ships a REST + CLI feature (no UI), so coverage is **API / contract / unit level** — there is
no browser E2E layer to generate. The story landed with a full test suite already; this QA pass scanned
each layer's defined behaviours against its tests, found untested branches, and **auto-applied** gap-fill
tests.

## Gaps Discovered and Applied

| # | Layer | Untested defined branch | AC | Test added |
| - | ----- | ----------------------- | -- | ---------- |
| 1 | Lookup seam | Different **case** misses via a distinct dedup key (only different-tenant was covered) | AC5 | `ResolveMemoryUnitIdAsync_DifferentCase_MissesViaDistinctKey` |
| 2 | Lookup seam | Constructor `null` redis argument guard | — | `Constructor_NullRedis_ThrowsArgumentNullException` |
| 3 | Lookup seam | Propagation is not limited to a connection drop — any `RedisException` subtype propagates | AC6 | `ResolveMemoryUnitIdAsync_RedisServerError_Propagates` |
| 4 | Endpoint | A non-connection `RedisException` (server error) still maps to `503` (catch is on the base class) | AC6 | `HandleAsync_RedisServerError_Returns503BackendError_NotFalse404` |
| 5 | Client | `2xx` with a `null` body → structured `INVALID_RESPONSE` (empty-body throw branch), never a silent miss | AC4 | `LookupAsync_200EmptyBody_ThrowsInvalidResponse_NotNull` |
| 6 | Client | `2xx` with an unparseable body → `INVALID_RESPONSE` (JsonException catch branch) | AC4 | `LookupAsync_200UnparseableBody_ThrowsInvalidResponse` |
| 7 | CLI command | Blank **tenant**/**case** (whitespace) → `Plumbing` exit, client untouched (only blank source-uri was covered) | AC4 | `Run_BlankTenantOrCase_ReturnsPlumbingExitCode` (theory, 2 cases) |

## Generated Tests

### Seam — `tests/Hexalith.Memories.Server.Tests/Ingestion/SourceUriMemoryUnitLookupTests.cs`

- [x] Existing: hit → id; miss → null; transient `reserved` marker → null (AC3); exact-key build / no re-hash (AC2); different-tenant miss (AC5); connection-down propagates (AC6); blank-input guards.
- [x] **Added:** different-case miss (AC5); null-redis ctor guard; `RedisServerException` propagates (AC6).

### Endpoint — `tests/Hexalith.Memories.Server.Tests/Endpoints/MemoryUnitLookupEndpointTests.cs`

- [x] Existing: `200` + id; `404` structured not-found; transient-reserved → `404` (AC3); invalid tenant `400` (AC5); blank source-uri `400`; Redis-down `503` (AC6); cross-tenant `404` (AC5); different-case `404` (AC5); literal-route precedence via the real router (AC1).
- [x] **Added:** `RedisServerException` → `503 LOOKUP_BACKEND_UNAVAILABLE`, not a false `404` (AC6).

### Client — `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientLookupTests.cs`

- [x] Existing: `200` → id; `404` → null; `503` → `MemoriesRemoteException` (not null); path+query URL-encoding; blank-arg guards.
- [x] **Added:** `2xx` null body → `INVALID_RESPONSE`; `2xx` unparseable body → `INVALID_RESPONSE` (AC4).

### CLI — `tests/Hexalith.Memories.Cli.Tests/Cli/SearchLookupCommandTests.cs`

- [x] Existing: found → prints id + success (human); found JSON envelope; not-found → `NotFound` exit + error envelope; blank source-uri → `Plumbing`; missing required option → non-success without calling the client.
- [x] **Added:** blank tenant / blank case → `Plumbing` exit, client untouched (theory).

### Contract — `tests/Hexalith.Memories.Contracts.Tests/V1/MemoryUnitIdLookupSerializationTests.cs`

- [x] Existing (no gap): camelCase emit; round-trip; camelCase wire bind. Left unchanged.

## Coverage by Acceptance Criterion

| AC | Description | Status |
| -- | ----------- | ------ |
| AC1 | Exact keyed lookup, structured `404`, literal-route precedence | Covered |
| AC2 | Reuse permanent dedup record, no parallel store | Covered |
| AC3 | Transient reservation marker excluded | Covered |
| AC4 | Additive contract + client + CLI surface, error mapping | Covered (**+3 gaps**) |
| AC5 | Tenant + case isolation, cross-tenant/different-case → not-found | Covered (**+2 gaps**) |
| AC6 | Backend outage → structured `503`, never a false `404` | Covered (**+2 gaps**) |

## Results

| Test project (classes run) | Build | Result |
| -------------------------- | ----- | ------ |
| `Hexalith.Memories.Server.Tests` (seam + endpoint) | 0 warnings | **25 passed, 0 failed, 0 skipped** |
| `Hexalith.Memories.Cli.Tests` (client + CLI) | 0 warnings | **16 passed, 0 failed, 0 skipped** |

7 new test cases (5 facts + 2 theory data rows) added; both projects build clean under the
warnings-as-errors gate; all touched classes green.

## Next Steps

- A real-Redis race / two-thread concurrency proof for the seam belongs in the deferred
  Aspire/Testcontainers lane (substitute-based determinism is used here per the story's Dev Notes).
- No further gaps identified — all defined success / validation / failure branches of the lookup seam,
  endpoint, client method, CLI command, and contract are now covered.
</content>
