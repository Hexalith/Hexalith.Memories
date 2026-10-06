---
title: 'Story 27.22: Prepare C1.16 connection linkage evidence'
type: 'feature'
created: '2026-10-05'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '109e527ba36ebf27e2e81d9f458718ff22f60a72'
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
- [x] `tools/verify-access-telemetry-c1-linkage.ps1` — implement bounded secret-safe, read-only attribution using a unique absent synthetic state key and selected PostgreSQL session identity; retain source/command hashes and fail closed on ambiguity, without changing the neutral C1.16 producer result.
- [x] `tests/tooling/access_telemetry_c1/linkage_test.py` — prove complete attribution and denied, stale, duplicate, malformed, timeout, secret, wrong-profile, and changing-pod paths with no target call on invalid inputs.
- [x] `docs/operations/access-telemetry-adapter-production.md` — document exact linkage command, required scope, archive and reviewer handoff, plus restrictive interpretation of all receipts.
- [x] `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md` — record the offline linkage support, exact checks and pending live prerequisites without changing the checkpoint or claiming review/acceptance.

**Acceptance Criteria:**
- Given a complete synthetic exact-PG2 scenario, when the linkage collector runs, then it emits a unique immutable, secret-safe neutral packet with bounded source and command hashes and an independently attributable read/session correlation.
- Given incomplete, mismatched, stale or ambiguous observations, when collection runs, then it exits nonzero without positive linkage or gate credit; invalid profile/mode fails before target calls or directory creation.
- Given offline success, when the story is updated, then C1.16 live capture, external archive, independent review and Production/security gates remain pending.

## Implementation Notes

- Read-only preflight on 2026-10-05 used the configured `jpiquot@local` context. The active kubeconfig points to `https://192.168.1.30:6443`; its `hexalith-memories` PostgreSQL StatefulSet is running `postgres:18.4-trixie` at image digest `3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a`, so this target is not eligible for exact-PG2 C1.16 capture. No capture or cluster mutation occurred. The example `/approved-evidence` archive path is absent locally.

## Spec Change Log

- 2026-10-06: The full C1 suite exposed a baseline stale count assertion in `runtime_control_plane_identity_test.py`: it required 24 unregistered gates while the unchanged epic context records 23 after C1.16 registration. Corrected the assertion to the current exact 23-gate restriction; all Production and historical acceptance checks remain intact. The same guard exposed missing historical packet/review references in the baseline context summary; restored only their existing tracked Story 27.21 paths and hashes, without changing historical evidence or acceptance.

## Review Triage Log

| ID | Lens / location | Verdict | Route | Evidence and disposition |
| :-- | :-- | :-- | :-- | :-- |
| B1 | Blind / HTTP probe | high | patch | A response-supplied transport marker can produce observed evidence after failed transport. Require unforgeable transport status and reject every response body, including blank lines and truncation. |
| B2 | Blind / credential validation | high | patch | The conditional treats grep execution errors as a clean token. Fail closed on utility or validation execution failure before the HTTP request. |
| B3 | Blind / metadata request | high | patch | The reused helper follows redirects and proxy settings with its custom token header. Restrict the new collector's metadata invocation; preserve the historical producer and helper bytes. |
| B4 | Blind / PostgreSQL environment | high | patch | libpq hostaddr can select TCP despite an explicit socket host. Clear inherited PGHOSTADDR before the new collector's psql observation. |
| B5 | Blind / archive path | medium | patch | A file occupying the approved directory permits all calls before output failure. Reject this demonstrated invalid archive state before contact. |
| B6 | Blind / kubeconfig context binding | high | defer | Context-name trust already underlies the existing producers. The collector does not authenticate an operator authorization reference or freeze kubeconfig mapping. Platform Operations must bind and protect the actual endpoint/trust mapping in independently retained target authority before any live use; no such use is authorized here. |
| B7 | Blind / attribution label | medium | patch | Neutral fields and pending disposition prevent gate acceptance, but the attribution string describes exclusivity more conclusively than the collector establishes. Label the candidate correlation as awaiting independent exclusive-window verification. |
| B8 | Blind / session incarnation | high | patch | A session preceding the selected Dapr incarnation passes current ordering checks. Reject sessions predating the selected sidecar/backend incarnation outside the documented clock tolerance. |
| B9 | Blind / command hash handoff | medium | patch | The raw command hash commits an omitted random key and cannot be independently recomputed. Use a documented normalized template hash for the private challenge substitutions, retaining the command ledger shape. |
| B10 | Blind / documented utilities | low | patch | Metadata invokes wget, which is absent from the prerequisite list. Document the restricted metadata client's required capabilities and failure behavior. |
| E1 | Edge / HTTP probe | high | patch | Independent offline reproduction accepted a spoofed exit marker followed by truncated newline data with nc exit 1. Same framing defect as B1. |
| E2 | Edge / token validation | high | patch | An executable grep returning 127 still led to observed evidence. Same utility-error defect as B2. |
| E3 | Edge / metadata request | high | patch | Two synthetic localhost endpoints demonstrated token forwarding on HTTP 302. Same new-invocation defect as B3. |
| E4 | Edge / helper loading | high | patch | Hash verification and dot-sourcing perform separate reads, so concurrent replacement can execute unchecked bytes. Verify and execute a single bounded immutable helper-byte snapshot. |
| E5 | Edge / incomplete-observation claim | high | patch | The failed-transport marker bypass contradicts failure closure; this is the same reproduced B1/E1 defect, retained as a separate finding row. |
| V1 | Verification / challenge interval | medium | patch | Pre-verified mutation removed interval checks while all 16 tests passed. Add advancing activity before/after the allowed interval, asserting blocked/null/nonzero results. |
| V2 | Verification / authorization expiry | high | patch | Pre-verified mutation removed the scope deadline while all 16 tests passed, allowing a delayed command to run past authority. Add deadline termination evidence during a running observation. |
| V3 | Verification / emitted SQL contract | medium | defer | Synthetic JSON does not execute the emitted SQL; an alias mutation evades the fixture. The expressly synthetic preparation does not supply a PostgreSQL infrastructure lane. Deployment Adapter Developer must execute the unchanged query contract in an isolated PostgreSQL 18.6 environment before live collection. |

The sixteen `patch` findings were implemented without changing the frozen intent, existing producer/helper or approved deployment inputs. The edge-review follow-up reproduced the original bypasses against the fixes and confirmed E1–E5 addressed. B6 and V3 remain separately owned prerequisites in the deferred-work ledger and runbook; they prohibit live collection until satisfied. Final fixture verification is recorded below.

## Design Notes

The separate collector should bracket one authenticated, read-only Dapr state GET for a generated absent synthetic key with bounded PostgreSQL session observations. Project only selected `pg_stat_activity` identity/timestamp fields and `pg_stat_ssl` status, never SQL/query text or the key. Require an exact selected pod IP, approved role/database, TLS session and a unique session whose activity advances within the challenge interval; any concurrent or unprovable attribution produces a blocker. A neutral collector receipt is candidate evidence for later independent review, not a C1.16 disposition.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'linkage_test.py' -v` — new positive and denial fixtures pass without skips.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` — all C1 fixtures pass, with no skips.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-22-component-and-backend-identity` — one checked story.
- `git diff --check` — no whitespace errors.

## Implementation Verification

The root reviewed the complete tracked/untracked diff and actual review patches against the approved scope. All four offline preparation tasks are complete. The complete attribution, ambiguous/missing session, and drift/sensitive-output matrix rows ran and passed in the final 26-method linkage suite (274.195 seconds, zero failures/errors/skips); raw log `/tmp/story-27-22-final-linkage-tests.log`. Added regressions execute the real request shell for spoofed transport status, every body byte, missing/failing validators, redirects/proxies, and inherited PostgreSQL environment; helper snapshot, command reconstruction, container/session birth, challenge interval and running-command authorization deadline checks also pass. Live C1.16 capture, external archive and independent disposition remain pending.

The initial review-patched focused run exercised 26 methods in 252.126 seconds and found two missing-grep fixture subcase failures: removing the outer PATH stopped the dotnet-based PowerShell launcher before collector execution. Restricting PATH only inside the request shell corrected the fixture; the four missing/failing-validator phases then passed in 11.852 seconds, and the final complete linkage run passed. The root interrupted its first broad run before linkage because discovery had imported that faulty fixture, then restarted on corrected bytes. Neither run is represented as a successful final broad receipt. Logs: `/tmp/story-27-22-linkage-review-fixes.log`, `/tmp/story-27-22-linkage-validator-final.log`, `/tmp/story-27-22-final-all-c1-tests.log`.

Final verified SHA-256 `fa80a342753a547c12bd824a80154ad348110777d71987022b2f52bd7624fa45` — `tools/verify-access-telemetry-c1-linkage.ps1`.

Final verified SHA-256 `4dd921d0e1a095c8703c52f55e89b763c8687f65485e5c1358c9b36ea09ef987` — `tests/tooling/access_telemetry_c1/linkage_test.py`.

Final root-owned full C1 verification passed all 85 methods in 1075.938 seconds, zero failures/errors/skips, using the exact `*_test.py` command above; raw log `/tmp/story-27-22-final-all-c1-tests-v2.log`. This includes historical and PG2 C1.16 capture, all 26 linkage methods, and the corrected historical-acceptance/Production restriction guard. The one-story slice guard, PowerShell parser and `git diff --check` also passed. Frozen intent, original producers and approved deployment inputs remain unchanged; final source hashes include the EOF formatting correction recorded below.

## Offline Spec Completion

Completion applies to the approved offline preparation only. Story 27.22 and its sprint row stay `in-progress`: the live C1.16 checkpoint and operator tasks remain pending. The generic workflow review-status synchronization does not override that human-owned checkpoint or the repository phase-ledger evidence rule. No independent live review, other-gate or Production credit is claimed.

## File Scope

Allowed to modify:

- `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/epic-27-context.md`
- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/operations/access-telemetry-adapter-production.md`
- `tests/tooling/access_telemetry_c1/linkage_test.py`
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py`
- `tools/verify-access-telemetry-c1-linkage.ps1`

Read/verify only:

- `tools/verify-access-telemetry-c1.ps1`
- `tools/access-telemetry-c1-component-backend.ps1`
- `deploy/**`

## File List

- `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/epic-27-context.md`
- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/operations/access-telemetry-adapter-production.md`
- `tests/tooling/access_telemetry_c1/linkage_test.py`
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py`
- `tools/verify-access-telemetry-c1-linkage.ps1`

## Cross-Tenant Negative Evidence

**Surfaces:** Approved operator context/namespace/workload scope, selected pod IP, runtime role/database, session attribution and secret-safe evidence output.

**Tests:** `test_scope_and_source_mismatches_precede_calls_and_directory_creation`, `test_wrong_ip_role_database_tls_active_and_timestamp_fields_block`, `test_wrong_images_and_component_scope_block_before_challenge`, `test_secret_shaped_stdout_stderr_and_decoded_fields_are_not_retained`.

**Command:** `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'linkage_test.py' -v`.

**Result:** 26 test methods passed in 274.195 seconds, zero failures/errors/skips; `/tmp/story-27-22-final-linkage-tests.log`. Denials have nonzero exits and no positive observations; invalid scope precedes calls/output. This offline evidence grants no tenant-isolation gate or live acceptance credit.

## Change Log

The phase ledger is adopted at final handoff, with the current runner-derived discovery as its baseline. This adoption is late; it does not pretend the baseline was captured before implementation. Earlier count deltas are not reconstructed or invented. The baseline includes the 26 linkage methods already implemented and reviewed; the original 16-method and final 26-method execution receipts above retain the actual earlier observations. Future phase deltas start at this adoption point.

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-06 | create-story | Administrator build workflow adopts the current offline spec into the phase ledger; no earlier delta is inferred. | Current adoption baseline 85 C1 test methods; phase delta +0, cumulative delta +0. Discovery: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases())"` returned 85. | matched 9/9 against `109e527ba36ebf27e2e81d9f458718ff22f60a72`; complete tracked/untracked diff `/tmp/story-27-22-final-review.diff`; no exclusions. |
| 2026-10-06 | dev-story | Completed collector, denial fixtures, runbook and pending live handoff before adoption; all code and tests are included in the adopted baseline. | Post-adoption phase delta +0, cumulative delta +0; 85 -> 85 C1 test methods. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` passed 85 methods in 1075.938 seconds, zero skips. | matched 9/9; same Git baseline and complete tracked/untracked diff; no exclusions. |
| 2026-10-06 | code-review | Three independent review lenses completed; sixteen patch findings addressed, two live-use prerequisites deferred; edge reproductions verified the fixes. All behavioral review patches predate ledger adoption; the final EOF-only correction adds +0 methods and passed the source-hash subset. | Post-adoption phase delta +0, cumulative delta +0; 85 -> 85 C1 test methods. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` passed 85 methods; final discovery and raw receipts are recorded above; linkage lane 26 passed, zero skips. | matched 9/9; root inspected the full diff and actual patches; frozen intent, original producers and approved deployment inputs preserved; no exclusions. |

Final ownership validation: `python3 tools/check-story-review-readiness.py --story-key spec-27-22-component-and-backend-identity --changed-files-file /tmp/story-27-22-staged-files.txt --derive-cumulative` ran at `review` and returned `C1: all 9 changed paths are declared.` and `Story review readiness validation passed.` The file-scope guard passed all nine paths. Commitlint passed the draft. The automatic tenant-evidence path matcher is a no-op for the new collector filename; its explicit attached evidence schema was validated directly and the named denial tests passed. No bypass or scope override was used.

The staged whitespace check exposed one trailing blank line in the formerly untracked collector. Removed only that final CRLF after the full suite passed; no executable content changed. Final source-hash binding is rechecked with the complete-attribution, private-command reconstruction and source/scope denial methods below.

Final source-byte check: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 PYTHONPATH=tests/tooling/access_telemetry_c1 python3 -m unittest linkage_test.ConnectionLinkageTests.test_complete_attribution_emits_unique_immutable_neutral_packets linkage_test.ConnectionLinkageTests.test_private_challenge_command_hash_is_reconstructable linkage_test.ConnectionLinkageTests.test_scope_and_source_mismatches_precede_calls_and_directory_creation -v` passed all 3 selected methods in 26.343 seconds, zero failures/errors/skips; `/tmp/story-27-22-final-source-hash-tests.log`. These are a subset of the unchanged 85-method discovery, not an additional method count. Final staged whitespace check passes.
