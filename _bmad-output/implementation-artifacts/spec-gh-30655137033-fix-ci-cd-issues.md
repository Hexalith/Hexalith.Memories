---
title: 'Stabilize integration-fast readiness and lane ownership'
type: 'bugfix'
created: '2026-07-31'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'fd78b0211e8dd42127890e34d4268c783e0e7d83'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/29-1-openbao-backed-apphost-secret-topology.md'
  - '{project-root}/tests/README.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions run `30655137033` fails `integration-fast` because fixture startup never proves the permitted Access Telemetry Clock and Lifecycle Dapr secret components are responsive before their first exact authorization-matrix reads, and because an environment-sensitive 60-second cold-start assertion is mixed into the uncontrolled functional fast lane. The same unchanged code has produced 9–10 second passes and 132–192 second failures on `ubuntu-latest`; verification exposed the equivalent Lifecycle readiness race and a repeatable broad-suite MCP search HTTP 500 that passes in isolation but fails after the preceding fast-lane sequence.

**Approach:** Add bounded, secret-safe readiness polling for the permitted Clock and Lifecycle sidecar reads before releasing the shared fixture, separate disclosure, restart, and performance responsibilities, and diagnose and remove the MCP search test's broad-suite sequence/resource dependency without weakening its end-to-end assertion. Keep disclosure, authorization, and MCP search blocking-fast while full restart and NFR timing run in their declared slow/performance lanes. Preserve the 60-second NFR7 target.

## Boundaries & Constraints

**Always:** Keep the existing 30-second per-request ceiling, 5-minute readiness budget, exact fingerprint comparison, exact allow/deny status assertions, MCP end-to-end search assertion, and 60-second NFR7 threshold. Keep sensitive-disclosure and MCP search verification in `integration-fast`; ensure the fast-lane evidence verifier requires the OpenBao topology class.

**Ask First:** Changing workflow schedules, runner labels, job time budgets, OpenBao/Dapr/MCP production configuration, authorization outcomes, MCP behavior contracts, or the 60-second NFR7 threshold.

**Never:** Fix the race by only increasing `HttpClient.Timeout`; retry an unexpected authorization result or MCP test failure into success; log or persist secret values; skip, weaken, or delete OpenBao or MCP evidence; touch dependency/submodule pointers or Story 31.1 production-platform records.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Slow permitted component | Clock or Lifecycle sidecar endpoint accepts connections late | Startup polls until each permitted secret endpoint returns HTTP 200, then the matrix verifies its fingerprint | Exhaustion fails with endpoint-safe status/error and recent redacted logs |
| Wrong boundary | Wrong value or forbidden/unauthorized component access | Existing fingerprint and exact-status assertions fail closed | Never reinterpret an unexpected result as transient success |
| Fast lane | Functional filter excludes slow/performance traits | Authorization, disclosure, and other functional OpenBao checks execute and pass | Required-surface verification fails if the class disappears |
| Slow/performance lane | Full restart or cold-start NFR selector | Restart recovery executes as slow; cold start remains `<60s` as performance evidence | A genuine NFR breach remains a failure in that lane |
| Broad-suite MCP search | MCP search test runs after preceding fast-lane fixtures/resources | End-to-end search crosses the Dapr hop and returns the expected non-error result | A real service error remains visible; fixture state/resources are isolated or made ready rather than masked by retrying the assertion |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- shared topology startup, named sidecar clients, endpoint polling, and redacted diagnostics.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs` -- exact Dapr matrix, disclosure, restart, and NFR7 assertions.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs` -- focused regression guards for lane traits/readiness behavior.
- `tests/Hexalith.Memories.IntegrationTests/McpServerIntegrationTests.cs` -- MCP end-to-end search assertion that passes alone but fails in broad-lane sequence.
- `tools/integration-fast-required-surfaces.txt` -- required executed-class inventory consumed after the blocking integration run.

## Tasks & Acceptance

**Execution:**
- [x] `AspireIngestionPipelineFixture.cs` -- reuse the bounded endpoint-polling pattern to prove the Clock sidecar's permitted secret route is ready before fixture initialization completes, without reading or emitting the response body.
- [x] `AspireIngestionPipelineFixture.cs` -- apply the same bounded, fail-closed, body-free readiness gate to the Lifecycle sidecar's permitted secret route.
- [x] `OpenBaoTopologyIntegrationTests.cs` -- split disclosure from cold-start timing; retain disclosure as fast, mark full topology restart `IntegrationSlow`, and mark NFR7 with the repository-standard slow/performance traits while keeping its threshold and environment measurement.
- [x] `AspireIngestionPipelineFixtureTests.cs` -- guard the intended fast/slow/performance classification and any extracted retry decision seam, including cancellation and fail-closed behavior.
- [x] `tools/integration-fast-required-surfaces.txt` -- require the OpenBao topology class in successful fast-lane TRX evidence.
- [x] Diagnose and fix the repeatable broad-suite sequence/resource dependency behind `McpServerIntegrationTests.CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop` without retrying or weakening the assertion.
- [x] Verification -- build Release, run focused fixture/topology tests, exercise slow/performance selectors, then run the exact CI fast filter and evidence verifier.

**Acceptance Criteria:**
- Given transient Clock or Lifecycle sidecar startup delay, when the shared topology initializes, then it converges within existing budgets and the exact permitted fingerprint assertions pass without exposing a value.
- Given non-convergence, cancellation, a wrong fingerprint, or an unexpected access status, when the probe/matrix runs, then it fails closed with actionable secret-safe diagnostics.
- Given the CI fast filter, when tests complete, then functional OpenBao authorization and disclosure tests execute successfully, full restart/NFR timing do not execute, and required-surface verification passes.
- Given slow and performance selectors, when OpenBao tests run, then full restart recovery and the unchanged `<60s` NFR7 assertion remain executable rather than skipped or deleted.
- Given the exact broad fast-lane sequence, when MCP search executes after preceding integration fixtures, then the end-to-end Dapr hop returns the expected non-error search result without relying on a test retry.

## Spec Change Log

- 2026-07-31 -- Human-approved scope amendment: extend the bounded permitted-sidecar readiness gate from Clock to Lifecycle after the exact fast-lane verification exposed the same startup race.
- 2026-07-31 -- Human-approved scope amendment: diagnose and fix the MCP search HTTP 500 that passes alone but reproduces in the broad fast-lane sequence.

## Design Notes

`WaitForEndpointAsync` already provides linked AppHost cancellation, per-probe timeout, polling, last safe status/error, and bounded recent logs. The Clock and Lifecycle readiness gates should reuse that behavior. Splitting the combined disclosure/NFR method prevents variable runner timing from suppressing the security check.

The MCP failure must be diagnosed from the broad-lane resource/fixture sequence. An isolated pass is supporting evidence, not acceptance evidence; the exact fast filter must complete successfully without retrying the failing assertion.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Fixtures.AspireIngestionPipelineFixtureTests -parallel none -noLogo` -- expected: all focused guards pass.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Fixtures.OpenBaoTopologyIntegrationTests -parallel none -noLogo` -- expected: all functional, restart, disclosure, and NFR checks pass on an NFR-capable host.
- `bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" --configuration Release --no-build --results-directory TestResults/integration-fast && python3 tools/verify-integration-fast-coverage.py --results-directory TestResults/integration-fast` -- expected: fast lane passes and reports the OpenBao surface.
- `git diff --check` -- expected: no whitespace errors.

**Executed 2026-07-31:**
- PASS -- focused Release build completed with 0 warnings and 0 errors.
- PASS -- `AspireIngestionPipelineFixtureTests` completed 9/9, covering transient convergence, 401/403 fail-closed behavior, cancellation, exhaustion diagnostics, and lane traits.
- PASS -- a fresh final-binary run of `DaprSidecarMatrix_EnforcesExactComponentAndKeyAllowDenyBoundaries` completed 1/1 in 13.292 seconds after the Clock readiness gate converged.
- PARTIAL -- the unfiltered live OpenBao class completed 6/7 in 241.468 seconds; its existing in-place replacement test failed when Aspire canceled the Clock sidecar restart command and the AppHost tore down fail closed.
- PARTIAL -- the exact CI fast filter completed 260 passed, 1 failed, and 8 skipped in 19m25s. The sole failure was the lifecycle sidecar's permitted marker read timing out at the unchanged 30-second request ceiling; the Clock readiness probe had converged.
- PASS -- the fast-lane TRX verifier required and found `OpenBaoTopologyIntegrationTests`; TRX inspection confirmed disclosure/authorization methods executed while full restart and NFR7 were excluded.
- PASS -- `--list-tests` with `Category=IntegrationSlow` selected full restart and NFR7, while `Category=Performance` selected NFR7.
- PASS -- `git diff --check` reported no whitespace errors.

**Executed after the 2026-07-31 Lifecycle scope amendment:**
- PASS -- the focused Release build completed with 0 warnings and 0 errors in 14.50 seconds.
- PASS -- `AspireIngestionPipelineFixtureTests` completed 9/9, and a fresh final-binary Dapr matrix completed 1/1 in 16.543 seconds after both permitted-sidecar readiness gates converged.
- PARTIAL -- the exact CI fast filter completed 259 passed, 2 failed, and 8 skipped in 19m02s. All five fast OpenBao topology methods passed, including the exact Dapr matrix, disclosure scan, and in-place recovery. The two failures were outside the amended OpenBao scope: `McpServerIntegrationTests.CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop` returned HTTP 500, and `TenantDeletionIntegrationTests.DeleteTenant_ConcurrentRequests_ShouldReuseSingleWorkflowInstance` observed distinct workflow IDs.
- PASS -- the fast-lane TRX verifier found every required surface, including `OpenBaoTopologyIntegrationTests`; TRX inspection confirmed full restart and NFR7 remained excluded.
- PASS -- fresh slow/performance discovery selected full restart plus NFR7 for `Category=IntegrationSlow` and NFR7 for `Category=Performance`.
- PASS -- `git diff --check` reported no whitespace errors.

**Executed before the 2026-07-31 MCP scope amendment:**
- PASS -- isolated reruns completed 1/1 for MCP search in 140.240 seconds and 1/1 for concurrent tenant deletion in 16.312 seconds.
- FAIL -- a second exact fast-lane run reproduced the MCP search HTTP 500 after about 6m30s, confirming a broad-suite sequence/resource interaction despite the isolated pass; the run was stopped before TRX finalization.

**Executed after the 2026-07-31 MCP scope amendment:**
- PASS -- failure-only resource diagnostics reproduced the pre-call / in-place OpenBao rotation / post-call sequence and isolated the HTTP 500 before the Server application: the MCP Dapr sidecar logged `DaprBuiltInServiceRetries`, while no Server request was observed.
- PASS -- fixture recovery now requires both a successful raw MCP-sidecar search and the MCP application's real `/ready` upstream check after OpenBao rotation; the MCP search assertion remains single-shot and unchanged.
- PASS -- the deterministic three-test sequence completed 3/3, including an authenticated MCP search before rotation and the original MCP end-to-end search immediately after rotation.
- PASS -- the focused Release IntegrationTests build completed with 0 warnings and 0 errors.
- PARTIAL -- the first exact CI fast-filter verification completed 260 passed, 1 failed, and 8 skipped in 20m29s. OpenBao in-place rotation passed in 1m22s and the immediately following MCP search passed in 0.568 seconds; the sole failure was an unrelated per-test Ollama fixture `/alive` startup timeout before its test body ran.
- PASS -- the timed-out Ollama-provider test completed 1/1 in 2m32s when rerun unchanged.
- PASS -- the literal required fast command completed 261 passed, 0 failed, and 8 skipped in 15m38s, and `verify-integration-fast-coverage.py` found every required surface including OpenBao topology and MCP integration.
- PASS -- `git diff --check` reported no whitespace errors.

**Executed after the 2026-08-01 review hardening:**
- PASS -- the exact focused Release IntegrationTests build completed with 0 warnings and 0 errors in 6.79 seconds.
- PASS -- `AspireIngestionPipelineFixtureTests` completed 37/37 in 0.336 seconds, covering the overall deadline against a non-cooperative hanging probe, per-probe timeout recovery, unexpected-exception propagation, status-set overlap rejection, active caller cancellation, secret/MCP status classification, diagnostic redaction, resource-category shapes, and runnable Fact metadata.
- PASS -- integration-fast selector discovery included all 37 Docker-free fixture guards, the five intended fast OpenBao topology methods, and both MCP server methods while retaining the slow/performance exclusions.
- PASS -- the integration-fast verifier's six synthetic Python fixtures accepted mapped passed results and rejected discovered-only, skipped-only, wrong-class, and wrong-method evidence; repository line-ending fixtures completed 4/4.
- PASS -- the literal required fast command completed 298 passed, 0 failed, and 8 skipped in 12m05s. The fresh TRX verifier required `outcome=Passed` and found every class surface plus the exact OpenBao authorization matrix, exact disclosure scan, and exact MCP search method.
- PASS -- `git diff --check` reported no whitespace errors.

## File Scope

Allowed files for this story:

- `.github/workflows/ci.yml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-gh-30655137033-fix-ci-cd-issues.md`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Mcp/McpServerIntegrationTests.cs`
- `tests/tooling/integration_fast_coverage/integration_fast_coverage_test.py`
- `tools/integration-fast-required-surfaces.txt`
- `tools/verify-integration-fast-coverage.py`

## Suggested Review Order

**Recovery boundaries**

- Start with the health-gated OpenBao rotation and MCP recovery contract.
  [`AspireIngestionPipelineFixture.cs:539`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L539)

- Inspect fail-closed MCP status classification and safe recovery diagnostics.
  [`AspireIngestionPipelineFixture.cs:637`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L637)

- Verify Clock and Lifecycle readiness gates preserve existing budgets.
  [`AspireIngestionPipelineFixture.cs:1788`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L1788)

- Review strict overall deadline and narrow transient retry handling.
  [`AspireIngestionPipelineFixture.cs:2367`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L2367)

**Lane ownership and evidence**

- Confirm disclosure remains fast while restart and NFR evidence move slow.
  [`OpenBaoTopologyIntegrationTests.cs:131`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs#L131)

- Require passed TRX results instead of discovered class definitions.
  [`verify-integration-fast-coverage.py:49`](../../tools/verify-integration-fast-coverage.py#L49)

- Pin exact OpenBao authorization, disclosure, and MCP search methods.
  [`integration-fast-required-surfaces.txt:7`](../../tools/integration-fast-required-surfaces.txt#L7)

- Run verifier regression fixtures in the blocking Docker-free CI job.
  [`ci.yml:266`](../../.github/workflows/ci.yml#L266)

**Regression and diagnostics**

- Exercise deadlines, fail-closed statuses, redaction, and runnable traits.
  [`AspireIngestionPipelineFixtureTests.cs:162`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs#L162)

- Capture secret-safe MCP diagnostics for result and transport failures.
  [`McpServerIntegrationTests.cs:87`](../../tests/Hexalith.Memories.IntegrationTests/Mcp/McpServerIntegrationTests.cs#L87)

- Reject discovered-only, skipped-only, and wrong-method TRX evidence.
  [`integration_fast_coverage_test.py:16`](../../tests/tooling/integration_fast_coverage/integration_fast_coverage_test.py#L16)
