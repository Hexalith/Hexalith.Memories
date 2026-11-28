---
title: 'Align verification ownership documentation with the current CI lanes'
type: 'chore'
created: '2026-11-28'
status: 'done'
route: 'dispatch'
baseline_commit: '2b9e49bcf079034ffa12c59a20f523fff4dfb6a8'
review_loop_iteration: 0
context: ['{project-root}/AGENTS.md', '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md']
---

<frozen-after-approval reason="human-owned intent">

## Intent

DW-779 addresses “Align the pre-existing CI-ownership documentation with the current shared CI build timeout and eight-project coverage lane.” The ledger reflects the historical eight-project lane. Current source uses nine projects. The runbook and contributor guidance still disagree about workflow revision, timeout and report count.

## Boundaries & Constraints

Update the existing ownership sections to reflect the checked-in contract. Preserve historical rollout and rollback records. No workflow, dependency, CI setting, source package, or submodule changes. Do not edit the ledger/spec or commit. Use run-with-temp for disposable verification jobs.

</frozen-after-approval>

## Code Map

- `docs/dev/release-runbook.md` contains the shared/local CI ownership table and description.
- `CONTRIBUTING.md` contains the contributor-facing CI ownership and evidence section.
- `.github/workflows/ci.yml` records the current shared workflow caller, matching execution SHA, inputs, and artifact retention.
- `tools/test-projects.unit-contract.txt` and the coverage gate fixture JSON define the current report inventory.

## Tasks & Acceptance

- [x] Correct the current ownership narrative in both documentation files.

Given the current workflow and inventory, both docs agree with the exact workflow revision and execution SHA, current configured build time budget, and current local coverage report count. The documents retain historical source-bound claims and rollback references.

## Implementation Notes

## Spec Change Log

## Review Triage Log

The rollback instructions name current checks and the exact historical commit containing the retired fixtures.

## Verification

Read the active configuration and inventory, verify both edited sections against it, and run the relevant existing documentation or CI alignment fixture. Do not add a new YAML-text gate.

All 26 coverage fixtures passed. The source cross-check matched the workflow pin, execution SHA, Builds gitlink, 60-minute budget, nine coverage reports, 76.5% gate, shard options, and 7/14-day artifact retention. It also verified the historical runbook rollback boundary and the contributor link.

## File List

- `CONTRIBUTING.md`
- `docs/dev/release-runbook.md`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-28 | create-story | Scoped the verified deferred item | Focused checks planned | Recorded affected surfaces |
| 2026-11-28 | dev-story | `4c7cb7efb3ed918b3b8c3d09ef940186e511b278` | 26 coverage fixtures and source-bound documentation checks | Matched 2/2 |
| 2026-11-28 | code-review | `7d7ca81c10b58960093fa806d39e6b2b8a69f103` | 26 coverage fixtures and source-bound documentation checks | Matched 2/2 |
