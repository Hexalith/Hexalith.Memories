---
title: 'Story 27.22: Prepare C1.16 connection linkage evidence'
type: 'feature'
created: '2026-10-05'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md'
  - '{project-root}/docs/operations/access-telemetry-adapter-production.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.22 has a callable PG-ONPREM-2 C1.16 identity producer and passing offline fixtures, but no method to collect independent Dapr-to-backend connection evidence. The configured target runs PostgreSQL 18.4, so exact-PG2 live capture and review remain pending.

**Approach:** Prepare a separate bounded, read-only C1.16 linkage collector and offline denial fixtures, then document its later operator use. The user chose offline preparation on 2026-10-05 after the configured target failed the exact-PG2 preflight. Live execution and independent disposition remain separate future work.

## Boundaries & Constraints

**Always:** Require any future live observation to bind to exact PG-ONPREM-2 SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`, target identity, source and command hashes, stable selected pods, exact approved images, and PostgreSQL 18.6 / 180006. Keep producer packet `gateStatus`, `componentBehavior`, `productionLifecycleWrites`, and `connectionLinkage` as `not-evaluated`, and `productionGatePassed: false`. Keep live target contact and acceptance pending until an eligible target, external archive, and independent reviewer are identified.

**Never:** Read or export connection strings, tokens, Secret values, query text, or tenant records; mutate tenant data; alter the approved profile or historical PG1 C1.15/C1.16 evidence; scale or enable lifecycle workloads; infer C1.17, another C1 gate, Production, Story 27.4, or A41 credit.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Complete offline fixture | Synthetic exact-PG2 pod, Component and PostgreSQL session responses | Separate neutral linkage packet binds the selected Dapr state read to a uniquely attributable backend session without credential disclosure | Does not grant live or gate credit |
| Missing or ambiguous linkage | Session cannot be uniquely tied to selected sidecar/backend, or pool reuse obscures attribution | No positive linkage observation; checkpoint stays pending | Bounded, secret-safe blocker and nonzero collector exit |
| Drift or sensitive output | Wrong identity/version/image/profile, replaced pod, secret-shaped diagnostics, or invalid mode | No positive partial observation or acceptance | Fail closed before archive promotion; preserve prior immutable files |

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1.ps1` — literal C1.16/PG2 dispatch, bounded kubectl transport, neutral immutable packet; reuse unchanged unless a verified defect requires a narrow fix.
- `tools/access-telemetry-c1-component-backend.ps1` — selected Component, loaded Dapr metadata, image and read-only PostgreSQL identity capture; its `connectionLinkage: not-evaluated` is deliberate.
- `tests/tooling/access_telemetry_c1/gate_c1_16_test.py` — 19 focused PG2 and historical fixtures; extend only for affected capture behavior.
- `deploy/kubernetes/base/dapr/access-telemetry-store.yaml`, `deploy/kubernetes/base/access-telemetry-postgresql.yaml`, `deploy/kubernetes/base/access-telemetry-deployments.yaml` — exact Component reference, backend Service/StatefulSet and sidecar selectors; do not alter deployment state.
- `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml` — registered checkpoint and status; reconcile only after evidence and independent disposition.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/verify-access-telemetry-c1-linkage.ps1` — implement bounded secret-safe, read-only attribution using a unique absent synthetic state key and selected PostgreSQL session identity; retain source/command hashes and fail closed on ambiguity, without changing the neutral C1.16 producer result.
- [ ] `tests/tooling/access_telemetry_c1/linkage_test.py` — prove complete attribution and denied, stale, duplicate, malformed, timeout, secret, wrong-profile, and changing-pod paths with no target call on invalid inputs.
- [ ] `docs/operations/access-telemetry-adapter-production.md` — document exact linkage command, required scope, archive and reviewer handoff, plus restrictive interpretation of all receipts.
- [ ] `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md` — record the offline linkage support, exact checks and pending live prerequisites without changing the checkpoint or claiming review/acceptance.

**Acceptance Criteria:**
- Given a complete synthetic exact-PG2 scenario, when the linkage collector runs, then it emits a unique immutable, secret-safe neutral packet with bounded source and command hashes and an independently attributable read/session correlation.
- Given incomplete, mismatched, stale or ambiguous observations, when collection runs, then it exits nonzero without positive linkage or gate credit; invalid profile/mode fails before target calls or directory creation.
- Given offline success, when the story is updated, then C1.16 live capture, external archive, independent review and Production/security gates remain pending.

## Implementation Notes

- Read-only preflight on 2026-10-05 used the configured `jpiquot@local` context. The active kubeconfig points to `https://192.168.1.30:6443`; its `hexalith-memories` PostgreSQL StatefulSet is running `postgres:18.4-trixie` at image digest `3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a`, so this target is not eligible for exact-PG2 C1.16 capture. No capture or cluster mutation occurred. The example `/approved-evidence` archive path is absent locally.

## Spec Change Log

## Review Triage Log

## Design Notes

The separate collector should bracket one authenticated, read-only Dapr state GET for a generated absent synthetic key with bounded PostgreSQL session observations. Project only selected `pg_stat_activity` identity/timestamp fields and `pg_stat_ssl` status, never SQL/query text or the key. Require an exact selected pod IP, approved role/database, TLS session and a unique session whose activity advances within the challenge interval; any concurrent or unprovable attribution produces a blocker. A neutral collector receipt is candidate evidence for later independent review, not a C1.16 disposition.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'linkage_test.py' -v` — new positive and denial fixtures pass without skips.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` — all C1 fixtures pass, with no skips.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-22-component-and-backend-identity` — one checked story.
- `git diff --check` — no whitespace errors.
