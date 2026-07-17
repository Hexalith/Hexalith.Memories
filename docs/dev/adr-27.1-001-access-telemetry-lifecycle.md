# ADR 27.1-001: Access Telemetry Lifecycle

## Status and Decision Metadata

| Field | Proposed value |
| :---- | :-------------- |
| Status | Accepted |
| Decision date | 2026-07-17 |
| Approver | Administrator |
| Architecture owner | Hexalith.Memories maintainers |
| Operational lifecycle owner | Hexalith Platform Operations |
| Affected deployment | Production Kubernetes deployment owned by Kustomize |
| Selected family | Repository-owned dedicated write-only telemetry store |
| Selected technology | A separate Redis 7.4 access-telemetry workload using `redis/redis-stack-server:7.4.0-v8@sha256:798ab84d9f266936b034ab11c4d04a2b8e4b441884c5aa7d17ac951eefdf742a` |
| Implementation gate | Stories 27.2 and 27.3 are unblocked to implement and verify this accepted contract; neither implementation start nor ADR acceptance closes A41 |

This accepted decision defines a lifecycle target but does not implement it.
Current JSON-console emission and optional OTLP export remain the only shipped
paths. Story 27.2 implements the target; Story 27.3 verifies it in the
Production shape and owns the evidence-backed A41 close-out gate.

## Verified Current State

- `AccessTelemetryLog` emits nine success families (7501-7509) and nine error
  families (7511-7519) through `AccessTelemetryCategory` using typed
  `AccessTelemetryEvent` logger state.
- ServiceDefaults always registers OpenTelemetry logging and UTC JSON console.
  It registers the OTLP exporter only when `OTEL_EXPORTER_OTLP_ENDPOINT` is
  non-empty. An endpoint routes telemetry; it does not define retention.
- The production Server has two replicas, a read-only root filesystem, no OTLP
  endpoint or access-telemetry backend, and only an ephemeral `/tmp` `emptyDir`.
  Kubernetes removes `emptyDir` data with the Pod, while persistent volumes
  outlive individual Pods. [Kubernetes volumes](https://kubernetes.io/docs/concepts/storage/volumes/)
- No committed component owns access-record TTL, purge, persistent buffering,
  lifecycle health, or storage capacity.
- JSON console preserves an outer JSON envelope but formats `@AuditEvent` via
  record `ToString()`, so `QueryParams` values cannot be recovered. The future
  writer must consume typed logger state, not reparse stdout.
- Search and source-URI producers can currently place raw `query`, `subject`,
  and `sourceUri` values in `QueryParams`. Durable storage of those values is
  prohibited; Story 27.2 must sanitize them before enqueue.

## Options Evaluated

| Lifecycle field | Deployment-owned OpenTelemetry Collector plus Grafana Loki | Dedicated Redis access-telemetry workload | File or volume storage |
| :-------------- | :---------------------------------------------------------- | :---------------------------------------- | :--------------------- |
| Ownership and topology | Would require a newly owned Collector gateway, Loki deployment, object store, credentials, and on-call owner; none is committed. | Repository and Kustomize own a separate primary, replica, Sentinel quorum, credentials, PVCs, and policy. | Would require a new per-replica or concurrency-safe shared writer plus rotation and lifecycle controller. |
| Two-writer behavior | Collector can accept both Servers, but resource identity, queue/WAL, and backend deduplication are unspecified here. | Unique keys plus atomic `SET NX PXAT` accept concurrent Server writers without shared files or overwrite. | Current root is read-only and `/tmp` is per-Pod. A shared file has unresolved locking/rotation; per-replica files complicate complete purge evidence. |
| Durability and recovery | An endpoint alone is insufficient. Collector memory queues lose data on crash; a WAL and durable Loki backend would need explicit ownership. [Collector resiliency](https://opentelemetry.io/docs/collector/resiliency/) | AOF on two persistent data Pods, Sentinel failover, and `WAITAOF` acknowledgement give an executable durability boundary. Redis documents that `WAITAOF` confirms AOF fsync but does not make Redis strongly consistent. [Redis `WAITAOF`](https://redis.io/docs/latest/commands/waitaof/) | Current `emptyDir` is deleted on Pod removal. No durable volume, rotation recovery, or rescheduling contract exists. |
| Retention, expiry, purge, and clock | Loki retention and compactor settings could work, but no version, store, bound, purge cadence, or clock owner is committed. | Each event is a key with atomic absolute expiry; Redis expiry and server time are executable. [Redis `SET`](https://redis.io/docs/latest/commands/set/) and [`EXPIRE`](https://redis.io/docs/latest/commands/expire/) | Rotation by size/time is not record TTL. Physical deletion, late records, newer-record preservation, and executable purge proof remain custom work. |
| Failure and backpressure | Requires Collector queue size, retry age, WAL, overflow, and backend policy. Official guidance identifies full queues and Collector crashes as loss cases without persistent storage. | A bounded in-process queue, bounded retry age, Redis `noeviction`, and non-throwing logger-provider behavior can be owned and tested in Story 27.2. | File I/O, disk pressure, rotation races, and read-only-root failures would need a custom non-blocking provider and storage monitor. |
| Observability | Collector and Loki metrics exist, but the exact accepted-to-purged lifecycle and `NoData` contract are not repository-owned. | The application and lifecycle verifier can expose the required bounded signal set and Redis/PVC health without high-cardinality labels. | Custom queue, disk, rotation, per-replica, and purge instrumentation is required. |
| Privacy and tenant boundary | Collector processors could sanitize fields, but sending raw values before processing expands the privacy boundary. | Sanitization happens before enqueue; separate writer, lifecycle, and inspector ACLs keep the Server write-only. | Plain files increase inspection and accidental disclosure surface; per-node access controls and deletion evidence are unresolved. |
| Capacity and operating cost | Adds Collector, Loki, and object storage; capacity and cost are not presently sized or approved. | Adds two 512-GiB Redis data members with 1.5-TiB PVCs and three small Sentinel Pods. The ratified all-nine-operation envelope is 250 events/s cluster-wide; the 7-day projection uses 63.37% of configured Redis memory and 37.55% of each PVC including rewrite workspace. | Adds durable shared or per-replica volumes plus rotation/purge agents; cost is lower only if the unresolved correctness work is ignored. |
| Rollback | Removing a collector route can preserve console output, but queued/backend data ownership remains unspecified. | Disable the provider and roll back the Server while Redis continues expiring retained keys; never delete the store automatically. | Rolling back writers/rotators can strand files and stop purge unless another owner remains deployed. |
| Hard-gate result | Rejected: named products, but no repository-owned deployable retention contract. | Selected: lifecycle semantics, all-nine-operation capacity, resource cost, ownership, and bounded recovery gates are ratified below. | Rejected: fails the current two-replica, read-only-root, rescheduling, rotation, and executable-purge gates. |

## Selected Design and Rejected Alternatives

The selected design is a dedicated Redis 7.4 access-telemetry workload,
separate from the domain Redis and from Hexalith.EventStore. Story 27.2 will
deploy one
primary data Pod, one replica data Pod, and a three-Pod Sentinel quorum. Each
data Pod owns a persistent volume; the Server receives only a dedicated writer
credential. The implementation uses typed `AccessTelemetryEvent` state and one
unique Redis string key per sanitized record.

The selected image is the already pinned
`redis/redis-stack-server:7.4.0-v8@sha256:798ab84d9f266936b034ab11c4d04a2b8e4b441884c5aa7d17ac951eefdf742a`.
The workload does not use Search, Vector, Dapr state, pub/sub, or domain data;
the existing image is selected to avoid introducing an unreviewed image supply
chain in Story 27.2. A later image simplification requires its own validation.

The deployment shape accepted by this decision is two Redis data Pods, two
production PVCs, and three small Sentinel Pods. That footprint buys independent
credentials, persistence, failover, atomic per-record TTL, and executable
lifecycle evidence. Each data member has 512 GiB configured Redis memory inside
a 640-GiB Pod memory limit and one 1.5-TiB PVC. It must not be reduced by
sharing the domain Redis.

The Collector-plus-Loki option is rejected because the repository owns neither
deployment nor backend retention, and adding an endpoint would repeat the
current routing-without-lifecycle gap. File/volume storage is rejected because
the committed Server filesystem cannot retain it and no concurrency-safe,
rescheduling-safe rotation and purge implementation is available at the
decision gate.

## Ownership and Topology

Kustomize remains the Production authority. Story 27.2 will add a dedicated
two-replica `access-telemetry-redis` StatefulSet (one primary, one replica),
three Sentinel Pods with quorum two, a primary-discovery service, and one
`ReadWriteOnce` persistent volume claim per StatefulSet ordinal. The stable
ordinal and `volumeClaimTemplate` own each data member's identity; the two data
Pods never share a volume. Required Pod anti-affinity separates the data Pods
by `kubernetes.io/hostname`, and a `maxSkew: 1` topology-spread constraint
separates them by zone where the cluster exposes zones.

Each claim explicitly requests the Platform Operations-owned
`access-telemetry-retain` StorageClass. That class, not the PVC, owns
`reclaimPolicy: Retain`, `volumeBindingMode: WaitForFirstConsumer`, expansion,
the approved encrypted CSI provisioner, and its topology constraints. Story
27.2 deployment validation blocks when the class is absent or any property
differs. Replacement of a StatefulSet ordinal reattaches its existing claim.
If the bound volume cannot reattach, that member remains unready until an
operator either restores the volume or rebuilds the member from the surviving
primary onto an explicitly approved replacement claim; it never falls back to
`emptyDir`.

The three Sentinel Pods use required anti-affinity by hostname plus
zone-spread constraints and must occupy three independently failing nodes. The
rollout gate fails when three eligible failure domains are unavailable, because
placing two Sentinels on one node would let one node failure remove the majority
required to authorize failover.

A data-member `PodDisruptionBudget` sets `minAvailable: 1`, and rollout
automation permits only one voluntary data-member disruption at a time. Before
allowing a second voluntary disruption, the surviving or rebuilt replica must
report `master_link_status:up`, `master_sync_in_progress:0`, replication lag no
greater than 2 seconds, a successful AOF fsync, the accepted function-library
fingerprint, clock health, and an available Sentinel quorum. Failure of any gate
leaves the second disruption blocked; an operator cannot waive it as routine
maintenance evidence.

The lifecycle controller is a two-replica Deployment spread by hostname and
zone, with a `PodDisruptionBudget` of `minAvailable: 1`. It is active/passive:
Kubernetes Lease `access-telemetry-lifecycle-leader` elects one mutating leader,
and a Redis control function converts each acquired Lease UID/resourceVersion
into a monotonically increasing fencing epoch. Every purge, marker-rotation,
and compaction mutation carries that epoch; Redis rejects a stale epoch. The
standby performs read-only health observation. Controller state is reconstructible
from the expiry index plus versioned `access:control:v1:*` cohort, compaction,
rotation, and clock-attestation checkpoints in Redis. After restart or leader
loss, the new leader reacquires an epoch and resumes incomplete work
idempotently. Graceful termination stops new cohorts, checkpoints the current
cohort, and relinquishes the Lease; duplicate controllers cannot mutate under
the same or a stale epoch.

This workload is not a Dapr state store, pub/sub broker, vector index, domain
read model, or Hexalith.EventStore resource. It has an independent connection
string, Redis ACL file, Kubernetes Secrets, PVC capacity, alerts, and lifecycle
owner. The domain Redis credential must not authenticate to it.

Every client, replication, and Sentinel link is TLS-only (`port 0` on Redis)
with mutual certificate authentication and separate ACL credentials. Platform
Operations owns certificate issuance, rotation, and trust bundles. A
default-deny namespace `NetworkPolicy` permits only Server-to-Sentinel/data,
data-member replication, Sentinel quorum, lifecycle-controller, and authorized
operations-inspector flows; there is no public or cross-namespace data service.
The `access-telemetry-retain` StorageClass must attest CSI-backed encryption at
rest with its KMS key and rotation owner, and cluster Secret encryption protects
ACL, marker, and TLS material. Missing transport, network, or storage-encryption
evidence blocks deployment.

Authorities are deliberately separate:

| Authority | Owner | Allowed scope |
| :-------- | :---- | :------------ |
| `access-telemetry-writer` | Memories Server | Connect, health ping, call Redis `TIME`, invoke only the versioned write/marker-ack functions for `access:v1:*`, the exact `access:expiry:v1` index, and its own bounded writer-ack keys, and use `WAITAOF`; it cannot `GET`, `SCAN`, inspect, change configuration, or delete records. |
| `access-telemetry-lifecycle` | Hexalith Platform Operations lifecycle controller | Read the expiry index and versioned control checkpoints and invoke only the fenced purge/reconciliation, leader, clock, and marker-rotation functions for due `access:v1:*` records; it cannot change arbitrary Redis configuration or access domain Redis. |
| `access-telemetry-compactor` | Elected lifecycle-controller leader | Use `INFO persistence`, `BGREWRITEAOF`, and `BGSAVE` through a separately mounted credential, plus fenced compaction checkpoints; it cannot read record payloads, write event keys, or change Redis configuration. |
| `access-telemetry-inspector` | Authorized Hexalith Platform Operations responders | Read sanitized records and lifecycle metadata under `access:v1:*`; it cannot write, extend TTL, delete, or change configuration. |
| Redis administration | Hexalith Platform Operations | Manage Sentinel, ACLs, persistence, capacity, backup-free recovery, and emergency isolation; this credential is never mounted into the Server. |

The accepted operating footprint is two Redis data Pods, each requesting 8
vCPU with a 640-GiB memory limit and 512 GiB configured Redis memory, two
1.5-TiB PVCs, three 128-MiB Sentinel Pods, and two 256-MiB lifecycle-controller Pods.
The earlier 16-GiB memory and 32-GiB PVC values remain rejected planning
placeholders. The capacity evidence below is the deployment input.

## Multi-Replica Write and Durability Boundary

Both Server replicas discover the same Redis primary through Sentinel. Each
typed event receives a monotonic ULID record identifier in the provider. Its key
is `access:v1:<recordId>`; tenant, user, case, query, and source values never
appear in the key. Concurrent Servers therefore write disjoint keys without a
shared-file or rotation race.

The versioned Redis Functions library is loaded by the administrator before the
workload becomes eligible and is persisted in AOF/RDB and replicated to the
replica. The writer ACL allows only `TIME` and `FCALL` of named atomic write and
marker-ack functions over its write-only key patterns. The write function
validates the emission timestamp against Redis `TIME`, then atomically writes
the sanitized payload with absolute expiry and adds the key to
`access:expiry:v1`.

The writer submits a canonical immutable envelope that excludes
`acceptedAtUtc`; Redis creates that field from the same `TIME` sample only for
the first accepted write. The function persists both the completed record and
the SHA-256 digest of the submitted envelope. When the record key already
exists, it compares the submitted envelope digest, absolute expiry, and
expiry-index score, not newly generated acceptance time. An exact match is an
idempotent retry and returns the original `acceptedAtUtc`. Any difference
returns `record_id_conflict`, never overwrites or extends the existing record,
drops the incoming item, and makes lifecycle health unhealthy because it
indicates a record-identity or producer defect.

Both data Pods enable AOF with `appendfsync everysec`; RDB snapshots are enabled
for restart speed but are not the acknowledgement boundary. The primary uses
`min-replicas-to-write 1` and `min-replicas-max-lag 2`. The worker retains each
per-record function result: only `created` and `idempotent` results are
acknowledgement candidates; rejected or conflicting records are terminal and
never reported as persisted. After the batch, the provider issues
`WAITAOF 1 1 1500` on the same connection. It marks the candidate records
`persisted` only when both the primary and its one replica report that batch
fsynced to AOF. A timeout marks none of the candidates persisted; they remain
eligible for bounded retry, which returns idempotent results if the first
attempt reached Redis.

The durability boundary is the two retained PVCs. Within that boundary, the
acknowledged loss window is **0 seconds for any single Server Pod, Redis data
Pod, node, or one-PVC failure**, because acknowledgement follows fsync on both
data members. Redis and Sentinel are not a consensus store: simultaneous loss
or corruption of both data PVCs, storage-control-plane loss, or a disaster that
destroys the Kubernetes storage domain may lose all retained records and is
explicitly outside this boundary. The system makes no zero-loss claim for such
failures. Sentinel promotes the only eligible replica after a primary failure;
the returning member is rebuilt from the surviving primary.

The zero-loss claim requires Production-shaped evidence, not configuration
inspection alone. Story 27.3 must persist a manifest of acknowledged record
IDs, canonical-envelope hashes, and absolute expiries; then separately destroy
and simulate corruption of each current primary PVC after `WAITAOF`, force
promotion from the surviving PVC, and compare every manifest entry byte-for-byte
and expiry-for-expiry. It must provision an explicitly approved replacement
claim, complete full replica catch-up and the disruption gate, repeat with the
opposite ordinal, and prove new acknowledged writes survive. Any missing,
changed, or extended record invalidates the zero-loss boundary.

Every Redis data member and Memories Server writer must remain within 1 second
of an independent UTC reference and within 1 second of every other participating
member. Platform Operations owns a node time-synchronization monitor backed by
at least three approved upstream time sources. Each Server exposes only on its
private management port an mTLS-protected
`/internal/access-telemetry/clock` endpoint. It echoes a supplied nonce and
returns the Pod UID, process-start identifier, and current Unix milliseconds;
the default-deny NetworkPolicy permits only the lifecycle controller to call it.

Story 27.2 adds an `access-telemetry-clock-preflight` Kubernetes Job and keeps
the same logic running in the elected lifecycle controller once per minute. The
controller obtains a signed independent-reference token over mTLS, samples both
Redis members with `TIME`, and samples every ready Server writer through that
private endpoint. It emits canonical JSON containing schema version, unique
measurement ID, rollout UID, nonce, sampled/expiry times, the independent token
and its three-source quorum, all Pod UIDs/process IDs/Redis ordinals and measured
milliseconds, every computed delta, result, signer key ID, and signature. The
controller signs with its Platform Operations workload identity; consumers
verify both that signature and the nested independent-reference signature.

The accepted payload is stored atomically in the
`access-telemetry-clock-attestation` Kubernetes Lease annotation. Server service
accounts receive only `get`/`watch` on that one Lease. An attestation expires no
later than 90 seconds after sampling, is bound to the current rollout and exact
Pod identities, and is rejected if its measurement ID or sampled time does not
advance, its nonce or identity set differs, either signature is invalid, or the
independent reference is unavailable. These freshness, identity, and monotonic
rules prevent replay. The preflight Job must publish one accepted attestation
before lifecycle writes are enabled; it does **not** gate business readiness.
Static configuration errors and reachable remote mismatches still fail Server
startup, but a correctly configured unavailable sink or clock monitor leaves
business readiness available while lifecycle health fails closed.

Each writer watches the Lease, calls Redis `TIME` before every write batch, and
requires a fresh accepted attestation covering its Pod and both Redis members.
The controller continuously refreshes the independent comparison every minute;
a pairwise Server-to-Redis comparison alone is never sufficient. A missing,
stale, or violating attestation makes lifecycle health unhealthy, rejects
persistence with bounded reason `clock_attestation_unavailable`,
`producer_clock_skew`, or `redis_clock_skew`, and leaves the business service
and existing JSON-console/optional-OTLP emission running. Promotion triggers an
immediate fresh attestation recording the independent reference plus old and
new primary `TIME` values and deltas. Retention and purge evidence collected
during a violation is invalid and must be rerun after clock health is restored.
A clock-bound violation is an incident because Redis expiry itself is
wall-clock based and cannot be repaired after premature expiry.

## Retention, Expiry, Purge, and Clock

| Policy | Proposed value |
| :----- | :-------------- |
| Production default | 24 hours |
| Allowed minimum | 1 hour |
| Allowed maximum | 7 days |
| Configuration owner | Kustomize through `AccessTelemetryLifecycle__Retention` |
| Authoritative clock | Redis primary `TIME`, interpreted as UTC |
| Logical expiry | Absolute millisecond `PXAT` timestamp established atomically with the record write |
| Lifecycle sweep | Every 5 minutes |
| Physical-purge grace | No later than 15 minutes after logical expiry while the lifecycle health gate is healthy |

Production must set the retention value explicitly. Missing, blank, malformed,
below-minimum, above-maximum, zero, negative, or infinite values fail Production
startup before serving requests. No code path substitutes an unbounded TTL.
Development and tests may use the 24-hour default; tests may inject an internal
short duration only through test composition, never Production configuration.

Age begins at `AccessTelemetryEvent.Timestamp`, the event-emission instant,
preserved to Unix-millisecond precision.
Redis `TIME` is authoritative for acceptance and expiry enforcement. Emission
timestamps more than 1 second in the future are rejected as
`future_clock_skew`. Records whose emission time plus retention is already due
are rejected as `stale_before_acceptance`. A late record within the window gets
only its remaining lifetime; acceptance and retry never reset its age. An
emission timestamp no more than 1 second ahead is accepted with its declared
expiry, making 1 second the only clock-skew extension. This is the same bound as
the continuous writer/member/reference clock gate; there is no separate
two-minute exception.

At `PXAT`, Redis treats the record as logically absent. Redis active expiry and
the five-minute lifecycle sweep perform keyspace reclamation. The dedicated
workload sets `lazyfree-lazy-expire no`, `lazyfree-lazy-server-del no`, and
`lazyfree-lazy-user-del no`; cohort completion never depends on the
workload-global `lazyfree_pending_objects` gauge. The expiry-index score is the
absolute expiry timestamp in Unix milliseconds. Each fenced purge-function
invocation obtains Redis `TIME` itself, selects at most 512 due entries, invokes
synchronous `DEL` only for records with a score at or before that time, and
removes only those due entries from the index. It never selects or rewrites a
newer record.

The function records a cohort ID, fencing epoch, candidate count, synchronous
delete count, already-absent count, index-removal count, start/end Redis times,
and terminal result in `access:control:v1:purge:<cohortId>`. Because both active
expiry and explicit `DEL` use synchronous deallocation in this workload, a
successful function return proves Redis object-memory reclamation for that
exact candidate cohort; it does not claim allocator RSS was returned to the
operating system. The controller records `expired` for logical absence and
`purged` only after the cohort checkpoint proves every candidate is absent and
its index member removed. That cohort-specific proof must complete no later
than 15 minutes after logical expiry while the health gate is healthy.

The controller measures every function round trip against a 100-millisecond
execution budget and resumes due work after a bounded 25-to-100-millisecond
backoff. A budget overrun or an oldest due entry older than 15 minutes makes
lifecycle health unhealthy; catch-up continues resumably without increasing the
512-entry limit.

The 15-minute purge bound covers synchronous Redis key/index object removal.
AOF rewrite and RDB snapshot replacement have a separate 24-hour compaction
bound: at least one successful AOF rewrite and one replacement RDB snapshot
that postdate each purged cohort must complete within 24 hours. The elected
controller leader owns the separate `access-telemetry-compactor` credential.
It coalesces completed cohorts behind the earliest uncompacted cohort, invokes
`BGREWRITEAOF` and then `BGSAVE` when Redis reports no conflicting persistence
job, and stores the requested/start/completion timestamps and Redis persistence
identifiers in a fenced `access:control:v1:compaction` checkpoint. On failure or
leader restart, it reconstructs the earliest uncompacted cohort and retries
with bounded backoff; a stale leader cannot trigger or complete a checkpoint.
A missed compaction bound is unhealthy and means on-disk reclamation cannot be
claimed even when active keys are gone. Story 27.3 must verify active purge and
controller-triggered persisted-file compaction as separate evidence.

The existing 5 GB/day estimate is search-only planning input and is not used in
the accepted calculation. The all-nine-operation inputs, per-family fixture
measurements, overhead budgets, retention projections, outage recovery, and
approved resource cost are ratified in the next section. Deployment validation
rejects a retention/capacity pair whose projection exceeds 50% of PVC or 70% of
configured Redis memory. Capacity warns at 70%, is critical at 80%, and sets
lifecycle health unhealthy at 90%; `maxmemory-policy noeviction` forbids silent
deletion of newer records.

## Capacity Evidence and Admission Envelope

Administrator ratifies this owner-approved admission envelope for the committed
two-Server Production shape. It is a capacity contract, not an assertion that
current traffic already reaches every ceiling. The search input preserves the
existing high-end assumption of 10 requests/s for each of 10 active tenants on
each replica. Hexalith Platform Operations approves explicit ceilings for the
other eight families so every audited operation is represented. Kustomize must
declare these nine inputs; raising any family or the cluster total requires the
same calculation with an equal or larger resource reservation before rollout.

### Operation Envelope

| Operation | Per-replica events/s | Cluster events/s | Average sanitized bytes | P95 sanitized bytes |
| :-------- | -------------------: | ---------------: | ----------------------: | ------------------: |
| search | 100 | 200 | 867.8 | 893 |
| ingest | 3 | 6 | 833.8 | 859 |
| traverse | 5 | 10 | 810.8 | 836 |
| case-access | 8 | 16 | 788.8 | 814 |
| delete | 1 | 2 | 770.8 | 796 |
| tenant-lifecycle | 0.1 | 0.2 | 732.8 | 758 |
| tenant-config | 0.4 | 0.8 | 730.8 | 756 |
| case-member | 2 | 4 | 771.8 | 797 |
| annotation | 5.5 | 11 | 809.8 | 835 |

The measurement fixture is executable through
`AccessTelemetryRetentionDecisionTests`. For each family it serializes 100
compact UTF-8 records under the allowlisted persisted schema: 90 success and 10
bounded `internal_dependency_failure` records, fixed-length ULID/HMAC/W3C
identifiers and envelope hashes, both optional markers where the family permits them, and the
largest approved bounded parameter combination for that family. Average is the
arithmetic mean and P95 is nearest-rank item 95. These are deterministic
contract-fixture measurements rather than sampled Production percentiles. The
provider rejects a serialized persisted payload over 1,024 bytes, so the
capacity calculation remains bounded even when the fixture mix changes.

Every retained event receives a 1,536-byte per-record Redis memory budget:
1,024 bytes for the capped payload and 512 bytes for the key, dictionary/string
objects, absolute-expiry metadata, and expiry-index member/score. A 1.50
fragmentation multiplier covers allocator and data-structure variation. Each
event also receives a 4,096-byte per-record PVC budget covering the live AOF and
RDB representation, one simultaneous rewrite/snapshot workspace, incremental
AOF tail, and safety margin. Story 27.2 must measure `MEMORY USAGE`, AOF, RDB,
and fragmentation with the same fixture and may reduce neither budget; an
observed excess requires capacity increase or a new decision, not optimistic
recalculation.

For any configured retention `H` from 1 through 168 hours, the formulas are:

- events = `250 × 3,600 × H`;
- Redis memory = `events × 1,536 × 1.50 / 2^30` GiB, or `1.9312 × H` GiB;
- PVC including rewrite workspace = `events × 4,096 / 2^30` GiB, or
  `3.4332 × H` GiB.

### Retention Sizing

| Retention | Retained events | Redis memory per data member | PVC workspace per data member | 512-GiB memory utilization | 1.5-TiB PVC utilization |
| :-------- | --------------: | ---------------------------: | ----------------------------: | -------------------------: | -----------------------: |
| 1 hour | 900,000 | 1.93 GiB | 3.43 GiB | 0.38% | 0.22% |
| 24 hours | 21,600,000 | 46.35 GiB | 82.40 GiB | 9.05% | 5.36% |
| 7 days | 151,200,000 | 324.44 GiB | 576.78 GiB | 63.37% | 37.55% |

The maximum is below both admission gates: 63.37% is below 70% of 512 GiB,
and 37.55% is below 50% of the 1.5 TiB PVC. The approved operating-cost envelope
is therefore two 8-vCPU/640-GiB data Pods with 512 GiB configured Redis memory
each, two 1.5-TiB encrypted retained PVCs, three 128-MiB Sentinel Pods, and two
256-MiB lifecycle-controller Pods. This provider-neutral schedulable reservation is the
cost ceiling accepted by the approver and Platform Operations; a deployment
whose infrastructure quote cannot fund it must reduce retention within the
allowed range or obtain a revised decision, never silently undersize it.

The two Server queues are sized independently. At 125 events/s per replica,
the 8,192-record limit is reached after 65.5 seconds and consumes only 12 MiB at
the 1,536-byte budget, so it binds before the 64-MiB byte limit. The accepted
writer failure scenario is therefore a 60-second writer-sink outage. On
recovery, a conservative 256-record batch per 1.5-second `WAITAOF` timeout
processes 170.7 events/s per replica; after new traffic, 45.7 events/s remains
to drain 7,500 queued records in 164.2 seconds. The oldest record is persisted
within 224.2 seconds of emission, below the five-minute retry-age cap.

For purge, the accepted scenario is a 10-minute lifecycle-controller outage
while all 250 events/s continue, creating 150,000 due entries. One 512-record
invocation per 100-millisecond execution budget plus the maximum 100-millisecond
backoff yields 2,560 records/second. After concurrent arrivals, net catch-up is
2,310 records/second and drains the backlog in 65 seconds. Oldest-due age is
therefore at most 11 minutes 5 seconds, below the 15-minute active-purge bound.
Story 27.3 must reproduce both outage calculations in the deployed shape.

## Failure, Backpressure, Recovery, and Capacity

The provider uses a non-blocking bounded queue with both an 8,192-record limit
and a 64-MiB serialized-byte limit. `ILogger.Log` performs validation,
sanitization, and `TryWrite`; it never waits for Redis. A full queue drops the
new record with reason `queue_full` while existing JSON-console and optional
OTLP emission continue.

Story 27.2 registers the lifecycle provider with a provider-specific
`Information` filter for `AccessTelemetryCategory`. Console and OTLP providers
may use their own `Warning` filters, but a global category filter must not
suppress success events before the lifecycle provider sees them. This rule is
provider/category scoped, not tenant scoped: a single Server category cannot
apply different logging thresholds to different tenants.

The worker batches at most 256 records or 1 MiB. Because the persisted-schema
gate caps each canonical record at 1,024 UTF-8 bytes, the record bodies total at
most 256 KiB; the implementation must also measure RESP framing and reject a
batch before its complete encoded command exceeds 1 MiB. Retry uses exponential backoff
with full jitter from 100 milliseconds to 5 seconds, capped by 5 minutes from
event emission and by the record's absolute expiry. Shutdown receives 5 seconds
to flush. Anything remaining is dropped as `shutdown_timeout`; no local disk
spill is allowed. Recovery reconnects through Sentinel and drains retained
queue entries without changing their identifiers, emission times, or expiry.

Connection failures, primary failover, `WAITAOF` timeout, AOF errors,
`OOM command not allowed`, PVC exhaustion, function-version mismatch, and
sanitization failures have bounded reason codes. Attempts are retried only while
eligible; terminal items are counted as dropped. Redis `noeviction` prevents
capacity pressure from converting into silent loss. A transient or permanent
lifecycle failure may degrade or drop access records, but no provider exception
may escape `ILogger.Log`, block indefinitely, alter an HTTP response, or fail a
business operation.

Production configuration validity is different from sink availability. Static
validation of retention, queue limits, endpoint shape, required credential and
certificate references, exact Redis 7.4.0/image digest, contract epoch, and the
versioned function-library fingerprint completes before serving requests. A missing or
malformed static value fails startup.

Remote validation authenticates, verifies TLS identity, requires Redis
`redis_version:7.4.0`, confirms Kustomize admission attested the exact selected
image digest, confirms the exact persisted/replicated function-library
fingerprint, and proves the writer ACL includes only the accepted functions,
key patterns, `TIME`, `PING`, and `WAITAOF`. When Redis is reachable during
startup, any remote mismatch fails startup. When Redis is unreachable, those facts are
indeterminate rather than presumed valid: the business service starts,
JSON-console and optional OTLP emission continue, lifecycle health is
`Unhealthy` with reason `remote_validation_pending`, and the bounded queue and
retry-age limits determine loss without local-disk fallback.

The first successful connection must complete remote validation before any
write. Bad credentials, TLS identity, Redis version, function fingerprint, or
ACL then transitions the provider to terminal `configuration_invalid`, drops
queued and new lifecycle items with that bounded reason, stops connection/write
retries, and raises an operator alert; it never changes a business response.
Correction requires an explicit Server restart so startup validation is rerun.
Every new physical connection after disconnect must repeat TLS identity, Redis
7.4.0, ACL, and function-fingerprint validation before any write. A change to
the declared contract epoch, image attestation, credential Secret generation,
certificate generation, or function fingerprint drains the connection pool and
forces the same validation. A mismatch after reconnect or epoch change enters
terminal `configuration_invalid`; mere unreachability continues bounded retry.
A validated sink that later becomes unavailable instead follows the ordinary
bounded queue/retry policy and may recover automatically only after successful
revalidation. Startup never waits indefinitely for Redis and never falls back
to local disk.

## Observability

Hexalith Platform Operations owns these signals. The counter
`memories.access.telemetry.lifecycle.records` uses the bounded `state` values
**accepted**, **rejected**, **enqueued**, **persisted**, **retried**, **failed**,
**dropped**, **expired**, and **purged**. Optional `reason` values come from a
code-owned finite enum. Accepted means a typed event passed schema, timestamp,
and privacy validation; persisted means the `WAITAOF` boundary passed; failed
is an individual sink attempt; dropped is terminal; expired is logical absence;
purged means the fenced cohort checkpoint proves synchronous key/index object
removal for every candidate.

Additional gauges are queue records, queue bytes, oldest queued age, Redis
connectivity, Sentinel primary availability, AOF health, replica fsync lag,
memory and PVC utilization, expiry-index depth, oldest due age, purge-cohort
completion age, and earliest uncompacted cohort age. Health has `Healthy`, `Degraded`, `Unhealthy`, and
`NoData`. Health aggregation first evaluates configuration, validation, clock,
connectivity, persistence, capacity, purge, and compaction. `Unhealthy` takes
precedence over `Degraded`, which takes precedence over data-presence
evaluation. `NoData` is emitted only when the provider is enabled and every
sink/lifecycle check is otherwise `Healthy`, but no accepted or rejected access
event was observed in the last 15 minutes. A disconnected or unvalidated sink
with no events is therefore `Unhealthy`, never `NoData`. `NoData` is not
evidence of a healthy write path and alerts during an expected-traffic window.

Metric labels must never contain tenant, user, case, memory-unit, query,
subject, source, trace, span, or record identifiers. Health endpoints expose
only bounded state, reason, capacity percentages, and ages, not record payloads
or credentials.

## Privacy and Tenant Boundary

Sanitization occurs synchronously on typed logger state before enqueue, so raw
values never enter the new queue, retry state, Redis command, or metric path.
The persisted JSON schema is allowlisted:

| Persisted field | Policy |
| :-------------- | :----- |
| `recordId`, `schemaVersion`, `eventId` | Generated/bounded identifiers and schema fields. |
| `envelopeHash` | Lowercase SHA-256 of the canonical immutable envelope excluding `acceptedAtUtc` and `envelopeHash`; used for exact retry comparison. |
| `emittedAtUtc`, `acceptedAtUtc`, `expiresAtUtc` | UTC lifecycle timestamps; acceptance comes from Redis time. |
| `operationType`, `outcome`, `errorCode` | Values validated against bounded catalogs. |
| `markerKeyId` | Bounded non-secret identifier for the lifecycle marker key used to derive this record's markers. |
| `tenantMarker` | Full HMAC-SHA-256 of canonical tenant ID using the versioned lifecycle marker key identified by `markerKeyId`; `__rejected__` remains a bounded synthetic marker. |
| `userMarker`, `caseMarker` | Optional full HMAC-SHA-256 markers using the same versioned key and type-specific domain separators. |
| `queryParams` | Allowlisted booleans, bounded enums, counts, sizes, and weights only. Raw `query` becomes `queryLengthBucket`; raw `subject` becomes `subjectPresent`; raw `sourceUri` becomes the bounded `sourceKind` enum. |
| `resultCount`, `durationMs` | Non-negative bounded numeric values. |
| `traceId`, `spanId` | Validated W3C identifiers for authorized operational correlation. |

### Persisted Schema Bounds

Version 1 is canonical UTF-8 JSON under RFC 8785 ordering and number rules,
with no insignificant whitespace and exactly the allowlisted fields above.
Optional `userMarker`, `caseMarker`, `resultCount`, and `errorCode` are encoded
as JSON `null`, not omitted; `queryParams` is always an object. Unknown,
duplicate, incorrectly cased, non-scalar, or noncanonical fields fail closed.
The complete serialized record must be at most 1,024 UTF-8 bytes.

- `schemaVersion` is integer `1`; readers reject every other version. A field,
  bound, or version change requires a new accepted decision, dual-version
  reader/inspector tests for the maximum retention window, and an explicit
  retirement gate. Unknown fields are never silently ignored.
- `recordId` is a 26-character uppercase Crockford ULID. `eventId` is exactly
  7501-7509 for `ok` and the corresponding 7511-7519 value for `error`.
  `operationType` is exactly `search`, `ingest`, `traverse`, `case-access`,
  `delete`, `tenant-lifecycle`, `tenant-config`, `case-member`, or `annotation`.
  `outcome` is `ok` or `error`.
- `errorCode` is `null` for `ok`; error inputs map to exactly one of
  `invalid_input`, `not_found`, `forbidden`, `conflict`, `cancelled`,
  `dependency_unavailable`, `rate_limited`, `internal_dependency_failure`,
  `internal_failure`, or `unknown`. Raw exception type/message and arbitrary
  producer codes are never persisted.
- `markerKeyId` matches `[a-z0-9][a-z0-9-]{0,31}`. HMAC markers are exactly 64
  lowercase hexadecimal characters; only `tenantMarker` may instead be the
  literal `__rejected__`. `traceId` is 32 and `spanId` is 16 lowercase
  hexadecimal characters. `envelopeHash` is 64 lowercase hexadecimal
  characters.
- All timestamps use the fixed UTC millisecond form
  `yyyy-MM-ddTHH:mm:ss.fffZ`. `durationMs` is an integer from 0 through
  86,400,000; `resultCount` is `null` or an integer from 0 through 1,000,000.
  Floating-point values, negative values, and numeric strings are rejected.
- `queryParams` contains at most six lexicographically ordered keys, with only
  the per-operation keys and values in the following table. No free text,
  nested object, array, URI, identifier, or unlisted enum is accepted.
- At 256 records, the 1,024-byte record cap leaves at least 768 KiB of the
  1-MiB command limit for framing; Story 27.2 rejects a batch before its
  complete encoded command exceeds 1 MiB.

#### Query Parameter Bounds

| Operation | Exact bounded `queryParams` contract |
| :-------- | :----------------------------------- |
| `search` | `axis`: `lexical`, `semantic`, `hybrid`; `caseScope`: `single`, `all-authorized`; `explain`: boolean; `limitBucket`: `1-10`, `11-25`, `26-50`, `51-100`; `queryLengthBucket`: `0`, `1-32`, `33-128`, `129-256`, `257-1024`, `1025+`; `weightProfile`: `lexical`, `semantic`, `balanced`. |
| `ingest` | `batchSizeBucket`: `1`, `2-10`, `11-100`, `101-256`; `contentKind`: `document`, `text`, `image`, `audio`, `unknown`; `contentLengthBucket`: `0`, `1-64KiB`, `64KiB-1MiB`, `1-10MiB`, `10MiB+`; `sourceKind`: `upload`, `url`, `directory`, `text`, `unknown`. |
| `traverse` | `depthBucket`: `1`, `2`, `3`, `4`, `5`; `direction`: `in`, `out`, `both`; `edgeTypeCount`: integer `0..16`; `includeGaps`: boolean. |
| `case-access` | `accessKind`: `case`, `memory-unit`, `relation`; `projection`: `summary`, `detail`. |
| `delete` | `cascade`: boolean; `targetKind`: `case`, `memory-unit`, `relation`, `annotation`. |
| `tenant-lifecycle` | `action`: `provision`, `suspend`, `resume`, `delete`; `resourceCountBucket`: `0`, `1`, `2-3`, `4-8`, `9+`. |
| `tenant-config` | `action`: `create`, `update`, `delete`; `changedFieldCountBucket`: `0`, `1`, `2-3`, `4-8`, `9+`. |
| `case-member` | `action`: `add`, `update`, `remove`; `role`: `reader`, `editor`, `owner`. |
| `annotation` | `action`: `create`, `update`, `delete`; `annotationKind`: `note`, `correction`, `warning`; `subjectPresent`: boolean. |

The immutable envelope consists of every persisted field except
`acceptedAtUtc` and `envelopeHash`. The writer canonicalizes and hashes it;
Redis recomputes and verifies the hash, supplies `acceptedAtUtc` once, and
persists the completed canonical record. At 256 records, the 1,024-byte record
cap leaves at least 768 KiB of the 1-MiB command limit for framing; Story 27.2
must enforce both the per-record and fully encoded batch limits.

The marker secret is separate from Redis credentials and domain secrets. Raw
tenant, user, case, query, subject, source URI, payload, token, authorization
header, credential, exception, and unbounded metadata values are prohibited.
Schema rejection is fail closed for persistence and does not alter the request.
Marker rotation is a fenced two-phase protocol stored under
`access:control:v1:marker-rotation`. First, the controller stages a new Secret
generation and key ID while the old ID remains active. Both currently ready
writer Pod UIDs must load the generation and acknowledge it through the
marker-ack function. The controller then publishes a quiesce generation: every
writer atomically switches new events to the new key, acknowledges the switch,
and drains or expires all old-key queue/retry work. Redis records the last
successful write time for each key ID. Only after both exact Pod UIDs report no
old-key work may the fenced controller commit retirement; the Redis write
function then rejects the retired generation, preventing a stale or restarted
writer from adding a late old-key record. Missing writers, lost acknowledgements,
or leader change block and resumably reconstruct the rotation rather than
guessing completion.

Previous verification keys remain available from their Redis-recorded final
successful write for the 7-day maximum retention, plus the accepted 1-second
future-skew bound, plus the 15-minute active purge grace: at least 7 days,
15 minutes, and 1 second. A restarted writer must load the current committed
generation before enabling lifecycle writes. Every persisted marker carries
`markerKeyId`; missing, retired-for-write, or unknown key identifiers fail
closed and make lifecycle health unhealthy.

The Server has no read or arbitrary-delete authority. There is no tenant-facing
read API. Authorized inspection uses the separate inspector credential and
returns sanitized records only. Tenant markers preserve per-tenant correlation
without making tenant identifiers Redis keys or metric labels. Tenant deletion
does not erase retained infrastructure telemetry early; its opaque markers
remain only until their normal bounded expiry. This is an operational behavior,
not a legal-erasure or compliance claim.

Story 27.2 must attach negative evidence that a tenant-A request cannot inspect,
select, or label tenant-B records and that denial happens before the lifecycle
dependency. Preserve the Story 20.2 guards
`TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState`,
`SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies`,
and
`TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies`.
Preserve the Story 24.3 verifier guards
`VerifyAsync_DetectsSyntacticTenantIdMismatch_ReturnsFailed`,
`VerifyAsync_DetectsSemanticTenantIdMismatch_ReturnsFailed`, and
`VerifyAsync_DetectsMissingSemanticTenantId_ReturnsFailed`.

## Rollback and Transition

Story 27.2 introduces an independently switchable lifecycle provider. Existing
JSON-console emission and optional OTLP export remain enabled and unchanged
during rollout, failure, and rollback. Disabling the provider or rolling the
Server back to its old image stops new persistent lifecycle writes but does not
change business behavior.

Rollback never deletes the Redis workload, PVCs, credentials, or retained
records automatically. Redis and the lifecycle controller continue expiring
and purging existing records through their original absolute timestamps. A
separate, approved decommission operation may remove empty retained storage
after the maximum retention window and verification.

An old Server image cannot meet the proposed lifecycle target. Rollback is
therefore an explicit degraded incident state with an alert and owner, not an
acceptable steady state and not evidence that A41 is closed.

## Assurance Boundary

**Bounded infrastructure telemetry only; no tamper evidence, append-only
integrity, legal compliance, or certified audit retention.**

Redis administrators can modify data; sanitized records are operational
telemetry, not a cryptographic or legal audit ledger. The selected lifecycle
does not make retention certified, immutable, non-repudiable, or compliant with
any named regulation.

## Story 27.2 Implementation Handoff

Story 27.2 is unblocked by this accepted decision and owns this exact
implementation map:

1. Add options and Production startup validation for enablement, explicit
   1-hour-to-7-day retention, queue/retry/flush limits, capacity inputs,
   Sentinel endpoint, certificates, credentials, the exact Redis 7.4.0 version
   and selected image digest, contract epoch, and the persisted/replicated
   Redis Functions library fingerprint. Revalidate identity, ACL, version,
   image attestation, and fingerprint before writes on every reconnect or
   contract-epoch change.
2. Add a typed `AccessTelemetryEvent` logger provider, sanitizer, bounded queue,
   provider-specific `Information` filter, worker, stable ULID record identity,
   Redis-time validation, canonical envelope/hash, Redis-owned acceptance time,
   atomic write-plus-expiry/index function, retry-safe conflict detection,
   per-result batch tracking, `WAITAOF` acknowledgement, bounded retry, and
   non-throwing failure isolation. Enforce the 1,024-byte record and complete
   1-MiB encoded-batch limits. Do not parse stdout.
3. Add the exact Version 1 catalogs, encodings, field/query bounds,
   `markerKeyId`, and versioned HMAC key ring. Implement the fenced staged,
   quiesced, all-writer-acknowledged rotation barrier and retain old verification
   keys from the Redis-recorded final write through maximum retention, the
   one-second future-skew bound, and purge grace. Prove raw `query`, `subject`,
   and `sourceUri` never enter queue or Redis payloads.
4. Add the complete accepted/rejected/enqueued/persisted/retried/failed/dropped/
   expired/purged counter, bounded reason catalog, gauges, health states, and
   `NoData` behavior without high-cardinality labels.
5. Add the independent Kustomize Redis primary/replica/Sentinel resources,
   stable StatefulSet/PVC identity, the validated `access-telemetry-retain`
   StorageClass, required data/Sentinel failure-domain spreading, AOF/RDB and
   synchronous purge configuration, `noeviction`, exact function/command-scoped
   ACL roles including writer `TIME`, TLS-only links, default-deny NetworkPolicy,
   encrypted Secrets/PVC evidence, capacity validation, data/controller
   disruption budgets and catch-up gates, the private writer-clock endpoint,
   signed preflight Job/Lease protocol, probes, and two-replica fenced
   active/passive lifecycle controller with resumable control checkpoints and
   controller-triggered AOF/RDB compaction. Do not reuse domain Redis, Dapr
   state, pub/sub, or Hexalith.EventStore.
6. Add unit and integration tests for two concurrent writers, idempotent retry,
   Redis-owned acceptance time, record-ID conflict, Redis `TIME`, continuous
   signed independent-UTC attestations and replay/freshness/identity negatives,
   Server/member clock bounds, one-second skew/late arrival, exact millisecond
   expiry, fenced 512-record synchronous purge catch-up/newer preservation,
   leader failover/reconstruction, marker-rotation barriers, controller-triggered
   compaction, queue overflow, retry exhaustion, shutdown, per-result batch
   acknowledgement, AOF/replica acknowledgement, failover, disruption catch-up,
   capacity, provider-specific filtering, invalid configuration, startup sink
   unavailability followed by valid and invalid first connections, reconnect
   and contract-epoch revalidation, health-state precedence, and provider
   exception isolation.
7. Attach focused cross-tenant denial and privacy-negative results using the
   named Story 20.2 and Story 24.3 guards from the privacy section. No happy-path
   or broad-suite result substitutes for that negative evidence.

## Story 27.3 Verification and Operations Handoff

After Story 27.2 implementation, Story 27.3 must supply Production-shaped
evidence and the operations contract:

1. Start two Server writers against the dedicated workload and prove unique,
   sanitized records persist through the `WAITAOF` boundary without duplicate
   or cross-tenant selection.
2. Restart each Server, the Redis primary, the replica, and the lifecycle
   controller; reschedule each Redis data Pod and prove its PVC reattaches.
   Exercise one data-node failure and each independent Sentinel-node failure,
   then promotion and recovery, while JSON-console and optional OTLP emission
   continue. Separately destroy and corrupt each primary PVC after acknowledged
   writes, recover every manifest record and exact expiry from the survivor,
   rebuild an approved replacement, pass replica catch-up, and repeat with the
   opposite ordinal before accepting the zero-loss claim.
3. Use deterministic emitted timestamps and Redis time to prove the minimum,
   default, maximum, late-arrival, future-skew, member-clock, promotion-delta,
   continuous independent-attestation freshness/replay/identity, and
   already-expired bounds. Prove millisecond logical expiry, bounded fenced
   512-record catch-up, cohort-specific synchronous purge within 15 minutes,
   controller-triggered AOF/RDB compaction within 24 hours, expiry-index cleanup,
   controller restart reconstruction, and preservation of records whose expiry
   is later.
4. Exercise Redis outage, Sentinel unavailability, queue/byte exhaustion,
   `WAITAOF` timeout, AOF failure, `noeviction`/memory pressure, PVC pressure,
   malformed configuration, bad credentials, wrong image/version, ACL or
   function mismatch, pending remote validation, reconnect/contract-epoch
   revalidation, stale leader fencing, retry-age exhaustion, and shutdown
   timeout. Verify business requests and business readiness remain available
   during correctly configured sink/clock unavailability and never receive a
   provider exception.
5. Prove the full lifecycle signal set, capacity thresholds, bounded labels,
   health transitions, alerts, and `NoData` semantics from accepted through
   physical purge.
6. Prove inspection least privilege, write-only Server credentials, TLS on
   client/replication/Sentinel links, NetworkPolicy denials, encrypted PVC/Secret
   ownership, sanitized tenant correlation, raw query/subject/source absence,
   denial before lifecycle dependencies, and no tenant-facing read route.
7. Publish the operations runbook for rollout, capacity calculation, alerts,
   failover, recovery, inspection, credential rotation, rollback, degraded old
   image behavior, and verified decommission after the maximum window.
8. Only after all evidence passes, coordinate the A41 deferred entry and action
   close-out. Story 27.1 scheduling or ADR acceptance alone is never closure.
