---
baseline_commit: 119c0a49
creation_sprint_status_sha256: a0a85ef2a7dfd22493b677976e4eb9bd14f37b668ab03bdbe4237bd6c0bd4633
creation_scope_evidence: _bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md
---

# Story 27.1: Access Telemetry Retention Ownership Decision (Decision-First)

Status: review

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
  - [x] Record the evidence-weighted recommendation—currently a dedicated Redis access-telemetry workload with atomic TTL assignment—as a recommendation until the owner, default/range, privacy policy, durability boundary, and operating cost are ratified. Do not present it as the decision merely because Redis is already used elsewhere.
  - [x] If no candidate satisfies every hard gate, leave the ADR proposed, mark this story blocked during implementation, and do not start Stories 27.2 or 27.3.

- [x] Task 3 - Ratify the complete lifecycle contract in one canonical ADR (AC: 1, 2, 3)
  - [x] Create `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` with status, date, approver, architecture owner, operational lifecycle owner, affected deployment, selected family/technology, rejected alternatives, and explicit implementation gate.
  - [x] Define the writer-to-sink topology, separate resource/credential boundary, two-replica key or record uniqueness, concurrency semantics, acknowledgement point, persistence mode, restart/rescheduling behavior, and exact acknowledged loss window.
  - [x] Ratify a production retention default and allowed minimum/maximum with units, capacity rationale, and configuration ownership. Missing, malformed, below-minimum, above-maximum, or silently unbounded values must have explicit startup behavior.
  - [x] Define whether age begins at event emission or backend acceptance, the authoritative UTC clock, late-arrival and clock-skew treatment, logical expiry, physical reclamation, purge cadence/grace, and preservation of newer records.
  - [x] Define bounded queue capacity, retry/backoff and maximum age, shutdown flush limit, overflow/drop reasons, outage behavior, recovery/drain, disk/space exhaustion behavior, and whether invalid configuration fails startup. No telemetry-provider exception may escape into a business request.
  - [x] Define low-cardinality accepted/rejected/enqueued/persisted/retried/failed/dropped/expired/purged and health signals, their owner and `NoData` meaning, and capacity thresholds. Tenant, user, case, unit, query, or source values must not become metric labels.
  - [x] Define the persisted privacy schema and operational inspection boundary. Apply least privilege with separate write, lifecycle/purge, and inspection authorities appropriate to the selected family; the Server's authority must not include inspection or arbitrary record deletion. No tenant-facing read API is introduced. Require sanitization or bucketing of raw query and source values before persistent storage.
  - [x] Define rollback so existing JSON console/optional OTLP emission remains intact, retained records continue expiring, persistent storage is not deleted automatically, and a rollback to the old image is explicitly degraded rather than an acceptable steady state.
  - [x] State the assurance boundary verbatim: bounded infrastructure telemetry only; no tamper evidence, append-only integrity, legal compliance, or certified audit retention.

- [x] Task 4 - Project the ratified decision without pretending it is implemented (AC: 1, 2, 3)
  - [x] Update `_bmad-output/planning-artifacts/architecture.md` to replace the stale “MVP: structured log file” placeholder with the selected, ratified lifecycle target and a link to the canonical ADR.
  - [x] Update `docs/dev/telemetry.md` to distinguish current JSON-console/optional-OTLP emission from the ratified target, link the ADR, remove stale five-operation/one-node assumptions, and identify any current privacy deviation without claiming it is already fixed.
  - [x] Keep `20.5-A41-ACCESS-TELEMETRY-RETENTION` `carried-forward` and its matching action `open`. Do not edit the deferred ledger merely to imply progress; Story 27.3 owns evidence-backed closure coordination.
  - [x] Do not change production source, application settings, package references, Kubernetes/AppHost resources, deployment configuration tables, or operations runbooks in this decision story. Those belong to Stories 27.2 and 27.3.
  - [x] Record an exact Story 27.2 implementation map and Story 27.3 production-shaped verification map in the ADR, including two writers, restart/rescheduling, deterministic age bounds, purge, newer-record preservation, emission continuity, failure modes, and tenant/privacy negatives.

- [x] Task 5 - Add executable decision-drift guards (AC: 1, 2, 3)
  - [x] Add `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs` using the existing repository-root and Markdown contract-document seams.
  - [x] Add six focused facts: exactly one family is ratified and all three are evaluated; every required lifecycle field is present; retention is bounded and has no silent unbounded fallback; production two-replica/read-only-root facts and file hard gates remain explicit; architecture/telemetry/ADR are cross-linked and do not retain the stale structured-log-file claim; assurance/A41/privacy/downstream-story boundaries and the accepted/rejected/enqueued/persisted/retried/failed/dropped/expired/purged signal set remain explicit.
  - [x] Make guards structure-aware by reading exact headings/table rows. Avoid brittle whole-document substring counts or accepting required vocabulary from unrelated sections.
  - [x] Keep these as decision-document guards only. Provider, TTL, queue, Redis integration, multi-writer, restart, purge, and cross-tenant runtime tests belong to Stories 27.2 and 27.3.

- [x] Task 6 - Validate the decision-only completion (AC: 1, 2, 3)
  - [x] Run `git diff --check -- docs/dev/adr-27.1-001-access-telemetry-lifecycle.md _bmad-output/planning-artifacts/architecture.md docs/dev/telemetry.md tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md _bmad-output/implementation-artifacts/sprint-status.yaml`.
  - [x] Build `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` with the repository-pinned SDK, warnings as errors, Release configuration, and build-server isolation; then run the new decision test class directly with the xUnit v3 executable.
  - [x] Re-run the existing access-event schema, audit-emission, OpenTelemetry-registration, and metric-policy classes. These prove no regression in existing emission; they do not prove lifecycle implementation.
  - [x] If the current OpenTelemetry 1.16.0/OTLP exporter 1.17.0 downgrade error persists, report it as a pre-existing build blocker. Do not turn Story 27.1 into a package-upgrade story or claim a fresh-build result from an older binary.
  - [x] Reconcile the File List and the canonical phase ledger before moving the story beyond `in-progress`; no planned test count may be reported as actual.

## Dev Notes

Story 27.1 is the architecture/operator decision gate for Epic 27. Its one deliverable is a ratified, owned, deployable, and testable lifecycle contract. Story 27.2 implements that contract. Story 27.3 supplies production-shaped expiry/purge evidence, the operations runbook, and coordinated A41 close-out. Completing or scheduling 27.1 alone cannot close the residual. [Source: _bmad-output/planning-artifacts/epics.md#Story-27.1; _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-access-telemetry-retention-implementation.md]

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
| Dedicated write-only store | Strong repository ownership and directly executable TTL/purge semantics; must be separate from domain data. | Separate workload/credentials, atomic write-plus-expiry, server clock, bounded queue/retry, durable acknowledgement, restart behavior, read restriction, capacity, purge evidence, and operator ownership. | Adds an operational datastore and must not reuse domain Redis or persist current raw sensitive values. |
| File/volume | Simple format but poor fit for the committed production shape. | Durable shared or per-replica volume, safe concurrent rotation, stable pod identity/rescheduling, read-only-root compatibility, disk-pressure behavior, executable purge, capacity, recovery, and inspection controls. | Current Server has only ephemeral `/tmp`; shared-file concurrency and lifecycle ownership are unresolved. |

Current evidence-weighted recommendation: evaluate a dedicated Redis access-telemetry workload first, using unique records with atomic TTL assignment and Redis server time. It must be a separate deployment and credential boundary, not the existing domain Redis. A Redis acknowledgement plus the selected persistence mode defines durability; it does not imply no loss. This is guidance for the decision, not a pre-ratified outcome. If a named external backend provides a stronger owned contract, the ADR may select it with equally executable evidence.

### Required ADR Structure

The canonical ADR must contain exactly one instance of each top-level contract section so structure-aware guards can validate it:

1. Status and Decision Metadata
2. Verified Current State
3. Options Evaluated
4. Selected Design and Rejected Alternatives
5. Ownership and Topology
6. Multi-Replica Write and Durability Boundary
7. Retention, Expiry, Purge, and Clock
8. Failure, Backpressure, Recovery, and Capacity
9. Observability
10. Privacy and Tenant Boundary
11. Rollback and Transition
12. Assurance Boundary
13. Story 27.2 Implementation Handoff
14. Story 27.3 Verification and Operations Handoff

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

- The repository uses .NET SDK 10.0.302, `net10.0`, C# 14, warnings as errors, Aspire AppHost SDK 13.4.6, Dapr 1.18.4, OpenTelemetry core 1.16.0, and StackExchange.Redis instrumentation 1.16.0-beta.1. The shared build props currently resolve the OTLP exporter to 1.17.0, producing a downgrade error against the local 1.16.0 core pin; that mismatch is pre-existing scope, not a reason to broaden this decision story. [Source: global.json; Directory.Build.props; Directory.Packages.props; src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj; references/Hexalith.Builds/Props/Directory.Packages.props]
- Official OpenTelemetry guidance treats an OTLP gateway as a separately operated service and explicitly documents in-memory queue overflow, retry expiry, collector-crash loss without persistent storage, WAL disk failure, and the need to monitor queue capacity. An endpoint alone is therefore not a durability or retention policy. [Source: https://opentelemetry.io/docs/collector/deploy/gateway/; https://opentelemetry.io/docs/collector/resiliency/]
- Kubernetes persistent volumes have a lifecycle independent of individual Pods, while the current `emptyDir` does not satisfy retained storage across replacement. Any selected volume design must declare access mode, reclaim behavior, permissions, capacity, and pod-rescheduling semantics. [Source: https://kubernetes.io/docs/concepts/storage/persistent-volumes/; https://kubernetes.io/docs/tasks/configure-pod-container/security-context/]
- Redis `EXPIRE` provides key TTL controlled by Redis time; Redis Streams `XTRIM` can delete by stream ID but is not itself a per-record TTL guarantee. If Redis is selected, the ADR must choose one executable expiry model and define physical-reclamation evidence rather than mixing the two. [Source: https://redis.io/docs/latest/commands/expire/; https://redis.io/docs/latest/commands/xtrim/]
- Do not upgrade the SDK, Aspire, Dapr, OpenTelemetry, Redis libraries, collector, or backend in this story. A selected new runtime component and version become Story 27.2 inputs after ratification.

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

- **Canonical discovery status:** blocked; do not subtract a later count from the 2,132 observational inventory.
- **Blocker owner:** the shared `Hexalith.Builds` dependency-pin maintainer together with the Memories build maintainer.
- **Consequence:** current source cannot produce a trusted test-count baseline or verify the six new decision guards; no fresh-build or actual test-delta claim is permitted.
- **Measurable reopen trigger:** the shared OTLP exporter and local OpenTelemetry core pins resolve compatibly and the exact Release build above exits `0`; before adding tests, rerun the exact `-list methods` commands and record that fresh count as the canonical pre-change baseline.

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

Story 27.1 has one independently demonstrable outcome: one ratified lifecycle decision artifact that supplies an owned, deployable, and testable target. The matrix, ownership, topology, retention, failure, privacy, capacity, and rollback clauses are facets of that single decision.

The split is deliberate and must remain visible:

- Story 27.1 decides and synchronizes the contract; no runtime lifecycle behavior is claimed.
- Story 27.2 implements the selected sink/store, configuration validation, bounded expiry/purge, failure behavior, observability, and privacy-safe payload path.
- Story 27.3 supplies production-shaped evidence, the runbook, and coordinated A41 close-out.
- Story 20.5 remains historical `done`; its emission and rate-limit behavior must not be undone.

Completion evidence for 27.1 is the accepted ADR, synchronized architecture/telemetry text, six passing decision guards, and an explicit downstream handoff. A sink implementation, broad deployment package, operations runbook, or A41 closure would be a separate independently demonstrable outcome and is therefore out of this slice.

### Expected File Touches During Implementation

Required:

- `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` - canonical ratified decision and downstream contract.
- `_bmad-output/planning-artifacts/architecture.md` - replace the stale temporary gate with the selected target and ADR link.
- `docs/dev/telemetry.md` - reconcile current emission, ratified target, capacity/privacy drift, and assurance boundary.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs` - six structure-aware decision guards.
- `_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md` - Dev Agent Record, test evidence, completion notes, phase ledger, and cumulative File List.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - workflow status only.

Conditional:

- None expected. If a new file becomes necessary, justify it against the decision-only slice before editing.

Explicitly out of scope:

- Runtime source under `src/`.
- Server/AppHost application settings or project/package files.
- Kubernetes, Compose, or AppHost resource changes.
- `docs/operations/deployment-configuration.md` and operations runbooks.
- `_bmad-output/implementation-artifacts/deferred-work.md` status or A41 closure changes.
- Provider, store, queue, TTL, purge, integration, or deployment-shaped test implementation.

### Project Structure Notes

- Keep the full decision in one `docs/dev` ADR, then project only the durable architecture statement into `architecture.md` and the current-versus-target operator explanation into `telemetry.md`.
- Use the existing `tests/.../Architecture` namespace for decision drift guards and `MarkdownContractDocument` for exact-section/table validation.
- Do not add a new solution file; the repository uses `Hexalith.Memories.slnx`.
- Production deployment authority remains Kustomize. Do not infer a production topology from AppHost.
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

- 2026-07-16: Task 6 reproduced the canonical isolated Release-build `NU1605` blocker with the exact story command: OTLP exporter 1.17.0 requires OpenTelemetry core >=1.17.0 while the repository pins 1.16.0. A clearly noncanonical diagnostic fallback restored and built the unchanged pinned graph with `NoWarn=NU1605;DAPR_CONVERSATION;EXTEXP0001;xUnit1051` (the latter three preserve existing project suppressions when the global fallback property replaces `NoWarn`) and completed with 0 warnings/0 errors. The current-source decision class passed 6/0/0; the seven named telemetry regression classes passed 80/0/0; the full Server assembly passed 2,697 total with 0 errors, 0 failures, and one existing skip. Final discovery remained Server 2,151, Architecture 24, Telemetry 141. `git diff --check` passed. File List reconciliation used `git status --short -- docs/dev/adr-27.1-001-access-telemetry-lifecycle.md _bmad-output/planning-artifacts/architecture.md docs/dev/telemetry.md tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md`; all seven declared paths matched, while pre-existing same-file architecture/telemetry residual edits and unrelated sprint-status entries remain excluded.
- 2026-07-16: Task 5 added exactly six structure-aware xUnit facts over exact ADR/architecture/telemetry sections and tables plus the shared tool-markup anti-corruption guard. A controlled `Accepted` to `Proposed` mutation established RED (6 total, 5 failed); after restoring the ratified status and normalizing section whitespace, GREEN passed 6/0/0. Comparable no-restore Release discovery moved Server methods 2,145 -> 2,151 and Architecture methods 18 -> 24; Telemetry remained 141, for an actual Story 27.1 phase delta of +6 methods.
- 2026-07-16: Task 4 projected the accepted target into the existing dirty architecture and telemetry documents without overwriting their concurrent Epic 27 residual edits. The stale structured-file, five-operation, one-node, lossless-stdout, and operator-owned-retention assumptions are removed; current JSON/optional-OTLP behavior, current raw-value deviation, not-yet-implemented target, downstream handoffs, and open A41 boundary are explicit. No runtime source, settings, package, deployment resource, operations runbook, or deferred-work entry was changed by this story.
- 2026-07-16: Task 3 ratified ADR 27.1-001 with 14 canonical sections, a 24-hour Production default (1 hour-7 days), Redis `TIME`/`EXAT`, five-minute sweep and 15-minute purge grace, two-PVC `WAITAOF` durability boundary, explicit simultaneous-storage loss boundary, 8,192-record/64-MiB queue, five-minute retry age, five-second shutdown flush, low-cardinality lifecycle signals, separate writer/lifecycle/inspector authorities, HMAC markers and raw-value bucketing, capacity admission, rollback, and the required bounded-infrastructure-telemetry assurance statement.
- 2026-07-16: Task 2 evaluated OpenTelemetry Collector plus Grafana Loki, a dedicated Redis store, and file/volume storage against the same ownership, topology, durability, lifecycle, clock, failure, recovery, observability, privacy, capacity, and rollback fields. The proposed selection is the pinned Redis 7.4 workload in an independent primary/replica/Sentinel deployment; file storage fails the production hard gates, while Collector/Loki lacks a repository-owned deployable retention contract. Redis remains proposed until Task 3 ratifies the complete values.
- 2026-07-16: Dev-story preflight on `HEAD` `119c0a49` reverified all nine `AccessTelemetryLog` success/error families, the typed `AccessTelemetryEvent` logger-state seam, JSON-console `ToString()` loss of `QueryParams`, current producers, two-replica/read-only-root/ephemeral-`/tmp` production topology, and the absence of an owned TTL, purge, persistent-buffer, lifecycle-health, or capacity policy. Raw `query`, `subject`, and `sourceUri` producer values remain a Story 27.2 privacy prerequisite. Current pins remain .NET SDK 10.0.302, Aspire AppHost SDK 13.4.6, Dapr 1.18.4, local OpenTelemetry 1.16.0, imported OTLP exporter/hosting 1.17.0, and StackExchange.Redis 3.0.17; the exact canonical Release build remains blocked by `NU1605`. Existing dirty edits had already moved the architecture/telemetry text from an unqualified placeholder to an Epic 27 decision-first residual; implementation will preserve those concurrent edits while replacing only the stale structured-file target with the ratified ADR projection.
- 2026-07-16: Implementation plan: ratify the evidence-weighted dedicated Redis access-telemetry workload with a separate credential/resource boundary; project current-versus-target truth into architecture and telemetry docs; then add six structure-aware decision guards and validate through the repository fallback ladder without changing runtime code, packages, deployment resources, runbooks, or A41 status.
- 2026-07-16: Activated the customized `bmad-create-story` workflow, loaded all persistent project facts and full planning artifacts, and applied the historical-slice and phase-ledger fail-closed guards.
- 2026-07-16: Reverified current telemetry source, production topology, docs, tests, deferred A41 ledger, relevant prior-story evidence, package pins, git history, and official OpenTelemetry/Kubernetes/Redis guidance.
- 2026-07-16: An unprovenanced xUnit Release artifact listed 2,132 Server methods (`Architecture` 18; `Telemetry` 141), so it is retained as observational inventory only. Canonical current-source discovery is blocked by the pre-existing OpenTelemetry 1.16.0 versus OTLP exporter 1.17.0 `NU1605` mismatch and must be rerun after the owned dependency-pin correction.
- 2026-07-16: Create-story scope changed only this story record and the exact Epic 27/story status lines in `sprint-status.yaml`; unrelated dirty-worktree changes remain user-owned and excluded.

### Completion Notes List

- Task 6 complete: canonical fresh build remains blocked by the pre-existing OpenTelemetry `NU1605`; the explicit unchanged-pin diagnostic fallback built cleanly, the 6 decision guards, 80 telemetry regressions, and 2,697-test full Server assembly are green, `git diff --check` passed, and the cumulative File List reconciles 7/7.
- Task 5 complete: six decision-only drift guards passed 6/0/0 and increased the comparable Server/Architecture method inventory by exactly six without adding provider, Redis, TTL, queue, purge, multi-writer, restart, or cross-tenant runtime behavior.
- Task 4 complete: architecture and telemetry now distinguish shipped emission from the accepted target, link the canonical ADR, keep A41 open, and hand Stories 27.2/27.3 exact implementation and Production-verification obligations.
- Tasks 1-3 complete: current state and producer privacy were reverified, all three permitted families were evaluated, and the dedicated Redis lifecycle contract was accepted with concrete ownership, bounds, failure semantics, privacy controls, capacity, and downstream gates.
- Ultimate context engine analysis completed - comprehensive developer guide created.
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

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-16 | create-story | Created Story 27.1; moved Epic 27 from `backlog` to `in-progress` and Story 27.1 from `backlog` to `ready-for-dev`; no implementation performed. | Actual +0; cumulative +0; planned +6 decision-guard methods. Canonical discovery blocked by the owned `NU1605` pin mismatch; the exact command, owner, consequence, and reopen trigger are in Test Baseline. Noncanonical observation only: 2,132 methods (`Architecture` 18; `Telemetry` 141); do not use it for delta. | matched 3/3 against the create-story scope manifest and the declared pre-create snapshot; exact owned-line diff, name-status set, hashes, verification commands, and same-file exclusions are recorded in `_bmad-output/implementation-artifacts/tests/27-1-create-story-scope-evidence.md`. |
| 2026-07-16 | dev-story | Accepted ADR 27.1-001, synchronized architecture/operator telemetry truth, added six structure-aware decision guards, completed validation, and moved Story 27.1 to `review`; no runtime lifecycle implementation or A41 closure was claimed. | Actual +6; cumulative +6. Comparable current-source diagnostic discovery: Server 2,145 -> 2,151, Architecture 18 -> 24, Telemetry 141 -> 141; external same-lane delta +0. Canonical fresh discovery remains blocked by the exact pre-existing `NU1605`; the older 2,132 observation is unprovenanced and excluded. | matched 7/7 against the cumulative File List using the exact scoped `git status --short --` command in Task 6 evidence and the create-story scope manifest; pre-existing same-file architecture/telemetry residual edits and unrelated sprint-status entries are named exclusions; no conditional file was added. |
