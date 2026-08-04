---
title: 'Route fixture probes directly to named Dapr sidecars'
type: 'bugfix'
created: '2026-08-04'
status: 'done'
baseline_commit: 'a4697d96a73e23227c26baf69fa928e022fe1929'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-gh-30655137033-fix-ci-cd-issues.md'
  - '{project-root}/tests/README.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions run `30838751196` fails `integration-fast` because the shared fixture and standalone routed telemetry test send Clock/Lifecycle requests through Aspire/DCP proxy endpoints that hang or return an invalid response instead of reaching daprd. The shared five-minute gate exhausts with no status and fans out into 168 failures; the same signature recurs in every completed full CI run since the prior readiness remediation.

**Approach:** Generalize the fixture's proven log-based direct-daprd endpoint resolver from the Memories sidecar to any exact named sidecar, then use it for Clock and Lifecycle readiness, authorization-matrix reads, and the standalone routed telemetry test. Preserve the existing bounded, status-only, fail-closed OpenBao contract.

## Boundaries & Constraints

**Always:** Resolve only the exact requested `*-dapr-cli` resource and its latest valid positive daprd HTTP port; construct a loopback-only endpoint; keep the five-minute readiness budget, 30-second request ceiling, exact success/deny statuses, response-body avoidance, diagnostic redaction, caller/AppHost cancellation, fingerprint comparisons, and required fast-lane OpenBao evidence.

**Ask First:** Changing workflow triggers, runner images, time budgets, Dapr/OpenBao production configuration, secret names, authorization outcomes, component scopes, Vault policies, or application behavior.

**Never:** Fix this by increasing timeouts, weakening or retrying permanent authorization failures, widening `allowedSecrets` or OpenBao permissions, logging secret values/bodies, removing readiness or matrix evidence, modifying submodules, or absorbing unrelated Falkor/Ryuk, embedding-provider startup, or historical NFR failures.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Named sidecar ready | Exact resource log reports `HTTP server listening ... :<port>` | Probe uses `http://127.0.0.1:<port>` and receives the Dapr status | Never select the DCP proxy port |
| Multiple sidecars | Clock, Lifecycle, main, and similarly named categories are present | Resolver selects only the requested resource's latest valid port | Ignore partial-name and malformed matches |
| Port unavailable | Requested resource has no valid daprd port evidence | Initialization fails with an actionable, secret-safe resolution error | Do not silently enter the known hanging proxy path |
| Boundary denial | Direct sidecar returns a permanent 400/401/403/405/406/415 status | Existing readiness/matrix checks fail immediately | Do not retry denial into success |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- named sidecar client creation, direct daprd port parsing, Clock/Lifecycle readiness, and OpenBao matrix calls.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs` -- Docker-free resolver matching, malformed/missing evidence, and readiness fail-closed regression guards.
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryAspireRoutedIntegrationTests.cs` -- standalone AppHost log capture and direct Clock/Lifecycle daprd clients.

## File Scope

Allowed to modify:
- `_bmad-output/implementation-artifacts/spec-gh-30838751196-fix-ci-cd-issue.md`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryAspireRoutedIntegrationTests.cs`

## Tasks & Acceptance

**Execution:**
- [x] `AspireIngestionPipelineFixture.cs` -- parameterize direct daprd endpoint resolution by exact sidecar resource and route all named sidecar clients through it, while retaining main-sidecar behavior and bounded probes.
- [x] `AspireIngestionPipelineFixtureTests.cs` -- prove distinct resource/port selection, latest valid match, malformed and similarly named rejection, missing-evidence failure, and unchanged permanent-status handling.
- [x] `AccessTelemetryAspireRoutedIntegrationTests.cs` -- route standalone Clock and Lifecycle calls through their exact direct daprd loopback endpoints without changing assertions, topology, or budgets.
- [x] Verification -- build Release, run focused resolver/fixture tests, run the exact OpenBao authorization method, then run the literal CI fast lane and its required-surface verifier.

**Acceptance Criteria:**
- Given the CI topology exposes different DCP proxy and daprd ports, when Clock and Lifecycle probes run, then each uses its exact real daprd loopback port and converges within existing budgets.
- Given another sidecar has a similar category or a newer log entry, when resolving a named sidecar, then only the exact requested resource can supply the selected latest valid port.
- Given missing or malformed direct-port evidence, when client creation is attempted, then it fails safely without falling back to the known hanging proxy.
- Given the OpenBao matrix, when direct reads cross store/key boundaries, then permitted fingerprints still match and all existing denial statuses remain exact.
- Given the exact `integration-fast` selector, when the lane completes, then every required surface passes and the verifier accepts the TRX.

## Spec Change Log

- 2026-08-04 -- Human-approved scope amendment: absorb the standalone Access Telemetry Aspire routed test after the literal fast lane proved its independently owned DCP proxy path still keeps CI red.

## Design Notes

Run evidence distinguishes transport from authorization: the DCP proxy produced `Last status: n/a`, while the actual Clock daprd port returned HTTP 200 immediately. The existing main-sidecar resolver already parses daprd's `HTTP server listening on TCP address` line and forces IPv4 loopback. Reuse that contract with strict resource-category boundaries rather than changing topology, security policy, or timeout behavior.

The standalone `AccessTelemetryAspireRoutedIntegrationTests` initially remained deferred, then entered this spec through the 2026-08-04 human-approved scope amendment after full-lane verification.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Fixtures.AspireIngestionPipelineFixtureTests -parallel none -noLogo` -- expected: all focused guards pass.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Fixtures.OpenBaoTopologyIntegrationTests.DaprSidecarMatrix_EnforcesExactComponentAndKeyAllowDenyBoundaries -parallel none -noLogo` -- expected: permitted fingerprints and exact denials pass through direct sidecars.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryAspireRoutedIntegrationTests.AppHost_DaprRoutesClockHeartbeatActorStateInspectionAndHealth -parallel none -noLogo` -- expected: standalone routing, heartbeat, actor-state inspection, and health evidence pass through direct Clock/Lifecycle daprd endpoints.
- `bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" --configuration Release --no-build --results-directory TestResults/integration-fast && python3 tools/verify-integration-fast-coverage.py --results-directory TestResults/integration-fast` -- expected: fast lane and required-surface verification pass.
- `git diff --check` -- expected: no whitespace errors.

**Executed 2026-08-04:**
- PASS -- Release IntegrationTests build completed with 0 warnings and 0 errors.
- PASS -- `AspireIngestionPipelineFixtureTests` completed 44/44 after review hardening, covering exact distinct-resource resolution, latest valid port selection, delayed/malformed/similar/missing evidence, bounded fail-closed resolution, all six permanent denial statuses, cancellation, and unchanged readiness behavior.
- PASS -- the exact OpenBao authorization matrix completed 1/1 against real Aspire categories and direct Clock/Lifecycle daprd ports.
- PASS -- the amended standalone routed telemetry method completed 1/1 in 53.008 seconds after review hardening, with heartbeat, validation, inspection, actor-state, and health assertions unchanged.
- PARTIAL -- the completed literal fast lane produced 302 passed, 1 failed, and 8 skipped in 18m37s. Both repaired routing surfaces and every required surface passed; the sole failure was an unrelated embedding-provider fixture whose `memories` AppHost resource failed before the test body.
- PASS -- the fast-lane TRX verifier accepted every required surface and explicitly found passed OpenBao topology and routed Access Telemetry classes.
- PASS -- the sole full-lane failure, `EmbeddingProviderFailureIntegrationTests.Provider500_ExhaustsRetriesAndPersistsFailedUnit`, completed 1/1 unchanged in 241.753 seconds when rerun in isolation, supporting classification as environmental startup fallout rather than a routing regression; this isolated pass is supporting evidence, not broad-lane acceptance.
- STOPPED -- a final clean literal-lane attempt was canceled after the shared `memories` resource failed before routed endpoint resolution and fanned out immediate collection-fixture failures; it supplied no usable TRX and did not alter code.
- PASS -- `git diff --check` reported no whitespace errors.

**Matrix audit:**
- Named sidecar ready and multiple-sidecar isolation -- covered by `ResolveDaprSidecarHttpEndpoint_DistinctExactResources_ReturnDistinctDirectPorts`, the exact OpenBao matrix, and the standalone routed method; all passed.
- Port unavailable/delayed/malformed/similar evidence -- covered by `ResolveDaprSidecarHttpEndpoint_NewerMalformedAndSimilarEntries_ReturnLatestExactValidPort`, `ResolveDaprSidecarHttpEndpoint_MissingExactValidEvidence_FailsClosed`, `WaitForDaprSidecarHttpEndpoint_DelayedExactEvidence_ResolvesFreshSnapshot`, and `WaitForDaprSidecarHttpEndpoint_MissingEvidenceUntilDeadline_FailsClosed`; all passed in the 44-test fixture class.
- Boundary denial -- covered by `DaprSecretReadinessStatus_PermanentAndTransientBoundaries_AreExplicit` for 400/401/403/405/406/415 and by the exact live OpenBao matrix; all passed.

## Suggested Review Order

**Shared direct-sidecar routing**

- Start where fixture initialization abandons DCP proxies for exact named daprd endpoints.
  [`AspireIngestionPipelineFixture.cs:1769`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L1769)

- Resolve only exact resource categories and the latest valid IPv4-loopback port.
  [`AspireIngestionPipelineFixture.cs:1920`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L1920)

- Poll fresh log snapshots within the existing bounded readiness budget.
  [`AspireIngestionPipelineFixture.cs:1962`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L1962)

- Preserve Aspire resource information logs needed for direct endpoint discovery.
  [`AspireIngestionPipelineFixture.cs:1690`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L1690)

**Standalone routed telemetry parity**

- Route Clock and Lifecycle assertions through their independently resolved daprd endpoints.
  [`AccessTelemetryAspireRoutedIntegrationTests.cs:80`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryAspireRoutedIntegrationTests.cs#L80)

**Boundary verification**

- Prove distinct exact resources cannot borrow similar sidecar ports.
  [`AspireIngestionPipelineFixtureTests.cs:78`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs#L78)

- Cover delayed evidence convergence and bounded fail-closed exhaustion.
  [`AspireIngestionPipelineFixtureTests.cs:177`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs#L177)
