# Epic 27 Context: Access Telemetry Lifecycle Hardening

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give operators one explicitly owned, bounded, verifiable lifecycle for per-tenant access telemetry. Preserve required emission, accepted domain outcomes, tenant/privacy boundaries, and the infrastructure-telemetry assurance boundary while qualifying the deployment and closing the carried-forward retention residual only on accepted evidence.

## Stories

- Story 27.1: Access-Telemetry Retention Ownership Decision (Decision-First)
- Story 27.2: Bounded Retention/TTL and Purge Implementation
- Story 27.3: Production Adapter Manifest, Unit, and Deployment-Lane Qualification
- Story 27.4: Retention Verification, Operations Runbook, and A41 Close-Out
- Story 27.21: Runtime and Control-Plane Identity
- Story 27.22: Component and Backend Identity

## Requirements & Constraints

- Platform Operations owns configured TTL, observable purge progress, tenant-erasure mapping, bounded recovery, and dated accepted debt for unsupported profiles. Production qualification/configuration fails closed; no missing or invalid setting silently permits unbounded retention.
- Access telemetry covers tenant-attributed search/access operations. After admission, delivery remains bounded and non-blocking for accepted product writes, exposes degradation beyond the approved bound, and never changes domain truth or rolls back an accepted mutation. Preserve required emission continuity.
- Records are content-free: opaque tenant/case/principal identifiers, enumerated operation/outcome, and timing. Exclude query text, snippets, extracted or memory-unit content, derived content, credentials, and raw principal identities. A record that cannot be sanitized is not written; an unavailable principal-token mapping does not justify guessing or reconstructing identity.
- Write, purge, and inspection authority is independent of workload/channel authentication. Missing, rejected, or mismatched tenant scope fails closed before dependencies. The telemetry store is a tenant-isolated lifecycle resource; application filtering alone does not prove physical tenant denial.
- Retained opaque telemetry follows its TTL after tenant erasure. Operator-authorized retention accounting binds each invocation to exactly one erased tenant and permits counting/deleting records and using the mapping solely to locate them. It never exposes retained fields through product, export, support, or query surfaces. Enumerate mapping readers, record uses, destroy the mapping with its last addressed record, and record purge completion.
- Deployment-shaped evidence must show acknowledgement, durable recovery, expired-record unavailability and purge, newer-record preservation, emission continuity, and tenant/privacy denial across at least two Server writers and controlled workload, sidecar, actor, Placement, Scheduler, and backend faults.
- Runbooks must identify owner, configuration/defaults, capacity/storage impact, monitoring/alarms, purge verification, incidents, recovery, rollback, adapter reclamation/decommissioning, and measured RPO/RTO limits. Telemetry does not establish tamper evidence, append-only integrity, legal compliance, or certified audit retention.

## Technical Decisions

- EventStore remains authoritative for domain mutations. Access-telemetry storage is a separate operational plane; PostgreSQL adoption here does not change the Redis/FalkorDB search backend. Infrastructure access stays behind approved adapters and Dapr boundaries; credentials remain behind OpenBao/Dapr secret references.
- The current immutable profile is `PG-ONPREM-2`, canonical SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`: PostgreSQL 18.6, exact authenticated Dapr 1.18.1 identities, OpenBao 2.6.4/chart 0.29.6, and approved configuration bytes. Current C0, predecessor, approval, and terminal consumers reject old/mixed-profile evidence and source/configuration drift. Historical PG1 profiles, captures, and operator intent remain unchanged and earn no successor credit.
- Image/index identity, local read-only PostgreSQL peer identity, capability advertisements, secret references, and offline fixtures are distinct from actual execution-platform qualification, Dapr-to-backend linkage, behavioral evidence, and independent acceptance. Producer registration or capture never grants another gate or Production permission.
- Retained-volume/pod recovery does not establish node/site HA. Preserve approved workload, fault, durability, and physical-capacity limits; the 168-hour horizon is evidence-only and is not admitted on the exact 400-GiB profile.

## UX & Interaction Patterns

Use “access telemetry” in operator guidance and output. Present qualification, purge progress, degradation, recovery, and evidence limitations explicitly with text labels; colour or motion cannot carry the only status signal. An operational dashboard or conformance specimen does not activate the future product web surface.

## Cross-Story Dependencies

- Story 27.1 ratifies lifecycle ownership/policy before Stories 27.2 and 27.3 claim a sink/store. Story 27.2 supplies executed lifecycle predecessor evidence. Story 27.3 owns C0 and independent C2/C3/C4 adapter qualification; disposable-cluster health after secret-store substitution does not prove the Production OpenBao path or pass C1.
- All twenty-five C1 gates require separate same-current-profile running-target evidence and independent acceptance. Story 27.21 is done only for accepted historical PG1 C1.15; fresh PG2 C1.15 remains a separately scoped, unregistered prerequisite. Story 27.22 is the registered completed owner for independently accepted captured C1.16 component/backend identity and full-set connection linkage after all three build reviews; its canonical two-session refusal remains preserved. Twenty-three other gates remain held and unregistered; planning drafts confer no ownership or gate credit.
- Story 27.4 repository producers, validators, runbooks, dashboard, and guards may proceed as `awaiting-operator`. Completion, Production lifecycle writes, and A41 mutation require Story 27.3 qualified/done, actual registered successors done, every C1 gate passed, consistent immutable profile identity, deployment-shaped checkpoints, terminal validation, and publish verification.
- Keep `20.5-A41-ACCESS-TELEMETRY-RETENTION` carried forward and its sprint action open until the full close-out gate passes. Reconcile every A41 summary to the same canonical evidence at close-out; Epic 20 and Story 20.5 remain historical done records.

## Historical C1.15 evidence continuity — reconciled 2026-10-06

The tracked [Story 27.21 disposition](27-21-runtime-and-control-plane-identity.md#2026-10-04-independent-c115-packet-disposition) records accepted historical PG1 runtime/control-plane identity only. Preserve its [observed packet](../../artifacts/access-telemetry-c1/C1.15/c1.15-runtime-control-plane-identity-20261004T104156676Z-af39ddb5939044f38e438e6e584c3268.json), SHA-256 `17d7f350c3193ce6663364b0b4e6ef52d8f319f4ca4c0a25e9886a006ff0ed87`, and [independent review](../../artifacts/access-telemetry-c1/C1.15/c1.15-independent-framed-packet-review-20261004T104839838701Z-1b6be26b1d604a50bc53eefcc2203278.json), SHA-256 `8a70077297e68962f12da991d76c37a58a2faa86697ea0e2212f590941e62c73`. These references restore continuity lost from this context summary; no packet or disposition is changed. They grant no current PG2, C1.16, Production, other-gate or A41 credit. External archive retention remains pending as recorded in Story 27.21.
