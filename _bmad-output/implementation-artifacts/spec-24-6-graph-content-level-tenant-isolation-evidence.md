---
title: 'Story 24.6: Graph Content-Level Tenant Isolation Evidence'
type: 'feature'
created: '2026-08-12'
status: 'done'
review_loop_iteration: 0
baseline_commit: '0ecdffed0b131d05816306da1c7061eb88bda5bf'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-24-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `GraphIsolation` proves only that a tenant-named FalkorDB database exists, while Story 24.6's current test incorrectly treats that structural result as content-isolation proof.

**Approach:** Build a real two-tenant collision fixture, traverse both authenticated contexts, and reject foreign node/edge markers. Keep runtime verification read-only and label `GRAPH.LIST` as structural evidence pointing to that integration proof.

## Boundaries & Constraints

**Always:** Seed identical IDs/topology in both tenant databases with distinct payload markers; assert the graph-scoped edge IDs collide. Traverse authenticated HTTP routes for both tenants and assert own-marker presence plus foreign-marker absence. Preserve `GraphIsolation`, canonical search negatives, and Story 20.2 denial-before-dependency evidence.

**Ask First:** Public response-shape changes, runtime content scans, production mutations, or accepting a real-backend blocker.

**Never:** Treat `GRAPH.LIST` or mocks as content proof; bypass authenticated traversal; add ACL, resource-isolation, vector, semantic-family, or remediation scope.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Colliding graphs | Same node/edge IDs and shape; distinct markers | Both traversals return only local markers | Foreign/missing markers fail |
| Structural check | Target appears in `GRAPH.LIST` | Passes as structural-only and cites content proof | Missing database fails closed |
| Backend unavailable | Required proof cannot run | Story stays non-done | Human-accepted blocker must record owner, consequence, boundary, and reopen trigger |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` -- replace the placeholder collision test; remove the misleading all-axis test after canonical negatives pass.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- reuse tenant provisioning, FalkorDB, HTTP client, and route-derived authentication; read-only.
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs`, `Contracts/V1/TraversalEdgeInfo.cs` -- reuse parameterized seeding and traversal-visible `VerifiedBy`; read-only.
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` and its tests -- retain `GRAPH.LIST` only; pin structural wording, proof citation, and no `GRAPH.QUERY`.
- `docs/operations/{tenant-onboarding-offboarding,route-surface}.md` and `OperationalRunbookSetTests.cs` -- document and guard the evidence boundary.
- Canonical tenant/search integration tests and `ServerEndpointAuthorizationTests.cs` -- required read-only evidence unless execution exposes a defect.

## Tasks & Acceptance

**Execution:**
- [x] `TenantIsolationIntegrationTests.cs` -- seed two databases in identical order, place bounded node markers first and edge markers in `verifiedBy`, assert relationship-ID collision, traverse both authenticated contexts, and reject foreign IDs/markers.
- [x] `TenantIsolationIntegrationTests.cs` -- delete the redundant all-axis verifier test after the four narrower search-isolation tests execute successfully.
- [x] `TenantIsolationVerifier.cs` and tests -- make the structural-only boundary explicit without content queries or contract changes; remove dead graph-query setup.
- [x] Operator runbooks and guards -- cite the exact focused real-backend method/command and keep authenticated canary traversal separate from structural verification.
- [x] Story evidence -- attach collision, canonical-search, and denial test results; do not close without real-backend proof or an accepted blocker.

**Acceptance Criteria:**
- Given tenant A and B contain identical graph identifiers and shapes with distinct payload markers, when each is traversed through its authenticated tenant context, then every returned node and edge is fixture-local and zero foreign markers appear.
- Given the collision fixture passes and the four named axis-specific negatives are verified, when evidence is recorded, then the redundant all-axis verifier test is absent.
- Given runtime `GraphIsolation` uses `GRAPH.LIST`, when verifier output and operator docs describe it, then they call it structural database-existence evidence and cite the focused content-level proof without adding a runtime content scan.
- Given the integration lane cannot run, when completion is evaluated, then status remains non-done unless a human accepts a blocker recording owner, consequence, proof boundary, and reopen trigger.

## Spec Change Log

- 2026-08-12: Implemented the graph-collision proof, structural-only runtime wording and guards,
  canonical-negative cleanup, operator documentation, and attached verification evidence.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` -- expected: clean Debug build.
- `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` -- expected: real FalkorDB collision proof passes with this machine's active local Dapr placement/scheduler mappings.
- `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.GraphScopedSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.SyntacticSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.SemanticSearchIntegrationTests` -- expected: canonical negatives pass with this machine's active local Dapr placement/scheduler mappings.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false && DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Deployment.OperationalRunbookSetTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` -- expected: verifier, doc, and denial-before-dependency gates pass.

**Results (2026-08-12):**

- Integration Debug build passed with 0 warnings and 0 errors.
- Real-backend collision proof passed after review hardening: 1 total, 0 failed, 0 skipped,
  233.919 seconds. The local
  Dapr services were exposed on host ports 6050/6060, so the passing invocation supplied
  `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050` and
  `MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060`; Aspire provisioned the fixture-owned
  FalkorDB and both authenticated traversal requests completed against it.
- Canonical tenant/search negatives passed after review hardening: 63 total, 0 failed, 0 skipped,
  241.463 seconds, using the same local Dapr service-address prerequisites. This reverified the four
  canonical classes after the previously gated removal of the redundant all-axis test.
- Server verifier, runbook, and denial-before-dependency gate passed after a clean Debug build:
  54 total, 0 failed, 0 skipped, 7.693 seconds. The increased count includes the authenticated
  traversal-path denial-before-dependency case added during review.
- The exact Epic 24/Epic 25 checklist structural preservation command from
  `spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md` exited 0,
  proving one checklist heading and exactly one row for each of the six named invariants in both contexts.

**Tenant-isolation negative evidence:**

- Affected surfaces: tenant-scoped FalkorDB databases and authenticated
  `GET /api/v1/tenants/{tenantId}/traverse` responses.
- Collision test: `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`.
- Seeded boundary: identical node IDs, edge type, topology, timestamp, and insertion order in both
  tenant databases; distinct bounded node-content/source markers and `verifiedBy` edge markers.
- Assertions: graph-scoped relationship IDs collide; each traversal is complete and non-degraded,
  returns only the two fixture IDs, preserves the primary path and exact outgoing/incoming
  `CausedBy` relationship views, exposes its own node/edge markers and no gap markers, and contains
  zero foreign markers.
- Backend: real Aspire-provisioned FalkorDB; no mocked graph-content result.
- Structural matrix row: `TenantIsolationVerifierTests.VerifyAsync_GraphIsolation_IsStructuralOnlyAndCitesContentProof`
  ran in the passing server class gate and asserts that the complete FalkorDB `ExecuteAsync` command
  set is exactly three `GRAPH.LIST` calls, plus the independent proof citation.
- Backend-unavailable matrix row: `TenantIsolationVerifierTests.VerifyAsync_BackendUnavailable_ReturnsFailedCheckNotException`
  ran in the passing server class gate. The first live collision attempt also exercised the procedural
  proof boundary: the spec remained `in-progress` while the real-backend test body could not run.
- Environment note: the first invocation without the machine-specific Dapr addresses failed during
  shared fixture startup (1 failed, 307.993 seconds) because `/alive` remained 503 while the sidecars
  targeted unused default ports 50005/50006. The test body did not run on that attempt. Supplying the
  addresses of the already-running local placement/scheduler services made the required backend proof
  runnable and produced the passing result above; no production or fixture workaround was added.

## Suggested Review Order

**Content-isolation proof**

- Start with the collision fixture, authenticated traversals, and exact local topology assertions.
  [`TenantIsolationIntegrationTests.cs:65`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L65)

- Review bounded graph seeding and inspection used only by the real-backend fixture.
  [`TenantIsolationIntegrationTests.cs:165`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L165)

**Runtime evidence boundary**

- Confirm runtime verification remains structural-only and requires independent content proof.
  [`TenantIsolationVerifier.cs:305`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L305)

- Verify fixed-token cross-tenant traversal is denied before backend dependencies.
  [`ServerEndpointAuthorizationTests.cs:75`](../../tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs#L75)

**Operator and contract safeguards**

- Check build-first proof instructions and portable Dapr endpoint discovery guidance.
  [`tenant-onboarding-offboarding.md:153`](../../docs/operations/tenant-onboarding-offboarding.md#L153)

- Inspect structural wording, exact command, and source-level query guards.
  [`OperationalRunbookSetTests.cs:372`](../../tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs#L372)

- Finish with exact `GRAPH.LIST` call-set and proof-citation assertions.
  [`TenantIsolationVerifierTests.cs:335`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L335)
