---
title: 'Complete the next ten deferred work items'
type: 'chore'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Continue from DW-261 in ledger order. Complete ten actionable entries with one Conventional Commit per entry, record each completion and any new reasons for skipping, then build the solution and run all test projects before pushing to `main`.

</frozen-after-approval>

## Implementation Notes

- The previous batch ended at DW-261. This batch processes the next actionable entries in ledger order. The DW-731 priority applies to the next change to automatic integration lane selection.
- DW-263, DW-265, DW-270, DW-271, and DW-272 reconcile quickstart prerequisites, bound inline ingestion, scope retry telemetry, review the shared Redis instrumentation pin, and diagnose malformed activity capture.
- DW-275, DW-285, DW-286, DW-287, and DW-288 verify persisted dual embeddings, graph traversal, shared embedding quotas, and retry progress across tenants and worker restarts.
- The Redis instrumentation review creates DW-739 with a three-month review date and retains the 14-day trigger after the first stable release.

## Review Triage Log

- DW-263, medium, fixed. The early acceptance criterion and the Dapr implementation task now agree with the uppercase human-output label and the optional result flag.
- DW-265, high, fixed. The REST-side guard would have reached the MCP tool's generic error mapper. The MCP boundary now validates UTF-8 bytes and preserves the structured input error.
- DW-265, low, fixed. All public ingest and refresh overloads document the local size exception.
- DW-271, low, fixed. The telemetry decision and the NuGet comment identify the shared catalog as the current pin owner.
- DW-272, high, fixed. The parser test uses the actual breadcrumb payload property.
- DW-272, medium, fixed. Resource identity and log framing distinguish breadcrumbs from ordinary marker mentions.
- DW-272, medium, fixed. Invalid optional parent IDs fail as malformed capture.
- DW-275, medium, fixed. The publication, search, and persisted-state reads share a seven-second cancellation budget.
- DW-275, medium, fixed. Vector dimensions, case scope, AI origin, graph state, and durable workflow output are asserted.
- DW-285, medium, fixed. The test fixture uses the compact FalkorDB edge-map representation.
- DW-285 and DW-286, medium, fixed. The tests verify the generated traversal filter and the stub field projection.
- DW-287, medium, fixed. The activity test now drives a real quota actor and checks successive persisted remaining counts.
- DW-288, medium, fixed. An interruption after dequeue proves the Redis record survives for a replacement worker.
- DW-288, medium, fixed. A repeated retry uses the durable active instance and makes zero further submissions. The test checks one conversation and embedding call and the published Redis vector.
