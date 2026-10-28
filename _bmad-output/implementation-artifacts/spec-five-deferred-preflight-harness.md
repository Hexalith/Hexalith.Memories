---
title: 'Close five deferred release preflight harness gaps'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close DW-61, DW-62, DW-63, DW-64 and DW-65 as one bounded release-preflight test-harness correction. Given transient permission errors while removing a disposable fixture, when cleanup runs, then retry within a small fixed bound and propagate persistent failures; never silently leave resources behind. Given Python 3.9, when this test module is imported, then its annotations do not require Python 3.10 union evaluation. Given non-ASCII or malformed subprocess output, when the harness captures Git or PowerShell diagnostics, then decode UTF-8 deterministically and retain readable diagnostics without a decoding exception.

Given an ambient Git default branch other than main, when a working or bare fixture is initialized, then its initial branch is main. Given no PowerShell 7 executable, when unittest collects the preflight execution class, then report an explicit prerequisite skip before constructing fixtures; with PowerShell installed, execute all existing preflight assertions. Add meaningful isolated regressions and retain positive and exhaustion controls. Resolve exactly these five ledger entries with evidence and reopen triggers. Independently review, commit and push the completed changes on main as requested.

</frozen-after-approval>

## Implementation Notes

- Route assessment: no intent gaps, public API, dependency change, application code, topology change or irreversible implementation action. User explicitly authorizes main commit/push. This test-only harness does not exercise application startup; no Aspire lifecycle operation is needed.
- Baseline: clean main at ae7786eaaa6919de37d7d65b90e418a47dc446c2. Read the declared AI.Tools baseline/Git guidance, repository agent instructions, EditorConfig/attributes, build settings, contributor/test guidance, project context, architecture D17 and the existing preflight spec/test helpers. Treat current repository configuration as authoritative over stale version prose.
- Code map: tests/tooling/release_preflight/release_preflight_test.py owns all temporary directories, three subprocess capture paths, _init_repo and bare fixture initialization. Keep its existing production-script assertions. Add tests/tooling/release_preflight/preflight_harness_test.py for deterministic cleanup faults, real subprocess byte capture, ambient branch configuration and prerequisite discovery. Reuse the existing _environment sanitization so Git hook routing never leaks into disposable repositories.
- Keep cleanup retries visible as ordinary successful idempotent removal; rethrow the final PermissionError after three attempts. Use Optional annotations rather than expanding the repository-wide Python support contract. Document the harness prerequisites and cleanup behavior in tests/README.md.
- Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' -q; run the focused helper cases first and then the complete suite. Check annotation collection on Python 3.9 if an interpreter can be provisioned without repository dependency changes. Run line-ending and explicit file-scope checks and commitlint before/after committing. No .NET source/build dependency changed.

- Baseline focused execution suite passed 36/36 tests in 117.210 seconds. New regressions first reproduced the missing cleanup/fixture helpers, absent prerequisite guard and UnicodeDecodeError on all three capture paths; initial Git capture correction was caught by its regression before the complete run.
- Implemented bounded cleanup, Optional annotations, explicit UTF-8 replacement decoding, explicit main for working/bare fixtures and PowerShell prerequisite detection. Seven focused harness regressions passed on CPython 3.14.4 (0.093 seconds) and CPython 3.9.25 (0.066 seconds). An actual Python 3.9 module import plus typing.get_type_hints of all four subprocess helpers passed. The interpreter was provisioned outside the repository with uv; no tracked dependency changed. Windows lock behavior is fault-injected; no native Windows execution is claimed.
- Updated tests/README.md with the exact focused command, prerequisites, skips-as-non-evidence rule and bounded cleanup behavior. Resolved only DW-61 through DW-65 using focused evidence and explicit reopen triggers. Line-ending policy passed 4/4 tests. The complete 43-test suite is running. A CPython 3.9 execution of the exact baseline source reproduced its annotation TypeError, confirming the compatibility fix covers an observed failure.

- Complete expanded suite passed 43/43 tests in 122.480 seconds (exit 0), with PowerShell present and no skipped tests. Explicit file-scope validation passed without warnings. Tenant-isolation and review-readiness gates report no affected surface and no governed ledger respectively for this bounded standalone spec. Exactly DW-61 through DW-65 changed in the deferred ledger; fetched origin/main had zero divergence before commit preparation. Independent blind review is pending.

- Independent review returned five findings. Patched exact sleep-delay assertions and a detected-PowerShell positive control; added a maintained annotation-evaluation smoke test and documented its Python 3.9 command. Nine focused cases now pass on both CPython 3.14 and 3.9. Clarified that existing fake npm executable fixtures require a POSIX host; no complete native Windows suite support is claimed. The reviewer-confirmed missing PR enrollment and absence of a routine lower-runtime lane predate this harness correction and are separately deferred rather than changing release workflow scope.

- Final post-review complete suite passed 45/45 tests in 107.383 seconds (exit 0, no skips). The independent reviewer reran nine helper cases on Python 3.14 and Python 3.9 with warnings treated as errors, confirmed both the ten-second-delay and always-skipped-class mutations now fail, and reported no unresolved issues in the patches. Final lower-runtime run also passed 9/9 in 0.068 seconds; line endings passed 4/4 and whitespace checks passed. Conventional Commit message passed commitlint before commit preparation. Exactly five original deferred entries are resolved; two pre-existing workflow enrollment gaps are recorded as follow-ups. This standalone oneshot spec adds no sprint-status row.

## Review Triage Log

- Low, patched: cleanup regressions counted retries but did not constrain delays; assert exactly 50 ms and 100 ms in both recovery and exhaustion paths. This rejects the reviewed ten-second-delay mutation.
- Medium, patched: absence-only PowerShell testing allowed an always-skipped class mutation; the new available-executable control proves one test body executes with zero skips.
- Medium, deferred: the actual Python 3.9 import/annotation and helper checks pass, and a maintained annotation smoke case plus exact lower-interpreter command now exist. The default-interpreter release workflow still cannot detect all lower-runtime regressions; routine Python 3.9 enrollment is a separate workflow follow-up.
- Medium, deferred: .github/workflows/release.yml runs preflight fixtures on main pushes, while ci.yml omits them for PRs. This predates the five selected fixes. Record separate PR enrollment work with its missing-prerequisite policy and a deliberately failing-fixture acceptance check.
- Low, patched: the new prerequisite documentation omitted an existing POSIX fake-npm restriction; document the complete-suite host requirement rather than implying native Windows qualification from fault-injected cleanup tests.

## File Scope

Allowed files for this story:

- `tests/tooling/release_preflight/release_preflight_test.py`
- `tests/tooling/release_preflight/preflight_harness_test.py`
- `tests/README.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-preflight-harness.md`
