---
title: 'Close five deferred release preflight gaps'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close DW-540, DW-59, DW-69, DW-70 and DW-71 in the existing release preflight workflow. Given a repository with no previous release, when semantic-release reports its exact alternate first-release version marker, then parse the version and perform the same exact local/remote tag collision checks as later releases. Given malformed, conflicting or payload-only terminal records, when preflight parses them, then retain fail-closed rejection and logger provenance rules.

Given an invalid repository directory, when preflight starts, then fail with an actionable RepositoryPath diagnostic. Given a failed local ref-state probe, when Git exits with an unexpected code such as 128, then fail with the queried ref, recovery direction and original failure details before a remote probe. Given an exception with an inner cause, when preflight reports it, then retain that cause and the PowerShell script stack. Given only a peeled exact tag ref from the remote, when collision checks run, then reject it; a similarly prefixed peeled ref must pass.

Retain executable regressions and positive controls using disposable repositories and controlled command responses; do not publish packages or create external resources. Resolve exactly these five ledger entries with verification evidence and reopen triggers. Independently review, commit and push completed changes on main as requested.

</frozen-after-approval>

## Implementation Notes

- Route assessment: one bounded release-preflight robustness correction with no intent gaps, new dependency, public API or irreversible implementation action. The user explicitly authorizes committing and pushing main. No application/AppHost code or topology changes; application startup does not exercise this tool.
- Baseline: clean main at 46692dd0f6c7cd780608ce74fd827d81c481ae6d; fetched origin/main has no divergence. Read AGENTS.md, declared AI.Tools baseline/Git instructions, EditorConfig/attributes, build configuration, contributor/test guidance, project context and relevant architecture constraints. No submodule edits.
- Code map: tools/release-preflight.ps1 owns logger-scoped terminal parsing, local/remote ref checks, path resolution and final error reporting. Extend its existing tests/tooling/release_preflight/release_preflight_test.py suite. The installed, pinned semantic-release 25.0.8 lib/get-next-version.js confirms the alternate first-release marker. Keep exact tag matching and all existing parser rejection rules.
- Test command: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py'. Add controlled PowerShell command fixtures for ref errors, nested exceptions and peeled-only replies; retain actual Git repositories for successful and ordinary collision checks. Keep injected command behavior confined to test wrappers.

- Original focused suite passed 26/26 methods (72.295s, exit 0). New regressions reproduced first-release failures (plain, logger and ANSI records), missing/file RepositoryPath failures, loss of nested exception detail and generic local ref-probe errors before changing production code. The peeled-only regression already passes for exact collision rejection and similarly prefixed acceptance; it closes missing coverage rather than changing correct behavior.
- Implemented the alternate exact marker within the existing logger/provenance parser, directory validation before Resolve-Path, an exception-preserving local ref-state diagnostic and full exception/script stack output. Test-only PowerShell wrappers supply controlled Git errors and replies; actual Git still initializes repositories and performs non-intercepted commands. Test subprocesses strip inherited GIT_* routing variables so hook invocations cannot redirect disposable repositories.

- The first expanded run executed 35 methods and caught one defect in the new diagnostic: wrapping the exception retained its inner cause but replaced its original PowerShell stack. Corrected it to print the local-ref context and use bare rethrow. Both targeted error-path regressions then passed (2/2, 5.151s, exit 0); the complete suite is rerunning against the corrected source. Strengthened the ref-error fixture to emit a captured PowerShell error record rather than bypassing capture via Console.Error.
- Offline semantic-release configuration verification and its fail-closed self-tests passed via npm run verify:semantic-release-config. Line-ending policy suite passed 4/4; explicit story file-scope validation passed for all five planned paths. No application code changed, so no .NET/application build or deployment was needed for these script-only behaviors.

- Corrected expanded suite passed 35/35 methods (114.938s, exit 0) before review patches. Review tightened remote-probe reachability, restored both bare-marker payload controls, added payload-only live failures, covered existing non-Git directories and corrected malformed first-release guidance. The final suite has 36 methods (+10 from baseline), with additional parameterized inputs.

- Independent follow-up confirmed review findings 1–5 resolved and reran all six affected methods successfully (five in 30.064s, payload-only in 4.952s, exit 0). Its evidence check caught a missing assertion for the documented diagnostic forms; added explicit assertions for both accepted forms and ran that focused method separately. The complete 36-method run and this strengthened assertion run supply final verification.

- Final complete suite passed 36/36 methods in 120.463s (exit 0), versus 26 at baseline (+10 methods, plus parameterized cases). The strengthened diagnostic method passed all four inputs in 10.375s (exit 0). Independent review and follow-up completed; exactly DW-540/59/69/70/71 are closed with evidence and reopen triggers, and one pre-existing SemVer issue is separately deferred. Offline configuration verification, line-ending tests, file-scope validation and diff whitespace checks passed. Tenant evidence and readiness gates correctly report no affected surface/no governed ledger for this bounded oneshot artifact.

## Review Triage Log

- Medium, patched: error fixtures had no origin, so an absent ls-remote sentinel did not prove early termination. Both failure fixtures now configure origin; reaching the remote check necessarily records the sentinel and fails the test.
- Medium, patched: parameterizing the live-output positive test replaced its ordinary bare payload with a first-release payload. It now tests both payload forms against both genuine logger marker forms.
- Medium, patched: first-release provenance coverage lacked payload-only failures. Live npm fixtures now supply only a bare or plugin-logged first-release record; both must fail as unparseable before the matching local tag can be reported as a collision.
- Medium, patched: an existing non-Git directory lacked the same RepositoryPath recovery context as missing/file paths. Initial Git repository validation now emits that context and bare-rethrows the original error; a third input case covers it.
- Low, patched: malformed-marker guidance omitted the supported first-release syntax. It now names both forms, with a regression assertion.
- Medium, deferred: the inherited SemVer grammar accepts invalid leading-zero 01.0.0 and rejects valid combined prerelease/build 1.0.0-beta.1+build.1. Independent execution of baseline 46692dd0f6c7cd780608ce74fd827d81c481ae6d and changed source gives identical 0/1 exits for ordinary records and direct NextVersion input. A separate deferred entry records consistent SemVer validation without expanding this five-item preflight correction.

## File Scope

Allowed files for this story:

- `tools/release-preflight.ps1`
- `tests/tooling/release_preflight/release_preflight_test.py`
- `docs/dev/release-runbook.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-release-preflight.md`
