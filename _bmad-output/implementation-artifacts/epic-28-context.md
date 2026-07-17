# Epic 28 Context: Owner-Approved EventStore Runtime Adoption

## Goal

Align Memories Debug/source and Release/package modes to the exact EventStore identity authorized by
EventStore Story 1.20, while preserving the shipped zero-code DAPR ingestion contract.

## Activation And Ownership

Epic 28 and Story 28.1 remain backlog. Do not create the Story 28.1 implementation file or move it to
`ready-for-dev` until Story 1.20 durably records migration authorization, the approved tested source
SHA, the approved package version and hashes, and named owner approval. Memories and EventStore
maintainers jointly own adoption; the EventStore release owner owns the upstream artifact identities.

## Current Identity Gap

At registration, Memories pins EventStore source commit
`8aa6d0f0a417034d0c46eb9506fb7196a013401b`, while its Builds commit
`598f5063f13dccbaa1251d8af6a8a72ad5820c20` exposes EventStore package version `3.68.1`.
Neither identity is Story-1.20-approved migration evidence, and current EventStore HEAD or a tag cannot
substitute for that approval.

## Invariants

- Source mode requires EventStore gitlink and checkout equality and no EventStore submodule edits.
- Package mode requires exact approved package bytes, hashes, and version through an already-landed
  Builds commit, with no EventStore project reference in Release assets.
- Preserve the registration chain `AddMemoriesServerServices()` →
  `AddServerEventStoreIntegration()` → `AddMemoriesEventStoreIntegration()`.
- Preserve CloudEvents middleware/controller/subscription mapping, `/events/ingest`, `pubsub`, and
  `MEMORIES_EVENTSTORE_TOPIC`.
- Domain event streams continue through DAPR pub/sub; adoption does not introduce direct REST intake.
- Any behavioral or topology redesign requires a separate approved compatibility story.

## Required Evidence

- Isolated Debug/source and Release/package restore/build evidence.
- Exact EventStore Client/Aspire and transitive asset identities with approved hash comparison.
- Focused EventStore integration and Server contract tests.
- Real DAPR publish through the subscribed topic to a persisted/searchable result.
- Duplicate replay evidence proving the same event is ignored without a second memory unit.
