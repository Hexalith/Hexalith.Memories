---
title: 'Authorize the 2026-08-03 pushall synchronization'
type: 'maintenance'
created: '2026-08-03'
status: 'ready-for-dev'
review_loop_iteration: 0
baseline_commit: 'a91de0a62ad6b51cc608c58726384876680a30c2'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-01.md'
---

<frozen-after-approval reason="human-authorized synchronization envelope — do not modify unless the path set changes">

## Intent

**Problem:** The Administrator-authorized `/pushall` run contains one mixed snapshot of Story 27.21 work, earlier planning and readiness records, Story 27.3 and Story 31.2 reconciliation, and root-declared submodule pointer synchronization. No underlying story artifact truthfully owns every path in that aggregate snapshot.

**Approach:** Authorize exactly this mixed commit through a standalone synchronization spec whose scope enumerates every staged path. This spec owns only the commit and validation envelope; it does not transfer, widen, or rewrite the underlying story, planning, implementation, test, tooling, or dependency ownership.

## Boundaries & Constraints

**Always:** Keep the staged path set exact, preserve each underlying owner's records, use the mandated `/pushall` commit subject plus this spec's `Story-Key`, pass all repository hooks and commitlint checks, process only root-declared submodules, and push the superproject last.

**Ask First:** Any staged path-set change after this envelope is established, any additional content change, any merge conflict without a safe validated resolution, or any operation requiring history rewriting.

**Never:** Use `Scope-Override` to bypass forbidden-default protection, bypass hooks, reset or clean user work, rewrite fetched history, initialize nested submodules, force-delete a local branch, or delete an unmerged or moved remote branch.

## Ownership Partition

- Story 27.21 retains ownership of its C1.15 producer, fixture, story, and governance reconciliation.
- Story 27.3 and Story 31.2 retain ownership of their existing implementation and planning records.
- Readiness and correct-course workflows retain ownership of their reports and approved proposals.
- Dependency-sync sessions retain pointer-only ownership of the seven root-declared submodules.
- This spec owns only the aggregate `/pushall` commit and validation envelope.

## File Scope

Allowed files for this story:

- `.github/workflows/ci.yml`
- `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md`
- `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`
- `_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/epic-27-context.md`
- `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity.md`
- `_bmad-output/implementation-artifacts/spec-27-3-phase-1-contract-reconciliation.md`
- `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-03.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-02-prior.md`
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-02-rerun.md`
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-02.md`
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-03.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-2-reserved-eventstore-scope-and-deferred-work-retarget.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02-story-31-2-dapr-openbao-artifact-alignment.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md`
- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.PolymorphicSerializations`
- `references/Hexalith.Tenants`
- `tests/tooling/access_telemetry_c1/fixtures/c1_15_complete.json`
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py`
- `tools/verify-access-telemetry-c1.ps1`

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Exact authorized snapshot | The 29-path set above is staged | Scope, tenant-evidence, readiness, diff, hook, and commitlint gates pass | Stop before commit or push on any failure |
| Snapshot drift | A staged path is added, removed, or renamed | Exact File Scope comparison fails closed | Ask for renewed authorization; do not broaden silently |
| Remote movement | A fetched branch moves before pruning | Exact-OID lease rejects deletion | Preserve the branch and report the failed lease without retry |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-01.md` -- precedent for an exact authorized mixed `/pushall` envelope.
- `tools/check-story-file-scope.py` -- validates exact standalone-spec ownership, including forbidden-default paths.
- `tools/check-tenant-isolation-evidence.py` -- confirms whether the staged set requires tenant-isolation evidence.
- `tools/check-story-review-readiness.py` -- verifies the standalone spec does not violate story-readiness policy.
- `.githooks/commit-msg` -- composes commitlint and repository governance gates.

## Tasks & Acceptance

**Execution:**
- [x] Establish the exact owner-valid synchronization envelope without changing underlying story ownership.
- [ ] Stage exactly the declared File Scope and commit with `build: sync local changes via /pushall` plus `Story-Key: spec-pushall-sync-2026-08-03`.
- [ ] Finish the `/pushall` merge, push, safe local deletion, exact-lease remote pruning, final fetch, and final-branch verification.

**Acceptance Criteria:**
- Given the exact authorized snapshot, when repository scope and commit hooks run, then every path is accepted without `Scope-Override` or bypass flags.
- Given a successful validated commit, when the full branch range is checked, then commitlint passes before the superproject push.
- Given a successful default-branch push, when branch pruning runs, then only merged local branches and fetched remote tips proven ancestral are deleted using safe deletion and exact expected-OID leases.
- Given any divergence, moved ref, validation failure, or push rejection, when the workflow reaches that operation, then it preserves the affected work or branch and reports the exact failure.

## Design Notes

The synchronization commit message is fixed:

```text
build: sync local changes via /pushall

Synchronize the authorized, owner-partitioned root work and root-declared
submodule pointers.

Story-Key: spec-pushall-sync-2026-08-03
```

## Verification

**Commands:**
- `git diff --cached --check` -- expected: no whitespace or conflict-marker errors.
- `python3 tools/check-story-file-scope.py --story-key spec-pushall-sync-2026-08-03 --staged` -- expected: all 29 paths accepted.
- `python3 tools/check-tenant-isolation-evidence.py --story-key spec-pushall-sync-2026-08-03 --staged` -- expected: no triggered tenant-isolation surface, or fail closed with the exact evidence requirement.
- `python3 tools/check-story-review-readiness.py --story-key spec-pushall-sync-2026-08-03 --staged --derive-cumulative` -- expected: standalone-spec readiness passes.
- `npx commitlint --edit <message-file> --verbose` and `.githooks/commit-msg <message-file>` -- expected: proposed message and composed local gates pass.
- `npx commitlint --from "$(git merge-base origin/main HEAD)" --to HEAD --verbose` -- expected: complete push range passes.
