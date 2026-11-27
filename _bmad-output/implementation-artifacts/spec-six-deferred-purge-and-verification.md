---
title: 'Complete six deferred purge and verification items'
type: 'bugfix'
created: '2026-11-27'
status: 'in-review'
route: 'dispatch'
baseline_commit: '049b3d939eb55f65692ccaf299cb66ab1039f39c'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Resume the deferred work register after DW-689 in ledger order. Complete six actionable items through individual subagents and commit each implementation separately. Record reasons for skipping blocked or obsolete work. Build and test the complete solution, repair failures, close the six ledger entries, and push directly to main.

## Boundaries & Constraints

Preserve persisted identities, unaffected tenant data and existing callers. Use isolated fixtures and synthetic credentials. Keep centrally owned workflow and release policy under its owning repository. Use read-only access for existing deployment evidence.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs` keeps a per-tenant mapping index and standalone map, creation-lock and index keys.
- `tests/Hexalith.Memories.EventStore.Tests/FakeDaprStateStore.cs` backs the focused Dapr adapter tests.
- `tools/production-deployment-openbao.ps1` publishes the staged OpenBao bootstrap credentials and holds their temporary files.
- Story 27.3 and the Epic 27 copy of AC6 define deployment verification against staged OpenBao.
- `spec-dw-18-redis-ingest-race-proof.md` records the original reservation race proof.
- The annotation projection scheduler and the workflow replay-safety gate share the workflow runtime. The Redis in-flight registry supports explicit tracking without editing workflow history.

## Tasks & Acceptance

- [x] DW-690. Given a failed case-mapping deletion, when cleanup retries, then all affected map keys remain discoverable until cleanup succeeds.
- [x] DW-697. Given temporary bootstrap material on Unix, when kubectl reads it, then the directory and files allow only their owner to access them.
- [x] DW-698. Given a mutable state value or a conditional state mutation, when the fake store executes the request, then callers see detached values, accurate write preconditions and expiration.
- [x] DW-721. Given the qualifying staged-OpenBao evidence, when a maintainer reads either binding AC6 copy, then both state the same evidence requirement for the retained vault Components.
- [x] DW-724. Given the existing reservation proof and later implementation changes, when reviewed independently, then focused verification and any remaining actionable findings are recorded.
- [x] DW-740. Given an active annotation ingestion child that bypasses the normal scheduler, when the startup drain checks for incompatible in-flight work, then it continues waiting while that child can still execute. Existing workflow histories replay with the same commands.
- [ ] Run the complete build and test inventory, record six verified completions in the ledger, and validate each commit message.

## Implementation Notes

- The preceding batch processed entries through DW-689. All earlier open numbered entries already have skip reasons.
- A focused live-sidecar experiment on daprd 1.19.1 found that a rejected Redis transaction could still change the index and that a missing-ETag transaction upsert could replace an existing row. DW-691 and DW-694 require a compatible atomic substrate or a separate persistence and recovery design. DW-690 clears an assignment with a single-key ETag check. It keeps the empty marker indexed until tenant purge to prevent recycled ETags from invalidating a replacement assignment. Active mapping counts exclude the cleared markers. A separate fix retains purge discovery through failed physical cleanup.
- DW-696 requires central workflow ownership. DW-713 needs the Phase 1.5 successor story. DW-716's historic proof feed is out of CI and the gateway startup proof is blocked locally. Changes already present in `f11ce839` address DW-722 and DW-723. DW-729 was already measured, and a fresh read found the same rules. DW-730, DW-731, DW-733, DW-735 and DW-739 retain their recorded dependencies or reopening triggers.

## Review Triage Log

## Verification

- Build `Hexalith.Memories.slnx` and run all test projects individually, including fast and nightly integration plus benchmarks. Run applicable browser and repository tooling checks.
- Run commitlint on the full message before and after each commit.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-dw-18-redis-ingest-race-proof.md`
- `_bmad-output/implementation-artifacts/spec-six-deferred-purge-and-verification.md`
- `_bmad-output/planning-artifacts/epics.md`
- `docs/operations/upgrade-migration.md`
- `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs`
- `src/Hexalith.Memories.Server/Cases/CaseService.cs`
- `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/DaprCaseProjectionWorkflowScheduler.cs`
- `src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs`
- `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowStateReader.cs`
- `src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs`
- `src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs`
- `src/Hexalith.Memories.Server/Workflows/AnnotationProjectionWorkflow.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DaprAggregateCaseMappingStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/FakeDaprStateStore.cs`
- `tests/Hexalith.Memories.EventStore.Tests/FakeDaprStateStoreTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/EventStore/DaprStateStoreLiveSidecarTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestDedupReservationIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/IngestionEndpointE2ETests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DaprCaseProjectionWorkflowSchedulerTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Hosting/WorkflowReplaySafetyHostedServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowStateReaderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionWorkflowInFlightRegistryTests.cs`
- `tests/tooling/production_deployment_evidence/fixtures/openbao_bootstrap_kubectl.py`
- `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py`
- `tools/production-deployment-openbao.ps1`

## Cross-Tenant Negative Evidence

**Surfaces:** REST ingestion reservation compensation, keyed by tenant, case, and source identity or retry token.
**Tests:** `ServerEndpointAuthorizationTests.TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies`, `IngestDedupReservationIntegrationTests.ReleaseAsync_AfterBoundedRendezvousTimeout_TimesOutAndDeletesOnlyTheReservationKey`, and `IngestDedupReservationIntegrationTests.ReleaseAsync_AfterAReservationExpires_PreservesTheSuccessor`.
**Command:** `dotnet exec <Release Server.Tests assembly> -method '*.TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies'` and `dotnet exec <Release IntegrationTests assembly> -class '*.IngestDedupReservationIntegrationTests'`.
**Result:** All three tenant authorization cases and all three real Redis cases passed. Denial precedes scheduling, and release preserves both the foreign tenant reservation and the newer owner of the same reservation.
