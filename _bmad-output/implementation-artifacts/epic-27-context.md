# Epic 27 Context: Access Telemetry Lifecycle Hardening

**Current execution direction — 2026-10-21:** Use the short [active Spec 14](spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) and [current report](tests/27-4-retention-verification-evidence.md), under the [approved simplification](../planning-artifacts/sprint-change-proposal-2026-10-21.md). Freeze relevant runtime inputs, reuse the existing test, and obtain one bounded operational authorization. Unrelated documentation changes cannot block frozen inputs. Local functional completion is independent of the separately scheduled full qualification/A41 campaign. Superseded preparation history is removed; retained-resource dispositions live in the current report.


<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give Platform Operations an owned, bounded lifecycle for tenant access telemetry while preserving continuous emission, accepted domain outcomes and tenant/privacy boundaries. Qualify the exact deployment profile on attributable evidence, provide operating procedures, and close the retention residual only after full close-out conditions pass.

## Stories

- Story 27.1: Access-Telemetry Retention Ownership Decision (Decision-First)
- Story 27.2: Bounded Retention/TTL and Purge Implementation
- Story 27.3: Production Adapter Manifest, Unit, and Deployment-Lane Qualification
- Story 27.4: Retention Verification, Operations Runbook, and A41 Close-Out
- Story 27.21: Runtime and Control-Plane Identity
- Story 27.22: Component and Backend Identity

## Requirements & Constraints

- Operations owns TTL, observable purge, erasure mapping, bounded recovery, sanitization and dated accepted debt. Qualification/configuration fails closed before Production; invalid/missing settings never allow unbounded retention. After admission, delivery stays non-blocking within the approved failure bound, signals degradation beyond it, and never rolls back accepted mutations.
- Records use issued opaque identities and enumerated values; query text, snippets, content-derived values and raw principals are forbidden. Tenant authority is independent of workload/channel identity; write, service, clock, inspection and adapter authorities remain distinct. Deny before dependencies; filtering proves no physical isolation. Principal tokens are tenant-keyed, random, collision-checked and stable while active; physically purge their mapping and observe verification before erasure completion, never export it, and omit tokens whose mapping is unavailable rather than reconstructing it.
- Erasure requires durable telemetry handoff; restart/replay/restore cannot resurrect content. Post-erasure accounting binds one immutable tenant per invocation and permits only counting/deleting/locating records. Enumerate mapping readers, record uses, destroy the mapping with the last addressed record, and record completion in the erased-tenant register. Retained records remain outside product/export/support content.
- Full evidence covers acknowledgement, recovery, expiry/purge, newer-record preservation, emission and privacy across two Server writers and workload/sidecar/actor/Placement/Scheduler/backend faults. Runbooks cover ownership, settings/defaults, capacity, alarms, incidents, recovery, rollback, reclamation/decommissioning and measured RPO/RTO. Telemetry claims no tamper-evidence, append-only integrity, legal compliance or certified retention.

## Technical Decisions

- EventStore owns domain truth; PostgreSQL telemetry leaves Redis/FalkorDB search unchanged. Lifecycle application persistence is Dapr-only; use qualified secret-store references and keep backend/orchestrator access behind owning boundaries.
- Hexalith.Platform owns composition, resource admission, startup/readiness, run ownership and outcome-aware cleanup through Builds' existing runner/descriptor/result. Memories owns logger/lifecycle/clock services, module probes and retention assertions. Missing Platform capability is a blocker, not a new Memories environment/bootstrap framework.
- The functional route is Platform-owned local infrastructure with real PostgreSQL. Bind the actual Platform revision, direct Memories mapping, tool and descriptor. Preserve selected immutable Dapr 1.18.1/PostgreSQL 18.6 inputs; report incompatibility instead of substituting. Derive a local profile identity; Redis, hosted attachment and historical VM/bootstrap cannot satisfy this route.
- Local source/Debug, file secrets and development UTC provide limited functional evidence. Preserve signed-clock checks, real time, strong reads and actual retention; forbid fake clocks, backend mutation and capability bypass. Success requires observed Platform cleanup; failure/withdrawal preserves its actual resource/result policy. These outcomes grant no full-profile, independent-UTC, OpenBao/Kubernetes, CI, Production or A41 credit.
- Full qualification needs separately adopted exact target/profile bytes and eligible same-session evidence. Consult [the full checkpoint inventory](../../docs/operations/access-telemetry-full-qualification.md) and the [qualification profile documentation](../../docs/operations/access-telemetry-adapter-production.md#scope-and-immutable-profile), including its retained KVM manifest. KVM and old PG profiles remain history until an exact full Platform target/profile is adopted; this context adopts none. Historical PG1, closed-window PG2 and KVM captures are not transferable passes. Single-node retained-volume proof covers healthy-node replacement, not node/site HA; reserve physical amplification, durability/index overhead and reclamation workspace.
- Adopted `sole-owner-qualification/2026-10-12` permits Jérôme Piquot (`github:user:6775094`) to produce and accept non-Production evidence as `sole-owner/self-reviewed`. Keep distinct gate, Operations/Security and C5/C6 records without independence claims; C1.25 records owner security risk acceptance. Bounded owner-approved runs and private hashed/readback evidence replace institutional issuers/external custody in this mode. Exact target/action/window approval, runtime security, publication and Production remain separately governed.

## UX & Interaction Patterns

Use “access telemetry,” never “audit trail.” Explain status, cause, impact and next action; colour/motion alone cannot convey status. Evidence views grant no product-web activation. Platform supplies the environment; Memories reports behavior and assurance limits.

## Cross-Story Dependencies

- 27.1 ratifies policy before sink/store claims; 27.2 owns portable behavior; 27.3 owns C0 and independent C2/C3/C4 adapter qualification plus remediation. Disposable-cluster health cannot qualify Production OpenBao/C1.
- Existing Spec 14 is 27.4's sole functional execution specification; the full campaign's separate checklist names each gate's owner, evidence, review and completion state. The existing retention-verification report is the sole consolidated human report. Additional story-file registration is no longer a qualification predicate; preserve historical statuses/receipts without replacement numbered specs.
- All 25 distinct C1 checks, gate-specific bindings, semantic verifiers and attributable evidence remain mandatory. Historical 27.21/C1.15 and closed-window 27.22/C1.16 acceptance grants no reusable-session/other-gate credit. Offline fixtures, captures, preparation and policy adoption pass no live gate.
- Repository machinery may progress while live qualification is held. Completion/A41 require applicable 27.3 obligations, all 25 eligible same-session C1 passes, deployment-shaped C0–C6, terminal validation and publication verification on one unchanged adopted full profile. Functional feedback is separate; Spec 14/report retain exact readiness and operational authority.
- Current operational `20.5-A41-ACCESS-TELEMETRY-RETENTION` remains carried forward and its action open. Any accepted-debt disposition follows the [canonical project context](../project-context.md#critical-dont-miss-rules): named approver/owner, scope, rationale, risk/consequence, compensating controls, and a bounded review/expiry date or measurable reopen trigger. Debt acceptance passes no technical gate and completes neither Story 27.4 nor full qualification. Reconcile architecture/A41 to canonical evidence; Epic 20/20.5 remain historical done. Epics 31/35 retain infrastructure/security/release gates; tenant-lifecycle/erasure blockers retain their owners.
