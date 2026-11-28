# OpenBao independent security review handoff — Story 31.1

**Status:** Coordination request sent; independent review unsigned; independent evaluator unassigned.
**Prepared:** 2026-11-16.
**Scheduling and escalation owner:** Administrator / Platform Operations (`jpiquot`).
**Repository source commit:** `5c1c22583fb4037997b266942a4fd4e05a245ebf`.

The outstanding work is an independent evaluation of the measured `hexalith-keys` platform,
with separate conclusions for C4b, C5b and C7. This handoff collects the existing evidence and
the questions to answer. It supplies no signature, risk acceptance, waiver or deployment authority.
The source commit identifies the input records; it predates this handoff.

## Current checkpoint state

| Checkpoint | Evaluation required | Current state | Completion date |
| :--- | :--- | :--- | :--- |
| C4b | Independent countersignature for the static file-based seal limitation | pending / not complete | none |
| C5b | Independent countersignature for namespace-wide port 8200 ingress | pending / not complete | none |
| C7 | Independent platform-wide evaluation against the measured profile | pending / not complete | none |

The C7 waiver expired on 2026-10-26. Story 31.1 remains in-progress. Its authoritative current
review record is [platform evidence, section 4](../../_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md#4-security-reviewer-evaluation-task-6-checkpoint-c7),
with the summary in section 9.4. The operations owner and the persona acting for that owner
do not supply the required independence. A coding-agent report also supplies no countersignature.

## Profile to evaluate

These are existing observations from 2026-11-16, rather than new measurements made for this handoff.
Use [evidence section 9.1](../../_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md#91-current-measured-profile)
and the [operations runbook](../operations/openbao.md) for the full configuration and divergences.

| Subject | Recorded observation and evidence boundary |
| :--- | :--- |
| Target | Kubernetes context `jpiquot@local`; namespace `openbao`; release / StatefulSet `hexalith-keys`; application namespace `hexalith-memories` |
| Server identity | OpenBao `2.6.2`, image `quay.io/openbao/openbao:2.6.2@sha256:11fd73a2102cda9c55d5d881a8c3210303146a7ec1e8ac76f526e175c6d24641`; all three observed runtime imageIDs match |
| Chart identity | Pod label `openbao-0.29.5`; ten release metadata objects observed. Chart digest and release contents were not measured |
| Availability | Three healthy server replicas on the single node `node1`; HA enabled and active / standby endpoints observed. Voting membership and failover were not exercised |
| Seal | Declared static seal with a file in Secret `openbao-seal`, colocated with the encrypted storage trust boundary. Loaded-HCL reconciliation remains unproven |
| Ingress | Base policy admits every pod in `hexalith-memories` and `cert-manager` on 8200. Additional policies `deployment-seal-external` and `ext-secrets-1-custody-provider-29a4ace57e04` also admit 8200 |
| Service exposure | Four `hexalith-keys*` Services are ClusterIP. Extra `deployment-seal-transit` is NodePort `8200:30820/TCP`; Traefik routes and actual reachability require separate evidence |
| Recovery | A 2-of-3 recovery threshold is reported. Data, audit and snapshot storage share the node failure domain; successful snapshot Jobs establish no off-cluster restore proof |
| Smoke evidence | Section 9.3 records the CA-only status smoke. Status success establishes no audit-write, ACL, cross-prefix, restart-recovery or independent-approval result |

Repository-approved OpenBao `2.6.4` and chart `0.29.6` inputs are distinct from this measured
deployment. The older smoke-client image is also distinct from the measured server image.
The reviewer must bind conclusions to the evaluated deployment and remeasure drift before signing.

## Questions for the evaluator

For **C4b**, evaluate the consequence of combined access to seal material and encrypted storage,
the custody of recovery shares, and the actual compensating controls. Namespace Secret access
alone does not prove PVC-content access. Record whether the existing static-seal limitation is
accepted, rejected or requires conditions, with the responsible owner and external-seal reopen trigger.

For **C5b**, evaluate the complete additive ingress surface, including unrelated application pods,
cert-manager, the extra policies and NodePort. Distinguish configured access from tested reachability
and authentication / authorization evidence. Record the conclusion and conditions for the existing
namespace-wide ingress limitation, including the trigger to narrow access to actual consumers.

For **C7**, evaluate the whole measured platform, including identity/version drift, loaded
configuration, TLS, Kubernetes permissions, token scope, audit behavior and availability/recovery.
The two limitation records remain exactly the existing two; record other defects as findings and
owned divergences. Passing documentation guards supplies no runtime qualification.

The existing [AI technical review](../../_bmad-output/implementation-artifacts/tests/31-1-ai-security-review-2026-11-16.md)
requested changes. Its findings remain inputs for investigation, rather than completed fixes:

- Credential-shaped test failures can expose matching material in assertion diagnostics.
- Review-state guards do not represent assigned reviewers or separate partial signatures adequately.
- Advisory applicability against the deployed features remains unresolved.
- Audit coverage claims exceed the executed evidence; audit writes, rotation and capacity behavior need evidence.
- Public-certificate observations and the runbook's expiry-check instructions need reconciliation.
- Existing ingress, Kubernetes privilege, custody, loaded-configuration and recovery gaps remain owned.

The machine-readable [findings companion](../../_bmad-output/implementation-artifacts/tests/31-1-ai-security-review-2026-11-16.json)
preserves individual finding identifiers. Verify applicability and resolution against the source
revision being signed; this handoff does not disposition findings or assert a favorable clearance.

Following the owner's request, the assistant also completed a direct
[technical evaluation](../../_bmad-output/implementation-artifacts/tests/31-1-assistant-security-evaluation-2026-11-16.md)
on 2026-11-16. It records newly reproduced Pod-1/Pod-2 DNS certificate failures, unauthenticated
403 controls and executable reproductions of the guard defects. Its overall result is changes
requested. It supplements the prepared independent-review packet and supplies no independent signature.
At the owner's request, Codex added a dated, typed author attestation on 2026-11-16. That attribution
is non-cryptographic, retains the changes-requested conclusion and leaves the independent review open.
Jérôme Piquot subsequently supplied an explicit nominative owner approval, recorded at
2026-11-16T20:00:55Z in the report's owner-attestation appendix and structured companion. Its exact
declaration and follow-up confirmation are preserved. It approves the changes-requested report in
the owner capacity; it leaves the independent review and remediation findings open.

## Required signed return

The reviewer should return a signed report through the existing governance channel. Record each
checkpoint separately so different evaluators, dates, conditions and partial completion remain visible.

| Field | Required content |
| :--- | :--- |
| Evaluator | Identifiable person / authority and role |
| Independence | Explicit relationship to Platform Operations, the implementation and the owner; explain why the evaluation is independent |
| Evaluation date | Actual date of evaluation and signature |
| Provenance | Signed source document or durable verifiable approval reference; include the signature-verification method |
| Evaluated profile | Target context / namespace / release, measurement timestamps, exact server image identity, availability, seal and effective ingress |
| C4b outcome | Specific static-seal conclusion, reasoning, conditions, evidence references, owner and reopen trigger |
| C5b outcome | Specific ingress conclusion, reasoning, conditions, evidence references, owner and reopen trigger |
| C7 outcome | Platform-wide conclusion, unresolved findings and their owners / required evidence |
| Limitations of evaluation | Unexecuted checks, uncertainty and changes that invalidate the conclusions |

Missing, stale, non-independent or incomplete evidence leaves the affected gates pending without
completion dates. An appointment or unsigned report does not discharge a gate. Reconcile authentic
per-gate outcomes across the evidence, parent story, runbook, epic and sprint records only after verification;
resolve guard limitations without inventing all-gates completion. Preserve the historical waiver.
Story 31.2 activation remains tied to C1/C2/C3/C4a/C5a/C6; this handoff changes no migration gate.

## Ready-to-send request

> Please independently evaluate the measured OpenBao `hexalith-keys` platform described in this packet.
> Return a dated signed report identifying your role and independence, the exact evaluated profile,
> and separate conclusions for C4b (static seal), C5b (namespace-wide ingress) and C7 (whole platform).
> Address the existing technical review's unresolved findings and state any conditions or checks not
> performed. The recorded C7 waiver expired on 2026-10-26; no current independent sign-off is recorded.

At packet preparation, the request was local and no evaluator had been appointed or contacted.
On 2026-11-16, following the user's provision of the contact, a coordination request was sent through
Outlook to Platform Operations owner Jérôme Piquot (`jpiquot@itaneo.com`), with the prepared packet
attached. It asks him to nominate an independent evaluator and provide that person's contact and
availability. The send action completed successfully; recipient delivery has not been independently
confirmed. Jérôme is the coordination contact, rather than an appointed independent evaluator.
Administrator remains accountable for arranging the evaluation; all three gates remain pending.

## Packet and verification

The local packet under `artifacts/story-31-1/independent-review-handoff-2026-11-16/` contains a
ZIP with this handoff and the referenced evidence, story, configuration and test inputs. Its manifest
records SHA-256 hashes of the captured file bytes and the source commit; hashes provide integrity
checks, rather than independent authentication or a signature. Local capture timestamps do not
renew the earlier live measurements. Original local probe captures and external signed evidence
are not included or implied by the repository records.

The packet also records bounded searches of the repository and connected sources. No matching
independent signed evaluation was found in those searches; this does not prove that no external
record exists. Verification results and exact commands are retained beside the packet. No Secret
payloads, private keys or credentials should be included in the returned report or packet.

The attached ZIP remains the preparation snapshot with its original manifest and hashes. The later
coordination receipt is retained separately as `coordination-email.json` in the same local directory;
it does not change the attached bytes or supply a signature. The immediate post-send mailbox search
returned no matching message, so the record relies on the successful send action and claims no
additional mailbox or delivery verification.


## Guard remediation — 2026-11-16

The bounded local remediation addresses EVAL-03 and EVAL-04 from the owner-attested assistant
report. The credential guard now emits only a trusted relative path, category and count. Fixed
synthetic controls exercise its actual published-record path and check the full exception rendering
for detected values and surrounding text. Detection patterns and the labelled digest/identifier
exceptions are retained.

The review-state guard now distinguishes independent appointment, individual checkpoint signatures
and parent completion. An appointed reviewer supplies no signature or completion date. Each completed
checkpoint needs a valid date, current-profile and checkpoint scope, signer-bound independence and
verified provenance. Different independent authorities and dates may cover C4b, C5b and C7; pending
peers remain without completion dates. Parent completion also requires C1/C2/C3/C4a/C5a/C6.

The existing wholly unsigned and whole-review signed formats remain supported. The whole-review
format now requires an explicit `Evaluator provenance` field in section 4, in addition to its
`Evaluator independence` field: `Verified signature by <signer>; source: <durable signed reference>;
method: <signature-verification method>`. This is a required evidence field, rather than inferred
approval from an appointment. The per-checkpoint format uses `Evaluation status` beginning
`Per-checkpoint evaluations.` and separate `Checkpoint <id> evaluation`, `Checkpoint <id> provenance`
and `Checkpoint <id> independence` fields. Its checkpoint states must agree with the story's exact
rows, section 9.4, the runbook divergence/limitation text, the epic and sprint summary. These guards
validate recorded evidence and consistency; they do not authenticate an external signature themselves.
A maintainer must verify authentic external evidence before entering a real signed state.

The final Debug/source build passed with zero warnings/errors; all 74 documentation cases and the
pinned deployment regression passed without skips. Focused discovery grew from 18 to 27 methods
(+9), and executed cases from 18 to 74 (+56). Verification is recorded in the remediation spec and
the Story 31.1 phase ledger, with retained local logs under `artifacts/story-31-1/guard-remediation-2026-11-16/`. Synthetic controls change no real
approval. C4b/C5b/C7 remain pending / not complete without completion dates, the independent evaluator
remains unassigned, Administrator remains accountable, and the expired 2026-10-26 waiver remains
historical. The earlier signed report and its findings are preserved byte-for-byte. Independent
sign-off and the other platform findings, including TLS identity, advisory applicability, audit,
ingress, privilege, custody, loaded-configuration and recovery evidence, remain open. The separate
closeout draft remains pending; Story 31.1, its epic and sprint remain in-progress, and Story 31.2's
activation gate is unchanged.


### Review corrections — 2026-11-16

The earlier 74-case run above remains historical evidence for that implementation. Review exposed
additional consistency gaps, now corrected locally: review-state Markdown handling preserves
underscore identifiers; signatures require the affirmative canonical scope, with C7 explicitly
covering both accepted limitations and the amended AC2 qualifier; explicit coverage denials and
unresolved/contradictory authority evidence are refused. Completed source references and an explicit
“reviewer is not the platform owner” basis remain valid controls.

Pending numeric completion-date labels, conditional or unreviewed/unevidenced prerequisites,
waiver-extension or missing-reassessment triggers, opposite duplicate sprint declarations and extra
conflicting checkpoint statements are now refused. The C7 evidence fact and a fixed C7-only signed
control share the extracted pure C7-state validator; a missing separate C7 signature is rejected.

Focused review verification passed **76 cases across 20 selected methods**, with **0 errors,
failures, skips or not-run cases**, after a Debug/source build with **0 warnings/errors**. This
selection covers review-state controls and the two real current-state/C7 facts; its count is not a
replacement full-class count or a subtraction from the earlier 74-case run. Exact commands, selected
methods and source hashes are retained under
`artifacts/story-31-1/guard-remediation-2026-11-16/review-corrections/`. The parent performs the full
prescribed verification separately. No real signature, checkpoint cell, approval state, measurement
or activation criterion changed; independent sign-off and the remaining platform findings stay open.


### Final parent verification — 2026-11-16

After the review corrections, a fresh complete documentation-class run passed **106 cases** and
the pinned deployment regression passed **1 case**, with zero errors/failures/skips/not-run cases.
The Debug/source build with package auditing enabled reported zero warnings/errors. Focused method
discovery is **18 → 37 (+19)**, with none removed; executed cases are **18 → 106 (+88)**. All 16
review findings are fixed, with none deferred. The earlier 74-case and focused 76-case runs remain
historical evidence for their respective source versions and selections.

The parent recorded exact commands and settled-source, report-byte and checkpoint-preservation
checks in `artifacts/story-31-1/guard-remediation-2026-11-16/parent-verification.json`. Only the local
guard-remediation spec is complete. The actual independent reviewer remains unassigned; C4b/C5b/C7
and parent status remain unchanged. These results supply no independent signature or platform
qualification and do not resolve the other platform findings.


### Technical sign-off — guard remediation — 2026-11-16T21:16:18Z

**Signed by:** Codex (AI assistant).
**Capacity:** Implementer and coordinating technical reviewer of the local guard remediation.
**Form:** Typed technical attestation; non-cryptographic.
**Decision:** I sign off the bounded EVAL-03/EVAL-04 guard remediation and resolution of its
16 implementation-review findings against the exact source versions below.

The previously executed verification passed **106 documentation cases and 1 deployment regression**
with zero errors, failures, skips or not-run cases; the Debug/source build with auditing enabled
reported zero warnings/errors. No implementation-review finding was deferred. At signing, source
hashes and the recorded results were rechecked; no new runtime/platform evaluation is asserted.

The source remains uncommitted. Baseline `d2a39a84ccf2bcc1f89c428b50591c3c462aaedf` identifies repository context; the
SHA-256 values bind the actual verified worktree bytes, not a claimed implementation commit.

| Verified source | SHA-256 of exact worktree bytes |
| :--- | :--- |
| `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.SecurityControls.cs` | `96ce45cb0d9b74ca97a3f937d277afba9ed242c652aca0de44c7ab2008f406f5` |
| `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.SecurityGuards.cs` | `e8aacb55b64d022751b268b0916daeaaf75246ae42c77fc98e086aac38b36ccf` |
| `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` | `817d5fb7fbeded2ae3eeea273446592b764642d26aae3f3dce8129f5fabc8eea` |

Verification-record SHA-256: `6b931096a5c953ea6f39607fde4b7f682d9e9ea8a542e9c4876ce6024b0e3210`.
This handoff's covered pre-sign-off prefix is **17729 bytes**,
SHA-256 `cec39af0226a3d1b45483adae45e41f82b9d9c879958710f4bde6e731d5d4266`. The completed remediation spec's covered prefix is
**20976 bytes**, SHA-256 `bff27d9737569d885419b3a737a2549cbb5a80b0d2c14c1da61c2c8d9598c3b2`.
Prefixes preserve the existing UTF-8/CRLF bytes; this appended attestation is outside those prefixes.
The local receipt `artifacts/story-31-1/guard-remediation-2026-11-16/technical-signoff.json` records
the same attribution, exact source and evidence hashes, covered prefixes and resulting document hashes.
These hashes provide integrity checks rather than external authentication.

This is my assistant technical sign-off, not an independent human countersignature or an inferred
owner declaration. C4b/C5b/C7 remain pending without completion dates; Story 31.1, its epic and
sprint remain in-progress. Independent evaluation, other platform findings and the separate
closeout draft remain open. No waiver, migration activation or Production qualification changed.

## Story 31.1 disposition on 2026-11-22

On 2026-11-22, Jérôme Piquot (`jpiquot`) closed Story 31.1 by sole-owner risk acceptance for C4b, C5b and C7. No independent countersignatures were obtained. EVAL-01 remains open with Platform Operations. The earlier handoff and technical signature record the independent-review request and its status on November 16. The July C7 waiver expired on 2026-10-26. The November 16 technical evaluation requested changes. The owner declaration, its scope and the peer-TLS remediation reference are recorded in [platform evidence section 4](../../_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md#4-security-reviewer-evaluation-task-6-checkpoint-c7).

## Epic 31 retrospective reconciliation on 2026-11-28

The pre-sign-off handoff header and checkpoint table above (`lines 1–263`, including the 17,729-byte covered prefix) are preserved verbatim as the historical November 16 record. Their `in-progress`/`pending` statements are superseded by Story 31.1's 2026-11-22 sole-owner closeout, Story 31.2's 2026-11-28 completion (which reassessed the Story 31.2-activation trigger), Epic 31's `accepted-with-open-items` verdict, and `DW-791` in `deferred-work.md` tracking `EVAL-01`.
