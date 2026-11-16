# Epic 31 Context: OpenBao Secrets Platform and Runtime Secret-Store Migration

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Make the already-deployed OpenBao `hexalith-keys` platform reviewable through exact configuration documentation, explicit operating limitations, and independent security evaluation; then migrate the deployed runtime Dapr `secretstore` to OpenBao so every remaining Kubernetes Secret has a justified purpose. This formalizes an existing operations platform. The epic belongs to the Operational Readiness track, requires explicit sprint selection, and is excluded from MVP product-readiness credit. Platform documentation, runtime migration, and Production qualification require their own evidence.

## Stories

- Story 31.1: OpenBao Platform Hardening and Documentation
- Story 31.2: Runtime Dapr Secret-Store Migration to `hashicorp.vault`

## Requirements & Constraints

- Product services retrieve embedding-provider, application, and data-plane credentials exclusively through the Dapr Secrets API backed by OpenBao. Secret values must never enter ordinary configuration or environment variables, source control, workflow history, logs, telemetry, public contracts, CLI output, documentation, evidence, or snapshots. This includes unseal/recovery keys and root/operator tokens.
- Deployments supply only minimum, documented OpenBao bootstrap material outside that boundary. Narrowly scoped Kubernetes Secrets may hold required tokens and CA material. Direct pod inputs that Dapr cannot inject require a recorded exception and a separately approved migration design; direct Redis/FalkorDB credential injection remains an alignment gap and cannot qualify Production.
- Describe the availability profile as measured. Three Raft voters on one Kubernetes node provide pod/process failover while the node remains the whole failure domain. Voter count and HA mode must follow current observations rather than tracked manifests or historical measurements.
- The static file-based seal, whose key resides in a Kubernetes Secret beside the data, and namespace-wide port 8200 ingress are explicit limitations. Each needs an owner, consequence, compensating controls, and reopen trigger; neither may be represented as hardened or production-HA.
- Documentation completion does not supply an independent security countersignature. The owner and reviewer persona acting for that same owner are not independent. Both limitation countersignatures remain open, and the platform-wide evaluation waiver expired on 2026-10-26 without a recorded independent evaluation. Preserve the historical waiver without treating it as current approval or inventing a reviewer. Story 31.1 cannot reach `done` until these obligations are discharged.
- Evidence must distinguish approved repository inputs from deployed observations. Repository adoption of OpenBao 2.6.4 and chart 0.29.6 does not establish deployment or current security qualification. The measured 2.6.0 profile and smoke result are historical; an older CA-only client pin is not proof of the new server or client/server compatibility. Qualify the exact image/chart/profile with current rerunnable evidence.
- Verification must prove successful Dapr secret reads, unauthorized and cross-prefix denial, restart recovery, provider-neutral product dependencies, and secret-safe diagnostics. Component types and pod readiness alone are insufficient. A disposable lane that substitutes Kubernetes stores for vault stores cannot prove the Production OpenBao path.

## Technical Decisions

- Keep bootstrap, application, data-plane, and operator scopes separate and least-privileged. Product code depends on Dapr, without OpenBao SDKs, direct OpenBao HTTP clients/endpoints, Kubernetes Secret reads, or configuration fallbacks for application secrets. Protected local bootstrap/seeding inputs do not become another runtime provider.
- Two Dapr components use distinct read-only policies: `secretstore` maps to `secret/hexalith/memories/runtime` for Memories and embedding/LLM consumers; `access-telemetry-secrets` maps to `secret/hexalith/memories/access-telemetry` for Memories, lifecycle, and clock. Cross-prefix reads fail closed.
- The runtime migration uses `secretstores.hashicorp.vault` with `memories` and reserved `eventstore` scopes. Prove `memories` through a live scoped read. Prove reserved `eventstore` structurally through declared presence and demonstrated denial from a non-scoped app-id; do not fabricate an absent workload. A deployed `eventstore` app-id reopens the live-read requirement.
- Platform documentation may record manifest/live divergence as an owned gap. Reproducing the live Helm release from reconciled manifests is a separate Platform Operations obligation, rather than a Story 31.1 completion gate. Preserve that obligation and its evidence requirements.
- Credentials revoked by rotation must stop authenticating, and clients using superseded credentials must stop, within the committed-rotation 60-second freshness bound. Unavailable freshness proof fails closed. Risk acceptance and version pins never substitute for Production isolation/security qualification.

## Cross-Story Dependencies

- Story 31.2 implementation requires complete, guard-asserted platform documentation: Story 31.1 checkpoints C1, C2, C3, C4a, C5a, and C6. It does not require Story 31.1 `done`. Independent security obligations C4b, C5b, and C7 remain with Story 31.1 and are neither waived nor transferred by migration activation.
- Epic 29 owns Aspire/AppHost-local topology and provider-neutral composition; Epic 31 owns the deployed platform and runtime migration. Neither can close on the other's evidence. Host composition and lifecycle ownership remain with Hexalith.Platform.
- Epic 27 retains access-telemetry-specific secret components, its adapter secret backing, and the disposable production-deployment verifier/substitution. Story 31.2 may verify those stores as context without migrating them or changing that verifier.
- Story 31.2 appends the remaining-Secret justification inventory without rewriting Story 31.1's historical measured-platform evidence. Its implementation baseline follows Epic 28's owner-approved EventStore identity work so verification uses the resulting dependency baseline.
