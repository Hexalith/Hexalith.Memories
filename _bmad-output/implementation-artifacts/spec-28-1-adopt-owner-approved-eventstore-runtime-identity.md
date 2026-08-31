---
title: 'Adopt Owner-Approved EventStore Runtime Identity'
type: 'feature'
created: '2026-08-31'
status: 'in-progress'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-28-context.md']
baseline_commit: 'bcfd84012f346efc83fa1f13b1dbe3413ef6f52a'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Memories' EventStore submodule checkout has drifted past EventStore Story 1.20's owner-approved commit, the package pin doesn't match the approved proof identity, and the AppHost still provisions no `eventstore` gateway resource — so deferred item `23.7-APPHOST-EVENTSTORE-FULLSTACK` (real EventStore-to-Memories full-stack proof) stays open.

**Approach:** Pin Debug/source and Release/package modes to Story 1.20's exact approved identity (source SHA `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`; package `999.1.20-proof.fa2d1c9910f8`), add exactly one `eventstore` AppHost resource without duplicating Memories' existing `statestore`/`pubsub` Dapr components, and prove a real EventStore-originated Dapr publish is persisted and searchable with duplicate replay ignored.

## Boundaries & Constraints

**Always:**
- `references/Hexalith.EventStore` gitlink and checkout equal `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594` exactly in Debug/source mode; submodule content is not edited; no nested submodule init/update.
- Release/package mode resolves every `Hexalith.EventStore*` package asset at exactly `999.1.20-proof.fa2d1c9910f8` (hash manifest `4271ddc76411780591423ab024b776cd34a2abccf1cc2dac03a245e141dbe0bc`); the existing `FailReleaseWithHexalithProjectReferences`/`FailPackWithHexalithProjectReferences` guards (`Directory.Build.props`) must keep passing — no EventStore `ProjectReference` in the Release/Pack/Publish graph.
- Preserve the composition chain verbatim: `AddMemoriesServerServices()` -> `AddServerEventStoreIntegration()` -> `AddMemoriesEventStoreIntegration()`, `UseCloudEvents()`, `MapControllers()`, `MapSubscribeHandler()`, `/events/ingest`, the `pubsub` component, `MEMORIES_EVENTSTORE_TOPIC`.
- AppHost adds exactly one `eventstore` resource via `AddHexalithEventStoreGatewayProject(builder)` (project-resource-only helper) — never `AddHexalithEventStore(...)`, which hardcodes `statestore`/`pubsub` Dapr component names and collides with Memories' existing components.
- Only Memories-root-declared submodule gitlinks move (`Hexalith.EventStore`, and `Hexalith.Builds` if package identity requires it); no nested/unrelated submodule changes.
- If Story 1.20's live proof packet no longer shows `final_decision: available` and `authorize_consumer_migration: true` when re-verified, stop and leave the story `backlog` — no gitlink or topology change.

**Ask First:**
- Sourcing `999.1.20-proof.fa2d1c9910f8` package bytes for Release/package mode: `NuGet.config` today configures only `nuget.org`, and `Hexalith.Builds`' central `Directory.Packages.props` pins `HexalithEventStoreVersion` to `3.100.0`. Confirm whether `Hexalith.Builds` must be bumped to a commit exposing the proof version, or an isolated local feed is intended, before touching `NuGet.config`/`Directory.Packages.props`.
- Dapr sidecar wiring for the new `eventstore` resource (AppId, which `statestore`/`pubsub` references it needs): `AddHexalithEventStoreGatewayProject()` adds only the project resource, so sidecar/component wiring is caller-authored. Confirm the intended pattern before implementing.

**Never:**
- Never pair `AddHexalithEventStoreGatewayProject()`/`AddHexalithEventStorePlatformProjects()` with `AddHexalithEventStore(...)` in the same AppHost run (component-name collision).
- Never redesign ingestion/projection/deployment topology beyond identity adoption plus the one `eventstore` resource.
- Never treat a current tag, repo HEAD, or unapproved package version as sufficient authorization by itself.

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/1-20-owner-approved-parity-closure-proof-packet.md` -- source of truth for approved SHA/package/hash identity; re-verify live before pinning.
- `.gitmodules` (root, lines 4-6, 16-18) -- EventStore/Builds gitlink entries, no `branch=` pin (SHA-only).
- `src/Hexalith.Memories.AppHost/Program.cs:38` -- only existing EventStore-Aspire call (`AddHexalithEventStoreSecurity()`); add `AddHexalithEventStoreGatewayProject()` here.
- `src/Hexalith.Memories.AppHost/Program.cs:217-233` -- existing `statestore`/`pubsub` `AddDaprComponent` calls (Story-9.1 owned); the new `eventstore` resource's sidecar must reference these, not duplicate them.
- `src/Hexalith.Memories.AppHost/Program.cs:298-301,319-325` -- existing pattern for wiring a resource's Dapr sidecar to `stateStore`/`pubSub` via `WithReference` -- mirror for the `eventstore` resource.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStorePlatformExtensions.cs:92-99` -- `AddHexalithEventStoreGatewayProject(builder, eventStoreName="eventstore")`, project-resource-only, safe.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs:132-302` (esp. 185-222) -- `AddHexalithEventStore(...)`, hardcodes `statestore`/`pubsub`/sidecar AppId `eventstore` -- do not call.
- `Directory.Build.props` (root, lines 29-48, 94-100) -- `UseHexalithProjectReferences`/`HexalithEventStoreFromSource` switch and the Release/Pack leakage guards.
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj:15,19` -- conditional ProjectReference (line 15) vs PackageReference (line 19) on `HexalithEventStoreFromSource`.
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj:52` -- unconditional `PackageReference` to `Hexalith.EventStore.Client` (no source-mode switch here).
- `Directory.Packages.props` (root, lines 1-13) + `references/Hexalith.Builds/Props/Directory.Packages.props:8,40-52` -- `HexalithEventStoreVersion` default and per-package `PackageVersion` entries; currently `3.100.0`.
- `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs:110-139,288-310` -- real-Dapr-publish + duplicate-replay test; doc comment (22-26) already flags it as not full-stack EventStore proof -- extend or add a sibling proving the new `eventstore` resource is the publisher. Runs nightly only (`Category=IntegrationSlow`, excluded from PR/merge-queue per `.github/workflows/ci.yml:432`, included in `.github/workflows/nightly.yml:109`).
- `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs:33,143-158` and `DeploymentConfigurationContractTests.cs:23,64-181` -- existing contract tests locking route/topic/env wiring; must keep passing unmodified in substance.
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs:174,208` -- existing duplicate-delivery idempotency tests to reuse as the pattern for any new negative/tenant-isolation case.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.EventStore` gitlink -- pin checkout to `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`, verified live against the current proof packet -- satisfies source-identity AC. Debug/source build verified passing (`-p:UseHexalithProjectReferences=true`). CI gained a matching Debug/source build step in `.github/workflows/ci.yml`.
- [ ] `references/Hexalith.Builds` gitlink and `Directory.Packages.props` chain -- resolve to expose `HexalithEventStoreVersion=999.1.20-proof.fa2d1c9910f8` with matching hashes (after resolving the Ask-First sourcing question) -- satisfies package-identity AC.
- [ ] `src/Hexalith.Memories.AppHost/Program.cs` -- add `AddHexalithEventStoreGatewayProject(builder)` and wire its Dapr sidecar to the existing `statestore`/`pubsub` resources, matching the `memories` resource pattern -- satisfies single-`eventstore`-resource AC without duplicate ownership.
- [ ] `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/` -- add/extend a nightly integration test proving a publish originated from the new `eventstore` AppHost resource is persisted+searchable via Redis and FalkorDB, duplicate replay ignored, with negative evidence of no cross-tenant leakage -- satisfies full-stack proof AC.
- [ ] CI/build scripts -- add a Debug/source-mode build invocation (`-p:UseHexalithProjectReferences=true`) alongside the existing Release/package build, since none currently exists -- satisfies "both builds pass" AC.

**Acceptance Criteria:**
- Given Story 1.20 remains available/authorizing at re-verification, when Debug/source mode builds, then the EventStore gitlink+checkout equal the approved SHA and Debug build passes.
- Given the same, when Release/package mode restores, then all `Hexalith.EventStore*` assets resolve at the approved version/hash from the selected `Hexalith.Builds` gitlink, with no project-reference leakage.
- Given the AppHost starts, when the topology is composed, then exactly one `eventstore` resource exists and no duplicate `statestore`/`pubsub` ownership occurs.
- Given a real EventStore-originated Dapr publish, when it reaches Memories, then the resulting memory is persisted and searchable via Redis and FalkorDB, duplicate replay is ignored, and no cross-tenant result leakage occurs.
- Given Story 1.20 is blocked/non-authorizing/incomplete at any point, when this story is worked, then it stays `backlog` with no gitlink or topology change.

## Design Notes

The two Ask-First items are genuine open decisions, not implementation details to infer: (1) whether the proof package version ships via a `Hexalith.Builds` bump or an isolated local feed given `NuGet.config` only has `nuget.org` today, and (2) the exact sidecar/component wiring for the new `eventstore` resource. Resolve both with the human before writing AppHost or package-restore code — guessing either risks a component-name collision (production-impacting: `statestore`/`pubsub` are Memories' real Redis-backed stores) or a build that silently can't restore.

## Verification

**Commands:**
- `dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true` -- expected: success, EventStore consumed via ProjectReference at the pinned SHA.
- `dotnet build Hexalith.Memories.slnx --configuration Release` -- expected: success, EventStore consumed via PackageReference at the approved package version.
- `dotnet test tests/Hexalith.Memories.Server.Tests --filter "FullyQualifiedName~EventStoreIntegration|FullyQualifiedName~Deployment"` -- expected: all contract/idempotency tests pass unchanged in substance.
- `dotnet test tests/Hexalith.Memories.IntegrationTests --filter "Category=IntegrationSlow&FullyQualifiedName~EventStoreIntegration"` -- expected: real-Dapr-publish full-stack proof test passes, including duplicate-replay-ignored and no-cross-tenant-leakage assertions.

**Manual checks (if no CLI):**
- Diff the live `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/1-20-owner-approved-parity-closure-proof-packet.md` frontmatter against the pins used here immediately before implementation, in case Story 1.20 has moved since this spec was drafted.
