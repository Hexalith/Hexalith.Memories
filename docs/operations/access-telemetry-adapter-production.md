# Access Telemetry PostgreSQL 18.4 Production Appendix

## Scope and immutable profile

This appendix specializes
[Access Telemetry Lifecycle Operations](access-telemetry-lifecycle.md) for the sole
approved qualification target `PG-ONPREM-1`. It does not replace the neutral
runbook or certify the profile.

| Field | Exact value |
| :---- | :---------- |
| Profile ID | `postgresql-v2-dapr-1.18.1-postgresql-18.4-onprem-k8s1-openebs-local-retain-400g-v1` |
| Profile SHA-256 | `dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14` |
| Dapr component | `access-telemetry-store`, `state.postgresql/v2`, actor state store |
| PostgreSQL | 18.4, one raw StatefulSet replica, digest pinned by ADR 27.1-001 |
| Database/schema | `memories_access_telemetry` / `access_telemetry` |
| Storage | 400 GiB OpenEBS local retained volume on `node1`; request is not a reservation |
| Compute | request 4 CPU/8 GiB; limit 8 CPU/16 GiB |
| Connection pool | `maxConns: "40"`; two sidecars plus reserved/evidence sessions fit `max_connections=100` |
| Cleanup | `cleanupInterval: 5m`; actor purge remains normative |
| Network | ClusterIP only, TCP 5432 from approved identities, TLS 1.2+, `sslmode=verify-full` |
| HA boundary | no node, disk, zone, control-plane, or site high-availability claim |

Any change to image digest, Dapr/runtime/component version, context, namespace,
node, storage class/size, topology, resources, TLS identity, connection pool,
retention admission, or workload invalidates the approved deployment evidence and
requires a new approved decision. It changes the canonical profile hash only when
it changes a field in the ADR-defined canonical identity/capability/workload object;
all other running observations retain separate artifact hashes. Never patch an
approved profile packet in place.

## Ownership and secret boundary

The lifecycle application reaches PostgreSQL only through Dapr. The OpenBao-backed
connection string is visible only to the Dapr component and includes the dedicated
runtime role, private service DNS name, `memories_access_telemetry` database,
connection timeout, internal CA path, and `sslmode=verify-full`. Do not copy it into
application configuration, a shell transcript, a metric, or evidence.

The runtime role is limited to the dedicated database/schema and the Dapr-owned
tables. The adapter operator owns PostgreSQL maintenance and evidence sessions;
those credentials are never mounted into Memories or the lifecycle service.

## Retention, capacity, and cost admission

Use integer bytes and the measured profile formula:

```text
baseBytes = records * (measuredRecordBytes + measuredIndexBytes) * 1
controlBytes = 34,359,738,368
reclamationWorkspace = max(137,438,953,472, ceil_div(baseBytes, 4))
requiredPeak = baseBytes + controlBytes + reclamationWorkspace
schedulerBytes = 3 * 17,179,869,184
totalPlatformRequired = requiredPeak + schedulerBytes
```

The one-copy durability multiplier is `1`; backups are recovery evidence, not a
synchronous replica. Admit steady state only at or below 300,647,710,720 bytes
(70%). Treat 343,597,383,680 bytes (80%) as critical and 386,547,056,640 bytes
(90%) as lifecycle Unhealthy. The 168-hour software maximum is not admitted merely
because it is permitted by validation; it needs measured fit or a larger approved
profile.

Before rollout record measured record/index amplification, WAL and snapshot bytes,
tombstones/dead tuples, control overhead, reclamation workspace, Scheduler/Placement
volumes, host-filesystem headroom, competing volumes, storage performance, service
quota, price, funding owner, and evidence date. The 400-GiB PVC request is not
physical capacity or performance evidence.

## PostgreSQL monitoring and alarms

Collect only aggregate operations data. Never select telemetry payload columns into
an evidence transcript. Bind every SQL result to database/schema/table, UTC time,
profile hash, command hash, and nonzero row count.

Monitor connection utilization, transaction latency/errors, checkpoints, WAL,
database/table/index bytes, live/dead tuple estimates, autovacuum progress, locks,
disk headroom, and restart/recovery state. Use `pg_stat_database`,
`pg_stat_user_tables`, `pg_stat_user_indexes`, `pg_stat_progress_vacuum`,
`pg_database_size`, `pg_total_relation_size`, and the approved `pgstattuple`
extension through a restricted evidence role.

Alert at 70/80/90% capacity, pool exhaustion, TLS/authentication failure, Dapr
transaction or ETag failure, stalled autovacuum, excessive dead tuples, oldest-due
age above 15 minutes, or physical-evidence age approaching 24 hours. A missing
series is Unhealthy/NoData according to the neutral health precedence; it is never
assumed zero.

## Purge and physical reclamation proof

The lifecycle actor first proves logical purge through Dapr Delete, strong Get
absence, and index-member removal. PostgreSQL evidence begins only after that
immutable cohort packet exists.

1. Record the cohort ID, purge completion UTC, database, schema, Dapr table identity,
   candidate/deleted/already-absent/index-removal counts, and newer control cohort.
2. Use aggregate `pgstattuple` and table/index statistics to record allocator bytes,
   live tuples, dead tuples, and relation sizes before maintenance. Do not retrieve
   record values.
3. Execute ordinary `VACUUM (ANALYZE, INDEX_CLEANUP ON)` on the exact approved table.
   Do not use `VACUUM FULL`, table rewrite, ad hoc delete, or storage deletion as the
   normal proof.
4. Re-run the same aggregate collectors. Attribute the change to the same cohort and
   show all newer control records remain logically present.
5. Pass only when PostgreSQL allocator evidence decreases within 24 hours of active
   purge. Report relation-file or operating-system disk shrink as `not claimed`.

If reclamation misses the bound, mark lifecycle health Unhealthy, preserve the
evidence packet, stop claiming physical reclamation, and escalate to the adapter
owner. Do not call PostgreSQL maintenance APIs from Memories code.

## Incident recovery

For the in-profile fault, force loss and replacement of only the PostgreSQL
container/process while `node1` and the retained local volume remain healthy. Under
the two-writer workload, capture every Dapr acknowledgement before the fault,
disconnect duration, retries, queue/drop accounting, DNS/TLS reconnection,
PostgreSQL crash recovery, actor/reminder reconstruction, and observed zero
acknowledged-record loss.

On pool exhaustion, do not raise `maxConns` independently. Stop lifecycle writes,
preserve business readiness and console/OTLP emission, identify leaked/long-running
evidence sessions, and restore the approved pool profile. On WAL, dead-tuple, or
capacity pressure, stop admission growth and follow the approved vacuum/capacity
plan; never discard telemetry early.

Node, local-disk/PV, control-plane, site, operator deletion, credential compromise,
and logical corruption are outside profile. Use the named backup destination and
last successful restore rehearsal to state the potentially nonzero RPO/RTO. Never
describe a successful pod restart as node or site recovery.

## Backup, restore, and RPO/RTO

Before Production enablement, Platform Operations records the backup destination,
encryption/retention owner, successful restore command and UTC interval, restored
database/schema/table identity, consistency validation, resulting RPO/RTO, and
outside-profile statement. Security separately reviews backup authority and secret
handling. A scheduled backup, retained volume, or successful command exit without
restored-state validation is not evidence. Until a restore rehearsal establishes
measured limits, the outside-profile recovery claim is potentially nonzero RPO/RTO.

Restore into an isolated approved environment, validate the immutable profile and
schema, prove sanitized record/index/actor checkpoint consistency without exposing
payloads, and destroy the rehearsal environment under its authorized procedure.
Production restore is an incident decision and may rewind acknowledged state; record
the actual loss window rather than repeating the in-profile zero-loss claim.

## Upgrade and rollback

Pin the PostgreSQL 18.4 image and `linux/amd64` identity from ADR 27.1-001. For a
minor image, Dapr component, OpenEBS, Kubernetes, or Dapr runtime change, create a
new profile decision, verify backup/restore, capacity, TLS, transaction/ETag/TTL,
actor/reminder, throughput, fault, purge, and reclamation behavior, then obtain both
same-hash approvals.

Rollback disables new lifecycle writers first but leaves PostgreSQL, Dapr, the
lifecycle service, actor, clock, secrets, and existing records operating until
expiry and reclamation finish. Do not delete the StatefulSet, PVC/PV, database,
schema, keys, or backups automatically. A rollback to an old writer image is a
degraded incident with explicit owner and alert.

## Certificate, credential, and marker-key rotation

Rotate the PostgreSQL server certificate and internal CA through the adapter/OpenBao
owners. Verify service-DNS hostname validation and `sslmode=verify-full` before
revoking the old certificate. Rotate the database credential without exposing either
generation, observe sidecar reconnection, then re-run transaction and least-privilege
checks.

Lifecycle marker-key rotation follows the neutral durable actor protocol. PostgreSQL
administration must not re-HMAC, rewrite, inspect, or delete record values. Keep the
old verification key for the full 7-day maximum plus 15-minute purge grace and
1-second skew after the final old-key write.

## Verified decommissioning

Confirm all writers are disabled, the last cohort exceeded its absolute expiry,
active purge passed, `VACUUM (ANALYZE, INDEX_CLEANUP ON)` evidence is attributable
and within bound, newer/control counts are zero as expected, actor/reminder state is
retired, backup/evidence retention decisions are recorded, and two authorized owners
approve decommissioning. Scale down application surfaces before removing Dapr
components or OpenBao material. PVC/PV/database deletion is a separate destructive
approval because the OpenEBS policy is `Retain`.
