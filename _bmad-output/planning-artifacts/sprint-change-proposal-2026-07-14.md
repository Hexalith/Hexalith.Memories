# Sprint Change Proposal — CI/CD Recovery: Zot Release Contract + Restart-Durability Gate

- **Date:** 2026-07-14
- **Author:** Administrator (via Correct-Course workflow)
- **Trigger:** Red GitHub Actions runs
  [29308157143 (Nightly)](https://github.com/Hexalith/Hexalith.Memories/actions/runs/29308157143),
  [29276048173 (Release)](https://github.com/Hexalith/Hexalith.Memories/actions/runs/29276048173), and
  [29272615761 (Release)](https://github.com/Hexalith/Hexalith.Memories/actions/runs/29272615761)
- **Change scope:** Moderate — release contract correction plus restart-recovery diagnosis and hardening
- **Mode:** Incremental
- **Status:** Approved and implemented locally on 2026-07-14; GitHub Actions verification pending push

---

## Section 1 — Issue Summary

The supplied runs contain two independent failure families. Both are regressions against existing delivery
commitments, not new product requirements.

| Run | Failing job/step | Observed failure | Assessment |
|-----|------------------|------------------|------------|
| Nightly `29308157143` | `Slow integration tests` | `PipelinePersistenceIntegrationTests.RestartTopology_InFlightUrlIngestion_ShouldRestoreCaseCounterActorState` reached terminal `Failed`; 15 of 16 slow tests passed | Recurrent restart-recovery failure with insufficient diagnostics |
| Release `29276048173` | `Authenticate container registry` | `docker/login-action`: `Username and password required` | Unconditional login uses non-standard, unset secret names |
| Release `29272615761` | `Authenticate container registry` | Same error before build/test/release evaluation | Same release-contract defect |

### Root cause A — release workflow diverged from the shared Zot contract

`.github/workflows/release.yml` logs in on every push to `main` using
`CONTAINER_REGISTRY_USERNAME` and `CONTAINER_REGISTRY_PASSWORD`. No repository secret or variable with those
names is configured, so both runs fail before semantic-release can determine whether the commit produces a
release. The latest inspected run on current `main`,
[29313362597](https://github.com/Hexalith/Hexalith.Memories/actions/runs/29313362597), fails the same way.

The authoritative Hexalith.Builds release contract uses:

- `HEXALITH_ZOT_REGISTRY` (optional; defaults to `registry.hexalith.com`)
- `HEXALITH_ZOT_USERNAME`
- `HEXALITH_ZOT_API_KEY`

It authenticates only inside the actual semantic-release publish path. The Memories workflow instead requires
credentials even for non-release commits. The repository also publishes to non-standard nested names
`hexalith/memories-server` and `hexalith/memories-mcp`; the live registry and shared convention use flat module
names. The expected targets are `memories` and `memories-mcp`.

### Root cause B — recurrent restart test reaches a terminal workflow failure, but the gate hides the cause

The Nightly failure is not just a timeout: the public status payload reports `runtimeStatus: Failed`. The same
test has failed in multiple earlier Nightly runs. A focused Release run on current local `main` passed once in
2m24s, so the defect is timing/restart-bound and not deterministically reproducible on every host.

The current test helper waits the full three-minute timeout when it sees an unexpected terminal state and then
reports only the sanitized API text `Workflow failed.` It omits the Dapr failure detail, captured resource logs,
scripted-server request count, and counter-transition evidence needed to distinguish among:

- a product restart/replay defect;
- a delayed/redelivered activity edge;
- a topology readiness or upstream retry race; or
- a test-harness timing defect.

There is a concrete replay-safety risk to verify during correction: `CaseIngestionCounterState` remembers only
the single `LastTransitionId`, while its contract says deterministic transition IDs deduplicate workflow replay.
An older transition redelivered after a later transition is therefore not recognized as already applied. This is
an evidence-backed suspect, not yet claimed as the sole cause of the observed terminal workflow failure.

## Section 2 — Impact Analysis

- **Triggering commitments:** Story 26.1 (production artifacts/container publishing), Story 26.3 (real
  integration failure-mode coverage), Story 26.4 (Nightly quality gate), and Story 6.4 / PRD NFR17
  (in-flight workflow state survives restart without data loss).
- **Epic viability:** Epic 26 remains viable and in progress. No completed story history should be rewritten or
  reopened.
- **Required backlog change:** Add two corrective stories inside Epic 26:
  - **26.6 — Zot Release Contract Alignment**
  - **26.7 — Restart-Recovery Reliability Gate**
- **Epic ordering:** Make 26.6 and 26.7 the immediate blockers before closing Epic 26 or treating its release and
  resilience evidence as complete. Story 26.5 can remain backlog.
- **PRD impact:** No requirement change. NFR16/NFR17 are reinforced, not weakened.
- **Architecture impact:** No architecture decision change. Dapr Workflow durability and actor-backed counters
  remain the intended design; the implementation and evidence must conform to it.
- **UX impact:** None.
- **Operational artifacts affected:** release workflow, container publisher, project container metadata,
  Kubernetes manifests, deployment render/verification tools, integration diagnostics, CI/tooling tests, and
  release/recovery documentation as applicable.

## Section 3 — Recommended Path

Use **Option 1: Direct Adjustment** within Epic 26.

- **Effort:** Medium.
- **Risk:** Medium. Release changes touch publication credentials and immutable image names; restart work touches
  a hard reliability gate and may require persisted actor-state compatibility.
- **Why this option:** The product requirements and architecture remain valid. Two bounded corrective stories
  preserve completed history while making the missing release and durability evidence explicit.
- **Rollback:** Not recommended. Reverting container publication or restart coverage would remove required
  operational evidence without correcting the underlying contracts.
- **PRD/MVP reduction:** Not applicable. Relaxing zero-loss/restart behavior or excluding the failing test would
  contradict existing hard requirements.

## Section 4 — Detailed Change Proposals

### 4.1 Planning artifact adjustment — Epic 26

**OLD**

> **NFRs reinforced:** NFR7, NFR14, NFR16
>
> Epic 26 ends with Story 26.5.

**NEW**

> **NFRs reinforced:** NFR7, NFR14, NFR16, NFR17

Append:

#### Story 26.6: Zot Release Contract Alignment

As a release maintainer,
I want container publication to use the shared Hexalith Zot credential and repository conventions only when an
actual release is published,
So that ordinary `main` pushes do not fail on unused credentials and real releases publish discoverable images.

**Acceptance criteria:**

- Non-release semantic-release runs complete without attempting registry authentication or container pushes.
- Actual publish runs consume `HEXALITH_ZOT_REGISTRY`, `HEXALITH_ZOT_USERNAME`, and
  `HEXALITH_ZOT_API_KEY`; missing publish credentials fail at the publish boundary with an actionable,
  secret-safe message.
- Server and MCP targets are `registry.hexalith.com/memories:<version>` and
  `registry.hexalith.com/memories-mcp:<version>` by default across project metadata, scripts, rendered manifests,
  and verification tests.
- Build-only/dry-run container validation remains credential-free; immutable-tag digest reconciliation and
  aggregate publication evidence remain intact.
- Workflow inventory and publisher fixtures reject early unconditional login, legacy secret names, and legacy
  nested repository names.

#### Story 26.7: Restart-Recovery Reliability Gate

As a reliability maintainer,
I want restart tests to expose the first terminal failure and prove replay-safe counter/workflow recovery,
So that NFR17 regressions are actionable and cannot be hidden by timeouts or flaky green runs.

**Acceptance criteria:**

- The persistence-test waiter fails immediately on an unexpected terminal workflow state and attaches safe
  diagnostics: status payload, Dapr failure detail when available, relevant captured resource logs, scripted HTTP
  request count, and before/after counter snapshots.
- The recurrent `RestartTopology_InFlightUrlIngestion_ShouldRestoreCaseCounterActorState` failure is reproduced or
  otherwise isolated with the new evidence, and the evidenced product or harness cause is corrected.
- Counter transitions are idempotent for delayed/out-of-order replay, not only an immediately repeated last ID;
  any persisted-state evolution is backward compatible with existing actor state.
- The test still forces a restart while URL ingestion is in flight, reaches `Completed`, preserves the pending
  counter across restart, and drains all counter buckets to zero at completion.
- Verification includes focused repetition plus the full slow integration lane. Raising the timeout, suppressing
  terminal `Failed`, removing the topology restart, or weakening zero-loss assertions is not acceptable evidence.

### 4.2 Release implementation sequence

1. Remove the unconditional `docker/login-action` step.
2. Pass the standard Zot registry and credential variables only to the semantic-release publish path.
3. Make `publish-containers.ps1 -Push` validate and authenticate using the standard names before remote
   reconciliation/push; retain secret redaction. Keep non-`-Push` builds credential-free.
4. Change the repository mapping to flat `memories` / `memories-mcp` names in all authoritative and generated
   deployment surfaces.
5. Update Python fixtures and C# workflow/deployment inventory guards, then run release preflight, container
   build-only fixtures, relevant unit tests, and a Release build.
6. Confirm the standard Zot secrets are actually visible to this repository before intentionally publishing a
   release. Repository-level inspection cannot prove inherited organization-secret access.

### 4.3 Restart-recovery implementation sequence

1. Harden failure collection first, without changing the test pressure or timeout.
2. Re-run the focused restart scenario to capture the terminal cause.
3. Correct the evidenced replay, activity, readiness, or harness defect. If counter replay is involved, replace
   single-last-ID deduplication with a bounded, backward-compatible per-workflow replay ledger/watermark.
4. Add Docker-free counter-state compatibility and non-adjacent replay tests where applicable.
5. Repeat the focused scenario at least three times, run the complete slow lane, and use CI/Nightly as the final
   topology gate.

## Section 5 — Handoff and Success Criteria

- **Developer/Codex:** implement Stories 26.6 and 26.7 after approval; preserve unrelated worktree changes.
- **Quality verification:** run focused release/tooling/unit checks, repeated restart coverage, full slow lane, and
  inspect the resulting GitHub Actions evidence.
- **Repository/organization administrator:** intervene only if the actual publish path proves that
  `HEXALITH_ZOT_USERNAME` / `HEXALITH_ZOT_API_KEY` are not inherited by this repository.
- **Definition of success:** the two supplied Release failure modes no longer block non-release pushes; a real
  release is correctly wired to the shared Zot contract; the supplied Nightly restart scenario is green with
  actionable terminal diagnostics retained; no NFR16/NFR17 assertion is relaxed.

## Section 6 — Checklist Disposition

- **Trigger/context (1.1–1.3):** Done — runs, recurring evidence, errors, and affected stories identified.
- **Epic impact (2.1–2.5):** Done — Epic 26 remains viable; two corrective stories and priority change proposed.
- **Artifact impact (3.1–3.4):** Done — no PRD/architecture/UX conflict; implementation, deployment, test, and
  operations surfaces enumerated.
- **Path evaluation (4.1–4.4):** Done — direct adjustment selected; rollback and scope reduction rejected.
- **Proposal components (5.1–5.5):** Done — scope, artifact deltas, sequence, ownership, and gates defined.
- **Final review (6.1–6.2):** Done.
- **Explicit approval (6.3):** Done — Administrator approved on 2026-07-14.
- **Backlog/sprint-status edits (6.4):** Done — Stories 26.6 and 26.7 are in review pending GitHub evidence.
- **Implementation handoff (6.5):** Done — local implementation and verification complete; CI handoff pending push.

---

## Approval Gate

Administrator approved this proposal on 2026-07-14, authorizing the Epic 26 planning updates and implementation
within the scope and success criteria above.

## Implementation Result — 2026-07-14

- **Release correction:** Removed unconditional registry login; semantic-release now receives the shared
  `HEXALITH_ZOT_*` contract and the publisher authenticates with password-stdin only under `-Push`. All active
  container/deployment surfaces now use `memories` and `memories-mcp`.
- **Nightly root cause:** New fail-fast diagnostics reproduced the failure and exposed Dapr's exact
  `[URL_TIMEOUT]` detail. The global standard HTTP resilience handler was applying a hidden 10-second attempt
  timeout and nested transport retries around a 10-second test upstream, despite `UrlContentFetcher` and the
  durable workflow already owning timeout/retry. The nested handler was removed.
- **Replay hardening:** Case counter state now carries a bounded per-workflow sequence watermark while preserving
  the legacy `LastTransitionId`, covering non-adjacent and interleaved replay after restart.
- **Verification:** focused restart 3/3 green (32–34 seconds each); full slow integration lane 16/16 green;
  publisher fixtures 12/12; package fixtures 28/28; preflight fixtures 26/26; focused server/deployment/counter
  tests 29/29; release workflow guards 14/14; authenticated live semantic-release preflight green for `v2.6.0`;
  solution Release build 0 warnings/0 errors.
- **Remaining external evidence:** push the change and confirm the Release and Nightly workflows. An actual
  `v2.6.0` publication additionally proves that the organization-level `HEXALITH_ZOT_USERNAME` and
  `HEXALITH_ZOT_API_KEY` secrets are inherited by this repository.
