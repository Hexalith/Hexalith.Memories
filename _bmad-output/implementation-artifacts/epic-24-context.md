# Epic 24 Context: Observability & Performance Hardening

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

This epic makes Hexalith.Memories operable under realistic tenant and ingestion load: operators can trace ingestion across asynchronous workflow boundaries, read paths avoid avoidable backend round trips, tenant isolation moves from prefix-based detection toward structural enforcement, metrics become dashboard-ready, and hot paths stop amplifying writes or unbounded state growth. The work matters because tenant safety, traceability, and predictable performance are required before the system can be trusted as shared operational infrastructure.

## Stories

- Story 24.1: Trace Propagation Across the Workflow Boundary
- Story 24.2: Read-Path Caching & Tenant-List Bounding
- Story 24.3: Physical Tenant Isolation & Verifier Scaling
- Story 24.4: Metric Naming & Committed Dashboards
- Story 24.5: Hot-Path Write-Amplification Cleanup

## Requirements & Constraints

- Ingestion traces must remain connected across request entry points, DAPR workflow orchestration, activities, service calls, and backend work. Trace context must be propagated explicitly where async workflow boundaries would otherwise break continuity, and distributed trace completeness must be testable end to end.
- Structured logs must carry OpenTelemetry correlation context so operational diagnostics can connect logs, spans, workflow activity, and user-facing failures.
- Custom metrics must be exported through OpenTelemetry for ingestion throughput, search latency by retrieval axis, per-tenant index size, and pipeline queue depth. The metric set must be consumable in local development through a committed dashboard rather than only emitted by code.
- Metric names and tag keys must follow one canonical family. New or renamed instruments should align with the shared tag-key policy so dashboards and alerts do not need per-instrument exceptions.
- Read-heavy tenant metadata, embedding configuration, and corpus statistics must avoid repeated auxiliary backend round trips. Caches should be short-lived, invalidated by writes that change the underlying values, and safe for tenant-scoped use.
- Tenant listing must be bounded: API responses must support paging, and any fan-out across tenant actors or backends must use bounded concurrency to avoid dashboard refresh stampedes.
- Tenant isolation must satisfy zero cross-tenant leakage for ingestion, search, and graph traversal, including malformed, missing, swapped, or colliding tenant identifiers. Access layers must reject cross-tenant requests with actionable structured errors.
- Physical tenant isolation is the target posture. Query filters or naming prefixes alone are not sufficient as the long-term isolation guarantee because a filter bug must not be able to expose another tenant's data.
- Isolation verification must scale beyond pairwise deep scans. Verification should use cursor-based or aggregate checks so adding tenants does not make routine verification impractical.
- Tenant scaling must preserve existing-tenant performance within the documented target: adding loaded tenants should not materially degrade an existing tenant's benchmarked performance.
- Hot paths must not write state merely to answer reads. Activity streams, replay detection, and retry queues must be bounded or keyed so latency and memory use stay predictable under load.

## Technical Decisions

- DAPR is load-bearing infrastructure for workflows, actors, service invocation, state, pub/sub, and secrets. Workflow state is persisted through the Durable Task model, activities are individually retriable, and long-running ingestion work should remain observable rather than replaced with ad hoc queues.
- DAPR actors are used for per-tenant stateful singletons such as rate limiting and corpus statistics. Actor identity is type-scoped by actor type and tenant identifier so logs and monitoring can distinguish multiple actor responsibilities for the same tenant.
- .NET Aspire provides local orchestration, health checks, and observability defaults. The committed dashboard for this epic should fit that local developer/operator loop and may use Aspire or Grafana as long as the metrics are visible without manual reconstruction.
- Redis Stack remains the concrete starting backend, with RediSearch, Redis Vector, and DAPR state in the MVP topology. Backend portability is protected through identified extraction points and per-tenant backend selection; avoid broad new abstractions unless they are required by the isolation decision.
- FalkorDB tenant isolation must be database-level rather than label-level. Its process-level memory sharing means physical database separation does not by itself provide memory isolation, so verifier and dashboard signals should not imply stronger guarantees than the backend can provide.
- Tenant infrastructure resolution is a planned extension point that maps a tenant to backend connection details for the relevant stores. Multi-instance placement is intended to be configuration-driven when needed, while the current implementation may still route tenants to the same physical instances.
- Tenant provisioning and deletion are multi-backend operations. Isolation changes must respect existing rollback, deletion, and progress-tracking expectations rather than introducing one-store-only state.
- Query construction and backend access should continue to keep Redis/FalkorDB specifics out of domain logic where the architecture already defines boundaries. This epic should harden observability and performance without making future backend migration harder.

## UX & Interaction Patterns

- Operational surfaces should organize health, ingestion, tenant isolation, search quality, and backend degradation around operator decisions and recovery actions, not as an unrelated wall of metrics.
- Tenant verification should produce an operator evidence packet: verified state with timestamp and audit context when healthy; issue classification, affected scope, severity, and next action when unhealthy.
- Feedback for degraded backends, queue backlog, stale data, pending repair, or critical isolation risk must answer what happened, what is affected, how serious it is, and what to do next.
- Trust-critical status should stay attached to the affected tenant, workflow, evidence packet, or dashboard object. CLI, MCP, and future web UI surfaces should use the same semantics even when the presentation density differs.
- Status patterns should be accessible and explicit: do not rely on color alone, and keep long diagnostics behind details or inspection affordances while preserving the primary recovery command.

## Cross-Story Dependencies

- The isolation strategy is decision-first: verifier scaling and architecture updates should settle the physical isolation direction before enforcement work depends on it.
- Trace propagation and metric/dashboard work should share naming, correlation, and tag conventions so traces, logs, metrics, and dashboards can be joined during incident analysis.
- Read-path caching and write-amplification cleanup both affect tenant status, corpus statistics, and dashboard freshness. Cache invalidation, counters, stream bounds, and retry-queue keys should be coordinated so one story does not hide stale or unbounded behavior from another.
- Physical isolation hardening depends on the earlier tenant provisioning and isolation foundation, and must remain compatible with tenant authorization and context enforcement from the security hardening work.
