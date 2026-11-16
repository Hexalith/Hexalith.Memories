# Story 31.1 — assistant technical security evaluation

Date: 2026-11-16. Evaluator: **Codex assistant**, acting directly on the owner's request to perform
the evaluations. Final verified source revision: `262232e498511b51b5d01e769725ecc299519238`.

Collection began at `5c1c22583fb4037997b266942a4fd4e05a245ebf`; the shared checkout advanced
while this evaluation ran. All twelve manifest, runbook, evidence, checkpoint and guard inputs
listed in the companion were verified unchanged between the initial and final revisions, including
their current working-tree bytes after Git line-ending normalization. Other CI/test-project changes
are outside this evaluation; the focused executions certify the unchanged OpenBao guard behavior,
rather than a repository-wide regression result. Neither revision was created or applied by this evaluation.

## Conclusions

The technical evaluation of all three subjects is complete. **Changes requested** is the overall
result. The existing two platform limitations are confirmed, and fresh checks found a peer-DNS TLS
identity defect that earlier status checks did not exercise. Existing documentation tests pass while
separate live and synthetic checks expose gaps outside their coverage.

| Subject | Technical conclusion | Required action |
| :--- | :--- | :--- |
| C4b — Static file-based seal | The live seal is static; the seal Secret is mounted beside the persistent-data trust boundary. The 2-of-3 Shamir recovery threshold supplies no demonstrated external custody boundary. Retain the existing limitation; this evaluation grants no new risk acceptance | Platform Operations must establish the intended key/recovery custody and the external-seal reopen path before asserting a separate at-rest trust boundary |
| C5b — Namespace-wide port 8200 ingress | The broad ingress policy and additional sources remain configured. Consumer-only least privilege is not established. Two protected API paths refuse unauthenticated requests through the Service tunnel, but source-pod reachability and authenticated ACL isolation are unproven | Platform Operations must justify or narrow the complete additive policy/NodePort surface and produce allowed/denied source and ACL evidence |
| C7 — Whole platform | Changes requested: peer TLS identities fail, deployed/approved versions need advisory disposition, diagnostic and review-state guards have defects, and audit/recovery evidence is incomplete | Address the prioritized findings below and rerun the affected checks against the resulting exact profile |

This is an attributed assistant evaluation. It supplies **no independent human countersignature**,
waiver renewal, deployment authorization or Production qualification. C4b/C5b/C7 retain their current
mandatory-signature states, with no completion dates; Story 31.1 remains in-progress. No additional
reviewer or subagent performed this evaluation. The earlier coordination email remains a request to
the owner to arrange the separate independent review; no further message was sent for this evaluation.

## Evidence and executed checks

Commands, UTC timestamps, projected observations, probe scripts and logs are retained under
`artifacts/story-31-1/assistant-evaluation-2026-11-16/`. The companion JSON indexes the findings
and the evidence manifest. Source inputs were inspected without changing their implementations.

Companion: [structured evaluation](31-1-assistant-security-evaluation-2026-11-16.json).
The local `evaluation-evidence.zip` and `evaluation-evidence-manifest.json` preserve the observations,
both probe attempts, input hashes and this report. The archive is an integrity snapshot, with no
independent signature or approval attached.

### Live platform and custody

Fifteen Kubernetes metadata queries succeeded. They cover nodes, admitted server Pods, application
Pods, namespace restrictions, NetworkPolicies, Services, ServiceAccount defaults, namespaced and
cluster RBAC, persistent claims, snapshot scheduling and ingress objects. Only selected non-secret
fields were retained. No Secret object, credential file, operator token, private key, audit-log content
or encrypted state payload was read.

- One Kubernetes node, `node1`, hosts all three Running/Ready `hexalith-keys-0/1/2` servers.
- All three run OpenBao **2.6.2** at image digest
  `sha256:11fd73a2102cda9c55d5d881a8c3210303146a7ec1e8ac76f526e175c6d24641`.
- Public status queries succeed for all three using their existing local connection settings.
  A further CA-verifying Pod-0 DNS query at **19:45:18 UTC** reports `type: static`,
  `recovery_seal_type: shamir`, `t: 2`, `n: 3`, initialized, unsealed, and Raft storage.
- Admitted Pods run non-root with RuntimeDefault seccomp, no privilege escalation, all capabilities
  dropped and read-only root filesystems. TLS/seal mounts are read-only. Each Pod explicitly
  automounts its Kubernetes ServiceAccount token despite the ServiceAccount's false default.
- Two ClusterRoleBindings still grant `system:auth-delegator` to `openbao/hexalith-keys`.
  Discovery permissions and these grants were inventoried; this establishes neither an exhaustive
  effective-permissions proof nor the presence of an actual Kubernetes-auth consumer.
- Six 10Gi data/audit claims and the 2Gi snapshot claim are Bound on `openebs-hostpath-retain`.
  The snapshot CronJob remains enabled at `30 0 * * *`; last recorded success is
  `2026-11-16T00:30:05Z`. No restore, node-loss or off-cluster custody test was performed.

The declared static-seal file path and the existing ownership/limitation narrative remain inputs
from the repository. The status check confirms the active seal type, but does not prove which exact
HCL file or key reference the running process loaded. Static sealing should be evaluated against
an actual separate source of trust, as described by the [OpenBao static-seal documentation](https://openbao.org/docs/configuration/seal/static/).

### Network and unauthenticated denial

The base `hexalith-keys` policy still admits every Pod in namespaces `hexalith-memories` and
`cert-manager` on 8200. The two additional policies still allow the recorded Traefik/IP and isolated
custody sources. Four `hexalith-keys*` Services are ClusterIP; `deployment-seal-transit` remains
NodePort `8200:30820/TCP`. No standard Ingress or matching Traefik `IngressRoute` was found in the
queries performed; that is not proof of the absence of every possible route or external exposure.

Policies combine as a union; a tighter additional policy does not remove the existing broad grant.
Source egress and destination ingress both matter, and enforcement depends on the networking
implementation. These distinctions follow the [Kubernetes NetworkPolicy documentation](https://kubernetes.io/docs/concepts/services-networking/network-policies/).

Four direct `curl` probes from an MCP application container and `redis-stack-0` failed before an HTTP
result. Explicit capability checks confirmed that `curl` is absent. Those failures prove no denial
by NetworkPolicy. No tools were installed and no source Pod was modified to obtain a result.

An ephemeral loopback-only API-server port-forward to `service/hexalith-keys` then returned:

| Request without Authorization header | HTTP result at 19:41:46 UTC | Evidence boundary |
| :--- | :--- | :--- |
| `/v1/sys/health?standbyok=true` | 200 | Service health endpoint reachable through the tunnel |
| `/v1/sys/mounts` | 403 | This unauthenticated administrative read is refused |
| `/v1/auth/token/lookup-self` | 403 | This unauthenticated token inspection is refused |

Response bodies were discarded. The tunnel bypasses NetworkPolicy, and its certificate trust check
was intentionally disabled for the public-certificate/unauthenticated-request probe. These results
establish neither CA trust nor source-Pod connectivity, authenticated least privilege, cross-prefix
denial or functioning audit writes. The port-forward was terminated and awaited.

### TLS identity — newly reproduced failure

The tunnel negotiated TLS 1.3 and presented a certificate whose Service DNS identity matches, expiring
**2027-08-20 13:38:39 UTC**. Its public SAN inventory contains the Service identities, localhost,
127.0.0.1 and Pod-0 DNS names. This is a public certificate inventory, with no private-key access.

At **19:41:44–19:42:57 UTC**, direct per-Pod queries used each Pod's internal DNS name, its mounted
CA and `-tls-skip-verify=false`:

| Target identity | Result | Control |
| :--- | :--- | :--- |
| `hexalith-keys-0.hexalith-keys-internal.openbao.svc.cluster.local` | exit 0; status returned | Pod-0 DNS identity verifies |
| `hexalith-keys-1.hexalith-keys-internal.openbao.svc.cluster.local` | exit 1; x509 hostname mismatch | Same address succeeds with explicit Service DNS TLS identity and verification still enabled |
| `hexalith-keys-2.hexalith-keys-internal.openbao.svc.cluster.local` | exit 1; x509 hostname mismatch | Same address succeeds with explicit Service DNS TLS identity and verification still enabled |

The errors explicitly say that the certificate is not valid for the Pod-1 and Pod-2 names. The
successful controls isolate the hostname mismatch from general connectivity or CA failure. Only
the probe's expected TLS name changed; the deployment and certificates were not modified.

The earlier measured ConfigMap declares peer-specific TLS names for retry joins, so this mismatch
needs reconciliation. This evaluation does not claim that an existing Raft member has stopped
working or that a rejoin/failover has been attempted; those paths were not exercised.

## Prioritized findings

Severity here is the assistant's triage of the observed impact and evidence gaps, rather than a
new organization-approved risk decision. Existing accepted limitations remain identified separately.

| ID | Priority | Finding / evidence | Owner and required next step |
| :--- | :--- | :--- | :--- |
| EVAL-01 | High | New: Pod-1 and Pod-2 DNS identities fail trusted-CA certificate verification; both Service-identity controls succeed | Platform Operations: align issued certificate SANs and the TLS identities actually used by clients/retry joins, then verify each intended peer name without disabling verification |
| EVAL-02 | High | Existing concern reconfirmed: deployed 2.6.2 is inside the cited advisory ranges; repository candidate 2.6.4 omits the later October fixes. Actual affected-feature enablement remains unverified | Platform Operations: inventory affected features through an authorized metadata-only path; select and qualify a supported patched profile before deployment |
| EVAL-03 | High | Existing guard defect: detected long credential-shaped values are inserted into assertion diagnostics; a disposable synthetic control exercises the actual guard | Memories maintainer: report path/category/count without detected values or collection rendering, and verify safe failure output |
| EVAL-04 | Medium | Existing guard defect: unsigned records require an unassigned reviewer; one global signature branch also rejects an isolated completed C4b while C5b/C7 remain pending | Memories maintainer: validate appointment and each checkpoint independently, preserving pending dates and requiring all mandatory gates only for parent completion |
| EVAL-05 | Medium | Existing evidence gap: persistent audit declaration and unauthenticated status success prove neither correlated audit writes nor rotation/capacity handling | Platform Operations: capture a non-sensitive correlated audited operation, active-device evidence and rotation/failure handling without dumping audit contents |
| EVAL-06 | Existing limitation | Static seal and storage share the Kubernetes trust boundary; separate recovery custody is not demonstrated | Platform Operations: retain the two-row limitation record and its owner/consequences; establish the external-seal/custody reopen evidence |
| EVAL-07 | Existing limitation | Namespace-wide ingress plus additional policy/NodePort sources exceeds demonstrated consumer-only access | Platform Operations: inventory and justify/narrow the full surface; prove authorized connectivity and unauthorized/cross-prefix denial |
| EVAL-08 | Existing owned gap | One-node failure domain, colocated snapshot storage, duplicated auth-delegator grants and incomplete loaded-configuration evidence persist | Platform Operations: disposition each existing gap and provide restoration, permissions and loaded-profile evidence before stronger security/availability claims |

The bounded audit destination is a control dependency. The [audit-device documentation](https://openbao.org/docs/audit/)
excludes health/seal-status paths from auditing and explains failure blocking. The [file-device documentation](https://openbao.org/docs/audit/file/)
places rotation outside the file device and describes reopening on SIGHUP. These current documentation
pages describe the 2.7 line; this evaluation does not substitute them for testing the deployed 2.6.2
audit path, its active configuration or its rotation arrangement.

The guard locations are
`tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs`:
diagnostic construction at lines 633–650, and the global review-state branch at lines 1027–1084.
The prior AI report records these as EDGE-01, EDGE-02, VER-01 and VER-02; the two partial-state
entries describe overlapping aspects of the same validator design. This evaluation adds executed
synthetic reproductions rather than counting that overlap as separate new platform vulnerabilities.

## Advisory applicability checked against primary sources

The following upstream advisories were reopened during this evaluation. Version inclusion is
established; feature enablement, required permissions and exploitability on this deployment are not.
No exploit, malformed plugin input, key import, token renewal or root/rekey workflow was attempted.

| Primary advisory | Published / relevant correction | Remaining applicability question |
| :--- | :--- | :--- |
| [GHSA-gf3j-hm38-jhh5](https://github.com/openbao/openbao/security/advisories/GHSA-gf3j-hm38-jhh5) | 2026-10-01; 2.6.4 / 2.7.1 fixes Kubernetes-auth renewal after revocation | Is Kubernetes auth enabled and issuing renewable tokens? |
| [GHSA-q74w-hv5x-6hxf](https://github.com/openbao/openbao/security/advisories/GHSA-q74w-hv5x-6hxf) | 2026-10-01; 2.6.4 / 2.7.1 fixes affected root/rekey response auditing | Are the specific unauthenticated root/rekey endpoints enabled? Their default-disabled state and exact settings need runtime proof |
| [GHSA-5pmx-2p74-x478](https://github.com/openbao/openbao/security/advisories/GHSA-5pmx-2p74-x478) | 2026-10-26; 2.6.5 / 2.7.2 fixes Transit import restrictions | Is the relevant Transit import/policy combination present? |
| [GHSA-q824-44ff-f5j5](https://github.com/openbao/openbao/security/advisories/GHSA-q824-44ff-f5j5) | 2026-10-26; 2.6.5 / 2.7.2 fixes the OIDC public-client credentials-grant combination | Are the affected provider/client grant settings enabled? |
| [GHSA-8xxq-mq9m-xmhw](https://github.com/openbao/openbao/security/advisories/GHSA-8xxq-mq9m-xmhw) | 2026-09-23; built-ins fixed in 2.6.3 / 2.7.0; SDK fixes in 2.6.3 / 2.7.1 | Are affected parameter types exposed, and are separately built plugins patched? |

The October 26 advisories identify the 2.6 series as unsupported upstream and direct migration to
2.7. Consequently, merely deploying the repository's 2.6.4 candidate would not resolve this review.
Those advisory correction versions are minimums for the listed issues, rather than a claim that any
particular version is the latest or fully qualified. No image, dependency or deployment pin changed.

## Guard verification and limits

A fresh disposable Debug build with Hexalith source references passed with **zero warnings/errors**.
The existing **18 documentation cases and one deployment regression passed**, with zero failures,
errors or skips. New probes exercise the compiled real validator by reflection; they add no tracked
test method and change no product implementation. All temporary test-input mutations occur beneath
the wrapper-provided TMPDIR and are restored before the temporary workspace is removed.

The synthetic probes test assigned-but-unsigned records, an isolated C4b signature and whether the
actual secret-shape guard copies detected synthetic material into its failure message. Their exact
results and any harness correction are retained with the probe logs; synthetic values supply no
real credential or signature. Baseline success must not be interpreted as coverage of these states.

| Synthetic control against the actual compiled implementation | Result |
| :--- | :--- |
| Existing wholly unsigned and wholly signed fixture records | both accepted |
| Consistently named reviewer with no signature | rejected by the hard-coded unassigned requirement |
| C4b signed while the overall evaluation and C5b/C7 remain pending | rejected by the global pending-state branch before summary validation |
| Mixed-density synthetic canary in a temporary runbook copy | rejected, and the detected value is present in the exception message; value omitted from retained output |

The final probe job exited 0 because the three uncovered behaviors were reproduced, rather than
because they were fixed. The first harness attempt exited 1: its uppercase/digit-only input produced
a detected hexadecimal suffix, while the harness searched for the whole input. A mixed-density
synthetic input corrected that comparison without changing the implementation. Both attempts are
retained; the successful build/test/probe jobs are under `guard-probe-retry/`. The probe executable
and all temporary copies were removed by `run-with-temp` after the child processes finished.

No Secret payloads, real credential canaries, audit-log contents, private keys or operator tokens
were read. No live resource, configuration, credential, tenant route, runtime migration or Production
qualification changed. Public status and unauthenticated reads may produce normal server telemetry;
this evaluation does not claim the live audit device recorded those requests.

Authenticated ACL/cross-prefix isolation, active auth/engine inventory, audit writes/rotation,
external NodePort reachability, CNI enforcement from the source Pods, actual Raft membership,
restart/failover, restore and off-cluster custody remain unexecuted. Their absence is an explicit
evidence gap, rather than a fabricated pass. Passing tests and this completed technical report do
not discharge the separate independent-signature requirement.

## Assistant signature

**Signed by:** Codex, AI assistant.
**Signed at (UTC):** 2026-11-16T19:56:39Z.
**Capacity:** Author and performer of the C4b, C5b and C7 technical evaluations.
**Decision:** Changes requested.
**Evaluated source revision:** `262232e498511b51b5d01e769725ecc299519238`.

I attest that the covered report records the technical checks I performed, the observed results,
the conclusions I reached and the limits I disclosed. The overall evaluation requests corrections.

**Form:** Typed author attestation, non-cryptographic. This signature attributes the evaluation to
the assistant and supplies no independent human countersignature, risk acceptance or Production
clearance. Independent-signature checkpoint states and completion dates are unchanged.

**Covered content:** The first 18831 bytes of this UTF-8/CRLF Markdown file.
**Covered content SHA-256:** `sha256:cc4f3d3a8b25023fb3914d0456b4c9e23a4986f99144f9d604584f2515ee4cee`.

The signature section is appended after that covered prefix. The structured companion and local
`assistant-attestation.json` record the same attribution, timestamp, scope, byte count and digest.
The original `evaluation-evidence.zip` remains the pre-signature evidence snapshot; the local
`assistant-signed-evaluation.zip` contains that snapshot, this attributed report and the attestation.

## Owner attestation — Jérôme Piquot

**Attested by:** Jérôme Piquot (`jpiquot@itaneo.com`).
**Capacity:** Platform owner.
**Recorded at (UTC):** 2026-11-16T20:00:55Z.
**Provenance:** Explicit user declaration in this conversation, followed by an approval confirmation.
**Form:** Typed nominative owner attestation, non-cryptographic.

Verbatim declaration supplied by the owner:

> Je soussigné Jérôme Piquot, jpiquot@itaneo.com, approuve le rapport d’évaluation du 16 novembre 2026 et ses conclusions « corrections nécessaires », en qualité de propriétaire de la plateforme. Je demande l’enregistrement de mon attestation nominative.

Verbatim follow-up confirmation:

> j'approve

The owner approves this report and its changes-requested conclusions in the stated owner capacity.
This records the provided declaration without asserting an external identity verification or a
cryptographic signature. The findings are not marked fixed, the conclusion remains changes requested,
and the independent-reviewer checkpoints remain pending without completion dates.

**Approved report version:** The first 20201 bytes of this UTF-8/CRLF file, including
the preceding assistant attribution.
**Approved report SHA-256:** `sha256:d14ce5e846307c73c9937be00ec7bbf9a3aa563e3b37588fbb67ad531906e1ee`.

The existing assistant attribution and original evidence archives are preserved. Their stored full-file
hashes refer to the earlier versions, before this owner appendix. The structured companion records
this append-only attribution chain and the current report hash. The local `owner-attestation.json`
retains the exact declaration and provenance; `owner-attested-evaluation.zip` packages the current
report, structured companion, attestation and prior assistant-attributed evidence bundle.
