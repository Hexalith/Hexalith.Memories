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
