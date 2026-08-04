---
title: 'Authorize the BMAD 6.10.1n49 update'
type: 'maintenance'
created: '2026-08-04'
status: 'done'
baseline_commit: '7b55c62fdc64b7ea3ac0ec5ca693f4889de2135d'
review_loop_iteration: 0
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad/config.toml'
---

<frozen-after-approval reason="human-authorized BMAD 6.10.1n49 update envelope — do not modify unless the staged path set changes">

## Intent

**Problem:** The BMAD 6.10.1n49 installer refresh spans generated module configuration, synchronized Codex and Claude skill trees, loop integration files, and one already-published root submodule pointer, while no product story owns that maintenance snapshot.

**Approach:** Authorize only the staged BMAD update through a narrow standalone maintenance spec. Existing product stories retain responsibility for product code and planning artifacts.

## Boundaries & Constraints

**Always:** Preserve the staged installer output, validate the requested Conventional Commit subject before and after committing, verify the root-declared submodule pointer is published, and push the superproject only after all repository gates pass.

**Never:** Bypass hooks, rewrite history, initialize nested submodules, include product source or test changes, or alter the staged path set without renewed authorization.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-update-bmad-6-10-1n49-2026-08-04.md`
- `.agents/skills/**`
- `.bmad-loop/bmad_loop_hook.py`
- `.claude/skills/**`
- `.github/agents/bmad-agent-tech-writer.agent.md`
- `.gitignore`
- `_bmad/**`
- `references/Hexalith.FrontComposer`

</frozen-after-approval>

## Tasks & Acceptance

**Execution:**
- [x] Preserve the user-staged BMAD 6.10.1n49 installer output.
- [x] Verify the FrontComposer pointer targets a commit already published on its `origin/main`.
- [x] Establish the exact path envelope for the staged maintenance snapshot.
- [ ] Commit with the requested subject and this spec's `Story-Key`, validate the outgoing range, and push `main`.

**Acceptance Criteria:**
- Every staged path is accepted by repository scope validation without `Scope-Override`.
- The staged diff passes whitespace and conflict-marker validation.
- The exact requested subject and full outgoing commit range pass the pinned commitlint configuration.
- The root `main` push succeeds without force or history rewriting.

## Verification

- `git diff --cached --check`
- `python3 tools/check-story-file-scope.py --story-key spec-update-bmad-6-10-1n49-2026-08-04 --staged`
- `python3 tools/check-tenant-isolation-evidence.py --story-key spec-update-bmad-6-10-1n49-2026-08-04 --staged`
- `python3 tools/check-story-review-readiness.py --story-key spec-update-bmad-6-10-1n49-2026-08-04 --staged --derive-cumulative`
- `npx commitlint --from "$(git merge-base origin/main HEAD)" --to HEAD --verbose`
