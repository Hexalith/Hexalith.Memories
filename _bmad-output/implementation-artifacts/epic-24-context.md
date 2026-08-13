# Epic 24 Context: Observability & Performance Hardening

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Harden Hexalith.Memories for realistic multi-tenant operation by making asynchronous ingestion traceable end to end, eliminating avoidable read round trips and hot-path writes, bounding tenant-wide work, turning emitted metrics into usable dashboards, and advancing tenant isolation from naming conventions toward structural enforcement backed by scalable, accurately classified evidence. This matters because operators must be able to diagnose failures, prove zero cross-tenant leakage, and keep latency and resource use predictable as tenant count and data volume grow.

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

- Preserve zero cross-tenant leakage across ingestion, every search axis, and graph traversal. Evidence must cover malformed, swapped, and colliding tenant identifiers; graph proof requires identical tenant A/B structures and edge identifiers with distinct payload markers, then authenticated traversal returning no foreign nodes or edges.
- Adding tenants must not degrade an existing tenant by more than 5% at ten tenants with 100K memory units each. Tenant listing and verification must avoid unbounded fan-out and pairwise deep scans.
- Propagate W3C trace context from CLI or MCP through ingress, server, Dapr workflows, activities, and backends. Retain OpenTelemetry correlation in logs and verify trace completeness end to end.
- Export ingestion throughput, per-axis search latency, per-tenant index size, and pipeline queue depth through one naming family and a committed Aspire or Grafana dashboard.
- Front tenant status, embedding configuration, and corpus statistics reads with short-lived, tenant-scoped caches invalidated by relevant writes. Bound tenant-list paging and concurrency so operator refreshes cannot stampede actors or backends.
- Read paths and background loops must not cause avoidable durable writes or unbounded growth. Activity streams need bounded retention and counters, replay tracking needs an application-owned in-flight set, and retry queues need stable identifiers rather than serialized-value identity.
- Isolation verification must fail closed without mutating tenant data. Structural existence, content leakage, configured-vector compatibility, key-family membership, missing markers, and foreign markers are distinct claims and must not be conflated.
- Reviews touching ingestion must retain claim-checked large payloads, scheduling-time workflow configuration, chunk-addressed vectors with stable product identity, recoverable non-URL re-ingestion, provisioning-owned index creation with memoized readiness, and one rate-limit admission per provider operation or bounded batch. Each verdict needs rerunnable evidence, reviewer, date, and result; blockers need owner, consequence, proof boundary, and reopen trigger.

## Technical Decisions

- Dapr Workflow remains the durable orchestration boundary. Serialize trace context where propagation cannot cross workflow history, link workflow/activity spans to the initiating request, and register `Microsoft.DurableTask` telemetry. Orchestrators remain replay-safe; activities own side effects.
- Redis physical isolation targets per-tenant ACL users and tenant-scoped backend routing. Tenant workflows own per-tenant search indexes and graph databases. Prefixes, hash tags, logical databases, and query filters are not the primary security boundary.
- FalkorDB isolation is database-level. `GRAPH.LIST` proves database existence, not content isolation; focused real-backend integration evidence is authoritative, not a new runtime content scan.
- Raw and natural-language indexes must independently match the requested tenant's configured embedding dimension. Index-to-index agreement is secondary; unavailable or invalid configuration fails closed without creating or migrating indexes.
- Semantic marker evidence uses collision-safe classification from canonical namespace provenance and record shape. Active raw base/chunk and current natural-language records count; staging and legacy nested-NL records do not. Ambiguity is a classification gap, not an invented mismatch.
- A foreign marker on an active record means possible contamination; a missing marker means incomplete evidence. Both fail closed without payload disclosure and direct exact-key inspection or quarantine before tenant-scoped repair or reindex. Never recommend blanket prefix deletion.
- Aspire and OpenTelemetry are the common observability path. Trace, log, metric, dashboard, and tenant tags must share stable correlation and naming conventions so operators can move between signals without ad hoc translation.
- EventStore remains the domain source of truth; Redis and FalkorDB remain rebuildable projections. Performance cleanup must not make cached or backend read models authoritative domain state.

## UX & Interaction Patterns

Tenant verification should return an operator evidence packet: healthy results show verified state, timestamp, and audit context; unhealthy results classify the issue, affected scope, severity, and safest next action. Isolation failures are critical safety states; backend degradation and evidence gaps remain distinct. Feedback must explain what happened, impact, severity, and recovery without relying on color or hiding status in global notifications. Diagnostics should be bounded, payload-free, expandable, and semantically consistent across CLI, MCP, dashboards, and future web views.

## Cross-Story Dependencies

- Story 24.3 anchors physical isolation and scalable verification. Enforcement and migration must follow its ACL and backend-routing target.
- Stories 24.6-24.8 are separate verifier slices. Story 24.9 follows Story 24.8 because marker diagnostics depend on active-family classification.
- The proposed physical-isolation qualification, enforcement, migration, and runtime-evidence follow-ups were re-keyed to Stories 24.10-24.13. They remain held and unregistered until their existing activation gates pass; their former 24.6-24.9 identities are historical aliases only.
- Trace propagation, metric naming, and dashboard work share correlation and tag conventions. Read caching and write-amplification cleanup share invalidation, counter, stream-bound, and freshness semantics.
- Isolation work depends on tenant lifecycle ownership, ingress authorization, tenant-context enforcement, and the EventStore/projection model. It must not introduce create-if-missing behavior in feature or verifier paths.
