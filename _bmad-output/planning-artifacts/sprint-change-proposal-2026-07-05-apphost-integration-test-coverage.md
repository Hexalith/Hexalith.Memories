# Sprint Change Proposal: AppHost Integration Test Coverage

Date: 2026-07-05
Project: memories
Scope: Minor direct adjustment

## 1. Issue Summary

Integration test validation for AppHost behavior was incomplete. The AppHost project-resolution guard could be run directly, but it was not selected by the `Category=Integration` fast lane. The Docker-backed AppHost component-file ordering guard also lacked the integration trait, so the integration-fast lane could pass without executing the test that proves Dapr sidecars wait for AppHost-generated Redis component files.

The initial compile failure observed locally was stale restore metadata for `Hexalith.EventStore.Aspire`; a fresh restore cleared it. The durable correction is to ensure AppHost guards are part of the integration lane and required by the integration-fast coverage verifier. No tests assert NuGet package versions.

## 2. Impact Analysis

Epic impact: Epic 18.1 and Epic 26.3 are affected. The change reinforces AppHost project-resolution stability and closes a coverage gap in integration stub/test-surface enforcement.

Story impact: No product stories need re-scoping. Existing AppHost guard tests are reclassified into the integration-fast lane.

Artifact conflicts: PRD, architecture, and UX scope remain unchanged. CI test coverage inventory is updated to require AppHost coverage.

Technical impact: Integration lane now includes `AppHostProjectResolutionTests` and `AppHostComponentFileOrderingTests`. The coverage verifier requires both classes to appear in integration-fast TRX output.

## 3. Recommended Approach

Selected approach: Direct Adjustment.

Rationale: The issue is a test selection and coverage-verification gap, not a product or architecture change. Adding integration traits and required-surface entries is low risk and keeps the existing AppHost behavior guard executable in the intended Docker-backed lane.

Effort: Low.
Risk: Low.
Timeline impact: None beyond the AppHost startup test already intended for the integration lane.

## 4. Detailed Change Proposals

Test classification:

OLD:
- `AppHostProjectResolutionTests` had no `Category=Integration` trait.
- `AppHostComponentFileOrderingTests` had no `Category=Integration` trait.

NEW:
- Both AppHost guard classes are tagged with `Trait("Category", "Integration")`.

Rationale: The integration-fast lane filters on `Category=Integration`; without the trait, AppHost guard tests are not executed by that lane.

Coverage inventory:

OLD:
- `tools/integration-fast-required-surfaces.txt` did not require AppHost surfaces.

NEW:
- Required surfaces include:
  - `apphost-project-resolution`
  - `apphost-start-ordering`

Rationale: The post-test verifier should fail if the integration-fast lane stops executing AppHost coverage.

Explicit non-change:
- Do not assert NuGet package versions in tests. The tests validate project-symbol resolution, AppHost startup ordering, and lane coverage only.

## 5. Implementation Handoff

Scope classification: Minor.

Routed to: Developer agent for direct implementation.

Success criteria:
- `Category=Integration&FullyQualifiedName~AppHost` lists and runs both AppHost guard tests.
- Focused AppHost integration test run passes.
- `git diff --check` passes for touched files.
- No NuGet package version assertion is introduced.
