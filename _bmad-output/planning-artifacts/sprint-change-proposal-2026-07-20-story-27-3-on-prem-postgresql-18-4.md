---
project: memories
date: 2026-07-20
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

# Sprint Change Proposal — Story 27.3 On-Premises PostgreSQL 18.4 Profile

## 1. Issue Summary

Story 27.3 is locked to an Azure Database for PostgreSQL Flexible Server 17.10 and a
three-zone Kubernetes execution profile. The earlier preflight also searched for AKS,
although Kubernetes does not intrinsically need to be AKS. Administrator clarified that
every target is on premises, the reachable Kubernetes cluster is the intended deployment
target, and a new latest stable PostgreSQL server should be installed there. The Azure
database target is therefore not missing infrastructure to provision; it is the wrong
deployment assumption.

The current on-premises target is reachable at context `jpiquot@local`, but it has one
Ready `amd64` node, no topology zone labels, local OpenEBS host-path storage, and no
PostgreSQL operator. It can host a dedicated PostgreSQL server and prove retained-volume
pod/process recovery. It cannot honestly prove zone-redundant or node-loss failover.

As of 2026-07-20, PostgreSQL 18.4 is the latest stable/current minor release. PostgreSQL
19 is beta and is excluded from this deployment. The Docker Official Image publishes
`postgres:18.4-trixie`; the selected immutable image identities are:

- multi-platform index:
  `sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a`;
- `linux/amd64` manifest:
  `sha256:d93de42662696f278fb34354b06fdaa90ad7ca3106d6f72fbd01d16da006d2cf`.

Sources: [PostgreSQL versioning policy](https://www.postgresql.org/support/versioning/),
[PostgreSQL 18.4 release notes](https://www.postgresql.org/docs/release/18.4/), and
[PostgreSQL Docker Official Image](https://hub.docker.com/_/postgres?tab=tags).

### Supporting evidence

- Kubernetes context: `jpiquot@local`; server version `v1.34.9`.
- Node: `node1`, Ready, `amd64`, 32 CPU, approximately 126 GiB memory, no zone label.
- Host filesystem at discovery: approximately 1,007 GiB total and 801 GiB available.
- Storage class: `openebs-hostpath-retain`, `Retain`, `WaitForFirstConsumer`, local to
  `node1`, with no declared volume-expansion capability.
- Dapr 1.18.1 control-plane resources have three replicas, but all replicas and their
  retained volumes are on the same node; replica count is not node fault independence.
- No PostgreSQL operator CRD exists.
- The only existing PostgreSQL workload belongs to Keycloak in namespace `keycloak`;
  it is not an access-telemetry database and must not be reused or changed.
- `hexalith-memories` still uses `access-telemetry-store` as `state.redis/v1`; both
  lifecycle deployments remain at zero replicas with placeholder application images.
- `DaprAccessTelemetryStateStore.GetDueEntriesAsync` currently calls Dapr Query API,
  while `state.postgresql/v2` does not support Query. The accepted ADR requires explicit
  transactional expiry buckets, so this compatibility defect must be fixed before the
  PostgreSQL component can be exercised.

### Precise problem statement

This change is both a stakeholder correction and a technical-profile substitution:
replace the Azure-only `PG-AZ-1` target with an exact on-premises PostgreSQL 18.4
qualification profile. Preserve the Dapr/OpenBao application boundary and every
capability, privacy, evidence, and approval gate, but narrow the in-profile backend
fault to PostgreSQL pod/process loss while the single node and retained local volume
remain healthy. Node, disk, and site loss must be published as outside profile with no
HA claim and with backup/restore evidence required before Production enablement.

## 2. Impact Analysis

### Epic and story impact

- **Epic 27 remains viable and in progress.** Its bounded-retention objective does not
  depend on Azure.
- **Story 27.3 remains the C1 qualification story.** No new story is needed. Its exact
  target, fault model, capacity basis, deployment artifacts, and evidence wording change
  from `PG-AZ-1` to `PG-ONPREM-1`.
- **Story 27.4 remains backlog and gated.** It consumes the approved immutable profile,
  whatever its provider, only after Story 27.3 is done.
- **Stories 27.1 and 27.2 remain historical done records.** The Query-API incompatibility
  is treated as a narrowly scoped C1 adapter-compatibility defect; it does not reopen or
  rewrite their completed history.
- **Epics 28 and 29 are unchanged.** The OpenBao-first rule remains mandatory.
- No epic is removed, added, or reordered.

### Artifact conflicts

- **PRD:** No change. The PRD requires portability, Dapr/OpenBao secret handling, and
  reliability evidence; it does not mandate Azure or a PostgreSQL major version.
- **Architecture:** The portable Dapr-only design remains valid. ADR 27.1 and the access-
  telemetry architecture summary must name the on-premises profile and its bounded fault
  and capacity claims.
- **UX:** No interface, flow, component, accessibility, or localization change.
- **Deployment:** Add a dedicated PostgreSQL 18.4 StatefulSet/Service/PVC and hardening
  configuration; replace the access-telemetry Dapr state component and its port policy.
  Do not change Redis/FalkorDB or the Keycloak database.
- **Runtime:** Replace the unsupported Query-API expiry scan with deterministic explicit
  minute/shard bucket state before testing PostgreSQL v2.
- **Secrets:** PostgreSQL bootstrap credentials and TLS key material are documented direct
  pod-input exceptions to the OpenBao-first rule. The application connection string stays
  in OpenBao and is read only through the Dapr secret store.
- **Testing/evidence:** Update exact-profile structure guards, C1 tooling, manifests, and
  rejection/approval evidence. Preserve cross-tenant negative evidence.
- **Operations:** A backup destination is not currently identified. Installation may
  proceed with writes disabled, but C1 cannot pass until backup and restore are tested and
  the outside-profile RPO/RTO is approved.

### Exact replacement profile — `PG-ONPREM-1`

| Field | Exact decision |
| :-- | :-- |
| Profile ID | `postgresql-v2-dapr-1.18.1-postgresql-18.4-onprem-k8s1-openebs-local-retain-400g-v1` |
| Kubernetes target | context `jpiquot@local`, namespace `hexalith-memories`, one `amd64` node |
| Dapr component | `access-telemetry-store`, `type: state.postgresql`, `version: v2` |
| Dapr runtime | Existing 1.18.1 control plane and sidecars, recaptured by live digest |
| PostgreSQL | 18.4, Docker Official Image `postgres:18.4-trixie` |
| Image pin | `docker.io/library/postgres:18.4-trixie@sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a` |
| Platform manifest | `linux/amd64` digest `sha256:d93de42662696f278fb34354b06fdaa90ad7ca3106d6f72fbd01d16da006d2cf` |
| Workload | One raw Kubernetes StatefulSet replica; no operator and no HA claim |
| Compute envelope | request 4 CPU/8 GiB; limit 8 CPU/16 GiB; measured load evidence remains authoritative |
| Storage | 400 GiB PVC on `openebs-hostpath-retain`; `Retain`; local to `node1` |
| PostgreSQL 18 layout | mount `/var/lib/postgresql`; `PGDATA=/var/lib/postgresql/18/docker` |
| Database boundary | database `memories_access_telemetry`, schema `access_telemetry`, separate bootstrap/admin and least-privilege runtime roles |
| Network | ClusterIP only; no ingress/public endpoint; NetworkPolicy permits TCP 5432 only from approved lifecycle/bootstrap identities |
| TLS | TLS 1.2 or later; internal CA; service-DNS certificate; client `sslmode=verify-full`; CA and key rotation documented |
| Dapr connection | OpenBao secret `access-telemetry-postgresql`, key `connectionString`; no connection string in source, ConfigMaps, logs, or evidence |
| TTL/actor | `cleanupInterval: 5m`, `actorStateStore: "true"`, fixed lifecycle actor identity; no `queryIndexes` |
| Retention admission | Profile initially funds the configured 24-hour target only if measured physical use fits the thresholds below; a higher duration is rejected until a new capacity result/profile is approved |
| Physical reclamation | Cohort deletes plus `VACUUM (ANALYZE, INDEX_CLEANUP ON)`; prove allocator reuse, not OS-disk shrink |
| Production state | Disabled throughout installation and qualification; no lifecycle writer is enabled by provisioning the database |

The 400 GiB request is 429,496,729,600 bytes. Its profile thresholds are:

| State | Bytes |
| :-- | --: |
| Maximum steady-state admission (70%) | 300,647,710,720 |
| Reclamation critical boundary (80%) | 343,597,383,680 |
| Lifecycle Unhealthy boundary (90%) | 386,547,056,640 |

The PVC request is not proof of physical reservation: local host-path capacity is shared
with the node. C1 must capture filesystem availability, PostgreSQL relation/index/WAL
growth, control-state overhead, reclamation workspace, and competing-volume headroom.
The 7-day software maximum is not admitted by this profile unless measured evidence fits;
a larger disk, replicated storage, or a new approved profile is required otherwise.

### Dapr component contract

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

`queryIndexes` is absent. The runtime must use explicit transactional expiry-bucket
keys and Dapr state/transaction APIs only.

### Revised fault and recovery contract

The exact in-profile single-component fault is:

> Forced loss of the PostgreSQL container/process and replacement of its StatefulSet
> pod while `node1` and the bound retained OpenEBS local volume remain healthy.

C1 must inject this fault during the two-writer workload and prove zero loss of every
record acknowledged before the fault. It must record disconnect duration, retry/drop
accounting, Dapr reconnection, actor reactivation, reminder recovery, PostgreSQL crash
recovery, and observed RTO.

The following are explicitly outside profile: loss of `node1`, the local disk/PV path,
the whole Kubernetes control plane/site, operator deletion of retained data, credential
compromise, and logical corruption. `PG-ONPREM-1` is not HA and must never be described
as zone-, node-, or site-redundant. Production enablement requires a named backup target,
successful restore evidence, and an approved non-zero outside-profile RPO/RTO. If zero-
loss node failure is required, the cluster must first gain multiple fault-independent
nodes and replicated storage or an external on-premises HA PostgreSQL service.

## 3. Recommended Approach

Use **Option 1 — Direct Adjustment** within Epic 27.

- **Change classification:** Moderate. Product scope is unchanged; the exact deployment
  and assurance profile changes.
- **Implementation effort:** Medium for installation and compatibility; High for full
  C1 load, capacity, restore, fault, reclamation, and approval evidence.
- **Technical risk:** High before qualification because storage and the cluster have one
  physical fault domain; Medium for the bounded pod/process profile after proof.
- **MVP impact:** None. This is post-MVP operational hardening.
- **Release impact:** Access-telemetry Production writes remain disabled until C1 and
  later Story 27.4 pass.

### Alternatives considered

1. **Provision Azure PostgreSQL/AKS:** Rejected because Administrator states the target
   infrastructure is on premises.
2. **Reuse Keycloak PostgreSQL:** Rejected. It is a separate product boundary, has only a
   10 GiB PVC, and must not receive Memories lifecycle state.
3. **Keep Redis:** Rejected by the existing C1 atomicity/capacity decision.
4. **Install an HA PostgreSQL operator now:** Not viable on the current single node; more
   replicas would share the same physical failure domain and create false HA confidence.
5. **Wait for a multi-node cluster before any install:** Safer for full HA, but unnecessary
   for a fail-closed qualification server and contrary to the requested deployment.
6. **Use PostgreSQL 19 beta:** Rejected. “Latest” means latest stable for this server.

### Implementation sequence after approval

1. Apply the approved planning/ADR/story/profile changes without changing story status,
   A41, or enabling lifecycle writes.
2. Add the dedicated PostgreSQL 18.4 StatefulSet, Service, 400 GiB retained PVC,
   initialization, TLS, probes, Pod security settings, and NetworkPolicy. Generate
   credentials/certificates outside source and seed only the allowed Kubernetes/OpenBao
   targets; never emit values to logs or evidence.
3. Apply the server to `jpiquot@local/hexalith-memories` and prove PostgreSQL reports
   18.4, the expected image digest, TLS hostname verification, least privilege, network
   denial, retained-volume pod replacement, and no change to Keycloak PostgreSQL.
4. Replace `QueryStateAsync` with the ADR-required deterministic minute/shard expiry
   buckets and add atomicity, ordering, pagination/bounds, stale-index, and two-writer
   tests. Preserve cross-tenant denial-before-dependency evidence.
5. Replace the Dapr component with `state.postgresql/v2`, remove `queryIndexes`, seed the
   OpenBao connection string, and run C1 only with controlled temporary writers.
6. Run the exact capability, rollback-atomic transaction, ETag, TTL, actor/Scheduler,
   request-bound, 500 events/s, 150,000-record purge, isolation, encryption, capacity,
   pod/process fault, and reclamation probes with no skip.
7. Attach a backup target and restore proof; obtain separate named Platform Operations
   and security approvals bound to the immutable profile hash.
8. On any missing digest, capacity shortfall, failed probe, absent backup/restore result,
   or missing approval, return lifecycle replicas to zero, keep Story 27.3 `in-progress`,
   keep Story 27.4 `backlog`, and leave A41 open.

## 4. Detailed Change Proposals

### 4.1 Replace the Story 27.3 qualification target

**Artifacts:** `_bmad-output/planning-artifacts/epics.md` and
`_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`.

**Old:** `PG-AZ-1`, Azure PostgreSQL 17.10, zone-redundant primary/standby,
`Standard_D32ds_v5`, 4,095 GiB Premium SSD v2, Azure primary/AZ loss.

**New:** `PG-ONPREM-1` and the exact profile above. Preserve all C1 capabilities and the
two independent approvals. Change only the infrastructure identity, capacity admission
basis, and declared fault. Replace Azure region/quota/cost evidence with on-premises
node/storage/headroom/operations-cost evidence. Explicitly state that node/PV/site loss is
outside profile and that provisioning does not enable writes.

### 4.2 Replace the ADR qualification appendix

**Artifact:** `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md`.

**Old:** `Production Adapter Qualification — PG-AZ-1`.

**New:** `Production Adapter Qualification — PG-ONPREM-1`, containing the exact image,
StatefulSet/storage, Dapr component, capacity thresholds, Query-API exclusion, fault
boundary, backup/restore blocker, reclamation claim, and approval gate from this proposal.
Preserve the portable design and assurance boundary.

### 4.3 Add the PostgreSQL deployment artifacts

**Artifacts:** Kubernetes base/production deployment files and their focused tests.

Add:

- dedicated PostgreSQL Service, StatefulSet, ConfigMaps/bootstrap scripts, and 400 GiB
  `openebs-hostpath-retain` claim;
- official PostgreSQL 18.4 image pinned by digest;
- PostgreSQL 18 volume layout, readiness/liveness/startup probes, resource envelope, and
  hardened runtime identity;
- TLS CA/server material generation and secret-mount contract without committed values;
- dedicated database/schema/admin/runtime roles with minimum privileges;
- ingress/egress NetworkPolicies and no public Service;
- a fail-closed bootstrap/verification path that cannot touch `keycloak/postgres`.

Update `deploy/kubernetes/base/kustomization.yaml`,
`deploy/kubernetes/base/dapr/access-telemetry-store.yaml`, and
`deploy/kubernetes/base/access-telemetry-network-policy.yaml`. Keep
`ACCESS_TELEMETRY_ENABLED=false` and lifecycle/clock replicas at zero outside controlled
qualification runs.

### 4.4 Correct PostgreSQL v2 runtime compatibility

**Artifacts:** `DaprAccessTelemetryStateStore`, lifecycle tests, integration checkpoint,
and C1 verifier.

**Old:** `GetDueEntriesAsync` uses `DaprClient.QueryStateAsync` and Redis `queryIndexes`.

**New:** Explicit deterministic minute/shard expiry-bucket state committed through Dapr
transactions with the record, bounded due-bucket traversal, strong reads/ETags, exact
delete verification, and no Query API. Add focused two-writer collision and cross-tenant
negative evidence. This is the minimum compatibility work needed to exercise the selected
Dapr adapter, not a direct PostgreSQL dependency.

### 4.5 Update architecture, Story 27.4, and exact-profile guards

- Architecture access-telemetry summary: keep the backend-neutral contract and add the
  selected single-node profile's bounded assurance statement.
- Story 27.4 predecessor: replace `PG-AZ-1` with the immutable profile approved by Story
  27.3, currently `PG-ONPREM-1`; profile hash mismatch still returns ownership to 27.3.
- `AccessTelemetryRetentionDecisionTests`: bind the new ADR heading, profile ID, current
  stable PostgreSQL version/image pin, no-Query contract, and explicit non-HA language.
- C1 Python tooling/evidence: collect PostgreSQL image/version, StatefulSet/PVC/storage
  identity, node affinity, free-space measurement, TLS/network/role evidence, backup/
  restore result, fault transcript, and named approvals.
- Preserve the historical `PG-AZ-1` proposal and append-only story/debug ledger entries as
  historical evidence; do not rewrite prior facts.

### 4.6 Sprint status, PRD, and UX disposition

- `epic-27`: remains `in-progress`.
- `27-3-production-adapter-and-deployment-profile`: remains `in-progress`.
- `27-4-retention-verification-operations-runbook-and-a41-close-out`: remains `backlog`.
- A41 deferred/action state: unchanged.
- No story key is added, removed, or renamed; no structural sprint-status change is needed.
- PRD: no edit.
- UX design: no edit.

## 5. Implementation Handoff

### Product Owner / Administrator

- Approve or revise the replacement profile and the explicit single-node/non-HA risk.
- Confirm that installing the server with lifecycle writes disabled is the desired first
  outcome.
- Do not treat planning approval as Platform Operations/security approval or as A41
  closure.

### Developer / Maintainer

- Implement the manifests and expiry-bucket compatibility fix using existing repository
  patterns and focused tests.
- Preserve the Dapr-only product boundary; no application code may use a PostgreSQL SDK or
  connection string directly.
- Generate no secret value in source, command output, evidence, or logs.
- Deploy only to `jpiquot@local/hexalith-memories`; do not modify the Keycloak namespace.
- Keep Production writes disabled outside a bounded evidence run.

### Hexalith Platform Operations

- Own node/disk capacity, PostgreSQL operation, maintenance, monitoring, vacuum, upgrades,
  rollback, backup/restore, and outside-profile RPO/RTO.
- Supply or approve a backup destination before C1 approval.
- Run the pod/process fault and restore exercises, attach exact evidence, and approve the
  immutable profile only if the configured retention fits measured capacity.
- Acknowledge in writing that the profile has no node, disk, or site HA.

### Independent security reviewer

- Verify TLS `verify-full`, CA/key rotation, OpenBao scoping, Kubernetes Secret exceptions,
  least-privilege roles, component scopes, NetworkPolicy enforcement, encryption, image
  identity, evidence hashes, and cross-tenant denial.
- Approve separately from Platform Operations and bind approval to the same profile hash.

### Success criteria for Story 27.3 handoff

1. PostgreSQL 18.4 is Ready on the exact pinned image and retained 400 GiB profile.
2. No existing PostgreSQL, Redis, FalkorDB, OpenBao, or unrelated workload is reused or
   mutated as the new database.
3. Query API and `queryIndexes` are absent from the PostgreSQL lifecycle path.
4. Every C1 capability/load/fault/reclamation probe passes with zero skips and zero
   acknowledged-record loss for the bounded pod/process fault.
5. Measured 24-hour capacity fits the profile thresholds; durations outside the measured
   envelope fail admission.
6. Backup/restore evidence and outside-profile RPO/RTO are published without an HA claim.
7. Platform Operations and security approvals are separate, named, dated, and hash-bound.
8. Production lifecycle writes remain disabled until the terminal C1 verifier accepts the
   complete packet; Story 27.4 and A41 remain unchanged until then.

## 6. Change Navigation Checklist

| Item | Status | Finding |
| :-- | :-- | :-- |
| 1.1 Triggering story | [x] | Story 27.3 C1 exact Production-adapter gate. |
| 1.2 Core problem | [x] | Stakeholder correction: every target is on premises; the locked Azure profile is wrong. |
| 1.3 Supporting evidence | [x] | Live cluster/node/storage/Dapr/PostgreSQL inventory, current manifests, source incompatibility, and official PostgreSQL release/image data inspected. |
| 2.1 Current epic viability | [x] | Epic 27 remains viable with a profile substitution. |
| 2.2 Epic changes | [x] | Modify Story 27.3 target/fault/capacity contract; no new epic/story. |
| 2.3 Remaining epics | [x] | Story 27.4 consumes the new profile; Epics 28/29 unchanged. |
| 2.4 Obsolete/new epics | [N/A] | None. |
| 2.5 Order/priority | [x] | 27.3 still precedes 27.4. |
| 3.1 PRD conflict | [N/A] | No requirement or MVP change. |
| 3.2 Architecture conflict | [x] | ADR qualification appendix and architecture assurance wording require updates. |
| 3.3 UX conflict | [N/A] | No user-surface impact. |
| 3.4 Other artifacts | [x] | Kubernetes/Dapr/NetworkPolicy, runtime compatibility, tests, evidence, and operations guidance change. |
| 4.1 Direct adjustment | [x] | Viable; Medium installation effort, High full-qualification effort/risk. |
| 4.2 Rollback | [x] | Not viable or useful; portable Story 27.2 behavior remains the intended boundary. |
| 4.3 PRD/MVP review | [x] | Not needed; post-MVP operational profile only. |
| 4.4 Recommended path | [x] | Option 1, direct adjustment within Epic 27. |
| 5.1-5.5 Proposal components | [x] | Issue, impact, path, MVP disposition, action plan, and role handoff recorded. |
| 6.1 Checklist review | [x] | All applicable analysis sections addressed. |
| 6.2 Proposal accuracy | [x] | Cross-checked against the live target, current source/manifests, and official PostgreSQL sources. |
| 6.3 User approval | [x] | Administrator explicitly approved the complete proposal on 2026-07-20. |
| 6.4 Sprint-status update | [N/A] | No story/epic structural or status change is proposed. |
| 6.5 Handoff confirmation | [x] | Direct adjustment routed to Developer, Platform Operations, and independent security review. |

## 7. Approval and Routing Record

Administrator explicitly approved this proposal on 2026-07-20. It supersedes
`PG-AZ-1` as the active Story 27.3 qualification target and authorizes the listed
planning, repository, and fail-closed deployment work.

This planning approval does not certify `PG-ONPREM-1`, constitute either required C1
approval, enable lifecycle writes, mark Story 27.3 done, start Story 27.4, or close A41.
