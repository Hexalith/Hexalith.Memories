<!-- Review cadence: update when a deploy-config variable name, Dapr sidecar port, pub/sub component name, or app-id default changes, or quarterly — whichever comes first. Last reviewed: 2026-06-24. -->

# Deployment Configuration Contract (Story 18.2)

This document publishes the canonical environment-variable, Dapr sidecar-port, OTLP-exporter, and pub/sub event-intake configuration surface a downstream operator must supply to deploy Memories into a Kubernetes overlay, so placeholder-shaped env literals in consumer kustomizations can be replaced with real, documented values without first running aspirate.

Origin: MEM-2 (Parties consumer integration intake, Sprint Change Proposal 2026-05-27). The values below already exist in code (OTLP is environment-gated, the Dapr ports are set by the AppHost, the pub/sub component is shared with Redis); this contract publishes them in one operator-facing place and guards them against drift. **Full aspirate manifest emission is explicitly deferred** — see [Deferred: aspirate manifest emission](#deferred-aspirate-manifest-emission) below.

> **Code is the source of truth.** Every literal in this document is mirrored from the authoritative source file named in its row. A drift-guard test (see [Automated enforcement](#automated-enforcement)) fails the build if a documented name diverges from code.

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
| `PUBSUB_REDIS_HOST` | `redis:6379` | `deploy/dapr/components/pubsub.yaml` (YAML-interpolated) | **Env-only** (no C# constant; substituted into the Dapr component YAML). |
| `PUBSUB_REDIS_PASSWORD` | _(empty)_ | `deploy/dapr/components/pubsub.yaml` (YAML-interpolated) | **Env-only**; inject from a secret in production. |
| `MEMORIES_EVENTSTORE_TOPIC` | `memories-events` (AppHost-injected convention; **required downstream** — see note) | `EventIngestionController.TopicEnvVar`; value injected by `AppHost/Program.cs` | **Env-only**; **required in a downstream overlay** — there is no runtime fallback (see note below). Mirrors config `EventStoreIntegration:Routing:Topic`. |
| `ConnectionStrings__redis` | _(injected from the Redis endpoint by the AppHost)_ | `AppHost/Program.cs`; consumed in `Server/Program.cs` | **Env-only**. |
| `ConnectionStrings__falkordb` | _(injected from the FalkorDB endpoint by the AppHost)_ | `AppHost/Program.cs`; consumed in `Server/Program.cs` | **Env-only**. |
| `MEMORIES_DAPR_APP_ID` | `memories` (when unset) | `AppHost/Program.cs` (`ResolveDaprAppId`) | **Env-only**, optional override. |

In a downstream Kubernetes overlay the AppHost is not present, so the operator supplies `PUBSUB_REDIS_HOST`, `PUBSUB_REDIS_PASSWORD`, `MEMORIES_EVENTSTORE_TOPIC`, and the `ConnectionStrings__*` values directly (the `ConnectionStrings__*` pair points the Server at the in-cluster Redis and FalkorDB services).

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

A content-asserting drift-guard test protects this contract:
[`tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs`](../../tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs). It runs on every build (plain `[Fact]`s, no Docker/fixture) and:

- **Bidirectional constant tie (code rename OR doc rename fails the build):** asserts `EventIngestionController.TopicEnvVar == "MEMORIES_EVENTSTORE_TOPIC"`, `EventIngestionController.PubSubName == "pubsub"`, and the runtime-bindable `TenantEventRoutingOptions.PubSubName` default `pubsub`, AND that this document contains those same values. A rename on either side breaks the build.
- **Server app-id default tie:** asserts `ResolveDaprAppId` in `src/Hexalith.Memories.AppHost/Program.cs` still returns the default app-id `memories`, and that this document both documents `memories` and retains the `memories-server` reconciliation note.
- **Source ↔ doc cross-checks (test-enforced):** for literals with no C# constant, the test reads the authoritative source file via the same repo-root marker walk and asserts the literal appears in **both** source and this document:
  - `OTEL_EXPORTER_OTLP_ENDPOINT` and the Production-empty warning service `OtlpExporterWarningHostedService` — `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`.
  - Sidecar ports `3500`, `50001`, `3600`, `50101`; topic value `memories-events`; `ConnectionStrings__redis`, `ConnectionStrings__falkordb`; app-ids `memories-mcp` and `MEMORIES_DAPR_APP_ID` — `src/Hexalith.Memories.AppHost/Program.cs`.
  - `PUBSUB_REDIS_HOST`, `PUBSUB_REDIS_PASSWORD`, and the component `metadata.name` `pubsub` — `deploy/dapr/components/pubsub.yaml`.
  - `SourceToTenantMap` — `src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs`.
  - the config-section prefix `EventStoreIntegration:Routing` — `src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs`.
  - the ingest route attributes `[Route("events")]` and `[HttpPost("ingest")]` — `src/Hexalith.Memories.EventStore/EventIngestionController.cs`.
- **Doc-presence (test-enforced):** the routing key `EventStoreIntegration:Routing:SourceToTenantMap` and the subscription-discovery route `/dapr/subscribe` are asserted present in this document. The delivery route `POST /events/ingest` is additionally source-tied through its controller route attributes above.
- **Review-enforced (not reflectable):** the backend/dashboard ports `6379`, `6380`, `18888`, `18889` are architecture-projection values; they are asserted present in this document but their authoritative form lives in `architecture.md`, so divergence is caught by review, not by the test. The exact composition of the route surface (`/dapr/subscribe`, `POST /events/ingest`) is additionally guarded by `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs` against `../dev/eventstore-integration.md`.

## Deferred: aspirate manifest emission

Full aspirate (or equivalent) manifest generation — emitting ready-to-apply Kubernetes/Dapr manifests from the AppHost topology — is a larger, separable effort and is **explicitly deferred to a future story**. This story delivers the documented contract and its drift guard only. The deferral is recorded against `MEM-2` in [`_bmad-output/implementation-artifacts/deferred-work.md`](../../_bmad-output/implementation-artifacts/deferred-work.md); no follow-up story id is assigned yet.

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
