---
title: '31.2: Close the live Kubernetes secret-store path'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '31-2-runtime-dapr-secret-store-migration'
baseline_commit: '3662d5be96c8960ea09c83a93a7ea1df33762c7d'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
---

<frozen-after-approval reason="human-owned intent">

## Intent

Finish Story 31.2 checkpoint C2 on `jpiquot@local`. Deploy the approved Dapr 1.19.2 secret boundary, prove that running Memories sidecars enforce the built-in Kubernetes store deny policy, and account for every remaining Kubernetes Secret.

## Boundaries & Constraints

Follow the approved shared control-plane scope, including `ext-secrets-1-isolated`. Preserve the live application images, replica counts, PostgreSQL, component scopes and required OpenBao bootstrap grants. Use the Dapr 1.19.2 candidate to derive a scoped live diff. Keep Secret inspections to names, types and references. Reduce Dapr probe results to status and field presence inside the pod. Keep the existing qualification profile inputs and historic evidence intact.

## Approved Decisions

- **Maintenance prerequisites (`jpiquot@local`, 2026-11-28):** Use Option B — produce a one-off Velero backup of `hexalith-memories` to the existing `scaleway-kube-green` location (paired with logical export or explicit empty-tenant confirmation), verify an isolated restore in a disposable namespace (then clean it up), and execute a minimal single-cluster intake pause/drain checklist before applying the Dapr `1.19.2` rollout. Record this explicitly as the approved non-CSI procedure for `jpiquot@local`.
- **Residue Secret dispositions (`jpiquot@local`, 2026-11-28):** Use Option A — sole owner (`jpiquot`) approves dropping the RBAC grants and deleting `llm-secret`, `google-embedding-api-key`, `memories-embedding-client-secret`, and `access-telemetry-marker-key` after verifying zero live pod/component/env/volume consumers and confirming each namesake OpenBao key exists via metadata-only check (never reading values). Safety rail: if any Secret lacks a matching OpenBao key, do not delete it — retain that Secret without an RBAC grant and record the owner disposition, consequence, and reopen trigger.

## I/O & Edge-Case Matrix

| Scenario | Required result |
| --- | --- |
| Recovery, drain or live diff evidence is missing | Hold cluster changes and keep C2 pending with the specific blocker. |
| Upgraded sidecar | Verify runtime and app identity. Vault reads and existing denials still pass. A synthetic name through the built-in store receives an explicit policy denial. |
| Parked workload or reserved `eventstore` scope in Memories | Record configuration and workload inventory. Require a live read when the corresponding app runs in the namespace. |
| Residue Secret | Verify references and bindings. Before deletion, verify replacement key presence. Record the owner disposition and remeasure the inventory. |

</frozen-after-approval>

## Code Map

- `deploy/kubernetes/overlays/production-dapr-1.19/kustomization.yaml` (L1–43) — Derives scoped live patches: appends `{storeName: kubernetes, defaultAccess: deny}` to all four Configurations, pins `ghcr.io/dapr/daprd:1.19.2@sha256:7839fbb66d4be24ec52e347bec2dca66a91bef3079d8c8d57676f6d3ef92654f`, and narrows `memories-dapr-secret-reader` and `memories-access-telemetry-secret-reader`. Live `memories-access-telemetry` and `access-telemetry-postgresql` each have one ready replica; preserve live `openbao-access-telemetry-bootstrap` on `memories` while live `access-telemetry-secrets` scopes `memories`. Do not alter `deploy/kubernetes/overlays/production/` or `deploy/kubernetes/base/` (pinned by `ProductionDeploymentArtifactsTests` and PG-ONPREM-2 inputs; `base/service-accounts-rbac.yaml` L68–90 also holds `memories-conversation-component-reader`).
- `tools/probe-dapr-secret-scope.sh` (L1–104) & `tests/tooling/runtime_secret_store/test_scope_probe.py` (L1–82) — Reuse the POSIX/BusyBox `nc -e` + awk JSON parser and loopback `HTTPServer` harness (`fail_closed.py` / `test_fail_closed.py` live beside it). Currently hardcodes `store=secretstore` (L98–99); extend to support an identity-checked built-in `kubernetes` store denial using a synthetic key name (`403` + `ERR_PERMISSION_DENIED`), checking `/id`, `/runtimeVersion`, and `secretstores.kubernetes` registration without leaking tokens or values.
- `tests/Hexalith.Memories.Server.Tests/Deployment/RuntimeSecretStoreMigrationTests.cs` (L1–387, `.Output.cs` L1–90) — Guards `production-dapr-1.19` (`VerifyDapr119ProductionBoundary` L183–213), six-column `openbao.md` inventory parity (`Inventory_CoversJulyAndMeasuredNamesInBothDirections` L282), and secret-safe evidence (`PublishedMigrationEvidence_ContainsNoSecretMaterial` L380). Do not weaken `ProductionDeploymentArtifactsTests` or Story 31.1 frozen sections in `OpenBaoPlatformDocumentationTests`.
- `docs/operations/upgrade-migration.md` (L199–210, L300–319) & `docs/operations/backup-restore.md` (L40–109, L294–296) — Define recovery-point, non-CSI isolated restore, live diff, rollback, and intake-drain admission. Read-only preflight on 2026-11-28 confirmed `jpiquot@local` remains on Dapr `1.18.1`, Kubernetes `v1.34.9`, local `kubectl` `v1.36.1` (skew >1 minor — use a `v1.34.x` client for writes), no `snapshot.storage.k8s.io` APIs, Velero `scaleway-kube-green` available, and all four residue Secrets present.
- `docs/operations/openbao.md` (`## Dapr secret boundaries`, L448–530), `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md` (L405–415), `31-2-runtime-dapr-secret-store-migration.md`, and `sprint-status.yaml` — Append rollout/reconciliation records without rewriting Story 31.1 frozen sections or historical 2026-11-22 evidence; keep `openbao.md` inventory rows aligned with the evidence table so the bidirectional parity guard stays green.

## Tasks & Acceptance

**Execution:**
- [x] `tools/probe-dapr-secret-scope.sh`, `tests/tooling/runtime_secret_store/test_scope_probe.py` -- Parameterize store selection and add an identity-checked built-in `kubernetes` store synthetic-key denial check plus unit subtests (matching `/id`, `/runtimeVersion`, `403 ERR_PERMISSION_DENIED`, wrong identity/status rejection, zero canary leakage) -- Proves the built-in store deny policy safely inside a pod before cluster rollout.
- [x] `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md` -- Run a one-off Velero backup of `hexalith-memories` to `scaleway-kube-green` (with logical export or empty-tenant check), rehearse restore in a disposable namespace and clean it up, capture rollback inputs and exact scoped live diff using a `v1.34.x` client, and record intake pause/drain confirmation -- Satisfies the approved non-CSI maintenance admission gate before any cluster mutation.
- [x] `deploy/kubernetes/overlays/production-dapr-1.19/kustomization.yaml` (live derivation), `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md` -- Stage scoped live patches, upgrade shared Dapr control plane to `1.19.2`, and roll approved sidecars across `hexalith-memories` and `ext-secrets-1-isolated`, preserving live images, replicas, PostgreSQL, component scopes, and required bootstrap grants; record the sanitized live Vault and built-in `kubernetes` probe matrix -- Closes the live built-in Kubernetes secret-store path with verifiable evidence. Done for every Memories sidecar and three of five `ext-secrets-1-isolated` sidecars. eventstore and tenants were rolled back to 1.18.1 after a Redis ACL failure, which is recorded with an owner and reopen trigger.
- [x] `docs/operations/openbao.md`, `_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml` -- Verify zero live consumers and OpenBao metadata-only key presence for `llm-secret`, `google-embedding-api-key`, `memories-embedding-client-secret`, and `access-telemetry-marker-key`; drop their RBAC grants and delete each verified Secret (retaining any unseeded key with explicit disposition), remeasure names/types inventory, reconcile `openbao.md`, and update C2 and `sprint-status.yaml` only if deployed evidence supports closure -- Completes Story 31.2 C2 accountability without breaking Story 31.1 frozen guards. Grants dropped. All four Secrets were retained under the safety rail, because both bootstrap policies answered HTTP 403 to metadata list and read.

**Acceptance Criteria:**
- Given the upgraded, healthy deployment on `jpiquot@local`, when each running Memories sidecar is probed with `tools/probe-dapr-secret-scope.sh`, then authorized Vault lookups still succeed where scoped, unscoped/outside-allow-list Vault lookups remain denied, and synthetic-key reads through the built-in `kubernetes` store return explicit `ERR_PERMISSION_DENIED` policy denials with verified app and runtime identity.
- Given the live Kubernetes Secret inventory in `hexalith-memories`, when references, RBAC grants, and owner dispositions are reconciled in `docs/operations/openbao.md` and the evidence record, then every remaining Secret has a justified purpose or explicit owner disposition and all deployment, OpenBao documentation, and tooling guards pass without secret leakage.

## Implementation Notes

- The control-plane upgrade used Helm v3.22.1, not `dapr upgrade`. The CLI upgrade resets chart values and passes the trust bundle as release values. An offline render of chart dapr-1.18.1 with `global.ha.enabled=true`, `dapr_scheduler.cluster.storageClassName=openebs-hostpath-retain` and `dapr_scheduler.etcdSpaceQuota=16Gi` matched all 45 live objects, and the upgrade reused exactly those values.
- The live derivation applied the overlay's JSON patches to the live objects. `memories-dapr-secret-reader` keeps `openbao-access-telemetry-bootstrap` while live `access-telemetry-secrets` scopes `memories`. The overlay file was not edited.
- The `ext-secrets-1-isolated` templates declare their daprd container explicitly, so their sidecar image was patched directly. daprd 1.19.2 Redis state saves call `TYPE`, which the `registry-state` ACL users lack, so eventstore failed and eventstore and tenants were rolled back.
- The backup excludes PersistentVolumes, so the restore rehearsal needed a resource modifier that removes `spec.volumeName`. The first attempt stayed Pending and was cleared with a Velero restart.
- The deny-all intake pause blocked memories-mcp's upstream health check and was narrowed to memories and memories-mcp traffic during the rollout.
- The read-only Dapr bootstrap policies cannot read OpenBao metadata, and no other identity was available without inspecting Secret contents. The four residue Secrets therefore stay, without grants, until an operator confirms the keys with a metadata read.
- The probe takes an expected runtime version, mandatory for `builtin`, selects the store per check, adds telemetry-store checks for the lifecycle app ID and adds the identity-checked `builtin` synthetic-name check.
- `RuntimeSecretStoreMigrationTests.Inventory_CoversJulyAndMeasuredNamesInBothDirections` now binds the November 22 and November 28 names/types projections.

## Spec Change Log

## Review Triage Log

The November 28 review covers the diff since `baseline_commit`. Every finding was verified before its verdict.

| Review finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Verification gap 1: probe suite runs in no automated lane | Medium | Pre-verified gap. The existing deferral's reopen trigger fired (probe changed, live replay ran). The CI change belongs to Hexalith.CiCd. Defer: new deferred-work entry. |
| Verification gap 2: app-ID shape gate unobserved by tests | Medium | Pre-verified. Deleting the gate kept both methods green. Patch: uppercase and backslash-escaped `builtin` rows added, each failing only with the gate removed. |
| Verification other 1: version argument accepts `absent`/`unrecognized` | Low | Reproduced: `memories allowed absent` passed without `runtimeVersion`. Direct correction. Patch: non-empty version must match the awk version shape. |
| Verification other 2: version shape check had no effect | Low | The comment claimed the version reached `awk -v`. Same root cause as Verification other 1. Patch: comment corrected, and the stricter check now rejects sentinels. |
| Blind 1: File Scope says the spec frozen block is unchanged | Low | Planning added the owner's Approved Decisions inside the frozen block before dispatch. Patch: the story entry now says so. Moving the decisions would edit this spec, so that part is rejected. |
| Blind 2: acceptance criteria rewritten in the marking diff | False | Step-02 planning rewrote them before approval. The `ready-for-dev` spec read at dispatch already held both criteria, and the Spec Change Log records review loopbacks only. Rejected. |
| Blind 3: Option A/B no longer point to alternatives | Rejected | The fix edits this build's spec. The approved choices are recorded verbatim in Approved Decisions. |
| Blind 4: baseline move hides commit d67ae5d7 | Low | d67ae5d7 changed `fail_closed.py`, `test_fail_closed.py` and `RuntimeSecretStoreBootstrapTests.cs` between spec baselines. Patch: the Change Log names it. |
| Blind 5: test-delta ledger omits the Python lane | Low | The lane gained `test_builtin_kubernetes_store_denial_and_runtime_identity`, and its subtests grew. Patch: ledger updated. |
| Blind 6: recorded guard failure is stale | Medium | `epic-31-context.md` was corrected at 12:27Z, and the three classes then passed 167/167. Patch: every failure statement replaced with the corrected state. |
| Blind 7: done state while a check is recorded failing | Medium | Same root cause as Blind 6. Patch. |
| Blind 8: `epic-31-context.md` rewrite adds unsourced claims | Medium | No epics.md or architecture.md source for the Epic 35 qualification, the lifecycle-provisioned principals or the downstream key list. `PG-ONPREM-1` is sourced. Patch: HEAD wording restored for those passages. |
| Blind 9: deferred work not updated for fired triggers and new follow-ups | Medium | `deferred-work.md` was not in the diff. Defer: nine entries appended, covering the probe lane, McpCli census, ext-secrets ACL, hot-reload abort, residue deletion, runbook lessons, overlay alignment, backup retention and probe newline. |
| Blind 10: production overlay apply would also re-grant residue Secrets | Medium | The tracked production reader Role still names them. Patch: `openbao.md` states it. Defer: the tracked overlay alignment needs a PG-ONPREM-2 decision. |
| Blind 11: marker row lost its Kubernetes-consumer reopen trigger | Medium | Confirmed against HEAD. Patch: trigger restored. |
| Blind 12: Role check narrower than principal reach | False | At 12:40Z `kubectl auth can-i get secret/<name>` for the four names, and `list secrets`, answered `no` for all eight ServiceAccounts. Rejected. The result is recorded in the review replay. |
| Blind 13: residue reopen trigger is passive | Medium | The owner's operator identity can run a metadata-only `bao kv metadata get`. Defer: owner action before the backup expires on 2026-12-28. |
| Blind 14: backup copies every namespace Secret off-cluster | Medium | The backup excludes no Secret. Patch: the evidence states the copies and their retention. Defer: the retention decision. |
| Blind 15: no functional Memories health check on 1.19.2 | False | Memories state and pub/sub components use only `redisPassword` (no username), and the redis-stack config has no ACL users, so the ext-secrets `TYPE` failure cannot occur. Both memories daprd containers logged 0 error lines in 40 minutes. Rejected. |
| Blind 16: `builtin` check can pass without runtime verification | Medium | AC1 requires verified runtime identity. Patch: `builtin` exits 2 without a version, and the bare row now expects failure. |
| Blind 17: clock deny scope proven only structurally | Medium | The clock runs at 0 replicas, and matrix row 3 requires a reopen when a parked workload runs. Patch: structural-only statement and a clock scale-up trigger added. |
| Blind 18: inventory guard checks a union, not the claims | Low | Unlikely in everyday use. The fix adds assertion logic, and module CI should not grow story-evidence checks. Rejected. |
| Blind 19: runbooks lack the rollout lessons | Medium | A pre-existing runbook gap exposed by the rollout. Defer. |
| Blind 20: intake pause narrowed without a re-check | Low | No Ingress, NodePort or LoadBalancer targets the namespace, tenants were empty, and the narrowed policy still denied all other ingress. A post-hoc re-run is impossible. Rejected. |
| Blind 21: rehearsal never Ready; password newline mismatch | Medium | Pre-existing probe versus `--requirepass` behavior. Defer. |
| Blind 22: DB0-only check, rollback-input location, unsourced Dapr claims | Low | Patch: location and durable sources named, rollback target sourced, skew claim marked unsourced. The DB0 part is false: no Memories component sets `redisDB`. |
| Blind 23: C1 and evidence disagree about `eventstore` | False | C1 and matrix row 3 scope the reserved live read to `hexalith-memories`, and the ext-secrets eventstore cannot load Memories' namespaced components. Rejected. |
| Blind 24: "Owner action, not performed" labels contradict | Low | No test pins the text. Direct correction. Patch: labels reworded. |
| Blind 25: review gates not on record | Medium | The scope gate failed on `epic-31-context.md`. Patch: added to File Scope and File List. Scope and readiness gates now pass in trailer mode; readiness C1 is skipped on `main` and enforced in CI. |
| Edge 1: version sentinels pass; `+build` always fails | Low | Same root cause as Verification other 1. Patch. |
| Edge 2: allow-list scope would also deny the synthetic name | Low | The live read found all four Configurations with `defaultAccess: deny` and no `allowedSecrets`. Patch: the review replay pairs the probe with that structural read. |
| Edge 3: disabled built-in store reported unverified | Low | The deployment uses the deny-scope remediation, and the fix adds a branch for an unused alternative. Rejected. |
| Edge 4: read-mode invalid output prints `invalid absent` | Low | A malformed daprd response is unlikely, the probe still exits 1, and the fix adds a conditional. Rejected. |
| Edge 5: inventory union hides a stale Present row | Low | Same as Blind 18. Rejected. |
| Edge 6: McpCli census pins the old probe hash | Medium | `amendments/1.0.14.json` pins `b932a85a…`. The amendment belongs to McpCli. Defer. |
| Edge 7: marker reopen trigger removed | Medium | Same as Blind 11. Patch. |
| Edge 8: rotation reopen trigger removed from runtime residue rows | Medium | Confirmed against HEAD. Patch: restored in the three rows. |
| Edge 9: AC2 closure rests on a stale 166/167 record | Medium | Same as Blind 6. Patch. |
| Edge 10: `builtin` passes without the version argument | Medium | Same as Blind 16. Patch. |


## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true` -- expected: 0 warnings, 0 errors.
- Run built xUnit assembly directly for `RuntimeSecretStoreMigrationTests`, `OpenBaoPlatformDocumentationTests`, and `ProductionDeploymentArtifactsTests` -- expected: all cases pass, 0 skipped (164+ cases baseline).
- `python3 -B -m unittest discover -s tests/tooling/runtime_secret_store` (with BusyBox available for `test_scope_probe.py`) -- expected: all probe and fail-closed cases pass without canary leakage.
- `git diff --check` -- expected: clean exit 0.

**Manual checks (if no CLI):**
- Replay scoped Vault and built-in `kubernetes` store probes across running pods; record sidecar versions, sanitized names/types inventory, grant diff, `ext-secrets-1-isolated` and `hexalith-memories` canary health, Velero backup/rehearsal IDs, and any retained unseeded Secret disposition in `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md`.
