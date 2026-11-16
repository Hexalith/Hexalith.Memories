# Story 31.1 — EVAL-01 cluster-side source discovery

Prepared 2026-11-17 Europe/Paris; measurements use UTC on 2026-11-16.
Author: Codex, AI assistant. Direction: “get it in 192.168.1.30 withkubectl”.
Source revision: `341ae091bc9a94bfab0c7741314419713188ae8d`.

This addendum records read-only discovery, not a deployment or independent sign-off.
All earlier attested reports and the 2026-11-16 preparation addendum retain their bytes.
C4b/C5b/C7 remain pending / not complete without completion dates; Story 31.1 remains
in-progress. EVAL-01's certificate correction has not been deployed.

## Confirmed endpoint and ownership

`kubectl config view` confirms context `jpiquot@local` maps to cluster `local`,
API endpoint `https://192.168.1.30:6443`. Node `node1` has internal IP `192.168.1.30`.
This is the same cluster used for the earlier preflight.

Fourteen projected discovery queries completed successfully during
**2026-11-16T23:42:33.582601+00:00–2026-11-16T23:42:35.610886+00:00**.
The [JSON companion](31-1-eval-01-cluster-source-discovery-2026-11-17.json) retains
their exact argument arrays, UTC timestamps and selected output. They inspect
certificate/issuer references, existing configuration ownership, Secret names/types,
application release names and API resource types. No Secret payload was retrieved
for display, copied or decoded. No workload, image, HCL, TLS Secret or process changed.

The live HCL ConfigMap and StatefulSet report Helm ownership plus historical
`kubectl-client-side-apply` / `kubectl-patch` managers. These manager identities and
timestamps do not identify an external certificate issuance repository or procedure.
No Certificate in the cluster inventory targets `openbao/openbao-server-tls`, and
no namespaced Issuer exists in `openbao`. The namespace contains the known
`openbao-server-tls` and `hexalith-keys-pki` Secrets, plus bootstrap/release metadata
Secrets; only names and types were read.

## Discovered application PKI

The cluster does have a Ready `ClusterIssuer/openbao-internal-pki`:

| Field | Observed value |
|---|---|
| Server | `https://hexalith-keys.openbao.svc.cluster.local:8200` |
| Signing path | `pki-int/sign/hexalith-memories-services` |
| Authentication | Kubernetes mount `/v1/auth/kubernetes`, role `cert-manager-internal-pki` |
| ServiceAccount reference | `cert-manager/openbao-pki-issuer` |
| Transport CA reference | Secret `openbao-client-ca`, key `ca.crt`; contents not read |
| Current consumer | Certificate `hexalith-memories/access-telemetry-postgresql` |

An initial attempt to inspect an inline `spec.vault.caBundle` found it empty;
OpenSSL decoding exited 1. It established no public CA fingerprint. Subsequent
reference inspection found `caBundleSecretRef`; the referenced Secret was not read.
Both attempts remain in the structured evidence without a fabricated success.

At **2026-11-16T23:41:12.442090+00:00–2026-11-16T23:41:12.848908+00:00**, a
credential-free `bao read pki-int/cert/ca` through the existing mounted CA and explicit
`-tls-skip-verify=false` succeeded. The environment was cleared and HOME absent.
Only the public certificate was decoded in memory; no operator token or Secret read
was used. Returned public metadata:

- Subject: `CN=Hexalith Internal Intermediate CA`.
- Issuer: `CN=Hexalith Internal Root CA, O=Hexalith`.
- Certificate SHA-256 fingerprint:
  `9062ceea14026bce65d00bff3bd3a4147222daa9ff6b8b209cf7bd087ea8361b`.

The prior listener inventory reports issuer `CN=Hexalith Keys Internal CA, O=Hexalith`.
This is a different CA identity. Discovering a working application issuer does not
establish that its signing path can reproduce the current server certificate under
the intended unchanged CA. No server signing role, issuance authorization or original
CA custody is inferred, and no signing operation was attempted.

No KubeSphere `ApplicationRelease` or `app.k8s.io` Application objects were returned.
API discovery exposed no Flux, Argo CD or External Secrets resource types. These
bounded observations do not exclude host-local, external or historical automation.

## Remaining source gap and proposed next read

The cluster discovery has not supplied an authoritative issuing/renewal source or
safe install/restore procedure for the current shared server certificate. The existing
[rollout/rollback proposal](../../../docs/operations/openbao-peer-tls-eval-01.md)
therefore remains unapproved and incomplete at its custodian step.

The inherited story permits `kubectl get secret` for **names and types only**. No
Secret annotations, ownerReferences or managedFields were inspected. If the owner
explicitly permits a narrow metadata exception, the next proposed read is:

```bash
for name in openbao-server-tls hexalith-keys-pki; do
  kubectl --context=jpiquot@local --request-timeout=12s -n openbao \
    get secret "$name" -o jsonpath='{.metadata.name}{"\n"}{range .metadata.ownerReferences[*]}{.kind}{" "}{.name}{"\n"}{end}{range .metadata.managedFields[*]}{.manager}{" "}{.operation}{" "}{.time}{"\n"}{end}'
done
```

This command has **not been executed**. It selects only names, owner kind/name and
manager/operation/time. It excludes `.data`, `.stringData`, annotations (which may
embed an entire last-applied Secret), field ownership payloads, keys and credentials.
Its results may identify a controller or bootstrap manager; they may still leave
the external procedure unresolved. This proposed exception grants no certificate
issuance, Secret write, process reload or rollout approval.

The companion records hashes of the fourteen earlier protected inputs and both
preparation-addendum files. All fourteen original preservation checks passed.
No new post-rollout verification is asserted; its structured field remains null.
