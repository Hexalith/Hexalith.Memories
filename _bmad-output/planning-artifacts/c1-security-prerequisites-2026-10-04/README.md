# C1 Candidate Story Index

**Status:** 24 unregistered planning drafts; historical C1.16 preparation verified, no live gate credited.
**Date:** 2026-10-04

The approved 2026-08-03 allocation is preserved exactly. Existing Story 27.21
is excluded because it already owns the runtime identity capture. Every role below
is a planned accountable allocation from that approved mapping, not a newly
registered owner or an independently assigned reviewer.

[Sprint change proposal](../sprint-change-proposal-2026-10-04-c1-security-prerequisites.md),
[security/profile plan](security-requalification-plan.md), and
[verification record](verification.md) govern these drafts. The approved
[implementation handoff](implementation-handoff.md),
[D3 interchange contract](predecessor-interchange-contract.md) and
[exact inactive profile package](profile-candidate/README.md) record the next review inputs.

## Drafts

| Story | Gate | Single outcome / draft | Accountable role | Registration / live evidence |
| :---- | :--- | :--------------------- | :--------------- | :--------------------------- |
| 27.7 | C1.1 | [running-target CRUD round trip](stories/27-7-running-target-crud-round-trip.md) | Deployment Adapter Developer | held / not-started |
| 27.8 | C1.2 | [post-write strong read](stories/27-8-post-write-strong-read.md) | Deployment Adapter Developer | held / not-started |
| 27.9 | C1.3 | [ETag and `FirstWrite` semantics](stories/27-9-etag-and-first-write-semantics.md) | Deployment Adapter Developer | held / not-started |
| 27.10 | C1.4 | [rollback-atomic transaction](stories/27-10-rollback-atomic-transaction.md) | Deployment Adapter Developer | held / not-started |
| 27.11 | C1.5 | [effective TTL expiration](stories/27-11-effective-ttl-expiration.md) | Deployment Adapter Developer | held / not-started |
| 27.12 | C1.6 | [actor reactivation survival](stories/27-12-actor-reactivation-survival.md) | Deployment Adapter Developer | held / not-started |
| 27.13 | C1.7 | [Placement/Scheduler/reminder recovery](stories/27-13-placement-scheduler-reminder-recovery.md) | Platform Operations | held / not-started |
| 27.14 | C1.8 | [request-bound enforcement](stories/27-14-request-bound-enforcement.md) | Deployment Adapter Developer | held / not-started |
| 27.15 | C1.9 | [sustained two-writer workload](stories/27-15-sustained-two-writer-workload.md) | Platform Operations | held / not-started |
| 27.16 | C1.10 | [purge backlog catch-up](stories/27-16-purge-backlog-catch-up.md) | Platform Operations | held / not-started |
| 27.17 | C1.11 | [physical tenant-isolation denial](stories/27-17-physical-tenant-isolation-denial.md) | Security Engineer | held / not-started |
| 27.18 | C1.12 | [transport and at-rest encryption](stories/27-18-transport-and-at-rest-encryption.md) | Security Engineer | held / not-started |
| 27.19 | C1.13 | [capacity admission](stories/27-19-capacity-admission.md) | Platform Operations | held / not-started |
| 27.20 | C1.14 | [physical reclamation attribution](stories/27-20-physical-reclamation-attribution.md) | Platform Operations | held / not-started |
| 27.22 | C1.16 | [component/backend identity](stories/27-22-component-and-backend-identity.md) | Deployment Adapter Developer | held / not-started |
| 27.23 | C1.17 | [image/manifest/epoch/profile identity](stories/27-23-image-manifest-epoch-and-profile-identity.md) | Platform Operations | held / not-started |
| 27.24 | C1.18 | [node/storage/cost record](stories/27-24-node-storage-and-cost-record.md) | Platform Operations | held / not-started |
| 27.25 | C1.19 | [declared-fault durability](stories/27-25-declared-fault-durability.md) | Platform Operations | held / not-started |
| 27.26 | C1.20 | [out-of-profile statement](stories/27-26-out-of-profile-statement.md) | Platform Operations | held / not-started |
| 27.27 | C1.21 | [backup/restore proof](stories/27-27-backup-and-restore-proof.md) | Platform Operations | held / not-started |
| 27.28 | C1.22 | [RPO/RTO and no-HA boundary](stories/27-28-rpo-rto-and-no-ha-boundary.md) | Platform Operations | held / not-started |
| 27.29 | C1.23 | [operations approval](stories/27-29-operations-approval.md) | Platform Operations Approver | held / not-started |
| 27.30 | C1.24 | [non-HA acknowledgement](stories/27-30-non-ha-acknowledgement.md) | Platform Operations Approver | held / not-started |
| 27.31 | C1.25 | [independent security approval](stories/27-31-independent-security-approval.md) | Independent Security Reviewer | held / not-started |

## Registration and Execution Order

1. Decide the proposed successor profile and canonical evidence interchange.
2. Prepare security repository inputs and implement component/image/resource
   identity producers (27.22, 27.23, 27.24).
3. Finalize published fault exclusions (27.26) before faults; implement the
   state/actor/request and isolation/encryption slices.
4. Perform bounded preflight before load; obtain separate load, purge, reclamation,
   capacity, declared-fault and backup/restore evidence. Finalize measured recovery
   boundaries (27.28). No load/capacity dependency cycle is permitted: bounded
   resource preflight grants no passing capacity disposition.
5. Validate separate Operations approval (27.29), non-HA acknowledgement (27.30)
   and independent Security approval (27.31) after their evidence exists.

Every registration transaction supplies one supported literal producer mode,
one focused fixture, one finalized story and its exact checkpoint command,
classification/slice/claim records and passing creation guards; then it adds
exactly one epic definition and one `backlog` row. Green draft record checks are
not sufficient to register an absent producer.

The draft commands deliberately name proposed PG-ONPREM-2 and a proposed external
evidence root. The successor direction is approved; these exact profile bytes and external
evidence root are not adopted or authorized today. Finalize both from actual
reviewed inputs before making a command callable or authorizing target contact.
The candidate JSON file is an allocation index, not a sprint-status registry.

## Dated repository adoption — 2026-10-04

The exact candidate was approved and adopted by the bounded implementation spec.
See [the adoption record](adoption.md) for current selectors, retained historical receipts,
neutral capture boundaries and the single C1.16 owner. Earlier pending/adoption-held
text records proposal-time state; its original bytes and operator intent are preserved.
No target execution, security requalification, Production activation or gate pass is claimed.
