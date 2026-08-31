# Epic 29 Context: OpenBao-First Dapr Secret Management

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Aspire-hosted services must resolve application secrets exclusively through Dapr secret-store components backed by OpenBao, rather than local-file or Kubernetes secret stores. This closes the operational-readiness gap where product code could depend on a non-provider-neutral or less-secure secret path. Kubernetes Secrets remain permitted only for unavoidable bootstrap credentials or direct pod inputs that Dapr cannot inject. This epic owns only the Aspire/AppHost-local secret topology and the provider-neutral composition surface; the deployed-cluster OpenBao platform and the runtime Dapr `secretstore` migration belong to Epic 31 and must not be closed on this epic's evidence (or vice versa).

## Stories

- Story 29.1: OpenBao-Backed AppHost Secret Topology
- Story 29.2: Provider-Neutral Aspire Composition and Secret Verification

## Requirements & Constraints

- Product services must retrieve embedding-provider and other application runtime secrets exclusively through the Dapr Secrets API (`DaprClient.GetSecretAsync`), backed by OpenBao. Secret values must never appear in application configuration, ordinary environment variables, source control, logs, diagnostics, or Aspire model output.
- Configuration precedence places Dapr Secrets (backed by OpenBao) above Dapr configuration for sidecar/component settings, and secrets are never resolved through configuration fallback.
- Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials/CA material or direct pod inputs that Dapr cannot supply; every such exception must be documented and tested.
- Verification must be structural and behavioral: dependency tests, secret scanning, AppHost topology tests, and integration tests proving Dapr reads succeed and cross-prefix reads are denied, without disclosing secret values.
- Sequencing gate: Story 29.1 (topology) must be executable — resource, bootstrap, isolation, and readiness contract in place — before Story 29.2 (provider-neutral composition and verification) can claim completion.
- Reinforces NFR9 (exclusive Dapr-Secrets-via-OpenBao retrieval, with narrow documented Kubernetes Secret exceptions).

## Technical Decisions

- Two isolated Dapr secret-store components, each with a distinct read-only least-privilege policy (cross-prefix reads fail closed):
  - `secretstore` → OpenBao prefix `secret/hexalith/memories/runtime` → consumed by Memories Server and components resolving embedding/LLM secrets.
  - `access-telemetry-secrets` → OpenBao prefix `secret/hexalith/memories/access-telemetry` → consumed by Memories Server, access-telemetry lifecycle, and clock.
- Both components use `secretstores.hashicorp.vault`, never `secretstores.local.file` or `secretstores.kubernetes`, for application secret payloads.
- The Aspire AppHost owns the OpenBao resource, its health/initialization sequencing, Dapr component generation, protected bootstrap inputs, and secret seeding. Consumer services wait for OpenBao initialization and reference only their required Dapr component — they do not get broader access.
- A development-mode OpenBao profile must be explicit and pinned so it cannot silently become (or be mistaken for) a production topology.
- Bootstrap exception: the Dapr component must authenticate before it can read OpenBao. Locally, this uses Aspire secret parameters or protected temporary credential files — never committed secrets. In Kubernetes, narrowly scoped Secrets may hold only required OpenBao bootstrap tokens and CA certificates; migrating any further direct-pod-input exception requires a separately approved design (e.g., Agent Injector or CSI).
- Product code must depend only on Dapr — no OpenBao SDK, no OpenBao HTTP client/endpoint construction, no OpenBao provider credentials anywhere in product projects (including standalone Dapr templates and MCP/server/access-telemetry/clock code paths).
- The reusable `Hexalith.Memories.Aspire` extensions must accept externally provisioned Dapr secret-store resources rather than hard-coding a specific store, so downstream consumers can supply OpenBao (or another provider) without product code taking a direct dependency.
- This strengthened OpenBao-first contract supersedes prior secret-store decisions in already-completed historical stories (e.g., an earlier story's secret-provider approach and an earlier template's `secretstore.yaml` provider choice) — those stories remain historical/completed and are not reopened, but any current template or documentation must reflect the OpenBao-first rule.

## Cross-Story Dependencies

- Story 29.2 depends on Story 29.1: it cannot claim provider-neutral composition or Dapr access verification until Story 29.1's OpenBao resource, bootstrap, isolation, and readiness contract is executable in the AppHost.
- Epic 29 and Epic 31 share the same underlying secret-management contract (NFR9, `secretstore`/`access-telemetry-secrets` components) but own disjoint scopes: Epic 29 is Aspire/AppHost-local topology and provider-neutral composition; Epic 31 is the deployed-cluster OpenBao platform and the runtime secret-store migration. Neither epic's stories may be closed using the other's evidence.
