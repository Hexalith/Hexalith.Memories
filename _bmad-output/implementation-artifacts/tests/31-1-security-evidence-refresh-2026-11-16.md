# Story 31.1 — fresh technical evidence

**Prepared:** 2026-11-16T21:31:49Z. **Evaluator:** Codex, AI assistant, following the user's request “do evidence”.
**Source revision:** `14d7ea4fc0b149e4545b37e7ea75ce20d9e5ddd8`. Technical outcome: **changes requested**.
**Probe window:** 2026-11-16T21:25:55.967983+00:00–2026-11-16T21:29:18.976886+00:00.
**Target:** context `jpiquot@local`, namespace `openbao`, release `hexalith-keys`.

This refresh records newly executed checks after the EVAL-03/EVAL-04 guard remediation. It is
attributed technical evidence, with no independent human signature or new owner approval. The prior
attested reports, their covered prefixes and all current checkpoint rows retain their bytes.
C4b/C5b/C7 remain pending / not complete, without completion dates; Story 31.1 stays in-progress.
Administrator (`jpiquot`) remains accountable for arranging the independent evaluation. The expired
2026-10-26 waiver and Story 31.2 activation rules retain their existing states.

## Checkpoint evidence and disposition

| Checkpoint | Fresh observation | Disposition and remaining evidence |
| :--- | :--- | :--- |
| C4b — Static file-based seal | CA-verifying status reports static sealing, Shamir recovery, threshold 2 of 3, initialized/unsealed and Raft storage. Admitted Pods mount the seal and data in the existing Kubernetes trust boundary | Existing limitation remains. Separate key/recovery custody is unproven; independent countersignature remains pending |
| C5b — Namespace-wide port 8200 ingress | Policies still admit all application-namespace and cert-manager Pods plus the additional recorded sources. A direct credential-free check from redis-stack-0 reaches the Service: health 200; mounts and token inspection 403 | Confirms transport reachability from one data-plane Pod and refusal of two unauthenticated operations. Does not prove consumer-only isolation, authenticated ACLs, cross-prefix denial or negative CNI enforcement; independent countersignature remains pending |
| C7 — Whole platform | All 106 documentation cases and the pinned deployment regression pass. Pod-1/Pod-2 DNS certificate checks still fail; same targets pass with Service-name verification. Version/advisory, active audit, loaded-configuration and recovery gaps remain | Technical outcome remains changes requested. Local guard remediation is verified; platform corrections and an independent signed evaluation remain outstanding |

## Current measured profile

Fifteen metadata queries succeeded. The companion retains the exact argument arrays, timestamps and
selected non-secret observations for every query, together with input SHA-256 hashes.

- Three Running/Ready OpenBao servers share the single node `node1`; no Raft membership, failover
  or node-loss test was performed. All three imageIDs match
  `sha256:11fd73a2102cda9c55d5d881a8c3210303146a7ec1e8ac76f526e175c6d24641`; verified status reports **2.6.2**.
- Each server explicitly automounts its ServiceAccount token despite the ServiceAccount default
  being false. The two `system:auth-delegator` bindings and discovery permissions remain inventoried;
  these declarations establish no exhaustive effective-permissions or auth-consumer proof.
- The base ingress and two extra policies remain configured. Four `hexalith-keys*` Services are
  ClusterIP; `deployment-seal-transit` remains NodePort 30820. No external-reachability result is claimed.
- Six 10Gi data/audit claims and the 2Gi snapshot claim are Bound on `openebs-hostpath-retain`.
  The snapshot CronJob remains enabled at `30 0 * * *`, last recorded success
  `2026-11-16T00:30:05Z`. This supplies no off-cluster custody or restore proof.
- File metadata checks on all three declared audit files succeeded: mode 0600, uid 100/gid 1000,
  and nonzero sizes. No audit contents were read. File existence, sizes and mtimes establish neither
  the active audit-device settings nor correlated writes, rotation/capacity handling or failure blocking.

The repository candidate remains OpenBao 2.6.4 with chart 0.29.6. This refresh neither deploys nor
qualifies that candidate. Declared mounts/configuration and public status do not identify the exact
loaded HCL bytes or independently authenticate image/chart provenance.

## TLS and source-pod checks

Every `bao status` probe uses a cleared environment, an absent HOME token/config location, explicit
CA file and `-tls-skip-verify=false`. The CA is consumed by the installed CLI; no certificate/key
Secret contents are rendered or copied. No implicit operator credential is used.

| Target | Pod-DNS verification | Explicit Service-DNS control against the same target |
| :--- | :--- | :--- |
| hexalith-keys-0 | exit 0 | exit 0 |
| hexalith-keys-1 | exit 1, x509 hostname mismatch | exit 0 |
| hexalith-keys-2 | exit 1, x509 hostname mismatch | exit 0 |

These controls reproduce EVAL-01. They isolate a peer-name mismatch from general connectivity or CA
failure, without modifying certificates or claiming an actual join/failover failure.

Existing Python in `redis-stack-0` provided a new direct source-Pod check. The default trust-store
attempt failed with `SSLCertVerificationError`; that records only failure under that trust setup.
The following three requests explicitly disable certificate verification to measure transport/HTTP
and credential-free denial. They use a clean environment, no token/Authorization headers and no
response-body reads, at 2026-11-16T21:28:13Z:

| Credential-free GET | HTTP result | Evidence boundary |
| :--- | :--- | :--- |
| /v1/sys/health?standbyok=true | 200 | This source Pod can reach the Service |
| /v1/sys/mounts | 403 | This unauthenticated administrative read is refused |
| /v1/auth/token/lookup-self | 403 | This unauthenticated token inspection is refused |

This path uses the actual source-Pod network, with no API-server tunnel. It proves no CA identity,
negative NetworkPolicy result, authenticated ACL/cross-prefix isolation or audit correlation.
Six initial BusyBox-wget requests from MCP/application Pods produced no HTTP result; the failure
classification records SSL-client/certificate diagnostics. Those failures supply no network-denial
evidence. No tools were installed and no workload file was deliberately written.

## Fresh local verification

`run-with-temp bash artifacts/story-31-1/guard-remediation-2026-11-16/verify.sh evidence-refresh-2125`

The existing script builds Server.Tests in Debug with source references and NuGet audit enabled,
places build outputs and copied discovery inputs below wrapper-provided TMPDIR, then invokes the
built xUnit assembly for the complete documentation class and pinned deployment regression.
All child work completed before the wrapper removed its disposable directory.

| Check | Result |
| :--- | :--- |
| Debug/source build | 0 warnings, 0 errors; elapsed 1m07.52s |
| OpenBaoPlatformDocumentationTests | 106 cases; 0 errors/failures/skips/not run |
| ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal | 1 case; 0 errors/failures/skips/not run |
| Prior source/evidence/attested-prefix hashes | All match the existing technical sign-off record |

This fresh run verifies the completed diagnostic and review-state remediation; it supplies no
repository-wide regression result or platform security clearance. Exact build/test commands and log
hashes are retained in the companion, with logs in the bundle.

## Current upstream advisory evidence

The primary advisories were reopened on 2026-11-16. Measured 2.6.2 is in their published affected
version ranges. Feature enablement, necessary privileges and actual exploitability on this deployment
remain unverified; no exploit, key import, token issuance/renewal or rekey/root operation was attempted.

| Primary advisory | Published correction | Remaining applicability evidence |
| :--- | :--- | :--- |
| [Kubernetes-auth renewal](https://github.com/openbao/openbao/security/advisories/GHSA-gf3j-hm38-jhh5) | 2.6.4 / 2.7.1 | Active Kubernetes auth and renewable-token usage |
| [Root/rekey response auditing](https://github.com/openbao/openbao/security/advisories/GHSA-q74w-hv5x-6hxf) | 2.6.4 / 2.7.1 | Loaded endpoint settings; the affected endpoints are default-disabled in this version range |
| [Transit import restrictions](https://github.com/openbao/openbao/security/advisories/GHSA-5pmx-2p74-x478) | 2.6.5 / 2.7.2 | Transit import and affected policy configuration |
| [OIDC public-client grant](https://github.com/openbao/openbao/security/advisories/GHSA-q824-44ff-f5j5) | 2.6.5 / 2.7.2 | Active OIDC provider/client grant settings |
| [Plugin parameter auditing](https://github.com/openbao/openbao/security/advisories/GHSA-8xxq-mq9m-xmhw) | 2.6.3; 2.7.0 built-ins / 2.7.1 SDK | Exposed parameter types and independently built plugin versions |

The Transit and OIDC advisories declare the 2.6 line unsupported. The repository's 2.6.4 candidate
does not contain those October corrections. These patch versions are advisory minimums, with no
claim that any candidate is currently latest, secure for this configuration or Production-qualified.

## Evidence files and remaining obligations

[Structured evidence](31-1-security-evidence-refresh-2026-11-16.json) preserves the observations and
command arguments. The local bundle and manifest are under
`artifacts/story-31-1/evidence-refresh-2026-11-16-2125/`; the ZIP includes this report, its companion,
probe scripts, projected captures, verification logs and source input snapshots. Its manifest binds
the file bytes by SHA-256; it is an integrity inventory, with no independent signature attached.

Platform Operations (`jpiquot`) owns the remaining TLS, version/profile, least-privilege, custody,
audit and recovery evidence. Reopen the affected technical finding after its exact resulting profile
and failure/denial controls can be rerun. Authenticated ACL/cross-prefix and active audit checks need
an approved credential-safe execution path; this request does not override the prohibition on reading
Secret contents. The independent evaluator remains unassigned; Administrator owns scheduling.

No live resource/configuration, certificate, dependency, credential, tenant route, migration or
Production qualification changed. No message, commit, push or deployment was performed. The fresh
technical evidence is complete for the checks listed here; independent countersignatures and the
unexecuted platform checks remain separate outstanding obligations.
