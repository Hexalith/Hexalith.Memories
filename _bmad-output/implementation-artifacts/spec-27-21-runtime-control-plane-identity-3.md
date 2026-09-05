---
title: 'Story 27.21 DW-718 halt — C1.15 target remains ineligible'
type: 'feature'
created: '2026-09-05'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '3a7a70259d0ff185947fcc2e4216f7a275651d68'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-2.md'
  - '{project-root}/_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-27-context.md'
  - '{project-root}/docs/dev/adr-27.1-001-access-telemetry-lifecycle.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.21’s next residual is DW-718 (real C1.15 packet + independent review), but the approved PG-ONPREM-1 lifecycle target is still ineligible: Production lifecycle Deployments remain zero-scaled, the explicit alpha pair is absent, and no Ready `memories-access-telemetry` pods exist.

**Approach:** Halt the DW-718 capture path without cluster or manifest mutation. Record fresh ineligibility evidence, keep DW-718 and C1.15 `pending` / `not complete`, leave Story 27.21 `in-progress` / operator handoff authoritative, and do not synthesize a packet or claim gate passage.

**Decisions:**
- NEXT_SLICE: DW-718 (real C1.15 packet + independent review).
- IF_DW718_ELIGIBILITY: Not eligible — halt; keep DW-718 open; no Kubernetes scale/rollout/patch and no Production manifest or alpha-env injection in this pass.

## Boundaries & Constraints

**Always:** Keep the collector read-only and bound to literal `C1.15` / `PG-ONPREM-1` / context `jpiquot@local` / namespace `hexalith-memories` / lifecycle-only selector. Keep packets secret-safe, immutable, `gateStatus: not-evaluated`, and `productionGatePassed: false`. Preserve `spec-27-21-runtime-control-plane-identity.md` as the operator handoff authority. Leave DW-719, DW-645, Story 27.4, A41, and unrelated dirty paths untouched.

**Ask First:** Any later eligibility change (scale Ready lifecycle pods, inject alpha pair, rollout, or manifest edit) before a future DW-718 capture attempt.

**Never:** Mutate the cluster or Production manifests in this pass, run the live capture against an ineligible target as if it were success, fabricate or copy fixture output into `artifacts/access-telemetry-c1/`, treat fixture success as C1.15 acceptance, expose `DAPR_API_TOKEN`, fall back to Server pods, close A41, advance Story 27.4, or rewrite frozen historical registration/hardening rows.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Confirmed ineligible target | Production patch still has two `replicas: 0` lifecycle Deployments; base lacks alpha pair; namespace has no Ready lifecycle pods; no C1.15 artifact directory | Halt DW-718 capture; record dated ineligibility evidence; story stays `in-progress`; DW-718 stays `open`; operator handoff stays `awaiting-operator` | Do not mutate cluster/manifests or invent a packet |
| Accidental capture attempt | Someone runs the live producer while the target is still ineligible | Producer may emit a secret-safe blocker packet and nonzero exit; that is not gate evidence and must not be treated as DW-718 completion | Leave checkpoint `pending` / `not complete`; do not promote blocker output to reviewed evidence |
| Scope creep | Pressure to also land DW-719 or DW-645 in this pass | Refuse; those remain separately open deferred residuals | Only DW-718 halt evidence is in scope |

</frozen-after-approval>

## Code Map

- `deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml` -- read-only proof both lifecycle Deployments are `replicas: 0`; do not edit.
- `deploy/kubernetes/base/access-telemetry-deployments.yaml` -- read-only proof alpha env pair is absent; do not edit.
- `tools/verify-access-telemetry-c1.ps1` -- sole C1.15 collector; reuse unchanged; do not run as a success path against the ineligible target in this pass.
- `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity.md` -- authoritative `awaiting-operator` handoff and `operator_actions`; keep status and actions; append only a dated halt note if needed for this pass.
- `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` -- story `in-progress`, C1.15 `pending` / `not complete`; reconcile with a dated halt evidence note without claiming completion.
- `_bmad-output/implementation-artifacts/deferred-work.md:5334-5339` -- DW-718 remains `open`; append a dated 2026-09-05 reconfirmation that the target is still ineligible and capture was halted by human decision.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/epic-27-context.md` -- leave C1.15 pending / Story 27.21 `in-progress` semantics intact unless a one-line dated halt pointer is required for truthfulness; do not advance Story 27.4 or A41.
- `artifacts/access-telemetry-c1/C1.15` -- must remain absent or contain only non-promoted blocker output; never seed from fixtures.

## Tasks & Acceptance

**Execution:**
- [x] `deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml` and `deploy/kubernetes/base/access-telemetry-deployments.yaml` -- re-verify zero replicas and missing alpha pair with the same read-only checks used by Story 27.21 -- prove the target is still ineligible without editing either file.
- [x] Cluster namespace `hexalith-memories` (context `jpiquot@local`) -- confirm no Ready pods for `app.kubernetes.io/name=memories-access-telemetry` -- record the observation; do not scale or patch.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- append a dated DW-718 note that 2026-09-05 reconfirmed ineligibility and halted capture per human decision -- keep `status: open`.
- [x] `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` and `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity.md` -- append a short dated halt/evidence note that C1.15 remains pending and operator actions remain outstanding -- do not mark complete or change sprint to done.
- [x] Worktree hygiene -- ensure no new synthesized C1.15 packet under `artifacts/` and no producer/manifest code changes landed for this halt pass -- `git status` stays free of capture-success claims.

**Acceptance Criteria:**
- Given the checked-in Production overlays and live namespace listing, when eligibility is rechecked, then both lifecycle Deployments remain zero-scaled, the alpha pair remains absent, and no Ready lifecycle pod is present.
- Given the human halt decision, when this pass finishes, then DW-718 remains `open`, Story 27.21 remains `in-progress`, C1.15 remains `pending` / `not complete`, and no immutable observed Production packet is promoted as reviewed evidence.
- Given the scoped diff, when it is reviewed, then no cluster mutation, Production manifest edit, DW-719/DW-645 work, Story 27.4 advancement, or A41 closure is included.

## Implementation Notes

Executed 2026-09-05 as a governance halt only. Manifest and live-namespace checks matched the Design Notes preconditions; DW-718, Story 27.21, and the operator handoff received dated ineligibility/halt notes without changing open/`in-progress`/`awaiting-operator` authority. Sprint status, Epic 27 context, epics.md, Story 27.4, A41, DW-719, and DW-645 were left untouched. Producer and Production manifests were not edited.

## Spec Change Log

- 2026-09-05: Halted DW-718 capture after reconfirming PG-ONPREM-1 C1.15 target ineligibility (`replicas: 0` ×2, alpha pair absent, no Ready lifecycle pods, no `artifacts/access-telemetry-c1/C1.15` directory). Appended dated open-status notes to DW-718, Story 27.21, and the operator handoff. No cluster mutation, Production manifest edit, packet synthesis, Story 27.4 advancement, or A41 closure.

## Review Triage Log

- `false` — Spec-3 Approach appears to conflate DW-718 ledger status with C1.15 `pending` / `not complete`. Evidence: Decisions and Tasks keep DW-718 `status: open`; C1.15 checkpoint wording is separate; fixing Approach would edit the frozen block / this build's spec, which triage rejects.
- `false` — DW-718 `source_spec` still cites only Spec-2. Evidence: Spec-2 originated DW-718; the 2026-09-05 reconfirmation is appended in `reason` and does not replace the origin source.
- `false` — Spec-3 `context` omits `deferred-work.md`. Evidence: Code Map and Tasks already bind DW-718; omitting context is not a runtime defect; fixing would edit this build's spec.
- `false` — Spec-3 Verification allegedly ends with `OTHER_DIRTY:`. Evidence: that dump lives only in the review staging diff file `/tmp/spec-27-21-3-review-diff.wDKgRE`, not in Spec-3 Verification on disk.
- `medium` — Story 2026-09-05 halt Verification bullet omitted Spec-3’s `check-story-slice-scope.py` and scoped `git diff --check` outcomes. Evidence: bullet originally recorded only eligibility/kubectl/`test ! -e`; patched to include both required outcomes.
- `false` — Missing dated halt pointer in `sprint-status.yaml` / `epic-27-context.md` / `epics.md`. Evidence: Spec-3 allows leaving them intact when already truthful; Story 27.21 remains `in-progress` and C1.15 pending without requiring a new pointer.
- `low` — kubectl listing does not assert Ready columns. Evidence: empty namespace listing already proves no Ready pods; everyday halt evidence does not need a more complex Ready-column parser. Rejected as low with non-trivial fix.
- `false` — Spec-3 `type: 'feature'` mislabels a governance halt. Evidence: cosmetic frontmatter; fix would edit this build's spec.
- `false` — Missing I/O matrix row for authorized resume/capture. Evidence: Intent is halt-only; resume remains Ask First / future DW-718 work outside this pass.
- `low` — DW-718 reconfirmation appended into one long `reason:` line. Evidence: readable enough for ledger scan; restructuring the ledger schema is more than a direct correction. Rejected as low.
- `false` — Code Map allows absent-or-blocker while Verification requires `test ! -e`. Evidence: this pass verified absence; Code Map’s blocker branch is a non-promoted allowance, not a failed hygiene check for the observed empty path.
- `false` — Ready True pod could make kubectl exit zero while misread as ineligibility. Evidence: observed output was “No resources found”; a Ready pod would appear in the listing and contradict the halt evidence narrative rather than silently pass.
- `false` — Partial alpha (one key present) makes the combined evidence command exit nonzero despite continued ineligibility. Evidence: both alpha keys remain absent today; the historical Story 27.21 evidence command was reused unchanged and correctly confirms the current state.
- `false` — Claim that Approach keeps “DW-718 … pending”. Evidence: same as first row; Decisions/Tasks keep ledger `open`; frozen wording fix rejected as editing this build's spec.
- verification-gap: no findings.

## Design Notes

Human choice on 2026-09-05: pursue DW-718, but halt because the target is not eligible and eligibility mutation is not authorized. Fresh checks already showed `replicas: 0` ×2, alpha absent, no lifecycle pods in `hexalith-memories`, and no `artifacts/access-telemetry-c1/C1.15` directory. This pass is an evidence/governance halt, not a producer change.

Deferred capture command (unchanged; do not treat as success in this pass):

```powershell
pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.15 -ProfileId PG-ONPREM-1 -EvidenceDirectory ./artifacts/access-telemetry-c1/C1.15
```

## Verification

**Commands:**
- `test "$(rg -c '^  replicas: 0$' deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml)" -eq 2 && ! rg -q -F -e 'AccessTelemetryLifecycle__ComponentIsAlpha' -e 'AccessTelemetryLifecycle__AllowAlphaComponent' deploy/kubernetes/base/access-telemetry-deployments.yaml` -- expected: exit 0 (still ineligible).
- `kubectl --context jpiquot@local -n hexalith-memories get pods -l app.kubernetes.io/name=memories-access-telemetry --no-headers` -- expected: no Ready lifecycle pods (empty or non-Ready only).
- `test ! -e artifacts/access-telemetry-c1/C1.15` -- expected: no promoted real packet directory from this halt pass.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-21-runtime-and-control-plane-identity` -- expected: story file still OK after any dated halt note.
- `git diff --check` -- expected: no whitespace errors on scoped touched governance paths.
