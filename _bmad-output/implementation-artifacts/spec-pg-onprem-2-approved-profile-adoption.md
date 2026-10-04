---
title: 'Adopt the approved PG-ONPREM-2 qualification profile'
type: 'feature'
created: '2026-10-04'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '5b43fe2f8a0f04dc021921a077dff1a573c2ce5e'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad/custom/story-scope-guard.md'
  - '{project-root}/_bmad/custom/epic-ac-verification.md'
---

<frozen-after-approval reason="Administrator approved this exact candidate and coherent consumer adoption on 2026-10-04">

## Intent

**Problem:** Exact PG-ONPREM-2 bytes are approved, but active configuration and qualification consumers still select PostgreSQL 18.4/PG-ONPREM-1.

**Approach:** Adopt canonical SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` and its exact configuration into coherent current consumers. Make C1.16 callable on the successor and register its single owner after focused checks.

## Boundaries & Constraints

**Always:** Preserve the historical PG1 constructor/hash and immutable evidence. Copy four approved YAML files exactly; preserve all twelve bound source inputs, including the 2.6.0 CA-only smoke CLI. Use a qualification overlay for derived hashes. Current downstream qualification requires PG2 and rejects old/mixed evidence. Preserve capacity/workload/fault limits, disabled Production, zero application replicas and released Lease. Captures stay neutral; connection linkage needs actual independent proof.

**Never:** Contact a qualification target, deploy, read credentials, mutate tenant data, invent approvals/acceptance, close security/A41/27.4, rewrite historical operator intent, change unrelated AppHost defaults, publish Git, or alter the approved canonical identity/configuration bytes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Adoption | Exact approved files/profile | Matching current canonical hash and source/configuration identities | Refuse drift |
| Historical | Closed PG1 evidence/constructor | Same old hash; opt-in C1.16 historical capture remains neutral | No successor credit |
| Successor | Literal C1.16/PG2; stable exact PG/Dapr identities | Neutral immutable PG2 packet; actual 18.6/180006/peer identity | Safe blocker |
| Mismatch | Wrong image/version/hash, mixed or old predecessor/approval/C0 | Rejected; no positive partials | Deny before calls where possible |
| Invalid mode | C1.15/PG2, lowercase successor, historical opt-in/PG2 | Zero calls/no directory | Early refusal |

</frozen-after-approval>

## Code Map

Approved bytes: `../planning-artifacts/c1-security-prerequisites-2026-10-04/profile-candidate/`.
Embed profile in the existing verifier; overlay reporter hash.

## Tasks & Acceptance

**Execution:**
- [x] `deploy/openbao/values.yaml`, `deploy/kubernetes/base/access-telemetry-postgresql.yaml`, `deploy/kubernetes/base/access-telemetry-deployments.yaml`, `deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml` -- exact candidate copies.
- [x] `deploy/kubernetes/overlays/qualification/kustomization.yaml`, `deploy/kubernetes/overlays/qualification/qualification-gate.yaml` -- derived PG2 hash; disabled gate/Lease.
- [x] `tools/verify_access_telemetry_lifecycle.py`, `tools/access_telemetry_producer_common.py` -- PG2 constructor/current selectors; old/mixed/pin rejection; preserve historical constructor.
- [x] `tools/verify-access-telemetry-c1.ps1`, `tools/access-telemetry-c1-component-backend.ps1`, `tests/tooling/access_telemetry_c1/gate_c1_16_test.py` -- successor identity/provenance; preserve historical/early-denial behavior.
- [x] `tools/production-deployment-openbao.ps1`, `tools/validate-production-deployment-evidence.ps1`, `.github/workflows/ci.yml` -- coherent server pin; preserve fixed smoke client.
- [x] `tests/tooling/access_telemetry_lifecycle`, `tests/tooling/production_deployment_evidence`, `tests/Hexalith.Memories.Server.Tests/Deployment`, `tests/Hexalith.Memories.Server.Tests/Architecture` -- mismatch/invariance guards.
- [x] `docs/operations/openbao.md`, `docs/operations/access-telemetry-adapter-production.md`, `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` and prerequisite package -- dated adoption.
- [x] `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/implementation-artifacts/epic-27-context.md` -- one checked backlog owner, dated correction, 23 held.

**Acceptance Criteria:**
- Given approved bytes, when fixtures run, then current consumers select exact PG2; PG1 remains historical.

## Dev Notes

### Historical Context Classification

| Influence | Classification | Permitted use |
| --- | --- | --- |
| D1-D4/candidate | historical-reference-only | Identity/transaction. |
| 27.3/27.4/27.5/27.6 | anti-template | History only. |
| C1.16 preparation | current-narrow-pattern | Bounded capture. |

### Slice Proof

One profile adoption; one C1.16 owner.

### Epic AC Verification

Creation preflight: 2026-10-04, stated baseline/worktree.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| --- | --- | --- | --- | --- |
| PostgreSQL 18.4 is hashed into PG-ONPREM-1 | Behavior | `canonical_pg_onprem_profile().manifest()` from `tools/verify_access_telemetry_lifecycle.py` | dc194858...3d14; preserved. | confirmed |
| PG-ONPREM-2 is still rejected | Behavior | `rg -n ValidateSet tools/verify-access-telemetry-c1.ps1` | Creation PG1-only allowlist; implementation gap. | confirmed |
| Production remains statically disabled | Configuration | `rg -n -F ACCESS_TELEMETRY_ENABLED=false deploy/kubernetes/overlays/production/kustomization.yaml` | Exact false literal. | confirmed |

## Implementation Notes

Exact four-file adoption, current selectors and neutral C1.16/PG2 capture are implemented. The historical constructor, candidate identity/acquisition bytes, C1.15 packet, prior receipts, preparation spec, Story 27.21 and original 27.4 handoff are preserved. One canonical Story 27.22 is registered at backlog; the other 23 allocation files remain absent from implementation and sprint registration.

Pre-review parent diff audit: 31 existing files changed within the approved task surfaces and five files added. Full baseline/untracked diff: `/tmp/bmad-pg2-adoption-review-qrv80rvy.diff`; incremental snapshot diff: `/tmp/pg2-incremental-ew50ko87.diff`. Prior unchanged work remains separately attributable. No Git staging/publication or target contact occurred.

Pre-review corrections restored legacy lowercase C1.15, rejected missing/renamed/duplicate runtime identities, corrected current capacity prose to retained multiplier 2 and non-admitted 7d, distinguished historical OpenBao measurements/client from current server, and separated repository inactive defaults from live capture state. Exact approved bytes and frozen intent are unchanged.

Creation-time facts in the Epic AC table are historical preflight observations. Current recheck confirms the exact PG2 constructor/hash, literal successor acceptance and static disabled defaults; earlier PG1-only dispatch is superseded.

- Adopted all four YAML inputs exactly; preserved the twelve bound configuration inputs,
  fixed 2.6.0 smoke client, historical constructor/hash and archived evidence bytes.
- Embedded the exact PG2 manifest in the existing verifier; current selectors, source
  preflights, predecessor/approval/C0/terminal consumers and OpenBao server/CI pins
  coherently require PG2. Derived reporter hash lives only in the qualification overlay.
- C1.16/PG2 captures exact PostgreSQL 18.6/180006/peer and Dapr identities with immutable
  neutral provenance. Historical opt-in and legacy case-insensitive C1.15/PG1 behavior
  are preserved; invalid successor modes deny before calls/directory creation.
- Current runtime inventory requires exactly one named PostgreSQL/Dapr status and
  rejects missing, renamed, duplicate or wrong-image identities before enablement.
- Registered one checked Story 27.22 backlog owner; appended dated planning corrections,
  retained original historical operator intent and left twenty-three gates held.
- Executed receipt details and raw log paths are in the
  [dated adoption record](../planning-artifacts/c1-security-prerequisites-2026-10-04/adoption.md).
  Actual eligible-target capture, independent connection linkage/review, security
  requalification, Production activation, Story 27.4 completion and A41 closure remain
  pending and are not credited. No target contact, tenant mutation or Git publication occurred.


## Spec Change Log

## Review Triage Log

All sixteen findings are individually classified before grouping. Reports came from fresh blind/edge reviewers and the earlier independent read-only profile researcher, reused after the platform refused a fresh third thread.

| ID | Verdict | Evidence and route |
| --- | --- | --- |
| B1 | high | C0 checks PostgreSQL's template and never queries its running container; an old backend clears runtime guards. Publication's dirty-source guard is unrelated and would pass on a normally committed source. Patch actual backend identity. |
| B2 | medium | C0's new Dapr checks query only Memories despite requiring a live lifecycle Deployment; observed lifecycle/clock identity faults are unexamined. Patch explicit profile workload observations. |
| B3 | medium | C1.16's prefix-required regex rejects an exact approved bare digest, while Python accepts it. Patch representation admission without widening digest allowlists. |
| B4 | medium | ContainerID/restartCount are absent from inherited UID/image comparisons; reproduced restart retains observed. Pre-existing at adoption snapshot: defer with explicit coherence limitation. |
| B5 | medium | Inherited remote wget substitution buffers full body before local size limit. Pre-existing at adoption snapshot: defer remote bound with owning reopen evidence. |
| B6 | medium | Inherited phase regex accepts Bogus and filters before validating a non-running sibling's UID. Reproduced malformed sibling is omitted. Pre-existing parser limitation: defer. |
| B7 | low | Index digest alone does not observe node platform; C1.17 is separately held. Patch current docs to make unobserved platform qualification explicit; no new capability credit. |
| B8 | medium | C1.16's real offline kubectl fixture runs before pinned kubectl initialization in CI. Current PG2 adoption relies on that fixture lane, exposing ambient-client reliance. Patch dependency ordering and owning CI guard. |
| B9 | false | These records use bmad-build's explicitly required source_spec/summary/evidence shape. No repository consumer requires IDs or rejects that shape; source plus summary identifies the work, and reopen evidence is present. Preserve prior entries; new maintenance planning names owners. |
| B10 | medium | Security plan requires fresh C1.15 but only names generic allocated owners; current dispatcher deliberately rejects PG2/C1.15 and historical completion is preserved. Patch a concrete separately scoped prerequisite owner/reopen contract; no registration or command enablement. |
| E1 | medium | Same inherited restart blind spot verified at source and by offline reproduction. Pre-existing: defer with B4. |
| E2 | medium | Same inherited full remote response allocation verified at probe source. Pre-existing: defer with B5. |
| E3 | medium | Exact bare approved digest fails the changed successor's shared lexical guard before pin matching. Patch with B3. |
| E4 | high | PostgreSQL runtime is not collected by C0; dirty HEAD blocks publication here but does not establish runtime correctness on valid committed source. Patch with B1. |
| V1 | medium | Pre-verified gap: removing both Python caller preflight invocations leaves all seven adoption tests green; direct helper drift tests do not guard callers. Patch executed drift rejection at both CLI boundaries. |
| V2 | medium | Pre-verified gap: removing Python child admission leaves all seven adoption tests green; only the separate PowerShell path proves child success. Patch Python C0/downstream positive child fixtures. |

Survivor groups: patch B1/E4, B2, B3/E3, B7, B8, B10, V1, V2; defer B4/E1, B5/E2, B6. No intent gap or spec loopback: frozen exact pins, current rejection and neutral capture remain unchanged. All eight patch groups are implemented and verified. The three deferred groups were appended without modifying any existing ledger bytes; each names its owner, consequence and measurable reopen evidence. The parent corrected the new C1.17 handoff cell to retain the approved Platform Operations role; allocation bytes remain unchanged.

## Verification

[Exact commands, raw logs and phase/source attribution](../planning-artifacts/c1-security-prerequisites-2026-10-04/adoption-verification-evidence.json) preserve 23 logs. Passed: C1 58 + named matrix 6; lifecycle broad 75 + final adoption 7/capacity 32; production evidence 123; Server broad 91 + affected 28/final wording 3; CLI 66. Zero failures/errors/skips; builds, normal/optimized candidate checks, renders and scope guard exited 0. Earlier broad checks and final affected checks are explicitly distinguished.

| Matrix row | Executed covering test / receipt |
| --- | --- |
| Adoption | `test_exact_current_manifest_and_historical_manifest_are_independent`, `test_exact_approved_copies_and_all_bound_inputs_reject_each_drift`; final adoption log |
| Historical | `test_complete_capture_is_separate_immutable_and_neutral`, legacy lowercase capture; verbose C1 matrix |
| Successor | `test_successor_exact_index_and_child_capture_is_immutable_neutral_and_source_bound`; verbose C1 matrix |
| Mismatch | successor wrong/mixed image/version/peer/source tests; old/mixed predecessor/approval/C0 and wrong/missing PG/Dapr runtime tests; verbose matrix/final adoption logs |
| Invalid mode | `test_opt_in_and_exact_gate_profile_denials_precede_calls_and_directory_creation`; verbose C1 matrix |

Parent independently verified all archived raw bytes/hashes against the 23 original logs, both profile manifests, four exact copies/all 16 input hashes, preserved original C1.15 packet/specs/ledger, exactly one backlog owner and 23 absent registrations. All five rows have named executed passing coverage. Parsed archived production/qualification renders confirm exact named zero-replica workloads, disabled gate/current hash, released Lease, suspended reporter and neutral reporter data. The required Story 27.22 guard passed with exactly one file after the parent wording correction; `git diff --check` passed.

### Final review-patched source checks

Parent verification passed 79 lifecycle tests, 19 C1.16 tests and 67 CI inventory
tests, with zero failures/errors/skips. This includes actual backend/workload
runtime rejection and recheck, both CLI input-drift callers with zero kubectl calls,
Python authenticated-child success, neutral bare digest capture, historical
opt-in, invalid modes and the pinned CI client ordering guard. All five frozen
matrix rows retain named executed passing coverage.

The [final review receipt](../planning-artifacts/c1-security-prerequisites-2026-10-04/parent-final-review-verification-evidence.json)
separates current-source checks from the unchanged pre-review implementation receipt
and prior preparation receipt. It retains raw logs, source hashes, the original
reviewed diff and audit script. Candidate normal/optimized integrity checks, the
one-story scope guard, byte/registration audit and whitespace check passed.

Final phase snapshot audit: 33 existing files changed within authorized surfaces
and six files added; no original file is missing. All four exact copies, twelve
fixed inputs, both profiles, original C1.15 packet, 46 protected originals, frozen
intent and historical handoffs/receipts are preserved. The ledger is append-only.
Only Story 27.22 remains registered at backlog, with twenty-three gates held. HEAD
is unchanged and no Git publication or target contact occurred. Final diff at the
existing temp path is rewritten after patches; its original reviewed content is
retained in the final receipt.
