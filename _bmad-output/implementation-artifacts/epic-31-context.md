# Epic 31 Context: OpenBao Secrets Platform and Runtime Secret-Store Migration

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Own, harden, document, and security-review the already-deployed OpenBao `hexalith-keys` cluster platform, then migrate the runtime Dapr `secretstore` from Kubernetes Secrets to `hashicorp.vault`, so runtime secret resolution crosses one reviewed boundary and every remaining Kubernetes Secret is a justified exception. This epic formalizes a running operations platform; it does not stand one up. It is Operational Readiness work, excluded from MVP product readiness, and requires explicit sprint selection. Aspire/AppHost-local topology and provider-neutral composition belong to Epic 29 — neither epic may close on the other's evidence.

## Stories

- Story 31.1: OpenBao Platform Hardening and Documentation
- Story 31.2: Runtime Dapr Secret-Store Migration to `hashicorp.vault`

## Requirements & Constraints

- Product services retrieve embedding-provider and other application runtime secrets exclusively through the Dapr Secrets API, backed by OpenBao in deployed environments. Secret values must not appear in application configuration, ordinary environment variables, source control, logs, telemetry, CLI output, evidence artifacts, operations documentation, or test snapshots. Unseal keys, recovery keys, root or operator tokens, and other secret values must never appear in operations documentation or evidence.
- Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials or CA material, or direct pod inputs that Dapr cannot inject. After migration, every remaining Kubernetes Secret needs a recorded justification.
- Verification must prove successful Dapr reads, unauthorized and cross-scope reads fail closed, restart recovery, product projects contain no OpenBao SDK, HTTP client, endpoint, or provider credential, and secret values cannot leak into logs, telemetry, CLI output, or snapshots. Component type and pod readiness are not enough: an executed lane must load the vault-typed stores against a reachable OpenBao and resolve consumer secret references. The disposable production-deployment lane substitutes vault-typed stores with Kubernetes stores and cannot close this proof.
- The measured availability profile is single-node-hosted Raft: voters are co-located on one Kubernetes node, so that node is the whole failure domain even when HA mode is on. The static file-based seal (unseal key held in a Kubernetes Secret beside the data) and namespace-wide port 8200 ingress are accepted limitations of that profile. They must not be described as hardened or production-HA. Each limitation needs an owner, consequence, compensating controls, and a reopen trigger. Documented voter count and HA mode must match the running platform, not a drifted tracked manifest.
- Sensitive values are never resolved through configuration fallback. Product services must not treat Aspire secret parameters, .NET User Secrets, or Kubernetes Secrets as an alternative runtime secret provider.
- Close-out needs independent security countersignature of the documented limitations. In-story reviewer-persona evidence is not independent. Epic closure stays blocked until an Administrator-named independent reviewer and schedule exist; do not invent that owner. The obligation is not waived and is not moved onto Story 31.2.

## Technical Decisions

- Two isolated Dapr secret-store components, each with a distinct read-only policy (cross-prefix reads fail closed):
  - `secretstore` maps to OpenBao prefix `secret/hexalith/memories/runtime` for Memories Server and embedding/LLM secret consumers.
  - `access-telemetry-secrets` maps to OpenBao prefix `secret/hexalith/memories/access-telemetry` for Memories Server, access-telemetry lifecycle, and clock.
- Application secret payloads use `secretstores.hashicorp.vault`. Product code depends only on Dapr and must not construct OpenBao endpoints or read Kubernetes Secrets for application payloads.
- After migration, the runtime `secretstore` component uses `hashicorp.vault` with `memories` and `eventstore` scopes. Prove `memories` with a live scoped read. `eventstore` is a reserved scope with no deployed Dapr app-id `eventstore` (EventStore is consumed as a linked module, not a Dapr workload), so prove it structurally: declared presence plus a demonstrated denial from a non-scoped app-id. Do not fabricate an `eventstore` workload. A live `eventstore` read becomes required if an `eventstore`-app-id workload is later deployed.
- Access-telemetry secret components and the `PG-ONPREM-1` secret backing stay in Epic 27 adapter scope and are not migrated here. Story 31.2 may read the access-telemetry store's live vault type and consumer resolution as verify-only context. The production-deployment verifier and its Kubernetes-store substitution stay with Story 27.3 and must not be edited by this epic.
- Story 31.1 documents the measured deployed configuration, including named drift from the tracked manifest. Proving that a reconciled manifest reproduces the live Helm release is Platform Operations work outside this epic's close-out.
- Migrating remaining direct-pod-input Kubernetes Secrets beyond the bootstrap exception requires a separately approved design (for example Agent Injector or CSI).
- Story 31.2 may append the remaining-Secret inventory to the OpenBao operations document; Story 31.1's measured-platform sections stay frozen.

## Cross-Story Dependencies

- Story 31.2 must not enter implementation (`dev-story`) until Story 31.1's platform documentation is complete and guard-asserted: topology documented at the exact deployed configuration, smoke test runnable with a recorded result, and accepted limitations on record. Story 31.1 reaching `done` is not that gate; `done` still waits on the independent security countersignature that neither story's development can produce. Story preparation may proceed before the gate.
- Epic 29 owns Aspire/AppHost-local OpenBao topology and provider-neutral composition. Epic 31 owns the deployed-cluster platform and the runtime `secretstore` migration. Evidence from one cannot close the other.
- Epic 27 retains access-telemetry secret components, `PG-ONPREM-1` backing, and the production-deployment verifier. Story 31.2 must not take those over.
- Story 31.2's implementation baseline is sequenced after Epic 28's EventStore dependency-identity work lands, so the migration is evaluated against the final EventStore/Builds gitlinks. Delay in Epic 28 shifts when that baseline is taken, not the sequencing rule.
