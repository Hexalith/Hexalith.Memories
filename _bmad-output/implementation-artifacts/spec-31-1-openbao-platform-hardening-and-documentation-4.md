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

1. Which input should this run use? **Independent signed evaluation available:** provide its repository path or durable reference, evaluator identity, date and independence so the existing closeout can proceed. **No independent evaluation available:** retain this draft and all pending gates. **Redirect to local remediation:** explicitly change scope to the two recorded Memories guard defects (EVAL-03 diagnostic disclosure and EVAL-04 appointment/partial-signature handling); prepare that implementation without claiming independent approval. The owner-attested assistant report does not resolve the external closeout dependency.

## Code Map

- Evidence section 4 is authoritative; sections 9.1/9.3 hold today's profile/smoke evidence; section 9.4 is the current unsigned summary.
- The parent story holds separate C4b/C5b/C7 rows, cumulative File List and append-only phase ledger. All implementation tasks are checked.
- The runbook reviewer header, Named divergences and limitation narrative must follow authentic evidence; preserve its runtime Secret inventory.
- `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs`: reuse `ValidateCurrentSecurityRecords`, its independent signed control and `PlatformEvidence_RecordsExecutedSmokeTestAndReviewerState`. Existing guards cover wholly unsigned and wholly signed records; assigned-but-unsigned and partial-signature states are known defects. The current closeout scope plans execution only; remediation requires an explicit scope decision.
- `docs/governance/openbao-independent-security-review.md`: prepared evaluator handoff, exact measured-profile references, existing technical findings and required signed-return fields. This supporting document supplies no independent approval.
- `_bmad-output/implementation-artifacts/tests/31-1-assistant-security-evaluation-2026-11-16.md` and its JSON companion: owner-requested direct assistant evaluation, live peer-DNS TLS defect, unauthenticated-denial controls and executable reproductions of the diagnostic/review-state defects. Overall technical outcome: changes requested; no independent signature.

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

2026-11-16 continuation: repository investigation and bounded searches of connected Outlook,
Teams, GitHub issues and Pages found no matching independent signed evaluation. An OpenBao
configuration discussion is not a signed evaluation; bounded searches do not establish that no
external record exists. The existing AI review requests changes and expressly supplies no
independent countersignature. Reviewer identity and appointment remain unresolved; Administrator
is accountable for scheduling. The Open Question and draft status remain in place.

Prepared `docs/governance/openbao-independent-security-review.md` and a local, hash-inventoried
review packet under `artifacts/story-31-1/independent-review-handoff-2026-11-16/` for handover.
At packet preparation, the request had not been sent. No evaluator was invented, no external appointment or deployment
was made, and no checkpoint, waiver, parent-story status or Story 31.2 activation rule changed.
The packet distinguishes repository inputs, historical live measurements and unresolved findings.

Subsequent coordination on 2026-11-16: the user supplied Jérôme Piquot's contact,
`jpiquot@itaneo.com`. Because he is the recorded Platform Operations owner, an Outlook request
with the prepared ZIP attached asks him to nominate an independent evaluator and provide contact
details and availability. The send action completed successfully; no recipient delivery or mailbox
copy was independently verified. He remains the scheduling contact, not an independent signatory.
The original attached packet is preserved; the send receipt is retained separately as
`artifacts/story-31-1/independent-review-handoff-2026-11-16/coordination-email.json`.

The owner's subsequent request to perform the evaluations directly was completed as an attributed
assistant technical evaluation. Fresh metadata/status probes confirm the static seal, the single-node
three-server profile, broad ingress and Kubernetes grants. Direct CA-verifying Pod-DNS checks
newly reproduce certificate-name failures for peers 1 and 2; Service-name controls pass without
disabling verification. Two administrative reads without credentials return 403 through a loopback
API-server tunnel; this supplies no CNI or authenticated ACL proof. A fresh Debug/source-reference
build and 19 existing cases pass, while supplemental disposable controls reproduce diagnostic
disclosure and assigned/partial-review-state defects. No live configuration or source implementation
changed. The technical report requests changes; all independent-signature gates remain pending.

At the owner's subsequent signing request, Codex added a typed assistant-author attestation at
2026-11-16T19:56:39Z to the technical report and its structured companion. It records the exact
covered content length and SHA-256, preserves the changes-requested decision and explicitly
claims no cryptographic or independent human signature. The original evidence archive is retained;
`assistant-signed-evaluation.zip` packages the original snapshot, attributed report and receipt.

At 2026-11-16T20:00:55Z, recorded Jérôme Piquot's explicit nominative owner attestation and follow-up
approval verbatim in the report and structured companion. The owner approves the 2026-11-16 report
and its changes-requested conclusions. The existing assistant attribution and prior evidence bundles
remain preserved. The local owner-attestation receipt and `owner-attested-evaluation.zip` bind the
approved report version and the appended owner record; no independent-signature gate is discharged.

2026-11-16 build resumption at `d2a39a84ccf2bcc1f89c428b50591c3c462aaedf`: verified the assistant-covered
report prefix, owner-approved prefix and current full-report SHA-256 against the structured
companion; all three match. The recorded outcome remains changes requested and
`independent_human_signoff` remains false. Refreshed the stale Epic 31 context from current
planning artifacts. No independent signed return is present among the Story 31.1 records.

Planning facts: the remaining intent gap is the independent evaluation or an explicit redirection
to local remediation. Existing closeout work has no deployment, messaging, credential or other
irreversible action. Its footprint remains the listed records and execution of existing guards;
no new callable surface is planned. EVAL-03/EVAL-04 are actionable local defects, but this draft
cannot silently turn evidence incorporation into implementation of those fixes. No checkpoint,
parent-story status, waiver or Story 31.2 activation criterion changed during this resumption.

2026-11-16: User approved local EVAL-03/EVAL-04 remediation and retention of this full closeout
history. The separate implementation spec `spec-31-1-openbao-security-guard-remediation.md` carries
that work; this draft and its independent-evaluation dependency remain pending.

## Spec Change Log

- 2026-11-16: Added the independent-review handoff and search outcome as preparation only; preserved the frozen intent and pending signed-evidence dependency.
- 2026-11-16: Recorded the user-supplied owner contact and completed coordination email requesting independent-reviewer nomination; preserved pending gates and the original attached packet.
- 2026-11-16: Completed the owner's directly requested assistant evaluation, recorded current observations and three guard reproductions, and reported the new peer-DNS TLS defect; preserved the independent-signature requirement and all checkpoint/status values.
- 2026-11-16: Added Codex's dated non-cryptographic author attestation and content digest at the owner's request; preserved the changes-requested outcome and pending independent-signature gates.
- 2026-11-16: Recorded Jérôme Piquot's explicit nominative owner approval and follow-up confirmation verbatim, with provenance and report-version hashes; preserved remediation findings, independent-signature gates and draft closeout status.

## Review Triage Log

## Verification

Use `run-with-temp` to build Server.Tests in Debug with source references and disposable artifacts; retain useful logs outside TMPDIR. Copy the solution marker and required records/manifests into the temporary root for test discovery. Invoke the built assembly with `-class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo`, then method `ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`. Expected: 18 documentation cases and one regression pass without skips. Run `git diff --check`; compare checkpoint/status changes to the signed source. Existing synthetic controls cover independent closure, owner-persona rejection and stale-summary refusal; fixture signatures supply no real approval.

Preparation verification executed on 2026-11-16 against source commit
`5c1c22583fb4037997b266942a4fd4e05a245ebf`: disposable Debug/source-reference Server.Tests
build passed with zero warnings/errors, all 18 documentation cases passed, and the one pinned
deployment regression passed; zero failed/skipped. NuGet audit remained enabled. Exact commands,
results and logs are in `artifacts/story-31-1/independent-review-handoff-2026-11-16/validation.json`
and its sibling logs. These checks validate the existing records; they do not resolve the prior
AI review's uncovered states or supply independent security approval.

Both authored Markdown files passed local-link, CRLF and credential-shape checks without printing
candidate material. The frozen intent matches HEAD verbatim; draft status and authoritative
checkpoint/status records are preserved. `git diff --check` passed. No new tests or test-method
changes were introduced, and no runtime, deployment, credential or tenant surface changed.
