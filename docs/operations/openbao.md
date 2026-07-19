# Hexalith Keys OpenBao operations

`hexalith-keys` is the internal OpenBao server used by the production-shaped Kubernetes target. Platform
Operations owner: **Jérôme Piquot (`jpiquot`)**. Security reviewer: **Murat TEA for Jérôme**.

## Deployed profile

| Contract | Value |
| :------- | :---- |
| Kubernetes context | `jpiquot@local` |
| Application namespace | `hexalith-memories` |
| Namespace | `openbao` |
| Deployment ID | `memories-production-2.11.0-c7c2ca21-hexalith-keys-r2` |
| Profile ID | `redis-state-v1-dapr-1.18.1-openbao-2.6.0-4183b741eac062d9` |
| Evidence root | `_bmad-output/implementation-artifacts/tests` |
| Declared single-component fault | `dapr-sidecar-restart` |
| Helm release, Service, StatefulSet | `hexalith-keys` |
| Chart | official `oci://ghcr.io/openbao/charts/openbao`, `0.28.5`, digest `sha256:1c2e01185430b9bc426da870909fdccfbb4e3e4758f0c6f8cccfbceead4381ff` |
| Server image | `quay.io/openbao/openbao:2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653` |
| Endpoint | `https://hexalith-keys.openbao.svc.cluster.local:8200` (ClusterIP only) |
| Storage | single-node integrated Raft, retained `10Gi` data PVC |
| Audit | persistent JSON file device, retained `10Gi` audit PVC |
| TLS certificate expiry | 2027-08-20 13:38:39 UTC |
| Operator and Dapr token expiry | 2027-07-19 13:41:25 UTC |

The pinned application manifest hash is
`1deba6e0456bb44ea0624a0f436b209b5ede2c496cc9be98fea5b9dbee1db539`, the Helm manifest hash is
`f55ff3c237fad5047d6ad7d19a56a83c6546a2f31fc5830022bc3b3a51c9c8e3`, and the OpenBao namespace plus
service-account-hardening manifest hash is
`f3fe70b98c64ec9072bc3ab54fd07ffdde1e4d4550c53d8f20e2bb58eb70f3eb`. Their ordered composite profile
hash is `4183b741eac062d962a8ff1860a7aa049719a75f47e38e6fdcfb0fe1aeaa5d45`. Both namespaces carry the
profile identity as annotations so an evidence capture can reject drift before running a probe.

The checked-in, non-secret inputs are [`deploy/openbao/namespace.yaml`](../../deploy/openbao/namespace.yaml),
[`deploy/openbao/values.yaml`](../../deploy/openbao/values.yaml), and the Restricted-compatible
[`deploy/openbao/smoke-test.yaml`](../../deploy/openbao/smoke-test.yaml). Apply
[`deploy/openbao/service-account-hardening.yaml`](../../deploy/openbao/service-account-hardening.yaml) after
each Helm install/upgrade so the server does not receive an unused Kubernetes API token. TLS private
material, the static seal key, recovery shares, operator credentials, and Dapr tokens exist only in
Kubernetes Secrets and must not be copied into evidence or source control.

This is a production-shaped **single-server** deployment because the target Kubernetes cluster has one
node. It provides encrypted persistent storage and restart recovery, but not server or node high
availability. A multi-node production cluster must replace it with at least three OpenBao Raft voters and
an external KMS/HSM-backed seal.

## Dapr secret boundaries

Dapr uses OpenBao through component type `secretstores.hashicorp.vault`, which is the component type Dapr
documents for OpenBao compatibility. TLS verification is mandatory (`skipVerify: "false"`) and bootstraps
from narrowly RBAC-scoped Kubernetes Secrets:

| Dapr component | Bootstrap Secret | OpenBao prefix | Policy |
| :------------- | :--------------- | :------------- | :----- |
| `secretstore` | `openbao-runtime-bootstrap` | `secret/hexalith/memories/runtime` | `hexalith-memories-runtime` (read only) |
| `access-telemetry-secrets` | `openbao-access-telemetry-bootstrap` | `secret/hexalith/memories/access-telemetry` | `hexalith-memories-access-telemetry` (read only) |

The bootstrap Secrets contain only a scoped OpenBao token and the internal CA certificate. Dapr explicitly
recommends using a local secret store such as Kubernetes for this bootstrap credential.

OpenBao replaces Kubernetes as the provider for secrets resolved by Dapr components. It does not inject
environment variables into application or database containers. Therefore `redis-secret`,
`app-api-token`, `dapr-api-token`, clock keys, and similar direct pod inputs remain Kubernetes Secrets;
deleting those copies breaks pod startup. Adopting OpenBao Agent Injector or CSI for those values is a
separate deployment change.

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
kubectl apply -f deploy/openbao/smoke-test.yaml
kubectl -n openbao wait --for=condition=complete job/hexalith-keys-smoke-test --timeout=2m
```

Expected status is `initialized: true`, `sealed: false`, `storage_type: "raft"`, and OpenBao `2.6.0`.
The StatefulSet must be `1/1` Ready, both PVCs must be `Bound`, and the Service must remain `ClusterIP`.
Use the checked-in smoke-test Job because chart `0.28.5`'s built-in Helm test pod does not declare the
security context required by this namespace's enforced Restricted Pod Security profile. Do not weaken the
namespace policy to run that upstream hook.

## Rotation and recovery

Platform Operations must rotate the operator and Dapr tokens before **2027-07-19 13:41:25 UTC**, update
the two bootstrap Secrets, and restart the Dapr workloads because actor-state hot reload is disabled.
Rotate the server certificate before **2027-08-20 13:38:39 UTC**. Use token accessors from
`openbao/openbao-operator-credentials` to revoke superseded tokens; never log token IDs.

The `openbao-operator-credentials` Secret contains three recovery shares with a 2-of-3 threshold and the
non-root operator identity. The initial root token was revoked and is not retained. For a real production
service, export the shares into independent security-controlled escrow and remove them from the cluster
after the recovery ceremony is proven.

Do not delete `data-hexalith-keys-0` or `audit-hexalith-keys-0`. The StatefulSet PVC retention policy and
the `openebs-hostpath-retain` StorageClass retain them, but loss of the single node still requires an
off-cluster Raft snapshot and recovery plan.

## References

- [OpenBao Kubernetes deployment](https://openbao.org/docs/platform/k8s/)
- [OpenBao Helm production checklist](https://openbao.org/docs/platform/k8s/helm/run/)
- [Dapr OpenBao secret store](https://docs.dapr.io/reference/components-reference/supported-secret-stores/openbao/)
- [Dapr Vault-compatible component metadata](https://docs.dapr.io/reference/components-reference/supported-secret-stores/hashicorp-vault/)
