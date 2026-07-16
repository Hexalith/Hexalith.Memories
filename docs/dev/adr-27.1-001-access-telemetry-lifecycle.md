# ADR 27.1-001: Access Telemetry Lifecycle

## Status and Decision Metadata

| Field | Ratified value |
| :---- | :-------------- |
| Status | Accepted |
| Decision date | 2026-07-16 |
| Approver | Administrator |
| Architecture owner | Hexalith.Memories maintainers |
| Operational lifecycle owner | Hexalith Platform Operations |
| Affected deployment | Production Kubernetes deployment owned by Kustomize |
| Selected family | Repository-owned dedicated write-only telemetry store |
| Selected technology | A separate Redis 7.4 access-telemetry workload using the repository-pinned `redis/redis-stack-server:7.4.0-v8` image digest |
| Implementation gate | Stories 27.2 and 27.3 must implement and verify this contract before A41 can close |

The decision adds a lifecycle target; it does not claim that the target is
implemented. Current JSON-console emission and optional OTLP export remain the
only shipped paths until Story 27.2 completes.

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
| Two-writer behavior | Collector can accept both Servers, but resource identity, queue/WAL, and backend deduplication are unspecified here. | Unique keys plus atomic `SET NX EXAT` accept concurrent Server writers without shared files or overwrite. | Current root is read-only and `/tmp` is per-Pod. A shared file has unresolved locking/rotation; per-replica files complicate complete purge evidence. |
| Durability and recovery | An endpoint alone is insufficient. Collector memory queues lose data on crash; a WAL and durable Loki backend would need explicit ownership. [Collector resiliency](https://opentelemetry.io/docs/collector/resiliency/) | AOF on two persistent data Pods, Sentinel failover, and `WAITAOF` acknowledgement give an executable durability boundary. Redis documents that `WAITAOF` confirms AOF fsync but does not make Redis strongly consistent. [Redis `WAITAOF`](https://redis.io/docs/latest/commands/waitaof/) | Current `emptyDir` is deleted on Pod removal. No durable volume, rotation recovery, or rescheduling contract exists. |
| Retention, expiry, purge, and clock | Loki retention and compactor settings could work, but no version, store, bound, purge cadence, or clock owner is committed. | Each event is a key with atomic absolute expiry; Redis expiry and server time are executable. [Redis `SET`](https://redis.io/docs/latest/commands/set/) and [`EXPIRE`](https://redis.io/docs/latest/commands/expire/) | Rotation by size/time is not record TTL. Physical deletion, late records, newer-record preservation, and executable purge proof remain custom work. |
| Failure and backpressure | Requires Collector queue size, retry age, WAL, overflow, and backend policy. Official guidance identifies full queues and Collector crashes as loss cases without persistent storage. | A bounded in-process queue, bounded retry age, Redis `noeviction`, and non-throwing logger-provider behavior can be owned and tested in Story 27.2. | File I/O, disk pressure, rotation races, and read-only-root failures would need a custom non-blocking provider and storage monitor. |
| Observability | Collector and Loki metrics exist, but the exact accepted-to-purged lifecycle and `NoData` contract are not repository-owned. | The application and lifecycle verifier can expose the required bounded signal set and Redis/PVC health without high-cardinality labels. | Custom queue, disk, rotation, per-replica, and purge instrumentation is required. |
| Privacy and tenant boundary | Collector processors could sanitize fields, but sending raw values before processing expands the privacy boundary. | Sanitization happens before enqueue; separate writer, lifecycle, and inspector ACLs keep the Server write-only. | Plain files increase inspection and accidental disclosure surface; per-node access controls and deletion evidence are unresolved. |
| Capacity and operating cost | Adds Collector, Loki, and object storage; capacity and cost are not presently sized or approved. | Adds two Redis data Pods with PVCs and three small Sentinel Pods. The 7-day default is sized explicitly in the ratified contract. | Adds durable shared or per-replica volumes plus rotation/purge agents; cost is lower only if the unresolved correctness work is ignored. |
| Rollback | Removing a collector route can preserve console output, but queued/backend data ownership remains unspecified. | Disable the provider and roll back the Server while Redis continues expiring retained keys; never delete the store automatically. | Rolling back writers/rotators can strand files and stop purge unless another owner remains deployed. |
| Hard-gate result | Rejected: named products, but no repository-owned deployable retention contract. | Selected: every hard gate has a concrete Story 27.2/27.3 target. | Rejected: fails the current two-replica, read-only-root, rescheduling, rotation, and executable-purge gates. |

## Selected Design and Rejected Alternatives

The selected design is a dedicated Redis 7.4 access-telemetry workload, separate
from the domain Redis and from Hexalith.EventStore. Story 27.2 will deploy one
primary data Pod, one replica data Pod, and a three-Pod Sentinel quorum. Each
data Pod owns a persistent volume; the Server receives only a dedicated writer
credential. The implementation uses typed `AccessTelemetryEvent` state and one
unique Redis string key per sanitized record.

The selected image is the already pinned
`redis/redis-stack-server:7.4.0-v8@sha256:798ab84d9f266936b034ab11c4d04a2b8e4b441884c5aa7d17ac951eefdf742a`.
The workload does not use Search, Vector, Dapr state, pub/sub, or domain data;
the existing image is selected to avoid introducing an unreviewed image supply
chain in Story 27.2. A later image simplification requires its own validation.

The deployment cost accepted by this decision is two Redis data Pods, two
production PVCs, and three small Sentinel Pods. That cost buys independent
credentials, persistence, failover, atomic per-record TTL, and executable
lifecycle evidence. It must not be reduced by sharing the domain Redis.

The Collector-plus-Loki option is rejected because the repository owns neither
deployment nor backend retention, and adding an endpoint would repeat the
current routing-without-lifecycle gap. File/volume storage is rejected because
the committed Server filesystem cannot retain it and no concurrency-safe,
rescheduling-safe rotation and purge implementation is available at the
decision gate.

## Ownership and Topology

Kustomize remains the Production authority. Story 27.2 will add a dedicated
`access-telemetry-redis` workload containing two Redis data Pods (one primary,
one replica), three Sentinel Pods with quorum two, a primary-discovery service,
and one persistent volume claim per data Pod. Required Pod anti-affinity keeps
the two data Pods on different nodes. The persistent-volume reclaim policy is
`Retain`; pod replacement or rescheduling reattaches the Pod's volume instead
of falling back to `emptyDir`.

This workload is not a Dapr state store, pub/sub broker, vector index, domain
read model, or Hexalith.EventStore resource. It has an independent connection
string, Redis ACL file, Kubernetes Secrets, PVC capacity, alerts, and lifecycle
owner. The domain Redis credential must not authenticate to it.

Authorities are deliberately separate:

| Authority | Owner | Allowed scope |
| :-------- | :---- | :------------ |
| `access-telemetry-writer` | Memories Server | Connect, health ping, invoke the preloaded atomic write script for `access:v1:*` and the exact `access:expiry:v1` index key, and use `WAITAOF`; it cannot `GET`, `SCAN`, inspect, change configuration, or delete records. |
| `access-telemetry-lifecycle` | Hexalith Platform Operations lifecycle controller | Read the expiry index and execute the preloaded purge/reconciliation script only for due `access:v1:*` records; it cannot change arbitrary Redis configuration or access domain Redis. |
| `access-telemetry-inspector` | Authorized Hexalith Platform Operations responders | Read sanitized records and lifecycle metadata under `access:v1:*`; it cannot write, extend TTL, delete, or change configuration. |
| Redis administration | Hexalith Platform Operations | Manage Sentinel, ACLs, persistence, capacity, backup-free recovery, and emergency isolation; this credential is never mounted into the Server. |

Administrator ratifies the added operating footprint: two 16-GiB data Pods,
two 32-GiB PVCs for the default window, and three 128-MiB Sentinel Pods. Larger
retention settings must satisfy the capacity admission rule below before
deployment.

## Multi-Replica Write and Durability Boundary

Both Server replicas discover the same Redis primary through Sentinel. Each
typed event receives a monotonic ULID record identifier in the provider. Its key
is `access:v1:<recordId>`; tenant, user, case, query, and source values never
appear in the key. Concurrent Servers therefore write disjoint keys without a
shared-file or rotation race.

The preloaded write script validates the emission timestamp against Redis
`TIME`, then atomically writes the sanitized payload with absolute expiry and
adds the key to `access:expiry:v1`. Retrying the same event writes the same bytes
and the same expiry to the same key, so an ambiguous acknowledgement cannot
extend retention or create a duplicate.

Both data Pods enable AOF with `appendfsync everysec`; RDB snapshots are enabled
for restart speed but are not the acknowledgement boundary. The primary uses
`min-replicas-to-write 1` and `min-replicas-max-lag 2`. After each write batch,
the provider issues `WAITAOF 1 1 1500` on the same connection and marks those
records `persisted` only when both the primary and its one replica report the
batch fsynced to AOF. A timeout is an unconfirmed attempt and remains eligible
for bounded retry.

The durability boundary is the two retained PVCs. Within that boundary, the
acknowledged loss window is **0 seconds for any single Server Pod, Redis data
Pod, node, or one-PVC failure**, because acknowledgement follows fsync on both
data members. Redis and Sentinel are not a consensus store: simultaneous loss
or corruption of both data PVCs, storage-control-plane loss, or a disaster that
destroys the Kubernetes storage domain may lose all retained records and is
explicitly outside this boundary. The system makes no zero-loss claim for such
failures. Sentinel promotes the only eligible replica after a primary failure;
the returning member is rebuilt from the surviving primary.

## Retention, Expiry, Purge, and Clock

| Policy | Ratified value |
| :----- | :-------------- |
| Production default | 24 hours |
| Allowed minimum | 1 hour |
| Allowed maximum | 7 days |
| Configuration owner | Kustomize through `AccessTelemetryLifecycle__Retention` |
| Authoritative clock | Redis primary `TIME`, interpreted as UTC |
| Logical expiry | Absolute `EXAT` timestamp established atomically with the record write |
| Lifecycle sweep | Every 5 minutes |
| Physical-purge grace | No later than 15 minutes after logical expiry while the lifecycle health gate is healthy |

Production must set the retention value explicitly. Missing, blank, malformed,
below-minimum, above-maximum, zero, negative, or infinite values fail Production
startup before serving requests. No code path substitutes an unbounded TTL.
Development and tests may use the 24-hour default; tests may inject an internal
short duration only through test composition, never Production configuration.

Age begins at `AccessTelemetryEvent.Timestamp`, the event-emission instant.
Redis `TIME` is authoritative for acceptance and expiry enforcement. Emission
timestamps more than 2 minutes in the future are rejected as
`future_clock_skew`. Records whose emission time plus retention is already due
are rejected as `stale_before_acceptance`. A late record within the window gets
only its remaining lifetime; acceptance and retry never reset its age. An
emission timestamp up to 2 minutes ahead is accepted with its declared expiry,
making 2 minutes the maximum clock-skew extension.

At `EXAT`, Redis treats the record as logically absent. Redis active expiry and
the five-minute lifecycle sweep perform physical reclamation. The expiry index
score is the absolute expiry timestamp. The purge script obtains Redis `TIME`
itself, unlinks only records with a score at or before that time, and removes
only those due entries from the index. It never selects or rewrites a newer
record. The lifecycle controller records an expiry when a due key becomes
logically absent and a purge when both the key and its index member are gone.
An oldest due entry older than 15 minutes makes lifecycle health unhealthy.

The capacity baseline is the existing high-end estimate of 5 GB/day raw across
two Server replicas. A conservative 2.0 factor covers Redis keys, sanitized
payload variance, AOF, and fragmentation: 10 GB/day per data member. The
24-hour default therefore needs about 10 GB live data; one rewrite-sized copy
plus 30% headroom yields 26 GB, rounded to a 32-GiB PVC and 16-GiB memory limit
per member. The seven-day maximum requires at least a 256-GiB PVC and a
separately approved memory/cost plan per member. Deployment validation rejects
a retention/capacity pair whose projected steady state exceeds 50% of PVC or
70% of configured Redis memory. Capacity warns at 70%, is critical at 80%, and
sets lifecycle health unhealthy at 90%; `maxmemory-policy noeviction` forbids
silent deletion of newer records.

## Failure, Backpressure, Recovery, and Capacity

The provider uses a non-blocking bounded queue with both an 8,192-record limit
and a 64-MiB serialized-byte limit. `ILogger.Log` performs validation,
sanitization, and `TryWrite`; it never waits for Redis. A full queue drops the
new record with reason `queue_full` while existing JSON-console and optional
OTLP emission continue.

The worker batches at most 256 records or 1 MiB. Retry uses exponential backoff
with full jitter from 100 milliseconds to 5 seconds, capped by 5 minutes from
event emission and by the record's absolute expiry. Shutdown receives 5 seconds
to flush. Anything remaining is dropped as `shutdown_timeout`; no local disk
spill is allowed. Recovery reconnects through Sentinel and drains retained
queue entries without changing their identifiers, emission times, or expiry.

Connection failures, primary failover, `WAITAOF` timeout, AOF errors,
`OOM command not allowed`, PVC exhaustion, script-version mismatch, and
sanitization failures have bounded reason codes. Attempts are retried only while
eligible; terminal items are counted as dropped. Redis `noeviction` prevents
capacity pressure from converting into silent loss. A transient or permanent
lifecycle failure may degrade or drop access records, but no provider exception
may escape `ILogger.Log`, block indefinitely, alter an HTTP response, or fail a
business operation.

Production configuration validity is different from sink availability.
Invalid configuration, missing credentials, an unavailable write script, or a
Redis version below 7.2 fails startup. A correctly configured sink that becomes
unavailable after startup degrades through the bounded queue/retry policy and
raises unhealthy lifecycle signals without taking the business service down.

## Observability

Hexalith Platform Operations owns these signals. The counter
`memories.access.telemetry.lifecycle.records` uses the bounded `state` values
**accepted**, **rejected**, **enqueued**, **persisted**, **retried**, **failed**,
**dropped**, **expired**, and **purged**. Optional `reason` values come from a
code-owned finite enum. Accepted means a typed event passed schema, timestamp,
and privacy validation; persisted means the `WAITAOF` boundary passed; failed
is an individual sink attempt; dropped is terminal; expired is logical absence;
purged is physical key and expiry-index reclamation.

Additional gauges are queue records, queue bytes, oldest queued age, Redis
connectivity, Sentinel primary availability, AOF health, replica fsync lag,
memory and PVC utilization, expiry-index depth, and oldest due age. Health has
`Healthy`, `Degraded`, `Unhealthy`, and `NoData`. `NoData` means no accepted or
rejected access event was observed in the last 15 minutes while the provider is
enabled; it is not evidence of a healthy write path and alerts during an
expected-traffic window.

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
| `emittedAtUtc`, `acceptedAtUtc`, `expiresAtUtc` | UTC lifecycle timestamps; acceptance comes from Redis time. |
| `operationType`, `outcome`, `errorCode` | Values validated against bounded catalogs. |
| `tenantMarker` | Full HMAC-SHA-256 of canonical tenant ID using a dedicated lifecycle marker secret; `__rejected__` remains a bounded synthetic marker. |
| `userMarker`, `caseMarker` | Optional full HMAC-SHA-256 markers using the same dedicated secret and type-specific domain separators. |
| `queryParams` | Allowlisted booleans, bounded enums, counts, sizes, and weights only. Raw `query` becomes `queryLengthBucket`; raw `subject` becomes `subjectPresent`; raw `sourceUri` becomes the bounded `sourceKind` enum. |
| `resultCount`, `durationMs` | Non-negative bounded numeric values. |
| `traceId`, `spanId` | Validated W3C identifiers for authorized operational correlation. |

The marker secret is separate from Redis credentials and domain secrets. Raw
tenant, user, case, query, subject, source URI, payload, token, authorization
header, credential, exception, and unbounded metadata values are prohibited.
Schema rejection is fail closed for persistence and does not alter the request.

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

An old Server image cannot meet the ratified lifecycle target. Rollback is
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

Story 27.2 owns this exact implementation map:

1. Add options and Production startup validation for enablement, explicit
   1-hour-to-7-day retention, queue/retry/flush limits, capacity inputs,
   Sentinel endpoint, credentials, Redis 7.2 minimum, and script fingerprint.
2. Add a typed `AccessTelemetryEvent` logger provider, sanitizer, bounded queue,
   worker, stable ULID record identity, Redis-time validation, atomic
   write-plus-expiry/index script, `WAITAOF` acknowledgement, bounded retry, and
   non-throwing failure isolation. Do not parse stdout.
3. Add the allowlisted persisted schema and HMAC markers. Prove raw `query`,
   `subject`, and `sourceUri` never enter queue or Redis payloads.
4. Add the complete accepted/rejected/enqueued/persisted/retried/failed/dropped/
   expired/purged counter, bounded reason catalog, gauges, health states, and
   `NoData` behavior without high-cardinality labels.
5. Add the independent Kustomize Redis primary/replica/Sentinel resources,
   anti-affinity, retained PVCs, AOF, `noeviction`, ACL roles, Secrets, capacity
   validation, probes, and lifecycle controller. Do not reuse domain Redis,
   Dapr state, pub/sub, or Hexalith.EventStore.
6. Add unit and integration tests for two concurrent writers, idempotent retry,
   Redis `TIME`, skew/late arrival, exact expiry, purge/newer preservation,
   queue overflow, retry exhaustion, shutdown, AOF/replica acknowledgement,
   failover, capacity, invalid configuration, and provider exception isolation.
7. Attach focused cross-tenant denial and privacy-negative results using the
   named Story 20.2 and Story 24.3 guards from the privacy section. No happy-path
   or broad-suite result substitutes for that negative evidence.

## Story 27.3 Verification and Operations Handoff

Story 27.3 must supply Production-shaped evidence and the operations contract:

1. Start two Server writers against the dedicated workload and prove unique,
   sanitized records persist through the `WAITAOF` boundary without duplicate
   or cross-tenant selection.
2. Restart each Server, the Redis primary, the replica, and the lifecycle
   controller; reschedule each Redis data Pod and prove its PVC reattaches.
   Exercise Sentinel promotion and recovery while JSON-console and optional
   OTLP emission continue.
3. Use deterministic emitted timestamps and Redis time to prove the minimum,
   default, maximum, late-arrival, future-skew, and already-expired bounds.
   Prove logical expiry, purge within 15 minutes, expiry-index cleanup, and
   preservation of records whose expiry is later.
4. Exercise Redis outage, Sentinel unavailability, queue/byte exhaustion,
   `WAITAOF` timeout, AOF failure, `noeviction`/memory pressure, PVC pressure,
   malformed configuration, bad credentials, script mismatch, retry-age
   exhaustion, and shutdown timeout. Verify business requests never receive a
   provider exception.
5. Prove the full lifecycle signal set, capacity thresholds, bounded labels,
   health transitions, alerts, and `NoData` semantics from accepted through
   physical purge.
6. Prove inspection least privilege, write-only Server credentials, sanitized
   tenant correlation, raw query/subject/source absence, denial before
   lifecycle dependencies, and no tenant-facing read route.
7. Publish the operations runbook for rollout, capacity calculation, alerts,
   failover, recovery, inspection, credential rotation, rollback, degraded old
   image behavior, and verified decommission after the maximum window.
8. Only after all evidence passes, coordinate the A41 deferred entry and action
   close-out. Story 27.1 scheduling or ADR acceptance alone is never closure.
