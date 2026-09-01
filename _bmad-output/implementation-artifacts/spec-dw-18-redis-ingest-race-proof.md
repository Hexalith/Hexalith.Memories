---
title: 'DW-18 real Redis ingest reservation race proof'
type: 'chore'
created: '2026-09-01'
status: 'done'
baseline_revision: 'fa1ecb526ebb77b49704e68904225a6f87b130c0'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - 'tests/README.md'
warnings:
  - 'oversized'
deferred: []
---

<intent-contract>

## Intent

**Problem:** Story 18.4 proves its atomic ingest reservation only with substitute-controlled sequential outcomes; it does not exercise the production `SET NX` implementation under a real two-thread Redis race. DW-18 therefore remains open before any production concurrency claim.

**Approach:** Add a Docker-backed integration-fast test that releases exactly two worker threads against the production `IngestDedupReservation`, proves one winner and one duplicate loser, and verifies the winning instance identifier and TTL persisted in real Redis. Bind the exact proof method into the existing integration-fast required-surface manifest.

## Boundaries & Constraints

**Always:** Reuse `RedisStackFixture`; call the production reservation class; use two `Task.Run` workers and a bounded two-party `Barrier`; use unique tenant/case/source values; assert outcomes, winner/loser identifier linkage, Redis value, and positive bounded TTL; delete only the test key in `finally`; keep the test in the fast `Category=Integration` lane. Preserve the existing focused cross-tenant key-isolation evidence.

**Block If:** The production reservation seam or stable Redis fixture is unavailable, or the real Redis proof cannot be executed in the available Docker lane. Do not replace the requested proof with mocks or a sequential simulation.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; place Testcontainers code in `Server.Tests`; change reservation semantics, packages, AppHost topology, or CI job structure; use `FLUSHDB`; assume which contender wins; start the full Aspire/Dapr/FalkorDB topology for this Redis-only boundary test; conditionally skip when Docker is unavailable.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Real race | Two distinct candidate IDs cross a bounded barrier and reserve one unique identity in real Redis | Exactly one `Reserved`, one `DuplicateInFlight`, zero `FailOpen`; both results identify the winner; Redis stores the winner with a live TTL | Any other outcome fails the test |
| Rendezvous failure | Both workers do not reach the barrier within the bound | No indefinite hang | Throw a timeout failure |
| Test completion or failure | The unique reservation key may exist | Only that exact key is deleted | Cleanup runs in `finally` |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs` -- read-only production seam: `TryReserveAsync` performs `StringSetAsync(..., When.NotExists)`, returns `Reserved`/`DuplicateInFlight`/`FailOpen`, and reads the winner ID after a lost reservation.
- `src/Hexalith.Memories.Server/Activities/Ingestion/DedupKeyBuilder.cs` -- read-only key derivation used to calculate the exact `ingest-reserve:` Redis key for end-state assertions and cleanup.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs` -- digest-pinned Testcontainers Redis Stack fixture with readiness checks and a shared real `IConnectionMultiplexer`.
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestDedupReservationIntegrationTests.cs` -- new production-backed two-thread race proof; `Hexalith.Memories.Server` already grants this assembly internals access.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` -- retain deterministic unit coverage; update its class note to point to the new real-Redis authority.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/DedupKeyBuilderTests.cs` -- existing focused negative evidence: different tenant/case inputs produce different reservation identities.
- `tools/integration-fast-required-surfaces.txt` -- add the exact class/method identity so trait drift, skipping, or removal fails the post-lane coverage gate.
- `.github/workflows/ci.yml` and `tools/verify-integration-fast-coverage.py` -- read-only lane/gate definitions; integration-fast already builds and runs the target project against Docker and checks passed TRX methods.
- `.bmad-loop/runs/20260901-065621-43db/bundles/redis-ingest-race-proof/intent.md` -- read-only bundle intent and verbatim DW-18 source.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestDedupReservationIntegrationTests.cs` -- add the RedisStack collection test with a bounded two-thread rendezvous, dynamic-winner assertions, persisted value/TTL proof, and exact-key cleanup.
- [x] `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` -- replace the stale “future real-Redis test” note with the split authority: deterministic unit proof here, production-backed race proof in IntegrationTests.
- [x] `tools/integration-fast-required-surfaces.txt` -- require the exact new race method in successful integration-fast TRX evidence.

**Acceptance Criteria:**
- Given a live Redis Stack fixture and two contenders for the same tenant/case/source identity, when both threads call the production reservation concurrently, then exactly one result is `Reserved`, exactly one is `DuplicateInFlight`, neither is `FailOpen`, and the loser observes the dynamic winner's instance ID.
- Given the completed race, when Redis is inspected before cleanup, then the exact production reservation key stores the winner ID and has a non-null TTL greater than zero and no greater than the supplied TTL.
- Given the integration-fast required-surface gate, when the test is removed, renamed, skipped, fails, or loses its fast integration trait, then CI cannot satisfy the exact required class/method entry.
- Given existing tenant/case key isolation rules, when focused `DedupKeyBuilderTests` run, then different tenant and case inputs remain proven non-colliding; no routing selector changes are introduced by this test-only bundle.

## Spec Change Log

## Review Triage Log

### 2026-09-01 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 5: (high 0, medium 3, low 2)
- defer: 0
- reject: 16
- addressed_findings:
  - `[medium]` `[patch]` Increased the normal two-contender rendezvous bound from five to thirty seconds to avoid CI thread-pool pressure causing false failures while preserving the short injected timeout proof.
  - `[low]` `[patch]` Added a TTL lower bound with a thirty-second observation allowance so an incorrectly tiny positive TTL cannot satisfy the persisted-state proof.
  - `[medium]` `[patch]` Routed race cleanup through production `IngestDedupReservation.ReleaseAsync` so future production key-construction changes cannot leak the actual reservation; retained the direct absence assertion for the inspected key.
  - `[medium]` `[patch]` Moved both sentinel-test setup writes inside nested cleanup protection so partial setup and assertion failures still delete both unique keys.
  - `[low]` `[patch]` Asserted both real Redis setup writes succeeded so the key-scoped cleanup proof cannot pass vacuously.

## Design Notes

The ledger's historical `Server.Tests` location cannot host this proof safely: that project is the Docker-free mocked lane, while `tools/test-projects.integration-fast.txt` executes only `Hexalith.Memories.IntegrationTests`. The integration assembly already references Server, owns the stable Redis fixture, and has `InternalsVisibleTo`, so relocation to the integration project is the minimal executable interpretation of the intent.

## Verification

**Commands:**
- `docker version && docker info` -- expected: Docker client and daemon are available.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release -m:1` -- expected: zero errors and warnings-as-errors remain clean.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Ingestion.IngestDedupReservationIntegrationTests` -- expected: two passed, zero failed/skipped against a real container, covering the race plus bounded rendezvous-failure/key-scoped cleanup rows.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release -m:1 && DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Activities.Ingestion.DedupKeyBuilderTests` -- expected: focused tenant/case negative evidence passes.
- `python3 -m unittest discover -s tests/tooling/integration_fast_coverage -p '*_test.py'` -- expected: required-surface parser and passed-outcome enforcement tests pass.

## Auto Run Result

Status: done

Summary: Added a production-backed two-thread Redis reservation race test in the stable Docker integration lane. The proof establishes exactly one `Reserved` winner, one `DuplicateInFlight` loser that observes the winner ID, a persisted winner value with the requested TTL, bounded rendezvous failure, and key-scoped cleanup. The exact race method is now mandatory integration-fast evidence.

Files changed:
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestDedupReservationIntegrationTests.cs` -- added the real Redis race and matrix-edge integration tests with production reservation/release calls.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` -- updated the authority note to distinguish deterministic substitute coverage from the real Redis proof.
- `tools/integration-fast-required-surfaces.txt` -- pinned the exact passed race method in the integration-fast coverage gate.
- `_bmad-output/implementation-artifacts/spec-dw-18-redis-ingest-race-proof.md` -- recorded intent, implementation scope, verification, review triage, and completion evidence.

Review findings breakdown: 5 patches applied (high 0, medium 3, low 2); 0 items deferred; 16 items rejected. No intent gap or bad-spec loopback was required.

Follow-up review recommendation: true. Patched finding score = `(3 × medium 3) + (1 × low 2) = 11`; no high-severity patch was present.

Verification performed:
- `docker version && docker info` -- passed; Docker client/server 29.6.1 available.
- IntegrationTests Release build -- passed with 0 warnings and 0 errors.
- `IngestDedupReservationIntegrationTests` -- 2 passed, 0 failed, 0 skipped against the digest-pinned real Redis Stack container; all I/O matrix rows executed.
- Server.Tests Release build -- passed with 0 warnings and 0 errors.
- `DedupKeyBuilderTests` -- 13 passed, including focused different-tenant and different-case negative evidence.
- Integration-fast verifier fixtures -- 6 passed.
- `git diff --check` -- passed.

Residual risks: The complete integration-fast suite was not run locally; the exact new race method is pinned in `tools/integration-fast-required-surfaces.txt` so CI must execute and pass it. The deferred-work ledger was intentionally not edited; the orchestrator owns resolution recording.
