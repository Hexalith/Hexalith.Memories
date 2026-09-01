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
deferred:
  - summary: >-
      The REST /api/v1/ingest DuplicateInFlight branch — the loser receiving the winner's instance id
      instead of scheduling a second workflow — has no test at any level.
    evidence: |-
      Grepping DuplicateInFlight across src/ and tests/ returns only IngestDedupReservation.cs,
      IngestDedupReservationTests.cs (substitute unit) and the new IngestDedupReservationIntegrationTests.cs.
      The endpoint branch at src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:127 that returns
      Accepted(IngestStatusLocation(winnerInstanceId)) is observed by none of them, so returning the caller's
      own instance id there — or falling through to a second ScheduleAsync — would not fail any test.
      Pre-existing: this bundle adds the class-level race proof and does not touch the endpoint.
    location: >-
      src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:127
    severity: medium
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

### 2026-09-01 — Review pass (follow-up)
- intent_gap: 0
- bad_spec: 0
- patch: 2: (high 0, medium 2, low 0)
- defer: 1: (high 0, medium 1, low 0)
- reject: 22
- addressed_findings:
  - `[medium]` `[patch]` The key-scoped cleanup proof was tautological: the second test deleted the
    reservation key with its own `database.KeyDeleteAsync` and then asserted the sibling `:sentinel` key
    survived, so no line of it entered production code and a prefix-scoped or over-broad delete inside
    `IngestDedupReservation.ReleaseAsync` could not fail it. The deletion under proof is now the production
    `ReleaseAsync` call, and the test is renamed
    `ReleaseAsync_AfterBoundedRendezvousTimeout_TimesOutAndDeletesOnlyTheReservationKey` so its subject is a
    production member rather than the file's private test helper. The bounded-rendezvous `TimeoutException`
    proof and the outer sentinel cleanup are unchanged.
  - `[medium]` `[patch]` The race ran exactly once, so a non-atomic check-then-set regression passed whenever
    the two calls happened not to overlap — the pinned CI gate rested on a single scheduling interleaving.
    The race now repeats over `RaceRounds = 25` fresh identities inside the same pinned method, each round
    keeping every outcome, winner-linkage, persisted-value and TTL assertion, with the round index carried in
    the count assertions' failure messages. Verified by mutation: replacing the production `SET NX` with
    `KeyExistsAsync` + unconditional `StringSetAsync` fails the test (`race round 0 must have exactly one
    winner`); production source was restored and re-verified green.

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

Summary: Follow-up review pass over the DW-18 bundle. The production-backed two-thread Redis reservation race
already existed in the stable Docker integration lane; this pass hardened the two ways it could pass without
proving anything. The race is now repeated over 25 fresh identities so a lost-update regression cannot slip
through a single lucky interleaving, and the key-scoped cleanup row is now proven against production
`ReleaseAsync` instead of the test's own key delete. No intent gap or bad-spec loopback was required.

Files changed:
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestDedupReservationIntegrationTests.cs` -- race repeated over
  `RaceRounds = 25` fresh identities with round-indexed failure messages; cleanup test rerouted through production
  `ReleaseAsync` and renamed to a production-member subject.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` -- authority note split between the
  deterministic substitute proof and the real-Redis race proof (unchanged this pass).
- `tools/integration-fast-required-surfaces.txt` -- pins the exact passed race method; the method name was deliberately
  not renamed, so the gate entry still resolves.
- `_bmad-output/implementation-artifacts/spec-dw-18-redis-ingest-race-proof.md` -- triage log, deferred item, and this
  completion record.

Review findings breakdown: 2 patches applied (high 0, medium 2, low 0); 1 item deferred; 22 items rejected.

Follow-up review recommendation: true. Patched finding score = `(3 x medium 2) + (1 x low 0) = 6`, which is 5 or
more; no high-severity patch was present.

Verification performed:
- `docker version` -- passed; Docker server 29.6.1 available.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/... --configuration Release -m:1` -- passed, 0 warnings, 0 errors.
- `IngestDedupReservationIntegrationTests` -- Total 2, Failed 0, Skipped 0 against the digest-pinned real Redis Stack
  container (3.5s wall clock for all 25 race rounds plus the cleanup test).
- Mutation check of the race proof -- replacing the production `SET ... NX` with a check-then-set produced
  `Total 2, Failed 1` with `race round 0 must have exactly one winner`; `src/Hexalith.Memories.Server/Ingestion/`
  `IngestDedupReservation.cs` was then restored (`git checkout --`), confirmed content-identical to HEAD, rebuilt,
  and re-run green.
- `dotnet build tests/Hexalith.Memories.Server.Tests/... --configuration Release -m:1` -- passed, 0 warnings, 0 errors.
- `DedupKeyBuilderTests` -- Total 13, Failed 0, including the focused different-tenant and different-case negative
  evidence the intent required to be preserved.
- `python3 -m unittest discover -s tests/tooling/integration_fast_coverage -p '*_test.py'` -- 6 passed.
- `git diff --check` -- clean.

Residual risks:
- The full integration-fast lane was not run locally; only the focused class was executed. The exact race method
  remains pinned in `tools/integration-fast-required-surfaces.txt`, so CI must execute and pass it.
- The race proof stops at the `IngestDedupReservation` class boundary. The caller-visible half of MEM-4 — two
  concurrent `POST /api/v1/ingest` calls yielding one scheduled workflow and the loser receiving the winner's
  instance id — is untested at the endpoint and is recorded as the deferred item in this spec's frontmatter.
- `Barrier.SignalAndWait` blocks thread-pool threads from inside `Task.Run`, as the intent's constraints require.
  The 30s per-round bound absorbs normal CI pool pressure, but severe starvation would surface as a timeout.
- The deferred-work ledger was not edited by this run; the orchestrator owns DW-18's status and resolution.
