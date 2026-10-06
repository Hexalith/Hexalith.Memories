# Access Telemetry PostgreSQL 18.6 Production Appendix

## Scope and immutable profile

Adopted **2026-10-04** from the exact approved PG-ONPREM-2 candidate. This is
repository configuration adoption; no target was contacted and no capture,
security requalification, Production activation, Story 27.4 advancement or A41
closure is credited. The closed PG-ONPREM-1 constructor/hash and historical
operator packets retain their original identity and have no successor credit.


This appendix specializes
[Access Telemetry Lifecycle Operations](access-telemetry-lifecycle.md) for the sole
approved qualification target `PG-ONPREM-2`. It does not replace the neutral
runbook or certify the profile.

| Field | Exact value |
| :---- | :---------- |
| Profile ID | `postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-v2` |
| Profile SHA-256 | `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` |
| Dapr component | `access-telemetry-store`, `state.postgresql/v2`, actor state store |
| PostgreSQL | 18.6, one raw StatefulSet replica, digest pinned by ADR 27.1-001 |
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
PG2 additionally binds the exact PostgreSQL/Dapr platform manifests, OpenBao
image/chart/values/render, twelve fixed configuration/security inputs and three
workload files. Derived qualification hashes are overlay patches; the approved
reporter input stays byte-identical. All other running observations retain
separate artifact hashes. Never patch an
approved profile packet in place.

## C1.16 component and backend capture

Story 27.22 owns only C1.16 and is done after independently accepted
component/backend identity and full-set linkage for the captured 2026-10-06
window. The dated disposition below records that acceptance; future captures
require separately scoped authority and reviewer assignment. Its callable
identity producer is:

```powershell
pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.16 -ProfileId PG-ONPREM-2 -EvidenceDirectory /approved-evidence/access-telemetry-c1/C1.16
```

The directory is an operator-supplied external archive, not an authorized target
or existing evidence location. The producer requires exact approved source bytes,
stable Component/pod identities, the authenticated PostgreSQL/Dapr index or
linux/amd64 child, actual PostgreSQL 18.6 / 180006 and `peer:postgres` on the
read-only local socket. Advertised capabilities are recorded as advertisements.
`connectionLinkage: not-evaluated` remains until independent evidence links the
Dapr connection to this backend; a secret reference and local peer query cannot
prove that connection. Captures retain `gateStatus: not-evaluated` and grant no
behavioral, independent-review or activation credit. PG1 C1.16 remains a separate
historical mode requiring `-AllowHistoricalProfileCapture`; C1.15/PG2, lowercase
successor literals and historical opt-in/PG2 are refused before calls/output.

### Separate C1.16 connection linkage candidate

Historical preparation record, superseded for the accepted window below:

Offline linkage support was prepared on **2026-10-06**. Live collection, external
archive retention and independent disposition remain pending. The configured
target observed on 2026-10-05 runs PostgreSQL 18.4 and is ineligible for exact
PG2; `/approved-evidence` is an example, absent locally. This preparation grants
no target contact, scaling, qualification enablement or lifecycle-write authority.

On a separately authorized eligible target, use PowerShell 7.5 or later, with
`psql` in the backend container and `nc`, `grep` and `/bin/sh` in the lifecycle
container. The restricted client requires `nc -w 5` to connect directly to
`127.0.0.1:3500`, retain native exit status, and preserve HTTP response bytes.
`grep -q` must distinguish a match (0), no match (1), and execution errors; a
missing or failing validator refuses the request. Metadata must return one
HTTP/1.0 or HTTP/1.1 200 response with an exact `Content-Length`, complete bounded
headers, unencoded JSON and no chunked transfer encoding. The state GET must
return one complete 204 response with zero body bytes, including whitespace.
Responses are bounded to 1 MiB and headers to 16 KiB. Redirects are refused, and
proxy environment variables cannot affect the direct TCP destination. Missing
capabilities fail closed; this collector does not install utilities or change
approved images. Use the following command with the approved lifecycle pod name:

```powershell
pwsh ./tools/verify-access-telemetry-c1-linkage.ps1 -Gate C1.16 -ProfileId PG-ONPREM-2 -Mode Observe -LifecyclePod <approved-lifecycle-pod> -ScopeFile /approved-evidence/access-telemetry-c1/C1.16/linkage-scope.json -EvidenceDirectory /approved-evidence/access-telemetry-c1/C1.16/linkage
```

The operator approval receipt must be a bounded JSON object with exactly these
fields. Replace every placeholder through the separately approved operator
process; this example is neither an authorization nor a runnable receipt:

```json
{
  "schemaVersion": "hexalith.access-telemetry.c1.linkage.scope/v1",
  "gate": "C1.16",
  "profileId": "PG-ONPREM-2",
  "profileSha256": "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe",
  "context": "jpiquot@local",
  "namespace": "hexalith-memories",
  "lifecyclePod": "<approved-lifecycle-pod>",
  "evidenceDirectory": "/approved-evidence/access-telemetry-c1/C1.16/linkage",
  "authorizedFromUtc": "<approved UTC start ending in Z>",
  "authorizedUntilUtc": "<approved UTC end ending in Z>",
  "authorizationReference": "<retrievable operator approval reference>",
  "independentReviewer": "<named independent reviewer>",
  "exclusiveReadWindow": true,
  "exclusiveReadWindowEvidenceSha256": "<64 lowercase hex characters>",
  "collectorSha256": "<64 lowercase hex characters>",
  "identityHelperSha256": "<64 lowercase hex characters>",
  "lifecycleImageSha256": "<approved lifecycle image digest without sha256 prefix>"
}
```

The receipt is limited to 16 KiB and an active interval no longer than 15 minutes.
Bind `collectorSha256` and `identityHelperSha256` to the exact deployed bytes of
`tools/verify-access-telemetry-c1-linkage.ps1` and
`tools/access-telemetry-c1-component-backend.ps1`, using `Get-FileHash -Algorithm
SHA256` and lowercase hex. `lifecycleImageSha256` binds the approved application
image; PostgreSQL and Dapr must match the exact PG2 approved index or authenticated
linux/amd64 child. The collector also refuses drift in the sixteen approved
configuration/workload inputs before calls or directory creation. Invalid mode,
historical profile, source approval, archive binding or scope fails at the same
boundary. No Secret values or credentials belong in the receipt.

The approval must cover selected Component and pod identity observations,
authenticated Dapr metadata, one authenticated read-only state GET, and two
read-only local PostgreSQL evidence sessions. It must identify protected runtime
credential access and independently retained evidence that the selected sidecar's
read window is exclusive, including background component activity. The collector
does not establish or change traffic isolation, enable workloads, acquire a Lease,
or obtain credentials. Operators must use a separately authorized procedure for
the exclusive window; uncertainty about concurrent or sequential pool reuse is
a blocker.

The collector generates a random absent synthetic key and performs one strong
Dapr GET. It rejects all response body bytes and failed/truncated transports,
exporting only a direct HTTP 204 absence observation. The key, token, connection
string, SQL query text and tenant
records are never written into the packet. PostgreSQL snapshots project only
selected session identity/timestamps and TLS status for the exact selected pod IP,
plus actual 18.6 / 180006 and local `peer:postgres` evidence identity. A single
stable idle TLS 1.2+ runtime-role/database session must advance within the bounded
challenge interval. Missing, duplicate, active, stale, replaced or unadvanced
sessions, wrong images/identity, credential-shaped output, timeouts and changed
pod UID/IP/container incarnation fail closed with no positive observations.
Backend observations must be within five seconds of collector UTC, and a session
must not predate either selected Dapr/backend container start by more than that
five-second clock tolerance. Activity must advance within the GET interval with
a one-second tolerance; the two snapshots must span no more than 30 seconds.
The reviewer must confirm clock agreement within these bounds.

Activity timestamps describe the last command, as documented in
[PostgreSQL session statistics](https://www.postgresql.org/docs/18/monitoring-stats.html#MONITORING-PG-STAT-ACTIVITY-VIEW).
They cannot establish exclusive use of a reused pool connection. The independent
reviewer must verify the referenced exclusive-window evidence, the operator
authority and selected target, both snapshots, challenge interval, source/command
hashes and immutable C1.16 identity packet together. If this attribution cannot
be independently established, keep linkage pending and retain the blocker.

The attribution label is
`candidate-session-correlation-requires-independent-exclusive-window-verification`.
It describes a provisional correlation; the receipt does not establish the
exclusive window or independent connection linkage.

The `commands` ledger retains its `purpose`/`sha256` shape. For
`linkage-absent-state-get`, the digest covers a reconstructable command template:
replace the entire private `c1-linkage-absent-<64 hex characters>` path segment
with literal `__KEY__`, retaining every other argument byte. Reconstruct the probe
from the approved collector's `Get-LinkageHttpProbe` function definition and this
literal path. Evaluating only that function produces a string without contact;
do not execute or source the collector to verify a hash.

```powershell
$probe = Get-LinkageHttpProbe '/v1.0/state/access-telemetry-store/__KEY__?consistency=strong'
$template = @('--context', 'jpiquot@local', '-n', 'hexalith-memories', 'exec', '<approved-lifecycle-pod>', '-c', 'lifecycle', '--', '/bin/sh', '-ec', $probe)
$bytes = [Text.Encoding]::UTF8.GetBytes('kubectl ' + ($template -join [char]0x1f))
[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
```

The resulting lowercase digest must equal the ledger entry. Other command
digests cover the exact argument vector, using the same `kubectl ` prefix,
U+001F argument separators, UTF-8 encoding and SHA-256. No hidden challenge key
is required for verification.

Two prerequisites were recorded before live use:
Platform Operations must bind target authority to the actual API endpoint and
trust fingerprint and protect the kubeconfig/context mapping throughout the
observation window (B6). A context name alone can be repointed. Deployment Adapter
Developer must execute the emitted session SQL contract in an isolated PostgreSQL
18.6 environment, proving exact aliases, role/database, timestamp and TLS shapes
without tenant records (V3). Synthetic kubectl JSON does not execute that SQL.
These prerequisites and their dated dispositions are recorded in the
[deferred-work ledger](../../_bmad-output/implementation-artifacts/deferred-work.md)
and grant no target contact or C1.16 disposition.

### Isolated emitted SQL contract verification

The separate integration lane requires Docker on `linux/amd64`, PowerShell 7.5+
and OpenSSL. Prefetch only the approved PostgreSQL child before running it:

```bash
timeout 180 docker pull --platform linux/amd64 docker.io/library/postgres@sha256:0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_sql_contract -p '*_test.py' -v
```

Missing image/runtime/TLS prerequisites and timeouts fail without skips. The
lane creates uniquely named, labelled containers on an internal disposable
network, publishes no ports, and keeps the database in tmpfs. It extracts the
deployment's database/runtime-role bootstrap and peer mapping, uses disposable
credentials and a local CA with service-DNS `sslmode=verify-full`, and loads only
necessary PowerShell function definitions. Its local adapter executes the actual
emitted `env`/`psql` argument vector; it never sources collector top-level code,
calls Kubernetes, or manufactures session JSON. It exercises valid and denied
real observations, a temporary alias mutation, missing prerequisites and timeout
cleanup. The deliberate non-TLS scenario changes only the owned local server's
HBA temporarily and restores it.

Each run prints a new `/tmp/c1-sql-receipt-*/receipt.json` path and SHA-256. The
read-only, secret-safe receipt binds collector/helper/deployment/test and emitted
query hashes, exact image/server identity, commands/results, raw psql log hashes,
per-test outcomes and owned-resource cleanup. A `failed` receipt or nonzero test
exit leaves V3 unresolved. Copy receipts and raw logs to an independently retained
archive before relying on them; `/tmp` is local verification evidence only.

**Correction verified, 2026-10-06:** real execution found that original
`a.client_addr::text AS "clientAddr"` returned an IPv4 address with `/32`, causing
the existing validator to refuse the valid idle TLS session. After explicit user
approval, the collector now emits `host(a.client_addr) AS "clientAddr"`. All ten
integration methods pass, including a temporary-source regression restoring the
old projection and its exact denial, plus the broken-alias mutation. Receipt
`/tmp/c1-sql-receipt-gylthuxh/receipt.json` has SHA-256
`12b08a9a9030c8b6d5ddec3437d44132127ceaea96fe42f3bd4f9c8e2050aaa3` and
`status: passed` with successful owned cleanup. Deployment Adapter Developer's
V3 disposition was submitted for independent review; the reviewed completion
below supersedes that pending state. Local execution grants no live qualification.

**Reviewed V3 completion, 2026-10-06:** independent build review and its
receipt/cleanup recheck are complete. Parent final verification passed 19/19
methods (10 real PostgreSQL tests and 9 harness regressions), zero failures,
errors or skips, in 26.289 seconds. Final receipt `/tmp/c1-sql-receipt-3bd_gjy6/receipt.json` has SHA-256
`c42eb91b0b356cee4135c8da8b2289e07469a93b289eae8aa87425f226ac52b9`; raw runner log `/tmp/story-27-22-parent-final-sql.log` has SHA-256 `32482e58654fba8683843c39b4892dd6673c260a38e0233907cdeffb1978a111`. The receipt
requires every expected test identity and successful final outcomes after cleanup;
skips, partial suites, failed inspection and source/finalization failures cannot
produce a passing qualification receipt. **Only V3's isolated emitted-SQL
prerequisite is closed.** Re-execute and review after source/query/profile drift.

Historical V3-only summary, superseded for the accepted window below:

B6 target endpoint/trust authority and all eligible-target, credential, exclusive
window, archive and reviewer prerequisites remain required. This local lane
establishes no Dapr connection linkage, independent C1.16 acceptance, other-gate,
Production, Story 27.4 or A41 credit. Story 27.22 remains in-progress and its
C1.16 checkpoint remains pending / not complete.

Each live candidate collector invocation creates a new read-only `c1.16-connection-linkage-candidate-*.json`
receipt, preserving prior files. Retain its SHA-256, the separate unchanged
identity-producer packet/hash, scope approval, exclusive-window evidence, and a
durable external archive receipt before handing them to the named reviewer.
Local read-only file attributes are an immutability safeguard, not external
archive or independent review proof. Exit zero and `collectorStatus: observed`
mean candidate observations only. Every packet retains `connectionLinkage`,
`componentBehavior`, `productionLifecycleWrites` and `gateStatus` as
`not-evaluated`, `productionGatePassed: false`, and independent disposition
`pending`. A nonzero exit grants no positive partial evidence. Neither receipt
changes the C1.16 checkpoint or grants C1.17, another C1 gate, security,
Production, Story 27.4 or A41 credit.

### Independently accepted captured C1.16 — 2026-10-06

The [accepted disposition](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/independent-c1.16-accepted-disposition.json), SHA-256 `ad2d3024dff5cd00cb8518a3e65b16d006acf596046d193373d0ae55ea2cff70`, supersedes the pending C1.16 summaries above only for this closed actual PG2 window. Story27.22 is done and its C1.16 checkpoint is completed; all three build reviews and final repository checks are complete.

The [separately reviewed full-set procedure](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/independent-external-pool-procedure-disposition.json) retains every session from the unchanged selected-IP SQL. Two stable idle runtime TLS sessions were observed; exactly one existing session advances both activity clocks around one genuine204/empty-body GET, with the entire other row unchanged and independently verified exclusive controls. The canonical collector still refuses two sessions with exit1/session-attribution-ambiguous and null observations; its denied packet remains immutable and no filtered or repaired input is supplied.

[Closure](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/exclusive-window-post.json) and [final controls](../../../../../evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa/capture-pool-20261006T150600Z-3c2f874a/final-operator-controls.json) verify restored ingress, actual owned-probe absence, disabled lifecycle/Production controls, clock0 and unchanged retained storage. Neutral producer fields, prior dated observations and V3 history remain unchanged. This acceptance grants no other gate, Production activation, Story27.4 or A41 credit. Retained sources and evidence are local archive material; another capture requires fresh authority and review.

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
baseBytes = records * (measuredRecordBytes + measuredIndexBytes) * 2
controlBytes = 34,359,738,368
reclamationWorkspace = max(137,438,953,472, ceil_div(baseBytes, 4))
requiredPeak = baseBytes + controlBytes + reclamationWorkspace
schedulerBytes = 3 * 17,179,869,184
totalPlatformRequired = requiredPeak + schedulerBytes
```

**Dated correction 2026-10-04:** the retained executable admission multiplier is `2`,
budgeting one durable copy plus its WAL/snapshot copy. The earlier appendix multiplier
`1` understated this unchanged admission rule; the historical PG1 ADR table remains
historical. This correction grants no capacity relaxation or synchronous replica.
Admit steady state only at or below 300,647,710,720 bytes
(70%). Treat 343,597,383,680 bytes (80%) as critical and 386,547,056,640 bytes
(90%) as lifecycle Unhealthy. The 168-hour (`7d`) software maximum remains
evidence-only and is never admitted on this exact 400-GiB profile, even if a measured
requirement fits. A changed admission rule requires a new approved profile.

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

Pin the PostgreSQL 18.6 image and `linux/amd64` identity from ADR 27.1-001. For a
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
