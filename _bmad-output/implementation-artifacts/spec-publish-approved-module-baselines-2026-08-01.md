---
title: 'Authorize approved module baseline publication'
type: 'build'
created: '2026-08-01'
status: 'ready-for-dev'
baseline_commit: 'b9ae7b9d11a11cd239ff5aef7c552cd393da7b99'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-eventstore-source-and-3-89-package-identities.md'
---

<frozen-after-approval reason="human-owned publication intent — do not modify unless the Administrator renegotiates">

## Intent

**Problem:** The Administrator-approved correct-course proposal and the already-published Builds and
FrontComposer root gitlink revisions form one pending parent-repository snapshot, but the repository
commit gate requires one exact standalone owner for the aggregate changed-file set.

**Approach:** Authorize exactly one parent commit containing this ownership envelope, the approved
proposal, and the two root gitlink updates. This spec owns only the publication envelope; it does not
claim completion of Epic 28, Story 28.1, Story 28.2, or the FrontComposer verification route.

## Boundaries & Constraints

**Always:** Keep the staged path set equal to the four paths in File Scope; keep Builds at
`10af541e7b2a5a4664be37c9495930844e0954a8` and FrontComposer at
`a746cde4bd128399522f895a7ac7f077c4ee64da`; require both commits to be reachable from their fetched
`origin/main`; preserve all other root gitlinks; validate the message before and after commit; push
only a fast-forward update of root `main`.

**Ask First:** Any staged path addition, removal, rename, content change beyond this spec, OID change,
remote divergence, merge, rebase, force push, or broader ownership claim.

**Never:** Edit submodule content, initialize or update nested submodules, bypass hooks, use a scope
override, rewrite history, merge or prune branches, move the EventStore gitlink, or represent this
publication as implementation evidence for the approved Epic 28 split.

## Ownership Partition

- The correct-course workflow retains ownership of the approved planning proposal.
- Story 28.2 retains implementation and evidence ownership for EventStore `3.89.0` package adoption.
- The independent FrontComposer route retains its focused final-tree evidence obligation.
- This spec owns only the four-path commit and fast-forward push envelope.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-eventstore-source-and-3-89-package-identities.md`
- `references/Hexalith.Builds`
- `references/Hexalith.FrontComposer`

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Exact approved snapshot | The four declared paths are staged at the approved content and OIDs | All scope, evidence, readiness, whitespace, and commitlint gates pass | Stop before commit or push on any failure |
| Snapshot drift | A staged path or gitlink OID differs | Validation fails closed | Preserve the index and request renewed authorization |
| Root remote movement | `origin/main` advances before push | Do not push a non-fast-forward update | Stop and report the divergence |
| Missing submodule commit | A gitlink OID is not reachable from its fetched `origin/main` | Parent publication is unsafe | Stop without committing the parent snapshot |

</frozen-after-approval>

## Tasks & Acceptance

**Execution:**

- [x] Record the Administrator's authorization for the exact four-file publication envelope.
- [ ] Pass repository scope, evidence, readiness, whitespace, and commit-message gates.
- [ ] Commit the exact snapshot and push root `main` as a fast-forward update.

**Acceptance Criteria:**

- Given the exact staged snapshot, when the story-file-scope gate runs, then all four paths resolve
  to this standalone owner without an override.
- Given the two gitlink revisions, when reachability is checked after fetching their remotes, then
  each revision is an ancestor of its corresponding `origin/main`.
- Given a validated local commit and unchanged fetched root remote, when `main` is pushed, then local
  and remote heads match the new commit without force.
- Given any gate failure or remote movement, when publication cannot proceed safely, then no bypass,
  history rewrite, merge, or branch pruning occurs.

## Design Notes

The commit message uses this owner trailer:

```text
build(deps): adopt approved module baselines

Record the correct-course proposal for EventStore source and package
identities.

Advance Hexalith.Builds to expose EventStore 3.89.0 and synchronize
Hexalith.FrontComposer with its completed governance update.

Story-Key: spec-publish-approved-module-baselines-2026-08-01
```

## Verification

- `git diff --cached --name-only` equals the four paths in File Scope.
- `git diff --cached --check` reports no whitespace or conflict-marker errors.
- `python3 tools/check-story-file-scope.py --story-key spec-publish-approved-module-baselines-2026-08-01 --staged` passes.
- `python3 tools/check-tenant-isolation-evidence.py --story-key spec-publish-approved-module-baselines-2026-08-01 --staged` passes.
- `python3 tools/check-story-review-readiness.py --story-key spec-publish-approved-module-baselines-2026-08-01 --staged --derive-cumulative` passes.
- `npx commitlint --edit <message-file> --verbose` and `.githooks/commit-msg <message-file>` pass.
- `npx commitlint --from "$(git merge-base origin/main HEAD)" --to HEAD --verbose` passes before push.
