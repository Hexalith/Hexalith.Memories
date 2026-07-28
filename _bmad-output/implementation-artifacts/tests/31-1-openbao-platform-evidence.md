# Story 31.1 — OpenBao platform evidence

Story: `31-1-openbao-platform-hardening-and-documentation`
Baseline commit: `327d1a9d7eaef063c656a6af9df4eea84f47ca30`
Cluster context: `jpiquot@local` · Namespace: `openbao` · Application namespace: `hexalith-memories`
Measured by: Memories Maintainer (dev-story) on **2026-07-28**

No secret value is recorded in this artifact. `kubectl get secret` was used for names and types only.
The contents of `openbao-seal`, `openbao-operator-credentials`, `openbao-server-tls`, and
`hexalith-keys-pki` were never read, printed, or stored.

## 1. Re-measurement transcript (Task 1)

Every probe below was re-run at implementation time. The creation-time measurements dated 2026-07-28 in
the story file were treated as a starting point only, not as evidence.

### 1.1 Node topology

```text
$ kubectl config current-context
jpiquot@local

$ kubectl get nodes -o wide
NAME    STATUS   ROLES                  AGE   VERSION   INTERNAL-IP    OS-IMAGE             CONTAINER-RUNTIME
node1   Ready    control-plane,worker   11d   v1.34.9   192.168.1.30   Ubuntu 24.04.3 LTS   containerd://2.3.3
```

**One node.** Confirms the amended AC2 premise: the hosting and the failure domain are single-node.

### 1.2 Pods and scheduling

```text
$ kubectl -n openbao get pods -o custom-columns=NAME:.metadata.name,NODE:.spec.nodeName,STATUS:.status.phase
NAME                                   NODE    STATUS
hexalith-keys-0                        node1   Running
hexalith-keys-1                        node1   Running
hexalith-keys-2                        node1   Running
openbao-raft-snapshot-29751750-5clkh   node1   Succeeded
openbao-raft-snapshot-29753190-lgh85   node1   Succeeded
```

All three Raft voters and both snapshot Jobs are co-located on `node1`.

### 1.3 Workload, services, volumes, policy

```text
$ kubectl -n openbao get statefulset hexalith-keys -o jsonpath='{.spec.replicas}'
3

$ kubectl -n openbao get pod,service,pvc,networkpolicy
pod/hexalith-keys-0                        1/1     Running     0    7d13h
pod/hexalith-keys-1                        1/1     Running     0    7d20h
pod/hexalith-keys-2                        1/1     Running     0    7d20h
pod/openbao-raft-snapshot-29751750-5clkh   0/1     Completed   0    35h
pod/openbao-raft-snapshot-29753190-lgh85   0/1     Completed   0    11h

service/hexalith-keys            ClusterIP   10.233.41.33   8200/TCP,8201/TCP   8d
service/hexalith-keys-active     ClusterIP   10.233.18.54   8200/TCP,8201/TCP   7d20h
service/hexalith-keys-internal   ClusterIP   None           8200/TCP,8201/TCP   8d
service/hexalith-keys-standby    ClusterIP   10.233.62.69   8200/TCP,8201/TCP   7d20h

persistentvolumeclaim/audit-hexalith-keys-0   Bound   10Gi   RWO   openebs-hostpath-retain   8d
persistentvolumeclaim/audit-hexalith-keys-1   Bound   10Gi   RWO   openebs-hostpath-retain   7d20h
persistentvolumeclaim/audit-hexalith-keys-2   Bound   10Gi   RWO   openebs-hostpath-retain   7d20h
persistentvolumeclaim/data-hexalith-keys-0    Bound   10Gi   RWO   openebs-hostpath-retain   8d
persistentvolumeclaim/data-hexalith-keys-1    Bound   10Gi   RWO   openebs-hostpath-retain   7d20h
persistentvolumeclaim/data-hexalith-keys-2    Bound   10Gi   RWO   openebs-hostpath-retain   7d20h
persistentvolumeclaim/openbao-snapshots       Bound    2Gi   RWO   openebs-hostpath-retain   7d20h

networkpolicy.networking.k8s.io/hexalith-keys   app.kubernetes.io/instance=hexalith-keys,app.kubernetes.io/name=openbao   8d

$ kubectl -n openbao get sts hexalith-keys -o jsonpath='{range .spec.volumeClaimTemplates[*]}{.metadata.name}:{.spec.resources.requests.storage}:{.spec.storageClassName}{"\n"}{end}'
data:10Gi:openebs-hostpath-retain
audit:10Gi:openebs-hostpath-retain

$ kubectl -n openbao get sts hexalith-keys -o jsonpath='{.spec.persistentVolumeClaimRetentionPolicy}'
{"whenDeleted":"Retain","whenScaled":"Retain"}

$ kubectl -n openbao get sts hexalith-keys -o jsonpath='{.spec.podManagementPolicy}|{.spec.updateStrategy.type}|{.spec.serviceName}'
OrderedReady|OnDelete|hexalith-keys-internal
```

Six 10Gi PVCs from two volumeClaimTemplates at three replicas, plus the 2Gi snapshot PVC.

### 1.4 Deployed server configuration

```text
$ kubectl -n openbao get cm hexalith-keys-config -o jsonpath='{.data}'
```

Rendered `extraconfig-from-values.hcl`:

```hcl
ui = false
default_lease_ttl = "168h"
max_lease_ttl = "8760h"

listener "tcp" {
  address = "[::]:8200"
  cluster_address = "[::]:8201"
  tls_disable = 0
  tls_cert_file = "/openbao/userconfig/openbao-server-tls/tls.crt"
  tls_key_file = "/openbao/userconfig/openbao-server-tls/tls.key"
  tls_min_version = "tls12"
}

storage "raft" {
  path = "/openbao/data"

  retry_join {
    leader_api_addr = "https://hexalith-keys-0.hexalith-keys-internal.openbao.svc.cluster.local:8200"
    leader_ca_cert_file = "/openbao/userconfig/openbao-server-tls/ca.crt"
    leader_tls_servername = "hexalith-keys-0.hexalith-keys-internal.openbao.svc.cluster.local"
  }
}

seal "static" {
  current_key_id = "kubernetes-openbao-seal-v1"
  current_key = "file:///openbao/userconfig/openbao-seal/current.key"
}

audit "file" "persistent" {
  description = "Persistent JSON audit trail"
  options {
    file_path = "/openbao/audit/openbao-audit.json"
    format = "json"
    hmac_accessor = "true"
    mode = "0600"
  }
}

service_registration "kubernetes" {}
```

No `node_id` in the rendered config; it arrives through the environment instead:

```text
$ kubectl -n openbao get sts hexalith-keys -o jsonpath='{range .spec.template.spec.containers[0].env[*]}{.name}={.value}{.valueFrom.fieldRef.fieldPath}{"\n"}{end}'
BAO_API_ADDR=https://$(POD_IP):8200
BAO_CLUSTER_ADDR=https://$(HOSTNAME).hexalith-keys-internal:8201
BAO_RAFT_NODE_ID=metadata.name
```

### 1.5 NetworkPolicy and its blast radius

```text
$ kubectl -n openbao get networkpolicy hexalith-keys -o jsonpath='{.spec}' | jq .
ingress[0].from = [ namespaceSelector kubernetes.io/metadata.name=hexalith-memories,
                    namespaceSelector kubernetes.io/metadata.name=cert-manager ]
ingress[0].ports = [ 8200/TCP ]
ingress[1].from = [ podSelector app.kubernetes.io/instance=hexalith-keys,
                                app.kubernetes.io/name=openbao ]
ingress[1].ports = [ 8200/TCP, 8201/TCP ]
podSelector = app.kubernetes.io/instance=hexalith-keys, app.kubernetes.io/name=openbao
policyTypes = [ Ingress ]
```

Neither `namespaceSelector` carries a `podSelector`, so rule 0 admits every pod in both namespaces:

```text
$ kubectl -n hexalith-memories get pods --no-headers -o custom-columns=NAME:.metadata.name
access-telemetry-postgresql-0
falkordb-0
memories-b667844cf-6s9j7
memories-b667844cf-bs4gm
memories-mcp-8fd85c7c9-422vf
memories-mcp-8fd85c7c9-vf7n9
redis-stack-0

$ kubectl -n cert-manager get pods --no-headers -o custom-columns=NAME:.metadata.name
cert-manager-845566d58f-mx2vt
cert-manager-cainjector-7f544f8d56-vmmlm
cert-manager-webhook-6dccb4dc87-xrtvd
```

**10 pods** admitted on port 8200. No cert-manager `Certificate` or `Issuer` exists in namespace
`openbao` (`kubectl -n openbao get certificate,issuer` → `No resources found`), so the cert-manager rule
has no matching consumer.

### 1.6 ServiceAccount token reality and RBAC

```text
$ kubectl -n openbao get sa hexalith-keys -o jsonpath='{.automountServiceAccountToken}'
false

$ kubectl -n openbao get pod hexalith-keys-0 -o jsonpath='{.spec.automountServiceAccountToken}|{.spec.serviceAccountName}|{.status.containerStatuses[0].imageID}'
true|hexalith-keys|quay.io/openbao/openbao@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653

$ kubectl -n openbao get role,rolebinding
role.rbac.authorization.k8s.io/hexalith-keys-discovery-role                 2026-07-20T13:12:19Z
rolebinding.rbac.authorization.k8s.io/hexalith-keys-discovery-rolebinding   Role/hexalith-keys-discovery-role

$ kubectl -n openbao get role hexalith-keys-discovery-role -o jsonpath='{.rules}'
[{ apiGroups:[""], resources:["pods"], verbs:["get","watch","list","update","patch"] }]
```

The pod-level `true` overrides the ServiceAccount default `false`. The server holds and uses an API
token, which is what `service_registration "kubernetes"` requires.

Two ClusterRoleBindings bind this ServiceAccount to `system:auth-delegator`:

```text
hexalith-keys-server-binding  -> Helm-managed (chart openbao-0.28.5, release hexalith-keys)
hexalith-keys-tokenreview     -> applied with kubectl, labels app.kubernetes.io/part-of=hexalith-memories
```

The first is chart-rendered by `server.authDelegator.enabled: true`. The second is untracked duplicate
platform state.

### 1.7 Backup

```text
$ kubectl -n openbao get cronjob,job
cronjob.batch/openbao-raft-snapshot   30 0 * * *   False   0   11h   7d20h
job.batch/openbao-raft-snapshot-29751750   Complete   1/1   4s   35h
job.batch/openbao-raft-snapshot-29753190   Complete   1/1   4s   11h

$ kubectl -n openbao get cronjob openbao-raft-snapshot -o jsonpath='{.spec.schedule}|{.spec.successfulJobsHistoryLimit}|{.spec.failedJobsHistoryLimit}|{.spec.concurrencyPolicy}|{.spec.jobTemplate.spec.template.spec.serviceAccountName}'
30 0 * * *|2|3|Forbid|openbao-snapshot
```

Snapshot output lands on PVC `openbao-snapshots`, which is `openebs-hostpath-retain` on `node1` — the
same node as the data it protects.

### 1.8 Secret inventory (names and types only)

```text
$ kubectl -n openbao get secret --no-headers -o custom-columns=NAME:.metadata.name,TYPE:.type
hexalith-keys-pki                     Opaque
openbao-operator-credentials          Opaque
openbao-seal                          Opaque
openbao-server-tls                    kubernetes.io/tls
sh.helm.release.v1.hexalith-keys.v1   helm.sh/release.v1
...
sh.helm.release.v1.hexalith-keys.v9   helm.sh/release.v1
```

**Nine Helm release revisions.** `hexalith-keys-pki` is present and undocumented; its contents were not
read.

### 1.9 Namespace labels and profile annotations

```text
$ kubectl get ns openbao -o jsonpath='{.metadata.labels}{"\n"}{.metadata.annotations}'
labels:
  kubernetes.io/metadata.name=openbao
  pod-security.kubernetes.io/enforce=restricted   (+ enforce-version=latest)
  pod-security.kubernetes.io/audit=restricted     (+ audit-version=latest)
  pod-security.kubernetes.io/warn=restricted      (+ warn-version=latest)
annotations:
  hexalith.io/platform-owner=jpiquot
  hexalith.io/security-reviewer=murat-tea-for-jpiquot
  hexalith.io/composite-profile-sha256=4183b741eac062d962a8ff1860a7aa049719a75f47e38e6fdcfb0fe1aeaa5d45
  hexalith.io/helm-manifest-sha256=f55ff3c237fad5047d6ad7d19a56a83c6546a2f31fc5830022bc3b3a51c9c8e3
  hexalith.io/hardening-manifest-sha256=f3fe70b98c64ec9072bc3ab54fd07ffdde1e4d4550c53d8f20e2bb58eb70f3eb
  hexalith.io/deployment-id=memories-production-2.11.0-c7c2ca21-hexalith-keys-r2
  hexalith.io/profile-id=redis-state-v1-dapr-1.18.1-openbao-2.6.0-4183b741eac062d9
```

The `kubectl.kubernetes.io/last-applied-configuration` annotation shows `namespace.yaml` applied only the
labels and the two ownership annotations. The five profile-identity annotations come from the production
deployment renderer.

**Re-derived drift-rejection claim:** the annotations still describe release `r2` while the platform is
at Helm revision 9. Every change recorded in this transcript happened underneath unchanged annotations,
so an evidence capture keyed on them rejects nothing. The previous documentation claim that these
annotations let a capture "reject drift before running a probe" is disproven and was removed.

## 2. Reconciliation outcome (Task 2)

`deploy/openbao/values.yaml` was reconciled to the measured release. Settings changed:

| Setting | Was declared | Reconciled to | Deployed evidence |
| :------ | :----------- | :------------ | :---------------- |
| `server.standalone.enabled` | `true` | `false` | HA config rendered, not standalone |
| `server.standalone.config` | full HCL with `node_id` | removed | rendered config has no `node_id` |
| `server.ha.enabled` | `false` | `true` | `bao status` → `ha_enabled: true` |
| `server.ha.replicas` | not declared | `3` | StatefulSet `.spec.replicas = 3` |
| `server.ha.raft.enabled` | not declared | `true` | `storage "raft"` with `retry_join` |
| `server.ha.raft.setNodeId` | not declared | `true` | env `BAO_RAFT_NODE_ID=metadata.name` |
| `server.ha.raft.config` | not declared | the measured HCL verbatim | ConfigMap `hexalith-keys-config` |
| `server.service.active.enabled` | `false` | `true` | Service `hexalith-keys-active` |
| `server.service.standby.enabled` | `false` | `true` | Service `hexalith-keys-standby` |
| `server.serviceAccount.serviceDiscovery.enabled` | `false` | `true` | Role and RoleBinding `hexalith-keys-discovery-*` |
| `server.authDelegator.enabled` | `false` | `true` | ClusterRoleBinding `hexalith-keys-server-binding` |
| `server.networkPolicy.ingress` | 2 sources | 3 sources (adds `cert-manager`) | measured policy spec, section 1.5 |

`deploy/openbao/namespace.yaml` and `deploy/openbao/smoke-test.yaml` were exact as deployed and needed no
change. `deploy/openbao/service-account-hardening.yaml` keeps `automountServiceAccountToken: false`
unchanged; only an explanatory comment recording the pod-level override was added.

Untracked platform state was named rather than adopted, with owner and reopen trigger, in
`docs/operations/openbao.md` section "Deployed platform state not tracked in this repository":
the `openbao-raft-snapshot` CronJob, its `openbao-snapshot` ServiceAccount, the `openbao-snapshots` PVC,
the `hexalith-keys-tokenreview` ClusterRoleBinding, the `hexalith-keys-pki` Secret, and the three
operational Secrets that are deliberately out of repository.

No Restricted Pod Security label, TLS setting, image digest pin, or NetworkPolicy restriction was
weakened. The reconciled file was **not** applied; no `helm upgrade` was run and the running platform was
not changed by this story. That gap is recorded as a named divergence with a `helm diff` reopen trigger.

## 3. Smoke test execution (Task 4)

Executed against context `jpiquot@local`, namespace `openbao`.

```text
$ date -u +"%Y-%m-%dT%H:%M:%SZ"
2026-07-28T09:43:31Z

$ kubectl -n openbao get job hexalith-keys-smoke-test
Error from server (NotFound): jobs.batch "hexalith-keys-smoke-test" not found

$ kubectl apply -f deploy/openbao/smoke-test.yaml
job.batch/hexalith-keys-smoke-test created

$ kubectl -n openbao wait --for=condition=complete job/hexalith-keys-smoke-test --timeout=2m
job.batch/hexalith-keys-smoke-test condition met

$ kubectl -n openbao logs job/hexalith-keys-smoke-test
{
  "type": "static",
  "initialized": true,
  "sealed": false,
  "t": 2,
  "n": 3,
  "progress": 0,
  "nonce": "",
  "version": "2.6.0",
  "commit_date": "2026-07-14T16:39:27Z",
  "migration": false,
  "cluster_name": "vault-cluster-a3756bea",
  "cluster_id": "47c0ef18-869d-7b96-edd0-b6006200c9c8",
  "recovery_seal": true,
  "recovery_seal_type": "shamir",
  "storage_type": "raft",
  "build_date": "",
  "ha_enabled": true,
  "leader_address": "https://10.233.102.188:8200",
  "leader_cluster_address": "https://hexalith-keys-1.hexalith-keys-internal:8201",
  "raft_committed_index": 512,
  "raft_applied_index": 512
}

$ date -u +"%Y-%m-%dT%H:%M:%SZ"
2026-07-28T09:43:38Z
```

Required fields: `initialized: true`, `sealed: false`, `storage_type: raft`, `ha_enabled: true`,
`version: 2.6.0`. The payload contains no secret value; `t: 2` / `n: 3` are the recovery-share threshold
and count, not share material. Logs were captured 7 seconds after creation, well inside
`ttlSecondsAfterFinished: 300`.

## 4. Security reviewer evaluation (Task 6, checkpoint C7)

**Accepted blocker — no reviewer signature obtained.**

| Field | Record |
| :---- | :----- |
| Reviewer of record | Murat TEA for Jérôme (`murat-tea-for-jpiquot`), carried as namespace annotation `hexalith.io/security-reviewer` |
| Platform Operations owner | Jérôme Piquot (`jpiquot`) |
| Evaluation status | **Not signed.** No named evaluator reviewed the measured platform during this dev-story session |
| Owner of the blocker | Security reviewer `murat-tea-for-jpiquot`, with Platform Operations (`jpiquot`) accountable for scheduling it |
| Consequence | Both accepted limitations are documented with owner, consequence, compensating controls, and reopen trigger, but no independent security authority has evaluated the platform **as measured**. The static file-based seal and the namespace-wide port 8200 ingress are recorded as accepted without a countersignature |
| Reopen trigger | A dated, named evaluation by `murat-tea-for-jpiquot` covering the platform as measured, both accepted limitations, and the amended AC2 deployed-profile qualifier, appended to this artifact as its own row |
| Checkpoint C7 state | `pending` / `not complete` |

The Task 1 measurements (section 1), the Task 2 reconciliation (section 2), and the executed smoke test
(section 3) are the handover package for that evaluation. Recording a recommendation to seek review
would not be a recorded evaluation; this row stays open until a named evaluator signs it. Story 27.3's
unassigned AC4 security approver is the precedent for this record.

## 5. Test evidence (Task 7)

### 5.1 Build

```text
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

Result: **Build succeeded. 0 Warning(s), 0 Error(s).**

### 5.2 Discovery, named unit "xUnit test method"

```text
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo
```

| Discovery scope | Before dev-story | After dev-story | Phase delta |
| :-------------- | ---------------: | --------------: | ----------: |
| `Hexalith.Memories.Server.Tests`, all xUnit methods | 2,190 | 2,199 | +9 |
| `Hexalith.Memories.Server.Tests.Deployment` namespace | 48 | 57 | +9 |
| `OpenBaoPlatformDocumentationTests` | 0 (class absent) | 9 | +9 (`0 -> 9` new scope) |
| `ProductionDeploymentArtifactsTests` | 9 | 9 | +0 |
| `DeploymentConfigurationContractTests` | 7 | 7 | +0 |
| `OperationalRunbookSetTests` | 9 | 9 | +0 |

Every added method, by set difference of the two sorted discoveries — nine added, none removed:

```text
OpenBaoPlatformDocumentationTests.AcceptedLimitations_AreNeverDescribedWithStrengthVocabulary
OpenBaoPlatformDocumentationTests.AcceptedLimitations_HaveExactHeaderTwoKeyedRowsAndNoEmptyCell
OpenBaoPlatformDocumentationTests.AvailabilityProfile_IsBoundToTheMeasuredPlatformNotTheManifest
OpenBaoPlatformDocumentationTests.OpenBaoPlatformRecords_Exist
OpenBaoPlatformDocumentationTests.OwnedManifests_EachHaveADocumentedSectionTiedToTheirSource
OpenBaoPlatformDocumentationTests.PlatformEvidence_RecordsExecutedSmokeTestAndReviewerState
OpenBaoPlatformDocumentationTests.PlatformRecords_ContainNoLeakedToolCallMarkup
OpenBaoPlatformDocumentationTests.PlatformRecords_ContainNoSecretShapedMaterial
OpenBaoPlatformDocumentationTests.SmokeTest_IsNamedExactlyAndItsExecutedResultIsRecorded
```

`ProductionDeploymentArtifactsTests` changed assertions without changing its method count, exactly as
planned: phase delta `+0`, with `OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`
strengthened rather than added to.

Sorted method-set SHA-256 before dev-story:
`bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0` — byte-identical to the create-story
baseline, so the create baseline and the dev-story pre-snapshot are the same discovery.
After dev-story: `2f99c8cd0c4a4aceb0296e78eabae46aec947034cdd32b4583437a4640c2630b`.

The built-assembly SHA-256 is **not** recorded as evidence. Two Release builds of unchanged sources
produced different assembly hashes while yielding an identical sorted method set, so the assembly hash
does not identify a discovery and cannot support count agreement. The sorted method-set SHA-256 is used
instead.

### 5.3 Focused lane

```text
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo

Hexalith.Memories.Server.Tests  Total: 9, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.087s
```

### 5.4 The guard was proven to bite

A passing guard proves nothing until a wrong document fails it. Each mutation below was applied to the
real file, run, and reverted; every file was then confirmed byte-identical to its pre-mutation copy.

| Mutation | Expected to fail | Observed |
| :------- | :--------------- | :------- |
| Document claims `1` Raft voter instead of the measured `3` | `AvailabilityProfile_IsBoundToTheMeasuredPlatformNotTheManifest` | failed as expected |
| Seal limitation prose reworded to "a production-ready trade" | `AcceptedLimitations_AreNeverDescribedWithStrengthVocabulary` | failed as expected |
| Service-token value pasted into the evidence artifact | `PlatformRecords_ContainNoSecretShapedMaterial` | failed as expected |
| `values.yaml` scaled to `replicas: 1` without the document following | `AvailabilityProfile_...` and `OwnedManifests_EachHaveADocumentedSectionTiedToTheirSource` | both failed as expected |
| Numbered `Unseal Key` init-dump label, in its labelled colon form, added to the document | `PlatformRecords_ContainNoSecretShapedMaterial` | failed as expected |
| Numbered `Recovery Key` init-dump label, in its labelled colon form, added to the document | `PlatformRecords_ContainNoSecretShapedMaterial` | failed as expected |
| `Initial Root Token` init-dump label, in its labelled colon form, added to the document | `PlatformRecords_ContainNoSecretShapedMaterial` | failed as expected |
| PEM private-key block header added to the document | `PlatformRecords_ContainNoSecretShapedMaterial` | failed as expected |
| A third row added to the accepted-limitations table with placeholder cells | `AcceptedLimitations_HaveExactHeaderTwoKeyedRowsAndNoEmptyCell` | failed as expected |
| A 52-character mixed-case base64 payload pasted into the evidence artifact | `PlatformRecords_ContainNoSecretShapedMaterial` | failed as expected |

The `replicas: 1` mutation is the one that matters most: it is the shape the recorded drift actually took,
and it fails on both the document tie and the manifest tie rather than on only one of them.

### 5.5 Regression lanes

```text
-class ...Deployment.ProductionDeploymentArtifactsTests
Hexalith.Memories.Server.Tests  Total: 9, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 1.487s

-class ...Deployment.OperationalRunbookSetTests -class ...Deployment.DeploymentConfigurationContractTests
Hexalith.Memories.Server.Tests  Total: 16, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.125s
```

Whole-assembly regression, named unit **xUnit test case** (theory rows expand, so this unit is not
comparable with the test-method counts in section 5.2 and is never subtracted from them):

```text
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -noLogo

Hexalith.Memories.Server.Tests  Total: 2754, Errors: 0, Failed: 0, Skipped: 1, Not Run: 0, Time: 12.820s
```

Zero failures. The single skip is pre-existing and unrelated to this story.

### 5.6 Whitespace

```text
git diff --check
```

Clean; no whitespace error introduced. Markdown and C# files under this story are CRLF-terminated and
the two YAML manifests are LF-terminated, per `.gitattributes`.

The three init-dump rows above deliberately name their labels without reproducing the trailing colon form. The guard scans this artifact too, so writing the literal label here would trip it -- which is itself a small confirmation that the check is looking where it claims to.

The long-run check was tightened during development after it reported five of this story's own test method names. A base64-alphabet run is now treated as key-shaped only when it also mixes digits with upper and lower case, which encoded key material always does and a long PascalCase identifier never does. The final mutation row above re-proves the tightened rule still catches a real payload, so the fix removed a false positive without removing the check.
