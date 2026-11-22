---
title: '31.2: Verify deployed Dapr secret resolution through OpenBao'
type: 'feature'
created: '2026-11-22'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
story_key: '31-2-runtime-dapr-secret-store-migration'
baseline_commit: '043fb04e46b97757c599b5851c42d634913474a5'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
---

<frozen-after-approval reason="human-owned intent">

## Intent

Give operators reproducible evidence that the deployed `memories` app resolves its allowed credentials through Dapr's OpenBao-backed store, that the deployment enforces its access boundaries, and that every Kubernetes Secret left in the namespace has an accountable use or disposition.

## Boundaries & Constraints

Measure the existing `jpiquot@local` deployment in `hexalith-memories` using read-only probes. Retain TLS verification, bootstrap references, the runtime prefix and both component scopes. Inspect Secret names and types only. Keep Dapr tokens inside the pod and reduce responses there to key presence and HTTP status before emitting evidence. Exclude response bodies and credential values from every output. Reserve cluster changes, credential rotation, residue deletion and risk acceptance for their owners. Keep Epic 29 topology, Story 27.3’s verifier and Story 31.1’s platform record outside this change.

## I/O & Edge-Case Matrix

| Check | Required observation |
| --- | --- |
| Allowed name from running `memories` | Successful scoped read and expected field present. Record key name and status. |
| Same key from `memories-mcp`; disallowed or cross-prefix name from `memories` | Authorized sidecar request receives a store-scope or allow-list denial. Distinguish it from token errors, transport failure and an absent key. |
| Reserved `eventstore` scope | Verify scope membership, non-scoped denial and current workload inventory. A deployed app with that ID triggers a live read. |
| Runtime store cannot authenticate or load | Disposable failure test observes the component failure and absence of a successful read or fallback. |
| Cluster context unavailable | Record the failed preflight. Keep current inventory, drift and scoped-read checkpoints open. |

</frozen-after-approval>

## Code Map

- `deploy/kubernetes/base/dapr/{secretstore,config,statestore,pubsub,conversation-openai}.yaml` defines the store, policy and consumer routing. The access-telemetry store has its own bootstrap and prefix. Compare component declarations with both rendered overlays and the live component’s nonsecret fields.
- `deploy/kubernetes/base/service-accounts-rbac.yaml` names the grants. Reconcile them with pod env, volumes, image pulls, service accounts and Dapr bootstrap references. A component reference using `auth.secretStore` resolves through that named store.
- In `tests/Hexalith.Memories.Server.Tests`, reuse `NaturalLanguage/SubmoduleGuardTests.cs` for project scanning, the Deployment helpers for repository paths and rendered manifests, and `OpenBaoPlatformDocumentationTests.SecurityGuards.cs` for safe diagnostics. `src/Hexalith.Memories.Server/Ingestion/EmbeddingSecretStore.cs` is a Dapr consumer.
- `deferred-work.md` entries DW-726 and DW-727 record the September CR29/CR30 dispositions and their packet-verification limits. Story 29.2 migrated `deploy/dapr/components/secretstore.yaml`. Preserve those records and distinguish staged evidence from current deployed evidence.

## Tasks & Acceptance

- [x] In `Deployment/RuntimeSecretStoreMigrationTests.cs`, bind the Epic gate, local gate and Task 0 to exactly C1/C2/C3/C4a/C5a/C6. Require unique completed checkpoint rows. Mutation-test a missing checkpoint and a reinstated story-status gate.
- [ ] Record the current context, UTC timestamps, namespace, app IDs, pod names, component metadata and names/types inventory in `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md`. Compare tracked and deployed component settings, then exercise the matrix. Attribute each denial to its actual control. Record ownership and a reopen trigger for drift.
- [ ] Append the inventory in `docs/operations/openbao.md` using the legacy story's six columns and five classes. Reconcile the measured set and July baseline, both directions. Distinguish direct Redis/FalkorDB inputs, OpenBao keys, granted names and unused objects. Record residue disposition, owner, consequence and trigger without inventing acceptance.
- [x] Add guards for all product projects, Vault type and scopes in deployed artifacts, simulated bootstrap failure, and safe diagnostics. Check package references, provider clients, endpoint/configuration and credential patterns. Use synthetic canaries at observable output boundaries, including CLI, logging, telemetry and snapshots. Mutation-prove forbidden provider access and unsafe output with redacted failures. Scope the deployment guard to `deploy/kubernetes`.
- [x] Reconcile the parent story checkpoints and deferred-work references to attributable evidence. Record runner-derived method counts, mutation results, file list and outstanding blockers. Keep story and sprint status consistent.

Given a measured deployment, when operators replay the documented probes, then the matrix and per-Secret inventory substantiate AC1. Given product source and a synthetic secret, when the structural and output tests run, then provider coupling and value disclosure fail AC2 guards without disclosing the canary in their diagnostics.

## Implementation Notes

The base and both overlays retain the Vault runtime component and its two scopes. Production additionally removes the telemetry store from memories scope and removes the telemetry bootstrap grant. Story 29.2 already owns the migrated standalone template. The July inventory and current tracked routing are reconciled in the operations document. Current names/types and deployed drift remain unverified.

## Spec Change Log

## Review Triage Log

## Verification

Build `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` in Debug using the pinned .NET 11 SDK and source references. Discover methods with `-list methods`, then execute the migration, platform documentation and production deployment classes directly with xUnit. Run the applicable CLI/telemetry tests, the tenant isolation evidence checker with the explicit changed-file list, and `git diff --check`. Keep current deployed results, disposable failure tests and archived CI results separately attributable.

The run record is `tests/31-2-runtime-secret-store-evidence.md`. C3/C4 and historical C5 have attributable evidence. C1/C2 await read-only access to jpiquot@local. Story and sprint remain in progress.
