# Epic 27 Context: Access Telemetry Lifecycle Hardening

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give Platform Operations an explicitly owned, bounded and verifiable lifecycle for tenant access telemetry. Preserve required emission, accepted domain outcomes and tenant/privacy boundaries while qualifying the exact deployment profile and closing the carried-forward retention residual only on independently accepted evidence.

## Stories

- Story 27.1: Access-Telemetry Retention Ownership Decision (Decision-First)
- Story 27.2: Bounded Retention/TTL and Purge Implementation
- Story 27.3: Production Adapter Manifest, Unit, and Deployment-Lane Qualification
- Story 27.4: Retention Verification, Operations Runbook, and A41 Close-Out
- Story 27.21: Runtime and Control-Plane Identity
- Story 27.22: Component and Backend Identity

## Requirements & Constraints

- Platform Operations owns configured TTL, observable purge progress, tenant-erasure mapping, bounded recovery, sanitization and dated accepted debt for unsupported profiles. Qualification and configuration fail closed before Production activation; missing or invalid settings never permit unbounded retention.
- After admission, telemetry delivery is non-blocking for accepted product writes within the approved failure bound and exposes degradation beyond that bound. Failure never changes domain truth or rolls back an accepted mutation. Required emission remains continuous.
- Access records remain content-free and sanitized. Tenant authority and write, service, clock, inspection and adapter authorities are separate. Missing, rejected or mismatched scope fails before dependency access; application filtering alone cannot prove physical tenant denial.
- Tenant erasure must include a durable telemetry handoff. Retained opaque telemetry follows its TTL and remains outside product content, export and support surfaces. Recovery and restore must not resurrect erased tenant content; backup mechanics alone do not prove non-resurrection.
- Deployment-shaped evidence must demonstrate acknowledgement, durable recovery, expired-record unavailability and purge, newer-record preservation, emission continuity and tenant/privacy denial across at least two Server writers and controlled workload, sidecar, actor, Placement, Scheduler and backend faults.
- Runbooks identify owner, configuration/defaults, storage/capacity impact, monitoring and alarms, purge verification, incidents, bounded recovery, rollback, adapter reclamation/decommissioning and measured RPO/RTO limits. This is infrastructure telemetry with no tamper-evidence, append-only integrity, legal-compliance or certified-retention claim.

## Technical Decisions

- EventStore remains authoritative for domain mutations; telemetry is a separate operational plane. Its PostgreSQL adoption does not change the Redis/FalkorDB search backend. Application infrastructure access remains Dapr-only, with credentials behind Dapr secret references and OpenBao; no direct backend SDK or orchestrator API belongs in the lifecycle service.
- Current repository consumers require immutable `PG-ONPREM-2`, SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`: PostgreSQL 18.6, authenticated Dapr 1.18.1 identities, OpenBao 2.6.4/chart 0.29.6 and approved configuration bytes. Old/mixed-profile evidence and source/configuration drift are rejected. PG1 captures and original operator intent remain historical and receive no PG2 credit.
- Repository profile adoption is complete; live security upgrade, operational requalification and independent acceptance remain separate prerequisites. Image/index identity, local PostgreSQL peer identity, capability advertisements and secret references do not establish actual execution-platform qualification, Dapr-to-backend linkage or behavioral acceptance.
- Preserve the approved single-node, retained-local-volume fault and capacity boundary. Pod/process replacement with a healthy node and retained volume does not prove node/site HA. Reserve measured physical amplification, durability/index overhead and reclamation workspace; the 168-hour horizon is evidence-only and is not admitted on the exact 400-GiB profile. The existing 250-events/s workload seam does not satisfy the distinct approved 500-events/s, 30-minute C1 load gate.
- Canonical predecessors need unique gate artifacts, complete source/command/profile/session provenance and two distinct authenticated Operations/Security decisions; Jérôme Piquot (`github:user:6775094`) may approve both bundle roles under the named-owner exception, excluding every capture producer, while post-evidence C5/C6 still require separate reviewers. Neutral captures, offline fixtures, producer registration and authority preparation cannot manufacture acceptance. The reviewed integrated capture/disposition interchange and fresh qualification evidence remain required.

## UX & Interaction Patterns

Use “access telemetry” in operator guidance and output. Present qualification, purge, degradation, recovery, cause, impact and next action with explicit text labels. Colour or motion never carries the only status signal. Operational evidence views do not activate the future product web surface.

## Cross-Story Dependencies

- Story 27.1 ratifies lifecycle ownership and policy before Stories 27.2 and 27.3 claim a sink/store. Story 27.2 owns portable lifecycle evidence; Story 27.3 owns C0 and independent C2/C3/C4 adapter qualification. Disposable-cluster health after secret-store substitution does not prove the Production OpenBao path or pass C1.
- All twenty-five C1 gates need separately attributable current-profile running-target evidence and independent acceptance. Story 27.21 is done only for accepted historical PG1 C1.15; fresh PG2 C1.15 remains a separately scoped, unregistered prerequisite. Story 27.22 is done for independently accepted closed-window C1.16 component/backend identity and linkage; reusable session eligibility remains unproven. It grants no execution-platform, security, other-gate or activation credit. Twenty-three other gates remain held and unregistered; planning drafts confer no ownership or qualification.
- Story 27.4 repository producers, validators, runbooks, dashboard and close-out guards may proceed as `awaiting-operator`. Completion, Production lifecycle writes and A41 mutation require qualified Story 27.3, actual registered successors done, all C1 gates passed on one unchanged approved profile, deployment-shaped checkpoints, terminal validation and publish verification.
- Keep `20.5-A41-ACCESS-TELEMETRY-RETENTION` carried forward and its sprint action open until the complete close-out gate passes. Reconcile architecture and every A41 summary to the same canonical evidence; Epic 20 and Story 20.5 remain historical done records. Tenant enforcement, erased-tenant restore controls and other architecture Production blockers retain their own owners and evidence requirements.
