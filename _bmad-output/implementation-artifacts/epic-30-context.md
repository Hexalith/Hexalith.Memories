# Epic 30 Context: CI/CD Pipeline Ownership and Alignment

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Release maintainers can publish and recover one complete, identity-bound Memories release set without silent partial success. Hexalith.Builds owns standard CI and pinned release preparation. Memories owns module-specific verification, NuGet OIDC Trusted Publishing, publication hooks, deployment assets, and recovery, and no existing gate may weaken. The Administrator's 2026-10-31 reconciliation fixed the order and kept the current release set, but it passes no gate. This is Operational Readiness work: it needs explicit sprint selection and never counts toward MVP readiness or launch.

## Stories

- Story 30.1: Shared CI Core and Module-Specific Verification Lanes
- Story 30.2: Pinned Builds Publication and Recovery Qualification
- Story 30.3: Guarded Release Dispatch and Shared Caller Adoption
- Story 30.4: Five-Image Publication Contract
- Story 30.5: Partial-Release Recovery
- Story 30.6: Release Cutover Parity and Rollback

## Requirements & Constraints

- These stay as named local or companion lanes: story scope, tenant-negative evidence, tooling fixtures, package topology, web E2E, fast and nightly integration, deployment verification, benchmarks, and source-debug. Remove duplicate work only after equivalence is proven. Keep check names stable and documented, and keep TRX and coverage downloadable.
- Shared CI is delivered locally; live GitHub adoption is separate.
  - The shared MTP lane runs only the Contracts tests. The seven-project Coverlet union stays authoritative.
  - The always-run `build` check passes only if both `shared-ci` and `source-debug` succeeded. Failed, cancelled, or skipped predecessors fail it.
  - Commitlint gets the PR title explicitly and also runs on title edits.
- Qualification ties Memories' dry-run, set-drift, repeated-verification, partial-publication, recovery, and upload results to one immutable Builds revision, with owner approval. Upstream fixtures alone never qualify. Any mismatch, missing member, ambiguous destination, or incomplete evidence fails closed.
- Only an intentional operator dispatch publishes. Its SHA must be the `main` tip, and exact-source push CI for it must be green. The release job runs in protected `production` with `cancel-in-progress: false` and does not rerun application tests. Pushes never publish, which supersedes older publish-on-`main` descriptions. A failed identity, count, environment, or destination-absence proof stops the run.
- `tools/release-packages.json` is authoritative. It lists ten packages, including EventStoreExtensions, and `expected-package-count` is `10`. The five images are server, MCP host, access telemetry, telemetry clock, and domain service. The old nine-package/four-image acceptance is superseded.
- The `packageId` → `id` rename is atomic across the manifest, schema, validators, pack, recovery, and fixtures. Tagged historical manifests stay recoverable.
- A release set binds the declared project-to-repository mappings under one source/workflow/set identity (Story 30.4).
- Verify each image's platforms and health, and record the registry authorization mode. A registry push blocker needs a named owner, consequence, and reopen trigger; never work around it. One risk is unverified: zot earlier rejected challenge-response pushes with 401. Re-check it before calling publication green.
- Recovery publishes only the missing members, from immutable evidence. It never overwrites, retags, or accepts an ambiguous destination.
- Cutover must first prove dry-run and rehearsal parity across packages, images, GitHub Release assets, authorization, failure, and recovery. Rollback must never mutate a published version or tag.
- Each checkpoint needs its own owner, command or artifact, review state, and completion state. Planned artifacts and risk acceptances are not evidence.

## Technical Decisions

- Follow EventStore's shared-core-plus-companion-lane model, with Tenants-style coverage and consumer validation where supported. Extend Builds or keep a documented local exception rather than weaken evidence. Never copy shared logic into Memories.
- Pin shared callers to an approved 40-character lowercase Builds SHA, passed identically as `builds-execution-sha`. Never use `@main`.
- Release calls pinned `Github/prepare-domain-release` from a caller-owned production job that keeps OIDC and its hooks, with no API key.
- Resolve versions from the selected Builds catalog and pack only manifest entries. Source and package lanes need pristine evidence at corresponding revisions.
- Image repositories under one set identity: `memories`, `memories-mcp`, `memories-access-telemetry`, `memories-access-telemetry-clock`, and `memories-domain-service`.
- Memories generates the five `.tar.gz` deployment archives (server, mcp, access-telemetry, access-telemetry-clock, domain-service) from one reviewed SHA.

## Cross-Story Dependencies

- Order:
  - 30.2 needs 30.1, which gives it no qualification credit.
  - 30.3 needs 30.1 and 30.2.
  - 30.4 needs 30.2, 30.3, and an approved frozen multi-container identity.
  - 30.5 needs 30.4 and qualified immutable evidence from the owner-approved Builds revision.
  - 30.6 needs 30.3 through 30.5.
- Former 30.2 is now 30.1, and 30.2 is new. Former 30.1, 30.3, 30.4, and 30.5 became 30.3 through 30.6. Historical citations keep their old keys.
- 30.3 owns `CiTestInventoryTests.cs`, which keeps a declared Story 27.3 edit. Stories 30.4 and 30.6 own different sections of `docs/dev/release-runbook.md`.
- Story 27.3 owns `production-deployment-verification` and its tooling. That lane builds the four historical archives locally, and they must stay compatible. Epic 30 never gates or evidences it. Epic 31 owns deployed OpenBao and the secret-store migration.
