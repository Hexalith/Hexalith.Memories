---
title: 'Strengthen tenant isolation verification'
type: 'chore'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close ten deferred items that affect the reliability and diagnosis of tenant-isolation verification. Keep the product behavior and wire contract intact.

- Given a freshly provisioned collision fixture, when it seeds relationships, then it first proves the graph has zero relationships and names the tenant if the precondition fails (DW-654).
- Given a healthy traversal, when the collision test reads the HTTP response, then it checks required array members on the wire and the contract's omission of healthy default values before checking deserialized values (DW-661). Missing nodes, edges or gap markers produce named assertion failures (DW-666).
- Given cancellation or a graph driver timeout, when the existing verifier regressions execute, then cancellation prevents backend reads and the graph timeout yields a failed check without driver details (DW-667, DW-674).
- Given an OpenBao restart, when the actor responds, then a missing embedding configuration reports the recovery tenant and field (DW-669).
- Given the fast integration inventory, when the runbook, verifier and HTTP tests check the graph content proof reference, then they share the same exact class and test identity (DW-670).
- Given the verifier source, when the source guard executes in the verifier test class, then it requires the class-wide architectural scope and the graph-specific structural scope and preserves the existing command guard (DW-663, DW-671).
- Given a verification response, when the integration assertion expects a named core check, then missing and duplicate results identify that check (DW-672).

</frozen-after-approval>

## Implementation Notes

- Baseline is `d8d6984c021ab5f229db7f5c40d96e8266549d2a` with 346 open ledger entries. This test-only batch uses existing fixtures, the tracked fast integration inventory and the shared test support project. The focused cancellation and timeout baseline passed three cases.

- The verifier source guard now lives with the verifier tests. Server and integration tests resolve the proof citation from the tracked `graph-content-isolation` inventory row. The HTTP test also ties that row to its compiled test name.
- Pre-edit AppHost startup used isolated ports and separate disposable data volumes. The 30-second `aspire wait memories --status up` deadline elapsed. The session was stopped and its two volumes were removed. Focused live verification uses the integration test fixture with the local Dapr placement and scheduler endpoints.
- The first isolated runner attempts could not find repository files because xUnit moves its working directory to the output directory. The retry provides the required checkout files alongside the temporary artifacts.

- Final Debug builds with pinned source project references passed with no warnings or errors. The run used a disposable local clone with the standard output layout and exact root-declared submodule revisions.
- `dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net11.0/Hexalith.Memories.Server.Tests.dll -class '*TenantIsolationVerifierTests' -class '*OperationalRunbookSetTests'` passed 116 cases with no failures or skips.
- The live `dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net11.0/Hexalith.Memories.IntegrationTests.dll` run passed all four selected methods with no failures or skips in 23.759 seconds. The `-method` selectors matched `VerifyTenant_WithTwoProvisionedTenants_CoreIsolationChecksShouldPass`, `VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`, `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage`, and `InPlaceOpenBaoRestart_RotatesGenerationAndRecoversDependentSidecars`. The marker control proves the fixture rejects the deliberately planted edge marker.
- Exactly DW-654, DW-661, DW-663, DW-666, DW-667, DW-669, DW-670, DW-671, DW-672 and DW-674 closed. The numbered open ledger count is 336.

## Review Triage Log

- Low, rejected. The OpenBao scenario proves actor connectivity after a secret-store restart. The Google fallback is valid for that purpose, and durable recovery of a custom configuration is outside this assertion. The added null check still names the tenant when the actor proxy returns no configuration.
- Low, patched. The planted-data verifier lookups now use the same named uniqueness assertion as the main HTTP verifier test.
- Medium, patched. The runbook now checks complete command lines and a delimited citation. The HTTP and unit assertions match the citation as a complete identifier.
- Medium, patched. The relocated command guard includes partial verifier declarations regardless of whether each repeats the sealed modifier.
- Medium, patched. Required JSON arrays must appear exactly once so duplicate properties cannot hide differing content.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-ten-deferred-isolation-verification.md`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.Fast.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`
- `tests/Hexalith.Memories.TestHelpers/Documentation/GraphContentProof.cs`
- `tests/Hexalith.Memories.TestHelpers/Documentation/TestRepository.cs`
