---
title: 'Story 24.6 Fifth-Pass Action-Item Closure'
type: 'bugfix'
created: '2026-08-13'
status: 'review'
review_loop_iteration: 0
baseline_commit: '1d2862f42c08e2a04984379f0b603d5366ffe804'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-24-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-24-6-graph-content-level-tenant-isolation-evidence.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 24.6's real FalkorDB collision proof is substantively sound, but its fifth review pass found 25 unresolved code, test, documentation, and evidence-accounting defects. The inconsistent status and incomplete raw-range reconciliation prevent an evidence-backed close.

**Approach:** Apply every resolved fifth-pass decision and patch, strengthen the observable and mutation-sensitive tests, rerun the owning real-backend lanes, and reconcile all governed records against the final raw baseline-to-worktree path set.

## Boundaries & Constraints

**Always:** Preserve authenticated two-tenant collision proof, structural-only `GRAPH.LIST` runtime semantics, Story 20.2 denial-before-dependency evidence, tenant-explicit routing, the ratified C1/C2 umbrella, and all unrelated user/history changes. Use the final runner-derived results as authoritative and attach focused negative evidence.

**Ask First:** Public API/response-shape growth, a new runtime graph-content query, production mutation, accepting a failed real-backend lane, changing the ratified checkpoint scope, or modifying submodule contents/pointers.

**Never:** Treat mocks, method names, structural database existence, synthetic TRX fixtures, or a curated changed-file list as completion proof; weaken tenant isolation; reopen Stories 24.3 or 29.1; absorb Stories 24.7-24.9; or modify externally owned submodule/customization work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Observable graph evidence | Real provisioned tenant verification | HTTP result labels `GraphIsolation` structural-only, cites the manifest-bound proof method, and performs no graph-content scan | Missing graph fails closed |
| Collision sensitivity | Colliding graph IDs with local/foreign markers | Positive traversal remains local; edge mutation control fails for the expected assertion and its node-coverage boundary is disclosed | Foreign/missing markers fail |
| Sidecar rotation | OpenBao restart changes the Dapr endpoint | Test proves endpoint rotation before the recovered actor call | Unchanged endpoint fails the regression |
| Required integration lane | Final code and required-surface manifest | Owning methods execute on real backends and the full required fast lane is green | Record an accepted blocker or remain non-done |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` -- restore the broad verifier contract; keep the structural/content hedge local to `CheckGraphIsolationAsync`.
- `tests/Hexalith.Memories.Server.Tests/{Tenants/TenantIsolationVerifierTests.cs,Deployment/OperationalRunbookSetTests.cs}` -- bind citations to the required-surface manifest and reject every verifier `GRAPH.*` command except `GRAPH.LIST`, across source and received calls.
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` -- assert HTTP-visible AC3 details and retain collision/negative-control evidence.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/{OpenBaoTopologyIntegrationTests.cs,AspireIngestionPipelineFixture.cs}` -- assert endpoint rotation; fixture is reuse/read-only unless the test exposes a defect.
- `tools/integration-fast-required-surfaces.txt` -- pin the core verifier method as well as positive and mutation-control graph methods.
- `docs/operations/{tenant-onboarding-offboarding.md,route-surface.md}` -- preserve exact command boundary; normalize the paired section ending.
- `_bmad-output/{planning-artifacts/{epics.md,sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md},implementation-artifacts/{24-6-graph-content-level-tenant-isolation-evidence.md,spec-24-6-graph-content-level-tenant-isolation-evidence.md,epic-24-context.md,deferred-work.md,sprint-status.yaml}}` -- repair umbrella provenance, Epic 23 checklist continuity, deferred schema, test ledger, raw File List/exclusions, review anchors, and synchronized status.

## Tasks & Acceptance

**Execution:**
- [x] Server verifier/tests -- implement D2, P8, P10, and P12 without API growth; derive proof identity from the required-surface manifest and cover the HTTP-observable result.
- [x] Integration tests/manifest -- implement P13 and P16; disclose P11's edge-only mutation boundary; run the restart method, owning tenant class, and full required integration-fast lane.
- [x] Planning/story records -- implement D1, D4, P1-P7, P14-P15, and P17-P21; restore the Epic 23 review checklist and convert all eleven new deferred bullets to the documented structured schema.
- [x] Evidence/status -- apply D3 using one authoritative final owning-class run, remeasure affected server/tooling lanes, reconcile the uncurated raw changed set, then synchronize story/spec/epic/sprint status.

**Acceptance Criteria:**
- Given the final verifier and tests, when source, mock-call, HTTP, and required-surface guards run, then only `GRAPH.LIST` is allowed and every operator citation resolves to a passing real method.
- Given the final real-backend topology, when the restart and tenant-isolation lanes run, then endpoint rotation, recovered actor access, local node/edge markers, collision IDs, and assertion sensitivity are demonstrated with one authoritative result set.
- Given the raw `0ecdffed..worktree` path set, when readiness, slice-scope, tenant-evidence, deferred-schema, line-ending, and integration-fast gates run, then every path is declared or machine-readably excluded and every gate passes.
- Given all patches and evidence pass, when closure is recorded, then C1/C2, review iteration, File List, ledger counts, deferred evidence, epic/spec/story/sprint status, and D1/D4 rationale agree without stale claims.

## Spec Change Log

- 2026-08-13: Applied all four resolved decisions and F5-P1 through F5-P21. Reconciled the
  24-path raw baseline-to-worktree set, restored governance continuity, recorded the final
  real-backend, server, tooling, and integration-fast evidence, and submitted the closure for review.
- 2026-08-13: Seventh-pass review applied F6-P1 through F6-P26, repaired the newly exposed fixture
  allocation-failure cleanup and deferred-ledger schema issue, and reconciled the concurrent final
  raw union at 30 paths while preserving the two new synchronization-owned paths.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` -- expected: clean Debug build before direct Server.Tests execution.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Deployment.OperationalRunbookSetTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` -- expected: verifier, runbook, and authorization guards pass.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` -- expected: deferred-work and CI inventory guards pass.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` -- expected: the project compiles; record package-audit advisories and use `-p:NuGetAudit=false` for an isolated warning-free compile signal.
- `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Fixtures.OpenBaoTopologyIntegrationTests.InPlaceOpenBaoRestart_RotatesGenerationAndRecoversDependentSidecars` -- expected: endpoint rotation and recovered actor access pass.
- `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests` -- expected: the complete owning class passes against one real topology.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --disable-build-servers -m:1 /nr:false` -- expected: clean Release build before the `--no-build` lane.
- `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" --configuration Release --no-build --results-directory TestResults/integration-fast` -- expected: the required real-backend lane passes.
- `python3 tools/verify-integration-fast-coverage.py --results-directory TestResults/integration-fast` -- expected: every exact required class/method surface is present and passed in the lane TRX.
- `{ git diff --name-only 0ecdffed..HEAD; git diff --name-only HEAD; git ls-files --others --exclude-standard; } | sort -u > /tmp/story-24-6-raw-paths.txt` -- expected: the uncurated, index-aware baseline-to-worktree union contains 30 unique paths.
- `python3 tools/check-story-review-readiness.py --story-key 24-6-graph-content-level-tenant-isolation-evidence --changed-files-file /tmp/story-24-6-raw-paths.txt` -- expected: raw C1 and C6 readiness evidence passes while the story is in review.
- `python3 tools/check-story-slice-scope.py --story-key 24-6-graph-content-level-tenant-isolation-evidence` -- expected: the substantive story-slice gate passes.
- `python3 tools/check-tenant-isolation-evidence.py --story-key 24-6-graph-content-level-tenant-isolation-evidence --changed-files-file /tmp/story-24-6-raw-paths.txt` -- expected: the substantive tenant-isolation evidence gate passes.
- `python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py'` -- expected: 4 line-ending tests pass.
- `python3 -m unittest discover -s tests/tooling/integration_fast_coverage -p '*_test.py'` -- expected: 6 integration-fast coverage tests pass.
- `python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py'` -- expected: 45 review-readiness tests pass.
- `python3 -m unittest discover -s tests/tooling/tenant_isolation_evidence -p '*_test.py'` -- expected: 41 tenant-evidence tests pass.
- `python3 -m unittest discover -s tests/tooling/story_slice_scope -p '*_test.py'` -- expected: 20 story-slice tests pass.
- `python3 -c 'import pathlib, yaml; yaml.safe_load(pathlib.Path("_bmad-output/implementation-artifacts/sprint-status.yaml").read_text(encoding="utf-8")); print("sprint-status YAML: OK")'` -- expected: sprint status parses as YAML.
- `git diff --check` -- expected: no whitespace errors.

**Results (2026-08-13):**

- Server.Tests Debug build passed with 0 warnings/errors; verifier, runbook, and authorization
  classes passed 58/58 in 8.333 seconds. `CiTestInventoryTests` passed 66/66 in 0.569 seconds after
  the carried-forward entry's required `Rationale:` field was restored.
- The unpinned IntegrationTests Debug build passed with 0 errors and two `NU1903` advisories for
  `SSH.NET` 2025.1.0; the audit-disabled fallback passed with 0 warnings/errors in 49.51 seconds.
  The restart regression passed 1/1 in 262.862 seconds and proved the endpoint changed before
  recovered actor access after the seventh-pass allocation-failure cleanup.
- The authoritative `TenantIsolationIntegrationTests` class passed 7/7 in 243.340 seconds against
  one fresh real Aspire/FalkorDB topology, superseding every earlier method/class duration.
- The Release integration-fast lane passed 321 with 8 intentional skips and 0 failures in 21m09s;
  TRX verification satisfied all 19 required surfaces, including the core verifier, positive collision,
  planted-edge-marker assertion-sensitivity, and post-OpenBao-restart actor-recovery methods.
- Raw readiness passed all 30 uncurated paths (18 story-owned plus 12 named exclusions). The five tooling unit suites passed exactly:
  line endings 4, integration-fast coverage 6, readiness 45, tenant evidence 41, and slice scope 20.
  Separately, the substantive raw-readiness, story-slice, and tenant-isolation-evidence gates passed;
  sprint-status YAML parsing and `git diff --check` also passed.

## Suggested Review Order

**Isolation contract**

- Start with the structural-only production boundary and its manifest-bound content-proof citation.
  [`TenantIsolationVerifier.cs:304`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L304)

- Confirm clients receive the qualification and independent proof identity over HTTP.
  [`TenantIsolationIntegrationTests.cs:65`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L65)

**Guardrails and recovery**

- Review declaration-wide, call-site-sensitive rejection of non-structural graph commands.
  [`OperationalRunbookSetTests.cs:424`](../../tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs#L424)

- Verify runtime call inspection rejects every non-`GRAPH.LIST` command and scans nested arguments.
  [`TenantIsolationVerifierTests.cs:384`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L384)

- Confirm actor recovery is accepted only after the Dapr endpoint rotates.
  [`OpenBaoTopologyIntegrationTests.cs:148`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs#L148)

**Evidence and governance**

- Check the exact restart, core, collision, and assertion-sensitivity CI pins.
  [`integration-fast-required-surfaces.txt:9`](../../tools/integration-fast-required-surfaces.txt#L9)

- Reconcile the 28-path raw union, final evidence, and review-state ledger.
  [`24-6-graph-content-level-tenant-isolation-evidence.md:149`](24-6-graph-content-level-tenant-isolation-evidence.md#L149)

- Inspect the structured residual-risk register and its first-pass review section.
  [`deferred-work.md:3034`](deferred-work.md#L3034)

- Verify the approved two-checkpoint Story 24.6 exception remains narrowly scoped.
  [`epics.md:4602`](../planning-artifacts/epics.md#L4602)

- Finish with the sprint handoff held at review for independent acceptance.
  [`sprint-status.yaml:426`](sprint-status.yaml#L426)
