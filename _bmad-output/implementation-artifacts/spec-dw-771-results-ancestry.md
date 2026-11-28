---
title: 'Protect active test runs with overlapping results directories'
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

DW-771 addresses the flat deferred entry titled “Protect active test runs when results directories overlap by ancestry.” A runner that owns an ancestor directory can recursively clear a descendant while it is leased by another run. Coordinate the POSIX report wrappers so neither start order damages the active run's directory or output.

## Boundaries & Constraints

Preserve existing ownership checks, descriptor-relative preparation, same-directory rejection, staging, publication, and automatic lease recovery. Keep disjoint destinations independently usable. Use disposable workspaces via `run-with-temp` and keep native Windows behavior intact. Check whether existing native Windows handle-share behavior already protects both orders and exercise native or mixed fixtures where applicable. Product changes, the ledger, and Git mutations belong to the parent agent's workflow.

</frozen-after-approval>

## Code Map

- `tools/prepare-test-results.py` opens checkout and directory descriptors, leases and cleans the selected destination, and publishes staging. POSIX Bash and PowerShell share this path.
- `tools/prepare-test-results.ps1` owns the native Windows handle-relative path.
- `tests/tooling/integration_fast_coverage/test_runner_test.py` exercises race control with fake dotnet invocations.

## Tasks & Acceptance

- [x] `tools/prepare-test-results.py` — guard the directory walk and cleanup against active ancestor/descendant overlap.
- [x] `tests/tooling/integration_fast_coverage/test_runner_test.py` — prove refusal before damage in both start orders and that disjoint runs can progress.

Given a running ancestor or descendant, when the other wrapper selects the overlapping path, then it refuses before destructive preparation and the active run completes and publishes its reports. Given two disjoint destinations, when runs overlap, both can complete.

## Implementation Notes

POSIX holds shared ancestor and exclusive destination locks on the opened descriptors. Five existing native Windows fixtures passed. Separate NTFS probes reproduced native and Windows/WSL overlap gaps, including deletion of a sentinel before refusal. Record a separate follow-up for native and mixed execution.

## Spec Change Log

## Review Triage Log

The native Windows and mixed Windows/WSL overlap reproduction is a separate open issue under DW-780. This resolution covers the POSIX helpers.

## Verification

Run the focused test runner suite, including existing cleanup, publication, interruption, and same-destination regressions. Reproduce the original failure in an isolated copy if practical.

The baseline failed both overlap orders. The POSIX runner suite passed 27 tests with two Windows-only skips. The two separately executed native runner cases and three native publication cases passed. The changed POSIX behavior also passed both overlap orders on NTFS under WSL.

## Batch verification

The final Release solution build at `ef169af067a241dde1591f23edfc5d278660b107` produced zero warnings and errors. The unfiltered run across all 12 inventoried .NET projects passed 6,701 tests with 5 existing skips and zero failures. The final fast evidence catalog accepted the exact trace and predecessor results. The Chromium type check and all six browser tests passed, and artifact validation accepted nine outputs.

## File List

- `tests/tooling/integration_fast_coverage/test_runner_test.py`
- `tools/prepare-test-results.py`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-28 | create-story | Scoped the verified deferred item | Focused checks planned | Recorded affected surfaces |
| 2026-11-28 | dev-story | `b37b7ca87aa4d8b38c1ff45c2eef0f78fadc1ffb` | 27 POSIX passes and 5 native Windows checks | Matched 2/2 |
| 2026-11-28 | code-review | `b37b7ca87aa4d8b38c1ff45c2eef0f78fadc1ffb` | 27 POSIX passes and 5 native Windows checks | Matched 2/2 |
