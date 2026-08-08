<!-- Review cadence: update when a deploy-config variable name, Dapr sidecar port, pub/sub component name, or app-id default changes, or quarterly — whichever comes first. Last reviewed: 2026-07-12. -->

# Deployment Configuration Contract

This document publishes the canonical environment-variable, Dapr sidecar-port, OTLP-exporter, and pub/sub event-intake configuration surface a downstream operator must supply to deploy Memories into a Kubernetes overlay, so placeholder-shaped env literals in consumer kustomizations can be replaced with real, documented values without first running aspirate.

Origin: Story 18.2 published the downstream configuration surface. Story 26.1 adds the authoritative production Kustomize deployment while retaining the AppHost as the local topology reference.

> **Code is the source of truth.** Every literal in this document is mirrored from the authoritative source file named in its row. A drift-guard test (see [Automated enforcement](#automated-enforcement)) fails the build if a documented name diverges from code.

## Production Kustomize deployment (Story 26.1)

The committed production entry point is `deploy/kubernetes/overlays/production`. The deterministic render is:

```bash
kubectl kustomize deploy/kubernetes/overlays/production > /tmp/hexalith-memories-production.yaml
kubectl apply --dry-run=client -f /tmp/hexalith-memories-production.yaml
```

The render includes Server, MCP, Redis Stack, FalkorDB, DAPR sidecars/components, actor-enabled state, pub/sub, secret-store scoping, least-privilege Secret RBAC, probes, fixed bootstrap resources, and the 20 GiB/10 GiB persistent volumes. It contains no Kubernetes `Secret` values. The AppHost is not deployed, and neither Helm nor Aspire-published output is authoritative for this story.

Release automation produces `artifacts/deployment/hexalith-memories-production.yaml`, where both Server and MCP image tags equal the semantic-release version. For a manual non-release render, replace the `0.0.0` image placeholders in a downstream overlay; never deploy those placeholders unchanged.

### Required external inputs

Create the Secret resources below in namespace `hexalith-memories` through the infrastructure secret system. The names and keys are contracts; values must be non-empty and must not be committed. The production overlay itself generates `memories-production-config` with safe example OIDC values; a downstream overlay must replace those literals with the operator's real OIDC contract before deployment.

| Resource | Required keys | Consumers |
| :------- | :------------ | :-------- |
| Secret `redis-secret` | `password`, `falkordb-password` | Redis Stack, FalkorDB, Server, DAPR state/pubsub |
| Secret `llm-secret` | `OPENAI_API_KEY` | `llm-openai` Conversation component |
| Secret `google-embedding-api-key` | `google-embedding-api-key` | default Google embedding provider |
| Secret `memories-embedding-client-secret` | `memories-embedding-client-secret` | OIDC embedding provider mode |
| Secret `app-api-token` | `token` | DAPR sidecar-to-app authentication (`APP_API_TOKEN`) |
| Secret `dapr-api-token` | `token` | app-to-DAPR authentication (`DAPR_API_TOKEN`) |
| Secret `registry-credentials` | Kubernetes Docker config JSON | image pulls for Server and MCP |
| Secret `openbao-runtime-bootstrap` | `token`, `ca.pem` | TLS-verified, read-only OpenBao access for Dapr component `secretstore` |
| Secret `openbao-access-telemetry-bootstrap` | `token`, `ca.pem` | TLS-verified, read-only OpenBao access for Dapr component `access-telemetry-secrets` |
| Secret `access-telemetry-postgresql-bootstrap` | `admin-password`, `runtime-password` | PostgreSQL 18.4 `PG-ONPREM-1` StatefulSet bootstrap + initdb.d runtime-role creation |
| Secret `access-telemetry-postgresql-tls` | `tls.crt`, `tls.key`, `ca.crt` | PostgreSQL server TLS and the Dapr `access-telemetry-store` `verify-full` CA volume mount |
| Secret `access-telemetry-clock-key` | `verification-public-key` | lifecycle attestation verification (the clock's private signing key is OpenBao-resident) |
| Secret `access-telemetry-clock-sources` | `source-a-token`, `source-b-token`, `source-c-token` | `memories-access-telemetry-clock` external UTC source authentication |
| Generated ConfigMap `memories-production-config-*` | `OIDC_AUTHORITY`, `OIDC_ISSUER`, `OIDC_AUDIENCE`, `OIDC_TENANT_CLAIM` | identical Server/MCP OIDC validation contract; patch through Kustomize, not by creating a competing unhashed ConfigMap |

The OIDC authority and issuer must use HTTPS. Server and MCP intentionally consume the same audience and tenant-claim name. MCP forwards the validated inbound bearer unchanged when invoking Server; there is no production `Authentication:ServerUpstream` signing key.

> **Redis/FalkorDB password charset.** The `redis-secret` `password` and `falkordb-password` values are consumed inline — inside the `ConnectionStrings__redis` / `ConnectionStrings__falkordb` connection strings and the backends' `--requirepass` argument. Restrict them to characters that are safe in both contexts: avoid spaces, commas (`,`), equals signs (`=`), and `$`, which would otherwise split the connection string into spurious options or truncate the password passed to `--requirepass`. Prefer URL/shell-safe characters such as alphanumerics plus `-` `_` `.` `~`.

The two Dapr secret stores resolve their values from the internal `hexalith-keys` OpenBao service. Their
bootstrap Kubernetes Secrets hold only a scoped OpenBao token and CA; secret payloads are stored beneath
separate runtime and access-telemetry prefixes. Direct pod environment and container-argument consumers
still require their Kubernetes Secrets because Dapr does not inject environment variables. See
[Hexalith Keys OpenBao operations](./openbao.md) for the exact boundary, deployment profile, ownership,
rotation deadlines, and recovery requirements. The Dapr `access-telemetry-secrets` store additionally resolves the OpenBao-resident `access-telemetry-postgresql` connection string (which must carry `sslmode=verify-full` and `sslrootcert`), `access-telemetry-marker-key`, and the clock signing key from the `hexalith/memories/access-telemetry` prefix; these are not Kubernetes Secrets.

The default DAPR trust domain is `public` and the production namespace is `hexalith-memories`. If the cluster uses another trust domain or namespace, patch both the Server DAPR `Configuration` policy and the workload namespace together. Do not widen the deny-by-default `/api/v1/**` policy or add publisher app-ids: `eventstore` is the sole publisher and `memories` is subscriber-only.

The `memories-config` and `memories-access-telemetry-config` DAPR configurations explicitly disable the DAPR 1.18 `HotReload` feature. Both workloads use actor state stores, which DAPR cannot reload in place. After changing `statestore`, `access-telemetry-store`, or another component visible to either workload, apply the reviewed component manifest and run `kubectl rollout restart deployment/memories -n hexalith-memories`; when lifecycle replicas are enabled, also restart `deployment/memories-access-telemetry`. Wait for each rollout and require the structured `/ready` response to return top-level `status: Healthy`. Do not treat the DAPR log message that rejects an actor-state-store hot reload as evidence that a changed component was accepted.

### Apply and verify

1. Install DAPR 1.18 or later and confirm its control plane and injector are healthy.
2. Create the external resources above and make the two versioned images pullable through `registry-credentials`.
3. Render and run client-side validation using the commands above, then apply the rendered file.
4. Wait until the `memories` and `memories-mcp` application containers are running. Within 60 seconds, parse `/ready` and require top-level JSON `status` to equal `Healthy`; HTTP 200 alone is insufficient because `Degraded` also returns 200.
5. Run `tools/verify-production-deployment.ps1` with the four locally published OCI archives. The verifier creates a disposable DAPR-enabled cluster, stages a pinned TLS OpenBao (`2.6.0`) with the production endpoint DNS and bootstrap Secrets, leaves the production `secretstores.hashicorp.vault` components unmodified, checks schema/RBAC/ACL and Dapr secret allow/deny behavior, proves startup timing within the existing 60-second contract, and exercises optional-axis degradation plus critical Redis/DAPR/MCP-upstream failures. It has no skip path and must not fall back to `secretstores.kubernetes`.

The application port `8080` has no direct Service target. The committed `memories` and `memories-mcp` Services target DAPR port `3500`; any public ingress, TLS issuer, hostname, or network edge remains infrastructure-owned and must terminate at that DAPR seam.

### Rollback

Keep the preceding release's rendered deployment artifact. Reapply that artifact and use `kubectl rollout status` for `deployment/memories`, `deployment/memories-mcp`, `statefulset/redis-stack`, and `statefulset/falkordb`. If only a stateless image rollout is bad, `kubectl rollout undo deployment/<name> -n hexalith-memories` is sufficient. Do not delete Redis/FalkorDB PVCs during rollback; backup/restore is owned by Story 26.2.

## OTLP telemetry export

| Variable | Authoritative source | Semantics |
| :------- | :------------------- | :-------- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` | The OpenTelemetry OTLP exporter is wired **only when this value is non-empty**. When empty in Production the host logs a warning (`OtlpExporterWarningHostedService`) — telemetry is still collected in-process but is **not exported**. Not present in any `appsettings*.json`: this variable is **environment-only**. |

Set `OTEL_EXPORTER_OTLP_ENDPOINT` to the collector endpoint (for example the Aspire dashboard OTLP receiver, or your cluster's OpenTelemetry Collector). Leaving it empty is a supported, non-fatal posture: the service runs and self-collects, but exports nothing.

## Dapr sidecar ports

These are the **AppHost defaults**. The canonical source is `src/Hexalith.Memories.AppHost/Program.cs` — **not** the `Hexalith.Memories.Aspire` library (its sidecar options are parameterized and its inline comment mentions `3501`, which is not the deployed default).

| Service | Dapr app-id | HTTP port | gRPC port | Authoritative source |
| :------ | :---------- | :-------- | :-------- | :------------------- |
| Memories Server | `memories` (default; override with `MEMORIES_DAPR_APP_ID`) | `3500` | `50001` | `AppHost/Program.cs` (`ResolveDaprAppId`, sidecar options) |
| Memories MCP | `memories-mcp` | `3600` | `50101` | `AppHost/Program.cs` (offset so the MCP sidecar does not collide with the Server sidecar) |

**App-id reconciliation (documentation drift to be aware of):** the real Server Dapr app-id default is **`memories`** (returned by `ResolveDaprAppId`, overridable via the `MEMORIES_DAPR_APP_ID` environment variable). The architecture baseline (`_bmad-output/planning-artifacts/architecture.md` §Deployment Topology Baseline) projects the server app-id as `memories-server`; that is an unreconciled documentation projection. **Use `memories`** — it is the value the code emits. The MCP app-id `memories-mcp` is consistent across code and the architecture projection.

## Required runtime environment

| Variable / key | Default | Source-of-truth | Env-only or appsettings? |
| :------------- | :------ | :-------------- | :----------------------- |
| `EnableKeycloak` | enabled unless set to `false` | `AppHost/Program.cs` via `HexalithEventStoreSecurityExtensions` | AppHost/local-dev switch. Set to `false` to skip the local `security` resource and use symmetric-key/env-var JWT fallback for MCP. |
| `PUBSUB_REDIS_HOST` | `redis:6379` | legacy downstream-template contract retained in `deploy/dapr/components/pubsub.yaml` documentation | **Not consumed by the Story 26.1 production artifact.** The canonical component fixes the in-cluster Service name. |
| `PUBSUB_REDIS_PASSWORD` | _(empty)_ | legacy downstream-template contract retained in `deploy/dapr/components/pubsub.yaml` documentation | **Not consumed by the Story 26.1 production artifact.** It references `redis-secret/password` through DAPR's Kubernetes secret store. |
| `MEMORIES_EVENTSTORE_TOPIC` | `memories-events` (AppHost-injected convention; **required downstream** — see note) | `EventIngestionController.TopicEnvVar`; value injected by `AppHost/Program.cs` | **Env-only**; **required in a downstream overlay** — there is no runtime fallback (see note below). Mirrors config `EventStoreIntegration:Routing:Topic`. |
| `ConnectionStrings__redis` | _(injected from the Redis endpoint by the AppHost)_ | `AppHost/Program.cs`; consumed in `Server/Program.cs` | **Env-only**. |
| `ConnectionStrings__falkordb` | _(injected from the FalkorDB endpoint by the AppHost)_ | `AppHost/Program.cs`; consumed in `Server/Program.cs` | **Env-only**. |
| `MEMORIES_DAPR_APP_ID` | `memories` (when unset) | `AppHost/Program.cs` (`ResolveDaprAppId`) | **Env-only**, optional override. |

In a custom downstream Kubernetes overlay the AppHost is not present, so the operator supplies `MEMORIES_EVENTSTORE_TOPIC` and the `ConnectionStrings__*` values directly. The committed Story 26.1 overlay already wires those values, and its DAPR pub/sub component resolves the Redis password from `redis-secret`; `PUBSUB_REDIS_HOST` and `PUBSUB_REDIS_PASSWORD` apply only to older/custom component templates.

For local AppHost runs, the local identity provider appears in the Aspire dashboard as `security`.
It is Keycloak-backed, but consumers should depend on the `security` resource name rather than a
Keycloak-specific resource name. The AppHost propagates JWT bearer security settings to both the
Memories Server and the MCP host when `security` is enabled. When `EnableKeycloak=false`, the Server
and MCP host use their `Authentication__JwtBearer__*` environment variables or development
appsettings fallback.

> **`MEMORIES_EVENTSTORE_TOPIC` has no runtime default.** The `memories-events` value above is injected only by the AppHost (`src/Hexalith.Memories.AppHost/Program.cs`), which is absent in a downstream overlay. At runtime the topic is resolved purely from this environment variable — `EnvironmentTopicAttribute` for the `/dapr/subscribe` discovery probe, and the `EventStoreIntegration:Routing:Topic` config key for routing — and both resolve to `null` when neither is set, silently stopping event intake. A downstream operator **must** set `MEMORIES_EVENTSTORE_TOPIC` (reusing `memories-events` keeps it consistent with the AppHost-orchestrated deployment) or set `EventStoreIntegration:Routing:Topic` in configuration.

## Pub/sub event-intake deployment surface

Hexalith modules publish domain events to Memories through Dapr pub/sub. The deployment-relevant surface:

| Element | Value | Authoritative source |
| :------ | :---- | :------------------- |
| Pub/sub component name | `pubsub` | `EventIngestionController.PubSubName`; `deploy/dapr/components/pubsub.yaml` `metadata.name`; `TenantEventRoutingOptions.PubSubName` (a validator forces config `EventStoreIntegration:Routing:PubSubName` to equal this). |
| Topic env var | `MEMORIES_EVENTSTORE_TOPIC` | `EventIngestionController.TopicEnvVar` (default value `memories-events`). |
| Source→tenant routing key | `EventStoreIntegration:Routing:SourceToTenantMap` | `TenantEventRoutingOptions.SourceToTenantMap` — a longest-prefix, case-insensitive `Dictionary<string,string>` (empty `{}` by default). |
| Subscription-discovery route | `/dapr/subscribe` | Emitted by `MapSubscribeHandler()` in the Server host; advertises the topic resolved from `MEMORIES_EVENTSTORE_TOPIC` on component `pubsub`. |
| Delivery route | `POST /events/ingest` | `EventIngestionController` (`[Route("events")]` + `[HttpPost("ingest")]`). |
| Server sidecar ports (subscription + delivery) | `3500` (HTTP) / `50001` (gRPC) | See [Dapr sidecar ports](#dapr-sidecar-ports). Dapr reaches the Server through these to read `/dapr/subscribe` and deliver to `/events/ingest`. |

For the deep routing semantics (CloudEvents envelope requirements, at-least-once + replay behaviour, publisher-trust mitigations, the full route surface) see [`../dev/eventstore-integration.md`](../dev/eventstore-integration.md) §1.3–§1.6. This document publishes only the *deployment-config* view and does not duplicate those semantics.

## Backend and dashboard ports (for completeness)

These are the architecture Deployment Topology Baseline values (`_bmad-output/planning-artifacts/architecture.md` §Deployment Topology Baseline), provided so a downstream overlay can wire backing services and observability. They are review-enforced (see [Automated enforcement](#automated-enforcement)); Aspire assigns dynamic host ports locally unless explicitly pinned.

| Component | Port | Notes |
| :-------- | :--- | :---- |
| Redis Stack | `6379` | RediSearch + Vector + Dapr state/pubsub backend. |
| FalkorDB | `6380` | Graph database; container-internal `6379`. `6380` is the architecture-baseline host-port convention — the AppHost exposes the container's `6379` and Aspire assigns the host port (dynamic unless explicitly pinned). |
| Aspire dashboard | `18888` | Local-dev observability UI. |
| Aspire dashboard OTLP receiver | `18889` | OTLP endpoint the dashboard exposes for `OTEL_EXPORTER_OTLP_ENDPOINT` in local dev. |

## The guarantee (rename = breaking-change-for-consumers)

The variable names, the pub/sub component name, the topic env var, and the Dapr sidecar ports above form a **deployment contract** for downstream operators. Renaming any of them — `OTEL_EXPORTER_OTLP_ENDPOINT`, `PUBSUB_REDIS_HOST`, `PUBSUB_REDIS_PASSWORD`, `MEMORIES_EVENTSTORE_TOPIC`, `ConnectionStrings__redis`, `ConnectionStrings__falkordb`, `pubsub`, or changing a sidecar port — silently breaks every consumer kustomization that fills these placeholders, even though no C# member signature changed. Such a change is therefore a **breaking change for consumers** and must carry a breaking-change note. This mirrors the additive-only posture of the Story 18.1 [public-surface-stability contract](../dev/public-surface-stability.md).

## Automated enforcement

A structure-aware drift-guard test protects this contract:
[`tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs`](../../tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs). It runs on every build (plain `[Fact]`s, no Docker/fixture) and:

- **Exact table scope:** parses the OTLP, sidecar, runtime-environment, pub/sub, and backend-port tables under their exact headings and requires data-row counts `1/2/7/6/4`. Canonical variables, services, ports, and operations must occupy their expected normalized cells; matching prose elsewhere cannot satisfy the guard.
- **Bidirectional constant tie (code rename OR doc rename fails the build):** asserts `EventIngestionController.TopicEnvVar == "MEMORIES_EVENTSTORE_TOPIC"`, `EventIngestionController.PubSubName == "pubsub"`, and the runtime-bindable `TenantEventRoutingOptions.PubSubName` default `pubsub`, AND that authoritative table cells carry those values. A rename on either side breaks the build.
- **Server app-id default tie:** asserts `ResolveDaprAppId` in `src/Hexalith.Memories.AppHost/Program.cs` still returns the default app-id `memories`, and that this document both documents `memories` and retains the `memories-server` reconciliation note.
- **Source ↔ table-cell cross-checks (test-enforced):** for literals with no C# constant, the test reads the authoritative source file via the same repo-root marker walk and asserts the literal appears in **both** source and an authoritative table cell:
  - `OTEL_EXPORTER_OTLP_ENDPOINT` and the Production-empty warning service `OtlpExporterWarningHostedService` — `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`.
  - Sidecar ports `3500`, `50001`, `3600`, `50101`; topic value `memories-events`; `ConnectionStrings__redis`, `ConnectionStrings__falkordb`; app-ids `memories-mcp` and `MEMORIES_DAPR_APP_ID` — `src/Hexalith.Memories.AppHost/Program.cs`.
  - `PUBSUB_REDIS_HOST`, `PUBSUB_REDIS_PASSWORD`, and the component `metadata.name` `pubsub` — `deploy/dapr/components/pubsub.yaml`.
  - `SourceToTenantMap` — `src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs`.
  - the config-section prefix `EventStoreIntegration:Routing` — `src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs`.
  - the ingest route attributes `[Route("events")]` and `[HttpPost("ingest")]` — `src/Hexalith.Memories.EventStore/EventIngestionController.cs`.
- **Anti-corruption check:** rejects leaked `content`, `invoke`, `parameter`, or `tool_call` markup through the shared assertion-neutral helper.
- **Review-enforced source meaning:** backend/dashboard ports `6379`, `6380`, `18888`, `18889` must occupy the four exact table rows, while their architecture-projection meaning remains review-enforced because the source of truth is `architecture.md`.

## Production artifact ownership

Story 26.1 closes the former aspirate-manifest deferral with the Kustomize base and production overlay documented above. Aspirate/Helm output remains non-authoritative; deployment changes must update the Kustomize artifact, its executable tests, and this operator contract together.

## Operational runbooks

- [Capacity planning](./capacity-planning.md)
- [Incident response](./incident-response.md)
- [Index rebuild and recovery decisions](./index-rebuild.md)
- [Tenant onboarding and offboarding](./tenant-onboarding-offboarding.md)
- [Upgrade and migration](./upgrade-migration.md)
- [Monitoring and alerting thresholds](./monitoring-alerting-thresholds.md)
- [Hexalith Keys OpenBao operations](./openbao.md)

## References

- Story 18.2 — Deployment Configuration Contract Publication (this contract).
- MEM-2 — Parties consumer integration intake (Sprint Change Proposal 2026-05-27): document the deploy-config contract now, defer aspirate emission.
- [`../dev/eventstore-integration.md`](../dev/eventstore-integration.md) — pub/sub broker wiring (§1.3), routing config (§1.4), and route surface (§1.6); the canonical home for event-intake routing semantics.
- [`../dev/public-surface-stability.md`](../dev/public-surface-stability.md) — companion Story 18.1 contract (host project / assembly / namespace name stability).
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` — OTLP exporter env gate and Production-empty warning.
- `src/Hexalith.Memories.AppHost/Program.cs` — Dapr sidecar ports, topic env, connection-string keys, `ResolveDaprAppId` app-id default.
- `src/Hexalith.Memories.EventStore/EventIngestionController.cs` — `PubSubName` / `TopicEnvVar` constants and the ingest route.
- `deploy/dapr/components/pubsub.yaml` — `pubsub` component, `PUBSUB_REDIS_HOST` / `PUBSUB_REDIS_PASSWORD` interpolation.
- `_bmad-output/planning-artifacts/architecture.md` §Deployment Topology Baseline — backend/dashboard ports and app-id projection.
