# Epic 31 Context: OpenBao Secrets Platform and Runtime Secret-Store Migration

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Make the deployed OpenBao `hexalith-keys` platform independently reviewable through accurate configuration documentation, explicit operating limitations and security evaluation, then migrate the deployed runtime Dapr secret store from Kubernetes Secrets to OpenBao. This formalizes an existing operations platform. The epic belongs to Operational Readiness / Secret Management Platform, requires explicit sprint selection and supplies no MVP product-readiness credit. Platform documentation, runtime migration and Production qualification each require their own evidence.

## Stories

- Story 31.1: OpenBao Platform Hardening and Documentation
- Story 31.2: Runtime Dapr Secret-Store Migration to `hashicorp.vault`

## Requirements & Constraints

- Product services retrieve embedding-provider, application and data-plane credentials exclusively through the Dapr Secrets API backed by OpenBao. Secret values, including unseal/recovery keys and root/operator tokens, must never enter ordinary configuration or environment variables, source control, workflow history, logs, telemetry, public contracts, CLI output, documentation, evidence or snapshots.
- Deployments provide only documented minimum bootstrap material outside that boundary. Protected local bootstrap and one-time seeding inputs do not become alternative runtime secret providers. Every remaining Kubernetes Secret requires a justified purpose as unavoidable OpenBao bootstrap material or a direct pod input Dapr cannot inject. Direct Redis/FalkorDB credential injection remains an alignment gap rather than an approved second application-secret path.
- Document availability as measured. Co-located Raft voters provide pod/process failover while their single Kubernetes node remains the whole failure domain. Voter count and HA mode follow current observations rather than tracked manifests. Never infer node/site resilience or production-HA from the voter count.
- Retain the static file-based seal, whose unseal key resides in a Kubernetes Secret beside the data, and namespace-wide port 8200 ingress as two explicit limitations. Each needs an accountable owner, consequence, compensating controls and reopen trigger. Neither may be represented as hardened or production-HA.
- Documentation checkpoints are complete, but independent limitation countersignatures and the platform-wide evaluation remain pending without completion dates. The platform-wide waiver expired on 2026-10-26; the independent reviewer is unassigned and Administrator (`jpiquot`) remains accountable for scheduling. Preserve historical waiver evidence without inventing approval or treating an owner or owner persona as an independent reviewer. Story 31.1 cannot reach `done` before those obligations are discharged.
- Distinguish approved repository inputs, historical observations and the current measured deployment. OpenBao `2.6.4` / chart `0.29.6` are repository-adopted inputs, while the smoke-test asset retains `2.6.0`. Pins and a successful status smoke do not establish current security qualification. Qualify exact image/chart/configuration bytes with current rerunnable evidence; version changes and risk acceptance do not substitute for Production isolation evidence.
- Runtime migration evidence must demonstrate actual scoped Dapr secret resolution, unauthorized and cross-prefix denial, provider-neutral product dependencies and secret-safe diagnostics. Pod readiness and component type declarations alone are insufficient. A disposable lane that substitutes Kubernetes stores for vault stores proves no Production OpenBao path.

## Technical Decisions

- Keep bootstrap, application, data-plane and operator scopes separate and least-privileged. Product projects depend on Dapr rather than OpenBao SDKs, direct OpenBao HTTP clients/endpoints, Kubernetes Secret reads or application-secret configuration fallbacks.
- Migrate the runtime store to `secretstores.hashicorp.vault` with `memories` and reserved `eventstore` scopes. Prove `memories` through a live scoped read. Prove reserved `eventstore` structurally through declared presence and demonstrated denial from a non-scoped app-id; do not fabricate an absent workload. Deployment of an `eventstore` app-id reopens the live-read requirement.
- Platform documentation may record a manifest/live divergence as an owned gap. Reproducing the live Helm release from reconciled manifests remains separate Platform Operations work rather than a Story 31.1 checkpoint. Preserve its owner and reopen evidence requirement.
- Revoke superseded credentials at the provider within 60 seconds of committed rotation, and prevent connections or clients established under those credentials from outliving that bound. A consumer unable to prove freshness fails closed. Production qualification also requires per-tenant backend principals and current isolation evidence; dated risk acceptance supplies no qualification credit.

## Cross-Story Dependencies

- Story 31.2 implementation requires Story 31.1 documentation checkpoints C1, C2, C3, C4a, C5a and C6 to be complete and guard-asserted. It does not require Story 31.1 `done`. Independent security obligations C4b, C5b and C7 stay with Story 31.1 and are neither waived nor transferred by migration activation.
- Epic 29 owns Aspire/AppHost-local secret topology and provider-neutral composition; Epic 31 owns the deployed-cluster platform and runtime migration. Neither may close on the other's evidence. Hexalith.Platform owns host composition and lifecycle.
- Epic 27 retains access-telemetry-specific secret components, adapter secret backing and the disposable production-deployment verifier, including its Kubernetes-store substitution. Story 31.2 verifies those stores as context without migrating them or changing that verifier.
- Story 31.2 appends the remaining-Secret justification inventory while preserving Story 31.1's historical measured-platform evidence. Its implementation baseline follows Epic 28's owner-approved EventStore runtime identity work. Exact OpenBao Production qualification retains its separate current-profile evidence obligation.
