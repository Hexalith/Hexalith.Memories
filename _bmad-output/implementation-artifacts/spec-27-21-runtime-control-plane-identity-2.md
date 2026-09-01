---
title: 'Story 27.21 C1.15 producer identity hardening'
type: 'bugfix'
created: '2026-09-01'
status: 'done'
baseline_commit: 'dd7dd881ecbeecedb7b052b2931920732cedd811'
review_loop_iteration: 2
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity.md'
  - '{project-root}/docs/dev/adr-27.1-001-access-telemetry-lifecycle.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The C1.15 collector accepts two selected pods with the same Kubernetes UID, so a malformed or unstable target can appear to provide independent, consistent runtime identities. Its recheck contract and current handoff records also understate the exercised fail-closed coverage.

**Approach:** Reject duplicate pod UIDs, lock every post-capture identity/readiness drift branch with focused fixture evidence, and reconcile the current Story 27.21 handoff without claiming that the unavailable Production target has been observed or approved.

## Boundaries & Constraints

**Always:** Keep the collector read-only and bound to literal gate `C1.15`, profile `PG-ONPREM-1`, context `jpiquot@local`, namespace `hexalith-memories`, and lifecycle-only selector. Keep packets secret-safe, immutable, `gateStatus: not-evaluated`, and `productionGatePassed: false`. Preserve historical phase records and unrelated dirty worktree changes.

**Ask First:** Any Kubernetes scaling, patching, rollout, manifest change, Production write enablement, new C1 field/gate, or change to the frozen prior Story 27.21 contract.

**Never:** Fall back to Server pods, expose `DAPR_API_TOKEN`, synthesize a real C1.15 observation, treat fixture success or a blocker packet as gate acceptance, close A41, advance Story 27.4, or imply tenant-isolation/profile/approval proof outside C1.15.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Stable multi-pod capture | Ready lifecycle pods have distinct names and UIDs and consistent validated identities | Observed packet contains the allowlisted projection and remains unevaluated | Exit zero proves capture completeness only |
| Duplicate pod identity | Two selected pods share a case-sensitive UID | No observed packet is accepted | Write a secret-safe blocker packet and exit nonzero |
| Post-capture drift | Membership, label, deletion, readiness, lifecycle/daprd readiness, UID, or image changes on recheck | Capture fails closed for every drift family | Write a blocker packet naming the safe failure and exit nonzero |
| Approved target unavailable | Production lifecycle Deployments remain scaled to zero or lack explicit alpha values | Repository hardening completes while C1.15 stays pending | Do not mutate the cluster or claim real evidence |

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1.ps1:350-688` -- lifecycle selection and recheck; require string/nonblank ordinal name and UID identity for every eligible pod before any `kubectl exec`, then reuse the validated identity with ordinal readiness/container checks.
- `tools/verify-access-telemetry-c1.ps1:1-349,689-719` -- preserve bounded secret-scanned execution, create-new/read-only packets, hashing, and the always-unevaluated blocker envelope.
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py:41-211,213-907` -- fake target and whole-run fixtures; cover no-exec malformed/duplicate rejection, exact emitted identities, ordinal and type validation, every recheck shape/identity branch, secret exclusion, and structure-aware unavailable-target governance.
- `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md:7-114` -- reconcile current status/evidence while keeping initial backlog registration and the historical 6/12-test ledger rows intact.
- `_bmad-output/implementation-artifacts/deferred-work.md:115-120,5025-5030` -- preserve the migrated DW-17 wording, append its dated ownership correction, and retain the scoped open real-packet/reviewer residual.
- `_bmad-output/planning-artifacts/epics.md:4944-5068` and `_bmad-output/implementation-artifacts/sprint-status.yaml:164-472` -- preserve the 2026-08-03 backlog provenance separately from the dated current `in-progress` state without changing any gate disposition.
- `_bmad-output/implementation-artifacts/epic-27-context.md:9-43` -- retain baseline story order, rejected-access emission, three capacity horizons/checked arithmetic, and named predecessor gaps; change only the current 27.21 owner status.
- `deploy/kubernetes/base/access-telemetry-deployments.yaml` and `deploy/kubernetes/overlays/production/` -- read-only topology inputs; bind pending-state evidence to named lifecycle Deployments and the referenced Production patch.

## Tasks & Acceptance

**Execution:**
- [x] `tools/verify-access-telemetry-c1.ps1` -- validate all selected pod names and UIDs for nonblank ordinal uniqueness before the first metadata/alpha probe.
- [x] `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py` -- use exact distinct identities for valid capture; prove duplicate UID causes zero exec probes; cover case-differing UIDs and every recheck count, replacement, name, label, deletion, Ready/container-shape, Boolean, UID, and image branch with secret-safe blockers.
- [x] `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py` -- bind unavailable-target evidence to the two named Deployments and referenced Production patch, accept either blocking condition, scan Production inputs for alpha injection, and isolate the DW-718 section before checking `status: open`.
- [x] `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` and `_bmad-output/implementation-artifacts/deferred-work.md` -- reconcile current evidence/ownership and append records without rewriting history or the ledger migration.
- [x] `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, and `_bmad-output/implementation-artifacts/epic-27-context.md` -- align initial-registration versus current-status wording while preserving execution order, unrelated safeguards, other gates, Production disablement, Story 27.4, and A41.

**Acceptance Criteria:**
- Given valid distinct-pod fixtures and each malformed or drift variant, when the focused C1.15 lane runs, then valid capture passes and every duplicate/drift case produces a safe nonzero blocker result.
- Given the live approved target has no Ready lifecycle pod or explicit alpha pair, when repository work completes, then story and sprint remain `in-progress`, C1.15 remains pending/not complete, and the exact operator command plus independent-review residual stay open.
- Given the final scoped diff, when governance and slice checks run, then Production writes, Story 27.4, A41, other C1 gates, frozen historical rows, and unrelated dirty changes are unchanged.

## Spec Change Log

- 2026-09-01: Implemented ordinal pod-UID uniqueness, distinct valid multi-pod fixtures, duplicate-UID rejection, complete post-capture drift coverage, and truthful in-progress handoff/ledger/context reconciliation. C1.15 remains pending and unevaluated.
- 2026-09-01 review loop 1: Review found that sequential duplicate detection could probe one pod before failing, several recheck shape branches were not explicit, the pending-state guard was text-fragile, the compiled context lost unrelated safeguards, and the untracked spec escaped `git diff --check`. The execution map now requires a full no-exec identity pre-pass, exhaustive branch-shaped fixtures, named Production topology and isolated-ledger checks, current-authority reconciliation, baseline context preservation, and a separate untracked-file whitespace check. This avoids premature probes, false pending evidence, stale authority, and unrelated context regression. KEEP ordinal identity comparison, bounded secret-safe packets, distinct valid multi-pod capture, pending/not-complete gate semantics, and the user-owned ledger migration.
- 2026-09-01 review loop 2: Review found that PowerShell string coercion admitted malformed numeric pod identities, default comparisons admitted case drift in Ready/container keys, governance assertions could be satisfied by unrelated document text, and current status had been folded into dated backlog provenance. The collector now requires raw string/nonblank identity values and ordinal condition/container names; fixtures cover initial malformed identities and case drift; governance parses authoritative rows/sections and pins the Production/A41/Story 27.4 boundaries; dated historical text is restored with separate current-hardening statements; path accounting and Code Map ranges are current. KEEP the frozen intent, literal C1.15/PG-ONPREM-1 target, read-only/no-cluster behavior, ordinal identity semantics, bounded secret-safe immutable packets, complete existing branch coverage, distinct valid multi-pod capture, `pending` / `not complete` gate state, disabled Production writes, Story 27.4 backlog, open A41/DW-17 and DW-718, historical 6/12-test rows, the user-owned ledger migration, and all unrelated safeguards.

## Design Notes

Kubernetes UID is the stable identity across pod-name reuse. Build validated ordinal name and UID sets for the complete selected collection before invoking metadata; a duplicate must produce zero exec probes. Membership means the eligible Running target set selected by the lifecycle-only selector, not every returned non-Running item. The post-capture pass independently revalidates collection shape, identities, readiness, and image.

The original `spec-27-21-runtime-control-plane-identity.md` with `status: awaiting-operator` remains the authoritative operator handoff for real capture and independent disposition. This follow-up spec governs repository hardening only and cannot synthesize, replace, or approve that operator evidence.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` -- expected: every C1.15 producer fixture passes, including duplicate-UID and recheck-drift blockers.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-21-runtime-and-control-plane-identity` -- expected: exactly one governed story passes scope validation.
- `git diff --check -- tools/verify-access-telemetry-c1.ps1 tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py _bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/planning-artifacts/epics.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/epic-27-context.md` -- expected: no tracked scoped whitespace errors.
- `git diff --no-index --check -- /dev/null _bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-2.md` -- expected: no output and exit 1 because the whitespace-clean untracked file differs from `/dev/null`.

## Suggested Review Order

**Fail-closed identity validation**

- Preflight the complete pod set before any metadata probe can execute.
  [`verify-access-telemetry-c1.ps1:385`](../../tools/verify-access-telemetry-c1.ps1#L385)

- Revalidate ordinal identity, readiness, and image invariants after capture.
  [`verify-access-telemetry-c1.ps1:609`](../../tools/verify-access-telemetry-c1.ps1#L609)

**Regression contract**

- Prove duplicate UIDs block before exec while case-distinct UIDs remain valid.
  [`runtime_control_plane_identity_test.py:420`](../../tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py#L420)

- Reject non-string, blank, and duplicate initial identities without probing containers.
  [`runtime_control_plane_identity_test.py:505`](../../tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py#L505)

- Exercise every post-capture collection, readiness, identity, and image drift branch.
  [`runtime_control_plane_identity_test.py:526`](../../tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py#L526)

- Bind unavailable-target governance to authoritative topology and status sections.
  [`runtime_control_plane_identity_test.py:764`](../../tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py#L764)

**Operator handoff and governance**

- Record passing hardening evidence without claiming Production gate acceptance.
  [`27-21-runtime-and-control-plane-identity.md:81`](27-21-runtime-and-control-plane-identity.md#L81)

- Keep real packet capture and independent review as an explicit open residual.
  [`deferred-work.md:5025`](deferred-work.md#L5025)

- Separate historical backlog registration from the current in-progress hardening state.
  [`epics.md:4944`](../planning-artifacts/epics.md#L4944)

- Preserve the sprint boundary: C1.15 pending and Story 27.4 backlog.
  [`sprint-status.yaml:460`](sprint-status.yaml#L460)

- Summarize cross-story ownership without moving any Production-write boundary.
  [`epic-27-context.md:41`](epic-27-context.md#L41)
