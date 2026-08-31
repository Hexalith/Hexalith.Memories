# Epic 28 Context: Owner-Approved EventStore Runtime Adoption

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Memories' source (Debug) and package (Release) build modes must converge on the exact EventStore
runtime identity that EventStore Story 1.20 has durably authorized, instead of tracking an arbitrary
branch tip or an unapproved package version. This closes the last gap in claiming a real,
EventStore-originating full-stack proof (accepted deferred item
`23.7-APPHOST-EVENTSTORE-FULLSTACK`): today the AppHost does not provision an `eventstore` gateway
resource, and current source/package identities do not match Story 1.20's approved pins. The epic
matters because Memories' zero-code DAPR ingestion promise (any EventStore-originating event is
auto-indexed with causal-chain and dual-embedding metadata, no mapping code) must be provable against
one auditable, owner-approved dependency contract rather than an implicitly moving one.

As of 2026-08-02, EventStore Story 1.20's external authorization is satisfied (`final_decision:
available`, `authorize_consumer_migration: true`, a named 40-hex `tested_runtime_sha`, owner
approval, and an approved package version/hash inventory). That satisfies the *external* gate only:
Epic 28 and Story 28.1 remain `backlog` pending explicit selection, and a current tag, repository
HEAD, or unapproved package version is never sufficient authorization by itself.

Planning history also contains a later, more detailed proposal (2026-08-01) that would split this
work into a source-identity story and a package-identity story with specific pinned identities
(EventStore SHA `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`; published package `3.89.0` via
Hexalith.Builds `10af541e...`). That split is **not** reflected in the current `epics.md`,
`sprint-status.yaml`, or `architecture.md` (still D1-D31, no D32) — the canonical Epic 28 remains one
story with the original seven-scenario shape, most recently reconfirmed by a 2026-08-03 readiness
correction (which only flags a missing `**Acceptance Criteria:**` heading, not a content split).
Whoever selects this story should re-verify Story 1.20's current proof packet for the live
authoritative SHA/package identity rather than assuming either historical proposal's numbers are
still current.

## Stories

- Story 28.1: Adopt Owner-Approved EventStore Runtime Identity

## Requirements & Constraints

- The zero-code DAPR ingestion contract is a core product promise (PRD) and must not be redesigned:
  any event-sourced system publishing to a DAPR-compatible bus is auto-indexed (dual embeddings +
  CausationId/CorrelationId as causal graph edges) with no mapping code beyond a subscription.
  EventStore gets the reference/premium experience; the pattern must stay DAPR-generic.
- Source mode must pin to the exact SHA that Story 1.20 currently authorizes (verify live, don't
  assume a historical value), with the EventStore submodule content itself left unedited and only
  Memories-root-declared submodules initialized (no nested submodule init/update).
- Package mode must resolve `Hexalith.EventStore.Client`, `Hexalith.EventStore.Aspire`, and every
  resolved `Hexalith.EventStore*` asset at the exact approved version/hash, sourced only through the
  selected `Hexalith.Builds` gitlink (no Memories-local version override, no EventStore project
  reference leaking into the Release asset graph).
- If Story 1.20 is blocked, non-authorizing, or incomplete, the story stays `backlog` — no EventStore
  or Builds gitlink change, no ingestion/projection/deployment topology redesign.
- Any behavioral incompatibility that can't be resolved without changing the zero-code contract or
  topology must fail closed and route to a separately approved compatibility story — never be
  silently absorbed.
- Closing accepted deferred item `23.7-APPHOST-EVENTSTORE-FULLSTACK` requires: the AppHost provisions
  exactly one `eventstore` gateway resource (no duplicate `statestore`/`pubsub` ownership with
  Memories' existing Dapr components); a real EventStore-originating publish reaches Memories through
  Dapr; the resulting memory is persisted and searchable through both Redis and FalkorDB; duplicate
  replay is ignored; and negative evidence proves no cross-tenant result leakage.
- Evidence claim boundary (architecture): compile/build evidence proves dependency-graph
  compatibility only; Memories-owned Aspire evidence (Redis Stack, FalkorDB, Dapr sidecar) is not
  EventStore-originating proof; direct `/events/ingest` or Dapr-publish-to-Memories tests prove the
  Memories intake contract only. Do not claim "EventStore-to-Memories full-stack" evidence without
  meeting every criterion above.

## Technical Decisions

- Preserve the existing composition chain exactly: `AddMemoriesServerServices()` ->
  `AddServerEventStoreIntegration()` -> `AddMemoriesEventStoreIntegration()`, `UseCloudEvents()`,
  `MapControllers()`, `MapSubscribeHandler()`, the `/events/ingest` route, the `pubsub` component, and
  the `MEMORIES_EVENTSTORE_TOPIC` setting — no direct REST ingestion path for domain event streams.
- Domain state is EventStore-sourced (event-sourcing aggregate model); Redis (syntactic/vector) and
  FalkorDB (graph) are rebuildable projections written via DAPR Workflow activities with
  retry/compensation — this consistency model is unaffected by, and must survive, the identity
  adoption.
- Infrastructure-dependency boundary: product projects (Server, Cli, Mcp, Web, Client.Rest) reach
  infrastructure only via Dapr building blocks or Aspire-injected config; direct infra clients belong
  only in boundary projects (AppHost, Aspire, ServiceDefaults, Redis, EventStore). Adoption work must
  respect this boundary rather than adding new direct infra wiring in product code.
- Required executable guards: reject a gitlink/checkout that drifts from the approved source SHA;
  reject any local package-version/override authority for EventStore packages; reject nested-submodule
  initialization; keep source and package evidence lanes separate (a pass in one mode grants no
  evidence in the other — do not claim byte-identical convergence between them).
- Validation obligations before claiming completion: Debug/source and Release/package builds both
  pass; exact Client/Aspire assets are proven (project-reference in source mode, package in release
  mode, with matching hashes where applicable); focused EventStore/Server contract tests pass; and a
  real DAPR publish proves a persisted, searchable memory result with duplicate replay ignored.
- The current AppHost calls only `AddHexalithEventStoreSecurity()` — it does not call
  `AddHexalithEventStoreGatewayProject()`, `AddHexalithEventStorePlatformProjects()`, or
  `AddHexalithEventStore(...)`. Introducing the EventStore Aspire gateway helper will add Dapr
  resources also named `statestore`/`pubsub`, which collide with Memories' existing resources of the
  same name — this story must deliberately design single ownership for those components rather than
  blindly composing the helper.

## Cross-Story Dependencies

- Depends externally on EventStore Story 1.20 remaining `available`/`authorize_consumer_migration:
  true` with a current owner-approved identity; loss or invalidation of that authorization forces this
  story back to fail-closed/backlog.
- Depends on completed `23.7-APPHOST-EVENTSTORE-FULLSTACK` acceptance context (Epics 22/23/24
  retrospective actions) — this story is the designated resolution owner for that accepted gap and is
  the re-open trigger the moment it is selected.
- Epic 31 / Story 31.2 is intentionally sequenced to start its implementation baseline only after this
  epic's dependency-identity work lands, so its baseline reflects the final EventStore/Builds
  gitlinks.
- Only Memories-root-declared submodule gitlinks may move (EventStore, and possibly Builds if package
  identity is involved); submodule contents and nested submodules stay out of scope for this story.
