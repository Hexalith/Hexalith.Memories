---
title: 'Complete six deferred telemetry, tooling, and CI items'
type: 'bugfix'
created: '2026-11-27'
status: 'done'
route: 'oneshot'
baseline_commit: 'e5e3be99157db3a843c6e714a3b9b4bf1d55aa2a'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete the next six actionable items in register order. Enforce serialization of shared telemetry tests (DW-585), derive CLI SDK guidance from the checked minimum (DW-586), document exact access telemetry mapping boundaries (DW-597), move the lifecycle store double out of the runtime assembly (DW-599), retain evidence checkpoints and distinguish approved transfers (DW-630), and reconcile fast-integration timing guidance with successful hosted runs (DW-631). Record item dispositions and commit each completion separately.

</frozen-after-approval>

## Implementation Notes

- DW-585. The Server architecture test inspects both test projects and checks that shared telemetry tests belong to an xUnit collection with parallelization disabled. Existing serialized environment tests retain their collection.
- DW-586. CLI SDK checks and recovery messages use one SDK floor. The AppHost runtime check keeps its own framework requirement.
- DW-597. The ADR records the inclusive upper byte boundaries and distinguishes producer saturation from persisted-record validation. Exact boundary tests passed.
- DW-599. A test-support assembly contains the existing store double. Both consuming test projects pass focused checks and a Release publish omits the assembly.
- DW-630. The readiness check compares prior checkpoint labels against the current story. It requires the dated approving decision, destination, and accountability for transfers. A real Git fixture verifies the dispositions.
- DW-631. Three successful hosted jobs took roughly 12 minutes end to end. Their current 30- and 35-minute job timeouts already cover setup and the measured runtime. The required check name identifies the selector.

## Review Triage Log

- DW-585, patched. The guard recognizes shared collectors and emitters, combines partial declarations, and accepts collection names declared with `nameof`.
- DW-586, patched. SDK fixtures and assertions follow the minimum, and runtime tests still require the .NET 11 frameworks.
- DW-597, patched. The ADR records negative source handling and preserves the mapping section's one-table contract.
- DW-599, patched. The assembly-boundary regression uses the store interface to identify the runtime assembly without referencing an emitting implementation.
- DW-630, patched. Validation checks the staged story, the pull request merge base and intervening commits, and the selected commit's parent for explicit audits. Checkpoint labels identify their evidence sections. Proposals can remain pending, and transfer dispositions require dated approval and accountability.
- DW-631, reconciled. The guide cites the hosted measurements and explains the headroom for earlier slower runs. The required check name and workflow limits match their current roles.
