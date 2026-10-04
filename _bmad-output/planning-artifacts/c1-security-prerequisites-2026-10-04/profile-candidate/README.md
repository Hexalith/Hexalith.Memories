# Exact PG-ONPREM-2 Candidate for D2 Review

Status: inactive byte proposal. D1-D4 repository implementation and version direction are approved; this exact revision awaits its separately reserved review before adoption. No deployment, target observation, Production enablement or gate pass is claimed.

Candidate profile SHA-256: `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`.
Historical PG-ONPREM-1 SHA-256: `dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14`.
Source revision: `5b43fe2f8a0f04dc021921a077dff1a573c2ce5e`.

[Candidate profile](candidate-profile.json), [retained historical profile](historical-pg-onprem-1.json), [exact public response bytes](registry-source-evidence.json), [artifact acquisition](artifact-records.json), [Dapr identity](dapr-authentication-summary.json), [render receipts](commands-and-results.json), and [semantic render comparison](render-semantic-changes.json) provide the review inputs.

## Concrete repository inputs

| Inactive proposal | Eventual owning surface | Change |
| --- | --- | --- |
| [OpenBao values](openbao-values.candidate.yaml) | `deploy/openbao/values.yaml` | Chart 0.29.6 and verified 2.6.4 image; preferred host anti-affinity weight 100. |
| [PostgreSQL workload](postgresql.candidate.yaml) | `deploy/kubernetes/base/access-telemetry-postgresql.yaml` | Verified 18.6-trixie image and explicit successor profile identity; retained storage, TLS, peer/SCRAM, pool and durability settings. |
| [Lifecycle/clock workloads](lifecycle-deployments.candidate.yaml) | `deploy/kubernetes/base/access-telemetry-deployments.yaml` | Pin the verified Dapr 1.18.1 image; both replica counts remain 0. |
| [Qualification reporter](physical-reporter.candidate.yaml) | `deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml` | Same verified Dapr 1.18.1 sidecar pin. |

These files are outside every active Helm/Kustomize resource list. Adoption must update the active profile constructors/allowlists, source-bound validators, affected documentation and focused tests coherently. Current tools reject PG-ONPREM-2; copying a candidate file does not authorize target use.

## Exact public artifact identities

| Artifact | Image-index or chart SHA-256 | Linux/amd64 manifest SHA-256 |
| --- | --- | --- |
| PostgreSQL 18.6-trixie | `5a5a84b19854a9ffaa54082c166ff4ec27473a361e496e5ea167f298f2da9722` | `0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04` |
| OpenBao 2.6.4 | `cf2340fc9a22cb9358ca0defd1f39b65673836bd23fe2bb8984a07e11fe13ef4` | `bd3e8b6b67b5c4c3fc1064cd0f86eb8063d9ed92a7af608b6408d6748e6eef64` |
| Dapr 1.18.1 | `b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8` | `edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b` |
| OpenBao chart 0.29.6 OCI manifest | `98c8fc901e2579ac6da9a805537fcd7a19525ef8e563ae8737dc16fc8f641e3e` | Archive content `8079e985bdf608f965ada59c70051693d14dd2454ac16311229f367d0c48c4b9` |

Registry response bytes were hashed against content-digest headers and platform/config descriptors; the chart archive matched the first-party GitHub asset and published digest. The archived PostgreSQL 18.4 index separately authenticates the exact legacy index/child pair accepted by C1.16. These are public content-identity checks, without image execution or a signature attestation claim.

## Profile coverage and unchanged boundaries

The proposed canonical identity binds PostgreSQL/Dapr image and platform identities, OpenBao image/chart/values/render, twelve current component/secret/configuration/hardening inputs and the three proposed workload files. It also binds the inherited capability/workload declarations and explicit capacity/fault limits. Canonical capability flags are requirements; no actual capability pass is inferred.

Capacity is 400 GiB; steady/critical/unhealthy thresholds remain300647710720/343597383680/386547056640 bytes. Exactly 80% remains critical and 100% inadmissible. Only PostgreSQL pod/process faults with healthy node/local storage remain in-profile for zero acknowledged loss. Node/volume/control-plane/site loss is excluded, with no node HA and separately measured nonzero backup RPO/RTO. No existing capture is promoted to this new hash.

The old C1.15 packet's verified Dapr digest is an index, and contains no platform observation. Registry evidence authenticates the proposed linux/amd64 child; it does not retrospectively prove that child ran. The inactive workload pins address the currently external injector choice, and fresh identity evidence must confirm the actual eligible target.

## Rendering finding and proposed correction

The tracked values omit `server.affinity`. Both the old 0.28.5 and candidate 0.29.6 charts therefore render required host anti-affinity for three voters. That cannot schedule the documented three-voter/single-node topology from scratch. The explicit preferred override permits that already approved topology and preserves its absence of node-level availability.

Final render and strict lint exited 0 with empty stderr, using checksum-verified temporary Helm 3.22.0 and Kubernetes 1.34.9 rendering semantics. Raft/static-seal/TLS HCL remained identical, as did three voters, OnDelete, retained PVC templates and permissions. Other semantic changes are chart labels, the server/test-hook image, and Restricted security contexts inherited by the new test hook.

The built-in chart test accepts sealed state and mounts server TLS/seal material. It cannot replace the existing CA-only smoke test or security qualification. Existing network-policy/auth-delegator limitations and architecture blockers remain visible.

## Evidence replay and limits

Original command paths identify the actual temporary acquisition environment. `registry-source-evidence.json` stores downloaded/rendered bytes in base64 so Git line endings cannot alter their content identity. Decode an artifact's `original_bytes_base64` before comparing its hash; never hash the pretty JSON as though it were registry bytes. Archived raw render SHA-256 is `16001617572d803bc9b53731a54c8b564e3c6b5a3d9b28b34a727dad9d435303`.

The [candidate verification](verification.md) rechecks canonical bytes, retained descriptor chains, source bindings and independent inactive-state boundaries. It records only offline checks. Live policy/credential identities, target eligibility, recovery, encrypted storage, principal-driven isolation and named independent decisions remain executed qualification requirements; this source packet settles none of them.

Review approval covers these exact proposed profile/configuration bytes, the explicit preferred affinity and Dapr pins, and coherent repository consumer preparation. It does not authorize live upgrade, gate acceptance or Production activation. If any bound input changes during adoption, regenerate this manifest and review the resulting revision rather than retaining this hash by assertion.

## Dated repository adoption — 2026-10-04

The exact candidate was approved and adopted by the bounded implementation spec.
See [the adoption record](../adoption.md) for current selectors, retained historical receipts,
neutral capture boundaries and the single C1.16 owner. Earlier pending/adoption-held
text records proposal-time state; its original bytes and operator intent are preserved.
No target execution, security requalification, Production activation or gate pass is claimed.
