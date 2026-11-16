# Epic 30 Context: CI/CD Pipeline Ownership and Alignment

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Release maintainers can publish and recover one complete, identity-bound Memories release set (ten NuGet packages, five container images) without silent partial success. Reusable Hexalith.Builds workflows own standard CI and pinned release preparation. Memories keeps its module-specific verification, caller-owned NuGet OIDC Trusted Publishing, publication hooks, deployment assets, and recovery. This is Operational Readiness work, not MVP product readiness, and it is accepted only on concrete evidence: check names, release runs, inventory proof, runbook updates, or explicit accepted risks. The approved order is shared CI, qualification of the pinned Builds contract, guarded dispatch, five-image publication, partial recovery, then cutover and rollback.

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

- **Immutable Builds identity.** Every Hexalith.Builds caller (`domain-ci.yml`, `Github/prepare-domain-release`) pins one approved lowercase full 40-character SHA, identical to `builds-execution-sha`. Never use `@main` or any other moving ref. Fixtures prove that no mutable ref is used and refuse a mismatch.
- **CI never weakens a gate.** Shared CI runs Release build, warnings-as-errors, and an explicit `Hexalith.Memories.Contracts.Tests` MTP shard only, never inferred. The eight-project local Coverlet union stays authoritative. Story scope, tenant-negative evidence, tooling, package topology, web E2E, fast integration, production-deployment verification, nightly integration, and benchmarks remain named local lanes. The stable required `build` check always runs and passes only if `shared-ci` and `source-debug` both succeeded. TRX and coverage stay downloadable, and duplicates are removed only after equivalence is proven. Commitlint checks explicit PR titles, including edits, and commits reaching `main`.
- **Qualification fails closed.** Evidence binds to the exact Builds revision and needs owner approval. Upstream fixture success alone grants no credit. Identity mismatch, a missing member, an unsupported phase, an ambiguous destination, or incomplete evidence blocks dependent stories.
- **Guarded release.** Only an operator dispatch publishes, never an ordinary push. An unprotected preflight proves the SHA is the current `main` tip with green exact-source push CI. The release job runs in the protected `production` environment with `cancel-in-progress: false`. Application tests are not rerun. `expected-package-count` is `10`. A failed source, count, environment, destination-absence, or Builds-identity proof stops before publication. OIDC stays; no supplied API key.
- **Inventory.** `tools/release-packages.json` is authoritative: ten packages, including `Hexalith.Memories.EventStoreExtensions`. The `packageId` to `id` migration is atomic and keeps the same ten IDs. Recovery still reads historical tagged `packageId` manifests unchanged. No member is dropped.
- **Five images, one source/workflow/set identity.** The repositories are `memories`, `memories-mcp`, `memories-access-telemetry`, `memories-access-telemetry-clock`, and `memories-domain-service`. Each image has per-platform and workload-appropriate health verification. The registry authorization mode is recorded, and any push blocker is named with an owner, consequence, and reopen trigger, never worked around silently.
- **Recovery.** `recover-partial-release.yml` consumes immutable evidence and proves source, version, and inventory. It skips published members and publishes only the missing ones. It never overwrites, retags, or treats an ambiguous destination as success. Current-set recovery requires retained, hash-bound prepared bytes and production/exact-source authorization.
- **Cutover and rollback.** Retirement needs package, image, release-asset, authorization, failure, and recovery parity, and it applies only to the uninvoked `tools/test-release.ps1`. Rollback is a normal PR that restores the latest fully released guarded surface, currently v4.0.0. It refuses v3.0.19 through v3.0.25 and the push-triggered caller from before Story 30.3. No published version, mutable tag, or Latest may change.
- **Evidence.** Each checkpoint has its own owner, exact command or artifact, review state, and completion state. Planned rows and accepted risks are not passed evidence.

## Technical Decisions

- Shared logic lives in Hexalith.Builds and is never copied into Memories. Where a shared input would weaken a gate, extend Builds or document a local exception. The model is EventStore's shared core with companion lanes and Tenants-style coverage.
- The caller-owned production job keeps OIDC and the publication hooks.
- Deployment asset generation stays in the caller. All five `.tar.gz` archives come from one reviewed source SHA. The server, MCP, access-telemetry and access-telemetry-clock archives remain inputs to the Story 27.3 `production-deployment-verification` lane.
- The skopeo authfile path published all five images in the v3.0.24 through v3.0.26 runs. Registry immutability and race policy are still unproved and owned separately.
- Builds `81c88b34` and `ae77d569` are approved for recovery evidence. That approval changes no execution pin or allowlist.

## Cross-Story Dependencies

- The order is 30.1 through 30.6. Story 30.4 also needs an approved frozen multi-container identity, and 30.5 needs qualified recovery evidence. Story 30.6 needs 30.3 through 30.5 done.
- Story 30.7 follows the Epic 30 retrospective and carries its blocking actions A1 and A2 (items 91 and 92). Those items close only after green push CI on its final commit.
- Story 30.8 follows 30.7 and carries A3 (item 93): restore the eight hermetic extraction cases to unit-contract, route three latency cases to IntegrationSlow, guard automatic lane selection, and register the 60 untagged methods in DW-731. Its spec is authoritative; item 93 and the story close only after green exact-final-commit push CI with all 11 extraction cases passed in Server.Tests TRX. Completed 2026-11-16 on `898f451111740ccf489d56f7c20cd47edc2fcdf6`: push CI `43886197182`, CodeQL and Commitlint green; Server.Tests TRX confirms 11/11 extraction cases passed, none skipped. Item 93 is done; DW-731 stays open.
- Epic-level labels and planned tables for 30.4 through 30.6 are historical. Story specs record 30.4 and 30.5 as done, and their checkpoints and File Scopes are authoritative. Earlier runs predate 30.5, so final-code live proof is still owed as the next owner-dispatched additive snapshot.
- Earlier story numbers are historical aliases and must never be globally rewritten.
- Story 27.3 owns the `production-deployment-verification` lane, and Epic 30 never gates it. Epic 31 owns deployed OpenBao and the runtime Dapr secret-store migration.
