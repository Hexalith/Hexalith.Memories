# Story 27.3 C1 Adapter Profile Evidence

- captured_utc: `2026-07-26T16:34:39.690049+00:00`
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
- declared_single_component_fault: `postgresql-pod-process-loss-retained-local-volume`

## Immutable Profile Material

- profile_sha256: `01f6ce9f1cdf6fad022c6f86942d0a103c1685fcc9a54c47efc48ac58162d6cc`
- mutation_manifest_sha256: `04c034b9e8c8012f4813d52877042adbda9edc385f32d87c36f2019ecc3e61da`
- allowed_mutations: `[]`
- profile_hash_covers: `runtime-observed profile constructed by the invocation that produced this packet` (recorded 2026-07-26 by code review, chunk 3b)
- reviewed_canonical_profile_sha256: `dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14`
- runtime_matches_reviewed_profile: `unknown for this packet` — this capture predates the pinned canonical profile, and the live cluster is known to diverge from the reviewed manifests (the running `access-telemetry-store` still carries `sslRootCert`, removed from the repository manifest by the chunk-2 patch, and the pre-change `maxConns`). The cluster has not been re-applied. Until it is, `profile_sha256` above binds neither the reviewed manifests nor an approved profile, and no AC4 approval may be recorded against it.

## Safe Deployment Observations

| Observation | Value |
| :-- | :-- |
| deployments | `[{"available_replicas":2,"generation":4,"images":["registry.hexalith.com/memories@sha256:71e49b6e806ec2fa7c221e58600ba02115693923db05915663396be01b1c042c"],"name":"memories","namespace":"hexalith-memories","ready_replicas":2,"replicas":2,"resource_version":"1301790"},{"available_replicas":0,"generation":7,"images":["registry.hexalith.com/memories-access-telemetry@sha256:b3790e08c0091dfb723f6819e4706a6b1ef36a051b8c4afe38f5e362c5bdf8ea"],"name":"memories-access-telemetry","namespace":"hexalith-memories","ready_replicas":0,"replicas":0,"resource_version":"1308992"},{"available_replicas":0,"generation":5,"images":["registry.hexalith.com/memories-access-telemetry-clock@sha256:50413b716ca051fad7c4d68119527a1fc5f64c566eea22827769fd85d9de4c70"],"name":"memories-access-telemetry-clock","namespace":"hexalith-memories","ready_replicas":0,"replicas":0,"resource_version":"1308994"},{"available_replicas":2,"generation":2,"images":["registry.hexalith.com/memories-mcp@sha256:4d5cd738a89fdca71b7202d7661cecea8bab6b6e45d4fe505670cf7287205bb7"],"name":"memories-mcp","namespace":"hexalith-memories","ready_replicas":2,"replicas":2,"resource_version":"1301976"}]` |
| components | `[{"generation":1,"metadata_names":["redisHost","redisPassword"],"name":"access-telemetry-config","namespace":"hexalith-memories","resource_version":"855267","scopes":["memories","memories-access-telemetry"],"type":"configuration.redis","version":"v1"},{"generation":2,"metadata_names":["vaultAddr","caPem","skipVerify","tlsServerName","vaultToken","vaultKVPrefix","vaultKVUsePrefix","enginePath","vaultValueType"],"name":"access-telemetry-secrets","namespace":"hexalith-memories","resource_version":"880742","scopes":["memories","memories-access-telemetry","memories-access-telemetry-clock"],"type":"secretstores.hashicorp.vault","version":"v1"},{"generation":2,"metadata_names":["connectionString","sslRootCert","tablePrefix","metadataTableName","timeout","cleanupInterval","maxConns","connectionMaxIdleTime","actorStateStore"],"name":"access-telemetry-store","namespace":"hexalith-memories","resource_version":"1302834","scopes":["memories-access-telemetry"],"type":"state.postgresql","version":"v2"},{"generation":1,"metadata_names":["key","model","responseCacheTTL"],"name":"llm-openai","namespace":"hexalith-memories","resource_version":"855309","scopes":["memories"],"type":"conversation.openai","version":"v1"},{"generation":1,"metadata_names":["redisHost","redisPassword","allowedTopics","protectedTopics","publishingScopes","subscriptionScopes"],"name":"pubsub","namespace":"hexalith-memories","resource_version":"855314","scopes":["eventstore","memories"],"type":"pubsub.redis","version":"v1"},{"generation":2,"metadata_names":["vaultAddr","caPem","skipVerify","tlsServerName","vaultToken","vaultKVPrefix","vaultKVUsePrefix","enginePath","vaultValueType"],"name":"secretstore","namespace":"hexalith-memories","resource_version":"880741","scopes":["eventstore","memories"],"type":"secretstores.hashicorp.vault","version":"v1"},{"generation":1,"metadata_names":["redisHost","redisPassword","actorStateStore"],"name":"statestore","namespace":"hexalith-memories","resource_version":"855317","scopes":["memories"],"type":"state.redis","version":"v1"}]` |
| configurations | `[{"access_control_default":"deny","access_control_policy_count":2,"features":[],"generation":1,"name":"memories-access-telemetry-clock-config","namespace":"hexalith-memories","resource_version":"855318","secret_scope_count":1},{"access_control_default":"deny","access_control_policy_count":2,"features":[{"enabled":false,"name":"HotReload"}],"generation":2,"name":"memories-access-telemetry-config","namespace":"hexalith-memories","resource_version":"872776","secret_scope_count":1},{"access_control_default":"deny","access_control_policy_count":1,"features":[{"enabled":false,"name":"HotReload"}],"generation":2,"name":"memories-config","namespace":"hexalith-memories","resource_version":"872775","secret_scope_count":2},{"access_control_default":"deny","access_control_policy_count":0,"features":[],"generation":1,"name":"memories-mcp-config","namespace":"hexalith-memories","resource_version":"855321","secret_scope_count":1}]` |
| statefulsets | `[{"generation":3,"images":["docker.io/library/postgres:18.4-trixie@sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a"],"name":"access-telemetry-postgresql","namespace":"hexalith-memories","ready_replicas":1,"replicas":1,"resource_version":"1307292"},{"generation":2,"images":["falkordb/falkordb:v4.12.0@sha256:7927eb194df0fadf70e2deb6c20c8178743ec92aacd739c790f8a35ce5dee613"],"name":"falkordb","namespace":"hexalith-memories","ready_replicas":1,"replicas":1,"resource_version":"1301078"},{"generation":3,"images":["redis/redis-stack-server:7.4.0-v8@sha256:798ab84d9f266936b034ab11c4d04a2b8e4b441884c5aa7d17ac951eefdf742a"],"name":"redis-stack","namespace":"hexalith-memories","ready_replicas":1,"replicas":1,"resource_version":"1300979"}]` |
| pods | `[{"container_images":["ghcr.io/dapr/daprd:1.18.1","sha256:c68e099f4beefcdd008de4d1dbdc70b3fc1c84ba2481b2c40c8d519eeaa4fa5f"],"generation":1,"name":"memories-b667844cf-6s9j7","namespace":"hexalith-memories","node":"node1","phase":"Running","resource_version":"1301685"},{"container_images":["ghcr.io/dapr/daprd:1.18.1","sha256:c68e099f4beefcdd008de4d1dbdc70b3fc1c84ba2481b2c40c8d519eeaa4fa5f"],"generation":1,"name":"memories-b667844cf-bs4gm","namespace":"hexalith-memories","node":"node1","phase":"Running","resource_version":"1301769"}]` |

## Read-only Child Commands

| Command | Exit | Stdout SHA-256 | Stderr SHA-256 | Result |
| :-- | --: | :-- | :-- | :-- |
| `kubectl --context jpiquot@local --namespace hexalith-memories get deployments -o json ` | 0 | `946daec4673995619631c6fbc0a489c5f3b47bd7ee5ce1c413ab4a01a90ba3a6` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | ok |
| `kubectl --context jpiquot@local --namespace hexalith-memories get components.dapr.io -o json ` | 0 | `4722d3b30052c9f18e7bd164094b689ac9d4851ce6cf0f244b9bf97716a3d767` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | ok |
| `kubectl --context jpiquot@local --namespace hexalith-memories get configurations.dapr.io -o json ` | 0 | `a2e2031e8c550b7af09ca31a15881bc5eebb48bc16805beb8ebec75a16d3a13e` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | ok |
| `kubectl --context jpiquot@local --namespace hexalith-memories get statefulsets -o json ` | 0 | `42b485993580af61a5261c6944d7a5e8c22989f8d210bd7ff4d9c92640d4a752` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | ok |
| `kubectl --context jpiquot@local --namespace hexalith-memories get pods -l app.kubernetes.io/name=memories -o json ` | 0 | `7b98c47c4d8cc5dab0c02f64ae2791171c556a351228a59f93140b95068d1d69` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | ok |

The packet intentionally stores hashes and structural metadata only; it does not store secret values, backend credentials, or raw pod environment data.
