# Epic 24 Context: Observability & Performance Hardening

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

This epic hardens Hexalith.Memories for realistic shared-tenant operation: ingestion becomes traceable across asynchronous workflow boundaries, read paths stop paying avoidable auxiliary round trips, tenant-list refreshes are bounded, tenant isolation follows a structural enforcement target with scalable verification and explicit evidence semantics, emitted metrics become consumable through committed dashboards, and hot paths stop amplifying writes or unbounded state growth. The work matters because operational trust depends on proving tenant safety, diagnosing asynchronous work, and keeping latency and memory predictable under load.

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

- Epic 24 is part of the architecture-audit remediation wave. Before creating or implementing any story in this epic, re-verify the audit anchors and implementation-state assumptions cited by that story against the current repository, record the verification date and any moved or renamed anchors in the story, and update stale story scope before development begins.
- Ingestion tracing must connect the request entry point, Dapr workflow orchestration, workflow activities, service calls, and backend work. Trace context must be carried explicitly across workflow boundaries where automatic propagation is insufficient.
- Distributed trace completeness must be testable end to end. Workflow and activity spans should be linkable to the original ingest request, and Durable Task tracing must be included in the configured trace sources.
- Structured logs and diagnostics must preserve OpenTelemetry correlation context so operators can connect logs, spans, workflow activity, and user-facing failures during incident analysis.
- Custom metrics must cover ingestion throughput, search latency by retrieval axis, per-tenant index size, and pipeline queue depth. Metrics must be visible in local development through a committed dashboard rather than existing only as emitted instruments.
- Metric names and tag keys must converge on one canonical naming family aligned with the existing metric tag-key policy, so dashboards and alerts do not need instrument-specific exceptions.
- Tenant status, embedding configuration, and corpus statistics must avoid repeated read-path backend calls. Caches must be short-lived, tenant-scoped, and invalidated by writes that change the cached values, while preserving the cached and cold search latency goals under concurrent tenant load.
- Tenant listing must support paging and bounded fan-out. Any actor or backend fan-out used for tenant-list refresh must have bounded concurrency to avoid dashboard or operator refresh stampedes.
- Tenant isolation must remain a zero-leak requirement for ingestion, search, and graph traversal, including malformed, missing, swapped, or colliding tenant identifiers.
- Physical tenant isolation is the target posture. The Redis target is per-tenant ACL users resolved through tenant-scoped backend routing; query filters, prefixes, hash tags, or logical Redis databases may help placement/routing, but they are not the primary security boundary.
- Isolation verification must scale beyond pairwise deep scans. Verifier evidence should use cursor-based or aggregate checks so routine verification remains practical as tenant count grows, and should not overstate what process-level shared infrastructure can guarantee.
- Runtime `GraphIsolation` database existence is structural evidence only. NFR8 graph content proof requires a real tenant A/B fixture with identical graph structures, colliding edge identifiers, tenant-distinct markers, and zero foreign traversal results.
- Raw and natural-language semantic index dimensions must each match the requested tenant's configured embedding dimension. Equality between the index families is secondary evidence and cannot substitute for the configuration source of truth.
- Active semantic marker evidence must use collision-safe family classification. Migration staging and legacy nested-NL records are not active evidence, opaque identifiers cannot be classified by reserved-looking prefixes alone, and unresolved provenance is an evidence-classification gap.
- Missing and foreign markers on proven-active records both fail closed but have distinct meaning and recovery: missing is incomplete evidence, foreign is possible contamination, and remediation is named-key inspection/quarantine plus tenant-scoped repair or reindex after provenance verification—never blanket prefix deletion.
- Hot paths must not write durable state merely to answer reads. Activity streams, replay detection, and retry queues must be bounded or keyed so latency and memory stay predictable under load.

## Technical Decisions

- Dapr remains the required orchestration substrate for workflows, activities, actors, state, service invocation, pub/sub, and secrets. Workflow orchestrators must stay replay-safe, and side effects belong in activities.
- Dapr actors own per-tenant stateful singletons such as rate limits and corpus statistics. Actor identity and telemetry must remain tenant-aware so multiple actor responsibilities for the same tenant stay distinguishable, but read-only paths should not create actor-state writes.
- .NET Aspire provides the local orchestration and observability loop. Dashboard work should fit the existing Aspire/OpenTelemetry path; Grafana or Aspire dashboards are acceptable when committed and reproducible from the repo.
- Redis Stack remains the concrete starting backend for RediSearch, Redis Vector, Dapr state, actors, workflows, and pub/sub. Backend-specific query construction should stay behind the existing Redis/FalkorDB boundaries instead of leaking into domain logic.
- The planned Redis physical-isolation target is per-tenant ACL users combined with tenant-scoped backend resolution. Per-tenant RediSearch, raw vector, and natural-language vector indexes remain tenant lifecycle resources created and deleted by tenant workflows.
- D29 separates physical enforcement from verifier evidence: `GRAPH.LIST` is structural only; configured dimensions are authoritative; active semantic families require collision-safe provenance; and missing versus foreign marker outcomes remain fail-closed with distinct non-destructive guidance.
- FalkorDB tenant isolation must be database-level rather than label-level. Its process-level memory sharing means database separation does not imply memory isolation, so verifier and dashboard evidence must distinguish data isolation from resource isolation.
- Tenant provisioning and deletion are multi-backend workflow operations with rollback/progress expectations. Isolation hardening must remain compatible with tenant lifecycle workflows rather than creating indexes or databases opportunistically from read/search paths.
- Tenant and case identifiers must stay explicit across workflows, storage, search, CLI, MCP, REST, telemetry, and future UI contracts. External-facing failures should use structured, actionable error semantics.
- Hot-path cleanup should preserve EventStore-as-domain-source and rebuildable Redis/FalkorDB projection assumptions; direct Redis/FalkorDB state should not become a new authority for domain truth.

## UX & Interaction Patterns

- Operator surfaces should organize trace, health, ingestion, tenant isolation, search quality, and backend degradation around decisions and recovery actions, not as an unrelated wall of metrics.
- Tenant verification should produce an operator evidence packet: verified state with timestamp and audit context when healthy; issue classification, affected scope, severity, and next action when unhealthy.
- Degraded backend, queue backlog, stale data, pending repair, or critical isolation risk feedback must answer what happened, what is affected, how serious it is, and what to do next.
- Trust-critical status must stay attached to the affected tenant, workflow, evidence packet, or dashboard object. CLI, MCP, and future web UI surfaces should use the same evidence semantics even when presentation density differs.
- Tenant-marker evidence must distinguish incomplete evidence from possible contamination and must name a safe next action without implying that a prefix can be deleted wholesale.
- Status patterns must be accessible and explicit: do not rely on color alone, and keep detailed diagnostics available through inspection/expansion while preserving the primary recovery command.

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

- Story 24.3 is the decision anchor for physical isolation and scalable verifier evidence. Enforcement work should follow the ratified per-tenant Redis ACL and tenant-scoped backend-resolution target, and should not proceed on assumptions that contradict the architecture.
- Stories 24.6, 24.7, and 24.8 are independent verifier-correctness slices. Story 24.9 follows Story 24.8 because remediation meaning depends on proven-active family membership.
- The held physical-isolation candidates approved on 2026-08-03 are re-keyed to Stories 24.10-24.13 (qualification, enforcement, migration, runtime evidence). They remain unregistered until their existing gates pass; 24.6-24.9 are the canonical verifier-residual backlog homes.
- Trace propagation, metric naming, and dashboard work should share correlation and tag conventions so traces, logs, metrics, and dashboards can be joined during incident analysis.
- Read-path caching and hot-path write-amplification cleanup both affect tenant status, corpus statistics, activity visibility, and dashboard freshness. Cache invalidation, counters, stream bounds, and retry-queue keys need coordinated semantics.
- Physical isolation hardening depends on existing tenant provisioning, deletion, authorization, and context-enforcement foundations, and must remain compatible with the EventStore/projection consistency model.
