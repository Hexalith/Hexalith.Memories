---
title: 'Close five deferred commit validation gaps'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close DW-600, DW-616, DW-617, DW-618 and DW-620 through the existing Conventional Commit workflow. Given a missing local commitlint executable, when the commit-msg hook runs, then reject the commit with an npm ci recovery instruction. Given a malformed, merge-shaped, revert-shaped or version-only message, when the pinned policy validates it, then enforce the same type/subject rules as ordinary messages. Keep valid Conventional Commits, including chore and properly formatted reverts, working.

Given overlapping main pushes, when commitlint concurrency groups are resolved, then each push retains its own validation run, including while queued. Given pull requests and title edits, when the existing reusable workflow is called, then preserve title forwarding and validation triggers. Retain executable negative and positive tests of the pinned policy and real hook, plus caller configuration checks; isolate unrelated hook gates in disposable repositories.

Given historical commit 358bef352bf657bcd2a7c4e91cb0a6d298c3f7a4, when its omitted source change is documented, then record the expiry-order correction and its test-adapter scope accurately in durable release documentation. Resolve exactly the five selected entries with evidence. Independently review, commit and push the completed work on main as requested.

</frozen-after-approval>

## Implementation Notes

- Route assessment: one bounded developer-workflow correction; no unresolved intent, new dependency, runtime API, data mutation or deployment. Commit and push are explicitly authorized. No AppHost or application change; application startup is unrelated to the affected checks.
- Baseline: clean main and fetched origin/main both at cfc9aef4935aea7a0059d905c76225c9d5fa4e91. Read shared repository guidance, declared AI.Tools baseline/Git instructions, EditorConfig/attributes, build configuration, contributor/testing guidance and architecture constraints. No submodule changes.
- Code map: .githooks/commit-msg currently warns and continues without commitlint; commitlint.config.mjs retains built-in ignores; .github/workflows/commitlint.yml cancels all runs sharing a ref. The caller already handles PR title edits and forwards the title to Hexalith.Builds. Extend tests/tooling/commitlint using installed repository-pinned Node tooling and disposable repositories; no additional dependency. Update CONTRIBUTING.md and docs/dev/release-runbook.md for policy and the omitted historical note.
- Main push groups will include github.run_id to protect queued as well as running ranges. PR runs may continue superseding earlier runs for the same ref. Locally validate configuration and expression contract; GitHub scheduling itself is not executed locally.
- Baseline: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/commitlint -p '*_test.py' -v passed 3/3 methods, exit 0. Historical git show confirms the omitted source change adds ExpiresAtUtc ordering after ExpiryMinute to the in-memory lifecycle test adapter.

- Added regression fixtures before the fixes: 11 methods yielded 23 failing subcases against original policy/hook/workflow behavior, confirming ignored shapes, missing CLI and unsafe concurrency. The retained positive controls remained valid. After implementation, all 11 methods passed in 8.272s (exit 0), compared with the original three methods (+8 methods, plus parameterized message matrices).
- Implemented fail-closed local dependency handling, disabled default message ignores, separated push concurrency groups by run ID and preserved PR supersession. Contributor guidance describes the recovery and policy; the historical release note records the exact omitted in-memory ordering change without a Production claim. Resolved exactly DW-600/616/617/618/620 with focused evidence and reopen conditions.
- Actionlint, hook shell syntax and git diff --check passed. Independent review and compatibility checks follow.

- Hook compatibility: readiness 104/104 methods passed in 20.574s; story scope 71/71 in 5.132s; tenant evidence 41/41 in 2.831s; line-ending policy 4/4 in 0.838s. Each used PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/<directory> -p '*_test.py', exit 0. These verify the hook's downstream contracts and repository formatting; no application code was changed.



- Review patches strip inherited Git routing/index/config variables from disposable commands and prove an enclosing temporary repository's config/index remain unchanged. A mutation removing that isolation produces an expected Git add routing error in the retained regression. Hook copies preserve source permissions, successful controls require all three specific validators in order, and a real-hook conventional revert with its full body is accepted.
- Final focused suite passed 13/13 methods (exit 0); comparable discovery 3 -> 13 (+10 methods). Independent follow-up reran all 13 and confirmed all four patched findings resolved. Exactly the five selected dispositions cite this final result. One independently confirmed pre-existing empty-message bypass is deferred; no configured review layer was skipped.

## Review Triage Log

- Medium, patched: inherited GIT_DIR/worktree/index/config variables could redirect fixture commands into the launching repository. Commands now strip inherited GIT_*; a hostile-parent regression retains outer config/index bytes and validates the intended fixture repository.
- Medium, patched: forcing chmod on copied hooks masked a lost executable bit in the source. copy2 now preserves source mode; a nonexecuting hook cannot satisfy the retained downstream positive control.
- Low, patched: counting three downstream log lines did not identify the validators. Positive controls now require each expected validator name in order.
- Low, patched: the real-hook positive fixture omitted the explicitly preserved conventional revert body. A dedicated successful commit now includes the conventional revert header, This reverts commit body and owner trailer.
- Medium, deferred: explicit --allow-empty-message accepts empty subjects because the pinned CLI filters empty repository inputs before linting. Independently reproduced with exit 0 and empty HEAD subject; the CLI filter predates this change, while ordinary Git rejects empty messages. Appended a follow-up for local and shared-CI handling without expanding the five-item scope. The policy comment now describes the actual merge/revert/version behavior rather than universal validation.

## File Scope

Allowed files for this story:

- `.githooks/commit-msg`
- `commitlint.config.mjs`
- `.github/workflows/commitlint.yml`
- `tests/tooling/commitlint/commitlint_config_test.py`
- `tests/tooling/commitlint/commitlint_entrypoints_test.py`
- `CONTRIBUTING.md`
- `docs/dev/release-runbook.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-commit-validation.md`
