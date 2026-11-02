# Epic 30 Context: CI/CD Pipeline Ownership and Alignment

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Release maintainers can publish and recover one complete Memories release set with a provable source and artifact identity, without silently accepting a partial release. Hexalith.Builds owns reusable CI and pinned release preparation; Memories owns the module-specific checks, publication authorization, deployment assets, and recovery. This is Operational Readiness work, separate from MVP product readiness. The approved sequence is shared CI, local qualification of the pinned publication contract, guarded dispatch, complete image publication, partial recovery, and cutover.

## Stories

- Story 30.1: Shared CI Core and Module-Specific Verification Lanes
- Story 30.2: Pinned Builds Publication and Recovery Qualification
- Story 30.3: Guarded Release Dispatch and Shared Caller Adoption
- Story 30.4: Five-Image Publication Contract
- Story 30.5: Partial-Release Recovery
- Story 30.6: Release Cutover Parity and Rollback

## Requirements & Constraints

- Preserve every existing CI gate. Shared CI handles standard Release build and the explicit Contracts test shard. Memories keeps its seven-project Coverlet union, source-debug build, tenant-negative evidence, story scope, tooling, package topology, web E2E, fast and nightly integration, production deployment verification, and benchmarks. The stable `build` check succeeds only when both shared CI and source-debug succeed. Required check names, TRX, and coverage evidence remain usable.
- Qualification of the pinned Builds publication contract must use the current complete Memories set and owner-approved, exact-revision evidence. Upstream fixture success alone grants no Memories qualification or publication credit. Identity mismatch, a missing member, unsupported verification phase, ambiguous destination, or incomplete evidence fails closed.
- Publication is intentional: an operator dispatches the current `main` tip only after exact-source push CI succeeds. A protected production environment controls the release job; ordinary pushes never publish. The release preparation does not rerun application tests already covered by exact-source CI.
- The release inventory is ten NuGet packages, including EventStoreExtensions, and five images: server, MCP host, access telemetry, telemetry clock, and domain service. The package manifest is authoritative. A package-field migration must preserve all ten IDs and immutable historical recovery evidence.
- A release set binds project-to-repository mappings, source, workflow, and set identity. Each image needs platform and workload-appropriate health verification. Registry authorization mode and any push blocker must be recorded explicitly.
- Recovery uses immutable evidence to prove source, version, and inventory; it publishes only missing members and never overwrites, retags, or accepts an ambiguous destination. Cutover requires package, image, release-asset, authorization, failure, and recovery parity before retiring the prior path. Rollback cannot mutate published versions or tags.
- Each independently verifiable checkpoint needs an owner, exact command or artifact, review state, and completion state. Planned artifacts and accepted risks do not count as passed evidence.

## Technical Decisions

- Pin shared GitHub callers to an approved lowercase 40-character Hexalith.Builds SHA and pass that same SHA as `builds-execution-sha`; do not use a moving ref. Use the pinned `Github/prepare-domain-release` workflow from a caller-owned production job that retains NuGet OIDC Trusted Publishing and publication hooks.
- Keep shared workflow logic in Hexalith.Builds. If a reusable input cannot preserve a Memories gate, extend Builds or document a local exception. Preserve local production deployment asset generation and existing recovery compatibility.
- Generate the server, MCP, access-telemetry, access-telemetry-clock, and domain-service deployment archives from one reviewed source SHA. The first four remain inputs to the independent Story 27.3 production-deployment-verification lane, whose ownership and evidence are unaffected by Epic 30.

## Cross-Story Dependencies

- Story 30.1 precedes 30.2; 30.1 and 30.2 precede guarded dispatch in 30.3. Complete image publication in 30.4 requires 30.2 and 30.3, plus an approved frozen multi-container identity. Partial recovery in 30.5 requires completed publication and qualified immutable evidence. Cutover in 30.6 requires 30.3 through 30.5.
- Earlier Epic 30 story numbers are historical aliases; use the current six-story sequence. The declared Story 27.3 edit to `CiTestInventoryTests.cs` remains, while Story 30.3 owns the next change to that file. Epic 31 separately owns deployed OpenBao and the runtime secret-store migration.
