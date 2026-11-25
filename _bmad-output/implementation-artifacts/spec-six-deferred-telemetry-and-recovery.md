---
title: 'Complete six deferred work items for telemetry and recovery'
type: 'bugfix'
created: '2026-11-25'
status: 'done'
route: 'oneshot'
baseline_commit: '10aacb84392e04b57fcf98ef9cc6d769fc526ef2'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Continue the priority-ordered deferred work after DW-518. Complete six items, add regression coverage where applicable, record each closure in the register, and make one commit per completed item. Record why any item was skipped. Build the full solution, run every test project, and push the verified commits to `main`.

</frozen-after-approval>

## Implementation Notes

- DW-519 configures millisecond histogram boundaries at the instrument definitions in `MemoriesMeter`. `OpenTelemetryRegistrationTests` checks the exported bucket counts from the service defaults pipeline.
- DW-520 adds dated catalog links to Stories 7.5, 9.2, 9.3, and 20.5 while preserving the historical evidence.
- DW-522 initializes the case activity failure summary from the retained Redis stream before appending a new failure and protects concurrent initialization.
- DW-523 is already implemented by the atomic enqueue and prune scripts and their Redis integration tests.
- DW-524 reconciles legacy retry queues into the tenant backlog index before polling that index.
- DW-525 compares the observed hash state inside the retry mutation scripts, including when the payload was absent.
- DW-526 needs an overflow policy that preserves active and suspended workflows when the registry reaches capacity. Removing entries by age or size would leave the replay gate unaware of live workflows.
- DW-527 initializes replay safety only after a confirmed legacy drain and ignores the obsolete marker set by older writers.

## Review Triage Log

- Medium, patched. The telemetry guide describes the five-minute settling window after all replicas move to the new histogram boundaries.
- Low, retained coverage. The exporter regression exercises the seconds-scale percentile correction on both instruments. The search-specific threshold boundaries are explicit in the instrument definition.
- Medium, patched. The case summary reconciles previously undercounted and noncanonical values with an optimistic transaction. It preserves larger lifetime counts and derives legacy history from the retained stream.
- Medium, patched. A failed legacy key scan leaves the indexed retry queues available. Discovery retries on the next poll.
- Medium, patched. The retry race test checks sorted-set entries, payloads, completion lineage, and tenant backlog membership for every mutation outcome.
- Medium, patched. Replay-safety documentation specifies the existing intake quiescence rule and how to invalidate the marker during rollback to an untracked writer. Tests cover the shared drain confirmation across replicas and the startup timeout separately.
- Medium, deferred. Annotation projection starts ingestion as a child workflow outside the scheduler that populates the registry. Track the replay-safety coverage gap as DW-740.
