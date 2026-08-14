# Epic 24 Context: Observability & Performance Hardening

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Harden Hexalith.Memories for realistic multi-tenant operation: end-to-end async ingestion traces, fewer avoidable read round trips, bounded tenant-wide work, one metric family with a committed dashboard, hot-path write-amplification cleanup, and tenant isolation that moves from naming conventions toward D29 structural enforcement with classified evidence. Operators must diagnose failures, prove NFR8 zero leakage, and keep latency predictable as tenants and volume grow.

## Stories

- Story 24.1: Trace Propagation Across the Workflow Boundary
- Story 24.2: Read-Path Caching & Tenant-List Bounding
- Story 24.3: Physical Tenant Isolation & Verifier Scaling
- Story 24.4: Metric Naming & Committed Dashboards
- Story 24.5: Hot-Path Write-Amplification Cleanup
- Story 24.6: Graph Content-Level Tenant Isolation Evidence
- Story 24.7: Tenant-Configured Vector Dimension Verification
- Story 24.8: Semantic Isolation Key-Family Classification
- Story 24.9: Non-Destructive Tenant-Marker Diagnostics

## Requirements & Constraints

- Preserve NFR8 zero cross-tenant leakage across ingestion, every search axis, and graph traversal. Evidence must cover malformed, missing, empty, swapped, and colliding tenant identifiers; graph proof requires identical tenant A/B structures and edge identifiers with distinct payload markers, then authenticated traversal returning no foreign nodes or edges.
- Adding tenants must not degrade an existing tenant by more than 5% at ten tenants with 100K memory units each. Tenant listing and verification must avoid unbounded fan-out and pairwise deep scans.
- Propagate W3C trace context from CLI or MCP through ingress, server, Dapr workflows, activities, and backends. Retain OpenTelemetry correlation in logs and verify trace completeness end to end.
- Export ingestion throughput, per-axis search latency, per-tenant index size, and pipeline queue depth through one naming family and a committed Aspire or Grafana dashboard.
- Front tenant status, embedding configuration, and corpus statistics with short-lived, tenant-scoped caches invalidated by relevant writes. Bound tenant-list paging and concurrency so operator refreshes cannot stampede actors or backends.
- Read paths and background loops must not cause avoidable durable writes or unbounded growth. Activity streams need bounded retention and counters, replay tracking needs an application-owned in-flight set, and retry queues need stable identifiers rather than serialized-value identity.
- Isolation verification must fail closed without mutating tenant data. Structural existence, content leakage, configured-vector compatibility, key-family membership, missing markers, and foreign markers are distinct claims.
- FalkorDB data-vs-resource isolation is a distinct constraint from graph content isolation: a dedicated per-tenant database (not labels) isolates data; `GRAPH.LIST` / lifecycle database existence is resource and structural evidence only; process memory remains shared. Content-leakage claims require the real A/B fixture, not a resource check or a new runtime content scan.
- Stories 24.6-24.9 are the canonical verifier-residual backlog homes.
- Epic-wide audit-anchor preflight: before any story is authored, registered, selected, or implemented, re-verify the audit anchors and inherited claims against current code and record the verification date. This binds later Epic 24 stories.
- Cross-tenant negative-evidence carry-forward: scope-sensitive changes must attach focused negative validation evidence, not historical proof. Cite Story 20.2 denial-before-dependency and Story 24.3 verifier fail-closed/tenant-marker evidence when applicable, or a newer canonical replacement. Happy-path, broad-suite, or build-only evidence cannot close those changes.
- Reviews touching ingestion must retain claim-check, captured workflow config, chunked vectors, source-payload retention, tenant index readiness, and single-operation admission (see checklist). Each verdict needs rerunnable evidence, reviewer, date, and result; blockers need owner, consequence, proof boundary, and reopen trigger.

## Technical Decisions

- D29: Redis physical isolation targets per-tenant ACL users and tenant-scoped backend routing. Prefixes, hash tags, logical databases, and query filters are placement aids, not the primary security boundary. Verifier evidence uses classified structural and cursor checks, not pairwise deep scans. Full ACL provisioning, connection migration, and data migration remain follow-up enforcement.
- Dapr Workflow remains the durable orchestration boundary. Serialize trace context where it cannot cross workflow history, link workflow/activity spans to the initiating request, and register `Microsoft.DurableTask` telemetry. Orchestrators stay replay-safe; activities own side effects.
- Tenant workflows own per-tenant search indexes and FalkorDB databases. `TenantProvisioningWorkflow` remains the sole creation owner; feature and verifier paths must not create-if-missing.
- EventStore remains the domain source of truth; Redis and FalkorDB remain rebuildable projections. Performance cleanup must not make cached or backend read models authoritative domain state.
- Aspire and OpenTelemetry are the common observability path. Trace, log, metric, dashboard, and tenant tags must share stable correlation and naming.
- D29 verifier semantics: `GraphIsolation` is `GRAPH.LIST` database-existence evidence only. Raw and natural-language index dimensions must independently match the requested tenant's configured embedding dimension. Active-marker evidence uses collision-safe classification from canonical namespace provenance and record shape; staging and legacy nested-NL records do not count. A foreign marker means possible contamination; a missing marker means incomplete evidence. Both fail closed without payload disclosure; recover by exact-key inspection or quarantine, then tenant-scoped repair or reindex. Never recommend blanket prefix deletion.

## UX & Interaction Patterns

Tenant verification should return an operator evidence packet: healthy results show verified state, timestamp, and audit context; unhealthy results classify the issue, affected scope, severity, and safest next action. Isolation failures are critical; backend degradation and evidence gaps stay distinct. Feedback must explain what happened, impact, severity, and recovery without relying on color. Diagnostics should be bounded, payload-free, expandable, and consistent across CLI, MCP, dashboards, and future web views.

## Review Checklist — Epic 23 Ingestion Invariants

| Invariant | Required review evidence | Status |
|---|---|---|
| Claim-check workflow payloads | Inspect scheduler/claim-check/workflow inputs and tests; prove raw source bytes and large intermediate values are replaced by scoped references. | Correctively passed 2026-08-02; see the [Epic 24 retrospective addendum](epic-24-retro-2026-07-06.md). |
| Captured workflow configuration | Inspect scheduler capture and orchestrator reads; prove retry/NL settings come from durable workflow input. | Correctively passed 2026-08-02; see the [Epic 24 retrospective addendum](epic-24-retro-2026-07-06.md). |
| Chunked semantic vectors | Inspect chunk key construction/index writes, base-ID parsing, and semantic-result deduplication tests; prove `{tenant}:vec:{memoryUnitId}:{sequence}` while hashes and parsed/results-facing identity retain the base `MemoryUnitId`. | Correctively passed 2026-08-02; see the [Epic 24 retrospective addendum](epic-24-retro-2026-07-06.md). |
| Source-payload retention | Inspect failed-unit persistence/cleanup/re-ingestion and negative tests; prove a retained pointer or actionable rejection. | Correctively passed 2026-08-02; see the [Epic 24 retrospective addendum](epic-24-retro-2026-07-06.md). |
| Tenant index readiness | Inspect all four indexing activities, readiness verification, maintenance adapter, provisioning activities/workflow, and ownership guards; prove no on-demand index creation, fail-closed missing-index behavior, sole provisioning ownership, and only Story 23.7-approved in-place upgrades for known additive TAG fields before readiness is cached. | Correctively passed 2026-08-02; see the [Epic 24 retrospective addendum](epic-24-retro-2026-07-06.md). |
| Single-operation admission | Inspect Story 23.5 ingestion embedding activities, captured configuration, and actor/logic assertions; prove one admission for `GenerateEmbeddingActivity`'s single provider operation and one per bounded batch in `GenerateChunkEmbeddingsActivity`. | Correctively passed 2026-08-02; see the [Epic 24 retrospective addendum](epic-24-retro-2026-07-06.md). |

Every applicable verdict must record a rerunnable evidence command or artifact, reviewer, date, and pass/fail/blocked result. `N/A` requires diff evidence that the reviewed change cannot affect the invariant. The statuses above are dated corrective verification of the current tree, not evidence that this checklist ran during every original Epic 24 story review.

## Cross-Story Dependencies

- Story 24.3 anchors physical isolation and scalable verification; later enforcement follows its ACL and backend-routing target.
- Stories 24.6-24.8 may proceed independently; Story 24.9 follows Story 24.8 because marker diagnostics depend on active-family classification.
- Held physical-isolation qualification, enforcement, migration, and runtime-evidence follow-ups were re-keyed to Stories 24.10-24.13. They remain unregistered until existing activation gates pass; former 24.6-24.9 identities are historical aliases only.
- Trace, metric, and dashboard work share correlation and tag conventions. Read caching and write-amplification cleanup share invalidation, counter, stream-bound, and freshness semantics.
- Isolation work depends on tenant lifecycle ownership, ingress authorization, tenant-context enforcement, and the EventStore/projection model.
