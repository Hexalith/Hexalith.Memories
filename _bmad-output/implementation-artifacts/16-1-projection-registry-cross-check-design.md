# Story 16.1: Projection Registry Cross-Check Design

Status: done

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
8. Given source routes and projection bindings use different vocabulary, when the detector compares them, then the story defines a canonical comparison key before implementation: tenant id plus normalized route source prefix or aggregate token plus supported event/aggregate pattern, with documented casing, slash, prefix, wildcard, and version semantics.
9. Given projection registry authority controls whether warnings are trustworthy, when registry data is absent, default, unavailable, or explicitly non-authoritative, then the detector does not emit configured-but-unbound warnings and instead preserves existing mismatch output; warnings are emitted only when an authoritative registry is present for the relevant tenant/application boundary.
10. Given projection registry diagnostics are operator-facing, when a configured-but-unbound warning is emitted, then the payload and CLI rendering identify the configured source/tenant/event key, expected projection-binding key, projection identity when known, and a concrete remediation without adding required top-level JSON fields or changing severity filtering behavior.
11. Given projection binding data is tenant-scoped operational metadata, when the provider is queried and warnings are rendered, then the implementation must not enumerate or expose projection bindings from other tenants beyond the minimum tenant-mismatch evidence needed for the selected tenant's diagnostic, and logs, snapshots, and CLI output must not include connection strings, credentials, raw endpoint tokens, or DI implementation details.
12. Given the registry only proves declared runtime bindings, when a projection binding matches a route, then the detector treats that route as bound but does not claim the projection is live, healthy, caught up, or consuming successfully unless a later story adds health/lag evidence and contract tests for that separate signal.

## Party-Mode Hardening Clarifications

- Runtime-bound source of truth must be explicit. Prefer a Memories-owned projection binding provider interface in this repository, with an authority/posture value such as `Unknown`, `NonAuthoritative`, or `Authoritative`; do not infer runtime binding by scanning arbitrary DI internals or by naming conventions alone.
- `DiscoveryResult.Projections` from `Hexalith.EventStore` may be used only through a narrow adapter if it proves the same runtime-bound consumer semantics needed by the detector. If it is design-time or insufficiently authoritative, document that rationale and keep the contract owned by `Hexalith.Memories`.
- The configured-but-unbound warning is defined as: a `SourceToTenantMap` entry exists for the tenant/application scope, an authoritative projection registry is available for that scope, and no matching runtime projection binding covers the normalized route/event key.
- Non-warning states are explicit: no registry exposed, registry source unavailable, registry marked non-authoritative, projection binding found for another tenant only, or projection binding exists without any configured route. These states must not create new projection-binding warnings unless the story intentionally adds a separate additive diagnostic and tests it.
- Existing mismatch categories remain independent. The new projection check must not suppress, duplicate, or reclassify `UnhandledEventType`, `StaleHandler`, or `VersionMismatch`; combined cases should preserve all applicable diagnostics.
- Contract changes are additive and default-safe. Existing serialized `HandlerMismatchReport`, `HandlerRegistrationSnapshot`, CLI JSON, REST client, and formatter behavior must remain valid; prefer optional item-level fields or a new enum value with full serializer/formatter/test coverage over required top-level JSON additions.
- The feature detects and reports mismatches only. It must not auto-register projections, mutate `SourceToTenantMap`, create write-side handler management, retrofit server authentication, broaden into the Tier-2 integration-test backlog, or modify root-level or nested submodules.
- Operator docs must show concrete examples for configured-and-bound, configured-but-unbound, registry-absent/unknown, and tenant-mismatched binding states.

## Advanced Elicitation Hardening Clarifications

- Tenant scope is part of the trust boundary, not just the comparison key. The detector should query the provider for the current tenant/application scope and avoid broad cross-tenant enumeration in normal paths; any tenant-mismatch diagnostic must be additive, sanitized, and backed by a focused test.
- Matching behavior must be deterministic and centralized. Implement one matcher/normalizer for route prefixes, aggregate tokens, event names, wildcard/prefix patterns, casing, slash trimming, duplicates, and event-version suffixes; avoid duplicating ad hoc comparison logic across detector, CLI, docs, and tests.
- Duplicate or ambiguous projection bindings should not create unstable output. The story should define whether equivalent bindings are deduplicated, whether multiple projection identities are summarized deterministically, and whether conflicting patterns are reported as one configured-but-unbound warning or a separate additive diagnostic.
- Provider failures, timeouts, and partial discovery should degrade to an explicit unavailable/unknown posture that preserves existing mismatch categories and avoids configured-but-unbound false positives. If the implementation surfaces provider failure evidence, it must be optional, sanitized, and covered by tests.
- The new registry contract must not promise projection liveness, replay progress, lag, or subscription health. This story proves route-to-binding coverage only; runtime health belongs to a future telemetry or operations story.
- Keep implementation escape hatches small. Any EventStore discovery adapter, feature option, or authority flag should be a narrow wrapper around the repository-owned contract, with local tests proving default non-authoritative behavior before enabling authoritative warnings.

## Tasks / Subtasks

- [x] Task 0 - Preflight the deferred entry and current implementation (AC: 1-12)
  - [x] Confirm `Story-9.3-ProjectionRegistryCrossCheck` exists in `_bmad-output/implementation-artifacts/deferred-work.md` and is still carried-forward from Story 15.5.
  - [x] Read `HandlerMismatchDetector.cs`, `HandlerRegistryService.cs`, `HandlerMismatchDetectorTests.cs`, `HandlerRegistryServiceTests.cs`, `HandlerMismatchReport.cs`, `HandlerRegistrationSnapshot.cs`, `HandlersMismatchesCommand.cs`, `MemoriesClient.cs`, and `docs/dev/eventstore-integration.md` before editing.
  - [x] Inspect EventStore client discovery in the submodule (`DiscoveryResult`, `DiscoveredDomain`, `EventStoreServiceCollectionExtensions`, `IEventStoreProjection`) as reference only; do not modify submodule files unless the maintainer explicitly expands scope.

- [x] Task 1 - Define the projection binding contract (AC: 1, 2, 6, 8, 9, 11, 12)
  - [x] Add a small repository-owned contract for runtime projection bindings, likely under `src/Hexalith.Memories.EventStore` if the boundary belongs to EventStore integration, or under `src/Hexalith.Memories.Server/Handlers` if the binding source is intentionally server-local.
  - [x] Include enough shape for the detector to answer: tenant id, normalized source prefix or aggregate type, projection type/name or id, supported event/aggregate patterns, and whether the provider is authoritative for the returned tenant/application boundary.
  - [x] Define the canonical comparison key and normalization rules for route source prefix, aggregate token, event pattern, casing, slash trimming, wildcard/prefix matching, and event-version suffix handling before adding detector logic.
  - [x] Provide a default implementation with an explicit unknown or non-authoritative posture so deployments without projection bindings do not receive false warnings by default.
  - [x] Add at least one concrete adopter-facing example showing a `SourceToTenantMap` entry and the matching projection binding shape.
  - [x] If reusing EventStore `DiscoveryResult.Projections` is viable without new dependency churn, add an adapter. If not, document the reason in dev notes or operations docs.
  - [x] Define sanitized projection identity fields and explicitly exclude secrets, raw endpoints, DI container internals, and cross-tenant binding inventories from serialized diagnostics.

- [x] Task 2 - Wire the registry into handler mismatch detection (AC: 3, 4, 6, 8-12)
  - [x] Inject the projection binding provider into `HandlerMismatchDetector` without breaking existing constructor validation and tests.
  - [x] Update `Program.cs` service registration and every affected test builder when constructor dependencies change; use `ArgumentNullException.ThrowIfNull` and default-safe test fakes so warnings-as-errors catches missing setup.
  - [x] Add the configured-but-unbound projection check after routing entries are resolved and before telemetry emission, preserving existing stale, unhandled, and version-mismatch behavior.
  - [x] Emit configured-but-unbound warnings only when the projection binding provider reports an authoritative registry for the relevant tenant/application boundary.
  - [x] Treat provider exceptions, unavailable discovery, and partial results as unavailable/unknown posture unless the implementation adds a separately tested additive diagnostic for provider failure.
  - [x] Decide whether the new mismatch is a new `HandlerMismatchCategory` value or a clearly documented use of an existing category. If a new category is added, update JSON serialization, CLI formatting, tests, and docs in the same story.
  - [x] State the behavior for the reverse direction, runtime projection binding exists but no route is configured. Either leave it as no new mismatch in this story or add a separate additive diagnostic with severity and tests.
  - [x] State the behavior for duplicate, overlapping, or ambiguous projection bindings and keep output ordering deterministic for CLI, JSON, and tests.
  - [x] Ensure the detector's summary remains useful and does not count projection registry absence as observed event data.

- [x] Task 3 - Update registry/listing surfaces only where needed (AC: 5, 6)
  - [x] Update `HandlerRegistryService` only if the handler list needs to expose projection-binding status; otherwise leave list output unchanged and keep the story scoped to mismatches.
  - [x] If the REST contract changes, update `MemoriesJsonContext`, `MemoriesClient`, and consumer-driven contract tests with nullable/defaultable additive members; do not rename existing JSON fields.
  - [x] Preserve CLI behavior: JSON output remains unfiltered, `--severity` still filters by severity only, `--exclude-stale` suppresses only `StaleHandler`, and no new required top-level JSON field is introduced.
  - [x] If warning text changes, keep it stable enough for tests to assert the route/event key, projection binding key, affected tenant scope, and remediation text without depending on incidental prose.

- [x] Task 4 - Add focused tests (AC: 3-6, 8-12)
  - [x] Add `HandlerMismatchDetectorTests` coverage for route configured + no projection binding -> warning mismatch.
  - [x] Add `HandlerMismatchDetectorTests` coverage for route configured + matching projection binding -> no projection-binding mismatch.
  - [x] Add `HandlerMismatchDetectorTests` coverage for route configured + binding in another tenant -> warning mismatch for the requested tenant.
  - [x] Add `HandlerMismatchDetectorTests` coverage for no configured route + projection binding -> no projection-binding warning unless a separate additive reverse-direction diagnostic is explicitly added.
  - [x] Add absence-posture tests proving an empty/default registry does not create noisy warnings unless explicitly configured as authoritative.
  - [x] Add provider-failure and partial-discovery tests proving unavailable/unknown registry state does not emit configured-but-unbound warnings or suppress existing diagnostics.
  - [x] Add tenant-boundary tests proving cross-tenant binding data is not leaked into snapshots, logs, CLI output, or JSON beyond the selected diagnostic's sanitized tenant key.
  - [x] Add regression tests proving existing `UnhandledEventType`, `StaleHandler`, and `VersionMismatch` diagnostics still emit under current fixtures and still coexist with the new projection-binding warning when both conditions apply.
  - [x] Add normalization tests for casing, slash trimming, route prefixes, duplicate and overlapping bindings, wildcard/pattern coverage, deterministic ordering, and event-version suffix handling according to the comparison-key rules.
  - [x] Add contract/CLI tests if any HXL002 enum or serialized shape changes, including camelCase enum round trips, human/table rendering, severity filtering, and REST client contract coverage.

- [x] Task 5 - Update documentation and deferred-work disposition (AC: 2, 5, 7)
  - [x] Update `docs/dev/eventstore-integration.md` section 11 to explain the projection-registry cross-check and operator next steps for configured-and-bound, configured-but-unbound, registry-absent/unknown, and tenant-mismatched binding states.
  - [x] Document that a matching registry binding proves declared binding coverage only and is not a projection liveness, lag, replay, or health signal.
  - [x] Update `docs/dev/telemetry.md` only if new metrics or categories affect telemetry guidance.
  - [x] Update `_bmad-output/implementation-artifacts/deferred-work.md` for `Story-9.3-ProjectionRegistryCrossCheck` with the final disposition and evidence.
  - [x] If the deferred entry is not fully resolved, carry it forward with the precise remaining gap, such as auto-discovery enrichment, authoritative registry detection outside the host boundary, or projection metadata breadth.
  - [x] Add completion notes and file list to this story.

- [x] Task 6 - Validate (AC: 3-10)
  - [x] Run `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~HandlerMismatchDetectorTests|FullyQualifiedName~HandlerRegistryServiceTests"`.
  - [x] If contracts or CLI output changed, run `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~HandlersMismatchesCommandTests|FullyQualifiedName~MemoriesClientHandlersContractTests"`.
  - [x] If deferred-work structured fields changed, run `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"`.
  - [x] Run `git diff --check`.

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
- 2026-05-20: Red phase added failing `HandlerMismatchDetectorTests` for authoritative missing bindings, matching bindings, tenant-boundary safety, registry posture, provider failures, diagnostic coexistence, and normalization.
- 2026-05-20: Green phase added a Memories-owned `IProjectionBindingProvider` contract, default `Unknown` provider, deterministic route/projection matcher, additive `ProjectionBindingMissing` category, CLI/contract tests, docs, and deferred-work resolution.
- 2026-05-20: Broad `dotnet test .\Hexalith.Memories.slnx` reached unit projects cleanly but failed Docker/Aspire-dependent integration and benchmark fixtures because the local Docker runtime was unhealthy; non-container solution verification passed.

### Completion Notes List

- Ready-for-dev story created on 2026-05-19.
- Scope is limited to projection-binding registry design, detector proof, focused HXL002/CLI/server tests, docs, and deferred-work disposition.
- Do not mutate submodules or import the entire deferred-work backlog into Epic 16.
- Implemented a repository-owned projection binding registry contract in `Hexalith.Memories.EventStore` with explicit `Unknown`, `NonAuthoritative`, `Authoritative`, and `Unavailable` postures.
- Wired `HandlerMismatchDetector` to emit `ProjectionBindingMissing` only for authoritative tenant-scoped snapshots and to degrade provider errors without suppressing existing `UnhandledEventType`, `StaleHandler`, or `VersionMismatch` diagnostics.
- Centralized deterministic matching for tenant/source/event keys, including casing, slash normalization, aggregate token extraction, duplicate collapse, wildcard/prefix coverage, and event-version suffix stripping.
- Updated HXL002 enum serialization/CLI coverage, operator docs, and deferred-work disposition for `Story-9.3-ProjectionRegistryCrossCheck`.
- Validation passed for focused server/CLI lanes, deferred-work parser, `git diff --check`, and non-container solution tests. Full solution tests are blocked by local Docker runtime health for integration/benchmark fixtures.

### File List

- `_bmad-output/implementation-artifacts/16-1-projection-registry-cross-check-design.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/dev/eventstore-integration.md`
- `docs/dev/telemetry.md` (added by review patch)
- `src/Hexalith.Memories.Contracts/V1/HandlerMismatchReport.cs`
- `src/Hexalith.Memories.EventStore/DefaultProjectionBindingProvider.cs`
- `src/Hexalith.Memories.EventStore/EventStoreIntegrationLog.cs` (added by review patch — new events 9150-9152)
- `src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.EventStore/IProjectionBindingProvider.cs`
- `src/Hexalith.Memories.EventStore/ProjectionBinding.cs`
- `src/Hexalith.Memories.EventStore/ProjectionBindingRegistryAuthority.cs`
- `src/Hexalith.Memories.EventStore/ProjectionBindingSnapshot.cs`
- `src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs`
- `src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/HandlersMismatchesCommandTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/MemoriesClientHandlersContractTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DefaultProjectionBindingProviderTests.cs` (new — added by review patch)
- `tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs`

## Change Log

- 2026-05-19: Created ready-for-dev story artifact for Projection Registry Cross-Check Design.
- 2026-05-20: Party-mode review applied story hardening for projection-binding authority, canonical comparison keys, additive HXL002 compatibility, non-warning registry states, operator diagnostics, and regression test coverage.
- 2026-05-20: Advanced elicitation applied story hardening for tenant-boundary safety, deterministic matching, provider failure posture, duplicate binding behavior, and liveness non-goals.
- 2026-05-20: Implemented projection registry cross-check and marked story ready for review.
- 2026-05-20: Code review (bmad-code-review) — Acceptance Auditor 12/12 ACs satisfied. Triage produced 2 decisions (resolved), 14 patches (applied), 8 defers (recorded in `deferred-work.md` as 16.1-CR1..CR8), 5 dismissed. Focused validation: 25 detector + 8 CLI + 4 EventStore tests pass. Broader validation: 1839 Server.Tests pass (1 skipped, intentional submodule guard); 88 EventStore.Tests pass. Status → done.

## Party-Mode Review

- Date: 2026-05-20T11:25:32.1932091+02:00
- Selected story key: `16-1-projection-registry-cross-check-design`
- Command/skill invocation used: `/bmad-party-mode 16-1-projection-registry-cross-check-design; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - Runtime-bound projection registry authority was underspecified; the story needed explicit default, non-authoritative, unavailable, and authoritative states so deployments without registry data do not produce false warnings.
  - The canonical comparison key between `SourceToTenantMap` routes and projection bindings needed to define tenant scope, normalized route/aggregate/event patterns, casing, slash, prefix, wildcard, and version behavior.
  - HXL002 compatibility needed tighter guardrails so new diagnostics remain additive, default-safe, and do not alter CLI severity filtering, JSON shape, REST client behavior, or existing mismatch semantics.
  - Operator-facing warning payloads and docs needed concrete configured-and-bound, configured-but-unbound, registry-absent/unknown, and tenant-mismatched examples.
  - Tests needed a matrix that preserves existing `UnhandledEventType`, `StaleHandler`, and `VersionMismatch` behavior while proving tenant isolation, authority posture, reverse-direction behavior, and normalization cases.
- Changes applied:
  - Added acceptance criteria for canonical comparison keys, authoritative-registry warning posture, and actionable diagnostic payload behavior.
  - Added `## Party-Mode Hardening Clarifications` covering runtime registry source-of-truth, `DiscoveryResult.Projections` adapter limits, non-warning states, independent mismatch categories, additive contracts, non-goals, and operator-doc examples.
  - Tightened Task 1 with projection-binding authority, comparison-key normalization, default posture, and adopter-facing example requirements.
  - Tightened Task 2 with DI/test-builder impact, authoritative-only warning behavior, reverse-direction decision handling, and existing diagnostic preservation.
  - Tightened Task 3 with nullable/defaultable HXL002 serialization guidance, CLI filtering preservation, no required top-level JSON additions, and stable warning payload expectations.
  - Tightened Task 4 with tenant isolation, no-route + binding, existing diagnostic regression, normalization, enum/serializer, formatter, and REST client contract tests.
  - Tightened Task 5 with operator-state documentation and explicit carry-forward rules if `Story-9.3-ProjectionRegistryCrossCheck` is not fully resolved.
- Findings deferred:
  - None. The review findings were resolved as story-scope clarifications without adding implementation scope, mutating architecture policy, or changing cross-story contracts.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date: 2026-05-20T11:58:56.2860774+02:00
- Selected story key: `16-1-projection-registry-cross-check-design`
- Command/skill invocation used: `/bmad-advanced-elicitation 16-1-projection-registry-cross-check-design`
- Batch 1 method names:
  - Red Team vs Blue Team
  - Security Audit Personas
  - Failure Mode Analysis
  - Self-Consistency Validation
  - Tree of Thoughts
- Reshuffled Batch 2 method names:
  - First Principles Analysis
  - Pre-mortem Analysis
  - Architecture Decision Records
  - Challenge from Critical Perspective
  - Comparative Analysis Matrix
- Findings summary:
  - Tenant scope needed to be treated as a trust boundary so registry queries, warnings, snapshots, logs, and CLI output cannot leak unrelated tenant binding inventories or sensitive implementation details.
  - The matcher needed one deterministic normalization path for source prefixes, aggregate/event patterns, version suffixes, wildcard behavior, duplicates, ordering, and ambiguous bindings.
  - Provider exceptions, unavailable discovery, and partial registry data needed an explicit unknown/unavailable posture that avoids false configured-but-unbound warnings and does not suppress existing mismatch diagnostics.
  - The story needed to avoid overclaiming: a matching projection binding proves declared route-to-binding coverage only, not runtime projection liveness, catch-up, lag, or health.
  - Tests and docs needed direct evidence for tenant isolation, sanitized diagnostics, provider-failure degradation, duplicate/overlapping binding behavior, and liveness non-goals.
- Changes applied:
  - Added acceptance criteria for tenant-scope data handling and the binding-vs-liveness boundary.
  - Added `## Advanced Elicitation Hardening Clarifications` covering tenant-boundary safety, centralized deterministic matching, duplicate/ambiguous binding behavior, provider failure posture, liveness non-goals, and narrow authority/adaptor escape hatches.
  - Expanded Tasks 1 and 2 with sanitized identity requirements, provider exception/unavailable behavior, duplicate/overlapping binding decisions, and deterministic output ordering.
  - Expanded Task 4 with provider-failure, partial-discovery, tenant-boundary, duplicate/overlapping binding, and deterministic-ordering test requirements.
  - Expanded Task 5 with documentation that route-to-binding coverage is not projection liveness, replay, lag, or health evidence.
- Findings deferred:
  - Exact HXL002 category naming remains an implementation decision within this story's additive-contract guardrails.
  - Whether an EventStore `DiscoveryResult.Projections` adapter is viable remains an implementation decision after inspecting the local submodule surface; the repository-owned contract remains the required boundary.
- Final recommendation: ready-for-dev

## Story Completion Status

Story context created and ready for development. The developer has the active deferred ID, source targets, design guardrails, expected tests, and scope boundaries needed to implement the projection registry cross-check without reopening unrelated Story 9.3 work.

### Review Findings

Code review run: 2026-05-20, against commit `b7d3a2a` (`feat: Add projection binding provider and related classes`). Reviewers: Blind Hunter (diff-only), Edge Case Hunter (diff + repo), Acceptance Auditor (diff + spec + docs). Acceptance Auditor: **12/12 acceptance criteria satisfied**.

Decision-needed (resolved 2026-05-20 — "follow best practices"):

- [x] [Review][Decision] Aggregate-only OR-match — **Resolved**: keep OR-match between source prefix and aggregate token when only one is declared, AND added `.`→`/` source normalization so the most common false-cover case (notation difference between dot-style routes and slash-style bindings) is eliminated upstream. Documented breadth in `docs/dev/eventstore-integration.md` §11.4.1.
- [x] [Review][Decision] Binding-side `V<digits>` suffix stripped — **Resolved**: keep current version-agnostic matching; `VersionMismatch` is the dedicated diagnostic for concurrent-version drift. Documented the trade-off explicitly in §11.4.1 with a follow-up trigger for version-pinned binding semantics if operators report surprise.

Patch (applied 2026-05-20):

- [x] [Review][Patch] Provider failure swallowed silently → emits log event 9150 with exception type name [`HandlerMismatchDetector.cs`]
- [x] [Review][Patch] Null `snapshot.Bindings` guard → emits log event 9152 (treated as Unavailable) [`HandlerMismatchDetector.cs`]
- [x] [Review][Patch] Snapshot `TenantId` mismatch → emits log event 9151 [`HandlerMismatchDetector.cs`]
- [x] [Review][Patch] Generalized cancellation re-throw to `catch (Exception) when (cancellationToken.IsCancellationRequested)` (handles `AggregateException(OCE)`, `ObjectDisposedException`, `TaskCanceledException`) [`HandlerMismatchDetector.cs`]
- [x] [Review][Patch] `binding.TenantId` trim before compare [`ProjectionBindingMatcher.cs`]
- [x] [Review][Patch] `SupportedEventTypePatterns` null-coalesce before `.Count` [`ProjectionBindingMatcher.cs`]
- [x] [Review][Patch] Null entries in `snapshot.Bindings` filtered out in `IsCovered` [`ProjectionBindingMatcher.cs`]
- [x] [Review][Patch] F5: `NormalizeSource` now converts `.` and `\` to `/` so dot-style and slash-style routes canonicalize identically [`ProjectionBindingMatcher.cs`]
- [x] [Review][Patch] Non-trailing wildcards documented as unsupported in `docs/dev/eventstore-integration.md` §11.4.1
- [x] [Review][Patch] `docs/dev/telemetry.md` updated to list `ProjectionBindingMissing` as the 4th mismatch category, with log-event bank 9150-9159 reserved for Story 16.1 Warnings
- [x] [Review][Patch] `docs/dev/eventstore-integration.md` §11.4.1 documents the "no observations yet" `DefaultIfEmpty("*")` semantics and the behavioral-cliff guidance
- [x] [Review][Patch] CLI test `Human_Format_ExcludeStaleDoesNotSuppressProjectionBindingMissing` added [`HandlersMismatchesCommandTests.cs`]
- [x] [Review][Patch] CLI test asserts the kebab-cased runbook URL `handler-projection-binding-missing` in human output [`HandlersMismatchesCommandTests.cs`]
- [x] [Review][Patch] Detector test `ProjectionBindingMissing_MultipleRoutes_AreEmittedInDeterministicOrder` added [`HandlerMismatchDetectorTests.cs`]
- [x] [Review][Patch] New test class `DefaultProjectionBindingProviderTests` (4 tests) [`tests/Hexalith.Memories.EventStore.Tests/`]
- [x] [Review][Patch] New tests for provider failure / tenant mismatch / null Bindings / null entry / cancellation re-throw / dot-vs-slash source equivalence (6 added) [`HandlerMismatchDetectorTests.cs`]

Defer (real but post-MVP / not caused by this change / low risk):

- [x] [Review][Defer] Whitespace-only entry in `SupportedEventTypePatterns` silently promoted to wildcard `*` — operator-error edge; document or sweep later [src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:137-140]
- [x] [Review][Defer] Trailing `.` or `/` in event names/source prefixes not trimmed before terminal-segment split [src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:155-159, :183-187]
- [x] [Review][Defer] Embedded `\` in event names not normalized to `.` or `/` [src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:135]
- [x] [Review][Defer] Turkish-I / Unicode invariant casing produces non-byte-equal forms across tenants/routes [src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:132,167]
- [x] [Review][Defer] Bare `V2` input yields empty event key → `tenant/source//` double-slash comparison key (cosmetic) [src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:161]
- [x] [Review][Defer] Tenant-leakage telemetry/log payload assertion (currently only asserts Context/Suggestion text) [tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs]
- [x] [Review][Defer] Multi-slash collapse (`while (... "//")`) has no direct test [src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:127-130]
- [x] [Review][Defer] Wildcard suffix matching test depth — current tests pass via exact-match coincidence, not via `*` honoring [src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:108-111]

Dismissed (5):

- `SourceToTenantMap` not filtered by tenant (Blind Hunter): disproven — `HandlerMismatchDetector.cs:88-90` already filters before passing to matcher.
- O(routes × bindings) matcher perf: premature optimization; not a correctness bug.
- Inconsistent `Ordinal` / `OrdinalIgnoreCase` comparer mix: works correctly because all sides pass through `ToLowerInvariant`; refactor risk only.
- `DefaultProjectionBindingProvider` synchronous-throw on null tenant: standard .NET argument-validation contract.
- `ConfigureAwait(false)` SC test: project-wide rule already enforces.
