---
title: 'Preserve PowerShell test failure details when reports are missing'
type: 'bugfix'
created: '2026-11-28'
status: 'done'
route: 'dispatch'
baseline_commit: '0124b494967373f8458620d904940bc0a7ccdf1a'
review_loop_iteration: 0
context: ['{project-root}/AGENTS.md', '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md']
---

<frozen-after-approval reason="human-owned intent">

## Intent

DW-772 addresses “Preserve PowerShell test failures when TRX or Cobertura output is missing.” Preserve the primary native dotnet exit failure and available report diagnostics. A successful run still validates every required report.

## Boundaries & Constraints

Follow the existing Bash DW-743 contract where applicable and preserve PowerShell project selection, filtering, report host behavior, benchmark counts and staged publication. Use run-with-temp for scratch workspaces. Do not commit or edit the ledger/spec. Another implementer owns the runner ancestry regression. Coordinate any write to the shared Python fixture with the parent before editing it.

</frozen-after-approval>

## Code Map

- `tools/test.ps1` captures dotnet status and owns report discovery and validation.
- `tools/test.sh` contains the equivalent behavior implemented for DW-743.
- `tests/tooling/integration_fast_coverage/test_runner_test.py` contains fake-dotnet runner fixtures.

## Tasks & Acceptance

- [x] Preserve native failure details and available report evidence. Add focused coverage for the missing or invalid report cases.

Given native failure with absent TRX or coverage, the primary error identifies the dotnet exit status. Given an available report, retain its useful diagnostics and preserve staged publication. Given exit zero and invalid or missing required reports, still fail validation.

## Implementation Notes

## Spec Change Log

## Review Triage Log

The review fixes bind Cobertura to its CLR XML root and require actual TRX attributes. The runner refuses explicit native-zero TRX failures. The coverage gate owns semantic line-coverage checks.

## Verification

Run the PowerShell runner cases, including coverage and benchmark report contract cases. Check native Windows if readily available without assuming that POSIX is native Windows.

The baseline masked native exit 37 when both reports were absent and when a report parse failed. The focused runner command passed nine tests. Five independent native Windows 7.7.0 cases passed on NTFS, covering missing reports, partial publication, malformed reports and the benchmark contract.

## File List

- `tests/tooling/integration_fast_coverage/test_runner_test.py`
- `tools/test.ps1`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-28 | create-story | Scoped the verified deferred item | Focused checks planned | Recorded affected surfaces |
| 2026-11-28 | dev-story | `7073b4ea5767cfc010409f07296b87223c17294e` | 52 tooling passes and 7 native smoke cases, then 4 focused follow-up tests and 4 native scenarios | Matched 2/2 |
| 2026-11-28 | code-review | `453c7b0a5e41468b3e04820b2aa5fbcbd97a73df`, `10cc6518188e715c66a527fddda4f4cef2ff0661` | 52 tooling passes and 7 native smoke cases, then 4 focused follow-up tests and 4 native scenarios | Matched 2/2 |
