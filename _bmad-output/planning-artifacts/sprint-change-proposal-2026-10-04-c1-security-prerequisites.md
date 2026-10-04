# Sprint Change Proposal: C1 Registration and Security Prerequisites

**Date:** 2026-10-04  
**Mode:** Batch  
**Status:** Approved for repository implementation; exact profile bytes and live execution remain separately reviewed  
**Scope:** Moderate backlog preparation with an explicit architecture profile decision  
**Baseline:** `47027d35a1f4a2c6986bec85b5cae601ce1b201e`

The Administrator authorized a separate prerequisite effort and then explicitly
requested C1 and security prerequisite **planning**. This proposal implements that
planning request. It does not approve new profile bytes, register unsupported
producers, contact a target, upgrade a deployment, or authorize Git publication.

## 1. Issue Summary

Story 27.4's repository machinery is complete but its approved operator spec remains
`awaiting-operator`. The approved 2026-08-03 allocation defines one story per gate,
but only Story 27.21 is registered. Its accepted runtime identity capture grants
no Production gate pass. All 24 canonical story files remain absent. Following implementation approval,
C1.16 historical preparation is verified; the other 23 missing modes remain absent. Registering those names alone would violate the approved registration
transaction and repeat the withdrawn bundled-story failure.

Security requalification is a separate prerequisite. Repository pins remain
OpenBao 2.6.0/chart 0.28.5 and PostgreSQL 18.4. The architecture spine requires
upgrades and current evidence. PostgreSQL 18.4 is part of the immutable
`PG-ONPREM-1` identity, so a security upgrade cannot silently retain its hash or
inherit its prior evidence.

The canonical C1 predecessor also differs from the existing capture envelope:
the PowerShell runner emits neutral capture packets, while the Python validator
requires unique artifact/source identities, passing dispositions and two distinct
profile-bound approvals. Their interchange must be reviewed before any assembly
can authorize C2-C4; producer availability alone does not settle it.

## 2. Impact Analysis

| Artifact or area | Impact and proposed treatment |
| :--------------- | :---------------------------- |
| Epic 27 | Preserve the 25-gate allocation and one-gate registration rule; prepare the 24 missing slices without advancing gate states. |
| Stories 27.7-27.31 | Twenty-four held drafts, excluding existing Story 27.21, carry one outcome, approved accountable role, dependencies, negative evidence, classifications and claim verification. |
| Story 27.21 | Preserve accepted historical identity capture and its original packet/source hashes. A new profile/session needs its own current evidence and explicit gate evaluation. |
| Story 27.3 and Story 27.4 | Retain existing story history and current state. A future profile adoption reconciles both consumers and requires fresh same-profile evidence. |
| `epics.md` | Current 27.21 status prose is stale relative to the tracker. The future registration transaction appends a dated current-state correction and each actually supported successor definition. |
| `sprint-status.yaml` | Future producer transactions add exactly one matching `backlog` row each; planning adds no rows or completion credit. |
| PRD NFR8/NFR9/NFR10/NFR34 | Preserve isolation, Dapr-only secrets, tenant authority and fail-closed qualification. No product goal, MVP gate or assurance boundary changes. |
| Architecture AD-15/AD-17/AD-19 | Plan security/profile requalification; keep all other Production blockers visible. Updated versions are proposed evidence inputs, never qualification by themselves. |
| Epic 31 | Coordinate OpenBao platform ownership and future pin/probe updates; do not reopen or copy the broad Story 31.1 as an implementation template. |
| UX | No screen, interaction, navigation or accessibility behavior changes. |
| CI, deployment and tests | Identify future producer/fixture and exact-profile changes. Preserve the existing unrelated working-tree edits. |

The sprint tracking restriction in the 2026-09-12 correction applies to that
correction and its Epics 32-35 handoff; it is not used as an excuse to prohibit a
later separately approved Epic 27 registration transaction. This plan nevertheless
makes no tracker mutation because no new producer transaction is complete.

## 3. Recommended Approach

Use Direct Adjustment within the existing Epic 27 allocation. Keep withdrawn
Stories 27.5/27.6 withdrawn. Do not roll back working lifecycle machinery or reduce
MVP scope. The [candidate index](c1-security-prerequisites-2026-10-04/README.md)
links all 24 drafts and their proposed canonical story keys.

Execute the following stages in dependency order:

1. **Profile decision and evidence interchange:** approve a versioned successor
   profile and scope the observation/disposition-to-predecessor conversion. Preserve
   the old profile and source evidence. Do not make arbitrary profile hashes valid.
2. **Security repository preparation:** authenticate exact image/chart identities,
   render OpenBao 0.29.6 with an explicit 2.6.4 server override, prepare PostgreSQL
   18.6, and update only reviewed profile consumers and focused guards.
3. **Identity producer transactions:** begin with Story 27.22/component-backend
   identity, then 27.23/image-manifest-profile identity and 27.24/node-storage-cost.
   Register a story only when its literal mode, fixture and creation guards exist.
4. **Boundary and capability transactions:** prepare 27.26/failure exclusions before
   fault tests; implement CRUD/consistency/transaction/TTL/actor/request gates, then
   isolation/encryption and controlled load/purge/reclamation/durability/recovery.
   Bounded operator resource preflight precedes load; it is not a C1 capacity pass.
5. **Independent approvals and downstream qualification:** evaluate genuine
   Operations approval, non-HA acknowledgement and independent Security approval
   only after their evidence dependencies exist. Assemble and validate all 25
   unique gates on the revised profile before authorized C2-C4 or A41 work.

Stages 2-5 describe future implementation and operator work. Repository producer
development can use offline fixtures while security/profile work progresses, but
no old-profile observation is promoted into evidence for the revised profile.

Planning effort is bounded; implementation is substantial: 24 independently
reviewable producer slices plus security/profile/interchange prerequisites. Fault,
load, backup/restore and retention evidence dominate elapsed time. No completion
date is promised without approved target access, resource budgets, reviewers and
the required retention observation windows.

## 4. Detailed Change Proposals

### D1: Complete producer-backed registration, one slice at a time

**Current:** Only `27-21-runtime-and-control-plane-identity` owns a gate. The
runner now accepts C1.15 and explicit historical-opt-in C1.16 on PG-ONPREM-1;
PG-ONPREM-2 is still rejected. The other allocated IDs
are held definitions with no registered implementation files.

**Proposed:** Finalize each linked draft in its own bounded transaction containing
the literal supported mode, a focused fixture, finalized story file, exact
checkpoint command, Historical Context Classification, Slice Proof and Epic AC
Verification. Evaluate the finalized file with
`python3 tools/check-story-slice-scope.py --require-record --story-key <its exact key>`
and verify nonzero focused tests before adding its epic definition and `backlog`
row. The candidate map provides every exact key; the generic notation here is a
procedure, not an executable or placeholder checkpoint cell.

**Rationale:** Registration must represent an executable owner, not a reservation.
Planning drafts and green record-shape checks establish neither producer existence
nor gate acceptance. Approval-only modes consume real reviewer artifacts.

### D2: Version the security-qualified profile

**Current:** PostgreSQL 18.4 is hashed into `PG-ONPREM-1`; its canonical SHA-256 is
`dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14`.
The C1 predecessor and downstream terminal/close-out checks bind that exact hash.

**Proposed decision:** Retain `PG-ONPREM-1` as historical, and ratify a distinct
successor designated **PG-ONPREM-2** after exact artifacts are authenticated.
The candidate keeps the existing single-node, retained local-volume, Dapr-only,
400-GiB topology and zero-loss pod/process fault boundary. It proposes PostgreSQL
18.6 and OpenBao 2.6.4/chart 0.29.6. Include the full relevant secret-platform
identity or a separately authenticated composite linkage rather than quietly
ignoring OpenBao drift. The [exact inactive candidate](c1-security-prerequisites-2026-10-04/profile-candidate/README.md)
now provides authenticated public identities, concrete configuration bytes and
the computed candidate hash. Adoption and operational qualification remain held.

**Rationale:** A security upgrade changes the facts qualified by the old evidence.
The [security requalification plan](c1-security-prerequisites-2026-10-04/security-requalification-plan.md)
defines affected files, release evidence, backup/rollback prerequisites, negative
checks and the fresh evidence chain. Approval must explicitly authorize this
profile revision; generic permission to plan does not ratify it.

### D3: Define the canonical predecessor interchange

**Current:** C1.15 uses `hexalith.access-telemetry.c1.evidence/v1`,
`producerStatus: observed`, `gateStatus: not-evaluated` and
`productionGatePassed: false`. `_validate_predecessor` requires a different exact
per-gate artifact/source/command/disposition structure and two reviewer roles.

**Proposed:** Scope a separate narrow prerequisite that defines a reviewed,
lossless conversion with retained capture and disposition artifacts. Validate
profile, command/source bytes, source commit, artifact identity, timestamps,
nonzero results and reviewer authority. The assembler must reject missing,
duplicated, stale-session, mixed-profile, unreviewed and self-approved inputs.
Keep the existing C1.15 envelope valid as historical capture; do not change it
to claim a gate pass. Bind the resulting accepted gate to the canonical artifact
that actually supports its disposition, with retained capture provenance.

**Rationale:** The fields required by the downstream verifier cannot be inferred
from an observed packet or filled with synthetic approvals. The
[source-audited interchange contract](c1-security-prerequisites-2026-10-04/predecessor-interchange-contract.md)
requires fresh versioned capture and genuine authority receipts; it also records
current artifact-semantics and reviewer-authentication gaps. Source mapping and
current authorization freshness need their own executable negative evidence.

### D4: Reconcile current planning prose at actual registration

**Current text:** `epics.md` says Story 27.21 is `in-progress` and C1.15 is pending.

**Proposed dated note at Epic 27 and the relevant story paragraphs:**

> Current state corrected 2026-10-04: Story 27.21 is done and its independently
> reviewed runtime/control-plane identity capture is accepted/complete. That
> disposition grants no Production gate pass. Each additional owner is recorded
> only by its completed producer-backed registration transaction; all unregistered
> gates remain held. A revised profile requires fresh same-profile qualification.

Retain historical approval/rollback paragraphs as history. Do not recast old
observations as current. This note is proposed and is not represented as an
applied source correction; current-state justification uses the verified tracker,
story and compiled context, not the stale paragraph.

## 5. Implementation Handoff and Success Criteria

| Responsible role | Required handoff |
| :--------------- | :--------------- |
| Administrator / architecture owner | Decide D2 and approve exact profile/identity boundaries before profile adoption. |
| Product Owner / Developer | Finalize one candidate per supported producer; maintain literal key/gate parity and bounded registration changes. |
| Deployment Adapter Developer | Implement identity/capability slices and the reviewed interchange; keep capture, acceptance and activation distinct. |
| Platform Operations | Supply authorized target, external evidence archive, protected credential access, budget and maintenance/fault windows; own capacity, recovery and cleanup receipts. |
| Independent Operations and Security reviewers | Be different named people/authorized identities with separately evidenced decisions; approve actual evidence and no inferred gate credit. |

Success means all 24 missing producers have focused fixtures and compliant
registered story files; all 25 gate packets remain unique and profile-bound;
security/profile requalification and the interchange have current evidence; and
the canonical predecessor validates without skips or fabricated approvals.
This does not by itself pass Story 27.4's remaining C0-C6, terminal, publication or
A41 closure gates. Other architecture Production blockers remain independent.

## 6. Change Navigation Checklist

| Items | Status | Result |
| :---- | :----- | :----- |
| 1.1-1.3 Trigger, problem and evidence | [x] | Story 27.4 prerequisites and current source/registry audit establish the issue. |
| 2.1-2.5 Epic scope, dependencies and order | [x] | Preserve Epic 27 allocation; coordinate Epic 31 platform work; no new epic or MVP resequencing. |
| 3.1 PRD | [x] | Preserve NFR8/NFR9/NFR10/NFR34 and infrastructure-telemetry assurance. |
| 3.2 Architecture | [x] | D2 explicitly escalates immutable-profile revision; other security blockers remain open. |
| 3.3 UX | [N/A] | No product interaction changes. |
| 3.4 Other artifacts | [x] | Producer, fixture, profile, archive and bounded registration impacts are explicit. |
| 4.1-4.4 Options | [x] | Direct Adjustment selected; rollback/MVP reduction do not supply missing producers. |
| 5.1-5.5 Proposal and handoff | [x] | Candidate drafts, exact allocations, security steps and accountable roles are present. |
| 6.1-6.2 Review and accuracy | [x] | Current claims, candidate parity and mechanical draft checks are recorded in the verification report. |
| 6.3 Implementation approval | [x] | Administrator explicitly approved D1-D4 and the successor-profile direction on 2026-10-04. Exact authenticated bytes and live execution remain separately reviewed. |
| 6.4 Tracker changes | [!] | Deferred until actual per-story producer-backed registration transactions pass. |
| 6.5 Next handoff | [x] | Start with profile/interchange decisions and the Story 27.22 producer slice. |

## 7. Evidence and Approval

[Verification and re-runnable claim audit](c1-security-prerequisites-2026-10-04/verification.md)
records confirmed current-state claims, unsupported-mode fixtures and all 24
draft record checks. No new live evidence or independent acceptance is claimed.

**Approval:** The Administrator answered `yes` on 2026-10-04 to the complete D1-D4
repository implementation proposal, including the successor-profile direction and
candidate versions. This is the Product Owner / Developer handoff for the approved
bounded registration sequence. Exact authenticated image/chart digests and profile
bytes will be presented as a concrete revision before adoption; live execution
authorization remains separate. Existing unrelated work remains preserved.

## 8. Workflow Execution Log

| Date | Event | Result |
| :--- | :---- | :----- |
| 2026-10-04 | Administrator authorized separate prerequisite work and explicitly requested planning | Planning authorized. |
| 2026-10-04 | Historical slice and Epic AC verification policies loaded; current allocation and source inspected | One-gate drafts; no anti-template reuse. |
| 2026-10-04 | Primary release sources checked for OpenBao/chart/PostgreSQL candidates | Current candidates recorded with links and remaining qualification gates. |
| 2026-10-04 | Complete batch proposal and 24 drafts prepared | Initial planning complete; no registration or deployment credit. |
| 2026-10-04 | Administrator explicitly approved the complete D1-D4 proposal | Moderate PO/Developer handoff approved; investigate exact profile artifacts and interchange, then producer-backed registration beginning with Story 27.22. |
| 2026-10-04 | Historical C1.16 preparation verified and exact inactive candidate retained | Final 55 methods and separate five-method compatibility/denial check passed; exact-byte adoption review pending. All 24 story registrations remain held. |
