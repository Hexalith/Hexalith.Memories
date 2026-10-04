# Epic 27 Context: Access Telemetry Lifecycle Hardening

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Provide an owned, deployable, verifiable bounded lifecycle for per-tenant access telemetry, preserving emission, tenant/privacy boundaries, and observable degradation. Infrastructure telemetry supplies no tamper-evident, append-only, legal-compliance, or certified audit-retention guarantee.

## Stories

- Story 27.1: Access-Telemetry Retention Ownership Decision (Decision-First)
- Story 27.2: Bounded Retention/TTL and Purge Implementation
- Story 27.3: Production Adapter Manifest, Unit, and Deployment-Lane Qualification
- Story 27.4: Retention Verification, Operations Runbook, and A41 Close-Out
- Story 27.21: Runtime and Control-Plane Identity

## Requirements & Constraints

- Search, ingestion, mutation, and rejected access continue producing tenant-attributed records. Platform Operations owns TTL, purge progress, erasure mapping, bounded recovery, and dated accepted debt for unsupported profiles.
- Retention has explicit duration bounds and deterministic expiry/purge. Valid, missing, malformed, minimum, and maximum settings follow ratified startup policy; unbounded fallback is forbidden.
- Qualification/configuration fails closed before Production activation. Admitted delivery stays non-blocking within its approved failure bound; exceeding it surfaces degradation without changing domain truth. Two writers, restart/rescheduling, backpressure, sink failure, loss, and recovery require low-cardinality health/metrics without secrets, raw content, or unbounded tenant labels.
- Records are content-free: opaque identifiers, enumerated operation/outcome, and timing. Query text, snippets, extracted/memory-unit content, and derived content values are forbidden; otherwise do not write the record.
- Writes, expiry, purge, and inspection require tenant authority independent of channel authentication. Rejected, unknown, malformed, empty, and mismatched scope fails closed; cross-tenant negatives cover affected storage, routing, and inspection surfaces.
- Production qualification requires executed, re-runnable evidence on an immutable profile. Manifests, fixtures, and in-process tests prove only their own contracts. Owners, dates, accepted debt, and risk acceptance supply no gate credit.
- Capacity admission reserves measured physical amplification, durability, indexes, and reclamation workspace. The ratified envelope is 250 events/s cluster-wide, up to 151,200,000 records and 144.20 GiB of canonical payload at the seven-day maximum.
- Operations guidance covers ownership, settings/defaults, storage, monitoring/alarms, purge verification, incidents, recovery, rollback, reclamation, decommissioning, and honest RPO/RTO and assurance limits.

## Technical Decisions

- The lifecycle is container-service-neutral and Dapr-only. Product code has no direct Redis, Kubernetes, backend-SDK, orchestrator-API, or OpenBao dependency for this capability.
- Use typed-state sanitization, non-blocking buffering, Dapr invocation, and a fixed-identity actor with durable state/reminders. Dapr state, configuration, and secrets require a fail-closed behavioral capability gate.
- Expiry uses millisecond logical timestamps and actor-driven purge; continuously signed attestations use independent UTC with a one-second bound. Dynamic writer/key-rotation barriers prevent transitions racing active writers.
- Write, service, clock, inspection, and adapter authorities remain separate. Runtime and telemetry secrets use distinct Dapr secret-store components and OpenBao prefixes with separate read-only policies; cross-prefix reads fail closed. Physical reclamation is component-specific evidence collected outside the application API.
- The historical approved `PG-ONPREM-1` profile is dedicated PostgreSQL 18.4 Dapr v2 on the single-node on-premises cluster. Only PostgreSQL pod/process replacement is an in-profile zero-loss fault with node/local volume healthy. Node, volume, control-plane, and site loss are excluded; no HA claim, and backup/restore requires approved nonzero RPO/RTO. Current security qualification requires outdated PostgreSQL/OpenBao posture upgraded and requalified through reviewed profile evidence before activation.
- Erased tenants' opaque telemetry follows bounded TTL. An authenticated operator may only count/delete records and locate them through the mapping, scoped to one erased tenant; retained fields cannot enter product/export/support/query surfaces, nor may accounting recreate resources. Destroy the mapping with the last record; record completion in the platform erasure register.

## UX & Interaction Patterns

Say “access telemetry.” Distinguish capture completeness, independent review, gate acceptance, and degradation. Expose failure/recovery plainly; accepted domain writes remain accepted when telemetry later degrades.

## Cross-Story Dependencies

- Story 27.1 ratifies ownership, topology, failure, retention, purge, validation, and assurance before Stories 27.2/27.3 claim a sink/store.
- Story 27.2 owns portable implementation and executed lifecycle checkpoints. If Story 27.2 checkpoint gaps remain, they must close through actual executions accepted by an independent reviewer before Story 27.3 enters review. Story 27.3 owns C0 and independent C2/C3/C4 adapter qualification; C0 requires complete predecessor executions accepted by an independent reviewer.
- Story 27.21 alone owns C1.15 and is `done` after full verification and final implementation code review. On 2026-10-04 independent reviewer `/root/review_c1_packet` [accepted the real packet](../../artifacts/access-telemetry-c1/C1.15/c1.15-independent-framed-packet-review-20261004T104839838701Z-1b6be26b1d604a50bc53eefcc2203278.json) (review SHA-256 `8a70077297e68962f12da991d76c37a58a2faa86697ea0e2212f590941e62c73`, observed packet SHA-256 `17d7f350c3193ce6663364b0b4e6ef52d8f319f4ca4c0a25e9886a006ff0ed87`) for C1.15 runtime/control-plane identity capture only: checkpoint `accepted` / `complete`, DW-718 `done`. The remaining twenty-four C1 gates stay held without a registered owner. The original awaiting-operator handoff and unavailable-target/blocker evidence are preserved as historical; accepted capture grants no Production activation, write authority, C1.25 approval, human sign-off, Story 27.4 advance, or A41 closure.
- Story 27.4 owns deployment-shaped lifecycle proof, the operations runbook, and A41 close-out. Its repository machinery may proceed as `awaiting-operator`. Completion, Production lifecycle writes, and A41 close-out still require Story 27.3 and all properly registered C1 successors done, all twenty-five C1 gates passed on the same immutable profile hash, and complete terminal validation/publication evidence. A41 remains open until that closure contract is satisfied.
