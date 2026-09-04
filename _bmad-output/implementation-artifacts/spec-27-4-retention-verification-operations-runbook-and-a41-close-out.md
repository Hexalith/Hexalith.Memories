---
title: 'Story 27.4 retention verification, operations runbook, and A41 close-out'
type: 'feature'
created: '2026-09-02'
status: 'in-progress'
baseline_revision: '7fa095591bf9c9479aea7280319216959eaaa25f'
baseline_commit: '7fa095591bf9c9479aea7280319216959eaaa25f'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '_bmad-output/project-context.md'
  - 'docs/dev/adr-27.1-001-access-telemetry-lifecycle.md'
warnings:
  - 'oversized'
deferred: []
---

<intent-contract>

## Intent

**Problem:** Story 27.4 has no production-shaped C2-C6 evidence producers, canonical close-out matrix, or lifecycle operations runbooks, while the required C1 predecessor packet and independent approvals are still absent. A41 must remain open until immutable same-profile evidence proves the deployed lifecycle.

**Approach:** Complete and test every repository-owned producer, fail-closed validator, dashboard, runbook, and close-out guard now. Leave Production disabled and all evidence/closure states pending until authorized operators execute the controlled deployment scenarios and two independent reviewers approve the same immutable `PG-ONPREM-1` profile hash.

## Boundaries & Constraints

**Always:** Reuse the existing Dapr-only lifecycle, immutable profile/capacity primitives, redacted command observations, and structure-aware document guards. Bind every packet to commands, UTC timestamps, source/profile/workload hashes, immutable artifact hashes, owner, and review state. Require explicit nonzero results, exact same-profile equality, zero skips/failures, and tenant/privacy denial before dependencies. Preserve JSON-console/optional-OTLP audit continuity and the assurance limit: bounded infrastructure telemetry, not tamper-evident, append-only, legally compliant, or certified audit retention. Finalize repository-complete work as `awaiting-operator` with non-empty imperative `operator_actions` when live deployment actions or approvals remain.

**Block If:** A repository implementation decision would change the accepted Dapr-only architecture, immutable adapter profile, retention bounds, authority separation, durability/RPO/RTO claim, or A41 closure semantics without an approved governing decision. Missing cluster access, credentials, published images, running-target packets, or human approvals is an operator tail, not a `blocked` outcome.

**Never:** Write or revert `_bmad-output/implementation-artifacts/sprint-status.yaml`; treat its rows as verification; synthesize or share-discharge any C1 gate; contact an unapproved target; expose secrets/raw tenant content; enable Production lifecycle writes; weaken the zero-replica overlay; mutate A41 to resolved; reopen historical Epic 20/Story 20.5 records; or claim physical reclamation from logical deletion, broad health, static manifests, fakes, scheduling, or documentation alone.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Offline repository validation | No cluster credentials or live evidence | Producers, fixtures, docs, dashboard, and guards validate; canonical matrix stays pending | No target query and no A41 mutation |
| Controlled lifecycle proof | Authorized exact-profile target and complete per-checkpoint inputs | Immutable C2-C4 packets capture two writers, replacements/faults, expiry/purge/reclamation, observability, and tenant/privacy denial | Write a secret-safe blocker packet and exit nonzero on any missing, stale, skipped, or mismatched observation |
| Close-out preflight | C0-C5 and terminal checks passed on one profile hash | Emit exhaustive A41 inventory, exact allowed mutation set, and recoverable pre-mutation snapshot | Reject unclassified paths, incomplete approvals, profile drift, or non-passing evidence |
| Postflight or publish drift | Staged tree/commit differs from approved mutation manifest | Keep A41 open and report exact mismatch | Never repair by broad rewrite or by changing historical status |

</intent-contract>

## Code Map

- `_bmad-output/planning-artifacts/epics.md:5018` -- authoritative Story 27.4 predecessor gate and three acceptance criteria; Story 27.7-27.31/C1 evidence remains absent.
- `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:769` -- accepted handoff, immutable `PG-ONPREM-1` profile/hashes, human approval separation, operational scope, and A41 close-out prohibition.
- `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md:81` -- current transfer record assigning former lifecycle proof/runbook/close-out tasks to Story 27.4.
- `tools/verify-access-telemetry-lifecycle.py:23` -- CLI currently accepts only `adapter-profile`; extend with C2-C4 and A41 inventory/preflight/postflight/publish modes while retaining pre-query identity checks.
- `tools/verify_access_telemetry_lifecycle.py:99` -- reuse environment identity, checked capacity, immutable profile hashing, redacted observations, and per-run packet writing; `run_adapter_profile_checkpoint` is intentionally not behavioral proof.
- `tools/verify-access-telemetry-c1.ps1:284` -- reuse immutable, secret-safe packet and source/command hash conventions; C1.15 remains `not-evaluated` until external review.
- `tools/verify-production-deployment.ps1:89` -- reuse context refusal, evidence redaction, fault/recovery polling, and diagnostic patterns without treating the disposable lane as Production proof.
- `tests/tooling/access_telemetry_lifecycle/test_adapter_profile.py:1` -- existing offline fixture pattern; add exact retention/reclamation, failure/privacy, close-out inventory, staged-tree, and publish-integrity cases.
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs:27` -- portable two-writer/fault/continuity baseline; use as lower-layer evidence, never a running-target substitute.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs:23` -- extend structure-aware A41/runbook guards and leaked-markup rejection.
- `deploy/grafana/dashboards/memories-operability.json` -- add bounded, privacy-safe lifecycle panels; no lifecycle panels exist today.
- `deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml:1` -- read-only safety boundary during this run; lifecycle and clock remain zero replicas with an unproven profile.
- `docs/dev/telemetry.md:176` and `docs/operations/*.md` -- project implemented truth into one new neutral runbook, one PostgreSQL appendix, and narrow cross-links instead of duplicating procedures.
- `_bmad-output/implementation-artifacts/deferred-work.md:115` -- canonical open A41 residual plus DW-572/DW-578/DW-579/DW-584 reconciliation inputs; do not resolve before publish verification.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- orchestrator-owned and always read-only; status/action rows are neither defects nor proof.

## Trusted Evidence Contract

- A production checkpoint producer MUST execute, or directly control execution of, every reviewed child command used to decide the checkpoint. It MUST create the command transcript itself and MUST NOT turn operator-authored booleans, counters, timestamps, hashes, commit identifiers, or command-result metadata into `passed` evidence. When no authorized target is available, it may emit only `operator-pending`/`not-evaluated` evidence and must exit nonzero for a requested production proof.
- C1 satisfaction requires the complete canonical 25-gate predecessor evidence or its individually registered immutable successors. A synthetic aggregate packet, count, or shared discharge MUST NOT satisfy the predecessor. Every accepted source commit must resolve locally, every artifact/source hash must be recomputed from the bytes read, and every command/result and observation timestamp must be bounded, ordered, fresh, and nested within the producer-controlled run.
- Parse inputs fail-closed: reject Boolean-as-integer values, non-finite numbers, invalid or non-canonical UTF-8, oversized inputs, unknown fields where the schema is closed, raw/secret-shaped field aliases after case/separator normalization, symlink/path aliases outside the approved root, and labels outside the exact bounded sets (`state`, `reason`, and `outcome`).
- C2 evidence includes both writers, every declared replacement, exact acknowledgements, interruption/fault observations, recovery/continuity, and same-profile attribution. C3 evidence includes cohort, database/schema/table and tuple attribution, executed expiry/purge/reclamation commands, timestamps, older-record removal, newer-record preservation, and logical-versus-physical proof separation. C4 evidence includes every declared dependency failure, application-outage behavior, audit continuity, bounded observability, tenant denial, privacy denial before dependency access, and exact owner/approver attribution.
- Close-out is one immutable chain. Preflight verifies the actual C0-C5/terminal packets and approval bundle by content and recomputed hashes at a clean open commit, then records the exact allowed A41 paths and a recoverable snapshot. Postflight accepts only an authentic preflight packet from this tool, binds its packet hash, source head, snapshot, terminal/inventory inputs and exact index, and rejects any index or worktree drift. Publish verification accepts only that authentic postflight/preflight chain and proves remote containment of the intended branch/commit; a local commit alone is never `published`.
- A41 reconciliation is semantic as well as path-bounded: allowed documents must contain the exact approved close-out transitions, historical Epic 20/Story 20.5 completion must remain byte-stable, and evidence packets are per-run immutable (exclusive creation, no overwrite). Until that chain passes, every canonical matrix checkpoint and A41 source remains pending/open and Production stays disabled.
- Dashboard panels use the runtime metric names and units, expose missing-series as unhealthy/NoData, distinguish capacity from queue depth, and compute evidence age from a true last-evidence timestamp gauge rather than scrape time. Tests bind dashboard queries to the runtime metric contract instead of duplicating a test-local allowlist.
- Verification invokes every CLI mode through a subprocess and covers success plus fail-closed negatives. It includes a complete valid preflight/postflight/publish chain using isolated Git repositories and remotes, as well as tampered packet/hash, stale time, dirty-worktree, staged-tree, wrong-branch, and unpublished-commit rejection cases. Offline tests must never contact an unapproved target.

## Tasks & Acceptance

**Execution:**
- `tools/verify-access-telemetry-lifecycle.py`, `tools/verify_access_telemetry_lifecycle.py` -- add fail-closed C2 production-replacement, C3 retention/reclamation, C4 failure/privacy/observability, exhaustive A41 inventory, close-out pre/postflight, and commit publish-verification contracts; preserve immutable secret-safe evidence and refuse absent predecessors/profile equality.
- `tests/tooling/access_telemetry_lifecycle/` -- add deterministic fixtures covering cohort expiry, interrupted purge recovery, newer-record preservation, physical-reclamation attribution, failure/health precedence, tenant/privacy denial, raw/secret rejection, exhaustive worktree/index inventory, exact mutation sets, staged-tree equality, and commit equality.
- `docs/operations/access-telemetry-lifecycle.md`, `docs/operations/access-telemetry-adapter-production.md` -- publish the container-neutral lifecycle runbook and exact PostgreSQL 18.4 appendix covering ownership, configuration/default/min/max, capacity/cost, monitoring/alarms/NoData, purge proof, incidents, recovery, rollback, RPO/RTO limits, reclamation, key rotation, and decommissioning.
- `docs/dev/telemetry.md`, `docs/operations/{deployment-configuration,capacity-planning,monitoring-alerting-thresholds,incident-response,failure-recovery,disaster-recovery,upgrade-migration,rate-limiting}.md` -- add narrow lifecycle projections/cross-links and explicitly reconcile the rate-limit obligation without changing correct Story 20.5 quota behavior.
- `deploy/grafana/dashboards/memories-operability.json` -- add privacy-safe lifecycle queue, retry, clock/attestation, capacity, expiry/purge, reclamation-age, health, and profile-drift panels using bounded labels.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs`, `tests/Hexalith.Memories.Server.Tests/Deployment/AccessTelemetryOperationsContractTests.cs`, `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryA41CloseOutTests.cs`, `tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesDashboardTests.cs` -- bind exact runbook sections, dashboard queries, open-until-published A41 wording, canonical evidence schema, and anti-corruption rules.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` -- create the single canonical C0-C6 matrix with repository validations recorded and external observations/approvals explicitly pending; never represent templates as executed evidence.
- `_bmad-output/implementation-artifacts/deferred-work.md` and other verifier-discovered A41 summaries -- reconcile wording only if permitted by the close-out preflight; without complete live evidence, preserve `carried-forward`/open semantics and record no closure.

**Acceptance Criteria:**
- Given an offline checkout with no cluster credentials, when focused repository tests run, then every producer/validator, runbook contract, dashboard binding, and canonical-matrix schema passes without querying a target or changing A41/Production state.
- Given any incomplete predecessor set, profile drift, partial/secret-shaped observation, skipped fault, absent tenant denial, missing approval, or tree/commit mismatch, when a verification mode runs, then it emits a bounded blocker packet, exits nonzero, and leaves Production disabled and A41 open.
- Given the authorized immutable profile and a production-shaped deployment, when operators run the documented C2-C4 scenarios, then immutable evidence can prove exact acknowledgement, two-writer/replacement recovery, expiry/purge/reclamation, newer-record and audit continuity, bounded observability, and tenant/privacy denial before dependencies.
- Given C0-C5, terminal validation, and two independent same-hash approvals, when close-out preflight/postflight/publish verification run, then only the exact A41 mutation set is eligible and historical Epic 20/Story 20.5 completion remains unchanged.

## Spec Change Log

- 2026-09-02 — Review loop 1 replaced the self-attested evidence design with a trusted-producer contract. Production modes now have to execute controlled commands or remain `operator-pending`; all source and artifact identity is recomputed; C1 means the complete 25-gate predecessor; C2-C4 schemas carry the scenario-specific proof; and A41 preflight, postflight, and remote publish verification form one immutable chain. The amendment also makes evidence writes non-overwriting, strengthens input/privacy/path parsing, binds dashboard semantics to runtime metrics, and requires subprocess plus isolated-Git end-to-end tests. Known-bad approaches avoided: accepting operator-authored pass flags, treating a synthetic C1 summary as discharge, snapshotting an already-mutated tree, accepting hand-written chain packets, equating a local commit with publication, or deriving evidence age from scrape time. KEEP: `sprint-status.yaml` read-only; Production disabled and A41 open; neutral runbook plus PostgreSQL appendix; bounded privacy-safe labels; canonical pending-state matrix; no live target, secret, or historical-record mutation during repository validation.

## Review Triage Log

- Review loop 1 — `intent_gap: 0`; `bad_spec: 9` (`high: 8`, `medium: 1`); `patch: 0`; `defer: 0`; `reject: 4` (`low: 4`). Addressed findings: (1, high) checkpoint decisions relied on self-attested JSON and an incomplete C1 aggregate; (2, high) source/artifact/command/time/type/privacy provenance was not independently verified; (3, high) C2-C4 omitted required scenario attribution and observations; (4, high) preflight did not authenticate the complete evidence/approval chain or start from a clean tree; (5, high) postflight did not bind an authentic preflight snapshot, index, and worktree; (6, high) publish verification bypassed the chain and treated a local commit as published; (7, high) evidence could be overwritten and A41 content reconciliation was only path-based; (8, medium) dashboard age/capacity/NoData semantics were not bound to runtime metrics; (9, high) CLI and close-out chain coverage lacked subprocess, negative, and remote-publication tests. Rejected as non-defects: the workflow-transient `in-review` status, absence of final operator actions before finalization, unchanged orchestrator bookkeeping, and minor wording/style observations made moot by the redesign. KEEP: the read-only sprint-status boundary, open A41/disabled Production posture, runbook split and cross-links, bounded privacy-safe dashboard intent, canonical pending evidence matrix, and the earlier focused Python/.NET verification targets.

## Design Notes

The repository commit produced by this run prepares and verifies the evidence machinery; it is not the A41-closing commit. Close-out is a second, tightly bound mutation after live evidence and approvals exist. The canonical matrix must distinguish `repository-validated`, `operator-pending`, `passed`, and `rejected`; only `passed` external checkpoints may satisfy C2-C6.

## Verification

**Commands:**
- `PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` -- expected: exact fixture inventories pass with zero failures/errors/skips.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -parallel none -noLogo` -- expected: nonzero exact discovery, zero failed/errors/skipped/not-run.
- `git diff --check` -- expected: no whitespace errors; `sprint-status.yaml` absent from the changed path set.
