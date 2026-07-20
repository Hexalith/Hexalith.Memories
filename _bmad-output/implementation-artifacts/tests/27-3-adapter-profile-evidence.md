# Story 27.3 C1 Adapter Profile Evidence

- captured_utc: `2026-07-20T07:23:03Z`
- checkpoint: `adapter-profile`
- status: `rejected`
- rejection_reason: the PostgreSQL server-installation subset passed, but the complete no-skip C1 qualification has not run; the live Dapr component remains Redis, lifecycle/clock images remain placeholders at zero replicas, the exposed pre-existing OpenBao access token must be rotated before seeding the PostgreSQL connection, and backup/restore, workload/capacity/reclamation, full Dapr behavior, and separate approvals are absent
- production_lifecycle_writes: `disabled`
- evidence_is_approval: `false`

## Reviewed Identity

- kube_context: `jpiquot@local`
- kube_namespace: `hexalith-memories`
- deployment_id: `memories-production-2.11.0-c7c2ca21-hexalith-keys-r2`
- profile_id: `postgresql-v2-dapr-1.18.1-postgresql-18.4-onprem-k8s1-openebs-local-retain-400g-v1`
- evidence_root: `/home/administrator/projects/hexalith/memories/_bmad-output/implementation-artifacts/tests`
- declared_single_component_fault: `postgresql-container-or-process-loss-and-statefulset-pod-replacement-while-node1-and-the-retained-local-volume-remain-healthy`
- assurance_limit: `single-node, single-instance, non-HA; node, local-volume, control-plane, and site loss are outside profile`

## Immutable Profile Material

- combined_profile_sha256: `0952992388e7004aeb287a442b6b55c416a7b6db4bad55b931b713bf1a699c3c`
- collector_profile_sha256: `dbe2eb0e50c7f8144e769fcae5c9fded1fed6e04c167e288f52e186916e3ede6`
- collector_mutation_manifest_sha256: `e2ff545e79639245ae33be115be808ff321c476dbace963b574d27484fa0f590`
- rendered_production_sha256: `e2c9a5cd4536b740ecc629fb3d362123abf02d911b205f067a0b6c4bf8de8942`
- postgresql_manifest_sha256: `aa5581f06c9c6d1dbd3c74cfebe2be5c5079d6188e3369cfb4244439d84b3405`
- network_policy_sha256: `ae076135fe09c437fc620f491d23fb29d7b3f187cdc93dbd0f19d804f7f41c4f`
- lifecycle_deployment_sha256: `2e23562abe3b376914efaaf109cd34ba58f440f7296e430b39ba47d4e25e48e5`
- dapr_component_sha256: `ffe404bcb2ae600191bae2b98189cb61b3717c97e92548d595efad14da38597d`
- adapter_source_sha256: `d250bc1616e3039726121182618d7132a50be15a4d0dc796becd08b158ffe4a5`
- expiry_bucket_source_sha256: `e8d9e16a841e028818048c11a7affa5e47a3f7faccd1e199a072e4ab5e5a50bc`
- expiry_catalog_source_sha256: `fe530ba4b5e3d7b05ed05d7442ac82ab714172f22dca8d88b79306d3fb0c0074`
- collector_source_sha256: `a5badbdb51b33d65c8ce3907de5272c1354198adf1a49c4d8ce4300af0778677`
- collector_entrypoint_sha256: `6aeda3ec341e45c768cf8960a7369e55e02be4b328f73bec82c1119881a72e9c`
- collector_tests_sha256: `1f0164c2ffbce9be1d39cbf17931ca8a573ad024dc229c8ffac72f8d96b77f62`
- allowed_mutations: `[]`

## Installed Server Observations

| Observation | Result |
| :-- | :-- |
| PostgreSQL | `18.4 (Debian 18.4-1.pgdg13+1)` |
| requested and resolved image | `docker.io/library/postgres:18.4-trixie@sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a` |
| registry linux/amd64 manifest | `sha256:d93de42662696f278fb34354b06fdaa90ad7ca3106d6f72fbd01d16da006d2cf` |
| StatefulSet | `access-telemetry-postgresql`, `1/1` Ready on `node1`, final pod restart count `0` |
| PVC | `data-access-telemetry-postgresql-0`, Bound `400Gi`, `openebs-hostpath-retain`, PV `pvc-31258a19-5814-48c5-8209-1d92f2e6f8ed`, reclaim policy `Retain` |
| service | `ClusterIP`, port `5432`, no external IP |
| durability settings | data checksums `on`; `fsync=on`; `synchronous_commit=on`; `full_page_writes=on` |
| transport | hostname-verified TLS succeeded with TLS 1.3 / `TLS_AES_256_GCM_SHA384`; plaintext connection rejected |
| certificate | SHA-256 fingerprint `A1:27:02:AF:F5:48:ED:10:72:DB:BD:7A:D0:3E:7D:56:D4:48:AB:AA:51:93:A2:08:70:49:7D:01:DF:79:A2:19`; expires `2028-10-22T07:11:18Z` |
| database boundary | dedicated `memories_access_telemetry` database and `access_telemetry` schema; runtime role has CONNECT/USAGE/CREATE and is not superuser |
| network isolation | labeled verifier reached port 5432; an otherwise identical unlabeled pod was blocked; both temporary pods were removed |
| pod replacement | a non-sensitive probe row survived deletion/recreation of the StatefulSet pod on the same retained PV, was read after recovery, and was removed |
| protected material | the newly created PostgreSQL passwords and CA/server keypair were rotated after a diagnostic exposed their earlier base64 encodings; the superseded database passwords are invalid and the superseded certificate is no longer mounted |
| existing Keycloak PostgreSQL | deployment/PVC UIDs and resource versions remained unchanged before and after installation |

## Adapter Compatibility Verification

- Dapr PostgreSQL v2 does not implement Query State. The lifecycle adapter now uses deterministic `expiry-catalog` and `expiry-bucket/{minute}/{shard}` state committed transactionally with each record.
- Focused AccessTelemetry tests passed `61/61`, including atomic record/bucket/catalog writes, explicit due traversal without Query State, delete/prune behavior, and idempotent retries.
- Focused deployment and architecture guards passed `17/17`.
- The complete production Kustomize render passed Kubernetes server-side dry-run.
- The exact C1 Python inventory passed `4/4`; the aligned read-only collector recognized the immutable `PG-ONPREM-1` server identity and exited `1` at the expected fail-closed lifecycle-disabled gate.
- The checked-in component is `state.postgresql/v2`, has no `queryIndexes`, resolves its connection through OpenBao, and mounts the PostgreSQL CA read-only into the Dapr sidecar for `sslRootCert` verification.

## Remaining C1 Gates

- Rotate the pre-existing `openbao-access-telemetry-bootstrap` token through the documented rolling-restart procedure, then seed `access-telemetry-postgresql/connectionString` without exposing it.
- Publish immutable lifecycle and clock application image digests; replace the `0.0.0` placeholders only in a reviewed qualification run.
- Apply and exercise the PostgreSQL v2 Dapr component with the actual Dapr 1.18.1 sidecar and prove CRUD, strong reads, ETags, rollback-atomic transactions, TTL, actors, Placement/Scheduler, reminders, bounds, and failure behavior.
- Run the exact two-writer 500 events/s steady-state and 150,000-record purge workload; measure capacity operands, latency, catch-up, physical reclamation, and local-host headroom.
- Configure a named off-cluster backup destination and complete a successful restore, publishing bounded nonzero RPO/RTO without an HA claim.
- Obtain separate hash-bound Platform Operations and security approvals.

The packet intentionally stores public certificate identity, hashes, and structural results only. It stores no password, token, private key, connection string, raw Secret data, or pod environment value.
