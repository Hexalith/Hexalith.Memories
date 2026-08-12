---
baseline_commit: e902181dcdce599187e74fd2c3c9b12f995dcc18
approved_change: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md
---

# Story 24.6: Graph Content-Level Tenant Isolation Evidence

Status: review

Owner: Murat / Test Architect and Developer

Implementation source: [Story 24.6 implementation spec](spec-24-6-graph-content-level-tenant-isolation-evidence.md).
The approved requirements and history remain canonical here; implementation tasks, review-loop changes,
and command-backed execution evidence are maintained in the linked spec and reconciled below.

## Story

As an operator,
I want executable proof that graph content remains tenant-local when identifiers collide,
so that structural database existence is not mistaken for NFR8 leakage evidence.

## Acceptance Criteria

1. Given tenant A and tenant B contain identical node identifiers, identical graph shapes, and colliding edge identifiers but tenant-distinct payload markers, when each tenant is traversed through its authenticated tenant context, then every returned node and edge belongs to the requested tenant's seeded fixture and zero foreign markers appear.
2. Given `VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`, when this story completes, then it creates the collision-shaped fixture, traverses both tenant contexts, and asserts zero foreign node and edge markers. The redundant `VerifyTenant_SearchFromOtherContext_ZeroResultsAcrossAllAxes` test is removed after the current axis-specific cross-tenant search tests are cited and verified.
3. Given runtime `GraphIsolation` checks target database existence through `GRAPH.LIST`, when verifier and operator documentation describe the result, then they label it structural evidence and cite the focused integration command for content-level leakage proof; runtime verification does not add a graph-content scan.
4. Given the integration lane cannot run, when the story is reviewed, then the story remains blocked or records an accepted blocker with owner, consequence, proof boundary, and reopen trigger; unit mocks, names, or comments cannot substitute for NFR8 graph evidence.

## Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Story 24.3 | `historical-reference-only` | Preserve its structural verifier and fail-closed evidence; do not reopen or expand its completed slice. |
| Current graph/search integration-test bodies | `current-narrow-pattern` | Treat their actual bodies as the current problem baseline, not as proof of their names. |
| PRD NFR8 graph fixture | `current-narrow-pattern` | Reuse the exact identical-structure and colliding-edge outcome. |
| Story 20.2 | `current-narrow-pattern` | Re-run its denial-before-dependency pattern for any changed tenant entry surface. |

## Slice Proof

- One independently demonstrable outcome: real graph content-isolation evidence.
- Demonstration boundary: the collision-shaped FalkorDB fixture and the named graph traversal assertions pass while the existing axis-specific search negatives remain green.
- Excluded: Redis ACL enforcement, graph resource isolation, vector dimensions, semantic key classification, and tenant-marker remediation.

## Dev Notes

Runtime `GraphIsolation` remains a structural target-database diagnostic. Content-level NFR8 proof belongs to the real integration fixture, not a production verifier scan. The existing redundant all-axis test is removed only after these canonical search negatives are cited and pass:

- `TenantContextEnforcementIntegrationTests.Search_CrossTenantScope_ReturnsZeroResultsFromOtherTenant`
- `GraphScopedSearchIntegrationTests.SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults`
- `SyntacticSearchIntegrationTests.SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults`
- `SemanticSearchIntegrationTests.SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults`

Affected tenant-sensitive surfaces are the tenant verification endpoint, FalkorDB database routing, graph traversal fixtures, and cross-tenant search integration evidence. Completion retains Story 20.2 denial-before-dependency evidence and Story 24.3 fail-closed verifier evidence. The real-backend collision and canonical search lanes executed successfully on 2026-08-12; the story is ready for review.

### Epic AC Verification

Verified 2026-08-04 against `e902181dcdce599187e74fd2c3c9b12f995dcc18`.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| PRD NFR8 requires identical graph structures in tenants A and B, tenant-A traversal, and zero tenant-B nodes even when edge IDs collide. | requirement | `rg -n 'identical graph structures|edge IDs collide' _bmad-output/planning-artifacts/prd.md` | The requirement remains current. | confirmed |
| Story 24.3 left `GraphIsolation` as target database-existence evidence. | implementation | `rg -n 'GRAPH\\.LIST|GRAPH\\.QUERY' src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` | The graph check issues `GRAPH.LIST` and no content query. | confirmed |
| Current axis-specific cross-tenant search tests provide narrower canonical search evidence. | implementation | `rg -n 'Search_CrossTenantScope_ReturnsZeroResultsFromOtherTenant|SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults' tests/Hexalith.Memories.IntegrationTests` | Tenant-context, graph, syntactic, and semantic search negatives exist. | confirmed |

## Cross-Tenant Negative Evidence

- **Surfaces:** Tenant verification endpoint, FalkorDB tenant database routing, graph traversal fixtures, and graph/search result attribution.
- **Tests:** `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`, `TenantContextEnforcementIntegrationTests.Search_CrossTenantScope_ReturnsZeroResultsFromOtherTenant`, and `ServerEndpointAuthorizationTests.SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies`.
- **Command:** `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`; the build-first, canonical-search, and server authorization commands are in the verification table below.
- **Result:** passed on 2026-08-12 against the real Aspire-provisioned FalkorDB topology. After review hardening, the collision lane returned 1/1 passing in 233.919 seconds, the four canonical tenant/search classes returned 63/63 passing in 241.463 seconds, and the clean-build server verifier/runbook/denial gate returned 54/54 passing in 7.693 seconds. The redundant verifier-based test had already been removed after its required pre-removal 63/63 canonical gate. The linked implementation spec retains complete proof-boundary and environment evidence.

## Verification

| Focused evidence | Command | Observed result | Status |
| :--------------- | :------ | :-------------- | :----- |
| Integration Debug build | `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` | Clean build, 0 warnings and 0 errors. | passed 2026-08-12 |
| Collision-shaped graph fixture | `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` | Complete, non-degraded real FalkorDB traversal returned exact local topology and markers for both authenticated tenant contexts; 1 total, 0 failed, 0 skipped, 233.919 seconds. | passed 2026-08-12 |
| Canonical search isolation negatives | `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.GraphScopedSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.SyntacticSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.SemanticSearchIntegrationTests` | All 63 tests passed after review hardening; 0 failed, 0 skipped, 241.463 seconds. | passed 2026-08-12 |
| Story 20.2 denial-before-dependency and structural/runbook gates | `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false && DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Deployment.OperationalRunbookSetTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` | Clean build; verifier/runbook/authorization gate passed 54 total, 0 failed, 0 skipped, 7.693 seconds, including traversal-path denial-before-dependency. | passed 2026-08-12 |
| Epic 23 checklist preservation | `for context_doc in _bmad-output/implementation-artifacts/epic-24-context.md _bmad-output/implementation-artifacts/epic-25-context.md; do test "$(rg -c -F '## Review Checklist — Epic 23 Ingestion Invariants' "$context_doc")" = 1 || exit 1; for invariant_name in 'Claim-check workflow payloads' 'Captured workflow configuration' 'Chunked semantic vectors' 'Source-payload retention' 'Tenant index readiness' 'Single-operation admission'; do test "$(rg -c "^\\| $invariant_name \\|" "$context_doc")" = 1 || exit 1; done; done` | Exit 0; both contexts retain exactly one checklist and exactly one row for every invariant. | passed 2026-08-12 |

The local Dapr placement and scheduler services were mapped to `localhost:6050` and `localhost:6060`
rather than their default ports, so the Aspire-backed passing commands explicitly supplied those active
addresses. Generic operator instructions in the runbooks explain how to discover and set active local
service addresses without assuming this machine's mappings.

## File Scope

**Allowed to modify:**

- `_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/epic-24-context.md`
- `_bmad-output/implementation-artifacts/spec-24-6-graph-content-level-tenant-isolation-evidence.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/operations/route-surface.md`
- `docs/operations/tenant-onboarding-offboarding.md`
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`
