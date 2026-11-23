---
title: 'Resolve ten deferred story scope and hook findings'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make local story-scope validation reliable and document its current authoring and ownership rules.

- Given an ownerless change, when the shared scope validator runs, then it reports a no-op even for automation branches. Invalid or conflicting owners still fail (DW-162).
- Given a staged rename or a path with spaces, Unicode, or a newline, when the hooks gather changed paths, then they use NUL-delimited Git records and retain both rename endpoints. The message-phase checks share one fresh index snapshot and see changes staged during pre-commit (DW-163, DW-169).
- Given a changed-file operand, when a nonblank value normalizes to no path, then the scope check reports an error. An empty diff still produces a no-op (DW-166).
- Given an environment without a compatible Python interpreter, when either hook runs, then it gives actionable setup guidance. A Python 3 interpreter named `python` remains usable (DW-168).
- Given existing story-scope aliases, an unbackticked allowed bullet, or an artifact on a different Windows drive, when contributors author or validate scope, then the guide names the aliases and tests verify the warning and drive-free diagnostic (DW-172, DW-349, DW-350, DW-377, DW-378).

</frozen-after-approval>

## Implementation Notes

- Baseline `1767e933847d47b0a118bc403a1e55e8efca55ca` has 336 numbered entries with `status: open`. The batch affects contributor tooling. The existing commitlint suite ran 13 tests successfully at the baseline.
- `tools/check-story-file-scope.py` owns the authoring parser and standalone checks. The two `.githooks` entry points and three Python validators own staged path collection. Reuse the existing real-Git commit hook fixture and add focused validator cases for the retired story-scope test suite's relevant coverage. `CONTRIBUTING.md` owns the active operator guidance.
- The baseline returned a no-op for `--changed-file docs/..`. The updated scope validator rejects that input. A real Git hook fixture proved that a path containing spaces, Unicode and a newline retains its bytes through the message checks, and that the commit checks include a path staged by pre-commit.
- Git path diagnostics use the existing relative display convention. The Windows path cases use `ntpath` semantics on Linux. The local hooks and their tests ran on Linux.
- Final verification passed eight story-scope tests, fourteen commitlint tests, POSIX shell syntax checks and `git diff --check`.
- Closed DW-162, DW-163, DW-166, DW-168, DW-169, DW-172, DW-349, DW-350, DW-377 and DW-378. The BMAD dry run reports 326 open numbered entries. The review added two legacy-format follow-ups.

## Review Triage Log

- Medium, deferred. The Markdown File List cannot express a literal newline or backslash in a path. The focused change preserves staged Git names, and the comparison limitation is documented and tracked.
- Medium, deferred. The pull-request workflow still emits a line-oriented path list. The documentation and ledger record the verified discrepancy and workflow owner.
- Low, patched. Missing NUL lists now use the relative path formatter. The CLI reproduction verifies that it omits the absolute input location.
- Low, patched. The contributor guide gives the focused story-scope test command.
- Low, patched. The contributor guide gives the pinned commitlint prerequisite of Node.js 22.12 or later and distinguishes the current npm installation from retired semantic-release commands.
- Low, patched. The owner-discovery fixture verifies that a valid branch owner and a different valid trailer owner fail together.

## File Scope

Allowed files for this story:

- `.githooks/commit-msg`
- `.githooks/pre-commit`
- `.githooks/python-interpreter.sh`
- `CONTRIBUTING.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-ten-deferred-story-scope-hooks.md`
- `tests/tooling/commitlint/commitlint_entrypoints_test.py`
- `tests/tooling/story_scope/story_scope_paths_test.py`
- `tools/check-story-file-scope.py`
- `tools/check-story-review-readiness.py`
- `tools/check-tenant-isolation-evidence.py`
- `tools/story_git_paths.py`
