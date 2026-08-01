---
title: 'Authorize the 2026-08-01 pushall synchronization'
type: 'bugfix'
created: '2026-08-01'
status: 'ready-for-dev'
review_loop_iteration: 0
baseline_commit: '1d9e9c89ef53d877b4ec09face575c36e5889854'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Administrator-authorized `/pushall` run staged one mixed snapshot containing already-owned Story 27.3 work, Story 31 correction records, and root-declared submodule pointer synchronization. The required commit is blocked because no existing artifact may truthfully own every forbidden-default path in that aggregate snapshot.

**Approach:** Authorize exactly this mixed commit through a normal standalone spec whose scope enumerates every staged path. This spec owns only the synchronization envelope; it does not transfer, widen, or rewrite the underlying story, governance, code, test, tooling, or dependency ownership.

## Boundaries & Constraints

**Always:** Keep the staged path set exact, preserve each underlying owner's records, use the mandated `/pushall` commit subject plus this spec's `Story-Key`, pass all repository hooks and commitlint checks, and resume the superproject procedure only after the commit succeeds. The Administrator's 2026-08-01 repair authorization permits only the `Hexalith.Builds` and `Hexalith.Commons` gitlink OIDs to change after approval: rebuild their intended changes on current `origin/main`, preserve their old local histories on backup branches, and push only fast-forward updates. End on `main` and report every merge, deletion, skip, failure, and push result.

**Ask First:** Any staged path-set change, any staged content change other than the two authorized gitlink repairs, any change to existing Story 27.3 or Story 31 scope, any merge conflict without a safe validated resolution, or any operation requiring history rewriting beyond the recoverable local backup-branch repair or a broader authorization than this exact snapshot.

**Never:** Use `Scope-Override` to bypass forbidden-default protection, bypass hooks, reset or clean user work, rewrite fetched history, initialize nested submodules, force-delete a local branch, delete an unmerged or moved remote branch, or claim that an unpushed submodule commit is remotely available.

## Ownership Partition

- Story 27.3 retains ownership of its runtime, test, tooling, and associated governance work.
- Stories 31.1 and 31.2 retain ownership of their records, approved proposals, and shared governance hunks.
- Dependency-sync sessions retain pointer-only ownership of the six root-declared submodules.
- This spec owns only the aggregate `/pushall` commit and validation envelope.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`
- `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md`
- `_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-01.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-30.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-2-reserved-eventstore-scope-and-deferred-work-retarget.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01.md`
- `_bmad-output/process-notes/story-creation-lessons.md`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.PolymorphicSerializations`
- `references/Hexalith.Tenants`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleProcessor.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/IAccessTelemetryStateStore.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/InMemoryAccessTelemetryStateStore.cs`
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/AccessTelemetryStateStoreOrderingParityTests.cs`
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/DaprAccessTelemetryStateStoreTests.cs`
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/InMemoryAccessTelemetryStateStoreContractTests.cs`
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/LifecycleActorCheckpointTests.cs`
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/TransactionalDaprState.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs`
- `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py`
- `tools/production-deployment-health.ps1`
- `tools/validate-production-deployment-evidence.ps1`
- `tools/verify-production-deployment.ps1`

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Exact authorized snapshot | The approved 34-path set is staged, with only the authorized Builds and Commons pointer repairs permitted | Scope, tenant-evidence, readiness, diff, hook, and commitlint gates pass | Stop before commit or push on any failure |
| Snapshot drift | A staged path is added, removed, or renamed | The exact File Scope comparison fails closed | Ask for renewed authorization; do not broaden silently |
| Remote movement | A fetched branch moves before pruning | Exact-OID lease rejects deletion | Preserve the branch and report the failed lease without retry |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` -- precedent for an exact human-approved mixed `/pushall` owner envelope.
- `tools/check-story-file-scope.py` -- validates exact standalone-spec ownership, including forbidden-default paths.
- `tools/check-tenant-isolation-evidence.py` -- confirms whether the staged set requires tenant-isolation evidence.
- `tools/check-story-review-readiness.py` -- verifies the completed standalone spec does not violate story readiness policy.
- `.githooks/commit-msg` -- authoritative local composition of commitlint and repository governance gates.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-01.md` -- establish the exact owner-valid synchronization envelope without changing underlying story ownership.
- [ ] Git index and commit metadata -- stage exactly the declared File Scope and commit with `build: sync local changes via /pushall` plus `Story-Key: spec-pushall-sync-2026-08-01`.
- [ ] Superproject refs -- finish the `/pushall` merge, push, safe local deletion, exact-lease remote pruning, final fetch, and final-branch verification.

**Acceptance Criteria:**
- Given the exact approved staged snapshot, when repository scope and commit hooks run, then every path is accepted without `Scope-Override` or bypass flags.
- Given a successful validated commit, when the full branch range is checked, then commitlint passes before the superproject push.
- Given a successful default-branch push, when branch pruning runs, then only merged local branches and fetched remote tips proven ancestral are deleted using safe deletion and exact expected-OID leases.
- Given any divergence, moved ref, validation failure, or push rejection, when the workflow reaches that operation, then it preserves the affected work or branch and reports the exact failure.

## Spec Change Log

- 2026-08-01 -- Parent reachability verification found the approved Builds and Commons OIDs unavailable from their origins. The Administrator authorized rebuilding both intended changes on current `origin/main`, preserving the old local histories on backup branches, pushing only fast-forward updates, and updating only those two parent gitlink OIDs.

## Design Notes

The synchronization commit message is fixed:

```text
build: sync local changes via /pushall

Synchronize the authorized, owner-partitioned root work and root-declared
submodule pointers.

Story-Key: spec-pushall-sync-2026-08-01
```

## Verification

**Commands:**
- `git diff --cached --check` -- expected: no whitespace or conflict-marker errors.
- `python3 tools/check-story-file-scope.py --story-key spec-pushall-sync-2026-08-01 --staged` -- expected: all 34 paths accepted.
- `python3 tools/check-tenant-isolation-evidence.py --story-key spec-pushall-sync-2026-08-01 --staged` -- expected: no triggered tenant-isolation surface, or fail closed with exact evidence requirement.
- `python3 tools/check-story-review-readiness.py --story-key spec-pushall-sync-2026-08-01 --staged --derive-cumulative` -- expected: standalone-spec readiness passes.
- `npx commitlint --edit <message-file> --verbose` and `.githooks/commit-msg <message-file>` -- expected: proposed message and composed local gates pass.
- `npx commitlint --from "$(git merge-base origin/main HEAD)" --to HEAD --verbose` -- expected: complete push range passes.
