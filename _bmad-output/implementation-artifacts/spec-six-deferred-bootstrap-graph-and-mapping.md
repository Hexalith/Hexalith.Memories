---
title: 'Complete six deferred bootstrap, graph, and mapping items'
type: 'bugfix'
created: '2026-11-27'
status: 'in-progress'
route: 'dispatch'
baseline_commit: 'a161162e5cd1a678024eed6a86176757464d447a'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete the next six actionable entries in ledger order, with an independently validated commit for each. Record applicable reasons for skips. Validate the solution and complete test inventory before pushing directly to `main`.

## Boundaries & Constraints

Preserve existing entry points and persisted identities. Exercise the normal bootstrap, ingestion, tenant lifecycle, and Dapr fixture paths. Use isolated test data and synthetic credentials.

</frozen-after-approval>

## Code Map

- `tools/production-deployment-openbao.ps1` contains the bootstrap orchestration. `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py` already exercises isolated native kubectl invocations with a stub.
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` owns the collision proofs. `TenantDeletionIntegrationTests.cs` demonstrates the authenticated lifecycle endpoint and completion check.
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestionPipelineTests.cs` owns pipeline graph persistence coverage.
- `src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs` creates the configuration actor proxy and populates per-tenant caches.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` creates and swaps disposable Dapr clients.
- `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs` holds the current per-tenant derived mapping index and bounded concurrency retries.

## Tasks & Acceptance

- [ ] DW-650. Stub the kubectl process across the real `Invoke-DisposableOpenBaoBootstrap` path. Given valid bootstrap responses and scoped credentials, when bootstrap completes, then it records the expected stages and ownership, seeds each intended prefix, verifies isolation, and revokes root. Given stage failures, when invoked, then it stops downstream work and retains safe evidence of the failed stage. Extend the existing production deployment evidence tooling tests.
- [ ] DW-653. Update the tenant collision proofs and reuse the lifecycle delete-and-wait behavior. Given test-owned provisioned tenants, when either proof completes or throws, then cleanup attempts all owned tenants and waits for removal from the topology.
- [ ] DW-659. Update `IngestionPipelineTests.cs`. Given two active tenants, when REST ingestion writes a marked source for one, then the source is present in its persisted graph and absent in the other tenant's persisted graph.
- [ ] DW-681. Update `TenantEmbeddingConfigProvider.cs` and provider tests. Given an in-flight actor read, when the caller cancels first, then the read honors cancellation and its result never seeds the cache. Given a later successful request, then it fetches fresh configuration and preserves normal cache behavior.
- [ ] DW-684. Update `AspireIngestionPipelineFixture.cs` and add fixture tests. Given a fully installed client generation, when replacement construction fails after allocating disposable resources, then those candidates are disposed and generation, endpoint, and live clients stay intact.
- [ ] DW-689. Update `DaprAggregateCaseMappingStore.cs` and storage tests. Given two mappings for a tenant without an index, when both attempt index creation, then the winner is preserved and the loser retries with the current index so both types remain discoverable.
- [ ] Update `deferred-work.md` alongside each completed entry. Given a skipped prerequisite or an obsolete item, then the ledger records the reason.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- Run the production deployment tooling tests and focused .NET test classes.
- Build the solution and run all listed test projects individually, including the fast and nightly integration projects and benchmarks.
- Run commitlint on each complete commit message before and after committing.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `src/Hexalith.Memories.Server/Actors/ITenantConfigurationActor.cs`
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs`
- `src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/CancellationTrackingActorHandler.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/TenantEmbeddingConfigProviderTests.cs`
- `_bmad-output/implementation-artifacts/spec-six-deferred-bootstrap-graph-and-mapping.md`

## Cross-Tenant Negative Evidence

**Surfaces:** The typed tenant configuration actor and provider cache.
**Tests:** `TenantEmbeddingConfigProviderTests.GetAsync_DifferentTenants_DoNotShareCachedConfig` and `TenantConfigurationIntegrationTests.PutEmbeddingConfig_RateLimitChange_PropagatesToRateLimiterOnNextIngest`.
**Command:** `dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*.TenantConfigurationIntegrationTests'`.
**Result:** All nine live configuration tests passed. The rate-limit test reads both tenant actors and asserts that updating the first preserves the second tenant’s embedding configuration. The focused Server suite also passed the cache isolation case.
