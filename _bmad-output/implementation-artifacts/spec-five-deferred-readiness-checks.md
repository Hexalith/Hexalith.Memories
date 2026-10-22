---
title: 'Close five deferred story readiness checks'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close DW-604, DW-605, DW-606, DW-611 and DW-614 in the deferred-work register. The story readiness checker currently permits truncated evidence rows, stale File List paths, wildcard inventories, missing File Lists on otherwise governed artifacts, and whitespace-only exclusion owners. Enforce the existing story-phase-ledger policy with focused CLI regressions.

Given a review/done evidence table, when a row omits its review-status cell, then C6 fails with a row-specific diagnostic. Given an in-progress/review artifact carrying a ledger or evidence table, when its File List is missing or empty, then C1 fails. Given a supplied cumulative changed set, when File List entries and nonexcluded changed paths differ, then C1 reports both missing and stale exact paths; wildcard text cannot cover other paths. Given an exclusion, when its owner contains only whitespace, then validation fails. Exact inventories and named exclusions continue to pass.

Preserve the documented default-branch cumulative-diff skip, the no-op for artifacts with no governed sections, status vocabularies, and evidence checks below review. Leave unrelated deferred work and both pre-existing Spec 14/report edits untouched. The user authorized all five items and committing/pushing this work on main; no new runtime campaign is part of this work.

</frozen-after-approval>

## Implementation Notes

- Investigation: no unresolved user intent; bounded edits to the existing checker, its tests, this trace and five register dispositions. No new API, dependency, deployment, or data mutation. Commit/push is explicitly requested.
- Code map: `tools/check-story-review-readiness.py` owns `parse_exclusions`, `check_evidence_rows` and `validate` C1. Reuse path normalization, exact exclusion paths, status gates, and cumulative-diff derivation. Remove the now-unused glob matcher after C1 uses exact membership.
- Tests: `tests/tooling/story_review_readiness/story_review_readiness_test.py` provides subprocess fixtures and live-artifact liveness guards. Baseline command: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` — 45/45 methods passed in 3.285s. Add negative CLI cases and positive compatibility controls without changing the known live C6 inventory.
- Policy: `_bmad/custom/story-phase-ledger.md` requires identical cumulative file sets and a named exclusion owner. Normal one-shot non-epic work has no sprint-status row. This is repository tooling only; no application host execution is needed.
- Baseline revision: `779db8694087c4459174429287ee07c1caff1a84`. User approved preserving/excluding the two pre-existing dirty files. All implementation and review records will be appended here.

- Implemented the five refusal paths and removed unused glob matching. Added 12 test methods (45 -> 57 comparable methods), including exact normalized inventories, named exclusions, exclusion/credit conflicts, earlier-status evidence, ungoverned prose and inactive-status compatibility. Contributor documentation now explains symmetric exact reconciliation and blank-owner refusal.
- Final implementation command: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` — 57/57 methods passed in 5.278s, exit 0. Existing live C6 inventory is unchanged. Five selected regression methods were also run against the original checker from baseline `779db8694087c4459174429287ee07c1caff1a84`: all five fail as intended (16 negative cases), without test-runner errors. Temporary baseline tool was removed automatically.
- Updated only the five selected register dispositions with regression evidence and reopen triggers. No tenant routing, authorization, storage or runtime behavior changes; negative tenant evidence is not implicated.

- Independent review patched three edge cases: evidence header width now catches an omitted middle cell; separator-shaped data rows stay visible after the actual header delimiter; exclusion syntax validates even when the default-branch cumulative comparison skips. Added a literal-bracket filename control and disposable Git repository coverage for committed/staged/supplied cumulative paths and the main-branch skip. No named project repository or user Git state is mutated by those fixtures.
- Final focused verification after review: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` — 62/62 methods passed in 4.176s, exit 0. Comparable discovery is 45 -> 62, delta +17 methods. Known live evidence violations remain the same two artifacts. `git diff --check` passes. Original five register resolutions now cite this final suite result.

## Review Triage Log

- Medium, patched: a missing middle evidence cell can shift a date into the status position. Header width is now retained and truncated rows fail C6; the expanded review/done regression executes that reproduction.
- Medium, patched: delimiter-shaped data such as `| - |` was discarded after header parsing. Only the actual header delimiter is consumed; truncated and full placeholder rows now fail C6 under review/done.
- Medium, patched: a main-branch cumulative comparison skip bypassed exclusion syntax. Exclusions now parse before comparison routing, and a disposable main-repository fixture proves a blank owner fails while valid default-branch comparison still skips.
- Low, patched: cumulative reconciliation lacked execution coverage across history and index. Disposable Git fixtures now prove exact agreement, each missing source, and stale entries against committed, staged, and supplied paths.
- Low, patched: wildcard rejection lacked a literal-metacharacter positive control. An identical `src/literal[ab].cs` path in both sets now has a passing regression.
- Low, rejected: the optional historical original-checker probe was not retained as a separate executable invocation. The exact retained-suite command is recorded, and the five selected original-checker methods and canonical baseline revision are recorded in the referenced register resolutions. Maintainers normally reproduce via the retained suite; preserving a separate comparison runner would add more than a simple correction for rarely used supplementary evidence.
