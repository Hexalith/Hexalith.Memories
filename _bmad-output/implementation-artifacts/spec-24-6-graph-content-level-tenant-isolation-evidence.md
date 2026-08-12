---
title: 'Story 24.6: Graph Content-Level Tenant Isolation Evidence'
type: 'feature'
created: '2026-08-12'
status: 'done'
review_loop_iteration: 1
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

## Review Findings

Code review 2026-08-12 against `0ecdffed..63387538` (12 files, +505/-108). Six adversarial layers
plus parent-side independent re-execution. 46 findings: 4 decision-needed, 29 patch, 11 deferred,
2 dismissed as noise.

Independently reproduced by the reviewer, not accepted on the record alone: the server gate
(`54 total, 0 failed`), the real-backend collision proof (`1 total, 0 failed`, 175.204s),
`check-tenant-isolation-evidence.py` (pass), `check-story-file-scope.py` (pass), all three Epic AC
rows, and all seven Suggested Review Order anchors (no drift). The collision fixture genuinely
proves content-level isolation against a real FalkorDB backend; AC1 holds.

### Decisions resolved 2026-08-12 (Administrator) — all applied

- [x] [Review][Patch] (Decision 1 -> option 1: revert here, re-land separately) Epic-24 context regeneration bundled into the story commit — `epic-24-context.md` was regenerated by the `compile-epic-context` skill (its banner at `.claude/skills/bmad-build/compile-epic-context.md:20` matches byte-for-byte; `epic-27-context.md` got the same treatment in `8d0c1a58`). The generator is an LLM instruction file, not a deterministic script, so the rewrite is lossy and non-reproducible. It deleted the `NFR8` label (1 -> 0) and the `D29` anchor (1 -> 0) that the story is graded against, plus the approved-change text "24.6-24.9 are the canonical verifier-residual backlog homes" established by the proposal named in this story's own `approved_change` frontmatter; it added a new NFR ("nine loaded tenants / 5% / 10-tenant, 100K-units" — faithfully sourced from `prd.md:980` NFR12, verified) and silently changed "malformed, missing, swapped" to "malformed, empty, swapped". The only preservation evidence run covers the Epic 23 checklist table — a different section. Decide: revert here and re-land as its own change, or ratify the deletions.
- [x] [Review][Patch] (Decision 2 -> option 1: re-baseline the story to `0ecdffed`) Baseline conflict blocks the File List fix — story frontmatter declares `baseline_commit: e902181d` (24 commits back, spanning the whole BMAD 6.11.0 migration); the spec declares `0ecdffed` (= `HEAD~1`, the true implementation slice). `check-story-review-readiness.py` reads the *story* frontmatter, so once a `File List` is added its C1 check derives the cumulative set from `e902181d` and fails against unrelated work. C1 is skipped only on default branches; this is a feature branch, so it will run. Decide which baseline governs before fixing the File List.
- [x] [Review][Patch] (Decision 3 -> bundled with Decision 1: drop `epic-24-context.md` from the allow-list on revert) `## File Scope` was authored in the same commit whose out-of-slice edits it authorizes — the section (including `epic-24-context.md`) did not exist before `63387538`. Scope approval cannot be retroactive to the change it permits. Bundled with the epic-context decision above.
- [x] [Review][Patch] (Decision 4 -> option 1: one row per gate with owner and review state) Acceptance criteria plausibly cross the >5-gate checkpoint threshold — the four ACs decompose into ~7 independently verifiable gates, while the `Verification` table has no owner and no review-state column and its row 4 bundles three gates under one status. The executable gate keys on literal `C1`/`C2` identifiers this story does not use, so it cannot judge this.

### Patches — all applied 2026-08-12

- [x] [Review][Patch] `deferred-work.md` breaks CI: `Resolution:` must be `Evidence:` [_bmad-output/implementation-artifacts/deferred-work.md:1894]
- [x] [Review][Patch] Add the canonical `## Change Log` phase ledger with `create-story` and `dev-story` rows [24-6-graph-content-level-tenant-isolation-evidence.md]
- [x] [Review][Patch] Add `## File List` reconciling all 12 in-scope paths as `matched 12/12` [24-6-graph-content-level-tenant-isolation-evidence.md]
- [x] [Review][Patch] Rename the Verification table header `Status` to `Review status` so gate check C6 sees it [24-6-graph-content-level-tenant-isolation-evidence.md:75]
- [x] [Review][Patch] Record same-unit test counts: phase delta, cumulative delta, before/after totals, named unit; `54` is a theory-expanded case count and `63` a method count, presented side by side unlabeled; the deleted test (`TenantIsolationIntegrationTests` 7 -> 6 methods) is recorded nowhere [24-6-...md:70]
- [x] [Review][Patch] Pin the cited proof at method scope in `tools/integration-fast-required-surfaces.txt:14` — the entry is class-only, so deleting/renaming/skipping `VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` leaves `integration-fast` green while the production API and both runbooks keep citing it [tools/integration-fast-required-surfaces.txt:14]
- [x] [Review][Patch] Cover the untested fail-closed branch: all 15 `SetupGraphList` calls include the target tenant, so `if (!graphDatabases.Contains(tenantId))` is never exercised and the spec's "Missing database fails closed" matrix row is unproven [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:320]
- [x] [Review][Patch] Move both runbook `### Graph isolation evidence boundary` headings out of the positions that hijack following content — in `route-surface.md` it splits the route table from its `**Experimental diagnostics (HXL002)**` footnote; in `tenant-onboarding-offboarding.md` it sits inside numbered step 6, pulling onboarding steps 7-8 into the guarded section (154-190, closing only at `### Offboarding`) [docs/operations/route-surface.md:80, docs/operations/tenant-onboarding-offboarding.md:153]
- [x] [Review][Patch] Reconcile status across artifacts: spec `status: 'done'` vs story/sprint `review` vs `epics.md:4599` still `backlog`; `review_loop_iteration: 0` contradicts "after review hardening" [spec:5, epics.md:4599]
- [x] [Review][Patch] The DW entry is resolved while its owning story is only at `review`, and its `Re-open trigger` ("Story 24.6 is selected") names the event that already fired [deferred-work.md:1894]
- [x] [Review][Patch] Cross-Tenant Negative Evidence names `ServerEndpointAuthorizationTests.SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies`, but the traverse case was added to `TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState`; the cited test does not cover `/traverse` [24-6-...md:68]
- [x] [Review][Patch] Run and record `-class TenantIsolationIntegrationTests` — the file changed by 191 lines but only `-method` was executed, leaving five sibling real-backend tests with build-only evidence [24-6-...md:77]
- [x] [Review][Patch] Close the open DW entry requiring the three axis-specific search classes in the story's `Tests:` bullet — the gate it guarded was spent when the all-axis test was removed [deferred-work.md, spec-epic-24-verifier-residual-backlog entry]
- [x] [Review][Patch] Re-derive the Epic AC Verification table: `Class` values `requirement`/`implementation` are not the policy's four classes, and the header still reads "Verified 2026-08-04 against e902181d" although the diff changed row 2's subject [24-6-...md:57-63]
- [x] [Review][Patch] Add the explicit remediation-runtime-checklist applicability note — absent from both story and spec; reviewer independently re-derived it as not applicable [spec / story]
- [x] [Review][Patch] Relabel the classification row for current graph/search test bodies: `current-narrow-pattern` contradicts its own permitted use ("the current problem baseline, not proof of their names"); the bodies were deleted, not reused [24-6-...md:34]
- [x] [Review][Patch] Disclose in the proof boundary that seeding bypasses the production write path — `SeedCollisionGraphAsync` selects the graph itself via `falkor.SelectGraph(tenantId)`, so only read-path tenant routing is proven [spec proof-boundary bullets]
- [x] [Review][Patch] Add traverse denial cases for missing/blank `startNodeId` so a 400 cannot pre-empt the 403 [tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs:79]
- [x] [Review][Patch] Remove the dead assertion `edge.VerifiedBy == null || !edge.VerifiedBy.Contains(foreignEdgeMarker)` — unreachable behind the preceding strict equality, and vacuous on null if that line is ever relaxed [tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs:272]
- [x] [Review][Patch] Make the outer `WaitAsync` bound strictly larger than the 30s server-side query timeout; identical values race non-deterministically and `WaitAsync` abandons rather than cancels [TenantIsolationIntegrationTests.cs:240]
- [x] [Review][Patch] Assert "no command other than GRAPH.LIST" instead of the exact triple — `executedCommands.ShouldBe(["GRAPH.LIST","GRAPH.LIST","GRAPH.LIST"])` freezes three redundant round trips as a contract [TenantIsolationVerifierTests.cs:367]
- [x] [Review][Patch] Strengthen and relocate the source-text guard: the regex only matches a literal argument on a variable named `falkorDb` in one file, and a source assertion about the verifier sits in a runbook/deployment test class [OperationalRunbookSetTests.cs:407]
- [x] [Review][Patch] Guard `ContentSnippet`/`SourceUri` against null so a null yields an assertion message rather than a NullReferenceException [TenantIsolationIntegrationTests.cs:263]
- [x] [Review][Patch] Hoist `new FalkorDB(...)` and `SelectGraph(tenantId)` out of the per-node loop and the two helpers [TenantIsolationIntegrationTests.cs:165]
- [x] [Review][Patch] Restore an acceptance criterion alongside the observed result — the `Required result` column was replaced by `Observed result`, leaving re-runners no pass criterion [24-6-...md:75]
- [x] [Review][Patch] Rename `VerifyAsync_GraphIsolation_IsStructuralOnlyAndCitesContentProof` or split it — the body still asserts `AllPassed`, `SyntacticIsolation`, and `SemanticIsolation` details the new name hides [TenantIsolationVerifierTests.cs:335]
- [x] [Review][Patch] Add the canonical story to the spec's `context:` list plus a pointer to its classification/slice-proof record; `check-story-slice-scope.py` cannot see `spec-*.md` [spec:8-11]
- [x] [Review][Patch] Update pre-implementation tense in `Slice Proof` and `Dev Notes` ("is removed only after...") to reflect completed work [24-6-...md:38-53]
- [x] [Review][Patch] Order `using Hexalith.Memories.Telemetry;` before `using Hexalith.Memories.TestHelpers.Documentation;` [OperationalRunbookSetTests.cs:14]

### Deferred

- [x] [Review][Defer] ~30 lines duplicated verbatim across both runbooks and pinned identical by one test — deferred, pre-existing pattern
- [x] [Review][Defer] No teardown for seeded collision nodes/edges and provisioned tenants in the shared fixture — deferred, pre-existing
- [x] [Review][Defer] Collision precondition assumes each tenant graph starts with zero relationships — deferred, fresh random tenants make this near-impossible
- [x] [Review][Defer] Only `CausedBy`/`Explicit`/`File` and `depth=1` are covered; other edge types, origins, and deeper hops untested — deferred, beyond AC1
- [x] [Review][Defer] Verifier class XML doc re-characterization contradicts `5-3-tenant-isolation-verification.md:180` and no test pins either wording — deferred, pre-existing
- [x] [Review][Defer] Failed and backend-unavailable `GraphIsolation` branches carry no evidence-boundary framing and no wording test — deferred
- [x] [Review][Defer] The ~330-char `Details` prose string in an operator-facing API response has no length or format contract — deferred
- [x] [Review][Defer] No test asserts that ingesting into tenant A leaves another tenant's graph empty (production write-path isolation) — deferred, new scope
- [x] [Review][Defer] A ~500-char shell loop is embedded in a Verification table cell instead of citing its owning spec — deferred
- [x] [Review][Defer] Traversal completeness fields may be omit-when-default on the wire; assert wire presence rather than deserialized defaults — deferred
- [x] [Review][Defer] Deleting `SetupGraphQueryEmpty` removed the throw-stub from 12 tests; a reintroduced `GRAPH.QUERY` would return an unconfigured default in all of them — deferred, one test now pins the command set
