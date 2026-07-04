<!-- Review cadence: update when an `app.MapX("/api/…")` route is added/removed/renamed, when the pub/sub delivery or subscription-discovery route changes, when a health-probe path changes, or when the MCP transport route changes; otherwise quarterly — whichever comes first. Last reviewed: 2026-07-04. -->

# Invocable Route and Operation Surface Contract (Story 18.3)

This document publishes the canonical invocable surface of the Memories Server — every `/api/*` REST route plus the Dapr pub/sub delivery and subscription-discovery operations — in one operator-facing place so an external Dapr access-control policy (`accesscontrol.memories.yaml`) can be verified against real operation paths instead of an unverified `/process` placeholder.

Origin: MEM-3 (Parties consumer integration intake, Sprint Change Proposal 2026-05-27). The routes below already exist in code (46 minimal-API endpoints in the Server host, a pub/sub controller, and the framework-emitted subscription handler); this contract **publishes** them as the ACL-verifiable surface and **guards** them against drift. **Full OpenAPI/Swagger document emission stays explicitly deferred** — see [Deferred: OpenAPI document generation](#deferred-openapi-document-generation) below.

> **Code is the source of truth.** Every route in this document is mirrored from the authoritative source file named in its section. A content-asserting drift-guard test (see [Automated enforcement](#automated-enforcement)) fails the build if a mapped endpoint is added without documenting it, or if a documented route diverges from code.

## Dapr ACL framing and operation semantics

The ACL `accesscontrol.memories.yaml` governs the **Memories Server** Dapr app-id `memories` (the default; overridable with `MEMORIES_DAPR_APP_ID` — see [`./deployment-configuration.md`](./deployment-configuration.md#dapr-sidecar-ports)). The Memories MCP host runs under the **separate** app-id `memories-mcp` and is a different ACL target — an ACL for `memories` must not reference the MCP surface (see [MCP transport surface](#mcp-transport-surface-separate-app-id)).

The surface decomposes into two Dapr operation kinds, and an ACL must treat them differently:

- **Service invocation — the `/api/*` table.** A caller reaches a REST route through Dapr service invocation at `/v1.0/invoke/memories/method/<path>`. The ACL `operation` is the route path **without the leading slash** (the `method/…` segment), the `app` is `memories`, and `httpVerb` is the row's HTTP method. Translate each table row directly: `operation` = `method` joined with the path column, `httpVerb` = the method column. For example, the search row maps to operation `method/api/search` with verb `GET`, and the primary ingest row to operation `method/api/ingest` with verb `POST`.
- **Pub/sub delivery — `POST /events/ingest`.** This route is **not** reached through service invocation. The Dapr sidecar delivers CloudEvents to it after discovering the subscription via `/dapr/subscribe`; a service-invocation `method/*` ACL rule does not gate pub/sub delivery. See [Pub/sub event-intake operation surface](#pubsub-event-intake-operation-surface).

The Server also enforces application-layer security on `/api/*`: Story 20.1 added JWT bearer fallback authorization, Story 20.2 added principal-claim tenant authorization for tenant path/query/body routes, and Story 20.5 added inbound request limiting partitioned by authenticated tenant context. The health and Dapr infrastructure routes remain explicit anonymous exceptions for platform probes and sidecar delivery.

This satisfies "method, path, and Dapr operation semantics" (AC2): the table gives method + path, and the two bullets above give the Dapr operation mapping for each.

## REST `/api/*` operation surface

All 46 routes below are minimal-API endpoints mapped in `src/Hexalith.Memories.Server/Program.cs` (`app.MapGet/Post/Put/Delete/Patch`). There are no `MapGroup`/route-group prefixes; each path is the full template. The drift-guard test derives this list from the source, so a newly added endpoint that is not documented here fails the build.

| Area | Method + path | Purpose |
| :--- | :--- | :--- |
| Ingestion | `POST /api/ingest` | Start a content-ingestion workflow. |
| Ingestion | `GET /api/ingest/{instanceId}` | Read a tenant-authorized safe ingestion workflow status projection (`IngestionWorkflowStatus`), not raw Dapr `WorkflowState`. |
| Ingestion | `POST /api/ingest/url` | Ingest content from a URL. |
| Ingestion | `POST /api/ingest/directory` | Ingest a directory batch. |
| Ingestion | `GET /api/ingest/batches/{batchId}` | Read directory-batch ingestion status after authorizing the stored batch tenant before per-file status fan-out. |
| Tenants | `GET /api/tenants/{tenantId}/embedding-config` | Read the tenant embedding configuration. |
| Tenants | `PUT /api/tenants/{tenantId}/embedding-config` | Replace the tenant embedding configuration. |
| Tenants | `POST /api/tenants` | Provision a tenant. |
| Tenants | `GET /api/tenants/{tenantId}/provision-status/{instanceId}` | Read tenant-provisioning workflow status. |
| Tenants | `GET /api/tenants` | List tenants. |
| Tenants | `GET /api/tenants/{tenantId}` | Read a tenant. |
| Tenants | `GET /api/tenants/{tenantId}/configuration` | Read the resolved tenant configuration. |
| Tenants | `PATCH /api/tenants/{tenantId}` | Update the tenant display name. |
| Tenants | `DELETE /api/tenants/{tenantId}` | Start tenant deletion. |
| Tenants | `GET /api/tenants/{tenantId}/deletion-status/{instanceId}` | Read tenant-deletion workflow status. |
| Tenants | `POST /api/tenants/{tenantId}/verify` | Verify a tenant. |
| Consistency | `POST /api/tenants/{tenantId}/consistency/verify` | Start a consistency-verification run. |
| Consistency | `GET /api/tenants/{tenantId}/consistency/verify/{instanceId}` | Read consistency-verification status. |
| Consistency | `GET /api/tenants/{tenantId}/consistency/inspect/{memoryUnitId}` | Inspect a memory unit's consistency. |
| Consistency | `POST /api/tenants/{tenantId}/consistency/repair` | Start a consistency-repair run. |
| Consistency | `GET /api/tenants/{tenantId}/consistency/repair/{instanceId}` | Read consistency-repair status. |
| Export | `GET /api/tenants/{tenantId}/cases/{caseId}/export` | Export a single case. |
| Export | `GET /api/tenants/{tenantId}/export` | Export all cases for a tenant. |
| Cases | `POST /api/tenants/{tenantId}/cases` | Create a case. |
| Cases | `GET /api/tenants/{tenantId}/cases` | List cases. |
| Cases | `GET /api/tenants/{tenantId}/cases/{caseId}` | Read a case. |
| Cases | `GET /api/tenants/{tenantId}/cases/{caseId}/status` | Read case status. |
| Cases | `GET /api/tenants/{tenantId}/cases/{caseId}/failed-units` | List a case's failed memory units. |
| Memory units | `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` | Read a memory unit. |
| Memory units | `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri` | Resolve a source URI to its canonical memory-unit id by exact key (Story 18.5; literal segment, beats the `{memoryUnitId}` template). |
| Memory units | `POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest` | Re-ingest a single memory unit. |
| Memory units | `POST /api/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest` | Re-ingest all failed units in a case. |
| Cases | `GET /api/tenants/{tenantId}/cases/{caseId}/activity` | Read case activity. |
| Members | `PUT /api/tenants/{tenantId}/cases/{caseId}/members/{memberId}` | Add or update a case member. |
| Members | `DELETE /api/tenants/{tenantId}/cases/{caseId}/members/{memberId}` | Remove a case member. |
| Members | `GET /api/tenants/{tenantId}/cases/{caseId}/members` | List case members. |
| Memory units | `DELETE /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` | Delete a memory unit. |
| Cases | `DELETE /api/tenants/{tenantId}/cases/{caseId}` | Delete a case. |
| Annotations | `POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations` | Add an annotation to a memory unit. |
| Annotations | `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations` | List a memory unit's annotations. |
| Search | `GET /api/search` | Hybrid (semantic + keyword + graph) search. |
| Graph | `GET /api/tenants/{tenantId}/traverse` | Traverse the tenant relation graph. |
| Telemetry | `GET /api/tenants/{tenantId}/telemetry/summary` | Read the tenant telemetry summary. |
| Handlers | `GET /api/handlers` | Inspect the registered-handler snapshot. **Experimental (`HXL002`).** |
| Handlers | `GET /api/tenants/{tenantId}/handlers/mismatches` | Detect handler routing mismatches. **Experimental (`HXL002`).** |
| Graph | `PATCH /api/tenants/{tenantId}/edges/confidence` | Adjust relation-edge confidence. |

**Experimental diagnostics (`HXL002`).** The two `Handlers` rows are an experimental surface: each emits the `X-Memories-API-Experimental: HXL002` response header on every 2xx response, and SDK callers see the compile-time `[Experimental("HXL002")]` attribute. See [`../dev/experimental-apis.md`](../dev/experimental-apis.md). They are real, mapped routes today and are part of the ACL-verifiable surface; treat them as provisional rather than absent.

## Pub/sub event-intake operation surface

Hexalith domain modules **publish CloudEvents to DAPR** (to the configured pub/sub component and shared topic); they do **not** invoke the Memories REST ingestion routes for event streams. The Memories Server is the sidecar-managed subscriber. The pub/sub operation surface is:

| Operation | Method + path | Authoritative source | Notes |
| :--- | :--- | :--- | :--- |
| Subscription discovery | `GET /dapr/subscribe` | `app.MapSubscribeHandler()` — `Program.cs` | Framework-emitted; advertises the topic + route `/events/ingest` on component `pubsub`. |
| Pub/sub delivery | `POST /events/ingest` | `EventIngestionController` — `[Route("events")]` + `[HttpPost("ingest")]` | CloudEvents intake; content types `application/json`, `application/cloudevents+json`. Topic resolved from `MEMORIES_EVENTSTORE_TOPIC` on component `pubsub`. |

The topic is resolved at startup from the `MEMORIES_EVENTSTORE_TOPIC` environment variable (constant `EventIngestionController.TopicEnvVar`); the component name `pubsub` is the constant `EventIngestionController.PubSubName`. Because delivery is a pub/sub operation rather than a service-invocation `method/*` operation, an ACL that allows or denies `method/*` operations does not control event delivery to `/events/ingest`.

For the deep routing semantics (CloudEvents envelope requirements, at-least-once + replay behaviour, publisher-trust mitigations) see [`../dev/eventstore-integration.md`](../dev/eventstore-integration.md) §1.3–§1.6, and for the deployment-config view (ports, env vars, component name) see [`./deployment-configuration.md`](./deployment-configuration.md#pubsub-event-intake-deployment-surface). This document publishes only the enumerated operation surface and does not duplicate those semantics.

## Health and infrastructure probes

The three health-probe paths below are mapped by `app.MapDefaultEndpoints()` on **both** the Server and the MCP host; their path constants live in `HealthEndpointPaths` (`src/Hexalith.Memories.ServiceDefaults/Health/HealthEndpointPaths.cs`). They are infrastructure probes, not part of the application operation surface, and a downstream `memories` ACL normally does not need to allow service invocation to them.

| Probe | Path | Constant | Semantics |
| :--- | :--- | :--- | :--- |
| Aggregate health | `/health` | `HealthEndpointPaths.Health` | Surfaces every registered health check. |
| Liveness | `/alive` | `HealthEndpointPaths.Alive` | Runs only checks tagged `live`. |
| Readiness | `/ready` | `HealthEndpointPaths.Ready` | Runs only checks tagged `ready`. |

See [`../dev/health-checks.md`](../dev/health-checks.md) for probe semantics and tag conventions.

## MCP transport surface (separate app-id)

The Memories MCP host (`src/Hexalith.Memories.Mcp/Program.cs`) exposes a single transport route `POST /mcp` (`app.MapMcp("/mcp").RequireAuthorization()`), a JWT-Bearer-authenticated Streamable HTTP endpoint serving the agent tools (`search_memory`, `ingest_content`, `traverse_relations`, `get_case_info`). It runs under the **separate** Dapr app-id `memories-mcp`, so an ACL for the `memories` app-id **must not** reference `/mcp`. The MCP host also maps the same `/health`, `/alive`, and `/ready` probes via `MapDefaultEndpoints()`. See [`../dev/mcp-server.md`](../dev/mcp-server.md).

## No `/process` operation exists

There is **no `/process` operation anywhere on the Memories surface** — not in the `/api/*` REST routes, not in the pub/sub controller, and not as a framework route. A grep for `/process` across `src/` returns no production route literal. An ACL or downstream runbook that references `/process` is using the wrong operation path: domain events are delivered to `POST /events/ingest` (pub/sub) and there is no `/process` service-invocation method to allow. This mirrors the refutation already recorded in [`../dev/eventstore-integration.md`](../dev/eventstore-integration.md) §1.6. The drift-guard test asserts the `/process` literal is absent from both `Program.cs` and `EventIngestionController.cs`, so this negative claim is enforced against code, not just prose.

## The guarantee (rename = breaking-change-for-consumers)

The route templates, the pub/sub delivery and discovery routes, the topic env var, and the pub/sub component name above form an **operation-surface contract** for downstream ACL authors. Renaming or removing any `/api/*` route, the `/events/ingest` delivery route, the `/dapr/subscribe` discovery route, the `MEMORIES_EVENTSTORE_TOPIC` env var, or the `pubsub` component name silently breaks every consumer ACL rule that targets the old operation, even though no C# member signature changed. Such a change is therefore a **breaking change for consumers** and must carry a breaking-change note. This mirrors the additive-only posture of the Story 18.1 [public-surface-stability contract](../dev/public-surface-stability.md) and the Story 18.2 [deployment-configuration contract](./deployment-configuration.md).

## Automated enforcement

A content-asserting drift-guard test protects this contract:
[`tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs`](../../tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs). It runs on every build (plain `[Fact]`s, no Docker/fixture, repo-root marker walk) and enforces:

- **Forward tie (code → doc), the core sync guard:** every `app.Map(Get|Post|Put|Delete|Patch)("/api/…")` route literal is regex-extracted from `Program.cs` and asserted present verbatim (`Case.Sensitive`) in this document. A new endpoint added without documenting it fails the build.
- **Count tie:** the number of extracted `/api/*` route literals (currently **46**) must equal the number of `/api/` route rows in this document — defending against both silent omission and phantom rows. The failure message emits both counts so the Change Log delta is visible.
- **Pub/sub constant tie (bidirectional):** asserts `EventIngestionController.PubSubName == "pubsub"`, that the controller source contains `[Route("events")]` and `[HttpPost("ingest")]`, and that this document contains `POST /events/ingest`; asserts `app.MapSubscribeHandler()` appears in `Program.cs` and this document contains `/dapr/subscribe`.
- **Health constant tie:** asserts this document contains each `HealthEndpointPaths.Health` / `.Alive` / `.Ready` value (`/health`, `/alive`, `/ready`) **in its health-table row** (the path cell adjacent to its constant-name cell), so both a code-side rename of a probe path and a doc-side deletion of the health table fail the build — a bare path substring elsewhere in this document (prose, cross-links, references) cannot satisfy it.
- **`/process` negative tie (code-tied):** asserts neither `Program.cs` nor `EventIngestionController.cs` contains a `/process` route literal, **and** that this document contains the explicit "no `/process`" statement.
- **MCP route tie (source-text):** reads `src/Hexalith.Memories.Mcp/Program.cs` and asserts it contains `MapMcp("/mcp")`, and that this document documents `/mcp` under the `memories-mcp` app-id.
- **Dapr operation-mapping tie (AC2):** asserts this document contains the service-invocation operation form `/v1.0/invoke/memories/method/<path>` and the worked translation example `method/api/search`, so the Dapr-operation-semantics section cannot be silently dropped.
- **Publish-via-DAPR tie (AC4):** asserts this document states that domain modules **publish CloudEvents to DAPR** and do **not** invoke the REST ingestion routes for event streams — the explicit AC4 claim.
- **Experimental marker tie (`HXL002`, code ↔ doc):** asserts `Program.cs` stamps the `X-Memories-API-Experimental` header with `HXL002` and that this document keeps both the `X-Memories-API-Experimental: HXL002` header reference and the `Experimental (HXL002)` row marker, so the provisional status of the two `Handlers` routes cannot drift on either side.
- **Review-enforced (not literal-tied):** `/dapr/subscribe` is framework-emitted (no route literal in app code), so it is enforced by doc-presence plus the presence of `MapSubscribeHandler()` in `Program.cs`, not by a literal-string tie to a `/dapr/subscribe` source token. The per-row purpose text in the tables remains review-enforced.

## Deferred: OpenAPI document generation

AC2 permits "an OpenAPI document **or** a maintained route-surface doc". The repository has **no** OpenAPI/Swagger setup today (`AddOpenApi`, `MapOpenApi`, `AddSwaggerGen`, `UseSwagger`, Swashbuckle, and `Microsoft.AspNetCore.OpenApi` are all absent; no `openapi.json` exists). Standing up OpenAPI generation for 46 minimal-API endpoints plus the pub/sub controller is a larger, separable effort and is **explicitly deferred**. This story delivers the maintained route-surface contract and its drift guard only. The deferral is recorded as `MEM-3-OPENAPI` in [`../../_bmad-output/implementation-artifacts/deferred-work.md`](../../_bmad-output/implementation-artifacts/deferred-work.md); no follow-up story id is assigned yet.

## References

- Story 18.3 — Invocable Route and Operation Surface Publication (this contract).
- MEM-3 — Parties consumer integration intake (Sprint Change Proposal 2026-05-27): publish the real operation surface so the Parties ACL can replace the unverified `/process` placeholder; defer OpenAPI emission.
- [`./deployment-configuration.md`](./deployment-configuration.md) — sibling Story 18.2 operations contract (app-id, ports, env vars, pub/sub deployment surface).
- [`../dev/eventstore-integration.md`](../dev/eventstore-integration.md) §1.3–§1.6 — pub/sub broker wiring, routing config, route surface, and the `/process` refutation; the canonical home for event-intake routing semantics.
- [`../dev/experimental-apis.md`](../dev/experimental-apis.md) — the `HXL002` experimental diagnostic for the two `Handlers` routes.
- [`../dev/mcp-server.md`](../dev/mcp-server.md) — MCP `/mcp` transport surface and the four agent tools.
- [`../dev/health-checks.md`](../dev/health-checks.md) — health-probe semantics and tag conventions.
- [`../dev/public-surface-stability.md`](../dev/public-surface-stability.md) — companion Story 18.1 contract (host project / assembly / namespace name stability).
- `src/Hexalith.Memories.Server/Program.cs` — the 46 `/api/*` `app.MapX` route literals; `MapDefaultEndpoints()`; middleware order `UseCloudEvents()` → `MapControllers()` → `MapSubscribeHandler()`.
- `src/Hexalith.Memories.EventStore/EventIngestionController.cs` — `[Route("events")]`, `[HttpPost("ingest")]`, `PubSubName`, `TopicEnvVar`.
- `src/Hexalith.Memories.ServiceDefaults/Health/HealthEndpointPaths.cs` — `/health`, `/alive`, `/ready` path constants.
- `src/Hexalith.Memories.Mcp/Program.cs` — `MapMcp("/mcp").RequireAuthorization()` and the MCP host `MapDefaultEndpoints()`.
- `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` — the drift-guard test enforcing this contract.
