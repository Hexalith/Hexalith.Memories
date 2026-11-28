---
title: 'Distinguish absent Dapr containers from Docker pre-cleanup failures'
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

DW-773 addresses the flat deferred entry “Distinguish absent Dapr containers from Docker pre-cleanup failures.” The bootstrap accepts ordinary nonzero docker rm results, which hides daemon and permission failures as well as the expected absent-container result. Fail before launch on operational errors and preserve the diagnostic.

## Boundaries & Constraints

Preserve the current exact-name ownership checks, command deadlines, signal cleanup and successful first-run behavior. Classify absence conservatively. Keep Docker resources scoped to disposable test fixtures. Use run-with-temp for scratch workspaces. Do not commit or edit the ledger/spec. The parent owns Git and commit order.

</frozen-after-approval>

## Code Map

tools/init-dapr-runtime.sh owns Docker bootstrap cleanup and command diagnostics. tests/tooling/integration_fast_coverage/dapr_runtime_test.py owns fake Docker and process fixtures.

## Tasks & Acceptance

- [x] Implement the scoped behavior and the focused regression evidence.

Given a genuinely absent prior container, initialization can continue. Given a Docker daemon or authorization error, initialization fails before launch with useful diagnostics. Given timeout or interruption, existing budgets and process cleanup still work.

## Implementation Notes

## Spec Change Log

## Review Triage Log

A fault-injected log cleanup failure preserved the original Docker status and both diagnostics. The Dapr suite passed all 12 cases.

## Verification

Run the Dapr runtime tooling suite and bash -n. Demonstrate at least one red/green failure classification regression, and test existing cleanup of owned containers. Report any live verification assumptions.

All four synthetic Docker error cases reproduced wrongful baseline success. After the change, all 11 Dapr tooling tests and shell syntax passed. The installed Docker 29.8.2 client confirmed absent, mixed, denied, and unavailable responses against a disposable Engine socket. No external Docker resources were mutated by that proof.

## File List

- `tests/tooling/integration_fast_coverage/dapr_runtime_test.py`
- `tools/init-dapr-runtime.sh`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-28 | create-story | Scoped the verified deferred item | Focused checks planned | Recorded affected surfaces |
| 2026-11-28 | dev-story | `c546d436a042e38f099050f45fc2a86eee95f2d0` | 12 Dapr tooling tests | Matched 2/2 |
| 2026-11-28 | code-review | `c78d08f0bd0d66d83b4b633049fdc0c454837434` | 12 Dapr tooling tests | Matched 2/2 |
