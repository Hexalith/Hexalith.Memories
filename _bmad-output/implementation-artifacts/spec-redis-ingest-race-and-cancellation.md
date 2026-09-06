---
title: 'Redis ingest endpoint race and cancellation hardening'
type: 'bugfix'
created: '2026-09-06'
status: 'in-review'
baseline_revision: '6592c505136c43c6bf1fbed6ecdb8ca7c2895919'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/project-context.md'
warnings:
  - 'oversized'
deferred: []
---

<intent-contract>

## Intent

**Problem:** DW-722 leaves the REST duplicate-in-flight branch unprotected, so it could return the losing candidate or schedule twice. DW-723 exposes cancellation parameters that do not stop Redis waits, and the REST endpoint does not pass request cancellation into the reservation call.

**Approach:** Add deterministic endpoint coverage for winner forwarding and no scheduling, propagate request cancellation to the reservation, and make every reservation Redis await cancellable. If cancellation wins a race with `SET NX`, observe the still-running command and atomically remove only a late reservation still owned by that candidate.

## Boundaries & Constraints

**Always:** Preserve `Reserved`, `DuplicateInFlight`, `FailOpen`, TTL, key construction, and non-cancellation Redis-failure behavior; use the repository-standard `Task.WaitAsync(cancellationToken)` because StackExchange.Redis 3.1.31 has no token overloads; pre-check cancellation before dispatch; rethrow caller cancellation; observe every detached late-SET cleanup failure; use an atomic value-checked delete so cleanup cannot delete a successor; pass `HttpContext.RequestAborted` to the REST reservation call; retain cancellation-independent endpoint compensation after an owned reservation; keep existing cross-tenant denial and Redis race evidence green.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md` or `.bmad-loop` ledger material; schedule on `DuplicateInFlight`; convert cancellation into `FailOpen`; delete a reservation without checking ownership during late-SET cleanup; cancel or change workflow scheduling semantics; change keys, TTLs, packages, public contracts, topology, CI lanes, or required-surface manifests; add sleeps, conditional skips, or Docker to Server.Tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Duplicate REST ingest | `SET NX` loses and Redis returns a winner ID | `202`, winner ID in body and `Location`, zero scheduler calls | No error expected |
| Request already canceled | Token canceled before reserve dispatch | No Redis command and no scheduling | Propagate caller cancellation |
| Cancellation during `SET NX` | Redis task remains pending | Caller stops waiting; late success triggers owner-checked cleanup | Cleanup failures are observed/logged; TTL remains backstop |
| Cancellation during winner lookup | Lost `SET NX`, pending `GET` | Caller stops waiting | Propagate caller cancellation |
| Cancellation during release | Pending `DEL` | Caller stops waiting | Propagate caller cancellation; ordinary Redis failures remain swallowed/logged |
| Late reservation replaced | Key value changes before late cleanup | Replacement survives | Atomic comparison returns without deletion |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs` -- production `SET NX`, loser `GET`, and release `DEL`; add cancellation waits plus observed owner-checked late-SET cleanup. Existing Redis/timeout catches are the fail-open/release-warning contracts.
- `src/Hexalith.Memories.Server/Import/RedisImportStagingStore.cs:402` -- read-only precedent for atomic Lua `GET`/`DEL` ownership checks.
- `src/Hexalith.Memories.Server/DerivedStores/RedisDerivedStoreService.cs:53` -- read-only repository precedent for cancellation pre-checks and Redis-task `WaitAsync`.
- `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:116` -- REST preflight branch; pass `RequestAborted` only to reservation while keeping scheduling and owned-reservation compensation semantics unchanged.
- `tests/Hexalith.Memories.Server.Tests/Endpoints/IngestionEndpointE2ETests.cs` -- existing authorized `WebApplicationFactory` path; its Redis and scheduler substitutes can prove winner response/no second schedule and aborted-request behavior.
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs:58` -- read-only test seam exposing the keyed production Redis substitute and configuration overrides.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` -- deterministic coverage for pending `SET`, `GET`, `DEL`, pre-cancellation, exact cleanup ownership arguments, and preserved fail-open behavior.
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestDedupReservationIntegrationTests.cs` -- read-only real-Redis race and release-scope authority; must remain green.
- `tools/integration-fast-required-surfaces.txt:11` -- read-only existing pins for both Redis integration methods; no manifest change is required.
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` -- read-only attached negative evidence that tenant denial precedes Redis/scheduling dependencies.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs` -- honor cancellation at all three Redis waits and safely observe/compensate a canceled late-successful `SET NX` without deleting another owner.
- [ ] `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs` -- flow the HTTP request-aborted token into `TryReserveAsync`, leaving durable scheduling and compensation tokens unchanged.
- [ ] `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` -- cover pre-cancellation, each pending Redis operation, late-success ownership cleanup, successor preservation contract, and unchanged non-cancellation failures.
- [ ] `tests/Hexalith.Memories.Server.Tests/Endpoints/IngestionEndpointE2ETests.cs` -- cover exact duplicate winner forwarding/no scheduling and cancellation of a request blocked on reservation.

**Acceptance Criteria:**
- Given a valid authorized ingest whose preflight reservation returns `DuplicateInFlight`, when `/api/v1/ingest` handles it, then the response body and `Location` contain the Redis winner ID and the workflow scheduler is never called.
- Given an ingest request canceled while its Redis reservation is pending, when cancellation is observed, then the HTTP operation cancels without scheduling; any late successful reservation is deleted only if its value still equals that request's candidate ID.
- Given cancellation before dispatch or while `SET`, winner `GET`, or release `DEL` is pending, when the public reservation method observes it, then it throws caller cancellation promptly rather than returning `FailOpen` or waiting for Redis.
- Given existing Redis exceptions/timeouts, real-Redis race/release-scope tests, and cross-tenant endpoint denials, when focused regression suites run, then fail-open, compensation, atomic winner linkage, exact-key isolation, and denial-before-dependency behavior remain unchanged.

## Spec Change Log

## Review Triage Log

## Design Notes

StackExchange.Redis 3.1.31 exposes no cancellation-token overload for these commands. `WaitAsync` cancels only the caller's wait, so `SET NX` may still succeed afterward. The cancellation path must retain the original task, return cancellation immediately, then run an exception-observing helper that awaits the command and executes a single atomic `GET == candidate` / `DEL` script only on late success. Direct `DEL` is unsafe because a TTL expiry and new reservation could transfer ownership before cleanup.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release -m:1` -- expected: zero errors and warnings.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Ingestion.IngestDedupReservationTests -class Hexalith.Memories.Server.Tests.Endpoints.IngestionEndpointE2ETests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -parallel none -noLogo` -- expected: all focused cancellation, endpoint, and tenant-denial tests pass with none skipped.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release -m:1` -- expected: zero errors and warnings.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Ingestion.IngestDedupReservationIntegrationTests -parallel none -noLogo` -- expected: existing Docker-backed race and release-scope proofs pass with none skipped.
- `dotnet build Hexalith.Memories.slnx --configuration Release -m:1` -- expected: full solution build succeeds.
