---
title: 'Complete six deferred bootstrap, graph, and mapping items'
type: 'bugfix'
created: '2026-11-27'
status: 'done'
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

- [x] DW-650. Stub the kubectl process across the real `Invoke-DisposableOpenBaoBootstrap` path. Given valid bootstrap responses and scoped credentials, when bootstrap completes, then it records the expected stages and ownership, seeds each intended prefix, verifies isolation, and revokes root. Given stage failures, when invoked, then it stops downstream work and retains safe evidence of the failed stage. Extend the existing production deployment evidence tooling tests.
- [x] DW-653. Update the tenant collision proofs and reuse the lifecycle delete-and-wait behavior. Given test-owned provisioned tenants, when either proof completes or throws, then cleanup attempts all owned tenants and waits for removal from the topology.
- [x] DW-659. Update `IngestionPipelineTests.cs`. Given two active tenants, when REST ingestion writes a marked source for one, then the source is present in its persisted graph and absent in the other tenant's persisted graph.
- [x] DW-681. Update `TenantEmbeddingConfigProvider.cs` and provider tests. Given an in-flight actor read, when the caller cancels first, then the read honors cancellation and its result never seeds the cache. Given a later successful request, then it fetches fresh configuration and preserves normal cache behavior.
- [x] DW-684. Update `AspireIngestionPipelineFixture.cs` and add fixture tests. Given a fully installed client generation, when replacement construction fails after allocating disposable resources, then those candidates are disposed and generation, endpoint, and live clients stay intact.
- [x] DW-689. Update `DaprAggregateCaseMappingStore.cs` and storage tests. Given two mappings for a tenant without an index, when both attempt index creation, then the winner is preserved and the loser retries with the current index so both types remain discoverable.
- [x] Update `deferred-work.md` alongside each completed entry. Given a skipped prerequisite or an obsolete item, then the ledger records the reason.

## Implementation Notes

- The native bootstrap stub verifies generated inputs, scoped read paths, redacted failures, and root revocation.
- The collision tests delete owned tenants after success and after an injected failure. The ingestion test checks persisted state after workflow completion and cleans up its control tenant.
- Cancellation is checked before cache publication. Aggregate index creation uses FirstWrite and reconciles an ambiguous response before removing an unindexed mapping.
- The full run exposed stale cleanup wording in the runbook guard. It now verifies the documented lifecycle cleanup. The URL denial test compares workflow instance identities so ordinary setup-key changes cannot hide a newly scheduled workflow.

## Spec Change Log

## Review Triage Log

| Review finding | Verdict | Disposition |
| --- | --- | --- |
| Blind: provisioning 404 may precede registry creation | medium | Retry absence for attempted, unconfirmed provisioning. |
| Blind: shared waiter aborts on a transient status error | medium | Preserve bounded polling on nonterminal responses. |
| Blind: the added control tenant outlives the test | medium | Delete it through the fixture cleanup path. |
| Blind: ingestion assertion can precede a later graph write | medium | Observe successful workflow completion before reading the control graph. |
| Blind: canceled caller token can prevent index-failure rollback | medium | Compensate under an independent deadline and inspect ambiguous writes. |
| Blind: the recovery read can already contain the aggregate | medium | Treat observed membership as success and test the committed-write error. |
| Blind: the verifier's separate lookup deadline leaves the caller token live | medium | Record the independent deadline propagation gap for follow-up. |
| Blind: native bootstrap diagnostics can include private material | medium | Redact the propagated failure and verify an early failing command. |
| Blind: stub accepts unwired files and unexpected publication names | medium | Check material, wiring, targets, and exact Secret names. |
| Blind: scope inventory omitted five items | low | Record the complete set of changed files. |
| Edge: cleanup accepts absence before registration | medium | Require a resolved outcome or fail cleanup in the unconfirmed case. |
| Edge: a transient 5xx breaks the shared deletion wait | medium | Retain the previous retry behavior. |
| Edge: same-workflow graph writes can follow the probe | medium | Check after terminal success. |
| Edge: custom candidate transport Dispose could throw | low | The production fixture creates standard HTTP handlers. The synthetic failure requires an additional custom disposal fault. |
| Edge: indexing cancellation can strand the already saved map | medium | Verify persisted state and allow bounded compensation. |
| Edge: older writers omit the Redis FirstWrite marker | medium | Record the rolling-upgrade limitation with the pinned Redis component behavior. |
| Verification: collision wrapper failure path was only structural | medium | Exercise cleanup after an exception escapes the proof callback. |
| Verification: foreign-graph read was earlier than workflow completion | medium | Verify terminal workflow status before the graph query. |
| Verification: client and actor cancellation are exercised separately | maybe-false | The pinned SDK supplies the registered actor with the app request's abort token. A live sidecar with a deliberately blocked state read would settle end-to-end delivery. |
| Verification: artifact file scope covers only the cancellation commit | low | Expand the inventory to every item path. |
| Blind follow-up: cancellation can arrive during cache bookkeeping | medium | Check caller cancellation immediately before publication and exercise that boundary. |

## Verification

- Run the production deployment tooling tests and focused .NET test classes.
- Build the solution and run all listed test projects individually, including the fast and nightly integration projects and benchmarks.
- Run commitlint on each complete commit message before and after committing.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-six-deferred-bootstrap-graph-and-mapping.md`
- `docs/operations/tenant-onboarding-offboarding.md`
- `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs`
- `src/Hexalith.Memories.Server/Actors/ITenantConfigurationActor.cs`
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs`
- `src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DaprAggregateCaseMappingStoreTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/DaprClientRotationTrackingHandler.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestionPipelineTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/UrlIngestionPrivateHostDenialIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantDeletionIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/CancellationTrackingActorHandler.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/TenantEmbeddingConfigProviderTests.cs`
- `tests/tooling/production_deployment_evidence/fixtures/openbao_bootstrap_kubectl.py`
- `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py`
- `tools/production-deployment-openbao.ps1`

## Cross-Tenant Negative Evidence

**Surfaces:** The typed tenant configuration actor and provider cache.
**Tests:** `TenantEmbeddingConfigProviderTests.GetAsync_DifferentTenants_DoNotShareCachedConfig` and `TenantConfigurationIntegrationTests.PutEmbeddingConfig_RateLimitChange_PropagatesToRateLimiterOnNextIngest`.
**Command:** `dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net11.0/Hexalith.Memories.IntegrationTests.dll -class '*.TenantConfigurationIntegrationTests'`.
**Result:** All nine live configuration tests passed. The rate-limit test reads both tenant actors and asserts that updating the first preserves the second tenant’s embedding configuration. The focused Server suite also passed the cache isolation case.
