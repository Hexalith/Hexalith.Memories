# Test Automation Summary — Story 18.3 (Invocable Route and Operation Surface Publication)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-25 · **Role:** QA automation engineer (tests only — no code review / story validation)
- **Feature under test:** the published route/operation-surface contract `docs/operations/route-surface.md`
  and its drift-guard test `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs`.

This story is a **documentation + drift-guard test** deliverable, not a runnable feature. There is no HTTP
host stood up here and no browser surface, so "E2E" means **end-to-end contract enforcement**: the test
derives the real invocable surface from authoritative source (`Program.cs`, `EventIngestionController`,
`HealthEndpointPaths`, `Mcp/Program.cs`) and asserts it stays in lock-step with the published ACL-facing
document. No new REST/API endpoint exists to add functional API tests against — the routes already shipped in
prior epics; this pass hardens the **contract guard**.

The story shipped with **7** drift-guard `[Fact]`s. This QA pass closes **3 acceptance-criteria claims that
were required by the ACs but only review-enforced (untested)**, promoting them to mechanically test-enforced.

## Discovered Gaps (auto-applied)

| # | Gap (previously **uncovered** / review-enforced only) | AC | New gate |
|---|--------------------------------------------------------|----|----------|
| 1 | **AC4's explicit "publish-via-DAPR" statement** — that domain modules *publish CloudEvents to DAPR rather than invoking the Memories REST ingestion routes for event streams* — was not asserted by any test. The pub/sub gate tied the routes/constants but never this required sentence; a deletion of the AC4 claim would have passed. | AC4 | `PublishViaDaprStatement_IsDocumented` |
| 2 | **AC2's Dapr operation semantics** — the service-invocation mapping `/v1.0/invoke/memories/method/<path>` plus the worked translation example `method/api/search` — was review-enforced prose only. The forward/count ties cover method + path, but the "Dapr operation semantics" half of AC2 could be silently dropped. | AC2 | `DaprServiceInvocationOperationMapping_IsDocumented` |
| 3 | The two `Handlers` rows are part of the ACL-verifiable surface but **provisional** (`HXL002`). The Server stamps `X-Memories-API-Experimental: HXL002`, yet the experimental marker was review-enforced only — a code-side removal of the gate or a doc-side drop of the framing would not have failed the build. | AC1 / AC2 | `ExperimentalHandlersSurface_IsTiedToCodeAndDocumented` (code ↔ doc) |

The doc's **Automated enforcement** section was updated in the same pass so its self-description stays
accurate: the three claims moved out of the "review-enforced" bullet into explicit test-enforced bullets.

## Generated Tests

### Contract drift-guard tests (source-tied, plain `[Fact]`, no Docker/fixture)
- [x] `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` — **+3 cases** (7 → 10)
  - `DaprServiceInvocationOperationMapping_IsDocumented` — asserts `/v1.0/invoke/memories/method/` and the
    `method/api/search` worked example remain documented (AC2 semantics).
  - `PublishViaDaprStatement_IsDocumented` — asserts both halves of the AC4 claim (`publish CloudEvents to
    DAPR` + `REST ingestion routes for event streams`).
  - `ExperimentalHandlersSurface_IsTiedToCodeAndDocumented` — bidirectional tie: `Program.cs` stamps
    `X-Memories-API-Experimental` / `HXL002`, and the doc keeps the header reference + `Experimental (HXL002)`
    row marker.

All three follow the existing class idiom exactly: repo-root `.slnx` marker walk, `ShouldContain(…,
Case.Sensitive, …)`, ITANEO MIT header, file-scoped namespace `Hexalith.Memories.Server.Tests.Deployment`,
no `using Xunit;`, no new packages. Each reads its inputs fresh (no shared state, no order dependency, no
sleeps).

## Coverage

- AC coverage hardened by this pass: **AC2** (Dapr operation semantics, was prose-only), **AC4** (publish-via-DAPR
  statement, was untested), **AC1/AC2** (experimental-surface marker, was review-only).
- `RouteSurfaceContractTests`: **10/10** guards now enforce every AC mechanically — the only items left
  review-enforced are the framework-emitted `/dapr/subscribe` (already doc-presence + `MapSubscribeHandler()`
  tied) and per-row purpose prose.
- `/api/*` route enumeration: **45/45** still forward- and count-tied (unchanged).

## Validation (sandbox-safe runner)

- Build: `0` warnings / `0` errors under `TreatWarningsAsErrors=true`.
- New gap tests: **3/3** pass; full `RouteSurfaceContractTests` class: **10/10** pass.
- **Negative-proof:** each new guard was mutated on disk (doc token swapped) and confirmed to **FAIL**, then
  restored and confirmed to **pass** — proving the guards are live, not vacuous.
- Full `Hexalith.Memories.Server.Tests` suite: **1871 passed, 0 failed, 1 skipped** (was 1868 → **+3**, no
  regressions).
- `Hexalith.Memories.Cli.Tests` `CiTestInventoryTests`: **48/48** pass (`deferred-work.md` schema unaffected).
- Runner: `DiffEngine_Disabled=true dotnet exec …/Hexalith.Memories.Server.Tests.dll -class …` (VSTest
  `dotnet test` is blocked in this WSL sandbox by `SocketException (13)`).

## Checklist (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API tests generated (N/A — no new HTTP host/endpoint; contract is a published surface doc)
- [x] E2E (end-to-end contract enforcement) tests generated
- [x] Tests use standard test framework APIs (xUnit v3 / Shouldly)
- [x] Tests cover happy path (each AC claim present in code + doc)
- [x] Tests cover critical cases (drift detection — negative-proven by mutation)
- [x] All generated tests run successfully (3/3 new; class 10/10; suite 1871, 0 failed)
- [x] Proper locators (authoritative-source marker walk + `Case.Sensitive` literal ties — no brittle line refs)
- [x] Clear, descriptive test names
- [x] No hardcoded waits or sleeps
- [x] Tests are independent (each reads inputs fresh; no order dependency, no shared state)
- [x] Test summary created, saved under `implementation-artifacts/tests/`, includes coverage metrics

## Next Steps

- Run in CI alongside the existing `Deployment/*ContractTests` drift guards.
- OpenAPI/Swagger document emission remains deferred as `MEM-3-OPENAPI` in `deferred-work.md` — **out of scope**
  for this pass and unchanged.
