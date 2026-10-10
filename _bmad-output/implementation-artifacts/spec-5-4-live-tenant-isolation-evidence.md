---
title: 'Close the remaining Story 5.4 live tenant-isolation evidence gap'
type: 'chore'
created: '2026-10-10'
status: 'draft'
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

## Boundaries & Constraints

**Always:** Preserve bearer tenant authorization and caller-facing 403 `TENANT_FORBIDDEN`, authorized unknown-tenant 404 `TENANT_NOT_FOUND`, and internal Critical `TENANT_MISMATCH` for corrupt records. Production requires Dapr tokens; local/test defaults remain token-free. Attach focused cross-tenant denial evidence.

**Never:** Reset the completed story, change API error codes or secrets, force token mode into every local test, or treat direct FalkorDB graph separation as proof of authenticated HTTP graph selection.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Default sidecar | No token mode; direct metadata request | Test reaches real sidecar and asserts the existing default-mode contract | Fixture failure is reported with its failing resource and cause |
| Authenticated graph | Two bearer-scoped tenants with colliding edge IDs | Tenant A's HTTP traversal returns no tenant B nodes or edges | Foreign tenant request is denied before graph work |

</frozen-after-approval>

## Open Questions

- Which Story 5.4 follow-up did you intend? **Live fixture evidence** (the scope drafted here: run the blocked token-free sidecar and authenticated HTTP graph checks); **app-token denial** (prove absent and wrong `APP_API_TOKEN` fail on a live protected route); **routine token-enabled CI** (enroll the existing sidecar test); **MCP token transport** (make direct MCP integration requests work and test them in token mode); or **review only** (assess the already completed 5.4 work without new implementation). These are separately shippable changes recorded in `deferred-work.md`.

## Code Map

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- locate the startup wait or failing resource; preserve token propagation already added for direct clients.
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs` -- run the existing direct sidecar method in default mode; its enabled branch passed in the prior requalification.
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` -- run `VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` through authenticated HTTP; prior runs timed out before the body.
- `src/Hexalith.Memories.AppHost/Program.cs` -- retain `DAPR_API_TOKEN_MODE=enabled` behavior and production token wiring.
- `_bmad-output/implementation-artifacts/spec-5-4-tenant-context-requalification.md` -- prior evidence and accepted deferrals; do not rewrite its frozen intent.

## Tasks & Acceptance

**Execution:**
- [ ] `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- diagnose and fix fixture startup only where an observed failure requires it.
- [ ] `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs` -- run the token-free direct-sidecar assertion to completion; strengthen it only if it can false-pass.
- [ ] `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` -- run the authenticated two-tenant graph test to completion and assert no cross-tenant data.
- [ ] `_bmad-output/implementation-artifacts/spec-5-4-live-tenant-isolation-evidence.md` -- record commands, test results, negative evidence, and any accepted blocker with owner and reopen trigger.

**Acceptance Criteria:**
- Given default token mode and a running Aspire fixture, when the direct sidecar test executes, then it reaches its assertion and passes against the real sidecar.
- Given two authenticated tenants with identical graph structures and colliding edge IDs, when tenant A traverses through the HTTP endpoint, then the result contains no tenant B nodes or edges.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Debug -m:1` -- expected: zero errors.
- Run each named xUnit v3 method from the built test assembly with `-method` and bounded fixture startup -- expected: executed and passed, not skipped or timed out.
- `git diff --check` -- expected: no whitespace errors.
