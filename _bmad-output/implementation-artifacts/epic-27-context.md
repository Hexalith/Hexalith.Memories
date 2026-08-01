# Epic 27 Context: Access Telemetry Lifecycle Hardening

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give operators one explicitly owned, deployable, and verifiable bounded lifecycle for per-tenant access telemetry. The lifecycle must prevent unbounded growth while preserving required access-event emission, strict tenant and privacy boundaries, transparent degradation signals, and the product's limited assurance claim: this is infrastructure telemetry, not a tamper-evident, append-only, legally compliant, or certified audit-retention system.

## Stories

- Story 27.1: Access-Telemetry Retention Ownership Decision (Decision-First)
- Story 27.2: Bounded Retention/TTL and Purge Implementation
- Story 27.3: Production Adapter Manifest, Unit, and Deployment-Lane Qualification
- Story 27.4: Retention Verification, Operations Runbook, and A41 Close-Out

## Requirements & Constraints

- Search, ingestion, mutation, and rejected access activity must continue to produce tenant-attributed access records. Introducing lifecycle enforcement must not weaken the required audit emission path.
- Retention is bounded by an explicit duration range and deterministic expiry/purge semantics. Valid, missing, malformed, minimum, and maximum settings must follow the ratified startup policy; no path may silently fall back to unbounded retention.
- Lifecycle behavior must remain defined with two writers, restart or rescheduling, backpressure, and temporary sink failure. Delivery, recovery, loss, and degradation must be observable through low-cardinality health and metrics without secrets, raw content, or unbounded tenant labels.
- Writes, expiry, purge, and operational inspection must preserve tenant and privacy boundaries and fail closed for rejected, unknown, malformed, empty, or mismatched tenant scope. Cross-tenant negative evidence must exercise every affected storage, routing, and inspection surface.
- Production qualification must use executed, re-runnable evidence against an immutable deployment profile. Static manifests and in-process adapter tests establish only their own contracts; they cannot substitute for running-target behavior.
- Capacity admission must cover the one-hour, configured 24-hour, and seven-day horizons, include physical amplification, durability, indexes, and reclamation workspace, and use checked arithmetic. The ratified envelope is 250 events per second cluster-wide, up to 151,200,000 records and 144.20 GiB of canonical payload at seven days.
- Operational material must cover ownership, configuration, defaults, storage impact, monitoring, alarms, purge verification, incidents, recovery, rollback, reclamation, decommissioning, and honest RPO/RTO and assurance limits.

## Technical Decisions

- The lifecycle is container-service-neutral and Dapr-only. Product code must not depend on Redis, Kubernetes, a backend SDK, an orchestrator API, or OpenBao directly for this capability.
- The portable runtime uses typed-state sanitization, non-blocking buffering, Dapr service invocation, and a fixed-identity Dapr actor with durable state and reminders. Dapr state, configuration, and secrets sit behind a fail-closed behavioral capability gate.
- Expiry uses millisecond logical timestamps and actor-driven purge. Continuously signed attestations use an independent UTC source with a one-second bound. Dynamic writer and key-rotation barriers prevent lifecycle transitions from racing active writers.
- Write, service, clock, inspection, and adapter authorities are separate. Physical reclamation is component-specific evidence collected outside the application API.
- The approved production planning profile is `PG-ONPREM-1`: a dedicated PostgreSQL 18.4 Dapr v2 adapter on the current single-node on-premises cluster. PostgreSQL pod/process replacement is the only in-profile zero-loss fault when the node and retained local volume remain healthy. Node, volume, control-plane, and site loss are outside profile; no HA claim is permitted, and backup/restore requires approved nonzero RPO/RTO.
- Runtime and access-telemetry secrets use distinct Dapr secret-store components and OpenBao prefixes with separate read-only policies. Cross-prefix reads fail closed.

## Cross-Story Dependencies

- Story 27.1 ratifies ownership, topology, failure, retention, purge, validation, and assurance boundaries before Stories 27.2 or 27.3 may claim a sink/store implementation.
- Story 27.2 owns the portable lifecycle implementation and executed lifecycle-checkpoint evidence. Its open predecessor gaps `DW 27.3-CR42` through `DW 27.3-CR46` must be closed with actual executions before Story 27.3 can enter review.
- Story 27.3 remains the evidence recipient for C0 and the independent C2/C3/C4 adapter qualification. C0 cannot close until the five Story 27.2 gaps have executed evidence and an independent reviewer re-reviews C0.
- Story 27.3 owns no running-target C1 gate. All twenty-five C1 gates remain held without a registered owner until compliant successor story files, real per-gate producers, and a later approved registration exist. Completing Story 27.3 therefore enables no Production lifecycle write and does not advance Story 27.4.
- Story 27.4 owns deployment-shaped lifecycle proof, the operations runbook, and A41 close-out. It remains blocked until Story 27.3 and properly registered C1 successors are done, every C1 gate passes on the same immutable profile hash, and terminal validation and publication evidence are complete. A41 remains open until then.
