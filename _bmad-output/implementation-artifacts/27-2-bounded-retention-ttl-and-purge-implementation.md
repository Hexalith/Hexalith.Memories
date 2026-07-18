---
baseline_commit: 4856b0ab5d927ad07d82e5bed9b61597a380269e
creation_sprint_status_sha256: 39b50b4c6a49553494bc3b2e7aeb58f76c0c84e421362984b84701d47821fad6
creation_scope_evidence: _bmad-output/implementation-artifacts/tests/27-2-create-story-scope-evidence.md
---

# Story 27.2: Bounded Retention/TTL and Purge Implementation

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want the ratified access-telemetry sink/store to enforce a bounded lifecycle,
so that emitted access records do not grow without limit.

## Acceptance Criteria

1. Given Story 27.1's ratified contract, when Server and deployment configuration are applied, then access events enter the selected write-only sink/store with the documented bounded duration and expiry/purge behavior, while existing required audit emission remains continuous.

2. Given valid, invalid, missing, minimum, and maximum lifecycle settings, when the host starts in Development and Production, then configuration validation follows the ratified fail-closed/degraded policy and never silently falls back to unbounded retention.

3. Given two Server writers, restart/rescheduling, backpressure, and temporary sink/store failure, when access events are emitted, then behavior matches the ratified delivery and recovery contract and low-cardinality health/metrics expose loss or degradation without secrets, raw content, or unbounded tenant labels.

4. Given two authorized tenant contexts and rejected/unknown scope, when records are written, expired, purged, and inspected through any supported operational seam, then tenant/privacy boundaries fail closed and focused cross-tenant negative tests name the affected storage, routing, and evidence surfaces.

## Tasks / Subtasks

- [x] Task 1 - Reconcile and ratify the source-to-persisted V1 mapping before runtime persistence work (AC: 1, 4)
  - [x] Re-run the Story 27.1 handoff preflight against all nine current `AccessTelemetryLog` operation families and their typed `AccessTelemetryEvent` state. Build a checked-in, structure-guarded mapping matrix covering every event ID, severity, operation, outcome, error code, timestamp, query field, and nullable field.
  - [x] Resolve the current contract gaps explicitly: map the logger outcome `partial`; represent authorized cross-case search where `caseId` is null; map syntactic/semantic/graph/natural-language/hybrid search modes and arbitrary current weight combinations into bounded persisted catalogs; and define error-code mapping without leaking exception or response content.
  - [x] Amend the not-yet-shipped persisted V1 section of `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` only as needed to make the mapping total, deterministic, bounded, and privacy-safe. Preserve the public logger contract and the accepted Dapr-only architecture.
  - [x] Obtain recorded ratification from Administrator and the architecture owner, then update `AccessTelemetryRetentionDecisionTests` with structure-aware guards for the resolved mapping. This is a hard checkpoint: do not implement or enable persistence, the provider, or deployment topology until the mapping is ratified and the guards pass.
  - [x] If reconciliation requires a second independently shippable product, multiple persisted schemas, or a materially different lifecycle family, stop and split/replan rather than widening Story 27.2.

- [x] Task 2 - Add the internal lifecycle contracts and bounded configuration model (AC: 1, 2, 4)
  - [x] Add a non-packable internal `Hexalith.Memories.AccessTelemetry.Contracts` project for Dapr invocation DTOs, the canonical persistence record, lifecycle/configuration epochs, writer heartbeats, signed clock attestations, inspection responses, and bounded enums. Keep the public `Hexalith.Memories.Contracts.V1.AccessTelemetryEvent` unchanged and separate.
  - [x] Implement strict options and startup validation for enabled state; component/app identities; retention, queue, batch, retry, clock, and purge bounds; configuration epoch; component-profile hash; alpha opt-in; attestation verification key; marker-key references/generations; capacity evidence ID; physical-reclamation evidence ID; and exact schema version. Production must receive retention through Dapr configuration; missing, malformed, zero, infinite, below-one-hour, or above-seven-day values stop lifecycle writes without stopping business readiness. Development/tests may default to 24 hours; shorter durations are test-composition-only.
  - [x] Encode the accepted defaults and limits once: 24-hour default, one-hour minimum, seven-day maximum; 1,024-byte canonical record; 256-record/one-MiB batch; 8,192-record/64-MiB per-Server queue; five-second shutdown flush; and retry never beyond five minutes from emission or absolute expiry.
  - [x] Canonicalize the internal record as deterministic RFC 8785 UTF-8 JSON with explicit nulls, reject unknown/duplicate/wrong-case/noncanonical fields, generate monotonic ULID record IDs, and calculate the canonical-envelope SHA-256 used by idempotency checks.
  - [x] Use central package management and existing repository pins. Do not add versions to project package references or upgrade the SDK, Aspire, Dapr, OpenTelemetry, ULID, or test packages as part of this story.

- [x] Task 3 - Add the Server lifecycle logger provider and bounded delivery worker (AC: 1, 2, 3, 4)
  - [x] Add the lifecycle provider under `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/`. Consume typed `AccessTelemetryEvent` logger state by value type; never parse stdout and never use record `ToString()` as a persistence contract.
  - [x] Give this provider an isolated Information-level filter for `AccessTelemetryCategory`. Preserve the existing nine success/error event IDs and severities, JSON console provider, optional OTLP provider, endpoint responses, rate limits, authentication/authorization order, metrics, and one-emission `EndpointTelemetryScope` behavior.
  - [x] Sanitize synchronously before enqueue. Convert tenant/user/case identifiers to keyed HMAC markers; only `tenantMarker` may use `__rejected__` for rejected/unknown tenant scope, while rejected user/case handling and authorized cross-case records follow C1's ratified total mapping. Reduce query text to a length bucket, subject to presence, and source URI to a bounded source kind. Prohibit raw tenant, user, case, query, subject, URI, payload, token, credential, secret, and exception content.
  - [x] Implement a truly byte-and-record-bounded in-memory queue. `ILogger.Log` must use nonblocking admission, drop the new record on overflow with bounded reason `queue_full`, never spill to disk, and never let a lifecycle exception escape into a business request.
  - [x] Implement the hosted worker with batches no larger than 256 records and one MiB, Dapr service invocation to `memories-access-telemetry`, full-jitter retry from 100 milliseconds through five seconds, a five-minute/emission-expiry cap, bounded outage recovery, and a five-second shutdown flush. Two Server writers must produce unique records without instance/process identifiers becoming telemetry labels.
  - [x] Treat correctly configured Dapr unavailability as bounded degraded operation (`remote_validation_pending`); continue business traffic and existing JSON/OTLP emission. Treat later capability/configuration/profile mismatch as terminal `configuration_invalid` until an explicit service restart. Do not add a fallback sink.

- [x] Task 4 - Implement the Dapr-addressed lifecycle service, fixed actor, logical expiry, and purge (AC: 1, 2, 3, 4)
  - [x] Add the non-packable Web service with Dapr app ID `memories-access-telemetry`, using `AddServiceDefaults(configureRedisInstrumentation: false)` and only Dapr service invocation, state, actors/reminders, configuration, and secrets. No backend SDK, connection string, backend endpoint, orchestrator API, Kubernetes identity, or Pod UID may enter application code.
  - [x] Implement `AccessTelemetryLifecycleActor/global` as the sole serialized mutation authority. Store configuration epoch, component-profile hash, writer registry, 64-shard minute expiry cursor, purge/capacity/reclamation/rotation state, and reminder progress in actor state.
  - [x] In one Dapr transaction, write each canonical record and its minute/shard expiry index with ETags/strong-read semantics. The same record ID, hash, and absolute expiry is idempotent; differing bytes or expiry returns `record_id_conflict` and makes lifecycle health unhealthy.
  - [x] Base absolute Unix-millisecond expiry on the source event timestamp. Retry must never extend age. Reject an already-expired event, an event over one second in the future, replay, stale clock evidence, or untrusted time. Pass `ttlInSeconds = ceil(remaining lifetime)` only as defense in depth; never treat component TTL metadata as proof of authoritative deletion.
  - [x] Register an idempotent durable reminder every five minutes. Walk 64 minute shards, process no more than 512 due records per actor turn within an observed 100-millisecond budget, back off 25-100 milliseconds between turns, preserve newer records, and catch up until healthy logical purge is within 15 minutes.
  - [x] Define portable logical deletion as Dapr Delete followed by a strong Get returning absent and removal from the expiry index. The sanitized inspector may report bounded lifecycle evidence but cannot write, delete, extend expiry, rotate keys, or expose raw records. Do not claim component-specific physical reclamation here.
  - [x] Implement staged marker-key rotation: writers heartbeat every 10 seconds with 30-second leases; stage, acknowledge, drain, and activate epochs; retain the old key through the final old-key write plus seven days, one-second accepted skew, and 15-minute purge grace.

- [x] Task 5 - Implement the independently signed clock-attestation service and validation gates (AC: 1, 2, 3)
  - [x] Add the non-packable Web service with Dapr app ID `memories-access-telemetry-clock`, independent signing authority, no lifecycle-store authority, and no dependency on application/container/host wall-clock agreement as trust evidence.
  - [x] Require at least three independent authenticated UTC sources. Produce a majority interval no wider than 250 milliseconds and a signed attestation bound to deployment ID, app ID, unique service-instance ID, a new process-epoch ULID for every process start, component/profile identity, nonce, issued/expiry timestamps, and signer/key epoch.
  - [x] Refresh every 10 seconds and expire after 30 seconds. Verify signature, nonce, replay cache, context, profile, freshness, majority, and an absolute delta no greater than one second before Server acceptance and every lifecycle mutation.
  - [x] Require a new attestation after reconnect, actor/service failover, component/profile/configuration/key epoch change, or process restart. Stale/untrusted time stops lifecycle operations and marks lifecycle health unhealthy without stopping business traffic or existing JSON/OTLP emission.

- [x] Task 6 - Add fail-closed component capability gates, bounded observability, and authority separation (AC: 1, 2, 3, 4)
  - [x] Before the first write and after material change, run behavioral probes for strong CRUD/ETag behavior, multi-key transactions/conflicts, actor state/reactivation/failover/reminders, effective TTL, 1,024-byte records, 256-record/one-MiB transactions, two-writer throughput while purge runs, declared durability/failure behavior, tenant isolation/encryption, physical capacity, and reclamation evidence hooks.
  - [x] Probe the exact configured component profile; do not infer capability from the Dapr API or component name. A component that ignores or cannot prove effective per-record TTL fails the gate even though logical expiry remains authoritative. A failed/missing/stale probe blocks lifecycle writes and reports lifecycle Unhealthy while business readiness remains available. Do not enable an unproven Production profile; alpha components require explicit Production opt-in and an exact version pin.
  - [x] Preserve separate authorities: Server writer can invoke write/heartbeat only; lifecycle service can access its actor and `access-telemetry-store`, `access-telemetry-secrets`, and `access-telemetry-config` only; clock can sign attestations only; inspector has sanitized operations-only read access; adapter/operations evidence stays outside application credentials.
  - [x] Add the counter `memories.access.telemetry.lifecycle.records` with bounded `state` values accepted/rejected/enqueued/persisted/retried/failed/dropped/expired/purged and a bounded reason catalog. Add bounded queue, Dapr, attestation, state, latency, capacity, expiry, purge, reminder, and physical-evidence measurements required by the ADR.
  - [x] Implement health precedence `Unhealthy` over `Degraded` over `NoData`/`Healthy`. `NoData` is valid only when enabled, every gate is healthy, and no accepted/rejected record has appeared for 15 minutes. Health details must state cause, impact, owner, and next action without raw content, secrets, record/backend IDs, or tenant/user/case/query/source/trace/instance/process labels.

- [x] Task 7 - Wire local and deployable topology without certifying a Production adapter (AC: 1, 2, 3, 4)
  - [x] Register the contracts, lifecycle, clock, and test projects in `Hexalith.Memories.slnx`; extend AppHost and `Hexalith.Memories.Aspire` with the fixed app/component identities, Dapr references, configuration/secrets, dependencies, and health relationships. Keep Server as two independent writers.
  - [x] Add development/test Dapr component definitions for `access-telemetry-store`, `access-telemetry-secrets`, and `access-telemetry-config`, plus workload-specific Dapr configuration/ACL scopes. Development defaults must remain bounded and test-only short TTLs must not leak into Production.
  - [x] Add least-privilege Kubernetes manifests for lifecycle and clock workloads, services, Dapr annotations/configuration, NetworkPolicy/service-account/RBAC relationships, probes, non-root/read-only-root posture, and explicit configuration injection. Preserve the Server's two replicas and ephemeral `/tmp`; do not introduce local disk buffering.
  - [x] Production overlays must require an explicit eligible component profile and retention configuration. They may carry portable templates/evidence hooks, but must remain disabled/fail closed until Story 27.3 selects, pins, and proves the exact Production adapter.
  - [x] Preserve rollback independence: disabling the Server provider stops new lifecycle writes but does not alter JSON console/OTLP, delete storage/secrets/actor state, or stop retained records from expiring. An old Server image is an observable degraded incident, not an accepted steady state.

- [x] Task 8 - Prove the portable runtime slice and focused tenant/privacy boundaries (AC: 1, 2, 3, 4)
  - [x] Add focused unit tests for the mapping guard, options, Development/Production validation, typed-state extraction, provider filter, sanitization, canonicalization/hash, record/byte queue bounds, retry/flush behavior, and logger exception containment.
  - [x] Add service/actor tests for transaction atomicity, strong reads/ETags, idempotent retry, conflicting duplicate, source-age expiry, late/future/replayed events, retention decrease, 64-shard purge batching, newer-record preservation, reminder idempotency/reactivation/rescheduling, marker-key rotation, and inspector non-mutation.
  - [x] Add clock tests for authenticated source quorum, interval width, signing, nonce/context/profile validation, replay, freshness, delta, refresh, restart/failover, and epoch/key changes.
  - [x] Add focused Dapr integration evidence for two Server writers, unique IDs, 250-events/second admitted ceiling, 500-events/second component/probe traffic while purge runs, 60-second temporary outage, five-minute drain, queue full, restart/rescheduling, transient transaction failure, reminder recovery, business-readiness isolation, and existing console/OTLP continuity.
  - [x] Add two-authorized-tenant plus rejected/unknown-scope negatives that name and exercise the Server writer route, lifecycle service invocation, actor/state/index keys, purge selection, clock route, inspector route, Dapr component scopes, and adapter-evidence interface. Prove no cross-tenant marker mix-up, raw-field storage, authority escalation, or unbounded metric label.
  - [x] Preserve and re-run `TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState`, `SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies`, `TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies`, `VerifyAsync_DetectsSyntacticTenantIdMismatch_ReturnsFailed`, `VerifyAsync_DetectsSemanticTenantIdMismatch_ReturnsFailed`, and `VerifyAsync_DetectsMissingSemanticTenantId_ReturnsFailed`.
  - [x] Keep physical-reclamation, exact Production adapter/failover evidence, dashboards/alerts, operations runbook, and A41 closure explicitly pending for Story 27.3. Portable logical deletion and adapter evidence hooks do not prove physical reclamation.

- [x] Task 9 - Reconcile verification, evidence, phase ledger, and story scope (AC: 1, 2, 3, 4)
  - [x] Run focused tests and a clean Release build with repository-pinned versions and build-server isolation. Re-run exact method discovery on fresh assemblies and record comparable pre/post scopes; never report the observational baseline below as canonical or planned tests as actual.
  - [x] If `Hexalith.EventStore.Client >= 1.72.3` is still unavailable, preserve the exact canonical blocker, owner, consequence, and reopen trigger. Do not migrate EventStore or change its pin under Story 27.2; that belongs to the Epic 28 dependency-abstraction/adoption lane.
  - [x] Run `git diff --check`, deployment/YAML validation, structure guards, capability-probe tests, focused privacy negatives, and the existing emission regression classes. Record exact commands and results in the Dev Agent Record.
  - [x] Update `docs/dev/telemetry.md` to describe only implemented portable lifecycle truth. Do not add a Production runbook or claims of tamper evidence, append-only integrity, legal compliance, certified retention, exact adapter eligibility, physical reclamation, or A41 closure.
  - [x] Reconcile every changed path into the cumulative File List and add a `dev-story` Change Log row with runner-derived actual/cumulative test counts. If a checkpoint becomes independently shippable or the File List/phase ledger cannot be reconciled, stop and correct course before moving beyond `in-progress`.

### Review Findings

- [ ] [Review][Patch] Register concrete exact-profile capability probes; the production probe set is empty and the lifecycle remains permanently fail-closed [`src/Hexalith.Memories.AccessTelemetry/Program.cs`:46]
- [ ] [Review][Patch] Renew capability evidence before `ValidUntilUtc`; the startup-only hosted probe lets writes continue on stale evidence [`src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryCapabilityProbeHostedService.cs`:18]
- [ ] [Review][Patch] Resolve Production retention through Dapr configuration before validating lifecycle options [`src/Hexalith.Memories.AccessTelemetry/Program.cs`:17]
- [ ] [Review][Patch] Reject undefined `RetentionSource` enum values even when Development retention is otherwise valid [`src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryOptionsValidator.cs`:34]
- [ ] [Review][Patch] Keep invalid lifecycle queue/options configuration from terminating Server business-host startup [`src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`:508]
- [ ] [Review][Patch] Grant the Server service account access to `access-telemetry-marker-key`; current RBAC only grants the lifecycle identity [`deploy/kubernetes/base/service-accounts-rbac.yaml`:37]
- [ ] [Review][Patch] Derive `acceptedAtUtc` and absolute expiry from actor-verified time and configured retention instead of trusting writer-supplied timestamps [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleProcessor.cs`:49]
- [ ] [Review][Patch] Require fresh trusted-clock validation for every lifecycle mutation, including each batch mutation, heartbeat, purge, inspection, and rotation transition [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleActor.cs`:52]
- [ ] [Review][Patch] Bind attestations to authenticated caller identity and expected service/process/key epochs, and preserve replay protection across Server calls [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryClockGate.cs`:40]
- [ ] [Review][Patch] Compute a true overlapping strict-majority clock interval while tolerating bounded source failure and rejecting duplicate source identities [`src/Hexalith.Memories.AccessTelemetry.Clock/ClockAttestationService.cs`:41]
- [ ] [Review][Patch] Load the clock signing authority through scoped Dapr secrets instead of a direct environment secret [`src/Hexalith.Memories.AccessTelemetry.Clock/Program.cs`:14]
- [ ] [Review][Patch] Align expiry-entry serialization and both static/AppHost component query indexes so `expiryMinute` queries can find due records [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs`:78]
- [ ] [Review][Patch] Do not apply record expiry TTL to the expiry-index entry before authoritative delete-and-verify purge runs [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs`:46]
- [ ] [Review][Patch] Enforce configured purge limits/intervals, persist shard/minute cursors, and schedule bounded follow-up actor turns instead of looping unbounded [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleActor.cs`:125]
- [ ] [Review][Patch] Update retained/purged accounting only for inserted records and verified deletions, not idempotent retries or failed absence checks [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleActor.cs`:88]
- [ ] [Review][Patch] Compare expiry/hash before deletion so a stale expiry entry cannot delete a newer record reusing the same ID [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs`:95]
- [ ] [Review][Patch] Carry exact strong-read ETags and the required constant partition through transactional record/index mutations [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs`:18]
- [ ] [Review][Patch] Validate and cap heartbeat identities, generations, counts, and lease duration to prevent unbounded durable writer state [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleActor.cs`:106]
- [ ] [Review][Patch] Connect staged marker-key rotation to Server generation-aware heartbeats, queue drain counts, acknowledgements, and activation calls [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryHeartbeatWorker.cs`:27]
- [ ] [Review][Patch] Convert malformed canonical batches into bounded schema rejections instead of letting prevalidation exceptions escape actor invocation [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleActor.cs`:78]
- [ ] [Review][Patch] Stop all subsequent writes after `record_id_conflict` establishes terminal lifecycle state [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleActor.cs`:86]
- [ ] [Review][Patch] Reconcile persisted actor configuration after an explicit validated restart so epoch/profile changes can recover [`src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleActor.cs`:248]
- [ ] [Review][Patch] Enforce the exact operation/event/outcome tuples, bounded query catalogs, error catalogs, correlation pairing, and operation-specific nullability in canonical validation [`src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryCanonicalizer.cs`:227]
- [ ] [Review][Patch] Reject unknown subtypes and wrong-typed logger state instead of normalizing them into legitimate actions or `not-applicable` values [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetrySanitizer.cs`:212]
- [ ] [Review][Patch] Preserve blank-tenant rejected-scope case evidence with the sentinel mapping instead of discarding it during case validation [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetrySanitizer.cs`:98]
- [ ] [Review][Patch] Make queue admission truly nonblocking under contention; the monitor lock can currently stall business logging [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/BoundedAccessTelemetryQueue.cs`:53]
- [ ] [Review][Patch] Deserialize bounded terminal HTTP responses before success enforcement and stop retrying terminal configuration/conflict outcomes [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/DaprAccessTelemetryDeliveryClient.cs`:36]
- [ ] [Review][Patch] Prevent non-prefix expiry and partial acceptance from permanently wedging FIFO acknowledgement [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryWorker.cs`:44]
- [ ] [Review][Patch] Honor configured batch byte/record limits and retry timing instead of hard-coded global maxima [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryWorker.cs`:36]
- [ ] [Review][Patch] Treat HTTP timeouts as retryable and the bounded shutdown-flush deadline as normal completion instead of terminating the hosted service [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryWorker.cs`:56]
- [ ] [Review][Patch] Count persisted/dropped records rather than successful batches in lifecycle metrics [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryWorker.cs`:59]
- [ ] [Review][Patch] Feed delivery, clock, conflict, purge, and no-data state into runtime health and preserve the actual inspector failure reason [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleStatus.cs`:16]
- [ ] [Review][Patch] Replace C2-C6 in-memory/direct-call/source-text checkpoints with hosted and routed Dapr/actor/clock/state evidence, including sanitizer branches, the HTTP UTC source, and hosted-worker retry/shutdown behavior [`tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs`:28]
- [ ] [Review][Patch] Enable the local AppHost lifecycle/clock resources so the portable topology can actually be exercised [`src/Hexalith.Memories.AppHost/Program.cs`:253]
- [ ] [Review][Patch] Revalidate retention/profile/epoch/key material after bootstrap changes instead of publishing a permanently static sanitizer/runtime decision [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleBootstrapService.cs`:29]
- [ ] [Review][Patch] Repair the phase ledger: restore chronological rows, add exact discovery/File List commands and complete blocker provenance, then record fresh reconciled Server and AccessTelemetry discovery evidence [`_bmad-output/implementation-artifacts/27-2-bounded-retention-ttl-and-purge-implementation.md`:523]

### Implementation Checkpoints

These checkpoints are facets of one bounded-runtime outcome. A later checkpoint cannot bypass an earlier fail-closed gate.

| Checkpoint | Accountable owner | Required evidence artifact and command | Review state | Completion state |
| :--------- | :---------------- | :------------------------------------- | :----------- | :--------------- |
| C1 - Total source-to-persisted mapping | Administrator + Hexalith.Memories maintainers | Ratified mapping in `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` plus `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs`; run `dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests` | reviewed 2026-07-18 | complete; 9/9 green, runtime gate open |
| C2 - Server admission/delivery | Server implementation owner | `tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryCheckpointTests.cs`; run `dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle.AccessTelemetryDeliveryCheckpointTests` | reviewed 2026-07-18 | complete; 9/9 green |
| C3 - Lifecycle actor and purge | Lifecycle implementation owner | `tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/LifecycleActorCheckpointTests.cs`; run `dotnet exec tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Release/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll -class Hexalith.Memories.AccessTelemetry.Tests.Lifecycle.LifecycleActorCheckpointTests` | reviewed 2026-07-18 | complete; 10/10 green |
| C4 - Trusted clock | Clock implementation owner | `tests/Hexalith.Memories.AccessTelemetry.Tests/Clock/ClockAttestationCheckpointTests.cs`; run `dotnet exec tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Release/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll -class Hexalith.Memories.AccessTelemetry.Tests.Clock.ClockAttestationCheckpointTests` | reviewed 2026-07-18 | complete; 9/9 green and threaded into writer/actor gates |
| C5 - Capability/security/observability gate | Platform implementation owner | `tests/Hexalith.Memories.AccessTelemetry.Tests/Capability/CapabilityAndObservabilityCheckpointTests.cs`; run `dotnet exec tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Release/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll -class Hexalith.Memories.AccessTelemetry.Tests.Capability.CapabilityAndObservabilityCheckpointTests` | reviewed 2026-07-18 | complete; 19/19 green and exact-profile failures remain fail-closed |
| C6 - Portable composition and focused proof | Story 27.2 owner | `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs` plus reconciled story ledger/File List; run `dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests` | reviewed 2026-07-18 | complete; 8/8 green and Production remains disabled pending Story 27.3 |
| Story 27.3 handoff | Operations + adapter owner | Exact Production adapter, physical-reclamation/failover evidence, runbook, and A41 coordination in Story 27.3 artifacts | out of Story 27.2 | not started; must remain open |

## Dev Notes

### Scope and Authority

- The canonical authority is the accepted ADR at `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md`, especially **Story 27.2 Implementation Handoff**. `epics.md` supplies the four acceptance criteria and sequencing; the ADR supplies the ratified implementation contract. [Source: _bmad-output/planning-artifacts/epics.md#Story-27.2-Bounded-Retention-TTL-and-Purge-Implementation; docs/dev/adr-27.1-001-access-telemetry-lifecycle.md#Story-27.2-Implementation-Handoff]
- The implementation is one Dapr-addressed vertical runtime path: Server admission and delivery, lifecycle service/fixed actor, trusted clock, capability gates, bounded expiry/purge, privacy-safe inspection, observability, and portable composition. Checkpoints above make this breadth reviewable without treating every facet as a separately completed product.
- Story 27.3 owns the exact Production component selection/version and alpha opt-in, production-shaped durability/failover/cost/capacity proof, component-specific physical reclamation, dashboards/alerts, runbook, and coordinated A41 closure. Story 27.2 must not certify or close those outcomes. [Source: docs/dev/adr-27.1-001-access-telemetry-lifecycle.md#Story-27.3-Verification-and-Operations-Handoff]
- Story 20.5 remains historical `done`; existing access emission and rate limiting are preserved. Epic 28/EventStore dependency abstraction is a different lane. No domain event or access-telemetry record may be added to Hexalith.EventStore.
- Assurance boundary: bounded infrastructure telemetry only; no tamper evidence, append-only integrity, legal compliance, or certified audit retention.

### Contract Reconciliation Gate

The accepted architecture is ratified, but its internal persisted schema is not yet shipped. Current source exposes three unresolved mappings that must be closed before persistence code can be correct:

1. `AccessTelemetryEvent.Outcome` currently allows `ok`, `partial`, and `error`; the accepted persisted catalog names only `ok` and `error`.
2. Authorized all-case/cross-case search can carry `caseId = null`; the accepted schema otherwise requires a non-null case marker for search.
3. Current search telemetry includes syntactic, semantic, graph, natural-language, and hybrid axes plus weight values; the accepted persisted catalog describes a narrower set.

Task 1 is therefore a hard, reviewable prerequisite rather than permission to invent a silent mapping. The resolution must remain bounded and deterministic, must preserve current endpoint/logger behavior, and must be signed off by the decision owner. This does not reopen the selected Dapr family or Story 27.1 as a broad decision story.

### Ratified Runtime Contract

- Stable identities are `memories-access-telemetry`, `AccessTelemetryLifecycleActor/global`, `memories-access-telemetry-clock`, `access-telemetry-store`, `access-telemetry-secrets`, and `access-telemetry-config`.
- Memories application code has a Dapr-only boundary. State-store capability varies by component: TTL metadata may be ignored, and actor stores require the documented transaction/ETag behavior. Behavioral probes, not component-name inference, are mandatory.
- Persisted means the record and its minute/64-shard expiry index committed in one Dapr transaction. The actor serializes mutations. Retry identity is record ID + canonical hash + absolute expiry.
- Retention begins at source emission, not acceptance. The exact expiry timestamp is authoritative; Dapr TTL is defense in depth. Healthy active logical purge completes within 15 minutes. Physical reclamation within 24 hours is adapter evidence for Story 27.3.
- Current admitted cluster ceiling is 250 events/second across nine operations. Evidence envelope: one hour = 900,000 records/about 0.86 GiB; 24 hours = 21.6 million/about 20.60 GiB; seven days = 151.2 million/about 144.20 GiB. Treat these as gates, not a capacity promise for an unselected Production adapter.
- Capacity thresholds are steady state at or below 70%, reclamation at or below 80%, critical at 80%, and unhealthy at 90%. The probe must sustain at least 500 events/second while purge runs.
- Each Server queue is bounded simultaneously by 8,192 records and 64 MiB. At the declared two-writer ceiling this covers about 65.5 seconds per Server at 125 events/second. A 60-second outage and five-minute drain are accepted targets; overflow drops the new lifecycle copy and remains observable.
- Writer, lifecycle actor, clock, inspector, and adapter/operations authorities stay separate. There is no tenant-facing lifecycle read API.

### Current Code and Preservation Rules

- `AccessTelemetryLog` is the current source of nine success IDs `7501-7509` and nine error IDs `7511-7519`; `EndpointTelemetryScope` emits once, uses `partial` for successful partial work, and protects endpoint behavior from metric failure. Preserve these contracts. [Source: src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLog.cs; src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryScope.cs]
- `AccessTelemetryEvent` is the frozen public JSON-console/OTLP typed state, not the persistence schema. Do not repurpose or destructively edit it. [Source: src/Hexalith.Memories.Contracts/V1/AccessTelemetryEvent.cs]
- Follow `CapturingAuditLoggerProvider`: inspect the typed state value rather than stdout. The console's outer JSON representation is valid emission evidence but is not a lossless persistence input.
- Search and memory-unit lookup currently produce raw query/subject/source URI values. The lifecycle provider must sanitize these synchronously before its queue. Do not move durable-store concerns into endpoints or domain persistence. [Source: src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs; src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs]
- `ServiceDefaults` always enables UTC JSON console logging and conditionally enables OTLP. New lifecycle/clock services do not use the keyed Redis instrumentation path and should call `AddServiceDefaults(configureRedisInstrumentation: false)`. [Source: src/Hexalith.Memories.ServiceDefaults/Extensions.cs]
- Register Server behavior in `MemoriesServerServiceCollectionExtensions`; keep `Program.cs` thin. Lifecycle failure has its own health surface and must not make the existing business readiness endpoint unavailable.
- Production Server is currently two replicas, non-root, read-only root filesystem, with only ephemeral `/tmp`. Preserve that topology and do not add disk spill. [Source: deploy/kubernetes/base/server-deployment.yaml]

### Privacy and Tenant Isolation

- Sanitize before queue admission and canonicalization. Persist only the ratified bounded V1 fields/catalogs. Optional null fields remain explicit; unknown/duplicate/wrong-case fields are rejected.
- HMAC markers are deployment-secret keyed and rotation-aware. Raw identifiers and content must not reach the lifecycle queue, state store, index, lifecycle-path logs, health details, inspection result, or metrics. Existing console/optional-OTLP emission is preserved independently and is not a lifecycle storage input.
- Rejected/unknown tenant scope uses `tenantMarker = __rejected__`; no user/case marker may use that sentinel. C1 must ratify their bounded null/rejection handling. Authorized tenants remain distinguishable only through opaque markers; metrics never carry marker values.
- Tenant deletion does not grant early lifecycle deletion and must not create a new tenant-facing read/delete API; normal bounded expiry remains authoritative.
- Focused negatives must name storage, routing, purge, inspection, clock, and evidence surfaces. An authorization test that only stops before the new sink is necessary but insufficient for AC4.
- Operational health follows the existing UX principles of visible state, cause, impact, next action, owner, and recoverability. No FrontComposer/Web/Fluent UI route or UX artifact is part of this story.

### Project Structure Notes

Likely **new** paths; the developer may refine leaf filenames while preserving one type per file and the named boundaries:

- `src/Hexalith.Memories.AccessTelemetry.Contracts/` - internal non-packable invocation, persistence, clock, probe, and inspection contracts.
- `src/Hexalith.Memories.AccessTelemetry/` - lifecycle Web service with `Actors/`, `Configuration/`, `Persistence/`, `Purge/`, `Inspection/`, `Security/`, `Health/`, and `Telemetry/`.
- `src/Hexalith.Memories.AccessTelemetry.Clock/` - clock Web service with `Attestation/`, `Sources/`, `Signing/`, and `Health/`.
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/` - Server options, validator, provider/logger, sanitizer, canonicalizer/hash, bounded queue, worker, Dapr writer, health check, and bounded catalogs.
- `deploy/dapr/components/access-telemetry-store.yaml`, `access-telemetry-secrets.yaml`, and `access-telemetry-config.yaml` for local/test composition.
- `deploy/kubernetes/base/dapr/` workload-scoped component/configuration resources plus lifecycle/clock Deployment, Service, and NetworkPolicy resources under `deploy/kubernetes/base/`.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/`, Server lifecycle-provider tests, and focused integration tests under `tests/Hexalith.Memories.IntegrationTests/Telemetry/`.

Likely **updated** paths:

- `Hexalith.Memories.slnx`.
- `src/Hexalith.Memories.AppHost/Program.cs` and its project file.
- `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs` and `HexalithMemoriesSearchIndexServerResources.cs` or a new narrowly named resource descriptor beside them.
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`, the Server project file, and bounded lifecycle configuration in `appsettings.json`/`appsettings.Development.json`. Production must not gain a silent retention default.
- `deploy/kubernetes/base/server-deployment.yaml`, `service-accounts-rbac.yaml`, Dapr configuration, `kustomization.yaml`, and the Production overlay.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs`, Server/Integration test project files, the canonical ADR mapping section, and `docs/dev/telemetry.md`.

Do not edit `tools/release-packages.json`; new lifecycle projects are non-packable services/contracts. Prefer SDK container support and existing repository deployment patterns rather than adding Dockerfiles without a demonstrated need. Keep `.slnx`, central package versions, one type/object per C# file, file-scoped namespaces, Allman style, nullable/warnings-as-errors, XML documentation on public/protected/internal members, source-generated logging, `_camelCase` fields, and `CancellationToken` last.

### Testing Baseline and Planned Delta

A fresh comparable Server.Tests Release baseline was attempted on 2026-07-17 at commit `4856b0ab5d927ad07d82e5bed9b61597a380269e`:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

Restore failed with `NU1603`: `Hexalith.EventStore.Client >= 1.72.3` was unavailable and NuGet selected incompatible `2.0.0` for Server, tests, and Migrate.

- **Server.Tests canonical discovery status:** blocked. No current-source Release assembly or canonical baseline count exists for Story 27.2.
- **Blocker owner:** Epic 28 / EventStore dependency-package-feed and runtime-adoption lane.
- **Consequence:** do not subtract a later count from the observational inventory below or report it as a canonical delta; Story 27.2 must not fix/migrate the EventStore dependency.
- **Measurable reopen trigger:** when exact `Hexalith.EventStore.Client 1.72.3` is available again, or its owner approves and lands a compatible migration/pin change, rerun the exact build above and fresh xUnit method discovery against its output.

Observational inventory only: the existing Server Release DLL is dated 2026-07-17 11:57, is 2,912,768 bytes, and has SHA-256 `3e921113073db52ef5c2f0350b6a033fdc72e807f23841345ff653b4d7109f15`. Its xUnit method discovery produced Server 2,151, Architecture 24, Telemetry 141, Hosting 14, HealthChecks 61, Authentication 57, Endpoints 101, and Consistency 20 methods. Named affected classes observed: decision guards 6, schema 3, access log 9, endpoint scope 17, OpenTelemetry registration 13, endpoint authorization 14, and tenant-isolation verifier 16. The discovery output SHA-256 was `bcfe06a1d7c138e3ae02cf4b26f45339e472767d5ad051f7294ebdd5b83fe4d6`. It has no verified build-commit provenance and is not a canonical baseline.

Reproduce the observational inventory without a persistent temporary file:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods \
  | awk '/^Hexalith\.Memories\.Server\.Tests\./ { total++; if ($0 ~ /\.Architecture\./) architecture++; if ($0 ~ /\.Telemetry\./) telemetry++; if ($0 ~ /\.Hosting\./) hosting++; if ($0 ~ /\.HealthChecks\./) health++; if ($0 ~ /\.Authentication\./) auth++; if ($0 ~ /\.Endpoints\./) endpoints++; if ($0 ~ /\.Consistency\./) consistency++ } END { print total, architecture, telemetry, hosting, health, auth, endpoints, consistency }'
```

A separate fresh IntegrationTests Release baseline was attempted with:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

It also stopped at restore with four `NU1603` errors: `Hexalith.EventStore.Client 1.72.3` and `Hexalith.EventStore.Aspire 1.72.3` were unavailable and incompatible `2.0.0` packages resolved through Server, AppHost, and IntegrationTests.

- **IntegrationTests canonical discovery status:** blocked. No fresh current-source Release assembly or canonical method total exists.
- **Blocker owner:** Epic 28 / EventStore dependency-package-feed and runtime-adoption lane.
- **Consequence:** the existing IntegrationTests artifact below is inventory only and cannot be the before-side of a Story 27.2 delta.
- **Measurable reopen trigger:** after exact `Hexalith.EventStore.Client 1.72.3` and `Hexalith.EventStore.Aspire 1.72.3` availability, or an owner-approved compatible migration/pin change, rerun the exact IntegrationTests build and discovery commands.

Observational IntegrationTests inventory only: the existing Release DLL is dated 2026-07-16 16:20, is 1,166,336 bytes, and has SHA-256 `0f7e161484c7984f5f77da1bc755ef693048feb4ccd9474ee0ae77e90ed2a2c1`. It exposes 270 IntegrationTests methods, including 9 under `Telemetry`; its discovery output SHA-256 is `1fb4761bc1426af5997d2037915897251be560f6c02d9f9b94ceda1efa39d8a1`. It predates current HEAD and is not a comparable canonical baseline.

Reproduce that observational count with:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods \
  | awk '/^Hexalith\.Memories\.IntegrationTests\./ { total++; if ($0 ~ /\.Telemetry\./) telemetry++ } END { print total, telemetry }'
```

Planned Story 27.2 delta: **+104 to +144 xUnit test methods**, allocated by affected discovery unit as follows:

- Server provider/options/sanitizer/queue/worker/health: +36 to +48.
- New `Hexalith.Memories.AccessTelemetry.Tests` unit, a named `0 -> N` transition: lifecycle service/actor/schema/purge/rotation/probe +44 to +60, plus clock attestation/quorum/signature/replay +16 to +24, for +60 to +84 total.
- Existing `Hexalith.Memories.IntegrationTests` unit: Aspire/Dapr integration and named privacy/tenant negatives +8 to +12.

The new lifecycle-service test project is absent at create-story, so its planned lane is an explicit `0 -> N` discovery-scope transition, not evidence that existing behavior has zero coverage. Its first exact discovery command will be `dotnet exec tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Release/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll -list methods`. During development, record fully qualified pre/post method sets for comparable existing projects and that exact first discovery for the new project. Actual create-story delta is +0 in every lane.

### Latest Technical Information

Research snapshot: 2026-07-17. Repository pins control implementation.

- Dapr state TTL uses `ttlInSeconds`, but unsupported stores can ignore TTL metadata; authoritative expiry and deletion must therefore be implemented and behaviorally proved. [Source: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-store-ttl/]
- State transactions are component-dependent, while actor state stores require transactions and ETags/strong consistency. [Source: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/; https://docs.dapr.io/developing-applications/building-blocks/actors/actors-features-concepts/]
- Durable reminders survive actor deactivation; timers do not provide the same durable scheduling semantics. Use the actor reminder for purge and test reactivation/rescheduling. [Source: https://docs.dapr.io/developing-applications/building-blocks/actors/actors-timers-reminders/]
- Dapr service invocation supplies the portable app-to-app boundary; secret scopes can restrict component/key visibility. [Source: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/; https://docs.dapr.io/developing-applications/building-blocks/secrets/]
- Dapr's default request-size limit is larger than this ADR's one-MiB batch limit. The story limit is deliberately stricter and must be enforced before invocation. [Source: https://docs.dapr.io/operations/configuration/increase-request-size/]
- A custom `ILoggerProvider` can consume structured state and receive its own filter. The bounded Channel admission API supports nonblocking `TryWrite`; enforce the separate byte limit rather than relying on record count alone. [Source: https://learn.microsoft.com/dotnet/core/extensions/logging/custom-provider; https://learn.microsoft.com/dotnet/core/extensions/channels]
- Current repository pins: .NET SDK 10.0.302, `net10.0`, C# 14, Aspire AppHost SDK 13.4.6, CommunityToolkit Aspire Dapr 13.4.1-beta.686, Dapr 1.18.4, OpenTelemetry core/protocol/hosting 1.17.0, xUnit v3 3.2.2, Shouldly 4.3.0, NSubstitute 5.3.0 local override, and ByteAether.Ulid 1.3.8. Historical planning-document pins are stale.

### Historical Context Classification

| Reference | Classification | Permitted influence on Story 27.2 |
| :-------- | :------------- | :-------------------------------- |
| Story 27.1 whole-story execution shape | `historical-reference-only` | Decision provenance and accepted handoff only; do not copy its long decision/review task shape. |
| Accepted ADR 27.1 and Story 27.2 handoff | `current-narrow-pattern` | Current implementation authority; Task 1 may narrowly repair unshipped persisted mappings with ratification. |
| Story 27.1's superseded Redis/Kubernetes design iterations | `anti-template` | Do not restore a backend SDK, Redis contract, Kubernetes clock authority, Pod identity, or backend-specific application code. |
| Story 7.5 | `anti-template` | Preserve current FR67 emission/privacy intent only; do not copy its broad observability bundle. |
| Story 8.4 | `historical-reference-only` | Audit/trace helper context only; stdout one-resource evidence is not TTL/purge proof. |
| Story 8.5 | `anti-template` | Do not copy its bundled operational breadth. |
| Story 20.2 | `historical-reference-only` | Preserve the three named denial-before-dependency negatives and principal-derived identity. |
| Story 20.5 | `anti-template` | Preserve emission/rate limiting but do not reopen or copy the completed broad bundle. |
| Story 21.1 | `current-narrow-pattern` | Structure-aware decision-guard mechanics only if still applicable after Task 1. |
| Story 24.3 | `historical-reference-only` | Preserve the three named verifier/tenant-marker negatives; do not copy its decision-plus-implementation shape. |
| Story 24.4 | `current-narrow-pattern` | Reverified low-cardinality metric/tag policy may shape lifecycle signals. |
| Stories 26.1 and 26.5 | `anti-template` | Current manifest facts only; do not absorb their broad infrastructure/checkpoint shapes. |
| Story 26.6 | `current-narrow-pattern` | Reverified rollback and observable restoration mechanics only. |
| Story 26.8 | `historical-reference-only` | Epic sequencing context only; numeric adjacency is irrelevant. |
| Retention visibility proposal | `historical-reference-only` | Retained closure/visibility guard only; its no-Epic-27 scheduling clause was superseded. |

### Slice Proof

Story 27.2 has one independently demonstrable outcome: the ratified Dapr-addressed access-telemetry lifecycle operates end to end as a bounded, privacy-safe, failure-observable portable runtime path while existing JSON console/optional OTLP emission and business readiness remain intact.

The mapping, Server provider, queue/worker, lifecycle actor, trusted clock, component probe, authority separation, observability, topology, and focused tests are mutually dependent facets of that path. The checkpoint table gives each facet an owner, evidence, review, and completion state. None is independently complete or deployable without C1-C6, and no checkpoint may claim the overall outcome early.

The split boundary remains explicit:

- Story 27.1 supplied the accepted architecture and remains done; Task 1 only resolves unshipped persisted-field mappings required to implement it safely.
- Story 27.2 supplies portable code, bounded lifecycle behavior, logical deletion, capability/evidence contracts, composition, and focused proof.
- Story 27.3 selects and certifies the exact Production adapter, supplies production-shaped durability/failover/physical-reclamation evidence and runbook, and coordinates A41 close-out.
- Epic 28 owns the EventStore dependency-abstraction/adoption lane; it is not a prerequisite Story 27.2 may silently implement.

If development discovers multiple adapter implementations, a tenant-facing inspection product, a general trusted-time platform, a reusable persistence product with its own release outcome, or any separately demonstrable feature not required to make this one lifecycle path work, stop and split/correct course.

### Git Intelligence

- Baseline commit `4856b0ab5d927ad07d82e5bed9b61597a380269e` completed Story 27.1's Dapr-only decision documents and guards; it did not add lifecycle runtime code.
- Recent dependency/submodule commits changed shared pins and referenced implementations. Re-read current central versions and submodule guidance rather than copying historical version text.
- The worktree had one unrelated untracked planning proposal at create time: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17-infrastructure-dependency-abstraction.md`. It is user-owned and excluded from Story 27.2.
- No runtime lifecycle provider, lifecycle service, fixed actor, trusted clock service, or dedicated lifecycle Dapr component currently exists. Existing telemetry source files are preservation anchors, not partial Story 27.2 implementation.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic-27-Bounded-Access-Telemetry-Retention-Closure]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-27.2-Bounded-Retention-TTL-and-Purge-Implementation]
- [Source: docs/dev/adr-27.1-001-access-telemetry-lifecycle.md]
- [Source: _bmad-output/planning-artifacts/architecture.md#Security-Architecture]
- [Source: _bmad-output/planning-artifacts/prd.md#Observability]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-access-telemetry-retention-implementation.md]
- [Source: docs/dev/telemetry.md]
- [Source: project-context.md]
- [Source: _bmad/custom/story-scope-guard.md]
- [Source: _bmad/custom/story-phase-ledger.md]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-18: C1 preflight reconciled all nine `AccessTelemetryLog` families and all 14 frozen `AccessTelemetryEvent` properties against current emitters. Administrator ratified both named decision-owner rows, the canonical V1 clauses were reconciled, and three new structure guards moved through red to green; the focused class passes 9/9 and the runtime persistence gate is open.
- 2026-07-18: Comparable pre-development evidence at `6d7fd8aaa0a2fc58de741e31f38544fc15a10c08`: fresh isolated Release builds passed with 0 warnings/errors. Exact xUnit method discovery found Server.Tests 2,157 (sorted SHA-256 `98440744599bf454ee991a3cad39f69dee9185f6d1fe86106223638c3a194ae2`) and IntegrationTests 270 (sorted SHA-256 `038e0a140092d0a7910c17566a5b87a05bdc417699f67c4335516777168a30c4`); AccessTelemetry.Tests was absent (0).
- 2026-07-18: Fresh post-development discovery found Server.Tests 2,169 (+12, 0 removed; SHA-256 `e6cc2ce48900f19ed62665b42056aa9ca0837a838720abbb577e0a366963c27c`), IntegrationTests 278 (+8, 0 removed; SHA-256 `92f4cfeb683a0bb2cd90305e92f61f00eb53542e2aac231f3c61b7a9dcf96d55`), and the new AccessTelemetry.Tests lane 31 methods (0 -> 31; SHA-256 `4eea34c669021dc01446ddf0d44111c6aa38e482e92736ea155dcf84f5498460`).
- 2026-07-18: Exact checkpoint commands passed C1 9/9, C2 9/9, C3 10/10, C4 9/9, C5 19/19, and C6 8/8. Full AccessTelemetry.Tests passed 48/48. Existing access-emission classes plus the six named tenant negatives passed 69/69.
- 2026-07-18: `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore --nologo --verbosity:minimal` passed with 0 warnings/errors; `git diff --check` passed; `kubectl kustomize deploy/kubernetes/base` and `kubectl kustomize deploy/kubernetes/overlays/production` passed; PyYAML parsed the five standalone Dapr manifests. Offline `kubectl apply --dry-run=client --validate=false` could not perform API discovery because no Kubernetes API was available at `localhost:8080`.
- 2026-07-18: The earlier EventStore package-feed blocker is cleared for the current repository-pinned graph: isolated baseline builds and the final solution build restore/compile successfully without a Story 27.2 package or pin change. Epic 28 retains ownership of future EventStore dependency migration.
- 2026-07-18: External working-tree changes in Story 27.3 and `references/Hexalith.EventStore`, `references/Hexalith.FrontComposer`, and `references/Hexalith.Tenants` are unrelated, preserved, and excluded from Story 27.2 scope.

### Completion Notes List

- 2026-07-17: Created implementation-ready Story 27.2 from the accepted Dapr-only ADR, current source/deployment state, whole planning artifacts, official Dapr/.NET guidance, git history, and the repository's historical-slice/phase-ledger policies.
- 2026-07-18: Completed C1 with a ratified, total, bounded mapping for all logger families, partial outcome, nullable case/result states, query transformations, and error catalogs; no second product or schema was required.
- 2026-07-18: Completed Task 2 with an isolated non-packable contracts project, strict fail-closed lifecycle options, bounded DTO/enums, canonical explicit-null JSON and envelope hashing, strict canonical parsing, and process-monotonic ULIDs; the new contracts checkpoint passes 10/10.
- 2026-07-18: Completed C2 with typed logger-state extraction, total synchronous sanitization, HMAC markers, nonblocking record/byte admission, fixed-app Dapr invocation, bounded batches, retry/age/expiry caps, shutdown flush, heartbeat lease behavior, and exception containment; the focused checkpoint passes 9/9.
- 2026-07-18: Completed C3/C4 with the Dapr-only fixed global actor, transactional record/index writes, strict idempotency/conflict handling, source-age TTL, strong-delete purge, durable reminder state, staged marker rotation, independent three-source signed time, and single-use clock gates in both Server and actor paths; focused checkpoints pass 10/10 and 9/9.
- 2026-07-18: Completed C5 with exact-profile fail-closed capability evaluation, restart-scoped runtime gating, separated authority policy, bounded health details, and low-cardinality queue/Dapr/clock/state/purge/capacity/reminder metrics; focused capability and observability evidence passes 19/19.
- 2026-07-18: Completed Task 7 with solution/AppHost/Aspire composition plus bounded Dapr and least-privilege Kubernetes resources. The Production overlay stays explicitly disabled and unproven pending Story 27.3 adapter certification.
- 2026-07-18: Completed C6 with portable two-writer, capacity, outage/retry, purge-concurrency, restart/recovery, business-isolation, authority-route, tenant/privacy, and disabled-Production evidence; C6 passes 8/8 and preserved emission/tenant regressions pass 69/69.
- 2026-07-18: Completed Task 9 with fresh comparable runner discovery, a clean 0-warning/0-error Release solution build, focused checkpoint/regression tests, static/deployment validation, implemented-truth telemetry documentation, and exact 139-path cumulative File List reconciliation.
- 2026-07-17: Added a fail-closed source-to-persisted mapping checkpoint for the current `partial`, authorized cross-case null-case, and search-axis/weight gaps. No runtime implementation was performed.
- 2026-07-17: Kept Production adapter certification, physical-reclamation proof, runbook, dashboards/alerts, and A41 closure in Story 27.3; kept EventStore dependency work in Epic 28.
- 2026-07-17: Independent create-story validation passed after reconciling exact C1-C6 evidence commands, separate Server/Integration/new-project discovery lanes, tenant-marker sentinel scope, complete static-validation inputs, and source anchors.

### File List

- `Hexalith.Memories.slnx`
- `_bmad-output/implementation-artifacts/27-2-bounded-retention-ttl-and-purge-implementation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/27-2-create-story-scope-evidence.md`
- `deploy/dapr/access-telemetry-clock-config.yaml`
- `deploy/dapr/access-telemetry-lifecycle-config.yaml`
- `deploy/dapr/components/access-telemetry-config.yaml`
- `deploy/dapr/components/access-telemetry-secrets.yaml`
- `deploy/dapr/components/access-telemetry-store.yaml`
- `deploy/kubernetes/base/access-telemetry-deployments.yaml`
- `deploy/kubernetes/base/access-telemetry-network-policy.yaml`
- `deploy/kubernetes/base/dapr/access-telemetry-clock-config.yaml`
- `deploy/kubernetes/base/dapr/access-telemetry-config-store.yaml`
- `deploy/kubernetes/base/dapr/access-telemetry-lifecycle-config.yaml`
- `deploy/kubernetes/base/dapr/access-telemetry-secrets.yaml`
- `deploy/kubernetes/base/dapr/access-telemetry-store.yaml`
- `deploy/kubernetes/base/dapr/config.yaml`
- `deploy/kubernetes/base/kustomization.yaml`
- `deploy/kubernetes/base/server-deployment.yaml`
- `deploy/kubernetes/base/service-accounts-rbac.yaml`
- `deploy/kubernetes/base/services.yaml`
- `deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml`
- `deploy/kubernetes/overlays/production/kustomization.yaml`
- `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md`
- `docs/dev/telemetry.md`
- `src/Hexalith.Memories.AccessTelemetry.Clock/AuthenticatedUtcResponse.cs`
- `src/Hexalith.Memories.AccessTelemetry.Clock/AuthenticatedUtcSample.cs`
- `src/Hexalith.Memories.AccessTelemetry.Clock/ClockAttestationException.cs`
- `src/Hexalith.Memories.AccessTelemetry.Clock/ClockAttestationService.cs`
- `src/Hexalith.Memories.AccessTelemetry.Clock/EcdsaClockAttestationSigner.cs`
- `src/Hexalith.Memories.AccessTelemetry.Clock/Hexalith.Memories.AccessTelemetry.Clock.csproj`
- `src/Hexalith.Memories.AccessTelemetry.Clock/HttpAuthenticatedUtcSource.cs`
- `src/Hexalith.Memories.AccessTelemetry.Clock/IAuthenticatedUtcSource.cs`
- `src/Hexalith.Memories.AccessTelemetry.Clock/IClockAttestationSigner.cs`
- `src/Hexalith.Memories.AccessTelemetry.Clock/Program.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryCanonicalizer.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryContractException.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryHealthState.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryInspectionResponse.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryOptions.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryOptionsValidationResult.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryOptionsValidator.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryReason.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryRecord.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryRecordState.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryWriteBatchRequest.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryWriteBatchResponse.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/BoundedNonceReplayCache.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/ClockAttestationCanonicalizer.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/ClockAttestationRequest.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/ClockAttestationValidationContext.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/ClockAttestationValidationResult.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/ClockAttestationVerifier.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/Hexalith.Memories.AccessTelemetry.Contracts.csproj`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/LifecycleConfigurationEpoch.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/MonotonicRecordIdGenerator.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/RetentionConfigurationSource.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/SignedClockAttestation.cs`
- `src/Hexalith.Memories.AccessTelemetry.Contracts/WriterHeartbeat.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryAuthority.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryAuthorityAction.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryAuthorityPolicy.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryCapabilityEvidenceOptions.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryCapabilityGate.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryCapabilityGateResult.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryCapabilityProbeContext.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryCapabilityProbeHostedService.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryCapabilityProbeResult.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryCapabilityProbeRunner.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryCapabilityProfile.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryRuntimeGate.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryRuntimeHealthCheck.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/IAccessTelemetryCapabilityProbe.cs`
- `src/Hexalith.Memories.AccessTelemetry/Capability/IAccessTelemetryRuntimeGate.cs`
- `src/Hexalith.Memories.AccessTelemetry/Hexalith.Memories.AccessTelemetry.csproj`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryClockGate.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryExpiryEntry.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryExpiryIndex.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleActor.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleActorState.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleProcessor.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryPersistenceResult.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryPersistenceStatus.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryPurgeResult.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryStoreWriteStatus.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryStoredRecord.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/IAccessTelemetryClockGate.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/IAccessTelemetryLifecycleActor.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/IAccessTelemetryStateStore.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/InMemoryAccessTelemetryStateStore.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/MarkerKeyRotationCoordinator.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/MarkerKeyRotationPhase.cs`
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/MarkerKeyRotationState.cs`
- `src/Hexalith.Memories.AccessTelemetry/Observability/AccessTelemetryHealthEvaluator.cs`
- `src/Hexalith.Memories.AccessTelemetry/Observability/AccessTelemetryHealthSnapshot.cs`
- `src/Hexalith.Memories.AccessTelemetry/Observability/AccessTelemetryLifecycleMetrics.cs`
- `src/Hexalith.Memories.AccessTelemetry/Program.cs`
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`
- `src/Hexalith.Memories.AppHost/Program.cs`
- `src/Hexalith.Memories.Aspire/HexalithMemoriesAccessTelemetryExtensions.cs`
- `src/Hexalith.Memories.Aspire/HexalithMemoriesAccessTelemetryResources.cs`
- `src/Hexalith.Memories.Aspire/MemoriesAccessTelemetryClockProjectMetadata.cs`
- `src/Hexalith.Memories.Aspire/MemoriesAccessTelemetryProjectMetadata.cs`
- `src/Hexalith.Memories.Aspire/README.md`
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDaprHttpClientFactory.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryWorker.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryHeartbeatWorker.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleBootstrapService.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleHealthCheck.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleLogger.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleLoggerProvider.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleStatus.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryLifecycleStatusSnapshot.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQueuedRecord.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetrySanitizer.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetrySanitizerAccessor.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryWriterIdentity.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/BoundedAccessTelemetryQueue.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/DaprAccessTelemetryClockEvidenceProvider.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/DaprAccessTelemetryDeliveryClient.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/DaprAccessTelemetryHeartbeatClient.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/IAccessTelemetryClockEvidenceProvider.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/IAccessTelemetryDeliveryClient.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/IAccessTelemetryHeartbeatClient.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/ServerAccessTelemetryLifecycleMetrics.cs`
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Capability/CapabilityAndObservabilityCheckpointTests.cs`
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Clock/ClockAttestationCheckpointTests.cs`
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Contracts/AccessTelemetryContractsCheckpointTests.cs`
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj`
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/LifecycleActorCheckpointTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostProjectResolutionTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryCheckpointTests.cs`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-18 | dev-story | Implemented the ratified portable access-telemetry lifecycle: strict contracts/configuration, typed-state sanitizing provider, bounded queue/retry delivery, Dapr-only fixed actor/state/purge, independent signed clock, exact-profile fail-closed capability gate, bounded observability, authority-separated Aspire/Dapr/Kubernetes topology, and focused proof. Production stays disabled pending Story 27.3 adapter certification and physical-reclamation evidence. | Runner-derived actual/cumulative deltas: Server.Tests 2,157 -> 2,169, +12/+12; IntegrationTests 270 -> 278, +8/+8; AccessTelemetry.Tests 0 -> 31, +31/+31; total +51/+51 discovered methods, with 0 removed from comparable lanes. Exact sorted hashes: Server `98440744599bf454ee991a3cad39f69dee9185f6d1fe86106223638c3a194ae2` -> `e6cc2ce48900f19ed62665b42056aa9ca0837a838720abbb577e0a366963c27c`; Integration `038e0a140092d0a7910c17566a5b87a05bdc417699f67c4335516777168a30c4` -> `92f4cfeb683a0bb2cd90305e92f61f00eb53542e2aac231f3c61b7a9dcf96d55`; new lane `4eea34c669021dc01446ddf0d44111c6aa38e482e92736ea155dcf84f5498460`. Executed checkpoint cases: C1 9, C2 9, C3 10, C4 9, C5 19, C6 8; full new lane 48/48 and preserved emission/privacy regression selection 69/69. | matched 139/139 cumulative story paths: 138 development-owned changed/untracked paths against baseline `6d7fd8aaa0a2fc58de741e31f38544fc15a10c08`, plus the retained create-story scope-evidence artifact. Unrelated Story 27.3 and externally advanced EventStore, FrontComposer, and Tenants submodules were preserved and excluded. |
| 2026-07-17 | create-story | Created Story 27.2 and moved it from `backlog` to `ready-for-dev`; Epic 27 was already `in-progress`; no implementation performed. | Actual +0 and cumulative +0 in Server.Tests, IntegrationTests, and the absent/new AccessTelemetry.Tests lane; planned +104 to +144 xUnit methods. Exact blocked builds: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` and the same command targeting `tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj`; both stopped at `NU1603` because exact 1.72.3 EventStore packages were unavailable. Exact observational discoveries: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods` and the same command targeting the IntegrationTests DLL, with count/hash pipelines in **Testing Baseline and Planned Delta**. Observational only: Server 2,151 and IntegrationTests 270; do not use either for delta. New AccessTelemetry.Tests is absent and declared as a planned `0 -> N` lane with its exact future discovery command in that section. | matched 3/3 against baseline `4856b0ab5d927ad07d82e5bed9b61597a380269e` and pre-create sprint SHA-256 `39b50b4c6a49553494bc3b2e7aeb58f76c0c84e421362984b84701d47821fad6`; exact owned-line diff, scoped status, hashes, verification commands, and same-file exclusions are in `_bmad-output/implementation-artifacts/tests/27-2-create-story-scope-evidence.md`. |
