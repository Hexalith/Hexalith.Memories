# Hexalith Keys OpenBao operations

`hexalith-keys` is the internal OpenBao server deployed to the Kubernetes target reached through context
`jpiquot@local`. Platform Operations owner: **Jérôme Piquot (`jpiquot`)**. Recorded reviewer annotation: **Murat TEA for Jérôme (`murat-tea-for-jpiquot`)**, a persona for the owner,
not an independent authority. Independent reviewer: **unassigned**. On 2026-11-22, Jérôme Piquot (`jpiquot`) closed Story 31.1 by sole-owner risk acceptance for C4b, C5b and C7. No independent countersignatures were obtained. EVAL-01 remains open with Platform Operations.

The current deployment was measured read-only on **2026-11-16T15:45:59Z–15:46:49Z** against context
`jpiquot@local`; additional metadata projections followed at **2026-11-16T15:47:50Z**. The current profile
and smoke outcome are separate from the historical 2026-07-28/2026-09-06 measurements and the approved
2026-10-04 repository inputs. The [evidence transcript](../../_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md)
section 9 records commands and observations. Historical tables retain their original dates.

No secret value appears in this document. The unseal key material, recovery shares, operator
credentials, TLS private key, and Dapr tokens exist only in Kubernetes Secrets and are never copied into
documentation, evidence, or source control.

## Repository adoption on 2026-10-04

The Administrator approved exact PG-ONPREM-2 bytes, canonical SHA-256
`7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`. Current server tooling/CI and
`deploy/openbao/values.yaml` select OpenBao 2.6.4, image
`quay.io/openbao/openbao:2.6.4@sha256:cf2340fc9a22cb9358ca0defd1f39b65673836bd23fe2bb8984a07e11fe13ef4`,
linux/amd64 manifest `sha256:bd3e8b6b67b5c4c3fc1064cd0f86eb8063d9ed92a7af608b6408d6748e6eef64`,
official chart 0.29.6 OCI manifest `sha256:98c8fc901e2579ac6da9a805537fcd7a19525ef8e563ae8737dc16fc8f641e3e`
and content `sha256:8079e985bdf608f965ada59c70051693d14dd2454ac16311229f367d0c48c4b9`.
Values SHA-256 is `4d6e8909695100901be5008b5a4cd11d108ee03b495d26243d23a22ab25a0caa`;
archived render SHA-256 is `16001617572d803bc9b53731a54c8b564e3c6b5a3d9b28b34a727dad9d435303`.

The exact values use preferred hostname anti-affinity (weight 100) to permit the
already declared three-voter single-node topology. Raft/static-seal/TLS HCL,
OnDelete updates, retained PVCs, permissions, node-fault exclusions and security
blockers remain. The [approved package](../../_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/adoption.md)
records the offline source/configuration identity checks.

This adoption includes no deployment, target contact or security requalification.
The measured 2.6.0 platform profile and recorded smoke result below stay historical;
they do not prove the new server runs. The CA-only `deploy/openbao/smoke-test.yaml`
client remains exactly pinned to 2.6.0 and mounts no server key/seal material. The
chart's built-in test cannot replace it. The 2026-11-16 smoke below proves only 2.6.0-client-to-measured-2.6.2-server status access.
Fresh 2.6.0-client-to-2.6.4-server compatibility verification remains pending security requalification; the historical result is not that verification.
Development AppHost defaults are unchanged. Story 27.4 closed `done` on 2026-11-01 with A41 resolved by accepted debt (`DW-17`), while Production access-telemetry lifecycle remains disabled (`DW-730`).

## Current deployment measured on 2026-11-16

| Contract | Measured value on 2026-11-16 |
| :--- | :--- |
| Kubernetes context | `jpiquot@local` |
| Kubernetes nodes | one node, `node1`, roles `control-plane,worker`, `v1.34.9` |
| Namespace / application namespace | `openbao` / `hexalith-memories` |
| Helm release, Service, StatefulSet | `hexalith-keys` |
| Helm release revisions | 10 metadata objects (`sh.helm.release.v1.hexalith-keys.v1` through `v10`); release Secret contents not read |
| Chart label | `helm.sh/chart: openbao-0.29.5` on all three server pods; chart digest not measured and cannot be inferred from this label |
| Server image | `quay.io/openbao/openbao:2.6.2@sha256:11fd73a2102cda9c55d5d881a8c3210303146a7ec1e8ac76f526e175c6d24641`; all three runtime imageIDs match that digest |
| Server version | `2.6.2` on all three servers |
| Raft voters | `3` replicas, `3/3` Ready; `hexalith-keys-0`, `hexalith-keys-1`, `hexalith-keys-2` all Running on `node1`; three healthy servers observed, voting membership unmeasured |
| HA mode | `ha_enabled: true`; active endpoint `hexalith-keys-1`, standby endpoints `hexalith-keys-0` and `hexalith-keys-2` |
| Initialization / recovery | all three `initialized: true`, `sealed: false`, `storage_type: raft`; recovery seal `shamir`, threshold `2` of `3` |
| Storage and retry_join | ConfigMap declared HCL (loaded-runtime reconciliation unproven): integrated Raft at `/openbao/data`; three TLS-verified retry_join peers `hexalith-keys-0/1/2.hexalith-keys-internal.openbao.svc.cluster.local:8200`, each with matching leader_tls_servername and leader_ca_cert_file `/openbao/userconfig/openbao-server-tls/ca.crt`; approved values declare only peer 0 (owned divergence) |
| Per-pod addresses | StatefulSet template only (admitted Pod fields unprobed): `BAO_API_ADDR=https://$(POD_IP):8200`, `BAO_CLUSTER_ADDR=https://$(HOSTNAME).hexalith-keys-internal:8201`, `BAO_RAFT_NODE_ID` from `metadata.name`; no static node_id in HCL |
| Seal | ConfigMap declared HCL (loaded-runtime reconciliation unproven): `seal "static"`, current_key_id `kubernetes-openbao-seal-v1`, current_key `file:///openbao/userconfig/openbao-seal/current.key` |
| Audit / leases / UI | ConfigMap declared HCL (loaded-runtime reconciliation unproven): persistent JSON file `/openbao/audit/openbao-audit.json`, mode `0600`, hmac_accessor `true`; default/max lease TTL `168h`/`8760h`; `ui = false`; additional declared `allow_unauthenticated_workflows = false` |
| Persistent volumes | six 10Gi ReadWriteOnce data/audit PVCs, all Bound, `openebs-hostpath-retain`, each labelled part-of `hexalith-memories`; snapshot PVC `openbao-snapshots` 2Gi Bound on the same StorageClass |
| Services | four `hexalith-keys*` Services are ClusterIP; no standard Ingress objects observed, Traefik custom routes unprobed; internal Service headless with publishNotReadyAddresses true; main Service publishNotReadyAddresses absent (API default false); extra `deployment-seal-transit` NodePort `8200:30820/TCP` targets the active voter |
| TLS listener | ConfigMap declared HCL (loaded-runtime reconciliation unproven): `[::]:8200`, cluster `[::]:8201`, `tls_disable = 0`, `tls_min_version = "tls12"`; cert/key file references under `/openbao/userconfig/openbao-server-tls` |
| Service discovery / RBAC | `service_registration "kubernetes" {}`; Role `hexalith-keys-discovery-role` pods get/watch/list/update/patch, RoleBinding `hexalith-keys-discovery-rolebinding` to ServiceAccount `hexalith-keys` |
| ServiceAccount token | ServiceAccount `hexalith-keys` exists with no referenced legacy token Secret and default `false`; StatefulSet template requests `automountServiceAccountToken: true`; actual server Pod automount unprobed |
| Token review binding | both `hexalith-keys-server-binding` and `hexalith-keys-tokenreview` bind ServiceAccount `hexalith-keys` to `system:auth-delegator` |
| Pod Security / ownership | namespace Restricted enforce/audit/warn at latest; owner `jpiquot`, recorded reviewer annotation `murat-tea-for-jpiquot` (non-independent) |
| Pod / container security | StatefulSet template only (admitted Pod fields unprobed): runAsNonRoot true, uid 100, gid/fsGroup 1000, fsGroupChangePolicy OnRootMismatch, seccomp RuntimeDefault; allowPrivilegeEscalation false, drop ALL; additional declared readOnlyRootFilesystem true |
| Affinity / update / retention | preferred hostname anti-affinity weight 100; OnDelete, OrderedReady, governing Service `hexalith-keys-internal`; PVC whenDeleted/whenScaled Retain |
| Readiness / liveness | StatefulSet template only (admitted Pod fields unprobed): readiness exec `[bao, status]`, no skip-verify flag, thresholds initial 5s / period 5s / timeout 3s / failures 3; liveness HTTPS `/v1/sys/health?standbyok=true&sealedcode=204&uninitcode=204` on 8200, initial 30s / period 10s / timeout 3s / failures 3 |
| Resources / logging | StatefulSet template only (admitted Pod fields unprobed): requests 250m CPU / 256Mi, limits 1 CPU / 1Gi; imagePullPolicy IfNotPresent; BAO_LOG_LEVEL info, BAO_LOG_FORMAT json |
| Labels / config annotation | part-of `hexalith-memories`, platform-owner `jpiquot`; StatefulSet pod-template security-reviewer annotation and `openbao.hashicorp.com/config-checksum` present; admitted Pod annotations unprobed; OnDelete still requires deliberate pod replacement to adopt template changes |
| Secret mounts | StatefulSet template only (admitted Pod fields unprobed): TLS and seal Secret declarations readOnly true, defaultMode 288 (`0440`), under `/openbao/userconfig/openbao-server-tls` and `/openbao/userconfig/openbao-seal`; no Secret contents read |
| Optional resources | no Deployment or DaemonSet in namespace openbao; no UI Service and HCL ui false; consistent with approved injector/csi/ui disabled, not a Helm-values read |
| NetworkPolicy | `hexalith-keys` still admits every pod in `hexalith-memories` and `cert-manager` on 8200, self on 8200/8201; additive policies `deployment-seal-external` and `ext-secrets-1-custody-provider-29a4ace57e04` also admit 8200 (see owned policy rows) |
| Backup | CronJob `openbao-raft-snapshot`, `30 0 * * *`, Forbid, ServiceAccount `openbao-snapshot`, PVC `openbao-snapshots`; two succeeded Jobs, last success `2026-11-16T00:30:05Z`; no off-cluster recovery proven |
| Profile annotations | openbao still carries release-r2 deployment/profile IDs and the historical three manifest hashes/composite; hexalith-memories has no hexalith.io profile annotations in this probe; neither is a current qualification |

These are measured observations, not a deployment of the approved 2.6.4/chart 0.29.6 inputs.
Platform Operations (`jpiquot`) owns the version and rendered-configuration divergence below.
Three healthy servers and HA status were observed on **one node**. Voting membership was not
queried; ready replicas and `bao status` do not prove the three-voter membership or failover quorum.
The intended three-voter profile remains the availability model, with membership proof owned below.
Node loss still removes every observed server and the local backup failure domain. No Production qualification or tenant-routing claim follows from this capture.

## Deployed profile as measured

Historical 2026-07-28 observations, re-confirmed only for the bounded fields in evidence section 8.
Use the dated current profile above for present operations.

| Contract | Measured value on 2026-07-28 |
| :------- | :--------------------------- |
| Kubernetes context | `jpiquot@local` |
| Kubernetes nodes | one node, `node1`, roles `control-plane,worker`, `v1.34.9` |
| Namespace | `openbao` |
| Application namespace | `hexalith-memories` |
| Helm release, Service, StatefulSet | `hexalith-keys` |
| Helm release revisions | 9 (`sh.helm.release.v1.hexalith-keys.v1` through `v9`) |
| Chart | official `oci://ghcr.io/openbao/charts/openbao`, `0.28.5`, digest `sha256:1c2e01185430b9bc426da870909fdccfbb4e3e4758f0c6f8cccfbceead4381ff` — **not measured**; carried from the header comment in `deploy/openbao/values.yaml` (commit `4d2e4e2f`). No read-only probe here yields a chart digest, so it re-derives only from a `helm` client, which is absent. See [Named divergences](#named-divergences) |
| Server image | `quay.io/openbao/openbao:2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653` |
| Server version | `2.6.0` |
| Raft voters | `3` — StatefulSet `.spec.replicas = 3`; pods `hexalith-keys-0`, `hexalith-keys-1`, `hexalith-keys-2`, each `1/1 Running` |
| HA mode | `ha_enabled: true` — `bao status` reports a leader; Services `hexalith-keys-active` and `hexalith-keys-standby` exist |
| Voter scheduling | all three voters are scheduled on the single node `node1` |
| Storage | integrated Raft, `storage_type: raft`, path `/openbao/data`, `retry_join` against `hexalith-keys-0.hexalith-keys-internal.openbao.svc.cluster.local:8200` |
| Raft node id | supplied by container env `BAO_RAFT_NODE_ID` from `metadata.name`; absent from the rendered HCL |
| Seal | `seal "static"`, `current_key_id = "kubernetes-openbao-seal-v1"`, `current_key = file:///openbao/userconfig/openbao-seal/current.key` |
| Recovery seal | `shamir`, threshold `2` of `3` shares |
| Initialization | `initialized: true`, `sealed: false` |
| Persistent volumes | six retained 10Gi PVCs (`data-hexalith-keys-0..2`, `audit-hexalith-keys-0..2`), all `Bound`, StorageClass `openebs-hostpath-retain` |
| Audit | persistent JSON file device at `/openbao/audit/openbao-audit.json`, mode `0600` |
| Endpoint | `https://hexalith-keys.openbao.svc.cluster.local:8200` |
| Services | `hexalith-keys`, `hexalith-keys-internal` (headless), `hexalith-keys-active`, `hexalith-keys-standby` — those four `hexalith-keys*` Services are `ClusterIP` with no ingress on those four |
| TLS | on: `tls_disable = 0`, `tls_min_version = "tls12"`, cert and key from Secret `openbao-server-tls` |
| Service registration | `service_registration "kubernetes" {}`, backed by Role `hexalith-keys-discovery-role` (pods: `get`, `watch`, `list`, `update`, `patch`) |
| ServiceAccount token | ServiceAccount default is `false`, but pod spec sets `automountServiceAccountToken: true`, which wins — the server does receive and use a Kubernetes API token |
| Token review binding | **two** ClusterRoleBindings grant `system:auth-delegator` to the ServiceAccount: the chart-rendered `hexalith-keys-server-binding` and the untracked, non-Helm `hexalith-keys-tokenreview` |
| Pod Security | namespace carries Restricted `enforce`, `audit`, and `warn` labels at `latest` |
| Pod security context | `runAsNonRoot: true`, `runAsUser: 100`, `runAsGroup: 1000`, `fsGroup: 1000`, `seccompProfile: RuntimeDefault`, `allowPrivilegeEscalation: false`, all capabilities dropped |
| NetworkPolicy | port 8200 from every pod in `hexalith-memories` and every pod in `cert-manager`; ports 8200 and 8201 from the OpenBao pods themselves |
| Backup | CronJob `openbao-raft-snapshot` at `30 0 * * *`, ServiceAccount `openbao-snapshot`, 2Gi PVC `openbao-snapshots`, `concurrencyPolicy: Forbid`, two completed Jobs observed |
| Update strategy | `OnDelete`, `podManagementPolicy: OrderedReady`, governing Service `hexalith-keys-internal` |
| PVC retention | `whenDeleted: Retain`, `whenScaled: Retain` |
| Evidence root | `_bmad-output/implementation-artifacts/tests` — preserved from the pre-Story-31.1 profile table because Story 27.3's C1 record cites it as one of its six production-target values |
| Declared single-component fault | `dapr-sidecar-restart` — preserved for the same reason |
| Deployment ID | annotation reads `memories-production-2.11.0-c7c2ca21-hexalith-keys-r2` — the value written for release `r2`, still present at Helm revision 9; see [Named divergences](#named-divergences) |
| Profile ID | annotation reads `redis-state-v1-dapr-1.18.1-openbao-2.6.0-4183b741eac062d9` — likewise release-`r2` identity, not a current-state assertion |

The read-only probes behind this table are safe to repeat and none of them reads a Secret's contents:

```bash
kubectl get nodes -o wide
kubectl -n openbao get pods -o custom-columns=NAME:.metadata.name,NODE:.spec.nodeName
kubectl -n openbao get pod,service,pvc,networkpolicy
kubectl -n openbao get statefulset hexalith-keys -o jsonpath='{.spec.replicas}'
kubectl -n openbao get cm hexalith-keys-config -o jsonpath='{.data}'
kubectl -n openbao get networkpolicy hexalith-keys -o jsonpath='{.spec}' | jq .
kubectl -n openbao get pod hexalith-keys-0 -o jsonpath='{.spec.automountServiceAccountToken}|{.spec.serviceAccountName}|{.status.containerStatuses[0].imageID}'
kubectl -n openbao get sa hexalith-keys -o jsonpath='{.automountServiceAccountToken}'
kubectl -n openbao get role,rolebinding
kubectl get clusterrolebinding -o name | grep hexalith-keys
kubectl -n openbao get sts hexalith-keys \
  -o jsonpath='{.spec.podManagementPolicy}|{.spec.updateStrategy.type}|{.spec.serviceName}'
kubectl -n openbao get sts hexalith-keys -o jsonpath='{.spec.persistentVolumeClaimRetentionPolicy}'
kubectl -n openbao get sts hexalith-keys \
  -o jsonpath='{range .spec.volumeClaimTemplates[*]}{.metadata.name}:{.spec.resources.requests.storage}:{.spec.storageClassName}{"\n"}{end}'
kubectl -n openbao get sts hexalith-keys -o jsonpath='{.spec.template.spec.securityContext}'
kubectl -n openbao get sts hexalith-keys \
  -o jsonpath='{.spec.template.spec.containers[0].securityContext}{"\n"}{.spec.template.spec.containers[0].resources}'
kubectl -n openbao get sts hexalith-keys \
  -o jsonpath='{.spec.template.spec.containers[0].readinessProbe.exec.command}'
kubectl -n openbao get sts hexalith-keys \
  -o jsonpath='{range .spec.template.spec.containers[0].env[*]}{.name}={.value}{.valueFrom.fieldRef.fieldPath}{"\n"}{end}'
kubectl -n openbao get cronjob,job
kubectl -n openbao get secret --no-headers -o custom-columns=NAME:.metadata.name,TYPE:.type
kubectl get ns openbao -o jsonpath='{.metadata.labels}{"\n"}{.metadata.annotations}'
kubectl -n openbao exec hexalith-keys-0 -- env BAO_CACERT=/openbao/userconfig/openbao-server-tls/ca.crt bao status -format=json
```

`kubectl get secret` is used for names and types only. Never render `-o yaml` or a `.data` JSONPath for
`openbao-seal`, `openbao-operator-credentials`, `openbao-server-tls`, or `hexalith-keys-pki`.

## Availability profile

Both halves of this are true and both matter.

**Process-level failover is the intended three-voter model.** The current read-only probes observe
three healthy servers, `ha_enabled: true`, and leader/standby endpoints; they do not establish voting
membership or exercise leader election. With three voting members a single-pod loss would retain
quorum. The current membership and fault proof remain unmeasured, as owned below.

**There is no node-level availability.** The Kubernetes cluster has exactly one node, `node1`, and all
three observed servers are scheduled on it, as are both snapshot Jobs. The node is therefore the entire failure
domain: losing it loses all three observed servers, both PVC sets, and the snapshot volume at once. A voter count
of three does not change that, and this document does not present it as if it did.

Recovery from node loss depends on the `openbao-raft-snapshot` CronJob's output surviving the node,
which today it does not — the `openbao-snapshots` PVC is `openebs-hostpath-retain` on the same node. An
off-cluster copy of those snapshots is still required and is not yet in place. That gap is owned and
tracked as `Raft snapshots have no off-cluster copy` in [Named divergences](#named-divergences); until it
closes, this platform has no recoverable state after node loss.

## Owned manifests

These four files are the checked-in, non-secret inputs to the platform. The values section records
current approved repository configuration; the dated platform observations retain their original evidence.

### `deploy/openbao/values.yaml`

Current approved Helm values target chart `0.29.6` and OpenBao `2.6.4`, adopted 2026-10-04.
Story 31.1 historically reconciled chart 0.28.5/2.6.0 to the measured release after the original
standalone declaration was contradicted. That observation does not establish a successor deployment.

| Declared setting | Approved repository value or retained measured boundary |
| :--------------- | :---------------------- |
| `fullnameOverride: hexalith-keys` | Helm release, StatefulSet, and Service names |
| `server.image.tag` | `2.6.4@sha256:cf2340fc9a22cb9358ca0defd1f39b65673836bd23fe2bb8984a07e11fe13ef4` — digest-pinned |
| `global.tlsDisable: false` | listener runs TLS with `tls_min_version = "tls12"` |
| `server.standalone.enabled: false` | the standalone single-voter path is off |
| `server.ha.enabled: true` | `bao status` reports `ha_enabled: true` |
| `server.ha.replicas: 3` | StatefulSet `.spec.replicas = 3` |
| `server.affinity` | preferred hostname anti-affinity, weight 100; three voters can share one node; no node HA |
| `server.ha.apiAddr: null`, `server.ha.clusterAddr: null` | left unset so the chart emits per-pod container env `BAO_API_ADDR=https://$(POD_IP):8200` and `BAO_CLUSTER_ADDR=https://$(HOSTNAME).hexalith-keys-internal:8201`; pinning either would give all three voters one address |
| `server.ha.raft.enabled: true` | `storage "raft"` with `retry_join`, `storage_type: raft` |
| `server.ha.raft.setNodeId: true` | container env `BAO_RAFT_NODE_ID` from `metadata.name` |
| `server.service.type: ClusterIP` | the four `hexalith-keys*` Services are `ClusterIP`; no ingress exists on those Services |
| `server.service.active.enabled: true` | Service `hexalith-keys-active` |
| `server.service.standby.enabled: true` | Service `hexalith-keys-standby` |
| `server.serviceAccount.serviceDiscovery.enabled: true` | Role `hexalith-keys-discovery-role` and its RoleBinding, required by `service_registration "kubernetes"` |
| `server.authDelegator.enabled: true` | ClusterRoleBinding `hexalith-keys-server-binding` to `system:auth-delegator` |
| `server.dataStorage.size: 10Gi`, `server.auditStorage.size: 10Gi` | one `data` and one `audit` volumeClaimTemplate, materialized once per replica — six 10Gi PVCs at three replicas |
| `server.networkPolicy.ingress` | port 8200 from `hexalith-memories` and `cert-manager`; 8200 and 8201 from the OpenBao pods |
| `server.ha.raft.config` seal stanza | `seal "static"` reading `file:///openbao/userconfig/openbao-seal/current.key` |
| `server.ha.raft.config` audit stanza | `audit "file" "persistent"` at `/openbao/audit/openbao-audit.json` |
| `server.statefulSet.securityContext` | the Restricted-compatible pod and container security contexts on the running pods: `runAsNonRoot: true`, `runAsUser: 100`, `runAsGroup: 1000`, `fsGroup: 1000`, `fsGroupChangePolicy: OnRootMismatch`, `seccompProfile: RuntimeDefault`, `allowPrivilegeEscalation: false`, capabilities `drop: [ALL]` |
| `server.readinessProbe`, `server.livenessProbe` | thresholds as declared; historical readiness exec was `bao status -tls-skip-verify`; current measured exec is `bao status` without that flag (see [TLS posture of the probes](#tls-posture-of-the-probes)) |
| `server.resources` | requests `250m` CPU / `256Mi` memory, limits `1` CPU / `1Gi` memory |
| `server.logLevel`, `server.logFormat` | container env `BAO_LOG_LEVEL=info`, `BAO_LOG_FORMAT=json` |
| `server.extraLabels`, `server.annotations`, `server.configAnnotation` | `app.kubernetes.io/part-of: hexalith-memories` and `hexalith.io/platform-owner: jpiquot` labels, the `hexalith.io/security-reviewer` annotation, and the config-checksum pod-template annotation; OnDelete requires deliberate pod replacement to adopt changes |
| `server.volumes[].secret.defaultMode: 288` | the TLS and seal secret files are mounted `0440` |
| `injector.enabled: false`, `csi.enabled: false`, `ui.enabled: false` | no Agent Injector, no CSI provider, no web UI |

#### TLS posture of the probes

The historical 2026-07-28 readiness probe was `bao status -tls-skip-verify`. The 2026-11-16
StatefulSet instead declares `bao status`, without a skip-verify flag, and HTTPS liveness on port 8200.
The chart permits HTTP probe overrides; changing probes is Platform Operations work. The old
"cannot override" statement was inaccurate. All recorded operator status commands and the CA-only
smoke Job verify the CA, and both Dapr components declare `skipVerify: "false"`.

Do not copy `-tls-skip-verify` into an operator command or into the smoke test; the documentation guard
asserts its absence from `deploy/openbao/smoke-test.yaml`.

### `deploy/openbao/namespace.yaml`

Creates namespace `openbao` with the Restricted Pod Security profile and the two ownership annotations.
This file is exact as deployed and Story 31.1 changed nothing in it.

| Declared setting | Deployed value it binds |
| :--------------- | :---------------------- |
| `kubernetes.io/metadata.name: openbao` | the label the NetworkPolicy `namespaceSelector` matches peer namespaces by; the namespace the whole platform runs in |
| `pod-security.kubernetes.io/enforce: restricted` | enforced Restricted profile, version `latest` |
| `pod-security.kubernetes.io/audit: restricted` | audited Restricted profile, version `latest` |
| `pod-security.kubernetes.io/warn: restricted` | warned Restricted profile, version `latest` |
| `hexalith.io/platform-owner: jpiquot` | Platform Operations owner of record is `jpiquot` |
| `hexalith.io/security-reviewer: murat-tea-for-jpiquot` | security reviewer of record is `murat-tea-for-jpiquot`; see [Named divergences](#named-divergences) for why that identity is not independent of the platform owner |

The namespace also carries five profile-identity annotations that this file does **not** declare —
`hexalith.io/composite-profile-sha256`, `hexalith.io/helm-manifest-sha256`,
`hexalith.io/hardening-manifest-sha256`, `hexalith.io/deployment-id`, and `hexalith.io/profile-id`. They
are **not** applied by this manifest. No producer for them exists in this repository: `composite-profile`, `helm-manifest-sha256` and `hardening-manifest-sha256` appear only in the Story 31.1 records, and `tools/render-production-deployment.ps1` computes no annotation. Their origin is out-of-repo platform state with no identified owner. See
[Named divergences](#named-divergences) for what they are and are not evidence of.

### `deploy/openbao/service-account-hardening.yaml`

Applied after each Helm install or upgrade. It sets `automountServiceAccountToken: false` on the
`hexalith-keys` ServiceAccount, and the deployed ServiceAccount does read `false`.

The current StatefulSet template makes a pod-level request for `automountServiceAccountToken: true`, which would override
the ServiceAccount default in Pods created from that template. Actual server Pod automount is unprobed
in this capture; OnDelete permits older admitted Pod specifications. Historically the server held an
API token for `service_registration "kubernetes"` to keep the `-active` and `-standby` endpoints correct.

| Declared setting | Deployed value it binds |
| :--------------- | :---------------------- |
| `kind: ServiceAccount`, `name: hexalith-keys` | the ServiceAccount the OpenBao pods run as |
| `namespace: openbao` | the platform namespace |
| `automountServiceAccountToken: false` | the ServiceAccount default only — measured `false` on the ServiceAccount, historically overridden to `true` at pod level; current template requests true, actual server Pod automount unprobed |

What this file still achieves is denying an automounted token to any other pod that runs as this
ServiceAccount without requesting one in its own spec. Do not read it as evidence that the server holds
no API token; before Story 31.1 this document made exactly that claim and it was false as deployed.

### `deploy/openbao/smoke-test.yaml`

A Restricted-compatible Job that runs `bao status -format=json` against the Service endpoint. At the
2026-07-28 historical baseline, chart `0.28.5`'s built-in Helm test hook lacked the security context this
namespace's enforced Restricted profile requires. The adopted chart `0.29.6` inherits Restricted security
contexts, but its built-in test mounts server TLS/seal material and accepts sealed status. Retain the
approved CA-only Job; do not weaken the namespace policy or widen its Secret projection.

| Declared setting | Deployed value it binds |
| :--------------- | :---------------------- |
| `kind: Job`, `name: hexalith-keys-smoke-test` | one-shot Job `hexalith-keys-smoke-test`, created by [Smoke test](#smoke-test) and reaped by its own TTL |
| `BAO_ADDR: https://hexalith-keys.openbao.svc.cluster.local:8200` | the ClusterIP Service endpoint `hexalith-keys.openbao.svc.cluster.local:8200`, which selects the three observed server Pods; voting membership remains unmeasured |
| `BAO_CACERT: /openbao/tls/ca.crt` | `ca.crt` projected read-only from Secret `openbao-server-tls` via an explicit `items` list, so the server private key `tls.key` is **not** delivered to this pod |
| `automountServiceAccountToken: false` | the Job needs no Kubernetes API access |
| `runAsNonRoot: true`, `allowPrivilegeEscalation: false`, capabilities `- ALL` dropped | Restricted profile compliance |
| `backoffLimit: 0`, `activeDeadlineSeconds: 60`, `ttlSecondsAfterFinished: 300` | one attempt, one minute, reaped five minutes after finishing |
| image digest `sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653` | fixed approved OpenBao 2.6.0 CA-only CLI; its bytes remain bound to PG-ONPREM-2 |

## Smoke test

Run it against the named context with exactly these commands:

```bash
kubectl --context=jpiquot@local apply -f deploy/openbao/smoke-test.yaml
kubectl --context=jpiquot@local -n openbao wait --for=condition=complete job/hexalith-keys-smoke-test --timeout=2m
kubectl --context=jpiquot@local -n openbao logs job/hexalith-keys-smoke-test
```

Capture the logs before `ttlSecondsAfterFinished: 300` reaps the Job. Because `backoffLimit: 0` allows
no retry, a re-run requires deleting the prior Job first with
`kubectl --context=jpiquot@local -n openbao delete job hexalith-keys-smoke-test`,
only after confirming the previous instance completed. The 2026-11-16 preflight found no previous Job.

### Current smoke result on 2026-11-16

| Field | Observed value on 2026-11-16 |
| :--- | :--- |
| initialized | `true` |
| sealed | `false` |
| storage_type | `raft` |
| ha_enabled | `true` |
| version | `2.6.2` |
| Completion UTC | `2026-11-16T15:46:10Z` |
| Client image | `2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653` |
| Admitted CA projection | Job and Pod: exactly `items: [{key: ca.crt, path: ca.crt}]`, Secret `openbao-server-tls`, mode 288; Job confirms read-only mount and automount false; Pod mount/automount fields unprobed |
| Job / Pod result | `hexalith-keys-smoke-test` succeeded 1, Complete; Pod `hexalith-keys-smoke-test-5fj94` Succeeded, exit 0 |

The exact apply/wait/log commands above succeeded. The admitted Job and Pod were captured before
TTL cleanup, including the one-key projection; evidence section 9.3 records the commands and projected
result. This is a status/TLS check against measured 2.6.2, not a Dapr secret-read or 2.6.4 qualification.

### Recorded result

Historical result retained unchanged; it is not the current smoke outcome.

Executed **2026-07-28T13:24:43Z** against context `jpiquot@local`, namespace `openbao`, under the current
manifest — that is, with the `items: [ca.crt]` projection this document describes. The Job reached
`condition met`, and the projection was confirmed on the Job and Pod objects the API server admitted rather
than inferred from the file. An earlier run at **2026-07-28T09:43:31Z** produced identical fields but
predates that projection; it is superseded and is retained only in the evidence artifact. Observed fields:

| Field | Observed value |
| :---- | :------------- |
| `initialized` | `true` |
| `sealed` | `false` |
| `storage_type` | `raft` |
| `ha_enabled` | `true` |
| `version` | `2.6.0` |

The full captured payload, including the recovery-seal threshold and Raft index fields, is recorded in
[`31-1-openbao-platform-evidence.md`](../../_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md).
`bao status` emits no secret value; do not extend this Job with a command that would.

## Deployed platform state not tracked in this repository

Each row is live platform state with no owning file under `deploy/openbao/`. Naming it here is the
disposition Story 31.1 chose; adopting any row into the repository is a separate change.

| Artifact | Kind | Owner | Disposition and reopen trigger |
| :------- | :--- | :---- | :----------------------------- |
| `openbao-raft-snapshot` | CronJob, schedule `30 0 * * *`, `concurrencyPolicy: Forbid` | Hexalith Platform Operations (`jpiquot`) | Out-of-repo platform state. Reopen when snapshot output is copied off-cluster, at which point the CronJob and its retention become a tracked manifest |
| `openbao-snapshot` | ServiceAccount used by the snapshot CronJob | Hexalith Platform Operations (`jpiquot`) | Out-of-repo platform state. Reopen with the CronJob above |
| `openbao-snapshots` | PVC, 2Gi, `openebs-hostpath-retain`, `Bound` | Hexalith Platform Operations (`jpiquot`) | Out-of-repo platform state, and on the same node as the data it protects. Reopen with the CronJob above |
| `hexalith-keys-tokenreview` | ClusterRoleBinding to `system:auth-delegator`, applied with `kubectl`, not Helm-managed | Hexalith Platform Operations (`jpiquot`) | Out-of-repo platform state that duplicates the chart-rendered `hexalith-keys-server-binding`. Reopen when the duplicate is removed or adopted |
| `hexalith-keys-pki` | Secret, type `Opaque` | Hexalith Platform Operations (`jpiquot`) | Purpose not established by read-only probe and its contents are not read. Reopen when its purpose is documented or it is deleted |
| `openbao-seal`, `openbao-operator-credentials`, `openbao-server-tls` | Secrets holding seal key, recovery shares and operator identity, and server TLS | Hexalith Platform Operations (`jpiquot`) | Deliberately out of repository. These must never be checked in |
| `deployment-seal-transit` | Service, `NodePort`, port `8200:30820/TCP` | Hexalith Platform Operations (`jpiquot`) | Extra namespace Service observed 2026-09-06; not one of the four `hexalith-keys*` ClusterIP Services. Reopen when the NodePort is removed, adopted into a tracked manifest, or confirmed as a required seal-transit endpoint |
| `deployment-seal-external` | NetworkPolicy selecting OpenBao server pods | Hexalith Platform Operations (`jpiquot`) | Measured 2026-11-16: port 8200 from namespace `traefik` plus pod label `app.kubernetes.io/name=traefik`, or IP block `192.168.1.30/32`. Historical uncaptured-spec gap DW-729 now has this measurement; ledger disposition is separate. Reopen when these sources are removed, narrowed, or adopted into an owned manifest |
| `deployment-seal-runner-token` | Secret, type `kubernetes.io/service-account-token` | Hexalith Platform Operations (`jpiquot`) | Extra ServiceAccount token Secret observed 2026-09-06; name and type only, contents not read. Reopen when the minting ServiceAccount or controller is documented, or the Secret is deleted |
| `ext-secrets-1-custody-provider-29a4ace57e04` | NetworkPolicy selecting OpenBao server pods | Hexalith Platform Operations (`jpiquot`) | Measured 2026-11-16: port 8200 from namespace `ext-secrets-1-isolated` AND app In [`inventory`, `ext-secrets-1-custody`]. Additive to the other policies. Reopen when the custody lane ends or its consumer/owner justifies adoption or removal |
| `deployment-seal-runner` | Role and RoleBinding to ServiceAccount of the same name | Hexalith Platform Operations (`jpiquot`) | Measured service/NetworkPolicy/Traefik get-patch-update and deployment-seal-ca ConfigMap get grants; no Secret content inspected. Reopen with external seal-route ownership or privilege changes |

## Named divergences

Open reproduction, configuration, review and lifecycle gaps. Measurements above establish observed
state; they do not discharge these obligations. Each row carries an owner and reopen trigger.

| Divergence | Owner | Why it is open | Reopen trigger |
| :--------- | :---- | :------------- | :------------- |
| TLS certificate expiry `2027-08-20 13:38:39 UTC` | Hexalith Platform Operations (`jpiquot`) | Carried forward from the 2026-07-19 platform bootstrap. Re-deriving it requires reading `openbao-server-tls`, which this story's secret-read prohibition forbids, and no cert-manager `Certificate` resource exists in the namespace to report it | A Platform Operations capture of the certificate `notAfter`, or adoption of a cert-manager `Certificate` that publishes `status.notAfter` |
| Operator and Dapr token expiry `2027-07-19 13:41:25 UTC` | Hexalith Platform Operations (`jpiquot`) | Carried forward from the same bootstrap. Re-deriving it requires reading `openbao-operator-credentials` | A Platform Operations token-accessor listing that reports the TTL without printing a token |
| Namespace profile annotations do not reject drift | Hexalith Platform Operations (`jpiquot`) | The `hexalith.io/composite-profile-sha256` and `hexalith.io/deployment-id` annotations still read the values written for release `r2`, while the platform advanced to ten release metadata revisions as of 2026-11-16. An evidence capture keyed on them would have accepted every measured change as no-drift | A drift check that compares live object state rather than a stored annotation, or a renderer that rewrites the annotations on every applied revision |
| `cert-manager` NetworkPolicy rule has no matching consumer | Hexalith Platform Operations (`jpiquot`) | The policy admits every `cert-manager` pod on 8200, but no cert-manager `Certificate` or `Issuer` exists in namespace `openbao` to justify it. The source is retained in `deploy/openbao/values.yaml` **only** because the live policy carries it; removing it from the file alone would recreate tracked-vs-deployed drift. Narrowing is a live change and is sequenced live-first, then file, document and assertions together | The live policy no longer admits namespace `cert-manager` on 8200 — at which point `deploy/openbao/values.yaml`, this row and the documented ingress row are reconciled in one change |
| Reconciled `values.yaml` has not been re-applied | Hexalith Platform Operations (`jpiquot`) | Story 31.1 reconciled the file to the measured release but ran no `helm upgrade`, and `helm` is not available in the authoring environment. The July reconciliation described that historical platform. Current approved 2.6.4/chart 0.29.6 inputs differ from measured 2.6.2/chart label 0.29.5, and reproduction remains unproven. Every value in the reconciliation was inferred from rendered objects, never from a chart render. The 2026-07-28 scope ratifications carved this reproduce-release gap out of Story 31.1: it is Platform Operations work, not a Story 31.1 checkpoint | A Platform Operations `helm diff` or `helm upgrade --dry-run` against release `hexalith-keys` confirming an empty diff |
| `server.authDelegator.enabled: true` grants `system:auth-delegator` with no identified consumer | Hexalith Platform Operations (`jpiquot`) with security reviewer `murat-tea-for-jpiquot` | The chart-rendered ClusterRoleBinding `hexalith-keys-server-binding` grants the server ServiceAccount cluster-scoped TokenReview and SubjectAccessReview. No OpenBao Kubernetes auth method is configured anywhere in this repository, and Dapr authenticates its secret reads with a scoped OpenBao token rather than a Kubernetes ServiceAccount token, so no consumer of that privilege has been identified. The untracked `hexalith-keys-tokenreview` binding grants the same privilege out of band, so clearing the tracked value alone would not revoke it | A named consumer is documented, or both bindings are removed and `server.authDelegator.enabled` is set `false`. Raised by Story 31.1 code review 2026-07-28; AC2 fixes the accepted-limitations table at two rows, so promoting this to a third accepted limitation would need its own approved sprint change |
| Raft snapshots have no off-cluster copy | Hexalith Platform Operations (`jpiquot`) | The `openbao-raft-snapshot` CronJob writes to PVC `openbao-snapshots`, which is `openebs-hostpath-retain` on `node1` — the same and only node that holds all three voters and all six data and audit PVCs. Losing that node loses the data and its backups together, so the platform currently has no recoverable state after node loss. Stated in [Availability profile](#availability-profile) but previously carried no owner or trigger | An off-cluster or off-node copy of the snapshot output exists and a restore has been rehearsed, at which point the CronJob and its retention become a tracked manifest |
| Namespace manifest hashes no longer re-derive from the tracked files | Hexalith Platform Operations (`jpiquot`) | Story 31.1 changed `deploy/openbao/values.yaml` and `deploy/openbao/service-account-hardening.yaml`, both of which feed the `hexalith.io/helm-manifest-sha256` and `hexalith.io/hardening-manifest-sha256` annotations recorded below. Those annotations, the composite, `deployment-id` and `profile-id` all still identify release `r2` and are **not** re-derivable from the current tracked sources | The production deployment renderer recomputes and re-applies the profile annotations for the current revision |
| Documented voter count and HA mode require live re-measurement | Hexalith Platform Operations (`jpiquot`) | The 2026-11-16 capture confirms three healthy servers and ha_enabled true on one node, not voting membership; repository guards bind the dated observations, not live Kubernetes state, so a later cluster-only change can pass those guards | Any release revision, replica, HA-mode, seal or ingress change: re-measure and reconcile current documentation, evidence and assertions; approved values change only under their own authorization |
| The security reviewer of record is not independent of the platform owner | Platform Operations owner Jérôme Piquot (`jpiquot`) | On 2026-11-22, Jérôme Piquot (`jpiquot`) closed Story 31.1 by sole-owner risk acceptance for C4b, C5b and C7. No independent countersignatures were obtained. EVAL-01 remains open with Platform Operations. The July C7 waiver expired on 2026-10-26. | Reassess on seal/ingress/voter changes or Story 31.2 activation. |
| Approved inputs differ from current deployment | Hexalith Platform Operations (`jpiquot`) | Approved 2026-10-04 server 2.6.4/chart 0.29.6 differs from measured 2026-11-16 server 2.6.2 and chart label 0.29.5 at ten metadata revisions. The live chart digest was not measured. No upgrade/downgrade was authorized or applied | Platform Operations separately reconciles and reproduces the intended release, then captures exact deployed image/chart identity and fresh smoke evidence |
| Live rendered configuration differs from approved values | Hexalith Platform Operations (`jpiquot`) | Live HCL adds retry_join peers 1 and 2 and allow_unauthenticated_workflows false; container adds readOnlyRootFilesystem true plus writable tmp/home volumes. Current readiness is bao status without skip-verify. Approved values remain unchanged; configuration checksum with OnDelete does not automatically restart pods | An authorized reconciliation compares all rendered settings and records reproduction evidence; do not silently copy live changes into the approved inputs |
| Additional ingress policies widen effective sources | Hexalith Platform Operations (`jpiquot`) | NetworkPolicies are additive: deployment-seal-external admits traefik pods and 192.168.1.30/32; ext-secrets-1-custody-provider-29a4ace57e04 admits the named isolated custody apps on 8200. The hexalith-keys policy alone is not the effective source inventory. These are owned divergences, not a third accepted limitation | Owners justify, narrow, remove or adopt each extra source, followed by a fresh union-of-policies capture; no live ingress change is part of this story |
| Application namespace identity metadata no longer present | Hexalith Platform Operations (`jpiquot`) | The 2026-11-16 projection finds no hexalith.io annotations on hexalith-memories; openbao retains historical release-r2 hashes/IDs. The historical statement that both namespaces carry them is no longer a current-state claim | The separately owned profile producer establishes current namespace identity and recomputes annotations with evidence |
| Voting membership is unmeasured | Hexalith Platform Operations (`jpiquot`) | Three healthy servers, Ready replicas and unauthenticated status establish neither voting membership nor failover quorum. The single node remains the whole failure domain regardless of member roles | Obtain a separately authorized, redacted Raft membership proof and reconcile the intended three-voter profile; never read or publish Secret contents or operator credentials for this task |
| Declared configuration lacks loaded-runtime reconciliation | Hexalith Platform Operations (`jpiquot`) | Security, env, probe and mount fields above come from the StatefulSet template, not admitted server Pods; OnDelete permits older Pods. ConfigMap HCL is declared configuration, not proof that each server loaded it | Safely project each admitted server Pod's non-secret fields and reconcile them with its template; establish secret-safe loaded-configuration evidence separately before claiming effective runtime agreement |
| Traefik custom-route exposure is unmeasured | Hexalith Platform Operations (`jpiquot`) | An empty standard Ingress listing does not inventory Traefik IngressRouteTCP/MiddlewareTCP routes or external reachability of deployment-seal-transit | Read-only projection of relevant Traefik routes/middleware and referenced Service/allowlist, followed by owner-led exposure reconciliation; no route mutation is authorized here |

The three manifest hashes historically recorded on both namespaces were
`1deba6e0456bb44ea0624a0f436b209b5ede2c496cc9be98fea5b9dbee1db539` (application manifest),
`f55ff3c237fad5047d6ad7d19a56a83c6546a2f31fc5830022bc3b3a51c9c8e3` (Helm manifest), and
`f3fe70b98c64ec9072bc3ab54fd07ffdde1e4d4550c53d8f20e2bb58eb70f3eb` (OpenBao namespace plus
service-account hardening), composing to `4183b741eac062d962a8ff1860a7aa049719a75f47e38e6fdcfb0fe1aeaa5d45`.
They identify the release the annotations were written for. They are not a drift detector, and since
Story 31.1 changed both hashed manifest sources they no longer recompute from the tracked files — see the
`Namespace manifest hashes no longer re-derive` row above. Treat all four as release-`r2` identity only.

## Accepted limitations

On 2026-11-22 Jérôme Piquot (`jpiquot`) accepted the static file-based seal (C4b) and namespace-wide port 8200 ingress (C5b) for the measured profile without independent countersignatures. Each row records the consequence, compensating controls and reopen trigger.

| Limitation | Owner | Consequence | Compensating controls | Reopen trigger |
| :--------- | :---- | :---------- | :-------------------- | :------------- |
| Static file-based seal | Hexalith Platform Operations (`jpiquot`) with security reviewer `murat-tea-for-jpiquot` | The seal key is a file in Secret `openbao-seal`, in namespace `openbao`, beside the `data-hexalith-keys-0..2` PVCs it decrypts. One namespace-level read yields both the ciphertext and the key, so the storage encryption stops any attacker who cannot read the namespace and stops no attacker who can | Restricted Pod Security enforced on the namespace; RBAC scoped to the platform ServiceAccounts; the four `hexalith-keys*` Services ClusterIP with no LoadBalancer on those Services; no standard Ingress objects observed, while Traefik custom routes remain unmeasured; persistent JSON audit device recording every access. Deliberately **not** counted as compensating, because neither survives the consequence stated in this row: the port 8200 NetworkPolicy, which the adjacent limitation records as admitting every pod in two namespaces and contributing no defence in depth; and the `shamir` 2-of-3 recovery threshold, whose shares live in Secret `openbao-operator-credentials` in this same namespace and are not yet exported to escrow, so they sit inside the blast radius rather than outside it | Migrating `seal "static"` to an external KMS or HSM-backed seal, so the key stops living beside the data |
| Namespace-wide port 8200 ingress | Hexalith Platform Operations (`jpiquot`) with security reviewer `murat-tea-for-jpiquot` | The NetworkPolicy uses a `namespaceSelector` with no `podSelector`, so every pod in `hexalith-memories` and every pod in `cert-manager` may reach 8200 — 10 pods at the historical 2026-07-28 measurement, including `redis-stack-0`, `falkordb-0`, `access-telemetry-postgresql-0`, and both `memories-mcp` pods, which architecture D31 scopes to receive neither secret component | Dapr token authentication in front of every secret read; per-component OpenBao policies (`hexalith-memories-runtime` and `hexalith-memories-access-telemetry`, both read-only) scoping what a reachable caller may fetch; mandatory TLS verification with `skipVerify: "false"`; persistent audit device recording every request | Narrowing the selector to a `podSelector` covering only the pods that actually consume a Dapr secret component |

### Static file-based seal

The declared ConfigMap seal stanza is `seal "static"` with `current_key_id = "kubernetes-openbao-seal-v1"` and
`current_key = file:///openbao/userconfig/openbao-seal/current.key`. The StatefulSet template mounts the file read-only from
Secret `openbao-seal` in namespace `openbao` — the same namespace that holds the `data-hexalith-keys-0..2`
PVCs containing the encrypted Raft store.

The intended three-server profile does not change this risk: each declared server configuration uses
the same key from the same Secret. Current voting membership and loaded-runtime reconciliation remain
unproven; the single-node failure boundary and co-location of declared key material are unchanged.

This is the accepted trade for running an in-cluster secret store without an external key manager. It is
adequate for a single-operator platform where namespace access is already equivalent to platform
administration, and it is not adequate for a service whose threat model includes an attacker with
namespace-level read. Migrating to an external KMS or HSM-backed seal is the reopen trigger, and it is
the single change that most improves this platform's security posture.

### Namespace-wide port 8200 ingress

The `hexalith-keys` NetworkPolicy still has two ingress rules as measured on 2026-11-16.
The additional policies in the current profile are additive and widen effective sources on 8200;
see their owned rows above. This limitation describes namespace-wide access in the base policy. The first admits port 8200 from two
`namespaceSelector` sources, `hexalith-memories` and `cert-manager`, with no `podSelector` on either. A
`namespaceSelector` without a `podSelector` matches every pod in the namespace, so the rule admits all of
them. The second rule admits ports 8200 and 8201 from the OpenBao pods themselves, which is Raft cluster
traffic and is correctly scoped.

At the historical 2026-07-28 measurement the first rule covered 10 pods: seven in `hexalith-memories` — `access-telemetry-postgresql-0`,
`falkordb-0`, `redis-stack-0`, `memories-b667844cf-6s9j7`, `memories-b667844cf-bs4gm`,
`memories-mcp-8fd85c7c9-422vf`, `memories-mcp-8fd85c7c9-vf7n9` — and three in `cert-manager`. Per
architecture decision D31 the MCP sidecar is scoped to receive neither secret component, and the three
data backends consume no Dapr secret component at all, so most of that reachability serves no consumer.

Network reachability is not authorization: a reachable caller still needs a Dapr token and a scoped
OpenBao policy to read anything. The limitation is that the network layer contributes no defence in
depth here. The reopen trigger is narrowing the selector to a `podSelector` covering only the pods that
consume a Dapr secret component.

## Dapr secret boundaries

Dapr uses OpenBao through component type `secretstores.hashicorp.vault`. Dapr publishes an
[OpenBao secret store page](https://docs.dapr.io/reference/components-reference/supported-secret-stores/openbao/)
and a [HashiCorp Vault page](https://docs.dapr.io/reference/components-reference/supported-secret-stores/hashicorp-vault/);
the deployed component `type` string is the Vault one, because OpenBao serves the Vault API. Read that as a
statement about the type string only — it is taken from the deployed component, not inferred. TLS verification is
mandatory (`skipVerify: "false"`) and bootstraps from narrowly RBAC-scoped Kubernetes Secrets:

| Dapr component | Bootstrap Secret | OpenBao prefix | Policy |
| :------------- | :--------------- | :------------- | :----- |
| `secretstore` | `openbao-runtime-bootstrap` | `secret/hexalith/memories/runtime` | `hexalith-memories-runtime` (read only) |
| `access-telemetry-secrets` | `openbao-access-telemetry-bootstrap` | `secret/hexalith/memories/access-telemetry` | `hexalith-memories-access-telemetry` (read only) |

The bootstrap Secrets contain only a scoped OpenBao token and the internal CA certificate. Dapr
explicitly recommends a local secret store such as Kubernetes for this bootstrap credential. These two
prefixes are shared **application** scopes, not per-tenant partitions; nothing here isolates one tenant
from another.

OpenBao replaces Kubernetes as the provider for secrets resolved by Dapr components. It does not inject
environment variables into application or database containers. Therefore `redis-secret`,
`app-api-token`, `dapr-api-token`, clock keys, and similar direct pod inputs remain Kubernetes Secrets;
deleting those copies breaks pod startup. Adopting the OpenBao Agent Injector or CSI provider for those
values is a separate deployment change, and both are disabled in `deploy/openbao/values.yaml` today.

Story 29.2 retired the standalone `deploy/dapr/components/secretstore.yaml` and
`deploy/dapr/components/access-telemetry-secrets.yaml` templates' prior `secretstores.kubernetes` /
`secretstores.local.file` types. They now mirror the table above exactly — same
`type: secretstores.hashicorp.vault`, same bootstrap Secret names, same OpenBao prefixes and scopes —
but are deployment-oriented templates, not bound to a live cluster; they carry no bootstrap Secret of
their own, and the platform that applies them must supply `openbao-runtime-bootstrap` /
`openbao-access-telemetry-bootstrap` before the sidecar can start. On Kubernetes this `secretKeyRef`
resolves through the cluster's built-in Kubernetes secret store, matching `deploy/kubernetes/base/dapr/*`.
A standalone (non-Kubernetes) Dapr self-hosted host has no such resolver by default -- it would need its
own separately configured secret-store component to resolve these `secretKeyRef` references, which these
templates do not define; treat that deployment shape as unverified until it is exercised, not as a
supported target of this document.
`Hexalith.Memories.Aspire`'s two hosting extensions (`AddHexalithMemoriesSearchIndexServer`,
`AddHexalithMemoriesAccessTelemetry`) apply the same rule at the AppHost-composition layer: each takes
an externally-provisioned secret-store `IResourceBuilder<IDaprComponentResource>` instead of
constructing one, so the library itself never depends on OpenBao, Kubernetes, or any other secret
provider.

### Runtime Kubernetes Secret inventory

The July 28 names below come from the Story 31.2 creation measurement. On November 22 at 15:16Z, a names/types projection of `jpiquot@local` namespace `hexalith-memories` found fourteen of those fifteen names and no new name. `access-telemetry-postgresql` was absent. The inventory distinguishes live objects from grants in the tracked Kubernetes base and overlays. The four platform rows describe the July boundary in the separate `openbao` namespace, which that measurement did not list. Measurements, commands and drift are recorded in `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md`. On November 28 at 12:12Z, after the Dapr 1.19.2 rollout, the same projection found the same fourteen names and types.

| Secret | Namespace | Referenced by | Justification class | Owner | Reopen trigger |
| --- | --- | --- | --- | --- | --- |
| openbao-runtime-bootstrap | hexalith-memories | Present. Runtime Component `secretstore` resolves its token and CA through a Kubernetes secretKeyRef, tracked and live. The memories and reserved eventstore Roles grant get. | openbao-bootstrap | Platform Operations | Runtime authentication, trust root or app scope changes |
| openbao-access-telemetry-bootstrap | hexalith-memories | Present. Telemetry Component resolves its token and CA through a Kubernetes secretKeyRef. Lifecycle and clock Roles grant get. Base and qualification also grant it to the memories Role. Production removes that grant and the memories scope, but the live Role and scope still include memories. | openbao-bootstrap | Platform Operations / Story 27.3 | Telemetry bootstrap or scopes change, or a production overlay apply |
| app-api-token | hexalith-memories | Present. Direct pod input through env in the server, MCP, lifecycle and clock templates and the Dapr injector annotations. All five application Roles grant get. | direct-pod-input | Platform Operations | App-channel authentication changes |
| dapr-api-token | hexalith-memories | Present. Direct pod input through env in the same four templates and injector annotations. The qualification evidence reporter also consumes it. All five application Roles grant get. | direct-pod-input | Platform Operations | Sidecar API authentication changes |
| redis-secret | hexalith-memories | Present. Direct Redis/FalkorDB input. Server env builds the Redis and FalkorDB connection strings, and the redis-stack and falkordb StatefulSets read the password directly, tracked and live. Memories, eventstore and lifecycle Roles grant get. Separately, state and pub/sub resolve the namesake OpenBao key in the runtime store, and telemetry configuration resolves it in the telemetry store. On November 28, both memories sidecars and the lifecycle sidecar running Dapr 1.19.2 returned HTTP 200 with the password field present in their respective Vault stores. [November 28 probe matrix](../../_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md#built-in-kubernetes-store-and-vault-probe-matrix) | direct-pod-input | Memories Maintainer / data-plane owner | Review direct server credentials and excess grants when data-plane authentication changes |
| llm-secret | hexalith-memories | Present. The tracked base and qualification grant it through the memories Role; tracked production and live Roles no longer grant it. No env, volume or pull reference exists. The conversation component resolves OPENAI_API_KEY from the namesake runtime OpenBao key. Retained without a grant under the owner's safety rail, because OpenBao returned HTTP 403 to metadata list and read for the runtime prefix. | residue | Memories Maintainer / Platform Operations | Recheck on a new Kubernetes consumer, rotation or authorized metadata access. Delete only after a fresh check finds zero live consumers and authorized metadata confirms this key |
| google-embedding-api-key | hexalith-memories | Present. The tracked base and qualification grant it through the memories Role; tracked production and live Roles no longer grant it. No env, volume or pull reference exists. The runtime allow-list names the namesake OpenBao key for the embedding provider. Retained without a grant under the owner's safety rail, because OpenBao returned HTTP 403 to metadata list and read for the runtime prefix. | residue | Memories Maintainer / Platform Operations | Recheck on a new Kubernetes consumer, rotation or authorized metadata access. Delete only after a fresh check finds zero live consumers and authorized metadata confirms this key |
| memories-embedding-client-secret | hexalith-memories | Present. The tracked base and qualification grant it through the memories Role; tracked production and live Roles no longer grant it. No env, volume or pull reference exists. The runtime allow-list names the namesake OpenBao key for the embedding client. Retained without a grant under the owner's safety rail, because OpenBao returned HTTP 403 to metadata list and read for the runtime prefix. | residue | Memories Maintainer / Platform Operations | Recheck on a new Kubernetes consumer, rotation or authorized metadata access. Delete only after a fresh check finds zero live consumers and authorized metadata confirms this key |
| access-telemetry-marker-key | hexalith-memories | Present. The tracked base and qualification grant it through the memories and lifecycle Roles; tracked production and live Roles no longer grant it. Lifecycle env carries only the key name for the telemetry OpenBao store, and no env value, volume or pull reference exists. Retained without a grant under the owner's safety rail, because OpenBao returned HTTP 403 to metadata list and read for the telemetry prefix. | residue | Story 27.3 / Platform Operations | Recheck on marker resolution changes, a new Kubernetes consumer or authorized metadata access. Delete only after a fresh check finds zero live consumers and authorized metadata confirms this telemetry key |
| access-telemetry-clock-key | hexalith-memories | Present. Direct pod input. Lifecycle env reads the public verification key, tracked and in the running lifecycle pod. The clock uses a signing-key name in the telemetry store. The clock Role grants get. | story-27.3-retained | Story 27.3 | Verification-key delivery or signing contract changes |
| access-telemetry-clock-sources | hexalith-memories | Present. Direct pod input. Clock env references the three external time-source authentication tokens. The clock Deployment is at zero replicas, tracked and live. | story-27.3-retained | Story 27.3 | Time-source authentication changes |
| access-telemetry-postgresql | hexalith-memories | Absent on November 22 and present on July 28. The telemetry PostgreSQL component resolves connectionString from the telemetry OpenBao store. No tracked or live Kubernetes reference or Role grant exists. | residue | Story 27.3 / Platform Operations | The object reappears or gains a Kubernetes consumer |
| access-telemetry-postgresql-bootstrap | hexalith-memories | Present. Direct pod input. The PostgreSQL StatefulSet mounts it, tracked and live. Production renders zero replicas, while the live StatefulSet runs one. | story-27.3-retained | Story 27.3 | PostgreSQL initialization changes |
| access-telemetry-postgresql-tls | hexalith-memories | Present. Direct pod input. The PostgreSQL and lifecycle pods mount it, tracked and live. The untracked live postgresql-tls-reloader Role also grants get. | story-27.3-retained | Story 27.3 | Certificate mount or database trust changes, or adoption or removal of the reloader |
| registry-credentials | hexalith-memories | Present, type kubernetes.io/dockerconfigjson. imagePullSecrets on the memories, memories-mcp, eventstore, lifecycle and clock ServiceAccounts, tracked and live. Kubelet consumes the registry credential. | direct-pod-input | Platform Operations | Images, registry or pull identity change |
| openbao-seal | openbao | Platform file seal uses a mounted credential. | platform-31.1 | Platform Operations | Seal design changes |
| openbao-server-tls | openbao | Platform server listener certificate and key. | platform-31.1 | Platform Operations | Listener trust or renewal changes |
| hexalith-keys-pki | openbao | Platform PKI object from the July inventory. See the platform TLS record. | platform-31.1 | Platform Operations | CA or issuance changes |
| openbao-operator-credentials | openbao | Platform operator bootstrap and recovery custody per the July inventory. | platform-31.1 | Platform Operations | Operator credential custody changes |

The four story-27.3-retained rows are direct pod inputs for the clock or database qualification lane. Story 27.3 owns their delivery mechanisms. The base and qualification memories reader Role names nine Secrets, while tracked `production` now names four (`app-api-token`, `dapr-api-token`, `redis-secret`, `openbao-runtime-bootstrap`), retiring the `production-dapr-1.19` candidate overlay. Since November 28 the live Role names five: `app-api-token`, `dapr-api-token`, `redis-secret`, `openbao-runtime-bootstrap` and `openbao-access-telemetry-bootstrap`. The telemetry bootstrap grant stays while the live telemetry store scopes memories. The live and tracked `production` lifecycle Role no longer grants `access-telemetry-marker-key`. The Redis object has direct pod consumers. That use does not require the application ServiceAccount to read the Kubernetes API. Owners must check all RoleBindings and live consumers before narrowing these grants further.

Dapr's built-in `kubernetes` store reads from the Kubernetes API under its own Configuration scope. Without a `kubernetes` deny scope it bypasses OpenBao and the allow-lists and returns any Secret that the sidecar's ServiceAccount may get. For memories that includes `openbao-runtime-bootstrap`, so the application could obtain the runtime OpenBao token through its own sidecar. On November 22 a metadata-only request listed it in the memories sidecars beside both Vault stores, and as the only secret store in the memories-mcp sidecars. No Configuration scoped `kubernetes` then, and no workload set `dapr.io/disable-builtin-k8s-secret-store`. On November 28 the shared control plane and every running Memories sidecar moved to Dapr 1.19.2, pinned by digest. The four live Configurations `memories-config`, `memories-mcp-config`, `memories-access-telemetry-config` and `memories-access-telemetry-clock-config` gained a `kubernetes` scope with `defaultAccess: deny`, and `deploy/kubernetes/overlays/production` now renders the same pinned Dapr 1.19.2 sidecars, `kubernetes` deny scopes, and `op: test`-guarded residue grant removals directly (retiring the temporary `production-dapr-1.19` candidate overlay). The store stays registered. A read of a synthetic name that no Secret carries returned HTTP 403 `ERR_PERMISSION_DENIED` in all five running memories, memories-mcp and lifecycle sidecars, with app ID and runtime version verified. Permitted Vault reads still succeeded. The clock Configuration carries the same scope, but the clock Deployment runs zero replicas, so its proof is structural only. Platform Operations and Memories Maintainer own this control. The tracked `base` and `qualification` renders still retain PG-ONPREM-2 inputs (lacking the deny scope and retaining all four residue grants until PG-ONPREM-2 requalification). Applying `qualification` would leave the built-in path open and grant those names in `hexalith-memories-qualification`. Applying `production` to the live namespace preserves the built-in deny scopes and residue removals, and also scales the lifecycle and clock Deployments and PostgreSQL StatefulSet to zero, removes `memories` from both telemetry component scopes, and removes `openbao-access-telemetry-bootstrap` from the memories Role; a partial apply that drops the grant while live `access-telemetry-secrets` still scopes `memories` can break bootstrap on restart, so Platform Operations must coordinate RBAC and component scope changes. The November 28 rollout applied scoped patches and preserved live replicas, PostgreSQL, telemetry scopes and the bootstrap grant. Reopen on an overlay apply, when a Configuration loses the deny scope, when a new app ID joins the namespace, or when the clock Deployment scales above zero. [Tracked overlay alignment](../../_bmad-output/implementation-artifacts/deferred-work.md#dw-787-align-the-tracked-overlays-with-live-secret-store-enforcement)

On 2026-11-28 sole owner Jérôme Piquot (`jpiquot`) accepted C2 based on live enforcement and the Dapr 1.19.2 overlay boundary (folded directly into `deploy/kubernetes/overlays/production` in Epic 31 retrospective remediation A1). The owner separately accepted the four live grant removals and temporary retention of the residue Secrets under the HTTP 403 metadata-policy blocker.

**Story 31.2 activation reassessment (2026-11-28, Epic 31 retro A5):** Story 31.1's reviewer-independence divergence (`Reassess on seal/ingress/voter changes or Story 31.2 activation`) was reassessed after Story 31.2 activated, rolled out Dapr 1.19.2 built-in-store deny enforcement, and completed on 2026-11-28. The sole-owner risk acceptance for C4b, C5b, and C7 remains in force without independent countersignature, live `EVAL-01` peer-TLS remediation is registered in `deferred-work.md` as `DW-791`, and no seal/ingress/voter configuration changed in namespace `openbao`.

- `llm-secret`, `google-embedding-api-key` and `memories-embedding-client-secret`: their names were dropped from `memories-dapr-secret-reader`. Delete each object only after a fresh check finds zero live consumers and an authorized metadata-only read confirms its namesake key under `secret/hexalith/memories/runtime`. OpenBao returned HTTP 403 to metadata list and read under the Dapr bootstrap policies, leaving metadata confirmation blocked. Under the owner's safety rail all three stay without a grant. Owners are Memories Maintainer and Platform Operations. Consequence: an unused Kubernetes copy stays readable by anyone who can read Secrets in the namespace, and it can go stale after an OpenBao rotation. The Velero backup `hexalith-memories-31-2-pre-dapr-1192` also holds them until 2026-12-28.
- `access-telemetry-marker-key`: its name was dropped from the memories and lifecycle Roles. It stays without a live grant because the telemetry bootstrap policy also returned HTTP 403 to metadata list and read. Delete only after a fresh check finds zero live consumers and an authorized metadata-only read confirms its namesake key under `secret/hexalith/memories/access-telemetry`. Owners are Story 27.3 and Platform Operations, with the same consequence.
- `access-telemetry-postgresql`: no action remains. The object was already absent, and this story neither removed it nor identified who did.

The reserved `eventstore` Role grants get on `app-api-token`, `dapr-api-token`, `redis-secret` and `openbao-runtime-bootstrap` to a ServiceAccount that no workload uses. Owners are Memories Maintainer and Platform Operations. Any principal that can create pods with that ServiceAccount can read the runtime OpenBao bootstrap and the other three Secrets. Proposed disposition: remove the Role and RoleBinding until an eventstore-app-id workload is deployed, then grant only what it needs. That proposal is neither performed nor accepted here. The deployment of an eventstore-app-id workload remains the reopen trigger. The live drift is also recorded in the evidence, each item with an owner and reopen trigger. It comprises the telemetry store and configuration component still scoped to memories, the memories Role's telemetry bootstrap grant, the lifecycle allow-list without `access-telemetry-postgresql` and the untracked `postgresql-tls-reloader` objects. Platform Operations owns reconciliation.

The direct Redis/FalkorDB server inputs remain an alignment gap. They bypass Dapr application-secret resolution, and both backends and the server must stay synchronized on rotation. The runtime and access-telemetry prefixes are shared application scopes. Story 29.2 already migrated the standalone template under `deploy/dapr`. Its composition and bootstrap remain Epic 29 responsibilities.

## Health and access checks

Run checks without printing secret values:

`initialized` and `sealed` are **per-voter** fields, so check all three rather than one. The loop below is
deliberate: querying only `hexalith-keys-0` reports whichever role that pod happens to hold — at
measurement the leader was `hexalith-keys-1` — and would not reveal a single sealed or still-joining voter.

```bash
kubectl -n openbao get pod,service,pvc,networkpolicy
for pod in hexalith-keys-0 hexalith-keys-1 hexalith-keys-2; do
  echo "== $pod"
  kubectl -n openbao exec "$pod" -- \
    env BAO_CACERT=/openbao/userconfig/openbao-server-tls/ca.crt \
    bao status -format=json \
    | jq '{initialized,sealed,storage_type,ha_enabled,version}'
  kubectl -n openbao logs "$pod" --since=30m \
    | jq -r 'select(."@level" == "error")'
done
kubectl -n hexalith-memories get component secretstore access-telemetry-secrets -o yaml
```

Expected status on **every** voter is `initialized: true`, `sealed: false`, `storage_type: "raft"`,
and `ha_enabled: true`. OpenBao `2.6.0` is the 2026-07-28 historical baseline. The adopted PG-ONPREM-2
server input remains `2.6.4`, not deployed by this work. The current measured server is `2.6.2`
on all three voters; the 2026-11-16 smoke confirms that observed server only. The StatefulSet must be `3/3` Ready, all
six data and audit PVCs must be `Bound`, and the four `hexalith-keys*` Services must remain `ClusterIP`.
The extra Service `deployment-seal-transit` is NodePort `8200:30820` and is recorded under Deployed platform state not tracked.

After any Helm install or upgrade, re-apply the ServiceAccount hardening **and** re-check the pod-level
override it depends on:

```bash
kubectl apply -f deploy/openbao/service-account-hardening.yaml
kubectl -n openbao get sa hexalith-keys -o jsonpath='{.automountServiceAccountToken}{"\n"}'
kubectl -n openbao get pod hexalith-keys-0 -o jsonpath='{.spec.automountServiceAccountToken}{"\n"}'
kubectl -n openbao get endpoints hexalith-keys-active hexalith-keys-standby
```

The ServiceAccount should read `false`; verify the actual Pod override rather than infer `true`
from the StatefulSet template. That pairing is what lets
`service_registration "kubernetes"` keep the `-active` and `-standby` endpoints correct while no other pod
using this ServiceAccount gets a token by default. If a chart upgrade ever stops emitting the pod-level
`true`, the server loses the API token it needs and those two endpoints go stale **silently** — the pods
stay `Running` and `bao status` still reports a leader. The endpoint check above is the detection.

## Rotation and recovery

Platform Operations must rotate the operator and Dapr tokens before **2027-07-19 13:41:25 UTC**, update
the two bootstrap Secrets, and restart the Dapr workloads because actor-state hot reload is disabled.
Rotate the server certificate before **2027-08-20 13:38:39 UTC**. Both deadlines are carried forward
from the platform bootstrap and are listed in [Named divergences](#named-divergences) because neither was
re-derived here. Use token accessors from `openbao/openbao-operator-credentials` to revoke superseded
tokens; never log token identifiers.

The `openbao-operator-credentials` Secret contains three recovery shares with a 2-of-3 threshold and the
non-root operator identity, matching the measured `recovery_seal_type: shamir` with `t: 2`, `n: 3`. The
initial root token was revoked and is not retained. Export the shares into independent
security-controlled escrow and remove them from the cluster once the recovery ceremony is proven.

Do not delete any of `data-hexalith-keys-0`, `data-hexalith-keys-1`, `data-hexalith-keys-2`,
`audit-hexalith-keys-0`, `audit-hexalith-keys-1`, or `audit-hexalith-keys-2`. The StatefulSet PVC
retention policy and the `openebs-hostpath-retain` StorageClass retain them, but every one of them lives
on `node1`, so loss of that node still requires an off-cluster Raft snapshot and a recovery plan. The
`openbao-raft-snapshot` CronJob writes to a PVC on the same node and does not by itself satisfy that
requirement.

## References

- [OpenBao Kubernetes deployment](https://openbao.org/docs/platform/k8s/)
- [OpenBao Helm production checklist](https://openbao.org/docs/platform/k8s/helm/run/)
- [OpenBao static seal](https://openbao.org/docs/configuration/seal/static/)
- [OpenBao Raft storage and `retry_join`](https://openbao.org/docs/configuration/storage/raft/)
- [OpenBao Kubernetes service registration](https://openbao.org/docs/configuration/service-registration/kubernetes/)
- [Kubernetes ServiceAccount token automounting](https://kubernetes.io/docs/tasks/configure-pod-container/configure-service-account/)
- [Kubernetes NetworkPolicy selectors](https://kubernetes.io/docs/concepts/services-networking/network-policies/)
- [Dapr OpenBao secret store](https://docs.dapr.io/reference/components-reference/supported-secret-stores/openbao/)
- [Dapr HashiCorp Vault secret store](https://docs.dapr.io/reference/components-reference/supported-secret-stores/hashicorp-vault/)
- [Architecture D31 — OpenBao-first DAPR secret provider](../../_bmad-output/planning-artifacts/architecture.md#d31--openbao-first-dapr-secret-provider)
- [Story 31.1 platform evidence](../../_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md)
