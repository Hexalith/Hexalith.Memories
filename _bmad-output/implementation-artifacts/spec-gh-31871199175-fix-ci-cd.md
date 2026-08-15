---
title: 'Stop the second Access Telemetry AppHost from failing required CI'
type: 'bugfix'
created: '2026-08-15'
status: 'done'
baseline_commit: '7c6840f098885f2811e07dbbad33b8960412edeb'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-gh-31522252358-reduce-flaky-ci-checks.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-gh-30838751196-fix-ci-cd-issue.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Required `integration-fast` on run `31871199175` failed one test: `AccessTelemetryAspireRoutedIntegrationTests.AppHost_DaprRoutesClockHeartbeatActorStateInspectionAndHealth`. It boots a second full AppHost and waits on Aspire health for `memories-access-telemetry-clock-dapr-cli`, which `FailedToStart` at 49s. The rest of that job passed (320/321). This is the overengineered surface that keeps CI red — not missing workflow jobs.

**Approach:** Keep the unique live Dapr attest/heartbeat/inspect/actor assertions. Join the existing `AspireIngestionPipeline` topology, wait with the proven log-based daprd path, and delete the second AppHost plus Aspire `*-dapr-cli` health waits. Do not rewrite `ci.yml` or adopt `domain-ci.yml` in this story.

## Boundaries & Constraints

**Always:** Keep required check names `build`, `test-unit-contract`, and `integration-fast`; keep the exact selector `Category=Integration&Category!=IntegrationSlow&Category!=Performance`; keep fail-closed OpenBao matrix evidence; keep the unique Story 27.2 live route assertions (attest, `AllowsWrites`, heartbeat accepted, inspect Healthy/None/epoch, actor `lifecycle-control` writers + epoch/hash).

**Ask First:** Merging or renaming CI jobs, changing branch protection, adopting Hexalith.Builds `domain-ci.yml`, dropping `production-deployment-verification` from PR CI, moving this test to `IntegrationSlow`, or changing timeouts/secret statuses.

**Never:** Hide the failure with retries, `|| true`, or longer waits; disable or skip the unique route proof; wait on Aspire `WaitForResourceHealthyAsync` for `*-dapr-cli`; start a second AppHost in the fast lane; mutate GitHub repository settings; update submodules or Hexalith.Builds.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Shared topology running | `AspireIngestionPipeline` already started | Test uses fixture sidecar clients; no `CreateAsync<Projects.Hexalith_Memories_AppHost>` | N/A |
| Auxiliary sidecars not HTTP-ready | Clock/lifecycle daprd not yet in logs | `WaitForOpenBaoSidecarMatrixReadinessAsync` then invoke | Fail closed; no Aspire dapr-cli health wait |
| Unique route proof | Direct daprd invoke on clock + lifecycle | Attest, validate writes, heartbeat accepted, inspect Healthy, actor writers non-empty | HTTP/assertion failures stay red |
| Source regression | Routed test source | No second AppHost and no `WaitForResourceHealthyAsync` on `*-dapr-cli` | Guard fails if those calls return |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryAspireRoutedIntegrationTests.cs` -- constructor injects `AspireIngestionPipelineFixture`; `[Collection("AspireIngestionPipeline")]`; 3-minute linked CTS; `WaitForOpenBaoSidecarMatrixReadinessAsync` then shared `CreateDaprSidecarClient`; unique attest/heartbeat/inspect/actor assertions unchanged.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- localized clock/lifecycle readiness; internal named `CreateDaprSidecarClient` resolves log-based daprd loopback after matrix readiness; shared startup waits only `memories` healthy, not dapr-cli.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs` -- L15-19 / L62-65 pattern: inject fixture, call matrix readiness, do not boot AppHost.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs` -- Docker-free client-factory contracts; source guard bans `DistributedApplicationTestingBuilder`, `CreateAsync`, `WaitForResourceHealthyAsync`, and `WaitForHealthyAsync`, and asserts no `AccessTelemetryAspireRoutedCollection` type.
- `src/Hexalith.Memories.AppHost/Program.cs` -- L380-408 fixed clock `3800/50301` and lifecycle `3700/50201` ports (read-only: collision evidence).
- `.github/workflows/ci.yml` -- L382-442 required fast lane (read-only this story). Run `31871199175` job `integration-fast` failed only the routed test.

## Tasks & Acceptance

**Execution:**
- [x] `AspireIngestionPipelineFixture.cs` -- make named sidecar client creation internal and callable after matrix readiness -- the routed test must use log-resolved daprd, not DCP/Aspire dapr-cli health.
- [x] `AccessTelemetryAspireRoutedIntegrationTests.cs` -- join `[Collection("AspireIngestionPipeline")]`, inject `AspireIngestionPipelineFixture`, delete `AccessTelemetryAspireRoutedCollection` and the standalone AppHost/`WaitForHealthyAsync` path, keep unique invoke assertions -- remove the topology that failed CI.
- [x] `AspireIngestionPipelineFixtureTests.cs` -- cover the exposed client factory and a source guard that the routed test neither calls `DistributedApplicationTestingBuilder.CreateAsync` nor `WaitForResourceHealthyAsync` -- lock the simplification.

**Acceptance Criteria:**
- Given the shared fixture topology is running, when the routed Access Telemetry test runs, then it does not start a second AppHost and still proves attest, write validation, heartbeat, inspect, and actor state.
- Given clock/lifecycle sidecars are not yet HTTP-ready, when that test prepares clients, then it uses the existing localized daprd wait and fails closed without Aspire `*-dapr-cli` health.
- Given a future edit reintroduces `CreateAsync` or `WaitForResourceHealthyAsync` in the routed test, when Docker-free fixture tests run, then the source guard fails.
- Given the exact fast selector, when the lane can run, then the routed test is not a second-topology flake and required job names stay unchanged.

## Spec Change Log

- 2026-08-15: Joined the routed Access Telemetry test to the shared `AspireIngestionPipeline` topology, exposed the named daprd client factory, and locked the second-AppHost regression with Docker-free source guards.

## Design Notes

Run `31871199175` was otherwise green: `build`, `test-unit-contract`, `web-e2e-specimen`, and `production-deployment-verification` passed. CI is heavy (four parallel restores, fifteen tooling fixture steps, kind on every PR, local `ci.yml` instead of `domain-ci.yml`), but those extras did not cause this red. D17 / Story 30.2 owns shared-workflow adoption; merging the required `build` job needs a GitHub settings change. This story only removes the duplicate AppHost that collided on fixed Dapr ports.

Reuse OpenBao's collection pattern. Do not move clock/lifecycle waits into shared fixture startup — that would block every consumer again.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: 0 warnings, 0 errors.
- `dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~AspireIngestionPipelineFixtureTests"` -- expected: all fixture contracts including the new source guard pass.
- `bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" --configuration Release --no-build --results-directory TestResults/integration-fast` then `python3 tools/verify-integration-fast-coverage.py --results-directory TestResults/integration-fast` -- expected: routed test passes without a second AppHost; verifier accepts. If Docker/Dapr is unavailable, record the exact blocker and keep the Docker-free evidence.
- `git diff --check` -- expected: no whitespace errors.

**Results (2026-08-15):**
- Release integration-project build -- passed with 0 warnings and 0 errors.
- `AspireIngestionPipelineFixtureTests` -- 55 passed, 0 failed, including the named sidecar client factory contracts and the routed-test source guard.
- `git diff --check` -- passed with no whitespace errors.
- Exact fast selector without this machine's Dapr port mapping -- 183 passed, 149 failed, 0 skipped in 26m15s. Shared `AspireIngestionPipelineFixture` initialization timed out on `/alive` because local placement/scheduler are published on `localhost:6050`/`localhost:6060`, while the spec command uses CI's `127.0.0.1:50005`/`50006`. The routed method failed in 1 ms with the collection-fixture cascade, not a second AppHost or `*-dapr-cli` health wait.
- Coverage verifier on that TRX -- rejected missing required surfaces that share the same fixture cascade.
- Focused live routed method with `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050` and `MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060` -- 1 passed, 0 failed in 251.810s against one shared AppHost; attest, write validation, heartbeat, inspect, and actor state succeeded.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- expose named sidecar client creation after localized matrix readiness.
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryAspireRoutedIntegrationTests.cs` -- join the shared collection and delete the second AppHost.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs` -- client-factory and second-AppHost source guards.
- `_bmad-output/implementation-artifacts/spec-gh-31871199175-fix-ci-cd.md` -- approved contract, evidence, and review trail.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- deferred older-spec standalone AppHost documentation drift.

## Suggested Review Order

**Shared topology instead of a second AppHost**

- Join the existing Aspire collection and drop the standalone topology.
  [`AccessTelemetryAspireRoutedIntegrationTests.cs:17`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryAspireRoutedIntegrationTests.cs#L17)

- Bound the live proof with a 3-minute linked cancellation token.
  [`AccessTelemetryAspireRoutedIntegrationTests.cs:30`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryAspireRoutedIntegrationTests.cs#L30)

- Wait on localized daprd readiness, not Aspire `*-dapr-cli` health.
  [`AccessTelemetryAspireRoutedIntegrationTests.cs:33`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryAspireRoutedIntegrationTests.cs#L33)

- Reuse the fixture's log-resolved sidecar clients for unique invoke proof.
  [`AccessTelemetryAspireRoutedIntegrationTests.cs:36`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryAspireRoutedIntegrationTests.cs#L36)

**Named sidecar factory**

- Expose internal log-based daprd clients after matrix readiness.
  [`AspireIngestionPipelineFixture.cs:1157`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L1157)

**Regression locks**

- Guard against a second AppHost, Aspire dapr-cli health, and the old collection.
  [`AspireIngestionPipelineFixtureTests.cs:343`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs#L343)

