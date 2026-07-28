---
baseline_commit: 327d1a9d7eaef063c656a6af9df4eea84f47ca30
creation_sprint_status_sha256: be1272f1e43cef4ae01829056442678a8b62b0dbef70bc74a4fb8448fccb3e20
---

# Story 31.1: OpenBao Platform Hardening and Documentation

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator and security reviewer,
I want the deployed OpenBao platform documented at its exact configuration and its limitations recorded as accepted,
so that the secret-management boundary is reviewable on its own evidence before anything is migrated onto it.

## Acceptance Criteria

1. **Given** the deployed OpenBao `hexalith-keys` platform,
   **When** its topology is reviewed,
   **Then** `deploy/openbao/values.yaml`, `namespace.yaml`, `service-account-hardening.yaml`, and `smoke-test.yaml` are documented in `docs/operations/openbao.md` with their exact deployed configuration
   **And** the smoke test is runnable with a named command and recorded result.

2. **Given** the deployed availability profile as measured - OpenBao Raft voters co-located on a single Kubernetes node, so the node is the whole failure domain regardless of voter count,
   **When** the security reviewer evaluates it,
   **Then** the static file-based OpenBao seal - the unseal key held in a Kubernetes Secret beside the data - and the namespace-wide port 8200 ingress are surfaced explicitly as accepted limitations of that single-node-hosted profile, each with owner, consequence, compensating controls, and a reopen trigger
   **And** neither limitation is described as hardened or production-HA
   **And** the documented voter count and HA mode match the running platform rather than the tracked manifest.

3. **Given** platform documentation and evidence,
   **When** either is produced,
   **Then** no unseal key, recovery key, root or operator token, or other secret value appears in `docs/operations/openbao.md`, any evidence artifact, or any test snapshot (NFR9, project-context "Never expose secrets").

> **AC2 was amended on 2026-07-28** by the approved Sprint Change Proposal 2026-07-28 (Story 31.1
> deployed-profile ratification), because its original premise - "the single-node deployment profile" -
> was contradicted by the running platform's three Raft voters. See
> `### Ratified — AC2 deployed-profile qualifier (2026-07-28)`. Both limitations are unchanged; document
> the availability profile as measured, never as the tracked manifest declares it.

## Tasks / Subtasks

- [x] Task 1 - Re-measure the deployed platform before writing a single line of documentation (AC: 1)
  - [x] Re-run every read-only probe in `### Deployed-State Drift Measured At Creation` against context `jpiquot@local`. The creation-time measurements are dated 2026-07-28 and are a starting point, never the evidence; the platform drifted once already without any tracked file changing.
  - [x] Record, for each of the four owned files, the deployed value of every setting the file declares, and every deployed setting the file does **not** declare (HA, `retry_join`, `service_registration`, discovery RBAC, snapshot CronJob and its PVC/ServiceAccount, the `cert-manager` NetworkPolicy rule, the `hexalith-keys-pki` Secret).
  - [x] Do not print, copy, or store any secret value. `kubectl get secret` is permitted for **names and types only**; never `-o yaml`, never `-o jsonpath` over `.data`. Never read `openbao-seal`, `openbao-operator-credentials`, `openbao-server-tls`, or `hexalith-keys-pki` contents.
  - [x] Capture the current Helm release revision count and the deployed namespace annotations (`hexalith.io/composite-profile-sha256`, `helm-manifest-sha256`, `hardening-manifest-sha256`, `deployment-id`, `profile-id`) so the drift claim below is re-derived, not inherited.

- [x] Task 2 - Reconcile the four owned manifests with the deployed platform, or record the divergence as an owned gap (AC: 1)
  - [x] `deploy/openbao/values.yaml` currently declares `standalone.enabled: true`, `ha.enabled: false`, `service.active.enabled: false`, `service.standby.enabled: false`, and `serviceAccount.serviceDiscovery.enabled: false`. The deployed release contradicts every one of those. Reconcile the file to the deployed configuration - this is the reading of AC1 that leaves the repository honest.
  - [x] If reconciliation is not possible in one pass, record each unreconciled setting in `docs/operations/openbao.md` as a named divergence with an owner and a reopen trigger. Silence is not an option: AC1 says *exact deployed configuration*.
  - [x] Add the deployed-but-untracked artifacts to `deploy/openbao/` or name them explicitly as out-of-repo platform state with an owner. In scope for naming: the Raft snapshot CronJob (`30 0 * * *`), its `openbao-snapshot` ServiceAccount and `openbao-snapshots` 2Gi PVC, the `hexalith-keys-discovery-role`/`-rolebinding`, and the `cert-manager` ingress rule.
  - [x] Correct the `service-account-hardening.yaml` narrative. The file sets `automountServiceAccountToken: false` on the ServiceAccount, but the deployed pod spec sets it to `true` at pod level, which wins. The server therefore **does** receive and use a Kubernetes API token, for `service_registration "kubernetes"` backed by `hexalith-keys-discovery-role`. The doc's current claim that applying the file means "the server does not receive an unused Kubernetes API token" is false as deployed. State what the file actually achieves today.
  - [x] Do not weaken the Restricted Pod Security profile, remove TLS, change the image digest pin, or relax the NetworkPolicy to make a check pass.

- [x] Task 3 - Rewrite `docs/operations/openbao.md` against the measured platform (AC: 1, 2, 3)
  - [x] Give each of the four owned files its own documented section binding its deployed configuration: chart/image identity and digest, replica count and HA mode, storage and audit persistence, TLS posture and expiry, PSA labels and profile annotations, ServiceAccount automount reality, and the smoke-test Job's security context and endpoint.
  - [x] Correct every stale claim. At creation these were measurably false: "single-node integrated Raft"; "This is a production-shaped **single-server** deployment"; "not server or node high availability"; "loss of the single node still requires an off-cluster Raft snapshot and recovery plan" (a snapshot CronJob now exists); and the profile-annotation drift-rejection claim, which the drift below disproves.
  - [x] Add an accepted-limitations table with the exact header `| Limitation | Owner | Consequence | Compensating controls | Reopen trigger |` and exactly two rows keyed `Static file-based seal` and `Namespace-wide port 8200 ingress`. Every cell must be substantive; an empty or "TBD" cell fails the guard in Task 5.
  - [x] Static file-based seal row: the seal stanza is `seal "static"` with `current_key = file:///openbao/userconfig/openbao-seal/current.key`, mounted from Secret `openbao-seal` in the same namespace as the `data-hexalith-keys-*` PVCs. Anyone who can read the namespace's Secrets and volumes holds both the ciphertext and the key. Name the compensating controls that actually exist (Restricted PSA, RBAC scope, ClusterIP-only Service, NetworkPolicy, persistent audit device, 2-of-3 recovery threshold) and a reopen trigger tied to an external KMS/HSM-backed seal.
  - [x] Namespace-wide port 8200 ingress row: the NetworkPolicy admits **every** pod in namespace `hexalith-memories` on 8200, plus every pod in `cert-manager`. At creation that is 7 application-namespace pods, including `redis-stack-0`, `falkordb-0`, `access-telemetry-postgresql-0`, and both `memories-mcp` pods - and per architecture D31 the MCP sidecar is scoped to receive **neither** secret component. Name the reopen trigger as narrowing the selector to the pods that actually consume a Dapr secret component.
  - [x] State the availability profile precisely, per amended AC2: the documented voter count and HA mode must match the **measured** platform, not `values.yaml`. Say both halves plainly - leader election and standby failover exist between the voters, **and** all voters share one Kubernetes node, so the node is the entire failure domain and there is no node-level availability.
  - [x] Neither row, and no surrounding prose about either limitation, may describe it as hardened, production-HA, highly available, or production-ready.
  - [x] Preserve what is still true and still needed: the Dapr secret-boundary table, the rotation and recovery deadlines, the "do not delete the data/audit PVCs" instruction, and the reference links.
  - [x] The doc must remain CRLF-terminated per `.gitattributes` (`* text=auto eol=crlf`). The working-tree copy is currently LF; normalize it with the idempotent two-step `sed -i -e 's/\r$//' -e 's/$/\r/'`, never a bare append-CR.

- [x] Task 4 - Execute the smoke test and record its result as evidence (AC: 1, 3)
  - [x] Run `kubectl apply -f deploy/openbao/smoke-test.yaml` then `kubectl -n openbao wait --for=condition=complete job/hexalith-keys-smoke-test --timeout=2m`, and capture the Job's `bao status -format=json` output.
  - [x] Record the result in `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` with the exact commands, UTC timestamp, cluster context, namespace, and the observed `initialized`, `sealed`, `storage_type`, `ha_enabled`, and `version` fields. `bao status` emits no secret; do not add any command that would.
  - [x] The Job carries `ttlSecondsAfterFinished: 300` and `backoffLimit: 0`. Capture the logs before the TTL reaps it; a re-run needs the prior Job deleted first.
  - [x] If the cluster is unreachable at implementation time, record an accepted blocker with owner, consequence, and reopen trigger instead of a claimed result. Do not synthesize a status payload.

- [x] Task 5 - Bind the documentation to the manifests with an executable guard (AC: 1, 2, 3)
  - [x] Add `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs`, following the structure-aware pattern of `DeploymentConfigurationContractTests`: `MarkdownContractDocument.GetTableHeader`/`GetTableRows` for exact rows and counts, `GetSection` for narrative claims bound to their exact ATX section, and `ContractDocumentGuard.FindLeakedToolCallMarkup` for anti-corruption. Do not satisfy an authoritative claim with whole-document `ShouldContain`.
  - [x] Assert the accepted-limitations table's exact header, exactly two rows, both row keys, and that no cell is empty or a placeholder.
  - [x] Assert that the sections describing either limitation contain none of `hardened`, `production-HA`, `highly available`, or `production-ready` - the executable form of AC2's second clause.
  - [x] Assert amended AC2's third clause: the documented voter count and HA mode are bound to the **measured** platform, not to `values.yaml`. Pin the documented replica count and HA mode as exact literals so a doc that still says one voter, or a manifest reconciled without the doc following, fails the build. This clause exists because the recorded drift crossed nine Helm revisions unnoticed; an assertion that reads only the manifest would have missed all of it.
  - [x] Assert the doc names the exact smoke-test command and carries a recorded result section referencing the evidence artifact.
  - [x] Assert AC3 negatively: no PEM block markers, no `hvs.`/`s.` token prefixes, no `Unseal Key`/`Recovery Key`/`Initial Root Token` labels, and no long base64/hex run that would shape like key material, in either the doc or the evidence artifact.
  - [x] Tie each documented value to its manifest source the way `DeploymentConfigurationContractTests.ShouldAppearInBoth` does, so a manifest edit that is not mirrored in the doc fails the build.
  - [x] **Regression risk:** `ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal` currently pins `values.yaml` content including `(values.Split("size: 10Gi").Length - 1).ShouldBe(2)`. Reconciling the manifests in Task 2 will break it. Update it deliberately to the reconciled contract; do not delete assertions to get green, and do not weaken the TLS, digest, ClusterIP, or Restricted-PSA assertions it already holds.

- [x] Task 6 - Record the security reviewer's evaluation (AC: 2, 3)
  - [x] The reviewer of record is **Murat TEA for Jérôme** (`murat-tea-for-jpiquot`), carried as a namespace annotation and in the doc header. Platform Operations owner is **Jérôme Piquot** (`jpiquot`).
  - [x] The evaluation must be recorded as its own checkpoint row with a date and a named evaluator; a recommendation to seek review is not a recorded evaluation.
  - [x] The reviewer must evaluate the platform as measured, not as the manifests describe it. Hand over the Task 1 measurements and the Task 2 reconciliation together.
  - [x] If no reviewer signs, record an accepted blocker with owner, consequence, and reopen trigger, and leave the row `pending` / `not complete`. Story 27.3's unassigned AC4 security approver is the precedent for how this is recorded honestly.

- [x] Task 7 - Validate the slice and record the phase truthfully (AC: 1, 2, 3)
  - [x] Build and run the focused lane recorded under `### Testing Baseline and Planned Delta`, then re-run `ProductionDeploymentArtifactsTests` as regression evidence.
  - [x] Run `git diff --check`, reconcile the cumulative File List against `baseline_commit`, and append a `dev-story` Change Log row with runner-derived actual deltas. Do not rewrite the create-story row and do not count planned tests as actual.
  - [x] Fill every checkpoint row's review state and completion state from executed evidence. A row whose evidence did not run stays `pending` / `not complete` with a named blocker.

## Implementation Checkpoints

Every row is mandatory. Each carries an accountable owner, an exact evidence command or artifact, a review state, and a completion state. Completing this table documents and reviews the platform; it advances no Story 31.2 gate and migrates nothing.

| Checkpoint | Accountable owner | Required evidence artifact and command | Review state | Completion state | Completion date |
| :--------- | :---------------- | :------------------------------------- | :----------- | :--------------- | :-------------- |
| C1 - Four owned topology files documented at exact deployed configuration | Memories Maintainer | `docs/operations/openbao.md` sections for `values.yaml`, `namespace.yaml`, `service-account-hardening.yaml`, `smoke-test.yaml`, each bound by `OpenBaoPlatformDocumentationTests` to its manifest source; run `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` then `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo` | verified — guard executed 2026-07-28 | complete | 2026-07-28 |
| C2 - Deployed-state drift reconciled or recorded as an owned gap | Memories Maintainer + Hexalith Platform Operations (`jpiquot`) | The Task 1 re-measurement transcript in `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md`, plus either a reconciled `deploy/openbao/values.yaml` or a named divergence list with owner and reopen trigger. Serves AC1's *exact deployed configuration* clause | verified — transcript and reconciliation recorded | complete | 2026-07-28 |
| C3 - Smoke test executed with named command and recorded result | Hexalith Platform Operations (`jpiquot`) | `kubectl apply -f deploy/openbao/smoke-test.yaml` then `kubectl -n openbao wait --for=condition=complete job/hexalith-keys-smoke-test --timeout=2m`, with the Job's `bao status -format=json` fields recorded in `31-1-openbao-platform-evidence.md`. A synthesized status payload or an unexecuted command fails this row | verified — executed 2026-07-28T09:43:31Z, Job reached condition=complete | complete | 2026-07-28 |
| C4 - Accepted limitation: static file-based seal | Hexalith Platform Operations (`jpiquot`) + security reviewer (`murat-tea-for-jpiquot`) | The `Static file-based seal` row of the accepted-limitations table in `docs/operations/openbao.md`, with owner, consequence, compensating controls, and reopen trigger all substantive, asserted by `OpenBaoPlatformDocumentationTests` | pending — row asserted by the executed guard; reviewer signature carried by C7 | complete | 2026-07-28 |
| C5 - Accepted limitation: namespace-wide port 8200 ingress | Hexalith Platform Operations (`jpiquot`) + security reviewer (`murat-tea-for-jpiquot`) | The `Namespace-wide port 8200 ingress` row of the same table, naming the admitted namespaces and the pods they cover as measured, asserted by the same class | pending — row asserted by the executed guard; reviewer signature carried by C7 | complete | 2026-07-28 |
| C6 - No secret value in documentation, evidence, or snapshot | Memories Maintainer + security reviewer (`murat-tea-for-jpiquot`) | The negative assertions in `OpenBaoPlatformDocumentationTests` over both `docs/operations/openbao.md` and `31-1-openbao-platform-evidence.md`, run by the C1 command. Serves AC3 and NFR9 | verified — negative assertions executed over both records | complete | 2026-07-28 |
| C7 - Security reviewer's recorded evaluation | Security reviewer (`murat-tea-for-jpiquot`) | A dated, named evaluation recorded in `31-1-openbao-platform-evidence.md` covering the platform **as measured**, both accepted limitations, and the AC2 qualifier decision. An unassigned reviewer is recorded as an accepted blocker with owner, consequence, and reopen trigger, and this row stays `pending` | pending — accepted blocker: no named evaluator signed | not complete | — |

## Dev Notes

### Developer Context

The OpenBao `hexalith-keys` platform is **already deployed and running**. This story does not stand it up. It formalizes ownership, corrects the record, and puts the accepted limitations on file so that Story 31.2's runtime `secretstore` migration is evaluated against a known baseline.

Epic 29 already delivered the Aspire/AppHost-local OpenBao topology (Story 29.1, `done`). Nothing in `src/Hexalith.Memories.AppHost/**` is in scope here. Story 31.2 owns `deploy/kubernetes/base/dapr/secretstore.yaml` - which, note, **already** declares `secretstores.hashicorp.vault` as of commit `4d2e4e2f`. Do not touch it, do not claim its migration, and do not close Story 31.2's acceptance criteria from this story's evidence.

### Deployed-State Drift Measured At Creation

Measured 2026-07-28 by read-only probe against context `jpiquot@local`. **Re-derive all of it in Task 1 before writing documentation.** Every row is a fact the tracked artifacts and `docs/operations/openbao.md` currently contradict.

| Subject | Tracked artifact / current doc says | Deployed platform measured 2026-07-28 |
| :------ | :---------------------------------- | :------------------------------------ |
| Replicas | `standalone.enabled: true`, `ha.enabled: false`; doc: "single-node integrated Raft", "single-server deployment" | StatefulSet `.spec.replicas = 3`; pods `hexalith-keys-0/1/2` all `1/1 Running` |
| HA mode | `service.active.enabled: false`, `service.standby.enabled: false`; doc: "not server or node high availability" | `bao status` reports `ha_enabled: true`, `leader_address: https://10.233.102.188:8200`; Services `hexalith-keys-active` and `hexalith-keys-standby` exist |
| Node topology | Not stated; implied by "the target Kubernetes cluster has one node" | **Confirmed: one node.** `node1`, `control-plane,worker`, `v1.34.9`. All three voters and both snapshot Jobs are scheduled on it. Process-level failover is real; node-level availability is not. This is what AC2's amended premise names, and it is why the no-HA claim stands |
| Raft config | `storage "raft"` with `node_id = "hexalith-keys-0"`, no join | Deployed `hexalith-keys-config` has no `node_id` and adds a `retry_join` block against `hexalith-keys-0.hexalith-keys-internal.openbao.svc.cluster.local:8200` |
| Service registration | `serviceAccount.serviceDiscovery.enabled: false`; no registration stanza | Deployed config adds `service_registration "kubernetes" {}`; Role `hexalith-keys-discovery-role` and its RoleBinding exist |
| ServiceAccount token | `service-account-hardening.yaml` sets `automountServiceAccountToken: false`; doc: applied "so the server does not receive an unused Kubernetes API token" | SA is `false` but pod `hexalith-keys-0` sets `automountServiceAccountToken: true`, which wins at pod level. The token **is** mounted and **is** used by service registration |
| NetworkPolicy ingress | Two rules: `hexalith-memories` namespace on 8200, and self-selector on 8200/8201 | Adds a third source: `cert-manager` namespace on 8200 |
| Backup | Doc: "loss of the single node still requires an off-cluster Raft snapshot and recovery plan" | CronJob `openbao-raft-snapshot` at `30 0 * * *` with ServiceAccount `openbao-snapshot`, PVC `openbao-snapshots` (2Gi, Bound), two completed Jobs |
| Persistent volumes | Two 10Gi PVCs (`values.yaml` asserts exactly two `size: 10Gi`) | Six 10Gi PVCs (`data-` and `audit-` for each of three nodes), all `Bound`, plus the 2Gi snapshot PVC |
| Untracked Secret | Not mentioned | Secret `hexalith-keys-pki` exists in namespace `openbao` |
| Helm revisions | Doc pins one deployment ID `…-hexalith-keys-r2` | Nine `sh.helm.release.v1.hexalith-keys.v1…v9` release Secrets |
| Profile annotations | Doc: "Both namespaces carry the profile identity as annotations so an evidence capture can reject drift before running a probe" | Namespace annotations still read composite `4183b741eac062d962a8ff1860a7aa049719a75f47e38e6fdcfb0fe1aeaa5d45` and deployment ID `memories-production-2.11.0-c7c2ca21-hexalith-keys-r2`. The configuration drifted underneath unchanged annotations, so the annotation does **not** reject drift |

**What is still exactly true and must not be softened:**

- Image digest `quay.io/openbao/openbao@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653`; `bao status` reports version `2.6.0`, `initialized: true`, `sealed: false`, `storage_type: raft`.
- Namespace `openbao` carries the Restricted Pod Security enforce/audit/warn labels and both ownership annotations.
- All Services are `ClusterIP`; TLS is on (`tls_disable = 0`, `tls_min_version = "tls12"`); the persistent JSON audit device is configured.
- **AC2 limitation 1 holds verbatim:** `seal "static"` with `current_key = file:///openbao/userconfig/openbao-seal/current.key`, mounted from Secret `openbao-seal` in the same namespace as the data PVCs.
- **AC2 limitation 2 holds and has widened:** the NetworkPolicy admits every pod in `hexalith-memories` (7 at measurement) and every pod in `cert-manager` on port 8200.

Read-only probes used (safe to repeat; none reads a Secret's contents):

```bash
kubectl get nodes -o wide
kubectl -n openbao get pods -o custom-columns=NAME:.metadata.name,NODE:.spec.nodeName
kubectl -n openbao get pod,service,pvc,networkpolicy
kubectl -n openbao get statefulset hexalith-keys -o jsonpath='{.spec.replicas}'
kubectl -n openbao get cm hexalith-keys-config -o jsonpath='{.data}'
kubectl -n openbao get networkpolicy hexalith-keys -o jsonpath='{.spec}' | jq .
kubectl -n openbao get pod hexalith-keys-0 -o jsonpath='{.spec.automountServiceAccountToken}|{.spec.serviceAccountName}|{.status.containerStatuses[0].imageID}'
kubectl -n openbao get sa hexalith-keys -o jsonpath='{.automountServiceAccountToken}'
kubectl -n openbao get role,rolebinding
kubectl -n openbao get cronjob,job
kubectl -n openbao get secret --no-headers -o custom-columns=NAME:.metadata.name,TYPE:.type
kubectl get ns openbao -o jsonpath='{.metadata.labels}{"\n"}{.metadata.annotations}'
kubectl -n openbao exec hexalith-keys-0 -- env BAO_CACERT=/openbao/userconfig/openbao-server-tls/ca.crt bao status -format=json | jq '{initialized,sealed,storage_type,ha_enabled,leader_address,version}'
```

### Ratified — AC2 deployed-profile qualifier (2026-07-28)

**Resolved.** The Administrator selected Path A on 2026-07-28 and approved the amendment recorded in `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-31-1-deployed-profile-ac2-ratification.md`. AC2 above and the matching `epics.md` acceptance criterion and Implementation-evidence clause were amended together.

**What was contradicted and what was not.** The original premise read "Given the single-node deployment profile". The platform runs three Raft voters with `ha_enabled: true`, so that premise was false about the Raft topology. A follow-up measurement found the Kubernetes cluster has exactly **one node** (`node1`, `control-plane,worker`), with all three voters co-located on it - so "single-node" remained true about the hosting and the failure domain. The amendment keeps the phrase where it is accurate and drops it where it is not.

**Consequences for implementation:**

- Both accepted limitations are unchanged in substance. C4 (static file-based seal) and C5 (namespace-wide port 8200 ingress) stay in scope with their owners and evidence.
- The "neither limitation is described as hardened or production-HA" clause stands and now carries more weight: with three voters, a reader could mistake process-level failover for real availability. Say plainly that leader election exists **and** that one node holds the entire quorum.
- The amendment added one clause: the documented voter count and HA mode must match the running platform, not the tracked manifest. This is enforceable in Task 5 - assert the documented replica count and HA mode against the measured values, not against `values.yaml`. It exists because the drift below crossed nine Helm revisions with the profile-hash annotations unchanged.
- Do not revert the platform to single-voter Raft under this story. If that is ever wanted it is a separate live change needing Platform Operations approval on its own merits.

### Course Correction Record

- Approved by Administrator on 2026-07-28 in `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-31-1-deployed-profile-ac2-ratification.md`.
- Active effect: amend Story 31.1 AC2's premise and limitation label in both `epics.md` and this story file to name the deployed availability profile as measured; add the voter-count/HA-mode accuracy clause; align the `epics.md` Implementation-evidence clause. Both accepted limitations, all checkpoint rows, every owner, and the required-row set are unchanged.
- Scope classification: Minor; routed to `dev-story` for Story 31.1. No status advances, no story added, removed, renumbered, or split, no `sprint-status.yaml` change, and no change to the running platform.
- Not changed: `docs/operations/openbao.md` and `deploy/openbao/**` (this story's own deliverables), any Story 31.2 scope, the `resolved` `DW 27.3-CR6` record, and every previously approved dated sprint change proposal.

### Scope Boundary

**In scope:** `deploy/openbao/values.yaml`, `namespace.yaml`, `service-account-hardening.yaml`, `smoke-test.yaml`, `docs/operations/openbao.md`, the new documentation guard test, the deliberate update to the existing OpenBao manifest assertions it collides with, and this story's evidence artifact.

**Out of scope — Story 31.2:** `deploy/kubernetes/base/dapr/secretstore.yaml`, the per-Secret justification inventory for remaining Kubernetes Secrets, the structural no-SDK proof, and the runtime secret-resolution negative proof.

**Out of scope — Epic 29:** `src/Hexalith.Memories.AppHost/**`, `src/Hexalith.Memories.Aspire/**`, `deploy/dapr/**`, and the AppHost OpenBao tests. Story 29.1 is `done`; do not reopen it.

**Out of scope — Story 27.3:** the access-telemetry-specific secret components and the `PG-ONPREM-1` secret backing. `DW 27.3-CR17` names Stories 31.1/31.2 as the blocking owner of the missing `openbao-runtime-bootstrap` Secret in the disposable `kind` verification lane; that lane is Story 27.3 checkpoint C2 and is **not** discharged here. Repairing it is neither required nor forbidden by this story - if it is repaired, it is recorded against `DW 27.3-CR17`, not claimed as an AC of this story.

### Slice Proof

**Thin vertical slice:** measure the running platform, reconcile the four owned manifests and the operations document to it, execute the smoke test and record its result, record both accepted limitations with full owner/consequence/controls/reopen-trigger detail, bind all of it with one executable documentation guard, and obtain the reviewer's dated evaluation.

This is the minimum coherent slice because a documentation change with no executable binding drifts again the same week, and a limitation recorded without an owner and a reopen trigger is not an accepted limitation. It stops short of the runtime migration, which is Story 31.2's independently deployable outcome.

### Security and Documentation Guardrails

- **Never expose secrets** (project-context critical rule, NFR9). No unseal key, recovery share, root or operator token, TLS private key, or CA private material in the doc, the evidence artifact, a test fixture, or a snapshot. `kubectl get secret` for names and types only.
- Architecture **D31** is invariant and unchanged by this story: product services reach secrets only through the Dapr Secrets API. This story adds no code path to OpenBao.
- The runtime and access-telemetry OpenBao prefixes are shared **application** scopes, not tenant partitions. Do not describe them as per-tenant isolation.
- Do not weaken the Restricted Pod Security profile to make the upstream Helm test hook run; the checked-in smoke-test Job exists precisely because the chart's hook does not declare the required security context.
- Documentation accuracy is the deliverable here. A claim that cannot be re-derived from a read-only probe does not belong in the document.

### Remediation Runtime Checklist Applicability

**Not applicable — no workflow/runtime surface touched.** Re-derived from the actual change surfaces: this story changes documentation, four Kubernetes manifests describing an already-running secrets platform, and one test class. It touches no Dapr workflow, child workflow, or activity registration (Category 1, 2); no cleanup, deletion, compensation, or dedup of shared or tenant-scoped state (Category 3); and no migration, blue/green cutover, rollback, abort, or staging key, index, or alias (Category 4) - the runtime `secretstore` migration is Story 31.2. Category 5 File List reconciliation is discharged by the story phase ledger and is not duplicated here.

If Task 2 grows to apply a live change to the running platform under Path B, re-derive applicability before proceeding rather than inheriting this note.

### Tenant Isolation Evidence Applicability

**Not applicable — no tenant surface touched.** This story changes no tenant or case routing, endpoint filter, auth claim, tenant status, index/key/graph selection, actor ID, storage or query selector, MCP authorization or execution, evidence scope display, verifier marker, attribution, or tenant-scoped data movement. The OpenBao prefixes documented here are application scopes, and the allow-listed secret-name selection that is the tenant-safety seam is unchanged. State this explicitly in the completion record rather than leaving it implied; a secrets-boundary story will be asked.

### Expected File Ownership

| Disposition | Path | Story 31.1 expectation |
| :---------- | :--- | :--------------------- |
| Update | `docs/operations/openbao.md` | Rewrite against the measured platform; add the accepted-limitations table and the recorded smoke-test result |
| Update | `deploy/openbao/values.yaml` | Reconcile to the deployed HA configuration, or carry a named divergence list |
| Update | `deploy/openbao/namespace.yaml` | Only if a measured divergence requires it; PSA labels and ownership annotations are already exact |
| Update | `deploy/openbao/service-account-hardening.yaml` | Only if the reconciliation changes it; the narrative correction belongs in the doc |
| Update | `deploy/openbao/smoke-test.yaml` | Only if the executed run shows the manifest no longer matches the platform |
| New | `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` | The structure-aware documentation guard |
| Update | `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` | Deliberate update of `OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal` to the reconciled contract |
| New | `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` | Measurement transcript, executed smoke-test result, reviewer evaluation |
| Update | `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` | This story file |
| Update | `_bmad-output/implementation-artifacts/sprint-status.yaml` | Status transitions only |
| Preserve | `deploy/kubernetes/base/dapr/secretstore.yaml` | Story 31.2 |
| Preserve | `src/Hexalith.Memories.AppHost/**`, `src/Hexalith.Memories.Aspire/**`, `deploy/dapr/**` | Epic 29 |

Treat this table as guidance, not permission to overwrite concurrent work. Re-read every path immediately before editing and reconcile actual paths in the phase ledger.

### Testing Baseline and Planned Delta

Creation used a fresh Release build that completed with 0 warnings and 0 errors:

```text
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

The runner-derived baseline uses the named unit **xUnit test method**:

```text
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo
```

| Lane | Creation baseline (methods) | Planned Story 31.1 delta |
| :--- | --------------------------: | -----------------------: |
| `Hexalith.Memories.Server.Tests`, all xUnit methods | 2,190 | +6..10 |
| `Hexalith.Memories.Server.Tests.Deployment` namespace | 48 | +6..10 (all of the above) |
| `OpenBaoPlatformDocumentationTests` | 0 (class does not exist) | new class, +6..10 |
| `ProductionDeploymentArtifactsTests` | 9 | +0 expected; assertions change, method count should not |
| `DeploymentConfigurationContractTests` (pattern source) | 7 | +0; read-only reference |
| `OperationalRunbookSetTests` | 9 | +0; `openbao.md` is in neither its required nor its preserved list |

Sorted method-set SHA-256 at creation: `bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0`. Built assembly SHA-256: `3d87d277470fc3f419f5946724c33e528c102dea361e16610c7b12161d09c30c`.

Planned ranges are estimates, never evidence. At implementation time rebuild first, capture pre/post runner inventories, state any external same-lane delta separately, and record only observed comparable changes.

`dotnet test` fails in this sandbox with a `SocketException (13)`; use `dotnet exec` against the xUnit v3 assembly with `DiffEngine_Disabled=true`, as every command above does.

### Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Current deployed platform, measured read-only | current-narrow-pattern | The authoritative subject of this story; all documentation derives from re-measurement |
| Current `deploy/openbao/**` and `ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_*` | current-narrow-pattern | Manifest identity and the existing assertion style, both re-verified against the deployed platform before reuse |
| `DeploymentConfigurationContractTests` + `MarkdownContractDocument` + `ContractDocumentGuard` | current-narrow-pattern | The structure-aware doc-guard mechanism only; not its subject matter or its scope |
| Story 29.1 whole-story shape | anti-template | Its six-task AppHost slice, live-Aspire evidence model, and 27-path File List must not shape this documentation story |
| Story 29.1 security guardrails (D31 invariance, prefixes are not tenant partitions, never log response bodies) | current-narrow-pattern | Re-verified security constraints that still bind |
| Story 27.3 whole checkpoint shape | anti-template | Its 25-row C1 gate table, Checkpoint Execution Contract, and retention gates must not be copied. Only the four checkpoint column semantics the epic mandates are reused |
| Story 27.3 AC4 unassigned-approver blocker record | historical-reference-only | The honest way to record an unsigned reviewer; see C7 |
| Pre-split Story 31.1 (platform + runtime migration bundled) | anti-template | Superseded by the 2026-07-27 split. Do not reintroduce the `secretstore` migration into this story |
| `DW 27.3-CR6` and `DW 27.3-CR16` | historical-reference-only | Split provenance and the origin of the two named limitations; they do not define this story's shape |
| `DW 27.3-CR17` | historical-reference-only | Names 31.1/31.2 as blocking owner of the `kind` lane's missing bootstrap Secret; explicitly not an AC here |
| Epic 26 / Story 26.5 runbook-set shape | anti-template | `openbao.md` is not a Story 26.5 runbook and must not be forced into its eight-heading template |
| Commit `4d2e4e2f` | historical-reference-only | Provenance of the platform and of `secretstore.yaml`'s existing `hashicorp.vault` type; reusable facts come from re-measurement |

### Git Intelligence

- `4d2e4e2f` (`feat(openbao): Implement OpenBao-first secret management in Hexalith`) is the **only** commit that has ever touched `deploy/openbao/**` or `docs/operations/openbao.md`. Everything measured in the drift table above happened to the live cluster without a corresponding commit - which is the strongest available argument for the executable guard in Task 5.
- Creation baseline is `327d1a9d` (`fix: update subproject references and address deferred work status verification issues`).
- The recent history is Story 27.3 evidence and CI/release work (`8368ce1c`, `b073aa57`, `3c24f8c2`, `13cf0cbb`). None of it touches this story's owned paths; do not absorb it into the File List.
- The worktree is dirty at creation with user-owned Story 27.3 and Epic 30/31 planning work. Those paths are excluded from this story's File List by name in the Change Log.

### Latest Technical Information

- **Pod-level `automountServiceAccountToken` overrides the ServiceAccount default.** This is why `service-account-hardening.yaml` is inert for the running server pods and why the doc's claim about it is false as deployed.
- **OpenBao static seal** keeps the key material in a file the server reads at start. Co-locating that file's Secret with the data PVCs in one namespace means one namespace-read compromise yields both halves. This is the substance of the C4 limitation and it is unchanged by the move to three voters.
- **Kubernetes NetworkPolicy `namespaceSelector` with no `podSelector`** admits every pod in the matched namespace. This is why the C5 limitation is namespace-wide rather than consumer-scoped.
- **`service_registration "kubernetes"`** requires the API token and the discovery Role that the deployed platform now has; it is what keeps the `-active`/`-standby` Service endpoints correct in HA mode.
- **Dapr documents OpenBao through `secretstores.hashicorp.vault`**; there is no separate OpenBao component type. Relevant to the doc's Dapr boundary table, which stays.
- OpenBao `2.6.0` remains the pinned version and the running version; this story changes no pin.

### Project Structure Notes

- Work in the root repository and use `Hexalith.Memories.slnx`; do not create a legacy `.sln`.
- `.editorconfig` and `Directory.Build.props` enforce nullable, analyzers, warnings-as-errors, implicit usings, and one type per file. New C# files require the ITANEO copyright header.
- Test naming is descriptive PascalCase; assertions use Shouldly, never raw `Assert.*`. A global `Xunit` using already exists.
- `.gitattributes` is authoritative: `*.md` materializes CRLF, `*.yaml` stays LF. Normalize Markdown with the idempotent two-step `sed -i -e 's/\r$//' -e 's/$/\r/'`; the bare append-CR form corrupts already-CRLF files.
- Do not initialize or update nested submodules, update dependencies, stage, commit, push, or clean the worktree unless separately requested. The `references/` gitlinks are user-owned.

### References

- [Epic 31 and Story 31.1](../planning-artifacts/epics.md#story-311-openbao-platform-hardening-and-documentation)
- [Sprint Change Proposal 2026-07-27 — Epic 31 split](../planning-artifacts/sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md#44-cr16--epic-31-split-1-story--2)
- [Sprint Change Proposal 2026-07-26 — DW 27.3-CR6 split](../planning-artifacts/sprint-change-proposal-2026-07-26-readiness-coherence-and-27-3-splits.md)
- [Architecture D31 — OpenBao-first DAPR secret provider](../planning-artifacts/architecture.md#d31--openbao-first-dapr-secret-provider)
- [PRD NFR9](../planning-artifacts/prd.md#non-functional-requirements)
- [Canonical project context](../project-context.md)
- [Deferred work: DW 27.3-CR6, CR16, CR17](./deferred-work.md)
- [Story 29.1 — Aspire-local OpenBao topology (done, out of scope)](./29-1-openbao-backed-apphost-secret-topology.md)
- [Current operations document](../../docs/operations/openbao.md)
- [OpenBao Kubernetes deployment](https://openbao.org/docs/platform/k8s/)
- [OpenBao Helm production checklist](https://openbao.org/docs/platform/k8s/helm/run/)
- [OpenBao static seal](https://openbao.org/docs/configuration/seal/static/)
- [OpenBao Raft storage and `retry_join`](https://openbao.org/docs/configuration/storage/raft/)
- [OpenBao Kubernetes service registration](https://openbao.org/docs/configuration/service-registration/kubernetes/)
- [Kubernetes ServiceAccount token automounting](https://kubernetes.io/docs/tasks/configure-pod-container/configure-service-account/)
- [Kubernetes NetworkPolicy selectors](https://kubernetes.io/docs/concepts/services-networking/network-policies/)
- [Dapr HashiCorp Vault secret store](https://docs.dapr.io/reference/components-reference/supported-secret-stores/hashicorp-vault/)

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (`claude-opus-5`)

### Debug Log References

- 2026-07-28: Loaded Epic 31, the 2026-07-27 sprint change proposal, architecture D31, PRD NFR9, canonical project context, the story-scope guard, the story phase ledger, the remediation runtime checklist, story-creation lessons, and the current tracked OpenBao manifests, documentation, and deployment tests.
- 2026-07-28: Measured the deployed `openbao` namespace read-only against context `jpiquot@local` and recorded the drift table above. No Secret contents were read, printed, or stored.
- 2026-07-28: Captured the fresh Release build and runner-derived discovery baseline for `Hexalith.Memories.Server.Tests`.
- 2026-07-28 (dev-story): Re-ran every read-only probe against context `jpiquot@local` before writing any documentation. All creation-time drift rows re-confirmed, and three further divergences found that creation had not: `server.authDelegator.enabled` is `true` on the platform while the file declared `false`; an untracked, non-Helm ClusterRoleBinding `hexalith-keys-tokenreview` duplicates the chart-rendered `hexalith-keys-server-binding`; and no cert-manager `Certificate` or `Issuer` exists in namespace `openbao` to justify the cert-manager NetworkPolicy rule. No Secret contents were read, printed, or stored at any point.
- 2026-07-28 (dev-story): Executed the checked-in smoke test end to end and captured the Job payload 7 seconds after creation, well inside `ttlSecondsAfterFinished: 300`.
- 2026-07-28 (dev-story): Proved the new guard is not vacuous with ten mutations applied to the real files, each reverted and each file confirmed byte-identical afterwards. The run also exposed a false positive in the guard's own long-run heuristic, which reported five of this story's test method names; the rule was tightened to require base64 density (digits with mixed case) and re-proved against a real payload rather than relaxed. Recorded in evidence section 5.4.
- 2026-07-28 (dev-story): The built-assembly SHA-256 was dropped as evidence. Two Release builds of unchanged sources produced different assembly hashes while yielding a byte-identical sorted method set, so the assembly hash cannot support count agreement. The sorted method-set SHA-256 is used instead.

### Completion Notes List

- 2026-07-28: Created Story 31.1 as the documentation-and-accepted-limitations slice for the already-deployed OpenBao platform. Story 31.2's runtime `secretstore` migration is explicitly excluded.
- 2026-07-28: Recorded the measured divergence between the deployed platform and both the tracked manifests and the operations document, and routed AC2's contradicted "single-node" qualifier as an explicit decision rather than resolving it inside the story.
- 2026-07-28: Ultimate context engine analysis completed - comprehensive developer guide created.
- 2026-07-28 (dev-story): **AC1 satisfied.** All four owned files have their own section in `docs/operations/openbao.md` binding their deployed configuration, and the smoke test is runnable with a named command and carries a recorded, executed result.
- 2026-07-28 (dev-story): **AC2 satisfied as amended.** Both limitations are recorded with substantive owner, consequence, compensating controls, and reopen trigger; neither is described as hardened, production-HA, highly available, or production-ready, which is asserted executably over the whole section including its narrative subsections; and the documented voter count (`3`) and HA mode (`ha_enabled: true`) are pinned to the measured platform, with the manifest tied separately so a values.yaml-only drift also fails.
- 2026-07-28 (dev-story): **AC3 satisfied.** No secret value appears in the document, the evidence artifact, or any test fixture. The negative guard rejects PEM markers, `hvs.` and legacy `s.` token shapes, `bao operator init` dump labels, and any long key-shaped run that is not an allow-listed published digest — proven by four separate mutations.
- 2026-07-28 (dev-story): **Checkpoint C7 is NOT closed.** No named evaluator signed a security evaluation, so C7 stays `pending` / `not complete` and carries an accepted blocker with owner, consequence, and reopen trigger in evidence section 4. C4 and C5 record their documentation artifact as complete but explicitly defer the reviewer signature to C7; nothing in this story claims a security reviewer endorsed the platform.
- 2026-07-28 (dev-story): The reconciled `deploy/openbao/values.yaml` was **not applied**. No `helm upgrade` was run and the running platform was not changed by this story. That gap is a named divergence with a `helm diff` reopen trigger, so the file is honest about describing the platform rather than being proven to reproduce it.
- 2026-07-28 (dev-story): **Remediation runtime checklist — not applicable, re-derived from the actual development diff.** The diff is one Markdown operations document, one Markdown evidence artifact, two Kubernetes manifests describing an already-running secrets platform, one new test class, and strengthened assertions in one existing test class. It touches no Dapr workflow, child workflow, or activity registration (Categories 1 and 2); no cleanup, deletion, compensation, or dedup of shared or tenant-scoped state (Category 3); and no migration, blue/green cutover, rollback, abort, or staging key, index, or alias (Category 4) — the runtime `secretstore` migration remains Story 31.2. Category 5 is discharged by the story phase ledger's File List reconciliation and is not duplicated. No live change was applied to the platform, so the Path B re-derivation trigger recorded at creation did not fire.
- 2026-07-28 (dev-story): **Tenant isolation evidence — not applicable, re-derived from the diff.** No tenant or case routing, endpoint filter, auth claim, tenant status, index/key/graph selection, actor ID, storage or query selector, MCP authorization or execution, evidence scope display, verifier marker, attribution, or tenant-scoped data movement changed. The OpenBao prefixes documented here are shared application scopes, and the document says so explicitly rather than describing them as per-tenant isolation.

### File List

- `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — **partial ownership.** Story 31.1 owns only the `epic-31` and `31-1-openbao-platform-hardening-and-documentation` status rows. The Epic 27 rows in this file, including `27-5-running-pg-onprem-1-capability-qualification`, are Jérôme Piquot's concurrent 2026-07-28 work and are neither edited nor credited here. The file's working-tree CRLF endings are pre-existing and file-wide; they were not introduced by this story and were deliberately not normalized, because rewriting all 745 lines would absorb that concurrent work into this story's diff.
- `_bmad-output/implementation-artifacts/tests/31-1-create-story-scope-evidence.md`
- `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` — **new.** Re-measurement transcript, reconciliation outcome, executed smoke-test result, C7 accepted-blocker record, and test evidence.
- `_bmad-output/planning-artifacts/epics.md` — **partial ownership.** Story 31.1 owns only the three Story 31.1 hunks (amended AC2 premise, amended limitation label, amended Implementation-evidence clause). The remaining diff in this file is Jérôme Piquot's concurrent 2026-07-28 Epic 27 work and is neither edited nor credited here. Unchanged by dev-story.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-31-1-deployed-profile-ac2-ratification.md`
- `deploy/openbao/service-account-hardening.yaml` — comment recording the measured pod-level override added; `automountServiceAccountToken: false` unchanged.
- `deploy/openbao/values.yaml` — reconciled to the deployed HA release.
- `docs/operations/openbao.md` — rewritten against the measured platform.
- `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` — **new.** The structure-aware documentation guard, 9 methods.
- `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` — deliberate strengthening of `OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal` to the reconciled contract; no assertion deleted, method count unchanged.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-28 | create-story | Created context-ready Story 31.1; moved `epic-31` from `backlog` to `in-progress` and `31-1-openbao-platform-hardening-and-documentation` from `backlog` to `ready-for-dev`; left `31-2-runtime-dapr-secret-store-migration` at `backlog`. Recorded the measured deployed-state drift and the AC2 qualifier decision. No implementation, manifest, documentation, or dependency change occurred. | Actual phase delta **+0**, cumulative **+0**. Fresh Release build passed with 0 warnings / 0 errors: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`. Runner-derived baseline, named unit **xUnit test method**, exact discovery command `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo`: `Hexalith.Memories.Server.Tests` **2,190** methods, of which `Deployment` namespace **48**, `ProductionDeploymentArtifactsTests` **9**, `OperationalRunbookSetTests` **9**, `DeploymentConfigurationContractTests` **7**, and `OpenBaoPlatformDocumentationTests` **0** (class absent). Sorted method-set SHA-256 `bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0`; built assembly SHA-256 `3d87d277470fc3f419f5946724c33e528c102dea361e16610c7b12161d09c30c`. Planned Story 31.1 delta **+6..10** methods, all in the `Deployment` namespace; planned values are not actual evidence. | Matched **3/3** intended creation paths against declared baseline `327d1a9d7eaef063c656a6af9df4eea84f47ca30` using `git status --porcelain` for added working-tree files and `git diff --name-status 327d1a9d7eaef063c656a6af9df4eea84f47ca30` for modifications, reconciled in `_bmad-output/implementation-artifacts/tests/31-1-create-story-scope-evidence.md`. `sprint-status.yaml` changed from pre-create SHA-256 `be1272f1e43cef4ae01829056442678a8b62b0dbef70bc74a4fb8448fccb3e20` to the post-create value recorded in that evidence file. Jérôme Piquot owns the named pre-existing dirty exclusions, none of which is credited to this story: `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-c1-blocked-gate-split-and-c3-c4-ratification.md`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`, `tests/tooling/access_telemetry_lifecycle/test_adapter_profile.py`, and `tools/verify_access_telemetry_lifecycle.py`. |
| 2026-07-28 | correct-course | Ratified the AC2 deployed-profile qualifier under the approved Sprint Change Proposal 2026-07-28 (Story 31.1 deployed-profile ratification), Administrator-approved same day. Amended AC2's premise and limitation label in both `epics.md` and this story file to name the measured availability profile, added the voter-count/HA-mode accuracy clause, and aligned the `epics.md` Implementation-evidence clause. Recorded the confirming one-node measurement (`node1`, `control-plane,worker`, all three voters co-located) in the drift table, and carried the new clause into Task 3 and Task 5. Both accepted limitations, every checkpoint row and owner, the required-row set, and all statuses are unchanged. No implementation, manifest, documentation, dependency, or deployed-platform change occurred. | Actual phase delta **+0**, cumulative **+0**. Discovery was **not re-run**, and the create-story baseline of **2,190** `Hexalith.Memories.Server.Tests` xUnit **test methods** stands unchanged: this phase edited only Markdown planning and story artifacts, touching no test project, test source, or build input, so the assembly under `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo` is byte-identical at SHA-256 `3d87d277470fc3f419f5946724c33e528c102dea361e16610c7b12161d09c30c`. Planned Story 31.1 delta remains **+6..10** methods and is not evidence. | Matched **5/5** cumulative story paths against declared baseline `327d1a9d7eaef063c656a6af9df4eea84f47ca30`, using `git status --porcelain` plus `git diff --name-status 327d1a9d7eaef063c656a6af9df4eea84f47ca30`. Two paths joined the cumulative set this phase: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-31-1-deployed-profile-ac2-ratification.md` (new) and `_bmad-output/planning-artifacts/epics.md` (**partial ownership** - Story 31.1 owns only its three hunks at the Story 31.1 acceptance criteria and Implementation-evidence clause; verified with `git diff -U0 _bmad-output/planning-artifacts/epics.md`). Jérôme Piquot owns the rest of the `epics.md` diff (concurrent 2026-07-28 Epic 27 work) and the same named dirty exclusions as the create-story row; none is credited to this story. |
| 2026-07-28 | dev-story | Re-measured the deployed platform read-only, reconciled `deploy/openbao/values.yaml` to it, annotated `deploy/openbao/service-account-hardening.yaml` with the measured pod-level override, rewrote `docs/operations/openbao.md` against the measurement, executed the smoke test, added the `OpenBaoPlatformDocumentationTests` guard, and deliberately strengthened `ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal` to the reconciled contract with no assertion deleted. Three divergences beyond the creation drift table were found and recorded: deployed `authDelegator.enabled: true`, the untracked non-Helm ClusterRoleBinding `hexalith-keys-tokenreview`, and a cert-manager NetworkPolicy rule with no cert-manager `Certificate` or `Issuer` in the namespace. Checkpoints C1, C2, C3, and C6 are complete on executed evidence; C4 and C5 record their documentation artifact as complete with the reviewer signature deferred to C7; **C7 stays `pending` / `not complete`** under an accepted blocker because no named evaluator signed. The reconciled manifest was not applied: no `helm upgrade` ran and the running platform was not changed. | Actual phase delta **+9**, cumulative **+9** from the create baseline. Named unit **xUnit test method**; runner, discovery scope, filters, and configuration identical before and after, so the subtraction is between comparable discoveries. Exact command `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo` after `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` (0 warnings, 0 errors). Observed totals before -> after: `Hexalith.Memories.Server.Tests` **2,190 -> 2,199** (+9); `Deployment` namespace **48 -> 57** (+9); `OpenBaoPlatformDocumentationTests` **0 -> 9**, a named new scope, not a renamed one; `ProductionDeploymentArtifactsTests` **9 -> 9** (+0, assertions strengthened without adding a method); `DeploymentConfigurationContractTests` **7 -> 7** (+0); `OperationalRunbookSetTests` **9 -> 9** (+0). Set difference confirms nine methods added and none removed; every added name is listed in evidence section 5.2. Sorted method-set SHA-256 **bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0 -> 2f99c8cd0c4a4aceb0296e78eabae46aec947034cdd32b4583437a4640c2630b**; the pre-snapshot hash is byte-identical to the create baseline, so create baseline + cumulative story delta = observed total with **no external same-lane delta** to separate. Execution evidence in the distinct named unit **xUnit test case**, never subtracted from the method counts above: focused lane `OpenBaoPlatformDocumentationTests` **9 cases, 0 failed**; regression `ProductionDeploymentArtifactsTests` **9 cases, 0 failed**; `OperationalRunbookSetTests` plus `DeploymentConfigurationContractTests` **16 cases, 0 failed**; whole assembly **2,754 cases, 0 failed, 1 pre-existing skip**. Ten mutations against the real files each failed the intended assertion and were reverted, proving the guard is not vacuous; one of them exposed a false positive in the guard's long-run heuristic, which was tightened to require base64 density and re-proved against a real payload rather than relaxed. The built-assembly SHA-256 recorded at creation is superseded as evidence: two Release builds of unchanged sources produced different assembly hashes with a byte-identical method set, so it cannot support count agreement and only the method-set hash is used. | Matched **11/11** cumulative story paths against declared baseline `327d1a9d7eaef063c656a6af9df4eea84f47ca30`, using `git status --porcelain` for added working-tree files and `git diff --name-status 327d1a9d7eaef063c656a6af9df4eea84f47ca30` for modifications. `git diff --check` is clean. Six paths joined the cumulative set this phase: `deploy/openbao/values.yaml`, `deploy/openbao/service-account-hardening.yaml`, `docs/operations/openbao.md`, `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs`, and the two new files `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` and `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md`. No path was renamed and none returned to its baseline content. Two paths carry **partial ownership**: `_bmad-output/planning-artifacts/epics.md` (unchanged this phase) and `_bmad-output/implementation-artifacts/sprint-status.yaml`, where Story 31.1 owns only the `epic-31` and `31-1-...` status rows and Jérôme Piquot owns the concurrent Epic 27 rows including `27-5-running-pg-onprem-1-capability-qualification`. Named exclusions, all Jérôme Piquot's pre-existing dirty worktree and none credited here: `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-c1-blocked-gate-split-and-c3-c4-ratification.md`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`, `tests/tooling/access_telemetry_lifecycle/test_adapter_profile.py`, `tools/verify_access_telemetry_lifecycle.py`, and the user-owned gitlinks `references/Hexalith.Builds`, `references/Hexalith.EventStore`, `references/Hexalith.Tenants`. |
