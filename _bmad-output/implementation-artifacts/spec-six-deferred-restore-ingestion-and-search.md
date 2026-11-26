---
title: 'Complete six deferred restore, ingestion, and search items'
type: 'bugfix'
created: '2026-11-26'
status: 'in-progress'
route: 'one-shot'
baseline_commit: '81928b7e0a1700ac609a7af7c4d748fdd5aaede5'
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete the next six actionable deferred items in ledger order. Implement and verify each item, keep its closure and changes in a separate commit, record why any skipped item remains open, and verify the full batch before pushing to main.

</frozen-after-approval>

## Implementation Notes

- DW-556 uses the atomic Redis acquisition result to identify the active holder. An identical upload receives its workflow location only when Dapr confirms the instance exists. A conflicting or pending upload receives a 409. Duplicate staging is cleaned independently, and cancellation during the clean-target check releases the lease.
- DW-558 remains open as a convention for future bulk line-ending work. There is no current mass normalization to commit.

- DW-559 starts a dedicated Aspire topology with private URL hosts disabled at the server resource. The existing shared topology retains the loopback option for positive ingestion scenarios.
- DW-556 review caught actual gRPC scheduling exceptions and a possible lost Redis acknowledgment. The endpoint independently verifies status after failed submission, releases staging after confirmed pre-acceptance rejection, and retains it when submission is ambiguous. The pre-scheduling path cleans up staging on cancellation during scan, lease acquisition, or target verification.
- DW-560 exercises five ordered outcomes against the live Aspire API, with a source-admission fence for the hosted ownership conflict. The route test controls the exact competing claim and per-unit write failure. The Redis claim script validates/removes the sorted-set entry before deleting the failed-unit hash and dedup record, preserving both if the index write errors.
- DW-561 needs a worker that can hold real workflows at requested transitions. DW-562 and DW-563 need an isolated performance lane with a stable baseline.
- DW-564 scopes a failure to an exact query embedding input in the test-owned provider. The live API and Redis/FalkorDB remain active. The control query and healthy retry verify that other work and subsequent semantic retrieval still succeed.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-six-deferred-restore-ingestion-and-search.md` - verification trace
- `_bmad-output/implementation-artifacts/deferred-work.md` - deferred item status and evidence
- `docs/operations/backup-restore.md` - import retry behavior
- `src/Hexalith.Memories.Server/Endpoints/ImportEndpoints.cs` - restore retry handling
- `src/Hexalith.Memories.Server/Import/RedisImportStagingStore.cs` - atomic restore lease reservation
- `tests/Hexalith.Memories.Server.Tests/Endpoints/RestoreImportRetryTests.cs` - API response contract and staging cleanup
- `tests/Hexalith.Memories.IntegrationTests/Restore/RestoreLeaseIntegrationTests.cs` - physical lease ownership and tenant isolation

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` - resource-scoped URL policy configuration
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/UrlIngestionIntegrationTests.cs` - remove the skipped denial scenario
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/UrlIngestionPrivateHostDenialCollection.cs` - serialize the private-host topology
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/UrlIngestionPrivateHostDenialIntegrationTests.cs` - denial and persistence regression
- `src/Hexalith.Memories.Server/Ingestion/FailedUnitsRegistry.cs` - protect failed records on an index-write error
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/FailedUnitClaimIntegrationTests.cs` - live Redis race and failed-write proof
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs` - live five-outcome request and tenant/case isolation
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ReIngestionEndpointE2ETests.cs` - exact competing claim and registry write failure
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/EmbeddingProviderFaultPlan.cs` - exact query fault selection
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServer.cs` - test-owned provider fault wiring
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/IngestionIntegrationTestDriver.cs` - shared hybrid request and persisted hash snapshots
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/EmbeddingProviderFailureIntegrationTests.cs` - semantic-isolated degradation
- `tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs` - share search test helpers
- `tests/Hexalith.Memories.Integration.NightlyTests/Search/DegradationIntegrationTests.Nightly.cs` - use shared search test helpers

## Verification

- DW-556 affected surface is the tenant-scoped restore lease selector and the import reply using its owner. `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*RestoreLeaseIntegrationTests'` passed. `ConcurrentUploads_CoalesceOnlyWithinTenantAndKeepTheWinningLease` checks that an identical upload in another tenant acquires its own lease and that loser/winner cleanup preserves that tenant's ownership.
- `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net11.0/Hexalith.Memories.Server.Tests.dll -class '*RestoreImportRetryTests'` passed all four API cases, including pending ownership, failed state lookup, reuse of a confirmed workflow, and different content.

- DW-559 `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*UrlIngestionPrivateHostDenialIntegrationTests' -class '*UrlIngestionIntegrationTests'` passed all three cases using the owned Dapr placement and scheduler. The denied URL creates no workflow registry record, source-inventory or unit keys, or changes to case counters or activity.
- DW-556 review verification ran all seven cases in `RestoreImportRetryTests`, including definite gRPC rejection, ambiguous submission, and server-confirmed acceptance. The live Redis reuse and isolation case also passed on the reviewed code.
- DW-559 review verification also compared native Dapr workflow keys and checked the persisted reverse lookup for unexpected ingestion admission. The isolated final run of `UrlIngestion_PrivateIpWithAllowDisabled_ShouldRejectBeforeScheduling` passed.
- DW-560 `ReIngestBulk_MixedOutcomes_EnumeratedInResponse`, `PostBulkReIngest_WithLostClaimAndRegistryError_ReturnsFiveOrderedOutcomes`, and `ClaimWriteFailurePreservesTheFailedUnitAndOnlyOneRetryCanClaimAfterRepair` passed.
- DW-564 `HybridSearch_WhenQueryEmbeddingFails_DegradesOnlySemanticAndRecoversAsync` and the existing `OllamaOidcFakeServerTests` passed.

## Cross-Tenant Negative Evidence

**Surfaces:** ImportEndpoints and the tenant-wide restore lease key and workflow owner selection.

**Tests:** `ConcurrentUploads_CoalesceOnlyWithinTenantAndKeepTheWinningLease`

**Command:** `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*RestoreLeaseIntegrationTests'`

**Result:** Passed against live Redis Stack. Identical uploads in different tenants acquire separate leases. Neither a competing case import nor cleanup of a losing upload releases another owner.

**Surfaces:** DW-560 failed-unit tenant/case selectors and bulk retry authorization.

**Tests:** `ReIngestBulk_MixedOutcomes_EnumeratedInResponse`

**Command:** `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -method '*ReIngestBulk_MixedOutcomes_EnumeratedInResponse'`

**Result:** Passed against the Aspire topology. Another tenant is denied. A sibling case receives case mismatch for every known failed unit. Neither attempt changes the identified records or source inventory.
