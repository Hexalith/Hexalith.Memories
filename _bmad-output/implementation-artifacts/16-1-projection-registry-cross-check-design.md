# Story 16.1: Projection Registry Cross-Check Design

Status: ready-for-dev

## Story

As an operator,
I want handler mismatch detection to compare routing declarations with runtime-bound projection bindings,
so that events can no longer look "handled" from routing configuration while silently lacking a projection consumer.

## Acceptance Criteria

1. Given handler mismatch detection currently treats `EventStoreIntegration:Routing:SourceToTenantMap` as the registration source of truth, when this story designs and implements the projection cross-check, then the implementation defines an explicit repository-owned projection binding contract that can represent tenant, source prefix or aggregate, projection type/name, and supported event/aggregate patterns without mutating the `Hexalith.EventStore` submodule.
2. Given EventStore client discovery already exposes projection metadata through `DiscoveryResult.Projections`, when the implementation chooses a projection registry shape, then it reuses existing EventStore discovery concepts where compatible or records a clear rationale for a Memories-owned adapter, and it does not add a broad new dependency or reflection scanner without tests proving the need.
3. Given a tenant has a `SourceToTenantMap` entry but the runtime projection registry has no matching projection binding, when `HandlerMismatchDetector.DetectAsync` runs, then the report includes an actionable warning for the configured-but-unbound projection path without regressing existing `UnhandledEventType`, `StaleHandler`, or `VersionMismatch` behavior.
4. Given a tenant has both routing and matching projection bindings, when observed event types match the configured aggregate/source prefix, then mismatch detection remains healthy and does not emit the new projection-binding warning.
5. Given this story may extend the experimental HXL002 API shape, when it changes `HandlerMismatchCategory`, `HandlerMismatchReport`, `HandlerRegistration`, CLI formatting, or REST client behavior, then the change is additive, serialized through `MemoriesJsonContext`, covered by contract/CLI/server tests, and preserves existing JSON property names and CLI filtering semantics.
6. Given projection registry data may be absent in deployments that have not opted into the new contract, when the registry has no bindings, then the failure posture is explicit: either report projection bindings as unknown/disabled without false warnings, or emit warnings only when the operator has configured the registry as authoritative.
7. Given the deferred work entry is the source of this story, when the story completes, then `_bmad-output/implementation-artifacts/deferred-work.md` marks `Story-9.3-ProjectionRegistryCrossCheck` as `resolved`, `accepted`, or `carried-forward` with evidence or rationale, and focused validation covers the selected disposition.

## Tasks / Subtasks

- [ ] Task 0 - Preflight the deferred entry and current implementation (AC: 1-7)
  - [ ] Confirm `Story-9.3-ProjectionRegistryCrossCheck` exists in `_bmad-output/implementation-artifacts/deferred-work.md` and is still carried-forward from Story 15.5.
  - [ ] Read `HandlerMismatchDetector.cs`, `HandlerRegistryService.cs`, `HandlerMismatchDetectorTests.cs`, `HandlerRegistryServiceTests.cs`, `HandlerMismatchReport.cs`, `HandlerRegistrationSnapshot.cs`, `HandlersMismatchesCommand.cs`, `MemoriesClient.cs`, and `docs/dev/eventstore-integration.md` before editing.
  - [ ] Inspect EventStore client discovery in the submodule (`DiscoveryResult`, `DiscoveredDomain`, `EventStoreServiceCollectionExtensions`, `IEventStoreProjection`) as reference only; do not modify submodule files unless the maintainer explicitly expands scope.

- [ ] Task 1 - Define the projection binding contract (AC: 1, 2, 6)
  - [ ] Add a small repository-owned contract for runtime projection bindings, likely under `src/Hexalith.Memories.EventStore` if the boundary belongs to EventStore integration, or under `src/Hexalith.Memories.Server/Handlers` if the binding source is intentionally server-local.
  - [ ] Include enough shape for the detector to answer: tenant id, source prefix or aggregate type, projection type/name, and event/aggregate patterns covered.
  - [ ] Provide a default implementation with an explicit empty/unknown posture so deployments without projection bindings do not receive false warnings by default.
  - [ ] If reusing EventStore `DiscoveryResult.Projections` is viable without new dependency churn, add an adapter. If not, document the reason in dev notes or operations docs.

- [ ] Task 2 - Wire the registry into handler mismatch detection (AC: 3, 4, 6)
  - [ ] Inject the projection binding provider into `HandlerMismatchDetector` without breaking existing constructor validation and tests.
  - [ ] Add the configured-but-unbound projection check after routing entries are resolved and before telemetry emission, preserving existing stale, unhandled, and version-mismatch behavior.
  - [ ] Decide whether the new mismatch is a new `HandlerMismatchCategory` value or a clearly documented use of an existing category. If a new category is added, update JSON serialization, CLI formatting, tests, and docs in the same story.
  - [ ] Ensure the detector's summary remains useful and does not count projection registry absence as observed event data.

- [ ] Task 3 - Update registry/listing surfaces only where needed (AC: 5, 6)
  - [ ] Update `HandlerRegistryService` only if the handler list needs to expose projection-binding status; otherwise leave list output unchanged and keep the story scoped to mismatches.
  - [ ] If the REST contract changes, update `MemoriesJsonContext`, `MemoriesClient`, and consumer-driven contract tests.
  - [ ] Preserve CLI behavior: JSON output remains unfiltered, `--severity` still filters by severity only, and `--exclude-stale` suppresses only `StaleHandler`.

- [ ] Task 4 - Add focused tests (AC: 3-6)
  - [ ] Add `HandlerMismatchDetectorTests` coverage for route configured + no projection binding -> warning mismatch.
  - [ ] Add `HandlerMismatchDetectorTests` coverage for route configured + matching projection binding -> no projection-binding mismatch.
  - [ ] Add absence-posture tests proving an empty/default registry does not create noisy warnings unless explicitly configured as authoritative.
  - [ ] Add contract/CLI tests if any HXL002 enum or serialized shape changes.

- [ ] Task 5 - Update documentation and deferred-work disposition (AC: 2, 5, 7)
  - [ ] Update `docs/dev/eventstore-integration.md` section 11 to explain the projection-registry cross-check and operator next steps.
  - [ ] Update `docs/dev/telemetry.md` only if new metrics or categories affect telemetry guidance.
  - [ ] Update `_bmad-output/implementation-artifacts/deferred-work.md` for `Story-9.3-ProjectionRegistryCrossCheck` with the final disposition and evidence.
  - [ ] Add completion notes and file list to this story.

- [ ] Task 6 - Validate (AC: 3-7)
  - [ ] Run `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~HandlerMismatchDetectorTests|FullyQualifiedName~HandlerRegistryServiceTests"`.
  - [ ] If contracts or CLI output changed, run `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~HandlersMismatchesCommandTests|FullyQualifiedName~MemoriesClientHandlersContractTests"`.
  - [ ] If deferred-work structured fields changed, run `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"`.
  - [ ] Run `git diff --check`.

## Dev Notes

### Current Implementation State

Story 9.3 shipped a read-side handler registry and mismatch detector. `HandlerMismatchDetector.DetectAsync` loads observed event tuples from `IObservedEventTypeStore`, derives routed entries from `TenantEventRoutingOptions.SourceToTenantMap`, emits `StaleHandler` when no observations exist, emits `UnhandledEventType` when observed aggregate types do not match routed source-prefix aggregate tokens, and emits `VersionMismatch` when concurrent terminal event-name versions are observed. It does not prove that tenant application code has a projection bound for the routed event stream.

`HandlerRegistryService.GetSnapshotAsync` also treats `SourceToTenantMap` entries as handler registrations. It groups entries by tenant, verifies tenant state, reads observations once per tenant, and returns one `HandlerRegistration` row per source prefix. It intentionally degrades per tenant on observation-store read failures.

The deferred entry `Story-9.3-ProjectionRegistryCrossCheck` states the precise gap: an event can be "handled" from routing's point of view but still be silently ignored downstream by application projection code. Story 15.5 carried the item forward and proposed this story as the architectural design plus focused proof.

### Relevant Existing Files

- `src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs` - primary detector to extend.
- `src/Hexalith.Memories.Server/Handlers/HandlerRegistryService.cs` - list endpoint backing service; update only if registry status belongs in the list response.
- `src/Hexalith.Memories.Contracts/V1/HandlerMismatchReport.cs` - HXL002 mismatch API contract and enum values.
- `src/Hexalith.Memories.Contracts/V1/HandlerRegistrationSnapshot.cs` - HXL002 list API contract.
- `src/Hexalith.Memories.Cli/Commands/HandlersMismatchesCommand.cs` - CLI filtering and rendering path.
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` - REST client path for handler mismatches.
- `src/Hexalith.Memories.EventStore/*` - Memories-owned EventStore integration boundary; preferable home for a registry abstraction if it should be host-provided.
- `Hexalith.EventStore/src/Hexalith.EventStore.Client/Discovery/DiscoveryResult.cs` and `DiscoveredDomain.cs` - submodule reference for existing projection discovery concepts. Reference only unless scope is explicitly expanded.

### Design Guardrails

- Do not change root-level or nested submodules for this story. The submodule's `DiscoveryResult.Projections` can inspire an adapter, but this repository should own the Memories-side projection binding contract unless a separate submodule change is approved.
- Do not retrofit Memories Server authentication. Story 9.3 deferred that separately as `Story-9.3-MemoriesServerAuthN`.
- Do not broaden this into the Story 9.3 Tier-2 integration-test backlog. Keep this story focused on projection binding visibility and focused unit/contract proof.
- Preserve current mismatch categories and semantics unless adding an explicit new category is the smallest clear contract. If a new category is added, every formatter/serializer/test consumer must understand it.
- Treat absent projection registry data carefully. A default empty provider should not make every routed tenant noisy unless the operator has declared the registry authoritative.
- Keep telemetry low-cardinality. If a metric changes, reuse the existing `memories.handlers.mismatches` tenant/severity tags unless a documented reason justifies a new tag.

### Testing Notes

Use xUnit, Shouldly, and NSubstitute. The closest patterns are in `HandlerMismatchDetectorTests` and `HandlerRegistryServiceTests`. Keep new tests tenant-specific and deterministic. If adding a new enum value, add coverage where CLI human/table formatters render mismatch category names and where JSON round-trips through `MemoriesJsonContext`.

### Previous Story Intelligence

Story 15.5 explicitly warned not to patch `Story-9.3-ProjectionRegistryCrossCheck` casually inside a governance sweep. It positioned this item as an architectural design candidate with target artifacts `HandlerMismatchDetector.cs`, `HandlerRegistryService.cs`, `HandlerMismatchDetectorTests.cs`, and any projection-registry design note created by this story.

Story 9.3 completion notes record that the original surface is pure read-side: no handler-registration endpoint, no runtime subscription mutation, and `SourceToTenantMap` is the routing source of truth. This story can add projection-binding evidence, but should not create a write-side handler management feature.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 16 and Story 16.1 acceptance criteria.
- `_bmad-output/implementation-artifacts/deferred-work.md` - `Story-9.3-ProjectionRegistryCrossCheck` structured carry-forward entry and original Story 9.3 deferred note.
- `_bmad-output/implementation-artifacts/15-5-deferred-register-triage-sweep.md` - follow-up proposal shape and rationale for promoting this item.
- `_bmad-output/implementation-artifacts/9-3-handler-registration-and-mismatch-detection.md` - original handler registry implementation context, deferred entries, and read-side guardrails.
- `docs/dev/eventstore-integration.md` - operator docs for `SourceToTenantMap`, handler listing, and mismatch categories.
- `docs/dev/telemetry.md` - Story 9.3 handler metrics and substrate-separation guidance.
- `Hexalith.EventStore/src/Hexalith.EventStore.Client/Discovery/DiscoveryResult.cs` - existing projection discovery concept in the submodule.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Story created from the Story 15.5 follow-up proposal "Projection Registry Cross-Check Design" after Epic 15 closure.
- Source context loaded from `deferred-work.md`, Story 15.5, Story 9.3, `epics.md`, handler services/tests/contracts, CLI mismatch command, docs, and EventStore discovery reference files.
- No web research was needed; the implementation surface is repository-owned .NET code and local submodule reference material.

### Completion Notes List

- Ready-for-dev story created on 2026-05-19.
- Scope is limited to projection-binding registry design, detector proof, focused HXL002/CLI/server tests, docs, and deferred-work disposition.
- Do not mutate submodules or import the entire deferred-work backlog into Epic 16.

### File List

- `_bmad-output/implementation-artifacts/16-1-projection-registry-cross-check-design.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Story Completion Status

Story context created and ready for development. The developer has the active deferred ID, source targets, design guardrails, expected tests, and scope boundaries needed to implement the projection registry cross-check without reopening unrelated Story 9.3 work.
