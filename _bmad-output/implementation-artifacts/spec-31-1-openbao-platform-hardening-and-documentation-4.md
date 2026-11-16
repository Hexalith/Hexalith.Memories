---
title: '31.1: Record independent evaluation and close security checkpoints'
type: 'chore'
created: '2026-11-16'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '0d79ebfbd8c16b49ef551ce3194198e5d8e990c1'
story_key: '31-1-openbao-platform-hardening-and-documentation'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 31.1's implementation and today's platform-documentation refresh are complete. C4b/C5b/C7 remain pending: independent countersignatures for the seal and ingress limitations and a platform-wide evaluation. The C7 waiver expired on 2026-10-26; no independent signed evaluation is recorded.

**Approach:** Once authentic independent evidence is supplied, incorporate its conclusions into the existing records, validate their consistency and close satisfied gates. This draft does not claim an evaluation occurred.

## Boundaries & Constraints

**Always:** Verify evaluator identity, date, independence, evaluated profile and both limitations. Preserve historical observations, waiver history, secret-safe evidence, exactly two accepted limitations and repository line endings. Until all mandatory gates are discharged, keep the parent story in-progress and unsigned gates pending without completion dates. Administrator remains accountable for scheduling.

**Never:** Invent approval, substitute an owner persona or coding agent for an independent authority, renew the waiver, deploy or read Secret contents, change runtime migration or Production qualification, discharge separate Helm reproduction work, or change Story 31.2's activation gate.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected result | Failure handling |
|----------|---------------|-----------------|------------------|
| Independent evaluation | Dated signed record covering current platform and both limitations | Record provenance and individual outcomes | Incomplete scope keeps affected gates open |
| Missing evidence | No independent signed record | Gates pending; parent in-progress | Report external dependency |
| Deployment drift | Evaluated seal, ingress, voters or identity differs | Renew measured-profile evaluation | Older smoke evidence supplies no approval |

</frozen-after-approval>

## Open Questions

1. Is an independent signed evaluation available for C4b/C5b/C7? Provide the evaluator's identity, date and signed record or repository path for verification and incorporation; otherwise leave the story blocked on independent review without changing checkpoints or status. Appointment alone is insufficient. The repository cannot establish this external fact.

## Code Map

- Evidence section 4 is authoritative; sections 9.1/9.3 hold today's profile/smoke evidence; section 9.4 is the current unsigned summary.
- The parent story holds separate C4b/C5b/C7 rows, cumulative File List and append-only phase ledger. All implementation tasks are checked.
- The runbook reviewer header, Named divergences and limitation narrative must follow authentic evidence; preserve its runtime Secret inventory.
- `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs`: reuse `ValidateCurrentSecurityRecords`, its independent signed control and `PlatformEvidence_RecordsExecutedSmokeTestAndReviewerState`. Existing guards support both states; no new methods planned.

## Tasks & Acceptance

**Execution:**

- [ ] `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` — verify supplied provenance, independence and scope; record conclusions and each gate disposition; reconcile section 9.4.
- [ ] `docs/operations/openbao.md` — align current reviewer/divergence/limitation summaries with verified outcomes, preserving dated observations.
- [ ] `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` — mirror evidenced gate dispositions; append actual validation and reconcile File List.
- [ ] `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml` and `_bmad-output/implementation-artifacts/epic-31-context.md` — reconcile current summaries only after mandatory gates pass; preserve Story 31.2 status and activation criteria.
- [ ] `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` — execute existing guards and pinned deployment regression against resulting records; preserve signed/unsigned controls.

**Acceptance Criteria:**

- Given authentic independent evaluation of the current profile and both limitations, when provenance and scope are verified, then evidence and summaries agree on evaluator, date and individual outcomes.
- Given absent, non-independent, stale or incomplete evidence, when closeout is considered, then unsatisfied gates remain pending without completion dates and the parent stays in-progress.
- Given satisfied mandatory gates, when verification and workflow review pass, then Story 31.1 can close without platform changes or altered migration requirements.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Use `run-with-temp` to build Server.Tests in Debug with source references and disposable artifacts; retain useful logs outside TMPDIR. Copy the solution marker and required records/manifests into the temporary root for test discovery. Invoke the built assembly with `-class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo`, then method `ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`. Expected: 18 documentation cases and one regression pass without skips. Run `git diff --check`; compare checkpoint/status changes to the signed source. Existing synthetic controls cover independent closure, owner-persona rejection and stale-summary refusal; fixture signatures supply no real approval.
