---
title: 'Authorize the 2026-08-16 pushall synchronization'
type: 'maintenance'
created: '2026-08-16'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: '348ae37b094aa55cc12eda7b4c070f5b514afaa1'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-15.md'
---

<frozen-after-approval reason="human-authorized synchronization envelope — do not modify unless the path set changes">

## Intent

**Problem:** The user-authorized push must publish four root-declared submodule pointers after each submodule commit was already published. No underlying implementation story owns this pointer-only parent snapshot.

**Approach:** Authorize exactly one parent commit containing this synchronization envelope and the four staged gitlinks. This spec owns only the root commit, validation, and fast-forward push.

## Boundaries & Constraints

**Always:** Preserve every existing commit, process only root-declared submodules, validate Conventional Commit messages, require every target commit to equal its fetched `origin/main`, and push the superproject last.

**Never:** Bypass hooks, rewrite history, initialize nested submodules, modify submodule content, force-push, or include paths outside this exact envelope.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-16.md`
- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Tenants`

</frozen-after-approval>

## Tasks & Acceptance

**Execution:**

- [x] Verify every target submodule commit is the clean fetched `origin/main` tip.
- [x] Establish the exact owner-valid synchronization envelope.
- [ ] Commit and push the superproject snapshot as a fast-forward update.

**Acceptance Criteria:**

- Given the exact staged snapshot, when repository scope and commit hooks run, then every path is accepted without an override.
- Given each staged gitlink, when its submodule remote is fetched, then the target commit equals that submodule's `origin/main`.
- Given a validated local commit and unchanged root remote, when `main` is pushed, then local and remote heads match without force.
- Given any validation failure or remote movement, when publication cannot proceed safely, then the affected work is preserved and the push stops.

## Verification

- `git diff --cached --check`
- `python3 tools/check-story-file-scope.py --story-key spec-pushall-sync-2026-08-16 --staged`
- `python3 tools/check-tenant-isolation-evidence.py --story-key spec-pushall-sync-2026-08-16 --staged`
- `python3 tools/check-story-review-readiness.py --story-key spec-pushall-sync-2026-08-16 --staged --derive-cumulative`
- `npx commitlint --from "$(git merge-base origin/main HEAD)" --to HEAD --verbose`
