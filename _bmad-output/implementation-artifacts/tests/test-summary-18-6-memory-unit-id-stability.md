# Test Automation Summary — Story 18.6

**Feature:** MemoryUnitId Stability Contract
**Story:** `18-6-memory-unit-id-stability-contract`
**Workflow:** `bmad-qa-generate-e2e-tests` (gap-fill mode — story already implemented at status `review`)
**Date:** 2026-06-25
**Framework detected:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + NSubstitute (5.3.0). Matched the
project's existing test stack; no new framework introduced.
**Run command (sandbox):** built the test project, then ran the xUnit v3 assembly directly with
`DiffEngine_Disabled=true dotnet exec <test.dll> -class <FQN>` (`dotnet test`/VSTest socket is blocked
here — `SocketException 13`, per the story's Dev Agent Record).

## Scope

Story 18.6 is a **documentation + drift-guard** story (no UI, no new public API), so coverage is at the
**doc-contract + behavioural / unit level** — there is no browser E2E layer to generate. The story landed
with its doc-text drift guards (`MemoryUnitIdStabilityContractTests`) and several supporting behavioural
tests already green. This QA pass scanned the **runtime behaviour the published contract describes** against
its tests, found one genuine uncovered branch family, and **auto-applied** the gap-fill tests.

## Gaps Discovered and Applied

| # | Layer | Untested defined behaviour | Contract claim | Test added |
| - | ----- | -------------------------- | -------------- | ---------- |
| 1 | `IngestionWorkflow.ResolveMemoryUnitId` | For an **ordinary file/url** ingest the workflow returns `context.InstanceId` verbatim, so the workflow instance id **is** the `MemoryUnitId` (and is reused as-is — opaque, not parsed/validated). Both pre-existing instance-id tests were `SourceType.Event` only; the common file path that every REST/file ingest takes had **no** coverage. | §1 table: "the Dapr workflow instance id … for ordinary file/url ingests" | `RunAsync_FileSource_WithStableInstanceId_ShouldReuseInstanceIdAsMemoryUnitId` |
| 2 | `IngestionWorkflow.ResolveMemoryUnitId` | The `dedup:`-prefix regeneration is gated **strictly** on `SourceType.Event` (`RequiresIndependentMemoryUnitId`). The same `dedup:`-prefixed instance id that mints an independent id for an Event source must be **reused** for a file/url source. No test pinned the Event-only gate, so a regression broadening regeneration to all source types would silently change file/url ids and break the workflow-id == memory-id stability the lookup relies on. | §1 "EventStore `dedup:`-prefixed workflow instance ids"; §2 stability guarantee | `RunAsync_NonEventSourceWithDedupPrefixedInstanceId_ShouldReuseInstanceId` |

## Generated Tests

### Workflow — `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs`

- [x] Existing: `dedup:`-prefixed **Event** instance id → independent `NewGuid` id
  (`RunAsync_EventStoreDedupInstanceId_ShouldGenerateIndependentMemoryUnitId`); stable **Event** instance id
  reused (`RunAsync_EventReingestionWithStableInstanceId_ShouldReuseMemoryUnitId`); duplicate short-circuit
  returns existing id without re-indexing (`RunAsync_DuplicateSource_ShouldReturnEarlyWithExistingId`); token
  augment-not-replace dual permanent records
  (`RunAsync_SuccessfulIngestionWithToken_ShouldPersistBothSourceUriAndTokenDedupRecords`).
- [x] **Added:** file/url source reuses a concrete non-`dedup:` instance id as the `MemoryUnitId` and threads
  it into indexing + the permanent dedup write; non-Event source reuses a `dedup:`-prefixed instance id
  verbatim (proves the Event-only regeneration gate).

### Drift guard — `tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs`

- [x] Existing (no gap): doc presence; opaque / not-source-derived / not-ULID claims; TTL-less
  (`expiry: null`) dependency; loss/failure modes + id-resolution-authority claim; Story 18.5 lookup as the
  authoritative resolution path; Story 18.4 token augment-never-replace; Parties "decision D1" clarification;
  and the bidirectional doc↔code ties (`SaveDedupKeyActivity`, `DedupKeyBuilder`, `SourceUriMemoryUnitLookup`).
  Left unchanged.

### Supporting behavioural classes (reviewed, no gap)

- [x] `Activities/Ingestion/SaveDedupKeyActivityTests.cs` — TTL-less write (`expiry: null`, `When.Always`) +
  key/value + propagation. Covered.
- [x] `Activities/Ingestion/DedupKeyBuilderTests.cs` — source-URI key shape, `:tok:` namespace,
  never-collide augment invariant, tenant/case isolation, hash shape. Covered.
- [x] `Activities/Ingestion/CheckIdempotencyActivityTests.cs` — token precedence then source-URI fallback,
  transient-reservation exclusion, propagation. Covered.
- [x] `Ingestion/SourceUriMemoryUnitLookupTests.cs` + `Endpoints/MemoryUnitLookupEndpointTests.cs` +
  `Cli.Tests/ClientRest/MemoriesClientLookupTests.cs` — the Story 18.5 lookup seam/endpoint/client
  (hit/miss/marker/isolation/backend-error). Covered (these were gap-filled under the Story 18.5 QA pass).

## Coverage by Acceptance Criterion

| AC | Description | Status |
| -- | ----------- | ------ |
| AC1 | Precise stability guarantee; opaque / not-source-derived / not-ULID | Covered (**+2 behavioural gaps** on `ResolveMemoryUnitId`) |
| AC2 | TTL-less dedup-record lifetime dependency, doc↔code tied | Covered |
| AC3 | Parties "decision D1" ≠ Memories Architecture Decision D1 | Covered |
| AC4 | Loss/failure modes documented; dedup record is the id authority | Covered |
| AC5 | Story 18.5 lookup is the authoritative resolution path | Covered |
| AC6 | Token records augment, never replace, the source-URI record | Covered |

## Results

| Test class run | Build | Result |
| -------------- | ----- | ------ |
| `IngestionWorkflowTests` (workflow) | 0 warnings | **41 passed, 0 failed, 0 skipped** (was 39; **+2**) |
| `MemoryUnitIdStabilityContractTests` (drift guard) | 0 warnings | **10 passed, 0 failed, 0 skipped** |
| **Full `Hexalith.Memories.Server.Tests` assembly** | 0 warnings | **1942 passed, 0 failed, 1 skipped** (was 1940; **+2**; 1 pre-existing skip) |

2 new `[Fact]` tests added; the project builds clean under the warnings-as-errors gate; full regression green.
Files normalized to CRLF per `.editorconfig`.

## Next Steps

- A real-Redis ingest→lookup round-trip (re-ingest returns the same id; dedup-record deletion re-mints a new
  id) belongs in the deferred Aspire/Testcontainers integration lane, not the sandboxed unit layer — the
  substitute-based workflow tests pin the id-resolution branches deterministically here.
- No further gaps identified — the contract's documented runtime behaviour (id resolution for both
  source families, TTL-less permanent record, token augment-not-replace, duplicate short-circuit, and the
  Story 18.5 lookup seam) is now covered by behavioural tests in addition to the doc-text drift guards.
