---
baseline_commit: 0ecdffed0b131d05816306da1c7061eb88bda5bf
approved_change: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md
---

# Story 24.6: Graph Content-Level Tenant Isolation Evidence

Status: done

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
| Current graph/search integration-test bodies | `anti-template` | Treat their actual bodies as the current problem baseline, not as proof of their names. |
| PRD NFR8 graph fixture | `current-narrow-pattern` | Reuse the exact identical-structure and colliding-edge outcome. |
| Story 20.2 | `current-narrow-pattern` | Re-run its denial-before-dependency pattern for any changed tenant entry surface. |

## Slice Proof

- One independently demonstrable outcome: real graph content-isolation evidence.
- Demonstration boundary: the collision-shaped FalkorDB fixture and the named graph traversal assertions pass while the existing axis-specific search negatives remain green.
- Excluded: Redis ACL enforcement, graph resource isolation, vector dimensions, semantic key classification, and tenant-marker remediation.

## Dev Notes

Runtime `GraphIsolation` remains a structural target-database diagnostic. Content-level NFR8 proof belongs to the real integration fixture, not a production verifier scan. The redundant all-axis test was removed after these canonical search negatives were cited and passed:

- `TenantContextEnforcementIntegrationTests.Search_CrossTenantScope_ReturnsZeroResultsFromOtherTenant`
- `GraphScopedSearchIntegrationTests.SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults`
- `SyntacticSearchIntegrationTests.SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults`
- `SemanticSearchIntegrationTests.SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults`

remediation runtime checklist: not applicable — no workflow/runtime surface touched. Independently
re-derived by code review 2026-08-12 from the diff: the only production change is a doc comment and
the `GraphIsolation` `Details` string; there is no change to Dapr workflow orchestration, child-workflow
invocation, activity registration, cleanup/deletion/compensation/dedup of shared or tenant-scoped
state, or migration/rollback/staging markers. Category 5 (File List reconciliation) is discharged by
the Change Log below.

Affected tenant-sensitive surfaces are the tenant verification endpoint, FalkorDB database routing, graph traversal fixtures, and cross-tenant search integration evidence. Completion retains Story 20.2 denial-before-dependency evidence and Story 24.3 fail-closed verifier evidence. The real-backend collision and canonical search lanes executed successfully on 2026-08-12; the story is ready for review.

### Epic AC Verification

Verified 2026-08-04 against `e902181dcdce599187e74fd2c3c9b12f995dcc18`. Independently re-derived
2026-08-12 by code review against the post-patch worktree, because the reviewed diff changed the
subject of row 2 (`TenantIsolationVerifier.cs`). Every command below was re-run; all three verdicts
hold. `Class` uses the four canonical claim classes from `_bmad/custom/epic-ac-verification.md`
(quantitative, existence/absence, behavioral, location); the earlier `requirement`/`implementation`
labels were not policy vocabulary.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| PRD NFR8 requires identical graph structures in tenants A and B, tenant-A traversal, and zero tenant-B nodes even when edge IDs collide. | existence/absence | `rg -n 'identical graph structures|edge IDs collide' _bmad-output/planning-artifacts/prd.md` | Re-derived 2026-08-12: `prd.md:971` still carries the claim verbatim. | confirmed |
| Story 24.3 left `GraphIsolation` as target database-existence evidence. | behavioral | `rg -n 'GRAPH\\.LIST|GRAPH\\.QUERY' src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` | Re-derived 2026-08-12 post-patch: three `GRAPH.LIST` calls (lines 116, 315, 358), zero `GRAPH.QUERY`. | confirmed |
| Current axis-specific cross-tenant search tests provide narrower canonical search evidence. | existence/absence | `rg -n 'Search_CrossTenantScope_ReturnsZeroResultsFromOtherTenant|SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults' tests/Hexalith.Memories.IntegrationTests` | Re-derived 2026-08-12: all four named tests exist (TenantContextEnforcement:155, Semantic:119, Syntactic:108, GraphScoped:118). | confirmed |

## Cross-Tenant Negative Evidence

- **Surfaces:** Tenant verification endpoint, FalkorDB tenant database routing, graph traversal fixtures, and graph/search result attribution.
- **Tests:** `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`, `TenantContextEnforcementIntegrationTests.Search_CrossTenantScope_ReturnsZeroResultsFromOtherTenant`, `GraphScopedSearchIntegrationTests.SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults`, `SyntacticSearchIntegrationTests.SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults`, `SemanticSearchIntegrationTests.SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults`, and `ServerEndpointAuthorizationTests.TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState` (the theory carrying the `/traverse` cross-tenant denial rows; `SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies` covers the search axes and is retained as Story 20.2 evidence).
- **Command:** `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`; the build-first, canonical-search, and server authorization commands are in the verification table below.
- **Result:** passed 2026-08-12 against the real Aspire-provisioned FalkorDB topology, then re-run independently by code review after review patches. Post-review measured results: the full `TenantIsolationIntegrationTests` class 6/6 passing in 17.572s (the review widened the recorded lane from `-method` to `-class`); the four canonical tenant/search classes 63/63 passing in 191.626s; the server verifier/runbook/denial gate 58/58 passing in 7.385s; the full `Hexalith.Memories.Server.Tests` assembly 2799 passing, 1 skipped, 0 failed in 18.803s; and `CiTestInventoryTests` 65/65 passing after the deferred-work schema repair. The pre-patch collision method alone passed 1/1 in 175.204s. The linked implementation spec retains complete proof-boundary and environment evidence.

## Verification

| Focused evidence | Command | Owner | Required result | Observed result | Review status |
| :--------------- | :------ | :---- | :-------------- | :-------------- | :------------ |
| Integration Debug build | `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` | Murat | Clean build, 0 warnings and 0 errors. | Clean build, 0 warnings and 0 errors. | passed 2026-08-12 |
| Server.Tests Debug build | `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` | Murat | Clean build, 0 warnings and 0 errors. | Clean build, 0 warnings and 0 errors. | passed 2026-08-12 |
| Collision-shaped graph fixture (AC1) | `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=<placement> MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=<scheduler> DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` | Murat | Real FalkorDB traversal returns complete, non-degraded local topology and zero foreign node/edge markers for both authenticated tenants. | 1 total, 0 failed, 0 skipped, 175.204 seconds. | passed 2026-08-12 |
| Full owning integration class (review widening) | `... dotnet exec ... -class Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests` | Code review | All tests in the rewritten class pass, not only the collision method. | 6 total, 0 failed, 0 skipped, 17.572 seconds. | passed 2026-08-12 |
| Canonical search isolation negatives (AC2 precondition) | `... dotnet exec ... -class ...Tenants.TenantContextEnforcementIntegrationTests -class ...Search.GraphScopedSearchIntegrationTests -class ...Search.SyntacticSearchIntegrationTests -class ...Search.SemanticSearchIntegrationTests` | Murat | All declared cross-tenant search cases pass before and after the all-axis test removal. | 63 total, 0 failed, 0 skipped, 191.626 seconds. | passed 2026-08-12 |
| Story 20.2 denial-before-dependency (AC1 auth boundary) | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` | Murat | Cross-tenant `/traverse` is denied before tenant state, including missing and blank `startNodeId`. | Included in the 58-case server gate below; all traverse denial rows pass. | passed 2026-08-12 |
| Verifier structural-only boundary (AC3) | `DiffEngine_Disabled=true dotnet exec ...Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests` | Murat | `GraphIsolation` issues only `GRAPH.LIST`, cites the content proof, and fails closed when the target database is absent. | Included in the 58-case server gate below; both the structural-only and new fail-closed cases pass. | passed 2026-08-12 |
| Runbook evidence-boundary guard (AC3 docs) | `DiffEngine_Disabled=true dotnet exec ...Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.OperationalRunbookSetTests` | Murat | Both runbook sections state the structural-only boundary, cite the proof method and build-first command, and carry no machine-specific ports. | Included in the 58-case server gate below; 10 total, 0 failed standalone. | passed 2026-08-12 |
| Combined server gate | `DiffEngine_Disabled=true dotnet exec ...Server.Tests.dll -class ...TenantIsolationVerifierTests -class ...OperationalRunbookSetTests -class ...ServerEndpointAuthorizationTests` | Code review | All three named classes pass together. | 58 total, 0 failed, 0 skipped, 7.385 seconds. | passed 2026-08-12 |
| Full Server.Tests regression | `DiffEngine_Disabled=true dotnet exec ...Server.Tests.dll` | Code review | No regression outside the named classes. | 2799 total, 0 failed, 1 skipped, 18.803 seconds. | passed 2026-08-12 |
| Deferred-work schema gate | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` | Code review | Every `Status: resolved` deferred entry carries an `Evidence:` field. | 65 total, 0 failed, 0 skipped. Failed 2/65 before the review repaired the `Resolution:` field name. | passed 2026-08-12 |
| Story governance gates | `python3 tools/check-story-review-readiness.py --story-key 24-6-graph-content-level-tenant-isolation-evidence`; `python3 tools/check-story-slice-scope.py --story-key ...`; `python3 tools/check-tenant-isolation-evidence.py --story-key ... --changed-files-file <set>` | Code review | All exit 0 with substantive (non-vacuous) output. | All exit 0; readiness is now substantive because this review added the ledger and File List. | passed 2026-08-12 |
| Tooling suites | `python3 -m unittest discover -s tests/tooling/<suite> -p '*_test.py'` for `line_endings`, `integration_fast_coverage`, `story_review_readiness`, `tenant_isolation_evidence` | Code review | All suites pass after the required-surfaces and artifact edits. | 4, 6, 45 and 41 tests respectively; all OK. | passed 2026-08-12 |
| Epic 23 checklist preservation | `for context_doc in _bmad-output/implementation-artifacts/epic-24-context.md _bmad-output/implementation-artifacts/epic-25-context.md; do test "$(rg -c -F '## Review Checklist — Epic 23 Ingestion Invariants' "$context_doc")" = 1 || exit 1; for invariant_name in 'Claim-check workflow payloads' 'Captured workflow configuration' 'Chunked semantic vectors' 'Source-payload retention' 'Tenant index readiness' 'Single-operation admission'; do test "$(rg -c "^\\| $invariant_name \\|" "$context_doc")" = 1 || exit 1; done; done` | Murat | Exit 0 for both epic contexts. | Exit 0, re-confirmed after the review reverted the epic-24 context regeneration. | passed 2026-08-12 |

The local Dapr placement and scheduler services were mapped to `localhost:6050` and `localhost:6060`
rather than their default ports, so the Aspire-backed passing commands explicitly supplied those active
addresses. Generic operator instructions in the runbooks explain how to discover and set active local
service addresses without assuming this machine's mappings.

The PR follow-up exposed a deterministic `integration-fast` baseline defect outside the graph-isolation
implementation: the in-place OpenBao restart refreshed the fixture's raw Dapr client after the sidecar
port rotated but left its actor-proxy factory bound to the retired port. The authorized CI repair now
reconnects both clients and extends the restart test with a post-rotation actor call. A Release build
passed with 0 warnings and 0 errors; the exact restart regression passed 1/1 in 2.5443 minutes; and a
combined run covering that regression plus the two formerly failing actor-dependent classes passed
11 tests with 1 intentional skip in 2.8299 minutes.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-08-12 | create-story | Adoption baseline appended by `code-review` on 2026-08-12. Story 24.6 was authored 2026-08-04 (commit `7b55c62f`) — after the 2026-07-16 phase-ledger policy — but no canonical table was created, so no `create-story` row existed to read. Earlier deltas are not reconstructed. Owner: Murat / Test Architect and Developer. Declared comparison baseline re-set this review to `0ecdffed` (the spec baseline and the true implementation slice); the previous story frontmatter value `e902181d` spanned 24 unrelated commits including the BMAD 6.11.0 migration. | Baseline at `0ecdffed`, **derived arithmetically from the reviewed diff, not from live discovery**: Server.Tests three named classes = **52 test cases**; `TenantIsolationIntegrationTests` = **7 test methods**; the four canonical tenant/search classes = **63 test methods**. Blocker: live discovery at `0ecdffed` would require building a historical worktree, which would need uninitialised `references/` submodules. Owner: code review. Consequence: the create baseline is derived, not runner-observed. Reopen trigger: any dispute over the create-to-dev delta. All later rows are runner-derived. | Not reconstructed for this row; the cumulative set is reconciled at `dev-story` and `code-review` below. |
| 2026-08-12 | dev-story | Implemented the collision-shaped FalkorDB fixture and authenticated dual-tenant traversal, removed the redundant all-axis verifier test after the canonical negatives passed, made the runtime `GraphIsolation` boundary explicit as structural-only with a proof citation, and documented the evidence boundary in both operator runbooks. | Server.Tests three named classes: phase delta **+2 test cases** (52 -> 54; +1 `ServerEndpointAuthorizationTests` InlineData, +1 `OperationalRunbookSetTests` Fact). `TenantIsolationIntegrationTests`: phase delta **-1 test method** (7 -> 6; `VerifyTenant_SearchFromOtherContext_ZeroResultsAcrossAllAxes` removed). Canonical four classes: phase delta **+0 test methods** (63 -> 63). Cumulative story delta at this phase: +2 cases / -1 method / +0 methods. Units are not interchangeable: `54` is theory-expanded xUnit **test cases**, `63` and `6` are **test methods** in a different assembly. Command: `DiffEngine_Disabled=true dotnet exec <assembly> -class <named classes>`. | Not recorded at the time; reconciled by the `code-review` row below. |
| 2026-08-12 | code-review | Adversarial review over `0ecdffed..63387538` with six layers plus independent parent-side re-execution. 4 decisions resolved and 33 patches applied: repaired the `deferred-work.md` schema break that failed 2 CI tests, reverted the lossy `epic-24-context.md` regeneration (restoring the `NFR8` and `D29` anchors and the approved-change text), re-based the story to `0ecdffed`, created this ledger and the File List, pinned the cited proof at method scope in `tools/integration-fast-required-surfaces.txt`, covered the previously untested missing-target-graph fail-closed branch, restored the split verifier test, added `/traverse` denial rows for missing and blank `startNodeId`, relocated both runbook headings out of the positions that hijacked following content, and re-derived the Epic AC table against the post-patch worktree. | Review-patch phase delta: Server.Tests three named classes **+4 test cases** (54 -> 58; +2 `TenantIsolationVerifierTests` methods — the restored `VerifyAsync_SingleTenant_PerformsTargetStructuralChecks` split and the new `VerifyAsync_TargetGraphDatabaseMissing_FailsClosed` — and +2 `ServerEndpointAuthorizationTests` InlineData rows). `TenantIsolationIntegrationTests` **+0 test methods** (6 -> 6). Canonical four classes **+0 test methods** (63 -> 63). Cumulative story delta from the create baseline: **+6 test cases** (52 -> 58), **-1 test method** (7 -> 6), **+0 test methods** (63 -> 63). No external same-lane delta: no other owner changed these discovery lanes between phases, so create baseline + cumulative story delta = observed total in every lane. Commands and observed totals are in the Verification table above; the full `Hexalith.Memories.Server.Tests` assembly is 2799 passing / 1 skipped / 0 failed. | matched 13/13 against baseline `0ecdffed` using `git diff --name-status 0ecdffed`. No exclusions. |
| 2026-08-12 | ci-repair | PR #53 reproduced the same post-OpenBao-restart actor connection failures in two independent GitHub runners. Reconnected the fixture's state client and actor-proxy factory together whenever the primary Dapr endpoint rotates, then required the restart regression to complete a real actor call. | Existing test strengthened, so discovery delta **+0**. Release build: 0 warnings/errors. Exact restart regression: **1 passed**. Combined repair lane: **11 passed, 1 intentionally skipped, 0 failed**. | Added 2 exact forbidden-default test paths to File Scope and File List under the human-authorized CI repair; the story artifact already owned this reconciliation record. |

## File List

Cumulative story-scoped changed-file set, reconciled against `git diff --name-status 0ecdffed`.

- `_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-24-6-graph-content-level-tenant-isolation-evidence.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- `docs/operations/route-surface.md`
- `docs/operations/tenant-onboarding-offboarding.md`
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`
- `tools/integration-fast-required-surfaces.txt`

## File Scope

**Allowed to modify:**

- `_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-24-6-graph-content-level-tenant-isolation-evidence.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- `docs/operations/route-surface.md`
- `docs/operations/tenant-onboarding-offboarding.md`
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`
- `tools/integration-fast-required-surfaces.txt`
