# Epic 24 Context: Observability & Performance Hardening

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

This epic hardens Hexalith.Memories for realistic shared-tenant operation: ingestion becomes traceable across asynchronous workflow boundaries, read paths stop paying avoidable auxiliary round trips, tenant-list refreshes are bounded, tenant isolation follows a structural enforcement target with scalable verification, emitted metrics become consumable through committed dashboards, and hot paths stop amplifying writes or unbounded state growth. The work matters because operational trust depends on proving tenant safety, diagnosing asynchronous work, and keeping latency and memory predictable under load.

## Stories

- Story 24.1: Trace Propagation Across the Workflow Boundary
- Story 24.2: Read-Path Caching & Tenant-List Bounding
- Story 24.3: Physical Tenant Isolation & Verifier Scaling
- Story 24.4: Metric Naming & Committed Dashboards
- Story 24.5: Hot-Path Write-Amplification Cleanup

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
- Hot paths must not write durable state merely to answer reads. Activity streams, replay detection, and retry queues must be bounded or keyed so latency and memory stay predictable under load.

## Technical Decisions

- Dapr remains the required orchestration substrate for workflows, activities, actors, state, service invocation, pub/sub, and secrets. Workflow orchestrators must stay replay-safe, and side effects belong in activities.
- Dapr actors own per-tenant stateful singletons such as rate limits and corpus statistics. Actor identity and telemetry must remain tenant-aware so multiple actor responsibilities for the same tenant stay distinguishable, but read-only paths should not create actor-state writes.
- .NET Aspire provides the local orchestration and observability loop. Dashboard work should fit the existing Aspire/OpenTelemetry path; Grafana or Aspire dashboards are acceptable when committed and reproducible from the repo.
- Redis Stack remains the concrete starting backend for RediSearch, Redis Vector, Dapr state, actors, workflows, and pub/sub. Backend-specific query construction should stay behind the existing Redis/FalkorDB boundaries instead of leaking into domain logic.
- The planned Redis physical-isolation target is per-tenant ACL users combined with tenant-scoped backend resolution. Per-tenant RediSearch, raw vector, and natural-language vector indexes remain tenant lifecycle resources created and deleted by tenant workflows.
- FalkorDB tenant isolation must be database-level rather than label-level. Its process-level memory sharing means database separation does not imply memory isolation, so verifier and dashboard evidence must distinguish data isolation from resource isolation.
- Tenant provisioning and deletion are multi-backend workflow operations with rollback/progress expectations. Isolation hardening must remain compatible with tenant lifecycle workflows rather than creating indexes or databases opportunistically from read/search paths.
- Tenant and case identifiers must stay explicit across workflows, storage, search, CLI, MCP, REST, telemetry, and future UI contracts. External-facing failures should use structured, actionable error semantics.
- Hot-path cleanup should preserve EventStore-as-domain-source and rebuildable Redis/FalkorDB projection assumptions; direct Redis/FalkorDB state should not become a new authority for domain truth.

## UX & Interaction Patterns

- Operator surfaces should organize trace, health, ingestion, tenant isolation, search quality, and backend degradation around decisions and recovery actions, not as an unrelated wall of metrics.
- Tenant verification should produce an operator evidence packet: verified state with timestamp and audit context when healthy; issue classification, affected scope, severity, and next action when unhealthy.
- Degraded backend, queue backlog, stale data, pending repair, or critical isolation risk feedback must answer what happened, what is affected, how serious it is, and what to do next.
- Trust-critical status must stay attached to the affected tenant, workflow, evidence packet, or dashboard object. CLI, MCP, and future web UI surfaces should use the same evidence semantics even when presentation density differs.
- Status patterns must be accessible and explicit: do not rely on color alone, and keep detailed diagnostics available through inspection/expansion while preserving the primary recovery command.

## Cross-Story Dependencies

- Story 24.3 is the decision anchor for physical isolation and scalable verifier evidence. Enforcement work should follow the ratified per-tenant Redis ACL and tenant-scoped backend-resolution target, and should not proceed on assumptions that contradict the architecture.
- Trace propagation, metric naming, and dashboard work should share correlation and tag conventions so traces, logs, metrics, and dashboards can be joined during incident analysis.
- Read-path caching and hot-path write-amplification cleanup both affect tenant status, corpus statistics, activity visibility, and dashboard freshness. Cache invalidation, counters, stream bounds, and retry-queue keys need coordinated semantics.
- Physical isolation hardening depends on existing tenant provisioning, deletion, authorization, and context-enforcement foundations, and must remain compatible with the EventStore/projection consistency model.
