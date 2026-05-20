# MCP Server (Story 10.2)

## What the MCP Server is and is not

The Hexalith.Memories MCP Server is a small ASP.NET Core service that exposes memory operations as
typed [Model Context Protocol](https://modelcontextprotocol.io) tools. It is the LLM-agent surface
of the Memories system. It is **not** a general REST gateway, a caching layer, or a place to put
new business logic — every tool delegates to the existing Memories Server REST endpoints via DAPR
service invocation.

* Tool surface: `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info` (FR54).
* Transport: Streamable HTTP (`MapMcp()` + `WithHttpTransport(o => o.Stateless = true)`).
* Topology: separate DAPR-sided service (app-id `memories-mcp`) with its own sidecar pinned to
  ports 3600/50101.
* Boundary: MCP → Memories Server via `DaprClient.CreateInvokeHttpClient("memories-server")`. No
  direct Redis / FalkorDB / secret-store access (NFR11 + architecture.md §Cross-Cutting Concerns
  #4).

## Bearer authentication

The `/mcp` endpoint requires a JWT bearer token. The MCP host wires
`AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`, `AddJwtBearer()`, `AddMcp()`,
`AddAuthorization()`, `AddAuthorizationFilters()`, and `MapMcp().RequireAuthorization()`.

Configuration is read from `Authentication:JwtBearer`:

| Key | Notes |
|---|---|
| `Authority` | OIDC discovery endpoint for production IdPs. |
| `Issuer` | Required expected `iss`. |
| `Audience` | Required expected `aud`. |
| `SigningKey` | Development/test HS256 key when `Authority` is unset; must provide at least 32 bytes of effective key material. |
| `RequireHttpsMetadata` | Keep `true` outside local development. |
| `TenantClaimName` | Defaults to `tenant_id`; common alternatives are `tid` for Entra, a namespaced Auth0 claim, or `tenant` for Okta. |

JWT validation preserves original claim names, validates issuer/audience/signature/lifetime, requires
signed tokens and expiration, uses a 1-minute clock skew, and allows `HS256`, `RS256`, and `ES256`
by default. Failed authentication returns a sanitized RFC 6750 `WWW-Authenticate` header plus
`application/problem+json`; token values are never logged.

Each tool cross-checks its `tenantId` argument against normalized `memories:tenant` claims before
calling the Memories Server. Mismatches return MCP `CallToolResult { IsError = true }` with code
`TENANT_FORBIDDEN`; malformed tenant ids return `TENANT_MALFORMED` without echoing the unsafe input.

Recommended bearer lifetime is 15 minutes or less. MCP clients are responsible for token refresh;
Story 10.2 does not implement refresh-token rotation or OAuth PKCE.

For local smoke tests, mint a development token with:

```powershell
.\scripts\dev\mint-test-jwt.ps1 -TenantId acme
```

## Server ingress inventory

Current Memories Server ingress for MCP traffic is DAPR service invocation from `memories-mcp` to
`memories-server`, secured by DAPR API token mode when `DAPR_API_TOKEN_MODE=enabled`. Adding any
second direct Server ingress (admin UI, ops dashboard, direct REST client, public CLI endpoint) is a
gated event: land a Server-level authentication story before exposing that ingress.

## The four registered tools

### `search_memory`

| Parameter | Type | Required | Default | Notes |
|---|---|---|---|---|
| `tenantId` | string | yes | — | Tenant whose memories should be searched. |
| `query` | string | yes | — | Free-text query. |
| `case` | string | no | — | Optional case identifier for case-scoped search. |
| `axes` | enum | no | `Hybrid` | One of `Syntactic` / `Semantic` / `Hybrid` (case-insensitive on the wire — the SDK 1.2.0 schema renders PascalCase but tools deserialize either form). Use `traverse_relations` for graph traversal because graph operations require a starting memory unit. |
| `maxResults` | integer | no | `10` | Clamped to `[1, 100]`. |
| `tokenBudget` | integer | no | — | Server-side output budget. Results are truncated by relevance rank and response metadata reports `omittedCount`, `estimatedTokensTotal`, and `omittedReason`. |
| `explain` | boolean | no | `false` | Include explain metadata. |

### `ingest_content`

| Parameter | Type | Required | Default | Notes |
|---|---|---|---|---|
| `tenantId` | string | yes | — | |
| `caseId` | string | yes | — | |
| `content` | string | yes | — | UTF-8 text payload (binary support deferred to 10.2). |
| `sourceType` | enum | no | `File` | Only `File` is honored in 10.1 — `Url` and `Event` reject with `UNSUPPORTED_SOURCE_TYPE`. |
| `sourceUri` | string | no | `mcp://content` | |
| `contentType` | string | no | `text/plain` | |
| `ingestedBy` | string | no | `mcp` | |

Returns `{ "workflowInstanceId": "..." }` on success.

### `traverse_relations`

| Parameter | Type | Required | Default | Notes |
|---|---|---|---|---|
| `tenantId` | string | yes | — | |
| `from` | string | yes | — | Memory unit id to start traversal from. |
| `depth` | integer | no | `3` | Clamped to `[0, 10]`. |
| `edgeType` | string | no | — | Comma-separated edge type list. Invalid values reject client-side with `INVALID_EDGE_TYPE` — no server round-trip. Valid values: `causedBy`, `correlatedWith`, `references`, `contains`, `annotates`. |
| `tokenBudget` | integer | no | — | Server-side output budget. Traversal truncates leaves before the primary path where possible and reports `primaryPathIntact`. |
| `caseId` | string | no | — | Optional graph scope. **Note:** the original epic AC #4 said `graph_scope (object, optional)`. 10.1 flattens this to a single `caseId` string for LLM ergonomics and to match the underlying `GET /api/tenants/{tenantId}/traverse` endpoint shape. See _Graph scope parameter simplification_ below. |

### `get_case_info`

| Parameter | Type | Required | Default | Notes |
|---|---|---|---|---|
| `tenantId` | string | yes | — | |
| `caseId` | string | yes | — | |

Returns the `Case` record (id, name, status, member counts, recent activity timestamps).

## DAPR service-invocation hop

Every tool delegates to the existing `MemoriesClient` (`src/Hexalith.Memories.Client.Rest/`)
resolved over a DAPR-supplied `HttpClient`:

```csharp
HttpClient invokeClient = DaprClient.CreateInvokeHttpClient(appId: "memories-server");
MemoriesMcpDaprInvocationHandler.ApplyDaprApiToken(invokeClient);
```

`CreateInvokeHttpClient` resolves the local sidecar URL via the `DAPR_HTTP_ENDPOINT` env var
(defaults to `http://localhost:3500`), injects the `dapr-app-id: memories-server` header on every
request, and routes the call through the local sidecar's service-invocation path. The handler
adds `dapr-api-token` from the env when `DAPR_API_TOKEN_MODE=enabled` (Story 5.4 AC3 parity).

`HttpClient`'s default OpenTelemetry instrumentation (registered via `ServiceDefaults`) auto-adds
the `traceparent` header; the sidecar preserves it. The MCP-to-Server hop therefore inherits the
Story 7.5 / 8.4 distributed-trace continuity with no extra wiring.

### Why DAPR service invocation over direct HttpClient

1. **Secret scoping (NFR11 + architecture §Cross-Cutting Concerns #4)** — DAPR's secret-store
   scoping restricts which `app-id` can resolve which secret. The Memories Server has access to
   embedding-provider API keys; the MCP Server `app-id` is intentionally NOT listed in the scope.
   Routing every cross-service call through the sidecar makes this the only pathway.
2. **Token-mode parity (`DAPR_API_TOKEN_MODE`)** — the sidecar enforces `dapr-api-token` on
   inbound requests. A direct HTTP call would skip this gate.
3. **Operational uniformity** — the Aspire Dashboard, distributed-trace view, and resilience
   policies all assume sidecar traversal.

## Local dev workflow

```bash
dotnet run --project src/Hexalith.Memories.AppHost
```

Both `memories-server` and `memories-mcp` boot under the Aspire AppHost with their own DAPR
sidecars. The MCP endpoint is exposed at the Aspire-allocated HTTP port (open the Aspire Dashboard
and click `memories-mcp` to see the live URL), with the MCP server itself listening on `/mcp` for
Streamable HTTP traffic.

Connect any compliant MCP client (Claude Desktop, a custom `McpClient`, etc.) to
`http://<host>:<port>/mcp` with `Authorization: Bearer <token>`. The four tools above will appear
in the tool list after authentication.

### Local dev ports

Story 10.1 pins the MCP sidecar to `DaprHttpPort = 3600` / `DaprGrpcPort = 50101` so it does not
collide with the Memories Server sidecar at 3500/50001. These are **local-dev convenience values**
— under `Aspire.Hosting.Testing`, ports are randomized and resolved via `DAPR_HTTP_ENDPOINT`.

## Error response shape

Every tool error result is an MCP `CallToolResult { IsError = true }` carrying both prose and
structured payloads:

```text
[CODE] (service=memories-server): message suggestion
```

```json
{
  "code": "TENANT_NOT_FOUND",
  "service": "memories-server",
  "tool": "search_memory",
  "message": "Tenant 'acme' was not found.",
  "suggestion": "Run memories tenant list."
}
```

Generic / network failures map through `McpErrorMapper.MapGeneric`, which sanitizes the message
(no stack traces, no echo of caller-supplied input) and surfaces either `NETWORK_ERROR` or
`INTERNAL_ERROR` as the code.

## What remains deferred

* **MCP-specific trace-hop assertion** in `AspireEndToEndTraceTests`.
* **MCP sampling / elicitation** — Stateless mode remains enabled; server-to-client requests are
  out of scope for bearer-only auth.
* **MCP resources / prompts primitives** — only tools are registered.
* **OAuth PKCE / per-tool scopes / refresh-token rotation** — deferred to Phase 2. The 10.2 MVP
  model is authenticated caller plus matching tenant claim.

See `_bmad-output/implementation-artifacts/deferred-work.md` for the tracking entries.

## Operator rollback

If a disaster ships with `memories-mcp` (e.g., sidecar configuration leak, schema-emission
crash blocking AppHost startup):

1. **Single-line revert in AppHost** — comment out or remove the
   `AddProject<Projects.Hexalith_Memories_Mcp>("memories-mcp")...` block in
   `src/Hexalith.Memories.AppHost/Program.cs`. All other services continue running unchanged —
   the MCP surface is additive, not in the critical path of any existing feature.
2. **No data migration consequences** — MCP is read-through + ingestion passthrough, no
   MCP-specific persisted state, no schema change in Redis or FalkorDB.
3. **Consumer impact** — only LLM-agent clients that were already talking to the MCP endpoint.
   Internal consumers (CLI, Server REST, EventStore) are untouched.
4. **Re-land path** — fix the root cause, re-add the AppHost block, re-ship. No architectural
   reshape required.

## Graph scope parameter simplification

Epic AC #4 originally specified `graph_scope (object, optional)` for `traverse_relations`. The
current server endpoint (`GET /api/tenants/{tenantId}/traverse`) accepts only `caseId` +
`edgeTypes` (comma-separated) as query parameters. A nested `graph_scope` object on the MCP tool
schema would (a) require the LLM to construct nested JSON per-call — more tokens, lower tool-call
success rate, and (b) introduce a contract mismatch between the declared schema (object) and the
flat pass-through (two strings). 10.1 therefore flattens `graph_scope` into the optional
`caseId` string parameter. When the server grows richer graph-scope semantics (sourceFilter,
tenantCrossReference, etc.), the MCP schema expands at that point.

## Tool return types

Tool methods return protocol-level `CallToolResult` instances. Successful results carry the
JSON-serialized Memories contract in `content[0].text` and the same object in
`structuredContent`; search results add the canonical `evidencePacket` member so MCP agents read
the same trust semantics as CLI JSON and future UI consumers. Error results set `isError=true`
and carry the mapper's structured error payload, optionally with packet-style state and recovery
fields. See [`evidence-packet.md`](evidence-packet.md). Rationale:

1. **Explicit serialization control** — every tool routes through
    `McpToolResultSerializer.Serialize<T>` which uses `MemoriesJsonContext.Options` (camelCase,
    source-generated) before putting the payload into the protocol result. Returning a typed record
    would let the SDK pick its own serializer and create wire-format drift between MCP responses
    and the REST surface.
2. **Error-path correctness** — failures return actual MCP error results, not JSON strings that
    happen to contain an `isError` property. MCP clients can reliably inspect `CallToolResult.IsError`.
3. **Success-path symmetry** — success results mirror the text-content shape so the LLM always
    finds the payload at `content[0].text`, while clients that prefer structure can read
    `structuredContent`.
4. **AOT future-proofing** — the SDK schema generator is reflection-based for typed return
    shapes, which is AOT-fragile. Returning `CallToolResult` with pre-serialized content avoids
    adding typed return shapes to the MCP schema surface. AOT is not enabled in 10.1 (Risk #2 —
    tracked in deferred-work).

## CancellationToken handling

Tool methods propagate `OperationCanceledException` rather than mapping it to a tool-level error.
Rationale: the MCP client (which already knows it cancelled) does not benefit from a structured
"the tool failed" payload — it invites retry loops on user-initiated cancels. The MCP SDK handles
cancelled calls at the protocol layer.
