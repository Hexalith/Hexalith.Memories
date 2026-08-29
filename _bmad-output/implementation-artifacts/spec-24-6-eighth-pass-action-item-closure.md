---
title: 'Story 24.6 Eighth-Pass Action-Item Closure'
type: 'bugfix'
created: '2026-08-14'
status: 'review'
review_loop_iteration: 1
baseline_commit: '301041626f32d4fb9b6a1154e5e09d65a70a2fcc'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-24-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-24-6-graph-content-level-tenant-isolation-evidence.md'
  - '{project-root}/_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 24.6's FalkorDB collision proof still holds, but the eighth-pass review left 29 action items and four fail-closed blockers: lost `NFR8`/`D29` epic-context anchors, integration-fast evidence from binaries older than the assertions, a 30/34 raw File List, and an unrecorded +28 Server.Tests delta owned by Story 24.7.

**Approach:** Apply every Administrator-resolved eighth-pass decision and patch, rebuild and rerun the owning real-backend lanes, and re-reconcile File List under story-own-commit derivation (proposal G) instead of adding exclusion bullets.

## Boundaries & Constraints

**Always:** Preserve authenticated two-tenant collision proof, structural-only `GRAPH.LIST` runtime semantics, Story 20.2 denial-before-dependency evidence, the ratified C1/C2 umbrella, tenant-explicit routing, and unrelated history. Use runner-derived totals. Attach focused negative evidence. Keep E-D4's documented C2 tension (endpoint-rotation assertion stays required; same-port restart remains an exclusion).

**Ask First:** Public API/response-shape growth beyond the GraphIsolation `Details` prefix on failure/unavailable paths; a new runtime graph-content query; production mutation; accepting a failed real-backend lane; changing C1/C2 checkpoint scope; modifying submodule contents/pointers; committing the 2026-08-14 File List SCP under a 24.6 trailer.

**Never:** Implement proposals A–F (`--derive-story-commits`, hook, CONTRIBUTING, readiness-tool changes); treat mocks, method names, `GRAPH.LIST`, synthetic TRX, or a curated path list as completion proof; weaken tenant isolation; reopen Stories 24.3 or 29.1; absorb Stories 24.7–24.9 or the untracked 2026-08-14 SCP; add eleven concurrent-work exclusion bullets.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Observable graph evidence | Real provisioned tenant verification | Success, failure, and backend-unavailable `GraphIsolation.Details` all start with the structural-only prefix; citation stays the manifest-bound method name | Missing graph fails closed |
| Collision sensitivity | Colliding IDs; plant B's edge marker into A | Positive traversal stays local; A fails on the planted marker; B remains clean | Foreign/missing markers fail |
| Source guard | Server `*.cs` tree including `bin/`/`obj/` and `sealed partial class` | Guard reads source only and still finds `TenantIsolationVerifier` | Stale `obj` copies cannot satisfy the guard |
| Story-own File List | Baseline `0ecdffed` plus worktree | Derived set matches File List plus the one `Scope-Override:` exclusion; 24.7/pushall paths are absent | Untracked 2026-08-14 SCP is named as that proposal's worktree limit, not credited here |
| Required integration lane | Rebuild then Release `--no-build` | TRX `<Times>` are after the certifying sources; 19 required surfaces pass | Record an accepted blocker or remain non-done |

</frozen-after-approval>

## Historical Context Classification

This closure spec inherits the canonical Story 24.6 classification table in
[24-6-graph-content-level-tenant-isolation-evidence.md](24-6-graph-content-level-tenant-isolation-evidence.md).
It does not reopen Stories 24.3 or 29.1, does not absorb Stories 24.7-24.9, and
reuses only the Story 20.2 denial-before-dependency assertion pattern.

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Story 24.3 | `historical-reference-only` | Preserve its structural verifier and fail-closed evidence; do not reopen or expand its completed slice. |
| Story 29.1 | `current-narrow-pattern` | Reuse its OpenBao restart regression and fixture ownership as the ratified C2 pattern; do not reopen its completed secret-topology slice. |
| Story 20.2 | `anti-template` | Re-run only its denial-before-dependency assertion pattern, re-verified against current source. |
| Story 24.6 | `current-narrow-pattern` | Apply the eighth-pass patches inside the ratified C1/C2 umbrella; do not widen checkpoint scope. |

## Slice Proof

This closure spec is not a second tracking story. It applies Administrator-resolved
eighth-pass decisions and patches to the existing Story 24.6 C1/C2 umbrella recorded
in [24-6-graph-content-level-tenant-isolation-evidence.md](24-6-graph-content-level-tenant-isolation-evidence.md).
C1 remains the collision-shaped FalkorDB fixture and authenticated graph traversal.
C2 remains the post-OpenBao-restart endpoint-rotation and recovered actor-proxy call.
E-D4 keeps the endpoint-rotation assertion required and the same-port-restart exclusion.
No additional checkpoint is created here.

## Code Map

- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:23` -- `public sealed partial class`. Success, failed-graph, and GraphIsolation-unavailable Details all start with `Structural database-existence evidence only:`. `GRAPH.LIST` at `:124`, `:392`, `:435`; zero `GRAPH.QUERY`.
- `tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs` -- source walk skips `bin`/`obj`; regex `\b(?:sealed\s+partial|partial\s+sealed)\s+class\s+TenantIsolationVerifier\b`; GRAPH runbook sections require Aspire, FalkorDB, duration, container runtime, and leftover fixture data.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` -- `FirstArgumentStrings` stringifies non-string first args; the structural received-call test plants a typed `RedisKey` first argument so `.OfType<string>()` cannot silently drop it.
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` -- planted-edge keeps planting `edgeMarkerB` into A, re-traverses tenant B, and asserts B's collision-edge `PreviousConfidence` is still null (graph-backed when HTTP omits null).
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs` -- C2 required surface; current proof is the rebuilt Release TRX after this source's mtime, not the seventh-pass Debug 262.862s run.
- `docs/operations/{route-surface.md,tenant-onboarding-offboarding.md}` -- GRAPH sections name Aspire/FalkorDB topology, duration, container runtime, and leftover fixture data.
- `_bmad-output/implementation-artifacts/epic-24-context.md` -- Generated-from banner (no Edit freely); `NFR8`/`D29` present; tenant-identifier evidence names malformed/missing/empty/swapped/colliding; Epic 23 checklist has a Status column with dated 2026-08-02 verdicts; no File List / story-own-commit reconcile bullet.
- `_bmad-output/implementation-artifacts/{24-6-graph-content-level-tenant-isolation-evidence.md,spec-24-6-graph-content-level-tenant-isolation-evidence.md,spec-24-6-fifth-pass-action-item-closure.md,deferred-work.md,sprint-status.yaml}` -- File List is 19 owned paths plus one `Scope-Override:` exclusion; story/closure remain in review; fifth-pass readiness/tenant-evidence commands consume `/tmp/story-24-6-own-paths.txt`.
- `_bmad-output/planning-artifacts/{epics.md,sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md}` -- AC5 present after AC4; Story 24.6 Status is review.
- Proposal G command (plain git; do not wait for `--derive-story-commits`) yields 19 owned paths plus one `Scope-Override:` exclusion. The untracked 2026-08-14 SCP is that proposal's worktree limit and is not credited.

## Tasks & Acceptance

**Execution:**
- [x] `TenantIsolationVerifier.cs` plus Server.Tests guards -- E-P10 (exclude `bin`/`obj`; match `sealed`/`partial` either order), E-P12 (structural prefix on failure and unavailable GraphIsolation Details), E-P20 (stringify first call args). No new API fields.
- [x] Runbooks -- E-P13: both GRAPH sections name Aspire/FalkorDB topology, duration, container runtime, and leftover fixture data.
- [x] `TenantIsolationIntegrationTests.cs` -- E-P11 re-traverse tenant B after the plant; E-P19 drop the constant-vs-constant helper assert. Then E-P2: Release rebuild, rerun the recorded lane, prove TRX `<Times>` post-date the certifying sources.
- [x] Story/spec/epic/sprint/deferred/proposal records -- E-D1 AC5 after AC4 in the story and `epics.md`; E-D3 `correct-course` row; E-D5 amend AC3 to the D4 split; E-D2/G File List `matched` against story-own derivation; E-P1 banner + preservation-loop `NFR8`/`D29`; E-P4 record 52+6+28=86; E-P5 status/`review_loop_iteration`; E-P6–P9, P14–P18, P21–P24 ledger, dates, HCC/Slice Proof, deferred schema, and stale 30-path/28-path claims.

### Review Findings

- [x] [Review][Patch] Set closure spec frontmatter status to 'review' [_bmad-output/implementation-artifacts/spec-24-6-eighth-pass-action-item-closure.md:5]
- [x] [Review][Patch] Update stale execution duration and lane status comment [_bmad-output/implementation-artifacts/sprint-status.yaml:426]
- [x] [Review][Patch] Mark 24.6-CR-W6 as resolved in deferred-work.md following E-P12 implementation [_bmad-output/implementation-artifacts/deferred-work.md:3086-3094]
- [x] [Review][Patch] Add Historical Context Classification and Slice Proof sections to canonical implementation spec [_bmad-output/implementation-artifacts/spec-24-6-graph-content-level-tenant-isolation-evidence.md]
- [x] [Review][Patch] Restore chronological row ordering for retrospective correct-course row [_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md:204]
- [x] [Review][Patch] Complete blocked discovery record and cumulative delta in row 17 of story Change Log [_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md:208]
- [x] [Review][Patch] Restore continuous table formatting in Change Log across row 17 [_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md:207-209]
- [x] [Review][Patch] Reconcile row 17 File List cell with canonical matched N/N declaration against baseline 0ecdffed [_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md:208]
- [x] [Review][Patch] Maintain review status during intermediate chunk review (Group A of 19 story files) [_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md:8]
- [x] [Review][Defer] Missing CancellationToken and unhandled WRONGTYPE in syntactic hash prefix scan [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:405] — deferred, pre-existing
- [x] [Review][Defer] Inconsistent entry CancellationToken checks in TenantIsolationVerifier methods [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs] — deferred, pre-existing
- [x] [Review][Defer] Optimize ScanSemanticHashPrefixForTenantEvidenceAsync to single Redis command round-trip per key [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:453] — deferred, pre-existing (Story 24.8)
- [x] [Review][Defer] Broaden IsEmbeddingConfigurationUnavailable exception filters [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:735] — deferred, pre-existing (Story 24.7)
- [x] [Review][Defer] Support dual remediation when classification gap and dimension mismatch co-occur [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:358] — deferred, pre-existing (Story 24.8)
- [x] [Review][Defer] Add replica endpoint filtering in GetConnectedServers for clustered Redis [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:769] — deferred, pre-existing

**Acceptance Criteria:**
- Given the final verifier, tests, and runbooks, when source, received-call, and HTTP guards run, then only `GRAPH.LIST` is used, every GraphIsolation Details path is labeled structural-only, the source guard ignores build output, and operator docs state the real-topology preconditions.
- Given a rebuilt real-backend topology, when the owning tenant class and required integration-fast lane run, then local markers, planted-edge sensitivity with tenant B still clean, endpoint rotation, and recovered actor access pass, and the TRX window is after the certifying sources.
- Given baseline `0ecdffed` and the story-own-commit derivation, when File List is recorded, then it matches that derived set (owned paths plus the one `Scope-Override:` exclusion), eleven concurrent-work bullets are gone, and 24.7/pushall paths are auto-attributed.
- Given all patches and evidence, when closure is recorded, then AC3/AC5, C1/C2, E-D4 tension, ledger counts, GRAPH.LIST lines 124/392/435, deferred schema, epic-context anchors, and story/spec/epic/sprint status agree without stale 30-path or `done`-ahead-of-ledger claims.

## Spec Change Log

- 2026-08-14: Applied all five resolved eighth-pass decisions and E-P1 through E-P24.
  Re-reconciled File List under story-own derivation, restored epic-24 anchors and
  banner, recorded 52+6+28=86, rebuilt Release, and recertified the 19 required
  surfaces on a TRX that post-dates the certifying sources.
- 2026-08-14: Applied the follow-up patch set: tenant-B PreviousConfidence obligation,
  RedisKey first-arg stringify proof, gitlink restore, SCP unstage, fifth-pass
  own-paths consumption, epic-24 missing/Status restore, C2 TRX recency vs OpenBao
  source mtime, File List N = 19 owned + one Scope-Override exclusion, and the AC4
  accepted blocker for the prior Ollama `/alive` timeout. This pass the full
  integration-fast lane is green; story/closure remain in review.
- 2026-08-16: Applied all five ninth-pass review patches: review-status alignment,
  current lane duration/status, deferred W6 resolution, canonical historical/slice
  context, and chronological phase-ledger ordering. Focused governance checks pass;
  the final broad build and uncurated readiness gates remain blocked by concurrently
  modified Story 24.7/24.8 artifacts and dirty Builds/FrontComposer pointers.

## Design Notes

Proposal G is a recording change only. A–F own the `--derive-story-commits` flag; this spec runs the plain-git derivation from the 2026-08-14 SCP and must not edit `tools/check-story-review-readiness.py`. The untracked SCP path is that proposal's stated worktree limit until it lands under its own trailer.

AC5 (E-D1): after AC4, require that an in-place OpenBao restart change the primary Dapr sidecar HTTP endpoint and that a replacement actor-proxy factory complete a real post-rotation actor call in the required integration-fast surface. Scope is unchanged.

AC3 (E-D5): verifier Details carry the structural-only label plus the manifest-bound method citation; the paired runbooks carry the exact focused integration command. Do not lengthen V1 Details into a command string.

E-D4: keep the endpoint-rotation assertion and the same-port-restart exclusion; record owner Murat, consequence (same-port restart hard-fails a required gate), and reopen trigger in Slice Proof.

AC4 accepted blocker (2026-08-14): the prior eighth-pass Release lane failed on the out-of-scope Ollama e2e `/alive` timeout. Owner Murat / Test Architect and Developer. Consequence: that lane failure is not Story 24.6 proof and must not close the story. Proof boundary: the 19 required surfaces still passed on that TRX and on this pass. This pass the full lane is green (321/8/0). Reopen trigger: the same timeout appears on a required surface, or that e2e case is brought into this story's required set. Story and this closure remain in review.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` -- expected: 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Deployment.OperationalRunbookSetTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` -- expected: pass; record observed total (86 plus this phase's delta) with the 52+6+28 equation.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` -- expected: deferred-schema inventory passes.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --disable-build-servers -m:1 /nr:false` -- expected: clean Release build before `--no-build`; record `NU1903` if present and use `-p:NuGetAudit=false` only as the isolated compile fallback.
- `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Tenants.TenantIsolationIntegrationTests` -- expected: owning class passes on one real topology; this duration is AC1-authoritative.
- `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050 MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060 bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" --configuration Release --no-build --results-directory TestResults/integration-fast` -- expected: required lane green; TRX `start` after `TenantIsolationIntegrationTests.cs` and `OpenBaoTopologyIntegrationTests.cs` mtimes.
- `python3 tools/verify-integration-fast-coverage.py --results-directory TestResults/integration-fast` -- expected: 19/19 required surfaces.
- Story-own derivation into `/tmp/story-24-6-own-paths.txt` using the 2026-08-14 SCP Group-3 command with baseline `0ecdffed0b131d05816306da1c7061eb88bda5bf` and key `24-6-graph-content-level-tenant-isolation-evidence`, then `python3 tools/check-story-review-readiness.py --story-key 24-6-graph-content-level-tenant-isolation-evidence --changed-files-file /tmp/story-24-6-own-paths.txt` -- expected: exit 0 against the derived set (not the raw 34-path union).
- `python3 tools/check-story-slice-scope.py --story-key 24-6-graph-content-level-tenant-isolation-evidence` -- expected: exit 0.
- `python3 tools/check-tenant-isolation-evidence.py --story-key 24-6-graph-content-level-tenant-isolation-evidence --changed-files-file /tmp/story-24-6-own-paths.txt` -- expected: exit 0.
- Epic 23 + NFR8/D29 preservation loop over `epic-24-context.md` and `epic-25-context.md` -- expected: checklist rows plus `rg -c NFR8` and `rg -c D29` ≥ 1 in epic-24.
- `python3 -m unittest discover -s tests/tooling/<suite> -p '*_test.py'` for `line_endings`, `integration_fast_coverage`, `story_review_readiness`, `tenant_isolation_evidence`, `story_slice_scope` -- expected: all OK.
- `python3 -c 'import pathlib, yaml; yaml.safe_load(pathlib.Path("_bmad-output/implementation-artifacts/sprint-status.yaml").read_text(encoding="utf-8")); print("sprint-status YAML: OK")'` -- expected: parses.
- `git diff --check` -- expected: no whitespace errors.

**Results (2026-08-14):**

- Server.Tests Debug build: 0 warnings, 0 errors. Three-class gate **86/86** in 6.120s (52+6+28).
- `CiTestInventoryTests` **66/66** in 0.252s after the deferred-schema repair.
- IntegrationTests Debug build: 0 errors, two `NU1903` advisories for `SSH.NET` 2025.1.0.
- Owning class **7/7** in 254.358s against one real Aspire/FalkorDB topology after the tenant-B PreviousConfidence obligation (AC1-authoritative).
- IntegrationTests Release build: 0 errors, two `NU1903` advisories. Rebuilt `--no-build` lane: 321 passed, 8 skipped, 0 failed in 19m29s. Required-surface verification **19/19**. TRX `start=2026-08-15T00:07:50+02:00` is after `TenantIsolationIntegrationTests.cs` (00:02:38+02:00), `OpenBaoTopologyIntegrationTests.cs` (2026-08-13 15:08:42 +0200), and the Release assembly (00:07:46+02:00). The prior `/alive` timeout remains the AC4 accepted blocker and is not Story 24.6 proof.
- Story-own readiness, slice-scope, and tenant-isolation-evidence: exit 0 against the derived 19 owned paths plus one `Scope-Override:` exclusion. Tooling suites 4/6/45/41/20 all OK. Epic-24 NFR8=2 and D29=3. sprint-status YAML parses. `git diff --check` clean. Gitlinks `references/Hexalith.Builds`, `references/Hexalith.EventStore`, and `references/Hexalith.FrontComposer` match HEAD. The 2026-08-14 SCP remains untracked and uncredited.

**Ninth-pass patch verification (2026-08-16):**

- Tooling suites passed 4/4, 6/6, 45/45, 41/41, and 20/20; sprint-status YAML
  parsed; `git diff --check` reported no whitespace errors; CLI inventory passed 66/66.
- The uncurated story-own derivation produced 25 paths and readiness exited 1 on five
  concurrent worktree paths: the Story 24.7 story/spec, the untracked Story 24.8 spec,
  and dirty `references/Hexalith.Builds` / `references/Hexalith.FrontComposer` pointers.
  Removing only those externally owned worktree paths yielded the expected 20 entries
  (19 owned File List paths plus one exclusion), and focused readiness plus tenant-
  isolation evidence passed; that focused result is diagnostic evidence, not a green
  uncurated completion gate.
- The required Server.Tests build failed before test execution under the dirty Builds
  dependency state: three obsolete `RedisConnectionException` constructor errors and
  `SER301` on `ReleaseDedupKeyIfOwnedActivity`. No warning or analyzer gate was
  suppressed, and no externally owned source or gitlink was modified.

## Suggested Review Order

**Structural-only GraphIsolation contract**

- Prefix failure Details so operators never read missing-graph as content proof.
  [`TenantIsolationVerifier.cs:407`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L407)

- Prefix backend-unavailable GraphIsolation the same way, leaving other checks unchanged.
  [`TenantIsolationVerifier.cs:690`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L690)

- Keep the success citation on the manifest-bound proof method, not a command string.
  [`TenantIsolationVerifier.cs:414`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L414)

**Collision write isolation**

- Re-traverse tenant B and prove the plant did not set previousConfidence.
  [`TenantIsolationIntegrationTests.cs:145`](../../tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs#L145)

**Guards**

- Skip bin/obj and match sealed/partial in either order when scanning verifier source.
  [`OperationalRunbookSetTests.cs:430`](../../tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs#L430)

- Stringify first call args, including typed RedisKey, so GRAPH.LIST stays the only command.
  [`TenantIsolationVerifierTests.cs:917`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L917)

**Operator docs**

- Name Aspire/FalkorDB topology, duration, container runtime, and leftover fixture data.
  [`route-surface.md:94`](../../docs/operations/route-surface.md#L94)

- Keep the paired offboarding section identical on those preconditions.
  [`tenant-onboarding-offboarding.md:173`](../../docs/operations/tenant-onboarding-offboarding.md#L173)

**Governance**

- Add AC5 for endpoint rotation and recovered actor access after OpenBao restart.
  [`24-6-graph-content-level-tenant-isolation-evidence.md:28`](24-6-graph-content-level-tenant-isolation-evidence.md#L28)

- Mirror AC5 into the epic block so C2 has a named criterion.
  [`epics.md:4614`](../planning-artifacts/epics.md#L4614)

- Reconcile File List as 19 owned paths plus one Scope-Override exclusion.
  [`24-6-graph-content-level-tenant-isolation-evidence.md:207`](24-6-graph-content-level-tenant-isolation-evidence.md#L207)
