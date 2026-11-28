---
title: 'Complete deferred test execution and verification follow-ups'
type: 'bugfix'
created: '2026-11-27'
status: 'done'
route: 'dispatch'
baseline_commit: 'a67375fef3a6b8acc0c910b071f1844ac249da21'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete ten eligible deferred items within test execution and verification reliability. A verified risk of unintended PowerShell report cleanup selects this parent, which includes wrapper safety, disposable runtime setup, fixture proof, and local verification checks. Assign an implementer to each item, verify it, and make its own Conventional Commit with the DW ID.

## Boundaries & Constraints

Use disposable directories, process trees, trust material, and local resources for reproduction and proof. Preserve existing test selection, assertions, and supported caller behavior. Preserve historical evidence and respect deployment and hosted CI ownership. Skip entries whose recorded prerequisites are unavailable or whose implementation is already present. Use existing ledger notes and record fresh dispositions as applicable.

</frozen-after-approval>

## Code Map

- `tools/test.ps1`, `tools/test.sh`, and `tests/tooling/integration_fast_coverage/` own report preparation and Dapr bootstrap fixtures.
- `tools/verify-access-telemetry-c1-linkage.ps1` and `tests/tooling/access_telemetry_c1/` own post-observation verification.
- `tools/check-story-review-readiness.py` and its tooling tests own local review-readiness checks.
- Server and integration test projects own token, TLS, and live timeout proof.

## Tasks & Acceptance

- [x] Given a redirected or contested report destination, when either wrapper starts, then unrelated files and an active run's output survive.
- [x] Given a stalled bootstrap command, when its budget expires, then the setup lane exits boundedly and reaps owned work.
- [x] Given a stale capture, blocked lookup, or unavailable TLS evidence, when verification runs, then the owning test proves the exact expected behavior.
- [x] Given a numbered spec or BMAD review status, when the readiness check runs, then it evaluates the right artifact and applies the required gates.
- [x] Given an inherited token, when fixture hosts run, then token configuration follows each environment's existing contract.
- [x] Given ten verified item commits, when the full solution and test inventory pass, then close their ledger entries and push the separate commits to main.

## Implementation Notes

The first candidates are existing flat deferred entries. Assign IDs sequentially after DW-760 and preserve their original provenance at closure.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Disposition and evidence |
| --- | --- | --- |
| Blind 1 | medium | Deferred. The POSIX fixture reproduced an ancestor invocation clearing an active descendant. DW-763 covers contenders for the same directory. |
| Blind 2 | high | Patch. A native NTFS reproduction showed recursive Copy-Item overwrites an unrelated receipt through an unexpected project junction. |
| Blind 3 | medium | Deferred. A failing native invocation with no report reproduces the baseline missing-report diagnostic. This batch preserves the PowerShell exit contract. |
| Blind 4 | medium | Patch. The Python supervisor allocates staging and does not handle graceful signals while waiting for the native command. |
| Blind 5 | medium | Patch. A stalled final version command remained active after both readiness probes succeeded. |
| Blind 6 | medium | Deferred. The existing cleanup suppresses ordinary nonzero returns. Reliable Docker error classification requires a separate change. |
| Blind 7 | low | Declined. The tests verify process groups stop executing and the script waits for its direct jobs. Child process-table reaping is managed by the host after the launcher exits. |
| Blind 8 | high | Patch. CLI evidence reproduced success for an in-review spec with no File List or phase ledger. |
| Blind 9 | false | The exact spec key deliberately pins a standalone artifact. The original ledger asks for an active numbered revision when invoked with its sprint key. |
| Blind 10 | low | The disposable live Dapr 1.19.1 proof ran and its boundaries are recorded in the DW-766 evidence. The durable regression isolates cancellation at the typed HTTP request. |
| Blind 11 | medium | Patch. The manual inherited-token run succeeded. The existing automated EventStore factory tests do not set a controlled inherited token. |
| Edge 1 | medium | Deferred with Blind 1. Two independently leased directories can overlap through ancestry. |
| Edge 2 | medium | Patched with Blind 5. The final Dapr version call needed a bound. |
| Edge 3 | high | Patched with Blind 8. The early no-op accepted status-only in-review input. |
| Edge 4 | medium | Deferred with Blind 3. The missing reports hide the native failure in the existing PowerShell contract. |
| Verification 1 | medium | Patch. A cleanup mutant passed the old Dapr suite and left four ready-probe fixture processes running. Add their deadline-specific assertion. |
| Verification 2 | medium | Patch with Blind 11. Add the scoped inherited-token assertion through the actual Development EventStore host. |
| Verification 3 | medium | Deferred. Native verification ran locally. Hosted enrollment requires Hexalith.CiCd. |
| Verification 4 | medium | Patch fixture cleanup. The surviving test harness can own staging left by its intentional SIGKILL. The production graceful-signal path is addressed under Blind 4. |

## Verification

- The Release solution build passed on `0a19b53fdca6ca8bc16c14912b45e920dd06704c` with zero warnings and errors.
- All 12 .NET test projects passed without test selection filters. The TRX files recorded 6,688 passes, five existing skips, and zero failures. The totals include 732 fast integration tests, 60 nightly integration tests, and 17 benchmarks.
- The Chromium type check and all six tests passed. Artifact validation accepted nine outputs.
- Across 26 tooling groups, 1,625 cases passed and five platform-dependent cases skipped. Native Windows exercised the cleanup, destination-lease, and report-publication regressions separately. macOS PowerShell runtime coverage was unavailable.
- A separate check of the parked access-telemetry lifecycle lane under DW-730 reproduced the same 36 failures and 42 errors as the starting revision. The failures involved authority refusals and drift in the approved PG-ONPREM-2 disabled patch.

## Verification and Review Fixes

- `408a2064` Enforce readiness for status-only in-review specs.
- `7ca91333` Bound the Dapr version probe and verify readiness expiry.
- `bdba7a74` Verify the EventStore fixture with an inherited token.
- `eb36d016` Stop Bash report jobs on interruption.
- `9692ad82` Refuse unexpected Windows report destinations.
- `ff86755c` Wait for projections before exercising membership.
- `eedabd5e` Check event graph state after workflow completion.
- `11634063` Allocate a direct OpenBao fixture endpoint.
- `d67ae5d7` Keep bootstrap probe diagnostics from blocking daprd.
- `ee82015a` Wait for case projections in membership scenarios.
- `cf1d20ec` Wait for projections before testing case error paths.
- `0a19b53f` Wait for Redis at the published host port.
