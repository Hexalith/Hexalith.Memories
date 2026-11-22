# Story 31.2 runtime secret-store evidence

## Deployment preflight

On 2026-11-22T14:44:27Z–14:44:28Z, the requested context was `jpiquot@local` and the namespace was `hexalith-memories`. The following commands used the workspace kubectl. Each returned exit 1.

```sh
kubectl config current-context
kubectl config get-contexts jpiquot@local -o name
kubectl --context=jpiquot@local -n hexalith-memories --request-timeout=5s get pods -o 'custom-columns=NAME:.metadata.name,APP:.metadata.annotations.dapr\.io/app-id'
```

The client was kubectl v1.35.0. The respective errors were `current-context is not set`, `context jpiquot@local not found` and `context "jpiquot@local" does not exist`. No pod names, deployed app IDs, live component metadata, current Secret names/types or HTTP statuses were observed. The current runtime read, both denials, reserved-scope workload check, inventory and deployed drift comparison remain open. Platform Operations owns access and the drift comparison. Reopen when the approved read-only context is available or the component changes.

## Current Secret names and types

Preflight failed before enumeration. The empty table records missing evidence. Its rows must be filled from the names/types projection during replay.

| Secret | Type |
| --- | --- |

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

Run from the repository root. Record UTC start/end times, context, namespace, selected pods and app IDs with each result. Stop at an unavailable context. The following are replay commands. No live result is attributed to them here.

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

Select a ready app container from the pod list for each ID. Stream the probe to the existing BusyBox shell inside that container, with its existing DAPR_API_TOKEN and loopback sidecar. The probe reads the authenticated metadata endpoint first to confirm the app ID and registered component. It parses the secret response in memory and emits only public names, presence, HTTP status and a bounded outcome. It needs BusyBox awk, nc and timeout. The HTTP/1.0 parser rejects unsupported transfer encodings and unrecognized JSON. Record missing tools or unrecognized responses as inconclusive.

```bash
probe=tools/probe-dapr-secret-scope.sh
"${k[@]}" exec -i "$memories_pod" -c memories -- sh -s -- memories allowed < "$probe"
"${k[@]}" exec -i "$mcp_pod" -c memories-mcp -- sh -s -- memories-mcp unscoped < "$probe"
"${k[@]}" exec -i "$memories_pod" -c memories -- sh -s -- memories outside < "$probe"
# Required only when the workload inventory includes the reserved app ID.
"${k[@]}" exec -i "$eventstore_pod" -c "$eventstore_app_container" -- sh -s -- eventstore allowed < "$probe"
```

The positive request needs HTTP 200 and a nonempty password field under redis-secret with the Vault component registered. The MCP request uses the same key. Compare its authenticated metadata and deployed component scopes to distinguish an excluded store from a component that failed globally. If Dapr reports a name-policy denial, attribute it to that policy and record the independent component exclusion from metadata. The memories outside-name probe requests access-telemetry-marker-key via the runtime store and needs an explicit name-policy denial. An empty success, absent key, bad token, transport error or unknown error cannot satisfy a denial. Recheck with an actual eventstore pod upon deployment.

## Disposable failure and archived CI

The local bootstrap-failure test starts daprd 1.19.1 with app ID memories and the tracked Vault component in a temporary directory. Only the resolver and destination are isolated. An empty local bootstrap store cannot supply openbao-runtime-bootstrap. The sidecar reported the missing bootstrap and a fatal Vault component initialization failure, exited nonzero, and exposed no successful read or loaded replacement. The test polls the named Secrets API during startup and after exit. Run `python3 tests/tooling/runtime_secret_store/fail_closed.py deploy/kubernetes/base/dapr/secretstore.yaml` with daprd on PATH. This lane uses no live credentials.

DW-726 (CR29) and DW-727 (CR30) were resolved on September 2 against staged OpenBao. Run 33400812038, job 99516369413 and artifact 9761351293 are the register's references. The successful deployment job was independently checked then. The packet details, including resolution through the production component type, came from the August transcription. The register discloses that the packet was not downloaded and reparsed in that review. This November run adds no independent packet verification or current deployed read to either disposition.

## Verification on November 22

The SDK was 11.0.100, kubectl was v1.35.0, and the disposable sidecar was 1.19.1. The workspace used its cached NuGet packages. From the repository root:

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
| Product boundary | ProductProjects_DoNotAccessTheSecretProviderDirectly scanned the 12 product projects, including DomainService and EventStoreExtensions. The five named topology/infrastructure projects were excluded. Temporary changes added a provider SDK package to Contracts, and a client constructor, endpoint, configuration key and token-shaped literal to Server. Each focused runner invocation exited 1 with one failure. No injected value appeared in its diagnostics. Every changed file was restored byte-for-byte. |
| Deployment fallback | Temporarily replacing the tracked runtime type with the Kubernetes store type failed KubernetesRuntimeStore_UsesVaultBootstrapTlsAndBothScopes. Restoring the original bytes returned the selector to green. The independent disposable test observed absent bootstrap and fatal Vault initialization at 14:45:33Z during development, followed by the passing packaged integration execution. |
| Published evidence | Inserting an unlabelled synthetic base64 run containing slashes and padding into this evidence file failed PublishedMigrationEvidence_ContainsNoSecretMaterial. The diagnostic gave the category and count only. The input file was restored byte-for-byte. The shared detector also covers PEM, current/legacy token prefixes, operator-init labels, UUIDs and long hex runs. |
| Resolved credential at outputs | A Dapr client double supplied a fresh synthetic value. The embedding provider received it and returned it in a simulated HTTP failure. The actual transport, exception logger, structured audit telemetry and serialized public error snapshot were exercised. Removing the transport's response redaction caused the captured logger assertion to fail. The full runner output contained no synthetic value. The source was restored byte-for-byte and the test passed again. |
| Operator probe | Stock BusyBox sh, nc, awk and timeout processed 13 synthetic loopback cases. The runner checked permitted reads, both denial controls, absent/empty fields, nested values, the wrong registration, an unexpected success, the reserved-ID branch and auth errors. Every exit status matched the expectation. It inspected stdout and stderr for both generated canaries. These cases provide no deployed statuses. |

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

The diff whitespace check exited 0. Current AC1 remains blocked by cluster access. Memories Maintainer and Platform Operations own the live replay and the residue review. No present-tense deployment qualification or residue acceptance was recorded.
