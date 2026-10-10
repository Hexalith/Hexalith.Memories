---
title: 'Close the remaining Story 5.4 live tenant-isolation evidence gap'
type: 'chore'
created: '2026-10-10'
status: 'done'
baseline_commit: 'cac4a329b94300bf8cfa433530a24d4bd1a25f73'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-5-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-5-4-tenant-context-requalification.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.4 and its recent requalification are marked done, but the token-free sidecar and authenticated HTTP graph isolation tests did not reach their test bodies in bounded Aspire runs. Their live endpoint evidence remains open.

**Approach:** Restore reliable test-fixture startup, execute those two existing checks against the real sidecar and application, and record the exact negative isolation evidence. Change product code only if an executed test demonstrates a defect.

**Decision:** The user selected the live fixture evidence follow-up on 2026-10-10. The separately deferred app-token denial, token-enabled CI, and MCP transport work remain outside this spec.

## Boundaries & Constraints

**Always:** Preserve bearer tenant authorization and caller-facing 403 `TENANT_FORBIDDEN`, authorized unknown-tenant 404 `TENANT_NOT_FOUND`, and internal Critical `TENANT_MISMATCH` for corrupt records. Production requires Dapr tokens; local/test defaults remain token-free. Attach focused cross-tenant denial evidence.

**Never:** Reset the completed story, change API error codes or secrets, force token mode into every local test, or treat direct FalkorDB graph separation as proof of authenticated HTTP graph selection.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Default sidecar | No token mode; direct metadata request | Real sidecar returns 200, as the existing test requires | Fixture failure is reported with its failing resource and cause |
| Authenticated graph | Two bearer-scoped tenants with colliding edge IDs | Tenant A's HTTP traversal returns no tenant B nodes or edges | Foreign tenant request is denied before graph work |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- locate the startup wait or failing resource; preserve token propagation already added for direct clients.
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs` -- run the existing direct sidecar method in default mode; its enabled branch passed in the prior requalification.
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` -- run `VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` through authenticated HTTP and its planted-foreign-marker negative control; prior graph runs timed out before the body.
- `src/Hexalith.Memories.AppHost/Program.cs` -- retain `DAPR_API_TOKEN_MODE=enabled` behavior and production token wiring.
- `_bmad-output/implementation-artifacts/spec-5-4-tenant-context-requalification.md` -- prior evidence and accepted deferrals; do not rewrite its frozen intent.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- diagnose and fix fixture startup only where an observed failure requires it.
- [x] `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs` -- run the token-free direct-sidecar assertion to completion; strengthen it only if it can false-pass.
- [x] `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` -- run the authenticated two-tenant graph test and `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage` to completion; assert isolation and that the proof detects planted leakage.
- [x] `_bmad-output/implementation-artifacts/spec-5-4-live-tenant-isolation-evidence.md` -- record commands, test results, negative evidence, and any accepted blocker with owner and reopen trigger.

**Acceptance Criteria:**
- Given default token mode and a running Aspire fixture, when the direct sidecar test executes, then it reaches its assertion and passes against the real sidecar.
- Given two authenticated tenants with identical graph structures and colliding edge IDs, when tenant A traverses through the HTTP endpoint, then the result contains no tenant B nodes or edges.
- Given a deliberately planted foreign graph marker, when the negative-control test runs, then its collision assertions detect the contamination.

## Implementation Notes

- The fixture's first token-free startup took 147.602 seconds, and the planted-marker run took 142.174 seconds; the intervening graph run took 14.366 seconds. All reached their test bodies and passed within their bounds. Startup logs showed transient OTLP exporter connection refusals and a Dapr workflow gRPC ping/reconnect, but no persistent failed resource or test failure. The earlier 65- and 120-second bounds were too short to establish a fixture defect. No fixture, test, AppHost, or product code was changed. The direct sidecar test already resolves the exact `memories-dapr-cli` endpoint and asserts HTTP 200 from `/v1.0/metadata`, so it cannot pass by calling the application port.
- The authenticated graph test provisioned separate A and B tenants, inserted the same source/target node IDs and relationship type in both graphs, confirmed their edge IDs collided, and traversed both tenant paths over HTTP using bearer tokens scoped by the fixture to each requested tenant. Its strict node content, source URI, and edge `VerifiedBy` marker assertions passed for A and B, excluding foreign nodes and edges.
- The planted-marker negative control retained the colliding edge IDs, wrote tenant B's edge marker into tenant A's edge, then traversed A over authenticated HTTP. It required that the returned edge expose the planted B marker and that `AssertTraversalIsFixtureLocal` throw `ShouldAssertException` naming the tenant-local edge marker. Tenant B's traversal and underlying `previousConfidence` stayed unchanged. This demonstrates that the isolation assertion detects contamination instead of passing vacuously.
- The focused endpoint authorization theory ran all six cases, including tenant A's bearer requesting tenant B's traversal route. Each returned 403 `TENANT_FORBIDDEN` with no Dapr, actor, Redis, or FalkorDB dependency calls. This is the denial-before-graph-work evidence for the matrix row.
- No accepted blocker remains. Fixture startup latency is a residual operational risk; the Fixture/AppHost integration owner should reopen if a future 360-second bounded run reports `Total: 0` or a named resource fails readiness, using that run's resource logs to identify the cause.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 routine integration JWT settings | maybe-false | defer | `ci.yml` runs the integration lane without the test JWT environment used by this focused run, and the earlier local run without it stopped at EventStore gateway startup. CI failure has not been observed in this run; an env-free CI-equivalent run would settle it. This predates the selected live-evidence follow-up. |
| B2 live foreign-bearer denial | medium | defer | The live graph method attaches a route-matching bearer, so it does not prove live A-to-B caller denial. `ServerEndpointAuthorizationTests.TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState` ran all six cases, including traversal, and verifies 403 before dependencies. The additional live mismatch proof is a separate pre-existing gap beyond the two blocked methods selected here. |
| B3 external test timeout below fixture timeout | low | reject | The fixture permits up to 12 minutes while the evidence commands bound runs at 180/360 seconds; all three methods actually reached assertions and passed inside those bounds. The proposed correction is only to this spec's future command text, so it is rejected under the review rule for spec-only fixes. |
| B4 whole-run timing and log attribution | low | reject | The numbers are whole test-run durations and the transient OTLP/Dapr messages were observed without retained log excerpts. This does not change the recorded 1/1 executed results or show a product defect; narrowing those notes or attaching excerpts would edit only this spec, so the review rule rejects it. |

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Debug -m:1` -- passed: 0 warnings, 0 errors.
- Run each method below from the built xUnit v3 DLL with `-method`; unset `DAPR_API_TOKEN_MODE`, `DAPR_API_TOKEN`, and `APP_API_TOKEN`, and supply the fixture's test JWT settings (`Authentication__JwtBearer__SigningKey=hexalith-memories-test-signing-key-32b`, `Authentication__JwtBearer__Issuer=hexalith-memories-test`, `Authentication__JwtBearer__Audience=hexalith-memories-server`). Bound each run and distinguish a fixture timeout (`Total: 0`) from a test failure:
  - `timeout -s INT 180s env -u DAPR_API_TOKEN_MODE -u DAPR_API_TOKEN -u APP_API_TOKEN Authentication__JwtBearer__SigningKey=hexalith-memories-test-signing-key-32b Authentication__JwtBearer__Issuer=hexalith-memories-test Authentication__JwtBearer__Audience=hexalith-memories-server dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests.DaprSidecar_RequestWithoutApiToken_IsRejected` -- exit 0; 1/1 passed, 0 skipped, 147.602 seconds. The real sidecar metadata request returned 200 in default token mode.
  - `timeout -s INT 360s env -u DAPR_API_TOKEN_MODE -u DAPR_API_TOKEN -u APP_API_TOKEN Authentication__JwtBearer__SigningKey=hexalith-memories-test-signing-key-32b Authentication__JwtBearer__Issuer=hexalith-memories-test Authentication__JwtBearer__Audience=hexalith-memories-server dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` -- exit 0; 1/1 passed, 0 skipped, 14.366 seconds. Authenticated HTTP traversal excluded foreign nodes and edges in both directions despite equal edge IDs.
  - `timeout -s INT 360s env -u DAPR_API_TOKEN_MODE -u DAPR_API_TOKEN -u APP_API_TOKEN Authentication__JwtBearer__SigningKey=hexalith-memories-test-signing-key-32b Authentication__JwtBearer__Issuer=hexalith-memories-test Authentication__JwtBearer__Audience=hexalith-memories-server dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage` -- exit 0; 1/1 passed, 0 skipped, 142.174 seconds. The assertion caught the planted foreign marker.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1` -- passed: 0 warnings, 0 errors.
- `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -method Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests.TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState` -- passed 6/6 theory cases, 0 skipped, 4.830 seconds; traversal mismatch returned 403 `TENANT_FORBIDDEN` before dependencies.
- `git diff --check` -- passed: no whitespace errors.
