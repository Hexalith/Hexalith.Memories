# Epic 30 Context: CI/CD Pipeline Ownership and Alignment

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Release maintainers can publish and recover one complete, identity-bound Memories release set without silent partial success. Hexalith.Builds supplies reusable standard CI and release preparation; Memories keeps its module-specific evidence, caller-owned Trusted Publishing, publication hooks, deployment assets, inventory, and recovery. The Administrator's 2026-10-31 bounded reconciliation applies the approved six-story dependency order while preserving the current release set. It authorizes the selected shared CI slice, not publication or a waiver of later qualification and activation gates. Operational evidence remains separate from MVP readiness and launch authorization.

## Stories

- Story 30.1: Shared CI Core and Module-Specific Verification Lanes
- Story 30.2: Pinned Builds Publication and Recovery Qualification
- Story 30.3: Guarded Release Dispatch and Shared Caller Adoption
- Story 30.4: Five-Image Publication Contract
- Story 30.5: Partial-Release Recovery
- Story 30.6: Release Cutover Parity and Rollback

## Requirements & Constraints

- Alignment preserves tenant-negative evidence, story-scope enforcement, tooling fixtures, release-package topology, web E2E, fast integration, production deployment verification, nightly slow integration, benchmarks, source-debug verification, and unioned first-party coverage. Duplicate work may disappear only after demonstrated equivalence, with stable documented required checks and downloadable TRX/coverage evidence.
- The selected bounded shared MTP lane runs Contracts tests only. Keep the seven-project local Coverlet inventory and unioned coverage authoritative, with the small Contracts overlap documented. The existing Debug/source lane becomes `source-debug`. The stable `build` required check always runs and passes only when both `shared-ci` and `source-debug` succeeded; failures, cancellations, and skipped predecessors must fail it.
- Publication is an intentional operator dispatch from the exact current `main` tip with successful push CI for that same SHA. Ordinary pushes must not publish. Require protected production approval, non-cancelling release concurrency, and source revalidation before login/publication. Failed or ambiguous source, inventory, environment, destination-absence, or Builds-identity proof stops publication.
- Preserve today's ten package IDs, including EventStoreExtensions, and five images: server, MCP compatibility host, access telemetry, access-telemetry clock, and domain service. The canonical manifest remains authoritative; non-packable hosts do not increase its package count. Historical nine-package/four-image acceptance is superseded for active Epic 30 without dropping members or rewriting archived evidence.
- Manifest adoption updates schema, validators, pack scripts, recovery, and fixtures atomically from `packageId` to `id`; immutable historical tagged `packageId` inventories remain recoverable. The declared current package count for later guarded preparation is ten.
- A release set binds source, version, approved Builds revision, and mappings. Verify each image's declared platforms and workload-appropriate health. Registry authorization limitations need named owners, consequences, and reopen triggers; historical authentication observations are not current success evidence.
- Recovery consumes immutable identity-bound evidence, skips already-published members, publishes only missing members, and never overwrites, retags, or treats ambiguous destinations as success. Cutover needs dry-run and controlled rehearsal evidence across packages, images, assets, authentication, failure, and recovery before retiring the retained publisher. Rollback cannot mutate shipped artifacts.
- Each independently verifiable checkpoint needs its own owner, exact evidence command or artifact, review state, and completion state. Planned artifacts are not evidence. CI alignment additionally needs lane-by-lane old/new ownership, trigger, required-check, validation, and rollback mapping.

## Technical Decisions

- Use the EventStore-shaped shared-core-plus-companion-lane model, with compatible Tenants coverage and consumer-validation patterns. Delegate compatible work to upstream workflows; retain unsupported gates locally with documented exceptions or add capabilities upstream. Do not copy reusable logic into Memories.
- Pin shared `domain-ci.yml` to an approved lowercase 40-character Builds SHA and pass the identical `builds-execution-sha`. This bounded approved selection supersedes earlier mutable CI wording for Epic 30. Later guarded release uses `Hexalith/Hexalith.Builds/Github/prepare-domain-release` at the same full identity supplied as its execution SHA, retaining an ordinary caller-owned production job and NuGet OIDC Trusted Publishing. It must not introduce a supplied API key or duplicate application tests already proven by exact-source CI.
- CI produces Release assets using packaged external Hexalith dependencies. Source debugging remains Debug with project references. Use per-project test execution and the `.slnx` format. Conventional Commit validation includes explicit PR titles and edited-title events; this task does not change live branch settings.
- Memories owns deployment asset generation. Preserve `server.tar.gz`, `mcp.tar.gz`, `access-telemetry.tar.gz`, and `access-telemetry-clock.tar.gz` plus current `domain-service.tar.gz` from one reviewed source SHA. Product runtime secrets remain behind Dapr Secrets/OpenBao; pipeline alignment does not change that ownership or authorize credential disclosure.

## Cross-Story Dependencies

- Active order is shared CI → pinned-contract qualification → guarded dispatch → five-image publication → partial recovery → cutover. Shared CI is independently selectable without multi-container qualification. Guarded release requires completed CI and qualification; publication, recovery, and cutover retain their explicit predecessor and owner-approval gates. Candidate upstream support or successful upstream fixtures alone never qualifies Memories.
- The old-to-new map is former 30.2 → 30.1, new qualification → 30.2, former 30.1 → 30.3, former 30.3 → 30.4, former 30.4 → 30.5, and former 30.5 → 30.6. Earlier proposal/evidence citations retain historical identities; canonical Epic 30 and sprint metadata hold aliases. No later story gains completion credit from this reconciliation.
- Story 27.3 independently owns production rendering, verification, evidence tooling, and its deployment-verification CI lane, consuming the historical four stable archives from one reviewed source. Epic 30's status does not gate that lane and cannot supply its evidence. Deployed OpenBao hardening and runtime secret-store migration remain Epic 31.
