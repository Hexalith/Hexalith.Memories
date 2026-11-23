---
title: '31.2: Close the live Kubernetes secret-store path'
type: 'bugfix'
created: '2026-11-23'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
story_key: '31-2-runtime-dapr-secret-store-migration'
baseline_commit: '1bf1937c1f86fd6ce4e2f6957721bbecf83b1b35'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
---

<frozen-after-approval reason="human-owned intent">

## Intent

Finish Story 31.2 checkpoint C2 on `jpiquot@local`. Deploy the approved Dapr 1.19.2 secret boundary, prove that running Memories sidecars enforce the built-in Kubernetes store deny policy, and account for every remaining Kubernetes Secret.

## Boundaries & Constraints

Follow the approved shared control-plane scope, including `ext-secrets-1-isolated`. Preserve the live application images, replica counts, PostgreSQL, component scopes and required OpenBao bootstrap grants. Use the Dapr 1.19.2 candidate to derive a scoped live diff. Keep Secret inspections to names, types and references. Reduce Dapr probe results to status and field presence inside the pod. Keep the existing qualification profile inputs and historic evidence intact.

## I/O & Edge-Case Matrix

| Scenario | Required result |
| --- | --- |
| Recovery, drain or live diff evidence is missing | Hold cluster changes and keep C2 pending with the specific blocker. |
| Upgraded sidecar | Verify runtime and app identity. Vault reads and existing denials still pass. A synthetic name through the built-in store receives an explicit policy denial. |
| Parked workload or reserved `eventstore` scope in Memories | Record configuration and workload inventory. Require a live read when the corresponding app runs in the namespace. |
| Residue Secret | Verify references and bindings. Before deletion, verify replacement key presence. Record the owner disposition and remeasure the inventory. |

</frozen-after-approval>

## Open Questions

1. Which approved recovery and intake records apply to `jpiquot@local`? Provide the paired Redis/FalkorDB recovery point, isolated restore evidence and intake pause/resume playbook references if they exist. If they still need to be produced, identify the approved off-cluster destination, non-CSI backup procedure and pause/resume playbook so the maintenance prerequisites can be prepared.
2. What is the owner disposition for each of `llm-secret`, `google-embedding-api-key`, `memories-embedding-client-secret` and `access-telemetry-marker-key`? Approve deletion after checking live consumers and replacement key presence, or give an explicit retention decision with owner, consequence and reopen trigger. Story 27.3 and Platform Operations own the marker disposition.

## Code Map

- `deploy/kubernetes/overlays/production-dapr-1.19/kustomization.yaml` defines four Configuration scopes, four image annotations and two Role reductions. Its parent production render parks telemetry and carries placeholder application tags. Live telemetry and PostgreSQL each have one ready replica. Preserve the telemetry bootstrap grant used by `memories` while the live telemetry component includes that app.
- `docs/operations/upgrade-migration.md` and `docs/operations/backup-restore.md` define compatibility, scoped rollback, recovery and drain admission. Read-only preflight on November 23 found Dapr 1.18.1, Kubernetes v1.34.9, kubectl v1.36.1, no snapshot API resources, and all four residue names. Use a compatible client for changes.
- `tools/probe-dapr-secret-scope.sh` covers existing Vault cases. `tests/Hexalith.Memories.Server.Tests/Deployment/RuntimeSecretStoreMigrationTests.cs` guards rendered contracts. The previous replay and server dry runs are in `tests/31-2-runtime-secret-store-evidence.md` beside the story.
- `docs/operations/openbao.md` describes owner and credential-presence checks. The story's C2 row and its `sprint-status.yaml` entry publish closeout status.

## Tasks & Acceptance

- [ ] Extend `tools/probe-dapr-secret-scope.sh` and `tests/tooling/runtime_secret_store/test_scope_probe.py` for an identity-checked built-in store denial. Exercise the new check in a disposable fixture before rollout.
- [ ] Using `docs/operations/upgrade-migration.md` and `docs/operations/backup-restore.md`, verify the recovery point and isolated restore, review the exact scoped live diff and rollback inputs, and obtain direct drain evidence under the approved maintenance procedure.
- [ ] Using `deploy/kubernetes/overlays/production-dapr-1.19/kustomization.yaml`, stage the scoped changes, upgrade the shared Dapr control plane and roll the approved sidecars. Preserve required bootstrap grants, verify both namespaces, and record the sanitized live matrix in `tests/31-2-runtime-secret-store-evidence.md` beside the story.
- [ ] Reconcile live references, grants and owner dispositions in `docs/operations/openbao.md`. Update the story's C2 row and `sprint-status.yaml` only when the deployed evidence supports closure.

Given the upgraded, healthy deployment, when each running Memories sidecar is checked, then authorized Vault lookups still work where scoped and the built-in Kubernetes store denies application reads. Given the live Secret inventory, when all references and grants are reconciled, then every Secret has a justified use or an explicit owner disposition.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- Build `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` in Debug with source references. Run the runtime secret, OpenBao documentation and production deployment classes. Then run `python3 -B -m unittest discover -s tests/tooling/runtime_secret_store` with BusyBox available. The November 23 planning baseline passed 164 C# cases and all 13 probe subcases.
- Replay the scoped Vault and built-in store checks. Record the sidecar versions, sanitized inventory, grant diff, canary health and observation results. Record any remaining precondition as pending and run `git diff --check`.
