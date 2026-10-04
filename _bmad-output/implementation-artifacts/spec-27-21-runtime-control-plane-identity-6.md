---
title: 'Story 27.21 real C1.15 capture and independent review'
type: 'chore'
created: '2026-10-03'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-21-runtime-and-control-plane-identity'
baseline_commit: '905e6628bbb97787528ecbc635ebaf3bcf512366'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-718 lacks real C1.15 evidence and independent review. Producer hardening is complete; capture remains awaiting-operator.

**Approach:** Once the approved target is eligible, reuse the read-only producer and obtain independent packet review. Record actual evidence; capture completeness alone supplies no acceptance.

## Boundaries & Constraints

**Always:** Use C1.15 / PG-ONPREM-1 on jpiquot@local / hexalith-memories, lifecycle-only selection, Ready stable containers, authenticated metadata, explicit alpha values, and disabled writes. Preserve the original handoff. Packets retain gateStatus: not-evaluated and productionGatePassed: false.

**Never:** Synthesize evidence, sample Server pods, expose credentials, overwrite packets, or bypass prerequisites. Runtime/manifest changes, other gates, Production activation, Story 27.4/A41 changes, dependencies, and Git mutations are outside scope.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible target | Every story-required identity available | Immutable observed packet, then independent review | Capture alone supplies no acceptance |
| Ineligible target | No Ready pod or required runtime option | Preserve handoff; halt before capture | Record observed prerequisite blocker |
| Failed observation | Incomplete, secret-shaped, malformed, or drifting probe | Existing safe blocker and nonzero exit | No gate credit or fallback |
| Review incomplete | No explicit accepted disposition | DW-718 stays open | Never infer a pass |

## Decision

2026-10-04: The user chose to keep DW-718 awaiting an eligible operator-provided target. Resume read-only capture and independent review after readiness is supplied; no separate target-readiness proposal or deployment is authorized.

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1.ps1` — reuse unchanged: sole producer, validation, identity rechecks, immutable packets.
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py` — 27-method fixture; unavailable-target governance test.
- `deploy/kubernetes/base/access-telemetry-deployments.yaml` — read-only prerequisite input; zero replicas, alpha pair absent.
- `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity.md` — authoritative awaiting-operator handoff.
- `_bmad-output/implementation-artifacts/deferred-work.md` — DW-718 open; DW-719/parser follow-ups resolved; DW-645 separate.

## Tasks & Acceptance

**Execution:**
- [ ] `artifacts/access-telemetry-c1/C1.15` — recheck readiness; capture only when eligible, otherwise halt.
- [ ] `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` — append independent reviewer disposition and exact artifact/command evidence.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md` — close only DW-718 after accepted real evidence.

**Acceptance Criteria:**
- Given an eligible target, when capture succeeds, then its packet contains every story-required identity/hash and keeps the gate unevaluated.
- Given an ineligible target, when readiness is checked, then no capture/pass is claimed and the original handoff remains authoritative.
- Given review without complete acceptance evidence, when records are reconciled, then DW-718 stays open and Story 27.21 stays in-progress.
- Given accepted evidence, when close-out is proposed, then the packet and disposition are reviewable without granting other gates/write authority.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

2026-10-04 resumed DW-718 investigation: read-only checks again found no selected lifecycle pods and desired replicas 0 for both live Deployments. Repository Production inputs still omit the explicit alpha pair; the C1.15 evidence directory is absent. The existing capture-only scope cannot satisfy target readiness, and no remaining producer defect was identified. The user resolved prerequisite ownership by retaining the operator handoff. No live capture or independent packet review is possible yet.

2026-10-03: no documented open producer defect. Live pods are absent; both Deployments have desired replicas 0. Evidence directory and explicit base alpha settings are absent. Prerequisite ownership was resolved on 2026-10-04 by retaining the operator handoff; no irreversible action is authorized. Conditional footprint: packet and two evidence records. Only this draft has been written.

## Verification

- `kubectl --context jpiquot@local -n hexalith-memories get pods -l app.kubernetes.io/name=memories-access-telemetry --request-timeout=15s -o json` — exit 0, items: [].
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -k unavailable_production_target -v` — passed 1/1 in 0.012 seconds.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-21-runtime-and-control-plane-identity` — passed, one story checked.
- `git diff --check` — passed on the unchanged tracked tree.

Conditional capture command:

`pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.15 -ProfileId PG-ONPREM-1 -EvidenceDirectory ./artifacts/access-telemetry-c1/C1.15`

Live capture was not run; no packet or gate disposition was produced.

2026-10-04 readiness and governance recheck:

- `kubectl --context jpiquot@local -n hexalith-memories get pods -l app.kubernetes.io/name=memories-access-telemetry --request-timeout=15s -o 'jsonpath={range .items[*]}{.metadata.name}{"\t"}{.metadata.uid}{"\t"}{.status.phase}{"\t"}{range .status.conditions[?(@.type=="Ready")]}{.status}{end}{"\n"}{end}'` — exit 0, empty output: no selected lifecycle pods.
- `kubectl --context jpiquot@local -n hexalith-memories get deployment memories-access-telemetry memories-access-telemetry-clock --request-timeout=15s -o 'jsonpath={range .items[*]}{.metadata.name}{"\t"}{.spec.replicas}{"\t"}{.status.readyReplicas}{"\n"}{end}'` — exit 0; both desired replica counts are 0, with no ready replicas reported.
- Repository input inspection — both Production patch entries are `replicas: 0`; neither explicit alpha environment name appears in the base Deployments; `artifacts/access-telemetry-c1/C1.15` does not exist.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -k unavailable_production_target -v` — passed 1/1 in 0.013 seconds; this fixture supplies no running-target gate credit.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-21-runtime-and-control-plane-identity` — exit 0, exactly one story checked.
- `git diff --check` — exit 0. The untracked-draft `git diff --no-index --check -- /dev/null _bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-6.md` returned 1 with no diagnostics, the expected new-file difference result.

DW-718 remains open; Story 27.21 remains in-progress and C1.15 remains pending / not complete. The original awaiting-operator handoff is authoritative. Only this existing draft was updated; its prior evidence is preserved and its frozen intent contains only the explicitly chosen handoff decision.

2026-10-04 handoff decision: keep this draft pending an eligible operator-provided target. Planning approval and implementation have not occurred. The user requested a next-step resume prompt; no capture or reviewer disposition was produced.
