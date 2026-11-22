---
title: '31.1: Close OpenBao documentation under the approved sole-owner decision'
type: 'chore'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '0d79ebfbd8c16b49ef551ce3194198e5d8e990c1'
closeout_base_commit: '719820dbca5fb95e2f16f796b8a1124b2194d210'
story_key: '31-1-openbao-platform-hardening-and-documentation'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

On 2026-11-22 the sole contributor, Platform Operations owner Jérôme Piquot (`jpiquot`), approved the following Story 31.1 decision in the project conversation:

> I approve sole-owner closeout for 31.1, accept the documented static seal and namespace-wide ingress, and will track the peer-TLS finding separately.

Record the owner decision as the basis for closing the story. The earlier assistant technical signature covers the build and test results. The historical independent-review route and the expired July waiver remain distinguishable from this decision.

## Boundaries & Constraints

The exception covers C4b (static seal), C5b (namespace-wide ingress) and C7 (sole-owner closeout of the documented profile under amended AC2). Preserve the absence of independent countersignatures and all historical measured findings. EVAL-01 stays open with the owner in the existing peer-TLS remediation spec. Story 31.2's status and activation criteria, Epic 31 status, Production qualification, other findings and all runtime assets are outside this decision. No deployment or external actions.

## I/O & Edge-Case Matrix

| Scenario | Evidence | Result |
| --- | --- | --- |
| Approved story-specific exception | Exact declaration, owner, date and provenance agree with the recorded profile, all required checkpoint dispositions and current summaries | Story 31.1 done under sole-owner risk acceptance |
| Missing, altered or misattributed approval, missing mandatory checkpoint, contradictory summary, independent sign-off claim or peer-TLS closure credited to this decision | Incomplete or inconsistent evidence | Validation rejects the closeout |
| Ordinary unsigned or independently signed records | Existing pending, appointment or per-checkpoint evidence | Existing review paths and regression controls continue to apply |

</frozen-after-approval>

## Code Map

- Platform evidence §4 holds the authoritative decision and §9.4 its current summary. Bind the owner decision to the measured profile (§9.1, 2026-11-16), technical outcome, and the EVAL-01 peer-TLS remediation spec.
- The story checkpoint table, runbook, governance handoff, epics, epic context and sprint status publish current state. Preserve the dated record and the signed November 22 verification section. Update current language that otherwise contradicts this decision.
- The existing `OpenBaoPlatformDocumentationTests` partial class validates evidence, reviewer identity, cross-document state and mandatory parent prerequisites. Extend that logic narrowly for this explicit dated exception. Use positive and negative controls and retain the independent-review tests.
- The existing `spec-31-1-eval-01-peer-tls-remediation.md` owns the open finding. Reconcile any current parent-status assertion there and retain its pending execution and approval requirements.

## Tasks & Acceptance

- [x] Record the owner, date, exact declaration, provenance, scope and decisions for the three checkpoints in the evidence. Record EVAL-01 ownership and reference the existing open work.
- [x] Reconcile story, sprint, epic summary/context, runbook, and handoff. Mark only Story 31.1 done and give each affected checkpoint an accurate dated owner disposition. Append the story phase record.
- [x] Update the documentation guards and regression controls for the matrix. Retain validation of all other mandatory prerequisites and reject owner/persona approval on the independent route.
- [x] Build Server.Tests with .NET 11 in Debug and run the entire documentation class and pinned deployment regression. Record actual case counts and results. The prior disposable build under `/workspace/work/story-31-1/build` and the existing SDK/package cache are available. From the module root use `DOTNET_ROOT=/workspace/.memories-dotnet`, `PATH=/workspace/.memories-dotnet:$PATH`, `DOTNET_CLI_HOME=/workspace/.memories-dotnet-home`, `NUGET_PACKAGES=/workspace/.memories-nuget/packages`, `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --artifacts-path /workspace/work/story-31-1/build --disable-build-servers -m:1 /nr:false -p:MinVerVersionOverride=1.0.0`. Run the built xUnit assembly directly using the selectors under Verification. Use distinct log names for the new run.

Given the dated owner decision, when the records and regression checks agree, Story 31.1 closes on a sole-owner basis. Given a missing prerequisite or a claim beyond that decision, the validator refuses closure. The changes-requested historical report and the open peer-TLS finding retain their provenance.

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

2026-11-16T21:20:29Z: Resumed Story 31.1 planning at `14d7ea4fc0b149e4545b37e7ea75ce20d9e5ddd8` after the completed
EVAL-03/EVAL-04 remediation. Verified the current guard-source, recorded build/test logs, completed
remediation records and attested prefixes against `technical-signoff.json`; all hashes match. Its
recorded Debug/source build has zero warnings/errors, with 106 documentation cases and one pinned
deployment regression passing without skips. No build or test rerun is claimed by this resumption.

Repository investigation and bounded Outlook/Teams searches found no new independent signed return.
Outlook returned the existing evaluator-nomination coordination messages; Teams returned no matching
messages under the selected date filter. These searches do not establish that no external record
exists. The authoritative evidence and parent checkpoint rows still record C4b/C5b/C7 pending,
without completion dates, and the independent evaluator unassigned. The sole remaining intent gap
is the authentic independent evaluation reference. No irreversible action is planned; the current
footprint is this existing closeout draft, with no new callable surface. Preserve the previously
approved full history, frozen intent, immutable reports, parent/epic/sprint states and Story 31.2
activation gate. Further Platform Operations findings require a separate scope decision.

2026-11-16: The user's “do evidence” instruction authorized fresh technical collection and local
verification. Created `tests/31-1-security-evidence-refresh-2026-11-16.md` and its structured companion
beside this draft. Fifteen metadata projections, six CA-verifying peer/control probes, new direct
source-Pod HTTP checks and three audit-file metadata checks are recorded with exact arguments and
UTC timestamps. The fresh Debug/source build passed with zero warnings/errors; all 106 documentation
cases and the pinned deployment regression passed without skips. Direct redis-stack-0 transport
returns health 200 and two unauthenticated 403s with certificate verification explicitly disabled;
its default trust-store attempt fails. Pod-1/Pod-2 DNS identity failures remain reproducible while
Service-name controls pass. Technical outcome remains changes requested.

The new evidence ZIP, input snapshots, scripts, logs and SHA-256 manifest are retained under
`artifacts/story-31-1/evidence-refresh-2026-11-16-2125/`. Prior attested report bytes, checkpoint rows,
parent/epic/sprint states, waiver and Story 31.2 activation rules are preserved. This completes the
requested technical-evidence work; it does not supply an independent human authority or resolve the
remaining independent signed-return question. No deployment, credential read, external message,
staging, commit or push was performed.

## Spec Change Log

- 2026-11-16: Added the independent-review handoff and search outcome as preparation only; preserved the frozen intent and pending signed-evidence dependency.
- 2026-11-16: Recorded the user-supplied owner contact and completed coordination email requesting independent-reviewer nomination; preserved pending gates and the original attached packet.
- 2026-11-16: Completed the owner's directly requested assistant evaluation, recorded current observations and three guard reproductions, and reported the new peer-DNS TLS defect; preserved the independent-signature requirement and all checkpoint/status values.
- 2026-11-16: Added Codex's dated non-cryptographic author attestation and content digest at the owner's request; preserved the changes-requested outcome and pending independent-signature gates.
- 2026-11-16: Recorded Jérôme Piquot's explicit nominative owner approval and follow-up confirmation verbatim, with provenance and report-version hashes; preserved remediation findings, independent-signature gates and draft closeout status.

- 2026-11-16: Resumed closeout after completed guard remediation; updated the current Code Map, remaining evidence question and expected verification count while preserving all historical entries and the frozen intent. No independent-evidence gate changed.

- 2026-11-16: Completed the requested fresh technical evidence and verification in a separate report/companion and integrity bundle; preserved the closeout draft, frozen intent and independent-signature dependency.

## Review Triage Log

The November 22 review covers the closeout diff against `closeout_base_commit`. The original draft baseline also spans unrelated work merged into main.

| Review finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind review 1, post-change verification | Medium | The signed 106-case result predates the new route. Record the post-change build and both selectors separately. |
| Blind review 2, phase and file list | Medium | The implementation entry identifies the changed files. Append the final review and counts to the parent ledger. |
| Blind review 3, Task 6 instruction | Medium | The historical unsigned instruction needs a dated reference to the approved exception for this story. |
| Blind review 4, handoff entry point | Low | The November 16 prefix is signed. Link active records to the November 22 disposition within the handoff. |
| Blind review 5, prerequisite dates | Medium | The actual decision date bounds all prerequisite completion dates, including when rerun later. |
| Blind review 6, July waiver fields | Medium | Require the historical approver, C7-only scope, rationale, controls and reopen trigger. |
| Blind review 7, technical signature | Medium | Verify the linked signature and the previously published 1,205-byte section and digest. |
| Blind review 8, declaration fixture | Low | A fixed digest binds the exact declaration independently of the synthetic fixture's text constant. |
| Blind review 9, preceding sprint comments | Medium | A conflicting disposition can precede the dated summary in the same tracking block. Cover both positions. |
| Blind review 10, contradictory dispositions | Medium | Cover the reported incomplete checkpoint and reversed-word-order peer-TLS closure claims. |
| Blind review 11, assertion output | Medium | Use a field-only diagnostic for a corrupted owner decision cell. Test the exception text with synthetic input. |
| Edge review 1, later prerequisite | Medium | The fixed decision-date comparison rejects the November 23 example. |
| Edge review 2, section 4 prose | Medium | Check section 4’s decision narrative separately from its table and section 9.4. |
| Edge review 3, scope of July waiver | Medium | Require and exercise the historical C7-only waiver scope. |
| Edge review 4, earlier sprint comment | Medium | Exercise contradictory text before the November 22 disposition. |
| Edge review 5, Production claims | Medium | Reject a claim granting Production qualification through this decision. |
| Edge review 6, missing phase entry | Medium | The implementation and final review entries provide the closeout history. |
| Edge review 7, earlier test count | Medium | Keep the signed pre-closeout result and publish the current result separately. |
| Verification review 1, prerequisite cutoff | Medium | Run the prerequisite control with a clock later than the decision. |
| Verification review 2, isolated narrative conflict | Medium | Corrupt only the section 4 narrative to verify that the authoritative record is checked. |
| Verification review 3, altered signed section | Medium | Exercise a one-byte change in the signed content and a missing signature. |

## Verification

Use `run-with-temp` to build Server.Tests in Debug with source references and disposable artifacts; retain useful logs outside TMPDIR. Copy the solution marker and required records/manifests into the temporary root for test discovery. Invoke the built assembly with `-class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo`, then method `ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`. Expected after the completed guard remediation: 106 documentation cases and one regression pass without skips. The earlier 18-case preparation result below remains historical. Run `git diff --check`; compare checkpoint/status changes to the signed source. Existing synthetic controls cover independent closure, owner-persona rejection and stale-summary refusal; fixture signatures supply no real approval.

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

### 2026-11-22 closeout check

Memories was at `719820dbca5fb95e2f16f796b8a1124b2194d210`. All ten direct
submodules matched their fetched `origin/main`. The synchronized repositories and
Memories GitHub issues and pull requests updated since November 16 yielded no
independent signed evaluation. The assistant and owner report prefixes and full
report each matched the SHA-256 in the JSON companion. The companion records
`independent_human_signoff: false`. Evidence sections 4 and 9.4 still list the
reviewer as unassigned. The Administrator owns scheduling for C4b, C5b and C7.

The Debug build of `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj`
used .NET SDK 11.0.100, `-p:UseHexalithProjectReferences=true` and the repository's
NuGet audit settings. It completed with zero warnings and errors. The xUnit runner
executed the `OpenBaoPlatformDocumentationTests` class (106 cases) and
`ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`
(1 case). All 107 passed, with zero skipped. These checks cover the tracked records
and deployment manifests. The missing signed evaluation remains the closeout dependency.

### Signature for the November 22 verification

At the contributor’s request (“I am the only contributor, sign!”), I attest that
the November 22 Debug build completed with zero warnings and errors and that
all 106 OpenBao documentation cases and the deployment regression passed.
I verified the build and xUnit logs against the results recorded above.

Signed electronically by **/s/ Codex**, AI assistant who ran the verification, at
`2026-11-22T11:43:12Z` (UTC). This typed signature covers the preceding
`2026-11-22 closeout check` from its heading through its final CRLF
(1205 bytes, SHA-256 `748731eb663b76bdc7a9d53724683c48c46dc36c3db3fc60e325fecf7aa45b01`).
The signature covers repository verification. The November 16 platform evaluation
records a changes-requested decision, including the open peer TLS finding.

### Implementation verification on 2026-11-22

Platform evidence §4 records the exact conversation declaration, owner, date, measured profile and three dispositions. Story 31.1 is done under that decision. EVAL-01 remains with Jérôme Piquot in the separate peer-TLS draft. Of 238 sprint status values, only Story 31.1 changed. The six migration prerequisites, Story 31.2 status and Epic 31 status retain their prior values.

Using SDK 11.0.100, the Debug Server.Tests build with project references and NuGet audit enabled passed with 0 warnings and 0 errors. The command in Tasks & Acceptance used the existing disposable build directory. Both runs used the built xUnit assembly directly, through `run-with-temp.sh` with the solution marker, required records and manifests copied into the temporary discovery root.

| Selector | Passed | Failed | Errors | Skipped | Not run |
| --- | ---: | ---: | ---: | ---: | ---: |
| `-class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo` | 135 | 0 | 0 | 0 | 0 |
| `-method Hexalith.Memories.Server.Tests.Deployment.ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal -parallel none -noLogo` | 1 | 0 | 0 | 0 | 0 |

The five new test methods add 29 cases to the prior 106. The controls exercise the dated owner route, mismatched or missing approvals and checkpoints, inconsistent current records, and misattribution of independent approval or peer-TLS closure. Local logs are `/workspace/work/story-31-1/sole-owner-closeout-build.log`, `sole-owner-closeout-documentation.log` and `sole-owner-closeout-deployment.log` in the same directory. The November 22 signed verification section (1,205 bytes) and November 16 signed governance handoff prefix (17,729 bytes) each matched its recorded SHA-256. `git diff --check` passed.

### Focused review fixes on 2026-11-22

The decision guard now verifies the signed November 22 section, the Codex attestation, the exact declaration digest and the original C7-only waiver. Focused controls reject conflicting current statements, including claims about EVAL-01 and Production qualification, and check that a malformed decision cell cannot disclose its value in diagnostics.

The Debug source-reference rebuild completed with zero warnings and errors. The eight `SoleOwnerCloseout_*` methods and the two real-record tests, `CurrentSecurityCheckpoints_AgreeWithUnsignedEvidenceAndReviewerDivergence` and `PlatformEvidence_RecordsExecutedSmokeTestAndReviewerState`, passed all 39 selected cases with zero failures, errors, skips or not-run cases. Discovery with `-preEnumerateTheories -list tests` found 143 cases in the documentation class. Local logs are `/workspace/work/story-31-1/sole-owner-review-patched-build.log`, `sole-owner-review-patched-focused.log` and `sole-owner-review-discovery.log`.

### Final review verification on 2026-11-22

The final .NET 11 Debug build completed with zero warnings and errors. The full
OpenBao documentation class passed 143 cases and the pinned deployment regression
passed one, with zero failed, skipped or unexecuted cases. All 37 prior method
selectors were retained, with eight added. The review findings are resolved.

The current story, sprint and evidence agree on all three dated owner dispositions.
Both earlier technical attestations retain their original signed content. EVAL-01
is tracked separately in the peer-TLS remediation spec.
