# Epic 24 Context: Observability & Performance Hardening

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Harden shared-tenant operation by making asynchronous ingestion traceable end to end, removing avoidable read round trips and hot-path writes, bounding tenant-wide work, turning emitted metrics into usable dashboards, and advancing tenant isolation toward structural enforcement with scalable, accurately classified evidence. This matters because operators need predictable performance and proof they can trust when diagnosing workflow failures or tenant-safety risks.

## Stories

- Story 24.1: Trace Propagation Across the Workflow Boundary
- Story 24.2: Read-Path Caching & Tenant-List Bounding
- Story 24.3: Physical Tenant Isolation & Verifier Scaling (Decision-First)
- Story 24.4: Metric Naming & Committed Dashboards
- Story 24.5: Hot-Path Write-Amplification Cleanup
- Story 24.6: Graph Content-Level Tenant Isolation Evidence
- Story 24.7: Tenant-Configured Vector Dimension Verification
- Story 24.8: Semantic Isolation Key-Family Classification
- Story 24.9: Non-Destructive Tenant-Marker Diagnostics

## Requirements & Constraints

- Epic 24 is part of the architecture-audit remediation wave. Before creating or implementing any story in this epic, re-verify the audit anchors and implementation-state assumptions cited by that story against the current repository, record the verification date and any moved or renamed anchors in the story, and update stale story scope before development begins.
- Trace context must survive the durable workflow boundary and connect ingress, orchestration, activities, Dapr service calls, and backend work. Workflow/activity spans must link to the initiating request, Durable Task tracing must be registered, and a distributed trace completeness test must prove the path.
- Structured JSON logs must retain OpenTelemetry correlation context. Custom metrics must expose ingestion throughput, search latency per axis, per-tenant index size, and pipeline queue depth through one canonical naming/tag family and a committed local-development dashboard.
- Tenant status, embedding configuration, and corpus statistics need short-lived, tenant-scoped caches invalidated by relevant writes. Tenant listing must be paged and any actor/backend fan-out bounded so refreshes cannot stampede the system.
- Read paths must not persist state merely to answer reads. Activity streams, replay detection, and retry queues must use bounded or keyed designs that keep latency and memory predictable.
- Zero cross-tenant leakage is a hard gate across ingestion, every search axis, and graph traversal. Automated evidence must cover malformed, empty, swapped, and colliding tenant identifiers; isolation failures fail closed.
- Isolation verification must avoid pairwise deep scans and use cursor or aggregate evidence suitable for tenant growth. Adding nine loaded tenants must not degrade an existing tenant by more than 5% in the defined 10-tenant, 100K-units-per-tenant benchmark.
- Graph database existence is structural evidence only. Content-level proof requires a real tenant A/B fixture with identical graph shapes and identifiers, distinct payload markers, authenticated traversal, and zero foreign nodes or edges.
- Raw and natural-language index dimensions must each match the requested tenant's configured embedding dimension. Configuration lookup fails closed; agreement between two index values is not a substitute for the tenant configuration source of truth.
- Semantic marker scans must classify proven-active key families from canonical namespace provenance and record shape. Staging and legacy records are excluded; opaque colon-bearing identifiers cannot be classified by prefix/suffix shortcuts; ambiguity is an evidence gap.
- Missing and foreign markers on proven-active records both fail closed but remain distinct: missing means incomplete evidence, while foreign means possible contamination. Recovery is exact-key inspection/quarantine followed by tenant-scoped repair or reindex after provenance verification, never blanket prefix deletion.

## Technical Decisions

- Dapr workflows orchestrate sequencing, retry, and compensation; activities perform I/O; actors own per-tenant stateful singletons. Orchestrators remain replay-safe, and read-only actor paths must not create durable writes.
- The Redis physical-isolation target is per-tenant ACL users with tenant-scoped backend resolution. Per-tenant RediSearch/vector indexes remain lifecycle resources; prefixes, hash tags, and logical databases are placement aids, not the security boundary.
- FalkorDB isolation is database-level, not label-level. Shared process memory means database separation proves data isolation, not resource isolation.
- Tenant provisioning and deletion workflows remain the owners of index and graph lifecycle. Verification, search, and read paths inspect and fail clearly; they do not create or repair infrastructure on demand.
- OpenTelemetry is the common trace/log/metric path, with Aspire providing local orchestration and dashboard visibility. Correlation and tag conventions must allow incident investigation across all three signal types.
- Verifier evidence must preserve the ratified distinctions between structural graph evidence, tenant-configured dimensions, collision-safe active-family classification, and non-destructive missing-versus-foreign marker diagnostics. These semantics do not claim that the ACL enforcement target is already implemented.

## UX & Interaction Patterns

- Tenant verification should return an operator evidence packet: verified state and timestamp when healthy; otherwise issue class, affected scope, severity, bounded diagnostics, and the safest next action.
- Isolation failures are critical safety errors, not warnings. Recovery actions that broaden scope, repair data, or expose diagnostics require deliberate confirmation and visible tenant context.
- Operational views should organize traces, health, isolation, queue state, and degradation around decisions and recovery rather than presenting an unrelated wall of metrics.
- CLI, MCP, and web surfaces should share the same evidence and status semantics. Feedback must state what happened, what is affected, severity, and next action, without relying on color alone.

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

- Story 24.3 supplies the physical-isolation decision and scalable-verifier foundation. Full ACL qualification, enforcement, migration, and runtime-evidence candidates are held as unregistered Stories 24.10-24.13 until their activation gates pass.
- Stories 24.6, 24.7, and 24.8 are independent verifier-correctness slices. Story 24.9 follows Story 24.8 because safe marker diagnostics depend on proven-active family classification.
- Isolation work depends on existing tenant provisioning/deletion and authorization/context enforcement. Semantic classification must preserve migration staging state rather than mutate or delete it.
- Any change touching ingestion, workflow inputs, semantic indexing, tenant readiness, or embedding admission must record command-backed verdicts for the applicable Epic 23 invariants; `N/A` requires diff evidence, and blockers require an owner, consequence, proof boundary, and reopen trigger.
