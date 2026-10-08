# Epic 27 Context: Access Telemetry Lifecycle Hardening

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Bound tenant access telemetry under Platform Operations. Preserve emission, domain outcomes and tenant/privacy; qualify the exact profile and close the residual only on independently accepted evidence.

## Stories

- Story 27.1: Access-Telemetry Retention Ownership Decision (Decision-First)
- Story 27.2: Bounded Retention/TTL and Purge Implementation
- Story 27.3: Production Adapter Manifest, Unit, and Deployment-Lane Qualification
- Story 27.4: Retention Verification, Operations Runbook, and A41 Close-Out
- Story 27.21: Runtime and Control-Plane Identity
- Story 27.22: Component and Backend Identity

## Requirements & Constraints

- Operations owns TTL, observable purge, erasure mapping, bounded recovery, sanitization and dated accepted debt. Qualification/configuration fail closed before Production; invalid/missing settings cannot permit unbounded retention.
- After admission, delivery is non-blocking within the approved failure bound and signals degradation beyond it. Preserve continuous emission and accepted mutations/domain truth.
- Records are content-free, using opaque issued identities/enumerated values. Tenant authority is independent of workload/channel identity; write/service/clock/inspection/adapter authorities remain separate. Deny before dependencies; filtering cannot prove physical isolation.
- Erasure requires durable telemetry handoff. Opaque records follow TTL outside product/export/support. Retention accounting binds one immutable erased tenant per invocation and permits only counting/deleting/locating records. Enumerate mapping readers, record uses and purge with the last record. Restart/replay/restore cannot resurrect content.
- Prove acknowledgement, recovery, expiry/purge, newer-record preservation, emission/privacy across two Server writers and workload/sidecar/actor/Placement/Scheduler/backend faults.
- Runbooks cover owner, configuration/defaults, capacity, alarms, purge, incidents, recovery, rollback, reclamation/decommissioning and measured RPO/RTO. Telemetry claims no tamper-evidence, append-only integrity, legal compliance or certified retention.

## Technical Decisions

- EventStore owns truth; PostgreSQL telemetry leaves Redis/FalkorDB search unchanged. Lifecycle infrastructure access is Dapr-only, using secret references/OpenBao; no backend SDK/orchestrator API belongs in the service.
- Current consumers require immutable `PG-ONPREM-2`, SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`: PostgreSQL 18.6, authenticated Dapr 1.18.1 identities, OpenBao 2.6.4/chart 0.29.6 and approved configuration bytes. Reject old/mixed profiles and source/configuration drift; preserve historical PG1 captures and operator intent without PG2 credit.
- Profile adoption/runtime PG2 alignment are prepared; live security upgrade, requalification and acceptance remain separate. Image/index/local-peer identity, capabilities and secret references prove no execution platform, Dapr/backend linkage or behavior.
- Single-node retained-volume qualification covers healthy-node pod/process replacement, never node/site HA. Reserve physical amplification, durability/index overhead and reclamation workspace. The 168-hour horizon is evidence-only outside exact 400-GiB admission. The 250-events/s seam cannot satisfy 500-events/s, 30-minute C1 qualification.
- Require distinct gate artifacts, complete provenance and two authenticated Operations/Security decisions. Jérôme Piquot (`github:user:6775094`) may approve both bundle roles with distinct decisions/receipts, excluding every capture producer; reconsider before Production/account/role/producer changes. C5/C6 need separate reviewers. Captures/fixtures/preparation create no acceptance.

## UX & Interaction Patterns

Use “access telemetry.” Label status, cause, impact and next action; colour/motion alone cannot convey status. Evidence views confer no product-web activation.

## Cross-Story Dependencies

- 27.1 ratifies policy before sink/store claims; 27.2 owns portable runtime; 27.3 owns C0 and independent C2/C3/C4 adapter qualification. Disposable-cluster health cannot prove Production OpenBao/C1.
- All twenty-five C1 gates need current-profile, attributable running-target evidence and independent acceptance. Story 27.21 is done only for historical PG1 C1.15; fresh PG2 renewal remains separately scoped and unregistered. Story 27.22 is done only for accepted closed-window PG2 C1.16 identity/linkage; reusable session eligibility remains unproven. Twenty-three other gates stay held/unregistered. These outcomes grant no other-gate, execution-platform, security or activation credit.
- I1 structural readers are complete; shape/local consistency grants no authority. I2–I6 registry/provenance, semantic verification, custody, authority/session, assembly, consumer migration and verification remain incomplete. P1–P7 retain unresolved provider/bootstrap, time/precision, custody/session, delegation/topology, registrations, C1.16 eligibility/renewal and consumer-dispatch decisions despite policy/GitHub preparation. Fresh evidence, separately scoped repeated-source-label producer correction and inherited producer maintenance remain outstanding.
- Story 27.4 may build machinery as `awaiting-operator`; completion/Production writes/A41 require qualified 27.3, registered successors done, all C1 passed on one unchanged profile, deployment-shaped C2–C6, terminal validation and publish verification.
- Keep `20.5-A41-ACCESS-TELEMETRY-RETENTION` carried forward and its action open until close-out. Reconcile architecture/A41 to canonical evidence; Epic 20/20.5 remain historically done. Tenant enforcement, erased-tenant restore and other Production blockers retain their owners/evidence.
