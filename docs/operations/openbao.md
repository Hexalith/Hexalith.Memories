# Hexalith Keys OpenBao operations

`hexalith-keys` is the internal OpenBao server deployed to the Kubernetes target reached through context
`jpiquot@local`. Platform Operations owner: **Jérôme Piquot (`jpiquot`)**. Security reviewer:
**Murat TEA for Jérôme (`murat-tea-for-jpiquot`)**.

Everything below was re-measured read-only on **2026-07-28** against the running platform. Where the
tracked manifests and this document previously disagreed with the platform, the platform is the
authority and the manifests were reconciled to it. No claim in this document is carried over from an
earlier revision unless it is listed in [Named divergences](#named-divergences) with an owner and a
reopen trigger. The full probe transcript is
[`31-1-openbao-platform-evidence.md`](../../_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md).

No secret value appears in this document. The unseal key material, recovery shares, operator
credentials, TLS private key, and Dapr tokens exist only in Kubernetes Secrets and are never copied into
documentation, evidence, or source control.

## Deployed profile as measured

| Contract | Measured value on 2026-07-28 |
| :------- | :--------------------------- |
| Kubernetes context | `jpiquot@local` |
| Kubernetes nodes | one node, `node1`, roles `control-plane,worker`, `v1.34.9` |
| Namespace | `openbao` |
| Application namespace | `hexalith-memories` |
| Helm release, Service, StatefulSet | `hexalith-keys` |
| Helm release revisions | 9 (`sh.helm.release.v1.hexalith-keys.v1` through `v9`) |
| Chart | official `oci://ghcr.io/openbao/charts/openbao`, `0.28.5`, digest `sha256:1c2e01185430b9bc426da870909fdccfbb4e3e4758f0c6f8cccfbceead4381ff` |
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
| Services | `hexalith-keys`, `hexalith-keys-internal` (headless), `hexalith-keys-active`, `hexalith-keys-standby` — all `ClusterIP`, no ingress |
| TLS | on: `tls_disable = 0`, `tls_min_version = "tls12"`, cert and key from Secret `openbao-server-tls` |
| Service registration | `service_registration "kubernetes" {}`, backed by Role `hexalith-keys-discovery-role` (pods: `get`, `watch`, `list`, `update`, `patch`) |
| ServiceAccount token | ServiceAccount default is `false`, but pod spec sets `automountServiceAccountToken: true`, which wins — the server does receive and use a Kubernetes API token |
| Token review binding | ClusterRoleBinding `hexalith-keys-server-binding` binds the ServiceAccount to `system:auth-delegator` |
| Pod Security | namespace carries Restricted `enforce`, `audit`, and `warn` labels at `latest` |
| Pod security context | `runAsNonRoot: true`, `runAsUser: 100`, `runAsGroup: 1000`, `fsGroup: 1000`, `seccompProfile: RuntimeDefault`, `allowPrivilegeEscalation: false`, all capabilities dropped |
| NetworkPolicy | port 8200 from every pod in `hexalith-memories` and every pod in `cert-manager`; ports 8200 and 8201 from the OpenBao pods themselves |
| Backup | CronJob `openbao-raft-snapshot` at `30 0 * * *`, ServiceAccount `openbao-snapshot`, 2Gi PVC `openbao-snapshots`, `concurrencyPolicy: Forbid`, two completed Jobs observed |
| Update strategy | `OnDelete`, `podManagementPolicy: OrderedReady`, governing Service `hexalith-keys-internal` |
| PVC retention | `whenDeleted: Retain`, `whenScaled: Retain` |
| Deployment ID | `memories-production-2.11.0-c7c2ca21-hexalith-keys-r2` |
| Profile ID | `redis-state-v1-dapr-1.18.1-openbao-2.6.0-4183b741eac062d9` |

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
kubectl -n openbao get cronjob,job
kubectl -n openbao get secret --no-headers -o custom-columns=NAME:.metadata.name,TYPE:.type
kubectl get ns openbao -o jsonpath='{.metadata.labels}{"\n"}{.metadata.annotations}'
kubectl -n openbao exec hexalith-keys-0 -- env BAO_CACERT=/openbao/userconfig/openbao-server-tls/ca.crt bao status -format=json
```

`kubectl get secret` is used for names and types only. Never render `-o yaml` or a `.data` JSONPath for
`openbao-seal`, `openbao-operator-credentials`, `openbao-server-tls`, or `hexalith-keys-pki`.

## Availability profile

Both halves of this are true and both matter.

**There is process-level failover.** Three Raft voters run with `ha_enabled: true`. One is the leader,
the others are standbys, and the `hexalith-keys-active` and `hexalith-keys-standby` Services route to
them. Losing a single OpenBao pod triggers a leader election and the remaining two voters keep quorum.

**There is no node-level availability.** The Kubernetes cluster has exactly one node, `node1`, and all
three voters are scheduled on it, as are both snapshot Jobs. The node is therefore the entire failure
domain: losing it loses all three voters, both PVC sets, and the snapshot volume at once. A voter count
of three does not change that, and this document does not present it as if it did.

Recovery from node loss depends on the `openbao-raft-snapshot` CronJob's output surviving the node,
which today it does not — the `openbao-snapshots` PVC is `openebs-hostpath-retain` on the same node. An
off-cluster copy of those snapshots is still required and is not yet in place.

## Owned manifests

These four files are the checked-in, non-secret inputs to the platform. Each section below binds the
file to what the platform actually runs.

### `deploy/openbao/values.yaml`

The Helm values for chart `0.28.5`. Story 31.1 reconciled this file to the deployed release; before that
it declared a single standalone voter with HA disabled, which the running platform contradicted in every
one of those settings.

| Declared setting | Deployed value it binds |
| :--------------- | :---------------------- |
| `fullnameOverride: hexalith-keys` | Helm release, StatefulSet, and Service names |
| `server.image.tag` | `2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653` — digest-pinned |
| `global.tlsDisable: false` | listener runs TLS with `tls_min_version = "tls12"` |
| `server.standalone.enabled: false` | the standalone single-voter path is off |
| `server.ha.enabled: true` | `bao status` reports `ha_enabled: true` |
| `server.ha.replicas: 3` | StatefulSet `.spec.replicas = 3` |
| `server.ha.raft.enabled: true` | `storage "raft"` with `retry_join`, `storage_type: raft` |
| `server.ha.raft.setNodeId: true` | container env `BAO_RAFT_NODE_ID` from `metadata.name` |
| `server.service.type: ClusterIP` | all four Services are `ClusterIP`; no ingress exists |
| `server.service.active.enabled: true` | Service `hexalith-keys-active` |
| `server.service.standby.enabled: true` | Service `hexalith-keys-standby` |
| `server.serviceAccount.serviceDiscovery.enabled: true` | Role `hexalith-keys-discovery-role` and its RoleBinding, required by `service_registration "kubernetes"` |
| `server.authDelegator.enabled: true` | ClusterRoleBinding `hexalith-keys-server-binding` to `system:auth-delegator` |
| `server.dataStorage.size: 10Gi`, `server.auditStorage.size: 10Gi` | one `data` and one `audit` volumeClaimTemplate, materialized once per replica — six 10Gi PVCs at three replicas |
| `server.networkPolicy.ingress` | port 8200 from `hexalith-memories` and `cert-manager`; 8200 and 8201 from the OpenBao pods |
| `server.ha.raft.config` seal stanza | `seal "static"` reading `file:///openbao/userconfig/openbao-seal/current.key` |
| `server.ha.raft.config` audit stanza | `audit "file" "persistent"` at `/openbao/audit/openbao-audit.json` |
| `server.statefulSet.securityContext` | the Restricted-compatible pod and container security contexts on the running pods |
| `injector.enabled: false`, `csi.enabled: false`, `ui.enabled: false` | no Agent Injector, no CSI provider, no web UI |

### `deploy/openbao/namespace.yaml`

Creates namespace `openbao` with the Restricted Pod Security profile and the two ownership annotations.
This file is exact as deployed and Story 31.1 changed nothing in it.

| Declared setting | Deployed value it binds |
| :--------------- | :---------------------- |
| `metadata.name: openbao` | the namespace the whole platform runs in |
| `pod-security.kubernetes.io/enforce: restricted` | enforced Restricted profile, version `latest` |
| `pod-security.kubernetes.io/audit: restricted` | audited Restricted profile, version `latest` |
| `pod-security.kubernetes.io/warn: restricted` | warned Restricted profile, version `latest` |
| `hexalith.io/platform-owner: jpiquot` | Platform Operations owner annotation |
| `hexalith.io/security-reviewer: murat-tea-for-jpiquot` | security reviewer annotation |

The namespace also carries five profile-identity annotations that this file does **not** declare —
`hexalith.io/composite-profile-sha256`, `hexalith.io/helm-manifest-sha256`,
`hexalith.io/hardening-manifest-sha256`, `hexalith.io/deployment-id`, and `hexalith.io/profile-id`. They
are applied by the production deployment renderer, not by this manifest. See
[Named divergences](#named-divergences) for what they are and are not evidence of.

### `deploy/openbao/service-account-hardening.yaml`

Applied after each Helm install or upgrade. It sets `automountServiceAccountToken: false` on the
`hexalith-keys` ServiceAccount, and the deployed ServiceAccount does read `false`.

It does not stop the server from receiving a Kubernetes API token. The running pod spec sets
`automountServiceAccountToken: true`, and the pod-level setting overrides the ServiceAccount default. The
server therefore holds an API token and uses it, because `service_registration "kubernetes"` needs one to
keep the `-active` and `-standby` endpoints correct.

| Declared setting | Deployed value it binds |
| :--------------- | :---------------------- |
| `kind: ServiceAccount`, `name: hexalith-keys` | the ServiceAccount the OpenBao pods run as |
| `namespace: openbao` | the platform namespace |
| `automountServiceAccountToken: false` | the ServiceAccount default only — measured `false` on the ServiceAccount, overridden to `true` at pod level |

What this file still achieves is denying an automounted token to any other pod that runs as this
ServiceAccount without requesting one in its own spec. Do not read it as evidence that the server holds
no API token; before Story 31.1 this document made exactly that claim and it was false as deployed.

### `deploy/openbao/smoke-test.yaml`

A Restricted-compatible Job that runs `bao status -format=json` against the Service endpoint. It exists
because chart `0.28.5`'s built-in Helm test hook does not declare the security context this namespace's
enforced Restricted profile requires. Do not weaken the namespace policy to run that upstream hook.

| Declared setting | Deployed value it binds |
| :--------------- | :---------------------- |
| `kind: Job`, `name: hexalith-keys-smoke-test` | the Job created by [Smoke test](#smoke-test) |
| `BAO_ADDR: https://hexalith-keys.openbao.svc.cluster.local:8200` | the ClusterIP Service endpoint |
| `BAO_CACERT: /openbao/tls/ca.crt` | CA from Secret `openbao-server-tls`, mounted read-only |
| `automountServiceAccountToken: false` | the Job needs no Kubernetes API access |
| `runAsNonRoot: true`, `allowPrivilegeEscalation: false`, capabilities `- ALL` dropped | Restricted profile compliance |
| `backoffLimit: 0`, `activeDeadlineSeconds: 60`, `ttlSecondsAfterFinished: 300` | one attempt, one minute, reaped five minutes after finishing |
| image digest `sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653` | the same pinned image the server runs |

## Smoke test

Run it with exactly these commands:

```bash
kubectl apply -f deploy/openbao/smoke-test.yaml
kubectl -n openbao wait --for=condition=complete job/hexalith-keys-smoke-test --timeout=2m
kubectl -n openbao logs job/hexalith-keys-smoke-test
```

Capture the logs before `ttlSecondsAfterFinished: 300` reaps the Job. Because `backoffLimit: 0` allows
no retry, a re-run requires deleting the prior Job first with
`kubectl -n openbao delete job hexalith-keys-smoke-test`.

### Recorded result

Executed **2026-07-28T09:43:31Z** to **2026-07-28T09:43:38Z** against context `jpiquot@local`, namespace
`openbao`. The Job reached `condition=complete`. Observed fields:

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

## Named divergences

Open items where this document cannot assert a re-derived fact. Each carries an owner and a reopen
trigger; none is presented above as measured.

| Divergence | Owner | Why it is open | Reopen trigger |
| :--------- | :---- | :------------- | :------------- |
| TLS certificate expiry `2027-08-20 13:38:39 UTC` | Hexalith Platform Operations (`jpiquot`) | Carried forward from the 2026-07-19 platform bootstrap. Re-deriving it requires reading `openbao-server-tls`, which this story's secret-read prohibition forbids, and no cert-manager `Certificate` resource exists in the namespace to report it | A Platform Operations capture of the certificate `notAfter`, or adoption of a cert-manager `Certificate` that publishes `status.notAfter` |
| Operator and Dapr token expiry `2027-07-19 13:41:25 UTC` | Hexalith Platform Operations (`jpiquot`) | Carried forward from the same bootstrap. Re-deriving it requires reading `openbao-operator-credentials` | A Platform Operations token-accessor listing that reports the TTL without printing a token |
| Namespace profile annotations do not reject drift | Hexalith Platform Operations (`jpiquot`) | The `hexalith.io/composite-profile-sha256` and `hexalith.io/deployment-id` annotations still read the values written for release `r2`, while the platform advanced across nine Helm revisions. An evidence capture keyed on them would have accepted every measured change as no-drift | A drift check that compares live object state rather than a stored annotation, or a renderer that rewrites the annotations on every applied revision |
| `cert-manager` NetworkPolicy rule has no matching consumer | Hexalith Platform Operations (`jpiquot`) | The policy admits every `cert-manager` pod on 8200, but no cert-manager `Certificate` or `Issuer` exists in namespace `openbao` to justify it | Establishing the cert-manager consumer, or removing the rule from `deploy/openbao/values.yaml` |
| Reconciled `values.yaml` has not been re-applied | Hexalith Platform Operations (`jpiquot`) | Story 31.1 reconciled the file to the measured release but ran no `helm upgrade`. The file now describes the platform; it has not been proven to reproduce it | A Platform Operations `helm diff`/`helm upgrade --dry-run` against release `hexalith-keys` confirming an empty diff |

The three manifest hashes recorded on both namespaces are
`1deba6e0456bb44ea0624a0f436b209b5ede2c496cc9be98fea5b9dbee1db539` (application manifest),
`f55ff3c237fad5047d6ad7d19a56a83c6546a2f31fc5830022bc3b3a51c9c8e3` (Helm manifest), and
`f3fe70b98c64ec9072bc3ab54fd07ffdde1e4d4550c53d8f20e2bb58eb70f3eb` (OpenBao namespace plus
service-account hardening), composing to `4183b741eac062d962a8ff1860a7aa049719a75f47e38e6fdcfb0fe1aeaa5d45`.
They identify the release the annotations were written for. They are not a drift detector.

## Accepted limitations

Both limitations below are accepted for this deployed profile and are open by design, not defects to be
closed silently. Each has an owner, a stated consequence, the compensating controls that actually exist
today, and a trigger that reopens it.

| Limitation | Owner | Consequence | Compensating controls | Reopen trigger |
| :--------- | :---- | :---------- | :-------------------- | :------------- |
| Static file-based seal | Hexalith Platform Operations (`jpiquot`) with security reviewer `murat-tea-for-jpiquot` | The seal key is a file in Secret `openbao-seal`, in namespace `openbao`, beside the `data-hexalith-keys-0..2` PVCs it decrypts. One namespace-level read yields both the ciphertext and the key, so the storage encryption stops any attacker who cannot read the namespace and stops no attacker who can | Restricted Pod Security enforced on the namespace; RBAC scoped to the platform ServiceAccounts; all four Services `ClusterIP` with no ingress; NetworkPolicy restricting port 8200; persistent JSON audit device recording access; `shamir` recovery seal with a 2-of-3 share threshold | Migrating `seal "static"` to an external KMS or HSM-backed seal, so the key stops living beside the data |
| Namespace-wide port 8200 ingress | Hexalith Platform Operations (`jpiquot`) with security reviewer `murat-tea-for-jpiquot` | The NetworkPolicy uses a `namespaceSelector` with no `podSelector`, so every pod in `hexalith-memories` and every pod in `cert-manager` may reach 8200 — 10 pods at measurement, including `redis-stack-0`, `falkordb-0`, `access-telemetry-postgresql-0`, and both `memories-mcp` pods, which architecture D31 scopes to receive neither secret component | Dapr token authentication in front of every secret read; per-component OpenBao policies (`hexalith-memories-runtime` and `hexalith-memories-access-telemetry`, both read-only) scoping what a reachable caller may fetch; mandatory TLS verification with `skipVerify: "false"`; persistent audit device recording every request | Narrowing the selector to a `podSelector` covering only the pods that actually consume a Dapr secret component |

### Static file-based seal

The deployed seal stanza is `seal "static"` with `current_key_id = "kubernetes-openbao-seal-v1"` and
`current_key = file:///openbao/userconfig/openbao-seal/current.key`. The file is mounted read-only from
Secret `openbao-seal` in namespace `openbao` — the same namespace that holds the `data-hexalith-keys-0..2`
PVCs containing the encrypted Raft store.

The three voters do not change this. Each voter reads the same seal key from the same Secret, so the
number of copies of the ciphertext went up while the number of places the key lives stayed at one.

This is the accepted trade for running an in-cluster secret store without an external key manager. It is
adequate for a single-operator platform where namespace access is already equivalent to platform
administration, and it is not adequate for a service whose threat model includes an attacker with
namespace-level read. Migrating to an external KMS or HSM-backed seal is the reopen trigger, and it is
the single change that most improves this platform's security posture.

### Namespace-wide port 8200 ingress

The deployed NetworkPolicy has two ingress rules. The first admits port 8200 from two
`namespaceSelector` sources, `hexalith-memories` and `cert-manager`, with no `podSelector` on either. A
`namespaceSelector` without a `podSelector` matches every pod in the namespace, so the rule admits all of
them. The second rule admits ports 8200 and 8201 from the OpenBao pods themselves, which is Raft cluster
traffic and is correctly scoped.

At measurement the first rule covered 10 pods: seven in `hexalith-memories` — `access-telemetry-postgresql-0`,
`falkordb-0`, `redis-stack-0`, `memories-b667844cf-6s9j7`, `memories-b667844cf-bs4gm`,
`memories-mcp-8fd85c7c9-422vf`, `memories-mcp-8fd85c7c9-vf7n9` — and three in `cert-manager`. Per
architecture decision D31 the MCP sidecar is scoped to receive neither secret component, and the three
data backends consume no Dapr secret component at all, so most of that reachability serves no consumer.

Network reachability is not authorization: a reachable caller still needs a Dapr token and a scoped
OpenBao policy to read anything. The limitation is that the network layer contributes no defence in
depth here. The reopen trigger is narrowing the selector to a `podSelector` covering only the pods that
consume a Dapr secret component.

## Dapr secret boundaries

Dapr uses OpenBao through component type `secretstores.hashicorp.vault`, which is the component type Dapr
documents for OpenBao compatibility; there is no separate OpenBao component type. TLS verification is
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

## Health and access checks

Run checks without printing secret values:

```bash
kubectl -n openbao get pod,service,pvc,networkpolicy
kubectl -n openbao exec hexalith-keys-0 -- \
  env BAO_CACERT=/openbao/userconfig/openbao-server-tls/ca.crt \
  bao status -format=json \
  | jq '{initialized,sealed,storage_type,ha_enabled,version}'
kubectl -n openbao logs hexalith-keys-0 --since=30m \
  | jq -r 'select(."@level" == "error")'
kubectl -n hexalith-memories get component secretstore access-telemetry-secrets -o yaml
kubectl apply -f deploy/openbao/service-account-hardening.yaml
```

Expected status is `initialized: true`, `sealed: false`, `storage_type: "raft"`, `ha_enabled: true`, and
OpenBao `2.6.0`. The StatefulSet must be `3/3` Ready, all six data and audit PVCs must be `Bound`, and
every Service must remain `ClusterIP`.

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
- [Dapr HashiCorp Vault secret store](https://docs.dapr.io/reference/components-reference/supported-secret-stores/hashicorp-vault/)
- [Architecture D31 — OpenBao-first DAPR secret provider](../../_bmad-output/planning-artifacts/architecture.md#d31--openbao-first-dapr-secret-provider)
- [Story 31.1 platform evidence](../../_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md)
