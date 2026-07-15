---
baseline_commit: e574c313ae067b72ac7f69dd3061099c1a425466
---

# Story 26.5: Operational Runbook Set

Status: done

<!-- Epic 26 — Test, Deployment & Operational Readiness. Documentation-and-contract-test story closing the runbook portion assigned to audit finding A25. Do not change product behavior, deployment manifests, package versions, alert infrastructure, benchmark inputs, or submodule pointers. -->

## Story

As an operator,
I want the missing operational runbooks,
so that production incidents and lifecycle operations have documented procedures.

## Context

The canonical Epic 26 acceptance criterion names six missing operational procedures. The repository already contains deployment, failure-recovery, rate-limit, embedding-migration, pipeline-persistence, backup/restore, and disaster-recovery guidance, but it does not provide a complete operator path for capacity, incidents, index rebuild decisions, tenant lifecycle, upgrades, or alert thresholds. The missing documents must consolidate and link the existing sources rather than fork their contracts.

The implementation baseline also contains hazards that a prose-only treatment would amplify:

1. The production Kustomize namespace is `hexalith-memories`, the Redis StatefulSet/pod are `redis-stack`/`redis-stack-0`, and the generated data PVCs are `data-redis-stack-0` and `data-falkordb-0`. The current backup/restore and disaster-recovery examples still contain the older namespace `memories`, StatefulSet/pod references `redis`/`redis-0`, and old PVC names. The bare `redis` Service/container/component literal remains current where the manifests use it; only stale resource roles may be changed. The command examples must be reconciled and authenticated safely before the new incident and rebuild runbooks link operators to them.
2. There is no universal index-rebuild workflow. Consistency repair supports bounded projection repair, semantic repair still throws `NotSupportedException`, duplicate EventStore delivery short-circuits, and embedding dimension/provider changes use the existing blue/green migration tool. The rebuild runbook must present a decision tree, not invent a command.
3. The shipped CLI currently supports `tenant list`, not tenant create/delete/verify. Tenant onboarding/offboarding uses authenticated REST routes and asynchronous workflow-status polling. External Kubernetes/Dapr embedding secrets are operator-owned and are not removed by tenant deletion.
4. `/ready` can return HTTP 200 with JSON status `Degraded`; an HTTP-success-only check is insufficient. The repository has a committed Grafana dashboard but no committed alert-rule set. The runbooks must distinguish observed metrics, hard PRD targets, proposed thresholds, and deployed alerting.
5. Stories 26.2, 26.3, 26.4, 26.6, and 26.7 are in review at story-creation time. Implementation must re-run the Epic audit-anchor preflight against the then-current revision and must not turn pending or skipped evidence into an operational assurance.

Story 26.5 is checkpoint-heavy. The implementation remains one story only if every independently reviewable checkpoint records its owner, validation command/artifact, review status, and completion date in the evidence table below.

## Acceptance Criteria

1. **The exact six-runbook required-new-file set exists with a shared executable structure.** Create these files under `docs/operations/`, while preserving the existing operations files: `capacity-planning.md`, `incident-response.md`, `index-rebuild.md`, `tenant-onboarding-offboarding.md`, `upgrade-migration.md`, and `monitoring-alerting-thresholds.md`. Every file contains exact second-level sections for `Purpose and scope`, `Prerequisites and authorization`, `Signals and evidence`, `Procedure`, `Verification and evidence`, `Rollback, recovery, and stop conditions`, `Escalation evidence`, and `Related runbooks and sources`. Each runbook identifies owner/review cadence, last-verified date and repository revision, tenant/case/backend blast radius, destructive steps, required approvals, safe stop conditions, and redacted evidence to retain. No unfinished authoring marker (`TODO`, `TBD`, `REPLACE_ME`), example credential value, fabricated production result, or unverified completion claim remains; documented and initialized non-secret shell variables are required and are not authoring placeholders.

2. **Capacity planning publishes a measurable NFR14 sizing method.** `capacity-planning.md` distinguishes the committed bootstrap requests/limits/PVC sizes from recommendations; derives raw float32 vector payload as `4 × dimensions × stored-vector-count` while treating hashes, metadata/content, HNSW, graph, workflow, AOF/RDB rewrite, allocator, and replication/backup overhead as measured additions; and gives a repeatable representative-tenant procedure using Kubernetes utilization/PVC data plus Redis `INFO memory`, `INFO persistence`, `MEMORY USAGE`, `FT.INFO` and FalkorDB `GRAPH.MEMORY USAGE`. It calculates per-unit and per-tenant deltas, records model dimensions/chunk counts and assumptions, validates headroom under ingestion and persistence rewrite, and defines evidence-based scale/stop triggers. It states that `memories.index.size` is a document count rather than bytes, `noeviction` pressure can reject writes, no HPA is committed, and the single-replica Redis/FalkorDB topology must not be described as linearly scalable.

3. **Incident response is safe, severity-based, and degraded-mode aware.** `incident-response.md` defines severity/roles, first-response timing, tenant/case/backend/all-tenant blast-radius classification, communication/escalation, redacted evidence capture, containment, recovery, and post-incident follow-up. Its decision tree covers startup/liveness/readiness, Dapr/state-store failure, Redis/semantic/FalkorDB axis degradation, provider/rate-limit failure, queue or workflow stalls, ingestion failure registry, consistency/isolation signals, PVC/capacity pressure, and “healthy pods but zero ingestion” caused by missing `MEMORIES_EVENTSTORE_TOPIC`. It parses the `/health`, `/alive`, and `/ready` JSON contract, preserves safe service on unaffected axes where allowed, treats suspected cross-tenant data or secret exposure as critical containment events, and links recovery actions rather than encouraging direct Redis/FalkorDB/Dapr state edits.

4. **Index rebuild guidance selects only supported recovery paths.** `index-rebuild.md` provides a decision matrix separating: read-only consistency verify/inspect and the supported graph re-merge plus semantic/graph orphan removal (the workflow does not rebuild a missing syntactic record); blue/green embedding migration through `tools/MigrateEmbeddingVectors`; logical export/import or physical restore; and tenant reprovisioning followed by original-source re-ingestion/republication when a full rebuild is actually required. It explicitly documents unsupported semantic re-index, unrepairable missing syntactic data, EventStore dedup/no `forceReplay` limitation, chunked vector keys, current index-provisioning ownership, and the need to preserve aliases/old targets until verification. Every path requires pre-change backup, write/workflow coordination, tenant-scoped canary, per-axis search/count/consistency checks, stop criteria, and a feasible rollback or recovery route. It never instructs operators to run manual `FT.DROPINDEX`, ingestion-owned `FT.CREATE`, or a nonexistent generic rebuild command.

5. **Tenant onboarding/offboarding follows the current authenticated lifecycle contract.** `tenant-onboarding-offboarding.md` uses the current REST routes, and the typed client only where its surface supports the operation: the create body is `{ tenantId, displayName, vectorDimensions }`, while `MemoriesClient.CreateTenantAsync` currently has no dimensions parameter and therefore uses the 768 default. Provisioning polls the workflow/status contract to a terminal success and independently confirms `GET /tenants/{tenantId}` is `Active` before verification, provider/secret/quota configuration, canary ingest/search/telemetry, and handoff evidence. Offboarding requires intake freeze and stakeholder/retention approval, logical export and a physically consistent backup before destructive action, and an accepted asynchronous delete. Poll deletion status only while the registry entry exists: successful deletion removes that entry, so a later status request can return `404 TENANT_NOT_FOUND`, and no `Deleted` tenant state may be awaited. Treat that 404 as completion only after independent registry/index/graph/tenant-state absence and stale-access denial; if the tenant remains `Failed`/`CompensationFailed`, re-trigger DELETE through the existing idempotent recovery path. Retire externally managed secret material only after deletion verification. The runbook describes cross-tenant reference and telemetry/audit limitations honestly and does not publish nonexistent `memories tenant create`, `delete`, or `verify` commands.

6. **Upgrade/migration separates stateless rollback from durable-data recovery.** `upgrade-migration.md` covers version/runtime/CRD compatibility inventory, component/kubectl skew, rendered Kustomize diff, image/digest provenance, secret/config validation, immutable backup plus restore rehearsal, workflow/intake quiescence, canary tenant, rollout observation, and post-upgrade health/tenant-isolation/ingestion/search/consistency/export-restore verification. It cross-links the existing embedding blue/green procedure. Workflow-shape changes account for deterministic replay and the current replay-safety gate failing open on query error or timeout. Stateless Pod-template rollback must not be represented as reversal of schemas or durable data; data rollback uses retained logical exports/snapshots, and Redis/FalkorDB PVCs are never deleted as a normal rollback step.

7. **Monitoring guidance maps real signals to actionable, evidence-labeled thresholds.** `monitoring-alerting-thresholds.md` maps every recommended alert to a real application, health, Kubernetes, Redis, FalkorDB, Dapr, or OpenTelemetry signal; includes warning/critical condition, evaluation window, NoData behavior, blast radius, linked response procedure, and verification; and preserves the repository cardinality/redaction policy. It carries the hard PRD search p95 targets (syntactic 200 ms, semantic 500 ms, hybrid 1 s, graph 2 s), ingestion targets (>100 units/min for <=10 KB and >10/min for <=1 MB under the documented baseline), event freshness <5 s, and application cold-start within 60 s (<=60 s) as requirement targets. Queue depth, error ratios, memory/PVC headroom, restart/OOM, missing telemetry, and infrastructure thresholds without a product requirement are labeled baseline-dependent recommendations with a tuning/ownership procedure. It states that the committed dashboard observes signals but no alert rules are deployed by this story.

8. **Navigation is complete and existing recovery commands are safe.** Both `docs/operations/deployment-configuration.md` and `docs/operations/failure-recovery.md` contain direct Markdown links to all six new runbooks, satisfying the Epic acceptance criterion. The six runbooks link back to the relevant deployment, failure-recovery, backup/restore, disaster-recovery, consistency, telemetry, health, route, provider-migration, rate-limit, pipeline-persistence, and release guidance without copying contradictory procedures. As part of the final recovery-document handoff from Story 26.2, `backup-restore.md` and `disaster-recovery.md` are updated only where needed for current production namespace/resource/PVC names, secret-safe authenticated commands, physical consistency, and incident/rebuild/upgrade navigation. Quiesce intake and drain in-flight workflows before independent Redis/FalkorDB snapshots unless a supported group-snapshot primitive supplies an atomic boundary; poll `BGSAVE`/AOF persistence status to successful completion; never copy a live, mutating AOF directory, instead copying from a quiesced/read-only snapshot or maintenance mount; record both backend snapshot identities/timestamps; and resume intake only after backup verification. All changed relative Markdown links resolve.

9. **A Docker-free source-backed documentation contract prevents silent drift.** Add `tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs`, following the existing repo-root walk, xUnit v3, Shouldly, header, file-scoped namespace, and one-type-per-file conventions. The tests verify that the required-new-file set is exactly the six named runbooks while existing operations files remain, exact common section headings, direct links from both required anchor documents, relative-link resolution across changed runbooks, absence of unfinished authoring markers and unsafe/stale command examples, and a small set of critical literals tied to current sources (production namespace/resource names, health status semantics, canonical lifecycle routes, actual metric names, and dashboard path). Reuse `DeploymentConfigurationContractTests`/`DocumentationCompletenessTests`; do not add a second Markdown tool, package, test project, or CI lane, and do not reduce the existing deployment contracts to brittle bare-substring-only assertions.

10. **Completion evidence is scope-honest and checkpointed.** The implementation records its revision and audit-anchor reconciliation, completes the checkpoint evidence table, builds the Server test project and solution in Release with zero warnings/errors, passes the focused runbook/deployment contract tests and the normal Docker-free Server test project, and leaves `git diff --check` clean. The diff is limited to the declared documentation, one documentation-contract test, and BMad story/status tracking. It does not change product source, deployment manifests, alert infrastructure, schemas, package/version pins, generated artifacts, benchmark inputs/results, or `references/**` contents/pointers.

## Tasks / Subtasks

- [x] **Task 1 — Freeze the live audit anchors and evidence contract** (AC: 1, 8, 10)
  - [x] Record the implementation-start revision/date and re-check Stories 26.2, 26.6, and 26.7 plus the production Kustomize, route, health, telemetry, index, and workflow anchors. Record moved/stale anchors and the adopted replacement in the completion notes.
  - [x] Confirm the exact six filenames and shared headings before authoring; fill one checkpoint row per runbook, navigation/recovery reconciliation, contract test, and final verification.
  - [x] Treat current code/manifests and `project-context.md` as authoritative over stale planning-version/topology prose. Do not silently “correct” behavior in documentation.

- [x] **Task 2 — Author the capacity-planning runbook** (AC: 1, 2)
  - [x] Publish the current Server, MCP, Dapr sidecar, Redis, and FalkorDB requests/limits plus 20 GiB Redis and 10 GiB FalkorDB PVC bootstrap values by referencing the manifests, not duplicating them as timeless recommendations.
  - [x] Provide a worked, measurement-first worksheet for vector dimensions, chunks/stored-vector count, metadata/content, Redis Search/HNSW, FalkorDB, workflow/state, AOF/RDB, PVC growth, and headroom; label every value as measured, configured, formula-derived, or assumed.
  - [x] Include baseline/load/measure/project/verify/stop/recover steps and evidence artifacts. Explain no-HPA, single-replica, `noeviction`, provider quota, and process-local extraction-gate constraints.

- [x] **Task 3 — Author the incident-response runbook** (AC: 1, 3)
  - [x] Define severity, commander/owner roles, response timing, scope/blast-radius assessment, communications, containment, evidence/redaction, recovery verification, escalation, and post-incident review.
  - [x] Build the decision tree from structured health JSON and current metrics/log/error codes, including Degraded HTTP 200, missing event-topic ingestion, backend/provider/rate-limit/workflow/consistency/capacity branches, and missing telemetry.
  - [x] Route destructive or stateful recovery to the canonical backup/restore, disaster-recovery, failure-recovery, consistency, and index-rebuild procedures; never recommend secret disclosure or direct state edits.

- [x] **Task 4 — Author the index-rebuild decision runbook** (AC: 1, 4)
  - [x] Document the verify/inspect/repair, embedding migration, restore/import, and full reprovision/re-ingest paths with applicability, limitations, authorization, prerequisites, verification, stop, and rollback/recovery.
  - [x] Reuse `docs/dev/consistency.md`, `embedding-providers.md`, and the Story 26.2 recovery docs. Call out supported graph re-merge/orphan removal, unrepairable missing syntactic records, unsupported semantic re-index, duplicate EventStore replay, current chunked vector shape, and provisioning ownership.
  - [x] Require backup, intake/workflow coordination, tenant canary, per-axis parity, index/graph evidence, and preservation of old targets until acceptance.

- [x] **Task 5 — Author the tenant lifecycle runbook** (AC: 1, 5)
  - [x] Describe authenticated REST create/provision-status/verify and delete/deletion-status flows using the route constants/current endpoint contracts; document the typed client's current 768-dimension limitation and do not infer a CLI surface from stale prose or verifier messages.
  - [x] Define onboarding preflight for identity mapping, capacity/quota, dimensions/provider secret references, physical isolation, Active status, canary ingest/search, and telemetry handoff.
  - [x] Define offboarding freeze/approval/export/consistent-backup/delete/poll/independent-absence-verification/secret-retirement/evidence sequence, including the post-registry-removal `TENANT_NOT_FOUND` status race, idempotent DELETE recovery for failed states, and non-reversible/cross-tenant-reference boundaries.

- [x] **Task 6 — Author the upgrade/migration runbook** (AC: 1, 6)
  - [x] Cover inventory/compatibility, rendered diff, image provenance, secret/config, backup and restore rehearsal, intake/workflow drain, canary, rollout, structured health, smoke/parity, and observation-window evidence.
  - [x] Separate stateless rollback from state/schema recovery; retain PVCs and old logical/snapshot evidence. Link rather than duplicate embedding provider migration.
  - [x] Include Dapr workflow determinism/versioning/purge considerations, CRD/runtime upgrade order, Kubernetes skew, container-base compatibility, Redis Stack-to-Redis 8 planning, and staged FalkorDB migration as preflight topics only—do not upgrade any component in this story.

- [x] **Task 7 — Author the monitoring and alerting-threshold runbook** (AC: 1, 7)
  - [x] Inventory the actual `Hexalith.Memories` meter/dashboard signals and structured health entries; distinguish emitted metrics, infrastructure metrics, and signals the operator must supply.
  - [x] Publish a signal/condition/window/severity/NoData/blast-radius/action/verification table. Carry hard PRD targets exactly; mark all other starter thresholds provisional and require baseline/tuning ownership.
  - [x] Include stalled-progress rather than depth-only queue detection, index-count-versus-byte distinction, rate-limit/failure ratios, OOM/restart/PVC/AOF/backend signals, handler mismatch/drop, OTLP missing data, and low-cardinality/redaction rules.

- [x] **Task 8 — Complete navigation and reconcile recovery docs** (AC: 3-8)
  - [x] Add all six direct links to both `deployment-configuration.md` and `failure-recovery.md` while preserving their existing contracts, review-cadence comment, exact metrics/log IDs, and deployment drift guards.
  - [x] Reconcile only the stale namespace, StatefulSet/pod/PVC, authentication, physical-consistency, and navigation examples in `backup-restore.md` and `disaster-recovery.md`; parameterize safe values, distinguish the still-current `redis` Service/container/component from stale StatefulSet references, and validate commands against current production manifests.
  - [x] Require intake quiescence/workflow drain or a supported atomic group snapshot, completed Redis persistence status, no copying from a live mutating AOF directory, paired Redis/FalkorDB snapshot IDs/timestamps, verification, and controlled intake resume.
  - [x] Cross-link each new procedure to the minimum canonical related sources and validate every changed relative Markdown link.

- [x] **Task 9 — Add the runbook-set contract test** (AC: 1, 8, 9)
  - [x] Add `OperationalRunbookSetTests.cs` beside the existing deployment contracts; reuse repo-root discovery and Shouldly patterns, with no Docker, network, new library, or duplicate parser product.
  - [x] Verify the six-file required-new set without rejecting existing operations docs, headings, backlinks, relative links, no unfinished/unsafe examples, and critical code/manifest/metric/route anchors. Use structured/anchored checks and small source ties rather than restating entire runbooks in assertions.
  - [x] Keep `DeploymentConfigurationContractTests` green and add only this one C# type/file.

- [x] **Task 10 — Execute and record every checkpoint** (AC: 9, 10)
  - [x] Build `Hexalith.Memories.Server.Tests.csproj` in Release, then run the focused xUnit v3 assembly classes `OperationalRunbookSetTests` and `DeploymentConfigurationContractTests` with `DiffEngine_Disabled=true`.
  - [x] Run the normal Docker-free Server test project/lane and `dotnet build Hexalith.Memories.slnx --configuration Release -m:1 /nodeReuse:false`; record counts and any legitimate baseline change without copying Story 26.4's old count as a new result.
  - [x] Run `git diff --check`, resolve every Markdown link in the changed set, confirm the file-scope/forbidden-scope contract, and complete every evidence-table owner/status/date cell before moving the story to review.

### Review Findings

- [x] [Review][Patch] [High] Publish a source-backed synthetic reference corpus with reusable measured sizing coefficients for NFR14 pre-provision estimates [`docs/operations/capacity-planning.md:9`]
- [x] [Review][Patch] [Medium] Last-verified provenance points to a revision where none of the three runbooks existed [`docs/operations/capacity-planning.md:5`]
- [x] [Review][Patch] [High] Required whole-window Kubernetes and actual PVC-used-byte evidence is not collected by the procedure [`docs/operations/capacity-planning.md:63`]
- [x] [Review][Patch] [High] Capacity collection omits the natural-language vector family and measures base indexes instead of active migration aliases [`docs/operations/capacity-planning.md:37`]
- [x] [Review][Patch] [Medium] A single arbitrary Redis key cannot establish the required distributions and the sampling command aborts for an empty family [`docs/operations/capacity-planning.md:145`]
- [x] [Review][Patch] [Medium] Delta calculations lack guards for zero denominators, shared-backend noise, and totals below the float32 lower bound [`docs/operations/capacity-planning.md:154`]
- [x] [Review][Patch] [High] Health collection aborts on expected HTTP 503 responses, omits status codes, and samples only one of two Server pods [`docs/operations/incident-response.md:63`]
- [x] [Review][Patch] [Medium] A complete single-tenant outage without a safe unaffected path has no severity classification [`docs/operations/incident-response.md:20`]
- [x] [Review][Patch] [Medium] The current restore error inventory and its claimed operator-message source are stale and inconsistent [`docs/operations/incident-response.md:86`]
- [x] [Review][Patch] [High] Degraded-axis containment relies on voluntary callers and has no composed or fail-closed fallback [`docs/operations/incident-response.md:104`]
- [x] [Review][Patch] [High] Rollout undo can recreate pods with the same failing mutable ConfigMap or Secret configuration [`docs/operations/incident-response.md:112`]
- [x] [Review][Patch] [Medium] Consistency inspection defaults to an unrelated canary unit instead of requiring a reported discrepancy ID [`docs/operations/index-rebuild.md:87`]
- [x] [Review][Patch] [High] Missing natural-language projections have no supported recovery route through Paths A or C despite required all-axis parity [`docs/operations/index-rebuild.md:54`]
- [x] [Review][Patch] [High] Path C contradicts the clean-target restore contract by describing logical import as additive and safe to abandon [`docs/operations/index-rebuild.md:74`]
- [x] [Review][Patch] [High] Physical restore's all-tenant blast radius and the runbook's case-level blast radius are not disclosed [`docs/operations/index-rebuild.md:10`]
- [x] [Review][Patch] [High] Path D permits deletion with merely recorded active workflows and gives no bounded deletion failure/timeout route [`docs/operations/index-rebuild.md:130`]
- [x] [Review][Patch] [High] Lifecycle submit snippets can advance without a contract-valid 202, returned Location, and non-empty workflow ID [`docs/operations/tenant-onboarding-offboarding.md:74`]
- [x] [Review][Patch] [High] Provisioning and deletion polling are neither executable nor concretely bounded [`docs/operations/tenant-onboarding-offboarding.md:92`]
- [x] [Review][Patch] [High] Offboarding absence checks omit deletion-owned dedup and legacy natural-language vector key families [`docs/operations/tenant-onboarding-offboarding.md:147`]
- [x] [Review][Patch] [High] Offboarding permits recording active workflows instead of requiring a safe terminal disposition before deletion [`docs/operations/tenant-onboarding-offboarding.md:120`]
- [x] [Review][Patch] [High] Single-tenant recovery suggests a shared-backend physical restore without requiring isolated extraction or all-tenant recovery approval [`docs/operations/tenant-onboarding-offboarding.md:181`]
- [x] [Review][Patch] [High] Compensation-failed onboarding has no orphan-axis inventory and supported cleanup/recovery procedure [`docs/operations/tenant-onboarding-offboarding.md:177`]
- [x] [Review][Patch] [High] Render and diff preflight can destroy or trust an invalid rollback artifact and does not handle render/diff failures safely [`docs/operations/upgrade-migration.md:45`]
- [x] [Review][Patch] [High] The documented canary applies the complete production render and defines no partial-apply recovery branch [`docs/operations/upgrade-migration.md:115`]
- [x] [Review][Patch] [High] Reapplying the complete previous production render is mislabeled as stateless rollback [`docs/operations/upgrade-migration.md:162`]
- [x] [Review][Patch] [Medium] Rollout waits have no bounded timeout or timeout evidence path [`docs/operations/upgrade-migration.md:122`]
- [x] [Review][Patch] [High] Completion permits an undefined accepted degraded capability state that bypasses the healthy-canary gate [`docs/operations/upgrade-migration.md:149`]
- [x] [Review][Patch] [Medium] Workflow history operations assume Dapr 1.18 instead of the exact discovered runtime and CLI versions [`docs/operations/upgrade-migration.md:63`]
- [x] [Review][Patch] [High] Restore rehearsal does not require the exact target versions and data layout for a data-layout migration [`docs/operations/upgrade-migration.md:107`]
- [x] [Review][Patch] [High] Search p95 hard-target alerts are not gated by the NFR workload conditions and a qualifying sample floor [`docs/operations/monitoring-alerting-thresholds.md:81`]
- [x] [Review][Patch] [High] The ingestion hard-target alert measures scheduled documents rather than completed memory units and leaves the per-second-to-per-minute conversion implicit [`docs/operations/monitoring-alerting-thresholds.md:82`]
- [x] [Review][Patch] [Medium] The freshness hard-target alert lacks an executable derived signal and an explicit provider-degradation classification [`docs/operations/monitoring-alerting-thresholds.md:83`]
- [x] [Review][Patch] [High] The rate-limit ratio names no real accepted-request denominator [`docs/operations/monitoring-alerting-thresholds.md:87`]
- [x] [Review][Patch] [Medium] The ingestion failure ratio counts scheduling outcomes but is presented as end-to-end ingestion failure [`docs/operations/monitoring-alerting-thresholds.md:86`]
- [x] [Review][Patch] [Medium] The natural-language retry-stall alert names no source-backed progress signal [`docs/operations/monitoring-alerting-thresholds.md:89`]
- [x] [Review][Patch] [Medium] Most alert rows omit the required linked response procedure [`docs/operations/monitoring-alerting-thresholds.md:79`]
- [x] [Review][Patch] [Medium] Escalation guidance redacts workflow IDs required for protected Dapr incident correlation [`docs/operations/monitoring-alerting-thresholds.md:150`]
- [x] [Review][Patch] [High] Prove at least one active semantic chunk per memory unit in `verify-backup-recovery.py` and its tooling tests; aggregate `{tenant}:vec:*` counts include multiple chunks plus legacy-NL/staging keys and cannot certify per-unit recovery. Scope expansion approved during code review. [`docs/operations/backup-restore.md:322`]
- [x] [Review][Patch] [High] Compose returned root-relative restore status locations with `MEMORIES_BASE_URL` before polling in both logical-recovery procedures [`docs/operations/backup-restore.md:264`]
- [x] [Review][Patch] [High] Generate a lowercase DNS-compatible recovery ID before using it in Kubernetes `VolumeSnapshot` names [`docs/operations/backup-restore.md:70`]
- [x] [Review][Patch] [High] Restore every required production Secret and config input before requiring the full-cluster topology to become healthy [`docs/operations/disaster-recovery.md:175`]
- [x] [Review][Patch] [High] Prevent single-backend PVC recovery from mixing one old snapshot with the surviving backend's newer timeline; require the paired boundary or proof of exact equivalence [`docs/operations/disaster-recovery.md:69`]
- [x] [Review][Patch] [High] Verify physical recovery against pre-loss recovery-point evidence instead of a fresh export from the potentially truncated recovered state [`docs/operations/disaster-recovery.md:254`]
- [x] [Review][Patch] [High] Wire the primary CLI backup commands to `HEXALITH_MEMORIES_ENDPOINT` and `HEXALITH_MEMORIES_API_TOKEN` so the declared URL/token select the intended environment [`docs/operations/backup-restore.md:56`]
- [x] [Review][Patch] [High] Reject stale quiescence evidence by parsing and bounding `capturedAt`, not merely checking that it is a string [`docs/operations/backup-restore.md:81`]
- [x] [Review][Patch] [High] Validate export checksums and one recovery-point identity before the full-cluster loop restores a directory of tenant/case exports [`docs/operations/disaster-recovery.md:209`]
- [x] [Review][Patch] [High] Verify every restored tenant before intake resumes instead of checking only the separately required uppercase `$TENANT` [`docs/operations/disaster-recovery.md:254`]
- [x] [Review][Patch] [Medium] Make the optional case export conditional or require `CASE`; the current `set -u` procedure aborts a tenant-only backup [`docs/operations/backup-restore.md:114`]
- [x] [Review][Patch] [Medium] Persist terminal restore-status bodies instead of deleting the evidence that both runbooks require operators to retain [`docs/operations/backup-restore.md:264`]
- [x] [Review][Patch] [Medium] Capture the bound `VolumeSnapshotContent` and provider snapshot identity needed to recover after control-plane loss [`docs/operations/backup-restore.md:224`]
- [x] [Review][Patch] [Medium] Replace the unsupported automatic-sounding NL replay/re-index recovery claim with the actual original-source republication or re-ingestion path [`docs/operations/backup-restore.md:34`]
- [x] [Review][Patch] [Medium] Validate Markdown fragments and reference-style links instead of stripping anchors and parsing inline links only [`tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs:59`]
- [x] [Review][Patch] [Medium] Enforce preservation of the existing operations-document baseline, not only existence of the six new runbooks [`tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs:72`]
- [x] [Review][Patch] [Medium] Validate each `redis-cli` invocation independently so option reordering cannot bypass the credential-in-argv guard [`tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs:175`]

## File Scope

Allowed files for this story:

- `docs/operations/capacity-planning.md` - NEW. Measurement-first capacity and sizing procedure.
- `docs/operations/incident-response.md` - NEW. Severity, triage, containment, recovery, and escalation procedure.
- `docs/operations/index-rebuild.md` - NEW. Supported-path decision tree for consistency, migration, restore, and re-ingestion.
- `docs/operations/tenant-onboarding-offboarding.md` - NEW. Authenticated tenant lifecycle procedure.
- `docs/operations/upgrade-migration.md` - NEW. Compatibility, rollout, verification, and rollback/recovery procedure.
- `docs/operations/monitoring-alerting-thresholds.md` - NEW. Actual-signal alert threshold and tuning guidance.
- `docs/operations/deployment-configuration.md` - UPDATE. Preserve the maintained deployment contract and add direct links to all six runbooks.
- `docs/operations/failure-recovery.md` - UPDATE. Preserve retry/registry/metric/re-ingestion/log semantics and add direct links to all six runbooks; reconcile only text made stale by current reviewed recovery behavior.
- `docs/operations/backup-restore.md` - UPDATE only for current production resource/authentication correctness and incident/rebuild/upgrade navigation.
- `docs/operations/disaster-recovery.md` - UPDATE only for current production resource/authentication correctness and incident/rebuild/upgrade navigation.
- `tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs` - NEW. Docker-free inventory, structure, link, safety, and source-drift contract.
- `tools/verify-backup-recovery.py` - UPDATE by approved review decision. Prove active semantic coverage per memory unit instead of relying on an aggregate vector-key count.
- `tests/tooling/production_deployment_evidence/backup_recovery_verifier_test.py` - UPDATE by approved review decision. Cover per-unit semantic recovery failure.
- `_bmad-output/implementation-artifacts/26-5-operational-runbook-set.md` - UPDATE. Record evidence, completion notes, and final file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad status transitions.

Read/verify only unless a live mismatch blocks an acceptance criterion and is explicitly escalated:

- `deploy/kubernetes/**`, `deploy/dapr/**`, `deploy/grafana/**`
- `docs/dev/health-checks.md`, `docs/dev/telemetry.md`, `docs/dev/consistency.md`, `docs/dev/eventstore-integration.md`, `docs/dev/release-runbook.md`
- `docs/operations/embedding-providers.md`, `pipeline-persistence.md`, `rate-limiting.md`, `route-surface.md`
- `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs`
- `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs`
- Tenant lifecycle, consistency/index, and workflow replay-safety implementation files cited below
- `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs`
- `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `Hexalith.Memories.slnx`

Forbidden by default:

- Product source or API/route behavior changes
- Deployment/Dapr/Grafana manifests, alert-rule infrastructure, dashboards, resource requests/limits, probes, images, or PVC definitions
- Package/version pins, project files, solution/test inventories, or test-runner/CI changes
- Redis/Falkor schemas, migration behavior, workflow/actor state, benchmark assertions/data/results, or generated evidence
- `references/**` contents or submodule pointers
- Generated `TestResults/**`, `bin/**`, `obj/**`, coverage, package, export, backup, snapshot, or secret artifacts

## Dev Notes

### Non-negotiable operational contract

- Treat every procedure as an executable safety boundary: name authorization, blast radius, preconditions, observable success, stop condition, rollback/recovery, and safe escalation evidence. “Command exited 0” alone is not verification.
- Keep scope visible in examples. Use shell variables such as `$NAMESPACE`, `$TENANT_ID`, `$CASE_ID`, and `$WORKFLOW_ID`; initialize them from current sources and print/confirm non-secret values before a destructive step.
- Never print, inline, or copy credentials. Kubernetes Secrets are base64-encoded objects, not an encryption guarantee; commands must use the deployed secret path/pod environment or an approved secret manager without echoing values.
- EventStore remains truth for source events; Redis/FalkorDB are projections. Do not advise manual backend edits, replay-history mutation, or a generic rebuild that bypasses Dapr workflow/actor and tenant-isolation ownership.
- Preserve degraded capability semantics. Backend-only readiness failures may be `Degraded` with HTTP 200; parse JSON `status`/entries and affected capabilities. `/alive` is restart-oriented; `/ready` is traffic/capability-oriented; `/health` is the union diagnostic.
- Make missing/stale/conflicting data explicit. NoData, OTLP outage, pending workflow, accepted test skip, or missing benchmark evidence is not healthy evidence.
- The UX input contributes recoverable-trust/readability rules only: show scope, source, reasoning, state, and next safe action. There is no web, Fluent UI, accessibility-component, package, or submodule implementation in this story.

### Current source-of-truth anchors

- Production is Kustomize under `deploy/kubernetes/overlays/production`; namespace `hexalith-memories`; images `registry.hexalith.com/memories` and `registry.hexalith.com/memories-mcp`; Server Dapr app-id default `memories`, MCP `memories-mcp`. Do not copy the architecture document's stale `memories-server` projection.
- Committed bootstrap resources: Server app 500m/512Mi request and 2 CPU/2Gi limit; Server daprd 250m/256Mi and 1 CPU/512Mi; MCP app 100m/128Mi and 500m/512Mi; MCP daprd 100m/128Mi and 500m/256Mi; Redis/FalkorDB each 500m/1Gi and 2 CPU/4Gi; Redis PVC 20Gi, FalkorDB PVC 10Gi. These are deployed starting points, not capacity recommendations.
- Application startup probes allow 60 seconds; Redis/FalkorDB startup probes allow 300 seconds. `/ready` backend degradation remains HTTP 200. Tests should tie these facts to manifests/health docs instead of duplicating every literal in six runbooks.
- `MEMORIES_EVENTSTORE_TOPIC` has no runtime fallback outside AppHost injection. A healthy pod with zero intake must include this configuration branch.
- Tenant create accepts `tenantId`, `displayName`, and `vectorDimensions` (default 768, validated 1-4096); create/delete return 202 and a workflow/status location. Use `MemoriesRoutes` and endpoint code at implementation time rather than copying stale `route-surface.md` provenance text.
- Tenant delete removes tenant indexes/staging indexes, graph data, tenant state/event-store/migration keys, and registry entry, but not external Kubernetes/Dapr embedding secrets. Retire those only after deletion verification.
- `memories.index.size` counts documents. Byte capacity comes from Redis/FalkorDB/Kubernetes/persistence evidence. Metrics must not add case/user/unit identifiers as high-cardinality tags.
- The committed dashboard is `deploy/grafana/dashboards/memories-operability.json`; no committed alert rules were found at story creation. Story 26.5 documents alert ownership/thresholds but creates no alert automation.

### Runbook-specific implementation guidance

**Capacity.** Use `INFO memory`, `INFO persistence`, representative `MEMORY USAGE`, `FT.INFO` (`vector_index_sz_mb`, `total_index_memory_sz_mb`, document/index failures), and `GRAPH.MEMORY USAGE <graph> [SAMPLES n]` (`total_graph_sz_mb`, indices and component breakdown) together with pod/PVC utilization. Float32 payload is only a lower-bound component. Include ingestion/persistence rewrite and restore headroom. Higher HNSW parameters and indexed fields cost memory; measure the repository's actual schema rather than publishing a generic multiplier.

**Incident.** Start read-only, identify scope, capture bounded redacted logs/correlation/workflow IDs and safe error codes, then contain. Tenant-isolation, cross-tenant data, or secret-exposure suspicion is critical. Link to backend-specific recovery and retain unaffected axes where the health contract permits; restore only after diagnosis/backup. Include current import/restore error families but source their exact names from the error catalog during implementation.

**Index rebuild.** Consistency verify/inspect is not full history replay. Supported repair re-merges graph projections and removes orphan semantic/graph data; it does not recreate a missing syntactic record, and semantic re-index remains unsupported. Provider/dimension changes use `tools/MigrateEmbeddingVectors` with dry-run/live/resume/abort/rollback and blue/green ownership. Full history re-publication requires the documented delete/recreate/dedup constraints; no `forceReplay` bypass exists. Never destroy the active index before backup, canary, comparison, and rollback evidence.

**Tenant lifecycle.** Provisioning and deletion are asynchronous, compensating/idempotent workflows. Do not infer success from the initial 202. For provisioning, confirm terminal workflow success and an independently read `Active` tenant before handoff. For deletion, the status route can become `TENANT_NOT_FOUND` after successful registry removal; never wait for a `Deleted` tenant state, and accept 404 as completion only after independent backend/state/access checks. Onboard only after identity, dimensions/provider, quotas, physical isolation, canary ingestion/search, and telemetry verify. Offboard only after retention/legal approval and export/consistent backup; cross-references in other tenants and telemetry are not automatically a tamper-evident compliance record.

**Upgrade.** Reuse `docs/dev/release-runbook.md` and the current Kustomize flow. Quiesce publishers/intake and drain in-flight work before route/workflow-shape changes because the current replay-safety startup gate may fail open after a five-minute timeout or registry/Dapr query error. Dapr workflow instance reuse/versioning and purge behavior must be checked against the deployed runtime. A Pod rollback cannot reverse durable data.

**Monitoring.** Use actual `MemoriesMeter` names and the dashboard's normalized Prometheus names. Every alert needs an owner, severity, window, NoData behavior, link, and verification. Prefer “queue nonzero/rising plus no successful progress” to a universal depth. Mark infrastructure recommendations provisional until a measured baseline exists; do not claim Grafana panels are alerts.

### Testing requirements

- C# files use the ITANEO MIT header, file-scoped namespace `Hexalith.Memories.Server.Tests.Deployment`, one type per file, xUnit v3 `[Fact]`, Shouldly, PascalCase, nullable/warnings-as-errors, and the existing `Hexalith.Memories.slnx` repo-root marker walk.
- Parse direct Markdown link targets and headings sufficiently to distinguish actual links/sections from comments or unrelated substrings. Resolve anchors/files relative to each document and reject traversal outside the repository. External `https://` sources need not be fetched by tests.
- Include negative assertions for unfinished authoring markers, empty/example passwords, secret-value output, stale production resource roles, and nonexistent tenant lifecycle CLI examples without rejecting initialized non-secret shell variables, the current bare `redis` Service/container/component, or explanatory prose that warns about a limitation.
- Tie a small set of source-backed values bidirectionally where practical: production namespace from Kustomize, lifecycle paths from `MemoriesRoutes`, metric identifiers from `MemoriesMeter`, dashboard existence/path, and health status wording. Do not freeze complete command blocks or mutable prose in tests.
- Focused verification after a Release build:

  ```bash
  DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll \
    -class Hexalith.Memories.Server.Tests.Deployment.OperationalRunbookSetTests \
    -class Hexalith.Memories.Server.Tests.Deployment.DeploymentConfigurationContractTests \
    -parallel none -noLogo
  ```

- Then run the Server test project through its normal per-project runner and the Release `.slnx` build. Do not run solution-level `dotnet test`; follow repository per-project test guidance and record real counts.

### Previous story and Git intelligence

- Story 26.4 established fail-closed, source-tied evidence: absence/malformed/zero-test evidence fails, generated outputs are not source, and gates must not be weakened to manufacture green results. Its benchmark remains truthfully below the PRD line at the last recorded 75%; Story 26.5 must not claim all Epic 26 quality evidence is green.
- Recent commits completed coverage/package automation, current error-catalog additions, Zot image/auth alignment, and restart/replay hardening. Use the current revision after rebase and preserve those contracts. The documentation commit should use a docs-oriented conventional subject such as `docs: add operational runbook set`.

### Latest technical information (verified 2026-07-14)

- Repository pins win over current release availability: .NET SDK 10.0.301, Aspire 13.4.6, Dapr .NET packages 1.18.4, Redis Stack 7.4.0-v8, FalkorDB 4.12.0, OpenTelemetry 1.16.0, xUnit v3 3.2.2, and Shouldly 4.3.0. No version change belongs to this story.
- Dapr's current workflow guidance requires deterministic orchestrators and deliberate workflow version/patch handling; instance-ID reuse behavior changed in Dapr 1.18 and old state may require purge before reuse. Validate the deployed runtime/CRDs and perform incremental supported upgrades; do not infer runtime version from the SDK package alone.
- Kubernetes version is not pinned in this repository. Record the target cluster/server and kubectl/component skew during upgrade preflight. VolumeSnapshot depends on CSI/controller/CRDs, so retain a logical-export fallback.
- Redis Stack 7.4.0-v8 is the final Stack line and its maintenance has ended; Redis 8 is an upgrade-planning concern, not permission for an image change. Redis `everysec` AOF can lose roughly one second under catastrophic host loss; document the configured durability boundary honestly.
- FalkorDB's current releases are ahead of the pinned 4.12.0 image. Treat any image/data-layout change as a staged backup/restore/search-parity migration. `GRAPH.MEMORY` is a sampled production-capacity aid; higher sample counts cost more work.
- Prometheus alert guidance favors actionable, low-noise alerts linked to runbooks, and Grafana requires explicit missing-data behavior. A dashboard without alert rules is observability, not paging.

### Project Structure Notes

- The six files belong in the existing flat `docs/operations/` operator collection; do not create a second runbook tree or UI copy.
- The one documentation contract belongs beside `DeploymentConfigurationContractTests.cs` because it extends the same deployment/operations source-tie and is already included in the Docker-free Server test project.
- Keep canonical deep behavior in existing developer/operation sources and link to it. New runbooks own operator decision order, evidence, safety, and navigation—not duplicate API/schema/workflow specifications.
- Architecture/PRD version tables and topology prose contain known stale values. Current manifests, source constants, dependency props, and root/submodule `project-context.md` instructions are authoritative.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-265-Operational-Runbook-Set]
- [Source: _bmad-output/implementation-artifacts/epic-26-context.md#Constraints]
- [Source: _bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements]
- [Source: _bmad-output/planning-artifacts/architecture.md#Deployment-Architecture]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Recoverable-Trust]
- [Source: _bmad-output/implementation-artifacts/26-4-coverage-gating-and-benchmark-lane.md#Dev-Notes]
- [Source: docs/operations/deployment-configuration.md]
- [Source: docs/operations/failure-recovery.md]
- [Source: docs/operations/backup-restore.md]
- [Source: docs/operations/disaster-recovery.md]
- [Source: docs/operations/embedding-providers.md#Embedding-Migration-Runbook]
- [Source: docs/operations/pipeline-persistence.md]
- [Source: docs/operations/rate-limiting.md]
- [Source: docs/operations/route-surface.md]
- [Source: docs/dev/health-checks.md#Endpoint-Summary]
- [Source: docs/dev/telemetry.md#Metrics]
- [Source: docs/dev/consistency.md]
- [Source: docs/dev/eventstore-integration.md]
- [Source: docs/dev/release-runbook.md]
- [Source: deploy/kubernetes/overlays/production/kustomization.yaml]
- [Source: deploy/kubernetes/base/server-deployment.yaml]
- [Source: deploy/kubernetes/base/mcp-deployment.yaml]
- [Source: deploy/kubernetes/base/redis-statefulset.yaml]
- [Source: deploy/kubernetes/base/falkordb-statefulset.yaml]
- [Source: deploy/grafana/dashboards/memories-operability.json]
- [Source: src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs]
- [Source: src/Hexalith.Memories.Telemetry/MemoriesMeter.cs]
- [Source: src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs]
- [Source: src/Hexalith.Memories.Server/Endpoints/TenantLifecycleEndpoints.cs]
- [Source: src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs]
- [Source: src/Hexalith.Memories.Server/Workflows/TenantDeletionWorkflow.cs]
- [Source: src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs]
- [Official: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/]
- [Official: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-versioning/]
- [Official: https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-upgrade/]
- [Official: https://kubernetes.io/releases/version-skew-policy/]
- [Official: https://kubernetes.io/docs/concepts/storage/volume-snapshots/]
- [Official: https://kubernetes.io/docs/concepts/security/secrets-good-practices/]
- [Official: https://redis.io/docs/latest/commands/ft.info/]
- [Official: https://redis.io/docs/latest/commands/memory-usage/]
- [Official: https://redis.io/docs/latest/operate/oss_and_stack/management/persistence/]
- [Official: https://github.com/redis-stack/redis-stack]
- [Official: https://docs.falkordb.com/commands/graph.memory.html]
- [Official: https://docs.falkordb.com/operations/durability/]
- [Official: https://github.com/FalkorDB/FalkorDB/releases/latest]
- [Official: https://prometheus.io/docs/practices/alerting/]
- [Official: https://grafana.com/docs/grafana/latest/alerting/guides/missing-data/]

## Implementation Checkpoint Evidence

Complete every row before moving the story to review. Split the story if one row cannot be reviewed independently within the implementation window.

| Checkpoint | Owner | Validation command/artifact | Review status | Completion date |
|---|---|---|---|---|
| Audit-anchor preflight | Developer / Reviewer | Revision/date plus moved-anchor reconciliation in Completion Notes | pending | - |
| Capacity planning | Developer / Operator reviewer | `capacity-planning.md` + source-tied contract tests | pending | - |
| Incident response | Developer / Incident owner | `incident-response.md` + decision-tree/redaction review | pending | - |
| Index rebuild | Developer / Data owner | `index-rebuild.md` + supported-path/rollback review | pending | - |
| Tenant lifecycle | Developer / Tenant owner | `tenant-onboarding-offboarding.md` + route/destructive-step review | pending | - |
| Upgrade/migration | Developer / Release owner | `upgrade-migration.md` + compatibility/rollback review | pending | - |
| Monitoring/thresholds | Developer / Observability owner | `monitoring-alerting-thresholds.md` + signal/threshold/NoData review | pending | - |
| Navigation/recovery reconciliation | Developer / Operator reviewer | Changed-link resolution + current production command review | pending | - |
| Documentation contract | Developer / Test reviewer | Focused `OperationalRunbookSetTests` and existing deployment contracts | pending | - |
| Final scope and build | Developer / Reviewer | Server lane, Release `.slnx` build, `git diff --check`, file-scope audit | pending | - |

## Dev Agent Record

### Agent Model Used

OpenAI Codex (GPT-5)

### Implementation Plan

- Freeze source-backed operational literals and record every live mismatch before authoring.
- Implement the six runbooks in task order using the shared executable-safety section contract.
- Reconcile navigation and recovery commands only after the new procedures exist.
- Add one Docker-free source-drift contract, then run focused, project, solution, link, scope, and whitespace gates.

### Debug Log References

- 2026-07-14 audit RED: the exact six required runbook files were absent, so the preflight file-set assertion failed as expected.
- 2026-07-14 audit GREEN: source-anchor shell contract passed at revision `1553ee6` for production namespace, lifecycle routes, degraded-health semantics, `memories.index.size`, unsupported semantic repair, and the five-minute fail-open replay gate.
- Checkpoint tracker (review status is implementation self-review; independent peer review remains the next workflow):

  | Checkpoint | Owner | Validation command/artifact | Review status | Completion date |
  |---|---|---|---|---|
  | Audit-anchor preflight | Developer / Reviewer | Revision plus source-anchor shell contract | complete | 2026-07-14 |
  | Capacity planning | Developer / Operator reviewer | `capacity-planning.md` plus source-tied contract tests | complete | 2026-07-14 |
  | Incident response | Developer / Incident owner | `incident-response.md` plus decision-tree/redaction review | complete | 2026-07-14 |
  | Index rebuild | Developer / Data owner | `index-rebuild.md` plus supported-path/rollback review | complete | 2026-07-14 |
  | Tenant lifecycle | Developer / Tenant owner | `tenant-onboarding-offboarding.md` plus route/destructive-step review | complete | 2026-07-14 |
  | Upgrade/migration | Developer / Release owner | `upgrade-migration.md` plus compatibility/rollback review | complete | 2026-07-14 |
  | Monitoring/thresholds | Developer / Observability owner | `monitoring-alerting-thresholds.md` plus signal/threshold/NoData review | complete | 2026-07-14 |
  | Navigation/recovery reconciliation | Developer / Operator reviewer | Changed-link resolution plus production command review | complete | 2026-07-14 |
  | Documentation contract | Developer / Test reviewer | Focused operational and deployment contract tests: 11 passed | complete | 2026-07-14 |
  | Final scope and build | Developer / Reviewer | Server 2,629 total/0 failed/1 skipped; Release builds 0 warnings/errors; clean diff/scope gates | complete | 2026-07-14 |

### Completion Notes List

- Implementation started 2026-07-14 at `1553ee6708f644f3a4bc3638d3aaceed682b2371`; the story baseline remains `e574c313ae067b72ac7f69dd3061099c1a425466`.
- Story 26.2 is present at `review` and its current restore/recovery source was adopted. Story artifacts 26.6 and 26.7 are absent from this checkout although sprint tracking marks both `review`; their sprint comments and current checked-in CI/runtime sources are used without claiming their external evidence is present.
- Live replacements for stale planning prose: production namespace `hexalith-memories`; StatefulSets/pods `redis-stack`/`redis-stack-0` and `falkordb`/`falkordb-0`; generated PVCs `data-redis-stack-0` and `data-falkordb-0`; Server Dapr app-id `memories`; canonical lifecycle paths from `MemoriesRoutes`; health JSON semantics from `health-checks.md`; metric names from `MemoriesMeter`; consistency limitations from `SemanticIndexer`/repair workflow; replay behavior from `WorkflowReplaySafetyHostedService`.
- The required authoring contract is frozen at exactly six filenames and the eight exact second-level headings listed in AC1. Current code/manifests and project context take precedence over architecture version tables and missing review artifacts.
- Capacity planning now publishes the bootstrap resource/PVC values as source-backed configuration, a classified NFR14 worksheet, per-unit/per-vector/per-tenant delta method, authenticated backend measurement commands, persistence-rewrite headroom, and evidence-based scale/stop decisions without claiming HPA, backend redundancy, or linear scalability.
- Incident response now defines severity/timing/roles, read-only first response, structured health parsing, tenant/backend blast-radius containment, all required degradation and stalled-ingestion branches, current import/restore error families, redacted evidence, and explicit handoff to owned recovery procedures without direct state edits.
- Index recovery now uses a four-path decision matrix covering bounded consistency repair, blue/green migration, logical/physical restore, and destructive reprovision/re-ingestion. It records unsupported semantic repair, missing-syntactic and dedup/no-force-replay limits, chunked vectors, provisioning ownership, canary/parity gates, and retained-target rollback.
- Tenant lifecycle now follows the authenticated asynchronous route contract, distinguishes typed-client 768-default behavior, requires terminal provisioning plus independent Active/isolation/canary proof, and implements freeze/export/paired-backup/delete/404-race/independent-absence/secret-retirement offboarding with failed-state re-trigger and cross-tenant/compliance limits.
- Upgrade/migration now gates on live version/skew/inventory, rendered provenance diff, immutable backup and restore rehearsal, workflow quiescence and replay-version safety, canary/control parity, and observation evidence. It separates stateless rollback from CRD/schema/data recovery, retains PVCs, and treats Redis 8/FalkorDB changes as separate staged migrations.
- Monitoring/thresholds now maps real meter/dashboard/health/infrastructure sources to owner, severity, condition, window, NoData, blast radius, action, and verification. It preserves the exact PRD search/throughput/freshness/startup targets, labels other thresholds provisional, pairs queues with progress, and makes missing telemetry/cardinality/redaction explicit without claiming alert rules are deployed.
- Navigation now exposes all six new runbooks directly from both required anchors. Backup/recovery examples use the production namespace, StatefulSet/pod/PVC identities and in-container secret authentication; coordinated quiescence, completed persistence checks, paired snapshot evidence, quiesced artifact copying, verification, and controlled intake resumption replace the stale live-copy procedure. A focused shell gate resolved every relative link across all ten changed operation documents and rejected stale executable resource examples.
- The Docker-free documentation contract now parses real second-level headings and Markdown links outside code/comments, rejects repository traversal and unresolved targets, guards unfinished/unsafe executable examples, and ties namespace, data-plane resource names, lifecycle routes, emitted metric names, dashboard normalization, and Degraded-to-HTTP-200 semantics to canonical sources. The Release test project builds with zero warnings/errors; the focused new and existing deployment contracts pass 11/11.
- Final verification completed: the focused contracts pass 11/11; the normal Docker-free Server assembly passes 2,628 with one existing skip and zero failures (2,629 total, five more tests than the pre-change lane); both the Server test project and full Release solution build with zero warnings/errors; `git diff --check`, changed-link resolution, CRLF, 13-file declared scope, and forbidden-scope gates pass. The three dirty `references/Hexalith.EventStore`, `references/Hexalith.FrontComposer`, and `references/Hexalith.Tenants` worktrees pre-date this implementation and remain untouched/uncommitted.
- All ten owner/status/date rows are complete in the Dev Agent Record checkpoint tracker, the workflow-permitted execution-evidence location; the story-authoring checkpoint table above remains preserved as immutable context for independent review.
- 2026-07-15 adversarial review chunk 1 closed all 16 findings across capacity planning, incident response, and index rebuild. The source-backed synthetic coefficient, full-window/PVC evidence, active raw/NL index measurement, fail-closed incident containment, current restore errors, clean-target/all-tenant recovery boundaries, NL limitations, and bounded destructive-workflow gates are now explicit. Release Server.Tests build: 0 warnings/errors; focused operational/deployment contracts: 12/12; `git diff --check`: clean. Story status remains `review` because the remaining runbook/navigation/test chunks were deliberately deferred to follow-up review runs.
- 2026-07-16 adversarial review chunk 2 closed all 21 findings across tenant lifecycle, upgrade/migration, and monitoring/alerting. Accepted lifecycle requests and bounded polling now fail closed; deletion verification covers source-owned keys and shared-backend recovery is isolated; rollout/render/rollback are staged and scoped; alerts use semantically valid signals and direct response links. Release Server.Tests build: 0 warnings/errors; focused operational/deployment contracts: 13/13; Docker-free Server lane: 2,661 passed, 1 skipped, 0 failed (2,662 total); `git diff --check` and shell syntax checks: clean. Story status is `in-progress` because chunk 3 (navigation/recovery/test/tracking review) remains pending.
- 2026-07-16 adversarial review chunk 3 closed all 17 accepted findings across backup/recovery execution, verifier fidelity, Markdown navigation, operations-document preservation, and credential guards; one speculative `BGSAVE` race finding was dismissed. Recovery now uses a portable immutable manifest, paired backend snapshots, pre-loss per-tenant baselines, retained status evidence, bounded quiescence evidence, and per-memory-unit active semantic verification. Verifier unit tests pass 8/8; focused operational/deployment contracts pass 15/15; the Docker-free Server lane passes 2,663 with 1 skipped and 0 failed (2,664 total); Server.Tests and the Release solution build with 0 warnings/errors; all 15 changed-runbook Bash blocks parse; Python compilation and `git diff --check` are clean. Story status is `done`.

### File List

- docs/operations/capacity-planning.md
- docs/operations/incident-response.md
- docs/operations/index-rebuild.md
- docs/operations/tenant-onboarding-offboarding.md
- docs/operations/upgrade-migration.md
- docs/operations/monitoring-alerting-thresholds.md
- docs/operations/deployment-configuration.md
- docs/operations/failure-recovery.md
- docs/operations/backup-restore.md
- docs/operations/disaster-recovery.md
- tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs
- tests/tooling/production_deployment_evidence/backup_recovery_verifier_test.py
- tools/verify-backup-recovery.py
- _bmad-output/implementation-artifacts/26-5-operational-runbook-set.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

## Change Log

- 2026-07-16: Closed adversarial code-review chunk 3 — 17/17 accepted High/Medium findings patched across recovery runbooks, verifier/test scope, navigation, baseline preservation, and credential guards; focused contracts pass 15/15, verifier tests pass 8/8, and the Docker-free Server lane passes 2,663 with 1 skipped and 0 failed. Story completed.
- 2026-07-16: Closed adversarial code-review chunk 2 — 21/21 High/Medium findings patched across tenant lifecycle, upgrade/migration, and monitoring/alerting; focused contracts pass 13/13 and the Docker-free Server lane passes 2,661 with 1 skipped and 0 failed.
- 2026-07-15: Closed adversarial code-review chunk 1 — 16/16 High/Medium findings patched in capacity planning, incident response, and index rebuild; focused contracts pass 12/12.
- 2026-07-14: Added six source-backed operational runbooks, direct navigation, safe production-aligned backup/recovery procedures, and one Docker-free documentation contract. Completed all focused, Server, Release-build, link, whitespace, and scope gates and moved the story to review.
