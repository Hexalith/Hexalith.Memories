---
title: 'Publish telemetry evidence and approved baselines'
type: 'build'
created: '2026-08-01'
status: 'ready-for-dev'
baseline_commit: 'dbb4d77cc18cc40276e76dd98000be42aad33447'
review_loop_iteration: 0
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-27-2-lifecycle-checkpoint-gaps-cr42-cr46.md'
  - '{project-root}/_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md'
---

<frozen-after-approval reason="administrator-authorized publication envelope">

## Intent

**Problem:** The Administrator-authorized staged snapshot combines Story 27.2 lifecycle-checkpoint
evidence, Story 27.3 evidence receipt and planning corrections, an Epic 27 compiled context, and
three already-published root submodule pointers. No existing owner artifact covers that exact mixed
snapshot, so the repository commit gate correctly rejects it.

**Approach:** Authorize exactly one publication commit containing the paths below. This spec owns
only the commit envelope; it does not transfer or broaden the underlying implementation, evidence,
planning, or dependency ownership.

## Boundaries & Constraints

**Always:** Keep the staged set equal to this File Scope, preserve the recorded content and gitlink
OIDs, require every local hook and commitlint gate to pass, and push only a fast-forward update of
root `main` after confirming that every referenced submodule commit is available from its origin.

**Ask First:** Any path addition, removal, rename, content change beyond this envelope, gitlink OID
change, remote divergence, merge, rebase, force push, or history rewrite.

**Never:** Bypass hooks, use `Scope-Override`, alter submodule contents, initialize nested
submodules, discard user work, or claim that this envelope changes the underlying story ownership.

## Ownership Partition

- Story 27.2 retains ownership of the telemetry lifecycle tests and remediation specification.
- Story 27.3 retains ownership of its evidence receipt and approved planning corrections.
- The Epic 27 context remains a compiled planning artifact.
- Dependency-sync sessions retain ownership of the three root submodule pointer updates.
- This spec owns only the exact aggregate commit and push envelope.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/epic-27-context.md`
- `_bmad-output/implementation-artifacts/spec-27-2-lifecycle-checkpoint-gaps-cr42-cr46.md`
- `_bmad-output/implementation-artifacts/spec-publish-telemetry-evidence-and-baselines-2026-08-01.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-27-3-review-readiness-blockers.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01.md`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryOperationRendezvous.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryYamlLeastPrivilegeValidator.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/CollectingLogger.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/CollectingLoggerProvider.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/CoordinatedAccessTelemetryStateStore.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/FailFirstStateStore.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/InnerOperationOverlapStateStore.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/LifecycleProcessBoundaryDeliveryClient.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/ObservedFakeTimeProvider.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/TimedOutageAccessTelemetryDeliveryClient.cs`

## Acceptance Criteria

- The staged path set equals the 22 paths in File Scope with no additions or omissions.
- The integration-test project builds and the lifecycle checkpoint selector passes all eight tests.
- Scope, tenant-isolation, review-readiness, whitespace, and commitlint gates pass without bypasses.
- Each gitlink commit is reachable from its fetched submodule `origin/main`.
- Root `main` is pushed only when it remains a fast-forward continuation of `origin/main`.

</frozen-after-approval>

## Approval

The Administrator explicitly authorized creation of this exact standalone publication envelope on
2026-08-01 after the repository hook rejected the mixed staged snapshot.

## Verification

- `git diff --cached --name-only` must equal the File Scope above.
- `git diff --cached --check` must report no whitespace or conflict-marker errors.
- `python3 tools/check-story-file-scope.py --story-key spec-publish-telemetry-evidence-and-baselines-2026-08-01 --staged` must pass all 22 paths.
- `python3 tools/check-tenant-isolation-evidence.py --story-key spec-publish-telemetry-evidence-and-baselines-2026-08-01 --staged` must pass.
- `python3 tools/check-story-review-readiness.py --story-key spec-publish-telemetry-evidence-and-baselines-2026-08-01 --staged --derive-cumulative` must pass.
- Commitlint must pass before and after the commit, and across the complete push range.
