---
baseline_commit: e902181dcdce599187e74fd2c3c9b12f995dcc18
approved_change: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md
---

# Story 24.6: Graph Content-Level Tenant Isolation Evidence

Status: backlog

Owner: Murat / Test Architect and Developer

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

Affected tenant-sensitive surfaces are the tenant verification endpoint, FalkorDB database routing, graph traversal fixtures, and cross-tenant search integration evidence. Completion must retain Story 20.2 denial-before-dependency evidence and Story 24.3 fail-closed verifier evidence. Planned results are `pending`; this story cannot move to `done` until real-backend evidence executes.

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
- **Command:** `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests -class Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests` plus the focused Story 20.2 server authorization command below.
- **Result:** pending — this is a backlog evidence contract; all named real-backend and denial-before-dependency cases must execute and pass before `done`.

## Planned Verification

| Focused evidence | Command | Required result | Status |
| :--------------- | :------ | :-------------- | :----- |
| Collision-shaped graph fixture | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` | Real FalkorDB traversal returns zero foreign node and edge markers for both tenants. | pending |
| Canonical search isolation negatives | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.GraphScopedSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.SyntacticSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.SemanticSearchIntegrationTests` | All declared cross-tenant search cases pass. | pending |
| Story 20.2 denial-before-dependency | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` | Cross-tenant verification/search requests are denied before dependencies where applicable. | pending |
