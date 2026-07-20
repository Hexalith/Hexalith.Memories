---
project: memories
date: 2026-07-19
status: approved
change_scope: moderate
review_mode: batch
prepared_for: Administrator
trigger_story: 27.3
trigger_checkpoint: C1
approval_status: approved
approved_by: Administrator
approved_on: 2026-07-20
---

# Sprint Change Proposal — Story 27.3 C1 Production Adapter and Deployment Profile

## 1. Issue Summary

Story 27.3 reached C1 with the portable lifecycle implementation complete but without an eligible
Production state-store profile. The reviewed target currently exposes:

- Dapr runtime 1.18.1 and `state.redis/v1` for `access-telemetry-store`;
- a single Redis Stack StatefulSet with a 20 GiB PVC and `appendfsync everysec`;
- lifecycle and clock Deployments scaled to zero with placeholder `0.0.0` application images; and
- no executed atomicity, backend-fault zero-loss, capacity, throughput, or physical-reclamation proof.

The current profile cannot be promoted. The ADR's 24-hour canonical payload is 22,118,400,000 bytes
before index, actor, persistence, replication, allocator, or reclamation overhead, already exceeding
the committed 20 GiB reservation. More importantly, Redis transaction semantics cannot prove the
required rollback after a later operation fails. The reviewed C1 rejection packet correctly keeps
Production lifecycle writes disabled and A41 open.

### Evidence

- Current rejection packet:
  `_bmad-output/implementation-artifacts/tests/27-3-adapter-profile-evidence.md`.
- Current target identity:
  `redis-state-v1-dapr-1.18.1-openbao-2.6.0-4183b741eac062d9`.
- Current live application image:
  `registry.hexalith.com/memories@sha256:71e49b6e806ec2fa7c221e58600ba02115693923db05915663396be01b1c042c`.
- Current live Dapr sidecar image:
  `ghcr.io/dapr/daprd@sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8`.
- The Dapr state-store matrix identifies PostgreSQL v2 as a stable store supporting CRUD,
  transactions, ETags, TTL, actors, and workflows. The v2 component is the recommended PostgreSQL
  choice for new applications and supplies database-enforced transactions plus TTL cleanup:
  [Dapr state-store matrix](https://docs.dapr.io/reference/components-reference/supported-state-stores),
  [Dapr PostgreSQL v2 component](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-postgresql-v2/).
- Azure PostgreSQL zone-redundant HA synchronously commits to the standby and is designed for zero
  committed-data loss on planned and unplanned failover:
  [Azure PostgreSQL HA](https://learn.microsoft.com/en-us/azure/postgresql/flexible-server/concepts-high-availability).

### Precise problem statement

This is a failed Production-adapter approach discovered during implementation. The current Redis
profile is ineligible, while the remainder of Story 27.3 depends on a separately provisioned,
capacity-funded, fault-injected, and jointly approved adapter. Keeping adapter qualification and A41
close-out in one story obscures the platform handoff and makes the current story's slice proof false:
C1 is now a bounded, independently reviewable prerequisite with its own deployment and approval
outcome.

## 2. Impact Analysis

### Epic impact

- **Epic 27 remains valid and in progress.** Its objective is unchanged.
- **Story 27.3 does not remain intact.** It is narrowed to Production-adapter qualification and the
  deployment profile. Its completed C0 handoff, C1 tooling, rejection evidence, debug history, and
  existing C1 file changes stay with Story 27.3.
- **New Story 27.4** receives former Story 27.3 Tasks 2-8: deployment-shaped lifecycle verification,
  failure/privacy evidence, runbooks, reconciliation, A41 mutation, and terminal validation.
- **Epics 28 and 29 are not reordered or expanded.** Story 27.4 remains gated behind Story 27.3;
  Epic 29's broader OpenBao/Aspire work remains separate.
- No rollback of Story 27.2 is required. Its Dapr-only portable implementation is the correct boundary.

### Story impact

The split adds one backlog row but no new capability. It converts the current C1 stop condition into a
truthful platform-delivery boundary:

1. Story 27.3 selects and qualifies one exact adapter profile.
2. Story 27.4 verifies the retained lifecycle against that immutable profile and owns A41 close-out.

If Story 27.3 rejects the selected PostgreSQL profile, Story 27.4 remains backlog, Production writes
remain disabled, and another correct-course run is required. No C1 gate may be weakened.

### Artifact conflicts

- **PRD:** No change. FR67, NFR16, NFR17, the compliance boundary, and the Dapr/OpenBao boundary remain
  achievable.
- **Architecture:** Add the selected Production adapter profile and explicit Redis rejection to ADR
  27.1. The general architecture remains backend-portable and Dapr-only.
- **UX:** No interface, flow, component, responsive, localization, or accessibility change.
- **Epics and sprint ledger:** Split Story 27.3 and add Story 27.4.
- **Deployment:** Replace only `access-telemetry-store` and its backend. The domain/search Redis Stack
  remains unchanged and must not be reused for lifecycle state.
- **Testing/evidence:** Preserve current C1 tooling under Story 27.3. Move C2-C6 and A41 terminal
  evidence ownership to Story 27.4.
- **Operations/security:** A named Platform Operations approval and a separate named security approval
  become mandatory C1 artifacts.

### Technical impact

The selected qualification target is profile **`PG-AZ-1`**:

| Field | Exact decision |
| :-- | :-- |
| Profile ID | `postgresql-v2-dapr-1.18.1-azure-pg17.10-zrha-d32ds-v5-4095gib-40kiops-v1` |
| Dapr component | `access-telemetry-store`, `type: state.postgresql`, `version: v2` |
| Dapr runtime | 1.18.1 stable, with the digests listed below |
| Backend | Azure Database for PostgreSQL Flexible Server 17.10 |
| Availability | Zone-redundant HA; primary and synchronous standby in separate availability zones |
| Compute | General Purpose `Standard_D32ds_v5`, 32 vCores |
| Storage | Premium SSD v2, 4,095 GiB = 4,396,972,769,280 usable bytes |
| Storage performance | 40,000 provisioned IOPS and 1,200 MiB/s throughput |
| Network | Private access only; public network disabled; private DNS; egress restricted to TCP 5432 |
| TLS | TLS 1.2 or later, `sslmode=verify-full`, explicit CA bundle, hostname verification |
| Database boundary | Dedicated database `memories_access_telemetry`, schema `access_telemetry`, runtime role limited to that database/schema |
| TTL cleanup | Dapr `cleanupInterval: 5m`; logical expiry and actor purge remain normative |
| Actor store | `actorStateStore: "true"`; sole actor type `AccessTelemetryLifecycleActor`, fixed ID `global` |
| Dapr control plane | Three replicas each for Operator, Placement, Scheduler, Sentry, and Injector; required anti-affinity across three zones |
| Scheduler | Three 16 GiB retained volumes on zone-resilient storage; 16 GiB etcd quota per member |
| Retention | 1-hour minimum, 24-hour default, 7-day maximum; no backend default TTL |
| Physical reclamation | Cohort deletes plus ordinary `VACUUM (ANALYZE, INDEX_CLEANUP ON)`; `pgstattuple`/table and index statistics prove bytes returned to the PostgreSQL allocator within 24 hours |

PostgreSQL 17.10 is a currently supported Azure Flexible Server version. Azure documents TLS
certificate verification with `sslmode=verify-full`. PostgreSQL ordinary VACUUM returns dead-tuple
space to the database allocator; it does not claim disk shrink, and `pgstattuple` exposes table,
dead-tuple, and free-space byte counts. Those limits define the exact physical-reclamation claim:
[supported PostgreSQL versions](https://learn.microsoft.com/en-us/azure/postgresql/configure-maintain/concepts-supported-versions),
[TLS verification](https://learn.microsoft.com/en-us/azure/postgresql/security/security-tls-how-to-connect),
[Azure autovacuum behavior](https://learn.microsoft.com/en-us/azure/postgresql/troubleshoot/how-to-autovacuum-tuning),
[pgstattuple](https://www.postgresql.org/docs/17/pgstattuple.html).

The selected compute/storage shape follows Azure's documented D32ds_v5/Premium SSD v2 performance
envelope but remains subject to a live regional quota and price reservation:
[Azure PostgreSQL performance planning](https://learn.microsoft.com/en-us/azure/postgresql/compute-storage/concepts-optimal-performance).

### Required Dapr component configuration

The Production component must be generated from this non-secret contract. The connection string is an
OpenBao-backed Dapr secret and must contain `sslmode=verify-full`, the approved private FQDN, database,
runtime role, timeout, and CA path. It must never contain a public endpoint or appear in application
configuration.

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: access-telemetry-store
spec:
  type: state.postgresql
  version: v2
  initTimeout: 1m
  metadata:
    - name: connectionString
      secretKeyRef:
        name: access-telemetry-postgresql
        key: connectionString
    - name: tablePrefix
      value: access_telemetry.lifecycle_
    - name: metadataTableName
      value: access_telemetry.dapr_metadata
    - name: timeout
      value: 3s
    - name: cleanupInterval
      value: 5m
    - name: maxConns
      value: "64"
    - name: connectionMaxIdleTime
      value: 5m
    - name: actorStateStore
      value: "true"
auth:
  secretStore: access-telemetry-secrets
scopes:
  - memories-access-telemetry
```

`queryIndexes` is intentionally absent: PostgreSQL v2 does not implement Dapr Query API, and the
portable lifecycle already owns explicit transactional expiry-bucket keys. Adding a backend query path
would violate the Dapr-only portable contract.

### Required immutable image set

The C1 profile manifest must use image references by registry digest only. Tags and container runtime
layer IDs are insufficient.

| Workload | Required digest |
| :-- | :-- |
| Dapr sidecar | `ghcr.io/dapr/daprd@sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8` |
| Dapr Operator | `ghcr.io/dapr/operator@sha256:89661f52a3d37f5d528c35dd9d2b4ac76c7b274bd459c8570d6246b6bfdda549` |
| Dapr Placement | `ghcr.io/dapr/placement@sha256:6caf20016d115d4a7f133b9206b739a10abd9f558d76683b27be9ab60f759e26` |
| Dapr Scheduler | `ghcr.io/dapr/scheduler@sha256:c9bb9ada0cd6a63cd92c26470da1985124e423432af4e39f09b96979fd1059c0` |
| Dapr Sentry | `ghcr.io/dapr/sentry@sha256:2f98508dff56c75329dbd51674c89f41ce349e06c7744ab2519cb69ba338d41f` |
| Dapr Injector | `ghcr.io/dapr/injector@sha256:2793b954b1aef142d59bd5eae71bec4de5f71d16e9ad80fec81cbf3b4eea428c` |
| Memories Server | `registry.hexalith.com/memories@sha256:71e49b6e806ec2fa7c221e58600ba02115693923db05915663396be01b1c042c`, unless Story 27.3 changes Server code; then the replacement CI digest is mandatory |
| Lifecycle service | **Missing and blocking:** replace `0.0.0` with the signed CI-produced `registry.hexalith.com/memories-access-telemetry@sha256:<64-hex>` |
| Clock service | **Missing and blocking:** replace `0.0.0` with the signed CI-produced `registry.hexalith.com/memories-access-telemetry-clock@sha256:<64-hex>` |
| OpenBao used by the reviewed target | `quay.io/openbao/openbao@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653` |

The two missing application digests are intentionally not invented. Story 27.3 cannot become done and
the profile hash cannot be approved until both images are built from the reviewed source, signed,
scanned, deployed by digest, and recaptured from live Pod `imageID` values.

### Capacity contract

All arithmetic uses integer bytes and checked/arbitrary-precision operations.

| Retention | Records | Canonical payload bytes |
| :-- | --: | --: |
| 1 hour | 900,000 | 921,600,000 |
| 24 hours | 21,600,000 | 22,118,400,000 |
| 7 days | 151,200,000 | 154,828,800,000 |

For each row:

```text
baseBytes = records * (measuredRecordBytes + measuredIndexBytes) * 2
controlBytes = 34,359,738,368
reclamationWorkspace = max(137,438,953,472, ceil_div(baseBytes, 4))
requiredPeak = baseBytes + controlBytes + reclamationWorkspace
schedulerBytes = 3 * 17,179,869,184
totalPlatformRequired = requiredPeak + schedulerBytes
```

The durability multiplier is `2` for the primary and synchronous standby. Azure's internal storage
copies remain part of the service SKU and fault claim, not additional application-usable bytes.

The 4,095 GiB database reservation yields exact thresholds:

| State | Bytes |
| :-- | --: |
| Maximum steady-state admission (70%) | 3,077,880,938,496 |
| Reclamation critical boundary (80%) | 3,517,578,215,424 |
| Lifecycle Unhealthy boundary (90%) | 3,957,275,492,352 |

The reservation is a qualification floor, not a waiver. Measured record/index amplification, WAL,
autovacuum, backup, allocator, and cohort-reclamation evidence must still fit the formula. Platform
Operations must attach the live France Central (or approved target region) SKU availability, quota,
price, and funding approval before C1 passes. A region substitution changes the profile hash and
requires reapproval.

### Declared fault model

The exact in-profile single-component fault is:

> Loss of the Azure PostgreSQL primary compute and its availability zone while the synchronous standby
> zone and regional control plane remain available.

C1 must inject a forced failover while the two-writer workload is active and prove zero loss of every
transaction Dapr acknowledged before the fault. It must record disconnect duration, Dapr retry behavior,
queue/drop accounting, DNS reconnection, actor reactivation, reminder reconstruction, and the observed
recovery time. A Dapr sidecar restart remains a required process-fault test but is not the adapter's
declared backend fault.

Regional loss, simultaneous primary/standby loss, operator data deletion, credential compromise, and
logical corruption are outside the declared single-component profile. Platform Operations must publish
their separate RPO/RTO and recovery procedure. Azure's cross-region mechanisms are asynchronous and may
carry a non-zero RPO; they must not be described as zero-loss HA.

## 3. Recommended Approach

Use **Direct Adjustment with a bounded story split**.

- **Change classification:** Moderate — backlog reorganization plus a Platform Operations deployment
  and a security approval boundary.
- **Implementation effort:** High for Story 27.3 because it includes provisioned infrastructure,
  fault injection, a 30-minute load window, backlog drain, capacity measurement, and evidence review.
- **Timeline impact:** Add one story boundary, not a new epic. Estimate 3-5 working days after quota,
  images, credentials, and a three-zone target are ready; Story 27.4 retains its existing estimate.
- **Technical risk:** High until the exact profile passes; Medium after immutable C1 approval.
- **MVP impact:** None. This is post-MVP operational assurance.
- **Release impact:** Production access-telemetry lifecycle remains disabled until both stories pass.

### Alternatives considered

1. **Keep Redis and increase its PVC:** Not viable. Capacity alone cannot supply rollback atomicity, and
   `appendfsync everysec` cannot establish the required zero acknowledged-loss fault claim.
2. **Keep Story 27.3 intact:** Rejected. C1 now has an independent platform artifact, funding decision,
   fault model, and two-party approval. Its failure is a complete bounded outcome while A41 close-out
   remains unstarted.
3. **Rollback Story 27.2:** Not viable. The portable Dapr-only behavior is correct and reusable.
4. **Reduce retention or weaken C1:** Not permitted. The accepted 1-hour/24-hour/7-day contract and all
   capability gates remain unchanged.
5. **Review the PRD/MVP:** Unnecessary. Product requirements and user experience are unaffected.

### Implementation sequence

1. Apply the approved story/ledger split without changing A41.
2. Provision `PG-AZ-1`, the three-zone Dapr control-plane profile, and the dedicated security boundary.
3. Build, sign, scan, publish, and pin the lifecycle and clock application image digests.
4. Run C1 capability, transaction rollback, TTL, actor/reminder, request-size, throughput, failover,
   capacity, isolation, encryption, and cohort-reclamation probes against the exact profile.
5. Capture the immutable profile hash and both approvals. Keep writes disabled on any failed, missing,
   skipped, stale, or unapproved result.
6. Only after Story 27.3 is `done`, create/start Story 27.4 and execute C2-C6 plus A41 close-out.

## 4. Detailed Change Proposals

### 4.1 Narrow Story 27.3

**Artifact:** `_bmad-output/planning-artifacts/epics.md` and the current Story 27.3 implementation file.

**Old:**

```markdown
### Story 27.3: Retention Verification, Operations Runbook, and A41 Close-Out

As a security reviewer,
I want executable lifecycle evidence and one coordinated close-out,
so that A41 closes only after the policy works in the deployment shape.

Tasks 0-8 own C0-C6, exact-adapter selection, all deployment verification,
operations documentation, and A41 mutation in one story.
```

**New:**

```markdown
### Story 27.3: Production Adapter and Deployment Profile

As a Platform Operations and security review pair,
I want one immutable Production state-store profile qualified against every C1 gate,
so that deployment-shaped lifecycle verification starts only on an atomic, durable,
capacity-funded, and approved adapter.

Acceptance criteria:

1. The exact `PG-AZ-1` runtime, component, backend, Dapr control plane, application
   images, component/config manifests, actor/Scheduler identities, configuration
   epoch, profile hash, region, quota, and cost are captured from the running target.
2. CRUD, strong reads, ETags, rollback-atomic multi-key transactions, TTL, actor
   reactivation, Placement/Scheduler/reminder recovery, request bounds, two-writer
   500 events/s throughput, 150,000-record purge catch-up, isolation, encryption,
   capacity, and cohort-attributable physical reclamation all pass without skip.
3. Forced loss of the PostgreSQL primary/AZ proves zero loss of every acknowledged
   record. Outside-profile RPO/RTO and recovery are published without overstating them.
4. Hexalith Platform Operations approves capacity, quota, cost, operation, fault,
   upgrade, rollback, and reclamation evidence; a separate security reviewer approves
   identity, secrets, TLS, network, authorization, encryption, privacy, and evidence
   integrity.
5. Any missing digest, placeholder, profile drift, failed probe, missing approval, or
   unreserved capacity keeps Production writes disabled, Story 27.3 in progress, Story
   27.4 in backlog, and A41 open.
```

**Rationale:** C1 is independently deliverable and independently rejectable. Keeping its existing C0/C1
record with Story 27.3 preserves truthful history while removing unrelated terminal close-out scope.

### 4.2 Add Story 27.4

**Artifact:** `_bmad-output/planning-artifacts/epics.md`.

**Old:** No Story 27.4 exists; current Story 27.3 owns Tasks 2-8.

**New:**

```markdown
### Story 27.4: Retention Verification, Operations Runbook, and A41 Close-Out

As a security reviewer,
I want executable lifecycle evidence and one coordinated close-out against the
approved Production profile,
so that A41 closes only after the policy works in the deployment shape.

Predecessor gate:

- Story 27.3 is `done` with an immutable `PG-AZ-1` C1 packet, no skipped or stale
  result, and both required approvals.
- The live profile hash at Story 27.4 start exactly matches Story 27.3.

Scope:

- Move former Story 27.3 Tasks 2-8 here without weakening their acceptance text:
  multi-writer and replacement proof; retention/expiry/purge/reclamation; failure,
  privacy, authority, health, metrics, and alerts; runbook and adapter appendix;
  residual reconciliation; evidence-backed A41 mutation; and terminal validation.
- A41 remains `carried-forward` and its sprint action remains `open` until every
  Story 27.4 checkpoint and publish verification passes.
```

**Rationale:** The split preserves all close-out gates while preventing platform provisioning and
adapter rejection from being hidden inside a terminal security-remediation story.

### 4.3 Update the Epic 27 slice statement

**Artifact:** `_bmad-output/planning-artifacts/epics.md`.

**Old:** Story 27.3 alone owns exact-adapter certification, deployment evidence, runbook, and close-out.

**New:**

```markdown
Story 27.3 owns exact Production-adapter qualification and the immutable deployment
profile. Story 27.4 consumes only that approved profile and owns deployment-shaped
lifecycle evidence, operations documentation, and A41 close-out. Adapter rejection is
a complete Story 27.3 outcome only when it preserves fail-closed writes and routes a
new correct-course decision; it never closes A41.
```

### 4.4 Add the selected adapter appendix to ADR 27.1

**Artifact:** `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md`.

**Old:** The ADR defines portable capability gates but names no accepted Production adapter.

**New:** Add a **Production Adapter Qualification — PG-AZ-1** section containing the profile,
component YAML, image set, capacity arithmetic, fault model, physical-reclamation claim, approval
separation, and exact assurance limit from this proposal. Record Redis as rejected for C1 atomicity and
capacity. Keep the ADR's portable selected technology unchanged.

### 4.5 Update sprint status only after approval

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`.

**Old:**

```yaml
27-3-retention-verification-operations-runbook-and-a41-close-out: in-progress
```

**New:**

```yaml
27-3-production-adapter-and-deployment-profile: in-progress
27-4-retention-verification-operations-runbook-and-a41-close-out: backlog
```

Do not change `epic-27: in-progress`, the A41 action's `open` status, the deferred entry's
`carried-forward` status, or historical Epic 20/Story 20.5 records.

### 4.6 PRD and UX disposition

- **PRD:** No edit.
- **UX design:** No edit.
- **MVP scope:** No edit.

## 5. Implementation Handoff

### Scope classification

**Moderate.** Product direction is unchanged, but backlog ownership, platform provisioning, and
separated approvals must be coordinated before development can continue.

### Responsibilities

#### Product Owner / Administrator

- Approve or revise this proposal and the Story 27.3/27.4 split.
- Keep Epic 27 prioritized and A41 open.
- Approve any later retention or capacity change through a new decision; do not accept a gate waiver.

#### Hexalith Platform Operations — accountable owner

- Provision the exact `PG-AZ-1` Azure PostgreSQL and three-zone Dapr/Scheduler/Placement profile.
- Confirm region/SKU availability, quota, price, budget, backup, maintenance, and decommission funding.
- Supply the non-secret manifest, live identity, immutable image inventory, profile hash, configuration
  epoch, actor types, Scheduler connections, capacity operands, and current service health.
- Run the declared primary/AZ fault, record acknowledged IDs before/after, and publish measured recovery.
- Own database maintenance, TTL cleanup observation, VACUUM/`pgstattuple` reclamation evidence, alerts,
  upgrade, rollback, backup/restore, and outside-profile RPO/RTO.
- Sign `platform_operations_approval: approved` only after every operational C1 result passes.

#### Security reviewer — independent approval

- Verify private-only connectivity, `sslmode=verify-full`, CA rotation, encryption at rest, OpenBao
  secret scope, least-privilege database role/schema, Dapr deny-by-default invocation ACLs, component
  scopes, NetworkPolicy, and no backend authority in application containers.
- Verify raw tenant/user/case/query/subject/source values are absent from state, evidence, metrics, and
  logs; rerun the named cross-tenant denial-before-dependency tests.
- Review image signatures, vulnerability results, evidence hashes, fault output, and profile drift rules.
- Sign `security_approval: approved` separately. Security approval cannot be inferred from Platform
  Operations approval or delegated to the implementation agent.

#### Developer / Maintainer

- Preserve the Dapr-only application boundary and Story 27.2 portable behavior.
- Build and publish the missing lifecycle/clock images through the approved CI path.
- Implement only qualification tooling or manifest changes required by Story 27.3.
- Keep C2-C6, runbooks, reconciliation, and A41 mutation out of Story 27.3.
- Attach exact commands, fresh source/assembly hashes, immutable outputs, and zero-skip results.

### Success criteria for Story 27.3 handoff

1. No image tag or placeholder remains in the reviewed profile.
2. The live profile hash covers all non-secret component, backend, Dapr, capacity, fault, and image
   identities and remains unchanged through the probe.
3. Every C1 capability and load/fault/reclamation gate passes with zero skips/not-runs/unclassified
   errors and zero acknowledged-record loss.
4. Capacity fits the exact integer formula at 1 hour, 24 hours, and 7 days and the regional reservation
   is funded.
5. Platform Operations and security approvals are present, separate, named, dated, and hash-bound.
6. Production lifecycle writes remain disabled until the terminal C1 verifier accepts the exact packet.
7. Story 27.4 remains `backlog`; its implementation file is created and work starts only after Story
   27.3 is done.

## 6. Change Navigation Checklist

| Item | Status | Finding |
| :-- | :-- | :-- |
| 1.1 Triggering story | [x] | Story 27.3, C1 Production adapter gate. |
| 1.2 Core problem | [x] | Failed Redis Production-adapter approach plus missing runnable images/profile proof. |
| 1.3 Supporting evidence | [x] | Live target, rejection packet, manifests, capacity numbers, and image identities inspected. |
| 2.1 Current epic viability | [x] | Epic 27 remains viable with a bounded split. |
| 2.2 Epic changes | [x] | Add Story 27.4; no new epic. |
| 2.3 Remaining epics | [x] | Epics 28/29 unchanged. |
| 2.4 Obsolete/new epics | [N/A] | None. |
| 2.5 Order/priority | [x] | 27.3 qualification precedes 27.4 close-out. |
| 3.1 PRD conflict | [N/A] | No requirement or MVP change. |
| 3.2 Architecture conflict | [x] | ADR 27.1 receives the selected adapter appendix under this approved change. |
| 3.3 UX conflict | [N/A] | No user-surface impact. |
| 3.4 Other artifacts | [x] | Epics, current story, sprint ledger, ADR, its focused structure guard, and A41 backlog-home references are aligned; deployment/runtime evidence and operations docs remain implementation work. |
| 4.1 Direct adjustment | [x] | Viable; High implementation effort, High pre-proof risk. |
| 4.2 Rollback | [x] | Not viable; Story 27.2 remains correct. |
| 4.3 PRD/MVP review | [x] | Not viable or necessary. |
| 4.4 Recommended path | [x] | Direct adjustment with Story 27.3/27.4 split. |
| 5.1-5.5 Proposal components | [x] | Issue, impact, path, MVP disposition, and handoff recorded. |
| 6.1 Checklist review | [x] | All analysis sections addressed. |
| 6.2 Proposal accuracy | [x] | Cross-checked against live C1 evidence and authoritative docs. |
| 6.3 User approval | [x] | Administrator approved the complete proposal on 2026-07-20. |
| 6.4 Sprint-status update | [x] | Approved 27.3 rename and 27.4 backlog row applied without changing Epic 27 or A41 state. |
| 6.5 Handoff confirmation | [x] | Moderate change routed to Product Owner/Developer; named Platform Operations and security approvals remain C1 deliverables. |

## 7. Approval and Routing Record

Administrator approved this proposal on 2026-07-20. The bounded planning changes are applied to Epic
27, the current Story 27.3 artifact, ADR 27.1, the sprint ledger, and the A41 backlog-home references.
This approval authorizes the backlog split and qualification work; it is not a C1 platform or security
approval and does not waive any probe.

- Story 27.3 and Epic 27 remain `in-progress`.
- Story 27.4 is registered as `backlog` and has no implementation file until Story 27.3 is `done`.
- Production lifecycle writes remain disabled.
- The Redis profile remains rejected; `PG-AZ-1` remains the sole qualification target until C1 passes
  or a new correct-course decision replaces it.
- `20.5-A41-ACCESS-TELEMETRY-RETENTION` remains `carried-forward` with its action `open`.
