# OpenBao and PostgreSQL Security Requalification Plan

**Historical proposal-time sections:** the original pending/adoption-held claims below
are superseded only for repository adoption by the dated sections at the end.
Actual live qualification remains pending.

**Status:** D1-D4 repository preparation and version direction approved; exact-byte adoption, upgrade and actual qualification remain outstanding.  
**Date:** 2026-10-04  
**Decision owner:** Administrator / architecture owner.  
**Execution owner:** Hexalith Platform Operations, with independently named reviewers.

## Current Facts and Candidate Versions

| Component | Repository input | Proposed qualification candidate | Source and rationale |
| :-------- | :--------------- | :------------------------------- | :------------------- |
| OpenBao server | 2.6.0, digest-pinned in `deploy/openbao/values.yaml` and `tools/production-deployment-openbao.ps1` | 2.6.4, with authenticated exact image digest | [Official 2.6.4 release](https://github.com/openbao/openbao/releases/tag/v2.6.4), released 2026-10-01, carries security fixes beyond the architecture's 2.6.2 minimum. |
| OpenBao chart | 0.28.5 in the tracked values header | 0.29.6, with authenticated chart identity and explicit server-image override | [Official chart 0.29.6 release](https://github.com/openbao/openbao-helm/releases/tag/openbao-0.29.6) defaults to server 2.6.3; explicit 2.6.4 rendering/compatibility is a qualification task. |
| PostgreSQL | 18.4-trixie, digest-pinned in the StatefulSet and canonical profile | 18.6 in the same Debian/image family, with authenticated exact image/platform digest | [Official 18.6 release notes](https://www.postgresql.org/docs/release/18.6/) describe the upgrade from 18.4 and required security/configuration/data checks. |

The Administrator approved the PostgreSQL 18.6 and OpenBao 2.6.4/chart 0.29.6
version direction on 2026-10-04. The [inactive exact-byte package](profile-candidate/README.md)
retains authenticated public registry image/platform/config/chart identities,
candidate configuration and a canonical hash for the reserved exact-byte review.
That approval does not establish local security or compatibility qualification,
activate the successor profile, or authorize an upgrade. [Chart 0.30.0](https://github.com/openbao/openbao-helm/releases/tag/openbao-0.30.0)
also exists and changes the standalone storage default to PebbleDB. That documented
standalone change is not asserted to migrate this repository's Raft topology;
the broader chart/server line needs separate assessment if selected instead.

Public registry identity checks and offline preparation have executed and are
retained in the inactive package. Recheck official releases/advisories and
registry identities before separately authorized adoption/execution; today's
selected direction does not establish a permanent minimum.

## P1: Approve and Define the Successor Qualification Profile

1. Preserve the old `PG-ONPREM-1` manifest, hash and historical artifacts. Do not
   overwrite source-bound receipts or silently amend an accepted packet.
2. Review and ratify the retained exact `PG-ONPREM-2` proposal with its PostgreSQL image/platform
   digest and exact Dapr/component/runtime identity. Explicitly decide how the
   OpenBao chart/server/secret-policy identity is covered: include it in the
   canonical profile or bind an authenticated separately versioned composite.
3. Preserve the single-node/local-retained-volume fault envelope, no-HA statement,
   400-GiB capacity and authoritative admission thresholds unless a separate
   ratified decision changes them. A PVC request remains insufficient reservation.
4. Review the canonical profile creation in `tools/verify_access_telemetry_lifecycle.py`,
   all consumers of `STORY_27_4_PROFILE_SHA256`, the C1 runner profile allowlist,
   qualification gate/scenario inputs, Production profile annotations, ADR and
   operations appendix. Adopt the new profile coherently in a separate reviewed
   change with explicit old-profile rejection tests. Never accept an arbitrary
   supplied hash as an approved profile.
5. Reconcile planning references without rewriting historical decisions. Update
   the active security ledger with actual reviewed evidence only; version selection
   and a named owner do not resolve its qualification obligation.

**Exit evidence:** approved exact manifest and canonical hash, owner decision,
source/hash coverage, fixtures rejecting mixed and superseded profiles, and a
retained old-profile snapshot. The proposed profile label is unavailable to the
current runner until that adoption is implemented and approved.

## P2: Prepare OpenBao Upgrade and Secret-Plane Qualification

| Action | File or existing surface | Required evidence |
| :----- | :----------------------- | :---------------- |
| Authenticate the server image and chart | Official release assets/registry and `deploy/openbao/values.yaml` | Exact content/platform/chart hashes and source/date; no floating image pin. |
| Render and compare chart settings | Candidate chart plus values, `deploy/openbao/service-account-hardening.yaml`, `deploy/openbao/smoke-test.yaml` and namespace/network policy | Structural comparison for Raft, replicas, TLS, probes, security contexts, resources, PVCs, audit persistence and service-account permissions. |
| Update the existing renderer's expected image | `tools/production-deployment-openbao.ps1` | Same approved digest in manifests, rendered workload and verification expectation; no stale accepted platform identity. |
| Prepare backup and bounded recovery | OpenBao Raft snapshot/restore operations described in `docs/operations/openbao.md` | Independent off-node/off-cluster snapshot, integrity and recovery rehearsal; the existing same-node snapshot volume is not node-loss recovery. |
| Rehearse on an authorized isolated target | Existing OpenBao smoke/health and Dapr secret-store seams | Observed candidate server/chart identities, unseal/leader/health behavior, normal and denied secret reads, audit outcomes and recovery timing. |
| Check security fixes and identity revocation | Official 2.6.x advisories and release changes | Review applicability including expired AppRole credentials, revoked service-account JWT renewal, audit failure and secret-safe diagnostic behavior; attach actual applicable negative evidence. |
| Document current observations | `docs/operations/openbao.md` and focused deployment documentation tests | Distinguish historical measured state, newly rendered configuration and executed candidate proof. |

Offline render and strict lint have passed using a checksum-verified temporary
Helm 3.22.0 binary with Kubernetes 1.34.9 rendering semantics. The retained
[render replay receipt](profile-candidate/offline-render-replay.json) records both
exit-zero executions with empty stderr and the exact candidate render hash.
[Archived response bytes](profile-candidate/registry-source-evidence.json) and
[artifact receipts](profile-candidate/artifact-records.json) retain registry/chart
identity checks. These temporary-tool checks changed no installed dependencies
and contacted no cluster. Platform Operations still must supply the authorized
operational environment, independent backups/recovery rehearsal and actual
security/compatibility qualification before upgrade.

Dapr remains the application secret boundary. Keep runtime and telemetry secret
prefixes/read-only policies separate; require denied cross-prefix and unknown-app
reads, bad CA/hostname rejection and current credential revocation proof. Do not
mount backend/operator credentials into product services or log secret responses.
The broader per-tenant-backend-principal gap is a separate architecture blocker;
this upgrade does not discharge it.

## P3: Prepare PostgreSQL Upgrade and Adapter Requalification

| Action | File or surface | Required evidence |
| :----- | :-------------- | :---------------- |
| Authenticate the 18.6 candidate image | `deploy/kubernetes/base/access-telemetry-postgresql.yaml` | Exact image and `linux/amd64` platform digest; PostgreSQL server and relevant client/tool versions. |
| Inventory release applicability | Live authorized inventory, reviewed against official 18.6 release notes | Replication output-plugin configuration, pgcrypto use/cipher posture, COPY test scripts, GIN statistics and installed extension/index applicability. Apply required repairs only through separately reviewed operator steps. |
| Rehearse backup and restore first | Named independent destination and disposable restore target | Complete stored synthetic inventory, bounded nonzero RPO/RTO, tenant-erasure non-resurrection and rollback viability. |
| Prepare the candidate StatefulSet/profile | PostgreSQL manifest, init SQL/pg_hba/TLS, canonical profile and operations appendix | Keep authentication, verified TLS, least privilege, actor/state-store configuration, pool arithmetic and retained-volume behavior. |
| Qualify real adapter semantics | Existing Dapr and actor interfaces, supported per-gate producers | CRUD, strong reads, ETags/FirstWrite, rollback atomicity, TTL, actor recovery and request bounds on the actual candidate. In-process fakes do not settle these gates. |
| Qualify physical behavior | Capacity, purge, reclamation and declared fault collectors | Measured operand/admission results, cohort attribution, reusable allocator space and zero acknowledged loss inside the declared pod/process fault envelope. |

The PostgreSQL project says an 18.x minor upgrade does not require dump/restore,
but its release-specific cleanup/configuration instructions still apply.
Backup and rehearsal here protect the governed deployment and evidence boundary;
they do not assert that a major-version migration is required. Restore or downgrade
is never automatic: use only a reviewed compatible backup/rollback procedure and
preserve retained volumes and erasure authority.

## P4: Offline Guards Before Any Deployment

Inventory and update only relevant profile expectations. Focused existing classes
include `ProductionDeploymentArtifactsTests`, `OpenBaoPlatformDocumentationTests`,
`AppHostOpenBaoConfigurationTests`, `AccessTelemetryOperationsContractTests`,
`AccessTelemetryRetentionDecisionTests` and `AccessTelemetryA41CloseOutTests`.
Source paths are under `tests/Hexalith.Memories.Server.Tests/Deployment` or
`tests/Hexalith.Memories.Server.Tests/Architecture` as appropriate.

Use Debug/source-reference builds for local source work and the repository's
Release/package gate for CI. Run test projects individually; for focused xUnit v3
checks, build the target and execute its built assembly with single-dash `-class`
or `-method` selectors. The listed .NET lanes remain implementation verification instructions. Offline
package integrity and temporary-tool render/lint checks have already executed;
their receipts do not substitute for those project checks or live qualification.

Rerun affected lifecycle/profile/C1 fixtures and prove old/new profile mismatch
rejection, malformed or missing observations, denied tenant/scope before dependency
access and secret-safe output. Retain exact test names, command, result and changed
surfaces in the implementation story and independent review record.

## P5: Authorized Requalification and Evidence Disposition

| Stage | Owner | Gate to the next stage |
| :---- | :---- | :-------------------- |
| Approve exact revised profile and planned repository changes | Administrator / architecture owner | Reviewed identity/manifest bytes and explicit successor-profile decision. |
| Supply target, external archive, protected access and recovery/fault windows | Platform Operations | Explicit authorized non-Production target and bounded scenario; independent backup proven before mutation. |
| Execute updated security and identity evidence | Platform Operations and gate owners | Actual source-bound immutable packets and cleanup receipts, with no missing/skipped/failed observations. |
| Execute all remaining supported C1 slices | Allocated gate owners | Each unique artifact reviewed on the same revised profile; older C1.15 acceptance remains historical and requires fresh capture/evaluation for this profile. |
| Validate genuine Operations/Security decisions | Different named independent reviewers | Actual hash-bound evidence accepted, with explicit denial of automatic activation and other gate credit. |
| Authenticate the canonical C1 predecessor | Reviewed assembler/verifier owner | Exact fields/source/artifact identities and current authorization freshness pass. |
| Resume Story 27.4 | Its operator owner under separate live authorization | Complete same-profile C0-C6, terminal validation and the exact permitted publication/close-out chain. |

Every mutation lane independently observes its initial and final state: disabled
qualification gate, released Lease and zero lifecycle/clock replicas. It preserves
Production writes disabled. A static manifest, a prior target's cleanup or an
assumed Helm rollback cannot substitute for a current cleanup receipt.

## Remaining Inputs and Consequences

| Owner | Input still needed | Consequence while absent | Measurable reopen trigger |
| :---- | :----------------- | :---------------------- | :----------------------- |
| Administrator / architecture owner | Reserved exact-byte profile/configuration review and coherent adoption decision; version direction already approved | Profile consumers remain unchanged and new-profile commands invalid | Explicit approval of the retained authenticated final bytes/hash and reviewed consumer changes. |
| Platform Operations | Authorized operational tool environment and independently recoverable backup destination; offline render/digest checks already retained | No upgrade/recovery qualification | Authorized execution environment plus successful independent backup integrity and restore rehearsal. |
| Platform Operations | Exact non-Production target, external evidence root, protected credential access and capacity/fault windows | No live queries or mutations | Explicit target/scenario authorization and bounded preflight inputs. |
| Operations and Security owners | Different named independent reviewers and evidence retention ownership | No acceptance or canonical predecessor authorization | Reviewed assignments, retrievable archive and actual bound decisions. |
| Producer/validator owner | Canonical interchange and rejection tests | Capture packets cannot be treated as passing predecessors | Independently reviewed converter/assembler contract and executable negative evidence. |

This plan preserves the infrastructure-telemetry assurance limit and keeps A41
open. It neither substitutes for the rest of the architecture's Production gates
nor guarantees legal compliance, append-only telemetry or a tamper-evident audit
trail.

## Remaining qualification prerequisites — dated 2026-10-04

Exact PG2 repository adoption is implemented; earlier pending exact-byte/adoption
claims in the proposal-time sections are historical. An accepted OCI index does
not observe actual execution platform; that qualification remains held under
C1.17 / the unregistered Story 27.23 draft and earns no credit from C1.16.

Deployment Adapter Developer owns a separately scoped, unregistered future PG2
C1.15 producer/review prerequisite. Current `C1.15/PG-ONPREM-2` remains rejected
before target calls/output-directory creation; Story 27.21 and its accepted PG1
packet remain historical. Reopen requires approved executable producer/source
changes, nonzero positive/negative fixtures, separately authorized same-PG2 live
capture and a retrievable independently reviewed disposition. Generic C0 runtime
observations cannot substitute for that renewal.

Inherited in-Pod restart/container-incarnation and remote metadata-response
buffering limitations remain pending maintenance, owned by Deployment Adapter
Developer. No fixes or changes to historical ledger entries are claimed. The
[current handoff](implementation-handoff.md#remaining-qualification-prerequisites--dated-2026-10-04)
records concrete owner/consequence/reopen evidence for all four prerequisites.
No new story registration, live qualification, Production or A41 credit is granted.
