---
title: 'Story 24.6: Graph Content-Level Tenant Isolation Evidence'
type: 'feature'
created: '2026-08-12'
status: 'review'
review_loop_iteration: 8
baseline_commit: '0ecdffed0b131d05816306da1c7061eb88bda5bf'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-24-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md'
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
- Given runtime `GraphIsolation` uses `GRAPH.LIST`, when verifier output and operator docs describe it, then verifier Details carry the structural-only label plus the manifest-bound method citation, the paired runbooks carry the exact focused integration command, and runtime verification does not add a content scan.
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
- 2026-08-13: Applied all fifth-pass closure decisions and patches: repaired verifier and runbook
  guards, pinned the manifest-bound core/positive/control methods, proved endpoint rotation, restored
  governance continuity, structured the eleven deferred entries, and reconciled the raw worktree set.
- 2026-08-13: Seventh-pass `bmad-build` review applied all 26 sixth-pass patches, repaired the
  allocation-failure disposal and carried-forward ledger schema exposed by verification, reconciled
  the then-current raw union, and reran every command in this spec against current binaries.
- 2026-08-14: Eighth-pass closure applied AC5, the AC3 D4 split, story-own File List
  derivation, structural-prefix failure/unavailable Details, source and received-call
  guard repairs, planted-edge tenant-B re-traversal, and runbook topology preconditions.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` -- expected: the project compiles; record any package-audit advisory, then use `-p:NuGetAudit=false` to isolate a warning-free compile signal.
- `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` -- expected: real FalkorDB collision proof passes with this machine's active local Dapr placement/scheduler mappings.
- `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.GraphScopedSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.SyntacticSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.SemanticSearchIntegrationTests` -- expected: canonical negatives pass with this machine's active local Dapr placement/scheduler mappings.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false && DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Deployment.OperationalRunbookSetTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` -- expected: verifier, doc, and denial-before-dependency gates pass.

**Results (seventh-pass rerun, 2026-08-13):**

- Integration Debug build passed with 0 errors and two `NU1903` package-audit advisories for
  `SSH.NET` 2025.1.0; the audit-disabled compile fallback passed with 0 warnings and 0 errors in
  49.51 seconds.
- Real-backend collision proof passed after review hardening: 1 total, 0 failed, 0 skipped,
  15.572 seconds. The local
  Dapr services were exposed on host ports 6050/6060, so the passing invocation supplied
  `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050` and
  `MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060`; Aspire provisioned the fixture-owned
  FalkorDB and both authenticated traversal requests completed against it.
- Canonical tenant/search negatives passed after review hardening: 63 total, 0 failed, 0 skipped,
  241.613 seconds, using the same local Dapr service-address prerequisites. This reverified the four
  canonical classes after the previously gated removal of the redundant all-axis test.
- Server verifier, runbook, and denial-before-dependency gate passed after a clean Debug build:
  **58 total**, 0 failed, 0 skipped, 8.333 seconds. The count includes the authenticated
  traversal-path denial-before-dependency cases added during review.
- The exact Epic 24/Epic 25 checklist structural preservation command from
  `spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md` exited 0,
  proving one checklist heading and exactly one row for each of the six named invariants in both contexts.

**Tenant-isolation negative evidence:**

- Authoritative D3 result: the final full `TenantIsolationIntegrationTests` class run passed 7 total,
  0 failed, 0 skipped in 243.340 seconds against one fresh real Aspire/FalkorDB topology. This one
  owning-class run supersedes every earlier method/class duration recorded in historical review rows.

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
- Mutation-control result: covered by the authoritative 7/7 owning-class run in 243.340 seconds; no
  earlier standalone method duration is authoritative.
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
- P1 mutation control: `TenantIsolationIntegrationTests.VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage`
  passed inside the final authoritative owning-class run against the real Aspire-provisioned FalkorDB
  topology on 2026-08-13. It seeds both colliding tenant graphs, rewrites tenant A's edge with
  tenant B's marker through `GraphQueryBuilder.BuildUpdateEdgeConfidence`, confirms the foreign marker
  is returned through tenant A's authenticated traversal, and requires `AssertTraversalIsFixtureLocal`
  to throw for the tenant-local edge-marker assertion.
- P1 regression evidence: the final `TenantIsolationIntegrationTests` class passed 7 total, 0 failed,
  0 skipped in 243.340 seconds and supersedes the earlier separate positive/control timings; the
  IntegrationTests Debug build passed with 0 warnings/errors; and the combined verifier/runbook/denial
  gate passed 58 total, 0 failed, 0 skipped in 6.885 seconds after a clean Server.Tests Debug build.
  All commands used the documented `localhost:6050` placement and
  `localhost:6060` scheduler mappings where applicable.

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

- [x] [Review][Patch] (F5-P1) **[high]** Three in-scope changed paths are exempted by prose only.
  `references/Hexalith.Builds`, `references/Hexalith.EventStore`, `references/Hexalith.FrontComposer`
  are excluded in File List prose and two ledger cells, but `### File List Exclusions` holds exactly
  one bullet. `story-phase-ledger.md:118-131` is explicit that prose "does not exempt a path from the
  executable gate". Re-run independently: the raw 19-path set exits **1** with three `C1: ... changed
  but is not in the File List` violations, while the same run accepts `bmad_customization_test.py` —
  proving the machine-readable mechanism works and simply was not used. Every recorded "readiness
  passed" in this story was produced against a hand-curated 16-path input. Add three exclusion bullets
  with path, named owner, and reason, then re-record readiness against the raw set.
  [24-6-...md:163,181-183]
- [x] [Review][Patch] (F5-P2) **APPLIED 2026-08-13** — lifecycle surfaces now carry their
  phase-correct review states: story `review`, `sprint-status.yaml` `review`, `epics.md:4598`
  `in-progress`, and spec frontmatter `status: 'in-review'`; the canonical spec retains its completed
  review history at `review_loop_iteration: 5`. The separate fifth-pass closure spec remains
  `status: 'in-review'` with `review_loop_iteration: 0` until its own review begins.
  [epics.md:4598; spec:5-6]
- [x] [Review][Patch] (F5-P3) `deferred-work.md:1900` `Evidence:` is a phase stale — records the owning
  class at **6 total / 17.572 s** and the collision method at 175.204 s, superseded by 7/7 in 214.351 s
  and 1/1 in 209.076 s. It never names the P1 negative control, its `Re-open trigger:` protects only the
  positive method (so deleting the control trips nothing), and its status rationale still reads "Story
  24.6 returned to `in-progress`", a status the story no longer holds. Same shape as second-pass P5,
  regressed by the P1 closure. [deferred-work.md:1900-1901]
- [x] [Review][Patch] (F5-P4) The eleven new deferred entries appended by the first pass carry **none**
  of the required schema fields the same file documents at `:11-29` (`ID:`, `Status:`, `Source story:`,
  `Target artifact:`, `Re-open trigger:`, and one of `Evidence:`/`Rationale:`). They are bare prose
  bullets — unreferenceable by ID and invisible to the schema gate. The "legacy prose entries remain
  valid" clause covers pre-existing entries, not new ones. [deferred-work.md:3034+]
- [x] [Review][Patch] (F5-P5) Checkpoint C2's `Evidence command / artifact` cell contains only results
  ("Restart regression 1/1 passing in 2.5443 min; ..."), no command and no artifact — while
  `story-scope-guard.md` permits the umbrella exemption *only when* every checkpoint has one. C2 also has
  no row in the `## Verification` table. The reproducible invocation already exists at ledger row `:155`;
  lift it into the cell, add the PR #53 CI run as the artifact, and add a C2 verification row.
  [24-6-...md:58,117-133]
- [x] [Review][Patch] (F5-P6) The D2 umbrella ratification lives only in the story file. `epics.md:4597`
  changed by exactly one line (`backlog` -> `in-progress`) and carries no umbrella note; the repo's own
  precedent is `epics.md:5381,5395` (Story 31.1). At both surfaces the policy names as binding, Story
  24.6 still reads as a one-outcome story. [epics.md:4597]
- [x] [Review][Patch] (F5-P7) The final `code-review` ledger row omits four of the five required
  `Test count` elements — no cumulative story delta, no per-lane before/after totals, no named unit
  bound to each lane, and **no evidence command at all**. Every earlier row carries all five. This is
  the identical defect second-pass P8 recorded against the then-`ci-repair` row. [24-6-...md:159]
- [x] [Review][Patch] (F5-P8) The proof-method citation is unbound. The method name appears at seven
  sites; the two that guard it (`OperationalRunbookSetTests.cs:375`, `TenantIsolationVerifierTests.cs:381`)
  compare the verifier/runbook text to **string literals declared inside the tests**. Renaming the test
  method and updating `integration-fast-required-surfaces.txt` — the only edit CI forces — leaves both
  guards green while the operator-facing verify response and both runbooks instruct operators to run a
  `-method` invocation that resolves to no test. Derive the literal by parsing the manifest in
  `OperationalRunbookSetTests` and reuse it in the verifier assertion.
- [x] [Review][Patch] (F5-P9) Two new **required** `integration-fast` method surfaces were pinned with
  no lane evidence. The owning class carries only `[Trait("Category", "Integration")]` — not
  `IntegrationSlow` — so both methods, measured locally at 190.569 s and 209.076 s against real
  FalkorDB, now execute in CI's required fast lane (`ci.yml:429`). The only recorded post-patch evidence
  is `integration_fast_coverage` 6/6, which exercises the verifier script against synthetic TRX
  fixtures, not this manifest against a real lane result. Cite a green integration-fast run at HEAD or
  record an accepted blocker with owner, consequence, and reopen trigger.
- [x] [Review][Patch] (F5-P10) AC3's operator-observable output is pinned only by mocks. The `Details`
  string returned over `POST /api/v1/tenants/{tenantId}/verify` is asserted solely in the NSubstitute-mocked
  `TenantIsolationVerifierTests`, while the real-backend `VerifyTenant_WithTwoProvisionedTenants_CoreIsolationChecksShouldPass`
  already POSTs that endpoint and asserts nothing about `Details`. For a story whose thesis is that
  internal signals must not be mistaken for observable proof, AC3 closes on the shape it exists to
  forbid. Add the assertion to the existing real-backend test — no new method, no new lane.
- [x] [Review][Patch] (F5-P11) The P1 closure is partial and undisclosed. The negative control plants
  only `edgeMarkerB` and pins the thrown message to "tenant-local edge marker", exercising one of the
  ~15 assertions in `AssertTraversalIsFixtureLocal`. The four node-marker assertions — which carry
  NFR8's actual "zero tenant-B **nodes**" claim — have no failure-sensitivity control. Checkpoint C1
  reads an unqualified `complete` and the spec reads "P1 and N4 are closed". Disclose the boundary;
  a second control planting `nodeMarkerB` is the stronger optional fix. Related: the control is listed
  under **Cross-Tenant Negative Evidence -> Tests** although it never crosses tenants — it writes a
  literal into tenant A's own edge, making it assertion-sensitivity evidence, not cross-tenant evidence.
  [TenantIsolationIntegrationTests.cs:96-146]
- [x] [Review][Patch] (F5-P12) The repaired AC3 source guard still has escape hatches: the regex matches
  only `ExecuteAsync("<literal>")` in one file, and the only prohibition is the literal `GRAPH.QUERY`.
  A content read added as `GRAPH.RO_QUERY`, `GRAPH.EXPLAIN`, or `GRAPH.PROFILE`, issued through a
  `const`/interpolation, via sync `Execute`, or from another partial file, passes both assertions.
  Reject any `GRAPH.` command other than `GRAPH.LIST`, scan every `TenantIsolationVerifier*.cs`, and
  assert the full `ReceivedCalls()` surface rather than only calls named `ExecuteAsync`.
  [OperationalRunbookSetTests.cs:410-423; TenantIsolationVerifierTests.cs:387-394]
- [x] [Review][Patch] (F5-P13) `InPlaceOpenBaoRestart_RotatesGenerationAndRecoversDependentSidecars`
  never asserts the sidecar endpoint actually rotated. `ReconnectPrimaryDaprClients` is called only when
  `currentDaprEndpoint != DaprSidecarHttpEndpoint`, so if the sidecar returns on the same port the
  reconnect never runs and the new actor call succeeds against the original factory — green, with the
  repair unexercised. Capture the endpoint before and after and assert it changed.
  [OpenBaoTopologyIntegrationTests.cs:148-156; AspireIngestionPipelineFixture.cs:642]
- [x] [Review][Patch] (F5-P14) D2's second half was never applied. The Slice Proof's **Demonstration
  boundary** still describes only the graph outcome and the **Excluded** list is unchanged from the
  pre-D2 single-outcome text — it never mentions CI or fixture work. C2 has neither a demonstration
  boundary nor exclusions. [24-6-...md:49-51]
- [x] [Review][Patch] (F5-P15) No `Historical Context Classification` row for Story 29.1, which owns
  both fixture files C2 modifies (`29-1-...md:333,340`) and whose own review created the restart
  regression this story expands (`29-1-...md:104`). `historical-reference-only` is the correct label —
  `29-1-...md` carries none of the policy's anti-template marker phrases. [24-6-...md:31-36]
- [x] [Review][Patch] (F5-P16) `VerifyTenant_WithTwoProvisionedTenants_CoreIsolationChecksShouldPass`
  is the test the AC2 redundancy justification actually rests on — the third pass established that the
  deleted test's *name* was never the real coverage — yet it is the one method not pinned in
  `integration-fast-required-surfaces.txt`. Renaming or skipping it silently removes AC2's stated
  coverage while the lane stays green. [tools/integration-fast-required-surfaces.txt]
- [x] [Review][Patch] (F5-P17) The spec now carries **two** `## Suggested Review Order` H2 sections
  (`:157` and `:382`) with divergent anchors; the fourth pass appended a new one instead of updating
  the third pass's corrected one. A top-down reader — and any heading lookup, the pattern this repo's
  own `MarkdownContractDocument.GetSection` uses — takes the stale first. [spec:157]
- [x] [Review][Patch] (F5-P18) Fourth-pass anchor drift: "CI now requires **both** positive and
  planted-leak graph methods" cites `integration-fast-required-surfaces.txt:15`, which is only the
  positive entry; the negative control is line **16**. [spec:422-423]
- [x] [Review][Patch] (F5-P19) The "Runbook evidence-boundary guard (AC3 docs)" verification row is
  dated `passed 2026-08-12`, but `OperationalRunbookSetTests.cs` was last changed **2026-08-13** by the
  N5 repair, which rewrote the assertions inside exactly that row's subject test. The standalone
  measurement predates the change it is cited to cover. [24-6-...md:127]
- [x] [Review][Patch] (F5-P20) Checkpoint C1's `Review state` reads "implementation closure ready for
  review" while its `Completion state` reads "complete 2026-08-13". Dated, so C6 passes at `review`;
  under `done` the cell asserts the checkpoint's own review was never performed — the false-record shape
  N3 repaired one pass earlier. [24-6-...md:57]
- [x] [Review][Patch] (F5-P21) `docs/operations/route-surface.md` closes the new section with two
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

## Review Findings — Sixth Pass (closure review 2026-08-13)

Adversarial closure review over the uncommitted fifth-pass closure delta (`git diff HEAD`, 15 files,
+476/-154, 1103 lines), scope chosen by the Administrator over the cumulative 24-path range because
the fifth pass already reviewed `0ecdffed..HEAD` in full. `HEAD` (`3e92ca36`) has a tree byte-identical
to `1d2862f4` — the two `/pushall` merges re-landed work already on `main` — so this working tree **is**
the closure delta named by `spec-24-6-fifth-pass-action-item-closure.md`. Six context-free layers
(blind hunter, edge-case hunter, verification-gap, acceptance auditor, historical-slice guard,
story-phase-ledger auditor) plus independent parent-side re-execution. **The review was not chunked:**
the complete 1103-line diff was reviewed in this single invocation, so no chunk remains outstanding.

**Independently re-executed at the current worktree, not accepted on the record:** three-class server
gate **58 total, 0 failed, 0 skipped**; `CiTestInventoryTests` **66/66**; tooling suites **4 / 6 / 45 /
41 / 20** all OK; manifest **19** non-comment entries; `verify-integration-fast-coverage.py` exit 0
with all 19 required surfaces satisfied; TRX `total="329" passed="321" failed="0"`; readiness,
slice-scope, and tenant-isolation-evidence gates exit 0 against the true 24-path union;
`git diff --check` clean; sprint-status parses as YAML. Every one of those figures matched the record.

**Refuted hypothesis, recorded so it is not re-raised:** the integration assemblies (Debug 14:12:16,
Release 14:19:59) predate the sources' `15:08:42` mtime, which suggested the real-backend evidence was
stale. An ASCII `strings` probe appeared to confirm it. That probe was wrong — .NET stores literals as
UTF-16. Re-probed with `strings -el` plus positive controls, both assemblies **do** contain
`Structural database-existence evidence only` and `the restart regression must rotate the primary Dapr
endpoint before proving actor recovery.` The later mtime is a line-ending normalization touch, not a
content edit. **The 7/7, 1/1 restart, and 321-pass lane results are current, not stale.**

**AC1 continues to hold in substance:** the collision fixture proves content-level tenant isolation
against a real FalkorDB backend through authenticated per-tenant routes. The defects below are in the
AC3 guard mechanism and in the evidence/status accounting, not in the content-isolation proof.

**Seventh-pass disposition (2026-08-13):** all 26 patch items below were applied and independently
verified. Three required context-free layers reviewed the complete diff. Their live findings converged
on the source/received-call guards, raw-union accounting, status/evidence coherence, and one fixture
allocation-failure leak; no intent gap or bad-spec loopback was found. The node-marker mutation gap
and FalkorDB timeout result-shaping remain deferred, while repeated pre-existing boundaries retain
their existing deferred homes rather than being duplicated.

### Decisions needed — sixth pass

- [x] [Review][Decision] (F6-D1) **RESOLVED 2026-08-13** (Administrator: option (a) — the ratified C1 boundary is edge-only as recorded, so C1 remains `complete`. The resulting work is new patch F6-P26 plus F6-P19, both open below and left as action items.) C1 reads `complete` while carving out the mutation-sensitivity boundary for the assertions that carry AC1's own claim. `24-6-...md:59` records "node-marker mutation sensitivity remains outside the completed boundary", and `:122-129` states the four node-marker assertions have no equivalent mutation control. But AC1 (`:24`) and PRD NFR8 (`prd.md:971`, re-derived this pass) both state the claim in terms of **nodes** — "verify zero nodes from tenant B appear even if edge IDs collide". Meanwhile the authoritative C1 exclusions list at `:51` names six exclusions and does **not** include node-marker mutation sensitivity, so the boundary is disclosed in one cell and contradicted by omission in the list that governs it. Decide: **(a)** the ratified C1 boundary is edge-only as recorded — then add node-marker mutation sensitivity to the `:51` exclusions and open a structured deferred entry (F6-P19); or **(b)** node-marker failure-sensitivity is inside C1's ratified boundary — then C1 cannot read `complete`, and a node-marker mutation control is required before `done`. This is a scope call on the ratified checkpoint, not a record repair.

### Patches — sixth pass

- [x] [Review][Patch] (F6-P1) **[high]** The recorded raw-union generation command is index-blind and does not reproduce its own recorded result [`24-6-graph-content-level-tenant-isolation-evidence.md:149`, `:181`, `:185`; `spec-24-6-fifth-pass-action-item-closure.md:84`, `:107`] — every one of the 15 changes is **staged**, so `git diff --name-only` (index→worktree) returns 0 paths and `git ls-files --others` returns 0. Run verbatim, the recorded command yields **22** paths and readiness reports `C1: all 22 changed paths are declared.` exit 0 — not the recorded 24. The two silently dropped paths are this closure's own newest artifacts: `spec-24-6-fifth-pass-action-item-closure.md` (staged add) and `sprint-change-proposal-2026-08-04-...md` (staged modify). Substituting `git diff --name-only HEAD` yields exactly 24 and readiness reports `C1: all 24 changed paths are declared.` exit 0. **The 24-path scope claim is true; the command attesting it is not**, and as written it would let any staged, undeclared path escape the gate while still exiting 0 — the same "a curated input is not a reconciliation" defect F5-P1 was raised to cure, reintroduced by the command itself. Fix: replace `git diff --name-only` with `git diff --name-only HEAD` in all four locations and re-record the observed count and C1 line.
- [x] [Review][Patch] (F6-P2) **[high]** The AC3 source guard has regressed to the vacuous shape N5 repaired one pass earlier [`OperationalRunbookSetTests.cs:424-442`] — N5 replaced a count of every `ExecuteAsync` literal with a filter over `GRAPH.`-prefixed **call-site literals**, precisely so that deleting the last `GRAPH.LIST` call could not leave the guard green. This diff replaces that with `Regex.Matches(verifierSource, @"\bGRAPH\.[A-Z][A-Z0-9_.]*\b", IgnoreCase)` over the **joined file text**, which matches prose. `TenantIsolationVerifier.cs` carries `GRAPH.LIST` in a comment (`:114`), a failure message (`:324`) and the `Details` string (`:339`). Simulated by deleting all three real `ExecuteAsync("GRAPH.LIST")` calls: tokens still found `['GRAPH.LIST']`, `ShouldNotBeEmpty()` PASS, `ShouldAllBe(== "GRAPH.LIST")` PASS — **the guard is GREEN with zero real GRAPH.LIST calls**. The `verifierSource.ShouldNotContain("GRAPH.QUERY")` line was also deleted in the same hunk. Fix: match command literals at the call site (or strip comments and string literals before tokenizing) so `ShouldNotBeEmpty()` is non-vacuous again.
- [x] [Review][Patch] (F6-P3) **[high]** The received-call assertion no longer bounds non-`GRAPH.` commands, and its new comment overclaims [`TenantIsolationVerifierTests.cs:386-395`] — the pre-diff form collected every `ExecuteAsync` first argument on the FalkorDB mock and required each to equal `GRAPH.LIST`. The new form applies `.Where(command => command.StartsWith("GRAPH.", OrdinalIgnoreCase))` **before** the equality check, so a tenant-scoped content read that is not a `GRAPH.` command — `ExecuteAsync("KEYS", $"{tenantId}:*")`, `SCAN`, `HGETALL` — is dropped from the set entirely and both this guard and the source guard stay green. The pre-diff assertion failed on exactly that change. Separately, the new comment claims the rewrite ensures "a non-first command argument cannot evade the structural-only boundary"; `IDatabase.ExecuteAsync(String, Object[])` takes a **params array**, so `GetArguments()` returns `[command, object[] args]` and `.OfType<string>()` never flattens `args` — a command placed in the params array is still invisible. Fix: assert over the unfiltered received-string set and use the `GRAPH.` filter only for the non-empty check; flatten the params array; correct the comment.
- [x] [Review][Patch] (F6-P4) **[high]** The companion closure spec declares `status: 'done'` with `review_loop_iteration: 0` [`spec-24-6-fifth-pass-action-item-closure.md:5-6`] — contradicting four records in the same change, including its own applied F5-P2 text at `spec-24-6-...md:482-483` ("The separate fifth-pass closure spec **remains `status: 'in-review'`** with `review_loop_iteration: 0` until its own review begins"), its Spec Change Log ("submitted the closure for review"), `sprint-status.yaml:426` ("Closure review remains active"), and the story's own `review`. A spec at `done` with zero review iterations asserts a review that never ran. Fix: restate to `in-review` (see F6-P15 on the vocabulary).
- [x] [Review][Patch] (F6-P5) **[high]** `24.3-GRAPH-CONTENT-EVIDENCE` promoted to `Status: resolved` while its owning story is only at `review`, with the promotion rule deleted rather than satisfied [`deferred-work.md:1891-1893`] — the removed sentence read "Status is `carried-forward` rather than `resolved` because the owning Story 24.6 returned to `in-progress`; **it moves to `resolved` when Story 24.6 reaches `done`**." Story 24.6 is `review` in this very change, and the closure ledger row itself says "Story closure remains in review". Third recurrence: the first-pass patch and third-pass N2 each repaired it before. `CiTestInventoryTests` cannot catch it (66/66 green — it checks only vocabulary and `Evidence:` presence). Fix: restore `carried-forward` with its promotion condition until Story 24.6 reaches `done`.
- [x] [Review][Patch] (F6-P6) **[medium]** `epics.md` still records Story 24.6 as `in-progress` [`epics.md:4599`] while the story file, `sprint-status.yaml:426`, and the spec all read `review`/`in-review`. The same change edited `epics.md` (adding the umbrella note two lines below) without updating the Status line, so the closure spec's AC4 requirement that "epic/spec/story/sprint status … agree without stale claims" fails on the epic surface.
- [x] [Review][Patch] (F6-P7) **[medium]** The umbrella exception is attributed to the wrong decision in two planning artifacts [`epics.md:4602`; `sprint-change-proposal-2026-08-04-...md:159-163`] — both read "Decision D2 from the fifth-pass closure review". The umbrella was ratified by **second-pass D2** on 2026-08-12 (`spec-24-6-...md:260`, "option 2 — ratified as an approved multi-checkpoint story"); the fifth pass's own **F5-D2** is the verifier class-contract decision (`:443`), and **F5-D1** (`:438`) is what authorized propagating the note. `deferred-work.md` entry `24.6-CR-W5` uses "Fifth-pass Decision D2" correctly for the verifier decision, so the same label now denotes two different decisions across the governance record. This is the L14 citation-drift class. Fix: attribute the umbrella to second-pass D2 (2026-08-12), propagated per F5-D1.
- [x] [Review][Patch] (F6-P8) **[medium]** The superseded criterion still stands at the proposal's success criteria [`sprint-change-proposal-2026-08-04-...md:507`] — "Stories 24.6-24.9 are registered with one independent outcome …". F5-P6 added the supersession blockquote at `:159-163` inside the Story 24.6 narrative but not at the criterion it supersedes, so the authoritative success-criteria section still reads Story 24.6 as a one-outcome story. The §2.2 sequencing row at `:69` carries the same reading.
- [x] [Review][Patch] (F6-P9) **[medium]** The Epic 23 checklist row records a pass for a period in which the gate was red, and was not re-dated after this diff restored it [`24-6-...md:151`] — re-derived per commit, the `## Review Checklist — Epic 23 Ingestion Invariants` heading in `epic-24-context.md` is present at `c64e5514` and `36e1c551`, and **absent at `cb8e960f`, `1d2862f4`, and `HEAD`** (lost inside this story's own range). The recorded loop exits **1** at HEAD. This diff restores the section, so the gate now passes — but the row still reads `Exit 0, re-confirmed after the review reverted the epic-24 context regeneration | passed 2026-08-12`, and the closure spec's command list omits the Epic 23 command entirely.
- [x] [Review][Patch] (F6-P10) **[medium]** Checkpoint C2 cites a CI artifact that predates the assertion it is offered to prove [`24-6-...md:60`, `:144`] — the `[PR #53 integration-fast CI run]` link resolves to work merged as `c64e5514` (2026-08-12), while the endpoint-rotation assertion is added in this 2026-08-13 diff. `git log -S 'daprEndpointBeforeRestart'` returns **no commit**: the assertion exists only in the uncommitted worktree, so that run cannot have executed it. A valid at-HEAD artifact does exist — the Release integration-fast lane whose TRX satisfies the new `openbao-restart-actor-recovery` required surface. Fix: cite the at-HEAD artifact, or mark the PR #53 link as historical.
- [x] [Review][Patch] (F6-P11) **[medium]** Two mutually exclusive "final" durations are recorded for the same authoritative 58/58 server gate — **9.978 s** "after the final review patch and a clean Debug build" [`24-6-...md:146`, `:181`; closure spec `:99`] versus **6.885 s** "after a clean Debug build" [`deferred-work.md:1899`; `spec-24-6-...md:161`]. Both are described as post-patch. This is the mutually-inconsistent-duration defect F5-D3 was raised to cure, reintroduced by the patch set that claims to close it. (Independent re-runs this pass: 17.375 s and, from a second layer, 9.764 s — count identical at 58/58, so only the recorded durations disagree.)
- [x] [Review][Patch] (F6-P12) **[medium]** The bare `--story-key` readiness invocation and its exit-1 disclosure were deleted from the Verification table — `story-phase-ledger.md:172-177` requires recording "the exact invocation and its final line", and third-pass finding N1 existed precisely to keep the bare form's failure disclosed. `grep` now finds **zero** occurrences of it in the story. Re-run this pass: `python3 tools/check-story-review-readiness.py --story-key 24-6-graph-content-level-tenant-isolation-evidence` exits **1** with `C1: the changed set is empty for a governed story. An empty set must fail closed rather than pass vacuously.` Fix: restore a row recording both invocation forms with their true exit codes.
- [x] [Review][Patch] (F6-P13) **[medium]** The `dev-story` ledger row asserts a declared path is out of scope [`24-6-...md:173`] — it states `epic-24-context.md` "was reverted by the first-pass code review and so is **absent from the cumulative set at HEAD**". `git diff --numstat 0ecdffed..HEAD -- _bmad-output/implementation-artifacts/epic-24-context.md` returns `24 53`: the path **is** in the cumulative set, **is** declared at `## File List` (`:189`), and is modified again by this diff (+13, the Epic 23 restoration). A top-down reader is told a declared, in-scope path is not in scope.
- [x] [Review][Patch] (F6-P14) **[medium]** Both checkpoint `Review state` cells pre-certify a review that had not run when they were written [`24-6-...md:59`, `:60`] — both now read "reviewed 2026-08-13 (fifth-pass closure…)", but the same diff's own fifth-pass ledger row records that phase repaired nothing ("no source, test, or governance defect was repaired this phase", `matched 16/19 — not reconciled`), and every substantive change here (D2 verifier restore, F5-P8/P10/P12/P13/P16) post-dates the review the cells name. The closure was "submitted for review" — this one. Fix: restate to the true state, or let the appended sixth-pass `code-review` ledger row re-date them.
- [x] [Review][Patch] (F6-P15) **[medium]** Spec status `'in-review'` is outside the repository's closed status vocabulary [`spec-24-6-...md:5`] — `tools/check-story-review-readiness.py:83` defines `VALID_STATUSES = ("backlog", "ready-for-dev", "in-progress", "review", "done")` and C3 fails any other value. `in-review` is the only such value across the implementation-artifact specs (27 use `'done'`, 9 `'ready-for-dev'`, 1 `review`). It escapes C3 only because readiness no-ops on spec artifacts that carry no ledger. Fix: use `review`.
- [x] [Review][Patch] (F6-P16) **[medium]** The "Verifier structural-only boundary (AC3)" row is still dated `passed 2026-08-12` although its subject file was rewritten on 2026-08-13 by this diff [`24-6-...md:142`] — `TenantIsolationVerifierTests.cs:381` (manifest-bound citation) and `:386-395` (received-call rewrite) both changed. The adjacent runbook row at `:143` was correctly moved to `passed 2026-08-13`; this one was not. Same F5-P19 "measurement predates the change it covers" shape.
- [x] [Review][Patch] (F6-P17) **[medium]** The verifier source scan is narrower than the comment describing it [`OperationalRunbookSetTests.cs:424-430`] — the comment says "Scan every verifier source file recursively", but `Directory.GetFiles(ResolveRepoPath("src","Hexalith.Memories.Server","Tenants"), "TenantIsolationVerifier*.cs", AllDirectories)` is rooted at one directory and one filename prefix while `TenantIsolationVerifier` is declared `partial`. A part added under any other folder, or a differently named helper, escapes the guard entirely. Only one such file exists today, so this is latent — but it compounds F6-P2. Fix: discover files by `partial class TenantIsolationVerifier` declaration across the Server project, or assert the discovered set matches the declaration sites.
- [x] [Review][Patch] (F6-P18) **[medium]** Epic AC Verification row 2 carries stale line anchors after this diff moved them [`24-6-...md:112`] — the row records "three `GRAPH.LIST` calls (lines 116, 315, 358), zero `GRAPH.QUERY`". Re-run this pass (the row's subject file is touched by the reviewed diff, so `epic-ac-verification.md:146-147` requires re-derivation): the calls are now at **116, 317, 360**, shifted by the two-line comment inserted at `:305-306`. The **verdict holds** — three `GRAPH.LIST`, zero `GRAPH.QUERY` — only the anchors are stale. Rows 1 and 3 re-derived exactly as recorded (`prd.md:971`; `TenantContextEnforcement:155`, `Semantic:119`, `Syntactic:108`, `GraphScoped:118`).
- [x] [Review][Patch] (F6-P19) **[low]** The newly disclosed node-marker mutation gap has no structured deferred entry — the story and `deferred-work.md:1899` acknowledge in prose that the control "does not mutation-test the node-marker assertions", but no entry with `ID:`/`Status:`/`Re-open trigger:` was created, in the very change that converted eleven other residual risks into exactly that form. Contingent on F6-D1 option (a).
- [x] [Review][Patch] (F6-P26) **[medium]** The C1 exclusions list does not name the boundary C1's completion cell carves out [`24-6-...md:51`] — resolved by F6-D1 option (a): the ratified C1 boundary is edge-only, so node-marker mutation sensitivity must be stated in the authoritative exclusions list at `:51` alongside the six existing exclusions, not only in the completion cell at `:59`. Until it is, the list that governs the ratified boundary contradicts the cell that discloses it.
- [x] [Review][Patch] (F6-P20) **[low]** Deferred entry `24.6-CR-W8` names a `Target artifact:` that does not exist — `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestionPipelineIntegrationTests.cs`. The repository's ingestion class is `IngestionPipelineTests.cs` (the name also used in the required-surfaces manifest). The schema gate does not check path existence, so the re-open trigger points at nothing.
- [x] [Review][Patch] (F6-P21) **[low]** Deferred entry `24.6-CR-W1` places two paths in the single-valued `Target artifact:` field, which `deferred-work.md:21-23` defines as "the repository-relative path … the entry targets".
- [x] [Review][Patch] (F6-P22) **[low]** The F5-P18 anchor repair was computed against the pre-insertion manifest [`spec-24-6-...md:398-400`] — the bullet "CI now requires both positive and planted-leak graph methods" cites `integration-fast-required-surfaces.txt:16` and `:17`. Re-derived: line **16** is `tenant-isolation-core-verifier` (a third pin added by this same diff), line **17** is `graph-content-isolation` (positive), line **18** is `graph-content-isolation-assertion-sensitivity` (planted-leak). Neither cited line is the planted-leak entry. Should be `:17` and `:18`.
- [x] [Review][Patch] (F6-P23) **[low]** Two closure-spec review-order anchors are off target [`spec-24-6-fifth-pass-action-item-closure.md:128`, `:142`] — `TenantIsolationVerifierTests.cs:383` is a **blank line** (the described received-call inspection begins at `:384-386`), and `deferred-work.md:3037` lands mid-intro-paragraph rather than on the `## Deferred from: …(2026-08-12)` heading at `:3034`. The other eight anchors in that section resolve exactly.
- [x] [Review][Patch] (F6-P24) **[low]** The D4 paragraph was inserted between a sentence and the list it introduces [`24-6-...md:64-74`] — "…the redundant all-axis test was removed after these canonical search negatives were cited and passed:" is now followed by four lines of unrelated AC3 obligation text before the four bullets it introduces.
- [x] [Review][Patch] (F6-P25) **[low]** Dev Notes retain a superseded readiness statement [`24-6-...md:98`] — "The real-backend collision and canonical search lanes executed successfully on 2026-08-12; the story is ready for review", left beside the 2026-08-13 authoritative evidence that the same document declares supersedes it.

### Deferred — sixth pass

- [x] [Review][Defer] (F6-W1) The HTTP-visible AC3 assertion hard-codes the proof-method literal that F5-P8 unbound elsewhere [`TenantIsolationIntegrationTests.cs:69-71`] — the two Server.Tests guards now derive the citation from the required-surface manifest via `OperationalRunbookSetTests.GraphContentProofCitation`, but the integration test cannot reach that `internal` member across assemblies, so the literal is maintained in three places and a manifest-driven rename surfaces only after the ~21-minute integration lane. Fail-closed, so duplication rather than an escape hatch — deferred
- [x] [Review][Defer] (F6-W2) `24.6-CR-W5` was closed without addressing the original finding's second half — "no test pins either wording" — so the restored class-level XML summary has no detector and the entry's own re-open trigger cannot fire [`TenantIsolationVerifier.cs:18-19`] — deferred
- [x] [Review][Defer] (F6-W3) `result.Checks.Single(check => check.CheckName == "GraphIsolation")` throws a bare `InvalidOperationException` rather than a diagnosable assertion when the check is absent or duplicated [`TenantIsolationIntegrationTests.cs:65`] — deferred

### Dismissed — sixth pass (recorded so they are not re-raised)

- The new `ShouldNotBe(daprEndpointBeforeRestart, …)` makes a same-port sidecar restart red a required CI surface while C1/C2 exclusions list "same-port sidecar restart recovery" as out of scope. Dismissed: the behaviour is declared intentional in the closure spec's I/O matrix ("Unchanged endpoint fails the regression"), and `AspireIngestionPipelineFixture.cs:639-645` reconnects **only** when the endpoint differs — so on a same-port restart the clients are stale and the regression should fail. The assertion converts a confusing downstream failure into a named one.
- The closure spec's `baseline_commit: '1d2862f4'` differs from the `0ecdffed` baseline all reconciliation uses. Dismissed: each is correct for its own scope — the closure spec governs the `1d2862f4`→worktree delta, the story governs `0ecdffed`→worktree, and the closure spec's Verification explicitly uses the `0ecdffed` raw union for story reconciliation.
- No current full-`Hexalith.Memories.Server.Tests` assembly run covers the 2026-08-13 edits. Dismissed: the entire cumulative production delta is one XML doc comment and one `Details` string (`git diff 0ecdffed -- src/` = 3 insertions, 1 deletion), so the clean Debug build plus the focused 58-case gate is the proportionate proof the story already declares.
- Non-passing `GraphIsolation` branches carry no structural-only framing. Dismissed: already tracked as `24.6-CR-W6`, `Status: carried-forward`, with an owner and re-open trigger.

## Suggested Review Order

**Graph content-isolation proof**

- Start here: identical tenant graphs prove authenticated traversal remains fixture-local.
  [`TenantIsolationIntegrationTests.cs:75`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L75)

- The planted foreign edge marker proves the locality assertion fails closed.
  [`TenantIsolationIntegrationTests.cs:103`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L103)

- Cardinality, topology, and marker checks reject empty, degraded, or foreign results.
  [`TenantIsolationIntegrationTests.cs:300`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L300)

**Runtime structural boundary**

- Runtime verification remains read-only and structural through `GRAPH.LIST` only.
  [`TenantIsolationVerifier.cs:304`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L304)

- Received-call inspection rejects hidden commands and flattens parameter arrays.
  [`TenantIsolationVerifierTests.cs:387`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L387)

- Project-wide source discovery prevents comments or partial files bypassing the guard.
  [`OperationalRunbookSetTests.cs:392`](../../tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs#L392)

**Dapr restart recovery**

- Client replacement disposes partial allocations and swaps both Dapr clients atomically.
  [`AspireIngestionPipelineFixture.cs:1176`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs#L1176)

- The regression requires endpoint rotation before a real actor call succeeds.
  [`OpenBaoTopologyIntegrationTests.cs:148`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs#L148)

**Operator and CI evidence**

- Operators receive the structural/content distinction and portable proof command.
  [`tenant-onboarding-offboarding.md:161`](../../docs/operations/tenant-onboarding-offboarding.md#L161)

- Route guidance keeps authenticated canaries separate from two-tenant collision proof.
  [`route-surface.md:85`](../../docs/operations/route-surface.md#L85)

- Required surfaces bind restart, positive collision, and mutation-control methods to CI.
  [`integration-fast-required-surfaces.txt:9`](../../tools/integration-fast-required-surfaces.txt#L9)

**Review and lifecycle accounting**

- Two ratified checkpoints expose ownership, proof boundaries, and current review evidence.
  [`24-6-graph-content-level-tenant-isolation-evidence.md:55`](24-6-graph-content-level-tenant-isolation-evidence.md#L55)

- Structured deferred entries retain node-marker and timeout hardening without widening C1.
  [`deferred-work.md:3242`](deferred-work.md#L3242)

- Planning records preserve Story 24.6's narrow two-checkpoint umbrella exception.
  [`epics.md:4597`](../planning-artifacts/epics.md#L4597)

- The Epic 23 checklist remains intact across the regenerated Epic 24 context.
  [`epic-24-context.md:47`](epic-24-context.md#L47)

## Review Findings — Eighth Pass (code review 2026-08-14)

Scope chosen by the Administrator: `0ecdffed..7963579c` (29 files, +2113/-170, 2898 diff lines) — Story
24.6's own cumulative slice, isolated from Story 24.7's later commits so the reviewed range carries no
external-story contamination. **The review was not chunked:** the complete diff was reviewed in this
single invocation, so no chunk remains outstanding. Six context-free layers (blind hunter, edge-case
hunter, verification-gap, acceptance auditor, historical-slice guard, story-phase-ledger auditor) plus
independent parent-side re-execution.

AC1, AC2, and AC3 were re-confirmed to hold **in substance**. The defects below are evidence-currency and
governance-record defects, not defects in the graph-isolation proof itself.

### Independently re-executed (not accepted on the record)

- `python3 tools/check-story-slice-scope.py --story-key 24-6-graph-content-level-tenant-isolation-evidence` — exit 0.
- `python3 tools/check-tenant-isolation-evidence.py --story-key 24-6-graph-content-level-tenant-isolation-evidence --changed-files-file <raw 34>` — exit 0.
- `python3 tools/check-story-review-readiness.py --story-key 24-6-graph-content-level-tenant-isolation-evidence --changed-files-file <raw 34>` — **exit 1**, four unaccounted paths (E-P3).
- `python3 -m unittest discover -s tests/tooling/<suite> -p '*_test.py'` for `line_endings`, `integration_fast_coverage`, `story_review_readiness`, `tenant_isolation_evidence`, `story_slice_scope` — 4 / 6 / 45 / 41 / 20, all OK.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` — 66 total, 0 failed.
- `dotnet build tests/Hexalith.Memories.Server.Tests/... --configuration Debug --disable-build-servers -m:1 /nr:false` — 0 warnings, 0 errors; then the three-class gate — **86 total**, 0 failed (E-P4).
- `python3 tools/verify-integration-fast-coverage.py --results-directory TestResults/integration-fast` — exit 0, 19/19 surfaces, **against a TRX that predates the assertions it certifies** (E-P2).
- Epic AC rows 1–3 re-run: `prd.md:971` carries the NFR8 claim verbatim; three `GRAPH.LIST` and zero `GRAPH.QUERY` in the verifier; all four canonical search negatives present at the recorded lines. All three verdicts hold; row 2's line anchors drifted (E-P16).
- Epic 23 checklist preservation loop — exit 0 for both epic contexts, while `grep -c NFR8` and `grep -c D29` on the same file both return 0 (E-P1).

### Decisions needed — eighth pass

- **E-D1. Checkpoint C2 has no acceptance criterion at any surface.** Story ACs 1–4 and `epics.md:4596-4620` are entirely about graph content isolation; none mentions OpenBao, Dapr sidecar endpoints, or actor proxies. C2 nevertheless records `complete 2026-08-13`. The ratified umbrella supplies owner, evidence, review state, and completion state, but no AC defines what C2's completion *means*, so it rests on the implementer's restatement. Options: (a) re-key C2 to its own numbered story with its own AC; (b) keep the umbrella and add an explicit AC5 to both the story and `epics.md`. Both change approved scope.
- **E-D2. Two exclusion bullets name one owner where the history shows two.** `references/Hexalith.FrontComposer` moved under `spec-submodule-bumps-2026-08-11` (`09b3eb61`, `cb863b46`), `spec-pushall-sync-2026-08-13` (`c58d6431`), and `spec-pushall-sync-2026-08-14` (`30104162`); `references/Hexalith.Builds` under the first and last. The named owner's own record pins FrontComposer at `1fa8b0b1` while the reviewed diff shows `d3554825`, and `spec-pushall-sync-2026-08-13.md` claims FrontComposer in its own File Scope. A single-valued owner field cannot carry two envelopes: name the latest owner, enumerate both, or split the bullet.
- **E-D3. No `correct-course` row although `epics.md` and the frontmatter-declared `approved_change` proposal were both amended in range.** The amendments rewrite Story 24.6's own success criterion from "one independent outcome" to the two-checkpoint umbrella. `story-phase-ledger.md:8-16` makes `correct-course` canonical when that phase actually ran, with Story 31.1 as the worked example. Counter-argument on record: the proposal was approved before the baseline, and the amendments were performed by the 2026-08-13 `qa-gap-closure` row, which did perform the work. Genuinely two-way.
- **E-D4. C2's declared exclusion contradicts a required CI gate.** The Slice Proof lists "same-port sidecar restart recovery" as out of scope for C2, while `OpenBaoTopologyIntegrationTests.cs` now asserts `fixture.DaprSidecarHttpEndpoint.ShouldNotBe(daprEndpointBeforeRestart, ...)` and that method is pinned as a **required** `integration-fast` surface. A same-port restart — declared out of scope — now hard-fails a required gate. Either the exclusion text or the assertion must move.
- **E-D5. AC3's text and its implementation diverge under a ratified reading.** AC3 requires that "verifier **and** operator documentation ... cite the focused integration command". The verifier's `Details` string cites a method *name* with no command; only the two runbooks carry the invocation. Decision D4 ratified this split, but the AC text was never amended. Amending frozen acceptance text is human-owned; so is deciding to put a command in the verifier instead.

### Patches — eighth pass

- **E-P1. [high] The `epic-24-context.md` anchor loss recurred and two review records assert the opposite.** Traced per commit on the file: `0ecdffed` NFR8=1/D29=1 (82 lines) → `fdc76064` 0/0 (69) → `dbab1daa` 1/1 (82, the Decision D1 restore) → holds through `36e1c551` → **`cb8e960f` 0/0 (53 lines), a `build: sync local changes via /pushall` commit that silently reverted a decision-mandated repair** → `7963579c` 0/0 (66; the seventh pass restored the *Epic 23 checklist* sibling at the same location without re-checking these) → HEAD 0/0. Also lost: the epic-wide audit-anchor preflight bullet ("re-verify the audit anchors ... record the verification date"), which binds Stories 24.7–24.13; the FalkorDB data-vs-resource isolation constraint, which is what made C1's "graph resource isolation" exclusion meaningful; and the approved-change sentence "24.6-24.9 are the canonical verifier-residual backlog homes". The banner also flipped to `Compiled from planning artifacts. Edit freely.`, disabling the byte-for-byte detector that caught the first regression — `epic-25-context.md` and `epic-26-context.md` still carry the original. The story Change Log claims the regeneration was "reverted ... restoring the `NFR8` and `D29` anchors", and this spec's second-pass preamble claims they "are fully restored". Both are false at HEAD. No gate reads this file: a search of `tools/`, `tests/`, `.github/`, `.githooks/` for `epic-*-context.md` returns zero matches. Restore the four bullets and the banner, correct both claims, and extend the Epic 23 preservation loop to require one `NFR8` and one `D29` occurrence per guarded epic context.
- **E-P2. [high] The required-lane evidence was produced from binaries that could not contain the assertions it certifies.** The only TRX on disk records `start=2026-08-13T14:20:08 finish=2026-08-13T14:41:21`, while `TenantIsolationIntegrationTests.cs` and `OpenBaoTopologyIntegrationTests.cs` were written at **15:08:42** — 27 minutes after the lane finished. `verify-integration-fast-coverage.py` exits 0 anyway because it matches `(className, methodName)` pairs, and the method names predate the assertions. Delete the endpoint-rotation assertion and the four `graphCheck.Details.ShouldContain(...)` assertions and the recorded row, the TRX, and the verifier all still pass byte-identically. Compounding: the Debug assembly is 17:45:10 while `AspireIngestionPipelineFixture.cs` is 17:52:20, so the "1/1 in 262.862 s after the seventh-pass allocation-failure cleanup" result cannot contain that cleanup either; and the Release assembly is 17:09:28, so re-running the documented `--no-build` command would still exercise pre-seventh-pass code. Rebuild Release, re-run the recorded lane command and coverage verifier, and re-record the rows with the new TRX's `<Times>` window.
- **E-P3. [high] The cumulative File List is not reconciled at HEAD: 30 of 34 raw paths.** The uncurated index-aware union is **34**, not the recorded 30. Four paths carry neither a File List entry nor an exclusion bullet: `24-7-tenant-configured-vector-dimension-verification.md`, `spec-24-7-tenant-configured-vector-dimension-verification.md`, and `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` (owner `24-7-tenant-configured-vector-dimension-verification`, commits `0f577c48`/`dc5fde62`), plus `spec-pushall-sync-2026-08-14.md` (owner `spec-pushall-sync-2026-08-14`, commit `30104162`). Readiness exits **1** naming exactly those four. This is the fourth recurrence of the same defect (D1, F5-P1, F6-P1); the durable fix is a rule that scopes the union to the story's own commits rather than a hand-counted total any concurrent commit invalidates.
- **E-P4. [high] "No external same-lane delta" is false, and the recorded lane total is stale by 28 cases.** Runner-derived at HEAD, independently executed: the three-class server gate returns **86 total, 0 failed**, against the recorded 58. `52 + 6 + 0 = 58 ≠ 86`. The unrecorded external delta is **+28 test cases** owned by Story 24.7 (`0f577c48`, `dc5fde62`). Two of the three lane classes are story-owned File List paths that now silently absorb that work (`TenantIsolationVerifierTests.cs` +639/-9, `ServerEndpointAuthorizationTests.cs` +23/-0 beyond the reviewed tip), and `TenantIsolationVerifier.cs` gained +128/-3, so the story's declared production delta of "one XML doc comment and one `Details` string" is now 3 of 131 added lines on that path. `story-phase-ledger.md:55-60` requires the external delta be named separately so `create baseline + cumulative story delta + external delta = observed total` closes.
- **E-P5. [medium] This spec's frontmatter declares `status: 'done'` while every other surface reads `review`.** Story `Status: review`, `sprint-status.yaml:426`, `epics.md:4599`, and the closure spec all read `review`. Second-pass P3 repaired exactly this ("status advanced to `done` ahead of the ledger, disabling the check that would have caught D1") and F6-P15 directed the value to `review`; the seventh pass set `done`. `review_loop_iteration: 5` also understates the seven recorded passes.
- **E-P6. [medium] The seventh-pass ledger row omits four of five required `Test count` elements.** It records only a phase delta and bare results (`58/58`, `63/63`, `66/66`, `1/1`), with no cumulative story delta, no per-lane before→after totals, no named unit bound to each lane, and no evidence command. It also omits the owning `TenantIsolationIntegrationTests` lane entirely, although that phase changed three files under `tests/Hexalith.Memories.IntegrationTests/` — the exact reopen trigger the sixth-pass row set. Verbatim the defect F5-P7 raised one pass earlier.
- **E-P7. [medium] Stale `30-path` totals across seven permitted locations.** Story File List preamble, the Verification "Story governance gates" row, the seventh-pass ledger row, `sprint-status.yaml:426`, and three statements in the closure spec all assert 30 and a passing raw gate. The union is 34 and the gate exits 1.
- **E-P8. [medium] The closure spec carries no `Historical Context Classification` or `Slice Proof`, and the gate cannot see it.** `spec-24-6-fifth-pass-action-item-closure.md` contains zero occurrences of either heading, and its `context:` list omits the canonical story that holds the record. `tools/check-story-slice-scope.py:40-43` anchors `STORY_FILE_PATTERN` to `^_bmad-output/implementation-artifacts/(\d+-\d+-[a-z][a-z0-9-]*)\.md$`, so no `spec-*.md` is ever evaluated — the story's recorded slice-scope exit 0 says nothing about either spec file. Its own Boundaries name four prior stories, and its tasks span server source, two test assemblies, two runbooks, the CI manifest, and eight governance artifacts across four bundled groups covering 25 patch IDs. First-pass patch notes already flagged this gate blindness; the closure spec created two passes later reproduced it.
- **E-P9. [medium] The sixth-pass deferred section miscounts itself and its fifth entry escapes the schema.** The header declares "Four low-severity items ... Each carries the structured field block required by the schema above", and five bullets follow. The fifth (FalkorDB `RedisTimeoutException`) uses the legacy `source_spec:/summary:/evidence:` form with no `ID:`, `Status:`, `Source story:`, `Target artifact:`, or `Re-open trigger:` — precisely the defect F5-P4 was raised to cure — and its `source_spec` is a machine-absolute path where every other entry in the file uses repo-relative paths. `CiTestInventoryTests` passes because it matches vocabulary and `Evidence:` presence, so the entry has no id and no reopen trigger.
- **E-P10. [medium] The verifier source guard scans build output and matches only one declaration form.** `OperationalRunbookSetTests.cs:425-428` runs `Directory.GetFiles(<server project>, "*.cs", SearchOption.AllDirectories)` with no `bin`/`obj` exclusion — 430 files versus 422 excluding them — so a unit test reads generated `obj/**/*.g.cs` on every run, its result depends on whether the project has been built, and a stale verifier copy under `obj/` would satisfy it. The filter also requires `\bpartial\s+class\s+TenantIsolationVerifier\b`, so a `partial sealed class` declaration escapes the guard entirely, with `ShouldNotBeEmpty()` as the only completeness check.
- **E-P11. [medium] The negative control never proves the planted marker stayed in tenant A.** `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage` writes `edgeMarkerB` into tenant A through `falkor.SelectGraph(tenantA)` and then traverses tenant A only. Re-traversing tenant B and asserting it is still clean costs one call and distinguishes "the assertion is sensitive" from "the write corrupted both graphs" — and would be the first direct evidence that a `SelectGraph(tenant)` write lands in exactly one tenant, the write-path claim currently open as `24.6-CR-W8`.
- **E-P12. [medium] Only the success path of `GraphIsolation` carries the structural-only label.** The failed and backend-unavailable results return `Details` without the "Structural database-existence evidence only" prefix, so an operator reading a failed or degraded graph result has nothing telling them it is not content-isolation evidence. Apply the same prefix in the failure and `CreateBackendUnavailableResult` paths.
- **E-P13. [medium] The runbooks omit the preconditions that make the cited command runnable.** Both `route-surface.md` and `tenant-onboarding-offboarding.md` instruct operators to run the `-method` proof invocation but never state that it provisions a full real Aspire/FalkorDB topology, takes minutes, requires a container runtime, or that it seeds and leaves fixture data in tenant graphs.
- **E-P14. [medium] AC1's headline duration is internally inconsistent again.** The same evidence batch records the single collision method at **15.572 s** and the 7-method owning class at **243.340 s**. F5-D3 rejected an earlier 16.008 s class figure on exactly this reasoning — each `dotnet exec` brings up its own Aspire topology, so a sub-16 s run is hard to reconcile with a 209–243 s one — and resolved it by re-running to one authoritative result set. The 15.572 s figure is the AC1 row's `Observed result` and is not reconciled with the figure the same document calls authoritative.
- **E-P15. [medium] Story 29.1 is labelled `historical-reference-only` while C2 modifies its test and its fixture.** `story-scope-guard.md:15` defines that label as "dependency, decision, or evidence context" — not a modification licence. C2 adds assertions inside Story 29.1's `InPlaceOpenBaoRestart_RotatesGenerationAndRecoversDependentSidecars` and restructures `AspireIngestionPipelineFixture`'s startup path. The work itself is ratified; the classification row is not.
- **E-P16. [low] Epic AC row 2's line anchors are stale at HEAD.** Recorded as "three `GRAPH.LIST` calls (lines 116, 317, 360)"; at HEAD the call sites are **124, 392, 435**, shifted by Story 24.7's +128 lines. The verdict holds — three `GRAPH.LIST`, zero `GRAPH.QUERY`. This is the L14 pattern: re-derive and record the observed location, keep acting on the substance.
- **E-P17. [low] The closure spec contradicts itself on the raw-union total.** Its Suggested Review Order cites a "28-path raw union" while the same file states 30 in three other places and no artifact anywhere records 28.
- **E-P18. [low] The Story 20.2 classification cites an authority the policy does not name.** `20-2-...md:67` and `:173` contain the `do not reopen` phrase that `story-scope-guard.md` treats as an anti-template marker. The escape hatch is worded as "unless **current epics** explicitly approve a narrower use", but the cited standing instruction lives in the sprint change proposal, not `epics.md`, and `epics.md:4596-4620` makes no Story 20.2 approval. The substance is clean narrow reuse — three `[InlineData]` rows added to an existing theory in current source — so this is a label/authority-citation defect, not anti-template reuse. Either relabel the row `anti-template` (its permitted-use text already satisfies the gate) or add the approval to `epics.md` and cite that line.
- **E-P19. [low] An assertion in the shared helper can never fail.** `AssertTraversalIsFixtureLocal` ends with `ownEdgeMarker.ShouldNotBe(foreignEdgeMarker, "the fixture must seed distinct per-tenant edge markers.")`, comparing two compile-time constants supplied by the caller rather than seeded data. It reads as a fixture invariant but proves nothing about the graph, and it sits inside the helper that both the positive proof and the mutation control depend on.
- **E-P20. [low] The received-call guard drops non-string first arguments.** `TenantIsolationVerifierTests` collects `call.GetArguments().FirstOrDefault()` and filters `.OfType<string>()`, so any typed `IDatabase` read whose first parameter is a `RedisKey`/`RedisValue` is invisible to both the executed-command and `GRAPH.` token assertions, while the adjacent comment claims "every command-like first argument, without filtering by prefix" is asserted. Realistic graph content reads do go through `ExecuteAsync(string, …)` and are caught, so this is a boundary of the guard rather than a live hole — but the comment overstates it.
- **E-P21. [low] Deferred entry `24.6-F5-W2`'s rationale no longer matches its code.** It states the method "disposes `_actorHttpMessageHandler` and nulls `_actorProxyFactory`/`_actorProxyOptions` before assigning their replacements"; the rewritten method constructs both replacements first and only then disposes and swaps. The entry remains `carried-forward` against code that no longer matches it.
- **E-P22. [low] Deferred entry `24.6-CR-W1`'s rationale misstates its own guard.** It claims the duplicated runbook lines "are intentionally pinned identical by `OperationalRunbookSetTests.GraphIsolationEvidenceBoundary_SeparatesStructuralAndContentProof`". That test asserts only that each section *contains* the required tokens; it never compares the sections, and they are in fact not identical in this diff. The reopen trigger "the identity guard fails" therefore cannot fire.
- **E-P23. [low] The approved-change proposal still reads `**Status:** backlog.` for Story 24.6**, immediately above the umbrella blockquote this range inserts. The closure spec's AC4 requires epic/spec/story/sprint status to agree without stale claims.
- **E-P24. [low] Evidence rows are dated before the changes they are offered to cover.** The "Story 20.2 denial-before-dependency" row is `passed 2026-08-12` while its subject `ServerEndpointAuthorizationTests.cs` gains rows in this range and the combined gate cited in the same cell is dated `2026-08-13`. Third recurrence of the shape F5-P19 and F6-P16 each repaired on a different row; the durable fix ties an evidence row's date to its subject file's last change.

### Deferred — eighth pass

- **E-W1.** `GraphIsolation`'s `Details` returns a cluster-wide graph-database count over a per-tenant endpoint. Pre-existing, but rewritten in this range for a story whose thesis is not overstating isolation evidence.
- **E-W2.** The seventh-pass allocation-failure cleanup in `ReconnectPrimaryDaprClients` has no test in any lane and no deferred entry; the failure path is unreachable from tests.
- **E-W3.** The negative control plants a foreign edge marker into a provisioned tenant's real graph with no teardown, and is now pinned as a required `integration-fast` surface — widening the fixture-cleanup risk `24.6-CR-W2` already describes.
- **E-W4.** Test-hardening batch on the new assertions: null guards on `traversal.Nodes`/`GapMarkers`/per-node `Edges`, an already-cancelled `CancellationToken` case for `VerifyAsync`, empty and null `GRAPH.LIST` cases for `ParseGraphList`, `ShouldHaveSingleItem` instead of `Single()` for the graph-check lookup, a linked cancellation token for the seed query so an abandoned command is actually cancelled, and a null guard on the OpenBao recovery tenant's embedding config.

### Dismissed — eighth pass (recorded so they are not re-raised)

- Node-marker mutation-sensitivity control — the Administrator ratified C1's boundary as edge-only and `24.6-F6-W4` carries it.
- `/traverse` denial row for malformed `depth` — open as `24.6-F5-W6`.
- FalkorDB `RedisTimeoutException` not converted to a failed check — substance already deferred; E-P9 repairs the record.
- `ReconnectPrimaryDaprClients` null/dispose window — already cured; the method now constructs replacements before disposing.
- `spec-pushall-sync-2026-08-01.md` as an exclusion for an unchanged path — scope artifact: it changed at `8feb2a2d`, after the reviewed tip. The reviewed range reconciles at 29 + 1 = 30 with zero unaccounted.

### Fail-closed status — eighth pass

`done` is blocked. Fail-closed blockers: **E-P1** (a ledger and spec claim a repair the tree does not
contain, contradicting a resolved Administrator decision), **E-P2** (required-lane evidence produced from
binaries predating the assertions it certifies), **E-P3** (four unaccounted in-scope paths; readiness
exits 1), and **E-P4** (live counts disagree — 86 observed against 58 recorded). E-D1 through E-D5 need
Administrator rulings before the ledger can be finalized.

### Decisions resolved 2026-08-14 (Administrator) — eighth pass

All five eighth-pass decisions were resolved by the Administrator in the review session. Each converts to
a patch item; the resolutions are recorded here so a later reader does not re-open a settled question.

- **E-D1 → add AC5.** Keep the ratified two-checkpoint umbrella and write an explicit **AC5** stating the
  post-OpenBao-restart endpoint-rotation and recovered-actor-call obligation, in **both** the story file
  (after AC4) and `epics.md` at the Story 24.6 block. C2's `complete` state becomes provable against a
  named criterion rather than resting on the implementer's restatement. Scope is unchanged: this records
  the outcome C2 already demonstrates, and does not widen it.
- **E-D2 → enumerate all owners per path.** Rewrite the `references/Hexalith.FrontComposer` and
  `references/Hexalith.Builds` exclusion bullets to list every envelope that moved the gitlink, with the
  commit each contributed: FrontComposer under `spec-submodule-bumps-2026-08-11` (`09b3eb61`,
  `cb863b46`), `spec-pushall-sync-2026-08-13` (`c58d6431`), and `spec-pushall-sync-2026-08-14`
  (`30104162`); Builds under the first and the last. The bullet becomes multi-valued by design.
- **E-D3 → append a `correct-course` row.** `story-phase-ledger.md:8-16` makes `correct-course` canonical
  when that phase actually ran, and Story 31.1 is the worked example of `epics.md` and ratification-proposal
  paths joining the cumulative set with no row accounting for them. Append the row retrospectively, dated
  to the amendments it covers, naming the success-criterion rewrite it performed.
- **E-D4 → keep both, record the tension.** The endpoint-rotation assertion stays as written and stays
  pinned as a required `integration-fast` surface; the "same-port sidecar restart recovery" exclusion stays
  in the Slice Proof. The contradiction is recorded as an accepted, documented boundary rather than
  resolved in either direction. Owner: Murat / Test Architect and Developer. Consequence: a future
  same-port sidecar restart will hard-fail a required CI gate on a case the story declares out of scope.
  Reopen trigger: the restart method fails in CI on an endpoint that did not rotate, or C2's boundary is
  re-scoped.
- **E-D5 → amend AC3 to the ratified split.** Rewrite AC3 to state what Decision D4 ratified: the verifier
  carries the structural-only label plus the manifest-bound method citation, and the paired runbooks carry
  the exact focused integration command. Recorded tension: `epic-ac-verification.md:105-107` forbids
  weakening an acceptance criterion to match code that has not reached the stated end state. This
  amendment is judged to fall outside that prohibition because D4 is a ratified **design** decision — a
  command must not be lengthened into the V1 `Details` contract string — and not an accommodation of
  unfinished work. The amendment must preserve AC3's substantive obligation on both surfaces.
