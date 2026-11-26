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

## Verification

- DW-556 affected surface is the tenant-scoped restore lease selector and the import reply using its owner. `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*RestoreLeaseIntegrationTests'` passed. `ConcurrentUploads_CoalesceOnlyWithinTenantAndKeepTheWinningLease` checks that an identical upload in another tenant acquires its own lease and that loser/winner cleanup preserves that tenant's ownership.
- `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net11.0/Hexalith.Memories.Server.Tests.dll -class '*RestoreImportRetryTests'` passed all four API cases, including pending ownership, failed state lookup, reuse of a confirmed workflow, and different content.

- DW-559 `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*UrlIngestionPrivateHostDenialIntegrationTests' -class '*UrlIngestionIntegrationTests'` passed all three cases using the owned Dapr placement and scheduler. The denied URL creates no workflow registry record, source-inventory or unit keys, or changes to case counters or activity.

## Cross-Tenant Negative Evidence

**Surfaces:** ImportEndpoints and the tenant-wide restore lease key and workflow owner selection.

**Tests:** `ConcurrentUploads_CoalesceOnlyWithinTenantAndKeepTheWinningLease`

**Command:** `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*RestoreLeaseIntegrationTests'`

**Result:** Passed against live Redis Stack. Identical uploads in different tenants acquire separate leases. Neither a competing case import nor cleanup of a losing upload releases another owner.
