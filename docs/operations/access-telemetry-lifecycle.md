# Access Telemetry Lifecycle Operations

## Purpose and assurance boundary

This runbook operates the Dapr-only access-telemetry lifecycle defined by
[ADR 27.1-001](../dev/adr-27.1-001-access-telemetry-lifecycle.md). It is
container-service neutral: the operator may use any platform that preserves the
reviewed Dapr app, component, actor, configuration, secret, and identity contracts.

The retained records are bounded infrastructure telemetry. They are not
tamper-evident, append-only, legally compliant, immutable, non-repudiable, or
certified audit retention. Logical expiry and active purge do not prove that a
backend returned bytes to an allocator, volume, filesystem, or operating system.
Physical reclamation is a separate adapter-owned observation.

Production lifecycle writes remain disabled until the canonical C1 predecessor,
the same-profile C2-C6 packets, terminal validation, and two independent approvals
all pass. A missing packet, skip, zero-result command, stale timestamp, hash drift,
or approval gap is a stop condition. JSON-console and configured OTLP emission
remain active during lifecycle failure and rollback.

## Ownership and authority separation

| Role | Authority | Responsibility |
| :--- | :-------- | :------------- |
| Memories application owner | `access-telemetry-writer` | Send sanitized batches and heartbeats through Dapr; never inspect state or reach the backend. |
| Lifecycle owner | `access-telemetry-service` | Operate `memories-access-telemetry` and `AccessTelemetryLifecycleActor/global`; never use domain-store authority. |
| Time owner | `access-telemetry-clock` | Operate signed independent-UTC attestations; never read telemetry state. |
| Incident responder | `access-telemetry-inspector` | Read logically unexpired, sanitized operations data; never write, delete, extend retention, or rotate keys. |
| Adapter owner | `access-telemetry-adapter` | Operate the opaque backing component and collect capacity, durability, and physical-reclamation evidence. |
| Platform Operations reviewer | independent approval | Approve capacity, cost, operation, maintenance, backup/restore, reclamation, and RPO/RTO for the immutable profile hash. |
| Security reviewer | independent approval | Approve identity, TLS, secret scope, Dapr ACLs, privacy, images, and tenant-denial evidence for that same hash. |

The Platform Operations and security reviewer must be different named people.
Neither approval may be inferred from Administrator approval, a deployment, a
schedule, or the other review.

## Configuration and retention bounds

Production supplies the versioned Dapr configuration entry
`access-telemetry-lifecycle` explicitly. Validate the complete configuration before
accepting a write:

| Setting | Required value or bound |
| :------ | :---------------------- |
| Production retention | explicit; default target 24 hours |
| Minimum / maximum | 1 hour / 7 days |
| Queue | 8,192 records and 64 MiB per Server process |
| Batch | at most 256 records and 1 MiB encoded Dapr request |
| Retry age | at most 5 minutes from event emission and never beyond expiry |
| Shutdown flush | 5 seconds |
| Actor | `AccessTelemetryLifecycleActor/global` |
| Sweep | durable reminder every 5 minutes; at most 512 records per turn |
| Active-purge grace | oldest due age no greater than 15 minutes while healthy |
| Clock refresh / expiry | 10 seconds / 30 seconds |
| Clock uncertainty / delta | at most 250 ms uncertainty and 1 second absolute delta |
| Physical reclamation | adapter-declared; no later than 24 hours after active purge |

Missing, blank, malformed, zero, negative, infinite, below-minimum, or
above-maximum retention stops lifecycle persistence before dependency access. Do
not substitute a code default in Production. A retention reduction applies to new
records only unless an independently approved accelerated-purge operation exists.

## Rollout and enablement

1. Record the intended deployment ID, Dapr app/component identities, configuration
   epoch, marker-key generation, signed image digests, immutable profile hash, and
   workload hash. Never place connection strings, tokens, raw identifiers, queries,
   source URIs, or payloads in evidence.
2. Confirm JSON-console emission is active and record whether OTLP is configured.
3. Confirm the lifecycle and clock services can be deployed while every application
   provider and Production lifecycle write switch remains disabled.
4. Run the complete C1 predecessor producers. Each C1.1-C1.25 result must have its
   own artifact and reviewed source identity; an aggregate count is not evidence.
5. Obtain the independent C1 Platform Operations and security authorizations for
   the same `profile_sha256` and immutable C1 evidence hashes.
6. Keep the checked-in qualification lifecycle and clock deployments at zero. The
   host-side producer must first prove the disabled gate and empty namespace Lease,
   verify its namespace and shared `dapr-system` RBAC, acquire that Lease under the
   named approval, scale only the qualification workloads, and open the exact-profile
   gate for at most 45 minutes. Re-read running images, Dapr component and
   configuration, workload identity, profile hash, and key generation before the
   first write. Any mismatch is `configuration_invalid`, not a retry condition.
   Production stays disabled. An expired gate or stale Lease is rejected and
   explicitly restored to disabled/empty; it is never bypassed.
7. Run C2-C4 with reviewed scenario producers and archive the immutable per-run
   packets. Continue to C5/C6 and terminal validation only with zero failures and
   zero skips.

Use the repository verifier as the packet boundary. C2-C4 are selected from its
closed repository-owned producer registry; an operator cannot supply an executable
or raw argument vector. The allowlisted scenario document contains only
`schema_version: 1` and the target kind, Kubernetes context, qualification
namespace, and exact profile hash. Shared-system authority is derived from the
already-validated Platform Operations approval in C1, never from scenario input. It contains no
commands, counters, pass flags, timestamps, credentials, or arbitrary environment
values. For example:

```json
{"schema_version":1,"target":{"kind":"non-production-qualification","kube_context":"operator@qualification","namespace":"hexalith-memories-qualification","profile_sha256":"dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14"}}
```

Set `EVIDENCE_ROOT` to
an existing absolute directory outside the repository, then run:

```bash
python3 tools/verify-access-telemetry-lifecycle.py \
  --checkpoint adapter-profile \
  --repository-root "$PWD" \
  --kube-context "$KUBE_CONTEXT" \
  --namespace hexalith-memories-qualification \
  --deployment-id "$DEPLOYMENT_ID" \
  --profile-id postgresql-v2-dapr-1.18.1-postgresql-18.4-onprem-k8s1-openebs-local-retain-400g-v1 \
  --workload-profile adr-27.1-two-writer-500eps \
  --steady-state-minutes 30 \
  --purge-backlog-records 150000 \
  --declared-single-component-fault postgresql-pod-replacement \
  --evidence-root "$EVIDENCE_ROOT" \
  --evidence "$EVIDENCE_ROOT/adapter-profile-<run-id>.json" \
  --c0-wrapper "$EVIDENCE_ROOT/c0-<run-id>.json"
```

This is the source-bound C0 producer path. It runs only against the isolated
qualification namespace, authenticates its CLI and verifier bytes against a clean
`HEAD`, re-reads workload identity, and writes the adapter observation and C0
wrapper exclusively. An existing output, source drift, runtime/profile mismatch,
or identity change rejects the run. The same command without `--c0-wrapper`
retains the earlier Markdown diagnostic/rejection behavior and cannot satisfy C0.

Then run the first scenario checkpoint:

```bash
python3 tools/verify-access-telemetry-lifecycle.py \
  --checkpoint c2-production-replacement \
  --repository-root "$PWD" \
  --evidence-root "$EVIDENCE_ROOT" \
  --predecessor "$EVIDENCE_ROOT/c1.json" \
  --scenario-input "$EVIDENCE_ROOT/scenario-input.json" \
  --owner platform-operations \
  --evidence "$EVIDENCE_ROOT/c2-<run-id>.json"
```

Replace only the checkpoint and output filename for C3 and C4; the verifier selects
`tools/access_telemetry_c3_producer.py` or
`tools/access_telemetry_c4_producer.py` and verifies its Git blob before execution.
The producer controls its bounded, closed `kubectl` argument vectors and always
attempts the fixed disable operation after a failure. It never invokes a generic
`/operations/qualification/{checkpoint}/{command}` endpoint. The only Server
qualification surface is the no-input, gate-checked fixed workload endpoint, called
concurrently inside the two selected Server pods. Before enabling, its
`qualification-target-identity` command must observe the exact non-Production
namespace, approved profile hash, and disabled write state. Every passing packet
proves both that initial identity and the qualification lane's final disabled state.
Lease acquisition uses an atomic JSON Patch that tests the observed resource version
and empty holder before recording the C1 reviewer identity. C2 and C4 renew that
owned Lease and the qualification gate before session expiry.
Evidence paths are exclusive: choose a new run ID after every rejected or interrupted
run. Never edit, overwrite, or relabel a packet.

## C2 production replacement verification

Maintain two Server writers at the ADR total of exactly 250 accepted records/s while
the component sustains at least 500 operations/s during purge. Run the 30-minute
scenario only in the authorized non-Production qualification namespace while the
reviewed producer controls every action and observation. It must prove unique sanitized
record IDs, actor serialization, idempotent same-envelope retry, changed-envelope
conflict rejection, and exact Dapr transaction acknowledgement. Dependency
inspection must return an empty direct-backend set.

Replace, one at a time, both Server writers and their sidecars, the lifecycle and
clock services and their sidecars, the actor activation, all three Placement members,
and all three Scheduler members. The verifier's closed registry names every instance;
one aggregate replacement result cannot discharge multiple instances.
For each replacement record the controlled command, start/end UTC, immutable
stdout/stderr hashes, nonzero observation count, zero acknowledged-record loss,
recovery, and console continuity. When OTLP is configured, record its continuity as
well. Re-exercise the profile's declared adapter fault, then prove reconnect,
durable state/reminder reconstruction, and exact profile equality before resuming.

Stop immediately on a missing replacement, skipped command, count mismatch,
acknowledged loss, profile drift, state/reminder reconstruction gap, or direct
backend dependency. Production remains disabled throughout; the producer must also
restore the isolated qualification target to disabled before its packet can pass.

## C3 retention, purge, and reclamation verification

The C3 producer holds an exclusive lock on
`$EVIDENCE_ROOT/c3-retention-reclamation.journal.jsonl`. Each fsync'd JSONL entry
binds its sequence, predecessor hash, fixed command ID, aggregate result hash, and
UTC time. A concurrent producer or any rewritten, truncated, or reordered history
fails closed. This journal preserves attributable progress for the multi-day 1-hour,
24-hour, and 168-hour run; it is supporting recovery state, never passing evidence
by itself. During each horizon wait the qualification gate is closed and the host
runner polls the fixed PostgreSQL aggregate, so expiry is observed without requiring
an operator to resume inside the component TTL-cleanup interval.

Create separately identifiable 1-hour, 24-hour, and 168-hour cohorts plus later
records that must survive the older cohort's purge. Seed a final 125-record 24-hour
control immediately before observing the 168-hour cohort. Bind every cohort to database,
schema, table, tuple count, configuration epoch, actor epoch, profile/workload hash,
and executed command identifiers.

The reviewed producer must observe all of the following:

- event time retains UTC millisecond precision;
- a late accepted record receives only its remaining lifetime;
- an already expired record is rejected;
- stale, replayed, or wrong-identity attestations are rejected before mutation;
- the absolute local/reference delta never exceeds one second;
- Dapr TTL is set once as defense in depth and never extends on retry;
- interrupted purge resumes from durable state;
- transition and rollback cohorts complete without changing older expiry;
- `deleted + alreadyAbsent = candidates = indexRemovals` with a nonzero cohort;
- oldest-due age returns to at most 900 seconds;
- every newer control record remains present.

Logical proof ends after Dapr Delete, strong Get absence, and expiry-index removal.
The adapter then runs its separately authorized physical collector. Record the same
cohort/database/schema/table identity, the executed reclamation command, allocator
bytes before and after, and elapsed seconds from active purge. Pass only when the
cohort is attributable, reusable allocator free space increases, and age is at most 86,400 seconds.
The suspended qualification reporter Job is enabled only for this observation. It
uses the reviewed deployed lifecycle image and adapter Dapr identity, carries no
Kubernetes service-account token or RBAC, and submits only the fixed aggregate C3
document to the authenticated physical-evidence route. Before that Job is released,
the producer derives one deterministic artifact hash from the authenticated journal
context and the complete immutable C3 command/result prefix. If interruption occurs
after the Job completed but before the journal append, resume accepts only that
Job's identical logs and authenticated receipt for the same hash. Never unsuspend a
completed Job to rerun it, and never require namespace-wide Job create or delete
authority. Never record an operating-system disk shrink claim.

## C4 failure, privacy, and observability verification

Exercise every declared condition: Dapr outage, application/state/clock outage,
stale attestation, actor failover, reminder delay, queue record/byte exhaustion,
transaction, ETag and TTL failure, capacity pressure, bad configuration, bad key,
profile drift, reconnect, retry exhaustion, shutdown, and degraded rollback.

For every condition prove a nonzero business-request sample, zero business
failures, business readiness available, lifecycle health fail-closed, and continued
JSON-console/configured-OTLP audit emission. Authenticated business and privacy
probes read the short-lived qualification JWT only from the absolute, non-symlink,
owner-only file named by `HEXALITH_STORY_27_4_BUSINESS_BEARER_FILE`. The producer
streams that token only over stdin to the fixed in-pod request command. The token
never appears in argv, environment, logs, journals, or packets, and it authorizes
only tenant `story-27-4-qualification`. Missing, over-permissive, stale, malformed,
or leaked credentials fail before the qualification gate opens. `Unhealthy` takes
precedence over `Degraded`; `NoData` is valid only when the provider is enabled,
every lifecycle gate is otherwise healthy, and neither accepted nor rejected
activity has occurred for 15 minutes.

Prove all nine lifecycle states (`accepted`, `rejected`, `enqueued`, `persisted`,
`retried`, `failed`, `dropped`, `expired`, `purged`) and only bounded `state`,
`reason`, and `outcome` labels. Prove alert transitions, missing-series behavior,
and the true last-physical-evidence timestamp gauge. Scrape time is not evidence
time.

The privacy lane must show least-privilege inspection, no tenant-facing read route,
no raw or secret value in the queue/state/retry/metric/evidence chain, and all Story
20.2 and Story 24.3 tenant negatives. A denial passes only when dependency calls
after denial equal zero.

## Monitoring, alerts, and NoData

Import the lifecycle panels from
`deploy/grafana/dashboards/memories-operability.json`. Treat a missing lifecycle
series as unhealthy unless the provider is explicitly disabled and that disabled
state is displayed. Keep queue records separate from queue bytes and capacity
records/utilization; they have different saturation meanings.

Page the lifecycle owner for fail-closed health, profile drift, stale clock,
transaction/TTL failure, oldest queue age approaching 300 seconds, oldest due age
approaching 900 seconds, physical evidence age approaching 86,400 seconds, or
capacity at 80%. At 90% capacity lifecycle health is Unhealthy. Alert on a sustained
70% warning and require a new capacity/cost decision before increasing retention or
traffic.

Dashboards and alerts are operational hints, not checkpoint evidence. Archive the
reviewed commands and immutable results that explain each transition.

## Incident response and recovery

1. Declare a lifecycle-only incident and preserve business readiness. Confirm
   JSON-console/configured-OTLP continuity before changing anything.
2. Stop new lifecycle writes when identity, key, schema, capacity, time, component,
   transaction, TTL, or profile validity is uncertain. Do not purge while clock
   attestation is stale.
3. Capture the bounded health state/reason, queue records/bytes/oldest age,
   attestation age/delta/uncertainty, actor/reminder state, expiry backlog, capacity,
   and physical-evidence age. Exclude raw identifiers and backend credentials.
4. Repair through the owning authority. Plain unreachability may retry within the
   five-minute age; terminal `configuration_invalid` requires an explicit service
   restart after correction.
5. Revalidate identity and capability before mutation. Let the fixed actor resume
   purge or marker rotation idempotently from durable state.
6. Verify queue accounting, exact acknowledgement, newer-record preservation,
   oldest-due recovery, and audit continuity. Start a new immutable C2-C4 run when
   an incident invalidates an earlier observation.

Never infer recovery from a running container, ready endpoint, static manifest,
scheduled reminder, empty queue, or healthy dashboard alone.

## Rollback and RPO/RTO limits

Disable the independently switchable provider or roll back the Server image without
changing business responses. Keep the lifecycle service, clock, Dapr component,
actor state, secrets, and retained records deployed until normal expiry, active
purge, and physical reclamation complete. An old Server image is an explicit
degraded incident state, not an acceptable steady state.

The application contract claims zero acknowledged-record loss only for one Server
or lifecycle-service process/container/host loss and for the adapter's separately
proven declared fault. Use the adapter's published, potentially nonzero backup and
restore RPO/RTO for node, disk, control-plane, site, simultaneous, corruption, or
operator-deletion scenarios. Never widen the durability claim from a successful pod
replacement.

## Marker key rotation

1. Stage a new Dapr-secret generation without exposing its value and freeze the old
   generation for new registrations.
2. Capture the current dynamic writer membership. Require every live writer to load
   and acknowledge the staged key; a joining writer must start on the staged key.
3. Switch the active generation only after all live acknowledgements. Wait for each
   old-generation queue to report zero or for its five-minute maximum retry age to
   expire; record departed leases rather than guessing.
4. Record the final successful old-key write. Retain the verification key for at
   least 7 days, 15 minutes, and 1 second after that write.
5. Exercise process replacement and actor reactivation before declaring rotation
   complete. Unknown key IDs, missing acknowledgements, or stale writers fail
   closed.

## A41 close-out chain

A41 is open until remote publish verification succeeds. Build a terminal bundle
containing the exact same-profile C0-C6 packets, terminal validation, and both independent
approvals. Build an exact mutation manifest for the four verifier-approved A41
paths, including expected SHA-256 bytes and required/forbidden semantic fragments.

Run inventory and preflight from a clean open commit:

```bash
python3 tools/verify-access-telemetry-lifecycle.py --checkpoint a41-inventory \
  --repository-root "$PWD" --evidence-root "$EVIDENCE_ROOT" \
  --evidence "$EVIDENCE_ROOT/a41-inventory-<run-id>.json"

python3 tools/verify-access-telemetry-lifecycle.py --checkpoint close-out-preflight \
  --repository-root "$PWD" --evidence-root "$EVIDENCE_ROOT" \
  --bundle "$EVIDENCE_ROOT/terminal-bundle.json" \
  --mutation-manifest "$EVIDENCE_ROOT/a41-mutations.json" \
  --snapshot "$EVIDENCE_ROOT/a41-recovery-snapshot.json" \
  --remote origin --branch <approved-branch> \
  --evidence "$EVIDENCE_ROOT/a41-preflight.json"
```

Preflight writes a recoverable byte snapshot and binds the clean HEAD, branch,
terminal bundle, exhaustive inventory, mutation manifest, profile, and protected
historical paths. Apply only the approved edits, stage exactly those paths, and run
postflight:

```bash
python3 tools/verify-access-telemetry-lifecycle.py --checkpoint close-out-postflight \
  --repository-root "$PWD" --evidence-root "$EVIDENCE_ROOT" \
  --preflight "$EVIDENCE_ROOT/a41-preflight.json" \
  --mutation-manifest "$EVIDENCE_ROOT/a41-mutations.json" \
  --snapshot "$EVIDENCE_ROOT/a41-recovery-snapshot.json" \
  --evidence "$EVIDENCE_ROOT/a41-postflight.json"
```

Postflight rejects an unstaged byte, untracked path, index mismatch, missing semantic
transition, changed historical Epic 20/Story 20.5 byte, or preflight/hash drift. After
the exact commit is published, verify remote containment:

```bash
python3 tools/verify-access-telemetry-lifecycle.py --checkpoint publish-verification \
  --repository-root "$PWD" --evidence-root "$EVIDENCE_ROOT" \
  --preflight "$EVIDENCE_ROOT/a41-preflight.json" \
  --postflight "$EVIDENCE_ROOT/a41-postflight.json" \
  --mutation-manifest "$EVIDENCE_ROOT/a41-mutations.json" \
  --snapshot "$EVIDENCE_ROOT/a41-recovery-snapshot.json" \
  --commit <full-commit-id> --remote origin --branch <approved-branch> \
  --evidence "$EVIDENCE_ROOT/a41-publish.json"
```

A local commit is not published. Do not repair drift with a broad rewrite, edit
historical Epic 20/Story 20.5 completion, change `sprint-status.yaml`, or mark A41
resolved before the publish packet reports `published-close-out-verified`.

## Verified decommissioning

Decommission only after writers are disabled, the maximum retained cohort is
logically expired, active purge is complete, the adapter's physical-reclamation
proof passes, no old marker key remains required, backups and evidence are retained
under their owning policy, and an authorized operator approves the action. Remove
the lifecycle service and clock before removing their scoped Dapr components or
secrets. Verify that business service, JSON console, and configured OTLP remain
healthy. Storage deletion is a separate destructive action and is never implied by
application rollback or this runbook.
