---
title: 'Authorize the 2026-08-03 readiness-remediation pushall synchronization'
type: 'maintenance'
created: '2026-08-03'
status: 'ready-for-dev'
review_loop_iteration: 0
baseline_commit: '6698144d6129731e712354b334d8257cd96ee14e'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-03.md'
---

<frozen-after-approval reason="human-authorized synchronization envelope — do not modify unless the path set changes">

## Intent

**Problem:** The Administrator-authorized `/pushall` run contains one mixed snapshot of the 2026-08-03 implementation-readiness report and rerun, its remediation-batch sprint-change proposal, and three root-declared submodule pointer updates. No underlying story artifact truthfully owns every path in that aggregate snapshot.

**Approach:** Authorize exactly this mixed commit through a standalone synchronization spec whose scope enumerates every staged path. This spec owns only the commit and validation envelope; it does not transfer, widen, or rewrite the underlying planning, readiness, or dependency ownership.

## Boundaries & Constraints

**Always:** Keep the staged path set exact, preserve each underlying owner's records, use the mandated `/pushall` commit subject plus this spec's `Story-Key`, pass all repository hooks and commitlint checks, process only root-declared submodules, and push the superproject last.

**Ask First:** Any staged path-set change after this envelope is established, any additional content change, any merge conflict without a safe validated resolution, or any operation requiring history rewriting.

**Never:** Use `Scope-Override` to bypass forbidden-default protection, bypass hooks, reset or clean user work, rewrite fetched history, initialize nested submodules, force-delete a local branch, or delete an unmerged or moved remote branch.

## Ownership Partition

- Readiness and correct-course workflows retain ownership of the implementation-readiness report, rerun, and remediation-batch proposal.
- Dependency-sync sessions retain pointer-only ownership of the three updated root-declared submodules.
- This spec owns only the aggregate `/pushall` commit and validation envelope.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-03-readiness-remediation.md`
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-03-rerun.md`
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-03.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md`
- `references/Hexalith.Commons`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Tenants`

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Exact authorized snapshot | The 7-path set above is staged | Scope, tenant-evidence, readiness, diff, hook, and commitlint gates pass | Stop before commit or push on any failure |
| Snapshot drift | A staged path is added, removed, or renamed | Exact File Scope comparison fails closed | Ask for renewed authorization; do not broaden silently |
| Remote movement | A fetched branch moves before pruning | Exact-OID lease rejects deletion | Preserve the branch and report the failed lease without retry |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-03.md` -- precedent for an exact authorized mixed `/pushall` envelope earlier the same day.
- `tools/check-story-file-scope.py` -- validates exact standalone-spec ownership, including forbidden-default paths.
- `tools/check-tenant-isolation-evidence.py` -- confirms whether the staged set requires tenant-isolation evidence.
- `tools/check-story-review-readiness.py` -- verifies the standalone spec does not violate story-readiness policy.
- `.githooks/commit-msg` -- composes commitlint and repository governance gates.

## Tasks & Acceptance

**Execution:**
- [x] Establish the exact owner-valid synchronization envelope without changing underlying story ownership.
- [ ] Stage exactly the declared File Scope and commit with `build: sync local changes via /pushall` plus `Story-Key: spec-pushall-sync-2026-08-03-readiness-remediation`.
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

Story-Key: spec-pushall-sync-2026-08-03-readiness-remediation
```

## Verification

**Commands:**
- `git diff --cached --check` -- expected: no whitespace or conflict-marker errors.
- `python3 tools/check-story-file-scope.py --story-key spec-pushall-sync-2026-08-03-readiness-remediation --staged` -- expected: all 7 paths accepted.
- `python3 tools/check-tenant-isolation-evidence.py --story-key spec-pushall-sync-2026-08-03-readiness-remediation --staged` -- expected: no triggered tenant-isolation surface, or fail closed with the exact evidence requirement.
- `python3 tools/check-story-review-readiness.py --story-key spec-pushall-sync-2026-08-03-readiness-remediation --staged --derive-cumulative` -- expected: standalone-spec readiness passes.
- `npx commitlint --edit <message-file> --verbose` and `.githooks/commit-msg <message-file>` -- expected: proposed message and composed local gates pass.
- `npx commitlint --from "$(git merge-base origin/main HEAD)" --to HEAD --verbose` -- expected: complete push range passes.
