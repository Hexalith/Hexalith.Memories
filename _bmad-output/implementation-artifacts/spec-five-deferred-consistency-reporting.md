---
title: 'Distinguish notes and discrepancies in consistency reports'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Operators need to distinguish units requiring repair from informational observations, including when a verification result spans multiple batches or its stored lists are truncated.

- DW-302. Given a requested batch size of one, when the workflow processes mixed results across the effective ten-unit batch boundaries, then it retains each finding in order with the correct category and totals.
- DW-303. Given a truncated verification or a replay-safety warning, when an operator consults the consistency reference, then they can identify the truncated list and triage the reason emitted by the relevant server version.
- DW-304. Given a repair recommendation or note-only observation, when the workflow emits event 8201, then the structured entry identifies its category and retains its existing recommendation field.
- DW-305. Given a verification result with notes and differing retained and total counts, when serialized and deserialized through the generated JSON contract, then its entries and counts survive.
- DW-308. Given a completed verification, when the operator requests table output, then the published `notes` and `discrepancies` columns report the respective retained counts.

</frozen-after-approval>

## Implementation Notes

Base revision `54835f7aaf28b4d2656f0ca51bf3a35f0384e4e9`. The requested repository refresh completed before implementation.

The workflow appends `EntryKind` to event 8201 and uses the same classification that routes the record. The runbook separates the two current 9173 reasons from the two emitted by older hosts. The table regression uses the existing lowercase column names and checks both truncation directions.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Workflows/ConsistencyVerificationWorkflow.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyVerificationWorkflowTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyVerifyCommandTests.cs`
- `docs/dev/consistency.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-consistency-reporting.md`

## Review Triage Log

- Medium, fixed. Event 8201 now includes `WorkflowInstanceId` so operators can attribute findings to overlapping runs. A regression checks both the tenant and the instance on every finding.
- Medium, fixed. The runbook explains how to interpret historical events without the new fields and when overlapping runs require external correlation.
- Low, fixed. The CLI example saves one completed audit and reads its recommendations from that file.
- Medium, fixed. The event 9173 guidance explicitly states that the host proceeds after a query failure and that the operator must verify drain before resuming.
- Medium, fixed. Both 10,001-entry regression cases prove the first omitted unit is still logged under the appropriate category.
- Low, fixed. The structured-log test now includes a free-text-only observation with the `InformationalNote` fallback.

## Verification

Using .NET SDK 11.0.100 with the recorded dependency revisions:

- The complete Debug solution built with zero warnings and errors using source references and the Aspire 17.0.0 CLI bundle. The shared workspace has a read-only user profile, which blocks live Aspire startup at its first-use sentinel.
- All 41 focused server cases passed, including workflow routing and logging, replay safety, and operator documentation guards. The source-backed run used `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net11.0/Hexalith.Memories.Server.Tests.dll -class '*ConsistencyVerificationWorkflowTests' -class '*WorkflowReplaySafetyHostedServiceTests' -class '*ConsistencyModelDecisionTests' -class '*MemoryUnitIdStabilityContractTests'`.
- All 456 CLI and 600 contract cases passed when each Debug test assembly ran individually. The CLI uses the public verify command and both directions of truncation. The contract test checks that the resolved type metadata came from a generated JSON context.
- The full package-backed server assembly reported 3,362 passed, one skipped, and one failure with kubectl v1.35.0. `OperationalRunbookSetTests.RequiredRunbooks_ExistAndHaveExactSharedHeadings` rejects a pre-existing extra heading in `docs/operations/capacity-planning.md`. Both the test and the runbook match the base revision. The opt-in submodule-mutation test was skipped by its own configuration.
- In a source-backed full-server run one additional test, `ProjectAssets_RestoreEventStorePackagesAtCatalogVersion`, expected a package asset where the source profile correctly restored a project. It passed with the default package profile.
- All 15 consistency workflow tests also passed with the refreshed direct references. The full source restore on those revisions encountered NU1605 because EventStore requires a newer Aspire Dapr package than Memories pins. Dependency pointers are excluded from this change.
