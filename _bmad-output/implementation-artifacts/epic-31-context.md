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
- On 2026-11-22, Jérôme Piquot (`jpiquot`) closed Story 31.1 by sole-owner risk acceptance for C4b, C5b and C7. No independent countersignatures were obtained. EVAL-01 remains open with Platform Operations. The six documentation checkpoints are complete. The July C7 waiver expired on 2026-10-26.
- Distinguish approved repository inputs, historical observations and the current measured deployment. OpenBao `2.6.4` / chart `0.29.6` are repository-adopted inputs, while the smoke-test asset retains `2.6.0`. Pins and a successful status smoke do not establish current security qualification. Qualify exact image/chart/configuration bytes with current rerunnable evidence; version changes and risk acceptance do not substitute for Production isolation evidence.
- Runtime migration evidence must demonstrate actual scoped Dapr secret resolution, denied access, provider-neutral product dependencies and secret-safe diagnostics. OpenBao security evidence also covers cross-prefix denial and restart recovery. Record each required checkpoint with an accountable owner, evidence command or artifact, review state and completion state. A disposable lane that substitutes Kubernetes stores for Vault cannot prove the deployed OpenBao path.

## Technical Decisions

- Keep bootstrap, application, data-plane and operator scopes separate and least-privileged. Use distinct read-only OpenBao policies for the runtime and access-telemetry components, with cross-prefix requests failing closed. Product projects use the Dapr Secrets API for secret resolution.
- Migrate the runtime store to `secretstores.hashicorp.vault` with `memories` and reserved `eventstore` scopes. Prove `memories` through a live scoped read. Prove reserved `eventstore` structurally through declared presence and demonstrated denial from a non-scoped app-id; do not fabricate an absent workload. Deployment of an `eventstore` app-id reopens the live-read requirement.
- Platform documentation may record a manifest/live divergence as an owned gap. Reproducing the live Helm release from reconciled manifests remains separate Platform Operations work rather than a Story 31.1 checkpoint. Preserve its owner and reopen evidence requirement.
- Revoke superseded credentials at the provider within 60 seconds of committed rotation, and prevent connections or clients established under those credentials from outliving that bound. A consumer unable to prove freshness fails closed. Production qualification also requires per-tenant backend principals and current isolation evidence; dated risk acceptance supplies no qualification credit.

## Cross-Story Dependencies

- Story 31.2 implementation requires Story 31.1 documentation checkpoints C1, C2, C3, C4a, C5a and C6 to be complete and guard-asserted. It does not require Story 31.1 `done`. The November 22 dispositions of C4b, C5b and C7 belong solely to Story 31.1.
- Epic 29 owns Aspire/AppHost-local secret topology and provider-neutral composition; Epic 31 owns the deployed-cluster platform and runtime migration. Neither may close on the other's evidence. Hexalith.Platform owns host composition and lifecycle.
- Epic 27 retains access-telemetry-specific secret components, adapter secret backing and the disposable production-deployment verifier, including its Kubernetes-store substitution. Story 31.2 verifies those stores as context without migrating them or changing that verifier.
- Story 31.2 appends the remaining-Secret justification inventory while preserving Story 31.1's historical measured-platform evidence. Its implementation baseline follows Epic 28's owner-approved EventStore runtime identity work. Exact OpenBao Production qualification retains its separate current-profile evidence obligation.
