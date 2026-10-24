---
title: 'Inventory sources, refresh content and host Memories decisions'
type: 'feature'
created: '2026-10-24'
status: 'in-progress'
baseline_commit: '5a852389327bf45df7d59b2c2e29c7adbe353f19'
---

Status: in-progress

## Intent

Own the Memories implementation required by the approved Parties five-item batch: bounded source inventory, revision-aware current content, signed workload transport, and a real separate domain-service host. The parent approved spec is `spec-five-memories-inventory-and-publication-deferred-items.md`; Administrator approved it with "approve and continue" and requested main pushes. This owner artifact adopts the completed implementation for independent review and commit governance. It does not invent an earlier discovery delta or human code review.

Preserve ordinary-ingest defaults, exact tenant/source/case provenance, independent terminal proof, permanent erasure fences, public interface compatibility and the native Server route boundary. No package/catalog version changes, production migration, deployment or release dispatch are authorized here. Unknown legacy coverage remains incomplete.

## File Scope

Allowed files for this story:

- `Directory.Build.targets` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `Hexalith.Memories.slnx` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `_bmad-output/implementation-artifacts/spec-source-inventory-refresh-and-domain-host.md` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `docs/operations/route-surface.md` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Contracts/V1/SourceInventoryEntry.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Contracts/V1/SourceInventoryPage.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Contracts/V1/SourceRefreshProtocol.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.DomainService/Hexalith.Memories.DomainService.csproj` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.DomainService/MemoriesCaseAggregateAdapter.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.DomainService/MemoriesCommandScope.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.DomainService/MemoriesMemoryUnitAggregateAdapter.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.DomainService/MemoriesTenantAggregateAdapter.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.DomainService/Program.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.DomainService/Properties/launchSettings.json` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.DomainService/ScopedMemoriesAggregate.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.EventStore/Domain/Commands/CreateCaseCommand.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.EventStore/Domain/Commands/DeleteCaseCommand.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.EventStore/Domain/Commands/DeleteMemoryUnitCommand.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.EventStore/Domain/Commands/RegisterTenantCommand.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.EventStore/Domain/Commands/RequestAnnotationCommand.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.EventStore/Domain/Commands/UpdateTenantLifecycleStatusCommand.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.EventStore/Domain/Events/IMemoriesEventPayload.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Cases/AnnotationProjectionInput.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Cases/DeleteMemoryUnitProjectionActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Cases/ScheduleAnnotationIngestionActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticChunksActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Indexing/SemanticChunkIndexInput.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/AdmitSourceInventoryActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/IdempotencyInput.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/IdempotencyResult.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/IngestionInputValidator.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/PauseSourceInventoryProducerActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/PersistFailedUnitActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/PublishRefreshActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/ReconcileRefreshPublicationActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/RecordSourceInventoryCandidateActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/RecordSourceInventoryOutputActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/RefreshPublicationInput.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/SetSourceInventorySchedulingActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/SourceInventoryAdmissionInput.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/SourceInventoryOutputInput.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Ingestion/SourceInventorySchedulingInput.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Restore/RestoreDataPlaneActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Restore/RestoreReindexUnitActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/DerivedStores/RedisDerivedStoreService.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Endpoints/GraphEndpoints.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Endpoints/SourceInventoryEndpoints.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Endpoints/SourceInventoryQualificationEndpoints.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/EventStoreIntegration/EventStoreMemoriesCommandStore.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Infrastructure/TenantIndexReadinessVerifier.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/DaprWorkflowPayloadStore.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/GraphPublicationReadGate.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/GraphPublicationRepair.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/GraphSourcePublication.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/IngestionIdentityGuard.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/IngestionPayloadClaimCheck.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowStatusMapper.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/RefreshPublicationStore.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/SourceInventoryFailureReconciler.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/SourceInventorySnapshot.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/SourceInventoryStore.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/SourcePublicationVisibility.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/TenantIngestionOwnership.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Ingestion/WorkflowPayloadLedger.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Program.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Search/NaturalLanguageSemanticSearchService.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Search/SemanticSearchService.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Workflows/AnnotationProjectionWorkflow.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Workflows/Contracts/IndexInput.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Workflows/Contracts/NaturalLanguageIndexInput.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/InventoryStalledStream.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/InventoryStreamingContent.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/SourceInventoryClientTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.DomainService.Tests/Hexalith.Memories.DomainService.Tests.csproj` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.DomainService.Tests/MemoriesAggregateAdapterTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/RefreshPublicationIntegrationTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/SourceInventoryIntegrationTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/SourceInventoryProducerIntegrationTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/TenantIngestionLifecycleIntegrationTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexNaturalLanguageSemanticActivityTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticChunksActivityTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/IngestionInputValidatorTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/PersistFailedUnitActivityTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Activities/Restore/RestoreDataPlaneActivityTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Activities/Restore/RestoreReindexUnitActivityTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Architecture/IngestionWorkflowDeterminismGuardTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Authentication/IngestionStatusEndpointAuthorizationTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Endpoints/IngestionEndpointE2ETests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Hosting/MemoriesGatewayCapturingHandler.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Hosting/MemoriesGatewayCompositionTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Hosting/TenantIngestionCompositionTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Infrastructure/AtomicPublicationWriteFixture.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.
- `tests/Hexalith.Memories.Server.Tests/Workflows/TenantDeletionWorkflowTests.cs` -- approved inventory/refresh lifecycle implementation, verification or owner governance.

## Cross-Tenant Negative Evidence

**Surfaces:** Source inventory authorization and cursor scope, Redis/graph publication keys and tombstones, captured workflow/case identities, exact gateway command grants, and separate domain-service command/replay authority. This deliberately includes runtime/source and test paths in File Scope.
**Tests:** `SourceInventoryIntegrationTests.CursorsAreProtectedAndInvalidatedByConcurrentAdmission`, `IngestionStatusEndpointAuthorizationTests.SourceInventoryRejectsOtherTenantBeforeReadingOrFencing`, `MemoriesAggregateAdapterTests.ForeignEnvelopeSnapshotOrTailFailsBeforeAnyDecision`, `MemoriesAggregateAdapterTests.ReplayRouterOwnsOnlyItsExactPersistedDomainFallbackAndSdkType`.
**Command:** `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class *SourceInventoryIntegrationTests -class *RefreshPublicationIntegrationTests -class *SourceInventoryProducerIntegrationTests`; `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class *IngestionStatusEndpointAuthorizationTests -class *MemoriesGatewayCompositionTests`; `dotnet tests/Hexalith.Memories.DomainService.Tests/bin/Debug/net10.0/Hexalith.Memories.DomainService.Tests.dll`.
**Result:** Final real Redis/FalkorDB controls pass 21/21 with zero skips. Registered authorization/gateway controls 19/19 execute in the final source Server suite (3030 passed/1 existing destructive skip); domain-host controls pass 12/12 with zero skips. Foreign tenant/domain/aggregate/case, snapshot/tail scope and cursor tampering reject. The owned three-target live wrapper and child pass 1/1 each with actual signed credentials, persisted-stream comparison and real replay; unrelated tenant/case survive. Story 20.2 denial-before-dependency and Story 24.3 verifier/tenant-marker responsibilities remain enforced through those exact boundaries.

## Verification

The parent bounded receipt `tests/five-memories-inventory-and-publication-2026-10-24/verification.json` retains exact commands, XML hashes, mode-specific skips and private raw-evidence locations. Owner source Server: 3,030 passed plus one existing destructive skip; CLI/client: 505/505; Contracts: 596/596; domain host: 12/12; real adapters: 9/9. Release/package Server: 3,029 passed plus one existing destructive skip, CLI/client 505/505, domain host 12/12; package/catalog controls 2/2. The source Server excludes the single package-assets-only assertion, which executes in the package lane. Final builds have zero warnings/errors. Matching owner publication/adoption, production host/trust configuration, legacy reconciliation and broad Platform HXR020 remain separate gates.

### File List

- `Directory.Build.targets`
- `Hexalith.Memories.slnx`
- `_bmad-output/implementation-artifacts/spec-source-inventory-refresh-and-domain-host.md`
- `docs/operations/route-surface.md`
- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs`
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`
- `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`
- `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs`
- `src/Hexalith.Memories.Contracts/V1/SourceInventoryEntry.cs`
- `src/Hexalith.Memories.Contracts/V1/SourceInventoryPage.cs`
- `src/Hexalith.Memories.Contracts/V1/SourceRefreshProtocol.cs`
- `src/Hexalith.Memories.DomainService/Hexalith.Memories.DomainService.csproj`
- `src/Hexalith.Memories.DomainService/MemoriesCaseAggregateAdapter.cs`
- `src/Hexalith.Memories.DomainService/MemoriesCommandScope.cs`
- `src/Hexalith.Memories.DomainService/MemoriesMemoryUnitAggregateAdapter.cs`
- `src/Hexalith.Memories.DomainService/MemoriesTenantAggregateAdapter.cs`
- `src/Hexalith.Memories.DomainService/Program.cs`
- `src/Hexalith.Memories.DomainService/Properties/launchSettings.json`
- `src/Hexalith.Memories.DomainService/ScopedMemoriesAggregate.cs`
- `src/Hexalith.Memories.EventStore/Domain/Commands/CreateCaseCommand.cs`
- `src/Hexalith.Memories.EventStore/Domain/Commands/DeleteCaseCommand.cs`
- `src/Hexalith.Memories.EventStore/Domain/Commands/DeleteMemoryUnitCommand.cs`
- `src/Hexalith.Memories.EventStore/Domain/Commands/RegisterTenantCommand.cs`
- `src/Hexalith.Memories.EventStore/Domain/Commands/RequestAnnotationCommand.cs`
- `src/Hexalith.Memories.EventStore/Domain/Commands/UpdateTenantLifecycleStatusCommand.cs`
- `src/Hexalith.Memories.EventStore/Domain/Events/IMemoriesEventPayload.cs`
- `src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj`
- `src/Hexalith.Memories.Server/Activities/Cases/AnnotationProjectionInput.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/DeleteMemoryUnitProjectionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/ScheduleAnnotationIngestionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticChunksActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Indexing/SemanticChunkIndexInput.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/AdmitSourceInventoryActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/IdempotencyInput.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/IdempotencyResult.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/IngestionInputValidator.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/PauseSourceInventoryProducerActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/PersistFailedUnitActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/PublishRefreshActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/ReconcileRefreshPublicationActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/RecordSourceInventoryCandidateActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/RecordSourceInventoryOutputActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/RefreshPublicationInput.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/SetSourceInventorySchedulingActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/SourceInventoryAdmissionInput.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/SourceInventoryOutputInput.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/SourceInventorySchedulingInput.cs`
- `src/Hexalith.Memories.Server/Activities/Restore/RestoreDataPlaneActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Restore/RestoreReindexUnitActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs`
- `src/Hexalith.Memories.Server/Cases/CaseService.cs`
- `src/Hexalith.Memories.Server/DerivedStores/RedisDerivedStoreService.cs`
- `src/Hexalith.Memories.Server/Endpoints/GraphEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/SourceInventoryEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/SourceInventoryQualificationEndpoints.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/EventStoreMemoriesCommandStore.cs`
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs`
- `src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs`
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`
- `src/Hexalith.Memories.Server/Infrastructure/TenantIndexReadinessVerifier.cs`
- `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs`
- `src/Hexalith.Memories.Server/Ingestion/DaprWorkflowPayloadStore.cs`
- `src/Hexalith.Memories.Server/Ingestion/GraphPublicationReadGate.cs`
- `src/Hexalith.Memories.Server/Ingestion/GraphPublicationRepair.cs`
- `src/Hexalith.Memories.Server/Ingestion/GraphSourcePublication.cs`
- `src/Hexalith.Memories.Server/Ingestion/IngestionIdentityGuard.cs`
- `src/Hexalith.Memories.Server/Ingestion/IngestionPayloadClaimCheck.cs`
- `src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowStatusMapper.cs`
- `src/Hexalith.Memories.Server/Ingestion/RefreshPublicationStore.cs`
- `src/Hexalith.Memories.Server/Ingestion/SourceInventoryFailureReconciler.cs`
- `src/Hexalith.Memories.Server/Ingestion/SourceInventorySnapshot.cs`
- `src/Hexalith.Memories.Server/Ingestion/SourceInventoryStore.cs`
- `src/Hexalith.Memories.Server/Ingestion/SourcePublicationVisibility.cs`
- `src/Hexalith.Memories.Server/Ingestion/TenantIngestionOwnership.cs`
- `src/Hexalith.Memories.Server/Ingestion/WorkflowPayloadLedger.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs`
- `src/Hexalith.Memories.Server/Search/NaturalLanguageSemanticSearchService.cs`
- `src/Hexalith.Memories.Server/Search/SemanticSearchService.cs`
- `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs`
- `src/Hexalith.Memories.Server/Workflows/AnnotationProjectionWorkflow.cs`
- `src/Hexalith.Memories.Server/Workflows/Contracts/IndexInput.cs`
- `src/Hexalith.Memories.Server/Workflows/Contracts/NaturalLanguageIndexInput.cs`
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`
- `src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/InventoryStalledStream.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/InventoryStreamingContent.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/SourceInventoryClientTests.cs`
- `tests/Hexalith.Memories.DomainService.Tests/Hexalith.Memories.DomainService.Tests.csproj`
- `tests/Hexalith.Memories.DomainService.Tests/MemoriesAggregateAdapterTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/RefreshPublicationIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/SourceInventoryIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/SourceInventoryProducerIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/TenantIngestionLifecycleIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexNaturalLanguageSemanticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticChunksActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/IngestionInputValidatorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/PersistFailedUnitActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Restore/RestoreDataPlaneActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Restore/RestoreReindexUnitActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Architecture/IngestionWorkflowDeterminismGuardTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/IngestionStatusEndpointAuthorizationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/IngestionEndpointE2ETests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Hosting/MemoriesGatewayCapturingHandler.cs`
- `tests/Hexalith.Memories.Server.Tests/Hosting/MemoriesGatewayCompositionTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Hosting/TenantIngestionCompositionTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Infrastructure/AtomicPublicationWriteFixture.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/TenantDeletionWorkflowTests.cs`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-24 | create-story | Adopt approved parent implementation at its stable handoff for owner governance; earlier pre-adoption deltas are not reconstructed. | Runner-observed adoption baseline: source Server 3031 cases, CLI/client 505 cases, Contracts 596 cases, domain host 12 cases, real adapters 9 cases; commands above and parent receipt. Phase delta +0; cumulative +0 from adoption. | matched 93/93 current owner paths |
| 2026-10-24 | dev-story | Record stable implemented source and executed source/package/live boundaries; independent review remains pending. | Same adoption discovery units and filters: phase delta +0; cumulative +0. Source Server 3031, CLI/client 505, Contracts 596, domain host 12, real adapters 9 cases; executed results above. | matched 93/93 current owner paths |
| 2026-10-24 | dev-story | Implement review-loop lifecycle, graph coherence, refresh admission recovery and delayed-writer corrections; retain prior evidence as historical. | Real-backend discovery: adopted 9 to final 21, delta +12; source Server 3030 passed/1 skip; client 505; Contracts 596; domain 12; source/package/live boundaries in parent review1 receipt. | matched 109/109 current owner paths |

| 2026-10-24 | dev-story | Correct loop2 discarded outputs, original admission binding, annotation provenance and bounded native/HTTP work; add actual callers and retain prior receipts as historical. | Same adoption units: source Server 3031 to 3032 discovery (+1), CLI/client 505 to 514 (+9), Contracts 596 unchanged, domain 12 unchanged, real adapters 21 to 26 (+5). Phase delta +15; cumulative +27 from adopted Server3031/CLI505/Contracts596/domain12/adapters9. Final executed source: Server3031 passed/1 existing skip, CLI514/514, Contracts596/596, domain12/12, adapters26/26. Final package Server3030 passed/1 existing skip, CLI514/514 and domain12/12; owned wrapper/child1/1 each with zero skips. Modes and publication/deployment gates remain separate in the final loop2 receipt. | matched 116/116 current owner paths |

| 2026-10-24 | dev-story | Correct loop3 maintenance materialization, tenant teardown ownership, admission bounds and direct caller gaps; retain prior evidence as historical. | Same discovery units: source Server3032 to3038 (+6), client514 unchanged, Contracts596 unchanged, domain12 unchanged, real adapters26 to42 (+16). Phase delta+22; cumulative+49 from adoption. Executed final source Server3037 passed/1 existing skip; adapters42/42; package Server3036 passed/1 existing skip. Owned wrapper/child1/1 each without skips; full details in parent loop3 receipt. | matched125/125 current owner paths |

## Independent Review

Pending parent-directed independent review of the full root and owner diff. Status is review while that review is triaged and required corrections are verified.

## Review-loop implementation correction

The preserved source/package/live evidence above is historical after these corrections. Review-loop 1 adds scheduling/candidate/claim-check provenance; physically reconciles independently proven failed and unscheduled producers; leaves missing and terminated workflow state unresolved; retains durable deletion receipts and aliases; journals graph publication atomically with Redis authority and guards both graph readers with a publication epoch. Refresh admission exposes its explicit protocol, authoritative revision and immutable body binding so lost-ack recovery cannot qualify ordinary ingestion. Each domain adapter now has its own named file. No package/catalog version changes or deployed operations were performed. The parent owns final independent review, scope reconciliation, commitlint validation and owner-first publication.

The payload ledger retains every claim-check reference before save and a separate durable write token until affirmative Dapr acknowledgement. Purge fences the unit and refuses terminal absence while any write completion is unresolved; no timeout discards authority. Ordinary acknowledged writes preserve behavior. Actual failure persistence now atomically checks the unit tombstone so a delayed failure activity cannot recreate personal failed metadata or its case index after deletion. The original public three-argument Dapr payload-store constructor remains available; production DI composes the ledger-aware adapter.

Final review-loop receipt: parent `tests/five-memories-inventory-and-publication-2026-10-24/review1-verification.json`. Real backend controls 21/21; owned wrapper/child 1/1 each, zero skips; source Server 3030 passed/1 existing skip; package Server 3029 passed/1 existing skip; client 505/505 and domain host 12/12 in both modes; Contracts 596/596. No package/catalog versions changed. Independent review is still pending; this artifact does not claim parent signoff.

Review-loop 2 retains discarded candidates and physically reconciles them before source terminal proof, including failed producer completion after durable output acknowledgement loss. Annotation execution history can prove Unscheduled only before any child attempt. Public SDK inventory reads use bounded streamed bytes plus the existing total HTTP timeout, and exact deletion receipts are batched. Modern publication keys retain writer provenance; bounded legacy native discovery atomically seals its complete key set, including interrupted partial registration. The actual NL TopK caller and both ordinary dedup RunAsync callers qualify their production integration. Final source details and the separate package/live/browser/publication controls belong to the parent loop2 receipt; no reviewer or deployed signoff is inferred here.

Final loop2 parent receipt: `tests/five-memories-inventory-and-publication-2026-10-24/review2-verification.json`. It binds 19 XML receipts, 75 passing matrix references and 15 zero-warning/error builds. Selected source Server3031 passed/1 existing skip, CLI514/514, Contracts596/596, domain12/12 and real adapters26/26; package Server3030 passed/1 existing skip, CLI514/514 and domain12/12. The owned wrapper and child each pass1/1 without skips; physical absence, actual persisted domain replay and unrelated survivors remain verified. Private owned disposal and both independent remote teardown receipts report success; exact-session process/container and selected AppHost scans report zero. File Scope/File List remain exactly116/116. Parent-led formal independent review and owner-first Git publication remain pending; this record supplies no human signoff or deployed qualification.


Review-loop 3 preserves every prior control and adds exact maintenance-key retention, bounded tenant ownership and permanent replay fencing. Tenant data teardown precedes backend teardown and also runs when the native registry is already absent. Pending claim-check saves and each independent graph invocation prevent cleanup success; explicit graph-command ACL non-execution can settle, while timeouts and ambiguous acknowledgements remain fenced. Admission bounds match public authorization/readers (tenant128, source4096, other identities512) and reject controls before durable work. Original IndexGraphActivity and DeleteTenantDataKeysActivity constructor metadata shapes are retained; production DI selects the tenant-aware overloads. Actual Dapr/Redis/FalkorDB controls, overlapping same-unit writes and the public SDK boundary control execute in the loop3 adapter lane. Earlier evidence remains historical after these changes. Parent owns independent review and Git publication.

Final loop3 parent receipt: `tests/five-memories-inventory-and-publication-2026-10-24/review3-verification.json`. It binds19 XML receipts,107 passing matrix references and15 zero-warning/error builds. Source/package/live/browser and immutable publisher gates stay separate. Parent independently reviewed the implementation delta and owns formal review, commit validation and publication; this implementation artifact does not claim signoff.
