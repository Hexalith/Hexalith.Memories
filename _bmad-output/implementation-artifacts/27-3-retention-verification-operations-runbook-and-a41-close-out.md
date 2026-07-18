---
baseline_commit: 272c33bc5d30d71ac46f20e703b9d5456e75a093
creation_sprint_status_sha256: f2cc7a2520634d2ca280622faf1494477c0ec5500e24ae0c8b7baf36f1dc13c8
creation_scope_evidence: _bmad-output/implementation-artifacts/tests/27-3-create-story-scope-evidence.md
predecessor_status_at_creation: in-progress
---

# Story 27.3: Retention Verification, Operations Runbook, and A41 Close-Out

Status: ready-for-dev

Execution gate: context-ready only. Checkpoint C0 blocks Production-adapter certification, deployment-shaped proof, assurance claims, and every A41 status mutation until Story 27.2 is complete and its C1-C6 evidence, phase ledger, and cumulative File List reconcile.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a security reviewer,
I want executable lifecycle evidence and one coordinated close-out,
so that A41 closes only after the policy works in the deployment shape.

## Acceptance Criteria

1. **Given** a short test retention window and a production-shaped deployment,
   **When** old and new access events cross the expiry boundary across at least two Server writers and a controlled restart,
   **Then** focused evidence proves expired records are unavailable/purged, newer records remain, required audit emission continues, and tenant/privacy negative checks pass.

2. **Given** the ratified production duration,
   **When** operators deploy, monitor, change, or roll back the policy,
   **Then** telemetry, deployment-configuration, capacity, monitoring, incident, and recovery documentation identifies the owner, configuration, defaults, storage impact, purge verification, alarms, rollback, and assurance limits.

3. **Given** all implementation and documentation evidence passes,
   **When** A41 is closed,
   **Then** `20.5-A41-ACCESS-TELEMETRY-RETENTION` is reconciled from `carried-forward`, the matching sprint action is closed, architecture and all A41 summaries cite the evidence, and Epic 20/Story 20.5 remain historical `done` records rather than being reopened.

## Tasks / Subtasks

- [ ] Task 0 - Pass the Story 27.2 implementation handoff before beginning Story 27.3 evidence work (AC: 1, 2, 3)
  - [ ] Require Story 27.2 to be `done` with its C1-C6 checkpoints reviewed and complete, its `dev-story` and `code-review` ledger rows present, its actual runner-derived counts reconciled, and its cumulative File List matched. The creation-time `in-progress` sprint row, `ready-for-dev` story header, unchecked tasks, and pending C1-C6 rows are not completion evidence.
  - [ ] Re-read every Story 27.2 implementation, test, Dapr component, deployment, dashboard, and documentation path after that handoff. Replace all planned paths and counts in this story with the actual handoff inventory before making an implementation edit.
  - [ ] Re-run the predecessor's portable checkpoint class: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests`.
  - [ ] If Story 27.2 is incomplete or the class/path differs, record the exact blocker and return ownership to Story 27.2. Do not implement missing portable runtime behavior, claim pending/planned evidence as actual, or mutate A41 records under Story 27.3.

- [ ] Task 1 - Select, pin, approve, and capacity-gate the exact Production Dapr component profile (AC: 1, 2)
  - [ ] Record the exact Dapr runtime and component implementation identity from the running deployment, not from .NET package pins: runtime version, sidecar image digest, component type/API version/capabilities, Scheduler connections, actor types, enabled features, component manifest/profile hash, backend identity/version, configuration epoch, and any alpha opt-in.
  - [ ] Run the complete ADR behavioral probe against the exact candidate: strong CRUD/reads, ETags, record-plus-expiry-index atomicity, deterministic conflict behavior, actor state/reactivation/placement/reminders, effective TTL, request bounds, two-writer throughput during purge, durability, isolation/encryption, capacity, and physical-evidence hooks.
  - [ ] Fault-inject a later operation in the multi-key transaction and prove no partial record or expiry-index commit. Do not certify Redis from Dapr's capability table: the official Dapr 1.18 Redis implementation warns that transactions have no rollback and are unsuitable as a Production actor store unless the exact profile proves the ADR's atomicity contract.
  - [ ] Publish the adapter's declared single-component fault and prove zero acknowledged-record loss for that exact fault. A candidate that cannot prove zero is ineligible. Publish RPO/RTO only for failures outside the declared single-component profile, plus the physical-reclamation collector/bound, upgrade and rollback owner, and an approval by Hexalith Platform Operations plus the security reviewer.
  - [ ] Calculate and reserve `records × (measuredRecordBytes + measuredIndexBytes) × durabilityMultiplier + controlBytes + reclamationWorkspace` for 1 hour, 24 hours, and 7 days. Include actor/index state, transaction/AOF/WAL or snapshot overhead, replication/persistence buffers, allocator fragmentation, rewrite/compaction copy-on-write, Scheduler storage, quotas, cost, and decommission headroom.
  - [ ] Enforce steady state <=70%, reclamation peak <=80%, critical at 80%, Unhealthy at 90%, and measured >=500 events/s while purge runs. The committed single 20 GiB Redis StatefulSet cannot be presumed eligible: the ADR's 24-hour canonical payload is already 20.60 GiB before overhead.
  - [ ] Keep Production lifecycle writes disabled/fail-closed until this checkpoint passes. Memories and the lifecycle service remain Dapr-only; no backend SDK, connection string, backend credential, or orchestrator API enters application code.
  - [ ] If no eligible adapter passes every atomicity, zero-loss, capacity, security, and physical-reclamation gate, preserve each rejection and exact failed command in the evidence matrix, keep Production writes disabled and A41 open, and run correct-course. Never weaken a gate to force the current Redis profile through.

- [ ] Task 2 - Produce Production-shaped multi-writer, replacement, reminder, and durability evidence (AC: 1)
  - [ ] Run at least two Server writers through Dapr and prove unique sanitized records, fixed-actor serialization, exact transaction acknowledgement, idempotent retry, conflicting-record rejection, bounded batches, and no direct backend dependency.
  - [ ] Replace each Server writer, lifecycle-service instance, clock-service instance, Dapr sidecar, actor activation, Placement member, and Scheduler member in controlled steps. Prove durable state, actor/reminder reconstruction, configuration/key epoch convergence, and continued JSON-console and optional-OTLP emission.
  - [ ] Exercise the adapter's declared single-component fault and prove zero acknowledged-record loss. Treat any observed acknowledged loss as adapter ineligibility; RPO/RTO applies only outside the declared profile. Do not convert Redis `appendfsync everysec`, `WAIT`, a PVC-preserving pod replacement, or a broad health check into a stronger durability guarantee than the evidence supports.
  - [ ] Capture pre/post deployment identity, Dapr metadata/health, accepted record IDs and hashes, state/reminder checkpoints, backend persistence state, replacement timestamps, and observed recovery point in one reproducible evidence packet.

- [ ] Task 3 - Prove expiry, purge, newer-record preservation, and cohort-attributable physical reclamation (AC: 1, 2)
  - [ ] Use a short duration only through test composition for the wall-clock crossing test. Prove boundary validation and exact expiry arithmetic for the ratified 1-hour minimum, 24-hour default, and 7-day maximum without weakening Production values or pretending a compressed interval is a seven-day soak.
  - [ ] Cover the one-second clock/future bound, late and already-expired records, attestation source quorum/signature/freshness/replay/identity/profile, millisecond logical expiry, Dapr TTL defense in depth, retry without age extension, reminder delay, <=15-minute healthy purge catch-up, and preservation of newer records.
  - [ ] Prove portable purge with Dapr Delete, strong Get absence, and expiry-index removal, then separately prove adapter physical reclamation for the same bounded cohort within the adapter-declared bound, never greater than 24 hours after active purge.
  - [ ] For a Redis candidate, distinguish cohort key absence and dataset-memory reduction from AOF/disk reclamation; record `MEMORY USAGE`/dataset evidence and AOF size/rewrite evidence as applicable. RSS, `DBSIZE`, or global `expired_keys` alone is not cohort attribution.

- [ ] Task 4 - Exercise the failure, privacy, authority, health, metric, and alert matrix (AC: 1, 2)
  - [ ] Exercise Dapr/app/state/clock outage, stale attestation, actor failover, reminder/Scheduler delay, record and byte queue exhaustion, transaction/ETag/TTL failure, capacity pressure, malformed configuration or keys, profile drift, reconnect/revalidation, retry exhaustion, shutdown, and degraded old-image rollback.
  - [ ] Prove business requests and business readiness remain available while lifecycle assurance fails closed; expose lifecycle assurance separately. Represent ADR `NoData` as bounded health detail or a metric because ASP.NET Core health status itself is Healthy/Degraded/Unhealthy.
  - [ ] Prove all bounded lifecycle states (`accepted`, `rejected`, `enqueued`, `persisted`, `retried`, `failed`, `dropped`, `expired`, `purged`), bounded reasons/labels, Unhealthy > Degraded > NoData/Healthy precedence, the 15-minute NoData rule, actionable alerts, least-privilege inspection, raw-value absence, and no tenant-facing read route.
  - [ ] Exercise cross-tenant denial before lifecycle dependencies and name the writer route, lifecycle invocation, actor/state/index, purge selection, clock, inspector, Dapr component scopes, secret scopes, and adapter-evidence surface. Attach exact command/result evidence; broad-suite green or authorization-only proof is insufficient.
  - [ ] Re-run the inherited endpoint negatives `TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState`, `SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies`, and `TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies`, plus verifier negatives `VerifyAsync_DetectsSyntacticTenantIdMismatch_ReturnsFailed`, `VerifyAsync_DetectsSemanticTenantIdMismatch_ReturnsFailed`, and `VerifyAsync_DetectsMissingSemanticTenantId_ReturnsFailed`.
  - [ ] For a Redis adapter, require a named least-privilege ACL user/key prefix, TLS for the sidecar-to-Redis hop, network isolation, deny-by-default Dapr invocation ACLs, component/secret scopes, and API allowlists. Dapr mTLS does not secure the backend hop.

- [ ] Task 5 - Publish the lifecycle runbook, exact-adapter appendix, dashboards, alerts, and recovery links (AC: 2)
  - [ ] Add one container-service-neutral `docs/operations/access-telemetry-lifecycle.md` runbook and one exact-adapter appendix. Cover owner/authorities, prerequisites, rollout, explicit Production configuration/default/min/max, component identity/version/alpha risk, epoch convergence, capacity/cost, signals/alerts/NoData, inspection, incident triage, key rotation, purge/physical-evidence verification, recovery, upgrade/rollback, degraded old image, and verified decommission.
  - [ ] State Dapr component reload behavior and stop conditions: actor state-store changes require restart; preserve old manifests/profile; verify every writer/lifecycle instance observes the same epoch; never delete storage, actor state, secrets, or retained records during rollback.
  - [ ] Project implemented truth and cross-links into `docs/dev/telemetry.md`, `docs/operations/deployment-configuration.md`, `docs/operations/capacity-planning.md`, `docs/operations/monitoring-alerting-thresholds.md`, `docs/operations/incident-response.md`, `docs/operations/failure-recovery.md`, `docs/operations/disaster-recovery.md`, and `docs/operations/upgrade-migration.md` without copying an umbrella runbook into each file.
  - [ ] Add/extend executable dashboards and alert rules for queue/retry age, Dapr/actor/clock availability, attestation age/delta/uncertainty, transaction latency, capacity, expiry-index/oldest-due age, reminder/Scheduler age/storage, purge cohort age, adapter physical-evidence age, persistence/AOF or equivalent health, and profile drift. Keep every label bounded and privacy-safe.
  - [ ] Restate or intentionally retire, with evidence and owner, the orphaned A41 obligation in `docs/operations/rate-limiting.md`; preserve its correct Story 20.5 quota behavior.
  - [ ] Preserve the assurance limit verbatim in meaning: bounded infrastructure telemetry only, not tamper-evident, append-only, legally compliant, or certified audit retention.

- [ ] Task 6 - Resolve closure-owned residuals without hiding separately shippable work (AC: 2, 3)
  - [ ] Verify Story 27.2's source-to-persisted mapping ratification covers `partial`, authorized null-case search, all current search axes/weights, and bounded error mapping; do not treat a planned C1 as proof.
  - [ ] Reconcile architecture and telemetry wording against live emission, lifecycle, storage, and deployment behavior. Structure-guard exact rows/sections and reject leaked tool-call markup; whole-document substring matches are insufficient.
  - [ ] Explicitly dispose the Story 27.1 review residual for raw query/subject/source-URI values on preserved JSON-console/optional-OTLP routes: either sanitize before provider fan-out or document/category-exclude those routes from durable external retention. If this requires an independently deployable product-code outcome, split/correct-course and keep A41 open.
  - [ ] Verify no accepted-debt, proposal, retrospective, or tracker wording still falsely claims A41 is fully closed, entirely open, or closed by scheduling/ADR/logical deletion alone.

- [ ] Task 7 - Coordinate the evidence-backed A41 close-out without reopening historical work (AC: 3)
  - [ ] Create one canonical close-out evidence matrix linking every AC/checkpoint to exact environment identity, commands, immutable output/artifact hashes, dates, owners, review decisions, and failure/skip disposition. Zero unapproved skips are permitted.
  - [ ] Only after C0-C5 and the evidence matrix pass, change `20.5-A41-ACCESS-TELEMETRY-RETENTION` from `carried-forward` to a resolved evidence-citing disposition and close the matching sprint action from `open` to `done` with the same evidence anchor.
  - [ ] Reconcile current architecture, epics, canonical project context, telemetry guidance, audit finding summary, deferred register, and every current A41 summary. Append non-destructive closure notes to historical Story 20.5/retrospective records only where repository convention requires; never rewrite their original evidence or reopen their `done` statuses.
  - [ ] Keep Epic 27 and Story 27.3 lifecycle transitions truthful. Scheduling, ADR acceptance, a logical-delete unit test, or documentation alone never closes A41.

- [ ] Task 8 - Validate the governed story, tests, evidence, and cumulative scope (AC: 1, 2, 3)
  - [ ] Run focused Production-shaped evidence, selected-adapter probes, doc/deployment/dashboard structure guards, Kubernetes/Dapr YAML validation, security negatives, `git diff --check`, and a clean Release solution build with repository-pinned versions.
  - [ ] Discover fresh methods/cases for each affected runner unit immediately before and after Story 27.3. Map the external Story 27.2 delta explicitly; do not absorb predecessor tests into the Story 27.3 cumulative delta or mix xUnit methods with Python cases.
  - [ ] Append the `dev-story` ledger row with exact commands, phase/cumulative counts, external delta, and matched cumulative File List. Epic 28 owns EventStore runtime adoption; do not change its pins or submodule under this story.
  - [ ] If any checkpoint is independently shippable, any required evidence is missing, or the File List/ledger cannot reconcile, stop and correct course before moving to review or changing A41 state.

## Implementation Checkpoints

Story 27.3 is an explicitly approved checkpoint story. Every row is mandatory and none can claim the overall close-out early.

| Checkpoint | Accountable owner | Required evidence artifact and command | Review state | Completion state |
| :--------- | :---------------- | :------------------------------------- | :----------- | :--------------- |
| C0 - Story 27.2 completion handoff | Story 27.2 owner + code reviewer | Reconciled Story 27.2 C1-C6/ledger/File List plus `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests -parallel none -noLogo` | pending | blocked at creation: Story 27.2 is `in-progress`, runtime lane absent, C1-C6 pending |
| C1 - Exact Production adapter, capability, capacity, and security approval | Deployment adapter owner + Hexalith Platform Operations | `_bmad-output/implementation-artifacts/tests/27-3-adapter-profile-evidence.md`; run `python3 tools/verify-access-telemetry-lifecycle.py --checkpoint adapter-profile --evidence _bmad-output/implementation-artifacts/tests/27-3-adapter-profile-evidence.md` | pending | blocked on C0 |
| C2 - Multi-writer, replacement, reminder, and single-fault durability proof | Story 27.3 evidence owner + security reviewer | `_bmad-output/implementation-artifacts/tests/27-3-production-lifecycle-evidence.md`; run `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryRetentionProductionVerificationTests -class Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleFaultRecoveryTests -parallel none -noLogo` | pending | blocked on C0-C1 |
| C3 - Expiry, purge, newer preservation, and physical reclamation | Deployment adapter owner + security reviewer | `_bmad-output/implementation-artifacts/tests/27-3-retention-reclamation-evidence.md`; run `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Release/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll -class Hexalith.Memories.AccessTelemetry.Tests.Adapter.ProductionAdapterCapabilityAndReclamationEvidenceTests -parallel none -noLogo` and `python3 tools/verify-access-telemetry-lifecycle.py --checkpoint retention-reclamation --evidence _bmad-output/implementation-artifacts/tests/27-3-retention-reclamation-evidence.md` | pending | blocked on C1-C2 |
| C4 - Failure, tenant/privacy, observability, and alerts | Test architect + Hexalith Platform Operations | `_bmad-output/implementation-artifacts/tests/27-3-failure-privacy-observability-evidence.md`; run `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryTenantPrivacyProductionTests -parallel none -noLogo` and `python3 tools/verify-access-telemetry-lifecycle.py --checkpoint failure-privacy-observability --evidence _bmad-output/implementation-artifacts/tests/27-3-failure-privacy-observability-evidence.md` | pending | blocked on C0-C3 |
| C5 - Runbook and exact-adapter appendix | Technical writer + Hexalith Platform Operations + security reviewer | `docs/operations/access-telemetry-lifecycle.md` and `docs/operations/access-telemetry-adapter-production.md`; run `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.AccessTelemetryOperationsContractTests -class Hexalith.Memories.Server.Tests.Deployment.OperationalRunbookSetTests -class Hexalith.Memories.Server.Tests.Telemetry.MemoriesDashboardTests -parallel none -noLogo` | pending | blocked on C1-C4 |
| C6 - Coordinated A41 close-out | Security remediation owner + planning owner | `_bmad-output/implementation-artifacts/tests/27-3-a41-close-out-evidence.md`; run `python3 tools/verify-access-telemetry-lifecycle.py --checkpoint a41-close-out --evidence _bmad-output/implementation-artifacts/tests/27-3-a41-close-out-evidence.md` and `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -parallel none -noLogo` | pending | blocked on C0-C5; no A41 mutation allowed |

## Dev Notes

### Scope and Authority

- The exact Story/AC text comes from current `epics.md`; the accepted ADR's **Story 27.3 Verification and Operations Handoff** is the technical authority. Story 27.2 implements the portable runtime; Story 27.3 proves the exact deployment adapter, publishes operations guidance, and coordinates the one remaining A41 retention closure. [Source: _bmad-output/planning-artifacts/epics.md#Story-27.3-Retention-Verification-Operations-Runbook-and-A41-Close-Out; docs/dev/adr-27.1-001-access-telemetry-lifecycle.md#Story-27.3-Verification-and-Operations-Handoff]
- This is one independently demonstrable outcome: an accepted Production-shaped lifecycle assurance package that permits the single A41 retention residual to close. Adapter certification, lifecycle/failure/security evidence, operator documentation, and tracker reconciliation are mutually dependent review gates, not separate completion claims.
- Split/correct-course if discovery yields multiple adapters, a general trusted-time/persistence/operations platform, a tenant-facing inspection product, a runtime feature missing from Story 27.2, or an independently deployable raw-console/OTLP privacy change. Keep A41 open until the new slice completes.
- Bounded infrastructure telemetry remains separate from domain persistence. Do not add access telemetry to Hexalith.EventStore, repurpose domain state, or add direct infrastructure dependencies to product/lifecycle code.

### Current State, Required Updates, and Preservation Rules

| Path/surface | Current state at story creation | Story 27.3 change | Preserve |
| :----------- | :------------------------------ | :---------------- | :------- |
| Story 27.2 runtime/tests/deployment | Runtime projects, Server lifecycle folder, AccessTelemetry.Tests, and dedicated lifecycle components are absent; story status/checkpoints are unreconciled | C0 re-reads the actual completed handoff and binds exact evidence paths | Do not implement predecessor scope or count its planned tests as actual |
| `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` | Accepted portable contract and explicit 27.3 handoff; A41 remains open | Append exact approved adapter/profile and evidence anchors only after proof | Accepted Dapr-only design, authorities, bounds, rollback, assurance limit |
| `docs/dev/telemetry.md` | Documents FR67 JSON/OTLP emission, accepted-but-unimplemented lifecycle target, and open A41 residual | Describe only implemented/proved lifecycle and link the runbook/evidence | FR67 event IDs/schema, provider separation, JSON/optional-OTLP continuity, privacy warnings |
| Existing operations docs | Deployment, capacity, monitoring, incident, recovery, disaster, and rate-limit procedures exist but have no proven lifecycle adapter contract | Add narrow cross-links and surface-specific lifecycle steps; use one canonical runbook | Existing product/backend procedures and owners; avoid duplicated umbrella text |
| Production deployment | Two read-only-root Server replicas; shared `state.redis/v1`; one 20 GiB Redis Stack StatefulSet; no dedicated lifecycle service/store | Apply only the approved exact lifecycle profile after C1; keep separate identities/authority | Two writers, non-root/read-only posture, business readiness, existing shared state workloads |
| `AccessTelemetryRetentionDecisionTests.cs` | Six structure-aware tests guard the accepted ADR, current open residual, and anti-corruption rules | Reconcile guards to exact profile/evidence/closed state after C6 | Exact table/section binding and leaked-markup rejection |
| A41 deferred/action/current summaries | Deferred entry is `carried-forward`; sprint action is `open`; current summaries forbid premature closure | Update atomically to the same evidence-backed disposition after C0-C5 | Epic 20/Story 20.5 history and `done` statuses; original approval/evidence chronology |

Every future Story 27.2 UPDATE file must be read completely during C0. For each, record its state machine/data shape/API calls, the exact verification-only change, and behaviors to preserve before editing it. A planned filename in this story is not authority over the completed handoff.

### Architecture and Technology Guardrails

- Stable logical identities remain `memories-access-telemetry`, `AccessTelemetryLifecycleActor/global`, `memories-access-telemetry-clock`, `access-telemetry-store`, `access-telemetry-secrets`, and `access-telemetry-config` unless an accepted ADR amendment says otherwise.
- Repository pins control implementation: .NET SDK 10.0.302/net10.0/C# 14, Dapr .NET packages 1.18.4, Aspire AppHost SDK 13.4.6, StackExchange.Redis 3.0.17, xUnit v3 3.2.2, Shouldly 4.3.0, and NSubstitute 5.3.0. Do not upgrade dependencies in this story. The running Dapr runtime/component identity must be captured independently.
- Use current one-type-per-file, file-scoped namespace, XML documentation, source-generated logging, central package management, warnings-as-errors, `.slnx`, SDK containers, and existing composition roots.
- Preserve sanitization-before-enqueue, finite catalogs, bounded queues/batches/retries, signed independent UTC, source-age retention, transactional record/index writes, fixed-actor/reminder recovery, separate authorities, low-cardinality metrics, and business-readiness isolation.

### Latest Technical Information

Research snapshot: 2026-07-18. Repository pins remain authoritative.

- Dapr state-store feature tables are not behavioral certification. The official Dapr 1.18 Redis implementation states that Redis transactions lack rollback; the exact adapter must pass a partial-commit fault probe. [Source: https://docs.dapr.io/reference/components-reference/supported-state-stores/; https://raw.githubusercontent.com/dapr/components-contrib/v1.18.0/state/redis/redis.go]
- Dapr forwards `ttlInSeconds` to native stores but does not independently expire state; logical expiry and active purge remain authoritative and testable separately. [Source: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-store-ttl/]
- Actor reminders survive activation failover through Dapr Scheduler; Production evidence must cover Scheduler/Placement persistence and replacement, not only the actor process. [Source: https://docs.dapr.io/developing-applications/building-blocks/actors/actors-timers-reminders/; https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-persisting-scheduler/]
- Dapr metadata and health APIs expose runtime/component/actor/Scheduler identity needed in the evidence packet; do not infer runtime version from application packages. [Source: https://docs.dapr.io/reference/api/metadata_api/; https://docs.dapr.io/reference/api/health_api/]
- Actor state-store component changes require restart and component reload can transiently close/reinitialize the component; rollout/rollback must preserve manifests and prove epoch convergence. [Source: https://docs.dapr.io/operations/components/component-updates/; https://docs.dapr.io/reference/api/configuration_api/]
- Redis `everysec` AOF and `WAIT` do not establish a universal zero-loss guarantee. If Redis is selected, scope the tested fault/RPO precisely and separately prove dataset and AOF/disk reclamation for the test cohort. [Source: https://redis.io/docs/latest/operate/oss_and_stack/management/persistence/; https://redis.io/docs/latest/commands/wait/; https://redis.io/docs/latest/commands/memory-usage/; https://redis.io/docs/latest/commands/info/]
- The current local integration environment reported Dapr runtime 1.18.1 while application Dapr packages are 1.18.4. This is baseline evidence of identity drift, not the Production identity; C1 must capture and pin the running Production runtime/component profile explicitly.

### Testing Baseline and Planned Delta

Fresh comparable baselines were built on 2026-07-18 at commit `272c33bc5d30d71ac46f20e703b9d5456e75a093` with the pre-existing dirty Story 27.2 sprint transition and EventStore submodule work excluded from Story 27.3 ownership.

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo | awk '/^Hexalith\.Memories\.Server\.Tests\./ { total++; if ($0 ~ /\.Architecture\./) architecture++; if ($0 ~ /\.Telemetry\./) telemetry++; if ($0 ~ /\.Hosting\./) hosting++ } END { print total, architecture, telemetry, hosting }'
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo | sed -n '/^Hexalith\.Memories\.Server\.Tests\./p' | LC_ALL=C sort | sha256sum
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -parallel none -noLogo

DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods -noLogo | awk '/^Hexalith\.Memories\.IntegrationTests\./ { total++; if ($0 ~ /\.Telemetry\./) telemetry++ } END { print total, telemetry }'
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods -noLogo | sed -n '/^Hexalith\.Memories\.IntegrationTests\./p' | LC_ALL=C sort | sha256sum
```

- Server.Tests: build 0 warnings/errors; 2,157 xUnit methods total, including Architecture 24, Telemetry 141, and Hosting 17; sorted method-set SHA-256 `98440744599bf454ee991a3cad39f69dee9185f6d1fe86106223638c3a194ae2`. `AccessTelemetryRetentionDecisionTests` is 6 methods and ran 6/0/0.
- IntegrationTests: build 0 warnings/errors; 270 xUnit methods total, including Telemetry 9; sorted method-set SHA-256 `038e0a140092d0a7910c17566a5b87a05bdc417699f67c4335516777168a30c4`.
- `Hexalith.Memories.AccessTelemetry.Tests`: absent because Story 27.2 is incomplete. Its later first Story 27.2 discovery is an external `0 -> N` transition and must be mapped before Story 27.3 counts.
- `tests/tooling/access_telemetry_lifecycle`: absent. If created, its first Python discovery is a named Story 27.3 `0 -> N` transition.

Planned Story 27.3 additions are quantified by comparable named unit, not combined across runners:

- Server.Tests: +8 to +12 xUnit methods for exact evidence/doc/deployment/A41 guards.
- IntegrationTests: +22 to +32 xUnit methods for multi-writer, replacement, retention, fault, privacy, and continuity proof.
- AccessTelemetry.Tests after the external Story 27.2 baseline: +10 to +16 xUnit methods for adapter atomicity/capability/reclamation and clock/reminder edge evidence.
- New tooling lane: +8 to +12 Python `unittest` cases for environment identity, immutable evidence, capacity arithmetic, doc/status parity, and zero-unapproved-skip validation.

Actual create-story delta is +0 and cumulative +0 in every lane. At C0, capture post-Story-27.2 totals and record its external delta before subtracting any Story 27.3 result.

### Historical Context Classification

| Reference | Classification | Permitted influence on Story 27.3 |
| :-------- | :------------- | :-------------------------------- |
| Story 27.1 whole-story shape | `historical-reference-only` | Accepted decision provenance only; do not copy its long decision/review task shape. |
| Accepted ADR 27.1, especially the 27.3 handoff | `current-narrow-pattern` | Current technical/evidence authority. |
| Story 27.1 superseded Redis/Kubernetes iterations | `anti-template` | Never restore backend SDKs, Kubernetes clock/Pod identity, or backend/orchestrator application dependencies. |
| Story 27.2 | `current-narrow-pattern` | Completed C1-C6/negative-test handoff only; do not duplicate its implementation breadth or accept pending/planned evidence. |
| Story 7.5 | `anti-template` | Preserve FR67 emission only; its broad observability/stdout proof is not retention evidence. |
| Story 8.4 | `historical-reference-only` | Current-emission/test-helper provenance only; stdout evidence is not TTL/purge/reclamation proof. |
| Story 8.5 | `anti-template` | Do not copy its bundled operational scope. |
| Story 20.2 | `historical-reference-only` | Three reverified denial-before-dependency negatives and principal-derived identity only. |
| Story 20.5 | `anti-template` | Rate-limit/audit-emission provenance and residual ID only; never reopen or reuse the broad bundle. |
| Story 21.1 | `current-narrow-pattern` | Reverified structure-aware document-guard mechanics only. |
| Story 24.3 | `historical-reference-only` | Three verifier/tenant-marker negatives only; not its decision-plus-implementation shape. |
| Story 24.4 | `current-narrow-pattern` | Reverified finite metric/tag validation mechanics only. |
| Stories 26.1 and 26.5 | `anti-template` | Current manifest/doc facts only; never copy broad infrastructure/runbook/checkpoint scope. |
| Story 26.6 | `current-narrow-pattern` | Reverified rollback and observable restoration mechanics only. |
| Story 26.8 and Epic 20/21 retrospectives | `historical-reference-only` | Close-out chronology/provenance only; numeric adjacency and old green evidence are irrelevant. |
| Retention visibility proposal | `historical-reference-only` | Durable premature-closure guard; its no-scheduling text was superseded by the approved implementation proposal. |

### Slice Proof

Story 27.3 is one approved security-review decision: accept a Production-shaped lifecycle assurance package and use it to close the single A41 retention residual. C0-C6 give every mutually dependent facet an owner, evidence artifact/command, review state, and completion state. No checkpoint is a separately shippable success, and the close-out cannot proceed partially.

The split boundary is strict: 27.1 owns the accepted design, 27.2 owns portable runtime behavior, 27.3 owns exact-adapter certification/deployment evidence/runbook/close-out, and Epic 28 owns EventStore runtime adoption. Any general platform, tenant-facing product, multiple-adapter rollout, or missing runtime feature returns to planning instead of hiding inside this tracking story.

### Project Structure Notes

Likely **new** paths after C0; bind names to the actual predecessor structure:

- `docs/operations/access-telemetry-lifecycle.md` and `docs/operations/access-telemetry-adapter-production.md`.
- `_bmad-output/implementation-artifacts/tests/27-3-retention-verification-evidence.md` as the canonical evidence matrix.
- `tests/Hexalith.Memories.Server.Tests/Deployment/AccessTelemetryOperationsContractTests.cs` and `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryA41CloseOutTests.cs`.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Adapter/ProductionAdapterCapabilityAndReclamationEvidenceTests.cs`.
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryRetentionProductionVerificationTests.cs`, `AccessTelemetryLifecycleFaultRecoveryTests.cs`, and `AccessTelemetryTenantPrivacyProductionTests.cs`.
- `tools/verify-access-telemetry-lifecycle.py` plus tests under `tests/tooling/access_telemetry_lifecycle/` for immutable environment/evidence, checkpoint, capacity, and status-parity checks.
- Deployment-neutral alert rules only if no current executable alert artifact can carry them.

Likely **updated** current paths:

- Story 27.2 lifecycle/clock/Server provider, component, deployment, dashboard, and test paths discovered at C0 only where verification exposes a harness/configuration defect within 27.3 scope.
- `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md`, `docs/dev/telemetry.md`, and the operations documents named in Task 5.
- Selected lifecycle component/deployment overlays, `deploy/grafana/dashboards/memories-operability.json`, and exact manifest/evidence guards.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs` and actual Story 27.2 checkpoint tests.
- `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, current architecture/epics/project-context/audit summaries, and evidence-linked historical close-out notes permitted by Task 7.

Do not edit `tools/release-packages.json`, add Dockerfiles, change EventStore pins/submodules, reopen Epic 20/Story 20.5, or duplicate the canonical runbook across every operations document.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic-27-Access-Telemetry-Lifecycle-Hardening]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-27.3-Retention-Verification-Operations-Runbook-and-A41-Close-Out]
- [Source: docs/dev/adr-27.1-001-access-telemetry-lifecycle.md#Story-27.3-Verification-and-Operations-Handoff]
- [Source: docs/dev/adr-27.1-001-access-telemetry-lifecycle.md#Retention-Expiry-Purge-and-Clock]
- [Source: docs/dev/adr-27.1-001-access-telemetry-lifecycle.md#Capacity-Evidence-and-Admission-Envelope]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-access-telemetry-retention-implementation.md]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Story-20.5-Deferred-Retention-Slice-2026-07-04]
- [Source: docs/dev/telemetry.md#Retention-lifecycle-status]
- [Source: _bmad-output/project-context.md#Testing-Rules]
- [Source: _bmad/custom/story-scope-guard.md]
- [Source: _bmad/custom/story-phase-ledger.md]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-18: HALT at C0 / Task 0. Story 27.2 is not a completed handoff: its story header is `ready-for-dev`, sprint status is `in-progress`, Tasks 1-9 and checkpoints C1-C6 remain pending, and no `dev-story` or `code-review` ledger row exists. Exact required checkpoint command `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests -parallel none -noLogo` exited 0 but discovered/executed `Total: 0`; the named class is absent from source. Ownership returns to Story 27.2; Story 27.3 implementation, Production-adapter certification, evidence claims, and A41 mutations remain blocked.

### Completion Notes List

- 2026-07-18: Created Story 27.3 from current planning, the accepted ADR, the incomplete Story 27.2 handoff, live deployment/code/tests, official Dapr/Redis/.NET guidance, and the repository historical-slice/phase-ledger policies.
- 2026-07-18: Added C0 to prevent pending Story 27.2 work from being counted or implemented as Story 27.3, and added C1-C6 owners/evidence/review gates for the approved checkpoint breadth.
- 2026-07-18: Recorded the exact Redis atomicity, current 20 GiB capacity, Scheduler/reminder, durability, security, and physical-reclamation hazards that the exact Production adapter must prove rather than inherit.
- 2026-07-18: Ultimate context engine analysis completed - comprehensive developer guide created.

### File List

- `_bmad-output/implementation-artifacts/27-3-retention-verification-operations-runbook-and-a41-close-out.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/27-3-create-story-scope-evidence.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-18 | create-story | Created context-ready Story 27.3 and moved only its sprint row from `backlog` to `ready-for-dev`; no implementation or A41 close-out occurred. C0 records Story 27.2 as an external in-progress prerequisite. | Actual phase delta +0 and cumulative +0. Fresh comparable builds/discovery: Server.Tests 2,157 xUnit methods (Architecture 24, Telemetry 141, Hosting 17) and IntegrationTests 270 xUnit methods (Telemetry 9), using the exact build/count/hash commands in **Testing Baseline and Planned Delta**; both builds passed with 0 warnings/errors. AccessTelemetry.Tests is absent because Story 27.2 is incomplete and must be mapped later as its external `0 -> N` lane. Planned Story 27.3 ranges by named unit: Server +8..12 xUnit methods, Integration +22..32 xUnit methods, post-27.2 AccessTelemetry +10..16 xUnit methods, and new tooling +8..12 Python unittest cases. | matched 3/3 against baseline `272c33bc5d30d71ac46f20e703b9d5456e75a093` and pre-create sprint SHA-256 `f2cc7a2520634d2ca280622faf1494477c0ec5500e24ae0c8b7baf36f1dc13c8`; exact owned-line diff, exclusions, post-create hash, and verification commands are in `_bmad-output/implementation-artifacts/tests/27-3-create-story-scope-evidence.md`. |
