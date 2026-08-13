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
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` -- reuse tenant provisioning, FalkorDB, HTTP client, and route-derived authentication. Read-only **for the graph-isolation slice**; the `qa-gap-closure` phase changed it by +38/-24 (`git diff --numstat 0ecdffed..HEAD`) to reconnect the Dapr actor-proxy factory alongside the state client on sidecar-port rotation. Corrected 2026-08-13 by third-pass code review.
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
- 2026-08-13: Closed review finding P1 with a real FalkorDB negative control that plants tenant B's
  edge marker into tenant A's colliding graph and proves the authenticated traversal locality assertion
  fails on that foreign marker.
- 2026-08-13: Resolved N4 by the Administrator's recommended option: removed the externally owned
  customization-fixture repair from Story 24.6's checkpoint table and retained it only as the named
  File List exclusion owned by `spec-update-bmad-6-10-1n49-2026-08-04`.

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
  **58 total**, 0 failed, 0 skipped. The `54` recorded here previously was the mid-review total and was
  superseded by the first-pass review patches (+2 `TenantIsolationVerifierTests` methods, +2
  `ServerEndpointAuthorizationTests` InlineData rows). Independently re-measured by third-pass code
  review on 2026-08-13 at 58 total, 0 failed, 0 skipped, 6.650 s, and again at 7.896 s after the
  source-guard repair. The count includes the authenticated traversal-path denial-before-dependency
  cases added during review.
- The exact Epic 24/Epic 25 checklist structural preservation command from
  `spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md` exited 0,
  proving one checklist heading and exactly one row for each of the six named invariants in both contexts.

**Tenant-isolation negative evidence:**

- Affected surfaces: tenant-scoped FalkorDB databases and authenticated
  `GET /api/v1/tenants/{tenantId}/traverse` responses.
- Collision test: `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`.
- Negative-control test: `TenantIsolationIntegrationTests.VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage`.
- Seeded boundary: identical node IDs, edge type, topology, timestamp, and insertion order in both
  tenant databases; distinct bounded node-content/source markers and `verifiedBy` edge markers.
- Assertions: graph-scoped relationship IDs collide; each traversal is complete and non-degraded,
  returns only the two fixture IDs, preserves the primary path and exact outgoing/incoming
  `CausedBy` relationship views, exposes its own node/edge markers and no gap markers, and contains
  zero foreign markers.
- Backend: real Aspire-provisioned FalkorDB; no mocked graph-content result.
- Negative-control command:
  `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage`.
- Negative-control result: 1 total, 0 failed, 0 skipped, 190.569 seconds on 2026-08-13;
  the full owning class then passed 7/7 in 214.351 seconds.
- Proof boundary — write path is **not** covered: `SeedCollisionGraphAsync` obtains the tenant graph
  directly via `falkor.SelectGraph(tenantId)` (`TenantIsolationIntegrationTests.cs:213`) rather than
  ingesting through the production write path. This evidence therefore proves read-path tenant routing
  and graph-content locality under identifier collision; it does not prove that a production ingestion
  into tenant A selects tenant A's graph. That write-path claim remains an open deferred item ("no test
  asserts that ingesting into tenant A leaves another tenant's graph empty"). Written 2026-08-13 by
  third-pass code review; the first-pass patch that claimed this disclosure was checked off without the
  text ever being added.
- Structural matrix row: `TenantIsolationVerifierTests.VerifyAsync_GraphIsolation_IsStructuralOnlyAndCitesContentProof`
  ran in the passing server class gate and asserts the **boundary** — the recorded FalkorDB
  `ExecuteAsync` command set is non-empty and contains no command other than `GRAPH.LIST` — plus the
  independent proof citation. The earlier wording here ("exactly three `GRAPH.LIST` calls") described
  the assertion that first-pass patch 21 deliberately removed, because pinning the count froze three
  redundant round trips as a contract. Corrected 2026-08-13.
- Backend-unavailable matrix row: `TenantIsolationVerifierTests.VerifyAsync_BackendUnavailable_ReturnsFailedCheckNotException`
  ran in the passing server class gate. The first live collision attempt also exercised the procedural
  proof boundary: the spec remained `in-progress` while the real-backend test body could not run.
- Environment note: the first invocation without the machine-specific Dapr addresses failed during
  shared fixture startup (1 failed, 307.993 seconds) because `/alive` remained 503 while the sidecars
  targeted unused default ports 50005/50006. The test body did not run on that attempt. Supplying the
  addresses of the already-running local placement/scheduler services made the required backend proof
  runnable and produced the passing result above; no production or fixture workaround was added.
- P1 negative control: `TenantIsolationIntegrationTests.VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage`
  passed against the real Aspire-provisioned FalkorDB topology on 2026-08-13: 1 total, 0 failed,
  0 skipped, 190.569 seconds. It seeds both colliding tenant graphs, rewrites tenant A's edge with
  tenant B's marker through `GraphQueryBuilder.BuildUpdateEdgeConfidence`, confirms the foreign marker
  is returned through tenant A's authenticated traversal, and requires `AssertTraversalIsFixtureLocal`
  to throw for the tenant-local edge-marker assertion.
- P1 regression evidence: the unchanged positive collision method passed 1 total, 0 failed, 0 skipped
  in 209.076 seconds; the full `TenantIsolationIntegrationTests` class passed 7 total, 0 failed,
  0 skipped in 214.351 seconds; the IntegrationTests Debug build passed with 0 warnings/errors; and the
  combined verifier/runbook/denial gate passed 58 total, 0 failed, 0 skipped in 6.868 seconds after a
  clean Server.Tests Debug build. All commands used the documented `localhost:6050` placement and
  `localhost:6060` scheduler mappings where applicable.

## Suggested Review Order

**Content-isolation proof**

- Start with the collision fixture, authenticated traversals, and exact local topology assertions.
  [`TenantIsolationIntegrationTests.cs:68`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L68)

- Review bounded graph seeding and inspection used only by the real-backend fixture.
  [`TenantIsolationIntegrationTests.cs:213`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L213)

- Verify the planted-foreign-edge-marker negative control makes the same locality assertion fail.
  [`TenantIsolationIntegrationTests.cs:96`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L96)

**Runtime evidence boundary**

- Confirm runtime verification remains structural-only and requires independent content proof.
  [`TenantIsolationVerifier.cs:304`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L304)

- Verify fixed-token cross-tenant traversal is denied before backend dependencies.
  [`ServerEndpointAuthorizationTests.cs:79`](../../tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs#L79)

**Operator and contract safeguards**

- Check build-first proof instructions and portable Dapr endpoint discovery guidance.
  [`tenant-onboarding-offboarding.md:159`](../../docs/operations/tenant-onboarding-offboarding.md#L159)

- Inspect structural wording, exact command, and source-level query guards.
  [`OperationalRunbookSetTests.cs:372`](../../tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs#L372)

- Finish with exact `GRAPH.LIST` call-set and proof-citation assertions.
  [`TenantIsolationVerifierTests.cs:362`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L362)

## Review Findings

Code review 2026-08-12 against `0ecdffed..63387538` (12 files, +505/-108) — that SHA is the unsquashed PR #52 branch tip and is **not reachable from merged history**; the merged equivalent is `0ecdffed..fdc76064`, identical at 12 files, +505/-108 (re-derived 2026-08-13). Six adversarial layers
plus parent-side independent re-execution. 44 findings: 4 decision-needed, 29 patch, 11 deferred.
(An earlier revision said `46 findings ... 2 dismissed as noise`; no dismissed item was ever recorded,
so the total is corrected to the 44 items actually enumerated below. Note the 4 decisions are each
tagged `[Review][Patch]`, which is what produced the double-counted `33 patches` figure in the story
ledger. Corrected 2026-08-13.)

Independently reproduced by the reviewer, not accepted on the record alone: the server gate
(`54 total, 0 failed`), the real-backend collision proof (`1 total, 0 failed`, 175.204s),
`check-tenant-isolation-evidence.py` (pass), `check-story-file-scope.py` (**vacuous pass** — corrected
2026-08-13: the bare `--story-key` form exits 0 as a no-op on an unstaged worktree, and the non-vacuous
`--changed-files-file` form exits **1**, reporting `tests/tooling/bmad_customization/bmad_customization_test.py`
as out of scope. That is the expected D1 situation — the path was carried under its own `Scope-Override:`
trailer in `c64e5514`, which the commit-time gate honours and a cumulative-set run cannot see — but it
must not be cited as a clean pass), and all three Epic AC
rows. The claim that all seven Suggested Review Order anchors held with no drift was **wrong**: third-pass
code review re-derived every anchor on 2026-08-13 and found four materially drifted (`:65`->`:68`,
`:165`->`:168`, `:153`->`:159`, `:335`->`:362`) and one off-by-one inside a method signature
(`:305`->`:304`). The anchors below are the corrected values. The collision fixture genuinely
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

## Review Findings — Second Pass (code review 2026-08-12, against `c64e5514`)

Independent second-pass adversarial review run after PR #52 and PR #53 merged. Six layers
(blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor, historical-slice-guard,
story-phase-ledger) plus parent-side independent re-execution. Findings were triaged against the
post-patch worktree `c64e5514`, not the pre-patch slice `0ecdffed..fdc76064`.

Approximately 94 raw findings reduced to 18 live: 3 decision-needed, 15 patch, 0 newly deferred.
The remainder were dismissed as already fixed by the first-pass patches or already recorded on the
deferred list above. Verified green and unchanged: all three Epic AC Verification commands re-run
and confirmed; 145/145 tooling tests pass across `story_review_readiness`, `tenant_isolation_evidence`,
`integration_fast_coverage`, `bmad_customization`, and `story_slice_scope`; `TenantIsolationVerifier.cs`
issues only `GRAPH.LIST` with zero `GRAPH.QUERY`; the remediation-runtime checklist is correctly
recorded as not applicable; `epic-24-context.md` anchors (NFR8, D29, EventStore, banner) are fully
restored.

### Decisions needed — second pass

- [x] [Review][Decision] (D1) RESOLVED 2026-08-12 (Administrator, option 1 — record as a named exclusion; `### File List Exclusions` added to the story and the final ledger row restated to `matched 15/15` + 1 named exclusion). Unaccounted in-scope changed path breaks File List reconciliation — the cumulative set `git diff --name-status 0ecdffed..HEAD` returns 16 paths while `## File List` declares 15. Missing: `tests/tooling/bmad_customization/bmad_customization_test.py`, introduced by `c64e5514` under a different owner (`Story-Key: spec-update-bmad-6-10-1n49-2026-08-04`) with a carried `Scope-Override:` trailer. No `### File List Exclusions` block exists in either artifact, and the `code-review` ledger row still asserts `matched 13/13 ... No exclusions`. Per `story-phase-ledger.md` this is a fail-closed blocker for `done`. Decide: add the path to the File List, or record it as a machine-readable exclusion naming its owning story key and authorizing trailer. [24-6-...md:122,125-143]
- [x] [Review][Decision] (D2) RESOLVED 2026-08-12 (Administrator, option 2 — ratified as an approved multi-checkpoint story; `### Checkpoints` table added with owner, evidence command, review state, and completion state per outcome, and the Slice Proof no longer claims a single outcome). Hidden multi-slice scope — commit `c64e5514` bundles three independently demonstrable outcomes: graph-isolation review hardening, the OpenBao/Dapr actor-proxy CI repair, and the BMAD 6.11 customization-fixture repair owned by another story key. `## Slice Proof` still asserts "One independently demonstrable outcome" and its Excluded list never mentions CI or fixture work. Decide: re-key the two non-graph outcomes to their own numbered stories, or ratify 24.6 as a multi-checkpoint story giving each outcome its own checkpoint row with owner, evidence command, review state, and completion state. [24-6-...md:38-42,123]
- [x] [Review][Decision] (D3) RESOLVED 2026-08-12 (Administrator, option 1 — derived baseline ratified as a human-accepted blocker; the ledger row now names the accepting human and date). The only accepted blocker in the story is self-accepted — the `create-story` adoption row records `Owner: code review` for the derived (not runner-observed) baseline discovery. AC4 and `story-phase-ledger.md` both contemplate human acceptance of a blocker. Decide: ratify the derived baseline as a human-accepted blocker, or re-derive the baseline from a built historical worktree. [24-6-...md:120]

### Patches — second pass

- [x] [Review][Patch] (P1) **APPLIED 2026-08-13** — Added `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage`. The real-backend test preserves the graph-scoped relationship-ID collision, plants tenant B's edge marker into tenant A through a parameterized `GraphQueryBuilder` update, confirms the foreign marker returns through tenant A's authenticated traversal, and proves `AssertTraversalIsFixtureLocal` throws on the tenant-local edge-marker assertion. The negative control passed 1/1 in 190.569 seconds, the unchanged positive collision method passed 1/1 in 209.076 seconds, and the full owning class passed 7/7 in 214.351 seconds. The original vacuous-empty-collection rationale remains withdrawn: cardinality and completeness assertions already fail closed on empty or degraded traversal. [tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs:96]
- [x] [Review][Patch] (P2) **APPLIED 2026-08-13** (applied — the disclosure is now written into the proof-boundary bullets above) — A first-pass patch is recorded as applied but was not applied — spec:185 is checked off for disclosing that `SeedCollisionGraphAsync` bypasses the production write path, but no such disclosure exists in the spec proof-boundary bullets, the story Slice Proof demonstration boundary, or the `deferred-work.md` entry, which is flipped to `resolved` on the unqualified claim. [spec-24-6-...md:185,95-117]
- [x] [Review][Patch] (P3) **APPLIED 2026-08-13** (resolved — status reverted to `in-progress`, so readiness C1 executes again; the residual defect is tracked as N1 below) — Status advanced to `done` ahead of the ledger, disabling the check that would have caught D1 — the readiness gate prints `C1: status 'done' is outside {in-progress, review}; cumulative-diff comparison skipped`, so no automated surface ever compared the File List to the cumulative set. [24-6-...md:8]
- [x] [Review][Patch] (P4) **APPLIED 2026-08-13** (applied — Results corrected to 58 and the structural-matrix wording restated as a boundary assertion) — Spec Verification Results are one review pass stale — records `54 total` for the server gate where the story records `58 total`, and asserts the structural matrix test proves "exactly three `GRAPH.LIST` calls" although patch 21 deliberately replaced that count assertion with a boundary assertion. [spec-24-6-...md:88-90,107-109]
- [x] [Review][Patch] (P5) **APPLIED 2026-08-13** (applied — `deferred-work.md` Evidence now carries the post-patch measurements) — `deferred-work.md` Evidence field carries superseded totals (233.919s / 241.463s / 54 total) that contradict the story's post-patch measurements. [deferred-work.md:1900]
- [x] [Review][Patch] (P6) **APPLIED 2026-08-13** (applied, with a correction: `12 files, +505/-108` was never stale — it is exact for `0ecdffed..fdc76064`. Only the unreachable SHA and the worktree-form command were defects) — The recorded comparison range is unreachable from merged history — `git merge-base --is-ancestor 63387538 HEAD` fails; the merged equivalent is `fdc76064`. The stated `12 files, +505/-108` is stale (cumulative is `16 files, +750/-111`), and `git diff --name-status 0ecdffed` is worktree-form rather than `0ecdffed..HEAD`. [24-6-...md:122; spec-24-6-...md:150]
- [x] [Review][Patch] (P7) **APPLIED 2026-08-13** (applied — the row is now the canonical `qa-gap-closure`) — `ci-repair` is not a canonical phase name — `story-phase-ledger.md` enumerates exactly `create-story`, `correct-course`, `dev-story`, `qa-gap-closure`, `code-review`. [24-6-...md:123]
- [x] [Review][Patch] (P8) **APPLIED 2026-08-13** (applied — named unit, cumulative delta, and reproducible command added; the `matched N/N` half had already been satisfied by the D1 patch) — The `ci-repair` row omits the named discovery unit, the cumulative story delta, the exact evidence command, and any `matched N/N` File List reconciliation. [24-6-...md:123]
- [x] [Review][Patch] (P9) **APPLIED 2026-08-13** (applied — the blocked command is spelled out and the adoption row reconciles `matched 0/0` at the baseline) — The `create-story` adoption row omits the exact command that could not be run, and neither it nor the `dev-story` row carries `matched N/N` as the policy requires at every phase. [24-6-...md:120-121]
- [x] [Review][Patch] (P10) **APPLIED 2026-08-13** (applied — both real Server.Tests and integration commands recorded) — The `dev-story` row's evidence command is a placeholder (`dotnet exec <assembly> -class <named classes>`) and no command at all is given for the two integration lanes the row moves. [24-6-...md:121]
- [x] [Review][Patch] (P11) **APPLIED 2026-08-13** (applied — anchors corrected (four had materially drifted) and the governance-gate row restated) — The governance-gate evidence row claims readiness "is now substantive", but C1 did not execute; and spec:157 claims "all seven Suggested Review Order anchors (no drift)" while at least three have drifted — `TenantIsolationVerifierTests.cs:335` now lands on `VerifyAsync_SingleTenant_PerformsTargetStructuralChecks` while the bullet describes the `GRAPH.LIST` call-set assertions now at :362. [24-6-...md:99; spec-24-6-...md:157]
- [x] [Review][Patch] (P12) **APPLIED 2026-08-13** (applied — the AC2 redundancy justification now cites `VerifyTenant_WithTwoProvisionedTenants_CoreIsolationChecksShouldPass` and records the ordering claim as attested, not demonstrated. AC2 itself is human-owned and was not edited) — AC2's "removed only after" ordering claim has no falsifiable evidence — only one canonical-gate execution is recorded anywhere and it is labelled post-removal, while removal and evidence landed in a single commit. Separately, the coverage that actually makes the deleted test redundant is `VerifyTenant_WithTwoProvisionedTenants_CoreIsolationChecksShouldPass`, which the AC2 justification never cites. [24-6-...md:25,92]
- [x] [Review][Patch] (P13) **APPLIED 2026-08-13** (applied — `33 patches` corrected to `4 decisions + 29 patches` and the phantom `2 dismissed` removed) — Patch accounting disagrees across artifacts — the story ledger and `sprint-status.yaml` say "33 patches applied" while spec:151 says "4 decision-needed, 29 patch"; the 4 decisions are themselves tagged `[Review][Patch]`, so 33 double-counts them. The same sentence claims "2 dismissed as noise" while zero dismissed items are recorded anywhere. [24-6-...md:122; spec-24-6-...md:151]
- [x] [Review][Patch] (P14) **APPLIED 2026-08-13** (applied — Code Map now records the +38/-24 qa-gap-closure change) — The spec Code Map still labels `AspireIngestionPipelineFixture.cs` "read-only" although the story's cumulative range changes it by +38/-24. [spec-24-6-...md:42]
- [x] [Review][Patch] (P15) **APPLIED 2026-08-13** (applied — the label is retained with its justification recorded; both `do not reopen` phrases in `20-2-...md` guard Story 20.1 scope, not Story 20.2 shape) — The Story 20.2 classification row is labelled `current-narrow-pattern` although `20-2-...md` contains the `do not reopen` trigger phrase that `story-scope-guard.md` treats as an anti-template marker; the row is also an addition beyond the approved proposal's three-row table. Either relabel, or record why 20.2's guards bind Story 20.1's scope rather than 20.2's shape. [24-6-...md:36]

## Review Findings — Third Pass (verification-focused, code review 2026-08-13)

Scope chosen by Administrator: verify the fifteen open second-pass patch findings against the current
worktree rather than re-derive them, re-run every governance gate and Epic AC command independently,
and hunt only for what P1-P15 did not cover. No review layers were launched; all verification was
performed directly by the reviewing agent.

### Independently re-executed (not accepted on the record)

- Combined server gate: **58 total, 0 failed, 0 skipped** (6.650 s pre-patch, 7.896 s post-patch after a
  clean Debug build with 0 warnings / 0 errors). This confirms the story's `58` and refutes the `54`
  this spec previously recorded.
- Owning integration class against the real Aspire/FalkorDB topology:
  **6 total, 0 failed, 0 skipped, 16.008 s**, with live Redis, Dapr sidecar, and actor traffic in the
  run log. This independently corroborates the recorded 17.572 s and closes the question of whether the
  widened lane really exercised the backend.
- `check-story-review-readiness.py --changed-files-file <cumulative>`: exit 0,
  `C1: all 16 changed paths are declared.` / `C6: 2 evidence table(s) carry no unresolved pending row.`
- `check-story-review-readiness.py --story-key <key>` (bare): **exit 1**, see N1.
- `check-story-slice-scope.py`: exit 0. `check-tenant-isolation-evidence.py --changed-files-file`: exit 0.
- Tooling suites: `line_endings` 4, `integration_fast_coverage` 6, `story_review_readiness` 45,
  `tenant_isolation_evidence` 41, `story_slice_scope` 20, `bmad_customization` 33 — 149 total, all OK.
- `CiTestInventoryTests`: **66 total**, 0 failed (see N6).
- All three Epic AC Verification commands re-run; all three verdicts hold (`prd.md:971` carries the
  claim verbatim; three `GRAPH.LIST` and zero `GRAPH.QUERY` in the verifier; all four named canonical
  search tests exist). Epic 23 checklist preservation loop: exit 0.

### Verdicts on the second-pass findings

Thirteen of fifteen confirmed and applied. Two did not survive verification intact: **P1**'s failure
scenario is refuted (the finding survives only as a missing negative control — see its restated entry
above), and **P3** is discharged by the status revert. **P6** was confirmed but overstated: the
`12 files, +505/-108` figure it called stale is exact for the merged range `0ecdffed..fdc76064`.

### New findings — third pass

- [x] [Review][Patch] (N5) **APPLIED** — the AC3 source-level guard was vacuous. `OperationalRunbookSetTests.GraphIsolationEvidenceBoundary_SeparatesStructuralAndContentProof` asserted `falkorCommands.Count.ShouldBeGreaterThan(0)` on **every** `ExecuteAsync("…")` literal in the verifier, then filtered to `GRAPH.`-prefixed commands before `ShouldAllBe`. `TenantIsolationVerifier.cs:452` issues `FT.INFO`, so deleting every `GRAPH.LIST` call left the count above zero and the filtered sequence empty — `ShouldAllBe` then passed vacuously and the guard greenlit the removal of the very thing it guards. Fixed to require the **filtered** GRAPH-prefixed set to be non-empty. Verified by mutation: replacing the three `GRAPH.LIST` literals with a `const` reference makes the repaired guard fail (1 failed); the previous form would have passed. [tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs:415-422]
- [x] [Review][Patch] (N1) **APPLIED** — the Verification table recorded the bare `check-story-review-readiness.py --story-key <key>` invocation as exiting 0. It exits **1** (`C1: the changed set is empty for a governed story`). It exited 0 only while the story read `done`, which skipped C1 entirely. The row now records both forms and their true exits. [24-6-...md Verification table]
- [x] [Review][Patch] (N2) **APPLIED** — the `24.3-GRAPH-CONTENT-EVIDENCE` deferred entry read `Status: resolved` while its owning story had reverted to `in-progress` with findings open. This is the same shape a first-pass patch already repaired once; the second-pass status revert regressed it. Now `carried-forward`, with the promotion condition recorded in its re-open trigger. [deferred-work.md]
- [x] [Review][Patch] (N3) **APPLIED** — checkpoint C1 read `complete 2026-08-12` while its own `Review state` cell named three open findings, and C2 likewise. C1 is now an explicit blocked state with owner, consequence, and reopen trigger per `story-phase-ledger.md`; C2 is genuinely complete now that P7/P8 are applied. [24-6-...md Checkpoints]
- [x] [Review][Patch] (N6) **APPLIED** — the deferred-work schema gate row recorded `65 total` for `CiTestInventoryTests`. The real discovery total is **66**, and `tests/Hexalith.Memories.Cli.Tests` is unchanged across this story's entire range, so 66 was the total throughout; `65` was never measured. Confirmed by re-running the class at HEAD with the artifact edits stashed. [24-6-...md Verification table]
- [x] [Review][Decision] (N4) **RESOLVED 2026-08-13** (Administrator selected the recommended option) — Removed checkpoint C3 from Story 24.6. The BMAD customization-fixture repair remains recorded only under `### File List Exclusions`, with owner `spec-update-bmad-6-10-1n49-2026-08-04` and its `Scope-Override:` provenance. Story 24.6's checkpoint table now tracks only its two owned outcomes. [24-6-...md Checkpoints, File List Exclusions]

### Fail-closed status

P1 and N4 are closed. Nothing found in three passes, the P1 closure, or the N4 ownership resolution
threatens AC1 — the collision fixture proves content-level tenant isolation against a real FalkorDB
backend. The story is ready for final review reconciliation.

## Review Findings — Fourth Pass (build workflow review 2026-08-13)

Three context-free layers reviewed the complete `0ecdffed..HEAD` plus worktree diff: blind hunter,
edge-case hunter, and verification-gap reviewer. The verification-gap reviewer found no gap. The
other layers repeated the already recorded proof boundaries (production write routing, fixture cleanup,
broader edge/depth coverage, wire-presence proof, verifier wording, and shared mock strictness) and the
externally owned submodule/customization changes; those were rejected as known, explicitly bounded, or
outside Story 24.6 rather than duplicated in the deferred ledger.

Applied findings:

- [x] [Review][Decision] N4 — Administrator selected the recommended option: drop externally owned
  checkpoint C3 and retain its path only as the named File List exclusion.
- [x] [Review][Patch] Pin
  `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage` as its own
  `integration-fast` required method surface so the P1 control cannot silently disappear.
- [x] [Review][Patch] Replace abbreviated verification-table commands with the exact commands that
  produced the recorded graph, canonical-search, verifier, runbook, combined-server, and full-server
  evidence.

Post-patch checks: `integration_fast_coverage` 6/6, `line_endings` 4/4,
`story_review_readiness` 45/45, `tenant_isolation_evidence` 41/41, and `story_slice_scope` 20/20.
The substantive story gates passed for the 16-path story-scoped set (`C1: all 16 changed paths are
declared`, C6 clean, slice scope clean, tenant-isolation evidence clean); `git diff --check` passed.
The implementation subagent independently rechecked the N4/exclusion reconciliation and exact method
pin and found no defect. The real-backend negative-control result remains 1/1 from the implementation
phase; no C# source changed during review.

## Suggested Review Order

**Graph collision evidence**

- Start here: the planted foreign marker proves locality assertions can fail.
  [`TenantIsolationIntegrationTests.cs:96`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L96)

- Confirm the unchanged dual-tenant collision proof remains the positive contract.
  [`TenantIsolationIntegrationTests.cs:68`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L68)

- Check the marker-specific assertion that distinguishes the negative control's failure.
  [`TenantIsolationIntegrationTests.cs:333`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L333)

**Runtime evidence boundary**

- Runtime verification deliberately stays structural and read-only through `GRAPH.LIST`.
  [`TenantIsolationVerifier.cs:304`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L304)

- Unit coverage pins structural-only wording, commands, and missing-database failure.
  [`TenantIsolationVerifierTests.cs:362`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L362)

- Cross-tenant traversal is denied before tenant-state dependencies execute.
  [`ServerEndpointAuthorizationTests.cs:85`](../../tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs#L85)

**Dapr reconnection checkpoint**

- State and actor clients reconnect together after the sidecar endpoint rotates.
  [`AspireIngestionPipelineFixture.cs:1176`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L1176)

- The restart regression requires a real post-rotation actor call.
  [`OpenBaoTopologyIntegrationTests.cs:148`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs#L148)

**Operator and governance safeguards**

- Operators get the structural/content boundary and portable proof procedure.
  [`tenant-onboarding-offboarding.md:159`](../../docs/operations/tenant-onboarding-offboarding.md#L159)

- Contract tests keep both runbooks and verifier commands aligned.
  [`OperationalRunbookSetTests.cs:372`](../../tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs#L372)

- CI now requires both positive and planted-leak graph methods to pass.
  [`integration-fast-required-surfaces.txt:15`](../../tools/integration-fast-required-surfaces.txt#L15)

- Final checkpoints retain only Story 24.6's two owned outcomes.
  [`24-6-graph-content-level-tenant-isolation-evidence.md:53`](24-6-graph-content-level-tenant-isolation-evidence.md#L53)

## Review Findings — Fifth Pass (code review 2026-08-13)

Six context-free adversarial layers (blind hunter, edge-case hunter, verification-gap, acceptance
auditor, historical-slice guard, story-phase-ledger auditor) over the complete `0ecdffed..HEAD`
range at 19 paths, plus independent parent-side re-execution. The user selected the full raw range
rather than the story-scoped 16 paths, so the submodule-exclusion claim was audited rather than
accepted.

### Independently re-executed (not accepted on the record)

- Combined server gate: **58 total, 0 failed, 0 skipped, 8.905 s**. Confirmed the recorded `58`.
- `CiTestInventoryTests`: **66 total, 0 failed**. Confirmed the N6 correction.
- `TenantIsolationIntegrationTests`: **7** `[Fact]` methods at HEAD.
- Tooling suites: `line_endings` 4, `integration_fast_coverage` 6, `story_review_readiness` 45,
  `tenant_isolation_evidence` 41, `story_slice_scope` 20 — all OK.
- `check-story-review-readiness.py --changed-files-file <16-path story set>`: exit 0.
  **`--changed-files-file <raw 19-path set>`: exit 1** (see P1). Bare `--story-key`: exit 1, as recorded.
- `check-story-slice-scope.py`: exit 0. `check-tenant-isolation-evidence.py`: passed.
- All three Epic AC Verification rows re-run and hold: `prd.md:971` carries the claim verbatim;
  three `GRAPH.LIST` at `TenantIsolationVerifier.cs:116,315,358` with zero `GRAPH.QUERY`; all four
  canonical search tests exist at `TenantContextEnforcement:155`, `Semantic:119`, `GraphScoped:118`,
  `Syntactic:108`.
- Test assemblies confirmed newer than every source file, so the 58 is a live measurement rather
  than a stale binary reproducing the recorded number for the wrong reason.
- The remediation-runtime "not applicable" note is independently re-derived and holds: the entire
  production delta is one XML doc comment and one `Details` string.
- N5's repaired source guard is genuinely non-vacuous (`graphCommands.ShouldNotBeEmpty()` runs on the
  filtered GRAPH-prefixed set). The P1 negative control is a real mutation-style control.
- **AC1 holds in substance.** The collision fixture proves content-level tenant isolation against a
  real FalkorDB backend through authenticated per-tenant routes.

### Decisions needed — fifth pass

- [x] [Review][Decision] (F5-D1) **RESOLVED 2026-08-13** (Administrator: record D2 as superseding for Story 24.6 only, and propagate the umbrella note to `epics.md`; 24.7-24.9 stay governed by the original criterion). The resulting work is patch F5-P6 plus a dated note at the proposal's completion criterion, both open below. The approved change proposal named in this story's own
  `approved_change` frontmatter still states "Stories 24.6-24.9 are registered with **one independent
  outcome**" (`sprint-change-proposal-2026-08-04-...md:502`, template at `:194-199`), while D2 ratified
  Story 24.6 as a two-checkpoint umbrella. Decide: formally amend the proposal's completion criterion,
  or record on the proposal that D2 supersedes it for Story 24.6 only.
- [x] [Review][Decision] (F5-D2) **RESOLVED 2026-08-13** (Administrator: option (a), narrow the class summary so the hedge lives only on `CheckGraphIsolationAsync`, leaving the Story 5.3/24.3/24.7-24.9 contract untouched). The `"pre-existing"` label on the deferred bullet is corrected in the same change. Open as a patch below. The verifier class-level XML doc re-characterization
  (`TenantIsolationVerifier.cs:18-19`) is introduced **by this diff** yet is deferred as
  "pre-existing"; its new wording ("check-specific structural and **tenant-marker** diagnostics")
  pre-characterizes the marker checks owned by Stories 24.7-24.9, which this story's Slice Proof
  explicitly excludes, and contradicts `5-3-tenant-isolation-verification.md:180` which still reads
  "confirms architectural guarantees hold". Decide: (a) narrow the class summary so the hedge lives
  only on `CheckGraphIsolationAsync`, or (b) ratify the whole-verifier re-characterization as owned by
  24.6, which then requires updating `5-3-...:180` and pinning one wording with a test. Either way the
  "pre-existing" label is factually wrong and is corrected.
- [x] [Review][Decision] (F5-D3) **RESOLVED 2026-08-13** (Administrator: re-run the owning class at HEAD and restate, superseding every earlier figure; if the Aspire/FalkorDB topology is unavailable the item converts to a recorded blocker with owner, consequence, and reopen trigger). Open as a patch below. Recorded durations for the same class are mutually inconsistent:
  the third-pass ledger row and `deferred-work.md:1900` record `TenantIsolationIntegrationTests` at
  **16.008 s / 17.572 s** for six methods, while post-P1 rows record **190.569 s** and **209.076 s**
  for *single* methods of that class and **214.351 s** for all seven. Each `dotnet exec` brings up its
  own Aspire topology, so a 16 s full-class run is hard to reconcile with a 209 s single-method run —
  and the third pass used the 16.008 s figure specifically to "close the question of whether the
  widened lane really exercised the backend". Decide which runs are authoritative, or re-run and
  restate.
- [x] [Review][Decision] (F5-D4) **RESOLVED 2026-08-13** (Administrator: split the obligation explicitly - the runbooks discharge AC3's command obligation, the verifier discharges the structural-only label and method citation; no API string growth). Record the reading rather than leaving it implied. Open as a patch below. AC3 requires that "verifier **and** operator documentation ... cite
  the focused integration **command**". The runbooks cite the command; the verifier's `Details` cites
  only a method name, and lengthening that operator-facing API string conflicts with the existing
  deferred item recording it as an uncontracted ~330-char prose blob. Decide: extend the `Details`
  citation, or record that the runbooks discharge AC3's command obligation and the verifier discharges
  the label obligation.

### Patches — fifth pass

- [ ] [Review][Patch] (F5-P1) **[high]** Three in-scope changed paths are exempted by prose only.
  `references/Hexalith.Builds`, `references/Hexalith.EventStore`, `references/Hexalith.FrontComposer`
  are excluded in File List prose and two ledger cells, but `### File List Exclusions` holds exactly
  one bullet. `story-phase-ledger.md:118-131` is explicit that prose "does not exempt a path from the
  executable gate". Re-run independently: the raw 19-path set exits **1** with three `C1: ... changed
  but is not in the File List` violations, while the same run accepts `bmad_customization_test.py` —
  proving the machine-readable mechanism works and simply was not used. Every recorded "readiness
  passed" in this story was produced against a hand-curated 16-path input. Add three exclusion bullets
  with path, named owner, and reason, then re-record readiness against the raw set.
  [24-6-...md:163,181-183]
- [ ] [Review][Patch] (F5-P2) Four artifacts carry three different statuses: story `review`,
  `sprint-status.yaml` `review`, `epics.md:4598` `in-progress`, spec frontmatter `status: 'done'`.
  First-pass patch "Reconcile status across artifacts" is checked off as applied. `review_loop_iteration: 1`
  is also stale after four completed passes. [epics.md:4598; spec:5-6]
- [ ] [Review][Patch] (F5-P3) `deferred-work.md:1900` `Evidence:` is a phase stale — records the owning
  class at **6 total / 17.572 s** and the collision method at 175.204 s, superseded by 7/7 in 214.351 s
  and 1/1 in 209.076 s. It never names the P1 negative control, its `Re-open trigger:` protects only the
  positive method (so deleting the control trips nothing), and its status rationale still reads "Story
  24.6 returned to `in-progress`", a status the story no longer holds. Same shape as second-pass P5,
  regressed by the P1 closure. [deferred-work.md:1900-1901]
- [ ] [Review][Patch] (F5-P4) The eleven new deferred entries appended by the first pass carry **none**
  of the required schema fields the same file documents at `:11-29` (`ID:`, `Status:`, `Source story:`,
  `Target artifact:`, `Re-open trigger:`, and one of `Evidence:`/`Rationale:`). They are bare prose
  bullets — unreferenceable by ID and invisible to the schema gate. The "legacy prose entries remain
  valid" clause covers pre-existing entries, not new ones. [deferred-work.md:3034+]
- [ ] [Review][Patch] (F5-P5) Checkpoint C2's `Evidence command / artifact` cell contains only results
  ("Restart regression 1/1 passing in 2.5443 min; ..."), no command and no artifact — while
  `story-scope-guard.md` permits the umbrella exemption *only when* every checkpoint has one. C2 also has
  no row in the `## Verification` table. The reproducible invocation already exists at ledger row `:155`;
  lift it into the cell, add the PR #53 CI run as the artifact, and add a C2 verification row.
  [24-6-...md:58,117-133]
- [ ] [Review][Patch] (F5-P6) The D2 umbrella ratification lives only in the story file. `epics.md:4597`
  changed by exactly one line (`backlog` -> `in-progress`) and carries no umbrella note; the repo's own
  precedent is `epics.md:5381,5395` (Story 31.1). At both surfaces the policy names as binding, Story
  24.6 still reads as a one-outcome story. [epics.md:4597]
- [ ] [Review][Patch] (F5-P7) The final `code-review` ledger row omits four of the five required
  `Test count` elements — no cumulative story delta, no per-lane before/after totals, no named unit
  bound to each lane, and **no evidence command at all**. Every earlier row carries all five. This is
  the identical defect second-pass P8 recorded against the then-`ci-repair` row. [24-6-...md:159]
- [ ] [Review][Patch] (F5-P8) The proof-method citation is unbound. The method name appears at seven
  sites; the two that guard it (`OperationalRunbookSetTests.cs:375`, `TenantIsolationVerifierTests.cs:381`)
  compare the verifier/runbook text to **string literals declared inside the tests**. Renaming the test
  method and updating `integration-fast-required-surfaces.txt` — the only edit CI forces — leaves both
  guards green while the operator-facing verify response and both runbooks instruct operators to run a
  `-method` invocation that resolves to no test. Derive the literal by parsing the manifest in
  `OperationalRunbookSetTests` and reuse it in the verifier assertion.
- [ ] [Review][Patch] (F5-P9) Two new **required** `integration-fast` method surfaces were pinned with
  no lane evidence. The owning class carries only `[Trait("Category", "Integration")]` — not
  `IntegrationSlow` — so both methods, measured locally at 190.569 s and 209.076 s against real
  FalkorDB, now execute in CI's required fast lane (`ci.yml:429`). The only recorded post-patch evidence
  is `integration_fast_coverage` 6/6, which exercises the verifier script against synthetic TRX
  fixtures, not this manifest against a real lane result. Cite a green integration-fast run at HEAD or
  record an accepted blocker with owner, consequence, and reopen trigger.
- [ ] [Review][Patch] (F5-P10) AC3's operator-observable output is pinned only by mocks. The `Details`
  string returned over `POST /api/v1/tenants/{tenantId}/verify` is asserted solely in the NSubstitute-mocked
  `TenantIsolationVerifierTests`, while the real-backend `VerifyTenant_WithTwoProvisionedTenants_CoreIsolationChecksShouldPass`
  already POSTs that endpoint and asserts nothing about `Details`. For a story whose thesis is that
  internal signals must not be mistaken for observable proof, AC3 closes on the shape it exists to
  forbid. Add the assertion to the existing real-backend test — no new method, no new lane.
- [ ] [Review][Patch] (F5-P11) The P1 closure is partial and undisclosed. The negative control plants
  only `edgeMarkerB` and pins the thrown message to "tenant-local edge marker", exercising one of the
  ~15 assertions in `AssertTraversalIsFixtureLocal`. The four node-marker assertions — which carry
  NFR8's actual "zero tenant-B **nodes**" claim — have no failure-sensitivity control. Checkpoint C1
  reads an unqualified `complete` and the spec reads "P1 and N4 are closed". Disclose the boundary;
  a second control planting `nodeMarkerB` is the stronger optional fix. Related: the control is listed
  under **Cross-Tenant Negative Evidence -> Tests** although it never crosses tenants — it writes a
  literal into tenant A's own edge, making it assertion-sensitivity evidence, not cross-tenant evidence.
  [TenantIsolationIntegrationTests.cs:96-146]
- [ ] [Review][Patch] (F5-P12) The repaired AC3 source guard still has escape hatches: the regex matches
  only `ExecuteAsync("<literal>")` in one file, and the only prohibition is the literal `GRAPH.QUERY`.
  A content read added as `GRAPH.RO_QUERY`, `GRAPH.EXPLAIN`, or `GRAPH.PROFILE`, issued through a
  `const`/interpolation, via sync `Execute`, or from another partial file, passes both assertions.
  Reject any `GRAPH.` command other than `GRAPH.LIST`, scan every `TenantIsolationVerifier*.cs`, and
  assert the full `ReceivedCalls()` surface rather than only calls named `ExecuteAsync`.
  [OperationalRunbookSetTests.cs:410-423; TenantIsolationVerifierTests.cs:387-394]
- [ ] [Review][Patch] (F5-P13) `InPlaceOpenBaoRestart_RotatesGenerationAndRecoversDependentSidecars`
  never asserts the sidecar endpoint actually rotated. `ReconnectPrimaryDaprClients` is called only when
  `currentDaprEndpoint != DaprSidecarHttpEndpoint`, so if the sidecar returns on the same port the
  reconnect never runs and the new actor call succeeds against the original factory — green, with the
  repair unexercised. Capture the endpoint before and after and assert it changed.
  [OpenBaoTopologyIntegrationTests.cs:148-156; AspireIngestionPipelineFixture.cs:642]
- [ ] [Review][Patch] (F5-P14) D2's second half was never applied. The Slice Proof's **Demonstration
  boundary** still describes only the graph outcome and the **Excluded** list is unchanged from the
  pre-D2 single-outcome text — it never mentions CI or fixture work. C2 has neither a demonstration
  boundary nor exclusions. [24-6-...md:49-51]
- [ ] [Review][Patch] (F5-P15) No `Historical Context Classification` row for Story 29.1, which owns
  both fixture files C2 modifies (`29-1-...md:333,340`) and whose own review created the restart
  regression this story expands (`29-1-...md:104`). `historical-reference-only` is the correct label —
  `29-1-...md` carries none of the policy's anti-template marker phrases. [24-6-...md:31-36]
- [ ] [Review][Patch] (F5-P16) `VerifyTenant_WithTwoProvisionedTenants_CoreIsolationChecksShouldPass`
  is the test the AC2 redundancy justification actually rests on — the third pass established that the
  deleted test's *name* was never the real coverage — yet it is the one method not pinned in
  `integration-fast-required-surfaces.txt`. Renaming or skipping it silently removes AC2's stated
  coverage while the lane stays green. [tools/integration-fast-required-surfaces.txt]
- [ ] [Review][Patch] (F5-P17) The spec now carries **two** `## Suggested Review Order` H2 sections
  (`:157` and `:382`) with divergent anchors; the fourth pass appended a new one instead of updating
  the third pass's corrected one. A top-down reader — and any heading lookup, the pattern this repo's
  own `MarkdownContractDocument.GetSection` uses — takes the stale first. [spec:157]
- [ ] [Review][Patch] (F5-P18) Fourth-pass anchor drift: "CI now requires **both** positive and
  planted-leak graph methods" cites `integration-fast-required-surfaces.txt:15`, which is only the
  positive entry; the negative control is line **16**. [spec:422-423]
- [ ] [Review][Patch] (F5-P19) The "Runbook evidence-boundary guard (AC3 docs)" verification row is
  dated `passed 2026-08-12`, but `OperationalRunbookSetTests.cs` was last changed **2026-08-13** by the
  N5 repair, which rewrote the assertions inside exactly that row's subject test. The standalone
  measurement predates the change it is cited to cover. [24-6-...md:127]
- [ ] [Review][Patch] (F5-P20) Checkpoint C1's `Review state` reads "implementation closure ready for
  review" while its `Completion state` reads "complete 2026-08-13". Dated, so C6 passes at `review`;
  under `done` the cell asserts the checkpoint's own review was never performed — the false-record shape
  N3 repaired one pass earlier. [24-6-...md:57]
- [ ] [Review][Patch] (F5-P21) `docs/operations/route-surface.md` closes the new section with two
  consecutive blank lines before `## Pub/sub event-intake operation surface`; the paired insertion in
  `tenant-onboarding-offboarding.md` closes with one. The two sections are maintained as near-duplicates
  and pinned together by `OperationalRunbookSetTests`. [docs/operations/route-surface.md:108-109]

### Deferred — fifth pass

- [x] [Review][Defer] The first-pass patch "strengthen **and relocate** the source-text guard" was only
  half-applied — N5 strengthened it, but the verifier source assertion still lives in the runbook/deployment
  doc-contract class, so a verifier regression is reported by a runbook test — deferred, pre-existing shape
- [x] [Review][Defer] `ReconnectPrimaryDaprClients` disposes `_actorHttpMessageHandler` and nulls the
  factory *before* installing replacements, so a proxy handed out before the rotation faults with
  `ObjectDisposedException`; not currently reachable because the collection runs sequentially — deferred
- [x] [Review][Defer] The reconnect fires only when the endpoint changes, so a sidecar restarting on the
  same port keeps pooled connections to the killed process — deferred
- [x] [Review][Defer] `AssertTraversalIsFixtureLocal` dereferences `Nodes`/`Edges`/`GapMarkers` without
  null guards, yielding a `NullReferenceException` instead of an assertion naming the lost field — deferred
- [x] [Review][Defer] No cancellation-token coverage on `VerifyAsync`; the graph check's cancellation
  behaviour is unpinned — deferred
- [x] [Review][Defer] No `/traverse` denial rows for invalid or out-of-range `depth`, where a 400 could
  pre-empt the 403 — deferred, beyond AC1
- [x] [Review][Defer] `var actorConfig` is dereferenced in the restart regression with no null guard, and
  the assertion silently depends on the `OpenBaoRecoveryTenantId` tenant having a seeded embedding
  config — deferred
