---
title: 'Complete six deferred restore, ingestion, and search items'
type: 'bugfix'
created: '2026-11-26'
status: 'done'
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
- DW-565 bounds tenant and case verification to five seconds. Case verification reads the stored case record and preserves its tenant check independently of optional graph enrichment. A valid cached tenant status can allow tenant-wide search to proceed to the existing axis-specific responses. The outage test verifies each axis actually contributes after both storage resources restart.
- DW-566 proves the shared Redis failure response on syntactic search with a live graph backend. The request recovers once the stored case and index can be read again. The previous skipped topology assumptions were removed when their replacement tests landed.

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
- `docs/dev/health-checks.md` - document control-plane search availability
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` - reuse tenant-checked stored case reads
- `src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs` - bound and map search control-plane failure
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` - timeout, ownership, and denial-before-dependency regressions
- `tests/Hexalith.Memories.Integration.NightlyTests/Search/SearchControlPlaneOutageIntegrationTests.cs` - full outage and independent-axis recovery
- `tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.Fast.cs` - remove obsolete skipped scenarios
- `tests/Hexalith.Memories.Integration.NightlyTests/Search/SyntacticSearchControlPlaneOutageIntegrationTests.cs` - single-axis outage and recovery

## Verification

- DW-556 affected surface is the tenant-scoped restore lease selector and the import reply using its owner. `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*RestoreLeaseIntegrationTests'` passed. `ConcurrentUploads_CoalesceOnlyWithinTenantAndKeepTheWinningLease` checks that an identical upload in another tenant acquires its own lease and that loser/winner cleanup preserves that tenant's ownership.
- `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net11.0/Hexalith.Memories.Server.Tests.dll -class '*RestoreImportRetryTests'` passed all four API cases, including pending ownership, failed state lookup, reuse of a confirmed workflow, and different content.

- DW-559 `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*UrlIngestionPrivateHostDenialIntegrationTests' -class '*UrlIngestionIntegrationTests'` passed all three cases using the owned Dapr placement and scheduler. The denied URL creates no workflow registry record, source-inventory or unit keys, or changes to case counters or activity.
- DW-556 review verification ran all seven cases in `RestoreImportRetryTests`, including definite gRPC rejection, ambiguous submission, and server-confirmed acceptance. The live Redis reuse and isolation case also passed on the reviewed code.
- DW-559 review verification also compared native Dapr workflow keys and checked the persisted reverse lookup for unexpected ingestion admission. The isolated final run of `UrlIngestion_PrivateIpWithAllowDisabled_ShouldRejectBeforeScheduling` passed.
- DW-560 `ReIngestBulk_MixedOutcomes_EnumeratedInResponse`, `PostBulkReIngest_WithLostClaimAndRegistryError_ReturnsFiveOrderedOutcomes`, and `ClaimWriteFailurePreservesTheFailedUnitAndOnlyOneRetryCanClaimAfterRepair` passed.
- DW-564 `HybridSearch_WhenQueryEmbeddingFails_DegradesOnlySemanticAndRecoversAsync` and the existing `OllamaOidcFakeServerTests` passed.
- DW-565 `HybridSearch_AllStorageBackendsStopped_FailsClosedAndRecovers` and the existing FalkorDB-only degradation scenario passed on real Docker resources. The Server route suite verified stalled lookup, authorization precedence, and graph-independent ownership checks.
- DW-566 `SyntacticSearch_SharedRedisStopped_FailsClosedWhileGraphRemainsAvailable` passed on the same nightly topology as DW-565.

## Cross-Tenant Negative Evidence

**Surfaces:** ImportEndpoints and the tenant-wide restore lease key and workflow owner selection.

**Tests:** `ConcurrentUploads_CoalesceOnlyWithinTenantAndKeepTheWinningLease`

**Command:** `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*RestoreLeaseIntegrationTests'`

**Result:** Passed against live Redis Stack. Identical uploads in different tenants acquire separate leases. Neither a competing case import nor cleanup of a losing upload releases another owner.

**Surfaces:** DW-560 failed-unit tenant/case selectors and bulk retry authorization.

**Tests:** `ReIngestBulk_MixedOutcomes_EnumeratedInResponse`

**Command:** `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -method '*ReIngestBulk_MixedOutcomes_EnumeratedInResponse'`

**Result:** Passed against the Aspire topology. Another tenant is denied. A sibling case receives case mismatch for every known failed unit. Neither attempt changes the identified records or source inventory.

**Surfaces:** DW-565 search tenant verification and Redis case ownership.

**Tests:** `HybridSearch_TenantStateOutageFailsClosedBeforeRetrieval`, `CaseScopedSyntacticSearch_VerifiesCaseOwnershipWithoutRequiringGraph`

**Command:** `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net11.0/Hexalith.Memories.Server.Tests.dll -class '*SearchEndpointContractTests'`

**Result:** Passed. A tenant outside bearer claims is denied before Dapr or Redis reads. A case hash marked as another tenant is unavailable under the authorized route and no graph enrichment occurs during the ownership check.

## Review Triage Log

- DW-556. Patched production gRPC reconciliation, the possible lost Redis acknowledgment and cancellation after staging. Live Redis lease ownership and the HTTP response contract were verified independently.
- DW-559. Added native workflow-key and persisted reverse-index comparisons to the denial test.
- DW-560. Added real Redis guards for the failed claim write and competing owners. The hosted five-way case uses a source-admission fence, and the route test controls the exact lost-claim branch.
- DW-564. The exact query fault exercises the semantic embedding dependency using the live topology. A vector-store outage uses the shared Redis failure scenarios.
- DW-565 and DW-566. Removed optional graph enrichment from case verification, checked the returned memory-unit identifier, verified all recovered hybrid contributions, and bounded the Redis connection and single-axis recovery waits. When cached tenant state already answers a tenant-wide query, existing retrieval degradation applies.

## Full Verification

- `dotnet restore Hexalith.Memories.slnx` and `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore -m:1` passed.
- Ran every project in the unit/contract, integration-fast, integration-slow and benchmark inventories. The final Server test project run passed. Initial fast and nightly integration failures were replayed and passed on isolated topologies, including the FalkorDB-only degradation case after the case-read fix. Existing skips were retained.
- The Chromium suite passed all six checks. Type checking and artifact validation passed.
- The isolated AccessTelemetry SQL contract suite passed all 19 tests. The focused backup recovery verifier passed all eight cases. A supplementary full release-tooling sweep was stopped after product and focused verification finished.
