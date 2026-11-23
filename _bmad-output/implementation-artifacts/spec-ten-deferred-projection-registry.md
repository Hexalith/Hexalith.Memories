---
title: 'Resolve ten deferred projection registry findings'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make the projection-binding diagnostic reflect the tenant's configured routes and stored event observations.

- Given a binding pattern containing only whitespace, when the registry checks a route, then that entry grants no coverage. An empty collection, an empty string and explicit `*` retain their documented behavior (DW-450).
- Given equivalent route or event tokens with mixed separators, trailing separators, repeated slashes, or canonically equivalent Unicode spellings, when the matcher builds expectations and checks bindings, then coverage is deterministic across cultures. A version-only event retains a nonempty key, and tenant identity retains its existing ordinal comparison (DW-451–454, DW-456).
- Given an authoritative tenant snapshot containing a foreign binding and a suffix pattern whose prefix differs from the full event name, when diagnostics run, then the local pattern matches only its prefix and mismatch telemetry carries only the requested tenant and its route (DW-455, DW-457).
- Given a missing discovery index with surviving membership markers, when another observation arrives, then the store restores discovery for the marked aggregates within the existing cap. Given a rejected written or discovery index TTL refresh, when the store records an observation, then it retries within its existing CAS budget and uses its existing failure posture on exhaustion (DW-692, DW-693).

</frozen-after-approval>

## Implementation Notes

- Baseline `5ce83727ab62e8476980dc80c8287c3cd19dcc8e` has 326 open numbered and 141 open legacy ledger entries. The bounded change uses existing diagnostics, Dapr state keys, retry policy, and focused tests. The push to `main` was explicitly requested.
- `ProjectionBindingMatcher` owns binding pattern and route normalization. `HandlerMismatchDetectorTests` covers the public diagnostic and can capture structured logging and metrics. The EventStore integration guide and `ProjectionBinding` parameter documentation publish the pattern rules.
- `DaprObservedEventTypeStore` owns observation writes, the capped discovery index, and the uncapped purge index. Reuse its per-tenant keys, first-write options and ETag loop. Validate persisted outcomes with `DaprObservedEventTypeStoreTests` and `DaprStateStoreLiveSidecarTests`. The architecture's ADR-IDA-001 calls for real sidecar evidence for state-store semantics.
- The baseline AppHost reached healthy and stopped cleanly. Fresh Debug builds and direct xUnit runs passed 28 observed-event store cases, 36 mismatch detector cases, and eight live Dapr/Redis cases. The live recovery test deletes the discovery index, verifies both aggregates return after another observation, then proves that both indexes survive past fixture TTLs.
- Closed DW-450 through DW-457, DW-692 and DW-693. The ledger retains 316 open numbered items and 142 open legacy items, including the concurrency follow-up from this review.

## Review Triage Log

- Medium, patched. Bulk marker reads in bounded batches replace serial sidecar requests. The unit fixture places a retained marker after a batch of unmarked keys.
- Medium, patched. The rebuild reads the written index after observing the discovery miss. A unit test simulates another admission immediately before recovery reads the index.
- Medium, patched. Membership renewal now retries a rejected save and reports exhaustion. The conflict test verifies retries with TTL metadata for the written index, discovery index and marker.
- Medium, deferred. The pre-existing written-index compensation can delete another writer's state between the absence check and deletion. The ledger records this separate concurrency change.
- Medium, patched. An invalid Unicode pattern remains a literal and cannot abort the matcher. A focused test supplies an unpaired surrogate at runtime.
- Low, patched. The guide specifies that event-pattern normalization operates within the supplied aggregate. The CloudEvents router's second dotted segment rule remains the contract for assigning the aggregate.
- Medium, patched. A literal observed asterisk and an unobserved route have separate matching behavior. The test verifies both with the same concrete pattern.
- Low, patched. The guide distinguishes route terminal extraction from whole-token normalization of observed and binding aggregate names.
- Low, patched. ADR-IDA-001 now records the TTL refresh and bulk discovery recovery.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `docs/dev/eventstore-integration.md`
- `src/Hexalith.Memories.EventStore/DaprObservedEventTypeStore.cs`
- `src/Hexalith.Memories.EventStore/ProjectionBinding.cs`
- `src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs`
- `src/Hexalith.Memories.Server/Handlers/ProjectionBindingExpectation.cs`
- `src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs`
- `tests/Hexalith.Memories.EventStore.Tests/DaprObservedEventTypeStoreTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/FakeDaprStateStore.cs`
- `tests/Hexalith.Memories.IntegrationTests/EventStore/DaprStateStoreLiveSidecarTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs`
