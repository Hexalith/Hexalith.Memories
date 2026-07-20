# Story 27.3 C1 Adapter Profile Evidence

- captured_utc: `2026-07-20T11:21:32.846536+00:00`
- checkpoint: `adapter-profile`
- status: `rejected`
- rejection_reason: lifecycle deployment is disabled; Production writes remain fail-closed
- production_lifecycle_writes: `disabled`
- evidence_is_approval: `false`

## Reviewed Identity

- kube_context: `jpiquot@local`
- kube_namespace: `hexalith-memories`
- deployment_id: `memories-production-2.11.0-c7c2ca21-hexalith-keys-r2`
- profile_id: `postgresql-v2-dapr-1.18.1-postgresql-18.4-onprem-k8s1-openebs-local-retain-400g-v1`
- evidence_root: `/home/administrator/projects/hexalith/memories/_bmad-output/implementation-artifacts/tests`
- declared_single_component_fault: `postgresql-pod-process-loss-with-node1-and-retained-volume-healthy`

## Immutable Profile Material

- profile_sha256: `fba5b2ff22fcce7130e513e1ec94c73d8c97984f3857c6f80473386041c124c8`
- mutation_manifest_sha256: `450e4a751038e5578208eb287d6478d86e87ebe0ad18cbabfddf7ad32d6a9560`
- allowed_mutations: `[]`

## Safe Deployment Observations

| Observation | Value |
| :-- | :-- |
| deployments | `[{"available_replicas":2,"generation":3,"images":["registry.hexalith.com/memories@sha256:71e49b6e806ec2fa7c221e58600ba02115693923db05915663396be01b1c042c"],"name":"memories","namespace":"hexalith-memories","ready_replicas":2,"replicas":2,"resource_version":"880895"},{"available_replicas":0,"generation":1,"images":["registry.hexalith.com/memories-access-telemetry:0.0.0"],"name":"memories-access-telemetry","namespace":"hexalith-memories","ready_replicas":0,"replicas":0,"resource_version":"855240"},{"available_replicas":0,"generation":1,"images":["registry.hexalith.com/memories-access-telemetry-clock:0.0.0"],"name":"memories-access-telemetry-clock","namespace":"hexalith-memories","ready_replicas":0,"replicas":0,"resource_version":"855251"},{"available_replicas":2,"generation":1,"images":["registry.hexalith.com/memories-mcp@sha256:4d5cd738a89fdca71b7202d7661cecea8bab6b6e45d4fe505670cf7287205bb7"],"name":"memories-mcp","namespace":"hexalith-memories","ready_replicas":2,"replicas":2,"resource_version":"881057"}]` |
| components | `[{"generation":1,"metadata_names":["redisHost","redisPassword"],"name":"access-telemetry-config","namespace":"hexalith-memories","resource_version":"855267","scopes":["memories","memories-access-telemetry"],"type":"configuration.redis","version":"v1"},{"generation":2,"metadata_names":["vaultAddr","caPem","skipVerify","tlsServerName","vaultToken","vaultKVPrefix","vaultKVUsePrefix","enginePath","vaultValueType"],"name":"access-telemetry-secrets","namespace":"hexalith-memories","resource_version":"880742","scopes":["memories","memories-access-telemetry","memories-access-telemetry-clock"],"type":"secretstores.hashicorp.vault","version":"v1"},{"generation":1,"metadata_names":["redisHost","redisPassword","actorStateStore","queryIndexes"],"name":"access-telemetry-store","namespace":"hexalith-memories","resource_version":"855299","scopes":["memories-access-telemetry"],"type":"state.redis","version":"v1"},{"generation":1,"metadata_names":["key","model","responseCacheTTL"],"name":"llm-openai","namespace":"hexalith-memories","resource_version":"855309","scopes":["memories"],"type":"conversation.openai","version":"v1"},{"generation":1,"metadata_names":["redisHost","redisPassword","allowedTopics","protectedTopics","publishingScopes","subscriptionScopes"],"name":"pubsub","namespace":"hexalith-memories","resource_version":"855314","scopes":["eventstore","memories"],"type":"pubsub.redis","version":"v1"},{"generation":2,"metadata_names":["vaultAddr","caPem","skipVerify","tlsServerName","vaultToken","vaultKVPrefix","vaultKVUsePrefix","enginePath","vaultValueType"],"name":"secretstore","namespace":"hexalith-memories","resource_version":"880741","scopes":["eventstore","memories"],"type":"secretstores.hashicorp.vault","version":"v1"},{"generation":1,"metadata_names":["redisHost","redisPassword","actorStateStore"],"name":"statestore","namespace":"hexalith-memories","resource_version":"855317","scopes":["memories"],"type":"state.redis","version":"v1"}]` |
| configurations | `[{"access_control_default":"deny","access_control_policy_count":2,"features":[],"generation":1,"name":"memories-access-telemetry-clock-config","namespace":"hexalith-memories","resource_version":"855318","secret_scope_count":1},{"access_control_default":"deny","access_control_policy_count":2,"features":[{"enabled":false,"name":"HotReload"}],"generation":2,"name":"memories-access-telemetry-config","namespace":"hexalith-memories","resource_version":"872776","secret_scope_count":1},{"access_control_default":"deny","access_control_policy_count":1,"features":[{"enabled":false,"name":"HotReload"}],"generation":2,"name":"memories-config","namespace":"hexalith-memories","resource_version":"872775","secret_scope_count":2},{"access_control_default":"deny","access_control_policy_count":0,"features":[],"generation":1,"name":"memories-mcp-config","namespace":"hexalith-memories","resource_version":"855321","secret_scope_count":1}]` |
| statefulsets | `[{"generation":2,"images":["docker.io/library/postgres:18.4-trixie@sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a"],"name":"access-telemetry-postgresql","namespace":"hexalith-memories","ready_replicas":1,"replicas":1,"resource_version":"1117577"},{"generation":1,"images":["falkordb/falkordb:v4.12.0@sha256:7927eb194df0fadf70e2deb6c20c8178743ec92aacd739c790f8a35ce5dee613"],"name":"falkordb","namespace":"hexalith-memories","ready_replicas":1,"replicas":1,"resource_version":"855559"},{"generation":1,"images":["redis/redis-stack-server:7.4.0-v8@sha256:798ab84d9f266936b034ab11c4d04a2b8e4b441884c5aa7d17ac951eefdf742a"],"name":"redis-stack","namespace":"hexalith-memories","ready_replicas":1,"replicas":1,"resource_version":"855596"}]` |
| pods | `[{"container_images":["ghcr.io/dapr/daprd:1.18.1","sha256:c68e099f4beefcdd008de4d1dbdc70b3fc1c84ba2481b2c40c8d519eeaa4fa5f"],"generation":1,"name":"memories-589ff6d645-fp9qt","namespace":"hexalith-memories","node":"node1","phase":"Running","resource_version":"880875"},{"container_images":["ghcr.io/dapr/daprd:1.18.1","sha256:c68e099f4beefcdd008de4d1dbdc70b3fc1c84ba2481b2c40c8d519eeaa4fa5f"],"generation":1,"name":"memories-589ff6d645-kr9f9","namespace":"hexalith-memories","node":"node1","phase":"Running","resource_version":"880795"}]` |

## Read-only Child Commands

| Command | Exit | Stdout SHA-256 | Stderr SHA-256 | Result |
| :-- | --: | :-- | :-- | :-- |
| `kubectl --context jpiquot@local --namespace hexalith-memories get deployments -o json ` | 0 | `e8aab535b36d748e452da087368f1dec16a8e63cac4d71cde87527ec728fd958` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | ok |
| `kubectl --context jpiquot@local --namespace hexalith-memories get components.dapr.io -o json ` | 0 | `7e63ac35f4db121603f052c3d4a6e7465b059cca91474de40d5302734fc3b8ef` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | ok |
| `kubectl --context jpiquot@local --namespace hexalith-memories get configurations.dapr.io -o json ` | 0 | `a2e2031e8c550b7af09ca31a15881bc5eebb48bc16805beb8ebec75a16d3a13e` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | ok |
| `kubectl --context jpiquot@local --namespace hexalith-memories get statefulsets -o json ` | 0 | `36d8a017901e7203e12d1f2558b59d647677d5b2ac51dce36d842e9f27a68df2` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | ok |
| `kubectl --context jpiquot@local --namespace hexalith-memories get pods -l app.kubernetes.io/name=memories -o json ` | 0 | `e1c629c9d52c249a816b56958eaf9e11ae4c490c260a2c9af571aa390fbba323` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | ok |

The packet intentionally stores hashes and structural metadata only; it does not store secret values, backend credentials, or raw pod environment data.
