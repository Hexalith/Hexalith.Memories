# Test Automation Summary — Story 18.8

**Feature:** Cross-Module Dapr Event Intake Contract and Verification
**Story:** `18-8-cross-module-dapr-event-intake-contract-and-verification`
**Workflow:** `bmad-qa-generate-e2e-tests` (gap-fill mode — story already implemented at status `review`)
**Date:** 2026-06-25
**Framework detected:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + NSubstitute (5.3.0). Matched the project's
existing test stack; no new framework introduced.
**Run command (sandbox):** built the test project, then ran the xUnit v3 assembly directly with
`DiffEngine_Disabled=true dotnet exec <test.dll> -class <FQN>` (`dotnet test`/VSTest socket is blocked here —
`SocketException 13`, per the story's Dev Agent Record).

## Scope

Story 18.8 is a **contract + verification hardening** story over an event-intake path that already exists (no
new public API, no UI). The story landed at status `review` with strong **unit-level** and **doc-drift**
coverage: `MiddlewareOrderTests` (sidecar `/dapr/subscribe` discovery + `/process` absence), `DocumentationCompletenessTests`,
`RouteSurfaceContractTests`, `DeploymentConfigurationContractTests`, `TenantEventRouterTests`, and
`EventIngestionServiceTests` / `EventIngestionControllerTests`.

AC7 asks for evidence that proves three things: **(a)** sidecar subscription discovery, **(b)** source-prefix
routing for **at least two synthetic Hexalith modules**, and **(c)** duplicate-safe delivery. This QA pass
scanned each AC's proof against the **real HTTP pipeline** (middleware order + CloudEvents normalization +
controller outcome→HTTP mapping) and found that only **(a)** had end-to-end (Tier-2 in-process HTTP) coverage.
Items **(b)**, **(c)**, and the AC5 unknown-source non-retry drop were proven **only at the unit level** and
never driven through `/events/ingest` itself. All three gaps were **auto-applied** as a new E2E test class.

## Gaps Discovered and Applied

| # | AC | Untested behavior at the HTTP surface | Where it was previously only unit-proven | Test added |
| - | -- | ------------------------------------- | ---------------------------------------- | ---------- |
| 1 | AC2 + AC7 | Two synthetic Hexalith modules (`hexalith/tenants`, `hexalith/parties`) publishing to the **same** topic/endpoint are accepted and routed to **distinct** tenants through the real `/events/ingest` pipeline. The router unit test proves prefix matching; nothing proved the endpoint accepts and differentiates both module streams end-to-end. | `TenantEventRouterTests.ResolveAsync_TwoHexalithModulePrefixes_RouteToConfiguredTenants` (router only) | `TwoHexalithModulePrefixes_PublishedToSharedTopic_AreAcceptedAndRoutedDistinctly` |
| 2 | AC5 | An unknown source prefix delivered to `/events/ingest` **drops** with HTTP 200 (so DAPR does NOT redeliver) — not 500 — and returns the diagnosable `unknown-source` status with no workflow scheduled. The controller theory test mocks the service; nothing drove a real unknown-source event through the live service. | `EventIngestionServiceTests.ProcessAsync_UnknownSource_…` + `EventIngestionControllerTests` (mocked service) | `UnknownSourcePrefix_DropsWithoutRetry_AndReturnsDiagnosableStatus` |
| 3 | AC4 + AC7 | The **same** CloudEvent delivered twice (at-least-once pub/sub) is absorbed by preflight dedup — second delivery returns `duplicate` with `wasDuplicate=true` and **no second workflow** is scheduled — through the real HTTP pipeline. | `EventIngestionServiceTests.ProcessAsync_PreflightReservation_ReturnsDuplicate_…` (service only) | `DuplicateDelivery_ToSharedTopic_IsIdempotent_SecondDeliveryReturnsDuplicateWithoutRescheduling` |

The unknown-source E2E test additionally fires the real **EventId 9110** "no tenant mapping for source …"
warning end-to-end (observed in the test log), confirming AC5's *diagnosable* requirement at the live service
boundary rather than via a mock.

## Generated Tests

### E2E (Tier-2 in-process HTTP) — `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs` (NEW, 3 `[Fact]`)

- [x] `TwoHexalithModulePrefixes_PublishedToSharedTopic_AreAcceptedAndRoutedDistinctly` — posts a CloudEvent
  (`application/cloudevents+json`, the shape DAPR pub/sub delivers) for `hexalith/tenants/events` and for
  `hexalith/parties/events` to `/events/ingest`; asserts both 200 `accepted`, both with non-empty workflow
  instance ids, the two instance ids **differ** (distinct routing — the shared topic did not collapse the two
  streams), and the scheduler received exactly **2** schedule calls.
- [x] `UnknownSourcePrefix_DropsWithoutRetry_AndReturnsDiagnosableStatus` — posts an event with an unmapped
  source; asserts HTTP **200 (not 500)** so DAPR drops rather than retries, body `status = unknown-source`,
  `instanceId = null`, `wasDuplicate = false`, and the scheduler was **never** invoked.
- [x] `DuplicateDelivery_ToSharedTopic_IsIdempotent_SecondDeliveryReturnsDuplicateWithoutRescheduling` — posts
  the identical envelope twice with preflight dedup returning `Reserved` then `Duplicate`; asserts first → 200
  `accepted` + instance id, second → 200 `duplicate` + `wasDuplicate=true` + null instance id, and the
  scheduler scheduled **exactly once** (one memory unit per logical event).

Reuses the existing `EventStoreWebAppFactory` Tier-2 harness (boots the Server with the EventStore pipeline
wired in, every external adapter — router, scheduler, preflight dedup, Dapr client — replaced by NSubstitute
fakes; no Redis / FalkorDB / DAPR sidecar required), matching `MiddlewareOrderTests`.

### Reviewed, no gap (left unchanged)

- [x] `MiddlewareOrderTests` — `/dapr/subscribe` exposes `pubsubname=pubsub` / topic `memories-events` / route
  `events/ingest` (AC1); `UseCloudEvents()` no-op for plain JSON; `/process` → 404 (AC3); malformed structured
  CloudEvent → 400. Covered.
- [x] `DocumentationCompletenessTests`, `RouteSurfaceContractTests`, `DeploymentConfigurationContractTests` —
  doc/code drift guards for the cross-module contract literals, `/process` refutation, and deploy literals
  (AC1/AC3/AC6). Covered.
- [x] `TenantEventRouterTests` — case-insensitive longest-prefix routing for two Hexalith module prefixes,
  unknown-source, provisioning/deleting/auto-create-disabled/case-cap (AC2/AC5). Covered at the unit level.
- [x] `EventIngestionServiceTests` / `EventIngestionControllerTests` — preflight duplicate, schedule-failure
  release, unknown-source drop, outcome→HTTP mapping (AC4/AC5). Covered at the unit level.
- [x] `EventIngestionPipelineIntegrationTests.PublishViaDaprPubSub_…` — optional slow Dapr/Aspire smoke.
  **NOT RUN** — this sandbox has no Dapr sidecar/Docker/Aspire (the in-process tests log `Connection refused`
  to the sidecar, confirming none is present). AC4's duplicate-safety is now covered in the default lane by
  gap #3 (E2E) plus the existing service-level units; the slow path runs unchanged where infra is available.

## Coverage by Acceptance Criterion

| AC | Description | Status |
| -- | ----------- | ------ |
| AC1 | Sidecar subscription discovery (`/dapr/subscribe` → pubsub/topic/`events/ingest`) | Covered (`MiddlewareOrderTests`, unchanged) |
| AC2 | Two module source prefixes route through the shared topic | Covered (**+1** E2E: both prefixes accepted + routed distinctly through `/events/ingest`) |
| AC3 | ACL operation surface rejects `/process` | Covered (`RouteSurfaceContractTests` + `MiddlewareOrderTests`, unchanged) |
| AC4 | Duplicate Dapr deliveries are idempotent | Covered (**+1** E2E: duplicate delivery returns `duplicate`, one workflow scheduled) |
| AC5 | Unknown source drops without retry and is diagnosable | Covered (**+1** E2E: 200 drop + `unknown-source` status + EventId 9110 fired live) |
| AC6 | One-topic limitation + workaround published | Covered (`DocumentationCompletenessTests`, unchanged) |
| AC7 | Focused validation evidence: discovery + 2-module routing + duplicate-safe | Covered (**+3** E2E close the routing & duplicate-safe halves at the HTTP surface) |

## Results

| Test run | Build | Result |
| -------- | ----- | ------ |
| `CrossModuleEventIntakeE2ETests` (NEW) | 0 warnings | **3 passed, 0 failed, 0 skipped** |
| Focused contract set (Middleware + Documentation + CrossModuleE2E + RouteSurface + DeploymentConfiguration) | 0 warnings | **25 passed, 0 failed, 0 skipped** (was 22; **+3**) |
| **Full `Hexalith.Memories.Server.Tests` assembly** | 0 warnings | **1945 passed, 0 failed, 1 skipped** (was 1942; **+3**; pre-existing unrelated skip) |
| **Full `Hexalith.Memories.EventStore.Tests` assembly** | 0 warnings | **94 passed, 0 failed, 0 skipped** (unaffected) |

3 new `[Fact]` tests added; the project builds clean under the warnings-as-errors gate; full regression green.
**No production source changed** — the gaps were proof gaps (unit-only behavior not yet driven through the live
HTTP surface), not behavior gaps. New test code normalized to CRLF per `.editorconfig`. The published contract
docs are unchanged (the new tests prove existing contract behavior end-to-end, they do not add new claims).

## Next Steps

- The optional Dapr/Aspire pub/sub smoke (`EventIngestionPipelineIntegrationTests.PublishViaDaprPubSub_…`)
  remains the authoritative proof of the **actual** sidecar publish path and should be run in a CI/local lane
  that has Docker + Dapr available; it is environment-skipped here and was not re-run.
- No further default-lane gaps identified — sidecar discovery, two-module shared-topic routing, the
  unknown-source non-retry drop, and duplicate-safe delivery are now each proven by a runnable test at the
  **HTTP-pipeline** level in addition to the existing unit and doc-drift guards.
