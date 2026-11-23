---
title: 'Close five deferred integration tooling gaps'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make the existing integration test tooling easier to run locally and diagnose. Close DW-311, DW-312, DW-316, DW-323 and DW-327 and record the evidence in the deferred-work ledger.

- Given Bash TRX collection and no `python3`, when the test runner starts, then it reports the prerequisite before removing previous results or launching tests. Runs without a results directory still work without Python.
- Given a TRX report with the standard namespace or no namespace, when the Bash wrapper or coverage verifier reads it, then it finds the expected XML elements without Python 3.8 wildcard XPath.
- Given mapped passed tests, when the verifier runs, then it exits 0 for complete evidence or 1 for missing surfaces. Invalid or unreadable inputs exit 2, and runs without usable results exit 3.
- Given a developer without Docker, when they read the contributor guide, then they can select the Docker-free lane and know that Docker-backed integration needs a running engine.
- Given fast, slow, performance, or all-integration selectors, when the Bash and PowerShell wrappers run, then both choose the documented inventories. Exercise routing with an isolated `dotnet` fixture.

</frozen-after-approval>

## Implementation Notes

- Baseline `4e3a4d99548e7bdc394b78f5e74f91ab152ec86b`. The scoped files are `tools/test.sh`, `tools/verify-integration-fast-coverage.py`, `tests/tooling/integration_fast_coverage/`, `CONTRIBUTING.md` and the deferred-work ledger. Existing PowerShell routing is the parity control. The tooling changes require no application launch, external service, deployment or data change.
- The Bash preflight starts Python and imports ElementTree before replacing previous reports. Verification accepts standard or namespace-free TRX and reads only result and definition elements in their expected sections. Directory scanning propagates access errors. CONTRIBUTING.md documents Docker lane selection, the Bash prerequisite and the verifier's exit codes.
- The Bash and PowerShell wrappers already selected the documented projects. The new regression runs both entry points with an isolated `dotnet` fixture and checks the full project and MTP trait arguments.
- Validation passed 13 integration-tooling tests and 26 coverage-gate tests under Python 3.14, plus nine verifier tests under Python 3.9. Three new preflight and CLI tests failed on the baseline with the expected old exit codes. A retained Contracts TRX had 599 executed tests and retained the same 310 mapped passing method identities as the baseline verifier. A real unreadable subtree returned exit 2. `bash -n tools/test.sh` and `git diff --check` passed. Docker-backed integration execution was outside this tooling check.

## Review Triage Log

- High, deferred. The existing Bash results-directory guard accepts a dot-only path that targets the checkout root. The reviewer reproduced it in a disposable checkout and recorded the finding in the ledger.
- Medium, patched. An executable shim with a missing interpreter bypassed the first preflight. The runner now verifies Python starts and imports ElementTree. The fixture covers missing and broken interpreters before cleanup or a test launch.
- Medium, patched. `Path.rglob` could suppress scan errors and silently omit evidence. Traversal now propagates errors and a regression rejects an unreadable subtree even beside valid evidence.
- Medium, patched. Namespace and descendant matching accepted unrelated XML as test evidence. The verifier now accepts the TRX namespace or no namespace and scopes definitions and results to their respective sections.
- Medium, deferred. The Bash runner already loses the native test exit code if a failing process produces no TRX. The ledger records the follow-up.
- Medium, patched. The test fixture used to forward unexpected SDK calls. PowerShell now starts with the normal host path and installs the fixture path inside the shell. The fixture rejects non-test SDK invocations.
- Medium, patched. Project-only assertions could miss a trait-filter regression. Both wrappers now have executable checks of the full arguments.

## File Scope

Allowed files for this story:

- `CONTRIBUTING.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-integration-tooling.md`
- `tests/tooling/integration_fast_coverage/integration_fast_coverage_test.py`
- `tests/tooling/integration_fast_coverage/test_runner_test.py`
- `tools/test.sh`
- `tools/verify-integration-fast-coverage.py`
