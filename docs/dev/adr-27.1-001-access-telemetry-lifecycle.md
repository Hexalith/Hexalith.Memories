# ADR 27.1-001: Access Telemetry Lifecycle

## Status and Decision Metadata

| Field | Accepted value |
| :---- | :------------- |
| Status | Accepted |
| Decision date | 2026-07-17 |
| Approver | Administrator |
| Architecture owner | Hexalith.Memories maintainers |
| Operational lifecycle owner | Hexalith Platform Operations |
| Affected deployment | Any container service on which the required Dapr APIs and an eligible Dapr component profile are available |
| Selected family | Repository-owned dedicated write-only telemetry service |
| Selected technology | Dapr service invocation, state management, actors, reminders, configuration, and secrets; no application dependency on a specific state-store product or container orchestrator |
| Implementation gate | Story 27.2 implements the portable contract; Story 27.3 qualifies the exact Production adapter; Story 27.4 performs deployment-shaped verification and A41 close-out. Neither implementation start, profile selection, nor ADR acceptance closes A41. |

This accepted decision defines a portable lifecycle target but does not implement
it. Current JSON-console emission and optional OTLP export remain the only
shipped paths. Story 27.2 implements the portable target; Story 27.3 qualifies
the selected Production adapter and immutable deployment profile; Story 27.4
consumes only that approved profile and owns deployment-shaped verification,
operations documentation, and the evidence-backed A41 close-out gate.

## Verified Current State

- `AccessTelemetryLog` emits nine success families (7501-7509) and nine error
  families (7511-7519) through `AccessTelemetryCategory` using typed
  `AccessTelemetryEvent` logger state.
- ServiceDefaults always registers OpenTelemetry logging and UTC JSON console.
  It registers the OTLP exporter only when `OTEL_EXPORTER_OTLP_ENDPOINT` is
  non-empty. An endpoint routes telemetry; it does not define retention.
- The currently committed Production Server has two replicas, a read-only root
  filesystem, no OTLP endpoint or access-telemetry backend, and only ephemeral
  temporary storage. Those are current facts, not requirements on future
  container services.
- No committed component owns access-record TTL, purge, persistent buffering,
  lifecycle health, or storage capacity.
- JSON console preserves an outer JSON envelope but formats `@AuditEvent` via
  record `ToString()`, so `QueryParams` values cannot be recovered. The future
  writer must consume typed logger state, not reparse stdout.
- Search and source-URI producers can currently place raw `query`, `subject`,
  and `sourceUri` values in `QueryParams`. Durable storage of those values is
  prohibited; Story 27.2 must sanitize them before enqueue.

## Options Evaluated

| Lifecycle field | Deployment-owned OpenTelemetry backend | Dapr-backed dedicated lifecycle service | File or volume storage |
| :-------------- | :------------------------------------- | :-------------------------------------- | :--------------------- |
| Ownership and topology | Requires a named collector, backend, credentials, retention owner, and on-call contract; none is presently repository-owned. | The repository owns two Dapr-addressed services and one fixed-ID actor; Platform Operations supplies an eligible Dapr component profile without exposing its backend to Memories. | Requires a new per-instance or concurrency-safe shared writer, durable storage, rotation, and lifecycle controller. |
| Multi-writer behavior | A collector can accept concurrent writers, but durable acknowledgement and deduplication remain backend-specific and unspecified. | Every service instance invokes one fixed actor, which serializes idempotent transactional writes and lifecycle mutations. | Shared files require locking and rotation coordination; per-instance files complicate complete rescheduling and purge evidence. |
| Durability and recovery | An endpoint alone is insufficient; queues, WAL, backend recovery, and acknowledgement semantics need explicit ownership. | Dapr acknowledges only after the selected state component completes the accepted transaction; actor state and reminders reconstruct control after process or container replacement. | Local temporary storage is lost on replacement; durable volumes still require an exact recovery and rescheduling contract. |
| Retention, expiry, purge, and clock | Product retention can work only after a named version, clock, deletion bound, and evidence owner are committed. | Attested UTC fixes absolute logical expiry; Dapr TTL is defense in depth; actor reminders and expiry buckets drive bounded delete; the adapter proves backend physical reclamation. | Rotation by size or time is not record TTL and does not prove preservation of newer records. |
| Failure and backpressure | Requires owned queue size, retry age, overflow, and backend outage behavior. | A bounded non-blocking provider, bounded Dapr invocation retry, fail-closed lifecycle health, and business-path isolation are repository-owned. | File I/O, disk pressure, read-only roots, and rotation races require a custom non-blocking implementation. |
| Observability | Backend signals alone do not prove accepted-to-expired lifecycle or `NoData`. | The service and actor emit the complete bounded lifecycle signal set; the adapter publishes capability, capacity, durability, and reclamation evidence. | Queue, disk, rotation, rescheduling, and purge instrumentation would all be custom. |
| Privacy and tenant boundary | Sending raw values to an external route expands the privacy boundary unless sanitization precedes export. | Sanitization happens before enqueue; service invocation, state, lifecycle, and inspection authorities are separate; no tenant-facing read API exists. | Plain files broaden accidental inspection and disclosure risk. |
| Capacity and operating cost | Cannot be ratified without a named backend and quote. | The record envelope and event-rate ceiling are fixed; every chosen component must measure physical amplification and pass the portable capacity formula before rollout. | Cost is unknowable until storage, replicas, rotation workspace, and purge ownership are defined. |
| Rollback | Route removal can preserve console output, but queued and retained backend data still need an owner. | Disable the provider while the Dapr lifecycle service and actor continue expiring retained records; storage is never deleted automatically. | Rolling back writers or rotators can strand files and stop purge. |
| Hard-gate result | Rejected: routing is not a repository-owned lifecycle. | Selected: the application contract is Dapr-only and the deployment-specific facts are enforced by a capability/evidence gate. | Rejected: it fails the current multi-replica, read-only-root, rescheduling, rotation, and executable-purge gates. |

## Selected Design and Rejected Alternatives

The selected design is a dedicated Dapr-addressed access-telemetry lifecycle
service. Memories code calls Dapr APIs only. It does not link a backend SDK,
open a backend connection, name a backend product, or require Kubernetes or any
other orchestrator. The deployment can run on any container service that can
host the required Dapr runtime and pass this ADR's component profile.

Dapr does not provide a trusted-time building block. The design therefore owns
a small clock-attestation application reached through
[Dapr service invocation](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/).
Persistence uses [Dapr state management](https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/),
and lifecycle serialization and recovery use a fixed-ID
[Dapr actor](https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/)
with durable reminders. Dapr is the sole application integration surface for
the clock, state, actor control, configuration, and secrets.

The selected Dapr component may be stable, preview, or alpha. Alpha status is
not a waiver: Production must explicitly set `allowAlphaComponent: true`, pin
the component/runtime version, pass the same behavioral gates as any stable
component, and record an upgrade/rollback owner. Dapr's published support level
does not replace local evidence.

An external OTLP backend is rejected because the repository owns neither its
retention nor its durability. File/volume storage is rejected because the
current Server filesystem cannot retain it and no portable concurrency-safe,
rescheduling-safe rotation and purge implementation exists at this decision
gate.

## Ownership and Topology

The portable topology has these stable logical identities:

### Logical Identities

| Identity | Owner | Contract |
| :------- | :---- | :------- |
| Dapr app ID `memories-access-telemetry` | Hexalith.Memories maintainers | Accepts sanitized write batches, hosts `AccessTelemetryLifecycleActor`, exposes operations-only inspection, and reaches state only through Dapr. |
| Actor type/ID `AccessTelemetryLifecycleActor/global` | Hexalith Platform Operations lifecycle owner | Serializes writes, expiry-bucket mutation, purge, health checkpoints, capacity checkpoints, and marker rotation; actor state and reminders are durable. |
| Dapr app ID `memories-access-telemetry-clock` | Hexalith Platform Operations time owner | Returns short-lived, signed independent-UTC attestations through Dapr service invocation. |
| Dapr component `access-telemetry-store` | Deployment adapter owner | Supplies the eligible state and actor-state behavior; the backend implementation is opaque to Memories. |
| Dapr component `access-telemetry-secrets` | Deployment adapter owner | Supplies marker and attestation keys through the Dapr secrets API. |
| Dapr component `access-telemetry-config` | Deployment adapter owner | Supplies versioned lifecycle configuration and component-profile identity through the Dapr configuration API. |

The current two Server replicas, and any later replica count, invoke the same
`memories-access-telemetry` app ID. A deployment, service instance, and process
are identified without orchestrator-specific names: `deploymentId` is a stable
operator value; `serviceInstanceId` is unique for the running container;
`processEpoch` is a new ULID for every process start; and `componentProfileHash`
is SHA-256 over the non-secret, canonical capability profile. No Pod UID,
StatefulSet ordinal, Lease, PVC, or orchestrator API is part of the application
protocol.

The fixed actor ID provides one logical mutating owner. Dapr placement and
turn-based concurrency ensure only one activation handles a turn; durable actor
state and reminders allow a replacement activation to reconstruct unfinished
work. The actor never relies on in-memory leadership. Its state contains the
configuration epoch, writer registry, expiry-bucket cursor, purge checkpoints,
capacity evidence ID, physical-reclamation evidence ID, and marker-rotation
phase. Reminder callbacks are idempotent.

### Component Capability Gates

The deployment profile is eligible only after a Production-shaped probe proves
all of the following against the exact configured component:

| Capability gate | Required evidence |
| :-------------- | :---------------- |
| Dapr-only boundary | Network and dependency inspection proves Memories and the lifecycle service use Dapr APIs and no backend SDK, endpoint, credential, or orchestrator API. |
| State semantics | CRUD, strong reads, ETags, multi-key transactions, and deterministic conflict behavior pass on the exact component version. |
| Actor semantics | The component is configured as the one actor state store and passes actor state, reactivation, placement failover, and reminder recovery. Dapr documents that actor stores require transactions and ETags in its [supported-store matrix](https://docs.dapr.io/reference/components-reference/supported-state-stores). |
| TTL semantics | Per-record TTL is accepted, does not extend on retry, and makes data unavailable no later than its declared defense-in-depth deadline. Components that ignore TTL fail; Dapr documents that TTL support varies by component in its [state TTL guidance](https://docs.dapr.io/developing-applications/building-blocks/state-management/state-store-ttl/). |
| Request bounds | A 1,024-byte record and a complete 1-MiB/256-record transaction are supported without truncation. |
| Throughput | The fixed actor and component sustain twice the 250 events/s cluster ceiling while purge runs, with the queue-drain and latency bounds below. |
| Durability | Acknowledged transactions survive any one lifecycle-service process/container/host loss and the adapter's declared single-component failure scenario with a measured zero-record loss window. |
| Isolation and encryption | Dapr app/API scoping, service identity, transport protection, secret isolation, state encryption ownership, and operations-only inspection pass. |
| Capacity and reclamation | The adapter supplies the physical-amplification measurement, reserved capacity, logical-delete proof, backend physical-reclamation bound, and evidence collector described below. |

Failure of a gate blocks lifecycle writes and makes lifecycle health
`Unhealthy`; it does not make business readiness unavailable. There is no
fallback to local files, an implicit state component, or an unverified backend.

### Authorities

Authorities remain separate:

| Authority | Owner | Allowed scope |
| :-------- | :---- | :------------ |
| `access-telemetry-writer` | Memories Server | Invoke the lifecycle write and writer-heartbeat methods through Dapr; it cannot call state, inspect records, extend retention, or delete. |
| `access-telemetry-service` | Lifecycle service | Invoke the fixed actor and named Dapr state/configuration/secret components within the accepted key prefix; it has no domain-store authority. |
| `access-telemetry-clock` | Clock service | Read its signing material through Dapr secrets and return signed attestations; it cannot read or mutate telemetry state. |
| `access-telemetry-inspector` | Authorized Platform Operations responders | Invoke sanitized, operations-only inspection methods; it cannot write, extend TTL, rotate keys, or delete. |
| `access-telemetry-adapter` | Deployment adapter owner | Configure and operate the Dapr components and collect backend physical evidence outside the application API; its backend authority is never mounted into Memories or the lifecycle service. |

## Multi-Replica Write and Durability Boundary

Each Server instance creates a monotonic ULID `recordId` and submits a canonical
immutable envelope to `memories-access-telemetry` through Dapr service
invocation. The service forwards batches of at most 256 records and 1 MiB to
`AccessTelemetryLifecycleActor/global`. Actor turn serialization makes all
record creation, retry comparison, expiry-index mutation, and lifecycle control
single-writer operations even when the Server and lifecycle services scale.

For each first write, the actor obtains a fresh clock attestation, validates the
event time, assigns `acceptedAtUtc`, calculates `expiresAtUtc`, and commits the
record plus its minute/shard expiry-bucket entry in one Dapr state transaction.
Expiry buckets have 64 deterministic shards per UTC minute; at the 250 events/s
ceiling this averages fewer than 235 IDs per shard. Record keys and index keys
use a constant state partition value so the selected component's proven
multi-key atomicity applies.

The immutable envelope excludes `acceptedAtUtc` and `envelopeHash`. The actor
persists the completed record and the SHA-256 envelope digest. An existing
`recordId` with the same digest and absolute expiry is an idempotent retry and
returns the original acceptance time. Any byte or expiry difference is
`record_id_conflict`; it never overwrites or extends the record and makes
lifecycle health unhealthy.

`persisted` means Dapr returned success for the exact state transaction. The
application-level acknowledged loss window is **0 seconds for any single
Server or lifecycle-service process, container, or host loss**. Production also
requires the selected adapter to prove zero acknowledged-record loss for its
declared single-component failure scenario. Simultaneous failures outside that
profile use the adapter's published recovery-point objective; this ADR makes no
stronger claim. A component that cannot meet the Production durability gate is
ineligible, regardless of its Dapr support level.

### Continuous Independent UTC Attestation

Every Server instance and every lifecycle-service process refreshes an
attestation by invoking `memories-access-telemetry-clock` through Dapr every 10
seconds. The clock service samples at least three authenticated, independently
operated UTC sources, accepts a majority only when their interval intersection
has at most 250 milliseconds uncertainty, and signs the response with an
application-level key obtained through Dapr secrets. Dapr service identity and
mTLS protect invocation; the signature makes stored evidence independently
verifiable. Dapr's cryptography API is not treated as a signing API.

The signed payload contains `deploymentId`, `serviceAppId`,
`serviceInstanceId`, `processEpoch`, `componentName`, `componentProfileHash`, a
request nonce, reference UTC, measured local UTC, uncertainty, issued time, and
expiry. An attestation expires after 30 seconds, is single-context, and cannot
be replayed for another process or component profile. The verifier rejects an
invalid signature, nonce, identity, profile, source quorum, uncertainty, or
absolute local/reference delta greater than 1 second.

Event-time validation and process-clock validation use the same one-second
absolute bound and the same attested interval; no two-minute or additional
future-skew allowance exists. Transport uncertainty must fit inside the
one-second bound, not extend it. A fresh attestation is required before a write
or lifecycle mutation and immediately after service reactivation, component
profile change, clock-source change, or actor failover.

Missing or stale attestation stops persistence and lifecycle mutation,
invalidates purge evidence, and makes lifecycle health `Unhealthy`. It does
**not** gate business readiness or alter a business response. Already
acknowledged state remains within the durability boundary; unacknowledged
records remain subject to the bounded queue, retry age, and drop accounting.

## Retention, Expiry, Purge, and Clock

| Retention field | Accepted value |
| :-------------- | :------------- |
| Production default | 24 hours |
| Allowed minimum | 1 hour |
| Allowed maximum | 7 days |
| Configuration owner | Versioned Dapr configuration entry `access-telemetry-lifecycle` |
| Authoritative clock | Signed independent UTC from Dapr app ID `memories-access-telemetry-clock` |
| Logical expiry | Absolute millisecond `expiresAtUtc` committed atomically with the first record write |
| Defense-in-depth TTL | Dapr state TTL in whole seconds, set once to the ceiling of the remaining lifetime |
| Lifecycle sweep | Durable actor reminder every 5 minutes |
| Active-purge grace | No later than 15 minutes after logical expiry while lifecycle health is healthy |
| Physical-reclamation bound | Adapter-declared and verified per component, never greater than 24 hours after active purge |

Production must supply the retention value explicitly through Dapr
configuration. Missing, blank, malformed, below-minimum, above-maximum, zero,
negative, or infinite values fail lifecycle-service validation before writes.
No code path substitutes an unbounded TTL. Business readiness remains
available with lifecycle health fail-closed. Development and tests may use the
24-hour default; tests may inject a shorter duration only through test
composition.

Age begins at `AccessTelemetryEvent.Timestamp`, preserved to UTC
Unix-millisecond precision. Emission timestamps more than 1 second ahead of the
attested interval are rejected as `future_clock_skew`. Records whose emission
time plus retention is already due are rejected as `stale_before_acceptance`.
A late record within the window receives only its remaining lifetime;
acceptance and retry never reset age or extend expiry. Lowering retention
applies to new records only. Accelerated retroactive purge is outside this
contract and requires a separately accepted operation.

The actor writes `ttlInSeconds = ceil(expiresAtUtc - attestedNow)` exactly once.
Dapr TTL is defense in depth because components differ in support and use
whole-second metadata. The absolute `expiresAtUtc` remains normative: every
inspection or reconciliation method treats a record as absent at that exact
millisecond even if the component still holds bytes.

The actor reminder walks only expiry buckets whose minute is due. It processes
at most 512 records per actor turn, verifies each absolute expiry against a
fresh attestation, calls Dapr Delete only for due records, removes only their
index entries, and preserves later records. Each turn has a 100-millisecond
observed execution budget and resumes with a bounded 25-to-100-millisecond
backoff until oldest-due age is within 15 minutes.

A purge cohort checkpoint records cohort ID, actor/configuration epoch,
candidate count, deleted count, already-absent count, index-removal count,
attestation ID, start/end time, and terminal result. `expired` means logical
absence at `expiresAtUtc`. `purged` means Dapr Delete succeeded, a strong Dapr
Get reports absent, and the corresponding index member is removed for every
candidate. This proof is portable and deliberately does **not** claim that the
backend returned physical bytes to an allocator, file, volume, database, or
operating system.

The deployment adapter owns physical reclamation evidence outside the
application API. Its accepted component profile names the observable backend
artifact, evidence collector, maximum bound (at most 24 hours after active
purge), capacity effect, retry/recovery procedure, and operator. A successful
backend compaction, vacuum, tombstone collection, object deletion, or equivalent
can satisfy the gate only when it is attributable to the purged cohort. A
missed bound makes lifecycle health unhealthy and prevents a physical
reclamation claim; it never causes Memories to call a backend-specific API.

## Capacity Evidence and Admission Envelope

Administrator ratifies this owner-approved admission envelope for the current
two-Server Production traffic model. It is a logical workload contract, not a
backend reservation. Any replica-count change must preserve the same
cluster-wide ceiling or amend this ADR with a new capacity calculation. The
selected component adapter must translate the envelope to physical resources
and cost before rollout.

### Operation Envelope

| Operation | Per-replica events/s | Cluster events/s | Representative average sanitized bytes | Representative P95 sanitized bytes |
| :-------- | -------------------: | ---------------: | -------------------------------------: | ---------------------------------: |
| search | 100 | 200 | 874.8 | 900 |
| ingest | 3 | 6 | 856.8 | 882 |
| traverse | 5 | 10 | 831.8 | 857 |
| case-access | 8 | 16 | 806.8 | 832 |
| delete | 1 | 2 | 770.8 | 796 |
| tenant-lifecycle | 0.1 | 0.2 | 738.8 | 764 |
| tenant-config | 0.4 | 0.8 | 775.8 | 801 |
| case-member | 2 | 4 | 772.8 | 798 |
| annotation | 5.5 | 11 | 784.8 | 810 |

The deterministic fixture serializes 100 compact UTF-8 records per family: 90
success and 10 bounded `internal_dependency_failure` records, fixed-length
identifiers and hashes, and a representative high-cardinality combination of
allowed parameters. These averages and P95 values describe that contract
fixture; they are not Production percentiles and are not the admission ceiling.
The authoritative capacity ceiling is the 1,024-byte serialized-record limit.

For configured retention `H` from 1 through 168 hours:

- retained records = `250 × 3,600 × H`;
- maximum canonical payload = `records × 1,024` bytes;
- indexed-record count is the same as retained records, plus bounded actor,
  expiry-bucket, idempotency, and checkpoint state;
- the adapter measures physical bytes per record and index entry, replication
  or durability multiplier, transaction-log/snapshot/tombstone overhead,
  reclamation workspace, and actor/control overhead using the exact component.

### Retention Sizing

| Retention | Retained records | Maximum canonical payload |
| :-------- | ---------------: | ------------------------: |
| 1 hour | 900,000 | 0.86 GiB |
| 24 hours | 21,600,000 | 20.60 GiB |
| 7 days | 151,200,000 | 144.20 GiB |

For the chosen component, the adapter calculates:

`requiredPeak = records × (measuredRecordBytes + measuredIndexBytes) × durabilityMultiplier + controlBytes + reclamationWorkspace`.

Admission requires the projected steady state to use at most 70% of the
component's usable capacity, the projected reclamation peak to use at most 80%,
and observed throughput to remain at least 500 events/s while the purge worker
runs. The profile warns at 70%, is critical at 80%, and makes lifecycle health
unhealthy at 90%. No component eviction/default-retention behavior may silently
delete a record early or retain it unboundedly. Platform Operations records the
resource reservation, service quota, price, evidence date, and owner; a quote
that cannot fund the profile requires lower retention or a revised decision.

Each Server queue has an 8,192-record and 64-MiB bound. At the current
125 events/s per replica, the record count covers 65.5 seconds, so the accepted
full-rate invocation outage is 60 seconds. On recovery, the component must
sustain at least twice the current arrival rate and drain every eligible queued
record within its five-minute retry age. A 10-minute actor/reminder outage can
create 150,000 due records; the 512-record/100-millisecond turn plus bounded
backoff must restore oldest-due age below 15 minutes in the exact deployed
component. Story 27.3 measures both scenarios rather than relying on a product
name or theoretical IOPS.

## Failure, Backpressure, Recovery, and Capacity

The provider uses a non-blocking bounded queue with both an 8,192-record limit
and a 64-MiB serialized-byte limit. `ILogger.Log` performs validation,
sanitization, and `TryWrite`; it never waits for Dapr. A full queue drops the
new record with reason `queue_full` while existing JSON-console and optional
OTLP emission continue.

Story 27.2 registers the lifecycle provider with a provider-specific
`Information` filter for `AccessTelemetryCategory`. Console and OTLP providers
may use their own filters, but a global category filter must not suppress
success events before the lifecycle provider sees them. Until Story 27.2 ships,
regulated deployments that rely on the current console/OTLP trail must retain
`Information` for that category or explicitly accept the loss of success events.

The worker batches at most 256 records or 1 MiB. The record bodies total at
most 256 KiB; the implementation also measures the complete encoded Dapr
request and rejects a batch before it exceeds 1 MiB. Retry uses exponential
backoff with full jitter from 100 milliseconds to 5 seconds, capped by 5
minutes from event emission and by absolute expiry. Shutdown receives 5
seconds to flush. Anything remaining is dropped as `shutdown_timeout`; no
local-disk spill is allowed.

Dapr invocation failure, stale clock attestation, actor unavailability,
component rejection, ETag/transaction failure, TTL rejection, capacity
pressure, schema rejection, and configuration mismatch use bounded reason
codes. A provider exception never escapes `ILogger.Log`, blocks indefinitely,
alters an HTTP response, or fails a business operation.

Static validation covers the retention, queue, request limits, Dapr app and
component names, configuration epoch, component-profile hash, alpha opt-in,
attestation verification key, marker-key references, and exact schema version.
A missing or malformed static value fails lifecycle writes before any request
is accepted by the lifecycle service.

Remote validation runs through Dapr and executes the behavioral component
probe. If Dapr or a remote app is unreachable at startup, business readiness
stays available, JSON console and optional OTLP continue, and lifecycle health
is `Unhealthy` with reason `remote_validation_pending`. The bounded queue and
retry-age limits account for loss. The first successful connection completes
validation before any state write. An identity, profile, capability, schema,
or key mismatch transitions lifecycle persistence to terminal
`configuration_invalid`; correction requires an explicit lifecycle-service
restart.

Every Dapr sidecar reconnection, actor reactivation, configuration-epoch
change, component-profile-hash change, credential generation change, or clock
key generation change repeats the relevant gate before mutation. Plain
unreachability continues bounded retry; a mismatch is fail-closed. Actor state
and reminders resume incomplete purge and marker rotation idempotently.

## Observability

Hexalith Platform Operations owns
`memories.access.telemetry.lifecycle.records`, whose bounded `state` values are
**accepted**, **rejected**, **enqueued**, **persisted**, **retried**,
**failed**, **dropped**, **expired**, and **purged**. Optional `reason` values
come from a code-owned finite enum. `persisted` is the successful Dapr state
transaction; `expired` is absolute logical absence; `purged` is the portable
Dapr delete/Get/index proof. No one of those states claims backend physical
byte reclamation.

Additional gauges cover queue records/bytes/oldest age, Dapr invocation and
actor availability, attestation age/delta/uncertainty, state-component profile,
transaction latency, capacity utilization, expiry-index depth, oldest due age,
purge-cohort age, reminder age, and age of the latest adapter physical-evidence
sample. The adapter exports a bounded `physical_reclamation` state and evidence
age without exposing backend credentials or payloads.

Health is `Healthy`, `Degraded`, `Unhealthy`, or `NoData`. Aggregation evaluates
configuration, clock, Dapr connectivity, actor/state capability, durability,
capacity, purge, and adapter reclamation before data presence. `Unhealthy`
takes precedence over `Degraded`. `NoData` is emitted only when the provider is
enabled, every lifecycle check is otherwise healthy, and no accepted or
rejected event was observed for 15 minutes. An unavailable or unvalidated
lifecycle path with no events is `Unhealthy`, never `NoData`.

Metric labels must never contain tenant, user, case, memory-unit, query,
subject, source, trace, span, record, service-instance, process, or component
backend identifiers. Health endpoints expose only bounded state, reason,
capacity percentages, and ages, not payloads, secrets, or component metadata.

## Privacy and Tenant Boundary

Sanitization occurs synchronously on typed logger state before enqueue, so raw
values never enter the lifecycle queue, Dapr request, actor state, retry state,
or metrics. The persisted JSON schema is allowlisted:

| Persisted field | Policy |
| :-------------- | :----- |
| `recordId`, `schemaVersion`, `eventId` | Generated/bounded identifiers and schema fields. |
| `envelopeHash` | Lowercase SHA-256 of the canonical immutable envelope excluding `acceptedAtUtc` and `envelopeHash`; used for exact retry comparison. |
| `emittedAtUtc`, `acceptedAtUtc`, `expiresAtUtc` | UTC lifecycle timestamps; acceptance comes from the actor's fresh clock attestation. |
| `operationType`, `outcome`, `errorCode` | Values validated against bounded catalogs. |
| `markerKeyId` | Bounded non-secret identifier for the lifecycle marker key used to derive this record's markers. |
| `tenantMarker` | Full HMAC-SHA-256 of canonical tenant ID using the versioned lifecycle marker key identified by `markerKeyId`; `__rejected__` remains a bounded synthetic marker. |
| `userMarker`, `caseMarker` | Optional full HMAC-SHA-256 markers using the same key and type-specific domain separators. |
| `queryParams` | Allowlisted booleans, bounded enums, counts, sizes, and weights only. Raw `query` becomes `queryLengthBucket`; raw `subject` becomes `subjectPresent`; raw `sourceUri` becomes `sourceKind`. |
| `resultCount`, `durationMs` | Non-negative bounded numeric values. |
| `traceId`, `spanId` | Validated W3C identifiers for authorized operational correlation. |

### Story 27.2 C1 Mapping Ratification

The current logger contract exposes more states than the original persisted V1
text could represent. The reconciliation below is the ratified complete mapping.
It replaces every conflicting outcome, nullable-field, and `queryParams` clause
under **Persisted Schema Bounds** and **Query Parameter Bounds**.

#### Ratification Decision

| Decision field | Recorded value |
| :------------- | :------------- |
| Mapping version | Story 27.2 C1 source-to-persisted V1 reconciliation, 2026-07-18 |
| Administrator decision | ratified 2026-07-18 by Administrator |
| Architecture owner decision | ratified 2026-07-18 by Administrator on behalf of Hexalith.Memories maintainers |
| Runtime persistence gate | open — both ratifications recorded and structure guards green |
| Scope | One persisted schema and one lifecycle family; no split or replan trigger found |

#### Source Event Mapping

The provider consumes the typed `AccessTelemetryEvent` value together with the
actual `EventId` and `LogLevel` delivered to `ILoggerProvider`. It accepts only
these exact combinations. An event-ID, severity, operation, or outcome mismatch
rejects the lifecycle copy as `schema_mismatch`; it never guesses a family.

| Source operation | Success/partial event ID | Required level | Error event ID | Required level | Proposed persisted outcome rule |
| :--------------- | -----------------------: | :------------- | -------------: | :------------- | :------------------------------ |
| `search` | 7501 | `Information` | 7511 | `Warning` | 7501 + `ok` -> `ok`; 7501 + `partial` -> `partial`; 7511 + `error` -> `error` |
| `ingest` | 7502 | `Information` | 7512 | `Warning` | 7502 + `ok` -> `ok`; 7512 + `error` -> `error` |
| `traverse` | 7503 | `Information` | 7513 | `Warning` | 7503 + `ok` -> `ok`; 7513 + `error` -> `error` |
| `case-access` | 7504 | `Information` | 7514 | `Warning` | 7504 + `ok` -> `ok`; 7514 + `error` -> `error` |
| `delete` | 7505 | `Information` | 7515 | `Warning` | 7505 + `ok` -> `ok`; 7515 + `error` -> `error` |
| `tenant-lifecycle` | 7506 | `Information` | 7516 | `Warning` | 7506 + `ok` -> `ok`; 7516 + `error` -> `error` |
| `tenant-config` | 7507 | `Information` | 7517 | `Warning` | 7507 + `ok` -> `ok`; 7517 + `error` -> `error` |
| `case-member` | 7508 | `Information` | 7518 | `Warning` | 7508 + `ok` -> `ok`; 7518 + `error` -> `error` |
| `annotation` | 7509 | `Information` | 7519 | `Warning` | 7509 + `ok` -> `ok`; 7519 + `error` -> `error` |

`partial` is currently valid only for search event 7501 and is emitted for a
successful result with one or more degraded hybrid axes. It remains an
Information-level success-family event, retains `resultCount`, and maps
`HYBRID_DEGRADED` to bounded error code `dependency_unavailable`. Any other
partial combination is rejected as `schema_mismatch` until separately ratified.

#### Typed State Mapping

Every public property of the frozen logger state has one deterministic rule.
Generated lifecycle fields (`recordId`, `markerKeyId`, `acceptedAtUtc`,
`expiresAtUtc`, and `envelopeHash`) are not copied from logger state.

| `AccessTelemetryEvent` source field | Proposed persisted destination | Total mapping rule |
| :---------------------------------- | :----------------------------- | :----------------- |
| `schemaVersion` | `schemaVersion` | Require integer `1`; reject every other value. |
| `eventId` | `eventId` | Require the exact operation/outcome/level tuple in **Source Event Mapping**. |
| `timestamp` | `emittedAtUtc` | Parse invariantly as an offset-bearing timestamp, convert to UTC, truncate rather than round sub-millisecond ticks, and emit `yyyy-MM-ddTHH:mm:ss.fffZ`; blank, offset-free, or invalid input is rejected without a clock fallback. |
| `tenantId` | `tenantMarker` | `__rejected__`, blank, or invalid scope becomes the sole synthetic marker `__rejected__`; otherwise canonicalize the accepted tenant ID and HMAC it with the active marker key and tenant domain separator. |
| `operationType` | `operationType` | Require one exact ordinal operation in **Source Event Mapping**. |
| `caseId` | `caseMarker` plus bounded `caseScope`/`targetKind` | Apply **Case, Result, and Nullable Mapping**; no raw or prefixed case identifier is persisted. |
| `user` | `userMarker` | When `tenantMarker` is `__rejected__`, persist null. Otherwise HMAC a non-blank value, including `anonymous`, with the user domain separator; blank becomes null. |
| `queryParams` | `queryParams` | Read only the exact current per-operation source-key set in **Query Parameter Source Mapping**, transform allowlisted values, and drop the named raw fields. An unlisted, wrong-case, duplicate, or wrong-typed source key rejects the lifecycle copy. The source dictionary is never serialized directly. |
| `resultCount` | `resultCount` | Apply **Case, Result, and Nullable Mapping** and require integer `0..1,000,000` when present. |
| `durationMs` | `durationMs` | Require integer `0..86,400,000`; reject rather than clamp any other value. |
| `outcome` | `outcome` | Apply the exact tuple rule in **Source Event Mapping**; no case folding or fallback. |
| `errorCode` | `errorCode` | `ok` requires null; `partial` and `error` use **Error Code Mapping**. Source text is never copied. |
| `traceId` | `traceId` | Persist null only when both trace and span are null; otherwise require a lowercase 32-hex W3C trace ID paired with a valid span ID. |
| `spanId` | `spanId` | Persist null only when both trace and span are null; otherwise require a lowercase 16-hex W3C span ID paired with a valid trace ID. |

#### Case, Result, and Nullable Mapping

`tenantMarker = __rejected__` always forces `userMarker`, `caseMarker`,
`traceId`, and `spanId` to null. Only `tenantMarker` may carry that sentinel.
For an accepted tenant, the following rules are exhaustive:

| Operation | Source `caseId` and proposed `caseMarker` rule | Proposed bounded scope/target rule | Proposed `resultCount` rule |
| :-------- | :------------------------------------------ | :--------------------------------- | :-------------------------- |
| `search` | Non-blank -> HMAC; null -> null. | `caseScope=single` when present; `caseScope=all-authorized` when null; rejected/unknown scope uses `caseScope=rejected-or-unknown`. | Required for `ok`/`partial`; null for `error`. |
| `ingest` | Non-blank -> HMAC; null is allowed only for the current EventStore ingestion adapter and remains null. | `caseScope=case` when present; otherwise `caseScope=tenant`. | Always null. |
| `traverse` | Non-blank -> HMAC; null -> null. | `caseScope=single` when present; `caseScope=all-authorized` when null; rejected/unknown scope uses `caseScope=rejected-or-unknown`. | Required for `ok`; null for `error`. |
| `case-access` | A non-blank case ID is required and HMACed. | No additional scope field; the operation is case-scoped. | Required for `ok`; null for `error`. |
| `delete` | Non-blank -> HMAC; null is allowed only for source operation `tenant-delete`. | `targetKind=memory-unit`, `case`, or `tenant`; only `tenant` permits null. | Always null. |
| `tenant-lifecycle` | Must be null. | Action is tenant-scoped. | Always null. |
| `tenant-config` | Must be null. | Action is tenant-scoped. | Always null. |
| `case-member` | A non-blank case ID is required and HMACed. | Action is case-scoped. | Always null. |
| `annotation` | A non-blank case ID is required and HMACed. | Action is case-scoped. | Always null. |

Any combination outside this table is rejected as `schema_mismatch`; it is not
silently converted to an all-tenant or all-case scope.

#### Error Code Mapping

The sanitizer trims and compares the source code in memory using ordinal
uppercase rules, then emits only one bounded V1 value. Rules are evaluated in
the order below. The final row makes the mapping total without persisting,
logging, hashing, or returning the source value, exception type/message, or
response content.

| Proposed persisted `errorCode` | Ordered source-code rule |
| :----------------------------- | :----------------------- |
| `invalid_input` | `INVALID_*`, `MISSING_*`, `PAGINATION_LIMIT_EXCEEDED`, `BATCH_TOO_LARGE`, or `NESTED_ANNOTATION_NOT_ALLOWED` |
| `not_found` | `NOT_FOUND`, `*_NOT_FOUND`, or `UNKNOWN_SOURCE` |
| `forbidden` | `FORBIDDEN`, `*_FORBIDDEN`, `AUTO_CREATE_DISABLED`, or `DIRECTORY_INGESTION_DISABLED` |
| `conflict` | `CONFLICT`, `*_CONFLICT`, `CASE_MISMATCH`, `*_DELETING`, `*_PROVISIONING`, `MEMBER_LIMIT_EXCEEDED`, `CASE_CAP_EXCEEDED`, or `MEMORY_UNIT_NOT_INDEXED` |
| `cancelled` | `CANCELLED` or `REQUEST_CANCELLED` |
| `dependency_unavailable` | `DAPR_UNAVAILABLE`, `BACKEND_UNAVAILABLE`, `ALL_BACKENDS_UNAVAILABLE`, `GRAPH_UNAVAILABLE`, `GRAPH_TIMEOUT`, `LOOKUP_BACKEND_UNAVAILABLE`, `BATCH_TRACKING_UNAVAILABLE`, `TENANT_UNAVAILABLE`, `TENANT_FAILED`, `HYBRID_DEGRADED`, or any `*_TIMEOUT` |
| `rate_limited` | `RATE_LIMITED`, `TOO_MANY_REQUESTS`, or `HTTP_429` |
| `internal_dependency_failure` | `SCHEDULE_FAILED` or `BATCH_SCHEDULING_FAILED` |
| `internal_failure` | `UNHANDLED_EXCEPTION`, `HTTP_500`, `HTTP_502`, or `HTTP_503` |
| `unknown` | Null, blank, `UNKNOWN`, any unmatched source code, or a source code longer than 128 characters |

#### Query Parameter Source Mapping

The current source-key lists are unions across all emitters for each operation.
Missing keys use only the bounded default named below. No raw identifier,
free-text value, URI, array, arbitrary enum, offset, token budget, or numeric
weight is copied.

| Operation | Exact current source keys | Proposed exact persisted keys | Proposed transformations and explicit drops |
| :-------- | :------------------------ | :---------------------------- | :------------------------------------------ |
| `search` | `axis`, `axes`, `attributeFilterCount`, `explain`, `graphWeight`, `maxResults`, `metadataFilterCount`, `nlWeight`, `offset`, `query`, `semanticWeight`, `sourceType`, `subject`, `syntacticWeight`, `tokenBudget` | `axis`, `caseScope`, `explain`, `queryLengthBucket`, `subjectPresent`, `weightProfile` | `nl` -> `natural-language`; accepted axis catalog is `syntactic`, `semantic`, `graph`, `natural-language`, `hybrid`, `graph-scoped-syntactic`, `graph-scoped-semantic`, `unknown`. Null `caseId` maps as above. `query` length and subject presence are bucketed. `weightProfile` is `configured` when all four weights are null, `request-override` when any supplied combination is finite, non-negative, and has a positive enabled-axis total, otherwise `invalid`. Drop `axes`, both filter counts, `maxResults`, `offset`, `sourceType`, `tokenBudget`, and all numeric weights after classification. |
| `ingest` | `aggregateType`, `bytes`, `cloudEventId`, `cloudEventType`, `contentType`, `eventOutcome`, `sourceType` | `caseScope`, `contentKind`, `contentLengthBucket`, `eventOutcome`, `sourceKind` | Map source kind to `file`, `url`, `event`, `command`, `projection`, `discussion`, `annotation`, or `unknown`; MIME family to `document`, `text`, `image`, `audio`, or `unknown`; byte length to `0`, `1-64KiB`, `64KiB-1MiB`, `1-10MiB`, or `10MiB+`; event outcome to `not-applicable`, `accepted`, `duplicate`, `rejected`, or `unknown`. Drop CloudEvent and aggregate identifiers. |
| `traverse` | `depth`, `edgeTypes`, `startNodeId`, `tokenBudget` | `caseScope`, `depthBucket`, `direction`, `edgeTypeCount`, `includeGaps` | Depth bucket is `0`, `1`, `2`, `3`, `4`, `5`, `6-10`, or `invalid`; count at most 16 comma-separated edge-type tokens without storing them; current direction is constant `out` and current `includeGaps` is false. Drop start-node ID and token budget. |
| `case-access` | `memoryUnitId`, `sourceUri` | `accessKind`, `projection`, `sourceKind` | `accessKind` is `memory-unit-id` or `source-uri`; current projection is `detail`; source URI scheme maps to `url`, `file`, `other`, or `unknown`, and is `not-applicable` for ID lookup. Drop the memory-unit ID and URI. |
| `delete` | `memoryUnitIdPrefix`, `operation` | `cascade`, `targetKind` | Map `memory-unit-delete` -> false/`memory-unit`, `case-delete` -> true/`case`, and `tenant-delete` -> true/`tenant`; drop the identifier prefix. |
| `tenant-lifecycle` | `operation`, `state`, `workflowInstanceIdPrefix` | `action`, `workflowState` | Map `tenant-create`, `tenant-provision-status`, and `tenant-deletion-status` to `provision`, `provision-status`, and `deletion-status`; workflow state maps to `not-applicable`, `pending`, `running`, `completed`, `failed`, `terminated`, or `unknown`. Drop the workflow prefix. |
| `tenant-config` | `changedFields`, `fieldCount`, `forceReindex`, `operation` | `action`, `changedFieldCountBucket`, `configKind`, `forceReindex` | Current action is `update`; config kind is `embedding` or `display-name`; field count maps to `0`, `1`, `2-3`, `4-8`, or `9+`; drop the `changedFields` array. |
| `case-member` | `memberIdPrefix`, `operation` | `action`, `role` | Map current operations to `add` or `remove`; current typed state does not carry role, so persist bounded `unknown`; drop the member prefix. |
| `annotation` | `memoryUnitIdPrefix`, `operation` | `action`, `annotationKind` | Current action is `create`; current typed state does not carry annotation kind, so persist bounded `unknown`; drop the memory-unit prefix. |

The ratified mapping keeps the existing six-key maximum and the 1,024-byte record
ceiling. Re-running the deterministic 90-success/10-error fixture with the
exact keys produces the following reviewable, non-Production sizing values.

#### Ratified Fixture Evidence

| Operation | Proposed representative average sanitized bytes | Proposed representative P95 sanitized bytes |
| :-------- | -----------------------------------------------: | -------------------------------------------: |
| search | 874.8 | 900 |
| ingest | 856.8 | 882 |
| traverse | 831.8 | 857 |
| case-access | 806.8 | 832 |
| delete | 770.8 | 796 |
| tenant-lifecycle | 738.8 | 764 |
| tenant-config | 775.8 | 801 |
| case-member | 772.8 | 798 |
| annotation | 784.8 | 810 |

### Persisted Schema Bounds

Version 1 is canonical UTF-8 JSON under RFC 8785 ordering, escaping, and number
rules, with no insignificant whitespace and exactly the fields above. Optional
`userMarker`, `caseMarker`, `resultCount`, and `errorCode` are JSON `null`, not
omitted; `queryParams` is always an object. Unknown, duplicate, incorrectly
cased, non-scalar, or noncanonical fields fail closed. The complete serialized
record must be at most 1,024 UTF-8 bytes.

- `schemaVersion` is integer `1`; readers reject every other version. A field,
  bound, or version change requires a new accepted decision, dual-version
  reader/inspector tests for the maximum retention window, and an explicit
  retirement gate. Unknown fields are never silently ignored.
- `recordId` is a 26-character uppercase Crockford ULID. `eventId` is exactly
  7501-7509 for `ok`, 7501 for the sole `partial` search outcome, and the
  corresponding 7511-7519 value for `error`.
  `operationType` is exactly `search`, `ingest`, `traverse`, `case-access`,
  `delete`, `tenant-lifecycle`, `tenant-config`, `case-member`, or `annotation`.
  `outcome` is `ok`, `partial`, or `error`; only search event 7501 may be
  `partial`, as fixed by **Source Event Mapping**.
- `errorCode` is `null` for `ok`; partial and error inputs use Error Code
  Mapping and map to exactly one of
  `invalid_input`, `not_found`, `forbidden`, `conflict`, `cancelled`,
  `dependency_unavailable`, `rate_limited`, `internal_dependency_failure`,
  `internal_failure`, or `unknown`. Exception types/messages and arbitrary
  producer codes are never persisted.
- `markerKeyId` matches `[a-z0-9][a-z0-9-]{0,31}`. HMAC markers are exactly 64
  lowercase hexadecimal characters; only `tenantMarker` may be
  `__rejected__`. `traceId` is 32, `spanId` is 16, and `envelopeHash` is 64
  lowercase hexadecimal characters.
- All timestamps use `yyyy-MM-ddTHH:mm:ss.fffZ`. `durationMs` is integer
  `0..86,400,000`; `resultCount` is `null` or integer `0..1,000,000`.
  Floating-point values, negative values, and numeric strings are rejected.
- `caseMarker`, `resultCount`, and their bounded scope/target companions follow
  **Case, Result, and Nullable Mapping** exactly. Only `tenantMarker` may carry
  `__rejected__`; rejected tenant scope forces user, case, trace, and span
  markers to null. `userMarker` may otherwise be null for any operation.
- `queryParams` contains at most six ordinally, lexicographically ordered keys,
  with only the per-operation keys and values in the table below. No free text,
  nested object, array, URI, identifier, or unlisted enum is accepted.
- Story 27.2 rejects a batch before its complete encoded Dapr request exceeds
  1 MiB.

#### Query Parameter Bounds

| Operation | Exact bounded `queryParams` contract |
| :-------- | :----------------------------------- |
| `search` | `axis`: `syntactic`, `semantic`, `graph`, `natural-language`, `hybrid`, `graph-scoped-syntactic`, `graph-scoped-semantic`, `unknown`; `caseScope`: `single`, `all-authorized`, `rejected-or-unknown`; `explain`: boolean; `queryLengthBucket`: `0`, `1-32`, `33-128`, `129-256`, `257-1024`, `1025+`; `subjectPresent`: boolean; `weightProfile`: `configured`, `request-override`, `invalid`. |
| `ingest` | `caseScope`: `case`, `tenant`, `rejected-or-unknown`; `contentKind`: `document`, `text`, `image`, `audio`, `unknown`; `contentLengthBucket`: `0`, `1-64KiB`, `64KiB-1MiB`, `1-10MiB`, `10MiB+`; `eventOutcome`: `not-applicable`, `accepted`, `duplicate`, `rejected`, `unknown`; `sourceKind`: `file`, `url`, `event`, `command`, `projection`, `discussion`, `annotation`, `unknown`. |
| `traverse` | `caseScope`: `single`, `all-authorized`, `rejected-or-unknown`; `depthBucket`: `0`, `1`, `2`, `3`, `4`, `5`, `6-10`, `invalid`; `direction`: `out`; `edgeTypeCount`: integer `0..16`; `includeGaps`: false. |
| `case-access` | `accessKind`: `memory-unit-id`, `source-uri`; `projection`: `detail`; `sourceKind`: `url`, `file`, `other`, `unknown`, `not-applicable`. |
| `delete` | `cascade`: boolean; `targetKind`: `memory-unit`, `case`, `tenant`. |
| `tenant-lifecycle` | `action`: `provision`, `provision-status`, `deletion-status`; `workflowState`: `not-applicable`, `pending`, `running`, `completed`, `failed`, `terminated`, `unknown`. |
| `tenant-config` | `action`: `update`; `changedFieldCountBucket`: `0`, `1`, `2-3`, `4-8`, `9+`; `configKind`: `embedding`, `display-name`; `forceReindex`: boolean. |
| `case-member` | `action`: `add`, `remove`; `role`: `unknown`. |
| `annotation` | `action`: `create`; `annotationKind`: `unknown`. |

The immutable envelope consists of every persisted field except
`acceptedAtUtc` and `envelopeHash`. The writer canonicalizes and hashes it; the
actor verifies the hash, supplies `acceptedAtUtc` once, and persists the
completed record.

Marker secrets are distinct from state-component and domain secrets and are
read through Dapr secrets. Raw tenant, user, case, query, subject, source URI,
payload, token, authorization header, credential, exception, and unbounded
metadata values are prohibited. Schema rejection is fail-closed for lifecycle
persistence and does not alter the request.

Marker rotation is a durable fixed-actor protocol with dynamic writer
membership. Writers heartbeat every 10
seconds with deployment, instance, process epoch, loaded key generation, and
old-key queue count; membership leases expire after 30 seconds. The actor first
stages a new Dapr-secret generation and freezes the old generation for new
registrations. Every writer in the live membership snapshot must acknowledge
the staged key. A joining or restarted writer must load the staged/new key and
cannot create old-key work. The actor then switches the generation and waits
until each live old-generation writer reports zero queued work or its maximum
five-minute retry age expires. Departed processes cannot recover an in-memory
queue and their lease expiry is recorded, not guessed.

The actor records the final successful old-key write and schedules durable
reminders. The verification key remains available for the 7-day maximum
retention, plus the accepted 1-second future-skew bound, plus the 15-minute
active-purge grace: at least 7 days, 15 minutes, and 1 second after that final
write. Actor reactivation reconstructs the phase and acknowledgements from
state. Missing acknowledgements, stale writers, or unknown key IDs block or
reject lifecycle persistence rather than permitting a late retired-key write.

The Server has no state read or arbitrary-delete authority. There is no
tenant-facing read API. Authorized inspection returns sanitized records only
and enforces logical expiry before returning data. Tenant deletion does not
erase retained infrastructure telemetry early; opaque markers remain only
until normal bounded expiry. This is operational behavior, not a legal-erasure
or compliance claim.

Story 27.2 must preserve the Story 20.2 tenant-denial guards
`TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState`,
`SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies`,
and
`TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies`.
It must also preserve the Story 24.3 verifier guards
`VerifyAsync_DetectsSyntacticTenantIdMismatch_ReturnsFailed`,
`VerifyAsync_DetectsSemanticTenantIdMismatch_ReturnsFailed`, and
`VerifyAsync_DetectsMissingSemanticTenantId_ReturnsFailed`.

## Rollback and Transition

Story 27.2 introduces an independently switchable lifecycle provider. Existing
JSON-console emission and optional OTLP export remain enabled and unchanged
during rollout, failure, and rollback. Disabling the provider or rolling the
Server back stops new lifecycle writes but does not change business behavior.

Rollback never deletes a Dapr component, its backing data, secrets, actor
state, or retained records automatically. The lifecycle service and fixed actor
remain deployed until every record expires, active purge completes, and the
adapter's physical-reclamation evidence passes. A separate approved
decommission operation may then remove empty storage.

An old Server image cannot meet the accepted lifecycle target. Rollback is an
explicit degraded incident state with an alert and owner, not an acceptable
steady state and not evidence that A41 is closed.

## Assurance Boundary

**Bounded infrastructure telemetry only; no tamper evidence, append-only
integrity, legal compliance, or certified audit retention.**

Dapr and state-component administrators can modify data. Sanitized records are
operational telemetry, not a cryptographic or legal audit ledger. This design
does not make retention certified, immutable, non-repudiable, or compliant with
any named regulation.

## Story 27.2 Implementation Handoff

Story 27.2 is unblocked by this accepted decision and owns this implementation
map:

1. Add exact options and Dapr-configuration validation for enablement,
   retention, queue/retry/request bounds, app/component names, profile hash,
   alpha opt-in, configuration epoch, key generations, capacity evidence ID,
   and reclamation evidence ID. Do not add a backend SDK or orchestrator API.
2. Implement the typed-state sanitizer, non-blocking provider, bounded queue,
   provider-specific filter, worker, stable record identity, canonical
   envelope/hash, complete request-size gate, Dapr service invocation, and
   non-throwing failure isolation. Do not parse stdout.
3. Implement `memories-access-telemetry`, the fixed
   `AccessTelemetryLifecycleActor/global`, transactional record/index writes,
   exact retry conflict behavior, durable state/reminders, 64-shard expiry
   buckets, bounded purge turns, health checkpoints, and logical-expiry
   inspection filtering.
4. Implement `memories-access-telemetry-clock`, signed short-lived attestations,
   the three-source/uncertainty rule, 10-second refresh, 30-second expiry,
   one-second absolute gate, nonce/replay/identity/profile validation, and
   fail-closed lifecycle behavior with independent business readiness.
5. Implement the exact Version 1 schema and catalogs, Dapr-secret marker keys,
   dynamic writer membership, staged/acknowledged/drained actor rotation, and
   retention-plus-skew-plus-grace verification-key overlap.
6. Implement the component behavioral probe for strong state, ETags,
   transactions, TTL, actor recovery, reminders, request size, throughput,
   durability, scoping/encryption, capacity, and physical-reclamation evidence.
   Alpha components are allowed only through explicit opt-in and version pin.
7. Emit the complete accepted/rejected/enqueued/persisted/retried/failed/
   dropped/expired/purged signal set, bounded reasons/gauges, health precedence,
   and `NoData` behavior without high-cardinality labels.
8. Add unit and integration tests for concurrent writers, actor serialization
   and failover, idempotent retry/conflict, transaction atomicity, exact age and
   TTL, independent-clock freshness/replay/identity negatives, purge catch-up
   and newer-record preservation, marker rotation across scale/rollout, queue
   overflow, retry/shutdown, component rejection, reconnect/revalidation,
   business-path isolation, and the named Story 20.2/24.3 privacy negatives.

## Stories 27.3 and 27.4 Verification and Operations Handoff

After Story 27.2 implementation, Story 27.3 owns only exact Production-adapter
qualification. It must prove or reject the `PG-ONPREM-1` target below, publish its
immutable profile/evidence hash, and obtain separate Platform Operations and
security approvals. Production lifecycle writes remain disabled until C1
passes. Adapter rejection is a bounded Story 27.3 result but requires another
correct-course decision and never closes A41.

Story 27.4 remains backlog until Story 27.3 is `done` and the live profile hash
exactly matches the approved C1 packet. It then owns the former verification and
close-out work:

1. Run at least two Server writers through Dapr and prove unique sanitized
   records, actor serialization, idempotent retry, conflict rejection, exact
   Dapr transaction acknowledgement, and no direct backend dependency.
2. Replace each Server, lifecycle-service, clock-service, Dapr sidecar, actor
   activation, Placement member, and Scheduler member; prove durable
   state/reminder reconstruction and continued JSON-console/optional-OTLP
   emission. Re-exercise the approved adapter fault without profile drift.
3. Prove minimum/default/maximum retention, one-second clock/future bound, late
   and already-expired records, attestation freshness/replay/identity,
   millisecond logical expiry, Dapr TTL defense in depth, bounded purge catch-up,
   transition/rollback cohorts, crash recovery, physical reclamation, and
   preservation of newer records.
4. Exercise Dapr/app/state/clock outage, stale attestation, actor failover,
   reminder delay, queue/byte exhaustion, transaction/ETag/TTL failure,
   capacity pressure, bad configuration or keys, profile drift, reconnect, retry
   exhaustion, shutdown, and degraded rollback. Business requests and business
   readiness must stay available while lifecycle health fails closed.
5. Prove the full lifecycle signal set, bounded labels, health precedence,
   alerts, `NoData`, inspection least privilege, raw-value absence,
   cross-tenant denial before lifecycle dependencies, and no tenant-facing read
   route. The tenant-negative evidence rule remains mandatory.
6. Publish a container-service-neutral lifecycle runbook plus the selected
   adapter operations appendix for rollout, component upgrade/rollback,
   capacity, alerts, recovery, inspection, key rotation, degraded old image,
   physical evidence, and verified decommission.
7. Reconcile closure-owned residuals and run terminal governed validation
   against the unchanged approved profile.
8. Only after all C2-C6 evidence and publish verification pass, coordinate the
   A41 deferred entry and action close-out. Scheduling, profile selection, or
   ADR acceptance alone is never closure.

## Production Adapter Qualification — PG-ONPREM-1

`PG-ONPREM-1` is the sole approved qualification target, not a certified profile.
Story 27.3 must keep Production lifecycle writes disabled until every C1 probe,
image pin, configured-retention capacity admission, backup/restore result, and
separate approval below passes. Any target substitution or profile drift
requires another approved course correction.

### Exact qualification profile

| Field | Qualification contract |
| :---- | :--------------------- |
| Profile ID | `postgresql-v2-dapr-1.18.1-postgresql-18.4-onprem-k8s1-openebs-local-retain-400g-v1` |
| Dapr component | `access-telemetry-store`, `type: state.postgresql`, `version: v2` |
| Dapr runtime | 1.18.1 stable, pinned by the digests below |
| Backend | PostgreSQL 18.4 from the Docker Official Image, one raw Kubernetes StatefulSet replica |
| Kubernetes target | Context `jpiquot@local`, namespace `hexalith-memories`, Kubernetes v1.34.9, one `amd64` node named `node1` |
| Availability | Single node and single PostgreSQL replica; no node, disk, zone, control-plane, or site HA claim |
| Compute | Request 4 CPU/8 GiB; limit 8 CPU/16 GiB; measured C1 load evidence is authoritative |
| Storage | 400 GiB = 429,496,729,600 bytes requested from `openebs-hostpath-retain`, `Retain`, local to `node1`; the host-path request is not a physical reservation |
| PostgreSQL 18 layout | Mount `/var/lib/postgresql`; `PGDATA=/var/lib/postgresql/18/docker` |
| Storage performance | No guaranteed IOPS/throughput; C1 must measure the exact local disk and fail capacity/latency admission when the envelope is not met |
| Network/TLS | ClusterIP only, no public or ingress endpoint, NetworkPolicy egress/ingress limited to approved identities on TCP 5432, TLS 1.2 or later, `sslmode=verify-full`, explicit internal CA bundle, service-DNS hostname verification, and documented certificate rotation |
| Database boundary | Dedicated database `memories_access_telemetry`, schema `access_telemetry`, and runtime role limited to that database/schema |
| TTL/actor | `cleanupInterval: 5m`; logical expiry and actor purge remain normative; `actorStateStore: "true"`; sole actor type `AccessTelemetryLifecycleActor`, fixed ID `global` |
| Dapr control plane | Existing three replicas each for Operator, Placement, Scheduler, Sentry, and Injector, all currently co-located on `node1`; replica count does not provide node fault independence |
| Scheduler | Three 16 GiB retained local volumes plus three 1 GiB Placement volumes on the same node |
| Retention | 1-hour minimum, configured 24-hour target, 7-day software maximum; admit only the measured duration that fits this exact profile; no backend default TTL |
| Physical reclamation | Cohort deletes plus ordinary `VACUUM (ANALYZE, INDEX_CLEANUP ON)`; `pgstattuple` and table/index statistics must prove bytes returned to the PostgreSQL allocator within 24 hours. This is not an OS-disk-shrink claim. |

PostgreSQL 18.4 is the latest stable/current minor release on 2026-07-20;
PostgreSQL 19 is beta and is excluded. See the
[PostgreSQL versioning policy](https://www.postgresql.org/support/versioning/),
[PostgreSQL 18.4 release notes](https://www.postgresql.org/docs/release/18.4/),
and [PostgreSQL Docker Official Image](https://hub.docker.com/_/postgres?tab=tags).

The PostgreSQL v2 Dapr state component is selected for qualification because it
supports the required state, transactional, ETag, TTL, and actor surfaces. A
published capability row is not certification; the running profile must pass
the behavioral contract. See the [Dapr PostgreSQL v2 component reference](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-postgresql-v2/).

### Required Dapr component configuration

The Production component is generated from this non-secret contract. The
OpenBao-backed connection string must contain the approved private FQDN,
database, runtime role, timeout, CA path, and `sslmode=verify-full`. It never
appears in application configuration or evidence.

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
      value: "40"
    - name: connectionMaxIdleTime
      value: 5m
    - name: actorStateStore
      value: "true"
auth:
  secretStore: access-telemetry-secrets
scopes:
  - memories-access-telemetry
```

**`maxConns` corrected 2026-07-27 by approved Sprint Change Proposal 2026-07-27**
(`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md`).
This block previously pinned `maxConns: "64"`. Two lifecycle replicas each open their
own sidecar pool, so `"64"` demands 128 connections against `max_connections=100` and
exhausts the server during the ADR two-writer probe. `"40"` yields
`2 x 40 + 3 superuser-reserved + 10 evidence sessions = 93` and is the shipped value in
`deploy/kubernetes/base/dapr/access-telemetry-store.yaml`, guarded by
`ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryConnectionPoolFitsPostgreSqlMaxConnections`.
This is an approved profile change, not a documentation repair. It supersedes the
`maxConns` pinning in Sprint Change Proposal 2026-07-20, which remains append-only and is
not edited in place.

**Authoritative profile hash.** The approved `PG-ONPREM-1` profile hashes to
`profile_sha256 dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14` and
`mutation_manifest_sha256 2983ccdebedbd12e34bb1aec363335eb825301ce92d1c4ed87f8956d9c176b84`.
The artifact carrying the hash is `canonical_pg_onprem_profile()` in
`tools/verify_access_telemetry_lifecycle.py`, pinned by
`tests/tooling/access_telemetry_lifecycle/test_adapter_profile.py::AdapterProfileTests::test_canonical_pg_onprem_profile_hash_is_pinned`.
The hash covers the canonical profile object - identity, capabilities and workload - not
the rendered Kubernetes manifests and not the running cluster state. The AC4 hash-bound
approvals of Story 27.3 bind to this value.

`queryIndexes` is intentionally absent. PostgreSQL v2 does not implement the
Dapr Query API, and the portable lifecycle owns explicit transactional
expiry-bucket keys; adding a backend query path would violate the Dapr-only
contract.

### Required immutable image set

Every reviewed workload is pinned by registry digest. Tags, placeholders, and
container runtime layer IDs fail C1.

| Workload | Required digest |
| :------- | :-------------- |
| Dapr sidecar | `ghcr.io/dapr/daprd@sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8` |
| Dapr Operator | `ghcr.io/dapr/operator@sha256:89661f52a3d37f5d528c35dd9d2b4ac76c7b274bd459c8570d6246b6bfdda549` |
| Dapr Placement | `ghcr.io/dapr/placement@sha256:6caf20016d115d4a7f133b9206b739a10abd9f558d76683b27be9ab60f759e26` |
| Dapr Scheduler | `ghcr.io/dapr/scheduler@sha256:c9bb9ada0cd6a63cd92c26470da1985124e423432af4e39f09b96979fd1059c0` |
| Dapr Sentry | `ghcr.io/dapr/sentry@sha256:2f98508dff56c75329dbd51674c89f41ce349e06c7744ab2519cb69ba338d41f` |
| Dapr Injector | `ghcr.io/dapr/injector@sha256:2793b954b1aef142d59bd5eae71bec4de5f71d16e9ad80fec81cbf3b4eea428c` |
| PostgreSQL | `docker.io/library/postgres:18.4-trixie@sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a`; the `linux/amd64` manifest is `sha256:d93de42662696f278fb34354b06fdaa90ad7ca3106d6f72fbd01d16da006d2cf` |
| Memories Server | `registry.hexalith.com/memories@sha256:71e49b6e806ec2fa7c221e58600ba02115693923db05915663396be01b1c042c`, unless Story 27.3 changes Server code, in which case the replacement CI digest is mandatory |
| Lifecycle service | **Missing and blocking:** replace `0.0.0` with signed CI output `registry.hexalith.com/memories-access-telemetry@sha256:<64-hex>` |
| Clock service | **Missing and blocking:** replace `0.0.0` with signed CI output `registry.hexalith.com/memories-access-telemetry-clock@sha256:<64-hex>` |
| OpenBao | `quay.io/openbao/openbao@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653` |

The PostgreSQL manifest must resolve to the listed `linux/amd64` identity on
`node1`. The two missing application digests are deliberately not invented. C1
cannot pass until both images are built from reviewed source, signed, scanned,
deployed by digest, and recaptured from live Pod `imageID` values.

### Capacity contract

All operands are integer bytes/counts and all arithmetic is checked or
arbitrary precision.

| Retention | Records | Canonical payload bytes |
| :-------- | ------: | ----------------------: |
| 1 hour | 900,000 | 921,600,000 |
| 24 hours | 21,600,000 | 22,118,400,000 |
| 7 days | 151,200,000 | 154,828,800,000 |

```text
baseBytes = records * (measuredRecordBytes + measuredIndexBytes) * 1
controlBytes = 34,359,738,368
reclamationWorkspace = max(137,438,953,472, ceil_div(baseBytes, 4))
requiredPeak = baseBytes + controlBytes + reclamationWorkspace
schedulerBytes = 3 * 17,179,869,184
totalPlatformRequired = requiredPeak + schedulerBytes
```

The durability multiplier is `1` because this profile has one PostgreSQL data
copy; backups are recovery evidence, not a synchronous database replica. The
400 GiB PVC request has these exact gates:

| State | Bytes |
| :---- | ----: |
| Maximum steady-state admission (70%) | 300,647,710,720 |
| Reclamation critical boundary (80%) | 343,597,383,680 |
| Lifecycle Unhealthy boundary (90%) | 386,547,056,640 |

Measured record/index amplification, WAL, autovacuum, backup, allocator, and
cohort-reclamation evidence must fit the formula for the configured 24-hour
target. The 7-day software maximum is rejected unless a measured result fits or
a larger-storage profile is separately approved. Because OpenEBS host-path
shares node storage, C1 also captures current filesystem headroom and competing
volume use; the PVC request alone is not capacity reservation. Platform
Operations must attach node/storage availability, operating cost, monitoring,
and funding approval. A node, storage class, size, or resource-envelope
substitution changes the profile hash.

### Declared fault model

The in-profile single-component fault is forced loss of the PostgreSQL
container/process and replacement of its StatefulSet pod while `node1` and the
bound retained OpenEBS local volume remain healthy. C1 injects this fault under
the two-writer workload and proves zero loss of every transaction Dapr
acknowledged before the fault. It records disconnect duration, retries,
queue/drop accounting, service-DNS reconnection, PostgreSQL crash recovery,
actor reactivation, reminder reconstruction, and observed recovery. A Dapr
sidecar restart remains a required process-fault test but is not the declared
backend fault.

Loss of `node1`, the local disk/PV path, the Kubernetes control plane or site,
operator data deletion, credential compromise, and logical corruption are
outside the declared profile. `PG-ONPREM-1` is not HA and must never be described
as node-, disk-, zone-, or site-redundant. Platform Operations must attach a
named backup destination, successful restore evidence, and the resulting
potentially nonzero RPO/RTO before C1 can pass. A zero-loss node-failure claim
requires multiple fault-independent nodes and replicated storage or an external
on-premises HA PostgreSQL service under a new approved profile.

### Approval and assurance gate

Hexalith Platform Operations approves node/storage capacity, operating cost,
operation, the bounded pod/process fault, maintenance, reclamation, upgrade,
rollback, backup/restore, and outside-profile RPO/RTO, and explicitly
acknowledges the absence of node/disk/site HA. A separate security reviewer
approves ClusterIP-only TLS `verify-full`,
OpenBao scope, least-privilege database authority, Dapr ACL/component scopes,
NetworkPolicy, encryption, privacy, image signature/vulnerability evidence,
evidence hashes, and the required cross-tenant denial results. Each approval is
named, dated, and bound to the immutable profile hash; neither may be inferred
from the other or from Administrator approval of the planning corrections.

The Redis `state.redis/v1` profile with one 20 GiB volume and `appendfsync
everysec` is rejected for C1: its 24-hour canonical payload already exceeds the
reservation before overhead, and the exact profile has not proved rollback-
atomic multi-key failure behavior or zero acknowledged loss. This rejection
does not select a direct PostgreSQL application dependency; Memories and the
lifecycle service remain Dapr-only.

The assurance boundary remains bounded infrastructure telemetry only. This
profile does not make records tamper-evident, append-only, legally compliant,
immutable, non-repudiable, or certified audit retention.
