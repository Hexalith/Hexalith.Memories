# Story 31.1 — owner-requested AI security review

Date: 2026-11-16. Reviewer: Codex, with three separate AI review lenses. Requester and sole project owner: Jérôme Piquot (`jpiquot`), who stated: “I am the owner and only contributor. do the review”.

## Outcome

The requested technical review is complete. Changes are needed before a favorable security evaluation: the secret-detection test can echo a credential into CI logs; reviewer-state guards cannot represent assignment or partial completion; current advisories have not been dispositioned against the deployed features; and the claimed audit coverage exceeds the evidence. Existing platform limitations remain visible and owned.

This is an **owner-requested AI review**. It supplies technical findings and reproducible observations; it supplies no independent human countersignature, new risk acceptance, renewed waiver, Production qualification or deployment authorization. The existing checkpoint and sprint status values are unchanged. The review has been performed without requiring another contributor; this report does not rename the reviewer as an independent authority.

The prior closeout draft remains a draft; this review does not silently approve its proposed signed-closure work or rewrite its independence requirement. Actual findings now provide concrete follow-up work regardless of that governance decision.

## Scope and evidence

Reviewed source commit: `0d79ebfbd8c16b49ef551ce3194198e5d8e990c1`. Review inputs were the four `deploy/openbao/*.yaml` manifests, the operations runbook, `OpenBaoPlatformDocumentationTests`, and the current measured/evaluation/checkpoint evidence. The untracked closeout draft was context rather than a reviewed deployment change.

Read-only probes at **2026-11-16T17:00:46Z** against context `jpiquot@local`, namespace `openbao`, confirmed:

- Exactly one observed node, `node1`; all three server Pods Running/Ready on it.
- Each admitted server Pod runs OpenBao **2.6.2**, with the previously recorded digest, non-root identity, RuntimeDefault seccomp, no privilege escalation, all capabilities dropped and a read-only root filesystem.
- Each server Pod automounts its ServiceAccount token, declares readiness `bao status`, and uses HTTPS liveness. TLS/seal volume mounts are read-only.
- The base namespace-wide ingress rule and both additional policies remain. The four `hexalith-keys*` Services are ClusterIP; `deployment-seal-transit` remains a NodePort. These are object observations, not proof of reachability from every source or CNI enforcement.
- A CA-verifying `bao status -format=json` command returned initialized true, sealed false, Raft storage, HA enabled, version 2.6.2 and a 2-of-3 recovery threshold. This is a status observation, not a secret-read, audit-write, membership or failover test.

At **17:01:54Z**, an ephemeral loopback-only Kubernetes port-forward collected the public certificate from `service/hexalith-keys`, with SNI `hexalith-keys.openbao.svc.cluster.local`. TLS 1.3 was negotiated; the presented certificate reports expiry **2027-08-20 13:38:39 UTC** and includes the Service DNS name. This connection intentionally did not validate certificate trust and bypassed NetworkPolicy through the API-server tunnel. It inventories the served certificate only; it is not a trust/reachability qualification or a per-Pod certificate inventory. The port-forward was stopped and awaited. No Kubernetes Secret payload or private key was read.

Exact commands, projection fields, timestamps and observations are retained in `artifacts/story-31-1/security-review-2026-11-16/review-*.json`. The source packet and input hashes are retained beside them. These local files were available during review; they are not claimed as independently immutable evidence or repository-published artifacts.

## Evaluation of the three review subjects

| Subject | Technical evaluation | Disposition |
| :--- | :--- | :--- |
| C4b: static file-based seal | Combined access to the seal Secret and encrypted storage defeats the intended at-rest boundary. Recovery shares stored in the same namespace add no separate custody boundary. Pod restrictions, limited permissions and audit declarations do not remove that consequence. Namespace read permissions alone do not automatically grant PVC-content access. | Existing accepted limitation reaffirmed for the recorded profile; external seal/custody remains its reopen path. No independent countersignature is claimed. |
| C5b: namespace-wide ingress | Unrelated application/cert-manager Pods can reach port 8200 under the base policy; additional policies and the NodePort broaden the source inventory. Token authentication, ACLs and TLS remain necessary. Status success does not demonstrate cross-prefix denial or least-privilege token scope. | Existing limitation and owned ingress divergences reaffirmed; narrow selectors only through authorized operations. No new acceptance or independent countersignature is claimed. |
| C7: platform-wide evaluation | The platform has observable process protections, but a single failure domain, colocated recovery material/backups, advisory applicability gaps and unverified audit behavior remain. The documentation guards also have credential-output and review-state defects. | Review completed with changes requested, rather than a favorable security clearance. Passing documentation tests does not qualify Production. |

## Current advisory applicability

The deployed **2.6.2** version falls within the affected ranges below. The repository-approved **2.6.4** input is not evidence that a fix is deployed. The two October 26 advisories also affect that candidate. Version inclusion is established; feature enablement and exploit prerequisites were not established, and no exploit was attempted.

| Advisory | Upstream fix relevant to the 2.6 line | Applicability evidence still required |
| :--- | :--- | :--- |
| [Kubernetes-auth token renewal](https://github.com/openbao/openbao/security/advisories/GHSA-gf3j-hm38-jhh5) | 2.6.4; also 2.7.1 | Whether Kubernetes authentication is enabled and issues renewable tokens. The runbook identifies no repository consumer; that does not inventory live auth mounts. |
| [Root-generation/rekey audit responses](https://github.com/openbao/openbao/security/advisories/GHSA-q74w-hv5x-6hxf) | 2.6.4; also 2.7.1 | Whether the affected unauthenticated workflows are enabled. Upstream disables them by default on this version line; loaded settings remain unverified. |
| [Transit key-import path restrictions](https://github.com/openbao/openbao/security/advisories/GHSA-5pmx-2p74-x478) | 2.6.5; also 2.7.2 | Transit mount/import use and the applicable policy restrictions. |
| [OIDC public-client credentials grant](https://github.com/openbao/openbao/security/advisories/GHSA-q824-44ff-f5j5) | 2.6.5; also 2.7.2 | OIDC provider/client configuration and the affected grant combination. |
| [Malformed plugin parameters in audit logs](https://github.com/openbao/openbao/security/advisories/GHSA-8xxq-mq9m-xmhw) | Built-in fixes in 2.6.3; SDK/plugin fixes need their own inventory | Whether affected parameter types are exposed and plugin builds include the fix. |

The October 26 upstream advisories describe the 2.6 series as unsupported and direct migration to the supported 2.7 line. Platform Operations owns selection, digest pinning, compatibility checks and a separately authorized upgrade. This report does not change approved dependency inputs or deploy a candidate.

## Findings by lens

The machine-readable companion retains every lens finding. Overlap is intentional: EDGE-02 and VER-02 independently identify the same partial-completion problem. Already accepted limitations and owned gaps are labeled separately from newly discovered defects; they are not counted as newly undocumented issues.

### Adversarial

**ADV-01** — Live OpenBao 2.6.2 and approved 2.6.4 have no current advisory applicability disposition.

Location: `docs/operations/openbao.md:18-41,53-55; deploy/openbao/values.yaml:25-28`. Confirmed new advisory-specific finding; version drift was already owned.

Action: Inventory the affected authentication, Transit and OIDC features without credentials; document applicability and prepare a separately authorized supported, patched release.

Consequence: The deployed version is affected and the approved candidate still omits later fixes; exploit prerequisites were not established.

Sources: [reference 1](https://github.com/openbao/openbao/security/advisories/GHSA-gf3j-hm38-jhh5), [reference 2](https://github.com/openbao/openbao/security/advisories/GHSA-q74w-hv5x-6hxf), [reference 3](https://github.com/openbao/openbao/security/advisories/GHSA-5pmx-2p74-x478), [reference 4](https://github.com/openbao/openbao/security/advisories/GHSA-q824-44ff-f5j5), [reference 5](https://github.com/openbao/openbao/security/advisories/GHSA-8xxq-mq9m-xmhw).

**ADV-02** — Compensating controls claim every access/request is audited without runtime audit-write evidence.

Location: `docs/operations/openbao.md:62,411-412; deploy/openbao/smoke-test.yaml:53-56`. Confirmed new documentation/control-evidence finding; declared-versus-loaded caveat exists elsewhere.

Action: Qualify the claim to audited operations; disclose excluded health/seal-status paths and capture a correlated non-sensitive audited operation.

Consequence: The status smoke can pass without exercising the audit device, so the control is overstated.

Sources: [reference 1](https://openbao.org/docs/audit/).

**ADV-03** — The declared file audit destination has bounded storage with no documented rotation or pressure handling.

Location: `deploy/openbao/values.yaml:172-180,220-228; docs/operations/openbao.md:541-560`. Confirmed documentation/evidence gap; live rotation absence and disk-full behavior were not tested.

Action: Record an operations owner for rotation with SIGHUP, capacity/failure monitoring and audit availability verification; check the live arrangement before claiming rotation is absent.

Consequence: An unavailable sole audit destination can stop audited operations while unaudited status checks still succeed.

Sources: [reference 1](https://openbao.org/docs/audit/file/), [reference 2](https://openbao.org/docs/audit/).

**ADV-04** — The expiry divergence says remeasurement requires reading a Kubernetes Secret.

Location: `docs/operations/openbao.md:375,543-547`. Confirmed correction; parent public-certificate inventory remeasured expiry without Secret access.

Action: Inspect the public certificate at the intended endpoint and record identity and expiry, keeping private keys and Secret payloads unread.

Consequence: A safe expiry check is unnecessarily deferred and the historical rotation deadline stays unverified.

**ADV-05** — Static seal material and all recovery shares share the Kubernetes platform trust boundary.

Location: `deploy/openbao/values.yaml:145-158,215-218; docs/operations/openbao.md:411,416-429,550-553`. Existing accepted limitation reaffirmed; namespace read permissions alone do not imply PVC-content access.

Action: Retain the disclosed limitation, plan an external seal trust source and separate recovery custody; describe compromise as combined Secret and storage access.

Consequence: Combined key-custody and storage compromise defeats at-rest protection; colocated shares supply no independent custody.

Sources: [reference 1](https://openbao.org/docs/configuration/seal/static/).

**ADV-06** — The application and cert-manager namespaces admit every pod to port 8200.

Location: `deploy/openbao/values.yaml:70-110; docs/operations/openbao.md:378,412,431-449`. Existing accepted limitation reaffirmed; not a newly undocumented defect.

Action: Under a separately authorized operations change, narrow namespace-and-pod selectors to actual consumers and reconcile the record.

Consequence: Unrelated compromised workloads can reach the listener, increasing reliance on authentication and ACLs.

Sources: [reference 1](https://kubernetes.io/docs/concepts/services-networking/network-policies/).

**ADV-07** — Additional ingress policies and a NodePort broaden access beyond the four internal Services.

Location: `docs/operations/openbao.md:64,363-365,387,392`. Existing owned divergence, reconfirmed by live read-only probes; reachability from each source not exercised.

Action: Inventory Traefik routes, middleware and NodePort reachability, then justify or reconcile each source under its operations owner.

Consequence: Reviewing only the base policy or ClusterIP Services understates effective listener exposure.

Sources: [reference 1](https://kubernetes.io/docs/concepts/services-networking/network-policies/).

**ADV-08** — Two bindings grant system:auth-delegator with no identified Kubernetes-auth consumer.

Location: `deploy/openbao/values.yaml:52-55; docs/operations/openbao.md:68,380`. Existing owned divergence; does not imply permission to read Secret values.

Action: Identify its consumer or revoke both chart and out-of-band bindings under operations authorization; preserve required discovery access.

Consequence: A compromised server identity retains unnecessary review privileges; changing the Helm value alone leaves the duplicate grant.

**ADV-09** — Servers, persistent data/audit volumes and snapshots share one node.

Location: `docs/operations/openbao.md:63,78,175-183,381,555-560`. Existing recovery gap; successful snapshot Jobs supply no off-cluster recovery proof.

Action: Create an off-node/off-cluster snapshot copy and rehearse recovery before claiming node-loss recoverability.

Consequence: Loss of the storage failure domain can remove state and its only documented backup together.

**ADV-10** — OnDelete and ConfigMap/template observations do not establish each server's loaded configuration.

Location: `deploy/openbao/values.yaml:29,135; docs/operations/openbao.md:59-75,386,391`. Existing owned divergence; parent measured admitted security/probe/mount fields but not loaded HCL.

Action: Keep declared-versus-effective caveats and reconcile admitted Pod fields and loaded security settings independently.

Consequence: A configuration correction can leave earlier settings running.

**ADV-11** — Ready replicas and HA status do not prove voting membership or exercised failover.

Location: `docs/operations/openbao.md:56,81-86,169-172,390`. Existing owned evidence gap; current runbook already qualifies it.

Action: Retain intended-voter qualifications until redacted membership evidence and a separately scoped failover exercise exist.

Consequence: Healthy replicas can be mistaken for demonstrated quorum and process failover.

### Edge cases

**EDGE-01** — Detected credential-shaped strings are interpolated into assertion failures.

Location: `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs:635-650`. Confirmed new guard defect; both string.Join messages and collection assertion rendering require removal.

Action: Assert that detection counts are zero using path and category only; verify failure output with synthetic canaries.

Consequence: A real credential can be copied from the rejected document into persistent CI logs.

**EDGE-02** — A reviewer completes one checkpoint while the other security gates remain open.

Location: `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs:1027-1085`. Confirmed new guard defect; overlaps VER-02 and is retained separately.

Action: Validate each checkpoint state, evaluator and date independently; require all gates only for parent completion.

Consequence: Valid partial review fails CI or encourages premature closure of other gates.

### Verification gaps

**VER-01** — Appointing a reviewer before signature makes the unsigned-record guard fail.

Location: `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs:1053-1084`. Confirmed new guard defect.

Action: Permit assigned-but-pending records without completion dates and add an in-memory control for that state.

Consequence: Recording a genuine appointment fails CI unless the record stays falsely unassigned or claims early closure.

Verification evidence: Unsigned branch requires unassigned in evidence at1056 and every checkpoint owner at1080; controls at940-960 exercise wholly unassigned or wholly signed states only.

**VER-02** — Partial signatures or different checkpoint evaluators/dates cannot pass the global signed-state branch.

Location: `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs:1027-1074`. Confirmed new guard defect; overlaps EDGE-02 and is retained separately.

Action: Validate per-gate dispositions and signatures; test partial completion and mixed reviewers while the parent stays in-progress.

Consequence: CI pressures records to overstate all gates as complete under one signer and date.

Verification evidence: One signed flag at1027 selects all three states at1063-1084; signed path requires one evaluator/date. Story lines267-273 explicitly require separate gates to preserve partial completion.

## Validation and limits

Before this review, the same source tree passed a disposable source-reference Debug build with **zero warnings/errors**, all **18 documentation cases**, and the pinned deployment regression (**one case**), without skips. The discovered method counts were unchanged: 2,459 assembly / 106 Deployment / 18 documentation / 14 regression. Exact commands and logs are retained under `artifacts/story-31-1/continuation-2026-11-16/`. These executions verify the current guard behavior; they do not invalidate the newly identified unhandled states or unsafe failure messages. No code or deployment change was made during this review, so no additional build was required for the report files.

All three lenses completed: adversarial 11 findings, edge-case 2, verification-gap 2. The parent checked the implicated assertion and reviewer-state branches, compared accepted limitations with current projections, and verified the cited upstream advisories and audit semantics. [OpenBao audit documentation](https://openbao.org/docs/audit/) confirms health/seal-status exclusions and audit-failure blocking; [file-device documentation](https://openbao.org/docs/audit/file/) places rotation outside the device and requires reopening after rotation. The [Kubernetes NetworkPolicy documentation](https://kubernetes.io/docs/concepts/services-networking/network-policies/) confirms additive policy behavior.

No secret reads, credential dumping, exploit attempts, live resource changes, fault injection, restoration, token revocation, new smoke Job, commit, push or Production activation occurred. The certificate tunnel was the only transient local process and was cleaned up. Advisory feature inventory, loaded HCL, audit writes/rotation, live authentication/ACL scope, routes, off-cluster restore and voting/failover evidence remain outside the executed checks. Repository report files were checked for secret-shaped material without echoing candidate matches, and for line endings/whitespace.

## Follow-up ownership

The Memories maintainer owns the credential-safe assertion diagnostics and independent per-gate review-state validation. Platform Operations (`jpiquot`) owns advisory applicability, supported-version reconciliation, audit proof/rotation, public-certificate runbook correction and the existing ingress, privilege, custody and recovery gaps. This report records work to address; it does not mark any fix as implemented or any risk as newly accepted.

Companion: [canonical findings JSON](31-1-ai-security-review-2026-11-16.json).
