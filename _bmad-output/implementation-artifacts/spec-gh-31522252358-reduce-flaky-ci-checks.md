---
title: 'Stabilize required CI and remove duplicate fast-integration executions'
type: 'bugfix'
created: '2026-08-11'
status: 'done'
review_loop_iteration: 0
baseline_commit: '71cfcb9a223655262a4d41eac26df730a0786781'
context:
  - '{project-root}/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-26-memories-ci-cd-alignment.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Required `integration-fast` is unreliable and expensive: 144 of run 31522252358's 147 failures cascaded from one shared-fixture clock-sidecar timeout. Scheduled nightlies and unscoped branch pushes also repeat its evidence.

**Approach:** Preserve the required lane and coverage, localize auxiliary-sidecar readiness to its owning test, and clean failed fixture startups. Limit push CI to `main`, make nightly fast integration manual-only, and retain scheduled slow integration and benchmarks.

## Boundaries & Constraints

**Always:** Keep required job names and the exact fast selector `Category=Integration&Category!=IntegrationSlow&Category!=Performance`; preserve bounded, fail-closed OpenBao matrix checks, evidence artifacts, the original startup exception, and scheduled slow integration and benchmarks.

**Ask First:** Changing branch protection, required-check names, categorization/filtering, accepted secret statuses, timeouts, production deployment, or web E2E.

**Never:** Disable/soften tests, hide deterministic failures with retries or ignored errors, update dependencies, or mutate GitHub settings.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| PR commit | Open-PR branch push | One `pull_request` CI run | Required failures stay red with artifacts |
| Main commit | Push to `main` | One `push` CI run with existing check names | Required failures stay red |
| Nightly | Cron/manual event | Cron runs slow+benchmark; manual may also run fast | No scheduled fast duplicate |
| Auxiliary sidecar fault | Clock/lifecycle marker unavailable | Unrelated tests initialize; matrix performs the exact probe | Matrix alone reports bounded failure |
| Partial startup | Initialization throws after acquisition | Owned topology, scopes, and temp config are cleaned | Cleanup must not mask the original error |
| Red fast test | Any test fails | Inventory verifier and artifact upload still run | Job remains red |

</frozen-after-approval>

## Code Map

- `.github/workflows/{ci,nightly}.yml` -- triggers, lane ownership, verification, artifacts.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- shared topology lifecycle/readiness.
- `tests/Hexalith.Memories.IntegrationTests/OpenBaoTopologyIntegrationTests.cs` -- full sidecar-access matrix owner.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs` -- fixture contracts.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- workflow contracts.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/{ci,nightly}.yml` -- use main-only push CI, execute inventory verification after failures, and make nightly fast manual-only.
- [x] `AspireIngestionPipelineFixture.cs` and `OpenBaoTopologyIntegrationTests.cs` -- localize auxiliary readiness and clean failed startups without masking their error.
- [x] `AspireIngestionPipelineFixtureTests.cs` -- cover cleanup and localized readiness.
- [x] `CiTestInventoryTests.cs` -- lock down triggers, lane ownership, selector, and verifier condition.

**Acceptance Criteria:**
- Required-check names and the fast-test surface remain unchanged; no repository-setting update is needed.
- A healthy fast lane passes inventory verification and uploads its results.
- Failed initialization leaves no owned topology that can collide with a later independent fixture.

## Spec Change Log

- 2026-08-11: Implemented the approved CI trigger, lane-ownership, fixture-readiness, and failed-startup cleanup changes.
- 2026-08-11: Hardened failed-startup cleanup diagnostics and added exact readiness, cleanup-slot, and workflow-ownership review contracts.
- 2026-08-11: Declared the exact implementation File Scope required by the repository commit gate.

## Design Notes

The last 30 CI runs had no successful fast lane; nine sampled failures repeated two infrastructure signatures. xUnit aborts all fixture consumers when initialization throws and does not dispose that fixture. Keep server OpenBao canaries in common startup, but move clock/lifecycle probes used only by `DaprSidecarMatrix_ShouldEnforceSecretAccessBoundaries` to that test.

Scheduled nightly fast uses the same selector as required CI; slow integration and benchmarks are unique. Manual-only fast retains an operator diagnostic path.

## Verification

**Commands:**
- `actionlint .github/workflows/ci.yml .github/workflows/nightly.yml` -- workflows valid.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -c Release -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- build passes.
- `dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -c Release --no-build --filter "FullyQualifiedName~AspireIngestionPipelineFixtureTests"` -- fixture contracts pass.
- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj -c Release --filter "FullyQualifiedName~CiTestInventoryTests" -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- workflow contracts pass.
- Exact CI fast command plus `tools/verify-integration-fast-coverage.py` -- pass in a CI-compatible environment.
- `git diff --check` -- expected: no whitespace errors.

**Results (2026-08-11):**
- `actionlint .github/workflows/ci.yml .github/workflows/nightly.yml` -- passed.
- Release integration-project build -- passed with 0 warnings and 0 errors.
- `AspireIngestionPipelineFixtureTests` -- 46 passed, 0 failed.
- `CiTestInventoryTests` -- 66 passed, 0 failed.
- Exact fast selector -- 315 passed, 8 environment-gated skipped, 0 failed in 20m21s; the coverage verifier passed and confirmed the OpenBao authorization matrix.

**Review-fix results (2026-08-11):**
- `actionlint .github/workflows/ci.yml .github/workflows/nightly.yml` -- passed.
- Release integration-project build -- passed with 0 warnings and 0 errors.
- `AspireIngestionPipelineFixtureTests` -- 52 passed, 0 failed.
- `CiTestInventoryTests` -- 66 passed, 0 failed.
- Exact fast selector -- 321 passed, 8 environment-gated skipped, 0 failed in 1153.5s; the coverage verifier passed and confirmed every required surface, including the OpenBao authorization matrix.

## File Scope

Allowed files for this story:

- `.github/workflows/ci.yml` -- main-only push ownership and failure-path verification.
- `.github/workflows/nightly.yml` -- manual-only duplicate fast lane.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- exact workflow contracts.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- readiness isolation and failed-startup cleanup.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs` -- lifecycle regression coverage.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs` -- localized matrix readiness owner.
- `_bmad-output/implementation-artifacts/spec-gh-31522252358-reduce-flaky-ci-checks.md` -- approved contract, evidence, and review trail.

## Suggested Review Order

**Failure isolation and cleanup**

- Localize auxiliary readiness to its sole security-matrix consumer.
  [`OpenBaoTopologyIntegrationTests.cs:64`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs#L64)

- Probe auxiliary sidecars without blocking shared fixture initialization.
  [`AspireIngestionPipelineFixture.cs:539`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L539)

- Clean all six owned resources after partial startup while preserving its root failure.
  [`AspireIngestionPipelineFixture.cs:1226`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L1226)

- Aggregate cleanup faults without replacing the initiating exception.
  [`AspireIngestionPipelineFixture.cs:1277`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L1277)

**CI execution ownership**

- Run branch pushes only for main while preserving pull-request coverage.
  [`ci.yml:3`](../../.github/workflows/ci.yml#L3)

- Verify required-surface evidence after failures, then always upload artifacts.
  [`ci.yml:428`](../../.github/workflows/ci.yml#L428)

- Keep duplicate nightly fast integration manual while schedules retain unique evidence.
  [`nightly.yml:18`](../../.github/workflows/nightly.yml#L18)

**Regression contracts**

- Enforce exact workflow triggers, commands, artifacts, and selector ownership.
  [`CiTestInventoryTests.cs:134`](../../tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs#L134)

- Guard readiness localization, cancellation, sequencing, and cleanup behavior.
  [`AspireIngestionPipelineFixtureTests.cs:220`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs#L220)
