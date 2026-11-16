# Epic 30 Context: CI/CD Pipeline Ownership and Alignment

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Release maintainers can publish and recover one complete, identity-bound Memories release set (ten NuGet packages, five container images) without silent partial success. Reusable Hexalith.Builds workflows own standard CI and pinned release preparation. Memories keeps its module-specific verification, caller-owned NuGet OIDC Trusted Publishing, publication hooks, deployment assets and recovery. Alignment follows EventStore's shared core with companion lanes, plus Tenants-style coverage and consumer validation where they fit. It must never weaken tenant-negative, web E2E, integration, deployment, benchmark, package-inventory or partial-recovery gates. This is Operational Readiness work and is excluded from MVP readiness. The approved order is shared CI, local qualification of the pinned Builds contract, guarded dispatch, five-image publication, partial recovery, then cutover and rollback. A final story closes the blocking retrospective actions.

## Stories

- Story 30.1: Shared CI Core and Module-Specific Verification Lanes
- Story 30.2: Pinned Builds Publication and Recovery Qualification
- Story 30.3: Guarded Release Dispatch and Shared Caller Adoption
- Story 30.4: Five-Image Publication Contract
- Story 30.5: Partial-Release Recovery
- Story 30.6: Release Cutover Parity and Rollback
- Story 30.7: Epic 30 Retro Blockers
- Story 30.8: Restore the orphaned Integration tests and guard CI lane selection

## Requirements & Constraints

- **Immutable Builds identity.** `domain-ci.yml` and `Github/prepare-domain-release` are pinned to one approved lowercase 40-character SHA, the same one passed as `builds-execution-sha`. Fixtures refuse any mismatch.
- **Privileged workflows run only reviewed code.** Every external action in `release.yml` and `recover-partial-release.yml` is pinned to a reviewed full SHA, and `@main` is refused there. Unprivileged `ci.yml` keeps its own policy. Every release checkout sets `persist-credentials: false`, and semantic-release gets `GITHUB_TOKEN` so it can push the tag.
- **CI never weakens a gate.**
  - Shared CI runs the Release build with warnings as errors, plus one explicitly selected MTP shard: `Hexalith.Memories.Contracts.Tests`. Test selection is never inferred.
  - The local Coverlet union over the eight `tools/test-projects.unit-contract.txt` projects stays authoritative for coverage.
  - The Debug/source build runs as `source-debug`. The required `build` check always runs and passes only when both `shared-ci` and `source-debug` succeeded. Failed, cancelled and skipped predecessors all fail it.
  - These stay named local lanes: story scope, tenant-negative evidence, tooling, package topology, web E2E, fast integration and production-deployment verification. Nightly slow integration and benchmarks also stay.
  - TRX and coverage stay downloadable. Duplicate lanes are removed only after equivalence is proven.
  - Commitlint validates explicit PR titles, including edited ones, and every commit that reaches `main`.
- **Qualification fails closed.** Evidence binds to the exact Builds revision and needs owner approval. Upstream fixture success alone qualifies nothing. A mismatched identity, missing member, unsupported phase, ambiguous destination or incomplete evidence blocks the dependent stories.
- **Guarded release.**
  - Only an intentional dispatch from `main` publishes. An ordinary push never does.
  - An unprotected preflight proves the dispatch SHA is the current `main` tip and that exact-source push CI passed. That CI is the test authority, so application tests are not rerun.
  - The release job runs in the protected `production` environment with `cancel-in-progress: false` and `expected-package-count: 10`.
  - A failed source, count, environment, destination-absence or Builds-identity proof stops the run before publication.
  - OIDC stays in place; it is never replaced by a supplied API key.
- **Inventory.** `tools/release-packages.json` is authoritative: ten packages, including `Hexalith.Memories.EventStoreExtensions`. The `packageId`-to-`id` migration is atomic and keeps the same ten IDs. Recovery still reads historical tagged `packageId` manifests without rewriting them. The older nine-package/four-image acceptance is superseded, and no member may be dropped to match it.
- **Five images under one source/workflow/set identity:** `memories`, `memories-mcp`, `memories-access-telemetry`, `memories-access-telemetry-clock` and `memories-domain-service`. Each image gets platform verification and a health check suited to its workload. The registry authorization mode is recorded. A push blocker is recorded as a named blocker with an owner, consequence and reopen trigger, never worked around silently.
- **Recovery.** Recovery consumes immutable release evidence and proves source, version and inventory. It skips members that are already published and publishes only the missing ones. It never overwrites, retags or treats an ambiguous destination response as success. Current-set recovery needs retained, hash-bound prepared bytes and production/exact-source authorization. It must re-render deployment bytes the way the release runner checks them out, with git attributes such as CRLF applied.
- **Cutover and rollback.** Retiring the old path requires parity on packages, images, GitHub Release assets, OIDC/registry authorization, failure handling and recovery. Rollback is a normal PR that restores the latest fully released guarded surface. It never changes a published version, a mutable tag or Latest.
- **Evidence contract.** Each checkpoint has its own owner, exact command or artifact, review state and completion state. A planned row, a dated exception or a risk acceptance is never passed evidence and never closes a gate.

## Technical Decisions

- Shared workflow logic lives in Hexalith.Builds and is never copied into Memories. If a shared input would weaken evidence, either add the capability to Builds or keep the lane local with a documented exception.
- The ordinary caller-owned production job keeps NuGet OIDC Trusted Publishing and the existing publication hooks.
- Production-deployment asset generation stays in the caller. The `server`, `mcp`, `access-telemetry`, `access-telemetry-clock` and `domain-service` `.tar.gz` archives must all come from one reviewed source SHA. The first four stay compatible inputs to the Story 27.3 deployment-verification lane.
- Container base images are pinned to a reviewed digest. No owned image may inherit a floating base tag, and no project overrides `ContainerBaseImage`.
- The skopeo authfile path has published all five images. Server-side registry immutability and race policy are still unproved and owned elsewhere.
- Recovery evidence is approved from Builds `81c88b34` and `ae77d569`. That approval changes no execution pin or allowlist.

## Cross-Story Dependencies

- Stories run strictly in order from 30.1 to 30.6. Shared CI work earns no publication-qualification credit. Story 30.4 also needs an owner-approved, frozen multi-container identity with repeated per-container verification. Story 30.5 needs qualified immutable recovery evidence. Story 30.6 needs 30.3, 30.4 and 30.5 done.
- Story 30.7 carries retrospective actions A1 and A2 (items 91 and 92). They close only on green push CI for its final commit.
- Story 30.8 follows 30.7 and carries A3 (item 93): restore the eight hermetic extraction cases to unit-contract, route three latency cases to IntegrationSlow, guard automatic lane selection, and register the 60 untagged methods in DW-731. Its spec is authoritative; item 93 and the story close only after green exact-final-commit push CI with all 11 extraction cases passed in Server.Tests TRX. Completed 2026-11-16 on `898f451111740ccf489d56f7c20cd47edc2fcdf6`: push CI `43886197182`, CodeQL and Commitlint green; Server.Tests TRX confirms 11/11 extraction cases passed, none skipped. Item 93 is done; DW-731 stays open.
- In the epics file, the backlog labels and planned checkpoint tables for 30.3 to 30.6 are historical. Each story spec's checkpoints and File Scope are authoritative. Live runs before 30.5 do not prove the final recovery code; that proof is still owed as the next owner-dispatched additive snapshot.
- Former story keys (for example, the former 30.2 is now 30.1) are historical aliases. Map them through the alias table and never rewrite them globally.
- Story 27.3 independently owns production rendering, verification, evidence tooling and the `production-deployment-verification` lane. Epic 30 never gates that lane or supplies its evidence. Epic 31 owns the deployed OpenBao platform and the runtime Dapr secret-store migration.
