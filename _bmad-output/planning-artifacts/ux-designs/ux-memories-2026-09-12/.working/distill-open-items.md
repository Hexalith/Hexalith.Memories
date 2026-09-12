# Distillation Open Items

These items remain deliberately unresolved after the 2026-09-12 first distillation. They are not filled with inferred values in <code>DESIGN.md</code> or <code>EXPERIENCE.md</code>.

## Product and UX decisions

| Item | Current safe contract | Resolution owner or gate |
|---|---|---|
| Default trust detail versus <code>--explain</code> | Keep compact scope, source/origin, relevance caveat, freshness/degradation, omission, and recovery distinct from opt-in ranks, weights, matched terms, and graph diagnostics. | PRD Open Question 3; UX decision through change control before Epic 17 web activation. |
| Hybrid search start node — closed | Public search has no start-node input and auto-seeds its graph contribution; explicit starts belong to CLI <code>traverse</code> or MCP <code>traverse_relations</code>. Contracts.V1 has no start-provenance field. | Closed by the pinned PRD; presentation may name invocation-derived context but must not invent a wire field. |
| Future product host | The current RCL/specimen proves component conformance only. Routes, application authorization lifecycle, live retrieval/dispatch, notifications, focus, page measure, and activated IA do not exist. | Phase 2 product-host activation. |
| Phase 3 IA | Memory Explorer/Timeline and backend migration remain horizon labels. Only J6’s assessment → dry run → confirmation → progress → cutover/rollback contract is preserved. | Phase 3 discovery/change control. |
| Visual references and brand delta | No mockup, wireframe, import, logo, product color, type, shape, spacing, or elevation delta is selected. The spines inherit FrontComposer/Fluent 2 and keep the historical HTML non-normative. | Optional later UX direction work; not required for this draft. |

## Versioned protocol and implementation reconciliation

| Item | Current evidence | Safe spine posture |
|---|---|---|
| MCP ingest provenance | <code>ingest_content</code> exposes caller-controlled <code>ingestedBy</code>; FR65 requires server-derived normalized external <code>sub</code> or allowlist/grant-derived <code>system:*</code>. | Name the current field only as implementation debt; desired behavior remains fail-closed and server-derived. |
| MCP ingest source/content behavior | The schema advertises File/URL/Event and base64-capable content; current execution accepts File only and UTF-8 encodes the literal string. | Do not promise URL/Event or base64 behavior from MCP until implementation/schema agree. |
| Evidence Packet success coverage | Current <code>search_memory</code> success attaches Evidence Packet; ingest, traversal, and case-info success return their own profiles. Errors may attach a packet. | Preserve shared meanings without claiming a universal populated envelope. |
| Evidence Packet population | Current search mapping leaves freshness, per-source ingestion, MCP schema, and graph summary metadata unpopulated and can omit NL contribution evidence. | Show these fields as unavailable when absent; never synthesize values. |
| Freshness vocabulary | PRD requires <code>current/aging/stale/unknown</code>; current code has <code>unknown/current/stale/expired/pending</code>. Thresholds are not versioned/activated. | Use PRD human terms; defer wire reconciliation and thresholds to Contracts.V1/NFR33. |
| Score semantics | PRD requires normalized single-axis semantics and hybrid RRF contributions; current mapping treats some raw single-axis scores as unbounded/unknown. | Never derive factual or generic confidence from current raw values; contract/implementation needs reconciliation. |
| Case-info breadth | Tool description mentions member count and recent activity, but <code>Case</code> contains neither a member count nor richer activity beyond <code>lastUpdated</code>. | Spine maps only the real <code>Case</code> fields. |
| MCP parameter drift | PRD narratives contain <code>axis</code>/<code>axes</code>, snake_case examples, and graph-in-search language; current tool is <code>axes</code> with graph traversal delegated to <code>traverse_relations</code>. | Current implementation names are recorded as delivery facts; PRD owns future product behavior. |
| CLI 401 recovery variable | Current help/configuration correctly uses <code>HEXALITH_MEMORIES_API_TOKEN</code>, but the shipped HTTP 401 suggestion names nonexistent <code>HEXALITH_MEMORIES_TOKEN</code>. | UX guidance uses the correct name. Delivery needs a focused implementation copy change and a test that resolves the suggested variable. |
| Destructive-dialog allowlist comment | The current conformance allowlist comment says the dialog body owns focus trap/return, while the actual contract requires host launch through the shell Fluent service/provider lifecycle. | The spine carries the safe ownership split. Update the non-normative code comment with the next conformance-maintenance change. |

## Delivery and evidence gates

| Item | Current status | Required closure before claiming completion |
|---|---|---|
| Phase 1 CLI | <code>ingest</code>, <code>traverse</code>, <code>case</code>, and <code>explore</code> are top-level placeholders; tenant create/delete/verify and status case/failed are absent. G3 is blocked. | Real registered verbs, help/examples, stable output semantics, and timed clean-machine G3 evidence. |
| Hybrid graph contract | FR17 graph auto-seeding and FR34 case-partitioned tenant-wide merge are partial. | Contract-compatible implementation and current-wording cross-surface evidence. |
| Current-revision ingestion completion | FR6/FR13 and authoritative replay are partial; stale/config-incompatible and two-of-three acknowledgements must not complete. | Replay/rebuild evidence against current schema/config plus non-resurrection checks. |
| Tenant authority and provenance | FR44 hardening and FR65 server-derived provenance are incomplete under the September 12 wording. | Principal-driven cross-tenant, internal app allowlist/grant, protected-channel, and provenance tests. |
| Verified erasure | FR39 and erased-tenant replay/restore safety are incomplete. | Projection purge, irreversible EventStore inaccessibility, telemetry handoff, quarantine, ID non-reuse, and non-resurrection evidence. |
| Durable idempotency | FR75 authoritative duplicate suppression is partial. | Same command/event identity produces one durable mutation, one projection outcome, and one operation receipt/progress identity. |
| Fairness, readiness, degradation, and telemetry | FR8/FR66/FR72 and NFR13/NFR18/NFR22/NFR34 require current-wording verification. | Noisy-neighbor/admission tests, durable Retry-After, capability-aware readiness/degradation, and production-admission/runtime telemetry evidence. |
| Active CLI accessibility | NFR37 is not started and current-contract evidence is absent; shipped export/cancellation wire shapes are explicit exceptions. | Narrow-terminal, redirected-output, no-color, duplication, timeout/cancellation, secret-sanitization, semantic-parity automation, and a keyboard-only manual walkthrough with dated owner/disposition. |
| Phase 1 release governance | Current posture is no-go; G1–G6 are hard and the retained 2026-12-01 decision date is an assumption. G6 lacks successor-story ownership, ratified AD-14 phasing, and FR75/NFR37 architecture binding. | Current evidence or phase-specific approved exceptions, owners, and tracking entries for every G6 gap before any release decision. |
| Phase 1.5 launch | MCP/EventStore code exists, but L1–L3 have not run and the L1 reference sample is missing. | Dated gate evidence; do not claim launch on implementation alone. |
| Future web accessibility/performance | Component tests exist, but NFR32/NFR35 are inactive and route-level evidence is absent. | Activated host plus dated responsive, keyboard, focus, screen-reader, forced-colors, browser/AT, payload, CLS, and performance matrix. |

## Source notes

- The live [2026-09-12 PRD](../../../prd.md) is authoritative; the refreshed [PRD extraction](extract-prd.md) records its deltas.
- Exact shipped schemas/grammar and divergences come from [protocol implementation extraction](extract-protocol-implementation.md).
- Exact component ownership and conformance boundaries come from [UI implementation extraction](extract-ui-implementation.md).
- Remediation provenance and remaining verification expectations come from [validation extraction](extract-validation.md).
- Canonical migration decisions are recorded in [the memory log](../.memlog.md); this ledger does not replace it.
