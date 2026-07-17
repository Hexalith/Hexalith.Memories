---
baseline_commit: 119c0a49
creation_sprint_status_sha256: a0a85ef2a7dfd22493b677976e4eb9bd14f37b668ab03bdbe4237bd6c0bd4633
creation_scope_evidence: _bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md
---

# Story 27.1: Access Telemetry Retention Ownership Decision (Decision-First)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect and operator,
I want one ratified access-telemetry lifecycle contract,
so that implementation has an owned, deployable, and testable target.

## Acceptance Criteria

1. Given access telemetry currently reaches JSON console and optional OTLP export without a repository-owned bounded lifecycle, when the decision evaluates external OTLP storage, a dedicated write-only store, and any file/volume alternative, then it selects one design and records ownership, topology, multi-replica write behavior, durability boundary, retention default/range, expiry/purge semantics, clock source, failure/backpressure policy, recovery, observability, privacy/tenant boundary, capacity assumptions, and rollback.

2. Given production has two Server replicas and a read-only root filesystem, when the design is ratified, then no local-file approach is accepted without durable shared or per-replica storage, concurrency-safe rotation, defined pod-rescheduling behavior, and executable purge evidence; an unspecified external default does not count as a retention policy.

3. Given the PRD defines access records as infrastructure telemetry, when the assurance boundary is stated, then the decision does not claim tamper evidence, append-only integrity, legal compliance, or certified audit retention.

## Tasks / Subtasks

- [x] Task 1 - Re-run the current-state and privacy preflight before deciding (AC: 1, 2, 3)
  - [x] Verify that `AccessTelemetryLog` still emits the nine success/error operation families through `AccessTelemetryCategory`, and that ServiceDefaults still wires OpenTelemetry logging plus JSON console with OTLP conditional on `OTEL_EXPORTER_OTLP_ENDPOINT`.
  - [x] Verify the committed production topology still has two Server replicas, `readOnlyRootFilesystem: true`, no access-telemetry backend or OTLP endpoint, and only an ephemeral `/tmp` `emptyDir` volume.
  - [x] Verify that no committed component currently owns TTL, purge, persistent buffering, lifecycle health, or storage-capacity policy for access telemetry.
  - [x] Verify the storage seam from typed `AccessTelemetryEvent` logger state. Do not design the lifecycle provider by reparsing stdout: the current JSON console has a JSON outer envelope, but renders `@AuditEvent` through record `ToString()` and loses `QueryParams` structure.
  - [x] Re-check the typed event schema and every producer. Record the current privacy deviation that search and source-URI paths can place raw `query`, `subject`, or `sourceUri` values in `QueryParams`; do not ratify durable storage of those raw values.
  - [x] Record the current commit, moved anchors, package pins, and any adaptation in the Dev Agent Record before editing the decision artifacts.

- [x] Task 2 - Evaluate all three permitted design families and select exactly one (AC: 1, 2, 3)
  - [x] Compare a named deployment-owned external OTLP backend, a repository-owned dedicated write-only telemetry store, and file/volume storage in one evidence-backed matrix using the full lifecycle contract below.
  - [x] Name the selected technology and deployment shape. “Whatever the operator already has,” console rotation, an unspecified collector/backend, or documentation without an executable implementation target are not valid selections.
  - [x] Reject or block any file design that relies on the Server root filesystem or `/tmp`, shares a file without concurrency-safe rotation, loses records on pod replacement, or lacks executable purge evidence available during the decision.
  - [x] Treat a dedicated store as infrastructure telemetry storage, separate from the primary domain-data store and Hexalith.EventStore. It must use separate credentials, ownership, capacity, and lifecycle policy.
  - [x] Record the evidence-weighted recommendation as a recommendation until the owner, default/range, privacy policy, durability boundary, and operating cost are ratified. The final ratified target is a Dapr-only lifecycle service and component capability/evidence contract; it is not selected merely because a backend technology is already used elsewhere.
  - [x] If no candidate satisfies every hard gate, leave the ADR proposed, mark this story blocked during implementation, and do not start Stories 27.2 or 27.3.

- [x] Task 3 - Complete the lifecycle contract and ratify it only after every hard gate passes (AC: 1, 2, 3)
  - [x] Create `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` with status, date, approver, architecture owner, operational lifecycle owner, affected deployment, proposed family/technology, rejected alternatives, and explicit implementation gate.
  - [x] Define the writer-to-sink topology, separate resource/credential boundary, two-replica key or record uniqueness, concurrency semantics, acknowledgement point, persistence mode, restart/rescheduling behavior, and exact acknowledged loss window.
  - [x] Ratify a production retention default and allowed minimum/maximum with units, all-nine-operation capacity evidence, and configuration ownership. Missing, malformed, below-minimum, above-maximum, or silently unbounded values must have explicit startup behavior.
  - [x] Define whether age begins at event emission or backend acceptance, the authoritative UTC clock, late-arrival and clock-skew treatment, logical expiry, physical reclamation, purge cadence/grace, and preservation of newer records.
  - [x] Define bounded queue capacity, retry/backoff and maximum age, shutdown flush limit, overflow/drop reasons, outage behavior, recovery/drain, disk/space exhaustion behavior, and whether invalid configuration fails startup. No telemetry-provider exception may escape into a business request.
  - [x] Define low-cardinality accepted/rejected/enqueued/persisted/retried/failed/dropped/expired/purged and health signals, their owner and `NoData` meaning, and capacity thresholds. Tenant, user, case, unit, query, or source values must not become metric labels.
  - [x] Define the persisted privacy schema and operational inspection boundary. Apply least privilege with separate write, lifecycle/purge, and inspection authorities appropriate to the selected family; the Server's authority must not include inspection or arbitrary record deletion. No tenant-facing read API is introduced. Require sanitization or bucketing of raw query and source values before persistent storage.
  - [x] Define rollback so existing JSON console/optional OTLP emission remains intact, retained records continue expiring, persistent storage is not deleted automatically, and a rollback to the old image is explicitly degraded rather than an acceptable steady state.
  - [x] State the assurance boundary verbatim: bounded infrastructure telemetry only; no tamper evidence, append-only integrity, legal compliance, or certified audit retention.

- [x] Task 4 - Project the proposed decision without pretending it is ratified or implemented (AC: 1, 2, 3)
  - [x] Update `_bmad-output/planning-artifacts/architecture.md` to replace the stale “MVP: structured log file” placeholder with the selected, proposed lifecycle target and a link to the canonical ADR.
  - [x] Update `docs/dev/telemetry.md` to distinguish current JSON-console/optional-OTLP emission from the proposed target, link the ADR, remove stale five-operation/one-node assumptions, and identify any current privacy deviation without claiming it is already fixed.
  - [x] Keep `20.5-A41-ACCESS-TELEMETRY-RETENTION` `carried-forward` and its matching action `open`. Do not edit the deferred ledger merely to imply progress; Story 27.3 owns evidence-backed closure coordination.
  - [x] Do not change production source, application settings, package references, Kubernetes/AppHost resources, deployment configuration tables, or operations runbooks in this decision story. Those belong to Stories 27.2 and 27.3.
  - [x] Record an exact Story 27.2 implementation map and Story 27.3 production-shaped verification map in the ADR, including two writers, restart/rescheduling, deterministic age bounds, purge, newer-record preservation, emission continuity, failure modes, and tenant/privacy negatives.

- [x] Task 5 - Add executable decision-drift guards (AC: 1, 2, 3)
  - [x] Add `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs` using the existing repository-root and Markdown contract-document seams.
  - [x] Add six focused facts: exactly one family is proposed and all three are evaluated; every required lifecycle field is present; retention is bounded and has no silent unbounded fallback; production two-replica/read-only-root facts and file hard gates remain explicit; architecture/telemetry/ADR are cross-linked and do not retain the stale structured-log-file claim; assurance/A41/privacy/downstream-story boundaries and the accepted/rejected/enqueued/persisted/retried/failed/dropped/expired/purged signal set remain explicit.
  - [x] Make guards structure-aware by reading exact headings/table rows. Avoid brittle whole-document substring counts or accepting required vocabulary from unrelated sections.
  - [x] Keep these as decision-document guards only. Provider, TTL, queue, Dapr integration, multi-writer, restart, purge, and cross-tenant runtime tests belong to Stories 27.2 and 27.3.

- [x] Task 6 - Validate the decision-only completion (AC: 1, 2, 3)
  - [x] Run `git diff --check -- docs/dev/adr-27.1-001-access-telemetry-lifecycle.md _bmad-output/planning-artifacts/architecture.md docs/dev/telemetry.md tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md _bmad-output/implementation-artifacts/sprint-status.yaml`.
  - [x] Build `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` with the repository-pinned SDK, warnings as errors, Release configuration, and build-server isolation; then run the new decision test class directly with the xUnit v3 executable.
  - [x] Re-run the existing access-event schema, audit-emission, OpenTelemetry-registration, and metric-policy classes. These prove no regression in existing emission; they do not prove lifecycle implementation.
  - [x] If the current OpenTelemetry 1.16.0/OTLP exporter 1.17.0 downgrade error persists, report it as a pre-existing build blocker. Do not turn Story 27.1 into a package-upgrade story or claim a fresh-build result from an older binary.
  - [x] Reconcile the File List and the canonical phase ledger before moving the story beyond `in-progress`; no planned test count may be reported as actual.

### Review Findings

- [x] [Review][Patch] [High] Return the ADR to `Proposed` and block Stories 27.2/27.3 until the all-nine-operation capacity recalculation satisfies the ratification gate [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:182`]
- [x] [Review][Patch] [High] Keep the 15-minute active key/index purge bound and add a monitored, verified 24-hour AOF/RDB compaction bound [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:127`]
- [x] [Review][Patch] [High] Require Redis members to remain within 1 second UTC, block rollout and mark lifecycle unhealthy when exceeded, record promotion clock deltas, and reject purge evidence during violations [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:142`]
- [x] [Review][Patch] [High] Process at most 512 due records per purge script, enforce a 100 ms observed execution budget, and resume with bounded backoff until oldest-due age is within 15 minutes [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:173`]
- [x] [Review][Patch] [Medium] Correct the options matrix's contradictory `7-day default` wording [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:53`]
- [x] [Review][Patch] [High] Define correctly configured sink unavailability during Server startup as bounded degraded operation [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:218`]
- [x] [Review][Patch] [Medium] Replace second-precision `EXAT` with millisecond-precision `PXAT` throughout the contract [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:154`]
- [x] [Review][Patch] [Medium] Add marker-key versioning and retain verification keys through maximum retention plus purge grace [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:254`]
- [x] [Review][Patch] [High] Make the options guard verify every required lifecycle row and three nonblank candidate evaluations [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:25`]
- [x] [Review][Patch] [Medium] Guard the selected Redis technology, exact image digest, and projection consistency [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:31`]
- [x] [Review][Patch] [Medium] Guard the five-minute retry-age and absolute-expiry caps independently of five-second limits [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:90`]
- [x] [Review][Patch] [Medium] Guard the complete bounded metric-label contract instead of only the literal `tenant_id` [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:194`]
- [x] [Review][Patch] [Medium] Make create-story scope verification survive valid later status transitions [`_bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md:51`]
- [x] [Review][Patch] [High] Remove unsupported anti-template completion evidence claiming a comprehensive developer guide [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:306`]
- [x] [Review][Patch] [High] Record the exact comparable diagnostic build and pre/post discovery commands behind the `2,145 -> 2,151` ledger delta [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:326`]
- [x] [Review][Patch] [High] Make the Sentinel and persistent-volume topology executable by spreading Sentinels across independent failure domains and naming the StatefulSet identity, StorageClass/PV reclaim owner, binding/access modes, and topology constraints [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:89`]
- [x] [Review][Patch] [High] Replace the non-executable Kustomize clock preflight with a runtime gate against an independent UTC reference, and apply the bound to Server emission clocks as well as Redis members [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:148`]
- [x] [Review][Patch] [Medium] Stop treating `UNLINK` as completed physical memory reclamation: use a bounded synchronous deletion path or monitor lazy-free allocator completion separately [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:186`]
- [x] [Review][Patch] [High] Define the post-start configuration-invalid transition when Redis was unreachable during startup and the first connection later reveals invalid credentials, version, or function-library fingerprint [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:246`]
- [x] [Review][Patch] [Medium] Define lifecycle-health precedence so an unavailable or unhealthy sink cannot be masked by the `NoData` state [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:270`]
- [x] [Review][Patch] [High] Complete the privacy boundary with TLS for client, replication, and Sentinel links, namespace NetworkPolicy isolation, and explicit persistent-volume encryption ownership [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:281`]
- [x] [Review][Patch] [Medium] Retain old marker verification keys from the final old-key write for maximum retention plus accepted future skew and purge grace [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:303`]
- [x] [Review][Patch] [Medium] Reject `record_id_conflict` when an existing record key has payload bytes or absolute expiry different from the alleged retry [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:124`]
- [x] [Review][Patch] [High] Isolate lifecycle-provider filtering from the documented category-level `Warning` throttle so success records still reach the retention target [`docs/dev/telemetry.md:183`]
- [x] [Review][Patch] [Medium] Expand the six decision guards to preserve the exact durability scope, absolute clock gate, privacy warning, architecture assurance/A41 projection, and newly repaired topology, startup, health, purge, and security clauses [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:102`]
- [x] [Review][Patch] [Medium] Repair the phase ledger by updating the stale canonical-blocker consequence/reopen trigger, linking the dev-story cell to its exact discovery commands, and labeling the `2,697` execution results as xUnit test cases [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:198,374-375`]
- [x] [Review][Decision] [High] Reconcile clock-preflight readiness with degraded startup — The ADR makes Server readiness wait for a successful Redis/independent-UTC preflight while also requiring a correctly configured but unreachable sink to leave the business service running. Decide whether business readiness stays available with lifecycle health fail-closed, or whether the whole Server rollout must remain unready. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:185-205,317-340`]
- [x] [Review][Patch] [High] Run continuous independent-UTC enforcement through a platform-neutral Dapr-invoked clock-attestation service; every service instance must require a fresh attestation covering its process identity and the configured Dapr state component [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:185-205`]
- [x] [Review][Patch] [High] Replace Kubernetes clock Jobs/Leases, Pod identity, and Redis topology with a platform-neutral clock-attestation service invoked through Dapr service invocation; bind short-lived attestations to deployment, service-instance, process, and Dapr component identities [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:230-270`]
- [x] [Review][Patch] [High] Replace the Redis/Kubernetes selected design with Dapr state management as the sole application persistence dependency, a fixed-ID Dapr lifecycle-controller actor, durable Dapr actor state/reminders, and a fail-closed component-capability gate; alpha Dapr components are permitted [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:43-174`]
- [x] [Review][Patch] [High] Replace Redis lazy-free cohort evidence with a portable logical-expiry/deletion contract plus mandatory component-specific physical-reclamation verification outside the application API [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:236-254`]
- [x] [Review][Patch] [High] Replace AOF/RDB-specific compaction ownership with a Dapr component capability and evidence contract that names the deployment adapter responsible for physical reclamation [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:262-270`]
- [x] [Review][Decision] [High] Ratify exact persisted-schema bounds — The contract names bounded catalogs, numeric values, buckets, enums, and identifiers without defining their allowed values, maxima, canonical encodings, serialization limits, or schema-version behavior; a record can also exceed the worker's 1-MiB batch ceiling. Select the authoritative catalogs and bounds. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:301-306,372-400`]
- [x] [Review][Decision] [High] Reconcile the one-second clock gate with the two-minute future-skew window — The global clock contract rejects a writer more than one second from UTC, while the retention contract accepts its event timestamp up to two minutes ahead. Choose one consistent acceptance bound or explicitly distinguish independently verifiable cases. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:185-205,226-234`]
- [x] [Review][Patch] [High] Coordinate marker-key rotation through the fixed-ID Dapr lifecycle actor: stage the key, collect every live writer's acknowledgement through Dapr, wait for old-key queues to drain or expire, then start the retention-plus-grace overlap window with durable actor reminders and recovery [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:629-646`]
- [x] [Review][Patch] [High] Grant the writer the Redis `TIME` capability required by its minute-by-minute clock comparison [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:135-157`]
- [x] [Review][Patch] [High] Make the Story 27.2 remote gate require the selected Redis 7.4 image/digest instead of accepting Redis 7.2 [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:455-458`]
- [x] [Review][Patch] [High] Add production-shaped one-PVC loss or corruption recovery evidence for the zero-loss durability claim [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:175-183,496-503`]
- [x] [Review][Patch] [Medium] Rewrite decision guards for the Dapr-only contract, including exact component-capability gates, actor ownership/recovery, clock service, authority/configuration rows, retention-age rules, request-path isolation, and complete Story 27.2/27.3 handoff maps [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:81,163,268`]
- [x] [Review][Patch] [High] Define retry comparison so Redis-generated `acceptedAtUtc` cannot turn an exact retry into `record_id_conflict` [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:160-165`]
- [x] [Review][Patch] [High] Mark only successful or idempotent FCALL results persisted after a batch-level `WAITAOF` succeeds [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:167-173`]
- [x] [Review][Patch] [High] Re-run remote identity, ACL, version, and function-fingerprint validation after reconnect or contract-epoch change [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:323-340`]
- [x] [Review][Patch] [High] Add a data-member disruption budget and replica-catch-up gate before allowing a second voluntary disruption [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:88-114`]
- [x] [Review][Patch] [Medium] Allow the create-story monotonic-status check to accept `epic-27: done` after valid completion [`_bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md:67-76`]
- [x] [Review][Patch] [High] Repair the first code-review ledger row with phase-specific pre/post discovery evidence or a complete unavailable-evidence blocker [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:318,440`]
- [x] [Review][Defer] [High] Reconcile raw privacy-sensitive state on preserved JSON-console and optional OTLP routes [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:374-400`] — deferred, pre-existing
- [x] [Review][Decision] [High] Disposition the open platform-neutral clock-attestation patch — applied the user-ratified Dapr service-invocation redesign and re-ratified the ADR, projections, and guards without Kubernetes Job/Lease/Pod identity. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:230-270`]
- [x] [Review][Decision] [High] Reconcile attestation expiry with the accepted controller-outage loss accounting — the clock service is independent of the actor, attestations refresh every 10 seconds and expire after 30 seconds, stale evidence stops lifecycle persistence, and queue/retry/drop accounting is explicit while business readiness remains available. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:253`]
- [x] [Review][Decision] [High] Bound write outages from data-member maintenance under `min-replicas-to-write 1` — superseded by the Dapr component durability/capacity profile; no Redis replica policy remains, and each selected adapter must prove its declared single-component failure and recovery envelope. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:200`]
- [x] [Review][Decision] [Medium] Reconcile `WAITAOF 1 1 1500` with the permitted replica state — superseded by Dapr transaction acknowledgement and measured component throughput/drain gates; no Redis acknowledgement timeout remains. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:204`]
- [x] [Review][Decision] [Medium] Align the capacity fixture with its ratified methodology — retained the deterministic values as a representative contract fixture and made the exact 1,024-byte record cap, not its average/P95, the admission ceiling. [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:2256`]
- [x] [Review][Decision] [Medium] Define rotation-barrier membership across rollouts — the fixed actor now uses leased dynamic writer membership, a frozen generation snapshot, new-writer gating, acknowledgements, and bounded drain/expiry recovery without Pod identities. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:390-400`]
- [x] [Review][Decision] [Medium] Reconcile the 1-second future-skew rejection with the 1-second pairwise clock bound — process and event checks now use the same attested interval and one-second absolute bound, with transport uncertainty required to fit inside rather than extend it. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:185-205`]
- [x] [Review][Decision] [Medium] Rule the missing third code-review ledger row — the current code-review row records the historical third-review attribution gap and the Dapr supersession with a phase delta of zero methods. [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:526`]
- [x] [Review][Patch] [High] Refresh the stale canonical `NU1605` blocker record — OpenTelemetry core now pins 1.17.0; reran the unsuppressed canonical Release build and four-filter discovery and recorded the current result in the final code-review ledger row. [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:229`]
- [x] [Review][Patch] [Medium] Correct the false "all third-review findings resolved" claims in the Debug Log, Completion Notes, and 2026-07-17 dev-story ledger row to state that one High finding remained open until this review's Dapr disposition. [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:337,504,534`]
- [x] [Review][Patch] [Medium] Sync stale proposal-state wording to the Accepted decision — Dev Notes, Slice Proof, the ADR accepted-value headers, and guards now agree. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:5,279`]
- [x] [Review][Patch] [Medium] Name the `deferred-work.md` same-file exclusions — the story's final review record separates the Story 27.1 entries from concurrent `8bb0708a` content and records the authorized review-ledger deviation. [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:534`]
- [x] [Review][Patch] [Medium] Name the `sprint-status.yaml` post-baseline exclusions — the scope evidence now identifies the concurrent Epic 28 and Epic 18/19 changes as excluded. [`_bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md:51`]
- [x] [Review][Patch] [Medium] Reconcile the ADR section contract — the spec now lists all 15 top-level sections including `Capacity Evidence and Admission Envelope`, and the adaptation is recorded. [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:166,343`]
- [x] [Review][Patch] [Medium] Tie the capacity envelope to the source operation set — the existing capacity guard now derives the nine `AccessTelemetryLog` operation constants and matches them exactly to the envelope rows without adding a test method. [`src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLog.cs:38`]
- [x] [Review][Patch] [Medium] Restore an interim-scoped regulated-tenant warning in telemetry.md — the current-only console/OTLP window now explicitly requires `Information` or an accepted loss of success records. [`docs/dev/telemetry.md:183`]
- [x] [Review][Patch] [Medium] Specify the fencing-epoch mechanism — superseded by the fixed-ID Dapr actor's serialized turns, durable configuration epoch, and idempotent reminders; no Lease or Redis fencing counter remains. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:128`]
- [x] [Review][Patch] [Low] Normalize the new test file's mixed CRLF/LF working-tree line endings to CRLF [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:1`]
- [x] [Review][Patch] [Low] Remove the duplicated "768 KiB of the 1-MiB command limit" normative sentence; the rewritten Dapr request-size contract states the limit once. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:520`]
- [x] [Review][Patch] [Low] Allow the scope-evidence monotonic-status check to accept the story's sanctioned `blocked` transition [`_bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md:67`]
- [x] [Review][Patch] [Low] Add the retention-decrease clause — lowered retention applies to new immutable expiries only and accelerated retroactive purge is explicitly out of scope. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:279`]
- [x] [Review][Patch] [Low] Ratify the per-family optional-field mapping (`caseMarker`, `resultCount`) in Persisted Schema Bounds to match the fixture encoding. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:465`]
- [x] [Review][Patch] [Low] Align the guard measurement encoder with the ratified RFC 8785-compatible contract fixture using relaxed escaping and ordinal key ordering; fixture sizes remain unchanged. [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:2248`]
- [x] [Review][Patch] [Low] Harden guard table parsing — cell counts are asserted, row uniqueness is diagnostic, and P95 cells parse as integers. [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:1994`]
- [x] [Review][Patch] [Low] Make `ResolveRepoRoot` fail fast when the solution marker is absent instead of falling back to `AppContext.BaseDirectory` [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:2329`]
- [x] [Review][Patch] [Low] Extend the TBD/placeholder guard from one section to every ratified contract section [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:1976`]
- [x] [Review][Patch] [Low] Name the blocker owner in the first 2026-07-16 code-review ledger row's unavailable-evidence record [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:532`]
- [x] [Review][Patch] [Low] Correct the dev-story preflight SDK pin record to distinguish the 10.0.301 baseline snapshot from the later 10.0.302 working tree. [`_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md:345`]
- [x] [Review][Patch] [Low] Pin the architecture projection's current Dapr-neutral numeric capacity figures (151,200,000 records and 144.20 GiB canonical payload) in the cross-link guard. [`tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:342`]
- [x] [Review][Patch] [Low] State the two-replica traffic-model precondition explicitly while allowing any writer count through dynamic rotation/attestation; a changed cluster envelope requires an ADR amendment. [`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:89`]
- [x] [Review][Defer] [Low] `docs/operations/rate-limiting.md` was dropped from the A41 target-artifact list by the concurrent A41-entry rewrite (commit `8bb0708a`) without a stated disposition [`_bmad-output/implementation-artifacts/deferred-work.md:2205`] — deferred, pre-existing (concurrent proposal-owned edit, not Story 27.1 scope)

## Dev Notes

Story 27.1 is the architecture/operator decision gate for Epic 27. Its one deliverable is a ratified, owned, deployable, and testable lifecycle contract. The accepted design is a container-service-neutral Dapr-only lifecycle service with a fixed-ID actor and a deployment-specific component capability/evidence gate; it has no Redis or Kubernetes application dependency. Stories 27.2 and 27.3 are unblocked to implement and verify it. Story 27.3 supplies Production-shaped expiry/purge and physical-reclamation evidence, the operations runbook, and coordinated A41 close-out. Completing or scheduling 27.1 alone cannot close the residual. [Source: _bmad-output/planning-artifacts/epics.md#Story-27.1; _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-access-telemetry-retention-implementation.md]

### Discovery Results

- Loaded the complete planning inputs: `_bmad-output/planning-artifacts/epics.md`, `prd.md`, `architecture.md`, and `ux-design-specification.md`.
- Loaded the approved implementation proposal and the earlier visibility proposal. The 2026-07-16 implementation proposal supersedes the visibility proposal only on scheduling: the implementation path is now approved and Epic 27 is registered. The visibility proposal's two-path closure guard remains valid.
- Loaded repository and reference `project-context.md` files, the story-scope guard, story-creation lessons, phase-ledger policy, Epic 20 retrospective, Story 20.5 record, A41 deferred entry, current deployment/operator docs, source, tests, and recent relevant git history.
- Reverified current source rather than inheriting the stale “structured log file” planning premise.
- No user-facing UX or FrontComposer work is in scope. UX contributes only operator semantics: visible state, cause, impact, next action, owner, and privacy scope.

### Audit-Anchor Preflight

Reverified during story creation on 2026-07-16 against `HEAD` `119c0a49` with a pre-existing dirty worktree:

- `ConfigureOpenTelemetry` always adds OpenTelemetry logging and UTC JSON console output. `AddOpenTelemetryExporters` calls `UseOtlpExporter()` only when `OTEL_EXPORTER_OTLP_ENDPOINT` is non-empty; Production without it only registers a warning service. There is no owned retention, buffer, or backend. [Source: src/Hexalith.Memories.ServiceDefaults/Extensions.cs:74-109,238-250,534-543]
- `AccessTelemetryLog` emits nine success and nine error operation families through the dedicated `AccessTelemetryCategory`; event timestamps currently use `DateTime.UtcNow`. [Source: src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLog.cs:38-63,80-177]
- Production has two Server replicas, a read-only root filesystem, and only `/tmp` backed by `emptyDir`; the manifest has no OTLP endpoint, collector, lifecycle store, or durable access-telemetry volume. [Source: deploy/kubernetes/base/server-deployment.yaml:8-9,41-101,109-145]
- Kustomize is the production authority. AppHost is local reference topology, and it also has no access-telemetry store. [Source: docs/operations/deployment-configuration.md:11-22; src/Hexalith.Memories.AppHost/Program.cs:47-56,146-182]
- The architecture statement “MVP: structured log file” is stale and contradicted by source and operator docs. [Source: _bmad-output/planning-artifacts/architecture.md:227; _bmad-output/implementation-artifacts/deferred-work.md:2205-2207]
- Current telemetry docs correctly say routing is not retention, but their routing recipe and volume/log-level notes contain stale or unsafe assumptions: an unimplemented file route, five-operation event ranges, one-node capacity, and raw query examples. [Source: docs/dev/telemetry.md:134-187,266-279]
- Search and source-URI producers can put raw `query`, `subject`, or `sourceUri` values in the event `QueryParams`. This conflicts with the earlier sanitized-field intent and must become an explicit privacy prerequisite for 27.2, not a silently retained payload contract. [Source: src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs:124-143; src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs:69-81; _bmad-output/planning-artifacts/epics.md#Story-7.5]
- The JSON console outer envelope renders `@AuditEvent` through record `ToString()` rather than lossless nested JSON; `QueryParams` becomes a type name. Stdout is valid emission evidence but not a replayable storage input. The typed `AccessTelemetryEvent` logger state is the stronger future storage seam, and existing test infrastructure already extracts it. [Source: tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/AuditEventStreamReader.cs:238-246,393-413; tests/Hexalith.Memories.Server.Tests/Telemetry/Infrastructure/CapturingAuditLoggerProvider.cs:69-114]

Re-run this preflight when development starts. If any premise has changed, adapt the decision inputs before ratification.

### Decision Matrix and Hard Gates

| Family | Current fit | Mandatory proof before selection | Current risk |
| :----- | :---------- | :------------------------------- | :----------- |
| Named external OTLP backend | Reuses the existing exporter seam and can centralize policy. | Name and pin the collector/backend deployment contract, lifecycle owner, authentication/TLS, bounded backend retention, purge semantics, queue/WAL and loss boundary, two-writer identity, capacity, observability, and validation. | The repository currently names none of these. An OTLP endpoint proves routing only. |
| Dapr-backed dedicated lifecycle service | Strong repository ownership with a Dapr-only application boundary and portable logical lifecycle semantics. | Dapr service invocation, fixed actor, strong transactional state/ETag/TTL capability, independent-UTC attestation, bounded queue/retry, durability, capacity, purge, adapter physical evidence, and separate authorities. | The operator must select and prove an eligible Dapr component; no backend SDK or orchestrator API may leak into Memories. |
| File/volume | Simple format but poor fit for the committed production shape. | Durable shared or per-replica volume, safe concurrent rotation, stable pod identity/rescheduling, read-only-root compatibility, disk-pressure behavior, executable purge, capacity, recovery, and inspection controls. | Current Server has only ephemeral `/tmp`; shared-file concurrency and lifecycle ownership are unresolved. |

Ratified outcome: use a dedicated Dapr-addressed lifecycle service. Memories depends only on Dapr service invocation; the lifecycle service uses Dapr state, actors/reminders, configuration, and secrets. A fixed `global` actor serializes writes and control. The selected state component, including an explicitly opted-in alpha component, must pass the accepted capability, durability, capacity, isolation, and physical-reclamation evidence gates. No Redis, Kubernetes, or other backend/orchestrator API is part of the application contract.

### Required ADR Structure

The canonical ADR must contain exactly one instance of each top-level contract section so structure-aware guards can validate it:

1. Status and Decision Metadata
2. Verified Current State
3. Options Evaluated
4. Selected Design and Rejected Alternatives
5. Ownership and Topology
6. Multi-Replica Write and Durability Boundary
7. Retention, Expiry, Purge, and Clock
8. Capacity Evidence and Admission Envelope
9. Failure, Backpressure, Recovery, and Capacity
10. Observability
11. Privacy and Tenant Boundary
12. Rollback and Transition
13. Assurance Boundary
14. Story 27.2 Implementation Handoff
15. Story 27.3 Verification and Operations Handoff

The decision must use concrete values and names. A blank owner, “TBD,” “operator default,” “backend default,” or unbounded duration fails ratification. If file/volume is selected, the executable purge evidence required by AC2 must already exist as decision-support evidence; creating a production file pipeline in 27.1 would violate the slice.

### Security, Privacy, and Tenant Isolation

- FR67 requires per-tenant access records, while zero cross-tenant leakage remains a hard gate. Storage must preserve the sanitized tenant marker and must not expose a tenant-facing query API. [Source: _bmad-output/planning-artifacts/prd.md:929-935]
- Preserve principal-derived user identity and denial-before-dependency behavior. Story 27.2/27.3 negative evidence must cite the Story 20.2 authorization tests and Story 24.3 verifier/tenant-marker tests wherever the new sink becomes scope-sensitive.
- Unknown, malformed, and rejected tenant scope must remain represented by the bounded synthetic marker; metrics must not use tenant identifiers or other unbounded labels.
- Retention of access records does not inherit domain tenant-deletion semantics automatically. The ADR must state whether deleted-tenant identifiers remain until normal expiry and why, without making legal-erasure claims.
- Do not store raw query text, source payloads, tokens, secrets, credentials, or unbounded metadata. The current `QueryParams` deviation is an implementation prerequisite, not an approved schema.
- Invalid lifecycle configuration should fail closed at Production startup. A transient sink failure must have an explicit availability/loss policy; it must not throw from `ILogger.Log`, block indefinitely, or fall back silently to unbounded local disk.

### Capacity and Recovery Guardrails

- The current estimate is roughly 2.5 GB/day per node at its stated high-end assumptions. Two replicas would be roughly 5 GB/day raw before key/object overhead, indexes, persistence logs, fragmentation, replica overhead, and rewrite workspace. Recalculate with cluster request distribution and all nine operation families before selecting a duration. [Source: docs/dev/telemetry.md:266-279]
- Capacity evidence must state event rate, average and high-percentile serialized size, cluster versus per-replica load, retention window, storage overhead, purge workload, queue/outage budget, headroom, alert thresholds, and exhaustion behavior.
- Rollback applies only to the new lifecycle layer. It must not remove Story 20.5 audit emission or rate limiting, delete persistent storage, disable Information-level success events, or falsely mark A41 closed.

### Latest Technical Information

Research snapshot: 2026-07-16. Use repository pins during implementation; Story 27.1 changes no packages.

- The current repository uses .NET SDK 10.0.302, `net10.0`, C# 14, warnings as errors, Aspire AppHost SDK 13.4.6, Dapr 1.18.4, OpenTelemetry core 1.17.0, and StackExchange.Redis instrumentation 1.16.0-beta.1. The earlier Story 27.1 baseline at `119c0a49` used SDK 10.0.301 and OpenTelemetry core 1.16.0; those are historical snapshot facts, not current pins. [Source: global.json; Directory.Build.props; Directory.Packages.props; src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj; references/Hexalith.Builds/Props/Directory.Packages.props]
- Official OpenTelemetry guidance treats an OTLP gateway as a separately operated service and explicitly documents in-memory queue overflow, retry expiry, collector-crash loss without persistent storage, WAL disk failure, and the need to monitor queue capacity. An endpoint alone is therefore not a durability or retention policy. [Source: https://opentelemetry.io/docs/collector/deploy/gateway/; https://opentelemetry.io/docs/collector/resiliency/]
- Dapr state capabilities vary by component; actor stores require transactions and ETags, TTL support is not universal, and unsupported TTL metadata may be ignored. Story 27.2 therefore proves behavior against the exact component rather than inferring it from the Dapr API name. [Source: https://docs.dapr.io/reference/components-reference/supported-state-stores; https://docs.dapr.io/developing-applications/building-blocks/state-management/state-store-ttl/]
- Dapr service invocation provides the portable clock/lifecycle app-to-app boundary, while the fixed actor supplies turn serialization and durable reminders. Dapr does not supply trusted UTC or detached signing, so the clock service and application-level signature remain explicit owned behavior. [Source: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/; https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/; https://docs.dapr.io/developing-applications/building-blocks/actors/actors-timers-reminders/]
- Do not upgrade the SDK, Aspire, Dapr, OpenTelemetry, or any selected Dapr component in this decision story. The exact runtime/component version and alpha opt-in become Story 27.2 inputs.

### Test Baseline and Planned Delta

An observational runner inventory on 2026-07-16 used an existing Release xUnit artifact:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods > /tmp/story-27-1-server-tests-list.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.' /tmp/story-27-1-server-tests-list.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.Architecture\.' /tmp/story-27-1-server-tests-list.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.Telemetry\.' /tmp/story-27-1-server-tests-list.txt
```

Observed inventory: 2,132 Server test methods; named affected units `Architecture` = 18 and `Telemetry` = 141. The captured list output had SHA-256 `58ec23e95ab2f83bb6ccadbf0ae554c87c9b64a8bb10b0e6c85648e4076eb99c`, but the DLL has no verified build-commit provenance and is not a comparable canonical baseline. Planned Story 27.1 delta: six new methods in `AccessTelemetryRetentionDecisionTests`; actual create-story delta: +0.

A fresh Release build was attempted with:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

It failed during restore with `NU1605`: `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.17.0` requires `OpenTelemetry >= 1.17.0`, while the repository directly pins `OpenTelemetry 1.16.0`.

- **Canonical discovery status:** blocked; do not subtract a later count from the 2,132 observational inventory. The unchanged-pin diagnostic lane separately produced comparable current-source evidence for the six-method story delta.
- **Blocker owner:** the shared `Hexalith.Builds` dependency-pin maintainer together with the Memories build maintainer.
- **Consequence:** the repository cannot produce an unsuppressed canonical Release build or canonical discovery total. The diagnostic build and method delta remain explicitly noncanonical; they verify the current-source guards without replacing the blocked canonical lane.
- **Measurable reopen trigger:** after the shared OTLP exporter and local OpenTelemetry core pins resolve compatibly, run the exact unsuppressed Release build above and the same xUnit `-list methods` discovery against its output. Record the canonical post-review method set and compare its fully qualified names with the diagnostic post-review set; do not invent or reconstruct a canonical pre-change total.

Existing Release-artifact focused observations gathered during analysis:

- Access telemetry log/schema/versioning classes: 21 passed.
- Audit streams, OpenTelemetry registration, and metric-policy classes: 59 passed.
- `python3 -m unittest tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py`: 6 passed.

These noncanonical observations show that the existing artifact is runnable; they are not current-source build evidence and cannot establish an actual delta. The current Aspire audit integration test proves one-resource stdout emission; it does not prove retention, two writers, restart, rescheduling, TTL, purge, or privacy-safe storage.

### Historical Context Classification

| Reference | Classification | Permitted influence on Story 27.1 |
| :-------- | :------------- | :-------------------------------- |
| Story 20.5 | `anti-template` | Residual provenance only. It is a completed, broad rate-limit/audit bundle and must not be reopened or copied as the shape of this narrow decision story. |
| Story 7.5 | `anti-template` | FR67 schema/privacy intent only. Its broad system-observability scope is not reusable. |
| Story 20.2 | `historical-reference-only` | Principal-derived identity and denial-before-dependency evidence context for later scope-sensitive sink work. |
| Story 24.3 | `historical-reference-only` | Verifier/tenant-marker negative-evidence context for Stories 27.2 and 27.3; do not copy its decision-plus-implementation shape. |
| Epic 20 retrospective and doc verification | `historical-reference-only` | Carry forward current-anchor preflight, explicit partial closure, negative tests, and operator-document reconciliation; old green evidence is not current lifecycle proof. |
| Story 26.1 and Story 26.5 | `anti-template` | Current manifests/runbook principles only; their broad infrastructure and checkpoint-heavy shapes must not expand 27.1. |
| Story 26.6 | `current-narrow-pattern` | Reverified current deployment-state capture, rollback, and observable restoration semantics may shape the ADR recovery/rollback clauses. |
| Story 21.1 | `current-narrow-pattern` | Reverified `ConsistencyModelDecisionTests` may shape the narrow structure-aware decision-drift guard only; do not copy the story's broader history or file set. |
| Story 24.4 | `current-narrow-pattern` | Reverified low-cardinality metric/tag policy may shape lifecycle observability only. |
| Story 26.8 | `historical-reference-only` | Establishes sequencing before Epic 27 only; numeric adjacency has no implementation relevance. |

### Slice Proof

Story 27.1 has one independently demonstrable outcome: one ratified lifecycle decision artifact that supplies an owned, deployable, and testable target. The accepted Dapr-only ADR, synchronized projections, and passing six-method guard class provide that outcome. The matrix, ownership, topology, retention, failure, privacy, capacity, and rollback clauses are facets of that single decision.

The split is deliberate and must remain visible:

- Story 27.1 decides and synchronizes the contract; no runtime lifecycle behavior is claimed.
- Story 27.2 is unblocked by ratification and implements the Dapr lifecycle service, fixed actor, clock service, component gate, bounded expiry/purge, failure behavior, observability, and privacy-safe payload path.
- Story 27.3 is unblocked by ratification and supplies Production-shaped portable and adapter-specific evidence, the runbook, and coordinated A41 close-out.
- Story 20.5 remains historical `done`; its emission and rate-limit behavior must not be undone.

Completion evidence for 27.1 is the accepted ADR backed by all-nine-operation capacity evidence, synchronized architecture/telemetry text, six passing decision guards, and explicit downstream handoffs. Runtime implementation, a deployment adapter, operations runbook, or A41 closure is a separate independently demonstrable outcome and remains outside this slice.

### Expected File Touches During Implementation

Required:

- `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` - canonical accepted Dapr-only decision and downstream contract.
- `_bmad-output/planning-artifacts/architecture.md` - replace the stale temporary gate with the selected target and ADR link.
- `docs/dev/telemetry.md` - reconcile current emission, proposed target, capacity/privacy drift, and assurance boundary.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs` - six structure-aware decision guards.
- `_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md` - Dev Agent Record, test evidence, completion notes, phase ledger, and cumulative File List.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - workflow status only.

Conditional:

- None expected. If a new file becomes necessary, justify it against the decision-only slice before editing.

Explicitly out of scope:

- Runtime source under `src/`.
- Server/AppHost application settings or project/package files.
- Kubernetes, Compose, AppHost, or any other container-service resource changes.
- `docs/operations/deployment-configuration.md` and operations runbooks.
- `_bmad-output/implementation-artifacts/deferred-work.md` status or A41 closure changes, except review-governed append-only deferred findings recorded in the cumulative File List.
- Provider, store, queue, TTL, purge, integration, or deployment-shaped test implementation.

### Project Structure Notes

- Keep the full decision in one `docs/dev` ADR, then project only the durable architecture statement into `architecture.md` and the current-versus-target operator explanation into `telemetry.md`.
- Use the existing `tests/.../Architecture` namespace for decision drift guards and `MarkdownContractDocument` for exact-section/table validation.
- Do not add a new solution file; the repository uses `Hexalith.Memories.slnx`.
- The current Production deployment authority remains Kustomize, but the accepted application contract is container-service neutral. Do not infer a required runtime topology from Kustomize or AppHost.
- Do not initialize nested submodules.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic-27 and #Story-27.1]
- [Source: _bmad-output/planning-artifacts/prd.md:429-450,929-935]
- [Source: _bmad-output/planning-artifacts/architecture.md:211-227]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-access-telemetry-retention-implementation.md]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Story-20.5-Deferred-Retention-Slice]
- [Source: _bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md]
- [Source: _bmad-output/implementation-artifacts/epic-20-retro-2026-07-04.md]
- [Source: _bmad-output/project-context.md]
- [Source: _bmad/custom/story-scope-guard.md]
- [Source: _bmad/custom/story-phase-ledger.md]
- [Source: src/Hexalith.Memories.ServiceDefaults/Extensions.cs]
- [Source: src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLog.cs]
- [Source: src/Hexalith.Memories.Contracts/V1/AccessTelemetryEvent.cs]
- [Source: deploy/kubernetes/base/server-deployment.yaml]
- [Source: docs/dev/telemetry.md]
- [Source: docs/operations/deployment-configuration.md]
- [Source: tests/Hexalith.Memories.Server.Tests/Architecture/ConsistencyModelDecisionTests.cs]
- [Source: tests/Hexalith.Memories.Server.Tests/Telemetry/Infrastructure/CapturingAuditLoggerProvider.cs]
- [Source: tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/AuditEventStreamReader.cs]
- [Source: global.json; Directory.Build.props; Directory.Packages.props]
- [Source: https://opentelemetry.io/docs/collector/deploy/gateway/]
- [Source: https://opentelemetry.io/docs/collector/resiliency/]
- [Source: https://kubernetes.io/docs/concepts/storage/persistent-volumes/]
- [Source: https://redis.io/docs/latest/commands/expire/]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-17: The dev-story continuation ratified the all-nine-operation capacity gate and incorporated most of a concurrent third-review update, but one High platform-neutral clock/Dapr finding remained open despite the then-current completion wording. That historical Redis/Kubernetes design, its diagnostic 6/0/0 focused result, and its +0-method continuation delta are retained as phase evidence only. The current code-review phase resolves the omitted finding by superseding that topology with the accepted Dapr-only contract and records fresh canonical evidence below.
- 2026-07-16: The second code-review pass applied all 11 accepted patches. It made Redis/Sentinel/PV topology executable; replaced the declarative-only clock check with a measured independent-UTC rollout/runtime gate covering Redis and Server writers; separated logical purge from lazy-free memory reclamation; defined first-connection configuration-invalid behavior, health precedence, TLS/network/storage privacy controls, marker overlap, idempotency conflicts, and provider-specific lifecycle filtering; expanded all six decision guards; and repaired the phase ledger. The pre-review and post-review diagnostic assemblies discovered the same Server 2,151, Architecture 24, Telemetry 141, and story-class 6 methods, so this pass added +0 methods and the cumulative story delta remains +6. The canonical build still stops at the pre-existing `NU1605`; the diagnostic build passed with 0 warnings/0 errors, the focused class passed 6/0/0 xUnit test cases, and the full diagnostic lane reported 2,697 xUnit test cases, 1 unrelated failure, and 1 skip. Exact commands and artifact hashes are in `Second Code-Review Verification Commands`.
- 2026-07-16: Code review resolved four design decisions and applied all 15 accepted patches. The ADR returned to `Proposed`; Stories 27.2/27.3 are blocked until all-nine-operation capacity evidence supports ratification. The review added millisecond `PXAT`, one-second Redis clock discipline, bounded 512-record/100-ms purge batches, separate 15-minute active purge and monitored 24-hour persistence-compaction bounds, startup degraded-mode behavior, marker-key rotation, stronger document guards, and monotonic status verification. The canonical Release build still stops at the pre-existing `NU1605`. The unchanged-pin diagnostic build completed with 0 warnings/0 errors, the focused decision class passed 6/0/0 xUnit test cases, and discovery remained Server 2,151, Architecture 24, Telemetry 141 methods, so the review-patch method delta is +0 and the cumulative story method delta remains +6. The full diagnostic Server assembly reported 2,697 xUnit test cases, 1 unrelated failure, and 1 skip in `ContractDocumentGuardTests.GetSection_LfAndCrLf_IncludesSubordinatesAndStopsAtPeerHeading`; the focused method reproduced that out-of-scope line-ending assertion failure. The exact build and discovery commands are recorded below.
- 2026-07-16: Task 6 reproduced the canonical isolated Release-build `NU1605` blocker with the exact story command: OTLP exporter 1.17.0 requires OpenTelemetry core >=1.17.0 while the repository pins 1.16.0. A clearly noncanonical diagnostic fallback restored and built the unchanged pinned graph with `NoWarn=NU1605;DAPR_CONVERSATION;EXTEXP0001;xUnit1051` (the latter three preserve existing project suppressions when the global fallback property replaces `NoWarn`) and completed with 0 warnings/0 errors. The current-source decision class passed 6/0/0 xUnit test cases; the seven named telemetry regression classes passed 80/0/0 xUnit test cases; the full Server assembly passed 2,697 xUnit test cases with 0 errors, 0 failures, and one existing skip. Final discovery remained Server 2,151, Architecture 24, Telemetry 141 methods. `git diff --check` passed. File List reconciliation used `git status --short -- docs/dev/adr-27.1-001-access-telemetry-lifecycle.md _bmad-output/planning-artifacts/architecture.md docs/dev/telemetry.md tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md`; all seven declared paths matched, while pre-existing same-file architecture/telemetry residual edits and unrelated sprint-status entries remain excluded.
- 2026-07-16: Task 5 added exactly six structure-aware xUnit facts over exact ADR/architecture/telemetry sections and tables plus the shared tool-markup anti-corruption guard. A controlled `Accepted` to `Proposed` mutation established RED (6 total, 5 failed); after restoring the ratified status and normalizing section whitespace, GREEN passed 6/0/0. Comparable no-restore Release discovery moved Server methods 2,145 -> 2,151 and Architecture methods 18 -> 24; Telemetry remained 141, for an actual Story 27.1 phase delta of +6 methods.
- 2026-07-16: Task 4 projected the accepted target into the existing dirty architecture and telemetry documents without overwriting their concurrent Epic 27 residual edits. The stale structured-file, five-operation, one-node, lossless-stdout, and operator-owned-retention assumptions are removed; current JSON/optional-OTLP behavior, current raw-value deviation, not-yet-implemented target, downstream handoffs, and open A41 boundary are explicit. No runtime source, settings, package, deployment resource, operations runbook, or deferred-work entry was changed by this story.
- 2026-07-16: Task 3 delivered the 15-section ADR contract, including `Capacity Evidence and Admission Envelope`; the then-selected Redis-specific values are historical phase evidence and are superseded by the current accepted Dapr-only decision without changing the required top-level section set.
- 2026-07-16: Task 2 evaluated OpenTelemetry Collector plus Grafana Loki, a dedicated Redis store, and file/volume storage against the same ownership, topology, durability, lifecycle, clock, failure, recovery, observability, privacy, capacity, and rollback fields. The proposed selection is the pinned Redis 7.4 workload in an independent primary/replica/Sentinel deployment; file storage fails the production hard gates, while Collector/Loki lacks a repository-owned deployable retention contract. Redis remains proposed until Task 3 ratifies the complete values.
- 2026-07-16: Dev-story preflight on baseline `HEAD` `119c0a49` reverified all nine `AccessTelemetryLog` success/error families, the typed `AccessTelemetryEvent` logger-state seam, JSON-console `ToString()` loss of `QueryParams`, current producers, two-replica/read-only-root/ephemeral-`/tmp` production topology, and the absence of an owned TTL, purge, persistent-buffer, lifecycle-health, or capacity policy. Raw `query`, `subject`, and `sourceUri` producer values remain a Story 27.2 privacy prerequisite. That frozen baseline used .NET SDK 10.0.301 and OpenTelemetry core 1.16.0; later on 2026-07-16 the working tree moved to SDK 10.0.302, and the current tree pins OpenTelemetry core 1.17.0. Existing dirty edits had already moved the architecture/telemetry text from an unqualified placeholder to an Epic 27 decision-first residual.
- 2026-07-16: Implementation plan: ratify the evidence-weighted dedicated Redis access-telemetry workload with a separate credential/resource boundary; project current-versus-target truth into architecture and telemetry docs; then add six structure-aware decision guards and validate through the repository fallback ladder without changing runtime code, packages, deployment resources, runbooks, or A41 status.
- 2026-07-16: Activated the customized `bmad-create-story` workflow, loaded all persistent project facts and full planning artifacts, and applied the historical-slice and phase-ledger fail-closed guards.
- 2026-07-16: Reverified current telemetry source, production topology, docs, tests, deferred A41 ledger, relevant prior-story evidence, package pins, git history, and official OpenTelemetry/Kubernetes/Redis guidance.
- 2026-07-16: An unprovenanced xUnit Release artifact listed 2,132 Server methods (`Architecture` 18; `Telemetry` 141), so it is retained as observational inventory only. Canonical current-source discovery is blocked by the pre-existing OpenTelemetry 1.16.0 versus OTLP exporter 1.17.0 `NU1605` mismatch and must be rerun after the owned dependency-pin correction.
- 2026-07-16: Create-story scope changed only this story record and the exact Epic 27/story status lines in `sprint-status.yaml`; unrelated dirty-worktree changes remain user-owned and excluded.

### Dev-Story Diagnostic Discovery Commands

The dev-story diagnostic lane used the same runner, assembly, scope, and
configuration before and after adding the six decision methods. The exact
diagnostic build command was:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -p:NoWarn=NU1605%3BDAPR_CONVERSATION%3BEXTEXP0001%3BxUnit1051
```

The pre-change and post-change discovery snapshots used the same command shape;
only the output file name identified the chronological snapshot:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods > /tmp/story-27-1-server-tests-pre.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.' /tmp/story-27-1-server-tests-pre.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.Architecture\.' /tmp/story-27-1-server-tests-pre.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.Telemetry\.' /tmp/story-27-1-server-tests-pre.txt

DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods > /tmp/story-27-1-server-tests-post.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.' /tmp/story-27-1-server-tests-post.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.Architecture\.' /tmp/story-27-1-server-tests-post.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.Telemetry\.' /tmp/story-27-1-server-tests-post.txt
```

The resulting comparable method totals were Server `2,145 -> 2,151`,
Architecture `18 -> 24`, and Telemetry `141 -> 141`. This is explicitly the
diagnostic suppressed-pin lane; it does not replace the canonical `NU1605`
blocker record.

### Second Code-Review Verification Commands

The pre-review existing Release assembly had SHA-256
`5e111081fbe25023f28b86362a91bf8953eac34b1b590e28b76945b8a0bf44ef`;
after the diagnostic rebuild and accepted patches it had SHA-256
`405d0fdcfce34f079d67d44767d26890d0b785f6e8ec9838ba34ac5a96e69209`.
Both chronological snapshots used this exact in-memory discovery command:

```bash
DISCOVERY="$(DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods)"
printf '%s\n' "$DISCOVERY" | rg -c '^Hexalith\.Memories\.Server\.Tests\.'
printf '%s\n' "$DISCOVERY" | rg -c '^Hexalith\.Memories\.Server\.Tests\.Architecture\.'
printf '%s\n' "$DISCOVERY" | rg -c '^Hexalith\.Memories\.Server\.Tests\.Telemetry\.'
printf '%s\n' "$DISCOVERY" | rg -c '^Hexalith\.Memories\.Server\.Tests\.Architecture\.AccessTelemetryRetentionDecisionTests\.'
```

Both snapshots returned Server `2,151`, Architecture `24`, Telemetry `141`,
and `AccessTelemetryRetentionDecisionTests` `6` methods: review-patch delta
`+0`, cumulative story delta `+6`, and external same-lane delta `+0`.

The canonical build was rerun exactly and stopped at the known `NU1605`:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

The unchanged-pin diagnostic build completed with 0 warnings and 0 errors:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -p:NoWarn=NU1605%3BDAPR_CONVERSATION%3BEXTEXP0001%3BxUnit1051
```

The focused decision class passed 6/0/0 xUnit test cases:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -parallel none -noLogo
```

The full diagnostic Server lane used the same assembly and returned 2,697
xUnit test cases, 1 unrelated failure, and 1 skip:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -noLogo
```

The exact focused reproduction also failed 1/1 in the concurrent
contract-document line-ending guard, outside Story 27.1:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -method Hexalith.Memories.Server.Tests.Documentation.ContractDocumentGuardTests.GetSection_LfAndCrLf_IncludesSubordinatesAndStopsAtPeerHeading -parallel none -noLogo
```

### Capacity-Ratification and Third-Review Verification Commands

The chronological pre-change diagnostic assembly had SHA-256
`c4f1b27b10653e281990ad9849aaf1de2c072fe48e27144646068d978a6fcfd6`;
the final assembly had SHA-256
`0754e478281a821a5ae664742a35556cfca240aabe2732dfe6f92f4e8b28074c`.
Both snapshots used the exact four-filter discovery command from `Second
Code-Review Verification Commands` and returned Server `2,151`, Architecture
`24`, Telemetry `141`, and story class `6` methods. This continuation therefore
added `+0` methods; the cumulative story delta remains `+6`, and the external
same-lane delta is `+0`.

The unchanged-pin diagnostic build passed with 0 warnings and 0 errors:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -p:NoWarn=NU1605%3BDAPR_CONVERSATION%3BEXTEXP0001%3BxUnit1051
```

The final focused decision run passed 6/0/0 xUnit test cases:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -parallel none -noLogo -reporter quiet -xml /tmp/story-27-1-focused-final2-20260717.xml
```

The eight named existing emission/schema/metrics/registration regression
classes passed 118/0/0 xUnit test cases. Each class used this exact command
shape with its fully qualified class name and a distinct XML path:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Telemetry.<ClassName> -parallel none -noLogo -reporter quiet -xml /tmp/story-27-1-<ClassName>-20260717.xml
```

`<ClassName>` was `AccessTelemetryEventSchemaTests`, `AccessTelemetryLogTests`,
`AuditEventSchemaVersioningTests`, `AuditLogStreamTests`,
`EndpointTelemetryScopeTests`, `MemoriesMetricsTests`,
`MutationAuditLogStreamTests`, and `OpenTelemetryRegistrationTests`.

The unfiltered broad run still reproduced the pre-existing, out-of-scope CRLF
failure recorded above. The final fallback excluded only that exact method and
passed 2,695/0/1 of 2,696 xUnit test cases:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -method- Hexalith.Memories.Server.Tests.Documentation.ContractDocumentGuardTests.GetSection_LfAndCrLf_IncludesSubordinatesAndStopsAtPeerHeading -parallel none -noLogo -reporter quiet -xml /tmp/story-27-1-server-excluding-known-final-20260717.xml
```

The final canonical build was rerun without suppression and stopped at the
unchanged `NU1605`: imported OTLP exporter 1.17.0 requires OpenTelemetry core
at least 1.17.0 while the repository pins core 1.16.0.

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

The cumulative File List used baseline `119c0a49` and this exact path-scoped
reconciliation command:

```bash
git diff --name-status 119c0a49 -- docs/dev/adr-27.1-001-access-telemetry-lifecycle.md _bmad-output/planning-artifacts/architecture.md docs/dev/telemetry.md tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md _bmad-output/implementation-artifacts/deferred-work.md
```

It matched 8/8. The five dirty root-declared submodule checkouts
`references/Hexalith.Builds`, `references/Hexalith.EventStore`,
`references/Hexalith.FrontComposer`,
`references/Hexalith.PolymorphicSerializations`, and `references/Hexalith.Tenants`
are user-owned exclusions. All other baseline-to-current paths outside the
eight-path scope belong to later committed or concurrent work and are excluded;
no conditional Story 27.1 implementation file was added.

### Fourth Code-Review Verification Commands

The current review reran the exact unsuppressed canonical Release build after
the OpenTelemetry pin moved to 1.17.0:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

The old `NU1605` did not recur. Restore instead failed with three `NU1603`
errors because `Hexalith.EventStore.Client 1.72.3` was unavailable and NuGet
resolved incompatible `2.0.0`. The EventStore dependency/package-feed lane
owns that blocker. Its consequence is that this review cannot claim a fresh
canonical full dependency build. Reopen the canonical gate when the exact
1.72.3 package is available again or that owning lane intentionally updates
the package/API contract.

After a warning-suppressed diagnostic restore, compiling project references
confirmed the resolved 2.0.0 package is API-incompatible (`Gateway` and
`IEventStoreGatewayClient` are absent). To verify only the documentation/test
changes without disguising that source blocker, the review compiled the test
project against the previously built project-reference outputs:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-restore --disable-build-servers -m:1 /nr:false -p:BuildProjectReferences=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors= -p:NoWarn=DAPR_CONVERSATION%3BEXTEXP0001%3BxUnit1051%3BNU1603
```

That diagnostic test-project build passed with 0 warnings and 0 errors. The
focused current decision guards passed 6/0/0 xUnit test cases:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -parallel none -noLogo
```

The exact four-filter discovery remained Server `2,151`, Architecture `24`,
Telemetry `141`, and story class `6`. The current DLL SHA-256 is
`3e921113073db52ef5c2f0350b6a033fdc72e807f23841345ff653b4d7109f15`;
the discovery output SHA-256 is
`bcfe06a1d7c138e3ae02cf4b26f45339e472767d5ad051f7294ebdd5b83fe4d6`.
The fourth-review phase delta is `+0` methods, the cumulative Story 27.1 delta
remains `+6`, and the external same-lane delta is `+0`.

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods > /tmp/story-27-1-dapr-review-methods.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.' /tmp/story-27-1-dapr-review-methods.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.Architecture\.' /tmp/story-27-1-dapr-review-methods.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.Telemetry\.' /tmp/story-27-1-dapr-review-methods.txt
rg -c '^Hexalith\.Memories\.Server\.Tests\.Architecture\.AccessTelemetryRetentionDecisionTests\.' /tmp/story-27-1-dapr-review-methods.txt
sha256sum tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll /tmp/story-27-1-dapr-review-methods.txt
```

The current review owns exactly the eight cumulative Story 27.1 paths in the
File List. In `deferred-work.md`, only the two entries under "Deferred from:
code review of 27-1..." are Story 27.1 review additions; the A41 rewrite and
three unrelated-spec entries from committed sync `8bb0708a` are same-file
exclusions. In `sprint-status.yaml`, this review owns only Story 27.1's
`review` to `done` transition; the Epic 28 registration and Epic 18/19 action
changes are concurrent committed exclusions. Dirty root submodule checkouts
remain user-owned. The review-governed append-only deferred entries are the
authorized deviation from the story's original no-ledger-touch declaration.

### Completion Notes List

- Current code review complete: Administrator's Dapr-only disposition replaces the historical Redis/Kubernetes topology; architecture and operator telemetry projections are synchronized; Stories 27.2/27.3 remain unblocked; A41 remains open. The fixed actor, Dapr-invoked clock, component capability/evidence gate, dynamic rotation membership, portable purge, and adapter-owned physical reclamation resolve the previously omitted High finding and its superseded Redis-specific consequences.
- Final current evidence: the unsuppressed canonical Release restore is blocked by unrelated `NU1603` EventStore package-feed drift; the scoped diagnostic test-project build passed 0 warnings/0 errors; focused decision guards passed 6/0/0; comparable discovery remains six story-class methods (`+0` review, `+6` cumulative). Scoped diff check and eight-path File List reconciliation are recorded in the final code-review ledger row.
- Historical second-review checkpoint: all 11 then-accepted patches were applied without adding test methods; its `Proposed`/blocked state and `NU1605` evidence describe that phase only and are superseded by the current accepted Dapr decision and current `NU1603` canonical blocker record.
- Historical first-review verification: the six decision guards passed 6/0/0 and the full diagnostic Server lane reported 2,697 xUnit test cases, 1 unrelated failure, and 1 skip in `ContractDocumentGuardTests.GetSection_LfAndCrLf_IncludesSubordinatesAndStopsAtPeerHeading`; this remains phase evidence, not the current build claim.
- Task 5 complete: six review-hardened decision-only drift guards passed 6/0/0 and increased the comparable Server/Architecture method inventory by exactly six without adding provider, Redis, TTL, queue, purge, multi-writer, restart, or cross-tenant runtime behavior.
- Historical Task 4 checkpoint: architecture and telemetry distinguished shipped emission from the then-proposed target, linked the ADR, and kept A41 open.
- Historical Tasks 1-3 checkpoint: the dedicated-Redis proposal was returned to `Proposed`; that topology is superseded by the accepted Dapr-only decision.
- Story 27.1 created as a strict decision-first architecture/operator slice with one canonical ADR target and six planned structure-aware guards.
- Current source, two-replica production topology, privacy deviation, A41 boundary, package-build blocker, current technical guidance, history classification, and downstream story split are captured.
- No production code, dependency, application setting, deployment artifact, runbook, or A41/deferred-ledger status was changed during story creation.

### File List

- `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `docs/dev/telemetry.md`
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs`
- `_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-16 | create-story | Created Story 27.1; moved Epic 27 from `backlog` to `in-progress` and Story 27.1 from `backlog` to `ready-for-dev`; no implementation performed. | Actual +0; cumulative +0; planned +6 decision-guard methods. Canonical discovery blocked by the owned `NU1605` pin mismatch; the exact command, owner, consequence, and reopen trigger are in Test Baseline. Noncanonical observation only: 2,132 methods (`Architecture` 18; `Telemetry` 141); do not use it for delta. | matched 3/3 against the create-story scope manifest and the declared pre-create snapshot; exact owned-line diff, name-status set, hashes, verification commands, and same-file exclusions are recorded in `_bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md`. |
| 2026-07-16 | dev-story | Accepted ADR 27.1-001, synchronized architecture/operator telemetry truth, added six structure-aware decision guards, completed validation, and moved Story 27.1 to `review`; no runtime lifecycle implementation or A41 closure was claimed. | Actual +6 methods; cumulative +6 methods. Comparable current-source diagnostic discovery: Server 2,145 -> 2,151 methods, Architecture 18 -> 24 methods, Telemetry 141 -> 141 methods; external same-lane delta +0 methods. Exact build and discovery commands: `Dev-Story Diagnostic Discovery Commands`. Canonical fresh discovery remains blocked by the exact pre-existing `NU1605`; the older 2,132-method observation is unprovenanced and excluded. | matched 7/7 against the cumulative File List using the exact scoped `git status --short --` command in Task 6 evidence and the create-story scope manifest; pre-existing same-file architecture/telemetry residual edits and unrelated sprint-status entries are named exclusions; no conditional file was added. |
| 2026-07-16 | code-review | Resolved four design decisions and applied all 15 accepted patches; returned ADR 27.1-001 and the story to `Proposed`/`in-progress`, with Stories 27.2/27.3 blocked until all-nine-operation capacity evidence supports ratification. | A phase-specific pre-review discovery snapshot and assembly hash were not captured before that review edited the artifacts, so its claimed review-patch `+0` and external same-lane `+0` deltas are unavailable evidence and are not independently proven. The Story 27.1 phase-ledger owner owns this unavailable-evidence record; it cannot be reconstructed from the later working tree. The post-review diagnostic snapshot was Server 2,151, Architecture 24, Telemetry 141 methods; the cumulative story `+6` remains supported by the comparable dev-story 2,145 -> 2,151 discovery in `Dev-Story Diagnostic Discovery Commands`. Reopen only if a chronological pre-review assembly/hash is recovered; rerun the same four discovery filters and replace this blocker. Canonical Release build was blocked by the pre-existing `NU1605`; diagnostic build passed 0 warnings/0 errors and the focused class passed 6/0/0 xUnit test cases. Full diagnostic Server lane: 2,697 xUnit test cases, 1 unrelated failure, 1 skip; rerun after `ContractDocumentGuardTests.GetSection_LfAndCrLf_IncludesSubordinatesAndStopsAtPeerHeading` is repaired by its owning lane. | matched 7/7 against baseline `119c0a49` with the scoped `git diff --name-status`; the user confirmed this complete single Story 27.1 chunk, and no conditional file was added. |
| 2026-07-16 | code-review | Applied all 11 accepted second-pass patches across executable topology, absolute clock gating, lazy-free purge evidence, delayed configuration validation, health precedence, transport/network/storage privacy, marker overlap, retry conflict handling, lifecycle-provider filtering, decision guards, and ledger evidence. ADR 27.1-001 remains `Proposed` and the story remains `in-progress` pending the unchanged all-nine-operation capacity gate. | Review-patch delta +0 methods; cumulative story delta +6 methods; external same-lane delta +0 methods. Pre/post discovery remained Server 2,151, Architecture 24, Telemetry 141, and story class 6 methods. Exact commands: `Second Code-Review Verification Commands`. Canonical build blocked at the known `NU1605`; diagnostic build passed 0 warnings/0 errors; focused class passed 6/0/0 xUnit test cases; full lane reported 2,697 xUnit test cases, 1 unrelated failure, and 1 skip, with the exact failing method recorded in that command section. | matched 7/7 against baseline `119c0a49` with exact scoped `git diff --name-status`; scoped `git diff --check` passed; the user confirmed the complete Story 27.1 chunk, and no conditional file was added. |
| 2026-07-17 | dev-story | Ratified the all-nine-operation capacity envelope; accepted ADR 27.1-001; synchronized architecture/telemetry; resolved the concurrent third-review findings except one High platform-neutral clock/Dapr patch that remained open despite the then-current completion wording; moved Story 27.1 to `review`. The raw-console/OTLP privacy finding remained explicitly deferred and did not claim A41 closure. | Continuation delta +0 methods; cumulative story delta +6 methods; external same-lane delta +0. Comparable pre/post diagnostic discovery stayed Server 2,151, Architecture 24, Telemetry 141, story class 6. Exact historical commands/hashes: `Capacity-Ratification and Third-Review Verification Commands`. Diagnostic build passed 0 warnings/0 errors; focused 6/0/0; named regressions 118/0/0; broad fallback 2,695/0/1 of 2,696. The then-current canonical build was blocked by `NU1605`. | matched 8/8 against baseline `119c0a49` with the exact scoped `git diff --name-status`; the added deferred ledger is part of the interleaved review scope. Five user-owned root submodule checkouts and every other path outside the exact eight-path scope are named exclusions; scoped `git diff --check` passed; no conditional file was added. |
| 2026-07-17 | code-review | Dispositioned the previously open High platform-neutral finding and applied every approved patch: replaced the Redis/Kubernetes topology with a container-service-neutral Dapr-only lifecycle service, fixed-ID actor, independent clock app, dynamic writer rotation, portable logical purge, and component capability/durability/capacity/physical-evidence gate; synchronized projections and guards; reconciled historical scope/ledger gaps; moved Story 27.1 to `done`. The third-review phase was interleaved with dev-story and had no standalone pre-snapshot, so its missing attribution is recorded here rather than fabricated. | Review delta +0 methods; cumulative story delta +6; external same-lane delta +0. Current discovery: Server 2,151, Architecture 24, Telemetry 141, story class 6; DLL/discovery hashes and exact commands are in `Fourth Code-Review Verification Commands`. Unsuppressed canonical restore is blocked by unrelated `NU1603` because EventStore.Client 1.72.3 is unavailable and incompatible 2.0.0 resolves; owner, consequence, and reopen trigger are recorded there. Scoped diagnostic test-project build passed 0 warnings/0 errors; focused class passed 6/0/0. | matched 8/8 against baseline `119c0a49` with the exact scoped name-status set; this review owns the story's sprint-status transition and two append-only review deferrals, while committed `8bb0708a` same-file ledger content, Epic 28/Epic 18/19 sprint-status changes, dirty root submodules, and all paths outside the eight-path scope are explicit exclusions; scoped `git diff --check` passed. |
