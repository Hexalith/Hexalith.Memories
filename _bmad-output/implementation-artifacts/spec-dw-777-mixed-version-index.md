---
title: 'Qualify first-index creation for mixed-version rollouts'
type: 'bugfix'
created: '2026-11-28'
status: 'done'
route: 'dispatch'
baseline_commit: '453c7b0a5e41468b3e04820b2aa5fbcbd97a73df'
review_loop_iteration: 0
context: ['{project-root}/AGENTS.md', '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md']
---

<frozen-after-approval reason="human-owned intent">

## Intent

DW-777 addresses “Verify and document first-index creation during mixed-version Memories rollouts.” Previous and upgraded state writers use different Dapr Redis request options when the first index state row is created. Verify the counterexample and establish the supported operational boundary.

## Boundaries & Constraints

Use local disposable Dapr and Redis fixture state only. Preserve existing durable state shape, compare-and-save and workflow histories. Document a verifiable rollout restriction if coexisting legacy and new writes cannot safely initialize an absent index. Coordinate .NET build and Docker with the parent. Use run-with-temp; do not edit the ledger/spec or commit.

</frozen-after-approval>

## Code Map

Read the source spec spec-six-deferred-bootstrap-graph-and-mapping.md. DaprAggregateCaseMappingStore and the existing DaprStateStoreLiveSidecarTests own index creation and live Dapr/Redis contracts. Follow the Hexalith state instructions before persisting fixtures.

## Tasks & Acceptance

- [x] Implement the scoped behavior and focused regression evidence.

Given the pinned live Dapr Redis component, compare legacy unmarked first creation with upgraded FirstWrite and retained-ETag updates. Given an absent index during a mixed-version rollout, the runbook identifies when upgrade operators may safely resume writers.

## Implementation Notes

## Spec Change Log

## Review Triage Log

The live proof now checks the pinned Lua script conflict signature. The rollback path specifies drain, image digest verification, and a target-version persisted canary.

## Verification

Use the real disposable Dapr/Redis integration test and inspect persisted index payloads, including an existing ETag compare-and-save control.

The Release integration project built without warnings or errors. All 13 tests in the pinned live Dapr/Redis fixture passed, including the four new cases. Legacy-first loses the first type, upgraded-first refuses the stale write, and existing-index ETag controls preserve both members. All ten operational runbook checks passed.

## File List

- `docs/operations/upgrade-migration.md`
- `tests/Hexalith.Memories.IntegrationTests/EventStore/DaprStateStoreLiveSidecarTests.cs`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-28 | create-story | Scoped the verified deferred item | Focused checks planned | Recorded affected surfaces |
| 2026-11-28 | dev-story | `2b9e49bcf079034ffa12c59a20f523fff4dfb6a8` | 13 live sidecar and 10 runbook tests, then both targeted conflict cases | Matched 2/2 |
| 2026-11-28 | code-review | `f787262af2a6994cbd6fcef9c7f4a38fdb124737` | 13 live sidecar and 10 runbook tests, then both targeted conflict cases | Matched 2/2 |
