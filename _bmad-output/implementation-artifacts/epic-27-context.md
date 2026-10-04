# Epic 27 Context: Access Telemetry Lifecycle Hardening

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give operators an owned, bounded, verifiable lifecycle for per-tenant access telemetry. Preserve required emission, product truth, tenant privacy, and an honest assurance boundary while qualifying the Production deployment and closing the carried-forward retention action only on accepted evidence.

## Stories

- Story 27.1: Access-Telemetry Retention Ownership Decision (Decision-First)
- Story 27.2: Bounded Retention/TTL and Purge Implementation
- Story 27.3: Production Adapter Manifest, Unit, and Deployment-Lane Qualification
- Story 27.4: Retention Verification, Operations Runbook, and A41 Close-Out
- Story 27.21: Runtime and Control-Plane Identity
- Story 27.22: Component and Backend Identity

## Requirements & Constraints

- Platform Operations owns configured retention, observable purge progress, tenant-erasure mapping, bounded recovery, and dated accepted debt. Startup qualification and configuration fail closed; no setting may silently yield unbounded retention.
- Search, ingestion, mutation, and rejected access retain tenant-attributed telemetry. Delivery failure must not roll back an accepted product mutation or alter domain truth; delivery stays non-blocking within the approved failure bound and exposes degradation beyond it.
- Records contain only content-free opaque identifiers, enumerated operation/outcome, and timing. Never record query text, snippets, extracted content, memory-unit content, or derived values. If a record cannot be sanitized, do not write it.
- Tenant authority is required independently of workload/channel authentication for write, purge, and inspection. Rejected, missing, malformed, or mismatched scope fails closed. Erased-tenant accounting is limited to one tenant per invocation and only counting, deleting, or using the mapping to locate records; retained fields cannot enter product, export, support, or query surfaces.
- Production-shaped verification must cross a short expiry window with at least two Server writers and controlled workload, sidecar, actor, Placement, Scheduler, and backend faults. Evidence must show acknowledgement, durable recovery, expired-record unavailability and purge, newer-record preservation, emission continuity, and tenant/privacy denial before dependencies.
- Operations guidance must identify owner, settings and defaults, storage/capacity impact, monitoring and alarms, purge verification, incidents, recovery, rollback, adapter reclamation, decommissioning, RPO/RTO limits, and the assurance boundary. Access telemetry is infrastructure telemetry, not a tamper-evident or certified audit trail.

## Technical Decisions

- The lifecycle is container-service-neutral and Dapr-only: typed-state sanitization and non-blocking buffering feed Dapr invocation and a fixed-identity actor with durable state/reminders. Dapr state, configuration, and secrets pass a fail-closed behavioral capability gate. Product code has no direct backend, Kubernetes, or OpenBao dependency.
- Expiry uses millisecond logical timestamps and actor-driven purge. Independently sourced UTC attestations have a one-second bound; writer/key-rotation barriers protect transitions. Write, service, clock, inspection, and adapter authorities are separate, as are runtime and telemetry secret-store components and OpenBao prefixes.
- The current approved qualification identity is `PG-ONPREM-2`, canonical SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`: PostgreSQL 18.6, Dapr 1.18.1, and OpenBao 2.6.4/chart 0.29.6 with exact approved configuration bytes. Historical `PG-ONPREM-1` evidence grants no current-profile gate credit; mixed-profile evidence is refused.
- The single-node, retained-volume profile claims zero loss only for PostgreSQL pod/process replacement while node and volume remain healthy. It makes no HA claim. Capacity admission must reserve measured physical amplification, durability, indexes, and reclamation workspace for the 250-events/s cluster-wide envelope. The seven-day horizon is evidence-only and is not admitted on the exact 400-GiB profile.

## Cross-Story Dependencies

- Story 27.1 ratifies lifecycle ownership and policy before Stories 27.2 and 27.3 claim a sink/store. Story 27.2 supplies portable lifecycle implementation and executed predecessor checkpoints; Story 27.3 owns C0 and independent C2/C3/C4 adapter qualification. Offline tests and manifest checks do not prove running-target behavior.
- All 25 C1 gates require separate, current-profile running-target evidence and independent acceptance. Story 27.21 is done for historical PG1 C1.15 capture; fresh PG2 C1.15 evidence is still required. Story 27.22 is the registered backlog owner for C1.16, whose live capture, linkage, and review remain pending. The other 23 gates remain held without registered owners.
- Story 27.4 repository producers, validators, dashboard, and runbooks may proceed as `awaiting-operator`. Completion, Production lifecycle writes, and A41 close-out require same-profile predecessor acceptance, all properly registered C1 successors done, all 25 C1 gates passed, deployment-shaped C2-C6 evidence, terminal validation, and publish verification. A41 remains open until those conditions pass.
