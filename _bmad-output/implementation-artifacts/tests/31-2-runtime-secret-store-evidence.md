# Story 31.2 runtime secret-store evidence

## Deployment preflight

On 2026-11-22T14:44:27Z–14:44:28Z, the requested context was `jpiquot@local` and the namespace was `hexalith-memories`. The following commands used the workspace kubectl. Each returned exit 1.

```sh
kubectl config current-context
kubectl config get-contexts jpiquot@local -o name
kubectl --context=jpiquot@local -n hexalith-memories --request-timeout=5s get pods -o 'custom-columns=NAME:.metadata.name,APP:.metadata.annotations.dapr\.io/app-id'
```

The client was kubectl v1.35.0. The respective errors were `current-context is not set`, `context jpiquot@local not found` and `context "jpiquot@local" does not exist`. That shell had no kubeconfig defining the context. It observed no pod names, app IDs, component metadata, Secret names/types or HTTP statuses. The failed preflight is kept as history.

From 2026-11-22T15:16:11Z to 15:16:12Z, the same checks ran in the Claude Code shell with kubectl v1.36.1 and `~/.kube/config`. The current context was `jpiquot@local`, the named context resolved, and the pod listing succeeded. All later live observations come from that shell. No cluster object was created, changed or deleted. Apart from list and get calls, the session used `kubectl exec` only for the tool checks and probes described under Scoped reads and denials. No Secret content, token, CA or response body was printed or stored.

## Current Secret names and types

Measured 2026-11-22T15:16:26Z–15:16:29Z with the names/types projection from the read-only replay below.

| Secret | Type |
| --- | --- |
| access-telemetry-clock-key | Opaque |
| access-telemetry-clock-sources | Opaque |
| access-telemetry-marker-key | Opaque |
| access-telemetry-postgresql-bootstrap | Opaque |
| access-telemetry-postgresql-tls | Opaque |
| app-api-token | Opaque |
| dapr-api-token | Opaque |
| google-embedding-api-key | Opaque |
| llm-secret | Opaque |
| memories-embedding-client-secret | Opaque |
| openbao-access-telemetry-bootstrap | Opaque |
| openbao-runtime-bootstrap | Opaque |
| redis-secret | Opaque |
| registry-credentials | kubernetes.io/dockerconfigjson |

The namespace holds fourteen Secrets. Thirteen are `Opaque` and `registry-credentials` is `kubernetes.io/dockerconfigjson`. All fourteen names occur in the July 28 list of fifteen. `access-telemetry-postgresql` is the only July name now absent, and no new name appeared. The operations inventory keeps the absent July row with its disposition. Namespace `openbao` was not listed. Its four platform rows remain Story 31.1's record.

## Live workloads and app IDs

Measured 2026-11-22T15:16:26Z–15:16:29Z and 15:16:54Z–15:16:56Z. Replica columns compare the live object with `kubectl kustomize deploy/kubernetes/overlays/production`.

| Workload | Dapr app ID | Live replicas, ready | Production render | Pods |
| --- | --- | --- | --- | --- |
| Deployment memories | memories | 2, 2 | 2 | memories-675fd5d64-5zqpv, memories-675fd5d64-jtd8v |
| Deployment memories-mcp | memories-mcp | 2, 2 | 2 | memories-mcp-55d65d86d6-hh6mr, memories-mcp-55d65d86d6-rrd7z |
| Deployment memories-access-telemetry | memories-access-telemetry | 1, 1 | 0 | memories-access-telemetry-cb49f44d-cscb4 |
| Deployment memories-access-telemetry-clock | memories-access-telemetry-clock | 0, none | 0 | none |
| StatefulSet access-telemetry-postgresql | none | 1, 1 | 0 | access-telemetry-postgresql-0 |
| StatefulSet redis-stack | none | 1, 1 | 1 | redis-stack-0 |
| StatefulSet falkordb | none | 1, 1 | 1 | falkordb-0 |
| CronJob postgresql-tls-reloader | none | three completed Jobs | absent | Jobs started 12:23Z, 13:23Z and 14:23Z |

Every Dapr pod runs `ghcr.io/dapr/daprd:1.18.1`. Both memories sidecars started on 2026-09-25 and both memories-mcp sidecars on 2026-11-01. Pods `memories-*` use configuration `memories-config` and ServiceAccount `memories`. Pods `memories-mcp-*` use `memories-mcp-config` and ServiceAccount `memories-mcp`.

No pod, Deployment, ReplicaSet, StatefulSet, Job or CronJob template carries app ID `eventstore`. The `eventstore` ServiceAccount, Role and RoleBinding exist without a workload. Twelve older ReplicaSets keep the same four app IDs and Secret references as their Deployments.

## Live components and drift

Measured 2026-11-22T15:16:26Z–15:17:54Z. Both renders were taken from this worktree with `kubectl kustomize`.

The runtime store `secretstore` matches both renders. It is `secretstores.hashicorp.vault` v1 with scopes `eventstore` and `memories`. The boolean projection reported `true` for vaultAddr, tlsServerName, skipVerify, vaultKVPrefix (`hexalith/memories/runtime`), vaultKVUsePrefix, enginePath and vaultValueType. `caPem` and `vaultToken` reference `openbao-runtime-bootstrap` keys `ca.pem` and `token`, neither has an inline value, and no unexpected field or `ignoreErrors` exists. `statestore`, `pubsub` and `llm-openai` resolve `redis-secret/password` and `llm-secret/OPENAI_API_KEY` through `auth.secretStore: secretstore`, with the rendered scopes.

The telemetry store metadata also matches, with prefix `hexalith/memories/access-telemetry` and bootstrap `openbao-access-telemetry-bootstrap`. `access-telemetry-store` resolves `access-telemetry-postgresql/connectionString` through `access-telemetry-secrets`, as rendered. `memories-config`, `memories-mcp-config` and `memories-access-telemetry-clock-config` match their rendered secret scopes. The five ServiceAccount pull references, the Secret references in every workload template, the other four tracked Roles and all five tracked RoleBindings also match.

The following live objects differ from the production render. Times are the `kubectl-client-side-apply` managed-field times.

| Object | Live | Production render | Applied | Owner | Reopen trigger |
| --- | --- | --- | --- | --- | --- |
| Component access-telemetry-secrets scopes | memories, memories-access-telemetry, memories-access-telemetry-clock | memories-access-telemetry, memories-access-telemetry-clock | 2026-07-19T13:43:41Z | Platform Operations / Story 27.3 | Production overlay apply, or any telemetry store scope change |
| Component access-telemetry-config scopes | memories, memories-access-telemetry | memories-access-telemetry | 2026-07-19T11:52:26Z | Platform Operations / Story 27.3 | Production overlay apply, or any telemetry configuration scope change |
| Role memories-dapr-secret-reader | Nine names, including openbao-access-telemetry-bootstrap | Eight names | 2026-07-19T13:43:40Z | Platform Operations / Memories Maintainer | Production overlay apply, or the grant review in the operations inventory |
| Configuration memories-access-telemetry-config | Telemetry allow-list names access-telemetry-marker-key and redis-secret | Also names access-telemetry-postgresql | 2026-07-19T13:09:04Z | Story 27.3 / Platform Operations | Lifecycle store authentication change, or production overlay apply |
| CronJob, ServiceAccount, Role and RoleBinding postgresql-tls-reloader | Present. The Role grants get on access-telemetry-postgresql-tls | Absent from both renders and every tracked file | 2026-08-26T14:31:11Z | Platform Operations / Story 27.3 | Adopt the objects into tracked manifests or remove them |
| Deployment memories-access-telemetry and StatefulSet access-telemetry-postgresql | 1 replica each | 0 replicas each. Qualification renders PostgreSQL at 1 | Not measured | Story 27.3 / Platform Operations | Production overlay apply, or a lifecycle qualification decision |

The first three rows match the base and qualification shape. In the live namespace the memories sidecars therefore still receive the telemetry store, and `memories-config` permits `access-telemetry-marker-key` there. The production render removes that path. These drifts concern Story 27.3 objects or Kubernetes grants. None changes the runtime store, its bootstrap or its scopes. This story records them and did not apply, patch or delete anything.

## Scoped reads and denials

The probe ran from 2026-11-22T15:23:32Z to 15:23:34Z with `tools/probe-dapr-secret-scope.sh` at SHA-256 `b932a85ad5dfb7491984b24e36fc9722962162b6bcb71491323b5528b38ab895`. It ran in each app container through `kubectl exec -i <pod> -c <container> -- sh -s -- <app> <check> < tools/probe-dapr-secret-scope.sh`. A 15:17:45Z tool check found `/bin/busybox` 1.37.0 with nc, awk and timeout in both app images and `DAPR_API_TOKEN` present (presence only) with port 3500. A 15:21:05Z check showed that nc in both images supports `-e`.

| UTC | Pod and container | App and check | Authenticated metadata | Request | Result | Attributed control |
| --- | --- | --- | --- | --- | --- | --- |
| 15:23:32Z | memories-675fd5d64-5zqpv / memories | memories allowed | 200, one vault store named secretstore | secretstore, redis-secret | 200, password field present, exit 0 | Scoped success |
| 15:23:32Z | memories-675fd5d64-jtd8v / memories | memories allowed | 200, one vault store named secretstore | secretstore, redis-secret | 200, password field present, exit 0 | Scoped success |
| 15:23:33Z | memories-mcp-55d65d86d6-hh6mr / memories-mcp | memories-mcp unscoped | 200, secretstore absent | secretstore, redis-secret | 401, store unavailable, exit 0 | Component scope |
| 15:23:33Z | memories-mcp-55d65d86d6-rrd7z / memories-mcp | memories-mcp unscoped | 200, secretstore absent | secretstore, redis-secret | 401, store unavailable, exit 0 | Component scope |
| 15:23:33Z | memories-675fd5d64-5zqpv / memories | memories outside | 200, one vault store named secretstore | secretstore, access-telemetry-marker-key | 403, name policy, exit 0 | memories-config allow-list |
| 15:23:34Z | memories-675fd5d64-jtd8v / memories | memories outside | 200, one vault store named secretstore | secretstore, access-telemetry-marker-key | 403, name policy, exit 0 | memories-config allow-list |

The probe reports the password field's presence only. It printed no value or body.

Each denial is attributed to the control that produced it:

- **memories-mcp.** The same in-pod token authenticated the metadata request, which returned the pod's own app ID. A token error would have stopped the probe there. That metadata listed no `secretstore`, matching component scopes `eventstore` and `memories`. Dapr then answered 401 with `ERR_SECRET_STORE_NOT_FOUND`. A bounded diagnostic copy confirmed that code at 15:19:07Z. The probe parsed an HTTP response, which excludes a transport failure, and no key lookup can occur in an unregistered store. `memories-mcp-config` also denies this store by default with an empty allow-list. That second control was not reached.
- **memories outside name.** The vault store was registered, and Dapr answered 403 with `ERR_PERMISSION_DENIED`. `memories-config` allows only redis-secret, google-embedding-api-key, memories-embedding-client-secret and llm-secret on `secretstore` with `defaultAccess: deny`. `access-telemetry-marker-key` belongs to the telemetry store and prefix. Dapr rejects the name before any OpenBao lookup, so this request does not exercise the OpenBao runtime policy or prefix.
- **Successful reads.** Both memories pods resolved `redis-secret` through the vault-typed store with the field present. This substantiates the `memories` scope by a live scoped read for the current deployment.

### Probe transport defect found during replay

At 15:18:04Z–15:18:06Z the first replay used the probe committed at HEAD `2b82cb36` (SHA-256 `436d4bae55b39b437b841c5a4b953a3fc2985f7d89ccf3de3257dd0cb234b58a`). Both permitted reads returned 500 and the probe printed `unverified` and exited 1. Metadata, both MCP denials and both outside-name denials produced the results above. A scratch copy of the probe added two outputs: the Dapr error code when it matched `ERR_[A-Z_]+`, and fixed-pattern classes of the error message. It printed no message text. At 15:19:05Z–15:19:20Z it reported `ERR_SECRET_GET` for both pods, classed as a transport error with `context canceled`.

BusyBox 1.37.0 nc shuts down its write side at stdin EOF. Its `nc_bloaty.c` source calls `shutdown(netfd, SHUT_WR)` there. The `context canceled` class and the fixed replay indicate that daprd canceled the request when the client half-closed, which aborted the outbound OpenBao read. Metadata, component scope and name policy are decided locally, so they completed first. The 500 was neither a fallback nor an empty success, and the probe rejected it.

The probe now uses `nc -e`. A child shell writes the request and reads the response without half-closing the connection. It receives the token only through its environment and writes it only to the loopback socket. The synthetic loopback test now cancels a store read when the client has half-closed, as daprd does. The test is one unittest method that runs thirteen cases as subTests. With the old transport temporarily reinstated, the two permitted-read subTests failed and the other eleven passed. The restored probe matched its SHA-256 `b932a85ad5dfb7491984b24e36fc9722962162b6bcb71491323b5528b38ab895` and all thirteen subTests passed. Neither run printed the generated token or credential. The 15:23Z replay above used the fixed probe.

## Reserved eventstore scope

The structural proof for the reserved scope is current. The live `secretstore` declares scope `eventstore` beside `memories`, as do both renders. The live `pubsub` also lists it. No workload in the namespace carries app ID `eventstore`, so no live read was attempted or claimed for it. The memories-mcp rows demonstrate the denial for a non-scoped app ID. The running `memories-access-telemetry` app ID is also outside both runtime scopes. It was not probed because the probe defines no check for it. Memories Maintainer owns the reserved scope. Reopen when an eventstore-app-id workload is deployed, and require its authorized live read.

## Built-in Kubernetes secret store

Dapr loads a built-in `kubernetes` secret store in Kubernetes mode unless the workload disables it. The coordinator's review recorded it at 2026-11-22T15:51:51Z in memories-675fd5d64-5zqpv and memories-mcp-55d65d86d6-hh6mr. The same request ran again from 16:02:52Z to 16:02:53Z in all four application pods. It reads only the authenticated metadata endpoint and prints only secret-store names and types:

```sh
#!/bin/sh
set +x
set -eu
port=${DAPR_HTTP_PORT:-3500}
: "${DAPR_API_TOKEN:?missing}"
busybox timeout 8 busybox nc -w 5 127.0.0.1 "$port" -e busybox sh -c '
  printf "GET /v1.0/metadata HTTP/1.0\r\nHost: localhost\r\ndapr-api-token: %s\r\nConnection: close\r\n\r\n" "$DAPR_API_TOKEN"
  exec busybox cat >&2' 2>&1 >/dev/null | busybox tr -d '\n' | busybox grep -oE '"name":"[a-z0-9.-]+","type":"secretstores\.[a-z0-9.]+"' || echo "no secretstore entries matched"
```

```bash
"${k[@]}" exec -i "$pod" -c "$container" -- sh -s < meta-stores.sh
"${k[@]}" get deploy,sts -o 'custom-columns=NAME:.metadata.name,DISABLE:.spec.template.metadata.annotations.dapr\.io/disable-builtin-k8s-secret-store'
"${k[@]}" get pods -o 'custom-columns=NAME:.metadata.name,DISABLE:.metadata.annotations.dapr\.io/disable-builtin-k8s-secret-store'
"${k[@]}" get configurations.dapr.io -o 'custom-columns=NAME:.metadata.name,STORES:.spec.secrets.scopes[*].storeName'
grep -rn 'disable-builtin-k8s-secret-store\|storeName: kubernetes' deploy/kubernetes
```

| Pods | Secret stores in metadata |
| --- | --- |
| memories-675fd5d64-5zqpv, memories-675fd5d64-jtd8v | kubernetes (secretstores.kubernetes), access-telemetry-secrets and secretstore (both secretstores.hashicorp.vault) |
| memories-mcp-55d65d86d6-hh6mr, memories-mcp-55d65d86d6-rrd7z | kubernetes (secretstores.kubernetes) only |

No live workload template or pod sets `dapr.io/disable-builtin-k8s-secret-store`. The live Configurations scope only `secretstore` and `access-telemetry-secrets`, and no tracked file under `deploy/kubernetes` scopes `kubernetes` or sets the annotation. Every Kubernetes Secret granted to a pod's ServiceAccount is therefore reachable through the Dapr Secrets API outside OpenBao and the allow-lists. For memories that covers the nine names of the live `memories-dapr-secret-reader`, including `openbao-runtime-bootstrap`. For memories-mcp it covers `app-api-token` and `dapr-api-token`. The lifecycle pod was not measured. No read through the `kubernetes` store was attempted. Platform Operations and Memories Maintainer own closing the path, either with the disable annotation or with a `kubernetes` scope set to `defaultAccess: deny` in each Configuration. No manifest or cluster object was changed. Reopen when the path is closed or a Role grant changes.

## Tracked components

The production and qualification renders use the following contract. The xUnit method `KubernetesRuntimeStore_UsesVaultBootstrapTlsAndBothScopes` also checks raw Kubernetes manifests for any competing component named `secretstore`.

| Field | Runtime store | Telemetry store |
| --- | --- | --- |
| Name and type | secretstore, secretstores.hashicorp.vault v1 | access-telemetry-secrets, secretstores.hashicorp.vault v1 |
| TLS | https://hexalith-keys.openbao.svc.cluster.local:8200, server name hexalith-keys.openbao.svc.cluster.local, skipVerify false | Same |
| Kubernetes bootstrap | openbao-runtime-bootstrap, token and ca.pem references | openbao-access-telemetry-bootstrap, token and ca.pem references |
| KV | engine secret, map, use prefix true, hexalith/memories/runtime | engine secret, map, use prefix true, hexalith/memories/access-telemetry |
| Scopes | eventstore, memories in both overlays | Base/qualification include memories, memories-access-telemetry and memories-access-telemetry-clock. Production keeps only the latter two. |

The four runtime allowed names are redis-secret, llm-secret, google-embedding-api-key and memories-embedding-client-secret. State and pub/sub resolve redis-secret/password through the runtime store, and the conversation component resolves llm-secret/OPENAI_API_KEY there. Telemetry Redis and PostgreSQL resolve their component references through access-telemetry-secrets. The memories configuration permits access-telemetry-marker-key on that separate store. Both stores default to deny in memories-config. The production overlay removes memories from the telemetry store and telemetry configuration component, and removes its telemetry bootstrap RBAC grant (eight names versus nine in base/qualification). The remaining allow-list entry alone cannot load a component. The MCP workload uses memories-mcp-config, which grants no runtime names.

Production declares the memories, memories-mcp, memories-access-telemetry and memories-access-telemetry-clock IDs. Qualification also declares the access-telemetry-adapter reporter. No tracked workload declares eventstore. Memories Maintainer owns the reserved scope. Reopen when an eventstore-app-id workload is deployed and require its authorized live read.

## Read-only replay

Run from the repository root. Record UTC start/end times, context, namespace, selected pods and app IDs with each result. Stop at an unavailable context. The 2026-11-22T15:16Z–15:23Z results above came from these commands.

```bash
ctx=jpiquot@local
ns=hexalith-memories
k=(kubectl --context="$ctx" -n "$ns" --request-timeout=10s)
date -u +%FT%TZ
kubectl config get-contexts "$ctx" -o name
"${k[@]}" get secrets --no-headers -o custom-columns=NAME:.metadata.name,TYPE:.type
"${k[@]}" get pods -o 'custom-columns=NAME:.metadata.name,APP:.metadata.annotations.dapr\.io/app-id,CONFIG:.metadata.annotations.dapr\.io/config,SA:.spec.serviceAccountName,PHASE:.status.phase'
"${k[@]}" get deploy,sts,ds,rs,job -o 'custom-columns=KIND:.kind,NAME:.metadata.name,APP:.spec.template.metadata.annotations.dapr\.io/app-id'
"${k[@]}" get cronjob -o 'custom-columns=NAME:.metadata.name,APP:.spec.jobTemplate.spec.template.metadata.annotations.dapr\.io/app-id'
"${k[@]}" get components.dapr.io -o 'custom-columns=NAME:.metadata.name,VERSION:.spec.version,TYPE:.spec.type,SCOPES:.scopes,STORE:.auth.secretStore'
"${k[@]}" get configurations.dapr.io memories-config memories-mcp-config -o 'custom-columns=NAME:.metadata.name,SECRETS:.spec.secrets.scopes'
"${k[@]}" get sa -o 'custom-columns=NAME:.metadata.name,PULL:.imagePullSecrets[*].name'
"${k[@]}" get role -o 'custom-columns=NAME:.metadata.name,GRANTS:.rules[*].resourceNames'
"${k[@]}" get rolebinding -o 'custom-columns=NAME:.metadata.name,ROLE:.roleRef.name,SUBJECTS:.subjects[*].name'
"${k[@]}" get components.dapr.io -o 'custom-columns=NAME:.metadata.name,RESOLVER:.auth.secretStore,REFS:.spec.metadata[*].secretKeyRef.name,KEYS:.spec.metadata[*].secretKeyRef.key'
refs() {
  local kinds=$1 p=$2
  "${k[@]}" get "$kinds" -o "custom-columns=NAME:.metadata.name,SA:$p.serviceAccountName,ENV:$p.containers[*].env[*].valueFrom.secretKeyRef.name,ENVFROM:$p.containers[*].envFrom[*].secretRef.name,INIT:$p.initContainers[*].env[*].valueFrom.secretKeyRef.name,INITFROM:$p.initContainers[*].envFrom[*].secretRef.name,VOL:$p.volumes[*].secret.secretName,PROJECTED:$p.volumes[*].projected.sources[*].secret.name,PULL:$p.imagePullSecrets[*].name"
}
refs pods .spec
refs deploy,sts,ds,rs,job .spec.template.spec
refs cronjob .spec.jobTemplate.spec.template.spec
date -u +%FT%TZ
```

Compare each current name and type with the historical rows in the operations inventory. Mark July-only rows as absent only after a successful list. Add new live names, look for env, volume, pull, Role/Binding and bootstrap references, and assign owner and disposition. Inspect template references also for scaled-to-zero workloads. A name used under `auth.secretStore` belongs to that store. A Kubernetes grant alone supplies no consumer.

For each store, compare the live type, version, scopes and metadata to the rendered values above. The projection below emits safe reference names and keys and boolean comparisons only. An unexpected inline token or CA is reported as a mismatch without printing it.

```bash
for pair in 'secretstore runtime' 'access-telemetry-secrets access-telemetry'; do
  read -r store suffix <<< "$pair"
  template='{{range .spec.metadata}}{{.name}} {{if eq .name "vaultToken"}}{{if .secretKeyRef}}{{.secretKeyRef.name}}/{{.secretKeyRef.key}}{{end}} inline={{if .value}}true{{else}}false{{end}}{{else if eq .name "caPem"}}{{if .secretKeyRef}}{{.secretKeyRef.name}}/{{.secretKeyRef.key}}{{end}} inline={{if .value}}true{{else}}false{{end}}{{else if eq .name "vaultAddr"}}{{eq .value "https://hexalith-keys.openbao.svc.cluster.local:8200"}}{{else if eq .name "tlsServerName"}}{{eq .value "hexalith-keys.openbao.svc.cluster.local"}}{{else if eq .name "skipVerify"}}{{eq .value "false"}}{{else if eq .name "vaultKVPrefix"}}{{eq .value "hexalith/memories/SUFFIX"}}{{else if eq .name "vaultKVUsePrefix"}}{{eq .value "true"}}{{else if eq .name "enginePath"}}{{eq .value "secret"}}{{else if eq .name "vaultValueType"}}{{eq .value "map"}}{{else}}unexpected-field{{end}}{{"\n"}}{{end}}'
  if summary=$("${k[@]}" get component.dapr.io "$store" -o "go-template=${template/SUFFIX/$suffix}" 2>/dev/null); then
    printf '%s\n' "$summary"
  else
    printf 'component comparison unavailable for %s\n' "$store"
  fi
done
```

Select a ready app container from the pod list for each ID. Stream the probe to the existing BusyBox shell inside that container, with its existing DAPR_API_TOKEN and loopback sidecar. The probe reads the authenticated metadata endpoint first to confirm the app ID and registered component. It parses the secret response in memory and emits only public names, presence, HTTP status and a bounded outcome. It needs BusyBox awk, timeout and nc with `-e`. The HTTP/1.0 parser rejects unsupported transfer encodings and unrecognized JSON. Record missing tools or unrecognized responses as inconclusive.

```bash
probe=tools/probe-dapr-secret-scope.sh
"${k[@]}" exec -i "$memories_pod" -c memories -- sh -s -- memories allowed < "$probe"
"${k[@]}" exec -i "$mcp_pod" -c memories-mcp -- sh -s -- memories-mcp unscoped < "$probe"
"${k[@]}" exec -i "$memories_pod" -c memories -- sh -s -- memories outside < "$probe"
# Required only when the workload inventory includes the reserved app ID.
"${k[@]}" exec -i "$eventstore_pod" -c "$eventstore_app_container" -- sh -s -- eventstore allowed < "$probe"
```

The positive request needs HTTP 200 and a nonempty password field under redis-secret with the Vault component registered. The MCP request uses the same key. Compare its authenticated metadata and deployed component scopes to distinguish an excluded store from a component that failed globally. If Dapr reports a name-policy denial, attribute it to that policy and record the independent component exclusion from metadata. The memories outside-name probe requests access-telemetry-marker-key via the runtime store and needs an explicit name-policy denial. An empty success, absent key, bad token, transport error or unknown error cannot satisfy a denial. Recheck with an actual eventstore pod upon deployment.

The resumed run also used these read-only commands. The listing commands ran between 15:16:54Z and 15:18:51Z, and the tool checks at 15:17:45Z and 15:21:05Z.

```bash
"${k[@]}" get configurations.dapr.io -o 'custom-columns=NAME:.metadata.name,SECRETS:.spec.secrets.scopes'
"${k[@]}" get deploy,sts -o 'custom-columns=KIND:.kind,NAME:.metadata.name,REPLICAS:.spec.replicas,READY:.status.readyReplicas'
"${k[@]}" get pods -o 'custom-columns=NAME:.metadata.name,CONTAINERS:.spec.containers[*].name,IMAGES:.spec.containers[*].image'
"${k[@]}" get pods -o 'custom-columns=NAME:.metadata.name,START:.status.startTime,RESTARTS:.status.containerStatuses[*].restartCount,DAPRD_STARTED:.status.containerStatuses[?(@.name=="daprd")].state.running.startedAt'
"${k[@]}" get rs memories-675fd5d64 memories-mcp-55d65d86d6 -o 'custom-columns=NAME:.metadata.name,CREATED:.metadata.creationTimestamp'
"${k[@]}" get components.dapr.io,configurations.dapr.io
for store in secretstore access-telemetry-secrets; do
  "${k[@]}" get component.dapr.io "$store" -o 'go-template=ignoreErrors={{if .spec.ignoreErrors}}{{.spec.ignoreErrors}}{{else}}unset{{end}} initTimeout={{if .spec.initTimeout}}{{.spec.initTimeout}}{{else}}unset{{end}}{{"\n"}}'
done
for o in component/access-telemetry-secrets component/access-telemetry-config component/secretstore configuration/memories-access-telemetry-config role/memories-dapr-secret-reader cronjob/postgresql-tls-reloader role/postgresql-tls-reloader; do
  "${k[@]}" get "$o" -o jsonpath='{.metadata.generation} {.metadata.managedFields[*].time} {.metadata.managedFields[*].manager} {.metadata.creationTimestamp}{"\n"}'
done
kubectl --context="$ctx" -n openbao --request-timeout=15s get pods -o wide
git grep -n 'postgresql-tls-reloader' -- . ':!references'
# Tool checks in memories-675fd5d64-5zqpv/memories and memories-mcp-55d65d86d6-hh6mr/memories-mcp
"${k[@]}" exec "$pod" -c "$container" -- sh -c 'command -v busybox; for t in nc awk timeout; do busybox --list | grep -qx "$t" && echo "$t=yes" || echo "$t=no"; done; [ -n "${DAPR_API_TOKEN:-}" ] && echo token=present || echo token=absent; echo port=${DAPR_HTTP_PORT:-unset}'
"${k[@]}" exec memories-675fd5d64-5zqpv -c memories -- sh -c 'busybox 2>&1 | head -1; busybox nc --help 2>&1 | head -30; grep -E "^(ID|VERSION_ID)=" /etc/os-release; mount | grep " / "; id'
"${k[@]}" exec memories-mcp-55d65d86d6-hh6mr -c memories-mcp -- sh -c 'busybox 2>&1 | head -1; busybox nc --help 2>&1 | grep -E "^\s+-e"'
```

The render comparison projected both overlays to names, scopes, stores, references and grants with this script, run as `python3 -I render.py` from the repository root:

```python
import sys, yaml, json, subprocess
out = {}
for ov in ["production", "qualification"]:
    text = subprocess.run(["kubectl", "kustomize", "deploy/kubernetes/overlays/" + ov], capture_output=True, text=True, check=True).stdout
    docs = [d for d in yaml.safe_load_all(text) if isinstance(d, dict)]
    r = {"components": {}, "configs": {}, "roles": {}, "bindings": {}, "sa": {}, "workloads": {}, "secrets": [], "kinds": {}}
    for d in docs:
        kind = d.get("kind"); name = d["metadata"]["name"]; ns = d["metadata"].get("namespace")
        r["kinds"].setdefault(kind, []).append(name + ("@" + ns if ns else ""))
        if kind == "Component":
            md = d["spec"].get("metadata", [])
            r["components"][name] = {"type": d["spec"]["type"], "version": d["spec"]["version"], "scopes": d.get("scopes"), "store": (d.get("auth") or {}).get("secretStore"),
                "refs": [m["name"] + "=" + m["secretKeyRef"]["name"] + "/" + m["secretKeyRef"]["key"] for m in md if "secretKeyRef" in m]}
        elif kind == "Configuration":
            r["configs"][name] = (d.get("spec") or {}).get("secrets")
        elif kind == "Role":
            r["roles"][name] = [x for rule in d.get("rules", []) for x in rule.get("resourceNames", [])]
        elif kind == "RoleBinding":
            r["bindings"][name] = [d["roleRef"]["name"], [s["name"] for s in d.get("subjects", [])]]
        elif kind == "ServiceAccount":
            r["sa"][name] = [p["name"] for p in d.get("imagePullSecrets", [])]
        elif kind == "Secret":
            r["secrets"].append(name)
        elif kind in ("Deployment", "StatefulSet", "CronJob", "Job", "DaemonSet"):
            t = d["spec"]["jobTemplate"]["spec"]["template"] if kind == "CronJob" else d["spec"]["template"]
            sp = t["spec"]
            env = [e["valueFrom"]["secretKeyRef"]["name"] for c in sp.get("containers", []) + sp.get("initContainers", []) for e in c.get("env", []) if "valueFrom" in e and "secretKeyRef" in e["valueFrom"]]
            vol = [v["secret"]["secretName"] for v in sp.get("volumes", []) if "secret" in v]
            r["workloads"][kind + "/" + name] = {"replicas": d["spec"].get("replicas"), "sa": sp.get("serviceAccountName"), "app": (t["metadata"].get("annotations") or {}).get("dapr.io/app-id"), "env": sorted(set(env)), "vol": vol, "pull": [p["name"] for p in sp.get("imagePullSecrets", [])]}
    out[ov] = r
print(json.dumps(out, indent=1))
```

## Disposable failure and archived CI

The local bootstrap-failure test starts daprd 1.19.1 with app ID memories and the tracked Vault component in a temporary directory. Only the resolver and destination are isolated. An empty local bootstrap store cannot supply openbao-runtime-bootstrap. The sidecar reported the missing bootstrap and a fatal Vault component initialization failure, exited nonzero, and exposed no successful read or loaded replacement. The test polls the named Secrets API during startup and after exit. Run `python3 tests/tooling/runtime_secret_store/fail_closed.py deploy/kubernetes/base/dapr/secretstore.yaml` with daprd on PATH. This lane uses no live credentials. The deployed sidecars run daprd 1.18.1, so this disposable result is not a deployed observation.

The same script adds a second case. A local bootstrap store supplies a synthetic `openbao-runtime-bootstrap` with a generated self-signed CA, and `vaultAddr` points at the unreachable `https://127.0.0.1:1`. The Vault component then loads, and the read through `secretstore` fails with HTTP 500. The metadata lists only that Vault store and the local bootstrap store. No read succeeded and no fallback loaded. The script prints the failed stage, and it was the read. This lane has no consumer component, so it does not show whether state or pub/sub would fail to load. The case needs `openssl` on PATH.

DW-726 (CR29) and DW-727 (CR30) were resolved on September 2 against staged OpenBao. Run 33400812038, job 99516369413 and artifact 9761351293 are the register's references. The successful deployment job was independently checked then. The packet details, including resolution through the production component type, came from the August transcription. The register discloses that the packet was not downloaded and reparsed in that review. The November reconciliation adds no independent packet verification to either disposition. The current deployed reads above are separate evidence. They supplement those staged-evidence dispositions and do not change them.

## Verification on November 22

In that earlier run the SDK was 11.0.100, kubectl was v1.35.0, and the disposable sidecar was 1.19.1. The resumed run used kubectl v1.36.1. The workspace used its cached NuGet packages. From the repository root:

```bash
server=tests/Hexalith.Memories.Server.Tests
dotnet build "$server/Hexalith.Memories.Server.Tests.csproj" -c Debug --disable-build-servers -m:1 /nr:false -p:UseHexalithProjectReferences=true -p:MinVerVersionOverride=1.0.0
dll="$server/bin/Debug/net11.0/Hexalith.Memories.Server.Tests.dll"
export DiffEngine_Disabled=true
dotnet exec "$dll" -list methods -noLogo
dotnet exec "$dll" -class '*.RuntimeSecretStoreMigrationTests' -class '*.OpenBaoPlatformDocumentationTests' -class '*.ProductionDeploymentArtifactsTests' -parallel none -noLogo
python3 -B -m unittest discover -s tests/tooling/runtime_secret_store
```

Server build succeeded with zero warnings and errors. On the November spec baseline, direct discovery counted 2,475 server methods, including 133 in Deployment, 45 platform and 14 production methods. After implementation the same Debug discovery counted 2,482 server methods and 140 in Deployment. Seven new migration methods account for the full difference. Direct execution passed 164 cases with zero failures or skips: 7 migration, 143 platform documentation, and 14 production deployment. Theory cases account for the difference between platform method and execution counts.

| Check | Execution and result |
| --- | --- |
| Gate | ActivationGate_MatchesEpicAndCurrentCheckpointSet checked all three gate clauses and the unique complete platform rows. Its mutation companion removed, duplicated and marked C4a pending, also removed C4a from each operative list, and independently added a whole-story completion requirement to each gate. All nine mutations were rejected in memory. |
| Product boundary | ProductProjects_DoNotAccessTheSecretProviderDirectly scanned the 12 product projects, including DomainService and EventStoreExtensions. The five named topology/infrastructure projects were excluded. Temporary changes added a provider SDK package to Contracts, and a client constructor, endpoint, configuration key and token-shaped literal to Server. Each focused runner invocation exited 1 with one failure. No injected value appeared in its diagnostics. Every changed file was restored byte-for-byte. The November 22 review widened this scan, as recorded under Resumed replay verification. |
| Deployment fallback | Temporarily replacing the tracked runtime type with the Kubernetes store type failed KubernetesRuntimeStore_UsesVaultBootstrapTlsAndBothScopes. Restoring the original bytes returned the selector to green. The independent disposable test observed absent bootstrap and fatal Vault initialization at 14:45:33Z during development, followed by the passing packaged integration execution. |
| Published evidence | Inserting an unlabelled synthetic base64 run containing slashes and padding into this evidence file failed PublishedMigrationEvidence_ContainsNoSecretMaterial. The diagnostic gave the category and count only. The input file was restored byte-for-byte. The shared detector also covers PEM, current/legacy token prefixes, operator-init labels, UUIDs and long hex runs. |
| Resolved credential at outputs | A Dapr client double supplied a fresh synthetic value. The embedding provider received it and returned it in a simulated HTTP failure. The actual transport, exception logger, structured audit telemetry and serialized public error snapshot were exercised. Removing the transport's response redaction caused the captured logger assertion to fail. The full runner output contained no synthetic value. The source was restored byte-for-byte and the test passed again. The exception handler only reads the current activity and writes no tags, events or status to it. The structured audit event is therefore the telemetry being checked. After review the test also serializes the activity's tag objects and status description. |
| Operator probe | Stock BusyBox sh, nc, awk and timeout processed 13 synthetic loopback cases, run as subTests of one unittest method. The runner checked permitted reads, both denial controls, absent/empty fields, nested values, the wrong registration, an unexpected success, the reserved-ID branch and auth errors. Every exit status matched the expectation. It inspected stdout and stderr for both generated canaries. These cases provide no deployed statuses. |

Companion projects were built separately. Use `dotnet exec <built-test-dll>` with the following selectors and `-parallel none -noLogo`.

| Build and class selector | Cases passed |
| --- | --- |
| CLI Debug/source, -class '*.ConfigShowGoldenFileTests' -class '*.TokenRedactionTests' -class '*.CliCommandExecutorErrorTests' | 25. The golden test checks both captured streams before any diff formatter sees the bytes. |
| AccessTelemetry Debug/source, -class '*.CapabilityAndObservabilityCheckpointTests' | 75, including serialized diagnostic snapshots under synthetic failures. |
| Integration Release/package references with Aspire CLI 17.0.0, -method '*.RuntimeSecretStoreBootstrapTests.MissingBootstrap_PreventsRuntimeStoreLoadAndSuccessfulRead' | 1. This method is in the fast integration project and carries Category=Integration. |

The source-reference Debug integration restore returned NU1605. The checked-out EventStore dependency requests CommunityToolkit.Aspire.Hosting.Dapr preview.1.261120-0454 while Memories pins beta.1078. The separate Release/package build succeeded with zero warnings/errors. The server Debug source build above and the packaged integration result have different dependency graphs.

The changed-file input contained the 15 November paths enumerated in the parent story File List (current story/sprint plus the 13 implementation entries). From this worktree the commands are:

```bash
{ git diff --name-only 043fb04e -- . ':!references'; git ls-files --others --exclude-standard -- . ':!references'; } | sort -u > /tmp/31changed
python3 tools/check-tenant-isolation-evidence.py --changed-files-file /tmp/31changed
git diff --check
```

Tenant check output, verbatim:

```text
No tenant-isolation surface changed; evidence check is a no-op.
```

The diff whitespace check exited 0. At that point AC1 was blocked by cluster access.

### Resumed replay verification

The resumed run used kubectl v1.36.1 and SDK 11.0.100. The same server Debug build ran again at 15:26:20Z and succeeded with zero warnings and errors. Direct discovery again counted 2,482 server methods: 140 in Deployment, 7 migration, 45 platform and 14 production. The sorted method-set SHA-256 is `afcf3c1a2df79ed9f385e094ed0a6d31f2ffd1e3945a69d0c0c9330225d1438f`. This phase added no xUnit method. From 15:31:39Z to 15:31:45Z the three classes passed 164 cases with zero failures or skips.

A mutation added a synthetic measured name to the Current Secret names and types table. `Inventory_CoversJulyAndMeasuredNamesInBothDirections` failed because the operations inventory had no row for it. Restoring the file reproduced its SHA-256, and the method passed again.

The probe test used BusyBox 1.37.0 copied from the local busybox image and placed first on PATH. `python3 -B -m unittest discover -s tests/tooling/runtime_secret_store -p 'test_*.py'` passed at 15:32:02Z. Its single unittest method runs the 13 cases as subTests. The transport mutation is described under Probe transport defect found during replay. At 15:32:05Z `fail_closed.py` passed again with the local daprd 1.19.1. The component failed, and no read succeeded and no fallback loaded.

The changed-file input was regenerated with the command above into the session scratch directory. It listed the same 15 paths. The tenant check printed the same no-op line and exited 0. `git diff --check` exited 0. CLI, access-telemetry and packaged integration tests were not rerun, because this phase changed no product, CLI or integration source.

Review fixes followed at 16:00Z–16:03Z. `ProductProjects_DoNotAccessTheSecretProviderDirectly` now scans EventStore, ServiceDefaults and the root `Directory.Build.props`, `Directory.Build.targets` and `Directory.Packages.props`. It still excludes only AppHost, Aspire and Redis, and it skips listed files that no longer exist. The legacy-token alternative now requires a quoted literal, so a lambda parameter followed by a long member name no longer matches. `KubernetesRuntimeStore_UsesVaultBootstrapTlsAndBothScopes` now enumerates `deploy/kubernetes/overlays/*`. In each render it requires every Component with a secretKeyRef, other than the two Vault stores, to set `auth.secretStore` to one of them. It also pins the exact Vault metadata key set. The Debug build succeeded with zero warnings and errors, and the migration class passed 7 cases. Five temporary mutations each failed their selector and were restored byte-for-byte:

- an extra `caPath` key in the runtime component
- an appended Component with a secretKeyRef and no `auth.secretStore`
- a quoted `s.`-prefixed token literal in a Server source
- a provider marker in `Directory.Build.targets`
- a provider client name in ServiceDefaults

Moving a tracked Server source away left the guard passing, and the file was restored byte-for-byte. The probe test now also rejects the JSON-escaped canary, with a 20 s timeout per case, and passed again. `fail_closed.py` passed both cases at 16:00:58Z.

The deployed read, both denials, the names/types inventory and the drift comparison are current as of 2026-11-22T15:23:34Z. Residue disposal, grant narrowing, drift reconciliation and any acceptance remain with the named owners. This record claims no deployment qualification and no residue acceptance.

### Story 31.2 recheck at 17:10Z

On 2026-11-22 from 17:10:35Z to 17:12:22Z, kubectl v1.36.1 rechecked `jpiquot@local` in `hexalith-memories`. The names/types and pod app-ID projections reproduced the inventory above. At 17:13:05Z, the workload-template projections under Read-only replay again found no `eventstore` app ID.

The six in-pod calls used the commands under Read-only replay, targeting the same four pod and container pairs listed under Scoped reads and denials. Each call exited 0.

| UTC | Pods | Check | Result |
| --- | --- | --- | --- |
| 17:11:40Z–17:11:41Z | Both memories pods | `memories allowed` | `redis-secret` returned HTTP 200 with the password field present. |
| 17:11:41Z | Both memories pods | `memories outside` | HTTP 403 from the allow-list. |
| 17:11:42Z | Both memories-mcp pods | `memories-mcp unscoped` | HTTP 401 with the runtime store unavailable. Authenticated metadata confirmed the store was absent. |

The metadata-only replay under Built-in Kubernetes secret store again listed `secretstores.kubernetes` in all four pods. The live Deployment annotations and Configuration scopes provided no restriction for that store. The Role and RoleBinding projections confirmed nine named grants for `memories` and two for `memories-mcp`. All four residue Secrets still appeared in the names/types projection. C2 remains pending the owner decisions on the built-in store and those four Secrets. The probes emitted no credential values or response bodies and made no cluster changes.

The Debug source-reference build succeeded with zero warnings or errors. Direct execution of the three classes above passed all 164 cases. The BusyBox probe unittest passed its 13 subTests. The following disposable check passed both cases with daprd 1.19.1. Missing bootstrap failed component initialization, and unreachable OpenBao returned HTTP 500 without a successful read or fallback.

```bash
run-with-temp python3 -B tests/tooling/runtime_secret_store/fail_closed.py deploy/kubernetes/base/dapr/secretstore.yaml
```

The tenant isolation evidence checker printed `No tenant-isolation surface changed; evidence check is a no-op.`

### Disposable C2 policy check

A temporary copy of `deploy/kubernetes` set `storeName: kubernetes` with `defaultAccess: deny` in the four Dapr Configurations. `kubectl kustomize` rendered the production and qualification overlays. A parsed resource comparison found exactly those four added scopes in each overlay and preserved every existing resource setting, including the Vault component and its bootstrap references.

Standalone daprd 1.19.1 used the tracked runtime Vault component with a loopback TLS endpoint, a synthetic CA and a local-file bootstrap store named `kubernetes`. With the deny scope applied, the application-facing bootstrap read returned HTTP 403 and the bulk response contained no secrets. The allowed `redis-secret` read returned HTTP 200 with the expected synthetic field. The TLS endpoint required the synthetic bootstrap token, which verified that the component could still resolve its bootstrap. The fixture emitted statuses and assertions only.

The deployment uses daprd 1.18.1. Its operator resolves Kubernetes component references separately from the sidecar's application secret policy. A Kubernetes rollout and replay are required to confirm the deployed result. PG-ONPREM-2 pins the lifecycle and clock Configuration files, so adopting this candidate requires a profile decision. C2 remains pending, and the residue Secrets and grants still require owner disposition. [Dapr 1.18 secret scopes](https://v1-18.docs.dapr.io/operations/configuration/secret-scope/) and [Dapr 1.18.1 operator component resolution](https://github.com/dapr/dapr/blob/v1.18.1/pkg/operator/api/components.go#L113-L147).

### Dapr 1.19.2 verification

The C2 candidate targets Dapr 1.19.2, the latest stable 1.19 patch in Dapr's release feed. `docker buildx imagetools inspect ghcr.io/dapr/daprd:1.19.2` resolved the image index to `sha256:7839fbb66d4be24ec52e347bec2dca66a91bef3079d8c8d57676f6d3ef92654f`. [Dapr 1.19.2 release](https://github.com/dapr/dapr/releases/tag/v1.19.2)

The fail-closed harness ran with `DAPRD` set to the `/daprd` binary extracted from `daprio/dapr:1.19.2`. The binary reported `1.19.2`. Missing bootstrap failed component initialization. An unreachable Vault endpoint with synthetic bootstrap returned HTTP 500. Neither case returned a successful read or loaded a fallback.

A disposable kind cluster ran `dapr init -k --runtime-version 1.19.2 --set global.scheduler.placement.enabled=true --wait --timeout 170`. It used the two tracked Vault components, a TLS fixture at the configured service name, synthetic Kubernetes bootstrap Secrets, and four injected Deployments with the tracked app IDs and Configurations. The only Configuration changes were the four deny scopes. Each sidecar reported runtime version `1.19.2`. Kubernetes RBAC granted the pod ServiceAccount access to a synthetic Secret. Application requests through the registered built-in `kubernetes` store still returned HTTP 403 in all four sidecars.

| App ID | Scoped Vault result |
| --- | --- |
| `memories` | Runtime `redis-secret` and telemetry `access-telemetry-marker-key` returned HTTP 200 with the expected synthetic field. An out-of-list name returned HTTP 403. |
| `memories-mcp` | Neither Vault component was registered. |
| `memories-access-telemetry` | Telemetry `access-telemetry-marker-key` returned HTTP 200 with the expected synthetic field. |
| `memories-access-telemetry-clock` | Telemetry `access-telemetry-clock-key` returned HTTP 200 with the expected synthetic field. |

The TLS fixture required the corresponding bootstrap token for each prefix. Successful Vault reads therefore exercised the Kubernetes operator's bootstrap resolution with the deny scopes in place. The probes emitted only app IDs, status codes, and counts. This run used the base component scopes and synthetic infrastructure. C2 still requires approval of the deployment profile, an authorized rollout and replay, and disposition of the four residue Secrets and grants.

### Dapr 1.19.2 rollout preflight

The owner approved preparing the Dapr 1.19.2 deployment and explicitly authorized the shared `jpiquot@local` control-plane scope, including coordination of the `ext-secrets-1-isolated` sidecars. The read-only preflight found the shared control plane healthy on Dapr 1.18.1. Both namespaces had ready Dapr workloads. The Kubernetes server reported v1.34.9 and the local kubectl reported v1.36.1, two minors ahead. The cluster exposed no `snapshot.storage.k8s.io` API resources. The four residual Kubernetes Secrets were still present.

`kubectl kustomize deploy/kubernetes/overlays/production-dapr-1.19` renders the candidate. A parsed comparison against `production` found exactly four Configuration deny scopes, four digest-pinned Dapr sidecar images, and two Role changes that remove the residual grants. The existing production and qualification sources used by PG-ONPREM-2 are preserved. The candidate inherits the production settings, including the parked lifecycle workload, and has a different rendered profile from the live cluster. Its rollout requires a reviewed live diff and the release image inputs.

The Debug source-reference build and all 164 focused deployment cases passed. `kubectl` v1.34.9 matched the server version and passed ten targeted `--dry-run=server` patches against the four live Configurations, four Deployments, and two Roles. Those patches retained the live lifecycle bootstrap grant where it still serves `memories`.

`validate_current_profile_inputs()` independently refused the existing PG-ONPREM-2 checkout with `approved PG-ONPREM-2 input drifted: deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml`. A direct check compared all sixteen hashed inputs byte-for-byte with HEAD. The production disabled patch and production kustomization already differed from their historical approved hashes at HEAD. The new overlay preserves those inputs and has its own render.

The approved recovery-point and restore-rehearsal evidence and the deployment's intake-quiescence and workflow-drain record were unavailable in this preflight. The cluster rollout remains pending under the [upgrade and migration runbook](../../../docs/operations/upgrade-migration.md#3-backup-rehearse-and-quiesce). The live Dapr control plane, workloads, Configurations, Roles, and Secrets were inspected read-only.
